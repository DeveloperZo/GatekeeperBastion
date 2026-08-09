# 02 — Numbers

Every number here was read out of the source cited beside it. **Retuning means
re-deriving with this arithmetic, never adjusting by feel** — change a number and you
change the derivation and the tables in the same edit. Before tuning, clear the
leverage gate in `04_Agent_Rules.md`: name the tier and say why a higher one is wrong.

## Weapon — MK-1 Salvage Rifle

`Game/Player/PlayerGun.cs` (serialized defaults), `Sim/Meta/MetaUpgrades.cs` (pushed at run start).

| | L0 | L5 | | L0 | L5 |
|---|---|---|---|---|---|
| damage/shot | 12 | 15 | reload | 1.5s | 1.5s (fixed) |
| shots/second | 4 | 5 | range | 60 m | 60 m |
| clip | 12 | 15 | | | (rounds: 12/13/13/14/14/15) |

**Duty-cycle formula.** A cycle is `clip/rate` seconds of firing plus a fixed reload:

```
dps = (clip * damage) / (clip/rate + reload) = damage * rate * dutyCycle
L0:   (12 * 12) / (12/4 + 1.5) = 144 / 4.5 = 32.0 dps
L5:   (15 * 15) / (15/5 + 1.5) = 225 / 4.5 = 50.0 dps
```

Clip and rate ramp at the same +5%/level, so **the duty cycle is 3.0/4.5 = 66.7%
at every level** and dps is simply `damage x rate x 0.667`.

**Crit** (`PlayerGun.RollCrit`, `CritDamageMultiplier = 2`): average multiplier is
`1 + critChance`; at Luck 5 (0.25), `50 x 1.25 = 62.5 dps`. Only crit source in the game.
**No MK-1 → improvised salvage cutter**: 40% damage, 50% rate → 4.8 dmg at 2/s,
clip 12 → `57.6 / 7.5 = 7.7 dps`. A hard run, not a dead end.

## Player

`Game/Player/FirstPersonController.cs`, `Game/Player/PlayerHealth.cs`, `Game/Player/Interactor.cs`.

- 100 HP (L0; 125 at Vitality 5).
- Walk 5 m/s, sprint 7.5 m/s. No jump.
- Stamina 100 (125 at L5), drain 25/s, regen 18/s after a 1s delay →
  **4.0s of sprint = 30 m**; a full refill costs `1 + 100/18 = 6.6s`. Sprint beats the
  5.6 m/s chase and the Hunter's 6.5, so unlimited sprint would be a free kiting
  exploit. Change the five stamina numbers together or not at all.
- Interact (hold-E) range 3.2 m.

## Boarders — three kinds, each defined by what it REFUSES to do

`Sim/Waves/WaveDirector.StatsFor(kind)`, pushed onto each spawn by `Game/Enemies/WaveSpawner.cs`. All
three share one **0.7s swing clock**: kinds differ by what they hit, never by cadence.
HP 90 → 30 with 3x the bodies, so wave 1 is still **540 HP of boarder** (18 x 30 = the
old 6 x 90) — a redistribution, not an inflation.

| kind | HP | player dmg | hatch dmg | patrol | chase | refuses |
|---|---|---|---|---|---|---|
| Default | 30 | 6 | 4 | 3.2 | 5.6 | — |
| Hunter | 20 | 6 | **0** | 6.5 | 6.5 | the hatch (`ignoresHatch`) |
| Ram | 120 | **0** | 20 | 2.2 | 2.2 | the player (`ignoresPlayer`; never provoked) |

- **Default** 30 HP = **3 shots** (12/24/36) = `30/32 = 0.94s`. 6 dmg/0.7s = 8.6 dps
  each: three on you kill a 100 HP player in **3.9s**, five in **2.3s** (at the old
  18 dmg, five were 128.6 dps and **0.8s** — why damage fell as bodies tripled).
  Hatch 4/0.7s = 5.7 dps: one leaker needs 52s, so leaks are a crowd problem now.
