# Animation Inventory

<!--
Status: living tracker
Update trigger: when a clip lands, a controller changes, or a ruling below is
made; re-verify Status lines against Packs/ and the controllers, not this file
Last updated: 2026-08-18
-->

Every character and viewmodel clip the game needs, against what exists.
Baseline verified on disk 2026-08-18 (see `asset-audit-2026-08-18.md` for
method). Environment animation is code-owned in this project and is out of
scope here: doors slide by transform in `BastionDoor.cs`, combat effects are
generated in `Effects.cs`, station panels and HUD are Shift UI controllers.

Status values: **have** (clip on disk and wired), **missing** (code fires the
event, no clip exists), **no code hook** (would need Zo to add the event
first; listed so the want is tracked, not lost), **blocked** (waiting on a
named ruling).

Ruled 2026-08-18 (Zo): Synty humanoids stay for the three Gatekeeper kinds;
the viewmodel takes the shared path. Milestones M81 (attack), M82 (viewmodel
set), M83 (remote fire/reload) cover every **missing** and **unreachable**
line; M84 (flinch), M85 (death collapse), M86 (downed avatar) cover the
hookable **no code hook** lines, cap waived by Zo. Fileless on purpose:
spawn/entry (needs a wave-pacing ruling), dock (parked until M77 ships),
boss (blocked on M80's inputs).

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
| Idle / walk | have, borrowed | Reuses the player's idle and walk clips. Whether kinds get distinct locomotion is Zo's call; today kinds differ by material tint only. |
| Run | have | `Running.fbx` (2026-08-18): downloaded on the uploaded SK_UtilityBot character with its ARP rig intact, zero retarget; wired at the blend tree's 1.0 threshold. `Running_not_in_place` kept as the unwired root-motion twin. Not yet seen in play. |
| Attack | wired, unplayed | `Anim_attack_melee` set on the Attack state 2026-08-18 (exit time raised 0.2 -> 0.9); not yet seen in play, and the clip is not yet fitted to the 0.7s swing clock. Code fires the Attack trigger (EnemyGatekeeper, ShipIntruder); the state plays Anim_PlayerIdle. The single worst gap in the game. Milestone: M81 (one shared clip, all three kinds; per-kind variants deferred until a kind gains distinct behavior). |
| Hit reaction | planned | Milestone: M84 — masked upper-body flinch layer; the hook rides the existing `Damaged` aggro subscription (ShipIntruder gains one). |
| Death | planned | Milestone: M85 — collapse under the unchanged kill-confirm burst; only the despawn's destroy tail defers, wave-clear timing untouched. |
| Spawn / entry | no code hook | Waves spawn with no entry moment. No milestone on purpose: an entry motion either delays first pressure (a wave-pacing ruling, Zo's) or plays under movement and telegraphs nothing. Ruling needed before a gate can be written. |

## Boss / elite (M80)

| Clip | Status | Notes |
|---|---|---|
| Everything | blocked | M80 names boss/elite composition as an absent input. No clip list can exist before that ruling. |

## Weapon viewmodel, first person (no controller exists)

Code contract (WeaponViewModel): Fire trigger, Reload trigger, Speed float
(0.5 walk, 1.0 sprint). Six weapons (Br12, Cutter, Mk1, Mr35, PriC, SecD) on
four Synty meshes (rifle, sword, pistol, shotgun).

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

Code-driven and demo-visible today: **1 enemy attack clip + the viewmodel set
(4 clips shared)** — M81 and M82 close that, and M83 adds the missing code
drives for clips that already exist. M84 to M86 (flinch, death collapse,
downed avatar) add three more clips plus their hooks, covering everything on
this page that can currently carry a gate. Total new clips across the family:
eight. Still gateless: spawn/entry, dock, boss (reasons on their lines).
