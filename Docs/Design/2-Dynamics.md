# 2 — Dynamics

**Dynamics** in the MDA sense: the run-time behaviour of the mechanics acting on player
input and on each other. Leverage #6 information flows, #7 loop gain, #8 balancing loops,
#9 delays.

Nobody designs a dynamic directly. You build a mechanic, and the dynamic is what happens.
So this file is where a design intention gets checked against what the game actually did,
and everything in it is measured or derived. Two balance models have already been falsified
by play data; one is kept below, marked wrong, so it is not re-derived.

**A pacing or difficulty complaint usually lands here rather than in `4-Data.md`.** The
largest effects on record in this project came from loop structure and from geometry, not
from stats.

---

## 2.1 The loop the player is actually in (#6)

Daniel Cook's skill atom: the player holds a **mental model**, takes an **action**, the
system responds with **feedback**, and the model updates. A mechanic the player cannot see
the result of is not in their loop at all — it is a number in a file.

Gatekeeper Bastion's atom, one Wave at a time:

```
model    "I can hold the door if I thin the group before it lands"
  |
action   press T, pick targets, repair, set the dial next time
  |
feedback hatch HP falling, gatekeepers parked on the door, the end screen, the log
  |
model'   "eight parked is already lost" / "the deep breach is the one that matters"
```

**Feedback is the return path, so #6 is a dynamics problem, not a presentation one.** A full
pass tuning gatekeeper damage 4 → 18 never reached the running game because the prefab held the
old values; what exposed it was a log line printing `took 4 damage`. Pure #12 work,
disconnected at the wire, invisible until feedback caught it.

Three channels, each with a rule:

| Channel | Rule | Where |
|---|---|---|
| The HUD | absolute `GUI` rects only in the Bastion, never `GUILayout`; a layout exception strands the player on the end screen. | `../Architecture.md` gotcha 3 |
| The log | a line when a player-visible thing happened, or when a refusal must be answerable from the log alone ("I clicked BUY and nothing happened"). Once per distinct reason, never per frame. No counters, no aggregation. | `../Working-Rules.md` |
| The docs | anything countable is generated from the code; anything arguable is written. | `../README.md` |

**The deliberate silences are audited.** Every page in `../Play/` ends with a `## Gaps`
section listing what the game never tells the player. That list is this tier's backlog: a
rule the wiki has to explain because the game keeps it secret is a rule the player is
playing without, which means it is not in their model and cannot change their action.

---

## 2.2 Loops, not arcs

Cook's distinction: a loop is a repeatable cycle played for mastery, and **an arc is a
broken loop you exit immediately** — consumed once, worth less on repeat.

**This game is currently all loop and no arc, and that is a position rather than an
oversight.** There is no story beat, no unlock sequence, no destination that changes what
a Run is. The two things that could become arcs are the first death (which unlocks upgrade
wheel purchases and the equipment slot, and is the only one-time event in the game) and the
NAV MAP's second destination, which does not exist yet.

*Whether a Run needs an arc is unknown and needs play data.* It is the kind of question
that should be asked after Fellowship or Expression is answered in `1-Experience.md`, not
before.

---

## 2.3 The four loops (#7 gain, #8 balance)

Two drive, two damp, and the player holds the gain knob on one of them.

### R1 — kills → Salvage → upgrades → faster kills

The progression loop, and the only reinforcing loop in the meta. `AwardKill` banks
`floor(SalvagePerKill(Scavenging) × TetherSiphon.RewardMultiplier)` per body; Salvage buys
upgrade wheel purchases (`Sim/Meta/WeaponWheel.cs`) and attributes (+5%/level).

**A purchase is 11 points spread over three adjacent spokes, so R1's gain is no longer one
number.** `ShipCommands.BuyWheelUpgrade` centres one upgrade spoke and awards
`FocusAward = 5` to it and `NeighbourAward = 3` to each neighbour, 0 to the far two:
`PointsPerPurchase = 11`, `MaxPurchases = WeaponTier.MaxTier = 10`. Points compound per
point, at `DamagePerPoint = 1.0456`, `ClipPerPoint = 1.0283`, `ReloadPerPoint = 0.98035`,
`FireRatePerPoint = 1.0117`. Ability carries no dps term.

