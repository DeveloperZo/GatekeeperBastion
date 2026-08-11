---
name: gkb-strategy
description: Decide what Gatekeeper Bastion works on next and in what order — lane balance and queue caps, the leverage ladder that gates tuning, the milestone authoring contract, and how to report a recommendation. Use this skill whenever the user asks what to work on, what is next, what the priority is, whether something is worth doing now, how to sequence work, or asks to reorder the roadmap or to write, scope or re-cut a milestone — and whenever planning looks like it is running ahead of building. Use it even when the question sounds casual, such as "what should I do next", because ordering here is governed by lane balance and a leverage ladder rather than by preference. For judging whether a mechanic is a good idea, use gkb-design; for capturing an idea, use gkb-brainstorm.
metadata:
  project: gatekeeper-bastion
  update-trigger: when a lane rule, queue cap or the milestone contract changes
---

# Strategy

## Never

- **Give a point estimate or project a date forward.** Dates are stamped on completion only. *A
  kill criterion's date is the one deliberate exception — see `gkb-design`.*
- **Start a milestone with a vague gate.** A vague gate means it is not ready.
- **Write a criterion that requires driving the Unity editor.** Reading its output is fine.
- **Soften a gate silently.** If a gate is wrong, change it in an edit that says so, and log it.
- **Restate a countable fact in prose.** Countable is generated; arguable is written. Three
  documents once described three different games and none was the one in the repository — each
  had been correct when written.
- **Put a count inside a rule.** A count-based rule passes forever by renaming. Name the things,
  or state the rule as "adding one means naming the one it replaces".
- **Propose something on the cut list** in `Docs/Decisions.md` without saying why the verdict has
  expired.

## Always

- **Lead with what needs deciding** — the question, one line of context, a recommendation. Then
  the answer.
- **Carry a recommendation.** "Your call" with no position is work handed back rather than done.
- **State the claim, the evidence, and what would disprove it.**
- **Cite the shipped tool for any number**, not an ad-hoc parse. A one-off script that disagrees
  with the shipped reader is wrong until proven otherwise.
- **Check lane balance before adding to the queue.**

---

## The lanes

Design, build, play. **A lane may not run ahead of the lane feeding it**, and the queue caps
exist so unbuilt planning cannot accumulate.

The failure this prevents has happened before: a prior project ended with far more pages
describing the game than minutes of playable game, and its own postmortem named the
documentation as productive procrastination.

**If a session spends longer in the docs than in the code, that is the signal.** Not a rule to
argue with — a measurement to act on.

## Order is the only scheduling signal

A prior roadmap tracked estimate points against a velocity, and its postmortem named that
bookkeeping as procrastination too.

The milestone number is a stable ID, not a position. Rows sit in execution order; reordering
moves a row and never renumbers it. Status is the folder, so a status change is a file move plus
the table row, in one commit.

## The leverage ladder gates tuning

Before changing a number, ask which tier the change sits at. Numbers are the weakest place to
intervene and the first place everyone reaches.

Weakest to strongest: constants, buffer sizes, stock-and-flow structure, delays, balancing
loops, reinforcing loops, information flows, rules, self-organisation, goals, paradigm.

**A change at the level of information flows usually beats three passes of retuning**, because
retuning against arithmetic alone moves the symptom and leaves the structure.

## The milestone contract

One player-visible change, cut small enough that its criteria can only be true or false.

- **The gate is the numbered acceptance criteria.** What may close one is `gkb-verification`.
- **Say what is rejected, by name**, so it is not re-proposed inside the same work.
- **State the case where a rule does not apply.** A rule with no counter-case has not been
  thought about.
- **Carry kill criteria where the milestone is a bet** — see `gkb-design`.
- **Infrastructure needs a stated multiplier.** It is admitted only under an exception: it must
  make the rest cheaper to verify, and the argument names what it multiplies. A tool serving one
  question is throwaway, not a milestone.

Work that is wanted but cannot be cut that way has no milestone file and lives as a brainstorm
entry.

## Choosing a dependency

**When an agent writes the code, weight documentation, version stability and the existence of a
readable reference implementation above the feature list.** A library the model gets right on
the first attempt beats a better one it gets subtly wrong.

## Nothing here overrides the cut list

If a roadmap item smells like a cut feature, it is one.
