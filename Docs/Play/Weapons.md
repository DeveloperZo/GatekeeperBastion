<!-- Purpose: how weapons work as a system — slots, buying, equipping, Tiers, reloading, switching, and how to choose. / Status: working / Update trigger: when a weapon is added to or removed from the shop, when Tiers change what they scale, or when slot/reload behaviour changes. / Last updated: 2026-08-06 -->

# Weapons

Everything about the gun in your hands is decided on the Ship. Nothing about it can be changed once you are on the Bastion.

For damage, clip, cadence, range and prices, read the [Field Manual](Field-Manual.md) — it is generated from the build. This page is about what those numbers mean and what you can do about them.

## Two slots

You carry a **Primary** and a **Secondary**. That is the whole loadout you can fill. There is a third slot, the **Drone**, but no payload exists yet, so nothing can go in it and selecting it does nothing — if pressing a slot key seems to do nothing at all, that is what happened.

A weapon belongs to exactly one slot, permanently. A Secondary can never be your Primary; a Primary can never be your Secondary. **Which Secondary and which Primary** are both loadout decisions: the pairing is the shape of the fight (ranged + close, pure marksman, committed brawler).

You can carry a Primary and a Secondary at once, and you can also run with only the Secondary. If you own no weapon at all, you still fire something: the **improvised cutter**, a scratch-built fallback that does a fraction of the damage at a fraction of the cadence. It is not bought, cannot be upgraded, and exists only so you are never standing in a corridor unable to shoot. Buy the sidearm and it goes away.

## Buying and equipping

Both happen at the **ARMORY**, and only at the ARMORY.

Buying is one action: pay the Salvage, and the weapon is yours **and immediately equipped into its own slot**. There is no separate equip step for a first purchase. Ownership is permanent — nothing is ever consumed, lost on death, or left behind on the Bastion.

Once you own more than one Primary, the ARMORY is where you pick which of them fills the Primary slot. The panel has a Primary/Secondary toggle; it shows you the weapon currently in that slot, its Tier, and the rows you can buy or equip.

**The equip choice is fixed for the whole Run.** Nothing in the Bastion can change which weapon is in a slot — the fight-time keys switch *between* slots, never *what is in* them. Decide what kind of fight you are walking into before you walk into it.

Your first Secondary costs nothing and is still bought at the ARMORY — MK-1 or SEC-D, both free. That is deliberate: the game wants your first lesson to be where weapons come from, and which Secondary you take is already a build choice.

## What an upgrade purchase does

Every weapon has its own **upgrade wheel**, bought at the ARMORY with Salvage. Its upgrade spokes are **Reload**, **Clip**, **Damage**, **Fire Rate** and **Ability**, arranged in a ring. You centre one spoke as your **upgrade focus**, and a purchase pays out by distance around the ring: most to the spoke you centred, less to each of its two neighbours, and nothing to the two furthest away. Your Tier is derived from the total points you have bought. The award, the per-point steps and the price of each purchase are all in the [Field Manual](Field-Manual.md).

So a purchase moves several spokes at once, weighted by what you chose. Damage, clip, reload time and fire rate all move; which of them move *most* is the decision.

A wheel purchase **never** changes:

- **range**, **damage falloff**, **pellet count**, **spread**, or **pierce** — the shape of the weapon is fixed at purchase;
- which **slot** it goes in;
- anything about **any other weapon you own**.

**The MK-1 and SEC-D have no rings authored yet**, so they cannot be upgraded. The BR-12, MR-35 and PRI-C have wheels.

That last point is the expensive one. **Upgrade wheels are per weapon and never transfer.** Salvage poured into the shotgun does nothing for the rifle. Buying a second Primary means starting its wheel from nothing while your first Primary sits at whatever you paid for. If you are going to switch Primaries, switch early, before the investment gets deep.

The number of purchases has a ceiling, and the step price climbs as you go up. Weapon upgrades are also **locked until you have been Lost at least once** — you cannot buy one on a campaign that has never lost a Run, and the same gate holds the equipment slot at FABRICATION. Buying the weapons themselves is not locked, and neither are the CREW QUARTERS attribute tracks. If the upgrade buttons read as locked, that is why, and the only key is a death.

## Abilities

Every weapon carries one activated ability, on the same key for all of them. It has a
cooldown, and the HUD names it and counts the cooldown down.

- **BR-12 — Concussive Breach.** Shoves Gatekeepers back out of a cone in front of you.
- **MR-35 — Pin Shot.** **It has never fired as shipped** — the condition that arms it
  cannot be met in the build you are playing, so treat the rifle as having no usable
  ability until that is fixed.
- **MK-1 — Regen Surge.** While it runs, the Secondary's passive clip regeneration runs
  faster. Its duration is what the Ability spoke buys.

**How hard an ability hits depends on the Ability spoke of that weapon's upgrade wheel.**
At zero points invested it still works; it is simply at its floor. Since the MK-1 has no
ring authored, its Regen Surge is permanently at that floor.

An ability cannot be used mid-reload.

## Reloading

There is no ammo count and no reserve. Ammunition is infinite; the only thing a reload costs you is time, and time is the whole resource.

