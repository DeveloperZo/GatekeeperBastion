# M71 — The sidearm joins the wheel

<!--
Status: next
Update trigger: when the SEC-D ability decision lands (its ring copies this pattern), or
when the per-point table moves
Last updated: 2026-08-15
-->

The MK-1 gets an upgrade ring. Everything else — purchase command, pricing, ARMORY wheel
UI, point storage, save round-trip — shipped slot-agnostic with M21 and already refuses
unringed weapons with a logged reason; this slice is two switch cases, the tests that
gate them, and the doc recuts.

## Story

As a Boarder, I want to focus my MK-1 on the wheel the way I focus my primary, so that
the gun I fall back on is a build choice rather than a fixed stat block.

## Why it matters

The wheel is the game's build-expression lever (`../../Design/3-Mechanics.md`), and the
MK-1 is the one weapon every campaign owns that sits outside it — `../../Design/4-Data.md`
documents "it never improves" as a standing gap, and M21 deferred the ring to exactly
this slice (`../3. completed/M21-the-upgrade-wheel.md`, Remaining to close).

Leverage tier (`../../Working-Rules.md`): **#12, constants** — two data rows inside an
existing rule. A higher tier is wrong because the rule (award spread, pricing, derived
tier) already exists and nothing structural moves.

Pacing constraint (Zo, 2026-08-14, amended same session): the first wheel purchase must
cost at least 150 and leave headroom for a possible second off one wave-1 clear (450 =
`WaveDirector.BaseEnemyCount` 18 × 25 base salvage). The shared curve's 400 entry allowed
exactly one purchase with no headroom, so Secondaries get their own curve —
`WeaponTier.Cost(WeaponSlot.Secondary, t)` = 150 + 150 × t, first two purchases 150 + 300
= 450 exactly; Primaries keep 400 + 200 × t. This reverses the "reuse the primary cost
curve" call made earlier the same session, on Zo's instruction. Leverage tier: still
constants — a new lane on the existing linear-ramp rule, no structure moves.

## Kill criterion

If criterion 4 cannot pass with the shared per-point table — that is, keeping the
damage-focused MK-1 under the damage-focused primaries would need per-point steps below
the one-purchase ~20% perceptibility bar (`../../Design/4-Data.md`) — this stops, the
finding is filed against the shared table, and nothing ships. Resolves on the build's
first `Tools/verify/run.sh`; the slice is not shippable in a failing state.

## Acceptance criteria

1. `WeaponWheel.RingOf(WeaponId.Mk1Sidearm)` returns the cycle
   **Damage – Ability – Reload – Clip – FireRate**, and the shipped ring tests
   (`RingsAreUniqueAcrossShopWeapons`, `WeaknessNeverAdjacentToStrength`) pass unmodified.
2. `WeaponWheel.TryIdentity(Mk1Sidearm)` returns true with **strength Reload, weakness
   Damage**.
3. Player action: at the ARMORY with upgrades unlocked and ≥150 banked, the MK-1 row
   shows the wheel instead of "Upgrade ring not authored for this weapon yet."; clicking
   a spoke spends `WeaponTier.Cost(WeaponSlot.Secondary, tier)` and logs the existing
   `bought MK-1 Sidearm focus … (+…, +…) for … — T…` line. The SEC-D row shows its own
   wheel (FireRate–Ability–Reload–Clip–Damage); a centred Ability purchase is refused
   (`SpokePurchasable`) with a reason that names the next salvage cost. The other four
   spokes buy at `WeaponTier.Cost(WeaponSlot.Secondary, tier)`.
4. `SecondariesNeverRivalPrimaries` passes with the ring live (its `t > 0` skip no longer
   exempts the MK-1), and a new test asserts the MK-1's damage-centred `FocusHistory`
   stays below each primary's damage-centred history in `EffectiveDps` at every purchase
   count 1..10. Cross-centre comparisons (damage-focused MK-1 against a reload-focused
   primary) are build expression, not rivalry — deliberately unasserted; that is the
   counter-case this rule does not cover.
5. *(Recut 2026-08-14, same session — Zo widened the gate from "one purchase per wave-1
   clear on the shared curve" to a Secondary-slot curve with headroom for two.)*
   `WeaponTier.Cost(WeaponSlot.Secondary, t)` is 150 + 150 × t (150…1,500, cumulative
   8,250; `SecondaryTierCosts_LinearRamp_SumsTo8250`), Primaries keep 400 + 200 × t.
   A test asserts from source constants: the Secondary entry price is at least 150, and
   the first two purchases together fit `WaveDirector.BaseEnemyCount` × the base
   salvage-per-kill constant (Scavenging level 0, the 25 in `../../Design/4-Data.md`).
   Availability is unchanged: purchases stay gated on `MetaUpgrades.UpgradesUnlocked`
   (first lost run); this criterion is affordability only.
6. Nonzero MK-1 wheel points survive a JSON save round-trip.
7. Ability spoke reads back in play: with ability points bought, the Regen Surge log
   line's dial value changes (`WeaponAbility.Magnitude` — 4 s at 0 points, 10 s at
   focus-max 50).
