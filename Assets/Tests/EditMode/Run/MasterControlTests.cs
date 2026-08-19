using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class MasterControlTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = new MetaState();
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
        public void Sequence_FiveTerminalThenFiveHatch_EscapesWithoutSpendingSlot()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            for (int i = 0; i < 8; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
            Assert.AreEqual(200L, sim.Unbanked);
            int slots = sim.HatchBankSlotsLeft;
            long bankBefore = meta.BankedSalvage;

            bool hatchHalf = false;
            sim.MasterHatchHalfStarted += () => hatchHalf = true;
            SurviveMasterControl(sim);

            Assert.IsTrue(hatchHalf);
            Assert.AreEqual(RunPhase.Escaped, sim.Phase);
            Assert.IsTrue(sim.AllWavesCleared);
            Assert.AreEqual(0L, sim.Unbanked);
            Assert.Greater(meta.BankedSalvage, bankBefore);
            Assert.AreEqual(slots, sim.HatchBankSlotsLeft);
            Assert.AreEqual(1, meta.CleanExits);
            Assert.AreEqual("cleared", sim.LastRunReport.cause);
            Assert.AreEqual(MasterControlPhase.None, sim.MasterPhase);
        }

        [Test]
        public void HatchHalf_DoesNotDumpUntilLastWindow()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            for (int i = 0; i < 40; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
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
            for (int i = 0; i < 24; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
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
        public void CompletedTerminalIds_RoundTripJson_AndPreV7IsEmpty()
        {
            var meta = new MetaState();
            meta.SetCompletedTerminalIds(new[] { "T2", "T1" });
            var loaded = MetaState.FromJson(meta.ToJson());
            Assert.AreEqual(SaveData.CurrentVersion, loaded.SaveVersion);
            Assert.AreEqual(2, loaded.CompletedTerminalIds.Length);
            Assert.AreEqual("T2", loaded.CompletedTerminalIds[0]);
            Assert.AreEqual("T1", loaded.CompletedTerminalIds[1]);

            var old = MetaState.FromJson("{\"SaveVersion\":6,\"BankedSalvage\":0}");
            Assert.AreEqual(SaveData.CurrentVersion, old.SaveVersion);
            Assert.AreEqual(0, old.CompletedTerminalIds.Length);
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

            graph.RecordBankSurvived(out _);
            restored.RestoreOpenDoors(graph.CopyOpenDoorIds());
            Assert.IsTrue(restored.IsDoorOpen("D1"));
            Assert.IsFalse(restored.IsDoorOpen("D_control"));
        }
    }
}
