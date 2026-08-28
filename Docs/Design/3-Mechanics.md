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

| Rule | Protects | Where |
|---|---|---|
| A Run ends early on a death nobody can pay for, on breach, or on a Docked return at a surviving bank window — nothing else. | Challenge (fail state); Fellowship (guest exception) | Hatch destroyed still ends it outright: `ShipSystems.HatchDestroyed` → `EndRun` → `Lost`, `PodEjections++`. A death no longer does on its own — see the respawn row below; `GameController.PlayerDied` calls `NotifyPlayerDown` only once the dead seat cannot pay and no other seat is fighting. A client Boarder at 0 HP never called that path (`endsTheRunOnDeath` false on the avatar, `M49`); the `M98` recut put host and solo on the same footing. Amended 2026-08-17 (Zo), superseding "death is the only thing that ends a Run early": a player who survives a hatch-bank window may return to the Ship — held interact at the Hatch, until the next Wave is triggered — ending the Run **Docked**, not Lost; banked Salvage and door state persist via the successful-bank snapshot (`../Decisions.md`, *Locked — return to Ship at a surviving bank*). |
| A dead seat buys its way back out of the realm's bank. | Challenge (dying still costs); Fellowship (guest, host and solo on one rule) | `Respawns.Refuse` allows it anywhere in `RunPhase.Tethered`; `RunSim.TryRespawn` debits `Respawns.CostFor(taken)` via `SpendBanked`. Re-pursed 2026-08-25 with the award path: the carried pile it used to bill no longer fills on its own, and one pot means a death competes directly with a Tier. |
| Clearing the last Wave is not the win. Master Control's Hatch-bank is. | Fantasy (boarding objective); Challenge | Wave 5 terminal clear does not `CompleteRun`. `Escaped` / `CleanExits++` only after the Master Control Hatch half (`AdvanceMasterControl`). Both endings exit by one door: RETURN TO SHIP. |
| Shooting a Default makes it come for you. | Challenge; Expression (you choose to pull) | `EnemyGatekeeper.OnDamaged`. |
| A Hunter can never hurt the door, at any tuning. | Fantasy (kind identity); Challenge | `hatchDamage = 0`, `ignoresHatch`. |
| A Thief hunts the Boarder on a field-terminal Wave; shooting it still will not shout. | Fantasy (kind identity); Challenge | `ThiefGatekeeper.MayTargetPlayer` when `WaveMode.Terminal`; `CanBeProvoked` stays false. |
| A Cloaker refuses every target while a Boarder is looking at it. | Fantasy (kind identity); Challenge | `CloakerGatekeeper.MayTargetHatch` / `MayTargetPlayer` both gate on the gaze; `CloakerWatch.Watched` is the cone-and-range half. `CanBeProvoked` deliberately does not gate, so a shot lands the grudge and it comes for you when you look away. |
| All four kinds share one swing clock. | Challenge (readable kinds) | `GatekeeperAttackInterval`. A kind differs by what it hits and how hard, never by cadence. |
| Gatekeepers arrive in groups with a gap, not as a stream. | Challenge | `WaveSpawner(+.Groups)`. |
| Kills pay, and only in `Tethered + Assault`. | Challenge; Submission refused | `RunSim.AwardKill` → `BankKill` → `MetaState.BankedSalvage`, at face value as the body falls. Prep pays nothing; a kill inside a Hatch-bank window pays nothing (`CurrentKillAward`). |
| Nothing is at risk that the player did not choose to put at risk. | Expression (the stake); Challenge | The carried pile fills only by `RunSim.Withdraw` at the Hatch (Prep, door at full HP, prompted "Withdraw — hold E"). Breach and the death that ends the Run wipe it; a Docked return keeps `HatchBank.ReturnedKeepShare`. Death can no longer reach the bank behind it (Zo, 2026-08-25). |
| Repair spends banked Salvage as healing is applied. | Expression (you bought the rig) | `RunSim.RepairHatch`. Already spent if you die afterward. |
| One terminal trigger launches its zone's authored Wave count, as one composite rather than N draws with Prep between. | Challenge | `DemoZones.WavesPerTrigger` — 1 in zone 1, 3 in zone 2; off the demo a trigger is a single draw at whatever Wave the Run has reached (`GameController.StartZoneComposite`). `WaveDirector.MaxWaves = 5` is the Run's absolute end-Wave ceiling and is not re-keyed per trigger (`Sim/Bastion/FirstTerminal.cs`). |
| Crit has exactly one source. | Expression (ARMORY stays readable) | `MetaUpgrades.CritChance` × `PlayerGun.CritDamageMultiplier`. A second source would multiply against the first and the dps curve would stop being readable from the Armory panel. |
| Two effects never multiply the same currency. | Expression | The power-budget rule behind the 5-skill design, section 3.2. |
| Every stake raises danger and reward from one source. | Expression (the stake stays honest); Challenge | Section 3.1, *Stake — the authored risk*. `NextWave`: one table for money and bodies alike. |
| Every seat's kills credit the one realm bank. | Fellowship; Expression (own loadout) | `BankKill` credits the realm, not a per-slot pile. Co-op gets you further, not richer (`../Brainstorms/co-op.md`). |

