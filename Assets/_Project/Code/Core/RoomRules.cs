using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>Who gets the automatic door in an opening between two connected cells.</summary>
    public enum DoorPlacement
    {
        /// <summary>No door: corridor to corridor, or hall tile to hall tile.</summary>
        None = 0,

        /// <summary>The first cell gets the door in its opening (the leaves slide on its side).</summary>
        First = 1,

        /// <summary>The second cell gets the door in its opening.</summary>
        Second = 2,

        /// <summary>One cell is a hall tile; its door side piece already has the door.</summary>
        HallPiece = 3,
    }

    /// <summary>
    /// Rules every room type follows: which of its (local, unturned) sides may have a door, how many doors it
    /// may have, whether it is a small room, and who owns the automatic door between two rooms. Plain data;
    /// the layout, the level builder and the editor tools all read it.
    ///
    /// Side masks use one bit per <see cref="Direction"/>: bit 0 north, 1 east, 2 south, 3 west.
    /// </summary>
    public static class RoomRules
    {
        public const int North = 1 << (int)Direction.North;
        public const int East = 1 << (int)Direction.East;
        public const int South = 1 << (int)Direction.South;
        public const int West = 1 << (int)Direction.West;
        public const int AllSides = North | East | South | West;

        /// <summary>A big room (one whole cell) has at most this many doors.</summary>
        public const int BigRoomMaxDoors = 2;

        /// <summary>Every room (not a corridor or a hall tile) comes in this many fixed layouts (prefabs), chosen per cell.</summary>
        public const int RoomLayouts = 4;

        /// <summary>
        /// Door sides of a big room: two neighbouring sides. The walkways from them meet in the middle, so the other
        /// half of the room (along the north and east walls) is free for furniture and plant.
        /// </summary>
        public const int BigRoomDoorSides = South | West;

        public static int Mask(Direction side) => 1 << (int)side;

        /// <summary>
        /// Sides of the unturned prefab that may have a door. Corridors have exactly these arms; small rooms and
        /// the dead end only have their south opening; the office (the only big room) its south and west openings;
        /// hall tiles any side.
        /// </summary>
        public static int DoorSides(RoomType type)
        {
            switch (type)
            {
                case RoomType.CorridorStraight: return North | South;
                case RoomType.CorridorT: return East | South | West;
                case RoomType.CorridorCorner: return East | South;
                case RoomType.CorridorDeadEnd:
                case RoomType.Elevator:
                case RoomType.SmallOffice:
                case RoomType.SecurityOffice:
                case RoomType.PowerRoom:
                case RoomType.HvacRoom:
                case RoomType.StorageRoom:
                    return South;
                case RoomType.Office:
                    return BigRoomDoorSides;
                default: return AllSides;
            }
        }

        /// <summary>Corridor tiles, the lift end (<see cref="RoomType.Elevator"/>) among them.</summary>
        public static bool IsCorridor(RoomType type) =>
            type == RoomType.Corridor || type == RoomType.CorridorStraight || type == RoomType.CorridorT ||
            type == RoomType.CorridorCorner || type == RoomType.CorridorDeadEnd || type == RoomType.Elevator;

        /// <summary>
        /// Small rooms are a 6 x 6 m square by the south door of the cell (<see cref="RoomContract.SmallRoomHalfWidth"/>,
        /// <see cref="RoomContract.SmallRoomBackWall"/>); the rest of the cell is built up. They always have exactly one
        /// door, at the end of a corridor. Every room except the office is a small room.
        /// </summary>
        public static bool IsSmallRoom(RoomType type) =>
            type == RoomType.SmallOffice || type == RoomType.SecurityOffice || type == RoomType.PowerRoom ||
            type == RoomType.HvacRoom || type == RoomType.StorageRoom;

        /// <summary>
        /// How many fixed layouts (prefabs) a type has: <see cref="RoomLayouts"/> for rooms; for corridors one per arm
        /// the fire kit can be in that a turn cannot reach (<see cref="FireArmSide"/>): two for the corner (the second its
        /// mirror image), four for the T-junction (the fire kit in a side arm or in the stem, and their mirror images), one
        /// for the others (the lift end too); one for hall tiles.
        /// </summary>
        public static int LayoutCount(RoomType type)
        {
            switch (type)
            {
                case RoomType.CorridorCorner: return 2;
                case RoomType.CorridorT: return 4;
                case RoomType.Hall: return 1;
                default: return IsCorridor(type) ? 1 : RoomLayouts;
            }
        }

        /// <summary>
        /// The (local, unturned) arm of a corridor layout that carries the fire kit: fire-water riser, hose cabinet,
        /// extinguisher and call point. The layout picks the variant whose fire arm leads to a room, so the kit always
        /// stands by the entrance of a room, off the through-corridor. Corner layout 2 and T layouts 3 and 4 are mirror
        /// images of layouts 1 and 1, 2.
        /// </summary>
        public static Direction FireArmSide(RoomType type, int layout)
        {
            switch (type)
            {
                case RoomType.CorridorCorner: return layout == 1 ? Direction.South : Direction.East;
                case RoomType.CorridorT:
                    switch (layout)
                    {
                        case 1:
                        case 3:
                            return Direction.South;
                        case 2: return Direction.West;
                        default: return Direction.East;
                    }
                case RoomType.CorridorDeadEnd:
                case RoomType.Elevator:
                    return Direction.South;
                default: return Direction.North;
            }
        }

        /// <summary>Whether a corridor layout is the mirror image of another one (see <see cref="FireArmSide"/>).</summary>
        public static bool IsMirroredCorridor(RoomType type, int layout) =>
            (type == RoomType.CorridorCorner && layout == 1) || (type == RoomType.CorridorT && layout >= 2);

        /// <summary>Most doors a room of this type may have.</summary>
        public static int MaxDoors(RoomType type)
        {
            if (IsCorridor(type) || type == RoomType.Hall) return BitCount(DoorSides(type));
            if (IsSmallRoom(type)) return 1;
            return BigRoomMaxDoors;
        }

        /// <summary>Rooms whose entrances are automatic sliding glass doors with an access reader: every non-corridor.</summary>
        public static bool HasAutomaticDoors(RoomType type) => !IsCorridor(type);

        /// <summary>Turns a mask of local sides by quarter turns (clockwise seen from above, like the room's yaw).</summary>
        public static int Rotate(int mask, int quarterTurns)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            return ((mask << turns) | (mask >> (4 - turns))) & AllSides;
        }

        public static int BitCount(int mask)
        {
            int count = 0;
            for (int bits = mask & AllSides; bits != 0; bits &= bits - 1)
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// The quarter turns with which a room of this type fits a cell whose connected sides are
        /// <paramref name="connected"/>. A corridor fits only if its arms are exactly the connected sides; a room
        /// fits if it has few enough doors and every connected side is one of its door sides. Empty if it never fits.
        /// </summary>
        public static List<int> FittingTurns(RoomType type, int connected)
        {
            var turns = new List<int>(4);
            connected &= AllSides;
            if (type == RoomType.Hall || BitCount(connected) > MaxDoors(type))
            {
                return turns;
            }
            for (int q = 0; q < 4; q++)
            {
                int sides = Rotate(DoorSides(type), q);
                bool fits = IsCorridor(type) ? sides == connected : (connected & ~sides) == 0;
                // A cross fits every way round; the turn then decides which arm has the fire kit.
                if (fits && !turns.Contains(q))
                {
                    turns.Add(q);
                }
            }
            return turns;
        }

        /// <summary>
        /// Who gets the door between two connected cells. A hall tile's door side piece already has one; otherwise
        /// the room (not the corridor) gets it, and between two rooms the first one.
        /// </summary>
        public static DoorPlacement DoorBetween(RoomType first, RoomType second)
        {
            if (first == RoomType.Hall && second == RoomType.Hall) return DoorPlacement.None;
            if (first == RoomType.Hall || second == RoomType.Hall) return DoorPlacement.HallPiece;
            if (HasAutomaticDoors(first)) return DoorPlacement.First;
            if (HasAutomaticDoors(second)) return DoorPlacement.Second;
            return DoorPlacement.None;
        }
    }
}
