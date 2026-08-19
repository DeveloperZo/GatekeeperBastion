# M83 — Crewmates fire on camera

<!--
Status: later
Update trigger: when fire/reload replication lands, or when shot replication
gains a fuller form (tracers, muzzle flash) that supersedes this relay
Last updated: 2026-08-18
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
  the avatar's Animator. Follow the ClientRpc pattern already in
  `NetPlayerAvatar` (`CreditBankedClientRpc`). Owner does not trigger its own
  avatar (the avatar body is not the first-person view).
- **Hook points:** `PlayerGun` on a successful shot and `PlayerGun.Reload` on
  reload start. Rejected: hooking `WeaponViewModel` — the viewmodel is local
  presentation and can be absent.
- **One RPC per shot, no batching.** Whether high-rate weapons flood the
  channel: unknown, needs play data; batching is the named follow-up if it
  does.

## Acceptance criteria

1. In a two-player session, when player A fires, player B sees A's avatar
   play the firing motion; when A reloads, B sees the reload motion for its
   duration.
2. The local player's own screen is unchanged by this milestone (first-person
   view unaffected; the owner's avatar is not visibly triggered on the
   owner's client).
3. Remote avatars idle and locomote exactly as before when not firing.

## Implementation

Zo's code, in order:

1. **Change** `Assets/Scripts/Game/Net/NetPlayerAvatar.cs` — add
   `public void NotifyFired()` and `public void NotifyReloadStarted()` on the
   owner path, a ServerRpc to relay, and a ClientRpc that calls
   `_animator.SetTrigger` with `Animator.StringToHash("Fire")` /
   `("Reload")` hashes (add them beside the existing `SpeedHash`), skipping
   the owner. Null-guard the animator as the existing Speed path does.
2. **Change** `Assets/Scripts/Game/Player/PlayerGun.cs` and
   `PlayerGun.Reload.cs` — call the bound avatar's notify methods where a
   shot actually lands a fire (after ammo/rate gating passes) and where a
   reload actually starts. The avatar reference comes from however the player
   rig is already bound to its `NetPlayerAvatar`; do not add a new lookup
   path if one exists.
3. Reliability: fire may be sent unreliable (cosmetic, high-rate); reload
   start reliable (single event with a duration). One sentence in code
   comments, not a config surface.

## Verification

1. Criterion 1 — playthrough path: host plus one client (two windowed
   instances; `run-windowed.bat` exists for this). In a Run, A fires in B's
   view: firing motion on A's avatar. A reloads: reload motion. Repeat with
   roles swapped.
2. Criterion 2 — while A fires, A's own screen shows only the viewmodel
   behavior (M82's, or nothing if M82 has not landed).
3. Criterion 3 — B watches A walk and stand without firing; motion identical
   to the current build.

## Watch

`NetPlayerAvatar` already guards the animator against the fake-null lifetime
problem in its Speed path; the new triggers use the same guard.

## Depends on / Unblocks

Independent of M81/M82 (the clips it needs already exist). If a fuller
shot-replication milestone (tracers, muzzle flash, hit impacts on remote
screens) is ever cut, it supersedes this relay and should absorb it.
