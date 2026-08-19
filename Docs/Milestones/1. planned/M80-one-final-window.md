# M80 — One final window

<!--
Status: later
Update trigger: when the boss/elite composition is stamped, when the
mid-finale return or finale-access question is ruled, when
MasterControl.HatchWaves moves
Last updated: 2026-08-17
-->

Beats 6 + 7: the Control Room fight (copy + spawn wiring — the mechanics are
`M54`, in tree), then the finale re-ruled to **one** final Hatch-bank window
carrying the game's first boss/elite; survive it → Escaped and the demo ends.
Cut 7 of the demo family. **Not started-able until the two owner-Zo inputs
below are stamped** — cut now so the family is whole and the inputs are
named, per the cut decision (Zo, 2026-08-17).

## Story

As a player who opened the way in, I want the last fight to seal me in a
room, then send me back to the Hatch for one final window with something
bigger than anything before it — so that the demo ends on the loop's own
verb, held one last time, and the game opens up when I survive it.

## Why it matters

`M54` already runs the sequence; in tree the Hatch half is five windows,
ruled to one (`MasterControl.HatchWaves` 5 → 1, Zo 2026-08-17 — retune, keep
the multi-window machinery). Nothing today walks the player out of the room,
and nothing ends the demo — `DemoActive` has no readers here yet
(`../../Demo/7-hatch-finale.md`, Why this beat exists). Arc: the finale is
authored payoff; the boss/elite is its first authored body.

## Owner-Zo inputs (blocking — stamp before build)

1. **Boss/elite composition + stats** — absent, not invented (beat 7). Stamp
   into beat 7's Numbers. The boss-wears-player's-protocol-layout idea stays
   parked, not spec.
2. **Finale access under the ruled shape** — beat 6's storyboard still opens
   D3 on T4+T5, but the ruled demo shape has no T4/T5 and doors key on bank
   survival (M77). Whether D2 is the Control Room access and D3 is dead, or
   something else opens the room, is unruled. One ruling, stamped in beat 6.
3. Also open, recorded, not blocking the cut: **mid-finale Ship return**
   (does the finale window offer M77's return before Escaped?) — this
   milestone specs nothing for it either way; a Docked end must not
   `EndDemo`. Finale kit mapping is `[unconfirmed]` and resolves with
   input 2.

## Acceptance criteria

1. D3-open (or the ruled access) plays `demo.control_room` only — the merge
   rule replaces `demo.door_d3`; `demo.master_control` plays on first Master
   look-at; juice/stake stays on the Master prompt (a demo hide is a defect).
2. The five room Waves spawn from the `M73` ControlW/E closets, wired via
   `T_Master` UnlockedSpawns; Hatch HP is unchanged through the Terminal half.
3. `MasterControl.HatchWaves` is 1; `MasterControlTests` hold: single window,
   no slot spend, dump at end, Escaped only after it; the multi-window
   machinery is retuned, not deleted.
4. The boss/elite spawns in the final window per the stamped composition
   (input 1).
5. `MasterHatchHalfStarted` fires `demo.hatch_finale` (Subtitle, always) and
   the always-on send log; verification never gates on the verbose half-flip
   line.
6. On Escaped: `EndDemo` — `DemoActive false`, FAB powered, wheel uncapped,
   NAV chain restored, capsule never again — plus `demo.escaped` and
   `[Demo] demo ended`. On a Lost half: `DemoActive` stays true; a Docked
   end never calls `EndDemo`.
7. Buy-back at the finale window follows the M79 rule (live window only, own
   banked, last chance before Escaped); dead host stays unbuilt (open).
8. Loopback: both seats get the send; both load the shared Ship after the
   end screen; FAB is live on a seat whose Meta saved Escaped.
9. Playthrough: from D2 (M79) through Escaped — seal, five Waves, send, one
   window with the boss, BASTION STRIPPED, Ship with every console live.

## Implementation

Beat 6 and beat 7 Create / Change / Delete tables are the file list: the
merge rule and two beat-6 lines + log; the `MasterHatchHalfStarted` hook;
`DemoProgress.EndDemo` and its flags table; `HatchWaves` 5 → 1; the boss
spawn in the final window (from input 1); buy-back wiring at the finale.
**Delete** nothing — the five-window machinery stays.

## Traps

Beat 6 + beat 7 Traps apply verbatim. Highest-cost three: adding Escaped at
the end of the Terminal half (a third win — forbidden), setting
`DemoActive false` on a Lost Master, and deciding the mid-finale return in
code.

## Depends on / Unblocks

Depends: M79 (D2 open), `M54` + `M73` (in tree), M74 (lines), the two
owner-Zo inputs above. Unblocks: the live game on that save; the friends
close of the demo (still needs the five-Wave friends session,
`../../Roadmap.md`).
