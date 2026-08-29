# Field Manual



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
| `ESC` | pause | in the bastion |
| `BKSP` | controls | in the bastion, on the ship, end screen |

**FIGHT**

| Key | Does | Live when |
|---|---|---|
| `LMB` | fire | in the bastion |
| `RMB` | zoom | in the bastion, on the ship |
| `R` | reload | in the bastion |
| `1` | weapons | in the bastion |
| `2` | weapons | in the bastion |
| `3` | weapons | in the bastion |
| `Q` | last | in the bastion |
| `WHEEL` | cycle | in the bastion |
| `F` | ability | in the bastion |
| `G` | crew | in the bastion |
| `H` | self | in the bastion |

**BOARDING**

| Key | Does | Live when |
|---|---|---|
| `T` | start | between waves |
| `+` | ante up | between waves |
| `-` | ante up | between waves |
| `ENTER` | return | end screen |
| `SPACE` | return | end screen |
| `ESC` | return | end screen |
| `TAB` | mission | in the bastion |

**ARMORY**

| Key | Does | Live when |
|---|---|---|
| `[` | wheel | on the ship |
| `]` | wheel | on the ship |

**HATCH**

| Key | Does | Live when |
|---|---|---|
| `E` | hold repair | near the hatch |

**SHIP**

| Key | Does | Live when |
|---|---|---|
| `E` | interact | on the ship |
| `ESC` | settings | on the ship |

## Weapons

You carry one **Primary** and one **Secondary** at once. The Drone bay holds a payload — today the **Decoy**, sold at FABRICATION.
The Primary is chosen at the ARMORY before you embark and is fixed for the Boarding;
switching in the bastion swaps between the slots you already carry, never between
owned weapons. Each slot keeps its own clip, so switching is not a free reload.

| Weapon | Slot | Cost | Damage | Shots/s | Clip | Reload | Range | Notes |
|---|---|---|---|---|---|---|---|---|
| **MK-1 Sidearm** | Secondary | 0 | 30 | 3 | 8 | 1.4 s | 45 m | falls off past 12 m; free — always affordable |
| **SEC-D** | Secondary | 0 | 18 | 6 | 4 | 2.2 s | 8 m | falls off past 3 m; free — always affordable |
| **BR-12 Shotgun** | Primary | 900 | 19 ×7 | 1.2 | 6 | 2.6 s | 14 m | 7 pellets, 7° spread; falls off past 8 m |
| **MR-35 Rifle** | Primary | 1,200 | 16 | 8 | 27 | 2.46 s | 60 m | pierces 2; falls off past 25 m |
| **PRI-C** | Primary | 1,500 | 180 | 0.6 | 4 | 3.2 s | 90 m | pierces 3; falls off past 55 m |

### Upgrade wheel — what a purchase buys

Upgrades are **per weapon**, not global. One purchase awards **5 / 3 / 3 / 0 / 0** by ring distance (11 points). Tier is derived as total / 11. Cadence still reads as the
weapon: fire-rate points exist, but a purchase chooses a spoke. Purchases unlock
once this realm has earned any Salvage at all. Full focus on one spoke is **50** points; each neighbour reaches **30**.

| attribute | per point | at full focus (50) | at neighbour (30) |
|---|---|---|---|
| damage | ×1.05 | ×9.3 | ×3.81 |
| clip | ×1.03 | ×4.04 | ×2.31 |
| reload | ×0.98 | ×0.37 | ×0.55 |
| fire rate | ×1.01 | ×1.79 | ×1.42 |

Each Primary purchase on one spoke costs 400 / 600 / 800 / 1,000 / 1,200 / 1,400 / 1,600 / 1,800 / 2,000 / 2,200 salvage — **13,000** to take one weapon all the way (10 purchases).
Each Secondary purchase on one spoke costs 150 / 300 / 450 / 600 / 750 / 900 / 1,050 / 1,200 / 1,350 / 1,500 salvage — **8,250** cumulative.

## What comes aboard

Four kinds, and each is defined by what it **refuses** to do. Every kind swings on
the same 0.7 s clock — they differ in what they hit, never in how often.

| Kind | HP | Hits you for | Hits the hatch for | Speed | Refuses |
|---|---|---|---|---|---|
| **Default** | 50 | 6 | 4 | 2.8 → 4.6 m/s when provoked | nothing — it will march, and it will turn on you if you hurt it |
| **Hunter** | 33 | 6 | — | 5.6 m/s | to touch the hatch; it only ever wants you |
| **Thief** | 200 | — | 20 | 2.2 m/s | to turn when shot; on a Convert it marches the Hatch, field-terminal it hunts you — it never hurts you |
| **Cloaker** | 75 | 6 | 4 | 2.8 → 4.6 m/s when provoked | to advance while watched — any Boarder's gaze stops it dead, and it fades and closes the moment every eye is elsewhere |

