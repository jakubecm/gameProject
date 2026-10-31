using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The door of an attackable rack. It starts ajar; the rack-door key (F) swings it fully open, and the key on an open
    /// door shuts it completely. The <see cref="Rack"/> disables this component on every other rack (the interactor skips disabled ones).
    ///
    /// Put it on the door's hinge, next to the door's own collider: the interactor finds it only when the player looks at the
    /// door, never at the rack's sides or back (the body's collider has no interactable). <see cref="hinge"/> is the pivot the
    /// door leaf hangs on, turned about its local Y axis (negative angles swing the leaf towards the front, +Z).
    /// Other systems listen to <see cref="OpenChanged"/> instead of referencing the rack.
    /// </summary>
    public class RackDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform hinge;
        [Tooltip("How far the door stands open at the start, in degrees (0 = shut).")]
        [SerializeField, Range(0f, 180f)] private float ajarAngle = 22f;
        [SerializeField, Range(0f, 180f)] private float openAngle = 112f;
        [Tooltip("Degrees per second.")]
        [SerializeField, Min(1f)] private float swingSpeed = 130f;
        [SerializeField] private string openPrompt = "Otevřít dveře racku";
        [SerializeField] private string closePrompt = "Zavřít dveře racku";

        private float angle;
        private float target;
        private bool open;
        private bool initialised;

        /// <summary>Raised when the door is opened fully (true) or shut completely (false).</summary>
        public event Action<RackDoor, bool> OpenChanged;

        /// <summary>True from the moment the door is told to open until it is told to shut; the ajar start is not open.</summary>
        public bool IsOpen => open;

        /// <summary>Current opening angle in degrees (0 = shut).</summary>
        public float Angle
        {
            get
            {
                Initialise();
                return angle;
            }
        }

        public float OpenAngle => openAngle;

        public string Prompt => open ? closePrompt : openPrompt;

        public float HoldDuration => 0f;

        /// <summary>Rack doors are used with F.</summary>
        public InteractKey Key => InteractKey.Secondary;

        private void Update() => Step(Time.deltaTime);

        /// <summary>Back to the starting pose: ajar, not open.</summary>
        public void ResetToAjar()
        {
            initialised = true;
            open = false;
            angle = ajarAngle;
            target = ajarAngle;
            Apply();
        }

        /// <summary>Shuts the door at once, without a swing (a rack whose door never opens).</summary>
        public void CloseImmediately()
        {
            initialised = true;
            open = false;
            angle = 0f;
            target = 0f;
            Apply();
        }

        /// <summary>Opens a shut or ajar door, shuts an open one.</summary>
        public void Interact(Interactor interactor) => SetOpen(!open);

        public void SetOpen(bool value)
        {
            Initialise();
            if (open == value) return;

            open = value;
            target = open ? openAngle : 0f;
            OpenChanged?.Invoke(this, open);
        }

        /// <summary>Moves the door towards its target by <paramref name="deltaSeconds"/> (tests drive it without a player loop).</summary>
        public void Step(float deltaSeconds)
        {
            Initialise();
            angle = Mathf.MoveTowards(angle, target, swingSpeed * deltaSeconds);
            Apply();
        }

        private void Initialise()
        {
            if (initialised) return;

            initialised = true;
            angle = ajarAngle;
            target = ajarAngle;
            Apply();
        }

        private void Apply()
        {
            if (hinge != null)
            {
                hinge.localRotation = Quaternion.Euler(0f, -angle, 0f);
            }
        }
    }
}
