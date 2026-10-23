using UnityEngine;
using UnityEngine.InputSystem;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Turns keyboard, mouse and gamepad input into a <see cref="PlayerCommand"/> for the
    /// <see cref="FirstPersonController"/>. The actions are serialized, so their bindings can be changed in
    /// the Inspector; the settings below are the values a settings menu (task 5.6) changes.
    ///
    /// The cursor is locked while playing; Escape frees it (to reach the Editor or a menu) and a click takes
    /// it back. While the cursor is free or <see cref="InputEnabled"/> is false, the player does not move.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>Hold the button, or press once to switch on and again to switch off.</summary>
        public enum ButtonMode
        {
            Hold,
            Toggle,
        }

        [Header("Actions")]
        [SerializeField] private InputAction move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        [SerializeField] private InputAction lookMouse = new InputAction("LookMouse", InputActionType.Value, expectedControlType: "Vector2");
        [SerializeField] private InputAction lookStick = new InputAction("LookStick", InputActionType.Value, expectedControlType: "Vector2");
        [SerializeField] private InputAction crouch = new InputAction("Crouch", InputActionType.Button);
        [SerializeField] private InputAction sprint = new InputAction("Sprint", InputActionType.Button);

        [Header("Settings")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] private float mouseSensitivity = 0.08f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField] private float stickSensitivity = 140f;
        [SerializeField] private bool invertY;
        [SerializeField] private ButtonMode crouchMode = ButtonMode.Hold;
        [Tooltip("In Toggle mode, sprint switches off by itself when the player stops walking forward.")]
        [SerializeField] private ButtonMode sprintMode = ButtonMode.Hold;
        [Tooltip("Lock and hide the cursor while playing.")]
        [SerializeField] private bool lockCursor = true;

        private bool crouchToggled;
        private bool sprintToggled;

        /// <summary>False while something else (a terminal, a menu) owns the input; the player then stands still.</summary>
        public bool InputEnabled { get; set; } = true;

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Max(0f, value);
        }

        public float StickSensitivity
        {
            get => stickSensitivity;
            set => stickSensitivity = Mathf.Max(0f, value);
        }

        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        public ButtonMode Crouching
        {
            get => crouchMode;
            set
            {
                crouchMode = value;
                crouchToggled = false;
            }
        }

        public ButtonMode Sprinting
        {
            get => sprintMode;
            set
            {
                sprintMode = value;
                sprintToggled = false;
            }
        }

        private bool CanRead => InputEnabled && (!lockCursor || Cursor.lockState == CursorLockMode.Locked);

        private void Awake()
        {
            // An action without bindings (new in an older prefab, or added at runtime) gets its defaults.
            if (move.bindings.Count == 0) BindMove();
            if (lookMouse.bindings.Count == 0) BindLookMouse();
            if (lookStick.bindings.Count == 0) BindLookStick();
            if (crouch.bindings.Count == 0) BindCrouch();
            if (sprint.bindings.Count == 0) BindSprint();
        }

        private void OnEnable()
        {
            move.Enable();
            lookMouse.Enable();
            lookStick.Enable();
            crouch.Enable();
            sprint.Enable();
            if (lockCursor) SetCursorLocked(true);
        }

        private void OnDisable()
        {
            move.Disable();
            lookMouse.Disable();
            lookStick.Disable();
            crouch.Disable();
            sprint.Disable();
            if (lockCursor) SetCursorLocked(false);
        }

        private void OnDestroy()
        {
            move.Dispose();
            lookMouse.Dispose();
            lookStick.Dispose();
            crouch.Dispose();
            sprint.Dispose();
        }

        private void Update()
        {
            if (!lockCursor) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && InputEnabled) SetCursorLocked(true);
        }

        /// <summary>Reads this frame's input. Called by the controller once per frame.</summary>
        public PlayerCommand ReadCommand(float deltaTime)
        {
            if (crouch.WasPressedThisFrame() && CanRead) crouchToggled = !crouchToggled;
            bool wantsCrouch = crouchMode == ButtonMode.Toggle ? crouchToggled : crouch.IsPressed() && CanRead;

            if (!CanRead)
            {
                sprintToggled = false;
                return new PlayerCommand { Crouch = wantsCrouch };
            }

            var walk = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            if (sprint.WasPressedThisFrame()) sprintToggled = !sprintToggled;
            if (walk.y <= 0f) sprintToggled = false;
            bool wantsSprint = sprintMode == ButtonMode.Toggle ? sprintToggled : sprint.IsPressed();

            // Sprinting stands the player up, so a toggled crouch is cancelled by it.
            if (wantsSprint && walk.y > 0f) crouchToggled = false;

            var look = lookMouse.ReadValue<Vector2>() * mouseSensitivity + lookStick.ReadValue<Vector2>() * (stickSensitivity * deltaTime);
            if (invertY) look.y = -look.y;

            return new PlayerCommand
            {
                Move = walk,
                Look = look,
                Crouch = crouchMode == ButtonMode.Toggle ? crouchToggled : wantsCrouch,
                Sprint = wantsSprint,
            };
        }

        /// <summary>
        /// WASD / arrows and the left stick to walk, mouse and the right stick to look, C or Left Ctrl and the
        /// gamepad's right face button to crouch, Left Shift and a left stick click to sprint.
        /// </summary>
        public void ResetToDefaultBindings()
        {
            BindMove();
            BindLookMouse();
            BindLookStick();
            BindCrouch();
            BindSprint();
        }

        private void BindMove()
        {
            ClearBindings(move);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
        }

        private void BindLookMouse()
        {
            ClearBindings(lookMouse);
            lookMouse.AddBinding("<Mouse>/delta");
        }

        private void BindLookStick()
        {
            ClearBindings(lookStick);
            lookStick.AddBinding("<Gamepad>/rightStick");
        }

        private void BindCrouch()
        {
            ClearBindings(crouch);
            crouch.AddBinding("<Keyboard>/c");
            crouch.AddBinding("<Keyboard>/leftCtrl");
            crouch.AddBinding("<Gamepad>/buttonEast");
        }

        private void BindSprint()
        {
            ClearBindings(sprint);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");
        }

        private static void ClearBindings(InputAction action)
        {
            while (action.bindings.Count > 0)
            {
                action.ChangeBinding(0).Erase();
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
