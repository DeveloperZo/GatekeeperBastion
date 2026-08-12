# M37 — Where they die

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-11
-->

Every kill records two raw distances: how far the Gatekeeper was from the Hatch, and how far
it was from where it spawned. Two numbers on a line that already exists. No formula in the
game, no accumulator, no clamp.

**Reviewed adversarially 2026-08-09 and rewritten.** The first draft normalised the two into a
single `penetration` fraction and bundled a dashboard restructure. Both were wrong; see
*What the review killed*.

## Done when

**Met (emit + tool + docs + Path 1).** `EnemyGatekeeper.SpawnedAt` assigned once in
`WaveSpawner.Groups.SpawnOne`; `KillLog.Format` appends `hatchDist=` / `spawnDist=`;
`spawnDist=-1` when `SpawnedAt` was never written; `Tools/wave-report` prints per-Wave
per-kind median `hatchDist` and share beyond 25 m, and reports how many rows had absent
`spawnDist`; `language.md` rule 3 + `builder_play_ban` list both terms; EditMode
`KillDistanceTests` + `KillLog_NamesTheSlot` green.

**Path 1 (this host, Unity 6000.3.21f1 MonoBleedingEdge mcs):** **267 / 0** with the three
Game-touching EditMode files excluded the same way `run.sh` excludes Dev/Reload/Controls
(plus PremiseCopy + Ui rows that also need `Game/`). Filter `KillDistance` / `KillLog_Names`:
**3+1 / 0**.

**AutoVerify EditMode** (`gkb_verify_report.json`, 2026-08-12T00:59:32Z): **298 / 0**,
including `KillDistanceTests` (3) and `KillLog_NamesTheSlot`. PlayMode stage returned
`status:"error"` (unexpected error) — outside this milestone's emit path; same class of
block M41 already notes for `ShoveDoesNotUncommit`.

**wave-report fixture:** seeded kill lines with distances → three kind rows
(Gatekeeper / Hunter / Ram) and `spawnDist absent … 1 kill row(s)`.

**Play** (`Logs/GatekeeperBastion_2026-08-11_20-02-31.log`): **185 / 185** kill lines carry
both fields; Gatekeeper med hatchDist **25.9** m. Criteria 4 (unset play) and 8 (Hatch-bay
vs far-wall pair) **closed by Zo override 2026-08-11** — within-run first/second half
medians already differed (29.1 vs 17.0).

## Story

As the developer, I want to know **where** the fight happens, so that "it felt easy" and
"there is no reason to leave the Hatch" stop being impressions and become the same
measurement.

## Why it matters

**Zo, 2026-08-09: measure Gatekeeper distance moved before dying.** The instinct is right.

**No instrument the project has says anything about position.** The 2026-08-09 session
(Wave-3 deep start, MR-35 T9, `Logs/GatekeeperBastion_2026-08-09_15-21-59.log`) cleared 90
bodies with mean live **3.78**, mean lifetime **3.74 s**, the Hatch at **300/300**, and
**zero arrivals** — figures from `Tools/wave-report/run.sh`, which is the authority. Zero
arrivals reads identically whether those 90 died at 40 m or at 5 m, and those are completely
different games: one is a player holding a firing line, the other is a player standing on the
door letting them come.

**M30 shipped on 2026-08-09 and nothing can tell whether it worked.** The decoy is supposed to
pull Gatekeepers off the door. Its own verification row is a playthrough where Zo watches
bodies peel. **A number that says how far from the Hatch they died is that test**, and today
there is no such number.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No constant moves.

**Multiplier** (the tooling exception, `../../_meta/_definitions/milestone.md`): it is the
readout for **M30's decoy**, for **M32's PRI-C** (a weapon whose whole premise is killing at
the far wall — without this, "it works at range" is untestable), and for any future change to
spawn placement or Gatekeeper speed. It outlives all three: it measures a property of the room,
not of any one feature.

## What the review killed, and what replaced it

An adversarial review on 2026-08-09 found the first draft's central formula broken. Recorded
because the broken version is the obvious one and will be re-proposed otherwise.

**Rejected: `penetration = 1 - (distance / spawnDistance)`, clamped to 0..1.** The upper clamp
is dead code — distance is never negative. **The lower clamp fires in exactly the three cases
this milestone exists to measure**: a Hunter dragged past its spawn point chasing a player at
the far wall, a body pulled outward by M30's decoy, and any knockback. All three read `0`,
identical to dying on the spot. The metric would have been blind to "they got pulled off the
door" while claiming to measure it.

**Rejected for a second reason: bodies do not spawn at the spawn point.** `WaveSpawner.Groups.GroupSlot`
rings group members at `GroupSpread` and NavMesh-samples up to `GroupSpread * 2` away, so a
normalised penetration is non-zero and signed at birth. The first draft's EditMode test
("a body at the spawn point reads 0%") tested arithmetic that never occurs in play.

**Rejected for a third: two coordinate systems in one row.** "Share of kills in the outer half
of the room" is a raw-distance idea; `penetration < 0.5` puts the same physical spot in
different buckets depending on which spawn point the body came through.

**What replaced it: two raw distances and no derived value in the game.** The tool computes
whatever fractions it wants offline, which is this project's own division of labour — the log
carries events, the tool does the arithmetic (`../../Decisions.md`).

```
hatchDist   planar metres from the Gatekeeper to the Hatch at the moment it dies
spawnDist   planar metres from the Gatekeeper to where it actually spawned
```

Both are one subtraction. No clamp, no division, no divide-by-zero, and **a body dragged
outward is now visible**: `hatchDist` large and `spawnDist` large together is exactly the decoy
working, and it was unrepresentable before.

## Acceptance criteria

