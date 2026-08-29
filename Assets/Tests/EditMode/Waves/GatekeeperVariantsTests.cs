using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// EditMode tests for the four gatekeeper kinds and the horde-scale wave composition.
    ///
    /// EACH KIND IS DEFINED BY WHAT IT REFUSES TO DO, so the refusals are asserted as DATA here,
    /// not left to the Game layer's state machine to honour: a Hunter's hatchDamage is 0 and a
    /// Thief's playerDamage is 0, beside the ignoresHatch / ignoresPlayer flags that say the same
    /// thing about targeting. Both halves have to hold; either alone is a rule you can half-obey.
    /// The Cloaker's refusal (being seen) is a STATE in the Game layer, so what is pinned here is
    /// the opposite claim: its stat line is Default's with one owned number, the hp.
    ///
    /// FEWER AND TOUGHER, 2026-08-27 (Zo: "lower the volume of enemies and make them tougher").
    /// Every kind's maxHp rose by exactly 5/3 in one pass — Default 30 -> 50, Hunter 20 -> 33,
    /// Thief 120 -> 200, Cloaker 45 -> 75 — against a roster cut of about 40%. The RATIOS between
    /// the kinds are what this file is really about and 5/3 applied to all four leaves every one of
    /// them intact: a Cloaker is still 1.5 Defaults, a Thief is still four of them. So the tests
    /// below are written against the ratios and against the live constants, and the only maxHp
    /// literals left are the four declaration pins in the stat-line tests.
    ///
    /// The stat lines, and the arithmetic every one of them is derived from (MK-1 T0 = 30 damage
    /// x 3 shots/s = 59.0 sustained dps once the 8-round clip and 1.4 s reload are counted; player
    /// 100 hp, walk 5 m/s, sprint 7.5 m/s; hatch 300 hp):
    ///
    ///   | kind    | maxHp | player | hatch | interval | patrol | chase | refuses    |
    ///   | Default |    50 |      6 |     4 |     0.7s |    2.8 |   4.6 | —          |
    ///   | Hunter  |    33 |      6 |     0 |     0.7s |    5.6 |   5.6 | the hatch  |
    ///   | Thief   |   200 |      0 |    20 |     0.7s |    2.2 |   2.2 | the player |
    ///   | Cloaker |    75 |      6 |     4 |     0.7s |    2.8 |   4.6 | being seen (a state, not a flag) |
    ///
    ///   Default 50 hp = 2 MK-1 shots; three of them are 25.7 dps and kill a 100 hp player in
    ///           3.9s, five are 42.9 dps and 2.3s. (At the pre-horde 18 damage, five were 128.6
    ///           dps and 0.8s — not a fight, an execution.) The per-swing damage did NOT move on
    ///           2026-08-27, so the crowd budget is the one thing the retune left alone.
    ///   Hunter  33 hp = 2 MK-1 shots, and 5.6 m/s beats the 5 m/s WALK while losing to the
    ///           7.5 m/s SPRINT: the first enemy that makes the Stamina track worth buying.
    ///   Thief   200 hp = 7 MK-1 shots = 3.4s of focused fire, 20 hatch damage / 0.7s = 28.6 dps,
    ///           so it strips ~97 of the hatch's 300 hp while being killed. It must be
    ///           intercepted, which is what turns "shoot what is closest" into a decision.
    ///
    /// Composition teaches one kind per wave and THE DIAL'S SURPLUS IS ALL DEFAULT — see
    /// Composition_DialSurplusLandsOnDefaultOnly for why. The Hunter/Thief/Cloaker rows are the
    /// only tables restated here, because WaveDirector keeps them private and there is nothing to
    /// read them off; every DEFAULT count is derived as enemyCount minus the other three, which is
    /// what the sim itself does. Totals, gaps and the dial grid live in SiphonAndWaveTests; this
    /// file owns the kinds.
    /// </summary>
    public class GatekeeperVariantsTests
    {
        /// <summary>The unupgraded player's sustained dps, READ OFF THE CATALOG. It sat here as a
        /// const 32f — "MK-1 at L0, duty cycle included" — and that number described a 12-damage,
        /// 4-shot sidearm that has not existed since the Primary raise took MK-1 to 30 x 3.0. The
        /// literal survived because the only test using it happened to stay green: a Thief was 120
        /// hp and 120/32 came out at the 3.75 s the comment claimed. The 2026-08-27 hp pass is what
        /// exposed it, so it is derived now and cannot drift behind the row again.</summary>
        private static float PlayerDps
        {
            get
            {
                var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
                float cycle = mk1.clipSize / mk1.shotsPerSecond + mk1.reloadSeconds;
                return mk1.clipSize * mk1.damage * System.Math.Max(1, mk1.pellets) / cycle;
            }
        }

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
            Assert.AreEqual(50f, s.maxHp, 1e-6f);   // 30 x 5/3, 2026-08-27
            Assert.AreEqual(6f, s.playerDamage, 1e-6f);
            Assert.AreEqual(4f, s.hatchDamage, 1e-6f);
            Assert.AreEqual(0.7f, s.attackInterval, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperPatrolSpeed, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperChaseSpeed, s.chaseSpeed, 1e-6f);
            Assert.IsFalse(s.ignoresHatch, "the Default gatekeeper refuses nothing");
            Assert.IsFalse(s.ignoresPlayer, "the Default gatekeeper refuses nothing");
        }

        [Test]
        public void StatsFor_Hunter_IsTheExactStatLine()
        {
            var s = WaveDirector.StatsFor(GatekeeperKind.Hunter);
            Assert.AreEqual(33f, s.maxHp, 1e-6f);   // 20 x 5/3, 2026-08-27
            Assert.AreEqual(6f, s.playerDamage, 1e-6f);
            Assert.AreEqual(0f, s.hatchDamage, 1e-6f);
            Assert.AreEqual(0.7f, s.attackInterval, 1e-6f);
            Assert.AreEqual(5.6f, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(5.6f, s.chaseSpeed, 1e-6f);
            Assert.IsTrue(s.ignoresHatch);
            Assert.IsFalse(s.ignoresPlayer);
        }

        [Test]
        public void StatsFor_Thief_IsTheExactStatLine()
        {
            var s = WaveDirector.StatsFor(GatekeeperKind.Thief);
            Assert.AreEqual(200f, s.maxHp, 1e-6f);  // 120 x 5/3, 2026-08-27
            Assert.AreEqual(0f, s.playerDamage, 1e-6f);
            Assert.AreEqual(20f, s.hatchDamage, 1e-6f);
            Assert.AreEqual(0.7f, s.attackInterval, 1e-6f);
            Assert.AreEqual(2.2f, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(2.2f, s.chaseSpeed, 1e-6f);
            Assert.IsFalse(s.ignoresHatch);
            Assert.IsTrue(s.ignoresPlayer);
        }

        [Test]
        public void StatsFor_Cloaker_IsDefaultsLineWithItsOwnHp()
        {
            // The Cloaker's ONE owned number is 75 hp (45 x 5/3 with the 2026-08-27 pass); every
            // other figure IS Default's constant, read from the same place, so a Default retune
            // retunes the Cloaker with it and the watch rule stays the only thing that separates
            // them. The rule the number encodes is "one and a half Defaults", and that is asserted
            // as a RATIO below rather than left implied by two literals that moved together.
            var s = WaveDirector.StatsFor(GatekeeperKind.Cloaker);
            Assert.AreEqual(75f, s.maxHp, 1e-6f);
            Assert.AreEqual(1.5f, s.maxHp / WaveDirector.StatsFor(GatekeeperKind.Default).maxHp,
                1e-4f, "a Cloaker is one and a half Defaults - that ratio IS the kind");
            Assert.AreEqual(WaveDirector.GatekeeperPlayerDamage, s.playerDamage, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperHatchDamage, s.hatchDamage, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperAttackInterval, s.attackInterval, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperPatrolSpeed, s.patrolSpeed, 1e-6f);
            Assert.AreEqual(WaveDirector.GatekeeperChaseSpeed, s.chaseSpeed, 1e-6f);
            Assert.IsFalse(s.ignoresHatch, "the Cloaker's refusal is a state, never a pushed flag");
            Assert.IsFalse(s.ignoresPlayer, "the Cloaker's refusal is a state, never a pushed flag");
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
        public void StatsFor_SecondGearBelongsToTheKindsThatChangeTheirMind()
        {
            // Chase speed exists because a gatekeeper CHANGES ITS MIND (provoke-on-damage, the
            // biggest rule in the project). Hunter and Thief never do — one is already hunting
            // at spawn, the other cannot be provoked — so for them chase == patrol by
            // definition. The Cloaker is a Default whenever it is unobserved, provocation
            // included, so it carries Default's two gears. (Was named
            // OnlyTheDefaultKindHasASecondGear until the fourth kind made that claim false.)
            var d = WaveDirector.StatsFor(GatekeeperKind.Default);
            Assert.IsTrue(d.chaseSpeed > d.patrolSpeed, "the Default gatekeeper speeds up when provoked");
            var c = WaveDirector.StatsFor(GatekeeperKind.Cloaker);
            Assert.IsTrue(c.chaseSpeed > c.patrolSpeed, "an unobserved Cloaker is a Default, gears included");
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
        public void Thief_StripsASixthOfTheHatch_AndTheDeepestWavesComplementTakesMost()
        {
            // Why a Thief cannot be left for later: 200 hp is 3.4 s of an unupgraded player's FULL
            // attention (200 / 59.0 dps), during which it deals 20 / 0.7 = 28.6 dps to a 300 hp
            // hatch — about 97 hp, close to a third of the door, per Thief, and that is the GOOD
            // case where someone is already shooting it.
            //
            // TWO NUMBERS MOVED UNDER THIS TEST AT ONCE AND THEY PULLED OPPOSITE WAYS. The Thief
            // went 120 -> 200 hp, which lengthens the kill; PlayerDps stopped being the stale 32
            // and became the sidearm's real 59.0, which shortens it. The SECONDS are therefore not
            // the claim and never were — the claim is the FRACTION OF THE DOOR one Thief takes with
            // it, so that is what is pinned, off ShipSystems.HatchMaxHp rather than a literal 300.
            //
            // It is now just UNDER a third (32.3%) where it used to be just over (35.7%), because
            // the hp raise did not quite keep pace with the sidearm figure the old constant was
            // hiding. Asserted as a two-sided window: a Thief that stops being worth roughly a
            // third of the hatch stops being the enemy that has to be intercepted, and one worth
            // half the door is a different design problem in the other direction.
            var thief = WaveDirector.StatsFor(GatekeeperKind.Thief);
            float ttk = thief.maxHp / PlayerDps;
            float hatchDps = thief.hatchDamage / thief.attackInterval;
            float doorLost = ttk * hatchDps;
            float fraction = doorLost / ShipSystems.HatchMaxHp;
            // The other half of "it must be intercepted": it is FOUR Defaults of focused fire, so
            // answering one is a real commitment of attention and not an incidental trigger pull.
            Assert.AreEqual(4f, thief.maxHp / WaveDirector.StatsFor(GatekeeperKind.Default).maxHp,
                1e-4f, "a Thief is four Defaults of shooting - that ratio IS the heavy");
            // A SIXTH EACH NOW, AND THAT IS THE POINT OF THE DOOR BEING DOUBLED (2026-08-28).
            // This pinned a third, against a 300 hp hatch sized for Hatch-bank waves where the
            // player stands on it. Field waves put the door in the fight from 156 m away, so Zo
            // doubled the pool — and one Thief's share halved with it, exactly as intended.
            Assert.AreEqual(1f / 6f, fraction, 0.03f,
                $"one Thief that got home costs {doorLost:0.#} of the hatch's "
                + $"{ShipSystems.HatchMaxHp} hp ({fraction:0.0%})");

            // THE INTERCEPT CLAIM SURVIVES THE RETUNE, and it is the one worth guarding: a
            // single Thief is a slice, but the wave-5 complement is most of the door. Pinned
            // against the authored count so it tracks the roster instead of a frozen number —
            // if either the count or the hatch moves far enough that a full complement stops
            // threatening the door, Thieves have quietly become ignorable.
            int deepest = WaveDirector.MaxWaves - 1;
            int thieves = new WaveDirector().NextWave(deepest, 0).thiefCount;
            float complement = thieves * doorLost / ShipSystems.HatchMaxHp;
            Assert.Greater(complement, 0.5f,
                $"the deepest wave's {thieves} Thieves cost only {complement:0.0%} of the door "
                + "between them - nothing forces the player to intercept them");
        }

        [Test]
        public void ShotsToKill_IsTwoTwoAndSeven_AtTheUnupgradedSidearm()
        {
            // Renamed from _IsThreeTwoAndTen_AtTheUnupgradedTwelveDamage. Both halves of that name
            // were lies by 2026-08-27: the sidearm is 30 damage a shot, not 12, and the 5/3 hp
            // pass moved every count in the sentence.
            //
            // The reason the kinds read differently through a gunsight: at 30 damage a shot, a
            // 50 hp Default is 2, a 33 hp Hunter is 2, and a 200 hp Thief is 7 — a Thief costs
            // three and a half Hunters. The Cloaker's 75 lands at 3: one shot more than the
            // Default it impersonates, still nothing like a Thief.
            //
            // AND THE HUNTER PIN IS NOW ON A BOUNDARY WORTH SAYING OUT LOUD. A Hunter used to be
            // exactly two shots at 20 hp and 12 damage; at 33 against 30 it is two shots by three
            // points. It reads as "the same as a Default" through the sight even though it is
            // two-thirds of one, which is the granularity trap the catalog comments keep warning
            // about — one point of damage either way on either row flips it.
            Assert.AreEqual(2, ShotsToKill(GatekeeperKind.Default));
            Assert.AreEqual(2, ShotsToKill(GatekeeperKind.Hunter));
            Assert.AreEqual(7, ShotsToKill(GatekeeperKind.Thief));
            Assert.AreEqual(3, ShotsToKill(GatekeeperKind.Cloaker));

            // The claim underneath the counts, which survives a retune of either side: the shot
            // ladder must stay strictly ordered Hunter <= Default < Cloaker < Thief, or the kinds
            // stop being distinguishable by how long they take to kill.
            Assert.LessOrEqual(ShotsToKill(GatekeeperKind.Hunter), ShotsToKill(GatekeeperKind.Default));
            Assert.Less(ShotsToKill(GatekeeperKind.Default), ShotsToKill(GatekeeperKind.Cloaker));
            Assert.Less(ShotsToKill(GatekeeperKind.Cloaker), ShotsToKill(GatekeeperKind.Thief));
        }

        /// <summary>Shots from the FREE sidearm at zero wheel points. Read off the catalog: the
        /// literal 12 that used to live here described a row the Primary raise deleted.</summary>
        private static int ShotsToKill(GatekeeperKind kind)
        {
            var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            float damagePerShot = mk1.damage * System.Math.Max(1, mk1.pellets);
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
            // wave 3: the first enemy you must CHOOSE how to answer — the Thief through the
            // gun. wave 4: the Cloaker pair through the gaze — one debut per wave, so each
            // rule gets read alone. wave 5 escalates composition.
            //
            // The three kind rows were cut with the roster on 2026-08-27 — Hunters peak 12 -> 7,
            // Thieves 7 -> 4, Cloakers 3 -> 2 — and they are restated here, and ONLY here, because
            // WaveDirector keeps them private and there is nothing to read them off.
            // THE DEFAULT ROW IS NOT: it is enemyCount minus the other three, which is the
            // sim's own definition of it, so the shape of the teaching curve is asserted without
            // this fixture holding a fourth table that can drift on its own.
            var director = new WaveDirector();
            int[] hunters = { 0, 2, 4, 6, 7 };
            int[] thieves = { 0, 0, 1, 2, 4 };
            int[] cloakers = { 0, 0, 0, 1, 2 };
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                Assert.AreEqual(hunters[w], spec.hunterCount, "hunters, wave " + w);
                Assert.AreEqual(thieves[w], spec.thiefCount, "thieves, wave " + w);
                Assert.AreEqual(cloakers[w], spec.cloakerCount, "cloakers, wave " + w);
                Assert.AreEqual(spec.enemyCount - hunters[w] - thieves[w] - cloakers[w],
                    spec.defaultCount, "defaults, wave " + w);
            }

            // THE TEACHING SHAPE, which is the claim the three tables exist to make: exactly one
            // NEW kind appears per wave from wave 2 on, and no kind ever thins out once it has
            // been introduced. The roster cut halved the rows and left both properties intact.
            for (int w = 1; w < WaveDirector.MaxWaves; w++)
            {
                var prev = director.NextWave(w - 1, 0);
                var spec = director.NextWave(w, 0);
                int debuts = (prev.hunterCount == 0 && spec.hunterCount > 0 ? 1 : 0)
                           + (prev.thiefCount == 0 && spec.thiefCount > 0 ? 1 : 0)
                           + (prev.cloakerCount == 0 && spec.cloakerCount > 0 ? 1 : 0);
                Assert.LessOrEqual(debuts, 1,
                    "two kinds debuting in wave " + w + " means neither rule gets read");
                Assert.GreaterOrEqual(spec.hunterCount, prev.hunterCount, "hunters, wave " + w);
                Assert.GreaterOrEqual(spec.thiefCount, prev.thiefCount, "thieves, wave " + w);
                Assert.GreaterOrEqual(spec.cloakerCount, prev.cloakerCount, "cloakers, wave " + w);
            }
            Assert.AreEqual(0, director.NextWave(0, 5).hunterCount,
                "wave 1 is all Default at EVERY dial setting — there is nothing else to meet yet");
            Assert.AreEqual(0, director.NextWave(1, 5).thiefCount,
                "the Thief waits for wave 3 at every dial setting");
            Assert.AreEqual(0, director.NextWave(2, 5).cloakerCount,
                "the Cloaker waits for wave 4 at every dial setting — it never shares a debut");
        }

        [Test]
        public void Composition_CountsSumToEnemyCount_AtEveryWaveAndEveryDial()
        {
            // THE STRUCTURAL INVARIANT the Game layer's spawn loop is allowed to rely on without
            // checking: the four kind counts partition enemyCount exactly, everywhere, including
            // at probe indices past the five real waves and at the 120 cap.
            var director = new WaveDirector();
            int[] probes = { 0, 1, 2, 3, 7, 9, 40, 1000 };
            foreach (int w in probes)
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                {
                    var spec = director.NextWave(w, level);
                    Assert.AreEqual(spec.enemyCount,
                        spec.defaultCount + spec.hunterCount + spec.thiefCount + spec.cloakerCount,
                        "kind counts must partition enemyCount (wave " + w + ", dial " + level + ")");
                    Assert.IsTrue(spec.defaultCount >= 0 && spec.hunterCount >= 0
                        && spec.thiefCount >= 0 && spec.cloakerCount >= 0,
                        "no negative kind counts (wave " + w + ", dial " + level + ")");
                }
        }

        [Test]
        public void Composition_DialSurplusLandsOnDefaultOnly()
        {
            // THE RULE: the dial buys bodies, not composition. Hunter, Thief and Cloaker counts
            // are fixed per wave, so every extra gatekeeper the dial pays for is a Default. The
            // simplest rule that works, and the point of it is that the dial cannot silently
            // multiply the scariest enemy: dial 5 wave 3 would otherwise be 3 Thieves and 9
            // Hunters — a different fight rather than a bigger one. For the Cloaker the rule is
            // load-bearing twice over: a dial that multiplied Cloakers would multiply frozen
            // free salvage, the pre-mortem this kind ships against.
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
                    Assert.AreEqual(atZero.cloakerCount, spec.cloakerCount,
                        "cloakers are fixed per wave (wave " + w + ", dial " + level + ")");
                }
            }

            // The corner the rule exists for, spelled out: the biggest draw in the game. Every
            // figure is read off the dial-0 wave beside it, so the four literals that used to sit
            // here (99 / 77 / 12 / 7 / 3, all five of them red after the roster cut) are gone.
            var deepZero = director.NextWave(WaveDirector.MaxWaves - 1, 0);
            var top = director.NextWave(WaveDirector.MaxWaves - 1, TetherSiphon.MaxLevel);
            int extra = top.enemyCount - deepZero.enemyCount;
            Assert.Greater(extra, 0, "the top of the dial must actually add bodies to buy");
            Assert.AreEqual(deepZero.defaultCount + extra, top.defaultCount,
                "all " + extra + " extra bodies are Default");
            Assert.AreEqual(deepZero.hunterCount, top.hunterCount,
                "the same Hunters as at dial 0, and the dial may never add one");
            Assert.AreEqual(deepZero.thiefCount, top.thiefCount,
                "the same Thieves as at dial 0, and the dial may never add one");
            Assert.AreEqual(deepZero.cloakerCount, top.cloakerCount,
                "the same Cloakers as at dial 0, and the dial may never add one");
        }

        [Test]
        public void Composition_FullGrid_DefaultsAbsorbEveryDialStep()
        {
            // The Default column of the wave x dial grid. Hunter, Thief and Cloaker columns are
            // constant across each row (asserted above), so this IS the whole grid.
            //
            // IT WAS A THIRTY-CELL LITERAL AND THE ROSTER CUT TURNED EVERY CELL RED AT ONCE.
            // Thirty numbers restating two constants and three private rows is not a pin, it is a
            // second copy of NextWave. What the grid actually claims is that the Default column
            // ABSORBS the whole dial: at every wave and every dial setting, defaultCount is
            // enemyCount minus the three fixed rows, with the fixed rows read from the dial-0 wave
            // of the same index. That holds cell by cell and does not have to be re-typed when
            // either the roster or the composition moves.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var atZero = director.NextWave(w, 0);
                int fixedKinds = atZero.hunterCount + atZero.thiefCount + atZero.cloakerCount;
                for (int level = 0; level <= TetherSiphon.MaxLevel; level++)
                {
                    var spec = director.NextWave(w, level);
                    Assert.AreEqual(spec.enemyCount - fixedKinds, spec.defaultCount,
                        "defaults, wave " + w + ", dial " + level);
                }
            }

            // ...and the grid must actually VARY, or the paragraph above is describing a table of
            // one number. The deepest wave's Default column has to climb across the dial.
            Assert.Greater(director.NextWave(WaveDirector.MaxWaves - 1, TetherSiphon.MaxLevel)
                               .defaultCount,
                           director.NextWave(WaveDirector.MaxWaves - 1, 0).defaultCount,
                "the dial has to move the Default column, or this grid pins nothing");
        }

        [Test]
        public void Composition_HunterAndThiefCounts_CannotOverrunASmallWave()
        {
            // Defensive, not a tuning claim: the fixed per-wave counts are clamped into whatever
            // enemyCount happens to be, so a future wave smaller than 8 bodies cannot produce a
            // negative defaultCount. No real wave comes close — the smallest is 11 bodies with no
            // other kind in it, and the tightest is wave 5 at 39 with 13 non-Defaults.
            var director = new WaveDirector();
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                Assert.IsTrue(spec.hunterCount + spec.thiefCount + spec.cloakerCount <= spec.enemyCount,
                    "wave " + w);
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
            // Delivery still RISES with the wave (12.6 → 25.0 s) instead of sitting near-flat while
            // the roster grows — see SiphonAndWaveTests for the 2026-08-27 log that bought that
            // change, and for the note that the roster cut shortened every one of these spans by
            // about 40% without the gap curve being re-derived.
            //
            // BOTH SECOND COLUMNS WERE TRANSCRIBED AND BOTH WENT STALE TOGETHER. The pair of
            // five-literal arrays here said 21.0/30.8/36.8/38.4/45.0 as groups and
            // 71.4/127.6/188.6/254.4/325.0 as singles, and the roster cut moved all ten. What the
            // test is actually claiming is a RATIO — that reading the gap per individual inflates
            // delivery several times over — so the ratio is what is asserted, and the absolute
            // seconds are left to the fixture that owns the gap curve.
            float previousSpan = 0f;
            for (int w = 0; w < WaveDirector.MaxWaves; w++)
            {
                var spec = director.NextWave(w, 0);
                int groups = (spec.enemyCount + spec.groupSize - 1) / spec.groupSize;
                float asGroups = (groups - 1) * spec.spawnInterval;
                float asSingles = (spec.enemyCount - 1) * spec.spawnInterval;

                Assert.Greater(asGroups, 0f, "wave " + w + " must land over more than one group");
                Assert.Greater(asSingles / asGroups, 3f,
                    $"wave {w} takes {asGroups:0.#}s in groups and {asSingles:0.#}s one at a time - "
                    + "if that ratio ever collapses, the per-group reading has stopped mattering");
                Assert.Greater(asSingles, 40f,
                    "…and one at a time it is longer than the whole fight, wave " + w);

                if (w > 0)
                    Assert.Greater(asGroups, previousSpan,
                        "delivery must lengthen with the wave, not stay flat while the roster grows");
                previousSpan = asGroups;
            }

            // The group arithmetic itself, on the wave the Thief debuts in, derived rather than
            // spelled: a wave is ceil(count / width) groups and the last one is the short one.
            var thiefWave = director.NextWave(2, 0);
            int thiefWaveGroups = (thiefWave.enemyCount + thiefWave.groupSize - 1)
                                  / thiefWave.groupSize;
            Assert.GreaterOrEqual(thiefWaveGroups * thiefWave.groupSize, thiefWave.enemyCount);
            Assert.Less((thiefWaveGroups - 1) * thiefWave.groupSize, thiefWave.enemyCount);
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
                Assert.AreEqual(0, spec.cloakerCount);
                Assert.AreEqual(1, spec.groupSize, "singles, as this curve was written");
                Assert.AreEqual(WaveDirector.SpoolWaveInterval, spec.timeUntilNextWave, 1e-6f);
            }
        }
    }
}
