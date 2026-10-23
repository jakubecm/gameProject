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

        /// <summary>Distance from the room center to the middle of a wall (where a wall plug stands).</summary>
        public const float WallCenterOffset = CellHalfSize - WallThickness / 2f;

        // The generator may rotate rooms in these steps
        public const int RotationStepDegrees = 90;

        // Racks
        public const float RackWidth = 0.6f;
        public const float RackDepth = 1.2f;
        public const float RackHeight = 2.0f;

        // Server aisle: 6 slots along each of the west and east walls, 3 on each side of the opening
        public const int ServerAisleRackSlots = 12;
        public const int ServerAisleSlotsPerRun = 3;
        public const float ServerAisleSlotPitch = 1.2f;

        /// <summary>Distance from the room center to the first slot of a run, along the wall.</summary>
        public const float ServerAisleFirstSlotOffset = 2.4f;

        /// <summary>X of the rack center on the east/west walls: the rack's back stands against the wall.</summary>
        public const float ServerAisleSlotWallOffset = InteriorHalfSize - RackDepth / 2f;

        /// <summary>World X of the room pivot for a grid column.</summary>
        public static float CellOriginX(int column) => column * CellSize;

        /// <summary>World Z of the room pivot for a grid row (+Z is north).</summary>
        public static float CellOriginZ(int row) => row * CellSize;

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

        /// <summary>
        /// Room-local poses of the 12 rack slots in a server aisle. West wall racks face east
        /// (yaw 90), east wall racks face west (yaw 270).
        /// </summary>
        public static SlotPose[] GetServerAisleRackSlots()
        {
            var slots = new SlotPose[ServerAisleRackSlots];
            int index = 0;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * ServerAisleSlotWallOffset;
                float yaw = side < 0 ? 90f : 270f;
                for (int i = 0; i < ServerAisleSlotsPerRun; i++)
                {
                    float offset = ServerAisleFirstSlotOffset + i * ServerAisleSlotPitch;
                    slots[index++] = new SlotPose(x, -offset, yaw);
                    slots[index++] = new SlotPose(x, offset, yaw);
                }
            }
            return slots;
        }
    }
}
