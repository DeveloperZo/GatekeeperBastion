<!--
Purpose: Current representation of a Boarding — the core loop end to end: embark, Prep and Assault, the wave started at the Hatch and ended at a terminal, the deep start, kills paying on landing, and the four enders — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# A Boarding

A Boarding is one trip out to the Bastion and back, and it is the game's core activity. The Boarder embarks, sets the stake in the quiet of Prep, starts a Wave at the Hatch, then fights up the lane to the zone's terminal and shuts it off. A Gatekeeper killed during a Wave pays Salvage into the realm's bank the moment it falls, so no ending can take those earnings back. The bodies that drip in after a Wave's roster is spent pay nothing. A Boarding finishes in one of four ways: a seat goes down and no respawn is bought, the Hatch is destroyed, the Boarder docks home, or the Boarder throws the Master Control switch and the Bastion is stripped.

## Game

One trip out and back. Everything in it happens in an order I choose and cannot skip.

I embark already tethered. Nothing happens until I start a Wave, and I start it at the Hatch. Prep is the quiet: untimed, unpaid, and the only state where I can ante up or work the door without something chewing on me. Assault runs until I reach the zone's terminal and shut it off, or until the last body the Wave sent has dropped — whichever I get to first.

A field Wave hunts me and cannot touch the Hatch. A Convert hunts the Hatch and converts what I am carrying. Two different fights, one room.

Four ways it finishes, and I keep them in this order. I go down and stay down — a dead seat stands back up out of the realm's bank, while the bank covers the climbing price. The Hatch goes down, which nothing pays for. I dock home after a survived Convert. Or I outlast Master Control and the Bastion is stripped.

Every body I put down survives all four. The kills were banked as they fell, so the ending only decides what the screen calls it.

The Ship offers to start me deeper. It is not a skip. It is the same content delivered back to back, one Assault, no quiet in the middle. Same Waves. Much less room.

## The interaction

The Boarder invokes it — in a friends session, the party lead's EMBARK commits the party — against the Boarding's state machine, `RunSim`. Pressing EMBARK at the NAV MAP loads the Bastion and `EngageTether` moves the phase from Docked to Tethered, landing in Prep: quiet, untimed, nothing arriving until the Boarder starts it. Whatever loadout the seat carries at EMBARK is the loadout for the whole Boarding.

While Tethered the Boarding alternates two states. **Prep** is the only state where anting up, Convert, Withdraw and a safe repair are legal; it pays nothing. **Assault** is a live Wave: the Ante is locked, and kills pay only here (`AwardKill` is refused outside Tethered + Assault).

**A Wave begins at the Hatch and ends at a terminal**, without exception: `HatchOffersWaveStart` answers true while the current zone still has a terminal to run *and* while Master Control sits unstarted, and the control room's Master terminal is never entered into the unlock graph, so the second clause holds from the first Prep to the last.

`T` at the Hatch starts the current zone's trigger — `StartZoneWaveFromHatch` arms and starts the zone's own terminal, so the far spawns, the door and the completion behave as a terminal start would — and, once every field terminal is done, the finale ([Master Control](Master-Control.md)). No select is needed at the start line; the Ante is whatever the dial reads ([The Ante](Ante.md)). The zone's field terminals therefore never offer a start of their own, and refuse to be shut off before the zone's Waves have run (`FieldShutoff.WavesHaveRun`).

A zone's trigger runs its **whole authored Wave span as one composite** — consecutive Waves, delivery-gapped, with no Prep between them (`DemoZones.WavesPerTrigger`, `TriggerCompositeDraw`). How many Waves each zone runs: [Field Manual](Field-Manual.md). Delivery — groups, spawn points, pacing — is [Waves](Waves.md).

**The wave ends at the zone's terminal.** `T` there, in Assault or in Prep, shuts the zone off: the Wave stops, the terminal completes, the zone's door opens (`OpenDoorsFor`, persisted onto the realm so a Docked return finds it open), and **the whole carried pile banks on the spot** (`ShutOffFieldWave` — reaching the switch is the payout). Killing every authored body does not end a field Wave: once the roster is spent the lane **trickles** — a slow drip of Gatekeepers that pays nothing and continues until the shutoff (`FieldTrickle`; trickle kills award zero via `CurrentKillAward`). There is no abort.

