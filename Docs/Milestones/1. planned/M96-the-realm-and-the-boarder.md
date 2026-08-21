# M96 — The realm and the Boarder

<!--
Status: planned
Update trigger: when an item moves between the character side and the realm side, when
the save version changes, when the realm list gains a feature
Last updated: 2026-08-20
-->

One save becomes two. A **Boarder** is yours and travels: weapons, focus-ring upgrades,
protocols, equipment. A **realm** is a Ship and the bastion progress attached to it, and
it stays with whoever owns it. Playing in someone else's realm means their world keeps
the clears and the ship upgrades while your Boarder and its kit come home with you.

## Story

As a player joining a friend's game, I want the weapons and upgrades I earned to come
with me and their world's progress to stay theirs, so that neither of us loses anything
by playing together.

## Why it matters

**The ownership question is already ruled and currently unimplementable.** Zo ruled
2026-08-20 that a guest does work on someone else's ship and takes things back to their
own, with clear ownership over shared. `MetaState` has no seam that expresses it:
`BankedSalvage`, `EquippedPrimary`, `EquippedProtocol`, `WeaponsOwned`,
`WeaponWheelPoints`, upgrade levels and run counters are one document with one owner.
Persistence was deferred in the co-op demo specifically because this seam did not exist
(`../../Demo - Coop/README.md`).

**The model is borrowed and simplified.** `../../Brainstorms/realms.md` records the
source: No Rest for the Wicked's Realms, where the character is fully portable and the
world holds cleared areas, boss kills, town upgrades and resources.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.** This adds a
structure later work sits on rather than tuning a value.

## The split, ruled

Zo approved the initial allocation 2026-08-20 with the note that **play testing will
dictate the specifics**. Items may move; the seam does not.

| Boarder, travels | Realm, stays |
|---|---|
| Weapons owned | Salvage pot |
| Focus-ring upgrades per weapon | Bastions cleared |
| Equipped Primary and Secondary | Door and unlock state |
| Protocols claimed | Ship and hatch upgrades |
| Equipped protocol | Deepest wave reached |
| Equipment and drone | Runs lost, clean exits |
| | Realm name |

**Stated consequence, accepted rather than mitigated**: with weapons and upgrades on the
Boarder, a fresh realm can be entered by an arbitrarily strong player. Realm difficulty
cannot assume a curve it controls. That is the source model's behaviour and Zo's
farm-valve read of it is recorded under *Watch* in the brainstorm.

## Acceptance criteria

1. A player creates a realm from the main menu, names it, and enters it with an existing
   Boarder.
2. That Boarder arrives carrying every item in the left column: the same weapons owned,
   the same focus-ring points on each, the same equipped Primary, Secondary and protocol.
3. The new realm starts with none of the right column: no bastions cleared, no door
   unlocks, no ship upgrades, and the starting pot rather than a carried balance.
4. Buying a weapon in realm A and entering realm B shows that weapon owned in realm B.
5. Clearing a bastion in realm A and entering realm B shows that bastion uncleared in
   realm B.
6. Quitting to the main menu and re-entering realm A restores its cleared state, door
   state, ship upgrades and pot exactly.
7. A guest joining a host's realm plays in the host's world state and, on leaving, keeps
   every left-column change made during the session.
8. Nothing a guest does writes to the host's Boarder, and nothing the host does writes to
   a guest's.
9. Two realms exist simultaneously with independent right-column state and neither
   writes into the other.
10. A save written before this milestone does not load, and the game says so plainly
    rather than loading a corrupted state.

## Implementation

1. **Change** `Sim/Meta/SaveData.cs`. `CurrentVersion` 11 → 12, and split the payload
   into two serialized documents rather than one. Per the standing rule
   (`../../Decisions.md`): bump the schema, rename the keys, **never write a migration
   shim**. Criterion 10 is that rule's visible face.
2. **Create** `Sim/Meta/BoarderState.cs` (pure Sim). Left-column fields, lifted out of
   `MetaState` rather than duplicated.
3. **Create** `Sim/Meta/RealmState.cs` (pure Sim). Right-column fields, same lift.
4. **Change** `Sim/Meta/MetaState.cs` to compose the two rather than own the fields.
   Every existing read site keeps working through the composition; that is what makes
   this a restructure rather than a rewrite.
5. **Change** `GameController` so the realm side is host-owned and mutated only through
   command methods (R1, R3). A guest's Boarder is written locally by that guest; the
   realm is written only by the host.
6. **Create** a realm list on MainMenu: create, name, enter, and pick which Boarder
   enters. No difficulty selection, which is the source model's and not a question this
   project has.
7. **Change** `SaveService` call sites so Boarder and realm write independently. A realm
   write must not require a Boarder write or the guest case deadlocks.

Rejected: keeping one document with an owner field per record. It reads as cheaper and
makes every future query ask "whose is this", which is exactly the confusion the split
exists to remove.

## Verification

- Criteria 1-6, 9: EditMode tests over save and load of the two documents, asserting
  each column lands in the right place and that a second realm's right column is
  independent. Then a playthrough: create realm A, buy a weapon, clear a bastion, quit,
  create realm B, confirm the weapon and not the clear.
- Criteria 7, 8: PlayMode net test against `NetcodeIntegrationTest`. Host and guest, the
  guest buys, the guest disconnects, assert the guest's Boarder holds the purchase and
  the host's does not.
- Criterion 10: load a v11 save and read the refusal in `Logs/gkb_latest.log`.

**Playthrough path**: MainMenu, create realm "A", enter with a Boarder, buy a Primary,
clear one bastion, quit to menu, create realm "B", enter with the same Boarder. The
Primary is in your hands. The bastion is uncleared.

## Usable, or it did not ship

**Driven**: creating a realm, naming it, choosing which Boarder enters. **Shown**: the
realm list on the main menu, and a fresh world with a familiar gun. **Reads back**: the
weapon in hand in a realm that has never seen it, and the realm's own cleared state on
return.

## Unblocks

`M97` (the ship shows the realm's record) and `M98` (drop-in), neither of which has
anything to display or join without this.

## Do not build

Realm difficulty selection. Multiple realms as a headline feature beyond criterion 9's
correctness. Host migration. Any migration shim. A shared player wallet, which stays
rejected: the realm pot is the world's salvage, not a pooled personal balance.
