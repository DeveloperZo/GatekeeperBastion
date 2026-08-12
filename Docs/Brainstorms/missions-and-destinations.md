# Missions and destinations

<!--
Purpose: The NAV MAP console, run structure inside a bastion, and having more than one place to go.
Status: brainstorm
Update trigger: when the Run gains structure beyond Waves, or when a second destination exists.
Last updated: 2026-08-04
-->

## What this covers

What a Run **is** — its shape, its objectives, where it happens — as against how hard it
hits. The NAV MAP console, the console restructure, and destinations beyond ALPHA-7.

## Where it stands

One destination, one room, one objective. `Game/Ship/MapStation.cs` embarks; M14 adds a
start-Wave selector. A Run is five Waves of gatekeepers against one Hatch, in a 30 x 52 m
floor with three spawn points, and it ends when the Waves are cleared or the door falls.

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

1. The bastion is **much larger** than today's 30 × 52 room, with **many spawn points**.
2. The player explores and collects. **Nothing banks in the field.** Salvage accumulates at the
   Hatch.
3. **Gatekeepers reaching the Hatch take salvage as well as integrity** — and, later, other
   things. Hoarding is not safe; the vault leaks.
4. The player **triggers delivery** when ready. That is what signals the bastion.
5. Waves come from the spawn points. The player defends.
6. Waves end, the delivery banks to the ship, **more of the bastion opens**, and it repeats
   until the Master Control.

## Why each piece is load-bearing

**Delivering calls the wave, so greed authors the difficulty.** The natural coupling is the
amount delivered sizing the wave: the player writes their own curve, and `MaxWaves` becomes
emergent rather than the constant 5 with its fixed 18 / 30 / 42 / 54 / 66. The reference is
surfacing with treasure in *Depth*.

**This replaces the siphon dial rather than sitting beside it.** *Opt-in loot delivery* above
says a second wager "needs the first wager to be worth taking before a second means anything."
**Measured across every session log on disk: eleven Runs at siphon L0, two at L2.** The first
wager is not being taken. Two dials nobody turns is worse than one the player cannot avoid
making — the dial's multiplier becomes the delivery's rate.

**The Hatch holding losable salvage is what answers "why explore".** Not a destination out in
the field — a *stake* back home. Staying put does not protect it, because arrivals drain it.
That is the difference between a game that punishes leaving and one that prices it.

**Resources appear only once a wave is sufficiently spawned.** *(Zo's, and the sharpest piece
here.)* The standard tower-defense failure is that leaving your emplacement is pure downside, so
the correct play is always to stand still. Spawning field resources **during** the wave inverts
it: moving away is an opportunity rather than a penalty, and it cannot be farmed before pulling
the trigger. **"Sufficiently spawned" needs a definition** — share of the wave emitted, or first
group landed — or it is exploitable by delivering and immediately running.

**A main path exists and is chokeable; other spawn points bypass it.** That is the emplacement
decision — you can hold the choke or cover a flank, not both, and the Drone Bay (charges, one
live prop, HP, a gatekeeper-seconds tally) is the primitive that already exists for it and is
currently pointless because one room has nothing to divert anything *around*.

**Exploring and interacting alters spawn points.** `SpawnPoint` keeps a static registry driven
by `OnEnable` / `OnDisable`, so a spawn point reached and shut is nearly free mechanically. Its
**visibility is itself a lever** — known, suspected, or hidden — rather than a binary.

### Layout marks (Zo, 2026-08-11) — space for M47, behaviour later

Sketch: **H** Hatch defend, **S** spawns, **T** terminals, **D** locked doors, Open Areas as hubs,
Full/Half for T/S wings, **Control Room (76) last in unlock order**. Completing terminals in an
open wing opens a **D** and pressures the tied **S**. Spawns behind a lock are nominal for that
wing once live. **Unlock order is authored independently of distance** — Master Control can sit
near the Hatch and stay sealed until late. M47 authors the kit layout and the graph; runtime
terminal / door / wave-on-T is still this entry's loop, not M47's gate.

## Information is telegraphed, not guaranteed

Paths to the Hatch are shown tower-defense style so pressure can be read before delivery. **They
are a forecast and some things break them, deliberately.** `EnemyGatekeeper.Targeting` sets
`MayTargetHatch` false for Hunters, which exist "to break your way home, not to chase whoever
annoyed it on the way in" — Defaults and Rams honour a drawn line, Hunters do not.

Zo, 2026-08-11: *"I like that things can break that promise. No information is absolute and we
can telegraph to player."* So the display needs a **vocabulary of confidence**, not a single
line style: a committed lane, an uncertain one, a spawn point that is known versus suspected.
That is a HUD system and it belongs with **M36**.

## What would kill it

**Framed as a gradient, not a gate** (Zo, 2026-08-11). Holding the Hatch stays a valid way to
play. It should simply pay less.

**The test is two cleared Runs, deliberately different: one that explores, one that holds the
Hatch bay. The exploring Run banks materially more salvage.** If the two bank the same, the
field bought nothing and the expanded space is decoration. This is the same shape as M37's
two-Run comparison, and it can be read straight off the end-of-Run report.

Note what this does *not* say: it does not require the holding Run to fail. A player who wants
to turtle is making a lower-yield choice, not a wrong one, which is the same reasoning as
spawning field resources during the wave rather than penalising the player for staying.

**The weaker second signal:** the drawn paths do not change where the player stands.

## Settled

**Death costs salvage and the player respawns.** One currency of failure: arrivals drain the
Hatch, dying costs salvage. No drop-and-recover, no second loss model.

**The Master Control ends the Run.** A bastion is a Run; the nav map holds many. Whether a
bastion ever has more than one is not a question this entry needs answered.

**Wave sizing is not decided here.** It is a number, and numbers are the weakest place to
intervene — it gets set when there is something to tune against.

**Field resources appear once 10% of the wave has emitted** (Zo, 2026-08-11). At today's wave
sizes that is two to seven gatekeepers on the floor — early, and deliberately so: the player
leaves while pressure is still building rather than after it resolves. It also closes the
deliver-then-immediately-run exploit, since the trigger is emission and not the clock.

**Nothing blocks the build.** The entry is complete against the contract in
`../_meta/_definitions/brainstorm.md`.

## Before it graduates

**M37**, so "where they died" is measurable before and after. A field of approach that changed
nothing would otherwise be indistinguishable from one that worked.

**What it touches:** `SceneBuilder` geometry and the NavMesh bake, `SpawnPoint`, `WaveSpawner`,
`WaveDirector` composition and `MaxWaves`, `TetherSiphon` and the dial, `RunSim`'s banking path,
`HatchStation`, `DecoyBay` / `DecoyState`, the HUD, and the end-of-Run report. Pairs with **A
gatekeeper you must leave the Hatch for** in `gatekeepers-and-waves.md`, which is the same move
from the enemy side.
