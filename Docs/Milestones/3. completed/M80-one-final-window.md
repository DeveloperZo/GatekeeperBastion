# M80 — One final window

<!--
Status: shipped, Aug 2026 — built 2026-08-20. Playthrough (criterion 9) and
two-seat loopback (criterion 8) are not observed; they stay open on this
file the same way M79's composition bet did.
Update trigger: closed. Corrections only.
Last updated: 2026-08-20
Closed on: 2026-08-20. Gate: Unity EditMode `MasterControlTests` +
`DemoProgressTests` + `DemoFinaleSourceTests` + `HatchBankTests` +
`RunCompleteTests` 75/0. `lint-arch` clean. Offline `mcs` harness skipped
(mcs/mono not on PATH, same as `M79`).
-->

Beats 6 + 7: the Control Room fight (copy + spawn wiring — the mechanics are
`M54`, in tree), then the finale re-ruled to **one** final Hatch-bank window
carrying the game's first boss/elite; survive it → Escaped and the demo ends.
Cut 7 of the demo family. **Both owner-Zo inputs were stamped 2026-08-20 and
this milestone is startable.**

Built 2026-08-20. `EndDemo`, the HatchWaves retune, the elite, and the three
unplayed voice lines landed; the merge rule, ControlW/E wiring, and WaveIsIn
Lost-write were already in the tree.

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

Landed 2026-08-20, matching Code state:

- **Change** `MasterControl.HatchWaves` — `const` 5 → `static int` default 1. `HatchHalf_DoesNotDumpUntilLastWindow` sets 5 and TearDown restores it.
- **Change** `MasterControl.ApplyFinaleElite` — replaces the Hatch-bank spec with one Ram at `FinaleEliteHp` 240 (spawned 624 vs Wave-5 Ram 312). No escorts: that is the one lever. WaveSpec.finaleElite flags the HP override.
- **Create** `DemoProgress.EndDemo` — `DemoActive false`, `DemoFabPowered true`. Called from `EnterTerminalPhase` on Escaped only. `GameSession.PendingEscapedLine` carries `demo.escaped` onto the Ship after the flag flips.
- **Change** `OnMasterHatchHalfStarted` — `[Demo] hatch finale send` + `demo.hatch_finale` Subtitle, Party.
- **Change** first Master look-at — `demo.master_control` Lock, Party; start logs `[Demo] master-control room fight`.
- WaveIsIn Lost-write, D3 merge rule, ControlW/E wiring: already in tree. Buy-back stayed with `M90`.

**Delete** nothing — the five-window machinery stays.

## Verification

1. D3 merge — `DemoFinaleSourceTests.D3_SpeaksControlRoom_NotADoorId`; `BastionKitSceneTests` still names Spawn_ControlW/E.
2. Hatch HP through Terminal half — existing `MasterControlTests` (Hatch not damaged in Terminal). Five Waves in R15 is play, not this suite.
3. `HatchWaves` 1 — `HatchWaves_DefaultsToOne`; `Sequence_FiveTerminalThenHatchWindows_Escapes`; multi-window pin `HatchHalf_DoesNotDumpUntilLastWindow` at 5.
4. Elite — `FinaleWindow_IsOneEliteRam`; source pin `EliteSpawn_OverridesTheFirstRamHp`.
5. Send — source pin `HatchHalf_SendsFinaleSubtitle_BeforeAnyHalfFlipLogGate`.
6. EndDemo — `DemoProgressTests.EndDemo_FlipsTheTwoWrites_AndLeavesTheRungTwoRecord`; Lost leaves `DemoActive` (`HatchHalfBreach_KeepsDripped_WipesRemainder`); Dock refused on finale (`DockOffer_IsRefusedOnTheFinale`).
7. Withdrawn.
8. Party send is `PlayLocalAndBroadcast` on hatch_finale / escaped / master_control. Two-seat load not observed.
9. Playthrough not observed.

Unity EditMode 2026-08-20: the five fixtures above **75/0**. `lint-arch` clean.

## Traps

Beat 6 + beat 7 Traps apply verbatim. Highest-cost three: adding Escaped at
the end of the Terminal half (a third win — forbidden), setting
`DemoActive false` on a Lost Master, and deciding the mid-finale return in
code.

## Depends on / Unblocks

Depends: M79 (D2 open — built and closed 2026-08-20), `M54` + `M73` (in tree),
M74 (lines), the two owner-Zo inputs above. **Not** `M90`: buy-back was
withdrawn from this milestone's criteria (see criterion 7). Unblocks: the live
game on that save; the friends close of the demo (still needs the five-Wave
friends session, `../../Roadmap.md`).

## Open

- Criterion 8 — both seats on the shared Ship after Escaped. The Party
  broadcast is wired; a two-seat load was not run.
- Criterion 9 — playthrough from D2 through Escaped. EditMode proves the
  shape, not the fight.
