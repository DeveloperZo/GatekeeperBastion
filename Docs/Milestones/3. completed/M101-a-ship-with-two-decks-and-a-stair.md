# M101 — A ship with two decks and a stair

<!--
Status: next (current)
Update trigger: when the hull layout table below changes, or when the deck program moves a
station between decks.
Last updated: 2026-08-22
-->

Rebuilds the hull of `Ship - CoOp` as a long, narrow, two-deck vessel joined by an open
stair through a double-height void. Replaces the 117-cell single-deck plan. Geometry only:
lighting, dressing registers and the hero window stay in
[`ship-hull-and-interior`](../../Brainstorms/ship-hull-and-interior.md).

## Story

As a player, I want the ship to read as a vessel I live inside rather than a floor plan,
so that walking to a hatch is part of the game instead of a distance to cross.

## Why it matters

The hull today is 50 m × 95 m on one deck — a **1.9 : 1** footprint, 117 cells, every
ceiling flat at y = 4.0, and a main spine corridor a full cell wide. The reference plans put
a comparable playable deck at 30–40 m long by 18–24 m wide, and every published metric puts
a ship corridor at 2.0–2.6 m against our 5. That gap is why dressing passes have not moved
it: the ship is dimensioned like an office and shaped like a barge.

Three defects are structural rather than cosmetic and are fixed by construction here, not
by patching: there is **no exterior skin** (the `SM_Bld_Wall_Exterior_*` family is unused
and interior panels do hull duty); **twenty wall seams step 0.892 m** because the generator
let each room own its own side of a shared line; and the hull is **enclosed only by
graybox**, so from outside it reads as loose room boxes. Detail in the brainstorm's *Where
it stands*.

Deck count and hatch deck decided 2026-08-22 (Zo): **two decks, open stair not elevator;
hatches on the command deck.** Walkable area decided the same day: **~80 cells across both
decks**, down from 117 on one.

## Acceptance criteria

1. `Ship - CoOp` contains exactly two walkable decks, deck 1 floor at `y = 0` and deck 2
   floor at `y = 5`, of **40 and 36 floor cells** respectively — 76 total — laid out per the
   table in *Implementation*. Deck 2 is four short because those four cells are the void.
2. Walking from the deck 1 stair foot to the deck 2 stair head requires no loading screen,
   no trigger volume and no scene change — the player traverses geometry only.
3. From the deck 2 stair head the player can see the deck 1 floor of the double-height
   void without moving.
4. Every hull perimeter edge on both decks is covered by a piece from the
   `SM_Bld_Wall_Exterior_*` family or a window, and `enclosure.py` (see *Verification*)
   reports **0 open hull edges** and **0 graybox walls without visible dress**.
5. No wall line in the scene contains pieces of two opposing kinds — the seam check
   reports **0** plane steps.
6. All three `ShipHatchDoor` objects are on deck 2, on the starboard hull, at three
   distinct `z` stations with one of them within 5 m of the walkable mid-length, so
   `ShipHatches.OpenFor(1)` opens a hatch that is actually amidships.
7. All four stations, the `Ship Terminal`, the teleporter and every `Spawn_*` object are
   present, stand on a floor cell, and are reachable on foot from the player spawn.
8. In a Run started from the ship, the player can walk spawn → Map station → an open hatch
   without passing through any space wider than 5 m other than the CIC and the void.

## Implementation

Cell notation: cell `(ix, iz)` spans `x[5·ix, 5·ix+5]`, `z[5·iz, 5·iz+5]`. Bow is `+z`,
matching the current scene. The hull is symmetric about `x = 0`.

### Hull plan — both decks share this footprint

