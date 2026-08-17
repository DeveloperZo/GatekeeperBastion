# M35 — One look, drawn from one place

<!--
Status: completed, Aug 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-11
-->

**Four authored scenes, one theme, no UI generators.** Boot order is Splash → MainMenu →
Ship → Bastion. Splash is a standard splash: brief, branded, auto-advancing. MainMenu is
the customized Shift campaign menu — Gatekeeper's Protocol's, not Shift's demo. Ship's four
station terminals each open a working, data-bound panel. The HUD is Shift's shell driven
by our values, split for performance, and the menu is reachable back from play. Every
element in every canvas earns its place or is deleted. The scene files are the source of
truth for UI; the three editor mutators that regenerate them are deleted.

This is one milestone at Zo's direction, larger than the grain rule wants. The work
packages below are the honest split — exclusive file ownership, strict order, per-package
checkpoints, briefable to separate agents; work stops at any checkpoint without stranding
the game.

## Closed

**Shipped 2026-08-11 on Zo's close.** Boot path, authored scenes, deleted Shift regenerators,
menu/HUD/pause/quit-to-menu, and the station *drive* path (open → live data → purchase or
EMBARK) are in. Evidence at close: `gkb_bindings_report.json`
`2026-08-11T02:00:47Z` `missingCount:0` across Splash/MainMenu/Ship/Bastion;
`gkb_verify_report.json` `2026-08-11T04:03:46Z` EditMode 295/0, PlayMode 22/1
(`ShoveDoesNotUncommit` — combat latch, not UI).

**Gate softens (named, not silent):**

| Criterion | Soften |
|---|---|
| 5 | `InitializeOnLoad` also matches `StripSettingsToStations.cs` and `RepairStationPanels.cs` — sentinel-only station-shell tools kept for the deferred ship-terminal milestone; deleted when that milestone closes. |
| 18–25 (panel *detail*) | Drive path closed; Settings-shell strip, row chrome, readability over the live view, and related polish move to a later ship-terminal UI milestone (not filed yet). |
| 41 | Screenshot set not filed; visual QA rides with that follow-up. |
| 42 | Closed by Zo's 2026-08-11 verdict on this file. |

## Story

As the player, I want the game to boot like a finished game — splash, menu, ship,
fight, and a way back to the menu — with every screen working and nothing left over
from someone else's demo, so that what I see is Gatekeeper's Protocol and everything I
can click does something.

## Why it matters

The 2026-08-10 audit found the front end non-functional, not merely unpolished:

- The menu scene (today misnamed `Splash.unity`) contains **none of the four Button
  objects** `MainMenuView` binds (`MainMenuView.cs:25-31`: Continue / Start New /
  Options / Quit — the only object named `Continue` is a hotkey-hint label in the
  disabled demo splash branch). There is no working path from the menu to the Ship,
  to Options, or to quitting the game.
- All four Ship station panels have an **empty `Content`** — `ShiftShipUiImport.cs`
  `ClearContent` (:539-551) deleted the very templates `StationPanelView` needs
  (`StationPanelView.cs:27-33`). No station renders a single row; EMBARK exists in
  code (`MapStation.cs:87-98`) but not as a button, so **the game has no UI path to
  the Bastion**.
- The Shift demo's Quick Match tile, Friends tab, Party bar, Play Story tile, server
  browser modal and placeholder soldier profile are all still **active** in the menu
  scene. The title "Gatekeeper's Protocol" appears nowhere in the front end.
- `gkb_verify_report.json` (2026-08-10T04:04Z) is `status: "error", stage: "stuck"`
  ("TestRunner never finished stage 'editmode'") — nothing above was ever verified,
  and the verify loop itself is broken.
- The scenes are one sentinel file away from being wiped: `ShiftFrontEndMutate`
  regenerates `Splash.unity` from the demo on a 1 Hz poll (`:43-52`, `:155-196`) —
  the hand-edits-are-lost failure this milestone's first version was written against.

**Leverage tiers (`../../Working-Rules.md`): #10, structure** for the scene/source
changes, and **#6, information flow** for the new player-facing surfaces (menu
readouts, station rows, HUD placement) — named so the higher-tier moves are argued
here, not smuggled.

The target shape:

