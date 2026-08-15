# M63 — The garrison: a roster with bodies

<!--
Status: later
Update trigger: when a kind is added, removed or renamed, when the property list or the
per-bastion draw rule changes, when a kind-to-mesh mapping changes, when the Mixamo
retarget fails, or when a fourth in-build kind ships
Last updated: 2026-08-14
-->

Three kinds becomes a decided roster of **twelve**, recorded once so every later slice
builds against the same list. **Ram becomes Thief** in code and docs. The Garrison volume
in `Docs/Play/` is restructured to hold a roster rather than three pages, the session log
starts recording time spent at the Hatch per Wave — the number every stat line in this
roster is waiting on — and the three in-build kinds stop being coloured capsules: POLYGON
Sci-Fi Space characters from the pack already locked for `M61` (SKU 138857), Mixamo
Humanoid animation, agent footprint unchanged.

**This slice builds no new Gatekeeper.** It is the list, the rename, the volume shape,
the missing measurement, and bodies for the three kinds that already exist. Each new kind
is its own slice afterwards. `M61` dresses the Boarder. `M61-2` dresses the kit. This
dresses the things that walk at you. `M66` is hit confirm, audio tells, and player
identity — not this.

## Story

As the builder, I want the garrison decided, written down once, and worn by the three
kinds already in the build, so that twelve kinds arrive as one coherent set rather than
twelve separate arguments, and a Boarder can tell Default, Hunter and Thief apart by
looking rather than by remembering which capsule colour was which.

## Why it matters

**Three kinds were designed for one room and the room changed.** All three resolve at the
door. The bastion now has levels, lanes, blind corners, concealed approaches, terminals,
two Wave modes and a second player, and not one of the three uses any of it.

**A roster decided in pieces becomes twelve unrelated enemies.** The value of the set is
that no two take the same thing from the player, and that only holds if the list is
settled before the building starts. `../../Brainstorms/gatekeepers-and-waves.md` already
carries the rule that a kind is defined by a refusal; this slice extends that to a full
roster and to the properties that produce variants without new kinds.

**The measurement is still missing.** Every recorded loss is `HATCH BREACHED` with the
Boarder alive, and none of them says whether the door fell to too many arrivals or to
bodies standing too long. Those two answers want opposite kinds. M18 was demoted for
specifying a stat line without it. One log line closes it.

**And the three kinds that exist are capsules in a dressed room.** `SceneBuilder.Enemies.cs`
already says colour and size are the only tutorial this game has; capsules of three hues
are that tutorial, and silhouette is the tutorial now. A dressed Bastion (`M61-2`) with
capsule enemies is the same half-dressed defect `M61-2` Watch names for kit: a textured
room beside a primitive body looks broken in a way two primitives do not. The pack is
already on disk and already the one source (`../../Decisions.md`). Gatekeepers are
**androids**: robotic, or fully helmeted with no face. Organic aliens and unhelmeted
humans are out. The Boarder is the IQ astronaut; EVA suits stay unused so the garrison
does not look like a second Boarder.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** Nothing the player
does changes here — no hp, speed, damage or agent footprint moves. What changes is that
the list exists, the door-time number is readable, and a kind is readable from its
silhouette.

## Kill criterion

Two triggers; either fires alone.

**After the first three new kinds ship, if any two of them read as the same enemy in a
logged session, the roster is over-specified and gets recut** rather than extended. The
pairs to watch are named in the Watch section. Twelve is a target, not a promise.

**The first Wave after the meshes ship, sound off: if a player cannot name the three
kinds at lane range from silhouette alone, the mapping is wrong and gets recut** rather
than explained. **If Mixamo will not retarget onto the Synty Humanoid avatars, stop and
keep the capsules** — a T-pose marching is worse than a coloured capsule. Do not invent a
bob-script or a second animation pack to rescue it.

## Acceptance criteria

