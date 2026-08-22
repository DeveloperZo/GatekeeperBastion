# Asset audit, 2026-08-18

<!--
Status: report, with a standing strategy in section 6
Update trigger: when animation, audio, or character-art assets are added or a
decision below is ruled; figures were verified on disk 2026-08-18 and drift after that
Last updated: 2026-08-18
-->

Prompted by Zo's report that the assets are not serving the game, with Synty's
lack of animations as the example. Every figure below was measured on disk on
2026-08-18 (file counts, guid cross-references, controller contents). What the
game looks and feels like in play is Zo's report, not something this audit can
measure. The clip-by-clip ledger lives in `Animation-Inventory.md` (living
tracker); this document is the snapshot and the strategy.

## Decisions

**1. Enemy attack presentation.** The Gatekeeper animator's Attack state plays
`Anim_PlayerIdle.fbx` (verified in `Packs/Gatekeepers/Gatekeeper.controller`:
the Attack state's motion guid is PlayerIdle's guid). All three enemy kinds
(Default, Hunter, Thief) share this controller, so every melee attack in the
game is a character standing still in idle pose. The code is ready for better:
`EnemyGatekeeper.cs` and `ShipIntruder.cs` both fire an Attack trigger.
Question: fix this by sourcing humanoid attack clips, or rule that Gatekeepers
stop being humanoid Synty meshes (which changes what to source)?
Recommendation: rule the character direction first; it gates every animation
purchase. Then run Phase 0 of section 6 before spending anything.

**2. First-person weapon animation.** `WeaponViewModel.cs` drives Fire, Reload
and Speed animator parameters, but the project contains no animator controller
for weapons at all. Total gameplay controllers on disk: two (Boarder,
Gatekeeper). The six weapon prefabs in `Packs/Weapons/` have no Animator
component. So firing and reloading have no first-person motion as assets;
the code path is a no-op. Question: shared generic viewmodel set (3 to 4
clips) or bespoke per weapon (up to 24)? Recommendation: shared set first; it
is the smallest thing that makes the code path real, and per-weapon clips can
replace it weapon by weapon if play says so.

**3. Audio scope.** The whole game runs on 7 audio files: 5 SFX (pistol,
rifle, shotgun fire, one footstep, one reload) and 2 music tracks. There is no
enemy sound, no hit confirmation, no damage or death sound, no ambience, and
UI audio is only the 4 clips inside the Shift package. Question: does audio
sourcing become its own line before the demo, or is it ruled post-demo?
Recommendation: unknown whether current audio hurts the demo, that needs play
data; but the inventory is small enough that the decision should be explicit
rather than default.

**4. Dead weight (low stakes).** 3,395 Synty assets are imported; 33 distinct
assets are referenced by any scene, prefab, material or controller (guid scan
across Scenes, Prefabs, Packs, Resources). That is under 1 percent
utilisation. This costs import and project-open time, not quality. Whether
that cost is felt is unknown. Recommendation: leave in place unless editor
iteration time bothers you; pruning is reversible housekeeping, not design.

## 1. What is on disk

- Synty: two packs, `PolygonSciFiSpace` and `PolygonGeneric`. 3,395 assets
  with guids, including 1,148 model FBXs. Animation clips: zero. Animator
  controllers: zero. Zo's premise checks out exactly.
- `Packs/` (project-authored): Boarder (player: prefab, controller, 6
  animation FBXs, 3 HUD pngs), Gatekeepers (3 enemy prefabs + materials, 1
  shared controller), Weapons (6 prefabs, no controllers).
- Shift Sci-Fi UI (Michsky): in use for menus/HUD; 55 anim clips and 33
  controllers, all UI-side.
- Audio: 7 game files under `Resources/Audio`, 4 inside Shift.
- VFX: zero particle or VFX-graph assets; effects are generated in code
  (`Scripts/Game/Combat/Effects.cs`, e.g. `DeathExplosion` for enemy deaths).
- TextMesh Pro, MCPForUnity, misc plugins: tooling, not game content.

## 2. What the game actually uses

The 33 referenced Synty assets break down as: the Bastion wall/floor/ceiling
kit (wall, corner pillar, doorframe, window, floor, ceiling), a handful of
props (bed, crate, control panel, screen, table, oxygen tank, light panel,
bridge chair), 4 weapon meshes (pistol, rifle, shotgun, sword), 3 character
FBX sources (`Characters.fbx`, `BR_Characters.fbx`, `Generic_Characters.fbx`),
and their materials/textures/shader. Everything else in the two packs is
unreferenced.

So the actual art surface of the game is: one modular room kit, about ten
props, four weapon meshes, and Synty character meshes wearing three material
tints as the three enemy kinds.

## 3. Animation, as built

The full gameplay animation inventory is six clips, all in `Packs/Boarder`:
Walking, PlayerIdle, PlayerRun, RifleIdle, FiringRifle, Reloading.

What the code asks for versus what exists:

| Consumer | Params driven | Backed by |
|---|---|---|
| Boarder.controller (player avatar) | Speed (only param any code drives) | Locomotion blend: covered. Fire and Reload states exist with clips, but no code fires those triggers (`NetPlayerAvatar` sends Speed only, `WeaponViewModel` targets the viewmodel), so remote players never show firing or reloading. Code gap, not an asset gap. |
| Gatekeeper.controller (all 3 enemy kinds, ship intruders) | Speed, Attack | Locomotion reuses the player's Walking/Idle/Run; Attack plays PlayerIdle. Attack is not covered. |
| WeaponViewModel (first-person) | Speed, Fire, Reload | No controller exists. Not covered. |

