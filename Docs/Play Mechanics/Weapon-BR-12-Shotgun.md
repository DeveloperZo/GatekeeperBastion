<!--
Purpose: BR-12 Shotgun, an instance of Weapons — pellet cone Primary, Concussive Breach.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31
Where:
- Catalog row: Primary, pellet cone, no pierce, shortest Primary reach with early falloff — `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` (`Def`, `WeaponId.Br12Shotgun`)
- Pellets loop per shot; each pellet's ray stops on the first body — `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`Fire`, `FireRay` with `pierceCount` 0)
- Crit rolled per pellet but unreachable: the chance the roll reads is a hardcoded 0 on the gun (`MetaUpgrades.CritChance()` is frozen at 0 and has no caller) — `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`Fire`, `RollCrit`); `Assets/Scripts/Game/Player/PlayerGun.cs` (`CritChance`)
- Concussive cone selects, the Hatch sets the push direction, dial buys the metres — `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`Br12ConeHalfAngleDegrees`, `Magnitude`); `Assets/Scripts/Game/Player/PlayerAbility.cs` (`ActivateConcussive` — cone test on `forward`, delta is `awayFromHatch * metres`)
- The shove is a NavMesh warp and returns walk-back seconds; no walkable sample, no movement — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Shove`)
- Ring order and declared identity (strength Damage, weakness Reload) — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`RingOf`, `TryIdentity`)
-->

# BR-12 Shotgun

Instance of [Weapons](Weapons.md).

The Primary for things that come to the Boarder: a cone of pellets that each stop on the first body they touch, so the whole shot spends on one target instead of sweeping a group. It has the shortest reach of the three Primaries and starts losing damage soonest, and its ability shoves whatever it catches away from the Hatch.

## Game

A Primary for things that come to me. One pull throws a cone of pellets, and each pellet is nothing on its own. The shot is strong only when most of them land — which means close, and filling the crosshair.

Damage bleeds off with distance, and at the edge of its reach it does nothing at all. That reach is the shortest of the three Primaries by a wide margin — only the SEC-D in my other hand works at less — and there is no cue for where the falloff starts. A shot that looked like a hit can be a blank.

Pellets stop on the first thing they touch. The cone does not sweep a group — pellets that land on a second body are pellets that failed to kill the first. Pick one body. Give it everything.

Slow cadence, small clip, the longest reload of the three Primaries. Very few pulls between long, vulnerable pauses. Break contact and reload before empty, or swap, which is faster.

Let them close. That is the whole weapon. Against Hunters it costs nothing — they do the closing, and it deletes them.

Wrong tool for Thieves and for long corridors. Start on a Thief early, and count the time as spent, because the Hunters will spend it on me.

## Details

### Structure

Primary. One pull throws a cone of pellets. No pierce. The shortest reach of the three Primaries.

### Fire

Each pellet is weak alone; the shot is strong only when most of them land. Every pellet stops on the first body it touches, so the cone does not sweep a group — the whole shot spends on the nearest body in it. The Damage spoke raises the per-pellet figure.

Falloff starts nearest the muzzle of the three Primaries. Past the edge, pellets do nothing at all.

Wrong tool for Thieves and for long corridors. Let them close.

Where the lines fall: [Field Manual](Field-Manual.md).

### Ability

**Concussive Breach.** A cone in front of the Boarder, out to the weapon's own reach, picks the Gatekeepers — but the shove itself is directed away from the Hatch, not away from the Boarder, so every body it catches is pushed down the same line regardless of where it stood in the cone. The Ability spoke buys shove distance. The push needs walkable ground behind the body; without a sample, the body does not move.

### Rules

Its ring runs strength Damage, weakness Reload, with Ability adjacent to Damage.

## Additional Details

### Gaps

- Damage falls off with distance and there is no indicator of where that begins or how bad it is. Shots at medium range look identical to shots at point blank.
- Its reach is far shorter than either other Primary's. Past the edge, pellets do literally nothing — no damage, not reduced damage.
- Pellets do not pierce, so the cone does not spread damage across a crowd the way it looks like it should. Aiming "between" two Gatekeepers wastes the shot.
- The bracket crosshair is an identity cue for this weapon, not a spread indicator. It does not grow or shrink to show accuracy.
- Crit chance is rolled per pellet. Chance is frozen at nothing, so the extra rolls currently buy nothing.
- Its clip and reload make it structurally poor against Thieves, which the game never flags at the moment you buy it.
- Swapping to the sidearm is usually better than reloading under pressure — and swapping mid-reload destroys the reload progress.
