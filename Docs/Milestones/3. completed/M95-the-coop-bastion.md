# M95 — The co-op bastion

<!--
Status: completed, Aug 2026 (folder-moved 2026-08-25 at Zo's direction; built 2026-08-23, co-op demo leg unobserved)
Update trigger: when the zone shape changes, when the hatch count changes, when the
sketch parser gains or loses a mark kind
Last updated: 2026-08-23
-->

A second kit layout, `Assets/Scenes/Bastion_Coop.unity`, authored so that a party
of two or three docks two or three hatch doors against it and has to hold all of
them. **20–30 rooms, two storeys.** Ground is the demo spine (R1–R15). Storey 1
is three galleries that do not skip D1/D2/D3. Three hatches on R1.

## Story

As a party arriving on a Bastion, I want a place built around the fact that there are
several of us, so that the layout itself is the reason we cannot stand in one doorway.

## Why it matters

**`Bastion.unity` has one hatch and cannot express the rule.** The demo has a single
`H` in R1. Co-op presents two or three doors. That is the delta, not a reason to
shrink the map.

The one-zone shrink is struck (Zo, 2026-08-22). Demo is **15** rooms on one floor.
Co-op aims **20–30** split across **two** floors (`StoreyHeight` 5). Author for
three seats; two-seat runs mute the extra mouths (`S:id:edge:3`) and seal hatch 2.
Circular Variant A is grammar only — not a graph to copy this pass.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.**

## Acceptance criteria

1. `Bastion_Coop.unity` loads from a Run, is walkable end to end, and has no
   unreachable room, hole or seam.
2. The scene is in Build Settings and enabled, which `NetworkSceneManager` requires
   before any party-wide load.
3. The defended room carries **three** hatch-door positions along the side the ship
   docks to, with stable ids matching the Ship's three.
4. In a two-seat Run, hatches 1 and 3 are open and the middle reads sealed, and
   spawn marks with minSeats=3 (S7–S12) are inactive.
5. Standing at hatch 1, a player can see hatch 3's position and reach it on foot inside
   ten seconds at normal move speed.
6. The ground spine matches the demo: T1+T2 gate D1, T3 gates D2, T4+T5 gate the
   Control Room. Storey 1 does not skip those gates.
7. Every spawn room has a path to every hatch door that is open for its island.
8. Room count is **20–30**, with rooms on storey 0 and storey 1. Sketch is
   `Tools/bastion-sketch/bastion-coop-layout.txt` (28 rooms, three stairs).
9. A guest loads the scene and sees the same room and door state the host does.
10. `Bastion.unity` is unchanged by this milestone.

## Implementation

1. **Create** `Assets/Scenes/Bastion_Coop.unity`. Author it through the existing sketch
   path so the parser owns the graph: `Sim/Bastion/BastionSketchParser.cs` already
   reads mark kinds including `Hatch`, and `Editor/Bastion/BastionSketchEmitter.cs`
   emits from them. Scenes are source (`M35`), so the emitted scene is committed.
2. **Change** the sketch to carry three `Hatch` marks in the defended room rather than
   one. Confirm the parser accepts multiple; if it assumes one, that assumption is the
   first thing to fix and it is small.
3. **Change** Build Settings to add and enable the scene. This is also step 7 of `M92`;
   whichever lands first does it, and the other verifies it.
4. **Author** 20–30 rooms on two storeys. Ground = demo R1–R15. Upper = islands
   U1–U5, U6–U8, U10–U14 at the same XZ, `storey=1`. Stairs `R1 U1`, `R6 U6`,
   `R10 U10`. Parser: `storey=`, `stair`, `S:id:edge:3`.
5. **Use** the demo door graph on the ground. Three numbered hatch marks on R1.
   Two-seat mute is hatch 2 plus S7–S12.
6. **Author** the three stairs in the scene. Emit skips a `Stair_*` already under
   Authored. Pose the dress mesh (yaw 180, local z 10.5, matching R1/R6), destroy
   walk children, then call `BastionSketchStairs.FitWalkAndWellToDress` so climb
   defaults to +Z, the foot is buried, the landing is flush with storey 1, and the
   well is punched from live `Renderer.bounds`. Do not re-emit over those objects.

Rejected: procedurally generating the layout. Nothing else in this project generates a
bastion at runtime, and a demo needs a known shape to author beats against.

## Open questions

- **Whether the Bastion is a distinct hull fiction or the same hull re-cut.**
  *Unknown, and nothing below depends on it.* Art and marketing direction.
- Room counts and terminal counts are **tunable**; the shape is spec. Expect them to
  move once the beats are played.

## Verification

Playthrough is confirmation for remaining seams and for “see hatch 3”. It is not the
only close for disk or graph criteria. Criterion 9 is eyes-on and does not close the
milestone. This file stays in planned; co-op demo is phase 2.

| Criterion | Close |
|---|---|
| 1 walkable / no hole | PlayMode `BastionCoopStairWalkTests.CharacterController_ReachesStoreyOne_OnEachStair` climbs R1/R6/R10 from live mesh bounds (passed 2026-08-23, 6.86 s). Remaining room seams are playthrough. |
| 2 build settings | `BastionCoopSceneTests.BuildSettings_ListsCoopSceneEnabled_OnceTheFileExists` |
| 3 three hatches | Sketch `Parse_CoopLayoutFile_HasThreeNumberedHatchesOnOneRoom`; scene `CoopScene_Exists_HasDemoSpine_AndThreeHatchDoors` fails if Hatch_1..3 are missing |
| 4 two-seat mute | `SeatSpawns_ThreeSeatMarksAreMutedAtTwo`; `ShipHatchesTests.OpenFor_TwoSeats_OpensTheOuterPair` |
| 5 hatch 1→3 in 10 s | `Parse_CoopLayoutFile_HatchOneToThreeIsInsideTenWalkSeconds` (28 m / 5 m/s). Seeing hatch 3 is playthrough. |
| 6 no skip D1/D2/D3 | `Parse_CoopLayoutFile_UpperIslandsDoNotSkipLockedDoors` |
| 7 spawn→hatch | `Parse_CoopLayoutFile_EverySpawnReachesHatchRoom` |
| 8 20–30 two storeys | sketch 28 rooms / 13 storey-1; scene segment-guid count in band |
| 9 guest same state | eyes-on, two editors. Not a close gate. |
| 10 Bastion.unity | `git diff -- Assets/Scenes/Bastion.unity` empty |

**Playthrough path**: Play, Host, embark from the Ship, arrive in `Bastion_Coop`, walk
from the middle of the defended room to each of the three hatch positions, then each
stair to its gallery and back.

## Usable, or it did not ship

**Driven**: walking the layout and standing at a hatch door. **Shown**: three door
positions in the defended room, two open and one sealed at two seats. **Reads back**:
the hatch look-at strings from `M92`, and the run-start log naming the bastion.

## Supporting work

**Shown** depends on `M92` for the hatch door component and the seat-count assignment.
If `M92` slips, this scene can still be authored and walked; criteria 3 and 4 cannot
close until it lands.

## Do not build

A second destination. Circular Variant A as a new graph. Per-hatch HP or enemy target
selection, which are the demo's. Any change to `Bastion.unity`.