**These are boundaries, not laws.** Every one exists to protect the ranked aesthetics in
`1-Experience.md` §1.2 (stamp 2026-08-16: Fellowship, Expression, Fantasy; Challenge is
the vehicle). Restamp the Protects column when that table moves; the rules do not.
Breaking one deliberately is allowed and breaking one by accident is not. The discipline
that separates the two is `../Working-Rules.md`, *Design rules are boundaries, not laws*.

### Chance the player can model

Independent rolls that can starve a reward, a win, or a proc — or dump three in a row —
are refused (Expression: a reward the player can model). When a random event is added, it
uses one of these three (builder names;
never player-facing: `../_meta/_definitions/language.md`):

| Pattern | Means | Reset |
|---|---|---|
| **Pity timer** | After a stated count of failures, the event is guaranteed (e.g. a legendary after 50 empty pulls). | On the grant |
| **Escalating weights** | Chance rises a little on each failure. | On the trigger |
| **Pre-determination** | The sequence is locked before play — shuffle a deck at match start — so the session averages instead of clumping. | New sequence next Run / next shuffle |

**In tree today:** Wave composition is already a table (`WaveDirector.NextWave`), not a
roll. Spawn placement draws from seeded `RunSim.Rng` (`../Architecture/4-Testing-and-Determinism.md`).
Crit chance is frozen at 0; iid burst is how a dps curve stops being readable from the
ARMORY (`MetaUpgrades.CritChance`). The three-failed-Runs buy-past in `2-Dynamics.md`
section 2.5 is an **economy** guarantee, not a drop pity — do not add a second pity on
Salvage.

A proposal that adds a random drop, win, or proc names which of the three it is, in the
milestone, before the table of percentages. Observation that would kill a pick: a logged
session where the event never fired inside its pity / deck / weight cap, or where it
clumped inside one Wave after a shuffle that forbade that.

This is not a gacha shop. The gacha wording is the definition only.

### Stake — the authored risk

The game already calls this a stake in four places (`1-Experience.md` §1.1, §1.2 and the
Protects column above) without ever naming it as one category. **A stake is any mechanism
where the player pays to raise the danger and the payout together.** It is the formal name
for what gets called juicing, and it is the meta-rule the economy hangs off — the economy
is which currency each stake is paid in and what it buys.

| Stake | Paid in | Raises | Where |
|---|---|---|---|
| Intensity juice | Banked Salvage, at a look-at trigger | bodies and per-kill value | `IntensityJuice`, `TetherSiphon`, `WaveDirector.NextWave` |
| Withdraw depth | risk, by choosing to carry banked salvage | what a Convert can convert and what a wipe costs | `RunSim.Withdraw`, `HatchBank.CarryConvert` |
| Run multipliers | *proposed* | wave attributes and yield on that axis | `../Brainstorms/economy.md` |
| Roster authorship | *proposed* | which kinds exist, and what each is worth | `../Brainstorms/economy.md` |

**The rule: a stake raises danger and reward from one source.** `NextWave` enforces this
locally today — *"ONE table for money and bodies alike: TetherSiphon owns the curve, this
reads it"* — and the generalisation is that no stake may have a payout side authored apart
from its danger side. A stake with two tables can be tuned into pure upside by accident,
which is the one failure this category cannot survive.

Packing reward onto the Gatekeeper rather than onto the Wave satisfies this by
construction: a Wave's payout is whatever is standing in it, so there is no second table to
drift. That is the preferred form for any new stake.

