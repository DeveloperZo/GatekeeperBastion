<!--
Purpose: Current representation of the Hatch bank family — Withdraw, the Convert window, the Docked return and portal, breach loss, and repair — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# The Hatch

The Hatch is the door the Boarder came in through, and the one thing in the Bastion being
defended: if its health reaches zero the Boarding ends on the spot, and no amount of Salvage
buys it back. It is also the loop's cashier. Standing at the door, the Boarder can draw
banked Salvage into a carried pile, run a Convert that multiplies that pile while
Gatekeepers march on the door, repair the door at a price per point of health, or dock home
when the way out is open. Today the door's one prompt always leads with the Wave start, so
Convert and Withdraw are never offered as designed, and no seat in a solo Boarding can open
a Convert at all.

## Game

One door with a pulse, and everything on this hull is arranged around it.

The Hatch is the umbilical back to my Ship. It has its own health and it is the only thing here being defended. It cannot be moved, closed, or traded away. Zero ends the Boarding on the spot — the one loss nothing at all buys back. A dead Boarder can be priced back onto the floor out of what they were carrying. A dead door cannot, at any price, out of any purse.

It starts every Boarding whole. Whatever I let happen to it last time did not carry.

On a field-terminal Wave nothing can touch it. A Convert is the window where it can be hurt, and the same window where Unbanked converts — the payout and the damage come through the same slot. Defaults and Thieves bill the door. Hunters never do, at any range, standing on it.

Repair is me at the door, holding — with the Rig equipped, or the hold does nothing at any bank balance. It bills banked Salvage per point healed and will take the bank to exactly zero if I let it.

Survive a Convert and the door offers something better: hold, and dock home with what I banked. The offer lives until the next Wave starts, and never appears after a failed bank.

The arithmetic worth keeping: a Thief that reaches this door costs more than killing it. Not in danger. In money. That is the whole reason to walk out and meet one.

## The interaction

The Boarder invokes it, at one door, against the realm's bank and the seat's carried pile. The Hatch bank is **one mechanic, several verbs**, and their precedence is a stated rule, not an accident of prompt order: a dock hold in progress, then destroyed, then repair, then the wave start line, then Convert, then Withdraw, then the dock (`HatchVerbs.Choose` — the start line always outranks the bank verbs; [A Boarding](A-Boarding.md) owns the start line itself). **This page owns the withdraw and repair spends.**

**One consequence of that precedence dominates the family today.** `HatchOffersWaveStart` is true for the whole of a Boarding, not only while a zone is unfinished: its second clause is `MasterControlAwaitsStart`, and the Bastion's control room carries a Master terminal that is never entered into the unlock graph (`BastionLayout.RebuildGraph` loads Field terminals only), so it never reads as done — `BastionUnlockGraph.TryCompleteTerminal` refuses an id it was never given, with "unknown terminal".

The start line is therefore always on offer at an undamaged Hatch, and `Choose` consequently never returns Convert or Withdraw — neither prompt is reachable, **on any seat** — while `TickPrepCommands` routes `T` at the Hatch into the wave start before it ever reaches the Convert branch.

**Withdraw — bank into carry.** In Prep, at a full, undamaged Hatch, with nothing carried, hold Interact: banked Salvage moves into the operating seat's Unbanked pile at a fixed rate (tied to the Rig's repair rate; no Rig needed to withdraw). Kills bank as they land ([A Boarding](A-Boarding.md)), so Withdraw is the **only** thing that fills the carry. Withdrawn units leave the Run's banked tally too, so a later Convert cannot count them twice. The withdraw lands in whichever counted seat holds the verb, and refuses an uncounted slot — a guest's hold is a real withdraw, opened as a repair channel (`NetVerbs.SetRepairing`) that the host ticks against its own range check and credits to the requesting seat's slot. **The hold works and the prompt does not name it**: `RepairInteractable.Interact` runs the dock, then repair, then the withdraw on its own conditions and never consults the start line, while `Prompt` goes through `Choose` and so leads with the start line instead.

**Convert — the opt-in bet, and the one seat that can still take it.** The rule as the code holds it: in Prep, at a full Hatch, carrying Unbanked, `E` at the Hatch selects (locking the Ante dial) and `T` starts a Convert Wave. There is no per-Boarding cap. It is refused:

