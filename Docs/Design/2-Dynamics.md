# 2 — Dynamics

**Dynamics** in the MDA sense: the run-time behaviour of the mechanics acting on player
input and on each other. Leverage #6 information flows, #7 loop gain, #8 balancing loops,
#9 delays.

Nobody designs a dynamic directly. You build a mechanic, and the dynamic is what happens.
So this file is where a design intention gets checked against what the game actually did,
and everything in it is measured or derived. Two balance models have already been falsified
by play data; one is kept below, marked wrong, so it is not re-derived.

**A pacing or difficulty complaint usually lands here rather than in** `4-Data.md`**.** The
largest effects on record in this project came from loop structure and from geometry, not
from stats.

---



## 2.1 The loop the player is actually in (#6)

Daniel Cook's skill atom: the player holds a **mental model**, takes an **action**, the
system responds with **feedback**, and the model updates. A mechanic the player cannot see
the result of is not in their loop at all — it is a number in a file.

Gatekeeper's Protocol's atom, one Wave at a time (2026-08-30):

```
model    "I can hold the door if I thin the group before it lands"
  |
action   T at the Hatch to begin; pick targets; repair; T at the zone's terminal to end it
  |
feedback hatch HP falling, gatekeepers parked on the door, the bank climbing per body, the end screen, the log
  |
model'   "eight parked is already lost" / "the bodies already paid; the only question is the door"
```

The Hatch is also the finale start once `ControlRoomReachable` (`GameController.StartZoneWaveFromHatch`). Juice (`IntensityJuice`) is a parked lever — `Ante.Enabled = false`, `Try` refuses — so it is not in this atom.

**Feedback is the return path, so #6 is a dynamics problem, not a presentation one.** A full
pass tuning gatekeeper damage 4 → 18 never reached the running game because the prefab held the
old values; what exposed it was a log line printing `took 4 damage`. Pure #12 work,
disconnected at the wire, invisible until feedback caught it.

Three channels, each with a rule:


| Channel  | Rule                                                                                                                                                                                                                 | Where                         |
| -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| The HUD  | fight and end screens draw through `Game/Ui/` views (`BastionHudView`); `HudController.OnGUI` is the Dev-taint banner only. Never `GUILayout` on the Bastion HUD. | `../Architecture.md` gotcha 3 |
| The log  | a line when a player-visible thing happened, or when a refusal must be answerable from the log alone ("I clicked BUY and nothing happened"). Once per distinct reason, never per frame. No counters, no aggregation. | `../Working-Rules.md`         |
| The docs | anything countable is generated from the code; anything arguable is written.                                                                                                                                         | `../README.md`                |
| The frame | a Gatekeeper reads against the surface behind it, at the range it is fought from. Legibility outranks mood: an art pass that darkens the room owns the contrast it removes. | `../Brainstorms/bodies-and-art-direction.md` |


**Darkness is a channel too, and it is the one that fails silently.** The 2026-08-20
lighting pass dropped ambient and repainted the atlas toward a near-black hull, which is
right for the look and is exactly the change that can take a Gatekeeper out of the
player's model without taking it out of the game. The reference this art aims at is dark
*with strong light pools*; a room that is uniformly dark instead reads flat and hides its
occupants. **The check runs on every pass that touches ambient, emission or albedo
range**: stand at the far end of the longest sightline in the zone and confirm each kind
is identifiable before it is in range — not merely visible, *identifiable*, since kind
refusals are the whole of `1-Experience.md` §1.4 and a player who cannot tell a Thief from
a Hunter is playing without the rule. Where a body loses against its background, the fix
is the body's own emissive, never raising ambient back — ambient lifts the room and the
enemy together and returns the flat look the pass just removed. The robot pack ships a
separate emissive map per kind for exactly this.

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
a Run is. The two things that could become arcs are the realm's first earned Salvage (which unlocks
upgrade wheel purchases and the equipment slot, and is the only one-time event in the game —
and since 2026-08-25 it fires on the first body, so it is a thinner arc than it was)
and the NAV MAP's second destination, which does not exist yet.

*Whether a Run needs an arc is unknown and needs play data.* It is the kind of question
that should be asked after Fellowship is a session a friend can use (`1-Experience.md`
§1.2 ranks 1–3), not before.

---



## 2.3 The four loops (#7 gain, #8 balance)

Two drive, two damp. R2 is the gain knob; parked 2026-08-30, so Waves run at dial 0.

