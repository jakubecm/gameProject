using System;
using System.Collections.Generic;
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
    /// Creates the two hand-made preview scenes, assembled exactly as the level builder does it (<see cref="RoomRules"/>
    /// picks each corridor shape and turns every room so its doors face the corridors, <see cref="HallPlan"/> and
    /// <see cref="HallKit"/> build the hall, unused openings get wall plugs without door frames, every opening into a
    /// room gets an automatic door). Every door is unlocked. Rack slots get real rack props in the scene (not in the
    /// prefabs) until the rack generator (task 2.9) exists: every hall slot is filled, so every aisle is closed.
    /// - <c>Sandbox_B</c>: a rectangular server hall of 3 x 3 tiles (nodes A1 to C3) with the corridors around it
    ///   (a cross, T-junctions, a corner, a straight run and a dead end) and two doors into the hall, and one room of
    ///   every type off the corridors. The player starts in the lift, south-west of the hall.
    /// - <c>TestPreview</c>: one straight corridor with every layout of every room type on it (rooms in the north row,
    ///   plant rooms in the south row, in layout order from west to east), the lift end at its west end, every corridor
    ///   shape at its ends and the 3 x 3 hall at its east end, to check each layout. The player starts in the lift.
    /// Corridors get the turn and layout whose fire arm leads into a room, as in a generated level.
    /// </summary>
    public static class RoomPreviewSceneTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_B.unity";
        private const string TestScenePath = "Assets/_Project/Scenes/TestPreview.unity";

        /// <summary>A room of a preview: its cell, type, layout and the sides that have its doors.</summary>
        private readonly struct PreviewRoom
        {
            public readonly int Column;
            public readonly int Row;
            public readonly RoomType Type;
            public readonly int Layout;
            public readonly Direction[] Doors;

            public PreviewRoom(int column, int row, RoomType type, int layout, params Direction[] doors)
            {
                Column = column;
                Row = row;
                Type = type;
                Layout = layout;
                Doors = doors;
            }
        }

        private static readonly RoomType[] CorridorShapes =
        {
            RoomType.Corridor, RoomType.CorridorT, RoomType.CorridorStraight, RoomType.CorridorCorner, RoomType.CorridorDeadEnd,
        };

        [MenuItem("Tools/Cold Shutdown/Create Room Preview Scene")]
        public static void CreateRoomPreviewScene()
        {
            var rooms = new[]
            {
                new PreviewRoom(0, 2, RoomType.Office, 0, Direction.East, Direction.South),
                new PreviewRoom(2, 0, RoomType.Office, 1, Direction.North),
                new PreviewRoom(6, 1, RoomType.SmallOffice, 0, Direction.West),
                new PreviewRoom(1, 5, RoomType.SecurityOffice, 0, Direction.South),
                new PreviewRoom(4, 0, RoomType.PowerRoom, 0, Direction.North),
                new PreviewRoom(5, 2, RoomType.HvacRoom, 0, Direction.South),
                new PreviewRoom(5, 0, RoomType.StorageRoom, 0, Direction.North),
                new PreviewRoom(1, 0, RoomType.Elevator, 0, Direction.North),
            };
            var corridors = new[] { (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (1, 2), (1, 3), (1, 4), (3, 0) };
            var links = new[]
            {
                (0, 1, Direction.East), (1, 1, Direction.East), (2, 1, Direction.East), (3, 1, Direction.East), (4, 1, Direction.East),
                (1, 1, Direction.North), (1, 2, Direction.North), (1, 3, Direction.North), (3, 0, Direction.North),
                // Doors into the hall.
                (3, 1, Direction.North), (1, 3, Direction.East),
            };
            bool IsHall(int column, int row) => column >= 2 && column <= 4 && row >= 2 && row <= 4;

            CreateScene(ScenePath, 7, 6, rooms, corridors, links, IsHall,
                "Press Play: you start in the lift; the hall is north-east of it. The tile signs show the node codes A1 to C3.");
        }

        [MenuItem("Tools/Cold Shutdown/Create Test Preview Scene")]
        public static void CreateTestPreviewScene()
        {
            // Corridor along row 1: a dead end and a straight piece in the west, then one crossing per pair of rooms, then
            // a T-junction with a corner and a dead end south of it, and the hall in the east.
            const int firstCross = 2;
            var north = new[] { RoomType.Office, RoomType.SmallOffice, RoomType.SecurityOffice };
            var south = new[] { RoomType.PowerRoom, RoomType.HvacRoom, RoomType.StorageRoom };
            var rooms = new List<PreviewRoom> { new PreviewRoom(0, 2, RoomType.Elevator, 0, Direction.South) };
            int column = firstCross;
            for (int t = 0; t < north.Length; t++)
            {
                for (int layout = 0; layout < RoomRules.RoomLayouts; layout++, column++)
                {
                    rooms.Add(new PreviewRoom(column, 2, north[t], layout, Direction.South));
                    rooms.Add(new PreviewRoom(column, 0, south[t], layout, Direction.North));
                }
            }
            int junction = column;
            int hallColumn = junction + 1;

            var corridors = new List<(int, int)>();
            var links = new List<(int, int, Direction)>();
            for (int c = 0; c <= junction; c++)
            {
                corridors.Add((c, 1));
                if (c < junction) links.Add((c, 1, Direction.East));
            }
            corridors.Add((junction, 0));
            corridors.Add((junction + 1, 0));
            links.Add((junction, 0, Direction.North));
            links.Add((junction, 0, Direction.East));
            // The door into the hall's west side.
            links.Add((junction, 1, Direction.East));
            bool IsHall(int c, int r) => c >= hallColumn && c < hallColumn + 3 && r >= 1 && r <= 3;

            CreateScene(TestScenePath, hallColumn + 3, 4, rooms.ToArray(), corridors.ToArray(), links.ToArray(), IsHall,
                "Press Play: you start in the lift at the west end; walk east along the corridor. North: offices, small offices and security offices, south: power rooms, " +
                "cooling plants and storage rooms, each in layouts 1 to 4 from west to east. The hall (A1 to C3) is at the east end.");
        }

        private static void CreateScene(string path, int width, int height, PreviewRoom[] rooms, (int column, int row)[] corridors,
            (int column, int row, Direction side)[] links, Func<int, int, bool> isHall, string message)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RoomShellTool.WallPlugPath) == null)
            {
                Debug.LogError($"{RoomShellTool.WallPlugPath} not found. Run Tools/Cold Shutdown/Create Room Shell first.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<HallKit>(HallKitTool.KitPath) == null)
            {
                Debug.LogError($"{HallKitTool.KitPath} not found. Run Tools/Cold Shutdown/Create Graybox Rooms first.");
                return;
            }
            if (!ConfirmOverwrite(path) ||
                (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyRoomAmbientLight();

            // Load the assets only now: creating a scene unloads assets nothing references yet.
            var plug = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShellTool.WallPlugPath);
            var door = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPropsTool.PathOf(GrayboxProp.AutoSlidingDoor));
            var kit = AssetDatabase.LoadAssetAtPath<HallKit>(HallKitTool.KitPath);

            var map = new MapData(width, height);
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    if (!isHall(column, row)) continue;
                    foreach (var side in new[] { Direction.East, Direction.North })
                    {
                        if (isHall(column + side.ColumnOffset(), row + side.RowOffset())) map.Connect(column, row, side);
                    }
                }
            }
            foreach (var (column, row, side) in links) map.Connect(column, row, side);
            foreach (var room in rooms)
            {
                foreach (var side in room.Doors) map.Connect(room.Column, room.Row, side);
            }

            var halls = HallPlan.Create(map, isHall);
            var cells = new Dictionary<(int, int), (RoomType type, int layout)>();
            foreach (var room in rooms) cells.Add((room.Column, room.Row), (room.Type, room.Layout));
            var turns = new Dictionary<(int, int), int>();
            bool IsRoom(int c, int r) => isHall(c, r) || rooms.Any(room => room.Column == c && room.Row == r && !RoomRules.IsCorridor(room.Type));
            foreach (var (column, row) in corridors)
            {
                int connected = RoomLayout.ConnectedSides(map, column, row);
                var shape = CorridorShapes.First(type => RoomRules.FittingTurns(type, connected).Count > 0);
                var options = RoomLayout.FireArmOptions(map, column, row, shape, IsRoom);
                var (quarterTurns, layout) = options.Count > 0 ? options[0] : (RoomRules.FittingTurns(shape, connected)[0], 0);
                cells.Add((column, row), (shape, layout));
                turns.Add((column, row), quarterTurns);
            }
            foreach (var cell in cells)
            {
                if (turns.ContainsKey(cell.Key)) continue;
                var fitting = RoomRules.FittingTurns(cell.Value.type, RoomLayout.ConnectedSides(map, cell.Key.Item1, cell.Key.Item2));
                if (fitting.Count == 0)
                {
                    Debug.LogError($"Preview: {cell.Value.type} does not fit the doors of cell {cell.Key}.");
                    return;
                }
                turns.Add(cell.Key, fitting[0]);
            }

            var level = new GameObject("Level").transform;
            var cellObjects = new Dictionary<(int, int), Transform>();
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    bool hall = halls.TryGetCell(column, row, out var hallCell);
                    if (!hall && !cells.ContainsKey((column, row))) continue;

                    var (type, layout) = hall ? (RoomType.Hall, 0) : cells[(column, row)];
                    string prefabPath = hall ? GrayboxRoomsTool.PrefabPath(type) : RoomLayouts.PrefabPath(type, layout);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogError($"{prefabPath} not found. Run Tools/Cold Shutdown/Create Graybox Rooms first.");
                        return;
                    }

                    var cell = new GameObject($"Cell_{column}_{row}").transform;
                    cell.SetParent(level, false);
                    cell.localPosition = new Vector3(RoomContract.CellOriginX(column), 0f, RoomContract.CellOriginZ(row));
                    cellObjects.Add((column, row), cell);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cell);
                    instance.name = RoomRules.LayoutCount(type) > 1 ? $"Room_{type}_Layout{layout + 1}" : $"Room_{type}";

                    if (hall)
                    {
                        HallKit.LabelTile(instance, hallCell);
                        kit.AddPieces(cell, hallCell, (piece, parent) => (GameObject)PrefabUtility.InstantiatePrefab(piece, parent));
                        continue;
                    }

                    int quarterTurns = turns[(column, row)];
                    instance.transform.localRotation = Quaternion.Euler(0f, quarterTurns * RoomContract.RotationStepDegrees, 0f);
                    for (int i = 0; i < DirectionExtensions.Count; i++)
                    {
                        var side = (Direction)i;
                        if (map.IsConnected(column, row, side)) continue;
                        var plugInstance = (GameObject)PrefabUtility.InstantiatePrefab(plug, cell);
                        plugInstance.name = $"WallPlug_{side}";
                        RoomShellTool.ApplyPose(plugInstance.transform, RoomContract.GetOpeningPose(side));
                        LevelBuilder.HideDoorFrame(instance, side, quarterTurns);
                    }
                }
            }

            PlaceDoors(map, halls, cells, cellObjects, door);
            PlaceRacks(level);

            // The player starts in the lift car, facing its door, as in a generated level.
            var lift = rooms.First(room => room.Type == RoomType.Elevator);
            var pose = RoomContract.LiftStartPose;
            float liftYaw = turns[(lift.Column, lift.Row)] * RoomContract.RotationStepDegrees;
            var liftCell = new Vector3(RoomContract.CellOriginX(lift.Column), 0f, RoomContract.CellOriginZ(lift.Row));
            var player = SandboxSceneTool.CreatePlayer();
            player.transform.SetPositionAndRotation(liftCell + Quaternion.Euler(0f, liftYaw, 0f) * new Vector3(pose.X, 0f, pose.Z),
                Quaternion.Euler(0f, liftYaw + pose.YawDegrees, 0f));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Preview scene saved to {path}. {message}");
        }

        /// <summary>An automatic door in every connected opening of a room, as the level builder does it.</summary>
        private static void PlaceDoors(MapData map, HallPlan halls, Dictionary<(int, int), (RoomType type, int layout)> types,
            Dictionary<(int, int), Transform> cells, GameObject door)
        {
            RoomType TypeAt(int column, int row) => halls.TryGetCell(column, row, out _) ? RoomType.Hall : types[(column, row)].type;

            foreach (var (column, row) in cells.Keys)
            {
                foreach (var side in new[] { Direction.East, Direction.North })
                {
                    if (!map.IsConnected(column, row, side)) continue;
                    int otherColumn = column + side.ColumnOffset();
                    int otherRow = row + side.RowOffset();
                    var placement = RoomRules.DoorBetween(TypeAt(column, row), TypeAt(otherColumn, otherRow));
                    if (placement == DoorPlacement.First) AddDoor(cells[(column, row)], side, door);
                    else if (placement == DoorPlacement.Second) AddDoor(cells[(otherColumn, otherRow)], side.Opposite(), door);
                }
            }
        }

        private static void AddDoor(Transform cell, Direction side, GameObject door)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(door, cell);
            instance.name = $"Door_{side}";
            RoomShellTool.ApplyPose(instance.transform, RoomContract.GetOpeningPose(side));
        }

        /// <summary>
        /// A rack prop on every rack slot, so every aisle is closed on all sides: network racks at the west end of each
        /// hall row, a GPU rack in about one slot in six and a JBOD in one in eight, compute racks elsewhere. The security
        /// office gets its compute rack and its JBOD.
        /// </summary>
        private static void PlaceRacks(Transform level)
        {
            var racks = new GameObject("Racks (preview only)").transform;
            int index = 0;
            foreach (var slot in level.GetComponentsInChildren<RackSlot>())
            {
                var position = slot.transform.position;
                int hash = (Mathf.RoundToInt(position.x * 10f) * 73856093 ^ Mathf.RoundToInt(position.z * 10f) * 19349663) & int.MaxValue;
                // Slots sit in a RackSlots group directly under the room.
                var room = slot.transform.parent.parent;
                GrayboxProp prop;
                if (room.name.StartsWith($"Room_{RoomType.SecurityOffice}"))
                {
                    prop = slot.name.EndsWith("00") ? GrayboxProp.RackCompute : GrayboxProp.RackJbod;
                }
                else
                {
                    var local = room.InverseTransformPoint(position);
                    prop = local.x < -HallContract.PodHalfLength + RoomContract.RackWidth ? GrayboxProp.RackNetwork
                        : hash % 6 == 1 ? GrayboxProp.RackGpu
                        : hash % 8 == 3 ? GrayboxProp.RackJbod
                        : GrayboxProp.RackCompute;
                }

                var rack = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPropsTool.PathOf(prop)), racks);
                rack.name = $"{prop}_{index++:000}";
                rack.transform.SetPositionAndRotation(position, slot.transform.rotation);
            }
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

        /// <summary>The scenes are throwaway sandboxes, so they may be replaced, but only after asking; batch mode always replaces.</summary>
        private static bool ConfirmOverwrite(string path)
        {
            return Application.isBatchMode || !File.Exists(path) ||
                   EditorUtility.DisplayDialog("Cold Shutdown", $"{path} already exists. Replace it?", "Replace", "Cancel");
        }
    }
}
