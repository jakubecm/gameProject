using System;
using System.IO;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates every layout of every <see cref="RoomType"/> (<see cref="RoomLayouts"/>) as a prefab variant of the room
    /// shell, plus a <see cref="RoomDefinition"/> per type that lists its layouts. This tool only adds the game logic of a
    /// room (rack and terminal slots, the security office's video wall) and the walls that close a small room
    /// (<see cref="RoomRules.IsSmallRoom"/>); furniture and fittings come from Dress Graybox Rooms. Existing prefabs and
    /// definitions are never replaced, so hand edits are safe and a new room type only needs a new enum value and
    /// another run. Run Create Room Shell first. Rebuild Graybox Rooms (in <see cref="RoomDressingTool"/>) rebuilds the
    /// logic in place.
    /// </summary>
    public static class GrayboxRoomsTool
    {
        public const string DefinitionsFolder = "Assets/_Project/Data";

        public static string PrefabPath(RoomType type) => $"{RoomShellTool.RoomsFolder}/Room_{type}.prefab";

        public static string DefinitionPath(RoomType type) => $"{DefinitionsFolder}/RoomDefinition_{type}.asset";

        [MenuItem("Tools/Cold Shutdown/Create Graybox Rooms")]
        public static void CreateGrayboxRooms()
        {
            var shell = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShellTool.ShellPath);
            if (shell == null)
            {
                Debug.LogError($"{RoomShellTool.ShellPath} not found. Run Tools/Cold Shutdown/Create Room Shell first.");
                return;
            }

            // Hall tiles are not shell rooms: they are 6 m high and have no walls between them.
            HallKitTool.CreateHallKit();
            foreach (var (type, layout) in RoomLayouts.All())
            {
                CreateRoom(shell, type, layout);
            }
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                if (type != RoomType.Hall) SaveDefinition(type);
            }
            AssetDatabase.SaveAssets();
        }

        private static void CreateRoom(GameObject shell, RoomType type, int layout)
        {
            string prefabPath = RoomLayouts.PrefabPath(type, layout);
            if (RoomShellTool.SkipExisting(prefabPath))
            {
                return;
            }

            var room = (GameObject)PrefabUtility.InstantiatePrefab(shell);
            room.name = $"Room_{type}";
            AddLogic(room.transform, type, layout);
            RoomShellTool.SaveAsPrefab(room, prefabPath);
            Debug.Log($"Room prefab saved to {prefabPath}.");
        }

        /// <summary>
        /// Throws away and builds again the generated logic of an existing layout prefab (structure, slots, video wall),
        /// keeping its GUID and everything else in it. Returns false if the prefab does not exist yet.
        /// </summary>
        internal static bool RebuildLogic(RoomType type, int layout)
        {
            string path = RoomLayouts.PrefabPath(type, layout);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return false;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var name in new[] { StructureName, VideoWallName, "RackSlots", "TerminalSlot" })
                {
                    Transform child;
                    while ((child = root.transform.Find(name)) != null)
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
                AddLogic(root.transform, type, layout);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return true;
        }

        private static void AddLogic(Transform room, RoomType type, int layout)
        {
            var mirror = RoomLayouts.Mirror(type, layout);
            int design = RoomLayouts.Design(layout);
            if (RoomRules.IsSmallRoom(type))
            {
                AddSmallRoomWalls(room);
            }
            if (type == RoomType.Elevator)
            {
                AddLiftShaft(room);
            }

            var terminal = RoomLayouts.Terminal(type, design);
            if (terminal.HasValue)
            {
                var slot = new GameObject("TerminalSlot", typeof(TerminalSlot)).transform;
                slot.SetParent(room, false);
                RoomShellTool.ApplyPose(slot, mirror.Pose(terminal.Value));
            }

            var racks = RoomLayouts.Racks(type, design);
            if (racks.Length > 0)
            {
                var group = GrayboxUtility.CreateGroup("RackSlots", room);
                for (int i = 0; i < racks.Length; i++)
                {
                    var slot = new GameObject($"RackSlot_{i:00}", typeof(RackSlot)).transform;
                    slot.SetParent(group, false);
                    RoomShellTool.ApplyPose(slot, mirror.Pose(racks[i]));
                }
            }

            if (type == RoomType.SecurityOffice)
            {
                BuildVideoWall(room);
            }
        }

        public const string StructureName = "Structure";

        /// <summary>
        /// A small room is a 6 x 6 m square by its south door: a full-height wall across the cell (its room face at
        /// <see cref="RoomContract.SmallRoomBackWall"/>) and two side walls (room faces at
        /// ±<see cref="RoomContract.SmallRoomHalfWidth"/>) close it. The rest of the cell is built up and never seen.
        /// </summary>
        private static void AddSmallRoomWalls(Transform room)
        {
            var structure = GrayboxUtility.CreateGroup(StructureName, room);
            float wall = RoomContract.InteriorHalfSize;
            float thickness = RoomContract.WallThickness;
            float side = RoomContract.SmallRoomHalfWidth;
            float back = RoomContract.SmallRoomBackWall;
            GrayboxUtility.CreateStandingBox("BackWall", structure, new Vector3(0f, 0f, back + thickness / 2f),
                new Vector3(2f * wall, RoomContract.CeilingHeight, thickness), SurfaceMaterial.Wall);
            foreach (int x in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(x < 0 ? "SideWall_W" : "SideWall_E", structure, new Vector3(x * (side + thickness / 2f), 0f, (back - wall) / 2f),
                    new Vector3(thickness, RoomContract.CeilingHeight, back + wall), SurfaceMaterial.Wall);
            }
        }

        /// <summary>
        /// The lift end's landing wall and shaft: where a dead end's crossing is closed to the north, a full-height wall
        /// across the 3 m corridor at <see cref="RoomContract.LiftWallFace"/> with the lift door's opening in the middle,
        /// and the block behind the shaft. The shaft's sides are the corridor's partition blocks.
        /// </summary>
        private static void AddLiftShaft(Transform room)
        {
            var structure = GrayboxUtility.CreateGroup(StructureName, room);
            float face = RoomContract.LiftWallFace;
            float thickness = RoomContract.LiftWallThickness;
            float corridor = RoomDressingTool.CorridorHalfWidth;
            float half = RoomContract.LiftDoorWidth / 2f;
            float height = RoomContract.CeilingHeight;
            float door = RoomContract.LiftDoorHeight;
            float z = face + thickness / 2f;
            foreach (int x in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(x < 0 ? "LiftWall_W" : "LiftWall_E", structure, new Vector3(x * (corridor + half) / 2f, 0f, z),
                    new Vector3(corridor - half, height, thickness), SurfaceMaterial.Wall);
            }
            GrayboxUtility.CreateStandingBox("LiftWall_Lintel", structure, new Vector3(0f, door, z), new Vector3(2f * half, height - door, thickness), SurfaceMaterial.Wall);
            // Behind the car (and its counterweight) the cell is built up to the north wall.
            float back = RoomContract.LiftCarFront + RoomContract.LiftCarDepth + 0.25f;
            float wall = RoomContract.InteriorHalfSize;
            GrayboxUtility.CreateStandingBox("ShaftBack", structure, new Vector3(0f, 0f, (back + wall) / 2f), new Vector3(2f * corridor, height, wall - back), SurfaceMaterial.Wall);
        }

        /// <summary>
        /// The security office's video wall: 2 x 4 screens across the whole back wall, driven by a
        /// <see cref="SecurityMonitorWall"/>. The screens are game logic (they show the camera feeds), so they sit in their
        /// own <c>VideoWall</c> group, not in the dressing. Every layout has it on the back wall, facing the door.
        /// </summary>
        private static void BuildVideoWall(Transform room)
        {
            var videoWall = GrayboxUtility.CreateGroup(VideoWallName, room);
            videoWall.gameObject.AddComponent<SecurityMonitorWall>();
            foreach (float y in VideoWallRows)
            {
                foreach (float x in VideoWallColumns)
                {
                    GrayboxPropsTool.Place(GrayboxProp.VideoScreen, videoWall, new Vector3(x, y, RoomContract.SmallRoomBackWall), 180f, $"Screen_{x:0.00}_{y:0.00}");
                }
            }
        }

        public const string VideoWallName = "VideoWall";

        /// <summary>Centers of the video wall screens along the back wall (X), measured from the room center.</summary>
        internal static readonly float[] VideoWallColumns = { -1.875f, -0.625f, 0.625f, 1.875f };

        /// <summary>Heights of the screen centers.</summary>
        internal static readonly float[] VideoWallRows = { 1.3f, 2.05f };

        /// <summary>
        /// Creates the definition if it is missing and fills in its layouts. Layouts that are already assigned keep their
        /// prefab, so a definition pointed at another prefab by hand keeps that choice.
        /// </summary>
        internal static void SaveDefinition(RoomType type)
        {
            string path = DefinitionPath(type);
            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
            if (definition == null)
            {
                Directory.CreateDirectory(DefinitionsFolder);
                definition = ScriptableObject.CreateInstance<RoomDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("type").intValue = (int)type;
            var first = serialized.FindProperty("prefab");
            if (first.objectReferenceValue == null)
            {
                first.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RoomLayouts.PrefabPath(type, 0));
            }
            var others = serialized.FindProperty("otherLayouts");
            int count = RoomRules.LayoutCount(type) - 1;
            if (others.arraySize < count) others.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                var element = others.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    element.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RoomLayouts.PrefabPath(type, i + 1));
                }
            }
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(definition);
                Debug.Log($"Room definition saved to {path}.");
            }
        }

        /// <summary>Creates the hall's definition if it is missing (the hall has one prefab, built by the hall kit).</summary>
        internal static void SaveDefinition(RoomType type, GameObject prefab)
        {
            string path = DefinitionPath(type);
            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
            if (definition != null && definition.Prefab != null) return;
            if (definition == null)
            {
                Directory.CreateDirectory(DefinitionsFolder);
                definition = ScriptableObject.CreateInstance<RoomDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("type").intValue = (int)type;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            Debug.Log($"Room definition saved to {path}.");
        }
    }
}
