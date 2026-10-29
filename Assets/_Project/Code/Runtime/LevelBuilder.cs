using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Turns generated map data into a level: one cell object per grid cell at the position the room
    /// contract gives, the prefab of that cell's room type and layout turned by its quarter turns, and a wall plug
    /// in every opening that leads nowhere. A plugged opening loses its door frame, so it looks like the rest of the
    /// wall. The map and the layout are plain data from Core; this class
    /// only instantiates.
    ///
    /// Plugs hang on the cell object, not on the room, because rooms turn but openings stay where the
    /// map says they are.
    ///
    /// Hall tiles never turn and get no plugs: with a <see cref="HallKit"/> assigned, the kit adds their side
    /// and corner pieces (perimeter walls, doors) as <see cref="HallPlan"/> says.
    ///
    /// Every connected opening into a room gets an automatic sliding door (<see cref="RoomRules.DoorBetween"/>
    /// says which of the two cells owns it); like the plugs it hangs on the cell object, named
    /// <c>Door_&lt;Side&gt;</c>. Openings into a hall tile already have the door of the hall's side piece.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [SerializeField] private RoomDefinition[] roomDefinitions;
        [SerializeField] private GameObject wallPlugPrefab;
        [Tooltip("Automatic sliding door put into every connected opening of a room. Optional.")]
        [SerializeField] private GameObject doorPrefab;
        [Tooltip("Assembles hall tiles. Optional while no layout places halls.")]
        [SerializeField] private HallKit hallKit;
        [Tooltip("Moved into the lift (or the middle of the map without one) after every build. Optional.")]
        [SerializeField] private Transform player;

        [Header("Size")]
        [SerializeField, Min(1)] private int width = 6;
        [SerializeField, Min(1)] private int height = 6;

        [Header("On Start")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool randomSeedOnStart = true;
        [SerializeField] private int startSeed = 12345;

        private Transform levelRoot;

        /// <summary>Raised after a level has been built (and the player moved), for the debug menu and later systems.</summary>
        public event Action<LevelBuilder> LevelBuilt;

        public bool HasLevel => Layout != null;

        public int Seed { get; private set; }

        public int Width => width;

        public int Height => height;

        public MapData Map => Layout?.Map;

        public RoomLayout Layout { get; private set; }

        /// <summary>The cell the player starts in: the lift end, or the middle of the map if the layout has no lift.</summary>
        public (int column, int row) StartCell => Layout != null ? Layout.StartCell : (width / 2, height / 2);

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

        public static int NewRandomSeed() => new System.Random().Next(1, 1000000);

        private void Start()
        {
            if (buildOnStart)
            {
                Build(randomSeedOnStart ? NewRandomSeed() : startSeed);
            }
        }

        public bool Build(int seed) => Build(seed, width, height);

        /// <summary>Replaces the current level with the one for this seed and size. Returns false (and logs why) if it cannot be built.</summary>
        public bool Build(int seed, int newWidth, int newHeight)
        {
            if (newWidth < 1 || newHeight < 1)
            {
                Debug.LogError($"LevelBuilder: map size {newWidth} x {newHeight} is not valid.", this);
                return false;
            }
            if (wallPlugPrefab == null)
            {
                Debug.LogError("LevelBuilder: no wall plug prefab assigned.", this);
                return false;
            }

            var definitions = CollectDefinitions();
            if (definitions.Count == 0)
            {
                Debug.LogError("LevelBuilder: no room definition with a prefab assigned.", this);
                return false;
            }

            Clear();
            width = newWidth;
            height = newHeight;
            Seed = seed;

            var map = MapGenerator.Generate(seed, width, height);

            // Sorted, so the layout never depends on the order the definitions were assigned in.
            var availableTypes = new List<RoomType>(definitions.Keys);
            availableTypes.Sort();
            Layout = RoomLayout.Generate(map, seed, availableTypes);

            var halls = hallKit != null ? HallPlan.Create(Layout) : null;

            levelRoot = new GameObject("Level").transform;
            levelRoot.SetParent(transform, false);
            var cells = new Transform[width * height];
            foreach (var room in Layout.Rooms)
            {
                cells[room.Row * width + room.Column] = halls != null && halls.TryGetCell(room.Column, room.Row, out var hall)
                    ? BuildHallCell(room, definitions[room.Type].Prefab, hall)
                    : BuildCell(room, definitions[room.Type].GetLayout(room.Layout), map);
            }
            if (doorPrefab != null)
            {
                PlaceDoors(cells);
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

        private Transform BuildCell(PlacedRoom room, GameObject prefab, MapData map)
        {
            var cell = new GameObject($"Cell_{room.Column}_{room.Row}").transform;
            cell.SetParent(levelRoot, false);
            cell.localPosition = new Vector3(RoomContract.CellOriginX(room.Column), 0f, RoomContract.CellOriginZ(room.Row));

            var instance = Instantiate(prefab, cell);
            instance.name = $"Room_{room.Type}";
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, room.YawDegrees, 0f));

            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                var side = (Direction)i;
                if (map.IsConnected(room.Column, room.Row, side)) continue;

                var plug = Instantiate(wallPlugPrefab, cell);
                plug.name = $"WallPlug_{side}";
                RoomContract.GetOpeningPose(side).ApplyTo(plug.transform);
                HideDoorFrame(instance, side, room.QuarterTurns);
            }
            return cell;
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

        private Transform BuildHallCell(PlacedRoom room, GameObject prefab, HallCell hall)
        {
            var cell = new GameObject($"Cell_{room.Column}_{room.Row}").transform;
            cell.SetParent(levelRoot, false);
            cell.localPosition = new Vector3(RoomContract.CellOriginX(room.Column), 0f, RoomContract.CellOriginZ(room.Row));

            var instance = Instantiate(prefab, cell);
            instance.name = $"Room_{room.Type}";
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            HallKit.LabelTile(instance, hall);
            hallKit.AddPieces(cell, hall);
            return cell;
        }

        /// <summary>One door per connected pair of cells (each pair is visited once, from its west or south cell).</summary>
        private void PlaceDoors(Transform[] cells)
        {
            var map = Layout.Map;
            foreach (var room in Layout.Rooms)
            {
                foreach (var side in new[] { Direction.East, Direction.North })
                {
                    if (!map.IsConnected(room.Column, room.Row, side)) continue;
                    var other = Layout.GetRoom(room.Column + side.ColumnOffset(), room.Row + side.RowOffset());
                    switch (RoomRules.DoorBetween(room.Type, other.Type))
                    {
                        case DoorPlacement.First:
                            PlaceDoor(cells[room.Row * width + room.Column], side);
                            break;
                        case DoorPlacement.Second:
                            PlaceDoor(cells[other.Row * width + other.Column], side.Opposite());
                            break;
                    }
                }
            }
        }

        private void PlaceDoor(Transform cell, Direction side)
        {
            var door = Instantiate(doorPrefab, cell);
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
