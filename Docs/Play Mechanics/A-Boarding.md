<!--
Purpose: Current representation of a Boarding — the core loop end to end: embark, Prep and Assault, the wave started at the Hatch and ended at a terminal, the deep start, kills paying on landing, and the four enders — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Phase machine Docked → Tethered → Lost / Returned / Escaped — Assets/Scripts/Sim/Run/RunSim.cs (Phase, StartRun, EngageTether, EndRun, CompleteRun); Assets/Scripts/Sim/Run/RunPhase.cs
- Prep ↔ Assault alternation, draw-triggered — Assets/Scripts/Sim/Run/RunSim.cs (TriggerDraw, NotifyAssaultCleared); Assets/Scripts/Sim/Run/AssaultState.cs
- The Hatch is the start line while the current zone still has a terminal to run, or for Master Control once the control room is reachable — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (HatchOffersWaveStart = HatchBankStartBlocked || (MasterControlAwaitsStart && ControlRoomReachable), StartZoneWaveFromHatch, CanSelectTerminal refusing every Field terminal while it holds); Assets/Scripts/Game/Bastion/BastionLayout.cs (ControlRoomReachable)
- A zone's trigger runs its whole span as one composite — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (StartZoneComposite); Assets/Scripts/Sim/Run/RunSim.Composite.cs (TriggerCompositeDraw); Assets/Scripts/Sim/Bastion/DemoZones.cs (WavesPerTrigger, zone tables)
- The wave ends at the terminal; shutoff banks the carry and completes the zone — Assets/Scripts/Sim/Run/RunSim.cs (ShutOffFieldWave); Assets/Scripts/Game/Bastion/GameController.Terminals.cs (ShutOffTerminal, TickAssaultShutoffCommand)
- Shutoff availability, Assault and Prep alike, never on an unstarted zone — Assets/Scripts/Sim/Bastion/FieldShutoff.cs (MayRun, WavesHaveRun)
- Doors open on shutoff or survived bank, persisted to the realm — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (OpenDoorsFor)
- Trickle after the roster is spent; trickle bodies pay zero — Assets/Scripts/Sim/Waves/FieldTrickle.cs (Resolve, DueIn); Assets/Scripts/Sim/Run/RunSim.cs (SetFieldTrickle, CurrentKillAward)
- Kills bank on landing, face value, whole into the realm's pot, Assault-only — Assets/Scripts/Sim/Run/RunSim.cs (AwardKill, BankKill)
- Recall pad beside T2/T5: hold Interact while Tethered, teleport to the Hatch — Assets/Scripts/Game/Bastion/HatchRecallPad.cs; Assets/Scripts/Game/Bastion/GameController.Faucet.cs (TeleportLocalToHatch)
- A field Wave damages the Hatch at a scaled rate and bleeds the carried stake — Assets/Scripts/Sim/Run/RunSim.cs (DamageHatch); Assets/Scripts/Sim/Run/HatchBank.cs (FieldWaveDamageScale, AttritUnits)
- Dock offered on zone close and on a survived bank window; refused while Master Control is live — Assets/Scripts/Sim/Run/RunSim.Dock.cs (OfferDockAfterZoneShutoff, OfferDockIfBankSurvived, DockRun)
- After a zone shutoff, while the control gate is still shut, solo/host T at the Hatch can Convert — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (TickPrepCommands — HatchOffersWaveStart false, then StartHatchBankWave); Assets/Scripts/Game/Bastion/GameController.HatchBank.cs (HatchBankStartBlocked)
- Deep start chains Waves 1..N into the first Assault — Assets/Scripts/Sim/Run/RunSim.cs (TriggerDraw chain arm, Tick inter-wave timer, NotifyDeliveryComplete)
- Deep-start ceiling is entered-or-cleared, whichever is higher — Assets/Scripts/Sim/Run/RunSim.cs (ClampStartWave); Assets/Scripts/Sim/Meta/RealmRecord.cs (From)
- NAV MAP stepper, shown in the opening too since 2026-09-02 — Assets/Scripts/Game/Ship/MapStation.cs (start-wave picker wiring)
- Deepest entered rises on the draw, deepest cleared on the clear — Assets/Scripts/Sim/Run/RunSim.cs (LaunchWave, NotifyAssaultCleared)
- End screens: Lost / DOCKED / BASTION STRIPPED, converted / repair / net / deepest — Assets/Scripts/Game/Bastion/HudController.EndScreens.cs
-->

# A Boarding

A Boarding is one trip out to the Bastion and back, and it is the game's core activity. The Boarder embarks, sets the stake in the quiet of Prep, starts a Wave at the Hatch, then fights up the lane to the zone's terminal and shuts it off. A Gatekeeper killed during a Wave pays Salvage into the realm's bank the moment it falls, so no ending can take those earnings back. The bodies that drip in after a Wave's roster is spent pay nothing. A Boarding finishes in one of four ways: a seat goes down and no respawn is bought, the Hatch is destroyed, the Boarder docks home, or the Boarder throws the Master Control switch and the Bastion is stripped.

