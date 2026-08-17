# 2 — First Hatch-bank (designed loss)

<!--
Purpose: Convert Unbanked to the Ship, then fight a Hatch-bank that a 0-upgrade
MK-1 is not expected to survive. Loss keeps the converted bank.
Status: demo spec
Authority: implementable
Update trigger: when first-hatch hunter add is retuned (one lever), when
convert-then-fight scope is overridden
Last updated: 2026-08-16
Depends: voice.md, 1-first-terminal.md
-->

## What the player sees

After Wave 1 they are sent to the Hatch. Look, `E` select, `T` start. Salvage
converts **then** the defense starts. At 0 upgrades this fight is not survivable.
They go down. Banked Wave-1 Salvage is already on the Ship.

Party: the first down of **any** seat during this one window ends the Run for
everyone.

## Why this beat exists

Live Hatch-bank **drips** 20 Salvage/s during the 45 s window and **dumps** the
leftover on survive (`HatchBank.DripPerSecond`, `RunSim.DripTick`, `DumpUnbanked`).
Down during the window **wipes remaining Unbanked** (`EndRun` → `WipeUnbanked`).
Already-dripped credits stay. A designed loss that dies in the first seconds
returns ~0 banked. The ARMORY beat needs Wave 1’s 450 on the Ship.

`WeaponTier` already sizes the first two Secondary purchases (150+300) to one
Wave-1 clear at Scavenging 0. Convert-then-fight makes that arithmetic true on a
loss.

## In tree today

| Piece | Fact |
|---|---|
| Start | Hatch `E` then `T` → `TriggerDraw(WaveMode.HatchBank)` (`M53`) |
| Refuse | Unbanked 0, or `HatchBankSlotsLeft == 0` (starts at 3) |
| Carry | `WavesSinceBank` clamped 1..5. After one field Wave, carry 1 → composition `NextWave(0)` = Wave 1, convert ×1.00 |
| Drip | 20/s while Unbanked > 0 and timer > 0 |
| Dump | On survive (`NotifyAssaultCleared` while Hatch-bank and timer elapsed) |
| Wipe | On `EndRun` (down or Hatch 0). Dripped stays on Meta |
| Timer | 45 s. Enemy wipe and Unbanked 0 do **not** end it |
| Hatch HP | 300 at Hatch Armor 0 (`MetaUpgrades.HatchHp(0)`) |
| Hatch damage | Field-terminal Waves do not apply; Hatch-bank does (`RunSim.DamageHatch`) |
| Defaults on Hatch-bank | March Hatch unless provoked |
| Hunters | Hunt Boarder, 0 Hatch damage, 6.5 m/s, 20 HP, 6 player damage |
| Swing | 0.7 s, first swing on contact |
| Player | 100 HP (`MetaUpgrades.PlayerMaxHp`) |
| MK-1 | 0-cost Secondary, 0 wheel points |
| Down | Host/solo: `PlayerHealth.endsTheRunOnDeath` → `Lost`. Guest: `M57` wait on Ship, host continues (`M49`) |
| Checkpoint | `DeepestWaveReached` rises on **clear**, including a survived Hatch-bank. A loss does **not** raise it |
| Slots | This window spends 1 of 3 |

Wave 1 Hatch-bank (carry 1), solo: 18 Defaults at the Hatch. Hatch dps if all
arrive: 18 × 4 / 0.7 ≈ 102.9 → 300 HP lasts ~2.9 s. A Boarder who holds the
door with MK-1 (18 × 30 HP = 540, MK-1 working range 12 m) can still clear
bodies and live 45 s. That is why this beat **adds Hunters**, not a scripted
`Die()`.

## The change

Three host-only rules, first Hatch-bank of a `DemoActive` save only
(`!DemoFirstHatchConvertDone` / `!DemoFirstHatchSpecUsed`). Later Hatch-banks
in this demo use live drip+dump (README).

### 1. Convert then fight

In `TriggerDraw(WaveMode.HatchBank)` after `SnapshotHatchCarry` and slot spend,
if `DemoActive && !DemoFirstHatchConvertDone`:

1. `DumpUnbanked()` immediately (same `CreditSlot` / `BankedFrom` / `BankCap` as
   a survive dump).
2. Set `DemoFirstHatchConvertDone = true` on host Meta; copy guest converted via
   existing `GuestConverted`.
3. Skip `DripTick` for the rest of this window (`TotalUnbanked` is 0).
4. Log `[Demo] convert-then-fight dumped=<total> mul=<m>`.
5. Do **not** persist Meta yet if the Run still can fail other writes — follow
   existing Hatch-bank persist points; banked Salvage must survive `EndRun`.
   `EndRun` must not reverse `EarnSalvage` already applied.

