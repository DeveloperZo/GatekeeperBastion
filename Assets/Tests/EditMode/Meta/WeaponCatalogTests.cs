using System;
using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>M11 catalog + cost curve + migration, updated for M21's wheel-point shape.
    /// T0 (zero points) keeps the printed band; higher "tiers" use the flat-tier migration
    /// distribution (damage+clip only) so pacing gates stay comparable.</summary>
    public class WeaponCatalogTests
    {
        const float Tol = 1.5f;

        static int[] Flat(int tier)
        {
            var p = new int[WeaponWheel.AttrCount];
            WeaponWheel.DistributeFlatTier(p, tier);
            return p;
        }

        static float Dps(WeaponId id, int flatTier) =>
            WeaponWheel.EffectiveDps(WeaponCatalog.Def(id), Flat(flatTier));

        [Test]
        public void EffectiveDps_T0_MatchesPrintedBand()
        {
            // Zero points = old T0. Higher flat tiers diverge from the old dual-curve table by
            // design (M21); only the zero-investment band is pinned here.
            Assert.AreEqual(28f, Dps(WeaponId.Mk1Sidearm, 0), Tol);
            Assert.AreEqual(38f, Dps(WeaponId.Vk7Rifle, 0), Tol);
            Assert.AreEqual(33f, Dps(WeaponId.Br12Shotgun, 0), Tol);
            Assert.AreEqual(37f, Dps(WeaponId.Mr35Rifle, 0), Tol);
        }

        [Test]
        public void FlatMigration_Step_ExceedsNoiseFloor_AllSteps()
        {
            Assert.AreEqual(10, WeaponTier.MaxTier);
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var def = WeaponCatalog.Def(id);
                for (int t = 0; t < WeaponTier.MaxTier; t++)
                {
                    float a = WeaponWheel.EffectiveDps(def, Flat(t));
                    float b = WeaponWheel.EffectiveDps(def, Flat(t + 1));
                    float ratio = b / a;
                    Assert.Greater(ratio, 1.20f,
                        $"{def.displayName} flat T{t}→T{t + 1} ratio {ratio:0.###} must exceed 1.20");
                }
            }
        }

        [Test]
        public void TierCosts_LinearRamp_SumsTo13000()
        {
            long[] expected = { 400L, 600L, 800L, 1000L, 1200L, 1400L, 1600L, 1800L, 2000L, 2200L };
            for (int t = 0; t < WeaponTier.MaxTier; t++)
            {
                Assert.AreEqual(expected[t], WeaponTier.Cost(t), "cost at T" + t);
                Assert.AreEqual(expected[t], WeaponTier.Cost(WeaponSlot.Primary, t),
                    "Primary slot overload must match the shared curve at T" + t);
            }
            Assert.AreEqual(13000L, WeaponTier.SpentToReach(WeaponTier.MaxTier));
        }

        [Test]
        public void SecondaryTierCosts_LinearRamp_SumsTo8250()
        {
            long[] expected = { 150L, 300L, 450L, 600L, 750L, 900L, 1050L, 1200L, 1350L, 1500L };
            for (int t = 0; t < WeaponTier.MaxTier; t++)
                Assert.AreEqual(expected[t], WeaponTier.Cost(WeaponSlot.Secondary, t), "cost at T" + t);
            Assert.AreEqual(8250L,
                WeaponTier.SpentToReach(WeaponSlot.Secondary, WeaponTier.MaxTier));
        }

        [Test]
        public void PrimaryBand_T0_AvailablePrimaries_Within15Percent()
        {
            float min = float.MaxValue, max = 0f;
            foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
            {
                float dps = Dps(id, 0);
                if (dps < min) min = dps;
                if (dps > max) max = dps;
            }
            Assert.Greater(max, 0f);
            float spread = (max - min) / min;
            Assert.LessOrEqual(spread, 0.15f,
                $"Available Primary T0 band spread {spread:0.###} exceeds 15%");
        }

        [Test]
        public void Vk7_StaysDisabled_NotInShop()
        {
            Assert.IsFalse(WeaponCatalog.IsAvailable(WeaponId.Vk7Rifle));
            foreach (var id in WeaponCatalog.ShopWeapons())
                Assert.AreNotEqual(WeaponId.Vk7Rifle, id);
        }

        [Test]
        public void Br12_OneShotsWave2Default_Inside8m_FailsAt14m()
        {
            var def = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            float wave2Hp = WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave);
            float at8 = WeaponWheel.DamageAtRange(def, 0, 8f) * def.pellets;
            float at14 = WeaponWheel.DamageAtRange(def, 0, 14f) * def.pellets;
            Assert.GreaterOrEqual(at8, wave2Hp, $"BR-12 at 8m deals {at8}, need ≥{wave2Hp}");
            Assert.Less(at14, wave2Hp, $"BR-12 at 14m deals {at14}, must fail to one-shot {wave2Hp}");
        }

        [Test]
        public void Mr35_KillsWave3Ram_InSevenShots()
        {
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float wave3RamHp = WaveDirector.StatsFor(GatekeeperKind.Ram).maxHp
                * (1f + WaveDirector.EnemyHealthPerWave * 2f);
            float dmg = WeaponWheel.EffectiveDamage(def, 0);
            Assert.AreEqual(32f, dmg, 0.01f);
            Assert.AreEqual(7, (int)Math.Ceiling(wave3RamHp / dmg),
                "MR-35 must kill a wave-3 Ram (216 HP) in exactly 7 shots at T0");
            Assert.AreEqual(2, def.pierceCount);
        }

        [Test]
        public void ThrowawayComposite_OwnableEquippable_ComposesAllModes()
        {
            var id = WeaponId.ThrowawayComposite;
            var def = WeaponCatalog.Def(id);
            Assert.Greater(def.pellets, 1, "pellets");
            Assert.Greater(def.pierceCount, 0, "pierce");
            Assert.Greater(def.falloffStart, 0f, "falloff");

            var m = MetaState.CreateNew();
            m.EarnSalvage(def.cost);
            m.SetOwned(id, true);
            m.SetEquipped(id);
            Assert.IsTrue(m.Owns(id));
            Assert.AreEqual(id, m.Equipped(def.slot));

            float near = WeaponWheel.DamageAtRange(def, 0, def.falloffStart);
            float far = WeaponWheel.DamageAtRange(def, 0, def.range);
            Assert.Greater(near, far);
            Assert.AreEqual(0f, far, 0.01f);
        }

        [Test]
        public void Mk1_CostsZero_AndIsAlwaysAffordableOnFreshSave()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.IsFalse(m.OwnsAnyWeapon);
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost);
        }

        [Test]
        public void V1Save_MigratesWeaponOwned_ToMk1SecondaryWithTier()
        {
            string v1 =
                "{\"SaveVersion\":1,\"BankedSalvage\":500,\"TotalRuns\":4,\"PodEjections\":1," +
                "\"GunDamageLevel\":2,\"FireRateLevel\":1,\"ClipSizeLevel\":3,\"WeaponOwned\":true}";
            var m = MetaState.FromJson(v1);
            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponId.Mk1Sidearm, m.EquippedSecondary);
            Assert.AreEqual(3, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(18, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Damage));
            Assert.AreEqual(15, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Clip));
            Assert.AreEqual(0, m.PointOf(WeaponId.Mk1Sidearm, WheelAttr.Reload));
            Assert.AreEqual(SaveData.CurrentVersion, m.SaveVersion);
        }

        [Test]
        public void V1Save_WithoutWeapon_OwnsNothing()
        {
            string v1 = "{\"SaveVersion\":1,\"BankedSalvage\":50,\"WeaponOwned\":false}";
            var m = MetaState.FromJson(v1);
            Assert.IsFalse(m.OwnsAnyWeapon);
        }

        [Test]
        public void Json_RoundTrip_PreservesWeapons()
        {
            var m = MetaState.CreateNew();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.SetOwned(WeaponId.Br12Shotgun, true);
            m.ReplacePointsFromFlatTier(WeaponId.Br12Shotgun, 5);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            m.EquippedPrimary = WeaponId.Br12Shotgun;

            var loaded = MetaState.FromJson(m.ToJson());
            Assert.IsTrue(loaded.Owns(WeaponId.Br12Shotgun));
            Assert.AreEqual(5, loaded.TierOf(WeaponId.Br12Shotgun));
            Assert.AreEqual(30, loaded.PointOf(WeaponId.Br12Shotgun, WheelAttr.Damage));
            Assert.AreEqual(WeaponId.Br12Shotgun, loaded.EquippedPrimary);
        }

        [Test]
        public void NoSetTier_OnMetaState()
        {
            Assert.IsNull(typeof(MetaState).GetMethod("SetTier"));
        }

        /// <summary>Criterion 14 / M16 — cheapest available Primary under three-death earnings.
        /// Uses flat-tier migration points as the comparable spend ladder.</summary>
        [Test]
        public void PacingTarget_CheapestAvailablePrimary_UnderThreeDeathsEarnings()
        {
            var waves = new[]
            {
                new { Wave = 2, Need = 50f, ThreeDeaths = 2700L },
                new { Wave = 3, Need = 98f, ThreeDeaths = 5490L },
            };

            foreach (var w in waves)
            {
                long cheapest = long.MaxValue;
                string who = null;
                foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
                {
                    var def = WeaponCatalog.Def(id);
                    for (int t = 0; t <= WeaponTier.MaxTier; t++)
                    {
                        if (WeaponWheel.EffectiveDps(def, Flat(t)) + 0.01f < w.Need) continue;
                        long cost = def.cost + WeaponTier.SpentToReach(t);
                        if (cost < cheapest)
                        {
                            cheapest = cost;
                            who = $"{def.displayName} T{t}";
                        }
                        break;
                    }
                }

                Assert.AreNotEqual(long.MaxValue, cheapest,
                    $"wave {w.Wave}: no available Primary reaches {w.Need} theoretical dps");
                Assert.Less(cheapest, w.ThreeDeaths,
                    $"wave {w.Wave}: {who} costs {cheapest}, must be < 3× death earnings {w.ThreeDeaths}");
            }
        }
    }
}