**Stakes may stack** (Zo, 2026-08-20). Salvage is already multiplied by the dial and by
carry depth, and a third is accepted. This relaxes *Two effects never multiply the same
currency* for the stake category specifically: the rule stays a boundary — it means the
stack is watched and tuned, not that a stack is a defect. Every stake added to the table
above is a factor someone has to hold in their head.

**Not a stake:** pay-to-skip. Buying past a fight rather than into a harder one is cut
(`../Decisions.md`, *pay-to-juice … not pay-to-skip*).

---

## 3.2 Who may change the rules (#4)

The player's authority over their own Run. This is the surface the design expands when it
wants Expression (loadout, juice), Fantasy (named protocol), or Fellowship (party lead,
Ready / EMBARK). Reach here before reaching for a number.

| Lever | Window | Protects | Why that window |
|---|---|---|---|
| Intensity juice | Prep only, looking at a trigger. `IntensityJuice.Try` spends **banked** Salvage to raise (re-pursed 2026-08-25); lower is free, no refund. `SetSiphonLevel` remains for tests. `T` starts. Locked in Assault. | Expression; Challenge | The draw size is a pre-commitment, and it now costs take-home progress rather than money that was never safe. Choosing the stake after seeing the Wave is not a choice. |
| The weapon and where its wheel points | Ship only. `ShipCommands.BuyWheelUpgrade`; Bastion code never calls `EquipWeapon`. | Expression | The weapon is the build; a Run is played with the one you brought. |
| Boarder Protocol / Passive | Ship only. `CrewQuartersStation` / `ShipCommands.PickPassive`. Default sheet owned from first session. | Expression; Fantasy (named identity) | A Run is played with the sheet you brought. G/H fire in Assault (`PlayerProtocolAbilities`). |
| Hatch-bank | Prep, looking at the Hatch, with a carried pile above 0; locked until the zone's terminals are complete (Zo, 2026-08-17). `GameController.StartHatchBankWave`. | Expression; Fantasy (extract); Challenge | Convert is a fight, not a shop action, and since 2026-08-25 it is **out of the demo's critical path**: with kills banking on landing the pile is 0 unless the player deliberately withdrew, so the window cannot start unasked. It survives because Master Control's finale runs on it. No per-Run cap: the slot count was deleted (`RunSim.TriggerDraw`). |
| Return to Ship | After surviving a Hatch-bank window, until the next Wave is triggered; held interact at the Hatch. Ends the Run **Docked**, not Lost (Zo, 2026-08-17). | Challenge (risk is mitigated at a window, not at will); Expression (banked stays) | Banked Salvage and door state persist via the successful-bank snapshot. One window, not a bail-out lever. Parked intent, not spec: when the juice/multiplier mechanic lands, leaving early loses or reduces the salvage multiplier. |
| Party Ready / EMBARK | Ship, friends session. Host is party lead (`NetPartyLobby`). | Fellowship | Closed (`M72`, 2026-08-17). Solo Continue never starts a session. |
| The equipment slot | Ship only. `ShipCommands.BuyEquipmentSlot()` unlocks the slot for `EquipmentCatalog.SlotPrice = 300`, then `ShipCommands.BuyEquipment(EquipmentId)` fills it. | Expression | Repairing needs the rig owned *and* carried (`GameController.ApplyMetaEffects` configures `RepairInteractable` to 0 HP/s when it is not), so how much door the Boarder can buy back is settled before embarking. Purchasable in-Run it would turn a failing Hatch into a wallet check. |
| Start Wave | NAV MAP, before embarking. Party lead's pick in a friends session. | Fellowship; Challenge | A start of N delivers Waves 1..N back to back. |

**What the player may never change:** when a Wave arrives, or what a Gatekeeper kind
refuses to do. Those are section 3.1 (Challenge fail state, Fantasy kind identity) and they
are not negotiable from inside a Run. Whether the Run can be left moved off this list on
2026-08-17 (Zo) — at exactly the one window above; there is still no leave-anytime lever.

**Intensity is the oldest instance.** The body-count table is still
(`Sim/Meta/TetherSiphon.cs`), not a formula — do not simplify it into one. The player
authors the stake by juicing **banked** Salvage at a look-at trigger (`Sim/Run/IntensityJuice.cs`).
`WaveDirector` sizes Waves off the same table the reward comes from, so payout and body count cannot drift
apart.

