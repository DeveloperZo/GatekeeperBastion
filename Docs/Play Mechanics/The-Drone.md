<!--
Purpose: Current representation of the Drone bay as it is in the build — the third slot as a delivery mechanism, ghost-and-confirm placement, doctrine at place, and the payload roster.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- The bay is the third weapon slot; fire on it is placement, not a shot — `Assets/Scripts/Sim/Meta/WeaponSlot.cs` (`Drone`); `Assets/Scripts/Game/Player/PlayerGun.Slots.cs` (`ConfigureDrone`, `TickDroneFire`)
- Payloads sold at FABRICATION; buying equips; mid-Boarding equip refused, purchase still owned — `Assets/Scripts/Game/Ship/ShipCommands.cs` (`BuyPayload`, `EquipPayload`)
- One payload per Boarding: bay loadout locked and charges reset at Boarding start — `Assets/Scripts/Game/Bastion/GameController.cs` (`ApplyMetaEffects`); `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`ResetForRun`)
- Ghost-and-confirm placement on walkable ground within place range; alt-fire or slot switch cancels without a charge — `Assets/Scripts/Game/Bastion/DecoyPlacement.cs` (`OnFirePressed`, `UpdateGhost`, `Cancel`)
- Host-only confirm; a guest confirm is a request — `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`TryPlace`); `Assets/Scripts/Game/Bastion/DecoyPlacement.cs` (`ClientOnly`, `RequestPlaceDecoy`)
- Named refusals before the ghost (no charges, one already live) and on an invalid confirm — `Assets/Scripts/Game/Bastion/DecoyPlacement.cs` (`OnFirePressed`)
- Charges, one live machine at a time, a charge spent on place — `Assets/Scripts/Sim/Run/DecoyState.cs` (`TryPlace`, `ChargesRemaining`)
- The placed machine takes Gatekeeper Hatch-damage and dies on it, freeing the bay — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Attack`, `_decoyTarget.ApplyDamage(hatchDamage)`); `Assets/Scripts/Game/Bastion/DecoyObject.cs` (`ApplyDamage`, `DespawnInternal`); `Assets/Scripts/Sim/Run/DecoyState.cs` (`TakeDamage`); `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`NotifyDestroyed`)
- Player fire cannot touch it: no `Damageable`, and the prop is put on Ignore Raycast with a trigger collider — `Assets/Scripts/Game/Bastion/DecoyObject.cs` (`SpawnPrimitive`)
- Doctrine written onto the machine at place, from the worn protocol — `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`PlaceMode`); `Assets/Scripts/Sim/Run/DecoyState.cs` (`TryPlace` writes `Mode`); `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`DoctrineOf`, `DoctrineMode`)
- PHANTOM's hand-over, and the re-derive that limits it — `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`ApplyPhantom`); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`TransferInterestTo`, which ends by calling `Retarget()` — that method clears `_decoyTarget` and re-tests the ordinary pull, so a chaser outside the pull radius keeps nothing from the transfer)
- PARALLEL BAYS written, not read — the bay holds one — `Assets/Scripts/Sim/Run/DecoyState.cs` (`TryPlace`, the `MaxLive` check); `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`MaxLiveUnder`, uncalled from placement)
- Pull refusals: Thief never, committed Defaults never, radius-bound — `Assets/Scripts/Sim/Run/DecoyState.cs` (`ShouldPull`)
- Decoy catalog row (`cost`, `charges`, `HP`, `radii`) — `Assets/Scripts/Sim/Meta/PayloadCatalog.cs` (`Def`, `PayloadId.Decoy`)
-->

# The Drone

The third slot on the Boarder's weapon strip is not a gun but a bay, and it carries one payload, bought at FABRICATION and fixed for the whole Boarding. Firing on the bay places a machine on the ground rather than shooting: the first press raises a ghost, the second confirms and spends one of the Boarding's finite charges. One machine stands at a time, so each placement is one of the Boarding's remaining rescues. The Decoy that rides the bay today does not fight — it pulls Gatekeeper attention onto ground the Boarder chose, and it is the only payload that has been built.

## Game

The third slot on my weapon strip is not a gun. It is a bay, and FABRICATION decides what rides in it.

Today the bay holds the Decoy: a machine I place with a ghost and a confirm, built to be interesting to the garrison. It comes up already claiming attention — whose, and how much, depends on the doctrine I embarked wearing, which is the one quiet place a protocol touches hardware.

One payload rides per Boarding, chosen on the Ship like everything else. The bay's key does nothing while the bay is empty, and an empty bay looks exactly like a key that failed. It did not fail. It is waiting on FABRICATION.

A placed machine is a position I bought: somewhere the room wants to be that is not where I am, on ground I chose. The Decoy does not fight. It is bait with a work ethic.

The bench this bay was built for is longer than one machine. The rest of it: [Drone Payloads](Drone-Payloads.md).

## Details

### Structure

The third slot on the weapon strip is the bay. It holds a payload, not a gun. Payloads are sold at FABRICATION; buying one equips it. Exactly one payload rides per Boarding — the bay locks at Boarding start. A mid-Boarding equip is refused; a mid-Boarding purchase still lands as owned, with the equip deferred. The bay shares the guns' slot keys; an empty bay's key does nothing.

Charges are finite per Boarding. One machine stands at a time. A charge is spent on the confirm.

Placement is ghost-then-confirm. The first press raises a ghost that tracks the aim onto walkable ground within the payload's place range, coloured valid or invalid. The second press confirms. Alt-fire or switching slots cancels without spending a charge. The confirm is host-authoritative — a guest's confirm travels as a request.

A placed machine is not repaired. Death, or the Boarding resetting the bay, is what clears the live slot. Doctrine is written at placement; repairing the Hatch does not stamp or restamp it.

### Payload

The Decoy is the only payload in the build. It does not fight. It pulls Gatekeeper attention onto ground the Boarder chose, inside its radius, under the kind refusals that always stand.

It carries the payload's authored HP. It has no damage receiver of its own and sits where player fire cannot find it, so no Boarder can shoot one down. A Gatekeeper pulled to it swings at it with the figure it uses on the Hatch — a structure taking a structural blow — and the hit that empties the pool destroys the machine, frees the bay for the next charge, and logs the gatekeeper-seconds it absorbed.

The payload contract — finite supply, may hit very hard, rescues a moment, cannot carry a fight — governs this row and every row that joins it. Charges, HP, radii: [Field Manual](Field-Manual.md).

### Doctrine

Written at placement from the placer's equipped protocol, not at repair. STOCK places it as sold. PHANTOM hands over the attention the placer was carrying — with the caveat on [VEIL Protocol](Protocol-VEIL.md), which is where that hand-over is settled. PARALLEL BAYS is written but nothing reads it: the bay still holds the payload's own one live machine.

### Rules

Placement is refused by name in two places: before any ghost appears, when no charge is left or a machine is already live; on the confirm, while the ghost reads invalid.

Player fire cannot destroy it. Kind refusals outrank the pull: a Thief is never pulled, and a Default already committed to the Hatch is not pulled off it.

One payload per Boarding. Finite charges. One live.

Driven by the bay's slot key, Fire to aim and confirm, alt-fire to cancel. Shown by the slot strip's bay row, the ghost and its valid/invalid colour, and the HUD's decoy line. Reads back as the loaded bay, the ghost, the place, and a named refusal.

Bindings: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

A placed machine is a bought position: somewhere the room wants to be that is not where the Boarder stands, on ground the Boarder chose. The Decoy pulls Gatekeeper attention inside its radius, under the kind refusals that always stand — a Thief is never pulled, and a Default already committed to the Hatch is not pulled off it.

Charges are finite per Boarding and one machine stands at a time, so each placement spends one of the Boarding's remaining rescues; the payload contract — finite supply, may hit very hard, rescues a moment, cannot carry a fight — governs every payload on the bench. A machine that is doing its job is also being hit, so the pull and the pool run out together: the bait dies of its own success and the charge is gone with it.

The doctrine write is the one place a protocol touches hardware, but today it buys almost nothing — PARALLEL BAYS has no reader at all, and PHANTOM's hand-over survives only for chasers already inside the pull radius.

Removal test: without the bay there is no way to hold ground the Boarder is not standing on, the doctrines lose their object, and embark loses its third loadout decision — decisions change, so it is a mechanic.

### Classification

Secondary, loop. Serves Expression and Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Expression is the embarked payload and doctrine as loadout statements; Fantasy is machines set against a garrison. Secondary because it rescues a moment, occasionally and in combination, and cannot carry a fight.

### Interacts with

- [Weapons](Weapons.md) — the bay shares the weapon strip's slot keys and HUD row but holds a payload, not a gun; that page owns the slot rules.
- [Protocols](Protocols.md) — the worn protocol's doctrine is written onto the machine at place; that page owns the sheets and their doctrines.
- [Gatekeepers](Gatekeepers.md) — the pull obeys the kinds' refusals: a Thief never pulls, a committed Default stays committed; that page owns targeting.
- [The Hatch](The-Hatch.md) — the Decoy's paying job is pulling bodies off the Hatch the Boarder must hold; that page owns the withdraw and repair spends.
- [Upgrades](Upgrades.md) — FABRICATION's permanent purchases stand beside the payload shelf, the payload buy itself being this page's own spend; that page owns those permanent purchases and the earned-Salvage lock.

### Instances

One payload exists: the Decoy, and it is documented on this page. The catalog holds that one row, and FABRICATION binds that one row — so the bay is a single-shape mechanic today and this page carries no roster.

The bench of intended payloads is teased at [Drone Payloads — Coming Soon](Drone-Payloads.md) and argued in the fabrication brainstorm; none of them is built, and none becomes an instance here until it is. Instances, when they come, will vary on what the placed machine does to the room and on when it acts, and none may vary the payload contract or the one-payload-per-Boarding rule.

### Gaps

- Gatekeepers that path to a decoy do not currently attack it — the decoy is in practice indestructible bait, which is a recorded defect with its own task, not a design.
- The empty bay renders like a weapon slot, so selecting it before buying a payload reads as an input that failed to register.
- PARALLEL BAYS is written onto placed decoys and nothing reads it; the bay still holds one.
- Nothing at FABRICATION states the one-payload-per-Boarding rule before embark.
