<!-- Purpose: the moment-to-moment loop a Boarding is made of — where a wave begins, what ends it, and what each ending is worth. The one place the loop is stated; every other document defers to this. / Status: locked / Update trigger: when the start line, the end condition, or what a zone completion pays changes. / Last updated: 2026-09-01 (M110 constant-drift pass: Hatch HP 600, Wave-5 roster 39, four Thieves) -->

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
| 1. Start | The Hatch | `T` | Draws the current zone's **opening** waves — every wave the zone authors but the last, back to back, no Prep between them. |
| 2. Push | The lane | movement + fire | Most of the wave breaches at the FAR closet, so the fight travels toward the terminal with you. Once the authored bodies are spent, a **trickle** keeps the lane honest until the terminal is pressed. |
| 3. Call | The zone's terminal | `T` | Draws the zone's **last** wave into whatever is still standing (M115, 2026-09-02). |
| 4. End | The same terminal | `T` | Once the last wave is down the terminal arms: shuts the wave off, completes the zone, banks what you carry, opens the zone's door. |
| 5. Repeat | The Hatch | `T` | The next zone's trigger. Master Control is the last one — and it is a zone like the others: kill the elite arms its switch. |

### The terminal calls the last wave (2026-09-02)

Three of the four 2026-09-02 Boardings shut T2 within 9–32 s of the Hatch press with three to
six of wave 1's eleven dead and wave 2 never drawn, because the shutoff was legal once the
zone's *first* wave had drawn. The walk stopped being the pressure the moment a sprint could
end it. So the zone's last wave now belongs to its terminal: `FieldShutoff.MayCallFinalWave`
offers the call once the opening span has drawn, `RunSim.CallZoneFinalWave` draws it into the
live field — the opening's survivors, or the drip — and `FieldShutoff.MayRun` refuses the
shutoff until that wave has been drawn and is down (`RunSim.FieldShutoffArmed`, raised by the
spawner's `ArmShutoff` verdict). The Hatch is still the only start line; the terminal only
finishes what the Hatch began. `M115`. The opening and the last wave draw different shapes
(`M116`, `WaveShapes`): a zone opens on a Mass and its terminal calls a Pack (zone 1) or a
Convoy (zone 2), and the prompt names the shape before `T` — `4-Data.md`, "Waves and intensity".

### Master Control obeys the rule too (2026-08-28)

The finale used to break the loop in three places at once, and all three are gone:

- **It started at its own terminal.** That was a SECOND start line, the exact shape this
  document forbids, and the reason a run could begin anywhere but the Hatch. `T` at the Master
  terminal now refuses and says the finale starts at the Hatch.
- **Interacting with it sealed the control room.** The last beat became a box fight — the one
  place in the Bastion where the lane stopped mattering.
- **It ended at the Hatch**, in a Convert window, so the game's final wave was the only one that
  did not end at a terminal.

Now: start at the Hatch once D3 is open (`ControlRoomReachable`), fight the control room, and
**the finale elite is the gate**. Killing it does not end the run — it ARMS the Master terminal,
and throwing that switch is what ends the Boarding (`RunSim.ShutOffMasterControl`). A finale
cannot end by attrition any more than a zone can. The HUD states which half of the gate you are
in. Since 2026-09-03 the control room is two Waves at the Wave-5 size, Mass then Pack, a 12 s
quiet beat, then the elite behind an escort of four Hunters and two Cloakers that has to fall
before the elite takes damage — staged beats with a readable dip, not five of the same Wave.

**And the switch pays.** `ShutOffFieldWave` has always banked the carry at a zone terminal —
"reaching the switch IS the payout" — but removing the finale's Convert window quietly removed
its payout with it, leaving the Master switch as the one switch in the game that CONFISCATED a
stake, from the player who had just won. It banks now, like every other switch.

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
* **The shutoff is NOT legal before the zone's waves have been drawn** — all of them, the last
  one by the terminal's own press (2026-09-02). Walking up a quiet lane and pressing `T` must
  not skip the zone, and neither may a sprint past the first wave — see
  `FieldShutoff.MayRun` and `MayCallFinalWave`.
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
player, unconditionally and for life, from anywhere on the spine. A Wave 5 field wave is 39 of
them (`11 + 7*waveIndex`; it was 66 on the roster of the day). A logged run died to that six
times and paid 84% of its gross back in respawn fees.

The invulnerable half made the Thief meaningless as a side effect: its only weapon is
`hatchDamage`, so on a field wave it could affect nothing whatsoever. 12 of 13 walked a whole
run untouched, each one a full magazine's worth of nothing.

Both are now the same rule on both modes: the Hatch is a target, and it takes damage. **This
adds a second way to lose a field wave** (`cause="hatch"`) while the player is 66-156 m away —
deliberately. It is what makes walking away from the Hatch a decision instead of a formality,
and it gives the corridor a direction: the pressure is no longer only in front of you.

It does NOT change what completes a zone. Killing the field still does not; the terminal still
does. The Hatch is a thing you can lose, never a thing you can win at.

### The way home, and the two bugs that hid it (2026-08-28)

**The way home is a portal you can see, and it stands at the Hatch.** `HatchPortal` is a pad
beside the Hatch that stands dark until a zone closes and lights when it does; a held Interact
on it ends the Boarding Docked. It owns no rule — `RunSim.DockOffered` decides whether leaving
is legal and `RunSim.DockRun` performs it, the same two the Hatch's own prompt reads, so the two
surfaces cannot disagree. The Hatch prompt stays, for a player who walks to the door rather than
the pad. The visual is the Ship's wake capsule (Pad, Emitter, Glass) copied into the Bastion.

**T2 and T5 recall to the Hatch so the next zone can start there.** A zone ENDS at its terminal,
and the next wave still BEGINS at the Hatch. Those pads keep Pad/Emitter/Glass and carry
`HatchRecallPad` (2026-08-30): held Interact teleports to the Hatch (`TeleportLocalToHatch`)
while Tethered, prompt "Recall — Hatch", lit for the whole Boarding. The Master terminal has
none — its switch ends the Run, and docking is refused for the whole finale.

**Closing a zone offers the Dock.** The Hatch's "[E] Dock — return to Ship" prompt used to open
only after a surviving Hatch-bank window (`OfferDockIfBankSurvived`), and Convert was struck from
the demo on 2026-08-25 — so `DockOffered` could never become true and there was no way to end a
Boarding on purpose. Dying, abandoning, or the whole spine to Master Control were the only exits.
Completing a zone is the honest trigger: the carry has just banked, the door has just opened, and
the choice is genuinely press on or go home. Starting the next zone takes the offer back.

**This is the second time removing Convert orphaned something it did not own** — the first was
the door graph, where `RecordBankSurvived` was the only door-opening path. When a beat is cut,
search for what was reading it, not just for what called it.

**The field-wave door stake was turned on 2026-09-04**, from 0.15 to 0.29, the first time
that dial has moved. It had been solved against a door of half the current size and never
followed `ShipSystems.HatchMaxHp` when that doubled, so a leak of one Thief and three
Defaults had 87 s against a 45 s target. The derivation
and the table are in `4-Data.md`; `M112` adjudicates it across five untainted runs, of which
one has been played.

**The drip and the shutoff must agree, or wave 5 is inescapable.** Two rules disagreed and the
disagreement was self-sustaining: the drip started when the ROSTER was spent, while the flag that
makes dripped bodies free only rose when the FIELD WAS EMPTY. The drip kept a body alive, so the
field never emptied, so the flag never rose — every dripped body billed full salvage and the wave
could never clear. Meanwhile `T` at the terminal, the documented escape, was unreachable during
Assault because `TickFightInput` returned before calling the branch that handles it. Zone 1 hid
both: killing the field empty drops the sim to Prep, so the shutoff was always pressed from Prep
there. The 07:17 run authored 39 bodies in wave 5, killed 93, and had to be abandoned.

**The Hatch is shut while you are up the lane, and armoured for it**
(`HatchBank.FieldWaveDamageScale`). This is not a softening of the rule above; without it the
rule is unplayable. Hatch HP is 600 (`ShipSystems.HatchMaxHp`) and was sized for a mode where
the player stands on it. A single Thief with three Defaults is 45.7 raw dps and would take the
Hatch down in **13.1 s** — the 66 m walk back from the near terminal, near enough exactly — and
Wave 5 authors four Thieves. Scaled, that same group gives about **88 s**: twice the margin the
door bought when this was written, and comfortably the round trip to T2 plus a fight through.
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

1. **`[T] start the wave**` while `GameController.HatchOffersWaveStart` is true: the current
   zone still has a terminal (`HatchBankStartBlocked`), or Master Control is waiting and D3 is
   open (`MasterControlAwaitsStart && ControlRoomReachable`). This is checked FIRST — ahead of
   Convert and Withdraw — because a shutoff banks everything, so the player who returns to start
   the next wave arrives with nothing unbanked and the Convert branch would otherwise swallow the
   prompt.
2. **Convert / Withdraw**, only once there is no zone left to start.

**A field terminal never starts a wave.** `CanSelectTerminal` refuses one while
`HatchOffersWaveStart`, which covers the local look-at and `TryNetStartTerminal` (the co-op
entry) together. `T` at Master Control refuses and says the finale starts at the Hatch.

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
- [ ] Closing a zone offers the Dock, so the Hatch is a way home without a Convert window.
- [ ] The Hatch dock portal is dark before a zone closes and lit after, and ends the run on a hold.
- [ ] T2 and T5 recall to the Hatch while Tethered; Master has no pad.
- [ ] The Hatch starts the finale only once the control room is reachable.
- [ ] `T` at a zone terminal ends the wave in **Assault**, not only in Prep.
- [ ] The drip never outlives the flag that makes its bodies free.
- [ ] A field wave can target and damage the Hatch, and losing it ends the run.
- [ ] No body is forced onto the player merely because the Hatch is unavailable.
- [ ] Every prompt and demo line points at the verb that actually works.

The last one has its own history: no gate out-argues the voice telling the player where to go.
Two start lines and a demo line pointing at the wrong one is what sent the 2026-08-25 run to the
terminal to *begin* a wave.

## Where this is enforced

| Rule | Code | Test |
|---|---|---|
| The Hatch starts; the terminal refuses | `GameController.HatchOffersWaveStart`, `CanSelectTerminal`, `ControlRoomReachable` | `HatchStartLinePlayModeTests.PlainEmbark_TheHatchStarts_AndTheZoneTerminalRefusesTo` |
| T2/T5 recall; dock at the Hatch | `HatchRecallPad`, `HatchPortal`, `RunSim.DockOffered` | `RecallPadSceneTests`, `HatchPortalSceneTests` |
| Clearing the field does not complete the zone | `GameController.RecordCompositeClear` | `HatchStartLinePlayModeTests.KillingEveryBody_LeavesTheZoneOpen_UntilTheTerminal` |
| The shutoff completes, banks and opens the door | `ShutOffTerminal`, `RunSim.ShutOffFieldWave`, `OpenDoorsFor` | `HatchStartLinePlayModeTests.TheWaveStartedAtTheHatch_IsShutOffAtTheTerminal_AndBanks` |
| A Convert window has no off switch | `RunSim.ShutOffFieldWave` | `HatchBankTests.ShutOffFieldWave_RefusesAHatchBankWindow_AndKeepsTheStake` |
| The lane drips until the shutoff, and pays nothing for it | `FieldTrickle`, `WaveSpawner.CheckAssaultCleared` / `TickFieldTrickle` | `FieldTrickleTests` |

The spine's shape — which rooms, which spawn weights, how far — is
`Docs/Demo - Single/README.md`. The numbers are `4-Data.md`. This file owns only the loop.
