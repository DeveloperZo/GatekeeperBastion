# M99 — A range you can shoot in

<!--
Status: later
Update trigger: when a weapon, ability or Gatekeeper kind is added, or when the Ship
door that reaches the range moves
Last updated: 2026-08-20
-->

A firing range reached through a door on the Ship: every weapon at any tier, granted
on the spot, fired at real Gatekeepers at marked distances, with no Run in between.

## Story

As the developer, I want to compare two weapons in under a minute without buying them
or embarking, so that a weapon-feel judgement costs a minute instead of three Runs.

## Why it matters

**Weapon and ability criteria are currently gated on economy.** `M93`'s verification
reads *"buy each Primary at the ARMORY across three Runs"* and *"Three Runs, one weapon
each"*. None of what it is checking — does a shotgun feel like a shotgun, does the pin
hold, does the shove move a body — is a question about salvage, and routing it through
salvage means the answer costs a Run each time it is asked. `M82`, `M84`, `M85` and
`M66` all ask the same class of question and would all pay the same toll.

**The differences are already in the data and the loop to see them is too long.**
`../../Design/2-Dynamics.md` §2.1: a mechanic the player cannot see the result of is not
in their loop. That applies to the developer's loop too, and a three-Run round trip is
where a weapon-feel pass dies.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No stat moves here.

**Durable tooling, with the multiplier stated.** This is not a player-visible change and
qualifies under the tooling exception (`../_meta/_definitions/milestone.md`). It makes
`M93` cheap (retires its three-Run path entirely), and `M82`, `M84`, `M85` and `M66`
cheaper by the same mechanism — each currently needs a live Gatekeeper in front of a
live weapon, and each currently gets there through a Run.

## The range is its own scene, not a room in the Ship

Zo's preference was to build it into the Ship. **Rejected, for two reasons that are not
about taste.**

**A Gatekeeper cannot think on the Ship.** `EnemyGatekeeper.RunIsOver()` returns true
when `_game` is null — *"no controller: nothing left to fight for"* — and targeting reads
`_game.Sim` and `HatchStation.Instance`. That is exactly why `DemoShipFight.SpawnIntruder`
disables the brain and hand-rolls `ShipIntruder` instead. `PlayerAbility` pins and shoves
`EnemyGatekeeper` specifically (`RegisterPinHit`, `ApplyPin`, `_pinTarget`), so a range
built on `ShipIntruder` could never test an ability. Putting a `GameController` and a
`HatchStation` into the hideout to fix that would move Run machinery into the one scene
deliberately free of it.

**PRI-C outranges the Ship.** Its range is 90 m and MR-35's is 60 m
(`../../Design/4-Data.md`); the Ship interior is roughly 40 m across. Falloff is not
testable in a hideout at any budget.

So: **`Assets/Scenes/Range.unity`, reached through a door on the Ship.** The fiction Zo
wanted is preserved — you walk through a door in your hideout — while the scene is shaped
like a Bastion, where every piece of this already works unchanged.

## Acceptance criteria

1. A door in `Ship - CoOp` loads `Range.unity`, and a door in the range returns to the Ship
   with `MetaState` unchanged — no salvage earned, spent, or lost either way.
2. The range grants **any weapon at any tier** from a console, without purchase. The
   grant is session-only through the existing dev-taint path
   (`GameSession.NotifyDevAction` / `AdoptDevState`) and **never writes the real save** —
   returning to the Ship and reloading shows the original loadout.
3. A lane with **distance markers at 5, 14, 30, 60 and 90 m**, readable from the firing
   line. 14 / 60 / 90 are BR-12, MR-35 and PRI-C's range values, so falloff is checkable
   by walking to a marker rather than by reading a table.
4. A console spawns a chosen **Gatekeeper kind** at a chosen marker, with its brain live:
   it paths, it swings, and it can be pinned and shoved. Spawns are capped and a single
   input clears the lane.
5. A **static target** mode holds a Gatekeeper in place with full HP restored on demand,
   so damage-per-shot is readable without chasing it.
6. A readout on screen shows, for the current weapon since the last clear: shots fired,
   total damage, and time-to-kill on the last body killed. It reads from existing damage
   events; **no counters or rollups are added to `Sim/`**
   (`../../Design/3-Mechanics.md`, rejected list).
7. The range never runs a Wave, never touches `RunSim`, and cannot end a Run. Entering it
   from the Ship while a Run is live is refused, with the reason in the log.
8. `Range.unity` is in Build Settings after the three shipped scenes and does not
   disturb the existing order assertion.

## Implementation

**Create** `Assets/Scenes/Range.unity` — a Bastion-shaped scene: `GameController`,
`HatchStation`, a baked NavMesh over the lane, one `PlayerCamera` rig matching the
Bastion's, and the post-processing layer and volume per the 2026-08-20 pass. The lane is
a straight bay of at least 95 m with the five markers as authored geometry.

**Create** `Game/Range/RangeConsole.cs` — the weapon grant and the spawn control. Grants
go through `AdoptDevState` on a constructed `MetaState`, the same taint `LoadDemoCheckpoint`
already uses, so the range cannot corrupt a real save by construction rather than by care.

**Create** `Game/Range/RangeReadout.cs` — subscribes to the damage events that already
exist and draws through `Game/Ui/`, not `OnGUI` (`../../Architecture.md` gotcha 3).

**Change** `Game/Flow/SceneFlow.cs` — a `LoadRange()` beside `LoadShip()`, same shape.

**Change** `Ship - CoOp` — one door interactable. It is the destination hub (Zo,
2026-08-21), so the door goes there and not in `Ship - Demo`. Single subtree (`../../Architecture/8-Scene-Authoring.md`).

**Do not build**: a second enemy type for the range, a range-only weapon path, or any
`ShipIntruder` extension. The range uses the shipped Gatekeeper and the shipped weapons
or it is not measuring the game.

## Verification

- Criteria 1, 7: enter and leave, then reload the Ship and diff `gatekeeper_meta.json` —
  byte-identical. Attempt entry mid-Run and read the refusal line.
- Criterion 2: grant PRI-C at tier 4, return to Ship, reload, confirm the loadout is
  what it was. The Dev-taint banner is up the whole time.
- Criterion 3: fire BR-12 at the 5 m and 30 m markers and read the damage difference off
  the readout — the falloff is the point, not the number.
- Criteria 4, 5: spawn a Default at 30 m, pin it, confirm it holds; spawn a second, shove
  it, and read `concussive shove` metres from the log. This is `M93` criteria 6 and 7,
  with no Run.
- Criterion 6: kill one body, read TTK; clear, swap weapon, repeat. Two numbers, one
  minute.
- Criterion 8: `Tools/verify/run.sh` — the Build Settings order assertion stays green.

**Playthrough path**: Play, Continue, walk to the range door on the co-op ship, grant BR-12,
spawn a Default at 14 m, empty a clip, read the readout, clear, grant MR-35, repeat.

## Usable, or it did not ship

**Driven** — a door, then a console with weapon and spawn rows. **Shown** — the door is
signed and the markers are painted on the lane. **Reads back** — the readout on screen
and the same log lines the Bastion writes.

## Depends on / Unblocks

Depends: nothing. The Gatekeeper, the weapons and the abilities are all shipped.
Unblocks: `M93` (retires the three-Run path), and cheapens `M82`, `M84`, `M85`, `M66`.

## Trap

**Do not let the range grant touch the real save.** The failure is silent and expensive:
a tier granted for a test that survives into a played save makes every later economy
observation wrong, and nothing in the game would report it. `AdoptDevState` exists for
this and is the only sanctioned path.
