# M53 — Salvage leaves through the Hatch

<!--
Status: later
Update trigger: when unbanked, drip, attrition, dump-on-clear, or bank-slot rules change
Last updated: 2026-08-13
-->

Kills fill an **unbanked** pile. **Only a Hatch-bank converts it to the ship.** Interact
`T` at the Hatch in Prep starts that Wave (`WaveMode.HatchBank`): drip, hatch hits eat
unbanked and Hatch HP, clear dumps the remainder, breach wipes what is left. Hatch-banks
per Run are capped; a fatter pile converts at a higher multiplier.

Instant `AwardKill` into `BankedSalvage` ends. The five-Wave clear still ends the Run
(M54). The siphon dial still exists (M55).

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

Two exploring Runs after close, same loadout: one spends all Hatch-bank slots on small
piles, one carries into fewer, fatter banks. **The few-banks Run must credit more to
`BankedSalvage` on the end-of-Run report.** If they tie, the cap and multiplier did
nothing. If the fat-bank Run always loses the pile, greed is fake.

## Acceptance criteria

1. **`AwardKill` adds to `RunSim.Unbanked`, never to `MetaState.BankedSalvage`.** Down
   wipes unbanked; already-banked ship Salvage is kept (`../../Decisions.md` expiry). Log
   kill lines quote the unbanked delta, not a ship credit.
2. **HUD Score shows Unbanked and Banked separately.** Prompt/readback: a kill raises
   Unbanked only.
3. **`T` in Prep while looking at the Hatch, with Unbanked > 0 and a slot remaining,
   starts `WaveMode.HatchBank`.** Log: `[Hatch] hatch-bank started wave <n> unbanked=<n>
   slots=<left>`. `T` elsewhere, Unbanked 0, or no slots: logged refusal. Interact `E` at
   the Hatch is still repair (Prep and Assault).
4. **Hatch is vulnerable only during `WaveMode.HatchBank`.** `DamageHatch` applies; drip
   and attrition run; Defaults and Rams are Hatch-primary; Hunters still hunt the Boarder;
   Rams refuse the Boarder again (the terminal-only break is off).
5. **Drip, attrition, dump, breach** — numbers in `Sim/Run/HatchBank.cs`, generated into
   `../../Design/4-Data.md` in the same change:
   - drip `DripPerSecond = 20` unbanked → ship bank while the Wave is live
   - each Hatch HP of damage also removes that many unbanked (floor)
   - convert multiplier on the amount that actually banks:
     `< 400` → ×1.00; `400–799` → ×1.25; `≥ 800` → ×1.50, applied at drip and at dump
   - Wave clear: remaining unbanked dumps at that multiplier, then Unbanked = 0
   - Hatch to 0: remaining unbanked wiped, dripped stays, Run lost
6. **`HatchBankSlotsPerRun = 3`.** Each Hatch-bank start spends one. Slots do not refill
   mid-Run. A fourth start is the criterion-3 refusal.
7. **Withdraw:** Hatch full, Prep, Unbanked = 0, hold Interact: moves Salvage from
   `BankedSalvage` into Unbanked at the repair rate's salvage/second, 1:1. Unspent
   withdrawn rides with unbanked (lost on down / breach). Repair still spends ship bank
   only.
8. **A Hatch-bank consumes one of M14's five Waves** (same `CurrentWaveIndex` as a
   terminal Wave). Wave 5 clear still `Escaped`; if that Wave was a Hatch-bank, dump
   already fired; if it was terminal, leftover unbanked is **not** converted (M54 is the
   leftover win).
9. EditMode `HatchBankTests` prove criteria 1, 5, 6 (nibble 3×200 at ×1.00 = 600 ship;
   one 600 at ×1.25 = 750 ship). Existing AwardKill / salvage-drift tests rewritten to
   the unbanked path. PlayMode green.

## Implementation

**Create** `Assets/Scripts/Sim/Run/HatchBank.cs` — the constants and `ConvertMultiplier(long)`,
`Drip(long unbanked, float dt)`, `Attrit(long unbanked, float hatchDamage)`. No
MonoBehaviour.

**Change** `RunSim` — `Unbanked`, `HatchBankSlotsLeft` (start at 3). `AwardKill` credits
Unbanked. `TriggerDraw(WaveMode mode)` public; HatchBank refuses if slots 0 or Unbanked
0 (Game layer should have refused first; Sim logs too). Tick drip only while
`Mode == HatchBank && Assault`. `DamageHatch` path: after `Hatch.Damage`, attrit
Unbanked. `NotifyAssaultCleared` in HatchBank: dump remainder. `NotifyPlayerDown` /
hatch destroyed: Unbanked = 0. Repair still `BankedSalvage` only.

**Change** `GameController` — Prep `PullDraw`: if `Interactor.Current` is the Hatch
repair target, `TriggerDraw(HatchBank)` and spend the slot; else the M52 terminal
refusal. `DamageHatch` applies only in HatchBank.

**Change** `HudController.Fight` — Score line `Unbanked N  ·  Banked M`.

**Change** `SalvageLedger` / kill log so the run tally can show gross converted this Run
(dripped + dumped), not kill-instant bank. End screen uses that.

**Change** `RamGatekeeper.MayTargetPlayer` — true only when `Mode == Terminal`.

**Change** `language.md` — **unbanked**, **Hatch-bank**, **banked** stays ship-side.
Salvage row: not “banked the instant a Gatekeeper dies.”

**Change** `Docs/Play/Salvage.md`. `Tools/docgen/run.sh` after `HatchBank.cs` exists
(new GEN block or a hand table beside hatch — prefer a GEN block).

Rejected: delaying `AwardKill` in the one-room fight without this convert (already
forbidden). Rejected: musical-chairs rotor. Rejected: dump on terminal-clear of Wave 5
(that is a free extract). Rejected: repair spending unbanked (Watch).

## Supporting work

- **Driven by** `T` at the Hatch — here. Terminals still Interact.
- **Shown by** Unbanked on the Score card and the Hatch prompt — here.
- **Reads back as** Unbanked rising on kills, Banked rising only during Hatch-bank —
  here.

## Verification

1. `HatchBankTests` — criterion 5 arithmetic and criterion 6 slot refusal; the 200×3 vs
   600×1.25 case is the kill-criterion fixture.
2. `Tools/verify/run.sh` plus integration check.
3. Playthrough — terminal Wave (unbanked rises, hatch HP flat, door opens); `T` at Hatch;
   defend; Banked rises; a hatch hit drops both HP and Unbanked. A second Run nibbles vs
   fattens; read the two end reports.

## Do not build

| Not building | Why not |
|---|---|
| Master Control leftover convert | M54 |
| Juice / intensity | M55 |
| In-Run shop or respawn | Settled cut |
| Auto-repair / hatch turret | Cut in `fabrication.md` |

## Watch

- **First Hatch-bank of a broke Run cannot repair** — repair spends ship bank, which is
  empty until something has dripped. Hatch Armor is the buffer. Do not silently spend
  unbanked.
- Padding bodies to farm drip time is useless because of dump-on-clear; if play still
  stalls, the drip rate is not the lever, the dump is.
- Prefab `repairPerSecond` stays pushed at spawn (M23).

## Depends on / Unblocks

Depends on M52 (`WaveMode`, terminal start, Hatch invulnerable by default). Unblocks M54
and M55 (juice pays from unbanked).
