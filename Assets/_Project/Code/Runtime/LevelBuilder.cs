using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Turns generated map data into a level: one cell object per grid cell at the position the room
    /// contract gives, the room prefab for that cell's type turned by its quarter turns, and a wall plug
    /// in every opening that leads nowhere. The map and the layout are plain data from Core; this class
    /// only instantiates.
    ///
    /// Plugs hang on the cell object, not on the room, because rooms turn but openings stay where the
    /// map says they are.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [SerializeField] private RoomDefinition[] roomDefinitions;
        [SerializeField] private GameObject wallPlugPrefab;
        [Tooltip("Moved to the middle of the map after every build. Optional.")]
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

        /// <summary>The cell the player starts in: the middle of the map, so every direction is open to explore.</summary>
        public (int column, int row) StartCell => (width / 2, height / 2);

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

            var prefabs = CollectPrefabs();
            if (prefabs.Count == 0)
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
            var availableTypes = new List<RoomType>(prefabs.Keys);
            availableTypes.Sort();
            Layout = RoomLayout.Generate(map, seed, availableTypes);

            levelRoot = new GameObject("Level").transform;
            levelRoot.SetParent(transform, false);
            foreach (var room in Layout.Rooms)
            {
                BuildCell(room, prefabs[room.Type], map);
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

        private Dictionary<RoomType, GameObject> CollectPrefabs()
        {
            var prefabs = new Dictionary<RoomType, GameObject>();
            if (roomDefinitions == null) return prefabs;

            foreach (var definition in roomDefinitions)
            {
                if (definition == null || definition.Prefab == null) continue;
                if (prefabs.ContainsKey(definition.Type))
                {
                    Debug.LogWarning($"LevelBuilder: more than one definition for {definition.Type}; using the first.", this);
                    continue;
                }
                prefabs.Add(definition.Type, definition.Prefab);
            }
            return prefabs;
        }

        private void BuildCell(PlacedRoom room, GameObject prefab, MapData map)
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
            }
        }

        private void PlacePlayer()
        {
            if (player == null) return;

            var (column, row) = StartCell;
            var position = new Vector3(RoomContract.CellOriginX(column), 0f, RoomContract.CellOriginZ(row));

            // A CharacterController would pull the player back to the old spot if it moved while enabled.
            player.TryGetComponent(out CharacterController body);
            if (body != null) body.enabled = false;
            player.position = position;
            if (body != null) body.enabled = true;

            if (player.TryGetComponent(out FirstPersonController controller))
            {
                controller.SetView(0f, 0f);
            }
        }
    }
}
