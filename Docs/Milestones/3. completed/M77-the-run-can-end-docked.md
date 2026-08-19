# M77 — The run can end Docked

<!--
Status: completed, Aug 2026 — closed on criteria 2 and 3 only
Update trigger: when TH-9 to TH-14 close, or when the dock commit seat or the
Hatch E-collision is ruled
Last updated: 2026-08-19
-->

The run-structure change, on its own: doors open on bank-window survival
(never on terminal completions, never on a failed bank), and a player who
survives a bank window may hold interact at the Hatch — until the next Wave is
triggered — to end the Run **Docked**, not Lost, with banked Salvage and door
state persisting. Cut 4 of the demo family, split out of beats 3+4 at cut
(Zo, 2026-08-17) because it is the piece the rest of the game keeps.

## Story

As a player who just held a bank window, I want to choose between pushing
deeper and going home with what I banked — and to find the door open when I
come back — so that ending a run is a decision I make at the Hatch, not only
a death.

## Why it matters

Locked in `../../Decisions.md` (*Locked — return to Ship at a surviving
bank*, Zo 2026-08-17) and stamped through `../../Design/1-Experience.md`,
`2-Dynamics.md`, `3-Mechanics.md` and Demo beats 2/4/5/7. This is a **loop**
change: it converts every bank window after the authored losses into a
push-your-luck decision, at the fixed authoring cost of one run-end cause and
one door re-key. Because both authored losses are unclearable windows, Ship
return never skips the unlock deaths — the wheel key stays
`PodEjections >= 1` (beat 2, survive path).

Key code delta (beat 4, stated plainly): the in-tree `BastionUnlockGraph`
keys doors on terminal completions (M52/M53 lineage) — the superseded reader.
Doors key on bank-window survival now.

## Kill criteria

Bet. Evaluate at **10** logged post-demo Runs with at least one survived
window, or by **2026-09-30**, whichever comes first. Zo's play report is read
first; the counts locate, they do not decide.

1. **One dominant answer** — across those Runs, `docked at hatch` follows
   `bank window survived` either always or never. The dock/push-on choice
   collapsed; the parked early-return salvage-multiplier penalty
   (`../../Demo/README.md`, Gaps/parked) is the recorded counter-lever to
   evaluate then, not now.
2. **It reads as a loss** — Zo's play report says a Docked end feels like
   failing. Copy/end-screen recut, once, before touching the mechanic.

## Acceptance criteria

1. `EndRun("docked")` exists: phase Docked, not Lost — no `PodEjections++`,
   no Lost screen; the end screen names the Docked cause.
2. Surviving a bank window opens that zone's door
   (`[Demo] door open D<k> cause=bank`); `EndRun("hatch")` on the same window
   leaves it shut — a failed bank never unlocks.
3. A completed terminal no longer opens any door; the graph's terminal-AND
   path and the done-terminal `[E] Open door` branch are retired per beat 5's
   Delete table.
4. After a survived window, `[hold E] Dock — return to Ship` is live at the
   Hatch until the next Wave is triggered, then gone — held E after the
   trigger does nothing.
5. The successful-bank snapshot persists banked + door state: dock, re-embark,
   and the log shows `[Demo] snapshot restored doors=<…>` with the door open.
6. `demo.door_d1` (Lock, on `SetOpen` via bank survival) then
   `demo.dock_option` (Subtitle, by construction — never Lock) play in that
   order on the survival frame.
7. Solo down and Hatch breach still end the run Lost exactly as before
   (`M49` unchanged); with `DemoActive false` the door keying and dock rule
   hold — this is the game's rule, not a demo gate.
