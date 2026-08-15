# M61-2 handoff — cloud session to local (2026-08-14)

Everything in this folder mirrors its real repo path. Nothing here has been applied to the live
tree. Copy a file to the path shown below its heading to apply it.

The milestone itself is already live at
`Docs/Milestones/1. planned/M61-2-polish-pass-2-the-bastion.md` (re-cut this session, verified
by two adversarial passes). Read it first; it is the spec these files implement.

## Status of each file

| This folder | Applies to | State |
|---|---|---|
| `Assets/Scripts/Game/Bastion/BastionSegment.cs` | same path | **Unreviewed.** Written, agent died before its verification pass. |
| `Assets/Scripts/Game/Bastion/BastionSegment.Geom.cs` | same path | **Unreviewed.** Same. This is the big one (~the whole pack-mesh rewrite). |
| `Assets/Scripts/Editor/Bastion/BastionKitEmitter.cs` | same path | **Unreviewed.** Same. |
| `Assets/Scripts/Editor/Bastion/BastionSketchEmitter.cs` | same path | Verified by its author agent (diff reviewed, braces balanced, encoding preserved). |
| `Assets/Scripts/Sim/Bastion/BastionMeshMap.cs` | same path (**new file**) | Verified. Unity generates the `.meta` on import. |
| `Tools/docgen/Program.cs` | same path | Verified, compiled under mcs against stubs only. |
| `Docs/Design/4-Data.md` | same path | Verified. Insertion only, new `GKB:GEN bastion-mesh` marker pair. |
| `Assets/Synty/PolygonSciFiSpace/Materials/PolygonScifiSpace_01_A.mat` | same path | Verified. One line: `_Enable_Emission: 0` to `1`. |

No compile has been run against the real tree. No emit, no bake, no suite run.

## What the change does

Two layers per bastion segment, both under `_geom`, both `Gen_`-prefixed and marked with
`BastionGeneratedGeom`:

- **Collision layer** keeps today's exact box math (footprints, 3 m door opening, sills,
  lintels, `TryDoorPillars`, the window `NavMeshObstacle` carve) but the boxes lose their
  renderers and become `BoxCollider` only. Player physics and shot blocking are unchanged by
  construction.
- **Visual layer** is new: POLYGON Sci-Fi Space prefab instances, colliders stripped, tiled
  along each face. Floors and ceilings tile in 2D; walls, doorframes, windows and corner
  pillars tile along their edge; ceiling light panels sit on a 10 m grid inset one module from
  each edge.

Tile step is `2.5 * round(measured / 2.5)`, so pack pieces that measure ~5.08 m tile at exactly
5.0 and the overlap lips do their job. Measurement reads `sharedMesh.bounds` off the prefab
asset without instantiating, cached per prefab.

Seven pack prefab references are serialized onto `BastionSegment` (floor, ceiling, wall,
doorframe, window, corner, ceiling light) and assigned by `BastionKitEmitter` when it writes the
kit prefabs. That is deliberate: `BastionSegment.Rebuild` can run at play time and there is no
`AssetDatabase` in a player build. If any reference is null the segment logs an error and emits
no visual pieces at all, rather than falling back to primitives.

`CreateRuntimeMaterial` is deleted. It tried the URP Lit shader first, which is dead on Built-in
and fell through to `Unlit/Color` magenta. `floorMat` / `wallMat` and the `FloorMaterial` /
`WallMaterial` properties stay, because `BastionDoor.ApplySlabMaterial` reads `WallMaterial`.

`BastionSketchEmitter` gains the `Generated/` and `Authored/` split under the `BastionKit` root.
Re-emit clears `Generated/` only. `Authored/` is created if absent and never read, moved or
destroyed. A one-time migration pass moves pre-split hand-placed children into `Authored/` and
destroys old generated ones. This is the fix for the live hazard: today's emitter clears every
child of the kit root unconditionally, so a re-emit eats hand-placed work.

