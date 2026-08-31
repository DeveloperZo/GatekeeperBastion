<!--
Purpose: Current representation of the co-op session as it is in the build — opening a realm to friends, joining, following in at the break, how a Boarding counts its seats, and which seat may press what.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Ready gate, party-lead-only EMBARK, countdown, cancel and abort-on-un-Ready — `Assets/Scripts/Game/Net/NetPartyLobby.cs` (`RequestPartyEmbarkServerRpc`, `SetReadyServerRpc`, `CancelCountdownServerRpc`, `AbortCountdown`)
- Joins refused while the countdown runs — `Assets/Scripts/Game/Net/NetSession.Internal.cs` (`OnConnectionApproval` refuses on `IsCountingDown`)
- Friends unlock on being armed (MK-1) or the demo ending — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`MultiplayerUnlocked`), gated at `Assets/Scripts/Game/Flow/RealmSelectController.Hosting.cs` and `Assets/Scripts/Game/Flow/MainMenuController.cs`
- Same realm, same join code: CLOSE keeps the allocation; OPEN reuses it — `Assets/Scripts/Game/Flow/RealmSelectController.Hosting.cs` (`OnOpenToFriends`, `OnSessionBack`)
- The Boarding's seat roster: everyone aboard at embark, admissions in Prep only, departures held to the boundary — `Assets/Scripts/Game/Net/RunSeats.cs` (`BeginRun`, `Admit`, `NoteLeft`, `ApplyBoundary`)
- The boundary applied, doors recomputed and the count refreshed in one step — `Assets/Scripts/Game/Bastion/GameController.Seats.cs` (`TickSeatRoster`)
- FOLLOW: Prep-only, free, named refusal during an Assault, door opens on arrival — `Assets/Scripts/Game/Net/NetPlayerAvatar.Follow.cs` (`TryAcceptFollow`, `AwaitingFollow`)
- Seat count into the sim: the roster while a Boarding is live, the connected list before one — `Assets/Scripts/Game/Bastion/GameController.Wallets.cs` (`RefreshPlayerCount`); extra closets per seat (`MuteSeatSpawns`, `Assets/Scripts/Sim/Bastion/BastionSeatSpawns.cs`)
- Composition multiplies bodies by seats; authored kind counts do not scale — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`NextWave` — `enemyCount = min(cap·n, bodies·n)`, kind rows unscaled)
- Guest respawn asks the Boarding's one rule; the host respawns at the DOWN overlay — `Assets/Scripts/Game/Bastion/GameController.Respawn.cs` (`TryNetRespawn`), `Assets/Scripts/Game/Bastion/GameController.Down.cs` (`TryLocalRespawn`, `NotifySeatWentDown`)
- The counter is the Boarding's, the pile freeze, the Boarding-owned bleed clock — `Assets/Scripts/Sim/Run/RunSim.Respawns.cs` (`RespawnsTaken`, `SetSeatDown`), `Assets/Scripts/Sim/Run/PlayerWallets.cs` (`SetDown`, `TakeTotal` skips down seats)
- Per-seat hatch doors recomputed at embark, follow and the break — `Assets/Scripts/Game/Ship/ShipHatchAssignment.cs` (`Recompute`, called from the lobby, the follow and the boundary)
- The per-seat convert ceiling: co-op only, cumulative across the window, excess dropped rather than redistributed — `Assets/Scripts/Sim/Run/RunSim.Wallets.cs` (`CreditSlot` — the `PlayerCount > 1` branch, `_convertedThisBank`, `ResetConvertedThisBank`); the ceiling itself `Assets/Scripts/Sim/Run/HatchBank.cs` (`BankCap` — "Excess is lost, not redistributed")
- A guest's depth reaches the server and never returns to the guest's own save — `Assets/Scripts/Game/Net/NetPlayerAvatar.cs` (`PushProgress`, `ReportDeepestServerRpc`); `Assets/Scripts/Sim/Run/RunSim.Wallets.cs` (`SetSlotDeepest`); a clear in this Boarding raises every seat — `Assets/Scripts/Sim/Run/RunSim.cs` (`NotifyAssaultCleared` → `_wallets.SetDeepest`)
- The four Prep keys a guest holds, each sent as a request rather than applied locally — `Assets/Scripts/Game/Bastion/GameController.Client.cs` (`TickClientPrepCommands`, `LocalPrep`); `Assets/Scripts/Game/Net/NetPrep.cs` (`RequestJuice`, `RequestStartHatchBank`, `RequestStartTerminal`, `RequestStartMaster`, `PrepRange`)
- The host's adjudication of all four, and what each does with them — `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetJuice` applies; `TryNetStartHatchBank` applies; `TryNetStartTerminal` gates on `CanSelectTerminal`; `TryNetStartMaster` refuses unconditionally by design)
- A guest cannot start a zone or the finale: the terminal request meets the same start-line rule the lead's select does — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`CanSelectTerminal` — `if (HatchOffersWaveStart) return false`, `HatchOffersWaveStart`)
- A guest CAN open a Convert on the whole party — `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetStartHatchBank` — range only, then `StartHatchBankWave`); `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`OffersHatchBank` — no client test)
- A guest's ante is tested against the HOST's armed trigger, not the requester's — `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetJuice` — `_armedHatch`, `_armedTerminalId`); the guest's own select stays local `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`ToggleHatchSelect`, `ToggleTerminalSelect`)
- The lead is the seat whose clientId is the server's own — `Assets/Scripts/Game/Net/NetPartyLobby.cs` (`RequestPartyEmbarkServerRpc` — "embark refused — not party lead")
- A guest's Hatch hold reaches the host as a repair channel and credits that seat's slot — `Assets/Scripts/Game/Net/NetVerbs.cs` (`SetRepairing`, `TickRepair`); `Assets/Scripts/Game/Bastion/GameController.Client.cs` (`RepairHatchFromNet(float,int)`)
- The guest's shadow sim is started and never ticked, so its own phase is not the host's — `Assets/Scripts/Game/Bastion/GameController.cs` (`Update` — the `IsClientOnly` return above `Sim.Tick`, the `clientOnly` `StartRun`/`EngageTether` block); the host-written readout it reads instead `Assets/Scripts/Game/Net/NetRunMirror.cs` (`Phase`, `Unbanked`, `LateUpdate`)
- The end screen's one exit; a guest's press is a cooldowned request, not a solo exit — `Assets/Scripts/Game/Bastion/GameController.EndScreen.cs` (`ReturnToShip`, `GuestReturnCooldownSeconds`)
- The lead loads the shared Ship for the whole party — `Assets/Scripts/Game/Net/NetPartyLobby.cs` (`RequestPartyReturnToShip`, `RequestPartyReturnServerRpc`); `Assets/Scripts/Game/Net/NetSession.cs` (`LoadPartyShip`)
-->

