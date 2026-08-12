using System;
using GatekeeperBastion.Game;
using NUnit.Framework;
using UnityEngine;

namespace GatekeeperBastion.Tests
{
    /// <summary>Bastion kit table — footprint multiples and enum coverage, no scene.</summary>
    public class BastionKitFootprintTests
    {
        [Test]
        public void Footprint_EqualsMultipleTimesScaler_ForEachBlockKind()
        {
            foreach (BastionBlockKind kind in Enum.GetValues(typeof(BastionBlockKind)))
            {
                Vector2 footprint = BastionKit.Footprint(kind);
                int multiple = BastionKit.Multiple(kind);
                Assert.AreEqual(BastionKit.Scaler.x * multiple, footprint.x, 1e-4f, kind.ToString());
                Assert.AreEqual(BastionKit.Scaler.y * multiple, footprint.y, 1e-4f, kind.ToString());
            }
        }

        [Test]
        public void StoreyHeight_IsFive()
        {
            Assert.AreEqual(5f, BastionKit.StoreyHeight, 1e-4f);
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
