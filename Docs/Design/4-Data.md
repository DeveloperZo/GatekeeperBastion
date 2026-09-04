# 4 — Data

<!-- GENERATED BLOCKS — DO NOT EDIT INSIDE GKB:GEN … GKB:END MARKERS.
     Tools/docgen/run.sh splices these seventeen blocks from Sim/ (the list is
     Program.cs's KnownBlockIds): mk1-base, wheel, rings,
     tier-cost, abilities, gatekeepers, waves, dial, player-count, intensity-juice, hatch, hatch-bank,
     respawns, master-control, tracks, bastion-mesh, ship-camera. Prose outside the
     markers is hand-written and preserved byte-for-byte on regenerate. Run
     Tools/docgen/run.sh after any Sim/ change that owns a block; --check fails on drift.
     Last updated: 2026-09-03 (M116 lever 3 — the finale: two shaped control-room Waves,
     the 12 s beat, the escorted and shielded elite; the master-control block regenerated). -->

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
| damage/shot | 50 | reload | 1.4s |
| shots/second | 1.4 | range | 45 m |
| clip | 8 | | |
<!-- GKB:END mk1-base -->

**Duty-cycle formula.** A cycle is `clip/rate` seconds of firing plus a reload:

```
dps = (clip * damage) / (clip/rate + reload)
base: (8 * 50) / (8/1.4 + 1.4) = 400 / 7.11 = 56.2 dps
```

Reload is an upgrade spoke (`ReloadPerPoint = 0.98035f`) for every shop weapon, so the
duty cycle is not fixed across a campaign.

**MK-1 30 → 50 damage, 3.0 → 1.4 shots/s (Zo, 2026-09-02; #12).** Zo's 2026-08-16 verdict
asked for one-shot first-session fodder; the 2026-08-27 `BaseEnemyHp` 30 → 50 raise undid it
without a decision and the pin was renamed to match. At 50 the T0 sidearm one-shots a Wave-1
Default and two-shots Wave 2's 70 (`Mk1_T0_OneShotsWave1Default_TwoShotsWave2`, renamed back).
The rate is **solved, not picked**: a one-shot gun at the old 3.0 sustains 98 dps and 1.97 Wave-1
kills/s, above every Primary, and at 1.8 it still out-kills the MR-35 on the shallow wave (1.37 against 1.16
kills/s), which trips the 08-27 guard that no Primary loses to the free gun on its own axis
(`PrimaryBand_T0_EachAvailablePrimaryLeadsOnOneAxis_SoNoneIsDominated`). 1.4 is the highest
tenth at which the rifle still leads there (**1.12** kills/s against 1.16); from Wave 2 the
rifle leads by more (0.93 against 0.56). Sustained dps fell 59.0 → 56.2. Whether 0.71 s
between pulls reads as a sidearm is **unknown — needs play data**. The SEC-D's fire-rate-centred build
stays under focused Primaries (`SecDFireRateFocus_NeverRivalsFocusedPrimaries`). Secondaries
price on their own curve — the tier-cost block below — and the first two MK-1 purchases
(150 + 300) fit inside the 462 Wave 1 pays outright at kill-value 42 and intensity 0
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
  4.6 m/s chase and the Hunter's 5.6, so unlimited sprint would be a free kiting
  exploit. Change the five stamina numbers together or not at all.
- Interact (hold-E) range 3.2 m.

## Gatekeepers — four kinds, each defined by what it REFUSES to do

`Sim/Waves/WaveDirector.StatsFor(kind)`, pushed onto each spawn by `Game/Enemies/WaveSpawner.cs`. All
four share one **0.7s swing clock**: kinds differ by what they hit, never by cadence.
Two roster passes have moved these numbers without moving the total. The 2026-08-04 horde pass
took HP 90 → 30 with 3x the bodies (18 x 30 = the old 6 x 90 = **540 HP of gatekeeper** on Wave
1); the 2026-08-27 fewer-and-tougher pass reversed the direction, `BaseEnemyHp` 30 → 50 against
a roster cut `18+12i` → `11+7i`. Wave 1 is **550 HP of gatekeeper** today — `BaseEnemyHp` 50
across the eleven bodies the roster formula authors — so both passes were a redistribution,
not an inflation.

<!-- GKB:GEN gatekeepers — WaveDirector.StatsFor, GatekeeperAttackInterval -->
| kind | HP | player dmg | Hatch dmg | patrol | chase | refuses |
|---|---|---|---|---|---|---|
| Default | 50 | 6 | 4 | 2.8 | 4.6 | — |
| Hunter | 33 | 6 | **0** | 5.6 | 5.6 | the Hatch (`ignoresHatch`) |
| Thief | 200 | **0** | 20 | 2.2 | 2.2 | the player (`ignoresPlayer`; never provoked) |
| Cloaker | 75 | 6 | 4 | 2.8 | 4.6 | being seen (watch cone; a state in `CloakerGatekeeper`, not a flag) |
<!-- GKB:END gatekeepers -->

GEN `ignoresPlayer` is `WaveDirector.StatsFor`. Field-terminal hunt is a Game override
(`ThiefGatekeeper.MayTargetPlayer` when `WaveMode.Terminal`). Player-facing name is Thief.

- **Default** `BaseEnemyHp` 50 HP = **2 shots** at MK-1 T0. 6 dmg/0.7s = 8.6 dps
  each: three on you kill a 100 HP player in **3.9s**, five in **2.3s** (at the old
  18 dmg, five were 128.6 dps and **0.8s** — why damage fell as bodies tripled).
  Hatch 4/0.7s = 5.7 dps: one leaker needs 105s against `ShipSystems.HatchMaxHp`, so
  leaks are a crowd problem now.
- **Hunter** `HunterHp` 33 HP = **2 shots**. 5.6 m/s beats the 5 m/s walk and loses to the
  7.5 m/s sprint — the first Gatekeeper that makes **sprint** load-bearing. Zero Hatch
  damage: it cannot be answered by standing on the Hatch.
- **Speeds fell on the spine (Zo, 2026-08-25).** Default chase went `5.6 → 4.6` and the
  Hunter `6.5 → 5.6`, because the single-storey spine replaced one open room with a 227 m
  corridor. In the old room a chase slower than the 5 m/s walk meant a player could
  outrun everything forever; on the spine the walk back to a terminal *is* the pressure,
  and a horde faster than the player made repositioning impossible rather than costly. The
  Hunter keeps the one relationship that was ever load-bearing — faster than walk, slower
  than sprint — and is now the ONLY kind that outruns a walking player, which is what it
  was for.
- **Thief** `ThiefHp` 200 HP = **7 shots** = `200/59 = 3.39s` of focused fire while dealing
  `20/0.7 = 28.6` dps → `28.6 x 3.39 = 97` HP of the door (`ShipSystems.HatchMaxHp` 600), per
  Thief that got home *while being shot*. Intercepting it turns "shoot the closest" into "shoot the
  right thing"; it cannot hurt you, so it costs only the time the Hunters are using.
- **Cloaker** `CloakerHp` 75 HP = **3 shots** at MK-1 T0, where a Default is two — the ONLY number it
  owns; every other figure is Default's, read from the same constants, so the watch rule
  stays the whole difference. The rule is Game-layer state (`CloakerGatekeeper`): inside a
  **30° half-angle** cone of any live Boarder's planar forward, within **26 m**, with line
  of sight, it targets nothing and holds; release past 36° / 28 m (Schmitt band, 0.1 s
  poll), reveal-on-hit 1.0 s. Cloaked it renders on `Cloaker_Cloaked.mat` (Fade, alpha
  0.12, teal seams lit); observed or freshly hit, `Cloaker.mat`.
- Melee reach 2.4 m (player) / 3.5 m (Hatch), +1.2 m hysteresis. Aggro: shot at,
  or seen within 26 m; a provoked gatekeeper alerts others within 12 m; inside 8 m
  of the Hatch a Default commits to the Hatch regardless (a Hunter never does).

## Waves and intensity

`WaveDirector.NextWave(waveIndex, siphonLevel)`; `MaxWaves = 5`. The dial is a
hand-authored table, never a formula — `TetherSiphon.RewardMultiplierFor` =
**1 / 1.10 / 1.15 / 1.25 / 1.35 / 1.50**.

```
dial                  = TetherSiphon.RewardMultiplierFor(siphonLevel)
enemyCount            = min(120, round((11 + 7*waveIndex) * dial))
shape                 = WaveShapes.For(wave): 1 Mass, 2 Pack, 3 Mass, 4 Pack, 5 Convoy   (M116)
hunter/thief/cloaker  = min(round(share * (11 + 7*waveIndex)), WaveDirector.KindCap)     (dial-proof)
defaultCount          = enemyCount - hunterCount - thiefCount - cloakerCount  (takes the whole surplus)
enemyHealthMultiplier = 1 + 0.40*waveIndex                       (x each kind's HP)
groupSize             = min(3 + waveIndex, 7, enemyCount)        (3 / 4 / 5 / 6 / 7)
spawnInterval         = max(0.4, 7.3 + 0.40*min(waveIndex, MaxWaves-1))  SECONDS BETWEEN GROUPS
gatekeepers, wave 1..5   L0 11/18/25/32/39
```

**A Wave is a SHAPE, not a rung on one ramp (M116, 2026-09-02).** `WaveShapes.For(wave)`
names what a Wave is and `WaveShapes.Apply` writes its four counts. A shape is shares of the
unchanged `11 + 7i` roster, Default absorbing the remainder:

| Shape | Default | Hunter | Thief | Cloaker | legal from |
|---|---|---|---|---|---|
| Mass | 100 % | 0 | 0 | 0 | wave 1 |
| Pack | 60 % | 35 % | 0 | 5 % | wave 2 (the Cloaker share from wave 4) |
| Convoy | 70 % | 0 | 15 % | 15 % | wave 3 (the Cloaker share from wave 4) |

Rounding and gates: each non-Default count is `round(share × (11 + 7i))`, midpoints up, taken
of the **dial-0 solo roster** so the dial's and the party's surplus stay all Default; then
capped by `WaveDirector.KindCap`. `HunterPerWave` / `ThiefPerWave` / `CloakerPerWave` are the
per-Wave CEILINGS now, not the counts: **0** before the kind's debut (Hunter wave 2, Thief
wave 3, Cloaker wave 4) and `round(largest share × roster)` after it, so a legal shape is never
clipped and the debut gate is one fact in one place. Default is the remainder and the four
counts always sum to `enemyCount`. Which Wave draws which: a zone's opening alternates Mass,
Pack from its first Wave, and its terminal-called last Wave (M115) is a Convoy where the Thief
is legal, else a Pack — **1 Mass, 2 Pack, 3 Mass, 4 Pack, 5 Convoy** (the table below). Waves
off the zone table alternate Mass/Pack; the finale pins its own (lever 3). So the Hunter is
first met on Wave 2, the Cloaker on Wave 4 (two, the Pack's 5 %) and the Thief on Wave 5 (the
Convoy's six) — still one debut per Wave. Spawn groups are authored by kind
(`WaveShapes.GroupByKind`): a Hunter group is four Hunters, not three Defaults and a Hunter;
the full groups' order is shuffled on the seeded stream and a kind's short last group is
topped up with Defaults. The prompt reads `[T] call the last wave — Convoy`; the log carries
`zone start … shapes=Mass` and `T5 pressed — zone 2's last wave answers shape=Convoy`.
**Falsifier, from the log (M116 criterion 2):** Convoy Thief kills sit at a median `hatchDist`
closer to the Hatch than Pack Hunter kills, and the door's remaining pool on `wave N/5
cleared` falls on the Convoy and not on the Pack. Three untainted Boardings that fail it revert
the lever rather than retune it.

**`spawnInterval` is a MEAN, not a metronome (M116, 2026-09-02).** `WaveCadence.Schedule`
turns it into a seeded sequence at launch: groups come in pairs, the second `0.25–0.45 ×` the
mean after the first, and each pair is followed by a lull that repays it, so every two gaps
still sum to two means and the Wave's delivery window is exactly what the line above says.
Whether a Wave opens on a burst or a lull is the seed's; an odd last gap is the mean. Nothing
else moves — roster, group width and window are the 2026-08-27 lever and stay. At Wave 1 that
is a pair `2.6 s` apart, then `12.0 s` of nothing, then the fourth group `7.3 s` on. Read from
the log's `group k … breached at` timestamps: the falsifier is a coefficient of variation on
the inter-group gaps under **0.4**, or a peak `N live` no higher than the metronome's.

**`spawnInterval` is a GROUP gap now — the name did not change with it.** A Wave
lands in `(ceil(count/groupSize) - 1) * gap` = **21.9 / 30.8 / 32.4 / 42.5 / 44.5s**; read as a
per-individual gap the same Waves take far longer and no fight ever starts.

**THE ROSTER AND THE GAP ARE ONE LEVER** (2026-08-27). A Wave lands in `ceil(count/groupSize)`
groups, so the ~40% roster cut shortened every window by about 40% while group width stayed at
3/4/5/6/7 — delivery collapsed to 12.6 / 17.6 / 18.4 / 24.0 / 25.0 s. Body ARRIVALS barely moved,
so nothing in a bodies-per-second reading showed it, but each body now carries 5/3 the HP: **HP
pressure per second rose by that factor, Wave 5 from 114 to 203 HP/s**, silently undoing the
widening bought hours earlier to stop bodies stacking. The gap was re-solved to `7.3 + 0.40*w` to
hold HP/s at the pre-cut figures (25.1 / 40.9 / 69.4 / 82.8 / 113.9 against 25.7 / 40.9 / 61.6 /
92.8 / 114.4). Waves 3 and 4 sit ~12% off in opposite directions because `ceil()` is a step
function and a linear gap cannot follow it.

**The gap WIDENS with the wave; it used to narrow** (2026-08-27). Delivery time was near
constant at ~30 s while the roster grew 18 to 66, so arrivals climbed 0.86 to 2.16/s against a
player who cannot acquire targets faster than about 0.95/s — the 103-body wall at Wave 5. Group
width was already rising 3 to 7, so the old shrinking gap compounded with it instead of
offsetting it. The clamp holds probe indices at the last authored gap, matching `Row()`. First
field to check if a Wave feels empty. **The dial's surplus is all Default** (a shape's shares
are taken of the dial-0 roster, never of the dialled count), so a dialled Wave 4 is the same
eleven Hunters and two Cloakers in a bigger crowd and a dialled Wave 3 is a bigger Mass with
**no Cloaker at all** — the Hunter debuts on Wave 2, the Cloaker on Wave 4 and the Thief on
Wave 5, one Wave apart, so that two new rules never land together. A bigger fight, not a
different one: the dial cannot multiply the scariest Gatekeeper, nor multiply Cloakers into
frozen free salvage.

**`TerminalChainGap = 8s`** (`WaveDirector`). Unused by the field loop: `T` at the Hatch
starts one Wave (Zo, 2026-08-25), `T` at the zone's terminal ends it, and Prep afterwards is
untimed. The constant stays
because `ArmSequentialDraws` still exists for later terminals that re-lock banking. Not
`InterWaveGap` (3 s after *delivery* during a deep-start overlap). Do not retune
`InterWaveGap` to steal this job.

**One table drives reward AND body count, on purpose**: `WaveDirector` reads
`TetherSiphon`, so payout and pressure cannot drift apart (it used to charge on three
axes at once: +600% Gatekeeper HP for +50% money). +50% bodies for +50% money is break-even
on paper; the wager is the residual, since gatekeepers overlap and leak. **Siphon 0 is the
baseline** every table below rests on:

<!-- GKB:GEN waves — WaveDirector.NextWave(i, 0) -->
| Wave | Bodies | Default | Hunter | Thief | Cloaker | HP × | groups of | Every |
|---|---|---|---|---|---|---|---|---|
| 1 | 11 | 11 | 0 | 0 | 0 | 1 | 3 | 7.3 s |
| 2 | 18 | 12 | 6 | 0 | 0 | 1.4 | 4 | 7.7 s |
| 3 | 25 | 25 | 0 | 0 | 0 | 1.8 | 5 | 8.1 s |
| 4 | 32 | 19 | 11 | 0 | 2 | 2.2 | 6 | 8.5 s |
| 5 | 39 | 27 | 0 | 6 | 6 | 2.6 | 7 | 8.9 s |
<!-- GKB:END waves -->

<!-- GKB:GEN dial — TetherSiphon.RewardMultiplierFor, NextWave body counts -->
| Dial | You earn | Wave 1 sends | Wave 5 sends |
|---|---|---|---|
| 0 | ×1.00 | 11 | 39 |
| 1 | ×1.10 | 12 | 43 |
| 2 | ×1.15 | 13 | 45 |
| 3 | ×1.25 | 14 | 49 |
| 4 | ×1.35 | 15 | 53 |
| 5 | ×1.50 | 17 | 59 |
<!-- GKB:END dial -->

Body count scales linearly with connected players: `min(120 × n, round((11 + 7×i) × dial) × n)`.
`n = 1` is the table above. Hunters, Thieves and Cloakers stay at the shape's solo counts; surplus is Default.
Bank cap at conversion is solo expected unbanked for waves 1..DeepestWaveReached at dial 0 /
scavenging 0, and applies only when `n > 1`.

<!-- GKB:GEN player-count — WaveDirector.SupportedPlayerCount, NextWave n, HatchBank.BankCap -->
| | |
|---|---|
| supported | 3 (host plus two) |
| capacity | 6 |
| wave 1 bodies ×1 / ×3 | 11 / 33 |
| bank cap deepest 1 | 462 |
| bank cap deepest 2 | 1218 |
<!-- GKB:END player-count -->

**Ante parked 2026-08-30.** `Ante.Enabled` is false; Waves run at dial 0. That flag is the return switch. CostPerStep stays in Sim, so this block stays generated from it.

<!-- GKB:GEN intensity-juice — IntensityJuice.CostPerStep -->
| | |
|---|---|
| juice per step | 100 banked |
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

**Repair requires the Repair Rig (M23).** The equipment slot unlocks once the realm has
earned any salvage (`LifetimeSalvageEarned > 0` — re-keyed with the wheel on 2026-08-25 when
the Convert left the demo; `HatchConversions >= 1` before that, `M78`) and costs Salvage once; the rig is bought on top and equipping it is
automatic (one piece). Without the rig equipped, repair is refused and the Hatch prompt
names the next missing step. FABRICATION's old `RepairRigLevel` track and CREW QUARTERS'
`EngineeringLevel` are deleted, and so is the migration that used to grant the slot and rig
free: schema 13 split the save into a Boarder and a realm and ended migration outright, so a
document below `SaveFormat.CurrentVersion` is refused by name rather than reshaped.

<!-- GKB:GEN hatch — ShipSystems, MetaUpgrades.HatchHp, EquipmentCatalog -->
| cost | value |
|---|---|
| Hatch HP (Armor 0) | 600 |
| Hatch HP (Armor 5) | 750 |
| per HP healed | 2 Salvage (`ShipSystems.RepairSalvagePerHp`) |
| equipment slot | **300** Salvage, once, after the first bank |
| repair rig | **300** Salvage, flat 8 HP/s |
<!-- GKB:END hatch -->

## Hatch-bank (M53)

**Out of the demo's critical path since 2026-08-25** (`Decisions.md`, *Locked — kills bank as
they land*): kills credit `MetaState.BankedSalvage` directly, so the carried pile is 0 unless
the player deliberately ran `RunSim.Withdraw` at the Hatch, and the window cannot start on a
pile the player did not choose to build. It stays in code because Master Control's finale runs
on it. Convert and Hatch composition both read field Waves cleared since the last Hatch-bank
(snapshotted at start). The window is `DurationSeconds`; the pile hitting 0 does not end it.
Leftover dumps when the timer ends.

<!-- GKB:GEN hatch-bank — HatchBank.DripPerSecond, DurationSeconds, CarryConvert, CarryWaveIndex, WithdrawPerSecond -->
| | |
|---|---|
| drip | 8 staked/s |
| duration | 45 s |
| withdraw | 16 salvage/s |
| returned keeps | 10% of a leftover stake (Lost wipes the stake, never the bank) |

| Waves carried | convert | Hatch Wave | bodies at intensity 0 |
|---|---|---|---|
| 1 | ×1.00 | 1 | 11 |
| 2 | ×1.10 | 2 | 18 |
| 3 | ×1.25 | 3 | 26 |
| 4 | ×1.35 | 4 | 32 |
| 5 | ×1.50 | 5 | 39 |
<!-- GKB:END hatch-bank -->

## Respawns (M98 recut)

One rule prices every dead seat's way back into a live run — guest, host, solo alike, at any
point in `RunPhase.Tethered`. **The purse is the realm's bank** (`RunSim.TryRespawn` →
`SpendBanked`, re-pursed 2026-08-25 with the award path): the carried pile it used to bill no
longer fills on its own, and one pot is the point — a death now competes directly with a Tier,
and a crew is solvent together or not at all. Affordability rises with every body, because
every body banks. There is no charge pool and no mid-Assault gate. The counter is the run's
rather than the seat's, so a party that keeps dying prices itself out together. The old pair — free embark on
Hatch-bank Prep, flat 450 buy-back on Hatch-bank Assault — is retired (`Decisions.md`,
*Locked — co-op down*). `BaseCost` = 150 is one Secondary MK-1 purchase, so the first death
is affordable off one good push and the third is not casual. The rule is leverage **#5**; the
numbers are **#12**. Cap and bleed-out timer are the generated block below.

<!-- GKB:GEN respawns — Respawns.BaseCost, CostFor, Refuse -->
| | |
|---|---|
| paid from | the realm's bank; a staked pile is never touched |
| cost | 150 × min(taken + 1, 3) |
| first three | 150 / 300 / 450 |
| climbs to | 450 and stops — every later death is the same price |
| cap | 2 per run, all seats together |
| bleed-out | 10 s to buy back, or the Boarding ends |
| refused when | the cap is spent, or the bank is short of the price |
<!-- GKB:END respawns -->

Master Control is `MasterControl.TerminalWaves` control-room Waves at the Wave-5 size, drawn
with the finale's own shapes (`MasterControl.ShapeForTerminalWave`: Mass, then Pack — never
`WaveShapes.For` of the clamped roster index, which would hand it Wave 5's Convoy), a gap of
`WaveDirector.InterWaveGap` between them, then a quiet beat of `MasterControl.FinaleBeatSeconds`
before the elite is drawn (`RunSim.AdvanceMasterControl`, logged `master-control beat — 12 s
before the elite`). Five became two on 2026-09-03 (M116, lever 3): five identical Wave-5 rosters
were 57% of the 2026-09-02 run (`playthrough.md`, claim 5) and read as a grind, not a finale;
the beat is Valve's finale-script delay of 12 s before the tank, where the old 3 s was a gap the
player could not tell from the one between Waves.

The finale Wave is the elite and its escort: one Thief at `MasterControl.FinaleEliteHp` (240
pre-multiplier; Wave-5 Thief spawned HP is `ThiefHp` 200 × 2.60 = 520, the elite is 240 × 2.60
= 624) behind `EscortHunters` 4 and `EscortCloakers` 2 — seven bodies in two groups, the escort
first and the elite alone one Wave-5 group gap later (`WaveShapes.GroupByKind` puts the Thief
block last on a `finaleElite` spec; `ApplyFinaleElite` sets the group width to the whole escort).
The shield rule is `MasterControl.EliteShielded(escortAlive)`: the elite refuses damage while
any escort body stands, applied from the spawner's escort ledger (`WaveSpawner.FinaleEscortAlive`)
on the elite's hit path (`EnemyGatekeeper.RefuseIfShielded`), with the flash and the flinch
kept so a shielded hit reads as no damage rather than no hit. It logs `finale elite shielded —
n escorts standing` once, and `finale elite exposed` when the last escort falls. Four Hunters
and two Cloakers because those are the two kinds that make the player move rather than hold a
line; the counts are a first cut — `unknown — needs play data`. `FinaleEliteHp` did not move with
the 2026-08-27 roster pass or with the escort, so the elite still stands 1.2× a stock Wave-5
Thief where it stood 2× (624 against the old 120 × 2.60 = 312). The trickle is off for the
whole sequence (`GameController.FieldWaveAwaitsShutoff` refuses on `MasterSequenceLive`), and
docking is refused throughout.

Falsifiers, from an untainted log (M116 criterion 3): the finale's share of a winning Run —
`master-control started` to `master shutoff banked` against the `[Run] summary` duration — at
or under 30%; every escort kill line before the elite's; the elite's group breached no sooner
than 12 s after the second control-room Wave's last kill.

<!-- GKB:GEN master-control — MasterControl.TerminalWaves, ShapeForTerminalWave, FinaleBeatSeconds, EscortHunters, EscortCloakers, FinaleEliteHp, WaveDirector.InterWaveGap -->
| | |
|---|---|
| terminal Waves | 2 — Mass, then Pack, at the Wave-5 size |
| gap | 3 s |
| beat before the elite | 12 s |
| escort | 4 Hunters, 2 Cloakers — the elite is shielded until they are down |
| elite waves | 1, and killing it arms the terminal |
| finale elite HP | 240 |
<!-- GKB:END master-control -->

Arithmetic for the 300/300 pair: Waves 1 and 2 clear for 1,218 at dial 0 (the bank-cap row
above, 29 bodies at `SalvagePerKill` 42), so 600 fits inside one post-loss Run alongside a
400 wheel purchase. **The equivalence the pair was priced on is gone**: 600 bought exactly one
full repair of the 300 HP door it was set against, and at `ShipSystems.HatchMaxHp` 600 a full
repair is 1,200 — slot plus rig now costs half a door, not a whole one.

Scale of the per-HP cost: a full repair of the Hatch at Armor 0 is **1,200 Salvage**
(`ShipSystems.HatchMaxHp` 600 at 2/HP). Measured
two-Wave Runs on 2026-08-04 grossed 1,072 / 1,182 / 1,075 (pre-M22 figures, so inflated by
roughly 20), which put a full repair of the door as it then stood at about half a Run's take and
puts a full repair of the raised one at **about a whole Run's take** — the door doubled and the
per-HP rate did not.

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
| gatekeeper-seconds / charge | `120 / 3.43 ≈ 35` (~20% of the door's ~175 gs budget: `ShipSystems.HatchMaxHp` 600 / 3.43) |

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

**Kill awards floor once (M22).** `AwardKill` takes `floor(CurrentKillValue)` straight into
`MetaState.BankedSalvage` and `SalvageLedger.RunSalvage` (`BankKill`). Pre-M22 logs
that quote an end-of-Run `earned` figure are not comparable to post-M22 `gross` / `net`
lines: the old tally kept fractional kill value and floored once at report time, so a
fractional dial overstated the take by about `0.25 × kills` at dial 3 (measured 19 and 21
Salvage of drift on 2026-08-04 Runs). Instant kill-into-bank ended with M53 and came back on
2026-08-25 — logs between those two dates are the only ones where a kill was not money.

## Practice range (M117)

`Sim/Range/PracticeWave.cs`, `Sim/Run/RunSim.Practice.cs`. A range wave is one kind at one
size on wave 1's beat. Zo, 2026-09-03: "Distance - default to 20 m / Wave Size - X, small,
normal, large / Cooldown - no bypass. reset them on wave start."

| | | derivation |
|---|---|---|
| small | **1** | one body: the time-to-kill read the range was built for (M99) |
| normal | **11** | wave 1's roster, `11 + 7i` at i = 0 — the normal a Boarding opens with |
| large | **25** | wave 3's roster, `11 + 7i` at i = 2 — the first wave the shape table stands a Thief on |
| breach | **20 m** default; 5 / 14 / 20 / 30 / 60 / 90 at the terminal | from the firing line; the first cut's closets sat 90 m out and put the fight a minute from the press; the marks are the three Primaries' range figures, two short marks and the default (Zo: "a dimension for distance ... triggered at a terminal") |
| beat | wave 1's | group width, mean gap and health multiplier are `WaveDirector.NextWave(0)`'s; the roster is rewritten to one kind, no debut gate |
| cooldowns | reset on draw | every timer to zero once as the wave is drawn; no bypass |

## Upgrade tracks

**Two cost curves, on purpose.** Weapon purchases are linear and reachable because weapons
are the progression. Hatch Armor keeps the doubling curve. CREW attribute tracks are gone
(save v8 refund). Default Protocol is equipped at CREW QUARTERS, not bought as levels.

**Weapon upgrade wheel** (`Sim/Meta/WeaponWheel.cs`) — five attributes (reload, clip,
damage, fire rate, ability). One purchase awards **5 / 3 / 3 / 0 / 0** by ring distance.
Ten purchases, 110 points. Tier is derived (`total / 11`), never stored. Salvage cost
keys on how many times that spoke was the centre (`WeaponTier.Cost(slot, centredCount)`),
not on derived Tier — stacking Damage raises the next Damage buy; Reload stays at its
own count. Neighbour +3 does not raise the neighbour's price. Rule is leverage #5; the
400/150 curves are #12 and did not move.

<!-- GKB:GEN tier-cost — WeaponTier.Cost, SpentToReach -->
Cost of the next purchase on one centred spoke, Primary slot `400 + 200 × centredCount` = 400/600/800/1,000/1,200/1,400/1,600/1,800/2,000/2,200,
**13,000** cumulative to stack ten on that spoke. Secondary slot `150 + 150 × centredCount` = 150/300/450/600/750/900/1,050/1,200/1,350/1,500,
**8,250** cumulative. Neighbour spill does not raise a neighbour's next price. Ten purchases still cap the weapon.
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
| MK-1 Sidearm | 50 | 1 | 1.4 | 8 | 1.4s | 45 m, falloff from 12 m | — | 56.2 |
| SEC-D | 18 | 1 | 6.0 | 4 | 2.2s | 8 m, falloff from 3 m | — | 25.1 |
| VK-7 Rifle *(disabled)* | 10 | 1 | 5.5 | 25 | 2.1s | 60 m | — | 37.6 |
| BR-12 Shotgun | 19 | 7 | 1.2 | 6 | 2.6s | 14 m, falloff from 8 m | — | 105.0 |
| MR-35 Rifle | 18 | 1 | 8.0 | 27 | 2.463s | 60 m, falloff from 25 m | 2 bodies ×0.26 after first | 83.2 |
| PRI-C | 180 | 1 | 0.6 | 4 | 3.2s | 90 m, falloff from 55 m | 3 bodies ×0.75 after first | 73.0 |

**MR-35 16 → 18 damage** (Zo, 2026-09-02, "increase MR-35 damage"; #12). Sustained 74.0 →
83.2. One shot fewer on waves 1, 2, 3 and 5 of the Default ladder — 3/4/5/7/8 shots on
50/70/90/110/130 where it was 4/5/6/7/9. 18 is the highest whole number that keeps two stated
rules: the deepest Thief stays out of one magazine (27 × 18 = 486 against the wave-5 Thief's
520, so the heavy still costs a reload; the boundary moves from wave 4 to wave 5), and the
field still out-delivers a T0 rifle before the last wave (`SiphonAndWaveTests`' crossover stays
on wave 4; at 19 the wave-4 body drops to six shots and the crossover moves to wave 5, which
that pin refuses as "a spike, not a run"). What this gives up: PRI-C no longer shadows the
rifle on dps (73.0 against 83.2) and BR-12 no longer
matches it on kills/s across the ladder; each keeps its own axis (deepest body, pierce line)
and the dominance pin. Against the 2026-09-02 play: the T0 rifle lost wave 5 twice and the
T5 rifle walked it; whether 18 moves the T0 wall is **unknown — needs play data**.

**Primaries cost 1,500 / 2,000 / 2,500** (Zo, 2026-09-02; #12; were 900 / 1,200 / 1,500). The
ARMORY's Primary rack opens on Salvage EARNED — `DemoProgress.PrimaryRackSalvage`, the cheapest
of these — so the price is the gate. Wave 1 alone pays 11 × 42 = 462 and a zone-1 clear
29 × 42 = 1,218, so the BR-12 is a zone-1 clear plus seven kills: the second Boarding at the
latest, never a Boarding the player did not fight. 3:4:5 kept; PRI-C is two zone-1 clears.
Whether that is the right distance is **unknown — needs play data**.

**THE PRIMARY TIER WAS RAISED ~2x BECAUSE THE FREE SECONDARY OUT-KILLED IT** (2026-08-27,
Zo's ruling: "It is the rifle. We should push the primary damage up"). BR-12 6 → 12/pellet,
MR-35 8 → 16, PRI-C 90 → 180. `EnemyHealthPerWave` was restored 0.30 → 0.40 in the same pass
("can scale health on later waves"), putting the Default ladder back to 30/42/54/66/78 —
**read that ladder as history**: `EnemyHealthPerWave` is still 0.40, but the fewer-and-tougher
pass below raised `BaseEnemyHp` 30 → 50 the same day, so the live ladder is 50/70/90/110/130.

The MK-1 Sidearm sustained **59.0 dps at cost 0** (56.2 since the 2026-09-02 recut above)
against Primaries that cost 1,500-2,500 Salvage (900-1,500 until 2026-09-02)
and sat at 33.2 / 37.0 / 36.5. It is not offset by ammo starvation, which was the obvious
defence and is wrong: the Secondary's `0.4 rounds/s` regen
(`WeaponAbility.SecondaryRegenPerSecond`) is a never-dry FLOOR, not a throttle —
`PlayerGun.TickSecondaryRegen` returns early while `reloadTimer > 0`, so a manual 1.4 s reload
still owns the clip and the full figure is genuinely sustainable.

**Judge this ladder on kills/s against the HP table, never on the dps column.** Duty-cycle dps
ignores overkill, and at the 30 damage of that hour a 70 HP Wave-2 body cost the sidearm three
whole shots (two at today's 50).
Measured at EQUAL TIER (T0) against the 30/42/54/66/78 ladder that stood that hour, the sidearm
led the old rifle by 6% at Wave 2 and 70% at Wave 4 — not the flat 60% the dps column implies.
The granularity is the whole reason
the gap swings that far, and it is why an argument from dps alone reached the wrong answer
twice while this was being tuned. After the raise the MR-35 leads on every wave of the ladder.

BR-12 and PRI-C read LOW on single-target kills/s (0.789 and 0.405 at T0) and that is not a
defect: their value is multi-body — 7 pellets and a 3-body pierce — which a single-target
metric cannot see. Do not "fix" those two by reading that column.

**FEWER AND TOUGHER, 2026-08-27** (Zo: "lower the volume of enemies and make them tougher...
reducing volume by at least 40%"). The roster went `18+12i` → `11+7i` (39-41% down on every
Wave) and every kind's `maxHp` rose by 5/3 — Default 30 → 50, Hunter 20 → 33, Thief 120 → 200,
Cloaker 45 → 75. The total HP pool per Wave lands within 2% of what it was, so this is a
LEGIBILITY change, not a difficulty cut: the same work arrives as fewer, longer-lived targets
in a corridor whose firing angles were widened in the same pass.

Two things moved with it that are easy to miss, and both were side effects rather than intent:

- **`SalvagePerKill` 25 → 42.** A kill is priced FLAT and never reads the body's HP, so cutting
  the roster 40% cut run income 40%. Every price in the game is denominated in kills — respawns,
  Primaries, tracks — so the roster silently re-prices the whole shop unless this moves with it.
  Held to the decimal: a five-Wave run paid 5,250 before and pays 5,250 now.
- **BR-12 damage 12 → 19/pellet.** Buys no power. It one-shot every body in the table before and
  one-shots every body now (7 × 19 = 133 against a deepest Default of 130), so its kills/s is
  unchanged at 0.789 flat. Without it the raised ladder would have pushed a Wave-3 body past the
  volley and HALVED the weapon — a cliff introduced by an enemy change, in a weapon nobody was
  retuning. Its dps column now reads 105 and that number is overkill, not power.

Base dps is the duty-cycle formula with pellets folded into the pull:

```
dps    = clip * damage * pellets / (clip/rate + reload)
BR-12  = 6 * 19 * 7 / (6/1.2 + 2.6)    = 798 / 7.60 = 105.0
MR-35  = 27 * 18    / (27/8.0 + 2.463) = 486 / 5.84 = 83.2
PRI-C  = 4 * 180    / (4/0.6 + 3.2)    = 720 / 9.87 = 73.0
```

**What a full damage focus buys** (ten purchases, 13,000 Salvage, `WeaponTier.Cost`). BR-12
centres Damage, so Damage takes 50 points and its ring neighbours Clip and Ability take 30
each, while Reload and FireRate take none: damage `12 x 9.30 = 111.6`, clip
`round(6 x 2.31) = 14`, cycle `14/1.2 + 2.6 = 14.27s` → `14 x 111.6 x 7 / 14.27 =` **767 dps**.
MR-35 centres Damage between Reload and Clip: damage `18 x 9.30 = 167.4`, clip
`round(27 x 2.31) = 62`, reload `2.463 x 0.55 = 1.355s` → **1,140 dps** (1,013 at the old 16). The first single
purchase on that line is worth **+31%** (BR-12 `66.3 → 86.9`), which is the number the noise
floor below is measured against. Whether Wave 5 still clears against a 767/1,140 ceiling is
**unknown — needs play data**.

**A rate-for-damage swap is never ceiling-neutral, even when it is base-neutral**
(2026-08-27). The MR-35 was first recut `32/1.6/10` → `8/8.0/27` purely for the assault-rifle
fantasy, with the reload solved to hold base dps at exactly 37.0 — and that same swap still
moved its focus ceiling 436 → 507. Reload is a FIXED cost amortised over `clip/rate` seconds,
so the wheel's clip multiplier buys more from a long firing window than a short one. Any weapon
re-expressed at a higher rate for feel gains ceiling for free unless its reload is re-solved at
focus rather than at base. The subsequent damage raise compounds it: the ceiling went to 1013,
and the 2026-09-02 raise to 18 takes it to 1,140.
Treat that as arithmetic, not a measurement — this file's own rule is that the ceiling question
is answered by play data.

**Hatch Armor** (`Sim/Meta/UpgradeTracks` + `MetaUpgrades.cs`) — `base * (1 + 0.05 * level)`,
exactly +25% at level 5. Crit chance is frozen at 0; there is no Luck shop. Engineering and
`RepairRigLevel` are off the enum entirely, and off the save with it — the schema-13 split
ended migration, so nothing converts an old track into a free equipment slot any more
(`Sim/Meta/SaveFormat.cs`).

<!-- GKB:GEN tracks — UpgradeTracks.All, MetaUpgrades.NextCost -->
Cost `200 * 2^currentLevel` = 200/400/800/1,600/3,200, **6,200** to max a track.

| Track | Station | Level 0 | Level 5 |
|---|---|---|---|
| Hatch Armor | FABRICATION | 600 HP | 750 HP |
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

## Wave-report dashboard (M38, re-cut by M103)

`Docs/dashboard.html` Session tab parses `GatekeepersProtocol_*.log` (current;
`GatekeeperBastion_*.log` still parse) in the browser after you
choose the Logs folder (or drop the files). Focus / Project tabs read
`Docs/dashboard-data.js` from `Tools/dashboard-data/run.ps1` (milestones, commits, questions,
rule-based next focus). Sample filters and era cutoffs.
wave-report still prints every Run; Dev and config taint are soft-excluded from summary means only.

**Harness quarantine (M103).** A PlayMode test session writes its log under
`Logs/harness/` — `LogFile` detects the Unity Test Framework's generated
`InitTestScene*` first scene, never the `IntegrationCheck`/`AutoVerify` tag. Human
sessions keep `Logs/` (30 kept); harness keeps 6. Trigger: on 2026-08-24, 19 of the
day's 28 files were harness carrying ~550 synthetic Runs, and the 30-file prune had
real sessions hours from deletion. The Session tab's *Include PlayMode* toggle is now
**off by default** for pre-quarantine files still in `Logs/`; everything kept remains
labeled, never silently dropped.

**Run summary Metric line (M103, schema 2 since 2026-09-04).** Every Run end also emits one
machine line beside the prose (`GameController.EndScreen.LogRunMetric`):
`[Run] summary schema=2 result=<lost|stripped|docked|abandoned> cause=… wave= startWave=
duration= gross= repair= net= banked= respawns= seats= dial= checkpoint= config=
configTaint= dev= protocol= scene= seed=`. **Schema 2 adds `protocol=`**, the named
protocol the Run was played under, because schema 1 could not tell a FORGE run from a
protocol-less one and every balance reading averaged the two. The run-begins line carries
`protocol=` and `passive=` for the same reason. Readers match the `summary schema=` prefix
rather than the number, so schema-1 logs still parse. Facts only — kills and deaths stay on their event
lines and the readers do the arithmetic (the no-counters-in-Assets decision,
`Decisions.md`). `checkpoint`/`config` name the F12/F11 beat and loadout row the
session was built from. **`configTaint` is beat-relative** (owner ruling 2026-08-24):
configurations grow valid as the beats progress — a row granting a Primary is tainted
before the beat where the Primary rung unlocks, and a kit costing more than the
progression had earned by that beat is tainted anywhere (a max-upgraded kit always
is). The rule (`GameController.CheckpointConfigTainted`) reads both facts off the
constructed beat state (`DemoRungTwoOpened`, `LifetimeSalvageEarned`) **before** the
overlay lands, so it follows the beat builders with no hardcoded table; pinned by
`CheckpointConfigTaintTests`. Verdicts today: row 1 (MK-1 T2) valid from beat 3, rows
2–4 (PRI-C T2 + protocol) valid from beat 5. A checkpoint Run with a valid row counts
as play data; `dev=1` records only that the session's save is suppressed. Adding a
field is compatible; renaming or removing one is `schema=2`.

The dashboard's **Checkpoint configurations** panel lists every observed (beat,
config) cell with the game's verdicts and a per-cell override (clean / tainted /
game rule) for tuning the classification from the page — overrides are
browser-local (`localStorage`), re-classify those Runs across every panel on the
spot, and never touch the game's rule; a *mixed* verdict on one cell means the beat
economy changed between those Runs.

| Constant | Value | Source |
|---|---|---|
| Short Run label | **5 s** | Runs shorter than this are labeled `short` (crashed / aborted / PlayMode harness churn). Measured from `run begins` to `RUN OVER` / `BASTION STRIPPED` / `DOCKED` / next `run begins`. |
| PlayMode discriminator | `stage=playmode` in the log | File-level, for pre-quarantine files; new harness sessions live in `Logs/harness/` and are never read. Labeled `playmode`; excluded from the sample by default, still counted on the page. |
| Harness quarantine | first scene `InitTestScene*` | `Util/LogFile.SessionBanner` → `Logs/harness/`, keep 6. |
| Era: pre-weapon | filename date **≤ 2026-08-03** | `Docs/_meta/Logs/` 2026-08-02/03, pre-horde (`2-Dynamics.md`). |
| Era: horde retune | **2026-08-04 … 2026-08-07** | After M10 / horde body counts; before wheel-era sessions dominate. |
| Era: weapons and wheel | **2026-08-08 … 2026-08-12** | Primaries + upgrade wheel in session logs. |
| Era: hatch-bank loop | **≥ 2026-08-13** | Field terminals, Unbanked, Hatch-bank, intensity juice (parked in the current alpha, 2026-08-30), Master Control. |

`Tools/wave-report` remains the terminal/`--html` reader; the page is the bookmark.

---

Why these values and not others: `2-Dynamics.md`. What they are in service of:
`1-Experience.md`. What enforces them: `3-Mechanics.md`.

