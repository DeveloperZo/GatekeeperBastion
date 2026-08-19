# M87 — A bought body in an hour

<!--
Status: later
Update trigger: when the Built-In Shader Graph target is confirmed or refuted in
the editor, when the first character renders in the Bastion, or when a second
bought pack arrives and needs the same path
Last updated: 2026-08-18 (rewritten: the package was read; the URP-swap bet it
originally carried is void and a better one replaces it)
-->

One bought character standing in the Bastion, in under an hour, so the body
question in
[`bodies-and-art-direction`](../../Brainstorms/bodies-and-art-direction.md) can be
answered by looking instead of arguing.

## Story

No player-facing change and no player story. It exists because the material cost
was the thing standing between a bought body and a graduation test nobody had
taken.

## Why it matters

The project is Built-in Render Pipeline: no URP or HDRP package in
`Packages/manifest.json`, `GraphicsSettings.asset` has
`m_CustomRenderPipeline: {fileID: 0}`. The assumption was that every bought pack
therefore costs an afternoon of hand-rebuilt materials, which is why the android
entry had never been tested.

**Read from the package 2026-08-18** (`scifirobotpack_hdrp_urp.unitypackage`,
extracted and inspected, not inferred from the store page):

- The materials do not use URP Lit or HDRP Lit. They use one custom Shader Graph,
  `SG_SciFiRobotMat.shadergraph`, with properties `_BaseColor`, `_Normal`,
  `_Emissive`, `_RoughMetAO`, `_PaintMask`, `_PaintTexture`.
- **That graph ships three targets**: `UniversalTarget`, `HDTarget`, and
  `BuiltInTarget` / `BuiltInLitSubTarget`. It is authored to compile for the
  Built-in Render Pipeline as well as the two scriptable ones, and the materials
  carry `_BUILTIN_QueueControl` alongside their HDRP keywords.
- `com.unity.shadergraph` 17.3.0 is already in this project's manifest.
- Textures ship **unpacked as well as packed**: per character there is a
  `_BaseColor`, `_NormalGL`, `_Metallic`, `_Rough`, `_AO` and `_Emissive`, in
  addition to the combined `_RoughMetAO` the graph consumes. `NormalGL` is the
  OpenGL convention Unity expects.

So the original bet — that a URP Lit variant would swap to Standard by property
rename — is void, because there is no URP Lit material in the package. The bet
that replaces it is better: **the pack's own Shader Graph already targets
Built-in, so nothing needs converting at all.**

This also reverses the earlier advice to take the fbx `source.zip`. The
`.unitypackage` is now the correct download, because the shader graph, the
material assignments and the texture import settings are the valuable part.

Decisions made here, not left to the implementer:

- **One character, not four.** Utility Bot. The graduation test needs one body in
  the scene; converting three more before anyone has looked at one is work spent
  ahead of the decision it serves.
- **Import selectively.** Take `Models/SK_UtilityBot.fbx`, `Materials/`,
  `Textures/` and the shader graph. Leave the demo scene, the studio HDRI, the
  background sphere and platform, `Timeline.playable`, the nested
  `SciFiRobotsPack.unitypackage`, and the whole `Animation/` folder — those 24
  pose controllers and four pose FBXs are single-frame poses, which the asset
  audit already recorded as filling zero tracker rows.
- **No script until one is needed.** The original milestone specified a `/tmp`
  converter. If the Built-in target compiles, there is nothing to convert and the
  script is never written. Per the tooling gate, that is the correct outcome, not
  a missed opportunity.
- **The four pose FBXs already in `Packs/Gatekeepers/sci-fi humanoids/` are
  redundant** once this lands and should be removed rather than left as a second
  copy of something the package owns.

## Acceptance criteria

1. `SK_UtilityBot.fbx`, its textures and its materials are in the project, and the
   demo scene, HDRI, platform, background sphere, Timeline and `Animation/` folder
   are not.
2. `M_UtilityBot` renders on the Utility Bot with albedo, normal, emission and a
   metal/roughness response. Nothing magenta, no shader compile error in the
   console.
3. `SK_UtilityBot.fbx` reads `animationType: 3` in its `.meta` and its avatar
   configures with no unassigned required bone.
4. A Utility Bot instance placed in `Bastion.unity` renders correctly under that
   scene's lighting, not just in the pack's own studio lighting.
5. `Anim_attack_melee` plays on that instance in the Animator preview, arms and
   legs moving with it.
