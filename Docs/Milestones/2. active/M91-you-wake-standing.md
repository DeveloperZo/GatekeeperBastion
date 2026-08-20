# M91 — You wake standing

<!--
Status: next (current)
Update trigger: when the wake sequence, the glass, or the beat-0 reload matrix moves.
Last updated: 2026-08-20
-->

Beat 0's wake capsule becomes a teleporter: you materialise **upright** on a pad, inside a
glass cylinder that retracts when you release it. The horizontal bed goes, and three
defects it caused go with it.

## Story

As a player waking on a dead Ship, I want to materialise standing inside a tube I can see
holding me, so that the first thing the game does is legible instead of a black screen I
cannot move in.

## Why it matters

**The bed is the source of three dated defects, all commented in `ShipCapsule.cs`.** A
horizontal pose in a standing rig forced `PlacePlayerInBed` to take XZ from the marker but Y
from the rig (2026-08-18, falling through the deck), forced yaw-only rotation (2026-08-18,
skewed mouse look), and forced `SetCapsuleTrigger(true)` — which is **never restored**, so
the capsule's colliders are ghosts for the whole session because the standing rig shares its
footprint. A pad below and an emitter above share no footprint with a standing rig, and all
three go away rather than being managed.

**The glass makes `Rooted` diegetic.** Today the stir phase leaves the player able to look
but not walk with nothing on screen explaining why, which reads as a bug on a first play. A
tube that has not retracted yet explains it without a line of dialogue. That is Sensation
(rank 5) paying for itself, and it costs the same 0.8 s animation the lid already runs.

Not a tuning change; no leverage tier is touched. The wake constants keep their values
(`Design/4-Data.md` is not involved — these live in the script as beat-0 authored timings).

## Acceptance criteria

1. **Fresh wake places the player standing.** With `DemoActive` and `DemoCapsuleOpened`
   false, the player materialises upright on the pad: look free, movement rooted, **look
   pitch 0°**, glass closed, screen black then fading. No teleporter collider overlaps the
   `CharacterController` at any point.
2. **The prompt is at eye level.** The unlock raycast is horizontal — the interact collider
   sits on a ring or console at the pad's edge, not overhead and not underfoot. `Prompt`
   reads `[E] release` / `[E] releasing` while sealed and null otherwise.
3. **Hold-E retracts the glass.** Holding for `UnlockHoldSeconds` (1.0 s, unchanged) drops
   the glass into the pad over `GlassRetractSeconds` (0.8 s), then unroots movement.
   **No forced pitch at any point in the sequence** — wherever the player was looking is
   where they stay.
4. `MetaState.DemoCapsuleOpened` writes true and saves on release, exactly as today. The
   field keeps its name.
5. **Reload after release**: glass down, no reseal, no fade, teleporter is furniture, player
   placed at the `Spawn_Teleporter` marker with the NAV standing objective — the beat-0
   reload matrix is otherwise unchanged.
6. **`DemoActive` false**: dormant — glass down, no interaction, no fade, no voice.
7. **No collider is switched to trigger at runtime.** `SetCapsuleTrigger` no longer exists;
   a grep for it returns nothing outside this file's history.
8. Dev checkpoints `F12` 1–4 still adopt with beat 0 complete and never touch the
   teleporter.
9. **Playthrough**: from Play on a fresh save, wake, release, and walk to the NAV MAP
   without the view snapping or the body clipping. Confirms 1–3 and 5 on screen.

## Implementation

**Rename, do not adapt.** `ShipCapsule.cs` becomes `ShipTeleporter.cs` (`git mv`), class
`ShipTeleporter`. The phase machine survives intact — `Dormant → Black → Fade → Stir →
Sealed → Opening → Done` — because a materialise fade-up is the same fade the wake already
runs. Roughly 60 of its 230 lines change; the rest is a rename.