1. **The roster is recorded once**, in `../../Brainstorms/gatekeepers-and-waves.md`, as
   twelve kinds: Default, Hunter, Thief, Source, Enabler, Alarmer, Bypasser, Reclaimer,
   Exploder, Standoff Shooter, Ailment Shooter, Cloaker. Each carries its refusal and
   **what it takes from the player**, and no two entries name the same thing taken. The
   file states plainly that behaviours, properties and every number are **open**, decided
   per slice, and that this list is the roster and not a specification.
2. **The refusal rule is restated in its useful form.** The refusal governs the **target**
   a kind will and will not go after, and that stays enforced in code. Damage amounts,
   fallback attacks and what a kind does when cornered are **not** protected, and an
   enemy that scratches you on the way past has not lost its identity. The current
   absolute wording in `../../Architecture/` or `../../Design/1-Experience.md` §1.4 is
   amended in the same change to say so.
3. **Properties are listed separately from kinds**, so a variant never becomes a
   thirteenth kind: movement (climber, vent-born, drops from above), shield,
   invulnerable until a stated condition, visibility and camouflage, pack with a leader,
   splits on death, and the elite flag. The file says a property may ride on any kind.
4. **The draw rule is recorded.** A bastion shows a subset of the roster, not the roster.
   One difficulty budget per bastion, Wave templates with slots that draw from that
   bastion's set, and the last Waves reserved for the heaviest draw. Whether a bastion's
   set is authored, drawn, or a fixed core plus a draw is **stated as an open choice**,
   not decided here.
5. **Ram becomes Thief everywhere, with no behaviour change.**
   `Sim/Waves/GatekeeperKind.cs`, `WaveDirector.RamPerWave`, `Game/Enemies/RamGatekeeper.cs`,
   prefabs, tests, `Docs/Design/`, `Docs/Play/` and the milestone files that name it.
   Persisted save keys are checked and either migrated or left alone deliberately, stated
   either way. One commit. A Run before and after is identical apart from the name.
6. **The Thief's new rule is recorded but not built.** *Never damages while there is
   salvage to take; rams to take salvage from whoever holds it; damages only when there is
   nothing to take.* Written into `../../Decisions.md` and the brainstorm. The behaviour
   builds against M53's unbanked pile (`RunSim.Unbanked`, shipped 2026-08-13) in the
   Thief's own slice; this criterion is a recording only.
7. **M53's Ram exception is removed.** Criterion 4 of
   `M53-salvage-leaves-through-the-hatch.md` turns a terminal-only break back off. Under
   the Thief's rule there is no break to turn off, so that clause goes in this change and
   the edit says why.
8. **The Garrison volume is written, all twelve.** `Docs/Play/` gains a Gatekeepers hub
   explaining the garrison as a set of refusals, plus **one entry per kind, twelve of
   them**, in `../../_meta/_definitions/language.md` *Player-facing voice*, with `## See
   also` per M62 criterion 1. Origin plus fight tells, one pause each. **This is the
   Garrison volume M62 copies**, so it is written here and not deferred.
9. **A page that is not in the build does not reach the player.** Each kind page carries
   an `in_build` flag in its front matter, and M62's copy step ships only the pages marked
   true. The source folder is the whole roster; the player build is what exists. Flipping
   one flag is how a kind's entry ships in the same slice as the kind, with no page to
   write at that point because it is already written.
10. **Time at the Hatch is logged.** Gatekeeper-seconds spent within the Hatch's damage
    range, per Wave, on the session log, and `Tools/wave-report/run.sh` prints it beside
    the existing per-Wave medians. **This is the criterion the rest of the roster is
    waiting on**, because arrivals and dwell want opposite kinds and nobody can currently
    tell which one is breaking the door.
11. **No stat lines for unbuilt kinds.** No hp, damage, speed or Wave count is written for
    any kind that does not exist in the build. `../../Design/4-Data.md` gains nothing this
    slice except the door-time column.
