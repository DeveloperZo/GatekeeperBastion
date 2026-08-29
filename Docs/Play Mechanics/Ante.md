<!--
Purpose: Current representation of the Ante — the per-Wave wager — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# The Ante

The Ante is a wager the Boarder sets in Prep, standing at the Hatch or a terminal, before starting a Wave. Raising it spends Salvage out of the realm's bank; in exchange every Gatekeeper of that Wave pays more Salvage and the Wave sends more of them. The extra bodies are always the plainest kind, never anything tougher, so a raised Wave is the same fight with more in it. Starting the Wave locks the dial for the whole Assault, lowering it again is free and refunds nothing, and it resets to zero once the Assault is over.

## Game

The Ante is a wager, placed per Wave, before I commit. It is the one dial on this machine that is mine.

Raising it multiplies what each body pays and how many bodies arrive. Both halves read off the same table and cannot come apart. It is the cleanest mechanism on the Bastion and I do not fully understand why it was built this honest.

It toughens nothing. It tightens nothing. Every extra body is a Default — never a Hunter, never a Thief. A raised Wave is the same fight with more of the cheapest thing in it.

Set it in Prep, at the trigger, before the select. Once Assault begins it is locked. Turning it down is free and refunds nothing.

It costs money I already own. Every step comes out of the bank — the same bank the weapons come out of, the same bank the door and the dead seats come out of. That is not a stake skimmed off something that was never mine; it is take-home progress, spent forward on a Wave I have not fought yet.

Three lines of the ledger before I raise it. Can I kill the surplus before it reaches me. What is my health afterwards, given it does not come back while I stand. And is the door already hurt — because the repair, the raise and the extra pay all meet in one pot. The Ante pays gross. The Hatch charges net.

Start deep and one ante governs the whole chain. I learned that at full price.

## The interaction

The Boarder invokes it, in Prep only, while looking at a trigger — the Hatch, a field terminal, or Master Control (`TryJuice` carries a look-at gate the Game layer supplies). `+` antes up, `-` antes down. **This page owns the ante spend**: each up step charges `IntensityJuice.CostPerStep` of **banked** Salvage through `RunSim.SpendBanked` — the realm's pot, the same purse the consoles, the repair, and the respawn fee charge — and a step the bank cannot cover is refused with the bank and the cost named in the log. Anting down is free and refunds nothing. The dial is clamped 0..5 (`TetherSiphon`). Cost per step and the curve: [Field Manual](Field-Manual.md).

**Every seat may ante, and every seat spends the same pot.** A guest's `+`/`-` does not move its own machine's dial: `TickClientPrepCommands` sends `NetPrep.RequestJuice`, and the host applies it through `TryNetJuice` → `Sim.TryJuice` — the same `IntensityJuice.Try`, the same `SpendBanked`, the realm's one pot, and the raised Wave the whole crew then fights. Two gates differ from the Boarder's own:

- **The look-at gate stays on the guest's machine.** The host supplies `atTrigger` as true unconditionally and substitutes a **range** test of its own (`NetPrepInRangeOfTrigger` — the Hatch or any Field/Master terminal within `NetPrep.PrepRange`), so a guest's step can be refused for looking away by its own client and for standing too far by the host.
- **The select lock is read on the host's controller**, not the requester's, so a lead standing at a selected trigger refuses every guest's step with the same "juice refused — intensity selected" line, while a guest's own select locks nothing but its own send.

Who holds which seat, and what else arrives on that same Prep surface, is [The Crew](The-Crew.md)'s.

Where a select exists, `E`-selecting arms the start and locks anting up — "juice refused — intensity selected" — and selecting again unlocks it. Three do: the Hatch (arming a Convert), the Master terminal (`CanSelectTerminal` returns true for that kind before any start-line test), and a terminal holding a retrigger offer. The ordinary field-terminal select does not: `CanSelectTerminal` refuses every Field terminal while the Hatch is the start line, which is the whole of a Boarding ([A Boarding](A-Boarding.md) owns that rule). The loop's ordinary start needs no select either — `T` at the Hatch starts the zone at whatever the dial reads, arming the zone's terminal on the Boarder's behalf.

Starting a Wave locks the dial for the whole Assault (`SetSiphonLevel` and `TryJuice` are both Prep-only refusals): the Ante is a pre-commitment, not something backed out of when the room gets loud. When the Assault ends with nothing chained behind it, the dial resets to zero and the spent tally clears (`NotifyAssaultCleared`) — every pull at every trigger is a fresh decision; the game does not remember a setting across Waves. One ante therefore governs a whole deep-start chain and a zone's whole composite span, and one ante rides the whole Master Control sequence — the reset is skipped between its Waves ([Master Control](Master-Control.md)).

**Both halves read one table.** The reward multiplier per dial step is a hand-authored curve in `TetherSiphon.Curve`; `RunSim.KillValueAt` multiplies the per-kill base by it, and `WaveDirector.NextWave` sizes the Wave's body count off the same static table — payout and body count cannot drift apart. The dial scales body count only: composition rows (Hunter, Thief, Cloaker) are never scaled, so **every surplus body is a Default**; health multipliers, group size and spawn gaps belong to the Wave, not the dial. Per kill, the multiplied value is floored to a whole unit.

Driven by `+`/`-` at a looked-at trigger, on any seat (bindings: [Field Manual](Field-Manual.md)). Shown by the trigger's prompt, which prices the armed pull — Ante level, cost per step, bodies. Reads back as the Ante level, the bank falling per step, the refusal lines, and every kill paying the multiplied award.

