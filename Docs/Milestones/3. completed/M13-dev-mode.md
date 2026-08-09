# M13 — Dev mode

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-04
-->

An editor/dev-build console for setting up a fight instantly: configure a loadout,
jump to a Wave, replay the one you just lost. Never present in a release build.

## Done when

**Met.** F1 lists every binding; each Dev action is a `GameController` command and
logs `[Dev]`; `DevTainted` suppresses `GameSession.Save`; Dev scripts compile out of
release behind `UNITY_EDITOR || DEVELOPMENT_BUILD`.

Play evidence in `Logs/GatekeeperBastion_2026-08-04_11-47-00.log`: `overlay open`,
`session tainted`, `GrantSalvage`, `ClearGatekeepers`, repeated `Save skipped — session
tainted by Dev mode`. Harness: AutoVerify EditMode **193 / 0**, PlayMode **10 / 0**
including `DevSessionTests` (`gkb_verify_report.json`, 2026-08-04). Criterion 4 grep
over `Assets/Scripts/Game/Dev` is empty. Criterion 3 rests on the `#if` wrap (player
build grep optional confirmation on first release cut).

## What shipped

- `Game/Dev/DevConsole.cs` + `DevCommands.cs` — F1–F9 table, absolute-GUI overlay,
  F3 digit jump, F7 loadout picker, F9 live gatekeeper readout; entire files `#if`'d out
  of release.
- `GameController.DevCommands.cs` — always-compiled command surface (`ReplayWave`,
  `JumpToWave`, `ClearGatekeepers`, `RestoreHatch`, `TogglePlayerInvulnerability`,
  `GrantSalvage`, `SetLoadout`).
- `RunSim.AbortAssault` / `SeekWave` — replay and jump without a second mutation path.
- `GameSession.NotifyDevAction` / `DevTainted` — Save no-op for the rest of the play
  session; cleared on `StartNew` / `ContinueGame`.
- `Controls.Down`/`Held` respect `DevConsole.Suppresses` so F3 digits and F7 picker
  keys do not also fire weapon slots / interact.
- SceneBuilder adds `DevConsole` to Bastion and Ship.

| key | does |
|---|---|
| F1 | toggle overlay |
| F2 | replay this Wave |
| F3 | jump to Wave N (then digit) |
| F4 | kill all live gatekeepers |
| F5 | refill Hatch and player |
| F6 | toggle player invulnerability |
| F7 | loadout picker (1/2 slot, Q/E weapon, Z/C tier, Enter) |
| F8 | grant 5,000 Salvage |
| F9 | gatekeeper-state readout |

## What it did not settle

A `[Dev]`-tagged log is still not evidence for any other milestone's play gate. F7
mutates Meta in memory only while tainted — disk stays clean. Five-Wave prologue cost
and deep start remain M14.
