<!--
Purpose: Current representation of retriggering — refighting a zone whose Waves are already partly behind the Boarder, and the election between the whole zone and a resume from its deepest cleared Wave — as it is in the build, including the fact that its launch is built and unreachable in normal play.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# Retriggering

Retriggering is a way to refight a zone the Boarder has already partly beaten. At that
zone's deepest terminal, in Prep, the Boarder chooses between running the whole zone again
and resuming from the deepest Wave cleared in an earlier Boarding. Neither is free: the long
way is a longer fight for more Salvage, and the resume refights the Wave it begins on.
Clearing the chain records depth and nothing else, so the terminal still has to be reached
and shut off before the zone's door opens and the carried pile banks. The rule, the choice
and the prompt are all built, but no key can reach the launch today, so what a Boarding
actually gets is the whole zone, every time.

## The interaction

The Boarder invokes it at a zone's deepest field terminal, in Prep, against the zone's
saved per-zone record. It is a **host-side interaction and it is gated to the opening**:
the offer resolver refuses outright when `DemoActive` is false, and refuses for a
client-only seat, so a guest sees no offer and a realm past its first Escaped has none at
all ([The Opening](The-Opening.md)).

**Built, and unreachable in normal play.** The offer rule, the arm, the election and the
prompt row are all present and correct; both routes to the start key are closed by the
loop's start line ([A Boarding](A-Boarding.md) owns it). `HatchOffersWaveStart` answers
true for the whole of a Boarding — its second clause is `MasterControlAwaitsStart`, and
the Master terminal is never entered into the unlock graph (`BastionLayout.RebuildGraph`
loads Field terminals only), so it is never "done". From that one fact:

- **At the terminal** the start key's retrigger branch checks `HatchOffersWaveStart`
  first and refuses by name — *"refused — the wave starts at the Hatch"* — and that
  getter is true whenever an offer stands, so the refusal is unconditional.
- **At the Hatch** `StartZoneWaveFromHatch` does carry a branch that launches the elected
  retrigger, but `TickPrepCommands` clears the arm on the same frame the crosshair leaves
  the armed terminal, so the arm can never still be set when that branch runs.
- **The prompt is unreachable too**, on the same condition: the terminal returns its
  *"start the wave at the Hatch"* row above the retrigger row.

What a Boarding gets instead is the composite from the Hatch: `T` there runs the zone's
first authored Wave through its last, which is exactly the span the composite election
would have run. **The partial start is the part that is lost** — there is no way to begin
a zone anywhere but at its first Wave. The rules below are what the code would do if the
start key reached them.

**When the offer stands.** Every one of these must hold:

- the terminal is the zone's deepest one, and one the zone table authors;
- the zone has a saved deepest cleared Wave above 0 from a *previous* Boarding;
- that saved depth is ahead of what this Boarding has cleared in the zone;
- the zone is not already complete this Boarding;
- the zone's door is not standing open.

That last test reads as "the Convert made the refight moot" in the rule's own comment; what
it means today is that the zone was closed at its terminal in an earlier Boarding and the
open door persisted onto the realm ([Doors](Doors.md) owns that persistence).

**The two elections.** The default is the **whole zone**: its first authored field Wave
through its last. The alternative is a **partial start** at the zone's deepest cleared
Wave — which is the deepest cleared Wave *itself*, not the one after it, so at least one
Wave is always refought. The partial is offered only when it would actually differ from
the whole-zone start; when the saved depth is the zone's first Wave the election is refused
by name.

**Arm, elect, start.** At a terminal holding an offer, the interact key arms it (and
disarms it, and the first arm per zone speaks a line); while it is armed the dial keys swap
the election rather than moving the Ante — anting up has no seat in that Prep, and the Wave
runs at the level the dial already reads ([The Ante](Ante.md)). Looking away from the terminal
drops the arm — which is also what closes the Hatch-side route above. The start key would
launch it, and refuses by name for a client seat, for a vanished offer, for a Wave already
pending, and when the Hatch is the start line, which is the refusal above
([A Boarding](A-Boarding.md) owns the start-line rule).

