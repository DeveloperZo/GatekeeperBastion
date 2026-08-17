# 7 — Hatch finale

<!--
Purpose: When the Control Room door opens after the room half, send them to the
Hatch. Survive Master Control’s Hatch-bank half → Escaped. DemoActive ends.
Status: demo spec
Authority: implementable
Update trigger: when HatchWaves changes, when DemoActive-off side effects change
Last updated: 2026-08-16
Depends: voice.md, 6-master-control.md, M54 (in tree)
-->

## What the player sees

The Control Room door opens. Subtitles: defend the Salvage **at the Hatch**.
They run the umbilical. Five Hatch-bank windows (`M54` second half). Survive →
**Escaped**. The pile dumps. They are back on the Ship. The dead-Ship demo is
over: juice, FAB, full wheel, NAV chain.

A win that never leaves the Control Room is the travel kill already on `M54`.
This beat does not add a third ending. It adds the send-home line.

## Why this beat exists

`M54` second half already: door `SetOpen(true)`, `MasterControl.HatchWaves = 5`
windows, `DurationSeconds` 45, drip/attrition as live Hatch-bank, convert mul
snapshotted at Hatch-half start, **does not** decrement `HatchBankSlotsLeft`,
intermediate windows do not dump, last window dumps remainder, `CompleteRun` /
`Escaped`. Breach or down: leftover Unbanked wiped, dripped stays, `Lost`.

Nothing walks them to the Hatch when the door opens. `MsgPlayer` is
Hatch-under-attack, which is correct once they are there and not before.

## In tree today

| Piece | Fact |
|---|---|
| Flip | `AdvanceMasterControl` when Terminal half `_masterLeft` hits 0 → `MasterControlPhase.Hatch`, raise `MasterHatchHalfStarted`, log `master-control hatch half — hatchWaves=5` |
| Unseal | Game layer on that event: door open, `ActivateHatchBankSpawns` (`M54` impl) |
| Convert | Snapshot at Hatch-half start, `_masterHatchMulLocked` |
| Dump | Only when Hatch half completes (`AdvanceMasterControl` last hatchBank) |
| Unbanked 0 | Allowed on this half (`M54` c4) |
| Slots | Not spent |
| `MsgPlayer` | Hatch under attack during Hatch-bank |
| End | `CleanExits++`, not `PodEjections`. `GameSession.Save` |

Convert-then-fight and first-hatch Hunter spec **must not** apply
(`DemoFirstHatchConvertDone` already true; Master path is `_masterPhase==Hatch`).
Any-seat-down override is already off after beat 2.

## The change

1. On `MasterHatchHalfStarted`: `demo.hatch_finale` Party **Subtitle** (Assault
   or the InterWaveGap Prep — if the flip happens in Prep gap, Lock is allowed
   for 3 s; prefer **Subtitle** so a Boarder already running is not rooted).
   Spec: **Subtitle always** for this id.
2. `MsgPlayer` may stay Hatch-under-attack once the first Hatch-bank window
   is live. Do not replace it with story.
3. On `Escaped` Docked: `demo.escaped` Party Lock. Then set
   `DemoActive = false`, `DemoFabPowered = true`, `DemoWheelBuysLeft` ignored,
   juice allowed, NAV chain on, FAB open. Save.
4. Lost on this half: `DemoActive` stays true. They retry Master from a deep
   start that restores graph (`M54` c6) if the Hatch-bank half was not the
   success snapshot — a loss here does **not** save Control Room complete.
   NavUnlock already high. They walk D3 again if restore says so.

### DemoActive off (exactly once)

When `Phase == Escaped` after this sequence:

| Flag | Value |
|---|---|
| `DemoActive` | `false` |
| `DemoFabPowered` | `true` |
| Juice | live |
| Wheel cap | off |
| NAV | `DeepestWaveReached` + chain |
| Capsule | never again |
| CREW / ARMORY / NAV power | stay powered |

Do not reset `DemoNavPowered` / weapons / protocol.

### Travel kill

Do not add sim code that fails the Run if they stay in R15. The kill is an
observation on Hatch HP and `hatchDist` (`M54`). Demo copy tells them to go.
If they ignore it and still Escaped, that observation still stands.

## Create

| Path | Role |
|---|---|
| Hook on `MasterHatchHalfStarted` | Already exists on `RunSim`; GameController subscribes for door + voice |
| `DemoProgress.EndDemo(meta)` | Flags table above |

