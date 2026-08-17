# 4 — Embark at Wave 2

<!--
Purpose: NAV start Wave 2 because the first Hatch-bank started (loss counts).
Skip, not chain. BankCap stays on waves converted, not the skip wave. CREW
lights after Wave 2 has started once.
Status: demo spec
Authority: implementable
Update trigger: when skip-vs-chain is overridden, when Protocol beat is moved
Last updated: 2026-08-16
Depends: voice.md, 2-first-hatch-bank.md, 3-ship-after-first-loss.md (same grain)
-->

## What the player sees

NAV may start Wave 2 because the Hatch-bank in beat 2 **started**. The selector
**skips** to that Wave; it does not dump Waves 1..N with no Prep. Party lead
still commits; guests Ready; 5 s countdown.

After they have **started** a Wave 2 assault once and returned to the Ship
(Lost or later), CREW can be repaired and Default claimed at 0 salvage.

## Why this beat exists

Live `DeepestWaveReached` rises on Assault **clear** (`NotifyAssaultCleared`).
Dying on Hatch-bank does not raise it, so NAV stays capped at 1. Live
`StartRun(N)` **chains** 1..N (`M14`, `DeepStartTests`). `Play/The-Ship.md`
says that chain is the point of the picker. This demo’s checkpoint is last
Hatch-bank **started**, and skip is the onboarding read (do not make them
refight Wave 1 shapelessly).

`HatchBank.BankCap` uses `DeepestWaveReached` (host) / wallet deepest (guests).
If NAV skip uses the same int, guests can bank Wave 2’s expected pile after
only converting Wave 1.

## In tree today

| Piece | Fact |
|---|---|
| Picker | `MapStation`: `deepest = max(1, DeepestWaveReached)` clamped to `MaxWaves` (5). `_startWave` clamped to 1..deepest |
| Party | `NetPartyLobby.PendingStartWave`. Lead pick only. Guests read-only destination (`M72` c6) |
| Start | `GameController`: `PendingStartWave` from session, else lobby NV, then `Sim.StartRun(startWave)` |
| Chain | `TriggerDraw(Terminal)` if `CurrentWaveIndex==0 && _startWave>1` sets `_chainActive` through `_startWave-1` |
| BankCap | Sum of `NextWave(i,0,1).enemyCount * 25` for i in 0..deepest-1. Applied only `PlayerCount>1` |
| Graph restore | Deep start restores `CompletedTerminalIds` from last **successful** Hatch-bank (`M54` c6). Wave-1 start does not restore |
| CREW | Live. This demo still dark until `DemoCrewPowered` |
| `M70` | Default 0-salvage claim, Barrage `G` / Rush `H`. Not closed |

Example from the demanded checkpoint: cleared field W3, died on that Hatch-bank
→ may start W4. Died on field W3 with no newer Hatch-bank → cap stays at last
Hatch-bank started.

After beat 2: field cleared W1, Hatch-bank started, died → `DemoNavUnlockWave=2`,
`DemoBankCapWave=1`.

## The change

### NAV picker

While `DemoActive`:

- Max start wave = `DemoNavUnlockWave` (not `DeepestWaveReached`).
- Default the picker to that max (they should not have to discover Wave 2).
- Hide or disable waves above max (existing clamp).
- Panel copy: `start Wave 2 — the Hatch-bank you began counts` when max≥2.
- Play `demo.embark_wave2` Party Lock the first time the lead opens powered NAV
  with `DemoNavUnlockWave >= 2`.

While `!DemoActive`: restore `DeepestWaveReached` + chain (`The-Ship.md`).

### Skip, not chain

`RunSim.StartRun` / first `TriggerDraw(Terminal)`: if `DemoActive`, do **not**
set `_chainActive`. `StartRun(2)` launches Wave 2 only (`CurrentWaveIndex` starts
so the first LaunchWave is index 1).

Implementation: set `CurrentWaveIndex = startWave-1` as today, but skip the
block:

```
if (CurrentWaveIndex == 0 && _startWave > 1) { chain... }
```

when `Meta.DemoActive`. First draw at startWave 2: `CurrentWaveIndex` is 1 after
launch, composition `NextWave(1)` = 30 bodies, 4 Hunters, 0 Thieves, HP mul 1.40
(`EnemyHealthPerWave` 0.40).

