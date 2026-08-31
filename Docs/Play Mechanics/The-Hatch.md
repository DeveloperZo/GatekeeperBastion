<!--
Purpose: Current representation of the Hatch bank family — Withdraw, the Convert window, the Docked return and portal, breach loss, and repair — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Verb precedence, one pure rule: dock-hold, destroyed, repair, start line, Convert, Withdraw, dock — Assets/Scripts/Sim/Bastion/HatchVerb.cs (HatchVerbs.Choose, ShowsDockAlongside)
- The Hatch is the start line while HatchBankStartBlocked, or for Master Control once MasterControlAwaitsStart && ControlRoomReachable — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (HatchOffersWaveStart, HatchBankStartBlocked, MasterControlAwaitsStart, StartZoneWaveFromHatch); Assets/Scripts/Game/Bastion/BastionLayout.cs (ControlRoomReachable, FindMaster); Assets/Scripts/Sim/Bastion/BastionUnlockGraph.cs (ControlRoomReachable)
- The Boarder's own T reaches Convert when the start line is off; the guest's Prep RPC never consulted it — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (TickPrepCommands — HatchOffersWaveStart branch above StartHatchBankWave); Assets/Scripts/Game/Bastion/GameController.Client.cs (TickClientPrepCommands — hatchStartable branch, no start-line test); Assets/Scripts/Game/Net/NetPrep.cs (RequestStartHatchBank); Assets/Scripts/Game/Bastion/GameController.NetPrep.cs (TryNetStartHatchBank, NetPrepInRangeOfHatch)
- The client refusal inside the Convert start tests the executing machine, so the guest's request never meets it — Assets/Scripts/Game/Bastion/GameController.HatchBank.cs (StartHatchBankWave, the _net.IsClientOnly guard)
- The withdraw hold does not consult the start line; Interact tests dock above repair, while Choose puts Repair above Dock — Assets/Scripts/Game/Bastion/RepairInteractable.cs (Interact, Prompt, HatchVerbs.Choose)
- A guest's Hatch hold is a repair/withdraw channel the host ticks and credits to that seat's slot — Assets/Scripts/Game/Net/NetVerbs.cs (SetRepairing, TickRepair, InRepairRange); Assets/Scripts/Game/Bastion/GameController.Client.cs (RepairHatchFromNet, TryWithdrawFromHpRequest)
- The dock is refused on a client seat outright — Assets/Scripts/Game/Bastion/RepairInteractable.cs (DockOffered — IsClientOnly)
- Withdraw: Prep, full Hatch, carry-0 start; bank → operating seat's pile; refuses uncounted slots; un-earns from the Run tally — Assets/Scripts/Sim/Run/RunSim.cs (Withdraw); rate Assets/Scripts/Sim/Run/HatchBank.cs (WithdrawPerSecond, WithdrawUnits)
- Convert start: Prep T once the start line is off, zone gate, no cap; refuses unbanked=0, all Waves cleared, Master sequence live — Assets/Scripts/Game/Bastion/GameController.HatchBank.cs (StartHatchBankWave, HatchBankStartBlocked); Assets/Scripts/Sim/Run/RunSim.cs (TriggerDraw WaveMode.HatchBank); Assets/Scripts/Sim/Bastion/DemoZones.cs (HatchBankBlocked)
- Convert window: timed drip then dump, multiplied by carry; wipe or carry-0 does not end it; no off switch — Assets/Scripts/Sim/Run/RunSim.cs (Tick HatchBank branch, SnapshotHatchCarry, NotifyAssaultCleared, ShutOffFieldWave refusal); HatchBank.cs (DurationSeconds, DripUnits, CarryConvert); Assets/Scripts/Sim/Run/RunSim.Wallets.cs (DripTick, DumpUnbanked, CreditSlot)
- The carry counts Waves DRAWN since the last bank, not Waves cleared — Assets/Scripts/Sim/Run/RunSim.cs (WavesSinceBank, incremented in LaunchWave; read by SnapshotHatchCarry)
- Convert composition rides the carry's Wave table plus an authored extra Thief — HatchBank.cs (CarryWaveIndex, ApplyHatchComposition)
- The door is hurt on both modes; field damage scaled; every hit bleeds the carry — Assets/Scripts/Sim/Run/RunSim.cs (DamageHatch); HatchBank.cs (FieldWaveDamageScale, AttritUnits)
- Dock offered on a survived window and on a zone shutoff; refused throughout Master Control; ends Returned, keeps a share of the carry — Assets/Scripts/Sim/Run/RunSim.Dock.cs (OfferDockIfBankSurvived, OfferDockAfterZoneShutoff, DockRun); RunSim.Wallets.cs (KeepReturnedShare); HatchBank.cs (ReturnedKeepShare, ReturnedKeep)
- The portal pad: lights when the way home opens, same rule, same hold — Assets/Scripts/Game/Bastion/HatchPortal.cs
- Breach ends the Boarding Lost; fresh full pool each Boarding, sized by Hatch Armor — Assets/Scripts/Sim/Run/RunSim.cs (OnHatchDestroyed, ctor new ShipSystems(MetaUpgrades.HatchHp(...))); Assets/Scripts/Sim/Ship/ShipSystems.cs (HatchDestroyed, RepairSalvagePerHp)
- Repair: bank-charged per HP, drains to exactly zero, fractional accrual, refuses destroyed/full/empty-bank — Assets/Scripts/Sim/Run/RunSim.cs (RepairHatch, RepairSalvageSpent)
- The Rig gate: equipped state and rate pushed every Boarding, zero when rigless; missing step named — Assets/Scripts/Game/Bastion/GameController.cs (ApplyMetaEffects repair push); RepairInteractable.cs (Configure); Assets/Scripts/Sim/Meta/MetaState.cs (RepairMissingStep)
- Survived window opens doors, and counts the conversion — Assets/Scripts/Game/Bastion/GameController.HatchBank.cs (OnBankWindowSurvived); RunSim.Wallets.cs (CreditSlot, HatchConversions)
-->

