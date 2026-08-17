using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    public class BastionSegmentDoorAlongTests
    {
        [Test]
        public void TryDoorPillars_Centered_SplitsEvenly()
        {
            Assert.IsTrue(BastionSegment.TryDoorPillars(
                65f, 3f, 0f, out float open, out float west, out float east));
            Assert.AreEqual(0f, open, 1e-4f);
            Assert.AreEqual(31f, west, 1e-4f);
            Assert.AreEqual(31f, east, 1e-4f);
        }

        [Test]
        public void TryDoorPillars_Offset_ShiftsOpeningAlongEdge()
        {
            Assert.IsTrue(BastionSegment.TryDoorPillars(
                65f, 3f, 10f, out float open, out float west, out float east));
            Assert.AreEqual(10f, open, 1e-4f);
            Assert.AreEqual(41f, west, 1e-4f);
            Assert.AreEqual(21f, east, 1e-4f);
            Assert.AreEqual(65f, west + 3f + east, 1e-4f);
        }

        [Test]
        public void TryDoorPillars_ClampsToKeepOpeningOnEdge()
        {
            Assert.IsTrue(BastionSegment.TryDoorPillars(
                40f, 3f, 100f, out float open, out float west, out float east));
            Assert.AreEqual(18.5f, open, 1e-4f);
            Assert.AreEqual(37f, west, 1e-4f);
            Assert.AreEqual(0f, east, 1e-4f);
        }
    }
}
