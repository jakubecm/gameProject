using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>The welcome screen over the flying camera: the glitching title and PLAY / SETTINGS / EXIT below it, as one block whose ink is centred on the screen, both ways.</summary>
    public class MainMenuScreen : UiScreen
    {
        // Gaps between the ink edges of the rows.
        private const float LineGap = 30f;

        private Text cold;
        private Text shutdown;
        private Button play;
        private Button settings;
        private Button exit;
        private TitleGlitch glitch;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action ExitClicked;

        protected override void Build(RectTransform root)
        {
            var dim = UiKit.Box(root, "Dim", new Color(0f, 0.01f, 0.02f, 0.5f));
            UiKit.Stretch(dim.rectTransform);

            var centre = new Vector2(0.5f, 0.5f);
            var title = UiKit.NewRect("Title", root);
            UiKit.Stretch(title);
            cold = UiKit.Label(title, "Title Cold", "COLD", 150, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(cold.rectTransform, centre, centre, Vector2.zero, new Vector2(1400f, 170f));
            shutdown = UiKit.Label(title, "Title Shutdown", "SHUTDOWN", 150, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(shutdown.rectTransform, centre, centre, Vector2.zero, new Vector2(1400f, 170f));
            glitch = title.gameObject.AddComponent<TitleGlitch>();
            glitch.Init(cold, shutdown);

            play = UiKit.MenuButton(root, "PLAY", () => PlayClicked?.Invoke(), 44, 68f, 520f, TextAnchor.MiddleCenter);
            settings = UiKit.MenuButton(root, "SETTINGS", () => SettingsClicked?.Invoke(), 44, 68f, 520f, TextAnchor.MiddleCenter);
            exit = UiKit.MenuButton(root, "EXIT", () => ExitClicked?.Invoke(), 44, 68f, 520f, TextAnchor.MiddleCenter);
            SetFirstSelected(play);
        }

        protected override void Arrange()
        {
            UiKit.Stack(0f,
                new UiKit.StackRow(cold.rectTransform, UiKit.Ink(cold)),
                new UiKit.StackRow(shutdown.rectTransform, UiKit.Ink(shutdown), LineGap),
                new UiKit.StackRow((RectTransform)play.transform, UiKit.ButtonInk(play), UiKit.TitleGap),
                new UiKit.StackRow((RectTransform)settings.transform, UiKit.ButtonInk(settings), UiKit.ItemGap),
                new UiKit.StackRow((RectTransform)exit.transform, UiKit.ButtonInk(exit), UiKit.ItemGap));
            glitch.Rehome();
        }
    }
}
