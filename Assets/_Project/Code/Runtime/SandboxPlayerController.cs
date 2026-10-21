using UnityEngine;
using UnityEngine.InputSystem;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Throwaway WASD + mouse-look controller so sandbox scenes can be walked before the real
    /// first-person controller exists (task 1.7). Remove it once that controller replaces it.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SandboxPlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float lookSensitivity = 0.1f;

        private CharacterController controller;
        private float pitch;

        private void Awake() => controller = GetComponent<CharacterController>();

        private void OnEnable() => SetCursorLocked(true);

        private void OnDisable() => SetCursorLocked(false);

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            // Escape frees the mouse (to reach the Editor); a click takes it back.
            if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            else if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);

            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            if (cameraPivot != null) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

            var input = Vector3.zero;
            if (keyboard.wKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed) input.z -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            controller.SimpleMove(transform.TransformDirection(input.normalized) * moveSpeed);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
