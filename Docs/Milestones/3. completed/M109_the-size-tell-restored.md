# M109_ — The size tell, restored

<!--
Status: later
Update trigger: when a gatekeeper kind's authored spread moves, when the navmesh agent radius is rebaked, or when the finale elite's body scale changes
Last updated: 2026-09-01
-->

Every gatekeeper is back at the size it was authored to read at, and the finale elite is a
quarter bigger than it is today — achieved by growing the elite rather than by shrinking the
other four kinds, which is what the 2026-08-20 change did.

## Story

As a Boarder, I want a Default to fill a doorway the way a person does and the elite to be
unmistakably the biggest thing in the control room, so that the size channel the enemy
tutorial claims to use is doing work at both ends instead of only the top.

## Why it matters

`SceneBuilder.Enemies.cs` says colour and size are the only tutorial this game has. On
2026-08-20 the size half was tuned by moving the wrong dial: `BodyScale` went 1.0 → 0.75 so
that the finale elite would tower, and its own comment says so — *"BodyScale shrinks all
three together so the finale elite towers over them"*. The elite got its tell, and the cost
was paid by every other kind. A Default is 0.75 of the size it was authored at, a Hunter
0.6375, and the fight reads smaller than it was drawn.

**The shrink did not even buy what it was for, and that is the finding this milestone
turns on.** The elite's read against a Default is a ratio, and `BodyScale` sits on both
sides of it: elite body is `thiefSpread × BodyScale × FinaleEliteScale`, a Default is
`1 × BodyScale`, so the ratio reduces to `thiefSpread × FinaleEliteScale` and `BodyScale`
cancels. It was `1.4 × 1.5 = 2.1` before 2026-08-20 and `1.4 × 1.5 = 2.1` after. The
changelog's "2.1×, where it was 1.0×" is real, but every bit of it came from **removing
`DressVisual`'s `1/scale` counter-scale** in the same change — until then no kind's body
scaled at all, so the elite rendered at 1.0 like everything else. The 0.75 that rode along
moved this number by nothing, and charged four kinds for it.

So the elite's tell has exactly one dial that has ever worked on it once the counter-scale
was gone: `MasterControl.FinaleEliteScale`. This turns that one, which is presentation only
by construction and costs nothing mechanical, and hands the 0.75 back.

Leverage: **#12, constants** — the weakest tier, and admitted here only because this is not
a retune. It is the reversal of one named decision whose side effect was never wanted, and
the replacement dial is already in the tree and already scoped to presentation. If the
elite still does not read as a boss after this, that is a structural finding about the
control room, not a fourth pass on these three numbers.

**This milestone does not gate `M109`.** `M109`'s Do-not-build list rules out tuning changes
mid-stamp, and that still holds: this is a separate cut that shares the number for filing
only. It is presentation, not one of `M112`'s named dials — body scale is deliberately
absent from `4-Data.md` (changelog, 2026-08-20).

## The numbers

Authored spread is `Default 1 : Hunter 0.85 : Thief 1.2 : Cloaker 1`, times `BodyScale 1.0`.

