# M55 — Pay for intensity

<!--
Status: next
Update trigger: when juice cost, the intensity readout, the terminal confirm, or siphon-dial removal changes
Last updated: 2026-08-13
-->

The siphon dial as a free Prep stepper goes away. **Intensity is a visible number on the
trigger you are about to pull. The player pays carried Salvage (unbanked, including
withdrawn) to raise it there, then confirms before the Wave starts.** More danger, more
Salvage into the pile. No pay-to-skip on doors. No brick. The body/reward table stays
`TetherSiphon.RewardMultiplierFor` — this slice changes how you set the level, not the
curve.

**Recut 2026-08-13 (keys):** `+` / `-` juice at the look-at. `E` selects the value
(including Intensity 0) and locks juice. `T` starts the Wave. `E` again unlocks juice.
Looking away unselects. ARMORY keeps `[` `]` for the wheel.

**Recut 2026-08-13 (review, before M54 play):** juice and the stake readout live on the
terminal (Field, Master Control) and Hatch-bank look-at, not as a room-wide dial you
set and walk away from. `T` is refused until that trigger is E-selected this Prep.
M54 already shows Siphon on the prompt and gates the free dial to the look-at; this
slice makes juice cost Unbanked and renames the HUD. `BastionTerminalKind.Master` is
the Master flag. Juice on Master covers the whole sequence (one select at the pull).

## Story

As a Boarder, I want to spend Salvage I am already carrying to make the next Wave harder
and richer, so that the stake is a payment I confirm at the trigger, not a free dial I
leave at 0.

## Why it matters

Eleven logged Runs at siphon L0, two at L2 — the *free* stake was not taken. Replacing it
with a paid bump on the same table is the PoE 2 shape settled 2026-08-13. Paying from the
ship bank sitting on the Ship was rejected; juice spends unbanked.

Leverage tier: **#8, information flows** (the number is visible at the pull, the
composition is not) and **#9, rules** (you pay, and you cannot start without confirming).
Cost per step is **#12**, initial `100`.

## Kill criterion

Ten logged Runs after close. **If juice is unused on all of them (intensity stays 0 every
pull), stop** — the paid stake failed the same way the free dial did. Readable off the
start-Wave log line.

## Acceptance criteria

1. **`+` / `-` juice only while looking at a startable trigger** (Field terminal, Master
   Control, or Hatch for Hatch-bank) and only while that trigger is not E-selected. Each
   step spends `IntensityJuice.CostPerStep = 100` from Unbanked (refuses if Unbanked < 100,
   logged). Cannot spend `BankedSalvage`. Cannot juice during Assault. Cannot raise
   intensity by paying to skip a door. Looking elsewhere, or juicing after `E` select:
   logged refusal, no spend. ARMORY `[` `]` stay on the wheel.
2. **HUD shows `Intensity N` (0–5), never `Siphon L`.** No composition-by-kind preview.
   Fight strip, Prep panel, and the trigger prompt all. Prompt names Intensity, juice cost,
   Unbanked, and next-Wave body count.
3. **A Wave does not start until the stake is E-selected this Prep at that trigger.**
   `E` selects (locks juice). `T` starts. `E` again unlocks juice. Intensity 0 is a valid
   select. `T` without select: logged refusal, no Wave. Juice attaches to that pull. After
   that Wave/sequence starts, further juice applies to the *next* trigger. Master Control:
   one juice covers the whole sequence. Hatch hold-`E` repair is unchanged when the Hatch
   is damaged.
4. **`TetherSiphon.RewardMultiplierFor` and `WaveDirector.NextWave` are unchanged.**
   Code type `TetherSiphon` may keep its name; player-facing “siphon dial” is retired.
5. **language.md and `Docs/Play/The-Siphon-Dial.md`:** dial row replaced by **intensity**
   and **juice**. `The-Siphon-Dial.md` retitled or redirected in the same change so the
   lint language check does not see a retired term in `Docs/Play/`.
6. EditMode `IntensityJuiceTests`: Unbanked 250 juices twice (level 0→2, Unbanked 50),
   third step refuses; BankedSalvage untouched; Assault juice refuses; juice away from a
   trigger refuses. Verify + PlayMode green. Start-Wave log: `intensity=<n> juiced=<spent>`.

## Implementation

**Create** `Assets/Scripts/Sim/Run/IntensityJuice.cs` — `CostPerStep = 100`,
`MaxLevel = TetherSiphon.MaxLevel`.

**Change** `RunSim.SetSiphonLevel` — Prep-only still, but the Game layer must have
debited Unbanked first; Sim also refuses a raise when Unbanked < cost if called directly
(tests use a `TryJuice(int delta)` that does both). Lowering intensity is free and does
not refund (no brick, and no sell-back).

**Change** `GameController` — `+` / `-` → `TryJuice` only while `Interactor.Current` is a
startable Field/Master terminal or the Hatch, and only while not E-selected. `E` toggles
select; `T` starts. Log the start line with intensity.

**Change** `HudController.Fight` / Prep panel / `BastionTerminal.Prompt` — `Intensity N`.
Shortcut panel: `+` `-` are juice, not siphon dial. `T` is start.

**Change** `language.md`, `Docs/Play/The-Siphon-Dial.md` (rename to intensity),
`Docs/Play/Waves.md`, `Docs/Play/The-Game.md`, `Docs/README.md` player sentence.
`Decisions.md` deferred *Deliver-loot-before-the-run-ends* marked shipped by this slice
in the same change.

**Change** `4-Data.md` — cost constant via docgen; dial table stays the pressure/reward
curve.

Rejected: a second body table for juice. Rejected: paying from ship bank. Rejected:
showing exact composition. Rejected: pay-to-skip doors. Rejected: renaming `TetherSiphon`
in this slice (identifier chore, no player change). Rejected: juice from across the room.

## Supporting work

- **Driven by** `+` / `-` at the trigger, `E` select, `T` start — here.
- **Shown by** `Intensity N` on the Score strip, Prep panel, and trigger prompt — here.
- **Reads back as** Unbanked dropping by 100, the next Wave's bodies/kill value matching
  the table at that level — here.

## Verification

1. `IntensityJuiceTests` — criterion 1 and 6.
2. `Tools/lint-language/run.sh` — no “siphon dial” in `Docs/Play/`.
3. Playthrough — at a Field terminal juice 1 step, `E` select, `T` start, Unbanked −100,
   Wave reads Intensity 1 and pays the L1 kill value into unbanked. Select at 0 without
   juice: Wave starts at Intensity 0. Try juice at 0 unbanked: refusal. Try `T` without
   select: refusal. Door still opens only on Wave clear.

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
- `E` selects and `T` starts. Do not put start back on Interact. Hatch hold-`E` stays repair
  when the door is damaged.

## Depends on / Unblocks

Depends on M53 (Unbanked is the wallet) and M54 (Master pull exists; Field confirm
surface exists). Does not unblock destinations.
