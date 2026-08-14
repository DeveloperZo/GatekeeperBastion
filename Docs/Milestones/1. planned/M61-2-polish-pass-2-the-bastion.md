# M61-2 — Polish Pass 2, the Bastion

<!--
Status: later
Update trigger: when a kit block or side kind gains or loses a mesh mapping, when the Generated/Authored split changes, or when the pack changes
Last updated: 2026-08-14
-->

M61 dresses the Boarder, the guns and the Ship's consoles and explicitly leaves the Bastion in
primitives. This is that half: **the M47 kit blocks stop being generated boxes and start being
POLYGON Sci-Fi Space meshes**, so every room in every bastion changes from one edit.

Same paradigm as the Ship: **generate, then author.** The difference is that the Bastion's
generator already exists (`BastionSketchEmitter` emits the scene layout; `BastionKitEmitter`
writes the kit prefabs), so this slice changes what the segments emit rather than building a
new generator.

Footprints stay locked (`M47`, `../../Decisions.md`). No new block kind, no new side kind, no
NavMesh redesign. This cut takes deliberate tech debt to ship the look fast (Zo 2026-08-14);
the debt is listed in **Do not build**, each item with its trigger.

## Story

As a Boarder, I want the bastion to look like a place someone built and then abandoned, so
that the space M47 shaped reads as architecture rather than as a diagram of architecture.

## Why it matters

**The kit is the leverage and it is already paid for.** Because M47 composed the bastion from
prefab blocks, a mesh mapping per block type propagates to every room, in this bastion and in
every bastion built afterwards. There is no per-room pass and there never will be.

**The Ship's art pass is the proof and the warning at once.** Zo 2026-08-14: the Ship polish
came out amazing, and this pass is the same class of upgrade. The same hand-placed pass also
left placement defects a generator cannot make: `Ship.unity` holds ten grid-named light
fixtures (`Light_x_z`) placed at grid points attached to nothing, and wall runs with gaps at
arm ends. Placement computed from the thing being enclosed does not have those failure
classes. And the scale rules out doing it by hand twice: the Ship's floor is 25 five-metre
tiles, ~625 m² (`Ship.unity`); the emitted bastion's block footprints (`BastionKit.cs`
multiples 5/8/13/21/47 on the 5×8 scaler, over the current sketch) total on the order of
150,000 m² — two orders of magnitude more surface.

Leverage tier (`../../Working-Rules.md`): **#12, constants.** No rule changes, no number
moves, no footprint moves. What changes is what the kit is made of.

## Kill criterion

**The paradigm is the bet, not the art.** After the first re-emit and before the end of the
first authoring session: hand-tune something in the scene, re-emit again. **If the authored
change does not survive, the Generated/Authored split is wrong and this stops until it is
fixed**, because a generator that eats your work turns into a generator nobody dares run.
Note this is not hypothetical: today `BastionSketchEmitter.EmitSketch` clears every child of
the kit root unconditionally, so the split must be built before any authoring happens, not
after.

## Acceptance criteria

### The generator measures, it does not assume

1. **The emitter derives its tile step from the pack's snap grid and checks it against
   `Renderer.bounds` read off each pack prefab**, rather than assuming a module size. The
   pack is a 5 m grid with a 2.5 m small grid, corner and end pivots, and pieces that
   measure ~5.08 m because of overlap lips (`Assets/Synty/PolygonSciFiSpace/Models/`);
   the doorframe is narrower than a plain wall. So the step is the snap size and the
   measured bounds are the sanity check, not the other way round. The kit's golden-mean
   footprints (5 / 8 / 13 / 21 / 47, `BastionKit.cs`) are the truth; the pack's piece sizes
   are discovered. The emit logs, once per piece type, nominal step and measured bounds.
