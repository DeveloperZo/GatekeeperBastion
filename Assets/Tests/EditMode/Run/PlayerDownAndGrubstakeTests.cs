using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>
    /// EditMode tests for the death-only run-end model and the new-save economy:
    /// NotifyPlayerDown, CreateNew at 0 banked, free MK-1 ownership, WeaponTier costs,
    /// the first-death upgrade gate, and persistence of weapon fields.
    /// </summary>
    public class PlayerDownAndGrubstakeTests
    {
        private static RunSim NewTetheredSim(out MetaState meta)
        {
            meta = new MetaState();
            var sim = new RunSim(meta, seed: 4242);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        private static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        // ---- NotifyPlayerDown: the death path (bookkeeping only) ----

        [Test]
        public void PlayerDown_MidAssault_Pods_AndLosesNothing()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.TriggerDraw();
            Kill(sim, 2);
            Assert.AreEqual(50L, meta.BankedSalvage, "banked while still alive");

            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(50L, meta.BankedSalvage, "dying costs the player zero currency");
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("player", sim.LastLossReport.cause);
            Assert.AreEqual(50L, sim.LastLossReport.grossEarned);
        }

        [Test]
        public void PlayerDown_DuringPrep_AlsoEndsTheRun()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.LastLossReport.grossEarned);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("player", sim.LastLossReport.cause);
        }

        [Test]
        public void Dying_NeverReducesBankedSalvage_AtAnySiphonLevel()
        {
            long[] bankedPerKill = { 25L, 27L, 28L, 31L, 33L, 37L };
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                MetaState meta;
                var sim = NewTetheredSim(out meta);
                meta.BankedSalvage = 1000;
                sim.SetSiphonLevel(level);
                sim.TriggerDraw();
                Kill(sim, 4);
                long before = meta.BankedSalvage;
                Assert.AreEqual(1000L + 4L * bankedPerKill[level], before, "level " + level + " earnings");

                sim.NotifyPlayerDown();
                Assert.AreEqual(before, meta.BankedSalvage, "death took currency at level " + level);
            }
        }

        [Test]
        public void PlayerDown_NoOpWhenDocked()
        {
            var meta = new MetaState();
            var sim = new RunSim(meta, 4242);
            sim.StartRun();
            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Docked, sim.Phase);
            Assert.AreEqual(0, meta.PodEjections);
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void PlayerDown_NoOpWhenAlreadyLost_NeverDoubleCounts()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.TriggerDraw();
            Kill(sim, 2);
            sim.NotifyPlayerDown();
            long banked = meta.BankedSalvage;

            sim.NotifyPlayerDown();
            Assert.AreEqual(banked, meta.BankedSalvage);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
        }

        [Test]
        public void HatchDeath_ReportsCauseHatch()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.TriggerDraw();
            sim.Ship.Hatch.Damage(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual("hatch", sim.LastLossReport.cause);
        }

        [Test]
        public void PlayerDown_AfterHatchAlreadyFell_DoesNotCountAgain()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.TriggerDraw();
            Kill(sim, 2);
            sim.Ship.Hatch.Damage(99999f);
            sim.NotifyPlayerDown();
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("hatch", sim.LastLossReport.cause);
        }

        [Test]
        public void Parity_HatchDeath_And_PlayerDeath_IdenticalEconomics()
        {
            MetaState metaHatch, metaPlayer;
            var simHatch = NewTetheredSim(out metaHatch);
            var simPlayer = NewTetheredSim(out metaPlayer);
            foreach (var sim in new[] { simHatch, simPlayer })
            {
                sim.SetSiphonLevel(2);
                sim.TriggerDraw();
                Kill(sim, 3);
            }

            simHatch.Ship.Hatch.Damage(99999f);
            simPlayer.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Lost, simHatch.Phase);
            Assert.AreEqual(RunPhase.Lost, simPlayer.Phase);
            Assert.AreEqual(metaHatch.BankedSalvage, metaPlayer.BankedSalvage, "same bank");
            Assert.AreEqual(84L, metaPlayer.BankedSalvage); // 3 * floor(28.75) at dial 2
            Assert.AreEqual(simHatch.LastLossReport.grossEarned, simPlayer.LastLossReport.grossEarned);
            Assert.AreEqual(84L, simPlayer.LastLossReport.grossEarned, "gross equals banked awards (M22)");
            Assert.AreEqual(metaHatch.PodEjections, metaPlayer.PodEjections);
            Assert.AreEqual("hatch", simHatch.LastLossReport.cause);
            Assert.AreEqual("player", simPlayer.LastLossReport.cause);
        }

        // ---- CreateNew: zero banked, free MK-1 ----

        [Test]
        public void CreateNew_StartsAtZero_OwnsNothing()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(0L, m.BankedSalvage);
            Assert.IsFalse(m.OwnsAnyWeapon);
            Assert.AreEqual(WeaponId.None, m.EquippedSecondary);
            Assert.AreEqual(0, m.TotalRuns);
            Assert.AreEqual(0, m.PodEjections);
            Assert.AreEqual(SaveData.CurrentVersion, m.SaveVersion);
        }

        [Test]
        public void CreateNew_CanTakeFreeMk1_CannotAffordAPrimary()
        {
            var m = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.CanBuy(m, 0), "0 banked cannot touch any 200-cost track");
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost);
            Assert.GreaterOrEqual(m.BankedSalvage, 0L);
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.SetEquipped(WeaponId.Mk1Sidearm);
            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(0L, m.BankedSalvage, "free purchase leaves the bank untouched");
            Assert.IsFalse(m.BankedSalvage >= WeaponCatalog.Def(WeaponId.Br12Shotgun).cost);
        }

        [Test]
        public void WeaponTier_Costs_Exact()
        {
            Assert.AreEqual(400L, WeaponTier.Cost(0));
            Assert.AreEqual(600L, WeaponTier.Cost(1));
            Assert.AreEqual(1600L, WeaponTier.Cost(6));
            Assert.AreEqual(13000L, WeaponTier.SpentToReach(WeaponTier.MaxTier));
        }

        [Test]
        public void WeaponTier_BuyPath_DeductsAndIncrements()
        {
            var m = new MetaState();
            m.EarnSalvage(400);
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            Assert.IsTrue(WeaponTier.CanBuy(m, 0));
            Assert.IsTrue(m.SpendSalvage(WeaponTier.Cost(0)));
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 1);
            Assert.AreEqual(1, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(0L, m.BankedSalvage);
        }

        // ---- Upgrade gate: locked until the first death ----

        [Test]
        public void UpgradesUnlocked_FalseAtZeroDeaths_TrueFromFirstDeath()
        {
            var m = new MetaState();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m), "fresh save: gate closed");
            m.PodEjections = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "first death opens the shop");
            m.PodEjections = 7;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "stays open");
        }

        [Test]
        public void UpgradesUnlocked_OpensViaPlayerDownPath()
        {
            var meta = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));
            var sim = new RunSim(meta, 7);
            sim.StartRun();
            sim.EngageTether();
            sim.NotifyPlayerDown();
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta));
        }

        // ---- Persistence ----

        [Test]
        public void Json_RoundTrip_PreservesWeaponOwnershipAndTiers()
        {
            var m = new MetaState();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 3);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            var loaded = MetaState.FromJson(m.ToJson());
            Assert.IsTrue(loaded.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(3, loaded.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponId.Mk1Sidearm, loaded.EquippedSecondary);

            var m2 = new MetaState();
            var loaded2 = MetaState.FromJson(m2.ToJson());
            Assert.IsFalse(loaded2.OwnsAnyWeapon);
        }

        [Test]
        public void Json_OldSaveWithoutWeaponFields_OwnsNothing()
        {
            var loaded = MetaState.FromJson("{\"SaveVersion\":1,\"BankedSalvage\":5000,\"TotalRuns\":9}");
            Assert.IsFalse(loaded.OwnsAnyWeapon);
            Assert.AreEqual(5000L, loaded.BankedSalvage);
        }

        [Test]
        public void SaveData_RoundTrip_PreservesWeapons()
        {
            var m = new MetaState();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, WeaponTier.MaxTier);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            var restored = MetaState.FromSaveData(m.ToSaveData());
            Assert.IsTrue(restored.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponTier.MaxTier, restored.TierOf(WeaponId.Mk1Sidearm));

            var fresh = MetaState.FromSaveData(new SaveData());
            Assert.IsFalse(fresh.OwnsAnyWeapon);
        }
    }
}
