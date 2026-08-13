# M52 — Waves start at a terminal

<!--
Status: later
Update trigger: when field-terminal interact, targeting mode, or the T-key remap changes
Last updated: 2026-08-13
-->

The first player-visible cut of the field loop
(`../../Brainstorms/missions-and-destinations.md`). **Interact at a field terminal starts
a Wave that hunts the Boarder. The Hatch cannot be hurt. Clearing that Wave completes the
terminal and opens its locked door.** The Siphon Draw key no longer starts Waves. Salvage
is still banked on the kill (M53 delays that). The five-Wave clear still ends the Run
(M54 replaces that win).

## Story

As a Boarder, I want a Wave to start when I use a field terminal, so that progress through
the bastion is the fight, and sitting at the Hatch during that fight earns nothing.

## Why it matters

The shipped fight is still Hatch defence started by `T` (`GameController` Prep →
`RunSim.TriggerDraw`). M47 authors **T** / **D** marks and the unlock graph;
`BastionTerminal.Activate` currently completes a terminal with no Wave. The loop's first
verb is that interact.

Leverage tier (`../../Working-Rules.md`): **#9, rules** — who starts a Wave, and whether
the Hatch is a target. Not a retune.

Evidence the Hatch-sit is the whole game: session
`Logs/GatekeeperBastion_2026-08-08_17-00-15.log` (24 Hatch arrivals / 210 bodies).

## Kill criterion

After close, ten logged Runs on the kit bastion. **If median `hatchDist` on those
terminal-Wave kills sits in the Hatch bay (`Tools/wave-report`, M37), stop** — the field
is decoration and this slice did not create a second fight.

## Acceptance criteria

1. **Prep `T` (Siphon Draw) is a logged refusal** — `Waves start at a terminal` — and does
   not call `TriggerDraw`. Shortcut panel copy matches.
2. **Looking at a wing field terminal in Prep, Interact Down starts the next Wave** in
   `WaveMode.Terminal`. Log: `[Terminal] <id> started wave <n> mode=terminal`. Already
   complete, in Assault, or Master Control (`isMaster`) → logged refusal, no Wave.
3. **During `WaveMode.Terminal`, `GameController.DamageHatch` is a no-op.** Hatch HP does
   not move. The HUD Hatch card still shows. Loss this slice is Boarder down only.
4. **Targeting follows the mode** (same `HunterPerWave` / `RamPerWave` tables):
   Defaults and Rams hunt the Boarder; Hunters still do; **Rams hunt the Boarder on
   terminal Waves only** (dated break of “Ram never hurts you,” this mode only —
   `../../Brainstorms/gatekeepers-and-waves.md`). `MayTargetHatch` is false for every kind.
5. **The terminal is not marked complete until that Wave clears.** Then
   `BastionUnlockGraph.TryCompleteTerminal`; if the door's set is done, `BastionDoor.SetOpen(true)`
   and pressure spawns enable. Dying mid-Wave leaves the terminal incomplete.
6. **Field terminals are `IInteractable` with a prompt** `[E] Terminal — start Wave` (or
   the refusal reason). They do **not** open a Ship station panel. Visual is the kit **T**
   mark / console mesh already in the scene — no new canvas.
7. **M14 still holds:** five Waves, deep-start chain on the first `TriggerDraw` of a Run
   with `StartWave > 1`, clear of Wave 5 still `Escaped`. Instant `AwardKill` into
   `BankedSalvage` still holds.
8. EditMode: `TerminalWaveTests` (new) plus existing `BastionUnlockGraphTests` / RunSim
   suites green. PlayMode suites green after any helper that pressed `T` to start a Wave
   now calls `TriggerDraw` (tests already do). Solo log of a terminal Wave contains the
   criterion-2 line and no hatch-damage line from Gatekeepers.

## Implementation

**Create** `Assets/Scripts/Sim/Run/WaveMode.cs` — `enum WaveMode { Terminal, HatchBank }`.
HatchBank is unused this slice; M53 is the first writer.

**Change** `RunSim` — `WaveMode Mode { get; private set; }` (default Terminal).
`TriggerDraw` sets `Mode = WaveMode.Terminal` and keeps today's wave-index / chain
behaviour. `NotifyAssaultCleared` raises a new event or returns enough for the Game layer
to complete a pending terminal (do not put scene ids in Sim).

