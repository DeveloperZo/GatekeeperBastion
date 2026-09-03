using System;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M116 lever 1: the group gap is a mean, not a metronome. The window a wave
    /// lands in is unchanged (the roster and the gap are one lever on HP/s — 2026-08-27), the
    /// beat inside it is seeded bursts and lulls, and the falsifier from the session log is the
    /// coefficient of variation of the inter-group gaps: above 0.4, or the cadence did not move.</summary>
    public class WaveCadenceTests
    {
        static float Sum(float[] a) { float s = 0f; foreach (var x in a) s += x; return s; }

        static float Cov(float[] a)
        {
            if (a.Length < 2) return 0f;
            float mean = Sum(a) / a.Length;
            float v = 0f;
            foreach (var x in a) v += (x - mean) * (x - mean);
            return (float)Math.Sqrt(v / a.Length) / mean;
        }

        [Test]
        public void TheWindowIsUnchanged_OnEveryAuthoredWave()
        {
            var director = new WaveDirector();
            var rng = new Random(7);
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                int groups = WaveCadence.GroupsFor(spec.enemyCount, spec.groupSize);
                var gaps = WaveCadence.Schedule(groups, spec.spawnInterval, rng);
                Assert.AreEqual(groups - 1, gaps.Length, "one gap between each pair of groups, wave " + (w + 1));
                Assert.AreEqual((groups - 1) * spec.spawnInterval, Sum(gaps), 1e-3f,
                    "the delivery window moved on wave " + (w + 1) + " — HP/s is the roster's lever, not this one's");
            }
        }

        [Test]
        public void TheBeatVaries_AboveTheLogsFalsifier()
        {
            var director = new WaveDirector();
            for (int seed = 1; seed <= 20; seed++)
            {
                var rng = new Random(seed);
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                {
                    var spec = director.NextWave(w, 0);
                    int groups = WaveCadence.GroupsFor(spec.enemyCount, spec.groupSize);
                    var gaps = WaveCadence.Schedule(groups, spec.spawnInterval, rng);
                    if (gaps.Length < 2) continue;
                    Assert.Greater(Cov(gaps), 0.4f,
                        "seed " + seed + " wave " + (w + 1) + " is a metronome: CoV " + Cov(gaps));
                }
            }
        }

        [Test]
        public void PairsAreHot_LullsRepay_AndNothingDropsUnderTheFloor()
        {
            var rng = new Random(3);
            float mean = 7.3f;
            var gaps = WaveCadence.Schedule(5, mean, rng); // four gaps, two pairs
            Assert.AreEqual(4, gaps.Length);
            for (int i = 0; i < 4; i += 2)
            {
                float a = gaps[i], b = gaps[i + 1];
                float burst = Math.Min(a, b), lull = Math.Max(a, b);
                Assert.GreaterOrEqual(burst, mean * WaveCadence.BurstMin - 1e-4f);
                Assert.LessOrEqual(burst, mean * WaveCadence.BurstMax + 1e-4f);
                Assert.AreEqual(2f * mean, burst + lull, 1e-4f, "a pair repays its burst");
                Assert.GreaterOrEqual(burst, WaveDirector.MinSpawnInterval);
            }
        }

        [Test]
        public void AnOddTail_KeepsTheMean()
        {
            var gaps = WaveCadence.Schedule(4, 8f, new Random(11)); // three gaps: one pair, one tail
            Assert.AreEqual(3, gaps.Length);
            Assert.AreEqual(8f, gaps[2], 1e-4f, "the unpaired last gap is the mean");
            Assert.AreEqual(24f, Sum(gaps), 1e-3f);
        }

        [Test]
        public void SeededMeansRepeatable_AndDifferentSeedsDiffer()
        {
            var a = WaveCadence.Schedule(7, 8.9f, new Random(42));
            var b = WaveCadence.Schedule(7, 8.9f, new Random(42));
            CollectionAssert.AreEqual(a, b, "the same seed must land the same beat — replay reads it");
            bool anyDiffer = false;
            for (int seed = 1; seed <= 6 && !anyDiffer; seed++)
            {
                var c = WaveCadence.Schedule(7, 8.9f, new Random(seed));
                for (int i = 0; i < c.Length; i++)
                    if (Math.Abs(c[i] - a[i]) > 1e-4f) { anyDiffer = true; break; }
            }
            Assert.IsTrue(anyDiffer, "six seeds produced one beat");
        }

        [Test]
        public void NothingToPace_IsEmpty_AndFallsBackToTheMean()
        {
            Assert.AreEqual(0, WaveCadence.Schedule(1, 7.3f, new Random(1)).Length, "one group has no gap");
            Assert.AreEqual(0, WaveCadence.Schedule(0, 7.3f, new Random(1)).Length);
            Assert.AreEqual(0, WaveCadence.Schedule(4, 0f, new Random(1)).Length, "no mean, no schedule");
            var spec = new WaveSpec { spawnInterval = 3f };
            Assert.AreEqual(3f, WaveCadence.GapAfter(spec, 1), "a spec without a schedule keeps its mean gap");
            spec.groupGaps = new[] { 1f, 5f };
            Assert.AreEqual(1f, WaveCadence.GapAfter(spec, 1));
            Assert.AreEqual(5f, WaveCadence.GapAfter(spec, 2));
            Assert.AreEqual(3f, WaveCadence.GapAfter(spec, 3), "past the schedule, the mean again");
        }

        [Test]
        public void TheSimAttachesASchedule_ToEveryWaveItLaunches()
        {
            var meta = MetaState.CreateNew();
            var sim = new RunSim(meta, 5);
            sim.StartRun();
            sim.EngageTether();
            WaveSpec launched = default;
            sim.WaveStarted += s => launched = s;
            sim.TriggerDraw();
            int groups = WaveCadence.GroupsFor(launched.enemyCount, launched.groupSize);
            Assert.IsNotNull(launched.groupGaps, "the launch carries its beat");
            Assert.AreEqual(groups - 1, launched.groupGaps.Length);
            Assert.AreEqual((groups - 1) * launched.spawnInterval, Sum(launched.groupGaps), 1e-3f);
        }
    }
}
