# 1 — Two doors to hold

<!--
Purpose: Zone 1 together. The Hatch is the start line; T2 is the shutoff. Two
hull doors are open. Combat is still one HP pool. The beat that would prove the
hatch rule cannot, until targeting splits.
Status: working
Authority: implementable against 0.34.2
Update trigger: when the routing stamp is overridden, when WaveDirector
player scaling changes, when DemoZones moves
Last updated: 2026-08-30 (faucet spine T2×2 → D1; singular HatchStation;
playerCount multiplier still live; recall pad)
Depends: voice-coop.md, 0-the-invite.md, ../Play Mechanics/The-Faucet-Loop.md
-->

## Why this beat exists

This is the beat the whole demo was meant to rest on: if a party of two can
stand at one door and clear the Wave, there is no co-op game here.

**The layout shipped. The rule did not.** Two (or three) hull doors open with
the seats. Bodies walk to `HatchStation.Instance`. `WaveDirector.NextWave`
still multiplies `enemyCount` by `playerCount`. Per-seat body count did **not**
get replaced by extra doors; both levers are live.

The stamped ordering stays the spec for when targeting splits (counts tunable):

> one Boarder covering both doors < the pressure two open doors take < two Boarders,
> one on each

Until then this beat walks the live zone-1 loop, and the kill criterion cannot fire.

## What `WaveDirector` does today

`NextWave` sets `enemyCount = min(MaxEnemyCount * playerCount, bodies * playerCount)`
(`WaveDirector.cs:260`). `hunterCount`, `thiefCount` and `cloakerCount` are fixed
table rows, then `Min` against that scaled total. Health, group size and spawn
interval never see `playerCount`. Solo (`playerCount == 1`) is byte-identical to
the one-seat table, and that is a test.

## Functional storyboard

Continues from beat 0 step 5. Standing arrives as **Start at the Hatch**.

### 0 — Start at the Hatch

| | |
|---|---|
| **Goal** | Begin zone 1 together. |
| **Player** | Lead presses `T` at the Hatch in Prep. No E-select while Ante is parked. Guests cannot start a zone. |
| **Game** | `demo.coop.first_terminal` · Lock (Prep) · Party · Game. **Two hatch doors open, and they will go for whichever is weaker. They will not split evenly.** |
| **Standing** | Shut off T2 |
| **Prompt** | Hatch start line. Field terminals refuse to start (`CanSelectTerminal` while `HatchOffersWaveStart`). |
| **Log** | existing zone-start / composite line (`zone start zone=1 waves=2 mode=composite`) |
| **Have** | `StartZoneWaveFromHatch` arms T2 and runs the zone-1 composite: **2** Waves, no Prep between (`DemoZones`: `{T2}`, `WavesPerTrigger` 2). |
| **Build** | The line. The "whichever is weaker" sentence describes M-A targeting, which is not live — stamped until it is. |

### 1 — The walk

| | |
|---|---|
| **Goal** | Discover the objective is a place, not a body count. |
| **Player** | Fight up the lane. Authored roster, then trickle (pays nothing). `T` at **T2** in Assault or Prep shuts the zone off. Hold Interact **1.5 s** on **T2 Portal** to recall to the Hatch. |
| **Game** | — |
| **Standing** | Shut off T2 |
| **Prompt** | Terminal shutoff row. Recall pad is the Ship-teleporter graybox beside T2. |
| **Log** | shutoff, door-open, bank-on-switch |
| **Have** | `ShutOffFieldWave` banks the carry, completes T2, opens **D1**, offers dock. Field Waves can hurt the Hatch at `HatchBank.FieldWaveDamageScale` (**0.15**). One HP pool, **600**. |
| **Build** | Target selection: banking door else lowest-HP open door, re-eval on HP change. **Spawn rooms stay untouched.** Hunters already ignore the Hatch. |

### 2 — Clear

| | |
|---|---|
| **Goal** | Bank the lesson before Convert asks for a withdrawn pile. |
| **Player** | T2 is off. D1 is open. Dock is offered until the next Wave starts. |
| **Game** | `demo.coop.after_terminal` · Subtitle · Party · Game. **Wave down. A Convert moves salvage home through those same doors.** |
| **Standing** | Convert at the Hatch — or dock, or walk D1 |
| **Log** | existing shutoff line |
| **Have** | Kills already banked on landing. Convert needs a Withdraw first (`TotalUnbanked > 0`). |
| **Build** | The line. Deepest-entered writes for every seat including guests (`PushProgress`). |

## Acceptance shape

Player action: two players start zone 1 at the Hatch and shut off T2. Reads
back: the composite-start log, the shutoff, D1 open.

M-A's original read-back — target log showing both hatch ids, unattended door
taking damage — waits on targeting.

## Kill criterion

Carried from [`README.md`](README.md). **If both players spend the majority of
the Wave inside the same hatch's defensible area and still clear it, the hatch
rule did nothing the geometry could not, and it reverts to one hatch.** Cannot
fire while targeting is singular. Stamped 2026-08-20, before the work.

## Gaps

- Target selection is **stamped, not ruled**, and **not built**.
- Seat-count body scaling stacks with extra doors. The 2026-08-20 "hatch
  replaces the multiplier" rule never landed.
- Scene closets currently serialize `minSeats: 1`. The mute API exists
  (`BastionSeatSpawns`); S7–S12 `:3` marks are not in the current sketch.
