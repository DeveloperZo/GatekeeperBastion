<!--
Purpose: Current representation of the practice range as it is in the build — the kit it stands up, the rules that differ from a Boarding, the surfaces that stand targets and draw waves, and the door in and out.
Status: in build and auditable. The way in shipped 2026-09-03 — a PRACTICE RANGE door beside the ARMORY whose panel picks a protocol and two weapons (owned at the realm's points, unowned at base) and loads the range with exactly that kit. The dev digits are gone: the range's verbs are two keys and the draw on a board live anywhere on the lane and printed on the readout beside its values — Z kind, X wave size, T draw — plus a distance terminal beside the firing line that picks the mark the wave breaches from (20 m until pressed), no cooldown bypass, and the cooldowns reset as a wave starts (M117, criteria 3–4, rewritten twice on 2026-09-03 on Zo's play: nothing is walked to, and there is a key per dimension).
in_build: true
coming_soon: false
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-09-03
Where:
- One component for both directions, 1.0 s hold, prompts either way — `Assets/Scripts/Game/Range/RangeDoor.cs` (`HoldSeconds`, `_returnsToShip`, `Prompt`)
- A released frame zeroes the hold and it starts over — `Assets/Scripts/Game/Range/RangeDoor.cs` (`Interact`, the `_holdFrame != Time.frameCount - 1` check)
- The hold deliberately matches the Ship teleporter's — `Assets/Scripts/Game/Ship/ShipTeleporter.cs` (`UnlockHoldSeconds` = 1.0f)
- The door is an ordinary interact surface — `Assets/Scripts/Game/Player/Interactor.cs` (`IInteractable` — `Prompt`, `Interact(float)`)
- The refusal while a Boarding is live is in the loader, not the door — `Assets/Scripts/Game/Flow/SceneFlow.cs` (`LoadRange`, refuses on `Sim.Phase == RunPhase.Tethered`)
- A refused load re-arms the door instead of deadening it — `Assets/Scripts/Game/Range/RangeDoor.cs` (`Interact`, `_loading = false` and the refusal log)
- The way back is an ordinary hub load with no refusal of its own — `Assets/Scripts/Game/Flow/SceneFlow.cs` (`LoadShipCoop`)
- The door beside the ARMORY is a station: a slab body, a panel cloned from the ARMORY's, three selector rows and CONFIRM / CANCEL — `Assets/Scripts/Game/Ship/RangeDoorStation.cs` (`StationName`, `BuildPanel`, `Confirm`, `WeaponLabel`)
- The kit as data: protocol, Primary, Secondary, and each weapon's wheel points; an owned weapon carries the realm's, an unowned one is base and flagged; wrong-slot, disabled and unsold ids are refused — `Assets/Scripts/Sim/Range/RangeLoadout.cs` (`From`, `PrimaryIsBase`, `SecondaryIsBase`, `Summary`)
- The chosen kit crosses the scene load on a static consumed on the range's first frame, the way the MAP's start wave does — `Assets/Scripts/Game/Flow/GameSession.cs` (`PendingRangeLoadout`, beside `PendingStartWave`)
- Through the door the range owns exactly the chosen kit at the carried points; opened directly it owns the whole catalogue as before — `Assets/Scripts/Game/Range/RangeConsole.cs` (`EnsureRangeState`, `AdoptLoadout`, `UnlockEverything`)
- The practice rule goes on with the kit, on both paths — `Assets/Scripts/Game/Range/RangeConsole.cs` (`EnsureRangeState` → `EnterPractice`) into `Assets/Scripts/Sim/Run/RunSim.Practice.cs` (`EnterPracticeMode`, `PracticeMode`)
- Under the practice rule a kill pays nothing and the Hatch takes no damage, so the Run cannot be Lost there — `Assets/Scripts/Sim/Run/RunSim.cs` (`CurrentKillAward`, the `PracticeMode` term; `DamageHatch`, the early return)
- Carrying a realm's points into the fresh state is an exact replace, centres derived from points — `Assets/Scripts/Sim/Meta/BoarderState.cs` (`ReplacePoints`)
- Weapons arrive at full focus; a grant rewrites wheel points from a flat tier, clamped to the wheel's reach — `Assets/Scripts/Game/Range/RangeConsole.cs` (`GrantWeapon` → `ReplacePointsFromFlatTier`, `WeaponWheel.MaxPurchases`)
- The wheel only ever adds points — no refund, reset or removal member exists — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`Award`, `DistributeFlatTier`; public surface)
- A protocol is set rather than claimed, bypassing the shop's three refusals — `Assets/Scripts/Game/Range/RangeConsole.cs` (`GrantProtocol`) against `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`TryClaim`)
- The conjured wallet keeps the realm's earned-equals-banked-plus-spent ledger in balance — `Assets/Scripts/Game/Range/RangeConsole.cs` (`UnlockEverything`, `RangeSalvage`, `BankedSalvage` and `LifetimeSalvageEarned` raised together)
- The session is tainted before the state is swapped, not after — `Assets/Scripts/Game/Flow/GameSession.cs` (`AdoptDevState` calls `NotifyDevAction` before assigning `Meta`)
- A tainted session's writes are logged no-ops on all three paths — `Assets/Scripts/Game/Flow/GameSession.cs` (`RefusesWrite`, used by `Save`, `SaveBoarder`, `SaveRealm`)
- The taint is on screen for the rest of the session — `Assets/Scripts/Game/Dev/DevTaintBanner.cs` (`IsVisible`, `Draw`)
- Cooldown bypass is on by default, and is a toggle — `Assets/Scripts/Game/Range/RangeRules.cs` (`bool _bypass = true`, `CooldownBypass`, `ToggleCooldownBypass`)
- The bypass clears the timer from outside every frame rather than branching inside the ability — `Assets/Scripts/Game/Range/RangeRules.cs` (`Update` → `PlayerAbility.ClearCooldown`, `PlayerProtocolAbilities.ClearCooldowns`)
- Respawn is off by default, 2 s when on, and ignores anything killed before it was switched on — `Assets/Scripts/Game/Range/RespawnQueue.cs` (`Enabled`, `DefaultDelaySeconds`, `NoteDown`)
- The standing ledger, the six-body cap and the respawn queue remain in the console for the dev console's clear and are off the board — `Assets/Scripts/Game/Range/RangeConsole.cs` (`SpawnAt`, `ClearLane`, `LaneCap`)
- Targets are the shipped kinds at their own stats, no wave scaling, navmesh-snapped — `Assets/Scripts/Game/Enemies/WaveSpawner.Range.cs` (`SpawnForRange`, `WaveDirector.StatsFor`, multiplier 1f)
- A practice kill pays no Salvage and holds no Assault open — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`CreateActor`, the `waveBody: false` early return, before `AliveCount++` and the death listener)
- A guest cannot stand a target — `Assets/Scripts/Game/Enemies/WaveSpawner.Range.cs` (`SpawnForRange`, `Net.IsClientOnly` refusal)
- Marks at 5, 14, 30, 60 and 90 m, authored once as markers with a post either side to read distance by — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`RangeMarks`, `PlaceMarks`)
- The board: one key per dimension, live anywhere on the lane, one verb per frame — Z kind, X wave size, T draws — `Assets/Scripts/Game/Range/RangeBoard.cs` (`Update`, `CycleSize`, `Draw`)
- The distance dimension is the one surface on the lane: a terminal beside the firing line, E cycles the mark the next wave breaches from, and the draw hands that mark's spawn point to the spawner — `Assets/Scripts/Game/Range/RangeDistanceTerminal.cs` (`Interact`, `SelectedMetres`, `SelectedPoint`) into `Assets/Scripts/Game/Range/RangeBoard.cs` (`Draw` writing `WaveSpawner.spawnPoints`)
- The two board keys are bindings in their own `Range` context, off the shortcut strip, and share no key with anything live in a fight, since the fight keys stay live on the lane — `Assets/Scripts/Game/Input/Controls.cs` (`ControlId.RangeKind`, `RangeSize`, `ControlContext.Range`) pinned by `Assets/Tests/EditMode/ControlsTests.cs` (`RangeBoardKeys_LiveOnTheLane_ShareNoKeyWithTheFight`)
- The kind is one selection on the console, cycled from the board through all four — `Assets/Scripts/Game/Range/RangeConsole.cs` (`Kinds`, `SelectedKind`, `CycleKind`)
- Three sizes: one body, wave 1's roster, wave 3's roster; six breach distances, 20 m the default — `Assets/Scripts/Sim/Range/PracticeWave.cs` (`SmallCount`, `NormalCount`, `LargeCount`, `Distances`, `DefaultMetres`, `Next`)
- A draw is wave 1's beat with the roster rewritten to one kind at the size, no debut gate — `Assets/Scripts/Sim/Run/RunSim.Practice.cs` (`DrawPracticeWave`, `PracticeCount`, `ShapePracticeWave`)
- No cooldown bypass; the timers are reset once, from outside, as a wave is drawn — `Assets/Scripts/Game/Range/RangeRules.cs` (`ResetCooldowns`) called from `Assets/Scripts/Game/Range/RangeBoard.cs` (`Draw`)
- The board's line — every key beside its current value, or the standing count while a wave is live — is the readout's third row, rebuilt only when a value changes; the readout's counters restart on each draw so its numbers are that wave's — `Assets/Scripts/Game/Range/RangeBoard.cs` (`Line`, `Draw`) into `Assets/Scripts/Game/Range/RangeReadout.cs` (`Repaint`, `ResetCounters`)
- A draw is refused, and says so, while a wave is live or for a guest — `Assets/Scripts/Game/Range/RangeBoard.cs` (`Draw`, `WaveLive`)
- A range draw is a single-wave assault at the named wave in the named shape, through the real director and beat; nothing chains after it — `Assets/Scripts/Sim/Run/RunSim.Practice.cs` (`DrawPracticeWave` → `TriggerCompositeDraw(wave, wave)`, `ShapeOverride`) and `Assets/Scripts/Sim/Run/RunSim.cs` (`LaunchWave` → `ShapePracticeWave`)
- The range never owes a shutoff, so a practice wave ends when its last body falls — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`FieldWaveAwaitsShutoff`, the `PracticeMode` return) into `Assets/Scripts/Sim/Waves/FieldTrickle.cs` (`Resolve` → `Cleared`)
- The Bastion's own Prep and Assault verbs stand down on the range, so T on the board is not also a refused Hatch draw — `Assets/Scripts/Game/Bastion/GameController.cs` (`TickFightInput`, the `PracticeMode` return)
- Every mark carries a spawn point; the default mark's is alone on the spawner's explicit list until the board hands it another — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`PlaceSpawnPoints`, `WireRangeFixtures` writing `spawnPoints` and the terminal's `_points` / `_metres`)
- The copy's Hatch is switched off, so nothing on the lane targets it — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`StripHatch`) and `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Retarget`, `hatchAvailable` false when `HatchStation.Instance` is null)
- The scene is Tethered from its first frame like any Bastion — `Assets/Scripts/Game/Bastion/GameController.cs` (`Start` → `StartRun`, `Sim.EngageTether()`)
- Which is why the mission drawer is closed once, through the player's own flag — `Assets/Scripts/Game/Range/RangeRules.cs` (`CloseMissionDrawerOnce`, `Controls.SetMissionDrawerOpen`)
- Wave counter, hatch integrity, salvage cards and the Boarder chip are switched off and unbound on the range HUD — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`CombatHudToHide`, `StripCombatHud`, `UnbindRunEconomyCards`)
- The readout reports shots, damage, last time-to-kill, every cooldown and both toggle states — `Assets/Scripts/Game/Range/RangeReadout.cs` (`Repaint`, `AppendCooldown`, `LastTimeToKill`)
- The dev console keeps only its general rows on the range; F4's clear also empties the standing ledger and the readout — `Assets/Scripts/Game/Dev/DevConsole.Range.cs` (`HiddenInRange`, `ClearRangeLane`); the F3 range menu and its digits are gone
- An editor builder authors both ends — the station and its panel on the hub, on the partition wall that closes the ARMORY's bay, 2.5 m to the station's left; the return slab proud of the near wall behind the firing line, framed so it reads as a doorway — `Assets/Scripts/Editor/Scenes/SceneBuilder.RangeDoors.cs` (`BuildRangeDoorsInPlace`, `AuthorShipDoorStation`, `ShipDoorPos`, `StripArmoryBody`, `AuthorRangeReturnDoorInScene`, `AuthorRangeReturnDoor`, `RangeDoorInset`). The Range Scene build authors the return slab too, so a lane rebuild keeps its door, and it copies from `Bastion_Coop.unity` with the copy's Hatch and three portals switched off — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`BuildRangeSceneInPlace`, `HallFloorBounds`, `StripBastionPortals`)
- That builder is driven by a file sentinel and deliberately carries no menu row — `Assets/Scripts/Editor/Scenes/SceneBuilder.cs` (`RangeDoorSentinelPath`, `PollSentinel` → `BuildRangeDoorsInPlace`)
- The range scene is built from a Bastion copy and registered in the build settings; the marks, their spawn points, the distance terminal and the board are authored and wired by the same build — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`BuildRangeSceneInPlace`, `StripBastionRooms`, `PlaceControlRoom`, `PlaceMarks`, `WireRangeFixtures`, `RegisterRangeScene`)
-->

