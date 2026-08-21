# 4 — Data

<!-- GENERATED BLOCKS — DO NOT EDIT INSIDE GKB:GEN … GKB:END MARKERS.
     Tools/docgen/run.sh splices these sixteen blocks from Sim/: mk1-base, wheel, rings,
     tier-cost, abilities, gatekeepers, waves, dial, player-count, intensity-juice, hatch, hatch-bank,
     master-control, tracks, bastion-mesh, ship-camera. Prose outside the
     markers is hand-written and preserved byte-for-byte on regenerate. Run
     Tools/docgen/run.sh after any Sim/ change that owns a block; --check fails on drift. -->

**Mechanics, the data half** (MDA), and the bottom of the leverage ladder — #11 buffer
sizes and #12 constants. Read last, on purpose.

MDA's point and Meadows' point are the same one from two directions: a designer can only
directly touch this layer, and it is the layer with the least leverage over what the player
actually experiences. Everything in `1-Experience.md`, `2-Dynamics.md` and `3-Mechanics.md`
is what these values are in service of.

Before changing a number, clear the gate in `../Working-Rules.md`: name the tier you are
targeting, and say why a higher one is wrong.

Every countable number here is emitted from the source cited on its `GKB:GEN` marker.
**Retuning means regenerating with `Tools/docgen/run.sh`, then re-deriving** the hand-written
arithmetic that sits beside the tables — change a number and you change the derivation in
the same edit. Loop behaviour, pacing models and the evidence about what a change actually
did are `2-Dynamics.md`.

## Weapon — MK-1 Sidearm

`Sim/Meta/WeaponCatalog.cs` (base row), `Sim/Meta/WeaponWheel.cs` (per-point multipliers).
**There are no weapon "levels".** The flat Tier curve was replaced by the upgrade wheel. The
MK-1 has a ring (strength Reload, weakness Damage — the rings table below). SEC-D has a
ring (strength FireRate, weakness Reload); its Ability spoke is not purchasable because
the catalog ability is still `AbilityId.None`.

<!-- GKB:GEN mk1-base — WeaponCatalog.Def(Mk1Sidearm) -->
| | base | | base |
|---|---|---|---|
| damage/shot | 30 | reload | 1.4s |
| shots/second | 3 | range | 45 m |
| clip | 8 | | |
<!-- GKB:END mk1-base -->

**Duty-cycle formula.** A cycle is `clip/rate` seconds of firing plus a reload:

```
dps = (clip * damage) / (clip/rate + reload)
base: (8 * 30) / (8/3 + 1.4) = 240 / 4.07 = 59.0 dps
```

Reload is an upgrade spoke (`ReloadPerPoint = 0.98035f`) for every shop weapon, so the
duty cycle is not fixed across a campaign. MK-1 T0 damage 30 one-shots a Wave-1 Default
(`Mk1_T0_OneShotsWave1Default`). That puts T0 duty-cycle dps above the Secondary band;
first-session fodder TTK is the demand (2026-08-16). The SEC-D's fire-rate-centred build
stays under focused Primaries (`SecDFireRateFocus_NeverRivalsFocusedPrimaries`). Secondaries
price on their own curve — the tier-cost block below — and the first two MK-1 purchases
(150 + 300) equal the 450 a wave-1 Hatch-bank can convert at kill-value 25 and intensity 0
(`FirstWheelPurchase_AffordableOffOneWaveOneClear`).

**Crit** (`PlayerGun.RollCrit`, `CritDamageMultiplier = 2`): `MetaUpgrades.CritChance()` is
frozen at 0. Average multiplier is therefore 1. There is no Luck shop.
**No MK-1 → improvised cutter**: 40% damage, 50% rate → 12 dmg at 1.5/s,
clip 8 → `96 / 6.73 = 14.3 dps`. A hard Run, not a dead end.

## Player

`Game/Player/FirstPersonController.cs`, `Game/Player/PlayerHealth.cs`, `Game/Player/Interactor.cs`.

