# Missions and destinations

<!--
Purpose: The NAV MAP console, run structure inside a bastion, and having more than one place to go.
Status: brainstorm
Update trigger: when the Run gains structure beyond Waves, or when a second destination exists.
Last updated: 2026-08-13
-->

## What this covers

What a Run **is** — its shape, its objectives, where it happens — as against how hard it
hits. The NAV MAP console, the console restructure, and destinations beyond ALPHA-7.

## Where it stands

One destination, one objective. `Game/Ship/MapStation.cs` embarks; M14 adds a start-Wave
selector. A Run is five Waves against one Hatch and still ends when the Waves are cleared
or the door falls. **M47 is active:** the kit palette (Quarter / Half / Full / Open Area /
Control Room) and the unlock graph exist in Sim; `Bastion.unity` is being authored from
those segments. Runtime terminal / door / wave-on-T is still this file, not M47's gate.
The shipped fight the player can play today is still the one-room Hatch defence; the
composed bastion is the M47 proof, not yet the loop.

## Graduation test

A decision on whether a Run's structure is Waves-in-one-room or objectives-across-areas,
because the two produce different milestones for everything downstream.

**Answered 2026-08-11 (Zo): waves in one room, and the room gets much bigger.** Not
objectives-across-areas — that invalidates the wave director, the siphon dial and the Hatch as
the loss condition all at once. See *The bastion becomes a field of approach* below, which is
the entry this test was waiting for.

## Ideas

### The console restructure
**This is about mission structure**, not about moving the player. Bastions hold N
consoles. Trigger a hack at one, Gatekeepers spawn, clear them, a bulkhead opens to the next
area, repeat to a Master Console that downloads rare resources. The download triggers an
escape Wave where the objective returns to defending the Hatch.

Two consequences fall out rather than being the point: the player advances away from the
Hatch to progress, and gatekeepers spawning in opened areas have much further to travel to
reach it. The player can push deeper or extract early, and after the final area spawns
can come from **any** area already opened — progress becomes exposure.

Start at two consoles. *Whether N consoles reads as pacing or as repetition is unknown.*

### Destinations that differ
More than one bastion, differing in something other than difficulty — layout, hazard,
composition, what the Master Console pays. Needs the console restructure first; two
identical rooms is not a choice.

### Opt-in loot delivery
A second wager beside the siphon dial: make a Run harder for more pay, by choice. Needs
the first wager to be worth taking before a second means anything.

### A reason the bastion is a place
The 48.5 m between breach and Hatch is currently travel time and nothing else. Anything
that makes the floor's geometry matter — cover, chokepoints, a route worth knowing —
would repay the space that already exists.

### The bastion is a field you work, and delivering is what calls the wave

**Zo, 2026-08-11**, refined across two passes. This is one mechanic, not several — the entries
*Opt-in loot delivery*, *Destinations that differ* and *A reason the bastion is a place* above
all fold into it, and it is **`../Decisions.md`'s deferred *Deliver-loot-before-the-run-ends*
graduating**: "a neutral default, and an opt-in that pays more and makes the Run harder." It
has been waiting for a shape since it was deferred.

**Loop.** Exploring, timing and defending become one decision instead of three systems.

## The loop

**Zo, 2026-08-12 — two triggers, two fights.** The 2026-08-11 delivery loop below is the
same job, split so the Hatch is only a defence when a bank is on the line.

1. The bastion is **much larger** than today's 30 × 52 room, with **many spawn points**.
2. **Terminal trigger — accumulate.** A field terminal starts a Wave that hunts the
   Boarder. Kills fill an unbanked pile. The Hatch is not a target and is not vulnerable.
   Clearing the terminal Wave does **not** bank. A cleared terminal set still opens its **D**.
   Intensity is visible as a number, not an exact composition. The player may **pay to
   raise** that intensity (more danger, more Salvage into the pile). No pay-to-skip.
3. The player may pull more terminals before banking. The pile grows. Hatch-banks per Run
   are **limited**, so nibbling every terminal is spending a slot. Greed is carrying more
   unbanked into fewer banks; the convert multiplier is higher when the pile is.
4. **Hatch trigger — bank.** The Hatch starts a Wave whose primary is the Hatch, players
   second. The Hatch is vulnerable only now. Unbanked **drips** into the ship bank for the
   duration. Hatch hits attrit unbanked **and** Hatch HP. Hatch to 0: remaining unbanked
   is wiped, already-dripped stays, the Run ends. Wave clears: remainder dumps to the ship
   (so padding the Wave to farm drip time is useless). Unspent withdrawn rides with
   unbanked. The player may pay to juice this Wave the same way.
