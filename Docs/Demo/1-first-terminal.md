# 1 — First terminal

<!--
Purpose: First Bastion Prep: explore, one field terminal, Wave 1 Defaults, no juice.
Then send them to the Hatch, not the next door.
Status: demo spec
Authority: implementable
Update trigger: when T1/T2 first-terminal rule changes, when juice-hide scope changes
Last updated: 2026-08-16
Depends: voice.md, 0-wake-on-a-dead-ship.md
-->

## What the player sees

EMBARK. Quiet Hatch. Copy: explore, find a field terminal, wake the place; Salvage
from the fight repairs the Ship. Look at the terminal. **No juice, no `+/-`.** Hold
`E` selects, `T` starts Wave 1. Defaults hunt the Boarder. After the clear: another
terminal unlocks the next doors — **Hatch-bank first.**

They do not hear intensity, dial, or siphon.

## Why this beat exists

The live prompt is `[+/-] juice` and `[E] select` (`BastionTerminal`). The live graph
lets them walk T1 then T2 then D1 without a Hatch-bank. The demo’s unit is one Wave
then Hatch-bank. Juice is a bet the first session has not earned.

## In tree today

| Piece | Fact |
|---|---|
| Prep until `E` then `T` | `M52`. `GameController` arms on Interact Down, `T` calls `TriggerDraw` |
| Wave 1 | `WaveDirector.NextWave(0, 0, n)`: 18 Defaults per player at dial 0, 0 Hunters, 0 Thieves (`HunterPerWave[0]=0`, `RamPerWave[0]=0`). HP mul 1.0. Group size 3 |
| Prompt | `[+/-] Terminal T1 — Wave (k/n)` plus `Intensity L juice 100 unbanked … bodies` (`BastionTerminal.Prompt`, `AppendStake`) |
| `wavesToComplete` | Code default **1**. Play / Field Manual still say T1 is **5** (docgen/hardcoded). Sketch emit uses 1. Demo uses **1** |
| Juice | `IntensityJuice.Try`: Prep, at trigger, costs 100 Unbanked per step, `MaxLevel` from siphon. First Wave Unbanked is 0 so juice already fails for lack of pile — the **prompt still teaches the verb** |
| Doors | D1 requires T1 **and** T2 (`bastion-defense-layout.txt`). Completing T1 does not open D1 |
| Mission drawer | Tab, auto-open once per Run, prep/assault chrome |
| `MsgGeneral` | `"arriving at the bastion..."` then cleared (`BastionHudView`) |
| `MsgPlayer` | Hatch-under-attack only |
| Sequencer | None. No “go to Hatch” after one terminal |
| Deep start | `StartRun(1)` normal. Party `PendingStartWave` from lead |

Kit south block: Hatch on R1 south; T1 in R2 west; T2 in R3 east; both reachable
without D1.

## The change

### Juice withheld

While `DemoActive`:

- `IntensityJuice.Try` returns false immediately with
  `[Demo] juice refused — demo withheld` (before unbanked/look-at checks).
- `BastionTerminal` Prompt: no `+/-`, no `AppendStake` intensity/juice/foresight
  roster. Armed: `[T] Terminal T1 — start Wave (0/1)` then `[E] edit` omitted if
  there is nothing to edit. Unarmed: `[E] Terminal T1 — select`.
- Master Control prompt in beat 6 uses the same hide.
- `GameController` juice keybinds (`+/-`) no-op with the same log (rate-limit:
  once per 2 s per seat).

### One terminal, then Hatch

A `DemoRun` (Game, lives on `GameController`, host-owned) tracks this Run:

| Flag | Meaning |
|---|---|
| `FirstFieldId` | First Field terminal `E`-selected this Run while `DemoActive` && `DemoNavUnlockWave == 1` |
| `FirstFieldCleared` | That terminal’s `wavesToComplete` reached |

Rules:

1. Any Field terminal may be the first (T1 or T2). Do not hard-lock T1 if they
   wander east.
2. After `FirstFieldCleared`, other Field terminals refuse `T` with
   `[Demo] start refused — Hatch-bank first` and prompt
   `Terminal T2 — Hatch-bank first`. Looking at them still allowed.
3. Hatch look-at after that clear plays `demo.after_wave1` once (Lock, Prep) if
   they have not started Hatch-bank yet. If they walk to Hatch without looking at
   a second terminal, same line on Hatch look-at.
4. Doors do not open from a single terminal (already true for D1). Do not add a
   special open.

### Copy / HUD

- Replace first-Run `MsgGeneral` `"arriving at the bastion..."` with nothing, or
  with the `demo.embark_first` subtitle already playing. Do not stack both.
- Mission drawer: still fight chrome. Do not put story objectives in it.
- `demo.explore`: Subtitle, Party, once, if 45 s of Prep elapse with no Field
  terminal looked at (`Interactor` current is not a Field `BastionTerminal`).
- `demo.terminal_look`: Lock, Party, first time any seat looks at a Field
  terminal this Run (host broadcasts). Continue/advance then they `E`/`T`.

### Wave 1 combat

No composition change. Defaults hunt the Boarder on a field-terminal Wave
(`EnemyGatekeeper` targeting). Dial stays 0. `ArmSequentialDraws(1)` only.

Salvage: 25 per kill (`MetaUpgrades.SalvagePerKill`). 18 bodies × 1 player = 450
Unbanked if every Default pays. At `playerCount` 2, `NextWave` doubles bodies
(18×2) and wallets split (`M65`). Do not retune Wave 1 here.

## Create

| Path | Role |
|---|---|
| `Assets/Scripts/Game/Demo/DemoRun.cs` | Host state for this Run: first field id, juice hide, explore timer, Hatch-bank-first refuse |
| `Assets/Scripts/Game/Demo/DemoHud.cs` | Prompt builders used by `BastionTerminal` when `DemoActive` |

