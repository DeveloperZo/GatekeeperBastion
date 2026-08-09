# Tuning — constants and buffers

**The lowest leverage tier in the doc set (#11 buffer sizes, #12 constants), and
deliberately the last thing you read.** It is unnumbered because it is not part of the
ladder the numbered files descend: `01_Game.md` is what the game believes and enforces,
`02_Systems.md` is the loops and structure those rules produce, and this file is the
values those loops are currently running at.

A number here is the tier you can always return to. It is not where a session starts.
Before changing one, clear the gate in `04_Agent_Rules.md`: name the tier you are
targeting, and say why a higher one is wrong.

Every number here was read out of the source cited beside it. **Retuning means re-deriving
with this arithmetic, never adjusting by feel** — change a number and you change the
derivation and the tables in the same edit. Pacing models, loop behaviour and the evidence
about what a change actually did are `02_Systems.md`.

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

**A level is smaller than the noise, so no *attribute* purchase is perceptible.** The
measured three-attempt series is in `02_Systems.md` (loop B1): a **20% run-to-run spread**
against purchases worth `+4.5%` and `+3.0%`, signal to noise ~**0.23**. A step the player
can feel has to clear roughly **20%**; the weapon Tier curve does, the attribute curve does
not, and that is the trade for keeping attributes cheap early.


---

Why these values and not others: `02_Systems.md`. What they are in service of: `01_Game.md`.
