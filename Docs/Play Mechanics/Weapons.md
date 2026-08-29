<!--
Purpose: Current representation of the weapon system as it is in the build — Ship-only buy and equip, the two gun slots with their own clips, reload and swap, per-weapon fire behaviour and abilities, the upgrade-wheel purchase, and how a shot is aimed under the Ship's isometric rig.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# Weapons

A Boarder carries two guns, a Primary and a Secondary, bought and equipped at the ARMORY
on the Ship between Boardings. Aboard the Bastion the loadout is fixed: the fight keys
only move between the slots already carried, ammunition is infinite, and a reload costs
time and nothing else. Each gun also has its own upgrade wheel, and a purchase spends the
realm's banked Salvage into that one gun for good, with no refund and no way to move
points to another weapon. The shop weapons differ in the shape of their fire, and all but one
carry an ability of their own.

## Game

Everything about the gun in my hands was decided on the Ship, and the Bastion honors the spec exactly. The fight-time keys only move between the slots I already carry.

A Primary and a Secondary. The Drone bay holds a payload, not a gun, and the payload is FABRICATION's. Guns are the ARMORY's, and only the ARMORY's.

Ammunition is infinite. A reload bills time and nothing else. Each slot keeps its own clip, so a dry Primary is not a crisis — it is a swap. The swap throws away a reload in progress, which has cost me more than it should have, more than once.

Every shop weapon but one carries an activated ability on the same key, and how hard it lands is bought on that weapon's own wheel. A purchase moves several spokes at once, weighted toward the one I centre. It never moves reach, spread, pierce, or anything on a weapon I am not holding. Money into a gun stays in the gun. The wheel is a marriage.

My Primary answers one of the room's three questions well and makes the other two harder. There is no gun that answers all three. I have looked. I keep looking.

## The interaction

A Boarder buys, equips and upgrades guns at the ARMORY, on the Ship, between Boardings;
aboard the Bastion the fight-time keys only move between the slots already carried.
Bastion code never calls the equip command, so the loadout a Boarding starts with is the
loadout it ends with, pushed into the scene once at Boarding start. The Boarder begins on
the Primary when one is carried, otherwise on the Secondary.

**Buying and equipping.** Paying a weapon's price from the realm's banked pot makes it
owned for good and equips it into its own slot at once. A weapon belongs to exactly one
slot, permanently, and once more than one weapon per slot is owned the ARMORY is where
the slot is filled. Both Secondaries cost nothing and are still bought — the free
purchase is a real purchase.

Until some weapon is owned every shot comes from the **improvised cutter**, a
deliberately weak fallback at a fraction of the damage and cadence. The same fallback
covers an active slot holding no weapon, though a slot key that names an empty slot is a
no-op, so a Boarding cannot swap into one.

**The upgrade-wheel purchase** (owned by this page; [Upgrades](Upgrades.md) links back). Every
shop weapon has its own five-spoke **upgrade wheel** — Reload, Clip, Damage, Fire Rate,
Ability — in a ring order unique to that weapon. The player centres one spoke as the
**upgrade focus** and pays; the purchase pays out by ring distance — most to the centred
spoke, less to each neighbour, nothing to the two furthest — so a weapon's spoke adjacency
is its identity. The price of a purchase rides how many have already centred that spoke,
on a cheaper lane for Secondaries; **Tier** is derived from total points, and ten
purchases cap a weapon. A purchase moves only what the five spokes name: range, spread,
pellet count, pierce, falloff and slot are frozen at the catalog row, and points never
transfer between weapons.

The wheel refuses a purchase in three cases:

- Any purchase, until this realm has earned any Salvage at all — a lifetime figure, so
  spending the bank to zero cannot re-lock the wheel.
- A centred purchase on SEC-D's Ability spoke; neighbour awards still land on it.
- In the demo, a purchase past the two rungs of the ladder's metered budget.

The demo's other rule is a *buy* lock rather than a wheel lock: the ARMORY's Primary rows
stay shut until a Boarding has entered the last Wave of zone 1, or the zone-2 bank has
been lost. Award, steps and prices: [Field Manual](Field-Manual.md).

**Reload and swap.** Ammunition is infinite; a reload bills time only and always fills
the clip. Each slot has its own clip and its own reload timer, stowed slots never tick,
and firing on an empty clip starts the reload on its own. The interrupt is asymmetric:

