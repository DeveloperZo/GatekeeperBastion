<!-- Purpose: the shape of one Boarding end to end — embarking, Prep and Assault, starting a Wave at the Hatch and ending it at a terminal, Master Control, Docked return, and the ways it can finish. / Status: working / Update trigger: when the Boarding's phases, the draw model, the deep-start option, or the Boarding-enders change. / Last updated: 2026-08-25 (kills bank as they land; the Convert leaves the demo) -->

# A Boarding

## Game

One trip out and back. Everything in it happens in an order I choose and cannot skip.

I embark already tethered. Nothing happens until I start a Wave, and I start it at the Hatch. Prep is the quiet: untimed, unpaid, and the only state where I can ante up or work the door without something chewing on me. Assault runs until I reach the zone's terminal and shut it off, or until the last body the Wave sent has dropped — whichever I get to first.

A field Wave hunts me and cannot touch the Hatch. A Convert hunts the Hatch and converts what I am carrying. Two different fights, one room.

Four ways it finishes, and I keep them in this order. I go down and stay down — a dead seat stands back up out of the realm's bank, while the bank covers the climbing price. The Hatch goes down, which nothing pays for. I dock home after a survived Convert. Or I outlast Master Control and the Bastion is stripped.

Every body I put down survives all four. The kills were banked as they fell, so the ending only decides what the screen calls it.

The Ship offers to start me deeper. It is not a skip. It is the same content delivered back to back, one Assault, no quiet in the middle. Same Waves. Much less room.

## Reference

A Boarding is one trip out to the Bastion and back. It starts on your Ship, it ends Lost, Docked, or Escaped, and every unit of Salvage it earned is yours in all three cases — a kill banks the moment the body falls.

### Before you leave: the Ship

You walk your own Ship between Boardings. Four consoles, each owning one subject: **ARMORY** (weapons and their Tiers), **CREW QUARTERS** (the protocol you embark wearing), **FABRICATION** (the Hatch, the Repair Rig, and the Decoy), and **NAV MAP** (where you go, and the button that commits you). On a friends session the Ship is shared; only the party lead's EMBARK commits the party.

Whatever you are carrying when you press EMBARK is what you have for the whole Boarding. There is no way to change your weapons once you are aboard the Bastion — the fight-time weapon keys only switch between the slots you already carry.

The NAV MAP will let you embark with no weapon at all. It warns you and then lets you go, because you are allowed to make bad bets.

### Boarding

There is no landing sequence. You embark, the Bastion scene loads, and you are already there, already tethered, standing near the Hatch with the Boarding live. The Bastion is quiet.

It stays quiet. Nothing is coming, nothing is counting down, and nothing will happen at all until you decide it should.

### Prep and Assault

Once aboard, the Boarding alternates between two states, and it will keep alternating until the Boarding ends.

**Prep** is the quiet. No Gatekeepers exist, no Salvage is coming in, and the Boarding clock is the only thing moving. This is the only time you can ante up, and it is the only time you can repair the Hatch without something chewing on it. Prep has no time limit whatsoever — you can stand in it as long as you like. It also pays nothing, so standing in it forever is not a strategy, just a rest.

**Assault** starts when you press `T` at the Hatch, or when you Convert there, and lasts until every Gatekeeper that Wave sent is dead — or, on a field Wave, until you reach the zone's terminal and shut it off, which completes the zone and banks what you are carrying. The Ante locks the moment it starts. Kills credit the bank only while an Assault is live. A Convert window has no off switch: the ante is why.

### Starting a Wave, and ending it

**A Wave begins at the Hatch.** Look at it in Prep, ante up if you want, and press `T`. While the zone you are in still has a terminal to finish, that is the only place a Wave can be started — the zone's field terminals say so on their own prompts and refuse the key. Bindings: [Field Manual](Field-Manual.md).

**A Wave ends at a terminal.** Press `T` at the zone's field terminal while the Wave is live and it shuts down: the Wave stops, the zone completes, and anything you are carrying banks there. Killing the last body ends it too. What there is not is an abort — both exits are at the far end of the fight.

That shape is the point. The distance between the door and the terminal is the Wave, and the walk down it with everything the Bastion sent is what a Boarding actually is.

Each field terminal is authored with the number of Waves it takes to complete, and the terminals on the kit bastion do not all take the same number: the early ones deliver one Wave per start, and the deepest one delivers its Waves back to back off a single start, with no Prep in between. Which terminal takes what: [Field Manual](Field-Manual.md). After a clear, Prep lasts as long as you want. Convert is refused until every field terminal standing in the zone you are in has been completed. Once they are, the Hatch stops being the start line: it offers **Withdraw** while you are carrying nothing, and a Convert on whatever you withdrew.

See the [Field Manual](Field-Manual.md) for how many Waves a Boarding holds.

### Clearing a Wave

Gatekeepers arrive in groups rather than a stream. Each group walks out of a spawn point — a room the Boarder cannot open — together; the next group uses the next point in the rotation, so pressure moves around the room rather than pouring from one opening.

The Wave is clear when the last Gatekeeper of it is dead **and** the Bastion has finished sending them. Not when the last group spawns — when the last body drops. A single Gatekeeper still alive somewhere keeps you in Assault, and nothing despawns on its own mid-Boarding.

When it ends you drop back to Prep with no timer. The Ante unlocks. Completing that terminal does not end the Boarding.

A field Wave hunts **you**. The Hatch cannot be hurt during that fight, and standing next to it earns nothing — only killing pays, and killing pays instantly wherever you do it.

