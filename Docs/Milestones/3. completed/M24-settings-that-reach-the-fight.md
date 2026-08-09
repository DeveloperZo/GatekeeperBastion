# M24 — Settings that reach the fight

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-08
-->

One settings screen, reachable from the main menu **and from inside a Run**, backed by a store
that is the only reader and writer of preference values.

## Done when

**Met (code + Path 1 EditMode).** `OptionsScreen` (absolute `GUI`, no `GUILayout`) opens from
main-menu OPTIONS, Ship `O`, and Bastion `O`; `MovementLocked` + cursor capture/restore; no
`Time.timeScale`; sensitivity on `Settings.Declarations` with live per-frame read;
`MainMenuController` options panel and FPC serialized sensitivity deleted; RESTORE DEFAULTS;
`PlayerPrefsSettingsStore` boot-bound. mcs harness **229 / 0** including five `SettingsTests`
(no skip) — 2026-08-08. Binding is `ControlId.Options` / `KeyCode.O` (Escape unavailable).

## Remaining to close

| # | Status | Evidence |
|---|---|---|
| 3–4 + Path 2 | **open** | Unity AutoVerify hung in `InitTestScene` (Editor title, 2026-08-08); stop the TestRunner / exit play mode, then write `gkb_verify_request.txt` |
| Playthrough path | **open** | Menu OPTIONS → sensitivity → embark → mid-Assault `O` → gatekeepers still move → change look speed same Run; one `[Settings]` line per commit |
| Criterion 10 byte-identical | **open** | Needs a clean AutoVerify run (suite must leave `gatekeeper_meta.json` + PlayerPrefs unchanged) |

## What shipped

- `Sim/Settings/` (store shared with M25's `DiscardReloadOnSwap` row) + `MouseSensitivity` declaration (`gkb_sensitivity`, 0.2–8, default 2).
- `Game/Flow/OptionsScreen.cs` — declaration-driven rows, RESTORE DEFAULTS, BACK; commit log on mouse-up / close.
- `Game/Flow/PlayerPrefsSettingsStore.cs` — boot bind; settings never touch the campaign save.
- `MainMenuController` OPTIONS → `OptionsScreen`; deleted `Panel.Options` / `DrawOptions` / pref constants.
- `FirstPersonController` reads `Settings.GetFloat(MouseSensitivity)` each look frame.
- `ControlId.Options` on `O` in Fight+Ship; `HudController` / `ShipHud` tick + draw; station panels yield while options is open.

## Story

As a player, I want to change how the game plays for me without quitting to the menu, so that
a preference is something I set when I notice it rather than something I put up with.

## Why it matters

The old main-menu options panel wrote `PlayerPrefs` directly and was read once in
`FirstPersonController.Awake`, so a mid-session change did nothing and the screen was
unreachable in a Run. M15 cut a held-`Tab` overlay as a settings screen in disguise. M25
needed a home for `DiscardReloadOnSwap`.

## Acceptance criteria

1. One options screen from menu, Ship, Bastion — **Met** (`OptionsScreen` + three callers).
2. Absolute `GUI`, no `GUILayout` — **Met** (`OptionsScreen.cs`).
3. Open locks movement and frees cursor; close restores — **Met** in code; Path 2 play open.
4. Sim keeps running; no `Time.timeScale` — **Met** in code; Path 2 play open.
5. Store in `Sim/` behind `ISettingsStore`; adapter in `Game/` — **Met** (harness, no skip).
6. Declaration row / `SettingId` enum — **Met** (`SettingIdsAreUnique`).
7. Sensitivity migrated; old menu panel + serialized field deleted — **Met**.
8. Live per-frame read — **Met** (`FirstPersonController.Update`).
9. RESTORE DEFAULTS — **Met** (`RestoreDefaultsResetsAll`).
10. Persist outside campaign save; Dev-tainted still writes — **Met** in code; byte-identical open.
11. One row per `Declarations` entry — **Met** (sensitivity + M25 discard).

## Supporting work

- **Driven** — menu OPTIONS; `O` on Ship/Bastion (`ControlId.Options`).
- **Shown** — the screen; shortcut panel lists `O`.
- **Reads back** — `[Settings]` log line per commit (slider mouse-up / close / restore / bool toggle).

## Verification

Path 1 green — see Done when. Path 2 / playthrough still open in Remaining.

## Watch

- Do not fill the screen with greyed placeholders.
- No key rebinding here.
- Escape must not close options (station panel collision).

## Depends on

Nothing. **M25 already shipped** on this store.