# The Crew

A realm's owner opens it to friends from the chooser and reads out a join code, and the friends who join stand on that world's Ship and shop their own loadout out of its one pot. Only the party lead may embark, start a Wave or take the party home; a guest gets four Prep keys, and each one travels as a request the lead's machine settles. The garrison prices the crew: a Wave's body count multiplies by the seats standing, counted once at the break and never mid-Wave, so arriving and leaving both wait for the boundary. A seat that goes down is bought back out of the same pot at a price that climbs with every buy-back, whoever pays it.

## Game

The room counts us.

A realm is opened from the chooser: OPEN TO FRIENDS, and a join code to read out. Friends land on this world's Ship and shop their own loadout out of the world's one pot. The party lead owns EMBARK. Everyone else readies.

Arrive after the party has gone and nothing is wasted. The Ship holds me, the prompt names the next break, and FOLLOW puts me aboard at that break, free, my own hatch door opening as I arrive.

The garrison answers the number of us standing, and it answers at the break, never mid-Wave. A Wave already on the floor stays the Wave it was, whoever arrives or leaves. It counts once, at the boundary, and lives with the answer.

More eyes is the real change. Two of us can hold two Cloakers. A crew burst has a crew to land on. Every Default that flips is flipping onto somebody — attention becomes a thing we pass between us, and passing it well is most of what a good crew is.