## What has not been done

1. **Compile.** Nothing here has seen a C# compiler against the real tree. Expect to fix
   signatures where the three unreviewed files meet the verified ones. The seam to check first
   is `BastionSegment.ResetRebuildCounters()`, `RebuildObjectsCreated` and
   `RebuildObjectsMarked`, which `BastionSketchEmitter` calls and `BastionSegment` must define.
2. **Emit and bake.** `gkb_bastion_kit_request.txt` (kit prefabs, must run first so the seven
   refs land), then `gkb_bastion_sketch_request.txt` (layout), then Bake Bastion NavMesh.
3. **Suites.** `Tools/verify/run.sh`, then `gkb_verify_request.txt` for the full bridge.
   Baseline to beat is the 2026-08-14 report: EditMode 360 pass / 1 fail
   (`BastionDoorApertureTests`, pre-existing, expects 4.5 where the code says 3.5), PlayMode
   30 pass / 1 fail (`NetVerbTests.Repair_ClientChannel`). The gate is no NEW failures.
4. **Verification steps 1 to 9** in the milestone, starting with the kill criterion: put an
   object under `Authored/`, re-emit, confirm it survived. Do that before anything else.
5. **`BastionSegmentDoorAlongTests`** asserts pillar-split internals and
   `BastionDoorApertureTests` asserts aperture size. Both should still pass since the collision
   layer math is unchanged, but they are the first tests to read if something moved.

## Project context Cursor will not have

This lived in Claude desktop project memory, not in the repo.

**Working rules Zo holds agents to:** decisions first in every response; no token theatre; no
unfalsifiable design claims (every claim needs a source, a falsifiable test, or an explicit
"unknown, needs play data"); no invented jargon; no qualitative verdicts drawn from quantitative
data; audit docs against code before trusting them, because these docs drift within days.

**Doc errors found this session and already fixed in the re-cut milestone,** listed so they do
not creep back: the kit footprints are golden-mean 5/8/13/21/47, not Fibonacci; the scene
subtree belongs to `BastionSketchEmitter`, not `BastionKitEmitter` (which writes prefab assets);
nothing emits ceilings today, so ceilings are a new emission class rather than a mesh swap; the
NavMesh bakes from render meshes, not colliders, so the mesh swap itself is the pathing risk;
leverage tier #12 is named "constants" in `Working-Rules.md`.

**Decisions Zo made on the gate (2026-08-14):** simplest path, tech debt accepted to ship the
look. Byte-identical Build All is cut outright, because Build All rewrites the NavMesh asset by
design and both M35 and M47 already waived byte-identity. The M47 path-length band was never
measured, so nothing can "still hold" against it. Deferred with triggers, in the milestone's
Do not build table: geometry-walk tests, spawn-path harness, occlusion bake, lightmap and baked
lighting, pinning the standalone quality level (it runs Ultra today and nothing sets it).

**Pack facts worth keeping:** one main atlas material, `PolygonScifiSpace_01_A`, and the Synty
shader graph carries an explicit Built-in target, so no URP conversion anywhere (M61's step 1
says convert to URP; that is wrong for this project). Fixture prefabs ship with no `Light`
components. Wall and structural pieces ship convex `MeshCollider`s about 0.9 m deep, floors ship
`BoxCollider`s, many props ship none, which is why the visual layer strips colliders wholesale.
The pack has no door-leaf prefab, only open frames, and exactly two window pieces, both
exterior-styled, with no glass.

**Repo hygiene noticed but not touched:** two pre-existing red tests (above); the menu-row fence
in `Architecture/README.md` is stale (it lists a row that does not exist, omits three that do,
and `lint-arch` enforces no menu rules at all); the `gkb-verification` skill cites
`AutoVerify.cs`, which no longer exists, the real bridge being
`Assets/Scripts/Editor/Tools/IntegrationCheck.cs`; roughly forty stray `gkb_*` report and log
files sit at the repo root.
