# 4 — Data

**Standing note.** This file is scheduled to become generated output read straight out of
the live code, with a drift check, on the same contract as `../Play/Field-Manual.md`. Until
that lands it is hand-maintained, so a number here can be stale from the moment the code
changes until someone edits this page.

**Mechanics, the data half** (MDA), and the bottom of the leverage ladder — #11 buffer
sizes and #12 constants. Read last, on purpose.

MDA's point and Meadows' point are the same one from two directions: a designer can only
directly touch this layer, and it is the layer with the least leverage over what the player
actually experiences. Everything in `1-Experience.md`, `2-Dynamics.md` and `3-Mechanics.md`
is what these values are in service of.

Before changing a number, clear the gate in `../Working-Rules.md`: name the tier you are
targeting, and say why a higher one is wrong.

Every number here was read out of the source cited beside it. **Retuning means re-deriving
with this arithmetic, never adjusting by feel** — change a number and you change the
derivation and the tables in the same edit. Loop behaviour, pacing models and the evidence
about what a change actually did are `2-Dynamics.md`.

## Weapon — MK-1 Sidearm

`Sim/Meta/WeaponCatalog.cs` (base row), `Sim/Meta/WeaponWheel.cs` (per-point multipliers).
**There are no weapon "levels".** The flat Tier curve was replaced by the upgrade wheel; the MK-1
has no ring authored (`WeaponWheel.RingOf` returns null for it), so it never improves.

| | base | | base |
|---|---|---|---|
| damage/shot | 14 | reload | 1.4s |
| shots/second | 3.0 | range | 45 m |
| clip | 8 | | |

**Duty-cycle formula.** A cycle is `clip/rate` seconds of firing plus a reload:

```
dps = (clip * damage) / (clip/rate + reload)
base: (8 * 14) / (8/3 + 1.4) = 112 / 4.07 = 27.5 dps
```

Reload is an upgrade spoke (`ReloadPerPoint = 0.98035f`) for weapons that have a ring, so the
duty cycle is not fixed across a campaign — it is only fixed for the MK-1, which cannot buy
points.

**Crit** (`PlayerGun.RollCrit`, `CritDamageMultiplier = 2`): average multiplier is
`1 + critChance`; at Luck 5 (0.25), `27.5 x 1.25 = 34.4 dps`. Only crit source in the game.
**No MK-1 → improvised cutter**: 40% damage, 50% rate → 5.6 dmg at 1.5/s,
clip 8 → `44.8 / 6.73 = 6.7 dps`. A hard Run, not a dead end.

## Player

`Game/Player/FirstPersonController.cs`, `Game/Player/PlayerHealth.cs`, `Game/Player/Interactor.cs`.

- 100 HP (L0; 125 at Vitality 5).
- Walk 5 m/s, sprint 7.5 m/s. No jump.
- Stamina 100 (125 at L5), drain 25/s, regen 18/s after a 1s delay →
  **4.0s of sprint = 30 m**; a full refill costs `1 + 100/18 = 6.6s`. Sprint beats the
  5.6 m/s chase and the Hunter's 6.5, so unlimited sprint would be a free kiting
  exploit. Change the five stamina numbers together or not at all.
- Interact (hold-E) range 3.2 m.

## Gatekeepers — three kinds, each defined by what it REFUSES to do

`Sim/Waves/WaveDirector.StatsFor(kind)`, pushed onto each spawn by `Game/Enemies/WaveSpawner.cs`. All
three share one **0.7s swing clock**: kinds differ by what they hit, never by cadence.
HP 90 → 30 with 3x the bodies, so Wave 1 is still **540 HP of gatekeeper** (18 x 30 = the
old 6 x 90) — a redistribution, not an inflation.

| kind | HP | player dmg | Hatch dmg | patrol | chase | refuses |
|---|---|---|---|---|---|---|
| Default | 30 | 6 | 4 | 3.2 | 5.6 | — |
| Hunter | 20 | 6 | **0** | 6.5 | 6.5 | the Hatch (`ignoresHatch`) |
| Ram | 120 | **0** | 20 | 2.2 | 2.2 | the player (`ignoresPlayer`; never provoked) |

