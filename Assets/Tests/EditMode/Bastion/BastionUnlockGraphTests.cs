using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    public class BastionUnlockGraphTests
    {
        static BastionUnlockGraph SampleGraph()
        {
            return BastionUnlockGraph.Build(
                new[]
                {
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D1",
                        RequiredTerminalIds = new[] { "T1", "T2" },
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
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T2" },
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T_control" },
                });
        }

        [Test]
        public void CompletingT1AndT2_MakesD1Eligible_ButOnlyASurvivedBankOpensIt()
        {
            var graph = SampleGraph();

            Assert.IsTrue(graph.TryCompleteTerminal("T1", out string reason1));
            Assert.IsNull(reason1);
            Assert.IsFalse(graph.IsDoorOpen("D1"));

            Assert.IsTrue(graph.TryCompleteTerminal("T2", out string reason2));
            Assert.IsNull(reason2);
            Assert.IsFalse(graph.IsDoorOpen("D1"),
                "both terminals done is eligibility, not an unlock");

            Assert.IsTrue(graph.OpenEligibleDoors(out string[] opened));
            Assert.IsTrue(graph.IsDoorOpen("D1"));
            Assert.AreEqual(1, opened.Length);
            Assert.AreEqual("D1", opened[0]);
        }

        [Test]
        public void BankSurvived_WithNoTerminalsDone_OpensNothing()
        {
            var graph = SampleGraph();

            Assert.IsFalse(graph.OpenEligibleDoors(out string[] opened),
                "a bank survived before any terminal is done unlocks nothing");
            Assert.AreEqual(0, opened.Length);
            Assert.IsFalse(graph.IsDoorOpen("D1"));
        }

        [Test]
        public void BankSurvived_Twice_ReportsEachDoorOnce()
        {
            var graph = SampleGraph();
            graph.TryCompleteTerminal("T1", out _);
            graph.TryCompleteTerminal("T2", out _);

            Assert.IsTrue(graph.OpenEligibleDoors(out string[] first));
            Assert.AreEqual(1, first.Length);

            Assert.IsFalse(graph.OpenEligibleDoors(out string[] second),
                "an already-open door is not re-reported, so the door line plays once");
            Assert.AreEqual(0, second.Length);
            Assert.IsTrue(graph.IsDoorOpen("D1"));
        }

        [Test]
        public void OpenDoors_RoundTripThroughRestore()
        {
            var graph = SampleGraph();
            graph.TryCompleteTerminal("T1", out _);
            graph.TryCompleteTerminal("T2", out _);
            graph.OpenEligibleDoors(out _);

            string[] ids = graph.CopyOpenDoorIds();
            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual("D1", ids[0]);

            var restored = SampleGraph();
            Assert.IsFalse(restored.IsDoorOpen("D1"));
            restored.RestoreOpenDoors(ids);
            Assert.IsTrue(restored.IsDoorOpen("D1"),
                "a Docked return finds the door it paid for standing open");
            Assert.IsFalse(restored.IsTerminalDone("T1"),
                "door state restores on its own; it does not fake terminal progress");

            restored.RestoreOpenDoors(ids);
            Assert.AreEqual(1, restored.CopyOpenDoorIds().Length, "restore is idempotent");
        }

        [Test]
        public void RestoreOpenDoors_SkipsUnknownIds()
        {
            var graph = SampleGraph();
            graph.RestoreOpenDoors(new[] { "D_gone", "", null });
            Assert.AreEqual(0, graph.CopyOpenDoorIds().Length);
        }

        [Test]
        public void ControlDoor_StaysLockedUntilItsTerminalsDone()
        {
            var graph = SampleGraph();
            graph.TryCompleteTerminal("T1", out _);
            graph.TryCompleteTerminal("T2", out _);
            graph.OpenEligibleDoors(out _);

            Assert.IsTrue(graph.IsDoorOpen("D1"));
            Assert.IsFalse(graph.IsDoorOpen("D_control"));
            Assert.IsFalse(graph.ControlRoomReachable);

            Assert.IsTrue(graph.TryCompleteTerminal("T_control", out _));
            Assert.IsFalse(graph.IsDoorOpen("D_control"),
                "the control gate is survival-keyed too — completing it is not enough");
            graph.OpenEligibleDoors(out _);
            Assert.IsTrue(graph.IsDoorOpen("D_control"));
            Assert.IsTrue(graph.ControlRoomReachable);
            Assert.IsTrue(graph.IsFullyUnlocked);
        }

        [Test]
        public void TryCompleteTerminal_RefusesUnknownAndDoubleComplete()
        {
            var graph = SampleGraph();

            Assert.IsFalse(graph.TryCompleteTerminal("T_missing", out string unknown));
            Assert.AreEqual("unknown terminal", unknown);

            Assert.IsTrue(graph.TryCompleteTerminal("T1", out _));

            Assert.IsFalse(graph.TryCompleteTerminal("T1", out string again));
            Assert.AreEqual("already complete", again);
        }

        [Test]
        public void Validate_FailsWithNoControlGate()
        {
            var graph = BastionUnlockGraph.Build(
                new[]
                {
                    new BastionUnlockGraph.DoorSpec
                    {
                        DoorId = "D1",
                        RequiredTerminalIds = new[] { "T1" },
                    },
                },
                new[]
                {
                    new BastionUnlockGraph.TerminalSpec { TerminalId = "T1" },
                });

            Assert.AreEqual("graph has no control gate", graph.Validate());
        }
    }
}
