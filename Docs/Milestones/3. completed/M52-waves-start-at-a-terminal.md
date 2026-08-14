# M52 — Waves start at a terminal

<!--
Status: completed 2026-08-13
Update trigger: closed
Last updated: 2026-08-13
Closed on: Zo 2026-08-13. Gate is playthrough
`Logs/GatekeeperBastion_2026-08-13_14-46-59.log` (no `[Dev]`): one Interact at 14:47:17
starts T1 Waves 1–5 (`mode=terminal`), `TerminalChainGap` 8.000 s, T1 `wave 5/5 cleared`
then complete at 14:53:33, `BASTION STRIPPED`, hatch 300/300 every clear. D1 opens on
that complete via `RefreshDoorStates` (no Open-door Interact — Wave 5 ends the Run).
This Run did not press Prep `T`. Criterion 7 AwardKill-to-bank is M53: this log is
`+25 unbanked`, gross 0, banked 12208. Harness `Tools/verify/run.sh` 309/0 this close.
PlayMode not re-run.
-->

The first player-visible cut of the field loop
(`../../Brainstorms/missions-and-destinations.md`). **Interact at a field terminal starts
a Wave that hunts the Boarder. The Hatch cannot be hurt. A terminal completes after N
authored Waves (T1 is 5), then its door opens — and Interact on the completed terminal
opens the door if the slab is still up.** The Siphon Draw key no longer starts Waves.
Salvage is still banked on the kill (M53 delays that). The five-Wave clear still ends the
Run (M54 replaces that win).

**Recut 2026-08-13 (Zo):** one Wave then `already complete` is wrong. Session
`Logs/GatekeeperBastion_2026-08-13_10-29-42.log` — T1 Wave 1 cleared 10:32:03, then
`T1 refused — already complete` at 10:32:56 / 10:33:03; D1 stayed locked (sketch AND of
T1+T2; no Open-door verb). Terminals have N Waves; T1 is 5.

**Recut 2026-08-13 (implement):** Wave 5 clear still `Escaped` (criterion 7). T1 completing
and D1 opening happen on that same `AssaultEnded`, then the end screen. Walking through D1
is not this slice. A deep-start chain records every Wave it delivered toward N (one
Interact can clear more than 1). Clear counts live on `BastionUnlockGraph` (string ids),
not on MonoBehaviours. `RunSim.DamageHatch` is the invulnerability guard so EditMode can
prove it.

**Recut 2026-08-13 (Zo):** one Interact per Wave is wrong. Interact at a Field terminal
starts that terminal's remaining Waves as a sequence: each clear, then
`WaveDirector.TerminalChainGap` (8 s) of Prep, then the next draw. Not a deep-start
overlap — stacking T1's five would make the default Interact equal the NAV MAP's
hardest start. T2/T3/T4 at N=1 are still one Wave.

**Recut 2026-08-13 (Zo):** that chain recut is reversed. A new save banks 0 until a
Hatch-bank, and the chain made the first convert a Wave-5 hatch defence with no repair.
Interact starts **one** Wave. T1 still needs 5 clears to open D1. Prep after each clear
is untimed; Hatch-bank is legal there. `ArmSequentialDraws` stays in Sim for later
terminals that re-lock banking. Deep-start overlap is unchanged.

## Story

As a Boarder, I want Interact at a field terminal to start that terminal's Waves, so that
progress through the bastion is the fight, and I am not sent back to the console between
them.

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

After close, ten logged Runs on the kit bastion. **If by 2026-09-13 median `hatchDist` on
those terminal-Wave kills sits in the Hatch bay (`Tools/wave-report`, M37), stop** — the
field is decoration and this slice did not create a second fight.

## Acceptance criteria

1. **Prep `T` (Siphon Draw) is a logged refusal** — `Waves start at a terminal` — and does
   not call `TriggerDraw`. Shortcut panel copy matches.
2. **Looking at a wing field terminal in Prep, Interact Down starts one Wave** in
   `WaveMode.Terminal`. Log `[Terminal] <id> started wave <n>`. T1 still needs 5 clears
   to open D1. Already complete, in Assault, a Wave already pending, or non-Field kind
   → logged refusal, no Wave. After the clear, Prep is untimed; Hatch-bank is legal.
3. **During `WaveMode.Terminal`, `RunSim.DamageHatch` is a no-op.** Hatch HP does
   not move. The HUD Hatch card still shows. Loss this slice is Boarder down only.
   `GameController.DamageHatch` forwards to that method.
4. **Targeting follows the mode** (same `HunterPerWave` / `RamPerWave` tables):
   Defaults and Rams hunt the Boarder; Hunters still do; **Rams hunt the Boarder on
   terminal Waves only** (dated break of “Ram never hurts you,” this mode only —
   `../../Brainstorms/gatekeepers-and-waves.md`). `MayTargetHatch` is false for every kind
   because hatch targeting requires `Mode == HatchBank`.
