using System.IO;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates the terminal prefab (a desk with a monitor, a keyboard and a mouse; the screen is a world-space
    /// canvas that the <see cref="Terminal"/> fills with the desktop at run time; it starts switched off) and
    /// Sandbox_C, a test scene with two of them. The model is a plain stand-in: the final terminal model (task 2.10) replaces the meshes and keeps the
    /// Terminal component, the view point and the screen canvas. Existing assets are never replaced; delete the
    /// prefab to build it again.
    /// </summary>
    public static class TerminalTool
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Terminals/Terminal.prefab";
        public const string ThemePath = "Assets/_Project/Data/DesktopTheme_Office.asset";
        public const string KnowledgePath = "Assets/_Project/Data/Lore/knowledge_base.json";
        public const string WebPath = "Assets/_Project/Data/Web/web_pages.json";
        public const string WordsPath = "Assets/_Project/Data/Minigames/password_words.json";
        public const string PlasticPath = "Assets/_Project/Art/Materials/M_TerminalPlastic.mat";
        public const string DeskPath = "Assets/_Project/Art/Materials/M_TerminalDesk.mat";
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_C.unity";

        // The screen is 1280 x 720 canvas units of half a millimetre: 0.64 x 0.36 m.
        private const float CanvasScale = 0.0005f;
        private const float MonitorHeight = 1.105f;

        [MenuItem("Tools/Cold Shutdown/Create Terminal Prefab")]
        public static void CreateTerminalPrefab()
        {
            if (File.Exists(PrefabPath))
            {
                Debug.Log($"{PrefabPath} already exists, skipped. Delete it to regenerate.");
                return;
            }

            AssetDatabase.Refresh();
            var knowledge = AssetDatabase.LoadAssetAtPath<TextAsset>(KnowledgePath);
            var web = AssetDatabase.LoadAssetAtPath<TextAsset>(WebPath);
            var words = AssetDatabase.LoadAssetAtPath<TextAsset>(WordsPath);
            if (knowledge == null || web == null || words == null)
            {
                Debug.LogError($"The data files {KnowledgePath}, {WebPath} and {WordsPath} are needed first.");
                return;
            }

            var theme = LoadOrCreateTheme();
            var plastic = LoadOrCreateMaterial(PlasticPath, new Color(0.07f, 0.075f, 0.085f), 0.45f);
            var desk = LoadOrCreateMaterial(DeskPath, new Color(0.55f, 0.56f, 0.58f), 0.2f);

            var root = new GameObject("Terminal");

            // Desk: a top and two side panels. The pivot is on the floor, the front of the monitor looks along +Z.
            Part("Desk_Top", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.73f, 0f), new Vector3(1.3f, 0.04f, 0.7f), desk, true);
            Part("Desk_Left", PrimitiveType.Cube, root.transform, new Vector3(-0.62f, 0.355f, 0f), new Vector3(0.04f, 0.71f, 0.66f), desk, true);
            Part("Desk_Right", PrimitiveType.Cube, root.transform, new Vector3(0.62f, 0.355f, 0f), new Vector3(0.04f, 0.71f, 0.66f), desk, true);

            // Monitor on a stand, a keyboard, a mouse and a small computer on the desk.
            Part("Monitor_Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.7575f, 0f), new Vector3(0.22f, 0.015f, 0.16f), plastic, false);
            Part("Monitor_Neck", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.845f, -0.02f), new Vector3(0.05f, 0.16f, 0.04f), plastic, false);
            Part("Monitor_Frame", PrimitiveType.Cube, root.transform, new Vector3(0f, MonitorHeight, 0f), new Vector3(0.70f, 0.42f, 0.04f), plastic, true);
            Part("Keyboard", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.765f, 0.22f), new Vector3(0.42f, 0.02f, 0.15f), plastic, false);
            Part("Mouse", PrimitiveType.Cube, root.transform, new Vector3(0.3f, 0.7625f, 0.24f), new Vector3(0.06f, 0.025f, 0.1f), plastic, false);
            Part("Computer", PrimitiveType.Cube, root.transform, new Vector3(-0.5f, 0.93f, -0.1f), new Vector3(0.2f, 0.36f, 0.4f), plastic, false);

            // The player's eyes go here: in front of the monitor, looking at it.
            var viewPoint = new GameObject("ViewPoint").transform;
            viewPoint.SetParent(root.transform, false);
            viewPoint.localPosition = new Vector3(0f, MonitorHeight - 0.01f, 0.62f);
            viewPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var screen = BuildScreen(root.transform);

            var terminal = root.AddComponent<Terminal>();
            var serialized = new SerializedObject(terminal);
            serialized.FindProperty("viewPoint").objectReferenceValue = viewPoint;
            serialized.FindProperty("screen").objectReferenceValue = screen;
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.FindProperty("startsOn").boolValue = false;
            serialized.FindProperty("webFile").objectReferenceValue = web;
            serialized.FindProperty("loreFile").objectReferenceValue = knowledge;
            serialized.FindProperty("passwordWordsFile").objectReferenceValue = words;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"Terminal prefab saved to {PrefabPath}.");
        }

        [MenuItem("Tools/Cold Shutdown/Create Terminal Sandbox")]
        public static void CreateTerminalSandbox()
        {
            CreateTerminalPrefab();
            if (!File.Exists(PrefabPath)) return;

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

        private static Canvas BuildScreen(Transform parent)
        {
            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            screen.transform.SetParent(parent, false);
            screen.transform.localPosition = new Vector3(0f, MonitorHeight, 0.0205f);
            screen.transform.localScale = Vector3.one * CanvasScale;

            // A canvas is seen correctly from its back: the viewer stands on the side its forward axis points away from.
            // The monitor looks along +Z, so the canvas turns around, or the screen would be mirrored and Unity's UI
            // would ignore clicks on it (it skips graphics that face away from the camera).
            screen.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var rect = (RectTransform)screen.transform;
            rect.sizeDelta = new Vector2(Desktop.Width, Desktop.Height);

            var canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Text is drawn at twice the size and scaled down, which keeps it sharp on a small screen.
            var scaler = screen.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 2f;
            return canvas;
        }

        private static GameObject Part(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            if (!collider) Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
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

        private static Material LoadOrCreateMaterial(string path, Color color, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
