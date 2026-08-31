<!--
Purpose: Current representation of Waves as they are in the build — the garrison's delivery: composition per Wave, groups with gaps, per-Wave health scaling, and the field trickle after the roster is spent.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-30
-->

# Waves

A Wave is what the garrison sends when the Boarder starts one: a fixed set of Gatekeepers, shuffled together and delivered as groups, one spawn point per group, with a gap between groups. Each Wave is bigger than the last in four ways at once — more bodies, tougher bodies, wider groups and a longer gap between groups — so a later Wave arrives over a longer window rather than a faster one. The Ante is parked; Waves run at dial 0. Once a Wave's roster is spent the lane keeps dripping lone Gatekeepers until the terminal is shut off, and those bodies pay no Salvage and mend no health, so waiting a Wave out earns nothing.

## Game

Each Wave is bigger than the last, and it grows in four directions at once. More bodies. Tougher bodies. Wider groups. Longer gaps. I have counted, and the count only ever moves one way.

The Ante is parked. Nothing I can press adds a body.

The kinds arrive on a schedule I cannot buy, sell, or argue with. First Wave: Defaults only. Hunters after that. Thieves and Cloakers later still, thickest at the end. Inside a group the mix is shuffled, so there is no clean phase of one problem — sorting what is in front of me is the job, every group, every time.

What makes a Wave hard was never any single machine. It is overlap: how much of the Wave stands in the room at once. Early, I finish a group and wait. Late, the next group is inside before the last one is down.

Starting deep from the Ship does the same thing on purpose — the overlap moves between Waves instead of within one. Same content. Less room. No quiet.

## The interaction

The garrison invokes this one: a started Wave is delivered onto the field by the spawner, and the delivery — not any single body — is what the Boarder fights. Starting a Wave and shutting it off are [A Boarding](A-Boarding.md)'s pair: a Wave begins at the Hatch and ends at the zone's terminal, and one start runs the zone's whole authored span back to back with no Prep between. What this page owns is what arrives and how.

Each Wave is a deterministic spec of (Wave index, seats): a body count, fixed per-kind counts with Defaults absorbing the remainder, a health multiplier, a group width and a gap between groups. The roster is **shuffled, not blocked** — drawn on the run's seeded stream, so the same seed replays the same roster — and delivered as groups of the Wave's width through **one spawn point per group**, one group per gap, the point rotating group to group. Which kinds exist and what each refuses is [Gatekeepers](Gatekeepers.md)'. The parked Ante would still scale body count only, surplus Defaults ([The Ante](Ante.md)).

**The field trickle — this page owns it.** A field Wave's roster running dry is not the end of it: once the roster is spent, the field is empty and the zone still owes its shutoff, the Assault stays open and the lane drips lone Defaults on a fixed cadence, capped at a small live count, until the terminal is pressed. The one place that decides the field is empty is the one place that starts the drip.

**Trickle bodies pay nothing and mend nothing** — the kill award reads zero for the whole trickle, at the source, so every payout path is covered, and the kill-fed [Regen](Regen.md) boost is skipped on the same seam, because a body that pays nothing must not be a slower way of saying free health. The same award also reads zero inside a Convert window, where the stake is the pay ([The Hatch](The-Hatch.md)).

Driven by the sim's Wave start (the Boarder's `T` — [A Boarding](A-Boarding.md)). Shown by groups arriving at spawn points, and by the lane still feeding after the roster on an unfinished zone. Reads back as the `Gatekeepers` group lines — `group 3: 4 Gatekeepers, 1 Hunter breached at S2 (…/…) — 12 live` — and the sim's `field trickle on/off` lines.

## Consequences

Four things escalate together across a Boarding, and none of them is the Ante's: **more bodies** per Wave; **tougher bodies** — one health multiplier per Wave applied to every kind alike, which is the axis that silently makes a familiar weapon stop working; **wider groups**; and a **gap between groups that widens with the Wave**, so a later Wave is delivered over a longer window rather than a faster one.

The axis carrying the pressure is health per second rather than bodies per second: the 2026-08-27 roster cut took roughly 40% of the body count out and put it back as HP on every kind, and the group gap was then re-solved to hold health-per-second at its pre-cut figures. The field accumulates at a rate that can be moved through rather than one that buries the player where they stand.

Ante would multiply the body count only, and its surplus is entirely Defaults — health, group width, gap and the Hunter/Thief/Cloaker counts never see it ([The Ante](Ante.md)). Seat count multiplies bodies the same way ([The Crew](The-Crew.md)). The dial is parked; those terms sit at zero.

The fixed schedule is what the player plans against: the first Wave is all Defaults and exists to teach the room; Hunters enter on the second and grow; Thieves on the third; Cloakers on the fourth — one Wave after the Thief, deliberately, so each new refusal gets read — and both stay deliberately scarce. Because groups are shuffled, there is never a clean phase of one problem.

