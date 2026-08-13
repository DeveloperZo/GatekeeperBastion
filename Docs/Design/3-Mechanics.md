# 3 — Mechanics

**Mechanics** in the MDA sense: the rules, the actions available, and the structures the
game is built out of. Leverage #4 who may change the rules, #5 rules, #10 stocks and
physical structure.

This is the only layer a designer can touch directly, and the layer whose effects on the
player are least direct. The values these rules currently Run at are `4-Data.md`; what they
produce at Run time is `2-Dynamics.md`.

**Countable facts are not restated here** (`../README.md`, *Generated, or written*). Stats,
costs, Wave composition and key bindings are `../Play/Field-Manual.md`, generated from the
code.

---

## 3.1 The rules (#5)

The highest-leverage work in this project has been rule work. `provoke-on-damage`
(`EnemyGatekeeper.OnDamaged`) is roughly 15 lines and is the largest behaviour change the game
has had; no tuning pass has come close.

| Rule | Where |
|---|---|
| Death is the only thing that ends a Run early. | The Run ends early on host down or Hatch destroyed: `NotifyPlayerDown` / `ShipSystems.HatchDestroyed` both funnel through one `EndRun` → `Lost`, `PodEjections++`. A client Boarder at 0 HP does not call that path (`endsTheRunOnDeath` false on the avatar, `M49`). Solo is unchanged. |
| Clearing the last Wave is the only other ending. | → `Escaped`, `CleanExits++`. Both endings exit by one door: RETURN TO SHIP. |
| Shooting a Default makes it come for you. | `EnemyGatekeeper.OnDamaged`. |
| A Hunter can never hurt the door, at any tuning. | `hatchDamage = 0`, `ignoresHatch`. |
| A Ram hunts the Boarder on a field-terminal Wave; shooting it still will not shout. | `RamGatekeeper.MayTargetPlayer` when `WaveMode.Terminal`; `CanBeProvoked` stays false. |
| All three kinds share one swing clock. | `GatekeeperAttackInterval`. A kind differs by what it hits and how hard, never by cadence. |
| Gatekeepers arrive in groups with a gap, not as a stream. | `WaveSpawner(+.Groups)`. |
| Kills pay, and only in `Tethered + Assault`. | `RunSim.AwardKill` → Unbanked. Prep pays nothing. |
| Unbanked converts only through a Hatch-bank. | `TriggerDraw(WaveMode.HatchBank)`; drip/dump → `BankedSalvage`. Down/breach wipes leftover. |
| Repair spends banked Salvage as healing is applied. | `RunSim.RepairHatch`. Already spent if you die afterward. |
| A Run is `WaveDirector.MaxWaves` Waves and `TriggerDraw` refuses one more. | `Sim/Waves/WaveDirector.cs`. |
| Crit has exactly one source. | `MetaUpgrades.CritChance` × `PlayerGun.CritDamageMultiplier`. A second source would multiply against the first and the dps curve would stop being readable from the Armory panel. |
| Two effects never multiply the same currency. | The power-budget rule behind the 5-skill design, section 3.2. |

**These are boundaries, not laws.** Every one exists to protect fun and player expression;
breaking one deliberately is allowed and breaking one by accident is not. The discipline
that separates the two is `../Working-Rules.md`, *Design rules are boundaries, not laws*.

---

## 3.2 Who may change the rules (#4)

The player's authority over their own Run. This is the surface the design expands when it
wants to give the player more to express, and it is the tier to reach for before reaching
for a number.

| Lever | Window | Why that window |
|---|---|---|
| The siphon dial | Prep only. `SetSiphonLevel` is a logged no-op during Assault. | The draw size is a pre-commitment. Choosing the stake after seeing the Wave is not a choice. |
| The weapon and where its wheel points | Ship only. `ShipCommands.BuyWheelUpgrade`; Bastion code never calls `EquipWeapon`. | The weapon is the build; a Run is played with the one you brought. |
| Attributes | Ship only. | |
| The equipment slot | Ship only. `ShipCommands.BuyEquipmentSlot()` unlocks the slot for `EquipmentCatalog.SlotPrice = 300`, then `ShipCommands.BuyEquipment(EquipmentId)` fills it. | Repairing needs the rig owned *and* carried (`GameController.ApplyMetaEffects` configures `RepairInteractable` to 0 HP/s when it is not), so how much door the Boarder can buy back is settled before embarking. Purchasable in-Run it would turn a failing Hatch into a wallet check. |
| Start Wave | NAV MAP, before embarking. | A start of N delivers Waves 1..N back to back. |

**What the player may never change:** whether the Run can be left, when a Wave arrives, or
what a Gatekeeper kind refuses to do. Those are section 3.1 and they are not negotiable from inside a
Run.

