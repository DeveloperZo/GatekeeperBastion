# M53 — Salvage leaves through the Hatch

<!--
Status: completed 2026-08-13
Update trigger: closed
Last updated: 2026-08-13
Closed on: Zo 2026-08-13. Gate is playthrough
`Logs/GatekeeperBastion_2026-08-13_18-23-55_p92464.log` (Run clean; `[Dev]` StartCoopHost
is after `BASTION STRIPPED`): T1 Waves 1–5 hatch 300/300 salvage 0, T1 complete 18:31:10;
Hatch-bank 18:31:53 wave 5 unbanked=5250 slots=2, 78 gatekeepers, kills `+0`; timer
45.002 s then salvage 7413 hatch 292/300. Wave 5 with Unbanked did not Escaped. Close
log still `BASTION STRIPPED` after dump — stay-Prep is EditMode
`HatchBankAfterWave5_ConvertsAndStaysPrep` / `ThenTerminalDrawStarts`. Harness
`Tools/verify/run.sh` 316/0 this close. PlayMode not re-run (same as M52).
-->

Kills fill an **unbanked** pile. **Only a Hatch-bank converts it to the ship.** Interact
`T` at the Hatch in Prep starts that Wave (`WaveMode.HatchBank`): drip while a **45 s
timer** runs, hatch hits eat unbanked and Hatch HP, the timer dumps the remainder, breach
wipes what is left. Enemy wipe and Unbanked hitting 0 do not end it. Hatch-banks per Run
are capped; a fatter pile converts at a higher multiplier.

Instant `AwardKill` into `MetaState.BankedSalvage` ends. Terminal Wave 5 does not end the
Run when Unbanked remains — the door opens, Prep holds, Hatch-bank converts. The siphon
dial still exists (M55). Master Control's two-phase sequence is M54.

**Recut 2026-08-13 (review):** convert multiplier is snapshotted from Unbanked at Hatch-bank
start (live-on-remaining lets drip eat the fat bonus; the 600→750 fixture would only hold
on instant dump). Hatch-bank `TriggerDraw` never deep-start chains. Hatch-bank enables T1's
Wave Spawns plus completed Field terminals' UnlockedSpawns (otherwise Prep after Wave 1 has
an empty registry). Withdraw is a separate `RunSim.Withdraw`, not `RepairHatch`. Kill
criterion dated.

**Recut 2026-08-13 (playthrough):** Wave 5 terminal clear no longer `Escaped` while Unbanked
> 0 (that wiped the pile on the default T1 ride). Hatch-bank does not consume a field Wave.

**Recut 2026-08-13 (hatch-bank window):** composition is one step past the last field Wave.
Convert is the 45 s timer (drip, then dump). Unbanked 0 and enemy wipe do not end it.
Kills during Hatch-bank credit 0.

**Recut 2026-08-13 (after hatch-bank):** timer dump after the field is stripped stays
Prep. The T1 door is already open. Terminal draws past `MaxWaves` are allowed
(composition clamps at `MaxWaves - 1`). `Escaped` is Wave 5 terminal with Unbanked 0
only. Master Control remains M54.

**Recut 2026-08-13 (carry):** convert and Hatch composition both read field Waves
cleared since the last Hatch-bank. 1 Wave → ×1.00 and Wave 1 (18 bodies, 0 Rams).
5 Waves → ×1.50 and Wave 5. Pile-size convert is gone. Same greed, counted in Waves
so a new save can convert after Wave 1.

## Story

As a Boarder, I want Salvage I earn in the bastion to stay at risk until I defend a
Hatch-bank, so that greed is carrying a bigger pile into fewer extracts.

## Why it matters

`RunSim.AwardKill` credits `MetaState.BankedSalvage` before the body lands
(`GameController.GatekeeperKilled`). That made “currency is simply kept”
(`../../Decisions.md`) true for the one-room fight, and it makes leaving the Hatch
pointless as an economy. The field loop's convert is the Hatch-bank
(`../../Brainstorms/missions-and-destinations.md`).

Leverage tier: **#8, information / #9, rules** for unbanked vs banked and the slot cap.
The slot count and multiplier table are **#12 constants**, initial values below, tunable
after close — not a reason to hold the milestone.

## Kill criterion

Two exploring Runs after close, same loadout, by **2026-09-13**: one spends all Hatch-bank
slots on small piles, one carries into fewer, fatter banks. **The few-banks Run must
credit more to `BankedSalvage` on the end-of-Run report.** If they tie, the cap and
multiplier did nothing. If the fat-bank Run always loses the pile, greed is fake.

