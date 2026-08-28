using System;
using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M26 Path 1 — SEC-D catalog presence, identity, power band, and kill-line shape.</summary>
    public class SecondaryCatalogTests
    {
        const float Tol = 0.05f;

        static int[] Flat(int tier)
        {
            var p = new int[WeaponWheel.AttrCount];
            WeaponWheel.DistributeFlatTier(p, tier);
            return p;
        }

        static float TimeToEmpty(WeaponDef def) =>
            WeaponWheel.EffectiveClip(def, 0) / WeaponWheel.EffectiveFireRate(def, 0);

        [Test]
        public void ArraySize_ExceedsEveryListedId()
        {
            foreach (WeaponId id in WeaponCatalog.All())
            {
                Assert.Greater(WeaponCatalog.ArraySize, (int)id,
                    $"ArraySize {WeaponCatalog.ArraySize} must exceed id {(int)id} ({id})");
            }
            Assert.Greater(WeaponCatalog.ArraySize, (int)WeaponId.SecD);
        }

        [Test]
        public void SecDIsInEveryCatalogList()
        {
            var def = WeaponCatalog.Def(WeaponId.SecD);
            Assert.AreEqual(WeaponSlot.Secondary, def.slot);
            Assert.AreEqual("SEC-D", def.displayName);
            Assert.AreEqual(0L, def.cost);

            bool inAll = false, inShop = false, inAvailable = false, inSecondary = false;
            foreach (var id in WeaponCatalog.All())
                if (id == WeaponId.SecD) inAll = true;
            foreach (var id in WeaponCatalog.ShopWeapons())
                if (id == WeaponId.SecD) inShop = true;
            inAvailable = WeaponCatalog.IsAvailable(WeaponId.SecD);
            foreach (var id in WeaponCatalog.ForSlot(WeaponSlot.Secondary))
                if (id == WeaponId.SecD) inSecondary = true;

            Assert.IsTrue(inAll, "All()");
            Assert.IsTrue(inShop, "ShopWeapons()");
            Assert.IsTrue(inAvailable, "IsAvailable()");
            Assert.IsTrue(inSecondary, "ForSlot(Secondary)");
        }

        [Test]
        public void SecondariesNeverRivalPrimaries()
        {
            for (int t = 0; t <= WeaponTier.MaxTier; t++)
            {
                var pts = Flat(t);
                float br12 = WeaponWheel.EffectiveDps(WeaponCatalog.Def(WeaponId.Br12Shotgun), pts);
                float mr35 = WeaponWheel.EffectiveDps(WeaponCatalog.Def(WeaponId.Mr35Rifle), pts);
                float primaryFloor = Math.Min(br12, mr35);

                foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Secondary))
                {
                    // Unringed Secondaries cannot buy points — equal investment is T0 only.
                    if (!WeaponWheel.HasRing(id) && t > 0) continue;

                    // BOTH EXEMPTIONS REMOVED, 2026-08-27, AND THE TEST WAS EMPTY WITHOUT THEM.
                    // This fixture skipped MK-1 at every tier ("2026-08-16: MK-1 T0 damage 30 so
                    // a Wave-1 Default is one shot; duty-cycle dps then exceeds the Secondary
                    // band") and SEC-D above T0 ("2026-08-15: SEC-D exceeds BR-12 duty-cycle dps
                    // from T5, 155 vs 152"). There are exactly two ringed Secondaries, so between
                    // them the skips excused every case the loop could have caught above T0 —
                    // a guard named SecondariesNeverRivalPrimaries that had stopped guarding.
                    //
                    // Both exemptions existed for ONE reason: the free MK-1 sustained 59.0 dps
                    // while every purchasable Primary sat at 33-37, so a Secondary out-damaging
                    // the Primary floor was the normal state of the catalog and the pin had to be
                    // hollowed out to stay green. The 2026-08-27 Primary raise (~2x on every
                    // Primary; Secondaries untouched) is precisely the fix for that inversion, so
                    // the exemptions are what should have been deleted by it. Measured after the
                    // raise, across all eleven tiers, MK-1 runs 10-12% under the Primary floor
                    // and SEC-D 34-62% under it — the claim in the name is true again at every
                    // rung, so it is asserted again at every rung.
                    float dps = WeaponWheel.EffectiveDps(WeaponCatalog.Def(id), pts);
                    Assert.Less(dps, primaryFloor,
                        $"{id} flat T{t} dps {dps:0.##} must stay below Primary floor {primaryFloor:0.##}");
                }
            }
        }

        /// <summary>Renamed FROM Mk1_T0_OneShotsWave1Default_TwoShotsWave2 on 2026-08-27, because
        /// the claim in that name INVERTED. BaseEnemyHp went 30 -> 50 and the sidearm did not move,
        /// so a wave-1 Default now costs two shots where it cost one, and a wave-2 body costs three
        /// where it cost two. That is a deliberate consequence of the fewer-and-tougher pass, not a
        /// regression: the free sidearm out-killed every purchasable Primary on the opening wave
        /// precisely BECAUSE its one shot was the whole kill, and taking that away is most of what
        /// the raise was for.</summary>
        [Test]
        public void Mk1_T0_TwoShotsWave1Default_ThreeShotsWave2()
        {
            var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            float dmg = mk1.damage * System.Math.Max(1, mk1.pellets);

            Assert.AreEqual(2, ShotsToKill(dmg, DefaultHp(0)), "wave-1 Default");
            Assert.AreEqual(3, ShotsToKill(dmg, DefaultHp(1)), "wave-2 Default");

            // THE CLAIM UNDERNEATH THE TWO COUNTS, and the reason it is worth a pin at all: the
            // free sidearm must never one-shot the calibration wave again. One shot per kill is
            // what let it beat a 1200-salvage rifle on wave 1 at equal tier, and it is the exact
            // shape the 2026-08-27 pass was written to remove.
            Assert.Greater(ShotsToKill(dmg, DefaultHp(0)), 1,
                $"the free sidearm deals {dmg} into a {DefaultHp(0)} hp wave-1 body; if that ever "
                + "becomes one shot again, every Primary loses the opening wave to it");

            // ...and the ladder has to keep costing it more, or the sidearm scales for free.
            for (int w = 1; w < WaveDirector.MaxWaves; w++)
                Assert.GreaterOrEqual(ShotsToKill(dmg, DefaultHp(w)),
                                      ShotsToKill(dmg, DefaultHp(w - 1)),
                    "wave " + w + " must not be cheaper for the sidearm than the wave before it");
            Assert.Greater(ShotsToKill(dmg, DefaultHp(WaveDirector.MaxWaves - 1)),
                           ShotsToKill(dmg, DefaultHp(0)),
                "the deepest wave must cost the sidearm strictly more shots than the first");
        }

        static float DefaultHp(int waveIndex) =>
            WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave * waveIndex);

        static int ShotsToKill(float damagePerShot, float hp) =>
            (int)System.Math.Ceiling(hp / damagePerShot);

        [Test]
        public void FirstWheelPurchase_AffordableOffOneWaveOneClear()
        {
            // Zo 2026-08-14: one wave-1 clear funds the first Secondary purchase with
            // headroom for a second, and the entry price stays at least 150 so it is not
            // trivial. All sides read source constants so a retune of any breaks loudly.
            long waveOneTake =
                (long)(WaveDirector.BaseEnemyCount * MetaUpgrades.SalvagePerKill());
            long first = WeaponTier.Cost(WeaponSlot.Secondary, 0);
            long second = WeaponTier.Cost(WeaponSlot.Secondary, 1);
            Assert.GreaterOrEqual(first, 150L, "entry price floor (Zo)");
            Assert.LessOrEqual(first + second, waveOneTake,
                $"two purchases ({first} + {second}) must fit one wave-1 clear ({waveOneTake})");
        }

        [Test]
        public void SecDAndMk1AreDifferentWeapons()
        {
            var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            var secd = WeaponCatalog.Def(WeaponId.SecD);

            float mk1AtWorking = WeaponWheel.DamageAtRange(mk1, 0, mk1.falloffStart);
            float secdAtMk1Working = WeaponWheel.DamageAtRange(secd, 0, mk1.falloffStart);
            Assert.Greater(mk1AtWorking, 0f);
            Assert.Less(secdAtMk1Working, mk1AtWorking * 0.05f,
                $"SEC-D at MK-1's {mk1.falloffStart} m working distance deals {secdAtMk1Working}, must be near-zero");

            float mk1Empty = TimeToEmpty(mk1);
            float secdEmpty = TimeToEmpty(secd);
            Assert.GreaterOrEqual(mk1Empty / secdEmpty, 2f,
                $"time-to-empty MK-1 {mk1Empty:0.##}s vs SEC-D {secdEmpty:0.##}s must differ by ≥2×");
        }

        [Test]
        public void IdentityCuesAreUnique()
        {
            var seen = new HashSet<string>();
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var def = WeaponCatalog.Def(id);
                string key = $"{def.crosshair}|{def.tracerR:0.###},{def.tracerG:0.###},{def.tracerB:0.###}";
                Assert.IsTrue(seen.Add(key), $"{id} shares crosshair+tracer with another shop weapon ({key})");
            }
        }

        [Test]
        public void BothSecondariesFree_ReachableOnFreshCampaign()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost);
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.SecD).cost);

            m.SetOwned(WeaponId.SecD, true);
            m.SetEquipped(WeaponId.SecD);
            Assert.AreEqual(WeaponId.SecD, m.EquippedSecondary);

            var loaded = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.IsTrue(loaded.Owns(WeaponId.SecD));
            Assert.AreEqual(WeaponId.SecD, loaded.EquippedSecondary);
        }

        [Test]
        public void KillLog_NamesTheSlot()
        {
            string primary = KillLog.Format(
                "Hunter", WeaponSlot.Primary, 25L, 3, hatchDist: 34.2f, spawnDist: 11.8f);
            Assert.AreEqual(
                "killed a Hunter with Primary (+25 unbanked) -- 3 left hatchDist=34.2 spawnDist=11.8",
                primary);

            string secondary = KillLog.Format(
                "Default", WeaponSlot.Secondary, 10L, 0, hatchDist: 5.0f, spawnDist: -1f);
            Assert.AreEqual(
                "killed a Default with Secondary (+10 unbanked) -- 0 left hatchDist=5.0 spawnDist=-1.0",
                secondary);

            string unknown = KillLog.FormatUnknownSlot(
                "Thief", 40L, 1, hatchDist: 0.4f, spawnDist: 2.5f);
            Assert.AreEqual(
                "killed a Thief (+40 unbanked) -- 1 left hatchDist=0.4 spawnDist=2.5",
                unknown);
        }

        [Test]
        public void SecD_AbilityUnset_RingAuthored()
        {
            Assert.AreEqual(AbilityId.None, WeaponCatalog.Def(WeaponId.SecD).abilityId);
            var ring = WeaponWheel.RingOf(WeaponId.SecD);
            Assert.IsNotNull(ring);
            Assert.AreEqual(WeaponWheel.AttrCount, ring.Length);
            WheelAttr strength, weakness;
            Assert.IsTrue(WeaponWheel.TryIdentity(WeaponId.SecD, out strength, out weakness));
            Assert.AreEqual(WheelAttr.FireRate, strength);
            Assert.AreEqual(WheelAttr.Reload, weakness);
            Assert.IsFalse(WeaponWheel.SpokePurchasable(WeaponId.SecD, WheelAttr.Ability));
        }

        [Test]
        public void SecDFireRateFocus_NeverRivalsFocusedPrimaries()
        {
            for (int n = 1; n <= WeaponWheel.MaxPurchases; n++)
            {
                float secd = WeaponWheel.EffectiveDps(
                    WeaponCatalog.Def(WeaponId.SecD),
                    WeaponWheel.FocusHistory(WeaponId.SecD, WheelAttr.FireRate, n));
                foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
                {
                    float primary = WeaponWheel.EffectiveDps(
                        WeaponCatalog.Def(id),
                        WeaponWheel.FocusHistory(id, WheelAttr.Damage, n));
                    Assert.Less(secd, primary,
                        $"SEC-D fire-rate focus x{n} dps {secd:0.##} must stay below {id}'s {primary:0.##}");
                }
            }
        }
    }
}
