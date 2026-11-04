using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Editor;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Tests.EditMode.Terminals
{
    /// <summary>The pictures of the desktop: wallpapers (cut to fit), program icons, and the theme that holds them.</summary>
    public class DesktopArtTests
    {
        private const string ThemePath = "Assets/_Project/Data/DesktopTheme_Office.asset";
        private const string IconsFolder = "Assets/_Project/Art/Terminal/Icons";

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in created)
            {
                if (item != null) Object.DestroyImmediate(item);
            }
            created.Clear();
        }

        // ---------------------------------------------------------------- the theme

        [Test]
        public void TheWallpaperForAKey_IsStable_AndWrapsAroundTheList()
        {
            var theme = Theme(wallpapers: new[] { Picture(10, 10), Picture(10, 10), Picture(10, 10) });

            Assert.AreSame(theme.wallpapers[0], theme.WallpaperFor(0));
            Assert.AreSame(theme.wallpapers[2], theme.WallpaperFor(2));
            Assert.AreSame(theme.wallpapers[0], theme.WallpaperFor(3), "wraps around");
            Assert.AreSame(theme.wallpapers[1], theme.WallpaperFor(-2), "negative keys work too");
            Assert.AreSame(theme.WallpaperFor(12345), theme.WallpaperFor(12345), "the same key, the same picture");
        }

        [Test]
        public void TheWallpaperForAKey_SkipsEmptySlots_AndIsNullWithoutPictures()
        {
            var picture = Picture(10, 10);
            Assert.AreSame(picture, Theme(wallpapers: new Texture2D[] { null, picture }).WallpaperFor(0), "an empty slot is skipped");
            Assert.IsNull(Theme(wallpapers: new Texture2D[0]).WallpaperFor(7));
            Assert.IsNull(Theme(wallpapers: null).WallpaperFor(7));
            Assert.IsNull(Theme(wallpapers: new Texture2D[] { null }).WallpaperFor(7));
        }

        [Test]
        public void TheIconOfAProgram_IsFoundById_IgnoringCase()
        {
            var icon = Picture(8, 8);
            var theme = Theme(icons: new[] { new AppIcon { appId = "browser", icon = icon }, new AppIcon { appId = "empty", icon = null } });

            Assert.AreSame(icon, theme.IconFor("browser"));
            Assert.AreSame(icon, theme.IconFor("BROWSER"));
            Assert.IsNull(theme.IconFor("empty"), "an entry without a picture is no icon");
            Assert.IsNull(theme.IconFor("unknown"));
            Assert.IsNull(theme.IconFor(null));
        }

        // ---------------------------------------------------------------- fitting a picture to the screen

        [Test]
        public void APictureThatIsWiderThanTheScreen_IsCroppedAtTheSides()
        {
            var rect = Desktop.CoverRect(2f, 16f / 9f);

            Assert.AreEqual(1f, rect.height, 1e-5f);
            Assert.AreEqual((16f / 9f) / 2f, rect.width, 1e-5f);
            Assert.AreEqual((1f - rect.width) / 2f, rect.x, 1e-5f, "the middle of the picture");
        }

        [Test]
        public void APictureThatIsTallerThanTheScreen_IsCroppedAtTheTopAndBottom()
        {
            var rect = Desktop.CoverRect(1f, 16f / 9f);

            Assert.AreEqual(1f, rect.width, 1e-5f);
            Assert.AreEqual(9f / 16f, rect.height, 1e-5f);
            Assert.AreEqual((1f - rect.height) / 2f, rect.y, 1e-5f);
        }

        [Test]
        public void APictureOfTheScreensShape_IsShownWhole()
        {
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), Desktop.CoverRect(16f / 9f, 16f / 9f));
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), Desktop.CoverRect(0f, 16f / 9f), "a broken picture does not break the screen");
        }

        [Test]
        public void ThePictureOnTheScreen_IsNotStretched()
        {
            var screen = Track(new GameObject("Screen", typeof(RectTransform)));
            var picture = Picture(400, 300);
            var desktop = new Desktop((RectTransform)screen.transform, Theme(), true, 0f, picture);

            var raw = desktop.SystemRoot.Find("Wallpaper/Picture").GetComponent<RawImage>();
            Assert.AreSame(picture, raw.texture);
            // What is shown has the screen's shape: width / height of the cut-out picture equals 16 / 9.
            float shownAspect = raw.uvRect.width * picture.width / (raw.uvRect.height * picture.height);
            Assert.AreEqual(16f / 9f, shownAspect, 1e-3f);
            Assert.IsNotNull(desktop.SystemRoot.Find("Wallpaper/Tint"), "a veil keeps the icons readable");
            Assert.IsNull(desktop.SystemRoot.Find("Wallpaper/WallpaperBand"));
        }

        [Test]
        public void WithoutAPicture_TheDesktopHasItsPlainColour()
        {
            var screen = Track(new GameObject("Screen", typeof(RectTransform)));
            var desktop = new Desktop((RectTransform)screen.transform, Theme());

            Assert.IsNull(desktop.SystemRoot.Find("Wallpaper/Picture"));
            Assert.IsNotNull(desktop.SystemRoot.Find("Wallpaper/WallpaperBand"));
        }

        // ---------------------------------------------------------------- icons on the desktop

        [Test]
        public void AProgramWithAnIcon_ShowsItOnTheDesktop_TheOthersKeepTheLetterTile()
        {
            var screen = Track(new GameObject("Screen", typeof(RectTransform)));
            var icon = Picture(64, 64);
            var theme = Theme(icons: new[] { new AppIcon { appId = "browser", icon = icon } });
            var desktop = new Desktop((RectTransform)screen.transform, theme);
            var browser = new BrowserApp(WebLibrary.Parse("{\"home\":\"start\",\"pages\":[{\"id\":\"start\",\"title\":\"Start\",\"url\":\"start.local\",\"kind\":\"portal\"}]}"));
            var knowledge = new LoreApp(null);
            desktop.AddApp(browser);
            desktop.AddApp(knowledge);

            var picture = desktop.SystemRoot.Find("Icons/Icon_browser/Tile/Icon").GetComponent<RawImage>();
            Assert.AreSame(icon, picture.texture);
            Assert.IsFalse(picture.raycastTarget, "the click goes to the button");
            Assert.AreEqual("", desktop.SystemRoot.Find("Icons/Icon_browser/Tile/Caption").GetComponent<Text>().text, "no letter over the picture");

            Assert.IsNull(desktop.SystemRoot.Find("Icons/Icon_knowledge/Tile/Icon"));
            Assert.AreEqual("K", desktop.SystemRoot.Find("Icons/Icon_knowledge/Tile/Caption").GetComponent<Text>().text);

            desktop.SystemRoot.Find("Icons/Icon_browser/Tile").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(desktop.IsOpen(browser), "a picture icon opens its program like a tile does");
        }

        [Test]
        public void TheIcon_AlsoShowsInTheWindowTitleAndOnTheTaskbar()
        {
            var screen = Track(new GameObject("Screen", typeof(RectTransform)));
            var icon = Picture(64, 64);
            var desktop = new Desktop((RectTransform)screen.transform, Theme(icons: new[] { new AppIcon { appId = "knowledge", icon = icon } }));
            var knowledge = new LoreApp(null);
            desktop.AddApp(knowledge);

            var window = desktop.Open(knowledge);

            Assert.AreSame(icon, window.Root.Find("TitleBar/Icon").GetComponent<RawImage>().texture);
            Assert.AreSame(icon, desktop.SystemRoot.Find("Taskbar/Task_knowledge/Icon").GetComponent<RawImage>().texture);
            desktop.CloseAll();
        }

        // ---------------------------------------------------------------- the drawn placeholders

        [Test]
        public void TheShapes_KnowTheirInsideFromTheirOutside()
        {
            Assert.Less(DesktopArtPainter.Circle(new Vector2(1f, 0f), Vector2.zero, 5f), 0f);
            Assert.AreEqual(0f, DesktopArtPainter.Circle(new Vector2(5f, 0f), Vector2.zero, 5f), 1e-4f);
            Assert.Greater(DesktopArtPainter.Circle(new Vector2(9f, 0f), Vector2.zero, 5f), 0f);

            Assert.Less(DesktopArtPainter.RoundedRect(Vector2.zero, Vector2.zero, new Vector2(10f, 6f), 3f), 0f);
            Assert.Greater(DesktopArtPainter.RoundedRect(new Vector2(11f, 0f), Vector2.zero, new Vector2(10f, 6f), 3f), 0f);
            Assert.Greater(DesktopArtPainter.RoundedRect(new Vector2(10f, 6f), Vector2.zero, new Vector2(10f, 6f), 3f), 0f, "the corner is rounded off");

            Assert.AreEqual(-2f, DesktopArtPainter.Segment(new Vector2(5f, 0f), Vector2.zero, new Vector2(10f, 0f), 4f), 1e-4f);
            Assert.Greater(DesktopArtPainter.Segment(new Vector2(5f, 5f), Vector2.zero, new Vector2(10f, 0f), 4f), 0f);

            var square = new[] { new Vector2(-4f, -4f), new Vector2(4f, -4f), new Vector2(4f, 4f), new Vector2(-4f, 4f) };
            Assert.Less(DesktopArtPainter.Polygon(Vector2.zero, square), 0f);
            Assert.AreEqual(1f, DesktopArtPainter.Polygon(new Vector2(5f, 0f), square), 1e-4f);
        }

        [Test]
        public void TheEdgeOfAShape_IsOnePixelSoft()
        {
            Assert.AreEqual(1f, DesktopArtPainter.Coverage(-3f));
            Assert.AreEqual(0f, DesktopArtPainter.Coverage(3f));
            Assert.AreEqual(0.5f, DesktopArtPainter.Coverage(0f), 1e-5f);
        }

        [Test]
        public void EveryDrawnIcon_IsARoundedBadgeWithAWhiteSymbol()
        {
            // (the icon, a point inside its white symbol as an offset from the middle, in pixels)
            var cases = new[]
            {
                ("browser", DesktopArtPainter.BrowserIcon(), new Vector2(70f, 0f)),
                ("knowledge", DesktopArtPainter.KnowledgeIcon(), new Vector2(-56f, 14f)),
                ("security", DesktopArtPainter.SecurityIcon(), new Vector2(40f, -22f)),
            };
            foreach (var (name, texture, symbolPoint) in cases)
            {
                created.Add(texture);
                Assert.AreEqual(256, texture.width, name);
                Assert.AreEqual(256, texture.height, name);

                Assert.AreEqual(0f, texture.GetPixel(2, 2).a, 1e-3f, $"{name}: the corner is transparent");
                var symbol = texture.GetPixel(128 + (int)symbolPoint.x, 128 + (int)symbolPoint.y);
                Assert.Greater(symbol.r + symbol.g + symbol.b, 2.8f, $"{name}: the symbol is white");
                Assert.AreEqual(1f, symbol.a, 1e-3f);

                var badge = texture.GetPixel(128, 128 + 110);
                Assert.AreEqual(1f, badge.a, 1e-3f, $"{name}: the badge is solid");
                Assert.Less(badge.r + badge.g + badge.b, 2.4f, $"{name}: the badge has its colour, not white");
            }
        }

        [Test]
        public void TheIcons_HaveTheirOwnColours()
        {
            var colours = new[] { DesktopArtPainter.BrowserIcon(), DesktopArtPainter.KnowledgeIcon(), DesktopArtPainter.SecurityIcon() }
                .Select(texture => { created.Add(texture); return texture.GetPixel(128, 128 + 110); })
                .ToList();

            Assert.Greater(colours[0].b, colours[0].r, "the browser is blue");
            Assert.Greater(colours[1].r, colours[1].b, "the knowledge base is amber");
            Assert.Greater(colours[2].r, colours[2].g * 2f, "the lock is red");
        }

        [Test]
        public void TheWallpaperPlaceholders_AreFourDifferentOpaquePictures()
        {
            var looks = new List<Color>();
            for (int variant = 0; variant < DesktopArtPainter.WallpaperNames.Length; variant++)
            {
                var texture = DesktopArtPainter.PaintWallpaper(variant, 64, 36);
                created.Add(texture);
                Assert.AreEqual(1f, texture.GetPixel(10, 10).a, 1e-3f);
                Assert.AreNotEqual(texture.GetPixel(32, 2), texture.GetPixel(32, 34), "a gradient from the bottom to the top");
                looks.Add(texture.GetPixel(32, 18));
            }
            Assert.AreEqual(looks.Count, looks.Distinct().Count(), "every variant looks different");
        }

        // ---------------------------------------------------------------- what the tool made

        [Test]
        public void TheThemeAsset_HoldsAnIconForEveryProgram_AndWallpapers()
        {
            var theme = AssetDatabase.LoadAssetAtPath<DesktopTheme>(ThemePath);
            Assert.IsNotNull(theme, $"{ThemePath} is missing. Run Tools/Cold Shutdown/Create Terminal Prefab.");

            foreach (string app in new[] { "browser", "knowledge", "password" })
            {
                Assert.IsNotNull(theme.IconFor(app), $"no icon for {app}. Run Tools/Cold Shutdown/Update Desktop Art.");
            }
            Assert.GreaterOrEqual(theme.wallpapers.Length, 1, "no wallpaper. Run Tools/Cold Shutdown/Update Desktop Art.");
            Assert.IsTrue(theme.wallpapers.All(wallpaper => wallpaper != null));
        }

        [Test]
        public void TheIconFiles_AreSmallAndCrisp()
        {
            foreach (string file in new[] { "Icon_browser.png", "Icon_knowledge.png", "Icon_security.png" })
            {
                var importer = AssetImporter.GetAtPath($"{IconsFolder}/{file}") as TextureImporter;
                Assert.IsNotNull(importer, file);
                Assert.LessOrEqual(importer.maxTextureSize, 512, file);
                Assert.IsTrue(importer.alphaIsTransparency, $"{file}: the rounded corners are transparent");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, $"{file}: no compression artefacts on a small icon");
            }
        }

        [Test]
        public void EveryWallpaper_IsASensibleSize()
        {
            var theme = AssetDatabase.LoadAssetAtPath<DesktopTheme>(ThemePath);
            Assume.That(theme, Is.Not.Null);
            foreach (var wallpaper in theme.wallpapers)
            {
                string path = AssetDatabase.GetAssetPath(wallpaper);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);
                Assert.LessOrEqual(importer.maxTextureSize, 2048, $"{path}: a wallpaper does not need more than 2048 pixels");
                Assert.IsTrue(File.Exists(path));
            }
        }

        // ---------------------------------------------------------------- news photos

        [Test]
        public void TheWebImageForAKey_IsFoundIgnoringCase_AndNullWhenThereIsNone()
        {
            var photo = Picture(16, 9);
            var theme = DesktopTheme.CreateDefault();
            created.Add(theme);
            theme.webImages = new[] { new WebImage { key = "ghost", image = photo }, new WebImage { key = "empty", image = null } };

            Assert.AreSame(photo, theme.WebImageFor("Ghost"));
            Assert.IsNull(theme.WebImageFor("empty"));
            Assert.IsNull(theme.WebImageFor("unknown"));
            Assert.IsNull(theme.WebImageFor(null));
            Assert.IsNull(theme.WebImageFor(""));
        }

        [Test]
        public void ThePlaceholderPhotos_AreDrawn16To9_AndAllDifferent()
        {
            var seen = new HashSet<string>();
            foreach (string key in WebImagePainter.Keys)
            {
                var texture = WebImagePainter.Paint(key);
                created.Add(texture);

                Assert.AreEqual(WebImagePainter.Width, texture.width, key);
                Assert.AreEqual(WebImagePainter.Height, texture.height, key);
                var pixels = texture.GetPixels32();
                int bright = pixels.Count(pixel => pixel.r + pixel.g + pixel.b > 150);
                int dark = pixels.Count(pixel => pixel.r + pixel.g + pixel.b < 400);
                Assert.Greater(bright, 1000, $"{key} is not all black");
                Assert.Greater(dark, 1000, $"{key} is not all bright");
                Assert.IsTrue(pixels.All(pixel => pixel.a == 255), key);
                long checksum = pixels.Aggregate(17L, (hash, pixel) => hash * 31 + pixel.r * 3 + pixel.g * 5 + pixel.b * 7);
                Assert.IsTrue(seen.Add(checksum.ToString()), $"{key} looks like another picture");
            }
        }

        [Test]
        public void ThePlaceholderPhotos_AreTheSameEveryTime()
        {
            var first = WebImagePainter.Paint("city");
            var second = WebImagePainter.Paint("city");
            created.Add(first);
            created.Add(second);
            CollectionAssert.AreEqual(first.GetPixels32(), second.GetPixels32());
        }

        [Test]
        public void TheThemeAsset_HoldsAPhotoForEveryNewsPicture()
        {
            var theme = AssetDatabase.LoadAssetAtPath<DesktopTheme>(ThemePath);
            Assert.IsNotNull(theme);
            foreach (string key in WebImagePainter.Keys)
            {
                Assert.IsNotNull(theme.WebImageFor(key), $"{key}: run Tools/Cold Shutdown/Update Desktop Art");
            }
            Assert.IsNotNull(theme.HeadlineFont, "there is always a font for headlines");
        }

        [Test]
        public void ANewsStory_WithAPhoto_ShowsItCutToFit()
        {
            var photo = Picture(48, 16);
            var theme = DesktopTheme.CreateDefault();
            created.Add(theme);
            theme.webImages = new[] { new WebImage { key = "ghost", image = photo } };

            var screen = Track(new GameObject("Screen", typeof(RectTransform)));
            var desktop = new Desktop((RectTransform)screen.transform, theme, true);
            var browser = new BrowserApp(WebLibrary.Parse(File.ReadAllText("Assets/_Project/Data/Web/web_pages.json")));
            desktop.AddApp(browser);
            desktop.Open(browser);
            browser.Navigate("news_front");

            var page = desktop.WindowOf(browser).Content.Find("Scroll/Viewport/Page");
            var lead = page.Find("Front/Card_news_ghost/Photo").GetComponent<RawImage>();
            Assert.AreSame(photo, lead.texture);
            Assert.Less(lead.uvRect.width, 1f, "a 3:1 photo is cut at the sides to fill the lead slot");
            Assert.AreEqual(1f, lead.uvRect.height, 0.001f);
            Assert.IsNotNull(page.Find("Front/Side/Card_news_law/PhotoPlaceholder"), "a story without a photo gets a block");

            browser.Navigate("news_ghost");
            Assert.AreSame(photo, page.Find("Article/Photo").GetComponent<RawImage>().texture);
            desktop.CloseAll();
        }

        private DesktopTheme Theme(Texture2D[] wallpapers = null, AppIcon[] icons = null)
        {
            var theme = DesktopTheme.CreateDefault();
            theme.wallpapers = wallpapers;
            theme.icons = icons;
            created.Add(theme);
            return theme;
        }

        private Texture2D Picture(int width, int height)
        {
            var texture = new Texture2D(width, height);
            created.Add(texture);
            return texture;
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }
    }
}
