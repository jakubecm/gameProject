using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Tests.EditMode.Terminals
{
    /// <summary>The desktop, its power, the browser, the knowledge base and the Password minigame, clicked through by calling the buttons' click handlers.</summary>
    public class DesktopTests
    {
        private const string KnowledgePath = "Assets/_Project/Data/Lore/knowledge_base.json";
        private const string WebPath = "Assets/_Project/Data/Web/web_pages.json";
        private const string WordsPath = "Assets/_Project/Data/Minigames/password_words.json";

        private GameObject screen;
        private Desktop desktop;
        private BrowserApp browser;
        private LoreApp knowledge;
        private PasswordApp password;
        private IReadOnlyList<string> words;
        private int nextSeed;

        [SetUp]
        public void SetUp()
        {
            Build(startOn: true);
        }

        [TearDown]
        public void TearDown()
        {
            desktop.CloseAll();
            Object.DestroyImmediate(screen);
            Time.timeScale = 1f;
        }

        private void Build(bool startOn)
        {
            if (screen != null)
            {
                desktop.CloseAll();
                Object.DestroyImmediate(screen);
            }

            screen = new GameObject("Screen", typeof(RectTransform));
            desktop = new Desktop((RectTransform)screen.transform, DesktopTheme.CreateDefault(), startOn);

            browser = new BrowserApp(WebLibrary.Parse(File.ReadAllText(WebPath)));
            knowledge = new LoreApp(LoreLibrary.Parse(File.ReadAllText(KnowledgePath)));
            words = PasswordWordList.Parse(File.ReadAllText(WordsPath));
            nextSeed = 100;
            password = new PasswordApp(words, () => nextSeed++);
            desktop.AddApp(browser);
            desktop.AddApp(knowledge);
            desktop.AddApp(password);
        }

        // ---------------------------------------------------------------- the desktop

        [Test]
        public void TheDesktop_HasAnIconForEveryProgram_AndATaskbarWithAClock()
        {
            Assert.IsNotNull(desktop.SystemRoot.Find("Icons/Icon_browser/Tile"));
            Assert.IsNotNull(desktop.SystemRoot.Find("Icons/Icon_knowledge/Tile"));
            Assert.IsNotNull(desktop.SystemRoot.Find("Icons/Icon_password/Tile"));
            Assert.IsNotNull(desktop.SystemRoot.Find("Taskbar/Bar/Start"));

            var clock = desktop.SystemRoot.Find("Taskbar/Bar/Clock").GetComponent<Text>();
            StringAssert.IsMatch(@"^\d\d:\d\d$", clock.text);
            Assert.AreEqual(new Vector2(1280f, 720f), ((RectTransform)screen.transform).sizeDelta);
        }

        [Test]
        public void TheIcons_AreNamedInEnglish()
        {
            var labels = desktop.SystemRoot.Find("Icons").GetComponentsInChildren<Text>().Where(text => text.name == "Label").Select(text => text.text).ToList();
            CollectionAssert.AreEquivalent(new[] { "Browser", "Knowledge Base", "Security" }, labels);
        }

        [Test]
        public void ClickingAnIcon_OpensTheWindow_AndClosingItClosesIt()
        {
            Tile("knowledge").onClick.Invoke();

            Assert.IsTrue(desktop.IsOpen(knowledge));
            var window = desktop.WindowOf(knowledge);
            Assert.IsNotNull(window.Content);
            Assert.IsNotNull(desktop.SystemRoot.Find("Taskbar/Task_knowledge"), "the taskbar shows the open window");

            window.CloseButton.onClick.Invoke();
            Assert.IsFalse(desktop.IsOpen(knowledge));
            Assert.IsNull(desktop.SystemRoot.Find("Taskbar/Task_knowledge"));
        }

        [Test]
        public void ClickingTheIconOfAnOpenProgram_BringsItsWindowToTheFront_InsteadOfOpeningASecondOne()
        {
            Tile("knowledge").onClick.Invoke();
            Tile("password").onClick.Invoke();
            var first = desktop.WindowOf(knowledge);
            Assert.AreEqual(desktop.WindowOf(password).Root, first.Root.parent.GetChild(first.Root.parent.childCount - 1), "the last one opened is in front");

            Tile("knowledge").onClick.Invoke();

            Assert.AreEqual(2, desktop.Windows.Count);
            Assert.AreEqual(first.Root, first.Root.parent.GetChild(first.Root.parent.childCount - 1));
        }

        [Test]
        public void AWindow_StaysInsideTheScreen_AboveTheTaskbar()
        {
            foreach (var app in new ITerminalApp[] { browser, knowledge, password })
            {
                var window = desktop.Open(app);
                var corners = new Vector3[4];
                window.Root.GetWorldCorners(corners);
                var screenRect = (RectTransform)screen.transform;
                foreach (var corner in corners)
                {
                    var local = screenRect.InverseTransformPoint(corner);
                    Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(640f + 0.01f), app.Id);
                    Assert.That(local.y, Is.InRange(-360f + Desktop.TaskbarHeight - 0.01f, 360f + 0.01f), app.Id);
                }
            }
        }

        // ---------------------------------------------------------------- power

        [Test]
        public void ATurnedOffComputer_ShowsABlackScreen_AndOpensNothing()
        {
            Build(startOn: false);

            Assert.AreEqual(PowerState.Off, desktop.Power);
            Assert.IsFalse(desktop.SystemRoot.gameObject.activeSelf, "no desktop");
            Assert.IsTrue(screen.transform.Find("Blackout").gameObject.activeSelf);
            Assert.IsNull(desktop.Open(browser));
            Assert.IsFalse(desktop.IsOpen(browser));
        }

        [Test]
        public void SwitchingOn_ShowsTheBootScreen_ThenTheDesktop()
        {
            Build(startOn: false);
            var states = new List<PowerState>();
            desktop.PowerChanged += states.Add;

            desktop.PowerOn();
            Assert.AreEqual(PowerState.Booting, desktop.Power);
            Assert.IsTrue(screen.transform.Find("Boot").gameObject.activeSelf);
            Assert.IsFalse(screen.transform.Find("Blackout").gameObject.activeSelf);

            desktop.Tick(Desktop.BootSeconds * 0.5f);
            Assert.AreEqual(PowerState.Booting, desktop.Power);
            Assert.AreEqual(0.5f, screen.transform.Find("Boot/BarBack/BarFill").GetComponent<RectTransform>().anchorMax.x, 0.01f, "the bar fills as it boots");

            desktop.Tick(Desktop.BootSeconds);
            Assert.AreEqual(PowerState.On, desktop.Power);
            Assert.IsTrue(desktop.SystemRoot.gameObject.activeSelf);
            Assert.IsFalse(screen.transform.Find("Boot").gameObject.activeSelf);
            CollectionAssert.AreEqual(new[] { PowerState.Booting, PowerState.On }, states);
            Assert.IsNotNull(desktop.Open(browser), "programs open once it is on");
        }

        [Test]
        public void ClickingTheBlackScreen_SwitchesTheComputerOn()
        {
            Build(startOn: false);

            screen.transform.Find("Blackout").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(PowerState.Booting, desktop.Power);
        }

        [Test]
        public void TheBlackScreen_OnlyAsksToBeClickedWhileSomeoneSitsAtIt()
        {
            Build(startOn: false);
            var hint = screen.transform.Find("Blackout/Hint").gameObject;
            Assert.IsFalse(hint.activeSelf);
            StringAssert.Contains("Click to power on", hint.GetComponent<Text>().text);

            desktop.SetSeated(true);
            Assert.IsTrue(hint.activeSelf);
            desktop.SetSeated(false);
            Assert.IsFalse(hint.activeSelf);
        }

        [Test]
        public void TheStartMenu_ShutsTheComputerDown_AndClosesEveryWindow()
        {
            Tile("knowledge").onClick.Invoke();
            knowledge.Select(0);
            Tile("password").onClick.Invoke();
            Assert.IsTrue(GamePause.IsPaused);
            var menu = desktop.SystemRoot.Find("StartMenu").gameObject;
            Assert.IsFalse(menu.activeSelf);

            desktop.SystemRoot.Find("Taskbar/Bar/Start").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(menu.activeSelf, "the Start button opens the menu");
            StringAssert.Contains("Shut down", menu.GetComponentInChildren<Text>().text);

            menu.transform.Find("ShutDown").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(PowerState.ShuttingDown, desktop.Power);
            Assert.AreEqual(0, desktop.Windows.Count, "nothing stays open");
            Assert.IsFalse(GamePause.IsPaused, "the open document no longer pauses the game");
            Assert.IsFalse(menu.activeSelf);

            desktop.Tick(Desktop.ShutdownSeconds + 0.1f);
            Assert.AreEqual(PowerState.Off, desktop.Power);
            Assert.IsTrue(screen.transform.Find("Blackout").gameObject.activeSelf);
        }

        [Test]
        public void ClickingAwayFromTheStartMenu_ClosesIt()
        {
            var menu = desktop.SystemRoot.Find("StartMenu").gameObject;
            desktop.SystemRoot.Find("Taskbar/Bar/Start").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(menu.activeSelf);

            desktop.SystemRoot.Find("MenuBlocker").GetComponent<Button>().onClick.Invoke();

            Assert.IsFalse(menu.activeSelf);
            Assert.IsFalse(desktop.SystemRoot.Find("MenuBlocker").gameObject.activeSelf);
        }

        [Test]
        public void ShuttingDown_AComputerThatIsOff_DoesNothing()
        {
            Build(startOn: false);
            desktop.ShutDown();
            Assert.AreEqual(PowerState.Off, desktop.Power);
        }

        [Test]
        public void ARestart_GivesTheDesktopBack_WithEveryProgramStillThere()
        {
            desktop.ShutDown();
            desktop.Tick(Desktop.ShutdownSeconds + 0.1f);
            desktop.PowerOn();
            desktop.Tick(Desktop.BootSeconds + 0.1f);

            Assert.AreEqual(PowerState.On, desktop.Power);
            Assert.AreEqual(3, desktop.Apps.Count);
            Assert.IsNotNull(desktop.Open(password));
        }

        // ---------------------------------------------------------------- the browser

        [Test]
        public void TheBrowser_OpensOnTheStartPage_WithLinksToTheSites()
        {
            Tile("browser").onClick.Invoke();
            var content = desktop.WindowOf(browser).Content;

            Assert.AreEqual("start", browser.Current.id);
            Assert.AreEqual("start.local", content.Find("Toolbar/AddressBar/Address").GetComponent<Text>().text);
            Assert.IsNotNull(content.Find("Scroll/Viewport/Page/Tiles/Link_news_front"), "a tile for the news");
            Assert.AreEqual("Service Network", content.Find("Scroll/Viewport/Page/Heading").GetComponent<Text>().text);
            foreach (var link in browser.Current.links)
            {
                var tile = content.Find("Scroll/Viewport/Page/Tiles/Link_" + link.target);
                Assert.IsNotNull(tile, link.target);
                Assert.AreEqual(link.label, tile.Find("Label").GetComponent<Text>().text);
                Assert.AreEqual(link.badge, tile.Find("Badge/Letters").GetComponent<Text>().text);
                Assert.IsNotNull(tile.Find("Note"), link.target);
            }
            Assert.IsFalse(content.Find("Toolbar/Back").GetComponent<Button>().interactable, "nothing to go back to yet");
            Assert.IsFalse(browser.IsReading, "a start page is not something to read");
        }

        private Transform Page() => desktop.WindowOf(browser).Content.Find("Scroll/Viewport/Page");

        private Transform OpenNews()
        {
            Tile("browser").onClick.Invoke();
            Page().Find("Tiles/Link_news_front").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("news_front", browser.Current.id);
            return Page();
        }

        private static string TextOf(Transform parent, string path) => parent.Find(path).GetComponent<Text>().text;

        [Test]
        public void TheNewsSite_HasAMastheadAMenuAndAFrontPage()
        {
            var page = OpenNews();

            Assert.IsNotNull(page.Find("Masthead/LogoBox"), "the logo");
            Assert.AreEqual("AAC", TextOf(page, "Masthead/LogoBox/Name"));
            Assert.AreEqual("NEWS", TextOf(page, "Masthead/Word"));
            foreach (string target in new[] { "news_front", "news_world", "news_business", "news_technology", "news_climate", "news_sport", "news_culture", "news_science" })
            {
                Assert.IsNotNull(page.Find("Menu/Nav_" + target), target);
            }
            Assert.IsNull(page.Find("SectionTitle"), "the open tab already says which section this is");

            Assert.IsNotNull(page.Find("Front/Card_news_ghost"), "the lead story");
            foreach (string side in new[] { "news_law", "news_final", "news_festival" })
            {
                Assert.IsNotNull(page.Find("Front/Side/Card_" + side), side);
            }
            Assert.AreEqual(10, page.GetComponentsInChildren<Button>().Count(button => button.name.StartsWith("Card_")), "every story is a card");
            Assert.AreEqual(browser.Current.url, desktop.WindowOf(browser).Content.Find("Toolbar/AddressBar/Address").GetComponent<Text>().text);
            Assert.IsFalse(browser.IsReading, "the front page is not an article");
        }

        [Test]
        public void TheMenu_MarksTheOpenSection_AndOpensTheOthers()
        {
            var page = OpenNews();
            Assert.IsNotNull(page.Find("Menu/Nav_news_front/Underline"));
            Assert.IsNull(page.Find("Menu/Nav_news_world/Underline"));

            page.Find("Menu/Nav_news_world").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("news_world", browser.Current.id);
            Assert.IsNotNull(page.Find("Front/Card_news_law"), "its lead story");
            Assert.IsNotNull(page.Find("Menu/Nav_news_world/Underline"));
            Assert.IsNull(page.Find("Menu/Nav_news_front/Underline"));
        }

        [Test]
        public void ClickingAStory_OpensTheArticle_WithItsPartsAndPausesTheGame()
        {
            var page = OpenNews();

            page.Find("GridRow/Card_news_heat").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("news_heat", browser.Current.id);
            Assert.IsTrue(browser.IsReading);
            Assert.IsTrue(GamePause.IsPaused);

            var story = browser.Current;
            Assert.AreEqual(story.title, TextOf(page, "Article/Headline"));
            Assert.AreEqual(story.summary, TextOf(page, "Article/Summary"));
            StringAssert.Contains("CLIMATE", TextOf(page, "Article/Tag"));
            StringAssert.Contains(story.byline, TextOf(page, "Article/Byline"));
            StringAssert.Contains(story.date, TextOf(page, "Article/Byline"));
            Assert.IsNotNull(page.Find("Article/PhotoPlaceholder"), "no picture in the default theme: a placeholder block");
            Assert.AreEqual(story.caption, TextOf(page, "Article/Caption"));
            Assert.GreaterOrEqual(page.Find("Article").GetComponentsInChildren<Text>().Count(text => text.name.StartsWith("Paragraph_")), 3);
            Assert.IsNotNull(page.Find("Menu/Nav_news_climate/Underline"), "an article belongs to its section's tab");

            foreach (string related in story.related)
            {
                Assert.IsNotNull(page.Find("Article/Related/Card_" + related), related);
            }
            page.Find("Article/Related/Card_" + story.related[0]).GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(story.related[0], browser.Current.id, "a related story opens");

            browser.Back();
            Assert.AreEqual("news_heat", browser.Current.id);
            browser.Back();
            Assert.AreEqual("news_front", browser.Current.id);
            Assert.IsFalse(GamePause.IsPaused);
        }

        [Test]
        public void TheMenu_ClickedInsideAnArticle_LeavesTheArticle()
        {
            OpenNews();
            browser.Navigate("news_law");
            Assert.IsTrue(GamePause.IsPaused);

            Page().Find("Menu/Nav_news_business").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("news_business", browser.Current.id);
            Assert.IsFalse(GamePause.IsPaused);
        }

        [Test]
        public void ReadingAnArticle_PausesTheGame_AndGoingBackLetsItRun()
        {
            Tile("browser").onClick.Invoke();
            var content = desktop.WindowOf(browser).Content;
            browser.Navigate("news_front");
            Assert.IsFalse(GamePause.IsPaused, "headlines are not an article");

            bool? reading = null;
            browser.ReadingChanged += value => reading = value;
            browser.Navigate("news_law");
            Assert.IsTrue(browser.IsReading);
            Assert.IsTrue(GamePause.IsPaused);
            Assert.AreEqual(true, reading);

            content.Find("Toolbar/Back").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("news_front", browser.Current.id);
            Assert.IsFalse(GamePause.IsPaused);
            Assert.AreEqual(false, reading);
        }

        [Test]
        public void TheBackAndHomeButtons_WalkThroughTheHistory()
        {
            Tile("browser").onClick.Invoke();
            browser.Navigate("news_front");
            browser.Navigate("news_demand");
            browser.Navigate("news_front");
            Assert.IsTrue(browser.CanGoBack);

            browser.Back();
            Assert.AreEqual("news_demand", browser.Current.id);
            browser.Back();
            Assert.AreEqual("news_front", browser.Current.id);
            browser.Back();
            Assert.AreEqual("start", browser.Current.id);
            Assert.IsFalse(browser.CanGoBack);

            browser.Navigate("grid_status");
            desktop.WindowOf(browser).Content.Find("Toolbar/Home").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("start", browser.Current.id);
            browser.Back();
            Assert.AreEqual("grid_status", browser.Current.id, "Home is a step in the history too");
        }

        [Test]
        public void TheBrowser_ShowsAMessageForAPageThatDoesNotExist()
        {
            Tile("browser").onClick.Invoke();
            browser.Navigate("nowhere");

            Assert.AreEqual("Page not found.", desktop.WindowOf(browser).Content.Find("Scroll/Viewport/Page/Title").GetComponent<Text>().text);
            browser.Back();
            Assert.AreEqual("start", browser.Current.id);
        }

        [Test]
        public void LeavingTheComputer_OrClosingTheBrowser_LetsTheGameRun()
        {
            Tile("browser").onClick.Invoke();
            browser.Navigate("news_talks");
            Assert.IsTrue(GamePause.IsPaused);

            desktop.Suspend();
            Assert.IsFalse(GamePause.IsPaused);
            desktop.Resume();
            Assert.IsTrue(GamePause.IsPaused);

            desktop.WindowOf(browser).CloseButton.onClick.Invoke();
            Assert.IsFalse(GamePause.IsPaused);
            Assert.IsFalse(browser.IsReading);
        }

        // ---------------------------------------------------------------- the knowledge base

        [Test]
        public void TheKnowledgeBase_ListsTheDocuments_AndOpeningOnePausesTheGame()
        {
            Tile("knowledge").onClick.Invoke();
            var content = desktop.WindowOf(knowledge).Content;
            Assert.AreEqual(knowledge.Articles.Count, content.GetComponentsInChildren<Button>().Length, "one button per document");
            Assert.IsFalse(knowledge.IsReading);
            Assert.IsFalse(GamePause.IsPaused);

            bool? reading = null;
            knowledge.ReadingChanged += value => reading = value;
            content.Find("List/Entry_" + knowledge.Articles[0].id).GetComponent<Button>().onClick.Invoke();

            Assert.IsTrue(knowledge.IsReading);
            Assert.AreEqual(0, knowledge.SelectedIndex);
            Assert.IsTrue(GamePause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(true, reading);
            Assert.AreEqual(knowledge.Articles[0].title, content.Find("Heading").GetComponent<Text>().text);
            Assert.AreEqual(knowledge.Articles[0].text, content.Find("Scroll/Viewport/Text").GetComponent<Text>().text);

            content.Find("List/Entry_" + knowledge.Articles[1].id).GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(knowledge.Articles[1].text, content.Find("Scroll/Viewport/Text").GetComponent<Text>().text);
            Assert.IsTrue(GamePause.IsPaused);
        }

        [Test]
        public void ClosingTheKnowledgeBase_LetsTheGameRunAgain()
        {
            Tile("knowledge").onClick.Invoke();
            knowledge.Select(0);
            Assert.IsTrue(GamePause.IsPaused);

            desktop.WindowOf(knowledge).CloseButton.onClick.Invoke();

            Assert.IsFalse(knowledge.IsReading);
            Assert.IsFalse(GamePause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void LeavingTheComputer_LetsTheGameRun_AndComingBackPausesAgain()
        {
            Tile("knowledge").onClick.Invoke();
            knowledge.Select(0);

            desktop.Suspend();
            Assert.IsFalse(GamePause.IsPaused, "away from the computer the game runs");
            Assert.IsTrue(knowledge.IsReading, "the document stays open");

            desktop.Resume();
            Assert.IsTrue(GamePause.IsPaused, "back at the document");
        }

        [Test]
        public void AKnowledgeBaseWithNothingOpen_DoesNotPauseTheGame()
        {
            Tile("knowledge").onClick.Invoke();
            desktop.Suspend();
            desktop.Resume();
            Assert.IsFalse(GamePause.IsPaused);
        }

        // ---------------------------------------------------------------- the password minigame

        [Test]
        public void ThePasswordMinigame_ShowsTenWords_AndFourTries()
        {
            Tile("password").onClick.Invoke();
            var content = desktop.WindowOf(password).Content;

            Assert.AreEqual(PasswordPuzzle.DefaultCandidateCount, content.GetComponentsInChildren<Button>().Count(button => button.name.StartsWith("Word_")));
            StringAssert.Contains("4 / 4", content.Find("Tries").GetComponent<Text>().text);
            StringAssert.Contains("Attempts", content.Find("Tries").GetComponent<Text>().text);
            Assert.AreEqual("Access locked", content.Find("Heading").GetComponent<Text>().text);
            Assert.IsFalse(content.Find("Retry").gameObject.activeSelf);
        }

        [Test]
        public void PickingTheRightWord_WinsAndReportsIt()
        {
            Tile("password").onClick.Invoke();
            var content = desktop.WindowOf(password).Content;
            var results = new List<MinigameResult>();
            password.Completed += results.Add;

            string answer = SecretOfFirstPuzzle();
            Word(content, answer).onClick.Invoke();

            Assert.AreEqual(PasswordState.Won, password.Puzzle.State);
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].Success);
            Assert.AreEqual("password", results[0].MinigameId);
            Assert.AreEqual(1, results[0].Attempts);
            StringAssert.Contains("ACCESS GRANTED", content.Find("Status").GetComponent<Text>().text);
            Assert.IsTrue(content.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Word_")).All(button => !button.interactable), "no more picks after winning");
        }

        [Test]
        public void WrongWords_ShowTheLikeness_AndCostTries()
        {
            Tile("password").onClick.Invoke();
            var content = desktop.WindowOf(password).Content;
            string answer = SecretOfFirstPuzzle();
            string wrong = password.Puzzle.Candidates.First(word => word != answer);

            Word(content, wrong).onClick.Invoke();

            Assert.AreEqual(PasswordState.Playing, password.Puzzle.State);
            StringAssert.Contains("3 / 4", content.Find("Tries").GetComponent<Text>().text);
            string log = content.Find("Log/LogText").GetComponent<Text>().text;
            StringAssert.Contains(wrong, log);
            StringAssert.Contains($"match {PasswordPuzzle.Likeness(wrong, answer)} / {answer.Length}", log);
            Assert.IsFalse(Word(content, wrong).interactable, "a word is tried once");
        }

        [Test]
        public void FourWrongWords_LockTheTerminal_AndOfferANewPuzzle()
        {
            Tile("password").onClick.Invoke();
            var content = desktop.WindowOf(password).Content;
            var results = new List<MinigameResult>();
            password.Completed += results.Add;
            string answer = SecretOfFirstPuzzle();
            var firstCandidates = password.Puzzle.Candidates.ToList();

            foreach (string wrong in firstCandidates.Where(word => word != answer).Take(4))
            {
                Word(content, wrong).onClick.Invoke();
            }

            Assert.AreEqual(PasswordState.Lost, password.Puzzle.State);
            Assert.AreEqual(1, results.Count);
            Assert.IsFalse(results[0].Success);
            StringAssert.Contains("LOCKED OUT", content.Find("Status").GetComponent<Text>().text);
            StringAssert.Contains(answer, content.Find("Status").GetComponent<Text>().text, "the password is shown once it is over");
            Assert.IsTrue(content.Find("Retry").gameObject.activeSelf);

            content.Find("Retry").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(PasswordState.Playing, password.Puzzle.State);
            Assert.AreNotEqual(string.Join(",", firstCandidates), string.Join(",", password.Puzzle.Candidates), "a new puzzle");
            Assert.AreEqual(PasswordPuzzle.DefaultTries, password.Puzzle.TriesLeft);
            Assert.IsTrue(content.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Word_")).All(button => button.interactable));
        }

        [Test]
        public void ClosingAndReopeningThePassword_KeepsThePuzzle()
        {
            Tile("password").onClick.Invoke();
            string answer = SecretOfFirstPuzzle();
            string wrong = password.Puzzle.Candidates.First(word => word != answer);
            password.Pick(wrong);
            var puzzle = password.Puzzle;

            desktop.WindowOf(password).CloseButton.onClick.Invoke();
            Tile("password").onClick.Invoke();

            Assert.AreSame(puzzle, password.Puzzle);
            var content = desktop.WindowOf(password).Content;
            StringAssert.Contains("3 / 4", content.Find("Tries").GetComponent<Text>().text);
            Assert.IsFalse(Word(content, wrong).interactable);
        }

        [Test]
        public void WithoutAWordList_ThePasswordShowsAMessageInsteadOfBreaking()
        {
            var empty = new PasswordApp(new List<string>());
            desktop.AddApp(empty);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no usable word list"));
            var window = desktop.Open(empty);

            Assert.IsNotNull(window.Content.Find("Missing"));
            Assert.IsNull(empty.Puzzle);
        }

        private Button Tile(string app) => desktop.SystemRoot.Find($"Icons/Icon_{app}/Tile").GetComponent<Button>();

        private static Button Word(RectTransform content, string word) => content.Find("Word_" + word).GetComponent<Button>();

        // The password of the first puzzle the app makes (seed 100), found on a copy so the app's own puzzle is untouched.
        private string SecretOfFirstPuzzle()
        {
            var copy = PasswordPuzzle.Create(words.ToList(), 100);
            return copy.Candidates.Single(word => PasswordPuzzle.Create(words.ToList(), 100).Guess(word).IsCorrect);
        }
    }
}
