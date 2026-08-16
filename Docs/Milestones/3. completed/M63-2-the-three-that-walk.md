# M63-2 — The three that walk

<!--
Status: completed 2026-08-15
Update trigger: closed
Last updated: 2026-08-15
Closed on: Zo 2026-08-15. Gate: GatekeeperPresentationTests
Wrappers_ExistAsThreeDistinctAssets + EnemyPrefabs_KeepPreSliceAgentFootprint
Passed; CompendiumFormTests green (`gkb_verify_report.json` 2026-08-15T21:47:12Z).
Criterion 4 silhouette Zo-closed. Mixamo kill criterion did not fire.
-->

Default, Hunter and Thief (still `GatekeeperKind.Ram` in code until `M63`) get Synty
bodies and Mixamo walk. All three stay melee. The Boarder's capsule mesh comes off.
`M63` keeps the twelve-kind roster record, the code rename, door-time, and unbuilt
pages.

## Story

As a Boarder, I want the three Gatekeepers that already exist, and my own body, to be
meshes rather than pills, so I can tell Default, Hunter and Thief apart by looking and
so a dressed Bastion is not standing next to a primitive.

## Why it matters

`SceneBuilder.Enemies.cs` already says colour and size are the only tutorial this game
has. Capsules of three hues are that tutorial, and silhouette is the tutorial now. A
dressed Bastion (`M61-2`) with capsule enemies is the same half-dressed defect `M61-2`
Watch names for kit.

The Boarder root in `Bastion.unity` / `Ship.unity` still carries a capsule
`MeshFilter` (Unity mesh `10208`). `MeshRenderer.m_Enabled` is 0 in YAML;
`BoarderPresentation` still special-cases "the debug capsule the camera sits inside".
Remove the mesh, not just disable the renderer.

Leverage: **#6, information flows.** No hp, speed, damage or agent footprint moves.

## Kill criterion

**The first Wave after the meshes ship, sound off: if a player cannot name the three
kinds at lane range from silhouette alone, the mapping is wrong and gets recut** rather
than explained. **If Mixamo will not retarget onto the Synty Humanoid avatars, stop and
keep the capsules** — a T-pose marching is worse than a coloured capsule. Do not invent a
bob-script or a second animation pack to rescue it.

## Acceptance criteria

1. **Each in-build kind has one wrapper prefab** under `Assets/Packs/Gatekeepers/`,
   referencing the pack the same way `Assets/Packs/Weapons/` does. Mapping:

   | Kind (code) | Wrapper | Pack prefab |
   |---|---|---|
   | Default | `Assets/Packs/Gatekeepers/Default.prefab` | `SM_Chr_RobotFemale_01` |
   | Hunter | `Assets/Packs/Gatekeepers/Hunter.prefab` | `SM_Chr_SpaceSoldier_HelmetArmor_Male_01` |
   | Ram | `Assets/Packs/Gatekeepers/Thief.prefab` | `SM_Chr_BR_War_Robot_01` |

   Three distinct prefab refs, no duplicate. HelmetArmor: helmet on, sibling head/hair
   off. Organic aliens, unhelmeted humans, EVA suit, and `PolygonGeneric` are not used.
2. **Agent footprint, collider and stats are unchanged.** Root `NavMeshAgent` radius /
   height / baseOffset stay the values `SceneBuilder.Enemies.cs` already bakes (Default
   1.0, Hunter 0.85, Ram 1.4). Capsule collider stays on the root. Visual child
   localScale does not stack a second 1.4 on War_Robot. `Configure` still pushes sim
   stats at spawn. No number in `../../Design/4-Data.md` moves.
3. **They walk, and they are melee.** Animator on the visual child: Idle / Walk / Run
   from Mixamo-named Humanoid takes without skin, same pipeline as `M61` Boarder. Melee
   swing is a Mixamo-named take; Idle stand-in is allowed. NavMeshAgent owns
   translation; animator does not write root motion. Capsule `MeshRenderer` on the
   enemy root is disabled or removed. No projectile, no new attack interval, no ranged
   kind. Swing clock stays `GatekeeperAttackInterval`.
4. **A player can tell the three apart at lane range with the sound off**, including in
   a mixed group. Scale still does Thief vs Hunter work; the mesh must not collapse that.
5. **Net spawn still works.** `NetworkObject` stays on the root. Clients see the same
   mesh as the host. No new netvar. Solo unchanged. Session-log shape unchanged.
6. **The Boarder's capsule mesh is gone.** Player root in `Bastion.unity` and
   `Ship.unity`: remove `MeshFilter` + `MeshRenderer` (Unity capsule `10208`). Keep
   `CharacterController`. Same on `Assets/Prefabs/Net/NetPlayerAvatar.prefab` (renderer
   already disabled — remove the mesh). `BoarderPresentation` no longer has to skip a
   debug capsule because it is not there. Nothing re-enables a Player capsule at
   runtime or on Build All.
