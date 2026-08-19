# M87 — The Ship, from above

<!--
Status: later
Update trigger: when camera rotation, floor count, or the wall render-state rule changes
Last updated: 2026-08-17
-->

An isometric camera and unit targeting, **on the Ship only**.

**This is a probe, not a conversion.** The Ship has no combat, so it tests the camera, the
body, movement, interaction and the wall handling without also having to solve aiming, crits,
enemy readability or crowd rendering. If it reads better, the Bastion follows in its own slice.
If it does not, the Ship reverts and the perspective question is settled for the price of one
scene.

Single floor. Locked camera. The first-person path stays intact for the duration.

## Story

As the builder, I want the cheapest honest test of a paradigm change, so the decision rests on
playing it rather than on arguing about it.

## Why it matters

**Perspective was inherited rather than chosen.** `../../Design/1-Experience.md` names Guns 'n
Goblins as the Ancestor, and that game is a first-person FPS with building, so the camera came
across with the rest of the inspiration and was never put to a decision. Zo raised it 2026-08-14
at the last moment it is cheap: `M61-2` dresses the kit and `M66` authors hit feedback, and both
assume a camera.

**The objection that would have killed it does not hold.** Concealment in this game is layer
membership, not line of sight: a stealthed Cloaker is not drawn to you at all unless you share
its layer, and that is true from any angle. Spawns are a room you are not in. Neither depends on
a narrow view.

**What the wider camera buys is the thing M47 exists to create.** The bastion's premise is that
positions differ and no seat sees everything, but from inside your own eyes a player can never
compare two positions, only occupy one. The emplacement decision is currently made half blind. A
twelve-kind roster (`M63`) also becomes far more achievable when a fixed camera angle makes
silhouette design work.

**And half of `M61` transfers regardless.** The Boarder body, the animator and the six weapon
prefabs were built so remote clients had something to look at. Under this camera, that is what
everyone looks at.

Leverage tier (`../../Working-Rules.md`): **#1, paradigm.** Nothing else in the project sits
this high, which is why it gets a probe rather than a decision memo.

## Kill criterion

**Stamped at start.** After close, walk the Ship and operate all four stations. **If it is not
at least as clear and as pleasant as first person, the camera is wrong for this game**: the Ship
reverts, the Bastion conversion never starts, and `Decisions.md` records first person as chosen
rather than inherited. An ambiguous result is a fail, not a reason to keep both cameras.

## Acceptance criteria

### The camera

1. **Isometric, fixed angle, rotation locked.** The consequence is stated rather than
   discovered: with a locked camera, camera-facing is a constant, so wall handling is a bake
   decision. **If rotation is ever wanted, all four wall sides must be emitted and the toggle
   becomes a runtime system.** Deciding that later costs a re-emit.
2. **Single floor.** No storey culling, no ramp hysteresis, none of the multi-floor machinery.
   That arrives with the Bastion or not at all.
3. **The camera follows the player with a stated dead zone.** Numbers into
   `../../Design/4-Data.md` generated, not hand-written (`M33`).

### The player

4. **The Boarder body is the player and is visible**, using the existing animator and its
   Idle / Walk / Run clips. No new animation work in this slice.
5. **Targeting is unit-level.** Aim follows the cursor and resolves to a body, not to a point on
   a body. Crit stays exactly as it is today, the roll in `PlayerGun.Firing` against Luck.
   Nothing new is built for precision.
6. **The first-person path is not deleted.** The Bastion still runs first person for the
   duration of the probe. **The mixed state never ships**; it exists only so the probe is
   reversible in one commit.

### The walls

7. **Camera-facing sides render full, half or none, and half is a piece rather than a scaled
   full wall.** Scaling a Synty wall to half height squashes its trim, panel lines and vents, so
   it reads as a compressed full wall. Check the pack for a low wall or railing first. If two
   pieces are stacked, a trim strip covers the join so the seam is a detail rather than a
   defect.
8. **Only the sides that toggle are split.** With rotation locked that is two sides; the far two
   stay single full pieces. Doubling renderer count across every wall would fight the batching
   criteria `M61-2` depends on.
9. **Collision and NavMesh stay full height regardless of what renders.** The obvious
   implementation disables the top-half GameObject and takes its collider with it, and then
   players shoot over a wall that is solid. **Asserted by a test that raycasts a camera-facing
   wall at full height with its top half hidden and expects a hit.**
10. **Wall height is a render state, not a side kind.** `M47`'s Wall / Door / Window / None are
    untouched, and Window still means sight passes and walk does not. Side kind is what a face
    does; wall height is how it is drawn from this camera.

### Nothing regresses

11. **All four stations open and are operable, and EMBARK works.** `ShipSceneContractTests`
    still reaches every console.
12. **Ship HUD hosts stay readable at the new framing** and do not overlap
    (`../../Decisions.md` host list).
13. **Co-op holds.** A client sees the host's Boarder and the reverse, and both see the same
    wall render states.
14. **Suites green, same names plus the wall raycast test.** No new counter, no accumulator, no
    stats object. **No new menu row without asking** (`../../Architecture/README.md`).

## Implementation

**Create** the isometric rig and follow behaviour, scoped to the Ship scene so the Bastion is
untouched.

**Change** the Ship generator to emit camera-facing walls as two pieces and far walls as one,
and to tag which group is which.

**Create** one small component that owns the render state per wall group. Renderers only; it
never touches colliders.

**Do not touch** `M61`'s view-model, crosshair or FOV zoom. They stay working on the Bastion.

Rejected: **converting the Bastion here.** That is the expensive half and this exists to decide
whether to pay for it.

Rejected: **camera rotation and multi-floor.** Both are real systems and neither is needed to
answer the question.

Rejected: **deleting the first-person path.** A probe you cannot reverse is a conversion.

Rejected: **transparency or dither cutaways for the near walls.** Both look better and both
break static batching, which `M61-2` relies on to keep draw calls down.

Rejected: **a fifth side kind.** See criterion 10.

## Verification

1. Criteria 1 to 6 — play the Ship. Walk it, look at it, use it.
2. Criterion 9 — the raycast test, with the top half hidden. This is the one that will be
   implemented wrong.
3. Criteria 11 to 13 — all four stations, EMBARK, then a two-instance session.
4. **The kill criterion last, and honestly.** Compare against a first-person Ship run from the
   same day, not from memory.

## Watch

- **Two cameras in one build is a temporary state with a stated end.** If the result is
  ambiguous, that is the fail condition, not an argument for shipping both.
- **Half a wall reads as enclosure; no wall reads as a floating platform.** The Ship already
  demonstrated the second one on 2026-08-14.
- **The real bill arrives with the Bastion, and it is animation rather than camera.** Strafe,
  backpedal and an aim offset so the upper body tracks where you point, plus fire and reload
  clips that are actually distinct. Today `Anim_FiringRifle`, `Anim_Reloading` and
  `Anim_RifleIdle` are byte-identical to `Anim_PlayerIdle`, and `Anim_Walking` to
  `Anim_PlayerRun`. Price that before the Bastion slice, not during it.
- **The boarding-a-held-vessel fantasy softens under this camera** (`../../Design/1-Experience.md`
  §1.2). Decide whether that is load-bearing or decorative while playing the probe, because it
  is the only thing left on the first-person side that is not a cost.

## Depends on / Unblocks

**Depends on** the generated Ship landing first, since criterion 7 is an emitter change.

**Unblocks the Bastion conversion, or cancels it.** Either outcome is worth the slice, which is
the point of running it on the cheap scene.
