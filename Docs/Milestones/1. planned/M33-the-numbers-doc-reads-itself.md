# M33 — The numbers doc reads itself out of the build

<!--
Status: later
Update trigger: when a generated block is added to or removed from Docs/Design/4-Data.md, or when the splice format changes
Last updated: 2026-08-09
-->

`Docs/Design/4-Data.md` stops being hand-maintained. Its countable half is spliced in by
`Tools/docgen/run.sh` from the live `Sim/` code; its derivations and arguments stay
hand-written in the same file, outside the generated blocks. `--check` fails on drift, and
runs from `Tools/verify/run.sh`.

## Story

As the developer, I want the numbers in `Design/4-Data.md` emitted by the same command that
emits the Field Manual, so that a tuning change cannot leave the doc I read before tuning
describing a game that no longer exists.

## Why it matters

**The failure, 2026-08-08.** Three milestones shipped that day and the doc was wrong within
hours.

| Wrong content | What the code says | Shipped by |
|---|---|---|
| An Engineering attribute track at CREW QUARTERS | `UpgradeTrack` holds `HatchArmor, Vitality, Stamina, Scavenging, Luck` (`Game/Ship/ShipCommands.cs:8`) | M23 |
| A FABRICATION Repair Rig track | Repair is equipment: `EquipmentCatalog.SlotPrice` 300, rig 300, flat 8 HP/s | M23 |
| A flat Tier curve, `T0..T7` dps per weapon | `WeaponWheel` — 5/3/3/0/0 by ring distance, Tier derived as `total / 11` | M21 |

Two of those three are still in the file today: the stale `T0..T7` table sits under *Weapon
abilities* three sections below a line reading "There are no weapon levels", and the deleted
tracks survive as prose that names them. `4-Data.md` is roughly 12 KB, and it is read out of
eight files (`MetaUpgrades`, `WeaponTier`, `WeaponWheel`, `WeaponCatalog`, `EquipmentCatalog`,
`TetherSiphon`, `WaveDirector`, `ShipSystems`) by hand, every time any of them changes.

**The generator is broken too, so nothing caught it.** `Docs/Play/Field-Manual.md` line 154
still sells Engineering at CREW QUARTERS and line 168 still sells a Repair Rig *track*, in a
file whose own header says it is generated. `Tools/docgen/Program.cs` calls
`WeaponTier.DamageMul`, `WeaponTier.ClipMul` and `WeaponTier.EffectiveDps`; M21 moved all
three to `WeaponWheel` and `WeaponTier.cs` now holds only `MaxTier`, `Cost`, `CanBuy` and
`SpentToReach`. The generator has not compiled since M21 landed, so `run.sh --check` has been
exiting **2** — could not run — where a reader sees exit non-zero and assumes drift was
reported. The committed manual is therefore three milestones old, and the check that exists
to say so has been saying it in a code nobody distinguished from a missing `mono`.

**Multiplier.** Every milestone that touches a parameter currently owes a manual sweep of
`4-Data.md` plus every table quoting the number (`AGENTS.md`, section 6). M16, M19, M20, M21
and M23 each paid it; M31 and M32 will. This removes that step from all of them and replaces
it with one command, on the contract `Docs/README.md` already states as a standing rule:
countable is generated, arguable is written.

Leverage tier (`../../Working-Rules.md`): **#6, information flows**, for the developer. Not
#5: no rule of the game changes, no number moves, and nobody gains or loses the ability to
tune. A higher tier is the wrong move because the defect is not that the wrong value was
chosen — it is that a correct change did not reach the document the next session reads before
choosing. Going lower is what has already been tried twice: hand-correcting the numbers is
exactly what produced the 2026-08-08 state, and each correction lowers the urgency to fix the
structure under it (fixes that fail, `Working-Rules.md`).

## What is generated, and what stays hand-written

**The test: a block is generated when every value in it is the return of one expression in
`Assets/Scripts/Sim/`.** If producing it needs an assumption the code does not hold — a play
measurement, a Run outcome, a judgement about what matters — it is prose and stays outside
the blocks.

