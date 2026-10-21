using System;
using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>
    /// Generates the room layout. A depth-first random spanning tree makes every room reachable,
    /// then a share of the closed walls is opened as extra connections so the map has loops.
    /// Uses only System.Random(seed), so the same seed, width and height always give the same map.
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>Extra connections are added on top of the spanning tree: this share of the tree's connections, at least.</summary>
        public const double MinLoopFraction = 0.10;

        /// <summary>...and this share at most. The actual share is picked randomly in between.</summary>
        public const double MaxLoopFraction = 0.20;

        public static MapData Generate(int seed, int width, int height)
        {
            var map = new MapData(width, height);
            var random = new Random(seed);

            BuildSpanningTree(map, random);
            AddLoops(map, random);
            return map;
        }

        private static void BuildSpanningTree(MapData map, Random random)
        {
            var visited = new bool[map.Width * map.Height];
            var path = new Stack<(int column, int row)>();
            var unvisitedNeighbours = new List<Direction>(DirectionExtensions.Count);

            var start = (column: random.Next(map.Width), row: random.Next(map.Height));
            visited[start.row * map.Width + start.column] = true;
            path.Push(start);

            while (path.Count > 0)
            {
                var (column, row) = path.Peek();

                unvisitedNeighbours.Clear();
                for (int d = 0; d < DirectionExtensions.Count; d++)
                {
                    var direction = (Direction)d;
                    int nextColumn = column + direction.ColumnOffset();
                    int nextRow = row + direction.RowOffset();
                    if (map.InBounds(nextColumn, nextRow) && !visited[nextRow * map.Width + nextColumn])
                    {
                        unvisitedNeighbours.Add(direction);
                    }
                }

                if (unvisitedNeighbours.Count == 0)
                {
                    path.Pop();
                    continue;
                }

                var chosen = unvisitedNeighbours[random.Next(unvisitedNeighbours.Count)];
                map.Connect(column, row, chosen);

                int chosenColumn = column + chosen.ColumnOffset();
                int chosenRow = row + chosen.RowOffset();
                visited[chosenRow * map.Width + chosenColumn] = true;
                path.Push((chosenColumn, chosenRow));
            }
        }

        private static void AddLoops(MapData map, Random random)
        {
            // Every closed wall between two rooms, listed once (looking only east and north).
            var closedWalls = new List<(int column, int row, Direction direction)>();
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (column + 1 < map.Width && !map.IsConnected(column, row, Direction.East))
                    {
                        closedWalls.Add((column, row, Direction.East));
                    }
                    if (row + 1 < map.Height && !map.IsConnected(column, row, Direction.North))
                    {
                        closedWalls.Add((column, row, Direction.North));
                    }
                }
            }

            double fraction = MinLoopFraction + random.NextDouble() * (MaxLoopFraction - MinLoopFraction);
            int extra = (int)Math.Round(map.ConnectionCount * fraction);
            extra = Math.Min(extra, closedWalls.Count);

            // Partial Fisher-Yates shuffle: only the first `extra` positions are needed.
            for (int i = 0; i < extra; i++)
            {
                int pick = i + random.Next(closedWalls.Count - i);
                (closedWalls[i], closedWalls[pick]) = (closedWalls[pick], closedWalls[i]);

                var wall = closedWalls[i];
                map.Connect(wall.column, wall.row, wall.direction);
            }
        }
    }
}