Log `[Demo] start skip wave=2 chain=0`.

### BankCap

While `DemoActive` && `PlayerCount>1`:

- Host `CreditSlot(0)` uses `DemoBankCapWave` not `DeepestWaveReached`.
- Guests: wallet deepest already set to `DemoBankCapWave` on Hatch-bank start
  (beat 2). Do not `SetDeepest` to 2 on skip embark.

BankCap(1) = 18×25 = 450. BankCap(2) = 18×25 + 30×25 = 1200, only after a
Hatch-bank **starts** following a Wave 2 field clear.

### Graph restore

`StartRun(2)` today restores completed terminals from last **successful**
Hatch-bank. Beat 2 was a **loss**, so snapshot should be empty: T1 (or T2) is
**not** complete, D1 still shut. They replay the south block at Wave 2
difficulty. That matches “same rooms, harder Wave.”

If a later survived Hatch-bank saved ids, restore as live (`M54` c6). Do not
invent a restore-on-Hatch-start; loss does not complete the terminal for the
door graph unless it already completed before the Hatch-bank. Field terminal
completion happens on Wave clear (`BastionUnlockGraph.TryCompleteTerminal`) —
T1 **is** complete in the **Run** they lost, but Meta snapshot writes on
successful Hatch-bank. Confirm persist point: only success. Loss → Wave-2 start
with empty `CompletedTerminalIds` → T1 interactable again.

**Spec:** Wave-2 skip start after a **lost** first Hatch-bank: empty graph,
T1/T2 up, D1 shut. They may Hatch-bank-first again after one terminal (beat 5
repeats the unit, including Hatch-bank-first until D1’s AND is satisfied).

### Default Protocol (extra beat)

Set `DemoWave2Seen = true` on host+guest Meta when any Wave 2 assault **starts**
(field `LaunchWave` with index 1, or Hatch-bank carry 2 — first time is the
field skip). Persist on that start.

Next Docked with `DemoWave2Seen && !DemoCrewPowered`: CREW prompt
`[E] repair CREW QUARTERS`. Hold 1.5 s → `DemoCrewPowered`, `demo.crew_repair`,
then the live CREW panel. Claim Default via existing `M70` 0-salvage path.

Do not auto-equip Default in the capsule. If `M70` is not closed, this beat
**depends** on the claim command existing in tree; do not rebuild CREW.

`demo.crew_ready` Local Lock once when they first Dock with `DemoWave2Seen` and
CREW still dark (the Ship visit after Wave 2 started — could be the loss from
Wave 2 or a later visit). If they claim before the next embark, Barrage/Rush
are live on that next fight.

NAV still does not require protocol.

## Create

| Path | Role |
|---|---|
| `DemoProgress.NavMax(meta)` | `DemoActive ? DemoNavUnlockWave : DeepestWaveReached` |
| `DemoProgress.BankCapWave(meta)` | demo vs live |
| Tests | Skip StartRun(2) launches one Wave at index 1; chain off; BankCap 450 after beat 2 |

## Change

| File | Change |
|---|---|
| `MapStation.BuildPanel` | Picker max from `DemoProgress.NavMax`. Default `_startWave` to max when demo and max>1 |
| `RunSim.TriggerDraw(Terminal)` | No chain when `DemoActive` |
| `RunSim.StartRun` | Clamp startWave to `NavMax` |
| `RunSim.Wallets.CreditSlot` | BankCap argument `DemoBankCapWave` when demo |
| `GameController` / `LaunchWave` hook | Set `DemoWave2Seen` |
| `CrewQuartersStation` | Dark until `DemoCrewPowered`; hold-repair like ARMORY |
| `ShipCommands` protocol claim | Unchanged once CREW is open |
| `Play/The-Ship.md` | One **Gaps** line: demo skip while `DemoActive`; chain after first Escaped. Do not rewrite the live picker paragraph as if skip were the campaign |

`DeepStartTests`: add a demo-flag case or a parallel `DemoSkipTests`. Do not
weaken the live chain tests.

## Delete

Nothing.

## Numbers