- **Hunter** 20 HP = **2 shots**, 0.63s. 6.5 m/s beats the 5 m/s walk and loses to the
  7.5 m/s sprint — the first enemy that makes **Stamina** worth buying. Zero hatch
  damage: it cannot be answered by standing on the hatch.
- **Ram** 120 HP = **10 shots** = `120/32 = 3.75s` of focused fire while dealing
  `20/0.7 = 28.6` dps → `28.6 x 3.75 = 107` HP, a third of the 300 HP door, per Ram that
  got home *while being shot*. Intercepting it turns "shoot the closest" into "shoot the
  right thing"; it cannot hurt you, so it costs only the time the Hunters are using.
- Melee reach 2.4 m (player) / 3.5 m (hatch), +1.2 m hysteresis. Aggro: shot at,
  or seen within 26 m; a provoked boarder alerts others within 12 m; inside 8 m
  of the hatch a Default commits to the hatch regardless (a Hunter never does).

## Waves and the siphon dial

`WaveDirector.NextWave(waveIndex, siphonLevel)`; `MaxWaves = 3`. The dial is a
hand-authored table, never a formula — `TetherSiphon.RewardMultiplierFor` =
**1 / 1.10 / 1.15 / 1.25 / 1.35 / 1.50**.

```
dial                  = TetherSiphon.RewardMultiplierFor(siphonLevel)
enemyCount            = min(120, round((18 + 12*waveIndex) * dial))
hunterCount           = {0,4,6}[waveIndex]    ramCount = {0,0,2}[waveIndex]  (dial-proof)
defaultCount          = enemyCount - hunterCount - ramCount   (takes the whole surplus)
enemyHealthMultiplier = 1 + 0.25*waveIndex                       (x each kind's HP)
groupSize             = 3 + waveIndex                            (3 / 4 / 5)
spawnInterval         = max(0.4, 2.6 - 0.13*waveIndex)   SECONDS BETWEEN GROUPS
boarders, wave 1/2/3  L0 18/30/42  L1 20/33/46  L2 21/35/48
                      L3 23/38/53  L4 24/41/57  L5 27/45/63   run 90/99/104/114/122/135
```

**`spawnInterval` is a GROUP gap now — the name did not change with it.** A wave
lands in `(ceil(count/groupSize) - 1) * gap` = **13.0 / 17.3 / 18.7s**; read as a
per-individual gap the same waves take 44 / 72 / 96s and no fight ever starts. First
field to check if a wave feels empty. **The dial's surplus is all Default** (Hunter and
Ram counts are fixed per wave), so L5 wave 3 is 55 D + 6 H + 2 R: a bigger fight, not a
different one, and the dial cannot multiply the scariest enemy.

**One table drives reward AND body count, on purpose**: `WaveDirector` reads
`TetherSiphon`, so payout and pressure cannot drift apart (it used to charge on three
axes at once: +600% enemy HP for +50% money). +50% bodies for +50% money is break-even
on paper; the wager is the residual, since boarders overlap and leak. **Siphon 0 is the
baseline** every table below rests on: **18 / 30 / 42** boarders, **30 / 37.5 / 45** HP
Defaults, **2.60 / 2.47 / 2.34s** group gaps.

## Hatch

`Sim/Ship/ShipSystems.cs`, `MetaUpgrades.HatchHp`. 300 HP base, 375 at Hatch Armor 5.
Repair = `RepairPerSecond(RepairRig) * RepairSpeedMultiplier(Engineering)`: 8 HP/s
at all-L0, `10 x 1.25 = 12.5 HP/s` at all-L5.

