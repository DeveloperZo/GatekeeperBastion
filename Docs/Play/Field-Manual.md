# Field Manual

<!-- GENERATED FILE — DO NOT EDIT BY HAND.
     Produced by Tools/docgen/run.sh from the live code: Controls.cs,
     WeaponCatalog, WeaponTier, WaveDirector, TetherSiphon, MetaUpgrades.
     Re-run it after any change to those files; a hand edit here is lost.
     Prose about what the game IS lives in The-Game.md and is hand-written. -->

Everything you can press, carry, fight and buy — read straight out of the build.
If this file and the game disagree, the file was not regenerated; the game is right.

## Controls

The game shows these to you in play — a panel in the bottom-left lists whatever is
live right now, so this table is a reference, not something you have to memorise.

**MOVE**

| Key | Does | Live when |
|---|---|---|
| `WASD` | move | in the bastion, on the ship |
| `MOUSE` | look | in the bastion, on the ship |
| `SHIFT` | sprint | in the bastion, on the ship |
| `ESC` | free cursor | in the bastion, on the ship |
| `BKSP` | toggle panel | in the bastion, on the ship, end screen |

**FIGHT**

| Key | Does | Live when |
|---|---|---|
| `LMB` | fire | in the bastion |
| `R` | reload | in the bastion |
| `1` | weapons | in the bastion |
| `2` | weapons | in the bastion |
| `3` | weapons | in the bastion |
| `Q` | last | in the bastion |
| `WHEEL` | cycle | in the bastion |

**RUN**

| Key | Does | Live when |
|---|---|---|
| `T` | siphon draw | between waves |
| `[` | siphon | between waves |
| `]` | siphon | between waves |
| `ENTER` | return | end screen |
| `SPACE` | return | end screen |
| `ESC` | return | end screen |

**HATCH**

| Key | Does | Live when |
|---|---|---|
| `E` | hold repair | near the hatch |

**SHIP**

| Key | Does | Live when |
|---|---|---|
| `E` | interact | on the ship |

## Weapons

You carry one **Primary** and one **Secondary** at once, plus an empty melee slot.
The Primary is chosen at the ARMORY before you embark and is fixed for the run;
switching in the bastion swaps between the slots you already carry, never between
owned weapons. Each slot keeps its own clip, so switching is not a free reload.

| Weapon | Slot | Cost | Damage | Shots/s | Clip | Reload | Range | Notes |
|---|---|---|---|---|---|---|---|---|
| **MK-1 Sidearm** | Secondary | 0 | 14 | 3 | 8 | 1.4 s | 45 m | free; falls off past 12 m |
| **SEC-D** | Secondary | 0 | 18 | 6 | 4 | 2.2 s | 8 m | free; contact only; falls off past 3 m |
| **BR-12 Shotgun** | Primary | 900 | 6 ×7 | 1.2 | 6 | 2.6 s | 14 m | 7 pellets, 7° spread; falls off past 8 m |
| **MR-35 Rifle** | Primary | 1,200 | 32 | 1.6 | 10 | 2.4 s | 60 m | pierces 2 at 0.26× after the first; falls off past 25 m |
| **PRI-C** | Primary | 1,500 | 90 | 0.6 | 4 | 3.2 s | 90 m | pierces 3 at 0.75× after the first; no indoor falloff |

### Tiers — what upgrading a weapon buys

Upgrades are **per weapon**, not global. Damage rises ×1.25 a Tier and clip ×1.15; fire rate never changes, because cadence is how a weapon reads. Tiers unlock
after your first death. Effective damage per second, every pellet connecting:

| Weapon | T0 | T1 | T2 | T3 | T4 | T5 | T6 | T7 |
|---|---|---|---|---|---|---|---|---|
| MK-1 Sidearm | 28 | 36 | 47 | 61 | 79 | 102 | 131 | 167 |
| BR-12 Shotgun | 33 | 44 | 57 | 73 | 94 | 122 | 157 | 201 |
| MR-35 Rifle | 37 | 48 | 62 | 80 | 102 | 131 | 167 | 214 |

Each Tier costs 400 / 600 / 800 / 1,000 / 1,200 / 1,400 / 1,600 / 1,800 / 2,000 / 2,200 salvage — **13,000** to take one weapon all the way.

## What comes aboard

