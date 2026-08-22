# Milestone

<!--
Purpose: Authoring contract and scaffold for milestone files under Docs/Milestones/. The prose is the rules; the code block near the bottom is what you copy to make a new one.
Status: working
Update trigger: when the milestone shape, the status folders, or the lifecycle changes.
Last updated: 2026-08-19
-->

A **milestone is an implementable unit of work** — the equivalent of a user story.
It states what changes for the player, why that matters, how to build it, and how to
prove it is done. Someone who has not been in the conversation should be able to pick
one up and execute it without designing anything.

It is not a design doc, not a task list, and not a place for numbers that live
somewhere else.

Milestones were the first recurring document class in `Docs/`. See `../../README.md`
for the rule that governs which classes earn a contract.

## The bar

**If an implementer would have to make a design decision to proceed, the milestone
is not finished.** Every choice that shapes the result — a type's shape, a number, a
file that gets deleted rather than adapted, which of two reasonable approaches — is
made *here*, in writing, with the reason. Decisions genuinely left open are labelled
as open and given an owner or a trigger, never left implicit.

The two failure modes, both of which have shipped in this project and been rejected:

- **Anemic.** A title, a gate, and a gesture at the change. Reads like a task. The
  implementer ends up designing the thing, which means the milestone decided nothing.
- **Inflated.** Analysis, history, and re-derived arithmetic padding a small change.
  Numbers belong in `../../Design/4-Data.md`; a milestone links to them.

Length is not the measure. **Density of decisions is.**

## One grain, or the roadmap means nothing

Every milestone is the same size of thing: **one player-visible change, specifiable
now, finishable without stopping to decide what it is.** A roadmap whose rows range
from a chore to an epic cannot be ordered, because the rows are not comparable.

Four kinds of thing keep getting written as milestones and are not:

| Not a milestone | Why | Where it goes |
|---|---|---|
| **An activity** — "play the loop and record outcomes" | Has no end state and changes nothing. Playing is how milestones are *verified*. | The `## Verification` section of the milestone it proves |
| **A phase** — "off gray-box", "art and audio pass" | Names a period of the project, not a unit of work. | Strategy prose in `../../Roadmap.md` |
| **An epic** — "co-op, 2–4 players" | Decomposes into many milestones; its gate can only be vague. | Brainstorm, until one slice of it can be cut |
| **A chore** — "wire the prefabs", "re-run Build All Scenes" | Real work, no player-visible change, done once. | Inside the milestone that needs it, or just do it |

The last row is the one that costs most: a chore promoted to a milestone displaces a
milestone that had a player consequence.

**Durable tooling is the one exception**, and it is narrow. Infrastructure that
lowers the cost of verifying *many* milestones — a test harness, a dev console —
qualifies, with a **developer Story** and a stated multiplier: which milestones it
makes cheaper, and by how much. A one-off script written to answer one question does
not; that is a chore, and the answer belongs in the milestone that asked.

The test that separates them: **would this still be used after the milestone that
motivated it shipped?** A log parser written to settle one balance argument would not.
`M13 — Dev mode` would, by every play-verified criterion in the table.

**A file exists only when its gate can be written.** Work that is wanted but not yet
specifiable is a Brainstorm row in the roadmap and has no file. Pre-creating files
for distant work produces exactly the vague gates this contract forbids, and they age
into a roadmap nobody trusts.

## Usable, or it did not ship

A capability the player cannot drive, or can drive but is never told about, is not a
player-visible change. M11's weapon slots are the case that proves it: `1`/`2`/`3`, `Q`
and the wheel are bound and working, and the developer who specified them could not find
them in play. M15 exists to undo that.

**Applies when the milestone gives the player something to do, own, or choose that they
could not before.** Tuning an existing number does not qualify; adding a station, a
binding, a slot, a selector, a resource or a state does. Developer tooling answers the
same three questions for the developer.

| | The question | The failure it catches |
|---|---|---|
| **Driven** | What does the player press, click, or walk into to use this? | A system with no input. |
| **Shown** | What tells them it exists, in play, without reading a document? | A binding nobody finds. |
| **Reads back** | What confirms it did something — a HUD value, a log line, a visible change? | A purchase that moves a number nobody can see. |

Each answer is either **in this milestone** or **named against the milestone that owns
it**, under `## Supporting work`. A binding belongs to the milestone that introduces the
capability, not to a follow-up, unless that follow-up is named. **Silence reads as
handled** — that is exactly how the slots shipped invisible.

