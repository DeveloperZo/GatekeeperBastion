# M19 — Repair costs Salvage

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-05
-->

Charge **2 Salvage per hit point** to repair the Hatch, deducted as the repair is
applied.

## Done when

**Met.** `ShipSystems.RepairSalvagePerHp = 2`; `RunSim.RepairHatch` charges via
`MetaState.SpendSalvage` as HP is applied, stops at bank zero, and subtracts spend
from `LossReport.earned`. AutoVerify EditMode **211 / 0** (eight `RepairCostTests`),
PlayMode **10 / 0** (`gkb_verify_report.json`, 2026-08-06T03:26:56Z). mcs harness:
`RepairCost` **8 / 0**, full EditMode (minus Controls/Dev) **200 / 0**.

## What shipped

- `ShipSystems.RepairSalvagePerHp = 2f` beside `HatchMaxHp`.
- `RunSim.RepairHatch(requestedHp)` — need before cost; continuous accrual in Salvage
  quanta; `RepairSalvageSpent` on the Run; `LossReport.earned = gross - repairSpent`.
- `GameController.RepairHatch` command; `RepairInteractable` requests `rate * dt` and
  prompts rate / session cost / no-Salvage.
- Prep panel and end screens show the cost; run-over log includes `repair spent N`.
- `Docs/Design/4-Data.md` Hatch section describes the shipped rate.
- `Assets/Tests/EditMode/Ship/RepairCostTests.cs`.

## What it did not settle

Whether **2/HP** is the right rate, and whether zero-bank should **block** (shipped) or
**slow** repair — both *unknown, needs play data*. Playthrough path (hold E on a
damaged Hatch with and without bank) was not closed with a gate log this session.

## Story

As a player, I want repairing the door to cost me something I wanted to spend elsewhere,
so that how much damage I let through is a decision I carry rather than a number that
resets for free.

## Why it matters

Repair was free and unlimited during Prep, which made the Hatch a renewable resource and
Prep a button with no reason not to press it. Nothing in a Run competed for Salvage; it
flowed one way, bastion to ship.

At 2/HP, against the Hatch damage in the logs (88 HP after Wave 1, 276 after Wave 2):

| die on | run gross | full repair | share of gross |
|---|---|---|---|
| Wave 2 | 900 | 176 | 20% |
| Wave 3 | 1,830 | 728 | 40% |
| Wave 4 | 3,060 | 1,488 | 49% |
| Wave 5 | 4,590 | 2,448 | 53% |

Repairing everything every Wave breaks the three-death pacing target
(`Docs/Design/4-Data.md`) at Waves 4–5: three such runs bank 4,716 against the 6,300 Wave 4
requires. Repairing nothing clears it everywhere. That gap is the decision the cost
exists to create.

Leverage tier (`Docs/Working-Rules.md`): **#5, rules** — it adds a cost where there was
none, rather than tuning an existing rate.

## Acceptance criteria

1. Repairing the Hatch deducts `2 * healedHp` from banked Salvage, charged continuously
   as healing is applied, not in a lump at the end. **Met** —
   `ContinuousTicks_ChargeAsApplied_NotAsLump`.
2. Repair **stops** when banked Salvage reaches zero, leaving the Hatch partially healed
   and the bank at exactly zero — never negative. **Met** —
   `PartialRepair_AtInsufficientFunds_LandsBankAtExactlyZero`.
3. With zero banked Salvage, holding the repair key heals nothing and the HUD says why.
   **Met** in sim (`ZeroBank_HealsNothing`); HUD prompt/Prep copy ships
   `no salvage to repair` — play confirm open.
4. The HUD shows the live rate and the cost so far while repairing. **Met** in
   `RepairInteractable.Prompt` — play confirm open.
5. Salvage spent on repair is subtracted from the Run's earnings in the end-of-run
   report. **Met** — `EndReport_EarnedSubtractsRepairSpend`; end screens show
   `repair spent` when non-zero.
6. `Docs/Design/4-Data.md`'s Hatch section matches the shipped constant. **Met**.

## Implementation

**Change** `Sim/Ship/ShipSystems.cs` — `RepairSalvagePerHp = 2f`.

**Change** `Sim/Run/RunSim.cs` — `RepairHatch(float requestedHp)`; track
`RepairSalvageSpent`; `LossReport` gains `repairSpent`; `earned` is net.

**Change** `Game/Bastion/GameController.cs` — `RepairHatch` command.

**Change** `Game/Bastion/RepairInteractable.cs` — request through GameController;
prompt carries rate, session cost, insufficient funds.

**Change** `Game/Bastion/HudController.Panels.cs` / `.EndScreens.cs` — Prep and end
copy.

**Change** `Docs/Design/3-Mechanics.md`, `Docs/Design/3-Mechanics.md` — shipped cost.

**Create** `Assets/Tests/EditMode/Ship/RepairCostTests.cs`.

## Verification

1. Criteria 1–2, 5–6 — EditMode `RepairCostTests` (8), AutoVerify EditMode 211/0.
2. Criterion 3–4 playthrough — from Play: clear a Wave with Hatch damage, enter Prep,
   hold E at the Hatch with bank > 0 (prompt shows rate and rising cost); spend the
   bank to zero and hold again (heals nothing; prompt / Prep say no Salvage). Log should
   show `hatch fully repaired` only when topped off, and run-over `repair spent N` when
   any was spent.
3. mcs harness — `Tools/verify` with Unity MonoBleedingEdge `mcs`/`mono`,
   `--filter RepairCost` → 8 passed.

## Open questions

- **Whether 2/HP is right** is *unknown, needs play data*.
- **Whether repair should be blocked entirely at zero Salvage or slowed** is open.
  Criterion 3 shipped blocked.

## Watch

- `MetaState.BankedSalvage` is banked-at-kill, so repairing spends money the player
  already owns. Dying after repairing must not refund it.
- M14's deep start skips Prep, and therefore skips repair. Its crossover argument
  assumes repair has value; this milestone is what gives it any.

## Depends on

Nothing. Pairs with M14 — the deep start's design rests on repair being worth skipping.
