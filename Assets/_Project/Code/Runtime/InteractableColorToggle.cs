using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>Test helper for the sandbox: switches the object's color each time it is used.</summary>
    [RequireComponent(typeof(Interactable), typeof(Renderer))]
    public class InteractableColorToggle : MonoBehaviour
    {
        [SerializeField] private Color colorA = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color colorB = new Color(0.2f, 0.9f, 0.3f);

        private Interactable interactable;
        private Renderer targetRenderer;
        private bool useB;

        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            targetRenderer = GetComponent<Renderer>();
            targetRenderer.material.color = colorA;
        }

        private void OnEnable() => interactable.Interacted += OnInteracted;

        private void OnDisable() => interactable.Interacted -= OnInteracted;

        private void OnInteracted(Interactor interactor)
        {
            useB = !useB;
            targetRenderer.material.color = useB ? colorB : colorA;
        }
    }
}
