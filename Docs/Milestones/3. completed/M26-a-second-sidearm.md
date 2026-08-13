# M26 — A second sidearm, and a loadout that has a shape

<!--
Status: Path 1 shipped, August 2026 — ability + ring still Zo's
Update trigger: when SEC-D's ability, ring, or pairing matrix changes; when play closes the open rows
Last updated: 2026-08-09
-->

`SEC-D` joins the catalog: a close-quarters sidearm that empties its regenerating pool fast and
does nothing past a few metres. With two Primaries and two Secondaries, choosing a loadout
becomes choosing a shape.

## Done when

**Met (code + EditMode Path 1).** Catalog row in every list; identity cues unique among shop
weapons; power band and MK-1 differentiation pinned by `SecondaryCatalogTests`; kill lines name
the slot via `KillLog` + `Damageable.LastDamagingSlot`; both Secondaries cost 0 so either is a
valid first buy. Ability and ring intentionally unset (Open).

## Remaining to close

| # | Status | Owner / evidence |
|---|---|---|
| Criterion 7 — ability | **blocked** | Zo authors a non-MK-1 ability meeting M20's contract |
| Criterion 8 — ring | **blocked** | Zo authors strength/weakness (one line) meeting M21 criteria 6–7 |
| Path 2 — ARMORY two Secondary rows + equip save round-trip | **open** | Play: buy SEC-D, quit, reload, confirm equipped |
| Playthrough path | **open** | Embark MR-35 + SEC-D; Hunter at contact dies; Default across the room does nothing |
| Play question in `GatekeeperZo Notes/playthrough.md` | **open** | Does SEC-D change how you fight, or only what you press when Primary is dry? |
| Watch — kill-share | **instrumented, unread** | Grep `with Secondary` / `with Primary` on a logged Run; split by kind if BR-12 dented |

## What shipped

- `WeaponId.SecD = 5`; `WeaponCatalog.ArraySize` re-anchored to SecD; row in `Def` / `All` /
  `IsAvailable` / `ShopWeapons` / `ForSlot(Secondary)`.
- Base stats: 18 dmg, 6/s, clip 4, reload 2.2 s, range 8 m, falloff from 3 m, coral Dot tracer;
  duty-cycle ≈ 25.1 dps; time-to-empty ≈ 0.67 s (MK-1 ≈ 2.67 s).
- Kill attribution: `PlayerGun` records slot on `Damageable`; `WaveSpawner` emits
  `killed a {kind} with {Primary\|Secondary} (+n salvage) — n left`.
- `SecondaryCatalogTests` (Path 1); M20 ability tests skip SecD as the only allowed unset shop
  ability.
- Play docs + Field Manual + `4-Data.md` rows; ARMORY empty-slot copy names both Secondaries.

## Story

As a player, I want my sidearm to be a choice I make before my first Run, so that the weapon I
fall back on says something about how I intend to fight.

## Why it matters

The Secondary is the weapon you start with and the weapon you hold when the Primary is dry —
and dry is a real state, not an edge case, because Primary ammo drops at a per-kind percentage
and some kinds drop nothing
(`../../Brainstorms/closed/armory-and-weapons.md`). One weapon in the class means every player's
opening is identical and every loadout has the same second half.

The pairing is the point:

| | MK-1 (ranged) | `SEC-D` (close) |
|---|---|---|
| **BR-12** (close Primary) | covers the shotgun's blind spot | committed brawler — everything dies inside a few metres, nothing outside |
| **MR-35** (ranged Primary) | pure marksman, **no answer to a Hunter in your face** | reach when loaded, contact when dry |

Leverage tier (`../../Working-Rules.md`): **#5, rules** — it adds a decision the player did not
have. #12 was available and rejected: re-statting the MK-1 makes the same choice differently
weighted, not a different choice.

## Acceptance criteria

1. `SEC-D` is a catalog row in the Secondary slot, buyable and equippable at the ARMORY,
   present in `Def`, `All()`, `IsAvailable()` and `ShopWeapons()`. — **Met**
   (`SecDIsInEveryCatalogList`).
2. **Every kill is logged with the slot that made it.** — **Met** (`KillLog_NamesTheSlot`;
   wiring in `WaveSpawner` / `Damageable`).
3. Distinguishable from the MK-1 without reading a number (crosshair + tracer). — **Met**
   (`IdentityCuesAreUnique` — Dot + coral vs Cross + amber).
4. Useless at MK-1 working distance; pool empties ≥2× faster. — **Met**
   (`SecDAndMk1AreDifferentWeapons`).
5. Class power band: Secondary aggregate sustained below both Primaries at equal investment.
   — **Met** (`SecondariesNeverRivalPrimaries`). Contact superiority over MR-35 is correct.
6. Both Secondaries regenerate ammo; SEC-D spends faster per shot. — **Met** (shared regen
   path; smaller clip / higher rate).
7. Ability meeting M20 — **Open (Zo)**.
8. Ring meeting M21 criteria 6–7 — **Open (Zo)**.
9. Either Secondary first on a new campaign; neither free-by-default blocks the other. — **Met**
   (both cost 0; `BothSecondariesFree_ReachableOnFreshCampaign`).

## Supporting work

- **Driven** — bought and equipped at the ARMORY like any weapon; fired on the existing input.
- **Shown** — Dot crosshair + coral tracer; ARMORY Secondary list has two rows.
- **Reads back** — purchase/equip log names `SEC-D`; kill lines name the slot.

## Verification

### Path 1 — EditMode `SecondaryCatalogTests` (+ existing ability/wheel suites)

| Test | Asserts | Criterion |
|---|---|---|
| `SecDIsInEveryCatalogList` | Def / All / IsAvailable / Shop / Secondary slot | 1 |
| `KillLog_NamesTheSlot` | format includes slot | 2 |
| `IdentityCuesAreUnique` | no shared crosshair+tracer among shop weapons | 3 |
| `SecDAndMk1AreDifferentWeapons` | near-zero at MK-1 range; TTE ≥2× | 4 |
| `SecondariesNeverRivalPrimaries` | aggregate dps below both Primaries | 5 |
| `BothSecondariesFree_ReachableOnFreshCampaign` | cost 0 + equip round-trip | 9 |
| `ArraySize_ExceedsEveryListedId` | ArraySize > every All() id | Implementation |

### Path 2 / Playthrough

Still open — see Remaining.

## Open

- **`SEC-D`'s ability** (criterion 7) and **its ring** (criterion 8). Neither is authored.
  Guessing a strength/weakness pair is how the rings stop meaning anything. *Zo's.* Catalog
  shipped without them on purpose (`Depends on`).

## Watch

- Kill-share per slot (and by kind) is now readable from transcripts. If Secondary share is
  the majority of kills, the Primary has stopped being the reason to save up.
- Close Secondary + close Primary may dent the BR-12 — same kill-share, split by kind.

## Depends on

**M20** for the ability contract, **M21** for the ring. Catalog row shipped first with both
left unset.