- 100 HP (frozen; Vitality track deleted).
- Walk 5 m/s, sprint 7.5 m/s. No jump.
- Stamina 100 (frozen; Stamina track deleted), drain 25/s, regen 18/s after a 1s delay →
  **4.0s of sprint = 30 m**; a full refill costs `1 + 100/18 = 6.6s`. Sprint beats the
  5.6 m/s chase and the Hunter's 6.5, so unlimited sprint would be a free kiting
  exploit. Change the five stamina numbers together or not at all.
- Interact (hold-E) range 3.2 m.

## Gatekeepers — three kinds, each defined by what it REFUSES to do

`Sim/Waves/WaveDirector.StatsFor(kind)`, pushed onto each spawn by `Game/Enemies/WaveSpawner.cs`. All
three share one **0.7s swing clock**: kinds differ by what they hit, never by cadence.
HP 90 → 30 with 3x the bodies, so Wave 1 is still **540 HP of gatekeeper** (18 x 30 = the
old 6 x 90) — a redistribution, not an inflation.

<!-- GKB:GEN gatekeepers — WaveDirector.StatsFor, GatekeeperAttackInterval -->
| kind | HP | player dmg | Hatch dmg | patrol | chase | refuses |
|---|---|---|---|---|---|---|
| Default | 30 | 6 | 4 | 3.2 | 5.6 | — |
| Hunter | 20 | 6 | **0** | 6.5 | 6.5 | the Hatch (`ignoresHatch`) |
| Thief | 120 | **0** | 20 | 2.2 | 2.2 | the player (`ignoresPlayer`; never provoked) |
<!-- GKB:END gatekeepers -->

GEN `ignoresPlayer` is `WaveDirector.StatsFor`. Field-terminal hunt is a Game override
(`ThiefGatekeeper.MayTargetPlayer` when `WaveMode.Terminal`). Player-facing name is Thief.

- **Default** 30 HP = **1 shot** at MK-1 T0. 6 dmg/0.7s = 8.6 dps
  each: three on you kill a 100 HP player in **3.9s**, five in **2.3s** (at the old
  18 dmg, five were 128.6 dps and **0.8s** — why damage fell as bodies tripled).
  Hatch 4/0.7s = 5.7 dps: one leaker needs 52s, so leaks are a crowd problem now.
- **Hunter** 20 HP = **1 shot**. 6.5 m/s beats the 5 m/s walk and loses to the
  7.5 m/s sprint — the first Gatekeeper that makes **sprint** load-bearing. Zero Hatch
  damage: it cannot be answered by standing on the Hatch.
- **Thief** 120 HP = **4 shots** = `120/59 = 2.03s` of focused fire while dealing
  `20/0.7 = 28.6` dps → `28.6 x 2.03 = 58` HP of the 300 HP door, per Thief that
  got home *while being shot*. Intercepting it turns "shoot the closest" into "shoot the
  right thing"; it cannot hurt you, so it costs only the time the Hunters are using.
- Melee reach 2.4 m (player) / 3.5 m (Hatch), +1.2 m hysteresis. Aggro: shot at,
  or seen within 26 m; a provoked gatekeeper alerts others within 12 m; inside 8 m
  of the Hatch a Default commits to the Hatch regardless (a Hunter never does).

## Waves and intensity

`WaveDirector.NextWave(waveIndex, siphonLevel)`; `MaxWaves = 5`. The dial is a
hand-authored table, never a formula — `TetherSiphon.RewardMultiplierFor` =
**1 / 1.10 / 1.15 / 1.25 / 1.35 / 1.50**.