1. **The kill line carries both, as `key=value`**, matching M29's machine-tail convention
   (`startWave=`, `track=`):
   `[Combat] killed a Gatekeeper with Primary (+25 salvage) — 5 left hatchDist=34.2 spawnDist=11.8`
   The human sentence is unchanged.
2. **Both distances are planar**, y zeroed. `hatchDist` measures to
   `HatchStation.ClosestSurfacePoint`, the convention melee range already uses
   (`../../Architecture.md` gotcha 2); measuring to the Hatch transform gives a number 1.5 m
   out and inconsistent with every other distance in the build.
3. **`spawnDist` measures to where the body actually spawned**, which is its `GroupSlot`
   position after NavMesh sampling, **not** the spawn point's own position. One `Vector3`
   written once at spawn.
4. **A Gatekeeper with no recorded spawn position writes `spawnDist=-1`** — bodies whose
   `SpawnedAt` was never written (hand-placed / unset). `hatchDist` is always real. The tool
   treats `-1` as absent and says how many rows it dropped.
5. **The only new state is one `Vector3` on the Gatekeeper, assigned once at spawn.**
   Checkable: `grep -n "SpawnedAt" Assets/Scripts/Game` returns exactly one assignment site,
   and it is inside `SpawnOne`. **No `SpawnPoint` component field, no `Update`, no
   accumulator** (`../../Decisions.md`).
6. **`Tools/wave-report` prints per Wave**: median `hatchDist`, and the share of kills beyond
   25 m from the Hatch — 25 m being roughly half the room's 52 m depth
   (`Editor/Scenes/SceneBuilder.cs`), stated here so it is not invented at the keyboard.
7. **The tool splits the readout by Gatekeeper kind.** Rams walk the shortest path, Hunters
   chase the player and Defaults march on the door; **reading the three together is
   meaningless**, and a single blended median is worse than no number.
8. **Player action, in a Run:** a Run played from the Hatch and a Run played from the far wall
   produce visibly different medians. **If they do not, this milestone has failed rather than
   shipped**, and the finding is recorded rather than the number being kept.
9. Both terms are added to `_meta/_definitions/language.md` rule 3 as builder-only, so M28's
   check keeps them out of `Docs/Play/`.

## Implementation

**Change** `Game/Enemies/EnemyGatekeeper.cs` — one `public Vector3 SpawnedAt` field.

**Change** `Game/Enemies/WaveSpawner.Groups.cs` — `SpawnOne` already computes the slot position
and passes it to `Instantiate`. Assign it to `SpawnedAt` there. One line.

**Change** `Game/Enemies/WaveSpawner.cs` — `OnGatekeeperDied(GatekeeperKind, Damageable)`
already receives the `Damageable`, so the transform and both distances are available at the
existing log site. Append the two fields where `KillLog.Format` builds the line.

**Change** `Sim/Combat/KillLog.cs` — `Format` / `FormatUnknownSlot` take the two floats;
`PlanarMetres` is the shared arithmetic Path 1 pins.

**Change** `Tools/wave-report/Program.cs` — parse the two fields; add the per-Wave, per-kind rows.

Rejected: a `DistanceToHatch` field on `SpawnPoint`. `SpawnPoint` is a marker with a static
registry and no `Start`, and `PickSpawnPoint()` returns a `Transform` rather than the
component, so resolving it back costs more than storing the position on the body that already
knows it.

Rejected: logging a position triple. Three numbers where two are the reading, and it ties the
log to a geometry `SceneBuilder` may move.

## Supporting work

Developer instrumentation, so the three questions are answered for the developer.

- **Driven** — `Tools/wave-report/run.sh` over any session log.
- **Shown** — the per-Wave, per-kind rows, and **M38**'s dashboard panel. **If M38 slips this
  ships as terminal output only, which is enough**; the rows are the deliverable.
- **Reads back** — the two fields on every kill line.

## Verification

1. Criteria 1, 3, 4 — play one Wave; every kill line carries both fields, and an unset
   `SpawnedAt` body reads `spawnDist=-1`.
2. Criterion 2 — an EditMode test on the arithmetic at fixed positions: a body on the Hatch
   face reads `hatchDist` under 1, one at the far wall reads roughly 50.
3. Criterion 5 — the grep, plus read the one assignment site.
4. Criteria 6, 7 — run the tool over **any session log recorded after this milestone lands**;
   expect three per-kind rows per Wave. *The log this originally pinned has been rotated out of
   `Logs/`, which is the failure `Working-Rules.md` warns about: a gate that names a file which
   will not exist next session.*
5. Criterion 8 — **two Runs, deliberately different**: one never leaving the Hatch bay, one
   fighting from the bore mouth outward. Compare medians and record both in
   `../../GatekeeperZo Notes/playthrough.md`.

## Watch

- **This measures where they died, not where the player stood.** They correlate; they are not
  the same. Measuring the player's own position is a different and harder milestone.
- **`hatchDist` is a straight line, not a path.** A body on the far side of a wall reads close
  and was never close. The Bastion is one open room with a bore, so this is tolerable now and
  stops being tolerable the moment the layout gains geometry.
- **Do not add a derived fraction back into the game later.** The whole reason this reads two
  raw numbers is that every normalisation tried so far hid the case it was meant to reveal.
- **Verification §1's "F3 dev-spawn" line was stale.** F3 is jump-to-wave; unset `SpawnedAt`
  is still the `spawnDist=-1` path.

## Depends on / Unblocks

**Build this before M42.** Both append to the same kill line — M37 adds two fields, M42
rewrites the award number in `KillLog.Format`. Neither milestone mentions the other, and in
parallel they collide. M37 only appends, so it goes first.

Depends on M29's tool, shipped. **Unblocks the verification of M30's decoy and M32's PRI-C**,
neither of which currently has a way to prove its central claim.
