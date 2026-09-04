using System.Collections.Generic;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The practice rule: a range draw launches the named wave in the named
    /// shape with a seeded beat, pays nothing, cannot lose the Run at the Hatch, is refused
    /// off practice, and hands the sim back to Prep with nothing owed when the last body
    /// falls.</summary>
    public class PracticeWaveTests
    {
        static RunSim PracticeSim(MetaState meta, int seed = 7)
        {
            var sim = new RunSim(meta, seed);
            sim.StartRun();
            sim.EngageTether();
            sim.EnterPracticeMode();
            Assert.IsTrue(sim.PracticeMode);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            return sim;
        }

        static void AssertCounts(WaveSpec expected, WaveSpec actual)
        {
            Assert.AreEqual(expected.enemyCount, actual.enemyCount, "enemyCount");
            Assert.AreEqual(expected.defaultCount, actual.defaultCount, "defaultCount");
            Assert.AreEqual(expected.hunterCount, actual.hunterCount, "hunterCount");
            Assert.AreEqual(expected.thiefCount, actual.thiefCount, "thiefCount");
            Assert.AreEqual(expected.cloakerCount, actual.cloakerCount, "cloakerCount");
        }

        [Test]
        public void Draw_LaunchesTheNamedWave_InTheNamedShape_WithACadenceSchedule()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            var specs = new List<WaveSpec>();
            sim.WaveStarted += w => specs.Add(w);

            Assert.IsTrue(sim.DrawPracticeWave(5, WaveShape.Convoy));
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(5, sim.CurrentWaveIndex, "the draw seeks the wave it names");
            Assert.AreEqual(1, specs.Count);

            // Wave 5's own roster, re-shaped as a Convoy: the same four counts WaveShapes
            // writes from the same roster index, and the size is untouched.
            WaveSpec own = new WaveDirector().NextWave(4, 0, 1);
            WaveSpec expected = WaveShapes.Apply(own, WaveShape.Convoy, 4);
            AssertCounts(expected, specs[0]);
            Assert.Greater(specs[0].thiefCount, 0, "a Convoy carries Thieves");
            Assert.AreEqual(0, specs[0].hunterCount, "and never a Hunter");
            Assert.AreEqual(own.enemyHealthMultiplier, specs[0].enemyHealthMultiplier, 1e-6f);

            int groups = WaveCadence.GroupsFor(specs[0].enemyCount, specs[0].groupSize);
            Assert.Greater(groups, 1);
            Assert.IsNotNull(specs[0].groupGaps, "the beat is seeded at launch");
            Assert.AreEqual(groups - 1, specs[0].groupGaps.Length);
            Assert.IsFalse(sim.ChainPending, "one wave, nothing chained");
            Assert.IsFalse(sim.FieldTrickleLive);
            Assert.IsFalse(sim.ShapeOverride.HasValue, "the override is consumed by the launch");
        }

        [Test]
        public void Draw_WithNoShape_TakesTheWavesOwn()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            var specs = new List<WaveSpec>();
            sim.WaveStarted += w => specs.Add(w);

            Assert.IsTrue(sim.DrawPracticeWave(2, null));
            Assert.AreEqual(2, sim.CurrentWaveIndex);
            AssertCounts(new WaveDirector().NextWave(1, 0, 1), specs[0]);
        }

        [Test]
        public void Draw_AsMass_IsAllDefault()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            var specs = new List<WaveSpec>();
            sim.WaveStarted += w => specs.Add(w);

            Assert.IsTrue(sim.DrawPracticeWave(4, WaveShape.Mass));
            Assert.AreEqual(specs[0].enemyCount, specs[0].defaultCount);
            Assert.AreEqual(0, specs[0].hunterCount + specs[0].thiefCount + specs[0].cloakerCount);
        }

        [Test]
        public void Kill_PaysZero()
        {
            var meta = MetaState.CreateNew();
            var sim = PracticeSim(meta);
            Assert.IsTrue(sim.DrawPracticeWave(3, null));

            Assert.AreEqual(0L, sim.CurrentKillAward);
            for (int i = 0; i < 20; i++) sim.AwardKill();
            Assert.AreEqual(0L, meta.BankedSalvage, "nothing banks from a practice body");
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "and the Run's ledger stays empty");
        }

        [Test]
        public void HatchDamage_IsIgnored_AndCannotLoseTheRun()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            Assert.IsTrue(sim.DrawPracticeWave(1, null));
            float before = sim.Ship.Hatch.Hp;
            Assert.Greater(before, 0f);

            sim.DamageHatch(before * 10f);
            Assert.AreEqual(before, sim.Ship.Hatch.Hp, 1e-6f, "the Hatch is not in the fight");
            Assert.IsFalse(sim.Ship.Hatch.IsDestroyed);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase);
        }

        [Test]
        public void Draw_IsRefused_OffPractice()
        {
            var sim = new RunSim(MetaState.CreateNew(), 7);
            sim.StartRun();
            sim.EngageTether();
            int started = 0;
            sim.WaveStarted += w => started++;

            Assert.IsFalse(sim.PracticeMode);
            Assert.IsFalse(sim.DrawPracticeWave(3, WaveShape.Pack));
            Assert.AreEqual(0, started);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(0, sim.CurrentWaveIndex);
        }

        [Test]
        public void Draw_IsRefused_WhileAWaveIsLive_AndOutsideTheField()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            int started = 0;
            sim.WaveStarted += w => started++;

            Assert.IsFalse(sim.DrawPracticeWave(0, null));
            Assert.IsFalse(sim.DrawPracticeWave(WaveDirector.MaxWaves + 1, null));
            Assert.AreEqual(0, started);

            Assert.IsTrue(sim.DrawPracticeWave(2, null));
            Assert.IsFalse(sim.DrawPracticeWave(3, null), "one wave at a time");
            Assert.AreEqual(1, started);
            Assert.AreEqual(2, sim.CurrentWaveIndex);
        }

        [Test]
        public void Cleared_ReturnsToPrep_WithNoTrickle_NoShutoffOwed_AndTheNextDrawIsFree()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            int ended = 0;
            sim.AssaultEnded += () => ended++;

            Assert.IsTrue(sim.DrawPracticeWave(5, WaveShape.Convoy));
            sim.NotifyDeliveryComplete();
            sim.NotifyAssaultCleared();

            Assert.AreEqual(1, ended);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "the last wave completes nothing");
            Assert.IsFalse(sim.FieldTrickleLive);
            Assert.IsFalse(sim.FieldShutoffArmed);
            Assert.IsFalse(sim.SequentialPending);
            Assert.IsFalse(sim.ChainPending);
            Assert.IsFalse(sim.MasterSequenceLive);

            Assert.IsTrue(sim.DrawPracticeWave(1, WaveShape.Mass), "the lane is free again");
            Assert.AreEqual(1, sim.CurrentWaveIndex, "and the cursor is honest after a seek back");
        }
    }
}
