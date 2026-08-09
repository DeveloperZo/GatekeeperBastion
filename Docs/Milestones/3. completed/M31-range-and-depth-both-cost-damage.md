# M31 — Range and depth both cost damage

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

A shot that passes through a second body does half damage to it, and every Primary loses
damage with distance. Where you stand and how deep the line is both become decisions.

**It also carries the fix for the MR-35's pin**, which could not fire as shipped, because
the fix is the same first-damaged-body flag this milestone already adds.

## Done when

**Met (code + EditMode Path 1).** `pierceDamageMul` on `WeaponDef` / catalog (MR-35
**0.26**, Throwaway 0.25, else 1.0); flat apply in `FireRay`; falloff table (MK-1 start 12,
MR-35 range 60 / start 25, BR-12 unchanged); pin register on first damaged body only;
`PinBuildState` unchanged. Criteria 1–6 and 9–11 on Path 1 (criterion 6 via the 0.26
correction below). Criterion 8: one struct field, no new Sim type. Dotnet EditMode harness
Pierce filter **9 / 0** after the retune — 2026-08-09.

## Correction, 2026-08-09 — MR-35 `pierceDamageMul` is 0.26, not 0.5

Criterion 1 and the 2026-08-08 note sized **0.5** against a 35-point shop ceiling that is
not in the code. At `FocusAtMax` 50 that left pierce at ~148.7 vs Wave-5 78 HP. Zo set
**0.26** on 2026-08-09 (`78 / 297.5`) so criterion 6 holds at the real ceiling. Criterion 1's
written 0.5 and criterion 6's "35 Damage points / 152.4 / 76.2" stay as history; the live
values are catalog `0.26f` and FocusAtMax arithmetic. Correction, not a retro-edit
(`../../_meta/_definitions/milestone.md`, *Shipped files stop changing*).

## Remaining to close

| # | Status | Evidence / action |
|---|---|---|
| Criteria 7, 10 | **open — playthrough** | Wave-1 Hatch shot → one kill line; three shots into a line → `Pin Shot` in the log |
| FireRay layer mask | **open** | Audit item: no Enemy/World layers exist yet; buffer raised 16→32 with full-buffer warn only |

## Story

As a player, I want a shot through three bodies to be worth less than three shots, so that
an upgraded rifle is a weapon with a shape rather than a line-deleter.

## Why it matters

**Pierce is free today and the upgrade wheel multiplies it.** The MR-35 ships
`pierceCount: 2` at every Tier (`Assets/Scripts/Sim/Meta/WeaponCatalog.cs`), so it has
always hit three bodies, each for the full amount. At T0 that is 32 damage against a
Wave-1 Gatekeeper's 30 HP — one trigger pull, three corpses. At the shop ceiling
`WeaponWheel.DamagePerPoint` 1.0456 over 35 focused points gives **4.76x**, so the same
shot lands 152 and deletes a line at any Wave. Zo's report on 2026-08-08: weapons hit too
hard and punch through multiple enemies after upgrading.

**Distance costs nothing either.** `falloffStart` is 0 on both the MK-1 and the MR-35,
which the field's own comment defines as no falloff. The Bastion floor is 30 m by 52 m
(`Editor/Scenes/SceneBuilder.cs`) with the Hatch at its south end, so a player standing at
the door does identical damage to a Gatekeeper at the bore mouth and one at the far wall.
Position is currently free, which is one more reason the Hatch is the only place worth
standing.

**0.5 is Zo's number and it lands on the HP curve well.** A fully damage-focused MR-35 does
152.4; halved that is 76.2, against a Wave-5 Gatekeeper's 78 HP
(`../../Design/4-Data.md`). So the maximum shop build still clears lines through Wave 4 and
stops clearing them at Wave 5 — the ceiling arrives exactly where the fight is hardest,
without a special case anywhere in the code.