- A reload started on an empty clip cannot be cut short by fire — the trigger does
  nothing until it finishes.
- A reload resumed on a clip that still had rounds is cancelled by the shot, which fires
  from what was left.

Switching slots is instant, and by default it discards an in-progress reload on the
outgoing slot (an options setting keeps the progress instead). The Secondary slot refills
its own clip without reloading, whether it is in hand or stowed, but never while a manual
reload is running on it.

**Firing and the ability.** One fire method branches on the weapon's catalog fields —
pellets, pierce, falloff — never a subclass per weapon. Every shop weapon but SEC-D
carries one activated ability, all on the same binding and all sharing **one** cooldown
timer on the rig. The duration written onto that timer is the active weapon's catalog
figure, so swapping weapons hands the Boarder the cooldown already running, not a fresh
charge. How hard the ability lands is that weapon's Ability spoke, and at zero points it
still works at its floor. Critical hits are unreachable in this build: the chance the
roll reads is a hardcoded zero, so the per-pellet roll can never land.

**Aiming under the isometric rig.** The gun holds the same mode flag every other component
does — the rig's presence and enabled state, null or disabled meaning first person
([Moving](Moving.md) holds that account). While it is live, two things about a shot change
and nothing else does.

The **muzzle moves to the body** and the direction is resolved off the cursor ray rather
than the camera forward: the first live body along that ray wins and the shot is aimed at
its collider centre, bare geometry is aimed at the cursor's surface point, and open space
is aimed down the ray at cast range. The spread axis is rebuilt around whatever comes
back. And **the cursor lock stops being a precondition for firing**, because a free cursor
is the working state there. Pellets, pierce, falloff, damage, the clip and the ability are
untouched; it is the aim that is different. Whether the rig is authored and enabled in the
shipped hub is scene wiring and is not readable here.

Driven by the ARMORY panel on the Ship and, in the fight, the slot keys — the two gun
slots, the bay, a last-slot key and the mouse wheel, a key naming an empty slot doing
nothing — plus Reload, Fire and the weapon-ability binding (bindings:
[Field Manual](Field-Manual.md)). Shown by the
ARMORY rows and upgrade wheel, and the HUD's slot strip, clip bar, ability line and per-weapon
crosshair. Reads back as `bought … — equipped …`, `bought … focus … (+…, +…)`,
`switched to …` and `reload … discarded at … s remaining` log lines, and the visible
clip and cooldown counts.

## Consequences

Which Primary and which Secondary is the shape of the fight: each Primary answers one of
the room's three questions — things that come to the Boarder, things that will not, the
far half of the room — and makes the other two harder ([Gatekeepers](Gatekeepers.md) for who asks
them).

The upgrade wheel deepens that commitment: the money goes permanently into one gun, and
the same focus buys a different gun on a different ring, so the loadout is the build.
Swap discipline is real economy — a dry Primary is a swap, not a crisis, and a swap
thrown mid-reload pays that reload twice.

Removal test: without weapons there is no way to kill a Gatekeeper, so no Salvage, no
Ante worth raising and no Hatch worth holding — a different game outright; removing only
the upgrade wheel removes the build half of loadout expression.

## Classification

Core, loop. Serves Expression and Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16
rank stamp), with Challenge as the vehicle. Expression is the loadout as the player's
statement; Fantasy is the Boarder's kit.

## Interacts with

- [Salvage](Salvage.md) — every purchase on this page spends the realm's banked pot, the
  buys here being this page's own spends; that page owns what a kill is worth, and
  [Realms](Realms.md) owns the pot.
- [Upgrades](Upgrades.md) — one lock (this realm has earned any Salvage) covers this page's
  upgrade wheel and that page's equipment slot; that page owns every permanent purchase that
  is not a gun.
- [A Boarding](A-Boarding.md) — the loadout freezes at embark, and the kills that fund the
  next ARMORY visit pay on landing; that page owns kills-pay-on-landing.
- [The Drone](The-Drone.md) — the third slot on the weapon strip is a bay, not a gun: it
  shares the slot keys and HUD strip, and fire on it is placement; that page owns the bay.
