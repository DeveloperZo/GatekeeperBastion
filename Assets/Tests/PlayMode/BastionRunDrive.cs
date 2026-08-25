using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>Drives a live Run through the host-side verbs a player's inputs reach — the
    /// terminal trigger, the Hatch-bank window, the Master Control pull — inside the real
    /// Bastion scene. Waves are STRIPPED rather than fought: every fixture built on this
    /// proves the Run's wiring and progression, never its difficulty. Shared so the chain
    /// and the campaign spine speak one vocabulary instead of two drifting copies.</summary>
    internal static class BastionRunDrive
    {
        /// <summary>Range given to the host-side verbs. The tests stand where they load, so
        /// the reach check is deliberately not the thing under test.</summary>
        public const float ReachMetres = 1000f;

        /// <summary>Kills credited per Wave. Enough to bank a pile the window can convert;
        /// the exact figure is not a rule any assertion here reads.</summary>
        public const int KillsPerWave = 8;

        /// <summary>One field terminal, one Wave, cleared without fighting.</summary>
        public static IEnumerator RunSingleTerminal(
            GameController gc, RunSim sim, BastionLayout layout, string terminalId)
        {
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the bastion has no terminal " + terminalId + ".");
            Assert.IsTrue(
                gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
                terminalId + " refused its trigger.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault,
                terminalId + "'s trigger did not start a Wave.");

            yield return null; // one scene frame: the spawner emits into the real rooms
            AwardKills(gc);
            StripAndDeliver(sim);
            sim.NotifyAssaultCleared();

            Assert.AreEqual(AssaultState.Prep, sim.Assault,
                terminalId + "'s Wave did not hand the Run back to Prep.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                terminalId + " did not complete on its clear.");
        }

        /// <summary>The zone's whole Wave span on one trigger (the T3 composite).</summary>
        public static IEnumerator RunZoneComposite(
            GameController gc, RunSim sim, BastionLayout layout, int zone)
        {
            string terminalId = DemoZones.TerminalIdAt(zone, 0);
            BastionTerminal terminal = layout.FindTerminal(terminalId);
            Assert.IsNotNull(terminal, "the bastion has no terminal " + terminalId + ".");
            Assert.IsTrue(
                gc.TryNetStartTerminal(terminalId, terminal.transform.position, ReachMetres),
                terminalId + " refused its trigger.");

            int endWave = DemoZones.EndWave(zone);
            for (int wave = DemoZones.FirstWave(zone); wave < endWave; wave++)
            {
                yield return null;
                AwardKills(gc);
                StripAndDeliver(sim);
                sim.NotifyAssaultCleared();
                sim.Tick(WaveDirector.InterWaveGap);
            }

            yield return null;
            AwardKills(gc);
            StripAndDeliver(sim);
            sim.NotifyAssaultCleared();

            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the composite did not clear.");
            Assert.IsTrue(layout.IsTerminalDone(terminalId),
                terminalId + " did not complete.");
        }

        /// <summary>Open a bank window through the host-side verb and survive its whole
        /// timer in one sim advance, so nothing in the scene can breach the Hatch.</summary>
        public static IEnumerator SurviveBank(GameController gc, RunSim sim)
        {
            Assert.Greater(sim.TotalUnbanked, 0L,
                "the field Waves banked nothing — the window would refuse on unbanked=0.");
            HatchStation hatch = HatchStation.Instance;
            Vector3 at = hatch == null ? Vector3.zero : hatch.transform.position;
            Assert.IsTrue(gc.TryNetStartHatchBank(at, ReachMetres), "the bank refused.");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            sim.Tick(HatchBank.DurationSeconds);
            yield return null;

            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "the survived window ended the Run.");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the window did not close.");
        }

        /// <summary>The Master Control pull and its whole sequence: the Terminal half's Waves,
        /// the roll into the Hatch half on the follow-up timer, and the survived finale window
        /// that completes the Run. Leaves the sim in <see cref="RunPhase.Escaped"/>.</summary>
        public static IEnumerator WinThroughMasterControl(
            GameController gc, RunSim sim, BastionLayout layout)
        {
            BastionTerminal master = layout.FindMaster();
            Assert.IsNotNull(master, "the Control Room has no Master terminal.");
            Assert.IsTrue(
                gc.TryNetStartMaster(master.TerminalId, master.transform.position, ReachMetres),
                "the Master pull refused.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the pull did not start a Wave.");
            Assert.IsTrue(sim.MasterSequenceLive, "the pull did not arm the sequence.");

            for (int wave = 0; wave < MasterControl.TerminalWaves; wave++)
            {
                yield return null;
                AwardKills(gc);
                StripAndDeliver(sim);
                sim.NotifyAssaultCleared();
                sim.Tick(WaveDirector.InterWaveGap + 0.1f);
            }

            Assert.AreEqual(WaveMode.HatchBank, sim.Mode,
                "the Terminal half did not roll into the Hatch half.");
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the finale window did not draw.");
            Assert.Greater(sim.TotalUnbanked, 0L,
                "the Terminal half banked nothing — the finale would dump an empty pile.");

            // A Hatch window is SURVIVED, not kill-cleared: NotifyAssaultCleared is refused
            // while its timer runs, so strip the bodies and let the window expire.
            yield return null;
            StripAndDeliver(sim);
            sim.Tick(HatchBank.DurationSeconds + 0.1f);
            yield return null;

            Assert.AreEqual(RunPhase.Escaped, sim.Phase,
                "surviving the finale Hatch wave must complete the Run.");
        }

        public static void StripAndDeliver(RunSim sim)
        {
            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            if (spawner != null) spawner.DespawnAllLive();
            sim.NotifyDeliveryComplete();
        }

        public static void AwardKills(GameController gc)
        {
            for (int i = 0; i < KillsPerWave; i++) gc.GatekeeperKilled();
        }
    }
}