- while the current zone still has a terminal to finish;
- while the Master Control sequence is live;
- once every Wave is cleared;
- with nothing carried.

**The Boarder's own `T` never reaches it; a guest's does.** `TickPrepCommands` tests the Hatch start line above the Convert branch, and that test is always true (above), so a solo or host seat starts a zone — or, once every field terminal is done, the finale — where it selected a Convert.

The client path has no such branch at all: `TickClientPrepCommands` goes straight from `RepairInteractable.OffersHatchBank` to `NetPrep.RequestStartHatchBank` → `GameController.TryNetStartHatchBank`, which checks range to the Hatch, arms the Hatch host-side and calls `StartHatchBankWave` itself. **Nothing in that path refuses a client.** `StartHatchBankWave`'s `_net.IsClientOnly` guard tests the machine executing the call, and the call executes on the host, where it is false; it would only fire on a client-only machine calling the method locally, which no code path does. So "the Convert is refused on a client seat" is exactly backwards: the guest is the only seat that can open one, and the window it opens commits the whole party. The codebase says the same thing in its own words — "Convert was struck from the demo on 2026-08-25" (`RunSim.Dock.cs`), which is why the dock had to be re-hung on the zone shutoff.

What the guest is *told* is a third thing again: its Hatch prompt comes from the same `HatchVerbs.Choose` call, which leads with the start line, so the seat holding the only live Convert reads "[T] start the wave" and never sees the stake line — Ante, carry, multiplier, bodies — that prices the bet its key actually places.

When a window does run it runs a fixed span (duration, drip rate: [Field Manual](Field-Manual.md)): the carry drips into the bank multiplied by a carry factor snapshotted from how many field Waves have been **drawn** since the last bank — `WavesSinceBank` is incremented in `LaunchWave`, not on a clear — and whatever is left dumps when the timer ends. Killing the Wave early or the carry hitting zero does **not** end the window — a Convert has no off switch, at the door or at any terminal.

The Wave it sends matches the carry's Wave table, with an extra Thief at one authored carry ([Waves](Waves.md)); its Thieves target the door again, because `MayTargetPlayer` goes false for the kind on this mode ([Gatekeepers](Gatekeepers.md) owns targeting). Surviving the window opens any door the zone has earned and offers the dock; the Hatch breaching ends the Boarding before any of that fires.

**The door is in the fight on both Wave modes** (Zo, 2026-08-27 — this falsified the old rule that field Waves leave it invulnerable). On a Convert it takes full damage; on a field Wave the damage is scaled well down (`HatchBank.FieldWaveDamageScale`) — the Hatch is shut and armoured while the Boarder is up the lane, but a body that reaches it is never inert. Every hit on the Hatch also bleeds the carried pile: `DamageHatch` scales the damage first and then both the door and the attrition read the same figure, so a field Wave cannot drain the stake at a rate the Hatch itself is armoured against. Which kinds bill the door and which refuse it: [Gatekeepers](Gatekeepers.md).

**Docked return.** The dock offer opens on a survived Convert **and on a zone's terminal shutoff** (2026-08-28), and lasts until the next Wave starts; it is refused for the whole of Master Control. Hold Interact at the Hatch — or at the **portal**, a pad beside the Hatch that lights when the way home opens and runs the same hold against the same sim rule — and the Boarding ends Returned (screen: DOCKED, a good ending; the loss counter does not move).

**The dock is a host-seat verb only**: `DockOffered` returns false outright on a client, so a guest's hold at the Hatch or the portal falls through to repair or withdraw and never ends the Boarding — which seat may take the party home is stated in the code as unruled, not as guest-refused-on-purpose. Each seat keeps a small authored share of its leftover carry (`HatchBank.ReturnedKeepShare`) — a rebate, not a conversion; a Lost end wipes the carry entirely, the bank surviving both.

**Breach.** Hatch HP at zero ends the Boarding Lost on the spot, whatever else is happening; there is no repair from zero and no purse that buys a door back. It starts every Boarding at full HP, sized by the realm's Hatch Armor track ([Upgrades](Upgrades.md)).

