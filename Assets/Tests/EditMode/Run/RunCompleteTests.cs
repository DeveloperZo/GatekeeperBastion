using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// EditMode tests for the RUN-COMPLETE ending.
    ///
    /// Field Wave 5 keeps Unbanked in Prep. Hatch-bank after that converts and stays Prep.
    /// Death wipes leftover Unbanked. Converted ship bank is kept either way.
    /// </summary>
    public class RunCompleteTests
    {
        private static RunSim NewTetheredSim(out MetaState meta)
        {
            meta = new MetaState();
            var sim = new RunSim(meta, seed: 31337);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        private static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        /// <summary>Five field Waves. Leaves Unbanked in Prep when kills happened.</summary>
        private static void ClearFieldWaves(RunSim sim, int siphonLevel)
        {
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.SetSiphonLevel(siphonLevel);
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, siphonLevel).enemyCount);
                sim.NotifyAssaultCleared();
            }
        }

        private static void HatchBankDump(RunSim sim)
        {
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
        }

        // ================================================================
        //  THE HEADLINE: winning and dying pay the same
        // ================================================================

        [Test]
        public void Wave5Clear_KeepsUnbanked_DeathWipes()
        {
            var director = new WaveDirector();

            MetaState metaVictory;
            var victory = NewTetheredSim(out metaVictory);
            MetaState metaDeath;
            var death = NewTetheredSim(out metaDeath);

            foreach (var sim in new[] { victory, death })
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                {
                    sim.TriggerDraw();
                    Kill(sim, director.NextWave(w, 0).enemyCount);
                    if (w < WaveDirector.MaxWaves - 1) sim.NotifyAssaultCleared();
                }

            Assert.AreEqual(0L, metaVictory.BankedSalvage, "kills stay unbanked until Hatch-bank");
            Assert.AreEqual(0L, metaDeath.BankedSalvage);
            Assert.AreEqual(5250L, victory.Unbanked);
            Assert.AreEqual(5250L, death.Unbanked);

            victory.NotifyAssaultCleared();
            death.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Tethered, victory.Phase);
            Assert.AreEqual(AssaultState.Prep, victory.Assault);
            Assert.AreEqual(5250L, victory.Unbanked);
            Assert.AreEqual(0L, metaVictory.BankedSalvage);
            Assert.AreEqual(0, metaVictory.CleanExits);
            Assert.AreEqual(RunPhase.Lost, death.Phase);
            Assert.AreEqual("player", death.LastRunReport.cause);
            Assert.AreEqual(0, metaDeath.CleanExits);
            Assert.AreEqual(1, metaDeath.PodEjections);
            Assert.AreEqual(0L, metaDeath.BankedSalvage);
            Assert.AreEqual(0L, death.Unbanked);
        }

        [Test]
        public void SameKills_SameBank_AlsoWhenTheHatchFalls_AndAtEverySiphonLevel()
        {
            // The parity above is not a siphon-0 accident, and hatch death is no different.
            var director = new WaveDirector();
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                MetaState metaVictory, metaPlayerDeath, metaHatchDeath;
                var victory = NewTetheredSim(out metaVictory);
                var playerDeath = NewTetheredSim(out metaPlayerDeath);
                var hatchDeath = NewTetheredSim(out metaHatchDeath);

                foreach (var sim in new[] { victory, playerDeath, hatchDeath })
                    for (int w = 0; w < WaveDirector.MaxWaves; w++)
                    {
                        sim.SetSiphonLevel(level);
                        sim.TriggerDraw();
                        Kill(sim, director.NextWave(w, level).enemyCount);
                        if (w < WaveDirector.MaxWaves - 1) sim.NotifyAssaultCleared();
                    }

                victory.NotifyAssaultCleared();
                playerDeath.NotifyPlayerDown();
                hatchDeath.Ship.Hatch.Damage(99999f);

                // Total kills = 5 waves of round((18 + 12w) * dial) — 210/231/242/265/284/315 across
                // the dial — each banking floor(25 * dial) = 25/27/28/31/33/37.
                int kills = 0;
                for (int w = 0; w < WaveDirector.MaxWaves; w++) kills += director.NextWave(w, level).enemyCount;
                Assert.AreEqual(new[] { 210, 231, 242, 265, 284, 315 }[level], kills,
                    "whole-run gatekeepers at dial " + level);

                Assert.AreEqual(0L, metaVictory.BankedSalvage, "victory at siphon " + level);
                Assert.AreEqual(0L, metaPlayerDeath.BankedSalvage, "player death at siphon " + level);
                Assert.AreEqual(0L, metaHatchDeath.BankedSalvage, "hatch death at siphon " + level);
            }
        }

        // ================================================================
        //  The completion ending itself
        // ================================================================

        [Test]
        public void ClearingAllWaves_LeavesUnbankedUntilHatchBank()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearFieldWaves(sim, 0); // 210 kills at 25 = 5250

            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
            Assert.AreEqual(5250L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage, "terminal Wave 5 does not convert leftover");
            Assert.AreEqual(0, meta.CleanExits);
            Assert.AreEqual(5, meta.DeepestWaveReached);

            HatchBankDump(sim);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(HatchBank.BankedFrom(5250L, HatchBank.CarryConvert(5)), meta.BankedSalvage);
            Assert.AreEqual(0, meta.CleanExits);
            Assert.AreEqual(0, meta.PodEjections, "hatch-bank after Wave 5 is not a run end");
        }

        [Test]
        public void RunComplete_ReportIsClearedAndNamesTheTake()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            MasterControlTests.SurviveSequence(sim, 0);

            Assert.AreEqual("cleared", sim.LastRunReport.cause);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
        }

        [Test]
        public void RunComplete_AtSiphonThree_BanksTheFullMultipliedTake()
        {
            // 265 gatekeepers at dial 3 (x1.25 bodies) at 25 * 1.25 = 31.25 each (x1.25 money) —
            // AwardKill floors once per kill: 265 x 31 = 8215 unbanked. Five field Waves carry
            // converts at ×1.50.
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearFieldWaves(sim, 3);
            long pile = sim.Unbanked;
            Assert.AreEqual(8215L, pile);
            HatchBankDump(sim);

            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            long converted = HatchBank.BankedFrom(pile, HatchBank.CarryConvert(5));
            Assert.AreEqual(converted, meta.BankedSalvage);
            Assert.AreEqual(converted, sim.Salvage.RunSalvage);
            Assert.AreEqual(0, meta.CleanExits);
        }

        [Test]
        public void HatchBankAfterField_DoesNotEscape()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            var phases = new System.Collections.Generic.List<RunPhase>();
            int assaultsEnded = 0;
            sim.PhaseChanged += p => phases.Add(p);
            sim.AssaultEnded += () => assaultsEnded++;
            ClearFieldWaves(sim, 0);
            HatchBankDump(sim);

            Assert.AreEqual(WaveDirector.MaxWaves + 1, assaultsEnded,
                "five field Waves plus the Hatch-bank");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.IsFalse(phases.Contains(RunPhase.Escaped));
        }

        [Test]
        public void EscapedIsTerminal_ExactlyLikeLost()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            MasterControlTests.SurviveSequence(sim, 0);
            long banked = meta.BankedSalvage;

            for (int i = 0; i < 40; i++) sim.Tick(0.5f);
            sim.TriggerDraw();
            sim.AwardKill();
            sim.NotifyAssaultCleared();
            sim.NotifyPlayerDown();
            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
            Assert.AreEqual(banked, meta.BankedSalvage, "a post-run kill cannot pay");
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);
            Assert.AreEqual(1, meta.CleanExits);
            Assert.AreEqual(0, meta.PodEjections);
            Assert.AreEqual("cleared", sim.LastRunReport.cause);
        }

        // ================================================================
        //  Dying on the final wave
        // ================================================================

        [Test]
        public void DeathOnTheFinalWave_IsAPod_ButKeepsEveryCreditEarned()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves - 1; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }
            sim.TriggerDraw();  // final wave
            Kill(sim, 3);       // 18+30+42+54+3 = 147 kills at 25 = 3675 unbanked
            Assert.AreEqual(3675L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);

            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(sim.AllWavesCleared, "dying on the last wave clears nothing");
            Assert.AreEqual(0L, meta.BankedSalvage, "never converted");
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
            Assert.AreEqual("player", sim.LastRunReport.cause);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual(0, meta.CleanExits);
        }

        [Test]
        public void HatchDeath_OnTheFinalWave_IsStillAPod()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves - 1; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }
            sim.TriggerDraw();
            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual("hatch", sim.LastRunReport.cause);
            // Waves 1..4 cleared (18+30+42+54=144), died on wave 5 before any kill.
            Assert.AreEqual(0L, meta.BankedSalvage, "never converted");
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual(0, meta.CleanExits);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(4, meta.DeepestWaveReached, "cleared through wave 4 only");
        }

        [Test]
        public void ARunLostInsideTheFinalAssaultEndedHandler_DoesNotAlsoComplete()
        {
            // AssaultEnded fires before the run completes, so a listener can still end the run.
            // A dead run must never also count a clean exit — and either way the money is the same.
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            var director = new WaveDirector();
            sim.AssaultEnded += () =>
            {
                if (sim.CurrentWaveIndex == WaveDirector.MaxWaves) sim.NotifyPlayerDown();
            };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                Kill(sim, director.NextWave(w, 0).enemyCount);
                sim.NotifyAssaultCleared();
            }

            Assert.AreEqual(RunPhase.Lost, sim.Phase, "the death wins the ending");
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(0, meta.CleanExits);
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("player", sim.LastRunReport.cause);
            Assert.AreEqual(0L, meta.BankedSalvage, "never converted");
        }

        // ================================================================
        //  Run boundaries: the tally resets, the bank does not
        // ================================================================

        [Test]
        public void NewRun_ResetsRunSalvage_ButBankedSalvageCarriesOverAndAccumulates()
        {
            var meta = new MetaState();

            var run1 = new RunSim(meta, 1);
            run1.StartRun();
            run1.EngageTether();
            ClearFieldWaves(run1, 0);
            Assert.AreEqual(0L, run1.Salvage.RunSalvage);
            Assert.AreEqual(0L, meta.BankedSalvage);

            // Run 2 over the SAME meta: fresh tally, same (empty) bank.
            var run2 = new RunSim(meta, 2);
            run2.StartRun();
            run2.EngageTether();
            Assert.AreEqual(0L, run2.Salvage.RunSalvage, "the per-run tally starts at zero");
            Assert.AreEqual(0L, meta.BankedSalvage, "terminal clears convert nothing");

            run2.TriggerDraw();
            Kill(run2, 6);
            Assert.AreEqual(150L, run2.Unbanked, "run 2's own pile");
            Assert.AreEqual(0L, meta.BankedSalvage);

            run2.NotifyPlayerDown();
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, run2.LastRunReport.grossEarned, "earned is converted, not kill awards");
            Assert.AreEqual(2, meta.TotalRuns);
        }

        [Test]
        public void RunComplete_CleanExitsPersistThroughJson()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            MasterControlTests.SurviveSequence(sim, 0);

            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(1, loaded.CleanExits);
            Assert.AreEqual(0, loaded.PodEjections);
            Assert.AreEqual(0L, loaded.BankedSalvage);
        }

        [Test]
        public void RunComplete_WithZeroKills_BanksNothingButStillCounts()
        {
            // The game layer can clear a wave without RunSim seeing kills (scripted despawn,
            // enemies walking off). That is a completed run worth exactly nothing — clearing
            // waves is not itself paid, only kills are.
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            MasterControlTests.SurviveSequence(sim, 0);
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
            Assert.AreEqual(1, meta.CleanExits);
        }
    }
}
