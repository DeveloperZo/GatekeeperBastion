# M42 — A repository that reads as clean

<!--
Status: later
Update trigger: when .gitattributes changes, when an asset pack is re-imported, or when the LFS allowance is approached
Last updated: 2026-08-10
-->

`git status` on an untouched checkout prints `nothing to commit, working tree clean`.
Today it prints **141 modified files with zero real changes**. History is rewritten once, as
a single clean root commit, with binaries in LFS and line endings normalised by a file that
travels with the repository.

**Written to be executed by a coding agent.** Every check states its expected output and what
to do when it does not appear. **Four steps are hard aborts** — an agent that continues past
one produces a rewritten history that is worse than the one it replaced.

## Story

As the developer, I want an untouched checkout to read as clean, so that `git status` and
`git diff` are usable signals rather than noise I have to look past.

## Why it matters

**141 tracked files show as modified and not one of them has a real change.** Measured
2026-08-10: `git diff --ignore-cr-at-eol` across the whole tree prints nothing. Every file is
DOTween, Odin Inspector or MCPForUnity — packages nobody in this project has edited.
`core.autocrlf` is unset both locally and globally, so the working tree and the index
disagree about line endings and always will.

**The cost is not tidiness, it is that two tools stop working.** `git status` cannot show
what changed and `git diff` cannot be read. This lands the same week the repository becomes
load-bearing for the milestone process — `../../Roadmap.md` says a status change is
`git mv` plus the table row **in one commit**, and that instruction assumes a diff a person
can check.

**It is also carrying files it should not.** Twelve `gkb_*.log` and `*_report.json`
transcripts from M35's editor batches sit untracked at the repository root, alongside
`_backup/` and `_to_delete/`. And two files that *are* real work have never been committed:
`Assets/Scripts/Game/Ui/UiEventSystem.cs` and `Assets/Prefabs/Ui/MainButton.prefab`.

**Binaries are in git objects rather than LFS**, roughly 39 MB across `.png`, `.dll`, `.mp4`,
`.ttf` and `.ogg`. They do not delta-compress, so every future re-import of a paid asset pack
adds its full size to history permanently.

**This is the cheapest this will ever be.** History is one commit. Every commit from here
makes the rewrite larger and the force-push more disruptive.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** It restores a readout
that already exists and currently lies.

**Multiplier** (the tooling exception, `../../_meta/_definitions/milestone.md`): every
milestone from here is committed, moved between folders and reviewed through this repository.
M34, M39 and M41 all gate on a `git diff --stat` that must be readable.

## Preconditions

**`.gitattributes` and `.gitignore` are already written and on disk** (2026-08-10). This
milestone does not author them; it applies them. Read both before starting — the
`.gitattributes` header states why Unity YAML stays in git, and `../../Decisions.md` records
that decision so it is not re-litigated mid-run.

## Acceptance criteria

1. **Git LFS is installed and initialised**, confirmed by `git lfs version` printing a
   version and `git lfs install` succeeding. **ABORT if `git lfs version` errors.** Every
   later step depends on the filter being active before any file is staged, and an agent that
   proceeds will commit ~39 MB of binaries into git objects and then force-push them.
2. **A `backup-original` branch points at the current root commit** before anything is
   rewritten, and it still exists at the end of the milestone. It is the undo.
3. **`git diff --ignore-cr-at-eol --stat` prints nothing at all**, run before the rewrite.
   **ABORT if it prints any file.** That file has a real edit the line-ending noise was
   hiding, and rewriting history would bury it. Report the file rather than resolving it —
   deciding what to do with an unexpected edit is not this milestone's work.
4. **The new history is one root commit on an orphan branch**, created with
   `git checkout --orphan`, then **`git rm -r --cached .` before `git add -A`.**
   The `git rm` is not optional: `--orphan` keeps the existing index and `.gitignore` does
   not apply to already-tracked files, so without it every ignored file is carried through
   and criterion 5 fails.
5. **The staged set is correct in both directions**, checked from `git status --porcelain`
   before the commit:

   | Must be absent | Must be present |
   |---|---|
   | `gkb_*.log`, `*_report.json`, `bastion_ui_assemble.log` | `Assets/Scripts/Game/Ui/UiEventSystem.cs` |
   | `_backup/`, `_to_delete/`, `.cursor/cli.json` | `Assets/Prefabs/Ui/MainButton.prefab` |
   | anything under `Library/`, `Temp/`, `Logs/` | `.gitattributes`, `.gitignore` |

   **Both columns are checked.** An agent that verifies only the exclusions will happily ship
   a commit that drops two files of real M35 work.
6. **`git lfs ls-files` returns several hundred entries**, and includes
   `Assets/Shift - Complete Sci-Fi UI/Textures/Background/Background Advanced.mp4` and
   `Assets/Plugins/Sirenix/Assemblies/Sirenix.OdinInspector.Editor.dll`.
   **ABORT if it returns nothing** — that means the LFS filter was not active during
   `git add`, and the fix is `git reset` and a restart from criterion 1, not a push.
7. **`git lfs ls-files` contains no `.unity`, `.prefab`, `.asset`, `.meta` or `.mat` file.**
   Asset serialization is Force Text (`ProjectSettings/EditorSettings.asset`,
   `m_SerializationMode: 2`), so those are YAML and belong in git as text
   (`../../Decisions.md`, 2026-08-10). **`.unity` is the largest category on disk at 22.7 MB**,
   which makes it the thing an optimiser reaches for first; putting it in LFS trades a
   mergeable file for an opaque pointer.
8. **The commit message carries no attribution trailer** — no `Co-authored-by`, no
   `Made-with`, no `Generated with`. Subject and body only
   (`.cursor/rules/no-commit-attribution.mdc`, `alwaysApply: true`). Checkable:
   `git log -1 --format=%B | grep -iE "co-authored|generated with|made with"` returns nothing.
