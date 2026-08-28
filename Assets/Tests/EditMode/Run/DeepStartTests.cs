using System.Collections.Generic;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M14 deep start: DeepestWaveReached clamp, clear-only rise, chained delivery
    /// order and InterWaveGap, and per-kill salvage unchanged by start wave.</summary>
    public class DeepStartTests
    {
        static RunSim Begin(MetaState meta, int startWave, int seed = 1)
        {
            var sim = new RunSim(meta, seed);
            sim.StartRun(startWave);
            sim.EngageTether();
            return sim;
        }

        static void DeliverAndChainNext(RunSim sim)
        {
            Assert.IsTrue(sim.ChainPending, "expected a chained follow-up still pending");
            sim.NotifyDeliveryComplete();
            Assert.IsTrue(sim.ChainPending, "gap should keep the chain pending");
            // Tick exactly the gap — next wave launches on the frame the timer hits zero.
            sim.Tick(WaveDirector.InterWaveGap);
        }

        [Test]
        public void DeepestWaveReached_DefaultsToOne_AndPersistsAcrossJson()
        {
            var meta = MetaState.CreateNew();
            Assert.AreEqual(1, meta.DeepestWaveReached);

            meta.DeepestWaveReached = 3;
            var loaded = MetaState.FromSnapshotJson(meta.ToSnapshotJson());
            Assert.AreEqual(3, loaded.DeepestWaveReached);

            // Absent field → 1. Depth lives on the realm, so these are realm documents.
            var absent = RealmState.FromJson(
                "{\"SaveVersion\":13,\"BankedSalvage\":0,\"TotalRuns\":0}");
            Assert.IsNotNull(absent);
            Assert.AreEqual(1, absent.DeepestWaveReached);

            // Written 0 → 1.
            var zero = RealmState.FromJson(
                "{\"SaveVersion\":13,\"DeepestWaveReached\":0}");
            Assert.IsNotNull(zero);
            Assert.AreEqual(1, zero.DeepestWaveReached);
        }

        [Test]
        public void StartRun_ClampsToDeepestWaveReached_AndNeverAboveMaxWaves()
        {
            var meta = MetaState.CreateNew();
            meta.DeepestWaveReached = 2;

            var sim = Begin(meta, 99);
            Assert.AreEqual(2, sim.StartWave, "refuses anything above DeepestWaveReached");

            meta.DeepestWaveReached = 1;
            var sim2 = Begin(meta, 0);
            Assert.AreEqual(1, sim2.StartWave, "floor is 1");

            meta.DeepestWaveReached = 99;
            var sim3 = Begin(meta, 99);
            Assert.AreEqual(WaveDirector.MaxWaves, sim3.StartWave, "also capped by MaxWaves");
        }

        [Test]
        public void DeepestWaveReached_RisesOnlyOnClear_NeverOnReachOrDeath()
        {
            var meta = MetaState.CreateNew();
            var sim = Begin(meta, 1);

            sim.TriggerDraw(); // wave 1 live — reached, not cleared
            Assert.AreEqual(1, meta.DeepestWaveReached);
            sim.NotifyPlayerDown();
            Assert.AreEqual(1, meta.DeepestWaveReached, "dying on a wave does not unlock it");

            var sim2 = Begin(meta, 1);
            sim2.TriggerDraw();
            sim2.NotifyAssaultCleared(); // cleared wave 1
            Assert.AreEqual(1, meta.DeepestWaveReached, "wave 1 clear leaves deepest at 1");

            sim2.TriggerDraw();
            sim2.NotifyAssaultCleared(); // cleared wave 2
            Assert.AreEqual(2, meta.DeepestWaveReached);

            sim2.TriggerDraw();
            sim2.NotifyPlayerDown(); // died on wave 3
            Assert.AreEqual(2, meta.DeepestWaveReached, "death on 3 does not raise deepest to 3");
        }

        [Test]
        public void DeepestWaveReached_NeverLowers()
        {
            var meta = MetaState.CreateNew();
            meta.DeepestWaveReached = 4;
            var sim = Begin(meta, 1);
            sim.TriggerDraw();
            sim.NotifyAssaultCleared(); // cleared wave 1 only
            Assert.AreEqual(4, meta.DeepestWaveReached);
        }

        [Test]
        public void DeepStart_ChainsWavesInOrder_WithInterWaveGap_NoPrepBetween()
        {
            var meta = MetaState.CreateNew();
            meta.DeepestWaveReached = 3;
            var sim = Begin(meta, 3);
            var specs = new List<WaveSpec>();
            int ended = 0;
            sim.WaveStarted += w => specs.Add(w);
            sim.AssaultEnded += () => ended++;

            // The four body counts below were 18 / 30 / 42 / 54 and the 2026-08-27 roster cut
            // moved all four. They are read off the director now: what this fixture claims is
            // that a deep start replays waves 1..N IN ORDER, not what any of them contains.
            var director = new WaveDirector();

            sim.TriggerDraw();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual(director.NextWave(0, 0).enemyCount, specs[0].enemyCount);
            Assert.AreEqual(1, sim.CurrentWaveIndex);
            Assert.IsTrue(sim.ChainPending);

            // Mid-delivery: assault must not clear even if somehow told.
            sim.NotifyAssaultCleared();
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(0, ended);

            DeliverAndChainNext(sim);
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual(director.NextWave(1, 0).enemyCount, specs[1].enemyCount);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            Assert.AreEqual(AssaultState.Assault, sim.Assault, "no Prep between chained waves");

            DeliverAndChainNext(sim);
            Assert.AreEqual(3, specs.Count);
            Assert.AreEqual(director.NextWave(2, 0).enemyCount, specs[2].enemyCount);
            Assert.AreEqual(3, sim.CurrentWaveIndex);
            Assert.IsFalse(sim.ChainPending, "last chained wave is live; no more auto-launches");

            // Clearing the whole chain unlocks deepest 3 and returns to Prep.
            sim.NotifyAssaultCleared();
            Assert.AreEqual(1, ended);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(3, meta.DeepestWaveReached);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);

            // After the chain, the next draw is wave 4 — normal single assault.
            sim.TriggerDraw();
            Assert.AreEqual(4, specs.Count);
            Assert.AreEqual(director.NextWave(3, 0).enemyCount, specs[3].enemyCount);
            for (int i = 1; i < specs.Count; i++)
                Assert.Greater(specs[i].enemyCount, specs[i - 1].enemyCount,
                    "the chain must replay the waves in ORDER, wave " + i);
            Assert.IsFalse(sim.ChainPending);
        }

        [Test]
        public void DeepStart_InterWaveGap_IsExactlyThreeSeconds()
        {
            var meta = MetaState.CreateNew();
            meta.DeepestWaveReached = 2;
            var sim = Begin(meta, 2);
            int waves = 0;
            sim.WaveStarted += _ => waves++;

            sim.TriggerDraw();
            Assert.AreEqual(1, waves);
            sim.NotifyDeliveryComplete();

            sim.Tick(WaveDirector.InterWaveGap - 0.01f);
            Assert.AreEqual(1, waves, "must not launch early");
            sim.Tick(0.01f);
            Assert.AreEqual(2, waves, "launches on the gap boundary");
            Assert.AreEqual(WaveDirector.InterWaveGap, 3f, 1e-6f);
        }

        [Test]
        public void DeepStart_PerKillSalvage_UnchangedByStartWave()
        {
            var a = MetaState.CreateNew();
            a.DeepestWaveReached = 3;
            var normal = Begin(a, 1, seed: 10);
            normal.TriggerDraw();
            float vNormal = normal.CurrentKillValue;
            normal.AwardKill();

            var b = MetaState.CreateNew();
            b.DeepestWaveReached = 3;
            var deep = Begin(b, 3, seed: 10);
            deep.TriggerDraw();
            float vDeep = deep.CurrentKillValue;
            deep.AwardKill();

            Assert.AreEqual(vNormal, vDeep, 1e-6f);
            Assert.AreEqual(normal.Unbanked, deep.Unbanked);
            Assert.AreEqual(a.BankedSalvage, b.BankedSalvage);
            Assert.AreEqual(MetaUpgrades.SalvagePerKill(), vNormal, 1e-5f,
                "dial 0 scavenging 0 baseline is the flat price of a kill, start wave or not");
        }

        [Test]
        public void DeepStart_ClearingWaveFive_StaysPrep()
        {
            var meta = MetaState.CreateNew();
            meta.DeepestWaveReached = 5;
            var sim = Begin(meta, 5);
            var specs = new List<WaveSpec>();
            sim.WaveStarted += w => specs.Add(w);

            sim.TriggerDraw();
            // Deliver the whole 1..5 chain.
            for (int i = 0; i < 4; i++)
                DeliverAndChainNext(sim);
            Assert.AreEqual(5, specs.Count);
            Assert.AreEqual(5, sim.CurrentWaveIndex);
            Assert.IsFalse(sim.ChainPending);

            sim.NotifyAssaultCleared();
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.IsFalse(sim.AllWavesCleared);
            Assert.AreEqual(5, meta.DeepestWaveReached);
            Assert.AreEqual(0, meta.CleanExits);
        }

        [Test]
        public void DeepStart_ChainedDeliverySpan_AtWave3_IsAboutNinetyOneSeconds()
        {
            // Third name for this pin in one day, and the walk is the whole story:
            //   94.6 s  — roster 18+12i, gap 4.2 + 0.20i
            //   54.6 s  — roster cut to 11+7i, gap untouched. A wave is delivered in
            //             ceil(count/groupSize) groups, so a ~40% smaller roster at unchanged
            //             group width (3/4/5/6/7) shortened every window by ~40%. Body arrivals
            //             barely moved, so nothing in a bodies-per-second reading showed it —
            //             but bodies carry 5/3 the HP now, so the chain was landing the SAME
            //             total HP in 58% of the wall clock. That is the exact stacking the
            //             03:18 widening had been bought to stop, undone by a change to a
            //             different constant.
            //   91.1 s  — gap re-derived to 7.3 + 0.40i to hold HP-per-second at the pre-cut
            //             figures. Above the 85.4 s that preceded the widening, which is the
            //             direction that was intended all along.
            //
            // Criterion 7 arithmetic: three delivery windows plus two 3 s inter-wave gaps —
            // 21.9 + 3 + 30.8 + 3 + 32.4 = 91.1 s.
            //
            // THE ROSTER AND THE GAP ARE ONE LEVER. This pin exists so that moving either one
            // alone cannot happen quietly again.
            //
            // This pins the sim-side schedule; the play log is the gate for the real spawn path.
            var director = new WaveDirector();
            float span = 0f;
            for (int w = 0; w < 3; w++)
            {
                var spec = director.NextWave(w, 0);
                int groups = (spec.enemyCount + spec.groupSize - 1) / spec.groupSize;
                span += (groups - 1) * spec.spawnInterval;
                if (w < 2) span += WaveDirector.InterWaveGap;
            }
            Assert.AreEqual(91.1f, span, 0.05f);
        }
    }
}
