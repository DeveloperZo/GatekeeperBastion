# M84 — Gatekeepers flinch when shot

<!--
Status: later
Update trigger: when the flinch clip lands, or when M81's sourcing kill
criterion fires (the clip source changes with it)
Last updated: 2026-08-18
-->

One flinch clip on a masked upper-body layer of the shared Gatekeeper
controller, triggered from the damage event the code already raises.

## Story

As a player, I want a Gatekeeper to flinch when my shot lands, so that hits
read on the target and not only on the HUD.

## Why it matters

No hit reaction exists anywhere (`Animation-Inventory.md`). The hook is
nearly free: `Damageable.Damaged` already fires and `EnemyGatekeeper`
already subscribes to it for aggro, so the animator side is one `SetTrigger`
beside an existing line. Hit feedback is exactly the class of thing graybox
evidence under-reads; the current build judges combat without it.

Decisions made here:

- **A new masked layer, not a base-layer state.** Upper-body avatar mask,
  override, weight 1, one Flinch state exiting to an empty default. The base
  layer's locomotion blend and Attack state are untouched, so a flinching
  Gatekeeper keeps walking and an attack in progress is not cancelled.
  Rejected: base-layer interrupt — it would fight the NavMesh-driven Speed
  blend and cancel attacks, turning feedback into a stun mechanic, which
  this is not.
- **Every hit retriggers the flinch.** Whether high fire rates make it
  strobe: unknown, needs play data. A retrigger cooldown is the named lever
  if it does; it is not added speculatively.
- **Clip source: the free retarget path.** Same sourcing bet as M81; M81's
  kill criterion covers this clip too, no separate one.
- **Ship intruders flinch too.** `ShipIntruder` has a `Damageable` but does
  not subscribe to `Damaged` today; it gains the same one-line subscription.

## Acceptance criteria

1. In a Run, landing a shot on a Gatekeeper shows a visible flinch at the
   moment of the hit.
2. A flinching Gatekeeper's movement is uninterrupted (it keeps walking or
   running through the flinch).
3. An attack in progress is not cancelled by a flinch (melee damage still
   lands on its normal interval while under fire).
4. All three kinds flinch, and a ship intruder flinches in the Ship fight
   (shared controller, no fork).

## Implementation

1. **Create** `Packs/Gatekeepers/Anim_Flinch.fbx` — humanoid import, avatar
   source per the M81 pattern.
2. **Change** `Packs/Gatekeepers/Gatekeeper.controller` — add a `HitReact`
   trigger and a second layer (upper-body mask, override) containing
   Empty → Flinch → Empty.
3. **Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` — in
   `OnDamaged`, `SetTrigger` the new hash, null-guarded like
   `DriveAnimator`.
4. **Change** `Assets/Scripts/Game/Ship/ShipIntruder.cs` — subscribe to
   `_damageable.Damaged` (and unsubscribe, matching EnemyGatekeeper's
   add/remove pairing) and fire the same trigger.

## Verification

1. Criteria 1 and 2 — playthrough path: from Play, start a Run, shoot a
   Gatekeeper while it crosses open ground; flinch on each hit, no movement
   hitch.
2. Criterion 3 — let one reach the Hatch, keep shooting it; hatch damage
   sparks continue on interval.
3. Criterion 4 — Ship fight to the intruder spawn; shoot it.
4. Co-op visibility of the flinch shares M85's replication question; verify
   in the same two-instance session M85 uses.

## Watch

The `Damaged` subscription in `EnemyGatekeeper` is add/remove paired against
the fake-null lifetime problem; the new trigger lives inside the existing
handler, adding no new subscription there.

## Depends on / Unblocks

Clip source follows M81's Phase 0 result. Independent otherwise.
