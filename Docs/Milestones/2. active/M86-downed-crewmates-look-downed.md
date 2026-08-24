# M86 — Downed crewmates look downed

<!--
Status: active
Update trigger: when the down clip lands
Last updated: 2026-08-23
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
has since shipped (**M90**, corrected 2026-08-23 — this doc previously cited
the wrong milestone, M79), so the state must be enterable and leavable.

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
2. When A is restored from zero (dev heal, or buy-back at a live window —
   M90 has shipped), B sees A's avatar stand and locomote normally again.
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
   avatar stand. Also provable via buy-back now that M90 has shipped.
3. Criterion 3 — normal co-op play before and after; no change to live
   avatars.

### What has landed

**Landed 2026-08-23 (agent).** `Boarder.controller` gained a `Dead` bool
parameter, a `Down` state (`Any State -> Down` on `Dead == true`, `Down ->
Locomotion` on `Dead == false`, both no-exit-time so the transition is
driven purely by the bool), and `NetPlayerAvatar.DriveRemoteAnimator` now
sets `Dead` from `Hp.Value <= 0f` every frame and skips the Speed drive
while dead, guarded by the same `_animator == null` fake-null check the
Speed path already used.

**Not landed: the clip itself.** `Down`'s `m_Motion` is `{fileID: 0}` (None)
— `Packs/Boarder/Anim_Down.fbx` has not been sourced. Zo is sourcing it
separately (free retarget path, mixamorig skeleton matching the other six
Boarder clips). Once the FBX and its `.meta` land, wiring it in is a
one-line edit to the `Down` state's `m_Motion` field in
`Boarder.controller` (state fileID `-8000000000000000001`) — no other
change is needed to close the milestone. Until then, a downed remote
avatar holds its bind pose rather than a collapse pose; criteria 1 and 2
are structurally provable today (the state transition and the stand-back-up
path both work), but the visual is not yet the intended clip.

Nothing has been run through the Editor or bridge for this change yet.

## Watch

`NetPlayerAvatar` binds its animator with the same fake-null guards as the
Speed path; the `Dead` drive uses them. The dead-host case (M49
host-death-ends-run, buy-back conflict) is an open project ruling — this
milestone renders the state and takes no position on who may be down.

## Depends on / Unblocks

Independent to build. Criterion 2's full verification no longer waits on
anything — M90 (buy-back) has shipped; the dev restore path remains the
cheaper proof for day-to-day iteration.
