<!--
Purpose: Current representation of Master Control — the finale sequence that ends a Boarding Escaped — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Sequence constants: five terminal Waves, one elite, HP the one lever, presentation-only scale — Assets/Scripts/Sim/Run/MasterControl.cs (TerminalWaves, FinaleEliteCount, FinaleEliteHp, FinaleEliteScale, ApplyFinaleElite)
- Phases: Terminal then Elite; the sequence does not end on its own from Elite — Assets/Scripts/Sim/Run/MasterControl.cs (MasterControlPhase)
- Start: Prep-only, refuses a live or pending sequence, disables deep-start chaining — Assets/Scripts/Sim/Run/RunSim.Master.cs (StartMasterControl)
- Advance: back-to-back Waves on the follow-up timer; elite is a Terminal-mode control-room Wave; killing it arms the switch — Assets/Scripts/Sim/Run/RunSim.Master.cs (AdvanceMasterControl, MasterAwaitsShutoff)
- The switch ends the Boarding: banks the carry, then completes Escaped — Assets/Scripts/Sim/Run/RunSim.Master.cs (ShutOffMasterControl); Assets/Scripts/Sim/Run/RunSim.cs (CompleteRun, CleanExits)
- The elite spec replaces the last control-room Wave; every sequence Wave draws the deepest table — Assets/Scripts/Sim/Run/RunSim.cs (LaunchWave — MasterControlPhase.Elite gate and CurrentWaveIndex >= MaxWaves clamp)
- Terminal mode, so ordinary field-Wave targeting and scaled Hatch damage run through the finale — Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs (ChasesPlayer, hatchAvailable); Assets/Scripts/Sim/Run/RunSim.cs (DamageHatch)
- Starts at the Hatch; the Master terminal refuses to start — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (StartZoneWaveFromHatch finale branch, StartTerminalWave Master refusal, MasterControlAwaitsStart)
- Game-side start gate; the room is not sealed — Assets/Scripts/Game/Bastion/GameController.MasterControl.cs (StartMasterControl)
- The switch is thrown at the terminal, Prep or Assault — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (ShutOffTerminal Master branch, CanShutOffTerminal, TickPrepCommands Master branch)
- The elite arrives in the control room; the room is unsealed, the Hatch closets untouched — Assets/Scripts/Game/Bastion/GameController.HatchBank.cs (OnMasterHatchHalfStarted)
- Docking refused throughout the sequence — Assets/Scripts/Sim/Run/RunSim.Dock.cs (OfferDockIfBankSurvived, OfferDockAfterZoneShutoff, both refusing on MasterSequenceLive)
- Convert refused while the sequence is live — Assets/Scripts/Game/Bastion/GameController.HatchBank.cs (StartHatchBankWave)
- No trickle during the sequence — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (FieldWaveAwaitsShutoff refusing on MasterSequenceLive)
- Demo spine the finale sits after: two zones, T2 and T5, Waves per trigger 2 then 3, doors D1 and D3 — Assets/Scripts/Sim/Bastion/DemoZones.cs (ZoneCount, TerminalIdsByZone, WavesPerTriggerByZone, DoorIdByZone)
-->

# Master Control

Master Control is the finale, and the only way a Boarding ends in a win. Once every field terminal in the Bastion is done, the Boarder starts it at the Hatch like any other Wave: a run of Waves fought in the control room, then one oversized Gatekeeper built from the Thief. Killing that body does not end anything — it arms the Master terminal, and the Boarder has to walk to the switch and throw it, which banks whatever they are carrying and ends the Boarding with the Bastion stripped. Docking home and Convert are both refused for the whole sequence, so once it starts the only ways out are winning it, dying, or losing the Hatch.

## Details

### Structure

The last zone in everything but the zone table. It starts at the Hatch like every other zone. With no field zone left to start, `T` at the Hatch in Prep starts the sequence; the Master terminal itself refuses to start a Wave — it is where the sequence ends.

The demo this sits after is two zones: T2 then T5, two Waves then three, doors D1 then D3. Counts: [Field Manual](Field-Manual.md).

Whatever the Ante dial reads at the start governs every Wave of the sequence — the sim never resets the dial between them, only after the sequence is over ([The Ante](Ante.md) owns the ante spend).

### Terminal waves

Five ordinary Terminal-mode Waves fought in the control room, launched back to back off the follow-up timer, each drawn from the deepest Wave's table. Terminal mode is not a Boarder-only hunt: the Hatch is what they march on, at the scaled rate, and one turns on the Boarder only when shot, when the Boarder is inside its aggro radius, or when the door is gone. The control-room door is not sealed. The room's own spawn closets deliver.

### Elite

One more control-room Wave whose spec is replaced by a single Thief-built elite — one body, no escorts, HP the only mechanical lever, drawn half again as large in presentation only. It arrives where the fight already is; the Hatch's closets are not armed. Killing it does not end the Boarding — it arms the Master terminal. A finale can no more end by attrition than a zone can.

### Switch

`T` at the Master terminal throws the switch: any carried pile banks on the spot, and the Boarding completes — phase Escaped, screen BASTION STRIPPED, the realm's clean-exit counter ticks. Opening and Escaped are that throw after the elite has armed the switch, and nowhere else.

### Rules

Docking is refused throughout. Both dock-offer paths return while the sequence is live. A Convert is refused too. The field trickle never runs. Kills pay as on any field Wave, going down follows the ordinary rule ([Going Down](Going-Down.md)), and the Hatch breaching still ends the Boarding Lost ([The Hatch](The-Hatch.md)).

Refusals are named: not in Prep, sequence already live or pending, draw did not start; the switch answers that the elite is still standing until the elite is down.

Driven by `T` at the Hatch to start and `T` at the Master terminal to end. Shown by the Master terminal's prompt, the control-room fight, and the elite's oversized body. Reads back as the sequence's log lines and the BASTION STRIPPED end screen.

Bindings: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

Master Control is the only way a Boarding ends Escaped: the complete is called from the switch and nowhere else. It is what the whole realm half prices toward — clean exits are the counter it moves, and clearing the last field Wave deliberately does not end anything.

Because the switch banks the carry, a pile withdrawn and fought through the finale is kept, not confiscated; because the elite only arms the switch, the win is the Boarder's hand on the terminal, never the sim deciding for them. Docking and Convert being refused throughout makes the sequence a commitment: once started, the ways out are winning it, dying, or losing the door.

Removal test: without it the Boarding has no win condition — an endless loop with no Escaped, no BASTION STRIPPED, no clean-exit record. A different game.

### Classification

Core, arc. Serves Fantasy and Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

### Interacts with

- [A Boarding](A-Boarding.md) — the fourth ender, beginning at the loop's one start line and ending at a terminal like every Wave; that page owns the start line, the shutoff and kills-pay-on-landing.
- [The Ante](Ante.md) — one ante governs the whole sequence, and the dial is not reset between its Waves; that page owns the ante spend.
- [The Hatch](The-Hatch.md) — the start line stands at the door, docking and Convert are refused for the sequence's whole span, and a breach still ends everything; that page owns the withdraw and repair spends.
- [Waves](Waves.md) — each sequence Wave draws the deepest Wave's table; that page owns delivery.
- [Gatekeepers](Gatekeepers.md) — the elite is Thief-built: Thief damage, Thief speed, Thief refusals; that page owns targeting.
- [Realms](Realms.md) — Escaped ticks the realm's clean exits and the banked carry lands in the realm's pot; that page owns the one pot per realm.
