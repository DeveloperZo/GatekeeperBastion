using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// EditMode tests for the RUN-COMPLETE ending.
    ///
    /// Field Wave 5 leaves the run's kills BANKED and returns to Prep. A Hatch-bank after that
    /// is an opt-in bet on money the player already owns: it has to be withdrawn back out at
    /// the Hatch first, and the window converts it at the carry multiplier. Death wipes a
    /// withdrawn pile and nothing behind it — so winning and dying pay the same base, and what
    /// separates them is only what was staked (re-keyed 2026-08-25, Zo).
    /// </summary>
    public class RunCompleteTests
    {
        private static RunSim NewTetheredSim(out MetaState meta)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 31337);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        private static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        /// <summary>Five field Waves. Leaves the whole take BANKED, in Prep, with nothing
        /// staked — pair it with <see cref="StakeEverythingBanked"/> when a pile is wanted.</summary>
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
            StakeEverythingBanked(sim);
            sim.TriggerDraw(WaveMode.HatchBank);
            sim.Tick(HatchBank.DurationSeconds);
        }

        /// <summary>Put the whole bank on the table. These fixtures seed no bank of their own,
        /// so this is exactly what the Run's kills paid — the pile the window used to be
        /// handed for free.</summary>
        /// <summary>The whole-run body count at <paramref name="siphonLevel"/>, and what it
        /// banks. DERIVED — the 2026-08-27 pass moved BOTH factors and in opposite directions:
        /// the roster fell ~40% (210 bodies at dial 0 became 125) and SalvagePerKill rose 25 -> 42
        /// to compensate. That a five-wave dial-0 run still banks EXACTLY 5250 is the arithmetic
        /// the pass was solved for, and it is the one figure in this fixture worth naming.</summary>
        private static int WholeRunKills(int siphonLevel)
        {
            var director = new WaveDirector();
            int kills = 0;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                kills += director.NextWave(w, siphonLevel).enemyCount;
            return kills;
        }

        private static long Banked(int siphonLevel, int kills) =>
            kills * (long)(MetaUpgrades.SalvagePerKill()
                           * TetherSiphon.RewardMultiplierFor(siphonLevel));

        private static long WholeRunTake(int siphonLevel) =>
            Banked(siphonLevel, WholeRunKills(siphonLevel));

        private static void StakeEverythingBanked(RunSim sim)
        {
            long bank = sim.BankedSalvage;
            if (bank > 0L) RunEconomyFixture.Stake(sim, bank);
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

            // Re-key 2026-08-25. The headline used to be "both end with an empty bank" — parity
            // at zero, which is the cheapest kind of parity to hold. It is now parity at 5250:
            // both seats banked every kill as it landed, and the death has nothing to take,
            // because the wipe can only reach a pile the player chose to withdraw. (The method
            // name still says KeepsUnbanked/DeathWipes and no longer describes either half.)
            Assert.AreEqual(WholeRunTake(0), metaVictory.BankedSalvage,
                "every body in the run, banked on landing");
            Assert.AreEqual(WholeRunTake(0), metaDeath.BankedSalvage);

            // AND THE FIGURE ITSELF, named once, because holding it is what the 2026-08-27 pass
            // was solved for: the roster fell 210 bodies -> 125 and SalvagePerKill rose 25 -> 42,
            // and a dial-0 five-wave run pays the same 5250 it paid before either moved. Every
            // price in the shop is denominated in that number.
            Assert.AreEqual(5250L, WholeRunTake(0),
                "the roster cut was priced to hold a five-wave run at exactly 5250 salvage");
            Assert.AreEqual(0L, victory.Unbanked, "neither seat staked anything");
            Assert.AreEqual(0L, death.Unbanked);

            victory.NotifyAssaultCleared();
            death.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Tethered, victory.Phase);
            Assert.AreEqual(AssaultState.Prep, victory.Assault);
            Assert.AreEqual(0L, victory.Unbanked);
            Assert.AreEqual(WholeRunTake(0), metaVictory.BankedSalvage);
            Assert.AreEqual(0, metaVictory.CleanExits);
            Assert.AreEqual(RunPhase.Lost, death.Phase);
            Assert.AreEqual("player", death.LastRunReport.cause);
            Assert.AreEqual(0, metaDeath.CleanExits);
            Assert.AreEqual(1, metaDeath.PodEjections);
            Assert.AreEqual(metaVictory.BankedSalvage, metaDeath.BankedSalvage,
                "the same kills pay the same whether the wave ended in a clear or a body");
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

                // Total kills = 5 waves of round((BaseEnemyCount + CountPerWave*w) * dial), each
                // banking floor(SalvagePerKill * dial).
                //
                // BOTH TABLES USED TO BE WRITTEN OUT HERE — 210/231/242/265/284/315 bodies and
                // 25/27/28/31/33/37 salvage — and the 2026-08-27 pass turned all twelve red in
                // one edit, in opposite directions. They are derived now; SiphonAndWaveTests owns
                // the pin on the body grid and RunSimTests the pin on the salvage curve.
                int kills = 0;
                for (int w = 0; w < WaveDirector.MaxWaves; w++) kills += director.NextWave(w, level).enemyCount;
                Assert.AreEqual(WholeRunKills(level), kills,
                    "whole-run gatekeepers at dial " + level);

                // Re-key 2026-08-25: the three endings used to agree on zero, which any bug
                // that paid nobody would also satisfy. They now have to agree on the exact
                // face-value take, so the parity is checked against a number only a correct
                // AwardKill can produce.
                long take = Banked(level, kills);
                Assert.AreEqual(take, metaVictory.BankedSalvage, "victory at siphon " + level);
                Assert.AreEqual(take, metaPlayerDeath.BankedSalvage, "player death at siphon " + level);
                Assert.AreEqual(take, metaHatchDeath.BankedSalvage, "hatch death at siphon " + level);
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
            ClearFieldWaves(sim, 0); // the whole dial-0 roster

            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(WaveDirector.MaxWaves, sim.CurrentWaveIndex);
            // Re-key 2026-08-25: five cleared waves leave the take banked and nothing carried,
            // so the Hatch-bank below is no longer the toll that decides whether the run paid.
            // (The method name still says LeavesUnbanked; what it leaves is a full bank.)
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(WholeRunTake(0), meta.BankedSalvage,
                "every body in the run, banked as it landed");
            Assert.AreEqual(0, meta.CleanExits);
            Assert.AreEqual(5, meta.DeepestWaveReached);

            HatchBankDump(sim);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(HatchBank.BankedFrom(WholeRunTake(0), HatchBank.CarryConvert(5)),
                meta.BankedSalvage);
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
            // The dial-3 run at x1.25 bodies and x1.25 money, with AwardKill flooring once per
            // kill: 157 gatekeepers at floor(42 x 1.25) = 52 each, so 8164. It read 265 x 31 =
            // 8215 before the 2026-08-27 pass moved both factors. Five field Waves carry, so the
            // window converts at x1.50.
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            ClearFieldWaves(sim, 3);
            // Re-key 2026-08-25: the take reads off the bank now, and HatchBankDump withdraws
            // the whole of it back out before the window — so the pile the multiplier is
            // applied to is the same take, reached by the route the player now takes.
            long pile = sim.BankedSalvage;
            Assert.AreEqual(WholeRunTake(3), pile);
            Assert.Greater(WholeRunTake(3), WholeRunTake(0),
                "the dial has to pay more over a whole run, or nobody would ever turn it up");
            HatchBankDump(sim);

            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            long converted = HatchBank.BankedFrom(pile, HatchBank.CarryConvert(5));
            Assert.AreEqual(converted, meta.BankedSalvage);
            // The tally is the converted figure alone. Staking took the kills' own inflow
            // back out of it (SalvageLedger.Unearn), so the multiplied return is counted
            // once rather than stacked on top of the salvage it was made from.
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
            Kill(sim, 3);       // four cleared waves, plus three on the fifth
            // Re-key 2026-08-25: the method name promised "keeps every credit earned" and, until
            // the ruling, delivered the opposite — the whole take sat unbanked and the death took
            // all of it. It is now literally true, which is what the ruling was for.
            int killsMade = WholeRunKills(0) - director.NextWave(WaveDirector.MaxWaves - 1, 0).enemyCount + 3;
            long earned = Banked(0, killsMade);
            Assert.AreEqual(earned, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);

            sim.NotifyPlayerDown();
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(sim.AllWavesCleared, "dying on the last wave clears nothing");
            Assert.AreEqual(earned, meta.BankedSalvage, "the pod leaves with every credit");
            Assert.AreEqual(earned, sim.LastRunReport.grossEarned);
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
            // Waves 1..4 cleared, died on wave 5 before any kill.
            // Re-key 2026-08-25: a hatch breach is the other loss kind and it too stops at the
            // bank — the four cleared waves are already paid for, and wave 5's zero kills are
            // the only thing this ending costs.
            int cleared = WholeRunKills(0)
                          - director.NextWave(WaveDirector.MaxWaves - 1, 0).enemyCount;
            Assert.AreEqual(Banked(0, cleared), meta.BankedSalvage,
                "the first four waves, kept");
            Assert.AreEqual(Banked(0, cleared), sim.LastRunReport.grossEarned);
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
            // Re-key 2026-08-25: "either way the money is the same" is still the second half of
            // this test's point, and the money is now 5250 rather than nothing — a re-entrant
            // death inside the clear handler must not cost the run its banked kills either.
            Assert.AreEqual(WholeRunTake(0), meta.BankedSalvage,
                "the whole run's take, untouched by the ending");
        }

        // ================================================================
        //  Run boundaries: the tally resets, the bank does not
        // ================================================================

        [Test]
        public void NewRun_ResetsRunSalvage_ButBankedSalvageCarriesOverAndAccumulates()
        {
            var meta = MetaState.CreateNew();

            var run1 = new RunSim(meta, 1);
            run1.StartRun();
            run1.EngageTether();
            ClearFieldWaves(run1, 0);
            // Re-key 2026-08-25: the method name has promised "carries over AND ACCUMULATES"
            // since it was written, and used to demonstrate it on an empty bank in both runs.
            // Kills bank on landing, so there is finally something to carry and accumulate.
            Assert.AreEqual(5250L, run1.Salvage.RunSalvage);
            Assert.AreEqual(WholeRunTake(0), meta.BankedSalvage);

            // Run 2 over the SAME meta: fresh tally, and run 1's bank still standing.
            var run2 = new RunSim(meta, 2);
            run2.StartRun();
            run2.EngageTether();
            Assert.AreEqual(0L, run2.Salvage.RunSalvage, "the per-run tally starts at zero");
            Assert.AreEqual(WholeRunTake(0), meta.BankedSalvage, "the bank is not a per-run figure");

            run2.TriggerDraw();
            Kill(run2, 6);
            long runTwoTake = Banked(0, 6);
            Assert.AreEqual(runTwoTake, run2.Salvage.RunSalvage, "run 2's own tally");
            Assert.AreEqual(0L, run2.Unbanked, "nothing staked");
            Assert.AreEqual(WholeRunTake(0) + runTwoTake, meta.BankedSalvage,
                "and run 2's kills accumulate onto run 1's");

            run2.NotifyPlayerDown();
            Assert.AreEqual(WholeRunTake(0) + runTwoTake, meta.BankedSalvage,
                "a second death cannot undo either run");
            Assert.AreEqual(runTwoTake, run2.LastRunReport.grossEarned,
                "gross is this run's inflow only");
            Assert.AreEqual(2, meta.TotalRuns);
        }

        [Test]
        public void RunComplete_CleanExitsPersistThroughJson()
        {
            MetaState meta;
            var sim = NewTetheredSim(out meta);
            MasterControlTests.SurviveSequence(sim, 0);

            var loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
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
