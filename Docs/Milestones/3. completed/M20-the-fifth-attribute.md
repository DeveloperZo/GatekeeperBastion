# M20 — The fifth attribute: every weapon does something

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

Every shop weapon gains one activated ability on its own key. The ability is the weapon's
fantasy, and it is the fifth attribute the upgrade wheel (M21) needs in order to exist.

## Closed

**Shipped 2026-08-08 on Zo's verdict after play.** Criteria 1–4 and 6 closed on Path 1 and
Path 2 evidence during implementation; **criterion 5 — the gatekeeper-second bar — closed by
the player, not by a figure in this file.** If the number from that session is wanted on
record it comes from the session log, and this file takes a correction rather than a
retro-edit (`../../_meta/_definitions/milestone.md`, *Shipped files stop changing*).

Shape as shipped: **MK-1 dial = duration** (Zo, 2026-08-07), fixed `Mk1SurgeMultiplier`
2.5x. MR-35 **auto-pins** at its hit threshold; **F** fires the ability.

## Correction, 2026-08-09 — the dial range in criteria 4 and in the tests is 0..50, not 0..35

Criterion 4 reads "M21 will drive that dial from 0 to 35 accumulated points", the
`Implementation` test note reads "monotonic and non-saturating across 0..35", and the
`DialIsMonotonicAndUncapped` row inherits it. **The shipped ceiling is 50.**
`Sim/Meta/WeaponAbility.cs:19` sets `MaxDialPoints = WeaponWheel.FocusAtMax`, and
`WeaponWheel.FocusAtMax = FocusAward 5 x MaxPurchases 10 = 50` — `MaxPurchases` is
`WeaponTier.MaxTier`, which is **10** (`Sim/Meta/WeaponTier.cs:9`). 35 was the figure when
`MaxTier` was 7.

The criterion's substance holds and is met: `WeaponAbility.Magnitude` computes
`t = p / (float)MaxDialPoints` and clamps at `MaxDialPoints`, so the curve is linear with no
plateau across its whole range whatever that range is. What is wrong is only the number
written down for the range's top, and any test that pins `35` as the ceiling would now be
testing 70% of the dial. Correction, not a retro-edit
(`../../../_meta/_definitions/milestone.md`, *Shipped files stop changing*).

## Correction, 2026-08-09 — the Pin Shot could not fire as shipped

`PlayerGun.Firing.RegisterPinHitIfNeeded` ran once per damaged body inside `FireRay`, and
`PinBuildState.RegisterHit` zeroes `Hits` on target change. A pierce through two or three
bodies therefore left the build at **1** against `PinHitThreshold` 3, so the Pin Shot only
reached threshold when a Gatekeeper had nothing behind it — six logged sessions, zero
`Pin Shot` events. **M31** gates the pin register to the first damaged body of a shot;
`PinBuildState` itself is unchanged. Correction, not a retro-edit
(`../../../_meta/_definitions/milestone.md`, *Shipped files stop changing*).

## Story

As a player, I want each gun to *do* something only it can do, so that the weapon I carry
is a character rather than a damage number.

## Why it matters

The game has four weapon attributes — reload, clip, damage, fire rate — and all four are
numbers on a spreadsheet. Nothing a weapon does is unique to it; the BR-12 and the MR-35
differ in how they aim and not at all in what they let you attempt.

M21 needs five attributes on its ring. **A ring of five has 12 distinct arrangements and
excludes two attributes per focus; a ring of four has 3 and excludes one.** The
difference between those two is whether a weapon has an identity the player can read off
the wheel. This milestone exists to make the five-ring possible, and it has to pay for
itself independently — hence the gatekeeper-second bar below.

Leverage tier (`../../Working-Rules.md`): **#5, rules.** It adds a verb the player did
not have, rather than scaling a number they already had.

## The normalization rule

Abilities deliberately differ in **mechanism** — a shotgun's and a rifle's should not be
the same shape. They are normalized on **impact**, in the one currency that already
decides runs: **gatekeeper-seconds removed from the door** (`../../Design/3-Mechanics.md`, the Hatch
budget: 87 gatekeeper-seconds at 300 HP, ~17.5 per Wave across five Waves).

