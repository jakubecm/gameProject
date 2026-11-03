using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Look of a <see cref="UiKit.MenuButton"/>: the label turns cyan while the button is selected. Hovering with the mouse selects it too, so
    /// the keyboard, the gamepad and the mouse share one highlight.
    /// </summary>
    internal class MenuButtonVisual : MonoBehaviour, IPointerEnterHandler
    {
        private Text text;

        public void Init(Text label)
        {
            text = label;
            text.color = UiKit.Bright;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void Update()
        {
            bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            text.color = selected ? UiKit.Accent : UiKit.Bright;
        }
    }
}