## Consequences

A raise converts banked progress into a forward bet: the bank drops now, and in exchange every body of the next pull pays more and the pull sends more bodies — all of them Defaults, none of them tougher. Because there is one pot, every step raised is also a step off the same purse the repair, the respawn and the shops draw on: an ante the Boarder cannot finish killing is money burnt, and the damage a bigger Wave leaves behind is only partly paid back before the next one ([Regen](Regen.md)).

In a crew that pot is shared and the dial is not owned: any seat's step spends everybody's bank and sizes everybody's Wave, with no per-seat allowance and no confirmation from the seat that opened the realm. The reset-to-zero after each pull makes the stake per-decision, not per-Boarding.

Removal test: without the Ante, Wave size and income are fixed and the stake half of "the player sets the pace and the stake" is gone — a different game.

## Classification

Core, loop. Serves Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

## Interacts with

- [A Boarding](A-Boarding.md) — locked for the Assault, one ante per deep-start chain and per zone composite; that page owns kills-pay-on-landing, which the multiplier rides.
- [Waves](Waves.md) — the dial scales body count only, and composition, health, groups and gaps are the Wave's own; that page owns delivery.
- [Master Control](Master-Control.md) — one ante governs the whole finale sequence; that page owns the sequence.
- [The Hatch](The-Hatch.md) — a Convert runs at the dial as armed, and a guest reaches the Convert through the same Prep RPC surface it antes on; that page owns the withdraw and repair spends, and the repair charges the same pot.
- [The Crew](The-Crew.md) — every seat can raise the dial and every step comes out of the realm's shared bank; that page owns the seat roster and co-op admission.
- [Going Down](Going-Down.md) — the respawn fee charges the same bank, so a raise is also a step off the way back; that page owns the respawn spend.
- [Realms](Realms.md) — the pot every step spends from; that page owns the one pot per realm.
- [Gatekeepers](Gatekeepers.md) — the surplus is Defaults only and the dial can never add another kind; that page owns targeting.

## Where

- Ante up spends banked Salvage, refuses a short bank by name; lowering is free, no refund — `Assets/Scripts/Sim/Run/IntensityJuice.cs` (`Try`, `CostPerStep`)
- The spend path into the realm's pot — `Assets/Scripts/Sim/Run/RunSim.cs` (`TryJuice`, `SpendBanked`, `JuiceSpentThisPull`)
- Dial 0..5, hand-authored reward curve, one static table — `Assets/Scripts/Sim/Meta/TetherSiphon.cs` (`Curve`, `RewardMultiplierFor`, `Level` clamp)
- Payout reads the curve — `Assets/Scripts/Sim/Run/RunSim.cs` (`KillValueAt`, `CurrentKillValue`, `CurrentKillAward` floor)
- Body count reads the same curve; composition rows never scaled, surplus is all Defaults — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`NextWave`)
- Prep-only, look-at gate; `+`/`-` routed with the trigger test — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`TickPrepCommands` dial branch)
- A guest's ante: sent as a Prep RPC, applied once on the host, charging the same banked pot — `Assets/Scripts/Game/Bastion/GameController.Client.cs` (`TickClientPrepCommands` — the `delta` branch and its `atTrigger` test, `prep.RequestJuice`); `Assets/Scripts/Game/Net/NetPrep.cs` (`RequestJuice`, `RequestJuiceServerRpc`, `PrepRange`); `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetJuice` — `Sim.TryJuice(delta, true)`)
- The host substitutes a range test for the look-at, and reads its OWN select lock rather than the requester's — `Assets/Scripts/Game/Bastion/GameController.NetPrep.cs` (`TryNetJuice` — `NetPrepInRangeOfTrigger`, `NetPrepInRangeOfHatch`, `_armedHatch`, `_armedTerminalId`); `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`AnyTriggerInRange`)
- Selecting arms the start and locks anting; select again unlocks — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`ToggleTerminalSelect`, `ToggleHatchSelect`, the "juice refused — intensity selected" gate)
- Which triggers offer a select: Master unconditionally, Field only while the Hatch is not the start line — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`CanSelectTerminal`, `HatchOffersWaveStart`); the Hatch's own arm `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`OffersHatchBank`)
- Locked for the whole Assault — `Assets/Scripts/Sim/Run/RunSim.cs` (`SetSiphonLevel` Prep guard); `Assets/Scripts/Sim/Run/IntensityJuice.cs` (`Try` Prep guard)
- Reset to zero after an unchained pull; not reset inside the Master sequence — `Assets/Scripts/Sim/Run/RunSim.cs` (`NotifyAssaultCleared` — the `Siphon.Level = 0` branch, and the master-sequence return above it)

## Gaps

- Nothing says the Ante is locked during an Assault. The shortcut panel simply stops showing it, which reads as a bug rather than a rule.
- Nothing warns you that starting deep from the NAV MAP means one ante before that first start governs the entire chained delivery — several Waves' worth of bodies, at a stake you chose before seeing any of them.
- The reward curve is hand-authored with uneven steps. The prompt shows the current and next values but never the shape, so "one more step" is worth a different amount at different points and you cannot see that coming.
- Salvage is floored to a whole number per kill, so part of a raised Ante is quietly rounded away on every body.
- Nothing tells you that the Ante's surplus is entirely Defaults. Without that, raising it reasonably reads as "the Wave gets harder in every way", which is the wrong model and makes the Ante look worse than it is.
