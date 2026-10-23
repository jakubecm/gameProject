using System.IO;
using ColdShutdown.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates Sandbox_B: the three graybox rooms in a row (Corridor, ServerAisle, Office) joined
    /// through their east/west openings, every other opening closed with a wall plug, and the
    /// sandbox player in the corridor. A hand-made stand-in for the level builder.
    /// </summary>
    public static class RoomPreviewSceneTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_B.unity";

        private static readonly RoomType[] Row = { RoomType.Corridor, RoomType.ServerAisle, RoomType.Office };

        [MenuItem("Tools/Cold Shutdown/Create Room Preview Scene")]
        public static void CreateRoomPreviewScene()
        {
            var plug = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShellTool.WallPlugPath);
            if (plug == null)
            {
                Debug.LogError($"{RoomShellTool.WallPlugPath} not found. Run Tools/Cold Shutdown/Create Room Shell first.");
                return;
            }
            if (!ConfirmOverwrite() ||
                (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);

            for (int column = 0; column < Row.Length; column++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxRoomsTool.PrefabPath(Row[column]));
                if (prefab == null)
                {
                    Debug.LogError($"{GrayboxRoomsTool.PrefabPath(Row[column])} not found. Run Tools/Cold Shutdown/Create Graybox Rooms first.");
                    return;
                }

                var origin = new Vector3(RoomContract.CellOriginX(column), 0f, RoomContract.CellOriginZ(0));
                var room = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                room.transform.position = origin;

                for (int i = 0; i < DirectionExtensions.Count; i++)
                {
                    var side = (Direction)i;
                    bool open = (side == Direction.West && column > 0) || (side == Direction.East && column < Row.Length - 1);
                    if (!open)
                    {
                        var plugInstance = (GameObject)PrefabUtility.InstantiatePrefab(plug, room.transform);
                        plugInstance.name = $"WallPlug_{side}";
                        RoomShellTool.ApplyPose(plugInstance.transform, RoomContract.GetOpeningPose(side));
                    }
                }

                var lightObject = new GameObject($"Light_{Row[column]}");
                lightObject.transform.position = origin + new Vector3(0f, RoomContract.CeilingHeight - 0.3f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = RoomContract.CellSize;
                light.intensity = 3f;
            }

            var player = SandboxSceneTool.CreatePlayer();
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Room preview scene saved to {ScenePath}. Press Play and walk east from the corridor through the server aisle into the office.");
        }

        /// <summary>The scene is a throwaway sandbox, so it may be replaced, but only after asking; batch mode always replaces.</summary>
        private static bool ConfirmOverwrite()
        {
            return Application.isBatchMode || !File.Exists(ScenePath) ||
                   EditorUtility.DisplayDialog("Cold Shutdown", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel");
        }
    }
}
