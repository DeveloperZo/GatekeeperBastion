# M32 — A Primary that wants the far wall

<!--
Status: Path 1 shipped, August 2026 — playthrough criterion 7 remains
Update trigger: when PRI-C's stats, its ability, its ring, or the Primary power band change
Last updated: 2026-08-09
-->

`PRI-C` joins the catalog: a slow, heavy, long-range rifle that keeps full damage at
distances where the MR-35 is down to a third, and punches a funnelled line at the bore.

## Done when

**Met (code + EditMode Path 1).** Catalog row in every list; profile pinned by
`PriCCatalogTests`; full damage at 50 m; sustained dps within 5% of MR-35; Wave-5 pure
fire time >100 s; Railshot dial 3→8 / 12 s / 4 s arm; ring **C D R A F** (Clip/Reload
swapped vs MR-35 — M21 `RingsAreUniqueAcrossShopWeapons`); `PlayerGun` consumes one armed
Railshot overriding pierce count and mul. Criterion 7 playthrough remains.

## Remaining to close

| # | Status | Owner / evidence |
|---|---|---|
| Criterion 7 — bore kills at range | **open** | Play: buy PRI-C, embark Wave 3, fire up the bore from the Hatch; swap to MR-35 at same Tier and confirm the far kills do not land |
| Railshot play log | **open** | Fire armed Railshot into a funnelled line; `Ability` lines show arm + fire with body count |
| Gatekeeper-second bar on Railshot | **open** | M20 criterion 5 in play — provisional credit is 1.0 s/body in `WeaponAbility.PriCRailshotSecondsPerBody` |

## What shipped

- `WeaponId.PriC = 6`; `WeaponCatalog.ArraySize` re-anchored; row in `Def` / `All` /
  `IsAvailable` / `ShopWeapons` after MR-35 / `ForSlot(Primary)`.
- Base stats: 90 dmg, 0.6/s, clip 4, reload 3.2 s, range 90 m, falloff from 55 m, pierce 3
  ×0.75, violet Dot tracer; duty-cycle ≈ 36.5 dps.
- `AbilityId.PriCRailshot`; magnitude 3→8 bodies at full damage; cooldown 12 s; armed 4 s.
- `PlayerAbility.ActivateRailshot` → `PlayerGun.ArmRailshot` / `TryConsumeRailshot` overrides
  pierce for exactly one shot.
- Wheel ring **C D R A F**, strength Damage, weakness FireRate (Clip/Reload swapped vs MR-35).
- `PriCCatalogTests`; Play docs + Field Manual + `4-Data.md` rows.

## Story

As a player, I want a weapon that is better the further out I fight, so that leaving the
Hatch is something a build can want rather than a risk with no reward.

## Why it matters

**M31 opens a gap on purpose and this fills it.** After M31 the MK-1 is at zero past 45 m
and the MR-35 is at 43% at 45 m and 29% at the far wall. The Bastion floor is 52 m deep
(`Editor/Scenes/SceneBuilder.cs`) and the back half of it is owned by no weapon.

**It also gives the standing-at-the-Hatch problem a tool rather than another push.** Zo's
note, already in `../../Design/1-Experience.md`, is that there is no incentive to move away
from the Hatch, and today's Run took 24 Hatch arrivals across 210 bodies. M30 works the
problem from the threat side by making the door attackable from range. This works it from
the loadout side: a weapon whose damage is highest where the player currently never goes.
Neither is sufficient alone and they are deliberately not the same milestone.

**The bore is why it pierces.** The Hatch bay narrows to a gap at x −3..3, z −10, so every
Gatekeeper walking at the door funnels through a 6 m mouth. A slow heavy rifle aimed up that
bore is the one place in the room where a line reliably forms — which is a positional
payoff rather than the free line-delete M31 removes.

Leverage tier (`../../Working-Rules.md`): **#5, rules.** It adds a reason to occupy space
the game already has, rather than scaling anything.

## Acceptance criteria

1. `PRI-C` is a catalog row in the **Primary** slot, buyable at the ARMORY, equippable,
   persisted across Runs, and present in `Def`, `All()`, `IsAvailable()` and
   `ShopWeapons()`. — **Met** (`PriCIsInEveryCatalogList`)
2. Its profile is the table below, and `WeaponId` gains a **new value at the next free
   number** — never a renumber, never a reused id (`WeaponId.cs`). — **Met** (`PriC = 6`)
3. **It keeps full damage across the whole room.** `falloffStart` 55 exceeds the longest
   sightline (~50 m), so no shot inside the Bastion is ever reduced by distance. — **Met**
4. **It does not out-sustain the MR-35.** At T0 both land within 5% on sustained dps
   including reload. — **Met** (36.5 vs 37.0)
5. **It cannot carry a late Wave alone.** At 0.6 shots/second a Wave-5 delivery of 66 bodies
   takes >100 seconds of pure firing. — **Met**
6. It has one ability on `F` and a five-spoke ring. — **Met** (Railshot + C D R A F)
7. **Player action, in a Run:** standing at the Hatch and firing up the bore produces kills
   at the far end that the MR-35 at the same position and Tier does not. — **Open**
   (arithmetic gate in `FarWall_PriCKillsWhereMr35DoesNot`; playthrough still required)

## The weapon

| field | value | why |
|---|---|---|
| `slot` | Primary | |
| `cost` | 1500 | above the MR-35's 1200; it is the third Primary, not the first |
| `damage` | 90 | one-shots any non-Ram at any Wave (Wave-5 body is 78 HP) |
| `shotsPerSecond` | 0.6 | the whole cost of that one-shot |
| `clipSize` | 4 | |
| `reloadSeconds` | 3.2 | |
| `range` | 90 | |
| `falloffStart` | 55 | criterion 3 — never falls off indoors |
| `pellets` | 1 | |
| `pierceCount` | 3 | the bore shot |
| `pierceDamageMul` | 0.75 | above the MR-35's 0.26 (M31): its line is the payoff for a rate this low |
| `crosshair` | `Dot` | |

## The ability — Railshot

`F` arms the next shot: it pierces **N bodies at full damage**, ignoring
`pierceDamageMul`. Dial is N, **3 at zero investment and 8 at a full ability focus**, cooldown
12 s, and the armed state lapses after 4 s if unfired.

## The ring

| weapon | ring order | strength | weakness |
|---|---|---|---|
| `PRI-C` | C D R A F | Damage | Fire rate |

Clip and Reload swapped vs the MR-35 so M21's unique-ring rule holds. Strength/weakness
unchanged (Damage / Fire rate, two apart, Ability adjacent to weakness).

## Open questions

- Whether `falloffStart` 55 is reachable is **unknown until the Bastion changes size.**

## Depends on / Unblocks

**Depends on M31** — shipped. Also depended on M21 closing for ARMORY range lines — M21
criteria 9–10 closed.
