# M85 — The collapse under the burst

<!--
Status: completed, Aug 2026
Closed on: 2026-08-22 (disk / harness). Criteria 1, 3, 4 eyes-on wait on `M99`
(`Decisions.md` 2026-08-22).
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-22
-->

A killed Gatekeeper drops before it vanishes. The kill-confirm burst stays
exactly as it is; the body collapses under it, and only the destroy is
deferred, never the sim.

## Story

As a player, I want a killed Gatekeeper to fall, so that kills read on the
body as well as in the particle burst.

## Why it matters

Death today is `OnDeath` → `SpawnDeathBurst` → `WaveSpawner.Despawn`, which
removes the actor from the live ledger and destroys it in the same call
(verified 2026-08-18). The body vanishes the frame it dies.

Decisions made here:

- **The burst stays.** It is the established kill confirm, tinted per kind
  for free, and readable across the arena. The collapse plays under it.
  Rejected: replacing the burst with a corpse — it trades a long-range read
  for a short-range one. Rejected: ragdoll — physics cost and a co-op sync
  surface, for a two-second corpse.
- **Sim timing is untouched.** `onDeath` listeners, the kill line, and
  `_live.Remove` all fire at the death moment exactly as now. `Despawn`
  already separates ledger removal from destruction; only its destroy tail
  (`netObj.Despawn(true)` / `Destroy`) is deferred by the clip length while
  the actor is inert. A corpse must never delay a wave clear.
- **The corpse is inert.** Agent already stopped by `OnDeath`; the collider
  is disabled at death so the body blocks no shots, no movement, no aggro.
- **Clip source: free retarget path**, under M81's sourcing kill criterion.
  Wired clip is `Anim_death_falling_back`; `_forward` and `_twist` stay
  imported as deliberate spares (ruled 2026-08-18, Zo; which of the three
  is this close, 2026-08-22).
- **Co-op carrier is `NetEnemy.PlayDeathRpc`**, `[Rpc(SendTo.NotServer)]`,
  the same send as `NetVerbs` barrage. Host presentation is
  `EnemyGatekeeper.BeginDeathPresentation` from `OnDeath`. Clones never run
  `onDeath` (damage stays on the host), so without this Rpc they would sit
  frozen for the deferral. `NetEnemy.OnHostDeath` must not
  `NetworkObject.Despawn` when `HasLifetimeOwner` — that call is what
  deleted the corpse in co-op today. WaveSpawner still owns destroy.
- **Generator authors the Death state.** `EnsureGatekeeperController`
  returning the existing asset is how M81's Attack clip would have reverted;
  `EnsureDeathState` runs on both branches, no-op when present.

## Acceptance criteria

1. Killing a Gatekeeper shows the burst at the death moment, unchanged, and
   the body collapsing under it; the body is gone when the clip ends.
2. Wave-clear timing is unchanged: the wave-complete log line lands at the
   same kill that closes the wave today, not after the last corpse clears.
3. A collapsing body blocks nothing: shots pass through, players and
   Gatekeepers path through, it cannot be damaged or aggroed.
4. In a two-player session, both players see the collapse.
5. `Packs/Gatekeepers/Gatekeeper.controller` has a `Die` trigger and a
   base-layer Death state whose motion guid matches
   `Anim_death_falling_back.fbx`; Attack still references
   `03ede1e3f3df7b14fbb44ae773ed0c02`. `DeathCollapseTests` pins the disk
   and the call sites.

## Implementation

1. **Use** `Packs/Gatekeepers/Animations/Anim_death_falling_back.fbx`. The
   other two death takes stay imported as spares. Variation itself is
   selection code and is out of this grain.
2. **Change** `Packs/Gatekeepers/Gatekeeper.controller` — add a `Die`
   trigger and a base-layer Death state with no exit transition. Any State
   → Death, duration 0.05, no exit time, `canTransitionToSelf` false. Die
   is listed before Attack so it wins if both fire.
3. **Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs` —
   `EnsureDeathState` adds the parameter and state if missing, and is
   called from both branches of `EnsureGatekeeperController`.
4. **Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` — `OnDeath`
   calls `BeginDeathPresentation` (Die trigger, agent off, colliders off)
   then `SpawnDeathBurst`, then `Despawn(this, CorpseSeconds)`.
5. **Change** `Assets/Scripts/Game/Enemies/WaveSpawner.cs` — `Despawn`
   keeps `_live.Remove` immediate and defers only the destroy tail by a
   `corpseSeconds` parameter (default 0). Pending bodies live on `_corpses`;
   `TickDelivery` counts them down; `DestroyTrackedGatekeepers` still
   wipes them on run end.
6. **Change** `Assets/Scripts/Game/Net/NetEnemy.cs` — `OnHostDeath` fires
   `PlayDeathRpc` and returns without despawning when the spawner owns
   the actor. Fixtures with no spawner still despawn immediately, so
   `NetKillTests` is unchanged.

## Verification

1. Criterion 5 — `Tools/verify/run.sh --filter DeathCollapse`. Expect
   `DeathCollapseTests` 6/0. A controller that had not done the work has
   no `Die` parameter and no `guid: 09d7b63af6247d34281bf977e47d7eb0`.
2. Criterion 2 — disk: `OnGatekeeperDied` still decrements `AliveCount` and
   calls `CheckAssaultCleared` on the death event, not on `DestroyActor`.
   Play confirmation: wave-complete line still sits on the final kill line
   in `Logs/gkb_latest.log`, not two seconds later.
3. Criteria 1 and 3 — playthrough on `M99` when it ships: kill a Gatekeeper
   in the open; burst plus collapse; walk through the body and shoot through
   it before it clears. Not a full Run.
4. Criterion 4 — two windowed instances on the range (or loopback): client
   watches a host-side kill, and the reverse.

### What has landed

**Landed 2026-08-22 (agent).** Clip, Die trigger, Death state, deferred
destroy, collider disable, and the co-op Rpc are on disk. Criterion 5 is
the close gate. Criteria 1, 3, 4 are structurally in place; eyes-on waits
on `M99` (`Decisions.md` 2026-08-22).

`Anim_death_falling_back.fbx` guid `09d7b63af6247d34281bf977e47d7eb0`;
Death `m_Motion` fileID `-203655887218126122` matches the Mixamo take.
Attack still references `03ede1e3f3df7b14fbb44ae773ed0c02`. Death has
`m_Transitions: []`. `CorpseSeconds` is 2.

`EnsureDeathState` is a no-op when the state is already present, so a
later Build All does not dirty the controller.

## Watch

`Despawn`'s null guard ("destroyed between bookkeeping and here") must
survive the deferral: `DestroyActor` re-checks for fake-null before
touching the object. `DestroyTrackedGatekeepers` (run end) must still
clear corpses whose deferred destroy has not fired — they are not on
`_live` any more.

`NetEnemy.OnHostDeath` used to despawn in the same breath as death. A
future "the clone vanished instantly" report should look there first.

## Depends on / Unblocks

Clip source follows M81's Phase 0 result. M84's co-op flinch visibility
still rides a two-instance session; this milestone's Rpc does not carry
the flinch.