| Scene | Role | Contents |
|---|---|---|
| `Splash.unity` (new, small) | Standard splash. Slot 0. | Black backdrop, "GATEKEEPER BASTION" title in the Shift theme, fade in/hold/out, async-loads MainMenu. Skippable. |
| `MainMenu.unity` (today's `Splash.unity`, renamed) | The campaign menu. | Shift menu Canvas, customized: Home = Continue / Start New / Options / Quit + campaign strip; Settings panel; Exit and Overwrite modals; title branding. Demo residue deleted. |
| `Ship.unity` | Walkable hub. | Station-only UI canvas, inactive until a terminal opens. Four authored station panels with bound content. Small always-on Ship HUD canvas. Esc opens Settings through the same canvas; Settings carries QUIT TO MENU. |
| `Bastion.unity` | The fight. | Shift HUD shell, values bound, dynamic elements on their own canvas. Shift pause canvas wired, non-blocking, with QUIT TO MENU. End screens wired. |

The old `MainMenu.unity` (a camera, an EventSystem and a dangling prefab instance) is
deleted. Scene names then say what they contain.

**Render mode:** the menu canvas returns to **Screen Space – Camera** as Shift ships
it (the demo's `Background` and `BlurManager` were built against it;
`UiCanvasGuard.cs:7-10` says Overlay breaks them — the current Overlay state is
`ShiftFrontEndMutate.ForceOverlay` residue). Ship and Bastion HUD canvases stay
Overlay. If blur or background misrender under Screen Space – Camera in the editor
check, that is a finding to bring back here, not a licence to improvise.

**The Escape contract** (new in rev 3 — Esc currently has four uncoordinated
consumers; `Controls.cs:139-141` documents the collision). Each scene has exactly
one Esc dispatcher, and nothing else polls Esc:

- **MainMenu:** Shift's modal hotkeys as shipped; no dispatcher needed.
- **Ship — `ShipController`:** priority per press: open modal → close it; station
  panel open → `StationTerminal.Close()`; Settings open → close Settings;
  otherwise → activate the station canvas and open Settings.
  `StationTerminal.LateUpdate`'s own Esc poll and `OptionsScreen.Tick`'s Esc-close
  are **deleted**; the dispatcher calls their close methods.
- **Bastion — `HudController`:** priority per press: open modal → close it;
  Settings open → close Settings; pause open → resume; end screen showing → the
  existing Confirm/ReturnToShip binding; otherwise → open the pause overlay and
  free the cursor (restore cursor and lock state on resume per the current phase).
  The scene-side hotkey-27 components on the pause canvas are deleted — pause
  open/close is code-driven, not scene-hotkey-driven (today the shipped canvas has
  no `Window In` wiring at all; Esc-opens-pause is new code, and criterion 31
  specifies it).
- The `Options` = "O" binding is **deleted** from `Controls.cs`; Options is reached
  via the menu button, Ship Esc, or Bastion pause. The ESC row in the binding
  table is updated so the shortcut strip (criterion 35) stays truthful.

## What Shift provides — measured 2026-08-10

Still the working reference: the panel idiom (`<Panel>` = `CanvasGroup` + Animator
over `Shortcut Keys` / `Hotkeys` / `Content`; our content goes under `Content` and
nowhere else), `MainPanelManager.OpenPanel(name)` routing, the bar recipe (sliced
`Filled` fillRect inside a `Mask` with `m_ShowMaskGraphic: 0`), `Modal Window/Exit`
+ `ModalWindowManager` + `BlurManager`, `QualityManager` for resolution/vsync,
and the theme in `Resources/Shift UI Manager.asset` (`UIManagerImage` /
`UIManagerText` on every element; Rajdhani SemiBold/Medium at 25 / 29.5 / 30 / 36 /
40; canvas 1920×1280 match 0.5; the fill/border alphas and the cyan/negative accents
as Shift ships them). Do not tidy these numbers; the art was drawn against them.

Prefabs this milestone draws on: `Main Button` (menu + row actions + pause/quit
buttons + NAV MAP ◀/▶), `Chapter Button` (rows that carry art), `Panel Button`
(MainMenu tabs only), `Horizontal Selector` and `Slider` and `Switch` (Options
rows), `Keyboard Shortcut Key` (shortcut-strip keycaps), `Icon Button` (armory
wheel spokes), `Modal Window/Exit` (confirm modals), `Scrollbar` (armory catalog).

## Acceptance criteria

### A — boot flow: a splash, then a menu

1. Pressing Play from `Splash.unity` (or launching a build) shows a standard splash —
   "GATEKEEPER BASTION" in Rajdhani over black, fade in ≤ 0.5 s, hold, fade out,
   total ≤ 4 s — then lands in MainMenu with no input required. Any key or click
   after the first displayed frame skips straight to MainMenu. (Skippability is
   deliberately unadvertised — convention, not a discoverability gap; noted here so
   the silence is a decision.)
2. The splash contains no press-any-key gate, no loading bar, and no sprite or logo
   from the Shift package. It is our title card only.
3. Scenes are re-cut in one commit: current `MainMenu.unity` **deleted**, current
   `Splash.unity` **renamed** to `MainMenu.unity` (file + `.meta` in one move), new
   lightweight `Splash.unity` **created**. `EditorBuildSettings` order is Splash,
   MainMenu, Ship, Bastion. Review step (not a bare grep — log tags and `SceneFlow`'s
   own constant definitions legitimately match): every scene-name literal outside
   `SceneFlow` is inspected in the diff and justified or removed (including the
   stale three-scene error text at `SceneFlow.cs:126` and MapStation's embark-fail
   copy).

### B — the generators are gone; scenes are source

4. **Deleted, with `.meta` files — deleted, not renamed:**
   `ShiftFrontEndMutate.cs`, `ShiftShipUiImport.cs`, `ShiftBastionUiImport.cs`.
   `grep -rn "ShiftFrontEndMutate\|ShiftShipUiImport\|ShiftBastionUiImport" Assets/Scripts`
   returns nothing, and the reviewer confirms in the diff that no equivalent
   scene-mutating code was added under a new name. The prose is the criterion; the
   grep is the smoke test. (This scope is all of `Assets/Scripts` — the last dodge
   was a generator moved into `Editor/Tools` to sit outside a Ui-folder grep, by its
   own comment at `ShiftShipUiImport.cs:28-30`.)
5. No editor code creates, destroys, reparents or rewires objects under any UI
   canvas root or authored gameplay host, on any trigger (menu item, sentinel,
   `InitializeOnLoad`, `DidReloadScripts`, asset postprocessor, scene-saved
   callback). `grep -rn "InitializeOnLoad" Assets/Scripts/Editor` matches only
   `AutoVerify.cs` and `SceneBuilder.cs`, whose sentinel loops (verify requests;
   geometry builds) are retained and blind to authored content per criterion 6.
6. `SceneBuilder`'s owned, rebuildable set shrinks to content that carries **no
   serialized cross-references**: the `Environment` root, the `SpawnPoints` root,
   the `Directional Light`, and the NavMesh asset — rebuilt **in place, in the open
   scene**, never via `NewScene`; `CaptureUiRoots` / `RestoreUiRoots` are deleted.
   Everything that holds or receives serialized references becomes authored,
   persistent scene content that `SceneBuilder` never touches: the station consoles
   and their `*Station` components, the player rig, `ShipController`,
   `GameSystems`, the `EventSystem`, and every UI canvas. Migration is one-time in
   WP-1: run the current Build All once, commit the result, then delete those
   creation paths (`MakeStationConsole`, the player-rig build, the controller
   `AddComponent`s) from `SceneBuilder`. (This is what makes criterion 24's
   serialized terminal→view references survivable — the previous design had
   `SceneBuilder` destroying and recreating the very objects that hold them.)
7. From a clean committed tree, run Build All, then Build All again.
   `git diff -U0 -- Assets/Scenes` after each run contains no `+`/`-` line
   belonging to a UI object block or an authored gameplay host (spot-check: no
   diff hunk contains `Canvas`, `UIManager`, `TextMeshPro`, `Panel`, `Button`,
   `Modal`, `Station`, `ShipController`). Whole-file byte identity is not
   required — rebuilt geometry may reserialize — but authored blocks are
   untouched.
8. Docs are amended before the milestone closes — each in the commit of the WP
   that made the change where feasible, swept by WP-5 otherwise:
   `../../Architecture.md`, `../../Decisions.md` (scenes are authored source for
   UI and gameplay hosts; `SceneBuilder` owns environment/spawns/NavMesh only; the
   deleted mutators named so they are not re-invented; the pause decision's
   criterion anchor re-pointed at criterion 31 here), `../../../AGENTS.md` (the
   "scenes are generated, hand-edits are lost" warning corrected),
   `../../Roadmap.md` (the M35 row — which still describes the abandoned
   prefab-generator scope — plus the stale Current/Recommended-next strategy
   paragraph, and a change-log entry for this gate rewrite), and
   `../3. completed/M36-the-hatch-is-always-on.md` (it says "M35 assembles the front
   end from prefabs" and names `Theme.cs`, which no longer exists — both corrected
   to this scope).

### C — the menu is Gatekeeper's Protocol's

9. The Home panel offers exactly four actions, as Shift `Main Button` instances
   under `Home/Content` (a vertical layout group, so hiding CONTINUE reflows the
   list): **CONTINUE** (only when a save exists — `GameSession`'s existing save
   check; absent otherwise, with START NEW in the top slot), **START NEW**,
   **OPTIONS**, **QUIT**. Clicking CONTINUE or START NEW loads the Ship. This is
   the player-action criterion: from the menu, a mouse click reaches the Ship
   scene.
10. The campaign strip (banked salvage, runs, deepest wave — the readout
    `MainMenuController.RefreshMenu` already computes) renders next to CONTINUE
    from a serialized label. It earns its place: it is what makes CONTINUE
    informed.
11. START NEW over an existing save opens an overwrite-confirm modal (a duplicate
    of the Exit modal object named `Confirm Overwrite`, own `ModalWindowManager`,
    `BlurManager` on the parent). Confirm proceeds and overwrites; cancel returns
    to the menu unchanged.
12. QUIT opens the Exit modal; confirm calls `Application.Quit()` in a build and
    stops Play mode under `#if UNITY_EDITOR` (this guard is **created** — none
    exists today, `MainMenuController.cs:28`).
13. OPTIONS opens Shift's Settings panel. The unbound `OptionsView` component that
    shadows it today is deleted from the scene. Every setting the game exposes
    (the `SettingId` set — master/music/sfx volume, display mode, reload-swap
    discard, sensitivity; confirm the full set against `Sim/Settings/SettingId.cs`
    when implementing) appears as a Shift `Horizontal Selector`, `Slider` or
    `Switch` under the Settings sub-tabs, bound through `OptionsView` **serialized
    references**. Sub-tab order must match the `SettingCategory` enum —
    `OnResetPage` casts tab index to category (`OptionsScreen.cs:164-166`).
    `QualityManager` keeps resolution / vsync only; audio routes exclusively
    through our `Settings`/`AudioBuses` path, never Shift's mixer (two audio
    systems is how volume settings stop persisting). Demo-only settings rows are
    deleted. The Controls sub-tab is **generated at open** from
    `Controls.CollectPanelRows` by cloning a serialized row template (keycap +
    label) — hand-authored rows would drift from the single-source table
    (`Controls.cs:7-9`); no rebind UI in this milestone, and the `Key Binding`
    modal is deleted with the other demo modals. Changing master volume and
    display mode works and persists; the 15 s display-mode revert flow still
    functions.
