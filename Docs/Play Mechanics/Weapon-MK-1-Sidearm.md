<!--
Purpose: MK-1 Sidearm, an instance of Weapons — differences only.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
-->

# MK-1 Sidearm

Instance of [Weapons](Weapons.md).

The free Secondary that still works at distance: one accurate round per pull, no spread and no pierce, out to most of a corridor. Its damage starts falling off well inside that reach, and its ability speeds up the slot's own clip refill rather than anything the shot does.

## Game

The reach Secondary. Free, and still bought, at the ARMORY. The other free Secondary is SEC-D, which trades reach for contact.

Early, it is the whole loadout if I picked reach. Later, it is the bridge: what I fire while the Primary is empty. Each slot keeps its own clip, so swapping and continuing to shoot beats standing still through a reload with something walking at me — and with this one, the bridge still works at distance.

One accurate round per pull. No scatter, no pierce. A miss is a whole round of nothing, so it rewards a planted stance and a placed shot more than either Primary does. Its best work is finishing bodies that are nearly done, which saves the Primary's clip for something that needs all of it.

Poor at volume. Small clip, modest damage, and everything on this hull gets tougher every Wave while this does not. Deep in a Boarding, the same body takes visibly more pulls.

Wrong tool for a Thief. Emptying this into one, reload by reload, is usually slower than its walk to the Hatch. I have run that race. The Thief won.

## Differs

- The reach Secondary: one accurate round per pull — a single pellet, no spread, no
  pierce — at a range that covers most of a corridor. Damage starts falling off well
  inside that reach, and past its end a shot does nothing. Where the lines fall:
  [Field Manual](Field-Manual.md).
- Free at the ARMORY (catalog cost 0), and still a purchase.
- **Regen Surge**, its ability: while the surge runs, the Secondary slot's passive clip
  refill ticks at a fixed multiplier, and only while the MK-1 is the active slot. The
  Ability spoke stretches the surge's duration, never its rate.
- Its ring runs strength Reload, weakness Damage, with Ability adjacent to Reload — a
  readiness focus feeds Regen Surge — and Damage's neighbours exclude Clip, so no single
  purchase compounds damage with clip on this gun.

## Where

- Catalog row: Secondary, cost 0, single pellet, no pierce, falloff inside reach — `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` (`Def`, `WeaponId.Mk1Sidearm`)
- Surge multiplies the slot refill at a fixed rate, active slot only; dial is duration — `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`Mk1SurgeMultiplier`, `Magnitude`); `Assets/Scripts/Game/Player/PlayerGun.cs` (`TickSecondaryRegen`); `Assets/Scripts/Game/Player/PlayerAbility.cs` (`ActivateRegenSurge`)
- Ring order and declared identity (strength Reload, weakness Damage) — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`RingOf`, `TryIdentity`)
- Cost 0 is a real purchase, affordable at an empty bank — `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWeapon`)

## Gaps

- It is free but must still be bought. A new campaign that never opens the ARMORY fights the entire Bastion with the improvised cutter for no stated reason.
- Its main long-term value is as a swap target during your Primary's reload. Nothing in the game suggests using the Secondary this way.
- Swapping to it mid-reload cancels the Primary's reload progress permanently, so the swap is only free if you were not already reloading.
- Falloff starts well inside its reach and past maximum range it does nothing, with no visible cue where either line is. Where they fall: [Field Manual](Field-Manual.md).
