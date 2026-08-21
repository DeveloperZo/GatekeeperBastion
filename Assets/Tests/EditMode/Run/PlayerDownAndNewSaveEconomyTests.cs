using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// EditMode tests for the death-only run-end model and the new-save economy:
    /// NotifyPlayerDown, CreateNew at 0 banked, free MK-1 ownership, WeaponTier costs,
    /// the first-lost-run upgrade gate, and persistence of weapon fields.
    /// </summary>
    public class PlayerDownAndNewSaveEconomyTests
    {
        private static RunSim NewTetheredSim(out MetaState meta)
        {
            meta = MetaState.CreateNew();
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
            Assert.AreEqual(50L, sim.Unbanked, "unbanked while still alive");
            Assert.AreEqual(0L, meta.BankedSalvage);

            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked, "down wipes unbanked");
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("player", sim.LastRunReport.cause);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
        }

        [Test]
        public void PlayerDown_DuringPrep_AlsoEndsTheRun()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("player", sim.LastRunReport.cause);
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
                Assert.AreEqual(1000L, before, "level " + level + " ship bank unchanged by kills");
                Assert.AreEqual(4L * bankedPerKill[level], sim.Unbanked, "level " + level + " unbanked");

                sim.NotifyPlayerDown();
                Assert.AreEqual(before, meta.BankedSalvage, "death took currency at level " + level);
                Assert.AreEqual(0L, sim.Unbanked, "unbanked wiped at level " + level);
            }
        }

        [Test]
        public void PlayerDown_NoOpWhenDocked()
        {
            var meta = MetaState.CreateNew();
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
            Assert.AreEqual("hatch", sim.LastRunReport.cause);
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
            Assert.AreEqual("hatch", sim.LastRunReport.cause);
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
            Assert.AreEqual(0L, metaPlayer.BankedSalvage);
            Assert.AreEqual(simHatch.LastRunReport.grossEarned, simPlayer.LastRunReport.grossEarned);
            Assert.AreEqual(0L, simPlayer.LastRunReport.grossEarned, "neither converted");
            Assert.AreEqual(metaHatch.PodEjections, metaPlayer.PodEjections);
            Assert.AreEqual("hatch", simHatch.LastRunReport.cause);
            Assert.AreEqual("player", simPlayer.LastRunReport.cause);
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
            Assert.AreEqual(SaveFormat.CurrentVersion, m.SaveVersion);
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
            var m = MetaState.CreateNew();
            m.EarnSalvage(400);
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            Assert.IsTrue(WeaponTier.CanBuy(m, 0));
            Assert.IsTrue(m.SpendSalvage(WeaponTier.Cost(0)));
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 1);
            Assert.AreEqual(1, m.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(0L, m.BankedSalvage);
        }

        // ---- Upgrade gate: locked until salvage first converts through the Hatch ----
        // Re-keyed 2026-08-18 (Zo): the loss gate never fired for a player who banks and
        // docks. Losing still opens the shop in practice, because the authored losses drip
        // salvage home first — the DEATH is no longer the key, the CONVERT is.

        [Test]
        public void UpgradesUnlocked_FalseAtZeroConversions_TrueFromTheFirstBank()
        {
            var m = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m), "fresh save: gate closed");
            m.PodEjections = 7;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "losses alone open nothing — a player who banked nothing has nothing to spend");
            m.HatchConversions = 1;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "the first bank opens the shop");
            m.HatchConversions = 7;
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "stays open");
        }

        [Test]
        public void UpgradesUnlocked_OpensViaTheConvertPath_NotThePlayerDownPath()
        {
            var meta = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));
            var sim = new RunSim(meta, 7);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            for (int i = 0; i < 24; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(2f); // a few drip units convert
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "salvage through the Hatch is the unlock");
            sim.NotifyPlayerDown();
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "the loss that follows changes nothing about it");
        }

        // ---- Persistence ----

        [Test]
        public void Json_RoundTrip_PreservesWeaponOwnershipAndTiers()
        {
            var m = MetaState.CreateNew();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, 3);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            var loaded = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.IsTrue(loaded.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(3, loaded.TierOf(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponId.Mk1Sidearm, loaded.EquippedSecondary);

            var m2 = MetaState.CreateNew();
            var loaded2 = MetaState.FromSnapshotJson(m2.ToSnapshotJson());
            Assert.IsFalse(loaded2.OwnsAnyWeapon);
        }

        [Test]
        public void SaveData_RoundTrip_PreservesWeapons()
        {
            var m = MetaState.CreateNew();
            m.SetOwned(WeaponId.Mk1Sidearm, true);
            m.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, WeaponTier.MaxTier);
            m.EquippedSecondary = WeaponId.Mk1Sidearm;
            var restored = MetaState.FromSnapshotJson(m.ToSnapshotJson());
            Assert.IsTrue(restored.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponTier.MaxTier, restored.TierOf(WeaponId.Mk1Sidearm));

            var fresh = MetaState.CreateNew();
            Assert.IsFalse(fresh.OwnsAnyWeapon);
        }
    }
}
