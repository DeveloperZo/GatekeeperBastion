# Play mechanic

<!--
Purpose: Definition of a play mechanic, and the authoring contract for entries under Docs/Play Mechanics/ — what an entry is, how one is added, and its format. The format is derived from the definition, not inherited from the previous Compendium entry shape. The code blocks near the bottom are what you copy.
Status: working
Update trigger: when the definition, the entry bar, the mechanic/instance split, or the entry format changes.
Last updated: 2026-08-31 (house format: Details + Additional Details; Where in the header comment)
-->

## What a play mechanic is

**A play mechanic is a named way an agent — a Boarder seat or the garrison —
interacts with the state of a Boarding or the realm between Boardings, under the
rules, with consequences the player must account for.**

Derived from three sources that agree:

- **Consequence is the identifier** (Zo, 2026-08-29, after Brazie). Mechanics are how
  the interlocking pieces of a game — rules, challenges, goals, actions, strategies,
  game states — interact *meaningfully*. The same concept is a mechanic in one game
  and fluff in another: a flaming arrow that burns vines off a path is a mechanic; a
  flaming arrow that is a normal arrow wearing fire VFX is fluff.
- **A mechanic is invoked by an agent against the game state** (Sicart 2008). The
  agent is not only the player: provoke-on-damage and each kind's targeting refusals
  are mechanics by this test, which is why Gatekeepers is a mechanic and not just
  content. In this game most mechanics are Boarder-invoked by design — the player
  sets the pace and the stake, and the clock never pays.
- **Rules and mechanics are different things** (Sicart): a rule constrains the
  possibility space; a mechanic is the interaction inside it. This is already the
  project's structure — `Design/3-Mechanics.md` §3.1 is the rules; an entry is the
  interaction those rules shape. A rule row is never an entry; an entry never
  restates a rule.

**The falsifiable form of the consequence test is removal.** Take the thing out: if
a decision the player makes, or an outcome the run records, changes — it is a
mechanic. If nothing changes but the picture, it is fluff. "The player must account
for it" is not a feeling; it is this test.

**State is not a mechanic.** Salvage, HP, the Wave count are stocks; the mechanics
are the interactions that move them — kills paying, withdrawing, repairing. A family
of interactions on one stock is one entry when the player treats it as one thing:
the Hatch bank is one mechanic, not four.

**Core gameplay** is the essential activity performed again and again (Salen &
Zimmerman). Here: embark, Prep, ante up, Assault, Convert, return. Every mechanic
serves that loop or the realm half that feeds it.

Fluff gets no entry and is not a defect — feedback and legibility work is `Design/`
material, it is just not a mechanic. Fluff that quietly grows consequences has
become a mechanic and gets an entry that says so.

## Mechanic first, then instances

Entries describe **the mechanic, then its instances**. Gatekeepers is the mechanic;
the kinds are instances. The mechanic page is written first and owns everything
shared: the interaction, its consequences, the rules guarding it, and the axes on
which instances are allowed to differ. **An instance page owns only its
differences** and never restates the mechanic. The test: a sentence that would be
true on every sibling instance page belongs on the mechanic page.

**The instance bar: at least one behavioural difference** — something this one
refuses or does that its siblings do not. A variant that differs only in numbers is
a Field Manual row, not an instance page. This is what keeps the kinds honest (each
defined by what it refuses) and keeps the roster from sprawling into a stat sheet.

A mechanic with one shape (the Ante, the Hatch bank) is just a mechanic page.
Instances are added when real variants exist, never scaffolded in advance.

## The entry

One entry per mechanic, one per instance, under `Docs/Play Mechanics/`. An entry is
the **current representation of the mechanic as it is in the build** — precise,
auditable, updated in the same change as any milestone that touches it.

**The format is derived from the definition; the previous Compendium shape does not
dictate it** (Zo, 2026-08-29).

**Every entry opens with a summary, and it is the most important thing on the page**
(Zo, 2026-08-29). Detailed reading is encouraged; it is never required to understand
what a mechanic is. The summary is two to four sentences of plain language, sitting
directly under the title, before any heading and before the `## Game` block: what the
mechanic is, what the player does, what it costs or pays, and — where the mechanic is
not wholly shipped — one clause saying so. No code identifiers, no file paths, no
section references, no links, no jargon. Someone who reads only the summary
understands the basis of the mechanic; someone who reads on gets the precision.

