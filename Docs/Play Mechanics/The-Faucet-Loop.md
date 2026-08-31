<!--
Purpose: Current representation of the faucet loop as it is in the build — the Wave that begins at the Hatch and ends at the zone's terminal, which shipped and is on by default, plus the M102 graybox extensions that are built, dev-gated and off. The shipped half is described as what a player meets; the gated half's experience claims are labelled unknown, because the playthrough that is that graybox's whole point has not been run.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change; and the change that graduates the graybox extensions from dev-gated to shipped.
Last updated: 2026-08-31
Where:
- The start line: a wave begins at the Hatch, and that prompt outranks Convert, Withdraw and Dock — Assets/Scripts/Sim/Bastion/HatchVerb.cs (HatchVerb.StartWave, HatchVerbs.Choose, ShowsDockAlongside); Assets/Scripts/Game/Bastion/RepairInteractable.cs (the HatchVerb.StartWave prompt branch)
- T at the Hatch runs the current zone's span; a terminal may not start one while it is — Assets/Scripts/Game/Bastion/GameController.Terminals.cs (HatchOffersWaveStart, StartZoneWaveFromHatch, CanSelectTerminal, TickPrepCommands)
- The wave ends at the zone's terminal, in Assault or in Prep, without being killed to the last body — Assets/Scripts/Sim/Run/RunSim.cs (ShutOffFieldWave); Assets/Scripts/Game/Bastion/GameController.Terminals.cs (ShutOffTerminal, TickAssaultShutoffCommand); Assets/Scripts/Sim/Bastion/FieldShutoff.cs (MayRun, WavesHaveRun)
- The shutoff banks the carry, completes the terminal, opens the door and offers the dock — Assets/Scripts/Sim/Run/RunSim.cs (ShutOffFieldWave — DumpUnbanked, NotifyAssaultCleared); Assets/Scripts/Game/Bastion/GameController.Terminals.cs (OpenDoorsFor); Assets/Scripts/Sim/Run/RunSim.Dock.cs (OfferDockAfterZoneShutoff)
- Which verb is live in which Assault state, shutoff included — Assets/Scripts/Sim/Bastion/BastionVerbs.cs (BastionVerb.Shutoff, BastionVerb.WaveStart, BastionVerb.Faucet, LiveIn, IsLive)
- The terminal's shutoff prompt in both states — Assets/Scripts/Game/Bastion/BastionTerminal.cs (the CanShutOffTerminal branch; FaucetPrompt for the gated origin rows)
- Recall pad beside T2/T5 — Assets/Scripts/Game/Bastion/HatchRecallPad.cs
- The trickle keeps the walk a fight and pays nothing — Assets/Scripts/Sim/Waves/FieldTrickle.cs (Resolve, DueIn, SecondsBetweenBodies, MaxLive); Assets/Scripts/Sim/Run/RunSim.cs (FieldTrickleLive, SetFieldTrickle, CurrentKillAward); a trickle body also mends nothing — Assets/Scripts/Game/Bastion/GameController.cs (GatekeeperKilled)
- Gated: graybox rules — mark, cap, endure bonus, convert bonus, the three axes and the spec transform — Assets/Scripts/Sim/Run/FaucetLoop.cs (MarkAfterWaves, DraftCap, WaveEndureBonus, ConvertBonus, DraftSize, ApplyAxis, FaucetAxis, AxisName)
- The dev toggle, off by default, refused mid-Assault, resetting a live cycle when turned off — Assets/Scripts/Sim/Run/RunSim.Faucet.cs (_faucetActive, FaucetActive, SetFaucetLoop, ResetFaucetCycle)
- The binding and its compilation gate: Home, labelled toggle faucet loop (graybox), wrapped in #if UNITY_EDITOR || DEVELOPMENT_BUILD — Assets/Scripts/Game/Dev/DevCommands.cs (Kind.ToggleFaucetLoop, KeyCode.Home); Assets/Scripts/Game/Dev/DevConsole.cs; Assets/Scripts/Game/Bastion/GameController.Faucet.cs (ToggleFaucetLoop)
- Waves auto-chain with no further input, one gap apart — RunSim.Faucet.cs (FaucetOwnsClear, FaucetChainTick); Assets/Scripts/Sim/Waves/WaveDirector.cs (InterWaveGap)
- Waves hunt the Hatch; sight-provoke suppressed for the cycle — Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs (faucetField in Retarget); RunSim.Faucet.cs (FaucetHatchHunt)
- Hatch damage unscaled while a cycle is live — Assets/Scripts/Sim/Run/RunSim.cs (DamageHatch — Mode == WaveMode.Terminal && !_faucetCycleLive scale gate)
- Income per Wave endured — RunSim.Faucet.cs (FaucetOwnsClear → AwardSplit(FaucetLoop.WaveEndureBonus))
- One Ante for the whole chain — Assets/Scripts/Sim/Run/RunSim.cs (NotifyAssaultCleared — FaucetOwnsClear return above Siphon.Level reset)
- The mark as commitment floor; draft; teleport on confirm — RunSim.Faucet.cs (FaucetMarked, TryArmFaucetShutoff, OpenFaucetDraft, TryPickFaucetAxis); GameController.Faucet.cs (TickFaucetAssaultCommands, TickFaucetDraft, TeleportLocalToHatch, StartFaucetBank)
- Master Control resets a live cycle and drops a pending axis — Assets/Scripts/Sim/Run/RunSim.Master.cs (ResetFaucetCycle)
- Gated defect: only the single-draw arm begins a cycle; both demo zones route to the composite — Assets/Scripts/Sim/Run/RunSim.cs (TriggerDraw, the only BeginFaucetCycleIfEligible call site); Assets/Scripts/Sim/Run/RunSim.Composite.cs (TriggerCompositeDraw, no call); Assets/Scripts/Sim/Bastion/DemoZones.cs (WavesPerTriggerByZone)
- Gated defect: both shutoffs on one latched T — Assets/Scripts/Game/Bastion/GameController.cs (TickFightInput calling TickFaucetAssaultCommands then TickAssaultShutoffCommand); Assets/Scripts/Game/Bastion/GameController.Terminals.cs (TickAssaultShutoffCommand, no faucet guard)
- The record, its acceptance criteria and the owed playthrough — Docs/Milestones/3. completed/M102-the-faucet-loop-graybox.md; the design intent, the pre-registered kill criteria and the co-op shape — Docs/Brainstorms/coop-bastion-complexity.md (The faucet loop, What would kill it, Tests, Open)
-->

