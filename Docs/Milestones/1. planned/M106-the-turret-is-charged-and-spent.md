# M106 — The turret is charged, and spent

<!--
Status: later
Update trigger: when the Outrider lands (M107) and the turret's aura, charge rate or Exhausted window is retuned against it.
Last updated: 2026-08-29
-->

FORGE's turret stops running on a life clock and an ammo count. It charges by firing,
Overcharge is spent from that charge on a held key, and spending it ends the machine.
Machines become naturally vulnerable and Magnet Hands retires.

## Story

As a Boarder, I want the turret's big moment to be something its own defending earned, so
that where I place it and how long I let it work is the decision rather than when I press
a key off cooldown.

## Why it matters

Today Overcharge is a cooldown ability: press `H`, the turret redlines for a window, and
nothing about the turret's own work fed it. The recut
(`../../Brainstorms/boarder-protocols.md`, *FORGE — the recut*, Zo 2026-08-29) makes the
special something the machine earns inside the fight it gets spent in, which is what turns
a placement into a bet on a position.

Three things already in the tree make this cheaper than it looks. The life burn is already
retired — `BoarderProtocol.OverchargeBurnForSeconds` returns 0 and
`OverchargeHpPerSecond` has no reader. The turret is already mortal by default since
2026-08-28, so natural vulnerability is mostly already true. And `TurretState` already
carries the shot counters this milestone re-purposes.