8. Docs recut in the same change: `Tools/docgen/run.sh` regenerates the `rings`,
   `mk1-base` and `wheel` blocks; the hand prose claiming the MK-1 "never improves" and
   "cannot buy points" (`../../Design/4-Data.md`) and the "Unbuilt inside this lever"
   paragraph (`../../Design/3-Mechanics.md`) are recut to drop "SEC-D has no ring"; M21's
   Remaining-to-close MK-1 row and the `verification.md` MK-1 row close, per that file's
   two-edit rule. SEC-D ring recut 2026-08-15: ring authored, Ability purchase locked.

## Implementation

**Change** `Assets/Scripts/Sim/Meta/WeaponWheel.cs` — add `Mk1Sidearm` cases to `RingOf`
and `TryIdentity`. Ring reason: on this cycle Damage's neighbours are Ability and
FireRate, not Clip — the rejected layouts that give Damage the {FireRate, Clip}
neighbourhood maximise duty-cycle compounding on a 1.4 s-reload gun, which is exactly the
never-rival risk — and Ability sits beside the Reload strength, so the readiness build
feeds Regen Surge (+3 ability per reload-centred purchase).

**Change** `Assets/Scripts/Sim/Meta/WeaponTier.cs` — add `Cost(WeaponSlot, int)` and
`SpentToReach(WeaponSlot, int)` overloads carrying the Secondary lane; the tier-only
methods stay as the Primary/historical curve.

**Change** `Assets/Scripts/Game/Ship/ShipCommands.cs` and
`Assets/Scripts/Game/Ship/ArmoryStation.cs` — the charge and the ARMORY price label
price by `WeaponCatalog.Def(id).slot`.

**Change** `Tools/docgen/Program.cs` — the `tier-cost` block and the Field-Manual wheel
section emit both curves.

**Change** `Assets/Tests/EditMode/Meta/SecondaryCatalogTests.cs` — the criterion-4
focused never-rival test and the criterion-5 pacing test.

**Change** `Assets/Tests/EditMode/Meta/WeaponWheelTests.cs` (or the existing round-trip
test home) — criterion 6.

**Change** `Docs/Design/4-Data.md`, `Docs/Design/3-Mechanics.md`,
`Docs/Milestones/3. completed/M21-the-upgrade-wheel.md`,
`Docs/GatekeeperZo Notes/verification.md` — criterion 8. Run `Tools/docgen/run.sh`.

**Nothing changes** in `MetaState` or `SaveData` — storage and persistence shipped
slot-agnostic with M21. `MetaState.ReconstructSpentFromLevels` deliberately stays on the
historical shared curve: it only runs for pre-wheel migration and tamper repair, and
every purchase it reconstructs was made at that curve; pricing it by slot would misstate
the v1-save history the ledger fixtures pin. Named so nobody rebuilds them.

Rejected (then reversed 2026-08-15, Zo): **SEC-D ring** — first kept off because
`AbilityId.None` would sell dead Ability points; reversed when Zo asked for a SEC-D
wheel with Ability purchase locked (`SpokePurchasable`) instead of no ring.
Rejected (then reversed by the criterion-5 recut): **a cheaper secondary cost curve** —
first kept off to avoid new constants, reversed the same session when Zo set the
headroom-for-two constraint that the shared 400 entry cannot meet.
Rejected: **a secondary-specific per-point table** — only on the kill criterion firing.

## Verification

1. Criteria 1, 2, 4, 5, 6 — `Tools/verify/run.sh`; all named tests green (all are
   Sim-level EditMode).
2. Criterion 8 — `Tools/docgen/run.sh --check` clean; grep `4-Data.md` for
   "never improves" returns nothing.
3. Criteria 15-style regression — `gkb_verify_request.txt`, compare
   `gkb_verify_report.json` to the 2026-08-15T00:09:20Z baseline (door-aperture,
   `Repair_ClientChannel`, 4× `HudSeparationTests` are the knowns); no new names.
4. Playthrough path — from Play: MainMenu → Ship → ARMORY. MK-1 row shows the wheel; buy
   Reload focus on a save with one lost run and ≥150 banked; expect
   `bought MK-1 Sidearm focus reload (+ability, +clip) for 150 — T1`. Embark, trigger
   Regen Surge; expect its log line's dial to reflect the ability points. Confirms
   Driven (spoke click), Shown (the wheel replaces the not-authored note), Reads back
   (purchase log line, tier label, surge dial).

## Watch

- **M21's central claim is still unplayed** — "two opposite focuses feel like different
  weapons" sits open in `verification.md` for the primaries, and this slice extends the
  same unanswered question to the MK-1. It does not answer it; the row stands.
- **The ARMORY loud-fail for a roster outgrowing its authored rows** is untouched — the
  MK-1 already has a row.

## Depends on / Unblocks

**Depends on** M20 (Regen Surge) and M21 (the wheel) — both shipped. **Unblocks** nothing
further on the SEC-D ability, which stays gated on the M26 decision (Zo).
