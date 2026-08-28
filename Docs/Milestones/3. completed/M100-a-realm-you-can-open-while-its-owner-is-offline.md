# M100 — A realm you can open while its owner is offline

<!--
Status: completed, Aug 2026 (folder-moved 2026-08-25 at Zo's direction; 41 tests offline, never run on two machines)
Update trigger: when the realm document stops living in one machine's save directory,
when the access list gains a role beyond member, when the lock's holder stops being the
host, when accounts stop being anonymous
Last updated: 2026-08-24
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
3. A player not on the access list is refused, with a reason, before the realm becomes a
   session — nothing of it is adopted, listed or playable on their machine. *(Reworded
   2026-08-24: store-side secrecy is not available to an anonymous client — see the
   premise corrections — so this criterion gates what the client does, which is the part
   this milestone owns.)*
4. A second member opening a realm that is already open is refused **as locked**, naming
   the holder, and the refused opener writes nothing — the realm's history stays single.
   *(Reworded 2026-08-24 from "no second copy is created": per-member copies are how the
   store works now; what must stay single is the history.)*
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

## Premises corrected before building, 2026-08-24

Two of this doc's premises did not survive contact with the store, and one flow it needs
was never specced. Checked against the Cloud Save 3.x documentation (access classes,
SDK 3.1+) rather than remembered.

- **There is no single shared document.** The plan assumed whoever holds the lock writes
  *the* realm document under the owner's ID. Cloud Save's permission model forbids it: a
  client writes only its **own** player data, and the public access class is
  world-readable but **self-writable only**. A guest can never write the owner's copy —
  not the body, not the lock. Writing to another player's slot needs Cloud Code, which is
  server-side logic deployed through a dashboard: a second authority, a deploy step, and
  exactly the "something running server-side" this milestone's cost model refuses.
  **The model is now per-member copies**: every member writes the realm under the realm's
  key in their own public data; a reader fetches the copy of every member the realm
  names and takes the highest revision. This is NOT the rejected cache-and-merge — the
  lock still means exactly one writer at a time, so revisions are a chain, never a fork,
  and nothing is ever merged.
- **The lock rides every copy, and that pays for criterion 10.** Step 3's "lock as fields
  on the realm document, one round trip" survives per-copy: the holder's heartbeat
  rewrites its own copy — lock fields AND live body together — so a mid-session write-back
  falls out of the lock design for free. A lock is live if ANY member's copy carries a
  fresh claim; breaking a stale one is *proceeding past it* (nobody can clear a crashed
  holder's copy, and nobody needs to — staleness is the release). A simultaneous claim is
  resolved after the fact: claim, re-read every copy, and the later claim concedes
  (tie broken on player id). Clock skew cannot false-break a live lock because a
  stale-looking claim is confirmed by observation — re-read after a wait, and only an
  UNCHANGED heartbeat is stale; a fresh-looking claim is simply believed.
- **Admission had no flow.** Criterion 3 refuses non-members, but nothing said how anyone
  becomes a member. **The invitation is playing together**: OPEN TO FRIENDS over Relay
  shares the realm (first upload) and admits each guest that connects — the host learns
  the guest's player ID from the connection payload, the guest learns
  (realm id, owner id, name) from the session and remembers it on its Boarder. No new
  button, no code to type beyond the Relay code that already exists. LAN sessions do not
  share (nobody is signed in) and lose nothing they have today.
- **A shared realm is an online realm — stated cost.** Once shared, every open takes the
  lock, the owner's own solo ENTER included; a machine that cannot reach the store is
  refused with the reason rather than allowed to fork the history. An unshared realm is
  untouched by all of this and keeps working fully offline.

## Implementation

1. **Add** `com.unity.services.cloudsave` to `Packages/manifest.json`. Authentication is
   already a dependency and `RelayJoin` already signs in anonymously, so there is a player
   ID to key on before any of this starts.
2. **Create** `Sim/Meta/RealmShare.cs` — the envelope (owner, members, revision, lock
   fields) and every decision as pure rules: who may open, what is stale, who wins a
   contested claim, which copy is freshest, what each refusal says. No store, no clock,
   no Unity — ticks are handed in, so the whole of criteria 3–8's logic runs in the
   offline harness.
3. **Create** the store seam in `Game/Flow/` — `IRealmStore` (write own copy, read a
   named player's copy), `CloudRealmStore` against Cloud Save's public access class, and
   a fake in the tests. Envelope and body are two keys written in one batch.
4. **Create** `Game/Flow/RealmShareService.cs` — the orchestration: share-on-host, admit
   guest, open remote (fetch → freshest → admission → claim → confirm), heartbeat,
   release-on-close. The working copy lives in `gkb_save/remote/`, NOT `gkb_save/realms/`
   — a borrowed world must not list in the chooser as one of yours. A local sidecar under
   `gkb_save/share/` marks an owned realm as shared.
5. **Change** the chooser: the picked Boarder's remembered realms appear as rows beside
   the local ones (same `SaveRow` shape, marked in the name), ENTER routes remote rows
   through the service, DELETE on a remote row forgets it. The view is untouched — it
   already paints whatever rows it is given.
6. **Change** `NetSession` (connection payload carries the guest's player id) and
   `NetPartyLobby` (three replicated strings carry realm id / owner id / name to guests).
7. **Do not** move the session off Relay, and do not add a dedicated host. Criterion 1 is
   about the document being reachable, and the brainstorm's cost check is only free
   because nothing is running server-side.

Rejected: a realm cached on every member's disk and **merged** later. Merging two
divergent realm histories has no rule that is not arbitrary, and the lock removes the
need for one. Per-member copies are storage, not histories — the lock keeps the chain
single. Also rejected: Cloud Code as the write authority (server-side logic and a deploy
pipeline for a problem the lock already solves client-side).

## Verification

- The rules themselves — stale window, contest, freshest pick, admission, refusal
  wording: `Assets/Tests/EditMode/Meta/RealmShareTests.cs`, pure Sim, runs in the
  offline harness.
- Criteria 3, 4, 5, 6, 7, 8 as flows: `Assets/Tests/EditMode/Flow/RealmShareServiceTests.cs`
  against a faked store, so the open/refuse/break/release paths are tested without a
  network. (Unity-only — the service is Game-side, so the offline harness cannot see it.)
- The Boarder's remembered-realm index: `Assets/Tests/EditMode/Meta/BoarderKnownRealmsTests.cs`,
  pure Sim.
- Criteria 1, 2, 9, 10: two machines, one signed out between the two halves of criterion
  2. Criterion 9 also reads back as a test that no Boarder field appears in any realm
  write. Criterion 10's write-back is the heartbeat — the holder's copy carries the live
  body, so "next successful contact" is the next beat.

**Playthrough path**: owner creates a realm and quits the game entirely. A second member
opens the same realm, banks salvage, and quits. The owner returns and reads the higher
number off the plate `M97` put on the wall.

## Usable, or it did not ship

**Driven**: opening a realm from the menu when its owner is not there. **Shown**: the
realm's own numbers on the hull, and a named refusal when it is locked. **Reads back**:
salvage banked by someone else, present on the owner's next visit.

## State at build, 2026-08-24

Built to the corrected model, end to end:

- **Sim**: `RealmShare` (envelope, lock rules, contest, freshest-pick, refusal wording;
  constants 45 s beat / 180 s stale / 10 s observe) and the Boarder's known-realm rows
  (`RememberRealm` / `ForgetRealm`, three parallel JSON arrays, additive — an older
  document reads as none remembered, no version bump under the `WeaponWheelCentres`
  precedent). 32 tests in the offline harness.
- **Game**: `IRealmStore` (sign-in, write-own, read-of, delete-own — deliberately no
  write-to-another call), `CloudRealmStore` on the public access class,
  `RealmShareRemote` (fetch → decide → observe → claim → contest), `RealmShareService`
  (held state, share-on-host, admit, beat, release), `RealmShareBeacon` (45 s heartbeat
  carrying the live body; releases when the session leaves the realm; skips EditMode).
- **Wiring**: the join payload carries the guest's player id (`NetSession` approval hook;
  identity, never authorization); `NetPartyLobby` replicates realm id / owner / name so a
  guest's Boarder remembers the realm; the chooser lists remembered realms as
  "(shared)" rows beside local ones, routes ENTER through the store for them, forgets
  them on DELETE (armed, like everything destructive there), and takes the lock before
  entering an owned realm that has a sidecar. Hosting over Relay is what shares a realm
  and admits guests; LAN never shares. OPEN TO FRIENDS on a borrowed realm refuses for
  now — hosting a borrowed realm is real but its own change.
- **Saves**: a borrowed realm's working copy lives in `gkb_save/remote/`, its writes
  routed there by `GameSession.CurrentRealmIsRemote`; the share sidecar lives in
  `gkb_save/share/`; this machine's own realms directory is untouched.

**Owed**: criteria 1, 2, 9, 10 on two machines (the doc's verification section already
says so), and an eyes-on of the chooser rows. The store adapter compiles against Cloud
Save 3.2.0 but no call has crossed the wire to the real service yet — the first
two-machine session is also the first live test of the access-class read.

## Supporting work

Depends on `M96` for a realm document that is already separate from the Boarder, and is
the missing half of `M98`'s first criterion — `M98` builds joining a party mid-run, this
builds reaching the realm at all. Neither needs the other to land first.

## Open questions

- **Anonymous IDs are per-device.** A realm owned by an anonymous ID is unreachable from
  a new machine, so "owned by a player" is really "owned by an install" until account
  linking exists. Flagged in the brainstorm as its own problem. Stamped: ship on
  anonymous, and treat linking as the follow-on. Linking is also what store-side privacy
  waits on — a public copy is readable by anyone holding the owner's player id and the
  realm id (two unguessable strings, but unguessable is not access control).
- Whether a member can be removed by anyone but the owner is not specced. Stamped: owner
  only. (Removal is the owner editing members in their own copy under the lock; the
  removed member's stale copy loses every future freshest-pick and its next open is
  refused, which is criterion 8's path.)
- How long the stale window should be is **unknown, needs play data**. Stamped: long
  enough that a loading screen never breaks a live lock. Built as: heartbeat every 45 s,
  a claim is stale-looking after 180 s of silence, and stale-looking is confirmed by a
  10 s observation window before it may be broken — the constants live beside the rules
  in `Sim/Meta/RealmShare.cs`.

## Do not build

A dedicated or headless host. Host migration. Merging divergent realms. Account linking.
Roles beyond owner and member. Moving the Boarder off the device. Any server-side
validation of purchases — that is called out separately in the brainstorm and is not this.
