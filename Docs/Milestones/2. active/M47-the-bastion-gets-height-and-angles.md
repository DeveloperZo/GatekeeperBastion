# M47 — The bastion gets height and angles

<!--
Status: next (current)
Update trigger: when the scaler, the block multiples, or a side-state rule changes
Last updated: 2026-08-12
-->

**Two outputs.** The **artifact** is the palette — five golden-mean segments with per-side
Wall / Door / Window / None. **Done** is proved by **producing a bastion with that palette**:
`Bastion.unity` authored from kit instances, playable, meeting the shape and sightline gates
below. Prefabs on disk without a bastion do not close M47.

## Story

As the developer, I want a kit of tunable room segments, so that a bastion is composed rather than
freehanded — and as the player, I want the floor I fight on to be a thing I learn and use, so that
where I stand is a decision.

## Why it matters

**Play made the need visible.** Zo, 2026-08-11: firing lanes make the player very effective;
corners give gatekeepers travel time unshot. The current 30 × 52 room is a shooting gallery with
decoration. The emplacement fantasy needs positions that differ.

**The kit is the multiplier; the bastion is the proof.** One vocabulary for this destination and
later ones (`../../Brainstorms/missions-and-destinations.md`). Side state is the fast tune;
obstacles are the remaining authoring. Closing on the palette alone would ship a tool nobody has
used to make the space.

Leverage tier (`../../Working-Rules.md`): **#10, structure.**

## Building blocks (the palette)

A **scaler** is a 5 × 8 rectangle. Every room footprint is a golden-mean multiple of that scaler.
Tweak the scaler to retune the whole kit; add a table row when another multiple is needed.

| Block | Multiple | Footprint (scaler 5×8) |
|---|---|---|
| **Quarter room** | 5 | 25 × 40 |
| **Half room** | 8 | 40 × 64 |
| **Full room** | 13 | 65 × 104 |
| **Open Area** | 21 | 105 × 168 |
| **Control Room** | 47 | 235 × 376 |

There is no connector kit. Each segment has **four sides**, each **Wall / Door / Window / None**.

| Side | Sight | Walk |
|---|---|---|
| **Wall** | blocked | blocked |
| **Door** | open | open |
| **Window** | open | blocked |
| **None** | open | open |

**Storey height is a static 5.** Shared faces: each segment draws its own side; double walls are
fine for graybox. Side = Door is an authored aperture; **locked** vs **open** is authoring (and
later runtime), not a sixth side kind.

## Map heuristics (generate + author)

Layout is half generated segments and half authored placement of **H / S / T / D** and unlock
order. Zo's sketch (2026-08-11): defend at **H**, branch through Full/Half rooms with **T** and
**S**, locked **D** gates between Open Areas, **Control Room (47) unlocked last**. Unlock order
is authored — the Control Room may sit near the Hatch in space and still be unreachable until
late doors open.

| Mark | Meaning |
|---|---|
| **H** | Hatch — the defend point (`HatchStation`). One per bastion. |
| **S** | Spawn point. Behind a lock = nominal when that wing is live; else open-wing spawn. |
| **T** | Terminal. Player use pressures tied **S** and contributes to unlocking a **D**. |
| **D** | Locked door (Door side that starts sealed). Opens when its terminal set is done. |

**Rules**

1. **H is the stake.** The player defends it; approaches lead toward it.
2. **Open wing:** terminals the player can reach without a lock. Completing them opens one or more
   **D** and causes spawns at the **S** tied to those terminals.
3. **Locked wing:** **S** behind a closed **D** may still be nominal spawn sources once that door's
   stage is live; until then they are sealed with the wing.
4. **Progression is unlock order, not distance.** A graph of T→D→T→D… ends at the **Control Room
   (47)**. Reordering which terminals open which doors changes the path without moving rooms —
   Master Control can be adjacent and still last.
5. **Palette sizes on the spine:** Open Areas (21) as hubs / defend / staging; Full (13) for
   terminal rooms; Half (8) for spawn alcoves; Control (47) once, last in the unlock graph.
   Quarter (5) as stubs. Break bilateral symmetry even when the sketch is mirrored.

**Generate → tune → author:** agent seeds segments (sketch emit), sides, H/S/T/D, unlock graph,
and named obstacles; Zo hand-moves for finish; bake NavMesh. Full workflow in
`../../Working-Rules.md` (*Bastion authoring*). Build All never regenerates playable geometry.

Runtime terminal interact (Ship console reuse, wave-on-locked-T, Master terminal) and delivery
banking remain `../../Brainstorms/missions-and-destinations.md`. M47 ships the unlock **graph**,
markers, and sketch→kit emit so that loop has a place to land.

### Sketch emit

Write `Tools/bastion-sketch/*.txt`, then create `gkb_bastion_sketch_request.txt` at the project
root (path to sketch, or empty for default). Emits kit rooms + H/S/T/D under `Environment`
(with `BastionLayout`). Obstacle seed: menu **Place Bastion Obstacles** or
`gkb_bastion_obstacles_request.txt`. NavMesh: **Bake Bastion NavMesh**.