- Each slot has **its own clip and its own reload**. Emptying your Primary does not touch the Secondary's clip.
- Reloading always fills the clip completely. There is no partial top-up and no penalty for reloading with rounds left.
- **The Secondary refills itself**, slowly, without reloading, and faster while an MK-1 Regen Surge is running. The Primary does not. The rate is in the [Field Manual](Field-Manual.md).
- A manual reload is refused when the clip is already full.
- Firing on an empty clip **starts the reload for you**. You do not need to notice.
- Reload progress is not shown as a number, but the HUD shows it filling.

The judgement this asks for: know roughly where you are in the clip and break contact *before* you are dry, not after. A weapon that empties fast and refills slowly wants you to back off around a corner and reload on your own schedule. A Gatekeeper closing on you does not care that the reload was nearly done.

## Switching slots

Switching is instant — there is no draw animation, no delay, nothing to wait out. There are slot keys, a cycle control, and a return-to-last-slot key; see the [Field Manual](Field-Manual.md). Keys pointed at an empty slot do nothing, and the cycle control skips empty slots, so on a sidearm-only Run every switch input is silently ignored.

**Switching cancels an in-progress reload and throws away the progress** (default). The HUD
says so while you are reloading if another slot holds a weapon, and the log names the slot and
how much time was lost. An options setting can keep the progress instead; a stowed reload never
advances on its own, and firing with rounds still in the clip cancels a resumed reload.

Which means the swap is a real tool and a real trap:

- **The tool.** With a Primary and a Secondary, a dry Primary is not a crisis. Swap to the sidearm and keep firing instead of standing still through a long reload. That is what the Secondary is for.
- **The trap.** Swapping *away* mid-reload and swapping back means you paid the reload twice — unless you turned the discard setting off. Either commit to the reload or commit to the swap.

You begin a Run on your Primary if you have one, otherwise on the Secondary.

## Choosing a Primary

The Bastion asks three questions at once and your Primary answers one of them well.

- **Hunters** run straight at you, arrive fast, and die fast. They close the distance for you. A weapon that hits hard up close and does not need precision answers them.
- **Rams** walk to the Hatch, ignore you completely, cannot be provoked, and soak punishment. Shooting one changes nothing about it except its health. You want to start killing it while it is still far from the Hatch, which means reach and damage per shot.
- **Defaults** march on the Hatch and turn on you only if you hurt them — and hurting one shouts, pulling its nearby group onto you. They come in the largest numbers and they line up in corridors, which is where a shot that continues through bodies pays.

So: the **BR-12 Shotgun** is the answer to things that come to you. The **MR-35 Rifle** is the answer to things that do not. The **PRI-C** is the answer to the far half of the room that neither owns after M31. Whichever you take, the other questions get harder, and that is the decision.

Two practical notes:

- **Wave depth is a factor.** Gatekeepers get tougher every Wave of a Run while your Tier does not move mid-Run. A Primary that clears a body in one trigger pull early may need two later. Plan for the late Wave, not the first one.
- **Critical hits come only from Luck** at CREW QUARTERS, never from a weapon or a Tier, and no weapon has a headshot or a weak point. Crits are rolled per pellet, so a multi-pellet weapon gets more rolls per trigger pull than a single-shot one.

Cosmetically, each weapon has its own tracer colour. Crosshair shapes are shared — only the BR-12's brackets are unique, and the MK-1 and MR-35 both use the cross. **The crosshair shape does not describe accuracy** — a wide bracket crosshair is not a spread indicator.

## Gaps

- Ammunition is infinite. The game shows a clip counter but never states that there is no reserve pool, so it reads like an ammo economy that does not exist.
- Tiers are per weapon and do not transfer. The ARMORY shows the Tier next to each weapon but never warns you that buying a second Primary restarts the investment.
- Weapon Tiers are locked until your first death. The panel says "locked" but the fact that the *only* key is being Lost — not Salvage, not Waves cleared — is not explained.
- An upgrade purchase moves damage, clip, reload, fire rate and the ability together, weighted by the spoke you centred. Nothing explains the ring, and nothing tells you that range, spread, pellet count, pierce and falloff are frozen at purchase and never move.
- The Drone slot exists and is empty until a payload ships. Pressing its key is a no-op that looks identical to an input that failed to register.
- The improvised cutter is never named as a thing you are choosing; if you skip the free sidearm, you are just holding a weak gun with no explanation of why.
- Equipped weapons cannot be changed once a Run starts. The game does not tell you this before you commit at the NAV MAP.
- Firing on an empty clip auto-starts the reload, so a "jammed" trigger is usually just an unnoticed reload.
- Gatekeeper health scales up as a Run goes deeper while your weapon does not, so the same weapon at the same Tier is quietly worse on the last Wave than on the first.
- Critical hits are rolled once per pellet, which makes the Luck track worth measurably more on a multi-pellet weapon than on a single-shot one. Nothing surfaces this.
- The crosshair shape is a weapon identity cue, not an accuracy readout, and it does not uniquely identify the weapon — two of the three share one.
