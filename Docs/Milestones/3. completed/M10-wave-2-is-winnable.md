# M10 — Wave 2 is winnable

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-04
-->

Widen the spawn group gap so gatekeepers arrive slower than they can be killed. One
formula changed; nothing else in the game moved.

## Done when

**Met.** Dial-0 Wave 2 clears with Hatch above 25%, and the Wave's 30 bodies land
over 27–29 s. Proven in `Logs/GatekeeperBastion_2026-08-03_20-41-32.log` (e.g.
`wave 2/3 cleared — salvage 1200, hatch 284/300`; groups 1→8 span ~28.0 s).
EditMode suite green at ship (163 passed), including the rewritten gap pins.

## What shipped

- `WaveDirector.NextWave` group gap: `max(0.4, 4.2 - 0.20*waveIndex)` →
  **4.20 / 4.00 / 3.80** (was `2.6 - 0.13*wave`).
- Delivery at dial 0: **21.0 / 28.0 / 30.4 s** (was 13.0 / 17.3 / 18.7).
- Doc comment, EditMode pins, and `Design/4-Data.md` updated with the live formula so
  a later session cannot "fix" the gap back from a stale comment.

Numbers and the gatekeeper-second diagnosis live in `../../Design/3-Mechanics.md`.

## What it did not settle

Wave 3's out-delivery after this gap was M2 (shipped: `MaxGroupSize = 4`). Pacing
may feel empty at 28 s delivery; do not narrow this gap to answer that. Purchases
still do not move outcomes enough to feel — that was M11 (shipped).
