# M112 — Tuned by playing, not by arithmetic

<!--
Status: later
Update trigger: after each adjudicating play session, and whenever a watched dial moves
Last updated: 2026-09-01
-->

Every dial the August rebalance moved was solved on paper and says so in its own comments.
This milestone plays the alpha until three of those dials are measurements, and moves each
one at most once.

## Story

As a Boarder, I want leaving the Hatch to be a real decision, wave 5 to be a fight that
must travel, and the turret to be a deployable with a rhythm, so that the pressure the
docs describe is the pressure I actually feel.

## Why it matters

The tree is explicit about what has never been played. `../../Design/0-Gameplay.md:156`:
the field-wave door scaling is "arithmetic, not a measurement … the first dial to turn".
`../../Design/4-Data.md:512`: whether wave 5 still clears against the raised 767/1013
ceiling is "unknown — needs play data". `M106` sits in planned with its implementation in
the 0.34.2 tree, "folder status waits on play". And the one relevant observation on record
points the worrying way: the 12:42 run on 2026-08-28 ended with the door at 570/600 —
thirty damage across two full zones — which reads as a stake that never bites.

Leverage: **#12, constants** — the dials are `HatchBank.FieldWaveDamageScale`,
`ShipSystems.HatchMaxHp`, the spawn-gap pair, and the turret trio. Nothing structural
moves here; if play data says the structure is wrong, that is a new milestone, not a
bigger commit on this one.

## Acceptance criteria

1. An evidence base of **five untainted full runs** (start at the Hatch, end however they
   end) exists in `Logs/`, listed by filename in this file, with `Tools/wave-report`
   output attached for each. Checkpoint-tainted runs are excluded by the 2026-08-20
   taint ruling.
2. **The door dial is adjudicated.** From those runs: minimum door HP reached per run,
   read from `hatch under attack` lines and the summary. The dial
   (`FieldWaveDamageScale` × `HatchMaxHp`) moves **at most once**, with the evidence and
   direction recorded in `4-Data.md` beside the constant.
3. **Wave 5 is adjudicated.** From the same runs: whether wave 5's field was ever cleared
   with the player inside Hatch radius the whole wave (kill lines carry `hatchDist=`), and
   the wave-5 outcome per run. At most one move of the spawn-gap pair follows.
4. **The turret is adjudicated** — charge/spend/Exhausted read from the `[Protocol]` and
   turret log lines across at least two FORGE runs. `M106` moves folders on this
   criterion, and its own acceptance list is the gate; this criterion only supplies the
   play sessions it is waiting for.
5. Each dial that moves does so in its own commit, one cause one change, and each session
   moves at most one dial before the next session.
6. `4-Data.md`'s "unknown — needs play data" at line 512 and `0-Gameplay.md`'s "first dial
   to turn" paragraph are rewritten to state what was measured — numbers and log
   filenames, not adjectives.

## Kill criterion

Pre-registered 2026-09-01, before the work, while the decision is still neutral.

**The bet:** the field-wave door stake creates a real decision to leave the Hatch.

**Kill:** if across the five criterion-1 runs at ante 0 the door never drops below **480
of 600** (80%), the stake is theatre — and the response is to **cut it**, not to retune
it: revert the door to invulnerable on field waves and return the Thief question to the
design table, per the repair discipline's "a workaround that bypasses the system" clause
in reverse. Adjudicated from criterion 2's table, not from feel. Date: five runs or
**2026-09-21**, whichever comes first; if five untainted runs have not happened by then,
that is itself the finding (the starved lane is play, and the milestone reverts to
planned with that recorded).

**Not a kill signal:** whether the door *felt* threatened, whether wave 5 was fun, or any
single dramatic run. Feel improves discontinuously with polish; this criterion is only
about whether the mechanism ever engages.

## Pre-mortem

- The five runs happen but all at cautious play, door untouched, and the kill fires on a
  stake that would bite a bolder player. Mitigation: criterion 2 records *why* the door
  was safe (body arrivals vs interceptions from the log), and the kill's "cut" verdict is
  Zo's to stay, stated on this file.
- One dial move invalidates the earlier runs' comparability. Mitigation: criterion 5's
  ordering — adjudicate all three dials from the same five-run base *before* the first
  move; post-move runs start a new table.
- The suites go red mid-milestone from a dial's pinned derivations. Expected: the pins
  derive from constants, so a dial move recompiles them — any pin that breaks instead of
  following is itself a defect to fix under M111's rules.

## Implementation

**Change** `Assets/Scripts/Sim/Run/HatchBank.cs` / `Assets/Scripts/Sim/Ship/ShipSystems.cs`
/ `Assets/Scripts/Sim/Waves/WaveDirector.cs` / `Assets/Scripts/Sim/Meta/BoarderProtocol.cs`
— only the adjudicated dial, only once each, evidence in the same commit.
**Change** `Docs/Design/4-Data.md`, `Docs/Design/0-Gameplay.md` — criterion 6.

Rejected alternative: a tuning spreadsheet over simulated runs — that is the retune-by-
arithmetic this milestone exists to stop, and the design doc already marks one such model
as wrong rather than deleted.

## Verification

1. Criterion 1 — the run list in this file; `Tools/wave-report/run.sh` on each names the
   summary line and taint state.
2. Criteria 2–4 — the tables in this file, each cell citing a log filename and line
   field (`hatch under attack`, `hatchDist=`, the turret lines).
3. Criteria 5 — `git log` on the four Sim files shows at most one tuning commit each
   within this milestone's range.
4. Playthrough path — the runs are the milestone; the shortest route is EMBARK at ante 0
   and playing to an ending.

## Do not build

- New enemy kinds, new waves, new weapons — content is not tuning.
- Any structural change to the loop's verbs; a structural finding becomes a new file.

## Depends on / Unblocks

Depends on: `M109` (a loop that cannot be finished cannot be tuned), `M103`'s Unity leg
(the summary line adjudicates taint and outcomes cheaply). Unblocks: `M106`'s folder move,
`M107`'s kill-criterion window (deploy-order data rides the same logs), and E10's deferred
difficulty-default question, which inherits criterion 2's table as its evidence.
