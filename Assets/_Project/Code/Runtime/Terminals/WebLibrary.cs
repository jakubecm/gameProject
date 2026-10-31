using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>A link on a page: the words on the button and the id of the page it leads to.</summary>
    [Serializable]
    public class WebLink
    {
        public string label;
        public string target;

        // Used by the tiles of a "portal" start page; all optional.
        public string note;
        public string badge;
        public string color;
    }

    /// <summary>
    /// One page of the in-game web. <c>kind</c> is "portal" (a start page), "list" (headlines) or "article" (something to
    /// read: the game is paused while one is open).
    ///
    /// <c>layout</c> chooses how the page is drawn. Empty: a plain page (title, text, link buttons). "news_section": a page of the
    /// news site with its masthead and menu that shows the <c>stories</c> (the first is the lead story, the next three are
    /// the side stories, the rest are a grid of cards). "news_article": an article of the news site, with its section, byline,
    /// picture (the <c>image</c> key, see <see cref="DesktopTheme.WebImageFor"/>), caption, paragraphs (the text, split at blank
    /// lines) and <c>related</c> stories. "portal": the start page, a heading and a tile for every link (see
    /// <see cref="WebLink"/>).
    /// </summary>
    [Serializable]
    public class WebPage
    {
        public string id;
        public string url;
        public string title;
        public string kind;
        public string date;
        public string text;
        public WebLink[] links;

        public string layout;
        public string section;
        public string tag;
        public string byline;
        public string summary;
        public string image;
        public string caption;
        public string[] stories;
        public string[] related;

        public bool IsArticle => string.Equals(kind, "article", StringComparison.OrdinalIgnoreCase);

        public bool IsNewsSection => string.Equals(layout, "news_section", StringComparison.OrdinalIgnoreCase);

        public bool IsNewsArticle => string.Equals(layout, "news_article", StringComparison.OrdinalIgnoreCase);

        public bool IsNews => IsNewsSection || IsNewsArticle;

        public bool IsPortal => string.Equals(layout, "portal", StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    internal class WebFile
    {
        public string home;
        public WebLink[] newsNav;
        public WebPage[] pages;
    }

    /// <summary>The pages of the in-game web (Assets/_Project/Data/Web), as a small site the browser can walk through.</summary>
    public sealed class WebSite
    {
        private readonly Dictionary<string, WebPage> pages;

        public string Home { get; }

        /// <summary>The menu of the news site: a label and the section page it opens.</summary>
        public IReadOnlyList<WebLink> NewsNav { get; }

        public IReadOnlyCollection<WebPage> Pages => pages.Values;

        public WebSite(string home, IEnumerable<WebPage> pages, IEnumerable<WebLink> newsNav = null)
        {
            this.pages = new Dictionary<string, WebPage>(StringComparer.OrdinalIgnoreCase);
            foreach (var page in pages)
            {
                if (page != null && !string.IsNullOrWhiteSpace(page.id) && !this.pages.ContainsKey(page.id))
                {
                    this.pages.Add(page.id, page);
                }
            }
            Home = home;
            NewsNav = (newsNav ?? new WebLink[0]).Where(link => link != null).ToList();
        }

        public bool TryGet(string id, out WebPage page)
        {
            if (id == null)
            {
                page = null;
                return false;
            }
            return pages.TryGetValue(id, out page);
        }

        /// <summary>Everything that is wrong with the site: a missing home page, a link or a story that leads nowhere. Empty when it is fine.</summary>
        public IReadOnlyList<string> FindProblems()
        {
            var problems = new List<string>();
            if (!TryGet(Home, out _)) problems.Add($"The home page '{Home}' does not exist.");

            foreach (var link in NewsNav)
            {
                if (!TryGet(link.target, out _)) problems.Add($"The news menu item '{link.label}' leads to '{link.target}', which does not exist.");
            }

            foreach (var page in pages.Values)
            {
                if (string.IsNullOrWhiteSpace(page.title)) problems.Add($"Page '{page.id}' has no title.");
                foreach (var link in page.links ?? new WebLink[0])
                {
                    if (!TryGet(link.target, out _)) problems.Add($"Page '{page.id}' links to '{link.target}', which does not exist.");
                }
                foreach (string story in page.stories ?? new string[0])
                {
                    if (!TryGet(story, out _)) problems.Add($"Page '{page.id}' lists the story '{story}', which does not exist.");
                }
                foreach (string related in page.related ?? new string[0])
                {
                    if (!TryGet(related, out _)) problems.Add($"Page '{page.id}' is related to '{related}', which does not exist.");
                }
            }
            return problems;
        }
    }

    public static class WebLibrary
    {
        /// <summary>Reads the web file: <c>{ "home", "newsNav": [ {label, target} ], "pages": [ ... ] }</c>, see <see cref="WebPage"/>.</summary>
        public static WebSite Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new WebSite("start", new List<WebPage>());

            try
            {
                var file = JsonUtility.FromJson<WebFile>(json);
                var pages = file?.pages ?? new WebPage[0];
                return new WebSite(string.IsNullOrWhiteSpace(file?.home) ? pages.FirstOrDefault()?.id : file.home, pages, file?.newsNav);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"The web file is not valid JSON: {exception.Message}");
                return new WebSite("start", new List<WebPage>());
            }
        }
    }
}