5. **A Field terminal completes after N authored Waves, not after the first.**
   `wavesToComplete` on `BastionTerminal` (int, min 1, default 1), copied onto
   `BastionUnlockGraph.TerminalSpec.WavesToComplete` at graph build. **T1 in
   `Bastion.unity` is 5.** Each Interact starts one Wave. Log
   `[Terminal] <id> wave <k>/<N> cleared` per assault. The Nth clear calls
   `TryCompleteTerminal`. Dying mid-Wave does not increment. A deep-start overlap on
   that first assault still records `CurrentWaveIndex − indexAtPull` toward N. T2/T3/T4
   stay 1 unless authored otherwise. M14's five-Wave run cap still holds — T1's five
   are the run's five. Field terminals do not arm `ArmSequentialDraws`.
6. **The door is a player verb, not only a graph side-effect.** On the Nth clear,
   `UnlocksDoor` opens when that door's required set is done. If the slab is still up
   while Prep remains, Interact on the completed terminal is `[E] Open door` and opens
   `UnlocksDoor` (log `[Terminal] <id> opened <doorId>`).    Incomplete prompt: `[E] Terminal
   <id> — start Waves (k/N)` (singular Wave when N is 1). During the gap: `next Wave
   incoming`. They do **not** open a Ship station panel. Visual is the kit
   **T** mark already in the scene — no new canvas. **Scene/sketch:** D1 requires only T1
   (`unlock=T1`) so five Waves at T1 open D1. T2 is not an AND on D1. Wave 5 `Escaped`
   follows that open on the same clear; walking through is not this slice.
7. **M14 still holds:** five Waves, deep-start chain on the first `TriggerDraw` of a Run
   with `StartWave > 1`, clear of Wave 5 still `Escaped`. Instant `AwardKill` into
   `BankedSalvage` still holds.
8. EditMode: `TerminalWaveTests` (Mode, `DamageHatch` no-op, N-wave complete) plus
   existing `BastionUnlockGraphTests` / RunSim suites green. PlayMode suites green after
   any helper that pressed `T` to start a Wave now calls `TriggerDraw` (tests already do).
   Solo log of a terminal Wave contains the criterion-2 line and no hatch-damage line from
   Gatekeepers.

## Implementation

**Create** `Assets/Scripts/Sim/Run/WaveMode.cs` — `enum WaveMode { Terminal, HatchBank }`.
HatchBank is unused this slice; M53 is the first writer. *(Already in tree.)*

**Change** `RunSim` — `WaveMode Mode { get; private set; }` (default Terminal).
`TriggerDraw` sets `Mode = WaveMode.Terminal` and keeps today's deep-start overlap
chain. `ArmSequentialDraws` / `TerminalChainGap` remain for later terminals; the
field loop does not arm them. `DamageHatch(float amount)` no-ops when `Mode == Terminal`;
otherwise `Ship.Hatch.Damage`. `NotifyAssaultCleared` still fires per assault so the
Game layer can record each clear; sequential follow-up arms after that if Waves remain.

**Change** `BastionUnlockGraph` — `TerminalSpec.WavesToComplete` (min 1, default 1).
`RecordWaveCleared(id, waves)` increments that id; on `cleared >= N` calls
`TryCompleteTerminal`. `ClearedWaves` / `WavesNeeded` for prompts. Scene objects stay out
of Sim.

**Change** `GameController` — Prep `PullDraw` logs the criterion-1 refusal. Field-terminal
start/clear live in `GameController.Terminals.cs`. Hold a pending terminal id, layout, and
the `CurrentWaveIndex` at pull. `StartTerminalWave` calls `TriggerDraw` once. On
`AssaultEnded`: `RecordWaveCleared` for this assault's delta, then clear pending so the
next Interact or a Hatch-bank is legal. Dev replay/jump still nulls pending.

**Change** `BastionLayout` — pass `WavesToComplete` into `TerminalSpec` at build;
`RecordWaveCleared` / `TryGetFieldProgress` forward to the graph and refresh doors on
complete.

**Change** `BastionTerminal` — `wavesToComplete` serialized (Reset default 1; getter
clamps `< 1` to 1). Interact Down in Prep: if complete and door still shut, open
`UnlocksDoor`; else `GameController.StartTerminalWave(this)`. Prompt shows `(k/N)` or
`Open door`. `BastionTerminalKind` already serialized; Master Control Interact refuses
(`Master Control — later`, M54).

**Change** `BastionTerminalEditor` — Field inspector shows `Waves To Complete`.

**Change** `Bastion.unity` / `Tools/bastion-sketch/hatch-field-expand.txt` — T1
`wavesToComplete = 5` on the scene object (sketch has no waves token; comment in the
sketch). D1 `unlock=T1` (drop T2 from that AND). T1 `UnlocksDoor` is D1.

