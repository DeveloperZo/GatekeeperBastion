# 8 — Scene Authoring

<!--
Purpose: How scenes are structured so an agent can work on one part without breaking the rest, and what a scene edit must assert.
Status: working
Update trigger: when a scene root is added or renamed, when UI moves out of a gameplay scene, or when a swap rule changes.
Last updated: 2026-08-22
-->

Current Ship hubs: **`Ship - Demo.unity`** (solo / transitional) and **`Ship - CoOp.unity`**
(destination hub). Historical notes below that say `Ship.unity` mean the pre-rename hub;
the authoring rules still apply to whichever hub you edit.

## Why this file exists

`Ship.unity` broke on 2026-08-14 while an agent was doing M61's console swap. The eight
placeholder `Desk` and `Screen` cubes on the four station roots were set inactive, per M61
criterion 4, and the Synty replacements were never parented in. The stations were empty, the
suite stayed green, and nothing said so.

**The geometry was never damaged.** No missing meshes, no null materials, no bad transforms,
no broken prefab links. Eight boolean flags. But finding that took a full parse of a 3.9 MB
file, which is the actual problem this file addresses.

## The measurement that explains it

`Ship.unity`, as measured 2026-08-14:

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

844 of `Ship.unity`'s 958 objects are the four station panels. Those belong in prefabs
instantiated at bind, or in a UI scene loaded additively. **Target: `Ship.unity` under 100
objects**, small enough for an agent to hold entirely.

Until that lands, treat the `Canvas` subtree as off-limits for any task whose subject is the
ship itself, and say so in the task.

### 3. One subtree, one task, one commit

A scene task names the subtree it may touch and touches nothing else. "Revamp the ship" is not
a task. "Replace the four `Stations/Station_*/Desk` meshes" is.

The existing hierarchy is already segmented correctly and should be preserved rather than
redesigned:

```
Environment/     Floor_Common, Floor_{N,S,E,W}, Wall_{side}_{relation}ODoor, Alcove_{side}_{dir}
Stations/        Station_{Armory,CrewQuarters,Fabrication,Map}/{Desk,Screen,Interact,Label}
ShipDress/       Alcove_{North,South,East,West}, CommonRoom
Player/          PlayerCamera
```

Names are unique, compass-anchored and path-addressable. That is what makes a subtree
instruction possible. Do not rename toward generic names.

### 4. A swap asserts the arrival, not the departure

This is the rule the failure was missing. M61 criterion 4 said the cubes must be *inactive or
deleted*. It was satisfied. Nothing said a console must be *present*.

**Every replacement states what must exist afterwards**, and the check asserts that. The
general form: after removing X, assert Y renders. The specific one owed here: **every
`Stations/Station_*` root has at least one enabled renderer among its descendants.** One
assertion in `ShipSceneContractTests`, and this class of break cannot survive a suite run.

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
