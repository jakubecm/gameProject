using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Moves the camera with the walking rhythm of the <see cref="FirstPersonController"/>: a dip on every
    /// footstep, a sideways sway over the standing foot with a slight roll, smaller when crouching and
    /// bigger when sprinting, a slightly wider field of view while sprinting, and a short dip after
    /// landing. Lives on the camera (a child of the camera pivot). It only offsets the camera, never the
    /// view direction, so aiming at terminals stays steady. <see cref="BobEnabled"/> is the "head bob
    /// on/off" option of the settings menu (task 5.6).
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;
        [SerializeField] private bool bobEnabled = true;

        [Header("Walking")]
        [SerializeField] private float verticalAmplitude = 0.018f;
        [SerializeField] private float lateralAmplitude = 0.012f;
        [Tooltip("Roll in degrees at the far end of the sway.")]
        [SerializeField] private float rollAmplitude = 0.2f;
        [Tooltip("Bob size while crouched, relative to walking.")]
        [SerializeField, Range(0f, 1f)] private float crouchFactor = 0.6f;
        [Tooltip("Bob size at full sprint, relative to walking. Below 1, because the steps already come faster.")]
        [SerializeField, Range(0.5f, 2f)] private float sprintFactor = 0.85f;
        [Tooltip("How quickly the bob fades in and out when starting and stopping.")]
        [SerializeField] private float blendSpeed = 6f;

        [Header("Sprint")]
        [Tooltip("Extra field of view in degrees at full sprint; 0 turns it off.")]
        [SerializeField] private float sprintFovIncrease = 3f;
        [SerializeField] private float fovBlendSpeed = 6f;

        [Header("Landing")]
        [Tooltip("Camera dip in metres per m/s of impact speed.")]
        [SerializeField] private float landingDipPerSpeed = 0.012f;
        [SerializeField] private float maxLandingDip = 0.12f;
        [SerializeField] private float landingRecovery = 9f;

        private Camera viewCamera;
        private float baseFov;
        private Vector3 restPosition;
        private float intensity;
        private float landingDip;

        public bool BobEnabled
        {
            get => bobEnabled;
            set => bobEnabled = value;
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<FirstPersonController>();
            }
            restPosition = transform.localPosition;
            viewCamera = GetComponent<Camera>();
            if (viewCamera != null) baseFov = viewCamera.fieldOfView;
        }

        private void OnEnable()
        {
            if (controller != null) controller.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (controller != null) controller.Landed -= OnLanded;
            transform.localPosition = restPosition;
            transform.localRotation = Quaternion.identity;
            if (viewCamera != null) viewCamera.fieldOfView = baseFov;
        }

        private void LateUpdate()
        {
            if (controller == null) return;

            float deltaTime = Time.deltaTime;
            // 1 at walking speed, rising to sprintFactor at full sprint.
            float sprintBlend = Mathf.InverseLerp(controller.WalkSpeed, controller.SprintSpeed, controller.HorizontalSpeed);
            float target = !controller.IsGrounded ? 0f
                : Mathf.Clamp01(controller.HorizontalSpeed / controller.WalkSpeed) * Mathf.Lerp(1f, sprintFactor, sprintBlend);
            intensity = Mathf.MoveTowards(intensity, target, blendSpeed * deltaTime);
            landingDip = Mathf.Lerp(landingDip, 0f, 1f - Mathf.Exp(-landingRecovery * deltaTime));

            var offset = Vector2.zero;
            float roll = 0f;
            if (bobEnabled)
            {
                float scale = intensity * Mathf.Lerp(1f, crouchFactor, controller.CrouchAmount);
                offset = StrideModel.BobOffset(controller.StridePhase, verticalAmplitude, lateralAmplitude) * scale;
                roll = -rollAmplitude * Mathf.Sin(Mathf.PI * controller.StridePhase) * scale;
            }

            transform.localPosition = restPosition + new Vector3(offset.x, offset.y - landingDip, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, roll);

            if (viewCamera != null)
            {
                float targetFov = baseFov + (controller.IsSprinting ? sprintFovIncrease * sprintBlend : 0f);
                viewCamera.fieldOfView = Mathf.MoveTowards(viewCamera.fieldOfView, targetFov, fovBlendSpeed * sprintFovIncrease * deltaTime);
            }
        }

        private void OnLanded(float impactSpeed)
        {
            // The landing dip is physical feedback, so it stays even with head bob turned off.
            landingDip = Mathf.Min(maxLandingDip, landingDip + impactSpeed * landingDipPerSpeed);
        }
    }
}