`DemoFirstHatchConvertDone` is consumed even if they cancel… they cannot cancel
a live Assault. If `TriggerDraw` refused (unbanked 0), do not set the flag.

### 2. Unwinnable spec (one lever)

On `LaunchWave` for that same first Hatch-bank, after `NextWave(carryIndex, …)`:

- Keep `defaultCount` / `ramCount` from Wave 1 (18 Defaults, 0 Thieves, ×
  `playerCount`).
- Set `hunterCount = 4` (the Wave-2 Hunter row, `HunterPerWave[1]`).
- `enemyCount = defaultCount + hunterCount + ramCount`.

Log `[Demo] first-hatch spec H=4 D=<n> R=0 playerCount=<n>`.
Set `DemoFirstHatchSpecUsed = true`.

Kill observation (retune **only** `hunterCount`, not duration, not Default
count, not MK-1 dps): a 0-upgrade MK-1 **solo** log where this window reaches
`NotifyAssaultCleared` with the Boarder alive. If that exists, add **2** Hunters
(4→6) in a later change that says so.

Do not call `PlayerHealth.Die()` from demo code.

### 3. Checkpoint on start

On this `TriggerDraw(HatchBank)` (and every later non-Master Hatch-bank while
`DemoActive`):

- `DemoBankCapWave = max(DemoBankCapWave, CurrentWaveIndex)`  
  After Wave 1, `CurrentWaveIndex` is 1 (terminal launch incremented it).
- `DemoNavUnlockWave = max(DemoNavUnlockWave, CurrentWaveIndex + 1)` → 2.
- Guest wallet `SetDeepest(slot, DemoBankCapWave)` so `BankCap` does not use
  the skip wave.

Do **not** write `DeepestWaveReached` here. Cleared-wave tracking stays on
`NotifyAssaultCleared`.

Persist Meta on Run end so a crash mid-Hatch-bank is the only loss of the
checkpoint; prefer writing NavUnlock at Hatch-bank start and `GameSession.Save`
on host (guests save their own deepest on the converted RPC path).

### 4. Any-seat down ends this window

While this first Hatch-bank Assault is live, a guest down calls the host
`EndRun("down")` instead of `NetShipWait.Enter`. Log
`[Demo] first-hatch down ends run seat=<n>`. After `DemoFirstHatchSpecUsed`
and this Run has ended, restore `M49`/`M57` for all later Runs.

Host/solo down already ends the Run.

Hatch HP 0 still `EndRun("hatch")`. Converted bank stays.

## Create

| Path | Role |
|---|---|
| `DemoProgress` helpers | `IsFirstHatchBank(meta)`, `ApplyHatchStartCheckpoint(meta, currentWaveIndex)` |
| Tests | `DemoHatchBankTests`: dump-before-fight; loss keeps banked; hunter count 4; NavUnlock 2 on start |

## Change

| File | Change |
|---|---|
| `RunSim.TriggerDraw(HatchBank)` | Convert-then-fight branch; checkpoint fields. Demo flags live on Meta already passed into `RunSim` — add the v9 fields to the Meta the sim holds. Sim **may** read `MetaState` demo fields (they are Sim types). Do not reference Game/DemoVoice |
| `RunSim.LaunchWave` | First-hatch Hunter add |
| `RunSim.Tick` Hatch-bank | Skip drip when Unbanked already dumped for this flag |
| `GameController` / `PlayerHealth` / net down | First-hatch any-seat down → `EndRun` |
| `RunSim.Wallets.CreditSlot` | Unchanged formula. After dump, `BankCap` uses `DemoBankCapWave` when `DemoActive` (slot 0: that field; guests: wallet deepest set on start) |

`HatchBank.BankCap(int deepestWave)`: while demo, pass `DemoBankCapWave` (1 after
this start), not `DemoNavUnlockWave` (2). Solo `PlayerCount==1` still skips
BankCap (byte-identical path).

## Delete

Nothing. Do not remove drip for later Hatch-banks.

## Numbers

| Name | Value | Source |
|---|---|---|
| Convert mul carry 1 | ×1.00 | `HatchBank.MultLow` |
| Dump if 18 kills | 450 | 18×25 |
| Two Secondary buys | 150+300=450 | `WeaponTier.Cost(Secondary, 0..1)` comment |
| Window | 45 s | `HatchBank.DurationSeconds` |
| Hunters added | 4 | `HunterPerWave[1]`; retune +2 if survived |
| Hunter TTK on 100 HP if 4 connect | 100 / (4×6/0.7) ≈ **2.9 s** | arithmetic; disproved if they kite all 4 |
| Hatch TTK if 18 Defaults connect | 300 / (18×4/0.7) ≈ **2.9 s** | arithmetic |
| Slots after this window | 2 left if it started | spent even on loss |

