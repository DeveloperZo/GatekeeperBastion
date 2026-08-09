# M0 — Playable v0 loop

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-03
-->

The first end-to-end playable: launch the game, walk the ship, buy something,
disembark, fight three Waves, die or clear, come back and spend.

## Done when

**Met.** Solo, start to death or clear, without a manual scene load.

## What shipped

- Four generated scenes — `Splash`, `MainMenu`, `Ship`, `Bastion` — built by
  `Editor/SceneBuilder.cs` and registered in build settings.
- Four ship stations: ARMORY, FABRICATION, CREW QUARTERS, NAV MAP.
- The draw-triggered fight: T pulls a Wave, three Waves, no timer.
- Three gatekeeper kinds, each defined by what it refuses to do.
- The 0–5 siphon dial driving reward and body count from one table.
- Meta upgrades with a save file at `gatekeeper_meta.json`.
- One-file-per-session logging and the `mcs`+`mono` EditMode harness (134 tests)
  plus the AutoVerify file bridge.

Detail lives in `../../Design/3-Mechanics.md` and `../../Architecture.md`. This file does
not restate them.

## What it did not settle

The arithmetic in `../../Design/4-Data.md` is unplayed. That is M1's job, and the
Wave-3 gap it exposes is M2's.