| Row | z | Cells (ix) | Beam | Deck 1 | Deck 2 |
|---|---|---|---|---|---|
| 0 | 0–5 | −1..0 | 10 m | Engine room | Aft observation |
| 1 | 5–10 | −1..0 | 10 m | Engine room | Aft observation |
| 2 | 10–15 | −2..1 | 20 m | Fabrication | Gallery |
| 3 | 15–20 | −2..1 | 20 m | Fabrication / Armory | Gallery + **hatch 1** at z 17.5 |
| 4 | 20–25 | −2..1 | 20 m | Armory | Gallery |
| 5 | 25–30 | −2..1 | 20 m | **Stair foot** (port), drive core | Stair head (port), **void** centre |
| 6 | 30–35 | −2..1 | 20 m | Drive core | Landing, **hatch 2** at z 32.5 |
| 7 | 35–40 | −2..1 | 20 m | Mess | CIC |
| 8 | 40–45 | −2..1 | 20 m | Crew quarters | CIC, **hatch 3** at z 47.5 |
| 9 | 45–50 | −2..1 | 20 m | Crew quarters | CIC — Map station on the forward bulkhead |
| 10 | 50–55 | −1..0 | 10 m | Forward stores | Cockpit approach |
| 11 | 55–60 | −1..0 | 10 m | Forward stores | Cockpit |

Walkable envelope 60 m × 20 m, 3 : 1 — the exterior reaches ~5.5 : 1 once the non-walkable
bow and drive sections are added below.

**There is exactly one double-height volume**, and the stair and the drive core share it.
It is cells `(−1,5) (0,5) (−1,6) (0,6)` — `x[−5,5]`, `z[25,35]` — where deck 2 has no
floor. The drive core stands on the deck 1 floor of that volume and reads full height from
both decks; the stair climbs its port edge and the deck 2 landing rings it at `ix = −2`
and `ix = 1`. One volume rather than two because a second double-height space halves the
impact of the first — it is the only place on the ship where the ceiling is not 4 m, and
that is what makes it the payoff for the walk.

### Geometry rules

**One wall line, one owner.** For every line, all pieces use the owner's convention: a
line at constant `z` is owned by the cell to its **south** and dressed with `N` pieces; a
line at constant `x` is owned by the cell to its **west** and dressed with `E` pieces. This
is what makes criterion 5 true by construction; it is the rule the current hull lacks.

**Corridors are 2.5 m.** The spine runs `x[−1.25, 1.25]` down the centreline on both decks.
Wall pieces are 5 m long and sit off the cell grid — legal, since only floors and ceilings
are gridded and they run underneath. Rooms open off the spine to port and starboard.

**Ceiling height varies by piece, not by scale.** `SM_Bld_Ceiling_02` (underside 3.49 m) in
corridors, `SM_Bld_Ceiling_01` (4.0 m) in rooms. **Do not scale walls to change height** —
every wall in the family is 4.0 m tall and the doorframe aperture is 2.56 m, so a 0.65 Y
scale yields a 1.66 m opening the player cannot walk through. The height contrast in this
ship comes from the void, not from squashed corridors.

**Cross-section is an octagon.** Flat top, 45° upper chamfers, flat sides, 45° lower
chamfers, flat bottom, applied to the exterior skin only. Walls rotate freely so this needs
no new art; floor tiles overshoot under an angled wall and the skin hides the overhang.
Chosen over a chamfered *plan* because the section is what stops a box reading as a box,
and it costs one rule rather than a bespoke footprint per row.

**Exterior skin** is `SM_Bld_Wall_Exterior_01..04` on every perimeter edge, outboard of the
interior wall, with `SM_Bld_Wall_Exterior_Pillar_01` at each plan corner. The interior
`SM_Bld_Wall_01` stays as the inside face. This is the layer the ship has never had.

**Vertical grid is 5 m.** Deck 1 floor `y = 0`, deck 2 floor `y = 5`. A 4 m wall plus the
ceiling slab to 4.489 plus deck 2's floor at 4.793–5.0 leaves a 0.3 m structural void that
is never visible.

