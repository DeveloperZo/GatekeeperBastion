# 02 — Systems

The loops the game runs on, how strongly they drive (#7), what balances them (#8), the
delays inside them (#9), and the stocks and geometry they move (#10).

**This is the tier between design and tuning.** A pacing or difficulty complaint arrives
looking like a number problem and usually is not one — the largest effects on record in
this project came from geometry and from loop structure, not from stats. `Tuning.md` holds
the constants; this file holds the reasons a constant is the wrong lever.

Everything here is measured or derived, and everything unmeasured says so. Two balance
models have already been falsified by play data; one is kept below, marked wrong, so it is
not re-derived.

---

## The loops (#7 gain, #8 balance)

Four loops. Two drive, two damp, and the player holds the gain knob on one of them.

### R1 — kills → salvage → upgrades → faster kills

The progression loop, and the only reinforcing loop in the meta.

`AwardKill` banks `floor(SalvagePerKill(Scavenging) × TetherSiphon.RewardMultiplier)` per
body. Salvage buys weapon Tiers (~30% a step, linear cost ramp) and attributes (+5%/level,
uniform). Better weapon, faster kills, more salvage.

**The loop does not close inside a run.** Purchases happen only in the Ship scene, so R1
closes once per run, not continuously — a run cannot spiral. That is a structural
decision, not a tuning one, and it is what makes a run's difficulty a fixed quantity the
moment you embark.

**And it does not close at all until you have died once.** Tiers are locked behind
`MetaUpgrades.UpgradesUnlocked == (PodEjections >= 1)`. A first run is played at whatever
the loop's gain was before the loop existed. Attributes are deliberately exempt — they are
the answer to dying, so they must be buyable before you have died (`01_Game.md` §3).

**Two curves in one shop, deliberately.** Weapons are the progression and must stay
reachable; attributes are a side investment and should get expensive. Weapon damage, fire
rate and clip left `MetaUpgrades` for `WeaponCatalog` + `WeaponTier` to make that split
structural rather than a matter of two slopes.

### R2 — the dial: the player's hand on the gain

`Sim/Meta/TetherSiphon.cs`. Raising the dial multiplies both reward and body count off the
same hand-authored table, so payout and pressure cannot drift apart. They did once, and
the result was +600% enemy HP for +50% money.

**The dial adds only Default boarders.** HP, group size and spawn gap ignore it. So R2
raises the gain on R1 by adding load to B1 and nothing else — one lever, one mechanism.

Changeable in Prep only. The player raises the gain before seeing the wave, which is what
makes it a stake rather than a reaction.

### B1 — kill rate against arrival rate

The loop that decides whether the door survives, and it is **much weaker than it looks.**

Killing faster does not straightforwardly reduce hatch damage, because hatch damage is
`arrivals × dwell` and raising dps cuts dwell while raising arrivals. Three wave-2
attempts, one purchase between each:

| attempt | bought | kill rate | effective | killed | reached hatch | duration | outcome |
|---|---|---|---|---|---|---|---|
| 1 | Clip L2 | 0.81/s | 28.2 HP/s | 58% | 15 | 27.5s | BREACH |
| 2 | Damage L3 | 0.96/s | 33.8 HP/s | 70% | 19 | 29.2s | BREACH |
| 3 | FireRate L3, dial 0 | 0.93/s | 33.1 HP/s | 83% | 22 | 31.5s | BREACH |

Kill fraction rose 58% → 83% and arrivals rose 15 → 22, because surviving longer gives
more of the wave time to walk to the door. **The two nearly cancel.** This is why three
consecutive upgrade purchases produced three breaches, and why the answer was a
delivery-rate change (#10) rather than more dps (#12).

**Delivery rate is the lever that targets the mechanism.** Wave 2 at dial 0 delivered 1.74
bodies/s against a measured 0.93 kill rate:

| group gap | delivery | in/s | leak vs 0.93 kill/s |
|---|---|---|---|
| 2.47s (pre-M10) | 17.3s | 1.74 | +0.81/s |
| 3.00s | 21.0s | 1.43 | +0.50/s |
| 3.50s | 24.5s | 1.22 | +0.29/s |
| 4.00s | 28.0s | 1.07 | +0.14/s |

M10 shipped `max(0.4, 4.2 − 0.20×wave)`. Earlier softens (−0.13/wave from −0.55, then
dropping the dial's gap term) were not enough after the horde and swing-landing fixes.

**Kill rate is not constant across a run.** Measured 1.23 / 1.50 / 1.67 per second on
waves 3 / 4 / 5 against an earlier band of 0.61–1.08/s. Denser waves mean less time walking
between targets, so part of each wave's escalation is offset by the shooter getting more
efficient inside it.

### B2 — hatch damage → repair spend → slower progression

Repair costs banked salvage as healing is applied (`RunSim.RepairHatch`, 2 salvage/HP), and
that spend is gone if you die afterward. So damage taken at the door is paid for out of R1.

**Repair discipline is a real choice with a real price.** The three-deaths pacing target
below holds before repair costs; a player who repairs fully each wave narrows the margin to
1.7× at wave 2 and 1.2× at wave 3, and fails at waves 4–5. Unlimited repair is a choice
that costs progression.

---

## The delays (#9)

Delays are what make the loops readable, and two of them are load-bearing.

| Delay | Length | What it does |
|---|---|---|
| Deep breach → hatch | ~15s (48.5 m of z at 3.2 m/s patrol) | The single largest structural quantity in the fight. See #10 below. |
| R1 closing | once per run | A run's difficulty is fixed at embark; no in-run spiral. |
| R1 unlocking | until the first death | Tiers are gated on `PodEjections >= 1`. |
| Inter-wave gap on a deep start | 3s | A start-wave of N chains waves 1..N back to back with this gap. |
| Reload | 1.5s, fixed at every level | Clip and rate ramp together, so the duty cycle is 66.7% at every level. |
| Stamina refill | 1s hold + 100/18 = 6.6s | Sprint (7.5 m/s) beats the 5.6 chase and the Hunter's 6.5, so unlimited sprint would be free kiting. |

**The five stamina numbers change together or not at all.** They encode a single
relationship — sprint must outrun a chase for a bounded time — and moving one of them
silently converts kiting from a resource into a tactic.

---

## Stocks and structure (#10)

### Geometry beats stats here

The floor is 30 × 52 m. The hatch is at z −20.5; breaches sit at z 9 and z 28. The deep
breach is **48.5 m of z from the door, about 15 seconds at the 3.2 m/s patrol speed.**

Enemy stats barely register against that. This is the project's clearest case of #10
beating #12 outright, and it is the first thing to check when a wave feels wrong: a change
to where bodies enter moves more than a change to what they are.

**Still in no model:** travel time across the derelict for the *player*, aiming accuracy,
and repair downtime.

### The hatch's boarder-second budget

The door has a budget and it is measured, not predicted. One parked Default deals
`4 / 0.7 = 5.71` dps at a **60% landing rate**, so 3.43 effective.

```
budget (boarder-seconds) = hatchHP / 3.43     300 -> 88    345 -> 101    375 -> 109
spend  (boarder-seconds) = SUM over arrivals of seconds spent standing at the door
```

Attempt 3 above spent roughly **220 boarder-seconds against a budget of 101.**

**Landing rate is the most load-bearing number in this file and the least replicated.**
Measured twice on 2026-08-03 — 34.2 HP/s and 35.1 HP/s with ~10 parked, against a
theoretical 57.1 — and once at ~33% on the pre-fix build. *Unknown beyond those samples;
needs more play data.*

**Where the budget went — two correct changes that compound.** The horde retune tripled
bodies and cut hatch damage to a third: break-even only if landing holds. The swing-landing
fix then took landing ~33% → ~60%. Together `3 × (1/3) × 1.8 = 1.82×` the pre-horde hatch
pressure, against a hatch that grew 300 → 345 (`1.15×`). The door is **1.58× weaker
relative to its wave** than before either change.

**Hatch Armor is a buffer (#11) and buffers do not fix this.** One level is `+15 HP =
+4.4 boarder-seconds` against a ~120 boarder-second deficit.

**The door collapses non-linearly once arrivals accumulate.** In the 2026-08-07 T1 run the
Hatch went 224 → 148 → 72 → breached in **6.4 seconds** with eight or more bodies parked
on it, which is the budget behaving as modelled.

### The stocks

| Stock | Filled by | Drained by | Buffer size |
|---|---|---|---|
| Hatch HP | `RunSim.RepairHatch`, paid from banked salvage | boarder dwell × landing rate | `Tuning.md` |
| Banked salvage | `AwardKill`, immediately on each kill | repair, and Ship purchases | unbounded |
| Player HP | CREW QUARTERS Vitality between runs only | boarder swings | `Tuning.md` |
| Stamina | regen after a 1s delay | sprint | `Tuning.md` |
| Clip | reload | firing | `Tuning.md` |

**Banked salvage is the only stock that survives a run.** Dying takes nothing back and
clearing takes nothing back; the two endings differ only in which counter ticks.

---

## The pacing target — no wave costs more than three deaths

**Stated intent:** a player who dies repeatedly on a wave should be able to buy past it
within **three failed runs**. It is a constraint on R1's gain, and it is what sized the
Tier curve.

Required effective dps per wave assumes a survivable leak of 0.15 bodies/s and the measured
0.74 realization factor (theoretical dps → dps actually delivered):

| wave | in/s | avg HP | need HP/s | need theoretical | cheapest Tier | cumulative cost |
|---|---|---|---|---|---|---|
| 1 | 0.86 | 30.0 | 21 | 29 | T0 | 900 |
| 2 | 1.07 | 35.8 | 33 | 45 | T1 | 1,300 |
| 3 | 1.38 | 49.3 | 61 | 82 | T3 | 2,700 |
| 4 | 1.88 | 60.9 | 105 | 142 | T6 | 6,300 |
| 5 | 2.16 | 75.5 | 151 | 205 | T7 | 7,900 |

Against three failed runs' earnings at that wave (a run dying on wave N banks the waves
below it plus ~60% of N, at 25/kill and dial 0):

| die on | earns/run | ×3 | needs | margin |
|---|---|---|---|---|
| wave 2 | 900 | 2,700 | 1,300 | 2.1× |
| wave 3 | 1,830 | 5,490 | 2,700 | 2.0× |
| wave 4 | 3,060 | 9,180 | 6,300 | 1.5× |
| wave 5 | 4,590 | 13,770 | 7,900 | 1.7× |

**Every wave clears inside two deaths' earnings** before repair costs. B2 narrows this —
see above.

**After M23 the target assumes the player brought the rig.** A player without it cannot
repair at any price, so these margins apply to a loadout rather than to the game. The
rigless case has no numbers at all and should not be guessed at; it is the first thing a
logged run after M23 should report.

**Under three Tiers this failed outright.** The old ceiling was 82 theoretical dps against
wave 4's 142 and wave 5's 205: no amount of earning bought past them, because the content
was sized beyond the shop. Seven Tiers is the fix, and this table is the check to re-run
whenever either curve moves.

---

## Five-wave session — measured 2026-08-07

**The player's report comes first, because it is the only evidence about the experience:**
Zo — an expert player — reports the session was **fun and semi-challenging**. Everything
below is instrumentation, and none of it overrides that.

Two runs, same player, same weapon, same start wave, dial 0, twenty minutes apart
(`Logs/GatekeeperBastion_2026-08-07_16-34-30.log`):

| | MR-35 **T1** | MR-35 **T4** |
|---|---|---|
| wave 2 | 272/300 | 300/300 |
| wave 3 | breached | 300/300 |
| wave 4 | — | 300/300 |
| wave 5 | — | 300/300 |
| repair spent | 56 | 0 |
| outcome | lost on wave 3 after 134s | DERELICT STRIPPED, gross 5250 |

### What this does not tell us

An earlier version of this section read these numbers as proof the curve was broken. That
was wrong and is withdrawn. See `01_Game.md` §2 — hatch remaining measures leakage, not
effort, and the metric saturates.

Specifically unsupported by this data: that the curve is bimodal (n=2, one player, skill
uncontrolled); that wave 5 is too easy; that the wheel would have one correct spoke; that
anything needs reopening. The T1 and T4 numbers differ, and the reason could be the Tier,
the player's second attempt at the same content, or both — the session cannot separate them.

---

## Kill rate vs delivery rate — the model that failed

Intent: an unupgraded player clears wave 1 and dies on wave 2; a fully upgraded one clears
all three. The model below was the gate used to justify that. **It is wrong and is kept
only so it is not re-derived.** `pool = (D×30 + H×20 + R×120) × hpMultiplier`,
`TTK = pool / dps`, unupgraded 32 dps and maxed 62.5:

| wave | count (D/H/R) | HP pool | delivery | TTK @32 | TTK @62.5 |
|---|---|---|---|---|---|
| 1 | 18 (18/0/0) | 540 | 13.0s | 16.9s | 8.6s |
| 2 | 30 (26/4/0) | 1075 | 17.3s | 33.6s | 17.2s |
| 3 | 42 (34/6/2) | 2070 | 18.7s | 64.7s | 33.1s |

Checked against ten logged sessions in `_meta/Logs/` (2026-08-02/03, pre-horde build:
6/10/14 boarders at 90 HP, so wave 1's pool is identical at 540 and compares directly).
Both halves fail, in opposite directions.

**32 dps is ~2.5× what a player sustains.** Effective HP/s = wave pool ÷ kill span,
unupgraded and after the prefab-damage fix: **6.5 / 9.7 / 21.3** on waves 1/2/3 (session
`22-03-39`) and **12.6 / 14.9** (`22-24-08`). The 66.7% duty cycle is a ceiling: the player
also walks, turns, waits on pathing, and picks targets.

**"Behind by N seconds" is not a loss condition, so the comparison decides nothing.** A
wave has no deadline — delivery is how long the spawner takes to place bodies, not how long
the player has to remove them. Logged wave 1 clears ran **22.8s to 78.8s** against the same
13.0s of delivery, every one a win.

### What actually ends a run

`HATCH BREACHED`, in every valid loss on record (`22-24-08` wave 2, `22-53-43` wave 3), with
the player still alive. **Two sessions are not evidence at all**: `20-40-29` and `21-06-48`
log `[Player] took 4 damage`, the prefab-drift build where boarders hit at 22% of design —
the untouched-hatch triple clear came from there.

---

## Archetypes to watch

**Fixes that fail.** A symptom treated with a number returns at a new value, and each pass
lowers the urgency to look at the structure under it. Three upgrade purchases against wave 2
produced three breaches; the fix was the group gap.

**POSIWID — the purpose of a system is what it does.** Stated purpose: defend the hatch. For
several sessions the logs showed the hatch finishing runs at 300/300 while the player cleared
everything untouched. Revealed purpose: a shooting gallery with a scoreboard. Rules fixed
that; numbers had not.

**Measuring what is easy rather than what matters.** Hatch remaining is cheap to log and
saturates. See `01_Game.md` §2; this is an open problem, not a solved one.

---

Constants and their derivations: `Tuning.md`. What the loops are for: `01_Game.md`.
