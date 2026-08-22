# Ship Hull and Interior

<!--
Purpose: The shape of the player's ship and how its interior reads — hull form, deck
count, room metrics, lighting model and set dressing. Not what the ship's stations do,
which is the station milestones; not the exterior of any other vessel.
Status: brainstorm
Update trigger: when M101 lands a hull, or when a remaining entry graduates.
Last updated: 2026-08-22
-->

## What this covers

The form and feel of `Assets/Scenes/Ship - CoOp.unity` — its footprint, silhouette,
deck count, corridor and ceiling metrics, lighting model, windows and dressing. The
brief is *Normandy-inspired, not a replica* (Zo, 2026-08-22). It does not cover what
the four stations do, the hatch assignment rules, or the demo beats that play on the hull.

## Where it stands

Measured from the scene file 2026-08-22, not from docs.

The hull is **117 cells on a 5 m grid, one deck**: x[-25,25] and z[-30,65], so 50 m of
beam by 95 m of length, **1.9 : 1**. Ceilings are flat at **y = 4.0** in every cell. The
main spine corridor is a full cell wide — **5 m**. Visible geometry is
`ShipDress` (Synty `PolygonSciFiSpace`); collision is a parallel graybox under
`Environment` whose renderers are all disabled.

Three findings that a rebuild has to answer, all pre-existing:

- **There is no exterior skin.** The `SM_Bld_Wall_Exterior_*` family is unused; interior
  `SM_Bld_Wall_01` panels do hull duty. From outside, and now through the windows added
  2026-08-22, the ship is a set of room boxes with slab edges showing between them.
- **Twenty wall seams step 0.892 m.** The hull was generated room by room, so each room
  placed its own walls with its own inside/outside convention; wherever two rooms share a
  line the pieces alternate N/S or E/W and the wall plane jumps by one wall thickness,
  leaving an open return. Lines affected: x = -15, 10, 20 and z = -20, -10, 0, 25, 40.
  The one Zo found is `S_50_400` against `Win_N_150_400` at z = 40.
- **Nothing in the scene emits light.** There is exactly one `Light` (the directional);
  all 58+ "lights" are emissive Synty meshes. Unity emissive only affects Lightmap Static
  geometry, and reaches dynamic objects through Light Probes alone — so the ceiling panels
  glow without lighting anything, and a flat ambient is doing all the real work.

`Sim/Ship/ShipHatches.cs` fixes `Count = 3` and opens the middle hatch for one seat, the
outer pair for two, all three for three — with a comment that the hatches sit at the far
ends of the hull by construction. That rule wants a long linear hull and is currently
satisfied by a gallery down one side of a wide one.

## Decided

- **Two decks, joined by an open stair, not an elevator.** Decided 2026-08-22 (Zo).
- **Hatches live on the command deck, beside the CIC.** Decided 2026-08-22 (Zo).
- **Walkable area is ~80 cells across both decks**, down from 117 on one. Decided
  2026-08-22 (Zo).

The footprint, the stair and the deck program left for `M101`.

## Graduation test

**The hull exists.** Every remaining entry is placement or treatment against geometry, so
none can be specified until `M101` has landed a hull to place against.

## Constraints

Any milestone from this file respects these.

- **Kit metrics tile or they drift.** Bethesda's rule (GDC 2013): every sub-kit footprint
  must be an integer multiple of every other, and the grid snap is half the smallest
  footprint. A 2.5 m corridor sub-kit tiles with the 5 m room module at 1.25 m snap; a 3 m
  one never will.
- **One wall line, one owner.** Every piece on a given line uses the same convention, so
  the wall plane cannot step. This is what removes all twenty seams by construction rather
  than by twenty fixes.
- **Colliders stay on the graybox.** Every dress `MeshCollider` in the scene is disabled
  and collision comes from `Environment`. New dress inherits that; a rebuilt hull still
  needs its graybox rebuilt alongside it.
- **Ceiling height changes by piece, never by scale.** Every wall in the kit is exactly
  4.0 m tall and the doorframe aperture is 2.56 m, so scaling a wall to shorten a room
  yields a door too low to walk through. `SM_Bld_Ceiling_02` (underside 3.49 m) against
  `SM_Bld_Ceiling_01` (4.0 m) is the whole available range without new art; deeper contrast
  has to come from a double-height volume.
- **The three hatches keep a middle.** `ShipHatches.OpenFor` picks index 1 alone at one
  seat, so the hull needs a real midships position, not two ends and a filler.

## Ideas

### Engine pods forward of their pylon roots

If only one silhouette feature survives, this is it. The Normandy's pylons root in the aft
third and sweep outboard and *forward*, putting the glowing nacelles alongside the waist
rather than trailing the stern. Almost nothing else does this, which is why it reads
instantly. Pods behind the roots is a generic spaceship.

### Light from what people are doing, not from the ceiling

The stated art brief for the Normandy interior is *a modern submarine, with crew working at
bright computer screens in an otherwise dark environment*. That is the inverse of a ceiling
panel scheme: the light is at chest height and comes from work surfaces. The published
starting ratio for key-to-fill is about 3 : 1, and the failure mode named by a Call of Duty
lighting artist is exactly ours — a light on every fixture, nothing dark, so nothing reads
as bright and the eye loses the contours of the room. Pairing each emissive panel with a
real Rect light, dropping the ambient, and adding fog rather than fill is the shape of it.
Note the light-count cap: plain Forward allows 8 additional lights per object.

### A dressing register per deck or wing

The Nostromo ran four distinct dressing languages in one ship — sterile command, cluttered
mess, oily industrial hold — and the Normandy does the same over its decks. One kit plus
several dressing registers reads as several places, which is the cheapest variety
multiplier available and needs no new modules. The matching research finding: players
object to repeated *detail* far sooner than to repeated *architecture*, so the variation
budget belongs in decals, props and material instances, never in more wall meshes.

### One window you stop at, many you walk past

The 28 windows added 2026-08-22 are all the same size and all in circulation space, which
makes them all walk-past windows. The Normandy's memorable views are attached to its
*least* functional rooms — a bar and a library — because the window is the reason to be
there. A window with nothing in front of it is scenery regardless of size. One hero
window at the end of a framed sightline with a seat, plus small rhythmic portholes
elsewhere, is worth more than a uniform band of glass. Apertures also need real thickness,
glass and a seal; a zero-depth hole reads as a cut-out, not a pressure hull.

### The ship should contain a model of itself

Cheap and disproportionately effective in Mass Effect: the galaxy map cursor is the
Normandy, and the CIC carries a second hologram of the ship showing its installed upgrades.
We already have a Map station and a realm record; a hull hologram that reflects the
realm's actual state is the same trick and it gives the Map station a reason to be a
destination rather than a console.

## Rejected

**A five-deck SR-2 layout.** Five decks needs an elevator, deck streaming, and a
per-deck rebuild of hatch assignment and spawn addressing, and the SR-2's aft third exists
to solve a problem we do not have (it is too heavy to land, so it carries a shuttle). The
SR-1's three decks map cleanly to the 5 m grid; two of them map more cleanly still.

**Regenerating the hull from `SceneBuilder.Ship`.** The generator is what produced the
twenty seams and the missing exterior skin, and re-running it destroys hand authoring —
recorded already, but worth restating here because a hull rebuild is exactly when someone
reaches for it.
