# M14 — Five Waves, and you choose how deep to start

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-05
-->

Extend the bastion from three Waves to five, and let the player begin a Run at any
Wave they have previously reached — where starting deep means **every Wave up to that
one arrives, back to back**, not a skip.

## Done when

**Met.** `MaxWaves = 5`; NAV MAP start-Wave `1..DeepestWaveReached`; chained delivery with
`InterWaveGap = 3s`; end screen `DEEPEST WAVE N`. AutoVerify EditMode **203 / 0**,
PlayMode **10 / 0**, nine `DeepStartTests` (`gkb_verify_report.json`,
2026-08-04T17:22:51Z).

## What shipped

- `WaveDirector` — five Waves; `HunterPerWave` / `RamPerWave` `{0,4,6,10,12}` /
  `{0,0,2,4,7}`; `MaxGroupSize = 7`; `InterWaveGap = 3f`.
- `MetaState` / `SaveData` — `DeepestWaveReached` (absent/0 → 1); rises on clear only.
- `RunSim.StartRun(startWave)` — first assault chains Waves 1..N on delivery-complete;
  `NotifyDeliveryComplete` + Tick-driven gap.
- `WaveSpawner` — concurrent per-Wave emit structs; aggregate `MaxEnemyCount`; clear only
  when `!ChainPending`.
- `MapStation` — start-Wave selector; `GameSession.PendingStartWave`.
- End screens — `DEEPEST WAVE N` on Lost and Escaped.
- `[Run] run begins … starting at wave N`.
- `DeepStartTests` + five-Wave pins in Siphon / Gatekeeper / Run suites.

## What it did not settle

Play criteria 7–9 (logged ~85 s deep-start span, on-screen deepest unlock, IFF
crossover at Wave 3) were not closed with a gate log at ship — re-check in play if
chaining reads wrong or the crossover sits elsewhere; the knob is `InterWaveGap`.
Whether Waves 4–5 are beatable in practice remains a later balance question (M16).

## Story

As a player, I want to start a Run as deep as I have earned, so that reaching Wave
five is a fight I can practise rather than a prologue I re-run every time.

## Why it matters

Two problems, one answer.

**Three Waves is not a Run, it is an opening.** The count curve, the health ramp and
the composition tables all extend cleanly; what is missing is the back half where
Rams stop being a novelty and become the fight.

**And a five-Wave run is unplayable to iterate on.** Reaching Wave 5 to learn
anything about Wave 5 costs ~150 s of delivery first (`Docs/Design/2-Dynamics.md`). Deep
start is what makes the added content reachable — which is why these ship together
rather than as two milestones.

The deep start is **not a skip, and that is the whole design.** Choosing Wave 4 does
not hand you Wave 4; it sends Waves 1 through 4 at you in sequence with a short gap
between them. Same bodies, same Salvage, far less recovery time — a condensed run.

### The intent that governs its tuning

**Overlapping Waves must be easier than sequential ones if and only if the player has
bought enough to clear the Wave.** That conditional is the point: the deep start is
the payoff for investment, not a shortcut around it, and it should punish anyone who
takes it early.

It is not automatic — it falls out of two effects pulling opposite ways, and the
tuning job is to keep the crossover somewhere a player can reach.

**Chaining removes Prep, which is where repair happens.** The logs show ~12 s of
repair plus ~5 s to re-draw between Waves; chaining trades all of that for the 3 s
gap. Starting at Wave 3 is therefore ~28 s shorter and one full Hatch repair poorer.
Fewer total seconds means fewer gatekeeper-seconds against the door
(`../../Design/2-Dynamics.md`) — **provided the room does not fill up.**

**Whether it fills up is entirely the player's kill rate.** Survivors carried into
the next Wave are `bodies - killRate x deliverySeconds`:

| kill rate | after w1 | after w2 | after w3 | after w4 |
|---|---|---|---|---|
| 0.50/s | 7.5 | 16.0 | 26.8 | 39.6 |
| **0.93/s** *(measured, mid-tier)* | 0.0 | 4.0 | 13.7 | 27.2 |
| 1.40/s | 0.0 | 0.0 | 0.0 | 13.7 |

At 0.93/s Wave 1 is already dead before Wave 2 lands, so a start at Wave 2 costs a
repair and saves 14 s — close to neutral, tipping positive with one more Tier. At
0.50/s every Wave hands its leftovers to the next and the fight compounds. **That is
the IFF, and the inter-Wave gap is the knob that moves where it sits.**

Leverage tier (`../../Working-Rules.md`): **#10 for the curve extension** — the
count, health and composition tables are structure, not magnitudes — and **#5 for the
deep start**, which adds a decision at the NAV MAP that did not exist.

## Acceptance criteria

1. `WaveDirector.MaxWaves == 5`, and `NextWave` returns the table below for Wave
   indices 0–4 at dial 0.
