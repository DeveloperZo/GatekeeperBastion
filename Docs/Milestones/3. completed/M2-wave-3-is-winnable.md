# M2 — Wave 3 is winnable

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-04
-->

Make the last Wave beatable by an invested player. Wave 3 is the only Wave carrying
Rams.

## Done when

**Met.** An invested run clears Wave 3 with both Rams killed and Hatch above 0.
Proven in `Logs/GatekeeperBastion_2026-08-04_07-51-40.log`:

- `Secondary=MK-1 Sidearm T3` (Tier-3 investment; Primary empty — pre-Primary purchase)
- two `killed a Ram` lines
- `wave 3/3 cleared — salvage 2723, hatch 216/300`
- `BASTION STRIPPED — all 3 waves cleared`

Unupgraded / lightly upgraded runs still die at or before Wave 3 (Wave-2 spawn points in
`Logs/GatekeeperBastion_2026-08-03_20-41-32.log`). Ceiling moved, floor did not.

## What shipped

- Measured failure after M10 (`20-41-32`): six dial-0 Wave-3 attempts all failed to
  **out-delivery** — Defaults flooded the Hatch; Rams rarely reached the door.
- One lever: `WaveDirector.MaxGroupSize = 4` so `groupSize` is **3 / 4 / 4** (was
  3 / 4 / 5). Wave 3 delivery **30.4s → 38.0s**. Gap, counts, HP, Ram counts
  untouched.
- EditMode pins and `Design/4-Data.md` updated with the cap.

## What it did not settle

The gate clear was **siphon L1**, not L0 — still an invested clear of Wave 3 with
both Rams dead. The gatekeeper-second **3.43** figure in `Design/3-Mechanics.md` was not
replaced from this Run (criterion 3); re-derive when a dial-0 clear is logged if it
matters. Purchases / Primaries as progression remain M11.