6. Elapsed time from opening the importer to criterion 4, recorded in this file.
7. Nothing outside the new folders changes: no pipeline package added to
   `Packages/manifest.json`, `SceneBuilder.Enemies.cs` untouched, the three Synty
   Gatekeeper prefabs untouched.

## Kill criterion

The bet is *the pack's Shader Graph compiles and renders on Built-in.* If it
errors, renders black or renders untextured after import, that bet is dead —
record it here and fall back, which is now cheap and no longer needs a repack
guess:

> Build one Standard material by hand. Albedo `T_UtilityBot_BaseColor`, Normal
> `T_UtilityBot_NormalGL`, Occlusion `T_UtilityBot_AO`, Emission
> `T_UtilityBot_Emissive`. Metallic/smoothness is the only composite: R from
> `T_UtilityBot_Metallic`, alpha from `1 - T_UtilityBot_Rough`. Both inputs ship
> as separate maps, so this is one known compositing step, not a channel-layout
> guess against a packed ORM.

What is lost on that fallback: paint colour, paint mask, wear mask and the camo
variants, which are all graph features. That costs the six colourways, and if
per-kind tinting later matters it is a reason to make the graph work rather than
to accept the fallback permanently.

## Implementation

Zo executes. Steps in order:

1. **Import** `scifirobotpack_hdrp_urp.unitypackage`, deselecting everything
   outside `Models/SK_UtilityBot.fbx`, `Materials/`, `Textures/` and
   `SG_SciFiRobotMat.shadergraph`. Watch the console during import — a shader
   compile error here fires the kill criterion before anything else is tried.
2. **Check** the Utility Bot renders in the Project preview or a scratch scene.
   The material's HDRP-only keywords (`_DISABLE_SSR_TRANSPARENT`, `_MaterialID`,
   `_RequireSplitLighting`, `_RayTracing`) will fall into `m_InvalidKeywords` on
   Built-in; that is expected and not itself a failure.
3. **Set** the rig: Humanoid, Create From This Model, then open Configure and read
   the mapping. The pack is UE5 Mannequin rigged (`spine_01`, `thigh_l`,
   `clavicle_r`), so Unity maps by hierarchy rather than name; chest, upper chest
   and the hands are where a slot is most likely left empty.
4. **Set** texture Max Size to 2048. 4K on a wave enemy seen at ten metres is
   memory spent on nothing.
5. **Delete** the four redundant pose FBXs at
   `Packs/Gatekeepers/sci-fi humanoids/` and their sidecars.
6. **Place** one instance in `Bastion.unity` next to a Synty wall, at player
   height, where a Gatekeeper would stand.
7. **Look at it**, and write what you see into the brainstorm's android entry.
   That sentence is the graduation test, and it is the only output of this
   milestone that still matters once the tooling is forgotten.

**Rejected 2026-08-18:** converting the package to URP first in a scratch project
(Unity's [HDRP-to-URP shader
guide](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/convert-hdrp-shaders-to-urp.html)).
The converter only runs *toward* URP and no official URP-to-Built-in path exists,
so it ends holding materials this project still cannot use. It is also moot now
that the graph is known to carry a Built-in target. The glTF and fbx `source.zip`
downloads are rejected for a different reason: they carry no shader graph and no
material assignments, so they would force the fallback path by default.

## Verification

1. Criteria 1 and 7 — `git status`, changes confined to the new folders plus this
   file and the brainstorm. The repo uses Git LFS, so a machine without
   `git-lfs` reports every tracked binary as modified; check where
   `git lfs version` answers.
2. Criterion 3 — read `animationType` in the FBX's `.meta`; the bone check needs
   Configure Avatar and no file read can do it.
3. Criteria 2, 4 and 5 — Scene view and the Animator preview. None of these is
   provable offline and none may be called done from a file read.

## Watch

Do not add a render pipeline package to fix a magenta material. That is the
project-wide migration this milestone exists to avoid, and it would arrive
disguised as a one-line fix in the package manager.

Do not touch `SceneBuilder.Enemies.cs`. The three Synty body paths are hard-coded
there and swapping them is the *next* decision, gated on what step 7 shows.

## Depends on / Unblocks

Depends on nothing. Unblocks the graduation test in
`Brainstorms/bodies-and-art-direction.md`, and therefore the android Gatekeepers
entry and any later use of the alien or the weapon pack.
