using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class MasterControlTests
    {
        [TearDown]
        public void RestoreHatchWaves()
        {
            MasterControl.HatchWaves = 1;
        }

        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static void SurviveMasterControl(RunSim sim)
        {
            SurviveSequence(sim, 0);
        }

        /// <summary>Drive both Master Control halves. Used by RunCompleteTests.</summary>
        public static void SurviveSequence(RunSim sim, int killsPerTerminalWave)
        {
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                Assert.AreEqual(AssaultState.Assault, sim.Assault);
                Assert.AreEqual(WaveMode.Terminal, sim.Mode);
                for (int k = 0; k < killsPerTerminalWave; k++)
                    sim.AwardKill();
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            Assert.AreEqual(MasterControlPhase.Hatch, sim.MasterPhase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            sim.Tick(WaveDirector.InterWaveGap);

            for (int i = 0; i < MasterControl.HatchWaves; i++)
            {
                Assert.AreEqual(AssaultState.Assault, sim.Assault);
                Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
                sim.Tick(HatchBank.DurationSeconds);
                if (i < MasterControl.HatchWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }
        }

        [Test]
        public void Wave5Clear_UnbankedZero_StaysPrep()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                sim.TriggerDraw();
                sim.NotifyAssaultCleared();
            }

            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(0, meta.CleanExits);
        }

        [Test]
        public void Sequence_FiveTerminalThenHatchWindows_Escapes()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            // Kills bank as they land (Zo, 2026-08-25), so the finale needs a pile PUT there:
            // the run stakes back exactly what the Wave paid, by the route the player now takes.
            // The figure used to be written as the literal 200 (eight kills at 25); SalvagePerKill
            // 25 -> 42 makes it 336, and the claim was only ever "whatever the wave paid is what
            // goes on the table".
            long expected = 8L * (long)MetaUpgrades.SalvagePerKill();
            long earned = RunEconomyFixture.KillAndStake(sim, 8);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(expected, earned);
            RunEconomyFixture.Stake(sim, earned);
            Assert.AreEqual(expected, sim.Unbanked);
            long bankBefore = meta.BankedSalvage;

            bool hatchHalf = false;
            sim.MasterHatchHalfStarted += () => hatchHalf = true;
            SurviveMasterControl(sim);

            Assert.IsTrue(hatchHalf);
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.IsTrue(sim.AllWavesCleared);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.Greater(meta.BankedSalvage, bankBefore);
            Assert.AreEqual(1, meta.CleanExits);
            Assert.AreEqual("cleared", sim.LastRunReport.cause);
            Assert.AreEqual(MasterControlPhase.None, sim.MasterPhase);
        }

        [Test]
        public void HatchHalf_DoesNotDumpUntilLastWindow()
        {
            MasterControl.HatchWaves = 5;
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            // Staked back out of the bank: the finale's drip has nothing to meter otherwise,
            // because kills no longer leave a carried pile behind (Zo, 2026-08-25).
            long earned = RunEconomyFixture.KillAndStake(sim, 40);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            long pile = sim.Unbanked;
            Assert.Greater(pile, (long)(HatchBank.DripPerSecond * HatchBank.DurationSeconds));
            sim.Tick(WaveDirector.InterWaveGap);
            sim.Tick(10f);
            Assert.Less(sim.Unbanked, pile);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            sim.Tick(HatchBank.DurationSeconds - 10f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.Greater(sim.Unbanked, 0L);
        }

        [Test]
        public void HatchHalfBreach_KeepsDripped_WipesRemainder()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            // The whole point of this test is a pile that is AT RISK when the Hatch goes, and
            // since 2026-08-25 (Zo) the only pile at risk is one the player staked.
            long earned = RunEconomyFixture.KillAndStake(sim, 24);
            sim.NotifyAssaultCleared();
            RunEconomyFixture.Stake(sim, earned);
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            sim.Tick(WaveDirector.InterWaveGap);
            sim.Tick(10f);
            long banked = meta.BankedSalvage;
            Assert.Greater(banked, 0L);
            Assert.Greater(sim.Unbanked, 0L);
            sim.Ship.Hatch.Damage(99999f);
            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(banked, meta.BankedSalvage);
            Assert.AreEqual(0, meta.CleanExits);
            Assert.IsTrue(meta.DemoActive, "a Lost half must not end the demo");
        }

        [Test]
        public void SecondPull_RefusedWhileLive()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.StartMasterControl());
            Assert.IsFalse(sim.StartMasterControl());
            Assert.AreEqual(MasterControlPhase.Terminal, sim.MasterPhase);
        }

        [Test]
        public void HatchBankWithUnbankedZero_StillRunsThenEscapes()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            SurviveMasterControl(sim);
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.AreEqual(0L, meta.BankedSalvage);
        }

        [Test]
        public void CompletedTerminalIds_RoundTripJson_AndAbsentIsEmpty()
        {
            var meta = MetaState.CreateNew();
            meta.SetCompletedTerminalIds(new[] { "T2", "T1" });
            var loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(SaveFormat.CurrentVersion, loaded.SaveVersion);
            Assert.AreEqual(2, loaded.CompletedTerminalIds.Length);
            Assert.AreEqual("T2", loaded.CompletedTerminalIds[0]);
            Assert.AreEqual("T1", loaded.CompletedTerminalIds[1]);

            var absent = RealmState.FromJson(
                "{\"SaveVersion\":13,\"BankedSalvage\":0}");
            Assert.IsNotNull(absent);
            Assert.AreEqual(0, absent.CompletedTerminalIds.Length);
        }

        [Test]
        public void RestoreCompletedTerminals_KeepsProgress_AndOpenDoorsRestoreSeparately()
        {
            var graph = BastionUnlockGraph.Build(
                new[]
                {
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D1",
                        RequiredTerminalIds = new[] { "T1" },
                    },
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D_control",
                        RequiredTerminalIds = new[] { "T4" },
                        OpensControlRoom = true,
                    },
                },
                new[]
                {
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T4" },
                });

            Assert.IsTrue(graph.TryCompleteTerminal("T1", out _));
            string[] ids = graph.CopyCompletedTerminalIds();
            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual("T1", ids[0]);

            var restored = BastionUnlockGraph.Build(
                new[]
                {
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D1",
                        RequiredTerminalIds = new[] { "T1" },
                    },
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D_control",
                        RequiredTerminalIds = new[] { "T4" },
                        OpensControlRoom = true,
                    },
                },
                new[]
                {
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T4" },
                });
            restored.RestoreCompletedTerminals(ids);
            Assert.IsTrue(restored.IsTerminalDone("T1"));
            Assert.IsFalse(restored.IsDoorOpen("D1"),
                "terminal progress no longer implies an open door");

            graph.OpenEligibleDoors(out _);
            restored.RestoreOpenDoors(graph.CopyOpenDoorIds());
            Assert.IsTrue(restored.IsDoorOpen("D1"));
            Assert.IsFalse(restored.IsDoorOpen("D_control"));
        }

        [Test]
        public void HatchWaves_DefaultsToOne()
        {
            Assert.AreEqual(1, MasterControl.HatchWaves);
        }

        [Test]
        public void FinaleWindow_IsOneEliteThief()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.StartMasterControl());
            for (int i = 0; i < MasterControl.TerminalWaves; i++)
            {
                sim.NotifyAssaultCleared();
                if (i < MasterControl.TerminalWaves - 1)
                    sim.Tick(WaveDirector.InterWaveGap);
            }

            WaveSpec hatch = default;
            sim.WaveStarted += s => hatch = s;
            sim.Tick(WaveDirector.InterWaveGap);

            Assert.IsTrue(hatch.finaleElite, "the final window carries the elite flag");
            Assert.AreEqual(MasterControl.FinaleEliteCount, hatch.enemyCount);
            Assert.AreEqual(MasterControl.FinaleEliteCount, hatch.thiefCount);
            Assert.AreEqual(0, hatch.hunterCount, "no escorts");
            Assert.AreEqual(0, hatch.defaultCount, "no escorts");
            Assert.AreEqual(1, hatch.groupSize);
            Assert.Greater(MasterControl.FinaleEliteHp * hatch.enemyHealthMultiplier,
                WaveDirector.StatsFor(GatekeeperKind.Thief).maxHp * hatch.enemyHealthMultiplier,
                "spawned elite HP must sit above a Wave-5 Thief");
        }

        [Test]
        public void SkipToMasterHatchHalf_DrawsEliteWithExpectedUnbanked()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            WaveSpec hatch = default;
            bool hatchHalf = false;
            sim.WaveStarted += s => hatch = s;
            sim.MasterHatchHalfStarted += () => hatchHalf = true;

            Assert.IsTrue(sim.SkipToMasterHatchHalf());
            Assert.IsTrue(hatchHalf, "hatch spawns arm before the draw");
            Assert.AreEqual(MasterControlPhase.Hatch, sim.MasterPhase);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.AreEqual(HatchBank.MasterTerminalUnbanked(), sim.Unbanked);
            Assert.AreEqual(HatchBank.CarryConvert(5), sim.HatchConvertMultiplier, 1e-5f);
            Assert.IsTrue(hatch.finaleElite);
            Assert.AreEqual(1, hatch.enemyCount);
            Assert.AreEqual(1, hatch.thiefCount);
        }
    }
}
