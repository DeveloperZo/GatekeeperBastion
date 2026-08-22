# M82 — The weapon in your hands

<!--
Status: completed
Update trigger: when the viewmodel controller or clips land, or when a weapon
gains a bespoke clip that overrides the shared set
Last updated: 2026-08-22
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
5. Animator components in `Bastion.unity` with no controller assigned
   (audit's open note; four on `Gen_*_WindowPiece` as of 2026-08-20) are
   resolved: assigned this controller or removed.

## Implementation

Landed 2026-08-20. Clips and controller are assets under `Packs/Weapons/`.
The Animator sits on Boarder's `Grip` (not the body): `PoseLocalGun` writes
Grip world TRS every LateUpdate, so the four clips key the six wrapper roots
(`Mk1`, `SecD`, `Br12`, `Mr35`, `PriC`, `Cutter`) and never the fbx child.
`WeaponViewModel` binds `_grip.GetComponent<Animator>()` before
`GetComponentInChildren`, so Fire/Reload/Speed do not hit `Boarder.controller`.

Reload: `NotifyReload` sets `animator.speed = clipLength / ActiveReloadSeconds`
(`PlayerGun.EffectiveReloadSeconds`, the same number `StartReload` writes on
the timer). LateUpdate restores `speed = 1` when `IsReloading` is false.
`NotifyFired` restores speed and `ResetTrigger(Reload)` so a mid-reload shot
does not play Fire at the scaled clock.

`WeaponView.Hold` is the per-class rest pose on Grip (PRI-C higher and flatter,
BR-12 rolled, cutter off the look axis). Shared clips stay identical; the hold
is what stops a sniper sitting in the pistol pocket. Animator lookup is Grip
only — `GetComponentInChildren` was binding `Boarder.controller`.

Criterion 5: four controller-less Animators on `Gen_*_WindowPiece` in
`Bastion.unity` were removed (imported kit avatars, not a viewmodel).

## Verification

1. Criteria 1 to 3 — playthrough on `M99` when it ships (not a full Run). Until then:
   `ViewModelAnimTests` + controller/clip assets on disk close existence. Eyes-on kick /
   reload / bob are confirmation after the range.
2. Criterion 4 — switch through all six weapons in one Run, fire and reload
   at least two of them; confirm no weapon goes static. A guid search of the
   weapon prefabs and viewmodel object should find exactly one
   `.controller` reference. `ViewModelAnimTests` pins clips, Grip lookup,
   hold, and Bastion's zero controller-less Animators.
3. Criterion 5 — scene search for Animator components with a null controller
   in `Bastion.unity`; expect zero after the change.

## Watch

`WeaponViewModel` guards every animator call with null checks today; keep
them (the Ship scene may run without a viewmodel). Do not fall back to
`GetComponentInChildren<Animator>` — that is the body.

Since 2026-08-19 each gun wrapper nests its fbx on a child that carries a corrective
rotation (barrel +Z, top +Y — see `Assets/Packs/weapon_source/README.md`). **Key the
clips on the wrapper root only.** The wrapper roots are identity
and were kept at their original fileIDs so Boarder's overrides still bind; animating the
fbx child instead overwrites the correction and lays the gun on its side. Class
silhouette is `WeaponView.Hold` on Grip, not a second set of clips.

## Depends on / Unblocks

Independent of M81 and M83. Unblocks `M93` criterion 4 (viewmodel fire/reload
motion at each weapon's rate).

## Closed, Aug 2026

Closed by Zo 2026-08-22. Shared `ViewModel.controller` + four clips on disk;
`ViewModelAnimTests`; Bastion controller-less window Animators removed. Eyes-on
that Verification routed through `M99` is accepted as first-pass play rather than
a range stamp — `M99` was still building at close.
