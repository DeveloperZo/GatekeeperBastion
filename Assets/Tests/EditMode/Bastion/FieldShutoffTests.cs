using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// The shutoff verb's availability rule (<see cref="FieldShutoff.MayRun"/>). The verb
    /// ends a field wave at its terminal, and it is available in Assault AND in Prep — the
    /// signature deliberately carries no AssaultState, so no caller can re-grow the
    /// 2026-08-28 gate (`if (Assault != Prep) return;` above the handler) at this layer.
    /// The prompt and the input path both call this predicate; these tests are what pins
    /// the rule when either caller is rewired.
    /// </summary>
    public class FieldShutoffTests
    {
        static string Zone1Terminal => DemoZones.TerminalIdAt(1, 0);

        static int Zone1Ready => DemoZones.FirstWave(1);

        [Test]
        public void MayRun_OffersTheShutoff_OnceTheZoneHasRun()
        {
            Assert.IsTrue(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: false,
                    WaveMode.Terminal, Zone1Terminal, terminalDone: false,
                    currentWaveIndex: Zone1Ready),
                "the shutoff is not offered on a zone whose wave is running");
        }

        [Test]
        public void MayRun_RefusesAZoneNobodyStarted()
        {
            // The shutoff ends a wave that began at the Hatch — pressing it on an unstarted
            // zone would let the player skip the lane entirely.
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: false,
                    WaveMode.Terminal, Zone1Terminal, terminalDone: false,
                    currentWaveIndex: Zone1Ready - 1),
                "the shutoff was offered on a zone with no wave run at it");
        }

        [Test]
        public void MayRun_RefusesDoneTerminals_ConvertWindows_AndDeadRuns()
        {
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: false,
                    WaveMode.Terminal, Zone1Terminal, terminalDone: true,
                    currentWaveIndex: Zone1Ready),
                "a completed terminal still offered its shutoff");
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: false,
                    WaveMode.HatchBank, Zone1Terminal, terminalDone: false,
                    currentWaveIndex: Zone1Ready),
                "a Convert window offered an off switch it does not have");
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: true,
                    WaveMode.Terminal, Zone1Terminal, terminalDone: false,
                    currentWaveIndex: Zone1Ready),
                "a dead run offered the shutoff");
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Lost, runIsOver: false,
                    WaveMode.Terminal, Zone1Terminal, terminalDone: false,
                    currentWaveIndex: Zone1Ready),
                "a terminal phase offered the shutoff");
        }

        [Test]
        public void MayRun_RefusesIdsOffTheZoneTable()
        {
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: false,
                    WaveMode.Terminal, null, terminalDone: false, currentWaveIndex: 99),
                "a null id was offered a zone shutoff");
            Assert.IsFalse(FieldShutoff.MayRun(RunPhase.Tethered, runIsOver: false,
                    WaveMode.Terminal, "NOT-A-TERMINAL", terminalDone: false,
                    currentWaveIndex: 99),
                "an id outside the zone table was offered a zone shutoff");
        }

        [Test]
        public void WavesHaveRun_EnforcesTheZoneRule_AndWaivesItOffTheTable()
        {
            Assert.IsFalse(FieldShutoff.WavesHaveRun(Zone1Terminal, Zone1Ready - 1),
                "an unstarted zone counted as run");
            Assert.IsTrue(FieldShutoff.WavesHaveRun(Zone1Terminal, Zone1Ready),
                "a started zone did not count as run");
            // Off the table (Master Control, finale, null): no zone rule to enforce — the
            // caller's OTHER checks decide, exactly as ShutOffTerminal's refusal path expects.
            Assert.IsTrue(FieldShutoff.WavesHaveRun(null, 0),
                "an off-table id was held to a zone rule it does not have");
        }
    }
}
