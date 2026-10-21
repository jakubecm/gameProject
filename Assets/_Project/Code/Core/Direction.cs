using System;

namespace ColdShutdown.Core
{
    /// <summary>
    /// Side of a cell. North is +row (+Z in the world), East is +column (+X).
    /// Values are explicit because they double as bit indices in <see cref="MapData"/>.
    /// </summary>
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public static class DirectionExtensions
    {
        public const int Count = 4;

        public static Direction Opposite(this Direction direction) => (Direction)(((int)direction + 2) % Count);

        /// <summary>Change in column when stepping one cell in this direction.</summary>
        public static int ColumnOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.East: return 1;
                case Direction.West: return -1;
                default: return 0;
            }
        }

        /// <summary>Change in row when stepping one cell in this direction.</summary>
        public static int RowOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return 1;
                case Direction.South: return -1;
                default: return 0;
            }
        }
    }
}