**The upgrade wheel is the other one, and it is built** (`Sim/Meta/WeaponWheel.cs`; M21,
`../Milestones/3. completed/M21-the-upgrade-wheel.md`). One purchase centred on an upgrade spoke
pays `FocusAward = 5` to the centre, `NeighbourAward = 3` to each neighbour, and nothing to the
two spokes at ring distance 2: `5 + 3 + 3 = 11 = PointsPerPurchase`, landing on three spokes of
five. `MaxPurchases = WeaponTier.MaxTier = 10`, so a weapon's whole life is `10 × 11 = 110` points,
and `DerivedTier = TotalPoints / 11` is a readout of what was bought rather than the thing
bought. The salvage cost of a purchase is `WeaponTier.Cost(slot, centres on that spoke)`:
stacking Damage raises the next Damage buy, not Reload. Neighbour +3 does not raise the
neighbour's next price. Ten purchases still cap the weapon.

Because the far two are paid nothing, adjacency on a weapon's wheel is that weapon's identity,
and the ring order is where the design decision sits. `RingOf` authors BR-12 as
`Reload, Clip, Damage, Ability, FireRate`, MR-35 as `Reload, Damage, Clip, Ability, FireRate`,
PRI-C as `Clip, Damage, Reload, Ability, FireRate` (Clip/Reload swapped vs MR-35 so rings stay unique),
MK-1 as `Damage, Ability, Reload, Clip, FireRate` (strength Reload, weakness Damage), and
SEC-D as `FireRate, Ability, Reload, Clip, Damage` (strength FireRate, weakness Reload).
BR-12, MR-35 and PRI-C declare strength Damage (`TryIdentity`); BR-12's weakness is Reload and MR-35/PRI-C's is FireRate,
each at ring distance 2 from Damage, so no single purchase can pay a weapon's strength and its
weakness. Centring Damage feeds Clip and Ability on the BR-12 and Reload and Clip on the MR-35 —
one choice, two guns. Centring Damage on PRI-C feeds Reload and Clip.

Centring an upgrade focus is a commitment that also feeds its neighbours. The player cannot buy
one attribute alone, and the 3 that lands either side is the price of the 5. That is the
authorship this lever hands over.

SEC-D's Ability spoke is on the ring so uniqueness and award arithmetic stay the same five
spokes, but `WeaponWheel.SpokePurchasable` refuses a centred Ability purchase while the
catalog ability is `AbilityId.None`. Neighbour awards onto Ability still apply.

**Code-side authority, so a rule has one owner:** `ShipCommands` is the only path by which
station UI mutates `MetaState`; `GameController` command methods are the only path to
`RunSim` mutators; UI is read-only everywhere (`../Architecture.md`).

### Designed, not built — the 5-skill system

Dated 2026-08-15: Boarder Protocols occupy CREW QUARTERS; this 5-skill design stays parked in `../Brainstorms/crew-skills-and-perks.md` and is not what CREW sells.

The remaining expansion of this tier, and a parked surface for player expression
(rank 2, `1-Experience.md` §1.2). No milestone owns it; this section is
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
silently becomes dominant. It is the same rule that keeps crit chance a single source
(`MetaUpgrades.CritChance`, `PlayerGun.CritDamageMultiplier`): a second source would
multiply against the first and the dps curve would stop being readable from the Armory panel.
Chance is frozen at 0; there is no Luck shop.

**Research finding, recorded so it isn't assumed away:** Remnant 2 has **no mechanical
dual-archetype combination** — pairing two archetypes only generates a cosmetic combined name.
The reference does not do what this design does, so the Primary / Secondary / Passive Link
structure is **beyond its reference and is not pre-validated by it**. *Its balance is unknown;
needs play data.*

---

## 3.3 Structure — the map, the stocks, the budget (#10)

### Geometry beats stats here

The fight is kit rooms (M61-2 scaler-2 station volumes, Control Room last). Graybox
`BuildSpawnPoints` coords (30 × 52 m, Hatch z −20.5, deep pair 48.5 m) are retired as the
live floor. Sketch Hatch dock is `z = -61` (`Sim/Bastion`); player travel time across the
dressed Bastion is **unknown — needs measurement**.

