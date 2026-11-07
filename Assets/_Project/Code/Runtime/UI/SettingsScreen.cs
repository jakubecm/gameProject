using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The settings page, opened from the main menu and from the pause screen: a volume slider and a mute switch, centred. They move and show
    /// their values, but nothing is connected to the game's audio yet. The rows are one under the other with the gaps of the other screens
    /// (title gap under the title, item gap between the rows), each centred horizontally; MUTE and its box are centred as one element.
    /// </summary>
    public class SettingsScreen : UiScreen
    {
        private const float MuteGap = 24f;
        private const float BoxSize = 38f;
        private const float BoxOutline = 2f;

        private Text title;
        private Text volumeLabel;
        private RectTransform volumeSlider;
        private RectTransform muteRow;
        private Text muteLabel;
        private RectTransform muteBox;
        private Button back;

        public event Action BackClicked;

        protected override void Build(RectTransform root)
        {
            var dim = UiKit.Box(root, "Dim", new Color(0f, 0.01f, 0.015f, 0.85f));
            UiKit.Stretch(dim.rectTransform);

            var centre = new Vector2(0.5f, 0.5f);
            title = UiKit.Label(root, "Title", "SETTINGS", 80, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, centre, centre, Vector2.zero, new Vector2(900f, 130f));

            var label = UiKit.Label(root, "Volume Label", "VOLUME  80", 34, UiKit.Bright, TextAnchor.MiddleCenter);
            UiKit.Place(label.rectTransform, centre, centre, Vector2.zero, new Vector2(900f, 56f));
            volumeLabel = label;
            var volume = UiKit.NewSlider(root, 0f, 100f, 80f, true);
            volumeSlider = (RectTransform)volume.transform;
            UiKit.Place(volumeSlider, centre, centre, Vector2.zero, new Vector2(600f, 28f));
            volume.onValueChanged.AddListener(value => volumeLabel.text = "VOLUME  " + value.ToString("0"));

            muteRow = UiKit.NewRect("Mute Row", root);
            muteLabel = UiKit.Label(muteRow, "Mute Label", "MUTE", 34, UiKit.Bright, TextAnchor.MiddleCenter);
            UiKit.Place(muteLabel.rectTransform, centre, centre, Vector2.zero, new Vector2(200f, 44f));
            var mute = UiKit.NewToggle(muteRow, false);
            muteBox = (RectTransform)mute.transform;
            UiKit.Place(muteBox, centre, centre, Vector2.zero, new Vector2(BoxSize + 2f * BoxOutline, BoxSize + 2f * BoxOutline));

            back = UiKit.MenuButton(root, "BACK", () => BackClicked?.Invoke(), 40, 72f, 360f, TextAnchor.MiddleCenter);
            SetFirstSelected(volume);
        }

        protected override void Arrange()
        {
            // MUTE and its box are one element: centred together, the box (with its outline) after the label's ink.
            var muteInk = UiKit.Ink(muteLabel);
            float boxWidth = BoxSize + 2f * BoxOutline;
            float width = muteInk.width + MuteGap + boxWidth;
            float height = Mathf.Max(muteInk.height, boxWidth);
            var muteRowRect = muteRow;
            UiKit.Place(muteRowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            muteLabel.rectTransform.anchoredPosition = new Vector2(-width / 2f + muteInk.width / 2f - muteInk.center.x, -muteInk.center.y);
            muteBox.anchoredPosition = new Vector2(width / 2f - boxWidth / 2f, 0f);

            // The slider's ink: its track and handle (22 px), the width of its rect.
            var sliderInk = Rect.MinMaxRect(-300f, -11f, 300f, 11f);
            UiKit.Stack(0f,
                new UiKit.StackRow(title.rectTransform, UiKit.Ink(title)),
                new UiKit.StackRow(volumeLabel.rectTransform, UiKit.Ink(volumeLabel), UiKit.TitleGap),
                new UiKit.StackRow(volumeSlider, sliderInk, UiKit.ItemGap),
                new UiKit.StackRow(muteRowRect, Rect.MinMaxRect(-width / 2f, -height / 2f, width / 2f, height / 2f), UiKit.ItemGap),
                new UiKit.StackRow((RectTransform)back.transform, UiKit.ButtonInk(back), UiKit.ItemGap));
        }
    }
}
