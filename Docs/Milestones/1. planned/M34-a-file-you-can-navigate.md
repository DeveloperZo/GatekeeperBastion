# M34 — A file you can navigate

<!--
Status: later
Update trigger: when the region order changes, or when the layout check moves into AutoVerify
Last updated: 2026-08-09
-->

Every C# file in `Assets/Scripts` gets the region order from
`../../Architecture/7-File-Layout.md`, and a committed check fails the verify run when one
does not.

## Story

As the developer, I want a member's category to be a stated fact in the file rather than a
guess from where it happens to sit, so that "split this file" and "cache this in `Awake`"
become answerable instructions instead of judgement calls.

## Why it matters

**The size cap has nowhere to point.** `../../Architecture.md` says split at 400 lines in
`Game/`; `PlayerGun.cs` is 471 and `DevConsole.cs` is 411
(`../../Architecture/5-Audit-2026-08-09.md`). "Split it" cannot be executed without first
deciding what the seam is. With regions, the seam is already written down.

**And the failure mode is documented, in Zo's own prior project.** InfinityQube's file-size
rule was marked CRITICAL, backed by `check_file_sizes.bat`, and its December 2025 audit found
twelve files over limit — `WaveManager.cs` at 2,083 lines against a 600 cap, 247% over, with
the limits themselves later revised upward to accommodate them
(`../../Architecture/6-InfinityQube-Precedence.md`). The rule was not wrong. It was
unenforced, because the script had to be invoked. **That is the whole argument for criterion
7**, and it is why this milestone is not just a formatting sweep.

**Multiplier.** Every future milestone that adds a member to an existing file gets a stated
place to put it, and every future "this file is too big" gets a stated seam. The check
outlives this milestone by the test in `../../_meta/_definitions/milestone.md`: it runs on
every verify from here on, on files that do not exist yet.

Leverage tier (`../../Working-Rules.md`): **#6, information flows** — for the developer. It
changes nothing the player sees, which is why it needs the tooling exception and a developer
Story rather than a player one.

## Acceptance criteria

1. Every type in `Assets/Scripts` with **three or more members** carries regions in the order
   given by `../../Architecture/7-File-Layout.md` — the Unity order for `Game/`, `Editor/`
   and `Util/`, the Sim order for `Sim/`.
2. **Presence is optional, order is not.** A region that would be empty is absent; an empty
   `#region`/`#endregion` pair anywhere is a failure.
3. **No member is added, removed, renamed or resignatured.** The multiset of member
   signatures per type is byte-identical before and after, and this is asserted by the
   verification in step 1 below rather than by review.
4. **Files whose class body contains a preprocessor directive are wrapped, not reordered**,
   and carry a one-line comment saying why. `DevConsole.cs` is the known case.
5. **Fields whose initializer references another field of the same type keep their relative
   order.** The check reports them; the implementer does not move them.
6. `Sim/` files contain no `Inspector`, `References` or `Unity Lifecycle` region. If one is
   needed, that is a seam violation and the milestone stops rather than adding the region.
7. **`Tools/lint-layout/run.sh` exits 0 on the tree as it stands after the sweep, and
   non-zero on a seeded violation, naming `file:line`, the type and the misplaced region.**