A Convert Wave hunts the Hatch. It is one step heavier than the last field Wave. Defaults, Thieves and Cloakers go for the door; Hunters still hunt you. What you are carrying drips into the bank while the window runs. Killing the Wave early, or the carried pile hitting 0, does not end it. The timer converts whatever is left. A hatch hit eats the carried pile too. There is no cap on how many Converts a Boarding may run.

### What ends a Boarding

Four things end a Boarding.

**You die and stay dead.** Your health does not come back while you stand — there is no healing, no regeneration, no pickup. What you take, you keep until you go down. Going down offers a **priced respawn** paid out of the realm's bank, at a price that climbs with every respawn this Boarding, whoever took it. Solo and host wait behind a DOWN overlay that names the price and the bank; a guest waits on the Ship. A bank short of the price and, if nobody else still fights, the Boarding is over. The whole rule: [Going Down](Going-Down.md).

**The Hatch falls.** The Hatch is the umbilical junction back to your Ship. When its health reaches zero the Boarding ends on the spot, whatever else was happening, and it cannot be repaired back from zero.

Both of those are **Lost**. The screen names which one happened, and that the Boarder was thrown off the Bastion.

**You dock.** Survive a Convert and, until you start the next Wave, hold interact at the Hatch to return to the Ship. The Boarding ends **Docked**, not Lost — banked Salvage stays, and the door that window opened stays open when you come back. Fail the bank and that prompt never appears. Trigger the next Wave and it is gone.

**You survive Master Control.** Ante up once at the console — it covers the whole sequence — then select and start. The door seals and a run of Boarder-hunt Waves hits that room. Then the door opens and one last Convert window starts, and what walks into it is a single Thief built heavier than anything the field sends. Survive that and the leftover converts. That is **Escaped** — the screen reads BASTION STRIPPED. Clearing the last field Wave does not. How many Waves in each half: [Field Manual](Field-Manual.md). Convert after the field is stripped converts and leaves you in Prep; the door is already open.

### What the endings actually mean

Less than you would expect, and less than it used to be. Every kill is already banked, so no ending can take a Boarding's earnings away from you; the only thing an ending settles is a pile you deliberately withdrew, which is wiped on a death or a breach and kept in small part on a Docked return. Docked does not tick a loss. Clearing adds no bonus. Lost and Escaped differ in three things:

- what the screen says;
- which lifetime counter ticks — Boardings lost, or Boardings cleared;
- whether your deepest Wave rises. It rises **only on a clear**, never on reaching a Wave and dying in it.

That last one is the only mechanical consequence of failing, and it only matters because of the option below.

Lost, Docked, and Escaped end screens are the same shape: converted this Boarding, repair spent, net, what you have banked now, your deepest Wave, and one button back to the Ship. There is no other way off either screen.

### Starting deeper: the NAV MAP start-Wave selector

The NAV MAP has a stepper that picks which Wave to start at. Its ceiling is the deepest Wave you have **cleared**, so it only opens up by winning Waves, not by dying in them.

This is not a skip. Starting deeper means the earlier Waves are **delivered back to back at the start of the Boarding**, as one long Assault with no Prep between them and a short fixed gap after each one finishes arriving. They overlap in the room. That overlap is the entire point of the option: you fight the same content compressed, with several Waves' worth of Gatekeepers alive at once, instead of one Wave at a time with quiet in between.

Concretely, if you start deep:

- it is a single Assault, so the Ante is locked for the whole chain;
- there is no quiet in the middle of it — no repositioning without pressure;
- the earlier Waves keep their own composition and toughness; a first Wave delivered inside a chain is still a first Wave;
- once the chain clears, the Boarding continues from the next field terminal, one Wave at a time.

A condensed Boarding, then. Same Waves, same total bodies, much less room.

## See also

[Waves](Waves.md). [Ante](Ante.md). [The Hatch](The-Hatch.md). [Salvage](Salvage.md). [Going Down](Going-Down.md). [The Ship](The-Ship.md).

## Gaps

- **Your health never returns while you stand.** Nothing in the game says this. There is no heal, no regen, no item; the only refills are Boarding start and a paid respawn.
- **Repairing the Hatch spends banked Salvage** — the same purse you shop with at the consoles. The prompt quotes the rate, but nothing tells you that the money is coming out of your upgrade budget, or that with an empty bank you cannot repair at all.
- **Repairing needs the Repair Rig equipped.** Without it the key does nothing however much you have banked, and nothing in the Boarding says why.
- Nothing states that there is **no leave-anytime bail-out**. Outside a survived Convert window, down and breach remain the only early enders; the dock prompt is that window only.
- Nothing states that the Ante is **locked** during an Assault. The panel simply stops listing it.
- The start-Wave ceiling is your deepest **cleared** Wave, not deepest reached. The NAV MAP shows the number without saying which of those it is.
- During a deep-start chain the game holds spawns back when too many Gatekeepers are alive at once. You never see this; a chain can feel like it stalled when it is actually throttling itself.
- The Wave-cleared test requires **every** spawned Gatekeeper dead. If one is stuck or lost somewhere in the Bastion, you stay in Assault with nothing visibly happening and no indication of where it is — reaching the terminal is the way out of that, and nothing says so at the time.
- Salvage per kill is floored to a whole number, so part of a raised Ante is rounded away on every body.
- The end screen still labels the Boarding's earnings "converted this boarding", which is the old name for a step that no longer exists. The figure itself is right: everything you killed for.
- Each Boarding uses a seed you cannot see or choose.
- The Drone slot appears on the weapon strip. FABRICATION sells the Decoy that fills it; until then the slot looks like a failed swap.
