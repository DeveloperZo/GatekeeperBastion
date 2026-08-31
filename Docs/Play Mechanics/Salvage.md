<!--
Purpose: Current representation of Salvage — the only currency: where it comes from, why a kill is safe the moment it lands, what a Withdraw puts back at risk, and what competes for it.
Status: working
Update trigger: when the award path, the Withdraw, the repair charge, or the Boarding-end settlement changes.
Last updated: 2026-08-31
Where:
- Kills bank on landing, face value, Assault-only, whole into the realm's pot — Assets/Scripts/Sim/Run/RunSim.cs (AwardKill, BankKill, CurrentKillAward); Assets/Scripts/Sim/Meta/MetaState.cs (EarnSalvage); Assets/Scripts/Sim/Meta/RealmState.cs (EarnSalvage); Assets/Scripts/Sim/Run/SalvageLedger.cs (Earn)
- Convert window and trickle pay zero — RunSim.cs (CurrentKillAward returns 0 while WaveMode.HatchBank or FieldTrickleLive)
- Prep pays nothing — AwardKill refused outside Tethered + Assault
- Withdraw is the only fill of the carried pile — RunSim.cs (Withdraw); Assets/Scripts/Sim/Run/HatchBank.cs (WithdrawPerSecond, WithdrawUnits)
- Repair bills the bank — RunSim.cs (RepairHatch, Spend path through bank); Assets/Scripts/Sim/Ship/ShipSystems.cs (RepairSalvagePerHp)
- Respawn bills the bank — Assets/Scripts/Sim/Run/RunSim.Respawns.cs (TryRespawn → SpendBanked)
- Ante parked, spends nothing — Assets/Scripts/Sim/Run/IntensityJuice.cs (Ante.Enabled); IntensityJuice.Try refuses while parked
- Bank survives every ending; leftover carry wiped on Lost, rebated on Returned — RunSim.cs (EndRun); Assets/Scripts/Sim/Run/RunSim.Wallets.cs (WipeUnbankedSlot, KeepReturnedShare); Assets/Scripts/Sim/Run/RunSim.Dock.cs (DockRun)
- Shops spend the same pot — Assets/Scripts/Sim/Meta/MetaState.cs (SpendSalvage); ARMORY / FABRICATION / CREW QUARTERS
-->

# Salvage

Salvage is the only currency. A Gatekeeper killed during a live Assault pays the realm's bank at face value the moment it falls, so no ending can take those earnings back. Prep and a Convert window pay nothing. Nothing is at risk unless the Boarder Withdraws it at the Hatch; repair and respawn bill the same bank the shops do. The Ante is parked and spends nothing.

## Game

Salvage is the only currency. There is no second resource, and nothing here to collect but the pay.

The sequence is one step long. A Gatekeeper drops and the money is the realm's, that instant, at face value. It waits on no door, no window and no walk home. Prep pays nothing. Walking pays nothing. Surviving pays nothing. Stripping the place pays no bonus. Killing, during a live Wave. That is the entire payroll.

So a Boarding cannot end with nothing to show for it. I can die on the Wave that beat me and still come off the Bastion richer than I went on, because everything behind me was paid for as it fell. The Boarding that proves I am underequipped is the Boarding that buys the fix.

Nothing is at risk unless I put it there. There is a hold at the Hatch that pulls money back out of the bank and into my pocket, and a pocket is a thing a death can empty. I never have to touch it. When I do it is because I meant to bet, not because the room charged me a toll for standing in it.

Everything bills the same pot, and that is where the tension went. The repair I hold at the door. The seat I buy back off the floor. The weapon, the Tiers, the Hatch Armor, the Rig, the Decoy. The Ante is parked; it does not spend anything today.

A Boarding where the door takes a beating still costs me twice: once at the door, once at the console I walk back to and cannot afford. The bank is not a score. It is not banked so I can look at it.

## Details

### Structure

Salvage is a stock. The mechanics that move it are the earn, the spend, and the one way to put it at risk. There is no second resource and no crafting material. Everything bought is bought with this.

### Earn

