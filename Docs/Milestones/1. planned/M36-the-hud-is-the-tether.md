# M36 — The HUD is the tether

<!--
Status: later
Update trigger: when the tether readout's states change, or when a new Run state needs a HUD reading
Last updated: 2026-08-09
-->

The three numbers in three corners become one instrument shaped like the thing the game is
actually about: the line running from you to the Hatch to your Ship. Prep and Assault stop
looking the same. And a body reaching the Hatch becomes something you see rather than
something you notice afterwards.

## Story

As a player, I want to feel the Hatch behind me while I am looking somewhere else, so that
holding the door is a thing I am doing rather than a number I check.

## Why it matters

**The game's fiction is a tether and the UI does not know it.** You are the Boarder. You cut
into a Bastion someone else holds; the Hatch is your way home and the siphon runs along the
tether back to your Ship (`../../_meta/_definitions/language.md`). That is a strong, specific
premise, and it currently reads as a health bar, a second health bar, and a counter.

**Two of Zo's standing reports are the same problem seen twice.** "The UI is missing
information severely" (2026-08-08) and "no incentive to move away from the Hatch"
(`../../Design/1-Experience.md`). A player who cannot feel the Hatch from across the room has
only one safe place to stand. **This milestone does not fix the incentive — M30 does — but a
player who cannot read the door from 40 m away will not use what M30 gives them.**

**The Siphon Draw is the only thing in the game that starts a clock**
(`../../Design/1-Experience.md`, section 1.1: the player sets both the pace and the stake, and
the game supplies neither). It is the most important verb in the Run and pressing it changes
nothing on screen except a wave counter. That is the single largest gap between what this
game is and what it looks like.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** It moves no constant and
adds no rule. It changes what the player can see, which is the tier Meadows puts immediately
above rules for a reason: the same fight becomes a different decision when you can read it.

## What the Shift purchase does not cover

Read from the import, 2026-08-09: Shift ships **no HUD component of any kind** — no bar, no
readout, no crosshair. Its component list is menus and panels. **So M35 assembles the front
end from prefabs, and everything below is built.** Two Shift pieces do help and are named in
the implementation: `UIParticleRenderer` for the salvage pulse and `GradientEffect` for the
tether's fill. Motion is **DOTween Pro**, because Shift drives its own components with
Animator clips and coroutines and has no DOTween dependency to piggyback on.

## The three changes

### 1. The tether readout replaces the Hatch bar

One continuous element anchored to the bottom of the screen, read from the player's end to
the Hatch's end. It carries three things at once that are currently three separate readouts:

- **Hatch integrity** is the length and colour of the far end. Full is unbroken and `Accent`
  amber; damaged frays and reddens from the Hatch end inward. **Readable without the number.**
- **Salvage banked** pulses along the line toward the Ship on each kill, one pulse per kill,
  amplitude by value. The wager is visible as it pays.
- **Gatekeepers at the door** show as marks at the Hatch end, one per body standing there.
  Two marks and three marks look different at a glance.

The number stays, small, beside it. **The point is not to remove numbers; it is that the
shape carries the reading and the number confirms it.**

### 2. Prep and Assault are visibly different instruments

Prep is quiet: the tether is dim and slack, the siphon dial is the prominent element and it
is adjustable, and a single prompt names the one thing the player can do. Assault is live:
the dial locks and recedes, the Wave counter and Gatekeeper count come forward, the tether
tightens and lights.

**The transition is the draw**, one DOTween sequence of about 0.4 s, and it is the moment the
Run's only bell gets rung. **A single frame from either state, with all text removed, must be
identifiable as Prep or Assault** — that is criterion 4 and it is the whole design in one
sentence.

### 3. A hit on the Hatch is peripheral

When a Gatekeeper commits to the Hatch, a mark appears on the tether pointing at the spawn
point it came from, and the Hatch end pulses on each hit. The player is usually aiming
somewhere else; this has to land in peripheral vision, which means motion and position, not
text.

## Acceptance criteria

1. **Hatch integrity is readable from the tether with the numeric label hidden.** Test: hide
   the label, screenshot at 100 / 70 / 40 / 15 percent, and a reader who has not seen the
   values orders the four correctly.
2. **One pulse travels the tether per kill**, and the pulse is visible in peripheral vision
   while the crosshair is elsewhere. No pulse is emitted for salvage that is not a kill.
3. **Gatekeepers standing at the Hatch are shown as discrete marks**, and one, two and four
   are distinguishable from a single frame.
