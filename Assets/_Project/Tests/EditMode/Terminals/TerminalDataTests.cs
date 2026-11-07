using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColdShutdown.Tests.EditMode.Terminals
{
    public class TerminalDataTests
    {
        private const string KnowledgePath = "Assets/_Project/Data/Lore/knowledge_base.json";
        private const string WebPath = "Assets/_Project/Data/Web/web_pages.json";
        private const string WordsPath = "Assets/_Project/Data/Minigames/password_words.json";

        private readonly object owner = new object();
        private readonly object otherOwner = new object();

        [TearDown]
        public void TearDown()
        {
            GamePause.Release(owner);
            GamePause.Release(otherOwner);
            Time.timeScale = 1f;
        }

        [Test]
        public void KnowledgeBase_HasDocumentsWithEverythingTheReaderShows()
        {
            var articles = LoreLibrary.Parse(File.ReadAllText(KnowledgePath));

            Assert.GreaterOrEqual(articles.Count, 4);
            Assert.AreEqual(articles.Count, articles.Select(article => article.id).Distinct().Count(), "ids must be unique");
            foreach (var article in articles)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(article.id), "id");
                Assert.IsFalse(string.IsNullOrWhiteSpace(article.category), $"{article.id}: category");
                Assert.IsFalse(string.IsNullOrWhiteSpace(article.title), $"{article.id}: title");
                Assert.IsFalse(string.IsNullOrWhiteSpace(article.date), $"{article.id}: date");
                Assert.Greater(article.text.Length, 80, $"{article.id}: text");
            }
        }

        [Test]
        public void KnowledgeBase_TellsAboutPriorIncidents()
        {
            var articles = LoreLibrary.Parse(File.ReadAllText(KnowledgePath));
            Assert.GreaterOrEqual(articles.Count(article => article.category == "Incident"), 2, "prior incidents to read about");
            Assert.IsTrue(articles.Any(article => article.category == "Procedure"));
        }

        [Test]
        public void LoreParse_SkipsArticlesWithoutATitle()
        {
            var articles = LoreLibrary.Parse("{\"articles\":[{\"id\":\"a\",\"title\":\"Title\",\"text\":\"x\"},{\"id\":\"b\",\"title\":\"\",\"text\":\"y\"}]}");
            Assert.AreEqual(1, articles.Count);
            Assert.AreEqual("a", articles[0].id);
        }

        [Test]
        public void LoreParse_GivesAnEmptyListForNothingOrGarbage()
        {
            Assert.IsEmpty(LoreLibrary.Parse(null));
            Assert.IsEmpty(LoreLibrary.Parse("   "));
            Assert.IsEmpty(LoreLibrary.Parse("{}"));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("not valid JSON"));
            Assert.IsEmpty(LoreLibrary.Parse("this is { not json"));
        }

        [Test]
        public void TheWeb_HasNoDeadLinks_AndAHomePage()
        {
            var site = WebLibrary.Parse(File.ReadAllText(WebPath));

            Assert.IsEmpty(site.FindProblems());
            Assert.IsTrue(site.TryGet(site.Home, out var home));
            Assert.AreEqual("portal", home.kind);
            Assert.IsTrue(home.IsPortal);
            foreach (var link in home.links)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(link.badge), $"{link.target} needs a badge");
                Assert.IsFalse(string.IsNullOrWhiteSpace(link.note), $"{link.target} needs a note");
                Assert.IsTrue(ColorUtility.TryParseHtmlString(link.color, out _), $"{link.target} needs a colour like #EE5414");
            }
        }

        [Test]
        public void TheWeb_HasANewsSiteWithSectionsAndArticles()
        {
            var site = WebLibrary.Parse(File.ReadAllText(WebPath));

            Assert.GreaterOrEqual(site.NewsNav.Count, 4);
            Assert.IsTrue(site.TryGet("news_front", out var front));
            Assert.IsTrue(front.IsNewsSection);
            Assert.AreEqual("list", front.kind);

            var sections = new System.Collections.Generic.List<WebPage>();
            foreach (var link in site.NewsNav)
            {
                Assert.IsTrue(site.TryGet(link.target, out var section), link.label);
                Assert.IsTrue(section.IsNewsSection, link.label);
                Assert.GreaterOrEqual(section.stories.Length, 2, $"{link.label} has stories");
                sections.Add(section);
            }

            var articles = site.Pages.Where(page => page.IsNewsArticle).ToList();
            Assert.GreaterOrEqual(articles.Count, 16);
            Assert.GreaterOrEqual(front.stories.Length, 8);
            var frontSections = front.stories.Select(id => { site.TryGet(id, out var story); return story.section; }).Distinct().ToList();
            Assert.GreaterOrEqual(frontSections.Count, 5, "the front page mixes sections: " + string.Join(", ", frontSections));
            CollectionAssert.Contains(sections.Select(section => section.section).ToList(), "Sport", "not only about the AI story");
            foreach (var article in articles)
            {
                Assert.IsTrue(article.IsArticle, $"{article.id} is something to read (pauses the game)");
                Assert.IsTrue(article.url.StartsWith("aacnews.net"), article.id);
                Assert.IsTrue(sections.Any(section => section.section == article.section && section.stories.Contains(article.id)), $"{article.id} is in its section");
                Assert.Greater(article.text.Length, 200, article.id);
                foreach (string field in new[] { article.date, article.byline, article.summary, article.caption, article.image })
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(field), article.id);
                }
                Assert.IsNotEmpty(article.related, article.id);
                StringAssert.Contains("DRAFT", article.text, article.id);
            }
        }

        [Test]
        public void TheNewsSite_EveryArticlePictureHasAPlaceholder()
        {
            var site = WebLibrary.Parse(File.ReadAllText(WebPath));

            foreach (var article in site.Pages.Where(page => page.IsNewsArticle))
            {
                CollectionAssert.Contains(ColdShutdown.Editor.WebImagePainter.Keys, article.image, article.id);
            }
        }

        [Test]
        public void TheWeb_EveryPageHasAUniqueAddress()
        {
            var site = WebLibrary.Parse(File.ReadAllText(WebPath));
            Assert.AreEqual(site.Pages.Count, site.Pages.Select(page => page.url).Distinct().Count());
            Assert.IsTrue(site.Pages.All(page => !string.IsNullOrWhiteSpace(page.url)));
        }

        [Test]
        public void TheWebParse_FindsDeadLinks_AndGivesAnEmptySiteForGarbage()
        {
            var site = WebLibrary.Parse("{\"home\":\"a\",\"pages\":[{\"id\":\"a\",\"title\":\"A\",\"links\":[{\"label\":\"x\",\"target\":\"nowhere\"}]}]}");
            Assert.AreEqual(1, site.FindProblems().Count);
            StringAssert.Contains("nowhere", site.FindProblems()[0]);

            Assert.IsEmpty(WebLibrary.Parse(null).Pages);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("not valid JSON"));
            Assert.IsEmpty(WebLibrary.Parse("not { json").Pages);
        }

        [Test]
        public void PasswordWordFile_MakesPuzzles()
        {
            var words = PasswordWordList.Parse(File.ReadAllText(WordsPath));

            Assert.GreaterOrEqual(words.Count, 12);
            for (int seed = 0; seed < 50; seed++)
            {
                var puzzle = PasswordPuzzle.Create(words, seed);
                Assert.AreEqual(PasswordPuzzle.DefaultCandidateCount, puzzle.Candidates.Count, $"seed {seed}");
            }
        }

        [Test]
        public void PasswordWordFile_WordsAreUpperCaseLettersOnly()
        {
            foreach (string word in PasswordWordList.Parse(File.ReadAllText(WordsPath)))
            {
                Assert.IsTrue(word.All(character => character >= 'A' && character <= 'Z'), word);
            }
        }

        [Test]
        public void PasswordWordParse_GivesAnEmptyListForNothingOrGarbage()
        {
            Assert.IsEmpty(PasswordWordList.Parse(null));
            Assert.IsEmpty(PasswordWordList.Parse("{}"));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("not valid JSON"));
            Assert.IsEmpty(PasswordWordList.Parse("[[["));
        }

        [Test]
        public void ThePlayerFacingTexts_AreInEnglish()
        {
            // No Czech letters in the texts the player reads: the desktop captions and every data file.
            const string czech = "áčďéěíňóřšťúůýžÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ";
            foreach (string path in new[] { KnowledgePath, WebPath, WordsPath, "Assets/_Project/Code/Runtime/Terminals/DesktopTexts.cs" })
            {
                string text = File.ReadAllText(path);
                Assert.IsFalse(text.Any(character => czech.Contains(character)), $"{path} contains Czech letters");
            }
        }

        [Test]
        public void TheGame_IsPausedWhileSomethingAsks_AndRunsAgainWhenTheLastOneLetsGo()
        {
            Assert.IsFalse(GamePause.IsPaused);
            bool? last = null;
            System.Action<bool> listener = paused => last = paused;
            GamePause.Changed += listener;
            try
            {
                GamePause.Request(owner);
                Assert.IsTrue(GamePause.IsPaused);
                Assert.AreEqual(0f, Time.timeScale);
                Assert.AreEqual(true, last);

                GamePause.Request(otherOwner);
                GamePause.Release(owner);
                Assert.IsTrue(GamePause.IsPaused, "the other one still wants the pause");

                GamePause.Release(otherOwner);
                Assert.IsFalse(GamePause.IsPaused);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.AreEqual(false, last);
            }
            finally
            {
                GamePause.Changed -= listener;
            }
        }

        [Test]
        public void AskingTwice_OrLettingGoWithoutAsking_DoesNothingWrong()
        {
            GamePause.Request(owner);
            GamePause.Request(owner);
            GamePause.Release(owner);
            Assert.IsFalse(GamePause.IsPaused, "one owner counts once");

            Assert.DoesNotThrow(() => GamePause.Release(owner));
            Assert.DoesNotThrow(() => GamePause.Release(null));
            Assert.IsFalse(GamePause.IsPaused);
        }
    }
}