**Generated** (ten blocks, ids as marked in Implementation):

| Block | Emitted from |
|---|---|
| `mk1-base` | `WeaponCatalog.Def(WeaponId.Mk1)` — damage, rate, clip, reload, range |
| `wheel` | `WeaponWheel` per-point constants, `FocusAward` / `NeighbourAward`, `PointsPerPurchase`, `MaxPurchases`, and a full-focus column pair read from `WeaponWheel.FocusAtMax` and `NeighbourAtMax` — **50 and 30 today** (`FocusAward` 5 and `NeighbourAward` 3, each x `MaxPurchases` = `WeaponTier.MaxTier` = 10). The emitter reads those two constants; a literal `35` / `21`, or any other literal pair, fails criterion 15 |
| `rings` | `WeaponWheel.RingOf`, `TryIdentity`, `AttrLabel`; MK-1 prints *unset* because `RingOf` returns null |
| `tier-cost` | `WeaponTier.Cost(0..MaxTier-1)` and `SpentToReach(MaxTier)` |
| `abilities` | `WeaponAbility.Magnitude(id, 0)` / `(id, MaxDialPoints)`, `Cooldown`, the fixed constants |
| `gatekeepers` | `WaveDirector.StatsFor(kind)` for all three kinds, `GatekeeperAttackInterval` |
| `waves` | `WaveDirector.NextWave(i, 0)` for `i < MaxWaves` — counts, HP multiplier, group size, group gap |
| `dial` | `TetherSiphon.RewardMultiplierFor(MinLevel..MaxLevel)`, and the Wave 1 / Wave 5 body counts at each level |
| `hatch` | `ShipSystems.HatchMaxHp`, `RepairSalvagePerHp`, `MetaUpgrades.HatchHp(MaxLevel)`, `EquipmentCatalog.SlotPrice`, `Def(RepairRig)` cost and rate |
| `tracks` | `UpgradeTracks.All` — every track, its station, its level-0 and level-`MaxLevel` value, and the `NextCost` series with its cumulative |

**Hand-written, and it stays inline next to the table it argues about:**

- The **duty-cycle formula** and its worked line (`(8 * 14) / (8/3 + 1.4) = 27.5 dps`). It is a
  model of how firing and reloading interleave, chosen by a person; the code has no expression
  that returns it, and emitting the arithmetic would state the model as if the build held it.
- The **stamina paragraph**. Two of the five numbers (`moveSpeed` 5, `sprintSpeed` 7.5) are
  `[SerializeField]` on `Game/Player/FirstPersonController.cs` and a prefab outranks them
  (`Architecture.md`, gotcha 1), so a generated figure would be a claim the running game can
  contradict. The other three are consts on the same Game-layer MonoBehaviour, outside what
  the Sim-only harness compiles. The reasoning — sprint beats the 5.6 chase and the Hunter's
  6.5, so unlimited sprint is a free kiting exploit, so the five move together or not at all —
  is an argument in any case.
- **Every derivation on top of an emitted number**: three shots to a Default, 3.9 s and 2.3 s
  to kill a 100 HP player, the Ram's 107 HP of door per arrival, "540 HP of gatekeeper — a
  redistribution, not an inflation", the group-gap read of `spawnInterval`.
- **Every justification**: two cost curves on purpose, one table driving reward and body count
  on purpose, Luck as the only crit source, the wheel's per-point values marked *retune
  freely*.
- **Every play measurement and every open question**: the 1,072 / 1,182 / 1,075 two-Wave
  grosses, the 19 and 21 Salvage of pre-M22 drift, the signal-to-noise ~0.23 against a 20% run
  spread, and each *unknown, needs play data* label. These are history and judgement; the build
  cannot produce them and must never be able to overwrite them.

**How the file carries both halves without a silent overwrite.** Marker splicing, not two
files. The generator reads the committed `4-Data.md`, replaces only the text between
`<!-- GKB:GEN <id> — <source symbols> -->` and `<!-- GKB:END <id> -->`, and copies every other
byte through unchanged. A paragraph added outside a block survives regeneration byte for byte;
a hand edit made *inside* a block is reported by `--check` as drift naming the block id before
the next write reaches it, so the loss is announced rather than silent, and the opening marker
says on its own line which symbols own the region.

