<!--
Purpose: SEC-D, an instance of Weapons — the contact Secondary, no ability.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31
Where:
- Catalog row: Secondary, cost 0, short range with near-muzzle falloff, burst clip, `AbilityId.None` — `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` (`Def`, `WeaponId.SecD`)
- Centred Ability purchase refused on an empty-ability weapon — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`SpokePurchasable`)
- Ability binding refuses on it — `Assets/Scripts/Game/Player/PlayerAbility.cs` (`TryActivate`)
- Ring order and declared identity (strength FireRate, weakness Reload; Ability seated beside FireRate by design) — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`RingOf`, `TryIdentity`)
- The self-refill it leans on is the Secondary slot's, not its own — `Assets/Scripts/Game/Player/PlayerGun.cs` (`TickSecondaryRegen`)
-->

# SEC-D

Instance of [Weapons](Weapons.md).

The free Secondary for whatever has already closed: the shortest reach of any weapon, dead at the distance the MK-1 works comfortably at, but a fast burst up close. It leans on the slot's slow self-refill rather than its reload, and it is the only weapon in the shop with no ability at all.

## Game

The contact Secondary. Free at the ARMORY. The other free Secondary is the MK-1, which trades contact for reach.

It dumps a regenerating pool into whatever has already found me. Inside a few metres it hits hard. At the distance the MK-1 works comfortably, it is a dead trigger. It answers things that arrived, and nothing else.

Swap to it when something is on me, not when something is still coming down the corridor. Empty the pool into the body in front of me, let the quiet refill it. The refill is slow against how fast it empties, and it does not care that I am in a hurry.

Paired with a reach Primary it is the answer to a Hunter in my face. Paired with the BR-12 it commits the whole loadout to knife range — a decision about the entire Boarding, not about this gun.

Wrong tool for a Thief. Everything is, at this range, for as long as that takes.

It carries no ability, and the spoke where one would live cannot be bought. The wheel keeps drawing the empty seat. I have stopped asking who it is for.

## Details

### Structure

Secondary. Catalog cost 0, and still a purchase. One pellet per pull, no pierce. The shortest reach of any weapon in the shop.

### Fire

A dump-and-refill economy: a small clip at a high rate empties in a fraction of the MK-1's burst, its manual reload is the slower of the two Secondaries, and the pool comes back through the Secondary slot's self-refill — slow against how fast the pool empties.

Falloff begins a few strides out. At the MK-1's working distance it is a dead trigger. It answers things that have already closed, and nothing else. Wrong tool for a Thief.

Where the lines fall: [Field Manual](Field-Manual.md).

### Ability

None. The ability binding refuses on it. The upgrade wheel refuses a centred purchase on its Ability spoke — the spoke stays on the ring, and neighbour awards still land there.

### Rules

Its ring runs strength Fire Rate, weakness Reload, with Ability beside Fire Rate — a cadence focus dumps one neighbour award into the unpurchasable spoke — and Clip away from Fire Rate, so rate cannot compound with clip on its burst.

## Additional Details

### Gaps

- Ability is blank until authored; the Ability spoke cannot be bought. Without an ability it is a gun with a ring, not a full build.
- Kill-share vs BR-12 needs a logged Boarding with SEC-D equipped.
- Codename until identity is solid.