- **Default** 30 HP = **3 shots** (14/28/42) = `30/27.5 = 1.09s`. 6 dmg/0.7s = 8.6 dps
  each: three on you kill a 100 HP player in **3.9s**, five in **2.3s** (at the old
  18 dmg, five were 128.6 dps and **0.8s** — why damage fell as bodies tripled).
  Hatch 4/0.7s = 5.7 dps: one leaker needs 52s, so leaks are a crowd problem now.
- **Hunter** 20 HP = **2 shots** = `20/27.5 = 0.73s`. 6.5 m/s beats the 5 m/s walk and loses to the
  7.5 m/s sprint — the first Gatekeeper that makes **Stamina** worth buying. Zero Hatch
  damage: it cannot be answered by standing on the Hatch.
- **Ram** 120 HP = **9 shots** = `120/27.5 = 4.36s` of focused fire while dealing
  `20/0.7 = 28.6` dps → `28.6 x 3.75 = 107` HP, a third of the 300 HP door, per Ram that
  got home *while being shot*. Intercepting it turns "shoot the closest" into "shoot the
  right thing"; it cannot hurt you, so it costs only the time the Hunters are using.
- Melee reach 2.4 m (player) / 3.5 m (Hatch), +1.2 m hysteresis. Aggro: shot at,
  or seen within 26 m; a provoked gatekeeper alerts others within 12 m; inside 8 m
  of the Hatch a Default commits to the Hatch regardless (a Hunter never does).

## Waves and the siphon dial

`WaveDirector.NextWave(waveIndex, siphonLevel)`; `MaxWaves = 5`. The dial is a
hand-authored table, never a formula — `TetherSiphon.RewardMultiplierFor` =
**1 / 1.10 / 1.15 / 1.25 / 1.35 / 1.50**.

```
dial                  = TetherSiphon.RewardMultiplierFor(siphonLevel)
enemyCount            = min(120, round((18 + 12*waveIndex) * dial))
hunterCount           = {0,4,6,10,12}[waveIndex]  ramCount = {0,0,2,4,7}[waveIndex]  (dial-proof)
defaultCount          = enemyCount - hunterCount - ramCount   (takes the whole surplus)
enemyHealthMultiplier = 1 + 0.40*waveIndex                       (x each kind's HP)
groupSize             = min(3 + waveIndex, 7, enemyCount)        (3 / 4 / 5 / 6 / 7)
spawnInterval         = max(0.4, 4.2 - 0.20*waveIndex)   SECONDS BETWEEN GROUPS
gatekeepers, wave 1..5   L0 18/30/42/54/66
```

**`spawnInterval` is a GROUP gap now — the name did not change with it.** A Wave
lands in `(ceil(count/groupSize) - 1) * gap` = **21.0 / 28.0 / 30.4 / 28.8 / 30.6s**; read as a
per-individual gap the same Waves take 44 / 72 / 96s and no fight ever starts. First
field to check if a Wave feels empty. **The dial's surplus is all Default** (Hunter and
Ram counts are fixed per Wave), so L5 Wave 3 is 55 D + 6 H + 2 R: a bigger fight, not a
different one, and the dial cannot multiply the scariest Gatekeeper.

**One table drives reward AND body count, on purpose**: `WaveDirector` reads
`TetherSiphon`, so payout and pressure cannot drift apart (it used to charge on three
axes at once: +600% Gatekeeper HP for +50% money). +50% bodies for +50% money is break-even
on paper; the wager is the residual, since gatekeepers overlap and leak. **Siphon 0 is the
baseline** every table below rests on: **18 / 30 / 42 / 54 / 66** gatekeepers,
**30 / 42 / 54 / 66 / 78** HP Defaults, **4.20 / 4.00 / 3.80 / 3.60 / 3.40s** group gaps.

## Hatch

`Sim/Ship/ShipSystems.cs`, `MetaUpgrades.HatchHp`. 300 HP base, 375 at Hatch Armor 5.
Repair rate is a flat **8 HP/s** on the equipped Repair Rig (`EquipmentCatalog`); there is
no Engineering multiplier and no Repair Rig track.

