<!-- Purpose: the shape of one Run end to end — embarking, Prep and Assault, starting a Wave at a field terminal, Master Control, and the two ways it can finish. / Status: working / Update trigger: when the Run's phases, the draw model, the deep-start option, or the Run-enders change. / Last updated: 2026-08-15 -->

# A Run

## Game

A Run is one trip out to the Bastion and back. You embark already tethered near the Hatch. Nothing happens until you start a Wave.

Prep is quiet and untimed. Assault lasts until every Gatekeeper that Wave sent is dead. A field-terminal Wave hunts you; a Hatch-bank hunts the Hatch and converts Unbanked.

Three endings: you die, the Hatch falls, or you survive Master Control. Converted Salvage stays either way. Unbanked is wiped on Lost. How many Waves: [Field Manual](Field-Manual.md).

## Reference

A Run is one trip out to the Bastion and back. It starts on your Ship, it ends with you Lost or with Master Control's Hatch-bank survived, and either way only Salvage that made it through a Hatch-bank is yours.

### Before you leave: the Ship

You walk your own Ship between runs. Four consoles, each owning one subject: **ARMORY** (weapons and their Tiers), **CREW QUARTERS** (your own attributes), **FABRICATION** (the Hatch and the repair rig), and **NAV MAP** (where you go, and the button that commits you).

Whatever you are carrying when you press EMBARK is what you have for the whole Run. There is no way to change your weapons once you are aboard the Bastion — the fight-time weapon keys only switch between the slots you already carry.

The NAV MAP will let you embark with no weapon at all. It warns you and then lets you go, because you are allowed to make bad bets.

### Boarding

There is no landing sequence. You embark, the Bastion scene loads, and you are already there, already tethered, standing near the Hatch with the Run live. The Bastion is quiet.

It stays quiet. Nothing is coming, nothing is counting down, and nothing will happen at all until you decide it should.

### Prep and Assault

Once aboard, the Run alternates between two states, and it will keep alternating until the Run ends.

**Prep** is the quiet. No Gatekeepers exist, no Salvage is coming in, and the Run clock is the only thing moving. This is the only time you can juice intensity, and it is the only time you can repair the Hatch without something chewing on it. Prep has no time limit whatsoever — you can stand in it as long as you like. It also pays nothing, so standing in it forever is not a strategy, just a rest.

**Assault** starts when you use a field terminal or Hatch-bank at the Hatch, and lasts until every Gatekeeper that Wave sent is dead. Intensity locks the moment it starts. Kills pay Unbanked only while an Assault is live.

### Starting a Wave

A Wave starts when you look at a field terminal in Prep. Juice if you want, then select — including intensity 0 — then start **one** Wave. There is no abort once Assault is live: the only way out is killing everything in it. Bindings: [Field Manual](Field-Manual.md).

Juice while looking at the Hatch; select, then start a Hatch-bank. Start without a select does not start a Wave.

A field terminal takes a fixed number of Waves to complete — the first one on the kit bastion takes the Run's full five. Each start after a select delivers one. After it clears, Prep lasts as long as you want: Hatch-bank, or walk back to the terminal and pull the next. When the last of those clears, the terminal is done and its door opens.

See the [Field Manual](Field-Manual.md) for how many Waves a Run holds.

### Clearing a Wave

Gatekeepers arrive in groups rather than a stream. Each group comes through one spawn point together; the next group uses the next point in the rotation, so pressure moves around the room rather than pouring from one door.

The Wave is clear when the last Gatekeeper of it is dead **and** the Bastion has finished sending them. Not when the last group spawns — when the last body drops. A single Gatekeeper still alive somewhere keeps you in Assault, and nothing despawns on its own mid-run.

When it clears you drop back to Prep with no timer. That is when a Hatch-bank is offered — Prep names it if you have Unbanked. Intensity unlocks. Clearing the last field Wave of that terminal opens its door and leaves Unbanked in Prep. It does not end the Run.

A Wave started at a field terminal hunts **you**. The Hatch cannot be hurt during that fight. Sitting at the Hatch earns nothing until you Hatch-bank.

A Hatch-bank Wave hunts the Hatch. It is one step heavier than the last field Wave. Defaults and Thieves go for the door; Hunters still hunt you. Unbanked drips into Banked while the timer runs. Killing the Wave early, or Unbanked hitting 0, does not end it. The timer dumps whatever is left. A hatch hit also eats Unbanked.