### R1 — kills → Salvage → upgrades → faster kills

The progression loop, and the only reinforcing loop in the meta. `AwardKill` credits
`MetaState.BankedSalvage` directly as each body falls (`RunSim.BankKill`, Zo 2026-08-25) —
there is no conversion step between a kill and spendable money, and therefore no way to
fight a Run and bank none of it. Kill value is flat `MetaUpgrades.SalvagePerKill()` × intensity (`TetherSiphon.RewardMultiplier`).
Banked Salvage buys upgrade wheel purchases (`Sim/Meta/WeaponWheel.cs`) and Hatch Armor.
CREW QUARTERS does not sell attributes; Default Protocol is equipped, not bought.

**A purchase is 11 points spread over three adjacent spokes, so R1's gain is no longer one
number.** `ShipCommands.BuyWheelUpgrade` centres one upgrade spoke and awards
`FocusAward = 5` to it and `NeighbourAward = 3` to each neighbour, 0 to the far two:
`PointsPerPurchase = 11`, `MaxPurchases = WeaponTier.MaxTier = 10`. Points compound per
point, at `DamagePerPoint = 1.0456`, `ClipPerPoint = 1.0283`, `ReloadPerPoint = 0.98035`,
`FireRatePerPoint = 1.0117`. Ability carries no dps term.

Step size is therefore a function of where the focus lands. MR-35 (`WeaponCatalog`: damage
32, 1.6 shots/s, clip 10, reload 2.4s) with its spoke order `Reload, Damage, Clip, Ability, FireRate` (`WeaponWheel.RingOf`), at `WeaponWheel.EffectiveDps` =
`clip × damage ÷ (clip ÷ rate + reload)`, is 10 × 32 ÷ (10 ÷ 1.6 + 2.4) = **36.99 dps**
before any purchase. One purchase, by centre:


| centre   | points awarded                     | dps   | step   |
| -------- | ---------------------------------- | ----- | ------ |
| Damage   | Damage +5, Reload +3, Clip +3      | 48.15 | +30.2% |
| Reload   | Reload +5, Damage +3, FireRate +3  | 44.56 | +20.5% |
| Clip     | Clip +5, Damage +3, Ability +3     | 43.38 | +17.3% |
| FireRate | FireRate +5, Reload +3, Ability +3 | 39.22 | +6.0%  |
| Ability  | Ability +5, Clip +3, FireRate +3   | 38.94 | +5.3%  |


Arithmetic on the constants above, not measured. The Damage row in full: damage
32 × 1.0456^5 = 39.99, clip `round(10 × 1.0283^3)` = 11, reload 2.4 × 0.98035^3 = 2.261,
rate unchanged at 1.6, so 11 × 39.99 ÷ (11 ÷ 1.6 + 2.261) = 48.15. The old flat "~30% a
step" holds as the best case only; the worst case is a sixth of it, for the same
400–2,200 Salvage (`WeaponTier.Cost` = 400 + 200 × centredCount on that spoke over 0..9,
`Sim/Meta/WeaponTier.cs`).

**The loop does not close inside a Run.** Purchases happen only in the Ship scene, so R1
closes once per Run and a Run cannot spiral. That is a structural decision, not a tuning
one, and it is what makes a Run's difficulty a fixed quantity the moment you embark.

**And it does not close at all until the realm has earned something.** Wheel purchases and
the equipment slot are gated on `MetaUpgrades.UpgradesUnlocked`, which reads
`LifetimeSalvageEarned > 0` (re-keyed off `HatchConversions >= 1` on 2026-08-25 by Zo when
the Convert left the demo; off `PodEjections` before that, `M78`). Lifetime on purpose:
spending the bank to zero must not re-lock the wheel. A first Run is played at whatever the
loop's gain was before the loop existed, and exactly one Run is. Buying a weapon is not
gated. Hatch Armor and the protocol are not gated. Attribute tracks were refunded (save v8).

**Two cost curves in one shop, deliberately.** Weapons are the progression and must stay
reachable; Hatch Armor is the remaining doubling-curve track. Damage, fire rate and
clip left `MetaUpgrades` for `WeaponCatalog`, `WeaponTier` (the cost curve) and
`WeaponWheel` (the multipliers) to make that split structural rather than a matter of two
slopes. `UpgradeTrack` is `HatchArmor` only. CREW QUARTERS equips `BoarderProtocol`.

