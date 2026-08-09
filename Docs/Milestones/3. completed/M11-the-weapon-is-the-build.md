# M11 — The weapon is the build

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-04
-->

Replace the global Damage / Fire Rate / Clip tracks with a weapon catalog, per-weapon
Tiers, multi-slot gun, and a contextual ARMORY. Buying a Tier is the first purchase
a player can feel (~30% dps per step against a ~20% noise floor).

## Done when

**Met**, with two deliberate exceptions noted below. AutoVerify green at close:
EditMode **193 / 0**, PlayMode **10 / 0** (`gkb_verify_report.json`, 2026-08-04).

| # | Criterion | Evidence |
|---|---|---|
| 1–6 | Free MK-1, contextual ARMORY, slots 1/2/3 + wheel + Q, Primary locked for the Run, per-slot clip, HUD/crosshair/tracer identity | Ship/Bastion play + PlayMode slot-clip / firing tests |
| 7 | Every Tier step >20% effective dps | `WeaponCatalogTests.Tier_Step_ExceedsNoiseFloor_AllSevenSteps` |
| 8 | Available Primaries within 15% at T0 | `PrimaryBand_T0_AvailablePrimaries_Within15Percent` (VK-7 excluded) |
| 9–10 | BR-12 / MR-35 role numbers | EditMode pins in `WeaponCatalogTests` |
| 11 | Fifth weapon = catalog row only | `ThrowawayComposite_OwnableEquippable_ComposesAllModes` |
| 12 | Three Primaries fail differently in logged Runs | **Waived** — play-data comparison deferred; VK-7 disabled |
| 13 | v1/v2 → v3 weapon migration | `V1Save_MigratesWeaponOwned_ToMk1SecondaryWithTier` |
| 14 | Pacing &lt; 3× death earnings, Waves 2–5 | `PacingTarget_CheapestAvailablePrimary_UnderThreeDeathsEarnings` |
| 15 | `GunDamage` / `ShotsPerSecond` / `ClipSize` gone | compile + EditMode; replaced by `WeaponTier` |

**VK-7 stays disabled** (`WeaponCatalog.IsAvailable` false). Id and `Def()` kept for
saves; shop / equip / F7 / loadout refuse it. Criteria 8 and 14 run against BR-12 and
MR-35. Re-enable is a catalog flag flip when wanted.

## What shipped

- `Sim/Meta/`: `WeaponSlot`, `WeaponId`, `WeaponDef`, `WeaponCatalog`, `WeaponTier`
  (`MaxTier = 7`, cost `400 + 200×tier`, cumulative 7,000).
- `MetaState` / `SaveData` v3: ownership, tiers, equipped slots; migration from
  GunDamage/FireRate/Clip; fresh save owns nothing, MK-1 costs 0.
- `ShipCommands`: `BuyWeapon` / `BuyTier` / `EquipWeapon`; ARMORY contextual to slot.
- `PlayerGun`: per-slot clip/reload, pellets / pierce / falloff, identity cues.
- Docs: `Design/4-Data.md` Tier table and available-Primary pacing.

## What it did not settle

- **VK-7 in the shop** — disabled on purpose; the three-Primary fail-differently gate
  (criterion 12) waits on it or equivalent play data.
- **Wave 3 clear** — still M2. **Five Waves / deep start** — still M14.
- **Discoverability of slot keys** — bindings work; teaching them is M15.