```
dial                  = TetherSiphon.RewardMultiplierFor(siphonLevel)
enemyCount            = min(120, round((18 + 12*waveIndex) * dial))
hunterCount           = {0,4,6,10,12}[waveIndex]  thiefCount = {0,0,2,4,7}[waveIndex]  (dial-proof)
defaultCount          = enemyCount - hunterCount - thiefCount   (takes the whole surplus)
enemyHealthMultiplier = 1 + 0.40*waveIndex                       (x each kind's HP)
groupSize             = min(3 + waveIndex, 7, enemyCount)        (3 / 4 / 5 / 6 / 7)
spawnInterval         = max(0.4, 4.2 - 0.20*waveIndex)   SECONDS BETWEEN GROUPS
gatekeepers, wave 1..5   L0 18/30/42/54/66
```

**`spawnInterval` is a GROUP gap now — the name did not change with it.** A Wave
lands in `(ceil(count/groupSize) - 1) * gap` = **21.0 / 28.0 / 30.4 / 28.8 / 30.6s**; read as a
per-individual gap the same Waves take 44 / 72 / 96s and no fight ever starts. First
field to check if a Wave feels empty. **The dial's surplus is all Default** (Hunter and
Thief counts are fixed per Wave), so L5 Wave 3 is 55 D + 6 H + 2 T: a bigger fight, not a
different one, and the dial cannot multiply the scariest Gatekeeper.

**`TerminalChainGap = 8s`** (`WaveDirector`). Unused by the field loop: `T` after `E`
starts one Wave, Prep after the clear is untimed, Hatch-bank is legal there. The constant stays
because `ArmSequentialDraws` still exists for later terminals that re-lock banking. Not
`InterWaveGap` (3 s after *delivery* during a deep-start overlap). Do not retune
`InterWaveGap` to steal this job.

**One table drives reward AND body count, on purpose**: `WaveDirector` reads
`TetherSiphon`, so payout and pressure cannot drift apart (it used to charge on three
axes at once: +600% Gatekeeper HP for +50% money). +50% bodies for +50% money is break-even
on paper; the wager is the residual, since gatekeepers overlap and leak. **Siphon 0 is the
baseline** every table below rests on:

<!-- GKB:GEN waves — WaveDirector.NextWave(i, 0) -->
| Wave | Bodies | Default | Hunter | Thief | HP × | groups of | Every |
|---|---|---|---|---|---|---|---|
| 1 | 18 | 18 | 0 | 0 | 1 | 3 | 4.2 s |
| 2 | 30 | 26 | 4 | 0 | 1.4 | 4 | 4 s |
| 3 | 42 | 34 | 6 | 2 | 1.8 | 5 | 3.8 s |
| 4 | 54 | 40 | 10 | 4 | 2.2 | 6 | 3.6 s |
| 5 | 66 | 47 | 12 | 7 | 2.6 | 7 | 3.4 s |
<!-- GKB:END waves -->

<!-- GKB:GEN dial — TetherSiphon.RewardMultiplierFor, NextWave body counts -->
| Dial | You earn | Wave 1 sends | Wave 5 sends |
|---|---|---|---|
| 0 | ×1.00 | 18 | 66 |
| 1 | ×1.10 | 20 | 73 |
| 2 | ×1.15 | 21 | 76 |
| 3 | ×1.25 | 23 | 83 |
| 4 | ×1.35 | 24 | 89 |
| 5 | ×1.50 | 27 | 99 |
<!-- GKB:END dial -->

Body count scales linearly with connected players: `min(120 × n, round((18 + 12×i) × dial) × n)`.
`n = 1` is the table above. Hunters and Thieves stay on the per-wave rows; surplus is Default.
Bank cap at conversion is solo expected unbanked for waves 1..DeepestWaveReached at dial 0 /
scavenging 0, and applies only when `n > 1`.

<!-- GKB:GEN player-count — WaveDirector.SupportedPlayerCount, NextWave n, HatchBank.BankCap -->
| | |
|---|---|
| supported | 3 (host plus two) |
| capacity | 6 |
| wave 1 bodies ×1 / ×3 | 18 / 54 |
| bank cap deepest 1 | 450 |
| bank cap deepest 2 | 1200 |
<!-- GKB:END player-count -->

