# M110 — The prose agrees with the tables

<!--
Status: later
Update trigger: when lint-constants gains or loses a watched constant, or when a Design doc's numbers authority moves
Last updated: 2026-09-01
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

## Acceptance criteria

1. Every stale line enumerated above states the live value or is rewritten to derive it in
   words from the named constant — and superseded analysis is kept as dated history where
   the house style already does that (`2-Dynamics.md` B1), never silently deleted.
2. `Docs/Decisions.md:542`'s spawn-gap entry records the `7.3 + 0.40*w` supersession the
   same way the M10 entry it sits in recorded the previous one.
3. `Tools/lint-constants/run.sh` gains a **scalar watch**: a declared list of watched Sim
   constants (`ShipSystems.HatchMaxHp`, `WaveDirector.BaseEnemyHp`, the four kind HPs,
   `MetaUpgrades.SalvagePerKill`, `Respawns.BaseCost`/`MaxRespawns`/`BleedOutSeconds`,
   `HatchBank.FieldWaveDamageScale`, the spawn-gap base and slope) whose values are parsed
   from the C# and compared against bare numerals adjacent to the constant's name or its
   plain-English alias in `Docs/Design/`, `Docs/Demo - Single/` and `Docs/Decisions.md`.
4. The check exits non-zero when a watched constant cannot be parsed from the source —
   a deleted rule must never become a passing run.
5. Proven twice: exit 0 on the fixed tree, and a seeded `"the Hatch's 300 HP"` in
   `4-Data.md` is caught by name and line.
6. The check runs inside `Tools/verify/run.sh` — a check that has to be remembered is not
   enforcement.
7. `Docs/Milestones/3. completed/` stays out of scope (doc-freshness exempt list) and the
   run says so in its own output, so a clean pass cannot be mistaken for having audited
   history.

## Implementation

**Change** the five documents named in criterion 1 — prose only; `GKB:GEN` blocks are
docgen's and are already correct.
**Change** `Tools/lint-constants/` — add the scalar watch beside the array comparison; the
watched list lives in the tool's own header as data, one line per constant with its
aliases ("the door", "Hatch HP"), because the doc side has no single phrasing to parse a
rule from.
**Change** `Tools/lint-constants/` selftest — two new fixtures: the seeded scalar hit, and
a source file with a watched constant renamed away (must exit 2).

Rejected alternative: regenerating the prose from docgen. The prose's value *is* the
argument around the numbers; generating it would delete the reasoning to save the
arithmetic. Rejected alternative: a docs-wide sweep of every numeral — the false-positive
rate on distances, dates and percentages would teach everyone to ignore the tool.

## Verification

1. Criteria 1–2 — read the five files at the cited lines; each states the live value.
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