**Repair costs `ShipSystems.RepairSalvagePerHp` (2) Salvage per HP**, charged as healing is
applied through `RunSim.RepairHatch`. Stops when the bank hits zero (partial heal, never
negative). Free unlimited Prep repair is gone — that is the decision the cost exists to
create; the share-of-gross table and three-death pacing interaction live in
`../Milestones/3. completed/M19-repair-costs-salvage.md`. Whether 2/HP is the right rate is
*unknown, needs play data*.

**Repair requires the Repair Rig (M23).** The equipment slot unlocks at the first lost Run
(`PodEjections >= 1`) and costs Salvage once; the rig is bought on top and equipping it is
automatic (one piece). Without the rig equipped, repair is refused and the Hatch prompt
names the next missing step. FABRICATION's old `RepairRigLevel` track and CREW QUARTERS'
`EngineeringLevel` are deleted; saves that owned either load with the slot and rig granted
free.

| cost | value | status |
|---|---|---|
| per HP healed | 2 Salvage (`ShipSystems.RepairSalvagePerHp`) | unchanged |
| equipment slot | **300** Salvage, once, after first loss | *tunable, needs play data* |
| repair rig | **300** Salvage, flat 8 HP/s | *tunable, needs play data* |

Arithmetic for the 300/300 pair: a Run that dies on Wave 2 banks ~900 at dial 0 (pacing
tables), so 600 fits inside one post-loss Run alongside a 400 wheel purchase — and prices the tool at
exactly one full 300 HP repair (600 at 2/HP).

Scale of the per-HP cost: a full repair of the base 300 HP Hatch is **600 Salvage**. Measured
two-Wave Runs on 2026-08-04 grossed 1,072 / 1,182 / 1,075 (pre-M22 figures, so inflated by
roughly 20), which puts a full repair at **about half a Run's take**.

## Decoy payload (M30)

`Sim/Meta/PayloadCatalog.cs`, `Sim/Run/DecoyState.cs`. The drone bay holds one payload per
Run; the decoy is the first row. All figures **initial, tunable, needs play data**.

