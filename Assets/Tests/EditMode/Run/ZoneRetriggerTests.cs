using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M78 composite retrigger: the sim launches the elected Waves consecutively
    /// under the per-trigger MaxWaves cap, and DemoRun's offer rules decide when the zone's
    /// deepest terminal offers it at all. The Unity routing (arming, election input, spawn
    /// activation, terminal completion) is GameController's and is pinned by source in
    /// FirstReturnSourceTests.</summary>
    public class ZoneRetriggerTests
    {
        static RunSim Tethered(out MetaState meta, int seed = 1)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        static List<WaveSpec> Record(RunSim sim)
        {
            var launched = new List<WaveSpec>();
            sim.WaveStarted += spec => launched.Add(spec);
            return launched;
        }

        [Test]
        public void Composite_LaunchesTheWholeZoneConsecutively()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var launched = Record(sim);

            Assert.IsTrue(sim.TriggerCompositeDraw(
                FirstTerminal.ZoneOneFirstWave, FirstTerminal.ZoneOneEndWave));
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(1, launched.Count, "wave 1 launches on the trigger");
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            Assert.IsTrue(sim.ChainPending, "wave 2 is owed — no Prep between them");

            // A clear report while the chain is pending is ignored, exactly like deep start.
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);

            sim.NotifyDeliveryComplete();
            sim.Tick(WaveDirector.InterWaveGap);
            Assert.AreEqual(2, launched.Count, "wave 2 follows on the delivery gap");
            Assert.AreEqual(2, sim.CurrentWaveIndex);

            sim.NotifyDeliveryComplete();
            Assert.IsFalse(sim.ChainPending, "the elected range is delivered");
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "one clear ends the composite");
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void PartialStart_LaunchesTheRemainderOnly()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            var launched = Record(sim);

            Assert.IsTrue(sim.TriggerCompositeDraw(
                FirstTerminal.ZoneOneEndWave, FirstTerminal.ZoneOneEndWave));
            Assert.AreEqual(1, launched.Count, "a single-wave election is one assault");
            Assert.AreEqual(FirstTerminal.ZoneOneEndWave, sim.CurrentWaveIndex,
                "the cursor starts at the elected wave, not at 1");
            Assert.IsFalse(sim.ChainPending);

            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
        }

        [Test]
        public void Composite_RefusedOutsidePrep_AndOverTheCap()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsFalse(sim.TriggerCompositeDraw(2, 1), "backwards range");
            Assert.IsFalse(sim.TriggerCompositeDraw(0, 1), "wave 0 does not exist");
            Assert.IsFalse(sim.TriggerCompositeDraw(1, WaveDirector.MaxWaves + 1),
                "past the per-trigger cap — refused, never silently clamped");

            sim.TriggerDraw();
            Assert.IsFalse(sim.TriggerCompositeDraw(1, 2), "not in Prep");
        }

        [Test]
        public void Composite_DoesNotDisturbNavDeepStartState()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            Assert.IsTrue(sim.TriggerCompositeDraw(2, 2));
            Assert.AreEqual(1, sim.StartWave,
                "a retrigger is not a NAV deep start — StartWave-gated restores keep meaning");
        }

        // ---- DemoRun offer rules ----

        static MetaState PostLossMeta()
        {
            var m = MetaState.CreateNew();
            m.HatchConversions = 1; // loss 1 banked its drip
            m.SetZoneDeepest(FirstTerminal.ZoneOneNumber, FirstTerminal.ZoneOneEndWave);
            return m;
        }

        [Test]
        public void Offer_DefaultsToComposite_WithPartialElectable()
        {
            var offer = DemoRun.RetriggerOffer(
                PostLossMeta(), FirstTerminal.SecondId,
                zoneWavesClearedThisRun: 0, doorOpen: false);
            Assert.IsTrue(offer.Available);
            Assert.AreEqual(FirstTerminal.ZoneOneNumber, offer.Zone);
            Assert.AreEqual(FirstTerminal.ZoneOneFirstWave, offer.CompositeStart);
            Assert.AreEqual(FirstTerminal.ZoneOneEndWave, offer.EndWave);
            Assert.AreEqual(FirstTerminal.ZoneOneEndWave, offer.PartialStart,
                "partial starts at the deepest cleared wave — at least one Wave refights");
            Assert.IsTrue(offer.PartialAvailable);
            Assert.AreEqual(2, offer.WavesFrom(offer.CompositeStart));
            Assert.AreEqual(1, offer.WavesFrom(offer.PartialStart));
        }

        [Test]
        public void Offer_PartialCollapses_WhenOnlyWaveOneWasCleared()
        {
            // ZoneDeepest is raise-only, so build the shallower save directly.
            var shallow = MetaState.CreateNew();
            shallow.HatchConversions = 1;
            shallow.SetZoneDeepest(FirstTerminal.ZoneOneNumber, 1);
            var offer = DemoRun.RetriggerOffer(shallow, FirstTerminal.SecondId, 0, false);
            Assert.IsTrue(offer.Available);
            Assert.IsFalse(offer.PartialAvailable,
                "a partial equal to the composite is no election");
        }

        [Test]
        public void Offer_OnlyAtTheZonesDeepestTerminal()
        {
            var m = PostLossMeta();
            Assert.IsFalse(DemoRun.RetriggerOffer(m, FirstTerminal.Id, 0, false).Available,
                "T1 keeps its plain start — the retrigger lives at the deepest terminal");
            Assert.IsFalse(DemoRun.RetriggerOffer(m, "T9", 0, false).Available);
            Assert.IsFalse(DemoRun.RetriggerOffer(null, FirstTerminal.SecondId, 0, false)
                .Available);
        }

        [Test]
        public void Offer_Withdrawn_WhenTheDoorIsOpen_OrTheZoneIsDoneThisRun()
        {
            var m = PostLossMeta();
            Assert.IsFalse(DemoRun.RetriggerOffer(m, FirstTerminal.SecondId, 0, true).Available,
                "a surviving bank opened the door — nothing to resume");
            Assert.IsFalse(DemoRun.RetriggerOffer(
                    m, FirstTerminal.SecondId, FirstTerminal.ZoneOneEndWave, false).Available,
                "zone complete this Run — the bank is the next step, not a refight");
        }

        [Test]
        public void Offer_NeverDuringTheFirstAttempt_OrOffTheDemo()
        {
            var first = MetaState.CreateNew();
            Assert.IsFalse(DemoRun.RetriggerOffer(first, FirstTerminal.SecondId, 0, false)
                .Available, "nothing cleared yet — nothing to retrigger");

            var midFirstRun = MetaState.CreateNew();
            midFirstRun.SetZoneDeepest(FirstTerminal.ZoneOneNumber, 1);
            Assert.IsFalse(DemoRun.RetriggerOffer(midFirstRun, FirstTerminal.SecondId, 1, false)
                .Available, "deepest written THIS Run is progress, not a loss to resume");

            var live = PostLossMeta();
            live.DemoActive = false;
            Assert.IsFalse(DemoRun.RetriggerOffer(live, FirstTerminal.SecondId, 0, false)
                .Available, "the campaign keeps its live terminals untouched");
        }

        [Test]
        public void ZoneOfWave_MapsZoneOne_AndFloorsDeeperWavesOut()
        {
            Assert.AreEqual(1, FirstTerminal.ZoneOfWave(1));
            Assert.AreEqual(1, FirstTerminal.ZoneOfWave(FirstTerminal.ZoneOneEndWave));
            Assert.AreEqual(2, FirstTerminal.ZoneOfWave(FirstTerminal.ZoneOneEndWave + 1));
        }
    }
}