5. Repeat until bank slots are spent or the Master Control. Master Control is one
   pull that starts a Wave sequence: terminal-mode Waves fought in the Control Room,
   then the door opens and the remaining Waves are Hatch-bank. Leftover unbanked
   converts on that Hatch-bank half.

## Why each piece is load-bearing

**Greed authors the Hatch-bank Wave and the convert.** Unbanked carried into the Hatch
trigger sizes that Wave and raises the salvage multiplier. A player who risks more banks
more than one who converts often. Hatch-banks per Run are capped so the safe nibble spends
the slots. The cap count is a `4-Data.md` number when this ships, not a rule here.

**The player-set siphon dial is replaced by visible intensity plus pay-to-juice** (Zo,
2026-08-13), PoE 2 shape: more danger, more reward. There is no brick and no exact preview
— an intensity number, not a composition list. Eleven Runs at L0, two at L2: the *free*
stake was not being taken; a paid bump can fail the same way if base intensity is already
the whole fight. Juice attaches to the trigger about to be pulled (terminal, Hatch, or Master Control).
Pay from carried Salvage (unbanked or withdrawn), not from the ship bank sitting on the
Ship. No pay-to-skip on doors.

**The Hatch is only a defence when a bank is on the line.** Terminal Waves do not attack
it. Hatch-sit during accumulate earns nothing. Master Control's first half is that rule;
its second half is the Hatch-bank that converts leftover. The 10% resource-spawn rule
stays **cut**.

**The Hatch-bank is a drip, not a lump** (Zo, 2026-08-13). Unbanked converts to the ship
bank over the Wave. Hatch hits siphon unbanked and Hatch HP. Sloppy defence costs money
without requiring a breach. Breach wipes what is still unbanked; what already dripped is
off the bastion. **Remainder dumps on a successful clear** — otherwise leaving bodies alive
to extend drip time is the dominant play, and a fat pile may not finish in one slot (that
fights fewest-banks). Convert rate is a `4-Data.md` number when this ships.

**Same mix, targeting mode changes.** Defaults switch with the mode. Hunters always the
Boarder. Rams hunt on terminal Waves only (Zo, 2026-08-13 — dated break of “Ram never
hurts you,” that mode only) and Hatch-only on Hatch-bank Waves. See
`gatekeepers-and-waves.md`.

**A main path exists and is chokeable; other spawn points bypass it.** That is the emplacement
decision — you can hold the choke or cover a flank, not both, and the Drone Bay (charges, one
live prop, HP, a gatekeeper-seconds tally) is the primitive that already exists for it and is
currently pointless because one room has nothing to divert anything *around*.

**Exploring and interacting alters spawn points.** `SpawnPoint` keeps a static registry driven
by `OnEnable` / `OnDisable`, so a spawn point reached and shut is nearly free mechanically. Its
**visibility is itself a lever** — known, suspected, or hidden — rather than a binary.

### Layout marks (Zo, 2026-08-11) — space for M47, behaviour later

Sketch: **H** Hatch defend, **S** spawns, **T** terminals, **D** locked doors, Open Areas as hubs,
Full/Half for T/S wings, **Control Room (47) last in unlock order**. Completing terminals in an
open wing opens a **D** and pressures the tied **S**. Spawns behind a lock are nominal for that
wing once live. **Unlock order is authored independently of distance** — Master Control can sit
near the Hatch and stay sealed until late. M47 authors the kit layout and the graph; runtime
terminal / door / wave-on-T is still this entry's loop, not M47's gate.

### Field terminals (Zo, 2026-08-12)

**Reuse the Ship console.** Bastion field terminals are the same authored console presentation
and interact pattern as Ship stations (`StationTerminal` chrome / prefab family) — not Ship
panel content (Armory, Map, etc.). `BastionTerminal` stays the unlock-graph marker; the Ship
console is what the player sees and uses.

**Wing terminals open doors.** Completing the terminal set on a locked **D** is what opens that
door (already the unlock graph). Progress through locked bulkheads is terminal work.

**Control Room has a distinct Master terminal.** It is not another wing T. It is the last
unlock target. One pull starts a Wave sequence (see Settled — Master Control).

**Locked-door terminals start accumulate Waves** (Zo, 2026-08-12). Player-primary, Hatch
not a target. Door opens after that Wave clears and the terminal set for the door is
complete — not a silent unlock on interact alone.

**The Hatch starts bank Waves.** Hatch-primary, players second. This is the only window
the Hatch can be hurt — including Master Control's second half.