4. **Prep and Assault are identifiable from one frame with all text removed.**
5. **The draw is a transition, not a cut** — a DOTween sequence of 0.35 to 0.5 s, and pressing
   the draw during it is ignored rather than queued.
6. **A Hatch hit produces a directional mark naming the spawn point**, and the mark is present
   for at least 1.5 s so a player looking away can still catch it.
7. **Player action, in a Run:** from the Hatch bay at Wave 3, with the crosshair on a
   Gatekeeper 30 m away, the player can state Hatch integrity to within one band without
   moving the crosshair.
8. **Nothing new is counted.** The tether reads live state the sim already exposes: Hatch HP,
   `AliveCount`, kill events, committed-Gatekeeper count. **No accumulator**
   (`../../Decisions.md`).
9. **It degrades.** With DOTween absent or animation disabled, every reading above is still
   correct and static. Motion is emphasis, never the only channel.
10. **Colour is never the only channel.** Hatch integrity is length first and colour second;
    the arrival marks are position and count. This is the same rule the project's charts
    follow and it is what makes the readout survive a colourblind player and a screenshot.

## Implementation

**Create** `Assets/Prefabs/Ui/Tether.prefab` and `Game/Ui/TetherView.cs` — the readout, its
states, and setter methods: `SetIntegrity(float)`, `PulseSalvage(long)`, `SetAtDoor(int)`,
`MarkArrival(string spawnPoint)`, `SetPhase(RunPhase)`. Built as plain uGUI with
`Michsky.UI.Shift.GradientEffect` on the fill and `UIParticleRenderer` for the pulse; animated
with DOTween Pro, which is the game's motion layer and not Shift's.

**Change** `HudController` — push into `TetherView` from the events it already subscribes to.
It gains no state; the events exist.

**Change** `Game/Ui/Theme.cs` — no new tokens. If a state needs one, it earns a row first
(M35's rule).

**Delete** the separate Hatch bar and the standalone Gatekeeper counter, which this replaces.
Say so in the commit; two readouts for one fact is how a HUD gets crowded.

Rejected: a radial or arc tether. A straight line reads its own length at a glance and an arc
does not, and length is the primary channel for criterion 1.

Rejected: moving the readout to the world, drawn on the actual Hatch. It solves the fiction
and fails criterion 7, because the player cannot see the Hatch from most of the room. **Worth
revisiting as an addition once M30 gives a reason to be at the far wall.**

Rejected: a damage-direction indicator around the crosshair, the conventional solution. It
tells the player where *they* are being hit. The loss condition is the door, not the player,
and pointing at the player is what the current HUD already gets wrong.

## Supporting work

- **Driven** — no new input. The player presses the same keys.
- **Shown** — the tether is always on screen; it is the discovery mechanism for itself.
- **Reads back** — criteria 1 through 7, played.

## Verification

1. Criteria 1, 3, 4 — screenshots, judged by someone who has not seen the values. Zo, or a
   second pair of eyes. Record the verdict in `../../GatkeeperZo Notes/playthrough.md`.
2. Criterion 5 — the log shows one draw per Wave with no double-fire during the transition.
3. Criterion 8 — `git diff Assets/Scripts` adds no field that accumulates across frames.
4. Criterion 9 — disable animation and repeat criteria 1 and 3.
5. Criterion 7 — playthrough path: embark at Wave 3, stand at the bore, hold the crosshair on
   a distant Gatekeeper, call out Hatch integrity, then check the number.

## Watch

- **This is the milestone most likely to be judged by feel and least able to prove itself.**
  Criteria 1, 3 and 4 are the honest ones because they are screenshot tests a stranger can
  run. Anything softer than that does not belong in the gate; it belongs in the playthrough
  entry, flagged `[unconfirmed]` under the tier table in
  `../../GatkeeperZo Notes/playthrough.md`.
- **Do not animate the number.** A counting-up salvage total is the reflex and it makes the
  most-read value on screen unreadable for the second it is moving.
- Overdraw. The tether is on screen every frame of every Run and it is the one element where
  True Shadow glow is tempting and expensive (`../../Architecture/3-Unity-Runtime.md`).
- **The end screens are out of scope.** They are read once, in calm, and they do not need
  this. Scope creep here is what turns a feel pass into a quarter.

## Depends on / Unblocks

**Depends on M35** — there is nowhere to build this until uGUI and Shift are in.
Unblocks nothing formally. Pairs with **M30**: the tether is what makes leaving the Hatch
survivable, and M30 is the reason to leave.