| Name | Value | Source |
|---|---|---|
| Unlock after beat 2 start | NAV 2, BankCap 1 | beat 2 |
| Wave 2 solo dial 0 | 30 bodies, 4 Hunters, 0 Thieves, HP ×1.40 | `NextWave(1,0,1)` |
| BankCap(1) | 450 | 18×25 |
| BankCap(2) | 1200 | 18×25+30×25 |
| InterWaveGap (unused on skip) | 3 s | `WaveDirector` |
| Countdown | 5 s | `NetPartyLobby.CountdownSeconds` |

## Multiplayer

| Event | Authority |
|---|---|
| Start wave | Lead’s picker (`PendingStartWave`). Guest NAV read-only. Guest `DemoNavUnlockWave` 1 (joined late, never started a Hatch-bank) does **not** pull the party back to 1 |
| Guest BankCap | Their wallet deepest. Late joiner with deepest 1 fighting Wave 2: cap 450. Excess lost, not redistributed (`HatchBank.BankCap` comment) |
| Ready / countdown / join refuse | Unchanged `M72` |
| `demo.embark_wave2` | Party, lead opened NAV |
| CREW repair | Local, after `DemoWave2Seen` (set on all seats in the Run when Wave 2 starts — host writes each Meta if guests persist locally; guests set their own flag on replicated wave-start) |

Wave-start is host sim. Guest Meta `DemoWave2Seen`: set from the replicated
assault start (existing net wave mirror). If a guest was on `NetShipWait`, they
still get the flag (they were in the Run).

Lead disconnect: session ends.

## Voice

`demo.embark_wave2` Party Lock, first NAV open at unlock≥2.
`demo.crew_ready` Local Lock, first Docked with Wave2Seen and CREW dark.
`demo.crew_repair` Local Lock, CREW repair complete (then they click claim).

Fight: `G`/`H` already on Fight shortcut (`M70`). No extra Assault VO for
Protocol.

## Driven / Shown / Reads-back

- **Driven by** lead EMBARK at Wave 2; CREW hold + claim.
- **Shown** picker default 2; skip (one Wave, Prep after clear, not stacked 1+2);
  CREW prompt after Wave 2 seen.
- **Reads-back** `[Demo] start skip wave=2 chain=0`, `[Demo] wave2 seen`,
  `[Demo] CREW powered`, existing `[Protocol]` / Ship claim logs from `M70`.

## Logs

| Event | Line |
|---|---|
| Skip start | `[Demo] start skip wave=2 chain=0` |
| Wave 2 launch | existing WaveStarted plus `[Demo] wave2 seen` once |
| BankCap used | existing wallet logs; deepest must print 1 not 2 on first post-loss Hatch-bank |
| CREW | `[Demo] CREW powered` |

## Traps

- Clamping `StartRun` to `DeepestWaveReached` (still 1) and ignoring
  `DemoNavUnlockWave` — they cannot pick 2.
- Chain still on because `CurrentWaveIndex==0` at StartRun(2) before launch —
  read the actual `StartRun` index init; skip must be tested, not inferred from
  comments.
- Restoring T1 complete on a lost Hatch-bank (would open D1 with T2 later
  without the intended south-block Wave 2).
- Setting guest deepest to 2 on embark.
- Requiring Ready on CREW claim.
- Auto-own Default when CREW powers, skipping the 0-salvage click (`M70` kill:
  CREW must be a purchase you walk to).

## Depends / Unblocks

Depends: beat 2 checkpoint fields; beat 3 ARMORY (same milestone recommended);
`M70` claim in tree for CREW. Unblocks: beat 5 repeating unit at Wave 2+.

## Verification

1. EditMode: demo Meta navUnlock=2, deepest=1 → picker max 2; `StartRun(2)` one
   wave index 1; `ChainPending` false; BankCap(host) 450.
2. Existing `DeepStartTests` still green for `DemoActive false`.
3. PlayMode: after beat 2 loss, NAV defaults to 2; EMBARK; first bodies are
   Hunters+Defaults of Wave 2 (log spec `hunterCount=4` solo); no Wave 1 dump.
4. Loopback: guest with bankCapWave 1 cannot exceed 450 converted on a later
   Hatch-bank until they start a Hatch-bank at a higher field wave.
5. CREW stays dark until a Wave 2 start log exists on that save.
6. No `[Dev]`.

## Rejected

Keeping chain as the demo picker (`The-Ship.md` live behaviour waits until
`!DemoActive`). Gift-completing T1 on loss. Shared protocol loadout.
Host-forced EMBARK without Ready (`M72` c5).
