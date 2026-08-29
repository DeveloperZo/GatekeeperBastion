<!--
Purpose: the FORGE Protocol, an instance of Protocols — differences only: PARALLEL BAYS doctrine, Field Deploy, Overcharge, Magnet Hands.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
-->

# FORGE Protocol

Instance of [Protocols](Protocols.md).

The only sheet where something other than the Boarder deals damage: it stands a turret that fires on its own, speeds up every Boarder near it, and draws Gatekeepers off the wearer onto itself. The turret is mortal and runs out on its own clock, and the sheet's promised second decoy bay is written but does nothing yet.

## Game

This sheet puts a second machine in the room for the garrison to account for, and it is the only sheet where something other than me deals the damage.

Its doctrine widens the bay: two of my decoys may stand live at once instead of one.

The crew burst stands a turret where I am looking. It acquires and fires on its own out to a useful distance, and while it stands, every Boarder near it fires faster. A plant, an emplacement and a buff on one invoice — and standing a new one replaces the old, so the fleet is always exactly one machine deep.

The personal burst redlines it. The turret fires much harder, my own gun fires harder, and the machine pays for the window out of its own life at a steady burn. A full window on a healthy turret leaves it wounded. On a wounded one, it is a burnout I scheduled. It refuses outright with nothing standing.

The passive is the one nobody reads and the one that makes the sheet work: a Gatekeeper that would come for me takes the turret instead, whenever both are in reach. It is the only way the turret is ever attacked. Without it the machine would be furniture. With it, the machine is mortal, and it is mortal on my behalf.

Attention and structure are the same subject here. I build the thing they look at.

## Differs

**Doctrine: PARALLEL BAYS.** Two of the Boarder's decoys may stand live at once. **Not yet active** — the widening rule exists in the catalog and has no caller, so the bay still holds the payload's authored one and a decoy placed under PARALLEL BAYS behaves exactly as one placed under STOCK.

**Crew: Field Deploy.** Stands a turret on the floor the caster is looking at, within reach; looking at a wall or open air puts it a step in front of the caster. The press is instant — no window, straight to cooldown, because the turret's own life is its uptime. It acquires and fires on its own out to its range, and while it stands, every Boarder inside its aura fires faster; the aura belongs to the machine and lasts exactly as long as it does. Deploying again replaces the standing turret.

**The turret is mortal three ways, whichever comes first.** Gatekeeper damage kills it — hardware is mortal by default, no passive required. Its magazine runs dry: it carries a fixed round budget and running out expires it. And its life clock runs out: it stands a fixed number of seconds whether or not it is shooting, so a turret covering a quiet lane still expires. Running dry, timing out and being killed are kept as distinct events.

**Personal: Overcharge.** Redlines the standing turret for the window — it fires much faster and the caster's own gun fires faster. The spike is paid from the turret's own separate overcharge belt of rounds, not from its life and not from its ordinary magazine: the per-second life burn was retired (2026-08-28) because it charged the player twice for one spike, and the retired burn now bills zero. A belt that runs dry drops the turret back to normal fire; the turret is still standing with whatever its magazine held. Overcharge refuses, before the cooldown is spent, when no turret is standing.

**Passive: Magnet Hands.** Widens the turret's pull; it does not grant it (2026-08-28 — before that the passive was the only way a turret could be attacked, and an unworn passive made the machine invulnerable). Wearing it, a Gatekeeper that would hunt the wearer diverts to that Boarder's turret from anywhere inside the turret's aura radius; without it, a Gatekeeper only swings at a machine already within its own melee reach — it is in the way, so it gets hit.

Range, damage, rates, budgets, the life clock, aura radius and multipliers: [Field Manual](Field-Manual.md).

## Where

- PARALLEL BAYS written but unread — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`ParallelBaysMaxLive`, `MaxLiveUnder` — no caller outside its own definition); `Assets/Scripts/Sim/Run/DecoyState.cs` (`TryPlace` refuses a second decoy on the payload's `MaxLive`, mode unread)
- Field Deploy is instant, the slot goes straight to cooldown — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`OnCrew`, `BoarderAbilityState.TrySpendInstant`); placement point and reach — `Assets/Scripts/Game/Player/PlayerTurretControl.cs` (`Deploy`, `PlacementPoint`); replace-not-add — `Assets/Scripts/Sim/Run/TurretState.cs` (`TryPlace`)
- The three deaths kept distinct — `Assets/Scripts/Sim/Run/TurretState.cs` (`TakeDamage` enemy kill; `SpendShot` magazine expiry; `TickLife` clock expiry; `Expire` marked not-enemy-damage); budgets and the clock — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`TurretShotBudget`, `TurretLifeSeconds`)
- Overcharge draws on its own belt, never the magazine or the life — `Assets/Scripts/Sim/Run/TurretState.cs` (`SpendShot` overcharged branch, `OverchargeShotsRemaining`, `BurnOvercharge` burns what the retired rule returns); the retired burn bills zero — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`OverchargeBurnForSeconds` returns 0, `OverchargeShotBudget`)
- Overcharge's precondition asked before the window is spent — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`OnPersonal`, `HasTurret` check); rate and self multipliers — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`OverchargeTurretRateMul`, `OverchargeSelfFireMul`)
- Magnet Hands widens the pull, mortal-by-default targeting — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Retarget` turret block, `TurretPullRadius`, `PrefersHardware`)
- The aura is the machine's and read positionally by every gun — `Assets/Scripts/Game/Bastion/TurretObject.cs` (`FireMulAt`); composed in `Assets/Scripts/Game/Player/PlayerGun.Slots.cs`; radius and multiplier — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`TurretAuraRadius`, `TurretAuraFireMul`)

## Gaps

- Nothing tells you the turret can be destroyed, or that Magnet Hands is the reason it can. Losing one reads as a bug.
- The turret has no visible health, so there is no way to judge whether Overcharge will burn it out.
- The aura radius is not drawn. You cannot tell whether you or a teammate is inside it.
- Overcharge refusing with no turret standing is silent in play.
- PARALLEL BAYS is written onto every decoy placed and nothing reads it, so the doctrine row on the sheet promises a second bay the Drone bay does not give.
