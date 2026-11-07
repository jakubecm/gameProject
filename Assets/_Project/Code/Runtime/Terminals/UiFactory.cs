using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>Small helpers that make the uGUI objects of the terminal desktop, so the programs stay short.</summary>
    internal static class UiFactory
    {
        private static Font font;

        /// <summary>The built-in font; it has the Czech letters.</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>Destroys an object: at the end of the frame while playing (it may be the button whose click is being handled), at once in the Editor.</summary>
        public static void Dispose(GameObject gameObject)
        {
            if (gameObject == null) return;
            gameObject.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(gameObject);
            else UnityEngine.Object.DestroyImmediate(gameObject);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image Image(string name, Transform parent, Color color, bool raycast = true)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Text Text(string name, Transform parent, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, Font font = null)
        {
            var label = Rect(name, parent).gameObject.AddComponent<Text>();
            label.font = font != null ? font : Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(string name, Transform parent, string caption, DesktopTheme theme, int fontSize,
            Color? normal = null, Color? textColor = null, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var image = Image(name, parent, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var baseColor = normal ?? theme.button;
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, theme.buttonHover, 0.85f);
            colors.pressedColor = Color.Lerp(baseColor, theme.buttonPressed, 0.9f);
            colors.selectedColor = baseColor;
            colors.disabledColor = theme.buttonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            var label = Text("Caption", image.transform, caption, fontSize, textColor ?? theme.bodyText, alignment);
            Fill(label.rectTransform, 10f, 0f, 10f, 0f);
            return button;
        }

        /// <summary>Stretches a rect over its parent, with a margin on each side.</summary>
        public static void Fill(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Puts a rect on a corner or edge of its parent: the anchor and pivot are the same point (0 to 1 on each axis).</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    }
}
