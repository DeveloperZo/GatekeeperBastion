# M63 — The garrison is a roster

<!--
Status: later
Update trigger: when a kind is added, removed or renamed, when the property list or the
per-bastion draw rule changes, or when a fourth in-build kind ships
Last updated: 2026-08-20
-->

Three kinds becomes a decided roster of **twelve**, recorded once so every later slice
builds against the same list. The session log starts recording time spent at the Hatch
per Wave — the number every stat line in this roster is waiting on.

**This slice builds no new Gatekeeper and no mesh.** Bodies, Mixamo, and the three
in-build Play pages are `M63-2`. Each new kind after that is its own slice. `M66` is
hit confirm, audio tells, and player identity — not this.

**Shaved off 2026-08-20:** the **Ram→Thief rename** (criterion 5) and the **`in_build`
Compendium filter** (criteria 8 and 9) moved to `M63-3`, which builds the Cloaker — a
fourth kind cannot be authored under a name the enum is about to lose, and it needs a
player page that the eight unbuilt kinds must not follow into the Compendium. The
criteria keep their numbers here rather than being renumbered, because criterion 10 is
cited by name from `../../Brainstorms/gatekeepers-and-waves.md`.

## Story

As the builder, I want the garrison decided and written down once, so that twelve kinds
arrive as one coherent set rather than twelve separate arguments.

## Why it matters

**Three kinds were designed for one room and the room changed.** All three resolve at the
door. The bastion now has levels, lanes, blind corners, concealed approaches, terminals,
two Wave modes and a second player, and not one of the three uses any of it.

**A roster decided in pieces becomes twelve unrelated enemies.** The value of the set is
that no two take the same thing from the player, and that only holds if the list is
settled before the building starts. `../../Brainstorms/gatekeepers-and-waves.md` already
carries the rule that a kind is defined by a refusal; this slice extends that to a full
roster and to the properties that produce variants without new kinds.

**The measurement is still missing.** Every recorded loss is `HATCH BREACHED` with the
Boarder alive, and none of them says whether the door fell to too many arrivals or to
bodies standing too long. Those two answers want opposite kinds. M18 was demoted for
specifying a stat line without it. One log line closes it.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** Nothing the player
does changes here — no hp, speed, damage or agent footprint moves. What changes is that
the list exists and the door-time number is readable. Bodies are `M63-2`.

## Kill criterion

Two triggers; either fires alone.

**After the first three new kinds ship, if any two of them read as the same enemy in a
logged session, the roster is over-specified and gets recut** rather than extended. The
pairs to watch are named in the Watch section. Twelve is a target, not a promise.

Mesh silhouette and Mixamo retarget are `M63-2`.

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
5. **Moved to `M63-3`** (2026-08-20): Ram becomes Thief everywhere, with no behaviour
   change, on its own commit.
6. **The Thief's new rule is recorded but not built.** *Never damages while there is
   salvage to take; rams to take salvage from whoever holds it; damages only when there is
   nothing to take.* Written into `../../Decisions.md` and the brainstorm. The behaviour
   builds against M53's unbanked pile (`RunSim.Unbanked`, shipped 2026-08-13) in the
   Thief's own slice; this criterion is a recording only.
7. **M53's Ram exception is removed.** Criterion 4 of
   `M53-salvage-leaves-through-the-hatch.md` turns a terminal-only break back off. Under
   the Thief's rule there is no break to turn off, so that clause goes in this change and
   the edit says why.
8. **Moved to `M63-3`** (2026-08-20): unbuilt kinds' pages are builder records, not
   player Compendium. Eight named kinds that are not in the build have brainstorm rows
   only, or Play pages marked `in_build: false`.
9. **Moved to `M63-3`** (2026-08-20): a page that is not in the build does not reach the
   player — the `in_build` flag and the copy step that honours it (missing flag = true).
10. **Time at the Hatch is logged.** Gatekeeper-seconds spent within the Hatch's damage
    range, per Wave, on the session log, and `Tools/wave-report/run.sh` prints it beside
    the existing per-Wave medians.
