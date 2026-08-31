# Realms

<!--
Purpose: The realm model — what belongs to a player and what belongs to a world — and
**the safehouse it exists to serve**. Two aims, not one: the persistence split, and a Ship
that is worth walking into because a crew's time accumulates in it. Drop-in play sits on
both. Sourced from No Rest for the Wicked (Zo, 2026-08-20) and deliberately simplified.
Status: brainstorm
Update trigger: when an item moves across the character/realm line, when a remaining
idea graduates, or when the farm-valve watch turns into a decision.
Last updated: 2026-08-30 (OPEN TO FRIENDS reuses the join code; respawn spends the realm bank, not Unbanked)
-->

## What this covers

**Two central aims.** The persistence split, and the safehouse that split is for — a realm
model is only worth building if it makes a Ship somebody wants to come back to. Decisions
here are judged against both, and where they conflict the safehouse is the reason the
realm exists.

**Boundary with `the-safehouse.md`** (one canonical list per thing): this file owns
*ownership, identity and where state lives*. That file owns *what there is to do in the
Ship*. Activities never propose their own storage; anything they bank is realm state.

Persistence split between the player and the world: what a Boarder carries between
sessions, what a Ship and its bastion progress hold instead, and what that split makes
possible — a safehouse worth walking into and a party you can join without everyone
being online at the same minute. It does not cover the co-op demo, which **defers
persistence entirely** (`../Demo - Coop/README.md`) and therefore inherits nothing from
this file until it ships.

## Where it stands

**Was "nothing here is built"; corrected 2026-08-22, restamped 2026-08-25.** `M96` shipped the
split and `M98` built the mid-run join, so most of the list below described the world before
them. Each entry now says what is actually true. The reachable realm itself was built
2026-08-24 (`M100` — per-member copies in the store, the lock riding each copy; two-machine
criteria owed), so what remains unproven is a real second machine. Milestones are cited here by
bare id: the folder is the status, so a path written into this file dies at the next status
change, and `../Roadmap.md` is the addressable index.

- ~~**One save, one balance.**~~ **Built (`M96`).** There are two documents:
  `BoarderState` travels with the player, `RealmState` stays with the world, and `MetaState`
  is now a facade forwarding to both. The whole salvage ledger, the run record, doors and
  terminals, hatch armour and the demo flags are the realm's.
- **Station buys are still not net-routed**, with one exception. Every Ship station
  mutates that seat's local `GameSession.Meta` with no server check. `ShipCommands.BuyUpgrade`
  is the exception and it is a client-side REFUSAL, not a route: a visitor is told the hatch is
  not theirs to upgrade. Any shared balance still forces the first server-validated buy in the
  project. **Trust is accepted, not solved**, and `M100` gave the gap a named edge rather than
  closing it: a guest can no longer write hatch armour to your realm, but every other purchase
  silently writes to the guest's OWN realm while the Ship HUD and the wall plate beside them
  show the host's numbers. Two salvage figures on one screen, and a buy that lands somewhere
  the player is not looking. Stated cost of a friends game.
- **The backing store is free at this scale, checked 2026-08-21.** Cloud Save's free tier is
  5 GiB/month storage, 1,000,000 writes and 1,000,000 reads; Authentication is free outright, so
  account linking is a code task rather than a bill. Against a realm that is one save file's
  worth of JSON and a group writing tens of times a session, neither axis binds. Verify before
  relying on it: `unity.com/products/gaming-services/pricing`.
- **Joins are refused during the embark countdown** (`M72`), and a guest still cannot dock
  the Run (`DockRun` has no net route). ~~There is no mid-run join surface of any kind.~~
  **`M98` built one**: a late arrival lands on the Ship, waits, and follows the party in at a
  Prep boundary — `RunSeats`, `AwaitingFollow` / `RequestFollow`, and the FOLLOW labels. `M98`
  was recut 2026-08-24 so the same seam owns death: one priced respawn rule, then
  2026-08-25 kills-bank-as-they-land moved the fee onto `SpendBanked` (realm bank,
  `"not enough banked"`, cap 2, 10 s bleed-out). Eyes-on criteria on the join half are still owed.
- **OPEN TO FRIENDS keeps its join code** (2026-08-30): closing the panel and reopening the
  same realm reuses the Relay allocation. Boarders have their own name field on REALMS.
- ~~**The Ship has no display objects.**~~ **`M97` built one**: the `Realm Record` root on
  `Ship - CoOp` carries twelve marks, a seven-mount weapon rack and a record plate. `M92`
  excluded trophies; the record is not one — it displays the realm, not the player. (This entry
  cited `Ship.unity`, which does not exist. There is one hub, `Ship - CoOp`; `Ship - Demo`
  retired with the scene-pair consolidation and `SceneFlow` no longer names it.)
- **The crew pot** (`../Demo - Coop/0-the-invite.md`) is one salvage balance the party
  shops from, with persistence deferred. It is realm-side salvage with the character
  half switched off, which is why it does not have to be thrown away later.