**Repair costs `ShipSystems.RepairSalvagePerHp` (2) salvage per HP**, charged as healing is
applied through `RunSim.RepairHatch`. Stops when the bank hits zero (partial heal, never
negative). Free unlimited Prep repair is gone — that is the decision the cost exists to
create; the share-of-gross table and three-death pacing interaction live in
`Milestones/3. completed/M19-repair-costs-salvage.md`. Whether 2/HP is the right rate is
*unknown, needs play data*.

**Repair becomes conditional (M23).** Repair is currently free capability —
`RepairPerSecond(0)` returns a working rate and `RepairInteractable` checks no ownership, so
every save can repair from its first Run. M23 gates repairing on carrying the **repair rig**
in the Utility slot, and deletes both speed knobs above: FABRICATION's `RepairRigLevel` and
CREW QUARTERS' `EngineeringLevel` go, leaving the rig's own rate. The formula at the top of
this section is therefore **pre-M23** and does not survive it.

Three costs, where there was one:

| cost | value | status |
|---|---|---|
| per HP healed | 2 salvage (`ShipSystems.RepairSalvagePerHp`) | unchanged by M23 |
| owning the rig | purchase price | *undecided — M23 leaves free-vs-bought open* |
| carrying the rig | one Utility slot | unquantifiable until a second Utility item exists |

Scale of the first: a full repair of the base 300 HP Hatch is **600 salvage**. Measured
two-wave runs on 2026-08-04 grossed 1,072 / 1,182 / 1,075 (pre-M22 figures, so inflated by
roughly 20), which puts a full repair at **about half a run's take**.

**What this invalidates.** The boarder-second budget below, the three-death pacing target,
and every repair figure in `Milestones/3. completed/M19-repair-costs-salvage.md` were all
derived in a game where repair was always available and cost only salvage. After M23 they
describe a player who *brought the rig*, and say nothing about one who did not. Runs logged
either side of M23 are not comparable on anything involving the door. *Whether the rig ends
up feeling compulsory rather than chosen is unknown and needs play data* — M23 carries the
fallback if it does.

**Kill awards floor once (M22).** `AwardKill` takes `floor(CurrentKillValue)` and credits
that integer to both `MetaState.BankedSalvage` and `SalvageLedger.RunSalvage`. Pre-M22 logs
that quote an end-of-run `earned` figure are not comparable to post-M22 `gross` / `net`
lines: the old tally kept fractional kill value and floored once at report time, so a
fractional dial overstated the take by about `0.25 × kills` at dial 3 (measured 19 and 21
salvage of drift on 2026-08-04 runs).

## Upgrade tracks

**Two cost curves, on purpose.** Weapon Tiers are linear and reachable because weapons
are the progression; player and ship attributes keep the doubling curve because they
are a side investment.

**Weapon Tiers** (`Sim/Meta/WeaponTier.cs`) — `damage x 1.25^t`, `clip x 1.15^t`, fire
rate never scales. `MaxTier = 7`; cost of the next Tier is `400 + 200 x currentTier`
= 400/600/800/1000/1200/1400/1600, **7,000 cumulative**. Every step is ~30% effective
dps, above the measured ~20% noise floor below.

## Weapon abilities (M20)

Numbers in `Sim/Meta/WeaponAbility.cs`. Dial points are 0 until M21; `t = p/35`.

| weapon | ability | dial | at 0 | at 35 | cooldown | fixed |
|---|---|---|---|---|---|---|
| BR-12 | Concussive Breach | metres of shove | 3.5 m | 10.5 m | 10 s | cone half-angle 35° |
| MR-35 | Pin Shot | seconds held | 1.5 s | 6.0 s | 6 s | 3 hits to pin; 1.25 s decay |
| MK-1 | Regen Surge | **surge duration** | 4 s | 10 s | 10 s | rate fixed at 2.5× |

Secondary baseline regen: **0.4 rounds/s**. Surge multiplies that rate only while Secondary
is active. Boarder-seconds log tag: `Ability`.

The VK-7 is **in the catalog but not in `ShopWeapons()`** — deliberately disabled until
a weapon polish pass; kept so old saves deserialize. Its row below is reference only.

