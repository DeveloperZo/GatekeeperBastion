# M113 — Hands that hold the gun

<!--
Status: completed, Sep 2026 (closed by Zo 2026-09-02: the capability is in; tuning for immersion is owed and listed under Closed)
Update trigger: when the arms mesh is cut, when a weapon needs a per-class arms offset,
or when WeaponViewModel's Fire/Reload/Speed contract changes
Last updated: 2026-09-01 (recut: arms as a prop on the animated wrapper root, cut from
SK_Tactical — no FPS arms pack, no pack purchase, no clip or controller authoring.
Reviewed the same day; six factual claims and three dangling decisions corrected below.)
-->

A rigid forearms-and-hands prop rides the weapon wrapper root the `M82` clips already
animate, so the gun that kicks, bobs and dips is held rather than floating. Cut from the
Boarder's own mesh. No pack is bought and no clip is authored.

## Story

As a player, I want to see hands holding the gun I am firing, so that shots and reloads
read as a body doing something instead of a mesh floating in the lower-right of the screen.

## Why it matters

This is Zo's priority 1 of six named 2026-08-30
([`Advice.md`](../../GatekeeperZo%20Notes/Advice.md)): first-person arms + gun in sockets,
so fire/reload/move read as a body. `WeaponView.cs:32` says the same from the numbers side:
the shared hip pocket is "a floating gun until arms exist."

Leverage tier (`../../Working-Rules.md:266`): **#6, information flows.** No Sim number
moves, no weapon stat changes, no new player verb — what changes is what the screen tells
the player about their own body. A lower tier is wrong because retuning the camera-space
offset is what `Advice.md` rules out by name: "Not a viewmodel-bob retune."

### Why this is not the FPS arms pack `M82` predicted

`M82` named an FPS arms pack as the successor. **That path is closed** (Zo, 2026-09-01: no
available pack clears the bar), and two measurements say the pack was carrying more weight
than it looked:

- `md5sum Assets/Packs/Boarder/Anim_*.fbx` — **six files, two distinct hashes.**
  `Anim_FiringRifle`, `Anim_Reloading`, `Anim_RifleIdle` and `Anim_PlayerIdle` are one
  file; `Anim_PlayerRun` and `Anim_Walking` are another. Each `.md` sidecar records it.
  The Boarder skeleton has an idle and a walk; its fire and reload are the idle renamed.
- `BoarderPresentation.cs:33-34` — the body is **one SkinnedMeshRenderer**
  (`Boarder.prefab` holds exactly one, and zero MeshRenderers), which is why the local
  camera masks the whole body rather than the head. There is no arms geometry to reveal and
  no way to reveal part of one renderer.

So a pack would have been the project's first real weapon motion *and* its first arms
geometry *and* a second rig, bought at once. This recut buys none of that: `M82`'s four
clips are the motion, already shipped and already playing.

