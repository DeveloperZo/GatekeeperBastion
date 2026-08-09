# M15 — Shortcut panels

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-04
-->

Show the player what they can press. A panel on screen listing the keys that work
right now — dev keys included — and one fix to a collision on the number row.

## Done when

**Met.** One binding list feeds every input site and a bottom-left shortcut panel
(dev keys included when compiled in). Dial is `[` / `]`; the number row belongs to
weapon slots. AutoVerify EditMode **203 / 0**, PlayMode **10 / 0** including all six
`ControlsTests` (Editor.log, 2026-08-04). Criterion 3 play receipt:
`Logs/GatekeeperBastion_2026-08-04_12-20-39.log` — Prep `2`→`1` slot switches with no
dial line; separate `]` sets L1; draw at L1. Criterion 5: `KeyCode.` under `Game/`
only in `Input/Controls.cs` and `Game/Dev/`. Criterion 6 closed by the developer’s
play pass that produced that log (slots, dial, draw exercised from the panel).

## What shipped

- `Game/Input/Controls.cs` — single `{ key, alt, display, label, context }` list;
  `Down` / `Held` / `ScrollDelta` / panel row collection. Dial on `[` / `]`.
- `Game/Input/ShortcutPanel.cs` — bottom-left absolute-GUI panel; sections appear with
  context; Backspace collapses to `[?]`; DEV section from `DevCommands` when compiled in.
- Input sites wired: `FirstPersonController`, `PlayerGun`, `Interactor`,
  `GameController` (draw + `[`/`]` dial), `GameController.EndScreen`, `StationTerminal`.
- `HudController` + `ShipHud` draw the panel; Prep/Assault strips clear the left band;
  ad-hoc key hints removed. Labels teach: `1 2 3 weapons`, `T siphon draw`,
  `[ ] siphon Ln`, `E hold repair`.
- `Assets/Tests/EditMode/ControlsTests.cs` — no overlapping-context key share, ids
  resolve, dial off the number row, Prep number-row only on weapon slots, throwaway
  row surfaces.
- `AutoVerify` recovery: sentinel always wins over a stuck stage; refuses start while
  playing; idle stage auto-clears after 90s.

## What it did not settle

No options menu, no rebinding, no input-system migration. NAV MAP selector strip entry
remains M14’s. Criterion 6 contaminates whoever runs it — if a later first-time player
misses a binding, fix the panel labels, not a tutorial.