Step size is therefore a function of where the focus lands. MR-35 (`WeaponCatalog`: damage
32, 1.6 shots/s, clip 10, reload 2.4s) with its spoke order `Reload, Damage, Clip, Ability,
FireRate` (`WeaponWheel.RingOf`), at `WeaponWheel.EffectiveDps` =
`clip × damage ÷ (clip ÷ rate + reload)`, is 10 × 32 ÷ (10 ÷ 1.6 + 2.4) = **36.99 dps**
before any purchase. One purchase, by centre:

| centre | points awarded | dps | step |
|---|---|---|---|
| Damage | Damage +5, Reload +3, Clip +3 | 48.15 | +30.2% |
| Reload | Reload +5, Damage +3, FireRate +3 | 44.56 | +20.5% |
| Clip | Clip +5, Damage +3, Ability +3 | 43.38 | +17.3% |
| FireRate | FireRate +5, Reload +3, Ability +3 | 39.22 | +6.0% |
| Ability | Ability +5, Clip +3, FireRate +3 | 38.94 | +5.3% |

Arithmetic on the constants above, not measured. The Damage row in full: damage
32 × 1.0456^5 = 39.99, clip `round(10 × 1.0283^3)` = 11, reload 2.4 × 0.98035^3 = 2.261,
rate unchanged at 1.6, so 11 × 39.99 ÷ (11 ÷ 1.6 + 2.261) = 48.15. The old flat "~30% a
step" holds as the best case only; the worst case is a sixth of it, for the same
400–2,200 Salvage (`WeaponTier.Cost` = 400 + 200 × currentTier over currentTier 0..9,
`Sim/Meta/WeaponTier.cs`).

**The loop does not close inside a Run.** Purchases happen only in the Ship scene, so R1
closes once per Run and a Run cannot spiral. That is a structural decision, not a tuning
one, and it is what makes a Run's difficulty a fixed quantity the moment you embark.

**And it does not close at all until you have died once.** Wheel purchases and the equipment
slot are gated on `MetaUpgrades.UpgradesUnlocked == (PodEjections >= 1)`. A first Run is
played at whatever the loop's gain was before the loop existed. Buying a weapon is not
gated, and attributes are deliberately exempt.

**Two cost curves in one shop, deliberately.** Weapons are the progression and must stay
reachable; attributes are a side investment and should get expensive. Damage, fire rate and
clip left `MetaUpgrades` for `WeaponCatalog`, `WeaponTier` (the cost curve) and
`WeaponWheel` (the multipliers) to make that split structural rather than a matter of two
slopes. CREW QUARTERS is four tracks now that M23 deleted Engineering:
`UpgradeTrack` is `HatchArmor, Vitality, Stamina, Scavenging, Luck`.

### R2 — the dial: the player's hand on the gain

`Sim/Meta/TetherSiphon.cs`. Raising the dial multiplies reward and body count off the same
hand-authored table, so payout and pressure cannot drift apart. They did once, and the
result was +600% Gatekeeper HP for +50% money.

**The dial adds only Default gatekeepers.** HP, group size and spawn gap ignore it. R2 raises
R1's gain by adding load to B1 and nothing else — one lever, one mechanism. Changeable in
Prep only, so the player raises the gain before seeing the Wave.

### B1 — kill rate against arrival rate

The loop that decides whether the door survives, and it is **much weaker than it looks.**

Hatch damage is `arrivals × dwell`, and raising dps cuts dwell while raising arrivals. Three
Wave-2 attempts, one purchase between each. The bought column names the pre-wheel
per-attribute levels; the measurement stands, the purchase shape it was bought with does not:

