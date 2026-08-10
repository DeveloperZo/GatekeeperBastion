# M35 — One look, drawn from one place

<!--
Status: active
Update trigger: when a Shift version lands, when a screen is added, or when a value moves in Shift UI Manager
Last updated: 2026-08-10
-->

**The game's screens are Shift's demo scenes with our content in them.** Copy the Canvas
branches out of the two demo scenes, duplicate the `Home` panel once per screen, bind data.
Nothing is generated.

## Story

As the player, I want the game to look like something that was made, so that the screens read
as a product rather than as scaffolding.

## Why it matters

The UI is the last thing that still reads as a prototype, and Shift v2.0.12 ships a complete
main menu, a complete HUD, a working options screen and a non-blocking pause — all of it
currently unused. **Leverage tier (`../../Working-Rules.md`): #10, structure** — it changes
where the look comes from, not what any value is.

## What Shift ships — measured 2026-08-10

**Use the plain scenes, not the `URP & HDRP/` copies.** This project is Built-in Render
Pipeline (`ProjectSettings/GraphicsSettings.asset`, `m_CustomRenderPipeline: {fileID: 0}`), so
Shift's blur materials work; the URP copies exist to strip them.

### `Scenes/Main Menu/Main Menu (Desktop).unity`

One `Canvas`, Screen Space – Camera, **1920×1280, match 0.5**. Children in order:
`Event System`, `Menu Manager`, `Quality Manager`, `Background Music`, `UI Audio`,
`Background`, `Splash Screen`, `Main Panels`, `Modal Windows`, `Start Fade-in`.

| Object | Carries | Gives us |
|---|---|---|
| `Menu Manager` | `SplashScreenManager`, `MainPanelManager` | splash, and the panel router |
| `Quality Manager` | `QualityManager` | resolution, vsync, audio mixer — the options screen |
| `Modal Windows` | `BlurManager` (`_Size` 0→5, speed 25) | confirm modal, blurring the frame behind |
| `Start Fade-in` | `Image`, `Animator` | the opening fade |

**A panel is exactly three children.** `<Panel>` carries `CanvasGroup` + `Animator`
(`Animations/Panel/Main Panels/Main Panel.controller`) and contains `Shortcut Keys`, `Hotkeys`,
`Content`. **`Content` is a bare RectTransform — put our content there and it animates for
free.** `Home` is the simplest panel (160 nodes) and is the one to duplicate.

`MainPanelManager.panels` is a list of `{panelName, panelObject, buttonObject}`; open with
`OpenPanel("<name>")`. Buttons are `Prefabs/Button/Panel Button.prefab` under
`Top Panel/Button List`. **Nested managers work** — `Settings` carries its own with four
sub-tabs on `Panel Button Alternative.prefab`.

### `Scenes/Simple HUD/Simple HUD (Desktop).unity`

Two Canvases: `Canvas - In Game` (Overlay, order 0) and `Canvas - Pause Menu` (order 1), both
1920×1280 match 0.5.

**`Canvas - In Game` is 39 objects with zero prefab instances** — it copies clean. Contains
`Active Mission` (`VerticalLayoutGroup` + `ContentSizeFitter` list), `Armor & Health` (two
bars), `Minimap`, and a placeholder backdrop. **No crosshair, no ammo readout** — those are
ours to build from Shift parts.

**The bar recipe**: `Slider` whose `fillRect` is a **sliced** `Filled` image, clipped by a
parent `Bar` that is a `Mask` with `m_ShowMaskGraphic: 0` carrying a **Filled (`m_Type: 3`)**
angled-cut frame, over a sliced `Background`, under a sliced `Border` glow. That is how a
non-rectangular bar fills without smearing its 9-slice corners.

**The pause menu already satisfies `../../Decisions.md`.**
`Pause Menu Manager/Keyboard Hotkey` (`PressKeyEvent`, key 27) plays `Window In` and calls
`BlurInAnim()`. It never touches `Canvas - In Game` and never sets `Time.timeScale`.

**The HUD drives nothing.** Both sliders have an empty `onValueChanged` and hardcoded values;
`52%` / `69%` are literal strings typed to match. Shift supplies the shell, we supply every
value.

### The theme

`Resources/Shift UI Manager.asset`, edited at **Tools > Shift UI > Show UI Manager**.
`UIManagerImage` picks a colour by enum (`Primary`, `Secondary`, `PrimaryReversed`, `Negative`,
`Background`); `UIManagerText` picks colour and weight (`Light`…`Bold`). Values are universal;
an element opts out by deleting its component.

