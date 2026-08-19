# M79 — The second loss opens the ladder

<!--
Status: later
Update trigger: when the zone-2 shape, loss-2 ordering, ladder rung 2, or the
buy-back rule in Demo beat 5 moves
Last updated: 2026-08-17
-->

Beat 5: zone 2 as one terminal launching 3 consecutive Waves, a heavier bank
the secondary kit cannot clear — authored loss 2 by construction — ladder
rung 2 (Primary + 2 buys outside the secondary cap), co-op buy-back at a live
window, and D2 on survival. Cut 6 of the demo family.

## Story

As a player who beat zone 1, I want zone 2 to repeat the unit one size up —
three Waves off one trigger, a bank my current kit cannot hold — and my
second loss to put a Primary on the rack, so that the loop deepens by playing
it, and each death visibly buys the next rung.

## Why it matters

The ordering **is** the spec, numbers tunable (Zo, 2026-08-17, beat 5):
secondary-kit ceiling < zone 2 bank requirement < Primary + 1-upgrade kit.
The loss lands at the banked Wave by construction — composition, not script —
and M77's consistency rule holds it: docking requires a survived window, so
the secondary kit cannot skip this bank. Arc on the authored composition;
the 3-consecutive trigger and buy-back are loop pieces the game keeps.

Dead structure stays dead (beat 5, Why): slots, the AND-graph, the allow
list, one-completion-per-bank-start, D3=T4+T5.

## Acceptance criteria

1. Zone 2's trigger launches 3 Waves consecutively with no Ship phase
   between, under the re-keyed per-trigger `MaxWaves`; zone start / complete
   logs fire; the bank refuses until Wave 3 clears.
2. `EndRun("hatch")` on zone 2's window leaves D2 shut; survival with
   Primary + 1 logs `bank window survived zone=2` then
   `door open D2 cause=bank` (M77 machinery, wired here).
3. Docked at `PodEjections >= 2` plays `demo.second_loss` (never
   `demo.first_hatch_down`) and logs the ladder opening; rung 2 = the Primary
   row + 2 buys outside the secondary cap; a buy past the rung refuses named,
   without `SpendSalvage`; rung-1 history is unaffected.
4. Retrigger at zone 2's terminal defaults to composite (`start=1 waves=3`);
   partial election reads `DemoZoneDeepestWave[2]` and launches the remainder
   only.
5. Buy-back: a dead guest during a live window pays from their own banked,
   rejoins at the Hatch, and is logged; the same attempt after the window
   refuses with `no live window`; the shared pile is never touched; the door
   open replicates to both seats. The dead-host branch is not built (open,
   recorded).
6. Bank-slot bookkeeping (`HatchBank.SlotsPerRun`) is retired per the Delete
   table, reader sweep first.
7. Tuning observation, open until logged (`[unconfirmed]`): a rung-1-only run
   never logs `bank window survived zone=2`. Not closable on arithmetic
   alone.
8. Playthrough: D1 → zone 2 trigger → 3 Waves → bank falls on the secondary
   kit → Docked, Primary bought → retrigger → survive with Primary + 1 → D2
   opens → optional dock. Confirms 1–4 on screen.

## Implementation

Beat 5's Create / Change / Delete tables are the file list: zone 2 terminal
config, heavier bank composition (tunable, ordering fixed), ladder rung 2
bookkeeping extending `DemoWheelBuysLeft`, buy-back interact, D2 wiring on
M77's keying, and the slot-bookkeeping removal (`needs code verification` of
readers before deleting). Primary cost and buy-back cost are tunable and
unstamped — restamp in beat 5 when set (owner Zo).

## Open

- **Dead host vs `M49`** — open, recorded (Zo, 2026-08-17). Do not build the
  host branch until ruled.
- Zone 2 terminal reachability while D1 is shut (beat 5, Gaps) — needs code
  verification of the layout before deciding whether a refuse surface is
  still needed.

## Traps

Beat 5 Traps apply verbatim. Highest-cost three: softening the window so the
secondary kit can clear it (breaks the ordering and the dock-skip guard at
once), opening D2 from the terminal's done-branch, and resolving the
dead-host case in code "while in there."

## Depends on / Unblocks

Depends: M77 (door keying, dock, snapshot), M78 (plain embark, retrigger
offer, ladder rung 1). Unblocks: M80 (D2 open, party at the Control Room).
