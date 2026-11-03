using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The settings page, opened from the main menu and from the pause screen: a volume slider and a mute switch, centred. They move and show
    /// their values, but nothing is connected to the game's audio yet.
    /// </summary>
    public class SettingsScreen : UiScreen
    {
        public event Action BackClicked;

        protected override void Build(RectTransform root)
        {
            var dim = UiKit.Box(root, "Dim", new Color(0f, 0.01f, 0.015f, 0.85f));
            UiKit.Stretch(dim.rectTransform);

            var column = UiKit.NewRect("Column", root);
            UiKit.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 700f));
            UiKit.Column(column, 14f, TextAnchor.MiddleCenter, true);

            var title = UiKit.Label(column, "Title", "SETTINGS", 80, UiKit.Bright, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Size(title, -1f, 130f);

            var volumeLabel = UiKit.Label(column, "Volume Label", "VOLUME  80", 34, UiKit.Bright, TextAnchor.MiddleCenter);
            UiKit.Size(volumeLabel, -1f, 56f);
            var volumeSlot = UiKit.Slot(column, "Volume Slot", 50f);
            var volume = UiKit.NewSlider(volumeSlot, 0f, 100f, 80f, true);
            var centre = new Vector2(0.5f, 0.5f);
            UiKit.Place((RectTransform)volume.transform, centre, centre, Vector2.zero, new Vector2(600f, 44f));
            volume.onValueChanged.AddListener(value => volumeLabel.text = "VOLUME  " + value.ToString("0"));

            // MUTE and its box, as one group in the middle of the row.
            var muteSlot = UiKit.Slot(column, "Mute Slot", 70f);
            UiKit.Row(muteSlot, 24f, TextAnchor.MiddleCenter);
            var muteLabel = UiKit.Label(muteSlot, "Mute Label", "MUTE", 34, UiKit.Bright, TextAnchor.MiddleRight);
            UiKit.Size(muteLabel, 150f, 44f);
            var mute = UiKit.NewToggle(muteSlot, false);
            UiKit.Size(mute, 44f, 44f);

            UiKit.MenuButton(column, "BACK", () => BackClicked?.Invoke(), 40, 72f, 360f, TextAnchor.MiddleCenter);
            SetFirstSelected(volume);
        }
    }
}
