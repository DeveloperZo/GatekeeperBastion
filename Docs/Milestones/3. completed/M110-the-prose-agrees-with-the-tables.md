# M110 — The prose agrees with the tables

<!--
Status: later
Update trigger: when lint-constants gains or loses a watched constant, or when a Design doc's numbers authority moves
Last updated: 2026-09-01 (recut on first contact with the tree — see Recut below)
-->

`4-Data.md` is the numbers authority, and half its hand-written prose argues from retired
constants while the generated blocks beside it tell the truth. Fix the drift, then extend
`Tools/lint-constants` so a lone scalar can drift no more silently than an array can.

## Story

As the developer, I want every number a Design doc states in prose to agree with the
constant the code compiles, so that a tuning session argued from the docs is argued from
the game that exists — not from the one that existed in mid-August.

## Why it matters

The August retunes moved the door to 600, the roster to `11 + 7i`, the Default ladder to
50/70/90/110/130 and the spawn gap to `7.3 + 0.40*w` — and the prose kept the old numbers
in at least a dozen places while the `GKB:GEN` blocks centimetres away were regenerated:
`../../Design/4-Data.md` lines 81, 99, 104, 111, 112, 115, 205, 326, 328, 346, 456;
`../../Design/0-Gameplay.md:151`; `../../Decisions.md:542`; `../../Demo - Single/2-first-hatch-bank.md:68`
and `../../Demo - Single/README.md:220`. This drift class has already bitten as *code*: the
door raise was a silent no-op for a day because `MetaUpgrades.HatchHp` restated `300f`
instead of reading `ShipSystems.HatchMaxHp`. `lint-constants` cannot see any of it — its
own closing note says numeric **array** literals only.

Tooling is admitted with its multiplier: `M109` checks boxes against these docs, `M112`
tunes dials argued from them, and every future retune re-creates this exact debt unless
the check exists. It outlives its motivating milestone by construction.

## Recut, 2026-09-01

Written 2026-09-01 and recut the same day against the tree, before any of it was built.
Six things the milestone as first written could not do. The gate below is the recut one;
this block is the record of what changed and why, so the edit is not silent.

1. **Criterion 6 was already true.** `Tools/verify/run.sh:78` already runs `lint-constants`
   and fails the gate on it — it has since the array check shipped. A criterion an
   unchanged tree passes is not a gate (`../../Working-Rules.md`, Evidence), so 6 is recut
   to the thing that is *not* yet true: the run naming the scalar stage in its own output.

