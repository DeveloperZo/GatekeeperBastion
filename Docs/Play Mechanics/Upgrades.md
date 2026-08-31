<!--
Purpose: Current representation of the permanent meta purchases as they are in the build — the Hatch Armor track, the equipment slot and Repair Rig one-offs, and the earned-Salvage lock.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-30
-->

# Upgrades

Between Boardings a Boarder spends the realm's banked Salvage at FABRICATION on things
that are never lost: Hatch Armor levels, an equipment slot, and the Repair Rig that goes
into it. Hatch Armor raises how much punishment the Hatch can absorb, from the next
embark, and the Repair Rig is what makes repairing the Hatch possible at all. Nothing
bought here can be sold back or refunded, and the equipment slot stays locked until this
realm has earned its first Salvage. On a fresh realm the console itself is shut for the
whole opening, so none of it is buyable until the opening ends.

## Game

Permanent. Nothing bought here is lost when a Boarding ends badly — gear and protocol stay on my Boarder, the Hatch's improvements stay with the realm. Once a level is bought, it stays bought.

CREW QUARTERS sells identity, not numbers. There are no attribute tracks; health, sprint, pay per kill and crit chance do not move from that console, and I have stopped looking for them.

FABRICATION changes the door. Hatch Armor raises what it absorbs, from the next embark — buying it changes nothing I can see standing here, which is either faith or engineering, and on this Ship those are the same purchase. The Repair Rig is not a track: it is a one-off, and without it equipped I cannot repair at all, at any bank balance. It does not make repair cheaper. It makes repair exist.

Weapon Tiers and the equipment slot wait for the first Salvage this realm has earned. A protocol claim never waits. Hatch Armor waits on the opening ending, because FABRICATION is shut until then.

Hatch Armor is the buy when I lose Boardings with my health bar still full. That signal is clean: the Boarding ended at the door, and no upgrade to me fixes a door.

The Rig treats a symptom. If I am repairing often, the actual fault is Thieves reaching the door, and the fix for that is sold at the ARMORY.

## The interaction

Permanent purchases a Boarder makes on their own Ship, between Boardings, out of the
realm's banked Salvage — nothing here is lost when a Boarding ends badly, and nothing can
be sold back. What CREW QUARTERS sells is a protocol claim ([Protocols](Protocols.md));
weapons and their upgrade-wheel purchases are the ARMORY's ([Weapons](Weapons.md)); this page
owns the rest, all of it at FABRICATION:

**Hatch Armor**, the one remaining track: each level raises the Hatch's maximum HP by
the same step and costs double the level before. It is the realm's, not the Boarder's —
a guest standing on someone else's shared Ship is refused — and it applies from the next
embark, because a Boarding's Hatch pool is built once, at start. Levels, steps and
prices: [Field Manual](Field-Manual.md).

**The equipment slot**, bought once, locked (below).

**The Repair Rig**, a one-off piece of Equipment into that slot: buying it equips it.
The Rig is behavioural, not a discount — with no Rig equipped the repair rate pushed
into the Boarding is exactly 0 HP/s and the repair command refuses, so the Rig is what
makes repairing the Hatch exist at all. It repairs at one flat rate that nothing raises,
and it changes nothing about what repairing costs in Salvage — that spend is
[The Hatch](The-Hatch.md)'s.

**The opening closes the console outright.** For the whole of the opening FABRICATION is
a *locked preview* — visible and named, selling the full version's breadth, and it opens
no panel when interacted with. So Hatch Armor, the equipment slot, the Repair Rig and the bay's
payloads are all unreachable until the opening ends and the console is powered, whatever
the bank holds and whatever the lock below says ([The Opening](The-Opening.md)).

**The lock**: upgrade-wheel purchases and the equipment slot are locked until this realm
has earned any Salvage at all — a lifetime figure, deliberately, so spending the bank to
zero cannot re-lock them. Everything else is ungated by *this* lock:

- The protocol claim and the weapons themselves are buyable from the first minute of a
  fresh campaign.
- Hatch Armor and the Decoy carry no earned-Salvage lock either; what holds them is the
  shut console above.

Driven by the FABRICATION panel's rows (the upgrade-wheel purchases this lock also covers are
bought at the ARMORY and owned by [Weapons](Weapons.md)). Shown by each row's level,
price and locked state. Reads back as `bought Hatch Armor LN for … track=fabrication`,
`equipment slot unlocked — purchased for …`, `bought Repair Rig … — equipped` and the
named refusals (`locked until this realm has earned salvage`, `this realm is not yours
to upgrade`).

## Consequences

These purchases are the arc the Boardings feed: what is bought stays bought, so beyond
the guns the bank's destinations are the Hatch and the bay. Hatch Armor moves how long
the Hatch stands — the buy for Boardings lost with the Boarder's health bar still full.
The Repair Rig forecloses nothing and opens a whole verb: without it a damaged Hatch can
only be defended harder, never healed. The lock orders a fresh campaign — the first
Salvage earned is what turns the upgrade wheel and the slot from locked rows into decisions.

Removal test: strip these and every Boarding starts from the same Ship; progress through
failure loses its purchasable surface, so the game the player accounts for is a
different one.

## Classification

Primary, arc. Serves Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank
stamp), with Challenge as the vehicle. Expression at rank 2: permanent, chosen, kept —
shop money is what the Boardings killed for.