| | |
|---|---|
| Salvage price | **250** |
| charges / Run | **3** (reset at Run start, not Prep) |
| HP | **120** |
| pull radius | **12 m** (same as alert radius) |
| place range | **20 m** |
| max live | **1** |
| effective dps (conversion) | **3.43** (`4 / 0.7 × ~60%` landing) |
| gatekeeper-seconds / charge | `120 / 3.43 ≈ 35` (~40% of a 300 HP door's ~88 gs budget) |

Pulls Defaults and Hunters inside the radius; refuses Rams entirely and Defaults already
committed inside 8 m of the Hatch. Takes the attacker's Hatch-damage value. Player fire
cannot damage it. Per-Wave log tag: `Decoy` (`gatekeeper-seconds absorbed`).

**What this invalidates.** The gatekeeper-second budget below, the three-death pacing target,
and every repair figure in `../Milestones/3. completed/M19-repair-costs-salvage.md` were all
derived in a game where repair was always available and cost only Salvage. They now describe
a player who *brought the rig*, and say nothing about one who did not. Runs logged either
side of M23 are not comparable on anything involving the door. *Whether the rig ends up
feeling compulsory rather than chosen is unknown and needs play data* — M23 carries the
fallback if it does.

**Kill awards floor once (M22).** `AwardKill` takes `floor(CurrentKillValue)` and credits
that integer to both `MetaState.BankedSalvage` and `SalvageLedger.RunSalvage`. Pre-M22 logs
that quote an end-of-Run `earned` figure are not comparable to post-M22 `gross` / `net`
lines: the old tally kept fractional kill value and floored once at report time, so a
fractional dial overstated the take by about `0.25 × kills` at dial 3 (measured 19 and 21
Salvage of drift on 2026-08-04 Runs).

## Upgrade tracks

**Two cost curves, on purpose.** Weapon purchases are linear and reachable because weapons
are the progression; player and ship attributes keep the doubling curve because they
are a side investment.

**Weapon upgrade wheel** (`Sim/Meta/WeaponWheel.cs`) — five attributes (reload, clip,
damage, fire rate, ability). One purchase awards **5 / 3 / 3 / 0 / 0** by ring distance.
Ten purchases, 110 points. Tier is derived (`total / 11`), never stored. Cost of the next
purchase is still `400 + 200 × currentTier` = 400/600/800/1000/1200/1400/1600/1800/2000/2200,
**13,000 cumulative**.

Per-point compound multipliers (dials — retune freely). Mid-ladder (35 pts) keeps the old
T7 focus anchors; full focus is 50 pts:

| attribute | per point | focus (50) | neighbour (30) | at 35 (old T7) |
|---|---|---|---|---|
| damage | ×1.0456 | ×9.30 | ×3.81 | ×4.76 |
| clip | ×1.0283 | ×4.04 | ×2.31 | ×2.66 |
| reload | ×0.98035 | ×0.37 | ×0.55 | ×0.50 |
| fire rate | ×1.0117 | ×1.79 | ×1.42 | ×1.50 |
| ability | dial points → `WeaponAbility.Magnitude` (`t = p/50`) | | | |

Rings (cycles; focus pays centre 5, neighbours 3):

| Weapon | Ring | Strength | Weakness |
|---|---|---|---|
| BR-12 | R C D A F | damage | reload |
| MR-35 | R D C A F | damage | fire rate |
| PRI-C | C D R A F | damage | fire rate |
| MK-1 | *unset* | — | — |
| SEC-D | *unset* | — | — |

Pre-wheel flat Tier N migrates to **6N damage + 5N clip** points (N×11 total), zero elsewhere.

## Weapon abilities (M20)

Numbers in `Sim/Meta/WeaponAbility.cs`. Dial points come from the upgrade wheel's ability spoke;
`t = p/50`.

| weapon | ability | dial | at 0 | at 50 | cooldown | fixed |
|---|---|---|---|---|---|---|
| BR-12 | Concussive Breach | metres of shove | 3.5 m | 10.5 m | 10 s | cone half-angle 35° |
| MR-35 | Pin Shot | seconds held | 1.5 s | 6.0 s | 6 s | 3 hits to pin; 1.25 s decay |
| MK-1 | Regen Surge | **surge duration** | 4 s | 10 s | 10 s | rate fixed at 2.5× |
| PRI-C | Railshot | bodies at full damage | 3 | 8 | 12 s | arms 4 s; ignores `pierceDamageMul` |

**The MR-35's Pin Shot registers on the first damaged body of a shot only** (M31). Before
that fix, pierce through a line reset the build every body and the ability never fired;
authored magnitudes above were unread in play. The BR-12 and MK-1 rows shipped with M20.

Secondary baseline regen: **0.4 rounds/s**. Surge multiplies that rate only while Secondary
is active. Gatekeeper-seconds log tag: `Ability`.

The VK-7 is **in the catalog but not in `ShopWeapons()`** — deliberately disabled until
a weapon polish pass; kept so old saves deserialize. Its row below is reference only.

**Base rows** (`Sim/Meta/WeaponCatalog.cs`). **There is no Tier damage column any more.**
A purchase's effect depends on which spoke it centred, so dps is a function of the points
array (`WeaponWheel.EffectiveDps`) and cannot be tabulated against derived Tier.

| weapon | dmg/shot | pellets | shots/s | clip | reload | range | pierce | base dps |
|---|---|---|---|---|---|---|---|---|
| MK-1 Sidearm | 14 | 1 | 3.0 | 8 | 1.4s | 45 m, falloff from 12 m | — | 27.5 |
| SEC-D | 18 | 1 | 6.0 | 4 | 2.2s | 8 m, falloff from 3 m | — | 25.1 |
| VK-7 Rifle *(disabled)* | 10 | 1 | 5.5 | 25 | 2.1s | 60 m | — | 37.6 |
| BR-12 Shotgun | 6 | 7 | 1.2 | 6 | 2.6s | 14 m, falloff from 8 m | — | 33.2 |
| MR-35 Rifle | 32 | 1 | 1.6 | 10 | 2.4s | 60 m, falloff from 25 m | 2 bodies ×0.26 after first | 37.0 |
| PRI-C | 90 | 1 | 0.6 | 4 | 3.2s | 90 m, falloff from 55 m | 3 bodies ×0.75 after first | 36.5 |

Base dps is the duty-cycle formula with pellets folded into the pull:

```
dps    = clip * damage * pellets / (clip/rate + reload)
BR-12  = 6 * 6 * 7 / (6/1.2 + 2.6)   = 252 / 7.60 = 33.2
MR-35  = 10 * 32   / (10/1.6 + 2.4)  = 320 / 8.65 = 37.0
PRI-C  = 4 * 90    / (4/0.6 + 3.2)   = 360 / 9.87 = 36.5
```

**What a full damage focus buys** (ten purchases, 13,000 Salvage, `WeaponTier.Cost`). BR-12
centres Damage, so Damage takes 50 points and its ring neighbours Clip and Ability take 30
each, while Reload and FireRate take none: damage `6 x 9.30 = 55.8`, clip
`round(6 x 2.31) = 14`, cycle `14/1.2 + 2.6 = 14.27s` → `14 x 55.8 x 7 / 14.27 =` **383 dps**.
MR-35 centres Damage between Reload and Clip: damage `32 x 9.30 = 297.6`, clip
`round(10 x 2.31) = 23`, reload `2.4 x 0.55 = 1.32s` → **436 dps**. The first single purchase
on that line is worth **+31%** (BR-12 `33.2 → 43.6`), which is the number the noise floor
below is measured against. Whether Wave 5 still clears against a 383/436 ceiling is
**unknown — needs play data** (old sizing assumed ~187/214 at seven purchases).

**Attributes** (`Sim/Meta/MetaUpgrades.cs`) — `base * (1 + 0.05 * level)`, exactly +25%
at level 5; CritChance is additive `0.05 * level` because a chance of zero has nothing
to scale. Cost `200 * 2^currentLevel` = 200/400/800/1600/3200, **6,200** to max a track.
`Game/Ship/ShipCommands.UpgradeTrack` carries five rows: **Hatch Armor** at FABRICATION, and
**Vitality / Stamina / Scavenging / Luck** at CREW QUARTERS — four there, not five.
Engineering and `RepairRigLevel` are off the enum entirely; they survive only as legacy save
fields, which `MetaState.MigrateLegacyRepairTracks` converts into a free equipment slot and
Repair Rig.

**A level is smaller than the noise, so no *attribute* purchase is perceptible.** The
measured three-attempt series is in `2-Dynamics.md` (loop B1): a **20% run-to-run spread**
against purchases worth `+4.5%` and `+3.0%`, signal to noise ~**0.23**. A step the player
can feel has to clear roughly **20%**; one upgrade wheel purchase does (**+31%**, above), the
attribute curve does not, and that is the trade for keeping attributes cheap early.

## Wave-report dashboard (M38)

`Docs/dashboard.html` parses `GatekeeperBastion_*.log` in the browser after you choose the
Logs folder (or drop the files). Sample filters and era cutoffs:

| Constant | Value | Source |
|---|---|---|
| Minimum Run duration | **5 s** | Runs shorter than this are excluded (crashed / aborted / PlayMode harness churn). Measured from `run begins` to `RUN OVER` / `BASTION STRIPPED` / next `run begins`. |
| PlayMode discriminator | `stage=playmode` in the log | Not the `AutoVerify` tag — ordinary sessions can carry AutoVerify (`Logs/GatekeeperBastion_2026-08-09_15-21-59.log`). |
| Era: pre-weapon | filename date **≤ 2026-08-03** | `Docs/_meta/Logs/` 2026-08-02/03, pre-horde (`2-Dynamics.md`). |
| Era: horde retune | **2026-08-04 … 2026-08-07** | After M10 / horde body counts; before wheel-era sessions dominate. |
| Era: weapons and wheel | **≥ 2026-08-08** | Primaries + upgrade wheel in session logs. |

`Tools/wave-report` remains the terminal/`--html` reader; the page is the bookmark.

---

Why these values and not others: `2-Dynamics.md`. What they are in service of:
`1-Experience.md`. What enforces them: `3-Mechanics.md`.
