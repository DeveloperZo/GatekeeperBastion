# M86 — Downed crewmates look downed

<!--
Status: later
Update trigger: when the down clip lands, or when the buy-back/restore flow
(M79) changes what "restored from zero" means
Last updated: 2026-08-18
-->

A crewmate at zero HP drops in your view and stands back up when restored.
Reversible state, not a one-shot: restore-from-zero already exists in code.

## Story

As a co-op player, I want a crewmate at zero HP to visibly drop, so that who
needs help reads at a glance in a fight.

## Why it matters

`PlayerHealth.IsDead` exists, HP replicates through `NetPlayerAvatar.Hp`,
and a guest going down deliberately does not end the host's run
(`endsTheRunOnDeath` is per-player; verified 2026-08-18). But the avatar has
no down state: a dead crewmate's body keeps idling upright. Restore from
zero clears `IsDead` without moving the body, and buy-back at live windows
is planned (M79), so the state must be enterable and leavable.

Decisions made here:

- **A `Dead` bool on `Boarder.controller`, not a trigger** — the state is
  reversible on restore. A Down state overrides locomotion while true;
  transition back to the blend when false. Rejected: a death trigger plus a
  separate revive trigger — two one-shots to keep in sync where one
  replicated bool already exists.
- **Driven from the replicated Hp** in `NetPlayerAvatar`'s existing animator
  drive: `Hp <= 0` sets it, restore clears it. No new replication surface.
- **One clip, a collapse that holds its final frame.** Free retarget path,
  under M81's sourcing kill criterion.
- **The local first-person view is out of scope** — what the dying player
  sees stays whatever the run-end/down flow shows today.

## Acceptance criteria

1. In a two-player session, when player A reaches zero HP, player B sees A's
   avatar drop and stay down.
2. When A is restored from zero (dev heal now; buy-back once M79 lands), B
   sees A's avatar stand and locomote normally again.
3. A live avatar's idle, walk and run are unchanged, and Fire/Reload (M83,
   if landed) still play.

## Implementation

1. **Create** `Packs/Boarder/Anim_Down.fbx` — humanoid import, avatar per
   the existing Boarder clip pattern.
2. **Change** `Packs/Boarder/Boarder.controller` — add `Dead` (bool) and a
   Down state overriding the locomotion blend, with entry and exit
   transitions on the bool.
3. **Change** `Assets/Scripts/Game/Net/NetPlayerAvatar.cs` — in
   `DriveRemoteAnimator`, `SetBool` from the replicated `Hp` value,
   null-guarded like the Speed drive. Skip driving Speed while down.

## Verification

1. Criterion 1 — playthrough path: two windowed instances; the guest stands
   in Gatekeeper melee until down; the host watches the avatar drop.
2. Criterion 2 — restore the downed player from zero via the dev path
   (`PlayerHealth`'s restore already clears `IsDead`); the host watches the
   avatar stand. Re-verify via buy-back when M79 lands.
3. Criterion 3 — normal co-op play before and after; no change to live
   avatars.

## Watch

`NetPlayerAvatar` binds its animator with the same fake-null guards as the
Speed path; the `Dead` drive uses them. The dead-host case (M49
host-death-ends-run, buy-back conflict) is an open project ruling — this
milestone renders the state and takes no position on who may be down.

## Depends on / Unblocks

Independent to build. Criterion 2's full verification is cheapest after M79;
the dev restore path proves it before then.