8. The check runs from `Tools/verify/run.sh` and fails that run, so it is not something
   anyone has to remember to invoke (criterion 7's whole point).
9. **The region order lives in one machine-readable block inside
   `../../Architecture/7-File-Layout.md`**, not in the script. Same rule M28 sets for the
   language table: the document is the source of truth and the script reads it.
10. `Tools/verify/run.sh` and the Unity EditMode suite both pass unchanged after the sweep —
    **227 tests, same count, same names.**

## Implementation

### 1. The sweep

**Change** every file under `Assets/Scripts` meeting criterion 1. Roughly 48 files; the
count is not the work, the safety is.

**Do it with a brace-depth parser, not regex.** Attributes, XML doc comments,
expression-bodied members and nested types all break a line-oriented pass. The member's
leading trivia — doc comment, attributes, blank line — moves with the member or the file
loses its documentation.

**Order within a region is preserved.** This milestone groups; it does not sort. Sorting
alphabetically inside a region is a second, larger diff and is explicitly not wanted.

**Skip and report rather than guess.** Any file the parser is not confident about — a
directive inside the class body, an unbalanced construct, a field initializer dependency —
is left alone and listed in the milestone's close-out note. A partial sweep with an honest
skip list beats a complete sweep with one silent corruption.

### 2. The check

**Create** `Tools/lint-layout/run.sh` — shell and a small C# or Python reader, matching how
`Tools/docgen` and `Tools/verify` already work. It reads the order from the fenced block in
`7-File-Layout.md` (criterion 9), walks `Assets/Scripts`, and reports any type whose regions
appear out of order, any empty region, and any `Sim/` file carrying a Unity-only region.

**Change** `Tools/verify/run.sh` — call it, fail the run on non-zero.

**Change** `../../Architecture/7-File-Layout.md` — add the fenced block holding both orders,
with a line of prose saying the check reads it so nobody deletes it as duplication.

Rejected: an EditMode test. The rule is about file text, and the test assembly reads types
through reflection, where region markers do not exist — they are compiler trivia. A test
literally cannot see what this milestone enforces.

Rejected: a Roslyn analyzer. Correct in principle and it would run in the IDE, but it is a
second toolchain and a package dependency for a rule a 100-line script covers.

Rejected: doing the sweep by hand in a chat session. That was the original plan and it is
how a mechanical edit across 48 files produces one silent deletion nobody notices for a week.

## Supporting work

Developer tooling, so the three questions are answered for the developer.

- **Driven** — `Tools/lint-layout/run.sh`, and `Tools/verify/run.sh` for the developer who
  does not know it exists.
- **Shown** — a failing verify run naming the file, the type and the misplaced region.
- **Reads back** — exit code, and one line per violation.

## Verification

1. **Criterion 3 first, before anything else is checked.** Extract the sorted multiset of
   member signatures per type from `git stash`'s before-state and from the after-state, and
   diff them. Expect an empty diff. A sweep that passes every other criterion and fails this
   one has deleted code.
2. Criteria 1–2, 6 — `Tools/lint-layout/run.sh`; expect 0.
3. Criterion 7 — move one region above another in any file; expect non-zero naming that file,
   type and region. Restore.
4. Criterion 8 — seed the same violation and run `Tools/verify/run.sh`; expect non-zero with
   the layout check named as the cause.
5. Criterion 9 — add a region name to the fenced block only, touching no script; the check
   picks it up on the next run.
6. Criterion 10 — `Tools/verify/run.sh` and Unity EditMode; expect the same test count and
   names as before the sweep. **A changed test count means the sweep changed behaviour.**
7. Criterion 4 — `grep -n "#if" Assets/Scripts/Game/Dev/DevConsole.cs` and confirm the file
   carries the exemption comment.

## Watch

- **This milestone is a chore wearing a tool's clothes, and criterion 7 is what makes it not
  one.** If the check slips and only the sweep ships, the roadmap has spent a slot on
  formatting. Ship the check or ship nothing.
- **The diff will be enormous and will bury anything else in the same commit.** The sweep is
  its own commit, with nothing else in it, so `git log -p` stays readable and a later bisect
  can skip it.
- Regions are compiler trivia. They do not survive into reflection, IL, or a stack trace, and
  they cannot be asserted from a test. Everything about this milestone is checked by reading
  files, which is why the check is a script and not a fixture.
- `PlayerGun.cs` and `DevConsole.cs` are over the line cap and **stay over it here.** Naming
  the seam is this milestone; cutting it is a later one, and doing both at once makes
  criterion 3 uncheckable.

## Depends on / Unblocks

Depends on nothing. Best run **before M31 and M32**, which both edit `PlayerGun.Firing.cs`
and `WeaponCatalog.cs` — landing the sweep first means their diffs stay readable.
