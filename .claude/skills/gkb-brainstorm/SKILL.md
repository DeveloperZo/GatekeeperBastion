---
name: gkb-brainstorm
description: Move an idea from interesting to cuttable in Gatekeeper's Protocol — the entry contract for a brainstorm document, the graduation test that turns an entry into a milestone, and the checks that stop settled ground being re-proposed. Use this skill only when the work is the brainstorm document itself: capturing, parking, revisiting, or asking whether an entry is ready to graduate — including phrasings like "capture this", "add this to the brainstorm", "is X ready yet", "park this", or "I have a half-formed idea", and whenever a file under Docs/Brainstorms/ is being read or written. It does not cover evaluating, scoping, arguing or killing a proposed mechanic, resource, enemy or economy change; that is gkb-design from the first proposal, even when the idea is new and unwritten. For writing the milestone once an entry graduates, use gkb-strategy.
metadata:
  project: gatekeepers-protocol
  update-trigger: when the brainstorm contract or the graduation test changes
---

# Brainstorm

## The entry contract

An idea is not an entry until it has three things. Without them it is a note, and notes
accumulate.

1. **Loop or arc.** A loop generates engagement from a fixed authoring cost; an arc is consumed
   once and puts you on a content treadmill. An arc has to justify itself against onboarding or
   payoff specifically. Use `gkb-design` if the classification is not obvious.
2. **The observation that would kill it.** Not a risk — an observation, something that could be
   seen in a Run or read out of a session log. If nothing could disprove it, it cannot be built
   cheaply and it cannot be finished.
3. **Which existing system it touches.** An idea that touches nothing is a second game.

## Before writing anything

**Read `Docs/Decisions.md` first.** Cut features are recorded there by name, with the reason,
precisely so they are not re-proposed. Re-proposing one is legitimate only with a stated reason
the original verdict was wrong or has expired.

**Check `Docs/Brainstorms/` for an existing file.** One file per mechanic area, and a new file
needs its own graduation test.

## The graduation test

**An entry graduates when its acceptance criteria can be written** — criteria that can only be
true or false, checkable from the session log, the test harness, or a generated number.

Not when it feels ready, and not when it is well described. The test is whether someone could
write the gate.

**On graduation the milestone file is created and the entry is deleted.** Never both. An entry
that survives its own milestone becomes a second, drifting description of the same thing.
When a file's last Idea is gone and Open is empty, move it to `Docs/Brainstorms/closed/`
if a roster or Decided table is still the record; delete only when nothing remains worth
keeping.

## What stays an entry

- It needs play data nobody has yet — say which Run would produce it.
- It depends on something unbuilt — name the milestone.
- It cannot be cut small. An idea that only works as a whole subsystem is a different problem
  than a milestone, and the honest move is to write that down rather than slice it badly.

## Keeping the surface small

The queue caps exist because unbuilt planning is the easiest thing in the project to work on
instead of the project. A prior project's own postmortem named its documentation as productive
procrastination — more pages describing the game than minutes of gameplay.

**If a session ends with more entries than it started with and nothing was built, that is the
signal.**

## Writing the entry

What it is, loop or arc, what would kill it, what it touches, and what has to be true before it
can graduate. Anything longer is a milestone that has not admitted it yet.

State it in the game's own vocabulary rather than builder terms. The language check runs over
player-facing documents, and the habit should start here.
