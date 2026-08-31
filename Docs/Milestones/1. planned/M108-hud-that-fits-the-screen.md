# M108 — HUD that fits the screen

<!--
Status: later
Update trigger: when a fight or Ship HUD host is moved, or when CREW QUARTERS catalog rows are re-authored
Last updated: 2026-08-30
-->

Corner HUD and CREW QUARTERS catalog text sit on the screen, at a size that can be read, without overlapping their own labels. Hosts stay the locked Shift cards; they move and grow, they are not rebuilt.

## Story

As a Boarder, I want the fight HUD, the Ship HUD, and CREW QUARTERS to leave a margin, keep their labels apart, and set type large enough to read, so that I can tell who I am, what I am carrying, and which protocol is equipped without the chrome eating the words.

## Why it matters

Screenshots 2026-08-30: Bastion vitals clip at the left edge (`Hud Vitals` is at x=**24**, not the locked **135** in `../../Decisions.md`); MISSION [TAB] sits **8** canvas units above the prep heading; Profile/Score type is 14–20 on both Bastion and Ship; CREW catalog Name is **220** px so `FORGE Protocol` ellipsizes, and EQUIPPED wraps inside an **80** px Level cell that then shoves SELECT right.

Leverage: **#8, information flows**. Hosts already exist. The defect is inset, gap, and type size. Rebuilding corners, or drawing the HUD in `OnGUI`, is the higher tier and is wrong (`../../Architecture.md` gotcha 3).

## Acceptance criteria

1. **`Hud Vitals` on `Bastion_Coop.unity` is at anchoredPosition `(135, 24)`** and still `400 × 150`. Health and Stamina value TMP rects under that host have `sizeDelta.x ≥ 80`.
2. **`Hud Profile` on `Bastion_Coop.unity` is at `(40, −40)`, size `360 × 96`.** Icon left; Profile Level (BOARDER) and Protocol Line sit to its right. Profile Level `fontSizeBase ≥ 28`, overflow is not Truncate (mode 3).
3. **`Hud Score` on `Bastion_Coop.unity` is at `(−24, −40)`, size `400 × 96`.** Children: Score Salvage, Wave Counter. Score Salvage `fontSizeMin ≥ 20`; Wave Counter `fontSizeMin ≥ 18`.
4. **MISSION header does not sit on the prep body.** Right-drawer Panel VLG `m_Spacing ≥ 24` and `ChildControlHeight` 0. Drawer Right Header `sizeDelta.y ≥ 52`, LayoutElement `minHeight ≥ 52`, `fontSizeBase ≥ 24`. Active Mission `anchoredPosition.y ≤ −100`. Same numbers on `Range.unity`.
5. **Ship HUD (`Ship - CoOp.unity`):** Profile at `(40, −40)`, size `360 × 96`, with BOARDER and Protocol Line beside the icon. Score at `(−24, −40)`, size `360 × 96`. Profile Level `fontSizeBase ≥ 28`. Score Salvage and Wave Counter `fontSizeMin ≥ 18`. Weapon Name `fontSizeMin ≥ 16`. Every always-on host AABB stays ≥ **24** canvas units from every screen edge at 1920×1080 and 1280×720. Range Score stays inactive; Range Profile is on and holds the same identity stack.
6. **CREW QUARTERS catalog:** each of Vitality / Stamina / Scavenging / Luck Row has Name `sizeDelta.x ≥ 340` and Level `sizeDelta.x ≥ 160` with wrap off. Unequipped rows still reserve the Level column so SELECT shares an x with the equipped row.
7. **CREW QUARTERS chrome:** `Game Title` `fontSizeBase ≥ 48`. CREW `Station Title` `fontSizeBase ≥ 48`. Title→Purpose and Purpose→Deal gutters stay ≥16 (`M59PolishChromeTests`). Protocol sheet does not show a second BACK; chrome Close is the only back control.
8. **Existing M59 gates stay green:** pairwise fight/Ship AABB ≥24, overflow TMP, Profile Exp inactive, Hud Vitals `400 × 150`, station Categories/Preview inactive, station blur α≥0.65.

## Implementation

