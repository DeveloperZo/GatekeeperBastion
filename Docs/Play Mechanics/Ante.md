<!--
Purpose: Current representation of the Ante — the per-Wave wager — as parked in the build.
Status: parked
Update trigger: any milestone that restores or retires this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Parked 2026-08-30 (Zo); Waves run at dial 0 — Docs/Decisions.md (Parked — The Ante); Assets/Scripts/Sim/Run/IntensityJuice.cs (Ante.Enabled, static readonly false)
- Per-run copy of the flag — Assets/Scripts/Sim/Run/RunSim.cs (AnteEnabled, copied from Ante.Enabled); spend-path tests set RunSim.AnteEnabled
- Spend path — Assets/Scripts/Sim/Run/IntensityJuice.cs (Try, CostPerStep, refuses while parked); RunSim.SpendBanked
- Dial and curve — Assets/Scripts/Sim/Meta/TetherSiphon.cs (Level 0..5, Curve, RewardMultiplierFor)
- Look-at gate — IntensityJuice.Try (atTrigger); Game layer supplies it
- Guest ante goes host-side into the same pot — Assets/Scripts/Game/Bastion/GameController.Client.cs (TickClientPrepCommands → NetPrep.RequestJuice); Assets/Scripts/Game/Bastion/GameController.NetPrep.cs (TryNetJuice)
- Select lock where a select exists — Hatch Convert, Master terminal, retrigger offer — GameController.Terminals.cs; GameController.HatchBank.cs; GameController.MasterControl.cs
- Prompts / HUD / shortcut strip gate on Ante.Enabled — Assets/Scripts/Game/Bastion/RepairInteractable.cs; Assets/Scripts/Game/Bastion/BastionTerminal.cs; Assets/Scripts/Game/Bastion/HudController.Fight.cs; Assets/Scripts/Game/Input/ShortcutPanel.cs
- One ante governs a deep-start chain, a zone composite, and the whole Master Control sequence — Assets/Scripts/Sim/Run/RunSim.cs (NotifyAssaultCleared reset); RunSim.Master.cs; RunSim.Faucet.cs (FaucetOwnsClear)
-->

# The Ante

The Ante is parked: the dial, the bank spend, the select lock and the Ante readout are off, and Waves run at intensity 0. When it is on, it is a wager the Boarder sets in Prep, standing at the Hatch or a terminal, before starting a Wave. Raising it spends Salvage out of the realm's bank; every Gatekeeper of that Wave pays more and the Wave sends more of them, always the plainest kind. Starting the Wave locks the dial for the whole Assault, lowering it is free and refunds nothing, and it resets to zero once the Assault is over.

## Game

The Ante is a wager, placed per Wave, before I commit. It is the one dial on this machine that is mine.

Raising it multiplies what each body pays and how many bodies arrive. Both halves read off the same table and cannot come apart. It is the cleanest mechanism on the Bastion and I do not fully understand why it was built this honest.

It toughens nothing. It tightens nothing. Every extra body is a Default — never a Hunter, never a Thief. A raised Wave is the same fight with more of the cheapest thing in it.

Set it in Prep, at the trigger, before the select. Once Assault begins it is locked. Turning it down is free and refunds nothing.

It costs money I already own. Every step comes out of the bank — the same bank the weapons come out of, the same bank the door and the dead seats come out of. That is not a stake skimmed off something that was never mine; it is take-home progress, spent forward on a Wave I have not fought yet.

Three lines of the ledger before I raise it. Can I kill the surplus before it reaches me. What is my health afterwards, given it does not come back while I stand. And is the door already hurt — because the repair, the raise and the extra pay all meet in one pot. The Ante pays gross. The Hatch charges net.

Start deep and one ante governs the whole chain. I learned that at full price.

## Details

### Structure

Parked. The dial is off and Waves run at intensity 0. The spend path and curve stay in the tree behind a flag, deliberately off for players ([Decisions](../Decisions.md)). What follows is the mechanic as it was when parked.

### Dial

When enabled, the Boarder invokes it in Prep only, while looking at a trigger — the Hatch, a field terminal, or Master Control. `+` antes up, `-` antes down. The dial is clamped; cost per step and the curve: [Field Manual](Field-Manual.md).

Where a select exists, selecting arms the start and locks anting up; selecting again unlocks it. Three do: the Hatch (arming a Convert), the Master terminal, and a terminal holding a retrigger offer. The ordinary Hatch zone start needs no select — `T` starts the zone at whatever the dial reads.

Starting a Wave locks the dial for the whole Assault. When the Assault ends with nothing chained behind it, the dial resets to zero. One ante governs a whole deep-start chain and a zone's whole composite span, and one ante rides the whole Master Control sequence.

Both halves read one table. The reward multiplier per dial step is a hand-authored curve; body count reads the same table — surplus bodies are Defaults only.

### Spend

This page owns the ante spend: each up step charges banked Salvage — the realm's pot, the same purse the consoles, the repair, and the respawn fee charge — and a step the bank cannot cover is refused with the bank and the cost named in the log. Anting down is free and refunds nothing.

Every seat may ante, and every seat spends the same pot. A guest's `+`/`-` does not move its own machine's dial: the host applies it through the same spend, the realm's one pot, and the raised Wave the whole crew then fights.

### Rules

Parked: no prompt, no HUD readout, no shortcut-strip row, no spend. Waves run at dial 0.

When enabled: Prep only, at a trigger, refused mid-Assault, refused if the bank cannot cover the step. This page owns the ante spend.

## Additional Details

### Consequences

When enabled, the Ante is the one dial that sets how many bodies arrive and what each pays, paid from take-home progress before the Wave is fought. Extra bodies are Defaults only, so a raised Wave is the same fight with more in it. One ante governs a whole chain, which is the cost of starting deep.

Parked, those terms sit at zero and the spend never fires.

Removal test (when enabled): without it there is no per-Wave wager — pace remains, stake does not. Different game. Parked, deleting the spend path would change nothing a player can observe.

### Classification

Core, loop (when enabled). Serves Expression (`../Design/1-Experience.md` §1.2).

### Interacts with

- [Waves](Waves.md) — the dial multiplies body count only, surplus Defaults; that page owns delivery.
- [Salvage](Salvage.md) — the spend reads the realm's bank; that page is the stock.
- [The Hatch](The-Hatch.md) — selecting a Convert locks anting up; that page owns the Convert window and the withdraw and repair spends.
- [Going Down](Going-Down.md) — anting up spends the same bank the way back is bought from; that page owns the respawn spend.
- [Master Control](Master-Control.md) — one ante rides the whole sequence; that page owns the sequence.
- [Retriggering](Retriggering.md) — while a retrigger is armed the dial elects instead of anting up; that page owns the election.
