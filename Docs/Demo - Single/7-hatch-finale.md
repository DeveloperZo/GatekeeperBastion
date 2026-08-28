# 7 — Hatch finale

<!--
Purpose: The send out of the Control Room and the end of the demo. Door opens;
one final Convert window at the Hatch, with the finale elite; survive the
window → Escaped and the demo ends. A loss here does not end the demo.
Status: demo spec
Authority: implementable
Update trigger: when MasterControl.HatchWaves changes, when the finale elite is
retuned, when Zo rules the mid-finale return-to-Ship question
Last updated: 2026-08-25 (HatchWaves is 1 in the tree, not owed; the finale elite is
specced in MasterControl; DemoActive and EndDemo exist; buy-back row rewritten from
Respawns.cs)
Depends: voice.md, 6-master-control.md, M54 (in tree)
-->

## Why this beat exists

`M54`'s second half already does everything mechanical: door opens, Convert
windows of 45 s, drip and attrition as a live Convert, convert mul snapshotted
at the half start, no slot spend, the last window dumps the remainder,
`CompleteRun` → **Escaped**, `CleanExits++`. The retune landed:
`MasterControl.HatchWaves` is **1** in production and the multi-window
machinery is kept for the tests that pin it at 5. The elite landed with it —
`MasterControl.ApplyFinaleElite` replaces the window's spec with one Thief at
`FinaleEliteHp` **240** pre-multiplier and `FinaleEliteScale` **1.5×** body,
no escorts. Breach or a death nobody can pay for: leftover Unbanked wiped,
dripped stays, Lost.

Death is no longer the loop's only run ender (Zo, 2026-08-17): a player who
survives a Convert window may return to the Ship — held interact at the
Hatch until the next wave is triggered; the Run ends **Docked**, not Lost;
banked and door state persist. Whether the **finale** window offers that
return before Escaped is an open Zo ruling — see Gaps. This beat specs nothing
for it.

Nothing walks them to the Hatch when the door opens. `MsgPlayer` is
Hatch-under-attack, which is correct once they are there and not before.
`MetaState.DemoActive` and `DemoProgress.EndDemo` both exist now, so ending the
demo is a call rather than a field to add.

## Functional storyboard

Continues from beat 6 step 3: `MasterHatchHalfStarted` fired, door open, spawns
active.

### 0 — The send

| | |
|---|---|
| **Goal** | Leave the room. Everything Unbanked rides on the Hatch now. |
| **Player** | Run the umbilical to the Hatch. Fight input stays live. |
| **Game** | `demo.hatch_finale` · **Subtitle** · Party · Game. **The door is open. Get to the Hatch — everything Unbanked rides on holding it.** Subtitle always for this id: a Boarder already running must not be rooted. |
| **Standing** | Defend the Hatch |
| **Log** | `[Demo] hatch finale send` (build). The sim's `master-control hatch half — hatchWaves=1` is **verbose-only**; the send log is the always-on evidence. |
| **Have** | `MasterHatchHalfStarted` event on `RunSim`; unseal + `ActivateHatchBankSpawns` on the game layer. |
| **Build** | Subscribe for voice + send log. |

### 1 — One final window at the Hatch (the finale elite)

| | |
|---|---|
| **Goal** | Survive the single 45 s final window. The pile drips at 8/s × snapshotted mul; the dump comes at the window's end. The **finale elite** is in this window and is specced in code: one Thief, `enemyCount` 1, `groupSize` 1, `finaleElite` set, HP **240** pre-multiplier (×2.60 at Wave 5 → **624** spawned, twice the Wave-5 Thief line), body scale **1.5×**. Damage and speed stay the Thief's — HP is the one mechanical lever, and the scale is presentation only so the agent radius stays 0.42 and it cannot wedge in the doorway. |
| **Player** | Defend. HUD shows `CONVERT — Master Control {k}/{n} — {s}s` (live string, `HudController.Panels`; reads 1/1). `MsgPlayer` `HATCH UNDER ATTACK` on HP drops stays — do not replace it with story. |
| **Game** | — |
| **Standing** | Defend the Hatch |
| **Log** | drip credits; `[Gatekeepers] gatekeeper reached the hatch — attacking home` |
| **Have** | Everything. No slot spend; Unbanked 0 allowed on this half; the window does not dump early; 3 s gap auto-chains from the room half; attrition on Hatch damage still burns Unbanked; `HatchWaves` is 1; `ApplyFinaleElite` swaps the spec. `M54`'s intermediate-window no-dump path goes unused at `HatchWaves = 1` — keep it, do not delete. |
| **Build** | Nothing mechanical. Only the send log and the voice. |