**`MetaState.DemoCapsuleOpened` keeps its name.** It is a serialized save field; renaming it
costs a `SaveData` version bump and a migration for zero player-visible benefit. Add a one
line comment saying the name predates M91.

| File | Action | What |
|---|---|---|
| `Game/Ship/ShipCapsule.cs` | **delete** (via `git mv`) | becomes `ShipTeleporter.cs` |
| `Game/Ship/ShipTeleporter.cs` | **create** | below |
| `Game/Ship/DemoShipDirector.cs` | change | `_capsule` → `_teleporter` (`ShipTeleporter`); `_spawnCapsule` → `_spawnTeleporter`. Call sites unchanged: `BeginSealed` / `BeginOpen` / `BeginDormant` keep their names and contracts. |
| `Sim/Meta/MetaState.cs` | change | comment only, on `DemoCapsuleOpened` |
| `Assets/Scenes/Ship.unity` | **Zo authors** | see *Scene contract* |

**Changes inside `ShipTeleporter.cs`:**

- **Delete `SetCapsuleTrigger`** and both its calls. Pad and emitter colliders stay solid
  and stay solid for the session. *The glass carries no collider at all* — the player is
  `Rooted` for its entire closed life, so it has nothing to block, and a solid shell around
  the `CharacterController` is the depenetration bug this milestone exists to remove.
- **Delete `LidLookPitchDegrees`** and the `SetLookPitch` call in placement, and delete the
  corrective `_fpc.SetLookPitch(0f)` in the `Opening → Done` transition. Both existed only
  because the sealed pose looked 50° down at a lid. Starting at 0 needs no correction, and
  keeping the correction would yank the view of a player who looked up during the stir.
- **`PlacePlayerInBed` → `PlaceOnPad`**, keeping the **XZ-from-marker, Y-from-rig** rule and
  the **yaw-only** rotation rule verbatim with their dated comments. Those two rules are
  correct for a standing rig and each records a real defect; only the pitch line leaves.
  `_bedPose` → `_padPose`.
- **`_lid` → `_glass`; lift becomes retract.** `LidLiftMetres` → `GlassRetractMetres`,
  moving **down** into the pad (`Vector3.down`), not up. Rejected alternative: rising into
  the emitter above, which either clips it or forces the emitter far enough up that the
  tube stops reading as a tube.
- Log lines: `"spawn capsule"` → `"spawn teleporter"`, `"capsule unlocked"` →
  `"teleporter released"`. Standing objective `"Unlock the capsule"` → `"Release the
  teleporter"`. `DemoConversationIds.WakeStir` and `Wake` are unchanged — the lines still
  fit.

**Scene contract** (Zo authors; `Architecture/8-Scene-Authoring.md` — one subtree, one
commit, and the swap asserts the arrival):

| Object | Role | Binds to |
|---|---|---|
| `Demo Teleporter` | root, carries `ShipTeleporter` | — |
| `Demo Teleporter/Pad` | floor disc, solid collider | — |
| `Demo Teleporter/Emitter` | overhead housing, solid collider | — |
| `Demo Teleporter/Glass` | the cylinder, **no collider** | `_glass` |
| `Demo Teleporter/Console` | ring or panel at eye height, interact collider | the `IInteractable` raycast target |
| `Demo Teleporter/PadPose` | standing pose marker | `_padPose` |
| `Spawn_Teleporter` | on-deck marker after release | `_spawnTeleporter` |

Parts already owned: `Synty/PolygonSciFiSpace/Prefabs/FX/FX_LightRayRound.prefab` for the
beam, `SM_Hud_Rings_01` / `_04` for the materialise ring, `SM_Prop_Light_Panel_*` for the
pad glow. A scaled cylinder with a transparent material is the glass. **The FX are Shown,
not gated** — criteria 1–9 pass on graybox primitives; the dressing rides M61-2.

### What has landed

**Landed 2026-08-20 (agent): the code half. The scene half is Zo's and is not
started.**