**The stair** is a straight run up the void's port edge, from the deck 1 floor at
`z = 25` to the deck 2 landing at `z = 35`, rising 5 m over 10 m — **26.6°**, with a
landing at mid-height. Open on the void side with a rail. Chosen over a switchback because a straight
run keeps the deck 1 floor visible from the head, which criterion 3 requires.

### Files

**Create** `Assets/Scenes/Ship - CoOp.unity` hull subtree — `Environment` (graybox: floor,
ceiling and wall boxes per the table, renderers disabled, colliders enabled) and
`ShipDress` (Synty kit, colliders disabled), both rebuilt from scratch on the new plan.
**Create** `Tools/scene-check/enclosure.py` — the perimeter and seam checker used by
criteria 4 and 5; it already exists as a scratch script and graduates here because it is
run against every future hull edit, not once.
**Change** the four `Station_*`, `Ship Terminal`, `Demo Teleporter`, `Spawn_*` and
`Hatches` transforms — rehomed per the table.
**Delete** the 28 `Win_*` pieces and the `ShipDress/Detail` group from the 2026-08-22
pass — they are placed against a hull that no longer exists. The window *rule* survives in
the brainstorm; the placements do not.

**Create** `Tools/ship-hull/build_hull.py` — the generator. Reversed from the original
plan to keep it outside the repo: the scene diff is ~386k lines and cannot be reviewed
directly, so the auditable artefact has to be the 430-line script plus the checker. It
carries a destructive-operation warning because, like `SceneBuilder.Ship`, it deletes every
child of `Environment` and `ShipDress`. Re-running it is a decision, never a chore.

**Do not run `SceneBuilder.Ship`.** It produced the twenty seams and the missing skin, and
re-running it destroys hand authoring. The hull is authored offline as YAML with the editor
closed, per the practice already recorded for this scene.

## Open questions

**Criterion 8 is not met by the first build.** The hull shell, decks, void, stair, skin and
hatches are in; the side strips are still a single 8.75 m volume running the ship's length
because no interior room partitions exist yet. The partitions are awkward on this grid: a
5 m doorframe's opening is centred on a cell, so it does not line up with a spine corridor
centred on `x = 0`. Either the corridor moves to be centred on a cell, or the partitions
take a short return piece the kit does not have. **Owner: Zo.** Until it is settled the
rooms named in the layout table are labels, not enclosures.

## Verification

- **Criteria 1, 6, 7** — `python Tools/scene-check/enclosure.py --decks` prints the floor
  cell count per deck (expect `40 / 40`), the `y` of each deck plane (`0.0`, `5.0`), the
  three hatch `z` stations, and a reachability check listing every station, terminal,
  teleporter and spawn with the cell it stands on. Passing output names no object as
  `off-floor` or `unreachable`.
- **Criteria 4, 5** — the same script's `OPEN HULL EDGES` and seam counts must both read
  `0`. Both currently read `0` and `20` respectively on the pre-rebuild scene, so the seam
  number is the one that proves the change.
- **Criteria 2, 3, 8** — playthrough path: open `Ship - CoOp` directly (it is not in Build
  Settings), press Play, walk from the spawn to the Map station, then to the hatch that
  `ShipHatches.OpenFor(1)` opened, then down the stair to the drive core. Read
  `[ShipHatchAssignment]` in the console for the opened index. **This is confirmation, not
  a close gate** — no replayable slice covers ship traversal yet (`Decisions.md`,
  2026-08-22); close on the script output and confirm on the next play.
- **Regression** — full bridge run. The recorded pre-existing failure set is in the
  verification baseline; this milestone must not add to it. No C# changes here, so a clean
  EditMode/PlayMode delta is the expectation, not a hope.

## Depends on / Unblocks

Unblocks the lighting, dressing-register and hero-window entries in
[`ship-hull-and-interior`](../../Brainstorms/ship-hull-and-interior.md) — all three are
placement work against a hull that has to exist first.
