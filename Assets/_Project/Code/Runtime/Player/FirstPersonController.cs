using System;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// First-person movement on a CharacterController: walking with acceleration, mouse/stick look,
    /// sprinting with limited stamina, crouching with a headroom check, gravity and landing. It raises
    /// <see cref="Footstep"/> every time a foot touches the floor and <see cref="Landed"/> after a fall;
    /// head bob and footstep sounds listen to those instead of computing their own rhythm.
    ///
    /// Input comes as a <see cref="PlayerCommand"/> from <see cref="PlayerInputReader"/> on the same object;
    /// without a reader (or while it is blocked) the player just stands, so terminals can take over input.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("View")]
        [Tooltip("Turns up and down; the camera is its child. Its height follows the crouch.")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField, Range(45f, 89f)] private float pitchLimit = 85f;

        [Header("Walking")]
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float sprintSpeed = 4.4f;
        [SerializeField] private float crouchSpeed = 1.3f;
        [Tooltip("How fast the player gets up to speed, in m/s².")]
        [SerializeField] private float acceleration = 14f;
        [Tooltip("How fast the player stops, in m/s².")]
        [SerializeField] private float deceleration = 18f;
        [SerializeField] private float gravity = 18f;

        [Header("Sprint")]
        [Tooltip("Seconds of sprinting on a full stamina bar. 0 means unlimited.")]
        [SerializeField] private float sprintDuration = 6f;
        [Tooltip("Seconds to refill an empty stamina bar.")]
        [SerializeField] private float staminaRecoveryTime = 5f;
        [Tooltip("Seconds after sprinting before stamina starts to refill.")]
        [SerializeField] private float staminaRecoveryDelay = 1f;
        [Tooltip("After running out, sprinting is possible again from this much stamina (0..1).")]
        [SerializeField, Range(0f, 1f)] private float staminaToSprintAgain = 0.3f;
        [Tooltip("Only sprint when the stick or keys point at least this much forward (0..1).")]
        [SerializeField, Range(0f, 1f)] private float sprintForwardThreshold = 0.5f;

        [Header("Body")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.05f;
        [Tooltip("Eyes are this far below the top of the head.")]
        [SerializeField] private float eyeBelowTop = 0.14f;
        [Tooltip("Seconds to go fully down or up.")]
        [SerializeField] private float crouchTime = 0.2f;

        [Header("Footsteps")]
        [SerializeField] private float walkStepLength = 0.85f;
        [SerializeField] private float crouchStepLength = 0.6f;
        [SerializeField] private float sprintStepLength = 1.15f;
        [Tooltip("Falls faster than this (m/s) raise Landed.")]
        [SerializeField] private float landingThreshold = 1.5f;

        [Tooltip("Layers that block standing up and count as ground for footsteps.")]
        [SerializeField] private LayerMask environmentMask = ~0;

        // Small downward speed while grounded keeps the controller on slopes and stairs.
        private const float GroundStick = 2f;

        private CharacterController body;
        private PlayerInputReader input;
        private readonly StrideModel stride = new StrideModel();
        private Vector3 horizontalVelocity;
        private float verticalSpeed;
        private float height;
        private float yaw;
        private float pitch;
        private bool crouched;
        private bool sprinting;
        private bool exhausted;
        private float stamina = 1f;
        private float staminaRecoveryTimer;
        private bool wasGrounded = true;
        private bool initialized;

        /// <summary>A foot touched the floor.</summary>
        public event Action<FootstepEvent> Footstep;

        /// <summary>The player hit the ground after falling; the argument is the impact speed in m/s.</summary>
        public event Action<float> Landed;

        /// <summary>The player started (true) or stopped (false) crouching.</summary>
        public event Action<bool> CrouchChanged;

        /// <summary>The player started (true) or stopped (false) sprinting.</summary>
        public event Action<bool> SprintChanged;

        public bool IsGrounded { get; private set; } = true;

        public bool IsCrouching => crouched;

        public bool IsSprinting => sprinting;

        /// <summary>Remaining sprint stamina, 0 (empty) to 1 (full); for a stamina bar or breathing sounds.</summary>
        public float Stamina => stamina;

        /// <summary>True after stamina ran out, until it has refilled enough to sprint again.</summary>
        public bool IsExhausted => exhausted;

        /// <summary>0 when standing, 1 when fully crouched; follows the height transition.</summary>
        public float CrouchAmount => Mathf.InverseLerp(standingHeight, crouchingHeight, height);

        /// <summary>Actual horizontal speed in m/s (zero when walking into a wall).</summary>
        public float HorizontalSpeed { get; private set; }

        public float WalkSpeed => walkSpeed;

        public float SprintSpeed => sprintSpeed;

        /// <summary>Steps taken; see <see cref="StrideModel"/>.</summary>
        public float StridePhase => stride.Phase;

        public float Pitch => pitch;

        public float Height => height;

        public Transform CameraPivot => cameraPivot;

        private void Awake() => EnsureInitialized();

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            var command = input != null ? input.ReadCommand(deltaTime) : default;
            Simulate(command, deltaTime);
        }

        /// <summary>Advances the player by one frame. Update calls it with the input; tests call it directly.</summary>
        public void Simulate(PlayerCommand command, float deltaTime)
        {
            EnsureInitialized();
            Look(command.Look);
            UpdateCrouch(command.Crouch, deltaTime);
            UpdateSprint(command, deltaTime);
            Move(command.Move, deltaTime);
            cameraPivot.localPosition = new Vector3(0f, height - eyeBelowTop, 0f);
        }

        /// <summary>Turns the player to face a direction, e.g. when spawning.</summary>
        public void SetView(float yawDegrees, float pitchDegrees)
        {
            EnsureInitialized();
            yaw = yawDegrees;
            pitch = Mathf.Clamp(pitchDegrees, -pitchLimit, pitchLimit);
            ApplyView();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            body = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            if (cameraPivot == null)
            {
                cameraPivot = transform.Find("CameraPivot");
            }
            if (cameraPivot == null)
            {
                cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.SetParent(transform, false);
            }

            yaw = transform.eulerAngles.y;
            pitch = 0f;
            height = standingHeight;
            ApplyHeight();
            ApplyView();
        }

        private void Look(Vector2 look)
        {
            if (look == Vector2.zero) return;

            yaw = Mathf.Repeat(yaw + look.x, 360f);
            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            ApplyView();
        }

        private void ApplyView()
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateCrouch(bool wantsCrouch, float deltaTime)
        {
            // Releasing crouch under something low keeps the player down until there is room.
            bool shouldCrouch = wantsCrouch || (height < standingHeight - 0.001f && !HasHeadroom(standingHeight));
            if (shouldCrouch != crouched)
            {
                crouched = shouldCrouch;
                CrouchChanged?.Invoke(crouched);
            }

            float target = crouched ? crouchingHeight : standingHeight;
            float rate = (standingHeight - crouchingHeight) / Mathf.Max(crouchTime, 0.01f);
            float next = Mathf.MoveTowards(height, target, rate * deltaTime);
            if (next > height && !HasHeadroom(next)) return;

            height = next;
            ApplyHeight();
        }

        /// <summary>
        /// Sprinting needs the sprint input, walking mostly forward, standing upright on the ground and
        /// stamina. Stamina drains while sprinting and refills after a short pause; once it runs out the
        /// player has to recover part of it before sprinting again, so it cannot be tapped endlessly.
        /// </summary>
        private void UpdateSprint(PlayerCommand command, float deltaTime)
        {
            bool unlimited = sprintDuration <= 0f;
            bool upright = !crouched && height >= standingHeight - 0.001f;
            bool wantsSprint = command.Sprint && command.Move.y >= sprintForwardThreshold && upright && IsGrounded;
            bool canSprint = unlimited || (!exhausted && stamina > 0f);
            SetSprinting(wantsSprint && canSprint);

            if (unlimited)
            {
                stamina = 1f;
                return;
            }

            if (sprinting && HorizontalSpeed > walkSpeed * 0.5f)
            {
                stamina = Mathf.Max(0f, stamina - deltaTime / sprintDuration);
                staminaRecoveryTimer = staminaRecoveryDelay;
                if (stamina <= 0f)
                {
                    exhausted = true;
                    SetSprinting(false);
                }
                return;
            }

            if (staminaRecoveryTimer > 0f)
            {
                staminaRecoveryTimer -= deltaTime;
                return;
            }

            stamina = Mathf.Min(1f, stamina + deltaTime / Mathf.Max(staminaRecoveryTime, 0.01f));
            if (exhausted && stamina >= staminaToSprintAgain)
            {
                exhausted = false;
            }
        }

        private void SetSprinting(bool value)
        {
            if (value == sprinting) return;
            sprinting = value;
            SprintChanged?.Invoke(sprinting);
        }

        private void ApplyHeight()
        {
            body.height = height;
            body.center = new Vector3(0f, height / 2f, 0f);
        }

        /// <summary>True if the capsule can grow to <paramref name="targetHeight"/> without hitting anything.</summary>
        private bool HasHeadroom(float targetHeight)
        {
            float rise = targetHeight - height;
            if (rise <= 0f) return true;

            // Cast from the top sphere of the current capsule; the player's own collider is ignored
            // because the cast starts inside it.
            float radius = body.radius * 0.95f;
            var origin = transform.position + Vector3.up * (height - body.radius);
            return !Physics.SphereCast(origin, radius, Vector3.up, out _, rise + body.skinWidth, environmentMask, QueryTriggerInteraction.Ignore);
        }

        private void Move(Vector2 move, float deltaTime)
        {
            move = Vector2.ClampMagnitude(move, 1f);
            float maxSpeed = sprinting ? sprintSpeed : Mathf.Lerp(walkSpeed, crouchSpeed, CrouchAmount);
            var desired = (transform.right * move.x + transform.forward * move.y) * maxSpeed;
            float rate = desired.sqrMagnitude > horizontalVelocity.sqrMagnitude ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * deltaTime);

            if (IsGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = -GroundStick;
            }
            verticalSpeed -= gravity * deltaTime;
            float fallSpeed = -verticalSpeed;

            var before = transform.position;
            body.Move((horizontalVelocity + Vector3.up * verticalSpeed) * deltaTime);
            var moved = transform.position - before;
            moved.y = 0f;

            // Walking into a wall must not build up speed that would shoot the player off along it later.
            float actualSpeed = moved.magnitude / deltaTime;
            if (actualSpeed < horizontalVelocity.magnitude)
            {
                horizontalVelocity = horizontalVelocity.normalized * actualSpeed;
            }
            HorizontalSpeed = actualSpeed;

            IsGrounded = body.isGrounded;
            if (IsGrounded && !wasGrounded)
            {
                stride.PlantFoot();
                if (fallSpeed >= landingThreshold)
                {
                    Landed?.Invoke(fallSpeed);
                }
            }
            if (IsGrounded && verticalSpeed < -GroundStick)
            {
                verticalSpeed = -GroundStick;
            }
            wasGrounded = IsGrounded;

            if (!IsGrounded) return;

            // Longer strides when running, shorter when crouching; the cadence rises a little with speed.
            float stepLength = actualSpeed > walkSpeed
                ? Mathf.Lerp(walkStepLength, sprintStepLength, Mathf.InverseLerp(walkSpeed, sprintSpeed, actualSpeed))
                : Mathf.Lerp(walkStepLength, crouchStepLength, CrouchAmount);
            int steps = stride.Advance(moved.magnitude, stepLength);
            for (int i = 0; i < steps; i++)
            {
                Footstep?.Invoke(new FootstepEvent(transform.position, actualSpeed, crouched, sprinting, FindGround()));
            }
        }

        private Collider FindGround()
        {
            var origin = transform.position + Vector3.up * 0.2f;
            return Physics.Raycast(origin, Vector3.down, out var hit, 0.5f, environmentMask, QueryTriggerInteraction.Ignore)
                ? hit.collider
                : null;
        }
    }
}
