# M23 — The repair rig is equipment you bring

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

Repairing the Hatch requires the repair rig, a piece of **equipment**. The equipment slot
unlocks at the first lost Run and is bought with Salvage; the rig is bought on top of it.

## Done when

**Met (code + EditMode + PlayMode gate).** `SaveVersion` 4 (shared with in-tree wheel-points
work from M21 — one bump for both); `EquipmentId` / `EquipmentCatalog`; `MetaState`
equipment fields; Repair Rig + Engineering tracks deleted; migration grants slot+rig from
legacy levels; FABRICATION sells slot then rig; Hatch / Prep / NAV refuse without the rig
and name the missing step. AutoVerify EditMode **227 / 0** (eight `EquipmentTests` +
catalog pin), PlayMode `RiglessEmbark_CannotRepairHatch` **Passed** —
`gkb_verify_report.json`, 2026-08-08T19:45:34Z. Dotnet Sim harness **216 / 0**.

## Correction, 2026-08-09 — `SaveVersion` is now 5, not the 4 this file records

The `Done when` line and `What shipped` both state `SaveVersion` 4 / `MetaState` and
`SaveData` **v4**, and both were true at ship. **`SaveData.CurrentVersion` is now 5**
(`Sim/Meta/SaveData.cs:26`): the third weapon slot was renamed Melee to Drone on 2026-08-08,
carrying the JSON key `"EquippedMelee"` → `"EquippedDrone"`, with no compatibility shim
(`Sim/Meta/SaveData.cs:16`).

Nothing this milestone shipped changed. `EquipmentSlotPurchased`, `OwnedEquipmentMask` and
`EquippedEquipment` are still on `SaveData`, and the legacy `RepairRigLevel` /
`EngineeringLevel` migration fields are still there for the same reason. Correction, not a
retro-edit.

## Remaining to close

| # | Status | Evidence |
|---|---|---|
| Playthrough path | **open** | Fresh save → locked slot → lose → buy 300+300 → repair at 8 HP/s; negative half: refusal log line |
| Watch: first rigless Run after ship | **open** | Needs play data — hatch budget numbers assume the rig |
| Prices 300/300 | **open** | *tunable, needs play data* |

PlayMode suite still carries one pre-existing fail (`ShoveDoesNotUncommit`, M20) — not this
milestone.

## What shipped

- `Sim/Meta/EquipmentId.cs`, `EquipmentCatalog.cs` (slot 300, rig 300, flat 8 HP/s).
- `MetaState` / `SaveData` v4: `EquipmentSlotPurchased`, `OwnedEquipmentMask`,
  `EquippedEquipment`; legacy `RepairRigLevel` / `EngineeringLevel` kept on `SaveData` only
  for migration.
- `ShipCommands.BuyEquipmentSlot` / `BuyEquipment`; `UpgradeTrack.RepairRig` /
  `.Engineering` removed.
- FABRICATION equipment section; CREW QUARTERS four attributes.
- `RepairInteractable.Configure(rate, equipped)` always pushed; refusal prompt + log;
  `GameController.RepairHatch` and `HatchStation.ApplyRepair` gated; Dev restore exempt.
- Prep Hatch line and NAV MAP embark warning name a missing rig.
- `Docs/Design/4-Data.md` Hatch section; `language.md` track table.

## Story

As a player, I want fixing the Hatch to be a capability I earned and chose before the Run,
so that the door surviving is a decision I made at the Ship rather than a button that was
always there.

## Why it matters

Repair was free capability. `MetaUpgrades.RepairPerSecond(0)` returned a working rate and
`RepairInteractable` checked no ownership. Zo's arc: watch the Hatch take damage you cannot
solve, then unlock the tool having already seen the problem.

Leverage tier (`../../Working-Rules.md`): **#5, rules.**

## Acceptance criteria

1. Equipment slot in sim — **Met.**
2. Locked until `PodEjections >= 1`, then buyable; locked row names condition — **Met**
   (UI + `SlotLockedBeforeFirstLoss`).
3. Rig bought after slot; buying equips — **Met** (`RigRequiresSlot`).
4. Repair refused without rig; prompt names next step; refusal logged — **Met**
   (`RepairMissingStep_NamesNextGap`; interactable).
5. Rigless Run cannot move Hatch HP by player path — **Met**
   (`RiglessEmbark_CannotRepairHatch`).
6. `RepairPerSecond` / `RepairSpeedMultiplier` / levels / panel rows deleted — **Met**
   (`RemovedTracksAreGone`).
7. `SaveVersion` bump; no refund; reconstruct skips grants and deleted tracks — **Met.**
8. Legacy levels grant slot+rig free — **Met** (`MigrationGrantsSlotAndRigAndBalances`).
9. Still 2 Salvage/HP; rate flat 8 — **Met.**
10. Prices 300/300 in `4-Data.md` — **Met** (tunable).

## Usable, or it did not ship

- **Driven** — lose, buy slot, buy rig, repair — playthrough open.
- **Shown** — locked row; Hatch / Prep missing-step copy — **Met** in code.
- **Reads back** — slot purchase log; refusal log — **Met** in code; play confirm open.

## The fallback, if the rig turns out to be compulsory

Once a second equipment piece exists, if the rig is equipped in more than four of five
logged Runs, make the rig default kit outside the slot. Until that check can run, the
rig ships in the slot.

## Verification

### Path 1 — EditMode / harness

| Test | Asserts | Criterion |
|---|---|---|
| `SlotLockedBeforeFirstLoss` | unlock gate | 2 |
| `SlotPurchaseRequiresUnlockAndSalvage` | price once | 2 |
| `RigRequiresSlot` | buy equips | 3 |
| `MigrationGrantsSlotAndRigAndBalances` | grant + ledger | 7, 8 |
| `GrantsNotCountedInSpendFloor` | no grant in floor | 7 |
| `RemovedTracksAreGone` | deleted APIs | 6 |
| `RiglessEmbark_CannotRepairHatch` | HP frozen | 5 |

### Path 2 — AutoVerify

EditMode 227/0; PlayMode gate Passed (2026-08-08T19:45:34Z).

### Playthrough path

Fresh save → FABRICATION locked slot → embark, lose → buy slot (300), buy rig (300) →
repair at 8 HP/s. Negative half: before buying, interact produces no repair, prompt names
the step, one refusal line.

## Depends on

Collided with M20/M21 on `SaveVersion` only — M23 took the bump to **4**. Drone bay is
independent and later.
