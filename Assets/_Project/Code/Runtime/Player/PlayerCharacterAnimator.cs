using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Drives a downloaded, rigged human character (a Humanoid model with an Animator) from the state of the
    /// <see cref="FirstPersonController"/>: walking and running animations are blended by speed and played at
    /// the rate that keeps the feet from sliding, a jump starts the jump animation, crouching blends the
    /// crouch animations. The player sees the body when looking down, so the head is scaled to nothing and
    /// the hair and eyelashes only cast shadows; the camera sits in the head's place.
    ///
    /// It replaces the procedural mannequin (<see cref="PlayerBodyAnimator"/>); the tool Set Up Mixamo Player
    /// Body builds the Character, the Animator Controller and turns the mannequin off.
    /// Animator parameters: Speed (m/s), MotionSpeed and CrouchMotionSpeed (playback rates, negative when walking
    /// backwards), Grounded, Crouch (0 to 1), Jump (trigger). Missing animations are simply not used.
    /// </summary>
    public class PlayerCharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedParameter = Animator.StringToHash("MotionSpeed");
        private static readonly int CrouchMotionSpeedParameter = Animator.StringToHash("CrouchMotionSpeed");
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int CrouchParameter = Animator.StringToHash("Crouch");
        private static readonly int JumpParameter = Animator.StringToHash("Jump");

        [SerializeField] private FirstPersonController controller;
        [SerializeField] private Animator animator;
        [Tooltip("Scaled to nothing so the camera does not look out of the head. Hair and eyelashes are hidden by the tool.")]
        [SerializeField] private Transform headBone;

        [Header("Foot sync")]
        [Tooltip("How fast the walking animation moves the character in m/s. The foot slides when this is wrong; tune it in Play mode.")]
        [SerializeField] private float walkClipSpeed = 1.45f;
        [Tooltip("The same for the running animation.")]
        [SerializeField] private float runClipSpeed = 3.6f;
        [Tooltip("The same for the crouched walking animation.")]
        [SerializeField] private float crouchClipSpeed = 1.1f;
        [Header("Head")]
        [Tooltip("The head is kept this far behind the camera, in metres, so the chest never fills the view when looking down.")]
        [SerializeField] private float headBackOffset = 0.12f;

        [Tooltip("How smoothly the speed given to the animation follows the real speed, in seconds.")]
        [SerializeField] private float speedSmoothing = 0.08f;

        private float smoothedSpeed;
        private float speedVelocity;
        private float groundOffset;

        private void Awake()
        {
            if (controller == null) controller = GetComponentInParent<FirstPersonController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (controller != null) controller.Jumped += OnJumped;
        }

        private void OnDisable()
        {
            if (controller != null) controller.Jumped -= OnJumped;
        }

        private void Update()
        {
            if (controller == null || animator == null || animator.runtimeAnimatorController == null) return;

            float speed = controller.HorizontalSpeed;
            smoothedSpeed = Mathf.SmoothDamp(smoothedSpeed, speed, ref speedVelocity, speedSmoothing);

            animator.SetFloat(SpeedParameter, smoothedSpeed);
            bool backwards = controller.LocalVelocity.y < -0.3f;
            animator.SetFloat(MotionSpeedParameter, MotionSpeed(smoothedSpeed, backwards));
            animator.SetFloat(CrouchMotionSpeedParameter, CrouchMotionSpeed(smoothedSpeed, backwards));
            animator.SetBool(GroundedParameter, controller.IsGrounded);
            animator.SetFloat(CrouchParameter, controller.CrouchAmount);
        }

        private void LateUpdate()
        {
            if (controller == null) return;

            // The animation has posed the bones by now. Nothing of the head is drawn.
            if (headBone != null) headBone.localScale = Vector3.zero;

            float target = FloorSnap.Offset(controller.transform.position, controller.IsGrounded);
            groundOffset = Mathf.Lerp(groundOffset, target, 1f - Mathf.Exp(-25f * Mathf.Max(Time.deltaTime, 0f)));

            // The camera does not follow the animated head, so a body that leans, sways or travels in its
            // animation (running!) would drift ahead of it. The whole character is shifted every frame so
            // that its head stays where the camera is; the legs and arms still move as animated.
            var position = transform.localPosition;
            if (headBone != null)
            {
                Vector3 head = controller.transform.InverseTransformPoint(headBone.position);
                position.x -= head.x;
                position.z -= head.z + headBackOffset;
            }
            position.y = groundOffset;
            transform.localPosition = position;
        }

        /// <summary>
        /// Playback rate of the locomotion animation for a walking speed. Between the walking and the running
        /// animation the blend of both already moves at the right speed; outside it the animation is slowed
        /// down or sped up, so a foot on the floor stays where it is.
        /// </summary>
        public float MotionSpeed(float speed, bool backwards)
        {
            float rate = 1f;
            if (speed > 0.05f)
            {
                float animationSpeed = Mathf.Clamp(speed, Mathf.Max(0.1f, walkClipSpeed), Mathf.Max(walkClipSpeed + 0.1f, runClipSpeed));
                rate = speed / animationSpeed;
            }
            return backwards ? -rate : rate;
        }

        /// <summary>Playback rate of the crouched walking animation for a walking speed: one clip, so it is simply the ratio.</summary>
        public float CrouchMotionSpeed(float speed, bool backwards)
        {
            float rate = speed > 0.05f ? speed / Mathf.Max(0.1f, crouchClipSpeed) : 1f;
            return backwards ? -rate : rate;
        }

        private void OnJumped()
        {
            if (animator != null) animator.SetTrigger(JumpParameter);
        }
    }
}
