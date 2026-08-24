# Gatekeepers and Waves

<!--
Purpose: The gatekeeper roster and the Wave composition curve — what kinds exist, what each one refuses, and how Waves escalate across the five.
Status: brainstorm
Update trigger: when a gatekeeper kind is added or its refusal changes, or when the Wave composition tables move.
Last updated: 2026-08-17
-->

## What this covers

The gatekeeper roster and how Waves are composed from it: which kinds exist, what each kind
refuses to do, and how Waves 1–5 escalate. Not the tuning of any single number — that is
`../Design/4-Data.md` — and not what the player shoots them with, which is
[`armory-and-weapons`](closed/armory-and-weapons.md).

## Where it stands

Three kinds in `Sim/Waves/GatekeeperKind.cs`. Two Wave modes ship (`WaveMode`):
**Terminal** hunts the Boarder (Hatch cannot be hurt); **HatchBank** hunts the Hatch
and converts Unbanked (Hunters still hunt you). Default / Thief retarget with the
mode (`M52`/`M53`); Hunter refuses the Hatch on both. `WaveDirector`'s
`HunterPerWave` / `ThiefPerWave` tables fix the counts; `defaultCount` absorbs the
remainder; intensity surplus is entirely Default.

The two jobs are the shipped loop. What the three kinds still do not use: levels,
lanes, a second player as a targeting fact. Named roster rows after `M63` are
that thickening, not a replacement for Terminal vs Hatch-bank.

A **refusal** governs the **target** a kind will and will not go after, and that stays
enforced in code (`1-Experience.md` §1.4). Damage amounts, fallback attacks and what a
kind does when cornered are not protected. Overriding a target for a Wave mode is a
dated break.

**Ram became Thief in code** with `M63-3` (2026-08-20), name only. The salvage rule is
recorded below and not built.

## The roster

**One table; `state` is the only place a row's status lives.** Named 2026-08-13 (Zo)
when `M63` was cut. Twelve is a target, not a promise — `M63`'s kill criterion recuts
the list if two kinds read as the same enemy. Behaviours, properties and every number
are **open**, decided per slice. This list is the roster, not a specification.

No two rows name the same thing taken.

| Kind | Takes from the player | Refusal (target) | State |
|---|---|---|---|
| Default | the choice — Hatch or you | — | in-build |
| Hunter | the camp | the Hatch | in-build |
| Thief | salvage on a carrier | damaging while salvage exists (recorded, not built) | in-build |
| Source | the empty room | open | named |
| Enabler | other kinds' jobs | open | named |
| Alarmer | this group's size | open | named |
| Bypasser | coverage | open | named |
| Reclaimer | salvage on the floor | open | named |
| Exploder | the ground you stand on | open | named |
| Standoff Shooter | the Hatch from range | closing | named (was demoted M18) |
| Ailment Shooter | a resource that is not HP | a clean HP-only fight | named |
| Cloaker | information | being seen | in-build |

**In-build rows** keep today's targeting until their own slice. The Thief still walks the
old battering errand until the salvage slice; only its name has changed.

**Named rows** have no hp, damage, speed or Wave count. `4-Data.md` gains none of them
until the kind exists.

**Caller** in `boarder-protocols.md` is this table's Alarmer. **Leaper** there is a
movement property, not a thirteenth kind.

### Thief's rule (recorded, not built)

Never damages while there is salvage to take; rams to take salvage from whoever holds
it; damages only when there is nothing to take. Builds against `RunSim.Unbanked`
(`M53`, shipped). `M63` records this; the Thief's own slice implements it.

### Properties (not kinds)

A property may ride on any kind. A variant is never a thirteenth kind:

movement (climber, vent-born, drops from above), shield, invulnerable until a stated
condition, visibility and camouflage, pack with a leader, splits on death, the elite
flag.

### Draw rule (recorded, not built)

A bastion shows a subset of the roster, not the roster. One difficulty budget per
bastion, Wave templates with slots that draw from that bastion's set, last Waves
reserved for the heaviest draw. Whether a bastion's set is authored, drawn, or a
fixed core plus a draw is **open**.

## Graduation test

**A logged five-Wave run with the gatekeeper-seconds spent at the door recorded per Wave** —
which term of `arrivals x dwell` actually breaks the Hatch at Waves 4 and 5. Every valid
loss on record is `HATCH BREACHED` with the player alive, but not one of them says whether
the door fell to too many bodies or to bodies standing too long, and a new kind specified
without that answer is a guess wearing a stat line. `M63` criterion 10 ships the line.

## Open

| Question | Owner | Blocks |
|---|---|---|
| Authored set, random draw, or fixed core plus a draw, per bastion | Zo | the second bastion, not `M63` |
| Which non-HP resource the Ailment Shooter takes | that kind's slice | Ailment Shooter |
| Whether Alarmer and Source read as two enemies | play, after both ship | extending the roster |
| Whether Thief and Reclaimer read as two enemies | play, after both ship | extending the roster |

## Ideas

None. The M18 stand-off kind is the Standoff Shooter row above. Its stat line, Wave
counts and damage band stayed demoted — they were derived from Wave-4 arithmetic, not
from play (`../_to_delete/demoted-M18-20260806/`). M18's ID is not reused.
