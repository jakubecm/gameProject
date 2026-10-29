namespace ColdShutdown.Core
{
    /// <summary>
    /// Position of a slot inside a room, in room-local metres. The room pivot is the center
    /// of the floor, floor at y = 0, +Z is north. Yaw is rotation around +Y in degrees
    /// (yaw 90 makes the slot face +X); the slot faces into the room.
    /// </summary>
    public readonly struct SlotPose
    {
        public readonly float X;
        public readonly float Z;
        public readonly float YawDegrees;

        public SlotPose(float x, float z, float yawDegrees)
        {
            X = x;
            Z = z;
            YawDegrees = yawDegrees;
        }
    }

    /// <summary>
    /// The room contract: the one place where room dimensions live. The generator, the level
    /// builder and the editor tools all read these values. Keep CLAUDE.md in sync when changing them.
    /// All lengths are in metres.
    /// </summary>
    public static class RoomContract
    {
        // Cell and walls
        public const float CellSize = 12f;
        public const float CeilingHeight = 3f;
        public const float WallThickness = 0.2f;

        /// <summary>Walkable width/depth inside the walls.</summary>
        public const float InteriorSize = CellSize - 2f * WallThickness;

        /// <summary>Distance from the room center to the inner face of a wall.</summary>
        public const float InteriorHalfSize = InteriorSize / 2f;

        /// <summary>Distance from the room center to the outer face of a wall (the cell edge).</summary>
        public const float CellHalfSize = CellSize / 2f;

        // Openings: one per side, centered, always in the same place
        public const float OpeningWidth = 2.4f;
        public const float OpeningHeight = 2.6f;

        /// <summary>Width of the walkway from every opening to the center that must stay empty.</summary>
        public const float KeepClearWidth = 2.4f;

        /// <summary>
        /// The keep-clear cross is a walkway, so it reaches from the floor up to the opening height.
        /// Ceiling fittings above it (lights, detectors, trays over a door) do not block it.
        /// </summary>
        public const float KeepClearHeight = OpeningHeight;

        /// <summary>Distance from the room center to the middle of a wall (where a wall plug stands).</summary>
        public const float WallCenterOffset = CellHalfSize - WallThickness / 2f;

        // The generator may rotate rooms in these steps
        public const int RotationStepDegrees = 90;

        // Racks
        public const float RackWidth = 0.6f;
        public const float RackDepth = 1.2f;
        public const float RackHeight = 2.0f;

        // Automatic doors: the leaves of a bi-parting sliding door slide along the wall on the room side, so the
        // wall beside every opening of a room with automatic doors stays free up to the door head.

        /// <summary>
        /// Distance along the wall from the opening's middle to the end of the door pocket: the open leaves reach
        /// 2.4 m, the operator box 2.5 m, the access reader beside it 2.63 m.
        /// </summary>
        public const float DoorPocketEnd = 2.7f;

        /// <summary>Depth in front of the wall the leaves, their rails and the door operator need.</summary>
        public const float DoorPocketDepth = 0.3f;

        /// <summary>Height up to which the door pocket stays free (leaves plus the operator above the opening).</summary>
        public const float DoorPocketHeight = 2.95f;

        // Small rooms (small office, security office, power room, cooling plant, storage) are strictly square: 6 x 6 m on
        // the wall center lines (5.8 x 5.8 m inside), by the south door of the cell. The rest of the cell is built up:
        // a wall across the cell closes the room at z = 0 and two side walls at x = ±3 close it east and west.

        /// <summary>Room-local Z of the inner face of a small room's back wall; the room spans from the south wall to here.</summary>
        public const float SmallRoomBackWall = 0f;

        /// <summary>Walkable depth of a small room, from its south wall to its back wall.</summary>
        public const float SmallRoomDepth = SmallRoomBackWall + InteriorHalfSize;

        /// <summary>Distance from the room center to the inner face of a small room's side walls; the room is as wide as it is deep.</summary>
        public const float SmallRoomHalfWidth = SmallRoomDepth / 2f;

        /// <summary>A small room keeps a walkway this deep straight in from its door (instead of the whole cross).</summary>
        public const float SmallRoomKeepClearDepth = 2.2f;

        // Passenger lift (also the evacuation lift) at the closed north end of the lift end tile, opening straight onto the
        // 3 m corridor: a car for 6 to 8 people with room to spare (ISO 4190 wide car, 1.6 x 1.5 m inside, 2.3 m high)
        // behind a two-panel centre-opening door 1.1 x 2.1 m. The landing wall closes the corridor where the crossing of a
        // dead end would end; the shaft is behind it.

        /// <summary>Room-local Z of the corridor face of the lift's landing wall (the lift door's pivot).</summary>
        public const float LiftWallFace = 1.5f;

        public const float LiftWallThickness = 0.2f;
        public const float LiftDoorWidth = 1.1f;
        public const float LiftDoorHeight = 2.1f;
        public const float LiftCarWidth = 1.6f;
        public const float LiftCarDepth = 1.5f;
        public const float LiftCarHeight = 2.3f;

        /// <summary>Room-local Z of the inner face of the car's front (the door side); the car reaches north from here.</summary>
        public const float LiftCarFront = LiftWallFace + 0.34f;

        /// <summary>
        /// Where the player starts a run: the middle of the lift car, facing its door (room-local, yaw 180 = facing -Z).
        /// </summary>
        public static SlotPose LiftStartPose => new SlotPose(0f, LiftCarFront + LiftCarDepth / 2f, 180f);

        /// <summary>World X of the room pivot for a grid column.</summary>
        public static float CellOriginX(int column) => column * CellSize;

        /// <summary>World Z of the room pivot for a grid row (+Z is north).</summary>
        public static float CellOriginZ(int row) => row * CellSize;

        /// <summary>Grid column of the cell that contains a world X position (cells are centered on their origin).</summary>
        public static int ColumnAt(float worldX) => (int)System.Math.Floor(worldX / CellSize + 0.5);

        /// <summary>Grid row of the cell that contains a world Z position (cells are centered on their origin).</summary>
        public static int RowAt(float worldZ) => (int)System.Math.Floor(worldZ / CellSize + 0.5);

        /// <summary>
        /// Room-local pose of the opening on one side: centered in the wall thickness, on the floor,
        /// facing into the room. A wall plug placed with this pose fills that opening exactly.
        /// </summary>
        public static SlotPose GetOpeningPose(Direction side)
        {
            switch (side)
            {
                case Direction.North: return new SlotPose(0f, WallCenterOffset, 180f);
                case Direction.East: return new SlotPose(WallCenterOffset, 0f, 270f);
                case Direction.South: return new SlotPose(0f, -WallCenterOffset, 0f);
                default: return new SlotPose(-WallCenterOffset, 0f, 90f);
            }
        }
    }
}
