using System;
using System.Collections.Generic;
using System.Reflection;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M21 — upgrade wheel arithmetic, rings, migration, and the 2× temporal gate.</summary>
    public class WeaponWheelTests
    {
        [Test]
        public void AwardSpreadIsAlwaysEleven()
        {
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var ring = WeaponWheel.RingOf(id);
                if (ring == null) continue;
                foreach (WheelAttr centre in Enum.GetValues(typeof(WheelAttr)))
                {
                    var pts = new int[WeaponWheel.AttrCount];
                    WeaponWheel.Award(pts, ring, centre);
                    int sum = 0;
                    int fives = 0, threes = 0, zeros = 0;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        sum += pts[i];
                        if (pts[i] == 5) fives++;
                        else if (pts[i] == 3) threes++;
                        else if (pts[i] == 0) zeros++;
                        else Assert.Fail($"unexpected award {pts[i]} on {id} centre {centre}");
                    }
                    Assert.AreEqual(11, sum, $"{id} centre {centre}");
                    Assert.AreEqual(1, fives);
                    Assert.AreEqual(2, threes);
                    Assert.AreEqual(2, zeros);
                    Assert.AreEqual(5, pts[(int)centre]);
                }
            }
        }

        [Test]
        public void TierIsDerivedNeverStored()
        {
            Assert.IsNull(typeof(MetaState).GetMethod("SetTier",
                BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNull(typeof(MetaState).GetField("_weaponTiers",
                BindingFlags.NonPublic | BindingFlags.Instance));

            var m = MetaState.CreateNew();
            m.SetOwned(WeaponId.Br12Shotgun, true);
            Assert.AreEqual(0, m.TierOf(WeaponId.Br12Shotgun));
            m.AwardWheel(WeaponId.Br12Shotgun, WheelAttr.Damage);
            Assert.AreEqual(1, m.TierOf(WeaponId.Br12Shotgun));
            Assert.AreEqual(11, WeaponWheel.TotalPoints(m.PointsOf(WeaponId.Br12Shotgun)));
        }

        [Test]
        public void RingsAreUniqueAcrossShopWeapons()
        {
            var seen = new HashSet<string>();
            int ringed = 0;
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var ring = WeaponWheel.RingOf(id);
                if (ring == null) continue;
                ringed++;
                string key = WeaponWheel.CanonicalRingKey(ring);
                Assert.IsTrue(seen.Add(key), $"{id} ring collides with another shop weapon ({key})");
            }
            Assert.AreEqual(5, ringed, "every shop weapon has a ring");
        }

        [Test]
        public void SpokePurchasable_FollowsCatalogAbility()
        {
            Assert.IsTrue(WeaponWheel.SpokePurchasable(WeaponId.Mk1Sidearm, WheelAttr.Ability));
            Assert.IsTrue(WeaponWheel.SpokePurchasable(WeaponId.Mk1Sidearm, WheelAttr.Reload));
            Assert.IsTrue(WeaponWheel.SpokePurchasable(WeaponId.SecD, WheelAttr.FireRate));
            Assert.IsFalse(WeaponWheel.SpokePurchasable(WeaponId.SecD, WheelAttr.Ability));
            Assert.IsFalse(WeaponWheel.SpokePurchasable(WeaponId.None, WheelAttr.Damage));
        }

        [Test]
        public void WeaknessNeverAdjacentToStrength()
        {
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                WheelAttr strength, weakness;
                if (!WeaponWheel.TryIdentity(id, out strength, out weakness)) continue;
                var ring = WeaponWheel.RingOf(id);
                Assert.IsNotNull(ring, id.ToString());
                int si = -1, wi = -1;
                for (int i = 0; i < ring.Length; i++)
                {
                    if (ring[i] == strength) si = i;
                    if (ring[i] == weakness) wi = i;
                }
                Assert.GreaterOrEqual(si, 0);
                Assert.GreaterOrEqual(wi, 0);
                int dist = WeaponWheel.RingDistance(si, wi, ring.Length);
                Assert.Greater(dist, 1,
                    $"{id}: strength {strength} adjacent to weakness {weakness}");
            }
        }

        [Test]
        public void OppositeFocusesDivergeTwoFold()
        {
            // BR-12: damage focus vs reload focus (declared strength vs weakness).
            var def = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            int n = WeaponWheel.MaxPurchases;
            var dFocus = WeaponWheel.FocusHistory(WeaponId.Br12Shotgun, WheelAttr.Damage, n);
            var rFocus = WeaponWheel.FocusHistory(WeaponId.Br12Shotgun, WheelAttr.Reload, n);
            Assert.AreEqual(WeaponWheel.PointsPerPurchase * n, WeaponWheel.TotalPoints(dFocus));
            Assert.AreEqual(WeaponWheel.PointsPerPurchase * n, WeaponWheel.TotalPoints(rFocus));

            float burstD = WeaponWheel.BurstSeconds(def, dFocus[(int)WheelAttr.Clip],
                dFocus[(int)WheelAttr.FireRate]);
            float burstR = WeaponWheel.BurstSeconds(def, rFocus[(int)WheelAttr.Clip],
                rFocus[(int)WheelAttr.FireRate]);
            float reloadD = WeaponWheel.EffectiveReload(def, dFocus[(int)WheelAttr.Reload]);
            float reloadR = WeaponWheel.EffectiveReload(def, rFocus[(int)WheelAttr.Reload]);

            float burstRatio = Math.Max(burstD, burstR) / Math.Min(burstD, burstR);
            float reloadRatio = Math.Max(reloadD, reloadR) / Math.Min(reloadD, reloadR);
            Assert.GreaterOrEqual(Math.Max(burstRatio, reloadRatio), 2f - 1e-3f,
                $"burst {burstRatio:0.##}x reload {reloadRatio:0.##}x — need ≥2x on one");
        }

        [Test]
        public void Mk1WheelPoints_SurviveJsonRoundTrip()
        {
            // Two reload-centred purchases on the MK-1 ring: Reload +5, neighbours
            // Ability and Clip +3 each. Pins the ring's neighbour structure and the
            // save path in one round trip.
            var m = new MetaState();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Reload);
            m.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Reload);
            var loaded = MetaState.FromJson(m.ToJson());
            Assert.AreEqual(10, loaded.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Reload));
            Assert.AreEqual(6, loaded.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Ability));
            Assert.AreEqual(6, loaded.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Clip));
            Assert.AreEqual(0, loaded.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Damage));
            Assert.AreEqual(0, loaded.PointOf(WeaponId.Mk1Sidearm, WheelAttr.FireRate));
            Assert.AreEqual(2, loaded.TierOf(WeaponId.Mk1Sidearm));
        }

        [Test]
        public void MigrationFromFlatTiers()
        {
            // MK-1 cost 0 + SpentToReach(3)=1800 → reconstruct floor 1800 matches lifetime spent.
            const string json =
                "{\"SaveVersion\":3,\"BankedSalvage\":1000,\"LifetimeSalvageEarned\":2800," +
                "\"LifetimeSalvageSpent\":1800,\"WeaponsOwned\":[true,false,false,false,false]," +
                "\"WeaponTiers\":[3,0,0,0,0]," +
                "\"EquippedPrimary\":-1,\"EquippedSecondary\":0,\"EquippedDrone\":-1}";
            var m = MetaState.FromJson(json);
            Assert.AreEqual(3, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(18, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Damage));
            Assert.AreEqual(15, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Clip));
            Assert.AreEqual(0, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Reload));
            Assert.AreEqual(0, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.FireRate));
            Assert.AreEqual(0, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Ability));
            Assert.AreEqual(33, WeaponWheel.TotalPoints(m.PointsOf(WeaponId.Mk1Sidearm)));
            Assert.AreEqual(SaveData.CurrentVersion, m.SaveVersion);
        }

        [Test]
        public void SalvageLedgerStillBalances()
        {
            const string json =
                "{\"SaveVersion\":3,\"BankedSalvage\":1000,\"LifetimeSalvageEarned\":2800," +
                "\"LifetimeSalvageSpent\":1800,\"WeaponsOwned\":[true,false,false,false,false]," +
                "\"WeaponTiers\":[3,0,0,0,0]," +
                "\"EquippedPrimary\":-1,\"EquippedSecondary\":0,\"EquippedDrone\":-1}";
            var m = MetaState.FromJson(json);
            Assert.IsTrue(m.SalvageLedgerBalances);
            Assert.IsFalse(m.SalvageStatsWereRepaired);
            Assert.AreEqual(m.LifetimeSalvageEarned, m.BankedSalvage + m.LifetimeSalvageSpent);
        }

        [Test]
        public void PerPointTable_FocusAtMax_MatchesAnchors()
        {
            // Anchors kept at the old focus-35 targets for the mid ladder; full focus (50) is higher.
            Assert.AreEqual(4.77f, WeaponWheel.DamageMul(35), 0.02f);
            Assert.AreEqual(2.66f, WeaponWheel.ClipMul(35), 0.02f);
            Assert.AreEqual(0.50f, WeaponWheel.ReloadMul(35), 0.02f);
            Assert.AreEqual(1.50f, WeaponWheel.FireRateMul(35), 0.02f);
            Assert.AreEqual(50, WeaponWheel.FocusAtMax);
            Assert.AreEqual(9.30f, WeaponWheel.DamageMul(WeaponWheel.FocusAtMax), 0.05f);
            Assert.AreEqual(4.04f, WeaponWheel.ClipMul(WeaponWheel.FocusAtMax), 0.05f);
            Assert.AreEqual(0.37f, WeaponWheel.ReloadMul(WeaponWheel.FocusAtMax), 0.02f);
            Assert.AreEqual(1.79f, WeaponWheel.FireRateMul(WeaponWheel.FocusAtMax), 0.02f);
        }
    }
}