Environment motion needs no clips: doors slide by transform in code
(`BastionDoor.cs`), effects are code-generated. That pattern is consistent
across the project and is a strength to keep, not a gap.

Not present anywhere, listed without judgement: enemy hit reaction, enemy
death motion (deaths currently cut to the code-side explosion burst, which may
be the intended ruling), player death/downed (no code event exists), any
second locomotion set to distinguish Hunter/Thief movement from Default,
boss/elite anything (M80 already names boss composition as an absent input).
The full ledger with statuses is `Animation-Inventory.md`.

## 4. Audio, as built

`Resources/Audio/Sfx`: Fire-Pistol, Fire-Rifle, Fire-Shotgun, Footstep-Walk,
Reload. `Resources/Audio`: Stardust Drifter.mp3, Whispers of the Void.mp3.
That is the complete list. Events with no sound today include: enemy attack,
player or enemy taking damage, enemy death (the explosion burst is silent),
wave start/end, bank window, doors, terminals, pickups.

## 5. If humanoids stay: sourcing options for the animation gap

Both packs' characters are humanoid-rigged FBXs, and the existing six clips
already retarget onto them through Unity's humanoid avatar, so the pipeline is
proven in this project. Options, verified to exist as products on 2026-08-18
but not evaluated for fit:

- Synty's own ANIMATION line (Base Locomotion, Idles, and a full collection at
  syntystore.com) is built for these rigs.
- Mixamo remains the free humanoid clip source; attack, hit and death clips
  retarget the same way the current six clips do.
- Any humanoid animset from the Unity Asset Store retargets too.

Cost, licence terms, and clip quality per pack: not checked, would need a
pass once decision 1 is ruled.

## 6. Strategy: filling the gaps, and buy versus build

### The buy-versus-build test

Section 2's number is the cautionary tale for buying: two packs bought, 33 of
3,395 assets used. So every purchase gets the same gate, stated so it is
falsifiable: **before buying a pack, list the exact tracker lines
(`Animation-Inventory.md`) it would fill. If it fills fewer than 5, do not
buy; retarget free clips or author instead.** The threshold is Zo's to move;
the discipline is that the list is written before the purchase, not after.

When each side wins, as decision rules rather than taste:

- **Buy** when the need is commodity and the volume is real: full locomotion
  sets, per-kind movement variety, a body of hit/death/stagger clips. Nobody
  should hand-author a walk cycle. Today's tracker does not yet contain a
  5-line purchase for animation; it will if per-kind locomotion, boss, or
  bespoke per-weapon viewmodels get ruled in.
- **Build a workflow** (retarget-and-edit, hand keyframing, or code-driven
  motion) when the clip must match code timing or the game's identity. The
  enemy attack must line up with the damage moment the code already defines;
  a boss set is the game's signature and should not be recognizable from a
  marketplace. The project already leans this way successfully: doors and
  effects are code-owned motion.
- **Free retarget first, always,** as the probe: Mixamo costs nothing and
  runs through the already-proven humanoid pipeline. A purchase is only
  justified after a free clip has shown the slot works and the quality is the
  binding problem. Whether retargeted generic clips read acceptably on Synty
  proportions is unknown until Phase 0 answers it.

Audio gets the same test with the opposite expected outcome: SFX are
commodity, the current inventory is 7 files, so almost any decent pack clears
the 5-line gate. Buying is the likely answer there; hand-recording weapon and
UI sounds is a workflow this project has no pipeline for.

### Phases

**Phase 0, prove the loop, spend nothing.** One retargeted attack clip
(Mixamo or equivalent) into the Gatekeeper Attack state, then play. This is
the cheapest test that could kill the whole plan: if a generic humanoid
attack reads fine on Synty proportions in play, every later sourcing decision
is derisked; if it reads wrong, that is the finding that changes what to buy.
Wiring the clip is editor/code-side work (Zo). Exit: Zo's play report.

**Phase 1, the demo-visible minimum.** Per the tracker's closing count:
final enemy attack clip plus the shared viewmodel set (idle, fire, reload,
bob or code-driven bob). Roughly five clips, source chosen by Phase 0's
result. Also code-side, Zo's list: create and wire the viewmodel controller;
decide whether remote fire/reload (the unreachable Boarder states) is worth
networking now or parked.

**Phase 2, ruling-gated.** Death/hit/downed/spawn/dock presentation, per-kind
locomotion, boss set. Each blocked on a ruling named in the tracker; the
buy-versus-build gate applies per item once ruled. This is where a pack
purchase first becomes plausible for animation, because the volume arrives
here.

**Audio, parallel track.** If decision 3 rules audio in before the demo, run
the same shape: one free/cheap probe (enemy attack sound + hit confirm) to
see if it changes play feel, then a pack purchase against a written event
list from section 4.

## What this audit did not check

Visual quality in play (Zo's report is the only evidence on that), whether the
scene-side viewmodel Animator in Bastion.unity is wired to anything (two
Animator components there have no controller assigned; full scene tracing not
done), Shift UI's internal consistency, and editor import times for the unused
Synty content. Nothing in this document has been verified in the running
game.
