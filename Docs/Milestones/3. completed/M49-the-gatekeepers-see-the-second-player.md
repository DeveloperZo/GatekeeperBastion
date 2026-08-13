# M49 — The gatekeepers see the second player

<!--
Status: next
Update trigger: when the down rule changes
Last updated: 2026-08-13
-->

After M48 the client can kill but cannot be touched: the avatar has no collider, no health,
and no entry in the targeting set. This slice makes the second player **exist to the game** —
huntable, damageable, downable — which is the difference between a spectator with a gun and
a co-op fight.

**M48 shapes adopted.** `NetPlayerAvatar.Awake` destroyed any Collider — reverse that in
prefab and code. `PlayerHealth.OnEnable` always registered; the avatar skips auto-register
and the server registers only the non-owner body (the host rig stays the host's targeting
entry). `GameController.PlayerDied` ends the host run, so client-only refuses. Tests use
the same `NetcodeIntegrationTest` fixture as `NetKillTests`, including
`EnableSceneManagement = false`.

## Story

As a second player, I want gatekeepers to hunt me, hurt me and down me, so that I am playing
the same game as the host rather than shooting into it.

## Why it matters

Targeting was built for this and is waiting: `EnemyGatekeeper.Targeting.cs:98` already
retargets through `PlayerRegistry.NearestAlive(position)`, the registry already holds a
pruned list of `PlayerHealth` entries, and `NearestAlive` already skips the dead. M45 kept
`PlayerRegistry` as a scene registry on exactly this bet. The whole slice is therefore:
give the avatar a `PlayerHealth`, register it on the server, and replicate the consequences
back to its owner. No enemy code changes.

Leverage tier (`../../Working-Rules.md`): **#10, structure** — no tuned number moves;
`playerDamage` and player HP are read where they already live.

## Acceptance criteria

1. `NetPlayerAvatar` gains a **capsule collider** (reversing M48's no-collider decision —
   `CanSeePlayer`'s line-of-sight raycast at `EnemyGatekeeper.Targeting.cs:87` requires the
   hit collider be a child of the target, so an invisible-to-physics avatar can never be
   seen) and a **`PlayerHealth`**, server-authoritative, mirrored to the owner via
   `NetworkVariable<float>`.
2. While spawned, the server registers the avatar's `PlayerHealth` in `PlayerRegistry`
   (`[Net] avatar registered clientId=<n>` / unregistered on despawn or disconnect). The
   client's own instance never registers it — the server's targeting set is the only one
   that matters. The host-owned avatar is not registered (the FPS rig already is).
3. **The player action: standing closer to a breach than the host, a gatekeeper turns and
   hunts the client** — visible on both screens. The host log shows `[Net] avatar registered
   clientId=<n>` and the existing provoke line (`gatekeeper provoked — hunting the player`).
   That provoke line does not name a player; `EnemyGatekeeper` is unchanged (Implementation
   §3). The hunt is proved by `NearestAlive` in criterion 6 and by the visible turn in
   playthrough. Being shot by the client already provokes (M48 passes the avatar as the
   damage source); this criterion is about proximity and sight, not damage.
4. A gatekeeper swing on the avatar drops its server-side HP by `playerDamage`; the owner's
   screen reacts within the same second — the owner's local rig `PlayerHealth` is a **view**
   of the replicated value (server → owner, one direction), so the existing HUD, damage
   feedback and death flow fire without HUD changes.
5. **Client down is survivable for the run**: at 0 HP the owner enters the existing local
   death/down flow; the avatar stops drawing aggro (`NearestAlive` skips dead — no new
   code); `GameController.PlayerDied` on a client-only session is never called from `Die()`, so the
   host's run continues. Host down keeps the solo rule (host owns the run). Getting back
   up — how, or whether — is **unknown — needs design**, parked in
   `../../Brainstorms/co-op.md`; this slice does not revive. The client's full end-of-run
   experience is the next slice's, named there.