**The dial is the oldest instance.** It is a hand-authored table
(`Sim/Meta/TetherSiphon.cs`), not a formula — do not simplify it into one. `WaveDirector`
sizes Waves off the same table the reward comes from, so payout and body count cannot drift
apart.

**The upgrade wheel is the other one, and it is built** (`Sim/Meta/WeaponWheel.cs`; M21,
`../Milestones/3. completed/M21-the-upgrade-wheel.md`). One purchase centred on an upgrade spoke
pays `FocusAward = 5` to the centre, `NeighbourAward = 3` to each neighbour, and nothing to the
two spokes at ring distance 2: `5 + 3 + 3 = 11 = PointsPerPurchase`, landing on three spokes of
five. `MaxPurchases = WeaponTier.MaxTier = 10`, so a weapon's whole life is `10 × 11 = 110` points,
and `DerivedTier = TotalPoints / 11` is a readout of what was bought rather than the thing
bought.

Because the far two are paid nothing, adjacency on a weapon's wheel is that weapon's identity,
and the ring order is where the design decision sits. `RingOf` authors BR-12 as
`Reload, Clip, Damage, Ability, FireRate`, MR-35 as `Reload, Damage, Clip, Ability, FireRate`,
and PRI-C as `Clip, Damage, Reload, Ability, FireRate` (Clip/Reload swapped vs MR-35 so rings stay unique).
Both declare strength Damage (`TryIdentity`); BR-12's weakness is Reload and MR-35/PRI-C's is FireRate,
each at ring distance 2 from Damage, so no single purchase can pay a weapon's strength and its
weakness. Centring Damage feeds Clip and Ability on the BR-12 and Reload and Clip on the MR-35 —
one choice, two guns. Centring Damage on PRI-C feeds Reload and Clip.

Centring an upgrade focus is a commitment that also feeds its neighbours. The player cannot buy
one attribute alone, and the 3 that lands either side is the price of the 5. That is the
authorship this lever hands over.

**Unbuilt inside this lever:** the MK-1 and SEC-D have no authored ring — `RingOf` returns null and
`HasRing` is false, so those Secondaries cannot be focused at all. Rings exist for the BR-12, the
MR-35 and PRI-C. M21 lists the MK-1 ring as open on purpose, to be authored before a Secondary wheel.

**Code-side authority, so a rule has one owner:** `ShipCommands` is the only path by which
station UI mutates `MetaState`; `GameController` command methods are the only path to
`RunSim` mutators; UI is read-only everywhere (`../Architecture.md`).

### Designed, not built — the 5-skill system

The remaining expansion of this tier, and the last unbuilt surface for player expression,
which is the top-ranked aesthetic (`1-Experience.md`). No milestone owns it; this section is
the record until one does. It moved here from `../Decisions.md` on 2026-08-08 — it is a design,
not a decision.

Two equipped slots. Each skill has three parts:

- **Primary** — active only in slot 1.
- **Secondary** — active only in slot 2.
- **Passive Link** — always on in either slot; both equipped passives apply.

Rules: no same-skill-in-both-slots; **each passive must touch a unique stat**, so two equipped
passives can never stack on the same number.

The five skills:

| Skill | Domain |
|---|---|
| Leech | health |
| Reclaimer | ammo / reload |
| Marksman | distance / pierce |
| Breacher | AoE / double |
| Bulwark | defense |

**Power budget**: primary +20%, secondary +10%, passive +8%. Effects are priced by expected
value, and **two effects must never multiply the same currency** — that is how one build
silently becomes dominant. It is the same rule that keeps Luck the only crit source in the game
(`MetaUpgrades.CritChance`, `PlayerGun.CritDamageMultiplier`): a second crit source would
multiply against the first and the dps curve would stop being readable from the Armory panel.

**Research finding, recorded so it isn't assumed away:** Remnant 2 has **no mechanical
dual-archetype combination** — pairing two archetypes only generates a cosmetic combined name.
The reference does not do what this design does, so the Primary / Secondary / Passive Link
structure is **beyond its reference and is not pre-validated by it**. *Its balance is unknown;
needs play data.*

---

## 3.3 Structure — the map, the stocks, the budget (#10)

### Geometry beats stats here

The floor is 30 × 52 m. The Hatch is at z −20.5; the three spawn points sit at z 28 (two) and
z 9 (`Editor/Scenes/SceneBuilder.cs`, `BuildSpawnPoints`). The deep pair is **48.5 m of z from
the door, about 15 seconds at the 3.2 m/s patrol speed** (`WaveDirector.GatekeeperPatrolSpeed`).

Gatekeeper stats barely register against that. This is the clearest case in the project of
structure beating constants, and it is the first thing to check when a Wave feels wrong: a
change to where bodies enter moves more than a change to what they are.

