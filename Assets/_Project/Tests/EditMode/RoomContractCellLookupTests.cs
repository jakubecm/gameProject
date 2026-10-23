using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class RoomContractCellLookupTests
    {
        [Test]
        public void ColumnAndRowAt_ReturnTheCellContainingAPosition()
        {
            Assert.AreEqual(0, RoomContract.ColumnAt(0f));
            Assert.AreEqual(0, RoomContract.ColumnAt(5.9f));
            Assert.AreEqual(0, RoomContract.ColumnAt(-5.9f));
            Assert.AreEqual(1, RoomContract.ColumnAt(6.1f));
            Assert.AreEqual(-1, RoomContract.ColumnAt(-6.1f));
            Assert.AreEqual(3, RoomContract.RowAt(36f));
            Assert.AreEqual(3, RoomContract.RowAt(41f));
        }

        [Test]
        public void CellOrigin_IsInsideItsOwnCell()
        {
            for (int index = -5; index <= 20; index++)
            {
                Assert.AreEqual(index, RoomContract.ColumnAt(RoomContract.CellOriginX(index)));
                Assert.AreEqual(index, RoomContract.RowAt(RoomContract.CellOriginZ(index)));
            }
        }
    }
}
