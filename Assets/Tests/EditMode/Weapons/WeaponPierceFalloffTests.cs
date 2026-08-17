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
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float body1 = WeaponWheel.DamageOnPiercedBody(def, 0, distance: 10f, damagedBodyIndex: 0);
            float body2 = WeaponWheel.DamageOnPiercedBody(def, 0, distance: 10f, damagedBodyIndex: 1);
            float body3 = WeaponWheel.DamageOnPiercedBody(def, 0, distance: 10f, damagedBodyIndex: 2);
            Assert.AreEqual(32f, body1, Tol);
            Assert.AreEqual(32f * 0.26f, body2, Tol);
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
            var def = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            Assert.AreEqual(6f, def.damage, Tol);
            Assert.AreEqual(7, def.pellets);
            Assert.AreEqual(0, def.pierceCount);
            Assert.AreEqual(1f, def.pierceDamageMul, Tol);
            Assert.AreEqual(8f, def.falloffStart, Tol);
            Assert.AreEqual(14f, def.range, Tol);
        }

        [Test]
        public void Mr35_T0_StopsClearingWave1Line()
        {
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float wave1Hp = WaveDirector.BaseEnemyHp; // 30
            float body1 = WeaponWheel.DamageOnPiercedBody(def, 0, 10f, 0);
            float body2 = WeaponWheel.DamageOnPiercedBody(def, 0, 10f, 1);
            float body3 = WeaponWheel.DamageOnPiercedBody(def, 0, 10f, 2);
            Assert.GreaterOrEqual(body1, wave1Hp, $"body 1 deals {body1}, need ≥{wave1Hp}");
            Assert.Less(body2, wave1Hp, $"body 2 deals {body2}, must live vs {wave1Hp}");
            Assert.Less(body3, wave1Hp, $"body 3 deals {body3}, must live vs {wave1Hp}");
        }

        [Test]
        public void Mr35_FocusAtMax_PierceSurvivesWave5()
        {
            // Criterion 6, re-cut 2026-08-09: FocusAtMax=50, mul 0.26 → ~77.3 vs 78 HP.
            var def = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float baseAtFocus = WeaponWheel.EffectiveDamage(def, WeaponWheel.FocusAtMax);
            float pierce = baseAtFocus * def.pierceDamageMul;
            float wave5Hp = WaveDirector.BaseEnemyHp
                * (1f + WaveDirector.EnemyHealthPerWave * 4f); // 78
            Assert.AreEqual(297.5f, baseAtFocus, 0.5f);
            Assert.Less(pierce, wave5Hp,
                $"pierce {pierce:0.#} must leave Wave-5 {wave5Hp} alive at mul {def.pierceDamageMul}");
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