| weapon | T0 | T1 | T2 | T3 | T4 | T5 | T6 | T7 |
|---|---|---|---|---|---|---|---|---|
| MK-1 Sidearm | 28 | 36 | 47 | 61 | 79 | 102 | 131 | 167 |
| VK-7 Rifle *(disabled)* | 38 | 49 | 64 | 82 | 106 | 136 | 175 | 223 |
| BR-12 Shotgun | 33 | 43 | 57 | 73 | 95 | 122 | 157 | 201 |
| MR-35 Rifle | 37 | 48 | 62 | 80 | 102 | 131 | 168 | 213 |

**Attributes** (`Sim/Meta/MetaUpgrades.cs`) — `base * (1 + 0.05 * level)`, exactly +25%
at level 5; CritChance is additive `0.05 * level` because a chance of zero has nothing
to scale. Cost `200 * 2^currentLevel` = 200/400/800/1600/3200, **6,200** to max a track.

**A level is smaller than the noise, so no *attribute* purchase is perceptible.**
Measured 2026-08-03: three consecutive wave-2 attempts with a purchase between each
produced **28.2 / 33.8 / 33.1 HP/s — a 20% spread**, while the purchases were worth
`+4.5%` and `+3.0%`. Signal to noise ~**0.23**. A step the player can feel has to clear
roughly **20%**; the weapon Tier curve does, the attribute curve does not, and that is
the trade for keeping attributes cheap early.

## The pacing target — no wave costs more than three deaths

**Stated intent:** a player who dies repeatedly on a wave should be able to buy past it
within **three failed runs**. It is a constraint on the economy, and it is what sized
the Tier curve.

Required effective dps per wave assumes a survivable leak of 0.15 bodies/s and the
measured 0.74 realization factor (theoretical dps → dps actually delivered):

| wave | in/s | avg HP | need HP/s | need theoretical | cheapest VK-7 Tier | cumulative cost |
|---|---|---|---|---|---|---|
| 1 | 0.86 | 30.0 | 21 | 29 | T0 | 900 |
| 2 | 1.07 | 35.8 | 33 | 45 | T1 | 1,300 |
| 3 | 1.38 | 49.3 | 61 | 82 | T3 | 2,700 |
| 4 | 1.88 | 60.9 | 105 | 142 | T6 | 6,300 |
| 5 | 2.16 | 75.5 | 151 | 205 | T7 | 7,900 |

Against three failed runs' earnings at that wave (a run dying on wave N banks the waves
below it plus ~60% of N, at 25/kill and dial 0):

| die on | earns/run | x3 | needs | margin |
|---|---|---|---|---|
| wave 2 | 900 | 2,700 | 1,300 | 2.1x |
| wave 3 | 1,830 | 5,490 | 2,700 | 2.0x |
| wave 4 | 3,060 | 9,180 | 6,300 | 1.5x |
| wave 5 | 4,590 | 13,770 | 7,900 | 1.7x |

**Every wave clears inside two deaths' earnings** before repair costs. With repair at
2 salvage/HP the margin narrows to 1.7x at wave 2 and 1.2x at wave 3 for a player who
repairs fully each wave, and fails at waves 4–5 — see Hatch above. The target assumes
repair discipline; unlimited repair is a choice that costs progression.

**After M23 the target assumes something further: that the player brought the rig.** A
player without it cannot repair at any price, so the margins above apply to a loadout, not
to the game. The rigless case has no numbers at all yet and should not be guessed at — it is
the first thing a logged run after M23 should report.

**Under three Tiers this failed outright.** The old ceiling was 82 theoretical dps
against wave 4's 142 and wave 5's 205: no amount of earning bought past them, because
the content was sized beyond the shop. Seven Tiers is the fix, and this table is the
check that has to be re-run whenever either curve moves.

## Five-wave session — measured 2026-08-07