2. **Criterion 3 named five things that are not constants, and criterion 4 then failed the
   run for it.** Hunter, Thief and Cloaker hit points were inline literals inside
   `WaveDirector.StatsFor`'s switch, and the spawn-gap base and slope were inline literals
   in `NextWave`. Only Default's HP was ever a named `const`. So criterion 4 ("exit
   non-zero when a watched constant cannot be parsed") and criterion 5 ("exit 0 on the
   fixed tree") could not both hold. **Resolved by naming them first** — `HunterHp`,
   `ThiefHp`, `CloakerHp`, `SpawnGapBase`, `SpawnGapPerWave`, same values, no behaviour
   change. `WaveDirector` already argued for exactly this over its Default block ("one copy
   of each number, not two that can drift"); the other three kinds simply never got it, and
   that is *why* the docs could carry the pre-2026-08-27 figures with nothing going red.

3. **The watch list does not live in the tool.** The Implementation section first said it
   would sit "in the tool's own header as data". `check.py` states the opposite invariant:
   "It carries no rule value of its own: a missing fence or a missing key is exit 2, never
   a default." Every rule value is read from the ```constant-lint fence in
   `../../Working-Rules.md`, because a check that hardcodes its rule becomes a second
   source of truth and outranks the document. **The watch list goes in the fence.**

4. **A scalar watch was already built, measured and rejected — and the first draft did not
   say so.** `../../Working-Rules.md` records it: dropped at 6 hits, every one a false
   positive, because `Build`, `Count` and `Curve` are English words before they are
   symbols. That rejected design scanned *every* `const`. This one scans a **declared
   allow-list with explicit aliases**, which is a different instrument and dodges the
   measured failure mode — but the document would contradict the shipped tool the moment
   this landed. `Working-Rules.md` is therefore **added to criterion 1's edit set**, with
   the 2026-08-26 measurement kept as dated history rather than deleted.

5. **The scan surface was missing the place the drift actually was.** Criterion 3 first
   listed docs only. `WaveDirector.cs`'s own XML comment still documented
   `max(0.4, 4.2 + 0.20*min(waveIndex, MaxWaves-1))` against the live `7.3 + 0.40` twelve
   lines below it — an in-code restatement, the exact shape of the 2026-08-26 Cloaker
   drift the array check exists for, and the fence already scans `code_dirs` for that
   reason. Code comments are in scope, and that line is added to criterion 1.

6. **Two name collisions the watch has to survive.** `BaseCost` is declared twice with
   different values (`MetaUpgrades.BaseCost = 200`, `Respawns.BaseCost = 150`), and `0.40`
   is both `EnemyHealthPerWave` and `SpawnGapPerWave` — different levers. Watch entries
   resolve type-qualified, never by bare name.

7. **The enumerated five-file list was too small for the gate it was paired with, and the
   first working watch was 73% false.** Criterion 5 asks for exit 0 on the fixed tree, but
   the watch's first real run reported drift in eleven documents the milestone never
   listed. Fixing the five and shipping would have left the tool permanently red — the
   outcome this file's own rejected-alternative warns about ("would teach everyone to
   ignore the tool").

   Reading all fifteen hits changed the diagnosis. **Eleven were false**, worse in
   proportion than the 6-of-6 design rejected in August, and they were not one bug:

   - **Two documents are false by construction.** `../../Marketing/gaps.md` and
     `../../Marketing/pitch.md` are drift *registers* — gaps' table header is literally
     "Claim, and where it lived | The problem | Honest line". A document whose subject is
     stale numbers trips a stale-number check forever. `../../GatekeeperZo Notes/playthrough.md`
     is a play record whose header says the log holds the numbers and this holds what it
     was like; its figures are dated observations, and rewriting them falsifies a record.
     All three are exempt, on the `Docs/_meta/Logs` precedent. **The cost is stated, not
     glossed: exempting a register also exempts the register's own correct answers.**
   - **Four were history or a different lever stated correctly** — `WeaponCatalog.cs`
     recording `BaseEnemyHp went 30 -> 50`, `Demo - Coop/README.md` quoting
     `MasterControl.FinaleEliteHp`'s 240 on a line about a Thief.
   - **Two were formula lines** where a constant is an operand, not a subject — including
     the `WaveDirector` comment fixed under Recut 5.
   - **Two were this file**, quoting the stale figure it exists to catch. Criterion 5's
     fixture string now lives in `--selftest`, where a literal belongs.

   Five narrow rules, each measured alone and each with a silence/catch fixture pair,
   take the run to clean. One rule proposed during the recut — *a number equal to some
   other Sim constant's value is not drift* — was **measured and refused**: it silences
   three of the four genuine catches, because `EquipmentCatalog.SlotPrice` is 300,
   `RealmShare.StaleSeconds` is 180 and `ShipCamera.FovDegrees` is 30. The shipped rule
   requires the owning type to be *named on the line*.

   The lesson generalises past this milestone: **a drift check's cost is its false
   positives, and they are only visible on the real tree.** Precision was most of the
   work, and none of it could have been specified in advance.

## Acceptance criteria

1. Every stale line enumerated above — **plus `../../Working-Rules.md`'s scalar-constant
   bullet (Recut 4) and `WaveDirector.cs`'s `spawnInterval` comment (Recut 5)** — states
   the live value or is rewritten to derive it in words from the named constant — and superseded analysis is kept as dated history where
   the house style already does that (`2-Dynamics.md` B1), never silently deleted.
2. The spawn-gap entry in `Docs/Decisions.md` records the `7.3 + 0.40*w` supersession the
   same way the M10 entry it sits in recorded the previous one.
3. `Tools/lint-constants/run.sh` gains a **scalar watch**: a declared list of watched Sim
   constants (`ShipSystems.HatchMaxHp`, `WaveDirector.BaseEnemyHp`, the four kind HPs,
   `MetaUpgrades.SalvagePerKill`, `Respawns.BaseCost`/`MaxRespawns`/`BleedOutSeconds`,
   `HatchBank.FieldWaveDamageScale`, the spawn-gap base and slope) whose values are parsed
   from the C# and compared against bare numerals adjacent to the constant's name or its
   plain-English alias in `Docs/Design/`, `Docs/Demo - Single/` and `Docs/Decisions.md`.
4. The check exits non-zero when a watched constant cannot be parsed from the source —
   a deleted rule must never become a passing run.
5. Proven twice: exit 0 on the fixed tree, and a seeded stale Hatch-HP claim in
   `4-Data.md` is caught by name and line. The literal fixture string lives in
   `--selftest`, not here — a milestone that quotes the stale figure trips its own check.
6. `Tools/verify/run.sh` names the **scalar-watch stage** in its own output. (It has
   already run `lint-constants` since the array check shipped — see Recut 1 — so
   "the check runs inside verify" is not a gate. What is new is that a reader of the
   verify output can tell the scalar watch ran at all.)
7. `Docs/Milestones/3. completed/` stays out of scope (doc-freshness exempt list) and the
   run says so in its own output, so a clean pass cannot be mistaken for having audited
   history.

## Implementation

**Change** the documents named in criterion 1 — prose only; `GKB:GEN` blocks are docgen's
and are already correct.
**Change** `Assets/Scripts/Sim/Waves/WaveDirector.cs` — name the five literals the watch
has to be able to parse (Recut 2): `HunterHp` 33, `ThiefHp` 200, `CloakerHp` 75,
`SpawnGapBase` 7.3, `SpawnGapPerWave` 0.40. Same values, no behaviour change; the switch
arms and `NextWave` read the names instead of repeating the numbers.
**Change** `Docs/Working-Rules.md` — the ```constant-lint fence gains the watch list, one
entry per constant, type-qualified, with its plain-English aliases ("the door", "Hatch
HP"), because the doc side has no single phrasing to parse a rule from. It goes in the
fence and **not** in the tool (Recut 3): the tool carries no rule value of its own. The
section's scalar-constant bullet is rewritten in the same pass (Recut 4).
**Change** `Tools/lint-constants/` — add the scalar watch beside the array comparison,
reading the new fence keys, over the doc dirs *and* the code-comment surface (Recut 5).
**Change** `Tools/lint-constants/` selftest — two new fixtures: the seeded scalar hit, and
a source file with a watched constant renamed away (must exit 2).

Rejected alternative: regenerating the prose from docgen. The prose's value *is* the
argument around the numbers; generating it would delete the reasoning to save the
arithmetic. Rejected alternative: a docs-wide sweep of every numeral — the false-positive
rate on distances, dates and percentages would teach everyone to ignore the tool.

## Verification

0. The `WaveDirector` naming pass is behaviour-preserving, so the offline EditMode suite
   must read identically before and after it. `mcs`/`mono` are absent on this box; the
   substitute is a throwaway `dotnet` build of the same two stages against
   `Tools/verify/Shims.cs`, and its limits are stated with its result rather than glossed.
1. Criteria 1–2 — read the cited lines in the five documents, plus `Working-Rules.md` and
   `WaveDirector.cs`; each states the live value or is marked as dated history.
2. Criteria 3–5 — `bash Tools/lint-constants/run.sh` exit 0; seed the criterion-5 string,
   expect nonzero naming file and line; revert; `--selftest` exit 0 with the two new
   fixtures listed.
3. Criterion 6 — `Tools/verify/run.sh` output names the scalar watch stage (toolchain
   guard permitting; on this box the harness substitute is running the lint standalone,
   which is already how lint-language is run).
4. Playthrough path — none; this milestone has no runtime surface. Its player-visible
   effect is that the next tuning session reads true numbers.

## Watch

- dotnet docgen emits CRLF into spliced blocks; any regeneration done while editing
  `4-Data.md` must strip CR after (`gkb-compile-check` memory note).
- `lint-constants`' existing note — "a clean run is not proof the docs are right" — stays
  true; the scalar watch narrows the blind spot, it does not close it.

## Do not build

- A generator that rewrites prose.
- Watching Game-layer or UI constants — the watch list is Sim tuning constants only, the
  ones a balance argument cites.

## Depends on / Unblocks

Depends on: nothing. Unblocks: honest evidence for `M112`, and every future retune's doc
pass shrinking from an audit to a lint run.