Going down puts a seat back on the Ship, waiting, priced. Each of us pays our own way back out of what we were carrying, and the price is the crew's — it climbs with every seat bought back, whoever bought it.

Which is why the ones still standing matter to a Boarder who cannot afford the fee yet. Kills split into every pile, the dead one included, so a crew that keeps fighting is earning the price of its own dead back. When the last of us falls, that stops, and so does the Boarding.

## Details

### Structure

A realm opened to friends is one session in that world. The party stands on one shared Ship; each seat shops its own loadout out of the realm's one pot. The party lead is the seat whose machine runs the Boarding — party lead, host seat and the machine holding the sim are one seat. Every other seat is a guest, whose local copy of the Boarding is a shadow the host writes to but never ticks.

The garrison prices the crew at the break. Wave composition multiplies its body count by the seats playing at the moment the Wave is composed — the extra bodies are all Defaults; the authored Hunter, Thief and Cloaker counts do not scale — and extra spawn closets go live at higher seat counts. No Wave already on the floor changes size, health or composition when a seat joins or leaves.

### Join

The realm's owner opens it from the chooser — OPEN TO FRIENDS (or HOST ON LAN as the fallback) — and reads out a join code; JOIN on the main menu takes it. Closing the panel and opening again for the same realm keeps that code, so a friend already holding it is not locked out by a second press. The session refuses until the realm's Boarder is armed: friends unlock the moment the ARMORY's MK-1 is granted, or the moment the opening stretch ends.

Only the party lead's EMBARK commits the party. It refuses unless every guest has pressed Ready, then runs a countdown any seat can cancel and any un-Ready aborts; joins are refused at the door while the countdown runs, so the party embarks with exactly the seats it counted. As the Boarding goes live, everyone aboard is written into the Boarding's seat roster.

### Follow

A seat that connects mid-Boarding lands on the Ship, held by a wait the server owns. FOLLOW during the party's Prep admits it into the roster, boards it, and opens its own hatch door at that moment; the same press during an Assault refuses with a named reason rather than queueing. Following is free — that seat never died.

A seat that disconnects mid-Boarding is held on the roster until the next Prep, then dropped at that boundary — its door seals and the count moves in the same step. Sealing a hatch under attack would be a free save, so the boundary is the only place the roster moves, in either direction.

### Rules

The lead alone starts a Wave at the Hatch, throws a terminal's or Master Control's shutoff, and takes the party home at the dock.

A guest gets four Prep keys, and each travels as a request the host adjudicates:

- `E` selects, and never leaves the guest's own machine.
- `+`/`-` antes, spending the realm's bank on the party's behalf ([The Ante](Ante.md) owns that spend).
- `T` at a field terminal or at Master Control is always refused, so no guest can start a zone or the finale.
- `T` at the Hatch opens a Convert window that commits every seat's carried pile ([The Hatch](The-Hatch.md) owns the window).

Both roles can hold Interact at the Hatch to repair or withdraw — a guest's hold opens a channel the host ticks against its own range check and credits to that guest's slot. The two lists do not divide the way the fiction suggests: the seat that owns the realm and its pot is the one seat that cannot open a Convert, and the seat that can is a visitor.

A guest's ante is tested against the host's armed trigger, not its own, so a lead standing at a selected trigger refuses every guest's `+`/`-`, and a guest's refused Convert request leaves the host's Hatch armed until the lead looks away.

The end screen's only exit is RETURN TO SHIP, the same button on a win as on a loss. A guest's press does not take that one seat home: it travels as a request to the party lead, who loads the shared Ship for everybody. The guest keeps the button until the load lands — a short cooldown stops a refused load turning Confirm into a flood of requests — and with no lobby to ask, that seat drops to the main menu instead.