## Acceptance criteria

1. **`AwardKill` adds to `RunSim.Unbanked`, never to `MetaState.BankedSalvage`.** During
   Hatch-bank it credits 0 (Score only moves convert). Down wipes unbanked; already-banked
   ship Salvage is kept (`../../Decisions.md` expiry). Log kill lines quote `(+N unbanked)`,
   not a ship credit.
2. **HUD Score shows Unbanked and Banked separately.** Prompt/readback: a kill raises
   Unbanked only. During Hatch-bank, Unbanked falls as Banked rises. Client Score reads
   mirrored Unbanked (M50's `Banked` already exists).
3. **`T` in Prep while looking at the Hatch (`Interactor.Current` is `RepairInteractable`),
   with Unbanked > 0 and a slot remaining, starts `WaveMode.HatchBank`.** Log:
   `[Hatch] hatch-bank started wave <n> unbanked=<n> slots=<left>`. `T` elsewhere with
   Unbanked > 0: `[Hatch] hatch-bank refused — look at the Hatch`. Unbanked 0:
   `[Hatch] hatch-bank refused — unbanked=0`. No slots:
   `[Hatch] hatch-bank refused — no slots`. `T` with nothing to bank:
   `[Run] draw refused — Waves start at a terminal`. Interact `E` at the Hatch is still repair
   (Prep and Assault). Host `T` only this slice (same as M52's T). Prep card names
   Hatch-bank when Unbanked > 0 (untimed Prep after any field clear).
4. **Hatch is vulnerable only during `WaveMode.HatchBank`.** `DamageHatch` applies; drip
   and attrition run; Defaults and Rams are Hatch-primary; Hunters still hunt the Boarder;
   Rams refuse the Boarder again (the terminal-only break is off).
5. **Drip, attrition, dump, breach** — numbers in `Sim/Run/HatchBank.cs`, generated into
   `../../Design/4-Data.md` in the same change:
   - drip `DripPerSecond = 20` unbanked → ship bank while the timer is live
   - **duration `DurationSeconds = 45`.** Enemy wipe and Unbanked hitting 0 do not end it.
     Leftover dumps when the timer ends. A player who lost the pile to hatch hits still
     defends until the timer.
   - each Hatch HP of damage also removes that many unbanked (floor); destroyed, not converted
   - **convert multiplier snapshotted from Waves carried at Hatch-bank start**, applied
     to every drip and the dump: 1 → ×1.00; 2 → ×1.10; 3 → ×1.25; 4 → ×1.35; 5+ → ×1.50.
     Hatch composition is `NextWave(carry − 1)` — the Wave equal to how many you carried.
   - Hatch to 0 or Boarder down: remaining unbanked wiped, already-converted stays, Run lost
6. **`HatchBank.SlotsPerRun = 3`.** Each Hatch-bank start spends one. Slots do not refill
   mid-Run. A fourth start is the criterion-3 refusal.
7. **Withdraw:** Hatch full, Prep, Unbanked = 0, hold Interact: `RunSim.Withdraw` moves
   Salvage from `BankedSalvage` into Unbanked at
   `RepairSalvagePerHp × Repair Rig HP/s` (16/s), 1:1. Does **not** require the Repair Rig.
   Uses `SpendSalvage` (lifetime spent moves; convert `EarnSalvage`s it back). Unspent
   withdrawn rides with unbanked (lost on down / breach). Repair still spends ship bank
   only, still requires the Rig. `Sim.RepairHatch` on a full Hatch still returns 0.
8. **A Hatch-bank does not consume a field Wave** (`CurrentWaveIndex` does not advance).
   Composition is `NextWave(HatchBank.CarryWaveIndex(carry))` — the Wave equal to Waves
   carried, not one step past the run index.
   including after Wave 5 (`NextWave(5)` vs Wave 5's `NextWave(4)`). Terminal `TriggerDraw`
   may continue past `MaxWaves`; composition clamps at `MaxWaves - 1`. Hatch-bank is
   allowed after the field is stripped.
   **Terminal Wave 5 with Unbanked > 0 does not `Escaped`**. Unbanked 0 on that clear still
   `Escaped`. Hatch-bank timer dump after `CurrentWaveIndex >= MaxWaves` stays Prep (door
   already open). Later terminal clears do not `Escaped`.
   Kills during Hatch-bank credit 0 Unbanked (Score only moves convert: Unbanked down,
   Banked up). Master Control's 5+5 sequence is M54.
9. EditMode `HatchBankTests` prove criteria 1, 5, 6, 7 (nibble 3×200 at ×1.00 = 600 ship;
   one 600 at ×1.25 = 750 ship, including drip-then-dump at the snapshot). Existing
   AwardKill / salvage-drift tests rewritten to the unbanked path. PlayMode green.

## Implementation

**Create** `Assets/Scripts/Sim/Run/HatchBank.cs` — constants, `ConvertMultiplier(long)`,
`DripUnits(dt, ref accrued)`, `AttritUnits(hatchDamage)`, `BankedFrom(taken, mul)`,
`WithdrawPerSecond`, `DurationSeconds`. No MonoBehaviour. No live pile.

**Change** `RunSim` — `Unbanked`, `HatchBankSlotsLeft` (start at 3). `AwardKill` credits
Unbanked; does not call `EarnSalvage`. `TriggerDraw()` stays Terminal (tests, Dev).
`TriggerDraw(WaveMode mode)` public; HatchBank refuses if slots 0 or Unbanked 0, never
chains, snapshots `ConvertMultiplier(Unbanked)`, decrements slots, does not increment
`CurrentWaveIndex`. Composition is `NextWave(CurrentWaveIndex)` (stronger than the last
field Wave). Terminal may continue past `MaxWaves` (composition clamps at `MaxWaves-1`).
Tick drip while HatchBank + Assault + Unbanked > 0; timer always runs. `NotifyAssaultCleared`
during HatchBank with timer > 0 is ignored. Timer expiry dumps remainder and stays Prep
after the field is stripped. `CurrentKillAward` is 0 during HatchBank. `DamageHatch`: after
`Hatch.Damage`, attrit Unbanked (skip if the hit already ended the run). Terminal Wave 5
with Unbanked > 0: stay Prep. Unbanked 0 on that Wave 5 clear: `CompleteRun`.
`NotifyPlayerDown` / hatch destroyed: Unbanked = 0 without convert.
`CompleteRun` still wipes leftover (should already be 0). Repair still `BankedSalvage`
only. `Withdraw(float dt)` is the criterion-7 path. `SalvageLedger.RunSalvage` is
converted this Run (drip + dump), not kill awards.

**Change** `GameController` — Prep `PullDraw`: if `Interactor.Current` is
`RepairInteractable`, `StartHatchBankWave`; else if Unbanked > 0 and slots left,
`[Hatch] hatch-bank refused — look at the Hatch`; else the M52 terminal refusal.
`StartHatchBankWave` in the Terminals partial: Sim refuse (not the MaxWaves refuse), then
`BastionLayout.ActivateHatchBankSpawns`, then `TriggerDraw(HatchBank)`. `OnAssaultEnded`
despawns leftover Hatch-bank bodies, then restores Field spawns. `RepairHatch` / `RepairHatchFromNet`: when Hatch
full + Prep + Unbanked 0, call `Withdraw` (no Rig check); otherwise existing repair.

**Change** `BastionLayout` — `ActivateHatchBankSpawns`: enable T1's UnlockedSpawns plus
any completed Field terminal's UnlockedSpawns. `RestoreFieldSpawns` is
`RefreshTerminalStates`.

**Change** `HudController.Fight` — Score line `Unbanked N  ·  Banked M`. End screen
gross is converted this Run. Assault heading during Hatch-bank: `HATCH-BANK — Ns`.
Prep card, when Unbanked > 0 and slots left:
`Hatch-bank — T at the Hatch`. Sequential gap heading: `next Wave in Ns`. After Wave 5
with leftover: `field stripped`, not BASTION STRIPPED.

**Change** `NetRunMirror` — `Unbanked` NetworkVariable. Client Score / Hatch prompt read it.

**Change** `RepairInteractable` — Hatch-bank `T` on the prompt when Prep + Unbanked > 0 +
slots left. Withdraw prompt when Hatch full + Prep + Unbanked 0 + bank > 0; hold E
withdraws even without the Rig. Repair path unchanged.

**Change** `KillLog` — `(+N unbanked)`.

**Change** `Controls` — Prep `T` label: `Hatch-bank`.

**Change** `RamGatekeeper.MayTargetPlayer` — already true only when `Mode == Terminal`.

**Change** `language.md` — **unbanked**, **Hatch-bank**; **banked** stays ship-side.
Salvage row: not “banked the instant a Gatekeeper dies.” Siphon Draw row: Prep `T` is
Hatch-bank at the Hatch.

**Change** `Docs/Play/Salvage.md`, `A-Run.md`, `The-Game.md`, `Gatekeepers.md`,
`Getting-Started.md`, `The-Hatch.md`, `Waves.md`, `Play/README.md`.
`Tools/docgen/run.sh` after `HatchBank.cs` exists (`hatch-bank` GEN block).

Rejected: delaying `AwardKill` in the one-room fight without this convert (already
forbidden). Rejected: musical-chairs rotor. Rejected: dump on terminal-clear of Wave 5
(that is a free extract — they Hatch-bank). Recut: wipe on terminal Wave 5 (the T1 ride
never offered convert). Rejected: repair spending unbanked (Watch). Rejected: live
multiplier on remaining Unbanked (drip eats the fat bonus). Rejected: HatchBank-kind
terminal as the start verb (look-at + `T`; console is the same loop later). Rejected:
`TriggerDraw(HatchBank)` deep-start chaining. Rejected: putting withdraw inside
`RepairHatch`.

## Supporting work

- **Driven by** `T` at the Hatch — here. Terminals still Interact. Withdraw by hold `E`
  at a full Hatch in Prep — here.
- **Shown by** Unbanked on the Score card, the Prep line `Hatch-bank — T at the Hatch`
  when Unbanked > 0, and the Hatch prompt — here.
- **Reads back as** Unbanked rising on kills, Banked rising only during Hatch-bank —
  here. Withdraw: Unbanked up, Banked down.

## Verification

1. `HatchBankTests` — criterion 5 arithmetic and criterion 6 slot refusal; the 200×3 vs
   drip and dump (including drip-then-timer-dump of 600, both 750). Timer: Unbanked 0
   and NotifyAssaultCleared do not end Hatch-bank. Composition after Wave N is NextWave(N).
   Withdraw 16/s. Down wipes Unbanked, keeps Banked.
2. `Tools/verify/run.sh` plus integration check.
3. Playthrough — terminal Wave (unbanked rises, hatch HP flat, door opens). After first
   clear, Prep names Hatch-bank during the gap without looking at the Hatch. After Wave 5:
   door open, Prep, Unbanked kept, Hatch-bank offered. `T` at Hatch; Unbanked falls as
   Banked rises; a hatch hit drops both HP and Unbanked; Unbanked 0 does not end it;
   timer dump then Prep, door still open, walk D1 / use the next field terminal. A second
   Run nibbles vs fattens; read the two end reports (Lost or the Wave-5 Unbanked-0 path).

## Do not build

| Not building | Why not |
|---|---|
| Master Control leftover convert | M54 |
| Juice / intensity | M55 |
| In-Run shop or respawn | Settled cut |
| Auto-repair / hatch turret | Cut in `fabrication.md` |
| `BastionTerminalKind.HatchBank` as a start verb | Look-at + `T` this slice |
| Client `T` Hatch-bank NetVerb | Host `T` only, same as M52 |

## Watch

- **First Hatch-bank of a broke Run cannot repair** — repair spends ship bank, which is
  empty until something has dripped. Hatch Armor is the buffer. Do not silently spend
  unbanked.
- Padding bodies cannot extend convert: dump is on the timer. If they wipe the roster
  early they wait out the rest of the 45 s with an empty Hatch. If that is the read, the
  duration is the lever, not drip.
- Prefab `repairPerSecond` stays pushed at spawn (M23). Withdraw uses the catalog rate,
  not the prefab, so a rigless Boarder can still pull money back into the pile.
- T1's Wave Spawns are the hatch-bay set. Re-emitting the sketch does not add a Hatch
  spawn token; Hatch-bank reuses T1's four.
- Default T1 sequence opens D1 on Wave 5 and holds Prep with Unbanked. Hatch-bank after
  that converts and leaves the door open. Mid-run Hatch-bank still cancels remaining T1 draws.
- Zero Hatch-bank slots and leftover Unbanked after Wave 5 has no convert and no Escaped
  until a slot exists (they do not). Do not silently dump.

## Depends on / Unblocks

Depends on M52 (`WaveMode`, terminal start, Hatch invulnerable by default). Unblocks M54
and M55 (juice pays from unbanked).