**And pierce is why the Pin Shot has never fired.** `PlayerGun.Firing.RegisterPinHitIfNeeded`
is called once per damaged body inside `FireRay`, and `PinBuildState.RegisterHit` resets
`Hits` to zero whenever the target changes (`Sim/Meta/WeaponAbility.cs`). A shot through two
or three bodies therefore registers on each of them in distance order and **leaves the
build at 1, every time**. `PinHitThreshold` is 3, so the build only reaches it when the
player shoots a Gatekeeper with nothing behind it — the one situation in a wave-defense
game where a pin is worth least. Six logged sessions contain zero `Pin Shot` events
(`Brainstorms/Master-Brainstorm.md`). The decay is not the problem: `DecayRemaining` resets
on every hit, so three shots at 1.6/second build fine against a single target.

Leverage tier (`../../Working-Rules.md`): **#12 for the falloff numbers, #10 for the pierce
multiplier** — the multiplier adds a term to the damage rule that did not exist, which is
structure. A higher tier is wrong because the mechanism is right and only its magnitude and
its distance term are missing.

## Correction, 2026-08-09 — the 35-point ceiling this argument uses does not exist

**Every figure above that reads "35 focused points", "4.76x" or "152" was computed against a
shop ceiling that is not in the code.** A full focus is **50** points, and the argument's
central claim inverts at that number. **Nothing was retuned to match** — the milestone shipped
with 0.5 as written, and this correction is the record of the gap.

Where 50 comes from: `Assets/Scripts/Sim/Meta/WeaponWheel.cs` sets `FocusAward = 5` and
`MaxPurchases = WeaponTier.MaxTier`; `Assets/Scripts/Sim/Meta/WeaponTier.cs:9` sets
`MaxTier = 10`. So `FocusAtMax = FocusAward x MaxPurchases = 5 x 10 = 50`, and each
neighbour takes `NeighbourAward = 3` ten times for `NeighbourAtMax = 30`. One purchase pays
11 points (5 + 3 + 3 + 0 + 0), ten purchases pay 110 across the ring.

Both sets of arithmetic, on the same MR-35 base damage of **32**
(`WeaponCatalog.Def(WeaponId.Mr35Rifle)`) and the same Wave-5 Default HP of **78**
(`Sim/Waves/WaveDirector.cs`: `BaseEnemyHp` 30 x `(1 + EnemyHealthPerWave 0.40 x waveIndex 4)`
= 30 x 2.60):

| ceiling | damage multiplier | shot | after the 0.5 pierce multiplier | against 78 HP |
|---|---|---|---|---|
| 35 points *(assumed above; no longer reachable)* | `1.0456^35` = **4.762** | 32 x 4.762 = **152.4** | **76.2** | survives, by 1.8 |
| 50 points *(`WeaponWheel.FocusAtMax`)* | `1.0456^50` = **9.296** | 32 x 9.296 = **297.5** | **148.7** | dies, with 70.7 to spare |

"The ceiling arrives exactly where the fight is hardest" is therefore false at the real
ceiling. At 50 points the halved pierce hit is **1.91x** a Wave-5 Gatekeeper's HP, so the
maximum shop build still deletes lines through Wave 5, which is the outcome this milestone
was cut to remove. The 2 HP margin the *Watch* section calls fragile is already spent.

**The pierce multiplier is Zo's decision, not an agent's.** 0.5 was Zo's number on
2026-08-08 and it was picked against the 35-point figure. Choosing its replacement is a
tuning decision, and retuning against arithmetic with no logged Run behind it is this
roadmap's first named risk (`../../Roadmap.md`, *Dependencies and risks*). For scale only,
not as a proposal: the multiplier that lands the ceiling shot exactly on 78 HP is
`78 / 297.5` = **0.262**. Whether the answer is a lower multiplier, the compounding variant
already named as rejected-but-available below, a lower `WeaponWheel.DamagePerPoint`, or
leaving the ceiling strong on purpose, is open and unresolved.