By killing Gatekeepers during a live Assault, and by nothing else. A kill credits the realm's bank the moment the body falls, at face value — spendable on the Ship when the Boarding ends, however it ends. The quiet of Prep earns nothing. Walking the room earns nothing. Clearing the Bastion pays no completion bonus. A Gatekeeper killed inside a Convert window pays nothing at all: that window moves money already carried rather than earning more.

The Ante is parked, so the multiplier sits at 1. The rate: [Field Manual](Field-Manual.md).

### Spend

Repairing the Hatch charges banked Salvage as the healing lands, and will run the bank to exactly zero if held long enough ([The Hatch](The-Hatch.md) owns that spend). Standing a dead seat up is bought from the same bank, at a price that climbs with every respawn taken this Boarding ([Going Down](Going-Down.md) owns that spend). The Ante is parked and spends nothing.

On the Ship the same pot buys a weapon, weapon Tiers, Hatch Armor, the Repair Rig and the Decoy ([Upgrades](Upgrades.md) owns those purchases). CREW QUARTERS spends nothing on Default; VEIL and FORGE are claimable there too.

There is no second purse that quietly funds the mid-Boarding costs. A Boarding where the Hatch takes a beating costs twice: once in the Salvage that went into the door, and again in the purchase walked back to the Ship unable to make. There is no sell-back and no refund.

### Risk

The carried pile does not fill on its own. Look at the Hatch in Prep, with the door at full health, and hold Interact: banked Salvage flows back out into the pile being carried. That pile is what a Convert window converts, and it is the only thing a death can take. Withdraw is the only way to be carrying anything, which makes it the only way to lose anything. The game never stakes money on the Boarder's behalf.

If carried, the pile is gone on a death that ends the Boarding and gone if the Hatch falls. A Docked return keeps a small stated share of it. Lost, Docked and Escaped all keep every unit the realm earned. There is no named forfeit anywhere in the game.

### Rules

Nothing that happens on the Bastion can take banked Salvage off the Boarder. Going down does not. The Hatch falling does not. That is a rule rather than a side effect: it must be impossible to die with no progress to show for it.

Rates, costs and multipliers: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

A kill is safe the moment it lands, so a Boarding that fell apart on the Wave the Boarder was not equipped for still paid for every body put down on the way. That is what turns the Wave that beat them into the Wave that funds beating it.

Risk is exactly what was chosen: Withdraw is the only way money leaves the bank into a pile a death can empty. Repair and respawn compete with the next purchase because there is one pot. Treating the bank as a score is the wrong instinct — it is not banked so it can be looked at.

Removal test: without Salvage there is nothing to earn and nothing to spend. Different game.

### Classification

Core, loop.

### Interacts with

- [A Boarding](A-Boarding.md) — kills pay on landing into this stock; that page owns the Boarding.
- [The Hatch](The-Hatch.md) — Withdraw is the only risk, and repair bills this bank; that page owns the withdraw and repair spends.
- [Going Down](Going-Down.md) — respawn bills this bank; that page owns the respawn spend.
- [The Ante](Ante.md) — parked, spends nothing; that page owns the ante spend when it returns.
- [Upgrades](Upgrades.md) — shops spend this bank; that page owns those purchases.
- [Realms](Realms.md) — the pot is realm-side; that page owns the one pot per realm.
- [The Other Currencies](Experience-And-Cores.md) — the two that will join this one.

### Gaps

- The HUD still shows Unbanked and Banked separately, and Unbanked now reads 0 for most of a Boarding. Nothing says that line only moves if you Withdraw.
- Nothing at the Hatch explains what a Withdraw is *for*, or that the pile it creates is the only thing a death can take.
- It never tells you that a Gatekeeper killed outside a live Wave pays nothing, or that one killed inside a Convert window pays nothing either.
- Every station panel shows lifetime earned and lifetime spent. Neither is explained anywhere, and nothing says that lifetime earned is the figure the weapon Tiers and the equipment slot are waiting on.
- If a save is old or damaged, those lifetime figures are silently rebuilt from what you own. Your bank is always the truth; the lifetime lines may be a reconstruction.
- There is no way to sell anything back or refund a purchase, and nothing tells you that before you spend.
- The Ante is parked. A repair and a respawn still bill the same bank as the consoles do, and no prompt says so. Each one reads as its own small charge rather than as money off the next purchase.
