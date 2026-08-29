using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// The spent-roster decision (<see cref="FieldTrickle.Resolve"/>) and the trickle flag's
    /// lifecycle. Both exist because of the 2026-08-28 wave-5 deadlock: the drip's start
    /// condition and the pay-zero flag's raise condition lived in two Game-layer places and
    /// disagreed, so the drip's own bodies held the flag down forever — 39 authored bodies,
    /// 93 paid kills, and no way to end the wave. The decision is one pure function now;
    /// these tests pin its table, the seam protocol, and the invariant that the flag never
    /// survives an Assault exit.
    /// </summary>
    public class FieldWaveResolutionTests
    {
        static RunSim Tethered(out MetaState meta)
        {
            meta = MetaState.CreateNew();
            var sim = new RunSim(meta, 1);
            sim.StartRun();
            sim.EngageTether();
            return sim;
        }

        // ================================================================
        //  The decision table
        // ================================================================

        [Test]
        public void Resolve_HoldsWhileTheRosterIsStillDelivering_EvenWithAShutoffOwed()
        {
            // Half of the old bug: the drip used to start the moment delivery finished its
            // bookkeeping, regardless of the rest of the decision.
            Assert.AreEqual(FieldWaveResolution.Hold,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: true, aliveCount: 0,
                    awaitsShutoff: true),
                "the wave resolved while bodies were still owed to the field");
        }

        [Test]
        public void Resolve_HoldsWhileBodiesAreAlive_EvenWithAShutoffOwed()
        {
            // The other half, and the order that matters: the flag zeroes CurrentKillAward,
            // so raising it over a live field would stop paying for the wave the player is
            // still fighting. StartTrickle must be unreachable while anything stands.
            Assert.AreEqual(FieldWaveResolution.Hold,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 3,
                    awaitsShutoff: true),
                "the trickle was offered over a field that still has live bodies");
        }

        [Test]
        public void Resolve_HoldsWhileAChainIsPending_AndOutsideALiveAssault()
        {
            Assert.AreEqual(FieldWaveResolution.Hold,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: true, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: false),
                "a deep-start chain's gap read as a cleared wave");
            Assert.AreEqual(FieldWaveResolution.Hold,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Prep,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: true),
                "Prep resolved a wave that is not running");
            Assert.AreEqual(FieldWaveResolution.Hold,
                FieldTrickle.Resolve(RunPhase.Lost, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: false),
                "a terminal phase resolved a wave");
        }

        [Test]
        public void Resolve_SpentAndEmpty_TrickleWhenShutoffOwed_ClearedOtherwise()
        {
            Assert.AreEqual(FieldWaveResolution.StartTrickle,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: true),
                "a wave owing its shutoff cleared instead of dripping — the corridor walk "
                + "just stopped mattering");
            Assert.AreEqual(FieldWaveResolution.Cleared,
                FieldTrickle.Resolve(RunPhase.Tethered, AssaultState.Assault,
                    chainPending: false, rosterDelivering: false, aliveCount: 0,
                    awaitsShutoff: false),
                "a wave owing nothing was held open with no way to end it");
        }

        // ================================================================
        //  The seam protocol, driven the way the spawner drives it
        // ================================================================

        [Test]
        public void TheWave5Timeline_DripPaysZero_AndTheShutoffEndsIt()
        {
            // The full lifecycle the 07:17 run could not finish, with the resolver's verdict
            // applied exactly as WaveSpawner.CheckAssaultCleared applies it.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "the draw did not go live");

            // Authored kills pay while the field is being fought.
            long banked = meta.BankedSalvage;
            sim.AwardKill();
            Assert.Greater(meta.BankedSalvage, banked, "an authored kill paid nothing");

            // Roster spent, field empty, shutoff owed → the verdict is StartTrickle, and
            // applying it is what makes drip bodies free.
            var verdict = FieldTrickle.Resolve(sim.Phase, sim.Assault, sim.ChainPending,
                rosterDelivering: false, aliveCount: 0, awaitsShutoff: true);
            Assert.AreEqual(FieldWaveResolution.StartTrickle, verdict);
            sim.SetFieldTrickle(true);
            Assert.AreEqual(0L, sim.CurrentKillAward,
                "a trickle body is quoted a price after the verdict was applied");

            // The shutoff is the exit: it ends the Assault, clears the flag WITHOUT any
            // Game-layer help, and the next wave pays again.
            Assert.IsTrue(sim.ShutOffFieldWave(), "the shutoff refused the wave it exists to end");
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "the Assault survived its shutoff");
            Assert.IsFalse(sim.FieldTrickleLive,
                "the trickle flag outlived the shutoff — the next wave's kills would pay zero");
            sim.TriggerDraw();
            Assert.Greater(sim.CurrentKillAward, 0L, "the next wave did not resume paying");
        }

        // ================================================================
        //  The flag never survives an Assault exit
        // ================================================================

        [Test]
        public void AbortAssault_ClearsTheTrickleFlag()
        {
            // Dev replay/jump aborts the Assault without clearing it as a wave. Before
            // 2026-08-28 this exit leaked the flag, and every kill in the redrawn wave paid
            // zero until a wave start happened to reset it.
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.SetFieldTrickle(true);
            sim.AbortAssault();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.FieldTrickleLive, "AbortAssault leaked the trickle flag");
        }

        [Test]
        public void NotifyAssaultCleared_ClearsTheTrickleFlag()
        {
            MetaState meta;
            var sim = Tethered(out meta);
            sim.TriggerDraw();
            sim.SetFieldTrickle(true);
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.FieldTrickleLive, "a cleared wave leaked the trickle flag");
        }
    }
}
