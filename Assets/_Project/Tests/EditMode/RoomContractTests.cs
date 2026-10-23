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
        public void ServerAisle_HasTwelveSlots()
        {
            Assert.AreEqual(RoomContract.ServerAisleRackSlots, RoomContract.GetServerAisleRackSlots().Length);
        }

        [Test]
        public void ServerAisle_RacksStayInsideInterior()
        {
            foreach (var (minX, maxX, minZ, maxZ) in Footprints())
            {
                Assert.GreaterOrEqual(minX, -RoomContract.InteriorHalfSize - Epsilon);
                Assert.LessOrEqual(maxX, RoomContract.InteriorHalfSize + Epsilon);
                Assert.GreaterOrEqual(minZ, -RoomContract.InteriorHalfSize - Epsilon);
                Assert.LessOrEqual(maxZ, RoomContract.InteriorHalfSize + Epsilon);
            }
        }

        [Test]
        public void ServerAisle_RacksDoNotBlockKeepClearCross()
        {
            float half = RoomContract.KeepClearWidth / 2f;
            foreach (var (minX, maxX, minZ, maxZ) in Footprints())
            {
                // The cross is the union of a strip along X and a strip along Z, both centered on the origin.
                bool hitsStripAlongX = minZ < half - Epsilon && maxZ > -half + Epsilon;
                bool hitsStripAlongZ = minX < half - Epsilon && maxX > -half + Epsilon;
                Assert.IsFalse(hitsStripAlongX || hitsStripAlongZ);
            }
        }

        [Test]
        public void ServerAisle_RacksDoNotOverlapEachOther()
        {
            var boxes = Footprints().ToArray();
            for (int i = 0; i < boxes.Length; i++)
            {
                for (int j = i + 1; j < boxes.Length; j++)
                {
                    bool overlapX = boxes[i].minX < boxes[j].maxX - Epsilon && boxes[j].minX < boxes[i].maxX - Epsilon;
                    bool overlapZ = boxes[i].minZ < boxes[j].maxZ - Epsilon && boxes[j].minZ < boxes[i].maxZ - Epsilon;
                    Assert.IsFalse(overlapX && overlapZ, $"Slots {i} and {j} overlap");
                }
            }
        }

        [Test]
        public void ServerAisle_RackBacksStandAgainstWalls()
        {
            foreach (var (minX, maxX, _, _) in Footprints())
            {
                bool againstWest = System.Math.Abs(minX + RoomContract.InteriorHalfSize) < Epsilon;
                bool againstEast = System.Math.Abs(maxX - RoomContract.InteriorHalfSize) < Epsilon;
                Assert.IsTrue(againstWest || againstEast);
            }
        }

        [Test]
        public void ServerAisle_RacksFaceTheRoomInterior()
        {
            foreach (var slot in RoomContract.GetServerAisleRackSlots())
            {
                float expectedYaw = slot.X < 0 ? 90f : 270f;
                Assert.AreEqual(expectedYaw, slot.YawDegrees, Epsilon);
            }
        }

        // Rack footprints in room-local space. Slots are on the east/west walls and face along X,
        // so the rack depth runs along X and its width along Z.
        private static System.Collections.Generic.IEnumerable<(float minX, float maxX, float minZ, float maxZ)> Footprints()
        {
            foreach (var slot in RoomContract.GetServerAisleRackSlots())
            {
                float halfDepth = RoomContract.RackDepth / 2f;
                float halfWidth = RoomContract.RackWidth / 2f;
                yield return (slot.X - halfDepth, slot.X + halfDepth, slot.Z - halfWidth, slot.Z + halfWidth);
            }
        }
    }
}