`ShipCapsule.cs` → `ShipTeleporter.cs` by `git mv`, **`.meta` moved with it**, so
guid `e9544522404ff234d83f4cda8b38ce20` survives and `Ship.unity`'s component
reference still resolves. The phase machine is untouched. Applied per the change
list: `SetCapsuleTrigger` and both calls deleted; `LidLookPitchDegrees` and both
`SetLookPitch` calls deleted; `PlacePlayerInBed` → `PlaceOnPad`, `_bedPose` →
`_padPose`, `_lid` → `_glass`, `LidLift*` → `GlassRetract*` moving `Vector3.down`;
log lines and the standing objective restamped. The XZ-from-marker and yaw-only
rules are kept verbatim with their dated comments — only the trailing sentence
"Pitch goes through the controller's own pipeline below" changed, because after
the deletion it promised a call that no longer exists.

`DemoShipDirector`: `_capsule` → `_teleporter` (`ShipTeleporter`), `_spawnCapsule`
→ `_spawnTeleporter`, tooltips and the unwired-error text. `MetaState` gained the
comment on `DemoCapsuleOpened`; the field keeps its name.

**Two things the change list did not mention, found by building:**

1. `DemoProgressTests.UnlockReload_PlacesAtCapsuleSpawn` pins the director's
   **source text** — `StringAssert.Contains("PlaceAt(player, _spawnCapsule)")`.
   The rename would have failed it. Renamed to
   `ReleaseReload_PlacesAtTeleporterSpawn` and re-pinned on `_spawnTeleporter`.
2. `Docs/Demo - Single/0-wake-on-a-dead-ship.md` is the beat-0 spec and pins the
   prompt strings, the standing objective, both log lines and the lid mechanics.
   Its own update trigger is "when spawn furniture changes", so it was updated:
   beats 0-4, the reads-back roll-up and the PlayMode tape. `README.md`'s one-line
   summary with it.

**Landed 2026-08-20 (agent): the scene subtree, on Zo's call.** The rename left
`_teleporter`, `_glass`, `_padPose` and `_spawnTeleporter` reading null, and a
fresh wake logged "no teleporter wired" — beat 0 was dead, which is how the gap
was found, in play. Authored with a one-shot `[MenuItem]` in `Assets/_Scratch/`,
run through the MCP bridge and deleted after; the scratch folder is back to
`.gitkeep`.

*Authored twice.* The first pass was lost when `Ship.unity` was reverted to HEAD
mid-session — a concurrent agent had also run `SceneBuilder.Ship` into the same
file, which pushed the scene diff to ~18,800 lines, and Zo restored it. Re-run
against the clean file the subtree costs **550 lines and 23 objects**, which is
the honest size of this milestone's scene change; the rest was never ours. The
lesson is the one already on the record for this repo: re-check the tree before
reasoning about it, and keep a scene edit small enough that its diff is readable.

The wake object is reused rather than rebuilt, so the `ShipTeleporter` component
and its scene position survive: `Demo Capsule` → **`Demo Teleporter`** at
`(7.5, 0, 6.2)`, and `Bed Pose` → **`PadPose`**. `Bed` and `Lid` are
**deactivated, not deleted** — reversible, and their solid boxes are exactly what
would shove the `CharacterController`. Deleting them is a one-line cleanup left
to Zo.

| Object | Local | Collider |
|---|---|---|
| `Pad` | y 0.1, ⌀1.6 × 0.2 | Box, y 0.00→0.20 |
| `Glass` | y 1.4, ⌀1.6 × 2.4 | **none** |
| `Glass Inner` | child of `Glass`, scale `(-1,1,1)` | **none** |
| `Emitter` | y 2.75, ⌀1.76 × 0.3 | Box, y 2.60→2.90 |
| `Console` | y 1.75, z 0.98 | Box, 0.92 m from the capsule axis |