An entry whose summary cannot be written in four sentences is either two mechanics or
not yet understood. **A summary is not an abstract of the sections below it** — it is
the mechanic stated once, plainly, and the rest of the page is the evidence.

An instance page's summary is one or two sentences: what this one does differently,
in the same plain language.

The visible page is the same four beats on every entry. Subsections under Details
are unique to the mechanic class; the beats are not.

| Beat | Holds | Cannot be written means |
|---|---|---|
| Summary | the mechanic in four sentences | two mechanics, or not yet understood |
| `## Game` | the Boarder's notes | voice work still open; new entries may omit |
| `## Details` | how it is structured and what the rules are | you do not understand the mechanic yet |
| `## Additional Details` | Gaps, and on mechanic pages the consequence / class / coupling / roster | a finding, not a blank |

**`## Details`** — who invokes it, doing what, on what state, when it is available,
organised as the class's components. Always opens with **Structure** (the parts,
named) and closes with **Rules** (what is allowed, refused, and invariant). Between
those, the headings are that class's — the same headings on every sibling. Driven
by what input, shown where in play, and reads back as what: fold those three into
Rules, not a trailing dump. No code paths, no file names.

**`## Additional Details`** — everything that is true and not the sheet:

- **Consequences** (mechanic pages) — what the player must account for, and what
  the removal test says. This is the entry's reason to exist. Cannot be written
  means it fails the removal test — fluff, no entry.
- **Classification** — one line. **Core**: removing it makes this a different
  game. **Primary**: readily available and directly solves the challenge.
  **Secondary**: works occasionally or in combination. Then `<loop | arc>`, and
  the aesthetics served (`Design/1-Experience.md` §1.2).
- **Interacts with** — the other mechanics this one meaningfully touches, each
  coupling named in plain words, linked. Two entries never both own the same
  coupling — the mechanic that spends writes it; the other links back.
- **Instances** — mechanic page only, and only when the class has them. Axes
  instances may vary on; things no instance may vary (those are rules). Then the
  one canonical roster table — name, what it differs in, state (`in build` /
  `Coming Soon` / `parked` / `cut` with the Decisions link), link to its page.
  **Parked** means the mechanic stays in the tree behind a flag, deliberately off
  for players; cite `Decisions.md` and the flag in the header `Where:`. Ante
  (`Ante.md`, `Ante.Enabled`) is the exemplar.
- **Gaps** — the build's legibility debt on this mechanic.

**`Where:` in the opening HTML comment** — the code map: one row per load-bearing
claim, naming the file (and member where it matters). A claim that cannot name its
code is wrong or not built. The reader strips the comment. Do not restamp it as a
visible `## Where`.

An instance page's first line names and links its mechanic page. Its Details
headings are the class's, filled with what this one refuses, pays, or changes —
nothing shared restated as if it were unique. Coming Soon instances keep the same
headings and write "Not built" (or the one intended identity clause) rather than
inventing play.

**Do not use `## Differs`, `## The interaction`, or a visible `## Where`.** Those
are the previous shape. Protocol pages are the exemplar.

## The player-facing dimension on standardized entries

