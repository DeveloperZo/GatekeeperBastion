<!--
Purpose: Current representation of the Ship as it is in the build — the hub between Boardings, what each console owns, and how a visit works.
Status: working
Update trigger: when a station is added, removed, changes what it sells, or moves along the hull.
Last updated: 2026-08-31
Where:
- Station open on Interact Down against the console under the ray — Assets/Scripts/Game/Ship/ShipController.cs (Update Interact Down, ResolveTargetedStation, TryOpenStationFromInteract, ApplyWalkMode); Assets/Scripts/Game/Ship/StationTerminal.cs (Interact, Prompt)
- ARMORY buy and equip — Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs (BuyWeapon, EquipWeapon, BuyWheelUpgrade)
- CREW QUARTERS claim — Assets/Scripts/Game/Ship/CrewQuartersStation.cs; Assets/Scripts/Game/Ship/ShipCommands.cs (BuyProtocol)
- FABRICATION rows and the opening gate — Assets/Scripts/Game/Ship/EngineeringStation.cs; Assets/Scripts/Game/Ship/DemoStationGate.cs
- NAV MAP embark, start-Wave selector, FOLLOW/RESPAWN wait — Assets/Scripts/Game/Ship/MapStation.cs
- Tiers and equipment wait on lifetime Salvage earned — Assets/Scripts/Sim/Meta/MetaUpgrades.cs (UpgradesUnlocked)
-->

# The Ship

Between Boardings the Boarder stands on their own Ship, first person, hands empty until they buy a weapon. This is the only place Salvage is spent and the only way back to the Bastion. Four consoles, four subjects: weapons at the ARMORY, the protocol at CREW QUARTERS, the Hatch and its kit at FABRICATION, where to go and EMBARK at the NAV MAP. Weapon Tiers and the equipment slot wait until the realm has earned any Salvage; FABRICATION itself stays shut until the opening ends.

## Game

Between Boardings I stand on my own Ship, first person, hands empty until I buy something. This is the only place Salvage is spent and the only way back to the Bastion.

She is long and narrow and she is one deck, stern to bow. I come to forward, on the port side, with the NAV MAP a few paces ahead of me on the bow bulkhead and the hatch doors spaced down the starboard hull — one opening per seat, which is how I know how many of us there are before anyone speaks. CREW QUARTERS is across the spine from where I wake. Everything else that costs money is at the far end of the ship.

So the walk is the shape of a visit. The map is at my elbow and the guns are the whole hull away: the ARMORY sits aft to starboard and FABRICATION aft to port, and getting to either means walking the length of her, past all three hatches, the beam opening out amidships and closing again toward the stern. No loading, no prompt, nothing to hold — just distance, and enough of it that I have settled what I want before I arrive. Then the same walk forward again to leave, which is the part that feels like going. The party lead owns EMBARK, and every seat shops its own loadout.

Four consoles, four subjects, and nothing is sold in two places. Weapons and their wheels at the ARMORY. The protocol I wear at CREW QUARTERS. The Hatch, the Rig, the equipment slot and the Decoy at FABRICATION. Where I go, and the button that commits me, at the NAV MAP.

The order of a first visit: buy the free Secondary, because the first lesson is which console guns come from. Claim a protocol, which also costs nothing at its plainest. Look at the other two, so I know where the bills will arrive.

Weapon Tiers and the equipment slot stay shut until this realm has earned any Salvage at all. Nothing else waits. FABRICATION itself stays shut until the Bastion is stripped.

The NAV MAP will warn me if I own no weapon and no protocol, and then let me go anyway. It is willing to watch me make a bad bet. Everything on this Ship is.

## Details

### Structure

Four consoles, each owning one subject. Nothing is sold in two places.

- **ARMORY** — the weapon in the Boarder's hands: weapons, and the upgrade wheel for a weapon already owned. The only place that decides which owned weapon fills Primary and which fills Secondary.
- **CREW QUARTERS** — the protocol worn at embark.
- **FABRICATION** — Hatch Armor, the equipment slot, the Repair Rig, and the Decoy.
- **NAV MAP** — where to go, and going there.
- **PRACTICE RANGE** — a door beside the ARMORY that sells nothing: pick a protocol and two weapons and try them on the range ([The Range](The-Range.md)). An owned weapon goes at its own wheel points, an unowned one at base.