HP above is the base line. Later waves multiply it (see the wave table).

## The waves

**5 waves to a Boarding**, and none of them start until you press
`T` at the Hatch. A wave BEGINS at the Hatch and ENDS at a terminal: press `T` at the zone's
terminal while the wave runs and it shuts off, completing the zone and banking everything you
are carrying. Each zone runs its whole span back to back (zone 1 = 2 waves, the deeper zone = 3 waves). Prep after a clear lasts as long as you want — starting a Convert is legal there.
Gatekeepers arrive in groups, not a stream.

At Ante 0:

| Wave | Bodies | Default | Hunter | Thief | Cloaker | HP × | Arrive in groups of | Every |
|---|---|---|---|---|---|---|---|---|
| 1 | 11 | 11 | 0 | 0 | 0 | 1 | 3 | 7.3 s |
| 2 | 18 | 16 | 2 | 0 | 0 | 1.4 | 4 | 7.7 s |
| 3 | 25 | 20 | 4 | 1 | 0 | 1.8 | 5 | 8.1 s |
| 4 | 32 | 23 | 6 | 2 | 1 | 2.2 | 6 | 8.5 s |
| 5 | 39 | 26 | 7 | 4 | 2 | 2.6 | 7 | 8.9 s |

Hunters, Thieves and Cloakers are on a fixed schedule — the Ante never adds more of them.

## The Ante — the wager

Ante up at a field terminal, Master Control, or the Hatch (`+/-`), then `E` to select and `T` to start. Locked once a
wave is live. It pays more and sends more, and the extra bodies are **all Defaults** —
it cannot conjure a sixth Thief. It changes nothing else: no extra health, no tighter
spacing, no larger groups. Anteing up costs `100` banked Salvage per step — money you would otherwise take home.

| Ante | You earn | Wave 1 sends | Wave 5 sends |
|---|---|---|---|
| 0 | ×1.00 | 11 | 39 |
| 1 | ×1.10 | 12 | 43 |
| 2 | ×1.15 | 13 | 45 |
| 3 | ×1.25 | 14 | 49 |
| 4 | ×1.35 | 15 | 53 |
| 5 | ×1.50 | 17 | 59 |

## Salvage

**Gatekeepers are worth Salvage, and it is yours the moment the body falls.**
A kill banks straight to the realm at face value — no Convert stands between you and it,
and no ending can take it back. Nothing you have banked is ever at risk.

If you want more than face value you have to put some of it back on the table: hold `E`
at a full Hatch to **Withdraw**, then run a Convert Wave to bring it home multiplied.
That pile — and only that pile — is what a death or a Hatch breach can take.

A kill pays `42`, then multiplied by the Ante.
Nothing pays per second except the Convert drip (`8`/s), and the quiet between waves earns nothing at all.

The hatch is what you are defending: **600 HP** at Hatch Armor 0, up to **750**. Hold `E` near it to repair.
If it falls, the Boarding ends — whatever is banked stays banked. If you fall, you buy
yourself back out of the realm's bank: `150 × min(respawns taken + 1, 3)` — it climbs to 450 and stops. Anything you
staked on a Withdraw stands up with you untouched. There is one pot, and repair, the Ante
and your own returns all come out of it — which is what makes each of them a decision.

## The ship — where salvage goes

Between runs you walk your own ship. Four consoles, each owning one subject.

| Console | Owns | Sells |
|---|---|---|
| **ARMORY** | the weapon in your hands | weapons, and wheel purchases for whichever you carry |
| **CREW QUARTERS** | you | Default Protocol (STOCK; G Barrage; H Rush), VEIL Protocol (PHANTOM; G Ping; H Cloak; Ghost), FORGE Protocol (PARALLEL BAYS; G Field Deploy; H Overcharge; Magnet Hands) |
| **FABRICATION** | the ship | Hatch Armor, Equipment slot, Repair Rig, Decoy |
| **NAV MAP** | where you go | the destination, and embarking |

Hatch Armor runs 0–5 at +5% a level, costing 200 × 2^level — 200 / 400 / 800 / 1,600 / 3,200.

| Track | Level 0 | Level 5 |
|---|---|---|
| Hatch Armor — hatch HP | 600 HP | 750 HP |
| Equipment slot (once, after this realm earns anything) | — | 300 salvage |
| Repair Rig — flat repair rate | 8 HP/s | 300 salvage |
| Decoy — Drone payload | 3 charges | 250 salvage |

Crit chance is frozen at 0. There is no Luck shop. Protocols are CREW claims:
Default Protocol, VEIL Protocol, FORGE Protocol share one panel, Default at 0 salvage. AEGIS and LEDGER are not sold.

Weapon wheel purchases and the equipment slot unlock once this realm has earned any Salvage.

---

*Generated from the build by `Tools/docgen/run.sh`. What the game is, rather than
what it contains, is in [The Game](The-Game.md).*