### The standard to match

| | Value |
|---|---|
| Canvas | 1920×1280, match 0.5 |
| Font | Rajdhani-SemiBold SDF (97 of 100 uses), Rajdhani-Medium SDF |
| Sizes | 25, 29.5, 30, 36, 40 |
| Images | 76% Sliced. Borders: `Cut Frame Filled` 6, `Cut Frame Glow - 3px` 19, `Cut Frame Filled Big` 26, `Cut Frame Glow Big` 40, `Circle Filled Glow` 356 @750ppu |
| Panel idiom | filled body + glow border + `Circle Filled Glow` shadow |
| Alphas | fills 0.039, borders 0.686, icons 1.0 |
| Accent | cyan (0.388, 0.776, 1.0); negative red (1.0, 0.431, 0.392) |

## Acceptance criteria

### A — the generator goes, the scenes become source

1. **Deleted, with `.meta` files:** `BastionUiAssembler.cs`, `ShipUiAssembler.cs`,
   `ShiftFrontEndAssembler.cs`, `UiPrefabBuilder.cs`, `UiPrefabVariant.cs`, `UiFactory.cs`,
   `ShiftUi.cs`, `Theme.cs`, plus the `Build UI Prefabs` menu item and batch entry.
   `grep -rn "new GameObject\|AddComponent<" Assets/Scripts --include=*.cs` returns nothing
   under any `Ui` folder.
2. **`SceneBuilder` no longer touches UI.**
   `grep -n "InstantiateUiPrefab\|Prefabs/Ui" Assets/Scripts/Editor/Scenes/*.cs` returns
   nothing; the UI lives in `Ship.unity` and `Bastion.unity` as authored content.
3. **Build All twice leaves the UI byte-identical.** `../../Architecture/README.md` and
   `../../Decisions.md` are amended in the same commit: **`SceneBuilder` owns geometry, spawns
   and NavMesh; it does not own UI.**

### B — the front end is Shift's front end

4. **The `Canvas` branch of `Main Menu (Desktop).unity` is copied into the Ship scene**, with
   `Menu Manager`, `Quality Manager`, `Background`, `Splash Screen`, `Main Panels`,
   `Modal Windows` and `Start Fade-in` intact and Animators still bound.
5. **ARMORY, FABRICATION, CREW QUARTERS and NAV MAP are each `Home` duplicated** — three
   children kept, registered in `MainPanelManager.panels` with a `Panel Button`, opened by
   `OpenPanel`. **Our content sits under `Content` and nowhere else.**
6. **Controllers bind values and call `OpenPanel`.** `StationTerminal`, the station scripts,
   `SplashController`, `ShipHud` and `HudController` hold no layout, no colour, no rect maths.
7. **The confirm modal is `Prefabs/Modal Window/Exit.prefab`**, driven by `ModalWindowManager`
   with `ModalWindowIn()` / `ModalWindowOut()`, `BlurManager` on the parent.
8. **Options is Shift's Settings panel wired to `QualityManager`** — its resolution selector,
   `VsyncSet`, audio mixer. Our settings join as `Horizontal Selector`, `Slider` and `Switch`
   instances with unique save tags. No custom options widget is written.

### C — the HUD is Shift's HUD

9. **`Canvas - In Game` is copied wholesale into the Bastion scene**, then the placeholder
   backdrop and `Minimap` are removed.
10. **Hatch integrity and player health use the bar recipe unchanged** — sliced `Filled` as
    `fillRect`, `Mask` parent with `m_ShowMaskGraphic: 0`, sliced `Background` and `Border`.
    **Both sliders set `m_Interactable: 0`**; Shift ships them draggable.
11. **Each numeric label is driven from the same value as its bar.** One binding, one source.
12. **`Active Mission` becomes the objective line**, keeping its layout group, fitter and
    `<b>` markup.
13. **The pause overlay is Shift's pause Canvas as shipped** — second Canvas at order 1,
    Escape, `Window In` / `BlurInAnim()`. **It must not touch `Time.timeScale` and must not
    disable the game Canvas** (`../../Decisions.md`).
14. **Crosshair and weapon readout are built from Shift parts** — `Cut Frame` sprites, `Icon`
    sprites, the bar recipe. They use no sprite from outside Shift.

### D — the look is checkable