2. The NAV MAP console offers a **start Wave of 1..`MetaState.DeepestWaveReached`**, and
   refuses any higher value.
3. `DeepestWaveReached` starts at 1, rises only when a Wave is **cleared**, persists
   across runs, and is never lowered.
4. Starting at Wave N delivers Waves 1..N **in order, back to back**, with a **3 s
   gap** between the end of one Wave's delivery and the start of the next — not
   simultaneously, and with no Prep or draw between them.
5. A Run started at Wave N pays the same per-kill Salvage as reaching Wave N normally
   — the deep start grants no bonus and takes no penalty.
6. Clearing Wave 5 from any start ends the Run `Escaped` with `BASTION STRIPPED`.
7. A logged Run started at Wave 3 shows Waves 1, 2 and 3 delivered inside **~85 s**
   with no player input between them.
8. **A new deepest Wave is announced where the player is standing.** On the Run that
   first clears Wave N, the end screen reads `DEEPEST WAVE N`, and the NAV MAP offers N on
   the next visit. Checked by playing to a new deepest Wave and reading the screen, not
   by opening the save file.
9. **The conditional holds at Wave 3.** Four logged Runs — start-at-3 versus
   reach-3-normally, each at a T0 Primary and at a maxed one — where Hatch HP at the
   end of Wave 3 shows deep start **worse** at T0 and **better** at T3. If both
   loadouts land the same way, the inter-Wave gap is the value to move, and this
   criterion is what says which direction.

## Implementation

### 1. The curve (`Assets/Scripts/Sim/Waves/WaveDirector.cs`)

**Change** `MaxWaves` 3 → 5. Every existing formula already extends; only the two
composition tables need new entries.

| Wave | bodies | D | H | R | hp x | pool | group | gap | delivery | in/s |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 18 | 18 | 0 | 0 | 1.00 | 540 | 3 | 4.20 | 21.0 s | 0.86 |
| 2 | 30 | 26 | 4 | 0 | 1.40 | 1204 | 4 | 4.00 | 28.0 s | 1.07 |
| 3 | 42 | 34 | 6 | 2 | 1.80 | 2484 | 5 | 3.80 | 30.4 s | 1.38 |
| 4 | 54 | 40 | 10 | 4 | 2.20 | 4136 | 6 | 3.60 | 28.8 s | 1.88 |
| 5 | 66 | 47 | 12 | 7 | 2.60 | 6474 | 7 | 3.40 | 30.6 s | 2.16 |

HP multipliers follow `EnemyHealthPerWave = 0.40` (M16). The 0.25 column originally
authored here is superseded — retune magnitude against M16 criterion 2, not this table.

**Change** `HunterPerWave` `{0,4,6}` → `{0,4,6,10,12}` and `RamPerWave`
`{0,0,2}` → `{0,0,2,4,7}`.

Waves 4 and 5 introduce no new kind — there isn't one — so they escalate by
**composition**: Wave 4 is where Hunters stop being a garnish (10 of 54), Wave 5 is
seven Rams at 240 HP each, which is the fight that finally rewards holding the Hatch
instead of pushing out. *That ratio is a design call, not a derived number* — the
alternative was flat scaling of all three kinds, rejected because it makes Waves 4–5
read as "Wave 3 again, more of it."

Full run: **210 bodies, ~14,838 HP, 5,250 Salvage at dial 0** (HP pool after M16 slope).

**Change** `Docs/Design/4-Data.md` — the Wave table, the composition tables and the
count grid all gain two rows.

### 2. Deepest Wave reached

**Change** `MetaState.cs` / `SaveData.cs` — add `int DeepestWaveReached = 1`. Written
by `RunSim.NotifyAssaultCleared` via the existing meta reference, never by the Game
layer. Migration: absent in an old save → 1.

It rises on **clear**, not on reach, so a Wave the player was killed by does not
become a start option.

**Change** `GameController.EndScreen` — one line, `DEEPEST WAVE N`, shown on both end
screens (criterion 8). A save field the player never sees is an unlock they never learn
they earned; the end screen is where they already are when it happens, so this costs a
label rather than a notification system.

### 3. The deep start

`InterWaveGap = 3f` is a named constant in `WaveDirector`, not a literal. It is the
only tuning knob for criterion 9: raising it pushes the crossover toward weaker
loadouts, lowering it toward stronger ones.

**Change** `Sim/Run/RunSim.cs` — `StartRun` / `EngageTether` take a start Wave.
Internally the Run begins with `CurrentWaveIndex = 0` regardless; what changes is
that on `TriggerDraw`, if the chosen start Wave is > 1, the sim enters a **chained
delivery**: Wave 1 spawns, and when its *delivery* finishes (not when it is cleared)
a 3 s timer starts the next, up to the chosen Wave. Prep and the draw are skipped
between chained Waves.