Three kinds, and each is defined by what it **refuses** to do. Every kind swings on
the same 0.7 s clock — they differ in what they hit, never in how often.

| Kind | HP | Hits you for | Hits the hatch for | Speed | Refuses |
|---|---|---|---|---|---|
| **Default** | 30 | 6 | 4 | 3.2 → 5.6 m/s when provoked | nothing — it will march, and it will turn on you if you hurt it |
| **Hunter** | 20 | 6 | — | 6.5 m/s | to touch the hatch; it only ever wants you |
| **Ram** | 120 | — | 20 | 2.2 m/s | to notice you at all, even while you shoot it |

HP above is the base line. Later waves multiply it (see the wave table).

## The waves

**5 waves to a run**, and none of them start until you pull the
siphon draw. There is no timer: the quiet between waves lasts exactly as long as you
want it to. Gatekeepers arrive in groups, not a stream.

At the dial's lowest setting:

| Wave | Bodies | Default | Hunter | Ram | HP × | Arrive in groups of | Every |
|---|---|---|---|---|---|---|---|
| 1 | 18 | 18 | 0 | 0 | 1 | 3 | 4.2 s |
| 2 | 30 | 26 | 4 | 0 | 1.4 | 4 | 4 s |
| 3 | 42 | 34 | 6 | 2 | 1.8 | 5 | 3.8 s |
| 4 | 54 | 40 | 10 | 4 | 2.2 | 6 | 3.6 s |
| 5 | 66 | 47 | 12 | 7 | 2.6 | 7 | 3.4 s |

Hunters and Rams are on a fixed schedule — the dial never adds more of them.

## The siphon dial — the wager

Set between waves, locked once a wave is live. It pays more and sends more, and the
extra bodies are **all Defaults** — it cannot conjure a sixth Ram. It changes nothing
else: no extra health, no tighter spacing, no larger groups.

| Dial | You earn | Wave 1 sends | Wave 5 sends |
|---|---|---|---|
| 0 | ×1.00 | 18 | 66 |
| 1 | ×1.10 | 20 | 73 |
| 2 | ×1.15 | 21 | 76 |
| 3 | ×1.25 | 23 | 83 |
| 4 | ×1.35 | 24 | 89 |
| 5 | ×1.50 | 27 | 99 |

## Salvage

**Gatekeepers are worth money and the money is yours the instant they die.** Not at the
end of the wave, not if you survive — the moment they drop. Dying takes none of it
back, and clearing the bastion adds no bonus. A run you lose still paid you.

A kill pays `25` at Scavenging 0, up to `31.25` at 5, then multiplied by the dial.
Nothing pays per second, and the quiet between waves earns nothing at all.

The hatch is what you are defending: **300 HP** at Hatch Armor 0, up to **375**. Hold `E` near it to repair.
If it falls, the run ends. If you fall, the run ends. There is no other way out —
no bail-out, no escape lever, no respawn.

## The ship — where salvage goes

Between runs you walk your own ship. Four consoles, each owning one subject.

| Console | Owns | Sells |
|---|---|---|
| **ARMORY** | the weapon in your hands | weapons, and Tiers for whichever you carry |
| **CREW QUARTERS** | you | Vitality, Stamina, Engineering, Scavenging, Luck |
| **FABRICATION** | the ship | Hatch Armor, Repair Rig |
| **MAP** | where you go | the destination, and embarking |

Attribute and ship tracks run 0–5 at +5% a level, costing 200 × 2^level — 200 / 400 / 800 / 1,600 / 3,200.

| Track | Level 0 | Level 5 |
|---|---|---|
| Vitality — your HP | 100 | 125 |
| Stamina — sprint pool | 100 | 125 |
| Engineering — repair speed | ×1 | ×1.25 |
| Scavenging — salvage per kill | 25 | 31.25 |
| Luck — crit chance (double damage) | 0% | 25% |
| Hatch Armor — hatch HP | 300 | 375 |
| Repair Rig — repair rate | 8 HP/s | 10 HP/s |

Luck is the only source of critical hits in the game, on purpose: two sources would
multiply against each other and the numbers on the panel would stop meaning anything.

**Attributes are never locked.** They are the answer to dying, so you can buy them
before you have died. Weapon Tiers unlock after your first death.

---

*Generated from the build by `Tools/docgen/run.sh`. What the game is, rather than
what it contains, is in [The Game](The-Game.md).*