**An ability at zero investment removes >= 2 gatekeeper-seconds per Wave. At full wheel
focus it removes >= 5** — roughly 30% of a Wave's tolerance, which is what makes focusing
it a build rather than a courtesy. A crossbow-style kill-before-arrival removes a whole
dwell; a shove removes the remainder of several stands; sustained throughput removes them
faster. Different mechanisms, one measure.

## The abilities — DECIDED (Zo, 2026-08-06/07)

Not proposals. Build these.

### BR-12 Shotgun — concussive breaching shot

The shot shoves the cluster in front of it away from the Hatch. **Dial = metres of shove.**

Why it is worth a slot: once a Default reaches the Hatch it **commits permanently and can
never be provoked off it again** — damage is the only thing that moves a committed body
today. This is the only mechanic that moves one without killing it. Value counts directly as
bodies shoved times the seconds they spend walking back, which is how criterion 5 is measured.

- **A shove does NOT un-commit a Gatekeeper.** It buys walk-back time only. The one-way commit
  latch gets no exception.
- Wants contact range, which is where this weapon already lives.

### MR-35 Rifle — pin shot, with build-up

Hits **accumulate** on a single Gatekeeper; once they reach a threshold it is pinned in place.
**A single round never pins.** **Dial = seconds held.**

Why the build-up (Zo's call): a one-shot pin is too cheap once fire rate is a spoke a player
can push (M21), and accumulating rewards staying on one target, which is this weapon's
fantasy.

- **The threshold counts HITS, not damage.** `A` sits two
  spokes from `D` on this weapon's ring (M21), so a damage-focused build must get nothing
  toward the ability. Count damage and damage-focus feeds the pin for free and the ring stops
  meaning anything.
- **Build decays when you stop hitting that target**, or everything pins eventually and the
  commitment the mechanic runs on disappears.
- Why it earns the slot: a **Ram cannot be provoked, cannot be blocked, and shoulders through
  crowds.** Nothing in the game influences a Ram except killing it. A pin is the one thing
  that stops its clock without killing it — and it now costs sustained attention to land.

The pair is deliberate: **the shotgun moves bodies, the rifle stops them.**

### Edge cases, decided here so nobody has to guess

- **A Ram can be shoved and can be pinned.** It is immune to *provocation*, not to physics.
  Being the one kind nothing else influences is exactly why both abilities should reach it.
- **Shove does not un-commit** (above). **Pin does not un-commit either** — a pinned Gatekeeper
  at the door resumes hitting the door when the pin ends.
- **A pin re-applied to an already-pinned target refreshes the duration; it does not stack.**
  Two players pinning one body in co-op is one pin, refreshed.
- **Build-up is per Gatekeeper and does not transfer.** Switching targets abandons the progress
  on the old one, subject to the decay rule.
- **Shove affects every Gatekeeper in the cone**, not just the one aimed at — it is the
  shotgun's ability and the cone is the shotgun.

### Rejected, with the filter it leaves behind

*A round that makes them shout*, pulling a wider circle onto the player — rejected. The fault
was structural: the MR-35's weakness is Hunters reaching contact range, and that ability's
whole function was bringing bodies toward the player. **An ability must serve the weapon's
strength or patch its weakness; that one attacked the weakness.** Apply the same test to any
replacement.

Also rejected: instant reload (that is the Secondary's job, and it is a generic shooter
ability with no hook here); a marked target taking extra damage (a second crit source in all
but name — `MetaUpgrades.CritChance` states Luck is the only one); any dash or disengage
(`../../Decisions.md` cut mobility as a mechanic in a 30 x 52 m room); sealing a breach
point (that belongs to Utility).

### MK-1 Sidearm — faster ammo regeneration (Zo, 2026-08-07)

The Secondary regenerates its own ammo (see the ammo model in
`../../Brainstorms/closed/armory-and-weapons.md`). Its ability makes that regeneration run faster.

It rewards actually holding the Secondary rather than treating it as a backup, and it is
**self-contained** — it reads nothing about the Primary. That is the rule the rejected
alternative broke: an offhand weapon that fired during the Primary's reload was an exception
to how weapons work and was not strong enough to earn one.

**Shape (Zo, 2026-08-07): an activated ability with a duration.** Press the key,
regeneration runs faster for a stated number of seconds, then stops. **Dial = the duration**,
not the rate — a longer surge, not a fiercer one. The ability contract holds: key, cooldown,
one scalar dial, no passive exception.

**Measurement note.** This is the first ability whose value reaches the door **indirectly** —
more ammo, more shots, fewer arrivals. Criterion 5's gatekeeper-second bar still applies, but it
is measured one step removed from the effect, so expect a noisier number than the shove or the
pin produce.

## Acceptance criteria

1. `WeaponDef` gains an ability descriptor: an id, a display name, one **scalar dial**,
   and a cooldown. The dial is the field wheel points push in M21.
2. **All three shop weapons** — BR-12, MR-35 and MK-1 — have the abilities specified above.
   `None` and the throwaway row return an explicitly empty descriptor: empty is a stated
   value, never a default and never invented.
3. The ability fires on its own binding through `Controls`/`ControlId`, is listed in the
   M15 shortcut panel, and collides with no existing binding.
4. Each ability's effect is **monotonic and non-saturating across its whole dial range**,
   with no plateau. M21 will drive that dial from 0 to 35 accumulated points; M20 only has
   to guarantee the curve has no ceiling in it. An ability that stops improving partway is a
   trap on the wheel and fails here, before the wheel exists to expose it.
5. Each ability, used on cooldown for a whole Wave at zero dial, removes **>= 2
   gatekeeper-seconds** from the door; at full dial, **>= 5**. Measured, not modelled.
   **Measurement condition:** this can only be read in a Run where bodies actually reach the
   door. The 2026-08-07 session cleared Waves 2–5 at MR-35 T4 with the Hatch untouched
   (`../../Design/2-Dynamics.md`), so measure at a low Tier or a raised dial — a clean Run produces
   a zero and proves nothing.
6. One log line per activation naming the weapon and the ability, and one per Wave
   reporting gatekeeper-seconds removed. Criterion 5 cannot be read without them.

## Usable, or it did not ship

- **Driven** — a key binding, in `Controls`, on the shortcut panel.
- **Shown** — the HUD shows the ability name and its cooldown state on the active weapon.
- **Reads back** — the log names the ability by weapon on every activation.

## Implementation

### Sim

**Change** `Sim/Meta/WeaponDef.cs` — add `abilityId`, `abilityName`, `abilityDial`
(float), `abilityCooldown` (float). Struct stays serializable and engine-free.

**Create** `Sim/Meta/WeaponAbility.cs` — an `AbilityId` enum (`None = 0`, then one per
weapon; **never renumber**, the values reach save-adjacent logs) plus the pure arithmetic
mapping dial value to effect magnitude. Mechanism-specific behaviour lives in the Game
layer; only the numbers live here.

**Change** `Sim/Meta/WeaponCatalog.cs` — one ability per shop row. Adding a sixth weapon
stays a single row edit.

### Game

**Create** `Game/Player/PlayerAbility.cs` — LOCAL-OWNER. Reads the active slot's
`WeaponDef`, runs the cooldown, and dispatches by `AbilityId`. Effects that damage or
move gatekeepers go through the existing HOST-ONLY entry points (`Damageable.TakeDamage`,
R6) — never directly.

**Change** `Game/Input/Controls.cs` — new `ControlId.WeaponAbility`. Verify against the
M15 binding list that nothing is double-bound.

**Change** `Game/Bastion/HudController.cs` — ability name and cooldown ring beside the
clip readout.

### Tests

**Create** `Tests/EditMode/Weapons/WeaponAbilityTests.cs` — every `ShopWeapons()` row has
a non-empty ability (criterion 2); the dial is monotonic and non-saturating across 0..35
(criterion 4); no two shop weapons share an `AbilityId`.

## Verification

### Path 1 — `Tools/verify/run.sh --filter Ability`

`Tests/EditMode/Weapons/WeaponAbilityTests.cs`. Passing means every named test green and
**no `skipped` line** for the new file — a skip is coverage you did not get.

| Test | Asserts | Criterion |
|---|---|---|
| `EveryShopWeaponHasAnAbility` | all three `ShopWeapons()` rows return a non-empty `abilityId`; `None` and `ThrowawayComposite` return the empty descriptor exactly, not a default | 2 |
| `AbilityIdsAreUnique` | no two shop weapons share an `AbilityId` | 2 |
| `DialIsMonotonicAndUncapped` | effect(d+1) > effect(d) across the full dial range, and the last step is no smaller than 40% of the first — catches a plateau without pinning a curve | 4 |
| `PinBuildCountsHitsNotDamage` | N hits pin; one hit does not — damage is not an input to the build (`PinBuildState`) | *Edge cases* |
| `PinBuildDecaysAndIsPerTarget` | build falls after the decay window; switching targets does not carry it | *Edge cases* |
| `Mk1DialIsDuration_NotMultiplier` | MK-1 magnitude is seconds of surge; rate is the fixed `Mk1SurgeMultiplier` | MK-1 shape |

`PinBuildCountsHitsNotDamage` is the one that matters most — it is what stops M21's ring
collapsing, and it is the assertion most likely to be quietly "simplified" later.

Shove/pin MonoBehaviour edge cases (`ShoveDoesNotUncommit`, `PinRefreshesNeverStacks`) live
in PlayMode `WeaponAbilityPlayModeTests` — Path 1 has no NavMesh.

### Path 2 — AutoVerify, for anything touching a MonoBehaviour

Write `gkb_verify_request.txt`, read `gkb_verify_report.json`. Required green:

- One `ControlId` per ability input, with no duplicate `KeyCode` in any shared context, and
  the ability listed on the M15 shortcut panel in the fight contexts (criterion 3).
- A PlayMode test drives `PlayerAbility` on each Primary and asserts the effect fired through
  the HOST-ONLY entry points rather than touching gatekeepers directly (R6).

Path 1 cannot see any of the above.

### Criterion 5 — the gatekeeper-second bar, which only play can answer

**Not measurable in a clean Run.** The 2026-08-07 session cleared Waves 2–5 at MR-35 T4 with
the Hatch untouched (`../../Design/2-Dynamics.md`), so a good Run reports zero and proves nothing.

Run at **T0–T1, dial 0, starting at Wave 3** — the configuration that produced a breach on
2026-08-07 — twice per weapon: once never using the ability, once using it on cooldown all
Wave. Read the per-Wave `gatekeeper-seconds removed` line (criterion 6) and the
`[Hatch] hatch under attack` progression. The difference between the two Runs is the
ability's worth. Repeat at full dial for the >= 5 figure.

**This is a play measurement, and the player's report of it belongs in
`../../GatkeeperZo Notes/playthrough.md` next to the numbers.** Neither figure grades how the
ability feels, and nothing here may be read as saying it does.

### Playthrough path

Play → ARMORY → equip BR-12 → NAV MAP → start at Wave 3 → pull the draw → let a group reach
the Hatch → press the ability key.

On screen: bodies leave the door; the HUD shows the ability name and its cooldown filling.
In the log: one `[Combat]` line naming the weapon and the ability per activation, plus one
`gatekeeper-seconds removed` line at Wave end. Then swap to the MR-35 and hold fire on one Ram
until it pins — the pin is the one that has to be *visible*, because a target that stopped
moving and a target that died look the same at range.

## Open questions

- **Whether a cooldown is the right gate at all** is unverified. Charges spent and
  recovered per Wave would tie abilities to the Wave rhythm rather than a wall clock.
  Cooldown is chosen because it needs no new resource; if abilities end up spammed during
  Prep, charges are the fallback.

## Watch

- An ability that only ever helps at the door makes "leave the Hatch" harder, not easier.
  The floor's 48.5 m of z is already unused.
- Do not let an ability scale off damage as well as its own dial. Two multiplicative
  sources of one currency is how a build becomes dominant (`MetaUpgrades.CritChance`
  carries the same warning).

## Depends on

Nothing. **Blocks M21** — the wheel cannot have five attributes until this ships.