The distinction that matters: chaining is on **delivery complete**, not
**Wave cleared**. Chaining on clear would just be the normal loop with the pauses
removed, and the point is that the Waves overlap in the room.

After the chosen Wave, the Run continues normally — Prep, draw, next Wave — through
Wave 5.

**Change** `Game/Enemies/WaveSpawner.cs` — it currently assumes one live `WaveSpec`.
It needs to run **several concurrently**, each with its own group cursor and timer,
and report the Wave dead only when *all* chained Waves are dead. This is the real work
of the milestone and the thing most likely to be got wrong; `WaveSpawner.Groups.cs`
already isolates group emission, so the cursor becomes a small per-Wave struct in a
list rather than fields on the component.

**Change** `Game/Ship/MapStation.cs` — a start-Wave selector, 1..`DeepestWaveReached`,
defaulting to 1. It is the second decision the NAV MAP has ever offered; the first is
which bastion, which does not exist yet.

**Change** the `[Run] run begins` log line — include the start Wave. A log that does
not say where the Run began cannot be compared to one that started at 1.

### 4. Tests

**Change** `Assets/Tests/EditMode/Waves/SiphonAndWaveTests.cs` — every test asserting
a 3-Wave run, the count grid, and `MaxWaves`.

**Create** `Assets/Tests/EditMode/Run/DeepStartTests.cs` — criteria 2–5: the
`DeepestWaveReached` clamp, that it rises only on clear, chained delivery order and
the 3 s gap, and that per-kill value is unchanged by start Wave.

## Supporting work

- **Driven by** the NAV MAP console's start-Wave selector — here (`MapStation.cs`). It is
  clicked, not typed, so it needs no binding of its own.
- **Shown by** the selector itself for the choice, and the end-screen `DEEPEST WAVE N`
  line for the unlock — both here. **The NAV MAP's entry in the control table is M15's**;
  if M15 slips, the selector is still usable because it is on-screen at the station, so
  this milestone does not block on it. Chaining is legible from the Wave counter
  advancing with no Prep between Waves; if playtesting says it reads as a bug rather
  than a design, the fix is a strip line, which is M15's surface.
- **Reads back as** the `[Run] run begins` line naming the start Wave, the `wave N/5`
  HUD counter, and the end screen.

## Verification

1. Criteria 1–6 — `Tools/verify/run.sh`.
2. Criterion 7 — play a Run started at Wave 3 (dev mode's `F3` reaches the state; the
   **gate run must be clean**, started from the NAV MAP). From the log: three
   `wave N/5 —` lines with no `pulled the siphon draw` between them, first to last
   spanning ~85 s.
3. Criteria 3, 8 — playthrough path: from Play, start a Run, clear Wave 2, die on
   Wave 3. The end screen must read `DEEPEST WAVE 2`; back on the ship, the NAV MAP must
   offer 1–2 and not 3.

## Open questions

- **Where the crossover sits** is *unknown, needs play data*. Criterion 9 tests it at
  Wave 3 only, because that is the deepest Wave with any play history. Whether the
  conditional also holds at 4 and 5 is a later question, and the answer may be no —
  see Watch.
- **Whether Waves 4 and 5 are beatable in practice** is deliberately not a criterion
  here. The arithmetic now says yes — VK-7 T6 clears Wave 4 and T7 clears Wave 5
  (`../../Design/2-Dynamics.md`) — but arithmetic said things about Wave 2 that four logged
  runs falsified. This milestone ships the content and the access; the balance gate is
  a later one, and pretending otherwise is how M2's first version got written.

## Watch

- **The conditional's crossover depends on the shop ceiling, which M11 raised to meet
  this curve.** At three Tiers the ceiling was 82 theoretical dps and Waves 4–5 needed
  142 and 205, so a deep start into them stacked survivors regardless of investment and
  the IFF could not hold. M11's seven-Tier ramp reaches 223 on the VK-7, which clears
  Wave 5. **Re-derive criterion 9's crossover against the shipped ceiling, not the old
  one** — if this file and `../../Design/4-Data.md` disagree, the numbers file wins.
- The dial multiplies body count and its surplus is all Default
  (`Sim/Waves/WaveDirector.cs`). At Wave 5, dial 5, that is 99 bodies. `MaxEnemyCount`
  is 120 and still holds, but check it rather than assuming.
- Chained Waves must respect `MaxEnemyCount` **in aggregate**, or a Wave-5 deep start
  at dial 5 spawns several hundred agents and the NavMesh becomes the bottleneck
  rather than the fight.
- `RunSim` stays pure C# (`../../Architecture.md`). The chain timer ticks on
  `Tick(dt)` like everything else.

## Depends on

M10, which sets the group gaps this table extends. Sequencing after M13 is worth it —
verifying criterion 7 without a Wave jump means playing to Wave 3 by hand every
attempt.