8. Playthrough: survive a window (dev-tuned if needed ahead of M78's kit),
   dock, spend, re-embark through the open door. Confirms 1, 2, 4, 5 on
   screen.

## Implementation

From beat 4 steps 4–5 and beat 2's survive path: **Change**
`BastionUnlockGraph` / `BastionDoor.SetOpen` (bank-survival keying),
`RunSim.EndRun` (`"docked"` cause; snapshot carries banked + door state),
`GameController.HatchBank` (survival event); **Create** the Hatch dock
interact and the beat-4 test list rows that belong to this grain (failed bank
leaves the door shut; survived bank opens it; dock ends Docked with
snapshot). Voice ids per beat 4's table.

Two stated unknowns to resolve first, both `needs code verification` (beat 4,
Gaps): what the unlock graph subscribes to today, and what the snapshot
stores today. Resolve by reading, then scope — not by guessing.

## Open (from the cut)

- **Dock commit seat in co-op** (who holds E to end the party's Run) — not
  ruled; owner Zo, trigger before the co-op path of this milestone builds.
  Solo path is buildable now.
- **Dead-host buy-back vs `M49`** — recorded open (Zo, 2026-08-17); not this
  milestone's to resolve. Do not build the host branch.

## Traps

Beat 4 Traps apply — the three aimed at this grain: keying doors on terminal
completions because the in-tree graph does; counting a Docked end as a loss
(side effects included); leaving the dock interact live after the next Wave
trigger. Plus beat 5's: docking cannot skip an unsurvived window — the rule
is survival-gated by construction.

## Closed 2026-08-19 — on two criteria, not eight

Zo closed this on a playthrough from the capsule to the first door opening, and
approved. That run exercises **criterion 2** (surviving a bank window opens that
zone's door) and **criterion 3** (a completed terminal opens nothing, since the
door could only have come from the bank). Both are the heart of the change and
both are now observed on screen.

It stops before the dock. **Criteria 1, 4, 5, 6, 7 and 8 were NOT observed** and
are owed, not verified — filed as `TH-9` to `TH-14` in `../../Tech-Hygiene.md`.
The offline harness is green against a recorded baseline (459 passed, 11 failed,
failing set byte-identical before and after), but offline-green is not playable
and does not close any of the six.

Two things found in code while building remain open and are Zo's to rule: the
Hatch's `E` collision (the dock offer takes the key ahead of repair and
withdraw, so the Hatch cannot be repaired between a survived window and the next
Wave trigger), and the co-op dock commit seat.

## Verification status (2026-08-19)

Built solo-path-only at Zo's ruling, on top of M76 step 1 of 6 — the dependency
on M76 is **deferred, not met**. Nothing in this milestone's code surface needs
M76 steps 2-6.

Proven offline (`Tools/verify/run.sh`, mono in a cloud container, mirror of
`Sim/ Tests/ Game/Settings/ Game/Demo/` only):

- **460 passed, 10 failed.** The 10 are byte-identical to the pre-change
  baseline recorded on the same mirror — the known partial-mirror set
  (`GatekeeperPresentationTests` x2, `HudSeparationTests` x2,
  `M59PolishChromeTests` x5, `SessionMusicHostTests` x1). No new reds.
- 23 net new tests: `DockedRunTests` (10, whole dock lifecycle and the v10
  round trip), `DockAndDoorSourceTests` (7 source pins on the Game-side
  wiring), 5 added to `BastionUnlockGraphTests`, 2 added to `DemoVoiceTests`.
- Six existing tests were **rewritten, not deleted**, because they pinned the
  rule this milestone retires: `BastionUnlockGraphTests` x2,
  `TerminalWaveTests` x4, `MasterControlTests` x1. Each now asserts that
  terminal completion is eligibility and a survived bank is the unlock.
- `RunSim.cs` crossed the 600-line arch-lint cap when the dock path landed, so
  the dock path moved to `RunSim.Dock.cs`. Lint-arch, lint-language and docgen
  all pass.

**Not proven, by anything here.** The harness compiles only `Sim/`,
`Game/Settings/` and `Game/Demo/`. Every file below is MonoBehaviour or scene
facing: it was syntax-checked one file at a time with `mcs -target:library`
(only CS0234/CS0246 missing-type errors, no CS1xxx), which proves it parses and
nothing else.

`GameController.cs`, `GameController.HatchBank.cs`, `GameController.EndScreen.cs`,
`HudController.cs`, `HudController.EndScreens.cs`, `HudController.Pause.cs`,
`BastionLayout.cs`, `BastionTerminal.cs`, `RepairInteractable.cs`,
`WaveSpawner.cs`, `EndSummary.cs`.

So: **criterion 1 has offline evidence; criteria 3 and 7 have offline evidence
for the sim half and source pins for the Game half; criteria 2, 4, 5, 6 and 8
are unproven until the editor runs.** Nobody has pressed Play.

## Rulings made while building (not in the original spec)

1. **`RunPhase.Returned`, not `RunPhase.Docked`.** Criterion 1 asks for "phase
   Docked". `RunPhase.Docked` is enum value 0 and already means *aboard your
   ship, before boarding* — `IsTerminal` excludes it, `HudController` answers it
   with `ShowArriving()`, and three tests assert a Docked sim is inert
   (`TickWhileDocked_HasNoSideEffects`, `PlayerDown_NoOpWhenDocked`,
   `HatchDestroyedWhileDocked_DoesNotPod`). Reusing it would have made the run
   end without ending. A fifth value was appended instead — the enum's own
   comment says append-only. The player-facing word is still **DOCKED**; only
   the enum member is named Returned.
2. **The terminal-AND path survives as eligibility.** Criterion 3 says the
   "terminal-AND path" is retired. What was retired is *terminal completion
   opening a door*: `TryCompleteTerminal` no longer calls `TryOpenDoors`, and
   the `[E] Open door` branch is gone. The AND over `RequiredTerminalIds` stays,
   because deleting it leaves nothing to answer "which door does surviving this
   window open" — the sketch's `unlock=T1,T2` becomes the eligibility list, not
   the trigger. **This resolves the M76 open item** (the sketch keying D1 to
   T1+T2 versus beat 2's door-on-bank-success): both are now true and they are
   not the same statement.
3. **Save schema v10, no shim.** Door state stopped being derivable from
   `CompletedTerminalIds`, so `OpenDoorIds` is its own saved fact. Pre-v10 saves
   load with no open doors, deliberately: their doors were opened by the rule
   this milestone deletes, so re-deriving them would re-assert it.
4. **Door restore is ungated by start wave.** `RestoreCompletedTerminals` is
   gated on `StartWave > 1`; open doors are not, because a Docked return
   re-embarks at Wave 1 and criterion 5 requires the door to still be open.
5. **Voice order is enforced in `DemoVoice`, not by the caller.** A Subtitle
   raised on the same frame as a live Lock used to preempt it, which would have
   swallowed `demo.door_d1`. The one queue slot now accepts that pairing. This
   is why criterion 6's ordering is testable offline at all.
6. **Guests get a DOCKED screen, not "YOU DIED".** `RunEndKind.Docked = 3`
   appended to the wire enum. This is host-authoritative broadcast only; it does
   not touch the unruled co-op dock seat.

## Open, found in code, not in the original spec

- **The Hatch's E key is now quadruple-claimed, and dock wins.** `E` at the
  Hatch already meant hold-repair, hold-withdraw and tap-select-intensity. While
  the dock offer is live, dock takes E ahead of repair and withdraw, because it
  is the only one of the three that expires. **Consequence: the Hatch cannot be
  repaired between a survived window and the next Wave trigger** — which is
  exactly when a player would want to repair it. One escape hatch was built in:
  arming a Hatch-bank (`IsHatchBankArmed`) yields E back, so push-on stays
  reachable. This is a design cost, not a bug, and it is Zo's to rule:
  a timer on the offer, or moving the dock to its own collider beside the Hatch,
  are the two levers. Recorded rather than papered over.
- **Tap-versus-hold on the same key.** `TickPrepCommands` reads E-down to toggle
  intensity select while the Interactor reads E-held for the dock. The hold is
  1.5 s — longer than the capsule's 1.0 s precisely because this one ends the
  Run — but a deliberate long press on the select key still docks.

## Depends on / Unblocks

Depends: M76 (zone-gated banks worth surviving; D1 as the first target).
Unblocks: M78 (retrigger loop closes the right way), M79 (D2), M80 (finale
window rules reference the same survival machinery).
