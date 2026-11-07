using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>How a clue minigame ended; the clue system (task 2.2) will listen to this.</summary>
    public readonly struct MinigameResult
    {
        public readonly string MinigameId;
        public readonly bool Success;

        /// <summary>Guesses made, wrong and right.</summary>
        public readonly int Attempts;

        public MinigameResult(string minigameId, bool success, int attempts)
        {
            MinigameId = minigameId;
            Success = success;
            Attempts = attempts;
        }
    }

    /// <summary>
    /// The Password minigame (task 2.5) on the desktop: pick the password from a list of words, the answers say how
    /// many letters are on the right place, four wrong picks lock it. The rules are in <see cref="PasswordPuzzle"/>
    /// (Core); this class is the window. The puzzle stays as it is when the window is closed and opened again; only a
    /// locked terminal offers a new one.
    /// </summary>
    public sealed class PasswordApp : ITerminalApp
    {
        private readonly IReadOnlyList<string> words;
        private readonly Func<int> seedSource;
        private readonly Dictionary<string, Button> wordButtons = new Dictionary<string, Button>();

        private DesktopTheme theme;
        private RectTransform content;
        private Text triesText;
        private Text logText;
        private Text statusText;
        private Button retryButton;

        public PasswordApp(IReadOnlyList<string> words, Func<int> seedSource = null)
        {
            this.words = words ?? new List<string>();
            this.seedSource = seedSource ?? (() => Environment.TickCount);
        }

        public string Id => "password";

        public string Title => DesktopTexts.PasswordTitle;

        public string IconLetter => "Z";

        public Color IconColor => new Color(0.76f, 0.22f, 0.22f);

        public Vector2 WindowSize => new Vector2(780f, 470f);

        public PasswordPuzzle Puzzle { get; private set; }

        /// <summary>Raised once when the puzzle ends, won or locked.</summary>
        public event Action<MinigameResult> Completed;

        public void Build(RectTransform content, DesktopTheme theme)
        {
            this.theme = theme;
            this.content = content;
            wordButtons.Clear();

            if (Puzzle == null && !StartNewPuzzle())
            {
                var missing = UiFactory.Text("Missing", content, DesktopTexts.PasswordNoWords, theme.bodySize, theme.danger, TextAnchor.UpperLeft);
                UiFactory.Place(missing.rectTransform, UiFactory.TopLeft, new Vector2(24f, -24f), new Vector2(600f, 40f));
                return;
            }

            BuildInterface();
            Refresh();
        }

        public void Closed()
        {
        }

        public void Suspended()
        {
        }

        public void Resumed()
        {
        }

        /// <summary>Picks a word, as a click on it does.</summary>
        public void Pick(string word)
        {
            if (Puzzle == null || Puzzle.State != PasswordState.Playing || Puzzle.HasBeenTried(word)) return;

            Puzzle.Guess(word);
            Refresh();

            if (Puzzle.State != PasswordState.Playing)
            {
                Completed?.Invoke(new MinigameResult(Id, Puzzle.State == PasswordState.Won, Puzzle.Guesses.Count));
            }
        }

        /// <summary>Starts again after the terminal locked.</summary>
        public void Retry()
        {
            if (Puzzle == null || Puzzle.State != PasswordState.Lost) return;

            Puzzle = null;
            if (content == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                UiFactory.Dispose(content.GetChild(i).gameObject);
            }
            wordButtons.Clear();
            if (StartNewPuzzle())
            {
                BuildInterface();
                Refresh();
            }
        }

        private bool StartNewPuzzle()
        {
            try
            {
                Puzzle = PasswordPuzzle.Create(words.ToList(), seedSource());
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"The password minigame has no usable word list: {exception.Message}");
                Puzzle = null;
                return false;
            }
        }

        private void BuildInterface()
        {
            var heading = UiFactory.Text("Heading", content, DesktopTexts.PasswordHeading, theme.headingSize, theme.bodyText, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(heading.rectTransform, UiFactory.TopLeft, new Vector2(24f, -16f), new Vector2(460f, 36f));

            triesText = UiFactory.Text("Tries", content, "", theme.bodySize, theme.bodyText, TextAnchor.UpperRight, FontStyle.Bold);
            UiFactory.Place(triesText.rectTransform, UiFactory.TopRight, new Vector2(-24f, -20f), new Vector2(240f, 30f));

            var instructions = UiFactory.Text("Instructions", content, DesktopTexts.PasswordInstructions, theme.smallSize + 1, theme.mutedText, TextAnchor.UpperLeft);
            UiFactory.Place(instructions.rectTransform, UiFactory.TopLeft, new Vector2(24f, -58f), new Vector2(730f, 40f));

            // The candidate words in two columns.
            for (int i = 0; i < Puzzle.Candidates.Count; i++)
            {
                string word = Puzzle.Candidates[i];
                var button = UiFactory.Button("Word_" + word, content, word, theme, theme.bodySize + 2);
                int column = i % 2;
                int row = i / 2;
                UiFactory.Place((RectTransform)button.transform, UiFactory.TopLeft, new Vector2(24f + column * 176f, -108f - row * 48f), new Vector2(168f, 40f));
                button.onClick.AddListener(() => Pick(word));
                wordButtons[word] = button;
            }

            var logPanel = UiFactory.Image("Log", content, theme.panel);
            UiFactory.Place(logPanel.rectTransform, UiFactory.TopLeft, new Vector2(396f, -108f), new Vector2(358f, 236f));
            var logHeading = UiFactory.Text("LogHeading", logPanel.transform, DesktopTexts.PasswordLog, theme.smallSize + 1, theme.mutedText, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(logHeading.rectTransform, UiFactory.TopLeft, new Vector2(14f, -10f), new Vector2(330f, 22f));
            logText = UiFactory.Text("LogText", logPanel.transform, "", theme.bodySize, theme.bodyText, TextAnchor.UpperLeft);
            UiFactory.Place(logText.rectTransform, UiFactory.TopLeft, new Vector2(14f, -38f), new Vector2(330f, 190f));

            statusText = UiFactory.Text("Status", content, "", theme.headingSize, theme.success, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(statusText.rectTransform, UiFactory.BottomLeft, new Vector2(24f, 16f), new Vector2(520f, 44f));

            retryButton = UiFactory.Button("Retry", content, DesktopTexts.PasswordRetry, theme, theme.bodySize);
            UiFactory.Place((RectTransform)retryButton.transform, UiFactory.BottomRight, new Vector2(-24f, 18f), new Vector2(190f, 42f));
            retryButton.onClick.AddListener(Retry);
        }

        private void Refresh()
        {
            if (Puzzle == null || triesText == null) return;

            triesText.text = $"{DesktopTexts.PasswordTries}: {Puzzle.TriesLeft} / {Puzzle.MaxTries}";
            logText.text = string.Join("\n", Puzzle.Guesses.Select(guess => $"{guess.Word}    {DesktopTexts.PasswordMatch} {guess.Likeness} / {guess.Length}"));

            bool over = Puzzle.State != PasswordState.Playing;
            foreach (var pair in wordButtons)
            {
                pair.Value.interactable = !over && !Puzzle.HasBeenTried(pair.Key);
            }

            retryButton.gameObject.SetActive(Puzzle.State == PasswordState.Lost);
            switch (Puzzle.State)
            {
                case PasswordState.Won:
                    statusText.text = DesktopTexts.PasswordWon;
                    statusText.color = theme.success;
                    break;
                case PasswordState.Lost:
                    statusText.text = $"{DesktopTexts.PasswordLost}   ({DesktopTexts.PasswordWasLabel} {Puzzle.Secret})";
                    statusText.color = theme.danger;
                    break;
                default:
                    statusText.text = "";
                    break;
            }
        }
    }
}