14. `OptionsScreen` acquires its view by registration: `OptionsView` registers
    itself in `Awake` (canvas activation always precedes `Open`, so registration
    exists by first use), `OptionsScreen.Open` routes through the registered
    instance, which calls `OpenPanel("Settings")` on its scene's
    `MainPanelManager`; one error is logged if a scene ticks `OptionsScreen` with
    no registered view. The `FindFirstObjectByType` shadow-grab is deleted. Every
    scene that ticks `OptionsScreen` carries a registered `OptionsView` —
    including the Bastion (its pause Settings rows are OptionsView-bound,
    criterion 31), so no scene boots into a designed-in error path.
15. The title **GATEKEEPER BASTION** is visible on the menu (Top Panel, where the
    Shift logo sits today); the Shift logo sprite and `UIManagerGameLogo`
    component are removed. No Shift or Michsky branding text or logo renders
    anywhere in play.
16. Demo residue is **deleted** from `MainMenu.unity` — deleted, not deactivated,
    not renamed: `Placeholder Model`, `Profile` ("Welcome" / "Soldier Name"),
    `Play Story`, `Quick Match`, `Friends Tab`, the `Bottom Panel` Party bar
    (`You`, `Member 1-3`), modals `Play Chapter` / `Selected Server` /
    `Key Binding`, the entire disabled `Splash Screen` branch (Login, Sign Up,
    Press Any Key, Loading), the duplicate root `EventSystem`, any missing-script
    components left by WP-1's class deletions, **and the `SplashScreenManager`
    component on `Menu Manager`** (with the splash branch gone its `splashScreen`
    reference would be destroyed and its unguarded `OnEnable` would NRE — the
    exact defect criterion 23 removes from Ship; `MainPanelManager` handles
    first-panel entry on its own).
    Smoke test: `grep -c "Quick Match\|Friends\|Soldier Name\|Play Story\|Selected Server\|Squad" Assets/Scenes/MainMenu.unity`
    returns 0. The full list above is checked in the diff, not by grep — serialized
    property names (`enableLoginScreen`, `m_Placeholder`) make broader greps
    false-positive.
17. All menu wiring is by **serialized reference**. `MainMenuView`'s
    name-resolution fallback (`Resolve()` via `UiBind.Find`, whose
    first-match-by-name already bound the wrong `Home` — the scene has two — and a
    non-modal `Exit` — the scene has seven) is deleted; a null serialized
    reference logs one error naming the field at scene load instead of silently
    no-opping.

### D — every Ship terminal works

18. Walking to each of the four consoles and pressing Interact opens that
    station's panel with live data: **ARMORY** (slot toggle, equipped-weapon
    stats, wheel, priced catalog rows), **FABRICATION** (hatch armor track,
    equipment slot, repair rig, decoy payload rows with reason lines),
    **CREW QUARTERS** (four attribute tracks with level, cost, now→next effect),
    **NAV MAP** (destination premise, start-wave selector clamped to
    `DeepestWaveReached`, warnings, EMBARK).
19. In ARMORY, FABRICATION and CREW QUARTERS, one purchase completes end-to-end
    from a mouse click: the row button calls into `ShipCommands`, salvage and
    level change, and the row and header repaint the new values **without closing
    the panel**. Refusals (insufficient salvage, gate not met) render their reason
    line; nothing is greyed out without a reason (`../../Play/The-Ship.md`).
    NAV MAP sells nothing; its actions are the selector and EMBARK.
20. **EMBARK is a button.** Clicking it on NAV MAP sets the pending start wave and
    loads the Bastion. The wave selector stays MapStation's existing ◀/▶
    `AddButton` clones of the standard `_buttonPrefab`, labelled ◀ and ▶ — no new
    template, no controller change. (Rejected: Shift's `Horizontal Selector`
    prefab — its item/index model would force a MapStation redesign. Rejected in
    rev 3: restyling the arrows as `Icon Button` — the template contract has no
    icon-button template and one criterion is not worth a seventh template.)