# The Hatch

The Hatch is the door the Boarder came in through, and the one thing in the Bastion being
defended: if its health reaches zero the Boarding ends on the spot, and no amount of Salvage
buys it back. It is also the loop's cashier. Standing at the door, the Boarder can draw
banked Salvage into a carried pile, run a Convert that multiplies that pile while
Gatekeepers march on the door, repair the door at a price per point of health, or dock home
when the way out is open. Today the door's one prompt leads with the Wave start whenever a zone is unfinished or
the control room is open for Master Control; Convert and Withdraw answer when the start
line is off (zone terminals done, control gate still shut).

## Game

One door with a pulse, and everything on this hull is arranged around it.

The Hatch is the umbilical back to my Ship. It has its own health and it is the only thing here being defended. It cannot be moved, closed, or traded away. Zero ends the Boarding on the spot — the one loss nothing at all buys back. A dead Boarder can be priced back onto the floor out of the bank. A dead door cannot, at any price, out of any purse.

It starts every Boarding whole. Whatever I let happen to it last time did not carry.

A field Wave can still bite the door — thinner than a Convert, but a body that reaches it is never inert. A Convert is the window where Unbanked converts, and the same window where the door takes the full swing. Defaults and Thieves bill the door. Hunters never do, at any range, standing on it.

Repair is me at the door, holding — with the Rig equipped, or the hold does nothing at any bank balance. It bills banked Salvage per point healed and will take the bank to exactly zero if I let it.

Survive a Convert, or shut the zone's terminal off, and the door offers something better: hold, and dock home with what I banked. The offer lives until the next Wave starts, and never appears after a failed bank.

The arithmetic worth keeping: a Thief that reaches this door costs more than killing it. Not in danger. In money. That is the whole reason to walk out and meet one.

## Details

### Structure

One door, several verbs, one precedence: a dock hold in progress, then destroyed, then repair, then the wave start line, then Convert, then Withdraw, then the dock. The start line outranks every bank verb. [A Boarding](A-Boarding.md) owns the start line itself. This page owns the withdraw and repair spends.

The Hatch is the start line while the current zone still has a terminal to finish, or for Master Control once the control room is reachable. While a zone's terminals are done and the control gate is still shut, the start line is off and Convert and Withdraw can answer.

### Withdraw

In Prep, at a full, undamaged Hatch, with nothing carried, hold Interact: banked Salvage moves into the operating seat's carried pile at a fixed rate (tied to the Rig's repair rate; no Rig needed to withdraw). Kills bank as they land, so Withdraw is the only thing that fills the carry. Withdrawn units leave the Run's banked tally too, so a later Convert cannot count them twice. The withdraw lands in whichever counted seat holds the verb, and refuses an uncounted slot — a guest's hold is a real withdraw the host ticks and credits to that seat.

The hold does not consult the start line, so it can land while the prompt still names the start.

### Convert

In Prep, at a full Hatch, carrying a pile, `T` starts a Convert Wave. With the Ante parked there is no select to lock first. There is no per-Boarding cap. It is refused while the current zone still has a terminal to finish, while the Master Control sequence is live, once every Wave is cleared, or with nothing carried.

Solo and host reach Convert once the start line is off — after a zone shutoff while the control gate is still shut. A guest's start never consulted the start line.

When a window does run it runs a fixed span: the carry drips into the bank multiplied by a carry factor snapshotted from how many field Waves have been drawn since the last bank, and whatever is left dumps when the timer ends. Killing the Wave early or the carry hitting zero does not end the window — a Convert has no off switch, at the door or at any terminal. Duration, drip rate and multiplier: [Field Manual](Field-Manual.md).

The Wave it sends matches the carry's Wave table, with an extra Thief at one authored carry ([Waves](Waves.md)); its Thieves walk the door on every mode ([Gatekeepers](Gatekeepers.md) owns targeting). Surviving the window opens any door the zone has earned and offers the dock; the Hatch breaching ends the Boarding before any of that fires.

