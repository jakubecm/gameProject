using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// A bi-parting automatic sliding glass door. Photo cells on both sides watch a zone in front of the door;
    /// while someone (any <see cref="CharacterController"/>) stands in it and the door is not locked, both leaves
    /// slide apart, and they close again a moment after the zone is empty. The access reader's LED shows green
    /// while the door is unlocked and red while it is locked.
    ///
    /// The pivot is the opening pose (<see cref="ColdShutdown.Core.RoomContract.GetOpeningPose"/>): local X runs
    /// along the wall, +Z points to the side the leaves slide on. Locking is game logic (task 2.x sets
    /// <see cref="IsLocked"/>); in the preview every door is unlocked.
    /// </summary>
    public class AutoSlidingDoor : MonoBehaviour
    {
        [Header("Leaves")]
        [Tooltip("Leaf that slides towards -X, then the one that slides towards +X.")]
        [SerializeField] private Transform[] leaves = new Transform[2];
        [Tooltip("How far each leaf slides when the door is fully open (the leaf width).")]
        [SerializeField, Min(0f)] private float travel = 1.2f;
        [SerializeField, Min(0.05f)] private float openSeconds = 0.6f;
        [SerializeField, Min(0.05f)] private float closeSeconds = 0.9f;
        [Tooltip("The door stays open this long after the photo cells see nobody.")]
        [SerializeField, Min(0f)] private float holdOpenSeconds = 1.2f;

        [Header("Photo cells")]
        [Tooltip("Door-local box both photo cells watch (both sides of the opening).")]
        [SerializeField] private Vector3 sensorCenter = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 sensorSize = new Vector3(2.4f, 2f, 3.2f);
        [Tooltip("Seconds between two looks of the photo cells.")]
        [SerializeField, Min(0f)] private float sensorInterval = 0.1f;

        [Header("Access")]
        [SerializeField] private bool locked;
        [SerializeField] private Renderer[] readerLeds = new Renderer[0];
        [SerializeField] private Material unlockedLed;
        [SerializeField] private Material lockedLed;

        // Floors, walls, leaves and furniture around a doorway easily fill a small buffer, and a full buffer can leave
        // the player out of the answer, so the door would never open.
        private static readonly Collider[] Hits = new Collider[64];

        private Vector3[] closedPositions;
        private float openAmount;
        private float holdTimer;
        private float sensorTimer;
        private bool someoneNear;
        private bool opening;

        /// <summary>Raised when the leaves start to open (true) or to close (false).</summary>
        public event Action<AutoSlidingDoor, bool> Moving;

        /// <summary>Raised when the door is locked or unlocked.</summary>
        public event Action<AutoSlidingDoor, bool> LockChanged;

        /// <summary>0 = closed, 1 = fully open.</summary>
        public float OpenAmount => openAmount;

        public bool IsOpen => openAmount >= 1f;

        public bool IsClosed => openAmount <= 0f;

        public float Travel => travel;

        public bool IsLocked
        {
            get => locked;
            set
            {
                if (locked == value) return;
                locked = value;
                ShowLock();
                LockChanged?.Invoke(this, locked);
            }
        }

        private void Awake()
        {
            CacheClosedPositions();
            ShowLock();
        }

        private void Update()
        {
            sensorTimer -= Time.deltaTime;
            if (sensorTimer <= 0f)
            {
                sensorTimer = sensorInterval;
                someoneNear = SenseSomeone();
            }
            Step(someoneNear, Time.deltaTime);
        }

        /// <summary>
        /// Advances the door by <paramref name="deltaTime"/> seconds with the photo cells seeing someone or not.
        /// Update calls it with the photo cells' answer; tests call it directly.
        /// </summary>
        public void Step(bool presence, float deltaTime)
        {
            CacheClosedPositions();
            if (presence && !locked)
            {
                holdTimer = holdOpenSeconds;
            }
            else
            {
                holdTimer -= deltaTime;
            }

            bool wantOpen = holdTimer > 0f && !locked;
            if (wantOpen != opening && (wantOpen ? openAmount < 1f : openAmount > 0f))
            {
                Moving?.Invoke(this, wantOpen);
            }
            opening = wantOpen;

            float speed = wantOpen ? 1f / openSeconds : -1f / closeSeconds;
            openAmount = Mathf.Clamp01(openAmount + speed * deltaTime);
            ApplyLeaves();
        }

        /// <summary>True when a character stands in the photo cells' zone.</summary>
        public bool SenseSomeone()
        {
            var center = transform.TransformPoint(sensorCenter);
            var halfExtents = Vector3.Scale(sensorSize, transform.lossyScale) / 2f;
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, Hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (Hits[i] is CharacterController) return true;
            }
            return false;
        }

        private void CacheClosedPositions()
        {
            if (closedPositions != null && closedPositions.Length == leaves.Length) return;
            closedPositions = new Vector3[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                closedPositions[i] = leaves[i] != null ? leaves[i].localPosition : Vector3.zero;
            }
        }

        private void ApplyLeaves()
        {
            // Ease in and out, like a door operator.
            float eased = openAmount * openAmount * (3f - 2f * openAmount);
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] == null) continue;
                float direction = i == 0 ? -1f : 1f;
                leaves[i].localPosition = closedPositions[i] + new Vector3(direction * travel * eased, 0f, 0f);
            }
        }

        private void ShowLock()
        {
            var material = locked ? lockedLed : unlockedLed;
            if (material == null || readerLeds == null) return;
            foreach (var led in readerLeds)
            {
                if (led != null) led.sharedMaterial = material;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(sensorCenter, sensorSize);
        }
    }
}
