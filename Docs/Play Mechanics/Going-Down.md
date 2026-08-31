<!--
Purpose: Current representation of going down and the priced respawn — the purse, the climbing capped price, the bleed-out clock, and the DOWN surfaces — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- The rule: base price times a capped step, hard respawn cap, bleed-out seconds, named refusals reading the bank; the step cap sits above what the respawn cap lets the climb reach — Assets/Scripts/Sim/Run/Respawns.cs (CostFor, BaseCost, MaxSteps, MaxRespawns, BleedOutSeconds, Refuse)
- The spend: adjudicate, debit the realm's bank, count it; the carry survives — Assets/Scripts/Sim/Run/RunSim.Respawns.cs (TryRespawn → SpendBanked; RespawnRefuse passing BankedSalvage; NextRespawnCost; SeedRespawns)
- The bank behind SpendBanked — Assets/Scripts/Sim/Run/RunSim.cs (SpendBanked, BankedSalvage); Assets/Scripts/Sim/Meta/MetaState.cs (SpendSalvage → RealmState.BankedSalvage)
- Freeze while down; the Boarding-owned bleed clock; expiry reported, not enforced — Assets/Scripts/Sim/Run/RunSim.Respawns.cs (SetSeatDown, TickBleedOut, BleedOutRemaining, BleedOutExpired)
- A death with no respawn wipes that seat's carry only — Assets/Scripts/Sim/Run/RunSim.Wallets.cs (WipeUnbankedSlot)
- Host/solo DOWN flow: hold the Boarding open while a respawn can be honored, confirm buys back, bled-out ends it — Assets/Scripts/Game/Bastion/GameController.Down.cs (PlayerDied, TryEnterLocalDown, TryLocalRespawn, TickLocalDown, AnyRemoteSeatFighting)
- The two end-tests differ: death consults only remote fighters; the dead-room sweep also consults a waiting seat's purse — Assets/Scripts/Game/Bastion/GameController.Down.cs (TryEnterLocalDown against NotifySeatWentDown, AnyWaitingSeatCanPay)
- The DOWN line names price, purse and countdown — Assets/Scripts/Game/Bastion/HudController.Fight.cs (DownText, BleedClock)
- Guest gate: waiting seat pays the same rule; FOLLOW instead for a never-embarked seat — Assets/Scripts/Game/Bastion/GameController.Respawn.cs (TryNetRespawn)
- Guest wait surface rows on the NAV MAP — Assets/Scripts/Game/Ship/MapStation.cs (BindWaitRejoin, TryWaitRejoin)
-->

# Going Down

Going Down is what happens when a Boarder's health reaches zero: the seat drops, a bleed-out clock starts, and the realm can buy that seat back on the spot. The price comes out of the realm's banked Salvage — the same bank that buys weapons and repairs the Hatch — and it climbs with every respawn bought this Boarding, whoever it was bought for. A Boarding may buy only so many respawns in total; past that cap, or with a bank too short to pay and nobody left standing, the Boarding ends. The Hatch is the exception: when the door falls there is nothing to buy at any price.

## Game

Going down is a bill, and the realm pays it or the Boarding ends.

The purse is the bank — the same bank the guns come out of. Everything I killed for is already in it, so the way back up is bought with progress I had genuinely made, not with a stake that was never mine. It is the plainest possible statement of what a death costs: this many upgrades, not bought.

The price climbs with every respawn bought this Boarding, whoever it was bought for. The counter is the Boarding's, not mine. One tab, everybody drinking.

Which is the real trade, and it runs the other way from what I expected. I am never broke because I banked; I am broke because I spent. The Tier I bought on the way out, the repair I held too long at the door — every one of them is a death I can no longer afford, and none of them looked like that at the time.

Down, I wait behind the vignette with the offer written on it, priced against the bank. A guest waits on the Ship with the same offer. If the bank is short and nobody is left standing, there is no offer. That is the whole difference between a setback and a loss.

The Hatch is the exception to everything above. When the door falls there is nothing to buy. The Boarding is over, whatever the realm has banked.

## Details

### Structure

A dead seat invokes it — host and solo at the DOWN overlay, a guest from the Ship wait — against the Boarding's respawn counter and the realm's bank. Health climbs back on its own while standing ([Regen](Regen.md)), but only up to full and only slowly enough that a bad Wave still lands the Boarder here; the purchase is the only way back from zero. This page owns the respawn spend: the sim adjudicates, then debits the realm's bank. The fee leaves the seat's carried pile whole.

### Price

