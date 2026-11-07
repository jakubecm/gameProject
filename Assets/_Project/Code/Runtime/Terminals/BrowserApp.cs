using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// A small web browser for the in-game web: a start page, a news site that tells how the world is doing, and so on.
    /// The pages are data (see <see cref="WebLibrary"/>); a click on a link goes to the page it names, Back goes back and Home
    /// to the start page. While an article is open the game is paused, like in the knowledge base.
    /// </summary>
    public sealed class BrowserApp : ITerminalApp
    {
        private readonly WebSite site;
        private readonly Stack<string> history = new Stack<string>();

        private DesktopTheme theme;
        private RectTransform page;
        private Text address;
        private Button backButton;
        private ScrollRect scroll;
        private Image viewportImage;
        private VerticalLayoutGroup pageLayout;
        private NewsPageBuilder news;
        private StartPageBuilder start;
        private bool reading;

        public BrowserApp(WebSite site)
        {
            this.site = site ?? new WebSite("start", new List<WebPage>());
        }

        public string Id => "browser";

        public string Title => DesktopTexts.BrowserTitle;

        public string IconLetter => "B";

        public Color IconColor => new Color(0.20f, 0.52f, 0.88f);

        public Vector2 WindowSize => new Vector2(1120f, 640f);

        /// <summary>The page on screen, or null before the window has been opened.</summary>
        public WebPage Current { get; private set; }

        public bool CanGoBack => history.Count > 0;

        /// <summary>True while an article is open; the game is paused for that time.</summary>
        public bool IsReading => reading;

        public event Action<bool> ReadingChanged;

        public void Build(RectTransform content, DesktopTheme theme)
        {
            this.theme = theme;
            news = new NewsPageBuilder(theme, site, Navigate);
            start = new StartPageBuilder(theme, Navigate);
            history.Clear();
            Current = null;
            StopReading();

            // The toolbar: back, home, and the address of the page.
            var toolbar = UiFactory.Image("Toolbar", content, theme.panel);
            UiFactory.Place(toolbar.rectTransform, UiFactory.TopLeft, Vector2.zero, new Vector2(WindowSize.x, 48f));

            backButton = UiFactory.Button("Back", toolbar.transform, DesktopTexts.BrowserBack, theme, theme.smallSize + 2);
            UiFactory.Place((RectTransform)backButton.transform, UiFactory.TopLeft, new Vector2(8f, -6f), new Vector2(84f, 36f));
            backButton.onClick.AddListener(Back);

            var home = UiFactory.Button("Home", toolbar.transform, DesktopTexts.BrowserHome, theme, theme.smallSize + 2);
            UiFactory.Place((RectTransform)home.transform, UiFactory.TopLeft, new Vector2(100f, -6f), new Vector2(84f, 36f));
            home.onClick.AddListener(Home);

            var addressBar = UiFactory.Image("AddressBar", toolbar.transform, Color.white);
            UiFactory.Place(addressBar.rectTransform, UiFactory.TopLeft, new Vector2(196f, -6f), new Vector2(WindowSize.x - 208f, 36f));
            address = UiFactory.Text("Address", addressBar.transform, "", theme.bodySize - 2, theme.mutedText);
            UiFactory.Fill(address.rectTransform, 12f, 0f, 12f, 0f);

            // The page: a scrolling column that holds the title, the text and the links.
            var scrollRoot = UiFactory.Rect("Scroll", content);
            UiFactory.Fill(scrollRoot, 0f, 48f, 0f, 0f);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = UiFactory.Image("Viewport", scrollRoot, theme.windowBackground);
            UiFactory.Fill(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewportImage = viewport;

            page = UiFactory.Rect("Page", viewport.transform);
            page.anchorMin = new Vector2(0f, 1f);
            page.anchorMax = new Vector2(1f, 1f);
            page.pivot = new Vector2(0.5f, 1f);
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;
            pageLayout = page.gameObject.AddComponent<VerticalLayoutGroup>();
            pageLayout.childControlWidth = true;
            pageLayout.childControlHeight = true;
            pageLayout.childForceExpandHeight = false;
            SetPageStyle(PageStyle.Plain);
            page.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = page;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            Show(site.Home, false);
        }

        /// <summary>Goes to a page, as a click on a link does.</summary>
        public void Navigate(string pageId) => Show(pageId, true);

        public void Back()
        {
            if (history.Count > 0) Show(history.Pop(), false);
        }

        public void Home()
        {
            if (Current != null && string.Equals(Current.id, site.Home, StringComparison.OrdinalIgnoreCase)) return;
            Show(site.Home, true);
        }

        public void Closed()
        {
            StopReading();
            Current = null;
            history.Clear();
        }

        public void Suspended()
        {
            if (reading) GamePause.Release(this);
        }

        public void Resumed()
        {
            if (reading) GamePause.Request(this);
        }

        private void Show(string pageId, bool remember)
        {
            if (page == null) return;

            if (!site.TryGet(pageId, out var target))
            {
                target = new WebPage { id = pageId ?? "?", url = pageId ?? "?", title = DesktopTexts.BrowserNotFound, kind = "list", text = "" };
            }
            if (remember && Current != null) history.Push(Current.id);
            Current = target;

            for (int i = page.childCount - 1; i >= 0; i--)
            {
                UiFactory.Dispose(page.GetChild(i).gameObject);
            }

            address.text = target.url;
            backButton.interactable = history.Count > 0;

            SetPageStyle(target.IsNews ? PageStyle.News : target.IsPortal ? PageStyle.Portal : PageStyle.Plain);
            if (target.IsNews)
            {
                news.Build(page, target);
            }
            else if (target.IsPortal)
            {
                start.Build(page, target);
            }
            else
            {
                UiFactory.Text("Title", page, target.title, theme.headingSize + 4, theme.bodyText, TextAnchor.UpperLeft, FontStyle.Bold);
                if (!string.IsNullOrEmpty(target.date))
                {
                    UiFactory.Text("Date", page, target.date, theme.smallSize, theme.mutedText, TextAnchor.UpperLeft);
                }
                if (!string.IsNullOrEmpty(target.text))
                {
                    var body = UiFactory.Text("Body", page, target.text, theme.bodySize, theme.bodyText, TextAnchor.UpperLeft);
                    body.lineSpacing = 1.15f;
                }

                bool headlines = string.Equals(target.kind, "list", StringComparison.OrdinalIgnoreCase);
                foreach (var link in target.links ?? new WebLink[0])
                {
                    string destination = link.target;
                    var button = UiFactory.Button("Link_" + destination, page, link.label, theme, theme.bodySize,
                        headlines ? theme.panel : theme.button, theme.bodyText, TextAnchor.MiddleLeft);
                    var element = button.gameObject.AddComponent<LayoutElement>();
                    element.minHeight = headlines ? 52f : 42f;
                    element.preferredHeight = element.minHeight;
                    button.onClick.AddListener(() => Navigate(destination));
                }
            }

            scroll.verticalNormalizedPosition = 1f;
            if (target.IsArticle) StartReading();
            else StopReading();
        }

        private enum PageStyle { Plain, News, Portal }

        // The news site is white and centred with a narrow margin, the start page dark and centred; the plain pages are a
        // left-aligned column on the window colour.
        private void SetPageStyle(PageStyle style)
        {
            bool centred = style != PageStyle.Plain;
            viewportImage.color = style == PageStyle.News ? NewsPageBuilder.Paper : style == PageStyle.Portal ? StartPageBuilder.Background : theme.windowBackground;
            pageLayout.padding = style == PageStyle.News ? new RectOffset(32, 32, 0, 40) : style == PageStyle.Portal ? new RectOffset(32, 32, 56, 40) : new RectOffset(40, 40, 28, 40);
            pageLayout.spacing = style == PageStyle.News ? 0f : style == PageStyle.Portal ? 14f : 14f;
            pageLayout.childAlignment = centred ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
            pageLayout.childForceExpandWidth = !centred;
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
