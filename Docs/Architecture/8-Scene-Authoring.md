# 8 — Scene Authoring

<!--
Purpose: How scenes are structured so an agent can work on one part without breaking the rest, and what a scene edit must assert.
Status: working
Update trigger: when a scene root is added or renamed, when UI moves out of a gameplay scene, or when a swap rule changes.
Last updated: 2026-08-30 (Build All no longer rebuilds the Ship; HatchRecallPad named on
T2/T5 Portal; one-deck hull re-counted)
-->

The Ship hub is **`Ship - CoOp.unity`** — one hub, solo and party alike. Historical notes
below that say `Ship.unity` or `Ship - Demo.unity` mean hubs that no longer exist (the
scene pair consolidated onto the co-op hull); the authoring rules still apply.

Seven scenes are on disk as of 2026-08-30, all under `Assets/Scenes/`. Six are registered in
build settings, in this order: `Splash`, `MainMenu`, `RealmSelect`, `Ship - CoOp`,
`Bastion_Coop`, `Range`. The play chain runs `Splash → MainMenu → RealmSelect → Ship - CoOp
→ Bastion_Coop → Ship - CoOp`. `Bastion.unity` and `Ship - Demo.unity` are gone with the
rest of the solo pair.

**`Explore - Kit.unity` is the seventh.** It is in neither `SceneFlow` nor build settings.
Its scripts live in `Assets/_Scratch/`. `Docs/Release-Gate.md` calls it an authoring scene
that is not in the build; `Docs/Working-Rules.md` allows emitting a sketch into it. Treat it
as scratch; do not wire it into the play chain.

## What SceneBuilder owns

**Build All owns three root names and one asset; it does not rebuild the Ship.**
`OwnedRootNames` is `Environment`, `SpawnPoints` and `Directional Light`; the NavMesh asset
is the fourth owned thing. `BuildAllCore` has not called `RebuildShipGeometry` since
2026-08-26 — that method is retired and refuses if reached. On Bastion, kit `BastionSegment`s
under `Environment` skip the geometry wipe, so a Build All rebakes NavMesh only.

Stations, the player rig, `ShipController`, `GameSystems`, the `EventSystem` and every UI
canvas are authored content it never touches. `ShipDress` is not an owned root. Ship - CoOp
has no `SpawnPoints` root; Bastion's `SpawnPoints` sits empty under `Environment`. The
SpawnRoom wipe-skip still lives in `ClearOwnedRoots`, which only the retired ship rebuild
would call.

**Field recall pads are authored scene roots, not runtime meshes.** `T2 Portal` and
`T5 Portal` on `Bastion_Coop.unity` carry `HatchRecallPad` and the Ship-teleporter graybox
(`Pad` / `Emitter` / `Glass`). Hold Interact teleports to the Hatch.

Dock `HatchPortal` stays on `Hatch Portal` only. Changelog 2026-08-30 removed the runtime
cyan squares; do not spawn a stand-in. `RecallPadSceneTests` pins the two roots and the
component.

**It pins five scenes and keeps the rest.** `SceneBuilder.BootScenes` is Splash, MainMenu,
RealmSelect, Ship - CoOp, Bastion_Coop, and `MergeBuildScenes` puts those at the head of the
build list while keeping every other registered row after them with its enabled flag intact —
which is why `Range` survives a Build All it is not named in. The only rows it removes are
ones whose file is gone, so the list self-heals after a scene is deleted, and both the kept
and dropped counts are logged. `SceneBuilder.cs`'s class comment still names two deleted
scenes and a `ShipDress` skip that `BuildAllCore` no longer performs; read the code, not the
comment.

**Look-pass and Protocol Bar are separate from Build All.** `GatekeepersProtocol/Build/Ship Look
Pass` (Bake/Ship Look Lighting beside it) reads `ShipDress` for wall shapes and writes a
`Ship Look` root from `Assets/Settings/ShipLookProfile.asset`, replacing that root only.
`GatekeepersProtocol/Build/Protocol Bar` authors the Bastion protocol HUD and a
`Protocol Card Deck` into the CREW QUARTERS canvas. Neither wipes owned roots.

