using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Moves the player's body so it can be seen when looking down: legs that step with the walking rhythm
    /// of the <see cref="FirstPersonController"/> and stay planted on the floor (two-bone solver, see
    /// <see cref="LegSolver"/> and <see cref="WalkCycle"/>), arms that swing against the legs, a torso that
    /// leans when sprinting and crouching, tucked legs in the air and a bend after landing. Everything is
    /// computed from the controller's state, so there is no Animator and no animation clips; a final
    /// character model can be rigged to the same bones later. The head only casts shadows, so the camera
    /// does not look out of its inside.
    ///
    /// Lives on the Body child of the player. The tool Create Player Prefab builds the bones.
    /// </summary>
    public class PlayerBodyAnimator : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;

        [Header("Bones")]
        [SerializeField] private Transform pelvis;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform thighLeft;
        [SerializeField] private Transform thighRight;
        [SerializeField] private Transform shinLeft;
        [SerializeField] private Transform shinRight;
        [SerializeField] private Transform footLeft;
        [SerializeField] private Transform footRight;
        [SerializeField] private Transform upperArmLeft;
        [SerializeField] private Transform upperArmRight;
        [SerializeField] private Transform forearmLeft;
        [SerializeField] private Transform forearmRight;

        [Header("Walking")]
        [Tooltip("How high a foot rises while it swings forward, in metres, when walking and when sprinting.")]
        [SerializeField] private Vector2 footLift = new Vector2(0.09f, 0.17f);
        [Tooltip("Arm swing in degrees when walking and when sprinting.")]
        [SerializeField] private Vector2 armSwing = new Vector2(22f, 38f);
        [Tooltip("Elbow bend in degrees when walking and when sprinting.")]
        [SerializeField] private Vector2 elbowBend = new Vector2(12f, 70f);

        [Header("Posture")]
        [Tooltip("Forward lean of the torso in degrees at full sprint and when fully crouched.")]
        [SerializeField] private Vector2 leanDegrees = new Vector2(8f, 24f);
        [SerializeField] private float leanSpeed = 8f;

        [Header("Air and landing")]
        [Tooltip("Where the ankles hang below the hips while rising and while falling, in metres.")]
        [SerializeField] private Vector2 airAnkleDrop = new Vector2(0.55f, 0.85f);
        [SerializeField] private float airBlendSpeed = 14f;
        [Tooltip("How far the hips drop after landing, per m/s of impact speed.")]
        [SerializeField] private float landingDropPerSpeed = 0.02f;
        [SerializeField] private float maxLandingDrop = 0.18f;
        [SerializeField] private float landingRecovery = 10f;

        private float moveBlend;
        private float airBlend;
        private float lean;
        private float landingDrop;
        private float groundOffset;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<FirstPersonController>();
            }
        }

        private void OnEnable()
        {
            if (controller != null) controller.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (controller != null) controller.Landed -= OnLanded;
        }

        private void LateUpdate()
        {
            if (controller == null || pelvis == null) return;

            float deltaTime = Time.deltaTime;
            float speed = controller.HorizontalSpeed;
            bool grounded = controller.IsGrounded;

            moveBlend = Mathf.MoveTowards(moveBlend, grounded ? Mathf.Clamp01(speed / 0.5f) : 0f, 8f * deltaTime);
            airBlend = Mathf.MoveTowards(airBlend, grounded ? 0f : 1f, airBlendSpeed * deltaTime);
            landingDrop = Mathf.Lerp(landingDrop, 0f, 1f - Mathf.Exp(-landingRecovery * deltaTime));

            float sprint = controller.IsSprinting ? Mathf.InverseLerp(controller.WalkSpeed, controller.SprintSpeed, speed) : 0f;
            float crouch = controller.CrouchAmount;

            SnapToTheFloor(deltaTime, grounded);

            // The body hangs below the eyes, so it follows the crouch and every other eye movement of the controller.
            float hipHeight = controller.CameraPivot.localPosition.y - PlayerBodyDimensions.HipToEyes - landingDrop;
            pelvis.localPosition = new Vector3(0f, hipHeight, 0f);

            float targetLean = Mathf.Lerp(0f, leanDegrees.x, sprint) + leanDegrees.y * crouch + (grounded ? 0f : 6f);
            lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-leanSpeed * deltaTime));
            if (chest != null) chest.localRotation = Quaternion.Euler(lean, 0f, 0f);

            float direction = controller.LocalVelocity.y < -0.3f ? -1f : 1f;
            float halfStride = controller.CurrentStepLength * 0.5f;
            float lift = Mathf.Lerp(footLift.x, footLift.y, sprint) * Mathf.Lerp(1f, 0.5f, crouch);

            float leftSwing = AnimateLeg(false, thighLeft, shinLeft, footLeft, hipHeight, halfStride, lift, direction);
            float rightSwing = AnimateLeg(true, thighRight, shinRight, footRight, hipHeight, halfStride, lift, direction);

            AnimateArms(leftSwing, rightSwing, sprint, crouch);
        }

        /// <summary>
        /// The CharacterController floats about a skin width above the floor, by a varying amount, and the floor
        /// may be a ramp. The soles are put on the floor that is really under the player (they stay where they
        /// are in the air).
        /// </summary>
        private void SnapToTheFloor(float deltaTime, bool grounded)
        {
            float target = FloorSnap.Offset(controller.transform.position, grounded);

            float blend = deltaTime > 0f ? 1f - Mathf.Exp(-25f * deltaTime) : 1f;
            groundOffset = Mathf.Lerp(groundOffset, target, blend);

            var position = transform.localPosition;
            position.y = groundOffset;
            transform.localPosition = position;
        }

        /// <summary>Poses one leg and returns how far forward its foot is as a share of the half stride (-1 to 1).</summary>
        private float AnimateLeg(bool right, Transform thigh, Transform shin, Transform foot, float hipHeight, float halfStride, float lift, float direction)
        {
            if (thigh == null || shin == null) return 0f;

            // On the ground: the foot follows the walking cycle. In the air: the legs tuck up, then reach down again.
            float cycle = WalkCycle.CycleOf(controller.StridePhase, right);
            Vector2 step = WalkCycle.Foot(cycle, halfStride, lift);
            float groundForward = step.x * direction * moveBlend;
            float groundDown = hipHeight - PlayerBodyDimensions.AnkleHeight - step.y * moveBlend;

            float falling = Mathf.InverseLerp(controller.JumpSpeed, -controller.JumpSpeed, controller.VerticalSpeed);
            float airDown = Mathf.Lerp(airAnkleDrop.x, airAnkleDrop.y, falling);
            float airForward = right ? 0.04f : 0.14f;

            float forward = Mathf.Lerp(groundForward, airForward, airBlend);
            float down = Mathf.Lerp(groundDown, airDown, airBlend);

            var pose = LegSolver.Solve(PlayerBodyDimensions.ThighLength, PlayerBodyDimensions.ShinLength, forward, down);

            // Forward is a rotation about -X, a knee bending back is a rotation about +X.
            thigh.localRotation = Quaternion.Euler(-pose.ThighAngle, 0f, 0f);
            shin.localRotation = Quaternion.Euler(pose.KneeFlexion, 0f, 0f);
            // The sole stays level, even though the shin tilts.
            if (foot != null) foot.localRotation = Quaternion.Euler(pose.ShinAngle, 0f, 0f);

            return halfStride > 0f ? Mathf.Clamp(groundForward / halfStride, -1f, 1f) * (1f - airBlend) : 0f;
        }

        private void AnimateArms(float leftFoot, float rightFoot, float sprint, float crouch)
        {
            float amplitude = Mathf.Lerp(armSwing.x, armSwing.y, sprint) * Mathf.Lerp(1f, 0.5f, crouch);
            float bend = Mathf.Lerp(elbowBend.x, elbowBend.y, sprint);

            // An arm swings against the leg on its side. In the air both arms come up a little.
            float leftArm = Mathf.Lerp(-amplitude * leftFoot, 30f, airBlend);
            float rightArm = Mathf.Lerp(-amplitude * rightFoot, 30f, airBlend);
            float elbow = Mathf.Lerp(bend, 40f, airBlend);

            if (upperArmLeft != null) upperArmLeft.localRotation = Quaternion.Euler(-leftArm, 0f, 0f);
            if (upperArmRight != null) upperArmRight.localRotation = Quaternion.Euler(-rightArm, 0f, 0f);
            if (forearmLeft != null) forearmLeft.localRotation = Quaternion.Euler(-elbow, 0f, 0f);
            if (forearmRight != null) forearmRight.localRotation = Quaternion.Euler(-elbow, 0f, 0f);
        }

        private void OnLanded(float impactSpeed)
        {
            landingDrop = Mathf.Min(maxLandingDrop, landingDrop + impactSpeed * landingDropPerSpeed);
        }
    }
}
