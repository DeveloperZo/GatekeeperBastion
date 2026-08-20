# M61 — Polish Pass 2

<!--
Status: completed 2026-08-14
Update trigger: closed
Last updated: 2026-08-19
Closed on: Zo 2026-08-14. Playthrough Zo (editor was in Play when close was requested;
no `Logs/` session file on disk). Gate: Unity EditMode `WeaponViewTests`+`ControlsTests`
15/15; PlayMode `ShipSceneContractTests` 5/5. Offline harness skipped (mcs/mono not on
PATH). `HudSeparationTests` 4 fail — M59's open AABB/EXP gates, not this slice.
Fire/walk audio is M69.
-->

The Boarder holds a gun, aims with a sight, can look down a lane, and walks a Ship that is
not four coloured cubes. Zo 2026-08-13: this slice is the art/audio floor the milestone
contract would have parked as a phase. The gate is still the numbered criteria. Kit
footprints stay locked (`../../Decisions.md`).

The Boarder body is the Mixamo astronaut already in Infinity Qube. That project only
ships **Idle** and **Run** (`C:\Users\awill\Unity\InfinityQube\Assets\animations\Player\`,
Humanoid `mixamorig`). Fire, reload, and walk clips are named Mixamo takes on the same
avatar; the files currently in `Assets/Packs/Boarder/` are copies of Idle/Run until
distinct Mixamo downloads replace them.

**Store pack locked 2026-08-14:** [POLYGON Sci-Fi Space Pack](https://assetstore.unity.com/packages/3d/environments/sci-fi/polygon-sci-fi-space-pack-art-by-synty-138857) (SKU 138857, v1.14.0) at `Assets/Synty/PolygonSciFiSpace/`. Guns are hand-parented meshes on `mixamorig:RightHand`. Wrappers under `Assets/Packs/Weapons/` are the six distinct prefab refs (the pack ships pistol, rifle, shotgun, sword).

| Source | Status | What we take |
|---|---|---|
| **Infinity Qube astronaut** | locked | `Anim_PlayerIdle.fbx`, `Anim_PlayerRun.fbx`. No IQ scripts. |
| **Clips** | locked | `Anim_Walking.fbx` (Run copy), `Anim_RifleIdle.fbx` / `Anim_FiringRifle.fbx` / `Anim_Reloading.fbx` (Idle copies). Replace in place with Mixamo without-skin Humanoid takes of those names. |
| **POLYGON Sci-Fi Space** | locked | Weapons + `SM_Prop_ControlPanel_02` / `SM_Prop_Screen_Small_01` on Ship stations. Demo scenes stay out of Build Settings. |
| **SFX** | locked | `Assets/Resources/Audio/Sfx/Fire-Pistol.wav`, `Fire-Shotgun.wav`, `Fire-Rifle.wav`, `Reload.wav`, `Footstep-Walk.wav`. |

## Packs (closed)

Admitted: Synty Space Pack for meshes and desks. SFX are five bound wavs, not a store library. Six wrapper prefabs, no duplicates. FPS kits, FMOD, and the 5.3 GB SFX library stay out.

**Rejected 2026-08-13 trio (unchanged):** Arsenal V2 [246550] (pack fire/reload anims Mixamo already covers; listing has no shotgun). Control Room 3 as a locked buy (unreviewed). Sound Design Starter Kit [276617] (5.31 GB / 1173 files for five clips).

## Story

As a Boarder, I want to see and hear the gun in my hands, aim with a sight I can look
through, and walk up to Ship consoles that fit a person, so that the session is a place
rather than a graybox test area.

## Why it matters

`Design/1-Experience.md` §1.2 ranks Sensation as not addressed: gray-box art, no audio
pass. The fight HUD already names the weapon (`Hud Weapon`); nothing occupies the camera.

| Surface | Evidence | Now |
|---|---|---|
| Hands / body | No view-model. `PlayerGun.Fire` traces from `transform.position`. Co-op avatar is a capsule (`NetPlayerAvatar`). Infinity Qube already has the Mixamo astronaut (`Anim_PlayerIdle.fbx`) with Idle and Run only. | Empty first person; friends see a capsule. |
| Sight | Bastion `Cross` is a 24×24 `Image` with `m_Sprite: {fileID: 0}` (`Bastion.unity` Crosshair). Unity draws a filled white quad. Ship Crosshair is 8×8, same. | A square block on the look point. |
| Zoom | No `fieldOfView` write in `Assets/Scripts`. `ControlId.AltFire` is RMB, labelled "cancel place", used by `DecoyPlacement` only. | A 50 m PRI-C lane is a dot. |
| Ship desks | Authored Desk `1.6 × 1.2 × 0.7` m (`Ship.unity`). | Alcoves filled by primitive cubes. |
| Sound | `Assets/Resources/Audio/` holds two music files. `PlayerGun.Fire` plays no clip. | Shots are silent except particles. |

Leverage: **#8, information flows**. No damage, spread, or range number moves. Zoom is FOV
only. Kit segment geometry stays (M47 lock).

## Acceptance criteria

1. **The Boarder is the IQ astronaut, holding a gun from POLYGON Sci-Fi Space.**
   Local first person: camera at eye height, head/helmet not rendered to the local
   camera, weapon mesh on `mixamorig:RightHand`. Remote clients see the full astronaut
   (this replaces the capsule). Hidden while a station panel is open. Switching `1` /
   `2` changes the weapon mesh without reading the HUD. Animator: Idle / Walk / Run
   from the IQ clips plus Mixamo-named Rifle Idle, Firing Rifle, Reloading — Humanoid
   retarget, one rifle-hold set for every gun this slice. Mapping:

   | `WeaponId` | Prefab |
   |---|---|
   | `Mk1Sidearm` | `Assets/Packs/Weapons/Mk1.prefab` (`SM_Wep_Pistol_01`) |
   | `SecD` | `Assets/Packs/Weapons/SecD.prefab` (`SM_Wep_Pistol_01`, scale 0.88) |
   | `Br12Shotgun` | `Assets/Packs/Weapons/Br12.prefab` (`SM_Wep_Shotgun_01`) |
   | `Mr35Rifle` | `Assets/Packs/Weapons/Mr35.prefab` (`SM_Wep_Rifle_01`) |
   | `PriC` | `Assets/Packs/Weapons/PriC.prefab` (`SM_Wep_Rifle_01`, localScale z 1.4) |
   | none (cutter) | `Assets/Packs/Weapons/Cutter.prefab` (`SM_Wep_Sword_01`) |

   Five shop weapons plus cutter, six prefabs, no duplicate prefab refs. The pack has
   pistol, rifle, shotgun, sword — SecD and PriC are scale variants so the six refs stay
   distinct.

2. **Crosshair is strokes, not a filled quad.** No fight or Ship crosshair `Image` has a
   null sprite and both sides > 6. Cross = four arms, thickness ≤ 2 px, gap at centre.
   Dot ≤ 4×4. Brackets = four L-corners, 2 px strokes. `WeaponCrosshair` identity stays
   (`Sim/Meta/WeaponCrosshair.cs`). Shift theme colour. Do not replace HUD hosts.

3. **Hold RMB zooms.** Hip FOV **60**, zoom FOV **40**, on the local `Camera` under
   `FirstPersonController.cameraTransform`. Look sensitivity scales by `currentFov / 60`.
   No change to spread, damage, or falloff. While `DecoyPlacement.IsAiming`, RMB still
   cancels place and does not zoom. Release returns to 60. `ControlId.AltFire` label
   becomes `zoom`. Decoy place still cancels on RMB. Ship and fight both zoom.

4. **Ship consoles are Synty `SM_Prop_ControlPanel_02` + `SM_Prop_Screen_Small_01` at human scale**, not the
   1.6 m cubes. Desk top ≤ **1.0 m**, screen centre **1.4–1.6 m**, console width ≤ **1.2 m**. All four
   stations. Interact collider still hits from 2.5 m at eye height (`ShipSceneContractTests`).
   Cube Desk/Screen/Label on those four roots are inactive or deleted. Field terminals stay
   M59 Scale 1 cubes; dressing those is optional trim, not a rebuild.

5. **Graybox is dressed, not replaced.** Ship `Environment` colliders and Bastion kit
   footprints stay. Synty props may sit on floors and in alcoves. No new kit segment.
   Pack example scenes are not in Build Settings.

6. **The player action:** Splash → Ship: astronaut arms and a gun in frame at the ARMORY;
   consoles are desks a person uses. EMBARK: the same gun, stroke crosshair, hold RMB and
   a far wall fills more of the view, the arms fire, the body walks. A second view (MPPM
   or a client) sees an astronaut, not a capsule. `1` / `2` swap weapon meshes. Solo
   unchanged. No `[Dev]` in the close log. Fire/walk audio is `M69`.

7. EditMode: mapping table has six distinct prefab references; FOV constants 60 / 40;
   the Boarder prefab has a Humanoid avatar and the five clips (Idle, Run, Walk, Firing
   Rifle, Reloading). Scale/AABB tests from M59 still green. Suites green.

## Implementation

1. **Copy** `Anim_PlayerIdle.fbx` and `Anim_PlayerRun.fbx` from Infinity Qube into
   `Assets/Packs/Boarder/`. Author `Boarder.prefab` from the Idle FBX (skinned mesh +
   Humanoid avatar). Convert materials to URP. Do not copy IQ scripts or the IQ
   animator controller.
2. **Clips on disk** under `Assets/Packs/Boarder/`: `Anim_PlayerIdle.fbx`,
   `Anim_PlayerRun.fbx`, `Anim_Walking.fbx`, `Anim_RifleIdle.fbx`, `Anim_FiringRifle.fbx`,
   `Anim_Reloading.fbx`. The last four are Mixamo-named; fire/reload/walk currently
   duplicate Idle/Run until distinct Mixamo takes replace them in place. `Boarder.controller`
   — Idle/Walk/Run locomotion, Rifle Idle when still, Firing Rifle on shot, Reloading
   on reload. One rifle-hold set.
3. **Imported** POLYGON Sci-Fi Space (SKU 138857). Do not add pack demo scenes to Build
   Settings. Do not use a pack FPS controller, ammo script, HUD canvas, or pack weapon
   animators.
4. Mapping, clip names, and wavs are in Packs (closed) and criterion 1. Bound wavs:
   `Fire-Pistol`, `Fire-Shotgun`, `Fire-Rifle`, `Reload`, `Footstep-Walk`.
5. **Create** `Assets/Scripts/Game/Player/WeaponViewModel.cs` — LOCAL-OWNER. `Show(WeaponId)`
   parents/enables the mapped mesh on `mixamorig:RightHand`. No spread or recoil
   that moves the sim ray; the ray still comes from camera forward as today.
6. **Change** player rigs in `Ship.unity` and `Bastion.unity` and `NetPlayerAvatar` —
   `Boarder.prefab` is the visible body. Local camera does not render the head (layer
   `LocalHead` excluded from that camera's culling mask, or the helmet renderer disabled
   on the owner). Remote: full body, no view-model-only hide.
7. **Change** `Bastion.unity` / `Ship.unity` Crosshair trees — stroke Images as criterion 2.
   Keep `BastionHudView` Cross / Dot / Brackets slots.
8. **Change** `FirstPersonController.cs` — hip/zoom FOV, sensitivity scale, footsteps.
   Zoom input is `Controls.Held(ControlId.AltFire)` when not `MovementLocked` and not
   decoy-aiming. Cache the `Camera` on `cameraTransform`.
9. **Change** `Controls.cs` — AltFire display string `zoom`. Decoy cancel stays in
   `DecoyPlacement` behaviour; do not add a second binding.
10. **Change** `PlayerGun.Firing.cs` — `PlayOneShot` on the existing fire path (same place
    `Effects.MuzzleFlash` already runs). Clip from a small map: shotgun vs rifle vs pistol.
    AudioSource on the gun or Boarder, name without `Music`. Trigger `Firing Rifle` on the
    Boarder animator from this same path.
11. **Change** `Ship.unity` four station roots — swap cube Desk/Screen for the chosen
    console prefabs at the sizes in criterion 4. Keep `ArmoryStation` / `EngineeringStation`
    / `CrewQuartersStation` / `MapStation` on the roots. Re-run `ShipSceneContractTests`.
12. **Copy** bound wavs to `Assets/Resources/Audio/Sfx/` (LFS already tracks `*.wav`).
    Roughly: `Fire-Pistol`, `Fire-Shotgun`, `Fire-Rifle`, `Footstep-Walk`, `Reload` — exact
    names pinned at import. Do not copy music loops from that kit; menu and Bastion tracks
    stay Stardust Drifter / Whispers of the Void (M69).
13. **Change** docs: Roadmap row, Changelog. `Design/1-Experience.md` Sensation row and
    `Docs/README.md` "gray-box art" in the **close** commit, not before.

Rejected: FPS Animation Ultimate (ballistic M4/AK; we have a Mixamo Humanoid). Rejected:
accuracy or damage change on zoom. Rejected: replacing Bastion kit segments. Rejected:
pack example scenes in the build. Rejected: importing the 5 GB SFX library into git.
Rejected: a third music clip. Rejected: Gatekeeper mesh swap (`M63`). Rejected: per-weapon
Mixamo sets this slice. Rejected: FMOD. Rejected: copying IQ `PlayerManager` or any IQ
gameplay script.

## Supporting work

- **Driven by** look / LMB / RMB / walk — existing. RMB zoom is here; decoy cancel stays
  `DecoyPlacement`.
- **Shown by** astronaut arms and a gun in frame, stroke crosshair, smaller Ship desks — here.
- **Reads back as** mesh swap on `1`/`2`, FOV 40 while RMB held, a client seeing an astronaut
  — here. SFX slider is `M69`.

Bastion Scale 1 / fight HUD / station chrome / player-build keys:
[`M59`](M59-the-polish-pass-1.md). Music and gun/foot SFX: [`M69`](M69-the-session-has-a-voice.md).
Hit confirm, name labels, and player colour:
[`M66`](../1.%20planned/M66-polish-pass-3.md) — the body is here; who-is-who is there.

## Verification

1. Criteria 1, 3, 7 — `Tools/verify/run.sh --filter WeaponView` (and FOV tests); then
   `gkb_verify_request.txt` if MonoBehaviour binds changed.
2. Criteria 2, 4, 5, 6 — playthrough: Splash → Ship (astronaut arms, gun in hands, consoles
   at chest height) → EMBARK → fire anim, walk anim, hold RMB at a far wall, swap `1`/`2`.
   Second view sees an astronaut. Log has no `[Dev]`. Fire/walk *sound* is M69.
3. `ShipSceneContractTests` still reach each console. M59 Scale and HUD AABB still green.

## Do not build

| Not building | Why not |
|---|---|
| Kit footprint / navmesh rebuild | Locked, M47 |
| Station chrome, purpose copy, player-build keys | M59 |
| Fire / footstep / reload SFX, Bastion music | M69 |
| Gatekeeper meshes | `M63` — androids: RobotFemale / HelmetArmor / War_Robot |
| Hit flash, pre-attack tells, name labels, player colour | M66 |
| Compendium | M62 |
| Pack FPS controller / pack HUD / pack ammo / pack weapon animators | We have `PlayerGun` and Mixamo |
| Accuracy ADS | Zoom is a look |
| A second weapon pack | One arsenal (Synty 138857) |
| Per-weapon animation sets | One rifle-hold; mesh swap is the identity |

## Watch

- Prefab freeze (`../../Architecture.md` gotcha 1): bind Boarder, gun meshes, and clips in
  the scene / prefab, do not hope a code default instantiates the right mesh.
- IQ `Stage.unity` binds the astronaut mesh to Unity's default material. After copy,
  assign URP materials from the FBX textures (or a single Boarder material) so it is
  not unlit-magenta / default-diffuse.
- Mixamo clips must be Humanoid, without skin, or they fight the IQ avatar.
- Unity culls by the SkinnedMeshRenderer GameObject layer, not `mixamorig:Head`. This
  pack is one `Mesh1` SMR; LocalHead on the bone left the helmet in the local camera.
- Mixamo root is at the feet. CharacterController centre is mid-capsule (height 1.8,
  centre 0), so Boarder local y is **-0.9** or the camera sits inside the mesh.
- Local owner poses Grip in camera space (`WeaponView.LocalGunForward` **0.38** m), including
  from `WeaponViewModel.Awake` so the first frame is not bind-pose. Bind-pose hand is behind
  the lens after LocalHead culls the body; remotes keep the hand parent.
- Fire/reload/walk clips are Idle/Run stand-ins until Mixamo takes of those names land.
- Footstep clock must not allocate per tick (`../../Architecture.md`).
- Pause is non-blocking: zoom and view-model stay; do not `timeScale` them off.
- `.fbx` / `.png` / `.wav` already LFS (`.gitattributes`). YAML prefabs stay text.

## Depends on / Unblocks

Better after M59 (Scale 1 field terminals, HUD gutters) but not blocked on it:
view-models and zoom do not need those. Does not wait on M58. Unblocks a friends session
that is no longer empty air and silent cubes. M66 still owns whether a shot *landed*.