### R2 — intensity: the player's hand on the gain

`Sim/Meta/TetherSiphon.cs` is still the body-count table. Raising intensity multiplies reward and body count off the same hand-authored table, so payout and pressure cannot drift apart. They did once, and the result was +600% Gatekeeper HP for +50% money.

**Parked 2026-08-30.** The player cannot author this today: `Ante.Enabled = false`, Waves run at dial 0, and `IntensityJuice.Try` refuses the raise path. The model is juice of **banked** Salvage at a look-at trigger (`Sim/Run/IntensityJuice.cs`, re-pursed 2026-08-25): raise costs take-home progress, lower is free, `T` starts — R2's gain bought out of R1's output.

**Intensity adds only Default gatekeepers.** HP, group size and spawn gap ignore it. R2 raises
R1's gain by adding load to B1 and nothing else — one lever, one mechanism. Changeable in
Prep only, before the Wave is seen.

### B1 — kill rate against arrival rate

The loop that decides whether the door survives, and it is **much weaker than it looks.**

Hatch damage is `arrivals × dwell`, and raising dps cuts dwell while raising arrivals. Three
Wave-2 attempts, one purchase between each. The bought column names the pre-wheel
per-attribute levels; the measurement stands, the purchase shape it was bought with does not:


| attempt | bought              | kill rate | effective | killed | reached Hatch | duration | outcome |
| ------- | ------------------- | --------- | --------- | ------ | ------------- | -------- | ------- |
| 1       | Clip L2             | 0.81/s    | 28.2 HP/s | 58%    | 15            | 27.5s    | BREACH  |
| 2       | Damage L3           | 0.96/s    | 33.8 HP/s | 70%    | 19            | 29.2s    | BREACH  |
| 3       | FireRate L3, dial 0 | 0.93/s    | 33.1 HP/s | 83%    | 22            | 31.5s    | BREACH  |


Kill fraction rose 58% → 83% and arrivals rose 15 → 22, because surviving longer gives more
of the Wave time to walk to the door. **The two nearly cancel.** Three consecutive upgrade
purchases produced three spawn points, and the answer was a delivery-rate change rather than
more dps.

**Delivery rate is the lever that targets the mechanism.** Wave 2 at dial 0 delivered 1.74
bodies/s against a measured 0.93 kill rate:


| group gap       | delivery | in/s | leak vs 0.93 kill/s |
| --------------- | -------- | ---- | ------------------- |
| 2.47s (pre-M10) | 17.3s    | 1.74 | +0.81/s             |
| 3.00s           | 21.0s    | 1.43 | +0.50/s             |
| 3.50s           | 24.5s    | 1.22 | +0.29/s             |
| 4.00s           | 28.0s    | 1.07 | +0.14/s             |


M10 shipped `max(0.4, 4.2 − 0.20×wave)`. Earlier softens were not enough after the horde and
swing-landing fixes. **That sign was reversed on 2026-08-27** to
`max(0.4, 4.2 + 0.20×min(wave, MaxWaves−1))`, a gap that WIDENS as the Run escalates — and
**superseded again hours later the same day**, when the ~40% roster cut re-derived it. The
live formula is `max(MinSpawnInterval, SpawnGapBase + SpawnGapPerWave×min(wave, MaxWaves−1))`
— 7.30 / 7.70 / 8.10 / 8.50 / 8.90 s, not the 4.20 / 4.40 / 4.60 / 4.80 / 5.00 this section
argued toward. Cutting the roster while leaving group width alone had shortened every
delivery window by about the same 40%, so the gap and the roster are one lever, not two
(`WaveDirector.NextWave`; `../Decisions.md`, the M10 spawn-gap entry). The table above is
kept because its arithmetic is still how a gap converts to a delivery time, but read both
it and the 4.2 formulas as history.

**~~Kill rate is not constant across a Run.~~ FALSIFIED by play data** (2026-08-27). This
section previously read: *"Measured 1.23 / 1.50 / 1.67 per second on Waves 3 / 4 / 5 against
an earlier band of 0.61–1.08/s. Denser Waves mean less time walking between targets, so part
of each Wave's escalation is offset by the shooter getting more efficient inside it."*

A full five-wave playthrough
(`Logs/GatekeepersProtocol_2026-08-27_03-18-16_p17676.log`) measured **0.72 / 0.95 / 0.73**
on those same Waves — flat-to-declining, not rising, and roughly half the claimed figures.
The run ended `result=lost cause=player wave=5` with 103 bodies alive.

