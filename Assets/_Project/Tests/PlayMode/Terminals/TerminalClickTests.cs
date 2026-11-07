#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColdShutdown.Tests.PlayMode.Terminals
{
    /// <summary>
    /// Plays Sandbox_C with a simulated mouse: sits down at a computer that is off, clicks through the desktop on the
    /// world-space screen, reads a news article, plays the password minigame and shuts the computer down. This is the
    /// one test that can see whether the screen really can be clicked: it goes through the camera, the world-space
    /// canvas, the event system and the Input System, like a real mouse does.
    /// </summary>
    public class TerminalClickTests
    {
        private Mouse mouse;
        private InputSettings.BackgroundBehavior backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode editorBehavior;

        [SetUp]
        public void SetUp()
        {
            // A batch run has no focused Game view; without this the Input System drops the events.
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>("TestMouse");
        }

        [TearDown]
        public void TearDown()
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator ClickingTheScreenWithTheMouse_WorksFromSwitchingOnToShuttingDown()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Sandbox_C.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var terminal = UnityEngine.Object.FindObjectsByType<Terminal>().OrderBy(found => found.transform.position.x).First();
            var interactor = UnityEngine.Object.FindAnyObjectByType<Interactor>();
            var camera = interactor.GetComponent<Camera>();
            Assert.AreEqual(PowerState.Off, terminal.Desktop.Power, "the computer starts switched off");
            Assert.AreEqual("Turn on computer", terminal.Prompt);

            // Pressing E: switches it on and sits the player down.
            ((IInteractable)terminal).Interact(interactor);
            yield return WaitUntil(() => terminal.State == TerminalState.InUse && terminal.Desktop.Power == PowerState.On, 15f, "boot and sit down");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(), "an event system for the clicks");

            // The browser: start page, news (menu, story cards), an article (which pauses the game), back.
            yield return Click(camera, terminal.Desktop.SystemRoot.Find("Icons/Icon_browser/Tile"));
            Assert.IsTrue(terminal.Desktop.IsOpen(terminal.Browser), "clicking the Browser icon opens it");
            var page = terminal.Desktop.WindowOf(terminal.Browser).Content.Find("Scroll/Viewport/Page");

            yield return Click(camera, page.Find("Tiles/Link_news_front"));
            Assert.AreEqual("news_front", terminal.Browser.Current.id);
            yield return null;
            yield return Click(camera, page.Find("Menu/Nav_news_world"));
            Assert.AreEqual("news_world", terminal.Browser.Current.id, "a tab of the news menu");
            yield return null;
            yield return Click(camera, page.Find("Menu/Nav_news_front"));
            Assert.AreEqual("news_front", terminal.Browser.Current.id);
            yield return null;
            yield return Click(camera, page.Find("Front/Side/Card_news_law"));
            Assert.AreEqual("news_law", terminal.Browser.Current.id, "a click on a story card");
            Assert.IsTrue(GamePause.IsPaused, "reading an article pauses the game");

            yield return Click(camera, terminal.Desktop.WindowOf(terminal.Browser).Content.Find("Toolbar/Back"));
            Assert.AreEqual("news_front", terminal.Browser.Current.id);
            Assert.IsFalse(GamePause.IsPaused, "the headlines are not an article");

            yield return Click(camera, terminal.Desktop.WindowOf(terminal.Browser).CloseButton.transform);
            Assert.IsFalse(terminal.Desktop.IsOpen(terminal.Browser), "the X closes the window");

            // The password minigame: one click on a word.
            yield return Click(camera, terminal.Desktop.SystemRoot.Find("Icons/Icon_password/Tile"));
            Assert.IsTrue(terminal.Desktop.IsOpen(terminal.Password));
            var firstWord = terminal.Password.Puzzle.Candidates[0];
            yield return Click(camera, terminal.Desktop.WindowOf(terminal.Password).Content.Find("Word_" + firstWord));
            Assert.AreEqual(1, terminal.Password.Puzzle.Guesses.Count, "a click on a word is a guess");

            // Shutting down from the Start menu.
            yield return Click(camera, terminal.Desktop.SystemRoot.Find("Taskbar/Bar/Start"));
            yield return Click(camera, terminal.Desktop.SystemRoot.Find("StartMenu/ShutDown"));
            yield return WaitUntil(() => terminal.Desktop.Power == PowerState.Off, 10f, "shut down");
            Assert.AreEqual(0, terminal.Desktop.Windows.Count);

            // And on again by clicking the black screen.
            yield return Click(camera, terminal.GetComponentInChildren<Canvas>().transform.Find("Blackout"));
            yield return WaitUntil(() => terminal.Desktop.Power == PowerState.On, 10f, "boot again");

            terminal.Leave();
            yield return WaitUntil(() => terminal.State == TerminalState.Idle, 10f, "stand up");
            Assert.AreEqual("Use computer", terminal.Prompt);
        }

        // Moves the mouse onto the middle of a UI element as the player's camera sees it, presses and releases the left button.
        private IEnumerator Click(Camera camera, Transform target)
        {
            Assert.IsNotNull(target, "the element to click does not exist");
            var rect = (RectTransform)target;
            Vector2 point = camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));

            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float seconds, string what)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
            Assert.IsTrue(condition(), $"timed out waiting for: {what}");
        }
    }
}
#endif
