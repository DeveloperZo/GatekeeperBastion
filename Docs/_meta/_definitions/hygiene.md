# Hygiene pass

<!--
Purpose: The authoring contract for a hygiene pass — the unit that settles owed items. Sits beside _definitions/milestone.md and borrows its shape deliberately.
Status: working
Update trigger: when the pass lifecycle, the id rule, or the admission bar changes.
Last updated: 2026-08-25 (AutoVerify renamed to IntegrationCheck throughout, including the copy-this frontmatter)
-->

A **hygiene pass** is one sitting that settles a group of owed items. It is what a
milestone is to a feature: a named thing with a list, a close condition, and a folder
that says where it stands.

The ledger it replaces recorded items. That was the wrong unit. Six rows that all close
with the same IntegrationCheck run are not six things to track, they are one thing to
schedule, and a table cannot say so.

## The bar — what a pass may contain

An **item** is something real that was left undone and would otherwise be forgotten: an
acceptance criterion a closed milestone never separately observed, a one-shot artefact
still in the tree, a cleanup only the developer's hands can perform.

Three things are **not** items, and each has somewhere else to go.

- **Code that was never written.** "The string is not in Scripts" is not an unobserved
  criterion, it is an unbuilt one. That is a milestone, or a defect, or a deliberate
  retirement in an edit that says so.
- **Anything with a real gate.** If settling it changes what a player sees, needs a
  decision, or deserves its own acceptance criteria, it is a milestone.
- **A suspicion.** A row is not a claim that something is broken. It records that
  something was not observed, and nothing more.

## One sitting, or split it

**A pass is what one person can settle in one sitting.** If the list needs two evenings,
two machines, or a decision in the middle, it is two passes. This is the same grain rule
milestones carry, applied to the smaller unit, and it exists for the same reason: a pass
nobody can finish is a pass nobody starts.

The corollary is the useful one. **Group items by the observation that closes them, not
by the milestone that owed them.** Four criteria from three different milestones that all
fall out of one played run belong in one pass.

## Ids

**Item ids are `TH-n`, issued once, never reused, monotonic across open, settled and evicted.**
The next id is one past the highest that has ever existed, including settled and evicted
ones. Milestones and the changelog cite item ids, so a reused id silently re-points an old
reference at new work.

**Pass ids are `HP-n`**, same rule.

**The register is [`../../Hygiene/README.md`](../../Hygiene/README.md)**, which is to
`Hygiene/` what `Roadmap.md` is to `Milestones/`: the pass table, every id ever issued with
where it went, and the eviction record. It is the only place the high-water mark is kept, so
it outlives any pass and is updated in the same edit that issues an id.

The ledger this replaces issued `TH-9` through `TH-12` twice, once on 2026-08-18 and again
on 2026-08-19, which is what a shared table with no issuing rule produces.

## Where they live

```
Docs/Hygiene/
  1. open/       HP-n-title.md
  2. settled/    HP-n-title.md
```

Folder is status. A pass moves when every item in it is settled or evicted, and the move
is one line in `Changelog.md` like any other state transition.

## Required sections

**Frontmatter comment** — status, update trigger, last updated, and **who can close it**,
because most passes need either Zo's hands or the IntegrationCheck file bridge and that is
the scheduling constraint.

**One line on what the sitting is.** "Survive a window, dock, spend, re-embark, read the
log."

**Items** — a numbered list. Each item carries its `TH-n`, one sentence on what is owed,
where it came from, and what observation settles it. Nothing else. If an item needs a
paragraph it is probably a milestone.

**Not this pass** — anything a reader will expect to be here and is not, with where it
went. Cheap, and it stops the pass growing on contact.

## Rules

- **Authority for anything touching a MonoBehaviour, a scene or the save file is the
  IntegrationCheck file bridge** (`Assets/Scripts/Editor/Tools/IntegrationCheck.cs`,
  renamed from AutoVerify). A green offline harness does not settle those items
  (`Working-Rules.md`, Verification).
- **A pass settles by observation, not by argument.** "It must work, the test passes" does
  not close an item that says the test cannot see it.
- **An item may be evicted rather than settled**, when it turns out to be code that was
  never written or a rule that should be retired. Evicting is a recorded outcome with a
  reason, not a deletion.
- **A pass has no authority over design or code.** It records debts and what would settle
  them. It gates nothing.

## Starting structure (copy this)

```
# HP-n — Title

<!--
Status: open
Closable by: Zo at the keyboard / the IntegrationCheck file bridge / either
Update trigger: when an item is added, settled or evicted
Last updated: YYYY-MM-DD
-->

One line: what the sitting is.

## Items

1. **TH-n** — what is owed. From <milestone> criterion <n>. Settles when <observation>.

## Not this pass

- <thing a reader expects> — <where it went>
```
