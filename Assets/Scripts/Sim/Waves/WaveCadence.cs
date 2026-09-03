using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The rhythm a wave's groups arrive in. <see cref="WaveSpec.spawnInterval"/> is
    /// the MEAN gap between groups; this turns it into a seeded sequence of bursts and lulls
    /// whose total is the same delivery window, so HP per second over the wave is unchanged
    /// and only the beat moves (M116, Zo 2026-09-02: "no felt variance in waves").
    /// <para>Every ancestor draws its intervals from a window, never a value: Left 4 Dead's
    /// mobs at 90–180 s, Vermintide's three hordes 10–15 s apart then a 70–150 s lull. The
    /// shape here is the simplest one that reads: groups come in PAIRS — the second hot on
    /// the first's heels at <see cref="BurstMin"/>–<see cref="BurstMax"/> of the mean gap —
    /// and each pair is followed by a lull that repays the borrowed time, so every two gaps
    /// still sum to two mean gaps. A wave with an odd number of gaps keeps one plain gap at
    /// the end. Whether a wave opens on a burst or a lull is the seed's.</para>
    /// <para>What this deliberately does NOT vary: the roster, the group width, the window.
    /// The 2026-08-27 pass found the roster and the gap are one lever on HP/s; this keeps
    /// that lever where it was and varies only the delays (#9), which is the tier that owns
    /// "static".</para></summary>
    public static class WaveCadence
    {
        /// <summary>The hot half of a pair, as a fraction of the mean gap. 0.35 × 7.3 s is
        /// 2.6 s between the pair's two groups at wave 1.</summary>
        public const float BurstMin = 0.25f;
        public const float BurstMax = 0.45f;

        static readonly float[] Empty = new float[0];

        /// <summary>How many groups a roster lands in.</summary>
        public static int GroupsFor(int enemyCount, int groupSize)
        {
            if (enemyCount <= 0) return 0;
            if (groupSize < 1) groupSize = 1;
            return (enemyCount + groupSize - 1) / groupSize;
        }

        /// <summary>The seconds between consecutive groups: index k is the gap AFTER group
        /// k+1 (1-based), so a wave of G groups has G−1 entries. Sums to (G−1) × meanGap.
        /// Never below <see cref="WaveDirector.MinSpawnInterval"/>. Empty when there is
        /// nothing to pace.</summary>
        public static float[] Schedule(int groups, float meanGap, Random rng)
        {
            int gaps = groups - 1;
            if (gaps <= 0 || meanGap <= 0f || rng == null) return Empty;
            var result = new float[gaps];
            bool burstFirst = rng.Next(2) == 0;
            int i = 0;
            while (i < gaps)
            {
                if (gaps - i == 1)
                {
                    result[i] = meanGap; // an odd tail keeps the mean, not the beat
                    break;
                }
                float t = (float)rng.NextDouble();
                float burst = meanGap * (BurstMin + (BurstMax - BurstMin) * t);
                float lull = 2f * meanGap - burst;
                result[i] = burstFirst ? burst : lull;
                result[i + 1] = burstFirst ? lull : burst;
                i += 2;
            }
            for (int k = 0; k < result.Length; k++)
                if (result[k] < WaveDirector.MinSpawnInterval)
                    result[k] = WaveDirector.MinSpawnInterval;
            return result;
        }

        /// <summary>The gap the spawner should wait after emitting <paramref name="groupNumber"/>
        /// (1-based). A spec with no schedule — the trickle, a probe — falls back to its mean.</summary>
        public static float GapAfter(WaveSpec spec, int groupNumber)
        {
            var g = spec.groupGaps;
            int i = groupNumber - 1;
            if (g != null && i >= 0 && i < g.Length) return g[i];
            return spec.spawnInterval;
        }
    }
}
