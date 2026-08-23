using System;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Bastion kit table — footprint multiples and enum coverage, no scene.</summary>
    public class BastionKitFootprintTests
    {
        [Test]
        public void Footprint_EqualsMultipleTimesScaler_ForEachBlockKind()
        {
            foreach (BastionBlockKind kind in Enum.GetValues(typeof(BastionBlockKind)))
            {
                int multiple = BastionKit.Multiple(kind);
                float aspect = kind == BastionBlockKind.ControlRoom ? BastionKit.ControlRoomAspect : 1f;
                Assert.AreEqual(BastionKit.ScalerX * multiple, BastionKit.FootprintWidth(kind), 1e-4f, kind.ToString());
                Assert.AreEqual(BastionKit.ScalerZ * multiple * aspect, BastionKit.FootprintDepth(kind), 1e-4f, kind.ToString());
            }
        }

        [Test]
        public void Footprint_ControlRoom_IsWideLandscapeHall()
        {
            Assert.AreEqual(94f, BastionKit.FootprintWidth(BastionBlockKind.ControlRoom), 1e-4f);
            Assert.AreEqual(47f, BastionKit.FootprintDepth(BastionBlockKind.ControlRoom), 1e-4f);
        }

        [Test]
        public void StoreyHeight_IsFive()
        {
            Assert.AreEqual(5f, BastionKit.StoreyHeight, 1e-4f);
        }

        [Test]
        public void BastionFieldConsole_ScaleIsOne_DeskAndScreenMatchUseHeight()
        {
            float s = BastionKit.FieldConsoleScale;
            Assert.AreEqual(1f, s, 1e-4f);
            Assert.AreEqual(1.6f, BastionKit.FieldConsoleDeskWidth * s, 1e-4f);
            Assert.AreEqual(1.2f, BastionKit.FieldConsoleDeskHeight * s, 1e-4f);
            Assert.AreEqual(0.7f, BastionKit.FieldConsoleDeskDepth * s, 1e-4f);
            Assert.AreEqual(0.6f, BastionKit.FieldConsoleDeskCentreY * s, 1e-4f);
            Assert.AreEqual(1.7f, BastionKit.FieldConsoleScreenCentreY * s, 1e-4f);
        }

        [Test]
        public void BlockKind_EnumHasExactlyFiveTableRows()
        {
            var values = (BastionBlockKind[])Enum.GetValues(typeof(BastionBlockKind));
            Assert.AreEqual(5, values.Length);
            CollectionAssert.AreEquivalent(
                new[] { BastionBlockKind.Quarter, BastionBlockKind.Half, BastionBlockKind.Full,
                    BastionBlockKind.OpenArea, BastionBlockKind.ControlRoom },
                values);
        }
    }
}
