using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>The welcome screen over the flying camera: the glitching title in the middle of the screen and PLAY / SETTINGS / EXIT below it.</summary>
    public class MainMenuScreen : UiScreen
    {
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
            var cold = UiKit.Label(title, "Title Cold", "COLD", 150, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(cold.rectTransform, centre, centre, new Vector2(0f, 250f), new Vector2(1400f, 170f));
            var shutdown = UiKit.Label(title, "Title Shutdown", "SHUTDOWN", 150, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(shutdown.rectTransform, centre, centre, new Vector2(0f, 110f), new Vector2(1400f, 170f));
            title.gameObject.AddComponent<TitleGlitch>().Init(cold, shutdown);

            var buttons = UiKit.NewRect("Buttons", root);
            UiKit.Place(buttons, centre, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(520f, 300f));
            UiKit.Column(buttons, 4f, TextAnchor.UpperCenter, true);
            var play = UiKit.MenuButton(buttons, "PLAY", () => PlayClicked?.Invoke(), 44, 68f, 520f, TextAnchor.MiddleCenter);
            UiKit.MenuButton(buttons, "SETTINGS", () => SettingsClicked?.Invoke(), 44, 68f, 520f, TextAnchor.MiddleCenter);
            UiKit.MenuButton(buttons, "EXIT", () => ExitClicked?.Invoke(), 44, 68f, 520f, TextAnchor.MiddleCenter);
            SetFirstSelected(play);
        }
    }
}