| Kind | Root scale, was → is | Agent radius | Agent height | baseOffset |
|---|---|---|---|---|
| Default | 0.75 → **1.0** | 0.3 → **0.4** | 1.5 → **2.0** | 0.75 → **1.0** |
| Hunter | 0.6375 → **0.85** | 0.255 → **0.34** | 1.275 → **1.7** | 0.6375 → **0.85** |
| Thief | 1.05 → **1.2** | 0.42 → **0.48** | 2.1 → **2.4** | 1.05 → **1.2** |
| Cloaker | 0.75 → **1.0** | 0.3 → **0.4** | 1.5 → **2.0** | 0.75 → **1.0** |
| Elite body | 1.575 → **2.25** | unchanged (Thief's) | unchanged | unchanged |

**The Thief's authored spread moves 1.4 → 1.2, and that is the one concession.** At the
restored `BodyScale` a 1.4 Thief bakes `0.4 × 1.4 = 0.56` of agent radius onto a navmesh
carved at `agentRadius 0.5` (every scene, `NavMeshSettings` line 105) — the wedging failure
`MasterControl.FinaleEliteScale`'s own comment names, and the one
`EnemyPrefabs_StayInsideTheBakedAgentRadius` exists to catch. 1.2 bakes 0.48 and clears it.
The Thief still ends up **larger than it is today** (1.05 → 1.2); what it gives up is the
1.4 : 1 ratio against a Default, which drops to 1.2 : 1.

Rejected, by name, so it is not re-proposed inside this work:

- **Decoupling the Thief's body from its agent** (render 1.4, keep the footprint at 0.48),
  the trick `GrowEliteBody` uses for the elite. Correct for a boss the player fights once
  in an open hall; wrong for a kind the player shoots hundreds of times, because the
  `CapsuleCollider` rides the root and shots at the shoulders would miss a body that is
  drawn there.
- **Rebaking the navmesh at a larger agent radius.** Every scene rebaked and every doorway
  width in the Bastion kit revalidated, to buy 0.2 of Thief. That is a milestone about
  pathing, not about size, and the ceiling still caps the elite either way.
- **Leaving `BodyScale` at 0.75 and only growing the elite.** It reaches the elite ratio and
  leaves the defect this milestone is about — four kinds smaller than they were drawn.

## Acceptance criteria

1. **`SceneBuilder.Enemies.cs` carries `BodyScale = 1f`**, and the authored spread reads
   `Default 1f`, `Hunter 0.85f`, `Thief 1.2f`, `Cloaker 1f`, each still written as
   `<spread> * BodyScale` so the two channels stay separable.
2. **`MasterControl.FinaleEliteScale == 1.875f`**, and its doc comment states the derived
   elite body scale (2.25) and the ceiling headroom in criterion 6 rather than only the
   navmesh argument it carries today.
3. **The four committed prefabs match the table**, read from YAML with no editor:
   `Assets/Prefabs/{Enemy,Hunter,Thief,Cloaker}Gatekeeper.prefab` each carry root
   `m_LocalScale` and `NavMeshAgent` `m_Radius` / `m_Height` / `m_BaseOffset` at the
   values above, to 1e-4.
4. **No kind is baked wider than the carve.** Every one of the four — the Cloaker included,
   which is untested today — has agent radius ≤ **0.5**. The assertion states the rule, not
   four literals, so a future rescale cannot pass by moving all the numbers together.
5. **The elite is at least 1.8× a Default in body scale** (2.25 against 1.0), asserted from
   the Thief's committed root scale and `FinaleEliteScale` rather than from a baked literal.
   Today's ratio is 2.1×; this is the +25% on the elite dial expressed as the thing it is
   for. **`BodyScale` cancels out of this ratio** — it is on both sides, so the assertion
   reduces to `thiefSpread × FinaleEliteScale` and is identical at 0.75, 1.0 or 2.0. That is
   the design, not an oversight: the relative read is what the size channel delivers to a
   player, and scaling every kind together cannot change it. See the correction below.
6. **The elite's derived height is stated and bounded.** A test computes elite body height
   from `FinaleEliteScale × Thief root × 2.02` (the skinned bind-pose AABB, extent y 1.010
   in `Assets/Packs/Gatekeepers/Thief.prefab`) and asserts it against
   `BastionKit.StoreyHeight − BastionKit.FloorSlabThickness` = 4.5 m. **It lands at ~4.55 m
   — over by ~1%.** The criterion is that the number is computed, named in the failure
   message, and reviewed, not that it passes silently: see the adjudication below.
7. **`GatekeeperPresentationTests` is green** and moves with the change rather than being
   discovered failing — its `body` literal, its per-kind expectations and its doc comments
   all state the new numbers and why they are what they are.
8. **`Tools/verify/run.sh` exit code is read and reported**, and `Tools/docgen/run.sh
   --check` stays clean — body scale is presentation and must not appear in `4-Data.md`.
9. **`Docs/Changelog.md` carries the reversal**, naming the 2026-08-20 entry it reverses and
   why the Thief's 1.4 did not come back with the rest.

## Correction

Stated rather than edited away, because it is the reason this milestone is cheap.

This file was first written claiming criterion 5 stops the tell being lost "by moving
`BodyScale` alone", and that the 2026-08-20 change bought the elite's read by moving the
ratio's denominator. **Both are wrong.** `BodyScale` cancels out of the ratio entirely, so
there is no move of it — alone or otherwise — that criterion 5 can catch or that could ever
have degraded the elite's relative read. What the 2026-08-20 entry actually bought was the
counter-scale removal; its `BodyScale` 0.75 was a no-op on the number it was justified by.

Found while checking that the new test catches a seeded violation, not by reading the
argument again — which is the case for seeding rather than re-reading.

The consequence is that restoring `BodyScale` to 1.0 costs the elite's read **nothing**,
and the whole of the +25% on `FinaleEliteScale` is gain. What criterion 5 does gate is the
two dials that can actually move the ratio: the Thief's authored spread, and
`FinaleEliteScale`.

## Adjudication, pre-registered

Stated before the work, while the decision is still neutral.

**The bet:** growing the elite dial reads as a bigger boss without putting its head through
the control-room ceiling.

**The number that decides it:** criterion 6 computes ~4.55 m of elite against 4.5 m of
opening. That is inside the error bar of a bind-pose AABB — the extent includes arms at
their widest and does not promise the head is the tallest point at rest — so arithmetic
cannot settle it and neither can a test. It is settled by **one look at the elite in the
control room**, during `M109`'s stamp run, at no extra cost to that run.

**If the head clips:** `FinaleEliteScale` comes down once, to the largest value that clears
— not `BodyScale` back down, which is the move this milestone exists to undo. One move,
recorded here with what was seen.

**Not a signal:** whether the elite *felt* big enough, or whether any single kind looks
better in a screenshot. This is about whether the body fits the room it fights in.

## Implementation

**Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs` — `BodyScale` 0.75 → 1f;
`ThiefScale` 1.4f → 1.2f. Update the class remark, which states the spread as `1.4` and
explains the 0.75 shrink: it must now say the elite carries its own tell and why the Thief
stops at 1.2. Leave `DefaultScale` / `HunterScale` / `CloakerScale` in their
`<spread> * BodyScale` form.

**Change** `Assets/Scripts/Sim/Run/MasterControl.cs` — `FinaleEliteScale` 1.5f → 1.875f.
Keep the presentation-only paragraph; add the derived 2.25 and the headroom number. The
0.63 figure in the current comment is stale once the Thief is 1.2 and must be restated.

**Change** the four prefabs under `Assets/Prefabs/` — YAML edit, rung 1: root
`m_LocalScale` x/y/z, `NavMeshAgent` `m_Radius`, `m_Height`, `m_BaseOffset`. These are
generated by `SceneBuilder` and the edit must reproduce exactly what a rebuild would write,
so a later `GatekeepersProtocol/Build` re-run is a no-op diff. Do not touch the
`CapsuleCollider`, the wrapper child references, the `m_LocalScale.x: 1` modification
entries in the prefab-instance override blocks, or any GUID.

**Change** `Assets/Tests/EditMode/Enemies/GatekeeperPresentationTests.cs` — criteria 3–6.
`EnemyPrefabs_KeepTheAuthoredAgentFootprint` takes the new `body` and the 1.2 Thief and
gains the Cloaker. `EnemyPrefabs_StayInsideTheBakedAgentRadius` gains the Cloaker. Two new
asserts for criteria 5 and 6, reading the constants, with the computed metres in the
failure message.

**Change** `Docs/Changelog.md` — criterion 9.

**Change** `Docs/Roadmap.md` — the Order row, and the planned count (this takes the tenth
and last slot; at the cap, stop generating).

**Do not:** move the Default / Hunter / Cloaker spread; scale the elite's agent, capsule or
spawn height; add a second mechanical lever to the elite (`M80`: HP is the one lever);
put body scale into `4-Data.md`; rebake a navmesh; regenerate the prefabs from the editor
as the delivery mechanism when a text edit is the reviewable diff.

## Verification

1. Criteria 1, 2, 9 — read the three files; the constants are the gate.
2. Criteria 3–7 — `GatekeeperPresentationTests`, offline. It parses prefab YAML with
   regex and touches no scene, so it runs under `Tools/verify/run.sh` with no editor.
3. Criterion 8 — `Tools/verify/run.sh`; read and report the exit code, which is **2**
   where `mcs`/`mono` are absent and is not a pass. `Tools/docgen/run.sh --check` clean.
4. Playthrough path (confirmation, and the adjudication above): `M109`'s stamp run, at the
   control room. `GameController.BuildDemoCheckpoint` digit **H** puts the elite window up
   directly, so the look costs one checkpoint start rather than a full run.

## Watch

- `EnemyPrefabs_StayInsideTheBakedAgentRadius` is the load-bearing gate. The Thief at 0.48
  leaves **0.02** of margin against the 0.5 carve — the next spread change has almost none.
- `agent.baseOffset = scale` and the visual child's `localPosition (0, -1, 0)` are a matched
  pair: the body sits on the floor only while both track the root scale. Change one and the
  kinds sink or float.
- `GrowEliteBody` finds the visual by `SkinnedMeshRenderer`, not by name, and refuses to
  grow a body that sits on the root. That refusal is silent apart from one warn — if the
  elite stops growing, look there before looking at the constant.
- `M112`'s dial list does not include body scale and must not gain it here; if play says the
  Thief's *mechanics* are wrong, that is M112's lane, not this file's.

## Do not build

- New enemy kinds, new silhouettes, new materials — the four wrappers are the set.
- A per-kind scale asset or an authoring window. Two constants and a spread is the whole
  surface, and there is no second person to author for.
- A navmesh rebake, or any change to `BastionKit` room dimensions to make the elite fit.
- `M66` hit-feedback or identity polish riding along.

## Depends on / Unblocks

Depends on: nothing — the constants and the tests are all in the tree. Unblocks: nothing
mechanically. It rides `M109`'s stamp run for its one eyes-on adjudication, which is why it
is filed against that number, and it must not delay it.
