using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>A zone's last wave belongs to its terminal (Zo, 2026-09-02; M115). The Hatch
    /// draws the opening span, the terminal press draws the last wave into the live field, and
    /// the shutoff arms only once that wave is down. Sim-side rules only; the Hatch/terminal
    /// routing is GameController's and the spans it draws are pinned by source shape.</summary>
    public class ZoneFinalWaveTests
    {
        static RunSim Tethered(out MetaState meta)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, 1);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static int Zone1First => DemoZones.FirstWave(1);
        static int Zone1End => DemoZones.EndWave(1);

        [Test]
        public void TheOpeningIsNotTheLastWave_AndTheTerminalDrawsIt()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.TriggerCompositeDraw(Zone1First, Zone1End - 1), "the Hatch's span");
            Assert.IsFalse(sim.ZoneFinalWaveDrawn, "the opening span is not the last wave");
            Assert.IsFalse(sim.ZoneShutoffReady, "and nothing is ready to shut off");
            Assert.IsTrue(sim.CanCallZoneFinalWave, "the terminal may call the last wave");

            Assert.IsTrue(sim.CallZoneFinalWave());
            Assert.AreEqual(Zone1End, sim.CurrentWaveIndex, "the zone's last wave was drawn");
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(WaveMode.Terminal, sim.Mode);
            Assert.IsTrue(sim.ZoneFinalWaveDrawn);
            Assert.IsFalse(sim.ChainPending, "the last wave chains nothing");
            Assert.IsFalse(sim.CanCallZoneFinalWave, "it can be called once");
            Assert.AreEqual(Zone1End, meta.DeepestWaveEntered, "entered rises on the draw");
        }

        [Test]
        public void TheCall_IsRefused_BeforeTheOpeningHasRun_AndAfterTheLastWave()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsFalse(sim.CanCallZoneFinalWave, "nothing has been drawn at the Hatch");
            Assert.IsFalse(sim.CallZoneFinalWave());
            Assert.AreEqual(0, sim.CurrentWaveIndex);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);

            sim.TriggerCompositeDraw(Zone1First, Zone1End);   // the whole zone, old style
            Assert.IsFalse(sim.CanCallZoneFinalWave, "a chain still owes the last wave");
            sim.NotifyDeliveryComplete();
            sim.Tick(WaveDirector.InterWaveGap + 0.01f);      // the chain launches wave 2
            Assert.IsTrue(sim.ZoneFinalWaveDrawn);
            Assert.IsFalse(sim.CanCallZoneFinalWave, "the last wave is already on the field");
        }

        [Test]
        public void TheCall_ClearsTheDrip_AndKeepsTheAssaultLive()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerCompositeDraw(Zone1First, Zone1End - 1);
            sim.SetFieldTrickle(true);   // the opening span is spent; the lane is dripping
            Assert.AreEqual(0L, sim.CurrentKillAward, "drip bodies pay nothing");

            Assert.IsTrue(sim.CallZoneFinalWave());
            Assert.IsFalse(sim.FieldTrickleLive, "a fresh wave clears the drip");
            Assert.Greater(sim.CurrentKillAward, 0L, "the last wave's bodies pay");
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
        }

        [Test]
        public void TheShutoff_ArmsWhenTheLastWaveIsDown_AndEveryExitClearsIt()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerCompositeDraw(Zone1First, Zone1End - 1);
            sim.ArmFieldShutoff();
            Assert.IsFalse(sim.FieldShutoffArmed, "nothing arms before the last wave is drawn");

            sim.CallZoneFinalWave();
            Assert.IsFalse(sim.ZoneShutoffReady, "drawn is not down");
            sim.ArmFieldShutoff();
            Assert.IsTrue(sim.FieldShutoffArmed);
            Assert.IsTrue(sim.ZoneShutoffReady, "down arms the terminal");

            Assert.IsTrue(sim.ShutOffFieldWave(), "the armed shutoff closes the zone");
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.FieldShutoffArmed, "the shutoff exit clears the arm");

            sim.TriggerCompositeDraw(Zone1First, Zone1End);
            sim.NotifyDeliveryComplete();
            sim.Tick(WaveDirector.InterWaveGap + 0.01f);      // the chain launches wave 2
            sim.ArmFieldShutoff();
            Assert.IsTrue(sim.FieldShutoffArmed);
            sim.AbortAssault();
            Assert.IsFalse(sim.FieldShutoffArmed, "an abort clears the arm too");
        }

        [Test]
        public void InPrep_WithTheLastWaveDrawn_TheShutoffIsReady()
        {
            // A retrigger that elected only the last wave draws it at the Hatch; when its
            // field is cleared with nothing owed the sim is in Prep and the shutoff must not
            // wait on an arm that no spawner verdict will raise.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerCompositeDraw(Zone1End, Zone1End);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsTrue(sim.ZoneFinalWaveDrawn);
            Assert.IsTrue(sim.ZoneShutoffReady, "Prep with the last wave drawn is ready");
        }

        [Test]
        public void ZoneTwo_CallsWaveFive()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            int first = DemoZones.FirstWave(2), end = DemoZones.EndWave(2);
            Assert.IsTrue(sim.TriggerCompositeDraw(first, end - 1), "waves 3 and 4 at the Hatch");
            Assert.IsFalse(sim.CanCallZoneFinalWave, "the opening chain is still pending");
            sim.NotifyDeliveryComplete();
            sim.Tick(WaveDirector.InterWaveGap + 0.01f);
            Assert.AreEqual(end - 1, sim.CurrentWaveIndex, "wave 4 launched off the chain");
            Assert.IsTrue(sim.CanCallZoneFinalWave, "now the terminal may call wave 5");
            Assert.IsTrue(sim.CallZoneFinalWave());
            Assert.AreEqual(end, sim.CurrentWaveIndex);
        }

        [Test]
        public void Resolve_ArmsInsteadOfDripping_OnceTheLastWaveIsDrawn()
        {
            Assert.AreEqual(FieldWaveResolution.StartTrickle,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: true, finalWaveDrawn: false),
                "the opening span spent and empty drips until the terminal is pressed");
            Assert.AreEqual(FieldWaveResolution.ArmShutoff,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: true, finalWaveDrawn: true),
                "the last wave spent and empty arms the terminal — no drip");
            Assert.AreEqual(FieldWaveResolution.Cleared,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: false, finalWaveDrawn: true),
                "nothing owed is still a clear");
        }

        [Test]
        public void MayCallFinalWave_IsTheTerminalsQuestion()
        {
            string t = DemoZones.TerminalIdAt(1, 0);
            Assert.IsTrue(FieldShutoff.MayCallFinalWave(RunPhase.Tethered, runIsOver: false,
                WaveMode.Terminal, t, terminalDone: false, currentWaveIndex: Zone1End - 1));
            Assert.IsFalse(FieldShutoff.MayCallFinalWave(RunPhase.Tethered, runIsOver: false,
                WaveMode.Terminal, t, terminalDone: false, currentWaveIndex: Zone1End),
                "already drawn");
            Assert.IsFalse(FieldShutoff.MayCallFinalWave(RunPhase.Tethered, runIsOver: false,
                WaveMode.Terminal, t, terminalDone: false, currentWaveIndex: Zone1First - 1),
                "the opening has not run");
            Assert.IsFalse(FieldShutoff.MayCallFinalWave(RunPhase.Tethered, runIsOver: false,
                WaveMode.Terminal, t, terminalDone: true, currentWaveIndex: Zone1End - 1),
                "a done terminal calls nothing");
            Assert.IsFalse(FieldShutoff.MayCallFinalWave(RunPhase.Tethered, runIsOver: false,
                WaveMode.HatchBank, t, terminalDone: false, currentWaveIndex: Zone1End - 1),
                "not inside a Convert window");
            Assert.IsFalse(FieldShutoff.MayCallFinalWave(RunPhase.Tethered, runIsOver: false,
                WaveMode.Terminal, null, terminalDone: false, currentWaveIndex: Zone1End - 1),
                "off the zone table");
        }
    }
}
