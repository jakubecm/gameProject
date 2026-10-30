using ColdShutdown.Core;
using NUnit.Framework;

namespace ColdShutdown.Tests.EditMode
{
    public class RoomRulesTests
    {
        [Test]
        public void Rotate_TurnsSidesClockwise()
        {
            Assert.AreEqual(RoomRules.East, RoomRules.Rotate(RoomRules.North, 1));
            Assert.AreEqual(RoomRules.West, RoomRules.Rotate(RoomRules.South, 1));
            Assert.AreEqual(RoomRules.North, RoomRules.Rotate(RoomRules.West, 1));
            Assert.AreEqual(RoomRules.North | RoomRules.East, RoomRules.Rotate(RoomRules.East | RoomRules.South, 3));
            Assert.AreEqual(RoomRules.South, RoomRules.Rotate(RoomRules.South, 4));
            Assert.AreEqual(RoomRules.South, RoomRules.Rotate(RoomRules.South, -4));
        }

        [Test]
        public void Rotate_MatchesTheRoomYaw()
        {
            // A room turned by q quarter turns has yaw q * 90; its local south opening then faces this world side.
            for (int q = 0; q < 4; q++)
            {
                double yaw = q * 90 * System.Math.PI / 180.0;
                // Local south is -Z; Unity's yaw turns (0, -1) into (-sin, -cos).
                int worldColumn = (int)System.Math.Round(-System.Math.Sin(yaw));
                int worldRow = (int)System.Math.Round(-System.Math.Cos(yaw));
                int mask = RoomRules.Rotate(RoomRules.South, q);
                Direction side = Direction.North;
                for (int d = 0; d < 4; d++)
                {
                    if (mask == RoomRules.Mask((Direction)d)) side = (Direction)d;
                }
                Assert.AreEqual(worldColumn, side.ColumnOffset(), $"q = {q}");
                Assert.AreEqual(worldRow, side.RowOffset(), $"q = {q}");
            }
        }

        [Test]
        public void Corridors_FitOnlyTheirExactShape()
        {
            // A cross fits every way round; the turn picks its fire arm.
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, RoomRules.FittingTurns(RoomType.Corridor, RoomRules.AllSides).ToArray());
            Assert.IsEmpty(RoomRules.FittingTurns(RoomType.Corridor, RoomRules.North | RoomRules.South));
            Assert.AreEqual(new[] { 0, 2 }, RoomRules.FittingTurns(RoomType.CorridorStraight, RoomRules.North | RoomRules.South).ToArray());
            Assert.AreEqual(new[] { 2 }, RoomRules.FittingTurns(RoomType.CorridorT, RoomRules.North | RoomRules.East | RoomRules.West).ToArray());
            Assert.AreEqual(new[] { 3 }, RoomRules.FittingTurns(RoomType.CorridorCorner, RoomRules.North | RoomRules.East).ToArray());
            Assert.AreEqual(new[] { 1 }, RoomRules.FittingTurns(RoomType.CorridorDeadEnd, RoomRules.West).ToArray());
            Assert.IsEmpty(RoomRules.FittingTurns(RoomType.CorridorDeadEnd, RoomRules.West | RoomRules.East));
        }

        [Test]
        public void SmallRooms_HaveOneDoorAndTurnToIt()
        {
            foreach (var type in new[] { RoomType.SmallOffice, RoomType.SecurityOffice, RoomType.PowerRoom, RoomType.HvacRoom, RoomType.StorageRoom })
            {
                Assert.IsTrue(RoomRules.IsSmallRoom(type));
                Assert.AreEqual(RoomRules.South, RoomRules.DoorSides(type), type.ToString());
                Assert.AreEqual(1, RoomRules.MaxDoors(type));
                Assert.AreEqual(new[] { 3 }, RoomRules.FittingTurns(type, RoomRules.East).ToArray(), type.ToString());
                Assert.IsEmpty(RoomRules.FittingTurns(type, RoomRules.East | RoomRules.West), type.ToString());
            }
        }