A field Wave hunts the Boarder, and — since 2026-08-27 — it can also hurt the Hatch, at a scaled rate; every hit on the Hatch also bleeds the carried pile ([The Hatch](The-Hatch.md) owns the door's rules). A Convert Wave hunts the Hatch and is started, run and settled at the door — the Hatch page owns that window too.

**Kills pay on landing.** `AwardKill` floors the current kill value and `BankKill` credits it straight to the realm's bank at face value the moment the body falls — not split across seats: every seat banks into the one pot ([Realms](Realms.md) owns the pot). Nothing a Boarding earns by killing can be taken back by any ending; the only pile an ending settles is one deliberately withdrawn at the Hatch. Kill value and the Ante curve: [Field Manual](Field-Manual.md).

**Four enders**, in this order:

- A seat goes down and no respawn is bought — priced from the realm's bank, hard-capped per Boarding, on a bleed-out clock ([Going Down](Going-Down.md) owns the respawn spend); with nobody left standing the Boarding ends **Lost** (`EndRun("player")`).
- The Hatch reaches zero: **Lost** on the spot (`EndRun("hatch")`), nothing buys it back.
- The Boarder docks. The offer opens when a zone's terminal is shut off, or when a Convert window is survived, and lasts until the next Wave starts; the shutoff is the half a solo Boarding can actually reach ([The Hatch](The-Hatch.md) owns the Convert and why). A held Interact at the Hatch or at the lit portal pad beside it ends the Boarding **Returned** (screen: DOCKED — a good ending, no loss counter ticks).
- Master Control: the finale sequence ends when the Boarder throws the armed Master terminal — **Escaped**, screen BASTION STRIPPED ([Master Control](Master-Control.md) owns the sequence).

**Starting deeper.** Off the demo, the NAV MAP carries a start-Wave stepper, hidden while the demo runs — the demo's refight lives at the zone terminal's retrigger instead. Its ceiling is the realm's deepest Wave, the higher of deepest *entered* and deepest *cleared* (re-ruled 2026-08-27; dying in a Wave does open it). A deep start is not a skip: the first `T` delivers Waves 1..N back to back as one Assault, each launching a fixed gap after the previous Wave's delivery finishes, with no Prep between — several Waves' worth of Gatekeepers overlapping in the room, one Ante governing the whole chain.

Driven by EMBARK at the NAV MAP, `T` at the Hatch and at terminals, and the interact hold at the Hatch/portal (bindings: [Field Manual](Field-Manual.md)). Shown by the Hatch and terminal prompts, the HUD Wave state, and the door/portal lighting. Reads back as the prompt lines, the banked Salvage readout rising per kill, the terminal-shutoff and door-open log lines, and an end screen — YOU DIED or HATCH DESTROYED for a Lost Boarding, DOCKED for a Returned one, BASTION STRIPPED for Escaped — listing converted, repair spent, net, banked now, deepest Wave, and one button back to the Ship.

## Consequences

The Boarding is the whole risk envelope every other mechanic prices against. The player sets the pace and the stake: nothing arrives uncalled, Prep is unlimited and pays zero, and the distance between the Hatch and the terminal is the fight itself — the trickle exists so the walk cannot be skipped by killing everything first.

Because kills bank as they land, no ending can confiscate earnings. What an ending decides is which lifetime counter ticks (Boardings lost or cleared — a Returned end ticks neither), what the screen says, whether withdrawn Salvage is wiped (Lost), largely kept (banked at the switch), or rebated in small part (Returned), and how deep the realm's record reads. Deepest-entered rises on every draw; deepest-cleared only on a clear — and the deep-start ceiling reads the higher of the two.

Removal test: remove the Boarding and there is no game — this is the core activity every entry in this folder serves.

## Classification

Core, loop. Serves Fellowship, Expression and Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

## Interacts with

- [Waves](Waves.md) — what a trigger delivers: composition, groups, the trickle's cadence, the deep-start overlap; that page owns delivery.
- [The Ante](Ante.md) — the per-pull stake, locked for the whole Assault, one ante per chain; that page owns the ante spend.
- [The Hatch](The-Hatch.md) — the start line, and the bank family (Withdraw, Convert, dock, repair, breach) on the same door; that page owns the withdraw and repair spends.
- [Master Control](Master-Control.md) — the fourth ender: the finale sequence and the switch that ends the Boarding Escaped; that page owns the sequence.
- [Going Down](Going-Down.md) — what a dead seat costs; that page owns the respawn spend.
- [Gatekeepers](Gatekeepers.md) — who a field Wave and a Convert send, and what each kind refuses; that page owns targeting.
- [Realms](Realms.md) — the pot kills bank into, door persistence, and the realm record the deep start reads; that page owns the one pot per realm.
- [The Crew](The-Crew.md) — party lead owns EMBARK, and seats, waits and follows in a friends session; that page owns the session.
- [The Ship](The-Ship.md) — the hub the loop returns to, and the loadout fixed at EMBARK; that page owns the hull and its stations.

## Where

- Phase machine Docked → Tethered → Lost / Returned / Escaped — `Assets/Scripts/Sim/Run/RunSim.cs` (`Phase`, `StartRun`, `EngageTether`, `EndRun`, `CompleteRun`); `Assets/Scripts/Sim/Run/RunPhase.cs`
- Prep ↔ Assault alternation, draw-triggered — `Assets/Scripts/Sim/Run/RunSim.cs` (`TriggerDraw`, `NotifyAssaultCleared`); `Assets/Scripts/Sim/Run/AssaultState.cs`
- The Hatch is the start line for the whole Boarding — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`HatchOffersWaveStart`, `MasterControlAwaitsStart`, `StartZoneWaveFromHatch`, `CanSelectTerminal` refusing every Field terminal while it holds); `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`RebuildGraph` — Field terminals only, so the Master id never reads done)
- A zone's trigger runs its whole span as one composite — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`StartZoneComposite`); `Assets/Scripts/Sim/Run/RunSim.Composite.cs` (`TriggerCompositeDraw`); `Assets/Scripts/Sim/Bastion/DemoZones.cs` (`WavesPerTrigger`, zone tables)
- The wave ends at the terminal; shutoff banks the carry and completes the zone — `Assets/Scripts/Sim/Run/RunSim.cs` (`ShutOffFieldWave`); `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`ShutOffTerminal`, `TickAssaultShutoffCommand`)
- Shutoff availability, Assault and Prep alike, never on an unstarted zone — `Assets/Scripts/Sim/Bastion/FieldShutoff.cs` (`MayRun`, `WavesHaveRun`)
- Doors open on shutoff or survived bank, persisted to the realm — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`OpenDoorsFor`)
- Trickle after the roster is spent; trickle bodies pay zero — `Assets/Scripts/Sim/Waves/FieldTrickle.cs` (`Resolve`, `DueIn`); `Assets/Scripts/Sim/Run/RunSim.cs` (`SetFieldTrickle`, `CurrentKillAward`)
- Kills bank on landing, face value, whole into the realm's pot, Assault-only — `Assets/Scripts/Sim/Run/RunSim.cs` (`AwardKill`, `BankKill`)
- A field Wave damages the Hatch at a scaled rate and bleeds the carried stake — `Assets/Scripts/Sim/Run/RunSim.cs` (`DamageHatch`); `Assets/Scripts/Sim/Run/HatchBank.cs` (`FieldWaveDamageScale`, `AttritUnits`)
- Dock offered on zone close and on a survived bank window; refused while Master Control is live — `Assets/Scripts/Sim/Run/RunSim.Dock.cs` (`OfferDockAfterZoneShutoff`, `OfferDockIfBankSurvived`, `DockRun`)
- Deep start chains Waves 1..N into the first Assault — `Assets/Scripts/Sim/Run/RunSim.cs` (`TriggerDraw` chain arm, `Tick` inter-wave timer, `NotifyDeliveryComplete`)
- Deep-start ceiling is entered-or-cleared, whichever is higher — `Assets/Scripts/Sim/Run/RunSim.cs` (`ClampStartWave`); `Assets/Scripts/Sim/Meta/RealmRecord.cs` (`From`)
- NAV MAP stepper; hidden while the demo runs — `Assets/Scripts/Game/Ship/MapStation.cs` (start-wave picker wiring, demo branch)
- Deepest entered rises on the draw, deepest cleared on the clear — `Assets/Scripts/Sim/Run/RunSim.cs` (`LaunchWave`, `NotifyAssaultCleared`)
- End screens: Lost / DOCKED / BASTION STRIPPED, converted / repair / net / deepest — `Assets/Scripts/Game/Bastion/HudController.EndScreens.cs`