<!-- GKB:GEN intensity-juice — IntensityJuice.CostPerStep -->
| | |
|---|---|
| juice per step | 100 unbanked |
<!-- GKB:END intensity-juice -->

## Hatch

`Sim/Ship/ShipSystems.cs`, `MetaUpgrades.HatchHp`. Repair rate is a flat rate on the equipped
Repair Rig (`EquipmentCatalog`); there is no Engineering multiplier and no Repair Rig track.

**Repair costs `ShipSystems.RepairSalvagePerHp` Salvage per HP**, charged as healing is
applied through `RunSim.RepairHatch`. Stops when the bank hits zero (partial heal, never
negative). Free unlimited Prep repair is gone — that is the decision the cost exists to
create; the share-of-gross table and three-death pacing interaction live in
`../Milestones/3. completed/M19-repair-costs-salvage.md`. Whether 2/HP is the right rate is
*unknown, needs play data*.

**Repair requires the Repair Rig (M23).** The equipment slot unlocks when salvage first
converts through the Hatch (`HatchConversions >= 1` — re-keyed with the wheel, `M78`) and
costs Salvage once; the rig is bought on top and equipping it is
automatic (one piece). Without the rig equipped, repair is refused and the Hatch prompt
names the next missing step. FABRICATION's old `RepairRigLevel` track and CREW QUARTERS'
`EngineeringLevel` are deleted; saves that owned either load with the slot and rig granted
free.

<!-- GKB:GEN hatch — ShipSystems, MetaUpgrades.HatchHp, EquipmentCatalog -->
| cost | value |
|---|---|
| Hatch HP (Armor 0) | 300 |
| Hatch HP (Armor 5) | 375 |
| per HP healed | 2 Salvage (`ShipSystems.RepairSalvagePerHp`) |
| equipment slot | **300** Salvage, once, after the first bank |
| repair rig | **300** Salvage, flat 8 HP/s |
<!-- GKB:END hatch -->

## Hatch-bank (M53)

Kills fill `RunSim.Unbanked`. Only a Hatch-bank converts to the ship. Convert and Hatch
composition both read field Waves cleared since the last Hatch-bank (snapshotted at
start). The window is `DurationSeconds`; Unbanked hitting 0 does not end it. Leftover
dumps when the timer ends.

<!-- GKB:GEN hatch-bank — HatchBank.DripPerSecond, DurationSeconds, CarryConvert, CarryWaveIndex, WithdrawPerSecond, BuyBackCost -->
| | |
|---|---|
| drip | 8 unbanked/s |
| duration | 45 s |
| withdraw | 16 salvage/s |
| returned keeps | 10% of leftover unbanked (Lost wipes it all) |
| buy-back | 450 banked |

| Waves carried | convert | Hatch Wave | bodies at intensity 0 |
|---|---|---|---|
| 1 | ×1.00 | 1 | 18 |
| 2 | ×1.10 | 2 | 30 |
| 3 | ×1.25 | 3 | 43 |
| 4 | ×1.35 | 4 | 54 |
| 5 | ×1.50 | 5 | 66 |
<!-- GKB:END hatch-bank -->

Buy-back costs `HatchBank.BuyBackCost` = **450**, one Wave-1 Hatch-bank convert at dial 0
(`18 × 25`). The rule — pay to re-enter a live Hatch-bank Assault; free embark stays on
the survived Prep window — is leverage **#5**; this number is **#12**.

Master Control is 5 terminal Waves then 1 Hatch-bank window. Gap is `WaveDirector.InterWaveGap`.
The finale elite is one Thief, no escorts, at `MasterControl.FinaleEliteHp` (240 pre-multiplier;
Wave-5 Thief spawned HP is 120 × 2.60 = 312, the elite is 240 × 2.60 = 624). The 45 s window
stays Hatch-bank; the Wave-5 horde does not spawn with it.