**The criteria below are unchanged, and criterion 6 is knowingly stale as written.** Its
"35 Damage points, 152.4 base, 76.2 after the multiplier" carries the same dead ceiling, as
do the Verification steps that mirror it — step 1's "35 Damage points", and step 4's `T7`
loadout, which is now a mid-track Tier rather than the shop ceiling. They are left intact
because rewriting a gate to match a number is the softening the roadmap forbids
(`../../Roadmap.md`, *Maintaining this file*); criterion 6 is re-cut in the same decision
that sets the multiplier.

## Acceptance criteria

1. `WeaponDef` gains `float pierceDamageMul`, documented as the flat fraction applied to
   every damaged body **after the first**. `WeaponCatalog` sets it per weapon: MR-35
   **0.5**, `ThrowawayComposite` **0.25**, every other row **1.0**.
2. In `PlayerGun.Firing.FireRay`, damage is `base x falloff(distance) x (first ? 1 :
   pierceDamageMul)`. **The multiplier is flat, not compounding** — bodies 2 and 3 take the
   same fraction as each other.
3. `ThrowawayComposite` at 0.25 proves the field is read per weapon rather than
   hardcoded. A test that passes with the constant inlined has not tested criterion 1.
4. The falloff table below is what `WeaponCatalog` returns.
5. **At T0 the MR-35 stops clearing a Wave-1 line.** Body 1 takes 32 against 30 HP and
   dies; bodies 2 and 3 take 16 and live.
6. **At the shop ceiling a pierced Wave-5 Gatekeeper survives.** 35 Damage points, 152.4
   base, 76.2 after the multiplier, against 78 HP.
7. **Player action, in a Run:** from the Hatch, one MR-35 shot into a group of three
   Wave-1 Gatekeepers produces exactly one `killed a Gatekeeper` line, not three inside the
   same tenth of a second. Before this milestone the same shot produces three.
8. Nothing in `Sim/` gains a counter, a rollup or a stats object (`../../Decisions.md`).
9. **A pin hit is registered for the first damaged body of a shot only.** Bodies 2 and 3
   take damage and do not touch `PinBuildState`.
10. **The MR-35 pins a body in a line.** Three consecutive shots into a group of three
    Wave-1 Gatekeepers, aimed at the front one, produce a `Pin Shot` line in the log. Before
    this milestone the same three shots produce nothing.
11. `PinBuildState` is unchanged. The reset-on-target-change rule is correct — a player who
    switches targets should start over — and the defect is entirely in how many times per
    shot it is called.

## The falloff table

Anchored on the arena rather than on ratios. The floor runs z −22 to +30; the Hatch sits
at z −20.5 and the Hatch bay narrows to a bore at z −10, so the player at the door sees the
bore mouth at ~10 m and the far wall at ~50 m.

| weapon | range | falloffStart | at 10 m | at 25 m | at 45 m | owns |
|---|---|---|---|---|---|---|
| MK-1 Sidearm | 45 *(unchanged)* | 0 → **12** | 100% | 61% | 0% | the Hatch bay |
| BR-12 Shotgun | 14 *(unchanged)* | 8 *(unchanged)* | 67% | out of range | — | contact |
| MR-35 Rifle | 80 → **60** | 0 → **25** | 100% | 100% | 43% | the mid room |

The MR-35's `range` drops from 80 to 60 because 80 exceeds the room's longest sightline, so
a falloff that ends at 80 never bites: at 50 m it would still be at 60%. At range 60 the
same shot is at 29% across the room, which is a cost the player can feel. **The far half of
the room is now owned by nothing**, and that is deliberate — it is the niche M32's PRI-C is
cut to fill.

## Implementation

**Change** `Assets/Scripts/Sim/Meta/WeaponDef.cs` — add `pierceDamageMul` next to
`pierceCount`, with the comment stating it is flat and applies from the second body on.

**Change** `Assets/Scripts/Sim/Meta/WeaponCatalog.cs` — `Make` takes the new parameter; the
five rows carry the values in criterion 1 and the falloff table above.

