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

        /// <summary>Gatekeepers in wave 0 at siphon level 0.
        /// <para>CUT ~40% WITH THE ROSTER (Zo, 2026-08-27: "lower the volume of enemies and make
        /// them tougher... reducing volume by at least 40%"). 18+12i was 18/30/42/54/66; 11+7i is
        /// 11/18/25/32/39, which is 39-41% down on every wave. The total HP POOL is held roughly
        /// constant by the matching rise in every kind's maxHp, so this is a legibility change
        /// and not a difficulty cut: the same work arrives as fewer, longer-lived targets, which
        /// is what a corridor with consistent firing angles can actually be fought in.</para>
        /// <para>It also thins the pile that ended the 09:24 run: 50 bodies reached the Hatch
        /// and took it from 25% to breached in 2.7 s.</para></summary>
        public const int BaseEnemyCount = 11;

        /// <summary>Extra gatekeepers per wave index. See <see cref="BaseEnemyCount"/> — the two
        /// move together or the curve's shape changes rather than its scale.</summary>
        public const int CountPerWave = 7;

        /// <summary>Hard cap per wave AND aggregate live gatekeepers across a deep-start chain,
        /// per player. Solo (playerCount 1) stays 120. At dial 5 wave 5 the largest single wave
        /// is 99; the cap is still a guard, not a knob.</summary>
        public const int MaxEnemyCount = 120;

        /// <summary>Supported friends-alpha count (host plus two). Six is the intent;
        /// <see cref="NextWave"/> takes an int so six is a tuning pass, not a rewrite.</summary>
        public const int SupportedPlayerCount = 3;

        /// <summary>Capacity the code must not assume below. Spawn, HUD and wallets size to this.</summary>
        public const int PlayerCapacity = 6;

        /// <summary>Floor on seconds between spawn GROUPS. Inert at the authored five waves
        /// since the gap now WIDENS with the wave, and kept as the guard that any future
        /// shrinking term must clear.</summary>
        public const float MinSpawnInterval = 0.4f;

        /// <summary>Seconds between spawn GROUPS on the first authored wave, before the
        /// per-wave term. Named so the doc side has a symbol to cite: this base and
        /// <see cref="SpawnGapPerWave"/> are the pair every balance argument restates.</summary>
        public const float SpawnGapBase = 7.3f;

        /// <summary>Seconds added to the spawn-group gap per wave. NOT to be confused with
        /// <see cref="EnemyHealthPerWave"/>, which is also 0.40 and is a different lever.</summary>
        public const float SpawnGapPerWave = 0.40f;

        /// <summary>Gatekeepers per spawn group in wave 0; each later wave adds one, capped by
        /// <see cref="MaxGroupSize"/>.</summary>
        public const int BaseGroupSize = 3;

        /// <summary>Ceiling on spawn group width. The five-wave delivery table needs 3/4/5/6/7 —
        /// this lifted an earlier temporary 4-wide cap so waves 3–5 match Docs/Design/4-Data.md.
        /// Group width is the authored curve and this cap must never clip it again: a narrower
        /// group is what the earlier balance model reached for, and play data falsified it.
        /// The group GAP is a separate lever and it is now the one carrying delivery rate — but
        /// only ever in the WIDENING direction (see spawnInterval in <see cref="NextWave"/>).
        /// Narrowing the gap remains closed, and narrowing it to pay for a wider group is the
        /// exact trade that has already failed here twice.</summary>
        public const int MaxGroupSize = 7;

        /// <summary>HP multiplier slope per wave index: <c>1 + slope * waveIndex</c>.
        /// 1.00 / 1.40 / 1.80 / 2.20 / 2.60 → a Default is 50 / 70 / 90 / 110 / 130 HP.
        /// Wave 1 stays at 1.0 by construction, so this slope is a no-op on the one wave
        /// play data says already reads correctly.
        ///
        /// LOWERED 0.40 → 0.30, THEN RESTORED TO 0.40 THE SAME DAY (Zo: "can scale health on
        /// later waves"). Both halves are kept because the reasoning below is still the reason
        /// this constant is watched — it is the record of a lever that was pulled for the right
        /// diagnosis and the wrong cause. The cut was made to stop the HP curve binding the
        /// loadouts the demo sells; the actual cause was that every Primary was WEAKER THAN THE
        /// FREE SIDEARM (MK-1 59.0 dps against 33-37), so the whole tier was raised ~2x instead
        /// and the slope went back. Fixing the weapon made the HP cut unnecessary.
        ///
        /// The superseded analysis, kept for its method:
        /// The earlier raise 0.25 → 0.40 was meant to stop STRONG loadouts walking through late waves.
        /// It never did that. Measured against that log's ~0.90 kills/s acquisition cap — the
        /// rate a player achieves when damage is NOT the limit — the wave-5 sustained-dps
        /// ceiling at slope 0.40 was: MK-1 T0 0.69, MR-35 T0 0.48, MR-35 T2 0.89, MR-35 T3
        /// 1.16, MR-35 T5 2.06. The slope bound every loadout the demo actually SELLS and
        /// sailed under both strong ones. That is why a 1200-salvage Primary at T2 scored the
        /// same 0.73–0.95 kills/s as the free MK-1 T0 did on 2026-08-26: past wave 3 the
        /// player was fighting their own damage number, not choosing targets.
        /// At 0.30 the T2 ceiling would have been 1.04 — above the acquisition cap. The raise
        /// to the Primary tier clears it at 0.40 instead, and at T0 rather than T2.
        ///
        /// THAT INTENT IS NOT ABANDONED, IT MOVED. What stops a strong loadout clearing a
        /// late wave is now the delivery rate (see spawnInterval in <see cref="NextWave"/>):
        /// wave 5 delivers 1.47 bodies/s against a 0.90 acquisition cap, so no loadout at any
        /// tier can hold a wave-5 field by standing still. HP could only ever bind the weak
        /// end; delivery binds all of it, which is what the design rule actually wanted.
        /// Cap unchanged in kind: BR-12 still one-shots every body in the table, but the margin
        /// is now THREE POINTS at the deep end — 7×19 = 133 against a wave-5 Default's 130. That
        /// is the tightest boundary in the weapon set, and it is one tuning step from halving
        /// the weapon to 0.395 kills/s. Raising this slope without raising BR-12's damage with
        /// it is a cliff, not a retune.</summary>
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

        /// <summary>Hit points of one Default gatekeeper before enemyHealthMultiplier.
        /// <para>RAISED 30 -> 50 with the ~40% roster cut (Zo, 2026-08-27: "make them tougher").
        /// Every kind's maxHp rose by the same 5/3, so the wave's total HP pool is within 2% of
        /// what it was — fewer, tougher bodies doing the same total work. The ladder is now
        /// 50/70/90/110/130.</para>
        /// <para>Sizing weapons against this: judge on kills/s, not dps, because raising HP
        /// moves the OVERKILL BOUNDARIES and those move in steps. This change alone took the
        /// MK-1's wave-1 kill from one shot to two, which is why the free sidearm no longer
        /// out-kills the MR-35 on the opening wave.</para></summary>
        public const float BaseEnemyHp = 50f;

        /// <summary>Damage a Default gatekeeper deals to the player per swing.</summary>
        public const float GatekeeperPlayerDamage = 6f;

        /// <summary>Damage a Default gatekeeper deals to the hatch per swing.</summary>
        public const float GatekeeperHatchDamage = 4f;

        /// <summary>Seconds between swings. ALL THREE KINDS SHARE THIS ONE CLOCK: a kind differs
        /// by what it hits and how hard, never by cadence.</summary>
        public const float GatekeeperAttackInterval = 0.7f;

        /// <summary>Move speed while marching on the hatch (Default).</summary>
        public const float GatekeeperPatrolSpeed = 2.8f;

        /// <summary>Move speed once provoked (Default). Deliberately just UNDER the player's
        /// 5 m/s walk (Zo, playtest 2026-08-25): giving ground has to buy a moment, or the
        /// lane cannot be held and every fight is a sprint. It does not make kiting free —
        /// the lane runs toward the Hatch, the far closet keeps feeding, and standing still
        /// to shoot or reload hands the distance straight back.</summary>
        public const float GatekeeperChaseSpeed = 4.6f;

        /// <summary>Seconds between repeated spool-assault waves while the escape spool runs.</summary>
        public const float SpoolWaveInterval = 8f;

        // ---- The other three kinds' hit points. Default's is BaseEnemyHp; these three sat as
        //      literals inside StatsFor until they were named, which is why the docs could cite
        //      the pre-2026-08-27 figures with nothing going red. One copy of each number.

        /// <summary>Hit points of one Hunter. 20 x 5/3 with the 2026-08-27 fewer-and-tougher pass.</summary>
        public const float HunterHp = 33f;

        /// <summary>Hit points of one Thief. 120 x 5/3; the heavy, and the reason its COUNT fell
        /// hardest (7 -> 4 on wave 5).</summary>
        public const float ThiefHp = 200f;

        /// <summary>Hit points of one Cloaker. 45 x 5/3; still ~1.5 Defaults, which is the rule.
        /// The ONLY number this kind owns.</summary>
        public const float CloakerHp = 75f;

        // ---- Per-wave composition. Fixed counts; Default absorbs the remainder. ----

        private static readonly int[] HunterPerWave = { 0, 2, 4, 6, 7 };
        private static readonly int[] ThiefPerWave = { 0, 0, 1, 2, 4 };
        // Deliberately small: a Wave where somebody is always looking at every Cloaker freezes
        // them all and turns the kind into free salvage (the milestone's pre-mortem, case b).
        // Debut is wave 4, one wave AFTER the Thief: two new kinds landing in the same wave
        // means neither rule gets read. It debuts as a pair so the watch-freeze is witnessed.
        private static readonly int[] CloakerPerWave = { 0, 0, 0, 1, 2 };

        /// <summary>The stat line of one gatekeeper kind. Unknown kinds fall back to Default.</summary>
        public static GatekeeperStats StatsFor(GatekeeperKind kind)
        {
            var stats = new GatekeeperStats();
            stats.attackInterval = GatekeeperAttackInterval; // one swing clock for every kind

            switch (kind)
            {
                case GatekeeperKind.Hunter:
                    stats.maxHp = HunterHp;
                    stats.playerDamage = 6f;
                    stats.hatchDamage = 0f;   // THE RULE: a Hunter can never hurt the hatch
                    stats.patrolSpeed = 5.6f; // no second gear: it is already hunting at spawn
                    stats.chaseSpeed = 5.6f;   // still the only thing that outruns the player
                    stats.ignoresHatch = true;
                    break;

                case GatekeeperKind.Thief:
                    // 120 x 5/3. The Thief is the one kind where this is a real behaviour change
                    // and not just a longer TTK: at 200 base it is 520 HP on wave 5, and an
                    // MR-35 magazine carries 432, so the deepest Thief no longer dies inside one
                    // pull. That is intended — it is the heavy — but it is also why its COUNT
                    // fell hardest (7 -> 4 on wave 5): a tankier body that walks at the Hatch is
                    // exactly what breached it at 09:24, and toughness had to be paid for in
                    // number rather than added on top of it.
                    stats.maxHp = ThiefHp;
                    stats.playerDamage = 0f;  // THE RULE: a Thief can never hurt the player
                    stats.hatchDamage = 20f;
                    stats.patrolSpeed = 2.2f; // no second gear: nothing makes it change its mind
                    stats.chaseSpeed = 2.2f;
                    stats.ignoresPlayer = true;
                    break;

                case GatekeeperKind.Cloaker:
                    // TWO MK-1 T0 shots where a Default is one, since the 2026-09-02 sidearm recut
                    // (75 and 50 against 50 damage; three and two at the old 30). It reads as 1.5 Defaults,
                    // which is the rule and is unchanged; the SHOT COUNTS moved because raising
                    // HP moves overkill boundaries in steps. The ONLY number this kind owns. Everything else is
                    // Default's, read from the same constants, because the watch rule is the
                    // kind and its damage must not open a second question (see the milestone:
                    // "the one that stops when you look", door-time note).
                    stats.maxHp = CloakerHp;
                    stats.playerDamage = GatekeeperPlayerDamage;
                    stats.hatchDamage = GatekeeperHatchDamage;
                    stats.patrolSpeed = GatekeeperPatrolSpeed;
                    stats.chaseSpeed = GatekeeperChaseSpeed;
                    // Neither refusal flag: the Cloaker's refusal is a per-frame STATE
                    // (observed / unobserved) owned by the Game layer's CloakerGatekeeper,
                    // not a constant this struct could push.
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
        ///   enemyCount            = min(120 * n, round((11 + 7*waveIndex) * dial) * n)
        ///                           n = playerCount (clamped >= 1). n = 1 is today's table.
        ///                           dial = TetherSiphon.RewardMultiplierFor(siphonLevel)
        ///   hunterCount           = {0,2,4,6,7}[waveIndex]    (never scaled by the dial)
        ///   thiefCount            = {0,0,1,2,4}[waveIndex]     (never scaled by the dial)
        ///   cloakerCount          = {0,0,0,1,2}[waveIndex]     (never scaled by the dial)
        ///   defaultCount          = enemyCount - hunterCount - thiefCount - cloakerCount
        ///   enemyHealthMultiplier = 1 + EnemyHealthPerWave*waveIndex  (0.40 → 1.00/1.40/1.80/2.20/2.60)
        ///   groupSize             = min(3 + waveIndex, MaxGroupSize=7, enemyCount)  → 3/4/5/6/7
        ///   spawnInterval         = max(0.4, SpawnGapBase + SpawnGapPerWave*min(waveIndex, MaxWaves-1))
        ///                           → 7.30/7.70/8.10/8.50/8.90 — PER GROUP, see WaveSpec.
        ///                           SUPERSEDED 2026-08-27: this line read 4.2 + 0.20i, giving
        ///                           4.20/4.40/4.60/4.80/5.00, until the roster cut re-derived it.
        ///   timeUntilNextWave     = 0 — obsolete under the draw model; kept for WaveSpec shape.
        ///
        /// The dial scales BODY COUNT ONLY. Health, group size, spawn gap and composition do not
        /// see it, and the dial's surplus is entirely Default so it cannot multiply Hunters or
        /// Thieves. Both rules are load-bearing — see Docs/Design/2-Dynamics.md before relaxing either.
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
            // the four counts ALWAYS sum to enemyCount — the Game layer's spawn loop relies on
            // that without checking.
            spec.hunterCount = Math.Min(Row(HunterPerWave, waveIndex), spec.enemyCount);
            spec.thiefCount = Math.Min(Row(ThiefPerWave, waveIndex), spec.enemyCount - spec.hunterCount);
            spec.cloakerCount = Math.Min(Row(CloakerPerWave, waveIndex),
                                         spec.enemyCount - spec.hunterCount - spec.thiefCount);
            spec.defaultCount = spec.enemyCount - spec.hunterCount - spec.thiefCount
                                - spec.cloakerCount;

            spec.enemyHealthMultiplier = 1f + EnemyHealthPerWave * waveIndex;
            spec.groupSize = Math.Max(1, Math.Min(Math.Min(BaseGroupSize + waveIndex, MaxGroupSize),
                                                  spec.enemyCount));
            // THE GROUP GAP WIDENS WITH THE WAVE: 4.20 / 4.40 / 4.60 / 4.80 / 5.00 s, so the
            // five waves deliver over 21.0 / 30.8 / 36.8 / 38.4 / 45.0 s.
            //
            // It used to NARROW (4.2 - 0.20*i → 21.0 / 28.0 / 30.4 / 28.8 / 30.6 s), which held
            // delivery time roughly CONSTANT at ~30 s while the roster grew 18 → 66. Group width
            // rises 3→7 over the same span, so width and gap pushed the same way and compounded:
            // arrivals ran 0.86 → 1.07 → 1.38 → 1.88 → 2.16 bodies/s, a 2.5x climb, against a
            // player who kills at ~0.90/s at best. The 2026-08-27 03:18 run is what that costs —
            // peak live 8 → 27 → 33 → 57 → 103, six deaths on wave 5, run lost.
            //
            // Widening restores the intent: 0.86 → 0.97 → 1.14 → 1.41 → 1.47 bodies/s. Still
            // ABOVE the 0.90 acquisition cap on every wave after the first, so the field always
            // accumulates and the player is still never meant to clear it (Docs/Design/0-Gameplay.md:
            // a wave begins at the Hatch and ends at a terminal). It accumulates at a rate a
            // player can move through instead of one that buries them where they stand — and
            // wave 5's 45 s delivery now spans the 155 m walk to T5 (31 s walking / 25 s
            // sprinting) rather than emptying the closets before the player has left the Hatch,
            // so the fight travels up the lane the way the loop document says it should.
            //
            // This does NOT narrow the gap that the earlier widening opened, and does NOT touch
            // the authored 3/4/5/6/7 group width — both pins move further into their own intent.
            // waveIndex is clamped to the last authored wave so probe indices past wave 5 hold
            // at 5.0 s, the same "hold the last authored value" rule Row() uses for composition.
            // 4.2 + 0.20i -> 7.3 + 0.40i, RE-DERIVED WITH THE ROSTER CUT (2026-08-27).
            // A wave is delivered in ceil(count/groupSize) groups, so cutting the roster ~40%
            // while leaving group width (3/4/5/6/7) and this gap alone shortened every delivery
            // window by about the same 40%: 21.0/30.8/36.8/38.4/45.0 s collapsed to
            // 12.6/17.6/18.4/24.0/25.0. Body ARRIVALS barely moved, which is why this does not
            // show up in a bodies-per-second reading — but each body now carries 5/3 the HP, so
            // HP PRESSURE PER SECOND rose by that factor and wave 5 went from 114 to 203 HP/s.
            // That silently undid the widening bought hours earlier to stop bodies stacking.
            // The gap is solved to hold HP/s at the pre-cut figures: 25.1/40.9/69.4/82.8/113.9
            // against 25.7/40.9/61.6/92.8/114.4.
            //
            // Waves 3 and 4 sit ~12% off in opposite directions and that is not slack in the
            // fit — ceil(count/groupSize) is a STEP function, so group count moves in jumps
            // while a linear gap cannot follow. Any future roster change must re-derive this
            // line; the roster and this gap are one lever, not two.
            spec.spawnInterval = Math.Max(MinSpawnInterval,
                                          SpawnGapBase + SpawnGapPerWave * Math.Min(waveIndex, MaxWaves - 1));
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
            spec.thiefCount = 0;
            spec.cloakerCount = 0;
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
