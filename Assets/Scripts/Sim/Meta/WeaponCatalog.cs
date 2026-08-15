using System;
using System.Collections.Generic;

namespace GatekeeperBastion.Sim
{
    /// <summary>Static pure catalog of every weapon. Shape matches
    /// <see cref="WaveDirector.StatsFor"/> — holds no state, so it does not violate the
    /// no-new-statics rule. Adding a fifth listed weapon is a row here and nowhere else.</summary>
    public static class WeaponCatalog
    {
        /// <summary>One slot beyond the highest numeric <see cref="WeaponId"/> value.</summary>
        public static readonly int ArraySize = ComputeArraySize();

        static int ComputeArraySize()
        {
            int max = -1;
            foreach (WeaponId id in Enum.GetValues(typeof(WeaponId)))
            {
                int value = (int)id;
                if (value > max) max = value;
            }
            return max + 1;
        }

        /// <summary>Amber shared by MK-1 / VK-7 / BR-12 tracers.</summary>
        const float AmberR = 1f, AmberG = 0.9f, AmberB = 0.35f;

        /// <summary>Pale blue for the MR-35 — the long-range identity cue.</summary>
        const float BlueR = 0.55f, BlueG = 0.78f, BlueB = 1f;

        /// <summary>Hot coral for SEC-D — close-quarters identity cue.</summary>
        const float CoralR = 1f, CoralG = 0.38f, CoralB = 0.22f;

        /// <summary>Cool violet for PRI-C — far-wall identity cue.</summary>
        const float VioletR = 0.72f, VioletG = 0.45f, VioletB = 1f;

        /// <summary>The combat profile of one weapon. Unknown ids return an empty Secondary stub.</summary>
        public static WeaponDef Def(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Mk1Sidearm:
                    return Make(WeaponSlot.Secondary, "MK-1 Sidearm", cost: 0L,
                        damage: 14f, shotsPerSecond: 3.0f, clipSize: 8f, reloadSeconds: 1.4f, range: 45f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 0, pierceDamageMul: 1f, falloffStart: 12f,
                        AmberR, AmberG, AmberB, tracerWidth: 0.030f, WeaponCrosshair.Cross,
                        AbilityId.Mk1RegenSurge);

                case WeaponId.SecD:
                    // Close-quarters Secondary. Ring is authored; ability stays None (Zo)
                    // and WeaponWheel.SpokePurchasable refuses a centred Ability purchase.
                    // Cost 0 so either Secondary is reachable as a campaign's first buy.
                    // Burst empties in clip/rate = 4/6 ≈ 0.67s (MK-1 ≈ 2.67s, ≥2×). Range 8 m /
                    // falloff from 3 m → near-zero at the MK-1's 12 m working distance.
                    // Duty-cycle dps ≈ 25.1 — below both Primaries' T0 floor (~33).
                    return Make(WeaponSlot.Secondary, "SEC-D", cost: 0L,
                        damage: 18f, shotsPerSecond: 6.0f, clipSize: 4f, reloadSeconds: 2.2f, range: 8f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 0, pierceDamageMul: 1f, falloffStart: 3f,
                        CoralR, CoralG, CoralB, tracerWidth: 0.045f, WeaponCrosshair.Dot,
                        AbilityId.None);

                case WeaponId.Vk7Rifle:
                    // Disabled — kept so old saves still deserialize. Not in ShopWeapons.
                    return Make(WeaponSlot.Primary, "VK-7 Assault Rifle", cost: 900L,
                        damage: 10f, shotsPerSecond: 5.5f, clipSize: 25f, reloadSeconds: 2.1f, range: 60f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 0, pierceDamageMul: 1f, falloffStart: 0f,
                        AmberR, AmberG, AmberB, tracerWidth: 0.035f, WeaponCrosshair.Dot,
                        AbilityId.None);

                case WeaponId.Br12Shotgun:
                    return Make(WeaponSlot.Primary, "BR-12 Shotgun", cost: 900L,
                        damage: 6f, shotsPerSecond: 1.2f, clipSize: 6f, reloadSeconds: 2.6f, range: 14f,
                        pellets: 7, spreadDegrees: 7f, pierceCount: 0, pierceDamageMul: 1f, falloffStart: 8f,
                        AmberR, AmberG, AmberB, tracerWidth: 0.050f, WeaponCrosshair.Brackets,
                        AbilityId.Br12Concussive);

                case WeaponId.Mr35Rifle:
                    return Make(WeaponSlot.Primary, "MR-35 Rifle", cost: 1200L,
                        damage: 32f, shotsPerSecond: 1.6f, clipSize: 10f, reloadSeconds: 2.4f, range: 60f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 2, pierceDamageMul: 0.26f, falloffStart: 25f,
                        BlueR, BlueG, BlueB, tracerWidth: 0.060f, WeaponCrosshair.Cross,
                        AbilityId.Mr35Pin);

                case WeaponId.PriC:
                    // Far-wall Primary. falloffStart 55 exceeds indoor sightlines so no
                    // Bastion shot falls off. Sustained dps ≈ 36.5 — within 5% of MR-35's 37.0.
                    // pierce 3 ×0.75 is the bore payoff at 0.6 shots/s; Railshot overrides both.
                    return Make(WeaponSlot.Primary, "PRI-C", cost: 1500L,
                        damage: 90f, shotsPerSecond: 0.6f, clipSize: 4f, reloadSeconds: 3.2f, range: 90f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 3, pierceDamageMul: 0.75f, falloffStart: 55f,
                        VioletR, VioletG, VioletB, tracerWidth: 0.070f, WeaponCrosshair.Dot,
                        AbilityId.PriCRailshot);

                case WeaponId.ThrowawayComposite:
                    // Criterion 11: pellets + pierce + falloff together. Not listed in the shop.
                    // Explicit empty ability — never invent one for a test row.
                    // pierceDamageMul 0.25 proves FireRay reads the field per weapon.
                    return Make(WeaponSlot.Primary, "Throwaway Composite", cost: 1L,
                        damage: 5f, shotsPerSecond: 2f, clipSize: 10f, reloadSeconds: 1.5f, range: 20f,
                        pellets: 3, spreadDegrees: 5f, pierceCount: 1, pierceDamageMul: 0.25f, falloffStart: 10f,
                        AmberR, AmberG, AmberB, tracerWidth: 0.040f, WeaponCrosshair.Dot,
                        AbilityId.None);

                default:
                    return Make(WeaponSlot.Secondary, "None", cost: 0L,
                        damage: 0f, shotsPerSecond: 1f, clipSize: 1f, reloadSeconds: 1f, range: 1f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 0, pierceDamageMul: 1f, falloffStart: 0f,
                        AmberR, AmberG, AmberB, tracerWidth: 0.030f, WeaponCrosshair.Cross,
                        AbilityId.None);
            }
        }

