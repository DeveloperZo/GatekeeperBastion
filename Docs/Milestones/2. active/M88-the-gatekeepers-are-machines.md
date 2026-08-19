# M88 — The Gatekeepers are machines

<!--
Status: pack de-magentaed and configuration landed; needs a scene build to take effect
Update trigger: when the robot ShaderGraph gains a Built-In target, when a scene
build re-dresses the wrappers, or when a kind's model changes
Last updated: 2026-08-19
-->

One FaxLab3D robot per Gatekeeper kind, so the three kinds are told apart by
silhouette instead of by a tint on three near-identical humanoids.

## Story

As a player, I want to know which kind of Gatekeeper is coming at me before it
reaches me, so that I can choose what to do about it while there is still time.

## Why it matters

The three kinds are stat blocks wearing near-identical Synty humanoids,
separated by an emission tint on the pack's glow regions. That asks the player
to read a colour on a body they have already learned, at whatever distance and
lighting the Bastion happens to give them. Size does some work — Ram is 1.4
scale against Hunter's 0.85 — but Default and Hunter are the same shape.

The Default kind was already hand-swapped to the FaxLab3D UtilityBot, which is
also why the Gatekeeper clips retarget the way they do: `Running.fbx` and
M81's `Anim_attack_melee.fbx` were both downloaded on the uploaded SK_UtilityBot
character with its ARP rig intact. The clips were already authored for robots.
Hunter and Ram were the two that had not caught up.

Decisions made here, not left to the implementer:

- **Hunter takes `SK_Hunter`, Ram takes `SK_Exterminator`** (ruled 2026-08-19,
  Zo). `SK_Tactical` stays unassigned on purpose: M80 names boss/elite
  composition as an absent input, and a fourth distinct robot is exactly what
  that ruling would want available.
- **Silhouette becomes the kind tell; the tint stays as a second channel.** The
  per-kind emission write in `EnsureWrapperEmission` is kept and still works —
  each material carries its robot's emissive map, so the tint lands on the
  authored glow regions and not on the body.
- **The pack renders as authored; nothing is converted.**
  `SG_SciFiRobotMat.shadergraph` ships `BuiltInTarget`, `HDTarget` and
  `UniversalTarget`, so it compiles in this Built-In project untouched — M87 read
  that from the package on 2026-08-18 and it holds. The kind materials are copies
  of `M_UtilityBot`, `M_Hunter` and `M_Exterminator` on that same graph, which
  keeps paint colour, paint mask, wear mask and the emissive mask working.

  **The copy of that graph on disk had been stripped to `UniversalTarget` alone**
  — 122,228 bytes against the package's 129,174 — which is why every robot rendered
  magenta. How it lost two targets between the package and the project is not
  known; it is not a re-serialisation this session caused, and the file carried the
  vendor's own 2025-10-08 date. Restored from the package 2026-08-19, guid
  unchanged, and `Tools/lint-vendor/run.sh` now fails the gate if it happens again.

  Recorded because it cost a day: the magenta was read as "the pack ships a
  URP-only build", and all 24 vendor materials were rebuilt onto Built-In
  `Standard` before the on-disk graph was compared with the one in the package.
  That work is reverted. It was M87's documented fallback, taken without first
  checking the thing M87 had already checked.

## Acceptance criteria

1. Every `SkinnedMeshRenderer` in `Packs/Gatekeepers/Default.prefab`,
   `Hunter.prefab` and `Thief.prefab` has a `sharedMesh` owned by
   `SK_UtilityBot.fbx`, `SK_Hunter.fbx` and `SK_Exterminator.fbx` respectively.
2. No renderer in those three wrappers references a material outside
   `Packs/Gatekeepers/`.
3. Nothing on a Gatekeeper renders magenta, in the editor or in a Run.
4. In a Run, the three kinds are distinguishable on sight at engagement
   distance without reading their colour.
5. Locomotion and M81's Attack swing still play on all three kinds — the clips
   retarget onto the new avatars.
6. `[SceneBuilder] gatekeeper models retarget: all three avatars are Humanoid.`
   appears on a scene build, and no `pack prefab missing` or
   `gatekeeper material missing` error does.

## Implementation

**Landed 2026-08-19 (agent, Unity closed, nothing run):** the configuration.

- **Created** `Packs/Gatekeepers/{Default,Hunter,Thief}.mat` as copies of the
  three vendor materials. `Default.mat` previously held a Synty material and was
  orphaned — nothing referenced it — while `Default.prefab` pointed straight at
  `M_UtilityBot.mat` in the vendor folder, which `EnsureWrapperEmission` would
  have overwritten on the next build anyway.
- **Changed** `Packs/Gatekeepers/Default.prefab` — its three renderers now name
  `Default.mat`. This is also what `EnsureWrapperEmission` converges to on every
  build, so the prefab now agrees with the builder instead of diverging from it.
- **Changed** `Scripts/Editor/Scenes/SceneBuilder.Enemies.cs`:
  - the three source constants point at the robot prefabs, and three new model
    constants name the FBXs;
  - `WrapperLooksDressed` compares each skinned mesh's owning asset to the
    kind's current model. It previously asked only whether *a* skinned mesh
    existed, which reports dressed forever and is why swapping the source
    changes nothing on its own;
  - `MixamoRetargetsOntoSynty` is now `GatekeeperModelsRetarget` and gates on
    the three models the kinds actually use, rather than on two Synty FBXs
    nothing renders;
  - `BindAnimator` takes the avatar from the kind's own model;
  - `ApplyHunterHelmet` is gone — it toggled a Synty helmet mesh on and hair and
    head meshes off, and the robots ship one body each;
  - `PackCharacterMat` is gone. A missing kind material is now an error instead
    of being synthesised from a pack material, which is how a kind quietly ends
    up wearing another pack's atlas.

