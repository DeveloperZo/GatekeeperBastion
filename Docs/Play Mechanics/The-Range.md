<!--
Purpose: Current representation of the practice range as it is in the build — the kit it stands up, the rules that differ from a Boarding, and the door in and out.
Status: the range itself is in build and auditable; the way in is Coming Soon — nothing a player can press reaches it today, and a door is coming (Zo, 2026-08-29).
in_build: true
coming_soon: true
Update trigger: any milestone that touches this mechanic, in the same change; and the change that lands the way in, which clears the Coming Soon flag.
Last updated: 2026-08-31
Where:
- One component for both directions, 1.0 s hold, prompts either way — `Assets/Scripts/Game/Range/RangeDoor.cs` (`HoldSeconds`, `_returnsToShip`, `Prompt`)
- A released frame zeroes the hold and it starts over — `Assets/Scripts/Game/Range/RangeDoor.cs` (`Interact`, the `_holdFrame != Time.frameCount - 1` check)
- The hold deliberately matches the Ship teleporter's — `Assets/Scripts/Game/Ship/ShipTeleporter.cs` (`UnlockHoldSeconds` = 1.0f)
- The door is an ordinary interact surface — `Assets/Scripts/Game/Player/Interactor.cs` (`IInteractable` — `Prompt`, `Interact(float)`)
- The refusal while a Boarding is live is in the loader, not the door — `Assets/Scripts/Game/Flow/SceneFlow.cs` (`LoadRange`, refuses on `Sim.Phase == RunPhase.Tethered`)
- A refused load re-arms the door instead of deadening it — `Assets/Scripts/Game/Range/RangeDoor.cs` (`Interact`, `_loading = false` and the refusal log)
- The way back is an ordinary hub load with no refusal of its own — `Assets/Scripts/Game/Flow/SceneFlow.cs` (`LoadShipCoop`)
- The kit is a fresh state with the whole catalogue owned, adopted on the first frame rather than on the first grant — `Assets/Scripts/Game/Range/RangeConsole.cs` (`Start`, `EnsureRangeState`, `UnlockEverything`)
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
- A returning body comes back to the mark it stood on, not the nearest — `Assets/Scripts/Game/Range/RangeConsole.cs` (`Pack`/`Unpack`, `OnBodyDown`, `Update`)
- Six bodies at once, refused past that — `Assets/Scripts/Game/Range/RangeConsole.cs` (`LaneCap`, the `SpawnAt` cap refusal)
- Targets are the shipped kinds at their own stats, no wave scaling, navmesh-snapped — `Assets/Scripts/Game/Enemies/WaveSpawner.Range.cs` (`SpawnForRange`, `WaveDirector.StatsFor`, multiplier 1f)
- A practice kill pays no Salvage and holds no Assault open — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`CreateActor`, the `waveBody: false` early return, before `AliveCount++` and the death listener)
- A guest cannot stand a target — `Assets/Scripts/Game/Enemies/WaveSpawner.Range.cs` (`SpawnForRange`, `Net.IsClientOnly` refusal)
- Marks at 5, 14, 30, 60 and 90 m, authored and addressed by the same five values — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`RangeMarks`) and `Assets/Scripts/Game/Dev/DevConsole.Range.cs` (`RangeDistances`)
- The scene is Tethered from its first frame like any Bastion — `Assets/Scripts/Game/Bastion/GameController.cs` (`Start` → `StartRun`, `Sim.EngageTether()`)
- Which is why the mission drawer is closed once, through the player's own flag — `Assets/Scripts/Game/Range/RangeRules.cs` (`CloseMissionDrawerOnce`, `Controls.SetMissionDrawerOpen`)
- Wave counter, hatch integrity, salvage cards and the Boarder chip are switched off and unbound on the range HUD — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`CombatHudToHide`, `StripCombatHud`, `UnbindRunEconomyCards`)
- The readout reports shots, damage, last time-to-kill, every cooldown and both toggle states — `Assets/Scripts/Game/Range/RangeReadout.cs` (`Repaint`, `AppendCooldown`, `LastTimeToKill`)
- The range never draws a Wave, and the loader's own remarks say nothing in the hub reaches it — `Assets/Scripts/Game/Flow/SceneFlow.cs` (`Range` const and its remarks)
- Standing targets, cycling kind and protocol, and both toggles are dev-console only and compiled out of shipped builds — `Assets/Scripts/Game/Dev/DevConsole.Range.cs` (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`, `TryArmRange`, `TickRangeDigit`, `ClearRangeLane`)
- Nothing outside that file calls the console's spawn or grants; three grants have no caller at all — `Assets/Scripts/Game/Range/RangeConsole.cs` (`GrantWeapon`, `GrantPayload`, `GrantEquipment` uncalled; `SpawnAt`, `GrantProtocol`, `ClearLane` called only from `Assets/Scripts/Game/Dev/DevConsole.Range.cs`)
- An editor builder authors a slab into both scenes — the way in on the hub, the way back behind the firing line — `Assets/Scripts/Editor/Scenes/SceneBuilder.RangeDoors.cs` (`BuildRangeDoorsInPlace`, `AuthorDoor`, `ShipDoorPos`, `RangeDoorPos`)
- That builder is driven by a file sentinel and deliberately carries no menu row — `Assets/Scripts/Editor/Scenes/SceneBuilder.cs` (`RangeDoorSentinelPath`, `PollSentinel` → `BuildRangeDoorsInPlace`)
- The range scene is built from a Bastion copy and registered in the build settings — `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` (`BuildRangeSceneInPlace`, `StripBastionRooms`, `PlaceControlRoom`, `RegisterRangeScene`)
-->

# The Range

The practice range is a separate room, shaped like the Bastion, where a whole kit can be fired at real Gatekeepers with nothing at stake. It hands the Boarder a fresh kit with everything owned and every weapon at full focus, stands up to six targets at fixed marks from 5 to 90 metres, and bypasses ability cooldowns by default. A weapon can be judged in a minute instead of across three Boardings. It pays no Salvage and the session it runs in cannot write to disk, so nothing done there is carried out of it. Today nothing a player can press reaches the range, the controls that stand targets are compiled out of shipped builds, and a way in is coming.

## Details

### Structure

A separate scene, shaped like the Bastion so that a Gatekeeper can think in it. It draws no Wave, it pays nothing, and what it does to the session's state cannot be written to disk. On its first frame it mints a fresh kit, stands a floor of practice bodies, and shows a readout instead of the fight HUD.

### What it does

The kit is fresh, fully unlocked, and not the player's. Everything owned, weapons the wheel can reach set to full focus, the Decoy payload, the equipment slot and the Repair Rig, the Default protocol with its first passive, and a wallet large enough that nothing on the floor refuses for price.

Two reasons, both load-bearing: judging a weapon while carrying whatever was last equipped answers a question about the save rather than about the weapon, and until the game is tuned everything has to be reachable without earning it first (Zo, 2026-08-22). The conjured wallet raises lifetime-earned with it, because the realm ledger holds earned equal to banked plus spent.

Weapons are dialled, not bought. A grant sets a weapon's wheel points from a flat tier, clamped to what the wheel can actually reach, and re-equips on the spot. Nothing here goes through the ARMORY, and the wheel itself is still bought on the Ship ([Weapons](Weapons.md)).

Protocols are set rather than claimed, because the shop's claim would refuse three ways here: while a Boarding holds the loadout lock (a range is live from its first frame); a protocol already equipped, so cycling back would stop working; anything outside the shop catalogue — the opposite of what a range is for.

Every grant taints the session before it swaps the state. Once tainted, all three write paths become logged no-ops, and a banner sits on screen for the rest of the play session. Quitting or reloading from disk returns the untouched campaign.

Cooldown bypass is on by default, and is a toggle. It works by clearing the timer from outside every frame rather than by branching inside the ability.

Respawn is off by default. A body that goes down comes back at the mark it was standing on, not the nearest one, and turning the mode on does not resurrect anything killed before it was switched on. Clearing the floor clears what was queued to come back to it.

The targets are the game's own Gatekeepers, unscaled — same actor, same stats, same death routing, the kind's own health and no wave multiplier — at authored marks chosen as the range figures of the three Primaries plus two short marks so falloff is checked by walking (weapon ranges: [Field Manual](Field-Manual.md); what each kind refuses: [Gatekeepers](Gatekeepers.md)).

The floor holds six at once and refuses past that — beyond six it is a Wave, and Wave behaviour is what the Bastion is for. Killing a practice body neither pays Salvage into the realm nor holds an Assault open. A guest cannot stand a target at all.

The Active Mission drawer auto-opens on the Prep frame and is closed once, because a floor with no mission has no use for a PREP panel. The wave counter, hatch integrity, the salvage cards and the Boarder chip are switched off on the range's HUD, because the range never changes any of them.

### Way in

Coming Soon. Nothing a player can press on the Ship reaches the range. The shipped co-op hull carries no door; the loader's own remarks say it is entered by opening the scene directly. An editor builder can author a slab into both scenes — the way in on the hub, the way back behind the firing line — and that slab is not how shipped play gets there. A door is coming (Zo, 2026-08-29).

When a door exists it is an ordinary prompt-and-hold surface ([Interacting](Interacting.md)), the same hold the Ship's teleporter uses to release the glass. A released frame zeroes the timer. The refusal lives in the loader, not in the door: it refuses outright while a Boarding is live, because walking into the range from a Boarding would end that Boarding with none of the banking a Docked return does. A refused load re-arms rather than deadening itself. The way back is an ordinary hub load with no refusal of its own.

Standing a target, cycling which kind stands there, cycling the protocol, and both toggles are all on the dev console, behind an editor-or-development-build fence — no shipped build has them. Three of the console's grants (weapon, payload, equipment) have no caller anywhere.

### Rules

The range pays no Salvage. The session it runs in cannot write to disk.

Six bodies at once, refused past that. A guest cannot stand a target.

Cooldown bypass on by default; respawn off by default. Protocols are set, not claimed. Weapons are dialled, not bought.

The loader refuses the range while a Boarding is live.

Weapon ranges that the marks are chosen from: [Field Manual](Field-Manual.md). Bindings the same.

## Additional Details

### Consequences

What the range is for is separating a question about a weapon from a question about a save. Everything a Boarder brings to a fight is bought with banked Salvage on the Ship, and the wheel only ever adds points — there is no refund and no reset, so a purchase into a gun is a permanent commitment of a permanent currency ([Weapons](Weapons.md) owns the purchase; [Realms](Realms.md) owns the one pot it spends). Today the only way to find out whether the BR-12 answers the room the way a Boarder hoped is to buy it, embark, and learn the answer with a Boarding's unbanked Salvage riding on it.

That is the decision the range exists to change: with the range reachable, the sequence becomes rehearse, then spend — hold a weapon at the focus under consideration, put a Hunter at mid range and a Default at long, fire the ability as fast as it can be pressed, read the time-to-kill, and only then decide where the money goes. Without it, the first honest test of a purchase happens after the purchase, in the one place where being wrong also costs the Boarding.

The bypass is what makes that a minute rather than three Boardings. Every ability in the game waits on a real cooldown, so judging one at its shipped wait means judging it four or five times an outing. Clearing the timer from outside collapses that, and keeping it a toggle rather than a fixture means the wait itself can still be put back and judged.

The cost the player must account for is that the range answers a narrower question than it appears to. The bodies are the game's real Gatekeepers at their own stats, but they arrive one at a time at a fixed mark, up to six, with no group, no arrival cadence, no Ante multiplying their number, and no Hatch behind the Boarder to be walked toward ([Waves](Waves.md) owns delivery; [The Ante](Ante.md) owns the wager). A time-to-kill read on the range is a true statement about one body and a misleading one about a Wave, and the six-body cap is the code's own way of saying so.

Removal test, honestly, in two parts. As the build stands, removing the range changes nothing a player decides and nothing a Boarding records: nothing a player can press reaches the scene, and the surface that stands targets and grants kit is compiled out of shipped builds entirely. On today's evidence it is a developer tool, and the entry says so rather than claiming a consequence it cannot show. With the way in landed — the ruled intent — removal changes a real decision: the ARMORY purchase goes back to being made blind, an unclaimable protocol like VEIL or FORGE has no place it can be felt at all, and the cheapest test of a combat change goes back to costing three Boardings. That is a secondary mechanic's consequence, not a core one: it never solves the challenge itself, it changes what a Boarder knows before spending on the thing that does.

### Classification

Secondary, arc. Serves Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Expression at rank 2: the only belief that names an aesthetic is *skills and loadout amplify expression*, and the range is where a loadout can be tried before it is committed. Secondary rather than primary because it never solves a Boarding's challenge — it is available only between Boardings, and works by informing a spend that happens somewhere else. Arc rather than loop because it sits on the realm side of the split, beside the shops, and its whole consequence lands on a permanent purchase.

### Interacts with

- [Weapons](Weapons.md) — the range stands a weapon up at any point on its wheel without buying it, and re-equips on the spot; that page owns the buy, the ring and what a purchase moves.
- [Protocols](Protocols.md) — the range sets a protocol rather than claiming it, which is the only place VEIL and FORGE can be exercised outside the shop's catalogue; that page owns claiming and the sheets.
- [Interacting](Interacting.md) — the way in and out is an ordinary prompt-and-hold surface, and it borrows the release rule and the reach from that verb; that page owns the roster of interact surfaces.
- [Moving](Moving.md) — the marks exist to be walked between, chase-versus-walk margins and weapon falloff being the two things the floor is laid out to let a Boarder check on foot rather than read; that page owns the walk and the sprint pool.
- [The Ship](The-Ship.md) — the hub is where the way in belongs and where the way back returns; the editor builder authors a slab there, and the shipped co-op hull carries no door a player can press. That page owns the hull and its stations.
- [A Boarding](A-Boarding.md) — the loader refuses the range outright while a Boarding is live, so the two are mutually exclusive; that page owns the phase and what a Boarding ends with.
- [Gatekeepers](Gatekeepers.md) — the targets are that page's kinds, at their own stats with no wave scaling; that page owns targeting and what each kind refuses.
- [Waves](Waves.md) — the six-body cap is the boundary between the two: past it, the thing being read is delivery; that page owns delivery.
- [Salvage](Salvage.md) — the range pays nothing and its conjured wallet cannot reach a save; that page owns what a kill is worth. The pot itself is [Realms](Realms.md)'.
