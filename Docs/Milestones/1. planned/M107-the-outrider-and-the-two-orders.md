# M107 — The Outrider, and the two orders

<!--
Status: later
Update trigger: when the kill criterion below resolves, or when EMP's root length, the pairing radius or either charge rate is retuned after play.
Last updated: 2026-09-01
-->

FORGE gets a second machine: a follower that travels with the Boarder and shoots, charging
by firing exactly as the turret does. Its special is EMP — light damage and a
medium-length root — and when the two machines are near each other, each one's special
lifts the other. Deploying the turret first or the Outrider first are two routes to the
same peak, and which one a Boarder took is a record of how they played the Wave.

## Story

As a Boarder, I want a machine that comes with me when I push for the terminal, so that
holding ground and pushing across the zone are both things FORGE is good at, and the order
I deploy in is a choice rather than a habit.

## Why it matters

`M106` gives FORGE one machine that rewards standing still. That is half a protocol: the
Assault's real fork is hold the ground and grind the Wave down, or push across the zone
and shut the terminal off early (`../Play Mechanics/A-Boarding.md` owns that fork), and
FORGE currently only answers the first half.

The claim this milestone is built on, stated so it can be wrong: **the deploy order
becomes a record of how the Wave was played.** A charged turret is evidence it was placed
and defending; a charged Outrider with no turret out is evidence the Boarder went and
found the fight. That is the whole reason for two machines rather than one better one.

