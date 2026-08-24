# M97 — The ship remembers

<!--
Status: active
Update trigger: when the realm split changes what is available to display, when a
display surface is added or cut, when the Ship layout moves
Last updated: 2026-08-24
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

- Criteria 2, 3, 5, 8: `Assets/Tests/EditMode/Ship/RealmRecordViewTests.cs` — the view's
  rendered surfaces (marks lit, mounts filled, plate text) equal `RealmState`'s values after
  a simulated clear, purchase and deeper run, asserted again after a second mutation.
  **Replaces the grep this bullet used to ask for**: the built view carries a packed
  change-detection value that is not a display copy (every paint reads the record fresh),
  so a no-cache grep would fail the correct design — the mutation-repaint assert is the
  check that actually catches a surface trusting a stale number.
- Criteria 1, 4, 6, 9: playthrough. Enter a fresh realm, walk the hull, confirm it reads
  as empty and nothing is broken. Clear a bastion, dock, walk the same route, confirm the
  marks and counts changed.
- Criterion 7: eyes-on, two editor instances. The net fixture cannot see rendered props.

**Playthrough path**: enter a realm with two clean exits and three weapons owned.
From the Boarder spawn, walk to the ARMORY on deck 1 starboard and count three weapons
on the rack (the plate is on the same wall a few metres aft), then take the stair up and
walk the deck-2 port wall to count two marks.

## Premises corrected before building, 2026-08-22

Three of this doc's claims stopped being true when `M96` landed, and are corrected here rather
than worked around.

- **`Ship.unity` does not exist.** Step 2 named it. The realm's ship is `Ship - CoOp.unity`
  (Zo, 2026-08-21), and that is where the surfaces went.
- **Criterion 3 contradicted criterion 8.** The rack was to show "every weapon this realm's
  crew owns", but `M96` put weapons on the BOARDER — `RealmState` has none. The rack shows the
  kit of whoever is aboard, which is what a crew's rack physically is, and criterion 8 is read
  as "reads the record, never a cached copy of it" (Zo, 2026-08-22).
- **Criterion 2 asked for a count that does not exist.** There is no bastions-cleared field,
  and one bastion. The wall shows **one mark per clean exit** — a tally of runs survived here,
  which is real data and fills as you play (Zo, 2026-08-22).

## State at build, 2026-08-22

**Verified in the Unity runner: EditMode 656 passed / 11 failed, PlayMode 68 / 1.** Every one
of those twelve is the recorded pre-existing set; this milestone introduces no failure.

Built: `RealmRecord` (the four numbers, packed into one replicated long, clamped not
truncated), `NetRunMirror.RealmRecordPacked` with `RealmRecordPublisher` pushing host-side,
`RealmRecordSource` as the read seam, and `RealmRecordView` driving 12 marks, 7 weapon mounts
and a plate authored into `Ship - CoOp.unity`.

**Owed.**

- **Eyes on the hull.** Nobody has walked it. Criteria 1, 2 and 6 are judgements about what a
  player can read at a glance, and no test settles them. The geometry was corrected against
  the surrounding wall panels after a review found the rack buried and the mount plates in
  another room entirely, so it is right on paper and unseen in practice.
- **Criterion 7 needs two machines.** A guest reading the host's record is built and cannot be
  shown in one process, the same limit `M96`'s criterion 8 hit.

**Found while building, fixed here rather than left**: buying hatch armour never published, so
every other player aboard read a stale plate for the session; the Ship HUD's salvage and
cleared-wave still came from the visitor's own realm; and FABRICATION was ungated, letting a
guest raise their own realm's hatch armour while standing on someone else's ship.

**Found and NOT fixed**: a guest's shadow sim clamps its start wave and restores doors from its
own realm, diverging its local bastion layout from the host's. It predates the realm split and
is run-critical, so it wants two machines and its own change.

## Update 2026-08-24 — activated; the surfaces survived `M101`, and the owed test exists

`M101` rebuilt the hull out from under the authored surfaces: the record was placed at
fixed world coordinates against the old 117-cell single-deck ship, and the rebuild left
the marks and the rack metres outside a hull that now ends at x=10. The rescue
(2026-08-22, on `M101`) re-homed each group whole, keeping this milestone's own spacing:
the twelve marks now sit on the **deck-2 port wall** (x=-10, z 21.75–38.25, facing
inboard, 2.2 m above the deck), and the seven-mount rack and the plate sit on the
**deck-1 starboard wall in the ARMORY space** (z 16–22.5). `Tools/scene-check/
enclosure.py --decks` is 0/0/0 with every binding row ok on the current scene.

Found against the same wall and fixed here: **the plate's TextMesh was mirrored.** A
legacy TextMesh reads from its own -Z, and every station label on that wall carries +Z
outboard for exactly that reason; the plate carried +Z inboard, so its text was
readable only from inside the wall. One rotation flip (yaw -90 → +90), anchored
middle-centre so nothing moved.

The EditMode test the Verification section owed is written
(`RealmRecordViewTests`, six tests) and passes in the Unity runner along with the seven
packing tests. Full run on this tree, 2026-08-24: **EditMode 728 passed / 14 failed,
PlayMode 78 / 1.** The fifteen are the recorded pre-existing set plus three that
predate this work on today's earlier runs (the in-progress `M95` co-op scene gate, a
Compendium front-matter check, and a second SessionMusic row from the range) — the same
tree ran 722 / 14 before these changes, and the delta is exactly the six added tests.

Still owed, unchanged: **eyes on the hull** (criteria 1, 2, 6 are glance judgements) and
**criterion 7 on two machines**.

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
