# M73 — The spawn point is a room

<!--
Status: later
Update trigger: when the closet prefab, enemy-only door rule, or the six hung edges change.
Last updated: 2026-08-16
-->

Gatekeepers instantiate inside a hung closet and walk onto the fight floor through a
GateSlab that only they open. The Boarder cannot walk in. `SpawnPoint` stays the marker;
the prefab is what you place.

## Story

As a Boarder, I want Gatekeepers to walk out of a room I cannot open, so that a group
arrives through a shut slab rather than appearing on the floor I am standing on.

## Why it matters

Every group of every Wave currently Instantiates on the fight floor. `S_S1` in
`Assets/Scenes/Bastion.unity` is at local `(0, 0, 0)` of its Half; `WaveSpawner.Groups.GroupSlot`
rings `GroupSpread` **1.1** m around that origin and `NavMesh.SamplePosition`s at
**2.2** m (`GroupSpread * 2`, `WaveSpawner.Groups.cs`). The sketch already named these
spawn closets (`Tools/bastion-sketch/bastion-defense-layout.txt`); the mark is a
room-centre empty.

This is a **loop**. One prefab, placed per `S`, paid back on every group. It is not an
arc and not a new Gatekeeper kind.

Leverage (`../../Working-Rules.md`): **#10, physical structure.** The origin moves off
the playable floor; the sealed-wing rule, group rotation, `spawnInterval` and
`GroupSpread` do not. A HUD ping without moving the origin would be #6 on top of the
same pop-in. A sixth kit block would be the M47 Half-as-alcove again: scaler-2 Half is
**16** m (`BastionKit.FootprintWidth`), a walkable room.

## Kill criterion

The first logged Wave after this ships. **If any body in that Wave has `SpawnedAt` on
the fight-floor side of its closet door, recut** — the closet is too shallow, the
NavMesh bake missed the interior, or a leftover centre marker is still in the
registry. Stuck-behind-a-shut-slab is a defect, not a feel call.

## Acceptance criteria

1. **One prefab** `Assets/Prefabs/Bastion/SpawnRoom.prefab`. Interior **4** m wide ×
   **5** m deep × `BastionKit.StoreyHeight` high. Pivot at the door threshold, local
   **+Z** toward the fight floor, interior down **−Z**. `SpawnPoint` child at local
   **z ≤ −2.5** (2.5 > 2.2, so `GroupSlot` cannot sample past the door plane). Not a
   `BastionSegment`. Kit palette stays five.
2. **The door is `BastionDoor`**, `startsLocked = false`, opens-for **Enemies**
   (`EnemyGatekeeper` only). Progression gates and passage auto-doors stay Anyone.
   Walking a Boarder to the slab does not raise it. A Gatekeeper inside does. Same
   GateSlab slide as today (`SlideSpeed`, `ProximityRange`).
3. **Sketch mark is `S:<id>:<edge>`.** `S:S1` without an edge fails parse. Edges on
   this dest:

   | Id | Room | Edge |
   |---|---|---|
   | S1 | R4 | W |
   | S2 | R5 | E |
   | S3 | R7 | W |
   | S4 | R8 | E |
   | S5 | R13 | N |
   | S6 | R14 | N |

   That edge becomes a kit **Door** side (the aperture). It is not a passage (no
   partner) and not a progression `door` line. Validate rejects a spawn edge that
   `BastionSketchPassages.Find` would pair.
4. **Emit instantiates the prefab** on that aperture (`SpawnRoom.AttachTo`) instead of
   a centre `S_*` marker. Name the `SpawnPoint` child `S_<id>` so `breached at S_S1`
   stays. Parent under the room, not the `SpawnPoints` root (Build All deletes that
   root).
5. **`Bastion.unity` has exactly six `SpawnPoint`s**, each a child of a `SpawnRoom`,
   none at a room origin. The six centre markers `S_S1`…`S_S6` are gone. D2
   `sealedSpawns` still names S3–S6 (the children). `WaveSpawner.spawnPoints` stays
   empty (registry). Build All leaves the six instances in place.
6. **The player action:** EMBARK, `E` then `T` at a live field terminal. A group
   raises the closet slab and walks out; walking up to a shut closet slab does not
   open it. Log: `group N: … breached at S_S*` with no `[Dev]`. Solo unchanged.

## Implementation

**Create** `Assets/Prefabs/Bastion/SpawnRoom.prefab` — closet volume (pack wall/floor
language of M61-2, or kit `WallMaterial` boxes if a pack piece will not snap; one
prefab, not a generator). Children: `BastionDoor` + GateSlab at the pivot; interior
walls/floor/ceiling down −Z; `SpawnPoint` named `S_xx` at z ≤ −2.5. No serialized
WaveSpawner tunables.

**Create** `Game/Bastion/SpawnRoom.cs` —

```csharp
public sealed class SpawnRoom : MonoBehaviour
{
    public void AttachTo(BastionSegment segment, BastionDoorEdge edge);
}
```

`AttachTo` parents to the segment, sets the segment side to Door, `BastionDoor.Configure`
unlocked + Enemies, `SnapToAperture`. `OnValidate` / `Awake` re-asserts Enemies and
unlocked so a prefab override cannot freeze Anyone (`Architecture.md` gotcha 1).