        /// <summary>Every catalog id that occupies an ownership array slot, including the
        /// throwaway and disabled rows. Prefer <see cref="ShopWeapons"/> for ARMORY lists.</summary>
        public static IEnumerable<WeaponId> All()
        {
            yield return WeaponId.Mk1Sidearm;
            yield return WeaponId.Vk7Rifle;
            yield return WeaponId.Br12Shotgun;
            yield return WeaponId.Mr35Rifle;
            yield return WeaponId.ThrowawayComposite;
            yield return WeaponId.SecD;
            yield return WeaponId.PriC;
        }

        /// <summary>True when the weapon can be bought or equipped at the ARMORY.
        /// VK-7 is disabled (id kept for saves — never renumber). Throwaway is test-only.</summary>
        public static bool IsAvailable(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Mk1Sidearm:
                case WeaponId.SecD:
                case WeaponId.Br12Shotgun:
                case WeaponId.Mr35Rifle:
                case WeaponId.PriC:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Weapons a player can buy or equip at the ARMORY.</summary>
        public static IEnumerable<WeaponId> ShopWeapons()
        {
            yield return WeaponId.Mk1Sidearm;
            yield return WeaponId.SecD;
            yield return WeaponId.Br12Shotgun;
            yield return WeaponId.Mr35Rifle;
            yield return WeaponId.PriC;
        }

        /// <summary>Shop weapons whose <see cref="WeaponDef.slot"/> matches.</summary>
        public static IEnumerable<WeaponId> ForSlot(WeaponSlot slot)
        {
            foreach (var id in ShopWeapons())
            {
                if (Def(id).slot == slot)
                    yield return id;
            }
        }

        static WeaponDef Make(
            WeaponSlot slot, string displayName, long cost,
            float damage, float shotsPerSecond, float clipSize, float reloadSeconds, float range,
            int pellets, float spreadDegrees, int pierceCount, float pierceDamageMul, float falloffStart,
            float tracerR, float tracerG, float tracerB, float tracerWidth, WeaponCrosshair crosshair,
            AbilityId abilityId)
        {
            bool has = !WeaponAbility.IsEmpty(abilityId);
            return new WeaponDef
            {
                slot = slot,
                displayName = displayName,
                cost = cost,
                damage = damage,
                shotsPerSecond = shotsPerSecond,
                clipSize = clipSize,
                reloadSeconds = reloadSeconds,
                range = range,
                pellets = pellets,
                spreadDegrees = spreadDegrees,
                pierceCount = pierceCount,
                pierceDamageMul = pierceDamageMul,
                falloffStart = falloffStart,
                tracerR = tracerR,
                tracerG = tracerG,
                tracerB = tracerB,
                tracerWidth = tracerWidth,
                crosshair = crosshair,
                abilityId = abilityId,
                abilityName = has ? WeaponAbility.DisplayName(abilityId) : "",
                abilityCooldown = has ? WeaponAbility.Cooldown(abilityId) : 0f
            };
        }
    }
}
