# M16 — A difficulty curve

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-08
-->

Give the Waves a stated, measurable shape — pressure that rises Wave over Wave —
instead of an outcome set almost entirely by which gun the player brought.

## Done when

**Met as the initial tune.** Target curve stated; HP slope moved; harness green.
Three-Tier ±15 play band (criteria 2–5) deferred — reopen only if something is
clearly broken in play.

- Target curve in `Docs/Design/1-Experience.md`: Wave 1 ≈ 100%, 2 ≈ 85%, 3 ≈ 65% (±15),
  Waves 4–5 provisional.
- `EnemyHealthPerWave` **0.40** → multipliers **1.00 / 1.40 / 1.80 / 2.20 / 2.60**.
- Hatch HP / Hatch damage untouched (#12 reserved).
- AutoVerify EditMode **203 / 0**, PlayMode **10 / 0** (2026-08-04).

Baseline that justified the move: `Logs/GatekeeperBastion_2026-08-04_09-14-16`
(MR-35 T3 dial 0: **100 / 100 / 100**, 0 Hatch reaches, 0 player hits).

## What shipped

- `WaveDirector.EnemyHealthPerWave = 0.40f` (was literal `0.25`). Magnitude capped so
  BR-12 still one-shots a Wave-2 Default at 8 m (42 HP = 7×6 pellets).
- Lever 2 (`MaxGroupSize`) already restored by M14 (Wave 3 is 5-wide again); not
  moved again this pass. Gap formula untouched (M10).
- `Design/4-Data.md` — live formulas, target curve, lever table with justifying log.
- EditMode pins for HP / groups / delivery / five-Wave payouts.

## What it did not settle

- Whether T0 / T3 / T7 of one Primary all land in the ±15 band.
- Whether Wave 1 should threaten at all.
- Waves 4–5 paper pacing after the steeper slope (shop ceiling may not clear them).

Re-open with one lever at a time if play shows a flat strong clear or an early weak wall.

## Correction, 2026-08-08

**The instrument this milestone tuned against does not measure what it claimed.** Hatch
remaining measures leakage, not effort: a player who kills everything before it arrives
reads 100% whether the Wave was trivial or at their limit, and it saturates hardest for
the players best able to report how the game feels. On 2026-08-07 a five-Wave clear at
MR-35 T4 read 300/300 on every Wave and was reported as fun and semi-challenging; both
are true and the number carries neither (`../../Design/1-Experience.md`, section 1.3).

What shipped stands — the HP slope moved, the harness is green, and the constants are in
`../../Design/4-Data.md`. **The deferred ±15 play band is retired rather than pending**:
it cannot be met or failed meaningfully. Grading a curve needs a measure of effort, which
is `1-Experience.md`'s standing open question and belongs to whatever milestone answers
it, not to a re-opening of this one.
