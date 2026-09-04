<!--
Purpose: the VEIL Protocol, an instance of Protocols — PHANTOM doctrine, Ping, Cloak, Ghost.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31
Where:
- PHANTOM's inheritance at placement — DecoyBay.cs (ApplyPhantom, gated on State.Mode == DoctrineMode.Phantom)
- The transfer rule and its refusals (Thief, hatch-committed, not currently chasing the player) — EnemyGatekeeper.Targeting.cs (TransferInterestTo)
- The re-derive that limits it: the transfer's last statement is Retarget(), which zeroes the decoy target and re-runs ShouldPull against the pull radius — EnemyGatekeeper.Targeting.cs (Retarget); DecoyState.ShouldPull
- Ping's provoke-per-body pulse and its render window — PlayerProtocolAbilities.cs (PulsePing, PingWindowOpen); markers PingMarker.ShowAll
- The pull is the standing provoke rule, kind refusals included — EnemyGatekeeper.Targeting.cs (Provoke, CanBeProvoked)
- Cloak sets the rig off the target registry; firing collapses it — PlayerProtocolAbilities.cs (SetCloaked, NotifyFired); PlayerHealth.Cloaked; PlayerRegistry.NearestAlive skips Cloaked
- A kill refunds seconds against Cloak's cooldown — Assets/Scripts/Sim/Meta/BoarderProtocol.cs (CloakKillRefundSeconds, KillRefundSeconds, BoarderAbilityState.RefundCooldown) called from PlayerProtocolAbilities.NotifyKill on GameController's one kill seam, pinned by Assets/Tests/EditMode/Meta/CloakReentryTests.cs
- Ghost's acquisition bias, applied at bind and read per retarget — BoarderProtocol.GhostAcquisitionMul; PlayerProtocolAbilities.ApplyPassive; PlayerHealth.ConfigureAcquisition; EnemyGatekeeper.Targeting.cs (NoticeRangeFor, AggroRadiusFor)
-->

# VEIL Protocol

Instance of [Protocols](Protocols.md).

The sheet about who is looking at the Boarder: it shows the whole crew every Gatekeeper through walls, and it can drop the wearer off every targeting list until they fire. The cost lands on the caster, since the same burst that gives the crew the map pulls the room onto them. Its third promise, handing the Boarder's own pursuers to a placed decoy, buys only an early pull today rather than a wider one.

## Game

This sheet is about who is looking at me, which turns out to be the only question I ever really had.

I can hand the crew every Gatekeeper through walls — the most useful thing anyone can give them — and for that same window every Default and Hunter in the place is coming for me. The crew gets the map. I get the room. That is not a drawback. It is the price on the tin, and I choose where to set the tin down.

When the room is on me, I place a decoy and it comes up already holding the attention I was carrying, instead of pulling neutrally from whoever is near. I hand the room's interest to a machine and walk out of the room. What it buys today is an early pull, not a wider one — the bodies that stay on it are the bodies the ordinary pull would have taken next anyway — but the hand-off is still the loop.

Then I go dark. Off every targeting list. Anything mid-swing finishes the swing. Firing puts me back on instantly, so it is a way to leave, never a way to win. The room forgets me exactly as long as I give it nothing to remember.

Standing under all of that: they notice me closer in than they otherwise would. Not silence, not stillness — just less reach on being seen. Standing still was never the requirement. It took me three Boardings to stop believing it was.

## Details

### Structure

Four rows. Doctrine PHANTOM. Crew Ping. Personal Cloak. Passive Ghost.

`G` is a pulse: the crew gets the map, the caster gets the room. `H` drops the wearer off every targeting list until they fire.

### Doctrine

**PHANTOM.** A decoy the Boarder places is offered the attention that Boarder is currently carrying — at placement, every live Gatekeeper already chasing that Boarder is walked through a transfer. It is a transfer, never a creation: a Thief declines, a Gatekeeper committed to the Hatch keeps its commitment, and the payload's own live count is unchanged.

What the transfer buys today is an early tick, not extra reach. After the hand-off the body re-derives its target from the ordinary pull test, so the bodies that stay on the decoy are the bodies the ordinary pull would have taken next anyway. A chaser outside the pull radius is transferred and handed straight back.

### Crew

**Ping** — `G`.

Output: every Gatekeeper renders through walls, for every Boarder, for the window.

Cost: the same burst provokes every Gatekeeper that can be provoked onto the caster. The pull is the standing provoke rule, so each kind refusal is honoured without Ping knowing which kinds those are, and the provoked do not shout. A Thief walks on. Solo, the caster gets the render and the pull with nobody to hand the map to.

### Personal

**Cloak** — `H`.

Output: off every targeting list for the window. The garrison's target picker skips a cloaked Boarder outright. Gatekeepers already swinging finish the swing.

Cost: the presser's `H`. Firing exits it instantly. It is the only window the gun collapses. A way to leave, and since 2026-09-04 a way to come back: every gatekeeper the wearer kills while Cloak is cooling takes two seconds off the wait, so the way back into stealth is killing rather than waiting. A wearer who kills nothing waits exactly as long as before, and a trickle body buys nothing, the same rule that stops it paying Salvage or mending health. Nothing here grants damage.

### Passive

**Ghost.** Gatekeepers notice and acquire the wearer at a reduced fraction of their normal ranges — both the sight range and the proximity radius. It applies always, moving or still, and is rewritten at bind so a protocol swap cannot leave it on the rig.

### Rules

The loop on this sheet is take the eyes, hand them to a decoy, go dark, kill, and go dark again. The kill refund is the only thing on VEIL that answers the sheet's old complaint, that it bought survival and never tempo: it converts the wearer's own aggression into the next stealth, and never into damage.

The loop on this sheet is take the eyes, hand them to a decoy, go dark. Ping puts the room on the caster. PHANTOM offers that room to the next decoy placed. Cloak drops the caster off the list. Ghost is the standing discount on being seen, not a stealth condition.

Kind refusals outrank every row: a Thief is never pulled, never handed to a decoy, never cloaked off a list it was never on.

Windows, cooldowns, the Ghost fraction: [Field Manual](Field-Manual.md).

## Additional Details

### Gaps

- Ping's pull is not distinguished on screen from its render. Players read the through-walls effect as the whole ability and are surprised by the arrival.
- Nothing indicates you are cloaked to the garrison rather than merely unnoticed, and nothing warns that firing ends it.
- Ghost has no readout at all. A permanently reduced acquisition range is invisible and is the sheet's most valuable row.
- That a PHANTOM decoy inherits what is already provoked onto you is never stated. It looks like an ordinary decoy that happened to work well.
- Ping does not exclude Thieves in its description, so players expect it to move them.