### What ends a Run

Exactly three things end a Run, and only three.

**You die.** Your health does not come back during a Run — there is no healing, no regeneration, no pickup, no respawn. What you take, you keep until the Run ends. When it runs out you go down, and the Run is over — for solo, and for the host. If a second Boarder is in the Bastion, their down does not end the Run.

**The Hatch falls.** The Hatch is the umbilical junction back to your Ship. When its health reaches zero the Run ends on the spot, whatever else was happening, and it cannot be repaired back from zero.

Both of those are **Lost**. The screen names which one happened, and that the Boarder was thrown off the Bastion.

**You survive Master Control.** Juice once at the console — it covers the sequence — then select and start. The door seals; five Boarder-hunt Waves hit that room. The door opens; five Hatch-bank windows start. Leftover converts. That is **Escaped** — the screen reads BASTION STRIPPED. Clearing Wave 5 does not. Hatch-bank after the field is stripped converts and leaves you in Prep; the door is already open.

### What the endings actually mean

Less than you would expect. Converted Salvage stays on the ship. Unbanked is wiped if you go down or the Hatch falls. Wave 5 does not wipe it. Master Control's Hatch-bank converts leftover, then the win. Clearing adds no bonus. The two endings differ in three things:

- what the screen says;
- which lifetime counter ticks — runs lost, or runs cleared;
- whether your deepest Wave rises. It rises **only on a clear**, never on reaching a Wave and dying in it.

That last one is the only mechanical consequence of failing, and it only matters because of the option below.

Both end screens are identical in shape: converted this Run, repair spent, net, what you have banked now, your deepest Wave, and one button back to the Ship. There is no other way off either screen.

### Starting deeper: the NAV MAP start-Wave selector

The NAV MAP has a stepper that picks which Wave to start at. Its ceiling is the deepest Wave you have **cleared**, so it only opens up by winning Waves, not by dying in them.

This is not a skip. Starting deeper means the earlier Waves are **delivered back to back at the start of the Run**, as one long Assault with no Prep between them and a short fixed gap after each one finishes arriving. They overlap in the room. That overlap is the entire point of the option: you fight the same content compressed, with several Waves' worth of Gatekeepers alive at once, instead of one Wave at a time with quiet in between.

Concretely, if you start deep:

- it is a single Assault, so intensity is locked for the whole chain;
- there is no quiet in the middle of it — no repositioning without pressure;
- the earlier Waves keep their own composition and toughness; a first Wave delivered inside a chain is still a first Wave;
- once the chain clears, the Run continues from the next field terminal, one Wave at a time.

A condensed run, then. Same Waves, same total bodies, much less room.

## See also

[Waves](Waves.md). [Intensity](Intensity.md). [The Hatch](The-Hatch.md). [Salvage](Salvage.md). [The Ship](The-Ship.md).

## Gaps

- **Your health never returns during a Run.** Nothing in the game says this. There is no heal, no regen, no item, and the only refill happens at run start.
- **Repairing the Hatch spends banked Salvage** — the same purse you shop with at the consoles. The prompt quotes the rate, but nothing tells you that the money is coming out of your upgrade budget, or that with an empty bank you cannot repair at all.
- **Repairing needs the Repair Rig equipped.** Without it the key does nothing however much you have banked, and nothing in the Run says why.
- Nothing states that there is **no way to leave a Run early**. Players look for a bail-out; there isn't one, and no screen says so.
- Nothing states that intensity is **locked** during an Assault. The panel simply stops listing it.
- The start-Wave ceiling is your deepest **cleared** Wave, not deepest reached. The NAV MAP shows the number without saying which of those it is.
- During a deep-start chain the game holds spawns back when too many Gatekeepers are alive at once. You never see this; a chain can feel like it stalled when it is actually throttling itself.
- The Wave-cleared test requires **every** spawned Gatekeeper dead. If one is stuck or lost somewhere in the Bastion, you stay in Assault with nothing visibly happening and no indication of where it is.
- Salvage per kill is floored into Unbanked; converted drip/dump is what the Run tally shows.
- Each Run uses a seed you cannot see or choose.
- The Drone slot appears on the weapon strip and is always empty. Nothing says no payload exists yet.