**The body is `SK_Tactical.fbx` from `Assets/FaxLab3D/SciFiRobotCharactersPack`** (resolved
from `Boarder.prefab`'s mesh guid `2ff3eb3b95f647542b8516abfb5daaa5`), a robot. Armoured
forearms do not need to deform, so a rigid prop is a defensible look here rather than a
compromise. The comment at `BoarderPresentation.cs:33-34` calls it "this astronaut" and is
stale; correct it in this change.

### What this milestone pays off

**TH-24** (`../../Hygiene/1. open/HP-5-the-borrowed-body-holds-up.md`) owns the retirement
of `WeaponView.RightHandBone` and the unread `WeaponViewModel._rightHand`. M113 pays it —
see the Change section — so `HP-5` strikes TH-24 when this closes. **TH-21** (the remote
avatar's gun sits wrong because `Grip`'s offsets were tuned for the mixamo hand) is
adjacent and **stays open**: criterion 7 keeps the arms off remote avatars entirely, so
this milestone neither fixes nor worsens it.

## Acceptance criteria

1. **Structural.** `WeaponViewModel` parents the arms under the wrapper root returned by
   `MeshFor(id)` and never under `_grip`, asserted off source. Whenever a weapon mesh is
   shown the arms renderer is active; whenever every mesh is hidden it is inactive. Both
   states are set at the one choke point both paths already share.
2. **Structural.** The arms are a child of the transform the four `M82` clips key, so any
   translation or rotation those clips apply to the weapon applies identically to the arms.
   No clip, and no curve in any clip, is edited by this milestone —
   `git diff --stat Assets/Packs/Weapons/` shows zero changes to the four `.anim` files and
   to `ViewModel.controller`.
3. **Eyes-on, adjudicated by Zo, twelve binary rows.** For each of the six weapon meshes
   (`Mk1`, `SecD`, `Br12`, `Mr35`, `PriC`, `Cutter`) at each of two stances (hip, zoom):
   no hand polygon visibly intersects the weapon body, and no gap is visible between palm
   and grip. Each row is pass or fail; the criterion is twelve passes. A failed row names
   the weapon and gets a per-class `ArmsOffset` branch. **Two weapons are expected to fail
   and are not regressions:** the `Cutter`, whose hold rolls 50° off the look axis, and
   `SecD` — see the Watch note on wrapper scale.
4. In a Run, switching weapons with `1`/`2`/`3` moves the same single arms instance to the
   new weapon. `Boarder.prefab` contains exactly one `Arms.prefab` instance, asserted off
   disk; criteria 1–3 hold for the weapon switched to.
5. `WeaponViewModel.cs`'s public surface is unchanged: `Show(WeaponId)`, `NotifyFired()`
   and `NotifyReload()` keep their signatures. The two with callers outside the file —
   `NotifyFired` (`PlayerGun.Firing.cs:63`) and `NotifyReload` (`PlayerGun.Reload.cs:93`) —
   are called identically before and after. This is the criterion that proves the `M82`
   seam held.
6. `Packs/Weapons/ViewModel.controller` and the four `ViewModel_*.anim` clips still exist
   and still drive the motion — **this recut keeps them.** All **twelve** existing
   `ViewModelAnimTests` methods pass under their existing names, unmodified.
7. A remote player's avatar in co-op shows no arms prop. `NetPlayerAvatar.prefab` instances
   `Boarder.prefab` (guid `77e11a228ac13f74cab7a156a2902f2c`) but carries no
   `WeaponViewModel` — that component is authored on the Player in `Bastion_Coop.unity`,
   `Range.unity` and `Ship - CoOp.unity`, not on `Boarder.prefab`. The arms are authored
   inactive and only `WeaponViewModel` activates them.

## Implementation

### The shape, and why

`PoseLocalGun` writes `Grip`'s world TRS every `LateUpdate`, and the four clips key `Grip`'s
six **children** — the wrapper roots `Mk1`, `SecD`, `Br12`, `Mr35`, `PriC`, `Cutter`. Each
clip carries exactly one `m_EulerCurves` and one `m_PositionCurves` entry per root
(`ViewModel_Fire.anim`, `path:` at lines 50/84/118/152/186/220 and 255/289/323/357/391/425;
`m_ScaleCurves` is empty).

**Arms parent to the wrapper root that is currently shown, not to `Grip`.** A child of
`Grip` would sit still while the gun kicks out of its hands, because `Grip` is code-posed
and the animated transform is one level down. `WeaponViewModel` already resolves and enables
that root, so the reparent costs one statement in a path that already runs on every weapon
switch — and criteria 2 and 4 come free rather than being wired.

Rejected: **adding an `Arms` path to the four clips.** It works, but it is four clip edits
in the Animation window, it duplicates six curve pairs seven ways, and the hashed bindings
in each `.anim` make hand-editing the YAML a silent-corruption risk. Reparenting needs no
clip touched, which is what makes criterion 2's `git diff --stat` assertion possible.

Rejected: **one arms copy per wrapper root.** Six meshes to keep in sync, and criterion 4
says one instance.

Rejected: **skinned arms on the Boarder skeleton.** `BoarderPresentation.Awake` moves every
`SkinnedMeshRenderer` under the player onto `LocalHead` and masks that layer off the local
camera (`BoarderPresentation.cs:36-46`) — skinned arms would cull themselves on the frame
they were added. A `MeshRenderer` is invisible to that loop. This is the second reason for
rigid, after fewer moving parts.

### Create

**`Assets/Packs/Boarder/arms_source/README.md`** — source record following the
`Packs/weapon_source/README.md` precedent: which mesh was cut from what, the import scale,
and what is tracked versus restored. **`SK_Tactical.fbx` is not in git.**
`git check-ignore -v` resolves it to `.gitignore:149`
(`/Assets/FaxLab3D/SciFiRobotCharactersPack/*`); only `Materials/` and `Textures/*.meta` are
negated, which is `Decisions.md:107-109` narrowing the pack to 102 tracked files and holding
the models out. So the README **must** name the restore step — the pack re-imported from its
store archive — and state plainly that a clean clone cannot re-cut the arms without it. The
cut mesh itself is the tracked artifact.

**`Assets/Packs/Boarder/Arms.asset`** — the **right** forearm and hand cut from
`SK_Tactical.fbx` **in Unity, not Blender** (none installed 2026-09-01):
`Assets/Scripts/Editor/Tools/ArmsCutter.cs` bakes an instance with `SkinnedMeshRenderer.BakeMesh`,
keeps the triangles weighted to the right forearm/hand/digit bones, moves them into the rig's
world frame (Y-up, metres) and re-pivots on the `hand.r` bone. The first bake shipped the
pivot at the feet in Z-up space, 1.12 m off — an invisible prop; the pivot rule exists
because of that.

**The pose is authored on the rig, in the scene, with the gun in its hands** (Zo,
2026-09-01 evening), and the bake is expressed in that gun's wrapper-root frame, so the prop
reproduces the posed grip at identity under any wrapper root. Both arms bake as posed into
one mesh. Rejected after trying it: a right-arm bake mirrored for the left with two rigid
pieces tuned by TRS — it worked for the MK-1 hip row but the pose it gives is what one
untrained pair of hands can be nudged into, and a rig posed by eye against the gun is better
in every row. That route stays in the cutter behind a toggle. Two-handed because all six weapons share one `Hold` pocket today, so one stance is
consistent across the set by construction. The known exception is the `Cutter`, whose hold
rolls 50° off the look axis — its criterion 3 rows are the ones expected to fail and force
the first per-class offset.

**`Assets/Packs/Boarder/Arms.prefab`** — root `Arms` **authored active** so it previews in
the Project window and can be dropped in a scene to look at; `Hand.R` carries the mesh at
identity on a `MeshFilter` + `MeshRenderer` with `BoarderBot.mat` (guid
`17a8545fba269054bb6238abb2902025`) so the hands match the suit by construction. **Criterion
7 is the `m_IsActive: 0` override on the nested instance in `Boarder.prefab`**, not the
prefab root — a root authored inactive gave a blank thumbnail and an "empty" prefab to
anyone inspecting it. `ArmsOffset` on the root is the per-weapon deviation from the baked
pose — zero shared. `WeaponViewModel` moves the root only.

**The pose is in the mesh, not in any transform.** Zo posed the rig one-handed on
2026-09-01 — the gun in the **left** hand — and the cutter bakes in the gun's frame, so
`Hand.R` (the name is the prefab slot, not the hand) sits at identity and `ArmsOffset` at
zero. The cutter follows the gun: the gripping hand is whichever of `hand.l` / `hand.r` is
closest to the posing gun, and the one-hand modes bake that side. An earlier TRS-tuned
two-hand pose (right hand at (0, −0.05, −0.05) / (0, 90, −120), left at (−0.04, −0.07,
0.02)) was superseded by this and is recorded only so the numbers are not re-derived.
Bake log, 2026-09-01: `GunHandOnly (gun hand.l), frame: raw fbx 'SM_Pistol_Energy' with
the Mk1 wrapper correction undone at 0.17 m from hand.l`; mesh centre 23 cm behind the gun
origin. **The Boarder is left-handed** — that bake is the intent, not an accident, and the
camera pocket was mirrored to match on 2026-09-02 (see `WeaponView.cs` below).

**Pose against the wrapper, never the pack fbx.** The first left-hand bakes were posed on
`shooterweaponspackassets/fbx/SM_Pistol_Energy.fbx` (import scale 1); the wrapper nests
`weapon_source/MK-1.fbx` (import scale 100). Same model, different import, and the bake
came out rolled — palm up in play. The cutter prefers an `Mk1.prefab` instance within
reach and warns by asset path when a raw fbx is not the wrapper's own; the shipped bake
(2026-09-02) was posed on an `Mk1.prefab` instance in the rig's left hand.

**Criterion 3 status, 2026-09-02 (Zo):** `Mk1` hip — reads as held; "great improvement",
**polish owed**: the thumb does not wrap the grip convincingly. `Mr35` hip — reads as held
two-handed in play (left hand on the grip, right on the handguard; the class child switches
with the weapon, verified by the arms root re-parenting to the `Mr35` wrapper and `Rifle`
being the only active child). **Zo's eyes-on on that row (2026-09-02):** "center the arms more than have it sit in
the left pocket", and the right forearm's elbow cut was visible in mid-air. Both are
`Hold`, not the arms: `M82` shipped one pocket for all six weapons, and a long gun with its
stock at the sidearm's pocket origin sits low-left with its cut ends in frame. **Second
pocket added:** `WeaponView.LongGunHip()` for the rifle and shotgun classes — Right −0.05,
Down −0.17, Forward 0.20, no yaw or roll — nearer the centreline and pulled 18 cm toward
the camera so the elbow cuts fall below the frame edge (0.26 still showed the right
forearm's cut; 0.20 is the first value at which both forearms leave the frame, Zo's
eyes-on 2026-09-02). Tier #12, one lever, and the
sidearm pocket did not move. `Hold_PocketFollowsArmsClass` replaces the test that pinned
every shop gun to the MK-1 pocket; `Hold_LongGunsSitNearerCentreAndCloser` pins the
relation rather than the numbers. In play the MR-35 sits on the centreline with the muzzle
at the crosshair and both forearms leaving the frame at the bottom edges. That is a re-pose of the
thumb digits on the rig and a re-bake, not a code change, and it is the same pass that
walks the other eleven rows.

### Change

**`Assets/Scripts/Sim/Run/WeaponView.cs`**

- **Add** `ArmsOffset(WeaponId id, bool zoom = false)` returning `GunHold`, mirroring
  `Hold`'s signature exactly — `zoom` is in the signature because criterion 3 checks two
  stances, and a version without it cannot express the requirement. The returned TRS is
  local **to the wrapper root**, not to the camera. Its `switch` mirrors `MeshFor`'s shape:
  `Mk1Sidearm`, `SecD`, `Br12Shotgun`, `Mr35Rifle`, `PriC`, and a `default` covering
  `None`, `Vk7Rifle` and `ThrowawayComposite` — the cutter mesh. **Note there is no
  `WeaponId.Cutter`;** `WeaponId` has eight members (`WeaponId.cs:9-22`) and the cutter is
  `None`. Six meshes, eight ids — do not conflate them.
- **Open decision, owner Zo, trigger step 4's play pass:** the six floats behind
  `ArmsOffset` ship at zero and are set by play. Zero is a deliberate placeholder, not a
  guess dressed as a value.
- **Delete** `RightHandBone` — this pays **TH-24**. `mixamorig:RightHand` matches no bone
  since the Boarder redress, and leaving a Mixamo bone name in the file this milestone puts
  arms on is exactly the wrong thing for the next reader to find.
- `LocalGunRight` / `LocalGunDown` / `LocalGunForward`: **the pocket is mirrored to the
  left** (2026-09-02). Zo posed and keeps the gun in the Boarder's **left** hand, and a
  left hand cannot read in the right-hand pocket `M82` shipped, so `LocalGunRight` is
  −0.18 and the hip hold's yaw and roll flip sign with it (2 → −2, 10 → −10); the cutter's
  `None` hold mirrors the same way (0.22 → −0.22, −10 → 10, 50 → −50) so the melee hand is
  the same hand. One lever — a reflection across the camera's vertical plane — and nothing
  else in the hold moved. Tier #12 constant, and the only tier that can express which
  hand a fixed prop belongs to. The summary's stale "floating gun until arms exist" clause
  went in the same edit. `WeaponViewTests` bounds (`< 0.5`, `> −0.5`) and the
  Hold-returns-the-constant equality hold on the negative value unchanged.
- `Hold`'s `zoom` summary says "unused until an arms mesh can ADS" — correct that clause.

**`Assets/Scripts/Game/Player/WeaponViewModel.cs`**

- **Add** `[SerializeField] GameObject _arms;`, resolved in `Awake` by `FindGo("Arms")` when
  null, matching the six existing mesh fields — all of which serialize as `{fileID: 0}`, so
  runtime resolution is the established pattern here, not a new one.
- **Change** `EnableOnly(GameObject keep)` to own the arms **state**: `keep == null`
  deactivates them, otherwise it activates them and parents them under `keep.transform`. It
  is the **single choke point both paths already share** — `Show` and `ShowHidden` are its
  only callers — so activation and deactivation cannot drift apart. The signature does not
  change; it needs no `WeaponId`, because the offset is not written here.
  **Do not** activate the arms in `Awake` and deactivate them in `ShowHidden`: `Awake` runs
  once, so a Drone-slot or unowned episode would turn the arms off for the rest of the
  session and criterion 1 would fail on the way back.
- **Add** `PoseArms()`, called from `LateUpdate` beside `PoseLocalGun`, writing
  `WeaponView.ArmsOffset(_shown, _fpc.IsZooming)` as the arms' local TRS **every frame**.
  This split — state in `EnableOnly`, pose in `PoseArms` — is load-bearing and was a defect
  in this milestone's first draft. `EnableOnly` re-runs only when the shown weapon changes
  (`Show` guards on `id != _shown || _hidden`), so sampling `IsZooming` there reads the
  stance once at switch time and never again: the zoom pose could never differ from the hip
  pose, and criterion 3's six zoom rows could not pass. `PoseLocalGun` already writes `Grip`
  per frame, so this mirrors the file's existing shape rather than adding one.
- **Delete** the `_rightHand` `[SerializeField]` and its `Awake` resolution — the other half
  of TH-24. `grep -rn "_rightHand" Assets/Scripts/ Assets/Tests/` returns three hits: the
  declaration, a null check, and the assignment. Nothing consumes the value.
- **Correct** `Show`'s doc comment, which claims it is called "from the assemble bind".
  `grep` finds no caller outside this file; it is reached only from `ApplyShown`.

**`Assets/Scripts/Game/Player/BoarderPresentation.cs`** — correct the stale "This astronaut"
comment at lines 33-34 to name `SK_Tactical`. **Nothing else changes**, and that is
load-bearing: `CollectBodyRenderers()` sweeps every `Renderer` under the player except the
root (lines 69-88) and runs whenever `_bodyRenderers` is empty, which it is in all three
scenes — so the arms are hidden with the gun when a Ship station panel opens, for free. Do
not add the arms to a list; do not special-case them.

**`Assets/Packs/Boarder/Boarder.prefab`** — add the `Arms.prefab` instance under `Grip`.
It inherits `m_IsActive: 0` from the source prefab and writes no override;
`EnableOnly` reparents and activates it on the first frame a weapon shows.

### Tests — `Assets/Tests/EditMode/Run/ViewModelAnimTests.cs`

All **twelve** existing tests keep their names and stay green; no clip and no controller is
touched. `ViewModelFindsGripAnimator_NeverTheBody` already asserts
`GetComponentInChildren<Animator>` is absent (line 105) — leave it alone. **Add:**

- `Arms_ParentToShownWrapper_NotToGrip` — the source parents `_arms` under the kept wrapper
  inside `EnableOnly`, and never under `_grip`. This is the whole design in one assertion.
- `ArmsOffset_IsWrittenPerFrame_NotAtSwitchTime` — `PoseArms` reads
  `WeaponView.ArmsOffset(_shown, ...IsZooming)`, `LateUpdate` calls `PoseArms`, and
  `EnableOnly` does **not** mention `ArmsOffset`. Pins the state/pose split above so the
  frozen-stance defect cannot return.
- `Arms_ActivateAndDeactivate_ShareOneChokePoint` — `EnableOnly` contains both the active
  and inactive arms writes, and neither `Awake` nor `ShowHidden` contains an arms activation.
  This is the dangling-state bug the review caught, pinned so it cannot come back.
- `Arms_AreNotSkinned` — `Arms.prefab` contains `MeshRenderer` and not
  `SkinnedMeshRenderer`, so `BoarderPresentation`'s `LocalHead` sweep cannot eat them.
- `Arms_AuthoredInactive` — asserts `m_IsActive: 0` **in `Arms.prefab`**, and that
  `Boarder.prefab` contains exactly one `Arms.prefab` guid reference. It does **not** assert
  `m_IsActive: 0` inside `Boarder.prefab`: a nested instance inheriting the source value
  writes no override, so that string is not there to find. Criteria 4 and 7 off disk.
- `ArmsOffset_IsSharedAcrossWeapons` — `WeaponView.ArmsOffset` accepts `(WeaponId, bool)`,
  returns equal values for the five mapped catalog ids, and returns the same value for
  `None`, `Vk7Rifle` and `ThrowawayComposite`. A per-class branch cannot be added without
  updating the test that says there is not one.
- `RightHandBone_IsGone` — neither `WeaponView.cs` nor `WeaponViewModel.cs` mentions
  `mixamorig:RightHand` or `_rightHand`. Closes TH-24 off disk.

### Ordered

1. Code and tests first: `ArmsOffset`, the `EnableOnly` signature and arms rule, the
   `_arms` field, the two TH-24 deletions, the two comment corrections, the six new tests.
   Green with a placeholder cube standing in for the arms mesh — this proves the seam before
   any mesh exists, and it is the whole of what can land without Blender or the Editor.
2. Cut the arms in Unity: GatekeepersProtocol > Arms > Arms Cutter — spawn the rig, pose the
   hands on a weapon, bake. `arms_source/README.md` records the route and the restore step.
3. The bake writes `Arms.prefab` inactive; drag it under `Boarder.prefab`'s `Grip`, delete
   the posing rig, and clear `Assets/Scripts/Editor/Tools/ArmsCutter.cs` before the verify gate.
4. Play `Mk1` alone; set `ArmsOffset`'s floats off zero until criterion 3's two `Mk1` rows
   pass.
5. Walk the remaining ten rows. Add a per-class offset only where a row fails, naming the
   weapon in that commit. Expect the `Cutter` rows to be the ones that fail.

## Supporting work

- **Driven by** the existing weapon slots (`1`/`2`/`3`, `Q`, the wheel; `M11`, `M15`) — here.
  This milestone adds no input.
- **Shown by** the arms themselves, in first person, from the first frame of a Run — here.
- **Reads back as** the arms kicking on `Fire` and dipping on `Reload`, driven by the same
  `M82` clips that already move the gun — here.

## Verification

1. Criterion 1 — `Arms_ParentToShownWrapper_NotToGrip` and
   `Arms_ActivateAndDeactivate_ShareOneChokePoint`, off source. Criterion 3's zoom half
   additionally needs `ArmsOffset_IsWrittenPerFrame_NotAtSwitchTime`.
2. Criterion 2 — `git diff --stat Assets/Packs/Weapons/`; expect zero changed lines in the
   four `.anim` files and `ViewModel.controller`.
3. Criterion 3 — playthrough path: from Play, EMBARK, then for each weapon in turn stand
   still, look at it at hip, zoom, look again. Twelve rows recorded pass/fail in this file's
   close. Zo adjudicates.
4. Criterion 4 — `Arms_AuthoredInactive`'s single-guid half off disk; then in a Run, switch
   with `1`/`2`/`3` and confirm the hands follow rather than duplicating.
5. Criterion 5 — `grep -n "public void Show\|public void NotifyFired\|public void NotifyReload" Assets/Scripts/Game/Player/WeaponViewModel.cs`
   before and after; the three lines diff to nothing. `PlayerGun.Firing.cs:63` and
   `PlayerGun.Reload.cs:93` unchanged.
6. Criterion 6 — `Tools/verify/run.sh` green with all twelve original `ViewModelAnimTests`
   names present, and `ls Assets/Packs/Weapons/ViewModel*` still listing the controller and
   four clips.
7. Criterion 7 — `Arms_AuthoredInactive` off disk, then a two-client session looking at the
   other player. No arms on the remote body.
8. Suites — `Tools/verify/run.sh`, plus `lint-glyphs`, `lint-language`, `lint-vendor`.
   `docgen` and the test compile need mono, which this machine lacks (`M103`'s close records
   the same gap).

Criteria 3, 4's Run half and 7's two-client leg are eyes-on and close on Zo's play. Step 1's
tests, the greps and the prefab assertions close 1, 2, 5, 6 and 7's disk half on their own.

**Two limits, stated because they bound what a green run means.**

`Arms_AreNotSkinned` and `Arms_AuthoredInactive` read `Arms.prefab` and call `Assert.Ignore`
while it is absent, so they report ignored rather than passing. **Criteria 4 and 7 do not
close while either reports ignored** — a green suite with those two skipped is a suite that
has not checked them, and after step 3 lands the asset a later rename or deletion would
return them to skipping silently. Read the ignored list, not just the exit code.

Every test in `ViewModelAnimTests` asserts against **source text**, not behaviour: there is
no offline MonoBehaviour tier. A `PoseArms` whose body was `return;` passes all of them.
Criteria 1 and 2's structural half is therefore proven to be written, not proven to run —
criterion 3's play rows are what prove it runs.

## Watch

- `WeaponViewModel` guards every animator call with null checks (`M82` Watch). Keep them;
  the Ship scene may run a viewmodel with no arms wired.
- Do not fall back to `GetComponentInChildren<Animator>()` on the Boarder root — it binds
  `Boarder.controller` and `Fire`/`Reload` never reach the gun. The test asserting that
  string is absent is deliberate.
- `IsometricBodyPresentation` owns the `Speed` parameter while the iso rig runs
  (`WeaponViewModel.LateUpdate`). The arms do not change who wins that.
- **The prop is a stand-in for culled arms, so it is on exactly when the body is culled.**
  `BoarderPresentation` skips the `LocalHead` cull when the iso rig is wired (the body is
  drawn), and `EnableOnly` keeps the prop off under the same condition — otherwise the iso
  Ship view shows a third arm. The Scene view in play draws the body regardless of the game
  camera's mask, so three arms *there* is expected and not a defect (Zo, 2026-09-02).
  **Teammates are criterion 7, not this note:** they see `NetPlayerAvatar`, whose nested
  `Arms` is inactive by override and has no `WeaponViewModel` to flip it — a GameObject
  flag nothing replicates. Asserted on disk; the two-client eyes-on (Verification 7) is
  still owed, and a third arm seen there is a criterion failure, not polish.
- `Show` is reached from `LateUpdate` via `ApplyShown` every frame but only re-enters its
  body when the id changes or the view was hidden. The arms rule lives in `EnableOnly`,
  which `Show` calls — so it inherits that guard. Do not lift it into `ApplyShown`.
- One arms pose ships. The six weapons already share one `Hold`, so they are already all in
  the same pocket; a pose that suits a rifle and a sidearm equally is a known limit, not a
  regression. Criterion 3 measures intersection and gap, not grip realism.
- **The wrapper roots do not share a scale, and `GunHold` has no scale term.**
  `SecD.prefab:29` is `m_LocalScale: 0.88`; the other five are `1`, and `Boarder.prefab`
  writes no scale override on any of the six instances. The arms parent under the wrapper
  root, so one `ArmsOffset` in wrapper-local space gives `SecD` a hand 12% small on a 12%
  short offset. "Already in the same pocket" is true of `Grip` and **false of the transform
  the arms actually ride**. Either give `SecD` its own branch or add a scale term; decide
  when its rows are walked, not before.

### Per-class arms (2026-09-02)

The MR-35 rows fail by construction on a pistol bake — one hand, landing at the rifle's
stock — so the escalation the milestone reserved is taken: **one baked pose per arms
class**, not per-class offsets. `WeaponView.ArmsClassFor(WeaponId)` sorts the catalog the
way `FireClip` already does (Mk1 → Pistol; Mr35, PriC, SecD → Rifle; Br12 → Shotgun; the
cutter → Melee), `Arms.prefab` carries one child per class named by
`WeaponView.ArmsClassName`, and `EnableOnly` shows the matching child and hides the rest.
**A class with no bake shows no arms** — bare beats another class's hands on this gun. The
cutter's Class popup writes `Arms.<Class>.asset` into that child and leaves the others as
they are; the MK-1 bake migrated into `Pistol` unchanged. **Rifle baked 2026-09-02** (Zo,
two-handed, gun in the left hand, posed on an `Mr35.prefab` instance). The first attempt
went through the raw-fbx branch — the cutter knew only the *Mk1* wrapper as gun space — and
the support hand came out 34 cm above the gun; fixed by recognising any
`Packs/Weapons/*.prefab` instance root by source path. The re-bake logs
`frame: wrapper root 'Mr35' (Mr35.prefab)`, with the hands on the grip (12 cm forward of
the stock origin) and the handguard (34 cm forward), on opposite sides of the centreline.
`ArmsOffset` stays the per-weapon delta within a class. Rejected: sliding the pistol hand along the rifle with an offset —
it is still one hand, and the support hand is the thing a rifle needs.

## Do not build

- Per-weapon arms poses within a class. One bake per class; `ArmsOffset` is the delta.
- Skinned or independently animated arms. The prop inherits the wrapper root's motion; a
  hand that leaves the grip during a reload is a later milestone with its own clips.
- Arms for co-op remote avatars. `NetPlayerAvatar` remote presentation is `HP-5`; criterion
  7 exists to keep the arms out of it, and TH-21 stays open.
- A full ADS system. `zoom` reaches `ArmsOffset` only as far as criterion 3's zoom rows need.

## Depends on / Unblocks

Depends on `M82`, whose controller and four clips this milestone **keeps and rides**.
Independent of everything else in the queue. Supersedes `M82`'s stated plan of an FPS arms
pack; that path is closed and the reason is recorded above. Pays **TH-24** for `HP-5`.

## Closed, Sep 2026

Closed by Zo 2026-09-02: "We have the capability. We need to tune it for immersion."
What is in: the seam held (criterion 5), the `M82` clips and controller drive the arms
(criterion 6), the prop parents to the shown wrapper root and is on exactly when the body is
culled (criteria 1, 2, and the iso rule in Watch), one class child per arms class with
`Pistol` and `Rifle` baked from the Boarder's own mesh in each gun's frame, the camera
pocket mirrored to the Boarder's left hand, and a second pocket for long guns. Suite at
close: 841 passed, 4 failed (the four pre-existing scene-YAML reds), 5 ignored, on the
offline harness with its two pre-existing gates stubbed; four lints clean.

**Owed, in the order they bite:**

- Criterion 3, ten of twelve rows: `Mk1` zoom, `SecD` ×2, `Br12` ×2 (no Shotgun bake yet —
  shows no arms), `Mr35` zoom, `PriC` ×2, `Cutter` ×2 (no Melee bake yet — shows no arms).
  `Mk1` hip and `Mr35` hip read as held; `Mk1`'s thumb polish and both pockets' final
  numbers are the immersion tuning Zo named. The tuning surface is the six `LocalGun*` /
  `LongGun*` constants and the `Hand.*` poses re-baked from the rig.
- Criterion 7's two-client leg: a teammate looking at the other player. Asserted on disk,
  never seen.
- Criterion 4's Run half: `1`/`2`/`3` switching seen by eye. The class-child switch was
  verified in play by state, not by a person.
- `SecD`'s 0.88 wrapper scale (Watch) has not been looked at.

**Paid after close, 2026-09-02:** the Ship showed guns without hands. `Ship - CoOp.unity`'s
Boarder is an **unpacked copy**, not a `Boarder.prefab` instance, so the arms nested into
the prefab never reached it and `EnableOnly` found no `Arms`. The cutter's Step 4b wires an
inactive `Arms.prefab` instance under any scene `Grip` that is not part of a prefab
instance; run on the Ship and saved. Bastion and Range instance the prefab and needed
nothing. Any future scene with an unpacked Boarder needs Step 4b once.
- The bake tool lives at `Assets/Scripts/Editor/Tools/ArmsCutter.cs` (promoted out of
  `_Scratch` at close, because the source README depends on it and a clean clone must be
  able to re-cut).
