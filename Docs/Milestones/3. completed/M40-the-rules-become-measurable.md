# M40 — The rules become measurable

<!--
Status: later
Update trigger: when a cap's number or unit changes, when a grandfathered exception is added or removed, or when two Architecture files disagree
Last updated: 2026-08-09
-->

Every rule in `Docs/Architecture/` that M41's check will read is first made **decidable**: one
unit of measure stated once, a number for every folder, the grandfathered exceptions named
rather than counted, and the two places the documents contradict each other resolved.
**No code changes. This milestone is entirely documents, and it exists because a rule nobody
can decide cannot be checked.**

## Story

As the developer, I want every architecture rule to have exactly one reading, so that two
honest audits of the same tree cannot disagree.

## Why it matters

**Two audits of this codebase disagree, and both are defensible.** Under `wc -l`, eight files
are over cap. Under non-blank non-comment lines, **not one file in `Sim/` or `Game/` is over** —
the largest are `MetaState.cs` at 550 against 600 and `ShipCommands.cs` at 387 against 400.
`Architecture/5-Audit-2026-08-09.md:116` reports `PlayerGun.cs` at 471; today it is 481 raw and
386 code. **The rule as most files state it says "lines" and stops there.**

**The unit has in fact been chosen — once, in one file, and nowhere else.**
`Architecture/7-File-Layout.md:55`:

> Regions do not count toward the line cap. Sim ≤ 600, Game ≤ 400 (`Docs/Architecture.md`),
> measured as non-blank, non-comment, **non-directive** lines.

`Architecture.md:91` states the same caps with no unit. This milestone's first job is
therefore **reconciliation, not invention** — and the `non-directive` clause is the part an
implementer will drop, because it is the least obvious third of the definition and it is what
makes the regions sentence coherent.

**`Editor/` has never had a rule at all.** 9 files, 3,897 raw lines, and it holds the
project's largest and second-largest files — `BastionUiAssembler.cs` at 802 code lines and
`ShipUiAssembler.cs` at 642. A folder outside the rules by omission is the failure mode this
whole line of work exists to remove.

**"Two are grandfathered. There is no third" names neither, and there are three.**
`Architecture/README.md:15`. Measured: `GameController.Instance` (40 call sites),
`HatchStation.Instance` (5), `DecoyBay.Instance` (3). Plus `StationPanelView.EnsureInstance()`,
a static factory that is the same pattern under another name. **A count is not a rule** — it
passes forever by renaming, and it cannot tell an implementer which one to remove.

Leverage tier (`../../Working-Rules.md`): **#12, numbers** — this changes constants and
wording and nothing else. It earns its place only as the thing M41 is unable to be built
without.

**Multiplier**: M41 reads every one of these. A check built against an undecided rule
hardcodes somebody's guess and then outranks the document.

## Acceptance criteria

1. **One definition of a counted line exists in the project, and it is
   `Architecture/7-File-Layout.md:55`'s, verbatim: non-blank, non-comment, non-directive.**
   `Architecture.md` links to it rather than restating it. **A grep for the wording across
   `Docs/` returns the one file that states it, and links from everywhere else — no second
   phrasing anywhere.** Dropping `non-directive` is the specific failure this criterion
   guards: it is what makes "regions do not count toward the cap" true.
2. **`Editor/` has a number**, stated in `Architecture.md` in the same sentence, the same
   unit and the same shape as `Sim/` and `Game/`. **An exemption is not available.** If the
   honest number is larger than `Game/`'s because editor assemblers are long by nature, state
   that number and the one-line reason it differs. **Recording the number does not oblige
   anyone to split a file today** — it obliges the next person to notice.
3. **The two grandfathered singletons are named by type**, not counted.
   `Architecture/README.md:15` names them. **This milestone makes the call and does not hand
   it to an implementer**: the survivors are `GameController` and `HatchStation` — the first
   is the command surface every sim mutation already routes through, the second is referenced
   from five sites including the melee-range convention in `Architecture.md` gotcha 2.
   **`DecoyBay.Instance` is the third and is recorded as owed**, with its three call sites
   (`HudController.cs:308`, `HudController.cs:362`, `EnemyGatekeeper.Targeting.cs:139`) and
   the note that the bay is reachable through `GameController` like everything else.
   **Removing it is not this milestone's work** — naming it is.
4. **`StationPanelView.EnsureInstance()` is recorded in the same place**, as the static-factory
   form of the same pattern, so it is not discovered later and treated as a fourth exception.
