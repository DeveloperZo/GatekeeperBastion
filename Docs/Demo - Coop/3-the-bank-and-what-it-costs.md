# 3 — The Convert, and what it costs

<!--
Purpose: Convert as an opt-in bet on a withdrawn pile, with the Hatch as the
one door that can end the Boarding. Authored pressure, not authored earnings
(kills already banked on landing). Multi-door Convert is still Build.
Status: working
Authority: implementable against 0.34.2
Update trigger: when drip or window length moves, when Ante returns, when
per-door HP lands
Last updated: 2026-08-30 (kills bank on landing; Convert needs Withdraw;
Ante parked; singular Hatch 0 HP ends the Boarding)
Depends: voice-coop.md, 2-picking-each-other-up.md, ../Play Mechanics/The-Hatch.md
-->

## Why this beat exists

Kills already paid the realm pot as they fell. Convert is no longer how the
party earns; it is how the party **bets** a withdrawn pile against a 45 s
window at the door.

**The Ante is parked** (`Ante.Enabled = false`, 2026-08-30). No `+/-`, no
E-select. `T` starts Convert when Unbanked > 0.

**There are no per-seat wallets.** The realm pot is one balance. In a session
there *is* a per-seat Convert ceiling (`HatchBank.BankCap` against that seat's
deepest Wave); excess is destroyed, not handed to seats with room.

Two hull doors may be open. One HP pool is what they attack. When it hits 0
the Boarding ends (`EndRun("hatch")`); leftover Unbanked is wiped; banked
Salvage stays.

## Functional storyboard

Continues from beat 2. Standing arrives as **Convert at the Hatch — or dock**.

### 0 — Withdraw, then arm

| | |
|---|---|
| **Goal** | Put something at risk on purpose. |
| **Player** | Prep, Hatch at full. Hold Interact to Withdraw (prompt may hide behind the start line — `Choose` vs `Interact` split). Then `T` starts Convert. Lead or guest may start this one (`TryNetStartHatchBank`). |
| **Game** | `demo.coop.bank_teach` · Lock (Prep) · Party · Game. **The pile moves into the crew pot while you hold the doors. Whatever has not moved when a door falls is gone. T when ready.** |
| **Standing** | Start the Convert |
| **Prompt** | `[T] Convert  unbanked {n}` while Ante is parked (`RepairInteractable.cs:143-148`). No Ante row. |
| **Log** | existing `[Hatch] hatch-bank started …` (log keeps Hatch-bank — trap 8) |
| **Have** | Start gate: not client-as-sim, Tethered+Prep, auto-arm when `!Ante.Enabled`, zone terminals complete (`HatchBankStartBlocked`), `TotalUnbanked > 0`, not AllWavesCleared, not Master live (`GameController.HatchBank.cs:66-90`). Snapshot carry, timer **45 s**, no dump. |
| **Build** | The teach line. Multi-door `doors=` field on the log. Gate `IntensityJuice`; do not delete it. |

### 1 — The window

| | |
|---|---|
| **Goal** | Hold the Hatch for 45 s while the withdrawn pile drips at 8/s × the snapshotted multiplier into the realm pot. |
| **Player** | Fight at the door. Bodies hunt the Hatch. |
| **Game** | — |
| **Standing** | Hold the Hatch |
| **Prompt / HUD** | `CONVERT — {s}s`, banked rising, Hatch HP. |
| **Log** | existing drip and damage lines |
| **Have** | `HatchBank.DripPerSecond = 8f`, `DurationSeconds = 45f`. One `Subsystem`. Session ceiling: `CreditSlot` drops overflow. |
| **Build** | Window runs while **any** open door stands; per-door HP readout. Not live. |

### 2 — The fall — or the survive

| | |
|---|---|
| **Goal** | Either the door dies, or the window expires and dock is offered. |
| **Player** | Hatch 0 HP: Boarding Lost, leftover Unbanked wiped, banked kills kept. Window survived: remainder dumps, dock offered until the next Wave. |
| **Game** | On a fall: `demo.coop.first_bank_down` · Lock (Docked) · Party · Game. **They took the doors. What moved is in the pot. Spend it and go back in.** |
| **Standing** | Restock at the ship |
| **Log** | `EndRun("hatch")` or survived-bank / dump |
| **Have** | `EndRun` wipes remaining Unbanked and keeps banked. Authored "kit cannot hold both doors" cannot fire on one pool; Hatch pressure is `FieldWaveDamageScale` on the walk and full rate on Convert. |
| **Build** | The line and a `bank ended moved= lost= doors_down=` log. Per-door last-door rule. |

## Acceptance shape

Player action: two players Withdraw, start a Convert, and either survive the
45 s or lose the Hatch. Reads back: drip into the realm pot, and either the
survived-bank dump or `EndRun("hatch")` with banked Salvage intact.

## Verification

The net fixture can hold: Convert refuses at unbanked=0, drip credits bank,
Hatch 0 HP ends the Boarding, guest can request the start. Multi-door last-door
behaviour waits on per-door HP.

## Gaps

- **The Ante is parked, not deleted.** The prompt row and the propose-and-commit
  question come back with it.
- Authored-loss figures for a *multi-door* window are still absent. One-pool
  Hatch fall is the live loss.
- One-of-several doors slowing drip is **stamped**, not built.
- Guest-opens-Convert-for-the-whole-party is live and surprising
  (`The-Crew.md`); this beat does not re-teach it.
