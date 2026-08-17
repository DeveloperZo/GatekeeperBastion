using System;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M32 — PRI-C catalog presence, full-room damage, sustained band vs MR-35,
    /// Wave-5 fire-time floor, Railshot dial, and ring row.</summary>
    public class PriCCatalogTests
    {
        const float Tol = 0.05f;

        [Test]
        public void PriCIsInEveryCatalogList()
        {
            Assert.Greater(WeaponCatalog.ArraySize, (int)WeaponId.PriC);

            var def = WeaponCatalog.Def(WeaponId.PriC);
            Assert.AreEqual(WeaponSlot.Primary, def.slot);
            Assert.AreEqual("PRI-C", def.displayName);
            Assert.AreEqual(1500L, def.cost);

            bool inAll = false, inShop = false, inAvailable = false, inPrimary = false;
            foreach (WeaponId id in WeaponCatalog.All())
                if (id == WeaponId.PriC) inAll = true;
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
                if (id == WeaponId.PriC) inShop = true;
            inAvailable = WeaponCatalog.IsAvailable(WeaponId.PriC);
            foreach (WeaponId id in WeaponCatalog.ForSlot(WeaponSlot.Primary))
                if (id == WeaponId.PriC) inPrimary = true;

            Assert.IsTrue(inAll, "All()");
            Assert.IsTrue(inShop, "ShopWeapons()");
            Assert.IsTrue(inAvailable, "IsAvailable()");
            Assert.IsTrue(inPrimary, "ForSlot(Primary)");
        }

        [Test]
        public void Profile_MatchesAuthoredTable()
        {
            var def = WeaponCatalog.Def(WeaponId.PriC);
            Assert.AreEqual(90f, def.damage, Tol);
            Assert.AreEqual(0.6f, def.shotsPerSecond, Tol);
            Assert.AreEqual(4f, def.clipSize, Tol);
            Assert.AreEqual(3.2f, def.reloadSeconds, Tol);
            Assert.AreEqual(90f, def.range, Tol);
            Assert.AreEqual(55f, def.falloffStart, Tol);
            Assert.AreEqual(1, def.pellets);
            Assert.AreEqual(3, def.pierceCount);
            Assert.AreEqual(0.75f, def.pierceDamageMul, Tol);
            Assert.AreEqual(WeaponCrosshair.Dot, def.crosshair);
            Assert.AreEqual(AbilityId.PriCRailshot, def.abilityId);
        }

        [Test]
        public void FullDamage_AcrossBastionSightline()
        {
            // Criterion 3: falloffStart 55 exceeds ~50 m indoor sightlines.
            var def = WeaponCatalog.Def(WeaponId.PriC);
            Assert.AreEqual(90f, WeaponWheel.DamageAtRange(def, 0, 50f), Tol);
            Assert.AreEqual(90f, WeaponWheel.DamageAtRange(def, 0, 55f), Tol);
            Assert.Less(WeaponWheel.DamageAtRange(def, 0, 70f), 90f);
        }

        [Test]
        public void SustainedDps_WithinFivePercentOfMr35()
        {
            // Criterion 4: PRI-C 36.5 vs MR-35 37.0.
            float priC = WeaponWheel.EffectiveDps(WeaponCatalog.Def(WeaponId.PriC), new int[WeaponWheel.AttrCount]);
            float mr35 = WeaponWheel.EffectiveDps(WeaponCatalog.Def(WeaponId.Mr35Rifle), new int[WeaponWheel.AttrCount]);
            Assert.AreEqual(36.5f, priC, 0.1f);
            Assert.AreEqual(37.0f, mr35, 0.1f);
            float ratio = Math.Abs(priC - mr35) / mr35;
            Assert.LessOrEqual(ratio, 0.05f, $"sustained gap {ratio:0.###} exceeds 5%");
        }

        [Test]
        public void CannotCarryWave5Alone_PureFireTime()
        {
            // Criterion 5: 66 bodies / 0.6 shots/s > 100 s against ~40 s Wave-5 delivery.
            var wave5 = new WaveDirector().NextWave(waveIndex: 4, siphonLevel: 0);
            Assert.AreEqual(66, wave5.enemyCount);
            float fireSeconds = wave5.enemyCount / WeaponCatalog.Def(WeaponId.PriC).shotsPerSecond;
            Assert.Greater(fireSeconds, 100f, $"pure fire time {fireSeconds:0.#}s must exceed 100s");
        }

        [Test]
        public void FarWall_PriCKillsWhereMr35DoesNot()
        {
            // Criterion 7 arithmetic: at 45 m, PRI-C still deals full 90; MR-35 is ~43%.
            float wave5Hp = WaveDirector.BaseEnemyHp
                * (1f + WaveDirector.EnemyHealthPerWave * 4f); // 78
            var priC = WeaponCatalog.Def(WeaponId.PriC);
            var mr35 = WeaponCatalog.Def(WeaponId.Mr35Rifle);
            float priAt45 = WeaponWheel.DamageAtRange(priC, 0, 45f);
            float mrAt45 = WeaponWheel.DamageAtRange(mr35, 0, 45f);
            Assert.GreaterOrEqual(priAt45, wave5Hp);
            Assert.Less(mrAt45, wave5Hp);
        }

        [Test]
        public void RailshotDial_BodiesAtZeroAndMax()
        {
            Assert.AreEqual(3f, WeaponAbility.Magnitude(AbilityId.PriCRailshot, 0), Tol);
            Assert.AreEqual(8f,
                WeaponAbility.Magnitude(AbilityId.PriCRailshot, WeaponAbility.MaxDialPoints), Tol);
            Assert.AreEqual(12f, WeaponAbility.Cooldown(AbilityId.PriCRailshot), Tol);
            Assert.AreEqual(4f, WeaponAbility.PriCRailshotArmedSeconds, Tol);
        }

        [Test]
        public void Ring_MatchesAuthoredRow()
        {
            var ring = WeaponWheel.RingOf(WeaponId.PriC);
            Assert.IsNotNull(ring);
            Assert.AreEqual(5, ring.Length);
            Assert.AreEqual(WheelAttr.Clip, ring[0]);
            Assert.AreEqual(WheelAttr.Damage, ring[1]);
            Assert.AreEqual(WheelAttr.Reload, ring[2]);
            Assert.AreEqual(WheelAttr.Ability, ring[3]);
            Assert.AreEqual(WheelAttr.FireRate, ring[4]);
            Assert.IsTrue(WeaponWheel.TryIdentity(WeaponId.PriC, out var strength, out var weakness));
            Assert.AreEqual(WheelAttr.Damage, strength);
            Assert.AreEqual(WheelAttr.FireRate, weakness);
        }

        [Test]
        public void OneShot_NonRamAtEveryWave()
        {
            var def = WeaponCatalog.Def(WeaponId.PriC);
            for (int w = 0; w < 5; w++)
            {
                float hp = WaveDirector.BaseEnemyHp
                    * (1f + WaveDirector.EnemyHealthPerWave * w);
                Assert.GreaterOrEqual(def.damage, hp, $"Wave {w + 1} body {hp} must die to 90");
            }
        }
    }
}
