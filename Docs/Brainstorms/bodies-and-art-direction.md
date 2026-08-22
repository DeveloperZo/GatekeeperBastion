# Bodies and Art Direction

<!--
Purpose: What the characters and weapons in this game are made of, and whether non-Synty
sources can sit in a Synty scene without reading as an accident.
Status: brainstorm
Update trigger: when a body or weapon source is bought, rejected, or swapped in, or when
the in-scene coherence question is answered.
Last updated: 2026-08-19
-->

## What this covers

The visual identity of characters and held weapons, and the sourcing decisions behind
them. Not clip sourcing — that is `../Architecture/asset-audit-2026-08-18.md` section 6 and the
`M81`–`M86` family. Not the weapon roster or what weapons do, which is
[`armory-and-weapons`](closed/armory-and-weapons.md), closed.

## Where it stands

Verified in code 2026-08-18, not from docs.

Gatekeeper bodies are already three distinct Synty prefabs, wired in
`Editor/Scenes/SceneBuilder.Enemies.cs` (lines 47–52): Default is
`SM_Chr_RobotFemale_01`, Hunter is `SM_Chr_SpaceSoldier_HelmetArmor_Male_01`, Thief is
`SM_Chr_BR_War_Robot_01`. Two of the three are already robots. The audit's
"stats+tint variants" line describes **behavior**, not mesh, and has been read the wrong
way at least once.

The Boarder is not Synty. It is the Mixamo astronaut carried over from Infinity Qube,
with a `mixamorig` skeleton, and the guns are meshes hand-parented to
`mixamorig:RightHand` (`M61`). Six weapons (Br12, Cutter, Mk1, Mr35, PriC, SecD) sat on
four Synty meshes until 2026-08-19; the five guns now carry their own FaxLab3D
shooter-pack mesh and only Cutter is still Synty, so no two share a silhouette.

Of 3,395 Synty assets on disk, 33 are referenced anywhere. The Bastion environment is
Synty and stays Synty under every idea below.

## Graduation test

Whether a non-Synty character or weapon reads as **deliberate** rather than mismatched
when it stands in the Bastion — which no listing page, screenshot or argument can answer,
only one imported body in the scene.

## Constraints

Any milestone from this file respects these:

- **The Boarder skeleton does not change.** Guns hang off `mixamorig:RightHand`; a body
  swap there breaks weapon attachment for every weapon at once.
- **Body paths are code.** `SceneBuilder.Enemies.cs` hard-codes the three source prefabs,
  both Synty character FBXs and the avatar loader. A swap is a code change plus a prefab
  rebuild, not a drag and drop.
- **The project is Built-in Render Pipeline.** No URP or HDRP package in
  `Packages/manifest.json`; `GraphicsSettings.asset` has
  `m_CustomRenderPipeline: {fileID: 0}`. Any bought pack shipping URP/HDRP
  materials imports with missing shaders. The cost of a bought body is
  therefore "rebuild materials on Built-in Standard from the supplied PBR
  maps", not "drag in the package" — take an fbx/source download over a
  `.unitypackage`. Migrating the project to URP to avoid that would touch every
  Synty material, the Shift UI and `Effects.cs`, and is not in scope for any
  idea on this page.
- **UE5 Mannequin rigs import to Unity as Humanoid**, so existing Mixamo clips retarget
  onto them. Retarget *quality* on non-human proportions is *unknown, needs play data*.

## Open

| Question | Owner | Note |
|---|---|---|
| Is the robot's darkness the material or the scene? | Zo | Discriminator, no opinion needed: stand a Synty Gatekeeper in the same spot. Both dark = the Bastion/Ship lighting. Only the robot dark = the pack's albedo range, which is dark painted metal by design. The pack was authored under a studio HDRI that was deliberately not imported. Tuning-class either way until a body swap is real. |

## Ideas

### Android Gatekeepers

Replace the Synty Gatekeeper bodies with a bought android set (candidate: FaxLab3D's
Sci-Fi Humanoid Robots, four bodies, UE5 Manny rig, 12k–20k tris, 4K PBR). The stated
motive is that Synty humanoids do not read as androids. Two of the three current bodies
are already robots, so the gain is fidelity and specificity of the android read, not
robot-versus-human. The pack's own animations are six static poses and fill nothing;
this is a body purchase only.

**Graduation test PASSED 2026-08-18 (Zo).** The Utility Bot was imported and placed in
the Ship scene: *"looks amazing as a gatekeeper."* The feared fidelity clash against
flat-shaded Synty did not materialise. The pack's Shader Graph carries a `BuiltInTarget`
and compiled on this project's Built-in pipeline with no conversion, no repack and no
script (`M87`). One open question rides out of the test and is **not** part of it: the
body reads a little dark in scene lighting.

### The alien as a boss body

A bought creature (candidate: Riley van Heeswyk's Alien) as the `M80` boss, arriving with
its own idle, walk, run, death, melee and ranged clips on its own rig, so nothing has to
retarget. Reference is Harbinger from Mass Effect 2. What made Harbinger land was the
possession rule and the voice, not the silhouette; the voice pipe exists (`M74`), the
rule does not. Blocked on the same input `M80` names absent: boss and elite composition.
A body cannot answer that, so this entry cannot graduate ahead of it.

### Distinct weapon silhouettes

Six weapons on four meshes means two of them are visually indistinguishable from another
weapon in the hand. A bought weapon set (candidate: FaxLab3D's Shooter Weapons Pack,
15 static meshes) would give each of the six its own. Caveats: the pack has no melee
mesh, so Cutter still needs the Synty sword and the mix persists; its grenades and scopes
fill nothing, since neither mechanic exists; and at 5k–50k tris LOD0 it puts the highest-
fidelity object in the game directly in front of the camera, where the coherence question
is least forgiving. `M82` hand-keys the viewmodel clips against whatever mesh is there,
so the two are independent. **Built 2026-08-19** (commit `0ddc531`, no milestone —
"wire the prefabs" is a chore per `../_meta/_definitions/milestone.md`): the five guns
nest their fbx from `Assets/Packs/weapon_source/`, Cutter keeps the Synty sword, and the
grenades/scopes sit unused in the folder. The LOD0 coherence caveat is now answerable in
play and has not been answered.
