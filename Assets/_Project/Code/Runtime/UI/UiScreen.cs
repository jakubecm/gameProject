using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// One full-screen UI page (main menu, pause, settings, credits, intro text). It is made by <see cref="Create{T}"/> under a canvas, builds
    /// its own widgets, and fades in and out on unscaled time (so it also works while the game is paused). Shown, it selects its first
    /// button so the keyboard and the gamepad work without the mouse.
    /// </summary>
    public abstract class UiScreen : MonoBehaviour
    {
        private const float FadeSpeed = 6f;

        private CanvasGroup group;
        private float target;
        private Selectable first;
        private bool arranged;

        public bool IsOpen { get; private set; }

        internal static T Create<T>(Transform parent) where T : UiScreen
        {
            var rect = UiKit.NewRect(typeof(T).Name, parent);
            UiKit.Stretch(rect);
            UiScreen screen = rect.gameObject.AddComponent<T>();
            screen.group = rect.gameObject.AddComponent<CanvasGroup>();
            screen.group.alpha = 0f;
            screen.group.interactable = false;
            screen.group.blocksRaycasts = false;
            screen.Build(rect);
            rect.gameObject.SetActive(false);
            return (T)screen;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (!arranged)
            {
                // The text is measured as it is drawn, so the canvas has to be built first.
                arranged = true;
                Canvas.ForceUpdateCanvases();
                Arrange();
            }
            IsOpen = true;
            target = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            OnShown();
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        /// <summary>Fades the screen out.</summary>
        public void Hide()
        {
            IsOpen = false;
            target = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        /// <summary>Hides the screen at once, without the fade.</summary>
        public void HideNow()
        {
            Hide();
            group.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>The widgets are created here, under <paramref name="root"/> (stretched over the canvas).</summary>
        protected abstract void Build(RectTransform root);

        /// <summary>Places the widgets by their measured text (<see cref="UiKit.Ink"/>); called once, when the screen is first shown.</summary>
        protected virtual void Arrange() { }

        protected virtual void OnShown() { }

        /// <summary>Called every frame the screen is active, on unscaled time.</summary>
        protected virtual void Tick() { }

        /// <summary>The button the keyboard and the gamepad start on when the screen is shown.</summary>
        protected void SetFirstSelected(Selectable selectable) => first = selectable;

        private void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * FadeSpeed);
            if (target <= 0f && group.alpha <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            Tick();
        }
    }
}
