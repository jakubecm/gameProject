using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Lives on the player camera. Every frame it casts a ray from the camera center, remembers which
    /// <see cref="IInteractable"/> is being looked at, and triggers it on a key press, or after the
    /// key has been held long enough for objects with a hold duration.
    /// Other systems (prompt UI, sounds) listen to the events instead of referencing each other.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float maxDistance = 2.5f;
        [SerializeField] private LayerMask layerMask = ~0;

        private InputAction interactAction;
        private IInteractable focused;
        private bool holding;
        private float holdTime;

        /// <summary>Raised when the looked-at interactable changes. The argument is null when nothing is in reach.</summary>
        public event Action<IInteractable> FocusChanged;

        /// <summary>Raised while a key is held on a hold-to-use object, with progress from 0 to 1. Back to 0 when cancelled.</summary>
        public event Action<float> HoldProgressChanged;

        /// <summary>Raised after an interactable has been used.</summary>
        public event Action<IInteractable> Interacted;

        public IInteractable Focused => focused;

        /// <summary>Display name of the interact key, for on-screen hints (for example "E").</summary>
        public string KeyLabel => interactAction.GetBindingDisplayString();

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponent<Camera>();
            }
            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        }

        private void OnEnable() => interactAction.Enable();

        private void OnDisable()
        {
            interactAction.Disable();
            CancelHold();
            SetFocus(null);
        }

        private void OnDestroy() => interactAction.Dispose();

        private void Update()
        {
            if (viewCamera == null) return;

            SetFocus(FindTarget());
            if (focused == null) return;

            if (focused.HoldDuration <= 0f)
            {
                if (interactAction.WasPressedThisFrame()) Use();
                return;
            }

            // A hold has to start with a fresh press while looking at the object,
            // so holding the key while sweeping the view over it does nothing.
            if (interactAction.WasPressedThisFrame())
            {
                holding = true;
                holdTime = 0f;
            }

            if (!holding) return;

            if (!interactAction.IsPressed())
            {
                CancelHold();
                return;
            }

            holdTime += Time.deltaTime;
            HoldProgressChanged?.Invoke(Mathf.Clamp01(holdTime / focused.HoldDuration));

            if (holdTime >= focused.HoldDuration)
            {
                CancelHold();
                Use();
            }
        }

        private IInteractable FindTarget()
        {
            var cameraTransform = viewCamera.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);

            // Everything blocks the ray, so a wall between the player and a terminal hides it.
            if (Physics.Raycast(ray, out var hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<IInteractable>();
            }
            return null;
        }

        private void SetFocus(IInteractable target)
        {
            if (target == focused) return;

            CancelHold();
            focused = target;
            FocusChanged?.Invoke(focused);
        }

        private void CancelHold()
        {
            if (!holding) return;

            holding = false;
            holdTime = 0f;
            HoldProgressChanged?.Invoke(0f);
        }

        private void Use()
        {
            var used = focused;
            used.Interact(this);
            Interacted?.Invoke(used);
        }
    }
}
