# One-time: normalise line endings and move binaries to LFS

Run these yourself in a Windows terminal at `F:\Programming\Gatekeeper Bastion`.
**I cannot run them through the device bridge** — git write operations fail there with
`unable to unlink .git/index.lock: Operation not permitted`, and a half-finished history
rewrite is the worst possible outcome of a permissions error.

**Both files are already on disk**: `.gitattributes` (new) and `.gitignore` (replaced).

## Why a fresh root commit rather than `git lfs migrate`

History is **one commit**, and it is `Initial commit: Gatekeeper Bastion Unity project.`
There is nothing in it worth preserving through a rewrite. `git lfs migrate import` on a
one-commit repo does more work, has more ways to go wrong, and lands in the same place.
**Starting the history over is the simpler operation and the same result.**

This is also the last moment it is cheap. Every commit from here makes the rewrite bigger
and the force-push more disruptive.

## What is going to LFS, and what is deliberately not

`ProjectSettings/EditorSettings.asset` has `m_SerializationMode: 2` — Force Text. **Every
Unity asset type in this project is YAML**, so `.unity`, `.prefab`, `.asset`, `.mat`,
`.anim`, `.controller` and `.meta` stay in git as text.

That matters more than it sounds: **`.unity` is the largest category on disk at 22.7 MB**,
nearly all of it Shift's six demo scenes, and the instinct is to LFS the biggest thing.
Doing that would trade a diffable, mergeable file for an opaque pointer.

LFS takes the true binaries — roughly 39 MB across `.png` (21.9), `.dll` (6.8), `.mp4` (6.7),
`.ttf` (2.1) and `.ogg` (1.2).

## The sequence

```bat
cd /d "F:\Programming\Gatekeeper Bastion"

REM 0 — safety net and prerequisites
git branch backup-original
git lfs version
git lfs install
```

**`git lfs version` must print a version.** It is not installed in the environment I can
reach, which says nothing about your Windows Git — Git for Windows ships it as an optional
component. If it errors, install Git LFS first and re-run; everything below depends on the
filter being active before any file is staged.

`backup-original` keeps the current commit reachable locally after the force-push. Delete it
once you are satisfied.

```bat
REM 1 — confirm the 141 modifications really are only line endings
git diff --ignore-cr-at-eol --stat
```

**This must print nothing at all.** If it prints a file, that file has a real edit in it that
the line-ending noise was hiding, and it needs looking at before anything is rewritten.
(Measured 2026-08-10: it printed nothing.)

```bat
REM 2 — fresh root commit, with LFS filters and .gitattributes both in place
git checkout --orphan clean-root
git rm -r --cached . --quiet
git add -A
```

**`git rm -r --cached .` is not optional.** `--orphan` keeps the existing index, and
`.gitignore` does not apply to files that are already tracked. Without it, the twelve
`gkb_*.log` files, `_backup/` and `_to_delete/` come along.

```bat
REM 3 — look before you commit
git status --porcelain > staged.txt
```

Open `staged.txt` and check three things: no `gkb_*.log` or `*_report.json`, no `_backup/`
or `_to_delete/`, and `Assets/Scripts/Game/Ui/UiEventSystem.cs` plus
`Assets/Prefabs/Ui/MainButton.prefab` **are** there — those two are real M35 work that has
never been committed. Then delete `staged.txt`.

```bat
REM 4 — commit
git commit -m "Gatekeeper Bastion - Unity 6 wave-defense FPS" -m "Normalise line endings through .gitattributes and move binary assets to Git LFS. Replaces the previous root commit, which stored 141 files with platform-dependent line endings and ~39 MB of binaries as git objects." -m "Unity asset serialization is Force Text, so .unity, .prefab, .asset and .meta stay in git as YAML."
```

No attribution trailer, per `.cursor/rules/no-commit-attribution.mdc`.

```bat
REM 5 — prove LFS actually engaged, before pushing
git lfs ls-files
```

**Expect several hundred lines.** If it prints nothing, `git lfs install` did not take and
the binaries went into git objects again — reset to `backup-original` and start over rather
than pushing.

```bat
REM 6 — replace main
git branch -M clean-root main
git push --force origin main
```

`git push` carries the LFS objects automatically. If it reports a pre-push hook problem, run
`git lfs push --all origin main` and push again.

```bat
REM 7 — the check that matters
git status
```

**Expect `nothing to commit, working tree clean`.** That is the whole point of the exercise;
if 141 files still show as modified, `.gitattributes` is not being applied and nothing else
in this runbook worked either.

## After

- Delete `backup-original` once `git status` is clean and GitHub shows the new commit.
- `_to_delete/` holds two zero-byte `index.lock.stale*` files I moved out of `.git/` — my
  read-only `git status` calls created them and the bridge cannot unlink. **Delete that
  folder by hand**; a stale `index.lock` blocks your next git command.
- **GitHub's free LFS allowance is 1 GB of storage and 1 GB of bandwidth a month.** At ~39 MB
  this is not close, but every re-import of a paid asset pack stores a *new* copy of every
  changed binary and old versions are not reclaimed. Worth remembering the next time a Shift
  or Odin update lands.
