using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Draws the pages of the fictional news site "AAC News" in the browser: a masthead with the logo, a menu of sections,
    /// and either a section page (a lead story with a big picture, side stories with thumbnails, a grid of cards) or an
    /// article (headline, summary, byline, picture with caption, paragraphs, related stories). The logo is drawn from
    /// shapes and text, the colours are the site's own. The pages and their pictures are data, see <see cref="WebPage"/>.
    /// A story without a picture gets a coloured block in its place.
    /// </summary>
    internal sealed class NewsPageBuilder
    {
        /// <summary>Width of the page inside the browser window: the window is 1120 wide with 32 on each side.</summary>
        public const float PageWidth = 1056f;

        public const float ColumnWidth = 700f;
        private const float LeadWidth = 640f;
        private const float SideWidth = 388f;
        private const float GridCardWidth = 336f;
        private const float RelatedCardWidth = 220f;

        private static readonly Color Brand = new Color(0.93f, 0.33f, 0.08f);
        private static readonly Color Ink = new Color(0.09f, 0.09f, 0.11f);
        private static readonly Color Muted = new Color(0.40f, 0.40f, 0.44f);
        private static readonly Color Rule = new Color(0.86f, 0.86f, 0.88f);

        private readonly DesktopTheme theme;
        private readonly WebSite site;
        private readonly Action<string> navigate;
        private readonly Font headline;

        public NewsPageBuilder(DesktopTheme theme, WebSite site, Action<string> navigate)
        {
            this.theme = theme;
            this.site = site;
            this.navigate = navigate;
            headline = theme.HeadlineFont;
        }

        /// <summary>The colour of the pages of the news site behind everything.</summary>
        public static Color Paper => Color.white;

        public void Build(RectTransform page, WebPage target)
        {
            Masthead(page);
            Divider(page);
            Menu(page, target);
            Divider(page);

            if (target.IsNewsSection) Section(page, target);
            else Article(page, target);
        }

        // ---------------------------------------------------------------- the frame of the site

        private void Masthead(RectTransform page)
        {
            var bar = Horizontal("Masthead", page, PageWidth, 12f, TextAnchor.MiddleLeft, new RectOffset(0, 0, 14, 14));
            SetSize(bar, PageWidth, 76f);

            var box = UiFactory.Image("LogoBox", bar, Brand, false);
            SetSize(box.rectTransform, 96f, 48f);
            var name = UiFactory.Text("Name", box.transform, DesktopTexts.NewsName, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Fill(name.rectTransform);

            var word = UiFactory.Text("Word", bar, DesktopTexts.NewsWord, 38, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSize(word.rectTransform, 130f, 48f);

            Spacer(bar);
            var tagline = UiFactory.Text("Tagline", bar, DesktopTexts.NewsTagline, theme.smallSize, Muted, TextAnchor.MiddleRight);
            SetSize(tagline.rectTransform, 440f, 48f);
        }

        private void Menu(RectTransform page, WebPage current)
        {
            var row = Horizontal("Menu", page, PageWidth, 4f, TextAnchor.MiddleLeft, new RectOffset());
            SetSize(row, PageWidth, 46f);

            foreach (var link in site.NewsNav)
            {
                bool active = IsActive(link, current);
                var button = UiFactory.Button("Nav_" + link.target, row, link.label, theme, 17, new Color(1f, 1f, 1f, 0.004f), active ? Ink : Muted);
                var colors = button.colors;
                colors.highlightedColor = new Color(0f, 0f, 0f, 0.06f);
                colors.pressedColor = new Color(0f, 0f, 0f, 0.12f);
                button.colors = colors;
                if (active) button.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
                SetSize((RectTransform)button.transform, 36f + link.label.Length * 10f, 46f);

                if (active)
                {
                    var underline = UiFactory.Image("Underline", button.transform, Brand, false);
                    underline.rectTransform.anchorMin = new Vector2(0f, 0f);
                    underline.rectTransform.anchorMax = new Vector2(1f, 0f);
                    underline.rectTransform.pivot = new Vector2(0.5f, 0f);
                    underline.rectTransform.offsetMin = Vector2.zero;
                    underline.rectTransform.offsetMax = new Vector2(0f, 4f);
                }

                string target = link.target;
                button.onClick.AddListener(() => navigate(target));
            }
        }

        // The tab of the section being read is marked; an article belongs to the tab of its section.
        private bool IsActive(WebLink link, WebPage current)
        {
            if (!site.TryGet(link.target, out var section)) return false;
            if (string.Equals(section.id, current.id, StringComparison.OrdinalIgnoreCase)) return true;
            return !string.IsNullOrEmpty(current.section) && string.Equals(section.section, current.section, StringComparison.OrdinalIgnoreCase);
        }

        // ---------------------------------------------------------------- a section: lead, side stories, grid

        private void Section(RectTransform page, WebPage target)
        {
            var stories = Stories(target);
            if (stories.Count == 0)
            {
                var empty = UiFactory.Text("Empty", page, DesktopTexts.NewsNoStories, theme.bodySize, Muted, TextAnchor.MiddleCenter);
                SetSize(empty.rectTransform, PageWidth, 80f);
                return;
            }

            var front = Horizontal("Front", page, PageWidth, 28f, TextAnchor.UpperLeft, new RectOffset(0, 0, 28, 28));
            SetSize(front, PageWidth, -1f);
            VerticalCard("Card_" + stories[0].id, front, stories[0], LeadWidth, 270f, 32, 19, showSummary: true);

            if (stories.Count > 1)
            {
                var side = Vertical("Side", front, SideWidth, 18f, TextAnchor.UpperLeft, new RectOffset());
                foreach (var story in stories.Skip(1).Take(3))
                {
                    SideCard(side, story);
                }
            }

            var more = stories.Skip(4).ToList();
            for (int start = 0; start < more.Count; start += 3)
            {
                Divider(page);
                var row = Horizontal("GridRow", page, PageWidth, 24f, TextAnchor.UpperLeft, new RectOffset(0, 0, 24, 8));
                SetSize(row, PageWidth, -1f);
                foreach (var story in more.Skip(start).Take(3))
                {
                    VerticalCard("Card_" + story.id, row, story, GridCardWidth, 189f, 21, 0, showSummary: false);
                }
            }
        }

        private List<WebPage> Stories(WebPage section)
        {
            var found = new List<WebPage>();
            foreach (string id in section.stories ?? new string[0])
            {
                if (site.TryGet(id, out var story)) found.Add(story);
            }
            return found;
        }

        // ---------------------------------------------------------------- an article

        private void Article(RectTransform page, WebPage story)
        {
            var column = Vertical("Article", page, ColumnWidth, 14f, TextAnchor.UpperLeft, new RectOffset(0, 0, 30, 30));
            SetSize(column, ColumnWidth, -1f);

            string kicker = string.Join("  ·  ", new[] { story.tag, story.section }.Where(part => !string.IsNullOrEmpty(part)));
            var tag = UiFactory.Text("Tag", column, kicker.ToUpperInvariant(), theme.smallSize + 1, Brand, TextAnchor.UpperLeft, FontStyle.Bold);
            Fixed(tag.rectTransform, ColumnWidth);

            var title = UiFactory.Text("Headline", column, story.title, 40, Ink, TextAnchor.UpperLeft, FontStyle.Bold, headline);
            Fixed(title.rectTransform, ColumnWidth);

            if (!string.IsNullOrEmpty(story.summary))
            {
                var summary = UiFactory.Text("Summary", column, story.summary, 21, new Color(0.22f, 0.22f, 0.26f), TextAnchor.UpperLeft, FontStyle.Normal, headline);
                Fixed(summary.rectTransform, ColumnWidth);
            }

            var byline = UiFactory.Text("Byline", column, ByLine(story), theme.smallSize + 1, Muted, TextAnchor.UpperLeft);
            Fixed(byline.rectTransform, ColumnWidth);

            Photo(column, story, ColumnWidth, ColumnWidth * 9f / 16f);
            if (!string.IsNullOrEmpty(story.caption))
            {
                var caption = UiFactory.Text("Caption", column, story.caption, theme.smallSize, Muted, TextAnchor.UpperLeft, FontStyle.Italic);
                Fixed(caption.rectTransform, ColumnWidth);
            }

            var paragraphs = (story.text ?? "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < paragraphs.Length; i++)
            {
                var paragraph = UiFactory.Text("Paragraph_" + i, column, paragraphs[i].Trim(), 19, Ink, TextAnchor.UpperLeft, FontStyle.Normal, headline);
                paragraph.lineSpacing = 1.2f;
                Fixed(paragraph.rectTransform, ColumnWidth);
            }

            var related = (story.related ?? new string[0]).Select(id => site.TryGet(id, out var found) ? found : null).Where(found => found != null).Take(3).ToList();
            if (related.Count == 0) return;

            Divider(column, ColumnWidth);
            var heading = UiFactory.Text("RelatedHeading", column, DesktopTexts.NewsRelated, 24, Ink, TextAnchor.UpperLeft, FontStyle.Bold, headline);
            Fixed(heading.rectTransform, ColumnWidth);
            var row = Horizontal("Related", column, ColumnWidth, 20f, TextAnchor.UpperLeft, new RectOffset());
            SetSize(row, ColumnWidth, -1f);
            foreach (var other in related)
            {
                VerticalCard("Card_" + other.id, row, other, RelatedCardWidth, 124f, 18, 0, showSummary: false);
            }
        }

        private static string ByLine(WebPage story)
        {
            string by = string.IsNullOrEmpty(story.byline) ? "" : $"<b><color=#171719>{story.byline}</color></b>";
            string date = string.IsNullOrEmpty(story.date) ? "" : story.date;
            return by.Length > 0 && date.Length > 0 ? by + "\n" + date : by + date;
        }

        // ---------------------------------------------------------------- cards and pictures

        // A story as a card to click: the picture, a line with the tag, the section and the time, the headline, and maybe the summary.
        private void VerticalCard(string name, RectTransform parent, WebPage story, float width, float photoHeight, int headlineSize, int summarySize, bool showSummary)
        {
            var button = CardButton(name, parent, story.id);
            var group = button.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 8f;
            group.padding = new RectOffset(0, 0, 0, 10);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            SetSize((RectTransform)button.transform, width, -1f);

            Photo(button.transform, story, width, photoHeight);
            var meta = UiFactory.Text("Meta", button.transform, MetaLine(story, includeDate: width > 300f, includeTag: width > 300f), theme.smallSize, Muted, TextAnchor.UpperLeft);
            Fixed(meta.rectTransform, width);
            var title = UiFactory.Text("Headline", button.transform, story.title, headlineSize, Ink, TextAnchor.UpperLeft, FontStyle.Bold, headline);
            Fixed(title.rectTransform, width);
            if (showSummary && !string.IsNullOrEmpty(story.summary))
            {
                var summary = UiFactory.Text("Summary", button.transform, story.summary, summarySize, new Color(0.25f, 0.25f, 0.29f), TextAnchor.UpperLeft, FontStyle.Normal, headline);
                Fixed(summary.rectTransform, width);
            }
        }

        // A small story beside the lead: a thumbnail, and next to it the line and the headline.
        private void SideCard(RectTransform parent, WebPage story)
        {
            var button = CardButton("Card_" + story.id, parent, story.id);
            var group = button.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 14f;
            group.padding = new RectOffset(0, 0, 0, 0);
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            SetSize((RectTransform)button.transform, SideWidth, -1f);

            Photo(button.transform, story, 150f, 84f);
            var texts = Vertical("Texts", button.transform, 224f, 4f, TextAnchor.UpperLeft, new RectOffset());
            SetSize(texts, 224f, -1f);
            var meta = UiFactory.Text("Meta", texts, MetaLine(story, includeDate: true), theme.smallSize - 1, Muted, TextAnchor.UpperLeft);
            Fixed(meta.rectTransform, 224f);
            var title = UiFactory.Text("Headline", texts, story.title, 18, Ink, TextAnchor.UpperLeft, FontStyle.Bold, headline);
            Fixed(title.rectTransform, 224f);
        }

        private Button CardButton(string name, Transform parent, string storyId)
        {
            var image = UiFactory.Image(name, parent, new Color(0f, 0f, 0f, 0.004f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = new Color(0f, 0f, 0f, 0.004f);
            colors.highlightedColor = new Color(0f, 0f, 0f, 0.05f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.10f);
            colors.selectedColor = colors.normalColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.onClick.AddListener(() => navigate(storyId));
            return button;
        }

        /// <summary>The picture of a story cut to fit, or a coloured block in its colour when there is no picture.</summary>
        private RectTransform Photo(Transform parent, WebPage story, float width, float height)
        {
            var texture = theme.WebImageFor(story.image);
            RectTransform rect;
            if (texture != null)
            {
                var picture = UiFactory.Rect("Photo", parent).gameObject.AddComponent<RawImage>();
                picture.texture = texture;
                picture.uvRect = Desktop.CoverRect(texture.width / (float)texture.height, width / height);
                picture.raycastTarget = false;
                rect = picture.rectTransform;
            }
            else
            {
                var block = UiFactory.Image("PhotoPlaceholder", parent, SectionColor(story.section), false);
                var label = UiFactory.Text("Label", block.transform, DesktopTexts.NewsPhoto, theme.smallSize, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter);
                UiFactory.Fill(label.rectTransform);
                rect = block.rectTransform;
            }
            SetSize(rect, width, height);
            return rect;
        }

        private static Color SectionColor(string section)
        {
            int hash = string.IsNullOrEmpty(section) ? 7 : section.ToLowerInvariant().Aggregate(17, (value, character) => unchecked(value * 31 + character));
            return Color.HSVToRGB(Mathf.Abs(hash % 360) / 360f, 0.35f, 0.55f);
        }

        // "TECHNOLOGY  ·  2 hrs ago": the tag in the site's colour first, if the story has one.
        private static string MetaLine(WebPage story, bool includeDate, bool includeTag = true)
        {
            string tag = !includeTag || string.IsNullOrEmpty(story.tag) ? "" : $"<b><color=#{ColorUtility.ToHtmlStringRGB(Brand)}>{story.tag}</color></b>   ";
            var parts = new[] { story.section, includeDate ? story.date : null }.Where(part => !string.IsNullOrEmpty(part));
            return tag + string.Join("  ·  ", parts);
        }

        // ---------------------------------------------------------------- layout helpers

        private static RectTransform Vertical(string name, Transform parent, float width, float spacing, TextAnchor alignment, RectOffset padding)
        {
            var rect = UiFactory.Rect(name, parent);
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.padding = padding;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return rect;
        }

        private static RectTransform Horizontal(string name, Transform parent, float width, float spacing, TextAnchor alignment, RectOffset padding)
        {
            var rect = UiFactory.Rect(name, parent);
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.padding = padding;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return rect;
        }

        /// <summary>Gives a rect a size for the layout groups. A negative number leaves that direction to the content.</summary>
        private static void SetSize(RectTransform rect, float width, float height)
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

        // A text keeps the width of its column; the height follows from the words.
        private static void Fixed(RectTransform rect, float width) => SetSize(rect, width, -1f);

        private static void Spacer(RectTransform parent)
        {
            var spacer = UiFactory.Rect("Spacer", parent);
            var element = spacer.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
        }

        private static void Divider(RectTransform parent, float width = PageWidth)
        {
            var line = UiFactory.Image("Divider", parent, Rule, false);
            SetSize(line.rectTransform, width, 1f);
        }
    }
}
