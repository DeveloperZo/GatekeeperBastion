using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The authored loss that sells the Primary: a Run that ends Lost INSIDE zone 2's
    /// Hatch-bank window writes the gate flag, and nothing else does — not a loss during that
    /// zone's field Waves, not zone 1's bank, not a Docked end
    /// (Docs/Demo - Single/5-repeat-to-control-room.md). These are behaviour tests over RunSim; the
    /// source-shape pins live in FirstReturnSourceTests.</summary>
    public class ZoneTwoBankLossTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = new MetaState();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        /// <summary>Clear field Waves up to and including <paramref name="throughWave"/>, then
        /// open a Hatch-bank window on the pile they earned.</summary>
        static void ClearFieldThrough(RunSim sim, int throughWave)
        {
            for (int w = 1; w <= throughWave; w++)
            {
                sim.SeekWave(w);
                sim.TriggerDraw();
                for (int i = 0; i < 8; i++) sim.AwardKill();
                sim.NotifyAssaultCleared();
            }
        }

        static void OpenBankWindow(RunSim sim)
        {
            sim.TriggerDraw(WaveMode.HatchBank);
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the bank window is live");
            Assert.AreEqual(WaveMode.HatchBank, sim.Mode);
        }

        [Test]
        public void HatchBreachInsideZoneTwosWindow_WritesTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            OpenBankWindow(sim);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(meta.DemoZone2BankLost,
                "the bank that fell in zone 2 is what puts a Primary on the rack");
        }

        [Test]
        public void PlayerDownInsideZoneTwosWindow_WritesTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            OpenBankWindow(sim);

            sim.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsTrue(meta.DemoZone2BankLost, "player-down inside the window is the same loss");
        }

        [Test]
        public void LossDuringZoneTwosFieldWaves_DoesNotWriteTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.SeekWave(DemoZones.EndWave(2));
            sim.TriggerDraw();
            Assert.AreEqual(WaveMode.Terminal, sim.Mode, "a field Wave, not a bank window");

            sim.NotifyPlayerDown();

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "dying to the field is a loss, but it is not the bank falling");
            Assert.AreEqual(1, meta.PodEjections, "it still counts as an ending");
        }

        [Test]
        public void ZoneOnesBankFalling_DoesNotWriteTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(1));
            OpenBankWindow(sim);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "zone 1's bank is M76's authored loss and sells nothing on the Primary rack");
        }

        [Test]
        public void BothLossKinds_StillCountAnEnding()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            OpenBankWindow(sim);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(1, meta.PodEjections,
                "PodEjections is untouched by this milestone — it counts endings either way");
        }

        [Test]
        public void SurvivingTheWindow_LeavesTheGateShut()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            OpenBankWindow(sim);

            sim.Tick(HatchBank.DurationSeconds);

            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the window closed on its timer");
            Assert.AreNotEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "surviving is the path that opens D2, not the path that sells the Primary");
        }

        [Test]
        public void DockingOutOfASurvivedWindow_LeavesTheGateShut()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            OpenBankWindow(sim);
            sim.Tick(HatchBank.DurationSeconds);

            Assert.IsTrue(sim.DockRun(), "a survived window offers the dock");

            Assert.AreEqual(RunPhase.Returned, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost, "a Returned end is not a loss");
            Assert.AreEqual(0, meta.PodEjections);
        }

        [Test]
        public void AWaveDeeperThanZoneTwo_DoesNotWriteTheGate()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            // Terminals past the authored table draw past the last zone's end Wave, and so
            // does the Master Control terminal half. ZoneOfWave FLOORS those onto zone 2 for
            // logging; the gate must not, or the finale's lost window sells a Primary the
            // player already owns (Docs/Demo - Single/7-hatch-finale.md).
            int pastTheTable = DemoZones.EndWave(DemoZones.ZoneCount) + 1;
            Assert.AreEqual(DemoZones.ZoneCount, DemoZones.ZoneOfWave(pastTheTable),
                "the floor is still the right read for a log");
            Assert.IsFalse(DemoZones.WaveIsIn(2, pastTheTable),
                "but the gate asks whether the Wave BELONGS to the zone");

            ClearFieldThrough(sim, DemoZones.EndWave(2));
            // SeekWave clamps to MaxWaves, so it cannot reach past the table. Drawing onward
            // can: a Terminal draw may continue past MaxWaves and only the COMPOSITION is
            // clamped, so the Wave counter genuinely runs deeper.
            sim.TriggerDraw();
            for (int i = 0; i < 8; i++) sim.AwardKill();
            sim.NotifyAssaultCleared();
            Assert.AreEqual(pastTheTable, sim.CurrentWaveIndex,
                "the Run is now on a Wave the zone table does not author");
            OpenBankWindow(sim);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "a bank lost past zone 2's Waves is not zone 2's bank falling");
        }

        [Test]
        public void OffTheDemo_TheGateIsNeverWritten()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            meta.DemoActive = false;
            ClearFieldThrough(sim, DemoZones.EndWave(2));
            OpenBankWindow(sim);

            sim.Ship.Hatch.Damage(99999f);

            Assert.AreEqual(RunPhase.Lost, sim.Phase);
            Assert.IsFalse(meta.DemoZone2BankLost,
                "a campaign save past the demo has no ladder to open");
        }
    }
}
