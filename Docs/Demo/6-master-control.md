# 6 — Master Control (Control Room fight)

<!--
Purpose: Master Control is a fight inside a sealed Control Room. It is not the
win. Juice stays hidden. Send them to the Hatch in beat 7 when the door opens.
Status: demo spec
Authority: implementable
Update trigger: when MasterControl.TerminalWaves changes, when juice-hide lifts
Last updated: 2026-08-16
Depends: voice.md, 5-repeat-to-control-room.md, M54 (in tree)
-->

## What the player sees

They walk through D3 into the Control Room. Copy: this room will seal; the fight
is **inside**; this is not extract. Look at Master Control. **No juice.** `E`
starts the sequence (`M54`: one pull). Door shuts. Waves in `WaveMode.Terminal`
inside the room. Hatch is not a target. That half ending is beat 7’s cue, not
Escaped.

## Why this beat exists

`M54` already runs 5 terminal-mode Waves then 5 Hatch-bank windows. Live prompt
is `[E] Master Control` and still appends juice/intensity (`BastionTerminal` /
Master branch). Nothing says the room fight is not the haul home. A Boarder who
stays in R15 through the Hatch half is the travel kill already on `M54`.

This beat is copy, juice-hide, and a seal line. It does **not** add a third win
and does **not** retune `MasterControl.TerminalWaves = 5`.

## In tree today

| Piece | Fact |
|---|---|
| Sequence | `RunSim.StartMasterControl()` → `_masterPhase = Terminal`, `_masterLeft = 5`, `TriggerDraw(Terminal)` |
| Seal | Control Room door `SetOpen(false)` for the half (`M54` c3) |
| Hatch | Not a target (`DamageHatch` ignored in Terminal mode) |
| Gaps | `WaveDirector.InterWaveGap` (3 s) Prep between clears, not untimed Prep |
| Chain | Deep-start chain disabled on this pull |
| Prompt | Unarmed: `[+/-] Master Control` + `AppendStake` + `[E] select`. Armed: `[T] Master Control` + `[E] edit` (`BastionTerminal` Master branch). `M54` c2 said Interact starts the sequence; live code is **E then T**, same as Field. Demo keeps E then T |
| Kind | `BastionTerminalKind.Master`, id from sketch `M` in R15 |
| Spawns | `S_Control`; seal mutes layout spawns (`M54` closed note) |
| Win | **Not** this half. Unbanked still grows |
| Juice | `DemoActive` withhold from beat 1 still on |
| Travel kill | `M54` kill: staying in Control Room while Hatch-bank runs, Hatch holds without the Boarder |

`MasterControl.TerminalWaves = 5`, `HatchWaves = 5`. Composition past
`MaxWaves` clamps at wave index 4 (Wave 5 table): 66 bodies, 12 Hunters, 7
Thieves, HP ×2.60, ×`playerCount`.

## The change

1. On D3 open / first entry to R15 this Run: `demo.control_room` Party Lock Prep
   (if Assault, skip — they should not enter mid-Assault).
2. On first look-at Master this Run: `demo.master_control` Party Lock Prep,
   then they `E`.
3. Master prompt while `DemoActive`: unarmed `[E] Master Control — select`;
   armed `[T] Master Control — start`. No `+/-`, no Intensity, no foresight
   roster.
4. Keep live **E then T** (`IsWaveStartArmed` then `PullDraw`). Do not restore
   M54’s “Interact starts the sequence” and do not add a third key.
5. Juice withhold unchanged. `StartMasterControl` must not juice.
6. Log `[Demo] master-control room fight` on sequence start (in addition to
   `[Terminal] master-control started terminalWaves=5 hatchWaves=5`).

No composition change. No `TerminalWaves` change. No auto-pull.

## Create

Nothing required beyond `DemoVoice` ids and prompt branch already in
`DemoHud` / `BastionTerminal`.

Optional: trigger volume on R15 entry if look-at is easy to miss. Prefer
look-at + D3-open line so we do not add a new trigger type. D3-open already
plays `demo.door_d3` (beat 5); `demo.control_room` can be the **same moment**
as D3 open (one conversation, not two). Spec: **merge** — play `demo.control_room`
on D3 open, skip a separate `demo.door_d3` if both would fire. `demo.master_control`
stays on Master look-at.

## Change

| File | Change |
|---|---|
| `BastionTerminal` Master `Prompt` | Hide juice when `DemoActive` |
| `GameController` Master start | Existing; add demo log + voice on look-at |
| Door D3 open | `demo.control_room` instead of generic `demo.door_d3` |

## Delete

Nothing. Do not delete Hatch half. Do not delete travel-kill criterion.

## Numbers

| Name | Value | Source |
|---|---|---|
| Terminal half | 5 Waves | `MasterControl.TerminalWaves` |
| Gap | 3 s | `InterWaveGap` |
| Clamp composition | Wave 5 table | `MaxWaves-1` |
| Juice | withheld | `DemoActive` |

Do not retune these for the demo. If 5 room Waves is too long for a polished
first session, that is a **later** `#12` change with play data, not this spec.

## Multiplayer

| Event | Authority |
|---|---|
| Master `E` | Host sim, any seat at the terminal (live). One sequence |
| Seal | Host door, all seats |
| Voice | Party Lock Prep |
| Guest down during room half | `M57` wait. Host continues. Room fight does not use first-hatch any-seat-down |
| Scale | `playerCount` on each Wave |

If the lead never enters R15 and a guest pulls Master: allowed (live). Voice
still Party.

## Voice

`demo.control_room` Party Lock on D3 open (this is not extract).
`demo.master_control` Party Lock on Master look-at.

Assault during the 5 Waves: **no** locking VO. Optional none. Subtitles would
fight the room.

## Driven / Shown / Reads-back

- **Driven by** Interact at Master Control (`M54`).
- **Shown** door shut; Hatch not taking damage; Unbanked rising; prompt without
  juice.
- **Reads-back** existing master-control started line; `[Demo] master-control room fight`;
  door shut log if present.

## Logs

| Event | Line |
|---|---|
| Room copy | `[Demo] master-control room fight` |
| Sequence | `[Terminal] master-control started terminalWaves=5 hatchWaves=5` (existing) |
| Juice | `[Demo] juice refused — demo withheld` if they press `+/-` |

## Traps

- Adding `Escaped` at the end of the Terminal half. Forbidden.
- Starting Hatch half without opening D3 / unsealing (`M54` c4). Do not recut.
- Juice on the Master prompt teaching intensity at the finale.
- Locking input during the 5 Assaults.
- A third win if they clear the room without going to the Hatch.
- `timeScale = 0` “cinematic” seal.
- Spending a mid-Run Hatch-bank slot on this half (Terminal mode does not).

## Depends / Unblocks

Depends: D3 open (beat 5), `M54` in tree, juice-hide (beat 1). Unblocks: beat 7
when `MasterHatchHalfStarted` fires.

## Verification

1. PlayMode or EditMode `MasterControlTests` still green (5 then 5, no slot
   spend on Hatch half, Escaped only after Hatch half).
2. Demo prompt on Master contains no `juice` / `Intensity` substring.
3. Log `master-control room fight` then existing started line.
4. Hatch HP unchanged during Terminal half (existing rule; regression).
5. Loopback: both seats see seal; one Master pull.
6. No `[Dev]`.

## Rejected

A skip-Master debug that still Escapes. Reducing 5 to 1 without play data.
Companion in the room. FAB gift before the room.