9. **`main` is the clean root commit and the remote matches it**, after
   `git branch -M` and a force-push to `origin`. **This step is gated on criteria 1, 3, 5, 6
   and 7 having passed**; it is the only irreversible action in the milestone and the only
   one that touches anything outside the working copy.
10. **`git status` on the untouched tree prints `nothing to commit, working tree clean`.**
    This is the milestone. If 141 files still show as modified, `.gitattributes` is not being
    applied and nothing above worked.
11. **`git log --oneline` prints exactly one commit on `main`**, and
    `git cat-file -p HEAD` shows no parent.
12. **The build is unaffected.** `Tools/verify/run.sh` passes with the same test count as
    before the rewrite, and Unity opens the project without reimporting assets. **Line-ending
    normalisation reaches `Tools/*/run.sh`**, which is why `.gitattributes` pins `*.sh` to
    `eol=lf` in the working tree as well as in the repository — a CRLF checkout gives
    `bad interpreter: /usr/bin/env bash^M` and the harness stops running.
13. **`_to_delete/` is emptied by hand and the folder removed.** It holds two zero-byte
    `index.lock.stale*` files moved out of `.git/` on 2026-08-10, plus whatever else has
    accumulated. **A leftover `.git/index.lock` blocks the next git command**, so this is
    checked rather than assumed.

## Implementation

The command sequence, the expected output of each step and the abort conditions are in
`../../git-normalize-runbook.md`. **It is the implementation; do not re-derive it.**

Order: verify prerequisites (1, 2, 3) → orphan branch and stage (4) → check both columns
(5) → commit (8) → verify LFS (6, 7) → push (9) → verify the result (10, 11, 12, 13).

**Change** `Docs/Roadmap.md` and `Docs/Changelog.md` in a second commit once `main` is
clean — a status transition is a file move plus a row, and this milestone's own transition is
the first one the new history will record.

**Delete** `Docs/git-normalize-runbook.md` in that same second commit. It is a one-time
procedure and it has run; the durable record is `.gitattributes`, `.gitignore` and the two
`Decisions.md` entries.

Rejected: `git lfs migrate import`. Correct for a repository with history worth preserving.
Here history is one commit named `Initial commit: Gatekeeper Bastion Unity project.` — the
migrate path does more work and has more failure modes to reach the same place.

Rejected: `core.autocrlf=true` instead of `.gitattributes`. One machine, does not travel with
the repository, returns on the next clone (`../../Decisions.md`, 2026-08-10).

Rejected: committing the line-ending normalisation first and migrating to LFS afterwards. The
migration rewrites the normalisation commit anyway, so it is one noisy commit thrown away for
nothing.

Rejected: doing this through the Cowork device bridge. Git writes fail there —
`git status` alone could not unlink its own `.git/index.lock` ("Operation not permitted").
This milestone needs a real shell on the machine that owns the repository.

## Supporting work

Developer infrastructure, so the three questions are answered for the developer.

- **Driven** — `git status`, on every command.
- **Shown** — a clean tree, and a `git diff` that shows only what changed.
- **Reads back** — `git status`, `git lfs ls-files`, `git log --oneline`.

## Verification

Each row is a command and the output that closes it. **A row whose output does not match is
a failure, not a rounding error.**

| Criterion | Command | Expected |
|---|---|---|
| 1 | `git lfs version` | a version string |
| 2 | `git branch --list backup-original` | one line, at the end of the run |
| 3 | `git diff --ignore-cr-at-eol --stat` | **empty**, before the rewrite |
| 5 | `git status --porcelain` | both columns of the table above |
| 6 | `git lfs ls-files \| wc -l` | several hundred |
| 7 | `git lfs ls-files \| grep -E "\.(unity\|prefab\|asset\|meta\|mat)$"` | **empty** |
| 8 | `git log -1 --format=%B \| grep -iE "co-authored\|generated with\|made with"` | **empty** |
| 10 | `git status` | `nothing to commit, working tree clean` |
| 11 | `git log --oneline` | exactly one line |
| 12 | `Tools/verify/run.sh` | same test count as before the rewrite |
| 13 | `ls _to_delete .git/index.lock` | both absent |

Then, and only then: **clone the repository fresh into a scratch directory and run
`git status` in the clone.** It must also print `nothing to commit, working tree clean`.
**This is the criterion that cannot be faked by local state** — `.gitattributes` working on
the machine that wrote it proves less than it appears to.

## Watch

- **The failure mode is an agent that continues past a failed check.** Criteria 1, 3, 6 and 7
  are aborts because each one, ignored, produces a force-pushed history that is worse than
  the current one. The backup branch in criterion 2 exists for exactly that case.
- **Criterion 5's "must be present" column is the one that gets skipped.** Verifying that
  junk is absent feels like the job; two files of uncommitted M35 work are what actually get
  lost.
- **Criterion 9 is irreversible and touches the remote.** Everything before it is local and
  recoverable from `backup-original`. Nothing after it is.
- **If a clone exists anywhere else, the force-push breaks it.** Solo project, one machine as
  of 2026-08-10 — stated so that it is checked rather than assumed.
- **GitHub's free LFS allowance is 1 GB of storage and 1 GB of bandwidth a month.** At ~39 MB
  this is not close, but a re-imported asset pack stores a new copy of every changed binary
  and old versions are not reclaimed.

## Depends on / Unblocks

Depends on `.gitattributes` and `.gitignore`, both written 2026-08-10 and on disk.

**Unblocks nothing formally**, and makes `git diff` usable as evidence — which M34, M39 and
M41 all assume and none of them can currently rely on.