**Change** `Assets/Scripts/Game/Player/PlayerGun.Firing.cs` — `FireRay` tracks whether the
current body is the first damaged one and applies the multiplier. `_pierceScratch.Count`
already distinguishes them, so this is one expression, not new state.

**The same flag gates the pin.** `RegisterPinHitIfNeeded` moves inside the `first` branch.
That is criterion 9, it is one condition, and it is why the pin fix lives here rather than
in its own milestone: two milestones editing the same three lines of `FireRay` is worse
than one milestone doing both.

**Change** `Docs/Milestones/3. completed/M20-the-fifth-attribute.md` — append a correction
recording that the Pin Shot could not fire as shipped, the mechanism, and that M31 carries
the fix. Shipped files take corrections, never retro-edits
(`../../_meta/_definitions/milestone.md`).

`ApplyFalloff` is unchanged. It already does linear-to-zero between `falloffStart` and
`range`, which is the curve this milestone wants; only the inputs move.

Rejected: making the pierce multiplier a wheel attribute. It would give the player a way to
buy back exactly what this milestone removes, and the wheel already has five spokes
(`../../Design/4-Data.md`).

Rejected: compounding the multiplier down the line (100 / 50 / 25). Zo picked flat on
2026-08-08. Compounding is the stronger nerf and remains available if criterion 6 turns out
to be too generous in play.

## Supporting work

- **Driven** — no new input. The change is in what the existing trigger does.
- **Shown** — the ARMORY should state effective range and the pierce penalty. That surface
  belongs to **M21**, whose criteria 9–10 cover the ARMORY projection and are still open.
  If M21 slips, this ships with the numbers visible only in `../../Play/Field-Manual.md`,
  which is a wiki page and not the fight.
- **Reads back** — criterion 7: the kill lines a shot produces.

## Verification

1. Criteria 1–6 — `Tools/verify/run.sh`. New `WeaponPierceFalloffTests`: the per-weapon
   multiplier values, flat-not-compounding across three bodies, the falloff table at 10 /
   25 / 45 m, and the two HP-curve assertions at T0 and at 35 Damage points.
2. Criterion 3 — `grep -n "0\.5f" Assets/Scripts/Game/Player/PlayerGun.Firing.cs` returns
   nothing.
3. Criteria 9–11 — a `PinBuildSurvivesPierce` test asserting the build reaches 3 across
   three shots into a three-body line. Then playthrough: embark at Wave 1 with the MR-35,
   fire three times into the front of an arriving group; the log shows
   `MR-35 Rifle — Pin Shot` and a `pin held Ns` line.
4. Criterion 7 — playthrough path: Play → NAV MAP → embark at Wave 1 with the MR-35 → from
   the Hatch, fire once into an arriving group of three. The log shows one kill line. Then
   `F7` a damage-focused T7 loadout and repeat at Wave 5: still one kill line.
5. Criterion 8 — `git diff Assets/Scripts/Sim` adds one struct field and no type.

## Watch

- **Criterion 6 is arithmetic, not play.** 76.2 against 78 is a 2 HP margin, and any later
  move to `EnemyHealthPerWave` or to `DamagePerPoint` crosses it. Whoever moves either one
  re-checks this line rather than assuming it holds.
- The BR-12 fires 7 pellets at `pierceCount: 0`, so the multiplier never applies to it. A
  test asserting the shotgun is unchanged is worth more than it looks — it is the row most
  likely to break when `Make`'s signature grows.
- Falloff applies to the ability path only if the ability deals damage through `FireRay`.
  The MR-35's Pin Shot does not deal damage, so it is unaffected; confirm rather than
  assume when M32's Railshot lands.
- **Fixing the pin will move M20's gatekeeper-second reading off zero for the first time**,
  which means the ≥2 bar M20 shipped without meeting becomes measurable rather than
  hypothetical. Record the number; do not retune the pin inside this milestone.

## Depends on / Unblocks

Depends on nothing. **Unblocks M32**, whose PRI-C is defined by the far-room gap this
milestone opens.
