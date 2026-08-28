using System;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>EditMode tests for the siphon dial and the retuned deterministic wave scaling
    /// (draw model, exactly FIVE waves; M16 HP slope; M10 group gap; M14 composition).
    /// Formulas pinned exactly:
    ///   dial                  = TetherSiphon.RewardMultiplierFor(level)
    ///                           = 1 / 1.1 / 1.15 / 1.25 / 1.35 / 1.5   (hand-authored curve)
    ///   enemyCount            = min(120, round((BaseEnemyCount + CountPerWave*waveIndex) * dial))
    ///   enemyHealthMultiplier = 1 + EnemyHealthPerWave*waveIndex  (0.40; NO dial term)
    ///   groupSize             = min(3 + waveIndex, MaxGroupSize=7, enemyCount)  → 3/4/5/6/7
    ///   spawnInterval         = max(0.4, 7.3 + 0.40*min(waveIndex, MaxWaves-1))  (NO dial term)
    ///                           → 7.30/7.70/8.10/8.50/8.90 — SECONDS BETWEEN GROUPS since the
    ///                           horde retune, not between individuals
    ///   timeUntilNextWave     = 0 (obsolete under the draw model)
    /// Per-kind composition (defaultCount/hunterCount/thiefCount) is pinned in
    /// GatekeeperVariantsTests; this file owns the totals, the dial and the gaps.
    ///
    /// FEWER AND TOUGHER — THE 2026-08-27 ROSTER CUT (Zo: "lower the volume of enemies and make
    /// them tougher... reducing volume by at least 40%"). BaseEnemyCount 18 -> 11 and CountPerWave
    /// 12 -> 7, so the dial-0 curve fell 18/30/42/54/66 -> 11/18/25/32/39, 39-41% down on every
    /// wave. Every kind's maxHp rose by exactly 5/3 in the same edit (Default 30 -> 50), so the
    /// wave's total HP POOL barely moved: wave 1 was 18 x 30 = 540 and is 11 x 50 = 550. That makes
    /// it a LEGIBILITY change, not a difficulty cut — the same work arrives as fewer, longer-lived
    /// targets.
    ///
    /// THE HORDE RETUNE it partly walks back — 3x BODIES, 1/3 HP, GROUP SPAWNS.
    /// BaseEnemyCount 6 -> 18 and CountPerWave 4 -> 12 so that three kinds of gatekeeper had enough
    /// bodies on screen to read as different. BaseEnemyHp fell 90 -> 30 and player damage 18 -> 6
    /// in the same edit, which is what kept that survivable: wave 1 was 18 x 30 = 540 hp of
    /// gatekeeper, exactly as 6 x 90 had been. Spawning became groups because 42 individuals at the
    /// old gap was minutes of pure spawning. The kinds can be told apart now, so the 2026-08-27
    /// pass spent some of that body count back on toughness — the pool is the invariant across all
    /// three retunes, which is why it is the thing this fixture asserts.
    ///
    /// THE DIAL RETUNE — WHY REWARD AND BODY COUNT SHARE ONE TABLE.
    /// The dial used to pay 2^level (x1..x32) and charge for it on three axes at once: +3
    /// gatekeepers, +0.20 hp multiplier and -0.15s spawn gap per level. At L5 wave 1 that was 21
    /// gatekeepers at 180 hp every 1.85s — +600% total enemy hp. The developer capped compounding
    /// at +50% ("10, 15, 25, 35, 50 as the 5 percentages"), which would have made the old cost
    /// curve strictly irrational, so the cost was collapsed onto ONE axis reading the SAME
    /// table: 50% more bodies for 50% more money. Break-even on paper; the bet is the residual
    /// (simultaneous gatekeepers overlap and leak to the hatch, so they are harder than their raw
    /// hp says). Wave_CountAndReward_ShareOneTable is the guard that keeps the two sides welded.
    ///
    /// WHY THE GROUP GAP IS 7.3 + 0.40*wave — DO NOT NARROW IT BACK.
    /// After the horde + swing-landing fixes, wave 2 at dial 0 spent ~220 gatekeeper-seconds against
    /// a ~101 hatch budget while the player was alive and shooting (M10). Arrivals scale with
    /// bodies; spawnInterval is the rate that sets them. That has only ever been argued in one
    /// direction — WIDER — and this is the second time the argument has been made.
    ///
    /// The gap used to NARROW with the wave (4.20 / 4.00 / 3.80 / 3.60 / 3.40), which delivered
    /// 21 / 28 / 30.4 / 28.8 / 30.6 s: near-CONSTANT delivery time while the roster grew 18 → 66.
    /// Group width rises 3 → 7 across the same waves, so width and gap pushed the same way and
    /// compounded. Arrivals ran 0.86 / 1.07 / 1.38 / 1.88 / 2.16 bodies per second — a 2.5x climb
    /// against a player whose measured ceiling is ~0.90/s. Logs/GatekeepersProtocol_2026-08-27_
    /// 03-18-16_p17676 is the receipt: peak live 8 → 27 → 33 → 57 → 103, six deaths on wave 5,
    /// run lost at 293 s with 60 bodies still standing.
    ///
    /// 7.30 / 7.70 / 8.10 / 8.50 / 8.90 deliver 21.9 / 30.8 / 32.4 / 42.5 / 44.5 s at the CUT
    /// roster, so arrivals were 0.86 / 0.97 / 1.14 / 1.41 / 1.47 per second. STILL above ~0.90 on
    /// every wave after the first — the field is still meant to accumulate and the player is still
    /// not meant to clear it (Docs/Design/0-Gameplay.md) — but it accumulated at a rate a player
    /// can move through. Raising hatch HP instead is the fixes-that-fail shape. M14 restored group
    /// width to 3/4/5/6/7 after Primaries arrived and that curve is untouched here.
    ///
    /// THE ROSTER CUT MOVED THE OTHER HALF OF THAT ARITHMETIC AND THE GAP DID NOT FOLLOW.
    /// A wave is delivered in ceil(count / groupSize) groups, so cutting the count ~40% cut the
    /// number of groups by about as much while spawnInterval stayed put: spans are now 12.6 / 17.6
    /// / 18.4 / 24.0 / 25.0 s. Body arrivals barely moved (0.87 / 1.02 / 1.36 / 1.33 / 1.56 per
    /// second) — but each body carries 5/3 the HP, so the same pool now lands in ~55% of the time.
    /// This fixture therefore pins the SPAN off the live constants rather than restating seconds,
    /// and DeepStartTests owns the chained version of the same sum. Whether 7.3 + 0.40*wave is
    /// still the right gap after the cut is a TUNING question this fixture cannot answer; it can
    /// only refuse to hide the change, which is what deriving the span does.
    ///
    /// AND WHY EnemyHealthPerWave IS BACK AT 0.40 (Zo, 2026-08-27: "can scale health on later
    /// waves"). It had been cut 0.40 → 0.30 earlier the same day on the reading that the slope
    /// bound every loadout the demo SELLS and neither loadout it was written to stop: at 0.40 the
    /// wave-5 kills/s ceiling was MK-1 T0 0.69, MR-35 T0 0.48, MR-35 T2 0.89 against a ~0.90
    /// acquisition cap. THE OTHER HALF OF THAT ARITHMETIC THEN MOVED. The Primary tier was raised
    /// ~2x in the same session, so the ceiling the cut was buying is bought by the weapon instead.
    /// 0.40 is affordable again. The slope itself never moved on 2026-08-27 — BaseEnemyHp did, so
    /// the ladder is 50 / 70 / 90 / 110 / 130 HP for a Default rather than 30 / 42 / 54 / 66 / 78.
    ///
    /// The delivery-rate argument above is UNAFFECTED IN KIND and still carries M16's intent: what
    /// stops a strong loadout holding a late wave is arrivals outrunning a ~0.90 kills/s
    /// acquisition cap, which binds every tier equally where HP could only ever bind the weak end.
    /// The spawnInterval widening is NOT part of the 0.30 → 0.40 restore and does not come back
    /// with it.
    ///
    /// Wave 1's SHAPE is untouched in KIND by the retunes — hpMul 1.0 by
    /// construction — which is deliberate: wave 1 is the one wave play data says already reads
    /// correctly. Its CONTENTS moved with the roster cut like every other wave (18 x 30 -> 11 x 50);
    /// what is fixed is the two multipliers, not the bodies.
    ///
    /// NOTHING IN THIS FILE TRANSCRIBES THE HP LADDER OR THE COUNT CURVE. Every multiplier and
    /// per-body pool is computed from WaveDirector.BaseEnemyHp and EnemyHealthPerWave; every body
    /// count is computed from BaseEnemyCount and CountPerWave. The slope moved three times in a
    /// week (0.25 → 0.40 → 0.30 → 0.40) and each move used to cost four hand edits in this fixture
    /// alone; the roster cut then proved the same point about the counts, turning a five-by-six
    /// transcribed grid red in one edit. Derive both, or the fixture becomes a second copy of
    /// WaveDirector that goes stale on its own schedule.</summary>
    public class SiphonAndWaveTests
    {
        /// <summary>The dial, level by level. Every expectation in this file and the economy
        /// files derives from these six numbers.</summary>
        private static readonly float[] DialCurve = { 1.00f, 1.10f, 1.15f, 1.25f, 1.35f, 1.50f };

        /// <summary>The authored dial-0 body curve, read off the two constants that own it. This
        /// is the ONLY place in this fixture that says how many gatekeepers a wave holds.</summary>
        private static int BaseCount(int waveIndex)
        {
            return WaveDirector.BaseEnemyCount + WaveDirector.CountPerWave * waveIndex;
        }

        /// <summary>WaveDirector's documented rounding, reproduced so a dial column can be DERIVED
        /// rather than transcribed: snap to 4 decimals first, then midpoints away from zero.</summary>
        private static int Scaled(int baseCount, float dial)
        {
            return (int)Math.Round(
                Math.Round((double)baseCount * dial, 4, MidpointRounding.AwayFromZero),
                MidpointRounding.AwayFromZero);
        }

        /// <summary>Groups a wave is delivered in. The last group is short whenever groupSize does
        /// not divide the count, which is why this is a ceiling and not a quotient.</summary>
        private static int GroupsIn(WaveSpec spec)
        {
            return (spec.enemyCount + spec.groupSize - 1) / spec.groupSize;
        }

        /// <summary>Seconds from the first group landing to the last. (groups - 1) gaps, not
        /// groups gaps — the first group costs no wait.</summary>
        private static float DeliverySeconds(WaveSpec spec)
        {
            return (GroupsIn(spec) - 1) * spec.spawnInterval;
        }

        [Test]
        public void Siphon_Level_ClampsBelowZero()
        {
            var siphon = new TetherSiphon();
            siphon.Level = -3;
            Assert.AreEqual(0, siphon.Level);
        }

        [Test]
        public void Siphon_Level_ClampsAboveFive()
        {
            var siphon = new TetherSiphon();
            siphon.Level = 99;
            Assert.AreEqual(5, siphon.Level);
        }

        [Test]
        public void Siphon_RewardCurve_IsTheHandAuthoredFiveStep()
        {
            // The developer's five percentages — 10, 15, 25, 35, 50 — as multipliers, plus the
            // idle x1. NOT 2^level, NOT a linear ramp: the steps are +10, +5, +10, +10, +15
            // points and are meant to stay uneven.
            var siphon = new TetherSiphon();
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                siphon.Level = level;
                Assert.AreEqual(DialCurve[level], siphon.RewardMultiplier, 1e-6f, "level " + level);
            }
        }

        [Test]
        public void Siphon_RewardCurve_CompoundingNeverExceedsFiftyPercent()
        {
            // The whole point of the retune, asserted as an invariant rather than a table lookup:
            // the dial can never pay more than +50%, and it never goes backwards.
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                float m = TetherSiphon.RewardMultiplierFor(level);
                Assert.IsTrue(m >= 1f, "dial must never pay less than x1 (level " + level + ")");
                Assert.IsTrue(m <= 1.5f, "dial must never exceed +50% (level " + level + ")");
                if (level > 0)
                    Assert.IsTrue(m > TetherSiphon.RewardMultiplierFor(level - 1),
                        "the curve must rise at level " + level);
            }
            Assert.AreEqual(1.5f, TetherSiphon.RewardMultiplierFor(TetherSiphon.MaxLevel), 1e-6f,
                "the top of the dial is exactly +50%");
        }

        [Test]
        public void Siphon_RewardMultiplierFor_ClampsOutOfRangeLevels()
        {
            Assert.AreEqual(1f, TetherSiphon.RewardMultiplierFor(-7), 1e-6f);
            Assert.AreEqual(1.5f, TetherSiphon.RewardMultiplierFor(99), 1e-6f);
        }

        [Test]
        public void Siphon_InstanceMultiplier_IsTheStaticCurve()
        {
            // The instance property must be nothing but a lookup into the shared static table —
            // if these ever disagree the wave director and the payout are reading different dials.
            var siphon = new TetherSiphon();
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                siphon.Level = level;
                Assert.AreEqual(TetherSiphon.RewardMultiplierFor(level), siphon.RewardMultiplier, 1e-9f,
                    "level " + level);
            }
        }

        // ---- The five-wave curve ----

        [Test]
        public void Wave_MaxWaves_IsFive()
        {
            Assert.AreEqual(5, WaveDirector.MaxWaves);
        }

        [Test]
        public void Wave_BaseEnemyHp_IsFifty()
        {
            // The Game layer sizes each gatekeeper as its KIND's maxHp * enemyHealthMultiplier, so
            // the base pool lives in the sim next to the multiplier. 50 is the Default kind's line,
            // and BaseEnemyHp IS that number rather than a second copy of it —
            // GatekeeperVariantsTests owns that guard.
            //
            // THE DECLARATION PIN, deliberately a literal: this is the one place in the fixture
            // that names the live value, so a retune costs exactly one hand edit here. Raised
            // 30 -> 50 on 2026-08-27 with the ~40% roster cut (Zo: "make them tougher"), the same
            // 5/3 every other kind's maxHp took.
            Assert.AreEqual(50f, WaveDirector.BaseEnemyHp, 1e-6f);
        }

        [Test]
        public void Wave_WholeRunCurve_ExactAtSiphonZero()
        {
            // Dial-0 run: groups 3/4/5/6/7 every 7.30/7.70/8.10/8.50/8.90s (gap re-derived
            // again; M14 width, untouched). Group width and gap are the two curves this test still
            // lists, because they are authored here and nowhere else.
            //
            // THE HP COLUMN IS DERIVED, NOT LISTED. It used to sit here as
            // { 1.0, 1.3, 1.6, 1.9, 2.2 } and the 0.30 → 0.40 restore turned four of those five
            // literals red at once — a transcribed ladder is a second copy of EnemyHealthPerWave
            // and it is the copy that goes stale. The claim this test can still make about health
            // is that the multiplier is LINEAR in the wave index off a single slope constant, and
            // that is what is asserted.
            //
            // THE COUNT COLUMN IS NOW DERIVED FOR THE SAME REASON. It sat here as
            // 18/30/42/54/66 and the 2026-08-27 roster cut turned all five red at once. What the
            // curve actually claims is that it is LINEAR in the wave index off BaseEnemyCount and
            // CountPerWave — a straight line with an authored intercept and an authored slope —
            // so that is what is asserted, and the two constants are named once, in BaseCount().
            var director = new WaveDirector();
            int[] groups = { 3, 4, 5, 6, 7 };
            float[] interval = { 7.3f, 7.7f, 8.1f, 8.5f, 8.9f };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                Assert.AreEqual(BaseCount(w), spec.enemyCount, "count, wave " + w);
                Assert.AreEqual(1f + WaveDirector.EnemyHealthPerWave * w,
                    spec.enemyHealthMultiplier, 1e-5f, "hp multiplier, wave " + w);
                Assert.AreEqual(groups[w], spec.groupSize, "group size, wave " + w);
                Assert.AreEqual(interval[w], spec.spawnInterval, 1e-5f, "spawn interval, wave " + w);
            }
        }

        [Test]
        public void Wave_SiphonZero_IsTheHordeBaseline_AndTheGapsSurvivedBothRetunes()
        {
            // THE BASELINE GUARD. Every difficulty claim in Docs/02_Numbers.md is measured at
            // dial 0. The dial retune may change every other column and NOTHING in this one.
            //   count = BaseEnemyCount + CountPerWave*w
            //   hp    = 1 + 0.40*w      group = min(3+w, 7)      spawn = 7.3 + 0.40*w
            var director = new WaveDirector();
            var w0 = director.NextWave(0, 0);
            var w1 = director.NextWave(1, 0);
            var w2 = director.NextWave(2, 0);

            Assert.AreEqual(BaseCount(0), w0.enemyCount);
            Assert.AreEqual(BaseCount(1), w1.enemyCount);
            Assert.AreEqual(BaseCount(2), w2.enemyCount);

            // The DECLARATION pin for the roster, matching the one the slope gets below: two
            // literals, in one place, cut ~40% on 2026-08-27 (Zo: "lower the volume of enemies").
            Assert.AreEqual(11, WaveDirector.BaseEnemyCount, "roster cut 18 -> 11, 2026-08-27");
            Assert.AreEqual(7, WaveDirector.CountPerWave, "roster cut 12 -> 7, 2026-08-27");

            // The multiplier is read off the slope, so these three lines survive a retune; the
            // one line below them is the deliberate exception — it is the DECLARATION pin, the
            // single place in the fixture that names the live value of the slope, so that a
            // retune has exactly one hand edit here and this is it.
            Assert.AreEqual(1f, w0.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(1f + WaveDirector.EnemyHealthPerWave, w1.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(1f + WaveDirector.EnemyHealthPerWave * 2f, w2.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(0.40f, WaveDirector.EnemyHealthPerWave, 1e-5f,
                "restored 0.30 -> 0.40, 2026-08-27 (Zo: can scale health on later waves)");

            Assert.AreEqual(7.30f, w0.spawnInterval, 1e-5f);
            Assert.AreEqual(7.70f, w1.spawnInterval, 1e-5f);
            Assert.AreEqual(8.10f, w2.spawnInterval, 1e-5f);

            // THE DIRECTION GUARD. The gap must never again shrink as the wave grows: that is
            // what stacked 103 bodies on the player in the 2026-08-27 03:18 run. Group width
            // already rises 3→7, so a shrinking gap compounds with it.
            Assert.Greater(w1.spawnInterval, w0.spawnInterval,
                "the group gap must WIDEN with the wave, never narrow");
            Assert.Greater(w2.spawnInterval, w1.spawnInterval,
                "the group gap must WIDEN with the wave, never narrow");

            // And the derived per-Default-body pools the Game layer actually spawns: the wave-1
            // body IS BaseEnemyHp by construction, and each later wave is one slope step above it.
            Assert.AreEqual(WaveDirector.BaseEnemyHp,
                WaveDirector.BaseEnemyHp * w0.enemyHealthMultiplier, 1e-3f);
            Assert.AreEqual(WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave),
                WaveDirector.BaseEnemyHp * w1.enemyHealthMultiplier, 1e-3f);
            Assert.AreEqual(WaveDirector.BaseEnemyHp * (1f + WaveDirector.EnemyHealthPerWave * 2f),
                WaveDirector.BaseEnemyHp * w2.enemyHealthMultiplier, 1e-3f);

            // THE BR-12 CAP, as arithmetic rather than a comment: the shotgun's whole volley must
            // one-shot a Default. This is the ceiling on the HP curve, so it is asserted here where
            // the curve is pinned.
            //
            // IT WAS WRITTEN AS THE LITERAL 42 AND THAT WAS ABOUT TO BECOME A LIE THAT PASSED.
            // 42 was BR-12's volley at 6 damage x 7 pellets. The Primary raise took the volley to
            // 84, and the same day's slope restore took the wave-2 body from 39 HP to exactly 42 —
            // so the stale bound went on passing, on an exact tie, while describing a cap that had
            // moved 2x. Reading the volley off the catalog is the only version of this assertion
            // that can go red for the right reason.
            //
            // IT ALSO USED TO ASK ABOUT WAVE 2, AND THAT HAD STOPPED CONSTRAINING ANYTHING.
            // BaseEnemyHp 30 -> 50 was paid for with damage 12 -> 19/pellet, so the volley went to
            // 133 against a wave-2 body of 70 — a 1.9x margin, which is a bound no realistic
            // retune could cross. The claim the cap is actually making is about the DEEPEST body:
            // 130 against 133, three points of slack. That is the number that decides whether this
            // weapon keeps its one-shot or halves to 0.395 kills/s, so that is what is asserted.
            var br12 = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            float br12Volley = WeaponWheel.DamageAtRange(br12, 0, br12.falloffStart) * br12.pellets;
            float deepestDefault = WaveDirector.BaseEnemyHp
                * director.NextWave(WaveDirector.MaxWaves - 1, 0).enemyHealthMultiplier;
            Assert.LessOrEqual(deepestDefault, br12Volley,
                $"the deepest Default ({deepestDefault}) must stay inside BR-12's one-shot "
                + $"({br12Volley} at {br12.falloffStart} m) or the weapon halves");

            // Wave 1 is still a REDISTRIBUTION of the same hp pool it has always been — 6 x 90,
            // then 18 x 30, now 11 x 50. The pool is what survives the retunes, so the pool is what
            // is asserted; the factors are read off the live constants and not listed.
            Assert.AreEqual(WaveDirector.BaseEnemyCount * WaveDirector.BaseEnemyHp,
                w0.enemyCount * WaveDirector.BaseEnemyHp * w0.enemyHealthMultiplier, 1e-3f,
                "wave 1 is BaseEnemyCount bodies at BaseEnemyHp, and hpMul 1.0 by construction");

            // DELIVERY IS DERIVED FROM THE GROUP COUNT, NOT FROM A FIXED NUMBER OF GAPS. It used to
            // read `5 * w0.spawnInterval`, which pinned a six-group wave in arithmetic that never
            // looked at the wave: the roster cut took wave 1 to four groups and that line would
            // have gone on asserting 21.0 s about a 12.6 s delivery without ever going red.
            Assert.GreaterOrEqual(GroupsIn(w0) * w0.groupSize, w0.enemyCount,
                "the groups must between them carry the whole wave");
            Assert.Less((GroupsIn(w0) - 1) * w0.groupSize, w0.enemyCount,
                "and there must be no group left with nothing in it");
            Assert.Greater(DeliverySeconds(w0), 0f,
                "wave 1 takes more than one group to land, so it has a delivery span at all");

            // PRESSURE IS ARRIVALS MEASURED AGAINST WHAT THE PLAYER CAN ACTUALLY KILL, and it
            // must rise every wave until the field can no longer be held.
            //
            // This replaced a fixed `AcquisitionCap = 0.90f` on 2026-08-27. That number was a
            // player's measured bodies-per-second ceiling from one log — and it had quietly
            // stopped describing anything. It was measured against 30 HP bodies and the MR-35 at
            // 8 damage; after the Primary raise the same rifle kills a wave-2 body at 0.925/s,
            // so a 0.90 gate was passing waves the player could already out-kill while claiming
            // they "arrive faster than a player can kill". Enshrining a measurement as a constant
            // is the same error Docs/Design/2-Dynamics.md records under B1.
            //
            // The honest quantity is the RATIO. Kill rate is derived from the catalog and the HP
            // ladder — not restated — so it tracks both curves, and it is kills/s rather than dps
            // because raising HP moves overkill boundaries in steps.
            var rifle = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float shotsPerSecond = rifle.clipSize / (rifle.clipSize / rifle.shotsPerSecond
                                                     + rifle.reloadSeconds);
            float previousRatio = -1f;
            int crossover = -1;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                float bodyHp = WaveDirector.BaseEnemyHp * spec.enemyHealthMultiplier;
                int shots = (int)System.Math.Ceiling(bodyHp / rifle.damage);
                float killsPerSecond = shotsPerSecond / shots;
                float ratio = (spec.enemyCount / DeliverySeconds(spec)) / killsPerSecond;

                Assert.Greater(ratio, previousRatio,
                    "wave " + (w + 1) + " puts no more pressure on the player than the wave "
                    + "before it — the run stops escalating, whatever the body count says");
                previousRatio = ratio;
                if (crossover < 0 && ratio >= 1f) crossover = w + 1;
            }

            // And it must actually cross: a run that never out-delivers the player can be held
            // standing still to the end, which is the shape 0-Gameplay.md exists to refuse
            // (a wave begins at the Hatch and ENDS AT A TERMINAL — the walk has to be forced).
            Assert.Greater(crossover, 0,
                "no wave out-delivers the player, so the whole run is clearable without moving");
            Assert.Less(crossover, WaveDirector.MaxWaves,
                "the field only becomes unholdable on the last wave — the escalation lands too "
                + "late to be a run rather than a spike");
            Assert.Greater(crossover, 1,
                "wave 1 already out-delivers the player; it is the calibration wave and must "
                + "stay inside what a fresh loadout can hold");

            // WAVE 1 IS THE FIXED POINT — in SHAPE. Its two multipliers are 1.0 and the untouched
            // baseline gap, and no retune has moved either: it is the one wave the logs say already
            // reads correctly (peak live 8, 0.87 kills/s, no deaths), so it is the calibration
            // point every other wave is tuned against. Its body count and per-body hp DID move on
            // 2026-08-27, together and in opposite directions, which is why the pool above holds.
            Assert.AreEqual(1.0f, w0.enemyHealthMultiplier, 1e-5f, "wave 1 hp is 1.0 by construction");
            Assert.AreEqual(7.3f, w0.spawnInterval, 1e-5f, "wave 1 gap is the curve's base");
        }

        [Test]
        public void Wave_EnemyCount_FullGrid_EveryWaveAtEveryDial()
        {
            // The ENTIRE effect of the dial, DERIVED rather than transcribed. Rows are waves,
            // columns are dial 0..5. The grid used to be a five-by-six literal and the 2026-08-27
            // roster cut turned all thirty cells red in one edit, which is what a transcription
            // costs: thirty numbers restating two.
            //
            // At dial 0 the run is BaseEnemyCount*5 + CountPerWave*(0+1+2+3+4) = 125 gatekeepers.
            var director = new WaveDirector();

            int atZeroTotal = 0;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                atZeroTotal += director.NextWave(w, 0).enemyCount;

            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                int total = 0;
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                {
                    int count = director.NextWave(w, level).enemyCount;
                    Assert.AreEqual(Scaled(BaseCount(w), DialCurve[level]), count,
                        "wave " + w + " at dial " + level);
                    total += count;
                }
                Assert.GreaterOrEqual(total, atZeroTotal,
                    "the dial only ever adds bodies (dial " + level + ")");
            }

            // THE BREAK-EVEN CLAIM, which is the reason the dial exists: 50% more bodies for 50%
            // more money. It used to be assertable as an exact equality because 210 * 1.5 = 315
            // landed on a whole body in all five waves at once. It does not any more — 125 * 1.5 is
            // 187.5 and the run is 189 — so the claim is asserted with the slack it actually has:
            // each wave rounds independently, so a whole run can sit at most MaxWaves/2 bodies off
            // the exact multiple. Pinning 315 was pinning a coincidence of the old roster.
            int atTopTotal = 0;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                atTopTotal += director.NextWave(w, TetherSiphon.MaxLevel).enemyCount;
            float exact = atZeroTotal * TetherSiphon.RewardMultiplierFor(TetherSiphon.MaxLevel);
            Assert.AreEqual(exact, atTopTotal, WaveDirector.MaxWaves / 2f,
                "the top of the dial must buy the same +50% in bodies that it pays in salvage, "
                + "to within one rounded body per wave");
        }

        [Test]
        public void Wave_CountAndReward_ShareOneTable_AndCannotDiverge()
        {
            // THE ANTI-DRIFT GUARD. The reward multiplier and the body-count multiplier are the
            // same number by construction (WaveDirector reads TetherSiphon.RewardMultiplierFor).
            // This recomputes the count from the REWARD side alone: if anyone gives the wave
            // director its own count curve, or edits one table without the other, this fails.
            var director = new WaveDirector();
            var siphon = new TetherSiphon();
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                siphon.Level = level;
                float reward = siphon.RewardMultiplier; // the money side, nothing else
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                {
                    int baseCount = director.NextWave(w, 0).enemyCount;
                    int expected = (int)Math.Round(
                        Math.Round((double)baseCount * reward, 4, MidpointRounding.AwayFromZero),
                        MidpointRounding.AwayFromZero);
                    Assert.AreEqual(expected, director.NextWave(w, level).enemyCount,
                        "wave " + w + " at dial " + level + ": bodies must follow the reward table");
                }
            }
        }

        [Test]
        public void Wave_TheDialBuysBodiesAndNothingElse_HpAndGapAreFlatAcrossTheDial()
        {
            // Health and spawn gap USED to carry +0.20/level and -0.15s/level. They do not any
            // more, on purpose: one axis of cost, matched to the one axis of reward. Group size
            // and the Hunter/Thief counts joined that list with the horde retune — the dial buys
            // BODIES, and (GatekeeperVariantsTests) every one of them is a Default.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var atZero = director.NextWave(w, 0);
                for (int level = 1; level <= TetherSiphon.MaxLevel; level++)
                {
                    var spec = director.NextWave(w, level);
                    Assert.AreEqual(atZero.enemyHealthMultiplier, spec.enemyHealthMultiplier, 1e-6f,
                        "hp must not move with the dial (wave " + w + ", dial " + level + ")");
                    Assert.AreEqual(atZero.spawnInterval, spec.spawnInterval, 1e-6f,
                        "spawn gap must not move with the dial (wave " + w + ", dial " + level + ")");
                    Assert.AreEqual(atZero.groupSize, spec.groupSize,
                        "group size must not move with the dial (wave " + w + ", dial " + level + ")");
                    Assert.AreEqual(atZero.hunterCount, spec.hunterCount,
                        "hunters must not move with the dial (wave " + w + ", dial " + level + ")");
                    Assert.AreEqual(atZero.thiefCount, spec.thiefCount,
                        "thieves must not move with the dial (wave " + w + ", dial " + level + ")");
                    Assert.IsTrue(spec.enemyCount >= atZero.enemyCount,
                        "count is the only axis the dial moves, and it only goes up");
                }
            }
        }

        [Test]
        public void Wave_BaselineSpec_Exact()
        {
            var spec = new WaveDirector().NextWave(0, 0);
            Assert.AreEqual(BaseCount(0), spec.enemyCount);
            Assert.AreEqual(spec.enemyCount, spec.defaultCount,
                "wave 1 is the game as it was: all Default, whatever the roster size");
            Assert.AreEqual(1f, spec.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(3, spec.groupSize);
            Assert.AreEqual(7.3f, spec.spawnInterval, 1e-5f);
        }

        [Test]
        public void Wave_EnemyCount_ExactFormula()
        {
            // round((BaseEnemyCount + CountPerWave*wave) * dial), midpoints AWAY FROM ZERO.
            //
            // The rounding rule is the only thing this test can still own — the four cells it used
            // to name (42*1.25 = 52.5 -> 53 and friends) were chosen because they landed on
            // midpoints at the old roster, and the roster cut moved every one of them off. So the
            // midpoint case is now SEARCHED FOR rather than transcribed: whatever wave and dial
            // produce an exact .5, that cell must round up.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                    Assert.AreEqual(Scaled(BaseCount(w), DialCurve[level]),
                        director.NextWave(w, level).enemyCount,
                        "wave " + w + " at dial " + level);

            int midpoints = 0;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                {
                    double product = Math.Round((double)BaseCount(w) * DialCurve[level], 4,
                                                MidpointRounding.AwayFromZero);
                    if (Math.Abs(product - Math.Floor(product) - 0.5) > 1e-9) continue;
                    midpoints++;
                    Assert.AreEqual((int)Math.Ceiling(product),
                        director.NextWave(w, level).enemyCount,
                        $"midpoint {product} at wave {w} dial {level} must round UP, not to even");
                }
            Assert.Greater(midpoints, 0,
                "no midpoint cell in the grid - the away-from-zero rule is untested and this "
                + "assertion has stopped constraining anything; find a wave/dial pair that hits "
                + "an exact .5, or delete the claim");
        }

        [Test]
        public void Wave_EnemyCount_CapsAtOneTwenty()
        {
            // The cap sits well above the largest real wave, so it is a guard and never a knob.
            // Where it FIRST bites is a function of the roster: at 18+12i it was wave 9, at 11+7i
            // it is wave 16, and naming either as a literal pins the roster twice. So the crossing
            // is searched for off BaseCount and both sides of it are checked.
            var director = new WaveDirector();

            int firstCapped = 0;
            while (BaseCount(firstCapped) <= WaveDirector.MaxEnemyCount) firstCapped++;

            Assert.Greater(firstCapped, WaveDirector.MaxWaves,
                "the cap must not bite inside the five authored waves");
            Assert.AreEqual(BaseCount(firstCapped - 1),
                director.NextWave(firstCapped - 1, 0).enemyCount,
                "the last wave under the cap is still the raw curve");
            Assert.AreEqual(WaveDirector.MaxEnemyCount,
                director.NextWave(firstCapped, 0).enemyCount,
                "the first wave over the curve's ceiling is clamped, not extrapolated");
            Assert.AreEqual(WaveDirector.MaxEnemyCount, director.NextWave(1000, 5).enemyCount);

            // And the largest wave a run can actually contain: deepest authored wave, top of the
            // dial. It must sit under the cap, or the cap is silently tuning the hardest wave.
            int largestReal = director.NextWave(WaveDirector.MaxWaves - 1, TetherSiphon.MaxLevel)
                .enemyCount;
            Assert.AreEqual(Scaled(BaseCount(WaveDirector.MaxWaves - 1),
                                   DialCurve[TetherSiphon.MaxLevel]), largestReal);
            Assert.Less(largestReal, WaveDirector.MaxEnemyCount,
                "the largest REAL wave is under the cap, so the cap never tunes a run");
        }

        [Test]
        public void Wave_HealthMultiplier_ExactFormula()
        {
            // 1 + EnemyHealthPerWave*waveIndex, and that is all of it — the dial argument is
            // ignored. Each expectation is the formula, evaluated off the live slope: the point of
            // this pin is the SHAPE (linear in wave, blind to dial), and a transcribed
            // 1.3 / 1.6 / 2.2 only ever pinned one particular slope.
            var director = new WaveDirector();
            float slope = WaveDirector.EnemyHealthPerWave;
            Assert.AreEqual(1f + slope, director.NextWave(1, 0).enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(1f, director.NextWave(0, 5).enemyHealthMultiplier, 1e-5f);  // dial does nothing
            Assert.AreEqual(1f + slope * 2f, director.NextWave(2, 3).enemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1f + slope * 4f, director.NextWave(4, 0).enemyHealthMultiplier, 1e-4f);
        }

        [Test]
        public void Wave_EnemyHp_TimesMultiplier_IsTheActualPool()
        {
            // What the Game layer will actually spawn at siphon 0, per DEFAULT body: 50 / 70 / 90 /
            // 110 / 130 HP at the restored 0.40 slope (the same multiplier sizes a Hunter's 33 and
            // a Thief's 200). This ladder was transcribed here as a five-literal array and it went
            // stale three times in two days — 0.40 → 0.30 shrank it to 30/39/48/57/66, the restore
            // put it back, and BaseEnemyHp 30 → 50 then moved all five again from the other end —
            // so what it asserts now is the RELATIONSHIP the Game layer depends on:
            // spec.enemyHealthMultiplier times BaseEnemyHp is the pool, it starts at BaseEnemyHp,
            // and it rises by one flat slope step per wave and never by anything else.
            var director = new WaveDirector();
            float step = WaveDirector.BaseEnemyHp * WaveDirector.EnemyHealthPerWave;
            float previous = 0f;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                float pool = WaveDirector.BaseEnemyHp * director.NextWave(w, 0).enemyHealthMultiplier;
                Assert.AreEqual(WaveDirector.BaseEnemyHp + step * w, pool, 1e-3f, "wave " + w);
                if (w > 0)
                    Assert.AreEqual(step, pool - previous, 1e-3f,
                        "the ladder rises by one FLAT step, it does not compound (wave " + w + ")");
                previous = pool;
            }
            Assert.AreEqual(WaveDirector.BaseEnemyHp, previous
                - step * (WaveDirector.MaxWaves - 1), 1e-3f,
                "the deepest wave is the base pool plus four steps, no more");
        }

        [Test]
        public void Wave_SpawnInterval_ExactFormula()
        {
            // max(0.4, 7.3 + 0.40*min(waveIndex, MaxWaves-1)). Dial argument is inert — gap does
            // not see siphon.
            var director = new WaveDirector();
            Assert.AreEqual(7.30f, director.NextWave(0, 5).spawnInterval, 1e-5f); // 7.3
            Assert.AreEqual(7.70f, director.NextWave(1, 2).spawnInterval, 1e-5f); // 7.3 + 0.40
            Assert.AreEqual(8.10f, director.NextWave(2, 3).spawnInterval, 1e-5f); // 7.3 + 0.80
            Assert.AreEqual(8.50f, director.NextWave(3, 0).spawnInterval, 1e-5f); // 7.3 + 1.20
            Assert.AreEqual(8.90f, director.NextWave(4, 1).spawnInterval, 1e-5f); // 7.3 + 1.60
        }

        [Test]
        public void Wave_SpawnInterval_HoldsTheLastAuthoredGap_AndNeverBreaksTheFloor()
        {
            // The gap rises with the wave, so a probe index past the five real waves would run
            // away without a clamp — waveIndex is held at MaxWaves-1, the same "hold the last
            // authored value" rule Row() uses for the per-kind composition tables.
            var director = new WaveDirector();
            float lastAuthored = director.NextWave(WaveDirector.MaxWaves - 1, 0).spawnInterval;
            Assert.AreEqual(8.9f, lastAuthored, 1e-5f);
            Assert.AreEqual(lastAuthored, director.NextWave(1000, 5).spawnInterval, 1e-5f,
                "probe indices hold the last authored gap, they do not extrapolate");

            // The floor is inert while the gap widens, but it is still the guard any future
            // shrinking term has to clear, so it is asserted rather than assumed.
            for (int w = 0; w < 64; w++)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                    Assert.GreaterOrEqual(director.NextWave(w, level).spawnInterval,
                        WaveDirector.MinSpawnInterval,
                        "wave " + w + " at dial " + level + " broke the spawn-interval floor");
        }

        [Test]
        public void Wave_TimeUntilNextWave_IsAlwaysZero_Obsolete()
        {
            // The draw model has no wave timer: the field survives only for WaveSpec compatibility.
            var director = new WaveDirector();
            Assert.AreEqual(0f, director.NextWave(0, 0).timeUntilNextWave, 1e-6f);
            Assert.AreEqual(0f, director.NextWave(20, 5).timeUntilNextWave, 1e-6f);
        }

        [Test]
        public void Wave_InputsAreClampedNotThrowing()
        {
            var director = new WaveDirector();
            var spec = director.NextWave(-5, 99);
            Assert.AreEqual(director.NextWave(0, 5).enemyCount, spec.enemyCount);
        }
    }
}
