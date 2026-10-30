using System.IO;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates Main.unity: the player, a <see cref="LevelBuilder"/> wired to the world map and every room definition, and the F1
    /// <see cref="DebugMenu"/>. Press Play and the builder builds the world map and puts the player in the lift. Run Create Preview
    /// Scene first (it builds the rooms and the hall, and rewires an existing Main after every rebuild).
    /// </summary>
    public static class MainSceneTool
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/Cold Shutdown/Create Main Scene")]
        public static void CreateMainScene()
        {
            if (!File.Exists(RoomShell.WallPlugPath))
            {
                Debug.LogError($"{RoomShell.WallPlugPath} not found. Run Tools/Cold Shutdown/Create Preview Scene first.");
                return;
            }
            if (!ConfirmOverwrite() ||
                (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WorldTool.ApplyRoomAmbientLight();
            var player = SandboxSceneTool.CreatePlayer();
            var builder = WorldTool.CreateLevelBuilder(player.transform, true);
            WorldTool.CreateDebugMenu(builder);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Main scene saved to {ScenePath}. Press Play: the world map is built around you. F1 opens the debug menu.");
        }

        /// <summary>Points the level builder of the existing Main scene at the current assets and saves it (nothing else changes).</summary>
        internal static void UpdateMainScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var builder in root.GetComponentsInChildren<LevelBuilder>(true))
                {
                    WorldTool.WireLevelBuilder(builder, builder.Player, true);
                }
            }
            EditorSceneManager.SaveScene(scene);
        }

        private static bool ConfirmOverwrite()
        {
            return Application.isBatchMode || !File.Exists(ScenePath) ||
                   EditorUtility.DisplayDialog("Cold Shutdown", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel");
        }
    }
}
