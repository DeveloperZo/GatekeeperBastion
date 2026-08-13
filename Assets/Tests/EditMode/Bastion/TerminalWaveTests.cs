using GatekeeperBastion.Sim;
using NUnit.Framework;

namespace GatekeeperBastion.Tests
{
    public class TerminalWaveTests
    {
        [Test]
        public void TriggerDraw_SetsModeTerminal()
        {
            var meta = new MetaState();
            var sim = new RunSim(meta, seed: 1);
            sim.StartRun(1);
            sim.EngageTether();
            sim.TriggerDraw();
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
        }

        [Test]
        public void UnlockGraph_CompletesOnlyAfterTerminalMarked()
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
            Assert.IsTrue(graph.IsDoorOpen("D1"));
        }
    }
}
