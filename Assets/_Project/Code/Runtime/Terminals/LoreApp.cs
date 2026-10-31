using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The knowledge base, the lore reader of task 3.2: a list of documents (incident reports, procedures, notices) on the left, the chosen one on the right. While a document is
    /// open the game is paused (<see cref="GamePause"/>); closing the window, or leaving the computer, lets it run
    /// again. The texts come from a data file, see <see cref="LoreLibrary"/>.
    /// </summary>
    public sealed class LoreApp : ITerminalApp
    {
        private readonly IReadOnlyList<LoreArticle> articles;
        private readonly List<Button> entryButtons = new List<Button>();

        private DesktopTheme theme;
        private Text titleText;
        private Text metaText;
        private Text bodyText;
        private ScrollRect scroll;
        private bool reading;

        public LoreApp(IReadOnlyList<LoreArticle> articles)
        {
            this.articles = articles ?? new List<LoreArticle>();
        }

        public string Id => "knowledge";

        public string Title => DesktopTexts.KnowledgeTitle;

        public string IconLetter => "K";

        public Color IconColor => new Color(0.93f, 0.60f, 0.12f);

        public Vector2 WindowSize => new Vector2(880f, 540f);

        public IReadOnlyList<LoreArticle> Articles => articles;

        /// <summary>The document being read, or -1.</summary>
        public int SelectedIndex { get; private set; } = -1;

        /// <summary>True while a document is open; the game is paused for that time.</summary>
        public bool IsReading => reading;

        /// <summary>Raised when reading starts (true) or ends (false).</summary>
        public event Action<bool> ReadingChanged;

        public void Build(RectTransform content, DesktopTheme theme)
        {
            this.theme = theme;
            entryButtons.Clear();
            StopReading();
            SelectedIndex = -1;

            var list = UiFactory.Image("List", content, theme.panel);
            // The list is a column on the left side, as high as the window.
            list.rectTransform.anchorMin = new Vector2(0f, 0f);
            list.rectTransform.anchorMax = new Vector2(0f, 1f);
            list.rectTransform.pivot = new Vector2(0f, 0.5f);
            list.rectTransform.offsetMin = Vector2.zero;
            list.rectTransform.offsetMax = new Vector2(270f, 0f);

            if (articles.Count == 0)
            {
                var empty = UiFactory.Text("Empty", list.transform, DesktopTexts.KnowledgeEmpty, theme.bodySize, theme.mutedText, TextAnchor.UpperLeft);
                UiFactory.Place(empty.rectTransform, UiFactory.TopLeft, new Vector2(16f, -16f), new Vector2(238f, 30f));
            }

            for (int i = 0; i < articles.Count; i++)
            {
                int index = i;
                var article = articles[i];
                string caption = $"{article.title}\n<size={theme.smallSize}>{article.category}  ·  {article.date}</size>";
                var button = UiFactory.Button("Entry_" + article.id, list.transform, caption, theme, theme.bodySize - 1, theme.panel, theme.bodyText, TextAnchor.MiddleLeft);
                button.GetComponentInChildren<Text>().supportRichText = true;
                UiFactory.Place((RectTransform)button.transform, UiFactory.TopLeft, new Vector2(8f, -8f - i * 72f), new Vector2(254f, 68f));
                button.onClick.AddListener(() => Select(index));
                entryButtons.Add(button);
            }

            titleText = UiFactory.Text("Heading", content, DesktopTexts.KnowledgeChoose, theme.headingSize, theme.bodyText, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(titleText.rectTransform, UiFactory.TopLeft, new Vector2(290f, -14f), new Vector2(570f, 60f));
            metaText = UiFactory.Text("Meta", content, "", theme.smallSize, theme.mutedText, TextAnchor.UpperLeft);
            UiFactory.Place(metaText.rectTransform, UiFactory.TopLeft, new Vector2(290f, -70f), new Vector2(570f, 22f));

            var scrollRoot = UiFactory.Rect("Scroll", content);
            UiFactory.Fill(scrollRoot, 290f, 100f, 20f, 40f);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = UiFactory.Image("Viewport", scrollRoot, new Color(1f, 1f, 1f, 0.01f));
            UiFactory.Fill(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();

            bodyText = UiFactory.Text("Text", viewport.transform, "", theme.bodySize, theme.bodyText, TextAnchor.UpperLeft);
            bodyText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            bodyText.rectTransform.pivot = new Vector2(0.5f, 1f);
            bodyText.rectTransform.offsetMin = Vector2.zero;
            bodyText.rectTransform.offsetMax = Vector2.zero;
            bodyText.lineSpacing = 1.15f;
            var fitter = bodyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = bodyText.rectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var note = UiFactory.Text("Note", content, DesktopTexts.ReadingPauseNote, theme.smallSize, theme.mutedText, TextAnchor.LowerLeft);
            UiFactory.Place(note.rectTransform, UiFactory.BottomLeft, new Vector2(290f, 10f), new Vector2(570f, 22f));
        }

        /// <summary>Opens a document and pauses the game.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= articles.Count) return;

            var article = articles[index];
            SelectedIndex = index;
            titleText.text = article.title;
            metaText.text = $"{article.category}  ·  {article.date}";
            bodyText.text = article.text;
            scroll.verticalNormalizedPosition = 1f;
            StartReading();
        }

        public void Closed()
        {
            StopReading();
            SelectedIndex = -1;
        }

        public void Suspended()
        {
            if (reading) GamePause.Release(this);
        }

        public void Resumed()
        {
            if (reading) GamePause.Request(this);
        }

        private void StartReading()
        {
            if (reading) return;

            reading = true;
            GamePause.Request(this);
            ReadingChanged?.Invoke(true);
        }

        private void StopReading()
        {
            if (!reading) return;

            reading = false;
            GamePause.Release(this);
            ReadingChanged?.Invoke(false);
        }
    }
}
