<!--
Purpose: Coming Soon marker for the LEDGER Protocol, an instance of Protocols — differences only.
Status: teaser; the sheet is designed and not yet claimable.
in_build: true
coming_soon: true
Update trigger: replace this page when LEDGER ships.
Last updated: 2026-08-29
-->

# LEDGER — Coming Soon

Instance of [Protocols](Protocols.md).

Meant to be the sheet that turns salvage into position — moving money around the room and offering bets on it, never tolling money already earned — but it is not built and cannot be claimed.

## Game

Coming Soon.

A sheet about where the money goes. Salvage as position: payouts that walk toward me, piles that sit where something died for them, a way home that costs everything I am carrying and leaves it lying in the field. Claimable, in both senses.

Not on the panel yet. One rule will hold whatever else changes: it can move money around the room, and it can offer me a bet, but it can never be the reason a Boarding paid nothing.

## Differs

LEDGER is the salvage-economy identity — the sheet that turns Salvage into position: it may move money around the room and offer the Boarder a bet, and it may never put a toll in front of money already earned. Designed, not built — the design is recorded in `../Brainstorms/boarder-protocols.md`. What exists in the build is a display row that binds no claim control, so it is not claimable by any path, and no behaviour of any kind carries the name.

## Where

- Not in the catalog this build sells — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`BoarderProtocolId` ends at `Forge`; the `Shop` array deals Default, Veil, Forge and no more, so `IsCatalogued`, `DoctrineOf`, `CrewAbilityOf`, `PersonalAbilityOf` and `PassiveAt` have no arm for it, and `ValidateEquip` would sanitise an id that is not on that list back to `None`)
- The only LEDGER in the build is a display string: a Coming Soon row padding the CREW QUARTERS catalog, whose sheet is the stub and whose claim callback is null — `Assets/Scripts/Game/Ship/CrewQuartersStation.cs` (`StubLedger`, `ComingSoonNames`, `BuildCatalogMode`, the stub branch of `BuildSheetMode`)