## Game

One trip out and back. Everything in it happens in an order I choose and cannot skip.

I embark already tethered. Nothing happens until I start a Wave, and I start it at the Hatch. Prep is the quiet: untimed, unpaid, and the only state where I can work the door without something chewing on me. Assault runs until I reach the zone's terminal and shut it off, or until the last body the Wave sent has dropped — whichever I get to first.

A field Wave hunts the Hatch at a thinner rate, and hunts me the moment I give it a reason. A Convert hunts the Hatch at full swing and converts what I am carrying. Two different fights, one room.

Four ways it finishes, and I keep them in this order. I go down and stay down — a dead seat stands back up out of the realm's bank, while the bank covers the climbing price. The Hatch goes down, which nothing pays for. I dock home after a survived Convert or a shut-off zone. Or I throw Master Control's switch and the Bastion is stripped.

Every body I put down survives all four. The kills were banked as they fell, so the ending only decides what the screen calls it.

The Ship offers to start me deeper. It is not a skip. It is the same content delivered back to back, one Assault, no quiet in the middle. Same Waves. Much less room.

## Details

### Structure

Embark at the NAV MAP loads the Bastion and tethers the seat. Whatever loadout the seat carries at EMBARK is the loadout for the whole Boarding. In a friends session the party lead's EMBARK commits the party.

While tethered the Boarding alternates two states. **Prep** is quiet, untimed, unpaid — the only state where Convert, Withdraw and a safe repair are legal. **Assault** is a live Wave. The Ante is parked; Waves run at dial 0 ([The Ante](Ante.md)).

Four enders close it: an unbought death, a destroyed Hatch, a dock home, or Master Control's switch.

### Start and shutoff

A Wave begins at the Hatch and ends at a terminal. The Hatch is the start line while the current zone still has a terminal to finish, and for Master Control only once the control room is reachable. No select is needed at the start line. Field terminals never offer a start of their own, and refuse to be shut off before the zone's Waves have run.

`T` at the Hatch starts the current zone's trigger — the far spawns, the door and the completion behave as a terminal start would — and, once every field terminal is done and the control gate is open, the finale ([Master Control](Master-Control.md)). A zone's trigger runs its whole authored Wave span as one composite: consecutive Waves, delivery-gapped, with no Prep between them. How many Waves each zone runs: [Field Manual](Field-Manual.md). Delivery is [Waves](Waves.md).

`T` at the zone's terminal, in Assault or in Prep, shuts the zone off: the Wave stops, the terminal completes, the zone's door opens (persisted onto the realm so a Docked return finds it open), and the whole carried pile banks on the spot. Killing every authored body does not end a field Wave: once the roster is spent the lane trickles until the shutoff. There is no abort.

Hold Interact on the authored pad beside that terminal (T2 Portal / T5 Portal) teleports the Boarder back to the Hatch while the Boarding is live. The walk home is optional; the walk out is not.

After a zone shutoff, while the control gate is still shut, a solo or host seat can Convert at the Hatch ([The Hatch](The-Hatch.md) owns that window).

Off the demo, the NAV MAP carries a start-Wave stepper, hidden while the demo runs. Its ceiling is the realm's deepest Wave, the higher of deepest entered and deepest cleared. A deep start is not a skip: the first `T` delivers Waves 1..N back to back as one Assault, each launching a fixed gap after the previous Wave's delivery finishes, with no Prep between.

### Kills

A kill during live Assault credits the realm's bank at face value the moment the body falls — not split across seats: every seat banks into the one pot ([Realms](Realms.md) owns the pot). Nothing a Boarding earns by killing can be taken back by any ending; the only pile an ending settles is one deliberately withdrawn at the Hatch. Trickle bodies pay nothing. Kill value and the Ante curve: [Field Manual](Field-Manual.md).

### Enders

Four ways it finishes, in this order:

- A seat goes down and no respawn is bought — priced from the realm's bank, hard-capped per Boarding, on a bleed-out clock ([Going Down](Going-Down.md) owns the respawn spend); with nobody left standing the Boarding ends Lost.
- The Hatch reaches zero: Lost on the spot, nothing buys it back.
- The Boarder docks. The offer opens when a zone's terminal is shut off, or when a Convert window is survived, and lasts until the next Wave starts. A held Interact at the Hatch or at the lit portal pad beside it ends the Boarding Returned (screen: DOCKED — a good ending, no loss counter ticks).
- Master Control: the finale sequence ends when the Boarder throws the armed Master terminal — Escaped, screen BASTION STRIPPED ([Master Control](Master-Control.md) owns the sequence).