Existing entries carry an authored `## Game` block — the Boarder's working notes,
voice-governed by `Story/Voices.md` — and the shipped reader consumes it. **A
standardized entry keeps its `## Game` block, unchanged, above the builder
sections**, and may keep `## Gaps` (the build's legibility debt on this mechanic)
below them. The old `## Reference` and `## See also` do not survive as sections —
their material is absorbed into Details, Additional Details, and the header
`Where:`. Voice rules govern `## Game` only; Details is the structure of play,
not a code dump. Where the player-facing surface ultimately lives, and what
the reader parses, stays an open ruling; until it lands, `## Game` is preserved so
nothing shipped breaks and nothing authored is lost.

## Where this sits

Brainstorm (or a freestyle session) → milestone (or a freestyle session) →
implementation → **the mechanic entry, written or updated in the same change**.

Three amendments to that line, each of which this project has already paid for:

- **Same change, not after closure.** A milestone that ships a mechanic updates its
  entry in the commit that ships it, exactly as retuning a Sim constant runs docgen in
  the same change. A repair, a retune or a rule correction that never gets a milestone
  updates the entry too. A shipped milestone whose entry still describes the old
  behaviour is a bug in the close.
- **The entry is an input as well as an output.** It is what the next brainstorm entry
  and the next milestone are written against, so current state is read rather than
  re-derived from code every session.
- **An entry can be born without a milestone.** A mechanic already in the build with no
  entry is found by sweeping the code, not by reading the doc list; `Regen.md` and
  `Pausing.md` both arrived that way on 2026-08-29. A mechanic designed and not yet
  built may hold a Coming Soon entry before any implementation exists.

**These are guardrails, not perfect rules** (Zo, 2026-08-29). Two consequences worth
stating, because a later session will otherwise harden them into gates:

- **A targeted code change that moves no mechanic is overhead.** That is a label, not
  a veto — a build fix, a save format, a test harness or a dead-code removal is real
  work that happens to be overhead, and knowing which kind of work is in flight is the
  point. The check is one question: which entry does this change, and if the honest
  answer is none, say so rather than inventing one.
- **Drift is expected and tolerated.** A freestyle session will move the build without
  touching an entry. That is accepted; it is caught by auditing the code against the
  entries from time to time, the way this set was built. What is not tolerated is an
  entry that has been read, believed and acted on while being wrong — which is why the
  audit direction is code → entry, and why a claim without a code reference does not
  belong in a builder section.

## Adding an entry

1. **Run the consequence test first.** Draft `## Consequences`; if removal would
   change nothing, stop — no entry.
2. **Mechanic before instance.** An instance page without a mechanic page is not
   addable; write the mechanic page first, even if thin.
3. Classify before any pricing or tuning argument is written into the entry.
4. Verify every header `Where:` row against the code at write time — audit direction
   is code → entry, and when they disagree the code is what is true.
5. The entry lands in the same change as the milestone that ships the mechanic; a
   designed mechanic without a build is Coming Soon in the title, same marker rules
   as before.

## Formatting rules

- House header comment on every file: Purpose, Status, Update trigger, Last
  updated, and `Where:` (one row per load-bearing claim). The reader strips it.
- Terms from `language.md`, nothing coined.
- **Link, never copy**: numbers to the Field Manual (generated), decisions to
  `Decisions.md`, rules to `Design/3-Mechanics.md` §3.1.
- Decisions carry a date and an owner.
- The Instances roster is the one canonical list of that mechanic's instances,
  anywhere in Docs.
- File naming keeps the existing pattern: mechanic page named for the mechanic
  (`Gatekeepers.md`), instance pages `<Mechanic-singular>-<Instance>.md`
  (`Gatekeeper-Hunter.md`).

## Starting structure — mechanic page (copy this)

```markdown
# <Mechanic>

<!--
Purpose: Current representation of <mechanic> as it is in the build.
Status: <working | Coming Soon | parked>
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: YYYY-MM-DD
Where:
- <claim> — `path` (member)
-->

<Two to four sentences of plain language: what this is, what the player does, what it
costs or pays, and whether it is wholly in the build. No code, no links, no jargon.>

## Game

<Preserved Boarder-voice block on standardized entries; new entries may omit until
the player-facing ruling lands.>

## Details

### Structure
<The parts of this mechanic, named. The instances only fill them.>

### Rules
<What is allowed, refused, and invariant. Driven by / shown by / reads back as, folded in.>

## Additional Details

### Consequences

<What changes that the player must account for. What removal would change.>

### Classification

<core | primary | secondary>, <loop | arc>. Serves <aesthetics> (`../Design/1-Experience.md` §1.2).

### Interacts with

- [<Mechanic>](<file>.md) — <the coupling, plain words>.

### Instances

Instances vary on <axes>. No instance may vary <invariants — link the rules>.

| Instance | Differs in | State | Page |
|---|---|---|---|

### Gaps

<Legibility debt on this mechanic.>
```

Headings between Structure and Rules are the class's — copy them from a sibling,
do not invent a new set for one page.

## Starting structure — instance page (copy this)

```markdown
# <Instance>

<!--
Purpose: <Instance>, an instance of [<Mechanic>](<Mechanic>.md).
Status: <working | Coming Soon | parked>
Update trigger: any milestone that touches this instance, in the same change.
Last updated: YYYY-MM-DD
Where:
- <claim> — `path` (member)
-->

Instance of [<Mechanic>](<Mechanic>.md).

<One or two sentences: what this one does differently, in plain language.>

## Game

<Preserved Boarder-voice block on standardized entries.>

## Details

### Structure
<Which of the class's parts this one fills, and how.>

### Rules
<This instance's own grammar. Nothing shared restated as unique.>

## Additional Details

### Gaps

<Legibility debt on this instance.>
```

Class-specific headings sit between Structure and Rules. Protocol instances:
Doctrine, Crew, Personal, Passive. Gatekeeper instances: Refusals, Behaviour.
Weapon instances: Fire, Ability. Coming Soon keeps the same headings and writes
"Not built."

## Starting structure — protocol instance (copy this)

```markdown
# <Protocol>

<!--
Purpose: <Protocol>, an instance of [Protocols](Protocols.md).
Status: <working | Coming Soon>
Update trigger: any milestone that touches this instance, in the same change.
Last updated: YYYY-MM-DD
Where:
- <claim> — `path` (member)
-->

Instance of [Protocols](Protocols.md).

<One or two sentences: what this sheet does differently.>

## Game

<Preserved Boarder-voice block.>

## Details

### Structure
<Which four rows this sheet answers, and how the two keys work on it.>

### Doctrine
<What placement writes onto hardware.>

### Crew
<Name, key, output, cost.>

### Personal
<Name, key, who it hits.>

### Passive
<Name, or none, and what the garrison reads.>

### Rules
<Charge, pairing, refusals, composition — the sheet's own grammar.>

## Additional Details

### Gaps

<Legibility debt on this sheet.>
```

## Changelog

- 2026-08-31: House format is summary, Game, Details, Additional Details. Where
  lives in the header comment. Differs, The interaction, and a visible Where are
  the previous shape. Cause: the folder read as audits and dumps; Zo asked for the
  structure of each mechanic class, unique headings per class, shared organisation.
- 2026-08-31: Protocol instances replace Differs with Details (Structure, Doctrine,
  Crew, Personal, Passive, Rules) and park Where in the header comment. Cause: the
  four-row sheet was buried in The interaction and a code map, and each instance
  restated the class as a Differs dump instead of showing its components.
- 2026-08-29: Recorded the stance behind the pipeline (Zo): these are guardrails, not
  perfect rules — a targeted change that moves no mechanic is labelled overhead rather
  than forbidden, and drift from freestyle sessions is expected and caught by periodic
  code-against-entry audits.
- 2026-08-29: Added **Where this sits** — the pipeline line (Zo), plus the three
  amendments: same change rather than after closure, the entry as an input to the next
  brainstorm and milestone, and entries born from a code sweep with no milestone.
- 2026-08-29: **Every entry opens with a plain-language summary.** Cause: the
  standardized entries were auditable and unreadable — understanding a mechanic
  required reading the Boarder-voice block and then several hundred words of builder
  prose. Zo: detailed reading may be encouraged, never required. Precision was not
  reduced; a summary was put in front of it.
- 2026-08-29: Grilled and refined before landing (Zo): agent clause covers co-op
  seats and the realm; the consequence test given its falsifiable form (removal);
  state distinguished from mechanic (Salvage is a stock, the bank is the mechanic);
  instance bar set at behavioural difference, numbers-only variants stay Field
  Manual rows; core tier defined by removal. Standardization ruling added: existing
  `## Game` blocks are preserved above the builder sections, `## Gaps` may stay,
  `## Reference` and `## See also` are absorbed.
- 2026-08-29: Created, and re-cut the same day: entry format derived from the
  definition rather than inherited from the Compendium's Game/Reference/Gaps shape
  (Zo — previous shape should not dictate current definition). Earlier same-day
  cuts: a Design/Mechanics ledger (rejected — tracking is not the need), then
  reusing Compendium entries as-is (rejected — old shape dictating). Player-facing
  Compendium surface stays an open ruling with a reader dependency.