**What launches.** The elected span runs as one composite: consecutive Waves with no Prep
between them, delivery-gapped, one Ante governing the chain. Every spawn point the zone's
terminals own is switched live first, because a composite crosses every room the zone
holds and one terminal's spawns cannot feed the whole chain. The draw is refused rather
than clamped if the span is out of range or over the per-trigger cap, so a silently
shortened composite can never read as the zone re-run.

**What clearing the chain does — and does not — do.** When the chain's assault ends, the
zone's saved deepest is raised to the elected end Wave and **nothing else happens**. The
zone is not completed by surviving: reaching the terminal and shutting it off is the
objective, and the shutoff is the only thing that completes a zone, opens its door and
banks the carried pile ([A Boarding](A-Boarding.md) owns the shutoff). Per-Wave clears
also raise the saved depth as they land, so a composite that dies mid-chain still records
the Waves it did clear — which is what the next Boarding's partial election reads. The
record is raise-only: refighting a shallower Wave can never lower it. A terminal phase
drops a half-finished composite, and the next Boarding re-offers.

Driven by the interact and dial keys at the zone's deepest field terminal; the start key is
answered by the refusal above (bindings: [Field Manual](Field-Manual.md)). Shown by that terminal's prompt, where it can be reached at all, which reads RESTART ZONE with the Wave
count before it is armed and swaps to the elected row — START AT WAVE N with its own count,
or RESTART ZONE — once it is; today the terminal returns *"start the wave at the Hatch"*
instead. Reads back as the `Terminal` selection and election lines, the refusal line, and —
were a launch to happen — the `Demo` line naming zone, start Wave, Wave count and whether
the composite or the partial was elected.

## Consequences

**As designed, what a Boarder must refight after a loss is a choice rather than a fixed cost.**
Without it a Boarding that died two Waves into a zone re-enters that zone at its first
Wave, every time, and the ground already taken is ground taken again at full price. With
it the player prices the retry themselves: refight the whole zone for the Salvage and the
Ante the extra Waves carry, or resume at the deepest Wave already beaten and pay the
shortest honest route back to the terminal. Neither is free — the partial still refights
the Wave it starts on, so the election would be between a longer earning Boarding and a
shorter risk, not between playing and skipping.

It also keeps the walk honest. Because clearing the chain records depth and nothing else,
electing a partial start would buy a shorter fight and not a completed zone: the terminal
still has to be reached and shut off before the door opens and the pile banks. The saved
depth being raise-only means the option can only ever widen, never close, however badly a
later attempt goes.

**None of that is what a Boarding meets today.** With the launch unreachable, the retry
cost of a lost zone is fixed and the same every time: the Hatch runs the zone's whole
authored span, first Wave through last. Nothing in the Boarding's record changes with the
election, because no election is ever consumed. That is a defect in the build, not the
design — but it is what the entry has to say, and it is the reason the opening does *not*
have a stand-in for its hidden start-Wave picker ([The Opening](The-Opening.md)).

Removal test: as built, the outcomes a Boarding records change with the election, so it is
a mechanic. As reachable, deleting every member listed below would change nothing a player
could observe, and that is the falsifiable statement of the defect.

## Classification

Secondary, loop. Serves Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank
stamp), with Challenge as the vehicle. Expression at rank 2: the player authors the size
of their own retry. The tier is read off the code, which is complete: with the launch
unreachable the mechanic solves nothing in play at all, and it earns a tier again the
moment the start key can reach it.

## Interacts with

- [A Boarding](A-Boarding.md) — this is a second way into the same zone's composite, and it
  defers to the Hatch whenever the Hatch is the start line, which today is always; that page
  owns the Wave start at the Hatch and the terminal shutoff.
- [The Opening](The-Opening.md) — the offer is gated to it, and the hidden start-Wave picker
  has no stand-in while the launch is unreachable; that page owns the gate ladder.
- [Waves](Waves.md) — what the elected span delivers, and the composite's no-Prep pacing;
  that page owns delivery.
- [The Ante](Ante.md) — while the retrigger is armed the dial elects instead of anting up,
  and the Wave runs at the level already set; that page owns the ante spend.
- [The Hatch](The-Hatch.md) — the zone's door opens on the terminal shutoff or a survived
  Convert window, and an open door withdraws the offer; that page owns the Convert window
  and the withdraw and repair spends.
