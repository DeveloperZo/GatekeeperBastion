# Bodies and Art Direction

<!--
Purpose: What the characters and weapons in this game are made of, and whether non-Synty
sources can sit in a Synty scene without reading as an accident.
Status: brainstorm
Update trigger: when a body or weapon source is bought, rejected, or swapped in, or when
the in-scene coherence question is answered.
Last updated: 2026-08-30 (stamp catch-up; FaxLab3D Gatekeepers and guns unchanged since 2026-08-25 verify)
-->

## What this covers

The visual identity of characters and held weapons, and the sourcing decisions behind
them. Not clip sourcing — that is `../Architecture/asset-audit-2026-08-18.md` section 6 and the
`M81`–`M86` family. Not the weapon roster or what weapons do, which is
[`armory-and-weapons`](closed/armory-and-weapons.md), closed.

## Where it stands

Verified in code 2026-08-25, not from docs.

**The Gatekeepers are no longer Synty.** `M87` bought the first body and `M88` finished the
swap, and `Editor/Scenes/SceneBuilder.Enemies.cs` now wires one FaxLab3D robot per kind out of
`Assets/FaxLab3D/SciFiRobotCharactersPack/`: Default is `SK_UtilityBot`, Hunter is `SK_Hunter`,
Thief is `SK_Exterminator`, and the Cloaker — the fourth kind, `M63-3`, 2026-08-24 — is
`SK_Tactical`. Silhouette is what tells the kinds apart now, at a per-kind body scale of
1 : 0.85 : 1.4 under a shared `BodyScale` of **0.75**; before the swap they were three Synty
humanoids separated by an emission tint, which asked the player to read a colour on a body they
had already learned. The Cloaker is deliberately identical to Default in footprint and agent
metrics so the fourth kind changed no navmesh assumption.

The Boarder is not Synty. It is the Mixamo astronaut carried over from Infinity Qube,
with a `mixamorig` skeleton, and the guns are meshes hand-parented to
`mixamorig:RightHand` (`M61`). Six weapons (Br12, Cutter, Mk1, Mr35, PriC, SecD) sat on
four Synty meshes until 2026-08-19; the five guns now carry their own FaxLab3D
shooter-pack mesh and only Cutter is still Synty, so no two share a silhouette.

Of 3,395 Synty assets on disk, 33 were referenced anywhere (counted 2026-08-18, not
recounted since the body swap, which can only have lowered it). The Bastion environment is
Synty and stays Synty under every idea below.

## Graduation test

**Satisfied 2026-08-18 (Zo), and shipped since.** The test was whether a non-Synty character
or weapon reads as deliberate rather than mismatched when it stands in the Bastion, which no
listing page, screenshot or argument could answer — only one imported body in the scene. The
Utility Bot answered it, `M87` and `M88` swapped every Gatekeeper across, and the guns went
the same way. What is left in this file is the boss body, which is blocked on composition
rather than on coherence, and the lighting question in Open.

## Constraints

Any milestone from this file respects these:

- **The Boarder skeleton does not change.** Guns hang off `mixamorig:RightHand`; a body
  swap there breaks weapon attachment for every weapon at once.
- **Body paths are code.** `SceneBuilder.Enemies.cs` hard-codes one source fbx, one pack
  prefab and one body scale per kind, plus the shared controller and the avatar loader. A
  swap is a code change plus a prefab rebuild, not a drag and drop — and a fifth kind is
  four more constants there before it is anything else.
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
| Is the robot's darkness the material or the scene? | Zo | Open, and the swap is done, so the original discriminator — stand a Synty Gatekeeper beside one — no longer exists in the build. The remaining cheap one is a Synty prop in the same spot: both dark means the Bastion/Ship lighting, only the robot dark means the pack's albedo range, which is dark painted metal by design and was authored under a studio HDRI that was deliberately not imported. `ship-hull-and-interior.md` records that exactly one `Light` exists in the hull, which is the likelier half. |

## Ideas

### The alien as a boss body

A bought creature (candidate: Riley van Heeswyk's Alien) as the `M80` boss, arriving with
its own idle, walk, run, death, melee and ranged clips on its own rig, so nothing has to
retarget. Reference is Harbinger from Mass Effect 2. What made Harbinger land was the
possession rule and the voice, not the silhouette; the voice pipe exists (`M74`), the
rule does not. Blocked on the same input `M80` names absent: boss and elite composition.
A body cannot answer that, so this entry cannot graduate ahead of it.

### Distinct weapon silhouettes — done, kept for the caveat

**Built 2026-08-19** (commit `0ddc531`, no milestone — "wire the prefabs" is a chore per
`../_meta/_definitions/milestone.md`), and `M93` has since made the three Primaries read as
three guns in play as well as in the hand. The five guns nest their fbx from
`Assets/Packs/weapon_source/`, Cutter keeps the Synty sword because the pack has no melee
mesh, and the grenades and scopes sit unused in the folder since neither mechanic exists.

The one caveat that survives the build: at 5k–50k tris LOD0 this puts the highest-fidelity
object in the game directly in front of the camera, where the coherence question is least
forgiving. **Answerable in play and still unanswered.**
