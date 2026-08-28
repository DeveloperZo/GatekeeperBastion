# 1 — Two doors to hold

<!--
Purpose: Zone 1's field terminal with two hatch doors open. Bodies come from the
spawn rooms as they do today and pick which door to attack, so one player cannot
hold both. The beat that proves the hatch rule or kills it.
Status: demo spec
Authority: implementable
Update trigger: when the routing stamp is overridden, when the WaveDirector player
scaling changes, when the zone shape changes
Last updated: 2026-08-25 (thiefCount/cloakerCount identifiers; the quoted line and the
standing say Convert)
Depends: voice-coop.md, 0-the-invite.md
-->

## Why this beat exists

This is the beat the whole demo rests on. If a party of two can stand shoulder to
shoulder at one hatch and clear the wave, there is no co-op game here, only a solo
game with a spectator.

What makes that impossible is a rule rather than authored geometry. A hatch is a door
from the bastion to your ship and gatekeepers attack the door itself. With two doors
open, a body leaving a spawn room picks the one that is banking or weakest, so an
unattended door is a door that falls.

**The stamped ordering is the spec** (counts tunable):

> one Boarder covering both doors < the pressure two open doors take < two Boarders,
> one on each

A party that splits and holds is expected. A party that stacks and holds is a defect
in the routing or the tune, and the kill criterion in [`README.md`](README.md) is
what decides which.

## What changes in `WaveDirector`

Stated plainly because it is the one code delta that carries the beat.

Today `NextWave` multiplies `enemyCount` by `playerCount` and clamps the fixed
`hunterCount`, `thiefCount` and `cloakerCount` table rows against the scaled total.
Health multiplier, group size and spawn interval never see `playerCount`. The result
is proportionally more Default gatekeepers arriving from the same spawn rooms at the
same single door, since the surplus is entirely Default by construction.

**The hatch replaces that multiplier. It does not stack on it.** Per-seat body count
stays where it is; those bodies now divide across the open doors instead of massing
on one. At `playerCount == 1` the arithmetic must be byte-identical to today,
and that is a test, not an intention.

## Functional storyboard

Continues from beat 0 step 5. Standing arrives as **Find a field terminal**.

### 0 — Find it

| | |
|---|---|
| **Goal** | Get to the terminal without being walked there. |
| **Player** | Explore. Either seat may find it. |
| **Game** | — |
| **Standing** | Find a field terminal |
| **Prompt** | live `ShipHudView.SetPrompt` idle behaviour |
| **Log** | — |
| **Have** | Field terminals, prompts, the drawer. |
| **Build** | Nothing. No nag line. These players have done this. |

### 1 — Arm and start

| | |
|---|---|
| **Goal** | Commit together, host-authored. |
| **Player** | Either seat looks at the terminal. `E` arms. Host `T` starts. |
| **Game** | `demo.coop.first_terminal` · Lock (Prep) · Party · Game. **Two hatch doors open, and they will go for whichever is weaker. They will not split evenly.** |
| **Standing** | Clear the Wave |
| **Prompt** | Live: unarmed `[+/-] <terminal>` + stake + ` [E] select`; armed `[T]` + ` [E] edit`. |
| **Log** | existing terminal-start line, plus `[Hatch] entries active count=<n>` (build) |
| **Have** | Host-only start, Prep-only, armed id. Juice is deferred, so its prompt row is gated off. |
| **Build** | The line. Guests may arm; only the host starts (R1, and it is already the live rule). |

### 2 — The Wave

| | |
|---|---|
| **Goal** | Discover that the two of you are not defending the same thing. |
| **Player** | Fight. Bodies come from the spawn rooms and split between the two doors. |
| **Game** | — |
| **Standing** | Clear the Wave |
| **Prompt** | — |
| **Log** | `[Wave] target hatch=<id> kind=<…> n=<…>` at verbose; `[Wave] wave <n> composition …` always-on (build/extend) |
| **Have** | Wave spawning, `NetEnemy` host-spawned NetworkObjects with client clones. |
| **Build** | Target selection only. **Spawn rooms are untouched.** A gatekeeper's hatch target is the banking door if one is banking, else the lowest-HP open door, re-evaluated on hatch HP change rather than per frame. Hunters ignore hatches and are unaffected. Client hits stay client-authoritative as today (`NetEnemy.cs:75-76`); a known hole, not this beat's to close. |

### 3 — Clear

| | |
|---|---|
| **Goal** | Bank the lesson before the bank window asks for it. |
| **Player** | Last body drops. |
| **Game** | `demo.coop.after_terminal` · Subtitle · Party · Game, on clear. **Wave down. A Convert moves salvage home through those same doors.** |
| **Standing** | Convert at a hatch |
| **Log** | existing wave-clear line |
| **Have** | Wave clear, `DemoZoneDeepestWave` write on field-Wave clear (per-seat). |
| **Build** | The line. Write deepest per seat from the same rule as solo, for every seat including guests. |

## Acceptance shape

Player action: two players clear zone 1's Wave with two hatch doors open. Reads back:
the target log shows bodies picking both hatch ids, and the door left unattended takes
damage.

## Kill criterion

Carried from [`README.md`](README.md), repeated here because this is the beat that
tests it. **If both players spend the majority of the Wave inside the same hatch's
defensible area and still clear it, the hatch rule did nothing the geometry could not,
and it reverts to one hatch.** Needs per-player position logging against hatch
positions, which does not exist today. Stamped 2026-08-20, before the work.

## Gaps

- Target selection is **stamped, not ruled**. If a body instead locks the nearest door
  for its whole life, the party can zone the map and the beat's purpose weakens.
- The re-evaluation trigger (on hatch HP change) is a guess at the cheap shape.
  Absent, not invented: no re-target cadence is specced.
- Zone 1's terminal count and Wave count are inherited from the solo demo and not
  re-argued here.