Rejected: splitting the countable half into `4-Data-Tables.md`. `Design/` is read in numbered
order and the order is the argument (`AGENTS.md`, section 6); a derivation that has to link to
its own numbers stops being checkable while reading, and the ladder gains a file for no
reader's benefit.

Rejected: whole-file generation with the prose embedded in `Program.cs`, which is how the
Field Manual works. That file has no hand-written half. `4-Data.md`'s arguments are edited far
more often than its tables, and putting them inside a C# string literal makes editing an
argument a recompile.

## Acceptance criteria

1. `Tools/docgen/run.sh --check` exits **0** on a clean tree, with both targets checked, and
   prints one line per target naming it.
2. `Tools/docgen/run.sh` compiles at all. Today it does not: the run fails with
   `error CS0117: 'WeaponTier' does not contain a definition for 'DamageMul'` and exits 2.
   After this milestone, a clean-tree run exits 0 and writes both files.
3. Changing `MetaUpgrades.BaseCost` from `200` to `250` and not regenerating makes
   `Tools/docgen/run.sh --check` exit **1**, and the message names
   `Docs/Design/4-Data.md` and the block id `tracks`.
4. Changing `WaveDirector`'s group-gap slope and not regenerating makes `--check` exit **1**
   naming block `waves`. Changing `WeaponWheel.DamagePerPoint` names block `wheel`.
5. Deleting a value from `UpgradeTrack` and regenerating removes its row from
   `Docs/Design/4-Data.md` and from `Docs/Play/Field-Manual.md` with **no edit to
   `Tools/docgen/Program.cs`**. Adding a value adds both rows the same way. This is the
   criterion the Engineering track would have failed.
6. Appending a sentence to `4-Data.md` **outside** any generated block leaves `--check` at
   exit 0, and a subsequent `run.sh` leaves that sentence byte-identical
   (`diff` of before and after is empty except inside blocks).
7. Editing a number **inside** a generated block makes `--check` exit **1** and print the
   block id in the failure, before any write occurs.
8. A structural fault in the target file exits **2**, not 1, and says which: an unclosed
   `GKB:GEN`, a duplicate block id, a block id the generator has no emitter for, or an
   emitter with no block in the file. `Tools/docgen/run.sh --selftest` exercises all four
   against temp fixtures and exits 0 when each produced exit 2 with its own message.
9. `Docs/Play/Field-Manual.md` after regeneration contains no occurrence of `Engineering`
   and no `Repair Rig` *track* row; it lists the equipment slot at `EquipmentCatalog.SlotPrice`
   and the rig at its catalog cost and rate. `grep -c Engineering Docs/Play/Field-Manual.md`
   returns 0.
10. `Docs/Design/4-Data.md` after regeneration contains no `T0 | T1 | ... | T7` table; the
    weapon progression is the wheel's cost curve and per-point table.
11. EditMode `UpgradeTrackTableTests.EveryTrackHasLabelStationAndValues` passes: every value of
    `UpgradeTrack` returns a non-empty label, one of the four station names in
    `_meta/_definitions/language.md`, and a finite level-0 and level-`MaxLevel` value.
12. `Tools/verify/run.sh` calls `--check` and exits non-zero when either doc is stale; the
    harness output names the docgen check as the cause.
13. The mono probe and the target-typed `new(...)` shim keep their own exit code: with `mcs`
    off `PATH`, `--check` exits **2** with the `mono-devel` line, and a `new(` in `Controls.cs`
    the sed does not rewrite still exits **2** naming the line. Exit 1 means drift and nothing
    else.
14. `Docs/Design/4-Data.md` opens with a generated-file notice naming the blocks and stating
    that prose outside them is hand-written and preserved.
