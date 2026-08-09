# M28 — Vocabulary that cannot rot

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

A committed check that fails the build when a retired term reappears, when builder
vocabulary leaks into the player wiki, or when a station name loses its caps.

## Done when

**Met.** `Tools/lint-language/run.sh` reads the `language-lint` fence in
`_meta/_definitions/language.md`, exits 0 on the tree, exits non-zero on seeded
violations naming `file:line` and the term, and is called from `Tools/verify/run.sh`
(verify exits 1 with `language check failed` when the lint fails). Live Title-Case
`Armory` strings in `HudController` / `PlayerGun` corrected to `ARMORY`.

## What shipped

- `Docs/_meta/_definitions/language.md` — `language-lint` fence (retired, builder_play_ban,
  stations_title, boarder_enemy).
- `Tools/lint-language/run.sh` — shell + `rg --pcre2`; allowlist per criterion 2; collocational
  boarder-for-enemy with the heuristic note printed on every run.
- `Tools/verify/run.sh` — runs the lint before mcs.

## Story

As the developer, I want the language table enforced by a command rather than by memory,
so that a rename stays done after the session that did it.

## Why it matters

The 2026-08-08 rename is the evidence. `Boarder` did not retire, it **reversed meaning** —
it used to name the enemy and now names the player — so every pre-rename sentence still
reads plausibly and means the opposite side. That is trap 5 in
`../../_meta/_definitions/language.md`, and it is the one class of drift that cannot be
caught by reading, because nothing looks wrong.

The same pass turned up four contradictions that a check would have caught the moment they
were written: a row whose term appeared in its own *Never called* column, rule 3 citing a
file that does not define the terms it names, a definition using a short form its own row
had just banned, and a paragraph claiming a term takes no row three lines above its row.
None were subtle. All survived because nothing looks.

**Multiplier.** `Tools/docgen/run.sh --check` already proves the Field Manual's *numbers*
match the code. Nothing proves its *words* do, and the wiki is 20 hand-written pages plus
every milestone and design file. This check makes every future rename a one-command
verification instead of a manual sweep, and the project has now done two large ones
(M11's slots, this one). It outlives M27 by the test in the milestone contract: it would
still run after the milestone that motivated it shipped.

Leverage tier (`../../Working-Rules.md`): **#6, information flows** — for the developer.
It changes nothing the player sees, which is why it needs the tooling exception rather
than a player Story.

## Acceptance criteria

1. `Tools/lint-language/run.sh` exits 0 on the tree as it stands today, and non-zero on a
   seeded violation, naming `file:line` and the term.
2. **Retired terms fail anywhere outside an allowed context**: `derelict`, `Podded`,
   `breach point`, `muster point`, and `boarder` used for an enemy. The allowed contexts
   are the language table itself, `_to_delete/`, `_stage_snap/`, archived logs, and
   milestone files in `3. completed/` (shipped files are a record and stop changing).
3. **Builder vocabulary fails inside `Docs/Play/`**: `gatekeeper-seconds`,
   `arrivals x dwell`, `realization`. This is rule 3, which today is enforced by nobody.
4. **Station names fail when not in caps in player-facing strings**: `Armory`,
   `Crew Quarters`, `Fabrication`, `Nav Map` inside a string literal in `Assets/Scripts`
   or in prose in `Docs/Play/`. This caught a real one on 2026-08-08 —
   `"Buy the MK-1 at the ship's Armory."` sat beside a sibling line that got it right.
5. The retired list and the station list live in **one machine-readable block inside
   `../../_meta/_definitions/language.md`**, not in the script. The table stays the single
   source of truth; the script reads it. A term retired without touching the table is a
   term the check does not know about, which is the failure mode this whole milestone is
   about.
6. The check runs from `Tools/verify/run.sh` and fails that run, so it is not a thing
   someone has to remember to invoke.
7. `boarder`-for-enemy is detected by **context, not by the bare word** — the word is
   legal and now means the player. Criterion 2 is satisfied by flagging the pre-rename
   collocations (`boarders expected`, `the boarders`, `boarder kind`, `boarder-seconds`)
   rather than every occurrence, and the milestone states plainly that this is a heuristic
   with false negatives.

## Supporting work

- **Driven** — `Tools/lint-language/run.sh`, and `Tools/verify/run.sh` for the developer
  who does not know it exists.
- **Shown** — a failing verify run naming the file, line and term.
- **Reads back** — exit code, and one line per violation.

## Implementation

**Create** `Tools/lint-language/run.sh` — shell and grep, matching how `Tools/docgen`
and `Tools/verify` already work. No new language runtime; `mcs` is already a constraint
this project works around, and adding a second toolchain to lint prose is not worth it.

**Change** `../../_meta/_definitions/language.md` — add one fenced block holding the
retired terms, the builder-only terms and the caps-required station names, in a format a
shell script can read. Put it near the traps, and say in prose that the block is read by
the check so nobody deletes it as clutter.

**Change** `Tools/verify/run.sh` — call the check, fail the run on non-zero.

Rejected: making this an EditMode test. The violations live in markdown as much as in
C#, and the test assembly cannot see `Docs/`.

Rejected: auto-fixing. A rename that reverses a word's meaning cannot be applied
mechanically without reading each site, which is precisely how the 2026-08-08 sweep
produced a class named `SpawnPoint` in a file named `BreachPoint.cs` — a mismatch Unity
rejects at load. That one was repaired the same day; the point stands because nothing
caught it but a human rereading the diff.

## Verification

1. Criterion 1 — run it; expect 0. Then add `the derelict is stripped` to any wiki page
   and expect non-zero naming that file and line. Remove it.
2. Criterion 3 — add `arrivals x dwell` to `Docs/Play/Waves.md`; expect a failure. Today
   this passes silently.
3. Criterion 4 — add `"return to the Armory"` to any script; expect a failure.
4. Criterion 5 — retire a term by adding it to the block only, touching no script; the
   check picks it up on the next run.
5. Criterion 6 — `Tools/verify/run.sh` with a seeded violation exits non-zero and the
   report names the language check as the cause.

## Watch

Criterion 7 is where this earns distrust if oversold. The check cannot tell you that a
sentence written before the rename means the wrong thing; it can only flag the phrasings
that usually accompany it. Say so in the script's own output, or the first clean run will
be read as proof the tree is clean.

## Depends on / Unblocks

Depends on nothing. Best run **after M27**, whose new prose is the first material it has
not already been pointed at.
