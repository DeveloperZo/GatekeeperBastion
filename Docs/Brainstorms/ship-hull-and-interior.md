# Ship Hull and Interior

<!--
Purpose: The shape of the player's ship and how its interior reads — hull form, deck
count, room metrics, lighting model and set dressing. Not what the ship's stations do,
which is the station milestones; not the exterior of any other vessel.
Status: brainstorm
Update trigger: when the hull is rebuilt again, or when a remaining entry graduates.
Last updated: 2026-08-30 (`M104` took the lighting entry; one-deck collapse stands)
-->

## What this covers

The form and feel of `Assets/Scenes/Ship - CoOp.unity` — its footprint, silhouette,
deck count, corridor and ceiling metrics, lighting model, windows and dressing. The
brief is *Normandy-inspired, not a replica* (Zo, 2026-08-22). It does not cover what
the four stations do, the hatch assignment rules, or the demo beats that play on the hull.

## Where it stands

**The interior is one deck.** `M101` rebuilt the hull as two decks joined by an open stair
(merged 2026-08-22, measured from the scene file 2026-08-25); the collapse back to a single
deck was decided 2026-08-26 (Zo) and is the layout of record. Deck 2, the open stair and the
double-height void are gone. Everything that was upstairs came down to `y = 0` keeping its
`x` and `z`.

`Assets/Scenes/Ship - CoOp.unity` is therefore the **40** floor cells of the old deck 1,
one plane at `y = 0`, in the same footprint: 20 m of beam amidships narrowing to 10 m at
bow and stern, bow `+z`. The NAV MAP (`Station_Map`) is forward on the port bow bulkhead at
`(-7.5, 0, 50)`; the `Ship Terminal` is amidships to port at `(-4, 0, 21)`; `Hatch_1/2/3`
are on the starboard hull at `x 9.65`, `z 17.5 / 32.5 / 47.5`; `Spawn_Nav` is forward-centre
at `(0, 0, 42.5)` and `Spawn_Intruder_1/2` are starboard at `(7.5, 0, 17.5)` and
`(7.5, 0, 47.5)`. The ARMORY `(10, 0, 12.5)`, FABRICATION `(-10, 0, 17.5)`, CREW QUARTERS
`(10, 0, 42.5)` and the teleporter and player spawn (port, `z` 41–42.5) were already on that
floor and did not move. The twelve realm-record marks move to the **starboard wall in the
`z` 5–15 band** (Zo, 2026-08-26) — they collided with the port glass where the collapse put
them.

**The exterior is deliberately not being reprofiled.** It keeps the proportions the two-deck
rebuild gave it, because the ship's outside is to be presented as an image rather than
modelled. The hull did not get shorter and the skin is untouched: **28** `Skin1_*` panels and
**23** `Skin2_*`, so the ship is still not a set of room boxes seen from outside, and the
one-wall-line-one-owner rule below is still what keeps the seams deleted rather than patched.
Visible geometry is `ShipDress` (Synty `PolygonSciFiSpace`); collision is a parallel graybox
under `Environment` whose renderers are all disabled.

The one pre-rebuild finding neither the rebuild nor the collapse answers:

- **Nothing in the scene emits light.** There is still exactly one `Light` (the
  directional); the rest are emissive Synty meshes. Unity emissive only affects Lightmap
  Static geometry, and reaches dynamic objects through Light Probes alone — so the ceiling
  panels glow without lighting anything, and a flat ambient is doing all the real work.
  This is what the lighting idea below is against.

`Sim/Ship/ShipHatches.cs` fixes `Count = 3` and opens the middle hatch for one seat, the
outer pair for two, all three for three, with the hatches at three `z` stations bow to stern
by construction. The long linear hull that rule always wanted survives the collapse — the
length was never what the second deck was for — so the gallery workaround stays gone.

**Open against the collapse**, and not guessed at here: how many windows the one deck
actually carries now that the eight cut into deck 2 have gone with it, and whether the
untouched exterior skin still shows apertures with nothing behind them; and what became of
the drive core, which stood on the deck 1 floor of the void and read full height from both
decks. Both want a measurement, not a decision.

