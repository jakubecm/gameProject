using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    public enum TerminalState
    {
        Idle,
        Entering,
        InUse,
        Leaving,
    }

    /// <summary>
    /// A computer the player can sit down at (task 2.4). Pressing E on it switches it on if it is off and makes the
    /// camera glide to the screen; the player stands still and the mouse is free. The screen is a world-space canvas, so
    /// the desktop on it is clicked with the mouse; the Start menu shuts the computer down. Escape stands up. The game
    /// keeps running while the player is at a terminal, except while a document or a web article is open.
    /// The look of the computer (task 2.10) is the prefab's business: this component needs only the view point, the
    /// canvas on the screen and the data files.
    /// </summary>
    public class Terminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform viewPoint;
        [SerializeField] private Canvas screen;
        [Tooltip("A second monitor that only shows the wallpaper while the computer is on and is black otherwise. Optional.")]
        [SerializeField] private Canvas secondScreen;
        [SerializeField] private DesktopTheme theme;
        [Tooltip("Whether the computer is on when the game starts.")]
        [SerializeField] private bool startsOn;
        [Tooltip("Which wallpaper of the theme this computer shows. -1: a random one every time the game starts.")]
        [SerializeField] private int wallpaperIndex = -1;

        [Header("Programs")]
        [Tooltip("The pages of the browser (web_pages.json).")]
        [SerializeField] private TextAsset webFile;
        [Tooltip("The documents of the knowledge base (knowledge_base.json).")]
        [SerializeField] private TextAsset loreFile;
        [SerializeField] private TextAsset passwordWordsFile;

        [Header("View")]
        [Tooltip("Seconds the camera takes to reach the screen, and to go back.")]
        [SerializeField] private float transitionSeconds = 0.6f;
        [SerializeField] private float viewFieldOfView = 45f;
        [Tooltip("How much of the view the screen fills when seated (0.9 = the whole screen with a margin around it). The camera stops this close, or at the view point if that is closer.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float screenFill = 0.9f;

        // Computers in one scene take the wallpapers one after another from a random start, so no two look the same
        // (until the pictures run out). Reset for every play session.
        private static int nextWallpaper = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWallpapers() => nextWallpaper = -1;

        private static int NextWallpaperKey()
        {
            if (nextWallpaper < 0) nextWallpaper = UnityEngine.Random.Range(0, 1 << 20);
            return nextWallpaper++;
        }

        private Desktop desktop;
        private GameObject secondWallpaper;
        private Camera playerCamera;
        private Interactor interactor;
        private InteractionPrompt prompt;
        private HeadBob headBob;
        private PlayerInputReader reader;
        private GameObject hint;
        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;
        private float restFieldOfView;
        private float blend;
        private float target;

        public TerminalState State { get; private set; }

        /// <summary>The wallpaper picture on the desktop, or null for the plain colour.</summary>
        public Texture2D Wallpaper { get; private set; }

        public bool IsInUse => State != TerminalState.Idle;

        public Desktop Desktop => desktop;

        public BrowserApp Browser { get; private set; }

        public LoreApp Knowledge { get; private set; }

        public PasswordApp Password { get; private set; }

        /// <summary>The prompt over the computer: it offers to switch it on while it is off.</summary>
        public string Prompt => desktop != null && desktop.Power == PowerState.Off ? DesktopTexts.TurnOnPrompt : DesktopTexts.UsePrompt;

        public float HoldDuration => 0f;

        /// <summary>The player sat down at the computer.</summary>
        public event Action Entered;

        /// <summary>The player is back on their feet.</summary>
        public event Action Exited;

        /// <summary>The computer went off, started booting, is on, or is shutting down.</summary>
        public event Action<PowerState> PowerChanged;

        /// <summary>A clue minigame ended; the clue system listens to this.</summary>
        public event Action<MinigameResult> MinigameCompleted;

        private void Awake()
        {
            Build();
        }

        private void OnDisable()
        {
            if (IsInUse) Finish();
        }

        private void OnDestroy()
        {
            desktop?.CloseAll();
        }

        private void Update()
        {
            // Real time: the boot and the clock go on while the game is paused by a document.
            desktop?.Tick(Time.unscaledDeltaTime);

            var keyboard = Keyboard.current;
            if (State == TerminalState.InUse && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Leave();
            }
            if (State == TerminalState.InUse) UpdatePointer();
        }

        /// <summary>
        /// Over the screen the pointer is drawn on the desktop (it gets smaller and larger with the screen, like the rest) and the system's one is
        /// hidden; beside it the system's pointer is shown, so it is never lost.
        /// </summary>
        private void UpdatePointer()
        {
            var mouse = Mouse.current;
            bool over = false;
            if (desktop != null && screen != null && playerCamera != null && mouse != null)
            {
                var rect = (RectTransform)screen.transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mouse.position.ReadValue(), playerCamera, out var local) && rect.rect.Contains(local))
                {
                    desktop.SetPointer(local);
                    over = true;
                }
            }
            if (!over) desktop?.SetPointer(null);
            Cursor.visible = !over;
        }

        private void LateUpdate()
        {
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>Makes the desktop and its programs on the screen. Called once; the tests call it by hand.</summary>
        public void Build()
        {
            if (desktop != null || screen == null) return;

            var look = theme != null ? theme : DesktopTheme.CreateDefault();
            Wallpaper = look.WallpaperFor(wallpaperIndex >= 0 ? wallpaperIndex : NextWallpaperKey());
            desktop = new Desktop((RectTransform)screen.transform, look, startsOn, wallpaperPicture: Wallpaper);
            BuildSecondScreen(look);
            desktop.PowerChanged += state =>
            {
                ShowSecondScreen(state);
                PowerChanged?.Invoke(state);
            };

            if (webFile != null)
            {
                Browser = new BrowserApp(WebLibrary.Parse(webFile.text));
                desktop.AddApp(Browser);
            }
            else
            {
                Debug.LogWarning("Terminal: no web page file assigned; the Browser is not on the desktop.", this);
            }

            Knowledge = new LoreApp(loreFile != null ? LoreLibrary.Parse(loreFile.text) : null);
            desktop.AddApp(Knowledge);

            if (passwordWordsFile != null)
            {
                Password = new PasswordApp(PasswordWordList.Parse(passwordWordsFile.text));
                Password.Completed += result => MinigameCompleted?.Invoke(result);
                desktop.AddApp(Password);
            }
            else
            {
                Debug.LogWarning("Terminal: no password word file assigned; the Security program is not on the desktop.", this);
            }
        }

        // The second monitor: black, with the wallpaper over it while the computer is on (not while it boots or shuts down).
        private void BuildSecondScreen(DesktopTheme look)
        {
            if (secondScreen == null) return;

            var rect = (RectTransform)secondScreen.transform;
            rect.sizeDelta = new Vector2(Desktop.Width, Desktop.Height);
            for (int i = rect.childCount - 1; i >= 0; i--) UiFactory.Dispose(rect.GetChild(i).gameObject);
            var black = UiFactory.Image("Black", rect, Color.black, false);
            UiFactory.Fill(black.rectTransform);
            secondWallpaper = Desktop.PaintWallpaper(rect, look, Wallpaper).gameObject;
            ShowSecondScreen(desktop.Power);
        }

        private void ShowSecondScreen(PowerState state)
        {
            if (secondWallpaper != null) secondWallpaper.SetActive(state == PowerState.On);
        }

        /// <summary>True while the second monitor shows the wallpaper (the computer is on).</summary>
        public bool SecondScreenLit => secondWallpaper != null && secondWallpaper.activeSelf;

        /// <summary>Pressing E on the computer: it is switched on if it was off, and the player sits down.</summary>
        public void Interact(Interactor user)
        {
            Build();
            desktop?.PowerOn();
            Enter(user);
        }

        /// <summary>Sits the player down: the camera starts moving to the screen, the player stops, the mouse is freed.</summary>
        public void Enter(Interactor user)
        {
            if (State != TerminalState.Idle || user == null) return;

            playerCamera = user.GetComponent<Camera>();
            if (playerCamera == null || viewPoint == null)
            {
                Debug.LogError("Terminal: needs the player's camera and a view point.", this);
                return;
            }
            Build();
            if (desktop == null)
            {
                Debug.LogError("Terminal: has no screen canvas, so there is nothing to sit down at.", this);
                return;
            }

            interactor = user;
            reader = user.GetComponentInParent<PlayerInputReader>();
            headBob = user.GetComponent<HeadBob>();

            // The bob and the interaction prompt would fight with the camera and with the screen.
            interactor.enabled = false;
            prompt = user.GetComponent<InteractionPrompt>();
            if (prompt != null) prompt.enabled = false;
            if (headBob != null) headBob.enabled = false;
            if (reader != null) reader.InputEnabled = false;

            restLocalPosition = playerCamera.transform.localPosition;
            restLocalRotation = playerCamera.transform.localRotation;
            restFieldOfView = playerCamera.fieldOfView;

            screen.worldCamera = playerCamera;
            PrepareInput();
            ShowHint();
            desktop.SetSeated(true);
            desktop.Resume();

            blend = 0f;
            target = 1f;
            State = TerminalState.Entering;
            Entered?.Invoke();
        }

        /// <summary>Stands up: the camera goes back to the player's eyes. The windows stay open for the next visit.</summary>
        public void Leave()
        {
            if (State != TerminalState.Entering && State != TerminalState.InUse) return;

            desktop.Suspend();
            desktop.SetPointer(null);
            Cursor.visible = true;
            target = 0f;
            State = TerminalState.Leaving;
        }

        /// <summary>Moves the camera on its way to or from the screen. Called every frame; the tests call it with a time of their own.</summary>
        public void Advance(float deltaTime)
        {
            if (State == TerminalState.Idle || playerCamera == null) return;

            blend = Mathf.MoveTowards(blend, target, deltaTime / Mathf.Max(0.05f, transitionSeconds));
            ApplyCameraPose();

            if (target >= 1f)
            {
                State = blend >= 1f ? TerminalState.InUse : TerminalState.Entering;
            }
            else if (blend <= 0f)
            {
                Finish();
            }
        }

        /// <summary>
        /// How far from a screen the eye has to be for the screen to fill a share of the view: the distance at which both its
        /// height and its width fit, for a camera with this vertical field of view (degrees) and aspect ratio.
        /// </summary>
        public static float ViewDistanceFor(float screenWidth, float screenHeight, float fieldOfView, float aspect, float fill)
        {
            float tangent = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Clamp(fill, 0.1f, 1f);
            float forHeight = screenHeight * 0.5f / tangent;
            float forWidth = screenWidth * 0.5f / (tangent * Mathf.Max(0.1f, aspect));
            return Mathf.Max(forHeight, forWidth);
        }

        /// <summary>
        /// Where the camera sits when seated: on the view point's axis, in front of the middle of the screen, close enough
        /// that the screen fills the view (see <see cref="screenFill"/>) for the camera's own aspect ratio, so the text is as
        /// big as it can be on any display. Never farther away than the view point.
        /// </summary>
        public Vector3 ViewPosition
        {
            get
            {
                if (viewPoint == null) return transform.position;
                if (screen == null || playerCamera == null) return viewPoint.position;

                var rect = (RectTransform)screen.transform;
                Vector3 center = rect.TransformPoint(rect.rect.center);
                Vector3 toEye = -viewPoint.forward;
                float current = Vector3.Dot(viewPoint.position - center, toEye);
                float size = rect.lossyScale.x;
                float wanted = ViewDistanceFor(rect.rect.width * size, rect.rect.height * rect.lossyScale.y, viewFieldOfView, playerCamera.aspect, screenFill);
                return center + toEye * Mathf.Min(wanted, Mathf.Max(0.1f, current));
            }
        }

        private void ApplyCameraPose()
        {
            float eased = Mathf.SmoothStep(0f, 1f, blend);
            var cameraTransform = playerCamera.transform;
            var parent = cameraTransform.parent;
            Vector3 restPosition = parent != null ? parent.TransformPoint(restLocalPosition) : restLocalPosition;
            Quaternion restRotation = parent != null ? parent.rotation * restLocalRotation : restLocalRotation;

            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(restPosition, ViewPosition, eased),
                Quaternion.Slerp(restRotation, viewPoint.rotation, eased));
            playerCamera.fieldOfView = Mathf.Lerp(restFieldOfView, viewFieldOfView, eased);
        }

        // Back on the player's feet: everything that was switched off for the screen comes back.
        private void Finish()
        {
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = restLocalPosition;
                playerCamera.transform.localRotation = restLocalRotation;
                playerCamera.fieldOfView = restFieldOfView;
            }
            if (interactor != null) interactor.enabled = true;
            if (prompt != null) prompt.enabled = true;
            desktop?.SetPointer(null);
            if (headBob != null) headBob.enabled = true;
            if (reader != null) reader.InputEnabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            HideHint();
            desktop?.SetSeated(false);
            desktop?.Suspend();

            blend = 0f;
            target = 0f;
            State = TerminalState.Idle;
            Exited?.Invoke();
        }

        private void PrepareInput()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Clicking a world-space canvas needs an event system with the Input System's UI module.
            if (!Application.isPlaying || FindAnyObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            // WASD and the arrows must not move a selection around the desktop.
            eventSystem.GetComponent<EventSystem>().sendNavigationEvents = false;
        }

        private void ShowHint()
        {
            hint = new GameObject("TerminalHint", typeof(Canvas));
            var canvas = hint.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var label = UiFactory.Text("Hint", hint.transform, DesktopTexts.LeaveHint, 22, new Color(1f, 1f, 1f, 0.85f), TextAnchor.LowerRight);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(24f, 18f);
            label.rectTransform.offsetMax = new Vector2(-28f, -18f);
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        }

        private void HideHint()
        {
            if (hint != null) UiFactory.Dispose(hint);
            hint = null;
        }
    }
}