**Repair.** Hold Interact at a damaged Hatch. The gate is the Rig: repair does nothing unless the Repair Rig is equipped — `GameController.ApplyMetaEffects` pushes the equipped state and rate onto the door every Boarding (explicitly zero when rigless), and the prompt names the missing step (slot locked / slot unbought / Rig unowned / Rig unequipped — `MetaState.RepairMissingStep`; the Rig and the slot are FABRICATION purchases, [Upgrades](Upgrades.md)).

With the Rig, healing applies continuously while held and **charges banked Salvage per HP** (`ShipSystems.RepairSalvagePerHp`; rate and price: [Field Manual](Field-Manual.md)). Fractional HP accrues between frames so a short tap can buy nothing; a hold the bank cannot cover spends the entire bank, heals the affordable fraction, and stops at exactly zero.

Repair is legal during a live Wave, not only in Prep — but while a dock offer is live, the dock takes the hold ahead of repair, and the two surfaces disagree about it: `Interact` tests the dock offer before the repair branch, while `Choose` puts Repair above Dock, so a damaged Hatch inside a dock window prompts for repair and docks the Boarding on the hold instead.

Driven by held Interact for Withdraw / dock / repair, and Prep `E` then `T` for the Convert (bindings: [Field Manual](Field-Manual.md)). Which seat holds which verb: repair and withdraw are every seat's, the dock is the host seat's alone, and the Convert — today — is only a guest's — the roster and admission side of that is [The Crew](The-Crew.md)'s. Shown by the Hatch's one prompt line (verb-ordered — in practice the start line, the repair line, or, on a host seat, the dock line, never the Convert or Withdraw rows), the portal's lighting, and the HUD's Hatch bar. Reads back as the running withdraw/repair session totals in the prompt, the carry falling and the bank rising through a Convert, the door-open and dock-offer log lines, and the end screen's repair-spent line.

## Consequences

The Hatch is the loop's cashier and its one irreplaceable object. Withdraw turns safe progress into a stake that a Convert can multiply, that Hatch hits bleed, and that a death or breach wipes — the bank behind it survives everything, so risk is exactly what was chosen.

Solo, the multiply half is out of reach entirely — no seat in a solo Boarding can open the window — so a withdrawn pile has exactly one bank point: the zone's terminal, at face value ([A Boarding](A-Boarding.md) owns the shutoff). That makes Withdraw solo a pure "carry it up the lane" bet with no multiplier attached to it, which is not the bet the carry factor was priced for. In a crew it is the reverse and stranger: the multiplier exists, but the only seat that can arm it is a guest, and the seat that owns the realm's pot cannot.

Repair prices the door's damage in the same units as the shops: a Boarding that lets bodies reach the door pays twice, once in HP and once at the ARMORY. The dock decides when a Boarding banks its ending — pressing on clears the offer. And the breach is the one loss no purse can answer, which is why the door, not the Boarder, is what the Bastion actually threatens.

Removal test: without the bank family there is no stake, no priced repair, no chosen return — the economy's entire risk surface disappears; a different game.

## Classification

Core, loop. Serves Expression and Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

## Interacts with

- [A Boarding](A-Boarding.md) — the same door is the loop's start line, the start prompt outranks every bank verb, and breach is one of the four enders; that page owns the start line, the shutoff and kills-pay-on-landing, which empties the carry's old role.
- [The Ante](Ante.md) — the Convert runs at the dial as armed, selecting the Convert locks anting up, and both verbs arrive on a guest seat through the same Prep RPC surface; that page owns the ante spend.
- [Going Down](Going-Down.md) — the respawn fee charges the same bank the repair does, and a down seat's carry freezes so a Convert cannot drain it; that page owns the respawn spend.
- [Master Control](Master-Control.md) — dock and Convert are refused for the sequence's whole span; that page owns the sequence.
- [Waves](Waves.md) — the Convert Wave's composition and the carry's Wave table; that page owns delivery.
- [Gatekeepers](Gatekeepers.md) — who bills the door (Default, Thief, Cloaker) and who refuses it (Hunter), and the Thief refuses the Boarder on every Wave mode, so it walks the door whatever is running; that page owns targeting.
- [Realms](Realms.md) — Hatch Armor and open doors persist on the realm; that page owns the one pot per realm the bank is.
- [Upgrades](Upgrades.md) — the Repair Rig and equipment-slot purchases that gate repair, and Hatch Armor sizing the pool; that page owns those purchases and the track.
- [The Crew](The-Crew.md) — per-seat carried piles, and a guest's Convert credit capped by its own depth; that page owns the seat roster and which seat holds which verb.

