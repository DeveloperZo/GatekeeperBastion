# M68 — The garrison has a body

<!--
Status: later
Update trigger: when a kind-to-mesh mapping changes, when the Mixamo retarget fails, or when a fourth in-build kind ships
Last updated: 2026-08-14
-->

The three in-build Gatekeepers stop being coloured capsules and become POLYGON Sci-Fi Space
characters from the pack already locked for `M61` (SKU 138857). Colour-and-scale was the
tutorial (`SceneBuilder.Enemies.cs`); silhouette is the tutorial now.

`M61` dresses the Boarder. `M61-2` dresses the kit. This dresses the things that walk at
you. `M66` is hit confirm, audio tells, and player identity — not this.

## Story

As a Boarder, I want to tell Default, Hunter and Ram apart by looking, so that triage in a
lane does not depend on remembering which capsule colour was which.

## Why it matters

`SceneBuilder.Enemies.cs` already says colour and size are the only tutorial this game has.
Capsules of three hues are that tutorial. A dressed Bastion (`M61-2`) with capsule enemies
is the same half-dressed defect `M61-2` Watch names for kit: a textured room beside a
primitive body looks broken in a way two primitives do not.

The pack is already on disk and already the one source (`../../Decisions.md`). Gatekeepers
are **androids**: robotic, or fully helmeted with no face. Organic aliens and unhelmeted
humans are out. The Boarder is the IQ astronaut; EVA suits stay unused so the garrison
does not look like a second Boarder.

Leverage tier (`../../Working-Rules.md`): **#8, information flows.** No hp, speed, damage,
or agent footprint moves. What changes is what the player sees.

## Kill criterion

The first Wave after this ships, sound off. **If a player cannot name the three kinds at
lane range from silhouette alone, the mapping is wrong and gets recut** rather than
explained. **If Mixamo will not retarget onto the Synty Humanoid avatars, stop and keep
the capsules** — a T-pose marching is worse than a coloured capsule. Do not invent a
bob-script or a second animation pack to rescue it.

## Acceptance criteria

1. **Each in-build kind has one wrapper prefab**, tracked under `Assets/Packs/Gatekeepers/`,
   referencing the pack the same way `Assets/Packs/Weapons/` does. Mapping:

   | Kind | Wrapper | Pack prefab |
   |---|---|---|
   | Default | `Assets/Packs/Gatekeepers/Default.prefab` | `SM_Chr_RobotFemale_01` |
   | Hunter | `Assets/Packs/Gatekeepers/Hunter.prefab` | `SM_Chr_SpaceSoldier_HelmetArmor_Male_01` |
   | Ram | `Assets/Packs/Gatekeepers/Ram.prefab` | `SM_Chr_BR_War_Robot_01` |

   Three distinct prefab refs, no duplicate. No exposed face: disable any sibling head or
   hair on the HelmetArmor wrapper. Organic aliens (`Alien`, `Psionic`, `BigAlien`),
   unhelmeted crew/soldiers/junkers, and `SM_Chr_BR_EVA_Suit_01` are not used.
   `PolygonGeneric` is not a source.

2. **Agent footprint, collider and stats are unchanged.** Root `NavMeshAgent` radius /
   height / baseOffset stay the values `SceneBuilder.Enemies.cs` already bakes from
   `WaveDirector.StatsFor` and scale (Default 1.0, Hunter 0.85, Ram 1.4). Capsule collider
   stays on the root. The visual is a child at localScale that does **not** stack a second
   1.4 on War_Robot. `Configure` still pushes sim stats at spawn. No number in
   `../../Design/4-Data.md` moves.

3. **They walk.** Animator on the visual child: Idle / Walk / Run from Mixamo-named
   Humanoid takes without skin, same pipeline as `M61` Boarder. Melee swing is a Mixamo-
   named take; Idle stand-in is allowed until a distinct clip lands, same as Boarder's
   fire/reload. NavMeshAgent still owns translation; animator does not write root motion.
   Capsule `MeshRenderer` is disabled or removed so the primitive is not drawn under the
   mesh.

4. **A player can tell the three apart at lane range with the sound off**, including in a
   mixed group. Scale still does Ram vs Hunter work; the mesh must not collapse that.

5. **Net spawn still works.** `NetworkObject` stays on the root. Clients see the same mesh
   as the host. No new netvar. Solo unchanged. Session-log shape unchanged.

