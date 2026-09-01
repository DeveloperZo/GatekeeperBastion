# M106 — The turret is charged, and spent

<!--
Status: later
Update trigger: when the Outrider lands (M107) and the turret's aura, charge rate or Exhausted window is retuned against it.
Last updated: 2026-09-01
Recut 2026-09-01 (Zo): the rule change had already landed in the 0.34.2 tree — criteria
1-10 read true against the code on the day of the recut, and were kept as the gate rather
than deleted, because the folder move still waits on the play path. Criteria 11-14 are new
and are the milestone's remaining build. The stale "do not build the Outrider" guard was
struck, since M107's machine landed alongside this one. No criterion was softened; see
"What the recut changed" below.
-->

FORGE's turret stops running on a life clock and an ammo count. It charges by firing,
Overcharge is spent from that charge on a held key, and spending it ends the machine.
The machine wears that charge on its own body — a pylon whose crystal is dark when a hold
would be refused and lit when it would land. Machines become naturally vulnerable and
Magnet Hands retires.

## Story

As a Boarder, I want the turret's big moment to be something its own defending earned, so
that where I place it and how long I let it work is the decision rather than when I press
a key off cooldown — and I want to read whether it is ready by looking at the machine, not
by looking away from it.

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

**The readout is the recut's own argument, and it sits a tier above the rule change.**
The first cut of this milestone named the problem and then settled for half of it:
*"charge needs a readout, or criterion 1 is a refusal a player cannot predict."* The HUD
square landed and is correct. But charge is spent on a **placement** decision, and a
placement decision is made while looking at the machine across a room — the HUD tells you
only in the moment you have stopped looking at it. So the refusal in criterion 1 is still
one a player cannot predict at the moment they need to. Putting charge on the machine's
own body is an **information flow** (#7), above the rule change this milestone already
claims, and it is why the pylon is in this file rather than in a cosmetic one of its own.

Leverage note: two lower tiers were considered and rejected. Retuning
`TurretChargePerShot`, `TurretChargeMax` or the Exhausted window (#12, parameters) moves
how long the current shape lasts and leaves the shape. Adding a second HUD element — still
a readout, but on the surface the player is not looking at — moves the symptom and leaves
the structure: the machine stays mute.

Rejected by name, so it is not re-proposed inside this work:

- **A separate cosmetic milestone for the pylon.** It would have no gate of its own —
  "the turret looks better" is not a criterion that can be false. Tied to charge, it is.
- **Keeping the cylinder-and-barrel and adding a charge bar over the machine.** A second
  floating bar beside the life bar. It is legible and it is wrong: it reads as UI stapled
  to the world, and the phase chain still has no silhouette.
- **A Synty or FaxLab3D mesh for the pylon.** `Docs/Decisions.md` keeps vendor payloads
  out of git; a machine whose read depends on an unimported 534MB pack is a machine that
  is invisible on a fresh clone.

**Where the pylon rule does not apply.** The crystal carries charge, not health. The life
bar stays: a machine at full charge and one hit from falling must not read identically,
and folding health into the same glow would make exactly that mistake.

## Acceptance criteria

Criteria 1-10 landed in the 0.34.2 tree ahead of this recut and are kept as the gate — the
folder move still waits on the play path in Verification. Criteria 11-14 are the remaining
build.

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
11. A standing turret reads as a pylon: a dark tapered plinth with a faceted crystal
    floating above it, separated by a visible gap. No cylinder body and no barrel remain
    in `TurretObject.Spawn`.
12. The crystal carries charge. At zero charge it is inert — unlit, and visibly so from
    outside the turret's own range. At `ChargeMax` it is at full glow. Between them the
    glow rises with charge rather than snapping at a threshold, so a player looking only
    at the machine can tell whether a hold will be refused.
13. The phase chain is legible on the machine without reading the HUD: `Deployed` glows in
    the machine's own colour, `Special` runs the Overcharge colour with the crystal spun
    up, and `Exhausted` drops the crystal onto the plinth, unlit and motionless. Each is
    distinguishable from the other two at a glance.
14. The plinth turns to face what the turret is shooting; the crystal's spin is its own and
    does not encode facing. A client's presentation-only copy shows the same pylon, the
    same glow and the same phase reads as the server's.

## Implementation

Sim owns charge and the state chain; the Game layer owns the input and the presentation.
Criteria 1-10 are landed — the entries below are recorded so the file describes the tree,
and only the pylon entries are open work.

**Landed** `Assets/Scripts/Sim/Run/TurretState.cs` — `Charge` / `ChargeMax` raised by
`SpendShot` rather than by time; `TurretPhase` as `Available`, `Deployed`, `Special`,
`Exhausted`, `Despawned` with the transitions as methods. `LifeRemaining`, `TickLife`,
`OverchargeShotsRemaining` and `BurnOvercharge` are gone.

**Landed** `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` — `OverchargeHpPerSecond`,
`OverchargeBurnForSeconds`, `OverchargeShotBudget`, the Magnet Hands passive and
`TurretPullRadius` are gone.

**Landed** `BoarderSheets.cs`, `PlayerTurretControl.cs`, `EnemyGatekeeper.Targeting.cs`,
the HUD charge square, and the two Play Mechanics documents.

**Change** `Assets/Scripts/Game/Bastion/TurretObject.cs` — replace the primitive body in
`Spawn`. The plinth is a tapered frustum, the crystal an octahedron floating above it with
a gap. Neither is a Unity primitive, so both come from a small procedural mesh builder;
build each mesh **once into a shared static**, not per spawn. Facing stays on the plinth —
`TickFiring` currently rotates `transform`, which would drag the crystal with it, so the
plinth becomes the rotated child and the crystal is parented outside that rotation.

**Add** `Assets/Scripts/Game/Bastion/PylonMesh.cs` — the two meshes, as cached statics.
An octahedron of eight triangles for the crystal; a hexagonal frustum for the plinth. No
UnityEngine type crosses into `Sim/`; this is presentation and lives in `Game/`.

The crystal's eight triangles do **not** share their six corners. Criterion 11 asks for a
*faceted* crystal, URP/Lit has no flat-shading path, and six shared verts under
`RecalculateNormals` produce radial per-vertex normals that shade as a rounded blob rather
than cut stone. So each face carries its own corners — 24 verts, 8 triangles — and
`RecalculateNormals` is only safe *because* nothing is shared. The first cut of this
section said "six verts"; that was a sketch of the shape, and the criterion outranked it.

**Change** the crystal's drive — `PushTint` becomes a phase-and-charge push: emission
scaled by `Charge / ChargeMax`, colour by phase, spin and bob rate by phase, and the
crystal's local height dropping to rest on the plinth on `Exhausted`. It already runs on
presentation-only copies, which is what buys criterion 14's second sentence for free.

**Fix while rebuilding** `Spawn` allocates a `new Material` per turret and `PushTint`
mutates it, so every despawned turret leaks one material. The phase colours are a fixed
small set — hold them as shared statics and drive per-instance emission through a
`MaterialPropertyBlock` rather than by mutating a material. This is named here because the
rebuild touches the exact lines; it is not a separate repair.

**Do not build** a charge bar, a second HUD element, or any Outrider change. The Outrider
is `M107`'s and is already in the tree — do not retune it here.

## Supporting work

- **Driven by** the crew binding, press and hold — here.
- **Shown by** the pylon crystal, the charge readout on the ability strip, and the sheet's
  crew row — here.
- **Reads back as** the crystal's glow and rest state, the turret going quiet on Exhausted,
  and the placement and Overcharge log lines — here.

## Open questions

- The hold threshold in seconds, the charge per shot, `ChargeMax`, the Overcharge rate
  multiplier and the Exhausted window are all *unknown, needs play data*. Authored at the
  low end of plausible; expect to move them at `M107`.
- The crystal's float height, spin rate and bob amplitude are *unknown, needs a look*.
  Author them, then judge them on screen — no measurement can settle them.
- FORGE's passive — **open, owner Zo**, trigger the `M107` close
  (`../../Brainstorms/boarder-protocols.md`, *FORGE — the recut*).

## Verification

1. Criterion 7 — `grep -rn "MagnetHands\|Magnet Hands\|TurretPullRadius" Assets/Scripts`
   returns nothing.
2. Criteria 1, 2, 6 — EditMode tests on `TurretState`: charge rises only through
   `SpendShot`; Overcharge refuses at zero charge; charge survives a redeploy and is zero
   on a fresh deploy.
3. Criteria 4 and 5 — EditMode tests on the phase chain: `Special` ends into `Exhausted`;
   an `Exhausted` turret cannot re-enter `Deployed`.
4. Criterion 11 — `grep -rn "CreatePrimitive" Assets/Scripts/Game/Bastion/TurretObject*.cs`
   returns nothing.
5. Criterion 11, the caching half — an EditMode test asserts `PylonMesh` returns the same
   `Mesh` instance across calls, with the expected vertex and triangle counts. **This test
   is Unity Test Runner only.** `Mesh` is a UnityEngine type and `Tools/verify/run.sh`
   compiles only `Sim/`, `Util/Log.cs` and the EditMode tests it can build with `mcs`, so
   the file needs a row in the harness's `EXCLUDED` table with its reason. A file added
   there without one is coverage quietly dropped.
6. Criterion 9 — `Tools/verify/run.sh`, expect the recorded failure set and no new reds.
   The lints ahead of the compile — language, glyph, architecture, vendor and restated
   constants — are the half that runs on a machine with no `mono`, and the architecture
   lint is the one this milestone is most likely to trip: see Watch.
7. Criteria 3 and 8 — playthrough path: Play, claim FORGE at CREW QUARTERS, read the
   sheet, embark, start a Wave at the Hatch, tap the crew binding to place the turret, let
   it kill several bodies, tap again to redeploy it and confirm the charge readout does not
   reset, then hold the crew binding. Confirms the press-versus-hold split, the charge
   carrying, and Overcharge running from earned charge.
8. Criteria 12 and 13, in play — **judged on screen, not measured.** On the same
   playthrough, stand well back from a freshly placed turret and confirm the crystal is
   dark; watch it light as the machine fires; hold, and confirm the Overcharge read; then
   confirm the Exhausted turret is visibly standing, crystal down and unlit, and silent,
   then despawns without input. If the three phases are not tellable apart from across the
   room, criterion 13 is false regardless of what the code does.
9. Criterion 14 — in play, confirm the plinth swings toward what it shoots while the
   crystal keeps its own spin. In a two-seat session, confirm the guest's copy shows the
   same crystal state as the host's.

## What the recut changed

- Criteria 1-10 unchanged in wording; recorded as landed rather than deleted, because the
  gate is not closed until the play path in Verification runs.
- Criteria 11-14 added — the readout the first cut named and deferred.
- `Why it matters` now carries the #7 argument for the pylon and three rejected
  alternatives by name.
- The stale `Do not build the Outrider` guard replaced with `do not retune it here`.
- Implementation entries for landed work relabelled from `Change` to `Landed`.

## Watch

- `PlayerAbility` holds one `_cooldown` shared across weapons and divides progress by the
  active weapon's figure. The turret's charge must not be routed through it or the same
  wrong-denominator bug arrives on a second surface.
- The Coming Soon protocol stubs pass a placeholder sheet; changing the sheet strings must
  not change their shape.
- `Update` runs on every live turret including presentation copies. The crystal's spin and
  glow push must not allocate — no `new Material`, no `new Vector3[]`, no `GetComponent`
  per frame.
- `TurretLifeBar.Attach` parents to the turret root. Moving the visible body under a
  rotating plinth child must not put the life bar inside that rotation, or it will spin
  with the gun.
- **The architecture lint is the gate this milestone trips first.** `game_cap=400` lines
  with `over_cap_baseline=0`, and `TurretObject.cs` stood at 344 before the rebuild
  started. The pylon build plus the crystal drive does not fit — the file splits as a
  partial, which is this tree's existing answer (`HudController.Protocol.cs`,
  `PlayerGun.Firing.cs`). Splitting to pass a cap is only honest when each half is a
  subject; the presentation half is one.
- The same lint bans `?.`, `??`, `is null` and `ReferenceEquals` outright at baseline 0 —
  Unity's fake-null makes them wrong on a destroyed object — and bans milestone ids
  (`\bM[0-9]{2,3}\b`) anywhere under `Assets/Scripts`. A crystal-transform null guard is
  the easy place to trip the first; citing this file in a code comment is the second.
- Unity is not running while this is written, so no `.meta` is generated. A new `.cs`
  without a hand-written sibling `.meta` is invisible to the editor and the file reads as
  simply missing rather than as broken.

## Depends on / Unblocks

Depends on `M105` only for the sheet's row count, and only cosmetically — this milestone
can land first if the doctrine row is still present. Unblocks `M107`.