# The Range

The practice range is a separate room, shaped like the Bastion, where a whole kit can be fired at real Gatekeepers with nothing at stake. The way in is a door beside the ARMORY: its panel picks a protocol and two weapons — a weapon the realm owns at the wheel points it owns, one it does not own at base — and confirming loads the range with exactly that kit. From the firing line, one key picks the kind, one the wave size, the terminal at your elbow picks how far out it breaches (20 metres until pressed), and the start key draws a real wave; nothing is walked to. Cooldowns are real, and reset as each wave starts. A weapon can be judged in a minute instead of across three Boardings. It pays no Salvage, cannot lose the Run at the Hatch, and the session it runs in cannot write to disk, so nothing done there is carried out of it.

## Details

### Structure

A separate scene, shaped like the Bastion so that a Gatekeeper can think in it. It pays nothing, and what it does to the session's state cannot be written to disk. On its first frame it adopts the kit chosen at the door, puts the sim under the practice rule, and shows a readout instead of the fight HUD. Down the lane: a post pair at each of the six marks, each mark a place a wave can breach, and the distance terminal a step off the firing line. The verbs are on the board; the distance is on the terminal.

### What it does

The kit is fresh and not the player's, and through the door it is exactly what the panel chose: the protocol, the Primary and the Secondary, each weapon at the realm's own wheel points when the realm owns it and at base — zero points, marked "base (not owned)" on the row and " base" in the log — when it does not. The Decoy payload, the equipment slot with the Repair Rig, and a wallet large enough that nothing on the floor refuses for price are issued to every range kit. Opened directly, with nothing chosen, the range still owns everything at full focus as it did before the door.

