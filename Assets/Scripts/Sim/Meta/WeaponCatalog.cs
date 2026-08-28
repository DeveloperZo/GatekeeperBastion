using System;
using System.Collections.Generic;

namespace GatekeepersProtocol.Sim
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
                        damage: 30f, shotsPerSecond: 3.0f, clipSize: 8f, reloadSeconds: 1.4f, range: 45f,
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
                    // Damage 6 -> 12/pellet with the 2026-08-27 Primary raise, then 12 -> 19
                    // when BaseEnemyHp went 30 -> 50 the same day. The second step buys NO
                    // power: BR-12 one-shot every body in the table before and one-shots every
                    // body now (7 x 19 = 133 against a deepest Default of 130), so its kills/s
                    // is unchanged at 0.789 flat. Without it the raised ladder would have pushed
                    // a wave-3 body past the volley and HALVED this weapon to 0.395 — a cliff
                    // introduced by an enemy change, in a weapon nobody was retuning.
                    // Its dps column now reads ~105 and that number is a lie: it is overkill.
                    return Make(WeaponSlot.Primary, "BR-12 Shotgun", cost: 900L,
                        damage: 19f, shotsPerSecond: 1.2f, clipSize: 6f, reloadSeconds: 2.6f, range: 14f,
                        pellets: 7, spreadDegrees: 7f, pierceCount: 0, pierceDamageMul: 1f, falloffStart: 8f,
                        AmberR, AmberG, AmberB, tracerWidth: 0.050f, WeaponCrosshair.Brackets,
                        AbilityId.Br12Concussive);

                case WeaponId.Mr35Rifle:
                    // RECUT AS AN ASSAULT RIFLE (Zo, 2026-08-27): "its firing rate is way too
                    // low for the fantasy". It fired at 1.6 shots/s with a 10-round clip — a
                    // marksman cadence wearing a rifle's name. The 03:18 playthrough is the
                    // evidence: MR-35 at T2 held 0.87 kills/s, which is what the FREE MK-1 T0
                    // scored on 2026-08-26. A 1200-salvage Primary that kills no faster than
                    // the starting sidearm is not a purchase, it is a reskin.
                    //
                    // 8 shots/s, 27 rounds — the trigger is held, not tapped. The reload
                    // carries the odd 2.463 because it was solved for, not chosen: it was set
                    // when this line was first recut to hold sustained dps at the old 37.0.
                    //
                    // THAT 37.0 IS GONE, DELIBERATELY (Zo, 2026-08-27: "It is the rifle. We
                    // should push the primary damage up"). The whole Primary tier was raised
                    // ~2x in one pass — BR-12 6 -> 12/pellet, MR-35 8 -> 16, PRI-C 90 -> 180 —
                    // because the free MK-1 Sidearm sustains 59.0 dps and every Primary sat at
                    // 33-37. At EQUAL TIER the sidearm out-killed this rifle on every wave of
                    // the ladder, by 6% to 70%, which is the real content of the complaint that
                    // a 1200-salvage T2 rifle killed no faster than the starting gun. It did
                    // not, and the raw dps column had been saying so the whole time.
                    //
                    // MR-35 is now 74.0 sustained (BR-12 66.3, PRI-C 73.0 — the band holds its
                    // old shape, BR-12 10% low, exactly as when it was 33.2 against 37.0).
                    // Single-target kills/s at T0 now leads the sidearm on every wave. Judge a
                    // change here on kills/s against WaveDirector's HP ladder, NOT on the dps
                    // column: dps ignores overkill, and at 30 damage a body with 42 HP costs
                    // the sidearm two full shots. That granularity is why the gap measured 6%
                    // at wave 2 and 70% at wave 4 rather than a flat 60%.
                    //
                    // THE CLIP IS SIZED AGAINST THE TOUGHEST BODY ONE PULL SHOULD KILL, not
                    // rounded. At 26 rounds it once fell short of a mid-run Thief and the kill
                    // spanned a reload — a magazine one round shy of a meaningful target is the
                    // worst feeling a rifle can have. That margin is a function of
                    // WaveDirector.EnemyHealthPerWave, which is NOT this file's to know:
                    // re-derive it here whenever the HP curve moves rather than trusting the
                    // number that was true when this was written.
                    //
                    // RE-DERIVED AGAIN 2026-08-27, after the roster cut raised Thief HP to 200
                    // base: a magazine carries 27 x 16 = 432, and the deepest Thief is now 520.
                    // THE SIZING RULE HAS INVERTED — the boundary is wave 4 (440), so waves 4
                    // and 5 no longer die inside one pull. A wave-3 Thief still does, with four
                    // rounds spare. Left at 27: clip and reload were solved together for the
                    // duty cycle, so the magazine cannot be resized without re-solving the dps,
                    // and the Thief is the heavy — needing a reload for it is defensible. What
                    // is NOT defensible is a comment claiming otherwise, which is why this is
                    // recorded rather than quietly left. WeaponCatalogTests pins the boundary
                    // wave, so it goes red whichever direction that walks.
                    //
                    // Pierce stays 2 x 0.26: at 8 shots/s it reads as a line being cut through
                    // a column rather than one heavy round punching two bodies.
                    return Make(WeaponSlot.Primary, "MR-35 Rifle", cost: 1200L,
                        damage: 16f, shotsPerSecond: 8.0f, clipSize: 27f, reloadSeconds: 2.463f, range: 60f,
                        pellets: 1, spreadDegrees: 0f, pierceCount: 2, pierceDamageMul: 0.26f, falloffStart: 25f,
                        BlueR, BlueG, BlueB, tracerWidth: 0.060f, WeaponCrosshair.Cross,
                        AbilityId.Mr35Pin);

                case WeaponId.PriC:
                    // Far-wall Primary. falloffStart 55 exceeds indoor sightlines so no
                    // Bastion shot falls off. Sustained dps ≈ 73.0 — within 5% of MR-35's 74.0.
                    // Damage doubled 90 -> 180 with the 2026-08-27 Primary raise.
                    //
                    // PRI-C READS POORLY ON SINGLE-TARGET KILLS/S AND THAT IS CORRECT: 0.405/s
                    // flat, under the free MK-1 on every wave. It one-shots every body in the
                    // table, so its damage is mostly overkill against one — but the pierce is
                    // the weapon. 180 / 135 / 135 down a line all still exceed the deepest
                    // Default — but that body is 130 HP after the roster cut, so the pierced
                    // margin is FIVE POINTS where it used to be 57. A queue in a one-lane
                    // corridor is 3 bodies a shot: 1.622
                    // kills/s, which beats the sidearm everywhere. Do not "fix" this row by
                    // reading the single-target column.
                    // pierce 3 ×0.75 is the bore payoff at 0.6 shots/s; Railshot overrides both.
                    return Make(WeaponSlot.Primary, "PRI-C", cost: 1500L,
                        damage: 180f, shotsPerSecond: 0.6f, clipSize: 4f, reloadSeconds: 3.2f, range: 90f,
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