**Change** `GameController` — Prep `PullDraw` logs the criterion-1 refusal. Hold a
pending terminal id (Game layer, not Sim). `DamageHatch`: if `Sim.Mode == Terminal`,
return. On `AssaultEnded` / clear: call `BastionLayout.TryCompleteTerminal(pending)`.

**Change** `BastionTerminal` — implement `IInteractable`. `isMaster` serialized; Master
Control Interact refuses (`Master Control — later`, M54). Interact Down in Prep calls
`GameController.StartTerminalWave(this)` which `TriggerDraw`s and stores the id.
Delete the POC `Activate` that completes immediately, or make it the clear-path only.

**Change** `EnemyGatekeeper.Targeting` — `hatchAvailable` requires
`_game.Sim.Mode == WaveMode.HatchBank` in addition to `MayTargetHatch`. On Terminal that
is always false.

**Change** `RamGatekeeper` — `MayTargetPlayer` is true when `Mode == Terminal`;
`CanBeProvoked` stays false (shooting a Ram still does not shout). Comment the dated
break with the date.

**Change** `Controls` / shortcut panel — Prep `T` label: `terminal starts the wave` (not
siphon draw). Binding stays so M53 can remap it to Hatch-bank.

**Change** `../../_meta/_definitions/language.md` — **Wave** is started by a field
terminal (this slice) or a Hatch-bank (M53). **field terminal** row. **the Siphon Draw**
is no longer the start verb; keep the row until M55 kills the dial, marked *start verb
retired M52*.

**Change** `Docs/Play/A-Run.md` and `Docs/Play/Waves.md` so they do not say `T` starts
the Wave.

Rejected: subclassing `StationTerminal` (opens Ship panels). Rejected: completing the
terminal on Interact (silent unlock). Rejected: delaying `AwardKill` (M53). Rejected:
changing the five-Wave win (M54).

## Supporting work

- **Driven by** Interact at a field terminal — here. `T` is the refusal.
- **Shown by** the interact prompt and the shortcut-panel line — here.
- **Reads back as** the Wave starting, the door opening on clear, Hatch HP unchanged —
  here.

## Verification

1. Criteria 3, 4, 5, 8 — `TerminalWaveTests`: TriggerDraw in Terminal mode;
   `DamageHatch` command path does not reduce `Ship.Hatch.Hp`; graph completes only after
   a simulated clear; Master id refuses. `Tools/verify/run.sh` green at current counts plus
   the new tests.
2. Criterion 8 PlayMode — integration check; no new hatch-breach-from-enemies fixture this
   slice (Hatch-bank is M53).
3. Playthrough — from Play on `Bastion.unity`: walk to an open-wing **T**, Interact, clear
   the Wave, walk through the **D** that set owned. Hatch card does not drop. Log has the
   start line and the complete line. `T` in Prep logs the refusal.

## Do not build

| Not building | Why not |
|---|---|
| Soft-lock doors (switch / salvage-repair unlock, no Wave) | Parked until M52 has play data — third unlock loop |
| Unbanked pile / drip / bank slots | M53 |
| Master Control sequence | M54 |
| Intensity / juice / siphon-dial removal | M55 |
| Path telegraph HUD | Parked in the brainstorm |
| Completing a terminal without a Wave | Silent unlock, rejected |

## Watch

- **`T` is also the language for field terminals.** The key and the mark are easy to
  conflate in logs; the log tag is `Terminal`, the key refusal names the key.
- **Prefabs freeze `[SerializeField]`.** `isMaster` and interact colliders are authored
  on the kit instances, not pushed at spawn.
- Existing EditMode tests call `TriggerDraw` directly — they stay valid and now run in
  Terminal mode. Hatch-loss tests that call `Ship.Hatch.Damage` still end the Run; enemy
  swings through `DamageHatch` do not.

## Depends on / Unblocks

Depends on M47's authored **T**/**D** graph in `Bastion.unity` (playable, not necessarily
folder-closed). Unblocks M53 (Hatch-bank) and M54 (Master Control).