The trickle removes the empty corridor: holding the Hatch until the roster dies no longer buys a quiet walk to the terminal, and because trickle bodies pay zero, stalling in the lane costs time and ammunition rather than earning income — a player who never advances cannot out-earn one who does.

Removal test: without the delivery there is no fight between the start and the shutoff — the Boarding collapses into walking between consoles. Different game.

## Classification

Core, loop. Serves Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Fantasy at rank 3: a garrisoned vessel repelling an intrusion.

## Interacts with

- [A Boarding](A-Boarding.md) — this page begins where the start line ends; that page owns the Wave start at the Hatch and the terminal shutoff.
- [Gatekeepers](Gatekeepers.md) — the kinds delivered, each defined by what it refuses, delivery deciding when each kind enters and in what company; that page owns targeting.
- [The Ante](Ante.md) — the wager multiplies body count only, and its surplus is all Defaults; that page owns the ante spend.
- [The Crew](The-Crew.md) — the seat count is a term in composition, read at the Wave boundary and never mid-Wave; that page owns the seat roster.
- [The Hatch](The-Hatch.md) — the Convert window is a Wave-shaped delivery whose kills pay nothing, the stake being the pay; that page owns the window and the withdraw and repair spends.

Counts, composition table, health ladder, group widths and gaps: [Field Manual](Field-Manual.md).

## Where

- The per-Wave spec: body curve, fixed kind rows (Defaults absorb the remainder), health multiplier per Wave, group width, widening group gap, Ante and seats scaling bodies only — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`NextWave`, `HunterPerWave`, `ThiefPerWave`, `CloakerPerWave`, `EnemyHealthPerWave`)
- One health multiplier applied to every kind's own base at spawn — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`SpawnOne`, `CreateActor` — `stats.maxHp * multiplier`), stats per kind from `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`)
- Shuffled-not-blocked roster on the run's seeded stream — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`BuildRoster`)
- Group delivery: one spawn point per group, one group per gap, cap-held aggregate — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`AdvanceWave`, `EmitGroup`)
- One start runs the zone's whole span back to back — `Assets/Scripts/Sim/Bastion/DemoZones.cs` (`WavesPerTrigger`, `FirstWave`, `EndWave`, zone tables); the branch that enforces it — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`StartZoneComposite`, taken whenever `WavesPerTrigger > 1`), `Assets/Scripts/Sim/Run/RunSim.Composite.cs` (`TriggerCompositeDraw`), chained on the director's gap rather than the Prep gap of the sequential path — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`InterWaveGap`); `Assets/Scripts/Sim/Run/RunSim.cs` (`ArmSequentialDraws`, `TerminalChainGap`)
- The spent-roster verdict in one place: hold, start the trickle, or cleared — `Assets/Scripts/Sim/Waves/FieldTrickle.cs` (`Resolve`), applied by `Assets/Scripts/Game/Enemies/WaveSpawner.cs` (`CheckAssaultCleared`)
- The drip itself: fixed cadence, live cap, lone Defaults from the same rotation — `Assets/Scripts/Sim/Waves/FieldTrickle.cs` (`DueIn`, `SecondsBetweenBodies`, `MaxLive`); `Assets/Scripts/Game/Enemies/WaveSpawner.cs` (`TickFieldTrickle`), `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`SpawnTrickleBody`)
- Trickle bodies pay zero at the source; so does a Convert window — `Assets/Scripts/Sim/Run/RunSim.cs` (`CurrentKillAward` returns 0 while `FieldTrickleLive` or `WaveMode.HatchBank`; `SetFieldTrickle`)
- Trickle bodies feed no regen either — `Assets/Scripts/Game/Bastion/GameController.cs` (`GatekeeperKilled` returns on `Sim.FieldTrickleLive` before `PlayerHealth.NotifyKill`)
- The shutoff that ends the trickle is the zone terminal's, owed while the zone is unfinished — `Assets/Scripts/Sim/Bastion/FieldShutoff.cs` (`MayRun`), surfaced through `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`FieldWaveAwaitsShutoff`)

## Gaps

- Nothing in the fight tells you Gatekeeper health scales by Wave. The HUD shows your damage and the Gatekeeper's kind, never its health, so the same weapon quietly taking twice as long reads as the weapon being broken.
- Nothing tells you that group width and the gap between groups change per Wave. These are the two numbers you feel most and the two you are never shown.
- Nothing says that Hunter and Thief counts are fixed per Wave and immune to Ante. Players reasonably assume raising Ante multiplies everything.
- Groups arrive at spawn points in a fixed rotation, one point per group. You can learn the rotation by watching, but nothing marks the points and nothing indicates where the next group is coming from.
- The members of a group spawn inside the spawn point and walk out through its slab.
- Every kind swings on the same clock — kinds differ in what they hit and how hard, never in how often. Nothing communicates this, so Hunters read as faster attackers than they are.
- The composition of the Wave you are about to draw is never previewed by kind. The Prep panel gives you a total body count and nothing else.
