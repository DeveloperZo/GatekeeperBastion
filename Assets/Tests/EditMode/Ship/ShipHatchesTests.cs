using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The seat-count assignment rule: one seat opens the middle hatch, two
    /// open the outer pair, three open all three — and the packed wire form carries
    /// exactly that plus the owning slots.</summary>
    public class ShipHatchesTests
    {
        [Test]
        public void OpenFor_OneSeat_OpensTheMiddleOnly()
        {
            CollectionAssert.AreEqual(new[] { 1 }, ShipHatches.OpenFor(1));
        }

        [Test]
        public void OpenFor_TwoSeats_OpensTheOuterPair()
        {
            CollectionAssert.AreEqual(new[] { 0, 2 }, ShipHatches.OpenFor(2));
        }

        [Test]
        public void OpenFor_ThreeSeats_OpensAllThree()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, ShipHatches.OpenFor(3));
        }

        [Test]
        public void OpenFor_ClampsBelowOneAndAboveThree()
        {
            CollectionAssert.AreEqual(new[] { 1 }, ShipHatches.OpenFor(0),
                "there is always a local seat");
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, ShipHatches.OpenFor(4),
                "the guest cap holds the party at the hatch count");
        }

        [Test]
        public void PackFor_OneSeat_MiddleOwnedBySlotZero_OthersSealed()
        {
            int packed = ShipHatches.PackFor(new[] { 0 }, 1);
            Assert.IsFalse(ShipHatches.IsOpen(packed, 0));
            Assert.IsTrue(ShipHatches.IsOpen(packed, 1));
            Assert.IsFalse(ShipHatches.IsOpen(packed, 2));
            Assert.AreEqual(-1, ShipHatches.OwnerSlot(packed, 0));
            Assert.AreEqual(0, ShipHatches.OwnerSlot(packed, 1));
            Assert.AreEqual(-1, ShipHatches.OwnerSlot(packed, 2));
        }

        [Test]
        public void PackFor_TwoSeats_LowerSlotTakesHatchOne_HigherTakesHatchThree()
        {
            int packed = ShipHatches.PackFor(new[] { 0, 1 }, 2);
            Assert.IsTrue(ShipHatches.IsOpen(packed, 0));
            Assert.IsFalse(ShipHatches.IsOpen(packed, 1));
            Assert.IsTrue(ShipHatches.IsOpen(packed, 2));
            Assert.AreEqual(0, ShipHatches.OwnerSlot(packed, 0));
            Assert.AreEqual(-1, ShipHatches.OwnerSlot(packed, 1));
            Assert.AreEqual(1, ShipHatches.OwnerSlot(packed, 2));
        }

        [Test]
        public void PackFor_TwoSeats_KeepsTheActualSlotNumbers()
        {
            // The seat in slot 1 left; slots 0 and 2 remain and keep their numbers.
            int packed = ShipHatches.PackFor(new[] { 0, 2 }, 2);
            Assert.AreEqual(0, ShipHatches.OwnerSlot(packed, 0));
            Assert.AreEqual(2, ShipHatches.OwnerSlot(packed, 2));
        }

        [Test]
        public void PackFor_ThreeSeats_EverySeatOwnsItsHatch()
        {
            int packed = ShipHatches.PackFor(new[] { 0, 1, 2 }, 3);
            for (int i = 0; i < ShipHatches.Count; i++)
            {
                Assert.IsTrue(ShipHatches.IsOpen(packed, i));
                Assert.AreEqual(i, ShipHatches.OwnerSlot(packed, i));
            }
        }

        [Test]
        public void PackedZero_ReadsAsNothingAssigned()
        {
            for (int i = 0; i < ShipHatches.Count; i++)
            {
                Assert.IsFalse(ShipHatches.IsOpen(0, i));
                Assert.AreEqual(-1, ShipHatches.OwnerSlot(0, i));
            }
        }

        [Test]
        public void Apply_AdoptsThePackedOpenFlags()
        {
            var hatches = new ShipHatches();
            hatches.Apply(ShipHatches.PackFor(new[] { 0, 1 }, 2));
            Assert.IsTrue(hatches.IsOpenAt(0));
            Assert.IsFalse(hatches.IsOpenAt(1));
            Assert.IsTrue(hatches.IsOpenAt(2));
        }

        [Test]
        public void IdOf_IsOneBased_BowToStern()
        {
            Assert.AreEqual(1, ShipHatches.IdOf(0));
            Assert.AreEqual(2, ShipHatches.IdOf(ShipHatches.MiddleIndex));
            Assert.AreEqual(3, ShipHatches.IdOf(2));
        }
    }
}