## Information is telegraphed, not guaranteed

Paths to the Hatch are shown tower-defense style so pressure can be read before delivery. **They
are a forecast and some things break them, deliberately.** `EnemyGatekeeper.Targeting` sets
`MayTargetHatch` false for Hunters, which exist "to break your way home, not to chase whoever
annoyed it on the way in" — Defaults and Rams honour a drawn line, Hunters do not.

Zo, 2026-08-11: *"I like that things can break that promise. No information is absolute and we
can telegraph to player."* So the display needs a **vocabulary of confidence**, not a single
line style: a committed lane, an uncertain one, a spawn point that is known versus suspected.
That is a HUD system and it does **not** belong on M36 (always-on Hatch integrity only).
Park it until a dedicated telegraph milestone or a later HUD cut.

## What would kill it

Holding the Hatch during accumulate now banks **0** by construction, so the old
explore-vs-hold test would pass even if the field were decoration.

**The test is two exploring Runs:** one that spends Hatch-bank slots on small piles, versus
one that carries into fewer, fatter banks. The few-banks Run must credit more to the ship
account. If they bank the same, the cap and the multiplier did nothing. If the fat-bank
Run always loses the pile, greed is fake. Readable off the end-of-Run report.

**The weaker signal:** median `hatchDist` on terminal-Wave kills is not at the Hatch bay
(M37 / `wave-report`).

**Master Control travel:** if a Run that never leaves the Control Room after the door
opens still converts leftover (Hatch holds without the Boarder), the run-back is fake.
Readable off Hatch HP and `hatchDist` on that sequence's Hatch-bank Waves.

## Settled

**Only a Hatch-bank converts to the ship.** (Zo, 2026-08-13.) Terminal-clear opens the
door and leaves the pile unbanked. During a Hatch-bank, unbanked drips to the ship bank;
hatch hits attrit unbanked and Hatch HP; breach wipes remaining unbanked; clear dumps the
remainder. Unspent withdrawn rides with unbanked. Musical-chairs rotor stays cut; expiry
in `../Decisions.md`. Do not delay `AwardKill` in the one-room fight.

**A down ends the Run.** No in-Run respawn and no in-Run shop. **Ship stations stay open
between Runs** (Zo, 2026-08-13). “No upgrades on respawn” was a rebuttal to in-Run
respawn, not a locked ARMORY. Individual purchases may still gate on progression (Tiers
already do). Deep start restores the unlock graph from the last successful Hatch-bank;
unbanked is empty; bank slots reset.

**Hatch-banks per Run are limited**, and the convert multiplier is higher when the pile is
larger, so few fat banks beat many thin ones (Zo, 2026-08-13).

**Two start verbs, two fights.** Terminal = accumulate. Hatch = bank. Same mix; targeting
mode follows the trigger. Rams hunt on terminal Waves only.

**Visible intensity, pay to raise it, no brick.** PoE 2: more danger, more reward. No
pay-to-skip. Unspent withdrawn is at risk; spent stays.

**Hatch defenses are not a system.** Repair Rig and Hatch Armor remain; auto-repair does not.

**Master Control is a two-phase sequence** (Zo, 2026-08-13). Always in a Control Room.
One pull starts the Waves; the player does not re-pull. First half: terminal-mode, Hatch
not a target, pile still grows, Control Room door shut. That half clearing opens the door.
Second half: Hatch-bank (drip, attrition, dump-on-clear). Travel back and defend. Survive
it: leftover converts, Run ends (win). Breach or a down: remaining unbanked wiped,
already-dripped stays. This Hatch-bank half does not spend a mid-Run bank slot. How many
Waves sit in each half is a `4-Data.md` number when this ships (example: 5 then 5 of 10).
Field terminals reuse Ship consoles; wing T opens D.

**In-wave field resources at 10% — cut.** The terminal is the errand.

## Before it graduates

**M47 playable**, so the two-trigger loop has a floor. **M37**, so `hatchDist` can tell
terminal Waves from Hatch-bank Waves.

**What it touches:** `SceneBuilder` geometry and the NavMesh bake, `SpawnPoint`, `WaveSpawner`,
`WaveDirector` (targeting mode, not mix tables), `TetherSiphon` (replaced by intensity +
pay-to-juice), `RunSim`'s banking path and `EndRun`, `HatchStation` (vulnerable only on
Hatch-bank), `DecoyBay`, the HUD intensity readout, M14 deep start, and the end-of-Run
report. Targeting mode lives in `gatekeepers-and-waves.md`.