# The Faucet Loop

The Faucet Loop is the shape of a field fight: a Wave begins at the Hatch and ends at the
zone's terminal, so the objective is a place to walk to rather than a body count. The
Boarder decides when to leave the Hatch and when to end the Wave. The trip out is the leg
that is paid for: reaching the switch banks the whole carried pile, and the lane keeps
dripping Gatekeepers that pay nothing, so it cannot be cleared in advance from the safety of
the door. That shape is in the build and every player meets it. A set of extensions on top
of it is built, sits behind a developer-only switch, and has never been played. Those
extensions: Waves that keep coming on their own, Gatekeepers that march on the Hatch instead
of the Boarder, income for Waves endured, and a choice of modifiers that grows the longer
the Boarder holds.

## Details

### Structure

The mechanic is two layers. The shipped spine is the loop every player meets: a Wave begins at the Hatch and ends at the zone's terminal. The gated graybox sits on top of that spine behind a developer-only switch; it does not compile into a player build, and the 0.34.2 player build never sees it.

[A Boarding](A-Boarding.md) owns the Boarding that contains this shape — the composite span, the door, the enders — and this page does not restate it. Distances: [Field Manual](Field-Manual.md).

### Start and shutoff

`T` at the Hatch runs the current zone's Wave span. The Hatch is the only start line, and a field terminal refuses to start a Wave while it is. `T` at that zone's terminal shuts the Wave off — in Assault or in Prep, because the field may already be empty when the Boarder gets there and the walk is still the objective. The Wave does not have to be killed to the last body.

Hold Interact on the authored pad beside T2 or T5 teleports back to the Hatch. That is the shipped return, not the gated draft-confirm teleport.

The shutoff banks the whole carried pile, completes the terminal outright, opens the zone's door and offers the dock. The terminal is an off switch, not a trigger: the Wave ends where the Boarder walks to, so the spine between the Hatch and the terminal is the fight. The trip out is the priced leg; the trip home is not a second toll.

### Trickle

Between the two ends the lane keeps dripping: the field trickle holds the walk open once the authored roster is spent, and pays nothing. [Waves](Waves.md) owns the drip's shape.

### Gated

Behind a developer-only switch, off by default, that does not compile into a player build. Auto-chain, Hatch-hunt, endure income, the mark, the draft and the teleport home are not in the 0.34.2 player build.

When that switch is on: Waves keep coming with no further input until the Boarder turns the faucet off; Gatekeepers march on the Hatch (sight-provoke suppressed; damage-provoke and proximity peel unchanged); every faucet Wave cleared pays an endure bonus into the carried pile while kills still pay on landing; after a mark the origin terminal becomes the off verb and a modifier draft grows with the hold (Density, Toughness, Tempo); confirm teleports the Boarder to the Hatch and starts a bank window. The co-op vote is designed, not built.

A demo composite draw never begins a gated cycle. On a save where a cycle can begin, one `T` can run both shutoffs.

### Rules

The shipped spine has no toggle. A field Wave has no ending but the walk to the terminal.

Two shipped defects — shutoff unreachable during a live Wave, and a dock offer that swallowed the start prompt — were found in play and fixed.