Keep `DemoRun` out of Sim. Flags that persist are already on `MetaState`.

## Change

| File | Change |
|---|---|
| `IntensityJuice.Try` | Early out when `sim`/meta says demo withheld. Sim cannot read `GameSession`. Pass a `bool juiceAllowed` into `Try` **or** have `GameController.TryJuice` refuse before calling Sim. Prefer Game-layer refuse so Sim stays free of demo flags |
| `GameController` juice + start | Refuse juice. After first field clear, refuse `T` on other Field terminals |
| `BastionTerminal.Prompt` | Branch on `DemoActive` |
| `BastionHudView` | Skip or replace arriving message on first demo Run |
| `GameController` start of Run | Reset `DemoRun`. Play `demo.embark_first` if not already played on Ship |

Do **not** change `WaveDirector` in this beat.

## Delete

Nothing.

## Numbers

| Name | Value | Source |
|---|---|---|
| Wave 1 bodies (solo, dial 0) | 18 Defaults | `BaseEnemyCount + CountPerWave*0` |
| Salvage / kill | 25 | `MetaUpgrades.SalvagePerKill` |
| Expected Unbanked if full clear | 18×25 = **450** | arithmetic |
| Juice cost (withheld) | 100 / step | `IntensityJuice.CostPerStep` — unused |
| Explore nag | 45 s Prep | this spec (same magnitude as Hatch-bank window, not a sim constant) |
| Terminal waves this Interact | 1 | `BastionTerminal.wavesToComplete` default |

## Multiplayer

| Event | Authority |
|---|---|
| Juice key | Each seat local; Sim would apply on host. Refuse on host when any seat presses, and refuse locally so the prompt does not lie |
| `E` select / `T` start | Existing host sim. Party: whoever is at the terminal starts the Wave for everyone (`M52` / net verbs — do not recut) |
| First-field id | Host `DemoRun`. First `E` select wins |
| Other terminal `T` | Host refuse, all seats see Hatch-bank-first prompt (layout replicated; prompt is local string from host phase) |
| `demo.terminal_look` | Party. Do not lock guests who are already fighting if the look-at happens after `T` — if Assault, skip Lock and do not play |
| Guest `NetShipWait` | Not this beat (no Hatch-bank yet unless they skip — they cannot) |
| Lead start wave | 1. Guest deepest does not apply (`M72` c6) |

Scale: `playerCount` from connected seats in the Run. Wave 1 at 2 players is 36
Defaults (`WaveDirector` `* n`). That is the live rule; do not special-case demo.

## Voice

`demo.embark_first` Prep Lock Party (if not played on Ship).
`demo.explore` Prep Subtitle Party, 45 s nag.
`demo.terminal_look` Prep Lock Party, first Field look.
`demo.after_wave1` Prep Lock Party, on Hatch look-at after clear (or immediately on
clear if they are already looking at Hatch).

Assault: no locking line. Optional none.

## Driven / Shown / Reads-back

- **Driven by** `E` then `T` at one Field terminal.
- **Shown** prompt without juice; after clear, Hatch-bank-first on other terminals.
- **Reads-back** `[Demo] first field id=T1`, `[Demo] juice refused — demo withheld`,
  `[Demo] start refused — Hatch-bank first`, existing `[Run]` start/clear lines.

## Logs

| Event | Line |
|---|---|
| First select | `[Demo] first field id=T1` |
| Wave start | existing Run/Terminal start (keep) |
| Clear | `[Demo] first field cleared id=T1 unbanked=<n>` |
| Juice | `[Demo] juice refused — demo withheld` |
| Second terminal T | `[Demo] start refused — Hatch-bank first` |

## Traps

- Juice prompt teaching the verb even when Unbanked is 0. Hide the prompt, not
  only the spend.
- Foresight roster on `AppendStake` leaks Hunter/Thief counts. Hide the whole
  stake line.
- `wavesToComplete` authored 5 on a live scene object would make “one Wave” false.
  Sketch default is 1; **verify on the emitted Bastion** (`BastionTerminal`
  inspector / log `wavesToComplete` at register). If a terminal is 5, set it to 1
  in the same change as this beat (scene/prefab), do not retune Sim.
- Completing T1+T2 before Hatch-bank would open D1 and skip beat 2’s “Hatch
  first.” The `T` refuse on the second terminal is load-bearing.
- `MsgGeneral` + Lock conversation stacking.
- Putting objectives in the mission drawer (`M62` chrome).

## Depends / Unblocks

Depends: beat 0 EMBARK with MK-1. Unblocks: beat 2 Hatch-bank with a non-zero
Unbanked pile (Hatch-bank already refuses `unbanked=0`).

If they start Hatch-bank with 0 kills, existing refuse `[Run] hatch-bank refused — unbanked=0`.
Do not special-case; `demo.after_wave1` still sends them, they go back and fight.

## Verification

1. EditMode: prompt builder omits `juice` and `Intensity` when demo flag true.
2. EditMode: `IntensityJuice` not called when Game refuses (or Try sees allowed=false).
3. PlayMode: one Field `T` → Wave 1 → Prep; second Field `T` refused; Hatch `E`/`T`
   still legal (beat 2). Log `first field cleared` with `unbanked` > 0 on a full clear.
4. `+/-` during Prep logs withheld, siphon level stays 0.
5. Two-player loopback: body count 36 at dial 0 (`WavePlayerCount` already proves
   the formula; this beat logs `playerCount=2` on start).
6. No `[Dev]`.

## Rejected

Teaching juice “as a preview at 0.” Sequential 5-Wave T1. Auto-starting Wave 1.
A ghost trail to T1. Companion pointing at the terminal.
