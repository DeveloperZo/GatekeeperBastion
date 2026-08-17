using System;
using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M20 Path 1 — catalog, dial curves, and pin-build rules that do not need a
    /// scene. Behaviour that needs MonoBehaviours is Path 2 (PlayMode).</summary>
    public class WeaponAbilityTests
    {
        [Test]
        public void EveryShopWeaponHasAnAbility()
        {
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var def = WeaponCatalog.Def(id);
                // M26 Open: SEC-D ability is Zo's. Catalog may ship unset; nothing else may.
                if (WeaponAbility.IsEmpty(def.abilityId))
                {
                    Assert.AreEqual(WeaponId.SecD, id,
                        $"{id} must have an ability (only SecD may ship unset per M26 Open)");
                    continue;
                }
                Assert.AreNotEqual(AbilityId.None, def.abilityId, $"{id} must have an ability");
                Assert.IsFalse(string.IsNullOrEmpty(def.abilityName), $"{id} abilityName");
                Assert.Greater(WeaponAbility.Magnitude(def.abilityId, 0), 0f,
                    $"{id} ability magnitude");
                Assert.Greater(def.abilityCooldown, 0f, $"{id} abilityCooldown");
            }

            AssertEmpty(WeaponCatalog.Def(WeaponId.None));
            AssertEmpty(WeaponCatalog.Def(WeaponId.ThrowawayComposite));
            AssertEmpty(WeaponCatalog.Def(WeaponId.Vk7Rifle));
            AssertEmpty(WeaponCatalog.Def(WeaponId.SecD));
        }

        [Test]
        public void AbilityIdsAreUnique()
        {
            var seen = new HashSet<AbilityId>();
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var aid = WeaponCatalog.Def(id).abilityId;
                if (WeaponAbility.IsEmpty(aid)) continue;
                Assert.IsTrue(seen.Add(aid), $"duplicate AbilityId {aid} on {id}");
            }
        }

        [Test]
        public void DialIsMonotonicAndUncapped()
        {
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var aid = WeaponCatalog.Def(id).abilityId;
                if (WeaponAbility.IsEmpty(aid)) continue;
                float firstStep = 0f;
                float prev = WeaponAbility.Magnitude(aid, 0);
                Assert.Greater(prev, 0f, $"{aid} at 0");

                for (int p = 1; p <= WeaponAbility.MaxDialPoints; p++)
                {
                    float m = WeaponAbility.Magnitude(aid, p);
                    Assert.Greater(m, prev,
                        $"{aid}: effect({p}) must be > effect({p - 1}) (no plateau)");
                    float step = m - prev;
                    if (p == 1) firstStep = step;
                    prev = m;
                }

                float lastStep = WeaponAbility.Magnitude(aid, WeaponAbility.MaxDialPoints)
                    - WeaponAbility.Magnitude(aid, WeaponAbility.MaxDialPoints - 1);
                Assert.GreaterOrEqual(lastStep, firstStep * 0.4f,
                    $"{aid}: last step ({lastStep}) must be >= 40% of first ({firstStep})");
            }
        }

        [Test]
        public void PinBuildCountsHitsNotDamage()
        {
            var build = new PinBuildState();
            // One huge "damage" hit still counts as one hit.
            build.RegisterHit(targetKey: 7, WeaponAbility.PinBuildDecaySeconds);
            Assert.AreEqual(1, build.Hits);
            Assert.IsFalse(build.Ready);

            build.RegisterHit(7, WeaponAbility.PinBuildDecaySeconds);
            build.RegisterHit(7, WeaponAbility.PinBuildDecaySeconds);
            Assert.AreEqual(WeaponAbility.PinHitThreshold, build.Hits);
            Assert.IsTrue(build.Ready);

            // Rebuilding from scratch: N damage in one RegisterHit is still one hit.
            build.Clear();
            build.RegisterHit(7, WeaponAbility.PinBuildDecaySeconds);
            Assert.IsFalse(build.Ready, "one hit of any damage must not pin");
        }

        [Test]
        public void PinBuildCarriesAcrossTargets_DecaysOnIdle()
        {
            var build = new PinBuildState();
            build.RegisterHit(1, WeaponAbility.PinBuildDecaySeconds);
            build.RegisterHit(1, WeaponAbility.PinBuildDecaySeconds);
            Assert.AreEqual(2, build.Hits);

            // T10 MR-35 one-shots Defaults; charge must survive the target change so the
            // tipping shot on a Ram can Ready and pin.
            build.RegisterHit(2, WeaponAbility.PinBuildDecaySeconds);
            Assert.AreEqual(WeaponAbility.PinHitThreshold, build.Hits);
            Assert.AreEqual(2, build.TargetKey);
            Assert.IsTrue(build.Ready);

            build.Tick(WeaponAbility.PinBuildDecaySeconds + 0.01f);
            Assert.AreEqual(0, build.Hits, "build must clear after the decay window");
        }

        [Test]
        public void Mk1DialIsDuration_NotMultiplier()
        {
            float at0 = WeaponAbility.Magnitude(AbilityId.Mk1RegenSurge, 0);
            float atMax = WeaponAbility.Magnitude(AbilityId.Mk1RegenSurge, WeaponAbility.MaxDialPoints);
            Assert.Greater(atMax, at0);
            // Duration in seconds, not a 2–6× rate: at 0 must be a few seconds of surge.
            Assert.GreaterOrEqual(at0, 3f);
            Assert.LessOrEqual(at0, 8f);
            Assert.AreEqual(2.5f, WeaponAbility.Mk1SurgeMultiplier, 0.01f);
        }

        [Test]
        public void AssignedAbilities_MatchDecidedRoster()
        {
            Assert.AreEqual(AbilityId.Br12Concussive,
                WeaponCatalog.Def(WeaponId.Br12Shotgun).abilityId);
            Assert.AreEqual(AbilityId.Mr35Pin,
                WeaponCatalog.Def(WeaponId.Mr35Rifle).abilityId);
            Assert.AreEqual(AbilityId.Mk1RegenSurge,
                WeaponCatalog.Def(WeaponId.Mk1Sidearm).abilityId);
            Assert.AreEqual(AbilityId.PriCRailshot,
                WeaponCatalog.Def(WeaponId.PriC).abilityId);
        }

        [Test]
        public void CatalogAbility_MagnitudeAndCooldownAreSet()
        {
            foreach (WeaponId id in WeaponCatalog.ShopWeapons())
            {
                var def = WeaponCatalog.Def(id);
                if (WeaponAbility.IsEmpty(def.abilityId)) continue;
                Assert.Greater(WeaponAbility.Magnitude(def.abilityId, 0), 0f);
                Assert.AreEqual(WeaponAbility.Cooldown(def.abilityId), def.abilityCooldown, 0.001f);
            }
        }

        static void AssertEmpty(WeaponDef def)
        {
            Assert.AreEqual(AbilityId.None, def.abilityId);
            Assert.IsTrue(string.IsNullOrEmpty(def.abilityName));
            Assert.AreEqual(0f, WeaponAbility.Magnitude(def.abilityId, 0), 0.001f);
            Assert.AreEqual(0f, def.abilityCooldown, 0.001f);
        }
    }
}
