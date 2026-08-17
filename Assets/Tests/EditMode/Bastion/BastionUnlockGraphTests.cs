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
        public void CompletingT1AndT2_OpensD1()
        {
            var graph = SampleGraph();

            Assert.IsTrue(graph.TryCompleteTerminal("T1", out string reason1));
            Assert.IsNull(reason1);
            Assert.IsFalse(graph.IsDoorOpen("D1"));

            Assert.IsTrue(graph.TryCompleteTerminal("T2", out string reason2));
            Assert.IsNull(reason2);
            Assert.IsTrue(graph.IsDoorOpen("D1"));
        }

        [Test]
        public void ControlDoor_StaysLockedUntilItsTerminalsDone()
        {
            var graph = SampleGraph();
            graph.TryCompleteTerminal("T1", out _);
            graph.TryCompleteTerminal("T2", out _);

            Assert.IsTrue(graph.IsDoorOpen("D1"));
            Assert.IsFalse(graph.IsDoorOpen("D_control"));
            Assert.IsFalse(graph.ControlRoomReachable);

            Assert.IsTrue(graph.TryCompleteTerminal("T_control", out _));
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
