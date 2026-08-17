using System;

namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Deterministic wave scaling. Pure functions of (waveIndex, siphonLevel, playerCount) —
    /// no randomness, so host and clients agree byte-for-byte.
    /// </summary>
    /// <remarks>
    /// Every number here is justified in Docs/Design/4-Data.md. Retune there and here together;
    /// a value changed in one place only is the failure this file has already had once.
    /// </remarks>
    public sealed class WaveDirector
    {
        // ---- Wave curve ----

        /// <summary>Field Waves in a run. Indices are 0..MaxWaves-1. Terminal draws after
        /// MaxWaves clamp composition at MaxWaves-1. Hatch-bank does not consume a field Wave.</summary>
        public const int MaxWaves = 5;

        /// <summary>Gatekeepers in wave 0 at siphon level 0.</summary>
        public const int BaseEnemyCount = 18;

        /// <summary>Extra gatekeepers per wave index.</summary>
        public const int CountPerWave = 12;

        /// <summary>Hard cap per wave AND aggregate live gatekeepers across a deep-start chain,
        /// per player. Solo (playerCount 1) stays 120. At dial 5 wave 5 the largest single wave
        /// is 99; the cap is still a guard, not a knob.</summary>
        public const int MaxEnemyCount = 120;

        /// <summary>Supported friends-alpha count (host plus two). Six is the intent;
        /// <see cref="NextWave"/> takes an int so six is a tuning pass, not a rewrite.</summary>
        public const int SupportedPlayerCount = 3;

        /// <summary>Capacity the code must not assume below. Spawn, HUD and wallets size to this.</summary>
        public const int PlayerCapacity = 6;

        /// <summary>Floor on seconds between spawn GROUPS.</summary>
        public const float MinSpawnInterval = 0.4f;

        /// <summary>Gatekeepers per spawn group in wave 0; each later wave adds one, capped by
        /// <see cref="MaxGroupSize"/>.</summary>
        public const int BaseGroupSize = 3;

        /// <summary>Ceiling on spawn group width. The five-wave delivery table needs 3/4/5/6/7 —
        /// this lifted an earlier temporary 4-wide cap so waves 3–5 match Docs/Design/4-Data.md.
        /// Do not reopen the *gap* — that was the lever of an earlier balance model, kept closed
        /// as the record of what play data falsified; group width is the authored curve.</summary>
        public const int MaxGroupSize = 7;

        /// <summary>HP multiplier slope per wave index: <c>1 + slope * waveIndex</c>.
        /// Raised 0.25 → 0.40 so late waves absorb Primary dps — the earlier, shallower slope
        /// is the balance model play data falsified (MR-35 T3 cleared all three
        /// at 100% hatch / 0 hits in <c>Logs/GatekeeperBastion_2026-08-04_09-14-16</c>).
        /// Wave 1 stays at 1.0 by construction. Cap: slope 0.40 keeps BR-12's wave-2 one-shot
        /// at 8 m (42 HP = 7×6 pellets).</summary>
        public const float EnemyHealthPerWave = 0.40f;

        /// <summary>Seconds between the end of one chained wave's delivery and the start of the
        /// next during a deep start. The only tuning knob for the deep-start
        /// IFF crossover.</summary>
        public const float InterWaveGap = 3f;

        /// <summary>Seconds of Prep after a field-terminal Wave clears before the next Wave
        /// of that Interact. Not <see cref="InterWaveGap"/> (deep-start delivery overlap).</summary>
        public const float TerminalChainGap = 8f;

        // ---- The Default kind's stat line. StatsFor(Default) is built from exactly these six
        //      constants, so there is one copy of each number, not two that can drift. ----

        /// <summary>Hit points of one Default gatekeeper before enemyHealthMultiplier.</summary>
        public const float BaseEnemyHp = 30f;

        /// <summary>Damage a Default gatekeeper deals to the player per swing.</summary>
        public const float GatekeeperPlayerDamage = 6f;

        /// <summary>Damage a Default gatekeeper deals to the hatch per swing.</summary>
        public const float GatekeeperHatchDamage = 4f;

        /// <summary>Seconds between swings. ALL THREE KINDS SHARE THIS ONE CLOCK: a kind differs
        /// by what it hits and how hard, never by cadence.</summary>
        public const float GatekeeperAttackInterval = 0.7f;

        /// <summary>Move speed while marching on the hatch (Default).</summary>
        public const float GatekeeperPatrolSpeed = 3.2f;

        /// <summary>Move speed once provoked (Default). MUST exceed the player's 5 m/s walk or
        /// kiting is free.</summary>
        public const float GatekeeperChaseSpeed = 5.6f;

        /// <summary>Seconds between repeated spool-assault waves while the escape spool runs.</summary>
        public const float SpoolWaveInterval = 8f;

        // ---- Per-wave composition. Fixed counts; Default absorbs the remainder. ----

        private static readonly int[] HunterPerWave = { 0, 4, 6, 10, 12 };
        private static readonly int[] RamPerWave = { 0, 0, 2, 4, 7 };

        /// <summary>The stat line of one gatekeeper kind. Unknown kinds fall back to Default.</summary>
        public static GatekeeperStats StatsFor(GatekeeperKind kind)
        {
            var stats = new GatekeeperStats();
            stats.attackInterval = GatekeeperAttackInterval; // one swing clock for every kind

            switch (kind)
            {
                case GatekeeperKind.Hunter:
                    stats.maxHp = 20f;
                    stats.playerDamage = 6f;
                    stats.hatchDamage = 0f;   // THE RULE: a Hunter can never hurt the hatch
                    stats.patrolSpeed = 6.5f; // no second gear: it is already hunting at spawn
                    stats.chaseSpeed = 6.5f;
                    stats.ignoresHatch = true;
                    break;

                case GatekeeperKind.Ram:
                    stats.maxHp = 120f;
                    stats.playerDamage = 0f;  // THE RULE: a Ram can never hurt the player
                    stats.hatchDamage = 20f;
                    stats.patrolSpeed = 2.2f; // no second gear: nothing makes it change its mind
                    stats.chaseSpeed = 2.2f;
                    stats.ignoresPlayer = true;
                    break;

                default: // GatekeeperKind.Default, and anything unrecognised
                    stats.maxHp = BaseEnemyHp; // ONE copy of this number, shared with the wave curve
                    stats.playerDamage = GatekeeperPlayerDamage;
                    stats.hatchDamage = GatekeeperHatchDamage;
                    stats.patrolSpeed = GatekeeperPatrolSpeed;
                    stats.chaseSpeed = GatekeeperChaseSpeed;
                    break;
            }
            return stats;
        }

        /// <summary>
        /// Spec for the given wave.
        ///
        ///   enemyCount            = min(120 * n, round((18 + 12*waveIndex) * dial) * n)
        ///                           n = playerCount (clamped >= 1). n = 1 is today's table.
        ///                           dial = TetherSiphon.RewardMultiplierFor(siphonLevel)
        ///   hunterCount           = {0,4,6,10,12}[waveIndex]   (never scaled by the dial)
        ///   ramCount              = {0,0,2,4,7}[waveIndex]     (never scaled by the dial)
        ///   defaultCount          = enemyCount - hunterCount - ramCount
        ///   enemyHealthMultiplier = 1 + EnemyHealthPerWave*waveIndex  (0.40 → 1.00/1.40/1.80/2.20/2.60)
        ///   groupSize             = min(3 + waveIndex, MaxGroupSize=7, enemyCount)  → 3/4/5/6/7
        ///   spawnInterval         = max(0.4, 4.2 - 0.20*waveIndex)  — PER GROUP, see WaveSpec
        ///   timeUntilNextWave     = 0 — obsolete under the draw model; kept for WaveSpec shape.
        ///
        /// The dial scales BODY COUNT ONLY. Health, group size, spawn gap and composition do not
        /// see it, and the dial's surplus is entirely Default so it cannot multiply Hunters or
        /// Rams. Both rules are load-bearing — see Docs/Design/2-Dynamics.md before relaxing either.
        /// </summary>
        public WaveSpec NextWave(int waveIndex, int siphonLevel, int playerCount = 1)
        {
            if (waveIndex < 0) waveIndex = 0;
            siphonLevel = ClampLevel(siphonLevel);
            if (playerCount < 1) playerCount = 1;

            var spec = new WaveSpec();

            // ONE table for money and bodies alike: TetherSiphon owns the curve, this reads it.
            float dial = TetherSiphon.RewardMultiplierFor(siphonLevel);

            // Snap to 4 decimals BEFORE rounding to a whole body. 1.15f is really 1.14999997...,
            // so 30 * 1.15f lands a hair under 34.5 and a bare round would give 34 while the
            // exact-in-binary 1.25f gives 38 from 37.5 — the grid would be lumpy for a reason
            // that has nothing to do with tuning. Midpoints go up, always.
            double scaled = Math.Round((double)(BaseEnemyCount + CountPerWave * waveIndex) * dial,
                                       4, MidpointRounding.AwayFromZero);
            int bodies = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
            spec.enemyCount = Math.Min(MaxEnemyCount * playerCount, bodies * playerCount);

            // The Min() calls only bite on probe indices past the five real waves, and exist so
            // the three counts ALWAYS sum to enemyCount — the Game layer's spawn loop relies on
            // that without checking.
            spec.hunterCount = Math.Min(Row(HunterPerWave, waveIndex), spec.enemyCount);
            spec.ramCount = Math.Min(Row(RamPerWave, waveIndex), spec.enemyCount - spec.hunterCount);
            spec.defaultCount = spec.enemyCount - spec.hunterCount - spec.ramCount;

            spec.enemyHealthMultiplier = 1f + EnemyHealthPerWave * waveIndex;
            spec.groupSize = Math.Max(1, Math.Min(Math.Min(BaseGroupSize + waveIndex, MaxGroupSize),
                                                  spec.enemyCount));
            spec.spawnInterval = Math.Max(MinSpawnInterval, 4.2f - 0.20f * waveIndex);
            spec.timeUntilNextWave = 0f; // obsolete: no wave timer exists under the draw model
            return spec;
        }

        /// <summary>
        /// DORMANT in v0 (no escape path exists); kept for the escape-window design.
        /// Intensified wave used while the escape spool runs — strictly harsher than
        /// NextWave(0, siphonLevel) on every axis.
        ///   enemyCount            = 14 + 6*siphonLevel
        ///   enemyHealthMultiplier = 1.25 + 0.20*siphonLevel
        ///   spawnInterval         = max(0.15, 0.75 - 0.05*siphonLevel)
        ///   timeUntilNextWave     = 8 (waves keep coming until the spool finishes)
        /// All Default, groupSize 1. These numbers predate the horde pass and were deliberately
        /// left alone; they need a tuning pass if this ever wakes up.
        /// </summary>
        public WaveSpec SpoolAssault(int siphonLevel)
        {
            siphonLevel = ClampLevel(siphonLevel);

            var spec = new WaveSpec();
            spec.enemyCount = 14 + 6 * siphonLevel;
            spec.defaultCount = spec.enemyCount;
            spec.hunterCount = 0;
            spec.ramCount = 0;
            spec.enemyHealthMultiplier = 1.25f + 0.20f * siphonLevel;
            spec.groupSize = 1;
            spec.spawnInterval = Math.Max(0.15f, 0.75f - 0.05f * siphonLevel);
            spec.timeUntilNextWave = SpoolWaveInterval;
            return spec;
        }

        /// <summary>A composition row, holding the last authored value for probe indices past the
        /// five real waves.</summary>
        private static int Row(int[] table, int waveIndex)
        {
            return waveIndex >= table.Length ? table[table.Length - 1] : table[waveIndex];
        }

        private static int ClampLevel(int level)
        {
            if (level < TetherSiphon.MinLevel) return TetherSiphon.MinLevel;
            if (level > TetherSiphon.MaxLevel) return TetherSiphon.MaxLevel;
            return level;
        }
    }
}