Bindings: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

The garrison prices the crew. Wave composition multiplies its body count by the seats playing at the moment the Wave is composed — the extra bodies are all Defaults, the authored Hunter, Thief and Cloaker counts do not scale — and extra spawn closets go live at higher seat counts. No Wave already on the floor changes size, health or composition when a seat joins or leaves; the count is read once, at the boundary, which is why arriving and leaving both wait for the break.

Dying is survivable while the crew is not. A downed host holds behind the DOWN overlay and a downed guest waits on the Ship, both priced by one counter that belongs to the Boarding, not the seat — every buy-back raises the next seat's price, the Boarding's respawns are capped, and a bleed-out clock runs for the Boarding rather than per seat, so a party cannot refill it by taking turns. A down seat's staked pile is frozen while it waits — no Convert by the seats still fighting can drain it. When no seat is fighting and no waiting seat can pay, the Boarding ends.

The respawn spend, its price curve and its cap belong to [Going Down](Going-Down.md); the pot every seat kills into and every fee comes out of is the realm's one pot, owned by [Realms](Realms.md); that kills bank the moment the body falls is [A Boarding](A-Boarding.md)'s.

A crew converts under a per-seat ceiling that solo has no equivalent of. In a session — and only in a session; a solo Boarding runs the credit path untouched — what each seat may bank out of one Convert window is capped at what a solo Boarder could plausibly have carried out of Waves 1 up to that seat's own deepest cleared Wave. The cap is cumulative across the whole window, so a long drip cannot credit up to it on every tick, and what a seat converts past its ceiling is destroyed, not handed to the seats with room left — a shallow guest riding a deep host's Convert brings in less than the pile it fed, and nobody else collects the difference.

The host seat is measured against the realm's own record; a guest is measured against a depth its own client pushes to the server — and that a Wave cleared in this Boarding raises for every seat — which never travels back the other way, so how deep the host's world has been taken stays the host's fact. The window itself — when it opens, the drip, the carry multiplier — is [The Hatch](The-Hatch.md)'s.

Removal test: without the session there is no seat count, no follow, no shared pot to stand a dead seat up from — the friends alpha's stated shape (a friend is the reason a Boarder embarks) disappears. Different game.

### Classification

Core, loop. Serves Fellowship (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Fellowship is rank 1.

### Interacts with

- [Realms](Realms.md) — the session plays in one realm, and OPEN TO FRIENDS is the chooser's half of this page; that page owns the one pot per realm every seat banks into and spends from.
- [Going Down](Going-Down.md) — the Boarding-shared counter, the climbing price, the cap and the bleed-out clock, of which this page says only that the counter is the crew's; that page owns the respawn spend.
- [A Boarding](A-Boarding.md) — the Wave start at the Hatch and the terminal shutoff are the lead's alone, and a guest's request for either is refused by name; that page owns kills-pay-on-landing, which pays into the pot whoever fired.
- [The Hatch](The-Hatch.md) — the bank window a guest opens for the whole party, and the dock only the lead can take; that page owns the withdraw and repair spends.
- [The Ante](Ante.md) — a guest's `+`/`-` spends the realm's bank for everybody, tested against the lead's armed trigger rather than its own; that page owns the ante spend.
- [Waves](Waves.md) — the seat count is a term in Wave composition, read at the boundary; that page owns delivery.
- [Protocols](Protocols.md) — the Crew ability's output finally has a crew to land on; that page owns the sheets and their windows.
- [The Ship](The-Ship.md) — the shared Ship the party stands on, and the per-seat hatch doors; that page owns the hull and its stations.

### Gaps

- The join code is the only admission; there is no friends list and no invite, and nothing in-game says so.
- The follow prompt names the break but not how long the current Wave has left.
- Nothing tells a leaving seat that its door seals at the next boundary rather than immediately.
