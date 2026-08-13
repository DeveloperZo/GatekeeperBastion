# Missions and destinations

<!--
Purpose: The NAV MAP console, run structure inside a bastion, and having more than one place to go.
Status: brainstorm
Update trigger: when a remaining entry graduates, or when a second destination exists.
Last updated: 2026-08-13
-->

## What this covers

What a Run **is** — its shape, its objectives, where it happens — as against how hard it
hits. Destinations beyond ALPHA-7, path telegraph, spawn visibility. The two-trigger loop
has graduated (table below).

## Where it stands

**M47 completed 2026-08-13** (kit bastion, unlock graph). The field loop is planned: **M52** terminal
Waves, **M53** Hatch-bank, **M54** Master Control, **M55** intensity juice. One destination.

### Terminal roles (Zo, 2026-08-13)

| Role | Does | Status |
|---|---|---|
| **Field / progression terminal** | Interact starts that `T`'s remaining Boarder-hunt Waves (T1 = 5 from one Interact, 8 s Prep between clears) and opens its **`D`**; Open-door Interact if the slab is still up | M52 (next) |
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
| Field terminal starts its remaining Boarder-hunt Waves from one Interact (T1 = 5, 8 s gap); Hatch invulnerable; door on Nth clear or Open-door Interact; targeting mode | [`M52`](../Milestones/3.%20completed/M52-waves-start-at-a-terminal.md) |
| Unbanked pile; Hatch-bank drip / attrition / dump; 3 slots; convert multiplier | [`M53`](../Milestones/2.%20active/M53-salvage-leaves-through-the-hatch.md) |
| Master Control two-phase sequence is the win | [`M54`](../Milestones/1.%20planned/M54-master-control-is-the-win.md) |
| Visible intensity; pay-to-juice; siphon dial retired | [`M55`](../Milestones/1.%20planned/M55-pay-for-intensity.md) |

The console restructure, opt-in loot delivery, “a reason the bastion is a place,” and
*Deliver-loot-before-the-run-ends* (`../Decisions.md`) went with those four.

## Ideas

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
— rather than a binary. Not specified.

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
