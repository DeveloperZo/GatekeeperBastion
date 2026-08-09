# M27 — The game says who you are

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

The vocabulary inverted on 2026-08-08: Gatekeepers hold a Bastion and the player is the
Boarder who cuts in. Every file said so. Nothing the player saw did. Now the NAV MAP and
both end screens say it.

## Done when

**Met (code + EditMode + Play wiki).** NAV MAP premise names a garrisoned Bastion, Gatekeepers,
and the Hatch as the way back to the Ship (14 words); `Gate` removed rather than redefined;
lose screen says `thrown off the Bastion`; clear screen says `out through the Hatch`; Play
entry pages and `language.md` trap 6 updated. `PremiseCopyTests` **2 / 0**. AutoVerify
EditMode **265 / 0**, PlayMode **16 / 1** (`ShoveDoesNotUncommit`, M20 — not this milestone) —
`gkb_verify_report.json`, 2026-08-09T19:28:58Z.

## Remaining to close

| # | Status | Evidence |
|---|---|---|
| Criterion 6 (cold-start ask) | **open — Zo** | Ask one person who has not read the wiki, after they embark from a cold start reading only what the game shows: who the Gatekeepers are, and what the Hatch does |
| Playthrough path | **open — Zo** | Main menu → NAV MAP read premise → embark → lose (Hatch) → lose screen; then clear at dial 0 → clear screen |

## What shipped

- `MapStation.PremiseBrief` replaces `dormant Gate signature; gatekeepers expected`.
- `HudController.LoseFraming` / `ClearFraming` on the two terminal screens.
- `Gate` removed: NAV MAP + `Play/Upgrades.md` lock wording; `language.md` trap 6 records why.
- Play: `The-Game.md`, `Gatekeepers.md`, `A-Run.md`, `Getting-Started.md`.
- `PremiseCopyTests` — non-empty, ≤40 words, retired fragments and builder vocab absent.
- Incidental compile fix: `EnemyGatekeeper.Targeting.cs` missing `using GatekeeperBastion.Sim`
  (M30 `DecoyState` call) — blocked AutoVerify until fixed.

## Story

As a player, I want the game to tell me I am boarding a place someone else holds, so that
defending the Hatch reads as protecting my way home rather than guarding a door for no
stated reason.

## Why it matters

The rename landed in code, builder docs and the wiki in one pass. The premise did not
land anywhere a player looks. Searching all 20 pages of `../../Play/` for *board*,
*intruder* or *garrison* returned zero hits, and in play the only lines that touched the
fiction were `MapStation`'s `dormant Gate signature; gatekeepers expected` and the
`Bastion ALPHA-7` label. Neither said whose Bastion it is, why anyone is coming, or what
the Hatch is for.

**`Gate` is closed by removal.** It had three conflicting uses; none earned a table row.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.**

## Acceptance criteria

1. Before first embark, ≤40 words: Bastion held, Gatekeepers live there, Hatch is way back
   — **Met** (`MapStation.PremiseBrief`, 14 words on NAV MAP).
2. Destination panel names Bastion garrisoned — **Met** (`garrisoned` in PremiseBrief).
3. `Gate` one meaning or removed — **Met** (removed; trap 6 in `language.md`).
4. Lose screen: thrown off the Bastion — **Met** (`LoseFraming`).
5. Clear screen: stripped + out through the Hatch — **Met** (`ClearFraming`).
6. Cold-start ask — **Open** (Remaining).
7. No builder vocab / off-table terms in new lines — **Met** (`PremiseCopyTests`).

## Supporting work

- **Driven** — no new input. NAV MAP, embark, two end screens.
- **Shown** — NAV MAP premise. Owned here.
- **Reads back** — end screens. Owned here.

## Verification

Criteria 2, 4, 5 — AutoVerify EditMode green via `PremiseCopyTests` (presence + retired
terms absent, not exact wording). Criterion 3 — `Gate` fiction uses gone from MapStation and
`Play/Upgrades.md`. Criterion 6 and the four-surface playthrough remain Zo actions.

## Watch

Criterion 6 can fail while every other criterion passes. Do not soften it into "the text
explains the premise."

## Depends on / Unblocks

Depended on nothing. **Unblocks M28's first real target**: the prose this milestone wrote.