- [Protocols](Protocols.md) — protocol bursts multiply fire rate through whatever gun is in
  hand, the strongest zone aura winning rather than stacking; that page owns the sheets and
  their windows.
- [Gatekeepers](Gatekeepers.md) — pierce, pellet cones and reach are priced against how each
  kind closes, the kinds' refusals being what make the three questions three; that page owns
  targeting.

## Instances

Instances vary on slot class (Primary or Secondary), fire shape — a cone of pellets that
stop on the first body, a shot that pierces a line, and where falloff starts and reach
ends — and ability. PRI-C and MR-35 share the pierce axis; what clears them as separate
instances is behaviour, their distinct abilities — Railshot arms a lapsing full-damage
pierce, Pin Shot is hit-charged — which is the contract's instance bar.

No instance may vary these, which are rules (`../Design/3-Mechanics.md` §3.1–3.2):

- the upgrade wheel's award arithmetic, or the freeze on everything the five spokes do
  not name;
- the one-binding ability rule;
- the per-slot clip and reload.

Figures: [Field Manual](Field-Manual.md).

| Instance | Differs in | State | Page |
|---|---|---|---|
| MK-1 Sidearm | reach Secondary; one accurate round per pull; Regen Surge | in build | [MK-1 Sidearm](Weapon-MK-1-Sidearm.md) |
| SEC-D | contact Secondary; burst that lives off the slot's self-refill; no ability, centred Ability purchase refused | in build | [SEC-D](Weapon-SEC-D.md) |
| BR-12 Shotgun | Primary; cone of pellets, each stopping on the first body it touches; Concussive Breach | in build | [BR-12 Shotgun](Weapon-BR-12-Shotgun.md) |
| MR-35 Rifle | Primary; held-trigger pierce down a line; Pin Shot | in build | [MR-35 Rifle](Weapon-MR-35-Rifle.md) |
| PRI-C | Primary; deeper pierce at full damage across the whole floor; Railshot | in build | [PRI-C](Weapon-PRI-C.md) |

## Where

