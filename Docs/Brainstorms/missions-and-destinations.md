# Missions and destinations

<!--
Purpose: The NAV MAP console, run structure inside a bastion, and having more than one place to go.
Status: brainstorm
Update trigger: when a remaining entry graduates, or when a second destination exists.
Last updated: 2026-08-25 (Demo folder paths corrected; the restated T1 wave count replaced by its source, which four documents disagreed about)
-->

## What this covers

What a Run **is** — its shape, its objectives, where it happens — as against how hard it
hits. Destinations beyond ALPHA-7, path telegraph, spawn visibility. The two-trigger loop
has graduated (table below).

## Where it stands

**M47 completed 2026-08-13** (kit bastion, unlock graph). Field loop shipped: **M52** terminal
Waves, **M53** Hatch-bank, **M54** Master Control, **M55** intensity juice. One destination —
`SceneFlow.BastionName` is still ALPHA-7 and `SceneFlow.CombatScene` is still the single
`Bastion_Coop`, which the co-op hull (`M95`) replaced the old solo scene with rather than
adding to. Cold-start recut of T1's door and the Hatch-bank checkpoint for the **first
Bastion demo** lives in `../Demo - Single/` and `../Demo - Coop/` (the first-session brainstorm
was retired 2026-08-19); do not restamp those here as campaign rules.

### Terminal roles (Zo, 2026-08-13)

| Role | Does | Status |
|---|---|---|
| **Field / progression terminal** | Interact starts one Boarder-hunt Wave; a terminal opens its **`D`** after its authored count, which is `DemoZones.WavesPerTrigger` and is not restated here (this row said "the 5th clear" until 2026-08-25 and the code has moved twice since); Open-door Interact if the slab is still up. Hatch-bank is legal in the untimed Prep after each clear | M52 |
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
| Field terminal starts one Boarder-hunt Wave; a door opens on its authored count; Hatch invulnerable during Terminal; Hatch-bank legal after each clear | [`M52`](../Milestones/3.%20completed/M52-waves-start-at-a-terminal.md) |
| Unbanked pile; Hatch-bank drip / attrition / dump; 3 slots; convert multiplier | [`M53`](../Milestones/3.%20completed/M53-salvage-leaves-through-the-hatch.md) |
| Master Control two-phase sequence is the win | [`M54`](../Milestones/3.%20completed/M54-master-control-is-the-win.md) |
| Visible intensity; pay-to-juice; siphon dial retired | [`M55`](../Milestones/3.%20completed/M55-pay-for-intensity.md) |

The console restructure, opt-in loot delivery, “a reason the bastion is a place,” and
*Deliver-loot-before-the-run-ends* (`../Decisions.md`) went with those four.

## Ideas

### The Hatch is a hole in your own hull (Zo, 2026-08-18, redesigned 2026-08-20)

**The Hatch is not a door in the bastion you defend. It is the hole you open in your
own ship**, and opening it is what invites them aboard. Terminal Waves are where
salvage is collected. The survival window is where it is moved, and that window
happens on the Ship.

**The shape.** Terminal Waves in the bastion fill the pile. You open the Hatch when
you choose, and nothing comes before that — belief 4 applied to the defense and not
only to the Wave. Then two objectives with different verbs: **ferrying**, which is
spatial and easy, where some salvage credits as value and some are carried objects in
the manner of Depth's treasure, worth more or carrying an effect; and **upload**,
which is a timer you hold. Enemies enter through **one point to start**. Later Waves
find other angles; the single point is the first shape rather than the final one (Zo,
2026-08-20). Wiping costs the loot and **damages the ship's functionality**, degrading
bonuses rather than ending you.

**Why it holds together.** The chokepoint is known in advance and can be prepared,
which is the emplacement fantasy delivered rather than gestured at. The Hatch stops
being an abstraction with HP and becomes a decision with a reason attached to it.
Failure keeps the Run alive and makes the next one poorer, which is belief 1 as a
scene rather than a counter ticking. And the Ship stays the exhale, because it is only
dangerous while you have chosen to open it.

**Settled.**

- Ferrying interrupted is recoverable: a carried crate can be taken and chased down,
  which is what the Thief was designed to do. Upload interrupted is not recoverable,
  or the two objectives are the same failure twice with different art.
- Degradation is repairable and repair is paid, scaled to run count or to salvage.
  Permanent degradation breaks belief 2 on one bad night; free repair is not a cost.
  This is `M19`'s shape reused rather than a new economy.
- `ShipSystems` — Reactor, TetherCore, Engines, LifeSupport — is what degrades.
  `../Decisions.md` already defers those while keeping the aggregate "so the wiring
  keeps its shape", which is exactly this.
- `M72` closed the objection that the Ship was host-local. It is a shared live session
  with party-wide scene changes, so a defended Ship is a scene and encounter change
  rather than a session-layer one.

**Open.**

- **The carry has to cost something.** In Depth, carrying treasure makes you slow and
  visible, and that is the entire tension. If carrying a crate here costs nothing,
  ferrying is walking. Occupying your hands is the cost already in this game's
  vocabulary, and it composes with a Thief that goes for whoever is richest: one player
  carries, the others cover.
- **How many special-item effects, and what they modify.** Effects that change the
  *window* — speed the upload, seal a breach, worth triple but louder — stay inside the
  encounter. Effects that change the Run become a second item system.
- Whether the bastion Hatch-bank survives alongside this or is replaced by it.

**Graduation test:** one played Hatch-bank window that answers what the window is for.
If it is a fight you have while a number goes up, moving it to the Ship changes the
backdrop. If it is the moment the Run's stake is decided, it belongs where the stake
lives.

**Kill it if** the carry cost cannot be made to matter, because then ferrying is a
walk and only the upload is a fight, which the Hatch already is.

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
