using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>The pause screen (Escape): the game dimmed, PAUSED and RESUME / SETTINGS / EXIT in the middle (every row centred by its ink).</summary>
    public class PauseScreen : UiScreen
    {
        private Text title;
        private Button resume;
        private Button settings;
        private Button exit;

        public event Action ResumeClicked;
        public event Action SettingsClicked;
        public event Action ExitClicked;

        protected override void Build(RectTransform root)
        {
            var dim = UiKit.Box(root, "Dim", new Color(0f, 0.01f, 0.015f, 0.8f));
            UiKit.Stretch(dim.rectTransform);

            var centre = new Vector2(0.5f, 0.5f);
            title = UiKit.Label(root, "Title", "PAUSED", 90, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, centre, centre, Vector2.zero, new Vector2(700f, 130f));
            resume = UiKit.MenuButton(root, "RESUME", () => ResumeClicked?.Invoke(), 40, 72f, 460f, TextAnchor.MiddleCenter);
            settings = UiKit.MenuButton(root, "SETTINGS", () => SettingsClicked?.Invoke(), 40, 72f, 460f, TextAnchor.MiddleCenter);
            exit = UiKit.MenuButton(root, "EXIT", () => ExitClicked?.Invoke(), 40, 72f, 460f, TextAnchor.MiddleCenter);
            SetFirstSelected(resume);
        }

        protected override void Arrange()
        {
            UiKit.Stack(0f,
                new UiKit.StackRow(title.rectTransform, UiKit.Ink(title)),
                new UiKit.StackRow((RectTransform)resume.transform, UiKit.ButtonInk(resume), UiKit.TitleGap),
                new UiKit.StackRow((RectTransform)settings.transform, UiKit.ButtonInk(settings), UiKit.ItemGap),
                new UiKit.StackRow((RectTransform)exit.transform, UiKit.ButtonInk(exit), UiKit.ItemGap));
        }
    }
}