### Dock

The dock offer opens on a survived Convert and on a zone's terminal shutoff, and lasts until the next Wave starts; it is refused for the whole of Master Control. Hold Interact at the Hatch — or at the portal, a pad beside the Hatch that lights when the way home opens — and the Boarding ends Returned (screen: DOCKED; the loss counter does not move).

The dock is a host-seat verb only: a guest's hold at the Hatch or the portal falls through to repair or withdraw. Each seat keeps a small authored share of its leftover carry — a rebate, not a conversion. A Lost end wipes the carry entirely; the bank survives both.

### Repair

Hold Interact at a damaged Hatch. The gate is the Rig: repair does nothing unless the Repair Rig is equipped, and the prompt names the missing step. The Rig and the slot are FABRICATION purchases ([Upgrades](Upgrades.md)).

With the Rig, healing applies continuously while held and charges banked Salvage per HP. Fractional HP accrues between frames so a short tap can buy nothing; a hold the bank cannot cover spends the entire bank, heals the affordable fraction, and stops at exactly zero. Rate and price: [Field Manual](Field-Manual.md).

Repair is legal during a live Wave, not only in Prep. While a dock offer is live, the hold docks even if the prompt names repair.

### Breach

Hatch HP at zero ends the Boarding Lost on the spot; there is no repair from zero and no purse that buys a door back. It starts every Boarding at full HP, sized by the realm's Hatch Armor track ([Upgrades](Upgrades.md)).

A field Wave can chip the Hatch at a scaled rate; a Convert takes full damage. Every hit also bleeds the carried pile. Which kinds bill the door and which refuse it: [Gatekeepers](Gatekeepers.md).

### Rules

The start line used to hide Convert for the whole Boarding; it now yields once the zone is done and the control gate is still shut.

Repair and withdraw are every seat's; the dock is the host seat's alone. Driven by held Interact for Withdraw, dock and repair, and Prep `T` for Convert once the start line is off. Shown by the Hatch's one prompt line, the portal's lighting, and the HUD's Hatch bar. Reads back as the running withdraw and repair session totals in the prompt, the carry falling and the bank rising through a Convert, the door-open and dock-offer log lines, and the end screen's repair-spent line.

Bindings: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

The Hatch is the loop's cashier and its one irreplaceable object. Withdraw turns safe progress into a stake that a Convert can multiply, that Hatch hits bleed, and that a death or breach wipes — the bank behind it survives everything, so risk is exactly what was chosen.

Solo, the multiply half is reachable only after a zone's terminals are done and before the control gate opens — the start line yields then, and Convert can answer. A withdrawn pile still has the zone's terminal as a face-value bank point ([A Boarding](A-Boarding.md) owns the shutoff).

Repair prices the door's damage in the same units as the shops: a Boarding that lets bodies reach the door pays twice, once in HP and once at the ARMORY. The dock decides when a Boarding banks its ending — pressing on clears the offer. And the breach is the one loss no purse can answer, which is why the door, not the Boarder, is what the Bastion actually threatens.

Removal test: without the bank family there is no stake, no priced repair, no chosen return — the economy's entire risk surface disappears; a different game.

### Classification

Core, loop. Serves Expression and Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

### Interacts with

- [A Boarding](A-Boarding.md) — the same door is the loop's start line, the start prompt outranks every bank verb, and breach is one of the four enders; that page owns the start line, the shutoff and kills-pay-on-landing, which empties the carry's old role.
- [The Ante](Ante.md) — the Convert runs at the dial as armed, selecting the Convert locks anting up, and both verbs arrive on a guest seat through the same Prep RPC surface; that page owns the ante spend.
- [Going Down](Going-Down.md) — the respawn fee charges the same bank the repair does, and a down seat's carry freezes so a Convert cannot drain it; that page owns the respawn spend.
- [Master Control](Master-Control.md) — dock and Convert are refused for the sequence's whole span; that page owns the sequence.
- [Waves](Waves.md) — Field Waves can chip this door at a scaled rate; that page owns delivery, and the Convert Wave's composition rides the carry's Wave table.
- [Gatekeepers](Gatekeepers.md) — who bills the door (Default, Thief, Cloaker) and who refuses it (Hunter), and the Thief refuses the Boarder on every Wave mode, so it walks the door whatever is running; that page owns targeting.
- [Realms](Realms.md) — Hatch Armor and open doors persist on the realm; that page owns the one pot per realm the bank is.
- [Upgrades](Upgrades.md) — the Repair Rig and equipment-slot purchases that gate repair, and Hatch Armor sizing the pool; that page owns those purchases and the track.
- [The Crew](The-Crew.md) — per-seat carried piles, and a guest's Convert credit capped by its own depth; that page owns the seat roster and which seat holds which verb.

### Gaps

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
