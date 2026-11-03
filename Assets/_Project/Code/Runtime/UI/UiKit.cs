using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Builders for the menu screens: every screen is made from code at runtime (no prefabs to wire), in one look - a dark terminal with
    /// cyan and amber accents. Sizes are in pixels of a 1920 x 1080 reference canvas.
    /// </summary>
    internal static class UiKit
    {
        public static readonly Color Accent = new Color(0.36f, 0.86f, 0.96f);
        public static readonly Color Amber = new Color(1f, 0.72f, 0.22f);
        public static readonly Color Bright = new Color(0.9f, 0.95f, 0.98f);
        public static readonly Color Dim = new Color(0.52f, 0.62f, 0.67f);
        public static readonly Color PanelColor = new Color(0.025f, 0.04f, 0.055f, 0.94f);
        public static readonly Color Field = new Color(0.08f, 0.12f, 0.15f, 1f);

        private static Font mono;

        /// <summary>A monospaced system font (a terminal), or Unity's built-in font where none is installed.</summary>
        public static Font Mono
        {
            get
            {
                if (mono != null) return mono;
                mono = Font.CreateDynamicFontFromOSFont(new[] { "Menlo", "Consolas", "Courier New", "Courier" }, 32);
                if (mono == null) mono = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return mono;
            }
        }

        public static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        public static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Anchors a rect to one point of its parent (0..1 on both axes) and gives it a size and an offset from that point.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>A full-width row of a <see cref="Column"/> with the given height; what goes in it is anchored to its middle.</summary>
        public static RectTransform Slot(Transform column, string name, float height)
        {
            var slot = NewRect(name, column);
            Size(slot, -1f, height, 1f);
            return slot;
        }

        public static Image Box(Transform parent, string name, Color color)
        {
            var image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal)
        {
            var label = NewRect(name, parent).gameObject.AddComponent<Text>();
            label.font = Mono;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static LayoutElement Size(Component target, float width = -1f, float height = -1f, float flexibleWidth = -1f)
        {
            var element = target.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f) element.preferredWidth = width;
            if (height >= 0f) element.preferredHeight = height;
            if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
            return element;
        }

        /// <summary>A vertical stack. With <paramref name="fullWidth"/> every child is as wide as the stack, so text and buttons in it are centred by their own alignment.</summary>
        public static VerticalLayoutGroup Column(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.UpperLeft, bool fullWidth = false)
        {
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = fullWidth;
            group.childForceExpandHeight = false;
            return group;
        }

        public static HorizontalLayoutGroup Row(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        /// <summary>A plain text button: the label turns cyan while the button is selected or hovered.</summary>
        public static Button MenuButton(Transform parent, string label, UnityAction onClick, int fontSize = 44, float height = 68f, float width = 520f,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rect = NewRect(label + " Button", parent);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            Size(rect, width, height);

            var text = Label(rect, "Text", label, fontSize, Bright, align, FontStyle.Bold);
            Stretch(text.rectTransform);
            rect.gameObject.AddComponent<MenuButtonVisual>().Init(text);
            return button;
        }

        public static ColorBlock SelectableColors(Color normal, Color highlighted)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = Accent;
            colors.disabledColor = normal * 0.5f;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        public static Slider NewSlider(Transform parent, float min, float max, float value, bool wholeNumbers = false)
        {
            var root = NewRect("Slider", parent);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var track = Box(root, "Track", Field);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.offsetMin = new Vector2(0f, -5f);
            track.rectTransform.offsetMax = new Vector2(0f, 5f);

            var fillArea = NewRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(0f, -5f);
            fillArea.offsetMax = new Vector2(0f, 5f);
            var fill = Box(fillArea, "Fill", Accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = NewRect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            var handle = Box(handleArea, "Handle", Bright);
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(18f, 34f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = value;
            slider.colors = SelectableColors(Bright, Accent);
            return slider;
        }

        public static Toggle NewToggle(Transform parent, bool isOn)
        {
            var root = NewRect("Toggle", parent);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var background = Box(root, "Background", Field);
            background.raycastTarget = true;
            Place(background.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            var outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = Accent * 0.8f;
            outline.effectDistance = new Vector2(2f, -2f);
            var check = Box(background.transform, "Check", Accent);
            Place(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = isOn;
            toggle.colors = SelectableColors(Field, Accent * 0.45f);
            return toggle;
        }
    }
}
