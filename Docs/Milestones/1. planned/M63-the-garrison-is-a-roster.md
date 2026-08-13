# M63 — The garrison is a roster

<!--
Status: later
Update trigger: when a kind is added, removed or renamed, when the property list changes, or when the per-bastion draw rule changes
Last updated: 2026-08-13
-->

Three kinds becomes a decided roster of **twelve**, recorded once so every later slice
builds against the same list. **Ram becomes Thief** in code and docs. The Garrison volume
in `Docs/Play/` is restructured to hold a roster rather than three pages, and the session
log starts recording time spent at the Hatch per Wave, which is what every stat line in
this roster is waiting on.

**This slice builds no new Gatekeeper.** It is the list, the rename, the volume shape and
the missing measurement. Each kind is its own slice afterwards.

## Story

As the builder, I want the garrison decided and written down once, so that twelve kinds
arrive as one coherent set rather than twelve separate arguments, and so a friend can read
what each one refuses to do.

## Why it matters

**Three kinds were designed for one room and the room changed.** All three resolve at the
door. The bastion now has levels, lanes, blind corners, concealed approaches, terminals,
two Wave modes and a second player, and not one of the three uses any of it.

**A roster decided in pieces becomes twelve unrelated enemies.** The value of the set is
that no two take the same thing from the player, and that only holds if the list is
settled before the building starts. `../../Brainstorms/gatekeepers-and-waves.md` already
carries the rule that a kind is defined by a refusal; this slice extends that to a full
roster and to the properties that produce variants without new kinds.

**And the measurement is still missing.** Every recorded loss is `HATCH BREACHED` with the
Boarder alive, and none of them says whether the door fell to too many arrivals or to
bodies standing too long. Those two answers want opposite kinds. M18 was demoted for
specifying a stat line without it. One log line closes it.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** Nothing the player
does changes here. What changes is that the list exists and the door-time number is
readable.

## Kill criterion

**After the first three new kinds ship, if any two of them read as the same enemy in a
logged session, the roster is over-specified and gets recut** rather than extended. The
pairs to watch are named in the Watch section. Twelve is a target, not a promise.

## Acceptance criteria

1. **The roster is recorded once**, in `../../Brainstorms/gatekeepers-and-waves.md`, as
   twelve kinds: Default, Hunter, Thief, Source, Enabler, Alarmer, Bypasser, Reclaimer,
   Exploder, Standoff Shooter, Ailment Shooter, Cloaker. Each carries its refusal and
   **what it takes from the player**, and no two entries name the same thing taken. The
   file states plainly that behaviours, properties and every number are **open**, decided
   per slice, and that this list is the roster and not a specification.
2. **The refusal rule is restated in its useful form.** The refusal governs the **target**
   a kind will and will not go after, and that stays enforced in code. Damage amounts,
   fallback attacks and what a kind does when cornered are **not** protected, and an
   enemy that scratches you on the way past has not lost its identity. The current
   absolute wording in `../../Architecture/` or `../../Design/1-Experience.md` §1.4 is
   amended in the same change to say so.
3. **Properties are listed separately from kinds**, so a variant never becomes a
   thirteenth kind: movement (climber, vent-born, drops from above), shield,
   invulnerable until a stated condition, visibility and camouflage, pack with a leader,
   splits on death, and the elite flag. The file says a property may ride on any kind.
4. **The draw rule is recorded.** A bastion shows a subset of the roster, not the roster.
   One difficulty budget per bastion, Wave templates with slots that draw from that
   bastion's set, and the last Waves reserved for the heaviest draw. Whether a bastion's
   set is authored, drawn, or a fixed core plus a draw is **stated as an open choice**,
   not decided here.
5. **Ram becomes Thief everywhere, with no behaviour change.**
   `Sim/Waves/GatekeeperKind.cs`, `WaveDirector.RamPerWave`, `Game/Enemies/RamGatekeeper.cs`,
   prefabs, tests, `Docs/Design/`, `Docs/Play/` and the milestone files that name it.
   Persisted save keys are checked and either migrated or left alone deliberately, stated
   either way. One commit. A Run before and after is identical apart from the name.
6. **The Thief's new rule is recorded but not built.** *Never damages while there is
   salvage to take; rams to take salvage from whoever holds it; damages only when there is
   nothing to take.* Written into `../../Decisions.md` and the brainstorm. **The behaviour
   waits on M53**, because unbanked salvage does not exist yet. This criterion is a
   recording, and a slice that implements it without M53 is building against nothing.
7. **M53's Ram exception is removed.** Criterion 4 of
   `M53-salvage-leaves-through-the-hatch.md` turns a terminal-only break back off. Under
   the Thief's rule there is no break to turn off, so that clause goes in this change and
   the edit says why.
8. **The Garrison volume is written, all twelve.** `Docs/Play/` gains a Gatekeepers hub
   explaining the garrison as a set of refusals, plus **one entry per kind, twelve of
   them**, in `../../_meta/_definitions/language.md` *Player-facing voice*, with `## See
   also` per M62 criterion 1. Origin plus fight tells, one pause each. **This is the
   Garrison volume M62 copies**, so it is written here and not deferred.