- Buy pays banked Salvage, auto-equips into the def's slot, ownership permanent; cost 0 is a real purchase — `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWeapon`)
- Equip is Ship/ARMORY-only; Bastion never calls it — `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`EquipWeapon`)
- Loadout pushed once at Boarding start; begin on Primary when carried, else Secondary — `Assets/Scripts/Game/Bastion/GameController.cs` (`ApplyMetaEffects`)
- One slot per weapon; the five shop weapons and their slots — `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` (`Def`, `ShopWeapons`, `IsAvailable`); `Assets/Scripts/Sim/Meta/WeaponSlot.cs`
- Slot select on the two gun slots, the bay, a last-slot key and the mouse wheel; an unconfigured slot is a no-op — `Assets/Scripts/Game/Player/PlayerGun.Slots.cs` (`TickSlotInput`, `SelectSlot`, `CycleSlot`); `Assets/Scripts/Game/Input/Controls.cs` (`SlotPrimary`, `SlotSecondary`, `SlotDrone`, `SlotLast`)
- Per-slot clip and reload; only the active slot's timer advances — `Assets/Scripts/Game/Player/PlayerGun.Reload.cs` (`TickReload`, `SlotIsReloading`)
- Swap discards reload progress by default; an options setting keeps it — `Assets/Scripts/Game/Player/PlayerGun.Reload.cs` (`ApplyReloadOnSwap`); `Assets/Scripts/Game/Settings/Settings.cs` (`DiscardReloadOnSwap`, default true)
- The interrupt asymmetry: fire on an empty clip starts the reload and is refused; fire on a resumed reload that still had rounds cancels it and shoots — `Assets/Scripts/Game/Player/PlayerGun.cs` (`TryFire`, the `ClipRemaining <= 0` return before `CancelActiveReload`); `Assets/Scripts/Game/Player/PlayerGun.Reload.cs` (`CancelActiveReload`)
- Secondary slot self-refills in hand or stowed, and pauses under a manual reload on that slot; a surge multiplies the rate only while the Secondary is active — `Assets/Scripts/Game/Player/PlayerGun.cs` (`TickSecondaryRegen`); `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`SecondaryRegenPerSecond`)
- One fire method branches on pellets / pierce / falloff — `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`Fire`, `FireRay`); falloff arithmetic `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`FalloffFactor`)
- Ability on one binding, refused on an ability-less weapon; ONE cooldown timer on the component, whose duration is the active weapon's catalog figure — a swap inherits the running clock — `Assets/Scripts/Game/Player/PlayerAbility.cs` (the single `_cooldown` field, `Tick`, `TryActivate`, `BeginCooldown`); magnitudes and cooldowns `Assets/Scripts/Sim/Meta/WeaponAbility.cs` (`Magnitude`, `Cooldown`)
- Upgrade-wheel award by ring distance; rings per weapon and unique; only the five spokes move — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`Award`, `RingOf`, DamageMul–FireRateMul)
- Upgrade-wheel command and its named refusals — `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWheelUpgrade`)
- Spoke prices per centred count, cheaper Secondary lane; ten-purchase cap — `Assets/Scripts/Sim/Meta/WeaponTier.cs` (`Cost(`WeaponSlot`, `int`)`, `MaxTier`)
- Upgrade-wheel lock until the realm has earned Salvage (lifetime figure) — `Assets/Scripts/Sim/Meta/MetaUpgrades.cs` (`UpgradesUnlocked`)
- SEC-D centred Ability purchase refused; neighbour awards still land — `Assets/Scripts/Sim/Meta/WeaponWheel.cs` (`SpokePurchasable`)
- Demo: the ARMORY's Primary rows are locked on the BUY path, not the wheel path, and open on the last Wave of zone 1 entered or the zone-2 bank lost — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`PrimaryRowsOpen`, `PrimaryRowsWave` = `DemoZones.EndWave(1)`); `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWeapon`'s Primary refusal); the two wheel rungs `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWheelUpgrade` demo counters)
- Improvised cutter when no weapon is owned, and when the active slot holds none — `Assets/Scripts/Game/Player/PlayerGun.cs` (`UsingSalvageCutter`, `CutterDamageFactor`, `EffectiveDamage`); rate factor `Assets/Scripts/Game/Player/PlayerGun.Slots.cs` (`EffectiveFireRate`, `CutterRateFactor`)
- Under the isometric rig the shot is aimed from the body along the cursor ray — first live body wins at its collider centre, else the surface point, else cast range — and the cursor lock stops being required to fire — `Assets/Scripts/Game/Player/PlayerGun.cs` (`_isoRig`, `IsoAimActive`, `CanFire`'s `&& !IsoAimActive`); `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`Fire`'s `IsoAimActive` branch, `ResolveIsoAimDirection`); the ray and its cast range `Assets/Scripts/Game/Ship/IsometricCameraRig.cs` (`ScreenRay`), `Assets/Scripts/Sim/Ship/ShipCamera.cs` (`FollowDistance`) — whether the rig is authored and enabled in the shipped hub is scene wiring and not readable here, the mirror carrying no `.unity` files
- Crit is unreachable: the chance the roll reads is a hardcoded 0 on the gun, and `MetaUpgrades.CritChance()` is frozen at 0 with no caller — the roll still runs per pellet — `Assets/Scripts/Game/Player/PlayerGun.cs` (`CritChance` property); `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` (`Fire` pellet loop, `RollCrit`)

## Gaps

- Ammunition is infinite. The game shows a clip counter but never states that there is no reserve pool.
- Tiers are per weapon and do not transfer. The ARMORY never warns you that buying a second Primary restarts the investment.
- Weapon Tiers are locked until the first Hatch conversion. The panel says "locked" but what unlocks it is not explained.
- An upgrade purchase moves several spokes at once. Nothing explains the ring, and nothing tells you that range, spread, pellet count, pierce and falloff are frozen at purchase.
- FABRICATION sells the Decoy into the Drone bay. Selecting an empty bay still looks identical to an input that failed to register.
- The improvised cutter is never named; if you skip the free sidearm, you are just holding a weak gun.
- Equipped weapons cannot be changed once a Boarding starts. The game does not tell you this before you commit at the NAV MAP.
- Firing on an empty clip auto-starts the reload.
- Gatekeeper health scales up as a Boarding goes deeper while your weapon does not.
- Crit chance is frozen at nothing. Pellets would each roll if that ever changed; nothing in the game says so.
- The crosshair shape is a weapon identity cue, not an accuracy readout.
