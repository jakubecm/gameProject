using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates Main.unity: the player, a <see cref="LevelBuilder"/> wired to the world map and every room definition, the F1
    /// <see cref="DebugMenu"/> and the <see cref="GameFlow"/> (main menu, pause, settings). Press Play and the builder builds
    /// the world map and puts the player in the lift; the main menu comes first. Run Create Preview Scene first (it builds the
    /// rooms and the hall, and rewires an existing Main after every rebuild).
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
            WireGameFlow(builder, null);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterBuildScenes();
            Debug.Log($"Main scene saved to {ScenePath}. Press Play: the main menu comes first, PLAY puts you in the lift. Escape pauses, F1 opens the debug menu.");
        }

        /// <summary>Points the level builder of the existing Main scene at the current assets and saves it (nothing else changes).</summary>
        internal static void UpdateMainScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldTool.ApplyRoomAmbientLight();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var builder in root.GetComponentsInChildren<LevelBuilder>(true))
                {
                    WorldTool.WireLevelBuilder(builder, builder.Player, true);
                    WireGameFlow(builder, scene);
                }
            }
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// The scene's <see cref="GameFlow"/> (created when missing, so an older Main gets the menus too) pointed at the level builder and the
        /// level builder. <paramref name="scene"/> is the scene to look in, or null for the open new scene.
        /// </summary>
        internal static void WireGameFlow(LevelBuilder builder, Scene? scene)
        {
            GameFlow flow = null;
            if (scene.HasValue)
            {
                foreach (var root in scene.Value.GetRootGameObjects())
                {
                    flow = root.GetComponentInChildren<GameFlow>(true);
                    if (flow != null) break;
                }
            }
            if (flow == null) flow = new GameObject("Game Flow").AddComponent<GameFlow>();

            var serialized = new SerializedObject(flow);
            serialized.FindProperty("levelBuilder").objectReferenceValue = builder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Puts Main (the game: menus, then play) first in the build and keeps Sandbox_B (the preview) after it, so a build starts
        /// with the main menu. Other scenes already in the build settings stay as they are.
        /// </summary>
        internal static void RegisterBuildScenes()
        {
            var wanted = new[] { ScenePath, WorldTool.SandboxPath }.Where(File.Exists).ToList();
            var scenes = wanted.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
            scenes.AddRange(EditorBuildSettings.scenes.Where(scene => !wanted.Contains(scene.path)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static bool ConfirmOverwrite()
        {
            return Application.isBatchMode || !File.Exists(ScenePath) ||
                   EditorUtility.DisplayDialog("Cold Shutdown", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel");
        }
    }
}
