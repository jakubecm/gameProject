namespace ColdShutdown.Core
{
    /// <summary>
    /// Kind of room a cell can hold. Values are explicit so serialized assets stay valid
    /// when types are added; only ever append new values. Value 1 was the old server aisle room (racks now
    /// stand only in the hall and the security office) and value 2 the full-size office (removed: every room is a small room now); never reuse them.
    /// </summary>
    public enum RoomType
    {
        /// <summary>Corridor crossing: arms to all four sides.</summary>
        Corridor = 0,

        /// <summary>
        /// One tile of a server hall. Neighbouring hall tiles have no walls between them; the hall kit
        /// (see <see cref="HallPlan"/>) adds the perimeter walls and doors per side.
        /// </summary>
        Hall = 3,

        /// <summary>Security office (small room) with the video wall that shows the camera feeds.</summary>
        SecurityOffice = 4,

        /// <summary>Small office: a 6 x 6 m room by the door, one door.</summary>
        SmallOffice = 5,

        /// <summary>Electrical switch room (small room) with two transformers, switchgear and the load meters per hall section.</summary>
        PowerRoom = 6,

        /// <summary>Cooling plant (small room) with two plate heat exchangers, pumps, risers and the coolant meters.</summary>
        HvacRoom = 7,

        /// <summary>Straight corridor (north-south).</summary>
        CorridorStraight = 8,

        /// <summary>T-junction (east, south and west arms).</summary>
        CorridorT = 9,

        /// <summary>Corner (east and south arms).</summary>
        CorridorCorner = 10,

        /// <summary>Dead end (south arm only).</summary>
        CorridorDeadEnd = 11,

        /// <summary>Storage room (small room): shelving with spare parts and archive boxes, and a fixed sliding gate.</summary>
        StorageRoom = 12,

        /// <summary>
        /// Lift end: a corridor tile like the dead end (south arm only) whose closed end is the landing of the passenger
        /// lift, which is also the evacuation lift. No room in between: the lift opens straight onto the corridor, its own
        /// doors open automatically. The player starts every run in the lift car.
        /// </summary>
        Elevator = 13,
    }

    public static class RoomTypeExtensions
    {
        /// <summary>One character per room type, for the text map in the debug menu.</summary>
        public static char Symbol(this RoomType type)
        {
            switch (type)
            {
                case RoomType.Corridor:
                case RoomType.CorridorStraight:
                case RoomType.CorridorT:
                case RoomType.CorridorCorner:
                    return 'C';
                case RoomType.CorridorDeadEnd: return 'D';
                case RoomType.SmallOffice: return 'o';
                case RoomType.Hall: return 'H';
                case RoomType.SecurityOffice: return 'G';
                case RoomType.PowerRoom: return 'P';
                case RoomType.HvacRoom: return 'V';
                case RoomType.StorageRoom: return 'S';
                case RoomType.Elevator: return 'E';
                default: return '?';
            }
        }
    }
}
