<!--
Purpose: Coming Soon marker for the AEGIS Protocol, an instance of Protocols — differences only.
Status: teaser; the sheet is designed and not yet claimable.
in_build: true
coming_soon: true
Update trigger: replace this page when AEGIS ships.
Last updated: 2026-08-29
-->

# AEGIS — Coming Soon

Instance of [Protocols](Protocols.md).

Meant to be the holding-ground sheet, whose answer to what the room is looking at is ground the Boarder refuses to give, but it is not built and cannot be claimed.

## Game

Coming Soon.

There is a fourth answer to what the room should look at, and it is the ground itself. A sheet about holding: where the garrison slows, which swing never lands, who stands in the doorway and means it.

Not on the panel yet. When it is claimable, this page will earn its ink.

## Differs

AEGIS is the holding-ground identity — the sheet whose answer to "what is this room looking at" is the ground the Boarder refuses to give, with behaviour undecided where the brainstorm says so. Designed, not built — the design is recorded in `../Brainstorms/boarder-protocols.md`. What exists in the build is a display row that binds no claim control, so it is not claimable by any path, and no behaviour of any kind carries the name.

## Where

- Not in the catalog this build sells — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`BoarderProtocolId` ends at `Forge`; the `Shop` array deals Default, Veil, Forge and no more, so `IsCatalogued`, `DoctrineOf`, `CrewAbilityOf`, `PersonalAbilityOf` and `PassiveAt` have no arm for it, and `ValidateEquip` would sanitise an id that is not on that list back to `None`)
- The only AEGIS in the build is a display string: a Coming Soon row padding the CREW QUARTERS catalog, whose sheet is the stub and whose claim callback is null — `Assets/Scripts/Game/Ship/CrewQuartersStation.cs` (`StubAegis`, `ComingSoonNames`, `BuildCatalogMode`, the stub branch of `BuildSheetMode`)
