<!--
Purpose: MR-35 Rifle, an instance of Weapons — differences only.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
-->

# MR-35 Rifle

Instance of [Weapons](Weapons.md).

The Primary for things that will not come to the Boarder: a held trigger from a deep clip, accurate and full-strength far down a corridor, and it pierces, so a column of bodies is several hits from one pull. Its reach and its pierce depth are both second to the PRI-C's, and its ability charges off hits landed and pins a body where it stands.

## Game

A Primary for things that will not come to me. One heavy, accurate shot per pull, no spread, and full weight far enough down a corridor that most of a fight happens inside it. Past that the distance starts taking a cut, and the reach runs on well beyond where the BR-12 has given up. Only the PRI-C reaches further, and it buys that with cadence I would not pay.

It pierces. A shot continues through a body into the ones behind it, up to a limit. Walls still stop it; the dead do not soak it. The trick only pays when bodies queue, so I shoot down corridors, never across them. A group abreast is one hit. The same group in file is several, from one pull.

The cadence is slow, and a miss is real time gone. It rewards a steady hand and punishes a spray. If I am holding the trigger, I am holding the wrong gun.

Thieves are what it is for. Their walk is the entire budget I get, and this spends it from across the room without making me stand idle between shots the way the PRI-C does.

The weak spot is a Hunter already inside my range. One is fine. Several, arriving between shots, is how Boardings end. That is the price of taking this over the BR-12, and the price is real.

A Damage focus compounds here better than anywhere else. A Thief dropped one shot sooner is worth more than the step looks on the panel.

## Differs

- The line Primary, recut as an assault rifle (2026-08-27): a held trigger at a high rate
  from a deep clip — no spread, full damage far down a corridor before falloff begins,
  out to a reach second only to the PRI-C's. Rate, clip and lines:
  [Field Manual](Field-Manual.md).
- It pierces: a shot continues through a body into the ones behind it, at a reduced cut
  per later body. Walls stop it, corpses do not soak it, and each body takes one ray
  once. Depth and the per-body cut: [Field Manual](Field-Manual.md).
- **Pin Shot**, its ability: hits — never damage — charge a meter on the rifle,
  weapon-scoped across targets, decaying when the hits stop. Each shot charges once, on
  the first body it damages. A ready meter pins the next living body hit automatically,
  or fires on the ability binding; the Ability spoke buys pin seconds.
- Pale-blue tracer — its identity cue, and the at-a-glance answer to who is firing what
  in a co-op fight.
- Its ring runs strength Damage, weakness Fire Rate, with Ability adjacent to Fire Rate;
  Clip and Reload sit in the opposite order to the PRI-C's, so the same focus buys a
  different gun.

## Where

- Catalog row: Primary, held-trigger rate and deep clip (2026-08-27 recut), pierce, far falloff start, second-longest reach, blue tracer — `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` (`Def`, `WeaponId.Mr35Rifle`)
- Pierce ray: reduced damage per later body, corpses skipped without spending pierce, geometry stops the ray — `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`FireRay`); `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`DamageOnPiercedBody`)
- Pin charge counts hits only, weapon-scoped, with a decay window — `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`PinHitThreshold`, `PinBuildDecaySeconds`, `PinBuildState`)
- One charge per shot, on the first damaged body only — `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`FireRay`, `RegisterPinHitIfNeeded`)
- Auto-pin on a ready meter held for a living body; manual pin on the binding; dial buys pin seconds — `Assets/Scripts/Game/Player/PlayerAbility.cs` (`RegisterPinHit`, `TryManualPin`); `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`Magnitude`)
- Ring order and declared identity (strength Damage, weakness FireRate) — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`RingOf`, `TryIdentity`)

## Gaps

- Pierce is never explained in play. Nothing tells you a shot continues through bodies, so most players never deliberately line targets up — which is the weapon's best trick.
- Pierce is limited to a set number of bodies past the first, stops on solid geometry, and is not consumed by corpses. None of this is visible.
- Its falloff starts much further out than the shotgun's and further out again on the PRI-C. Nothing states that the three Primaries begin losing damage at three different distances, so "no falloff" is what players conclude.
- Its reach is long — second only to the PRI-C's — but there is no cue for where it ends, and past that point shots do nothing at all rather than reduced damage.
- The blue tracer is a deliberate identity cue for this weapon. It is not a different damage type or ammunition.
- Its structural weakness is Hunters at contact range, which is not apparent at the ARMORY where the panel only shows damage, cadence and clip.
- Its ability, the Pin Shot, charges across first-body hits on the rifle (not per living
  target). Same-target build could not ready once TTK fell below 3.
- The upgrade wheel can be focused on Fire Rate to close some of the cadence gap that makes close-range fights hard with this weapon, but nothing at the ARMORY frames the ring as a choice with that consequence.
