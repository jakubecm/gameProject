using System;
using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The one tool for the world: Tools/Cold Shutdown/Create Preview Scene. It checks the world map (<c>Data/WorldMap.asset</c>), rebuilds
    /// everything the world is made of from the code (textures, materials, every model, every room, the hall kit), bakes the hall of the
    /// map, rewires <c>Main</c> and builds the map into <c>Sandbox_B</c> with the same <see cref="LevelBuilder"/> the game uses. Generated
    /// assets are rewritten in place (same GUIDs); hand edits in them are lost, so change the code, not the assets.
    /// </summary>
    public static class WorldTool
    {
        public const string SandboxPath = "Assets/_Project/Scenes/Sandbox_B.unity";
        public const string WorldMapPath = Rooms.DefinitionsFolder + "/WorldMap.asset";

        /// <summary>
        /// The map a new WorldMap.asset starts with: the 2 x 3 hall in a ring of corridors on its west, south and east side with three
        /// doors into it, the lift opening onto the main entrance, and every room type and corridor shape off the ring (ROOMS.md).
        /// </summary>
        private const string DefaultMap =
            "# The one world map. North is at the top; one character per 12 x 12 m cell (format: WorldMapText).\n" +
            "# C corridor, D corridor with a door into the hall, E lift end (start), O office, G security office,\n" +
            "# P power room, V cooling plant, S storage room, H hall, . empty.\n" +
            ". . G . . . .\n" +
            ". O C H H C V\n" +
            ". . D H H D P\n" +
            ". . C H H C .\n" +
            "C C C D C C O\n" +
            ". S S E O . .\n";

        [MenuItem("Tools/Cold Shutdown/Create Preview Scene")]
        public static void CreatePreviewScene()
        {
            if (!Application.isBatchMode &&
                (!EditorUtility.DisplayDialog("Cold Shutdown",
                     "Rebuild all textures, materials, models, rooms and the hall from the code, bake the hall of the world map and build Sandbox_B? " +
                     "Hand edits in generated assets are lost.", "Build", "Cancel") ||
                 !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()))
            {
                return;
            }
            RebuildEverything();
        }

        /// <summary>
        /// The whole build without asking (also the batch entry point, <see cref="WorldBatch"/>): checks the map first, then imports files
        /// changed outside the Editor and the textures again, writes every generated texture again, resets the materials, rebuilds every
        /// prop, room layout and the hall kit in place, deletes props, rooms, definitions and meshes no code makes any more, bakes the hall,
        /// rewires <c>Main</c> (if it exists) and builds <c>Sandbox_B</c>, which stays open. The room shell and the wall plug are only made
        /// when missing: the rooms are prefab variants of the shell. Returns false if the map is not valid.
        /// </summary>
        public static bool RebuildEverything()
        {
            var worldMap = LoadOrCreateWorldMap();
            var parsed = worldMap.Parse();
            foreach (var warning in parsed.Warnings) Debug.LogWarning($"World map: {warning}", worldMap);
            if (!parsed.IsValid)
            {
                string errors = string.Join("\n", parsed.Errors);
                Debug.LogError($"World map ({WorldMapPath}) is not valid:\n{errors}", worldMap);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("Cold Shutdown", $"The world map is not valid:\n\n{errors}", "OK");
                return false;
            }

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(Surfaces.TextureFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            MeshBuilder.TrackWrites(() => Surfaces.RegenerateTextures(() =>
            {
                RoomShell.CreateRoomShell();
                Rooms.RebuildAll();
                HallBuilder.BuildKit();
            }));
            Props.DeleteObsoleteProps();
            HallBuilder.BakeHall(parsed.Layout);
            AssetDatabase.SaveAssets();

            if (File.Exists(MainSceneTool.ScenePath)) MainSceneTool.UpdateMainScene();
            // Last, so Sandbox_B is the scene left open.
            BuildSandbox();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>The world map asset, created with <see cref="DefaultMap"/> if it does not exist yet.</summary>
        public static WorldMap LoadOrCreateWorldMap()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(WorldMapPath);
            if (map != null) return map;
            Directory.CreateDirectory(Path.GetDirectoryName(WorldMapPath));
            map = ScriptableObject.CreateInstance<WorldMap>();
            map.Text = DefaultMap;
            AssetDatabase.CreateAsset(map, WorldMapPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"World map created at {WorldMapPath}.");
            return map;
        }

        /// <summary><c>Sandbox_B</c>: the player prefab and the world map built by a <see cref="LevelBuilder"/> in the editor (kept at Play), the F1 menu.</summary>
        private static void BuildSandbox()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyRoomAmbientLight();
            var player = SandboxSceneTool.CreatePlayer();
            var builder = CreateLevelBuilder(player.transform, false);
            CreateDebugMenu(builder);
            builder.Build((prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent));

            Directory.CreateDirectory(Path.GetDirectoryName(SandboxPath));
            EditorSceneManager.SaveScene(scene, SandboxPath);
            Debug.Log($"Preview scene saved to {SandboxPath}. Press Play: you start in the lift.");
        }

        /// <summary>A new "Level Builder" object wired to the world map, every room definition, the wall plug, the door, the rack and the player.</summary>
        internal static LevelBuilder CreateLevelBuilder(Transform player, bool buildOnStart)
        {
            var builder = new GameObject("Level Builder").AddComponent<LevelBuilder>();
            WireLevelBuilder(builder, player, buildOnStart);
            return builder;
        }

        /// <summary>Points a level builder at the current assets (after a rebuild, renamed rooms have new definitions).</summary>
        internal static void WireLevelBuilder(LevelBuilder builder, Transform player, bool buildOnStart)
        {
            // Load the assets only now: creating a scene unloads assets nothing references yet.
            var definitions = Enum.GetValues(typeof(RoomType)).Cast<RoomType>()
                .Select(type => AssetDatabase.LoadAssetAtPath<RoomDefinition>(Rooms.DefinitionPath(type)))
                .Where(definition => definition != null)
                .ToArray();
            var serialized = new SerializedObject(builder);
            serialized.FindProperty("worldMap").objectReferenceValue = LoadOrCreateWorldMap();
            var definitionsProperty = serialized.FindProperty("roomDefinitions");
            definitionsProperty.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
            {
                definitionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }
            serialized.FindProperty("wallPlugPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShell.WallPlugPath);
            serialized.FindProperty("doorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Props.PathOf(Prop.RoomDoor));
            serialized.FindProperty("rackPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Props.PathOf(Prop.Rack));
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("buildOnStart").boolValue = buildOnStart;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static DebugMenu CreateDebugMenu(LevelBuilder builder)
        {
            var menu = new GameObject("Debug Menu").AddComponent<DebugMenu>();
            var serialized = new SerializedObject(menu);
            serialized.FindProperty("levelBuilder").objectReferenceValue = builder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return menu;
        }

        /// <summary>
        /// The rooms are lit only by their own light fittings. Without baked bounce light, a trilight
        /// ambient stands in for it: surfaces facing down (the ceiling) get the light the floor reflects.
        /// </summary>
        internal static void ApplyRoomAmbientLight()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.06f, 0.06f, 0.065f);
            RenderSettings.ambientEquatorColor = new Color(0.13f, 0.13f, 0.135f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.29f, 0.28f);
        }
    }
}