**Change** `Assets/Scenes/Bastion_Coop.unity` — numbers in criteria 1–4. Profile holds icon + BOARDER + protocol (left-aligned beside the icon). Score is salvage/wave only. Do not widen Score past 400 (Hatch gap at 1280×720 is 36). Header LayoutElement preferred/min height **52**. Right-drawer Panel is the `Panel` whose children include `Drawer Right Header` (fileID `410409389`). Health/Stamina value TMP: the three RectTransforms with `anchoredPosition (-10, -1)` and `sizeDelta (0, 50)` — set x to **80**.

**Change** `Assets/Scenes/Ship - CoOp.unity` — criteria 5–7. Same Profile/Score recut as Bastion. Protocol Line lives under Hud Profile; bind `ShipHudView._protocolLine`. CREW panel only for Station Title (do not retitle ARMORY / FABRICATION / NAV MAP). If Station Title height grows, shift Purpose / Deal / Banked / Lifetime down so M59 gutters hold. Catalog rows are authored, not instantiated. `Game Title` lives on `Top Panel` and is shared across stations.

**Change** `Assets/Scenes/Range.unity` — same MISSION header/tab numbers as Bastion. Activate Hud Profile, bind `_profileRoot`, parent BOARDER + Protocol Line beside the icon. Score stays inactive.

**Change** `Assets/Scripts/Game/Ui/ShipHudView.cs` and `Assets/Scripts/Game/Ship/ShipHud.cs` — optional `_protocolLine`; `SetProtocolLine` always shows it; unequipped `"NO PROTOCOL"`.

**Change** `Assets/Scripts/Game/Ui/PanelRowView.cs` — `SingleLine(_level)` the same as name/cost/effect.

**Change** `Assets/Scripts/Game/Ship/CrewQuartersStation.cs` — `BindCatalogRow` passes `"\u00A0"` (nbsp) when unequipped so Level stays active. `BuildSheetMode` passes `onBack: null` into `ProtocolSheetView.Bind` so the in-sheet Back hides; chrome Close already pops the sheet.

**Change** `Assets/Tests/EditMode/Ui/HudSeparationTests.cs` — parse `fontSizeBase` / `fontSizeMin` / rect pos+size; assert criteria 1–6. Add an edge-gap check (host AABB vs screen ≥24) on Bastion and Ship hosts.

**Change** `Docs/Decisions.md` locked HUD tables to the new Profile/Score sizes and the restored Vitals inset.

**Do not:** rebuild hosts; add `GUILayout` to the fight HUD; runtime-rebuild RectTransforms; Instantiate catalog rows; change `Hud Vitals` size; retitle the other three stations; put BOARDER or protocol on Hud Score.

## Verification

1. Criterion 1–6, 8 — `HudSeparationTests` (new asserts + existing pairwise/overflow/EXP). Expect pass at both 1920×1080 and 1280×720.
2. Criterion 7 gutters — `M59PolishChromeTests.StationHosts_HeaderBodyCloseGuttersAtLeast16`.
3. Criterion 7 sheet Back — `StationPurposeTests` still accepts SELECT / BACK copy; a new assert that `ProtocolSheetView.Bind(..., onBack: null)` leaves `_back` inactive, or a YAML-independent unit on `Bind`.
4. Playthrough path (confirmation, not the close gate until a replayable HUD slice exists): Play → Bastion_Coop, Tab for MISSION, read corners; Ship → CREW QUARTERS catalog then FORGE sheet. Confirms clip, overlap, type size, and SELECT alignment on screen.

## Watch

- `HudSeparationTests.FightShortcutPrepAndPrompt_ClearLockedCards` — growing Score downward must still leave Prep vs Hatch ≥24.
- `M59PolishChromeTests.HudVitals_HostStays400x150` — do not grow Vitals.
- No `GUILayout` in Bastion HUD (`../../Architecture.md` gotcha 3).

## Do not build

- New HUD hosts or a homemade bar.
- OnGUI / IMGUI fight UI.
- A CREW row prefab or runtime factory.
- Re-cloning ARMORY/FABRICATION/NAV MAP chrome.
- `M66` hit-feedback / identity polish.