**The geometry is read off the rig, not assumed.** `Player` sits at y 1.1 with a
`CharacterController` of height 1.8 and centre 0 — so its feet are at **0.2**,
and that, not 0.1, is the deck. `PlayerCamera` is a child at local y 0.65, so the
eye is at **1.75**, which is where the console had to go for a horizontal unlock
ray. A first pass placed both 0.1–0.15 too low and was corrected before landing.

**Criterion 1's collider clause is checked, not asserted.** Against the real
capsule (r 0.35 + 0.08 skin, y 0.2→2.0): `Pad` touches the feet at 0.20 without
crossing them, `Emitter` clears the head by 0.60 m, `Console` clears the axis by
0.92 m horizontally, and `Glass` has no collider at all. The two that *would*
overlap, `Bed` and `Lid`, are inactive.

**Two corrections the primitives forced.** A Unity cylinder ships a
`CapsuleCollider`, and a capsule squashed flat cannot go below twice its radius —
it stays a fat sphere bulging into the standing rig, which is the depenetration
shove this milestone exists to delete. `Pad` and `Emitter` carry `BoxCollider`s
instead. And `GlassRetractMetres` moved **1.1 → 2.5**: 1.1 was a slab-lid number,
and a 2.4 m tube dropped 1.1 m is still chest-high, so criterion 3's "drops the
glass into the pad" would have failed on sight.

**Known graybox limit:** Built-in `Standard` exposes no `_Cull` here, so the tube
would vanish when the camera is inside it. `Glass Inner` — the same cylinder
mirrored on one axis, parented to `Glass` so it retracts with it and `_glass`
stays one binding — draws the inside surface. Whether that reads as glass is a
play question; the dressing rides M61-2.

**Flagged, not changed: the stamped copy.** This doc rules that `WakeStir` and
`Wake` "still fit". `demo.wake` opens **"Capsule open."**, which now describes
nothing on screen. Changing stamped copy is a voice-registry change this
milestone did not ask for, so it stands — but it is the line a first player will
read against a glass tube that just dropped into a pad.

## Verification

- **Criteria 1–6, 9** — play. `F12` is not usable here: checkpoints skip beat 0 by
  construction (criterion 8), so this needs a fresh save. Delete
  `%USERPROFILE%/AppData/LocalLow/<company>/<product>/gatekeeper_meta.json`, press Play,
  and read the session log for `[Demo] spawn teleporter`, then `[Demo] teleporter released`,
  then `Repair the NAV MAP` as the standing objective. A run that logs `spawn teleporter`
  and never logs `teleporter released` means the console raycast is not reachable —
  criterion 2 failed.
- **Criterion 7** — `grep -rn "SetCapsuleTrigger" Assets/Scripts` returns nothing.
- **Criterion 8** — `F12` then `1`; the Ship loads with NAV powered and no prompt on the
  teleporter. Repeat for `2`, `3`, `4`.
- **Regression** — the offline harness and AutoVerify stay green against the pre-existing
  failing set; `BuildSettings` and the EditMode pins on `BuildDemoCheckpoint` are the ones
  that would move if `MetaState` drifted.

**Driven** — hold `E` at the console. **Shown** — the glass is visibly holding you, and the
standing objective names the verb. **Reads back** — the glass drops, movement unroots, and
two log lines bracket it.

## Depends on / Unblocks

Depends: nothing — beat 0 is shipped and this replaces its opening object.
Unblocks: the respawn shape. A teleporter is the same object `M57`'s downed-player return
wants, and `Ship.unity` already carries a second `Spawn_Capsule` marker beside the demo one.
**Out of scope here** — this milestone changes beat 0's wake only.

## Trap

Do not "fix" the XZ-from-marker, Y-from-rig placement rule or the yaw-only rotation while
renaming them. Both are load-bearing and each records a 2026-08-18 defect; a marker Y
starts the `CharacterController` inside the deck, and marker pitch skews every subsequent
mouse look. The pitch line is the only one that leaves.