**Still in no model:** travel time across the Bastion for the *player*, aiming accuracy,
and repair downtime.

### The Hatch's gatekeeper-second budget

The door has a budget and it is measured, not predicted. One parked Default deals
`4 / 0.7 = 5.71` dps at a **60% landing rate**, so 3.43 effective.

```
budget (gatekeeper-seconds) = hatchHP / 3.43     300 -> 88    345 -> 101    375 -> 109
spend  (gatekeeper-seconds) = SUM over arrivals of seconds spent standing at the door
```

**Mean live is a different quantity.** Gatekeeper-seconds are time spent *at the Hatch*.
Mean live is time spent *in the room* — the time-weighted average of how many Gatekeepers
were alive during a Wave segment, reconstructed offline by `Tools/wave-report` from spawn
and kill lines (`1-Experience.md`, section 1.3). `mean live = arrivals/sec × mean lifetime`.
Do not substitute one for the other; a Wave can read high mean live with a full Hatch if
nothing parks, and the reverse.

The third Wave-2 attempt in `2-Dynamics.md` spent roughly **220 gatekeeper-seconds against a
budget of 101.**

**Landing rate is the most load-bearing number in the design and the least replicated.**
Measured twice on 2026-08-03 — 34.2 HP/s and 35.1 HP/s with ~10 parked, against a theoretical
57.1 — and once at ~33% on the pre-fix build. *Unknown beyond those samples; needs more play
data.*

**Where the budget went — two correct changes that compound.** The horde retune tripled bodies
and cut Hatch damage to a third: break-even only if landing holds. The swing-landing fix then
took landing ~33% → ~60%. Together `3 × (1/3) × 1.8 = 1.82×` the pre-horde Hatch pressure,
against a Hatch that grew 300 → 345 (`1.15×`). The door is **1.58× weaker relative to its
Wave** than before either change.

**Hatch Armor is a buffer and buffers do not fix this.** One level is
`+15 HP = +4.4 gatekeeper-seconds` against a ~120 gatekeeper-second deficit.

### The stocks

| Stock | Filled by | Drained by |
|---|---|---|
| Hatch HP | `RunSim.RepairHatch`, paid from banked Salvage | gatekeeper dwell × landing rate |
| Unbanked Salvage | `AwardKill` | Hatch-bank drip/dump, attrition, wipe on down/breach |
| Banked Salvage | Hatch-bank drip/dump | repair, withdraw, Ship purchases |
| Player HP | CREW QUARTERS Vitality, between Runs only | gatekeeper swings |
| Stamina | regen after a 1s delay | sprint |
| Clip | reload | firing |

**Banked Salvage is the only stock that survives a Run.** Unbanked does not. Sizes are `4-Data.md`.

---

## 3.4 The map — scenes and stations

`Splash → MainMenu → Ship → Bastion → Ship`. Scene names are exact, live in
`Game/Flow/SceneFlow.cs`, and all four are generated by `Editor/Scenes/SceneBuilder.cs` and
registered in build settings in that order. Hand-editing a scene loses the edit on the next
rebuild.

- **Splash** — title card; any key or a 2.5s timeout advances.
- **MainMenu** — CONTINUE / START NEW (with overwrite confirm) / OPTIONS / QUIT.
- **Ship** — walkable hub, four station consoles, one per alcove. No Run sim here. ARMORY
  west, FABRICATION east, CREW QUARTERS south, NAV MAP north
  (`Editor/Scenes/SceneBuilder.Ship.cs`). `MapStation.StationName` is `NAV MAP`, and the
  floating console label the builder writes is `NAV MAP` too
  (`Editor/Scenes/SceneBuilder.Ship.cs`, the `pal.ConsoleMap` console). The class name is
  still a known debt (`../_meta/_definitions/language.md`, trap 2).
- **Bastion** — the fight, in-fiction `ALPHA-7`. `GameController.Start()` calls `StartRun()`
  then `EngageTether()`, so you are fighting on arrival. `Docked` is a transient boot phase,
  not a screen.

While `Phase == Tethered` the sim alternates two sub-states (`Sim/Run/AssaultState.cs`):
**Prep**, quiet, no Gatekeepers and no income, the only window the dial moves in; and **Assault**,
a live draw where kills pay and the dial is locked. `WaveSpawner` reports the Wave dead via
`RunSim.NotifyAssaultCleared()` when spawning has finished and the alive count hits zero.

Save file: `Application.persistentDataPath/gatekeeper_meta.json`.

---

What these rules produce in play: `2-Dynamics.md`. What they are for: `1-Experience.md`.
The values: `4-Data.md`. Where the code lives: `../Architecture.md`.
