using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    public class TerminalWaveTests
    {
        [Test]
        public void TriggerDraw_SetsModeTerminal()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
        }

        [Test]
        public void DamageHatch_LandsInTerminalMode_BecauseTheFieldWaveHasAStake()
        {
            // REVERSED 2026-08-27 (Zo). This pinned the opposite: a field Wave left the Hatch
            // invulnerable. That made every body which reached the Hatch inert — most sharply
            // the Thief, whose ONLY weapon is hatchDamage, so on a field Wave it could affect
            // nothing at all and 12 of 13 walked a logged run untouched.
            //
            // Pinned as an inequality against the damage dealt rather than a fixed HP, so the
            // Hatch's own max can move without dragging this with it.
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(WaveMode.Terminal, sim.Mode,
                "this fixture is meaningless if the draw did not put us on a field Wave");
            float hp = sim.Ship.Hatch.Hp;
            sim.DamageHatch(25f);
            // Derived from the constant, never restated: a field Wave's hit is scaled because
            // the Hatch is shut while the player is up the lane. Pinning 3.75 here would make
            // the one dial that is expected to move read as a regression when it does.
            Assert.AreEqual(hp - 25f * HatchBank.FieldWaveDamageScale, sim.Ship.Hatch.Hp, 0.01f,
                "a field Wave cannot hurt the Hatch — the objective is back to being scenery");
            Assert.Less(HatchBank.FieldWaveDamageScale, 1f,
                "the field scale is not a scale; the Hatch is as soft as it is on a bank Wave");
            Assert.Greater(HatchBank.FieldWaveDamageScale, 0f,
                "the field scale is zero, which is invulnerability wearing a constant's name");
        }

        [Test]
        public void DamageHatch_DirectHatchDamage_StillApplies()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.TriggerDraw();
            float hp = sim.Ship.Hatch.Hp;
            sim.Ship.Hatch.Damage(10f);
            Assert.AreEqual(hp - 10f, sim.Ship.Hatch.Hp, 0.01f);
        }

        static BastionUnlockGraph FieldGraph(int t1Waves)
        {
            return BastionUnlockGraph.Build(
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
                        RequiredTerminalIds = new[] { "T_control" },
                        OpensControlRoom = true,
                    },
                },
                new[]
                {
                    new BastionUnlockGraph.TerminalSpec
                    {
                        TerminalId = "T1",
                        WavesToComplete = t1Waves,
                    },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T_control" },
                });
        }

        [Test]
        public void RecordWaveCleared_N2_CompletesOnSecondClear()
        {
            var graph = FieldGraph(2);
            Assert.IsTrue(graph.RecordWaveCleared("T1", 1, out string r1));
            Assert.IsNull(r1);
            Assert.IsFalse(graph.IsTerminalDone("T1"));
            Assert.IsFalse(graph.IsDoorOpen("D1"));
            Assert.AreEqual(1, graph.ClearedWaves("T1"));

            Assert.IsTrue(graph.RecordWaveCleared("T1", 1, out string r2));
            Assert.IsNull(r2);
            Assert.IsTrue(graph.IsTerminalDone("T1"));
            Assert.IsFalse(graph.IsDoorOpen("D1"), "completion is eligibility, not an unlock");
            graph.OpenEligibleDoors(out _);
            Assert.IsTrue(graph.IsDoorOpen("D1"));
            Assert.AreEqual(2, graph.ClearedWaves("T1"));
        }

        [Test]
        public void RecordWaveCleared_N1_CompletesOnFirstClear()
        {
            var graph = FieldGraph(1);
            Assert.IsTrue(graph.RecordWaveCleared("T1", 1, out _));
            Assert.IsTrue(graph.IsTerminalDone("T1"));
            Assert.IsFalse(graph.IsDoorOpen("D1"), "completion is eligibility, not an unlock");
            graph.OpenEligibleDoors(out _);
            Assert.IsTrue(graph.IsDoorOpen("D1"));
        }

        [Test]
        public void RecordWaveCleared_DeepStartDelta_DoesNotCompleteEarly()
        {
            var graph = FieldGraph(5);
            Assert.IsTrue(graph.RecordWaveCleared("T1", 3, out _));
            Assert.AreEqual(3, graph.ClearedWaves("T1"));
            Assert.IsFalse(graph.IsTerminalDone("T1"));
            Assert.IsFalse(graph.IsDoorOpen("D1"));

            Assert.IsTrue(graph.RecordWaveCleared("T1", 2, out _));
            Assert.AreEqual(5, graph.ClearedWaves("T1"));
            Assert.IsTrue(graph.IsTerminalDone("T1"));
            Assert.IsFalse(graph.IsDoorOpen("D1"), "completion is eligibility, not an unlock");
            graph.OpenEligibleDoors(out _);
            Assert.IsTrue(graph.IsDoorOpen("D1"));
        }

        [Test]
        public void RecordWaveCleared_RefusesUnknownAndAlreadyComplete()
        {
            var graph = FieldGraph(1);
            Assert.IsFalse(graph.RecordWaveCleared("T_missing", 1, out string unknown));
            Assert.AreEqual("unknown terminal", unknown);

            Assert.IsTrue(graph.RecordWaveCleared("T1", 1, out _));
            Assert.IsFalse(graph.RecordWaveCleared("T1", 1, out string again));
            Assert.AreEqual("already complete", again);
        }

        [Test]
        public void ArmSequentialDraws_AfterClear_WaitsGapThenDraws()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.ArmSequentialDraws(2);
            sim.TriggerDraw();
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            Assert.IsTrue(sim.SequentialPending);

            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            Assert.IsTrue(sim.SequentialPending);

            sim.Tick(WaveDirector.TerminalChainGap - 0.05f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(1, sim.CurrentWaveIndex);

            sim.Tick(0.1f);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);

            sim.NotifyAssaultCleared();
            Assert.IsFalse(sim.SequentialPending);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
        }

        [Test]
        public void FieldClear_LeavesUntimedPrep_HatchBankAllowed()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.TriggerDraw();
            long earned = RunEconomyFixture.KillAndStake(sim, 1);
            sim.NotifyAssaultCleared();
            Assert.IsFalse(sim.SequentialPending);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            // The bank window still refuses on an empty carry, and since 2026-08-25 kills no
            // longer fill one — so the withdraw at the full Hatch is what makes this untimed
            // Prep a break the player can actually open the door from.
            RunEconomyFixture.Stake(sim, earned);
            Assert.Greater(sim.Unbanked, 0L);
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
        }

        [Test]
        public void ArmSequentialDraws_One_DoesNotFollowUp()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.ArmSequentialDraws(1);
            sim.TriggerDraw();
            sim.NotifyAssaultCleared();
            Assert.IsFalse(sim.SequentialPending);
            sim.Tick(WaveDirector.TerminalChainGap + 1f);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(1, sim.CurrentWaveIndex);
        }

        [Test]
        public void TerminalChainGap_IsEightSeconds()
        {
            Assert.AreEqual(8f, WaveDirector.TerminalChainGap, 1e-6f);
        }

        [Test]
        public void UnlockGraph_OpensOnlyAfterTerminalMarkedAndBankSurvived()
        {
            var doors = new[]
            {
                new BastionUnlockGraph.DoorSpec
                {
                    DoorId = "D1",
                    RequiredTerminalIds = new[] { "T1" },
                    OpensControlRoom = false,
                },
            };
            var terminals = new[]
            {
                new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
            };
            var graph = BastionUnlockGraph.Build(doors, terminals);
            Assert.IsFalse(graph.IsDoorOpen("D1"));
            Assert.IsTrue(graph.TryCompleteTerminal("T1", out _));
            Assert.IsFalse(graph.IsDoorOpen("D1"));

            // The failed-bank case is this same assertion: a breached window ends the Run
            // before OpenEligibleDoors is ever reached, so the door is still shut here.
            Assert.IsTrue(graph.OpenEligibleDoors(out _));
            Assert.IsTrue(graph.IsDoorOpen("D1"));
        }
    }
}