9. **A page that is not in the build does not reach the player.** Each kind page carries
   an `in_build` flag in its front matter, and M62's copy step ships only the pages marked
   true. The source folder is the whole roster; the player build is what exists. Flipping
   one flag is how a kind's entry ships in the same slice as the kind, with no page to
   write at that point because it is already written.
10. **Time at the Hatch is logged.** Gatekeeper-seconds spent within the Hatch's damage
   range, per Wave, on the session log, and `Tools/wave-report/run.sh` prints it beside
   the existing per-Wave medians. **This is the criterion the rest of the roster is
   waiting on**, because arrivals and dwell want opposite kinds and nobody can currently
   tell which one is breaking the door.
11. **No stat lines for unbuilt kinds.** No hp, damage, speed or Wave count is written for
    any kind that does not exist in the build. `../../Design/4-Data.md` gains nothing this
    slice except the door-time column.
12. **No player-visible change** beyond the rename and the Play pages. Suites green, same
    test names plus the door-time assertions. **No new counter, no accumulator, no stats
    object** (`../../Decisions.md`). **No new menu row without asking**
    (`../../Architecture/README.md`).

## Implementation

**Change** `../../Brainstorms/gatekeepers-and-waves.md` into the roster record: twelve
kinds with refusal and what-it-takes, the property list, the draw rule, the amended
refusal wording, and the open questions kept open.

**Change** the Ram identifiers to Thief in one behaviour-preserving commit, on its own, so
a bisect can find it.

**Change** `../../Decisions.md` for the Thief's rule and the amended refusal scope.
**Change** M53 criterion 4.

**Create** the Gatekeepers hub in `Docs/Play/` and all twelve kind pages under it, with
`## See also` links both ways and an `in_build` flag on each. The three existing pages move
under the hub. This is the bulk of the writing in this slice and it is what M62 ships.

**Change** the session log to carry door-time per Wave, and `Tools/wave-report` to print
it. This is the only code with behaviour in this slice and it is additive.

Rejected: **building any of the nine new kinds here.** Twelve implementations is an epic,
and `../../_meta/_definitions/milestone.md` says an epic is not a milestone.

Rejected: **shipping unbuilt kinds' pages to the player.** The pages are written here so
M62 has a Garrison volume to copy; the `in_build` flag is what keeps a friend from reading
about an enemy they cannot meet. Writing the roster down is not lore ahead of the game.
Putting it in front of a player would be.

Rejected: **freezing behaviours or properties.** Zo, 2026-08-13: the shape is agreed, the
properties and behaviours are not. Each kind's slice decides its own, against this list.

Rejected: **numbers before the door-time log.** That is the mistake that demoted M18.

## Verification

1. Criteria 1 to 4 — read the file. Twelve kinds, twelve distinct things taken, properties
   listed apart from kinds, draw rule present, openness stated.
2. Criterion 5 — `grep -rn "Ram" Assets/ Docs/` returns only deliberate historical
   references. Play one Run before and after and diff the session log; only the name moves.
3. Criteria 7 to 9 — M53's clause is gone with a reason; the hub and twelve pages exist and
   link both ways; `Tools/lint-language/run.sh` passes; a copy step run with mixed flags
   ships only the in-build pages.
4. Criterion 10 — one played five-Wave Run, then `Tools/wave-report/run.sh` prints door-time
   per Wave. **Read it and record which term is breaking the door**, because that answer is
   the input to every kind that follows.
5. Criterion 12 — both suites, same names plus the new assertions.

## Do not build

| Not building | Why not |
|---|---|
| Any of the nine new kinds | Each is its own slice |
| Thief's salvage behaviour | Waits on M53; there is no unbanked pile yet |
| Shipping unbuilt kinds' pages into the build | Written now, gated by `in_build` |
| Stat lines for anything unbuilt | The door-time answer decides them |
| A thirteenth kind | Properties produce variants; kinds do not |
| Per-bastion draw implementation | Recorded as a rule here, built when a second bastion exists |

## Watch

- **Two pairs may collapse in play and the kill criterion is pointed at them.** Alarmer and
  Source both add enemies, and "doubles once if it lives" versus "keeps producing until
  killed" may not read differently in a fight. Thief and Reclaimer both move salvage, one
  off a carrier and one off the floor, and may feel like one enemy in two costumes.
- **No Gatekeeper in the game currently shoots.** Both ranged kinds need projectiles, firing
  positions and a pre-fire sound before either exists. That system is a sixth of the roster
  sitting behind one piece of work, which is an argument for building the Standoff Shooter
  early rather than late.
- **Audio is the real budget, not behaviour.** In first person an off-screen enemy has no
  telegraph at all. Twelve kinds means twelve sounds a player can tell apart, and in co-op
  it is also how the other player knows what is happening to you.
- **Twelve kinds at four or five per bastion is two or three distinct bastions before
  repeats.** The answer to that is the property list, not more kinds.

## Depends on / Unblocks

**Depends on nothing.** The door-time log is additive and the rename is mechanical.

**Unblocks every Gatekeeper slice after it**, because each one can then be cut against a
settled list, a settled property vocabulary, and a real answer about whether the door falls
to arrivals or to dwell.

**Feeds M62** directly: this slice writes the Garrison volume M62's reader copies, so M62
ships with a full volume rather than three pages. M62's copy step gains the `in_build`
filter.
