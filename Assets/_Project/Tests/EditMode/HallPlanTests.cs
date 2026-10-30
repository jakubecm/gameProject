using System.Linq;
using ColdShutdown.Core;
using NUnit.Framework;

namespace ColdShutdown.Tests.EditMode
{
    public class HallPlanTests
    {
        [Test]
        public void Sides_FollowTheMapAndTheNeighbours()
        {
            // Hall in columns 0-1, a room in column 2 connected to (1, 0) only.
            var map = new MapData(3, 2);
            map.Connect(0, 0, Direction.East);
            map.Connect(1, 0, Direction.East);
            map.Connect(1, 1, Direction.South);
            var plan = HallPlan.Create(map, (c, r) => c < 2);

            Assert.IsTrue(plan.TryGetCell(1, 0, out var cell));
            Assert.AreEqual(HallSideKind.Open, cell.Side(Direction.West));
            Assert.AreEqual(HallSideKind.Open, cell.Side(Direction.North));
            Assert.AreEqual(HallSideKind.Door, cell.Side(Direction.East));
            Assert.AreEqual(HallSideKind.Wall, cell.Side(Direction.South));

            // A hall is never divided: neighbouring hall tiles are open to each other even where the map does not connect them.
            Assert.IsTrue(plan.TryGetCell(0, 1, out var other));
            Assert.AreEqual(HallSideKind.Open, other.Side(Direction.East));
            Assert.AreEqual(HallSideKind.Open, other.Side(Direction.South));
            Assert.IsFalse(plan.Cells.Any(c => Enumerable.Range(0, 4).Any(d => c.Side((Direction)d) == HallSideKind.Closed)));
            Assert.IsFalse(plan.TryGetCell(2, 0, out _));
            Assert.AreEqual(4, plan.Count);
        }

        [Test]
        public void Corners_DependOnSidesAndDiagonal()
        {
            // L-shaped hall: (1, 1) is not hall.
            var map = new MapData(2, 2);
            var plan = HallPlan.Create(map, (c, r) => !(c == 1 && r == 1));

            plan.TryGetCell(0, 0, out var corner);
            Assert.AreEqual(HallCornerKind.Concave, corner.Corner(HallCorner.NorthEast));
            Assert.AreEqual(HallCornerKind.Outer, corner.Corner(HallCorner.SouthWest));
            Assert.AreEqual(HallCornerKind.EdgeSecond, corner.Corner(HallCorner.SouthEast)); // east is hall, south is not
            Assert.AreEqual(HallCornerKind.EdgeFirst, corner.Corner(HallCorner.NorthWest)); // west is not hall, north is

            plan.TryGetCell(1, 0, out var east);
            Assert.AreEqual(HallCornerKind.Outer, east.Corner(HallCorner.NorthEast));
            Assert.AreEqual(HallCornerKind.EdgeSecond, east.Corner(HallCorner.NorthWest)); // west is hall, north is not
        }

        [Test]
        public void InnerCorners_NeedTheDiagonal()
        {
            var plan = HallPlan.Create(new MapData(3, 3), (c, r) => true);
            plan.TryGetCell(1, 1, out var middle);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(HallCornerKind.Inner, middle.Corner((HallCorner)i));
            }
        }

        [Test]
        public void Codes_CountFromTheSouthWestOfTheHall()
        {
            var plan = HallPlan.Create(new MapData(5, 5), (c, r) => c >= 2 && r >= 1);
            plan.TryGetCell(2, 1, out var first);
            plan.TryGetCell(4, 4, out var last);
            Assert.AreEqual("A1", first.Code);
            Assert.AreEqual("C4", last.Code);
        }

        [Test]
        public void ColumnLabels_AreSpreadsheetLetters()
        {
            Assert.AreEqual("A", HallPlan.ColumnLabel(0));
            Assert.AreEqual("Z", HallPlan.ColumnLabel(25));
            Assert.AreEqual("AA", HallPlan.ColumnLabel(26));
            Assert.AreEqual("AZ", HallPlan.ColumnLabel(51));
            Assert.AreEqual("BA", HallPlan.ColumnLabel(52));
        }

        [Test]
        public void PodSlots_StandSideBySideFacingTheAisle()
        {
            var slots = HallContract.GetPodRackSlots();
            Assert.AreEqual(HallContract.TileRackSlots, slots.Length);
            Assert.AreEqual(2 * 2 * HallContract.RacksPerRow, slots.Length, "Two pods of two rows");
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float center = HallContract.PodZ(pod);
                foreach (int side in new[] { 1, -1 })
                {
                    var row = slots.Where(s => System.Math.Abs(s.Z - center) < HallContract.PodHalfDepth && System.Math.Sign(s.Z - center) == side)
                        .OrderBy(s => s.X).ToArray();
                    Assert.AreEqual(HallContract.RacksPerRow, row.Length);
                    for (int i = 1; i < row.Length; i++)
                    {
                        // No gap between neighbouring racks.
                        Assert.AreEqual(RoomContract.RackWidth, row[i].X - row[i - 1].X, 0.001f);
                    }
                    // Fronts face the aisle: the north row looks south, the south row north.
                    Assert.IsTrue(row.All(s => s.YawDegrees == (side > 0 ? 180f : 0f)));
                    Assert.IsTrue(row.All(s => System.Math.Abs(System.Math.Abs(s.Z - center) - RoomContract.RackDepth / 2f - HallContract.AisleWidth / 2f) < 0.001f));
                    Assert.AreEqual(-HallContract.PodHalfLength, row[0].X - RoomContract.RackWidth / 2f, 0.001f);
                    Assert.AreEqual(HallContract.PodHalfLength, row[row.Length - 1].X + RoomContract.RackWidth / 2f, 0.001f);
                }
            }
        }

        [Test]
        public void Pods_LeaveWalkwaysAndAHotAisle()
        {
            // Long aisles: the rows fill most of the tile, but every wall keeps a walkway.
            Assert.GreaterOrEqual(2f * HallContract.PodHalfLength, 8f);
            Assert.GreaterOrEqual(HallContract.EndWalkway, 1.5f);
            Assert.GreaterOrEqual(HallContract.SideWalkway, 1.2f);
            Assert.GreaterOrEqual(HallContract.HotAisleWidth, 1.2f);
            Assert.AreEqual(HallContract.WallInnerOffset, HallContract.PodOuterZ + HallContract.SideWalkway, 0.001f);
            Assert.GreaterOrEqual(HallContract.AisleWidth, 1.2f, "A cold aisle needs at least two floor tiles");
            Assert.GreaterOrEqual(HallContract.ContainmentHeight, RoomContract.RackHeight);
            Assert.Less(HallContract.ContainmentHeight, RoomContract.OpeningHeight);
        }

        [Test]
        public void Layout_NeverPlacesHallsAtRandom()
        {
            var types = (RoomType[])System.Enum.GetValues(typeof(RoomType));
            var layout = RoomLayout.Generate(MapGenerator.Generate(3, 8, 8), 3, types);
            Assert.IsFalse(layout.Rooms.Any(room => room.Type == RoomType.Hall));
            Assert.AreEqual(0, HallPlan.Create(layout).Count);
        }
    }
}