6. **Suites green, same names.** No new counter, no stats object (`../../Decisions.md`).
   SceneBuilder may still ensure components exist; it must not wipe the wrapper back to a
   visible capsule on Build All.

## Implementation

1. **Create** `Assets/Packs/Gatekeepers/` wrappers as criterion 1. Convert nothing to URP
   (`M61-2`: this project is Built-in; the pack shader graph already targets it).
2. **Change** `Characters.fbx` and `BR_Characters.fbx` import — Humanoid avatar, so Mixamo
   takes retarget. If the avatar mapping cannot be completed, the kill criterion fires;
   do not ship Generic T-poses.
3. **Copy** Mixamo-named Idle / Walk / Run (and a melee take when one exists) next to the
   wrappers, or reuse `Assets/Packs/Boarder/` clips if they retarget onto these avatars
   without retouch. One controller shared by the three wrappers is allowed; mesh is the
   identity, not a per-kind animator.
4. **Change** `SceneBuilder.Enemies.cs` — keep the capsule root, agent, `Damageable`, kind
   component, net components. Disable/remove the capsule renderer. Instance the mapped
   wrapper as a child. Do not `CreatePrimitive` as the thing the player looks at.
5. **Change** the three prefabs `WaveSpawner` already references
   (`enemyPrefab` / `hunterPrefab` / `ramPrefab`) so they are these roots. Prefabs freeze
   serialized values: push stats at spawn as today.
6. **Change** docs: Roadmap row, Changelog. Play pages do not gain a mesh lecture.

Rejected: **organic aliens** (`SM_Chr_Alien_01`, `Psionic`, `BigAlien`). They read as
creatures. The garrison is androids.

Rejected: **unhelmeted humans** (crew, junkers, Hunter_Female, soldiers with a head). A
face is a person.

Rejected: **EVA suit.** The Boarder is already an astronaut; a visor-suit Hunter would
look like a second one.

Rejected: **PolygonGeneric.** Not in the clone-import path (`../../Decisions.md`).

Rejected: **a Synty animation pack or FMOD.** Mixamo or capsules; no third vendor.

Rejected: **meshes for unbuilt kinds.** `M63` roster pages stay `in_build: false` until
the kind exists. A new kind's slice picks its mesh.

Rejected: **retuning hp/speed to match a bigger mesh.** Footprint stays; the mesh fits it.

Rejected: **folding this into `M61`, `M61-2`, or `M66`.** Boarder, kit, and hit-feedback
are already one grain each.

## Supporting work

- **Driven by** existing spawn — here. No new input.
- **Shown by** three silhouettes in a Wave — here.
- **Reads back as** a mixed group sortable with the sound off — criterion 4.

## Verification

1. Criteria 1, 2, 6 — EditMode: three distinct wrapper refs; agent radius/height equal to
   pre-slice values for each kind; `gkb_verify_request.txt` green; Build All does not
   restore a visible capsule renderer.
2. Criterion 3, 5 — one networked spawn (editor F10/F11 is enough): both screens show the
   mesh, not a capsule; agent still reaches the Hatch or the player as today.
3. Criterion 4 — playthrough: EMBARK, start a Wave that contains all three, sound off,
   name them at lane range. Log has no `[Dev]`.

## Watch

- Synty POLYGON characters are often Generic until the FBX is switched to Humanoid. That
  switch is the whole bet. Capsules stay until it works.
- Root scale 1.4 on Ram will enlarge the child. Counter-scale the visual or the War_Robot
  becomes a room. Criterion 2 is the net.
- Pack character prefabs are multi-character containers. Instance only the named mesh
  (RobotFemale / HelmetArmor / War_Robot), not inactive siblings. On HelmetArmor, the
  helmet stays; head and hair do not.
- Prefab freeze: do not serialize Mixamo clips as "the code default will find them."
- Hit flash in `M66` still has to land on the skinned mesh, not the disabled capsule
  renderer. If `M66` has not started, `Effects.cs` must target an enabled renderer.

## Depends on / Unblocks

**Depends on the pack import `M61` already required** (`Assets/Synty/PolygonSciFiSpace/`).
Does not wait on `M61`'s Boarder playthrough, `M61-2`'s emit, or `M66`. Better after
`M61-2` so they walk in a dressed room, not blocked on it.

**Unblocks** a friends session where the fight is three enemies, not three capsules.
`M66` audio tells still own off-screen identity.