### Rules

Nothing arrives uncalled. Prep pays zero. Field terminals refuse start while the Hatch is the start line. Dock is refused while Master Control is live.

A field Wave hunts the Boarder, and it can also hurt the Hatch at a scaled rate; every hit on the Hatch also bleeds the carried pile ([The Hatch](The-Hatch.md) owns the door's rules). A Convert Wave hunts the Hatch and is started, run and settled at the door.

Driven by EMBARK at the NAV MAP, `T` at the Hatch and at terminals, and the interact hold at the Hatch and portal. Shown by the Hatch and terminal prompts, the HUD Wave state, and the door and portal lighting. Reads back as the prompt lines, the banked Salvage readout rising per kill, the terminal-shutoff and door-open log lines, and an end screen — YOU DIED or HATCH DESTROYED for a Lost Boarding, DOCKED for a Returned one, BASTION STRIPPED for Escaped — listing converted, repair spent, net, banked now, deepest Wave, and one button back to the Ship.

Bindings: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

The Boarding is the whole risk envelope every other mechanic prices against. The player sets the pace and the stake: nothing arrives uncalled, Prep is unlimited and pays zero, and the distance between the Hatch and the terminal is the fight itself — the trickle exists so the walk cannot be skipped by killing everything first.

Because kills bank as they land, no ending can confiscate earnings. What an ending decides is which lifetime counter ticks (Boardings lost or cleared — a Returned end ticks neither), what the screen says, whether withdrawn Salvage is wiped (Lost), largely kept (banked at the switch), or rebated in small part (Returned), and how deep the realm's record reads. Deepest-entered rises on every draw; deepest-cleared only on a clear — and the deep-start ceiling reads the higher of the two.

Removal test: remove the Boarding and there is no game — this is the core activity every entry in this folder serves.

### Classification

Core, loop. Serves Fellowship, Expression and Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

### Interacts with

- [Waves](Waves.md) — what a trigger delivers: composition, groups, the trickle's cadence, the deep-start overlap; that page owns delivery.
- [The Ante](Ante.md) — parked; Waves run at dial 0. That page owns the ante spend when it returns.
- [The Hatch](The-Hatch.md) — the start line, and the bank family (Withdraw, Convert, dock, repair, breach) on the same door; that page owns the withdraw and repair spends.
- [Regen](Regen.md) — health climbs between hits; that page owns the rule.
- [Master Control](Master-Control.md) — the fourth ender: the finale sequence and the switch that ends the Boarding Escaped; that page owns the sequence.
- [Going Down](Going-Down.md) — what a dead seat costs; that page owns the respawn spend.
- [Gatekeepers](Gatekeepers.md) — who a field Wave and a Convert send, and what each kind refuses; that page owns targeting.
- [Realms](Realms.md) — the pot kills bank into, door persistence, and the realm record the deep start reads; that page owns the one pot per realm.
- [The Crew](The-Crew.md) — party lead owns EMBARK, and seats, waits and follows in a friends session; that page owns the session.
- [The Ship](The-Ship.md) — the hub the loop returns to, and the loadout fixed at EMBARK; that page owns the hull and its stations.

### Gaps

- Health climbs between hits ([Regen](Regen.md)). Nothing in the fight names the rate or the hold after a swing.
- **Repairing the Hatch spends banked Salvage** — the same purse you shop with at the consoles. The prompt quotes the rate, but nothing tells you that the money is coming out of your upgrade budget, or that with an empty bank you cannot repair at all.
- **Repairing needs the Repair Rig equipped.** Without it the key does nothing however much you have banked, and nothing in the Boarding says why.
- Nothing states that there is **no leave-anytime bail-out**. Dock is offered after a zone shutoff or a survived Convert; down and breach remain the other early enders.
- The Ante is **parked**. The shortcut strip no longer lists it; Waves run at dial 0.
- The start-Wave ceiling is the higher of deepest **entered** and deepest **cleared**. The NAV MAP shows the number without saying which of those it is.
- During a deep-start chain the game holds spawns back when too many Gatekeepers are alive at once. You never see this; a chain can feel like it stalled when it is actually throttling itself.
- The Wave-cleared test requires **every** spawned Gatekeeper dead. If one is stuck or lost somewhere in the Bastion, you stay in Assault with nothing visibly happening and no indication of where it is — reaching the terminal is the way out of that, and nothing says so at the time.
- Salvage per kill is floored to a whole number, so a raised Ante (parked) would round away on every body.
- The end screen still labels the Boarding's earnings "converted this boarding", which is the old name for a step that no longer exists. The figure itself is right: everything you killed for.
- Each Boarding uses a seed you cannot see or choose.
- The Drone slot appears on the weapon strip. FABRICATION sells the Decoy that fills it; until then the slot looks like a failed swap.
