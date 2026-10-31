using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Sets up the pictures of the terminal desktop. It makes placeholder icons (a globe, a book, a padlock) and placeholder
    /// wallpapers if there are none, sets the import settings of every picture, and fills the theme asset. Run it again
    /// whenever pictures are added or replaced.
    ///
    /// Icons: Assets/_Project/Art/Terminal/Icons/Icon_browser.png, Icon_knowledge.png, Icon_security.png. Overwrite a file
    /// with a better picture of the same name and run the tool (the theme keeps pointing at the file).
    /// Wallpapers: every image in Assets/_Project/Art/Terminal/Wallpapers and in Assets/ThirdParty/Wallpapers (add a row to
    /// CREDITS.md for each photo). Files called placeholder_* are used only while there are no others.
    /// News photos: placeholder pictures for the keys in <see cref="WebImagePainter.Keys"/> go to Assets/_Project/Art/Web/Images
    /// if missing; every image there and in Assets/ThirdParty/WebImages is a photo of the news site, its key is the file name
    /// without the extension (lower case), and a ThirdParty photo replaces a placeholder with the same key.
    /// </summary>
    public static class DesktopArtTool
    {
        public const string IconsFolder = "Assets/_Project/Art/Terminal/Icons";
        public const string WallpapersFolder = "Assets/_Project/Art/Terminal/Wallpapers";
        public const string PhotosFolder = "Assets/ThirdParty/Wallpapers";
        public const string WebImagesFolder = "Assets/_Project/Art/Web/Images";
        public const string WebPhotosFolder = "Assets/ThirdParty/WebImages";

        // The program ids (see ITerminalApp.Id) and the file of their icon.
        private static readonly (string appId, string file)[] IconFiles =
        {
            ("browser", "Icon_browser.png"),
            ("knowledge", "Icon_knowledge.png"),
            ("password", "Icon_security.png"),
        };

        [MenuItem("Tools/Cold Shutdown/Update Desktop Art")]
        public static void UpdateDesktopArt()
        {
            EnsureIcons();
            var wallpapers = FindWallpapers();
            if (wallpapers.Count == 0)
            {
                CreatePlaceholderWallpapers();
                wallpapers = FindWallpapers();
            }

            var icons = IconFiles.Select(entry => (entry.appId, AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconsFolder}/{entry.file}"))).ToList();
            EnsureWebImages();
            var webImages = FindWebImages();

            AssignToTheme(wallpapers, icons, webImages);
            Debug.Log($"Desktop art: {icons.Count} icons, {wallpapers.Count} wallpaper(s) and {webImages.Count} news picture(s) in the theme ({string.Join(", ", wallpapers.Select(wallpaper => wallpaper.name))}).");
        }

        private static void EnsureIcons()
        {
            Directory.CreateDirectory(IconsFolder);
            var painters = new Dictionary<string, System.Func<Texture2D>>
            {
                { "Icon_browser.png", DesktopArtPainter.BrowserIcon },
                { "Icon_knowledge.png", DesktopArtPainter.KnowledgeIcon },
                { "Icon_security.png", DesktopArtPainter.SecurityIcon },
            };

            bool wroteAny = false;
            foreach (var pair in painters)
            {
                string path = $"{IconsFolder}/{pair.Key}";
                if (!File.Exists(path))
                {
                    WritePng(path, pair.Value());
                    wroteAny = true;
                }
            }
            if (wroteAny) AssetDatabase.Refresh();

            foreach (var pair in painters)
            {
                ConfigureImport($"{IconsFolder}/{pair.Key}", maxSize: 256, uncompressed: true);
            }
        }

        private static void EnsureWebImages()
        {
            Directory.CreateDirectory(WebImagesFolder);
            bool wroteAny = false;
            foreach (string key in WebImagePainter.Keys)
            {
                string path = $"{WebImagesFolder}/{key}.png";
                if (File.Exists(path)) continue;

                WritePng(path, WebImagePainter.Paint(key));
                wroteAny = true;
            }
            if (wroteAny) AssetDatabase.Refresh();
        }

        /// <summary>The news photos by key; a photo in ThirdParty/WebImages wins over a placeholder with the same key.</summary>
        private static List<(string key, Texture2D image)> FindWebImages()
        {
            var found = new Dictionary<string, Texture2D>();
            foreach (string folder in new[] { WebImagesFolder, WebPhotosFolder })
            {
                if (!Directory.Exists(folder)) continue;

                var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path, System.StringComparer.Ordinal);
                foreach (string path in paths)
                {
                    ConfigureImport(path, maxSize: 1024, uncompressed: false);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null) found[Path.GetFileNameWithoutExtension(path).ToLowerInvariant()] = texture;
                }
            }
            return found.OrderBy(pair => pair.Key, System.StringComparer.Ordinal).Select(pair => (pair.Key, pair.Value)).ToList();
        }

        private static void CreatePlaceholderWallpapers()
        {
            Directory.CreateDirectory(WallpapersFolder);
            for (int i = 0; i < DesktopArtPainter.WallpaperNames.Length; i++)
            {
                string path = $"{WallpapersFolder}/placeholder_{DesktopArtPainter.WallpaperNames[i]}.png";
                if (!File.Exists(path)) WritePng(path, DesktopArtPainter.PaintWallpaper(i));
            }
            AssetDatabase.Refresh();
        }

        /// <summary>The wallpaper pictures: photos if there are any, otherwise the placeholders.</summary>
        private static List<Texture2D> FindWallpapers()
        {
            var folders = new[] { WallpapersFolder, PhotosFolder }.Where(Directory.Exists).ToArray();
            if (folders.Length == 0) return new List<Texture2D>();

            var all = AssetDatabase.FindAssets("t:Texture2D", folders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .ToList();

            var photos = all.Where(path => !Path.GetFileName(path).StartsWith("placeholder_")).ToList();
            var chosen = photos.Count > 0 ? photos : all;

            foreach (string path in chosen)
            {
                ConfigureImport(path, maxSize: 2048, uncompressed: false);
            }
            return chosen.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).Where(texture => texture != null).ToList();
        }

        private static void ConfigureImport(string path, int maxSize, bool uncompressed)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool changed = importer.textureType != TextureImporterType.Default
                || importer.maxTextureSize != maxSize
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.alphaIsTransparency != uncompressed
                || importer.textureCompression != (uncompressed ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ);
            if (!changed) return;

            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = maxSize;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = uncompressed;
            importer.mipmapEnabled = uncompressed;
            importer.textureCompression = uncompressed ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void AssignToTheme(List<Texture2D> wallpapers, List<(string appId, Texture2D icon)> icons, List<(string key, Texture2D image)> webImages)
        {
            var theme = AssetDatabase.LoadAssetAtPath<DesktopTheme>(TerminalTool.ThemePath);
            if (theme == null)
            {
                Debug.LogError($"{TerminalTool.ThemePath} not found. Run Tools/Cold Shutdown/Create Terminal Prefab first.");
                return;
            }

            var serialized = new SerializedObject(theme);
            var wallpaperList = serialized.FindProperty("wallpapers");
            wallpaperList.arraySize = wallpapers.Count;
            for (int i = 0; i < wallpapers.Count; i++)
            {
                wallpaperList.GetArrayElementAtIndex(i).objectReferenceValue = wallpapers[i];
            }

            var iconList = serialized.FindProperty("icons");
            iconList.arraySize = icons.Count;
            for (int i = 0; i < icons.Count; i++)
            {
                var entry = iconList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("appId").stringValue = icons[i].appId;
                entry.FindPropertyRelative("icon").objectReferenceValue = icons[i].icon;
            }
            var imageList = serialized.FindProperty("webImages");
            imageList.arraySize = webImages.Count;
            for (int i = 0; i < webImages.Count; i++)
            {
                var entry = imageList.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("key").stringValue = webImages[i].key;
                entry.FindPropertyRelative("image").objectReferenceValue = webImages[i].image;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }

        private static void WritePng(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
    }
}