Leverage tier: **rule change** (#5) plus one new element. The cheaper alternatives were
considered and rejected in the recut: a second turret is the same machine twice, and a
follower with no special is a damage number that could have been a weapon tier.

**This is a bet.** Its kill criterion is below and is pre-registered before the work.

## Acceptance criteria

1. Tapping the personal binding deploys the Outrider; it follows the Boarder and fires on
   the Gatekeeper nearest to the Boarder, not the one nearest to itself.
2. Outrider charge rises only while it fires, on the same rule as the turret's.
3. Holding the personal binding with a charged Outrider runs EMP; tapping with one already
   out redeploys it to the Boarder; holding with no charge refuses by name.
4. EMP applies light damage and a root of a stated length to every Gatekeeper it catches.
   A rooted body cannot close, and a body already in reach still finishes its swing.
5. With the Outrider deployed and within the pairing radius of a standing turret, running
   Overcharge puts the Outrider at maximum fire rate for the Overcharge window, and running
   EMP makes the turret's fire EMP rounds for the EMP window.
6. Outside the pairing radius, each special affects only its own machine, and the sheet
   says the pairing is proximity-gated.
7. Both machines end their special into Exhausted independently, on their own windows.
8. Both deploy orders are reachable in one Wave and both arrive at a max-rate EMP turret
   beside a max-rate Outrider.
9. The Outrider is a target by the ordinary rule, mortal, and its death is legible.
10. A single Boarding's log names, for each Wave, which machine was deployed first and what
    each machine's charge was when its special ran.
11. The offline harness passes with the recorded failure set and no new reds.
12. `Docs/Play Mechanics/Protocol-FORGE.md`, `Protocols.md` and `Gatekeepers.md` (the root
    is a new thing that can happen to a body) are updated in this same change.

## Kill criterion

**Pre-registered 2026-08-29, before the work, while the decision is still neutral.**

If, across ten logged Boardings played by Zo after this ships, the same deploy order is
used in **nine or more** of them, the two-orders claim is false: the order is a habit, not
a record, and the second machine is carrying a story it does not tell. On that result
FORGE collapses back to one machine with a charged special and the Outrider is cut, rather
than being retuned into being interesting.

Read from criterion 10's log line, which exists to answer exactly this. **Date: 30 days
after `M107` closes.** If ten Boardings have not been played by then, the criterion has
not resolved and the date moves once, by agreement — it does not quietly lapse.

Not a kill criterion, deliberately: whether the loop *feels* good. Feel improves
discontinuously with polish and this ships graybox.

## Pre-mortem

Written adversarially, because solo there is nobody else to voice the doubt.

- **The peak is the only line that gets played.** Both orders reach the same peak, so a
  Boarder finds whichever route is two seconds faster and never varies. The tell: criterion
  10's log shows one order at nine in ten, which is the kill criterion firing.
- **The Outrider makes the turret pointless on the push line.** A machine that follows and
  shoots is strictly more convenient than one that has to be placed. The tell: turrets get
  deployed only as an Overcharge battery next to the Outrider, never as a held position.
- **EMP's root does the whole job and the damage is noise.** If a medium root is enough to
  stop a Wave reaching the Hatch, nothing else on the sheet matters. The tell: Hatch damage
  taken per Wave drops sharply on FORGE against the other two protocols.
- **Charge-by-firing rewards the safest place to stand.** Both machines charge fastest
  where the most bodies are, which is the Hatch on a Convert and the lane mouth otherwise
  — so the mechanic may push toward camping the exact spot the loop wants a Boarder to
  leave. The tell: Outrider-first Boardings still end with the Boarder at the Hatch.
- **Two machines double the things that can be lost.** With both mortal, a bad placement
  loses both specials before either is spent, and the sheet reads as fragile rather than
  clever. The tell: specials spent per Boarding well below one each.

## Implementation

**Create** `Assets/Scripts/Sim/Run/OutriderState.cs` — the follower's Sim state, modelled
on the recut `TurretState`: the same phase chain and the same charge rule, plus follow
distance. Shared shape rather than a shared base class; two small parallel types read
better than one type with a machine-kind flag.

**Create** `Assets/Scripts/Sim/Run/GatekeeperRoot.cs` — `MayAdvance` / `MaySwing`, so the
root's close rule is EditMode-testable without NavMesh.

**Create** `Assets/Scripts/Game/Bastion/OutriderObject.cs` — the body that follows, fires
on the Boarder's nearest Gatekeeper, takes damage, and runs the EMP blast. Parallel to
`TurretObject`; `PlayerOutriderControl` cannot be the body.

**Create** `Assets/Scripts/Game/Player/PlayerOutriderControl.cs` — deploy, redeploy,
and the held-key EMP.

**Change** `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` — add the EMP ability, its window,
its root length, its damage, and the pairing radius the two specials read.

**Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` and its movement — the root. A
rooted body holds position, keeps its target, and swings if already in reach. This is the
garrison's first status effect, so it lands as one named state on the body rather than as a
speed multiplier, or the next one has nowhere to live. (Cut against the plan: the root landed
in `EnemyGatekeeper.cs` rather than `Targeting.cs`, which took the machine-divert block.)

**Change** `Assets/Scripts/Sim/Meta/BoarderSheets.cs` — FORGE's personal row is the
Outrider and EMP; the crew row names the pairing.

**Change** the HUD — the Outrider needs its own charge readout beside the turret's, and
the pairing needs to be visible or criterion 6 is invisible to the player.

**Change** `Assets/Scripts/Game/Bastion/GameController.cs` — the per-Wave log line for
criterion 10.

**Do not build** a passive for FORGE in this milestone. It is open with Zo as owner and
this close is its trigger.

## Supporting work

- **Driven by** the personal binding, press and hold — here.
- **Shown by** the second charge readout, the pairing indicator and the sheet — here.
- **Reads back as** the readouts, the root on caught bodies, and the per-Wave order line —
  here.

## Open questions

- Root length (`EmpRootSeconds` = 2.5), EMP damage (8), pairing radius (8 m), follow
  distance (4 m, orbiting), Outrider fire rate (2/s) and both charge rates (1 per shot, max 10)
  are authored at the low end — *unknown, needs play data*.
- Whether the Outrider should be blocked from following through a closed door, or simply
  path around, is undecided and only matters once doors are on the push line.

## Verification

1. Criteria 1, 2, 3, 7 — EditMode tests on `OutriderState` mirroring the `M106` turret
   tests: charge only through firing, refusal at zero charge, special ends into Exhausted.
2. Criteria 5 and 6 — EditMode tests on the pairing: inside the radius each special lifts
   the other, outside it neither does. Both orders tested, since the pairing must be
   symmetrical.
3. Criterion 4 — an EditMode test that a rooted Gatekeeper does not close and does finish a
   swing already started.
4. Criterion 11 — `Tools/verify/run.sh`, expect the recorded failure set and no new reds.
5. Criteria 8, 9, 10 — playthrough path: Play, claim FORGE, embark, start a Wave at the
   Hatch. Wave one: turret first, charge it, deploy the Outrider, hold crew, then hold
   personal. Wave two: Outrider first, charge it on the walk out, place the turret, hold
   personal, then hold crew. Confirms both orders reach the peak, and the log names the
   order and both charges for each Wave.
6. The kill criterion is read from criterion 10's log across ten Boardings, not from this
   verification.

**Offline leg run 2026-09-01. Criteria 1-7 and 12 hold; 8, 9 and 10 are built and unobserved.**
`Tools/verify/run.sh` cannot complete on this machine: `mcs`/`mono` are absent, and it aborts
at the glyph lint over three em dashes in `Ship - CoOp.unity` that have been there since
2026-08-21. Both stages were reproduced with the .NET SDK outside the tree instead, and the
lints were run individually. **EditMode 828 passed / 4 failed / 5 ignored** — the four are the
recorded scene-fixture set (`HudSeparationTests.ShipPause_CompendiumHasItsOwnRow`, both
`M59PolishChromeTests` rows, `RecallPadSceneTests.FieldTerminals_CarryRecallPads_NotDockPortals`)
and none is new. `OutriderStateTests` 6/0, `GatekeeperRootTests` 2/0, `TurretStateTests` 6/0,
`WaveMachineRecordTests` 11/0.

The one red this work owned is closed. The turret and Outrider commit pushed `NetVerbs.cs`
(356 to 549) and `PlayerProtocolAbilities.cs` (343 to 459) past the 400-line cap, adding two
files to the arch fence. Both are back under it — 277 and 399 — and the over-cap set is again
the five files that predate this work. `lint-arch` still exits 1 on those five, deliberately
(`../../Architecture.md`).

Criteria 8, 9 and 10 need the playthrough in step 5, and the kill criterion needs ten
Boardings. Neither has been observed, so the file stays here.

## Watch

- The root is the first status the garrison has ever carried. Anything that reads a
  Gatekeeper's speed directly, rather than asking it whether it may move, will fight this.
- Two machines both charging in a busy room is a per-frame allocation risk on the targeting
  scans; keep the nearest-Gatekeeper lookup off the per-frame path.

## Depends on / Unblocks

Depends on `M106`. Unblocks the FORGE passive decision, which is triggered by this close.
