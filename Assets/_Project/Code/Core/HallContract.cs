namespace ColdShutdown.Core
{
    /// <summary>
    /// The hall contract: dimensions of the server hall tiles. A hall tile uses the same 12 x 12 m cell
    /// and the same openings as every room (<see cref="RoomContract"/>), but it is 6 m high and has no
    /// walls between neighbouring hall tiles. Keep CLAUDE.md in sync when changing these values.
    ///
    /// Layout of one tile (room-local, metres from the center):
    /// - two pods side by side across the tile, one north and one south of the middle (<see cref="PodCenterZ"/>). Each
    ///   pod is a long contained cold aisle running east-west: two straight rows of fourteen racks, side by side
    ///   without gaps, fronts facing each other across a 1.2 m aisle. The aisle is closed on every side: rack backs
    ///   and sides outside, a containment roof lying on the rack tops, automatic sliding doors at both ends. Its floor
    ///   is perforated tiles only;
    /// - the rack backs of the two pods face each other across the hot aisle in the middle of the tile;
    /// - a free walkway around the pods: 1.6 m to the east and west perimeter walls (3.6 m cross walkway between the
    ///   pod ends of neighbouring tiles), 1.5 m to the north and south perimeter walls (3.4 m between tiles);
    /// - building columns only on the cell corners (one quarter in each of the four tiles).
    /// </summary>
    public static class HallContract
    {
        /// <summary>Underside of the roof deck (clear height of the hall).</summary>
        public const float CeilingHeight = 6f;

        public const float RoofThickness = 0.15f;

        /// <summary>Width of the contained cold aisle between the two rack rows (two perforated tiles).</summary>
        public const float AisleWidth = 1.2f;

        public const int RacksPerRow = 14;

        /// <summary>Pods (contained aisles) per tile, side by side across the tile.</summary>
        public const int PodsPerTile = 2;

        public const int PodRackSlots = 2 * RacksPerRow;

        public const int TileRackSlots = PodsPerTile * PodRackSlots;

        /// <summary>Half length of a pod along X (the rows): fourteen racks side by side.</summary>
        public const float PodHalfLength = RacksPerRow * RoomContract.RackWidth / 2f;

        /// <summary>Half depth of a pod along Z: half the aisle plus one rack.</summary>
        public const float PodHalfDepth = AisleWidth / 2f + RoomContract.RackDepth;

        /// <summary>Distance from the cell center to the middle of each pod's aisle (north pod +Z, south pod -Z).</summary>
        public const float PodCenterZ = 2.5f;

        /// <summary>Distance from the cell center to the outer rack backs of the pods.</summary>
        public const float PodOuterZ = PodCenterZ + PodHalfDepth;

        /// <summary>Width of the hot aisle in the middle of the tile, between the rack backs of the two pods.</summary>
        public const float HotAisleWidth = 2f * (PodCenterZ - PodHalfDepth);

        /// <summary>Underside of the containment roof over the pod: the roof panels lie directly on the rack tops, no gap.</summary>
        public const float ContainmentHeight = RoomContract.RackHeight;

        /// <summary>Thickness of the containment roof panels (its top is <see cref="ContainmentHeight"/> + this).</summary>
        public const float ContainmentPanel = 0.04f;

        /// <summary>Distance from the cell center to the inner face of a perimeter wall (same as a room wall).</summary>
        public const float WallInnerOffset = RoomContract.InteriorHalfSize;

        /// <summary>Distance from the cell center to the yellow walkway line in front of the perimeter wall equipment.</summary>
        public const float PerimeterLineOffset = WallInnerOffset - 0.5f;

        /// <summary>The yellow outline around each pod stands this far out from its racks.</summary>
        public const float PodOutline = 0.3f;

        /// <summary>Free floor between the pod ends and an east or west perimeter wall.</summary>
        public const float EndWalkway = WallInnerOffset - PodHalfLength;

        /// <summary>Free floor between the outer rack backs and a north or south perimeter wall.</summary>
        public const float SideWalkway = WallInnerOffset - PodOuterZ;

        /// <summary>Building columns are 0.6 m square and stand on the cell corners.</summary>
        public const float ColumnSize = 0.6f;

        /// <summary>Center of pod <paramref name="pod"/> (0 = north, 1 = south) along Z.</summary>
        public static float PodZ(int pod) => pod == 0 ? PodCenterZ : -PodCenterZ;

        /// <summary>
        /// Rack slots of both pods, north pod first. In each pod the north row faces south (yaw 180) and the south
        /// row faces north (yaw 0), so both fronts face the aisle; the slots in a row touch each other.
        /// </summary>
        public static SlotPose[] GetPodRackSlots()
        {
            var slots = new SlotPose[TileRackSlots];
            float rowOffset = AisleWidth / 2f + RoomContract.RackDepth / 2f;
            float firstX = -PodHalfLength + RoomContract.RackWidth / 2f;
            int index = 0;
            for (int pod = 0; pod < PodsPerTile; pod++)
            {
                foreach (int side in new[] { 1, -1 })
                {
                    for (int i = 0; i < RacksPerRow; i++)
                    {
                        slots[index++] = new SlotPose(firstX + i * RoomContract.RackWidth, PodZ(pod) + side * rowOffset, side > 0 ? 180f : 0f);
                    }
                }
            }
            return slots;
        }
    }
}
