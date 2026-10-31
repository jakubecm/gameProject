using System.Linq;
using ColdShutdown.Core;
using NUnit.Framework;

namespace ColdShutdown.Tests.EditMode
{
    /// <summary>The text format of the one world map (<see cref="WorldMapText"/>) and the map text drawing.</summary>
    public class WorldMapTextTests
    {
        /// <summary>A sample map (an earlier preview): a 2 x 3 hall with two D doors, every corridor shape, one room of each type.</summary>
        private const string SampleMap =
            "# comment lines and spaces are ignored\n" +
            ". G . . . . .\n" +
            ". C H H . . .\n" +
            ". D H H . . .\n" +
            ". C H H . V .\n" +
            "C C C D C C O\n" +
            ". E . C P S .\n";

        [Test]
        public void Parse_TheSampleMap_GivesEveryCellItsTypeAndNorthIsAtTheTop()
        {
            var result = WorldMapText.Parse(SampleMap);
            Assert.IsTrue(result.IsValid, string.Join("; ", result.Errors));
            var layout = result.Layout;
            Assert.AreEqual(7, layout.Map.Width);
            Assert.AreEqual(6, layout.Map.Height);
            Assert.AreEqual(RoomType.SecurityOffice, layout.GetRoom(1, 5).Type);
            Assert.AreEqual(RoomType.LiftEnd, layout.GetRoom(1, 0).Type);
            Assert.AreEqual(RoomType.Office, layout.GetRoom(6, 1).Type);
            Assert.IsFalse(layout.HasRoom(0, 0), "'.' is an empty cell");
            Assert.AreEqual((1, 0), layout.StartCell);
            Assert.AreEqual(6, layout.Rooms.Count(room => room.Type == RoomType.Hall));
        }

        [Test]
        public void Parse_CorridorShapesFollowTheirNeighbours()
        {
            var layout = WorldMapText.Parse(SampleMap).Layout;
            Assert.AreEqual(RoomType.CorridorDeadEnd, layout.GetRoom(0, 1).Type);
            Assert.AreEqual(RoomType.CorridorCross, layout.GetRoom(1, 1).Type, "Corridors west, east and north, the lift south");
            Assert.AreEqual(RoomType.CorridorT, layout.GetRoom(4, 1).Type, "The power room's door from the south");
            Assert.AreEqual(RoomType.CorridorStraight, layout.GetRoom(2, 1).Type, "A C next to the hall does not open into it");
            Assert.AreEqual(RoomType.CorridorCross, layout.GetRoom(3, 1).Type, "A D opens into the hall (and the dead end south of it)");
            Assert.AreEqual(RoomType.CorridorDeadEnd, layout.GetRoom(3, 0).Type, "The power room's door faces north, not this corridor");
            foreach (var room in layout.Rooms)
            {
                int connected = RoomLayout.ConnectedSides(layout.Map, room.Column, room.Row);
                if (room.Type == RoomType.Hall) continue;
                Assert.AreEqual(connected, RoomRules.Rotate(RoomRules.DoorSides(room.Type), room.QuarterTurns) & connected, $"{room.Type} at ({room.Column}, {room.Row}) is not turned to its connections");
                Assert.IsTrue(RoomRules.FittingTurns(room.Type, connected).Contains(room.QuarterTurns), $"{room.Type} at ({room.Column}, {room.Row})");
            }
        }

        [Test]
        public void Parse_RoomsHaveOneDoor_NorthFirst()
        {
            var layout = WorldMapText.Parse(SampleMap).Layout;
            Assert.AreEqual(RoomRules.North, RoomLayout.ConnectedSides(layout.Map, 4, 0), "The power room touches corridors north and west");
            Assert.AreEqual(RoomRules.West, RoomLayout.ConnectedSides(layout.Map, 6, 1));
            var result = WorldMapText.Parse(SampleMap);
            Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("PowerRoom")), "Several corridors next to a room are reported");
        }

        [Test]
        public void Parse_TheHallConnectsOnlyThroughDCorridors()
        {
            var map = WorldMapText.Parse(SampleMap).Layout.Map;
            Assert.IsTrue(map.IsConnected(3, 1, Direction.North));
            Assert.IsTrue(map.IsConnected(1, 3, Direction.East));
            Assert.IsFalse(map.IsConnected(2, 1, Direction.North));
            Assert.IsFalse(map.IsConnected(1, 2, Direction.East));
            Assert.IsTrue(map.IsConnected(2, 2, Direction.East), "Hall cells are one room");
        }

        [Test]
        public void Parse_CorridorsTurnTheirFireArmTowardsARoom()
        {
            var layout = WorldMapText.Parse(SampleMap).Layout;
            foreach (var room in layout.Rooms.Where(room => RoomRules.IsCorridor(room.Type) && room.Type != RoomType.LiftEnd))
            {
                var options = RoomLayout.FireArmOptions(layout.Map, room.Column, room.Row, room.Type, (c, r) => layout.HasRoom(c, r) && !RoomRules.IsCorridor(layout.GetRoom(c, r).Type));
                if (options.Count == 0) continue;
                Assert.Contains((room.QuarterTurns, room.Layout), options, $"Corridor at ({room.Column}, {room.Row})");
            }
        }

        [TestCase("C C C\n", "lift end")]
        [TestCase("E C E\n", "lift end")]
        [TestCase("E C X\n", "Unknown character")]
        [TestCase("E C .\n. . O\n", "no corridor")]
        [TestCase("E D H\n. . .\n. H H\n", "rectangle")]
        [TestCase("E C . C C\n", "reached")]
        public void Parse_ReportsMistakes(string text, string expected)
        {
            var result = WorldMapText.Parse(text);
            Assert.IsFalse(result.IsValid);
            Assert.IsNull(result.Layout);
            Assert.IsTrue(result.Errors.Any(error => error.Contains(expected)), $"Expected '{expected}' in: {string.Join("; ", result.Errors)}");
        }

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
    }
}
