using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>
    /// EditMode tests for the RUN-COMPLETE ending and the flat kill-banked economy.
    ///
    /// Clearing all WaveDirector.MaxWaves waves is a success and death is a failure, but they
    /// are worth EXACTLY THE SAME MONEY: currency is credited the instant an enemy dies, so a
    /// run ending is pure bookkeeping — CleanExits vs PodEjections, Escaped vs Lost, and a
    /// LossReport naming the cause. Nothing is staked, fractioned, or forfeited anywhere.
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

        /// <summary>Runs the whole three-wave gauntlet, killing every gatekeeper the director asks
        /// for at the given dial setting, and clearing each wave (so the run COMPLETES).</summary>
        private static void ClearWholeRun(RunSim sim, int siphonLevel)
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

        // ================================================================
        //  THE HEADLINE: winning and dying pay the same
        // ================================================================

        [Test]
        public void SameKills_SameBank_WhetherTheRunEndsInDeathOrInVictory()
        {
            // THE central promise of the economy: the run's ending has NO effect on money.
            // Two identical runs, kill for kill — 18+30+42+54+66 = 210 gatekeepers at 25 each.
            // One player clears the last wave and walks out; the other is killed by the last
            // gatekeeper of the last wave, after the same 210 kills. Both keep 5250.
            var director = new WaveDirector();

            MetaState metaVictory;
            var victory = NewTetheredSim(out metaVictory);
            MetaState metaDeath;
            var death = NewTetheredSim(out metaDeath);

            // Identical kill sequence in both sims.
            foreach (var sim in new[] { victory, death })
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                {
                    sim.TriggerDraw();
                    Kill(sim, director.NextWave(w, 0).enemyCount);
                    // The last wave is where the two runs diverge; clear the earlier ones.
                    if (w < WaveDirector.MaxWaves - 1) sim.NotifyAssaultCleared();
                }

            Assert.AreEqual(5250L, metaVictory.BankedSalvage, "banked during the fight, before either ending");
            Assert.AreEqual(5250L, metaDeath.BankedSalvage);

            victory.NotifyAssaultCleared(); // last wave down → run COMPLETE
            death.NotifyPlayerDown();       // killed on the last wave after the same 210 kills

            // Different endings...
            Assert.AreEqual(RunPhase.Escaped, victory.Phase);
            Assert.AreEqual(RunPhase.Lost, death.Phase);
            Assert.AreEqual("cleared", victory.LastLossReport.cause);
            Assert.AreEqual("player", death.LastLossReport.cause);
            Assert.AreEqual(1, metaVictory.CleanExits);
            Assert.AreEqual(0, metaVictory.PodEjections);
            Assert.AreEqual(0, metaDeath.CleanExits);
            Assert.AreEqual(1, metaDeath.PodEjections);

            // ...IDENTICAL money.
            Assert.AreEqual(metaVictory.BankedSalvage, metaDeath.BankedSalvage,
                "the ending must not move a single credit");
            Assert.AreEqual(5250L, metaVictory.BankedSalvage);
            Assert.AreEqual(5250L, metaDeath.BankedSalvage);
            Assert.AreEqual(victory.LastLossReport.grossEarned, death.LastLossReport.grossEarned);
            Assert.AreEqual(5250L, death.LastLossReport.grossEarned);
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
                long[] bankedPerKill = { 25L, 27L, 28L, 31L, 33L, 37L };
                int kills = 0;
                for (int w = 0; w < WaveDirector.MaxWaves; w++) kills += director.NextWave(w, level).enemyCount;
                Assert.AreEqual(new[] { 210, 231, 242, 265, 284, 315 }[level], kills,
                    "whole-run gatekeepers at dial " + level);
                long expected = kills * bankedPerKill[level];

                Assert.AreEqual(expected, metaVictory.BankedSalvage, "victory at siphon " + level);
                Assert.AreEqual(expected, metaPlayerDeath.BankedSalvage, "player death at siphon " + level);
                Assert.AreEqual(expected, metaHatchDeath.BankedSalvage, "hatch death at siphon " + level);
            }
        }

        // ================================================================
        //  The completion ending itself
        // ================================================================

        [Test]
        public void ClearingAllWaves_CompletesTheRun()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearWholeRun(sim, 0); // 210 kills at 25 = 5250

            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.IsTrue(sim.AllWavesCleared);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
            Assert.AreEqual(5250L, meta.BankedSalvage, "every kill's worth, banked as it happened");
            Assert.AreEqual(5250L, sim.Salvage.RunSalvage, "the tally records what the run earned");
            Assert.AreEqual(1, meta.CleanExits);
            Assert.AreEqual(0, meta.PodEjections, "completing a run is not a pod ejection");
            Assert.AreEqual(5, meta.DeepestWaveReached);
        }

        [Test]
        public void RunComplete_ReportIsClearedAndNamesTheTake()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearWholeRun(sim, 0);

            Assert.AreEqual("cleared", sim.LastLossReport.cause);
            Assert.AreEqual(5250L, sim.LastLossReport.grossEarned);
        }

        [Test]
        public void RunComplete_AtSiphonThree_BanksTheFullMultipliedTake()
        {
            // 265 gatekeepers at dial 3 (x1.25 bodies) at 25 * 1.25 = 31.25 each (x1.25 money) —
            // the dial buys both sides off one table. AwardKill floors once per kill and gives
            // that integer to both the bank and the run tally (M22), so they agree: 265 x 31 = 8215.
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearWholeRun(sim, 3);

            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(8215L, meta.BankedSalvage);   // 265 kills x floor(31.25)
            Assert.AreEqual(8215L, sim.Salvage.RunSalvage);
            Assert.AreEqual(8215L, sim.LastLossReport.grossEarned);
            Assert.AreEqual(1, meta.CleanExits);
        }

        [Test]
        public void RunComplete_PhaseChangedFires_Escaped_AfterTheFinalAssaultEnded()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            var phases = new System.Collections.Generic.List<RunPhase>();
            int assaultsEnded = 0;
            sim.PhaseChanged += p => phases.Add(p);
            sim.AssaultEnded += () => assaultsEnded++;
            ClearWholeRun(sim, 0);

            Assert.AreEqual(WaveDirector.MaxWaves, assaultsEnded, "one per wave, including the last");
            Assert.AreEqual(RunPhase.Escaped, phases[phases.Count - 1]);
        }

        [Test]
        public void EscapedIsTerminal_ExactlyLikeLost()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearWholeRun(sim, 0);
            long banked = meta.BankedSalvage;

            for (int i = 0; i < 40; i++) sim.Tick(0.5f); // ticks are inert
            sim.TriggerDraw();                            // no wave past MaxWaves
            sim.AwardKill();                              // nothing left to kill
            sim.NotifyAssaultCleared();                   // no live assault
            sim.NotifyPlayerDown();                       // cannot die after completing
            sim.Ship.Hatch.Damage(99999f);                // the hatch no longer matters

            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
            Assert.AreEqual(banked, meta.BankedSalvage, "a post-run kill cannot pay");
            Assert.AreEqual(5250L, meta.BankedSalvage);
            Assert.AreEqual(5250L, sim.Salvage.RunSalvage);
            Assert.AreEqual(1, meta.CleanExits);
            Assert.AreEqual(0, meta.PodEjections);
            Assert.AreEqual("cleared", sim.LastLossReport.cause);
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
            Kill(sim, 3);       // 18+30+42+54+3 = 147 kills at 25 = 3675 salvage
            Assert.AreEqual(3675L, sim.Salvage.RunSalvage);
            Assert.AreEqual(3675L, meta.BankedSalvage, "already banked, mid-wave");

            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(sim.AllWavesCleared, "dying on the last wave clears nothing");
            Assert.AreEqual(3675L, meta.BankedSalvage, "and forfeits nothing");
            Assert.AreEqual(3675L, sim.LastLossReport.grossEarned);
            Assert.AreEqual("player", sim.LastLossReport.cause);
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
            Assert.AreEqual("hatch", sim.LastLossReport.cause);
            // Waves 1..4 cleared (18+30+42+54=144), died on wave 5 before any kill.
            Assert.AreEqual(3600L, meta.BankedSalvage, "144 kills * 25, kept in full");
            Assert.AreEqual(3600L, sim.LastLossReport.grossEarned);
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
            Assert.AreEqual("player", sim.LastLossReport.cause);
            Assert.AreEqual(5250L, meta.BankedSalvage, "all 210 kills, exactly as if it had completed");
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
            ClearWholeRun(run1, 0);
            Assert.AreEqual(5250L, run1.Salvage.RunSalvage);
            Assert.AreEqual(5250L, meta.BankedSalvage);

            // Run 2 over the SAME meta: fresh tally, same bank, and it keeps growing.
            var run2 = new RunSim(meta, 2);
            run2.StartRun();
            run2.EngageTether();
            Assert.AreEqual(0L, run2.Salvage.RunSalvage, "the per-run tally starts at zero");
            Assert.AreEqual(5250L, meta.BankedSalvage, "the bank carries over untouched");

            run2.TriggerDraw();
            Kill(run2, 6);
            Assert.AreEqual(150L, run2.Salvage.RunSalvage, "run 2's own tally");
            Assert.AreEqual(5400L, meta.BankedSalvage, "5250 carried + 150 earned");

            run2.NotifyPlayerDown(); // and dying in run 2 keeps run 1's earnings too
            Assert.AreEqual(5400L, meta.BankedSalvage);
            Assert.AreEqual(150L, run2.LastLossReport.grossEarned, "earned is per-run, not cumulative");
            Assert.AreEqual(2, meta.TotalRuns);
        }

        [Test]
        public void RunComplete_CleanExitsPersistThroughJson()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearWholeRun(sim, 0);

            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(1, loaded.CleanExits);
            Assert.AreEqual(0, loaded.PodEjections);
            Assert.AreEqual(5250L, loaded.BankedSalvage);
        }

        [Test]
        public void RunComplete_WithZeroKills_BanksNothingButStillCounts()
        {
            // The game layer can clear a wave without RunSim seeing kills (scripted despawn,
            // enemies walking off). That is a completed run worth exactly nothing — clearing
            // waves is not itself paid, only kills are.
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                sim.NotifyAssaultCleared();
            }
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.LastLossReport.grossEarned);
            Assert.AreEqual(1, meta.CleanExits);
        }
    }
}