## Where

- Verb precedence, one pure rule: dock-hold, destroyed, repair, start line, Convert, Withdraw, dock — `Assets/Scripts/Sim/Bastion/HatchVerb.cs` (`HatchVerbs.Choose`, `ShowsDockAlongside`)
- The start line is offered for the whole Boarding, so Convert and Withdraw never lead the prompt on any seat — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`HatchOffersWaveStart`, `MasterControlAwaitsStart`); `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`RebuildGraph` — `terminalSpecs` takes Field terminals only, `FindMaster`); `Assets/Scripts/Sim/Bastion/BastionUnlockGraph.cs` (`TryCompleteTerminal` — "unknown terminal", `IsTerminalDone`)
- The Boarder's own `T` cannot reach the Convert; the guest's Prep RPC can, and is gated on range to the Hatch and nothing else — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`TickPrepCommands` — the `HatchOffersWaveStart` branch above `StartHatchBankWave`); `Assets/Scripts/Game/Bastion/GameController.Client.cs` (`TickClientPrepCommands` — the `hatchStartable` branch, no start-line test); `Assets/Scripts/Game/Net/NetPrep.cs` (`RequestStartHatchBank`, `RequestStartHatchBankServerRpc`, `PrepRange`); `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetStartHatchBank`, `NetPrepInRangeOfHatch`)
- The client refusal inside the Convert start tests the executing machine, so the guest's request never meets it — `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`StartHatchBankWave`, the `_net.IsClientOnly` guard); its two callers — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`TickPrepCommands`); `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetStartHatchBank`)
- The guest is offered the Convert by the Hatch and told it is the start line — `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`OffersHatchBank` — no client test, reads the mirror through `ReadRepairView`; `Prompt` — `HatchVerbs.Choose` with `gc.HatchOffersWaveStart`); `Assets/Scripts/Sim/Bastion/HatchVerb.cs` (`Choose` — `StartWave` above `Convert`)
- A guest's Hatch hold is a repair/withdraw channel the host ticks and credits to that seat's slot — `Assets/Scripts/Game/Net/NetVerbs.cs` (`SetRepairing`, `TickRepair`, `InRepairRange`); `Assets/Scripts/Game/Bastion/GameController.Client.cs` (`RepairHatchFromNet(float,int)`, `TryWithdrawFromHpRequest`)
- The dock is refused on a client seat outright — `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`DockOffered` — `IsClientOnly`)
- The withdraw hold does not consult the start line, so it works unprompted; and the hold's order (dock above repair) is not the prompt's (repair above dock) — `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`Interact` — `DockOffered`, then repair, then the `canStart` withdraw branch, against `Prompt`'s `HatchVerbs.Choose` call)
- The one prompt and the holds at the door — `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`Prompt`, `Interact`, `OffersHatchBank`, `HoldWithdraw`, `TickDock`)
- Withdraw: Prep, full Hatch, carry-0 start; bank → operating seat's pile; refuses uncounted slots; un-earns from the Run tally — `Assets/Scripts/Sim/Run/RunSim.cs` (`Withdraw(float,int)`); rate `Assets/Scripts/Sim/Run/HatchBank.cs` (`WithdrawPerSecond`, `WithdrawUnits`)
- Convert start: Prep `E` then `T`, zone gate, no cap; refuses unbanked=0, all Waves cleared, Master sequence live — `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`StartHatchBankWave`, `HatchBankStartBlocked`); `Assets/Scripts/Sim/Run/RunSim.cs` (`TriggerDraw(WaveMode.HatchBank)`); zone rule `Assets/Scripts/Sim/Bastion/DemoZones.cs` (`HatchBankBlocked`)
- Convert window: timed drip then dump, multiplied by carry; wipe or carry-0 does not end it; no off switch — `Assets/Scripts/Sim/Run/RunSim.cs` (`Tick` HatchBank branch, `SnapshotHatchCarry`, `NotifyAssaultCleared`, `ShutOffFieldWave` refusal); `Assets/Scripts/Sim/Run/HatchBank.cs` (`DurationSeconds`, `DripUnits`, `CarryConvert`); credit path `Assets/Scripts/Sim/Run/RunSim.Wallets.cs` (`DripTick`, `DumpUnbanked`, `CreditSlot`, guest cap)
- The carry counts Waves DRAWN since the last bank, not Waves cleared — `Assets/Scripts/Sim/Run/RunSim.cs` (`WavesSinceBank`, incremented in `LaunchWave`; read by `SnapshotHatchCarry`)
- Convert composition rides the carry's Wave table plus an authored extra Thief — `Assets/Scripts/Sim/Run/HatchBank.cs` (`CarryWaveIndex`, `ApplyHatchComposition`)
- The door is hurt on both modes; field damage scaled; every hit bleeds the carry — `Assets/Scripts/Sim/Run/RunSim.cs` (`DamageHatch`); `Assets/Scripts/Sim/Run/HatchBank.cs` (`FieldWaveDamageScale`, `AttritUnits`)
- Dock offered on a survived window and on a zone shutoff; refused throughout Master Control; ends Returned, keeps a share of the carry — `Assets/Scripts/Sim/Run/RunSim.Dock.cs` (`OfferDockIfBankSurvived`, `OfferDockAfterZoneShutoff`, `DockRun`); `Assets/Scripts/Sim/Run/RunSim.cs` (`EndRun` docked branch, `KeepReturnedShare` in `Assets/Scripts/Sim/Run/RunSim.Wallets.cs`); share `Assets/Scripts/Sim/Run/HatchBank.cs` (`ReturnedKeepShare`, `ReturnedKeep`)
- The portal pad: lights when the way home opens, same rule, same hold — `Assets/Scripts/Game/Bastion/HatchPortal.cs`
- Breach ends the Boarding Lost; fresh full pool each Boarding, sized by Hatch Armor — `Assets/Scripts/Sim/Run/RunSim.cs` (`OnHatchDestroyed`, ctor `new ShipSystems(MetaUpgrades.HatchHp(...))`); `Assets/Scripts/Sim/Ship/ShipSystems.cs` (`HatchDestroyed`, `RepairSalvagePerHp`)
- Repair: bank-charged per HP, drains to exactly zero, fractional accrual, refuses destroyed/full/empty-bank — `Assets/Scripts/Sim/Run/RunSim.cs` (`RepairHatch`, `RepairSalvageSpent`)
- The Rig gate: equipped state and rate pushed every Boarding, zero when rigless; missing step named — `Assets/Scripts/Game/Bastion/GameController.cs` (`ApplyMetaEffects` repair push); `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`Configure`); `Assets/Scripts/Sim/Meta/MetaState.cs` (`RepairMissingStep`)
- Survived window opens doors, and counts the conversion — a tally nothing now gates on, since the equipment lock was re-keyed to lifetime Salvage earned ([Upgrades](Upgrades.md) owns the lock) — `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`OnBankWindowSurvived`); `Assets/Scripts/Sim/Run/RunSim.Wallets.cs` (`CreditSlot`, `HatchConversions`)

## Gaps

- Nothing on the Ship tells you repair costs Salvage. FABRICATION sells you the repair rig and never mentions a price. You learn it at the door, mid-Boarding, with the meter already running.
- Nothing tells you the Hatch is restored to full at the start of every Boarding.
- Nothing tells you that repair works while a Wave is live. The repair prompt is presented alongside the quiet-phase panel, which reads as though it were a Prep-only action.
- Repair charges in whole units and holds the leftover fraction between frames. A very short tap can heal nothing and cost nothing, which looks exactly like the key not working.
- Nothing tells you that a hold you cannot afford will spend your entire bank and stop at zero rather than refusing.
- Nothing tells you that losing the Hatch counts as a lost Boarding in the same way your own death does — same counter, and it unlocks weapon Tiers just the same.
- The HUD warns you the Hatch is under attack but never what is hitting it.
- No upgrade changes repair at all once you own the Rig: not the price per point healed, and not the rate. The Rig is the whole of it.
- The code names four other ship subsystems — a reactor, a tether core, engines, life support. **None of them exist in play.** There is exactly one defended object in the Boarding and it is the Hatch.
- The underlying system would let a destroyed Hatch be repaired back into service, but you can never do it: the Boarding has already ended by the time it hits zero.

*Hatch health, repair rate and repair price: [Field Manual](Field-Manual.md).*
