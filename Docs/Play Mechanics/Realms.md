<!--
Purpose: Current representation of realms as they are in the build — the realm/Boarder pairing at the chooser and at CONTINUE, the persistence split, and the owner of the one-pot-per-realm coupling.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Realm-side fields, the pot and its ledger — `Assets/Scripts/Sim/Meta/RealmState.cs` (`BankedSalvage`, `EarnSalvage`, `SpendSalvage`, `HatchArmorLevel`, `CompletedTerminalIds`, `OpenDoorIds`, `DeepestWaveReached`/`Entered`, demo first-time gates)
- Boarder-side fields, the traveling half — `Assets/Scripts/Sim/Meta/BoarderState.cs` (weapons, wheel points, `EquippedProtocol`, `EquippedPassive`, equipment, `EquippedDrone`)
- The pair composed as one session state, every earn and spend forwarded to the realm — `Assets/Scripts/Sim/Meta/MetaState.cs` (`Boarder`, `Realm`, `EarnSalvage`, `SpendSalvage`)
- ENTER loads both documents and adopts the pair — `Assets/Scripts/Game/Flow/GameSession.cs` (`EnterRealm`, `CurrentRealmIsRemote`); the chooser's refusals and the co-op Ship landing — `Assets/Scripts/Game/Flow/RealmSelectController.cs` (`OnEnter`, `WhyNotEnter`, `OnCreate`)
- CONTINUE resolves the pair without the chooser: last-entered if loadable, else the first loadable row of each list, each half falling back independently; a null half names every refused document and starts a new campaign instead — `Assets/Scripts/Game/Flow/MainMenuController.cs` (`OnContinue`, `SetContinueVisible(GameSession.HasSave)`); `Assets/Scripts/Game/Flow/GameSession.cs` (`ContinueGame` → `EnterRealm`, the `StartNew` fallback); `Assets/Scripts/Game/Flow/RealmRegistry.cs` (`ResolveContinuePair`, `FirstUsable`, `LastRealmId`/`LastBoarderId`, `ReportUnloadable`)
- Every kill banks into the realm being played, unsplit, as it lands — `Assets/Scripts/Sim/Run/RunSim.cs` (`AwardKill`, `BankKill` → `_meta.EarnSalvage`); a Convert credits the same pot per seat — `Assets/Scripts/Sim/Run/RunSim.Wallets.cs` (`CreditSlot` → `_meta.EarnSalvage`)
- The Ante and the respawn fee charge the same bank — `Assets/Scripts/Sim/Run/RunSim.cs` (`SpendBanked`)
- The realm's ledger floor counts only what the realm itself sold, never the walked-in kit — `Assets/Scripts/Sim/Meta/RealmState.cs` (`ReconstructSpentFromLevels` and its remarks)
- A fresh realm refuses OPEN TO FRIENDS until armed — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`MultiplayerUnlocked`), asked before the code is handed out at `Assets/Scripts/Game/Flow/RealmSelectController.Hosting.cs`
- What a realm shows a guest whose machine has no realm document — `Assets/Scripts/Sim/Meta/RealmRecord.cs` (`From`, `Pack`)
- NEW REALM / NEW BOARDER mint, write and select a named document; a blank name falls back — `Assets/Scripts/Game/Flow/RealmSelectController.cs` (`OnCreate`, `OnNewBoarder`); `Assets/Scripts/Game/Flow/RealmRegistry.cs` (`CreateRealm`, `CreateBoarder`, `CleanName`)
- DELETE is a two-press arm against one named row, expiring on its own and disarmed by any other press — `Assets/Scripts/Game/Flow/ArmedAction.cs` (`Fire`, `IsArmed`, `Arm`, `Disarm`, `WindowSeconds`, `TickExpiry`); wired at `Assets/Scripts/Game/Flow/RealmSelectController.cs` (`OnDeleteRealm`, `OnDeleteBoarder`)
- Deleting retires the document; a deleted Boarder takes its kit and leaves realms untouched; the last Boarder is replaced — `Assets/Scripts/Game/Flow/RealmRegistry.cs` (`DeleteRealm`, `DeleteBoarder`); `Assets/Scripts/Game/Flow/SaveService.cs` (`RetireRealm`, `RetireBoarder`, `Retire`)
- A borrowed row is forgotten rather than deleted, two-press, and rejoining remembers it — `Assets/Scripts/Game/Flow/RealmSelectShared.cs` (`ArmedForget`, `Forget`); `Assets/Scripts/Sim/Meta/BoarderState.cs` (`ForgetRealm`)
- The realm-side track is refused to a visitor, in the command and on the row — `Assets/Scripts/Game/Ship/ShipCommands.cs` (`BuyUpgrade` owner check); `Assets/Scripts/Game/Ship/RealmRecordSource.cs` (`OwnsThisRealm`); `Assets/Scripts/Game/Ship/StationTerminal.cs` (`UpgradeRow` — "not your realm to upgrade")
-->