## Multiplayer

| Event | Authority |
|---|---|
| Convert dump | Host sim, split wallets, each `CreditSlot` |
| BankCap | `DemoBankCapWave=1` → 18×25=450 per guest cap (solo expected table at dial 0). Two players: 36 kills ×25 = 900 Unbanked split ≈ 450 each before cap |
| Hunter add | Spec counts are after `NextWave(..., playerCount)`, then set hunters to **4×playerCount** so two Boarders are not sharing four Hunters. Default row already ×n. **Spec: `hunterCount = 4 * playerCount`.** Log it. Solo 4, duo 8 |
| Down | Any seat → EndRun this window only |
| Voice `demo.hatch_convert` | Party Lock **Prep**, on Hatch `E` select (armed) before `T`, once |
| Voice `demo.hatch_fight` | Party Subtitle on Assault start |
| Voice `demo.first_hatch_down` | Party Lock on Ship after this Lost |
| Join | Already in Bastion; no join mid-Run (`M72` join is Ship). Mid-countdown N/A |

If the host never dies because guests soak Hunters: any-seat-down still ends it
when the first body drops. If nobody dies and they survive: kill observation
(raise hunters). ARMORY wheel stays locked until `PodEjections >= 1`
(`MetaUpgrades.UpgradesUnlocked`). Survive branch: skip “you died” line; play a
one-line Local `demo.first_hatch_down` variant only if we add
`demo.first_hatch_survived` — **add that id** if the kill observation fires;
do not write it until then.

## Voice

`demo.hatch_convert` Prep Lock Party (convert is about to happen on `T`).
`demo.hatch_fight` Assault Subtitle Party.
`demo.first_hatch_down` Docked Lock Party after Lost from this window.

Do not explain drip. Do not mention intensity.

## Driven / Shown / Reads-back

- **Driven by** Hatch `E` then `T`.
- **Shown** Unbanked drops to 0 at Assault start (HUD Unbanked / banked). Hunters
  on the Boarder; Defaults on the Hatch.
- **Reads-back** convert dump log; first-hatch spec log; `NAV unlock=2` log;
  `Run ended (down\|hatch)` with banked unchanged from dump.

## Logs

| Event | Line |
|---|---|
| Convert | `[Demo] convert-then-fight dumped=450 mul=1` |
| Spec | `[Demo] first-hatch spec H=4 D=18 R=0 playerCount=1` |
| Checkpoint | `[Demo] hatch-bank started fieldWave=1 navUnlock=2 bankCapWave=1` |
| Down | `[Demo] first-hatch down ends run seat=0` |
| Survive (kill) | `[Demo] first-hatch survived` |

## Traps

- Drip after dump is harmless at Unbanked 0; still skip so logs stay clean.
- `EndRun` wiping **banked** — it must not. Only Unbanked.
- Raising `DeepestWaveReached` to 2 on start → `BankCap` includes Wave 2 (30×25)
  and guests can bank more than they earned. Use `DemoBankCapWave`.
- Spending a Hatch-bank slot then dying: slot is gone. Correct.
- Guest wait on Ship would skip the designed party loss. Override this window only.
- Prefab Hunter counts on spawners — composition comes from `WaveSpec`, not
  serialized spawners.
- Scripted `Die()` at t=45. Forbidden.
- Convert-then-fight on Master Control Hatch half. Flag is first mid-Run
  Hatch-bank only; Master is beats 6–7.

## Depends / Unblocks

Depends: beat 1 Unbanked > 0. Unblocks: beat 3 ARMORY (needs `PodEjections >= 1`
and banked 450). Unblocks beat 4 NAV Wave 2 (`DemoNavUnlockWave=2` even on loss).

## Verification

1. EditMode: TriggerDraw HatchBank with Unbanked 450, demo first → Banked +=450,
   Unbanked 0, then `EndRun("down")` → Banked still +450, Unbanked 0.
2. EditMode: same without demo flag → Unbanked not dumped at start.
3. EditMode: LaunchWave first-hatch spec H=4×n.
4. EditMode: hatch start sets navUnlock 2, bankCapWave 1, `DeepestWaveReached`
   unchanged if it was 1.
5. PlayMode: 0-upgrade MK-1, this window; log either `Run ended` or
   `first-hatch survived` (the latter fails the designed-loss claim and needs
   the hunter lever).
6. Loopback: guest down during this window ends host Run; both return to Ship
   with converted bank.
7. No `[Dev]`.

## Rejected

Siphon during the window. Keeping drip so “some” salvage survives a fast death.
Scripted kill. Making Hatch invulnerable. Skipping the fight and auto-Losing.
Convert-then-fight on every Hatch-bank this demo.
