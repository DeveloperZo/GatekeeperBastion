# M97 — The ship remembers

<!--
Status: planned
Update trigger: when the realm split changes what is available to display, when a
display surface is added or cut, when the Ship layout moves
Last updated: 2026-08-20
-->

The Ship stops being four stations in an empty hull and starts showing what this realm
has done: bastions cleared, runs lost, the weapons this crew owns on a rack, the deepest
wave reached. A display pass on the hull `M92` already built, reading state `M96`
already stores.

## Story

As a player walking onto a realm's ship, I want to see what has been done here, so that
the place feels like somewhere a crew has been rather than a menu with corridors.

## Why it matters

**The record exists and is invisible.** `M96` puts bastions cleared, runs lost, clean
exits, deepest wave and ship upgrades on the realm. Today every one of those is a
number in a save file or a line in `gkb_latest.log`, and the Ship shows none of it:
`Ship.unity` is `Stations` plus `ShipDress` with two beds and nothing else. `M92`
deliberately excluded display objects so the hull could land first.

**It is the immersion argument, and it is Zo's** (2026-08-20): Payday 3 shipping without
a safehouse after its predecessor had one was a real loss, and this class of mechanic is
a rich source of immersion rather than decoration. `../../Brainstorms/realms.md` records
it, along with the reason it displays the **realm's** record rather than any one
player's — the same job Sacrament does in the source model.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No rule changes.
What changes is that state the game already keeps becomes something you walk past.

## Acceptance criteria

1. Standing on the Ship, a player can name at least three things this realm has done
   without opening a menu.
2. Bastions cleared in this realm are shown as a count and as distinguishable marks, and
   the display changes after clearing another.
3. Every weapon this realm's crew owns is visible on a rack in the ARMORY space, and a
   weapon bought in this session appears there before the next embark.
4. Runs lost and clean exits are shown somewhere on the hull.
5. Deepest wave reached is shown, and rises after a deeper run.
6. A realm with nothing done reads as empty rather than broken: no missing-texture
   placeholder, no zero-count label where an absence should be.
7. A guest aboard sees the host realm's record, identical to what the host sees.
8. Every display reads from `RealmState` and none of it holds its own copy of a number.
9. No new station, no new interaction verb, and no new input binding.

## Implementation

1. **Create** `Game/Ship/RealmRecordView.cs`. `REPLICATED-STATE` header (R5). Read-only
   (R2): it subscribes and renders, and never writes. One component driving all the
   surfaces below rather than one script per prop.
2. **Change** `Ship.unity` to place the surfaces: a wall for bastion marks, the ARMORY
   rack, and a plate carrying runs lost, clean exits and deepest wave. Scenes are source
   (`M35`), so the placed objects are committed.
3. **Change** `NetRunMirror` to carry the realm record fields a guest needs, so criterion
   7 holds without a guest reading a save it does not own.
4. **Reuse** the weapon prefabs from the viewmodel work rather than authoring rack
   props. If a weapon has no mesh yet it occupies a labelled empty mount, which satisfies
   criterion 6 without inventing art.
5. **Do not** add a Compendium page, a stats screen or a menu. If a number needs a menu
   to be legible it does not belong on the hull.

Rejected: a plaque with a text dump of every stat. It is cheap and it reads as a debug
overlay. The rule is that a surface shows one thing physically, and the count is legible
without reading.

## Verification

- Criteria 2, 3, 5, 8: EditMode test that the view's rendered values equal `RealmState`'s
  after a simulated clear, purchase and deeper run, plus a grep asserting no cached copy.
- Criteria 1, 4, 6, 9: playthrough. Enter a fresh realm, walk the hull, confirm it reads
  as empty and nothing is broken. Clear a bastion, dock, walk the same route, confirm the
  marks and counts changed.
- Criterion 7: eyes-on, two editor instances. The net fixture cannot see rendered props.

**Playthrough path**: enter a realm with two bastions cleared and three weapons owned.
From the Boarder spawn, walk to the ARMORY and count three weapons on the rack, then to
the record wall and count two marks.

## Usable, or it did not ship

**Driven**: walking the hull. **Shown**: the marks, the rack and the plate. **Reads
back**: counts that change after a run, without a menu.

## Supporting work

Depends on `M92` for the hull that has room for any of this, and on `M96` for a realm
record to read. Neither is optional: without `M96` there is nothing to display, which is
why this milestone could not be written while persistence was deferred.

## Do not build

A stats menu. A Compendium page. Player-specific display, which contradicts the realm
model. Cosmetic customisation. New stations or verbs.