| attempt | bought | kill rate | effective | killed | reached Hatch | duration | outcome |
|---|---|---|---|---|---|---|---|
| 1 | Clip L2 | 0.81/s | 28.2 HP/s | 58% | 15 | 27.5s | BREACH |
| 2 | Damage L3 | 0.96/s | 33.8 HP/s | 70% | 19 | 29.2s | BREACH |
| 3 | FireRate L3, dial 0 | 0.93/s | 33.1 HP/s | 83% | 22 | 31.5s | BREACH |

Kill fraction rose 58% → 83% and arrivals rose 15 → 22, because surviving longer gives more
of the Wave time to walk to the door. **The two nearly cancel.** Three consecutive upgrade
purchases produced three spawn points, and the answer was a delivery-rate change rather than
more dps.

**Delivery rate is the lever that targets the mechanism.** Wave 2 at dial 0 delivered 1.74
bodies/s against a measured 0.93 kill rate:

| group gap | delivery | in/s | leak vs 0.93 kill/s |
|---|---|---|---|
| 2.47s (pre-M10) | 17.3s | 1.74 | +0.81/s |
| 3.00s | 21.0s | 1.43 | +0.50/s |
| 3.50s | 24.5s | 1.22 | +0.29/s |
| 4.00s | 28.0s | 1.07 | +0.14/s |

M10 shipped `max(0.4, 4.2 − 0.20×wave)`. Earlier softens were not enough after the horde and
swing-landing fixes.

**Kill rate is not constant across a Run.** Measured 1.23 / 1.50 / 1.67 per second on Waves
3 / 4 / 5 against an earlier band of 0.61–1.08/s. Denser Waves mean less time walking between
targets, so part of each Wave's escalation is offset by the shooter getting more efficient
inside it.

### B2 — Hatch damage → repair spend → slower progression

Repair costs banked Salvage as healing is applied (`RunSim.RepairHatch`, 2 Salvage/HP), and
that spend is gone if you die afterward. Damage taken at the door is paid for out of R1.

**A Boarder without the Repair Rig cannot repair at any price, so B2 does not exist for that
loadout.** M23 moved the rig out of the tracks and into Equipment:
`GameController.RepairHatch` returns 0 unless `MetaState.HasRepairRigEquipped`, which needs
the equipment slot bought (`EquipmentCatalog.SlotPrice` 300, itself gated on the first loss),
the rig owned (`EquipmentCatalog.Def(RepairRig).cost` 300) and the rig equipped;
`MetaState.RepairMissingStep` names whichever step is missing. Rigless, Hatch damage costs
nothing and damps nothing, and the Hatch only ever goes down.

**Repair discipline is a real choice with a real price, once you carry the rig.** The
three-deaths pacing target below holds before repair costs; a player who repairs fully each
Wave narrows the margin to 1.7× at Wave 2 and 1.2× at Wave 3, and fails at Waves 4–5.

---

## 2.4 The delays (#9)

Delays are what make the loops readable, and two of them are load-bearing.

| Delay | Length | What it does |
|---|---|---|
| Deep breach → Hatch | ~15s (48.5 m of z at 3.2 m/s patrol) | The largest structural quantity in the fight (`3-Mechanics.md`). |
| R1 closing | once per Run | A Run's difficulty is fixed at embark; no in-Run spiral. |
| R1 unlocking | until the first death | Wheel purchases and the equipment slot gated on `MetaUpgrades.UpgradesUnlocked` = `PodEjections >= 1`. Buying a weapon and buying attributes are not. |
| Inter-Wave gap on a deep start | 3s | A start-Wave of N chains Waves 1..N back to back. |
| Reload | per weapon: MK-1 1.4s, BR-12 2.6s, MR-35 2.4s | A wheel spoke (`ReloadPerPoint = 0.98035`), so it shortens with focus — except on the MK-1, which has no ring. |
| Stamina refill | 1s hold + 100/18 = 6.6s | Sprint beats the chase and the Hunter, so unlimited sprint would be free kiting. |

