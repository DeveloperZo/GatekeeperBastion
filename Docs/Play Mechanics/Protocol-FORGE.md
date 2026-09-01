<!--
Purpose: the FORGE Protocol, an instance of Protocols — two machines, charge spent into Overcharge and EMP, proximity-gated pairing, passive CLASSIFIED.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-09-01
Where:
- Field Deploy is press versus hold — Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs (Tick); placement Assets/Scripts/Game/Player/PlayerTurretControl.cs; charge kept on live replace TurretState.TryPlace
- Outrider is press versus hold on the personal binding — PlayerOutriderControl.cs; follow and Boarder-nearest fire OutriderObject.cs; charge and phase OutriderState.cs
- Charge rises only through SpendShot; specials refuse at zero charge; Special ends Exhausted; Exhausted cannot re-enter Deployed — TurretState, OutriderState
- Pairing: inside the radius Overcharge lifts the Outrider's rate and EMP converts turret fire; outside, neither does — BoarderProtocol.cs (MachinesPaired, PairedOutriderRateMul, PairedTurretFiresEmp, PairingRadiusMetres)
- EMP blast and root — OutriderObject.ApplyEmpBlast; EnemyGatekeeper.Root; GatekeeperRoot.MayAdvance / MaySwing
- Ordinary melee divert onto a standing turret or Outrider — EnemyGatekeeper.Targeting.cs (Retarget machine block)
- HUD: turret charge on the crew square, Outrider charge on the personal square, pairing on the protocol line — HudController.Protocol.cs
- Sheet copy — BoarderSheets.cs (CrewForge, PersonalForge, PassiveForge, DoctrineForge)
-->

# FORGE Protocol

Instance of [Protocols](Protocols.md).

The only sheet where something other than the Boarder deals damage: it stands a turret that holds ground and a follower that travels with the Boarder. Both charge by firing. Each special lifts the other when the two machines are near.

## Game

This sheet puts two machines in the room, and they are the only things besides me that deal the damage.

I stand a turret where I am looking. It holds ground, fires on its own, and speeds every Boarder near it. Calling it up again moves the live one and keeps whatever charge it has already earned. Charge rises only while the machine is firing — an empty room earns nothing — so whatever I spend later was earned inside the fight I spent it in.

When the turret has charge, I can spend it into Overcharge: the machine redlines until the charge is gone, then stands Exhausted — silent, then gone. An Exhausted turret cannot be refreshed; clearing it starts the wait before I get another.

The other machine travels with me. The Outrider stays at my side and fires on the Gatekeeper nearest to me. It charges the same way. Spending its charge is an EMP: light damage and a root from the follower, then Exhausted the same as the turret.

When the two are near each other, spending either one lifts the other — Overcharge also puts the Outrider at maximum fire rate, and EMP makes the turret's fire root what it hits. Outside that distance each spend is its own. Both orders reach the same peak: a max-rate EMP turret beside a max-rate Outrider.

The passive row reads ** CLASSIFIED **. It stays that way on purpose until the two-machine loop has been played.

Attention and structure are the same subject here. I build the thing they look at, or I take it with me.

## Details

### Structure

Four rows. Doctrine PARALLEL BAYS. Crew Field Deploy. Personal Outrider. Passive ** CLASSIFIED **.

`G` and `H` are press versus hold on this sheet. A tap deploys or moves the machine. A hold spends whatever charge that machine has already earned. There is no timed burst window: each machine's own phase is the duration.

### Doctrine

**PARALLEL BAYS.** The row promises two decoys standing live at once. The Drone bay still holds one. The name is written onto every decoy placed and nothing reads it.

### Crew

**Field Deploy** — `G`.

Output: a turret on the floor the caster is looking at. It holds ground, acquires and fires on its own, and while it fires every Boarder inside its aura fires faster. A second plant moves the live turret and keeps its charge.

Cost: the caster's `G`. The plant is instant. Spending charge Overcharges the turret — refused at zero charge — and redlines the turret and the caster's own gun until that charge is gone, then Exhausted. Near the Outrider, Overcharge also puts the Outrider at maximum fire rate.

The turret is mortal three ways: Gatekeeper melee (a body already chasing the Boarder will swing at it), Overcharge spending it into Exhausted, or the Boarder clearing an Exhausted body off the field. It does not expire on a life clock and it does not run dry of a magazine.

### Personal

**Outrider** — `H`.

Output: a follower that stays with the Boarder and fires on the Gatekeeper nearest to the Boarder, not the one nearest to itself. A second deploy moves a live Outrider to the Boarder and keeps its charge.

Cost: the presser's `H`. Spending charge EMP-blasts from the follower — light damage and a root, refused at zero charge. Near the turret, the turret's fire becomes EMP rounds (same damage, plus the root) for the EMP window. EMP ends Exhausted. An Exhausted Outrider is cleared off the field.

### Passive

** CLASSIFIED **. Deliberately so rather than blank — an empty row is the panel reading as broken. The garrison reads nothing extra from this sheet.

### Rules

Charge rises only while the machine is firing. An empty room earns nothing.

Each special ends on its own charge window. They do not share a clock.

Pairing is proximity. Inside the radius, spending either machine lifts the other. Outside it, each spend is only itself. Both orders reach the same peak: a max-rate EMP turret beside a max-rate Outrider.

A Gatekeeper already chasing the Boarder will divert onto a nearer live turret or Outrider in melee. Kind refusals still outrank this: a Thief never turns.

Numbers — range, damage, rates, aura, pairing radius, root length, charge dials, hold threshold, Exhausted window, follow distance: [Field Manual](Field-Manual.md). Charge per shot, charge max, and the pairing radius are low-end placeholders.

## Additional Details

### Gaps

- Both machines carry a life bar, so how many swings one has left is readable in principle. In a scrum it is easy to miss, and nothing else calls out a machine about to fall.
- The aura radius is not drawn. The pairing radius is the protocol line reading "paired", not a disc on the floor.
- Charge is on the two squares; a refusal at zero charge is a log line the player may miss if they have not watched the square.
- PARALLEL BAYS is written onto every decoy placed and nothing reads it, so the doctrine row on the sheet promises a second bay the Drone bay does not give.
- Whether the Outrider should be blocked from following through a closed door is undecided.