This mattered far more than a wrong number, which is why it is recorded rather than deleted:
the offset it asserted was **the stated justification for compressing delivery as Waves grow**.
A rising kill rate makes a shrinking gap self-correcting; a flat one makes it compounding, and
group width was rising 3→7 at the same time. Arrivals climbed 0.86→2.16/s against a player who
cannot acquire targets faster than about 0.95/s. The 103-body wall was this paragraph's
consequence.

**The lesson is the method, not the constant.** The claimed rates were never measured against
a Run that reached Wave 5 — waves 3–5 had no logged data at all when this was written — so a
band extrapolated from early Waves was used to justify a curve that only bites in late ones.
Per `Docs/Design/`: measure to find where to look, decide by playing.

### B2 — Hatch damage → repair spend → slower progression

Repair costs banked Salvage as healing is applied (`RunSim.RepairHatch`, 2 Salvage/HP), and
that spend is gone if you die afterward. Damage taken at the door is paid for out of R1.

**A Boarder without the Repair Rig cannot repair at any price, so B2 does not exist for that
loadout.** M23 moved the rig out of the tracks and into Equipment:
`GameController.RepairHatch` returns 0 unless `MetaState.HasRepairRigEquipped`, which needs
the equipment slot bought (`EquipmentCatalog.SlotPrice` 300, itself gated on `LifetimeSalvageEarned > 0`),
the rig owned (`EquipmentCatalog.Def(RepairRig).cost` 300) and the rig equipped;
`MetaState.RepairMissingStep` names whichever step is missing. Rigless, Hatch damage costs
nothing and damps nothing, and the Hatch only ever goes down.

**Repair discipline is a real choice with a real price, once you carry the rig.** The
three-deaths pacing target below holds before repair costs; a player who repairs fully each
Wave narrows the margin to 1.7× at Wave 2 and 1.2× at Wave 3, and fails at Waves 4–5.

---



## 2.4 The delays (#9)

Delays are what make the loops readable, and two of them are load-bearing.


| Delay                          | Length                                        | What it does                                                                                                                                          |
| ------------------------------ | --------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Deep breach → Hatch            | unknown — needs measurement (kit rooms, M61-2; graybox 48.5 m is retired) | Structure still dominates stats (`3-Mechanics.md`). The old 30×52 / 48.5 m figure is the graybox floor. |
| R1 closing                     | once per Run                                  | A Run's difficulty is fixed at embark; no in-Run spiral.                                                                                              |
| R1 unlocking                   | until the realm's first kill                  | Wheel purchases and the equipment slot gated on `MetaUpgrades.UpgradesUnlocked` = `LifetimeSalvageEarned > 0`. Buying a weapon is not. Hatch Armor and protocol are not. |
| Inter-Wave gap on a deep start | 3s                                            | A start-Wave of N chains Waves 1..N back to back.                                                                                                     |
| Reload                         | per weapon: MK-1 1.4s, BR-12 2.6s, MR-35 2.4s | A wheel spoke (`ReloadPerPoint = 0.98035`), so it shortens with focus.                                                                                |
| Stamina refill                 | 1s hold + 100/18 = 6.6s                       | Sprint beats every chase; since 2026-08-25 a walk also beats a Default's 4.6, so the reserve is what a Hunter costs.                                   |


