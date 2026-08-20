# M80 — One final window

<!--
Status: later
Update trigger: when the boss/elite composition is stamped, when the
mid-finale return or finale-access question is ruled, when
MasterControl.HatchWaves moves
Last updated: 2026-08-20
-->

Beats 6 + 7: the Control Room fight (copy + spawn wiring — the mechanics are
`M54`, in tree), then the finale re-ruled to **one** final Hatch-bank window
carrying the game's first boss/elite; survive it → Escaped and the demo ends.
Cut 7 of the demo family. **Both owner-Zo inputs were stamped 2026-08-20 and
this milestone is startable.**

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

## Owner-Zo inputs — both stamped 2026-08-20; this milestone is startable

1. **Boss/elite composition: one high-HP elite, no escorts** (Zo, 2026-08-20).
   A single body well above any Wave-5 enemy, in an otherwise normal final
   window. One lever, so a failed ordering bet is attributable; the
   boss-wears-the-player's-protocol-layout idea stays parked, not spec.
   Exact HP / damage / speed are tunable and stamp into beat 7's Numbers.
2. **Finale access: D3 stays exactly as authored** (Zo, 2026-08-20) — **zero
   code**. This input was framed as blocking on the belief that the ruled shape
   had orphaned the room. The tree says otherwise (verified 2026-08-20): M77
   already re-keyed **every** door onto bank survival, D3 included, so its
   `requiredTerminalIds: T4,T5` is an *eligibility* predicate and not a
   trigger — the same shape D1 and D2 have. T4 and T5 sit behind D2 in the
   staging hall, are startable and completable today, and the merge rule that
   makes D3 speak `demo.control_room` instead of a `demo.door_d3` is already
   built. Nothing is owed.
   Rejected: making D2 the Control Room access. The seal follows the
   `OpensControlRoom` flag, so it would seal the staging hall **and** the
   control room rather than a room, and it would need R15 re-emitted for a
   south passage — which beat 5 forbids. Also rejected for now: a zone-3 row
   in `DemoZones`, which would buy the approach a zone-complete bank lock at
   the cost of breaking that table's stated invariant that the last zone ends
   at `WaveDirector.MaxWaves`.
3. **Mid-finale Ship return: already ruled "no", in code.**
   `RunSim.OfferDockIfBankSurvived` refuses the dock offer while
   `_masterPhase == MasterControlPhase.Hatch`, so a Docked end is
   *unreachable* on the finale rather than merely unguarded. Criterion 6's "a
   Docked end never calls `EndDemo`" therefore needs a docstring saying why,
   not a guard. If this is ever reversed, that one line is the change.

## Code state

Verified 2026-08-20 by reading the tree, the same pass that stamped the two
inputs. Where this section and the prose above disagree, this section wins.
**Most of this milestone is already built** — what remains is small and named.

1. **Criterion 1's merge rule is already built.** `GameController.HatchBank.cs`'s
   `DoorLineFor` maps D3 to `demo.control_room`, and `DemoConversationIds`
   documents `demo.door_d3` as deliberately absent. Nothing to build.
2. **Criterion 2's spawn wiring is already built.** The Master terminal's
   `unlockedSpawns` holds both `SpawnRoom` instances (`Spawn_ControlW` at
   x=-76, `Spawn_ControlE` at x=+76), inactive until
   `SetUnlockedSpawnsActive(true)` runs after the seal. Beat 6's "planned …
   Build" row is stale. What is still owed is *play* evidence that five Waves
   emerge in R15 — a Run, not a build.
3. **"`DemoActive` has no readers here yet" is false** — there are ~30
   production read sites. What is missing is a **writer**.
4. **"Wheel uncapped" and "FAB powered" are not separate levers.** Every demo
   gate (`PrimaryRowsOpen`, `MultiplayerUnlocked`, the wheel cap, the Primary
   lock, the ARMORY sealed rack) is already `DemoActive`-guarded and no-ops the
   moment the flag flips. `DemoFabPowered` has **zero readers** —
   `FabricationVerdict` keys on `DemoActive` alone. So `EndDemo` writes two
   fields and the rest follows.
5. **Criterion 6's flag list predates `M79`.** `DemoZone2BankLost`,
   `DemoRungTwoOpened` and `DemoRungTwoBuysLeft` are not in beat 7's table.
   **`EndDemo` must leave all three alone** — they are the record of what the
   player played, and every reader of them is already `DemoActive`-gated.
6. **The retune is not just a constant.** `MasterControl.HatchWaves` is a
   `const`. Setting it to 1 breaks
   `MasterControlTests.HatchHalf_DoesNotDumpUntilLastWindow` on two assertions,
   and — because it is `const` — permanently retires from test coverage the
   intermediate-window machinery this milestone says must not be deleted.
   **Decide the declaration, not just the value**: a settable `static int` keeps
   both shapes pinnable. Also rename
   `Sequence_FiveTerminalThenFiveHatch_Escapes`, and fix the hand-written prose
   in `../../Design/4-Data.md` (its GEN block regenerates itself; the sentence
   above it does not).
7. **`demo.escaped` has no carrier.** Once `EndDemo` sets `DemoActive false`,
   `DemoShipDirector` is bypassed entirely, so nothing on the Ship knows an
   Escape just happened. A one-shot signal — a transient session flag or a save
   field — is an unlisted build item. `demo.master_control` and
   `demo.hatch_finale` also exist as ids and copy with **no playback call
   site**, on hooks that already exist.
8. **A defect to fix here, inherited from `M79`.** A Lost on the Master Control
   Hatch half writes `DemoZone2BankLost`, because the Master terminal half draws
   past `MaxWaves` and `DemoZones.ZoneOfWave` floors any deeper Wave to the last
   authored zone. Harmless in outcome — the rack is open by then — but it is a
   wrong write. Fixed by bounding the check to zone 2's authored Wave span
   rather than trusting the floor.

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
7. **Withdrawn 2026-08-20 — buy-back is `M90`, not this milestone.** It was
   split out of `M79` when the tree showed per-seat banked Salvage does not
   exist (a guest's `Banked` is written only for slot 0 from the host's own
   save and is never reported back), and `M90` is blocked on three owner-Zo
   rulings. The finale ships without buy-back; if `M90` lands first, the
   finale window inherits it for free because it is the same bank-window
   rule. Nothing here depends on it.
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

Depends: M79 (D2 open — built and closed 2026-08-20), `M54` + `M73` (in tree),
M74 (lines), the two owner-Zo inputs above. **Not** `M90`: buy-back was
withdrawn from this milestone's criteria (see criterion 7). Unblocks: the live game on that save; the friends
close of the demo (still needs the five-Wave friends session,
`../../Roadmap.md`).