**The five stamina numbers change together or not at all.** They encode one relationship —
sprint must outrun a chase for a bounded time — and moving one silently converts kiting from
a resource into a tactic.

---

## 2.5 The pacing target — no Wave costs more than three deaths

**Stated intent:** a player who dies repeatedly on a Wave should be able to buy past it
within three failed Runs. It is a constraint on R1's gain, and it is what sized the Tier
curve.

Required effective dps per Wave assumes a survivable leak of 0.15 bodies/s and the measured
0.74 realization factor (theoretical dps → dps actually delivered):

| Wave | in/s | avg HP | need HP/s | need theoretical | cheapest Tier | cumulative cost |
|---|---|---|---|---|---|---|
| 1 | 0.86 | 30.0 | 21 | 29 | T0 | 900 |
| 2 | 1.07 | 35.8 | 33 | 45 | T1 | 1,300 |
| 3 | 1.38 | 49.3 | 61 | 82 | T3 | 2,700 |
| 4 | 1.88 | 60.9 | 105 | 142 | T6 | 6,300 |
| 5 | 2.16 | 75.5 | 151 | 205 | T7 | 7,900 |

**This table was derived pre-wheel and is kept as the record of what the seven-Tier decision
was sized on. Do not read the last two columns as current.** It assumed one Tier was one
fixed dps step for every buyer. Since M21 it is not: `WeaponWheel.DerivedTier` is
`TotalPoints / PointsPerPurchase`, which counts purchases and not what they bought, so two
players at Tier 10 having paid the same 13,000 Salvage (`WeaponTier.Cost` = 400 + 200 × tier,
cumulative 13,000) sit far apart. On MR-35 off a 36.99 base, ten purchases all centred on
Damage reach ~436 dps (arithmetic in `4-Data.md`) and a spread buy is **unknown, needs
re-derivation**. The pre-wheel seven-Tier record below still uses 7,000 / Tier 7.

Against three failed Runs' earnings at that Wave (a Run dying on Wave N banks the Waves
below it plus ~60% of N, at 25/kill and dial 0):

| die on | earns/Run | ×3 | needs | margin |
|---|---|---|---|---|
| Wave 2 | 900 | 2,700 | 1,300 | 2.1× |
| Wave 3 | 1,830 | 5,490 | 2,700 | 2.0× |
| Wave 4 | 3,060 | 9,180 | 6,300 | 1.5× |
| Wave 5 | 4,590 | 13,770 | 7,900 | 1.7× |

**Every Wave clears inside two deaths' earnings** before repair costs. B2 narrows this.

**M23 shipped, and the target now assumes the player brought the rig.** A player without it
cannot repair at any price, so these margins apply to a loadout rather than to the game. The
rigless case has no numbers and should not be guessed at; it is the first thing a logged Run
should report. Both prices are spend the table below does not carry: 300 for the slot plus
300 for the rig is 600 of R1's output before the first HP is healed.

**Under three Tiers this failed outright.** The old ceiling was 82 theoretical dps against
Wave 4's 142 and Wave 5's 205: no amount of earning bought past them, because the content was
sized beyond the shop. Raising the ceiling is the fix — seven purchases when this table was
derived, `WeaponTier.MaxTier = 10` today (`Sim/Meta/WeaponTier.cs`) — and this table is the
check to re-run whenever either curve moves.

---

## 2.6 Five-Wave session — measured 2026-08-07

**The player's report comes first, because it is the only evidence about the experience:**
Zo — an expert player — reports the session was **fun and semi-challenging**. Everything
below is instrumentation, and none of it overrides that.

Two Runs, same player, same weapon, same start Wave, dial 0, twenty minutes apart
(`Logs/GatekeeperBastion_2026-08-07_16-34-30.log`):

| | MR-35 **T1** | MR-35 **T4** |
|---|---|---|
| Wave 2 | 272/300 | 300/300 |
| Wave 3 | breached | 300/300 |
| Wave 4 | — | 300/300 |
| Wave 5 | — | 300/300 |
| repair spent | 56 | 0 |
| outcome | lost on Wave 3 after 134s | BASTION STRIPPED, gross 5250 |