- **Identity is anonymous and per-install.** `com.unity.services.authentication` and
  `com.unity.services.relay` are in `Packages/manifest.json`; Relay runs on anonymous auth.
  An anonymous ID persists only while its session token is cached, so a reinstall is a new
  player and a realm keyed to it is orphaned. **"Owned by an account" needs account
  linking** — same service, free at any tier, not turned on. The cost of fixing this is
  code, not money.
- **Standing save rule** (`../Decisions.md`): bump the schema, rename the key, never
  write a migration shim. Splitting one save into two documents is a restructure under
  that rule, not an exception to it.

## Graduation test

The character side and the realm side must be listed item by item, and the mid-run join
rule ruled, before any milestone can be written from this file.

**Satisfied 2026-08-20 (Zo)** for the entries that have since graduated. He approved the
initial split with the note that **play testing will dictate the specifics**, and ruled
the join rule as **between waves, never during waves**, reasoning from how quickly the
game can scale for an arriving player. Graduated to `M96` (the split and the save
restructure), `M97` (the Ship shows the realm's record) and `M98` (drop-in and the
between-waves join). Those milestones are now the record; the entries are gone from this
file rather than duplicated.

## Ideas

### More than one realm

The source model lets a player hold several realms, each with independent progress, and
carry one character across all of them. Correctness for two realms is a criterion in
`M96`. Creation and deletion of both realms and Boarders shipped on 2026-08-22 with an
armed two-press confirm and a soft delete that retires the document rather than unlinking it.
**Renaming still does not exist** and does not need to until someone has more realms than fit on a
screen.

### What a realm is worth naming after

Realms are named at creation (`M96`) and the name is realm-side state. Whether anything
else in the fiction hangs off that name, or it stays a label on a save, is untouched.
*Unknown, needs play data.*

## Source model, checked 2026-08-24

The header says "sourced from No Rest for the Wicked and deliberately simplified". These
are the facts of the source as of its **Together update (2026-01-22)**, checked against
the wiki and coverage (norestforthewicked.wiki.fextralife.com, Together Coop Update /
Multiplayer pages) rather than remembered, so the simplifications stay deliberate:

- **Their realm already outlives its host.** A shared realm is persistent and free,
  holds the owner plus up to 3 invitees, and is **joinable while the owner is offline** —
  the exact shape `M100` graduates. They go two steps further: ownership itself can be
  shared, and an owner can permanently remove a member. Our single-writer lock is the
  deliberate simplification — their realm is a document anyone entitled may open; ours is
  opened by one writer at a time, who hosts.
- **Their split is `M96`'s split.** World state, vendor upgrades and boss kills stay with
  the realm ("if your buddy upgrades a vendor while you are offline, that vendor is
  upgraded for you too"); the character — level, gear, whole inventory — travels between
  realms. Party cap 4, difficulty scales with party size. No correction to our line.
- **Their safehouse accrues by spending into the world.** Sacrament's building projects
  (smithy, enchanter, inn — material costs, up to three levels each) are realm state that
  changes what the town sells; players also buy personal houses with per-house and
  per-chest access permissions. The realm-side-investment half is our hatch armour today
  and `the-safehouse.md`'s list tomorrow; the personal-space half is theirs, not ours —
  a second player-owned space is already rejected below.
- **Their rewards are personal even when the job is the realm's.** Bounties are shared
  realm-wide but pay only whoever turns them in — the opposite of our "the pot is the
  world's" ruling (`../Decisions.md`). Recorded so the difference stays a choice rather
  than an accident.

## Watch

**The fresh-realm farm valve.** In No Rest for the Wicked, spinning a new realm makes
early content available again, which is a real strategy for clearing or skipping
content and, in Zo's play, not an enjoyable one (2026-08-20). Checked 2026-08-24: there
it is embraced, not incidental — "realm farming" is named community practice, resetting
spawns and loot while the character keeps everything — so Zo's dislike is a divergence
from the source, and if play shows the valve used here the answer is our own decision,
not a copy of theirs. If GKB's salvage pot is
realm-side, the same valve exists here. Accepted as a thing to watch rather than a
blocker; it becomes a decision only if a played session shows people using it.

## Constraints

Any milestone from this file respects `R1`-`R8` (`reference: research findings`), in
particular that sim mutations go through `RunSim` command methods called from
`GameController` commands, and that a realm balance is host-owned rather than a client
free-for-all. It also respects the standing save rule above. **A realm is opened by one writer at a
time**: any milestone that puts a realm anywhere reachable by two people ships the lock in
the same milestone, never as a follow-up. **No shared player
wallet**: a realm pot is the world's salvage, not a pooled personal balance, and the
rejected shared-party-wallet idea stays rejected.

## Rejected

A second player-owned space separate from the Ship (struck by Zo, 2026-08-20: "no more
host bastion just a bigger ship that doubles as safe house"). Host migration. Realm
difficulty selection at creation, which is their model and not a question this project
has yet.
