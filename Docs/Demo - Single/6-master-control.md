# 6 — Master Control (Control Room fight)

<!--
Purpose: Master Control is a fight inside a sealed Control Room. It is not the
win. Juice stays live (taught in beat 1). Send them to the Hatch in beat 7 when
the door opens.
Status: demo spec
Authority: implementable
Update trigger: when MasterControl.TerminalWaves changes, when the Master fight
spawn rooms land (M73)
Last updated: 2026-08-17
Depends: voice.md, 5-repeat-to-control-room.md, M54 (in tree), M73 (SpawnRoom
prefabs, active — Master fight spawn rooms)
-->

## Why this beat exists

`M54` already runs 5 terminal-mode Waves then the Hatch-bank half (in tree as 5
windows; ruled to a **single final window** — Zo, 2026-08-17; the retune is beat
7's). Live prompt is `[+/-] Master Control` with the full juice/stake line.
Nothing says the room fight is not the haul home. A Boarder who stays in R15
through the Hatch half is the travel kill already on `M54`.

This beat is copy and a seal line. It does **not** add a third win and does
**not** retune `MasterControl.TerminalWaves = 5`. Do not hide juice.

## Functional storyboard

Continues from beat 5: D3 opens when T4 and T5 are done. Standing arrives as
**Enter the Control Room**.

### 0 — D3 opens (merged line)

| | |
|---|---|
| **Goal** | Know the room seals and is not the way home. |
| **Player** | Walk through D3 into R15. |
| **Game** | `demo.control_room` · Lock (Prep) · Party · Game. **This room seals when you pull Master Control. The fight is in here. It is not the way home.** Plays **on D3 open**, replacing `demo.door_d3` — one conversation, not two. |
| **Standing** | Pull Master Control |
| **Log** | `[Demo] line id=demo.control_room …` |
| **Have** | D3 `OpensControlRoom` from sketch mark `control`; graph AND T4+T5. |
| **Build** | The merge rule in the demo runner: if `demo.door_d3` and `demo.control_room` would both fire, play only the latter. |

### 1 — Look at Master, arm, pull

| | |
|---|---|
| **Goal** | Start the sequence deliberately, with the bet available. |
| **Player** | Look at Master (sketch mark `M`, component id `Master`): `demo.master_control` Lock. May juice first (Unbanked from the Run). `E` arms, `T` starts. |
| **Game** | `demo.master_control` · Lock · Party · Game. **Master Control. Five Waves in a sealed room, then the door opens. E, then T, when you are ready.** |
| **Standing** | Survive the room |
| **Prompt** | Live: unarmed `[+/-] Master Control` + stake + ` [E] select`; armed `[T] Master Control` + ` [E] edit`. Keep both — juice on the Master prompt is correct. |
| **Log** | `[Terminal] master-control started terminalWaves=5 hatchWaves=1 intensity=… juiced=…` (in tree; `hatchWaves` reads 1 after beat 7's `HatchWaves` retune — Zo, 2026-08-17) plus `[Demo] master-control room fight` (build) |
| **Have** | `E` then `T` (live; `M54` c2's "Interact starts" is superseded). Host-only start, Prep-only, armed id. Seal: `BastionLayout.SetControlRoomSealed(true)` — forces the control door shut and **mutes layout spawns**. Deep-start chain cleared on this pull. `CurrentWaveIndex` is not zeroed: composition clamps at the Wave-5 table (66 bodies, 12 Hunters, 7 Thieves, ×2.60, ×`playerCount`). |
| **Build** | The two lines; the log. |

### 2 — Five room Waves (Assault ×5, 3 s gaps)

| | |
|---|---|
| **Goal** | Survive `WaveMode.Terminal` inside the room. The Hatch is not a target and takes no damage. Unbanked still grows. |
| **Player** | Fight. Gaps are `InterWaveGap` (3 s) — auto-chained, not untimed Prep. `G`/`H` live if the Default was claimed (beat 4). |
| **Game** | — no locking line during the five Assaults; subtitles would fight the room. |
| **Standing** | Survive the room (k/5) |
| **Log** | existing wave start/clear lines |
| **Have** | `StartMasterControl()`: phase Terminal, `_masterLeft = 5`, auto `TriggerDraw` between. `DamageHatch` is a no-op in Terminal mode. |
| **Build** | Nothing mechanical. Room fight walks out of `S_ControlW` / `S_ControlE` (`T_Master` UnlockedSpawns) — spawn rooms are authored with `M73`'s SpawnRoom prefab work (Zo, 2026-08-17). |

### 3 — Room half ends (hand-off to beat 7)

| | |
|---|---|
| **Goal** | The flip to the Hatch half is beat 7's cue, not a win. |
| **Player** | Fifth clear → `MasterHatchHalfStarted`: door unseals, Hatch-bank spawns activate. |
| **Game** | beat 7's `demo.hatch_finale` (Subtitle) plays here. |
| **Standing** | Defend the Hatch (beat 7) |
| **Log** | `master-control hatch half — hatchWaves=1` (after beat 7's retune) — **verbose-only** (`Log.Info`); the event, door state, and beat 7's send log are the always-on evidence |
| **Have** | `AdvanceMasterControl`: after 5 Terminal clears → phase Hatch, `_masterLeft = HatchWaves` (1 after beat 7's retune — Zo, 2026-08-17), convert mul snapshotted, event raised; `SetControlRoomSealed(false)` + `ActivateHatchBankSpawns()`. |
| **Build** | Nothing here. |

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 6 |
|---|---|---|---|
| E-then-T Master start, host, Prep | yes | `GameController.MasterControl` | Have |
| Seal (door + mute spawns) | yes | `BastionLayout.SetControlRoomSealed` | Have |
| 5+5 sequence, 3 s gaps | yes | `MasterControl.TerminalWaves/HatchWaves`, `RunSim` | Have — `HatchWaves` retunes 5 → 1 in beat 7 (Zo, 2026-08-17) |
| Hatch untouchable in room half | yes | `DamageHatch` no-op in Terminal mode | Have |
| Juice/stake on Master prompt | yes | `BastionTerminal` Master branch | Have — a demo hide is a defect |
| Master fight spawns in the kit | planned (Zo, 2026-08-17) | sketch marks `S:ControlW:W` `S:ControlE:E` on R15; author with `M73`'s SpawnRoom prefab work; wire `T_Master` UnlockedSpawns | Build |
| Voice ids / merge rule / demo log | no | — | Build |

## Change

| File | Change |
|---|---|
| `BastionTerminal` Master `Prompt` | Unchanged live juice/stake |
| `GameController` Master start | Add `[Demo] master-control room fight` + voice on look-at |
| Door D3 open | `demo.control_room` instead of `demo.door_d3` |
| Sketch / emitter | Control Room closets on R15 west/east, wired to Master — authored with `M73`'s SpawnRoom prefab work (Zo, 2026-08-17) |

## Delete

Nothing. Do not delete the Hatch half. Do not delete the travel-kill criterion.

## Numbers

| Name | Value | Source |
|---|---|---|
| Terminal half | 5 Waves | `MasterControl.TerminalWaves` |
| Gap | 3 s | `WaveDirector.InterWaveGap` |
| Clamp composition | Wave-5 table: 66 bodies, 12 Hunters, 7 Thieves, ×2.60 | index `MaxWaves−1` (`MaxWaves` is a per-trigger cap, not per-Run — Zo, 2026-08-17) |
| Juice | live | beat 1 teach |

Do not retune for the demo. If 5 room Waves is too long for a first session,
that is a later `#12` change with play data.

## Multiplayer

| Event | Authority |
|---|---|
| Master `E` | Host sim, any seat at the terminal (live). One sequence |
| Seal | Host door, all seats |
| Voice | Party Lock Prep |
| Guest down during room half | `M57` wait. Host continues. No buy-back here — buy-back needs a live bank window (beat 7; Zo, 2026-08-17) |
| Scale | `playerCount` on each Wave |

If the lead never enters R15 and a guest pulls Master: allowed (live). Voice
still Party.

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.control_room` | Lock (Prep) | Party | D3 open (replaces `demo.door_d3`) | Pull Master Control |
| `demo.master_control` | Lock | Party | first Master look-at this Run | Survive the room |

Assault during the 5 Waves: no lines.

## Driven / Shown / Reads-back

- **Driven by** `E` then `T` at Master Control (`M54`).
- **Shown** door shut; Hatch not taking damage; Unbanked rising; juice/stake on
  the Master prompt.
- **Reads-back** `[Terminal] master-control started …` (in tree),
  `[Demo] master-control room fight` (build).

## Logs

| Event | Line | Status |
|---|---|---|
| Room copy | `[Demo] master-control room fight` | build |
| Sequence | `[Terminal] master-control started terminalWaves=5 hatchWaves=1 …` | in tree (`hatchWaves` reads 1 after beat 7's retune) |
| Half flip | `master-control hatch half — hatchWaves=1` | in tree, **verbose-only** — do not gate a test on it |
| Juice | spend line from beat 1's build | build (beat 1) |

## Traps

- Adding `Escaped` at the end of the Terminal half. Forbidden.
- Starting the Hatch half without unsealing (`M54` c4). Do not recut.
- Hiding juice on the Master prompt. Taught in beat 1; live is correct.
- Locking input during the 5 Assaults.
- Gating verification on the half-flip log — it is `Log.Info`. Use the event /
  door state.
- Cutting the milestone before the room fight's spawn rooms are in scene. They
  are planned authoring with `M73`'s SpawnRoom prefab work (Zo, 2026-08-17) —
  resolved as planned work, not open design. Verify wired before cutting.
- `timeScale = 0` "cinematic" seal.
- Spending a mid-Run Hatch-bank slot on this half (Terminal mode does not).

## Depends / Unblocks

Depends: D3 open (beat 5), `M54` in tree, Master fight spawn rooms authored
(planned with `M73`'s SpawnRoom prefab work — Zo, 2026-08-17).
Unblocks: beat 7 on `MasterHatchHalfStarted`.

## Verification

1. `MasterControlTests` still green (5 Terminal then the single Hatch window —
   `HatchWaves` 1, beat 7's retune; no slot spend on the Hatch window, Escaped
   only after it).
2. Master prompt still contains juice/Intensity. A demo hide is a defect.
3. Log `master-control room fight` then the existing started line.
4. Hatch HP unchanged during the Terminal half (regression).
5. Kit scene: five room Waves actually spawn inside/at R15 (spawn rooms from
   `M73`'s SpawnRoom prefab work in scene and wired).
6. Loopback: both seats see the seal; one Master pull.
7. No `[Dev]`.

## Rejected

A skip-Master debug that still Escapes. Reducing 5 to 1 without play data.
Companion in the room. FAB gift before the room.