6. **Headless**: `NetTargetingTests` (same `NetcodeIntegrationTest` fixture as M48's
   `NetKillTests`) asserts: registration on spawn and unregistration on despawn;
   `NearestAlive` returns the avatar when it is nearer; a server-side `TakeDamage` shows up
   in the owner's replicated HP; death removes it from the targeting set. Green inside the
   integration-check report.
7. **Solo is unchanged**: both suites and the harness green at current counts; no `[Net]`
   line in a solo log.

## Implementation

1. **Change** `Assets/Prefabs/Net/NetPlayerAvatar.prefab` — capsule collider, `PlayerHealth`
   with auto-register and run-ending death both off.
2. **Change** `NetPlayerAvatar.cs` — stop destroying colliders. Server: register the
   `PlayerHealth` only when `IsServer && !IsOwner`; unregister on despawn. Subscribe HP
   into `NetworkVariable<float> Hp`. Owner: hide own renderer **and** disable own collider
   (the host rig already has physics); on `Hp` change, push into the local rig's
   `PlayerHealth` (view direction only). No wave-start revive — that mechanic is parked.
3. **Change** `PlayerHealth` — `contributeToRegistry` (default true; avatar false) so
   OnEnable does not pollute the targeting set; `endsTheRunOnDeath` (default true; avatar
   false) so a puppet `Die()` does not call `PlayerDied`; `ApplyReplicatedHp` for the
   owner's rig view. On the local rig, `Die()` does not call `PlayerDied` when
   `GameController.Net.IsClientOnly` (client down must not end the host run).
4. **No change** to `EnemyGatekeeper` or its partials — criterion 3 must pass on the
   existing registry path. If it cannot, that is a finding to record, not a patch to hide.
   The existing provoke line does not name a player; that is accepted, not patched.
5. **Create** `Assets/Tests/PlayMode/NetTargetingTests.cs` — criterion 6.
6. **Change** docs: Roadmap row, Changelog; Decisions.md only if the down rule stated here
   changes during the work.

Rejected: a bespoke aggro system for remote players — the registry already is the
multiplayer targeting set; adding a parallel one re-creates the static-list problem M45
removed. Rejected: friendly fire — the gun damages `Damageable`, players are `PlayerHealth`,
and keeping those types disjoint is the cheapest possible no-friendly-fire rule.

## Supporting work

- **Driven by** walking near a breach / getting hit — no new input.
- **Shown by** the gatekeeper physically turning and chasing — the mechanic is its own
  surface.
- **Reads back as** the owner's existing HP bar and down screen, plus the `[Net]`
  register log lines — here.

## Verification

1. Criteria 6, 7 — write `gkb_verify_request.txt`; integration-check report green at
   current counts plus `NetTargetingTests`; harness and lint-arch green.
2. Criteria 3–5 — playthrough path: MPPM one virtual player, as M48. Host boards and stays
   at the hatch; client walks toward the breach. Watch a spawned gatekeeper pick the client,
   let it down the client, confirm the host run continues (kills still happen, no `YOU DIED`).
   Read both logs for register / retarget / down lines. One person drives it: the host player
   is deliberately idle the whole time. Playthrough is confirmation, not the gate.

## Do not build

| Not building | Why not |
|---|---|
| Friendly fire | Type-disjoint by construction; a design question for never |
| Revive-by-teammate | A real co-op mechanic with real design weight — brainstorm entry, not a side effect |
| Wave-start auto-revive / in-run client respawn | How, or whether, the client gets back up is open (Zo, 2026-08-13). Parked in `../../Brainstorms/co-op.md`. Do not decide it by shipping. |
| Client end screens / run summary | Next slice (the client's run), named in `../../Brainstorms/co-op.md` |
| Per-player difficulty or HP scaling | Tuning tier; nothing above it is settled yet |

## Depends on / Unblocks

Depends on M48 (avatar, mirror, fixture). Unblocks the client's run lifecycle slice —
a client the game can kill is a client whose run can end.
