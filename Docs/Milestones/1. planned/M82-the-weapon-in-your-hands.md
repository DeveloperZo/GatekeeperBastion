# M82 — The weapon in your hands

<!--
Status: later
Update trigger: when the viewmodel controller or clips land, or when a weapon
gains a bespoke clip that overrides the shared set
Last updated: 2026-08-19
-->

One shared viewmodel animator — idle, move, fire, reload — hand-keyed in
Unity, serving all six weapons, making the Fire/Reload/Speed parameters
`WeaponViewModel` already drives do something.

## Story

As a player, I want my weapon to kick when I fire and move while I reload, so
that shots and reloads read without watching the HUD.

## Why it matters

`WeaponViewModel.cs` drives Fire, Reload and Speed on an Animator, but no
weapon controller exists anywhere in the project; the code path is a no-op
(audit, verified 2026-08-18). The shared path was ruled by Zo 2026-08-18.

**Scope note (Zo, 2026-08-18): no-arms is alpha scoping, not doctrine.** An arms
mesh is expected eventually. The replacement seam is the Animator parameter
contract (Fire, Reload, Speed) — `WeaponViewModel` never learns what is under
the Animator, so a future arms rig replaces the viewmodel subtree, its clips
and its controller wholesale with zero code change. When that day comes the
source is an FPS arms pack (rigged arms + per-class clips, own guns socketed
into its hands), which passes the purchase gate on volume; Mixamo stays
irrelevant to first person. Until then this milestone's four clips are the
placeholder, and discarding them later is the plan, not a loss.

On the buy-versus-build test this is **build**: there is no arms mesh, so
these clips animate weapon and viewmodel-root transforms — simple position and
rotation keys, authored in the Unity Animation window. Nothing to retarget,
and no store pack fits a game-specific viewmodel framing. These become the
project's first gameplay `.anim` assets.

Decisions made here:

- **Four clips**, animating viewmodel transforms: `ViewModel_Idle`,
  `ViewModel_Move`, `ViewModel_Fire`, `ViewModel_Reload`.
- **One controller**, `Packs/Weapons/ViewModel.controller` — parameters
  `Speed` (float), `Fire` (trigger), `Reload` (trigger), matching
  `WeaponViewModel`'s existing hashes exactly. Idle/Move blend on Speed
  (code already sends 0.5 walking, 1.0 sprinting); Fire and Reload as
  interrupting states returning to the blend.
- **Movement bob is clip-driven** through the Speed blend. Rejected:
  code-driven bob — the parameter and pipeline already exist, and one motion
  source is easier to keep coherent.
- **One reload clip, speed-scaled.** Weapons reload at different durations
  (`PlayerGun.Reload` owns the timer). The clip plays at
  `clipLength / reloadDuration` via the animator speed multiplier rather than
  authoring six reload clips. Rejected: per-weapon clips — six times the
  authoring for motion nobody has seen once yet.
- **One Animator on the viewmodel root** that `WeaponViewModel` finds
  (serialized field or `GetComponentInChildren`, both already in the code).
  Switching weapons must not swap controllers.

## Acceptance criteria

1. In a Run, firing any of the six weapons shows a visible kick on the
   viewmodel on each shot.
2. Reloading any weapon shows continuous motion that ends when the reload
   ends, including weapons with different reload durations.
3. Standing, walking and sprinting produce visibly different viewmodel
   motion.
4. Exactly one controller asset serves all six weapons; switching weapons
   mid-Run keeps criteria 1 to 3 true.
5. The two Animator components in `Bastion.unity` with no controller assigned
   (audit's open note) are resolved: assigned this controller or removed.

## Implementation

Editor and clip work plus one small code change; Zo executes, in order:

1. **Create** `Packs/Weapons/ViewModel.controller` with the parameters and
   states above.
2. **Create** the four `.anim` clips in `Packs/Weapons/`, keyed on the
   viewmodel root and weapon child transforms. Start crude: fire is a short
   back-kick and recover; reload is a lower-tilt-raise; move is a small loop.
3. **Change** the viewmodel object (scene or prefab): ensure one Animator on
   the root `WeaponViewModel` resolves, controller assigned.
4. **Change** `Assets/Scripts/Game/Player/WeaponViewModel.cs` — on reload
   start, set the animator speed to `clipLength / reloadDuration` for the
   Reload state (read the duration from the same source `PlayerGun.Reload`
   uses); restore to 1 when it completes. This is the only code in the
   milestone.
5. Resolve the two controller-less Animators in `Bastion.unity` (criterion 5).

## Verification

1. Criteria 1 to 3 — playthrough path: from Play, start a Run; fire the MK-1,
   then a slower-reloading weapon; reload each; sprint and stop while
   watching the viewmodel. Each transition should be visible on screen.
2. Criterion 4 — switch through all six weapons in one Run, fire and reload
   at least two of them; confirm no weapon goes static. A guid search of the
   weapon prefabs and viewmodel object should find exactly one
   `.controller` reference.
3. Criterion 5 — scene search for Animator components with a null controller
   in `Bastion.unity`; expect zero after the change.

## Watch

`WeaponViewModel` guards every animator call with null checks today; keep
them (the Ship scene may run without a viewmodel).

Since 2026-08-19 each gun wrapper nests its fbx on a child that carries a corrective
rotation (barrel +Z, top +Y — see `Assets/Packs/weapon_source/README.md`). **Key the
clips on the wrapper root and the viewmodel root only.** The wrapper roots are identity
and were kept at their original fileIDs so Boarder's overrides still bind; animating the
fbx child instead overwrites the correction and lays the gun on its side.

## Depends on / Unblocks

Independent of M81 and M83. Unblocks nothing structurally; first-person feel
work later builds on these clips.
