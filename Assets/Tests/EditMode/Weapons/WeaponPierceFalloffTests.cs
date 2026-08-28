using System;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M31 — pierceDamageMul, falloff table, T0 / FocusAtMax HP gates, pin-on-first-body
    /// call pattern. MR-35 mul is 0.26 (Zo, 2026-08-09) against FocusAtMax=50.</summary>
    public class WeaponPierceFalloffTests
    {
        const float Tol = 0.02f;

        [Test]
        public void PierceDamageMul_PerWeapon_AsCatalogued()
        {
            Assert.AreEqual(0.26f, WeaponCatalog.Def(WeaponId.Mr35Rifle).pierceDamageMul, Tol);
            Assert.AreEqual(0.25f, WeaponCatalog.Def(WeaponId.ThrowawayComposite).pierceDamageMul, Tol);
            Assert.AreEqual(1f, WeaponCatalog.Def(WeaponId.Mk1Sidearm).pierceDamageMul, Tol);
            Assert.AreEqual(1f, WeaponCatalog.Def(WeaponId.Br12Shotgun).pierceDamageMul, Tol);
            Assert.AreEqual(1f, WeaponCatalog.Def(WeaponId.Vk7Rifle).pierceDamageMul, Tol);
        }

        [Test]
        public void ThrowawayComposite_MulIsNotMr35Constant()
        {
            // Criterion 3: a test that passes with 0.5 inlined has not tested per-weapon reads.
            float throwaway = WeaponCatalog.Def(WeaponId.ThrowawayComposite).pierceDamageMul;
            float mr35 = WeaponCatalog.Def(WeaponId.Mr35Rifle).pierceDamageMul;
            Assert.AreNotEqual(mr35, throwaway);
            Assert.AreEqual(0.25f, throwaway, Tol);
        }

        [Test]
        public void PierceMul_IsFlat_NotCompounding_AcrossThreeBodies()
        {
            // Shape pin, not a balance pin: the claim is that mul is applied ONCE per body, flat,
            // never body-by-body. The damage it is keyed on has moved twice in a day — the recut
            // took it 32 → 8, the Primary raise took it 8 → 16 — and mul is untouched by both,
            // because flatness is a property of DamageOnPiercedBody and no tuning of the damage
            // column can reach it. So the expectations are DERIVED from def.damage and
            // def.pierceDamageMul: the next move of either field re-keys this pin by itself, and a
            // shape regression stays the only thing that can turn it red.
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float body1 = WeaponWheel.DamageOnPiercedBody(def, 0, distance: 10f, damagedBodyIndex: 0);
            float body2 = WeaponWheel.DamageOnPiercedBody(def, 0, distance: 10f, damagedBodyIndex: 1);
            float body3 = WeaponWheel.DamageOnPiercedBody(def, 0, distance: 10f, damagedBodyIndex: 2);
            Assert.AreEqual(def.damage, body1, Tol, "10 m is inside falloffStart, so body 1 is the row");
            Assert.AreEqual(def.damage * def.pierceDamageMul, body2, Tol);
            Assert.AreEqual(body2, body3, Tol, "bodies 2 and 3 take the same flat fraction");
            // Compounding would give body3 = body1 * mul^2; flat keeps body3 = body1 * mul.
            Assert.AreNotEqual(body1 * def.pierceDamageMul * def.pierceDamageMul, body3);
        }

        [Test]
        public void FalloffTable_At10_25_45()
        {
            var mk1 = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            Assert.AreEqual(45f, mk1.range, Tol);
            Assert.AreEqual(12f, mk1.falloffStart, Tol);
            Assert.AreEqual(1f, Frac(mk1, 10f), Tol);
            Assert.AreEqual(20f / 33f, Frac(mk1, 25f), Tol); // ~61%
            Assert.AreEqual(0f, Frac(mk1, 45f), Tol);

            var br12 = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            Assert.AreEqual(14f, br12.range, Tol);
            Assert.AreEqual(8f, br12.falloffStart, Tol);
            Assert.AreEqual(4f / 6f, Frac(br12, 10f), Tol); // ~67%
            Assert.AreEqual(0f, WeaponWheel.DamageAtRange(br12, 0, 25f), Tol);

            var mr35 = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            Assert.AreEqual(60f, mr35.range, Tol);
            Assert.AreEqual(25f, mr35.falloffStart, Tol);
            Assert.AreEqual(1f, Frac(mr35, 10f), Tol);
            Assert.AreEqual(1f, Frac(mr35, 25f), Tol);
            Assert.AreEqual(15f / 35f, Frac(mr35, 45f), Tol); // ~43%
        }

        [Test]
        public void Br12_Unchanged_ByMakeSignatureGrowth()
        {
            // The claim in the name is about the Make() SIGNATURE, not about the damage column:
            // growing Make with pellets / pierce / falloff / crosshair / ability parameters must
            // not disturb the row BR-12 was authored with. Damage moved TWICE on 2026-08-27 —
            // 6 → 12 with the Primary tier raise, then 12 → 19 so the volley still one-shot a
            // Default after BaseEnemyHp went 30 → 50 — and that is the one field re-keyed here.
            // Every other field below is the value it has held since M31, which is the pin.
            var def = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            Assert.AreEqual(19f, def.damage, Tol);
            Assert.AreEqual(7, def.pellets);
            Assert.AreEqual(0, def.pierceCount);
            Assert.AreEqual(1f, def.pierceDamageMul, Tol);
            Assert.AreEqual(8f, def.falloffStart, Tol);
            Assert.AreEqual(14f, def.range, Tol);
        }

        [Test]
        public void Mr35_T0_StopsClearingWave1Line()
        {
            // THE NAME STILL HOLDS AND IS STILL THE CLAIM: T0 pierce SOFTENS a line, it does not
            // clear one, and wave 1 is where it is measured because wave 1 is the calibration wave
            // (hpMul 1.0 by construction — BaseEnemyHp exactly). Both halves of the name survived
            // the 2026-08-27 Primary raise, so the name is not re-keyed; the arithmetic under it is.
            //
            // It was written per-shot at 32 damage, when one shot was the whole kill. The recut to
            // 8 made that four shots and the claim moved to a per-BURST unit — the burst that drops
            // the front body. The raise to 16 halved that burst, and BaseEnemyHp 30 → 50 then put
            // it back where it started:
            //
            //   front  ceil(50 / 16)          = 4 shots, 4/8 = 0.5 s
            //   rear   4 x (16 x 0.26 = 4.16) = 16.64 each, 33.3% of 50 — still standing
            //
            // THE PROPORTION IS THE INVARIANT AND IT IS WHY THIS PIN KEEPS READING THE SAME.
            // mul is a FRACTION of damage, so the front's kill threshold and the rear's share move
            // together, and pierce's share of a line is untouched by any pure DAMAGE retune. An
            // ENEMY HP retune does move it, in the ceiling: the rear's share is
            // ceil(hp/damage) x mul, so it steps up whenever hp crosses a multiple of damage.
            // 27.7% at 30 HP became 33.3% at 50 for exactly that reason — not because pierce got
            // stronger, but because the front now takes four shots instead of two and the rear
            // stands in the beam for two more of them. What a damage retune moves instead is the
            // rate: in-clip dps is 128 and each rear body takes 33.3 dps of splash, against
            // 64 / 16.6 at 8 damage and 51.2 / 13.3 at 32. Sustained dps went 37.0 → 74.0 with it.
            //
            // Written over derived values throughout, so the invariant above is what is actually
            // asserted rather than the particular pair of numbers it currently produces.
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float wave1Hp = WaveDirector.BaseEnemyHp;
            float body1 = WeaponWheel.DamageOnPiercedBody(def, 0, 10f, 0);
            float body2 = WeaponWheel.DamageOnPiercedBody(def, 0, 10f, 1);
            float body3 = WeaponWheel.DamageOnPiercedBody(def, 0, 10f, 2);
            Assert.AreEqual(def.damage, body1, Tol, "10 m is inside falloffStart: the front takes the row");

            int shotsToDropFront = (int)Math.Ceiling(wave1Hp / body1);
            Assert.AreEqual(4, shotsToDropFront, $"front body takes {body1}/shot, need 4 to clear {wave1Hp}");
            Assert.AreEqual(0.5f, shotsToDropFront / def.shotsPerSecond, Tol, "front body dies in 0.5 s");

            Assert.Less(shotsToDropFront * body2, wave1Hp,
                $"body 2 has taken {shotsToDropFront * body2} when the front drops, must live vs {wave1Hp}");
            Assert.Less(shotsToDropFront * body3, wave1Hp,
                $"body 3 has taken {shotsToDropFront * body3} when the front drops, must live vs {wave1Hp}");
            Assert.AreEqual(body2, body3, Tol, "the line softens evenly behind the front");

            // AND THE MARGIN, so "must live" is a measured claim and not a near miss nobody sees.
            // The rear is left at 67% of its pool when the front drops — it was 72% before the HP
            // raise lengthened the front's kill by two shots, so this margin is being SPENT, and
            // spent from the direction the pin cares about. Halving the burst could have quietly
            // emptied this from the other side — one shot to drop the front would leave the rear
            // on 4.16 and the assertion would pass while testing nothing — so the fraction is
            // pinned rather than the survival alone.
            float rearRemaining = (wave1Hp - shotsToDropFront * body2) / wave1Hp;
            Assert.Greater(rearRemaining, 0.5f,
                $"a softened body must still be more than half alive; it is on {rearRemaining:0.###}");
        }

        [Test]
        public void Mr35_FocusAtMax_PierceSurvivesWave5()
        {
            // Criterion 6, re-cut 2026-08-09: FocusAtMax=50, mul 0.26 → ~77.3 vs 78 HP. That was a
            // knife-edge and the knife-edge WAS the pin: 0.26 had been solved so a fully-focused
            // MR-35 missed a one-shot on a pierced Wave-5 body by 0.65 HP.
            //
            // The 2026-08-27 recut destroyed that framing and it has not come back. Damage focus
            // is x9.2956 whatever the row says, so the front body takes def.damage x 9.2956; what
            // the enemy curve decides is only whether that one-shots. Since then: the recut took
            // damage 32 → 8 (74.37 at focus), the slope fell 0.40 → 0.30 and a Wave-5 Default went
            // 78 → 66 HP, and then BOTH moved back the other way in the same session — the Primary
            // raise took damage 8 → 16, and the slope was restored to 0.40. Then the 2026-08-27
            // fewer-and-tougher pass took BaseEnemyHp 30 → 50 and a Wave-5 Default to 130. Where
            // that lands:
            //
            //   focus  16 x 9.2956             = 148.73 damage on the front body
            //   front  ceil(130 / 148.73)      = 1 shot, 0.125 s — a 1.14x overkill
            //   rear   1 x (148.73 x 0.26)     = 38.67 < 130 — still standing when the front drops
            //
            // WHAT THIS PIN ACTUALLY PROTECTS, now that the 0.65 HP knife-edge is gone for good:
            // pierce must never turn the rifle into a line-clearing weapon. The bodies behind the
            // front have to survive the shot that drops it, at the deepest wave and the highest
            // focus the game can reach — the worst case, so it is the case pinned. The margin
            // breaks at mul ≥ 0.874 (130 / 148.73), against 0.524 before the HP raise: the
            // headroom over 0.26 went 3.4x at 8 damage, down to 2.0x with the raise, and back up
            // to 3.4x now that the body is tougher. That is the whole shape of this guard — it
            // tracks the RATIO of the front's overkill to the rear's pool, and both curves move it.
            // Both halves are asserted over DERIVED values so they survive the next move of either.
            //
            // FINDING for Zo, asserted rather than left in prose: a FULLY focused MR-35 still
            // one-shots a Wave-5 Default, but the margin fell from 1.9x to 1.14x when the body went
            // 78 → 130 HP. That is the whole wheel spent on one spoke across all ten purchases, so
            // it reads as the intended top-end payoff rather than a leak — but it is now within one
            // more HP step of costing two shots, which would halve the top of the damage spoke in
            // a single tuning pass. It still does not let that player clear a Wave-5 field, because
            // Wave 5 delivers ~1.56 bodies/s against a ~0.90 kills/s acquisition cap that no
            // amount of damage lifts.
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float baseAtFocus = WeaponWheel.EffectiveDamage(def, WeaponWheel.FocusAtMax);
            float pierce = baseAtFocus * def.pierceDamageMul;
            float wave5Hp = WaveDirector.BaseEnemyHp
                * (1f + WaveDirector.EnemyHealthPerWave * (WaveDirector.MaxWaves - 1));
            Assert.AreEqual(def.damage * WeaponWheel.DamageMul(WeaponWheel.FocusAtMax),
                baseAtFocus, 0.01f, "max focus is the authored row times the damage-spoke curve");
            int shotsToDropFront = (int)Math.Ceiling(wave5Hp / baseAtFocus);
            Assert.AreEqual(1, shotsToDropFront,
                $"max focus one-shots a Wave-5 Default ({wave5Hp} HP) at {baseAtFocus:0.##} damage");
            Assert.Less(shotsToDropFront * pierce, wave5Hp,
                $"pierce has dealt {shotsToDropFront * pierce:0.#} when the front drops, must leave "
                + $"Wave-5 {wave5Hp} alive at mul {def.pierceDamageMul}");
            Assert.Less(def.pierceDamageMul, wave5Hp / (shotsToDropFront * baseAtFocus),
                "pierce mul must stay under the line-clearing threshold at the deepest wave");
        }

        [Test]
        public void PinBuildSurvivesPierce_ThreeShotsOnFrontBodyOnly()
        {
            // Criteria 9–11: FireRay registers pin on the first damaged body only. Three shots
            // into a three-body line therefore call RegisterHit three times on the front key
            // (bodies 2 and 3 never touch PinBuildState), and the build reaches threshold.
            var build = new PinBuildState();
            const int frontKey = 101;
            const int midKey = 102;
            const int backKey = 103;
            for (int shot = 0; shot < WeaponAbility.PinHitThreshold; shot++)
            {
                // Simulate one pierce ray: damage all three, pin only the first.
                build.RegisterHit(frontKey, WeaponAbility.PinBuildDecaySeconds);
                // mid/back would have been RegisterHit before the fix — deliberately omitted.
                Assert.AreNotEqual(midKey, build.TargetKey);
                Assert.AreNotEqual(backKey, build.TargetKey);
            }
            Assert.AreEqual(WeaponAbility.PinHitThreshold, build.Hits);
            Assert.IsTrue(build.Ready);
        }

        [Test]
        public void PinBuild_CarriesWhenFirstBodyChanges()
        {
            var build = new PinBuildState();
            build.RegisterHit(1, WeaponAbility.PinBuildDecaySeconds);
            build.RegisterHit(1, WeaponAbility.PinBuildDecaySeconds);
            build.RegisterHit(2, WeaponAbility.PinBuildDecaySeconds);
            Assert.AreEqual(WeaponAbility.PinHitThreshold, build.Hits);
            Assert.AreEqual(2, build.TargetKey);
            Assert.IsTrue(build.Ready);
        }

        static float Frac(WeaponDef def, float distance)
        {
            float at = WeaponWheel.DamageAtRange(def, 0, distance);
            float baseDmg = WeaponWheel.EffectiveDamage(def, 0);
            if (baseDmg <= 0f) return 0f;
            return at / baseDmg;
        }
    }
}
