# M95 — The co-op bastion

<!--
Status: planned
Update trigger: when the zone shape changes, when the hatch count changes, when the
sketch parser gains or loses a mark kind
Last updated: 2026-08-22
-->

A second kit layout, `Assets/Scenes/Bastion_Coop.unity`, authored so that a party
of two or three docks two or three hatch doors against it and has to hold all of
them. **20–30 rooms, two storeys.** Ground is the demo spine (R1–R15). Storey 1
is three galleries that do not skip D1/D2/D3. Three hatches on R1.

## Story

As a party arriving on a derelict, I want a place built around the fact that there are
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

Rejected: procedurally generating the layout. Nothing else in this project generates a
bastion at runtime, and a demo needs a known shape to author beats against.

## Open questions

- **Whether the derelict is a distinct hull fiction or the same hull re-cut.**
  *Unknown, and nothing below depends on it.* Art and marketing direction.
- Room counts and terminal counts are **tunable**; the shape is spec. Expect them to
  move once the beats are played.

## Verification

- Criteria 1, 5, 6, 8: playthrough. Embark, walk both storeys, time hatch 1 to 3,
  confirm 20–30 rooms and the three stairs.
- Criteria 2, 10: read `ProjectSettings/EditorBuildSettings.asset` for the enabled
  entry, and `git diff` on `Bastion.unity` showing no change.
- Criteria 3, 4, 7: EditMode test over the parsed sketch — three hatch marks, S7–S12
  minSeats=3, zone-1 spawns path to R1. Scene emit later.
- Criterion 9: eyes-on, two editor instances. The net fixture cannot test scene load
  across MPPM.

**Playthrough path**: Play, Host, embark from the Ship, arrive in `Bastion_Coop`, walk
from the middle of the defended room to each of the three hatch positions, then to a
field terminal and back.

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
