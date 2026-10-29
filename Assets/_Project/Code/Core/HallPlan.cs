using System;
using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>What stands on one side of a hall tile.</summary>
    public enum HallSideKind
    {
        /// <summary>Another hall tile, connected: open floor, the lanes continue into the neighbour.</summary>
        Open = 0,

        /// <summary>
        /// Unused: a server hall is never divided, so <see cref="HallPlan"/> never plans it (neighbouring hall tiles are
        /// always open to each other). Kept so serialized kits stay valid; its piece is open floor like <see cref="Open"/>.
        /// </summary>
        Closed = 1,

        /// <summary>Hall perimeter with a door to the connected room behind it.</summary>
        Door = 2,

        /// <summary>Hall perimeter without a door (a room that is not connected, or nothing).</summary>
        Wall = 3,
    }

    /// <summary>
    /// What fills the corner of a hall tile. A corner lies between two sides: the first one clockwise from
    /// north and the next one (the north-east corner lies between North and East).
    /// </summary>
    public enum HallCornerKind
    {
        /// <summary>Both sides and the diagonal are hall: a free-standing building column.</summary>
        Inner = 0,

        /// <summary>Both sides are hall but the diagonal is not: an inside corner of the perimeter walkway.</summary>
        Concave = 1,

        /// <summary>The first side is perimeter, the second is hall.</summary>
        EdgeFirst = 2,

        /// <summary>The first side is hall, the second is perimeter.</summary>
        EdgeSecond = 3,

        /// <summary>Both sides are perimeter: an outside corner of the hall.</summary>
        Outer = 4,
    }

    /// <summary>Corners of a cell. Corner i lies between side i and side i + 1 (as <see cref="Direction"/> values).</summary>
    public enum HallCorner
    {
        NorthEast = 0,
        SouthEast = 1,
        SouthWest = 2,
        NorthWest = 3,
    }

    /// <summary>One hall tile: its sides, its corners and its grid code (column letter + row number).</summary>
    public sealed class HallCell
    {
        private readonly HallSideKind[] sides;
        private readonly HallCornerKind[] corners;

        internal HallCell(int column, int row, HallSideKind[] sides, HallCornerKind[] corners, string columnLabel, string rowLabel)
        {
            Column = column;
            Row = row;
            this.sides = sides;
            this.corners = corners;
            ColumnLabel = columnLabel;
            RowLabel = rowLabel;
        }

        public int Column { get; }

        public int Row { get; }

        /// <summary>Letter of the hall column, counted from the west edge of the hall: A, B, ... Z, AA, AB, ...</summary>
        public string ColumnLabel { get; }

        /// <summary>Number of the hall row, counted from the south edge of the hall, starting at 1.</summary>
        public string RowLabel { get; }

        /// <summary>The code shown on the tile's sign, for example "B3".</summary>
        public string Code => ColumnLabel + RowLabel;

        public HallSideKind Side(Direction side) => sides[(int)side];

        public HallCornerKind Corner(HallCorner corner) => corners[(int)corner];

        public bool IsPerimeter(Direction side) => sides[(int)side] == HallSideKind.Door || sides[(int)side] == HallSideKind.Wall;
    }

    /// <summary>
    /// Plans the hall tiles of a map: for every hall cell, what stands on each side and in each corner,
    /// and the grid code of the tile. Plain data, deterministic, no randomness.
    ///
    /// A hall is one undivided space: between two hall tiles the side is always open (<see cref="HallSideKind.Open"/>),
    /// whether or not the map connects them, so there are no partitions in a hall. A side facing a room gets a door if
    /// the map connects them, otherwise a plain perimeter wall, as does the edge of the map. The racks stand only in
    /// the pods in the middle of every tile, so the walkway along the whole perimeter is always free.
    /// </summary>
    public sealed class HallPlan
    {
        private readonly Dictionary<int, HallCell> cells;
        private readonly int width;

        private HallPlan(Dictionary<int, HallCell> cells, int width)
        {
            this.cells = cells;
            this.width = width;
        }

        public IEnumerable<HallCell> Cells => cells.Values;

        public int Count => cells.Count;

        public bool TryGetCell(int column, int row, out HallCell cell) => cells.TryGetValue(row * width + column, out cell);

        /// <summary>Plans every cell of the layout whose room is a <see cref="RoomType.Hall"/>.</summary>
        public static HallPlan Create(RoomLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return Create(layout.Map, (column, row) => layout.GetRoom(column, row).Type == RoomType.Hall);
        }

        /// <summary>Plans every cell for which <paramref name="isHall"/> is true.</summary>
        public static HallPlan Create(MapData map, Func<int, int, bool> isHall)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (isHall == null) throw new ArgumentNullException(nameof(isHall));

            bool Hall(int column, int row) => map.InBounds(column, row) && isHall(column, row);

            int minColumn = int.MaxValue, minRow = int.MaxValue;
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (!Hall(column, row)) continue;
                    minColumn = Math.Min(minColumn, column);
                    minRow = Math.Min(minRow, row);
                }
            }

            var cells = new Dictionary<int, HallCell>();
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (!Hall(column, row)) continue;

                    var sides = new HallSideKind[DirectionExtensions.Count];
                    for (int d = 0; d < DirectionExtensions.Count; d++)
                    {
                        var side = (Direction)d;
                        bool neighbourIsHall = Hall(column + side.ColumnOffset(), row + side.RowOffset());
                        bool connected = map.IsConnected(column, row, side);
                        sides[d] = neighbourIsHall ? HallSideKind.Open
                            : connected ? HallSideKind.Door : HallSideKind.Wall;
                    }

                    var corners = new HallCornerKind[DirectionExtensions.Count];
                    for (int c = 0; c < DirectionExtensions.Count; c++)
                    {
                        var first = (Direction)c;
                        var second = (Direction)((c + 1) % DirectionExtensions.Count);
                        bool firstHall = !IsPerimeter(sides[(int)first]);
                        bool secondHall = !IsPerimeter(sides[(int)second]);
                        bool diagonalHall = Hall(column + first.ColumnOffset() + second.ColumnOffset(), row + first.RowOffset() + second.RowOffset());
                        corners[c] = firstHall && secondHall ? (diagonalHall ? HallCornerKind.Inner : HallCornerKind.Concave)
                            : firstHall ? HallCornerKind.EdgeSecond
                            : secondHall ? HallCornerKind.EdgeFirst
                            : HallCornerKind.Outer;
                    }

                    string columnLabel = ColumnLabel(column - minColumn);
                    string rowLabel = (row - minRow + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    cells.Add(row * map.Width + column, new HallCell(column, row, sides, corners, columnLabel, rowLabel));
                }
            }
            return new HallPlan(cells, map.Width);
        }

        /// <summary>Spreadsheet-style column letters: 0 is A, 25 is Z, 26 is AA.</summary>
        public static string ColumnLabel(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            string label = string.Empty;
            for (int n = index + 1; n > 0; n = (n - 1) / 26)
            {
                label = (char)('A' + (n - 1) % 26) + label;
            }
            return label;
        }

        private static bool IsPerimeter(HallSideKind kind) => kind == HallSideKind.Door || kind == HallSideKind.Wall;
    }
}
