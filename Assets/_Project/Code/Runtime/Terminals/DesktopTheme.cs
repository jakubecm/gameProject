using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>The icon picture of one program on the desktop, by the program's id ("browser", "knowledge", "password").</summary>
    [Serializable]
    public class AppIcon
    {
        public string appId;
        public Texture2D icon;
    }

    /// <summary>A picture of the in-game web (a news photo), by its key: the name of the file without the extension.</summary>
    [Serializable]
    public class WebImage
    {
        public string key;
        public Texture2D image;
    }

    /// <summary>
    /// Colours, pictures and text sizes of the terminal desktop (a modern office look). One asset, so the look can be
    /// changed in the Inspector without touching code; the terminal makes a default one if none is assigned.
    /// The pictures (wallpapers, icons) are filled in by Tools/Cold Shutdown/Update Desktop Art.
    /// </summary>
    [CreateAssetMenu(fileName = "DesktopTheme", menuName = "Cold Shutdown/Desktop Theme")]
    public class DesktopTheme : ScriptableObject
    {
        [Header("Desktop")]
        [Tooltip("The colour of the desktop when there is no wallpaper picture.")]
        public Color wallpaper = new Color(0.09f, 0.33f, 0.58f);
        public Color wallpaperBand = new Color(0.13f, 0.44f, 0.72f);
        public Color iconLabel = Color.white;

        [Header("Wallpaper pictures")]
        [Tooltip("Every computer picks one of these when the game starts. Empty: the plain colour above.")]
        public Texture2D[] wallpapers;
        [Tooltip("A veil over the picture, so the icons and the taskbar stay readable on bright photos.")]
        public Color wallpaperTint = new Color(0f, 0f, 0f, 0.22f);

        [Header("Program icons")]
        [Tooltip("A picture per program. A program without one gets a coloured tile with a letter.")]
        public AppIcon[] icons;

        [Header("In-game web")]
        [Tooltip("The photos of the news site: an article names one by its key. Filled in by Update Desktop Art from Art/Web/Images and ThirdParty/WebImages.")]
        public WebImage[] webImages;
        [Tooltip("The font of headlines on the news site. Empty: a serif font of the computer (Georgia, Times New Roman), or the normal font.")]
        public Font headlineFont;

        [Header("Taskbar")]
        public Color taskbar = new Color(0.07f, 0.09f, 0.13f, 0.97f);
        public Color taskbarText = Color.white;
        public Color accent = new Color(0.20f, 0.52f, 0.88f);

        [Header("Windows")]
        public Color windowBackground = new Color(0.96f, 0.97f, 0.98f);
        public Color titleBar = new Color(0.16f, 0.19f, 0.26f);
        public Color titleText = Color.white;
        public Color closeButton = new Color(0.78f, 0.22f, 0.22f);
        public Color panel = new Color(0.90f, 0.92f, 0.95f);

        [Header("Text")]
        public Color bodyText = new Color(0.12f, 0.14f, 0.18f);
        public Color mutedText = new Color(0.40f, 0.44f, 0.52f);
        public Color success = new Color(0.10f, 0.52f, 0.26f);
        public Color danger = new Color(0.76f, 0.16f, 0.16f);

        [Header("Buttons")]
        public Color button = new Color(0.84f, 0.88f, 0.94f);
        public Color buttonHover = new Color(0.72f, 0.81f, 0.94f);
        public Color buttonPressed = new Color(0.58f, 0.71f, 0.90f);
        public Color buttonDisabled = new Color(0.82f, 0.83f, 0.85f);

        [Header("Text sizes")]
        public int titleSize = 20;
        public int headingSize = 24;
        public int bodySize = 18;
        public int smallSize = 14;

        public static DesktopTheme CreateDefault() => CreateInstance<DesktopTheme>();

        /// <summary>
        /// The wallpaper for a key: the same key always gives the same picture, any whole number works (it wraps around the
        /// list). Null when the theme has no wallpaper pictures.
        /// </summary>
        public Texture2D WallpaperFor(int key)
        {
            if (wallpapers == null || wallpapers.Length == 0) return null;

            for (int step = 0; step < wallpapers.Length; step++)
            {
                int index = (((key + step) % wallpapers.Length) + wallpapers.Length) % wallpapers.Length;
                if (wallpapers[index] != null) return wallpapers[index];
            }
            return null;
        }

        /// <summary>The photo with a key (any case), or null if there is none. The news site shows a placeholder block then.</summary>
        public Texture2D WebImageFor(string key)
        {
            if (webImages == null || string.IsNullOrEmpty(key)) return null;

            foreach (var entry in webImages)
            {
                if (entry != null && entry.image != null && string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.image;
                }
            }
            return null;
        }

        private static Font serifFont;

        /// <summary>The font for headlines: the one set in the theme, else a serif font installed on the computer, else the normal font.</summary>
        public Font HeadlineFont
        {
            get
            {
                if (headlineFont != null) return headlineFont;
                if (serifFont == null)
                {
                    serifFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Cambria", "Palatino Linotype", "Serif" }, 32);
                }
                return serifFont != null ? serifFont : UiFactory.Font;
            }
        }

        /// <summary>The icon picture of a program, or null if it has none.</summary>
        public Texture2D IconFor(string appId)
        {
            if (icons == null || appId == null) return null;

            foreach (var entry in icons)
            {
                if (entry != null && entry.icon != null && string.Equals(entry.appId, appId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.icon;
                }
            }
            return null;
        }
    }
}
