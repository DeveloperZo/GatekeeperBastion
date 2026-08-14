# M54 — Master Control is the win

<!--
Status: completed 2026-08-13
Update trigger: closed
Last updated: 2026-08-13
Closed on: Zo 2026-08-13. Playthrough Zo. Gate: `Tools/verify/run.sh` 324/0;
Unity EditMode `MasterControlTests` 8/8; PlayMode 30/1
(`NetVerbTests.Repair_ClientChannel_SpendsHostWalletAndStops` 229.5 vs 229.0 ± 0.05,
not this slice). EditMode `BastionDoorApertureTests.TryGetDoorAperture_NorthDoor_AtHalfDepth`
is the same miss as M50/M51. Scene: `T_Master` in Control Room, `S_Control` spawn;
seal mutes layout spawns.
-->

Clearing Wave 5 is no longer the win. **Master Control, in a Control Room, is one pull
that starts a Wave sequence: terminal-mode Waves with the door shut, then the door
opens and the rest are Hatch-bank.** Survive that Hatch-bank half: leftover unbanked
converts, Run `Escaped`. This Hatch-bank does not spend a mid-Run bank slot.

**Recut 2026-08-13 (review):** `BastionTerminalKind.Master` is the flag (`IsMaster` is
derived). M53 already keeps Wave 5 with Unbanked > 0 in Prep; Unbanked 0 on that clear
still `Escaped` — this slice removes that remaining path. Terminal `TriggerDraw` past
`MaxWaves` already clamps. Sequence gaps are `InterWaveGap` of Prep after each clear
(five clears, not deep-start overlap). Hatch-bank windows in the second half use
`HatchBank.DurationSeconds`; dump remainder only when the half completes. Convert
multiplier snapshotted at Hatch-half start. Control Room has no Master terminal yet —
author one. Deep-start chain does not fire on the Master pull. SaveVersion **7**.

## Story

As a Boarder, I want using Master Control to lock me into a last fight and then send me
home to the Hatch, so that winning is getting the pile off the bastion, not walking into
a room.

## Why it matters

M53 ends a Run only on Wave 5 terminal with Unbanked 0. Leftover Unbanked stays and
converts on Hatch-bank. This milestone is Master Control's two-phase sequence
(`../../Brainstorms/missions-and-destinations.md`, Zo 2026-08-13). Without it,
the Control Room is another **T**.

Leverage tier: **#9, rules**. Sequence split is **#12 constants** (5 then 5 of 10),
tunable after close.

## Kill criterion

By **2026-09-13**: a Run that never leaves the Control Room after the door opens still
converts leftover (Hatch holds without the Boarder) **kills the travel.** Readable off
Hatch HP and `hatchDist` on that sequence's Hatch-bank Waves.

## Acceptance criteria

1. **Wave 5 clear does not `Escaped`, including Unbanked 0.** `TriggerDraw` in Terminal
   mode may continue past `WaveDirector.MaxWaves`; composition clamps at wave index
   `MaxWaves - 1`. Hatch-bank slots (M53) still cap extracts. Loss is still down or
   Hatch-bank breach.
2. **Master Control (`BastionTerminalKind.Master`) Interact in Prep starts the
   sequence.** One pull; the player does not re-pull. Log: `[Terminal] master-control
   started terminalWaves=<n> hatchWaves=<n>`. A wing **T** is unchanged. Pulling Master
   with a sequence already live: logged refusal.
3. **First half:** `MasterControl.TerminalWaves = 5` Waves, `WaveMode.Terminal`, Control
   Room door (`OpensControlRoom`) `SetOpen(false)` for the half. Hatch not a target.
   Unbanked still grows. After each clear, `WaveDirector.InterWaveGap` of Prep then the
   next draw (no untimed Prep). Deep-start overlap does not run on this pull.
4. **On that half clearing:** door `SetOpen(true)`. **Second half starts after
   InterWaveGap** (no untimed Prep in the Control Room): `MasterControl.HatchWaves = 5`
   Hatch-bank windows (`DurationSeconds`, drip / attrition as M53). Convert multiplier
   snapshotted from Unbanked at Hatch-half start and held. **Does not decrement
   `HatchBankSlotsLeft`.** Unbanked 0 is allowed. Intermediate window ends do not dump.
5. **Second half clear:** dump remainder, `Escaped`. Breach or down: remaining unbanked
   wiped, dripped stays, `Lost`.
