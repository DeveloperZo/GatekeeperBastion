using System.Collections.Generic;
using NUnit.Framework;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Tests
{
    /// <summary>EditMode tests for the RunSim state machine (Docked → Tethered → Lost),
    /// the Prep/Assault draw model, kill-banked salvage, death as pure bookkeeping, and
    /// MetaState JSON persistence.</summary>
    public class RunSimTests
    {
        private static RunSim NewSim(out MetaState meta)
        {
            meta = new MetaState();
            return new RunSim(meta, seed: 12345);
        }

        private static void TickFor(RunSim sim, float seconds, float dt = 0.5f)
        {
            int steps = (int)(seconds / dt);
            for (int i = 0; i < steps; i++) sim.Tick(dt);
        }

        private static void Kill(RunSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.AwardKill();
        }

        [Test]
        public void StartRun_EntersDocked_AndCountsRun()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            Assert.AreEqual(RunPhase.Docked, sim.Phase);
            Assert.AreEqual(1, meta.TotalRuns);
        }

        [Test]
        public void EngageTether_LandsInPrep_NoAutoWaves_NoIncome()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            var waves = new List<WaveSpec>();
            sim.WaveStarted += w => waves.Add(w);
            sim.StartRun();
            sim.EngageTether();
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);

            TickFor(sim, 120f); // two idle minutes: nothing may happen on its own
            Assert.AreEqual(0, sim.CurrentWaveIndex, "no wave timer exists — draws only");
            Assert.AreEqual(0, waves.Count);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "idle prep earns nothing");
            Assert.AreEqual(0L, meta.BankedSalvage, "and banks nothing");
            Assert.AreEqual(120f, sim.RunElapsed, 1e-3f, "run clock still counts prep time");
        }

        [Test]
        public void TriggerDraw_AfterWave5_StillDrawsClamped()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            int waves = 0;
            sim.WaveStarted += _ => waves++;
            sim.StartRun();
            sim.EngageTether();

            for (int i = 0; i < WaveDirector.MaxWaves; i++)
            {
                sim.TriggerDraw();
                Assert.AreEqual(i + 1, sim.CurrentWaveIndex);
                sim.NotifyAssaultCleared();
            }
            Assert.AreEqual(WaveDirector.MaxWaves, waves);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.IsFalse(sim.AllWavesCleared);

            sim.TriggerDraw();
            Assert.AreEqual(WaveDirector.MaxWaves + 1, waves);
            Assert.AreEqual(WaveDirector.MaxWaves + 1, sim.CurrentWaveIndex);
        }

        [Test]
        public void AllWavesCleared_FalseAfterWave5()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            Assert.IsFalse(sim.AllWavesCleared, "fresh run");

            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                sim.NotifyAssaultCleared();
                Assert.IsFalse(sim.AllWavesCleared, "wave " + (w + 1) + " down is not Escaped");
            }
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void AllWavesCleared_StaysFalseWhenTheRunIsLost()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            sim.NotifyAssaultCleared();
            sim.TriggerDraw();
            sim.NotifyPlayerDown(); // died on wave 2 — the unupgraded default
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
        }

        [Test]
        public void TriggerDraw_StartsAssault_FiresWave_WithDirectorSpec()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            var waves = new List<WaveSpec>();
            sim.WaveStarted += w => waves.Add(w);
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(2);
            sim.TriggerDraw();

            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            Assert.AreEqual(1, waves.Count);
            Assert.AreEqual(new WaveDirector().NextWave(0, 2).enemyCount, waves[0].enemyCount);
        }

        [Test]
        public void TriggerDraw_IgnoredWhenDocked_DuringAssault_AndWhenLost()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            int waves = 0;
            sim.WaveStarted += _ => waves++;
            sim.StartRun();

            sim.TriggerDraw(); // Docked → ignored
            Assert.AreEqual(0, waves);

            sim.EngageTether();
            sim.TriggerDraw(); // legal
            sim.TriggerDraw(); // mid-assault → ignored (no wave stacking)
            Assert.AreEqual(1, waves);
            Assert.AreEqual(1, sim.CurrentWaveIndex);

            sim.Ship.Hatch.Damage(99999f); // → Lost
            sim.TriggerDraw(); // terminal → ignored
            Assert.AreEqual(1, waves);
        }

        [Test]
        public void NoPassiveIncome_SixtySecondsOfAssaultWithZeroKillsPaysNothing()
        {
            // Salvage comes from SLAIN ENEMIES. The clock pays nothing, ever.
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(5); // x1.5 on nothing is still nothing
            sim.TriggerDraw();

            TickFor(sim, 60f);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "no siphon drip");
            Assert.AreEqual(0L, meta.BankedSalvage, "no passive income into the bank either");
            Assert.AreEqual(60f, sim.RunElapsed, 1e-3f, "the clock itself still runs");
        }

        [Test]
        public void AwardKill_BanksIntoMetaStateImmediately_25AtSiphonZero_31AtSiphonThree()
        {
            // Enemy dies → currency added. Not at run end, not conditionally: right now.
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(25f, sim.CurrentKillValue, 1e-5f, "level 0: 25 per kill");
            Assert.AreEqual(0L, meta.BankedSalvage, "nothing banked before the first kill");

            sim.AwardKill();
            Assert.AreEqual(25L, sim.Unbanked, "one kill at siphon 0 goes to unbanked");
            Assert.AreEqual(0L, meta.BankedSalvage, "ship bank is untouched");
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "tally is converted, not kill awards");

            // Dial up in prep, then a kill is worth 25 * 1.25 = 31.25 — floored once to 31.
            sim.NotifyAssaultCleared();
            sim.SetSiphonLevel(3);
            sim.TriggerDraw();
            Assert.AreEqual(31.25f, sim.CurrentKillValue, 1e-5f);
            sim.AwardKill();
            Assert.AreEqual(56L, sim.Unbanked, "one kill at siphon 3 adds 31 more unbanked");
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void AwardKill_BanksTheFullSiphonRange_25To37_TheHandAuthoredCurve()
        {
            // The dial's curve on kill value at Scavenging 0: 25 x (1, 1.1, 1.15, 1.25, 1.35,
            // 1.5) = 25 / 27.5 / 28.75 / 31.25 / 33.75 / 37.5, and AwardKill banks
            // (long)Math.Floor of that, so the BANK sees 25 / 27 / 28 / 31 / 33 / 37.
            // The top of the dial is 37, not the old 800.
            float[] expectedValue = { 25f, 27.5f, 28.75f, 31.25f, 33.75f, 37.5f };
            long[] expectedPerKill = { 25L, 27L, 28L, 31L, 33L, 37L };
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                MetaState meta;
                var sim = NewSim(out meta);
                sim.StartRun();
                sim.EngageTether();
                sim.SetSiphonLevel(level);
                sim.TriggerDraw();
                Assert.AreEqual(expectedValue[level], sim.CurrentKillValue, 1e-4f,
                    "CurrentKillValue at siphon level " + level);
                sim.AwardKill();
                Assert.AreEqual(expectedPerKill[level], sim.Unbanked, "siphon level " + level);
                Assert.AreEqual(0L, meta.BankedSalvage, "siphon level " + level + " ship bank");
            }
        }

        [Test]
        public void AwardKill_WaveOneAtLevelZero_EighteenKillsIsFourFiftyExact()
        {
            // The whole payout of a cleared wave 1 with no upgrades and the dial at rest.
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(18, new WaveDirector().NextWave(0, 0).enemyCount);
            Kill(sim, 18);
            Assert.AreEqual(450L, sim.Unbanked); // 18 * 25
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);
        }

        [Test]
        public void AwardKill_IgnoredOutsideALiveDraw()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();

            sim.AwardKill(); // Docked
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);

            sim.EngageTether();
            sim.AwardKill(); // Prep — nothing is spawned to kill
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);

            sim.TriggerDraw();
            sim.AwardKill(); // legal
            Assert.AreEqual(25L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);

            sim.NotifyAssaultCleared();
            sim.AwardKill(); // back in Prep
            Assert.AreEqual(25L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);

            sim.TriggerDraw();
            sim.NotifyPlayerDown(); // terminal
            sim.AwardKill();
            Assert.AreEqual(0L, sim.Unbanked, "kills after the run ends pay nothing; down wiped the pile");
            Assert.AreEqual(0L, meta.BankedSalvage, "never converted");
        }

        [Test]
        public void NotifyAssaultCleared_ReturnsToPrep_FiresEvent_KillsStopPaying()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            int ended = 0;
            sim.AssaultEnded += () => ended++;
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Kill(sim, 2); // 50 salvage at level 0

            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(1, ended);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "waves remain — the run is still live");

            TickFor(sim, 30f);
            Kill(sim, 3); // rejected in Prep
            Assert.AreEqual(50L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);
        }

        [Test]
        public void NotifyAssaultCleared_IgnoredOutsideAssault()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            int ended = 0;
            sim.AssaultEnded += () => ended++;
            sim.StartRun();
            sim.NotifyAssaultCleared(); // Docked → ignored
            sim.EngageTether();
            sim.NotifyAssaultCleared(); // Prep → ignored
            Assert.AreEqual(0, ended);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
        }

        [Test]
        public void SetSiphonLevel_NoOpDuringAssault_AllowedAgainInPrep()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.SetSiphonLevel(2);
            sim.TriggerDraw();
            sim.SetSiphonLevel(5); // the draw size is a pre-commitment
            Assert.AreEqual(2, sim.Siphon.Level, "dial locked during assault");
            sim.NotifyAssaultCleared();
            sim.SetSiphonLevel(5);
            Assert.AreEqual(5, sim.Siphon.Level, "dial free again in prep");
        }

        [Test]
        public void SetSiphonLevel_IgnoredWhenDocked()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.SetSiphonLevel(3); // still Docked — must be ignored
            Assert.AreEqual(0, sim.Siphon.Level);
            sim.EngageTether();
            sim.SetSiphonLevel(3);
            Assert.AreEqual(3, sim.Siphon.Level);
        }

        [Test]
        public void HatchDestroyed_MidAssault_Pods_AndTakesNoSalvageBack()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Kill(sim, 2);      // 50 unbanked at level 0
            Assert.AreEqual(50L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked, "breach wipes unbanked");
            Assert.AreEqual(0L, meta.BankedSalvage, "never converted");
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("hatch", sim.LastRunReport.cause);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned);
        }

        [Test]
        public void HatchDestroyed_DuringPrep_AlsoPods()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether(); // Prep, no draw pulled, zero salvage
            sim.Ship.Hatch.Damage(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual("hatch", sim.LastRunReport.cause);
            Assert.AreEqual(0L, sim.LastRunReport.grossEarned, "a run with no kills earns nothing");
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(1, meta.PodEjections);
        }

        [Test]
        public void HatchDestroyedWhileDocked_DoesNotPod()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.Ship.Hatch.Damage(99999f); // no live run yet
            Assert.AreEqual(RunPhase.Docked, sim.Phase);
            Assert.AreEqual(0, meta.PodEjections);
        }

        [Test]
        public void HatchRepairedAboveZero_ReArmsTheLoss()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.Ship.Hatch.Damage(sim.Ship.Hatch.MaxHp - 1f); // 1 HP left
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            sim.Ship.Hatch.Repair(50f);
            sim.Ship.Hatch.Damage(99999f); // now it falls for real
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(1, meta.PodEjections);
        }

        [Test]
        public void EngageTether_SecondCall_Ignored()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            var phases = new List<RunPhase>();
            sim.PhaseChanged += p => phases.Add(p);
            sim.StartRun();
            sim.EngageTether();
            sim.EngageTether(); // ignored
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            int tetheredCount = 0;
            foreach (var p in phases) if (p == RunPhase.Tethered) tetheredCount++;
            Assert.AreEqual(1, tetheredCount);
        }

        [Test]
        public void TickWhileDocked_HasNoSideEffects()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            TickFor(sim, 10f);
            Assert.AreEqual(0f, sim.RunElapsed, 1e-5f);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage);
            Assert.AreEqual(0, sim.CurrentWaveIndex);
        }

        [Test]
        public void LostPhase_IgnoresTickSideEffects()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            TickFor(sim, 4f);
            Kill(sim, 2);
            sim.Ship.Hatch.Damage(99999f); // → Lost (terminal)
            float elapsed = sim.RunElapsed;
            long banked = meta.BankedSalvage;

            TickFor(sim, 20f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(elapsed, sim.RunElapsed, 1e-5f);
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "never converted");
            Assert.AreEqual(banked, meta.BankedSalvage);
            Assert.AreEqual(0L, banked);
        }

        [Test]
        public void HatchDestroyedTwice_OnlyPodsOnce()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Kill(sim, 2);
            sim.Ship.Hatch.Damage(99999f);
            long banked = meta.BankedSalvage;
            sim.Ship.Hatch.Damage(99999f); // destroyed hatch ignores further hits
            Assert.AreEqual(banked, meta.BankedSalvage);
            Assert.AreEqual(1, meta.PodEjections);
        }

        [Test]
        public void HatchMaxHp_ComesFromMetaArmorLevel()
        {
            var meta = new MetaState();
            meta.HatchArmorLevel = 2;
            var sim = new RunSim(meta, 1);
            Assert.AreEqual(330f, sim.Ship.Hatch.MaxHp, 1e-5f); // 300 * (1 + 0.05*2)
            Assert.AreEqual(330f, sim.Ship.Hatch.Hp, 1e-5f);
        }

        [Test]
        public void MetaState_JsonRoundTrip_PreservesAllFields()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 9876543210L;
            meta.TotalRuns = 42;
            meta.CleanExits = 30;
            meta.PodEjections = 12;
            meta.DeepestWaveReached = 4;

            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(9876543210L, loaded.BankedSalvage);
            Assert.AreEqual(42, loaded.TotalRuns);
            Assert.AreEqual(30, loaded.CleanExits);
            Assert.AreEqual(12, loaded.PodEjections);
            Assert.AreEqual(4, loaded.DeepestWaveReached);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.SaveVersion);
        }

        [Test]
        public void AbortAssault_RewindsWave_WithoutFiringAssaultEndedOrCompleting()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            int ended = 0;
            sim.AssaultEnded += () => ended++;
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            sim.AbortAssault();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(0, sim.CurrentWaveIndex);
            Assert.AreEqual(0, ended, "AbortAssault must not look like a clear");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);

            // Same wave can be drawn again.
            int waves = 0;
            sim.WaveStarted += _ => waves++;
            sim.TriggerDraw();
            Assert.AreEqual(1, waves);
            Assert.AreEqual(1, sim.CurrentWaveIndex);
        }

        [Test]
        public void SeekWave_PointsNextDrawAtRequestedIndex()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            WaveSpec drawn = default;
            sim.WaveStarted += w => drawn = w;
            sim.StartRun();
            sim.EngageTether();

            sim.SeekWave(2);
            Assert.AreEqual(1, sim.CurrentWaveIndex, "SeekWave(2) leaves index at 1 for the next draw");
            sim.TriggerDraw();
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            Assert.AreEqual(new WaveDirector().NextWave(1, 0).enemyCount, drawn.enemyCount);
        }

        [Test]
        public void SeekWave_ClampsToMaxWaves()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.SeekWave(99);
            Assert.AreEqual(WaveDirector.MaxWaves - 1, sim.CurrentWaveIndex);
        }

        [Test]
        public void MetaState_FromJson_ReadsSaveVersion()
        {
            // FromJson always migrates forward to CurrentVersion (gotcha: raw JSON version
            // is not preserved after load).
            var loaded = MetaState.FromJson("{\"SaveVersion\":8,\"BankedSalvage\":5}");
            Assert.AreEqual(SaveData.CurrentVersion, loaded.SaveVersion);
            Assert.AreEqual(5L, loaded.BankedSalvage);
        }

        [Test]
        public void MetaState_FromJson_MissingFieldsFallBackToDefaults()
        {
            var loaded = MetaState.FromJson("{\"BankedSalvage\":100}");
            Assert.AreEqual(SaveData.CurrentVersion, loaded.SaveVersion);
            Assert.AreEqual(100L, loaded.BankedSalvage);
            Assert.AreEqual(0, loaded.TotalRuns);
        }

        [Test]
        public void MetaState_SaveDataRoundTrip()
        {
            var meta = new MetaState();
            meta.BankedSalvage = 777;
            meta.CleanExits = 3;
            var restored = MetaState.FromSaveData(meta.ToSaveData());
            Assert.AreEqual(777L, restored.BankedSalvage);
            Assert.AreEqual(3, restored.CleanExits);
            Assert.AreEqual(meta.SaveVersion, restored.SaveVersion);
        }
    }
}
