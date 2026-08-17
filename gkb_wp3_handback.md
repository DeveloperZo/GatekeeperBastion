# WP-3 Ship handback — 2026-08-10

## Urgent items

| Item | Status | Evidence |
|---|---|---|
| GatekeepersProtocol.dll compile | **PASS** | `Library/ScriptAssemblies/GatekeepersProtocol.dll` write 10:20:06 local; last `Csc …GatekeepersProtocol.dll` in Editor.log had no `error CS`. Stale log lines for `ShipHudView.SetShortcutRows` / `SetHudRelevant` predate the current API (`IReadOnlyList` + both methods present in `ShipHudView.cs`). |
| Crit 5 author scripts gone | **PASS** | `M35Wp3Author.cs`, `M35Wp3ForceRefresh.cs`, and the transient `M35Wp3BindingsOnce.cs` deleted. `Get-ChildItem -Recurse *M35Wp3*` = empty. `[InitializeOnLoad` under `Assets/Scripts/Editor` = `AutoVerify.cs` + `SceneBuilder.cs` only. |
| Settings / QUIT (WP-2 handoff) | **PASS** | Ship `OptionsView` on station Canvas: 4 tabs, 6 row bindings (DisplayMode/Master/Music/Sfx/MouseSensitivity/DiscardReloadOnSwap), `_controlsRowsRoot` + template. QUIT TO MENU → `ShipController.QuitToMenu` → `SceneFlow.LoadMainMenu()`. |
| BindingValidator Ship | **PASS** | `gkb_bindings_report.json` `2026-08-10T15:20:11Z` — `missingCount: 0` (all build scenes). |

## Criteria 18–25 + Ship halves

| Crit | Verdict | Notes |
|---|---|---|
| 18 stations open | **LIKELY PASS** | Four `StationPanelView`s + `_view`/`_canvasRoot`/`_panels` non-zero on all four `*Station`s. Needs in-editor Interact pass. |
| 19 purchase E2E | **LIKELY PASS** | Station logic unchanged; views/templates authored. Needs play. |
| 20 EMBARK button | **LIKELY PASS** | NAV MAP content + EMBARK authored. Needs play. |
| 21 canvas inactive + Esc | **PASS (code/scene)** | Station Canvas `m_IsActive: 0`. StationTerminal Esc poll removed. `ShipController.HandleEscape` owns Esc. |
| 22 residue deleted | **PASS** | Greps 0: Quick Match, Background Music, Play Chapter, Selected Server, Key Binding. |
| 23 SplashScreenManager | **PASS** | Grep 0. |
| 24 one view / serialized / loud | **PASS** | `EnsureInstance` gone; four views; station fields wired (YAML after reflection miss). |
| 25 ShipHudView own canvas | **PASS** | `Ship HUD Canvas` + `ShipHudView`; no GraphicRaycaster on HUD root. |
| 26 Ship half | **PASS (code)** | `SetHudRelevant` state-gated in `ShipHud.ApplyRelevance`. |
| 27 Ship half | **PASS (code)** | Change-gated pushes in `ShipHud.PushHud` / `UiBind`. |
| 28 Ship half | **PASS** | Dynamic child under Ship HUD Canvas. |
| 29 Ship half | **PARTIAL** | Raycaster stripped on Ship HUD. `enableDynamicUpdate` is WP-4 asset. |
| 32 Ship half | **PASS (code/scene)** | Settings + QUIT wired; confirm Options rows in play. |
| 33 Ship profiler | **FAIL / DEFERRED** | No 60s capture filed. |
| 36 Ship half | **PASS (code)** | `DevTaintBanner.Draw` under `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` in `ShipHud.OnGUI`. |

## Do-not-touch

MainMenu.unity / Bastion.unity / Splash / SceneBuilder / MainMenu* / Options* mutators — not edited this pass (except shared `PlayerHealth.spawnPoint` marked `[OptionalBinding]` to match null-safe runtime; clears Bastion+Ship validator rows for that field).

## Play still required

Interact all four stations → one buy each at ARMORY/FAB/CREW → NAV MAP ◀/▶ + EMBARK → Esc Settings → QUIT TO MENU → MainMenu. Crit 33 capture if still charged to WP-3.