<!-- GKB:GEN master-control — MasterControl.TerminalWaves, HatchWaves, FinaleEliteHp, WaveDirector.InterWaveGap -->
| | |
|---|---|
| terminal Waves | 5 |
| Hatch-bank Waves | 1 |
| finale elite HP | 240 |
| gap | 3 s |
<!-- GKB:END master-control -->

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

Pulls Defaults and Hunters inside the radius; refuses Thieves entirely and Defaults already
committed inside 8 m of the Hatch. Takes the attacker's Hatch-damage value. Player fire
cannot damage it. Per-Wave log tag: `Decoy` (`gatekeeper-seconds absorbed`).

**What this invalidates.** The gatekeeper-second budget below, the three-death pacing target,
and every repair figure in `../Milestones/3. completed/M19-repair-costs-salvage.md` were all
derived in a game where repair was always available and cost only Salvage. They now describe
a player who *brought the rig*, and say nothing about one who did not. Runs logged either
side of M23 are not comparable on anything involving the door. *Whether the rig ends up
feeling compulsory rather than chosen is unknown and needs play data* — M23 carries the
fallback if it does.

**Kill awards floor once (M22).** `AwardKill` takes `floor(CurrentKillValue)` into
`RunSim.Unbanked`. Hatch-bank drip and dump credit that converted integer to both
`MetaState.BankedSalvage` and `SalvageLedger.RunSalvage`. Pre-M22 logs
that quote an end-of-Run `earned` figure are not comparable to post-M22 `gross` / `net`
lines: the old tally kept fractional kill value and floored once at report time, so a
fractional dial overstated the take by about `0.25 × kills` at dial 3 (measured 19 and 21
Salvage of drift on 2026-08-04 Runs). Instant kill-into-bank ended with M53.

## Upgrade tracks

**Two cost curves, on purpose.** Weapon purchases are linear and reachable because weapons
are the progression. Hatch Armor keeps the doubling curve. CREW attribute tracks are gone
(save v8 refund). Default Protocol is equipped at CREW QUARTERS, not bought as levels.

**Weapon upgrade wheel** (`Sim/Meta/WeaponWheel.cs`) — five attributes (reload, clip,
damage, fire rate, ability). One purchase awards **5 / 3 / 3 / 0 / 0** by ring distance.
Ten purchases, 110 points. Tier is derived (`total / 11`), never stored.

<!-- GKB:GEN tier-cost — WeaponTier.Cost, SpentToReach -->
Cost of the next purchase, Primary slot `400 + 200 × currentTier` = 400/600/800/1,000/1,200/1,400/1,600/1,800/2,000/2,200,
**13,000** cumulative. Secondary slot `150 + 150 × currentTier` = 150/300/450/600/750/900/1,050/1,200/1,350/1,500,
**8,250** cumulative.
<!-- GKB:END tier-cost -->

Per-point compound multipliers (dials — retune freely). Mid-ladder (35 pts) keeps the old
T7 focus anchors; full focus is read from `WeaponWheel.FocusAtMax` / `NeighbourAtMax`:

<!-- GKB:GEN wheel — WeaponWheel per-point, FocusAtMax, NeighbourAtMax -->
| attribute | per point | focus (50) | neighbour (30) |
|---|---|---|---|
| damage | ×1.05 | ×9.3 | ×3.81 |
| clip | ×1.03 | ×4.04 | ×2.31 |
| reload | ×0.98 | ×0.37 | ×0.55 |
| fire rate | ×1.01 | ×1.79 | ×1.42 |
| ability | dial points → `WeaponAbility.Magnitude` (`t = p/50`) | | |
<!-- GKB:END wheel -->

Rings (cycles; focus pays centre 5, neighbours 3):

