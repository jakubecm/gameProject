using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Builds the one map (<see cref="WorldMap"/>) into the scene: one cell object per cell at the position the room contract gives,
    /// the prefab of that cell's room type and layout turned by its quarter turns, and a wall plug in every opening that leads nowhere
    /// (its door frame is hidden, so it looks like the rest of the wall). Plugs and doors hang on the cell object, not on the room,
    /// because rooms turn but openings stay where the map says they are.
    ///
    /// The hall is one prefab over its whole rectangle (<c>Room_Hall</c>, baked for this map by Create Preview Scene, with its
    /// perimeter walls and doors), placed at its south-west cell. Every connected opening into a room gets an automatic sliding door
    /// (<see cref="RoomRules.DoorBetween"/> says which of the two cells owns it), named <c>Door_&lt;Side&gt;</c>; the hall's own doors are
    /// in its prefab. Every rack slot gets a rack (<see cref="RackPlacement"/>).
    ///
    /// The same code builds the level at runtime (<c>Main</c>) and in the editor (<c>Sandbox_B</c>, where the editor passes an
    /// instantiate function that keeps prefab links). A level built in the editor is kept when Play starts.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        /// <summary>Seed of the racks' openable choice and fillings; fixed, so the one map always looks the same.</summary>
        public const int RackSeed = 1;

        [SerializeField] private WorldMap worldMap;
        [SerializeField] private RoomDefinition[] roomDefinitions;
        [SerializeField] private GameObject wallPlugPrefab;
        [Tooltip("Automatic sliding door put into every connected opening of a room. Optional.")]
        [SerializeField] private GameObject doorPrefab;
        [Tooltip("Rack put on every rack slot. Optional.")]
        [SerializeField] private GameObject rackPrefab;
        [Tooltip("Moved into the lift after every build. Optional.")]
        [SerializeField] private Transform player;
        [Tooltip("Build the map when Play starts, unless a level built in the editor is already there.")]
        [SerializeField] private bool buildOnStart = true;

        // Serialized, so a level built in the editor survives saving the scene.
        [SerializeField, HideInInspector] private Transform levelRoot;

        /// <summary>Raised after a level has been built (and the player moved), for the debug menu and later systems.</summary>
        public event Action<LevelBuilder> LevelBuilt;

        public bool HasLevel => Layout != null;

        public WorldMap WorldMap
        {
            get => worldMap;
            set => worldMap = value;
        }

        public MapData Map => Layout?.Map;

        public RoomLayout Layout { get; private set; }

        /// <summary>The cell the player starts in: the lift end.</summary>
        public (int column, int row) StartCell => Layout != null ? Layout.StartCell : (0, 0);

        /// <summary>
        /// Where the player starts (world position on the floor and the yaw they look along): in the middle of the lift
        /// car facing its door, or the middle of the start cell looking north if the layout has no lift.
        /// </summary>
        public (Vector3 position, float yaw) StartPose
        {
            get
            {
                var (column, row) = StartCell;
                var cell = new Vector3(RoomContract.CellOriginX(column), 0f, RoomContract.CellOriginZ(row));
                if (Layout == null || !Layout.HasLift) return (cell, 0f);
                var room = Layout.GetRoom(column, row);
                var pose = RoomContract.LiftStartPose;
                var turn = Quaternion.Euler(0f, room.YawDegrees, 0f);
                return (cell + turn * new Vector3(pose.X, 0f, pose.Z), Mathf.Repeat(room.YawDegrees + pose.YawDegrees, 360f));
            }
        }

        public Transform Player => player;

        private void Start()
        {
            if (levelRoot != null)
            {
                // Built in the editor: keep it, only read the map again.
                var parsed = worldMap != null ? worldMap.Parse() : null;
                Layout = parsed != null && parsed.IsValid ? parsed.Layout : null;
                LevelBuilt?.Invoke(this);
            }
            else if (buildOnStart)
            {
                Build();
            }
        }

        /// <summary>
        /// Replaces the current level with the world map. <paramref name="instantiate"/> creates an instance of a prefab under a parent
        /// (default <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object, Transform)"/>). Returns false (and logs why) if it cannot be built.
        /// </summary>
        public bool Build(Func<GameObject, Transform, GameObject> instantiate = null)
        {
            instantiate ??= (prefab, parent) => Instantiate(prefab, parent);
            if (worldMap == null)
            {
                Debug.LogError("LevelBuilder: no world map assigned.", this);
                return false;
            }
            if (wallPlugPrefab == null)
            {
                Debug.LogError("LevelBuilder: no wall plug prefab assigned.", this);
                return false;
            }
            var parsed = worldMap.Parse();
            foreach (var warning in parsed.Warnings) Debug.LogWarning($"World map: {warning}", worldMap);
            if (!parsed.IsValid)
            {
                Debug.LogError("World map: " + string.Join("\n", parsed.Errors), worldMap);
                return false;
            }
            var definitions = CollectDefinitions();
            foreach (var room in parsed.Layout.Rooms)
            {
                if (!definitions.ContainsKey(room.Type))
                {
                    Debug.LogError($"LevelBuilder: no room definition with a prefab for {room.Type}.", this);
                    return false;
                }
            }

            Clear();
            Layout = parsed.Layout;
            var map = Layout.Map;
            levelRoot = new GameObject("Level").transform;
            levelRoot.SetParent(transform, false);

            var cells = new Transform[map.Width * map.Height];
            bool hallPlaced = false;
            foreach (var room in Layout.Rooms)
            {
                if (room.Type == RoomType.Hall)
                {
                    // The hall is one prefab, placed once at its south-west cell.
                    if (!hallPlaced) BuildHall(definitions[RoomType.Hall].Prefab, instantiate);
                    hallPlaced = true;
                    continue;
                }
                cells[room.Row * map.Width + room.Column] = BuildCell(room, definitions[room.Type].GetLayout(room.Layout), instantiate);
            }
            if (doorPrefab != null)
            {
                PlaceDoors(cells, instantiate);
            }
            if (rackPrefab != null)
            {
                var racks = new GameObject("Racks").transform;
                racks.SetParent(levelRoot, false);
                int index = 0;
                RackPlacement.Assign(levelRoot.GetComponentsInChildren<RackSlot>(), slot =>
                {
                    var rack = instantiate(rackPrefab, racks);
                    rack.name = $"Rack_{index++:000}";
                    rack.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
                    return rack.GetComponent<Rack>();
                }, RackSeed);
            }

            PlacePlayer();
            LevelBuilt?.Invoke(this);
            return true;
        }

        /// <summary>Removes the current level, if any.</summary>
        public void Clear()
        {
            Layout = null;
            if (levelRoot == null) return;

            var old = levelRoot.gameObject;
            levelRoot = null;
            // Destroy waits until the end of the frame; hiding it first keeps the old level out of the way meanwhile.
            old.SetActive(false);
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);
        }

        private Dictionary<RoomType, RoomDefinition> CollectDefinitions()
        {
            var definitions = new Dictionary<RoomType, RoomDefinition>();
            if (roomDefinitions == null) return definitions;

            foreach (var definition in roomDefinitions)
            {
                if (definition == null || definition.Prefab == null) continue;
                if (definitions.ContainsKey(definition.Type))
                {
                    Debug.LogWarning($"LevelBuilder: more than one definition for {definition.Type}; using the first.", this);
                    continue;
                }
                definitions.Add(definition.Type, definition);
            }
            return definitions;
        }

        private Transform BuildCell(PlacedRoom room, GameObject prefab, Func<GameObject, Transform, GameObject> instantiate)
        {
            var cell = new GameObject($"Cell_{room.Column}_{room.Row}").transform;
            cell.SetParent(levelRoot, false);
            cell.localPosition = new Vector3(RoomContract.CellOriginX(room.Column), 0f, RoomContract.CellOriginZ(room.Row));

            var instance = instantiate(prefab, cell);
            instance.name = $"Room_{room.Type}";
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, room.YawDegrees, 0f));

            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                var side = (Direction)i;
                if (Layout.Map.IsConnected(room.Column, room.Row, side)) continue;

                var plug = instantiate(wallPlugPrefab, cell);
                plug.name = $"WallPlug_{side}";
                RoomContract.GetOpeningPose(side).ApplyTo(plug.transform);
                HideDoorFrame(instance, side, room.QuarterTurns);
            }
            return cell;
        }

        /// <summary>The hall prefab at its south-west cell; it never turns.</summary>
        private void BuildHall(GameObject prefab, Func<GameObject, Transform, GameObject> instantiate)
        {
            var plan = HallPlan.Create(Layout);
            int column = int.MaxValue, row = int.MaxValue;
            foreach (var cell in plan.Cells)
            {
                column = Math.Min(column, cell.Column);
                row = Math.Min(row, cell.Row);
            }
            var instance = instantiate(prefab, levelRoot);
            instance.name = "Room_Hall";
            instance.transform.SetLocalPositionAndRotation(new Vector3(RoomContract.CellOriginX(column), 0f, RoomContract.CellOriginZ(row)), Quaternion.identity);
        }

        /// <summary>
        /// A plugged opening is walled up: the door frame of the room's wall on that (map) side is switched off, so the
        /// plug looks like the rest of the wall. <paramref name="quarterTurns"/> is how far the room is turned.
        /// </summary>
        public static void HideDoorFrame(GameObject room, Direction side, int quarterTurns)
        {
            var local = (Direction)((((int)side - quarterTurns) % DirectionExtensions.Count + DirectionExtensions.Count) % DirectionExtensions.Count);
            var frame = room.transform.Find($"Shell/Wall_{local}/DoorFrame");
            if (frame != null) frame.gameObject.SetActive(false);
        }

        /// <summary>One door per connected pair of cells (each pair is visited once, from its west or south cell); the hall's are in its prefab.</summary>
        private void PlaceDoors(Transform[] cells, Func<GameObject, Transform, GameObject> instantiate)
        {
            var map = Layout.Map;
            foreach (var room in Layout.Rooms)
            {
                foreach (var side in new[] { Direction.East, Direction.North })
                {
                    if (!map.IsConnected(room.Column, room.Row, side)) continue;
                    var other = Layout.GetRoom(room.Column + side.ColumnOffset(), room.Row + side.RowOffset());
                    if (room.Type == RoomType.Hall || other.Type == RoomType.Hall) continue;
                    switch (RoomRules.DoorBetween(room.Type, other.Type))
                    {
                        case DoorPlacement.First:
                            PlaceDoor(cells[room.Row * map.Width + room.Column], side, instantiate);
                            break;
                        case DoorPlacement.Second:
                            PlaceDoor(cells[other.Row * map.Width + other.Column], side.Opposite(), instantiate);
                            break;
                    }
                }
            }
        }

        private void PlaceDoor(Transform cell, Direction side, Func<GameObject, Transform, GameObject> instantiate)
        {
            var door = instantiate(doorPrefab, cell);
            door.name = $"Door_{side}";
            RoomContract.GetOpeningPose(side).ApplyTo(door.transform);
        }

        /// <summary>Puts the player into the lift car, facing its door (see <see cref="StartPose"/>).</summary>
        private void PlacePlayer()
        {
            if (player == null) return;

            var (position, yaw) = StartPose;

            // A CharacterController would pull the player back to the old spot if it moved while enabled.
            player.TryGetComponent(out CharacterController body);
            if (body != null) body.enabled = false;
            player.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (body != null) body.enabled = true;

            if (player.TryGetComponent(out FirstPersonController controller))
            {
                controller.SetView(yaw, 0f);
            }
        }
    }
}
