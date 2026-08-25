# Weapons

<!--
Purpose: how weapons work as a system — slots, buying, equipping, Tiers, reloading,
switching, and how to choose. / Status: working / Update trigger: when a weapon is
added to or removed from the shop, when Tiers change what they scale, or when
slot/reload behaviour changes. / Last updated: 2026-08-20
-->

## Game

Everything about the gun in my hands was decided on the Ship, and the Bastion honors the spec exactly. The fight-time keys only move between the slots I already carry.

A Primary and a Secondary. The Drone bay holds a payload, not a gun, and the payload is FABRICATION's. Guns are the ARMORY's, and only the ARMORY's.

Ammunition is infinite. A reload bills time and nothing else. Each slot keeps its own clip, so a dry Primary is not a crisis — it is a swap. The swap throws away a reload in progress, which has cost me more than it should have, more than once.

Every shop weapon but one carries an activated ability on the same key, and how hard it lands is bought on that weapon's own wheel. A purchase moves several spokes at once, weighted toward the one I centre. It never moves reach, spread, pierce, or anything on a weapon I am not holding. Money into a gun stays in the gun. The wheel is a marriage.

My Primary answers one of the room's three questions well and makes the other two harder. There is no gun that answers all three. I have looked. I keep looking.

## Reference

### Two slots

You carry a **Primary** and a **Secondary**. That is the loadout you can fill. There is a
third slot, the **Drone** bay. It holds a payload, not a gun. Today that payload is the
**Decoy**, bought at FABRICATION.

A weapon belongs to exactly one slot, permanently. Which Secondary and which Primary is
the shape of the fight. You can boarding with only the Secondary. If you own no weapon at all,
you still fire something: the **improvised cutter**, a scratch-built fallback that does a
fraction of the damage at a fraction of the cadence. Buy the sidearm and it goes away.

### Buying and equipping

Guns happen at the **ARMORY**, and only at the ARMORY. The Decoy is sold at **FABRICATION**.

Pay the Salvage and the weapon is yours **and immediately equipped into its own slot**.
Ownership is permanent. Once you own more than one Primary, the ARMORY is where you pick
which fills the slot. The choice is fixed for the whole Boarding.

Your first Secondary costs nothing and is still bought at the ARMORY — MK-1 or SEC-D,
both free. Which Secondary you take is already a build choice.

### What an upgrade purchase does

Every shop weapon has its own **upgrade wheel**, bought at the ARMORY. Its spokes are
**Reload**, **Clip**, **Damage**, **Fire Rate** and **Ability**, arranged in a ring. You
centre one spoke as your **upgrade focus**, and a purchase pays out by distance around
the ring: most to the spoke you centred, less to each neighbour, nothing to the two
furthest away. Award, steps and prices: [Field Manual](Field-Manual.md).

A purchase never changes range, falloff, pellet count, spread, pierce, slot, or any other
weapon you own. **Upgrade wheels are per weapon and never transfer.**

Weapon upgrades are **locked until Salvage first converts through the Hatch**. Buying the weapons
themselves is not. If the buttons read locked, a death is the key. SEC-D's Ability spoke
cannot be bought; rotate off it.

### Abilities

Every shop weapon except **SEC-D** carries one activated ability, on the same binding for all
of them. The HUD names it and counts the cooldown. How hard it hits depends on that weapon's
Ability spoke. At zero points it still works, at its floor. An ability cannot be used
mid-reload. SEC-D has none; its Ability spoke cannot be bought.

- **BR-12 — Concussive Breach.** Shoves Gatekeepers back out of a cone in front of you.
- **MR-35 — Pin Shot.** Hits charge a meter. The shot that completes it pins whatever is
  still alive under the crosshair. A full meter can also be fired on the ability binding.
- **MK-1 — Regen Surge.** While it boardings, the Secondary's passive clip regeneration boardings
  faster.
- **PRI-C — Railshot.** The next shot pierces further, at full damage, then lapses if you
  do not fire.

### Reloading

There is no reserve. Ammunition is infinite; the only thing a reload costs you is time.

Each slot has its own clip and its own reload. Reloading always fills the clip. The
**Secondary refills itself**, slowly, without reloading, and faster while an MK-1 Regen
Surge is running. The Primary does not. Firing on an empty clip starts the reload for you.

### Switching slots

Switching is instant. Bindings: [Field Manual](Field-Manual.md). Keys pointed at an empty
slot do nothing.

**Switching cancels an in-progress reload and throws away the progress** (default). The
HUD says so while you are reloading if another slot holds a weapon. An options setting
can keep the progress instead.

The tool: a dry Primary is not a crisis — swap to the sidearm and keep firing. The trap:
swapping away mid-reload and swapping back means you paid the reload twice, unless you
turned the discard setting off.

You begin a Boarding on your Primary if you have one, otherwise on the Secondary.

### Choosing a Primary

The Bastion asks three questions at once and your Primary answers one of them well.

- **Hunters** boarding straight at you and die fast. A weapon that hits hard up close answers
  them.
- **Thieves** hunt you on a field-terminal Wave and walk to the Hatch on a Convert. Shooting
  them never shouts. You want reach and damage per shot.
- **Defaults** march on the Hatch, turn if you hurt them, and line up in corridors, which
  is where a shot that continues through bodies pays.

The **BR-12 Shotgun** is the answer to things that come to you. The **MR-35 Rifle** is the
answer to things that do not. The **PRI-C** is the answer to the far half of the room.
Whichever you take, the other questions get harder.

Gatekeepers get tougher every Wave of a Boarding while your Tier does not move mid-Boarding. Plan
for the late Wave. Crit chance is frozen at nothing. Crits, if they ever rolled, would
be per pellet.

## See also

[MK-1 Sidearm](Weapon-MK-1-Sidearm.md). [SEC-D](Weapon-SEC-D.md). [BR-12 Shotgun](Weapon-BR-12-Shotgun.md). [MR-35 Rifle](Weapon-MR-35-Rifle.md). [PRI-C](Weapon-PRI-C.md). [The Drone](The-Drone.md). [Upgrades](Upgrades.md).

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
