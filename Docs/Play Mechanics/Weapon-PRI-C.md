<!--
Purpose: PRI-C, an instance of Weapons — differences only.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
-->

# PRI-C

Instance of [Weapons](Weapons.md).

The Primary for the far half of the room: the longest reach in the game, the latest falloff, and the deepest pierce, so one line fired up a corridor is what it is for. It pays for that with the slowest cadence and smallest clip of the Primaries, and its ability arms a single shot to pierce further at full damage, which lapses if it is not fired.

## Game

The far-wall Primary. Full damage across the whole floor, at distances the MR-35 has already given up on. Nothing else I have carried treats the far half of the room as workable.

Slow. Slow enough that a late Wave will drown me if this is all I fire. It is empty by the time I turn around, so a Hunter in my face is the Secondary's job — always, not usually.

It is strongest where I never stand. Leave the Hatch. Fire up the bore when bodies stack in its mouth. One pierced line up a corridor is what the rate is paying for, and the line is worth the wait.

Its ability arms the next shot to pierce further at full damage, and lapses unfired. Arming it and then not firing is the most common way I waste it. The gun forgives distance and punishes indecision.

Its wheel orders clip and reload differently from the MR-35's, so the same focus buys a different gun. Strength is damage. Weakness is rate. It knows what it is.

## Differs

- The far-wall Primary: the longest reach in the game, and the furthest-out falloff
  start of any weapon in the catalog — further out than either other Primary's whole
  reach. The catalog row's own note is that this puts the line beyond the Bastion's
  indoor sightlines; that is the authored intent, not something the code can be read to
  prove. Where the lines fall: [Field Manual](Field-Manual.md).
- The deepest standing pierce of the Primaries, keeping most of the shot on each later
  body — the bore payoff, bought with the slowest cadence and smallest clip of the
  three. Depth and the per-body cut: [Field Manual](Field-Manual.md).
- **Railshot**, its ability: arms the next shot to pierce further — a dial-bought count
  of bodies — at full damage on every one, overriding the standing pierce depth and its
  per-body cut. An armed shot lapses unfired after a few seconds, and the armed window
  only counts down while armed.
- Its ring runs strength Damage, weakness Fire Rate; Clip and Reload sit in the opposite
  order to the MR-35's, so the same focus buys a different gun.

## Where

- Catalog row: Primary, slowest cadence and smallest Primary clip, longest reach, highest falloff start in the catalog, deepest standing pierce (3 bodies at the highest per-body cut) — `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` (`Def`, `WeaponId.PriC`)
- Railshot overrides pierce count and per-body cut at full damage for one shot — `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`Fire`, `FireRay` overrides); `Assets/Scripts/Game/Player/PlayerGun.cs` (`ArmRailshot`, `TryConsumeRailshot`)
- Armed shot lapses unfired; the window ticks only while armed — `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`PriCRailshotArmedSeconds`); `Assets/Scripts/Game/Player/PlayerGun.cs` (`ClearExpiredRailshot`, `Tick`)
- Dial buys bodies pierced at full damage — `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`Magnitude`); `Assets/Scripts/Game/Player/PlayerAbility.cs` (`ActivateRailshot`)
- Ring order, declared identity, and Clip/Reload swapped against the MR-35 so rings stay unique — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`RingOf`, `TryIdentity`, `CanonicalRingKey`)

## Gaps

- Playthrough still needs a logged Boarding standing at the Hatch vs the same shots on the MR-35.
- Codename until identity is solid.
