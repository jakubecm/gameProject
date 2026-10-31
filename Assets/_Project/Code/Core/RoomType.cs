namespace ColdShutdown.Core
{
    /// <summary>
    /// Kind of room a cell holds. Values are explicit so serialized assets stay valid when types are added; only ever append
    /// new values. Value 1 was the old server aisle room and value 2 the full-size office; never reuse them.
    /// Every room is one cell (12 x 12 m); the hall is one room over a rectangle of cells. Contents of every type: ROOMS.md.
    /// </summary>
    public enum RoomType
    {
        /// <summary>Corridor crossing: arms to all four sides.</summary>
        CorridorCross = 0,

        /// <summary>
        /// The server hall: one room over a rectangle of cells (one 12 x 12 m tile per cell, no walls between them), with perimeter
        /// walls and doors where the map connects it (<see cref="HallPlan"/>).
        /// </summary>
        Hall = 3,

        /// <summary>Security office (small room) with the video wall that shows the camera feeds.</summary>
        SecurityOffice = 4,

        /// <summary>Office: a 6 x 6 m room by the door, one door.</summary>
        Office = 5,

        /// <summary>Electrical switch room (small room) with two transformers, switchgear and the load meters per hall section.</summary>
        PowerRoom = 6,

        /// <summary>Cooling plant (small room) with two plate heat exchangers, pumps and the coolant thermometers.</summary>
        CoolingPlant = 7,

        /// <summary>Straight corridor (north-south).</summary>
        CorridorStraight = 8,

        /// <summary>T-junction (east, south and west arms).</summary>
        CorridorT = 9,

        /// <summary>Corner (east and south arms).</summary>
        CorridorCorner = 10,

        /// <summary>Dead end (south arm only).</summary>
        CorridorDeadEnd = 11,

        /// <summary>Storage room (small room): shelving with boxes and the goods lift.</summary>
        StorageRoom = 12,

        /// <summary>
        /// Lift end: a corridor tile like the dead end (south arm only) whose closed end is the landing of the passenger lift, which
        /// is also the evacuation lift. The player starts in the lift car.
        /// </summary>
        LiftEnd = 13,
    }

    public static class RoomTypeExtensions
    {
        /// <summary>
        /// The character of a room type in the world map text (<see cref="WorldMapText"/>) and the debug menu: every corridor shape is
        /// <c>C</c> (the shape follows from its neighbours), <c>E</c> lift end, <c>O</c> office, <c>G</c> security office, <c>P</c> power
        /// room, <c>V</c> cooling plant, <c>S</c> storage room, <c>H</c> hall.
        /// </summary>
        public static char Symbol(this RoomType type)
        {
            switch (type)
            {
                case RoomType.CorridorCross:
                case RoomType.CorridorStraight:
                case RoomType.CorridorT:
                case RoomType.CorridorCorner:
                case RoomType.CorridorDeadEnd:
                    return 'C';
                case RoomType.Office: return 'O';
                case RoomType.Hall: return 'H';
                case RoomType.SecurityOffice: return 'G';
                case RoomType.PowerRoom: return 'P';
                case RoomType.CoolingPlant: return 'V';
                case RoomType.StorageRoom: return 'S';
                case RoomType.LiftEnd: return 'E';
                default: return '?';
            }
        }
    }
}
