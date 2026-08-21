# M84 — Gatekeepers flinch when shot

<!--
Status: later (built 2026-08-21; playthrough 1–4 unstamped)
Update trigger: when criteria 1–4 are play-stamped, or when the clip is swapped
Last updated: 2026-08-21
-->

One short flinch clip on a masked upper-body additive layer of the shared
Gatekeeper controller, triggered from the damage event the code already raises.

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
  additive, weight 1, one Flinch state exiting to an empty default. The base
  layer's locomotion blend and Attack state are untouched, so a flinching
  Gatekeeper keeps walking and an attack in progress is not cancelled.
  Rejected: base-layer interrupt — it would fight the NavMesh-driven Speed
  blend and cancel attacks, turning feedback into a stun mechanic, which
  this is not.
- **Additive, not Override.** An Override layer with an empty default writes
  the avatar bind pose onto every masked bone for the rest of the run, which
  is a permanent T-pose torso on top of locomotion. Additive Empty is a
  no-op; Additive Flinch is a jolt on top of walk and Attack. Write Defaults
  off on both states, so only the keyed muscles move.
- **Every hit retriggers the flinch.** Any State → Flinch has
  `canTransitionToSelf` true (Attack's is false). Whether high fire rates
  make it strobe: unknown, needs play data. A retrigger cooldown is the
  named lever if it does; it is not added speculatively.
- **Clip is a 0.25s hand-keyed humanoid muscle `.anim`, not a Mixamo FBX.**
  Mixamo "Hit Reaction" takes are typically a second or more and include a
  weight shift; that duration fights "not a stun" and "every hit retriggers".
  M81 already accepted the free Mixamo path on the locomotion and Attack
  clips; this clip's job is a short snap. Path follows the pack convention
  (`Animations/`, lowercase). Rejected: Mixamo Hit Reaction FBX — wrong
  length for the mechanic.
- **Ship intruders flinch too.** `ShipIntruder` has a `Damageable` but does
  not subscribe to `Damaged` today; it gains the same subscription. A
  disabled `EnemyGatekeeper` on that prefab still has its Awake subscription
  live, so the explicit ShipIntruder hook is the one that survives if the
  brain is later removed; a double `SetTrigger` is harmless.
- **Thieves flinch.** `SetTrigger` runs in `OnDamaged` before `Provoke`, so
  a kind that refuses aggro still reads the hit.
- **Generator authors the layer.** `EnsureGatekeeperController` early-returns
  when the file exists; without a follow-up pass the HitReact layer dies on
  regenerate (the M81 Attack-clip lesson). `EnsureHitReactLayer` runs on
  both the create path and the existing-file path, and is a no-op when the
  layer is already present.

## Acceptance criteria

1. In a Run, landing a shot on a Gatekeeper shows a visible flinch at the
   moment of the hit.
2. A flinching Gatekeeper's movement is uninterrupted (it keeps walking or
   running through the flinch).
3. An attack in progress is not cancelled by a flinch (melee damage still
   lands on its normal interval while under fire).
4. Every kind that uses the shared controller flinches, and a ship
   intruder flinches in the Ship fight (shared controller, no fork).
5. `Packs/Gatekeepers/Gatekeeper.controller` has a `HitReact` trigger and a
   second layer named `HitReact` whose Flinch state's motion guid matches
   `Anim_flinch.anim`'s guid; Base Layer's Attack motion and the locomotion
   blend tree are unchanged.

## Implementation

1. **Create** `Packs/Gatekeepers/Animations/Anim_flinch.anim` — humanoid
   muscle clip, 0.25s, peak at 0.06s, `loopTime` 0. Keyed muscles: Spine
   Front-Back, Chest Front-Back, Head Nod Down-Up, Left/Right Arm Down-Up.
   Authored by `SceneBuilder.EnsureHitReactLayer` when the file is missing,
   then committed. Not an FBX.
2. **Create** `Packs/Gatekeepers/UpperBody.mask` — humanoid AvatarMask with
   Body, Head, LeftArm, RightArm, LeftFingers, RightFingers on; Root, legs
   and IK off. Same author.
3. **Change** `Packs/Gatekeepers/Gatekeeper.controller` — add a `HitReact`
   trigger and a second layer (that mask, Additive, weight 1) containing
   Empty (default, no motion) → Flinch → Empty. Any State → Flinch on
   `HitReact`, duration 0.05, no exit time, `canTransitionToSelf` true.
   Flinch → Empty exit time 0.85, duration 0.08. Write Defaults off on
   both states. Do not touch Base Layer.
4. **Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs` —
   `EnsureHitReactLayer` creates the clip and mask if missing, adds the
   parameter and layer if missing, and is called from both branches of
   `EnsureGatekeeperController`.
5. **Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` — add
   `AnimHitReact` hash next to `AnimAttack`.
6. **Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` —
   in `OnDamaged`, `SetTrigger` the new hash, null-guarded like
   `DriveAnimator`, before `Provoke`.
7. **Change** `Assets/Scripts/Game/Ship/ShipIntruder.cs` — subscribe to
   `_damageable.Damaged` (and unsubscribe, matching EnemyGatekeeper's
   add/remove pairing) and fire the same trigger.

## Verification

1. Criterion 5 — from the two files, no editor:

```bash
rg -n "m_Name: HitReact" "Assets/Packs/Gatekeepers/Gatekeeper.controller"
rg -n "^guid:" "Assets/Packs/Gatekeepers/Animations/Anim_flinch.anim.meta"
```

   The Flinch state's `m_Motion` guid equals the clip meta's `guid`. Attack
   still references `03ede1e3f3df7b14fbb44ae773ed0c02`. A controller that
   had not done the work has one layer and no `HitReact` parameter.
2. Criteria 1 and 2 — playthrough path: from Play, start a Run, shoot a
   Gatekeeper while it crosses open ground; flinch on each hit, no movement
   hitch.
3. Criterion 3 — let one reach the Hatch, keep shooting it; hatch damage
   sparks continue on interval.
4. Criterion 4 — shoot a Default, a Hunter and a Thief in the Bastion;
   Ship fight to the intruder spawn and shoot it.
5. Co-op visibility of the flinch shares M85's replication question; verify
   in the same two-instance session M85 uses.

### What has landed

**Landed 2026-08-21 (agent).** Clip, mask, layer and both code hooks are on disk.
Criterion 5 reads clean in the files. Criteria 1–4 are structurally in place and
unproven — they need the playthrough below.

`Anim_flinch.anim` guid `0cd7751cb3144b74fb47045e6061905f`; Flinch `m_Motion`
fileID `7400000` matches the clip meta. `UpperBody.mask` guid
`ea4cad77548780048875dbc14ba4b83e` is the HitReact layer's mask. Attack still
references `03ede1e3f3df7b14fbb44ae773ed0c02`. Layer blending is Additive
(`m_BlendingMode: 1`), weight 1, Write Defaults off on Empty and Flinch,
Any State → Flinch `m_CanTransitionToSelf: 1`.

`EnsureHitReactLayer` is a no-op when the layer is already present, so a later
Build All does not dirty the controller.

## Watch

The `Damaged` subscription in `EnemyGatekeeper` is add/remove paired against
the fake-null lifetime problem; the new trigger lives inside the existing
handler, adding no new subscription there. `ShipIntruder` is a new pair and
must match.

`EnsureGatekeeperController` returning the existing asset without a second
pass is how M81's Attack clip would have reverted; the HitReact layer uses
the same trap.

## Depends on / Unblocks

Independent. M81's Mixamo path is accepted and is not this clip's source.
