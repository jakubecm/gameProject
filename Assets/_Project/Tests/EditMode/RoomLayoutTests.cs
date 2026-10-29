using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class RoomLayoutTests
    {
        /// <summary>Every type the placeholder may place (hall tiles only come as a whole hall).</summary>
        private static readonly RoomType[] AllTypes = ((RoomType[])Enum.GetValues(typeof(RoomType))).Where(type => type != RoomType.Hall).ToArray();

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
        public void Layout_UsesEveryAvailableType_OverManyMaps()
        {
            var used = new HashSet<RoomType>();
            for (int seed = 0; seed < 30; seed++)
            {
                foreach (var room in RoomLayout.Generate(MapGenerator.Generate(seed, 10, 10), seed, AllTypes).Rooms)
                {
                    used.Add(room.Type);
                }
            }
            CollectionAssert.AreEquivalent(AllTypes, used);
        }

        [Test]
        public void Layout_PicksEveryLayoutOfARoomType_AndOnlyValidOnes()
        {
            var seen = new System.Collections.Generic.HashSet<(RoomType, int)>();
            for (int seed = 0; seed < 200; seed++)
            {
                var map = MapGenerator.Generate(seed, 8, 8);
                foreach (var room in RoomLayout.Generate(map, seed, AllTypes).Rooms)
                {
                    Assert.That(room.Layout, Is.InRange(0, RoomRules.LayoutCount(room.Type) - 1), room.Type.ToString());
                    seen.Add((room.Type, room.Layout));
                }
            }
            for (int layout = 0; layout < RoomRules.RoomLayouts; layout++)
            {
                Assert.IsTrue(seen.Contains((RoomType.Office, layout)), $"Office layout {layout + 1} is never used");
                Assert.IsTrue(seen.Contains((RoomType.StorageRoom, layout)), $"Storage room layout {layout + 1} is never used");
            }
        }

        [Test]
        public void Layout_FollowsTheDoorRules()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var map = MapGenerator.Generate(seed, 8, 8);
                foreach (var room in RoomLayout.Generate(map, seed, AllTypes).Rooms)
                {
                    int connected = RoomLayout.ConnectedSides(map, room.Column, room.Row);
                    int doors = RoomRules.Rotate(RoomRules.DoorSides(room.Type), room.QuarterTurns);
                    string where = $"seed {seed}, cell ({room.Column}, {room.Row}), {room.Type}";
                    if (RoomRules.IsCorridor(room.Type))
                    {
                        Assert.AreEqual(connected, doors, $"{where}: the arms do not match the connections");
                    }
                    else
                    {
                        Assert.AreEqual(0, connected & ~doors, $"{where}: a connection has no door");
                        Assert.LessOrEqual(RoomRules.BitCount(connected), RoomRules.MaxDoors(room.Type), $"{where}: too many doors");
                    }
                    if (RoomRules.IsSmallRoom(room.Type))
                    {
                        Assert.LessOrEqual(RoomRules.BitCount(connected), 1, $"{where}: a small room has more than one door");
                    }
                }
            }
        }

        [Test]
        public void Layout_FallsBackToTheCrossWhenNothingFits()
        {
            // Only offices and the cross: a T-junction cell can only be a cross with one arm plugged.
            var map = new MapData(3, 2);
            map.Connect(1, 0, Direction.West);
            map.Connect(1, 0, Direction.East);
            map.Connect(1, 0, Direction.North);
            var layout = RoomLayout.Generate(map, 4, new[] { RoomType.Corridor, RoomType.Office });
            Assert.AreEqual(RoomType.Corridor, layout.GetRoom(1, 0).Type);
            Assert.AreEqual(RoomType.Office, layout.GetRoom(0, 0).Type, "A dead end fits an office; the cross does not");
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
        public void Layout_HasExactlyOneLift_InTheDeadEndNearestTheMiddle()
        {
            int withLift = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var map = MapGenerator.Generate(seed, 6, 6);
                var layout = RoomLayout.Generate(map, seed, AllTypes);
                var lifts = layout.Rooms.Where(room => room.Type == RoomType.Elevator).ToList();
                // A map whose loops left no dead end has no place for the lift.
                if (RoomLayout.LiftCell(map) == null)
                {
                    Assert.IsEmpty(lifts, $"seed {seed}");
                    continue;
                }
                withLift++;
                Assert.AreEqual(1, lifts.Count, $"seed {seed}");
                var lift = lifts[0];
                Assert.IsTrue(layout.HasLift);
                Assert.AreEqual((lift.Column, lift.Row), layout.StartCell, $"seed {seed}: the player does not start in the lift");
                Assert.AreEqual(1, RoomRules.BitCount(RoomLayout.ConnectedSides(map, lift.Column, lift.Row)), $"seed {seed}: the lift is not in a dead end");
                Assert.AreEqual(RoomLayout.LiftCell(map), (lift.Column, lift.Row));
                // Its door faces the one connection.
                Assert.AreEqual(RoomLayout.ConnectedSides(map, lift.Column, lift.Row), RoomRules.Rotate(RoomRules.South, lift.QuarterTurns), $"seed {seed}");
            }
            Assert.Greater(withLift, 90, "Most maps should have a dead end for the lift");
        }

        [Test]
        public void Layout_WithoutTheLift_StartsInTheMiddle()
        {
            var layout = RoomLayout.Generate(MapGenerator.Generate(3, 7, 5), 3, AllTypes.Where(type => type != RoomType.Elevator).ToArray());
            Assert.IsFalse(layout.HasLift);
            Assert.AreEqual((3, 2), layout.StartCell);
        }

        [Test]
        public void Layout_TurnsTheFireArmOfCorridorsToARoom()
        {
            int checkedCorridors = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var map = MapGenerator.Generate(seed, 8, 8);
                var layout = RoomLayout.Generate(map, seed, AllTypes);
                foreach (var room in layout.Rooms.Where(r => RoomRules.IsCorridor(r.Type)))
                {
                    bool IsRoom(int c, int r) => !RoomRules.IsCorridor(layout.GetRoom(c, r).Type);
                    if (RoomLayout.FireArmOptions(map, room.Column, room.Row, room.Type, IsRoom).Count == 0) continue;
                    int fire = RoomRules.Rotate(RoomRules.Mask(RoomRules.FireArmSide(room.Type, room.Layout)), room.QuarterTurns);
                    var side = (Direction)System.Array.FindIndex(new[] { RoomRules.North, RoomRules.East, RoomRules.South, RoomRules.West }, m => m == fire);
                    var neighbour = layout.GetRoom(room.Column + side.ColumnOffset(), room.Row + side.RowOffset());
                    Assert.IsFalse(RoomRules.IsCorridor(neighbour.Type), $"seed {seed}, cell ({room.Column}, {room.Row}): the fire kit is not by a room");
                    checkedCorridors++;
                }
            }
            Assert.Greater(checkedCorridors, 50);
        }

        [Test]
        public void PlacedRoom_YawIsQuarterTurnsTimesNinety()
        {
            Assert.AreEqual(270f, new PlacedRoom(0, 0, RoomType.Corridor, 3).YawDegrees, 0.0001f);
        }

        private static List<string> Describe(RoomLayout layout) =>
            layout.Rooms.Select(room => $"{room.Column},{room.Row},{room.Type},{room.QuarterTurns},{room.Layout}").ToList();
    }
}