**Change** `Game/Bastion/BastionDoor.cs` — `enum BastionDoorOpensFor : byte { Anyone, Enemies }`
serialized `opensFor`, default Anyone. `AnyoneNear` uses Enemies →
`GetComponentInParent<EnemyGatekeeper>()`; Anyone keeps today's three checks. Extract

```csharp
internal static bool CountsAsOpener(BastionDoorOpensFor opensFor, Component onHit);
```

so EditMode can seed without a scene. `SetOpen` still toggles `sealedSpawns` on the
`SpawnPoint` objects, not the closet root.

**Change** `Sim/Bastion/BastionSketchMark` — `BastionSketchDirection Direction` on spawn
marks. **Change** `BastionSketchParser.ParseMark` — `S:S1:W`; missing edge fails.
**Change** `Validate` — spawn edge is not a passage and not a progression door.
**Change** `Tools/bastion-sketch/bastion-defense-layout.txt` — the six marks in the
table above.

**Change** `Editor/Bastion/BastionSketchEmitter.CreateMark` — spawn case loads
`Assets/Prefabs/Bastion/SpawnRoom.prefab`, `AttachTo`, names the child `S_` + RefId.
Delete the empty-marker path for spawn. Passage emit is unchanged: unpaired Door
sides are already not passages (`BastionSketchPassages`, `partnered`).

**Change** `Assets/Scenes/Bastion.unity` — six instances on R4/R5/R7/R8/R13/R14 at
the table edges; delete centre markers; retarget D2 `sealedSpawns` to the new
children; set those six Half sides to Door. Rebake NavMesh (`gkb_bastion_navmesh_request.txt`).
Bake already hides GateSlab (`SceneBuilder.BakeNavMesh`).

**Change** `Docs/Play/Waves.md` and `A-Run.md` on **close** — groups walk out of the
spawn point; drop the ring-on-the-floor sentence. Do not call a spawn point a door
(`language.md`). `3-Mechanics.md` §3.4 one sentence on close: origin is inside the
closet. Roadmap / Changelog in the same close commit.

Rejected: a sixth `BastionBlockKind`. Rejected: a new door class. Rejected: shrinking
`GroupSpread` so a shallower room works. Rejected: putting instances under
`SpawnPoints`. Rejected: deleting R4/R5/R7/R8/R13/R14 this slice (those Halves stay
walkable staging). Rejected: known/suspected/hidden spawn visibility
(`../../Brainstorms/missions-and-destinations.md`).

## Supporting work

- **Driven by** `E` then `T` (existing).
- **Shown by** the closet GateSlab sliding — here.
- **Reads back as** bodies walking out and `breached at S_S*` — here.

## Verification

1. Criterion 3 — `Tools/verify/run.sh --filter Sketch`. `S:S1` fails; `S:S1:W` on R4
   passes; hanging `S:S1:S` on R4 (the R2 passage) fails. Default layout file parses.
2. Criterion 2 — EditMode: `CountsAsOpener(Enemies, EnemyGatekeeper)` true;
   `CountsAsOpener(Enemies, CharacterController)` false; `Anyone` still true for both.
   Seed the opposite and watch it catch.
3. Criteria 1, 4, 5 — EditMode prefab load: interior 4×5, SpawnPoint z ≤ −2.5, door
   Enemies and unlocked. Scene: exactly six `SpawnPoint`, each under `SpawnRoom`, D2
   sealed list length 4. Grep `m_Name: S_S` under a room origin at `(0,0,0)` is zero.
4. Criterion 6 — playthrough: Splash → Ship → EMBARK → `E` then `T` on a live
   terminal. Slab rises for the group, not for the Boarder. Close log has
   `breached at S_S` and no `[Dev]`. `gkb_verify_request.txt` after the
   MonoBehaviour change; report `message` names any new red.

## Watch

- `GroupSlot` + a missing closet NavMesh snaps to the Half floor. Kill criterion.
- Leaf up for a passing group: a Boarder at the threshold can step in. Recut only
  if camping is observed; no second collider this slice.
- `MuteSpawns` / D2 `sealedSpawns` must disable the `SpawnPoint` child, not the
  closet root, or the aperture vanishes from nav.
- Master Control `UnlockedSpawns` is already empty in `Bastion.unity`; this slice
  does not add `S_Control`.
- Player-facing copy: spawn point, never door (`language.md`).

## Do not build

| Not building | Why not |
|---|---|
| Sixth kit block | 16 m Half is a fight room; the closet is a hung prefab |
| Known / suspected / hidden spawn visibility | Still brainstorm |
| Path telegraph | Parked, same file |
| `GroupSpread` / `spawnInterval` / group size | Numbers; the room is sized to 2.2 m |
| Deleting the six spawn Halves | Layout recut, second grain |
| One-way player blocker | Watch, not a system |
| Networked `BastionDoor` | Passage doors are already local `Update` |
| Vent / ceiling drop | A property on the roster, not this origin |

## Depends on / Unblocks

Needs M47 apertures and M61-2 GateSlab. Does not wait on M69 or M66. Does not jump
M72. Unblocks placing spawns on later dests by dropping a prefab on a wall.
