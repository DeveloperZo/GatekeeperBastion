using System.Collections.Generic;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The practice rule: a range draw launches one kind at one size on wave 1's
    /// beat, pays nothing, cannot lose the Run at the Hatch, is refused off practice, and
    /// hands the sim back to Prep with nothing owed when the last body falls.</summary>
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

        [Test]
        public void Sizes_AreTheRosterLadder_AndTheBreachIsTwentyMetres()
        {
            Assert.AreEqual(1, PracticeWave.Count(PracticeSize.Small), "one body: the time-to-kill read");
            Assert.AreEqual(WaveDirector.BaseEnemyCount, PracticeWave.Count(PracticeSize.Normal),
                "normal is the Bastion's wave 1");
            Assert.AreEqual(new WaveDirector().NextWave(2, 0, 1).enemyCount, PracticeWave.Count(PracticeSize.Large),
                "large is wave 3's roster");
            Assert.AreEqual(20, PracticeWave.SpawnMetres);
            Assert.AreEqual(PracticeSize.Normal, PracticeWave.Next(PracticeSize.Small));
            Assert.AreEqual(PracticeSize.Small, PracticeWave.Next(PracticeSize.Large), "the size wraps");
        }

        [Test]
        public void Draw_LaunchesOneKind_AtTheSize_OnWaveOnesBeat()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            var specs = new List<WaveSpec>();
            sim.WaveStarted += w => specs.Add(w);

            Assert.IsTrue(sim.DrawPracticeWave(GatekeeperKind.Hunter, PracticeSize.Large));
            Assert.AreEqual(AssaultState.Assault, sim.Assault);
            Assert.AreEqual(1, sim.CurrentWaveIndex, "a practice wave is wave 1 with the roster rewritten");
            Assert.AreEqual(1, specs.Count);

            WaveSpec own = new WaveDirector().NextWave(0, 0, 1);
            WaveSpec s = specs[0];
            Assert.AreEqual(PracticeWave.LargeCount, s.enemyCount);
            Assert.AreEqual(PracticeWave.LargeCount, s.hunterCount, "all of the one kind");
            Assert.AreEqual(0, s.defaultCount + s.thiefCount + s.cloakerCount);
            Assert.IsFalse(s.finaleElite);
            Assert.AreEqual(own.enemyHealthMultiplier, s.enemyHealthMultiplier, 1e-6f, "wave 1's health");
            Assert.AreEqual(own.groupSize, s.groupSize, "wave 1's group width");
            Assert.AreEqual(own.spawnInterval, s.spawnInterval, 1e-6f, "wave 1's mean gap");

            int groups = WaveCadence.GroupsFor(s.enemyCount, s.groupSize);
            Assert.Greater(groups, 1);
            Assert.IsNotNull(s.groupGaps, "the beat is seeded at launch");
            Assert.AreEqual(groups - 1, s.groupGaps.Length);
            Assert.IsFalse(sim.ChainPending, "one wave, nothing chained");
            Assert.IsFalse(sim.FieldTrickleLive);
            Assert.AreEqual(0, sim.PracticeCount, "the roster override is consumed by the launch");
        }

        [Test]
        public void Draw_Small_IsOneBody_OfAnyKind()
        {
            var kinds = new[] { GatekeeperKind.Default, GatekeeperKind.Thief, GatekeeperKind.Cloaker };
            foreach (var kind in kinds)
            {
                var sim = PracticeSim(MetaState.CreateNew());
                var specs = new List<WaveSpec>();
                sim.WaveStarted += w => specs.Add(w);
                Assert.IsTrue(sim.DrawPracticeWave(kind, PracticeSize.Small), kind.ToString());
                WaveSpec s = specs[0];
                Assert.AreEqual(1, s.enemyCount, kind.ToString());
                int ofKind = kind == GatekeeperKind.Default ? s.defaultCount
                    : kind == GatekeeperKind.Thief ? s.thiefCount : s.cloakerCount;
                Assert.AreEqual(1, ofKind, kind + " is the one body — no debut gate on the range");
                Assert.AreEqual(0, WaveCadence.GroupsFor(s.enemyCount, s.groupSize) - 1, "one group, no gap");
            }
        }

        [Test]
        public void Kill_PaysZero()
        {
            var meta = MetaState.CreateNew();
            var sim = PracticeSim(meta);
            Assert.IsTrue(sim.DrawPracticeWave(GatekeeperKind.Default, PracticeSize.Normal));

            Assert.AreEqual(0L, sim.CurrentKillAward);
            for (int i = 0; i < 20; i++) sim.AwardKill();
            Assert.AreEqual(0L, meta.BankedSalvage, "nothing banks from a practice body");
            Assert.AreEqual(0L, sim.Salvage.RunSalvage, "and the Run's ledger stays empty");
        }

        [Test]
        public void HatchDamage_IsIgnored_AndCannotLoseTheRun()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            Assert.IsTrue(sim.DrawPracticeWave(GatekeeperKind.Default, PracticeSize.Normal));
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
            Assert.IsFalse(sim.DrawPracticeWave(GatekeeperKind.Hunter, PracticeSize.Normal));
            Assert.AreEqual(0, started);
            Assert.AreEqual(AssaultState.Prep, sim.Assault);
            Assert.AreEqual(0, sim.CurrentWaveIndex);
        }

        [Test]
        public void Draw_IsRefused_WhileAWaveIsLive()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            int started = 0;
            sim.WaveStarted += w => started++;

            Assert.IsTrue(sim.DrawPracticeWave(GatekeeperKind.Default, PracticeSize.Normal));
            Assert.IsFalse(sim.DrawPracticeWave(GatekeeperKind.Hunter, PracticeSize.Small), "one wave at a time");
            Assert.AreEqual(1, started);
        }

        [Test]
        public void Cleared_ReturnsToPrep_WithNoTrickle_NoShutoffOwed_AndTheNextDrawIsFree()
        {
            var sim = PracticeSim(MetaState.CreateNew());
            int ended = 0;
            sim.AssaultEnded += () => ended++;

            Assert.IsTrue(sim.DrawPracticeWave(GatekeeperKind.Thief, PracticeSize.Large));
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

            Assert.IsTrue(sim.DrawPracticeWave(GatekeeperKind.Default, PracticeSize.Small), "the lane is free again");
            Assert.AreEqual(1, sim.CurrentWaveIndex, "and the cursor is honest after a seek back");
        }
    }
}
