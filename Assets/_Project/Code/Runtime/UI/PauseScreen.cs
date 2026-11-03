using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>The pause screen (Escape): the game dimmed, PAUSED and RESUME / SETTINGS / EXIT in the middle.</summary>
    public class PauseScreen : UiScreen
    {
        public event Action ResumeClicked;
        public event Action SettingsClicked;
        public event Action ExitClicked;

        protected override void Build(RectTransform root)
        {
            var dim = UiKit.Box(root, "Dim", new Color(0f, 0.01f, 0.015f, 0.8f));
            UiKit.Stretch(dim.rectTransform);

            var column = UiKit.NewRect("Column", root);
            UiKit.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 600f));
            UiKit.Column(column, 8f, TextAnchor.MiddleCenter, true);

            var title = UiKit.Label(column, "Title", "PAUSED", 90, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Size(title, 700f, 130f);
            var resume = UiKit.MenuButton(column, "RESUME", () => ResumeClicked?.Invoke(), 40, 72f, 460f, TextAnchor.MiddleCenter);
            UiKit.MenuButton(column, "SETTINGS", () => SettingsClicked?.Invoke(), 40, 72f, 460f, TextAnchor.MiddleCenter);
            UiKit.MenuButton(column, "EXIT", () => ExitClicked?.Invoke(), 40, 72f, 460f, TextAnchor.MiddleCenter);
            SetFirstSelected(resume);
        }
    }
}