15. **No point ceiling is a literal in `Program.cs`.** Raising `WeaponTier.MaxTier` from 10
    to 11 and regenerating moves the `wheel` block's full-focus columns from 50 / 30 to
    55 / 33, and the `tier-cost` block's cumulative from 13,000 to 15,400, with **no edit to
    `Tools/docgen/Program.cs`**. This is the wheel's version of criterion 5, and it is the
    criterion the `35` / `21` anchors would have failed: `MaxTier` was 7 when they were
    written and they survived the move to 10 unread.

## Implementation

**Create** `Assets/Scripts/Sim/Meta/UpgradeTrack.cs` — moves `enum UpgradeTrack` out of
`Game/Ship/ShipCommands.cs` into `GatekeeperBastion.Sim`, plus
`public static class UpgradeTracks` with `All` (the enum values in declaration order),
`Label(UpgradeTrack)`, `Station(UpgradeTrack)` returning the caps station name, and
`ValueAt(UpgradeTrack, int level)` returning `(float value, string unit)` by dispatching to the
existing `MetaUpgrades` accessors. The enum is data, and `Sim/` is the only layer the doc tools
and the mcs harness compile.

Rejected: compiling `Game/Ship/ShipCommands.cs` into docgen. It references `GameSession` in 15
places, which pulls the save layer and Unity behind it; the Sim-only source set is why the
harness runs in about a second.

**Change** `Assets/Scripts/Game/Ship/ShipCommands.cs` — delete the local enum and the `Label`
and `LevelOf` switch bodies' duplication, delegating to `UpgradeTracks`. `BuyUpgrade` keeps its
switch, because it writes `ref` fields on `MetaState` and that is a command, not data.
Namespace import only; call sites are unchanged.

**Change** `Tools/docgen/Program.cs`:

1. Repair the Tier section against `WeaponWheel` — `DamageMul` / `ClipMul` / `EffectiveDps`
   now take points, and Tier is `total / PointsPerPurchase`. The Field Manual's weapon
   progression becomes the purchase cost curve plus what one purchase awards.
2. Replace the hardcoded station and track literals in `Stations(...)` with a loop over
   `UpgradeTracks.All`, grouped by `Station(...)`. This is the line that let Engineering
   survive: the tracks were string literals in the generator, so the generator could be
   right about `MetaUpgrades` and wrong about which tracks exist.
3. Add a splice engine: `Splice(string committed, IDictionary<string, string> blocks)`, and
   `int Main(string[] args)` taking `--field-manual <path>` and `--data <path>`. Marker grammar
   is `<!-- GKB:GEN <id> — <symbols> -->` … `<!-- GKB:END <id> -->`. Faults from criterion 8
   return 2 with a distinct message each.
4. Add the ten emitters named in the boundary table above, each writing only its block body.

**Change** `Docs/Design/4-Data.md` — insert the ten marker pairs around the regions listed
above, delete the stale `T0..T7` table and the deleted-track prose, and add the header notice
from criterion 14. Everything else stays where it is, in its current order.

**Change** `Tools/docgen/run.sh` — accept `[--check] [field-manual|data|all]`, default `all`;
compile once, run both targets, exit 1 if either is stale and 2 on any could-not-run. Add
`--selftest`. One script rather than a second `Tools/datagen/`, which would duplicate the mono
probe, the `Controls.cs` sed shim and the source globbing three ways.

**Change** `Tools/verify/run.sh` — call `Tools/docgen/run.sh --check` after the test run; a
non-zero exit fails the harness, named in the output.

**Create** `Assets/Tests/EditMode/Meta/UpgradeTrackTableTests.cs` — criterion 11.

**Change** `Docs/README.md` — the standing rule currently names only the Field Manual; it
names both generated files.

## Supporting work

Developer tooling, so Driven / Shown / Reads back are answered for the developer.

- **Driven by** `Tools/docgen/run.sh`, and by `Tools/verify/run.sh` for the developer who
  does not know it exists.
- **Shown by** the failing verify run, and by the generated-file notice at the top of
  `4-Data.md`.
- **Reads back as** the exit code and the per-target line naming the file and the block.

