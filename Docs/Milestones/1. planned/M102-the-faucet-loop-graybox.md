# M102 — The faucet loop, graybox

<!--
Status: planned (built 2026-08-24, dev-gated; harness green, playthrough owed)
Update trigger: when the faucet rules change, when the draft semantics are decided by
play, or when the slice graduates from dev-gated to shipped.
Last updated: 2026-08-24
-->

A dev-gated graybox of the faucet loop from `../../Brainstorms/coop-bastion-complexity.md`:
field Waves hunt the Hatch, the terminal is a faucet with an off verb, holding longer
earns a bigger modifier draft, and shutting off teleports the Boarder to the Hatch for
the bank stand. Off by default; the shipped loop is byte-identical until the dev toggle
is pressed.

## Story

As a Boarder, I want the Waves I start to besiege the Hatch while I choose when to walk
out and cut them off, so that holding longer is a bet I place rather than a script I
follow.

## Why it matters

The co-op complexity thread (`../../Brainstorms/coop-bastion-complexity.md`) found the
shipped field wave has no spatial anchor — spawn distance is a tuning hack, extra guns
melt one doorway, and nothing occupies a second player. The faucet loop anchors every
Wave on the Hatch, prices the shutoff trip, and makes duration the stake. This is
**TRANSFORMATIVE tier (#1 paradigm)** on the leverage ladder (`../../Working-Rules.md`),
which is exactly why it ships dev-gated: the paradigm's kill criteria are pre-registered
in the brainstorm and this slice exists to run them, not to commit the game.

The graybox's named question: **is the hold decision real** — does the player ever leave
at N, ever reach N+2, and when do they choose to travel?

## Acceptance criteria

1. With the toggle off, a Run is unchanged: the full EditMode and PlayMode suites pass
   with the same failure set as the pre-change baseline (recorded set only).
2. Pressing the dev console faucet key toggles `RunSim.FaucetActive`; the console lists
   the binding and the log names the state each way.
3. Faucet on: starting a field-terminal Wave chains automatically — each cleared Wave
   starts the next after `WaveDirector.InterWaveGap`, with no input, and the wave index
   climbs.
4. Faucet on: Gatekeepers on a field Wave target the Hatch, the Hatch takes damage
   (and unbanked attrits with it, as in a bank window), and a Gatekeeper transiently
   chases a Boarder inside its aggro radius, returning to the Hatch when the Boarder
   leaves. Sight alone provokes nobody in faucet mode.
5. Each faucet Wave cleared pays `FaucetLoop.WaveEndureBonus` unbanked, visible on the
   HUD unbanked readout.
6. After `FaucetLoop.MarkAfterWaves` cleared Waves the origin terminal is marked (HUD
   line + worldspace marker), and `T` at it arms the shutoff; `T` there before the mark
   is refused with a logged reason.
7. Shutoff armed: chaining stops, and when the live Wave clears the draft opens — one
   axis at N endured, two at N+1, three at N+2, never more. `E` cycles the highlighted
   axis, `T` confirms.
8. On confirm the Boarder is placed at the Hatch (no fall-through, yaw only) and a
   Hatch-bank window starts with the chosen axis applied to its Wave (Density,
   Toughness or Tempo — visible in the spec: body count, HP multiplier, or spawn gap)
   and the conversion multiplier raised by `FaucetLoop.ConvertBonus`.
9. After the bank resolves the Run is in Prep with the faucet cycle reset; the loop can
   be run again at the same or another field terminal.
10. `FaucetLoopTests` (EditMode, pure sim) covers chaining, the endure bonus, the mark
    at N, draft growth and cap, shutoff refusal before N, and axis application to the
    bank spec — all runnable offline.

## Implementation

Sim first, Game after; every faucet path is inert while `FaucetActive` is false.

1. **Create** `Assets/Scripts/Sim/Run/FaucetLoop.cs` — static rules class in the
   `HatchBank` mold: `MarkAfterWaves = 2`, `DraftCap = 3`, `WaveEndureBonus = 100L`,
   `ConvertBonus = 0.25f`, `enum FaucetAxis : byte { Density, Toughness, Tempo }`,
   `int DraftSize(int wavesEndured)` (0 before the mark, then `1 + endured − mark`,
   capped), and `WaveSpec ApplyAxis(WaveSpec spec, FaucetAxis axis)` — Density: +50%
   Defaults (count and `enemyCount` together, preserving the `WaveSpec` invariant),
   Toughness: `enemyHealthMultiplier × 1.5`, Tempo: `spawnInterval × 0.6` floored at
   `WaveDirector.MinSpawnInterval`. A third post-`NextWave` transformer beside
   `ApplyHatchComposition` and `ApplyFinaleElite`, applied in `RunSim.LaunchWave`.
2. **Change** `Assets/Scripts/Sim/Run/RunSim.cs` — faucet state and transitions:
   `FaucetActive` (+ `SetFaucetLoop(bool)`, refused mid-Assault with a logged reason),
   `FaucetWavesEndured`, `FaucetMarked`, `FaucetShutoffArmed` (+ `TryArmFaucetShutoff`),
   `FaucetAwaitingDraft` and `FaucetDraftSize`, `TryPickFaucetAxis(FaucetAxis)`.
   `NotifyAssaultCleared`: when a faucet field Wave clears — award the endure bonus,
   increment endured, then either re-arm the chain on the existing `_followUpTimer`
   seam (shutoff not armed) or enter `FaucetAwaitingDraft` (armed). `DamageHatch`: the
   Terminal-mode early return gains `&& !FaucetActive`. `LaunchWave`: apply the pending
   axis to a Hatch-bank spec and fold `ConvertBonus` into that window's convert
   multiplier. Rejected: a third `WaveMode` value — every `Mode ==` branch in Sim and
   Game would need auditing for a slice whose whole point is two behavioral deltas.
3. **Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` — the
   `hatchMode` line also true when the sim reports faucet-field; the sight-provoke
   branch gains `&& !faucet` so only damage provokes stickily in faucet mode. The
   existing `playerNear` term already supplies the transient peel; no new mechanism.
4. **Change** `Assets/Scripts/Game/Dev/DevCommands.cs` + `DevConsole.cs` +
   `Assets/Scripts/Game/Bastion/GameController.DevCommands.cs` — one new binding
   (`Kind.ToggleFaucetLoop`), the standard three-part registration, `NotifyDevAction`
   taint on toggle.
5. **Change** `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (+ a small
   partial if line caps demand) — route `E`/`T` to the draft while
   `Sim.FaucetAwaitingDraft`; arm shutoff on `T` at the marked terminal during a live
   faucet Wave; on draft confirm, place the player at the Hatch using the
   `PlacePlayerNear` pattern (CC disable, `SetPositionAndRotation` with authored Y and
   yaw only, `Physics.SyncTransforms()`, re-enable), activate the bank spawn set and
   start the bank window, bypassing the demo-zone bank gate while `FaucetActive` with
   a logged line saying so.
6. **Change** `Assets/Scripts/Game/Bastion/BastionTerminal.cs` — prompt branches for
   faucet live / marked / shutoff armed, in the existing branch ladder with the
   existing `StringBuilder`.
7. **Change** `Assets/Scripts/Game/Bastion/HudController.Fight.cs` (+ view if needed) —
   a change-gated push on the free `SetGeneralMessage` lane for the faucet status line
   (wave count, mark, draft options), and `PingMarker.Attach` on the marked terminal's
   transform.
8. **Create** `Assets/Tests/EditMode/Run/FaucetLoopTests.cs` — the `Tethered/Kill`
   helper idiom from `HatchBankTests`, plus the auto-chain assertion pattern from
   `TerminalWaveTests.ArmSequentialDraws_AfterClear_WaitsGapThenDraws`.

## Supporting work

- **Driven by** the dev console toggle plus the shipped `E`/`T` verbs — here.
- **Shown by** the console binding list, the terminal prompt branches, the HUD status
  line and the worldspace mark — here.
- **Reads back as** the unbanked readout moving per Wave, the draft line, the bank
  window's spec and convert multiplier, and log lines for every refusal — here.

## Verification

1. Criteria 1, 10 — offline: `dotnet build` the four generated csproj replicas at 0
   errors, then the EditMode suite through the file bridge (`gkb_verify_request.txt`);
   pass = baseline failure set only, plus `FaucetLoopTests` green.
   **Ran 2026-08-24 (dev-tainted session): EditMode 795/15, PlayMode 82/0 — all 13
   `FaucetLoopTests` pass, and every failure is the recorded pre-existing set plus two
   casualties of concurrent M95 / M63-3 sessions (`CoopScene_Exists…` stair land,
   `CatalogVolumes…` Play page). No failure is this milestone's.**
2. Criteria 3–9 — `FaucetLoopTests` assertions per criterion where the rule is sim-side
   (chain, bonus, mark, draft size, refusal, axis application).
3. Playthrough path — from Play: solo, embark, dev console → faucet toggle, `E`+`T` at
   T1, hold two Waves (watch the Hatch HP and unbanked), walk to T1 under fire, `T`,
   clear the Wave, `E`-cycle and `T`-confirm an axis, arrive at the Hatch, survive the
   bank. Confirms the loop end to end and is the graybox experiment itself.

## Open questions

- Draft semantics — pick one of a growing set is built; *additional picks* is the
  alternative. Unknown, needs play data.
- Whether the shipped juice dial folds into the draft. Unknown, needs play data —
  both are live in this slice and they stack.
- Co-op: the vote panel (any Boarder initiates, visible votes, party lead confirms)
  and party-wide teleport are the designed shape; this slice is solo-tested and
  guests are untested with the toggle on.

## Do not build

The vote panel. The authored seat-set pick (the origin terminal is the off switch in
this slice). Carrying a modifier to the next terminal. Replacing per-kill pay with
endure-only pay. Any Lost/respawn rule change (Zo owns that separately). Any change to
the language table (waits for the real feature). Any change visible with the toggle
off.

## Depends on / Unblocks

Depends on nothing unshipped. Unblocks the brainstorm's graybox tests and, if the
paradigm survives, the real milestone that recuts the loop docs and the co-op layout
work in `M95`'s footprint.
