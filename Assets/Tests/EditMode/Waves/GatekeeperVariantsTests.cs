using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// EditMode tests for the three gatekeeper kinds and the horde-scale wave composition.
    ///
    /// EACH KIND IS DEFINED BY WHAT IT REFUSES TO DO, so the refusals are asserted as DATA here,
    /// not left to the Game layer's state machine to honour: a Hunter's hatchDamage is 0 and a
    /// Thief's playerDamage is 0, beside the ignoresHatch / ignoresPlayer flags that say the same
    /// thing about targeting. Both halves have to hold; either alone is a rule you can half-obey.
    ///
    /// The stat lines, and the arithmetic every one of them is derived from (MK-1 L0 = 12 damage
    /// x 4 shots/s = 32 EFFECTIVE dps after the 66.7% duty cycle; player 100 hp, walk 5 m/s,
    /// sprint 7.5 m/s; hatch 300 hp):
    ///
    ///   | kind    | maxHp | player | hatch | interval | patrol | chase | refuses    |
    ///   | Default |    30 |      6 |     4 |     0.7s |    3.2 |   5.6 | —          |
    ///   | Hunter  |    20 |      6 |     0 |     0.7s |    6.5 |   6.5 | the hatch  |
    ///   | Thief     |   120 |      0 |    20 |     0.7s |    2.2 |   2.2 | the player |
    ///
    ///   Default 30 hp = 3 shots = 30/32 = 0.94s; three of them are 25.7 dps and kill a 100 hp
    ///           player in 3.9s, five are 42.9 dps and 2.3s. (At the pre-horde 18 damage, five
    ///           were 128.6 dps and 0.8s — not a fight, an execution.)
    ///   Hunter  20 hp = 2 shots = 0.63s, and 6.5 m/s beats the 5 m/s WALK while losing to the
    ///           7.5 m/s SPRINT: the first enemy that makes the Stamina track worth buying.
    ///   Thief     120 hp = 10 shots = 3.75s of focused fire, 20 hatch damage / 0.7s = 28.6 dps, so
    ///           it strips 28.6 x 3.75 = 107 of the hatch's 300 hp while being killed. It must be
    ///           intercepted, which is what turns "shoot what is closest" into a decision.
    ///
    /// Composition teaches one kind per wave (18/0/0, 26/4/0, 34/6/2 at dial 0) and THE DIAL'S
    /// SURPLUS IS ALL DEFAULT — see Composition_DialSurplusLandsOnDefaultOnly for why.
    /// Totals, gaps and the dial grid live in SiphonAndWaveTests; this file owns the kinds.
    /// </summary>
    public class GatekeeperVariantsTests
    {
        private const float PlayerDps = 32f;      // MK-1 at L0, duty cycle included (02_Numbers.md)
        private const float PlayerHp = 100f;
        private const float PlayerWalkSpeed = 5f;
        private const float PlayerSprintSpeed = 7.5f;

        // ================================================================
        //  The stat lines, exactly
        // ================================================================

        [Test]
        public void StatsFor_Default_IsTheExactStatLine()
        {
            var s = WaveDirector.StatsFor(GatekeeperKind.Default);
            Assert.AreEqual(30f, s.maxHp, 1e-6f);
            Assert.AreEqual(6f, s.playerDamage, 1e-6f);
            Assert.AreEqual(4f, s.hatchDamage, 1e-6f);
            Assert.AreEqual(0.7f, s.attackInterval, 1e-6f);
            Assert.AreEqual(3.2f, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(5.6f, s.chaseSpeed, 1e-6f);
            Assert.IsFalse(s.ignoresHatch, "the Default gatekeeper refuses nothing");
            Assert.IsFalse(s.ignoresPlayer, "the Default gatekeeper refuses nothing");
        }

        [Test]
        public void StatsFor_Hunter_IsTheExactStatLine()
        {
            var s = WaveDirector.StatsFor(GatekeeperKind.Hunter);
            Assert.AreEqual(20f, s.maxHp, 1e-6f);
            Assert.AreEqual(6f, s.playerDamage, 1e-6f);
            Assert.AreEqual(0f, s.hatchDamage, 1e-6f);
            Assert.AreEqual(0.7f, s.attackInterval, 1e-6f);
            Assert.AreEqual(6.5f, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(6.5f, s.chaseSpeed, 1e-6f);
            Assert.IsTrue(s.ignoresHatch);
            Assert.IsFalse(s.ignoresPlayer);
        }

        [Test]
        public void StatsFor_Thief_IsTheExactStatLine()
        {
            var s = WaveDirector.StatsFor(GatekeeperKind.Thief);
            Assert.AreEqual(120f, s.maxHp, 1e-6f);
            Assert.AreEqual(0f, s.playerDamage, 1e-6f);
            Assert.AreEqual(20f, s.hatchDamage, 1e-6f);
            Assert.AreEqual(0.7f, s.attackInterval, 1e-6f);
            Assert.AreEqual(2.2f, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(2.2f, s.chaseSpeed, 1e-6f);
            Assert.IsFalse(s.ignoresHatch);
            Assert.IsTrue(s.ignoresPlayer);
        }

        // ================================================================
        //  The refusals, asserted as data
        // ================================================================

        [Test]
        public void StatsFor_TheTwoFlagsAreSetOnExactlyOneKindEach()
        {
            // The flags are the design. If a third kind ever sets one, or a kind sets both (an
            // enemy that targets nothing at all), this is the test that says so.
            int ignoresHatch = 0, ignoresPlayer = 0;
            foreach (GatekeeperKind kind in System.Enum.GetValues(typeof(GatekeeperKind)))
            {
                var s = WaveDirector.StatsFor(kind);
                Assert.IsFalse(s.ignoresHatch && s.ignoresPlayer,
                    kind + " refuses both targets, which is an enemy that does nothing");
                if (s.ignoresHatch) ignoresHatch++;
                if (s.ignoresPlayer) ignoresPlayer++;
            }
            Assert.AreEqual(1, ignoresHatch, "exactly one kind ignores the hatch: Hunter");
            Assert.AreEqual(1, ignoresPlayer, "exactly one kind ignores the player: Thief");
            Assert.IsTrue(WaveDirector.StatsFor(GatekeeperKind.Hunter).ignoresHatch);
            Assert.IsTrue(WaveDirector.StatsFor(GatekeeperKind.Thief).ignoresPlayer);
        }

        [Test]
        public void StatsFor_ThiefNeverDamagesThePlayer_AndHunterNeverDamagesTheHatch()
        {
            // THE DESIGN RULES AS DATA. Zeroed damage is the same refusal the flags state, said a
            // second way so it cannot be half-obeyed: a Hunter that somehow ends up at the hatch
            // still cannot hurt it, and a Thief that somehow swings at you still cannot hurt you.
            Assert.AreEqual(0f, WaveDirector.StatsFor(GatekeeperKind.Thief).playerDamage, 1e-6f,
                "a Thief must never be able to hurt the player");
            Assert.AreEqual(0f, WaveDirector.StatsFor(GatekeeperKind.Hunter).hatchDamage, 1e-6f,
                "a Hunter must never be able to hurt the hatch");

            // ...and the flag and the zero must agree, on every kind, in both directions.
            foreach (GatekeeperKind kind in System.Enum.GetValues(typeof(GatekeeperKind)))
            {
                var s = WaveDirector.StatsFor(kind);
                if (s.ignoresHatch)
                    Assert.AreEqual(0f, s.hatchDamage, 1e-6f, kind + " ignores the hatch but can damage it");
                else
                    Assert.IsTrue(s.hatchDamage > 0f, kind + " targets the hatch but cannot damage it");

                if (s.ignoresPlayer)
                    Assert.AreEqual(0f, s.playerDamage, 1e-6f, kind + " ignores the player but can damage them");
                else
                    Assert.IsTrue(s.playerDamage > 0f, kind + " targets the player but cannot damage them");
            }
        }

        [Test]
        public void StatsFor_OnlyTheDefaultKindHasASecondGear()
        {
            // Chase speed exists because a Default gatekeeper CHANGES ITS MIND (provoke-on-damage,
            // the biggest rule in the project). Hunter and Thief never do — one is already hunting
            // at spawn, the other cannot be provoked — so for them chase == patrol by definition.
            var d = WaveDirector.StatsFor(GatekeeperKind.Default);
            Assert.IsTrue(d.chaseSpeed > d.patrolSpeed, "the Default gatekeeper speeds up when provoked");
            var h = WaveDirector.StatsFor(GatekeeperKind.Hunter);
            Assert.AreEqual(h.patrolSpeed, h.chaseSpeed, 1e-6f, "a Hunter has one speed");
            var r = WaveDirector.StatsFor(GatekeeperKind.Thief);
            Assert.AreEqual(r.patrolSpeed, r.chaseSpeed, 1e-6f, "a Thief has one speed");
        }

        [Test]
        public void StatsFor_AllThreeKindsShareOneSwingClock()
        {
            // Kinds differ by WHAT they hit and how hard, never by cadence — which is what lets
            // every dps number in the docs be read straight off the damage column.
            foreach (GatekeeperKind kind in System.Enum.GetValues(typeof(GatekeeperKind)))
                Assert.AreEqual(WaveDirector.GatekeeperAttackInterval,
                    WaveDirector.StatsFor(kind).attackInterval, 1e-6f, kind + "'s swing clock");
        }

        [Test]
        public void StatsFor_DefaultMaxHp_IS_BaseEnemyHp_NotASecondCopyOfIt()
        {
            // THE ANTI-DRIFT GUARD. BaseEnemyHp is the wave curve's hp number and StatsFor's
            // Default row is the spawner's; they are ONE constant, so a retune of either is a
            // retune of both. A copy here is exactly the kind of divergence that put 50 hp in a
            // prefab while the sim said 90 (03_Architecture.md, gotcha 1).
            Assert.AreEqual(WaveDirector.BaseEnemyHp,
                WaveDirector.StatsFor(GatekeeperKind.Default).maxHp, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperPlayerDamage,
                WaveDirector.StatsFor(GatekeeperKind.Default).playerDamage, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperHatchDamage,
                WaveDirector.StatsFor(GatekeeperKind.Default).hatchDamage, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperPatrolSpeed,
                WaveDirector.StatsFor(GatekeeperKind.Default).patrolSpeed, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperChaseSpeed,
                WaveDirector.StatsFor(GatekeeperKind.Default).chaseSpeed, 1e-6f);
        }

        [Test]
        public void StatsFor_UnknownKind_FallsBackToDefault()
        {
            // A serialized int out of range must spawn a gatekeeper, not a zero-hp one that dies on
            // the first frame.
            var fallback = WaveDirector.StatsFor((GatekeeperKind)99);
            var d = WaveDirector.StatsFor(GatekeeperKind.Default);
            Assert.AreEqual(d.maxHp, fallback.maxHp, 1e-6f);
            Assert.AreEqual(d.playerDamage, fallback.playerDamage, 1e-6f);
            Assert.AreEqual(d.hatchDamage, fallback.hatchDamage, 1e-6f);
            Assert.AreEqual(d.patrolSpeed, fallback.patrolSpeed, 1e-6f);
            Assert.AreEqual(d.chaseSpeed, fallback.chaseSpeed, 1e-6f);
            Assert.IsFalse(fallback.ignoresHatch);
            Assert.IsFalse(fallback.ignoresPlayer);
        }

        // ================================================================
        //  The arithmetic those numbers were chosen from
        // ================================================================

        [Test]
        public void Hunter_OutrunsTheWalk_AndLosesToTheSprint()
        {
            // The Hunter's whole job. If it ever beats the sprint, sprinting stops being an
            // answer and Stamina stops being worth buying; if it drops under the walk, it is not
            // a hunter at all and you can stroll away from it.
            float hunter = WaveDirector.StatsFor(GatekeeperKind.Hunter).patrolSpeed;
            Assert.IsTrue(hunter > PlayerWalkSpeed, "a Hunter must catch a walking player");
            Assert.IsTrue(hunter < PlayerSprintSpeed, "a sprinting player must be able to break away");
        }

        [Test]
        public void ThreeDefaultGatekeepers_KillAnUnupgradedPlayerInUnderFourSeconds()
        {
            // The crowd budget the whole horde retune is built around, at wave 1's 1.0x health:
            // 6 damage / 0.7s = 8.57 dps each; three = 25.7 dps = 3.9s; five = 42.9 dps = 2.3s.
            // The pre-horde 18 damage made five gatekeepers 128.6 dps and 0.8s, which is why per-body
            // damage HAD to fall when the body count tripled.
            var s = WaveDirector.StatsFor(GatekeeperKind.Default);
            float onePerSecond = s.playerDamage / s.attackInterval;
            Assert.AreEqual(8.571f, onePerSecond, 1e-3f);
            Assert.AreEqual(3.89f, PlayerHp / (3f * onePerSecond), 1e-2f, "three of them, in seconds");
            Assert.AreEqual(2.33f, PlayerHp / (5f * onePerSecond), 1e-2f, "five of them, in seconds");
        }

        [Test]
        public void Thief_StripsAboutAThirdOfTheHatch_WhileBeingKilled()
        {
            // Why a Thief cannot be left for later: 120 hp is 3.75s of an unupgraded player's FULL
            // attention (120 / 32 dps), during which it deals 20 / 0.7 = 28.6 dps to a 300 hp
            // hatch — 107 hp, better than a third of the door, per Thief, and that is the GOOD case
            // where someone is already shooting it.
            var thief = WaveDirector.StatsFor(GatekeeperKind.Thief);
            float ttk = thief.maxHp / PlayerDps;
            float hatchDps = thief.hatchDamage / thief.attackInterval;
            Assert.AreEqual(3.75f, ttk, 1e-3f, "seconds of focused fire to kill one Thief");
            Assert.AreEqual(28.57f, hatchDps, 1e-2f, "hatch damage per second");
            Assert.AreEqual(107.1f, ttk * hatchDps, 0.1f, "hatch hp lost to one Thief that got home");
            Assert.IsTrue(ttk * hatchDps > 300f / 3f, "one Thief is worth more than a third of the hatch");
        }

        [Test]
        public void ShotsToKill_IsThreeTwoAndTen_AtTheUnupgradedTwelveDamage()
        {
            // The reason the three kinds read differently through a gunsight: 12 damage a shot,
            // so 30 hp is 3 shots, 20 hp is 2, and 120 hp is 10 — a Thief costs five Hunters.
            Assert.AreEqual(3, ShotsToKill(GatekeeperKind.Default));
            Assert.AreEqual(2, ShotsToKill(GatekeeperKind.Hunter));
            Assert.AreEqual(10, ShotsToKill(GatekeeperKind.Thief));
        }

        private static int ShotsToKill(GatekeeperKind kind)
        {
            const float damagePerShot = 12f; // MK-1 at L0
            float hp = WaveDirector.StatsFor(kind).maxHp;
            return (int)System.Math.Ceiling(hp / damagePerShot);
        }

        // ================================================================
        //  Wave composition
        // ================================================================

        [Test]
        public void Composition_TeachesOneKindPerWave_ExactAtDialZero()
        {
            // wave 1: the game as it was. wave 2: the first enemy that comes for YOU.
            // wave 3: the first enemy you must CHOOSE to shoot. waves 4–5 escalate composition.
            var director = new WaveDirector();
            int[] def = { 18, 26, 34, 40, 47 };
            int[] hunters = { 0, 4, 6, 10, 12 };
            int[] thieves = { 0, 0, 2, 4, 7 };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                Assert.AreEqual(def[w], spec.defaultCount, "defaults, wave " + w);
                Assert.AreEqual(hunters[w], spec.hunterCount, "hunters, wave " + w);
                Assert.AreEqual(thieves[w], spec.thiefCount, "thieves, wave " + w);
            }
            Assert.AreEqual(0, director.NextWave(0, 5).hunterCount,
                "wave 1 is all Default at EVERY dial setting — there is nothing else to meet yet");
            Assert.AreEqual(0, director.NextWave(1, 5).thiefCount,
                "the Thief waits for wave 3 at every dial setting");
        }

        [Test]
        public void Composition_CountsSumToEnemyCount_AtEveryWaveAndEveryDial()
        {
            // THE STRUCTURAL INVARIANT the Game layer's spawn loop is allowed to rely on without
            // checking: the three kind counts partition enemyCount exactly, everywhere, including
            // at probe indices past the three real waves and at the 120 cap.
            var director = new WaveDirector();
            int[] probes = { 0, 1, 2, 3, 7, 9, 40, 1000 };
            foreach (int w in probes)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                {
                    var spec = director.NextWave(w, level);
                    Assert.AreEqual(spec.enemyCount,
                        spec.defaultCount + spec.hunterCount + spec.thiefCount,
                        "kind counts must partition enemyCount (wave " + w + ", dial " + level + ")");
                    Assert.IsTrue(spec.defaultCount >= 0 && spec.hunterCount >= 0 && spec.thiefCount >= 0,
                        "no negative kind counts (wave " + w + ", dial " + level + ")");
                }
        }

        [Test]
        public void Composition_DialSurplusLandsOnDefaultOnly()
        {
            // THE RULE: the dial buys bodies, not composition. Hunter and Thief counts are fixed per
            // wave, so every extra gatekeeper the dial pays for is a Default. The simplest rule that
            // works, and the point of it is that the dial cannot silently multiply the scariest
            // enemy: dial 5 wave 3 would otherwise be 3 Thieves and 9 Hunters — a different fight
            // rather than a bigger one.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var atZero = director.NextWave(w, 0);
                for (int level = 1; level <= TetherSiphon.MaxLevel; level++)
                {
                    var spec = director.NextWave(w, level);
                    int surplus = spec.enemyCount - atZero.enemyCount;
                    Assert.AreEqual(atZero.defaultCount + surplus, spec.defaultCount,
                        "the whole surplus is Default (wave " + w + ", dial " + level + ")");
                    Assert.AreEqual(atZero.hunterCount, spec.hunterCount,
                        "hunters are fixed per wave (wave " + w + ", dial " + level + ")");
                    Assert.AreEqual(atZero.thiefCount, spec.thiefCount,
                        "thieves are fixed per wave (wave " + w + ", dial " + level + ")");
                }
            }

            // The corner the rule exists for, spelled out: the biggest draw in the game.
            var top = director.NextWave(4, TetherSiphon.MaxLevel);
            Assert.AreEqual(99, top.enemyCount);
            Assert.AreEqual(80, top.defaultCount, "all 33 extra bodies are Default");
            Assert.AreEqual(12, top.hunterCount, "still twelve Hunters, exactly as at dial 0");
            Assert.AreEqual(7, top.thiefCount, "still seven Thieves, exactly as at dial 0");
        }

        [Test]
        public void Composition_FullGrid_DefaultsAbsorbEveryDialStep()
        {
            // The Default column of the wave x dial grid, transcribed from the running code.
            // Hunter and Thief columns are constant across each row (asserted above), so this IS
            // the whole grid.
            var director = new WaveDirector();
            int[][] defaults =
            {
                new[] { 18, 20, 21, 23, 24, 27 }, // wave 0: count itself, no other kinds yet
                new[] { 26, 29, 31, 34, 37, 41 }, // wave 1: count - 4 hunters
                new[] { 34, 38, 40, 45, 49, 55 }, // wave 2: count - 6 hunters - 2 thieves
                new[] { 40, 45, 48, 54, 59, 67 }, // wave 3: count - 10 hunters - 4 thieves
                new[] { 47, 54, 57, 64, 70, 80 }, // wave 4: count - 12 hunters - 7 thieves
            };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                    Assert.AreEqual(defaults[w][level], director.NextWave(w, level).defaultCount,
                        "defaults, wave " + w + ", dial " + level);
        }

        [Test]
        public void Composition_HunterAndThiefCounts_CannotOverrunASmallWave()
        {
            // Defensive, not a tuning claim: the fixed per-wave counts are clamped into whatever
            // enemyCount happens to be, so a future wave smaller than 8 bodies cannot produce a
            // negative defaultCount. No real wave comes close (the smallest is 18).
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                Assert.IsTrue(spec.hunterCount + spec.thiefCount <= spec.enemyCount, "wave " + w);
                Assert.IsTrue(spec.defaultCount > 0, "every wave has Default gatekeepers in it, wave " + w);
            }
        }

        // ================================================================
        //  Groups — the spawn-loop contract
        // ================================================================

        [Test]
        public void GroupSize_IsThreePlusWaveIndex_CappedAtMaxGroupSize()
        {
            // M14: authored width 3/4/5/6/7 — MaxGroupSize=7 is the ceiling, not a wave-3 clamp.
            var director = new WaveDirector();
            Assert.AreEqual(3, director.NextWave(0, 0).groupSize);
            Assert.AreEqual(4, director.NextWave(1, 0).groupSize);
            Assert.AreEqual(5, director.NextWave(2, 0).groupSize);
            Assert.AreEqual(6, director.NextWave(3, 0).groupSize);
            Assert.AreEqual(7, director.NextWave(4, 0).groupSize);
            Assert.AreEqual(WaveDirector.MaxGroupSize, director.NextWave(4, 0).groupSize,
                "wave 5 hits the cap");
            Assert.AreEqual(WaveDirector.BaseGroupSize, director.NextWave(0, 0).groupSize,
                "wave 1's group is the authored base, not a literal that can drift from it");
        }

        [Test]
        public void GroupSize_IsAtLeastOne_AndNeverBiggerThanTheWave()
        {
            // The Game layer divides by this and loops with it: it may never be 0, and a group
            // bigger than the wave it belongs to is nonsense.
            var director = new WaveDirector();
            int[] probes = { 0, 1, 2, 3, 9, 40, 1000 };
            foreach (int w in probes)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                {
                    var spec = director.NextWave(w, level);
                    Assert.IsTrue(spec.groupSize >= 1, "group size must be spawnable, wave " + w);
                    Assert.IsTrue(spec.groupSize <= spec.enemyCount,
                        "group size must fit in the wave, wave " + w + ", dial " + level);
                }
        }

        [Test]
        public void SpawnInterval_IsPerGroup_WhichIsTheOnlyReasonAHordeWaveCanBeDelivered()
        {
            // THE MEANING CHANGE, as arithmetic. spawnInterval is now seconds between GROUPS, so a
            // wave lands in (ceil(count / groupSize) - 1) * gap seconds. Read as a per-INDIVIDUAL
            // gap — which is what it used to mean — the same waves take several times longer than
            // the fight itself, and wave 3 alone would spend minutes spawning.
            var director = new WaveDirector();
            // Wave 3 is nine groups of five (M14 width); delivery matches Docs/02_Numbers.md.
            float[] asGroups = { 21.0f, 28.0f, 30.4f, 28.8f, 30.6f };
            float[] asSingles = { 71.4f, 116.0f, 155.8f, 190.8f, 221.0f };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                int groups = (spec.enemyCount + spec.groupSize - 1) / spec.groupSize;
                Assert.AreEqual(asGroups[w], (groups - 1) * spec.spawnInterval, 1e-2f,
                    "seconds to deliver wave " + w + " in groups");
                Assert.AreEqual(asSingles[w], (spec.enemyCount - 1) * spec.spawnInterval, 1e-2f,
                    "…and one at a time, which is the reading to avoid, wave " + w);
            }
            Assert.AreEqual(9, (42 + 5 - 1) / 5, "wave 3 is nine groups of five");
        }

        [Test]
        public void SpoolAssault_IsAllDefaultSingles_AndTheDormantCurveIsUntouched()
        {
            // SpoolAssault is dormant (no escape path exists). The horde pass did not retune it:
            // groupSize 1 keeps its 0.75s gap meaning exactly what it meant when spawnInterval
            // counted individuals, so waking it up later starts from the numbers it was written
            // with. It still has to satisfy the partition invariant.
            var director = new WaveDirector();
            for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
            {
                var spec = director.SpoolAssault(level);
                Assert.AreEqual(14 + 6 * level, spec.enemyCount, "dormant count curve, level " + level);
                Assert.AreEqual(spec.enemyCount, spec.defaultCount, "spool waves are all Default");
                Assert.AreEqual(0, spec.hunterCount);
                Assert.AreEqual(0, spec.thiefCount);
                Assert.AreEqual(1, spec.groupSize, "singles, as this curve was written");
                Assert.AreEqual(WaveDirector.SpoolWaveInterval, spec.timeUntilNextWave, 1e-6f);
            }
        }
    }
}
