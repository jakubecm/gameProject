using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColdShutdown.Core;
using ColdShutdown.Editor;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Tests.EditMode.Terminals
{
    /// <summary>
    /// Sits the real player prefab down at a terminal: the camera goes to the screen and back, the player stops and
    /// is released, a computer that is off is switched on, the game is paused only while something is being read.
    /// The camera is advanced by hand (no frames run in EditMode). Also checks the terminal prefab that
    /// Tools/Cold Shutdown/Create Terminal Prefab builds.
    /// </summary>
    public class TerminalTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string TerminalPrefabPath = "Assets/_Project/Prefabs/Terminals/Terminal.prefab";
        private const string KnowledgePath = "Assets/_Project/Data/Lore/knowledge_base.json";
        private const string WebPath = "Assets/_Project/Data/Web/web_pages.json";
        private const string WordsPath = "Assets/_Project/Data/Minigames/password_words.json";

        private readonly List<GameObject> created = new List<GameObject>();
        private GameObject player;
        private Interactor interactor;
        private PlayerInputReader reader;
        private Camera playerCamera;
        private Terminal terminal;
        private Transform viewPoint;

        [SetUp]
        public void SetUp()
        {
            player = Track((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath)));
            interactor = player.GetComponentInChildren<Interactor>(true);
            reader = player.GetComponent<PlayerInputReader>();
            playerCamera = interactor.GetComponent<Camera>();

            terminal = CreateTerminal(startsOn: true);
            viewPoint = terminal.transform.Find("ViewPoint");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            created.Clear();
            var hint = GameObject.Find("TerminalHint");
            if (hint != null) Object.DestroyImmediate(hint);
            Time.timeScale = 1f;
        }

        // A computer with a view point, a screen canvas and the three data files, on or off.
        private Terminal CreateTerminal(bool startsOn)
        {
            var computer = Track(new GameObject("Computer"));
            computer.transform.position = new Vector3(0f, 0f, 3f);
            var component = computer.AddComponent<Terminal>();

            var view = new GameObject("ViewPoint").transform;
            view.SetParent(computer.transform, false);
            view.localPosition = new Vector3(0.3f, 1.2f, 0.6f);
            view.localRotation = Quaternion.Euler(10f, 180f, 0f);

            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Canvas));
            screen.transform.SetParent(computer.transform, false);

            var serialized = new SerializedObject(component);
            serialized.FindProperty("viewPoint").objectReferenceValue = view;
            serialized.FindProperty("screen").objectReferenceValue = screen.GetComponent<Canvas>();
            serialized.FindProperty("startsOn").boolValue = startsOn;
            serialized.FindProperty("webFile").objectReferenceValue = new TextAsset(File.ReadAllText(WebPath));
            serialized.FindProperty("loreFile").objectReferenceValue = new TextAsset(File.ReadAllText(KnowledgePath));
            serialized.FindProperty("passwordWordsFile").objectReferenceValue = new TextAsset(File.ReadAllText(WordsPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            component.Build();
            return component;
        }

        [Test]
        public void TheDesktop_HasAllThreePrograms()
        {
            Assert.IsNotNull(terminal.Desktop);
            Assert.AreEqual(3, terminal.Desktop.Apps.Count);
            Assert.IsNotNull(terminal.Browser);
            Assert.IsNotNull(terminal.Knowledge);
            Assert.IsNotNull(terminal.Password);
        }

        // ---------------------------------------------------------------- walking up

        [Test]
        public void SittingDown_StopsThePlayer_AndStartsTheCameraMoving()
        {
            var entered = 0;
            terminal.Entered += () => entered++;
            Assert.IsTrue(reader.InputEnabled);
            Assert.IsTrue(interactor.enabled);

            terminal.Enter(interactor);

            Assert.AreEqual(TerminalState.Entering, terminal.State);
            Assert.IsTrue(terminal.IsInUse);
            Assert.IsFalse(reader.InputEnabled, "the player stands still");
            Assert.IsFalse(interactor.enabled, "no interaction prompt over the screen");
            Assert.IsFalse(playerCamera.GetComponent<HeadBob>().enabled, "no head bob over the screen");
            Assert.AreEqual(1, entered);
        }

        [Test]
        public void TheCamera_GlidesToTheScreen_AndLooksAtIt()
        {
            terminal.Enter(interactor);
            var start = playerCamera.transform.position;

            terminal.Advance(0.3f);
            var halfway = playerCamera.transform.position;
            Assert.AreEqual(TerminalState.Entering, terminal.State);
            Assert.Greater(Vector3.Distance(start, halfway), 0.05f, "on its way");
            Assert.Greater(Vector3.Distance(halfway, terminal.ViewPosition), 0.05f, "not there yet");

            terminal.Advance(1f);
            Assert.AreEqual(TerminalState.InUse, terminal.State);
            Assert.AreEqual(0f, Vector3.Distance(playerCamera.transform.position, terminal.ViewPosition), 1e-3f);
            Assert.AreEqual(0f, Quaternion.Angle(playerCamera.transform.rotation, viewPoint.rotation), 0.05f);
            Assert.AreEqual(45f, playerCamera.fieldOfView, 1e-3f, "narrower for reading");
            Assert.AreEqual(playerCamera, terminal.GetComponentInChildren<Canvas>().worldCamera, "clicks on the screen are cast from the player's camera");
        }

        [Test]
        public void TheCamera_StaysOnTheScreen_WhileInUse()
        {
            terminal.Enter(interactor);
            terminal.Advance(2f);

            player.transform.position += new Vector3(0.2f, 0f, 0.1f);
            terminal.Advance(0.1f);

            Assert.AreEqual(0f, Vector3.Distance(playerCamera.transform.position, terminal.ViewPosition), 1e-3f);
        }

        [Test]
        public void Standing_UpGivesEverythingBack()
        {
            var left = 0;
            terminal.Exited += () => left++;
            var restPosition = playerCamera.transform.localPosition;
            var restRotation = playerCamera.transform.localRotation;
            float restFieldOfView = playerCamera.fieldOfView;
            terminal.Enter(interactor);
            terminal.Advance(2f);

            terminal.Leave();
            Assert.AreEqual(TerminalState.Leaving, terminal.State);
            Assert.IsFalse(reader.InputEnabled, "still frozen while the camera travels back");

            terminal.Advance(0.3f);
            Assert.AreEqual(TerminalState.Leaving, terminal.State);
            terminal.Advance(2f);

            Assert.AreEqual(TerminalState.Idle, terminal.State);
            Assert.IsFalse(terminal.IsInUse);
            Assert.IsTrue(reader.InputEnabled);
            Assert.IsTrue(interactor.enabled);
            Assert.IsTrue(playerCamera.GetComponent<HeadBob>().enabled);
            Assert.AreEqual(0f, Vector3.Distance(restPosition, playerCamera.transform.localPosition), 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(restRotation, playerCamera.transform.localRotation), 0.01f);
            Assert.AreEqual(restFieldOfView, playerCamera.fieldOfView, 1e-3f);
            Assert.AreEqual(1, left);
            Assert.IsNull(GameObject.Find("TerminalHint"), "the Esc hint is gone");
        }

        [Test]
        public void AlreadyAtAComputer_NothingHappensWhenAskingAgain()
        {
            var entered = 0;
            terminal.Entered += () => entered++;
            terminal.Enter(interactor);
            terminal.Enter(interactor);
            terminal.Advance(2f);
            terminal.Enter(interactor);

            Assert.AreEqual(1, entered);
            Assert.AreEqual(TerminalState.InUse, terminal.State);
        }

        [Test]
        public void ATerminalThatIsNotSetUp_LeavesThePlayerAlone()
        {
            var bare = Track(new GameObject("Bare", typeof(Terminal)));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("needs the player's camera and a view point"));

            bare.GetComponent<Terminal>().Enter(interactor);

            Assert.AreEqual(TerminalState.Idle, bare.GetComponent<Terminal>().State);
            Assert.IsTrue(reader.InputEnabled);
            Assert.IsTrue(interactor.enabled);
        }

        // ---------------------------------------------------------------- the power

        [Test]
        public void ThePromptOffersToSwitchOn_AComputerThatIsOff()
        {
            var off = CreateTerminal(startsOn: false);
            Assert.AreEqual(PowerState.Off, off.Desktop.Power);
            Assert.AreEqual("Turn on computer", off.Prompt);
            Assert.AreEqual("Use computer", terminal.Prompt);
            Assert.AreEqual(0f, off.HoldDuration, "a press is enough");
        }

        [Test]
        public void PressingE_OnTheComputer_SitsThePlayerDown()
        {
            // The Interactor looks for an IInteractable on whatever it hits, and the Terminal is one.
            Assert.IsInstanceOf<IInteractable>(terminal);

            ((IInteractable)terminal).Interact(interactor);

            Assert.AreEqual(TerminalState.Entering, terminal.State);
            Assert.AreEqual(PowerState.On, terminal.Desktop.Power, "an on computer stays on");
        }

        [Test]
        public void PressingE_OnAComputerThatIsOff_SwitchesItOn_AndSitsDown()
        {
            var off = CreateTerminal(startsOn: false);
            var states = new List<PowerState>();
            off.PowerChanged += states.Add;

            ((IInteractable)off).Interact(interactor);

            Assert.AreEqual(TerminalState.Entering, off.State);
            Assert.AreEqual(PowerState.Booting, off.Desktop.Power);
            off.Desktop.Tick(Desktop.BootSeconds + 0.1f);
            Assert.AreEqual(PowerState.On, off.Desktop.Power);
            CollectionAssert.AreEqual(new[] { PowerState.Booting, PowerState.On }, states);
        }

        [Test]
        public void ShuttingDown_FromTheStartMenu_LeavesThePlayerAtABlackScreen_ThatCanBeClickedOn()
        {
            terminal.Enter(interactor);
            terminal.Advance(2f);

            var screen = terminal.GetComponentInChildren<Canvas>().transform;
            terminal.Desktop.SystemRoot.Find("Taskbar/Bar/Start").GetComponent<Button>().onClick.Invoke();
            terminal.Desktop.SystemRoot.Find("StartMenu/ShutDown").GetComponent<Button>().onClick.Invoke();
            terminal.Desktop.Tick(Desktop.ShutdownSeconds + 0.1f);

            Assert.AreEqual(PowerState.Off, terminal.Desktop.Power);
            Assert.AreEqual(TerminalState.InUse, terminal.State, "the player is still sitting there");
            Assert.IsTrue(screen.Find("Blackout/Hint").gameObject.activeSelf, "the black screen says how to switch it on");

            screen.Find("Blackout").GetComponent<Button>().onClick.Invoke();
            terminal.Desktop.Tick(Desktop.BootSeconds + 0.1f);
            Assert.AreEqual(PowerState.On, terminal.Desktop.Power);
        }

        // ---------------------------------------------------------------- pausing

        [Test]
        public void ReadingADocument_PausesTheGame_OnlyWhileAtTheComputer()
        {
            terminal.Enter(interactor);
            terminal.Advance(2f);
            terminal.Desktop.Open(terminal.Knowledge);
            terminal.Knowledge.Select(0);
            Assert.IsTrue(GamePause.IsPaused);

            terminal.Leave();
            Assert.IsFalse(GamePause.IsPaused, "away from the computer the game runs, the infection goes on");

            terminal.Advance(2f);
            terminal.Enter(interactor);
            Assert.IsTrue(GamePause.IsPaused, "the document is still open when the player sits down again");
            terminal.Advance(2f);
            terminal.Leave();
            terminal.Advance(2f);
            Assert.IsFalse(GamePause.IsPaused);
        }

        [Test]
        public void TheCameraMoves_EvenWhileTheGameIsPaused()
        {
            // Reading pauses the game (time scale 0), but the camera is driven by real time: Advance is given real seconds.
            terminal.Enter(interactor);
            terminal.Advance(2f);
            terminal.Desktop.Open(terminal.Browser);
            terminal.Browser.Navigate("news_law");
            Assert.AreEqual(0f, Time.timeScale);

            terminal.Leave();
            terminal.Advance(2f);

            Assert.AreEqual(TerminalState.Idle, terminal.State);
            Assert.AreEqual(1f, Time.timeScale, "the way back released the pause");
        }

        [Test]
        public void ThePasswordResult_ReachesTheTerminalsEvent()
        {
            var results = new List<MinigameResult>();
            terminal.MinigameCompleted += results.Add;
            var password = terminal.Password;
            terminal.Desktop.Open(password);

            // The terminal's puzzle has the clock's seed, so its password is not known here: try words until it ends.
            foreach (string word in password.Puzzle.Candidates)
            {
                if (password.Puzzle.State != PasswordState.Playing) break;
                password.Pick(word);
            }

            Assert.AreEqual(1, results.Count, "once, when the puzzle ended");
            Assert.AreEqual("password", results[0].MinigameId);
            Assert.AreEqual(password.Puzzle.State == PasswordState.Won, results[0].Success);
        }

        [Test]
        public void SittingDown_HidesTheCrosshair_AndStandingUpBringsItBack()
        {
            var prompt = interactor.GetComponent<InteractionPrompt>();
            Assert.IsNotNull(prompt, "the player's camera has the interaction prompt (the centre dot)");
            Assert.IsTrue(prompt.enabled);

            terminal.Enter(interactor);
            Assert.IsFalse(prompt.enabled, "no dot over the screen");
            terminal.Leave();
            terminal.Advance(5f);
            Assert.IsTrue(prompt.enabled, "back on the player's feet the dot is back");
        }

        [Test]
        public void TheDesktop_DrawsThePointerWhereItIsTold_AndHidesItOtherwise()
        {
            terminal.Build();
            var desktop = terminal.Desktop;
            Assert.IsFalse(desktop.PointerVisible, "no pointer until the mouse is over the screen");

            desktop.SetPointer(new Vector2(120f, -40f));
            Assert.IsTrue(desktop.PointerVisible);
            Assert.AreEqual(new Vector2(120f, -40f), desktop.PointerPosition.Value);
            var last = desktop.SystemRoot.parent.GetChild(desktop.SystemRoot.parent.childCount - 1);
            Assert.AreEqual("Pointer", last.name, "drawn above everything, also above the black screen");
            Assert.IsFalse(last.GetComponent<UnityEngine.UI.RawImage>().raycastTarget, "it never catches a click");

            desktop.SetPointer(null);
            Assert.IsFalse(desktop.PointerVisible);
        }

        // ---------------------------------------------------------------- the workstation prop is the computer

        private const string WorkstationPath = "Assets/_Project/Prefabs/Props/Workstation.prefab";
        private const string DeskPath = "Assets/_Project/Prefabs/Props/Desk.prefab";

        private Terminal NewWorkstation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkstationPath);
            Assume.That(prefab, Is.Not.Null);
            var instance = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            var component = instance.GetComponent<Terminal>();
            Assert.IsNotNull(component, $"{WorkstationPath} has no Terminal. Run Tools/Cold Shutdown/Create Preview Scene.");
            return component;
        }

        private static Canvas Field(Terminal component, string name) => (Canvas)new SerializedObject(component).FindProperty(name).objectReferenceValue;

        private static Transform Monitor(Terminal component, string name) => component.transform.Find(name);

        [Test]
        public void TheWorkstationPrefab_IsAComputer_WiredUp()
        {
            var component = NewWorkstation();
            var serialized = new SerializedObject(component);
            var view = (Transform)serialized.FindProperty("viewPoint").objectReferenceValue;
            var screen = Field(component, "screen");
            var second = Field(component, "secondScreen");
            Assert.IsNotNull(view, "view point");
            Assert.IsNotNull(screen, "the canvas of the desktop");
            Assert.IsNotNull(second, "the canvas of the second monitor");
            Assert.IsNotNull(serialized.FindProperty("webFile").objectReferenceValue, "web file");
            Assert.IsNotNull(serialized.FindProperty("loreFile").objectReferenceValue, "knowledge base file");
            Assert.IsNotNull(serialized.FindProperty("passwordWordsFile").objectReferenceValue, "password word file");
            Assert.IsNotNull(serialized.FindProperty("theme").objectReferenceValue, "theme");
            Assert.IsFalse(serialized.FindProperty("startsOn").boolValue, "the computer is off until someone switches it on");

            Assert.AreEqual(RenderMode.WorldSpace, screen.renderMode);
            Assert.AreEqual(new Vector2(1280f, 720f), ((RectTransform)screen.transform).sizeDelta);
            Assert.IsNotNull(screen.GetComponent<GraphicRaycaster>(), "the mouse needs it to hit the desktop");
            Assert.IsNull(second.GetComponent<GraphicRaycaster>(), "the second monitor does not catch the mouse");
            Assert.IsNull(component.GetComponent<Interactable>(), "the Terminal is the interactable itself");
            Assert.AreEqual(1, component.GetComponentsInChildren<Collider>().Length, "one box for the interaction ray");

            component.Build();
            Assert.AreEqual("Turn on computer", component.Prompt);
        }

        [Test]
        public void TheTwoCanvases_LieOverTheirMonitorPictures_JustInFrontOfThem()
        {
            var component = NewWorkstation();
            foreach (var (field, monitor) in new[] { ("screen", "Monitor_Left"), ("secondScreen", "Monitor_Right") })
            {
                var canvas = Field(component, field);
                var picture = Monitor(component, monitor).Find("Monitor_Picture");
                var box = picture.GetComponent<MeshFilter>().sharedMesh.bounds;
                var corners = new Vector3[4];
                ((RectTransform)canvas.transform).GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = picture.InverseTransformPoint(corner);
                    Assert.GreaterOrEqual(local.x, box.min.x - 1e-3f, $"{field}: inside the picture, left");
                    Assert.LessOrEqual(local.x, box.max.x + 1e-3f, $"{field}: inside the picture, right");
                    Assert.GreaterOrEqual(local.y, box.min.y - 1e-3f, $"{field}: inside the picture, bottom");
                    Assert.LessOrEqual(local.y, box.max.y + 1e-3f, $"{field}: inside the picture, top");
                    Assert.Greater(local.z - box.max.z, 0f, $"{field}: in front of the picture");
                    Assert.Less(local.z - box.max.z, 0.005f, $"{field}: close to the picture");
                }
            }
        }

        [Test]
        public void TheSecondMonitor_ShowsOnlyTheWallpaper_WhileTheComputerIsOn_AndIsBlackOtherwise()
        {
            var component = NewWorkstation();
            component.Build();
            var desktop = component.Desktop;
            Assert.IsFalse(component.SecondScreenLit, "off: black");

            desktop.PowerOn();
            Assert.IsFalse(component.SecondScreenLit, "booting: still black");
            desktop.Tick(Desktop.BootSeconds + 0.1f);
            Assert.AreEqual(PowerState.On, desktop.Power);
            Assert.IsTrue(component.SecondScreenLit, "on: the wallpaper");

            var second = Field(component, "secondScreen");
            Assert.IsNotNull(second.transform.Find("Wallpaper"));
            Assert.IsNull(second.transform.Find("Wallpaper/Icons"), "nothing but the wallpaper");
            Assert.AreEqual(0, second.GetComponentsInChildren<Button>(true).Length);
            if (component.Wallpaper != null)
            {
                Assert.AreSame(component.Wallpaper, second.transform.Find("Wallpaper/Picture").GetComponent<RawImage>().texture, "the same picture as the desktop");
            }

            desktop.ShutDown();
            Assert.IsFalse(component.SecondScreenLit, "shutting down: black again");
            desktop.Tick(Desktop.ShutdownSeconds + 0.1f);
            Assert.IsFalse(component.SecondScreenLit);

            desktop.PowerOn();
            desktop.Tick(Desktop.BootSeconds + 0.1f);
            Assert.IsTrue(component.SecondScreenLit, "and the wallpaper comes back");
        }

        [Test]
        public void EveryWorkstationInTheRooms_IsAComputer()
        {
            int found = 0;
            foreach (string room in new[] { "Room_Office", "Room_SecurityOffice" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Rooms/{room}.prefab");
                Assume.That(prefab, Is.Not.Null);
                var instance = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                {
                    if (!child.name.StartsWith("Workstation")) continue;
                    found++;
                    Assert.IsNotNull(child.GetComponent<Terminal>(), $"{room}/{child.name} is not a computer");
                }
            }
            Assert.GreaterOrEqual(found, 3, "the office has one desk and the security office two");
        }

        [Test]
        public void TheSandboxTerminal_IsADeskWithAWorkstationOnIt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerminalPrefabPath);
            Assume.That(prefab, Is.Not.Null);
            var instance = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            Assert.IsNotNull(instance.transform.Find("Desk"));
            var workstation = instance.transform.Find("Workstation");
            Assert.IsNotNull(workstation);
            Assert.IsNotNull(workstation.GetComponent<Terminal>());
            Assert.AreEqual(Props.DeskHeight, workstation.localPosition.y, 1e-3f, "on the desk");
        }

        [Test]
        public void TheScreen_FacesTheViewer_SoItIsNotMirroredAndTheMouseCanHitIt()
        {
            // A world-space canvas is seen correctly from its back: its forward axis has to point the way the viewer looks.
            // Facing the other way mirrors everything on the screen, and Unity's UI ignores clicks on graphics that face away.
            var component = NewWorkstation();
            var view = (Transform)new SerializedObject(component).FindProperty("viewPoint").objectReferenceValue;
            var canvas = Field(component, "screen");

            Assert.Greater(Vector3.Dot(canvas.transform.forward, view.forward), 0.99f);
            Assert.AreEqual(canvas.GetComponent<GraphicRaycaster>().ignoreReversedGraphics, true, "the default that makes this matter");
        }

        [Test]
        public void TheViewPoint_LooksStraightAtTheWholeScreen_EvenOnANarrowDisplay()
        {
            var component = NewWorkstation();
            var serialized = new SerializedObject(component);
            var view = (Transform)serialized.FindProperty("viewPoint").objectReferenceValue;
            var canvas = Field(component, "screen");
            float fieldOfView = serialized.FindProperty("viewFieldOfView").floatValue;

            var corners = new Vector3[4];
            ((RectTransform)canvas.transform).GetWorldCorners(corners);
            float tangent = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
            {
                foreach (var corner in corners)
                {
                    var local = view.InverseTransformPoint(corner);
                    Assert.Greater(local.z, 0.2f, "the screen is in front of the camera");
                    Assert.Less(Mathf.Abs(local.y) / local.z, tangent, $"top and bottom visible at {aspect:0.00}");
                    Assert.Less(Mathf.Abs(local.x) / local.z, tangent * aspect, $"sides visible at {aspect:0.00}");
                }
            }
        }

        [Test]
        public void TwoComputersWithARandomWallpaper_ShowDifferentPictures()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerminalPrefabPath);
            Assume.That(prefab, Is.Not.Null);
            var computers = new List<Terminal>();
            for (int i = 0; i < 3; i++)
            {
                var terminal = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponentInChildren<Terminal>();
                terminal.Build();
                computers.Add(terminal);
            }

            Assume.That(computers[0].Wallpaper, Is.Not.Null, "the theme has wallpapers (run Update Desktop Art)");
            Assert.AreNotSame(computers[0].Wallpaper, computers[1].Wallpaper);
            Assert.AreNotSame(computers[1].Wallpaper, computers[2].Wallpaper);
        }

        [Test]
        public void TheViewDistance_FitsTheScreenToTheView_ForAnyDisplayShape()
        {
            // A 0.64 x 0.36 m screen, 45 degrees: the height decides on a wide display, the width on a narrow one.
            float wide = Terminal.ViewDistanceFor(0.64f, 0.36f, 45f, 16f / 9f, 1f);
            Assert.AreEqual(0.36f * 0.5f / Mathf.Tan(22.5f * Mathf.Deg2Rad), wide, 1e-3f);
            float narrow = Terminal.ViewDistanceFor(0.64f, 0.36f, 45f, 4f / 3f, 1f);
            Assert.Greater(narrow, wide, "a 4:3 display has to stand farther back to see the sides");
            Assert.Greater(Terminal.ViewDistanceFor(0.64f, 0.36f, 45f, 16f / 9f, 0.9f), wide, "a margin means farther");
        }

        [Test]
        public void TheSeatedCamera_IsCloserThanTheViewPoint_YetSeesTheWholeScreen()
        {
            var real = NewWorkstation();
            var serialized = new SerializedObject(real);
            var view = (Transform)serialized.FindProperty("viewPoint").objectReferenceValue;
            var canvas = Field(real, "screen");
            real.Enter(interactor);
            playerCamera.aspect = 16f / 9f;

            var position = real.ViewPosition;
            Assert.Less(Vector3.Distance(position, canvas.transform.position), Vector3.Distance(view.position, canvas.transform.position) - 0.05f, "closer than the view point");

            var corners = new Vector3[4];
            ((RectTransform)canvas.transform).GetWorldCorners(corners);
            real.Advance(5f);
            float tangent = Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            foreach (var corner in corners)
            {
                var local = playerCamera.transform.InverseTransformPoint(corner);
                Assert.Less(Mathf.Abs(local.y) / local.z, tangent, "top and bottom visible");
                Assert.Less(Mathf.Abs(local.x) / local.z, tangent * playerCamera.aspect, "sides visible");
            }
            real.Leave();
            real.Advance(5f);
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }
    }
}
