<!--
Purpose: Hunter, an instance of Gatekeepers — differences only.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31
Where:
- Never targets the Hatch, hardened — `Assets/Scripts/Game/Enemies/HunterGatekeeper.cs` (`MayTargetHatch => false`)
- The rule stated in the sim too — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, Hunter case: `hatchDamage = 0f`, `ignoresHatch = true`); `Assets/Scripts/Sim/Waves/GatekeeperStats.cs` (`ignoresHatch`)
- Hunts from spawn; commit latch unreachable — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`ChasesPlayer`'s `!hatchAvailable` term; `Retarget`'s commit guard on `hatchAvailable`)
- One gear, and the only kind above the walk — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, Hunter case: `patrolSpeed` equals `chaseSpeed`, both above `GatekeeperChaseSpeed`); the walk and the sprint it sits between — `Assets/Scripts/Game/Player/FirstPersonController.cs` (`moveSpeed`, `sprintSpeed`, and the remarks that pin the three numbers together)
- Not self-provoked, so a hit still shouts — `Assets/Scripts/Game/Enemies/HunterGatekeeper.cs` (remarks; no `CanBeProvoked` override); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`OnDamaged` → `Provoke(shout: true)`)
- Cheap solver — `Assets/Scripts/Game/Enemies/HunterGatekeeper.cs` (`AvoidanceQuality`)
-->

# Hunter

Instance of [Gatekeepers](Gatekeepers.md).

The only kind faster than a walking Boarder, and the only one that never touches the Hatch — it comes for the Boarder from the moment it arrives and cannot be moved onto the door. Holding the door does nothing about it, and shooting it still pulls its neighbours in.

## Game

The lean one. I get its silhouette, and that is all I get — the Hatch alarm never fires for it, because it has never once touched the Hatch.

It chose me the moment it arrived. Not provoked, not lured, not confused about anything: it walks straight at me, at full pace, for life. It has no second gear because it never needed one.

The Hatch does not exist in whatever it uses for a world. It will not swing at the door while standing on it. So holding the door answers the three kinds that want the door and answers this one not at all — it just delivers it to the worst place in the room to be found.

It is the fastest thing coming and the easiest to kill, and both of those are the same instruction. There is no positioning answer. The answer is the gun, and it gets the gun first.

Open on one walking in with escort and the escort comes too — it arrived hunting but was never provoked, and provoking it shouts. The Thieves beside it do not look up. They never look up.

## Details

### Structure

May not target the Hatch. May target a player. Can be provoked. The Hatch question is the one it answers no.

### Refusals

The Hatch, ever, at any range. Hatch damage is zero: a Hunter standing on the Hatch still cannot hurt it. The hatch-commit latch can never close on one.

### Behaviour

One gear, and it is the fast one. Patrol speed equals chase speed. That single speed is the only Gatekeeper speed above the Boarder's walk, still under a sprint ([Moving](Moving.md)). Backing away while shooting is not a free win against the one kind built to catch the Boarder. Figures: [Field Manual](Field-Manual.md).

A hit still shouts. Cheapest crowd solver: a straight line at one moving target.

### Rules

It hunts from the moment it spawns. With no Hatch in its world, the shared rule's "nowhere else to go" clause holds from the first tick, so it beelines the nearest live Boarder for life without needing provocation.

It is not self-provoked at spawn. It arrives at full tilt anyway, and leaving the provocation unspent means the first hit still pulls its group.

## Additional Details

### Gaps

- Its hits and a Default's hits feel identical, so when your health drops you cannot tell which kind is on you without turning around.
- The remaining-Gatekeepers count on the HUD does not separate Hunters out, so you can never confirm you have cleared them.
- That a Hunter cannot damage the Hatch even standing on it is never stated. Players defend the Hatch against them for no reason.
- That shooting a Hunter alerts nearby Defaults is invisible, and it is the most common way to accidentally triple the size of a fight.
- The Hatch alarm is silent on Hunters — no Hatch damage means no alert, so a Hunter arriving is the one threat with no on-screen tell at all beyond its silhouette.