12. **Each in-build kind has one wrapper prefab**, tracked under `Assets/Packs/Gatekeepers/`,
    referencing the pack the same way `Assets/Packs/Weapons/` does. The criterion-5 rename
    lands first, so the wrapper is `Thief.prefab` from the start and no `Ram.prefab` ever
    exists. Mapping:

    | Kind | Wrapper | Pack prefab |
    |---|---|---|
    | Default | `Assets/Packs/Gatekeepers/Default.prefab` | `SM_Chr_RobotFemale_01` |
    | Hunter | `Assets/Packs/Gatekeepers/Hunter.prefab` | `SM_Chr_SpaceSoldier_HelmetArmor_Male_01` |
    | Thief | `Assets/Packs/Gatekeepers/Thief.prefab` | `SM_Chr_BR_War_Robot_01` |

    Three distinct prefab refs, no duplicate. No exposed face: disable any sibling head or
    hair on the HelmetArmor wrapper. Organic aliens (`Alien`, `Psionic`, `BigAlien`),
    unhelmeted crew/soldiers/junkers, and `SM_Chr_BR_EVA_Suit_01` are not used.
    `PolygonGeneric` is not a source.
13. **Agent footprint, collider and stats are unchanged.** Root `NavMeshAgent` radius /
    height / baseOffset stay the values `SceneBuilder.Enemies.cs` already bakes from
    `WaveDirector.StatsFor` and scale (Default 1.0, Hunter 0.85, Thief 1.4). Capsule
    collider stays on the root. The visual is a child at localScale that does **not**
    stack a second 1.4 on War_Robot. `Configure` still pushes sim stats at spawn. No
    number in `../../Design/4-Data.md` moves beyond criterion 10's door-time column.
14. **They walk.** Animator on the visual child: Idle / Walk / Run from Mixamo-named
    Humanoid takes without skin, same pipeline as `M61` Boarder. Melee swing is a Mixamo-
    named take; Idle stand-in is allowed until a distinct clip lands, same as Boarder's
    fire/reload. NavMeshAgent still owns translation; animator does not write root motion.
    Capsule `MeshRenderer` is disabled or removed so the primitive is not drawn under the
    mesh.
15. **A player can tell the three apart at lane range with the sound off**, including in a
    mixed group. Scale still does Thief vs Hunter work; the mesh must not collapse that.
16. **Net spawn still works.** `NetworkObject` stays on the root. Clients see the same mesh
    as the host. No new netvar. Solo unchanged. Session-log shape unchanged beyond
    criterion 10's door-time line.
17. **No player-visible change** beyond the rename, the Play pages and the meshes. Suites
    green, same test names plus the door-time assertions. **No new counter, no
    accumulator, no stats object** (`../../Decisions.md`). **No new menu row without
    asking** (`../../Architecture/README.md`). SceneBuilder may still ensure components
    exist; it must not wipe a wrapper back to a visible capsule on Build All.

## Implementation

Ordered. The rename is step 2 and lands before any body work, so every wrapper, field and
page is authored as Thief from the start — no `Ram.prefab` is created and renamed a step
later.

1. **Change** `../../Brainstorms/gatekeepers-and-waves.md` into the roster record: twelve
   kinds with refusal and what-it-takes, the property list, the draw rule, the amended
   refusal wording, and the open questions kept open.
2. **Change** the Ram identifiers to Thief in one behaviour-preserving commit, on its own,
   so a bisect can find it. Everything after this step uses Thief naming.
3. **Change** `../../Decisions.md` for the Thief's rule and the amended refusal scope.
   **Change** M53 criterion 4 (criterion 7 here; the edit says why).
4. **Create** the Gatekeepers hub in `Docs/Play/` and all twelve kind pages under it, with
   `## See also` links both ways and an `in_build` flag on each. The three existing pages
   move under the hub. This is the bulk of the writing in this slice and it is what M62
   ships.
5. **Change** the session log to carry door-time per Wave, and `Tools/wave-report` to
   print it. This and the mesh swap are the only code with behaviour in this slice; the
   log line is additive.
