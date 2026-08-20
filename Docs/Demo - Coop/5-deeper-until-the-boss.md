# 5 — Deeper, until the boss

<!--
Purpose: The demo's back half. The party repeats hold-the-doors and restock, getting
incrementally further each time, until the kit is good enough to reach and take the
boss. Escaped ends the demo.
Status: demo spec
Authority: implementable
Update trigger: when the boss is specced, when the incremental-progress rule changes,
when the number of return trips is tuned
Last updated: 2026-08-20
Depends: voice-coop.md, 4-restock-and-go-back.md
-->

## Why this beat exists

**Progress through the bastion is incremental** (Zo, 2026-08-20). The party does not
make one deep run. They hold the doors, come home, upgrade and restock, and go back in
further. The boss is what the loop is climbing toward, and it is reachable only once
the kit has been built by repeating beats 3 and 4.

That is a deliberate simplification of the earlier shape, which had a sealed Control
Room and a single dramatic finale window with the party split across doors. **There is
no dramatic split.** Hatches are doors. The party holds them, together or apart as the
moment requires, and the drama comes from the boss and from how thin the pot is, not
from a staged separation.

## Functional storyboard

Continues from beat 4, and beats 3 and 4 have run at least twice by the time this
starts.

### 0 — Further each time

| | |
|---|---|
| **Goal** | Feel the run extending: a terminal further, a door further, a wave longer. |
| **Player** | Embark, retrigger the deepest terminal, clear more than last time, bank, come home. Repeat. |
| **Game** | — |
| **Standing** | Get past where you stopped |
| **Prompt / HUD** | Existing terminal and bank prompts. |
| **Log** | existing wave and bank lines, plus `[Demo] deepest reached=<n> attempt=<n>` (build) |
| **Have** | The deepest-terminal composite retrigger with partial start electable, from the solo demo. Doors unlock on bank success. |
| **Build** | The progress log, so a played session can be read back afterwards. Nothing about the loop itself is new; this step is beats 3 and 4 running again. |

### 1 — The last stretch opens

| | |
|---|---|
| **Goal** | Know the boss is the end and that this kit is what is fighting it. |
| **Player** | The final door opens on the last bank's success. |
| **Game** | `demo.coop.boss_ahead` · Lock (Prep) · Party · Game. **Whatever is through there is the last of it. This is the kit you are taking in.** |
| **Standing** | Take the boss |
| **Log** | `[Demo] boss stretch open` (build) |
| **Have** | Door unlock on bank-window survival. |
| **Build** | The line and the log, and the door keyed to the last bank rather than to a terminal AND. |

### 2 — The boss

| | |
|---|---|
| **Goal** | One fight the party takes together, with the doors still to hold behind them. |
| **Player** | Fight it. The ordinary pressure on the open doors does not stop. |
| **Game** | — |
| **Standing** | Take the boss |
| **Prompt / HUD** | `MsgPlayer` `HATCH UNDER ATTACK` still names a door if one is taking damage. |
| **Log** | boss spawn and death lines (build, with the boss) |
| **Have** | Nothing. |
| **Build** | **Absent, not invented.** The boss's stats, abilities and composition are unspecced here exactly as they are in the solo demo. What this beat specs is only the frame: the boss is fought while the doors are still live, so the party cannot give it their whole attention, and it is reachable only after the restock loop has run. The creature is another file. |

### 3 — Escaped

| | |
|---|---|
| **Goal** | End as a party. |
| **Player** | Boss down, final bank window held, remainder dumps. Escaped. |
| **Game** | `demo.coop.escaped` · Lock (Docked) · Party · Game. **Clear. You both got out with what you moved.** |
| **Standing** | — (demo ends) |
| **Log** | existing `CompleteRun` → Escaped, `CleanExits++`; plus `[Demo] co-op demo complete seats=<n> attempts=<n>` (build) |
| **Have** | `CompleteRun`, Escaped, `CleanExits`. |
| **Build** | Escaped is a **party** result: a seat waiting on the ship at that moment still escapes. The demo ends here and `DemoCoopActive` clears. A loss here does not end the demo; the party restocks and comes back, same as every other attempt. |

## Acceptance shape

Player action: two players reach the boss only after at least two restock trips, fight
it while doors are still under attack, and reach Escaped together. Reads back: the
deepest-reached log rising across attempts, the boss-stretch line, and the demo-complete
log naming the attempt count.

## Verification

The net fixture can hold: the final door keys on bank success, Escaped fires with a
seat on the ship, the Run does not end when one of several doors falls. Whether the
incremental climb feels like progress rather than repetition is eyes-on, and it is the
main thing this beat could get wrong.

## Gaps

- **The boss is unspecced.** Absent, not invented. This beat cannot close without it.
- **How many trips before the boss is reachable is not specced.** It is a tune against
  the crew pot's fill rate. Too few and the loop does not read; too many and it is a
  grind. Needs a played run, not an argument.
- Buy-back at a live window is **cut from this demo**. It depended on per-seat banked
  salvage, which the crew pot and deferred persistence remove. `M90` is unaffected and
  stays planned for whenever wallets come back.
- Three-seat behaviour is untested by construction: this demo authors for two and the
  rules must not break at three.