**The player's report comes first, because it is the only evidence about the experience:**
Zo — an expert player — reports the session was **fun and semi-challenging**. Everything
below is instrumentation, and none of it overrides that.

**The target curve** (stated by `Milestones/3. completed/M16`, recorded here for the first
time — M16's `Done when` claimed it lived in this file and it did not): hatch remaining at
each wave clear, **wave 1 ~100%, wave 2 ~85%, wave 3 ~65%, all +/-15**; waves 4-5 provisional.

**Two runs, same player, same weapon, same start wave, dial 0, twenty minutes apart**
(`Logs/GatekeeperBastion_2026-08-07_16-34-30.log`):

| | MR-35 **T1** | MR-35 **T4** |
|---|---|---|
| wave 2 | 272/300 | 300/300 |
| wave 3 | breached | 300/300 |
| wave 4 | — | 300/300 |
| wave 5 | — | 300/300 |
| repair spent | 56 | 0 |
| outcome | lost on wave 3 after 134s | DERELICT STRIPPED, gross 5250 |

**Kill rate rises with wave number:** 1.23 / 1.50 / 1.67 per second on waves 3 / 4 / 5,
against a previously measured band of 0.61-1.08/s. Denser waves mean less time walking
between targets. Part of each wave's escalation is offset by the shooter getting more
efficient inside it.

**The door collapses non-linearly once arrivals accumulate.** In the T1 run the Hatch went
224 -> 148 -> 72 -> breached in **6.4 seconds** with eight or more bodies parked on it, which
is the boarder-second budget behaving as modelled (~3.43 effective dps per parked Default).

### What this does not tell us

An earlier version of this section read these numbers as proof the curve was broken. That was
wrong and is withdrawn. **Hatch remaining measures leakage, not effort.** A player who kills
everything before it arrives registers 100% whether the wave was trivial or whether they were
working at their limit — the metric saturates, and it saturates hardest for exactly the
players most able to tell you how the game feels.

Specifically unsupported by this data: that the curve is bimodal (n=2, one player, skill
uncontrolled); that wave 5 is too easy; that the wheel would have one correct spoke; that
anything needs reopening. The T1 and T4 numbers differ, and the reason could be the Tier, the
player's second attempt at the same content, or both — the session cannot separate them.

**The open question this does raise is about the instrument, not the game:** if hatch
remaining reads 100% across a wide range of genuinely different challenge levels for a skilled
player, it cannot by itself grade a difficulty curve. Something that tracks effort rather than
leakage — time at full attention, distance travelled, how close the player came to being
overrun — would say more. *Unknown what the right measure is; needs a decision, not more
arithmetic.*

## Kill rate vs delivery rate — the model that failed

Intent: **an unupgraded player clears wave 1 and dies on wave 2; a fully upgraded one
clears all three.** The model below was the gate used to justify that. **It is wrong and
is kept only so it is not re-derived.** `pool = (D x 30 + H x 20 + R x 120) x hpMultiplier`,
`TTK = pool / dps`, unupgraded 32 dps and maxed 62.5:

| wave | count (D/H/R) | HP pool | delivery | TTK @32 | TTK @62.5 |
|---|---|---|---|---|---|
| 1 | 18 (18/0/0) | 540 | 13.0s | 16.9s | 8.6s |
| 2 | 30 (26/4/0) | 1075 | 17.3s | 33.6s | 17.2s |
| 3 | 42 (34/6/2) | 2070 | 18.7s | 64.7s | 33.1s |

Checked against ten logged sessions in `Docs/_meta/Logs/` (2026-08-02/03, pre-horde build:
6/10/14 boarders at 90 HP, so **wave 1's pool is identical at 540** and compares directly).
Both halves fail, in opposite directions.

**32 dps is ~2.5x what a player sustains.** Effective HP/s = wave pool ÷ kill span,
unupgraded and after the prefab-damage fix: **6.5 / 9.7 / 21.3** on waves 1/2/3 (session
`22-03-39`) and **12.6 / 14.9** (`22-24-08`). The 66.7% duty cycle is a *ceiling*: the
player also walks, turns, waits on pathing, and picks targets.

**"Behind by N seconds" is not a loss condition, so the comparison decides nothing.** A
wave has no deadline — delivery is how long the spawner takes to place bodies, not how long
the player has to remove them. Logged wave 1 clears ran **22.8s to 78.8s** against the same
13.0s of delivery, every one a win.

### What actually ends a run

`HATCH BREACHED`, in every valid loss on record (`22-24-08` wave 2, `22-53-43` wave 3), with
the player still alive. **Two sessions are not evidence at all**: `20-40-29` and `21-06-48`
log `[Player] took 4 damage`, the prefab-drift build where boarders hit at 22% of design —
the untouched-hatch triple clear came from there.

## The hatch's boarder-second budget — the model that replaced it

The door has a budget and it is measured, not predicted. One parked Default deals
`4 / 0.7 = 5.71` dps at a **60% landing rate**, so 3.43 effective. Divide:

```
budget (boarder-seconds) = hatchHP / 3.43     300 -> 88    345 -> 101    375 -> 109
spend  (boarder-seconds) = SUM over arrivals of seconds spent standing at the door
```

Landing rate is measured twice on 2026-08-03 — 34.2 HP/s and 35.1 HP/s with ~10
parked, against a theoretical 57.1 — and once at ~33% on the pre-fix build. **It is
the most load-bearing number here and the least replicated:** *unknown beyond those
samples, needs more play data.*

**Upgrades do not move the outcome, and the logs say why.** Three wave-2 attempts,
one purchase between each:

| attempt | bought | kill rate | effective | killed | reached hatch | duration | outcome |
|---|---|---|---|---|---|---|---|
| 1 | Clip L2 | 0.81/s | 28.2 HP/s | 58% | 15 | 27.5s | BREACH |
| 2 | Damage L3 | 0.96/s | 33.8 HP/s | 70% | 19 | 29.2s | BREACH |
| 3 | FireRate L3, dial 0 | 0.93/s | 33.1 HP/s | 83% | 22 | 31.5s | BREACH |

Attempt 3 spent roughly **220 boarder-seconds against a budget of 101**. Killing
faster raised the kill fraction 58% → 83% *and* raised arrivals 15 → 22, because
surviving longer gives more of the wave time to walk to the door. Hatch damage is
`arrivals x dwell`; damage cuts dwell and raises arrivals, and the two nearly
cancel.

**Where the budget went — two correct changes that compound.** The horde retune
tripled bodies and cut hatch damage to a third: break-even *only if landing holds*.
The swing-landing fix then took landing ~33% → ~60%. Together
`3 x (1/3) x 1.8 = 1.82x` the pre-horde hatch pressure, against a hatch that grew
300 → 345 (`1.15x`). **The door is 1.58x weaker relative to its wave than before
either change.**

Delivery rate is the lever that targets the mechanism — wave 2 at dial 0 delivers
1.74 bodies/s against a measured 0.93 kill rate:

| group gap | delivery | in/s | leak vs 0.93 kill/s |
|---|---|---|---|
| 2.47s (current) | 17.3s | 1.74 | +0.81/s |
| 3.00s | 21.0s | 1.43 | +0.50/s |
| 3.50s | 24.5s | 1.22 | +0.29/s |
| 4.00s | 28.0s | 1.07 | +0.14/s |

Hatch Armor is not an answer: one level is `+15 HP = +4.4 boarder-seconds` against a
~120 boarder-second deficit. See `Milestones/planned/M2` for the leverage ordering.

**Still not in any model:** travel time across the derelict (30 x 52 m floor, hatch at
z −20.5, breaches at z 9 and 28), aiming accuracy, and repair downtime.
