# First Bastion demo — a voice that walks you through

<!--
Purpose: Minimal diegetic narration for the first-Bastion demo: dead Ship, one
terminal, designed Hatch-bank loss, ARMORY with two upgrades, start Wave 2.
Not the rest of the campaign.
Status: brainstorm
Update trigger: when Zo stamps convert-then-fight, the Hatch-bank checkpoint, or
the demo's extra sequences; when a prompt string is authored.
Last updated: 2026-08-16
-->

## What this covers

The cold-start of the **first Bastion demo**. Repair as the teaching verb. What is
said, and in what order, until they can embark at Wave 2 with two upgrades.
Not Master Control, not intensity, not FABRICATION, not a second destination.

## Where it stands

No onboarding. Ship consoles are all live. Weapon is a 0-salvage ARMORY buy;
Default Protocol is a 0-salvage CREW claim (`M70`, not closed). T1 opens on the
**5th** clear (`M52`). `DeepestWaveReached` rises on **clear**; NAV deep start
**chains** 1..N (`M14`). Unbanked is lost on a down. ARMORY Tiers unlock after
first loss. CREW is not allowed to lock behind a death. M62 rejected a companion
narrator. Narrative in `Design/1-Experience.md` is listed declined.

## Graduation test

Zo stamps convert-then-fight for the demo Hatch-bank, and Hatch-started as the
NAV cap — then a milestone can name the prompt set and the log lines.

## Loop or arc

**Arc**, onboarding for one Bastion demo. The loop it teaches is the existing one.

## What would kill it

A logged cold start that never fires `T` at a field terminal, or that survives
the first Hatch-bank with zero wheel purchases, or that returns from that loss
with `BankedSalvage = 0` so the two ARMORY upgrades cannot be bought.

## What it touches

Ship station repair-to-use (NAV, then ARMORY). Starting weapon grant. T1 door
count for this demo (`M52`). Hatch-bank convert timing (`M53`). NAV start-wave
cap (`M14`). ARMORY purchase cap of two. Optionally CREW claim timing (`M70`).
Mission drawer / interact prompts, not a companion.

## Decided — Zo, 2026-08-16

Wake in a capsule on a barely-live Ship. Teach move / look / interact by
repairing. Repair **NAV MAP** before a Bastion can be selected. Interact on the
Ship grants the starting weapon. First Bastion: explore, find a field terminal
that powers the place and releases Salvage to repair the Ship. First `T` starts
Wave 1 — **do not mention intensity**. After that clear, another terminal is
needed to unlock the door; **before** that door, Hatch-bank. That Hatch-bank at
zero upgrades is **not survivable**. Revive. Repair **ARMORY**; at most **two**
wheel purchases. Next embark may start Wave 2.

**Checkpoint is last Hatch-bank *started*, not last field Wave cleared.**
Completed Wave 3 then died on that Hatch-bank → NAV may start Wave 4. Died on
field Wave 3 with no newer Hatch-bank → cap stays at the last Hatch-bank started.

## Grill

Dying on a Hatch-bank today wipes Unbanked. A designed first loss with
convert-on-survive leaves beat 3 with nothing to spend. Zo's order is bank,
*then* the unsurvivable defense: **convert-on-start** for that Hatch-bank.
Stamp or veto.

"Power / release Salvage" is fiction over `T` starts a Wave, kills pay Unbanked.
No per-second siphon.

The weapon **grant** replaces the first 0-salvage ARMORY buy. ARMORY teaches the
shop one death later.

T1's five-clear door cannot host "one Wave, then Hatch, door waits on another
terminal." Recut for this demo; do not treat that as a campaign door rule.

NAV "start Wave 2" reads as **skip**, not a 1..N chain. Stamp skip vs chain.

## Beats — this demo

| Beat | Where | Player does | Withholds |
|---|---|---|---|
| 0 | Ship | Wake. Walk. Repair **NAV**. Take the starting weapon. | ARMORY / CREW / FAB. Intensity. |
| 1 | Bastion | Explore. One field terminal. `E` then `T`. Wave 1 Defaults hunting you. | Intensity. The door. |
| 1b | After clear | *Another terminal unlocks the door.* Do not walk it yet. | Opening the door this run. |
| 2 | Hatch | Hatch-bank. Convert-then-fight. Not survivable at 0 upgrades. | Survival. |
| 3 | Ship | Repair **ARMORY**. At most two wheel purchases. | CREW, FAB, a third buy. |
| 4 | NAV / Bastion | Embark at **Wave 2** (Hatch-bank in beat 2 *started*). | Intensity still unmentioned. |

## Extra sequences (same demo, after 4)

Only these. Not FAB, juice, Thieves, or Master Control.

| Seq | When | Does |
|---|---|---|
| Second terminal | On the Wave-2 embark, if they still need a door | Pays off 1b. Hunters are Wave 2's roster, not a lecture. |
| Default Protocol | After they have fought Wave 2 once and are back on the Ship | Repair **CREW**. 0-salvage Default claim (`M70`). Barrage / Rush for the next fight. Never auto-owned on the capsule. |

## Open

| Question | Blocks |
|---|---|
| Convert-then-fight: this demo Hatch-bank only, or every Hatch-bank? | Beat 2 salvage |
| Skip-to-Wave vs chain 1..N | Beat 4 |
| Grant is MK-1 only, or MK-1 vs SEC-D after ARMORY repair? | Beats 0 and 3 |
| Include the Default Protocol sequence in the demo cut, or stop at beat 4? | Extra seq; `M70` |

## Constraints

- First Bastion demo only. No new resource.
- No companion (M62). No siphon. No CREW lock behind death.
- Capsule is furniture; `Podded` stays retired.
- Do not mention intensity in this demo.