21. The station canvas is **inactive while walking**. Opening a terminal activates
    it and calls `OpenPanel(<station>)`; closing (close button, or Esc via the
    dispatcher) plays the panel out, waits for the panel-out animation state (or a
    0.3 s cap, whichever first), deactivates the canvas, and returns movement.
    Esc on the Ship follows **the Escape contract** above, dispatched solely by
    `ShipController` — `StationTerminal`'s own Esc poll is deleted. Esc while
    walking opens Settings through the same activation path; Settings on the Ship
    carries QUIT TO MENU (criterion 32).
22. Deleted from `Ship.unity` — deleted, not deactivated, and confirmed in the
    diff: the top `Panel Button` tab bar (stations are entered at consoles, not by
    tab; tabs bypass `StationTerminal`'s state), the demo `Background`, the
    `Background Music` host, the hidden campaign `Home` panel and its button,
    modals other than `Exit`, and the leftover `Quick Match` string's object.
23. The Ship's `SplashScreenManager` (destroyed `splashScreen` reference, NREs on
    every load) is removed from `Ship.unity`.
24. One panel-view instance per station panel, wired by serialized reference from
    its `StationTerminal` (the singleton `StationPanelView.EnsureInstance` /
    `UiBind.First` path is deleted). Missing bindings log one error naming the
    station and field; the silent `if (_view != null)` skip
    (`StationTerminal.cs:99`) is replaced by that error. (These references
    survive Build All because criterion 6 moved the consoles out of
    `SceneBuilder`'s rebuild set.)
25. The Ship HUD strip (crosshair, interact prompt, salvage, objective line)
    renders from an authored `ShipHudView` on its own small canvas. Walking up to
    a console shows the interact prompt — this is how a player discovers the
    terminals (Shown).

### E — the HUD is performant, and play can reach the menu

26. No per-frame `SetActive` toggling of HUD roots — on either scene. Phase and
    visibility transitions (Prep/Assault/end screens, panel show/hide, Ship
    prompt/relevance) run on state-change events; per-frame code writes values
    only. The `HideAll()`-then-`Show()` pattern in `HudController.PushHud` /
    `BastionHudView` / `EndScreenView` and the per-frame `SetVisible` toggling in
    `ShipHud.PushHud` (`ShipHud.cs:56-61`) are gone.
27. Every view setter is change-gated at the `UiBind` choke point: equal values do
    not touch `TMP_Text.text`, slider values, or `CanvasGroup` fields. Per-frame
    number formatting allocates nothing in steady state (`SetText` char-buffer
    overloads or cached strings; the `$"…"` interpolations, `Substring`,
    `ToString` and LINQ `ToArray` in the per-frame push paths are removed —
    `HudController.cs:166-375`, `HudController.Panels.cs:60-100`,
    `ShipHud.cs:64-68`, `ShortcutPanelView.cs:57`, `ValueAnimator.cs:119-121`,
    which also gains an early-out when display equals target).
28. Dynamic HUD elements (bars, timer/wave text, weapon block, shortcut strip,
    vignette) live on a **child canvas** separate from static frame art, in both
    `Ship.unity` and `Bastion.unity`, so a changing value re-batches dozens of
    graphics, not the full 719-image (Bastion) / 1060-image (Ship) canvas. (Rule:
    Unity's "Optimizing Unity UI" — one dirty Graphic re-analyzes its whole
    Canvas; sub-canvases isolate children.)
29. `Shift UI Manager.asset` has `enableDynamicUpdate` **off** — the field is
    `[HideInInspector]`, so this is a debug-inspector or YAML edit, not a code
    change (790 themed components in Bastion, 1157 in Ship each run `LateUpdate`
    per frame when it is on). `GraphicRaycaster` is removed from non-interactive
    HUD canvases and `Raycast Target` is off on HUD graphics, in both scenes.
30. The remaining view bindings exist in the scenes with serialized references:
    `BastionHudView` (every field), `ShortcutPanelView`, `EndScreenView`. Dying
    or finishing a Run shows the end screen with its numbers (today both render
    nothing — every field is null). End screens return to the Ship, as the loop
    already does (`GameController.EndScreen.cs:114`); the menu is one pause away
    (criterion 32), not an end-screen action.
31. Pause on the Bastion is Shift's pause canvas, **wired by the Esc dispatcher**
    (the shipped canvas has no `Window In` wiring — opening it is new code in
    `HudController`): Esc plays `Window In` / `BlurInAnim()` and frees the
    cursor; resume reverses both; it must not touch `Time.timeScale` and must
    not disable the game canvas (`../../Decisions.md` — re-anchor that decision
    to this criterion). Gatekeepers keep moving behind the overlay. The canvas
    is pruned to exactly: RESUME (its dangling persistent call fixed), the
    Settings sub-panel **rebound to OptionsView rows per criteria 13-14** (the
    shipped Quality-Manager-driven demo rows — Language, Field of view, Vehicle
    sensitivity, UI Scale, Enable tutorials — are deleted; two audio systems is
    how volume stops persisting), and QUIT TO MENU with its already-wired
    confirm modal. Deleted: `Load Last Checkpoint`, the pause `Key Binding`
    modal, the duplicate `Event System` child, the dangling hotkey component,
    and the demo `Background` if it occludes the fight (world visibility behind
    the overlay is the criterion; blur is the idiom).
32. **Quit to menu exists, and it saves.** On the Bastion, QUIT TO MENU on the
    pause canvas opens its confirm modal ("the run ends here"); confirm calls
    `GameSession.Save()` then `SceneFlow.LoadMainMenu()` — the same persistence
    the existing `OnApplicationQuit` path already performs mid-run
    (`GameController.cs:409-412`; salvage banks into `MetaState` live during a
    run, so a no-save quit would be a rollback exploit, not parity). On the
    Ship, the Settings panel carries QUIT TO MENU and loads the menu directly
    (state is already saved at purchase time; no confirm). Because the save
    runs, the campaign strip and CONTINUE agree after quitting — CONTINUE
    resumes on the Ship with exactly the state the strip shows. Player-action
    criterion: from an Assault, Esc → QUIT TO MENU → confirm lands on the menu,
    and CONTINUE then resumes on the Ship.
33. Profiler evidence, 60 s development-build captures — Bastion in Prep and in
    Assault, Ship walking the hub — filed in `../../GatekeeperZo Notes/`:
    (a) deep-profile shows **no GC.Alloc rows attributed to `Game/Ui`,
    `HudController`, `ShipHud` or `UiBind` methods** in steady state (global
    0 B/frame is not the gate — other systems and dev-build overhead allocate);
    (b) the static HUD canvas records **zero** `Canvas.BuildBatch` over the
    capture, and the dynamic canvas rebuilds only on frames where a displayed
    value changed (the 1 Hz timer tick is the expected floor during quiet
    Assault). Menu-idle canvas traffic is **not** gated — Shift's Background and
    fade Animators legitimately animate there. Absolute ms targets are not set;
    these captures are the baseline.
34. HUD element placement on Bastion's **1920×1280 / match 0.5** canvas, by
    **anchor / parent / rect** moves (Shift's sliced bar fill recipe untouched),
    to the segmented fight HUD. Every figure here is a **canvas unit**, not a
    screen pixel — the 24-unit inset is the same inset at every resolution.

    | Host | Anchor = pivot | Position | Size |
    |---|---|---|---|
    | `Hud Profile` | top-left | (24, −24) | 280 × 72 |
    | `Hud Score` | top-right | (−24, −24) | 320 × 72 |
    | `Hud Vitals` | bottom-left | (24, 24) | 360 × 96 |
    | `Hud Weapon` | bottom-right | (−24, 24) | 360 × 96, **fixed** |
    | `Hud Msg General` | (0.5, 1) | (0, −120) | ≤ 720 wide |
    | `Hud Msg Player` | (0, 0.5) | (24, derived) | 360 × 56 |
    | `Hud Drawer Left` | (0,0)–(0,1) stretch | x = +24 | 400 wide |
    | `Hud Drawer Right` | (1,0)–(1,1) stretch | x = −24 | 400 wide |

    Chrome for every card and drawer is a clone of `Active Mission/Shadow` —
    Shift ships that object **inactive**, and a clone left inactive is the
    blank-card failure. After clone, `UIManagerImage` is set to
    `ColorType.Background` with `keepAlphaValue` off so the theme fill
    (α ≈ 0.94) reads as a card; Primary + keepAlpha at α ≈ 0.04 is the
    ghost-pill failure. Contents: profile = LV + EXP; score = Banked /
    Run · Siphon · Wave; vitals = HEALTH + STAMINA only, from the existing
    `Armor & Health` rows; weapon = name + ammo + an **always-on clip bar**
    filled by `clipRemaining / capacity`, replaced while reloading by a
    RELOADING label over `ReloadProgress`. The weapon card's third row is
    reserved space: its contents show and hide, the card never resizes.

    Each drawer stretches a band inset 120 from the top and 144 from the bottom;
    the visible card inside that band is capped at 640 tall and centred in it, so
    no drawer covers a corner card. Left = controls + hatch (BKSP), right =
    mission / prep detail (TAB). Both closed by default, and closed means
    deactivated — nothing peeks inside the inset. The right drawer auto-opens
    **once per Run**, at the first Prep; the latch re-arms when a Run enters
    `Docked`.

    `Hud Msg Player`'s anchored Y is the one RectTransform value a view may
    write: `0` while the left drawer is closed, `drawerTop + 24 +
    toastHalfHeight` while it is open, read from the live drawer rect so a
    drawer resize cannot overlap the lane.

    Quiet Prep is the four cards and the crosshair, nothing else. Retired to
    inactive or into a drawer: the floating siphon / salvage / Time Wave lines,
    the always-on `Active Mission` panel, the mid-left shortcut strip, the
    standalone bottom-right reload block, `Armor & Health`, `Player Status`,
    `Weapon Block`. Crosshair and interact prompt centre; vignette and
    end/emergency hosts unchanged. Hatch leaves the always-on vitals card
    (drawer plus message lane); M36 puts Hatch back on the always-on surface as its
    own top-centre host.

    Every visible HUD pixel stays ≥24 **canvas units** from any screen edge — no
    clipped slider chrome, no label hanging past a card. Verified against the
    criterion-41 screenshots at both capture resolutions.

### F — dev surfaces are polished

35. The shortcut strip is built from Shift parts: keycap sprites from
    `Keyboard Shortcut Key` + Rajdhani labels through `UIManagerText`, pooled
    rows fed by a structured row API (key label + description + section —
    replacing the raw string lines and the 7.2 px/char width guess; the API
    change is WP-1's so both scenes consume it). DEV entries appear only in dev
    builds, visually separated (Negative accent), not interleaved.
36. `DevConsole` stays IMGUI and dev-only (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
    — a tool, not product UI, outside the theme accounting. The dead legacy
    `ShortcutPanel.Draw` IMGUI path is deleted. `DevTaintBanner` gains its two
    call sites (it has zero today): `HudController` on the Bastion (WP-4) and
    `ShipHud` on the Ship (WP-3), so dev-tainted sessions show their banner in
    dev builds and none of it exists in release.

### G — one look, and it survives being looked at

37. `grep -rn "guid: 0000000000000000f000000000000000" Assets/Scenes` returns
    zero (extend to `Assets/Prefabs` if that folder comes to exist) — no Unity
    built-in UI sprite anywhere. Each scene has 2-3 today; run this first, it
    fails loudest.
38. Every `Image` in authored UI carries `UIManagerImage`, every `TextMeshProUGUI`
    carries `UIManagerText`;
    `grep -rn "new Color\|Color32(\|Color\.\(red\|green\|blue\|cyan\|yellow\|magenta\|white\|black\)" Assets/Scripts/Game/Ui Assets/Scripts/Game/Bastion Assets/Scripts/Game/Ship Assets/Scripts/Game/Flow`
    returns nothing (world-space FX colors in `Game/Combat/FxColors.cs` and
    dev-only IMGUI in `Game/Dev` are out of scope on purpose); all UI text is
    Rajdhani at 25 / 29.5 / 30 / 36 / 40 — checked by grepping the scene files
    for `m_fontSize` values and the two Rajdhani font asset guids.
39. `grep -rn "Ellipsis" Assets/Scripts` returns nothing — labels wrap or their
    container fits them — and no authored `RectTransform` is left at the default
    `sizeDelta 100×100` with centred anchors.
40. **Binding validation is headless.** **Create**
    `Editor/Tools/BindingValidator.cs`: a reflection-based editor walk (menu item
    + batch entry) over the four scenes that reports every null
    `[SerializeField]` `UnityEngine.Object` field on non-vendor MonoBehaviours,
    excluding fields marked with a new `[OptionalBinding]` attribute (views
    annotate their legitimately-null fields, e.g. `OptionsRowBinding`'s unused
    widget slots). Reflection-based so it runs before any view changes — WP-1's
    first run produces the wiring worksheet, and the final run must report
    **zero missing bindings across all four scenes** (owned by WP-5's
    close-out). Results go to `gkb_bindings_report.json` — a separate file, so
    `AutoVerify`'s fixed report schema is untouched. Durable tooling with a
    stated multiplier: every missed wire across the ~200 serialized references
    this milestone authors — and every future scene edit — becomes a headless
    failure instead of a mid-playthrough discovery. The verify pipeline itself
    must be green first: the current `stuck` editmode state is diagnosed and
    fixed in WP-1 (cause unknown — start from `gkb_m35_autoverify.log` at the
    repo root).
41. Screenshots at 1920×1080 and 1280×720, filed in `../../GatekeeperZo Notes/`:
    splash, menu, options, overwrite modal, all four station panels, Ship HUD at
    a console, Bastion HUD in Prep and in Assault, pause overlay, quit-to-menu
    confirm, both end screens — fifteen per resolution. Produced at WP-5's
    close-out.
42. Zo plays one Run from a cold boot — splash → menu → START NEW → ship (one
    purchase at ARMORY, FABRICATION, CREW QUARTERS) → NAV MAP → EMBARK → fight →
    pause and resume once → death or extraction → end screen → back on the Ship →
    Esc → QUIT TO MENU → CONTINUE with the campaign strip resumes on the Ship.
    Zo writes the verdict on this file. The greps and the validator are
    necessary; this row is what closes the milestone.

## Implementation

Five work packages, **strictly sequential** (WP-1 → WP-2 → WP-3 → WP-4 → WP-5),
each briefable to one agent. Rev 2 claimed WP-2/3/4 were parallelizable; review
showed three real seams (the Options row shapes WP-3/4 instantiate are defined by
WP-2; the shortcut-row API WP-3/4 consume is defined in WP-1; scene-side settings
content depends on WP-2's row design), so the order is now strict and the seams are
explicit hand-offs. Rules:

- **Exclusive ownership.** Each WP lists the files and scenes it owns; a WP never
  edits another WP's files. If a WP needs a change in a file it does not own, it
  reports the need in its handback, it does not make the edit. (`MainMenu.unity`
  appears twice by design: WP-1 performs the rename only, no content edits; WP-2
  owns the content.)
- **Handback.** A WP is done when its checkpoint passes: its named criteria, the
  binding validator on its scenes (criterion 40's final all-scene zero belongs to
  WP-5), and its greps. The handback message states each criterion checked and any
  need discovered in files it does not own.

### WP-1 — foundations: scenes re-cut, generators deleted, shared APIs, verify loop green

Owns: `Assets/Scenes/Splash.unity` (new), the rename of `Splash.unity` →
`MainMenu.unity` (rename only), deletion of old `MainMenu.unity`; `SceneFlow.cs`,
`SceneBuilder.cs`, `SceneBuilder.Ship.cs`, `SceneBuilder.Enemies.cs`;
`SplashController.cs`, `SplashView.cs`, `MenuArenaBackdrop.cs`; the three mutators;
`UiBind.cs`, `ValueAnimator.cs`, `ShortcutPanel.cs`, `ShortcutPanelView.cs`;
`Editor/Tools/BindingValidator.cs` (new), `AutoVerify.cs` and the test assemblies
(`GatekeepersProtocol.Tests`, `GatekeepersProtocol.PlayTests`).

- **Delete** `ShiftFrontEndMutate.cs`, `ShiftShipUiImport.cs`,
  `ShiftBastionUiImport.cs` (+ `.meta`). Deleted rather than fenced: their
  one-shot import value is spent, their sentinel pollers regenerate scenes
  underneath the editor, and criterion 4 exists because "fenced" was already
  tried and dodged. Their only cross-references are to each other.
- Scene re-cut per criterion 3; new Splash scene: camera, one Overlay canvas,
  black `Image`, title `TextMeshProUGUI` (Rajdhani SemiBold, `UIManagerText`),
  `CanvasGroup` fade. **Change** `SceneFlow.cs`: `Splash` = boot, `MainMenu` =
  menu (the existing const stops being legacy and becomes live — do not delete
  it); `LoadMainMenu()` loads MainMenu; fix the stale three-scene error text.
  **Change** `SceneBuilder` build-scene registration to four scenes.
- **Change** `SceneBuilder` per criterion 6: shrink the owned set to
  `Environment`, `SpawnPoints`, `Directional Light`, NavMesh; in-place rebuild
  only; **delete** `CaptureUiRoots` / `RestoreUiRoots` and the creation paths for
  stations, player rig, `ShipController`, `GameSystems`, `EventSystem` after the
  one-time migration build. The largest code item in the milestone; it sits
  here because criterion 7 gates every later WP's scene work.
- **Rewrite** `SplashController.cs` (**delete** `SplashView.cs`,
  `MenuArenaBackdrop.cs`): timer + fade + async-load + any-key skip. Rewritten
  rather than adapted: the old class is a press-any-key loader whose
  inert-if-menu-present guard only made sense when splash and menu shared a scene.
- **Change** `UiBind.cs`: change-gate `SetText` / `SetBar` / `SetRelevant`; add
  the cached-formatting helper. **Change** `ValueAnimator.cs`: early-out when
  display equals target. **Change** `ShortcutPanel.cs` / `ShortcutPanelView.cs`:
  the structured row API (key label + description + section), pooled keycap rows,
  delete the dead `Draw` path. These land here so WP-2/3/4 consume them
  read-only; each gets EditMode coverage in this WP's checkpoint.
- **Create** `BindingValidator.cs` per criterion 40 (reflection-based;
  `[OptionalBinding]` attribute ships with it; separate
  `gkb_bindings_report.json`). **Diagnose and fix** the stuck AutoVerify editmode
  stage (start from `gkb_m35_autoverify.log` at the repo root); update the
  PlayMode scene-contract tests to the four-scene build list; the three-scene
  smoke assertion that lived in the deleted `ShiftFrontEndMutate.Smoke` moves
  into the PlayMode tests.
- Checkpoint: criteria 1-7; verify loop green (`AutoVerify` reports
  `status: "complete"`); new EditMode tests for UiBind gating and the shortcut
  row API pass; the validator runs and its report is the wiring worksheet for
  WP-2/3/4 (red entries expected and recorded, not a failure here).

### WP-2 — the menu

Owns: `MainMenu.unity` content; `MainMenuView.cs`, `MainMenuController.cs`,
`OptionsScreen.cs`, `OptionsView.cs`.

- Delete the criterion-16 residue (including the `SplashScreenManager` component
  and missing-script leftovers); author the four `Main Button`s and the
  campaign-strip label under `Home/Content`; retitle Top Panel; add
  `Confirm Overwrite`; revert the canvas to Screen Space – Camera and bind the
  existing Main Camera; author the Settings rows per criterion 13 (including the
  serialized Controls row template) and prune demo rows.
- **Change** `MainMenuView.cs`: serialized-reference wiring only; delete
  `Resolve()`, `SetVisible`, `HasFrontEnd`; keep `_campaignLine` (criterion 10 —
  `MainMenuController.RefreshMenu` already feeds it). **Change**
  `MainMenuController.cs`: add the editor-guarded quit; everything else keeps its
  flow logic (Continue-hiding already exists). **Change** `OptionsScreen.cs` /
  `OptionsView.cs`: registration and open-path per criterion 14; serialized row
  bindings per criterion 13; delete `OptionsScreen`'s own Esc-close (the scene
  dispatchers own Esc); annotate optional fields with `[OptionalBinding]`.
- Checkpoint: criteria 9-17; validator green on MainMenu; all four buttons and
  both modals exercised in-editor.

### WP-3 — the Ship

Owns: `Ship.unity`; everything in `Game/Ship/`; `StationPanelView.cs`,
`StationRowView.cs`, `ArmoryWheelView.cs`, `ShipHudView.cs`.

- Delete the criterion-22/23 residue. Author each panel's `Content` against
  `StationPanelView`'s full template contract — **all six templates**:
  `_rowPrefab` (adapted `Main Button` + price/level label pair), `_labelPrefab`,
  `_headingPrefab` (UIManagerText styles), `_buttonPrefab` (`Main Button`),
  `_spacerPrefab` (bare RectTransform), `_rowHostPrefab` (vertical group host) —
  plus per-station extras: the ARMORY wheel (five `Icon Button` spokes at 72°
  around a hub, top spoke = the focused weapon per `ArmoryWheelView.Configure`'s
  distance-0 semantics, clockwise following ring order from
  `ArmoryWheelDisplay`; hub title/sub, two projection lines, buy button and
  reason line beneath the hub — the one genuinely custom build, budget it as
  such); NAV MAP ◀/▶ per criterion 20, EMBARK `Main Button`, Negative-accent
  warning rows; a close button per panel wired to `StationTerminal.Close`.
- Author the Ship instances of WP-2's Settings rows (Settings panel content in
  the station canvas) including QUIT TO MENU per criterion 32; author the Ship
  keycap strip instance of WP-1's `ShortcutPanelView`.
- **Change** `StationPanelView.cs`: instance-per-panel; delete `EnsureInstance`;
  serialized template refs; loud errors; `[OptionalBinding]` annotations.
  **Change** `StationTerminal.cs`: serialized `_view` and `_canvasRoot`;
  open/close per criterion 21; delete its Esc poll. **Change**
  `ShipController.cs`: the Ship Esc dispatcher per the Escape contract; Settings
  activation; QUIT TO MENU calls `SceneFlow.LoadMainMenu()`. **Change**
  `ShipHud.cs` / `ShipHudView.cs`: authored view on its own canvas (criterion
  28's Ship half), serialized refs, remove per-call `EnsureBuilt`, remove the
  per-frame interpolations and `SetVisible` toggling (criteria 26-27's Ship
  half), add the `DevTaintBanner` call site (criterion 36's Ship half).
- Station controllers keep their logic; only view plumbing changes.
  `ShipCommands` is untouched — already the single, sound mutation path.
- Checkpoint: criteria 18-25, plus the Ship halves of 26-29, 32, 33 and 36
  (including the Ship profiler capture); validator green on Ship; a full
  mouse-driven ship loop in-editor.

### WP-4 — the Bastion: HUD, pause, quit to menu

Owns: `Bastion.unity`; `HudController.cs` (+ partials); `GameController.cs`
(+ partials); `BastionHudView.cs`, `EndScreenView.cs`; `Controls.cs`;
`DevTaintBanner.cs`; `Resources/Shift UI Manager.asset`.

- **Authoring, not just wiring** — the scene has hosts only for Armor & Health,
  Active Mission and Shortcut Keys; everything else `BastionHudView` (47 fields)
  and `EndScreenView` (15 fields) bind must be **created** from Shift parts:
  crosshair variants, vignette, stamina block, weapon/ammo/reload block, ability
  block, interact prompt, assault strip, arriving and emergency strips, both end
  screens — placed per criterion 34, split onto the dynamic child canvas per
  criterion 28. Strip `GraphicRaycaster` from non-interactive canvases; Raycast
  Target off on HUD graphics.
- Pause per criterion 31 (prune list included) and QUIT TO MENU per criterion 32
  — `GameController` owns the save-then-load call, mirroring its own
  `OnApplicationQuit`. Author the Bastion instances of WP-2's Settings rows in
  the pause canvas; author the Bastion keycap strip instance.
- **Change** `HudController.cs` (+ partials): the Bastion Esc dispatcher per the
  Escape contract (delete the scene hotkey components it replaces); event-driven
  phase transitions; value pushes only per frame; cache `PlayerAbility` /
  `WaveSpawner` refs once; `DevTaintBanner` call site. **Change** `Controls.cs`:
  delete the `Options`="O" binding; update the ESC row so the shortcut strip
  stays truthful. `[OptionalBinding]` annotations on both views.
- Set `enableDynamicUpdate` off in `Shift UI Manager.asset` (YAML or debug
  inspector — the field is hidden).
- Checkpoint: criteria 26-36 except the Ship halves WP-3 already closed
  (including the Bastion profiler captures); validator green on Bastion; pause,
  quit-to-menu, and both end screens exercised in-editor.

### WP-5 — close-out: docs, sweeps, screenshots

Owns: `../../../AGENTS.md`, `../../Architecture.md`, `../../Decisions.md`,
`../../Roadmap.md`, `../3. completed/M36-the-hatch-is-always-on.md`; the final
validator run; the screenshots; code-comment sweep.

- The criterion-8 doc list. Sweep stale in-code criterion references — code
  comments cite the old M35 numbering (`StationPanelView.cs:12-13`,
  `BastionHudView.cs:133,227`, `EndScreenView.cs:9`, `UiBind.cs:10,116`,
  `MainMenuView.cs:13`, `SplashController` if any survive) — update or delete
  them so they stop misleading the next agent.
- Final `BindingValidator` run: zero missing bindings across all four scenes
  (criterion 40's closing state). The criterion-37/38/39 global greps. The
  criterion-41 screenshots.
- Checkpoint: criteria 8, 37-41; every link in the touched docs resolves. Then
  criterion 42 — Zo's run.

Rejected: keeping any import tool "for re-runs". A re-runnable importer and
authored scenes cannot coexist; the importer's `ClearContent` deleting the
authored templates is how the stations broke.

Rejected: fixing `UiBind.Find`'s first-match lookup instead of removing
name-based wiring. Names are not references; two `Home`s and seven `Exit`s in
one scene already proved the failure mode.

Rejected: a modal confirm on EMBARK. Warnings render inline; embarking is the
loop, not a destructive action.

Rejected: quit-to-menu on the end screens. One action per screen; the end
screens return to the Ship and the menu is one Esc away there.

Rejected: a no-save quit-to-menu. The game already saves mid-run on app quit
(`GameController.cs:409-412`) and banks salvage live; a no-save path would be
the only way to roll back a run in progress — a new exploit, not parity.

Rejected: splitting this into separate milestone files. The grain rule
(`../../_meta/_definitions/milestone.md`) argues for a split; kept as one file
at Zo's direction, with per-WP checkpoints standing in for per-milestone gates.

## Verification

1. Criterion 37 first — the built-in-sprite grep; expect 0 (currently 2-3 per
   scene, so a pass means work happened).
2. Criteria 3-5 — the greps as smoke tests plus the named diff reviews; expect 0
   matches / only `AutoVerify.cs` and `SceneBuilder.cs`.
3. Criteria 6-7 — Build All twice from a clean tree; the `git diff -U0`
   authored-block check as written in criterion 7.
4. Criterion 40 — `BindingValidator`: `gkb_bindings_report.json` with zero
   missing bindings (per-WP on its own scenes; all four at WP-5), and
   `AutoVerify` green (`status: "complete"`). Blocks claiming anything below.
5. Criteria 9-17 — in-editor: load MainMenu, click all four buttons, both
   modals, every Options row; `Logs/gkb_latest.log` shows the scene loads and
   zero errors.
6. Criteria 18-25 — the WP-3 checkpoint loop: every console, one purchase each
   where purchases exist, EMBARK; read the purchase lines in the log and the
   repainted rows on screen; Esc behaviour at each dispatcher priority level.
7. Criteria 26-34 — the criterion-33 profiler captures; pause during an Assault
   (criterion 31): Gatekeepers keep moving behind the overlay; then Esc →
   QUIT TO MENU → confirm → menu → CONTINUE resumes on the Ship with the state
   the campaign strip showed (criterion 32).
8. Criteria 35-36 — dev build: keycap strip renders, taint a session and see
   the banner; release build: none of it exists.
9. Criteria 38-39, 41 — the theme and Ellipsis greps, the font-size/guid scene
   grep, the fifteen screenshots at both resolutions.
10. **Playthrough path** — from Play in `Splash.unity`: splash auto-advances
    (run it twice: once hands-off, once skipping) → no CONTINUE on first boot
    (delete the save file via `SaveService`'s path, or the dev console wipe, to
    reach that state) → START NEW → Ship → each console, Interact, buy where a
    purchase exists, close → NAV MAP → set wave → EMBARK → fight → pause,
    resume → die or extract → end screen with the Run's numbers → back on the
    Ship → Esc → QUIT TO MENU → menu → CONTINUE present with the campaign
    strip, resumes on the Ship. Confirms criteria 1, 9, 10, 18-21, 25, 30, 31,
    32 in one two-boot sequence.
11. Criterion 42 — Zo's run and verdict, written into this file.

## Watch

- **`MainPanelManager.Awake` plays `panels[0]`** — harmless while the station
  canvas is inactive until opened; if the canvas is ever left active at load,
  ARMORY fades in over the ship uninvited.
- **Panels are invisible while edited** (inactive panels store `CanvasGroup`
  alpha 0); set alpha 1 to work, back to 0 before saving.
- **Layout Group + Content Size Fitter mis-position in the editor** — toggle the
  Canvas or enter Play; keep `LayoutGroupPositionFix` where Shift put it. It is
  only dangerous under a per-frame SetActive pattern, which criterion 26
  removes.
- **Do not tidy Shift's numbers** (1920×1280, 25-based sizes, the shipped
  alphas).
- **The copied HUD is a fake until bound** — demo sliders are hardcoded and
  labels are typed strings; criteria 27 and 30 exist so the fake cannot
  survive.
- **Renaming scenes moves GUIDs with the `.meta`** — rename in the editor, then
  fix `EditorBuildSettings` before anything else.
- **Shift runs on the legacy input system** (store page) — fine today (the
  project polls legacy input), but a future Input System migration touches
  every Shift interactive.
- **Script execution order is not an arbitration mechanism.** The Esc contract
  exists because three scripts polled the same key in the same frame; if a new
  Esc consumer appears, it joins a dispatcher, it does not poll.

## Open questions

- Panel readability over the live first-person view with the demo `Background`
  deleted — unknown, needs play check. If a scrim is needed, the named fallback
  is the modal `BlurManager` idiom, not a new ad-hoc overlay.
- Blur and menu `Background` behavior after the Screen Space – Camera revert —
  unknown, needs editor check.
- Cause of the AutoVerify `stuck` editmode state — unknown; WP-1 diagnoses it,
  starting from `gkb_m35_autoverify.log` at the repo root.
- Profiler ms baselines — unknown until the criterion-33 captures exist;
  targets get set from the baseline, not invented.
- Whether abandoning a run via quit-to-menu should carry a stake (the
  reward-framed loss design) — deliberately **not** decided here; criterion 32
  ships app-quit parity, and the stake question goes to the roadmap Brainstorm.

## Do not build

- No matchmaking, lobby, friends, or server UI — the co-op entry point is a
  later milestone (`../../Roadmap.md` Brainstorm). Removing the fake Quick
  Match is not a decision against MP; the multiplayer-readiness rules in
  `../../Architecture/1-Boundaries.md` still govern everything here.
- No new theme, palette, or font. The theme is `Shift UI Manager.asset`,
  customized there only.
- No loading screens beyond the splash; scene loads are async and short.
- No key rebinding UI; no quit-to-menu on end screens; no abandonment stake
  (see Open questions).

## Depends on / Unblocks

Depends on Shift v2.0.12, imported (store-verified compatible with Unity 6 and
Built-in RP, May 2026 build). **Unblocks M36** (HUD tether — needs the bound,
split HUD; M36's file is corrected by criterion 8). Ship station panel polish
(Settings-shell strip, row chrome, readability, criterion-41 screenshots) is
deferred to a later milestone — not filed at close.
