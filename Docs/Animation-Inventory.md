# Animation Inventory

<!--
Status: living tracker
Update trigger: when a clip lands, a controller changes, or a ruling below is
made; re-verify Status lines against Packs/ and the controllers, not this file
Last updated: 2026-08-21
-->

Every character and viewmodel clip the game needs, against what exists.
Baseline verified on disk 2026-08-18 (see `Architecture/asset-audit-2026-08-18.md` for
method). Environment animation is code-owned in this project and is out of
scope here: doors slide by transform in `BastionDoor.cs`, combat effects are
generated in `Effects.cs`, station panels and HUD are Shift UI controllers.

Status values: **have** (clip on disk and wired), **missing** (code fires the
event, no clip exists), **no code hook** (would need Zo to add the event
first; listed so the want is tracked, not lost), **blocked** (waiting on a
named ruling).

Ruled 2026-08-18 (Zo): Synty humanoids stay for the three Gatekeeper kinds;
superseded 2026-08-19 (Zo): each kind takes a FaxLab3D robot instead (M88), which
suits the clips — every Gatekeeper clip was downloaded on the SK_UtilityBot rig.
The viewmodel ruling below is untouched;
the viewmodel takes the shared path. Milestone M81 (attack) closed 2026-08-19;
M82 (viewmodel
set), M83 (remote fire/reload) cover every **missing** and **unreachable**
line; M84 (flinch, built 2026-08-21, playthrough open), M85 (death collapse), M86 (downed avatar) cover the
hookable **no code hook** lines, cap waived by Zo. Fileless on purpose:
spawn/entry (needs a wave-pacing ruling), dock (parked until M77 ships),
boss (composition stamped on `M80`: one high-HP Thief elite, no distinct clip).

## Player, third person (Boarder.controller, used by remote avatars)

| Clip | Status | Notes |
|---|---|---|
| Idle | have | Anim_PlayerIdle |
| Walk | have | Anim_Walking |
| Run | have | Anim_PlayerRun |
| Rifle idle | have | Anim_RifleIdle |
| Fire | have, unreachable | Anim_FiringRifle is wired in the controller but no code fires the trigger; remote players never show firing. Code gap (Zo), not an asset gap. Milestone: M83. |
| Reload | have, unreachable | Anim_Reloading, same situation as Fire. Milestone: M83. |
| Death / downed | planned | Milestone: M86 — `Dead` bool driven from replicated Hp, reversible on restore-from-zero. Local first-person out of M86's scope. |
| Dock / interact | no code hook | Hold-E dock (M77) has no animator hook planned. Parked until M77 ships (its named trigger); no milestone on purpose. |

## Gatekeepers (Gatekeeper.controller, shared by Default, Hunter, Thief and ship intruders)

| Clip | Status | Notes |
|---|---|---|
| Idle | have, borrowed | Reuses the player's idle clip (the wired 0-threshold motion is `Anim_FiringRifle`, byte-identical to it). Whether kinds get a distinct idle is Zo's call; from M88 the kinds differ by robot model rather than by material tint. |
| Walk | have | `Anim_walking_slow_big` wired at the blend tree's 0.5 threshold 2026-08-19, replacing the borrowed player walk (`Anim_Walking`, byte-identical to the run clip, so walk read as a jog). Same SK_UtilityBot download route as Running. Shared controller: changes every Gatekeeper and ship-intruder walk, plus the probe's player body. Not yet seen in play. |
| Run | have | `Running.fbx` (2026-08-18): downloaded on the uploaded SK_UtilityBot character with its ARP rig intact, zero retarget; wired at the blend tree's 1.0 threshold. `Running_not_in_place` kept as the unwired root-motion twin. Not yet seen in play. |
| Attack | have | `Anim_attack_melee` on the Attack state (exit 0.9); fitted 2026-08-19 to the second slash of the 3.5s download (0.53s, done ~0.56s inside the 0.7s interval). M81 closed 2026-08-19 (Zo). Code fires Attack (EnemyGatekeeper, ShipIntruder). Per-kind variants deferred until a kind gains distinct attack behavior. |
| Hit reaction | have | `Anim_flinch.anim` on a masked additive HitReact layer; `EnemyGatekeeper.OnDamaged` and `ShipIntruder` fire `HitReact`. Built 2026-08-21; not yet seen in play (M84 criteria 1–4). |
| Death | planned | Milestone: M85 — collapse under the unchanged kill-confirm burst; only the despawn's destroy tail defers, wave-clear timing untouched. |
| Spawn / entry | no code hook | Waves spawn with no entry moment. No milestone on purpose: an entry motion either delays first pressure (a wave-pacing ruling, Zo's) or plays under movement and telegraphs nothing. Ruling needed before a gate can be written. |

## Boss / elite (M80)

| Clip | Status | Notes |
|---|---|---|
| Everything | no code hook | Composition is one high-HP Thief (`FinaleEliteHp` 240). No distinct clip list — it reuses Thief's controller. A unique silhouette (`SK_Tactical`) is still free. |

## Weapon viewmodel, first person (no controller exists)

Code contract (WeaponViewModel): Fire trigger, Reload trigger, Speed float
(0.5 walk, 1.0 sprint). Six weapons (Br12, Cutter, Mk1, Mr35, PriC, SecD) on
six meshes — five FaxLab3D shooter-pack fbx sources under `Packs/weapon_source/`,
plus the Synty sword on Cutter (2026-08-19).

| Clip | Status | Notes |
|---|---|---|
| Idle | missing | Milestone: M82. |
| Fire | missing | Milestone: M82. |
| Reload | missing | Milestone: M82; one clip, speed-scaled to each weapon's reload duration. |
| Movement bob | missing | Milestone: M82, clip-driven through the Speed blend (ruled there; code-driven bob rejected while the param and pipeline already exist). |

Shared path ruled by Zo 2026-08-18: four hand-keyed clips + one controller
(`Packs/Weapons/ViewModel.controller`, spec in M82) serving all six weapons.
Bespoke per-weapon clips can override individual slots later if play calls
for it.

## Count that matters

Code-driven today: **1 enemy attack clip + 1 flinch clip + the viewmodel set
(4 clips shared)** — M81 closed the swing; M84 wired the flinch (not yet seen
in play); M82 closes the viewmodel rest, and M83 adds the missing code
drives for clips that already exist. M85 to M86 (death collapse,
downed avatar) add two more clips plus their hooks, covering everything on
this page that can currently carry a gate. Total new clips across the family:
eight. Still gateless: spawn/entry, dock, boss (reasons on their lines).
