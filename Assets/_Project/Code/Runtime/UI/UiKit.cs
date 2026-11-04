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

        /// <summary>The gap (between ink edges) under the title of every screen, to its first item.</summary>
        public const float TitleGap = 56f;

        /// <summary>The gap (between ink edges) between the items of a menu, the same on every screen.</summary>
        public const float ItemGap = 40f;

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

        /// <summary>
        /// The ink of a label: the rectangle (in the canvas's reference pixels, relative to the middle of the label's rect) that its glyphs
        /// really cover. A text box is not the text: the line box has room above and below and glyphs have side bearings, so only the ink can be
        /// centred exactly. Read from the generator the label is drawn with, so call it once the canvas has been updated (the screens do, in
        /// <see cref="UiScreen.Arrange"/>); the label's rect must have its final size.
        /// </summary>
        public static Rect Ink(Text label)
        {
            var generator = label.cachedTextGenerator;
            float unit = 1f / label.pixelsPerUnit;
            if (generator.vertexCount <= 0)
            {
                var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                generator = new TextGenerator();
                generator.Populate(label.text, settings);
                unit = 1f / settings.scaleFactor;
            }
            var verts = generator.verts;
            int count = verts.Count;
            if (count <= 0) return new Rect(0f, 0f, 0f, 0f);

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                var p = verts[i].position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            return Rect.MinMaxRect(minX * unit, minY * unit, maxX * unit, maxY * unit);
        }

        /// <summary>Moves a stretched label so that its ink is centred in its rect (horizontally only when asked); returns the ink, now centred.</summary>
        public static Rect CentreInk(Text label, bool horizontal = true)
        {
            var ink = Ink(label);
            var shift = new Vector2(horizontal ? -ink.center.x : 0f, -ink.center.y);
            label.rectTransform.offsetMin += shift;
            label.rectTransform.offsetMax += shift;
            return Rect.MinMaxRect(-ink.width / 2f, -ink.height / 2f, ink.width / 2f, ink.height / 2f);
        }

        /// <summary>The ink of a <see cref="MenuButton"/>'s text, centred in the button (see <see cref="CentreInk"/>).</summary>
        public static Rect ButtonInk(Button button, bool horizontal = true) => CentreInk(button.GetComponentInChildren<Text>(), horizontal);

        /// <summary>One row of a <see cref="Stack"/>: its rect, the ink of its content relative to the middle of the rect, and the space above it.</summary>
        public readonly struct StackRow
        {
            public readonly RectTransform Target;
            public readonly Rect Ink;
            public readonly float GapBefore;

            public StackRow(RectTransform rect, Rect ink, float gapBefore = 0f)
            {
                Target = rect;
                Ink = ink;
                GapBefore = gapBefore;
            }
        }

        /// <summary>
        /// Places rows one under the other on the middle of the screen, measured between ink edges: every row is centred horizontally by its
        /// ink, and the whole block (its ink, top of the first row to bottom of the last) is centred vertically on <paramref name="middleY"/>.
        /// </summary>
        public static void Stack(float middleY, params StackRow[] rows)
        {
            float height = 0f;
            for (int i = 0; i < rows.Length; i++) height += rows[i].Ink.height + (i > 0 ? rows[i].GapBefore : 0f);

            float y = middleY + height / 2f;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (i > 0) y -= row.GapBefore;
                float middle = y - row.Ink.height / 2f;
                y -= row.Ink.height;
                var centre = new Vector2(0.5f, 0.5f);
                row.Target.anchorMin = centre;
                row.Target.anchorMax = centre;
                row.Target.pivot = centre;
                row.Target.anchoredPosition = new Vector2(-row.Ink.center.x, middle - row.Ink.center.y);
            }
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
            rect.sizeDelta = new Vector2(width, height);

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
            track.rectTransform.offsetMin = new Vector2(0f, -3f);
            track.rectTransform.offsetMax = new Vector2(0f, 3f);

            var fillArea = NewRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(0f, -3f);
            fillArea.offsetMax = new Vector2(0f, 3f);
            var fill = Box(fillArea, "Fill", Accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = NewRect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(7f, 0f);
            handleArea.offsetMax = new Vector2(-7f, 0f);
            var handle = Box(handleArea, "Handle", Bright);
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(14f, 22f);

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

            // White, so the box takes exactly the colours of the toggle's colour block.
            var background = Box(root, "Background", Color.white);
            background.raycastTarget = true;
            Place(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            var outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = Accent * 0.8f;
            outline.effectDistance = new Vector2(2f, -2f);
            var check = Box(background.transform, "Check", Accent);
            Place(check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = isOn;
            toggle.colors = ToggleColors(isOn);
            // Checked, the box keeps a stronger blue (and the square inside shows); unchecked, it keeps a faint blue. Hover only brightens it.
            toggle.onValueChanged.AddListener(on => toggle.colors = ToggleColors(on));
            return toggle;
        }

        private static ColorBlock ToggleColors(bool isOn)
        {
            var normal = isOn ? new Color(0.14f, 0.40f, 0.54f) : new Color(0.09f, 0.19f, 0.27f);
            var hover = isOn ? new Color(0.20f, 0.52f, 0.68f) : new Color(0.14f, 0.30f, 0.41f);
            var colors = SelectableColors(normal, hover);
            // The clicked box stays in its own colour; only the pointer over it brightens it.
            colors.selectedColor = normal;
            colors.pressedColor = Accent * 0.7f;
            return colors;
        }
    }
}