Gatekeeper stats barely register against that. This is the clearest case in the project of
structure beating constants, and it is the first thing to check when a Wave feels wrong: a
change to where bodies enter moves more than a change to what they are. Dressed kit rooms
are also the Fantasy surface of the fight (`M61-2`).

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
| Carried Salvage | `RunSim.Withdraw` only | Hatch-bank drip/dump, hatch-hit attrition, wipe on the death that ends the Run and on breach |
| Banked Salvage | `AwardKill` → `BankKill`, as each body falls; Hatch-bank drip/dump | repair, juice spend, respawn fee (`Respawns.CostFor`), withdraw, Ship purchases |
| Player HP | run start / bind; pool is frozen (`MetaUpgrades.PlayerMaxHp`) | gatekeeper swings |
| Stamina | regen after a 1s delay; pool is frozen | sprint |
| Clip | reload | firing |

**Banked Salvage is the only stock that survives a Run, and it is now where a kill lands.**
A carried pile does not survive, which is exactly why nothing fills it but a deliberate
Withdraw. Sizes are `4-Data.md`.

---

## 3.4 The map — scenes and stations

`Splash → MainMenu → RealmSelect → Ship - CoOp → Bastion_Coop → Ship - CoOp`. Scene names are
exact, live in `Game/Flow/SceneFlow.cs`, and are registered in build settings in that order
with `Range` last. Splash, MainMenu, stations, Bastion kit and a dressed Ship (`ShipDress`)
are authored scene content. `SceneBuilder` registers build settings and never touches hosts
or UI (`Architecture.md` gotcha 4). A `ShipDress` root skips the Ship Environment wipe;
without it, collider graybox is still generated. Kit geometry persists.

- **Splash** — title card; any key or a 2.5s timeout advances.
- **MainMenu** — CONTINUE / START NEW (with overwrite confirm) / HOST / JOIN / OPTIONS / QUIT.
  Host/Join start a friends session (`NetSession` DDOL) before the hub. Solo Continue never does.
- **RealmSelect** — the realm chooser. START NEW opens it; CONTINUE re-enters the realm played
  last. A session is a Boarder paired with a realm, and this is where that pair is chosen.
- **Ship - CoOp** — walkable hub, four station consoles, one per alcove. No Run sim here.
  Named for the party it can hold, and solo play lives here too — the old `Ship - Demo` hub
  retired with the scene-pair consolidation. ARMORY
  west, FABRICATION east, CREW QUARTERS south, NAV MAP north
  (`Editor/Scenes/SceneBuilder.Ship.cs`). Friends session: shared Ship, party lead owns
  EMBARK (`M72` closed 2026-08-17). `MapStation.StationName` is `NAV MAP`, and the
  floating console label the builder writes is `NAV MAP` too
  (`Editor/Scenes/SceneBuilder.Ship.cs`, the `pal.ConsoleMap` console). The class name is
  still a known debt (`../_meta/_definitions/language.md`, trap 2).
- **Bastion_Coop** — the fight, in-fiction `ALPHA-7`. `GameController.Start()` calls `StartRun()`
  then `EngageTether()`, so you are fighting on arrival. `Docked` is a transient boot phase,
  not a screen. Groups instantiate inside a hung spawn closet and walk onto the fight floor;
  the origin is not on the playable floor.
- **Range** — the practice range, Bastion-shaped but never drawing a Wave. In the build
  settings and reachable only by opening `Range.unity` directly; nothing in the hub leads
  there yet.

While `Phase == Tethered` the sim alternates two sub-states (`Sim/Run/AssaultState.cs`):
**Prep**, quiet, no Gatekeepers and no income, the only window intensity juice moves in; and **Assault**,
a live draw where kills pay and intensity is locked. `WaveSpawner` reports the Wave dead via
`RunSim.NotifyAssaultCleared()` when spawning has finished and the alive count hits zero.

Save files: `Application.persistentDataPath/gkb_save/`, split into `boarders/` and `realms/`
at schema 13, one `.json` per document (`Game/Flow/SaveService.cs`). The pre-split
`gatekeeper_meta.json` is never read or written and is deliberately left on disk.

---

What these rules produce in play: `2-Dynamics.md`. What they are for: `1-Experience.md`.
The values: `4-Data.md`. Where the code lives: `../Architecture.md`.