        [Test]
        public void Doors_BelongToTheRoomNotTheCorridor()
        {
            Assert.AreEqual(DoorPlacement.Second, RoomRules.DoorBetween(RoomType.CorridorT, RoomType.SmallOffice));
            Assert.AreEqual(DoorPlacement.First, RoomRules.DoorBetween(RoomType.SecurityOffice, RoomType.CorridorStraight));
            Assert.AreEqual(DoorPlacement.First, RoomRules.DoorBetween(RoomType.SmallOffice, RoomType.PowerRoom));
            Assert.AreEqual(DoorPlacement.None, RoomRules.DoorBetween(RoomType.Corridor, RoomType.CorridorDeadEnd));
            Assert.AreEqual(DoorPlacement.HallPiece, RoomRules.DoorBetween(RoomType.Corridor, RoomType.Hall));
            Assert.AreEqual(DoorPlacement.HallPiece, RoomRules.DoorBetween(RoomType.Hall, RoomType.SmallOffice));
            Assert.AreEqual(DoorPlacement.None, RoomRules.DoorBetween(RoomType.Hall, RoomType.Hall));
        }

        [Test]
        public void LiftEnd_IsACorridorTile()
        {
            // The lift opens straight onto the corridor: the lift end is a corridor tile with one arm, like a dead end.
            Assert.IsTrue(RoomRules.IsCorridor(RoomType.Elevator));
            Assert.IsFalse(RoomRules.IsSmallRoom(RoomType.Elevator));
            Assert.AreEqual(RoomRules.South, RoomRules.DoorSides(RoomType.Elevator));
            Assert.AreEqual(new[] { 1 }, RoomRules.FittingTurns(RoomType.Elevator, RoomRules.West).ToArray());
            Assert.IsFalse(RoomRules.HasAutomaticDoors(RoomType.Elevator));
            Assert.AreEqual(DoorPlacement.None, RoomRules.DoorBetween(RoomType.Elevator, RoomType.CorridorStraight));
            // Next to a room with doors, that room's door closes the opening.
            Assert.AreEqual(DoorPlacement.Second, RoomRules.DoorBetween(RoomType.Elevator, RoomType.SmallOffice));
            Assert.AreEqual(DoorPlacement.First, RoomRules.DoorBetween(RoomType.StorageRoom, RoomType.Elevator));
        }

        [Test]
        public void Corridors_HaveTheirFireArmOnAnArm_AndMirrorsReachTheOtherArms()
        {
            foreach (RoomType type in System.Enum.GetValues(typeof(RoomType)))
            {
                if (!RoomRules.IsCorridor(type)) continue;
                int reached = 0;
                for (int layout = 0; layout < RoomRules.LayoutCount(type); layout++)
                {
                    int fire = RoomRules.Mask(RoomRules.FireArmSide(type, layout));
                    Assert.AreNotEqual(0, fire & RoomRules.DoorSides(type), $"{type} layout {layout + 1} has its fire kit in a walled-up arm");
                    reached |= fire;
                }
                // Without turning: the corner and the T reach every arm through their layouts.
                if (type == RoomType.CorridorCorner || type == RoomType.CorridorT)
                {
                    Assert.AreEqual(RoomRules.DoorSides(type), reached, $"{type} cannot have its fire kit in every arm");
                }
            }
            Assert.IsTrue(RoomRules.IsMirroredCorridor(RoomType.CorridorCorner, 1));
            Assert.IsFalse(RoomRules.IsMirroredCorridor(RoomType.CorridorCorner, 0));
        }

        [Test]
        public void Rooms_HaveOneLayout_CorridorsOnePerFireArm()
        {
            foreach (RoomType type in System.Enum.GetValues(typeof(RoomType)))
            {
                int expected = type == RoomType.CorridorCorner ? 2
                    : type == RoomType.CorridorT ? 4
                    : RoomRules.IsCorridor(type) || type == RoomType.Hall ? 1
                    : RoomRules.RoomLayouts;
                Assert.AreEqual(expected, RoomRules.LayoutCount(type), type.ToString());
            }
            Assert.AreEqual(1, RoomRules.RoomLayouts);
        }
    }
}