## Change

| File | Change |
|---|---|
| `GameController` Master Hatch half | Play `demo.hatch_finale`; existing unseal |
| `GameController` / Ship load on Escaped | `EndDemo` + `demo.escaped` |
| `MapStation` / juice / FAB gates | Read `DemoActive` (already) |

## Delete

Nothing. Do not delete the five Hatch windows.

## Numbers

| Name | Value | Source |
|---|---|---|
| Hatch windows | 5 | `MasterControl.HatchWaves` |
| Window length | 45 s | `HatchBank.DurationSeconds` |
| Gap between windows | 3 s | `InterWaveGap` |
| Slot spend | 0 | `M54` c4 |
| Drip | 20/s | live |
| Convert mul | snapshotted at half start from carry | `M54` |

Five × 45 s plus gaps is a long finale. Same as live Master Control. Do not
shorten without play data.

## Multiplayer

| Event | Authority |
|---|---|
| Half flip | Host sim, all seats |
| Voice finale | Party Subtitle |
| Voice escaped | Party Lock on shared Ship after end screen (`M72` c7 party return) |
| Guest down | `M57`. Host can still Escaped without them at the Hatch (travel kill / carry). Converted dump uses wallets |
| `EndDemo` | Each seat’s Meta on Escaped persist. Guest who was waiting on Ship still Escapes with the party if the Run did — follow existing end-screen persist. If guest was disconnected, their save may remain `DemoActive`; next solo Continue they still on demo. Acceptable |
| Join | Session is in Bastion; no Ship join |

Ready is not used on finale. End screen dismiss → party Ship (`M72` c7).

## Voice

`demo.hatch_finale` Party Subtitle on Hatch half start (go to the Hatch).
`demo.escaped` Party Lock first Ship frame after Escaped.

No companion thanking them. No siphon mention.

## Driven / Shown / Reads-back

- **Driven by** surviving the Hatch half (`M54`), not a new button.
- **Shown** door open; Hatch-bank HUD; end screen Escaped; Ship live consoles
  including FAB.
- **Reads-back** `[Demo] hatch finale send`, `[Demo] demo ended`, existing
  `master-control hatch half`, `Escaped` / CleanExits logs.

## Logs

| Event | Line |
|---|---|
| Send | `[Demo] hatch finale send` |
| Half | `master-control hatch half — hatchWaves=5` (existing) |
| Escaped | existing CompleteRun line plus `[Demo] demo ended` |
| Lost on half | existing `Run ended (down\|hatch)` — `DemoActive` still true |

## Traps

- Convert-then-fight on Master Hatch half (would dump before five windows).
- First-hatch Hunter add on these windows.
- Any-seat-down ending the finale because the override leaked.
- Setting `DemoActive false` on **Lost** Master.
- `timeScale = 0` victory cinematic.
- A Ship “you won” GUILayout overlay.
- Calling `EndDemo` on CleanExits from a **non-Master** path (there isn’t one
  if Wave 5 cannot Escaped). Guard: `EndDemo` only from Escaped after
  `MasterControl` sequence, or simply from any `Escaped` (live win is only
  Master). **Any Escaped** is enough.

## Depends / Unblocks

Depends: beat 6 sequence start, `M54` Hatch half. Unblocks: live game on that
save (juice, FAB, chain, uncapped wheel). Friends close still needs `M72` c9.

## Verification

1. `MasterControlTests` Hatch half: 5 windows, no slot spend, dump at end,
   Escaped.
2. Demo flag: after Escaped JSON has `DemoActive false`; juice Try succeeds in
   a following Run; FAB `TryOpenStation` succeeds.
3. Lost on Hatch half: `DemoActive` still true.
4. Log `hatch finale send` on half start; `demo ended` on Escaped.
5. Loopback: both seats get `hatch finale send`; both load shared Ship; FAB
   live on a seat whose Meta saved Escaped.
6. No `[Dev]`.

## After this save

`Play/The-Ship.md` chain is the picker again. Intensity is speakable. FAB sells
Hatch Armor / Repair Rig / Decoy. Wheel to MaxTier. Protocol already claimed.
Second destination is still not this demo.

## Rejected

Third win in the Control Room. Auto-move to Hatch. Shortening 5 windows in this
spec. Companion. Pause-as-credits with `timeScale = 0`.
