using System;
using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>
    /// Plain data for a generated map: a Width x Height grid where every cell is a room, and each
    /// side of each room is either connected to the neighbouring room or closed.
    /// Cells are addressed by (column, row); column 0 / row 0 is the south-west corner.
    /// Connections are always symmetric: if A is connected to B, then B is connected to A.
    /// </summary>
    public sealed class MapData
    {
        private readonly byte[] connections;

        public int Width { get; }
        public int Height { get; }

        public MapData(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1.");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1.");

            Width = width;
            Height = height;
            connections = new byte[width * height];
        }

        public bool InBounds(int column, int row) =>
            column >= 0 && column < Width && row >= 0 && row < Height;

        /// <summary>True if the room at (column, row) has an open passage on the given side.</summary>
        public bool IsConnected(int column, int row, Direction direction)
        {
            if (!InBounds(column, row)) throw new ArgumentOutOfRangeException($"Cell ({column}, {row}) is outside the map.");
            return (connections[Index(column, row)] & (1 << (int)direction)) != 0;
        }

        /// <summary>
        /// Opens a passage between a cell and its neighbour in the given direction (both sides).
        /// The neighbour must be inside the map.
        /// </summary>
        public void Connect(int column, int row, Direction direction)
        {
            int neighbourColumn = column + direction.ColumnOffset();
            int neighbourRow = row + direction.RowOffset();
            if (!InBounds(column, row)) throw new ArgumentOutOfRangeException($"Cell ({column}, {row}) is outside the map.");
            if (!InBounds(neighbourColumn, neighbourRow)) throw new ArgumentException($"Cell ({column}, {row}) has no neighbour to the {direction}.");

            connections[Index(column, row)] |= (byte)(1 << (int)direction);
            connections[Index(neighbourColumn, neighbourRow)] |= (byte)(1 << (int)direction.Opposite());
        }

        /// <summary>Number of passages between rooms (each shared wall counts once).</summary>
        public int ConnectionCount
        {
            get
            {
                int count = 0;
                for (int row = 0; row < Height; row++)
                {
                    for (int column = 0; column < Width; column++)
                    {
                        if (IsConnected(column, row, Direction.East)) count++;
                        if (IsConnected(column, row, Direction.North)) count++;
                    }
                }
                return count;
            }
        }

        /// <summary>Number of cells that can be reached from the start cell by walking through open passages.</summary>
        public int CountReachableCells(int startColumn, int startRow)
        {
            if (!InBounds(startColumn, startRow)) throw new ArgumentOutOfRangeException($"Cell ({startColumn}, {startRow}) is outside the map.");

            var visited = new bool[Width * Height];
            var pending = new Stack<int>();
            visited[Index(startColumn, startRow)] = true;
            pending.Push(Index(startColumn, startRow));
            int reached = 0;

            while (pending.Count > 0)
            {
                int current = pending.Pop();
                reached++;
                int column = current % Width;
                int row = current / Width;

                for (int d = 0; d < DirectionExtensions.Count; d++)
                {
                    var direction = (Direction)d;
                    if (!IsConnected(column, row, direction)) continue;

                    int next = Index(column + direction.ColumnOffset(), row + direction.RowOffset());
                    if (visited[next]) continue;
                    visited[next] = true;
                    pending.Push(next);
                }
            }

            return reached;
        }

        private int Index(int column, int row) => row * Width + column;
    }
}
