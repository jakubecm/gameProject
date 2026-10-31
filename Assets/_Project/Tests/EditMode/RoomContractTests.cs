using System.Linq;
using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class RoomContractTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void Opening_FitsInsideWall()
        {
            Assert.Less(RoomContract.OpeningWidth, RoomContract.InteriorSize);
            Assert.Less(RoomContract.OpeningHeight, RoomContract.CeilingHeight);
        }

        [Test]
        public void KeepClearWidth_MatchesOpeningWidth()
        {
            Assert.AreEqual(RoomContract.OpeningWidth, RoomContract.KeepClearWidth, Epsilon);
        }

        [Test]
        public void KeepClearHeight_IsTheOpeningHeight()
        {
            Assert.AreEqual(RoomContract.OpeningHeight, RoomContract.KeepClearHeight, Epsilon);
        }

        [Test]
        public void InteriorSize_IsCellMinusTwoWalls()
        {
            Assert.AreEqual(11.6f, RoomContract.InteriorSize, Epsilon);
            Assert.AreEqual(5.8f, RoomContract.InteriorHalfSize, Epsilon);
        }

        [Test]
        public void NeighbouringCells_WallsStandBackToBack()
        {
            // Outer face of the east wall of column 0 is the outer face of the west wall of column 1.
            float eastFace = RoomContract.CellOriginX(0) + RoomContract.CellHalfSize;
            float westFace = RoomContract.CellOriginX(1) - RoomContract.CellHalfSize;
            Assert.AreEqual(eastFace, westFace, Epsilon);

            float northFace = RoomContract.CellOriginZ(0) + RoomContract.CellHalfSize;
            float southFace = RoomContract.CellOriginZ(1) - RoomContract.CellHalfSize;
            Assert.AreEqual(northFace, southFace, Epsilon);
        }

        [Test]
        public void CellOrigin_IsColumnAndRowTimesCellSize()
        {
            Assert.AreEqual(36f, RoomContract.CellOriginX(3), Epsilon);
            Assert.AreEqual(-24f, RoomContract.CellOriginZ(-2), Epsilon);
        }

        [Test]
        public void OpeningPose_StandsInWallMiddleAndFacesCenter()
        {
            foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
            {
                var pose = RoomContract.GetOpeningPose(side);
                Assert.AreEqual(side.ColumnOffset() * RoomContract.WallCenterOffset, pose.X, Epsilon, side.ToString());
                Assert.AreEqual(side.RowOffset() * RoomContract.WallCenterOffset, pose.Z, Epsilon, side.ToString());

                // Yaw 0 faces +Z; stepping forward from the opening must move toward the center.
                double yaw = pose.YawDegrees * System.Math.PI / 180.0;
                double forwardX = System.Math.Sin(yaw);
                double forwardZ = System.Math.Cos(yaw);
                Assert.AreEqual(-side.ColumnOffset(), forwardX, Epsilon, side.ToString());
                Assert.AreEqual(-side.RowOffset(), forwardZ, Epsilon, side.ToString());
            }
        }

        [Test]
        public void DoorPocket_HoldsTheOpenLeavesBesideTheOpening()
        {
            // Each leaf of the bi-parting door is half the opening wide and slides fully beside it.
            Assert.GreaterOrEqual(RoomContract.DoorPocketEnd, RoomContract.OpeningWidth);
            Assert.GreaterOrEqual(RoomContract.DoorPocketHeight, RoomContract.OpeningHeight);
            Assert.LessOrEqual(RoomContract.DoorPocketHeight, RoomContract.CeilingHeight);
            // Two pockets of neighbouring openings never meet in a corner.
            Assert.Less(RoomContract.DoorPocketEnd, RoomContract.InteriorHalfSize - RoomContract.DoorPocketDepth);
        }

        [Test]
        public void SmallRoom_IsHalfTheCellByItsSouthDoor()
        {
            Assert.AreEqual(RoomContract.InteriorHalfSize, RoomContract.SmallRoomDepth, Epsilon);
            Assert.Less(RoomContract.SmallRoomKeepClearDepth, RoomContract.SmallRoomDepth);
            // The back wall stays behind the east and west openings' middle, so their plugs close the room.
            Assert.GreaterOrEqual(RoomContract.SmallRoomBackWall, -Epsilon);
        }
    }
}