<!-- GKB:GEN rings — WeaponWheel.RingOf, TryIdentity, AttrLabel -->
| Weapon | Ring | Strength | Weakness |
|---|---|---|---|
| BR-12 | R C D A F | damage | reload |
| MR-35 | R D C A F | damage | fire rate |
| PRI-C | C D R A F | damage | fire rate |
| MK-1 | D A R C F | reload | damage |
| SEC-D | F A R C D | fire rate | reload |
<!-- GKB:END rings -->

Pre-wheel flat Tier N migrates to **6N damage + 5N clip** points (N×11 total), zero elsewhere.

## Weapon abilities (M20)

Numbers in `Sim/Meta/WeaponAbility.cs`. Dial points come from the upgrade wheel's ability spoke;
`t = p/50`.

<!-- GKB:GEN abilities — WeaponAbility.Magnitude, Cooldown, fixed constants -->
| weapon | ability | dial | at 0 | at 50 | cooldown | fixed |
|---|---|---|---|---|---|---|
| BR-12 | Concussive Breach | metres of shove | 3.5 m | 10.5 m | 10 s | cone half-angle 35° |
| MR-35 | Pin Shot | seconds held | 1.5 s | 6 s | 6 s | 3 hits to pin; 1.25 s decay |
| MK-1 | Regen Surge | **surge duration** | 4 s | 10 s | 10 s | rate fixed at 2.5× |
| PRI-C | Railshot | bodies at full damage | 3 | 8 | 12 s | arms 4 s; ignores `pierceDamageMul` |
<!-- GKB:END abilities -->

**The MR-35's Pin Shot registers on the first damaged body of a shot only** (M31), and the
hit charge is weapon-scoped — target changes do not reset Hits; only the 1.25 s decay does.
Same-target reset left the build unreachable once TTK fell below 3 (T10 FocusAtMax one-shots
Defaults and kills Wave-5 Thieves in two). The BR-12 and MK-1 rows shipped with M20.

Secondary baseline regen: **0.4 rounds/s**. Surge multiplies that rate only while Secondary
is active. Gatekeeper-seconds log tag: `Ability`.

The VK-7 is **in the catalog but not in `ShopWeapons()`** — deliberately disabled until
a weapon polish pass; kept so old saves deserialize. Its row below is reference only.

**Base rows** (`Sim/Meta/WeaponCatalog.cs`). **There is no Tier damage column any more.**
A purchase's effect depends on which spoke it centred, so dps is a function of the points
array (`WeaponWheel.EffectiveDps`) and cannot be tabulated against derived Tier.

| weapon | dmg/shot | pellets | shots/s | clip | reload | range | pierce | base dps |
|---|---|---|---|---|---|---|---|---|
| MK-1 Sidearm | 30 | 1 | 3.0 | 8 | 1.4s | 45 m, falloff from 12 m | — | 59.0 |
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

**Hatch Armor** (`Sim/Meta/UpgradeTracks` + `MetaUpgrades.cs`) — `base * (1 + 0.05 * level)`,
exactly +25% at level 5. Crit chance is frozen at 0; there is no Luck shop. Engineering and
`RepairRigLevel` are off the enum entirely; they survive only as legacy save fields, which
`MetaState.MigrateLegacyRepairTracks` converts into a free equipment slot and Repair Rig.

<!-- GKB:GEN tracks — UpgradeTracks.All, MetaUpgrades.NextCost -->
Cost `200 * 2^currentLevel` = 200/400/800/1,600/3,200, **6,200** to max a track.

| Track | Station | Level 0 | Level 5 |
|---|---|---|---|
| Hatch Armor | FABRICATION | 300 HP | 375 HP |
<!-- GKB:END tracks -->

**A Hatch Armor level is smaller than the noise.** The measured three-attempt series is in
`2-Dynamics.md` (loop B1): a **20% run-to-run spread** against purchases worth `+4.5%` and
`+3.0%`, signal to noise ~**0.23**. A step the player can feel has to clear roughly **20%**;
one upgrade wheel purchase does (**+31%**, above). Hatch Armor is a buffer, not a perceptible
fight change. There is no attribute shop.

