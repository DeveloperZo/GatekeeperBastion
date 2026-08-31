<!--
Purpose: the FORGE Protocol, an instance of Protocols — differences only: charged turret with Overcharge, Outrider with EMP, proximity-gated pairing, passive CLASSIFIED.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
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

## Differs

**Crew: Field Deploy.** Stands a turret on the floor the caster is looking at, within reach; looking at a wall or open air puts it a step in front of the caster. The plant is instant — no window, straight to cooldown. It acquires and fires on its own out to its range, and while it fires, every Boarder inside its aura fires faster. Redeploying replaces the standing turret and keeps its charge. Spending charge Overcharges it — refused when the standing turret has none — and redlines the turret (and the caster's own gun) until the earned charge is spent, then Exhausted. Near the Outrider, Overcharge also puts the Outrider at maximum fire rate. The pairing is proximity-gated.

**The turret is mortal three ways.** Gatekeeper damage kills it — hardware is a target in melee of a body already chasing the Boarder, with no protocol passive involved. Overcharge spends it into Exhausted: it stands silent, then despawns without further input. An Exhausted turret can also be cleared off the field. It does not expire on a life clock, and it does not run dry of a magazine.

**Personal: Outrider.** Deploys a follower that stays with the Boarder and fires on the Gatekeeper nearest to them, not the one nearest to itself. Redeploying a live Outrider moves it to the Boarder and keeps its charge. Spending charge EMP-blasts — refused without charge by that name — light damage and a root from the Outrider; near the turret, the turret's fire becomes EMP rounds (same damage, plus the root) for the EMP window. EMP ends Exhausted. An Exhausted Outrider is cleared off the field.

**Both specials end independently**, each on its own charge window.

**Passive.** ** CLASSIFIED **. Deliberately so rather than blank — an empty row is the panel reading as broken.

Range, damage, rates, aura, pairing radius, root length, charge dials, and the press-versus-hold bindings: [Field Manual](Field-Manual.md). Charge per shot, charge max, hold threshold, Exhausted window, pairing radius, follow distance, EMP damage and root length are low-end placeholders.

## Where

- Field Deploy is press versus hold — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`Tick`); placement — `Assets/Scripts/Game/Player/PlayerTurretControl.cs`; charge kept on live replace — `Assets/Scripts/Sim/Run/TurretState.cs` (`TryPlace`)
- Outrider is press versus hold on the personal binding — `Assets/Scripts/Game/Player/PlayerOutriderControl.cs`; follow and Boarder-nearest fire — `Assets/Scripts/Game/Bastion/OutriderObject.cs`; charge and phase — `Assets/Scripts/Sim/Run/OutriderState.cs`
- Charge rises only through `SpendShot`; specials refuse at zero charge by that name; Special ends Exhausted; Exhausted cannot re-enter Deployed — `TurretState`, `OutriderState`
- Pairing: inside the radius Overcharge lifts the Outrider's rate and EMP converts turret fire; outside, neither does — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`MachinesPaired`, `PairedOutriderRateMul`, `PairedTurretFiresEmp`, `PairingRadiusMetres`)
- EMP blast and root — `OutriderObject.ApplyEmpBlast`; `EnemyGatekeeper.Root`; `Assets/Scripts/Sim/Run/GatekeeperRoot.cs` (`MayAdvance`, `MaySwing`)
- Charge dials, hold threshold, pairing, EMP — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs`
- Ordinary melee divert onto a standing turret or Outrider — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Retarget` machine block)
- HUD: turret charge on the crew square, Outrider charge on the personal square, pairing on the protocol line — `Assets/Scripts/Game/Bastion/HudController.Protocol.cs`
- Per-Wave deploy-order line — `Assets/Scripts/Game/Bastion/GameController.cs` (`OnAssaultEnded`); latches — `PlayerProtocolAbilities.WaveMachineLine`
- Sheet copy — `Assets/Scripts/Sim/Meta/BoarderSheets.cs` (`CrewForge`, `PersonalForge`, `PassiveForge`)

## Gaps

- The turret has no visible health, so there is no way to judge how many swings it will take. The Outrider has the same bar the turret does; the tell is still easy to miss in a scrum.
- The aura radius is not drawn. The pairing radius is the protocol line reading "paired", not a disc on the floor.
- Charge is on the two squares; a refusal at zero charge is a log line the player may miss if they have not watched the square.
- PARALLEL BAYS is written onto every decoy placed and nothing reads it, so the doctrine row on the sheet promises a second bay the Drone bay does not give.
- Whether the Outrider should be blocked from following through a closed door is undecided.