15. **`grep -rn "guid: 0000000000000000f000000000000000" Assets/Scenes Assets/Prefabs` returns
    zero.** No Unity built-in UI sprite survives anywhere. **Run this first; it is the cheapest
    check and the one that fails loudest.**
16. **Every `Image` is `m_Type: 1` unless it is an icon or a fill**, and every icon names a
    sprite under `Assets/Shift - Complete Sci-Fi UI/Textures/`. `m_Type: 0` is under 25% of
    total Images, matching Shift's own 24%.
17. **Every `Image` carries `UIManagerImage`, every `TextMeshProUGUI` carries `UIManagerText`.**
    No colour is set on an element directly; `grep -rn "new Color\|Color32(" Assets/Scripts/Game/Ui`
    returns nothing.
18. **All text is Rajdhani** at 25 / 29.5 / 30 / 36 / 40, weight through `UIManagerText`.
19. **`grep -rn "Ellipsis" Assets/Scripts` returns nothing.** Labels wrap, or the container
    fits them.
20. **No authored `RectTransform` is left at `sizeDelta 100×100` with centred anchors.**

### E — it survives being looked at

21. **Screenshots at 1920×1080 and 1280×720**, filed in `../../GatekeeperZo Notes/`: splash,
    main menu, options, the four station panels, confirm modal, Bastion HUD in Prep, Bastion
    HUD in Assault, pause overlay, end screen.
22. **Zo plays one Run start to finish and gives a verdict on this file.** The greps above are
    necessary and not sufficient; this row is what closes the milestone.

## Implementation

**Copy, do not rebuild.** Open `Main Menu (Desktop).unity`, select the `Canvas` root, copy,
paste into the Ship scene. Same for `Canvas - In Game` and `Canvas - Pause Menu` from
`Simple HUD (Desktop).unity` into the Bastion scene. Delete what we do not use, then duplicate
`Home` per panel.

**Delete** the eight scripts in criterion 1 and their menu entries.
**Change** `SceneBuilder.cs` and `SceneBuilder.Ship.cs` to stop touching UI.
**Change** `HudController`, `StationTerminal`, `ShipHud`, `SplashController`, `OptionsScreen`,
`ShortcutPanel` to bind values and call `OpenPanel` / `ModalWindowIn`.
**Change** `../../Architecture/README.md` and `../../Decisions.md` per criterion 3.

Three package behaviours that otherwise read as bugs:

- **Panels are invisible while you edit them.** Inactive panels store `CanvasGroup` alpha 0.
  Set it to 1 to work, back to 0 before saving.
- **Layout Group + Content Size Fitter mis-positions children in the editor.** Disable and
  re-enable the Canvas, or enter Play. Keep `LayoutGroupPositionFix` on the objects that have it.
- **Turn off `Update Values` in UI Manager** unless actively changing theme values — it runs in
  `LateUpdate` across 260 components.

Rejected: generating any part of this, in any form. Criterion 1.

Rejected: keeping `Theme.cs`. Two sources of colour is how colours drift apart.
`Shift UI Manager.asset` is the one place.

Rejected: the `URP & HDRP/` scene copies. Built-in pipeline; those exist to remove blur that
works here.

## Verification

1. Criterion 15 first — expect zero, before claiming anything else is done.
2. Criteria 1, 2 — the greps, then Build All twice and `git diff` the scene files.
3. Criteria 16, 17, 18 — count `m_Type`, `UIManagerImage`, `UIManagerText` and font guids
   across authored content; compare to Shift's ratios.
4. Criterion 19 — the grep.
5. Criterion 13 — pause during an Assault; Gatekeepers keep moving behind the overlay.
6. Criterion 21 — the twelve screenshots, both resolutions.
7. Criterion 22 — the Run, and the verdict written here.

## Watch

- **`Content` is the whole contract.** Anything parented outside a panel's `Content` loses the
  panel animation and will look bolted on. If something will not fit under `Content`, the panel
  is the wrong shape.
- **Do not tidy Shift's numbers.** 1920×1280, 25-based sizes, alpha 0.039 and 0.686 look
  arbitrary and are what the art was drawn against. Imposing an 8-point grid makes a bought UI
  look worse than its own demo.
- **Copying the HUD gets zero behaviour.** Both sliders are hardcoded and their labels are
  typed strings; criterion 11 exists so the shipped fake does not survive the copy.

## Depends on / Unblocks

Depends on Shift v2.0.12, imported. **Blocks M36** and **M39's `Game/Ui` scope**.
