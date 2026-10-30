using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Minimal on-screen feedback for the <see cref="Interactor"/>: a crosshair dot, the key hint with
    /// the prompt text, and a progress bar for hold-to-use objects. It builds its own canvas so it
    /// works without any UI setup; the final look is the UI owner's job and can replace this.
    /// </summary>
    [RequireComponent(typeof(Interactor))]
    public class InteractionPrompt : MonoBehaviour
    {
        private Interactor interactor;
        private Text label;
        private GameObject barRoot;
        private RectTransform barFill;

        private void Awake()
        {
            interactor = GetComponent<Interactor>();
            BuildUi();
            ShowFocus(null);
        }

        private void OnEnable()
        {
            interactor.FocusChanged += ShowFocus;
            interactor.Interacted += Refresh;
            interactor.HoldProgressChanged += ShowProgress;
        }

        private void OnDisable()
        {
            interactor.FocusChanged -= ShowFocus;
            interactor.Interacted -= Refresh;
            interactor.HoldProgressChanged -= ShowProgress;
        }

        /// <summary>The prompt of an object can change when it is used (a rack door: open / close).</summary>
        private void Refresh(IInteractable used) => ShowFocus(interactor.Focused);

        private void ShowFocus(IInteractable target)
        {
            bool hasTarget = target != null;
            label.gameObject.SetActive(hasTarget);
            barRoot.SetActive(hasTarget && target.HoldDuration > 0f);
            ShowProgress(0f);

            if (!hasTarget) return;

            string key = $"[{interactor.KeyLabelFor(target)}]";
            label.text = string.IsNullOrEmpty(target.Prompt) ? key : $"{key} {target.Prompt}";
        }

        private void ShowProgress(float progress)
        {
            barFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("InteractionPromptCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var dot = CreateImage("Crosshair", canvasObject.transform, new Color(1f, 1f, 1f, 0.6f));
            Place(dot.rectTransform, Vector2.zero, new Vector2(6f, 6f));

            var labelObject = new GameObject("Label", typeof(Text));
            labelObject.transform.SetParent(canvasObject.transform, false);
            label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            Place(label.rectTransform, new Vector2(0f, -60f), new Vector2(800f, 44f));

            var bar = CreateImage("HoldBar", canvasObject.transform, new Color(0f, 0f, 0f, 0.6f));
            Place(bar.rectTransform, new Vector2(0f, -100f), new Vector2(240f, 10f));
            barRoot = bar.gameObject;

            var fill = CreateImage("Fill", bar.transform, Color.white);
            barFill = fill.rectTransform;
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = new Vector2(0f, 1f);
            barFill.offsetMin = Vector2.zero;
            barFill.offsetMax = Vector2.zero;
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Anchors a rect to the screen center with the given offset and size.
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
