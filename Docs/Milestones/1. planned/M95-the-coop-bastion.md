# M95 — The co-op bastion

<!--
Status: planned
Update trigger: when the zone shape changes, when the hatch count changes, when the
sketch parser gains or loses a mark kind
Last updated: 2026-08-20
-->

A second derelict layout, `Assets/Scenes/Bastion_Coop.unity`, authored so that a party
of two or three docks two or three hatch doors against it and has to hold all of them.
One zone plus the finale. Smaller than `Bastion.unity`.

## Story

As a party arriving on a derelict, I want a place built around the fact that there are
several of us, so that the layout itself is the reason we cannot stand in one doorway.

## Why it matters

**`Bastion.unity` has one hatch and cannot express the rule.** The current layout has a
single `H` in R1 and a door graph keyed to terminal completions across three zones. The
co-op demo's whole argument is that a party presents two or three doors to hold, and
there is nowhere in the tree that shape can exist.

**Travel is not the content here.** The solo demo spends its length teaching verbs.
This demo teaches nothing, so a three-zone walk is dead time. One zone plus the finale
is the shape (`Docs/Demo - Coop/README.md`), and the counts are tunable while the shape
is spec.

**The reference layout is a guide, not a command** (Zo, 2026-08-20). `Circular Variant
A` shows the grammar this project's bastions use: sized rooms, hub rooms at 21, spawn
rooms feeding a defended room, terminals gating doors, a Control Room at the end. Take
the grammar; do not copy the graph.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.** A new structure
the demo's beats run inside.

## Acceptance criteria

1. `Bastion_Coop.unity` loads from a Run, is walkable end to end, and has no
   unreachable room, hole or seam.
2. The scene is in Build Settings and enabled, which `NetworkSceneManager` requires
   before any party-wide load.
3. The defended room carries **three** hatch-door positions along the side the ship
   docks to, with stable ids matching the Ship's three.
4. In a two-seat Run, hatches 1 and 3 are open in this scene and the middle reads
   sealed, matching the Ship's assignment.
5. Standing at hatch 1, a player can see hatch 3's position and reach it on foot inside
   ten seconds at normal move speed.
6. The layout is one zone plus the finale: field terminals gating one bank, then the
   Control Room and its final window.
7. Every spawn room has a path to every open hatch door, so a body can target any of
   them.
8. Total room count is lower than `Bastion.unity`'s.
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
4. **Author** the room sizes from the existing grammar: hub rooms at 21, the defended
   room large enough that three door positions are not adjacent, spawn rooms at 8.
5. **Do not** re-use `Bastion.unity`'s door graph. This layout's doors key on bank
   success per the ruled loop unit, not on terminal completion.

Rejected: procedurally generating the layout. Nothing else in this project generates a
bastion at runtime, and a demo needs a known shape to author beats against.

## Open questions

- **Whether the derelict is a distinct hull fiction or the same hull re-cut.**
  *Unknown, and nothing below depends on it.* Art and marketing direction.
- Room counts and terminal counts are **tunable**; the shape is spec. Expect them to
  move once the beats are played.

## Verification

- Criteria 1, 5, 6, 8: playthrough. Embark into the scene, walk every room, time the
  run between hatch 1 and hatch 3, count rooms against `Bastion.unity`.
- Criteria 2, 10: read `ProjectSettings/EditorBuildSettings.asset` for the enabled
  entry, and `git diff` on `Bastion.unity` showing no change.
- Criteria 3, 4, 7: EditMode test over the parsed sketch asserting three hatch marks
  with the expected ids, and a navmesh path assertion from each spawn room to each
  hatch position.
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

A second destination. A third zone. Per-hatch HP or enemy target selection, which are
the demo's. Any change to `Bastion.unity`.
