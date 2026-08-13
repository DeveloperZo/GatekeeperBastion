# M55 — Pay for intensity

<!--
Status: later
Update trigger: when juice cost, the intensity readout, or siphon-dial removal changes
Last updated: 2026-08-13
-->

The siphon dial as a free Prep stepper goes away. **Intensity is a visible number. The
player may pay carried Salvage (unbanked, including withdrawn) to raise it on the trigger
about to be pulled.** More danger, more Salvage into the pile. No pay-to-skip on doors.
No brick. The body/reward table stays `TetherSiphon.RewardMultiplierFor` — this slice
changes how you set the level, not the curve.

## Story

As a Boarder, I want to spend Salvage I am already carrying to make the next Wave harder
and richer, so that the stake is a payment I can fail, not a free dial I leave at 0.

## Why it matters

Eleven logged Runs at siphon L0, two at L2 — the *free* stake was not taken. Replacing it
with a paid bump on the same table is the PoE 2 shape settled 2026-08-13. Paying from the
ship bank sitting on the Ship was rejected; juice spends unbanked.

Leverage tier: **#8, information flows** (the number is visible, the composition is not)
and **#9, rules** (you pay). Cost per step is **#12**, initial `100`.

## Kill criterion

Ten logged Runs after close. **If juice is unused on all of them (intensity stays 0 every
pull), stop** — the paid stake failed the same way the free dial did. Readable off the
start-Wave log line.

## Acceptance criteria

1. **Prep `[` / `]` no longer set intensity for free.** They juice: each step spends
   `IntensityJuice.CostPerStep = 100` from Unbanked (refuses if Unbanked < 100, logged).
   Cannot spend `BankedSalvage`. Cannot juice during Assault. Cannot raise intensity by
   paying to skip a door.
2. **HUD shows `Intensity N` (0–5), never `Siphon L`.** No composition preview. Fight
   strip and Prep panel both.
3. **Juice attaches to the next trigger** — the next terminal Interact, Hatch-bank `T`,
   or Master Control pull. That Wave (or Master sequence) uses `TetherSiphon.Level` as
   today for bodies and kill value. After that Wave/sequence starts, further `[` `]` in
   Prep apply to the *next* trigger. Master Control: one juice covers the whole sequence.
4. **`TetherSiphon.RewardMultiplierFor` and `WaveDirector.NextWave` are unchanged.**
   Code type `TetherSiphon` may keep its name; player-facing “siphon dial” is retired.
5. **language.md and `Docs/Play/The-Siphon-Dial.md`:** dial row replaced by **intensity**
   and **juice**. `The-Siphon-Dial.md` retitled or redirected in the same change so the
   lint language check does not see a retired term in `Docs/Play/`.
6. EditMode `IntensityJuiceTests`: Unbanked 250 juices twice (level 0→2, Unbanked 50),
   third step refuses; BankedSalvage untouched; Assault juice refuses. Verify + PlayMode
   green. Start-Wave log: `intensity=<n> juiced=<spent>`.

## Implementation

**Create** `Assets/Scripts/Sim/Run/IntensityJuice.cs` — `CostPerStep = 100`,
`MaxLevel = TetherSiphon.MaxLevel`.

**Change** `RunSim.SetSiphonLevel` — Prep-only still, but the Game layer must have
debited Unbanked first; Sim also refuses a raise when Unbanked < cost if called directly
(tests use a `TryJuice(int delta)` that does both). Lowering intensity is free and does
not refund (no brick, and no sell-back).

**Change** `GameController` Prep dial keys → `TryJuice`. Log the start line with
intensity.

**Change** `HudController.Fight` / Prep panel — `Intensity N`. Shortcut panel: `[` `]`
are juice, not siphon dial.

**Change** `language.md`, `Docs/Play/The-Siphon-Dial.md` (rename to intensity),
`Docs/Play/Waves.md`, `Docs/Play/The-Game.md`, `Docs/README.md` player sentence.
`Decisions.md` deferred *Deliver-loot-before-the-run-ends* marked shipped by this slice
in the same change.

**Change** `4-Data.md` — cost constant via docgen; dial table stays the pressure/reward
curve.

Rejected: a second body table for juice. Rejected: paying from ship bank. Rejected:
showing exact composition. Rejected: pay-to-skip doors. Rejected: renaming `TetherSiphon`
in this slice (identifier chore, no player change).

## Supporting work

- **Driven by** `[` `]` in Prep — here.
- **Shown by** `Intensity N` on the Score / Prep strip — here.
- **Reads back as** Unbanked dropping by 100, the next Wave's bodies/kill value matching
  the table at that level — here.

## Verification

1. `IntensityJuiceTests` — criterion 1 and 6.
2. `Tools/lint-language/run.sh` — no “siphon dial” in `Docs/Play/`.
3. Playthrough — Prep juice 1 step, pull a terminal, Unbanked −100, Wave reads Intensity 1
   and pays the L1 kill value into unbanked. Try juice at 0 unbanked: refusal. Door still
   opens only on Wave clear.

## Do not build

| Not building | Why not |
|---|---|
| New mix tables | Same mix, targeting already M52/M53 |
| Brick / exact preview | Settled no |
| Path telegraph | Parked |

## Watch

- If base intensity (0) is already the whole fight, juice will sit unused — that is the
  kill criterion, not a retune-during-the-slice.
- `TetherSiphon` remaining in code will tempt someone to put “Siphon” back on the HUD.
  The HUD string is the criterion.

## Depends on / Unblocks

Depends on M53 (Unbanked is the wallet) and M54 if juice on Master Control is to be
tested in play — the Sim hook can land after M53; the Master attach is a one-line on
`StartMasterControl`. Does not unblock destinations.