One deck, long and narrow, stern to bow. Spawn forward and to port; the NAV MAP ahead on the bow bulkhead, same side. Three hatch doors spaced down the starboard hull. CREW QUARTERS to starboard, level with spawn. ARMORY and FABRICATION at the stern — ARMORY starboard, FABRICATION port — so a weapon or a Hatch buy is a walk the length of the ship and back. That traverse is geometry: no loading screen, no trigger to step into.

### Stations

**ARMORY.** Buying a weapon equips it into its own slot at once. Upgrades are per weapon, so Salvage poured into one gun stays in that gun — a different Primary starts its wheel at nothing. In the Bastion the weapon keys only swap the slots already carried, never between things owned.

**CREW QUARTERS.** Claim Default at no cost, or claim VEIL or FORGE. AEGIS and LEDGER are not sold. Repairing the Hatch is not here.

**FABRICATION.** What it buys applies from the next embark, not retroactively. The console stays shut until the opening ends.

**NAV MAP.** One destination. A selector for which Wave to start on, capped by the higher of deepest Wave entered and deepest Wave cleared. EMBARK is the point of no return: once the Bastion loads, the only exits are clearing it and losing. On a friends session only the party lead's EMBARK commits the party. While the party fights without you — arrived late, or dead — the NAV MAP is the wait surface: FOLLOW at the next break, or a priced RESPAWN ([Going Down](Going-Down.md)).

The start-Wave selector does not skip. Starting deeper delivers every Wave up to that point back to back, with no Prep in between, and then the Boarding continues to the end. The overlap is the price.

The NAV MAP warns if you own no weapon or no protocol, then lets you go. No weapon is the improvised cutter. No protocol means `G` and `H` refuse.

### Rules

Walk. Look at a console until the prompt appears, interact, and a panel opens: movement locks, the cursor comes free, and the panel owns the screen until you close it. One panel at a time. Close it and you are walking again. Binding: [Field Manual](Field-Manual.md).

Every panel shows banked Salvage at the top. A row you cannot afford names how much more you need.

Each console carries its name on a label in the air beside it that turns to face you. That is the whole of the wayfinding — the corners of the screen carry profile, record and loadout, and nothing that points anywhere.

Weapon Tiers and the equipment slot stay locked until this realm has earned any Salvage. A protocol claim never waits. Hatch Armor waits until the opening ends, because FABRICATION is shut until then.

First visit: the free Secondary at the ARMORY, then a protocol at CREW QUARTERS, then look at the other two. Save a Primary until you know which fight you want — it is fixed for the whole Boarding, and its wheel does not follow you to the next gun.

## Additional Details

### Classification

Core, loop. Removing the hub removes the only place Salvage is spent and the only way back to the Bastion.

### Interacts with

- [Upgrades](Upgrades.md) — Hatch Armor, the equipment slot, the Repair Rig and the Decoy are FABRICATION rows; that page owns those purchases.
- [Protocols](Protocols.md) — claiming is a Ship action; that page owns the sheets.
- [Weapons](Weapons.md) — the ARMORY sells the guns and their wheels; that page owns the guns.
- [Realms](Realms.md) — every spend here debits the realm's pot; that page owns the split and the pot.
- [A Boarding](A-Boarding.md) — EMBARK is the point of no return onto that page's loop; that page owns the Boarding.
- [The Hatch](The-Hatch.md) — Hatch Armor bought here sizes the door; that page owns the door.

### Gaps

- Nothing anywhere lays out which purchases wait on the first Salvage earned and which wait on the opening ending. There is no one place that shows you the split, and the ARMORY's LOCKED row reads as though dying were what opens it.
- FABRICATION sells the Decoy into the Drone bay. The slot still looks like a third gun until you own one.
- The Hatch Armor track has a ceiling and you only learn where by seeing a row read MAX. Nothing shows you the whole ladder in advance.
- Repairing needs the Repair Rig equipped, and nothing on the Hatch or in the fight says so until you try to repair and nothing happens.
- An older weapon exists in the code and can still sit in an old save. It is not sold, cannot be equipped, and will not reach your hands.
- If a developer command has been used in a play session, nothing saves for the rest of that session. The hub gives you no visible sign — everything looks bought, and none of it is there next time.