Two reasons, both load-bearing: judging a weapon while carrying whatever was last equipped answers a question about the save rather than about the weapon, and an unowned weapon at base is the question the ARMORY purchase actually asks (Zo, 2026-09-02: "for unlocked weapons player can use base level"). The conjured wallet raises lifetime-earned with it, because the realm ledger holds earned equal to banked plus spent.

Weapons are dialled, not bought. A grant sets a weapon's wheel points from a flat tier, clamped to what the wheel can actually reach, and re-equips on the spot. Nothing here goes through the ARMORY, and the wheel itself is still bought on the Ship ([Weapons](Weapons.md)).

Protocols are set rather than claimed, because the shop's claim would refuse three ways here: while a Boarding holds the loadout lock (a range is live from its first frame); a protocol already equipped, so cycling back would stop working; anything outside the shop catalogue — the opposite of what a range is for.

Every grant taints the session before it swaps the state. Once tainted, all three write paths become logged no-ops, and a banner sits on screen for the rest of the play session. Quitting or reloading from disk returns the untouched campaign.

The practice rule goes on with the kit and never comes off. Under it a kill pays nothing into the realm, the Hatch takes no damage so the Run cannot be Lost there, and a wave can be drawn from the board. The copy's Hatch is switched off in the scene besides, so no body on the lane has it to walk to: everything that breaches hunts the Boarder.

