<!-- Purpose: the moment-to-moment loop a Boarding is made of — where a wave begins, what ends it, and what each ending is worth. The one place the loop is stated; every other document defers to this. / Status: locked / Update trigger: when the start line, the end condition, or what a zone completion pays changes. / Last updated: 2026-08-26 (the loop enforced after the 07-04 playthrough) -->

# 0 — The Gameplay Loop

This document exists because the loop kept drifting. It was decided on 2026-08-25, described in
three places, and implemented in none of them completely — and the 2026-08-26 playthrough walked
straight through the gap. **When this document and the code disagree, the code is wrong.**

## The loop, in one line

> **A wave BEGINS at the Hatch and ENDS at a terminal.**

That is the whole rule. Everything below is what it costs to mean it.

## The four beats

| Beat | Where | Verb | What it does |
|---|---|---|---|
| 1. Start | The Hatch | `T` | Draws the current zone's whole trigger — every wave the zone authors, back to back, no Prep between them. |
| 2. Push | The lane | movement + fire | Most of the wave breaches at the FAR closet, so the fight travels toward the terminal with you. Once the authored bodies are spent, a **trickle** keeps the lane honest. |
| 3. End | The zone's terminal | `T` | Shuts the wave off, completes the zone, banks what you carry, opens the zone's door. |
| 4. Repeat | The Hatch | `T` | The next zone's trigger. Master Control is the last one. |

## What ends a wave — the rule that was missing

**Killing every body does NOT complete a zone.** It ends the *fight*; it does not end the *run
up the lane*. The zone is complete when, and only when, the player reaches its terminal and
presses `T`.

This is the half that was wrong until 2026-08-26. `RecordCompositeClear` completed every
terminal in the zone the moment the field emptied, so the objective was satisfied by attrition
and the walk never had to happen. The 07:04 playthrough is the proof: the player held the Hatch,
killed all 48 bodies of zone 1's composite, watched `zone complete zone=1` fire from 16 m away,
then walked up to T2 and was refused three times at a prop with nothing left to do.

**Why it matters more than it looks.** The spine is a 227 m corridor built for one purpose: to
make the walk the pressure. If surviving where you stand finishes the zone, the corridor is
scenery and the game is the old single-room hold with a longer view. Every other decision on the
spine — the far-weighted spawns, the enemy speeds set under the player's walk, the one-lane
geometry — is priced on the assumption that the player has somewhere to be.

So:

* **Clearing the field does not return the Run to Prep** while the zone still owes a shutoff.
  The Assault stays live and the lane keeps dripping (below), because the wave is not over —
  only its roster is. The terminal is still lit and still the objective.
* **The shutoff is legal in Prep as well as Assault** all the same. The drip can be off (a zone
  the table does not author, a build with the trickle disabled, a Run that ended the wave some
  other way), and a player who arrives to a quiet lane must still be able to finish. Refusing
  outside Assault is what left the playthrough pressing a dead button.
* **The shutoff is NOT legal before the zone's waves have been drawn.** Walking up a quiet lane
  and pressing `T` must not skip the zone — see `GameController.ZoneWavesHaveRun`.
* **A player may run a zone's waves more than once** before closing it. More salvage, more risk,
  same door at the end. That is a choice, not an exploit.

### The trickle (Zo, 2026-08-26)

Killing every authored body used to leave an empty corridor between the player and the button.
The objective was positional and the pressure was not, so the walk stopped mattering the instant
the last body fell — the same hole that made completion-by-attrition wrong, one step further on.

**So the field keeps dripping until the terminal is shut off.** `FieldTrickle` owns the cadence:
one body every 3 s, capped at 6 alive. The spine's terminals sit 66 m and 156 m from the Hatch,
so an unhurried walk meets roughly 4 and 10 bodies — the corridor has to be fought through, and
it is nothing like a second wave. Sprinting meets fewer, which is the right incentive: the drip
rewards moving and only punishes standing still. The cap is the safety rail — a player who stops
faces a **standing** cost, never a growing one.

### What you leave behind (Zo, 2026-08-27)

**The Hatch is in the fight on a field wave, and it can be lost while you are up the lane.**
Until 2026-08-27 a field wave removed the Hatch from targeting entirely and made it
invulnerable. Both halves were wrong, and the targeting half was much the larger bug: the
"is there a hatch for me" term feeds the `_chasingPlayer` line in `EnemyGatekeeper.Retarget`,
so deleting the Hatch did not give bodies one fewer option — it forced **every** body onto the
player, unconditionally and for life, from anywhere on the spine. A Wave 5 field wave is 66 of
them. A logged run died to that six times and paid 84% of its gross back in respawn fees.

The invulnerable half made the Thief meaningless as a side effect: its only weapon is
`hatchDamage`, so on a field wave it could affect nothing whatsoever. 12 of 13 walked a whole
run untouched, each one a full magazine's worth of nothing.

Both are now the same rule on both modes: the Hatch is a target, and it takes damage. **This
adds a second way to lose a field wave** (`cause="hatch"`) while the player is 66-156 m away —
deliberately. It is what makes walking away from the Hatch a decision instead of a formality,
and it gives the corridor a direction: the pressure is no longer only in front of you.