## Bastion kit — mesh mapping (M61-2)

`Sim/Bastion/BastionMeshMap.cs`, read by the kit emitter. One structural set for every
block kind — Quarter / Half / Full / Open Area / Control Room share it; per-kind variety
is the authoring pass. Pack is POLYGON Sci-Fi Space (Synty 138857). A side kind with no
mapping throws at emit time — a loud fail, never a primitive.

<!-- GKB:GEN bastion-mesh — BastionMeshMap constants, ForSide -->
| Cell | Prefab |
|---|---|
| Floor | `SM_Bld_Floor_01` |
| Ceiling | `SM_Bld_Ceiling_01` |
| Wall | `SM_Bld_Wall_01` |
| Door | `SM_Bld_Wall_Doorframe_01` |
| Window | `SM_Bld_Wall_Exterior_Window_01` |
| None | nothing emitted |
| Wall corner | `SM_Bld_Wall_Corner_Pillar_01` |
| Ceiling fixture | `SM_Prop_Light_Panel_01` |
<!-- GKB:END bastion-mesh -->

## Ship camera — isometric probe

`Sim/Ship/ShipCamera.cs`, applied by `Game/Ship/IsometricCameraRig.cs`. Ship scene only —
the Bastion stays first person while the probe runs. Rotation is locked, so at 45° yaw the
ship's south and west walls are the camera-facing sides forever: wall render state is a bake
decision, and wanting rotation later means emitting all four wall sides plus a runtime
toggle. The camera holds still while the player stays inside the dead zone, then follows
with exponential smoothing at the stated rate.

<!-- GKB:GEN ship-camera — ShipCamera constants -->
| Rig | Value |
|---|---|
| yaw (locked) | 45° |
| pitch | 50° |
| field of view | 30° |
| follow distance | 38 m |
| dead zone half X / half Z | 1.5 / 1.5 m |
| follow lerp | 6/s |
<!-- GKB:END ship-camera -->

## Wave-report dashboard (M38)

`Docs/dashboard.html` Session tab parses `GatekeepersProtocol_*.log` (current;
`GatekeeperBastion_*.log` still parse) in the browser after you
choose the Logs folder (or drop the files). Focus / Project tabs read
`Docs/dashboard-data.js` from `Tools/dashboard-data/run.ps1` (milestones, commits, questions,
rule-based next focus). Sample filters and era cutoffs. Until the corpus is large, Session **labels** PlayMode,
short, and Dev rather than dropping them from the sample (see `Docs/dashboard.html`).
wave-report still prints every Run; Dev is soft-excluded from summary means only.

| Constant | Value | Source |
|---|---|---|
| Short Run label | **5 s** | Runs shorter than this are labeled `short` (crashed / aborted / PlayMode harness churn). Measured from `run begins` to `RUN OVER` / `BASTION STRIPPED` / next `run begins`. |
| PlayMode discriminator | `stage=playmode` in the log | File-level. Not the `IntegrationCheck` tag (`AutoVerify` in logs before 2026-08-12). Labeled `playmode`; not dropped. |
| Era: pre-weapon | filename date **≤ 2026-08-03** | `Docs/_meta/Logs/` 2026-08-02/03, pre-horde (`2-Dynamics.md`). |
| Era: horde retune | **2026-08-04 … 2026-08-07** | After M10 / horde body counts; before wheel-era sessions dominate. |
| Era: weapons and wheel | **2026-08-08 … 2026-08-12** | Primaries + upgrade wheel in session logs. |
| Era: hatch-bank loop | **≥ 2026-08-13** | Field terminals, Unbanked, Hatch-bank, intensity juice, Master Control. |

`Tools/wave-report` remains the terminal/`--html` reader; the page is the bookmark.

---

Why these values and not others: `2-Dynamics.md`. What they are in service of:
`1-Experience.md`. What enforces them: `3-Mechanics.md`.

