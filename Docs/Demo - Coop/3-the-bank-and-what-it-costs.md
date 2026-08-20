# 3 — The bank, and what it costs

<!--
Purpose: The zone's Hatch-bank with more than one door open. Hold them all while the
pile drips into the crew pot; whatever has not moved when a door falls is gone.
Authored loss 1. No juice, no per-seat caps.
Status: demo spec
Authority: implementable
Update trigger: when the drip rate or window length moves, when the authored-loss aim
moves, when juice comes back
Last updated: 2026-08-20
Depends: voice-coop.md, 2-picking-each-other-up.md
-->

## Why this beat exists

Everything before this has been the party spending. This is the party earning, and it
is the first time the doors matter for a reason other than survival: the pile moves
home through them.

**Juice is deferred** (Zo, 2026-08-20). The bet is not in a dial this time, it is in
how long you hold. **There are no per-seat conversion caps and no per-seat wallets**:
persistence is deferred, the crew pot is one balance, and what drips belongs to the
party.

That makes the beat simple and the loss legible. Two doors, one timer, one pot filling.
When a door falls the transfer stops and the remainder is gone.

## Functional storyboard

Continues from beat 2. Standing arrives as **Hatch-bank at a hatch**.

### 0 — Arm the bank

| | |
|---|---|
| **Goal** | Know the transfer and the stake before starting it. |
| **Player** | At any open hatch: `E` arms. `T` starts. Either seat may arm; the lead starts. |
| **Game** | `demo.coop.bank_teach` · Lock (Prep) · Party · Game. **The pile moves into the crew pot while you hold the doors. Whatever has not moved when a door falls is gone. T when ready.** |
| **Standing** | Start the Hatch-bank |
| **Prompt** | Unarmed: `[+/-] Hatch-bank` + unbanked + carry + doors open + ` [E] select`. Armed: `[T] Hatch-bank` + ` [E] edit`. **No juice row** (deferred). |
| **Log** | existing `[Hatch] hatch-bank started wave … carry=<k> convert=<x> unbanked=<n>`, extended with `doors=<n>` (build) |
| **Have** | Start gate in `GameController.HatchBank`: host, Tethered+Prep, armed, Unbanked > 0, zone terminals complete. `TriggerDraw(HatchBank)` snapshots carry, timer 45 s, no dump. Prompts in `RepairInteractable`. |
| **Build** | The teach line, the `doors` field on the existing log, and removing the juice row from the prompt while it is deferred. Do not delete the juice code; gate it. |

### 1 — The window

| | |
|---|---|
| **Goal** | Hold every open door for 45 s while the pile drips at 8/s × the snapshotted multiplier into the crew pot. |
| **Player** | Fight. Bodies come from the spawn rooms and pick whichever door is weaker, so neither can be abandoned for long. |
| **Game** | — |
| **Standing** | Hold the doors |
| **Prompt / HUD** | `HATCH-BANK — {s}s`, the crew pot rising, and per-door HP. `MsgPlayer` `HATCH UNDER ATTACK` names which door. |
| **Log** | existing drip and damage lines |
| **Have** | `HatchBank.DripPerSecond = 8f`, `DurationSeconds = 45f`, `CarryConvert`, live drip with no dump on start, `EndRun` wiping the remainder. |
| **Build** | The window runs while **any** open door stands. Drip is into the crew pot, one balance, no split and no cap. Per-door HP readout on the HUD. |

### 2 — The fall

| | |
|---|---|
| **Goal** | Lose the remainder and understand exactly why. |
| **Player** | The doors go down. This window is authored so the beat-0 kit cannot hold both to the end. |
| **Game** | `demo.coop.first_bank_down` · Lock (Docked) · Party · Game. **They took the doors. What moved is in the pot. Spend it and go back in.** |
| **Standing** | Restock at the ship |
| **Log** | existing run-end line; plus `[Hatch] bank ended moved=<n> lost=<n> doors_down=<n>` (build) |
| **Have** | `EndRun` wipes remaining Unbanked and keeps what dripped. |
| **Build** | The line and the log. **Authored aim**: enough in the pot to meaningfully restock, well short of clearing the next stretch. Both figures tunable; the ordering is the spec. |

## Acceptance shape

Player action: two players start a Hatch-bank with two doors open, hold for part of the
window, and lose the remainder when the doors fall. Reads back: the crew pot rose
during the window and stopped rising when the last door fell, and the bank-ended log
names how much moved and how much was lost.

## Verification

The net fixture can hold: the window runs while any open door stands, the drip lands in
one balance, the remainder is wiped on the fall. Whether the loss reads as fair rather
than arbitrary is eyes-on and is the thing the fixture cannot see.

## Gaps

- **Juice is deferred, not deleted.** The prompt row and the propose-and-commit question
  come back with it. Nothing here should make that harder to re-enable.
- The authored-loss figures are absent, not invented. They are a tune after a played
  run, and the ordering is what this beat specs.
- Whether losing one door of two should end the window early or only slow the drip is
  **stamped**: it slows, and the window ends when the last door falls.
