# Animation Inventory

<!--
Status: living tracker
Update trigger: when a clip lands, a controller changes, or a ruling below is
made; re-verify Status lines against Packs/ and the controllers, not this file
Last updated: 2026-08-25 (viewmodel set is on disk and wired — M82 closed; Cloaker added; blend-tree motions re-read off the controllers; M99 shipped)
-->

Every character and viewmodel clip the game needs, against what exists.
Baseline verified on disk 2026-08-18 (see `Architecture/asset-audit-2026-08-18.md` for
method); every Status line below re-read against `Packs/` and the three controllers on
2026-08-25. Environment animation is code-owned in this project and is out of
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
the viewmodel takes the shared path. M81 (attack) closed 2026-08-19, M82 (viewmodel set)
and M83 (remote fire/reload) closed 2026-08-22, M84 (flinch) 2026-08-23 and M85 (death
collapse) 2026-08-22 — all five are filed under `Milestones/3. completed/`, and M99, the
range those last two were waiting on for eyes-on, has since shipped too. **M86 (downed
avatar) is the only animation milestone still open**, and it now owes a clip rather than a
hook. Cap waived by Zo. Fileless on purpose:
spawn/entry (needs a wave-pacing ruling), dock (parked until M77 ships),
boss (composition stamped on `M80`: one high-HP Thief elite, no distinct clip).

## Player, third person (Boarder.controller, used by remote avatars)

| Clip | Status | Notes |
|---|---|---|
| Idle | have | `Anim_PlayerIdle` is on disk but **not wired** — the Move blend tree's 0 threshold holds `Anim_RifleIdle`, which is byte-identical to it, so the played result is the same clip. |
| Walk | have | `Anim_Walking` at the 0.5 threshold, byte-identical to `Anim_PlayerRun` at 1.0 — so the Boarder's walk and run are one clip and walk reads as a jog, the same fault `Anim_walking_slow_big` fixed on the Gatekeeper side. |
| Run | have | `Anim_PlayerRun` at the 1.0 threshold. |
| Rifle idle | have | `Anim_RifleIdle`; this is the wired idle (row 1). |
| Fire | have | Anim_FiringRifle; remote avatars `SetTrigger("Fire")` via `NetPlayerAvatar` (M83, 2026-08-22). |
| Reload | have | Anim_Reloading; remote avatars `SetTrigger("Reload")` via `NetPlayerAvatar` (M83, 2026-08-22). |
| Death / downed | missing | Milestone: M86 (still active). The hook is already in: `Boarder.controller` carries a `Dead` bool and a `Down` state, and `NetPlayerAvatar.cs:385` sets it from replicated Hp. `Down`'s motion slot is `{fileID: 0}` — empty. What M86 owes is the clip, not the wiring. Local first-person out of scope. |
| Dock / interact | no code hook | Hold-E dock (M77) has no animator hook planned. Parked until M77 ships (its named trigger); no milestone on purpose. |

## Gatekeepers (Gatekeeper.controller, shared by Default, Hunter, Thief, Cloaker and ship intruders)

All four kind prefabs under `Packs/Gatekeepers/` reference this one controller: `Default`
(SK_UtilityBot), `Hunter` (SK_Hunter), `Thief` (SK_Exterminator) and `Cloaker` (SK_Tactical,
which also dresses the Boarder).