## Interacts with

- [Weapons](Weapons.md) — the same earned-Salvage lock covers its upgrade wheel and this
  page's equipment slot; that page owns the guns and the upgrade-wheel purchase.
- [The Hatch](The-Hatch.md) — Hatch Armor sizes the pool the Hatch defends and the Rig is
  what makes repairing it exist; that page owns the withdraw and repair spends.
- [Realms](Realms.md) — Hatch Armor and the lock's lifetime-earned counter live on the
  realm, gear and protocol travel with the Boarder, and only the realm's owner may buy its
  track; that page owns the realm and Boarder split and the one pot per realm.
- [Salvage](Salvage.md) — every purchase here spends the realm's banked pot, these buys
  being this page's own spends; that page owns what a kill is worth.
- [The Drone](The-Drone.md) — FABRICATION's other shelf, the Decoy being a payload bought
  there and ungated by the earned-Salvage lock; that page owns the bay and its payload buy.
- [The Opening](The-Opening.md) — FABRICATION is a locked preview for the whole opening, so
  nothing this page sells is buyable on a fresh realm until the opening ends; that page owns
  the gate ladder.
- [Protocols](Protocols.md) — CREW QUARTERS sells identity, not numbers: there are no
  attribute tracks, and the Boarder's constants are frozen in code; that page owns the claim
  and the sheets.

## Where

- Hatch Armor: doubling price, fixed step per level, level cap — `Assets/Scripts/Sim/Meta/MetaUpgrades.cs` (`NextCost`, `HatchHp`, `MaxLevel`)
- The one track and its console — `Assets/Scripts/Sim/Meta/UpgradeTrack.cs` (`UpgradeTrack.HatchArmor`, `UpgradeTracks.Station`)
- Purchase path; a guest is refused on a realm not theirs — `Assets/Scripts/Game/Ship/ShipCommands.cs` (`BuyUpgrade`, `RealmRecordSource.OwnsThisRealm`)
- Applies from the next embark: the Boarding's Hatch pool is built once, at start — `Assets/Scripts/Sim/Run/RunSim.cs` (the constructor, `MetaUpgrades.HatchHp`)
- Equipment slot: one-off, locked until the first earn, priced — `Assets/Scripts/Game/Ship/ShipCommands.cs` (`BuyEquipmentSlot`); `Assets/Scripts/Sim/Meta/EquipmentCatalog.cs` (`SlotPrice`)
- Repair Rig: one-off, requires the slot, buying equips, flat rate — `Assets/Scripts/Game/Ship/ShipCommands.cs` (`BuyEquipment`); `Assets/Scripts/Sim/Meta/EquipmentCatalog.cs` (`Def`, `EquipmentId.RepairRig`)
- Repair is 0 HP/s without the Rig, and the repair command refuses riglessly — `Assets/Scripts/Game/Bastion/GameController.cs` (`ApplyMetaEffects` repair push; `RepairHatch`)
- The lock: lifetime Salvage earned — not a Convert, not a death — `Assets/Scripts/Sim/Meta/MetaUpgrades.cs` (`UpgradesUnlocked`)
- FABRICATION is a locked preview for the whole opening, with no state that can open it early: the verdict is unconditional on the opening being live — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`FabricationVerdict` returns `LockedPreview` for any live opening, `Open` only once it is over); the interact consumed and the prompt substituted, never a panel — `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`TryHandleInteractDown` and `PromptOverride`, the `EngineeringStation` branches); what reopens the console is the opening ending — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`EndDemo` clears `DemoActive`; the `DemoFabPowered` it also writes has no reader)
- The console's rows once it opens — `Assets/Scripts/Game/Ship/EngineeringStation.cs` (`BuildPanel` — Hatch Armor, equipment slot, Repair Rig, Decoy); the Repair Rig row is hidden until the slot is bought
- No attribute tracks: HP and stamina are frozen constants, and crit chance is frozen at 0 in two places at once — `Assets/Scripts/Sim/Meta/MetaUpgrades.cs` (`PlayerMaxHp`, `StaminaMax`, and `CritChance()`, which returns 0 and has no caller); the figure the gun actually rolls against `Assets/Scripts/Game/Player/PlayerGun.cs` (`CritChance` property, hardcoded 0)

## Gaps

- Nothing in the game lays out the gated/ungated split in one place. You infer it from a locked row at the ARMORY and a reassuring line on the other two panels.
- The lock opens on the first Salvage earned, not on a Convert and not on a Lost Boarding. Nothing on the Ship says that.
- Nothing reduces the Salvage cost of repairing. The Repair Rig is what lets you repair at all, at a fixed rate, and no panel says that owning it changes nothing about the price.
- Tiers being per weapon and non-transferable is stated nowhere before you buy.
- There is no respec and no refund. Nothing warns you.
- Hatch Armor takes effect on the next embark, and there is no way to confirm it landed short of starting a Boarding and reading the door.
- The Hatch Armor panel shows current and next effect, but never the whole ladder.
- Protocol bursts exist; CREW names Default, VEIL and FORGE. Fight HUD: protocol name
  top-left; Passive / Personal / Crew bar bottom-centre with `G`/`H` fills.

*Costs, levels, effects and the exact curve: [Field Manual](Field-Manual.md).*
