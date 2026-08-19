# M81 — The Gatekeeper's swing

<!--
Status: later
Update trigger: when the attack clip lands, when the sourcing kill criterion
fires, or when a Gatekeeper kind gains distinct attack behavior
Last updated: 2026-08-18 (reviewed 2026-08-18: exit-time and swing-clock steps
added; clip path and naming updated to the Animations/ sidecar convention)
-->

One retargeted attack clip into the shared Gatekeeper controller, so the Attack
trigger the code already fires shows a swing instead of the player's idle pose.
This is also Phase 0 of the asset strategy (`../../asset-audit-2026-08-18.md`,
section 6): the first proof of whether free retargeted clips read on Synty
proportions, which every later clip-sourcing decision depends on.

## Story

As a player, I want a Gatekeeper to visibly swing when it damages me or the
Hatch, so that incoming damage has a readable cause.

## Why it matters

Every enemy attack in the game currently plays `Anim_PlayerIdle` — the Attack
state's motion in `Packs/Gatekeepers/Gatekeeper.controller` is PlayerIdle's
guid (audit, verified 2026-08-18). `EnemyGatekeeper.Attack()` and
`ShipIntruder` both fire the trigger, and damage is applied in the same call.
Combat is being judged with its feedback layer missing; graybox evidence
under-reads exactly this kind of gap.

Decisions made here, not left to the implementer:

- **One clip, shared by all three kinds and ship intruders.** They share the
  controller; `HunterGatekeeper` is a 34-line Kind override, and kinds differ
  by stats and material tint, not behavior. Per-kind attack clips would
  differentiate nothing the player can attribute to behavior. Deferred, with
  trigger: a kind gains distinct attack behavior.
- **Source: the free retarget path** (Mixamo or equivalent), through the same
  humanoid-avatar route the six Boarder clips already use. Rejected for now:
  purchasing a pack — the audit's purchase gate needs 5 fillable tracker lines
  and this is 1.
- **Damage timing stays same-frame with the trigger.** The clip's contact
  frame will trail the damage moment. Moving the damage application is combat
  behavior change, out of this milestone's grain. Whether the lag is
  perceivable: unknown, needs play data. Prefer a short-windup clip and trim
  lead-in via import settings.

## Acceptance criteria

1. In a Run, a Gatekeeper in melee range plays a visible attack motion each
   time the player takes melee damage from it, and the motion reads through to
   its contact rather than cutting off during the windup.
2. A Gatekeeper at the Hatch plays the same motion when the hatch damage
   sparks fire.
3. The Attack state motion in `Packs/Gatekeepers/Gatekeeper.controller`
   references the new clip's guid; `Anim_PlayerIdle`'s guid
   (`92fb57b81e89f7f4b955089c4177c7a7`) appears only in the locomotion blend
   tree.
4. Idle, walk and run are unchanged (only the Attack state's motion moves).
5. A ship intruder in the Ship fight plays the same swing (shared controller,
   no fork).

## Kill criterion

This milestone carries the sourcing bet: *free retargeted generic clips read
acceptably on Synty proportions.* If two free clips have been wired and
rejected in play by **2026-09-15**, the free path is dead for character clips;
run the purchase gate from the audit's section 6 before any third attempt.
Rejection is Zo's play report, recorded in this file. Date movable while the
work is unstarted.

## Implementation

All asset and editor work, no new code. Zo executes; steps in order:

1. Source one humanoid melee attack FBX from a free library.
2. **Create** `Packs/Gatekeepers/Animations/Anim_attack_melee.fbx` — import as
   Humanoid. Clip files live in the `Animations/` subfolder under the new
   lowercase descriptive naming (ruled 2026-08-18, Zo), each with a `.md`
   sidecar recording its Mixamo settings (`Animations/README.md`). Avatar
   definition: **Create From This Model** (landed 2026-08-18). The clip carries
   the `mixamorig` skeleton, the same one `Anim_PlayerIdle` uses and verified
   absent from Synty's `Characters.fbx`, so Unity builds the mapping from the
   file itself. Copy From Other Avatar sourcing `Anim_PlayerIdle` is equivalent
   and was the Boarder six's route; it is insurance against auto-mapping, not a
   requirement, and it errored here when tried. The clip's avatar describes the
   clip's own skeleton — the Gatekeeper's Synty avatar is the retarget target
   and is untouched by this choice.
**Landed 2026-08-18 (agent, Unity closed, nothing run):** steps 3 and 4 are
applied in the `.controller` text — Attack `m_Motion` now points at
`Anim_attack_melee` (guid `03ede1e3f3df7b14fbb44ae773ed0c02`) and the
Attack -> Locomotion `m_ExitTime` is `0.9`. The clip fileID
(`-203655887218126122`) was **inferred** from the six Boarder clips, which all
share it because it derives from the `mixamo.com` take name; the FBX's
`clipAnimations` block is still empty so it could not be read directly. If the
Attack state shows a missing motion on next open, that inference is why.
Step 5 (fitting the clip to the 0.7s swing clock) is untouched and needs the
editor. Criterion 3 reads clean in the file; every other criterion is unproven.

3. **Change** `Packs/Gatekeepers/Gatekeeper.controller` — Attack state's
   motion becomes the new clip. No new controller; forking would split three
   behaviorally identical kinds for no reason.
4. **Change** the same controller's Attack -> Locomotion transition: its exit
   time is `0.2`, set for an idle stand-in, so the state leaves at 20% of the
   clip and a real swing would show only its windup. Raise it past the contact
   frame.
5. Fit the clip to the swing clock: every kind attacks on the same 0.7s timer
   (`WaveDirector.GatekeeperAttackInterval`), so the whole motion has to read
   inside that. Trim the lead-in or raise the state's speed (import and
   controller settings, not code).

The controller asset is authored by `SceneBuilder.Enemies.EnsureGatekeeperController()`,
which returns early when the file already exists, so the hand edit survives
normal builds. It would be lost only if the `.controller` were deleted and
regenerated, and the generator would rebuild the idle stand-in. Updating that
method to match is code and a separate call.

## Verification

1. Criterion 3 — open the Attack state in the Animator window and read the
   motion field; or search the `.controller` text for the Attack state block
   and confirm the old guid is absent from it.
2. Criteria 1, 2, 4 — playthrough path: from Play, start a Run, hold position
   near the Hatch until a Gatekeeper closes; take one melee hit and watch for
   the swing at the damage flash. Then let one reach the Hatch: sparks and
   swing together. The existing log line "gatekeeper hitting the hatch"
   stamps the damage moment if the co-occurrence is in doubt.
3. Criterion 5 — play the Ship fight to the intruder spawn; same observation.
4. Quality is decided by playing, not by this gate; the play report feeds the
   kill criterion above.

## Watch

Locomotion works today. Repair discipline applies: only the Attack state's
motion field changes; do not touch the blend tree while in there.

## Depends on / Unblocks

Depends on nothing. Its play result is Phase 0 evidence for every later clip
decision (M82's clips are hand-keyed and unaffected; Phase 2 sourcing is not).
