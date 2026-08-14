<!-- Purpose: the defended objective — what threatens it, how repair works, and what repair costs. / Status: working / Update trigger: when the damage sources, the repair charge, or the loss condition change. / Last updated: 2026-08-06 -->

# The Hatch

The Hatch is the umbilical junction back to your Ship, at one end of the Bastion. It is the only thing in the Run that is being defended, and it is why you are stood where you are stood.

It has its own health, separate from yours. It cannot be moved, closed, sealed, or traded away. **If it reaches zero the Run ends immediately** — the same ending as your own death, the same counter, the same screen. There is no version of the Run that continues without it.

Field-terminal Waves leave it invulnerable. A Hatch-bank Wave is when it can be hurt, and when Unbanked converts to the ship — drip on the Score card, dump when the timer ends. Unbanked hitting 0 does not end the defense. How hard that Wave is, and how much the convert pays, is how many field Waves you carried in before you called it.

It starts every Run whole. Whatever happened to it last time did not carry over.

## What damages it

Only two of the three things that come aboard will touch it.

A **Default** walks at the Hatch and hits it. It will ignore you entirely unless you hurt it, at which point it comes for you instead — which is the trade: you can pull one off the door, and pulling it means it is now your problem.

A **Ram** will *only* ever hit the Hatch. It will not turn on you, will not defend itself, and will keep walking while you empty a clip into its back. When it arrives it takes a serious bite. It is slow, and that slowness is the offer — you have time to deal with it if you spend the time on it.

A **Hunter** never touches the Hatch. It only wants you.

So the shape of the threat to the door depends entirely on what the Wave brought. A Wave with Rams in it is a Wave where the door is the thing at risk. A Wave without them is a Wave where you are.

## Repairing it

Stand at the Hatch and hold the interact key. It heals continuously while you hold, at a flat rate, but **only if you have the Repair Rig equipped** — without it, holding the key does nothing to a damaged Hatch. Let go and it stops. The rate is in the [Field Manual](Field-Manual.md).

Prep `E` then `T` while looking at the Hatch, with Unbanked and a slot left, starts a Hatch-bank. That is not repair.

If the Hatch is full, Prep, and Unbanked is 0, hold Interact to **withdraw** banked Salvage back into Unbanked. Withdraw does not need the Repair Rig.

**Repair is charged in Salvage, per point healed, out of the same bank you spend at the Ship.** The prompt at the door tells you the rate and totals what the current hold has cost you so far. That prompt is the only place the game ever mentions the price.

You can repair during a Wave, not only in the quiet of Prep. Whether that is a good idea is a separate question — standing still at the door with bodies in the room is exactly where a Hunter wants you.

If your bank is empty, you cannot repair at all. Not slowly, not partially: the prompt changes to tell you there is nothing to spend and the key does nothing.

If your bank is nearly empty, holding repair will heal as much as you can afford and leave you at zero.

## What repairing costs you

More than the number.

**Repair is paid for out of the same Salvage you were saving for a weapon.** Every point you put back into the door is a point that does not go into the thing that would have stopped the door being hit. That is why a Run where the Hatch takes a beating is a Run that costs you twice — you pay for the damage, and then you get back to the Ship and find you cannot afford what you went in for.

The honest consequence: **letting a Ram reach the door is more expensive than killing it, every time.** Not in danger — in money. That is the whole reason to walk out and meet one instead of holding the doorway.

The end screen breaks out what you spent on repairs, if you spent anything. It is worth reading, because it is the only place the Run tells you what the door cost you.

## See also

[A Run](A-Run.md). [Salvage](Salvage.md). [Gatekeepers](Gatekeepers.md). [The Ship](The-Ship.md).

## Gaps

- Nothing on the Ship tells you repair costs Salvage. FABRICATION sells you the repair rig and never mentions a price. You learn it at the door, mid-Run, with the meter already running.
- Nothing tells you the Hatch is restored to full at the start of every Run.
- Nothing tells you that repair works while a Wave is live. The repair prompt is presented alongside the quiet-phase panel, which reads as though it were a Prep-only action.
- Repair charges in whole units and holds the leftover fraction between frames. A very short tap can heal nothing and cost nothing, which looks exactly like the key not working.
- Nothing tells you that a hold you cannot afford will spend your entire bank and stop at zero rather than refusing.
- Nothing tells you that losing the Hatch counts as a lost Run in the same way your own death does — same counter, and it unlocks weapon Tiers just the same.
- The HUD warns you the Hatch is under attack but never what is hitting it.
- No upgrade changes repair at all once you own the Rig: not the price per point healed, and not the rate. The Rig is the whole of it.
- The code names four other ship subsystems — a reactor, a tether core, engines, life support. **None of them exist in play.** There is exactly one defended object in the Run and it is the Hatch.
- The underlying system would let a destroyed Hatch be repaired back into service, but you can never do it: the Run has already ended by the time it hits zero.

*Hatch health, repair rate and repair price: [Field Manual](Field-Manual.md).*