**Also landed 2026-08-19 (agent, Unity closed, nothing run): the pack renders
again.** `SG_SciFiRobotMat.shadergraph`, all 25 vendor materials and the four
`*_Emissive.png` import settings were restored byte-for-byte from
`scifirobotpack_hdrp_urp.unitypackage`. Guids match throughout, so no prefab or
material reference moved. `Packs/Gatekeepers/{Default,Hunter,Thief}.mat` are now
copies of the three vendor materials carrying the kind palette tint in both
`_EmissionColor` and `_EmissiveColor`, so the tint survives whichever spelling a
future shader uses — the same reason `EnsureWrapperEmission` writes all three.

**Still not landed. Until step 1 runs, Hunter and Ram are mid-swap: their
wrappers still hold Synty meshes and now wear the robot materials, so they will
look wrong. Run the build before judging them.**

1. Run a scene build. `EnsureWrapper` re-dresses Hunter and Ram from the robot
   prefabs — this is the step that actually swaps the two meshes, and nothing
   before it does. Nothing renders magenta after it: the Built-In materials do
   not depend on the vendor shader.
2. Optional now that the magenta is gone, and the cheapest version of it is a
   re-download. **The wrong build of the pack is what is installed.** The file imported was
   `scifirobotpack_hdrp_urp.unitypackage` — 147 assets, and exactly one shader
   in the whole archive: `SG_SciFiRobotMat.shadergraph`, URP-target only. The
   name says it. If the vendor ships a Built-In / standard-pipeline build of the
   same pack, importing that over the top replaces the graph and the 24 vendor
   materials with ones that compile here, and no hand editing happens at all.
   Check the Asset Store page for the pipeline variants before doing anything
   else.

   Failing that, open the graph in the editor, Graph Settings → Targets → `+` →
   **Built-In**, save. One graph backs all 24 vendor prefabs and all six
   material families, so it is the whole pack in one action. Deliberately not a
   text edit: grafting the target blocks in by hand needs two new `m_ObjectId`
   GUIDs, which is where the one-shot editor work rule says YAML editing stops
   being safe.

   Either way this recovers the vendor look; **neither is needed to clear the
   magenta**, which step 1 already does.

Once step 2 lands, the kind materials can be rebuilt from `M_UtilityBot.mat`,
`M_Hunter.mat` and `M_Exterminator.mat` to recover the paint-mask look and the
emission masks, which also brings the kind tint back to life.

## Verification

1. Criteria 1 and 2 — from the prefab text, no editor:

```bash
grep -A1 m_Mesh: "Assets/Packs/Gatekeepers/Hunter.prefab" | grep -oE 'guid: [0-9a-f]{32}' | sort -u
```

   Resolve each guid against `.meta` files; every one must be the kind's model.
   An unchanged build fails this, which is the point — the criterion cannot be
   satisfied without the re-dress.
2. Criteria 3 and 4 — playthrough: start a Run and let a wave of mixed kinds
   close. Nothing pink, and the three are separable before they arrive.
3. Criterion 5 — same run: watch one of each kind walk, then take a melee hit
   from each. This is also M81's play gate, so the two settle in one sitting.
4. Criterion 6 — read the scene build's console output.

## Watch

`Assets/FaxLab3D/` is a vendor dump and was fully gitignored, which is the
project's standing rule for vendor packs (`Assets/Synty/` is ignored the same
way). That is how the working UtilityBot was lost: a re-import overwrote it and
`git status` said nothing.

**Narrowed 2026-08-19.** The pack's `Materials/` and its `Textures/*.meta` are
now tracked — 102 files, 316KB — while the 534MB of textures, models, scenes and
the nested `.unitypackage` stay ignored. The materials and the Shader Graph are
the valuable, diffable part; tracking them means a re-import, or another silent
strip of the graph's targets, shows as a diff rather than as a bug report about
pink robots.

`Tools/lint-vendor/run.sh` closes the loop: it runs inside `Tools/verify/run.sh`
before compile, reads the `vendor-lint` fence in `Docs/Decisions.md`, and fails
the gate if `SG_SciFiRobotMat.shadergraph` loses its `BuiltInTarget` or goes
missing. That is the exact failure that happened, and it is the one thing no
amount of reading the materials would have caught. It adds no new verification
path — it is a sub-check of path 1, like lint-arch and lint-language. An
unimported pack is clean, so a fresh clone still passes. Proven both ways: clean
on the real tree, and it catches a seeded target strip, a deleted graph, and a
deleted fence (exit 2, not a silent pass).

It is detection, not prevention. A package import still overwrites these files.
Two consequences worth knowing: on a fresh clone the tracked `.meta` files sit
beside assets that are not there until the pack is imported, and Unity will drop
an orphan `.meta`; and if the graph is ever stripped again, restoring it means
pulling that one file out of the `.unitypackage` — which is how it was fixed here.

Unrelated but sitting in the same folder: `SciFiRobotsPack.unitypackage` is
261MB of the 534MB and lives *inside* `Assets/`, where Unity has no reason to
carry it. Deleting it is Zo's call, not this milestone's.

Repair discipline: Hunter and Ram render correctly today on Synty meshes. The
re-dress is the change; if a kind comes back wrong, the wrapper is regenerable
from its source prefab and nothing is hand-authored inside it.

## Depends on / Unblocks

Shares its playthrough with M81 (closed 2026-08-19); the swing has to keep
reading on all three bodies after the dress.
Leaves `SK_Tactical` free for M80's boss/elite ruling.