11. **No stat lines for unbuilt kinds.** No hp, damage, speed or Wave count is written for
    any kind that does not exist in the build. `../../Design/4-Data.md` gains nothing this
    slice except the door-time column.
12. **No player-visible change at all** — the rename left with criterion 5. Suites green,
    same test names plus the door-time assertions. **No new counter, no accumulator, no stats
    object** (`../../Decisions.md`). **No new menu row without asking**
    (`../../Architecture/README.md`). Bodies are `M63-2`.

## Implementation

Ordered. The rename and the copy-step filter left with criteria 5, 8 and 9; what remains
is the recording, `M53`'s clause, and the door-time log.

1. **Keep** the roster table already in `../../Brainstorms/gatekeepers-and-waves.md`
   (restored 2026-08-15). Do not rewrite identities. Amend the refusal wording in
   `1-Experience.md` §1.4 to match the brainstorm's target-only form.
2. **Moved to `M63-3`:** the Ram→Thief commit.
3. **Change** `../../Decisions.md` for the Thief's rule and the amended refusal scope.
   **Change** M53 criterion 4 (criterion 7 here; the edit says why).
4. **Moved to `M63-3`:** the copy step honouring `in_build`. Do not write unbuilt Play
   pages here unless a kind's slice needs one.
5. **Change** the session log to carry door-time per Wave, and `Tools/wave-report` to
   print it. Additive; no accumulator (`../../Decisions.md`).
6. **Change** docs: Roadmap row, Changelog.

Rejected: **building any new kind here.** The Cloaker is `M63-3`; each of the eight
still-named kinds after it is its own slice.
Rejected: **meshes, Mixamo, player capsule** — `M63-2`.
Rejected: **freezing behaviours or properties.** Zo, 2026-08-13: the shape is agreed, the
properties and behaviours are not.
Rejected: **numbers before the door-time log.** That is the mistake that demoted M18.

## Supporting work

- **Driven by** existing spawn — here. No new input.
- **Shown by** the Ram→Thief name in Play and HUD-adjacent copy — here. Silhouettes are
  `M63-2`.
- **Reads back as** door-time per Wave in `Tools/wave-report` — criterion 10.

## Verification

1. Criteria 1 to 4 — read the brainstorm. Twelve kinds, twelve distinct things taken,
   properties listed apart from kinds, draw rule present, openness stated.
2. Criteria 5, 8 and 9 — verified in `M63-3`, not here.
3. Criterion 7 — M53's clause is gone, with a reason written into the edit.
4. Criterion 10 — one played five-Wave Run, then `Tools/wave-report/run.sh` prints
   door-time per Wave.
5. Criterion 12 — both suites green (`gkb_verify_request.txt`), same test names plus the
   door-time assertions.

## Do not build

| Not building | Why not |
|---|---|
| Any new kind | The Cloaker is `M63-3`; each of the eight after it is its own slice |
| Meshes, Mixamo, player capsule | `M63-2` |
| Thief's salvage behaviour | Recorded only (criterion 6) |
| Stat lines for anything unbuilt | The door-time answer decides them |
| A thirteenth kind | Properties produce variants |
| Per-bastion draw implementation | Recorded as a rule here |

## Watch

- **Two pairs may collapse in play and the roster kill criterion is pointed at them.**
  Alarmer and Source both add enemies. Thief and Reclaimer both move salvage.
- **No Gatekeeper in the game currently shoots.** Both ranged kinds need projectiles
  before either exists. Argument for building the Standoff Shooter early rather than late.
- **Audio is the real budget, not behaviour.** Twelve kinds means twelve sounds.
- **Twelve kinds at four or five per bastion is two or three distinct bastions before
  repeats.** The answer is the property list, not more kinds.

## Depends on / Unblocks

**Depends on nothing.** The door-time log is additive. Bodies are `M63-2`, and the
rename and the `in_build` filter left for `M63-3` on 2026-08-20; none of them wait on
this file.

**Unblocks every Gatekeeper slice after it**, because each one can then be cut against a
settled list, a settled property vocabulary, and a real answer about whether the door
falls to arrivals or to dwell.
