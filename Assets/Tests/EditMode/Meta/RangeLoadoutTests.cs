using System;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The kit the range door hands across the scene load: owned weapons carry the
    /// realm's wheel points, unowned ones arrive at base and say so, and the protocol is any
    /// of the three on sale whether or not the realm has claimed one.</summary>
    public class RangeLoadoutTests
    {
        static MetaState RealmOwning(WeaponId id, WheelAttr centre, int purchases)
        {
            var m = MetaState.CreateNew();
            m.SetOwned(id, true);
            for (int i = 0; i < purchases; i++)
                m.AwardWheel(id, centre);
            return m;
        }

        [Test]
        public void OwnedWeapon_CarriesTheRealmsOwnPoints()
        {
            var m = RealmOwning(WeaponId.Mr35Rifle, WheelAttr.Damage, 3);
            var kit = RangeLoadout.From(m, BoarderProtocolId.Depot, WeaponId.Mr35Rifle, WeaponId.Mk1Sidearm);

            CollectionAssert.AreEqual(m.PointsOf(WeaponId.Mr35Rifle), kit.PrimaryPoints);
            Assert.IsFalse(kit.PrimaryIsBase);
            Assert.AreEqual(m.TierOf(WeaponId.Mr35Rifle), kit.PrimaryTier);
            Assert.AreEqual(3, kit.PrimaryTier, "three centred buys is three purchases of eleven points");
        }

        [Test]
        public void UnownedWeapon_IsBaseAndFlagged()
        {
            var m = RealmOwning(WeaponId.Mr35Rifle, WheelAttr.Damage, 3);
            Assert.IsFalse(m.Owns(WeaponId.Mk1Sidearm));
            var kit = RangeLoadout.From(m, BoarderProtocolId.Depot, WeaponId.Mr35Rifle, WeaponId.Mk1Sidearm);

            Assert.IsTrue(kit.SecondaryIsBase);
            Assert.AreEqual(0, WeaponWheel.TotalPoints(kit.SecondaryPoints));
            Assert.AreEqual(0, kit.SecondaryTier);
        }

        [Test]
        public void Protocol_AnyOfTheThreeOnSale_ClaimedOrNot()
        {
            var m = MetaState.CreateNew();
            // Re-keyed 2026-09-04: a fresh Boarder carries the grant. The point of the test is
            // unchanged — the range door offers all three whatever is equipped.
            Assert.AreEqual(BoarderProtocol.GrantedProtocol, m.EquippedProtocol);
            foreach (var p in BoarderProtocol.ShopProtocols())
            {
                var kit = RangeLoadout.From(m, p, WeaponId.Br12Shotgun, WeaponId.SecD);
                Assert.AreEqual(p, kit.Protocol);
            }
            Assert.Throws<ArgumentException>(() =>
                RangeLoadout.From(m, BoarderProtocolId.None, WeaponId.Br12Shotgun, WeaponId.SecD));
        }

        [Test]
        public void WrongSlot_Refused()
        {
            var m = MetaState.CreateNew();
            Assert.Throws<ArgumentException>(() =>
                RangeLoadout.From(m, BoarderProtocolId.Depot, WeaponId.Mk1Sidearm, WeaponId.SecD),
                "a Secondary in the Primary slot");
            Assert.Throws<ArgumentException>(() =>
                RangeLoadout.From(m, BoarderProtocolId.Depot, WeaponId.Mr35Rifle, WeaponId.PriC),
                "a Primary in the Secondary slot");
        }

        [Test]
        public void DisabledWeapon_Refused()
        {
            var m = MetaState.CreateNew();
            m.SetOwned(WeaponId.Vk7Rifle, true);
            Assert.IsFalse(WeaponCatalog.IsAvailable(WeaponId.Vk7Rifle));
            Assert.Throws<ArgumentException>(() =>
                RangeLoadout.From(m, BoarderProtocolId.Depot, WeaponId.Vk7Rifle, WeaponId.Mk1Sidearm));
            Assert.Throws<ArgumentException>(() =>
                RangeLoadout.From(m, BoarderProtocolId.Depot, WeaponId.ThrowawayComposite, WeaponId.Mk1Sidearm),
                "the test-only row is not on the rack either");
        }

        [Test]
        public void Summary_NamesProtocolAndEachWeaponsTier_MarkingBase()
        {
            var m = RealmOwning(WeaponId.Mr35Rifle, WheelAttr.Damage, 3);
            var kit = RangeLoadout.From(m, BoarderProtocolId.Forge, WeaponId.Mr35Rifle, WeaponId.Mk1Sidearm);
            Assert.AreEqual(
                BoarderProtocol.ProtocolName(BoarderProtocolId.Forge) + ", MR-35 Rifle T3, MK-1 Sidearm T0 base",
                kit.Summary());
        }

        [Test]
        public void ReplacePoints_AdoptsTheCarriedShapeExactly()
        {
            var realm = RealmOwning(WeaponId.Mr35Rifle, WheelAttr.Damage, 3);
            var kit = RangeLoadout.From(realm, BoarderProtocolId.Depot, WeaponId.Mr35Rifle, WeaponId.Mk1Sidearm);

            var range = MetaState.CreateNew();
            range.SetOwned(kit.Primary, true);
            range.ReplacePoints(kit.Primary, kit.PrimaryPoints);

            CollectionAssert.AreEqual(realm.PointsOf(WeaponId.Mr35Rifle), range.PointsOf(WeaponId.Mr35Rifle));
            Assert.AreEqual(realm.TierOf(WeaponId.Mr35Rifle), range.TierOf(WeaponId.Mr35Rifle));
            Assert.Throws<ArgumentException>(() => range.ReplacePoints(kit.Primary, new int[2]));
        }
    }
}
