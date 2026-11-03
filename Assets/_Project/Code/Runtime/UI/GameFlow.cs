using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    public enum GameState
    {
        /// <summary>The welcome screen over the flying camera (also its settings page).</summary>
        MainMenu,
        Playing,
        /// <summary>Escape: the game is frozen behind the pause screen (or the settings page opened from it).</summary>
        Paused,
    }

    /// <summary>
    /// Runs the game from the first frame to the end: after the level is built the main menu fades in over the flying camera; PLAY fades to
    /// the lift and hands the input to the player; Escape pauses. It builds all the UI itself (<see cref="UiScreen"/>s on one canvas) and
    /// swaps the menu camera and the player camera.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private LevelBuilder levelBuilder;
        [Tooltip("Skip the menu and start playing at once (for testing).")]
        [SerializeField] private bool startPlaying;

        private Canvas canvas;
        private MainMenuScreen mainMenu;
        private PauseScreen pause;
        private SettingsScreen settings;
        private Image fader;
        private MenuCameraRig menuCamera;
        private Camera playerCamera;
        private Interactor interactor;
        private InteractionPrompt prompt;
        private PlayerInputReader input;
        private UiScreen settingsReturn;
        private bool busy;

        public GameState State { get; private set; } = GameState.MainMenu;

        public event Action<GameState> StateChanged;

        private void Awake()
        {
            // Before the level builder's first frame: nothing of the game is shown until the menu says so.
            EnsureEventSystem();
            BuildUi();
            FindPlayer();
            menuCamera = new GameObject("Menu Camera").AddComponent<MenuCameraRig>();
            menuCamera.transform.SetParent(transform, false);
            fader.color = Color.black;
        }

        private void Start()
        {
            // Not in Awake: the player's own scripts have to be set up first.
            SetPlayerActive(false);
            StartCoroutine(Boot());
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.transform.SetParent(transform, false);
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // The order matters: later ones are drawn on top. The fader covers every page.
            mainMenu = UiScreen.Create<MainMenuScreen>(canvas.transform);
            settings = UiScreen.Create<SettingsScreen>(canvas.transform);
            pause = UiScreen.Create<PauseScreen>(canvas.transform);
            fader = UiKit.Box(canvas.transform, "Fader", Color.black);
            UiKit.Stretch(fader.rectTransform);

            mainMenu.PlayClicked += () => { if (!busy) StartCoroutine(StartGame()); };
            mainMenu.SettingsClicked += () => OpenSettings(mainMenu);
            mainMenu.ExitClicked += QuitGame;
            pause.ResumeClicked += Resume;
            pause.SettingsClicked += () => OpenSettings(pause);
            pause.ExitClicked += QuitGame;
            settings.BackClicked += CloseSettings;
        }

        private void FindPlayer()
        {
            var player = levelBuilder != null ? levelBuilder.Player : null;
            if (player == null) return;

            playerCamera = player.GetComponentInChildren<Camera>(true);
            interactor = player.GetComponentInChildren<Interactor>(true);
            prompt = player.GetComponentInChildren<InteractionPrompt>(true);
            input = player.GetComponentInChildren<PlayerInputReader>(true);
            if (input != null)
            {
                // Escape belongs to the pause screen now.
                input.EscapeFreesCursor = false;
            }
        }

        // The player's view, hands and input on (playing), or off (menus, where the menu camera renders).
        private void SetPlayerActive(bool active)
        {
            if (playerCamera != null) playerCamera.enabled = active;
            if (interactor != null) interactor.enabled = active;
            if (prompt != null) prompt.SetVisible(active);
            if (input != null) input.InputEnabled = active;
            if (menuCamera != null) menuCamera.Rendering = !active;
            SetCursorLocked(active);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void SetState(GameState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        // ---------------------------------------------------------------- start

        private IEnumerator Boot()
        {
            float waited = 0f;
            while (!levelBuilder.HasLevel && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            // The level builder has finished its frame: its lights and rooms exist.
            yield return null;

            menuCamera.Configure(levelBuilder);
            if (startPlaying)
            {
                SetPlayerActive(true);
                SetState(GameState.Playing);
                yield return FadeTo(0f, 0.6f);
            }
            else
            {
                // Straight to the main screen, out of black.
                ShowMainMenu();
                yield return FadeTo(0f, 0.8f);
            }
        }

        private void ShowMainMenu()
        {
            SetState(GameState.MainMenu);
            SetCursorLocked(false);
            mainMenu.Show();
        }

        private IEnumerator StartGame()
        {
            busy = true;
            yield return FadeTo(1f, 0.6f);
            mainMenu.HideNow();
            settings.HideNow();
            SetPlayerActive(true);
            SetState(GameState.Playing);
            yield return FadeTo(0f, 0.9f);
            busy = false;
        }

        private IEnumerator FadeTo(float alpha, float seconds)
        {
            float from = fader.color.a;
            fader.raycastTarget = true;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                fader.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, alpha, UiKit.Ease(t / seconds)));
                yield return null;
            }
            fader.color = new Color(0f, 0f, 0f, alpha);
            fader.raycastTarget = alpha > 0.01f;
        }

        // A cut in the flying camera: through black to the next shot. The camera keeps moving while it fades out (the fade fits in the
        // rig's cut lead) and the next shot is already moving when it appears.
        private IEnumerator CutToNextShot()
        {
            busy = true;
            yield return FadeTo(1f, MenuCameraRig.CutLead * 0.8f);
            menuCamera.NextShot();
            yield return FadeTo(0f, 0.6f);
            busy = false;
        }

        // ---------------------------------------------------------------- pages

        private void OpenSettings(UiScreen from)
        {
            settingsReturn = from;
            from.Hide();
            settings.Show();
        }

        private void CloseSettings()
        {
            settings.Hide();
            (settingsReturn ?? mainMenu).Show();
        }

        private void Pause()
        {
            SetState(GameState.Paused);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (input != null) input.InputEnabled = false;
            if (interactor != null) interactor.enabled = false;
            SetCursorLocked(false);
            pause.Show();
        }

        private void Resume()
        {
            settings.HideNow();
            pause.Hide();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (input != null) input.InputEnabled = true;
            if (interactor != null) interactor.enabled = true;
            SetCursorLocked(true);
            SetState(GameState.Playing);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- input

        private void Update()
        {
            if (menuCamera != null && !busy && (State == GameState.MainMenu) && menuCamera.ShotFinished)
            {
                StartCoroutine(CutToNextShot());
            }

            if (!BackPressed() || busy) return;

            switch (State)
            {
                case GameState.Playing:
                    Pause();
                    break;
                case GameState.Paused:
                    if (settings.IsOpen) CloseSettings();
                    else Resume();
                    break;
                case GameState.MainMenu:
                    if (settings.IsOpen) CloseSettings();
                    break;
            }
        }

        private static bool BackPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) return true;
            var gamepad = Gamepad.current;
            return gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame);
        }
    }
}