## Verification

1. Criteria 1, 2 — `Tools/docgen/run.sh --check` on a clean tree, expect exit 0 and two named
   target lines. Before the change the same command exits 2 with `CS0117` on
   `WeaponTier.DamageMul`; record that first so the repair is attributable.
2. Criterion 3 — edit `MetaUpgrades.BaseCost` to `250`, run `--check`, expect exit 1 and
   `Docs/Design/4-Data.md` plus `tracks` in the message. `run.sh`, then `--check` again,
   expect 0 and the cost series now reading 250 / 500 / 1,000 / 2,000 / 4,000. Revert, run
   `run.sh`, expect the file back to its committed bytes.
3. Criterion 4 — same loop for `WaveDirector`'s `4.2 - 0.20*waveIndex` and for
   `WeaponWheel.DamagePerPoint`; expect blocks `waves` and `wheel` named. Criterion 15 — set
   `WeaponTier.MaxTier` to `11`, run `run.sh`, expect the `wheel` block reading 55 / 33 and
   `tier-cost` reading 15,400 cumulative with `git diff Tools/docgen` empty; revert and
   regenerate, expect 50 / 30 and 13,000 back.
4. Criterion 5 — add `UpgradeTrack.Ballast` with a `MetaUpgrades` accessor, run `run.sh`,
   expect a new row in both docs with `Program.cs` untouched. Delete it and regenerate; expect
   both rows gone. This is the Engineering regression test, run by hand once.
5. Criteria 6, 7 — append `Test sentence.` after the *Hatch* prose, `--check` expect 0, `run.sh`,
   `diff` expect the sentence intact. Then change a `30` to a `31` inside block `gatekeepers`,
   `--check` expect 1 naming `gatekeepers`.
6. Criterion 8 — `Tools/docgen/run.sh --selftest`, expect 0 and four fault lines.
7. Criteria 9, 10 — `grep -c Engineering Docs/Play/Field-Manual.md` expect 0;
   `grep -c "| T7 |" Docs/Design/4-Data.md` expect 0.
8. Criterion 11 — `Tools/verify/run.sh --filter UpgradeTrack`, expect the fixture green and
   the suite's total unchanged otherwise.
9. Criteria 12, 13 — `Tools/verify/run.sh` with `MetaUpgrades.BaseCost` edited, expect non-zero
   naming the docgen check. Then `PATH= command -v mcs` removed from the environment, expect
   `--check` exit 2 and the `mono-devel` line.
10. Developer path — from a cold checkout: change one number in `Sim/`, run
    `Tools/verify/run.sh`, read the failure, run `Tools/docgen/run.sh`, commit both docs with
    the code change. Confirms the loop is one command, not a sweep of eight files.

## Watch

**The block boundary is the thing that rots.** A number moved from a hand-written paragraph
into a generated block, or the reverse, is invisible to `--check`, which only compares what
the markers already enclose. Criterion 8's "emitter with no block" fault catches one direction;
the other is caught by nobody, so the boundary table above is the record and this file's update
trigger names it.

`Docs/Play/Field-Manual.md` regenerating for the first time in three milestones will produce a
large diff touching weapons, tracks and the ship section at once. Land it in its own commit
from the `Program.cs` repair, so the wheel rewrite is readable.

`Architecture.md` gotcha 1 applies to anything emitted from a Game-layer `[SerializeField]`.
Nothing in the ten blocks reads one, and that is the reason the player movement numbers stayed
hand-written; do not extend a block to reach them.

## Depends on / Unblocks

**M21 shipped 2026-08-09**, so this no longer waits on it; the wheel emitters describe its
shipped shape. The MK-1 ring is still unset on purpose — `WeaponWheel.RingOf` returns null
for `Mk1Sidearm` — so `rings` prints *unset* for the MK-1 until that row is authored.
Unblocks nothing structurally, and shortens the doc step in M24, M25, M26, M30, M31 and M32.
Independent of **M28**, which checks the words in these files while this checks the numbers;
both hang off `Tools/verify/run.sh` and neither reads the other.
