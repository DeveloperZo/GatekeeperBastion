# arms_source

First-person forearms for `M113`, cut from the Boarder's own body so the hands match the
suit by construction.

| Artifact | Source | How it was made |
|---|---|---|
| `../Arms.<Class>.asset` (mesh, one per arms class; `Arms.Pistol.asset` is the MK-1 bake) | `Assets/FaxLab3D/SciFiRobotCharactersPack/Models/SK_Tactical.fbx` | **Right** forearm + hand only, baked in Unity by `Assets/Scripts/Editor/Tools/ArmsCutter.cs` (GatekeepersProtocol > Arms). In the rig's world frame (Y-up, metres), pivot at the `hand.r` bone |
| `../Arms.prefab` | the class meshes + `../BoarderBot.mat` | Root `Arms` **active** (so it previews and can be dropped in a scene) with one child per class (`Pistol`, `Rifle`, `Shotgun`, `Melee`) and the hand meshes under it at identity; `WeaponViewModel` shows the child for the weapon's class and nothing for a class not yet baked. `M113` criterion 7 is the `m_IsActive: 0` override on the instance inside `Boarder.prefab`, written by the cutter's Step 4 |

**No Blender.** Pose the rig by hand in the scene with a gun in its hands, set that gun as
the cutter's **Gun pivot**, bake. The bake is `SkinnedMeshRenderer.BakeMesh` expressed in the
gun's wrapper-root frame (barrel +Z, top +Y; a raw `SM_Pistol_Energy` fbx is corrected by the
Mk1 wrapper's child transform), so the prop reproduces the posed grip when parented at
identity under any weapon wrapper root — no TRS tuning. The cutter follows the gun: the
posing gun is the `SM_Pistol_Energy` or `Mk1.prefab` instance closest to either hand bone
(anything under a `Grip`, or further than 0.6 m from both hands, is refused with a warning —
a Boarder's own weapon or a prefab-stage copy at its origin is not the posing gun), and the
gripping hand is whichever of `hand.l` / `hand.r` is closer to it. Modes: both arms as posed
(one mesh per side — `Arms.asset` for the gun hand, `Arms.Other.asset` for the other — on
children `Hand.L` / `Hand.R` named by the hand they really are, so either can be ticked off
in the prefab and `EnableOnly`, which only touches the root, leaves it off in play), the gun
hand only, or the gun hand mirrored for the other side. A null pivot with no gun found falls back to the gripping
hand's own bone as the frame and says so. The triangle rule keeps those whose three corners
carry ≥ 0.5 of their skin weight on the forearm, hand and digit bones (Auto-Rig Pro names: `hand`, `forearm_stretch`, `forearm_twist`,
`forearm_twist_2`, `c_index1..3`, `c_middle1..3`, `c_ring1..3`, `c_pinky1..3`,
`c_thumb1..3`, each `.l` / `.r`). The upper arm is left out; the elbow cut sits behind the
camera near plane in first person. Vertex order survives the bake, which is what lets the
source mesh's weights decide which baked triangles are arm.

**Tracking.** `Arms.asset` and `Arms.prefab` are committed — they are the artifact. The
source fbx is **not** in git: `git check-ignore -v` resolves `SK_Tactical.fbx` to
`.gitignore:149`, and `Docs/Decisions.md` narrows the FaxLab3D pack to its `Materials/` and
`Textures/*.meta` (102 files). A clean clone can render the arms but cannot re-cut them
until the SciFiRobotCharactersPack is re-imported from its store archive.

**Re-cutting.** Re-import the pack, open the cutter, spawn the rig, pose the hands on a
weapon, bake. The pose is not stored anywhere but the baked mesh; if a different grip is
wanted, pose again and bake again. The "Curl fingers" button is a starting guess, not the
pose that shipped.