**The five stamina numbers change together or not at all.** They encode one relationship —
sprint must outrun a chase for a bounded time — and moving one silently converts kiting from
a resource into a tactic. Since the 2026-08-25 speed cut (Default chase 5.6 → 4.6, under the
player's 5 m/s walk; Hunter 6.5 → 5.6, now the only kind that outruns a walk) that
relationship is carried by the Hunter alone, so it is the number to watch.

---



## 2.5 The pacing target — no Wave costs more than three deaths

**Stated intent:** a player who dies repeatedly on a Wave should be able to buy past it
within three failed Runs. It is a constraint on R1's gain, and it is what sized the Tier
curve.

Required effective dps per Wave assumes a survivable leak of 0.15 bodies/s and the measured
0.74 realization factor (theoretical dps → dps actually delivered):


| Wave | in/s | avg HP | need HP/s | need theoretical | cheapest Tier | cumulative cost |
| ---- | ---- | ------ | --------- | ---------------- | ------------- | --------------- |
| 1    | 0.86 | 30.0   | 21        | 29               | T0            | 900             |
| 2    | 1.07 | 35.8   | 33        | 45               | T1            | 1,300           |
| 3    | 1.38 | 49.3   | 61        | 82               | T3            | 2,700           |
| 4    | 1.88 | 60.9   | 105       | 142              | T6            | 6,300           |
| 5    | 2.16 | 75.5   | 151       | 205              | T7            | 7,900           |


**This table was derived pre-wheel and is kept as the record of what the seven-Tier decision
was sized on. Do not read the last two columns as current.** It assumed one Tier was one
fixed dps step for every buyer. Since M21 it is not: `WeaponWheel.DerivedTier` is
`TotalPoints / PointsPerPurchase`, which counts purchases and not what they bought, so two
players at Tier 10 having paid the same 13,000 Salvage (`WeaponTier.Cost` = 400 + 200 × tier,
cumulative 13,000) sit far apart. On MR-35 off a 36.99 base, ten purchases all centred on
Damage reach ~436 dps (arithmetic in `4-Data.md`) and a spread buy is **unknown, needs
re-derivation**. The pre-wheel seven-Tier record below still uses 7,000 / Tier 7.

Against three failed Runs' earnings at that Wave. **This earnings table is pre-M53 / pre-juice:**
it assumed a Run dying on Wave N banks the Waves below it plus ~60% of N. That assumption is
closer to true again than it was — since 2026-08-25 a kill banks as it lands, so a Run dying on
Wave N keeps everything it killed — but the juice curve and the wheel both moved underneath it.
Do not treat the margins as a current constraint without new logs.


| die on | earns/Run | ×3     | needs | margin |
| ------ | --------- | ------ | ----- | ------ |
| Wave 2 | 900       | 2,700  | 1,300 | 2.1×   |
| Wave 3 | 1,830     | 5,490  | 2,700 | 2.0×   |
| Wave 4 | 3,060     | 9,180  | 6,300 | 1.5×   |
| Wave 5 | 4,590     | 13,770 | 7,900 | 1.7×   |


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


|              | MR-35 **T1**              | MR-35 **T4**                 |
| ------------ | ------------------------- | ---------------------------- |
| Wave 2       | 272/300                   | 300/300                      |
| Wave 3       | breached                  | 300/300                      |
| Wave 4       | —                         | 300/300                      |
| Wave 5       | —                         | 300/300                      |
| repair spent | 56                        | 0                            |
| outcome      | lost on Wave 3 after 134s | BASTION STRIPPED, gross 5250 |


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
| ---- | ------------- | ------- | -------- | ------- | --------- |
| 1    | 18 (18/0/0)   | 540     | 13.0s    | 16.9s   | 8.6s      |
| 2    | 30 (26/4/0)   | 1075    | 17.3s    | 33.6s   | 17.2s     |
| 3    | 42 (34/6/2)   | 2070    | 18.7s    | 64.7s   | 33.1s     |


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

**Amended (Zo, 2026-08-17; dock 2026-08-30):** breach and death are no longer the only enders.
`DockOffered` opens after a survived Hatch-bank (`OfferDockIfBankSurvived`) and after a zone
shutoff (`OfferDockAfterZoneShutoff` from `ShutOffFieldWave`). Held interact at the Hatch
until the next Wave is triggered ends the Run **Docked**, not Lost; banked Salvage and
door state persist via the successful-bank snapshot (`../Decisions.md`,
*Locked — return to Ship at a surviving bank*). Every loss on record above predates that rule.

**Amended again (`M98` recut):** a death is not an ender on its own. A dead seat raises the
DOWN overlay and may buy itself back anywhere in `RunPhase.Tethered` for
`Respawns.CostFor(taken)` = `150 × (taken + 1)`, debited from the **realm's bank**
(`RunSim.TryRespawn` → `SpendBanked`; re-pursed 2026-08-25), capped at `MaxRespawns = 2`
with `BleedOutSeconds = 10` to pay (`Respawns.cs`, 2026-08-30). `EndRun("player")` fires
when the seat cannot buy back (bank or cap) and no other seat is still fighting, or when
bleed-out expires with nobody standing (`GameController.PlayerDied`, `NotifySeatWentDown`);
that death wipes any carry the seat had withdrawn, and the run is over. Nothing above this
line was logged under the respawn rule.

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