## Acceptance criteria

### The palette (artifact)

1. **`Assets/Prefabs/Bastion/Kit/` holds exactly one prefab per table row** (five; ≤10 if a row is
   added in the same change). Names: `Quarter`, `Half`, `Full`, `OpenArea`, `ControlRoom`.
2. **Each prefab is a `BastionSegment`** with four sides independently Wall / Door / Window / None;
   meshes update on change without rebuilding the footprint.
3. **Every footprint equals `multiple × scaler`.** EditMode test; `StoreyHeight` is **5**.
4. **Side semantics match the sight/walk table** (Window blocks walk, opens sight; Door opens both).
5. **An emitter can write or refresh the five prefabs** without Build All (sentinel
   `gkb_bastion_kit_request.txt` or batch `-executeMethod`). No menu row unless asked.

### A bastion produced from it (verification that M47 is done)

6. **`Assets/Scenes/Bastion.unity` playable geometry is kit instances** (or frozen meshes from
   them). Hierarchy names **H**, **S***, **T***, **D*** (or equivalent clear marks) and a
   **Control Room** segment. Bootstrap of today's single room is gone at close.
7. **Unlock graph authored:** Control Room is last; at least two locked **D** stages before it;
   at least one open-wing **T** pair (or set) that opens the first **D**. Documented in the scene
   (component, ScriptableObject, or named hierarchy) so order can change without moving rooms.
8. **`SceneBuilder` no longer writes bastion walls, floors or ribs.** Spawns, NavMesh, light,
   wiring only. `grep` finds no bastion `Box(` in `Editor/Scenes/`.
9. **Build All twice leaves bastion playable geometry byte-identical.**
10. **`../../Decisions.md` and `../../Architecture.md` §4 amended** in the same commit as the
    freeze — kit vocabulary; authored geometry; no freestyle floor plates.
11. **At least two levels**, three Gatekeeper-pathable vertical routes; upper and lower have
    asymmetric approach sightlines. Not bilaterally symmetric.
12. **Long firing lane and corner-heavy approach** both exist, from side state and obstacles,
    named in the hierarchy.
13. **No spawn in line of sight from the Hatch bay** (raycast grid; failing ray names positions).
14. **First two seconds of Gatekeeper travel out of sight of the Hatch bay.**
15. **No standing position sees every approach.**
16. **Spawn path lengths to Hatch within ±25% of median**; all `PathComplete` for spawns that are
    reachable under the *fully unlocked* graph (locked wings excluded until their **D** would open).
17. **A Run plays end to end** on the kit bastion (locked doors may stay authored-open for this
    proof if runtime unlock is not built yet — say so in the playthrough note). Suites pass.
    **No new counter / accumulator / stats object.**
18. **M37 medians move** (`hatchDist`, `spawnDist`) before vs after, recorded in
    `../../GatekeeperZo Notes/playthrough.md`. If they do not, record that finding.

## Implementation

1. **Create** palette (`BastionKit`, segment, side enum, emitter, footprint tests).
2. **Compose** a bastion from the heuristics: Open-Area defend hub with **H**, Full/Half wings with
   **T**/**S**, locked **D** spine, **Control Room** last in the unlock graph; set sides;
   obstacles; bake NavMesh.
3. **Author** the unlock graph (order can change without moving rooms).
4. **Strip** bastion `Box(` from `SceneBuilder`; amend Decisions and Architecture.
5. **Park** emitter under `_to_delete/` if it should not survive (or keep as kit refresh only —
   not Build All).
6. **Editor check** for criteria 13–16. Ask before a menu row.

Rejected: closing on prefabs alone without a produced bastion.

Rejected: a separate connector prefab family.

Rejected: Build All regenerating playable geometry.

Rejected: delivery banking / siphon replacement here.

## Verification

1. Criteria 1–5 — prefabs, side toggles, footprint tests, emitter.
2. Criteria 6–7 — H/S/T/D marks and unlock graph; Control Room last.
3. Criteria 8–10 — SceneBuilder strip; Build All idempotent; docs amended.
4. Criteria 11–12 — hierarchy and levels.
5. Criteria 13–16 — raycast / path editor check.
6. Criterion 17 — Run end to end; suites.
7. Criterion 18 — `Tools/wave-report` before/after.

**M47 is done when a bastion produced from the palette passes 6–18.** Palette alone is incomplete.

## Watch

- **Empty volume** — sides and obstacles make large footprints legible.
- **Criterion 12 will be argued down** — "mostly not visible" is not the rule.
- **The kit tempts symmetry** — fails criterion 10/14.
- **Arrival rate falls when travel time rises** — game gets longer.
- **Hunters outside approach design** (Zo, 2026-08-11).

## Depends on / Unblocks

**Depends on M37** for criterion 17. Palette work does not wait on it.

**Unblocks** `../../Brainstorms/missions-and-destinations.md` — delivery, field resources, spawn
alteration, path display need a space worth moving through, and the palette to build the next one.
