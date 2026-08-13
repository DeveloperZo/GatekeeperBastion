# M54 — Master Control is the win

<!--
Status: later
Update trigger: when the two-phase sequence, door-seal, or leftover-convert rule changes
Last updated: 2026-08-13
-->

Clearing Wave 5 is no longer the win. **Master Control, in a Control Room, is one pull
that starts a Wave sequence: terminal-mode Waves with the door shut, then the door
opens and the rest are Hatch-bank.** Survive that Hatch-bank half: leftover unbanked
converts, Run `Escaped`. This Hatch-bank does not spend a mid-Run bank slot.

## Story

As a Boarder, I want using Master Control to lock me into a last fight and then send me
home to the Hatch, so that winning is getting the pile off the bastion, not walking into
a room.

## Why it matters

M14 ends a Run on the fifth `NotifyAssaultCleared`. M53 keeps leftover Unbanked after
Wave 5 and converts it on Hatch-bank. This milestone is Master Control's two-phase
sequence (`../../Brainstorms/missions-and-destinations.md`, Zo 2026-08-13). Without it,
the Control Room is another **T**.

Leverage tier: **#9, rules**. Sequence split is **#12 constants** (5 then 5 of 10),
tunable after close.

## Kill criterion

A Run that never leaves the Control Room after the door opens still converts leftover
(Hatch holds without the Boarder) **kills the travel.** Readable off Hatch HP and
`hatchDist` on that sequence's Hatch-bank Waves.

## Acceptance criteria

1. **Wave 5 clear does not `Escaped`.** `TriggerDraw` in Terminal mode may continue past
   `WaveDirector.MaxWaves`; composition clamps at wave index `MaxWaves - 1`. Hatch-bank
   slots (M53) still cap extracts. Loss is still down or Hatch-bank breach.
2. **Master Control (`BastionTerminal.isMaster`) Interact in Prep starts the sequence.**
   One pull; the player does not re-pull. Log: `[Terminal] master-control started
   terminalWaves=<n> hatchWaves=<n>`. A wing **T** is unchanged. Pulling Master with a
   sequence already live: logged refusal.
3. **First half:** `MasterControl.TerminalWaves = 5` Waves, `WaveMode.Terminal`, Control
   Room door (`OpensControlRoom`) `SetOpen(false)` for the half. Hatch not a target.
   Unbanked still grows. Chain with `WaveDirector.InterWaveGap` (no untimed Prep).
4. **On that half clearing:** door `SetOpen(true)`. **Second half starts immediately**
   (gap only, no Prep you can spend in the Control Room): `MasterControl.HatchWaves = 5`,
   `WaveMode.HatchBank`, drip / attrition / dump-on-clear as M53. **Does not decrement
   `HatchBankSlotsLeft`.**
5. **Second half clear:** dump remainder, `Escaped`. Breach or down: remaining unbanked
   wiped, dripped stays, `Lost`.
6. **Deep start** restores the unlock graph (completed terminals, open doors) from the
   last **successful Hatch-bank** of the previous Run (mid-Run or Master Control's second
   half). Unbanked starts at 0; bank slots reset to 3. Persist on `MetaState` / save
   (version bump, one migration path).
7. EditMode `MasterControlTests`: seal → 5 terminal clears → door open → 5 Hatch-bank
   without spending a slot → `Escaped` with Unbanked 0 and Banked increased; abort on
   hatch-0 during second half keeps dripped. PlayMode green. Prompt on the Master
   terminal: `[E] Master Control`.

## Implementation

**Create** `Assets/Scripts/Sim/Run/MasterControl.cs` — `TerminalWaves = 5`,
`HatchWaves = 5`. Sequence state lives on `RunSim` (phase of the sequence, remaining
counts), not a MonoBehaviour.

**Change** `RunSim.NotifyAssaultCleared` — do not set `AllWavesCleared` on wave 5.
If a Master sequence is live, decrement the current half and either start the next
Hatch-bank Wave, flip to the second half, or `EndRun` cleared. `TriggerDraw` past
`MaxWaves` allowed when `Mode == Terminal` and no sequence, or when the sequence is
driving.

**Change** `GameController` / `BastionTerminal` — `isMaster` Interact starts
`Sim.StartMasterControl()`. Seal/open the Control Room door via `BastionLayout`.

**Change** `MetaState` + `SaveData` — store last successful Hatch-bank graph snapshot
(completed terminal ids). Deep start applies it in `BastionLayout` on EngageTether.
Save version + one migration. Do not add a second migration path (M42).

**Change** `language.md` — **Master Control** row. **cleared** / **stripped** means
survived Master Control's Hatch-bank half, not “Wave 5 done.”

**Change** `Docs/Play/A-Run.md`. `4-Data.md` GEN or hand table for the 5+5 split.

Rejected: converting on Interact with no fight. Rejected: winning while leaving unbanked
on the table. Rejected: spending a mid-Run slot on the finale (greed Runs could not
extract). Rejected: untimed Prep between halves (the travel is the window).

## Supporting work

- **Driven by** Interact at Master Control — here.
- **Shown by** the Master prompt and the Control Room door sealing — here.
- **Reads back as** the mode flip, the door opening, Banked rising on the Hatch-bank
  half, then the win screen — here.

## Verification

1. `MasterControlTests` — criteria 3–6.
2. `Tools/verify/run.sh` + integration check; deep-start tests extended for graph
   restore.
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

## Watch

- Composition clamp at Wave 5 for extra terminal Waves will feel like the curve stopped.
  That is this slice; a later curve extension is a different milestone.
- Sequence chain uses the same `InterWaveGap` as deep start (3 s). Do not invent a second
  gap constant.
- `isMaster` is authored; a wing **T** must not have the flag.

## Depends on / Unblocks

Depends on M52 and M53. Unblocks M55 juice on the Master pull. Destinations still wait
on this destination's loop being playable.
