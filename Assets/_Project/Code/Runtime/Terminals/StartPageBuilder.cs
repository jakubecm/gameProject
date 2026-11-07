using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Draws the browser's start page (<c>layout</c> "portal"): a dark page with a heading and a row of tiles, one for every
    /// link of the page. A tile shows a coloured badge (the link's <c>badge</c> letters and <c>color</c>), its label and a
    /// short note. The tiles are buttons named <c>Link_&lt;target&gt;</c> inside <c>Tiles</c>.
    /// </summary>
    internal sealed class StartPageBuilder
    {
        public static readonly Color Background = new Color(0.07f, 0.10f, 0.17f);

        private const float TileWidth = 300f;
        private const float TileHeight = 214f;

        private static readonly Color Soft = new Color(0.70f, 0.77f, 0.88f);

        private readonly DesktopTheme theme;
        private readonly System.Action<string> navigate;

        public StartPageBuilder(DesktopTheme theme, System.Action<string> navigate)
        {
            this.theme = theme;
            this.navigate = navigate;
        }

        public void Build(RectTransform page, WebPage target)
        {
            var title = UiFactory.Text("Heading", page, target.title, 48, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Size(title.rectTransform, 1000f, 76f);

            var line = UiFactory.Image("Accent", page, theme.accent, false);
            Size(line.rectTransform, 84f, 4f);

            if (!string.IsNullOrEmpty(target.text))
            {
                var subtitle = UiFactory.Text("Subtitle", page, target.text, 21, Soft, TextAnchor.MiddleCenter);
                Size(subtitle.rectTransform, 1000f, 52f);
            }

            var gap = UiFactory.Rect("Gap", page);
            Size(gap, 10f, 30f);

            var tiles = UiFactory.Rect("Tiles", page);
            var row = tiles.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 28f;
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            Size(tiles, -1f, -1f);

            foreach (var link in target.links ?? new WebLink[0])
            {
                Tile(tiles, link);
            }

            var gapBelow = UiFactory.Rect("GapBelow", page);
            Size(gapBelow, 10f, 70f);
            var footer = UiFactory.Text("Footer", page, DesktopTexts.PortalFooter, theme.smallSize, new Color(0.45f, 0.52f, 0.64f), TextAnchor.MiddleCenter);
            Size(footer.rectTransform, 1000f, 30f);
        }

        private void Tile(RectTransform parent, WebLink link)
        {
            var background = UiFactory.Image("Link_" + link.target, parent, new Color(1f, 1f, 1f, 0.07f));
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.07f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.16f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.26f);
            colors.selectedColor = colors.normalColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            string destination = link.target;
            button.onClick.AddListener(() => navigate(destination));

            var group = background.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(24, 24, 24, 20);
            group.spacing = 10f;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Size(background.rectTransform, TileWidth, TileHeight);

            Color badgeColor = theme.accent;
            if (!string.IsNullOrEmpty(link.color)) ColorUtility.TryParseHtmlString(link.color, out badgeColor);
            var badge = UiFactory.Image("Badge", background.transform, badgeColor, false);
            Size(badge.rectTransform, 68f, 68f);
            var letters = UiFactory.Text("Letters", badge.transform, link.badge ?? "", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Fill(letters.rectTransform);

            var label = UiFactory.Text("Label", background.transform, link.label, 25, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            Size(label.rectTransform, TileWidth - 48f, -1f);
            if (!string.IsNullOrEmpty(link.note))
            {
                var note = UiFactory.Text("Note", background.transform, link.note, 17, Soft, TextAnchor.UpperLeft);
                Size(note.rectTransform, TileWidth - 48f, -1f);
            }
        }

        // A size for the layout groups; a negative number leaves that direction to the content.
        private static void Size(RectTransform rect, float width, float height)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (element == null) element = rect.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                element.minWidth = width;
                element.preferredWidth = width;
            }
            if (height >= 0f)
            {
                element.minHeight = height;
                element.preferredHeight = height;
            }
            element.flexibleWidth = 0f;
        }
    }
}