# Realms

A realm is a world and a Boarder is a person, saved apart and paired at the chooser before play. What stays with the realm is its banked Salvage, its Hatch Armor and the ground already opened; what travels with the Boarder is weapons, protocol and equipment, so a well-armed Boarder entering a fresh realm arrives broke. There is one pot per realm: every kill anyone makes banks into the world being played, and every bill for repairs, antes and standing a dead seat back up comes out of that same pot. Deleting a realm destroys the world and leaves the Boarders who walked it untouched.

## Game

A realm is the world. I am not.

The chooser comes first: REALMS, from the main menu. Worlds on one list, Boarders on the other. NEW REALM starts a world. NEW BOARDER starts a self. ENTER commits the pair onto that world's Ship. One Boarder can walk many worlds, in order, and nothing gets confused.

What stays when I leave is the world's: the banked pot, Hatch Armor, the doors already open, the deepest Wave already cleared. What travels is mine: weapons, wheel, protocol, equipment. I arrive in a fresh realm well-armed and broke.

The money is the world's and the gear is mine — wherever I bought it. Kill in a friend's realm and the pay lands in their pot as the body falls. Buy in a friend's realm and it leaves in my hands, billed to their world. Both directions are one rule read twice.

A realm opened to friends is played together in that world. Everything the crew kills for, whoever killed it, lands in the one pot — and every bill comes back out of that same pot: the repairs, the antes, and standing a dead seat up. Nobody has a private purse to be broke in. The crew is solvent together or not at all.

Deleting a realm deletes a world. The Boarders that walked it lose nothing of themselves.

## Details

### Structure

A realm is a world and a Boarder is a person, saved apart. The save is split down the middle and the pair is the whole of it.

- **Realm-side** (stays with the world): banked Salvage and the lifetime ledger, the Boarding counters, Hatch Armor, hatch conversions, completed terminals and open doors, the deepest Waves cleared and entered, and the first-time gates.
- **Boarder-side** (travels): weapons owned and each weapon's wheel, the equipped protocol and passive, equipment, and the drone bay's payload.

### Chooser

REALMS lists the realm documents and the Boarder documents on this machine. Each column has its own name field — NEW REALM and NEW BOARDER create from the typed name beside them (empty falls back to REALM / BOARDER). ENTER loads the two picked documents and makes them the session, landing the pair on that realm's Ship — the co-op hull, so the same realm looks the same however it is entered. The chooser refuses out loud: no realm, no Boarder picked, or a document written by another build each name themselves. A machine with no Boarder at all is given one rather than a dead end. An owned realm that has been shared opens under the shared-realm lock, and a borrowed row opens from the store ([Shared Realms — Coming Soon](Shared-Realms.md)).

CONTINUE is the same pairing without the picking. The main menu's CONTINUE — shown only when there is a save — resolves the pair itself and lands on the Ship without the chooser: each half is the one last entered if that document is still loadable, otherwise the first loadable row of its own list, and because the two halves fall back independently a CONTINUE can hand back the last realm beside a different Boarder when only one half is refusable. If either half comes back with nothing, the build names every document it refused and starts a new campaign rather than continuing — the refused documents are left on disk untouched.

NEW REALM and NEW BOARDER mint a document from the name typed into its own field, write it to disk, and select it. A blank or whitespace name falls back to a default rather than being refused, and a document that could not be written says so instead of pretending it exists.

DELETE beside either list is a two-press arm: the first press arms against that one named row for a few seconds and prints what the second press will do, and anything else disarms it — picking a different row, pressing the other DELETE, or simply waiting. A realm's document is retired, throwing away every Boarding anyone banked into it, and cannot be undone from inside the game. A Boarder takes its kit with it and leaves every realm untouched; deleting the last Boarder mints a replacement rather than leaving a chooser nobody can walk out of. A borrowed realm row is not a document but a memory held on the Boarder, so its DELETE is a two-press FORGET — the world lives on with its owner, and joining a session there remembers it again.

### Pot