## Decided

**One deck (Zo, 2026-08-26).** The interior collapses to `y = 0` and the second deck, the
open stair and the double-height void go with it. The exterior is not reprofiled to match,
because the outside of the ship is to be presented as an image. This supersedes the
2026-08-22 call for two decks joined by an open stair rather than an elevator, and the
~80-cells-across-both-decks target with it: walkable area is the 40 cells of the one deck.

What survives from 2026-08-22 and still places every idea below: the brief is
*Normandy-inspired, not a replica*, and the hatches sit beside the CIC on the deck the
player spawns on — which is now the only deck there is. `M101` remains the record for the
footprint and the geometry rules even though its deck program does not.

## Graduation test

**Satisfied — the geometry exists to place against** (`M101` landed the hull 2026-08-22; the
collapse to one deck 2026-08-26 changed which surfaces are available, not whether there are
any). Every remaining entry is placement or treatment against geometry, so each is
specifiable on its own. What this file still needs is a decision about which one is worth
doing; none is blocked on the hull.

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
  `SM_Bld_Ceiling_01` (4.0 m) is the whole available range without new art, and with the
  void gone there is no double-height volume left to borrow deeper contrast from. Height is
  not a variable this ship can play with any more; plan, dressing and light are.
- **The three hatches keep a middle.** `ShipHatches.OpenFor` picks index 1 alone at one
  seat, so the hull needs a real midships position, not two ends and a filler.

## Ideas

### Engine pods forward of their pylon roots

If only one silhouette feature survives, this is it. The Normandy's pylons root in the aft
third and sweep outboard and *forward*, putting the glowing nacelles alongside the waist
rather than trailing the stern. Almost nothing else does this, which is why it reads
instantly. Pods behind the roots is a generic spaceship.

### A dressing register per stretch of the hull

The Nostromo ran four distinct dressing languages in one ship — sterile command, cluttered
mess, oily industrial hold — and the Normandy does the same over its decks. On one deck the
same trick runs fore-and-aft instead of up-and-down, and the hull is long enough to carry
it: the narrow bow around NAV, the wide midships the hatches open off, the stern where the
shops are. This entry is worth more now than it was, because with the void gone it is the
main variety lever left. One kit plus several dressing registers reads as several places,
which is the cheapest variety multiplier available and needs no new modules. The matching
research finding: players
object to repeated *detail* far sooner than to repeated *architecture*, so the variation
budget belongs in decals, props and material instances, never in more wall meshes.

### One window you stop at, many you walk past

The hull carried **12** windows when it was measured 2026-08-25 — 4 on deck 1, 8 on deck 2,
all port or starboard — down from the 28 the pre-`M101` hull had. Two thirds of those were
cut into a deck that no longer exists, so the count wants re-measuring before anything is
placed against it. The objection does not depend on the number: they were all the same size
and all in circulation space, which makes them all walk-past windows. The Normandy's
memorable views are attached to its
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

## Graduated

| What | Milestone |
|---|---|
| Light from work surfaces, not the ceiling; one realtime light; glow band | `M104` (planned) |

## Rejected

**A five-deck SR-2 layout.** Five decks needs an elevator, deck streaming, and a
per-deck rebuild of hatch assignment and spawn addressing, and the SR-2's aft third exists
to solve a problem we do not have (it is too heavy to land, so it carries a shuttle). The
SR-1's three decks map cleanly to the 5 m grid; two mapped more cleanly still, and as of
2026-08-26 the interior is on one — so the distance from here to five decks is larger than
it has ever been, and this rejection is correspondingly firmer.

**Regenerating the hull from `SceneBuilder.Ship`.** The generator is what produced the
twenty seams and the missing exterior skin, and re-running it destroys hand authoring —
recorded already, but worth restating here because a hull rebuild is exactly when someone
reaches for it.
