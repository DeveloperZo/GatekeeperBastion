<!--
Purpose: Current representation of the Ante — the per-Wave wager — as parked in the build.
Status: parked
Update trigger: any milestone that restores or retires this mechanic, in the same change.
Last updated: 2026-08-30
-->

# The Ante

**Parked 2026-08-30 (Zo).** The dial, the bank spend, the select lock, and the Ante readout
are off. Waves run at intensity 0. Flip `Ante.Enabled` (`static readonly`, copied onto each
`RunSim.AnteEnabled`) in `Assets/Scripts/Sim/Run/IntensityJuice.cs` when this returns;
spend-path tests set `RunSim.AnteEnabled`. The spend path and curve stay.

What follows is the mechanic as it was when parked — kept so restoring it does not require
re-deriving the rules from code archaeology.

---

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

**Every seat may ante, and every seat spends the same pot.** A guest's `+`/`-` does not move its own machine's dial: `TickClientPrepCommands` sends `NetPrep.RequestJuice`, and the host applies it through `TryNetJuice` → `Sim.TryJuice` — the same `IntensityJuice.Try`, the same `SpendBanked`, the realm's one pot, and the raised Wave the whole crew then fights.

Where a select exists, `E`-selecting arms the start and locks anting up — "juice refused — intensity selected" — and selecting again unlocks it. Three do: the Hatch (arming a Convert), the Master terminal, and a terminal holding a retrigger offer. The ordinary Hatch zone start needs no select — `T` starts the zone at whatever the dial reads.

Starting a Wave locks the dial for the whole Assault. When the Assault ends with nothing chained behind it, the dial resets to zero. One ante governs a whole deep-start chain and a zone's whole composite span, and one ante rides the whole Master Control sequence.

**Both halves read one table.** The reward multiplier per dial step is a hand-authored curve in `TetherSiphon.Curve`; body count reads the same table — surplus bodies are Defaults only.

## Classification

Core, loop (when enabled). Serves Expression (`../Design/1-Experience.md` §1.2).

## Where

- Feature flag — `Assets/Scripts/Sim/Run/IntensityJuice.cs` (`Ante.Enabled`)
- Spend path — `Assets/Scripts/Sim/Run/IntensityJuice.cs` (`Try`, refuses while parked)
- Dial and curve — `Assets/Scripts/Sim/Meta/TetherSiphon.cs`
- Prompts / HUD / shortcut strip gate on `Ante.Enabled`
