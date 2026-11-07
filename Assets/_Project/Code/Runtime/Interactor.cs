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
        private InputAction secondaryAction;
        private IInteractable focused;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private bool holding;
        private float holdTime;

        /// <summary>Raised when the looked-at interactable changes. The argument is null when nothing is in reach.</summary>
        public event Action<IInteractable> FocusChanged;

        /// <summary>Raised while a key is held on a hold-to-use object, with progress from 0 to 1. Back to 0 when cancelled.</summary>
        public event Action<float> HoldProgressChanged;

        /// <summary>Raised after an interactable has been used.</summary>
        public event Action<IInteractable> Interacted;

        public IInteractable Focused => focused;

        /// <summary>The collider the view ray meets first (not the player's own), interactable or not; for the debug menu. Null when it meets nothing.</summary>
        public Collider LookedAt { get; private set; }

        public float LookedDistance { get; private set; }

        /// <summary>Display name of the interact key, for on-screen hints (for example "E").</summary>
        public string KeyLabel => interactAction.GetBindingDisplayString();

        /// <summary>Display name of the key that uses <paramref name="target"/> (E, or F for a rack door).</summary>
        public string KeyLabelFor(IInteractable target) => ActionFor(target).GetBindingDisplayString();

        private InputAction ActionFor(IInteractable target) => target != null && target.Key == InteractKey.Secondary ? secondaryAction : interactAction;

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponent<Camera>();
            }
            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            secondaryAction = new InputAction("InteractSecondary", InputActionType.Button, "<Keyboard>/f");
        }

        private void OnEnable()
        {
            interactAction.Enable();
            secondaryAction.Enable();
        }

        private void OnDisable()
        {
            interactAction.Disable();
            secondaryAction.Disable();
            CancelHold();
            SetFocus(null);
        }

        private void OnDestroy()
        {
            interactAction.Dispose();
            secondaryAction.Dispose();
        }

        private void Update()
        {
            if (viewCamera == null) return;

            SetFocus(FindTarget());
            if (focused == null) return;

            var action = ActionFor(focused);
            if (focused.HoldDuration <= 0f)
            {
                if (action.WasPressedThisFrame()) Use();
                return;
            }

            // A hold has to start with a fresh press while looking at the object,
            // so holding the key while sweeping the view over it does nothing.
            if (action.WasPressedThisFrame())
            {
                holding = true;
                holdTime = 0f;
            }

            if (!holding) return;

            if (!action.IsPressed())
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

            // Everything blocks the ray, so a wall between the player and a terminal hides it. If the thin ray finds nothing to use, a fat one
            // (3 cm) is tried, so the edges and gaps between the parts of a small object (a coffee machine) do not let the crosshair slip off it.
            LookedAt = null;
            return FromHit(ray, 0f) ?? FromHit(ray, 0.03f);
        }

        // The nearest thing the ray (or fat ray) meets, not counting the player's own colliders: a ray that starts at the edge of the player's
        // capsule, as when standing against a counter, must not be stopped by it.
        private IInteractable FromHit(Ray ray, float radius)
        {
            int count = radius <= 0f
                ? Physics.RaycastNonAlloc(ray, hits, maxDistance, layerMask, QueryTriggerInteraction.Ignore)
                : Physics.SphereCastNonAlloc(ray, radius, hits, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
            var self = transform.root;
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(self)) continue;
                if (nearest < 0 || hits[i].distance < hits[nearest].distance) nearest = i;
            }
            if (nearest < 0) return null;

            var collider = hits[nearest].collider;
            if (radius <= 0f)
            {
                LookedAt = collider;
                LookedDistance = hits[nearest].distance;
            }

            // A disabled component (a closed rack's door) is not interactable.
            foreach (var candidate in collider.GetComponentsInParent<IInteractable>())
            {
                if (candidate is Behaviour behaviour && !behaviour.isActiveAndEnabled) continue;
                // Too far for this one (it has a shorter reach than the ray).
                if (candidate.MaxReach > 0f && hits[nearest].distance > candidate.MaxReach) return null;
                return candidate;
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
