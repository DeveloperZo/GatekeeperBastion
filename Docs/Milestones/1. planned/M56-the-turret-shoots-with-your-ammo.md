# M56 — The turret shoots with your ammo

<!--
Status: later
Update trigger: when the siphon rate, life duration, onboard magazine, or the turret's damage model changes
Last updated: 2026-08-13
-->

`UTL-H` ships. Ghost-and-confirm placement, a **modest onboard magazine**, and then it
**siphons from the player's Primary reserve on the clock** for as long as it is deployed.
It dies when its life runs out or the reserve is dry. **It does not add ammo to the Run;
it moves where and when that ammo is fired.**

## Story

As a Boarder, I want a second firing position that spends the rounds I would have fired
myself, so that deploying it is a tempo decision rather than free cover I walk away from.

## Why it matters

**The payload's claim was never dps, and the measurement says dps would not have worked.**
`UTL-H`'s line in `../../Brainstorms/fabrication.md` is delivery during the player's dead
time: the 1.4 to 2.6 second reload windows, walking, repairing. The Wave-2 triple in
`../../Design/2-Dynamics.md` is the caution, and it is measured: raising player dps lifted
kill fraction 58 to 83% **and** arrivals 15 to 22, in near-cancellation. A turret that adds
damage would most likely repeat that result.

**The siphon is what makes this a different experiment rather than another dps knob.**
Total rounds spent per Run stays flat; only their distribution in time and space moves. So
the question `fabrication.md` records as unknown, *whether a second firing position moves
the outcome where more player dps did not*, becomes answerable, because the ammo variable
is held still.

**And it is the discouragement (Zo, 2026-08-13).** The Hatch is invulnerable outside a
Hatch-bank Wave (`M53`), so a turret parked at the door was never defending anything. The
siphon makes parking it cost the exploring you left to do. This is the same principle
`UTL-G` already carries: the ammo payload *moves ammo, it does not create it*.

Leverage tier (`../../Working-Rules.md`): **#9, rules** for the siphon and **#10,
structure** for a second firing position. Life duration, siphon rate and onboard rounds are
**#12 constants**, initial values below, tunable after close and not a reason to hold.

## Kill criterion

Ten logged Runs after close. **If the turret is placed and left standing near the Hatch on
most of them, the siphon rate is too low to do its job. If it is never placed at all, the
exchange rate is wrong.** Adjust the two numbers once. If a second ten Runs reads the same,
cut `UTL-H` from the roster rather than tuning it a third time.

## Acceptance criteria

1. **`UTL-H` is a drone-bay payload**, bought flat on the FABRICATION shelf, one payload
   per Run, own drone and own choice per player in co-op
   (`../../Brainstorms/fabrication.md`).
2. **Placement is ghost-and-confirm**, the pattern `M30` shipped. No new placement code
   path; `DecoyPlacement`'s ghost is the one mechanism.
3. **`TurretOnboardRounds = 30`.** It fires these first, before the siphon takes anything.
4. **The siphon runs on the clock, not on the trigger.** While deployed it draws
   `TurretSiphonPerSecond = 2` from the player's Primary reserve **whether or not it is
   firing**. This criterion is the milestone: a per-shot siphon makes an idle turret free
   and inverts the mechanic it exists to create.
5. **It ends at `TurretLifeSeconds = 45`, or when it needs a round and both the onboard
   magazine and the player's reserve are empty, whichever comes first.** The siphon may
   take the player to zero reserve; that is the cost being real. Logged on death with
   rounds fired and rounds siphoned.
6. **It does not inherit weapon upgrades.** Fixed damage and fire rate on the payload, not
   read from `WeaponCatalog` or the M21 spokes. A turret that scales with the build is a
   dps knob wearing a placement ghost, and `fabrication.md` already decided payload
   upgrades wait until a payload has been played.
7. **Targeting is nearest alive in range with line of sight**, through the existing
   `NearestAlive`. Gatekeeper refusals are unchanged: the turret is not a Hatch and not a
   Boarder, and no kind's refusal is overridden by its presence.
8. **The HUD shows the cost while it happens.** The Primary reserve readout drains visibly
   during the siphon, and the turret's remaining life is readable when looked at. The
   player should never be surprised by an empty gun.
9. **Numbers land in `../../Design/4-Data.md` generated, not hand-written** (`M33`), in the
   same change.
10. **Sim ownership is per player and placement is server-authoritative**, so this survives
    `M48` to `M51` without a re-cut. Kills it scores credit the placing player's Unbanked
    pile (`M53`) once that lands.
11. **A session log line carries rounds fired and rounds siphoned per Run**, so
    `Tools/wave-report` can read the kill criterion off the log rather than off memory.
12. **No new counter, no accumulator, no stats object** (`../../Decisions.md`). **No new
    menu row without asking** (`../../Architecture/README.md`).

## Implementation

**Create** the payload: a `UTL-H` row on the shelf, the placed object, and its siphon tick.
The siphon belongs in `Sim/`, driven from the Run tick, not from a MonoBehaviour `Update`
(`M45`).

**Reuse** `DecoyPlacement`'s ghost and confirm, and `DecoyObject`'s placed-object lifetime.
This slice adds one payload, not a second placement system.

**Change** `../../Design/4-Data.md` generation to carry the three constants.

Rejected: **a turret that carries its own independent supply.** That is the version
`fabrication.md` originally recorded, and it is free cover. The siphon is the whole point.

Rejected: **per-shot siphon.** See criterion 4.

Rejected: **auto-repair, or anything framed as Hatch defence.** The Hatch is invulnerable
outside a Hatch-bank Wave, so there is no door to hold. The Deployed Repair Rig is already
parked in `fabrication.md` for this reason.

Rejected: **payload upgrade tracks.** `fabrication.md`, and the roster has not been played.

## Verification

1. Criteria 3 to 5 — deploy with a known reserve, stand still, and watch the reserve fall
   on the clock with the turret not firing. Then deploy with a near-empty reserve and
   confirm it dies early.
2. Criterion 6 — a maxed Primary and a base Primary produce the same turret damage.
3. Criteria 7, 10 — an EditMode test for targeting and ownership; a loopback PlayMode test
   once `M48`'s rig is available.
4. Criterion 11 — one played Run, then read rounds siphoned out of the session log.
5. Kill criterion — ten logged Runs, reported together, not one at a time.

## Watch

- **The near-cancellation is the likeliest outcome, not a worst case.** If kill fraction
  rises and arrivals rise with it, the turret changed the numbers and not the fight, exactly
  as the Wave-2 triple did. That is a finding to record, and it is what the kill criterion
  is for.
- **Criterion 4 is the one that will be argued down**, because a clock siphon feels punishing
  in the first ten minutes of play. It is punishing on purpose. Softening it to per-shot is
  the same as deleting the milestone.
- **The zero-reserve case needs watching rather than pre-solving.** Being left dry by your
  own turret may read as a good hard lesson or as a bug. One played Run answers it; a reserve
  floor is the fix if it reads as a bug, and it is a constant, not a re-cut.

## Depends on / Unblocks

**Depends on `M30`** for the bay, the charge model and the ghost, all shipped.

**Reads best after `M52` and `M53`**, because the two Wave modes are what make Hatch cover
pointless and give the siphon its meaning, but it does not block on them: the payload and
its cost stand on their own and criterion 10 is written so the co-op slices do not force a
re-cut.

**Unblocks** the `fabrication.md` question of whether a second firing position is worth a
payload slot, which is currently the roster's largest unknown.
