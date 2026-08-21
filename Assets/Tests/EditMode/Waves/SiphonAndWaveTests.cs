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
    ///   enemyCount            = min(120, round((18 + 12*waveIndex) * dial))
    ///   enemyHealthMultiplier = 1 + EnemyHealthPerWave*waveIndex  (0.40; NO dial term)
    ///   groupSize             = min(3 + waveIndex, MaxGroupSize=7, enemyCount)  → 3/4/5/6/7
    ///   spawnInterval         = max(0.4, 4.2 - 0.20*waveIndex)  (NO dial term) — SECONDS
    ///                           BETWEEN GROUPS since the horde retune, not between individuals
    ///   timeUntilNextWave     = 0 (obsolete under the draw model)
    /// Per-kind composition (defaultCount/hunterCount/thiefCount) is pinned in
    /// GatekeeperVariantsTests; this file owns the totals, the dial and the gaps.
    ///
    /// THE HORDE RETUNE — 3x BODIES, 1/3 HP, GROUP SPAWNS.
    /// BaseEnemyCount 6 -> 18 and CountPerWave 4 -> 12 (18/30/42 at dial 0) so that three kinds of
    /// gatekeeper have enough bodies on screen to read as different. BaseEnemyHp fell 90 -> 30 and
    /// player damage 18 -> 6 in the same edit, which is what keeps that survivable: wave 1 is
    /// still 18 x 30 = 540 hp of gatekeeper, exactly as 6 x 90 was. Spawning became groups because
    /// 42 individuals at the old gap was minutes of pure spawning.
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
    /// WHY THE GROUP GAP IS 4.2 - 0.20*wave — DO NOT NARROW IT BACK.
    /// After the horde + swing-landing fixes, wave 2 at dial 0 spent ~220 gatekeeper-seconds against
    /// a ~101 hatch budget while the player was alive and shooting (M10). Arrivals scale with
    /// bodies; spawnInterval is the rate that sets them. 4.20 / 4.00 / 3.80 / 3.60 / 3.40 delivers
    /// 21 / 28 / 30.4 / 28.8 / 30.6s so measured ~0.93 kill/s can answer wave 2. Raising hatch
    /// HP instead is the fixes-that-fail shape. M14 restored group width to 3/4/5/6/7 after
    /// Primaries arrived; M16 raised EnemyHealthPerWave so strong loadouts stop walking through.</summary>
    public class SiphonAndWaveTests
    {
        /// <summary>The dial, level by level. Every expectation in this file and the economy
        /// files derives from these six numbers.</summary>
        private static readonly float[] DialCurve = { 1.00f, 1.10f, 1.15f, 1.25f, 1.35f, 1.50f };

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
        public void Wave_BaseEnemyHp_IsThirty()
        {
            // The Game layer sizes each gatekeeper as its KIND's maxHp * enemyHealthMultiplier, so
            // the base pool lives in the sim next to the multiplier (it was effectively 50 in a
            // prefab). 30 is the Default kind's line, and BaseEnemyHp IS that number rather than a
            // second copy of it — GatekeeperVariantsTests owns that guard.
            Assert.AreEqual(30f, WaveDirector.BaseEnemyHp, 1e-6f);
        }

        [Test]
        public void Wave_WholeRunCurve_ExactAtSiphonZero()
        {
            // Dial-0 run: 18/30/42/54/66 gatekeepers at 1.0/1.4/1.8/2.2/2.6x hp (M16 slope),
            // groups 3/4/5/6/7 every 4.20/4.00/3.80/3.60/3.40s (M10 gap; M14 width).
            var director = new WaveDirector();
            int[] counts = { 18, 30, 42, 54, 66 };
            float[] hp = { 1.0f, 1.4f, 1.8f, 2.2f, 2.6f };
            int[] groups = { 3, 4, 5, 6, 7 };
            float[] interval = { 4.2f, 4.0f, 3.8f, 3.6f, 3.4f };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                Assert.AreEqual(counts[w], spec.enemyCount, "count, wave " + w);
                Assert.AreEqual(hp[w], spec.enemyHealthMultiplier, 1e-5f, "hp multiplier, wave " + w);
                Assert.AreEqual(groups[w], spec.groupSize, "group size, wave " + w);
                Assert.AreEqual(interval[w], spec.spawnInterval, 1e-5f, "spawn interval, wave " + w);
            }
        }

        [Test]
        public void Wave_SiphonZero_IsTheHordeBaseline_AndTheGapsSurvivedBothRetunes()
        {
            // THE BASELINE GUARD. Every difficulty claim in Docs/02_Numbers.md is measured at
            // dial 0. The dial retune may change every other column and NOTHING in this one.
            //   count = 18 + 12*w
            //   hp    = 1 + 0.40*w      group = min(3+w, 7)      spawn = 4.2 - 0.20*w
            var director = new WaveDirector();
            var w0 = director.NextWave(0, 0);
            var w1 = director.NextWave(1, 0);
            var w2 = director.NextWave(2, 0);

            Assert.AreEqual(18, w0.enemyCount);
            Assert.AreEqual(30, w1.enemyCount);
            Assert.AreEqual(42, w2.enemyCount);

            Assert.AreEqual(1.0f, w0.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(1.4f, w1.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(1.8f, w2.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(WaveDirector.EnemyHealthPerWave, 0.40f, 1e-5f);

            Assert.AreEqual(4.20f, w0.spawnInterval, 1e-5f);
            Assert.AreEqual(4.00f, w1.spawnInterval, 1e-5f);
            Assert.AreEqual(3.80f, w2.spawnInterval, 1e-5f);

            // And the derived per-Default-body pools the Game layer actually spawns.
            Assert.AreEqual(30f, WaveDirector.BaseEnemyHp * w0.enemyHealthMultiplier, 1e-3f);
            Assert.AreEqual(42f, WaveDirector.BaseEnemyHp * w1.enemyHealthMultiplier, 1e-3f);
            Assert.AreEqual(54f, WaveDirector.BaseEnemyHp * w2.enemyHealthMultiplier, 1e-3f);

            // Wave 1 is still a REDISTRIBUTION of the pre-horde wave: 18 bodies at 30 hp = the
            // same 540 hp pool. M10 widened the five group gaps so delivery is 21.0s, not 13.0s.
            Assert.AreEqual(540f, w0.enemyCount * WaveDirector.BaseEnemyHp * w0.enemyHealthMultiplier, 1e-3f);
            Assert.AreEqual(6, (w0.enemyCount + w0.groupSize - 1) / w0.groupSize, "groups in wave 1");
            Assert.AreEqual(21.0f, 5 * w0.spawnInterval, 1e-4f, "seconds to deliver wave 1");
        }

        [Test]
        public void Wave_EnemyCount_FullGrid_EveryWaveAtEveryDial()
        {
            // The ENTIRE effect of the dial, transcribed from the running code. Rows are waves,
            // columns are dial 0..5. Run totals are 210/231/242/265/284/315 — the L5 run is exactly
            // 1.5x the L0 run, which is the same 1.5x the L5 payout is.
            var director = new WaveDirector();
            int[][] grid =
            {
                new[] { 18, 20, 21, 23, 24, 27 }, // wave 0, base 18
                new[] { 30, 33, 35, 38, 41, 45 }, // wave 1, base 30
                new[] { 42, 46, 48, 53, 57, 63 }, // wave 2, base 42
                new[] { 54, 59, 62, 68, 73, 81 }, // wave 3, base 54
                new[] { 66, 73, 76, 83, 89, 99 }, // wave 4, base 66
            };
            int[] runTotals = { 210, 231, 242, 265, 284, 315 };

            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                int total = 0;
                for (int w = 0; w < WaveDirector.MaxWaves; w++)
                {
                    int count = director.NextWave(w, level).enemyCount;
                    Assert.AreEqual(grid[w][level], count, "wave " + w + " at dial " + level);
                    total += count;
                }
                Assert.AreEqual(runTotals[level], total, "whole-run gatekeepers at dial " + level);
            }
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
            Assert.AreEqual(18, spec.enemyCount);
            Assert.AreEqual(18, spec.defaultCount, "wave 1 is the game as it was: all Default");
            Assert.AreEqual(1f, spec.enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(3, spec.groupSize);
            Assert.AreEqual(4.2f, spec.spawnInterval, 1e-5f);
        }

        [Test]
        public void Wave_EnemyCount_ExactFormula()
        {
            // round((18 + 12*wave) * dial), midpoints away from zero.
            var director = new WaveDirector();
            Assert.AreEqual(46, director.NextWave(2, 1).enemyCount); // 42 * 1.10 = 46.2  -> 46
            Assert.AreEqual(53, director.NextWave(2, 3).enemyCount); // 42 * 1.25 = 52.5  -> 53
            Assert.AreEqual(45, director.NextWave(1, 5).enemyCount); // 30 * 1.50 = 45.0  -> 45
            Assert.AreEqual(35, director.NextWave(1, 2).enemyCount); // 30 * 1.15 = 34.5  -> 35
        }

        [Test]
        public void Wave_EnemyCount_CapsAtOneTwenty()
        {
            // The cap sits above the largest real wave (99 at wave 5, dial 5).
            var director = new WaveDirector();
            Assert.AreEqual(114, director.NextWave(8, 0).enemyCount);  // 18 + 96 = 114, still under
            Assert.AreEqual(120, director.NextWave(9, 0).enemyCount);  // 126 → capped
            Assert.AreEqual(120, director.NextWave(15, 5).enemyCount);
            Assert.AreEqual(120, director.NextWave(1000, 5).enemyCount);
            Assert.AreEqual(99, director.NextWave(4, 5).enemyCount,
                "the largest REAL wave is under the cap, so the cap never tunes a run");
        }

        [Test]
        public void Wave_HealthMultiplier_ExactFormula()
        {
            // 1 + EnemyHealthPerWave*waveIndex, and that is all of it — the dial argument is ignored.
            var director = new WaveDirector();
            Assert.AreEqual(1.4f, director.NextWave(1, 0).enemyHealthMultiplier, 1e-5f);
            Assert.AreEqual(1.0f, director.NextWave(0, 5).enemyHealthMultiplier, 1e-5f);  // dial does nothing
            Assert.AreEqual(1.8f, director.NextWave(2, 3).enemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(2.6f, director.NextWave(4, 0).enemyHealthMultiplier, 1e-4f);
        }

        [Test]
        public void Wave_EnemyHp_TimesMultiplier_IsTheActualPool()
        {
            // What the Game layer will actually spawn at siphon 0, per DEFAULT body:
            // 30 / 42 / 54 / 66 / 78 HP (the same multiplier sizes a Hunter's 20 and a Thief's 120).
            var director = new WaveDirector();
            float[] expected = { 30f, 42f, 54f, 66f, 78f };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                Assert.AreEqual(expected[w],
                    WaveDirector.BaseEnemyHp * director.NextWave(w, 0).enemyHealthMultiplier,
                    1e-3f, "wave " + w);
        }

        [Test]
        public void Wave_SpawnInterval_ExactFormula()
        {
            // max(0.4, 4.2 - 0.20*waveIndex). Dial argument is inert — gap does not see siphon.
            var director = new WaveDirector();
            Assert.AreEqual(4.20f, director.NextWave(0, 5).spawnInterval, 1e-5f); // 4.2
            Assert.AreEqual(4.00f, director.NextWave(1, 2).spawnInterval, 1e-5f); // 4.2 - 0.20
            Assert.AreEqual(3.80f, director.NextWave(2, 3).spawnInterval, 1e-5f); // 4.2 - 0.40
            Assert.AreEqual(3.60f, director.NextWave(3, 0).spawnInterval, 1e-5f); // 4.2 - 0.60
            Assert.AreEqual(3.40f, director.NextWave(4, 1).spawnInterval, 1e-5f); // 4.2 - 0.80
        }

        [Test]
        public void Wave_SpawnInterval_NeverBelowFloor()
        {
            var director = new WaveDirector();
            Assert.AreEqual(
                WaveDirector.MinSpawnInterval,
                director.NextWave(1000, 5).spawnInterval,
                1e-5f);
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