6. **Create** `Assets/Packs/Gatekeepers/` wrappers as criterion 12. Convert nothing to URP
   (`M61-2`: this project is Built-in; the pack shader graph already targets it).
7. **Change** `Characters.fbx` and `BR_Characters.fbx` import — Humanoid avatar, so Mixamo
   takes retarget. If the avatar mapping cannot be completed, the kill criterion fires;
   do not ship Generic T-poses.
8. **Copy** Mixamo-named Idle / Walk / Run (and a melee take when one exists) next to the
   wrappers, or reuse `Assets/Packs/Boarder/` clips if they retarget onto these avatars
   without retouch. One controller shared by the three wrappers is allowed; mesh is the
   identity, not a per-kind animator.
9. **Change** `SceneBuilder.Enemies.cs` — keep the capsule root, agent, `Damageable`, kind
   component, net components. Disable/remove the capsule renderer. Instance the mapped
   wrapper as a child. Do not `CreatePrimitive` as the thing the player looks at.
10. **Change** the three prefabs `WaveSpawner` already references — `enemyPrefab` /
    `hunterPrefab` / `thiefPrefab` after step 2's rename — so they are these roots.
    Prefabs freeze serialized values: push stats at spawn as today.
11. **Change** docs: Roadmap row, Changelog. Play pages do not gain a mesh lecture.

Rejected: **building any of the nine new kinds here.** Twelve implementations is an epic,
and `../../_meta/_definitions/milestone.md` says an epic is not a milestone.

Rejected: **shipping unbuilt kinds' pages to the player.** The pages are written here so
M62 has a Garrison volume to copy; the `in_build` flag is what keeps a friend from reading
about an enemy they cannot meet. Writing the roster down is not lore ahead of the game.
Putting it in front of a player would be.

Rejected: **freezing behaviours or properties.** Zo, 2026-08-13: the shape is agreed, the
properties and behaviours are not. Each kind's slice decides its own, against this list.

Rejected: **numbers before the door-time log.** That is the mistake that demoted M18.

Rejected: **organic aliens** (`SM_Chr_Alien_01`, `Psionic`, `BigAlien`). They read as
creatures. The garrison is androids.

Rejected: **unhelmeted humans** (crew, junkers, Hunter_Female, soldiers with a head). A
face is a person.

Rejected: **EVA suit.** The Boarder is already an astronaut; a visor-suit Hunter would
look like a second one.

Rejected: **PolygonGeneric.** Not in the clone-import path (`../../Decisions.md`).

Rejected: **a Synty animation pack or FMOD.** Mixamo or capsules; no third vendor.

Rejected: **meshes for unbuilt kinds.** Roster pages stay `in_build: false` until the
kind exists (criterion 9). A new kind's slice picks its mesh.

Rejected: **retuning hp/speed to match a bigger mesh.** Footprint stays; the mesh fits it.

Rejected: **folding the body work into `M61`, `M61-2`, or `M66`.** Boarder, kit, and
hit-feedback are already one grain each.

## Supporting work

- **Driven by** existing spawn — here. No new input.
- **Shown by** three silhouettes in a Wave — here.
- **Reads back as** a mixed group sortable with the sound off — criterion 15 — and
  door-time per Wave in `Tools/wave-report` — criterion 10.

## Verification

1. Criteria 1 to 4 — read the file. Twelve kinds, twelve distinct things taken, properties
   listed apart from kinds, draw rule present, openness stated.
2. Criterion 5 — `grep -rn "Ram" Assets/ Docs/` returns only deliberate historical
   references. Play one Run before and after and diff the session log; only the name moves.
3. Criteria 7 to 9 — M53's clause is gone with a reason; the hub and twelve pages exist and
   link both ways; `Tools/lint-language/run.sh` passes; a copy step run with mixed flags
   ships only the in-build pages.
