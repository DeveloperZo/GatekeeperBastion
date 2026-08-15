# M21 — The upgrade wheel

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

Weapon upgrades become a choice. The ARMORY shows a five-spoke wheel; the player rotates
the attribute they want to the centre and buys. The centred attribute takes the major
gain, the two adjacent spokes take a minor one, and the two opposite spokes take nothing.
Each weapon orders its wheel differently, so the same choice buys a different build
depending on the gun.

## Done when

**Met (code + Path 1 + Path 2 play).** Criteria 1–8 on Path 1 (`WeaponWheelTests` 8/0;
mcs suite **229 / 0**, 2026-08-08/09). Criteria 9–10 + Driven/Shown closed on Zo's Path 2
play sign-off **2026-08-09**: five spokes with two opposite dark; projection in seconds of
fire and reload; `[`/`]` rotate and click-a-spoke; purchase through `ShipCommands` only.
Reads back: `ShipCommands.BuyWheelUpgrade` log names weapon, centre, both neighbours.

`SaveVersion` **4** shared with M23. `MaxTier` / `MaxPurchases` **10** (110 points; cost
curve cumulative **13,000**).

## Correction, 2026-08-09 — `SaveVersion` is now 5, not the 4 criterion 4 records

Criterion 4 and the summary line above both state `SaveVersion` **4**, shared with M23, and
both were true at ship. **`SaveData.CurrentVersion` is now 5** (`Sim/Meta/SaveData.cs:26`).
Version 5 is the third weapon slot's rename from Melee to Drone and the JSON key
`"EquippedMelee"` → `"EquippedDrone"`, taken on 2026-08-08 and deliberately not backward
compatible (`Sim/Meta/SaveData.cs:16`; `../../../_meta/_definitions/language.md`, trap 3).

**Criterion 4's substance is unaffected.** The flat-tier migration it gates — tier N becomes
N x 11 points as 6N damage plus 5N clip — is `WeaponWheel.DistributeFlatTier` and still
ships; only the version number the migration lands on has moved. The v4 saves this criterion
was written against are no longer readable at all, which is the v5 decision and not a defect
in this milestone. Correction, not a retro-edit.

## Remaining to close

| # | Status | Evidence |
|---|---|---|
| MK-1 ring | **closed 2026-08-14** | Authored in M71 (`../2. active/M71-the-sidearm-joins-the-wheel.md`): D–A–R–C–F, strength Reload / weakness Damage. Ring tests pass; SEC-D remains the only unringed weapon (M26 ability call). |
| Playthrough feel | **open** | Two opposite 110-pt focuses on BR-12 — do they feel like different weapons? (`playthrough.md`) |

## Story

As a player, I want to push my favourite weapon toward what it is already good at — or
shore up what it is bad at — so that my copy of a gun is not the same as everyone else's.

## Why it matters

`WeaponTier` gave every step the same thing: `damage x1.25`, `clip x1.15`, fire rate
never. Seven Tiers and 7,000 Salvage bought one predetermined outcome, with no decision in
it anywhere.

The wheel makes the **ring order** the weapon's design. Adjacency means "these two serve
the same fantasy", so focusing a weapon's strength deepens the identity and pays nothing
toward its weakness, while focusing the weakness patches it at the cost of the identity.
That is one authored permutation per weapon and **zero numbers** — which is the point:
per-point values are global, rings are per-weapon, so tuning feel and tuning balance stop
colliding.

Leverage tier (`../../Working-Rules.md`): **#6, information flows, and #5, rules.** The
wheel is not a new number; it is who-gets-told-what when a purchase happens.

## The mechanic

Five attributes, fixed order-independent set: **reload speed, clip size, damage, fire
rate, ability**. Each weapon carries a cyclic ordering of them — its **ring**.

One purchase awards **11 points**: 5 to the centred attribute, 3 to each ring-neighbour,
0 to the two opposite. Ten purchases, **110 points** total per weapon. A player who
centres the same attribute every time ends at 50 / 30 / 30 / 0 / 0.

