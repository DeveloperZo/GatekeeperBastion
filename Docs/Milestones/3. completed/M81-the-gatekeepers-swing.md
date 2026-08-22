# M81 — The Gatekeeper's swing

<!--
Status: completed
Closed on: 2026-08-19 (Zo). Criteria 1, 2, 4, 5 stamped by the close request;
criterion 3 already clean on disk. Kill criterion: free path accepted (not
rejected).
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-20 (correction: the flagged generator drift is closed;
see the end of "What has landed")
-->

One retargeted attack clip into the shared Gatekeeper controller, so the Attack
trigger the code already fires shows a swing instead of the player's idle pose.
This is also Phase 0 of the asset strategy (`../../Architecture/asset-audit-2026-08-18.md`,
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
   definition: **Copy From Other Avatar, sourcing `SK_UtilityBot.fbx`**
   (corrected 2026-08-19 against the file).

   The paragraph this replaces said the clip carries `mixamorig` and that the
   avatar should be Create From This Model. Both are wrong for this pack, and
   the correction is the reason Copy From Other Avatar "errored when tried" was
   also a misreading. Read off the two FBXs directly: `Anim_attack_melee.fbx`
   and `Running.fbx` have **byte-identical skeletons** — the same 72 bones,
   named `root.x`, `thigh_stretch.l`, `c_index1.l`, which is the Auto-Rig Pro
   naming of `SK_UtilityBot.fbx`, not `mixamorig`. That matches what the
   inventory already recorded for `Running.fbx`: downloaded on the uploaded
   SK_UtilityBot character with its rig intact, zero retarget. `Default.prefab`
   carries the SK_UtilityBot avatar; Hunter and Thief carry their own and are
   retargeted onto by Unity.

   So the working configuration for this pack is the one `Running.fbx` already
   uses and the one the attack clip already had. `Anim_attack_melee.fbx.meta`
   now differs from `Running.fbx.meta` in the clip block alone — name, trim and
   loop time — and in nothing else. Boarder's six are `mixamorig` and are a
   different pack; do not reason from them about this one.

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
   controller settings, not code). Landed by trim alone — the state's speed
   stays `1`, so the swing plays at its authored rate.

### What has landed

**Landed 2026-08-18 (agent, Unity closed, nothing run):** steps 3 and 4 are
applied in the `.controller` text — Attack `m_Motion` now points at
`Anim_attack_melee` (guid `03ede1e3f3df7b14fbb44ae773ed0c02`) and the
Attack -> Locomotion `m_ExitTime` is `0.9`.

**Landed 2026-08-19 (agent, Unity closed, nothing run):** step 5, and the
fileID risk above is closed.

*The fileID is no longer inferred.* `Anim_attack_melee.fbx.meta` now carries a
`clipAnimations` block that states `internalID: -203655887218126122`
explicitly, so the controller's reference is pinned by the meta rather than by
a guess about how Unity derives it. The guess was right — the file's take is
`mixamo.com`, read out of the binary FBX, the same take name `Running.fbx`
carries with that internalID written down — but it is now a fact in a file
instead.

*The clip is fitted, without opening the editor.* The download is **3.5s, 105
frames at 30fps, and it is a two-swing combo**, not one swing; nothing on disk
said so. Sampling the FBX's rotation curves for the right arm gives the shape:
frames 7-33 are a short forward strike (contact ~f26), frames 44-60 are a
wider horizontal slash sweeping `arm_stretch.r` through 74 degrees (contact
~f51), and everything after f60 decays back to idle.

The clip block trims to the **second slash, `firstFrame: 44`, `lastFrame: 60`**
— 16 frames, 0.533s. Contact lands mid-clip; the state's 0.9 exit fires at
0.48s, well past it, and the 0.08s blend out puts the Gatekeeper back in
Locomotion 0.56s after the trigger, inside the 0.7s interval with room. The
first strike was rejected for the fit: its windup-through-recovery span is 26
frames, 0.87s, over the clock, and trimming it to fit cuts either the windup or
the contact. `loopTime` is `0`; a swing that repeats is not a swing.

The second slash was also the larger read — 74 degrees of arm travel against
roughly 30 for the first. That is a curve measurement, not a judgement about
how it looks; **which one reads better is a play question**, and swapping is
one edit to the two frame numbers if the first strike is preferred.

Criterion 3 reads clean in the file. Criteria 1, 2, 4 and 5 are structurally in
place and unproven — they need the playthrough below. Nothing was run.

The controller asset is authored by `SceneBuilder.Enemies.EnsureGatekeeperController()`,
which returns early when the file already exists, so the hand edit survives
normal builds. It would be lost only if the `.controller` were deleted and
regenerated, and the generator would rebuild the idle stand-in.

**Correction, 2026-08-20 (agent).** The sentence above used to end "still not
made, and still the one way this milestone silently reverts." That is no longer
true: the generator now authors what the shipped asset holds. It loads
`Packs/Gatekeepers/Animations/Anim_attack_melee.fbx` through the same
`LoadHumanClip` the locomotion clips use and sets the Attack -> Locomotion exit
time to `0.9`, so a regenerated controller matches the hand edit instead of
reverting it. A missing clip logs an error naming the path and falls back to
Idle, rather than rebuilding the stand-in pose silently.

Not observed: the generator actually regenerating. Proving it means deleting the
committed `.controller` — and its `.meta`, whose guid three pack prefabs
reference — which is not worth the risk to a live pack for a dormant path. What
was checked instead: the Editor assembly compiles in Unity (the bridge's
precheck passed), `lint-arch` is clean, the full suites moved no test, and the
clip resolution is deterministic because `Anim_attack_melee.fbx.meta` defines
exactly one clip, `attack_melee`. Its import block is byte-identical to
`Running.fbx.meta` outside guid, clip name, trim and `loopTime`, and `Running`
is a working clip in this controller's blend tree.

## Verification

1. Criterion 3 — from the two files, no editor. The Attack state block in
   `Gatekeeper.controller` must name guid `03ede1e3f3df7b14fbb44ae773ed0c02`
   **and** fileID `-203655887218126122`, and that fileID must equal the
   `internalID` in the `clipAnimations` block of `Anim_attack_melee.fbx.meta`.
   Two files agreeing is the gate; a build that had not done the work fails it,
   which the old "confirm the old guid is absent" wording did not. Then confirm
   `92fb57b81e89f7f4b955089c4177c7a7` appears in the blend tree's `m_Childs`
   only.

```bash
grep -A16 'm_Name: Attack$' "Assets/Packs/Gatekeepers/Gatekeeper.controller" | grep m_Motion
grep -A4 'clipAnimations:' "Assets/Packs/Gatekeepers/Animations/Anim_attack_melee.fbx.meta" | grep internalID
```
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
