---
name: gkb-verification
description: How Gatekeeper Bastion proves a change works — the verification paths and what each can see, what may close a gate, the bar for adding a test, script or menu row, where generated output goes, and testing determinism. Use this skill whenever a change is about to be called done, verified, working or fixed, whenever the question is whether the current build behaves as it should (a fix holds, a wave is beatable, something broke), and whenever a test, check, tool or menu item is being added. Use it before writing "this works" in any form. It does not cover executing or implementing a milestone's acceptance criteria — that is gkb-development. For whether a change should exist or how to redesign it, use gkb-design. For diagnosing why something is broken, or writing the fix, use gkb-development instead.
paths: Assets/Scripts/**/*.cs, Assets/Tests/**/*.cs, Tools/**, Assets/Scripts/Editor/**
metadata:
  project: gatekeeper-bastion
  update-trigger: when a verification path is added or retired, or when a check moves into the verify run
---

# Verification

## Never

- **Report as verified what you did not observe.** "You should see X" is a prediction. Say what
  changed and what to look for.
- **Report work verified on a suite you did not run.**
- **Close a gate with a tainted log.** A session log carrying dev-command markers cannot support
  a claim. Neither can a stale one: before a log speaks for current code, the
  `Library/ScriptAssemblies` dll must be newer than the edited source and the log's literal
  strings must still exist in that source. A handoff once cited a FAIL log written by a dll
  two edits old.
- **Cite a path that will not exist next session.** Throwaway scripts live outside the repository
  and are never named in a document. Three milestone gates once pointed at a deleted harness
  path.
- **Add a verification path without naming the one it replaces.**
- **Add a menu row without asking first.** Naming the row it replaces is the argument you bring
  to the ask, not a substitute for it. A menu entry outlives the reason it was added.
- **Add a second menu root.** There is one, `GatekeeperBastion/`, and its members are
  deliberate. `Gatekeeper/HUD/` appeared from two one-shot migration tools
  (`Editor/Tools/PullBastionHudToShip.cs`, `Editor/Tools/RestoreShiftArmorHealth.cs`, ~220 lines
  each) that should not have been in the tracked tree at all — the sprawl arrives as a menu, but
  it starts as a script nobody deleted.
- **Leave generated output where it lands.** Ignoring is not cleaning.
- **Write a criterion an unchanged build would pass.** A negative grep is not a gate — that
  exact mistake shipped a UI milestone that satisfied every criterion and failed on sight.
  Hierarchy inspection is not proof either; the Game view must paint.

## Always

- **Run the offline harness first.** Seconds, and the fastest signal on any sim or tuning change.
- **Read the skipped lines.** A file the harness could not compile is coverage you did not get.
- **State the limits of the check you ran**, including its false negatives.
- **Prove a check twice**: it reproduces a known state on the real tree, *and* it catches a
  seeded violation. Seeding alone is not proof — a gutted check seeds and catches fine.

---

## The paths, in increasing cost

1. **`Tools/verify/run.sh`** — compiles `Assets/Scripts/Sim/`, `Assets/Scripts/Util/Log.cs` and
   the EditMode tests outside Unity with `mcs`, and runs them in seconds. Use it on every sim or
   tuning change before claiming anything. `--filter <substring>` narrows, `--verbose` shows
   passing names and game log lines. **It cannot see MonoBehaviours, scenes or the save file.**
2. **`Tools/wave-report/run.sh`** — offline reader over session logs, and the authority on what
   happened in a Run. `Docs/dashboard.html` shows the same numbers in a page.
3. **The AutoVerify file bridge** — write `gkb_verify_request.txt` at the project root; with the
   editor open, `Assets/Scripts/Editor/Tools/AutoVerify.cs` runs the full EditMode then PlayMode
   suites and writes `gkb_verify_report.json`. It survives the domain reloads PlayMode causes,
   and is also a menu row. **The only way to run PlayMode, and the only authority on anything
   touching a MonoBehaviour, a scene or the save file.**

Compile errors are written to `Library/Bee/CachedNodeOutput` — read them there rather than
asking the editor.

**Confirm the recompile before writing a sentinel.** An unfocused editor consumes
`gkb_*_request.txt` files without recompiling, so the run is whatever dll is loaded — check
the `Library/ScriptAssemblies` mtime against the edited source first. And a tool that reports
through a log file must reach its log write on every exit, exception included; sentinel
consumed with no log means an exception escaped before the write, not that nothing happened.
One strip tool burned several runs exactly this way.

**If you changed only comments, the harness result must be identical.** If it is not, you
changed code. Revert.

## What may close a gate

A criterion closes when it is checkable from the session log, the test harness, or a generated
number. "Feels better" does not close a milestone, and neither does a criterion that can be
satisfied without doing the work.

**A criterion may gate on reading the editor's output. It may not require driving the editor** —
that puts a domain reload in the loop and turns authoring into code generation.

## The bar for new tooling

Three kinds, and the bar rises:

- **Throwaway** — lives outside the repository, never named in a document, never committed.
- **Committed tool** — earns it by serving more than one session and more than one question.
- **Menu row** — highest bar, and **it requires permission before it is written**. Name the row
  it replaces when you ask.

**A one-shot migration is not a committed asset.** Delete-after-verify has failed twice here,
because cleanup inside a script only runs when the script succeeds, and the script that fails
halfway is the one that stays. The replacement is a gitignored scratch folder whose emptiness is
checked from outside the script.

## Generated output

One destination, and a cleanup step. Agent transcripts and report files at the repository root
are evidence for one session, not project history — the session log is the record. They are
gitignored, which keeps them out of history and does not take them off disk.

## Determinism

**Tested, not declared.** A recorded input stream replayed in an EditMode test against a state
hash.

Same-build only. That is a regression check, not a cross-platform guarantee — deterministic
replay does not survive debug versus release, let alone different hardware. Do not claim more,
and do not pursue bitwise cross-platform determinism; nothing here needs it.

## Checks read their rules from the document

A check that hardcodes the rule becomes a second source of truth and outranks the document.
Every check here parses the rule out of the file that states it, so changing the document
changes the check with no script edit.

**A check must exit non-zero when a rule block is missing**, rather than falling back to a
default — a default turns a deleted rule into a passing run.

**And a check that has to be remembered is not enforcement.** If it can run inside
`Tools/verify/run.sh`, it does.

## Scripts, not procedures

If the same steps are being reconstructed each session, write the script once. A script's output
is what you pay for; a procedure is paid for every time it is re-derived, and it drifts.