This is a **rule change** (#5): what ends the machine, and what pays for its special. No
existing number is retuned here; `M107` retunes against the second machine.

Leverage note: a lower tier was considered and rejected. Retuning `OverchargeShotBudget`
or the life clock (#12, parameters) moves how long the current shape lasts and leaves the
shape — an ability on a cooldown, and a machine that dies to arithmetic rather than to
being spent.

## Acceptance criteria

1. A deployed turret with no charge refuses Overcharge by name; the refusal names charge,
   not cooldown.
2. Turret charge rises only while the turret fires. A turret standing in an empty room
   gains no charge over any length of time.
3. Holding the crew binding with a charged turret standing runs Overcharge; tapping the
   crew binding with a turret standing redeploys it to where the Boarder is looking
   instead, and does not run Overcharge.
4. Overcharge ends into Exhausted: the turret stays on the field, stops acquiring and
   stops firing, and despawns after the Exhausted window without further input.
5. An Exhausted turret cannot be refreshed by redeploying onto it. Pressing the crew
   binding while it is Exhausted despawns it and starts the cooldown; it does not stand a
   fresh live turret.
6. Charge carries across a redeploy of a live turret, and is zero on a newly deployed one.
7. A Gatekeeper attacks a standing turret under the ordinary targeting rule, with no
   protocol passive involved. `Magnet Hands`, `MagnetHands` and any turret-pull radius do
   not exist in the tree.
8. FORGE's sheet shows a crew row and a personal row that name the press and the hold, and
   a passive row that reads as deliberately unfilled rather than blank.
9. The offline harness passes with the recorded failure set and no new reds.
10. `Docs/Play Mechanics/Protocol-FORGE.md` and `Protocols.md` describe the charged turret
    and the retired passive, in this same change.

## Implementation

Sim owns charge and the state chain; the Game layer owns the input and the presentation.

**Change** `Assets/Scripts/Sim/Run/TurretState.cs` — add `Charge` and `ChargeMax`, raised
by `SpendShot` rather than by time. Replace `LifeRemaining` / `TickLife` and the separate
`OverchargeShotsRemaining` belt with the state chain: `Available`, `Deployed`, `Special`,
`Exhausted`, `Despawned`, as one `TurretPhase` enum with the transitions as methods.
`BurnOvercharge` is deleted rather than adapted — it exists to spend life, and life is no
longer what pays.

**Change** `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` — delete
`OverchargeHpPerSecond`, `OverchargeBurnForSeconds` and `OverchargeShotBudget` (the belt
is replaced by charge). Delete the Magnet Hands passive from `PassiveAt` and its
`BoarderPassiveId` member; delete `TurretPullRadius` wherever it is read.

**Change** `Assets/Scripts/Sim/Meta/BoarderSheets.cs` — rewrite `CrewForge`,
`PersonalForge` and `PassiveForge`. The crew row names both inputs on one key. The
passive row is the authored TODO string, not an empty string: an empty passive row is what
`TryClaim`'s own remarks call a panel reading as broken.

**Change** `Assets/Scripts/Game/Player/PlayerTurretControl.cs` — the crew binding becomes
press-versus-hold. Press: deploy, or redeploy a live turret, or despawn an Exhausted one.
Hold past the threshold: Overcharge, refused without charge. Drive the phase transitions
and the Exhausted despawn from here.

**Change** `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` — FORGE's personal
binding does nothing this milestone; it is claimed by `M107`. Refuse it by name rather
than leaving it silent, so the sheet and the key agree.

**Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` — remove the
turret-pull branch. A turret is a target by the ordinary rule; nothing about who placed it
changes acquisition.

**Change** the HUD — charge needs a readout, or criterion 1 is a refusal a player cannot
predict. Put it on the existing ability strip rather than a new surface.

**Change** `Docs/Play Mechanics/Protocol-FORGE.md`, `Protocols.md` — in this same change.

**Do not build** the Outrider, EMP, the pairing rules, or any cross-machine effect. They
are `M107`. A turret that overcharges alone is the whole of this milestone.

## Supporting work

- **Driven by** the crew binding, press and hold — here.
- **Shown by** the charge readout on the ability strip and the sheet's crew row — here.
- **Reads back as** the charge readout, the turret going quiet on Exhausted, and the
  placement and Overcharge log lines — here.

## Open questions

- The hold threshold in seconds, the charge per shot, `ChargeMax`, the Overcharge rate
  multiplier and the Exhausted window are all *unknown, needs play data*. Author them at
  the low end of plausible and expect to move them at `M107`.
- FORGE's passive — **open, owner Zo**, trigger the `M107` close
  (`../../Brainstorms/boarder-protocols.md`, *FORGE — the recut*).

## Verification

1. Criterion 7 — `grep -rn "MagnetHands\|Magnet Hands\|TurretPullRadius" Assets/Scripts`
   returns nothing.
2. Criteria 1, 2, 6 — EditMode tests on `TurretState`: charge rises only through
   `SpendShot`; Overcharge refuses at zero charge; charge survives a redeploy and is zero
   on a fresh deploy.
3. Criterion 4 and 5 — EditMode tests on the phase chain: `Special` ends into `Exhausted`;
   an `Exhausted` turret cannot re-enter `Deployed`.
4. Criterion 9 — `Tools/verify/run.sh`, expect the recorded failure set and no new reds.
5. Criteria 3 and 8 — playthrough path: Play, claim FORGE at CREW QUARTERS, read the
   sheet, embark, start a Wave at the Hatch, tap the crew binding to place the turret, let
   it kill several bodies, tap again to redeploy it and confirm the charge readout does not
   reset, then hold the crew binding. Confirms the press-versus-hold split, the charge
   carrying, and Overcharge running from earned charge.
6. Criterion 4, in play — after Overcharge ends, the turret is visibly standing and silent,
   then despawns without input.

## Watch

- `PlayerAbility` holds one `_cooldown` shared across weapons and divides progress by the
  active weapon's figure. The turret's charge must not be routed through it or the same
  wrong-denominator bug arrives on a second surface.
- The Coming Soon protocol stubs pass a placeholder sheet; changing the sheet strings must
  not change their shape.

## Depends on / Unblocks

Depends on `M105` only for the sheet's row count, and only cosmetically — this milestone
can land first if the doctrine row is still present. Unblocks `M107`.