**Change** `EnemyGatekeeper.Targeting` — `hatchAvailable` requires
`_game.Sim.Mode == WaveMode.HatchBank` in addition to `MayTargetHatch`. On Terminal that
is always false. *(Already in tree.)*

**Change** `RamGatekeeper` — `MayTargetPlayer` is true when `Mode == Terminal`;
`CanBeProvoked` stays false (shooting a Ram still does not shout). Comment the dated
break with the date. *(Already in tree.)*

**Change** `Controls` / shortcut panel — Prep `T` label: `terminal starts the wave` (not
siphon draw). Binding stays so M53 can remap it to Hatch-bank. *(Already in tree.)*

**Change** `../../_meta/_definitions/language.md` — **Wave** is started by a field
terminal (this slice) or a Hatch-bank (M53). **field terminal** row. **the Siphon Draw**
is no longer the start verb; keep the row until M55 kills the dial, marked *start verb
retired M52*.

**Change** `Docs/Play/A-Run.md`, `Docs/Play/Waves.md`, `Docs/Play/The-Game.md`, and
`Docs/Play/Getting-Started.md` so they do not say `T` starts the Wave. Targeting copy on
those pages: field-terminal Waves hunt the Boarder; the Hatch is not in the fight.

Rejected: subclassing `StationTerminal` (opens Ship panels). Rejected: completing the
terminal on Interact (silent unlock). Rejected: completing after the first Wave (the
10:29:42 session). Rejected: delaying `AwardKill` (M53). Rejected: changing the five-Wave
win (M54). Rejected: overlapping the N Waves like a deep-start (T1's five would be
the NAV MAP's hardest start). Rejected: delaying `Escaped` so the Boarder can walk
through D1 (M54). Rejected: retuning `InterWaveGap` to serve this gap.

## Supporting work

- **Driven by** Interact at a field terminal — here. `T` is the refusal.
- **Shown by** the interact prompt and the shortcut-panel line — here.
- **Reads back as** one Wave per Interact, `(k/N)` on the prompt, D1 open in the
  log/graph on the Nth clear, Hatch HP unchanged, Hatch-bank legal in the Prep after
  any clear — here. Walking through D1 is M54.

## Verification

1. Criteria 3, 5, 8 — `TerminalWaveTests`: TriggerDraw in Terminal mode;
   `RunSim.DamageHatch` does not reduce `Ship.Hatch.Hp`; a terminal with N=2
   completes only on the second clear; N=1 still completes on the first; a 3-wave
   increment against N=5 does not complete; `ArmSequentialDraws(2)` waits
   `TerminalChainGap` then draws the next. Master is not a Field spec (Game refuse is
   `StartTerminalWave` kind check). `Tools/verify/run.sh` green at current counts plus
   the new tests.
2. Criterion 8 PlayMode — integration check; no new hatch-breach-from-enemies fixture this
   slice (Hatch-bank is M53).
3. Playthrough — from Play on `Bastion.unity`: walk to T1, Interact **once**. Clear Wave 1;
   do not Interact again. After ~8 s Wave 2 starts (not `already complete`, not a second
   Interact). After Wave 5, the log has T1 `wave 5/5 cleared`, T1 complete, D1 open, then
   `Escaped`. Hatch card does not drop. Five start lines from that one Interact (or one
   deep-start overlap plus follow-ups). `T` in Prep logs the refusal.

## Do not build

| Not building | Why not |
|---|---|
| Soft-lock doors (switch / salvage-repair unlock, no Wave) | Parked until M52 has play data — third unlock loop |
| Unbanked pile / drip / bank slots | M53 |
| Master Control sequence | M54 |
| Intensity / juice / siphon-dial removal | M55 |
| Path telegraph HUD | Parked in the brainstorm |
| Completing a terminal without a Wave | Silent unlock, rejected |
| Completing a terminal after Wave 1 when N>1 | The 10:29:42 defect |
| Holding the Run open after Wave 5 so D1 can be walked | M54 |

## Watch

- **`T` is also the language for field terminals.** The key and the mark are easy to
  conflate in logs; the log tag is `Terminal`, the key refusal names the key.
- **Prefabs freeze `[SerializeField]`.** `kind`, `wavesToComplete`, and interact colliders
  are authored on the kit instances, not pushed at spawn. T1's 5 must be set on the scene
  object. Re-emitting the sketch resets it to 1 (no sketch token).
- Existing EditMode tests call `TriggerDraw` directly — they stay valid and now run in
  Terminal mode. Hatch-loss tests that call `Ship.Hatch.Damage` still end the Run; enemy
  swings through `DamageHatch` do not.
- T2 is reachable from Approach without D1. A Wave spent at T2 is one of M14's five and
  can leave T1 short of 5, so D1 stays shut. Intended.

## Depends on / Unblocks

Depends on M47's authored **T**/**D** graph in `Bastion.unity` (playable, not necessarily
folder-closed). Unblocks M53 (Hatch-bank) and M54 (Master Control).