5. **`Architecture.md:91`'s "Split with `partial` … rather than growing past it" stops
   standing alone** and points at `Architecture/1-Boundaries.md` section 4's test — *would the
   split files pass as separate classes if the language allowed it?* **This is a pointer edit,
   not a rule change**: `1-Boundaries.md` is CRITICAL authority and `Architecture/README.md:54`
   already gives the folder precedence on rules about the craft. The rule changed when
   `1-Boundaries.md` was written; only the stale sentence is left.
6. **`Architecture/5-Audit-2026-08-09.md` takes a correction, and the correction is true.**
   It is a record and records take corrections, never retro-edits
   (`../../_meta/_definitions/milestone.md`). **The correction is that its file-size figures
   match no measure now in use** — `MetaState.cs` is recorded at 590 and is today 644 raw and
   550 code; `PlayerGun.cs` is recorded at 471 and is today 481 raw and 386 code;
   `DevConsole.cs` at 411 matches raw only. **Do not write "it counted raw lines"** — that is
   the tidy explanation and the numbers refute it.
7. **Every number in criteria 1 to 4 is stamped with the date it was measured** and the
   command that measured it, so the next audit can reproduce it rather than re-derive it.
8. **No file under `Assets/` is edited.** `git diff --stat` after this milestone touches
   `Docs/` only.

## Implementation

**Change** `Docs/Architecture.md` — the link to the line-counting definition (criterion 1),
the `Editor/` number (criterion 2), the `partial` pointer (criterion 5).

**Change** `Docs/Architecture/README.md` — the two named singletons and the two recorded
exceptions (criteria 3, 4).

**Change** `Docs/Architecture/7-File-Layout.md` — nothing about the definition itself; add
only the note that `Architecture.md` now points here and that M41's check reads it, so nobody
deletes it as duplication.

**Change** `Docs/Architecture/5-Audit-2026-08-09.md` — append the correction (criterion 6).

**Change** `Docs/Changelog.md` — one entry, because criteria 2 and 3 add rules that did not
exist.

Rejected: choosing the raw-line measure. It counts documentation as debt, which pushes
against `Architecture/2-Naming.md`'s comment rules, and it directly contradicts
`7-File-Layout.md:55`'s regions sentence.

Rejected: leaving `Editor/` exempt. Every file currently over any cap is in `Editor/Ui/`, so
an exemption would make the cap rule unable to fire on anything that exists in this tree. A
rule that cannot fail is not a rule.

Rejected: removing `DecoyBay.Instance` here. It is three call sites of real refactoring in a
milestone that otherwise edits no code, and criterion 8 is worth more than the tidiness.

## Supporting work

- **Driven** — read by M41's check, and by anyone auditing the tree.
- **Shown** — one wording in one file, linked from the rest.
- **Reads back** — the greps in the verification below.

## Verification

1. Criterion 1 — `grep -rn "non-blank" Docs/` returns exactly one statement of the definition;
   every other hit is a link to it.
2. Criterion 2 — `Architecture.md` states three folder caps and three numbers, in one unit.
3. Criteria 3, 4 — `Architecture/README.md` names two types and records two exceptions with
   `file:line`. `grep -rn "static.*Instance" Assets/Scripts/Game/` returns exactly the three
   plus `StationPanelView.EnsureInstance`, and each appears in the document.
4. Criterion 6 — read the appended correction against the three measured pairs above.
5. Criterion 8 — `git diff --stat` shows `Docs/` only.

## Watch

- **This milestone is one session of editing documents and it is the easiest thing in the
  project to do instead of the project** (`../../Roadmap.md`, risks). Its only justification
  is M41. **If M41 is not started in the same working period, this was procrastination** and
  the honest response is to say so on the file.
- **The `non-directive` clause is the part that will be dropped.** It is a third of a
  definition that reads like a rounding error and it is the reason the regions rule holds.
- **Criterion 3 makes a call Zo may want to make differently.** `DecoyBay` is named as the odd
  one out on call-site count and on the fact that `GameController` already owns sim mutation;
  if the preference is to grandfather `DecoyBay` and retire `HatchStation` instead, that is a
  one-line change to this criterion and the rest of the milestone is unaffected.

## Depends on / Unblocks

Depends on nothing. Independent of M39 and of M35.

**Blocks M41**, which cannot be built against rules that have not been decided.