Driven by `T` at the Hatch and `T` at the zone's terminal. Shown by the Hatch's start-line prompt and the terminal's shutoff row. Reads back as the door opening and the carried pile banking at the switch.

## Additional Details

### Consequences

The objective is positional, so the walk is the content. Because the terminal ends the Wave, a Boarder cannot finish a zone from the Hatch however well they shoot; and because the trickle holds the lane open once the roster is spent, they cannot empty the lane first and then stroll it. That is a decision the player must account for every zone: hold at the Hatch and let the pile grow, or leave and take the walk while the pile is still unbanked and still bleeding on every Hatch hit ([The Hatch](The-Hatch.md) owns that HP and what it bleeds). The trickle's bodies pay nothing and mend nothing, so stalling in the lane costs time and ammunition and earns neither.

The shutoff is the bank point at the far end. Reaching the switch banks the whole carry, which is what makes the outbound trip the priced leg and the trip home free — and it is why a Boarder who dies 60 m up the spine loses a pile they could have secured by pressing on. Removing that would leave the walk risk with no reward attached to its far end.

It changes what the Bastion's geometry is for. Spawn points become authored approaches on a lane whose two ends are both verbs, rather than a spawn-distance tuning problem. The gated extensions push that further — the stream walking to the Hatch, and the teleport deliberately removing the return leg's risk rather than the outbound one's.

Removal test. Take the loop's shape out and a field Wave has no ending but the last body, the terminal goes back to being a trigger, the walk up the lane stops being the fight the trickle was built to hold open, the carried pile has no bank point at the far end, and nothing opens the zone's door. That is a different game — and it is the game this build stopped being on 2026-08-26.

Play evidence. The shipped half has been played: it is the loop. The gated half has none. M102 is closed on the harness with the playthrough — which is the experiment — owed, and the named question is whether the hold decision is real. That question is unanswered, and the two gated defects above are why it cannot be asked yet.

Whether holding past the mark feels like a bet or a chore, whether the Hatch attrition curve and the draft curve are steep enough to make either end of the hold ever correct, whether standing at the Hatch and mowing converging streams simply beats everything, and whether a Boarder can count the three constants the decision needs — all *unknown — needs play data*. The pre-registered kill criteria and what each would falsify are in the brainstorm (`../Brainstorms/coop-bastion-complexity.md`, *What would kill it*).

Still open, and not resolved here. Whether the shipped Ante dial survives beside the draft or folds into it, whether the draft grows options or picks, and whether kills pay at all under endure income. The language table's field terminal row is owed a recut when the extensions ship for real, not for the graybox.

### Classification

Core, loop. Serves Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Expression is the Boarder authoring the stake by choosing when to leave the Hatch and when to end the Wave; the mechanic's Fellowship half is the co-op vote, which is designed, not built (`../Brainstorms/coop-bastion-complexity.md`, *The faucet loop*).

Core by the contract's removal test: taking the shape out returns the terminal to being a trigger, leaves a field Wave with no ending but the last body, and leaves the carried pile with nowhere to bank at the far end of the lane — a different game, reachable by every player today.

Loop by construction (the brainstorm's own reading): the authored spawn rooms and seat set are the fixed cost; the per-cycle decision of when to leave and when to end the wave is the variety.

### Interacts with

- [A Boarding](A-Boarding.md) — this page is the mechanic and its identity, and that page is where the shipped shape is described in full; that page owns the Boarding this loop is the spine of — the composite span a trigger runs, the enders, and kills-pay-on-landing, still live under the graybox.
- [The Hatch](The-Hatch.md) — the start line is on the Hatch, the Waves are what eat its HP while the Boarder holds, and the graybox's confirm starts one of its windows with an axis and a raised convert multiplier; that page owns the Convert window and the withdraw and repair spends.
- [Waves](Waves.md) — the three drafted axes are multipliers on that page's terms, the chain gap between faucet Waves, and the Tempo floor; that page owns delivery and the trickle's shape.
- [Gatekeepers](Gatekeepers.md) — the graybox changes one term of the rule: sight-provoke is suppressed for the cycle, while damage-provoke and the proximity peel are not; that page owns targeting and the provoke rule.
- [The Ante](Ante.md) — one Ante governs a whole faucet chain, the dial and the spent tally surviving it instead of resetting per Wave; that page owns the ante spend.
- [Realms](Realms.md) — every banked unit lands in the realm's pot, the pile the shutoff banks included; that page owns the one pot per realm.
- [Doors](Doors.md) — the zone's door opens on the shutoff and stays open, the shutoff being what earns it; that page owns the doors and their persistence.
- [Master Control](Master-Control.md) — the finale starts at the Hatch and ends at its own terminal, the same shape, and it resets any live faucet cycle and drops a pending axis; that page owns the sequence.