The next respawn costs a base price times the step, where the counter is respawns taken this Boarding, all seats together, and the climb stops at an authored step cap. Harder than the price: a Boarding may buy only so many respawns in total — the death after that is refused "out of respawns" whatever the bank holds.

The two caps do not both bite: with the respawn cap where it is, only the first steps of the climb are ever charged, so the step cap is unreachable in play — it is kept as the shape of the curve, so raising the respawn cap cannot also silently flatten the price. The figures: [Field Manual](Field-Manual.md). The counter resets with the Boarding.

### Bleed-out

Going down starts a countdown; the HUD draws it. The clock belongs to the Boarding, not the seat — a party dropping one at a time cannot refill it by taking turns. The sim reports expiry but does not act on it: whether the Boarding ends depends on whether a crewmate still fights, which is the Game layer's fact. Expired with nobody else standing, the seat's carry is wiped and the Boarding ends Lost.

### Rules

While a seat is down its carried pile is frozen — it is not the purse, but the crew's next Convert must not drain a pile whose owner cannot act. Solo and host wait behind the DOWN overlay naming the price and, when it refuses, the named reason. Confirm buys the return: the body is reset to its spawn and healed to full; the carry stands up untouched.

A guest waits on the Ship; the NAV MAP offers a priced RESPAWN for a seat that died (the same spend, same bank) and a free FOLLOW for a seat that never embarked — that seat never died and pays nothing ([The Crew](The-Crew.md)).

Two different branches decide when there is no offer, and they do not test the same thing:

- At the moment of death the Boarding holds open if the respawn can be honored or any remote seat is still fighting; if neither, that seat's carry is wiped and the Boarding ends Lost on the spot — a seat waiting on the Ship with money is not consulted here.
- The dead-room sweep runs when another seat drops while this one is already down: it ends the Boarding only when the refusal stands, nobody remote is fighting, and no waiting seat can pay.

Either way it is final by arithmetic, not by a special rule — piles freeze while down, so affordability cannot change on its own. A respawn never revives the Hatch (the breach is the loss nothing buys back — [The Hatch](The-Hatch.md)) and never changes the live Wave.

Driven by Confirm on the DOWN overlay (host/solo) and the NAV MAP RESPAWN row (guest). Shown by the DOWN vignette with the price, the bank and the countdown, and by the Ship wait surface. Reads back as the respawn-taken log line, the bank falling by the fee, and the body standing at the spawn healed.

Bindings: [Field Manual](Field-Manual.md).

## Additional Details

### Consequences

Dying is priced against what the Boarding has actually earned: kills bank as they land, so every body put down raises the affordability of the way back — and every spend (a Tier, a repair hold) is a death that may no longer be affordable, because there is one pot.

The shared counter and the hard cap make the crew's deaths one tab: the second respawn is dearer because of the first, whoever took it, and past the cap no bank buys another. The bleed-out clock turns buying back into a decision under pressure rather than a pause.

Removal test: without the priced respawn every death ends the Boarding outright and a death costs nothing but the Boarding — the entire "how mortal can we afford to be" ledger disappears.

### Classification

Primary, loop. Serves Fellowship (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

### Interacts with

- [A Boarding](A-Boarding.md) — an unbought death is one of the four enders; that page owns kills-pay-on-landing, which fills the purse.
- [Realms](Realms.md) — the fee debits the realm's pot, and a guest's return spends the realm it is playing in; that page owns the one pot per realm.
- [The Hatch](The-Hatch.md) — the breach is the loss no respawn touches, and a down seat's frozen carry is what its Convert may not drain; that page owns the withdraw and repair spends, and the repair charges the same bank.
- [The Ante](Ante.md) — anting up spends the same bank the way back is bought from; that page owns the ante spend.
- [The Crew](The-Crew.md) — the shared counter, the guest wait, FOLLOW for a seat that never embarked, and a standing crewmate holding the Boarding open; that page owns the session and its seats.
- [Regen](Regen.md) — what climbs a hurt Boarder back toward full, and why it does not save one that reaches zero; that page owns the regeneration rule.
- [Staying Alive](Staying-Alive.md) — not dying in the first place.

### Gaps

- Nothing teaches that the price counter is shared across the crew — the first expensive respawn reads as an error rather than a bill run up by earlier deaths.
- That the fee comes out of the bank is stated only on the DOWN overlay, and only at the moment you are reading it under pressure. Nothing at a console warns that the purchase you are about to make is also the respawn you might need.
- The refusal a short bank produces still reads as though it were about a carried pile rather than the bank, which is the wrong purse to go looking at.
