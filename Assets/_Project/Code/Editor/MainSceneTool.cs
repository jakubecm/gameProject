using System;
using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates Main.unity: the player, a <see cref="LevelBuilder"/> wired to the room definitions, the
    /// wall plug, the automatic door and the hall kit, and the F1 <see cref="DebugMenu"/>. Press Play and the builder makes a random map and
    /// puts the player in its middle. Run Create Room Shell, Create Graybox Rooms and Create Player Prefab
    /// first (the sandbox scene tools do the last one).
    /// </summary>
    public static class MainSceneTool
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/Cold Shutdown/Create Main Scene")]
        public static void CreateMainScene()
        {
            if (!File.Exists(RoomShellTool.WallPlugPath))
            {
                Debug.LogError($"{RoomShellTool.WallPlugPath} not found. Run Tools/Cold Shutdown/Create Room Shell first.");
                return;
            }
            if (Enum.GetValues(typeof(RoomType)).Cast<RoomType>().Any(type => !File.Exists(GrayboxRoomsTool.DefinitionPath(type))))
            {
                Debug.LogError("A room definition is missing. Run Tools/Cold Shutdown/Create Graybox Rooms first.");
                return;
            }

            if (!ConfirmOverwrite() ||
                (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RoomPreviewSceneTool.ApplyRoomAmbientLight();

            // Load the assets only now: creating a scene unloads assets nothing references yet, and an
            // asset loaded before it would be saved into the scene as an empty reference.
            var plug = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShellTool.WallPlugPath);
            var hallKit = AssetDatabase.LoadAssetAtPath<HallKit>(HallKitTool.KitPath);
            var door = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPropsTool.PathOf(GrayboxProp.AutoSlidingDoor));
            var definitions = Enum.GetValues(typeof(RoomType)).Cast<RoomType>()
                .Select(type => AssetDatabase.LoadAssetAtPath<RoomDefinition>(GrayboxRoomsTool.DefinitionPath(type)))
                .ToArray();

            var player = SandboxSceneTool.CreatePlayer();

            var builderObject = new GameObject("Level Builder");
            var builder = builderObject.AddComponent<LevelBuilder>();
            var serialized = new SerializedObject(builder);
            var definitionsProperty = serialized.FindProperty("roomDefinitions");
            definitionsProperty.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
            {
                definitionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }
            serialized.FindProperty("wallPlugPrefab").objectReferenceValue = plug;
            serialized.FindProperty("doorPrefab").objectReferenceValue = door;
            serialized.FindProperty("hallKit").objectReferenceValue = hallKit;
            serialized.FindProperty("player").objectReferenceValue = player.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var menuObject = new GameObject("Debug Menu");
            var menu = menuObject.AddComponent<DebugMenu>();
            var menuSerialized = new SerializedObject(menu);
            menuSerialized.FindProperty("levelBuilder").objectReferenceValue = builder;
            menuSerialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Main scene saved to {ScenePath}. Press Play: a random map is built around you. F1 opens the debug menu (seed, size, regenerate, map).");
        }

        private static bool ConfirmOverwrite()
        {
            return Application.isBatchMode || !File.Exists(ScenePath) ||
                   EditorUtility.DisplayDialog("Cold Shutdown", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel");
        }
    }
}