One pot per realm. Every seat in a session banks into the realm being played, on every kill, as the body falls, and every spend debits the same pot: weapon and wheel purchases, equipment, repairs, the Ante, and standing a dead seat back up. The only other pile that exists anywhere in the game is a stake deliberately withdrawn from this pot at the Hatch. The spenders own their own spends and link here: [The Ante](Ante.md), [The Hatch](The-Hatch.md) (withdraw and repair), [Going Down](Going-Down.md) (the respawn fee); that kills pay on landing is [A Boarding](A-Boarding.md)'s.

A guest killing in a friend's realm is enriching that world; a guest buying there walks out with the gear while the bill stays — with one exception, and it is the one the split predicts. A realm-side purchase is refused to a visitor. Hatch Armor belongs to the world rather than to the Boarder, so only the realm's owner may buy it; a guest is refused by name and the row will not take the press. What travels may be bought in anybody's realm; what stays may only be bought by the realm's own ([Upgrades](Upgrades.md) owns that track).

A well-equipped Boarder entering a fresh realm is well-armed and broke, and the realm's ledger counts only its own spending — the kit walked in from somewhere else. A fresh realm starts at the beginning (nothing banked, first-time gates closed) and refuses OPEN TO FRIENDS until its Boarder is armed with the ARMORY's MK-1 or the opening stretch has ended ([The Crew](The-Crew.md)).

### Rules

ENTER commits a pair. CONTINUE resolves the pair without the picking, each half falling back independently.

Deleting a realm deletes the world's half only. Deleting a Boarder takes the kit and leaves every realm untouched.

A visitor may not buy Hatch Armor.

## Additional Details

### Consequences

One pot per realm — this page owns that coupling. Every seat in a session banks into the realm being played, on every kill, as the body falls, and every spend debits the same pot: weapon and wheel purchases, equipment, repairs, the Ante, and standing a dead seat back up. The only other pile that exists anywhere in the game is a stake deliberately withdrawn from this pot at the Hatch. The spenders own their own spends and link here: [The Ante](Ante.md), [The Hatch](The-Hatch.md) (withdraw and repair), [Going Down](Going-Down.md) (the respawn fee); that kills pay on landing is [A Boarding](A-Boarding.md)'s.

The split is what the player accounts for. A guest killing in a friend's realm is enriching that world; a guest buying there walks out with the gear while the bill stays — with one exception, and it is the one the split predicts. A realm-side purchase is refused to a visitor. Hatch Armor belongs to the world rather than to the Boarder, so only the realm's owner may buy it; a guest is refused by name and the row will not take the press. What travels may be bought in anybody's realm; what stays may only be bought by the realm's own ([Upgrades](Upgrades.md) owns that track).

A well-equipped Boarder entering a fresh realm is well-armed and broke, and the realm's ledger counts only its own spending — the kit walked in from somewhere else. A fresh realm starts at the beginning (nothing banked, first-time gates closed) and refuses OPEN TO FRIENDS until its Boarder is armed with the ARMORY's MK-1 or the opening stretch has ended ([The Crew](The-Crew.md)). Deleting a realm deletes the world's half only.

Removal test: fold the halves back into one save and money, worlds and identity stop being separable — no playing a friend's world, no carrying a kit between worlds, no world that outlives a Boarder. Different game.

### Classification

Core, arc. Serves Fellowship and Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp). Fellowship at rank 1: a friend's world is a place to play. Expression at rank 2: the kit is the half that travels.

### Interacts with

- [The Crew](The-Crew.md) — a realm opened to friends is the session's world, and every seat plays into this realm's pot; that page owns the session.
- [A Boarding](A-Boarding.md) — kills bank into this realm's pot the moment the body falls; that page owns kills-pay-on-landing.
- [The Ante](Ante.md) — anting up bills the pot; that page owns the ante spend.
- [The Hatch](The-Hatch.md) — withdraw takes a stake out of the pot and repair bills it; that page owns the withdraw and repair spends.
- [Going Down](Going-Down.md) — the respawn fee bills the pot; that page owns the respawn spend.
- [Upgrades](Upgrades.md) — purchases debit the pot even when the thing bought is Boarder-side and leaves with the buyer, and the one realm-side track is the one thing a guest may not buy; that page owns those purchases and the Hatch Armor track.
- [Shared Realms — Coming Soon](Shared-Realms.md) — the same split, reached through the store when the world is not on this machine; that page owns the shared-realm lock and membership.

### Gaps

- Nothing on any panel says which side of the split a purchase lands on. The pot being the world's while the gear is the Boarder's is learnable only by leaving.
- The chooser refuses OPEN TO FRIENDS on a fresh realm with a named reason, but nothing earlier warns that a new world starts locked to friends.
- Deleting is guarded by the button's own confirm and nothing else; there is no archive.
