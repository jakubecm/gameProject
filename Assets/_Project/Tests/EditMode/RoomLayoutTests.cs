using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class RoomLayoutTests
    {
        private static readonly RoomType[] AllTypes = { RoomType.Corridor, RoomType.ServerAisle, RoomType.Office };

        [Test]
        public void ToText_DrawsWallsPassagesAndLabels()
        {
            var map = new MapData(2, 2);
            map.Connect(0, 1, Direction.East);
            map.Connect(0, 1, Direction.South);

            string text = map.ToText((column, row) => "COSO"[row * 2 + column]);

            string expected =
                "+---+---+\n" +
                "| S   O |\n" +
                "+   +---+\n" +
                "| C | O |\n" +
                "+---+---+";
            Assert.AreEqual(expected, text);
        }

        [Test]
        public void ToText_WithoutLabels_LeavesRoomsEmpty()
        {
            var map = new MapData(1, 1);
            Assert.AreEqual("+---+\n|   |\n+---+", map.ToText());
        }

        [Test]
        public void ToText_HasOneLinePairPerRow_ForGeneratedMaps()
        {
            var map = MapGenerator.Generate(5, 7, 4);
            var lines = map.ToText().Split('\n');
            Assert.AreEqual(2 * 4 + 1, lines.Length);
            Assert.IsTrue(lines.All(line => line.Length == 4 * 7 + 1));
        }

        [Test]
        public void Layout_CoversEveryCell_WithAvailableTypesAndQuarterTurns()
        {
            var layout = RoomLayout.Generate(MapGenerator.Generate(3, 6, 5), 3, AllTypes);

            Assert.AreEqual(30, layout.Rooms.Count);
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    var room = layout.GetRoom(column, row);
                    Assert.AreEqual(column, room.Column);
                    Assert.AreEqual(row, room.Row);
                    Assert.Contains(room.Type, AllTypes);
                    Assert.That(room.QuarterTurns, Is.InRange(0, 3));
                }
            }
        }

        [Test]
        public void Layout_SameSeed_GivesTheSameRooms()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var first = RoomLayout.Generate(MapGenerator.Generate(seed, 6, 6), seed, AllTypes);
                var second = RoomLayout.Generate(MapGenerator.Generate(seed, 6, 6), seed, AllTypes);
                CollectionAssert.AreEqual(Describe(first), Describe(second), $"seed {seed}");
            }
        }

        [Test]
        public void Layout_UsesEveryAvailableType_OnALargeMap()
        {
            var layout = RoomLayout.Generate(MapGenerator.Generate(1, 10, 10), 1, AllTypes);
            CollectionAssert.AreEquivalent(AllTypes, layout.Rooms.Select(room => room.Type).Distinct());
        }

        [Test]
        public void Layout_FollowsThePlaceholderWeights()
        {
            var counts = new Dictionary<RoomType, int>();
            int cells = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                foreach (var room in RoomLayout.Generate(MapGenerator.Generate(seed, 10, 10), seed, AllTypes).Rooms)
                {
                    counts[room.Type] = counts.TryGetValue(room.Type, out int count) ? count + 1 : 1;
                    cells++;
                }
            }

            Assert.AreEqual(0.50, counts[RoomType.Corridor] / (double)cells, 0.03);
            Assert.AreEqual(0.35, counts[RoomType.ServerAisle] / (double)cells, 0.03);
            Assert.AreEqual(0.15, counts[RoomType.Office] / (double)cells, 0.03);
        }

        [Test]
        public void Layout_OnlyUsesTheTypesItIsGiven()
        {
            var layout = RoomLayout.Generate(MapGenerator.Generate(1, 6, 6), 1, new[] { RoomType.Office });
            Assert.IsTrue(layout.Rooms.All(room => room.Type == RoomType.Office));
        }

        [Test]
        public void Layout_RequiresAtLeastOneType()
        {
            var map = MapGenerator.Generate(1, 3, 3);
            Assert.Throws<ArgumentException>(() => RoomLayout.Generate(map, 1, new RoomType[0]));
            Assert.Throws<ArgumentNullException>(() => RoomLayout.Generate(null, 1, AllTypes));
        }

        [Test]
        public void PlacedRoom_YawIsQuarterTurnsTimesNinety()
        {
            Assert.AreEqual(270f, new PlacedRoom(0, 0, RoomType.Corridor, 3).YawDegrees, 0.0001f);
        }

        private static List<string> Describe(RoomLayout layout) =>
            layout.Rooms.Select(room => $"{room.Column},{room.Row},{room.Type},{room.QuarterTurns}").ToList();
    }
}
