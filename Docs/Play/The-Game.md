# Gatekeeper Bastion

<!--
Purpose: the game explained to someone who has never seen it — what it is, what a Run
feels like, and why it is built the way it is. Hand-written prose; no tables of numbers.
Every number, key, weapon and Wave lives in Field-Manual.md, which is generated.
Status: working
Update trigger: when the loop, the wager, or the Run-ender changes.
Last updated: 2026-08-09
-->

A first-person Wave-defense game about boarding a place someone else already holds.

You are the **Boarder**. Your Ship cuts into a garrisoned alien **Bastion** and siphons it
for Salvage. **Gatekeepers** live there and come to throw you out. The umbilical back to
your Ship ends in the **Hatch** — your way home, and the thing you must hold. Lose the
Hatch, or go down yourself, and the Run is over.

You are alone in a corridor with a gun, the Hatch behind you, and a dial that decides how
hard the next few minutes are going to be.

## What a Run is

You embark from your own Ship. The NAV MAP states the premise before you leave. You arrive
already tethered — there is no landing sequence. The Bastion is quiet.

**It stays quiet until you decide otherwise.** There is no Wave timer. Nothing spawns
until you pull the siphon draw, and that means the pause between Waves is yours: repair
the Hatch, walk the room, pick your ground, or stand at the console and think. A game
that spawns on a clock is telling you when to be ready. This one asks.

Then you pull the draw and they come. Not in a stream — in **groups**, with a gap between
them, which is the difference between a fight you can read and a wall you cannot. Kill
them and the money is yours immediately, the instant each one drops. Clear the Wave and
the quiet comes back.

A Run is a fixed number of Waves. Clear them all and you have stripped the Bastion. How
many: [Field Manual](Field-Manual.md).

## The wager

Before each Wave you set the **siphon dial**. Higher pays more and sends more bodies.

It is deliberately a small, honest wager. The dial does not make Gatekeepers tougher, does
not tighten their spacing, and does not add a single extra Hunter or Ram — every body it
adds is the basic kind. You are betting that you can kill more of the same thing, not
that you can survive a different fight. And you set it in the quiet: once a Wave is live
the dial locks, so it is a decision you commit to rather than one you back out of.

## The three things that come at you

They are not tiers of the same Gatekeeper. Each one is defined by something it **refuses** to
do, and that refusal is the whole design.

A **Default** marches on the Hatch and ignores you — until you hurt it, and then it comes
for you instead. It is the one that can be pulled off the door, and pulling it is a
choice with a cost.

A **Hunter** will never touch the Hatch. It wants you, it is faster than your walk and
slower than your sprint, and it dies quickly. It is the reason you cannot simply park in
front of the door and hold the trigger.

A **Ram** will never touch you. You can empty a clip into its back and it will not turn
around, because it does not care about you at all — it is walking to the door, and if it
gets there it takes a serious bite out of it. It is slow. That slowness is the offer: you
have time to deal with it, if you spend that time on it.

So the room asks three questions at once, and the answer to each makes the other two
harder. That is the game.

## Losing, and what losing costs

There are exactly two ways a Run ends badly: **you die, or the Hatch dies.** There is no
bail-out, no escape lever, no cutting the tether early, no respawn. Once you are in, the
only exits are winning and losing.

But losing is cheap. **Every kill you banked stays banked.** Dying takes nothing back and
clearing takes nothing extra — the two endings differ in what the screen says and which
counter ticks, not in what you keep. A Run that fell apart on Wave two still paid for the
upgrade that gets you through Wave two next time.

That is the deal the game makes with you: the Runs are dangerous, and the failure is not.

## Between runs

You walk your own ship. Four consoles, and each owns exactly one subject so you always
know where a thing is bought.

**ARMORY** owns the weapon in your hands — the weapons themselves, and the upgrade wheel
for whichever one you are carrying. Upgrades are *per weapon*, so pouring Salvage into a
gun is a commitment to that gun, and buying a new one starts that investment over. Your
Primary is chosen here and fixed for the Run; you decide what kind of fight you are
walking into before you walk into it.

**CREW QUARTERS** owns you: how much you can take, how far you can sprint, how much you
earn, and your only source of critical hits.

**FABRICATION** owns the ship: how much the Hatch can take, and the Repair Rig — the
equipment without which the Hatch cannot be repaired at all.

**NAV MAP** owns where you go, and is where you commit to going there.

The first weapon is free, and it is still bought at the ARMORY — because the first thing
the game needs to teach you is that weapons come from that console.

## Where it is going

Two to four players on one hull. The whole thing is built for it already: the rules run
in a layer with no rendering in it, every object knows whether it would be owned by the
host or the player, and every change to the world goes through a single command path. The
co-op is not built. Nothing in the design is waiting on a rewrite to allow it.

## What it looks like right now

Gray boxes. No art pass, no audio pass, no menus beyond what the loop needs. The fight is
being made worth looking at before anyone makes it look like anything.

---

*Every number, key, weapon and Wave: [Field Manual](Field-Manual.md) — generated from the
build, so it cannot be out of date.*