2. **Every block kind and side kind resolves to a mapping**, stated as data in one table:
   Quarter / Half / Full / Open Area / Control Room, crossed with Wall / Door / Window /
   None, plus floor and ceiling. A kind with no mapping fails the emit loudly rather than
   emitting a primitive. Ceilings are a new emission class — today no segment emits one.
   The mapping this slice ships (one structural set for every block kind; per-kind visual
   identity is Zo's authoring pass, see the open item below):

   | Cell | Prefab |
   |---|---|
   | Floor | `SM_Bld_Floor_01` (5 m), `SM_Bld_Floor_Small_01` (2.5 m remainder) |
   | Ceiling | `SM_Bld_Ceiling_01` |
   | Wall | `SM_Bld_Wall_01` |
   | Door | `SM_Bld_Wall_Doorframe_01` |
   | Window | `SM_Bld_Wall_Exterior_Window_01` |
   | None | nothing emitted |
   | Wall corner (where two wall faces meet) | `SM_Bld_Wall_Corner_Pillar_01` |
   | Ceiling fixture | `SM_Prop_Light_Panel_01` |

### Closure and mounting are computed, never typed

3. **Every segment face marked `Wall` produces a continuous surface across that whole face**,
   with no gap at a neighbour boundary and no piece extending past the footprint.
4. **A face marked `Door` produces an opening a Gatekeeper can path through** (the pack has
   doorframes, no door leaves — an open frame satisfies this), and `Window` produces an
   opening sight passes and walk does not, matching `M47`'s side table. The gatekeeper
   walk-block stays the `NavMeshObstacle` carve the primitive window uses today; the pack's
   two window pieces have no glass pane, so nothing about the mesh blocks anything.
5. **Fixtures mount to surfaces.** Lights attach to the emitted ceiling plane, wall props to
   wall faces. **No fixture is placed at a grid coordinate in open air.**

### Generated and authored do not overlap

6. **The emitter owns exactly one subtree and never writes outside it.** `Generated/` under
   the kit root is emitted and may be destroyed and rebuilt at any time. `Authored/` beside
   it is Zo's and the emitter must not read, move or delete anything in it — it creates the
   node if absent and then leaves it alone forever. This replaces today's clear-everything
   behaviour.
7. **A re-emit preserves authored content**, proved by the kill-criterion procedure: author,
   re-emit, confirm the authored object is still there, still positioned, still bound.
8. **`BastionGeneratedGeom` marks everything the emitter creates** — geometry and the
   sketch-emitted markers alike — so the split is inspectable rather than conventional, and
   a stray hand-placed mesh inside `Generated/` is findable. The emit asserts and logs the
   count: objects created equals objects marked.

### Framerate is protected at emit time, not optimised later

**This project is Built-in Render Pipeline** (`GraphicsSettings.asset`,
`m_CustomRenderPipeline: {fileID: 0}`). Static batching and emissive-instead-of-realtime are
flags the emitter sets while it places a piece; setting them now is free, retrofitting means
touching every emitted object.

9. **Emitted geometry is marked static** (Batching, Occluder, Occludee, Navigation where it
   applies — Lightmap deferred with baked lighting, see Do not build), **and shadow casting
   is off for props, greebles and fixtures**, on for walls, floors and ceilings only.
10. **The emitted bastion uses the pack's shared atlas materials and nothing instantiates a
    material.** The pack runs on one main atlas material (`PolygonScifiSpace_01_A`); a
    per-instance material silently kills batching and is the most common way this goes
    wrong.
11. **Fixtures are emissive meshes, not real lights.** At most one realtime light in the
    bastion. The pack's fixture prefabs ship with no `Light` components
    (`SM_Prop_Light_Panel_01` and kin), and the atlas material already wires an emissive
    map but ships `_Enable_Emission: 0` — one flip on the shared material, done once, and
    batching survives.
12. **Colliders are box colliders authored on the kit segment; pack colliders are stripped
    at emit.** The pack's wall and structural pieces ship convex `MeshCollider`s ~0.9 m
    deep for a visually thin wall (`SM_Bld_Wall_01`); floors ship `BoxCollider`s and many
    props ship none. Unstripped, the proud hulls narrow doorways and windows against player
    movement and gunfire — see Watch.
13. **Frame time, batches and set-pass calls are recorded before and after** in
    `../../GatekeeperZo Notes/playthrough.md`, read from the Stats window standing in the
    largest space, at 1080p on Zo's machine both times. No budget ceiling this slice — the
    numbers are the baseline the next pass argues from. Batch count is the diagnostic;
    frame rate alone says something is wrong without saying what.

### Nothing regresses

14. **The NavMesh bakes and a played wave arrives.** The bake collects render meshes, not
    colliders (`SceneBuilder.BakeNavMesh`, `NavMeshCollectGeometry.RenderMeshes`), so
    swapping every mesh in the bastion changes the NavMesh directly. Proof is playing:
    waves reach the Hatch, a solo run completes.
15. **No new suite failures.** Baseline is the 2026-08-14 report: EditMode 360 pass / 1 fail
    (`BastionDoorApertureTests` — pre-existing, expects 4.5 where the code says 3.5),
    PlayMode 30 pass / 1 fail (`NetVerbTests.Repair_ClientChannel`). If the door-aperture
    test breaks *differently* because the geometry changed, re-cut it to the new geometry
    here; do not let this milestone add reds of its own.
16. **No new menu row** (`../../Architecture/README.md`); emit, bake and Build All run
    through the existing rows and sentinel files.

## Implementation

**Change** `BastionSegment.Geom.cs` — from building primitives to instantiating mapped pack
prefabs, tiled at the snap step from criterion 1, ceilings included. Strip pack colliders
and add the segment box colliders here; set static flags and shadow modes here. The segment
rebuilds from `OnEnable`/`OnValidate` at runtime, so pack prefab references must be
serialized on the segment or its kit prefab — there is no `AssetDatabase` in a player build.
The prefab-freeze gotcha (`../../Architecture.md`, gotcha 1) applies to this step directly.

**Delete** `BastionSegment.CreateRuntimeMaterial` — it exists to colour primitives, tries
the URP Lit shader first (dead on Built-in, falls through to `Unlit/Color` magenta), and a
runtime-created material is exactly what criterion 10 forbids. Pack prefabs bring their own
shared materials. Any surviving caller is a finding, not a reason to keep it.

**Create** the mapping table as data, one row per block kind and side kind, generated into
`4-Data.md` in the `M33` docgen pattern — note that is a new named block in
`Tools/docgen/Program.cs`, a code extension, not a config row.

**Change** `BastionSketchEmitter` — emit under `Generated/` only, create `Authored/` if
absent and then leave it alone forever. Re-emit clears `Generated/` only, replacing today's
unconditional `ClearChildren` of the whole kit root. (`BastionKitEmitter` writes the kit
prefab assets and is untouched by the subtree change.)

**Change** `Assets/Synty/PolygonSciFiSpace/Materials/PolygonScifiSpace_01_A.mat` — flip
`_Enable_Emission` to 1 once; the emissive map is already assigned.

**Re-emit**, rebake the NavMesh, then hand the scene to Zo for the authoring pass. That
order is the whole point: the generator guarantees the structure, the human does the taste.

Open: **per-kind visual variety** (wall variants 02–06, Mechanical, Pods, the Bridge set for
Control Room). Owner: Zo, during the authoring pass. Trigger to extend the table with
per-kind variants: the one-piece-per-cell uniformity reads bad in the authored scene.

Rejected: **hand-placing pack pieces in the Bastion scene.** The Ship at ~625 m² already
showed the defect classes; the bastion is two orders of magnitude larger.

Rejected: **byte-identical Build All ×2** (Zo 2026-08-14). Build All rewrites the NavMesh
asset by design; `M35` and `M47` both explicitly waived byte-identity. A cut, not a
deferral.

Rejected: **new block kinds or footprint changes.** M47 locked those and this is a dressing
pass. A shape the kit cannot say is a finding about the kit, filed, not solved here.

Rejected: **a second art pack.** One source (`Synty 138857`) for the Ship and the Bastion,
per M61. Mixed sources look worse together than one source used everywhere.

Rejected: **NavMesh redesign, new segment sides, or lighting design.** Fixtures mount where
the geometry says; how the bastion is lit is a later pass if it needs one.

## Verification

1. Criteria 6, 7 first, using the kill-criterion procedure: place an object under
   `Authored/`, re-emit, confirm it survives untouched. Nothing else matters if the
   workflow eats work.
2. Criterion 2 — seed one kind with no mapping row, emit, expect the loud failure naming
   the kind; restore the row. A loud fail that has never fired is a claim, not a check.
3. Criteria 1, 8 — read the emit log: per-piece-type step/bounds lines, and the
   created-equals-marked count.
4. Criteria 3 to 5 — stand in every block kind and look: no sky through walls, no gaps at
   neighbours, no floating fixtures. Closed by a dated line per block kind in
   `playthrough.md`, screenshots for anything questionable — eyes this slice, but recorded
   eyes.
5. Criteria 9 to 12 — grep the emitted scene and kit prefabs (YAML is text):
   `m_StaticEditorFlags` nonzero on generated geometry, `m_CastShadows: 0` on
   props/fixtures, every renderer's material GUID is the pack atlas, zero `!u!108` (Light)
   under `Generated/`, no `MeshCollider` under `Generated/`, `BoxCollider` on segments.
6. Criterion 13 — Stats window in the largest space, numbers into `playthrough.md`, before
   the swap and after, 1080p on Zo's machine.
7. Criterion 14 — bake, then play a Run: waves arrive at the Hatch, solo run completes.
8. Criteria 15, 16 — `gkb_verify_request.txt`, compare `gkb_verify_report.json` to the
   2026-08-14 baseline; no new names in the failure list.
9. Playthrough path — from Play: MainMenu → Ship → EMBARK → walk one full bastion floor.
   Confirms the dressed kit, mounted fixtures, and a wave that still arrives.

## Watch

- **Baked lighting fights generate-then-author.** Iterate with realtime plus light probes
  and accept that it looks flat; see Do not build.
- **M61 step 1 says convert the Boarder materials to URP. This project is Built-in.** The
  Synty shader graph carries an explicit Built-in target, so the correct action is no
  conversion at all — for the Boarder and for this pass alike.
- **Colliders are the silent regression, twice over.** Unstripped convex hulls sit ~0.9 m
  proud of a visually thin wall and narrow openings against the player and gunfire. And
  since the NavMesh bakes from render meshes, the mesh swap itself can close a gap
  gatekeepers pathed through — the symptom is a wave that never arrives rather than an
  error. Criterion 12 handles the first; verification 7's played wave is the net for the
  second.
- **Half-dressed reads worse than undressed.** A textured segment beside a primitive one
  looks broken in a way two primitives do not. Map every kind or map none — criterion 2's
  loud failure enforces this.
- **Window is the thin cell.** The pack has exactly two window pieces, both exterior-styled.
  Workable, but it is the one cell that may read wrong visually; if it does, that is a
  finding about the pack, filed, not a new kind invented here.
- **The authored subtree will get written to eventually**, by someone in a hurry or by an
  agent that did not read this file. Criterion 8's marker is what makes that findable
  rather than mysterious.

## Do not build

Debt taken on purpose (Zo 2026-08-14), each with its trigger:

| Deferred | Trigger to do it |
|---|---|
| Automated geometry-walk tests (closure, mounting, flags) | The recorded eyeball pass in Verification 4–5 misses a defect that ships |
| Spawn-path harness / path-length band (also unbuilt in M47) | A wave fails to arrive and the cause is not visible in the scene |
| Occlusion culling bake | Zo calls the after-numbers in `playthrough.md` unacceptable; bake writes scene data, so sequence it after authoring settles |
| Lightmap static flag + baked lighting | The flat look stops being acceptable; bake once when the shape is frozen, never per emit |
| Pinning the standalone quality level (runs Ultra today, nothing sets it) | A quality-dependent defect or cost shows up in play |

## Depends on / Unblocks

**Depends on `M47`'s kit** and on the pack imported for `M61` (already on disk at
`Assets/Synty/PolygonSciFiSpace/`; M61 itself is still planned, not shipped — this slice
does not wait on it). Does not depend on the Ship generator, though both exist for the same
reason.

**Unblocks** the first friends session being a judgement of the game rather than of its
placeholders, which `M61` starts and this finishes. Capsule Gatekeepers in a dressed room
are `M68`.