4. Criterion 10 — one played five-Wave Run, then `Tools/wave-report/run.sh` prints door-time
   per Wave. **Read it and record which term is breaking the door**, because that answer is
   the input to every kind that follows.
5. Criteria 12, 13 — EditMode: three distinct wrapper refs; agent radius / height equal to
   pre-slice values for each kind.
6. Criteria 14, 16 — one networked spawn (editor F10/F11 is enough): both screens show the
   mesh, not a capsule; agent still reaches the Hatch or the player as today.
7. Criterion 15 — playthrough: EMBARK, start a Wave that contains all three, sound off,
   name them at lane range. Log has no `[Dev]`.
8. Criterion 17 — both suites green (`gkb_verify_request.txt`), same test names plus the
   door-time assertions; Build All does not restore a visible capsule renderer.

## Do not build

| Not building | Why not |
|---|---|
| Any of the nine new kinds | Each is its own slice |
| Thief's salvage behaviour | Recorded only (criterion 6); its own slice against M53's unbanked pile |
| Shipping unbuilt kinds' pages into the build | Written now, gated by `in_build` |
| Stat lines for anything unbuilt | The door-time answer decides them |
| A thirteenth kind | Properties produce variants; kinds do not |
| Per-bastion draw implementation | Recorded as a rule here, built when a second bastion exists |
| Meshes for unbuilt kinds | A new kind's slice picks its mesh |

## Watch

- **Two pairs may collapse in play and the roster kill criterion is pointed at them.**
  Alarmer and Source both add enemies, and "doubles once if it lives" versus "keeps
  producing until killed" may not read differently in a fight. Thief and Reclaimer both
  move salvage, one off a carrier and one off the floor, and may feel like one enemy in
  two costumes.
- **No Gatekeeper in the game currently shoots.** Both ranged kinds need projectiles, firing
  positions and a pre-fire sound before either exists. That system is a sixth of the roster
  sitting behind one piece of work, which is an argument for building the Standoff Shooter
  early rather than late.
- **Audio is the real budget, not behaviour.** In first person an off-screen enemy has no
  telegraph at all. Twelve kinds means twelve sounds a player can tell apart, and in co-op
  it is also how the other player knows what is happening to you.
- **Twelve kinds at four or five per bastion is two or three distinct bastions before
  repeats.** The answer to that is the property list, not more kinds.
- Synty POLYGON characters are often Generic until the FBX is switched to Humanoid. That
  switch is the whole bet. Capsules stay until it works.
- Root scale 1.4 on the Thief will enlarge the child. Counter-scale the visual or the
  War_Robot becomes a room. Criterion 13 is the net.
- Pack character prefabs are multi-character containers. Instance only the named mesh
  (RobotFemale / HelmetArmor / War_Robot), not inactive siblings. On HelmetArmor, the
  helmet stays; head and hair do not.
- Prefab freeze: do not serialize Mixamo clips as "the code default will find them."
- Hit flash in `M66` still has to land on the skinned mesh, not the disabled capsule
  renderer. If `M66` has not started, `Effects.cs` must target an enabled renderer.

## Depends on / Unblocks

**The roster, rename and door-time log depend on nothing** — the log line is additive and
the rename is mechanical. **The body half depends on the pack import `M61` already
required** (`Assets/Synty/PolygonSciFiSpace/`). It does not wait on `M61`'s Boarder
playthrough, `M61-2`'s emit, or `M66`; better after `M61-2` so the meshes walk in a
dressed room, not blocked on it.

**Unblocks every Gatekeeper slice after it**, because each one can then be cut against a
settled list, a settled property vocabulary, and a real answer about whether the door
falls to arrivals or to dwell. **Unblocks a friends session where the fight is three
enemies, not three capsules**; `M66` audio tells still own off-screen identity.

**Feeds M62** directly: this slice writes the Garrison volume M62's reader copies, so M62
ships with a full volume rather than three pages. M62's copy step gains the `in_build`
filter.
