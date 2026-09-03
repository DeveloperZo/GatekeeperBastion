# M116 — Waves that differ

<!--
Status: active
Update trigger: when a lever lands or is cut, and after the adjudicating play session
Last updated: 2026-09-02
-->

Three levers against one finding: "the play felt static. No felt variance in waves."
Each lever is one commit and holds the roster, the HP ladder, the group width, the debut
gates and the one-lane spine still; variance redistributes what a wave is, never how much
of it there is. Spawn direction was a fourth lever and is cut (`Decisions.md`, Cut).

## Story

As a Boarder, I want the second wave to be a different fight from the first and the finale
to have a shape I can read, so that a Boarding is a sequence of decisions rather than the
same decision at a growing size.

## Why it matters

The 2026-09-02 stripped run: 308 kills from six closets in one fixed rotation at one
cadence, kill distances at a median 55 m for zone 1, the finale and the trickle alike, and
five identical wave-5 rosters making up 57% of the run (`playthrough.md`, claim 5). Nothing
in the tables varies between waves except count and HP.

Precedent (two research passes, 2026-09-02): every ancestor draws intervals from a window,
never a value (Left 4 Dead mobs at 90–180 s; Vermintide bursts then a 70–150 s lull); a wave
is a named type announced before its first body (Deep Rock's swarms, Killing Floor's squads,
Sanctum's printed lineup); a finale is staged beats with a readable dip (Valve's own finale
script: two hordes, a delay, the tank). The brainstorm already parks a draw rule for shapes;
lever 2 is that rule's first cut and graduates it.

Leverage: **#9 delays** (lever 1), **#5 rules** (levers 2 and 3). Higher tiers are wrong
because the loop's verbs and the roster are locked; lower tiers are wrong because no constant
makes five identical rosters into a sequence.

## Acceptance criteria

1. **The beat.** `WaveSpec.spawnInterval` is a mean; `WaveCadence.Schedule` turns it into a
   seeded sequence of paired bursts (`0.25–0.45 ×` the mean) and repaying lulls whose sum is
   the unchanged window, attached at `RunSim.LaunchWave` from the sim's `Rng`, and
   `WaveSpawner` waits the scheduled gap. From the log's `group k … breached at` timestamps:
   per-wave window within ±10% of 21.9 / 30.8 / 32.4 / 42.5 / 44.5 s, inter-group gap
   coefficient of variation above 0.4, and peak `N live` at least 1.5× the metronome's.
2. **Shapes.** A shape table replaces the per-wave kind arrays: Mass (all Default), Pack
   (Hunters, Cloakers from wave 4), Convoy (Thieves and Cloakers), shares of the unchanged
   `11 + 7i` count with Default absorbing the remainder, debut gates kept. A zone's opening
   draws Mass then Pack; its terminal-called last wave draws Convoy (zone 1: Pack, since the
   Thief debuts on wave 3). Spawn groups are authored by kind. The terminal prompt names the
   last wave's shape before `T`. From the log: Convoy Thief kills median closer to the Hatch
   than Pack Hunter kills, and the door's remaining pool on `wave N/5 cleared` falls on Convoy and not on Pack.
3. **The finale.** `MasterControl.TerminalWaves` 5 → 2 (Mass, then Pack at wave-5 size), a
   quiet beat of `MasterControl.FinaleBeatSeconds` before the elite, then the elite with an
   escort of four Hunters and two Cloakers, invulnerable until the escort is down, the
   trickle off, then the switch. The elite is named on the Hatch press. From `[Run] summary`:
   the finale's share of a winning run at or under 30%; escort kill lines before the elite's.
4. `4-Data.md` carries each number and its derivation. The three levers were meant to be
   three commits; they were built into shared files (`WaveSpawner`, `GameController.Terminals`,
   `4-Data.md`) in one working tree on 2026-09-02 and landed in one commit with `M115` — the
   attribution lives in this file and in `Decisions.md`, not in `git log`.

## Kill criterion

Pre-registered 2026-09-02. **The bet:** rhythm and shape are what "static" was missing.
**Kill, per lever:** if the lever's log falsifier in its criterion fails across three
untainted Boardings after it lands, the lever is reverted, not retuned — a variance the log
cannot see is a variance the player cannot feel. Feel is not a kill signal; the numbers are.
Date: three Boardings or **2026-09-21**.

## Pre-mortem

- Bursts stack the lane past what one rifle can answer and the wave-5 wall returns at T5.
  The window is unchanged, so the mean pressure is; the tell is peak live above 40 with the
  Hatch untouched — a rhythm problem, not a roster one, and `BurstMin` is the dial.
- Shapes read as more of the same because the kinds are not legible at the distance they
  arrive at. `M109_` restored the size tell; the terminal prompt names the shape. If the
  Convoy's `hatchDist` cluster still matches the Pack's, the shape did not land, whatever
  the table says.
- Two waves plus an escorted elite reads as a shorter grind rather than a finale. The quiet
  beat is the difference; if a log shows the elite drawn inside 5 s of the second wave's last
  kill, the beat is not landing.

## Implementation

**Create** `Assets/Scripts/Sim/Waves/WaveCadence.cs`, `Assets/Tests/EditMode/Waves/WaveCadenceTests.cs`.
**Change** `WaveSpec` (`groupGaps`), `RunSim.LaunchWave`, `WaveSpawner.Groups.cs`.
**Change** `WaveDirector` (shape table replaces the kind arrays), `WaveSpec`, the terminal
prompt, `DemoZones` (which shape each zone's opening and last wave draw).
**Change** `MasterControl` (`TerminalWaves`, `FinaleBeatSeconds`, escort table),
`RunSim.Master.cs` (the beat, the escort gate), `WaveSpawner` (elite invulnerable until escort down).
**Change** `Docs/Design/4-Data.md`, `Docs/Design/0-Gameplay.md`, `Docs/Decisions.md`.

Rejected: a spawn-direction lever (Zo, cut); a Left 4 Dead style intensity director
(amplitude changes are the roster's lever, and the loop refuses an adaptive pace — the
player starts the action).

## Verification

1. Criterion 1: `WaveCadenceTests` offline; the three log figures from one Boarding.
2. Criterion 2: shape tests offline (shares, gates, groups by kind); the two `hatchDist`
   distributions from one Boarding through zone 2.
3. Criterion 3: finale tests offline; one stripped run's `[Run] summary` and kill order.
4. Playthrough path — EMBARK, `T` at the Hatch, watch the second group land 2–3 s behind the
   first and the third go quiet; read the shape on the terminal prompt; Master Control ends
   inside a third of the run.

## Do not build

- A closet plan, an ahead/behind split, or any spawn placed by the player's position.
- A dial on any of it. The seed varies the beat; nothing the player buys does.

## Depends on / Unblocks

Depends on: `M115` (the last wave at the terminal is where Convoy lands). Unblocks: M112's
wave-5 and door readings, which now have a fight worth adjudicating.
