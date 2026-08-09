# Brainstorm

<!--
Purpose: Authoring contract for files under Docs/Brainstorms/. A brainstorm is a mechanic area that is wanted but not yet specifiable; this file says what one contains and how it graduates into a milestone.
Status: working
Update trigger: when the brainstorm shape or the graduation rule changes.
Last updated: 2026-08-06
-->

A **brainstorm** is a mechanic area the project intends to develop but cannot yet cut
into work. It holds the thinking so it is not re-invented, and it names the one thing
that has to become true before a milestone can be written.

It is the holding pen for everything that fails the milestone bar
(`milestone.md`): no checkable acceptance criteria yet.

## How a brainstorm gets written

**Zo's input arrives as a stream of consciousness and a laundry list. That is the correct
input format and it is not going to change.** The file is not a transcript of it. Turning
one into the other is the work, and it has three parts, in this order:

1. **Grill it.** Before anything is written down, challenge it against the code, the
   numbers, and what is already in this file. Say which part contradicts something, which
   part is already true in the build, which part cannot be measured. An idea that survives
   being argued with is worth recording; one that was merely typed is not. Recording an idea
   uncritically and discovering the contradiction three milestones later is the failure this
   step exists to prevent — it has happened.
2. **Distill it.** Ten sentences of thinking become one entry. Cut every restatement, every
   number that lives in `../../Design/4-Data.md`, and every clause that survives only because it
   sounded good. If an entry is longer than a paragraph it is a milestone that has not
   admitted it yet.
3. **Structure it.** Put it in the one place it belongs, below. A fact goes in *Decided*, a
   question goes in *Open*, a weapon goes in the roster table. Nothing is recorded in two
   places.

**One canonical list per thing.** The most expensive failure so far was a file with two
overlapping rosters under different names, so a decision landed in one and not the other and
the file contradicted itself for a week. **If something has a state — picked, rejected,
shipped — that state is a column in one table, never a mark scattered through prose.**

**Decisions carry a date and an owner.** "Decided 2026-08-06 (Zo)" is auditable; an
unattributed assertion is indistinguishable from something an agent invented.

## Where they live

`Docs/Brainstorms/<area>.md`. **No status folders** — every file here has the same
status, which is *not yet a milestone*. One file per mechanic area, not per idea; ideas
are entries inside the file.

Areas are durable and few. `armory-and-weapons.md`, not `fourth-primary.md`.

**`Master-Brainstorm.md` is the one exception and there is only ever one of it.** It is the
pinned master-control chat's working surface: raw observations from a play session, grilled
and held until each leaves for a milestone, a topic brainstorm, *already true*, or
rejection. It carries no *What this covers* and no *Graduation test* — it is unbounded by
design and each entry names its own exit — and its content is deleted as it cycles rather
than accumulating. Every other rule in this file applies to it, the grill step above most
of all. Added 2026-08-08 (Zo).

## Required sections

**`## What this covers`** — the boundary. One or two sentences, so a new idea has an
obvious home or obviously does not belong here.

**`## Where it stands`** — what is built today, with file references. A brainstorm that
does not say what already exists produces milestones that rebuild it.

**`## Graduation test`** — the single sentence naming what must be known or decided
before any milestone can be written from this file.

**It is a default, not a gate on the owner.** The test exists so an entry nobody has
decided about does not drift into a milestone on enthusiasm alone. **Zo can graduate any
entry by saying so**, and that decision satisfies the test. What does not relax is the
milestone bar: the resulting file still needs numbered, checkable acceptance criteria with
every design decision made in writing (`milestone.md`). Graduation by decision ends the
argument about *whether*; it does not license a vague milestone.

**`## Ideas`** — entries. Each is a heading and a short paragraph. An entry may carry
`*unknown, needs play data*`. Entries are not ordered by priority and carry no estimates.

## Optional

*Rejected* — things considered and dropped, with the reason. Cheaper than rediscovering
them. *Constraints* — rules any milestone from this area must respect.

## Graduating

An entry graduates when its acceptance criteria can be written, **or when Zo says it
graduates**. Either way:

1. Write the milestone in `Docs/Milestones/1. planned/`.
2. **Delete the idea's entry** from the brainstorm. The milestone is now the record.
3. Add the row to `Roadmap.md` and log the graduation in its change log.

The brainstorm file survives; only the graduated entry leaves. When a file's last entry
graduates and no new ones exist, delete the file.

**Do not graduate on enthusiasm** — your own. The test is whether someone else could build
it from the milestone without asking a question. If Zo calls the graduation and a criterion
still cannot be written, name the missing decision and ask for it; do not write a vague
milestone, and do not refuse the graduation.

## Rules

- **No estimates, no dates, no priority ordering.** A brainstorm is not a backlog.
- **Link, never copy.** Numbers live in `../../Design/4-Data.md`, decisions in
  `../../Decisions.md`.
- An idea that contradicts `Decisions.md` is a cut feature; say so in the entry or
  leave it out.
- Keep entries short. A brainstorm entry longer than a paragraph is a milestone that
  has not admitted it yet.

## Starting structure (copy this)

```markdown
# <Area>

<!--
Purpose: <what this area covers>
Status: brainstorm
Update trigger: <what forces a revision>
Last updated: YYYY-MM-DD
-->

## What this covers

## Where it stands

## Graduation test

<One sentence: what must be known or decided first.>

## Ideas

### <Idea>
<A paragraph. Label unknowns.>
```

## Changelog

- 2026-08-06: Graduation clarified — the graduation test is a default for undecided
  entries, not a gate on the owner. Zo graduates by decision; the milestone bar is unchanged.
- 2026-08-04: Created. The roadmap's inline Brainstorm section had grown past what a
  summary file should hold, and demoted milestones (M3 FABRICATION, M4 skills, M5 loot,
  M6 co-op, M7 outfitting, M8 destinations) had nowhere to keep their content.