| Clip | Status | Notes |
|---|---|---|
| Idle | have, borrowed | Read off the controller 2026-08-25: the Move blend tree's 0 threshold holds `Anim_Walking`, the borrowed player walk, which is byte-identical to `Anim_PlayerRun`. There is no separate Idle state, so 0 is the idle and a standing Gatekeeper plays a run cycle. `Anim_PlayerIdle` and `Anim_RifleIdle` are on disk and unreferenced by this controller. Whether kinds get a distinct idle or borrow one of those is Zo's call; from M88 the kinds differ by robot model rather than by material tint. |
| Walk | have | `Anim_walking_slow_big` wired at the blend tree's 0.5 threshold 2026-08-19, replacing the borrowed player walk (`Anim_Walking`, byte-identical to the run clip, so walk read as a jog). Same SK_UtilityBot download route as Running. Shared controller: changes every Gatekeeper and ship-intruder walk, plus the probe's player body. Not yet seen in play. |
| Run | have | `Running.fbx` (2026-08-18): downloaded on the uploaded SK_UtilityBot character with its ARP rig intact, zero retarget; wired at the blend tree's 1.0 threshold. `Running_not_in_place` kept as the unwired root-motion twin. Not yet seen in play. |
| Attack | have | `Anim_attack_melee` on the Attack state (exit 0.9); fitted 2026-08-19 to the second slash of the 3.5s download (0.53s, done ~0.56s inside the 0.7s interval). M81 closed 2026-08-19 (Zo). Code fires Attack (EnemyGatekeeper, ShipIntruder). Per-kind variants deferred until a kind gains distinct attack behavior. |
| Hit reaction | have | `Anim_flinch.anim` on a masked additive HitReact layer (`UpperBody.mask`); `EnemyGatekeeper.OnDamaged` and `ShipIntruder` fire `HitReact`. Disk/harness closed 2026-08-23 (`HitReactTests`, plus `HitReactMotionTests` in PlayMode); M84 is filed completed and M99 has shipped, so the range for eyes-on exists. |
| Death | have | `Anim_death_falling_back` on a base-layer Death state (no exit); `EnemyGatekeeper.OnDeath` fires `Die`, disables colliders, defers only the destroy tail. Built 2026-08-22 (`GatekeeperDeathAnimTests` in PlayMode); M85 is filed completed and M99 has shipped. `_forward` / `_twist` stay imported as spares, unreferenced by the controller. |
| Spawn / entry | no code hook | Waves spawn with no entry moment. No milestone on purpose: an entry motion either delays first pressure (a wave-pacing ruling, Zo's) or plays under movement and telegraphs nothing. Ruling needed before a gate can be written. |

## Boss / elite (M80)

| Clip | Status | Notes |
|---|---|---|
| Everything | no code hook | Composition is one high-HP Thief (`FinaleEliteHp` 240). No distinct clip list — it reuses Thief's controller. A unique silhouette is no longer free: `SK_Tactical` was spent on the Cloaker (the fourth kind, 2026-08-24) — the pack's four robots are all assigned. An elite silhouette now means scale (as shipped) or a new purchase. |

## Weapon viewmodel, first person (Packs/Weapons/ViewModel.controller)

Code contract (WeaponViewModel): Fire trigger, Reload trigger, Speed float
(0.5 walk, 1.0 sprint) — all three hashed in `WeaponViewModel.cs:10-12`. Six weapons
(Br12, Cutter, Mk1, Mr35, PriC, SecD) on six meshes — five FaxLab3D shooter-pack fbx
sources under `Packs/weapon_source/`, plus the Synty sword on Cutter (2026-08-19).

| Clip | Status | Notes |
|---|---|---|
| Idle | have | `ViewModel_Idle.anim` at the Move blend tree's 0 threshold. |
| Fire | have | `ViewModel_Fire.anim` on the Fire state. |
| Reload | have | `ViewModel_Reload.anim`; one clip, speed-scaled to each weapon's reload duration — `WeaponViewModel` sets and restores the speed around the trigger. |
| Movement bob | have | `ViewModel_Move.anim` through the Speed blend, as ruled. Code-driven bob stays rejected. It sits at **both** the 0.5 and 1.0 thresholds, so walk and sprint play the same bob at the same rate; whether sprint wants its own is open. |

Shared path ruled by Zo 2026-08-18 and shipped by M82: four hand-keyed clips plus one
controller serving all six weapons. The controller is referenced by `Boarder.prefab` and
nothing else — `WeaponViewModel` binds the Boarder body Animator through the grip, so the
viewmodel and the third-person rig share one Animator. `IsometricBodyPresentation` owns the
`Speed` parameter while the iso rig runs; `WeaponViewModel` stands off it. Bespoke
per-weapon clips can override individual slots later if play calls for it.

## Count that matters

Code-driven today: **1 enemy attack clip + 1 flinch clip + 1 death clip + remote
fire/reload + the viewmodel set (4 clips shared)** — M81 closed the swing, M84 the flinch,
M85 the collapse, M83 the remote Fire/Reload drives and M82 the viewmodel set. Every one of
those is closed on disk and in the harness, and M99 has shipped, so nothing here is waiting
on a range to be looked at. **M86 (downed avatar) owes one clip**; its `Dead` bool, `Down`
state and driver are already wired. Still gateless: spawn/entry, dock, boss (reasons on
their lines). Two wiring faults are open and neither has a milestone: the Gatekeeper idle
plays a run clip, and the Boarder's walk and run are the same file.