## Gaps

- **Your health never returns while you stand.** Nothing in the game says this. There is no heal, no regen, no item; the only refills are Boarding start and a paid respawn.
- **Repairing the Hatch spends banked Salvage** — the same purse you shop with at the consoles. The prompt quotes the rate, but nothing tells you that the money is coming out of your upgrade budget, or that with an empty bank you cannot repair at all.
- **Repairing needs the Repair Rig equipped.** Without it the key does nothing however much you have banked, and nothing in the Boarding says why.
- Nothing states that there is **no leave-anytime bail-out**. Outside a survived Convert window, down and breach remain the only early enders; the dock prompt is that window only.
- Nothing states that the Ante is **locked** during an Assault. The panel simply stops listing it.
- The start-Wave ceiling is your deepest **cleared** Wave, not deepest reached. The NAV MAP shows the number without saying which of those it is.
- During a deep-start chain the game holds spawns back when too many Gatekeepers are alive at once. You never see this; a chain can feel like it stalled when it is actually throttling itself.
- The Wave-cleared test requires **every** spawned Gatekeeper dead. If one is stuck or lost somewhere in the Bastion, you stay in Assault with nothing visibly happening and no indication of where it is — reaching the terminal is the way out of that, and nothing says so at the time.
- Salvage per kill is floored to a whole number, so part of a raised Ante is rounded away on every body.
- The end screen still labels the Boarding's earnings "converted this boarding", which is the old name for a step that no longer exists. The figure itself is right: everything you killed for.
- Each Boarding uses a seed you cannot see or choose.
- The Drone slot appears on the weapon strip. FABRICATION sells the Decoy that fills it; until then the slot looks like a failed swap.
