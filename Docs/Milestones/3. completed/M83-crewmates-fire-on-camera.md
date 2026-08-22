# M83 — Crewmates fire on camera

<!--
Status: completed, Aug 2026
Closed on: 2026-08-22. PlayMode `NetAvatarAnimTests` 2/0
(2026-08-22T15:56:13Z) and `DriveRemoteAnimator_SetsSpeedWhenMoved` Passed
(2026-08-22T15:57:11Z). Two-window eyes-on is confirmation, not the gate
(M48 fixture multiplier; `Decisions.md` 2026-08-22).
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-22
-->

Drive the Boarder controller's already-wired Fire and Reload states on remote
avatars, so a crewmate who is shooting looks like they are shooting. Code
only; the clips have existed since the controller was built.

## Story

As a co-op player, I want to see a crewmate's avatar fire and reload, so that
the shared fight reads at a glance.

## Why it matters

`Boarder.controller` contains Fire and Reload states with clips
(`Anim_FiringRifle`, `Anim_Reloading`), but nothing in the codebase fires
those triggers: `NetPlayerAvatar` replicates Speed only, and no shot
replication of any kind exists (grepped 2026-08-18; audit records it). In
co-op a crewmate's avatar walks and runs but never visibly fights.
Multiplayer is first-class in this project's rules, which makes a silent
crewmate a standing violation, not a polish item.

Decisions made here:

- **Relay shape:** the owning client notifies its avatar on shot fired and on
  reload started; server relays to non-owner clients, which `SetTrigger` on
  the avatar's Animator. `CreditBankedClientRpc` named in the 2026-08-18
  draft is gone; the live pattern is `[Rpc(SendTo.Server)]` then
  `[Rpc(SendTo.NotOwner)]`, same as `NetVerbs.ApplyBarrageClientRpc`. Owner
  does not trigger its own avatar (the avatar body is not the first-person
  view).
- **Hook points:** `PlayerGun.Fire` after a shot that passed ammo/rate gating,
  and `StartReload` when a reload actually starts. Bind is
  `NetPlayerAvatar.LocalOwner` — the FPS rig is not a child of the puppet.
  Rejected: hooking `WeaponViewModel` — the viewmodel is local presentation
  and can be absent.
- **One RPC per shot, no batching.** Whether high-rate weapons flood the
  channel: unknown, needs play data; batching is the named follow-up if it
  does.
- **Reload clip plays at authored length.** Weapon reload seconds are not
  replicated. Speed-scaling the Mixamo clip to each gun is a follow-up if
  play shows the motion cutting off or overstaying.

## Acceptance criteria

1. In a two-player session, when player A fires, player B sees A's avatar
   play the firing motion; when A reloads, B sees the reload motion for its
   duration.
2. The local player's own screen is unchanged by this milestone (first-person
   view unaffected; the owner's avatar is not visibly triggered on the
   owner's client).
3. Remote avatars idle and locomote exactly as before when not firing.

## Implementation

1. **Create** `Assets/Scripts/Game/Net/NetPlayerAvatar.Anim.cs` — partial,
   because the main file is already near the Game cap (Protocol.cs split for
   the same reason). `NotifyFired` / `NotifyReloadStarted` on the owner path;
   unreliable ServerRpc+NotOwner ClientRpc for fire; reliable pair for
   reload; `SetTrigger` hashes `Fire` / `Reload`; skip owner; null-guard
   `_animator` as the Speed path does. Reset the other trigger so a pending
   Reload cannot steal the next Fire (same as `WeaponViewModel`).
2. **Change** `Assets/Scripts/Game/Player/PlayerGun.cs`,
   `PlayerGun.Firing.cs`, `PlayerGun.Reload.cs` — cache `LocalOwner`, call
   notify from `Fire()` after gating and from `StartReload`. Drone placement
   is not a shot and is not notified.
3. Reliability: one sentence in the Anim.cs class comment, not a config
   surface.

## What has landed

`NetPlayerAvatar.Anim.cs` plus PlayerGun hooks. PlayMode
`NetAvatarAnimTests.NotifyFired_RemoteEntersFire_OwnerDoesNot` and
`NotifyReloadStarted_RemoteEntersReload_OwnerDoesNot` both Passed
(2026-08-22T15:56:13Z): guest owner notify puts the host clone in
Fire/Reload, host owner notify puts the client clone in Fire, and the
owning client's animator stays out of those states.
`DriveRemoteAnimator_SetsSpeedWhenMoved` Passed (2026-08-22T15:57:11Z).

## Verification

1. Criterion 1 — `NetAvatarAnimTests` as above, 2/0.
2. Criterion 2 — those tests assert the owning client's animator does not
   enter Fire/Reload. First-person viewmodel is untouched (`WeaponViewModel`
   still owns local Fire/Reload).
3. Criterion 3 — `NetLobbyPresenceTests.DriveRemoteAnimator_SetsSpeedWhenMoved`
   Passed 2026-08-22T15:57:11Z; this milestone does not write Speed.
4. Playthrough path (confirmation): host plus one client
   (`run-windowed.bat`). In a fight, A fires in B's view: firing motion on
   A's avatar. A reloads: reload motion. Repeat with roles swapped.

## Watch

`NetPlayerAvatar` already guards the animator against the fake-null lifetime
problem in its Speed path; the new triggers use the same guard.

## Depends on / Unblocks

Independent of M81/M82 (the clips it needs already exist). If a fuller
shot-replication milestone (tracers, muzzle flash, hit impacts on remote
screens) is ever cut, it supersedes this relay and should absorb it.
