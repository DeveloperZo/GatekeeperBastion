# Missions and destinations

<!--
Purpose: The NAV MAP console, run structure inside a bastion, and having more than one place to go.
Status: brainstorm
Update trigger: when a remaining entry graduates, or when a second destination exists.
Last updated: 2026-08-19
-->

## What this covers

What a Run **is** — its shape, its objectives, where it happens — as against how hard it
hits. Destinations beyond ALPHA-7, path telegraph, spawn visibility. The two-trigger loop
has graduated (table below).

## Where it stands

**M47 completed 2026-08-13** (kit bastion, unlock graph). Field loop shipped: **M52** terminal
Waves, **M53** Hatch-bank, **M54** Master Control, **M55** intensity juice. One destination.
Cold-start recut of T1's door and the Hatch-bank checkpoint for the **first
Bastion demo** lives in [`first-session.md`](first-session.md); do not restamp
those here as campaign rules.

### Terminal roles (Zo, 2026-08-13)

| Role | Does | Status |
|---|---|---|
| **Field / progression terminal** | Interact starts one Boarder-hunt Wave; T1 opens its **`D`** on the 5th clear; Open-door Interact if the slab is still up. Hatch-bank is legal in the untimed Prep after each clear | M52 |
| **Hatch-bank** | Starts Hatch-bank Wave; converts unbanked → banked | M53 (Hatch look-at + `T`, or console — same loop) |
| **Resource terminal** | Resources / boosts | Parked until the two Wave roles have play data |
| **Master Control** | Win sequence | M54 |

Passages (Door side, no `D`) have no terminal. Soft-lock doors stay parked.

## Graduation test

A second destination that differs in something other than difficulty — layout, hazard,
composition, or what Master Control pays — with criteria that can be true or false. Two
identical rooms is not a choice. Needs M54 playable so this destination's loop exists.

## Graduated — these left this file

Link, never restate.

| What | Milestone |
|---|---|
| Field terminal starts one Boarder-hunt Wave; T1 door on 5th clear; Hatch invulnerable during Terminal; Hatch-bank legal after each clear | [`M52`](../Milestones/3.%20completed/M52-waves-start-at-a-terminal.md) |
| Unbanked pile; Hatch-bank drip / attrition / dump; 3 slots; convert multiplier | [`M53`](../Milestones/3.%20completed/M53-salvage-leaves-through-the-hatch.md) |
| Master Control two-phase sequence is the win | [`M54`](../Milestones/3.%20completed/M54-master-control-is-the-win.md) |
| Visible intensity; pay-to-juice; siphon dial retired | [`M55`](../Milestones/3.%20completed/M55-pay-for-intensity.md) |

The console restructure, opt-in loot delivery, “a reason the bastion is a place,” and
*Deliver-loot-before-the-run-ends* (`../Decisions.md`) went with those four.

## Ideas

### The bank window happens on your ship (Zo, 2026-08-18)

**The Hatch is a placeholder for where the defense happens.** Today a Hatch-bank
Wave is defended at the Hatch, inside the bastion. The idea is that it eventually
happens **aboard the player's own ship**: you deliver, and what comes for the
salvage boards you.

Why it is worth taking seriously. The Hatch is only the umbilical; the thing
actually being defended is the ship the salvage is going to, so defending the
door is one indirection away from defending the thing. It also inverts the
premise cleanly — you board a held vessel, and delivering is what invites them
aboard yours. And it gives the Ship a reason to be a place. Right now it is four
stations and a walk; if it can be attacked, where those stations sit becomes level
design, and anything the player builds there is worth building because it persists
across Runs rather than being abandoned with the bastion.

That last part is the strongest pull: it is the first home base in the design.
`M56`'s turret and the decoys are currently spent on a space you leave. On the
ship they would accumulate, which is much closer to the emplacement fantasy than
placing a turret in someone else's corridor.

**Open, and each one is real work rather than a detail:**

- The Ship is host-local in co-op (`M51`). A defended ship has to be shared, and
  that is a session-layer change, not a scene change.
- Scale. The Ship is a small plus; a wave arriving there is a different fight
  from a wave crossing a bastion, and it may want its own composition rather than
  the bastion's.
- What losing it costs. Losing the bank, losing the Run, or damaging something
  that persists are three different games.
- Whether it replaces the Hatch-bank window or sits after it. Replacing keeps one
  defended moment per delivery; adding makes every delivery two fights.

**Graduation test:** the Hatch-bank window has been played enough to say what it
is for. If the answer is "a fight you have while a number goes up", moving it to
the ship changes only the backdrop and is not worth the session-layer cost. If the
answer is "the moment the Run's stake is decided", it belongs where the stake
lives.

**Kill it if** a played session shows the Ship works better as the one place
nothing can reach you. Somewhere safe is a design position, not an absence of one.

### Destinations that differ
More than one bastion, differing in something other than difficulty — layout, hazard,
composition, what Master Control pays. Needs M54; two identical rooms is not a choice.

### Path telegraph
Paths to the Hatch shown tower-defense style so pressure can be read before delivery.
**They are a forecast and some things break them.** Hunters exist to break the way home.
Needs a vocabulary of confidence (committed / uncertain / known vs suspected). **Not M36.**
Park until a dedicated telegraph milestone.

### Spawn visibility
`SpawnPoint` already enables/disables. Visibility is a lever — known, suspected, or hidden
— rather than a binary. Not specified. Walking out of a hung closet (enemy-only GateSlab)
is [`M73`](../Milestones/3.%20completed/M73-the-spawn-point-is-a-room.md), not this lever.

### Soft-lock doors (parked)
Locked doors that open without a Wave — explore switch / key, or salvage-paid repair. **Passage**
(Door side, no `D`) and **progression gate** (`BastionDoor` + wave `T`, M52) ship first. Soft-lock
is a third unlock loop; park until terminal Waves have play data so it cannot dodge the field
fight. Needs its own graduation when ready.

### Exploring alters spawn points
A spawn point reached and shut is nearly free (`OnEnable` / `OnDisable`). Waits on the
field loop being playable so there is somewhere to explore.

## Rejected

- **In-wave field resources at 10%** — the terminal is the errand.
- **Hatch defenses as a FAB shelf** — `fabrication.md`; Hatch Armor and Repair Rig stay.
- **Objectives-across-areas as the Run structure** (Zo, 2026-08-11) — the room gets bigger;
  Waves stay the delivery.
