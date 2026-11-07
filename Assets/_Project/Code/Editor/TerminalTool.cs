using System.IO;
using System.Linq;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Makes the game's computers. The computer is the <see cref="Prop.Workstation"/> (two monitors, keyboard, mouse; its model is the
    /// colleague's, see PROPS.md): this tool gives that prop prefab the <see cref="Terminal"/> component, a view point and two canvases
    /// laid exactly over the monitors' lit pictures (the mesh parts named <c>Monitor_Picture</c>): the desktop with the mouse on one
    /// monitor (<see cref="ActiveMonitor"/>), and on the other only the wallpaper while the computer is on, black otherwise. Every
    /// workstation in the rooms is therefore a computer. It also builds <c>Terminals/Terminal.prefab</c> (a desk with a workstation on it)
    /// and Sandbox_C, a test scene with two of them. Everything is rebuilt in place (same GUIDs); Create Preview Scene runs this after it has
    /// rebuilt the models (which start the workstation prefab from scratch).
    /// </summary>
    public static class TerminalTool
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Terminals/Terminal.prefab";
        public const string ThemePath = "Assets/_Project/Data/DesktopTheme_Office.asset";
        public const string KnowledgePath = "Assets/_Project/Data/Lore/knowledge_base.json";
        public const string WebPath = "Assets/_Project/Data/Web/web_pages.json";
        public const string WordsPath = "Assets/_Project/Data/Minigames/password_words.json";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_C.unity";

        /// <summary>The name of the mesh part that is a monitor's lit picture.</summary>
        public const string PictureName = "Monitor_Picture";

        /// <summary>
        /// The monitor that carries the desktop: the group named like this in the workstation prop. Seen from the chair it is the right-hand
        /// one, beside the mouse. The other monitor shows only the wallpaper.
        /// </summary>
        public const string ActiveMonitor = "Monitor_Left";

        private const string OtherMonitor = "Monitor_Right";

        // How far in front of a picture the canvas floats (metres), and how much of the picture it covers.
        private const float ScreenOffset = 0.0005f;
        private const float ScreenCover = 0.995f;

        // How far from the screen the view point is (the camera comes closer, see Terminal.ViewPosition).
        private const float ViewDistance = 0.62f;

        [MenuItem("Tools/Cold Shutdown/Create Terminal Prefab")]
        public static void CreateTerminalPrefab() => BuildTerminalPrefab();

        /// <summary>
        /// Turns the workstation prop prefab into a computer and builds the desk-and-workstation prefab for the sandbox. Needs the models
        /// (made by Create Preview Scene) and the data files. Returns false, with the reason logged, if something is missing.
        /// </summary>
        public static bool BuildTerminalPrefab()
        {
            AssetDatabase.Refresh();
            var knowledge = AssetDatabase.LoadAssetAtPath<TextAsset>(KnowledgePath);
            var web = AssetDatabase.LoadAssetAtPath<TextAsset>(WebPath);
            var words = AssetDatabase.LoadAssetAtPath<TextAsset>(WordsPath);
            if (knowledge == null || web == null || words == null)
            {
                Debug.LogError($"The data files {KnowledgePath}, {WebPath} and {WordsPath} are needed first.");
                return false;
            }
            string workstationPath = Props.PathOf(Prop.Workstation);
            if (!File.Exists(workstationPath) || !File.Exists(Props.PathOf(Prop.Desk)))
            {
                Debug.LogError($"{workstationPath} or the desk is missing. Run Tools/Cold Shutdown/Create Preview Scene first (it makes the models).");
                return false;
            }

            var theme = LoadOrCreateTheme();
            if (!WireWorkstation(workstationPath, theme, web, knowledge, words)) return false;
            BuildSandboxPrefab();
            return true;
        }

        private static bool WireWorkstation(string path, DesktopTheme theme, TextAsset web, TextAsset knowledge, TextAsset words)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Idempotent: take away what an earlier run added.
                foreach (var old in root.GetComponents<Terminal>()) Object.DestroyImmediate(old);
                foreach (string name in new[] { "Screen", "SecondScreen", "ViewPoint" })
                {
                    var old = root.transform.Find(name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }

                var active = root.transform.Find(ActiveMonitor);
                var other = root.transform.Find(OtherMonitor);
                if (active == null || other == null || active.Find(PictureName) == null || other.Find(PictureName) == null)
                {
                    Debug.LogError($"The workstation model needs two monitors named {ActiveMonitor} and {OtherMonitor}, each with a part named {PictureName}.");
                    return false;
                }

                // The interaction ray needs a collider on the prop. The model builder makes one (Workstation.cs); if the prefab was made
                // without it (an older build, or a merge that took the other side's prefab), make the same box here, so rewiring alone is enough.
                if (root.GetComponentInChildren<Collider>() == null)
                {
                    var box = root.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.225f, 0f);
                    box.size = new Vector3(1.14f, 0.45f, 0.55f);
                }

                var screen = BuildScreen(root.transform, "Screen", active, true);
                var secondScreen = BuildScreen(root.transform, "SecondScreen", other, false);

                // The player's eyes go here at most: in front of the active monitor, looking along its normal. The terminal moves the camera
                // closer if the display allows.
                var picture = PictureCenter(active);
                var normal = active.localRotation * Vector3.forward;
                var viewPoint = new GameObject("ViewPoint").transform;
                viewPoint.SetParent(root.transform, false);
                viewPoint.localPosition = picture.center + normal * ViewDistance;
                viewPoint.localRotation = Quaternion.LookRotation(-normal, Vector3.up);

                var terminal = root.AddComponent<Terminal>();
                var serialized = new SerializedObject(terminal);
                serialized.FindProperty("viewPoint").objectReferenceValue = viewPoint;
                serialized.FindProperty("screen").objectReferenceValue = screen;
                serialized.FindProperty("secondScreen").objectReferenceValue = secondScreen;
                serialized.FindProperty("theme").objectReferenceValue = theme;
                serialized.FindProperty("startsOn").boolValue = false;
                serialized.FindProperty("webFile").objectReferenceValue = web;
                serialized.FindProperty("loreFile").objectReferenceValue = knowledge;
                serialized.FindProperty("passwordWordsFile").objectReferenceValue = words;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"{path} is a computer now ({ActiveMonitor} carries the desktop).");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // A desk with a workstation on it, in the workstation's own place on a desk (see Rooms.PlaceDesk), for Sandbox_C.
        private static void BuildSandboxPrefab()
        {
            bool exists = File.Exists(PrefabPath);
            var root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("Terminal");
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }
            foreach (var old in root.GetComponents<Component>())
            {
                if (!(old is Transform)) Object.DestroyImmediate(old);
            }

            var desk = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Props.PathOf(Prop.Desk)), root.transform);
            desk.name = "Desk";
            var workstation = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Props.PathOf(Prop.Workstation)), root.transform);
            workstation.name = "Workstation";
            workstation.transform.localPosition = new Vector3(0f, Props.DeskHeight, -0.05f);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }

        [MenuItem("Tools/Cold Shutdown/Create Terminal Sandbox")]
        public static void CreateTerminalSandbox()
        {
            if (!BuildTerminalPrefab() || !File.Exists(PrefabPath)) return;

            if (File.Exists(ScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Cold Shutdown", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightObject.AddComponent<Light>().type = LightType.Directional;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            SandboxSceneTool.CreatePlayer();

            // Assets are loaded after the scene exists: making a new scene unloads assets nothing uses yet.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (float x in new[] { -1.2f, 1.2f })
            {
                var terminal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                terminal.transform.SetPositionAndRotation(new Vector3(x, 0f, 3.5f), Quaternion.Euler(0f, 180f, 0f));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Terminal sandbox saved to {ScenePath}. Press Play, walk to a computer and press E. Click on the screen; Escape stands up.");
        }

        // The centre and size of a monitor's picture in the prop's space (the monitor group is only moved and turned, never scaled).
        private static (Vector3 center, Vector2 size) PictureCenter(Transform monitor)
        {
            var picture = monitor.Find(PictureName);
            var bounds = picture.GetComponent<MeshFilter>().sharedMesh.bounds;
            var center = monitor.root.InverseTransformPoint(picture.TransformPoint(bounds.center));
            return (center, new Vector2(bounds.size.x, bounds.size.y));
        }

        /// <summary>
        /// A canvas laid over a monitor's picture: 1280 x 720 canvas units stretched to the picture, a hair in front of it, turned around so it
        /// faces the viewer. The one that carries the desktop gets a raycaster for the mouse.
        /// </summary>
        private static Canvas BuildScreen(Transform parent, string name, Transform monitor, bool clickable)
        {
            var (center, size) = PictureCenter(monitor);
            var screen = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            if (clickable)
            {
                screen.AddComponent<CanvasScaler>();
                screen.AddComponent<GraphicRaycaster>();
            }
            screen.transform.SetParent(parent, false);
            screen.transform.localPosition = center + monitor.localRotation * new Vector3(0f, 0f, ScreenOffset);
            screen.transform.localScale = new Vector3(size.x * ScreenCover / Desktop.Width, size.y * ScreenCover / Desktop.Height, size.x / Desktop.Width);

            // A canvas is seen correctly from its back: the viewer stands on the side its forward axis points away from.
            // The monitor looks the other way, so the canvas turns around, or the screen would be mirrored and Unity's UI
            // would ignore clicks on it (it skips graphics that face away from the camera).
            screen.transform.localRotation = monitor.localRotation * Quaternion.Euler(0f, 180f, 0f);

            var rect = (RectTransform)screen.transform;
            rect.sizeDelta = new Vector2(Desktop.Width, Desktop.Height);

            var canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Text is drawn at twice the size and scaled down, which keeps it sharp on a small screen.
            if (clickable)
            {
                var scaler = screen.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.dynamicPixelsPerUnit = 2f;
            }
            return canvas;
        }

        private static DesktopTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<DesktopTheme>(ThemePath);
            if (theme != null) return theme;

            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
            theme = ScriptableObject.CreateInstance<DesktopTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            return theme;
        }
    }
}