Where a piece is owned elsewhere, say what happens if that milestone slips: either the
capability ships dormant on purpose (M13's `F7` does, and says so) or it does not ship.

## Where they live

`Docs/Milestones/<status>/M#-slug.md`. **The folder is the status.** There are five:

| Folder | Status | Means |
|---|---|---|
| `1. planned/` | later | Specified and started-able. **At most 10 files.** |
| `2. active/` | next | Being worked. **At most 3 files** (`../../Working-Rules.md`). |
| `3. completed/` | completed | Every criterion met. Stamped with the month. |
| `4. deferred/` | deferred | Wanted, but there is no honest next step. Names its trigger. |
| `5. cancelled/` | cancelled | Dropped with the reason logged in `../../Changelog.md`. |

The numeric prefixes sort the folders in the order work moves through them.

Naming: `M#-slug.md` — `M2-wave-3-resolution.md`. The number is a **stable ID and
never changes or gets reused**; rows in `../../Roadmap.md` are listed in execution
order, so reordering is a row move, not a renumber. The slug is kebab-case and short.

## Lifecycle

A status change is three things in **one commit**:

1. `git mv` the file to the new status folder.
2. Update its `Status:` frontmatter line (and stamp the month if shipping).
3. Update the row and `File` link in `../../Roadmap.md`, and the **Current** line
   if the active milestone changed.

A file whose folder and frontmatter disagree is a bug. So is a roadmap row whose
`File` link 404s.

## Required sections

Five, in this order. All five are mandatory; a milestone missing one is not ready to
start.

**`## Story`** — one sentence, *As a … I want … so that …*. The role is usually the
player; it can be the developer for tooling. If the "so that" clause is hard to
write, the work may not be worth doing — that is the section earning its place.

**`## Why it matters`** — what changes for whoever plays or uses this, and the
evidence it is needed: a log line with a timestamp, a number in
`../../Design/4-Data.md`, a decision in `../../Decisions.md`. **Link, never copy.**
Two short paragraphs is usually enough. This is where the leverage tier is named
when the milestone touches a parameter (`../../Working-Rules.md`).

**`## Acceptance criteria`** — numbered, each independently checkable, each phrased
so it can only be true or false. Collectively they *are* the gate; there is no
separate "Done when" line any more. "Feels better", "is balanced" and "plays well"
are not criteria. If the only honest criterion is a judgement call, the milestone
belongs in Brainstorm. **If the milestone adds a capability, at least one criterion is
phrased as a player action in a Run** — "pressing `1` during Prep changes the weapon
slot and nothing else", not "the slot handler is wired".

**`## Implementation`** — ordered steps. Name every file as **create / change /
delete**, give signatures where they are load-bearing, and state the shape of new
types. Say what is deleted rather than adapted, and why. Enough that the implementer
executes rather than designs. Where two approaches are reasonable, pick one and give
the reason in a sentence — the rejected option is worth naming so it is not
rediscovered.

**`## Verification`** — the exact commands, test names and log lines that prove each
criterion, and what a passing result looks like. "Run the tests" is not verification;
*which* tests, asserting *what*. If a criterion can only be checked by playing, say
which run, at which dial, and what to read out of the log afterwards.

Anything the player drives gets a **playthrough path**: from pressing Play, the shortest
route to exercising it, and what on screen or in the log says it worked. A change proven
only by a passing test and a source grep is proven to exist, not proven to be usable.

**A playthrough path is not a blocking close gate until a replayable slice exists for
that class of criterion** (`Decisions.md`, 2026-08-22). Name the slice in Verification
(`M99`, an F12 beat, a PlayMode that paints). Until then, close what the harness and
disk can check; leave the playthrough as confirmation after the slice ships.

## Optional sections

Used only when there is something to say, in the order they usually appear: **Supporting
work** (see *Usable, or it did not ship* — required whenever an answer to Driven / Shown
/ Reads back lives in another milestone), *Open questions* (labelled *unknown, needs play
data* — never a guess in confident prose), *Watch* / *Trap*, *Do not build*, *Depends on*
/ *Unblocks*, *Parked because*.

**Do not pad to fill the template.** An empty heading is worse than a missing one.

## Rules

- **Link, never copy.** Numbers live in `../../Design/4-Data.md`, decisions in
  `../../Decisions.md`, gotchas in `../../Architecture.md`, the loop in
  `../../Design/3-Mechanics.md`. Quote a figure only where the milestone's argument turns on it.
  When a milestone and a source file disagree, **the source file wins**; say so.
- **Every criterion is checkable or it is not a criterion.**
- **Added but undrivable is not done.** Driven, Shown, Reads back — answered here or
  assigned to a named milestone. See *Usable, or it did not ship*.
- **Name the leverage tier** before proposing a parameter change, and say why a
  higher tier is wrong.
- **Shipped files stop changing.** They are a record. Corrections only, never
  retro-editing a gate to match what happened.
- **A milestone dies with its milestone.** If the work is dropped, delete the file
  and log it in the roadmap's change log. Do not leave a tombstone.
- **No new status folders.** Five is the set.
- **The changelog cites bare ids, not paths.** A status change moves the file, so any link
  written into `../../Changelog.md` dies at the next move — 12 had rotted by 2026-08-19.
  Write `` `M78` ``; `../../Roadmap.md` is the index that resolves it. The Roadmap's own
  `File` link is the exception, and it is updated in the same commit as the move.

## Starting structure (copy this)

```markdown
# M# — Title

<!--
Status: <shipped, Mon YYYY | next (current) | later | parked>
Update trigger: <what forces a revision of this file>
Last updated: YYYY-MM-DD
-->

<One or two lines: what this is, in plain terms.>

## Story

As a <player>, I want <capability>, so that <value>.

## Why it matters

<What changes for the player, and the evidence it is needed. Link, do not copy.
Name the leverage tier if this touches a parameter.>

## Acceptance criteria

1. <Checkable. True or false, nothing in between.>
2. <...>

## Implementation

**Create** `path` — <shape, signatures>.
**Change** `path` — <what, and what it replaces>.
**Delete** `path` / `member` — <why deleted rather than adapted>.

<Ordered steps if order matters. One sentence per rejected alternative.>

## Supporting work

<Only when an answer lives elsewhere. Delete this heading if all three are above.>

- **Driven by** <input> — <here | M#, and what happens if M# slips>.
- **Shown by** <surface that tells the player it exists> — <here | M#, ...>.
- **Reads back as** <HUD value, log line, visible change> — <here | M#, ...>.

## Verification

1. <criterion> — `<command>`, expect <result>.
2. <criterion> — play at dial N; the log should show <line>.
3. Playthrough path — from Play: <shortest route>. Confirms <what the player sees>.

## Watch

<Existing gotchas that apply, by reference.>

## Depends on / Unblocks
```

## Worked examples

- **`Milestones/3. completed/M11-the-weapon-is-the-build.md`** — shipped record of a
  system change (catalog, Tiers, migration, deletions). The planned draft lived under
  `1. planned/` until close; use a live planned file as the shape to copy.
- **`Milestones/3. completed/M14-five-waves-and-a-deep-start.md`** — shipped record of a
  five-Wave curve plus a NAV MAP decision (deep start). Use a live planned file as the
  shape to copy; this is the stamp.
- **`Milestones/3. completed/M10-wave-2-is-winnable.md`** — shipped record of the same
  grain (one formula: the group gap). Use planned files as the shape to copy; this
  is the stamp.
- **`Milestones/3. completed/M0-playable-v0-loop.md`** — a shipped file. Predates this
  contract; not a shape to copy.
M1 "play the loop, record outcomes" was deleted under this contract: it is an
activity, not a unit of work. Its content became the `## Verification` sections of
M10 and M2.

## Changelog

- 2026-08-04: Added **Usable, or it did not ship** — Driven / Shown / Reads back, an
  optional `## Supporting work` section for answers owned elsewhere, a player-action
  acceptance criterion, and a playthrough path in Verification. Cause: M11 specified
  working weapon slots that nothing in the game mentions, so the developer who wrote them
  could not find them in play; M15 is the remedial milestone. The contract previously let
  a milestone ship a capability with no control and no surface and still pass its gate.
- 2026-08-04: Narrowed the "a chore or a tool is not a milestone" rule. It was too
  broad and excluded durable infrastructure that unblocks verification across the
  roadmap. Chores still do not qualify; tooling does, with a developer Story, a stated
  multiplier, and the would-it-outlive-its-motivating-milestone test.
- 2026-08-04: Status folders renumbered `1. planned` / `2. active` / `3. completed`
  / `4. deferred` so they sort in workflow order.
- 2026-08-04: Reshaped around **implementable unit of work**. `## Done when` replaced
  by numbered `## Acceptance criteria`; `## Story`, `## Implementation` and
  `## Verification` made mandatory. Added "The bar" — a milestone that leaves design
  decisions to the implementer decided nothing. Prior shape produced files that read
  as tasks: a title, a gate, and a gesture.
- 2026-08-03: Created. Rules and scaffold in one file, following the Serennovas
  `_definitions/` pattern — a definition contains the generation pattern, not just
  the metaphysical rules. Template moved here out of `../../Roadmap.md` so it
  exists in exactly one place.