The board is two keys and the draw, pressed from wherever the Boarder stands: one cycles the kind through all four, one cycles the size — small, one body; normal, the Bastion's wave 1; large, its wave 3 — and the start key draws. The fourth dimension is the terminal beside the firing line: pressing it cycles the mark the next wave breaches from, 5 to 90 metres, 20 until it is pressed. The readout's third row is the board: every key beside its current value, or how many are standing while a wave is live, so the keys are read rather than remembered. Every action logs: "range spawn — 60 m", "range wave — 11 Hunter (normal) drawn at 60 m", "cooldowns reset — wave start".

There is no cooldown bypass. As a wave is drawn every ability timer is cleared once, from outside rather than by branching inside the ability, so each wave is met with a full kit at the shipped wait and the wait itself is part of what is judged.

The standing-target ledger and its respawn queue are off the board; they remain behind the dev console's clear.

The bodies are the game's own Gatekeepers at wave 1's numbers — same actor, same stats, same death routing — and the marks are the range figures of the three Primaries plus two short marks and the 20 m default, posts to read falloff against by walking and the places a wave can breach (weapon ranges: [Field Manual](Field-Manual.md); what each kind refuses: [Gatekeepers](Gatekeepers.md)).

Killing a practice body neither pays Salvage into the realm nor holds an Assault open. A guest cannot draw.

