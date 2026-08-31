<!--
Purpose: the VEIL Protocol, an instance of Protocols — differences only: PHANTOM doctrine, Ping, Cloak, Ghost.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
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

## Differs

**Doctrine: PHANTOM.** A decoy the Boarder places is offered the attention that Boarder is currently carrying — at placement, every live Gatekeeper already chasing that Boarder is walked through a transfer. It is a transfer, never a creation: a Thief declines, a Gatekeeper committed to the Hatch keeps its commitment, and the payload's own live count is unchanged.

**What the transfer buys today is an early tick, not extra reach.** The transfer writes the decoy onto the Gatekeeper and then re-runs the standing retarget on it; that method clears the decoy target it was just given and re-derives one from the ordinary pull test — inside the payload's pull radius, not a Thief, not hatch-committed. So the bodies PHANTOM leaves on the decoy are the bodies the ordinary pull would have taken on the next retarget anyway. A chaser outside the pull radius is transferred and handed straight back, while the placement line still counts and logs it as inherited.

**Crew: Ping.** For the window, every Gatekeeper renders through walls, for every Boarder. The same burst provokes every Gatekeeper that can be provoked onto the caster — the pull is the standing provoke rule, so each kind refusal (a Thief walks on) is honoured without Ping knowing which kinds those are, and the provoked do not shout. Solo, the caster gets the render and the pull with nobody to hand the map to: a deliberate mass-provoke.

**Personal: Cloak.** Off every targeting list for the window — the registry the garrison picks targets from skips a cloaked Boarder outright. Gatekeepers already swinging finish the swing. Firing exits it instantly; it is the only window the gun collapses.

**Passive: Ghost.** Gatekeepers notice and acquire the wearer at a reduced fraction of their normal ranges — both the sight range and the proximity radius are scaled. It applies always, moving or still, and is rewritten at bind so a protocol swap cannot leave it on the rig.

Windows, cooldowns, the Ghost fraction, and the ability bindings: [Field Manual](Field-Manual.md).

## Where

- PHANTOM's inheritance at placement, count of transfers logged — `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`ApplyPhantom`, gated on `State.Mode == DoctrineMode.Phantom`)
- The transfer rule and its refusals (Thief, hatch-committed, not currently chasing the player) — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`TransferInterestTo`)
- The re-derive that limits it: the transfer's last statement is `Retarget()`, which zeroes `_decoyTarget` / `_chasingDecoy` and re-runs `DecoyState.ShouldPull` against the pull radius — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Retarget`, the decoy-pull block); `Assets/Scripts/Sim/Run/DecoyState.cs` (`ShouldPull`)
- Ping's provoke-per-body pulse and its render window — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`PulsePing`, `PingWindowOpen`); markers drawn by `Assets/Scripts/Game/Enemies/PingMarker.cs` (`ShowAll`)
- The pull is the standing provoke rule, kind refusals included — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Provoke`, `CanBeProvoked`)
- Cloak sets the rig off the target registry; firing collapses it — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`SetCloaked`, `NotifyFired`); `Assets/Scripts/Game/Player/PlayerHealth.cs` (`Cloaked`); `Assets/Scripts/Game/Player/PlayerRegistry.cs` (`NearestAlive` skips `Cloaked`)
- Ghost's acquisition bias, applied at bind and read per retarget — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`GhostAcquisitionMul`, `Magnitude`); `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`ApplyPassive`); `Assets/Scripts/Game/Player/PlayerHealth.cs` (`ConfigureAcquisition`, `AcquisitionMul`); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`NoticeRangeFor`, `AggroRadiusFor`)

## Gaps

- Ping's pull is not distinguished on screen from its render. Players read the through-walls effect as the whole ability and are surprised by the arrival.
- Nothing indicates you are cloaked to the garrison rather than merely unnoticed, and nothing warns that firing ends it.
- Ghost has no readout at all. A permanently reduced acquisition range is invisible and is the sheet's most valuable row.
- That a PHANTOM decoy inherits what is already provoked onto you is never stated. It looks like an ordinary decoy that happened to work well.
- Ping does not exclude Thieves in its description, so players expect it to move them.