**The door collapses non-linearly once arrivals accumulate.** In the T1 Run the Hatch went
224 → 148 → 72 → breached in **6.4 seconds** with eight or more bodies parked on it, which is
the gatekeeper-second budget behaving as modelled (`3-Mechanics.md`).

### What this does not tell us

An earlier version of this section read these numbers as proof the curve was broken. That was
wrong and is withdrawn. See `1-Experience.md`, section 1.3 — Hatch remaining measures leakage, not
effort, and the metric saturates.

Specifically unsupported: that the curve is bimodal (n=2, one player, skill uncontrolled);
that Wave 5 is too easy; that the upgrade wheel would have one correct spoke; that anything needs
reopening. The T1 and T4 numbers differ, and the reason could be the Tier, the player's
second attempt at the same content, or both — the session cannot separate them.

---

## 2.7 Kill rate vs delivery rate — the model that failed

Intent: an unupgraded player clears Wave 1 and dies on Wave 2; a fully upgraded one clears
all three. The model below was the gate used to justify that. **It is wrong and is kept only
so it is not re-derived.** `pool = (D×30 + H×20 + R×120) × hpMultiplier`, `TTK = pool / dps`,
unupgraded 32 dps and maxed 62.5:

| Wave | count (D/H/R) | HP pool | delivery | TTK @32 | TTK @62.5 |
|---|---|---|---|---|---|
| 1 | 18 (18/0/0) | 540 | 13.0s | 16.9s | 8.6s |
| 2 | 30 (26/4/0) | 1075 | 17.3s | 33.6s | 17.2s |
| 3 | 42 (34/6/2) | 2070 | 18.7s | 64.7s | 33.1s |

Checked against ten logged sessions in `../_meta/Logs/` (2026-08-02/03, pre-horde build:
6/10/14 gatekeepers at 90 HP, so Wave 1's pool is identical at 540 and compares directly). Both
halves fail, in opposite directions.

**32 dps is ~2.5× what a player sustains.** Effective HP/s = Wave pool ÷ kill span,
unupgraded and after the prefab-damage fix: **6.5 / 9.7 / 21.3** on Waves 1/2/3 (session
`22-03-39`) and **12.6 / 14.9** (`22-24-08`). The 66.7% duty cycle is a ceiling: the player
also walks, turns, waits on pathing, and picks targets.

**"Behind by N seconds" is not a loss condition, so the comparison decides nothing.** A Wave
has no deadline — delivery is how long the spawner takes to place bodies, not how long the
player has to remove them. Logged Wave 1 clears ran **22.8s to 78.8s** against the same 13.0s
of delivery, every one a win.

### What actually ends a Run

`HATCH BREACHED`, in every valid loss on record (`22-24-08` Wave 2, `22-53-43` Wave 3), with
the player still alive. **Two sessions are not evidence at all**: `20-40-29` and `21-06-48`
log `[Player] took 4 damage`, the prefab-drift build where gatekeepers hit at 22% of design —
the untouched-Hatch triple clear came from there.

---

## 2.8 Archetypes to watch

**Fixes that fail.** A symptom treated with a number returns at a new value, and each pass
lowers the urgency to look at the structure under it. Three upgrade purchases against Wave 2
produced three spawn points; the fix was the group gap.

**POSIWID — the purpose of a system is what it does.** Stated purpose: defend the Hatch. For
several sessions the logs showed the Hatch finishing Runs at 300/300 while the player cleared
everything untouched. Revealed purpose: a shooting gallery with a scoreboard. Rules fixed
that; numbers had not.

**Measuring what is easy rather than what matters.** Hatch remaining is cheap to log and
saturates. See `1-Experience.md`, section 1.3; this is open, not solved.

---

What produces these dynamics: `3-Mechanics.md`. What they are for: `1-Experience.md`.
