# M18 — A boarder you must leave the hatch for

<!--
Status: later
Update trigger: when the Siege's stats, its wave counts, or its stand-off range change
Last updated: 2026-08-04
-->

A fourth boarder kind that shells the hatch from stand-off range and never closes.
Killing it requires walking away from the door.

## Story

As a player, I want a threat I cannot answer from where I am standing, so that holding
the hatch becomes a choice with a cost rather than the only correct position.

## Why it matters

Waves 4 and 5 currently escalate by composition alone — more Hunters, more Rams —
because there is no fourth kind to introduce (`WaveDirector.HunterPerWave` /
`RamPerWave`). Every existing threat resolves at the door: Defaults and Rams walk to it,
Hunters walk to the player, and standing at the hatch answers all three.

The Siege refuses to close. Ignoring one is affordable; ignoring three is not. At
3 x 4.0 dps it strips **346 HP** over wave 4's 28.8 s delivery, against a 300 HP hatch.

Leverage tier (`Docs/04_Agent_Rules.md`): **#5, rules.** It adds a decision — leave or
absorb — rather than scaling an existing number. #12 was available and rejected: raising
Ram counts makes the same fight heavier, not different.

## Acceptance criteria

1. `BoarderKind` gains `Siege = 3`, and `StatsFor(Siege)` returns the stat line below.
2. A Siege **never moves within `SiegeStandOff` of the hatch** and **never targets the
   player**, at any range, including when shot.
3. A Siege damages the hatch on its own interval while stationary at stand-off range,
   with no line-of-sight requirement.
4. Waves 1–3 contain zero Siege; waves 4 and 5 contain 3 and 5. Counts are fixed per
   wave and the siphon dial's surplus remains entirely Default.
5. Three Siege ignored for the whole of wave 4 deal **300–400 HP** to the hatch —
   enough that ignoring them loses the door, not so much that one unanswered shot does.
6. A logged wave-4 run shows at least one `[Boarders] siege opened fire` line and, if
   the player travels to them, `killed a Siege`.

## Implementation

### Sim

**Change** `Sim/Waves/BoarderKind.cs` — add `Siege = 3`. **Never renumber the existing
three**; the values are in save-adjacent logs and prefab wiring.

**Change** `Sim/Waves/WaveDirector.cs`:

| | Default | Hunter | Ram | **Siege** |
|---|---|---|---|---|
| maxHp | 30 | 20 | 120 | **60** |
| playerDamage | 6 | 6 | 0 | **0** |
| hatchDamage | 4 | 0 | 20 | **12** |
| attackInterval | 0.7 s | 0.7 s | 0.7 s | **3.0 s** |
| patrol / chase | 3.2 / 5.6 | 6.5 / 6.5 | 2.2 / 2.2 | **2.8 / 2.8** |
| refuses | — | the hatch | the player | **the player, and closing** |

The Siege is the first kind to break the shared 0.7 s swing clock. That clock existed so
kinds differed by what they hit rather than by cadence; a ranged shell is a different
action from a melee swing, and 3.0 s is what makes the shot readable at 35 m. Record the
exception in `Docs/02_Numbers.md` beside the table.

Add `SiegeStandOff = 35f` and `SiegeHatchDamage = 12f`, `SiegeAttackInterval = 3.0f` as
named constants — no literals.

Add `SiegePerWave = { 0, 0, 0, 3, 5 }` alongside the Hunter and Ram tables.
`defaultCount` absorbs the remainder, unchanged.

`BoarderStats` gains `standOffRange` (0 = closes to melee, as all three existing kinds
do). This is the field that expresses the refusal; a Siege with `standOffRange = 0` must
behave exactly like a Default.

### Game

**Create** `Assets/Scripts/Game/Enemies/SiegeBoarder.cs` — subclass of `EnemyBoarder`,
matching `HunterBoarder` / `RamBoarder`. Overrides `MayTargetPlayer => false`,
`CanBeProvoked => false`, and adds the stand-off: navigate toward
`HatchStation.ClosestSurfacePoint` but stop at `standOffRange`, then fire on the
interval.

**Push stats at spawn** via `EnemyBoarder.Configure` (`Docs/03_Architecture.md`,
gotcha 1). No damage number on the prefab.

**Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs` — generate
`SiegeBoarder.prefab`. Colour: pale green `(0.35, 0.70, 0.45)`, scale 0.95, distinct
from Default red, Hunter amber and Ram violet-steel. Re-run **Build All Scenes**;
scenes are generated (`Docs/03_Architecture.md`, gotcha 4).

**Change** `Game/Enemies/WaveSpawner.Groups.cs` — the kind shuffle takes a fourth
bucket. Counts must still sum to `enemyCount`.

**Log** one line when a Siege reaches stand-off and opens fire, and reuse the existing
kill line. Without it criterion 6 cannot be read.

### Tests

**Change** `Assets/Tests/EditMode/Waves/BoarderVariantsTests.cs` — composition sums with
four kinds at every wave and dial; `StatsFor(Siege)` exact; `SiegePerWave` zero through
wave 3.

**Create** the criterion-5 arithmetic check: `3 * (12 / 3.0) * 28.8` inside the 300–400
band, computed from the constants rather than hardcoded.

## Open questions

- **Whether a threat that cannot hurt the player is compelling to walk toward** is
  *unknown, needs play data*. The Ram has the same property and reads well, but the Ram
  comes to you. If players ignore Siege and eat the hatch damage, the lever is
  `SiegeStandOff` — closer means a cheaper trip — before touching its damage.
- **Whether stand-off pathing survives the NavMesh at 35 m** is unverified. The hatch
  stand-point work (`Docs/03_Architecture.md`, gotcha 2) exists because pathing near the
  hatch has bitten before; a stop-short target is a new case.

## Watch

- The three-kind grammar is that each kind is defined by what it refuses. The Siege
  refuses two things, which is one more than any existing kind. If it reads as arbitrary
  rather than legible in play, cut the player-damage refusal first — a Siege that can
  return fire is still a Siege.
- Do not give the dial the power to multiply Siege counts. Surplus stays Default.

## Depends on

Nothing. Interacts with M14 — waves 4 and 5 only exist there.