It does NOT change what completes a zone. Killing the field still does not; the terminal still
does. The Hatch is a thing you can lose, never a thing you can win at.

**The Hatch is shut while you are up the lane, and armoured for it**
(`HatchBank.FieldWaveDamageScale`). This is not a softening of the rule above; without it the
rule is unplayable. Hatch HP is 300 and was sized for a mode where the player stands on it. A
single Thief with three Defaults is 45.7 raw dps and would take the Hatch down in **6.6 s** —
less time than it takes to turn around at the near terminal — and Wave 5 authors seven Thieves.
Scaled, that same group gives about 45 s, which is the round trip to T2 plus a fight through.
**This number is arithmetic, not a measurement**: no field wave has ever been played with the
Hatch in the fight, so it is the first dial to turn after one is.

**Trickle bodies pay nothing.** Not salvage, and not experience once experience exists.
`RunSim.CurrentKillAward` returns 0 while `FieldTrickleLive` is set, so every payout path is
covered at its source. They are a clock, not an income: paying for them would turn "walk up the
lane" into "farm the lane", and let a player who never advances out-earn one who does.

Two ordering rules that are easy to get wrong, and were:

* The drip flag rises only **after the authored field is empty**. Raising it while authored
  bodies still live would quietly stop paying for the wave the player is still fighting.
* A fresh wave clears the drip. No drip outlives the Run — or the zone — that owned it.

## What a zone completion pays

All of it lands on the shutoff, because the shutoff is the achievement:

1. **The zone's terminals complete** — `GameController.ShutOffTerminal` is the only path.
2. **Everything carried banks**, at face value (`RunSim.ShutOffFieldWave` → `DumpUnbanked`).
   Kills already bank as they land, so this only matters when the player deliberately
   Withdrew — but the walk must never be a second toll on money already earned.
3. **The zone's door opens** — `GameController.OpenDoorsFor("shutoff")`. Doors used to open only
   on a survived Convert window, and when the Convert left the demo that made the spine
   impassable end to end: D1 could never open. One helper serves both causes now.

## What the Hatch is for

The Hatch is the start line and the bank, and it must never be ambiguous which one it is
offering. In Prep, in priority order:

1. **`[T] start the wave**` while any zone still has a terminal to run
   (`GameController.HatchOffersWaveStart`). This is checked FIRST — ahead of Convert and
   Withdraw — because a shutoff banks everything, so the player who walks back to start the
   next wave arrives with nothing unbanked and the Convert branch would otherwise swallow the
   prompt.
2. **Convert / Withdraw**, only once there is no zone left to start.

**A field terminal never starts a wave.** `CanSelectTerminal` refuses one while the Hatch is the
start line, which covers the local look-at and `TryNetStartTerminal` (the co-op entry) together.
Master Control is the exception: the control room is its own start line, and the one terminal
the Hatch cannot answer for.

## The invariants, as a checklist

A build honours the loop when all of these are true:

- [ ] `T` at the Hatch starts the zone's whole trigger, on a first run and on a returning save.
- [ ] `T` at a field terminal never starts a wave — local path and net path alike.
- [ ] Killing every body leaves the zone **incomplete**, and the lane keeps dripping.
- [ ] Trickle bodies pay 0; authored bodies pay in full right up to the last one.
- [ ] `T` at the zone's terminal completes it, in Assault and in Prep.
- [ ] `T` at a terminal whose zone has not been drawn is refused.
- [ ] Completing a zone opens its door, banks the carry, and says so.
- [ ] The Hatch's start prompt outranks Convert and Withdraw.
- [ ] A field wave can target and damage the Hatch, and losing it ends the run.
- [ ] No body is forced onto the player merely because the Hatch is unavailable.
- [ ] Every prompt and demo line points at the verb that actually works.

The last one has its own history: no gate out-argues the voice telling the player where to go.
Two start lines and a demo line pointing at the wrong one is what sent the 2026-08-25 run to the
terminal to *begin* a wave.

## Where this is enforced

| Rule | Code | Test |
|---|---|---|
| The Hatch starts; the terminal refuses | `GameController.HatchOffersWaveStart`, `CanSelectTerminal` | `HatchStartLinePlayModeTests.PlainEmbark_TheHatchStarts_AndTheZoneTerminalRefusesTo` |
| Clearing the field does not complete the zone | `GameController.RecordCompositeClear` | `HatchStartLinePlayModeTests.KillingEveryBody_LeavesTheZoneOpen_UntilTheTerminal` |
| The shutoff completes, banks and opens the door | `ShutOffTerminal`, `RunSim.ShutOffFieldWave`, `OpenDoorsFor` | `HatchStartLinePlayModeTests.TheWaveStartedAtTheHatch_IsShutOffAtTheTerminal_AndBanks` |
| A Convert window has no off switch | `RunSim.ShutOffFieldWave` | `HatchBankTests.ShutOffFieldWave_RefusesAHatchBankWindow_AndKeepsTheStake` |
| The lane drips until the shutoff, and pays nothing for it | `FieldTrickle`, `WaveSpawner.CheckAssaultCleared` / `TickFieldTrickle` | `FieldTrickleTests` |

The spine's shape — which rooms, which spawn weights, how far — is
`Docs/Demo - Single/README.md`. The numbers are `4-Data.md`. This file owns only the loop.