7. **Play pages for the three in-build kinds.** `Docs/Play/Gatekeeper-Default.md` and
   `Gatekeeper-Hunter.md` stay and each Game body states they close and swing (melee).
   `Gatekeeper-Ram.md` becomes `Gatekeeper-Thief.md`: title **Thief**, same current
   behaviour (code is still Ram until `M63`). Hub, README Garrison, See also, and
   `CompendiumCatalog` Garrison stems follow. `Tools/lint-language/run.sh` passes.
   `CompendiumFormTests` green. Field Manual stays Ram until `M63` regenerates it.
8. **SceneBuilder does not wipe the dress.** Build All may ensure components exist; it
   must not `CreatePrimitive` over a dressed prefab or restore a visible capsule
   renderer on enemies or the Player. Suites green. No new menu row, no new counter
   (`../../Decisions.md`, `../../Architecture/README.md`).

## Implementation

1. **Create** `Assets/Packs/Gatekeepers/` wrappers as criterion 1. Convert nothing to
   URP (this project is Built-in). Instance only the named character mesh, not the
   multi-character container.
2. **Change** `Characters.fbx` and `BR_Characters.fbx` import — Humanoid avatar. If the
   mapping cannot be completed, the kill criterion fires; do not ship Generic T-poses.
3. **Copy** Mixamo-named Idle / Walk / Run (and a melee take when one exists) next to
   the wrappers, or reuse `Assets/Packs/Boarder/` clips if they retarget without
   retouch. One controller shared by the three wrappers is allowed:
   `Assets/Packs/Gatekeepers/Gatekeeper.controller`. Apply Root Motion off.
4. **Change** `SceneBuilder.Enemies.cs` — keep the capsule root, agent, `Damageable`,
   kind component, net components. Disable/remove the capsule renderer. Instance the
   mapped wrapper as a child. If the prefab at the saved path already exists, load and
   dress it; do not `CreatePrimitive` as a wipe. Push stats at spawn as today.
5. **Change** the three prefabs `WaveSpawner` already references so they are these
   dressed roots.
6. **Change** Player in `Bastion.unity` and `Ship.unity`, and `NetPlayerAvatar.prefab`:
   delete MeshFilter + MeshRenderer. **Change** `BoarderPresentation` comments/skip if
   the root renderer is gone.
7. **Change** Play pages as criterion 7. Copy Compendium pages so StreamingAssets
   matches.
8. **Change** docs: this file, `M63` (body half moved here), Roadmap, Changelog.

Rejected: **building any of the nine new kinds.** Rejected: **ranged attacks.** Rejected:
**code rename Ram→Thief** — that is `M63`. Rejected: **organic aliens, unhelmeted
faces, EVA, PolygonGeneric.** Rejected: **a Synty animation pack or FMOD.** Rejected:
**retuning hp/speed to match a bigger mesh.** Rejected: **folding this back into M61,
M61-2, or M66.**

## Supporting work

- **Driven by** existing spawn — here. No new input.
- **Shown by** three silhouettes in a Wave, and no Player pill — here.
- **Reads back as** a mixed group sortable with the sound off — criterion 4.

## Verification

1. Criteria 1, 2 — EditMode: three distinct wrapper refs; agent radius / height equal
   to pre-slice values for Default / Hunter / Ram.
2. Criteria 3, 5 — one networked spawn (F10/F11): both screens show the mesh, not a
   capsule; agent still reaches the Hatch or the player as today.
3. Criterion 4 — playthrough: EMBARK, start a Wave that contains all three, sound off,
   name them at lane range. Log has no `[Dev]`.
4. Criterion 6 — grep Player / NetPlayerAvatar YAML for mesh `10208` returns none;
   Play Mode local view has no capsule around the Boarder.
5. Criterion 7 — `CompendiumFormTests`; lint-language; Esc → Compendium → Garrison →
   Thief.
6. Criterion 8 — Build All does not restore a visible capsule; suites green
   (`gkb_verify_request.txt`).

## Watch

- Synty POLYGON characters are often Generic until the FBX is switched to Humanoid.
- Root scale 1.4 on Ram/Thief enlarges the child. Counter-scale the visual.
- Pack character prefabs are multi-character containers.
- Prefab freeze: do not serialize Mixamo clips as "the code default will find them."
- `Damageable` already uses `GetComponentInChildren<Renderer>()` — hit flash lands on
  the skinned mesh once the capsule renderer is gone. Do not point it at a disabled
  capsule.
- `BoarderPresentation.SetStationHidden` must not re-enable a removed renderer.

## Depends on / Unblocks

Depends on the pack import `M61` already required
(`Assets/Synty/PolygonSciFiSpace/`). Not blocked on `M63`'s rename or door-time.

Unblocks a friends session where the fight is three enemies, not three capsules.
`M66` still owns off-screen audio identity.