## The rings — DECIDED (2026-08-06/07)

| Weapon | Ring | Strength | Weakness | Lean in | Patch |
|---|---|---|---|---|---|
| **BR-12** | `R C D A F` | damage in one pull | reload | `D(CA)` | `R(CF)` |
| **MR-35** | `R D C A F` | damage per shot | fire rate | `D(RC)` | `F(AR)` |

### The MK-1's ring — still to author

Its ability is decided (M20: regen surge). Strength is accuracy/thrift; weakness is volume.
Apply weakness opposite strength and ability adjacent to what compounds it, then add the
row before building the MK-1's wheel. Primaries shipped without it.

## Acceptance criteria

1. Buying a weapon upgrade requires a centred attribute. There is no unfocused purchase. — **Met**
2. A purchase awards exactly 5 / 3 / 3 / 0 / 0 by ring distance, wrapping. Total is always
   11, on every weapon, at every tier. — **Met**
3. Each owned weapon stores **five accumulated point counters**. Tier is derived
   (`total / 11`), never stored. `MetaState.SetTier` and the `SaveData` tier array are
   deleted, not deprecated. — **Met**
4. A pre-wheel save loads without loss: flat tier N becomes N x 11 points distributed as
   6N damage + 5N clip, zero elsewhere. `SaveVersion` is **4** (shared with M23). — **Met**
5. Per-point values live in **one global table**, one row per attribute. No per-weapon
   number exists anywhere except the weapon's base `WeaponDef` stats and its ring. — **Met**
6. **No two weapons in `ShopWeapons()` share a ring**, compared up to rotation and
   reflection. Weapons with no authored ring are skipped. — **Met**
7. **No weapon places its stated weakness adjacent to its stated strength.** — **Met**
8. Same weapon, two opposite focus histories at 110 points, differ by **>= 2x** in burst
   length or vulnerability window. — **Met**
9. The ARMORY wheel shows all five spokes at once: centre highlighted, the two neighbours
   lit, **the two opposite spokes visible but dark**. — **Met** (Path 2 play)
10. Before purchase the wheel projects **seconds of fire before a reload, and seconds of
    reload** — not raw multipliers. — **Met** (Path 2 play)

## Usable, or it did not ship

- **Driven** — `[` / `]` (Ship context; Prep siphon keeps the same keys) or click a spoke; buy confirms centre. — **Met**
- **Shown** — five spokes, two dark, with a before/after projection. — **Met**
- **Reads back** — purchase log names weapon, centred attribute, and the two that came along. — **Met**

## What shipped

- `Sim/Meta/WeaponWheel.cs` — rings, 5/3/3/0/0, per-point table, effective stats, migration shape.
- `MetaState` five counters per weapon; `TierOf` derived; `SetTier` gone; `AwardWheel` /
  `ReplacePointsFromFlatTier`.
- `WeaponTier` cost curve only (`MaxTier = 10`; cumulative 13,000).
- `ShipCommands.BuyWheelUpgrade`; `ArmoryStation` wheel UI; `PlayerGun` / `PlayerAbility`
  consume wheel points (ability dial included; `MaxDialPoints = FocusAtMax`).
- `ControlId.WheelRotatePrev/Next` on `[`/`]` under `ControlContext.Ship`.
- `WeaponWheelTests` Path 1; `Decisions.md` fire-rate reversal; `Design/4-Data.md` wheel table.

## Verification

### Path 1 — mcs harness `--filter Wheel`

**Green 2026-08-08/09:** 8/0 Wheel; full Sim EditMode **229 / 0** (Controls/Dev excluded as before).

### Path 2 — play

**Closed 2026-08-09 (Zo):** five spokes with two opposite dark; wheel position persists per weapon;
purchase through `ShipCommands` only.

### Playthrough path

Play → ARMORY → BR-12 → centre **damage** → read projection → buy → centre **reload** →
buy. Log line names focus and neighbours. Feel question still open → `playthrough.md`.

## Depends on

**M20** (shipped). **M23** shared `SaveVersion` 4.
