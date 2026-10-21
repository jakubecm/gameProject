using System;
using System.Text;
using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class MapGeneratorTests
    {
        private static readonly (int width, int height)[] Sizes =
        {
            (1, 1), (1, 5), (5, 1), (2, 2), (4, 7), (8, 8), (12, 9),
        };

        [Test]
        public void EveryCell_IsReachableFromEveryStart_ForManySizesAndSeeds()
        {
            foreach (var (width, height) in Sizes)
            {
                for (int seed = 0; seed < 25; seed++)
                {
                    var map = MapGenerator.Generate(seed, width, height);
                    // Connections are symmetric, so reaching everything from one cell is enough.
                    Assert.AreEqual(width * height, map.CountReachableCells(0, 0), $"{width}x{height}, seed {seed}");
                    Assert.AreEqual(width * height, map.CountReachableCells(width - 1, height - 1), $"{width}x{height}, seed {seed}");
                }
            }
        }

        [Test]
        public void SameSeed_GivesTheSameMap()
        {
            foreach (var (width, height) in Sizes)
            {
                for (int seed = 0; seed < 25; seed++)
                {
                    string first = Signature(MapGenerator.Generate(seed, width, height));
                    string second = Signature(MapGenerator.Generate(seed, width, height));
                    Assert.AreEqual(first, second, $"{width}x{height}, seed {seed}");
                }
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentMaps()
        {
            Assert.AreNotEqual(Signature(MapGenerator.Generate(1, 8, 8)), Signature(MapGenerator.Generate(2, 8, 8)));

            var distinct = new System.Collections.Generic.HashSet<string>();
            for (int seed = 0; seed < 100; seed++)
            {
                distinct.Add(Signature(MapGenerator.Generate(seed, 8, 8)));
            }
            Assert.AreEqual(100, distinct.Count);
        }

        [Test]
        public void Connections_AreSymmetric_AndNeverLeaveTheGrid()
        {
            foreach (var (width, height) in Sizes)
            {
                for (int seed = 0; seed < 25; seed++)
                {
                    var map = MapGenerator.Generate(seed, width, height);
                    for (int row = 0; row < height; row++)
                    {
                        for (int column = 0; column < width; column++)
                        {
                            for (int d = 0; d < DirectionExtensions.Count; d++)
                            {
                                var direction = (Direction)d;
                                if (!map.IsConnected(column, row, direction)) continue;

                                int nextColumn = column + direction.ColumnOffset();
                                int nextRow = row + direction.RowOffset();
                                Assert.IsTrue(map.InBounds(nextColumn, nextRow), $"Opening leaves the grid at ({column}, {row}) {direction}");
                                Assert.IsTrue(map.IsConnected(nextColumn, nextRow, direction.Opposite()), $"One-sided opening at ({column}, {row}) {direction}");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void ExtraConnections_AreTenToTwentyPercentOfTheSpanningTree()
        {
            const int width = 8;
            const int height = 8;
            int treeConnections = width * height - 1;
            int minimum = treeConnections + (int)Math.Round(treeConnections * MapGenerator.MinLoopFraction);
            int maximum = treeConnections + (int)Math.Round(treeConnections * MapGenerator.MaxLoopFraction);

            for (int seed = 0; seed < 200; seed++)
            {
                int count = MapGenerator.Generate(seed, width, height).ConnectionCount;
                Assert.GreaterOrEqual(count, minimum, $"seed {seed}");
                Assert.LessOrEqual(count, maximum, $"seed {seed}");
            }
        }

        [Test]
        public void Map_ContainsLoops_NotOnlyATree()
        {
            var map = MapGenerator.Generate(7, 8, 8);
            Assert.Greater(map.ConnectionCount, map.Width * map.Height - 1);
        }

        [Test]
        public void SmallMaps_NeverGetMoreConnectionsThanWalls()
        {
            // A 1xN map is a corridor with no spare walls to open.
            Assert.AreEqual(4, MapGenerator.Generate(3, 1, 5).ConnectionCount);
            Assert.AreEqual(0, MapGenerator.Generate(3, 1, 1).ConnectionCount);
            Assert.LessOrEqual(MapGenerator.Generate(3, 2, 2).ConnectionCount, 4);
        }

        [Test]
        public void ThousandSeeds_RunWithoutErrors_AndAreFullyReachable()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                var map = MapGenerator.Generate(seed, 10, 10);
                Assert.AreEqual(100, map.CountReachableCells(0, 0), $"seed {seed}");
            }
        }

        [Test]
        public void InvalidSizes_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(1, 0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(1, 5, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MapGenerator.Generate(1, -3, 5));
        }

        [Test]
        public void MapData_RejectsConnectionsOutsideTheGrid()
        {
            var map = new MapData(2, 2);
            Assert.Throws<ArgumentException>(() => map.Connect(0, 0, Direction.West));
            Assert.Throws<ArgumentException>(() => map.Connect(1, 1, Direction.North));
        }

        [Test]
        public void MapData_WithoutConnections_OnlyReachesTheStartCell()
        {
            var map = new MapData(3, 3);
            Assert.AreEqual(1, map.CountReachableCells(1, 1));
        }

        [Test]
        public void Direction_OppositeAndOffsets_AreConsistent()
        {
            Assert.AreEqual(Direction.South, Direction.North.Opposite());
            Assert.AreEqual(Direction.West, Direction.East.Opposite());
            Assert.AreEqual(1, Direction.North.RowOffset());
            Assert.AreEqual(1, Direction.East.ColumnOffset());
            for (int d = 0; d < DirectionExtensions.Count; d++)
            {
                var direction = (Direction)d;
                Assert.AreEqual(-direction.ColumnOffset(), direction.Opposite().ColumnOffset());
                Assert.AreEqual(-direction.RowOffset(), direction.Opposite().RowOffset());
            }
        }

        // One character per cell: its open sides as a hex digit. Easy to compare and readable in failures.
        private static string Signature(MapData map)
        {
            var text = new StringBuilder();
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    int mask = 0;
                    for (int d = 0; d < DirectionExtensions.Count; d++)
                    {
                        if (map.IsConnected(column, row, (Direction)d)) mask |= 1 << d;
                    }
                    text.Append(mask.ToString("x"));
                }
                text.Append('/');
            }
            return text.ToString();
        }
    }
}
