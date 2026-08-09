# 01 — The Game

Sci-fi wave-defense FPS. Unity 6, PC/Steam, first-person, gray-box art. Solo playable
now; 2–4 player co-op designed for and not built.

**Read this file top to bottom. Its sections descend Meadows' leverage ladder** — #1
paradigm at the top, #6 information flows at the bottom, and the sections are ordered so
that a change proposed low in the file has to be argued against everything above it.
Loops, delays and structure (#7–#10) are `02_Systems.md`. Buffers and constants (#11–#12)
are `Tuning.md`, which is deliberately last and deliberately unnumbered.

**This is the builder's view.** The game as a player meets it is `Play/The-Game.md`; every
key, weapon stat, wave and cost is `Play/Field-Manual.md`, generated from the code.
**Countable facts are not restated here** (`README.md`, *Generated, or written*) — this file
had four of them go stale at once.

---

## 1. The paradigm — what the game believes (#1)

Four things the code enforces. They were decided separately and they agree, which is what
makes them a paradigm rather than four features.

| Belief | Enforced by | Cut to get there |
|---|---|---|
| You cannot leave. | `EndRun` reached only by death or by clearing the last wave. `EscapeSpool` / `RunPhase.Spooling` dormant. | Leave-anytime tether (`05_Decisions.md`). |
| The clock pays nothing. | `RunSim.AwardKill` credits on death of a boarder; `RunSim.Tick` only advances a clock. | Per-second siphon income; `BaseSalvagePerSecond` removed. |
| Nothing starts until you start it. | No wave timer. `WaveSpec.timeUntilNextWave` always 0. A wave begins because the player pressed T (`RunSim.TriggerDraw`). | The wave timer. |
| Nothing is taken from you. | No ending moves salvage. `SalvageLedger` is a display tally. | Loss-stake "musical chairs" rotor. |

Together: **the player sets both the pace and the stake, and the game supplies neither.**
Pressure in this game is authored by the person under it. That is what "the draw is the
bell" names — `TriggerDraw` is the only thing in the run that starts a clock.

**What the game refuses to be**, each refusal paid for by a cut already made:

- Not an idle game. Time never pays; standing in Prep earns exactly zero.
- Not a game with an exit. There is no bail-out lever and no in-run respawn.
- Not a game that confiscates. Dying and clearing differ only in which counter ticks
  and what the end screen says.
- Not a game that rushes you. The one thing it will never do is start without you.

**Stated paradigm — not yet written by Zo.** The four rows above are the paradigm the
shipped code *demonstrates*, derived POSIWID-style from what it enforces. It is not the
same artifact as a stated intent, and where the two disagree the disagreement is the most
valuable thing in this file. This section should carry both.

---

## 2. What a good run is — the optimisation target (#2)

Everything below this line in the doc set is tuned toward some answer to this question,
and **the project's current answer is not measurable.** That is the standing #2 problem
and it outranks every open tuning question.

**Stated intent, on the economy** (`02_Systems.md`, *The pacing target*): a player who
dies repeatedly on a wave can buy past it within **three failed runs**. This is a
constraint, not a goal — it says what must not happen, not what a good run is.

**Stated intent, on the curve** (`Milestones/3. completed/M16`): hatch remaining at each
wave clear, wave 1 ~100%, wave 2 ~85%, wave 3 ~65%, all ±15; waves 4–5 provisional.

**Why that instrument does not answer the question.** Hatch remaining measures leakage,
not effort. A player who kills everything before it arrives reads 100% whether the wave
was trivial or whether they were at their limit — the metric saturates, and it saturates
hardest for exactly the players best able to say how the game feels. On 2026-08-07 a
five-wave clear at MR-35 T4 read 300/300 on every wave, and the player reported the
session as fun and semi-challenging. Both facts are true and the number carries neither.

**So the open question is about the instrument, not the game.** Something tracking effort
rather than leakage — time at full attention, distance travelled, how close the player came
to being overrun — would grade a curve. *Unknown what the right measure is; needs a
decision, not more arithmetic.*

**Until it is decided, the player's report outranks the instrumentation** (`GatkeeperZo
Notes/playthrough.md`). Metrics measure outputs; they do not measure the experience, and a
verdict drawn from them without a play report is a verdict about the log file.

---

## 3. What each actor wants (#3)

The subsystem goals. **Each boarder kind is defined by what it refuses to do, and each
refusal is a rule in code, not a tuning value** — so no tuning pass can soften one by
accident.

| Actor | Wants | Refuses | Enforced by |
|---|---|---|---|
| Default | the hatch; diverts to you when provoked | — | `EnemyBoarder.OnDamaged` |
| Hunter | you, only | the hatch, at any tuning | `hatchDamage = 0`, `ignoresHatch` |
| Ram | the hatch, only | you — and being turned around by being shot | `playerDamage = 0`, `ignoresPlayer` |

All three share one swing clock (`BoarderAttackInterval`). **A kind differs by what it
hits and how hard, never by cadence.** Boarders arrive in groups with a gap between them,
not as a stream.

The player's incentive is split across four stations, and the split is the design:

| Station | What it is for | Gated |
|---|---|---|
| ARMORY | the weapon in your hands — weapons and per-weapon Tiers | Tiers locked until the first death (`MetaUpgrades.UpgradesUnlocked`); buying a weapon is not |
| CREW QUARTERS | the player — Vitality, Stamina, Engineering, Scavenging, Luck | never, by design: attributes are the answer to dying, so they must be buyable before you have died |
| FABRICATION | the ship — hatch armor, repair rig | never |
| MAP | where you go and how deep you start | never |

"Engineering" means two things and the split is deliberate: FABRICATION's Repair Rig is
the rig's base HP/s, CREW QUARTERS' Engineering is your speed multiplier on it. They
multiply. `EngineeringStation.cs` keeps the old class name on purpose; every string the
player reads says FABRICATION (`_meta/_definitions/language.md`).

---

## 4. Who may change the rules (#4)

The player's rulemaking authority over their own run. This is the surface the design
expands when it wants to give the player more to express, and it is the tier to reach for
before reaching for a number.

**What the player may change, and when:**

| Lever | Window | Why the window |
|---|---|---|
| The siphon dial | Prep only. `SetSiphonLevel` is a logged no-op during Assault. | The draw size is a pre-commitment. Choosing the stake after seeing the wave is not a choice. |
| The weapon and its Tier | Ship only. | The weapon is the build; a run is played with the one you brought. |
| Attributes | Ship only. | |
| Start wave | MAP, before embarking. | A start of N delivers waves 1..N back to back. |

**What the player may never change:** whether the run can be left, when a wave arrives, or
what a boarder kind refuses to do. Those are section 5, and they are not negotiable from
inside a run.

**The dial is the clearest instance.** It is a hand-authored table (`Sim/Meta/TetherSiphon.cs`),
not a formula — do not simplify it into one. Its only cost is more boarders, all of them
Default; HP, group size and spawn gap ignore it. `WaveDirector` sizes waves off the same
table the reward comes from, so payout and body count cannot drift apart. They did once,
and the result was +600% enemy HP for +50% money.

**Code-side authority, so a rule has one owner:** `ShipCommands` is the only path by which
station UI mutates `MetaState`; `GameController` command methods are the only path to
`RunSim` mutators; UI is read-only everywhere (`03_Architecture.md`).

**Designed, not built — the expansion of this tier.** The 5-skill system
(`05_Decisions.md`) and the weapon focus ring (`Brainstorms/armory-and-weapons.md`) both
sit here: they hand the player authorship over which rules their run runs under. Neither
has graduated; the brainstorm is the record until a milestone moves it.

---

## 5. The rules — what is enforced (#5)

The highest-leverage work in this project has been rule work. `provoke-on-damage`
(`EnemyBoarder.OnDamaged`) is roughly 15 lines and is the largest behaviour change the
game has had; no tuning pass has come close.

| Rule | Where |
|---|---|
| Death is the only thing that ends a run early. | `NotifyPlayerDown` and `ShipSystems.HatchDestroyed` both funnel through one `EndRun` → `Podded`, `PodEjections++`. |
| Clearing the last wave is the only other ending. | → `Escaped`, `CleanExits++`. Both endings exit by one door: RETURN TO SHIP. |
| Shooting a Default makes it come for you. | `EnemyBoarder.OnDamaged`. |
| Shooting a Ram does nothing to its mind. | `ignoresPlayer`. |
| Kills pay, and only in `Tethered + Assault`. | `RunSim.AwardKill`. Prep pays nothing. |
| The money is yours the instant the boarder dies. | `AwardKill` credits `MetaState.BankedSalvage` immediately. |
| Repair spends banked salvage as healing is applied. | `RunSim.RepairHatch` at `ShipSystems.RepairSalvagePerHp`. Already spent if you die afterward. |
| A run is `WaveDirector.MaxWaves` waves and `TriggerDraw` refuses one more. | `Sim/Waves/WaveDirector.cs`. |
| Crit has exactly one source. | `MetaUpgrades.CritChance` × `PlayerGun.CritDamageMultiplier`. A second source would multiply against the first and the dps curve would stop being readable from the Armory panel. |
| Two effects never multiply the same currency. | The power-budget rule behind the 5-skill design (`05_Decisions.md`). |

**These are boundaries, not laws.** Every one exists to protect fun and player expression;
breaking one deliberately is allowed and breaking one by accident is not. The discipline
that separates the two — write the reason next to the rule, a failing boundary test asks a
question rather than gives an order, a break costs a dated sentence saying what it buys —
is `04_Agent_Rules.md`, *Design rules are boundaries, not laws*.

---

## 6. What the game tells you, and what it hides (#6)

Information flow is the tier the project has most often fixed a problem with, and the one
it least often plans. A full pass tuning boarder damage 4 → 18 never reached the running
game because the prefab held the old values; what exposed it was a log line printing
`took 4 damage`. Pure #12 work, disconnected at the wire, invisible until #6 caught it.

**Three channels, and each has a rule:**

| Channel | Rule | Where |
|---|---|---|
| The HUD | absolute `GUI` rects only in the Derelict, never `GUILayout`; a layout exception strands the player on the end screen. | `03_Architecture.md` gotcha 3 |
| The log | a line when a player-visible thing happened, or when a refusal must be answerable from the log alone ("I clicked BUY and nothing happened"). Once per distinct reason, never per frame. No counters, no aggregation. | `04_Agent_Rules.md` |
| The docs | anything countable is generated from the code; anything arguable is written. | `README.md` |

**The deliberate silences are audited.** Every page in `Docs/Play/` ends with a `## Gaps`
section listing what the game never tells the player. That list is the design's #6 backlog:
a rule the wiki has to explain because the game keeps it secret is a rule the player is
playing without. Read the Gaps sections before proposing player-facing communication work.

**The builder's channel has failed the same way.** On 2026-08-04 three documents described
three different games and none was the one in the repo. Each had been correct when written.
`Tools/docgen/run.sh --check` exists because of that day and exits non-zero on drift.

---

## Orientation — scenes, stations, geometry

Physical structure (#10), kept here for navigation. Why the geometry matters more than the
stats on it is `02_Systems.md`.

`Splash → MainMenu → Ship → Derelict → Ship`. Scene names are exact, live in
`Game/Flow/SceneFlow.cs`, and all four are generated by `Editor/Scenes/SceneBuilder.cs`
and registered in build settings in that order. Hand-editing a scene loses the edit on the
next rebuild.

- **Splash** — title card; any key or a 2.5s timeout advances.
- **MainMenu** — CONTINUE / START NEW (with overwrite confirm) / OPTIONS / QUIT.
- **Ship** — walkable hub, four station consoles, one per alcove. No run sim here.
  ARMORY west, FABRICATION east, CREW QUARTERS south, MAP north
  (`Editor/SceneBuilder.Ship.cs`).
- **Derelict** — the fight, in-fiction `ALPHA-7`. `GameController.Start()` calls
  `StartRun()` then `EngageTether()`, so you are fighting on arrival. `Docked` is a
  transient boot phase, not a screen. Floor is 30 × 52 m; hatch at z −20.5, breaches at
  z 9 and z 28.

While `Phase == Tethered` the sim alternates two sub-states (`Sim/Run/AssaultState.cs`):
**Prep**, quiet, no enemies and no income, the only window the dial moves in; and
**Assault**, a live draw where kills pay and the dial is locked. `WaveSpawner` reports the
wave dead via `RunSim.NotifyAssaultCleared()` when spawning has finished and the alive
count hits zero.

Save file: `Application.persistentDataPath/gatekeeper_meta.json`.

---

Numbers and their derivations: `Tuning.md`. Loops, delays and structure: `02_Systems.md`.
Why a thing is the way it is, or was cut: `05_Decisions.md`.