The draw is a single-wave assault through the same director, cadence and spawner a Boarding uses: wave 1's group width, wave 1's seeded beat, wave 1's health, with the roster rewritten to one kind at the chosen size — a Cloaker on the range is not gated to wave 4 — breaching at the mark the terminal chose ([Waves](Waves.md) owns delivery). Nothing chains after it, the lane never drips, and no shutoff is owed: the Assault ends when the last body falls, and the draw refuses — and says so — while one is still standing. A range wave pays nothing and banks nothing.

The Active Mission drawer auto-opens on the Prep frame and is closed once, because a floor with no mission has no use for a PREP panel. The wave counter, hatch integrity, the salvage cards and the Boarder chip are switched off on the range's HUD, because the range never changes any of them. The Bastion's own Prep and Assault verbs stand down under the practice rule, so the start key on the board is not also a refused Hatch draw.

### Way in

A door on the starboard wall 5 m aft of the ARMORY, authored by the range-door builder into the co-op hull. It is a station — the fifth, PRACTICE RANGE — so pressing it opens a panel in the same chrome the other four use, and nothing loads until the panel is confirmed. Three rows cycle on their button: Protocol (any of DEPOT, VEIL, FORGE, claimed or not), Primary, Secondary. A weapon row reads "<name> — T<n>" when the realm owns it and "<name> — base (not owned)" when it does not. The confirm row says "ENTER THE RANGE — practice, nothing is saved"; CANCEL closes the panel and loads nothing. The panel opens on whatever the Boarder is carrying, so confirming with nothing touched tries the kit in hand.

Confirm hands the kit across the scene load and calls the loader. The refusal lives in the loader, not in the door: it refuses outright while a Boarding is live, because walking into the range from a Boarding would end that Boarding with none of the banking a Docked return does. A refused confirm clears the pending kit and re-arms the row with the reason. The way back is the range's own slab behind the firing line — an ordinary prompt-and-hold surface ([Interacting](Interacting.md)), the same hold the Ship's teleporter uses, a released frame zeroing the timer — and an ordinary hub load with no refusal of its own.

Every range verb is a key, in a shipped build as much as in the editor, and none of them needs the Boarder anywhere in particular: the first cut put posts, a console and a far terminal on the lane, and one play showed a shooting range is not a place you walk to press things; the second cut put six keys on the board, and one message cut them to two and the draw. The distance came back as the one surface on the lane, at the firing line, because Zo asked for it at a terminal. The dev console keeps only its general rows there.

### Rules

The range pays no Salvage. The Hatch cannot be lost. The session it runs in cannot write to disk.

One wave at a time, refused while one is live. A guest cannot draw a wave.

Cooldown bypass on by default; respawn off by default. Protocols are set, not claimed. Weapons are dialled, not bought.

The loader refuses the range while a Boarding is live.

Weapon ranges that the marks are chosen from: [Field Manual](Field-Manual.md). Bindings the same.

## Additional Details

### Consequences

