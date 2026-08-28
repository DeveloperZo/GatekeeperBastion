# 0 — The invite

<!--
Purpose: Host opens a session, friend joins with a code, both shop the ship for a kit
out of the crew pot, NAV embark. No teaching. Hatches open by seat count and nobody
owns one.
Status: demo spec
Authority: implementable
Update trigger: when the multiplayer gate moves, when the crew pot's starting figure
moves, when the seat-count open rule changes, when the relay guest cap changes
Last updated: 2026-08-25 (relay cap already 2; gate signature re-read against the tree)
Depends: voice-coop.md, README.md
-->

## Why this beat exists

Every verb in this demo is already known. What is not known is the shape of a party:
who is here, what we can afford between us, and what happens when we commit.

The shop is the first thing the party does together and it is this beat's real
content. There is one pot and two people spending from it, so the first co-op decision
of the demo is who gets the better gun.

It is also where the gate that would make the demo unreachable gets opened.

## Functional storyboard

Copy stamped here until `DemoCoopLines` exists. Speaker is **Game**
([`voice-coop.md`](voice-coop.md)).

### 0 — Host opens the session

| | |
|---|---|
| **Goal** | Start a co-op run and get something to send a friend. |
| **Player** | MainMenu, Host. Relay allocates; the 6-char code shows. |
| **Game** | — |
| **Standing** | Send the code |
| **Prompt** | Live MainMenu host flow; code panel with copy. |
| **Log** | existing relay host line, plus `[Demo] co-op demo begins host=<code> seats=1/3` (build) |
| **Have** | `RelayJoin.TryHost`, code display, LAN fallback button on relay failure, code re-shows in the pause strip. |
| **Build** | **Gate fix (blocking).** `DemoProgress.MultiplayerUnlocked` now takes `(demoActive, wheelBuysLeft)` and passes any save with the demo off, so the refusal only bites while `DemoActive` is true with two buys left. It must key on the **solo** demo specifically, or the co-op entry has to clear `DemoActive` on its own save to get past it. Callers are `MainMenuController` and `RealmSelectController.Hosting`. **Cap fix: done** — `RelayJoin.MaxGuestConnections` is **2**. |

### 1 — Guest joins

| | |
|---|---|
| **Goal** | Be aboard, with no ceremony. |
| **Player** | MainMenu, Join, paste code. Spawns on the Ship deck. |
| **Game** | `demo.coop.aboard` · Subtitle · Local · Game. **You are aboard. Kit up at the ARMORY, then take a terminal with them.** |
| **Standing** | Buy a weapon |
| **Prompt** | — |
| **Log** | `[Demo] seat joined slot=<n> seats=<n>/3` (build) |
| **Have** | `NetSession`, `NetJoinIntent`, `NetPlayerAvatar` spawn, party lobby (`M72`). |
| **Build** | The line and the log. Nothing about the join surface changes. |

### 2 — Shop the ship

| | |
|---|---|
| **Goal** | Kit up, out of one pot, with someone else spending from it. |
| **Player** | Walk to the ARMORY. Buy a Primary. Both seats may use it. |
| **Game** | `demo.coop.crew_pot` · Lock (Ship) · Party · Game, on first ARMORY open. **One pot for the crew. What they spend, you do not have.** |
| **Standing** | Buy a weapon |
| **Prompt** | Live ARMORY string. Panel header shows the **crew pot** balance, not a personal one. |
| **Log** | `[Shop] slot=<n> bought <weapon> cost=<n> pot=<n>` (build) |
| **Have** | ARMORY panel, wheel, weapon catalog, `Meta.BankedSalvage`. Station buys are not net-routed at all today: each seat mutates its own local `GameSession.Meta` with no validation. |
| **Build** | **The crew pot.** One salvage balance the party shops from, host-owned, mutated only through a `GameController` command reached from a guest via `NetVerbs` (R1, R2, R3). **No purchase restrictions** (Zo, 2026-08-20): every catalog row is on sale from the first visit, and a player buys anything they can afford. The only refusal is insufficient balance, which is arithmetic and not a gate. `DemoProgress.PrimaryRowsOpen(m)` must be **inert under `DemoCoopActive`** (a flag that does not exist yet) — it hides Primary rows in `ArmoryStation` and refuses the buy in `ShipCommands.Weapons`, keyed on `DemoZone2BankLost`. The run starts with enough for two Primaries and change; tunable, and it lives with the other `#12` constants. **Kit is bought, not granted.** |

### 3 — Hatches open

| | |
|---|---|
| **Goal** | See how many doors this party has to hold. |
| **Player** | Nothing. The open ones are lit on the way past. |
| **Game** | `demo.coop.hatches` · Subtitle · Party · Game. **Two hatch doors open with two of you. Both are ways in.** (Copy reads the open count.) |
| **Standing** | Buy a weapon |
| **Prompt** | `HATCH <id> — open` or `HATCH <id> — sealed`. **No owner, no name, no verb** (Zo, 2026-08-20: responsible assignment does not help gameplay). |
| **Log** | `[Hatch] open count=<n> ids=<…>` (build) |
| **Have** | Nothing. Hatches are one `Subsystem` today with no identity. |
| **Build** | Open count from seat count (`M92`): one seat opens the middle, two open 1 and 3, three open all three. **Nobody is assigned a hatch.** They are doors the party holds between them, and who covers what is decided in the moment. |

### 4 — Ready and commit

| | |
|---|---|
| **Goal** | Leave together, deliberately. |
| **Player** | Guests READY at NAV. Lead commits. 5 s countdown. Any seat may cancel. |
| **Game** | — (the hatch line already carried it) |
| **Standing** | Embark |
| **Prompt** | Live NAV party flow. |
| **Log** | existing `[Run] run begins …` |
| **Have** | Party embark is built (`M72`): READY, lead commits, countdown, cancel by any seat, joins refused during the countdown. |
| **Build** | Nothing. This step is live. |

### 5 — Arrive

| | |
|---|---|
| **Goal** | See the doors before anything comes for them. |
| **Player** | Docked → Prep. The party spawns together, in sight of at least one open hatch. |
| **Game** | — |
| **Standing** | Find a field terminal |
| **Prompt** | — (mission drawer auto-opens on first Tethered Prep) |
| **Log** | existing `[Run] run begins at {BastionName} …` |
| **Have** | `StartRun` then `EngageTether`, drawer auto-open in `HudController`. |
| **Build** | Arrival placement is authored so an open hatch is visible from the spawn. Do not stack `MsgGeneral` with the arrival. |

## Acceptance shape

Player action: a second player joins with a code, both buy a weapon from one shared
balance, and they embark with two hatch doors open. Reads back: the ARMORY header
shows the pot falling as either seat spends, and the open-count log matches the seat
count.

## Gaps

- **The crew pot's starting figure is not set.** Absent, not invented. It wants to fund
  two Primaries and change; the number is a tune after a played run.
- **Station buys are unvalidated today.** The pot forces the first server-side check on
  a station buy in this project. That is new surface and it is this beat's.
- Whether a seat can see what the other is about to buy before they commit is not
  specced. Stamped: no, and it must not become a confirmation step. Nobody needs
  permission to spend.
