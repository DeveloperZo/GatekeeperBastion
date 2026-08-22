# M100 — A realm you can open while its owner is offline

<!--
Status: planned
Update trigger: when the realm document stops living in one machine's save directory,
when the access list gains a role beyond member, when the lock's holder stops being the
host, when accounts stop being anonymous
Last updated: 2026-08-22
-->

A realm is owned by one player, but **reaching it does not require that player to be
online**. The realm document is stored against its owner's ID rather than on the owner's
disk, anyone on its access list can open it, and a single-writer lock decides who is
holding it at any moment. The session itself is unchanged: still peer-hosted over Relay,
still no game server.

## Story

As one of a group who plays at different hours, I want to open my friend's realm while
they are asleep and have the salvage I bank still be there when they wake up, so that a
realm is a shared place rather than an appointment.

## Why it matters

**This is the one thing the 2am-chest shape needs that does not exist** (`Brainstorms/
realms.md`). Everything else about a realm is already asynchronous. `M96` split the
Boarder from the realm and `M97` put the realm's record on the wall, but the realm is
still a file in one machine's `gkb_save/realms/`, so it is reachable exactly when that
machine is running the game.

**What moves is the document, not the simulation.** Zo, 2026-08-21, on the shorthand
"hosted on a server": *the realm is owned by a player but hosting must be independent of
players*. That is satisfied by making the realm reachable, not by standing up a game
server. The run is still hosted by whichever member opened it.

**The Boarder stays personal.** It travels with the player and has no reason to leave the
device. Only the realm has to be reachable, which is also why `M96` split them.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.**

## The model

Three parts, from the brainstorm:

1. **The realm stored against the owner's player ID**, not against a filename on one
   machine.
2. **An access list.** Held on **both sides and kept in step** (Zo, 2026-08-21): the realm
   names its members, and each Boarder names the realms it has been admitted to. The
   realm's list is authoritative for *admission*; the Boarder's list is an index for
   *the menu*. They are both written on a successful entry, and a Boarder entry that the
   realm no longer honours is shown as unavailable and pruned when entry is refused.
3. **A single-writer lock** — `lockedBy` plus a heartbeat. Opening the realm takes the
   lock, a stale lock can be broken, and **the holder is the host**. Two members cannot
   have the realm open at once, which is what stops a realm forking into two histories.

## Acceptance criteria

1. A player opens a realm whose owner is not running the game, spawns on the Ship, and
   can use every station alone.
2. Salvage banked by that player is present the next time the **owner** opens the realm,
   without the owner having been online in between.
3. A player not on the access list is refused, with a reason, and the realm's contents
   never reach their machine.
4. A second member opening a realm that is already open is refused **as locked**, naming
   the holder, and no second copy of the realm is created.
5. A lock whose heartbeat has stopped for longer than the stale window can be broken by
   the next opener, and the broken lock is logged with the holder it displaced.
6. Closing a realm releases the lock, and a realm closed by a process that died is
   openable again once its heartbeat goes stale — no manual intervention.
7. The two roster halves agree after entry: the realm lists the entering Boarder and the
   Boarder lists the realm, asserted on both documents.
8. A Boarder listing a realm it has been removed from shows that realm as unavailable in
   the menu, is refused on entry, and the row is gone after the refusal.
9. The Boarder document never leaves the device.
10. Losing the network mid-session does not lose the run: the realm writes back on the
    next successful contact, and a failed write is surfaced rather than swallowed.

## Implementation

1. **Add** `com.unity.services.cloudsave` to `Packages/manifest.json`. Authentication is
   already a dependency and `RelayJoin` already signs in anonymously, so there is a player
   ID to key on before any of this starts.
2. **Change** `RealmRegistry` so the realm half resolves against the remote store while
   the Boarder half keeps reading `gkb_save/boarders/`. This is the seam `M96` built the
   directory split for, and it is why the two halves are already separate documents.
3. **Create** the lock as fields on the realm document rather than a second document, so
   taking the lock and reading the realm are one round trip and cannot disagree.
4. **Keep** the local `gkb_save/realms/` path as the working copy while the lock is held.
   The lock is what makes a local working copy safe; without it this would be a cache with
   no invalidation rule.
5. **Do not** move the session off Relay, and do not add a dedicated host. Criterion 1 is
   about the document being reachable, and the brainstorm's cost check is only free
   because nothing is running server-side.

Rejected: a realm cached on every member's disk and merged later. Merging two divergent
realm histories has no rule that is not arbitrary, and the lock removes the need for one.

## Verification

- Criteria 3, 4, 5, 6, 7, 8: EditMode against a faked store, so the lock's stale window
  and the roster reconciliation are tested without a network.
- Criteria 1, 2, 9, 10: two machines, one signed out between the two halves of criterion
  2. Criterion 9 also reads back as a test that no Boarder field appears in any realm
  write.

**Playthrough path**: owner creates a realm and quits the game entirely. A second member
opens the same realm, banks salvage, and quits. The owner returns and reads the higher
number off the plate `M97` put on the wall.

## Usable, or it did not ship

**Driven**: opening a realm from the menu when its owner is not there. **Shown**: the
realm's own numbers on the hull, and a named refusal when it is locked. **Reads back**:
salvage banked by someone else, present on the owner's next visit.

## Supporting work

Depends on `M96` for a realm document that is already separate from the Boarder, and is
the missing half of `M98`'s first criterion — `M98` builds joining a party mid-run, this
builds reaching the realm at all. Neither needs the other to land first.

## Open questions

- **Anonymous IDs are per-device.** A realm owned by an anonymous ID is unreachable from
  a new machine, so "owned by a player" is really "owned by an install" until account
  linking exists. Flagged in the brainstorm as its own problem. Stamped: ship on
  anonymous, and treat linking as the follow-on.
- Whether a member can be removed by anyone but the owner is not specced. Stamped: owner
  only.
- How long the stale window should be is **unknown, needs play data**. Stamped: long
  enough that a loading screen never breaks a live lock.

## Do not build

A dedicated or headless host. Host migration. Merging divergent realms. Account linking.
Roles beyond owner and member. Moving the Boarder off the device. Any server-side
validation of purchases — that is called out separately in the brainstorm and is not this.
