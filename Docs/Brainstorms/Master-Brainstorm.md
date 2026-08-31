# Master Brainstorm

<!--
Purpose: The transient working surface for the pinned master-control chat. Holds Zo's raw
observations after they have been grilled against the code and the logs, until each one
leaves for a topic brainstorm, a milestone, or the bin.
Status: working — content cycles
Update trigger: every session that produces a play report or a directive worth holding
Last updated: 2026-08-30 (stamina cycled to crew-skills; the two leftover
entries still have no milestone file)
-->

## What this is

The holding surface for whatever the pinned chat is currently working on. Entries arrive
from a play session or a directive, get argued with, and leave. **Nothing here is
permanent and nothing here is a commitment.**

It is the one deliberate exception to `../_meta/_definitions/brainstorm.md`: no boundary
statement, because it is unbounded by design, and no graduation test of its own, because
each entry carries its own exit. Everything else in that contract still applies — grill
before recording, one paragraph per entry, one canonical place per fact, decisions dated
and attributed.

## How it cycles

An entry leaves by exactly one of four doors, and leaving means **deleting the entry here**
and adding a row to *Cycled off*:

| Door | What happens |
|---|---|
| **Milestone** | Acceptance criteria can be written. The file is created and the row names it. |
| **Topic brainstorm** | It belongs to a durable area (`armory-and-weapons`, `gatekeepers-and-waves`, …) and is not urgent. Moved verbatim. |
| **Already true** | The build already does it. Row says where, so it is not re-raised. |
| **Rejected** | Argued and dropped. The reason goes to the topic brainstorm's *Rejected* so it is not rediscovered. |

An entry that has sat through three sessions without moving is a signal the door is
unclear, not that it needs more thinking.

---

## Live entries

**These two have sat far longer than the three sessions this file calls a signal.**
Each was re-checked against the tree on 2026-08-30 and each is still true. The door
for both is **Milestone**, unwritten. Do not keep thinking them here.

### A health buffer cannot absorb the way damage actually arrives

*Zo: enemies either barely touch you or swarm you, so modest health improvements are not
needed.*

**Confirmed, and this is the sharpest number in the log.** In Run 1, between 17:02:19.5 and
17:02:26.9, the player took **14 hits of exactly 6 damage and went 100 HP to 16 in 7.4
seconds**. Wave 1 of that Run had one hit; Wave 2 had thirteen. Vitality L2 costs 600 and
raises the pool 100 → 110, which buys **1.7 more hits, about 0.9 seconds** of that burst.
Zo's conclusion follows from the arithmetic: at this arrival concentration no purchasable
buffer changes the outcome, so the lever is how many Gatekeepers can be swinging at once,
not how much HP absorbs them.

**Still true, re-checked 2026-08-30.** No room-wide swing cap. Per-body only:
`WaveDirector.GatekeeperAttackInterval = 0.7f` and `EnemyGatekeeper._attackTimer`.
`GatekeeperRoot.MaySwing` is the EMP-root reach rule, not a concurrency cap.
`PlayerRegen` climbs between clumps (15:42:54 HP 21 → 15:43:18 HP 86 in the
`[Dev]` 15:36 file). It does not absorb a simultaneous cluster: **6 hits of 6
in 0.27 s**, 15:44:18.368–18.636, HP 37→1, then death
(`Logs/GatekeepersProtocol_2026-08-30_15-36-50_p14936.log`, `dev=1`, cannot
close a gate). The 2026-08-08 14×6 / 7.4 s file is gone from disk.

**Exit: milestone, unwritten.** Criterion can be written against a swing-concurrency
cap or a per-Gatekeeper swing cooldown; the 15:44 cluster is the live before figure.

### Pressing a key and getting nothing back

*Zo: I tried activating the ability for the MR-35 and nothing happened.*

Two defects, and only one of them is the pin. **`PlayerAbility.TryActivate` returns
silently on two of its three failure paths** — `if (_cooldown > 0f) return;` and
`if (!fired) return;` both produce no log line, no HUD change and no sound. Only the
no-ability-on-this-weapon case logs anything. So a player who presses `F` cannot tell
whether the ability is on cooldown, not yet armed, or broken, and the developer reading the
log afterwards cannot either. That is the same class of problem M22 was written for: the
game not saying what it did.

**Still true for weapon `F`, re-checked 2026-08-30.** `PlayerAbility.TryActivate` logs
only the no-ability-on-this-weapon case; `if (_cooldown > 0f) return;` is still silent,
and so is the `fired` path below it. Protocol `G`/`H` already log named refusals in
`PlayerProtocolAbilities`. No HUD toast for either.

**Exit: milestone.** The criterion is that every refusal path says why, in the log and
on the HUD, starting with the weapon `F` paths that are still mute.

---

## Constraints

Rules any milestone leaving this page respects.

- **One lever per logged Run** (`../Working-Rules.md`).
- **No counters, rollups or a stats object** — `RunStats` was built and cut
  (`../Decisions.md`).
- **Grading a curve uses M29.** `Tools/wave-report` prints mean live; Hatch remaining
  saturates and its band is retired.
- **M21 shipped** (2026-08-09). Wheel retunes need a named leverage tier and play data.

## Cycled off

| Entry | Date | Door | Where it went |
|---|---|---|---|
| Stamina has no destination | 2026-08-30 | Topic brainstorm | `crew-skills-and-perks.md`. Falsifier is still an `M102` playthrough (outbound under fire). 0.34.2 recall pads shorten the *return*; the Outrider follows, it is not a place to go. Rush is fire-mul, not sprint. |
| More bodies and more HP is one lever too many, and it feeds the pierce | 2026-08-25 | Already true | **M29 shipped** the instrument (`Tools/wave-report` prints mean live). The two objections stand as recorded: `../Working-Rules.md` requires one lever per logged Run, and raising count works against the swing-concurrency entry above, because more bodies in the room is more Gatekeepers able to swing at once. Raising HP alone is the cleaner half. Grade curve changes against mean live; Hatch remaining gives no effort reading (`M16`) |
| Crew Quarters cannot compete with the weapon on price | 2026-08-12 | Topic brainstorm | **Answered:** tracks had no job. CREW sells Crew abilities (burst, long cooldown) — `crew-skills-and-perks.md`. Tracks and 5-skill parked. |
| Loss / XP / hatch tools / specialization (Zo 2026-08-12) | 2026-08-12 | Topic brainstorm | `missions-and-destinations.md` (pile vs ship bank; two triggers), `fabrication.md` (hatch defenses cut), `crew-skills-and-perks.md` (Crew abilities) |
| The Primary's ability has never fired | 2026-08-08 | Milestone | **Fixed in M31 Path 1.** `RegisterPinHitIfNeeded` ran once per damaged body; `PinBuildState.RegisterHit` zeroed `Hits` on target change. M31 gates the pin to the first damaged body. Playthrough criterion still open on the M31 file. |
| The damage ceiling is ~4.8x and the pierce is free | 2026-08-08 | Milestone | **M31 closed criterion 6** — falloff table plus MR-35 `pierceDamageMul` **0.26** (Zo, 2026-08-09) so FocusAtMax pierce lands under Wave-5 78 HP. Far-room gap filled by **M32**. |