- [Realms](Realms.md) — the per-zone deepest record is realm-side, so what a retrigger can
  resume from is the world's memory rather than the Boarder's; that page owns the realm and
  Boarder split.

## Where

- Offer rules: opening-gated, deepest terminal only, previous-Boarding depth, zone
  incomplete this Boarding, no open door; the partial start and whether it differs —
  `Assets/Scripts/Game/Demo/DemoRun.cs` (`RetriggerOffer`, `DemoRetriggerOffer`,
  `PartialAvailable`, `WavesFrom`)
- Zone tables the offer reads: terminals, Wave span, deepest terminal, door —
  `Assets/Scripts/Sim/Bastion/DemoZones.cs` (`TerminalIdAt`, `ZoneOf`, `FirstWave`,
  `EndWave`, `WavesPerTrigger`, `DeepestTerminalId`, `DoorId`)
- Host-only resolve, field terminals only, refused while a Master Control sequence is live;
  Waves this Boarding has cleared in the zone — `Assets/Scripts/Game/Bastion/GameController.Retrigger.cs`
  (`RetriggerOfferFor`, `ZoneWavesClearedThisRun`)
- Arm and disarm, first-arm line, default election is the whole zone —
  `Assets/Scripts/Game/Bastion/GameController.Retrigger.cs` (`ToggleRetriggerSelect`)
- The dial elects composite vs partial, and refuses by name when there is no partial —
  `Assets/Scripts/Game/Bastion/GameController.Retrigger.cs` (`ToggleRetriggerElection`)
- Start: named refusals, zone spawns switched live, the launch, the log line —
  `Assets/Scripts/Game/Bastion/GameController.Retrigger.cs` (`StartZoneRetrigger`, `ActivateZoneSpawns`)
- Input routing at the terminal — interact arms, dial elects, and the Hatch start line
  outranks the start key — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs`
  (`TickPrepCommands` — the `PullDraw` retrigger branch's `HatchOffersWaveStart` refusal,
  the `Interact` branch, the dial branch, and the look-away that clears `_armedRetrigger`)
- Why that refusal always fires: the start line is offered for the whole Boarding because
  the Master terminal is never in the unlock graph — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs`
  (`HatchOffersWaveStart`, `MasterControlAwaitsStart`); `Assets/Scripts/Game/Bastion/BastionLayout.cs`
  (`RebuildGraph` — `terminalSpecs` built from `Kind == Field` only)
- The Hatch-side launch branch, and the look-away that makes it unreachable —
  `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`StartZoneWaveFromHatch`'s
  `_armedRetrigger` block; `TickPrepCommands`'s opening arm-clear)
- The retrigger prompt row sits below the start-line row and is never returned —
  `Assets/Scripts/Game/Bastion/BastionTerminal.cs` (`Prompt` — the `HatchOffersWaveStart`
  branch above the `RetriggerOfferFor` branch)
- Terminal prompt rows: RESTART ZONE with a Wave count, START AT WAVE N when partial is
  elected — `Assets/Scripts/Game/Bastion/BastionTerminal.cs` (`RetriggerPrompt`)
- The composite draw itself: Tethered + Prep only, no Master sequence, refused rather than
  clamped, no Prep between Waves — `Assets/Scripts/Sim/Run/RunSim.Composite.cs`
  (`TriggerCompositeDraw`)
- A cleared chain records depth and does not complete the zone —
  `Assets/Scripts/Game/Bastion/GameController.Retrigger.cs` (`RecordCompositeClear`)
- Per-Wave depth writes as each field Wave clears, so a chain that dies mid-way still
  records — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`OnFieldWaveCleared`)
- The per-zone deepest record itself, raise-only, and its realm-side storage —
  `Assets/Scripts/Sim/Meta/MetaState.cs` (`SetZoneDeepest`);
  `Assets/Scripts/Sim/Meta/RealmState.cs` (`SetZoneDeepest`, `ZoneDeepest`, `DemoZoneCount`)
- A terminal phase drops a half-finished composite — `Assets/Scripts/Game/Bastion/GameController.Retrigger.cs`
  (`ClearPendingRetrigger`); called from `Assets/Scripts/Game/Bastion/GameController.cs`
  (`OnPhaseChanged`)