## Why this file exists

`Ship.unity` broke on 2026-08-14 while an agent was doing M61's console swap. The eight
placeholder `Desk` and `Screen` cubes on the four station roots were set inactive, per M61
criterion 4, and the Synty replacements were never parented in. The stations were empty, the
suite stayed green, and nothing said so.

**The geometry was never damaged.** No missing meshes, no null materials, no bad transforms,
no broken prefab links. Eight boolean flags. But finding that took a full parse of a 3.9 MB
file, which is the actual problem this file addresses.

## The measurement that explains it

`Ship.unity`, as measured 2026-08-14 — the scene the M61 break happened in, kept here
because it is the evidence for the rules below:

| | count |
|---|---|
| GameObjects total | 958 |
| Under `Canvas` (Shift station panels) | 844 |
| The 3D ship | 57 |
| MonoBehaviours | 1707 |
| MeshRenderers | 38 |

**The ship is 6% of its own scene.** An agent asked to change a console is navigating a file
where fifteen out of every sixteen objects are UI chrome it must not touch. That is not an
agent limitation to work around; it is a layout problem to fix.

**The hull made it worse.** `Ship - CoOp.unity` measured **8.0 MB** and **2,784**
GameObjects against 3,119 MonoBehaviours, 1,023 MeshRenderers and three `Canvas` roots on
2026-08-25 — the two-deck hull, about three times the object count of the scene measured
above.

Re-counted 2026-08-30 after the one-deck collapse: **7.0 MB**, **2,381** GameObjects,
3,163 MonoBehaviours, 632 MeshRenderers (Unity YAML `--- !u!1 &` / `!u!114` / `!u!23`; file
size from disk). `Bastion_Coop.unity` is 6.3 MB and 2,070 GameObjects (was 12 MB / 4,103 on
2026-08-25). The hub is still an order of magnitude past the size an agent can hold. The
UI-versus-ship split is not measured here; counting it needs a Transform-id hierarchy walk,
not a GameObject-id grep.

## Rules

### 1. A gameplay scene holds placement, not content

Anything that has to be *authored* belongs in a prefab. The scene holds instances and
transforms. Then changing what a thing looks like is a prefab edit with local scope, and the
scene diff is a reference swap.

**Bastion rooms are the exception.** Kit prefabs (`Assets/Prefabs/Bastion/Kit`) generate a
layout quickly. Once a room sits under `Environment/BastionKit/Authored/`, it is unpacked
completely — unique scene geometry, not a prefab instance. Emit unpacks `Completely`;
Rebuild skips `Authored/` so a re-stamp cannot replace hand edits.

### 2. UI does not live in a gameplay scene

844 of `Ship.unity`'s 958 objects were the four station panels. Those belong in prefabs
instantiated at bind, or in a UI scene loaded additively. **Target: the Ship hub under 100
objects**, small enough for an agent to hold entirely. `Ship - CoOp.unity` was at **2,381**
on 2026-08-30, so the target is further away than when it was written.

Until that lands, treat the `Canvas` subtrees as off-limits for any task whose subject is the
ship itself, and say so in the task.

### 3. One subtree, one task, one commit

A scene task names the subtree it may touch and touches nothing else. "Revamp the ship" is not
a task. "Replace the four `Stations/Station_*/Desk` meshes" is.

The existing hierarchy is already segmented correctly and should be preserved rather than
redesigned:

`Ship - CoOp.unity`'s roots, read off the scene 2026-08-25. The one-deck collapse of
2026-08-26 changed what is inside `Environment` and `ShipDress`; the root set is the same:

```
Environment/       the hull graybox on an ix/iz grid: GFloor_*, GW_*, F_*, W_*, C_*.
                   Renderers disabled — this root is where collision comes from
Hatches/           Hatch_1..3, on the starboard hull
Directional Light  the scene's only Light
Stations/          Station_{Armory,CrewQuarters,Fabrication,Map}/{Desk,Screen,Interact,Label}
ShipDress/         the visible Synty geometry in per-room containers; colliders disabled
Player/            PlayerCamera
Canvas/            the station panels — off-limits under rule 2
```

