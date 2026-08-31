# 0 — The invite

<!--
Purpose: Host opens a realm to friends, guest joins with a code, both kit on the
shared Ship, hatch doors open by seat count, NAV embark. No extra teaching.
Status: working
Authority: implementable against 0.34.2
Update trigger: when the multiplayer gate moves, when OPEN TO FRIENDS changes,
when the seat-count open rule changes, when the relay guest cap changes
Last updated: 2026-08-30 (REALMS hosting, MK-1 unlock, realm pot, hatch assignment)
Depends: voice-coop.md, README.md, ../Play Mechanics/The-Crew.md
-->

## Why this beat exists

Every combat verb is already known from Demo - Single, or will be learned in the
Bastion the same way solo learns it. What is not known is the shape of a party:
who is here, which realm's pot they are spending, and what happens when they
commit.

The shop is the first thing the party does together. There is one realm pot and
two people who can spend it. Station buys are still local `ShipCommands` with no
server check; the guest HUD reads the host mirror (`RealmRecordSource`).

## Functional storyboard

Copy stamped here until `DemoCoopLines` exists. Speaker is **Game**
([`voice-coop.md`](voice-coop.md)).

### 0 — Host opens the session

| | |
|---|---|
| **Goal** | Start a friends session and get something to send. |
| **Player** | REALMS. Pick realm and Boarder. **OPEN TO FRIENDS**. Relay allocates; the 6-char code shows. Close and reopen the same realm: the code is reused. |
| **Game** | — |
| **Standing** | Send the code |
| **Prompt** | Live session panel; COPY. Boarder name is its own field, not the realm box. |
| **Log** | existing relay host line |
| **Have** | `RealmSelectController.Hosting.OnOpenToFriends` → `RelayJoin.TryHost`. LAN fallback. Code reuse on `OnSessionBack`. `RelayJoin.MaxGuestConnections` is **2**. Gate: `DemoProgress.MultiplayerUnlocked` — demo off, or `DemoWeaponGranted`. |
| **Build** | The co-op demo log line, if wanted. The gate and the cap are live. |

### 1 — Guest joins

| | |
|---|---|
| **Goal** | Be aboard, with no ceremony. |
| **Player** | Main Menu JOIN, paste code (or LAN). Spawns on **Ship - CoOp**. |
| **Game** | `demo.coop.aboard` · Subtitle · Local · Game. **You are aboard. Kit up at the ARMORY, then take a terminal with them.** |
| **Standing** | Buy a weapon |
| **Prompt** | — |
| **Log** | existing join / seat-spawn lines |
| **Have** | `MainMenuController` JOIN, `NetSession`, `NetPlayerAvatar` spawn, party lobby (`M72`). Guest loads its own campaign file; the realm it *stands in* is the host's (`GameSession` / `RealmRecordSource`). |
| **Build** | The line. Nothing about the join surface. |

### 2 — Shop the ship

| | |
|---|---|
| **Goal** | Kit up from the realm pot, with someone else able to spend it. |
| **Player** | Walk to the ARMORY. MK-1 / SEC-D cost **0**. Primaries stay hidden while `DemoActive` and `DeepestWaveEntered < 2`. |
| **Game** | `demo.coop.crew_pot` · Lock (Ship) · Party · Game, on first ARMORY open. **One pot for the crew. What they spend, you do not have.** |
| **Standing** | Buy a weapon |
| **Prompt** | Live ARMORY. Header reads `Meta.BankedSalvage` locally; a guest's *realm* readout is `RealmRecordSource.Banked`. |
| **Log** | existing shop lines |
| **Have** | ARMORY / wheel / catalog. `ShipCommands.BuyWeapon` mutates local Meta and `Save()`. Fresh realm pot is **0**. Fresh Boarder kit is empty (`BoarderState.CreateNew`) unless `SkipShipTutorial` granted the MK-1. |
| **Build** | Host-owned shop commands via `NetVerbs`, so a guest spend hits the realm pot rather than that seat's own save. `PrimaryRowsOpen` inert for a friends session (the `DemoCoopActive` flag was never added). Starting figure for two Primaries: not set, and MK-1 is free, so the first Boarding funds the rack. |

### 3 — Hatches open

| | |
|---|---|
| **Goal** | See how many hull doors this party has. |
| **Player** | Walk past them on the way to NAV. |
| **Game** | `demo.coop.hatches` · Subtitle · Party · Game. **Two hatch doors open with two of you. Both are ways in.** (Copy reads the open count.) |
| **Standing** | Buy a weapon |
| **Prompt** | `HATCH <id> — yours` / `HATCH <id> — SEAT n` / `HATCH <id> — sealed` (`ShipHatchDoor.cs:89-94`). |
| **Log** | assignment recompute |
| **Have** | `ShipHatches.OpenFor`: 1 → middle, 2 → indices 0 and 2 (ids 1 and 3), 3 → all. `PackFor` assigns owners. `ShipHatchAssignment.ServerRecompute` at embark / follow / break. |
| **Build** | Nothing. Open mask and assignment are live. |

### 4 — Ready and commit

| | |
|---|---|
| **Goal** | Leave together, deliberately. |
| **Player** | Guests READY at NAV. Lead commits. 5 s countdown. Any seat may cancel. |
| **Game** | — (the hatch line already carried it) |
| **Standing** | Embark |
| **Prompt** | Live NAV party flow. |
| **Log** | existing `[Run] run begins …` |
| **Have** | Party embark (`M72`): READY, lead commits, countdown, cancel, joins refused during countdown. |
| **Build** | Nothing. |

### 5 — Arrive

| | |
|---|---|
| **Goal** | See the Hatch before anything comes for it. |
| **Player** | Docked → Prep on **Bastion_Coop**. `T` at the Hatch is the start line. |
| **Game** | — |
| **Standing** | Start at the Hatch |
| **Prompt** | Hatch start-line prompt outranks Convert / Withdraw / Dock (`HatchVerbs.Choose`). Mission drawer auto-opens on first Tethered Prep. |
| **Log** | existing `[Run] run begins at {BastionName} …` |
| **Have** | `StartRun` then `EngageTether`. `HatchOffersWaveStart` while the current zone still has a terminal, or Master when `ControlRoomReachable`. |
| **Build** | Do not stack `MsgGeneral` with the arrival. |

## Acceptance shape

Player action: a second player joins with a code, both reach the ARMORY, and they
embark with two hull doors open. Reads back: the join and embark logs, and
`ShipHatches.OpenFor(2)` matching the open mask.

## Gaps

- Guest ARMORY spends are still that seat's local Meta. The realm pot the party
  *fights for* is the host's. Closing that split is this beat's remaining Build.
- Whether a seat can see what the other is about to buy before they commit is
  not specced. Stamped: no confirmation step. Nobody needs permission to spend.
- The 2026-08-20 "starts with enough for two Primaries" figure was never set.
  The pot starts at 0; MK-1 is free; Primaries wait on zone 1 being entered.