6. **Deep start** (`StartWave > 1`) restores the unlock graph (completed terminals, open
   doors) from the last **successful Hatch-bank** of the previous Run (mid-Run or Master
   Control's second half). Wave-1 start does not restore. Unbanked starts at 0; bank
   slots reset to 3. Persist on `MetaState` / save (version 7, one migration path).
7. EditMode `MasterControlTests`: 5 terminal clears → Hatch half → 5 Hatch-bank without
   spending a slot → `Escaped` with Unbanked 0 and Banked increased; abort on hatch-0
   during second half keeps dripped. Graph restore from saved ids. PlayMode green.
   Prompt on the Master terminal: `[E] Master Control`.

## Implementation

**Create** `Assets/Scripts/Sim/Run/MasterControl.cs` — `TerminalWaves = 5`,
`HatchWaves = 5`, `MasterControlPhase` (`None`, `Terminal`, `Hatch`). Sequence
state lives on `RunSim` (phase, remaining counts), not a MonoBehaviour.

**Change** `RunSim.NotifyAssaultCleared` — never `CompleteRun` on wave 5. If a Master
sequence is live, decrement the current half and either arm the next Wave at
`InterWaveGap`, flip to the Hatch half (snapshot convert multiplier, raise
`MasterHatchHalfStarted`), or dump and `EndRun` cleared. `TriggerDraw` past `MaxWaves`
stays allowed. Hatch-bank composition clamps at `MaxWaves` when `CurrentWaveIndex` is
past it. Sequence Hatch-bank skips the slot spend and the Unbanked-0 refusal.

**Change** `GameController` / `BastionTerminal` — `Kind == Master` Interact starts
`Sim.StartMasterControl()`. Log the criterion-2 line from Game. Seal/open the Control
Room door via `BastionLayout.SetControlRoomSealed` so `RefreshDoorStates` cannot reopen
it mid-half. After seal, enable the Master terminal's UnlockedSpawns (D3 sealedSpawns
would otherwise go dark). On Hatch half: unseal, `ActivateHatchBankSpawns`. Snapshot
completed terminal ids onto `MetaState` on every successful Hatch-bank clear. Deep start:
`BastionLayout.RestoreCompletedTerminals` on EngageTether when `StartWave > 1`.

**Change** `Assets/Scenes/Bastion.unity` — author a `BastionTerminalKind.Master` console
in the Control Room (`T_Master`), GameController wired, UnlockedSpawns `S_Control`
(Control Room; `S_S4` is EastStub and cannot feed a sealed-room fight).
Wing **T** kinds stay Field.

**Change** `MetaState` + `SaveData` — `CompletedTerminalIds` string array. Version **7**,
one migration in `FromSaveData` (missing / pre-v7 → empty). Do not add a second
migration path (M42). Bump the future-version fixture in `RunSimTests` off 7.

**Change** `BastionUnlockGraph` — `CopyCompletedTerminalIds` (sorted) and
`RestoreCompletedTerminals`.

**Change** `language.md` — **Master Control** row. **cleared** / **stripped** /
**Escaped** means survived Master Control's Hatch-bank half, not “Wave 5 done.”
**Change** `Docs/Play/A-Run.md` and `Getting-Started.md` to match.
`4-Data.md` GEN block for the 5+5 split (`Tools/docgen` emitter `master-control`).

Rejected: converting on Interact with no fight. Rejected: winning while leaving unbanked
on the table. Rejected: spending a mid-Run slot on the finale (greed Runs could not
extract). Rejected: untimed Prep between halves (the travel is the window). Rejected:
deep-start overlap for the five terminal Waves (tests need five clears; M52 already
rejected stacking T1).

## Supporting work

- **Driven by** Interact at Master Control — here.
- **Shown by** the Master prompt and the Control Room door sealing — here.
- **Reads back as** the mode flip, the door opening, Banked rising on the Hatch-bank
  half, then the win screen — here.

## Verification

1. `MasterControlTests` — criteria 3–6.
2. `Tools/verify/run.sh` + integration check; `DeepStart_ClearingWaveFive_Escapes`
   becomes stay-Prep; `RunCompleteTests` Escaped fixtures go through Master Control.
3. Playthrough — open the Control Room, pull Master Control, fight the room, door
   opens, run back, defend, win. A second attempt: sit in the Control Room for the
   second half and read Hatch HP / `hatchDist` (kill criterion, not the gate).

## Do not build

| Not building | Why not |
|---|---|
| Juice on the Master pull | M55; until then the siphon dial still applies |
| Path telegraph | Parked |
| Second destination | Stays brainstorm |
| In-Run respawn | Settled cut |
| Further Control Room spawn layout | `S_Control` is this slice; more points wait |

## Watch

- Composition clamp at Wave 5 for extra terminal Waves will feel like the curve stopped.
  That is this slice; a later curve extension is a different milestone.
- Sequence chain uses the same `InterWaveGap` as deep start (3 s). Do not invent a second
  gap constant. It is timed Prep after a clear, not delivery overlap.
- `Kind` is authored; a wing **T** must not be `Master`.
- Sealing D3 via `SetOpen(false)` deactivates its `sealedSpawns`. `SetControlRoomSealed`
  also mutes every spawn under the layout; re-enable Master UnlockedSpawns after the seal.
  `S_S4` is EastStub, not in the Control Room.

## Depends on / Unblocks

Depends on M52 and M53 (both completed). Unblocks M55 juice on the Master pull.
Destinations still wait on this destination's loop being playable.
