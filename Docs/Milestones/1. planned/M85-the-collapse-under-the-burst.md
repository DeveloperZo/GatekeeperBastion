# M85 — The collapse under the burst

<!--
Status: later
Update trigger: when the death clip lands, when the co-op carrier question is
answered in code, or when the death-burst ruling changes
Last updated: 2026-08-18
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

## Acceptance criteria

1. Killing a Gatekeeper shows the burst at the death moment, unchanged, and
   the body collapsing under it; the body is gone when the clip ends.
2. Wave-clear timing is unchanged: the wave-complete log line lands at the
   same kill that closes the wave today, not after the last corpse clears.
3. A collapsing body blocks nothing: shots pass through, players and
   Gatekeepers path through, it cannot be damaged or aggroed.
4. In a two-player session, both players see the collapse.

## Implementation

1. **Use** one of the three death clips already imported at
   `Packs/Gatekeepers/Animations/` (`Anim_death_falling_back`,
   `_forward`, `_twist`). One is wired; the other two stay imported as
   deliberate spares for later variation (ruled 2026-08-18, Zo). Variation
   itself is selection code and is out of this milestone's grain — humanoid
   import per the
   M81 pattern.
2. **Change** `Packs/Gatekeepers/Gatekeeper.controller` — add a `Die`
   trigger and a base-layer Death state with no exit transition.
3. **Change** `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` — in
   `OnDeath`, fire the trigger and disable the collider before calling
   `Despawn`.
4. **Change** `Assets/Scripts/Game/Enemies/WaveSpawner.cs` — `Despawn`
   keeps `_live.Remove` immediate and defers only the destroy tail by a
   `corpseSeconds` parameter (default 0, so every other caller is
   unchanged).

## Open questions

- *Co-op carrier for the death moment* — nothing replicates it today except
  the despawn itself; if the animator state does not replicate, non-host
  clients see a frozen body for the deferral. Recommendation: a ClientRpc
  fired where the deferral is scheduled, following the pattern already in
  `NetPlayerAvatar`. Zo picks the carrier in the netcode; criterion 4 holds
  either way. Unknown until the code is open.

## Verification

1. Criterion 2 — run a wave to completion before and after; compare the
   wave-complete line's position relative to the final kill line in
   `Logs/gkb_latest.log`. Identical ordering passes.
2. Criteria 1 and 3 — playthrough path: from Play, kill a Gatekeeper in the
   open; watch burst plus collapse; walk through the body and shoot through
   it before it clears.
3. Criterion 4 — two windowed instances; client-side observer watches a
   host-side kill, and the reverse.

## Watch

`Despawn`'s null guard ("destroyed between bookkeeping and here") must
survive the deferral: the scheduled destroy re-checks for fake-null before
touching the object. `DestroyTrackedGatekeepers` (run end) must still clear
corpses whose deferred destroy has not fired.

## Depends on / Unblocks

Clip source follows M81's Phase 0 result. M84's co-op verification rides in
this milestone's two-instance session.
