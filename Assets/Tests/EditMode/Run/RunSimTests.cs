using System.Collections.Generic;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>EditMode tests for the RunSim state machine (Docked → Tethered → Lost),
    /// the Prep/Assault draw model, kill-banked salvage, death as pure bookkeeping, and
    /// MetaState JSON persistence.</summary>
    public class RunSimTests
    {
        private static RunSim NewSim(out MetaState meta)
        {
            meta = MetaState.CreateNew();
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

        /// <summary>What one body is worth at <paramref name="siphonLevel"/>, and what the bank
        /// actually sees for it once AwardKill floors per body. DERIVED: SalvagePerKill moved
        /// 25 -> 42 on 2026-08-27 and every literal in this fixture's economy section moved with
        /// it, including three that were in test names.</summary>
        private static float KillValue(int siphonLevel) =>
            MetaUpgrades.SalvagePerKill() * TetherSiphon.RewardMultiplierFor(siphonLevel);

        private static long Banked(int siphonLevel, int kills) =>
            kills * (long)KillValue(siphonLevel);

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
        public void AwardKill_BanksIntoMetaStateImmediately_42AtSiphonZero_52AtSiphonThree()
        {
            // Renamed FROM _25AtSiphonZero_31AtSiphonThree: SalvagePerKill 25 -> 42 took both
            // halves of the name with it, and 42 x 1.25 floors to 52 rather than 31.
            // Enemy dies → currency added. Not at run end, not conditionally: right now.
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(KillValue(0), sim.CurrentKillValue, 1e-5f,
                "level 0 is the flat price of a kill, no dial in the way");
            Assert.AreEqual(0L, meta.BankedSalvage, "nothing banked before the first kill");

            // Re-key 2026-08-25: this used to prove the award landed in the AT-RISK pile and
            // left the bank alone. The ruling inverted it — a body that falls is progress the
            // player keeps — so the same kill now proves the opposite pair, and the carried
            // pile reading 0 is the load-bearing half: nothing is staked unless it was
            // deliberately withdrawn.
            sim.AwardKill();
            Assert.AreEqual(Banked(0, 1), meta.BankedSalvage, "one kill at siphon 0 banks as it lands");
            Assert.AreEqual(0L, sim.Unbanked, "and stakes nothing — only a Withdraw fills the pile");
            Assert.AreEqual(Banked(0, 1), sim.Salvage.RunSalvage, "a banked kill is this run's take");

            // Dial up in prep, then a kill is worth SalvagePerKill x 1.25 — 52.5 today, floored
            // once to 52. Face value: the carry multiplier is a Convert-window reward and nothing
            // was carried.
            sim.NotifyAssaultCleared();
            sim.SetSiphonLevel(3);
            sim.TriggerDraw();
            Assert.AreEqual(KillValue(3), sim.CurrentKillValue, 1e-5f);
            sim.AwardKill();
            Assert.AreEqual(Banked(0, 1) + Banked(3, 1), meta.BankedSalvage,
                "one kill at siphon 3 banks the dialled award on top of the first");
            Assert.Greater(Banked(3, 1), Banked(0, 1), "dialling up has to actually pay more");
            Assert.AreEqual(0L, sim.Unbanked);
        }

        [Test]
        public void AwardKill_BanksTheFullSiphonRange_42To63_TheHandAuthoredCurve()
        {
            // Renamed FROM _25To37: SalvagePerKill 25 -> 42 moved both ends of the range. The
            // dial's curve on kill value at Scavenging 0 is SalvagePerKill x (1, 1.1, 1.15, 1.25,
            // 1.35, 1.5) = 42 / 46.2 / 48.3 / 52.5 / 56.7 / 63, and AwardKill banks
            // (long)Math.Floor of that, so the BANK sees 42 / 46 / 48 / 52 / 56 / 63.
            //
            // THE TWO ARRAYS THAT USED TO SIT HERE ARE GONE. They were the same six numbers
            // written twice — once as the value, once as its floor — and the price raise turned
            // all twelve red. What the fixture is claiming is that CurrentKillValue is the flat
            // price times the dial, and that the bank sees the FLOOR of it, per body. Both are
            // now computed from the two constants that decide them.
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                MetaState meta;
                var sim = NewSim(out meta);
                sim.StartRun();
                sim.EngageTether();
                sim.SetSiphonLevel(level);
                sim.TriggerDraw();
                Assert.AreEqual(KillValue(level), sim.CurrentKillValue, 1e-4f,
                    "CurrentKillValue at siphon level " + level);
                sim.AwardKill();
                // Re-key 2026-08-25: the curve's SHAPE is unchanged, but it is read off the BANK,
                // which is where the comment above already said the floor lands.
                Assert.AreEqual(Banked(level, 1), meta.BankedSalvage, "siphon level " + level);
                Assert.AreEqual(0L, sim.Unbanked, "siphon level " + level + " stakes nothing");
                if (level > 0)
                    Assert.Greater(Banked(level, 1), Banked(level - 1, 1),
                        "every step of the dial must be worth taking, level " + level);
            }

            // AND THE FLOOR MUST ACTUALLY BITE SOMEWHERE, or "floored per body" is untested. At
            // 42 the fractional levels are 46.2 / 48.3 / 52.5 / 56.7; if a future price makes
            // every level whole, this pin has quietly stopped watching the rounding.
            int fractional = 0;
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                if (KillValue(level) > (long)KillValue(level)) fractional++;
            Assert.Greater(fractional, 0,
                "no siphon level produces a fractional award, so the per-body floor is untested");
        }

        [Test]
        public void AwardKill_WaveOneAtLevelZero_ElevenKillsIsFourSixtyTwoExact()
        {
            // Renamed FROM _EighteenKillsIsFourFiftyExact. BOTH numbers moved on 2026-08-27 and
            // they moved in opposite directions on purpose: the roster cut took wave 1 from 18
            // bodies to 11, and SalvagePerKill rose 25 -> 42 to stop that cutting run income by
            // 40%. 18 x 25 = 450 became 11 x 42 = 462 — the wave pays what it used to, within 3%,
            // which is the whole claim of the pass and the reason this pin is worth keeping.
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();
            sim.EngageTether();
            sim.TriggerDraw();
            int waveOne = new WaveDirector().NextWave(0, 0).enemyCount;
            Assert.AreEqual(WaveDirector.BaseEnemyCount, waveOne);
            Kill(sim, waveOne);
            // Re-key 2026-08-25: the arithmetic's destination is the bank — salvage a death
            // cannot take back, which is the whole point of that ruling.
            Assert.AreEqual(Banked(0, waveOne), meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(Banked(0, waveOne), sim.Salvage.RunSalvage);
        }

        [Test]
        public void AwardKill_IgnoredOutsideALiveDraw()
        {
            MetaState meta;
            var sim = NewSim(out meta);
            sim.StartRun();

            // Re-key 2026-08-25: the phase gate is the subject and it did not move, but the
            // meter that shows whether a kill paid did — a refused kill is now one that leaves
            // the BANK flat.
            sim.AwardKill(); // Docked
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);

            sim.EngageTether();
            sim.AwardKill(); // Prep — nothing is spawned to kill
            Assert.AreEqual(0L, meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);

            sim.TriggerDraw();
            sim.AwardKill(); // legal
            Assert.AreEqual(Banked(0, 1), meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);

            sim.NotifyAssaultCleared();
            sim.AwardKill(); // back in Prep
            Assert.AreEqual(Banked(0, 1), meta.BankedSalvage, "prep pays nothing — still the one kill");
            Assert.AreEqual(0L, sim.Unbanked);

            sim.TriggerDraw();
            sim.NotifyPlayerDown(); // terminal
            sim.AwardKill();
            Assert.AreEqual(Banked(0, 1), meta.BankedSalvage,
                "kills after the run ends pay nothing, and the death cannot reach behind them "
                + "for the award that already banked");
            Assert.AreEqual(0L, sim.Unbanked, "nothing was staked, so the wipe had nothing to take");
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
            Kill(sim, 2); // two bodies at level 0

            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(1, ended);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "waves remain — the run is still live");

            TickFor(sim, 30f);
            Kill(sim, 3); // rejected in Prep
            // Re-key 2026-08-25: "kills stop paying" is still the claim; the ledger it is read
            // off is the bank, which stays at the two assault kills and gains nothing from the
            // three prep ones.
            Assert.AreEqual(Banked(0, 2), meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(Banked(0, 2), sim.Salvage.RunSalvage);
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
            Kill(sim, 2);      // 50 banked at level 0
            Assert.AreEqual(Banked(0, 2), meta.BankedSalvage);
            Assert.AreEqual(0L, sim.Unbanked, "nothing staked — the pile needs a Withdraw");
            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked, "breach wipes unbanked — there was none to wipe");
            // Re-key 2026-08-25: "takes no salvage back" used to mean the breach found an empty
            // bank. It now means the stronger thing the name always promised — the breach walks
            // past 50 banked units and cannot touch one of them.
            Assert.AreEqual(Banked(0, 2), meta.BankedSalvage, "the breach cannot claw back banked kills");
            Assert.AreEqual(1, meta.PodEjections);
            Assert.AreEqual("hatch", sim.LastRunReport.cause);
            Assert.AreEqual(Banked(0, 2), sim.LastRunReport.grossEarned, "the pod leaves with the take");
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
            // Re-key 2026-08-25: the subject is that a terminal phase is inert, so the snapshot
            // taken before the ticks is what has to hold. It is a more honest witness now that
            // the two kills put a real 50 in the bank — a frozen zero could have been frozen by
            // accident, a frozen 50 could not.
            Assert.AreEqual(Banked(0, 2), banked, "two kills banked before the hatch fell");
            Assert.AreEqual(banked, meta.BankedSalvage, "twenty dead seconds move nothing");
            Assert.AreEqual(Banked(0, 2), sim.Salvage.RunSalvage, "and the run tally is equally frozen");
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
            var meta = MetaState.CreateNew();
            meta.HatchArmorLevel = 2;
            var sim = new RunSim(meta, 1);
            Assert.AreEqual(330f, sim.Ship.Hatch.MaxHp, 1e-5f); // 300 * (1 + 0.05*2)
            Assert.AreEqual(330f, sim.Ship.Hatch.Hp, 1e-5f);
        }

        [Test]
        public void MetaState_JsonRoundTrip_PreservesAllFields()
        {
            var meta = MetaState.CreateNew();
            meta.BankedSalvage = 9876543210L;
            meta.TotalRuns = 42;
            meta.CleanExits = 30;
            meta.PodEjections = 12;
            meta.DeepestWaveReached = 4;

            var loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(9876543210L, loaded.BankedSalvage);
            Assert.AreEqual(42, loaded.TotalRuns);
            Assert.AreEqual(30, loaded.CleanExits);
            Assert.AreEqual(12, loaded.PodEjections);
            Assert.AreEqual(4, loaded.DeepestWaveReached);
            Assert.AreEqual(SaveFormat.CurrentVersion, loaded.SaveVersion);
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
        public void RealmDocument_MissingFieldsFallBackToDefaults()
        {
            var loaded = RealmState.FromJson(
                "{\"SaveVersion\":13,\"BankedSalvage\":100}");
            Assert.IsNotNull(loaded, "a current-version realm document loads");
            Assert.AreEqual(100L, loaded.BankedSalvage);
            Assert.AreEqual(0, loaded.TotalRuns);
        }

        [Test]
        public void MetaState_SaveDataRoundTrip()
        {
            var meta = MetaState.CreateNew();
            meta.BankedSalvage = 777;
            meta.CleanExits = 3;
            var restored = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(777L, restored.BankedSalvage);
            Assert.AreEqual(3, restored.CleanExits);
            Assert.AreEqual(meta.SaveVersion, restored.SaveVersion);
        }
    }
}