What the range is for is separating a question about a weapon from a question about a save. Everything a Boarder brings to a fight is bought with banked Salvage on the Ship, and the wheel only ever adds points — there is no refund and no reset, so a purchase into a gun is a permanent commitment of a permanent currency ([Weapons](Weapons.md) owns the purchase; [Realms](Realms.md) owns the one pot it spends). Without the range, the only way to find out whether the BR-12 answers the room the way a Boarder hoped is to buy it, embark, and learn the answer with a Boarding's unbanked Salvage riding on it.

That is the decision the range changes: the sequence becomes rehearse, then spend — hold a weapon at the focus under consideration, put a Hunter at mid range and a Default at long, fire the ability as fast as it can be pressed, read the time-to-kill, and only then decide where the money goes. Without it, the first honest test of a purchase happens after the purchase, in the one place where being wrong also costs the Boarding.

The reset is what makes that a minute rather than three Boardings. Every ability in the game waits on a real cooldown; the range does not shorten the wait — the wait is part of what is judged — but it starts every wave from a full kit, so the first minute of each draw is the first minute of a Boarding rather than the tail of the last one.

The three sizes answer three different questions, and the cost the player must account for is knowing which one they asked. One body at a chosen mark answers a question about one body: its health, a weapon's damage, a time-to-kill. A normal or large wave answers a question about delivery — wave 1's group width and seeded beat at wave 1's or wave 3's count, arriving at one mark — with no Ante multiplying it and no Hatch behind the Boarder to be walked toward ([Waves](Waves.md) owns delivery; [The Ante](Ante.md) owns the wager). A time-to-kill read on one body is a true statement about one body and a misleading one about a Wave; a wave on the range is a Wave of one kind with its stake removed, which is a true statement about the delivery and says nothing about what it costs to hold a Hatch under it.

Removal test. With the door and the lane landed, removing the range changes a real decision: the ARMORY purchase goes back to being made blind, an unclaimable protocol like VEIL or FORGE has no place it can be felt at all, and every weapon and wave question goes back to being answered on live Boardings — which is where the 2026-09-02 tuning day answered all of them. That is a secondary mechanic's consequence, not a core one: it never solves the challenge itself, it changes what a Boarder knows before spending on the thing that does.

### Classification

Secondary, arc. Serves Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Expression at rank 2: the only belief that names an aesthetic is *skills and loadout amplify expression*, and the range is where a loadout can be tried before it is committed. Secondary rather than primary because it never solves a Boarding's challenge — it is available only between Boardings, and works by informing a spend that happens somewhere else. Arc rather than loop because it sits on the realm side of the split, beside the shops, and its whole consequence lands on a permanent purchase.

### Interacts with

- [Weapons](Weapons.md) — the range stands a weapon up at any point on its wheel without buying it, and re-equips on the spot; that page owns the buy, the ring and what a purchase moves.
- [Protocols](Protocols.md) — the range sets a protocol rather than claiming it, which is the only place VEIL and FORGE can be exercised outside the shop's catalogue; that page owns claiming and the sheets.
- [Interacting](Interacting.md) — the two doors and the distance terminal are ordinary interact surfaces and borrow the reach and the prompt from that verb; that page owns the roster of interact surfaces. The board is keys, not a surface.
- [Moving](Moving.md) — the marks exist to be walked between, chase-versus-walk margins and weapon falloff being the two things the floor is laid out to let a Boarder check on foot rather than read; that page owns the walk and the sprint pool.
- [The Ship](The-Ship.md) — the door is the hull's fifth station, beside the ARMORY, and the way back returns to the hull. That page owns the hull and its stations.
- [A Boarding](A-Boarding.md) — the loader refuses the range while a Boarding is live, so the two are mutually exclusive; that page owns the phase and what a Boarding ends with.
- [Gatekeepers](Gatekeepers.md) — the targets are that page's kinds, at their own stats with no wave scaling; that page owns targeting and what each kind refuses.
- [Waves](Waves.md) — the board draws wave 1's beat through that page's director, cadence and spawner, at the terminal's mark, with the roster one kind and the stake removed; that page owns delivery and what a wave is made of.
- [Salvage](Salvage.md) — the range pays nothing and its conjured wallet cannot reach a save; that page owns what a kill is worth. The pot itself is [Realms](Realms.md)'.