### 2a — Escaped

| | |
|---|---|
| **Goal** | The win, and the demo ends. |
| **Player** | The window clears → dump → **Escaped**. End screen: title **BASTION STRIPPED**, framing *out through the Hatch — Master Control Hatch-bank survived*, earned / repair / net / banked / deepest lines. `ENTER` returns to the Ship. |
| **Game** | `demo.escaped` · Lock · Party · Game, first Ship frame after Escaped. **Escaped. The Bastion is stripped and the Salvage is home. The Ship is yours now — every console, every Wave.** |
| **Standing** | — (demo over) |
| **Log** | always-on `[Run] BASTION STRIPPED — Master Control Hatch-bank survived, …` then `[Demo] demo ended` (build) |
| **Have** | `CompleteRun`: wipe (0 left), `CleanExits++`, `AllWavesCleared`, phase Escaped, Save. End screen strings in tree. |
| **Build** | `DemoProgress.EndDemo(meta)` — table below — plus the line and the log. |

### 2b — Lost on this half

| | |
|---|---|
| **Goal** | The demo does not end on a loss. |
| **Player** | Breach or host down → Lost, leftover wiped, dripped stays. They retry Master from a deep start; graph restore is success-only, so a loss here does not save Control Room complete. NavUnlock is already high. |
| **Game** | — (no special line; beat 2's `demo.first_hatch_down` does not refire — its trigger is the first lost Boarding only) |
| **Log** | existing `[Run] RUN OVER …` — `DemoActive` stays true |
| **Have** | `EndRun("hatch"/"player")` paths. |
| **Build** | Guard: `EndDemo` fires on **Escaped only**. Any Escaped is enough (live win is only Master). A Docked run end (if the mid-finale return is ever ruled in — Gaps) is not Escaped and must not `EndDemo`. |

### DemoActive off (exactly once)

| Flag | Value |
|---|---|
| `DemoActive` | `false` |
| `DemoFabPowered` | `true` |
| Juice | live (already live — beat 1 teach; nothing to lift) |
| Wheel cap | off |
| NAV | `DeepestWaveReached` + chain (live behaviour returns) |
| Capsule | never again |
| CREW / ARMORY / NAV power | stay powered |

Do not reset `DemoNavPowered` / weapons / protocol.

### Travel kill

Do not add sim code that fails the Run if they stay in R15. The kill is an
observation on Hatch HP and `hatchDist` (`M54`). Demo copy tells them to go.
If they ignore it and still Escaped, the observation still stands.

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 7 |
|---|---|---|---|
| Half flip + event | yes | `RunSim.AdvanceMasterControl`, `MasterHatchHalfStarted` | Have |
| Unseal + hatch spawns | yes | `GameController.HatchBank` | Have |
| Window machinery: no slot spend, end dump | yes | `RunSim` Master Hatch path | Have |
| Single final window (`HatchWaves` = 1) | **yes** | `MasterControl.HatchWaves` | Have |
| Finale elite in the final window | **yes** | `MasterControl.ApplyFinaleElite` / `FinaleEliteHp` / `FinaleEliteScale` | Have |
| Dead-seat buy-back | **yes, and not window-gated** | `Respawns.cs`, `RunSim.Respawns.cs` | Have |
| Return-to-Ship (Docked) mid-finale | — | — | **Open** — Zo ruling (Gaps) |
| Escaped / `CleanExits` / Save | yes | `CompleteRun` + end screen persist | Have |
| Win screen strings | yes | BASTION STRIPPED / out through the Hatch | Have |
| Finale HUD counter | yes | `CONVERT — Master Control {k}/{n} — {s}s` (reads 1/1) | Have |
| `MsgPlayer` under attack | yes | on HP drop (not mode-gated) | Have — keep |
| `DemoActive` / `EndDemo` | **yes** | `MetaState.DemoActive`, `DemoProgress.EndDemo` | Have |
| Voice ids / send + ended logs | no | — | Build |

## Create

| Path | Role |
|---|---|
| Hook on `MasterHatchHalfStarted` | GameController subscribes for voice + send log |
| `DemoProgress.EndDemo(meta)` | Flags table above |

## Change

| File | Change |
|---|---|
| `GameController` Master Hatch half | Play `demo.hatch_finale`; existing unseal |
| `GameController` / Ship load on Escaped | `EndDemo` + `demo.escaped` |
| `MapStation` / FAB / wheel gates | Read `DemoActive` (beats 0–4 wiring) |
| `MasterControl.HatchWaves` | Nothing — already **1** |
| Final window composition | Nothing — `ApplyFinaleElite` already swaps it |
| Boarding rejoin | Nothing — the priced respawn is in tree and is not window-gated |

## Delete

Nothing. `MasterControl.HatchWaves` is **1** in production and the multi-window
machinery is what the tests pin at 5 — do not delete it.

## Numbers

| Name | Value | Source |
|---|---|---|
| Final Convert window | **1** | `MasterControl.HatchWaves` (production; tests pin 5) |
| Window length | 45 s | `HatchBank.DurationSeconds` |
| Gap into the window | 3 s | `InterWaveGap` |
| Slot spend | 0 | `M54` c4 |
| Drip | 8/s | `HatchBank.DripPerSecond` |
| Convert mul | snapshotted at half start from carry | `M54` |
| Finale elite stats | HP **240** pre-multiplier (**624** spawned at Wave-5 ×2.60); Thief damage and speed; body scale **1.5×**, presentation only; one body, no escorts | `MasterControl.FinaleEliteHp` / `FinaleEliteCount` / `FinaleEliteScale` |
| Respawn cost | **150** × (respawns taken this Boarding + 1), from that seat's own Unbanked | `Respawns.CostFor` |

One 45 s window after five room Waves. Wave counts are tunable levers; do not
retune further without play data.

## Multiplayer

| Event | Authority |
|---|---|
| Half flip | Host sim, all seats |
| Voice finale | Party Subtitle |
| Voice escaped | Party Lock on shared Ship after end screen (`M72` c7) |
| Guest down | `M57` wait, or the buy-back row below. Host can still Escaped without them at the Hatch. Converted dump uses wallets |
| Buy-back | Shipped, and wider than the 2026-08-17 stamp: any dead seat is offered a respawn while the Boarding is Tethered, with no window gate, at **150** × (respawns taken this Boarding + 1) out of that seat's **own Unbanked**. On this half a seat's carry is large, so the offer will usually stand |
| Dead host | Closed — the host takes the same offer at the DOWN overlay, and `M49` bites only when nobody left can pay |
| `EndDemo` | Each seat's Meta on Escaped persist. A guest disconnected before the end may stay `DemoActive` on their save; next solo Continue they are still on demo. Acceptable |
| Join | Session is in Bastion; no Ship join |

Ready is not used on the finale. End screen dismiss → party Ship (`M72` c7).

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.hatch_finale` | Subtitle | Party | `MasterHatchHalfStarted` | Defend the Hatch |
| `demo.escaped` | Lock | Party | first Ship frame after Escaped | — (demo over) |

No companion thanking them. No siphon mention. No elite-arrival line is written
here — if one is added it needs draft copy for Zo.

## Driven / Shown / Reads-back

- **Driven by** surviving the Hatch half (`M54`), not a new button.
- **Shown** door open; the finale elite in the final window; the Convert HUD
counter; BASTION STRIPPED; Ship with all consoles live including FAB.
- **Reads-back** `[Demo] hatch finale send`, `[Demo] demo ended`, always-on
`[Run] BASTION STRIPPED — …`.

## Logs

| Event | Line | Status |
|---|---|---|
| Send | `[Demo] hatch finale send` | build |
| Half | `master-control hatch half — hatchWaves=1` | in tree, **verbose-only** |
| Escaped | `[Run] BASTION STRIPPED — …` + `[Demo] demo ended` | in tree + build |
| Lost on half | `[Run] RUN OVER …` — `DemoActive` still true | in tree |

## Traps

- Convert-then-fight on the Master Hatch half (would dump before the window).
- First-hatch Hunter add on this window.
- Setting `DemoActive false` on **Lost** Master.
- Gating a test on the half-flip log — it is `Log.Info`. Use the event or the
send log.
- Deleting the multi-window machinery. `HatchWaves` is 1 in production and the
  tests set it to 5 to pin the intermediate-window no-dump path.
- Deciding the mid-finale return-to-Ship in code — it is an open Zo ruling
(Gaps), not spec either way.
- Re-gating the buy-back to a live Convert window, or paying it from the shared
pile. `Respawns.Refuse` gates on the Boarding being Tethered and the seat's own
carry covering the price, and on nothing else.
- `timeScale = 0` victory cinematic.
- A Ship "you won" `GUILayout` overlay.
- Calling `EndDemo` from anywhere but Escaped. Any Escaped is enough (live win
is only Master).

## Depends / Unblocks

Depends: beat 6 sequence start, `M54` Hatch half, v9 flags. The elite's
composition is in `MasterControl` and is no longer a dependency. Unblocks: live game on that save (juice already
live; FAB, chain, uncapped wheel). Friends close still needs a five-Wave
friends dry-run (`Roadmap.md`). Shared Ship is `M72`.

## Verification

1. `MasterControlTests` Hatch half: single window (`HatchWaves` 1), no slot
  spend, dump at end, Escaped.
2. Demo flag: after Escaped, JSON has `DemoActive false`; FAB opens; wheel
  uncapped; NAV chain restored.
3. Lost on Hatch half: `DemoActive` still true.
4. Log `hatch finale send` on half start; `demo ended` on Escaped.
5. Loopback: both seats get the send; both load shared Ship; FAB live on a seat
  whose Meta saved Escaped.
6. `MasterControlTests`: the final window's spec comes back as one Thief with
   `finaleElite` set and `enemyCount` 1.
7. Buy-back: a dead guest carrying at least **150** × (taken + 1) rejoins at
  the Hatch and its own Unbanked is debited, remainder intact; one carrying
  less is refused `not enough unbanked`.
8. No `[Dev]`.

## Gaps / parked

- **Open (Zo to rule, one ruling):** does the finale window offer the loop's
  return-to-Ship before Escaped? Returning mid-finale (held interact at the
  Hatch; Run ends Docked; banked + door state persist) vs Escaped-on-survival.
- **Closed by what shipped:** dead-host buy-back vs `M49`. The host takes the
  same priced respawn as any seat.
- **Parked, not spec:** the finale elite wearing the player's own protocol
  layout (pre-authored, clearly marked, not cumulative) — a candidate
  satisfying `Marketing/gaps.md` Focus #4's probe. The elite itself ships; its
  layout is parked, and it has no behaviour of its own — it is a Thief with
  more HP and a bigger body, so calling it a boss is generous.

## After this save

`Play/The-Ship.md` chain is the picker again. FAB sells Hatch Armor / Repair
Rig / Decoy. Wheel to MaxTier. Protocol already claimed. Second destination is
still not this demo.

## Rejected

Third win in the Control Room. Auto-move to Hatch. Re-adding the five-window
finale (single window ruled 2026-08-17). Companion. Pause-as-credits with
`timeScale = 0`.
