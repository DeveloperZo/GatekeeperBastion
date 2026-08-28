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

        /// <summary>What the bank sees for <paramref name="kills"/> bodies at
        /// <paramref name="siphonLevel"/>, floored per body as AwardKill does. DERIVED, because
        /// SalvagePerKill moved 25 -> 42 on 2026-08-27 and every figure in this fixture is a
        /// multiple of it — including a six-entry per-level table that was written out by
        /// hand.</summary>
        private static long Banked(int siphonLevel, int kills) =>
            kills * (long)(MetaUpgrades.SalvagePerKill()
                           * TetherSiphon.RewardMultiplierFor(siphonLevel));

        // ---- NotifyPlayerDown: the death path (bookkeeping only) ----

        [Test]
        public void PlayerDown_MidAssault_Pods_AndLosesNothing()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            sim.TriggerDraw();
            Kill(sim, 2);
            // Re-keyed 2026-08-25 (Zo): the name was always the claim, and now it is literal.
            // Two kills used to leave their pay in an at-risk carry that this very death then
            // wiped; they bank on landing, so the run that ends here ends with something to show.
            Assert.AreEqual(Banked(0, 2), meta.BankedSalvage, "the kills banked as they landed");
            Assert.AreEqual(0L, sim.Unbanked, "and nothing was carried into the death");

            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked, "down wipes unbanked");
            Assert.AreEqual(Banked(0, 2), meta.BankedSalvage,
                "death cannot reach the bank behind it");
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("player", sim.LastRunReport.cause);
            Assert.AreEqual(Banked(0, 2), sim.LastRunReport.grossEarned);
            Assert.Greater(sim.LastRunReport.grossEarned, 0L,
                "a run with kills in it must never report nothing - that is the whole ruling");
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
            // The per-level table used to be written out here as six literals — 25 / 27 / 28 /
            // 31 / 33 / 37 — and SalvagePerKill 25 -> 42 turned every one of them red. It is
            // derived now; RunSimTests owns the pin on the curve itself.
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                MetaState meta;
                var sim = NewTetheredSim(out meta);
                meta.BankedSalvage = 1000;
                sim.SetSiphonLevel(level);
                sim.TriggerDraw();
                Kill(sim, 4);
                long before = meta.BankedSalvage;
                // Re-keyed 2026-08-25: kills now RAISE this number instead of leaving it be,
                // which only sharpens the pin — the bank is what the death is measured
                // against, so it has to be a figure the run actually put there.
                Assert.AreEqual(1000L + Banked(level, 4), before,
                    "level " + level + " kills banked at face value");
                Assert.AreEqual(0L, sim.Unbanked, "level " + level + " carried nothing");

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
            // Re-keyed 2026-08-25: three kills at dial 2 bank a real figure now (84 then, 144
            // since the 2026-08-27 price raise), so parity is a
            // shared non-zero figure rather than a shared nothing — which is the stronger
            // reading, since two ways of dying that both banked nothing would look identical
            // however differently they were bookkept.
            Assert.AreEqual(Banked(2, 3), metaPlayer.BankedSalvage);
            Assert.AreEqual(simHatch.LastRunReport.grossEarned, simPlayer.LastRunReport.grossEarned);
            Assert.AreEqual(Banked(2, 3), simPlayer.LastRunReport.grossEarned,
                "both banked what they killed and neither converted anything on top");
            Assert.Greater(Banked(2, 3), 0L,
                "parity between two ways of banking NOTHING would prove nothing");
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
        public void UpgradesUnlocked_FalseOnAFreshRealm_TrueFromTheFirstSalvageEarned()
        {
            // Re-keyed 2026-08-25: the gate was HatchConversions, raised only by a Convert
            // window, and the Convert is out of the demo. Kills bank as they land, so the
            // question is simply whether this realm has ever earned anything.
            var m = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m), "fresh save: gate closed");
            m.PodEjections = 7;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "losses alone open nothing — a player who banked nothing has nothing to spend");
            m.HatchConversions = 7;
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(m),
                "Convert windows are no longer the key — a counter alone must not open it");
            m.EarnSalvage(25L);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m), "the first kill opens the shop");
            m.SpendSalvage(25L);
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(m),
                "spending back to zero must not re-lock the wheel — the figure is lifetime");
        }

        [Test]
        public void UpgradesUnlocked_OpensOnTheFirstBankedKill_NotThePlayerDownPath()
        {
            // Re-keyed 2026-08-25: the sim-level half of the gate. It used to have to reach a
            // Convert window to open, which is the reachability problem Zo's rule change was
            // about — a starting kit dies before it gets there. One kill now opens it, and the
            // pin is kept at the sim level so the wiring from AwardKill to LifetimeSalvage-
            // Earned stays covered, not just the MetaState predicate above.
            var meta = MetaState.CreateNew();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta));
            var sim = new RunSim(meta, 7);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.IsFalse(MetaUpgrades.UpgradesUnlocked(meta),
                "drawing a wave is not earning anything");
            sim.AwardKill();
            Assert.IsTrue(MetaUpgrades.UpgradesUnlocked(meta),
                "the first body through the Hatch is the unlock");
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
