# M112 — Tuned by playing, not by arithmetic

<!--
Status: later
Update trigger: after each adjudicating play session, and whenever a watched dial moves
Last updated: 2026-09-04
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

   **Run 1 of 5 — `Logs/GatekeepersProtocol_2026-09-04_07-18-49_p51616.log`**, seed
   259921031, the file's second Run. `dev=0 checkpoint=0 config=0 configTaint=0`, so it
   counts. `result=lost cause=hatch wave=5 startWave=1 duration=419 gross=4704 banked=4554
   respawns=1 seats=1 dial=0`. Kit: **no Primary at all**, MK-1 Sidearm T0, 0 banked at
   embark, FORGE equipped (read from the `[Protocol]` lines, since the run predates the
   protocol field this milestone's session added to both machine lines).

   - **Zone 1 free.** Waves 1 and 2 closed with the door at 600/600 and 1302 banked. The
     stake never engaged, and the Dock was offered and declined.
   - **Zone 2 lost it on the last wave.** Wave 5 drew Convoy at the terminal at +332 s,
     the door took its first hit at +398 s and was gone at +419 s. 32 of 39 bodies reached
     it, about 20 of them inside six seconds. The player killed 24 in that wave, every one
     a Default; the 6 Thieves and 6 Cloakers were never killed.
   - **Minimum door HP: 0.** This is the criterion-2 reading and it refutes the kill
     criterion below on its own terms, though on a kit no bought Primary would resemble.
   - **The arithmetic underneath.** Wave 5 is 39 bodies at a 2.60 health multiplier, about
     7800 HP, delivered over a 44.5 s window: 175 dps to hold the line. MK-1 T0 sustains
     about 56 and MR-35 T0 about 83, so no shipped weapon holds wave 5 alone. The door's
     600 HP is the buffer that absorbs the difference, which is what makes this the door
     dial's question rather than the weapon table's.
   - **`wave-report` output is not attached**: mono is absent on this machine, so the tool
     refuses. The figures above are read from the log's own lines.
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

**Moved 2026-09-04, dial 1 of 3:** `HatchBank.FieldWaveDamageScale` 0.15 → 0.29, its own
commit, derivation and evidence in `4-Data.md` beside the constant. Direction is the
constant's own written solve against the live 600 door, not the run: the run's door loss was
a mass arrival that no scale value survives. The spawn-gap pair was the other candidate and
was **not** moved, because `M116`'s live kill criterion measures the per-wave delivery windows
that dial sets, and moving it mid-bet would rewrite the falsifier underneath it.

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
