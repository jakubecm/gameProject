using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The camera behind the intro and the main menu. It plays a loop of shots through the real map:
    /// one composed move through the server hall (a path with turns, a rise and a look that swings from side to side; it stops well before
    /// the far end), one slider from the corridor through the opening door into the power room (or else the security office; never both), a slow drone arc high
    /// in the office, the cooling plant and the storage room, and two slow dollies down a corridor. Every move runs at a constant speed,
    /// never easing in or out, so the camera is already travelling when a shot begins and still travelling while the picture fades out:
    /// the motion carries over the cut. A shot is finished <see cref="CutLead"/> seconds before its end (<see cref="ShotFinished"/>); the
    /// caller fades out while the camera keeps moving, calls <see cref="NextShot"/> and fades in. Without a hall in the map it looks out of
    /// the lift instead. Uses unscaled time.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MenuCameraRig : MonoBehaviour
    {
        // Clear of the perimeter wall and of the pod ends.
        private const float WallMargin = 1.4f;

        private const float HallSpeed = 0.7f;

        // A shot is reported finished this long before the end of its move; the cut (fade out) has to fit in it, so the camera never stops.
        public const float CutLead = 0.5f;

        // Offset from the cross walkway's centre line: the building columns (0.6 m square) stand on it, on the cell corners.
        private const float WalkwayOffset = 1.2f;

        private const float DroneHeight = 2.75f;
        private const float DroneRadius = 2.3f;
        private const float DroneSeconds = 34f;
        private const float DroneSweepDegrees = 100f;

        private const float SlideSeconds = 14f;
        private const float SlideStartBeforeDoor = 4.5f;
        private const float SlideDepthInside = 2.2f;
        private const float DoorOpenDistance = 3.6f;

        private const float DollySeconds = 10f;
        private const float DollyDistance = 4.5f;

        // The rooms a slider can go into, in the order tried.
        private static readonly RoomType[] SliderRooms = { RoomType.PowerRoom, RoomType.SecurityOffice };

        // Distances before the door the slider can start at, longest first; the first one whose whole run is clear is used.
        private static readonly float[] SlideStarts = { SlideStartBeforeDoor, 3.5f, 2.5f };

        private const float BodyRadius = 0.3f;

        private enum Kind
        {
            Static,
            Path,
            Slide,
            Orbit,
        }

        private sealed class Shot
        {
            public Kind Kind;
            public float Seconds;

            // Static and slide: where, and which way it looks (degrees; pitch positive looks down).
            public Vector3 From;
            public Vector3 To;
            public float Yaw;
            public float Pitch;

            // Path: the camera goes through Path (a smooth curve) and always looks at the matching point of Look.
            public Vector3[] Path;
            public Vector3[] Look;

            // Path: distance along Path at each point (the first is 0), so the camera travels at a constant speed.
            public float[] Distance;

            // Orbit: an arc around From at its height.
            public float Radius;
            public float StartAngle;
            public float Sweep;
            public float LookHeight;

            // Slide: the door the camera flies through, opened while it is near.
            public AutoSlidingDoor Door;
            public Vector3 DoorPoint;
        }

        private readonly List<Shot> shots = new List<Shot>();
        private Camera view;
        private int index;
        private float time;
        private AutoSlidingDoor openedDoor;

        public bool ShotFinished => shots.Count > 0 && time >= shots[index].Seconds - CutLead;

        /// <summary>Whether the camera renders; off while the game is played.</summary>
        public bool Rendering
        {
            get => view.enabled;
            set
            {
                view.enabled = value;
                enabled = value;
                if (!value) ReleaseDoor();
            }
        }

        private void Awake()
        {
            view = GetComponent<Camera>();
            view.fieldOfView = 62f;
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 150f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.black;
        }

        private void OnDisable() => ReleaseDoor();

        /// <summary>Plans the shots from the built level.</summary>
        public void Configure(LevelBuilder builder)
        {
            shots.Clear();
            var layout = builder.Layout;
            if (layout != null)
            {
                AddHallShots(layout);
                // Exactly one slider: the first of these rooms with a clear run from the corridor through its door.
                foreach (var type in SliderRooms)
                {
                    if (AddSlide(layout, type)) break;
                }
                AddDrone(layout, RoomType.Office);
                AddDrone(layout, RoomType.CoolingPlant);
                AddDrone(layout, RoomType.StorageRoom);
                AddCorridorShots(layout);
            }
            if (shots.Count == 0)
            {
                var (position, yaw) = builder.StartPose;
                shots.Add(new Shot { Kind = Kind.Static, Seconds = 20f, From = position + Vector3.up * 1.6f, Yaw = yaw });
            }
            Begin(0);
        }

        // ---------------------------------------------------------------- planning

        // One composed move in the hall, covering only part of the way.
        private void AddHallShots(RoomLayout layout)
        {
            bool found = false;
            int minColumn = int.MaxValue, maxColumn = int.MinValue, minRow = int.MaxValue, maxRow = int.MinValue;
            foreach (var room in layout.Rooms)
            {
                if (room.Type != RoomType.Hall) continue;
                found = true;
                minColumn = Mathf.Min(minColumn, room.Column);
                maxColumn = Mathf.Max(maxColumn, room.Column);
                minRow = Mathf.Min(minRow, room.Row);
                maxRow = Mathf.Max(maxRow, room.Row);
            }
            if (!found || maxColumn == minColumn) return;

            float inner = HallContract.WallInnerOffset - WallMargin;
            float west = RoomContract.CellOriginX(minColumn) - inner;
            float north = RoomContract.CellOriginZ(maxRow) + inner;
            float aisleZ = RoomContract.CellOriginZ((minRow + maxRow) / 2);
            // The cross walkway between the first two columns of tiles (3.6 m wide, building columns on its centre line at the cell corners).
            float walkway = RoomContract.CellOriginX(minColumn) + RoomContract.CellHalfSize;

            // 1. Down the hot aisle, out into the cross walkway, round the corner to the north and up, looking ahead and then at the pod ends.
            float turnZ = Mathf.Min(aisleZ + 4.5f, north);
            AddPath(
                new[]
                {
                    new Vector3(west, 1.5f, aisleZ),
                    new Vector3(walkway - 5f, 1.6f, aisleZ),
                    new Vector3(walkway - 1.2f, 1.8f, aisleZ),
                    new Vector3(walkway, 2.2f, aisleZ + 2.5f),
                    new Vector3(walkway, 2.8f, turnZ),
                },
                new[]
                {
                    new Vector3(west + 8f, 1.5f, aisleZ),
                    new Vector3(walkway, 1.7f, aisleZ),
                    new Vector3(walkway + 0.5f, 2.0f, aisleZ + 3f),
                    new Vector3(walkway + 3f, 2.2f, aisleZ + 6f),
                    new Vector3(walkway - 4f, 2.4f, turnZ + 4f),
                });
        }

        private void AddPath(Vector3[] path, Vector3[] look)
        {
            var distance = new float[path.Length];
            for (int i = 1; i < path.Length; i++) distance[i] = distance[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            shots.Add(new Shot { Kind = Kind.Path, Path = path, Look = look, Distance = distance, Seconds = distance[path.Length - 1] / HallSpeed + CutLead });
        }

        // A slider from the corridor in front of the room's door, through the door (which opens as the camera comes), into the room.
        // It is only added when the run from its start to its end is free of walls and props (the corridor in front of the door can turn).
        private bool AddSlide(RoomLayout layout, RoomType type)
        {
            if (!TryFindRoom(layout, type, out var room)) return false;

            var cell = CellOrigin(room);
            var turn = Quaternion.Euler(0f, room.YawDegrees, 0f);
            float doorZ = -RoomContract.CellHalfSize;
            var doorPoint = cell + turn * new Vector3(0f, 0f, doorZ);
            var to = cell + turn * new Vector3(0f, 1.6f, doorZ + SlideDepthInside);
            Physics.SyncTransforms();
            foreach (float before in SlideStarts)
            {
                var from = cell + turn * new Vector3(0f, 1.6f, doorZ - before);
                if (!IsRunClear(from, to)) continue;

                shots.Add(new Shot
                {
                    Kind = Kind.Slide, Seconds = SlideSeconds, From = from, To = to, Yaw = room.YawDegrees, Pitch = 3f,
                    DoorPoint = doorPoint, Door = NearestDoor(doorPoint),
                });
                return true;
            }
            return false;
        }

        // Whether the camera can start at <paramref name="from"/> and fly straight to <paramref name="to"/> without being in or passing a
        // wall; doors (which open for the camera) and the player do not count.
        private static bool IsRunClear(Vector3 from, Vector3 to)
        {
            foreach (var hit in Physics.OverlapSphere(from, BodyRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsObstacle(hit)) return false;
            }
            var direction = to - from;
            foreach (var hit in Physics.SphereCastAll(from, BodyRadius, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsObstacle(hit.collider)) return false;
            }
            return true;
        }

        private static bool IsObstacle(Collider collider) =>
            collider.GetComponentInParent<AutoSlidingDoor>() == null && collider.GetComponentInParent<CharacterController>() == null;

        // A slow arc high in the room, looking down at its middle, like a drone.
        private void AddDrone(RoomLayout layout, RoomType type)
        {
            if (!TryFindRoom(layout, type, out var room)) return;

            // A small room is the 6 x 6 m square by the south door of its cell, turned with the room.
            var centre = CellOrigin(room) + Quaternion.Euler(0f, room.YawDegrees, 0f) * new Vector3(0f, 0f, -RoomContract.SmallRoomDepth / 2f);
            centre.y = DroneHeight;
            // Starts over the back wall.
            shots.Add(new Shot
            {
                Kind = Kind.Orbit, Seconds = DroneSeconds, From = centre, Radius = DroneRadius, StartAngle = room.YawDegrees,
                Sweep = DroneSweepDegrees, LookHeight = 0.4f,
            });
        }

        // Slow dollies down the longest straight run of connected corridor tiles: from its start, and back from its end.
        private void AddCorridorShots(RoomLayout layout)
        {
            var map = layout.Map;
            int best = 0;
            (int column, int row, Direction direction) bestStart = (0, 0, Direction.East);
            foreach (var direction in new[] { Direction.East, Direction.North })
            {
                for (int row = 0; row < map.Height; row++)
                {
                    for (int column = 0; column < map.Width; column++)
                    {
                        // Only start where the run does not continue backwards.
                        int backColumn = column - direction.ColumnOffset();
                        int backRow = row - direction.RowOffset();
                        if (IsCorridorAt(layout, backColumn, backRow) && map.IsConnected(backColumn, backRow, direction)) continue;

                        int length = 0;
                        int c = column, r = row;
                        while (IsCorridorAt(layout, c, r))
                        {
                            length++;
                            if (!map.IsConnected(c, r, direction)) break;
                            c += direction.ColumnOffset();
                            r += direction.RowOffset();
                        }
                        if (length > best)
                        {
                            best = length;
                            bestStart = (column, row, direction);
                        }
                    }
                }
            }
            if (best < 2) return;

            var (startColumn, startRow, run) = bestStart;
            var along = new Vector3(run.ColumnOffset(), 0f, run.RowOffset());
            var first = new Vector3(RoomContract.CellOriginX(startColumn), 1.7f, RoomContract.CellOriginZ(startRow));
            var last = first + along * ((best - 1) * RoomContract.CellSize);
            float yaw = run == Direction.East ? 90f : 0f;
            var forward = first - along * 4f;
            var back = last + along * 4f;
            Physics.SyncTransforms();
            if (IsRunClear(forward, forward + along * DollyDistance))
            {
                shots.Add(new Shot { Kind = Kind.Slide, Seconds = DollySeconds, From = forward, To = forward + along * DollyDistance, Yaw = yaw, Pitch = 2f });
            }
            if (IsRunClear(back, back - along * DollyDistance))
            {
                shots.Add(new Shot { Kind = Kind.Slide, Seconds = DollySeconds, From = back, To = back - along * DollyDistance, Yaw = yaw + 180f, Pitch = 2f });
            }
        }

        private static bool TryFindRoom(RoomLayout layout, RoomType type, out PlacedRoom found)
        {
            foreach (var room in layout.Rooms)
            {
                if (room.Type != type) continue;
                found = room;
                return true;
            }
            found = default;
            return false;
        }

        private static Vector3 CellOrigin(PlacedRoom room) => new Vector3(RoomContract.CellOriginX(room.Column), 0f, RoomContract.CellOriginZ(room.Row));

        private static bool IsCorridorAt(RoomLayout layout, int column, int row) =>
            layout.Map.InBounds(column, row) && layout.HasRoom(column, row) && RoomRules.IsCorridor(layout.GetRoom(column, row).Type);

        private static AutoSlidingDoor NearestDoor(Vector3 point)
        {
            AutoSlidingDoor nearest = null;
            float best = 2.5f;
            foreach (var door in FindObjectsByType<AutoSlidingDoor>(FindObjectsSortMode.None))
            {
                float distance = HorizontalDistance(door.transform.position, point);
                if (distance >= best) continue;
                best = distance;
                nearest = door;
            }
            return nearest;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // ---------------------------------------------------------------- playing

        public void Begin(int shot)
        {
            ReleaseDoor();
            index = shots.Count == 0 ? 0 : shot % shots.Count;
            time = 0f;
            Apply();
        }

        public void NextShot() => Begin(index + 1);

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            Apply();
        }

        private void ReleaseDoor()
        {
            if (openedDoor != null) openedDoor.ForceOpen = false;
            openedDoor = null;
        }

        private void Apply()
        {
            if (shots.Count == 0) return;

            var shot = shots[index];
            float progress = Mathf.Clamp01(time / shot.Seconds);
            Vector3 position;
            Quaternion rotation;
            switch (shot.Kind)
            {
                case Kind.Path:
                {
                    float travelled = progress * shot.Distance[shot.Distance.Length - 1];
                    int segment = 0;
                    while (segment < shot.Path.Length - 2 && travelled > shot.Distance[segment + 1]) segment++;
                    float u = Mathf.Clamp01((travelled - shot.Distance[segment]) / Mathf.Max(shot.Distance[segment + 1] - shot.Distance[segment], 1e-4f));
                    position = CatmullRom(shot.Path, segment, u);
                    var target = CatmullRom(shot.Look, segment, u);
                    rotation = Quaternion.LookRotation(target - position, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.17f) * 0.5f);
                    break;
                }
                case Kind.Slide:
                    position = Vector3.Lerp(shot.From, shot.To, progress);
                    rotation = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f);
                    UpdateDoor(shot, position);
                    break;
                case Kind.Orbit:
                {
                    float angle = (shot.StartAngle + shot.Sweep * progress) * Mathf.Deg2Rad;
                    position = shot.From + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * shot.Radius;
                    var target = new Vector3(shot.From.x, shot.LookHeight, shot.From.z);
                    rotation = Quaternion.LookRotation(target - position, Vector3.up);
                    break;
                }
                default:
                    position = shot.From;
                    rotation = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f);
                    break;
            }
            transform.SetPositionAndRotation(position, rotation);
        }

        // The door is held open while the camera is within a few metres of it.
        private void UpdateDoor(Shot shot, Vector3 position)
        {
            if (shot.Door == null) return;

            bool near = HorizontalDistance(position, shot.DoorPoint) < DoorOpenDistance;
            shot.Door.ForceOpen = near;
            openedDoor = shot.Door;
        }

        // The point at <paramref name="u"/> (0 to 1) of the segment from points[i] to points[i + 1].
        private static Vector3 CatmullRom(Vector3[] points, int i, float u)
        {
            var p0 = points[Mathf.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Mathf.Min(i + 2, points.Length - 1)];
            return 0.5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (3f * p1 - p0 - 3f * p2 + p3) * u * u * u);
        }
    }
}
