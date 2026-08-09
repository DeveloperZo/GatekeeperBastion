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