Station and dress names are unique and path-addressable, which is what makes a subtree
instruction possible. Do not rename toward generic names. Other authored roots sit beside
that list (`ShipController`, `Demo Teleporter`, spawn markers, `Ship HUD Canvas`,
`Level Music`) and are not hull. A look-pass would add a `Ship Look` root; that root is not
in the scene until `GatekeepersProtocol/Build/Ship Look Pass` runs.

**`Environment` and `ShipDress` belong to `Tools/ship-hull/build_hull.py`, not to
SceneBuilder.** This is the correction that matters most on this page. The earlier wording
here called those names SceneBuilder-generated and told the reader to address the subtree
through the builder, which points an agent at the one tool that would destroy the ship.

`SceneBuilder.Ship` still compiles as the pre-M101 graybox, it has no menu row, and Build All
does not call it. Re-wiring it would wipe `Environment` and move the hatches. It produced
the twenty wall seams and the missing exterior skin that `M101` was cut to fix.
`Docs/Milestones/3. completed/M101-a-ship-with-two-decks-and-a-stair.md` and
`Docs/Working-Rules.md` both say do not run it.

`build_hull.py` is destructive by design too. It deletes every child of `Environment` and
`ShipDress` and re-emits the hull from its layout table, and it rehomes `Hatch_1..3`, the
four stations, the `Ship Terminal`, the teleporter, the `Spawn_*` objects and the
realm-record groups on the way past. That is the right tool and still not a chore: the diff
it makes is ~386k lines and cannot be read, so any hand pass landed since its last run has
to be folded back into the script first or it disappears silently. Address those two
subtrees by editing that script; address everything else by name.

### 4. A swap asserts the arrival, not the departure

This is the rule the failure was missing. M61 criterion 4 said the cubes must be *inactive or
deleted*. It was satisfied. Nothing said a console must be *present*.

**Every replacement states what must exist afterwards**, and the check asserts that. The
general form: after removing X, assert Y renders. The specific one owed here: **every
`Stations/Station_*` root has at least one enabled renderer among its descendants.** One
assertion in `ShipSceneContractTests`, and this class of break cannot survive a suite run.

**Still owed as of 2026-08-25.** `ShipSceneContractTests` asserts one of each station type,
an enabled `Collider` under each, a live player rig, and something that draws the interaction
prompt. It asserts nothing about renderers, so the exact M61 break would still pass.

This is the same shape as two other defects on record: M52 plus M53 gated a run on two player
outcomes and asserted neither's failure branch, and M61 criterion 8 asserts five animation
clips exist while three of them are duplicates of the other two. **Presence of the old thing
going away is not evidence of the new thing arriving.**

### 5. A manifest, so planning does not require parsing YAML

Generate a text listing of hierarchy path plus component types per scene, refreshed on build.
An agent plans against sixty lines instead of megabytes, and a diff of the manifest is a
readable summary of what a scene change did. This is the cheapest single item here and it is
what most directly answers "agents cannot edit large scenes."

### 6. Scene damage has a short checklist

When a scene is suspected broken, look for these before assuming worse. All are greppable in
the YAML and all were checked on 2026-08-14:

- `m_IsActive: 0` on things that should render
- `m_LocalScale` at or below zero, or absurdly large
- positions far from the origin, or NaN
- `m_Mesh: {fileID: 0}` or a materials list of `{fileID: 0}`
- `guid: 00000000000000000000000000000000` for a missing asset reference

Only the first fired. It is worth running the whole list anyway, because it converts "the ship
is fucked" into a specific number of specific objects in about a minute.

### 7. Scene edits are recoverable, so commit before an agent starts

Force Text serialization means every scene is a diff. An agent turned loose on an uncommitted
scene removes the cheapest possible recovery. Commit first, then let it work.

## What this does not cover

Whether the result looks good. These rules keep a scene structurally intact and keep an agent's
blast radius small. They say nothing about whether the console is the right console, and that
still needs eyes.
