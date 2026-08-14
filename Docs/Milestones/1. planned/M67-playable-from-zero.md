# M67 — Playable from zero

<!--
Status: later
Update trigger: when the observed performance band changes, when a structural invariant is added, or when a new progress gate is authored
Last updated: 2026-08-14
-->

Two played floor tests, a recorded performance envelope, and a short list of structural
invariants that fail when the rules make the game impossible. Built because **M52 and M53
each met their own criteria and jointly made the game unwinnable for a new player**, with
both suites green the whole time.

Nothing here asserts the game is winnable. It asserts the game is **possible**, and it trips
when a rule demands something no player has ever done.

## Story

As the builder, I want a check that fails when two slices compose into an impossible game, so
that the first person to notice is not a friend playing the build.

## Why it matters

**Every gate in this project is per-slice.** Acceptance criteria ask whether a milestone did
what it said. Nothing asks whether the game still works afterwards. M52 gated the door on five
Waves. M53 gated banking on the end of five Waves. Each is defensible alone. Together they
leave a new player with no partial success at any point in a Run, and no test anywhere had a
reason to look.

**The failure was authored, not coded**, which is why more unit tests of the usual kind would
not have caught it. The rules were implemented correctly.

**And the obvious fix is a trap this project has already fallen into.** An arithmetic model
that computes whether a Run is winnable is exactly the retune-against-arithmetic failure
`.claude/skills/gkb-design` names, and `../../Design/` still carries a model marked wrong from
the last attempt. That model would also have declared M52 plus M53 winnable, because the
problem was never the numbers.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No rule changes. What
changes is that an impossible composition is loud.

## Kill criterion

**If this suite is green through a session that a human finds unplayable, the invariants are
measuring the wrong thing** and get recut against whatever that session actually hit. Once,
then the approach is wrong rather than the list.

## Acceptance criteria

### A — the played floor

1. **A fresh-save dev command.** Wipes meta and starts Run one, leaving the real save
   untouched and restorable. **This is the cheapest item here and probably the most
   valuable**, because the reason the new-player path never gets tested is that testing it
   costs you your working save.
2. **A level-zero PlayMode run.** Fresh `MetaState`, no upgrades, a scripted player that only
   holds position near the Hatch and fires at the nearest target. The Run must reach a
   terminal state and **bank a non-zero amount**. If the least sophisticated competent player
   gets nothing at all, the game has no floor.
3. **A null-input run.** The player never acts. The Run must end as a clean loss with the
   report written, no hang, no soft-lock, no unreachable state. This is the crash and
   dead-end test, not a difficulty test.

### B — the recorded envelope, which is not a model

4. **The observed performance band is recorded as data**, sourced and dated, not as prose in a
   design doc: kill rate **0.61 to 1.08 per second across every loadout**
   (`../../Design/2-Dynamics.md`). Generated into `../../Design/4-Data.md` per `M33`. Changing
   the band is a deliberate, dated edit, because it means the game changed.
5. **Required kill rate per Wave is computed from the Wave tables and asserted to stay inside
   a stated multiple of that band.** The test does not say a Wave is winnable. It says a Wave
   now demands performance that has never been observed, and a human should look. The multiple
   is stated in the file rather than discovered.
6. **The fresh-save floor.** Wave one, zero upgrades, must require less than the **bottom** of
   the band. If a new player has to outperform the worst loadout ever measured to survive the
   first Wave, the Run has no on-ramp.

### C — the structural invariants, which are what would have caught it

These read the rules, not the combat, and none of them needs a model of the fight.

7. **Partial success exists.** At least one path through a Run yields non-zero banked salvage
   **without a full five-Wave clear**. M53's rule as written makes this zero.
8. **No compound gate on the first reward.** Count the conditions that must be satisfied before
   a player can bank anything. If more than one of them requires a player outcome, such as
   surviving N Waves or clearing a terminal set, fail. Two multiply into impossibility, and
   each looks reasonable alone.
9. **Difficulty is monotonic and bounded.** Wave N+1's budget is at least Wave N's, and the
   Wave-to-Wave ratio stays under a stated cap, so nobody can author a spike by editing one
   table row.
10. **The economy is solvent.** Salvage earned on a survivable path is at least the cost of the
    cheapest meaningful upgrade. Otherwise a player who cannot yet clear can never buy their
    way to clearing, which is a progression dead end that no single milestone owns.
11. **Every terminal state is reachable.** Win, loss, and any partial ending. A state nobody
    can enter is a rule that does not exist.

### D — the authoring rule, because this bug was written

12. **Every gate on player progress names what happens when the player fails it.** One sentence
    per gate, added to `_meta/_definitions/milestone.md` and to the `gkb-design` skill so it is
    asked before code exists. M52 and M53 each omitted it, and the omission is the whole bug.

### E — it must fail

13. **Re-create the M52 plus M53 combination and confirm the suite goes red**, then revert and
    confirm green. **This criterion is the milestone.** Everything above it is a claim until
    this is observed, and a gate that cannot fail is not a gate.
14. **Runs from `Tools/verify/run.sh`** beside the existing checks. Suites green, same names
    plus the new ones. No new counter, no accumulator, no stats object
    (`../../Decisions.md`). No new menu row without asking (`../../Architecture/README.md`).

## Implementation

**Create** the fresh-save command in the existing dev console (`M13`), with the real save moved
aside rather than deleted.

**Create** the two PlayMode runs as an extension of the existing combat fixture, not a new
harness.

**Create** the invariant checks in `Sim/`, reading the Wave tables and the gate rules as data.
They must not need a scene, so they run in EditMode and cost nothing per commit.

**Change** `../../Design/4-Data.md` generation to carry the band and the required-rate column.

Rejected: **a headless winnability model.** It is the arithmetic retune the design skill names,
`../../Design/` already carries one marked wrong, and it would have passed the exact
composition that prompted this milestone.

Rejected: **asserting the game is fun, balanced, or well-paced.** None of that is machine
readable and pretending otherwise is how a green suite starts substituting for playing.

Rejected: **a per-milestone playability sign-off.** The invariants are standing and automatic,
or they will be skipped on the milestone that most needs them.

## Verification

1. Criterion 13 first. Reintroduce the composition, watch it go red, revert, watch it go green.
   Nothing else in this file means anything until that is seen.
2. Criteria 1 to 3 — run them from a genuinely fresh profile folder, not a reset one.
3. Criteria 4 to 6 — hand-check one Wave's required rate against the band, so the arithmetic is
   confirmed once by a person.
4. Criteria 7 to 11 — each invariant gets a deliberately broken input proving it fires.

## Watch

- **The envelope is a record, not a target.** It says what has been observed. The moment
  someone tunes a Wave to sit neatly inside it, it has become the predictive model this
  milestone exists to avoid, and `../../Design/` already shows what that costs.
- **The band goes stale when weapons or enemies change**, and `M63` queues nine new
  Gatekeepers. Updating it must be deliberate and dated, and a silent widening to make a red
  test green is the failure mode to watch for.
- **Green means possible, not good.** None of this catches five Waves being too long, or a
  first fifteen minutes that is miserable. That still needs somebody playing who has not seen
  the systems.
- **Criterion 8 is the one that will be argued down**, because every individual compound gate
  has a good reason. So did both of the ones that caused this.

## Depends on / Unblocks

**Depends on nothing.** `RunSim` is already constructible standalone and seeded (`M45`), and the
PlayMode combat fixture already exists.

**Unblocks handing a build to someone**, because it is the difference between a suite that says
the code runs and a suite that says the game can be played.
