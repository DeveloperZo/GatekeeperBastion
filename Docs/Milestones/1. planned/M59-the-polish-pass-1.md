# M59 — Polish Pass 1

<!--
Status: later
Update trigger: when field-console scale, fight-HUD hosts, station chrome, pause key list, or HUD overflow rules change.
Last updated: 2026-08-14
-->

Friends QoL: the Bastion as a room a person stands in, and the Ship as something a stranger
can operate. Field terminals, fight HUD, station chrome and copy, player-build keys. Music
and gun/foot SFX are [`M69`](M69-the-session-has-a-voice.md). Guns, zoom, ship-console art
are `M61`. Compendium is `M62`.

## Story

As a Boarder, I want the Bastion's consoles and HUD, and the Ship's panels and keys, to fit
a person who has not been briefed, so that a friend can embark and fight without a developer
on voice chat.

## Why it matters

Friends alpha asks whether someone who is not Zo wants to stay in the session. Measured holes:

| Surface | Evidence | Now |
|---|---|---|
| Field terminals | `BastionFieldConsole.Scale` = `BastionKit.StoreyHeight / 2.5`. Storey is **5** (`BastionKitFootprintTests.StoreyHeight_IsFive`). Scale = **2**. Desk cube `3.2 × 2.4 × 1.4` m, centre Y `1.2` → top at **2.4 m**. | Taller than a standing Boarder. Authored to "read in Full/Open rooms", not to be used. |
| Fight HUD gaps | Locked hosts in `../../Decisions.md` (2026-08-10). M35 gated ≥24 from **screen edges**. Pairwise card gaps, shortcut vs vitals, Prep vs Hatch were not gated. | Drawers and strips can sit on cards depending on resolution and drawer state. |
| Score overflow | `Hud Score` 320×72, padding 16, Score Salvage wrap off / overflow Ellipsis, font 20; Wave Counter font 14, same overflow. `HudController.Fight` pushes `Run · Intensity · Wave n/5 · m:ss`. | Pairwise AABB stays green while the meta line ellipsizes. |
| Profile | `HudController.Fight` / `ShipHud` `SetProfile("LV 1", "EXP —")`. No EXP system. | A friend asks what EXP is. |
| Station chrome | M35 deferred panel detail. `Ship.unity` still contains `Categories` and `Preview Image`. `StationPanelView.SetHeader` paints name + banked + lifetime. | Settings-shell leftovers over the live view. |
| Station copy | `Play/The-Ship.md` Gaps: transfer; Rig-and-Salvage deal; no sell-back; movement lock; start-Wave label vs cleared. | A friend cannot answer those from the panels. |
| Keys | M15 shipped F1–F9 because the person playing was the person developing. `ShortcutPanel` appends DEV under `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`. Pause lists no playable keys. | A Development player build still lists skip-wave / god / grant-salvage. |
| End screen | Friends alpha: the run ends with a screen that tells them what happened. `EndScreenView` sequences verdict + net. Scrim/gutters were not gated. | |

Leverage: **#8, information flows** for HUD, chrome, copy, and keys; **#10, structure** for
terminal scale (the room-fill multiplier, not a texture). A higher tier (new kit geometry,
new HUD hosts, Input System rebinding) is wrong.

## Acceptance criteria

1. **`BastionFieldConsole.Scale` is `1f`.** The StoreyHeight/2.5 multiplier is gone. At
   Scale 1 the desk cube is `1.6 × 1.2 × 0.7` m, centre Y `0.6` (top **1.2 m**); screen
   centre Y **1.7 m**. EditMode asserts `Scale == 1f` and those three vectors. Re-apply
   via existing `ApplyScale` / `gkb_bastion_terminals_request.txt` so every authored T in
   `Bastion.unity` matches; do not leave old Scale-2 meshes in the scene.
2. **Always-on fight cards do not overlap.** The five locked hosts (`Hud Profile`,
   `Hud Score`, `Hud Hatch`, `Hud Vitals`, `Hud Weapon` — `../../Decisions.md`) have pairwise
   AABB gaps ≥ **24** canvas units at 1920×1080 and 1280×720. Edge gap ≥24 stays (M35).
   Hosts are not rebuilt; they may move or grow by that gap. Shift Icon Border + Slider +
   Text rows stay intact.
3. **Shortcut and Prep do not sit on those cards.** Expanded shortcut AABB does not
   intersect `Hud Vitals` or `Hud Weapon`. Prep/Assault heading sits ≥24 below `Hud Hatch`.
   Interact prompt stays centre; it does not intersect `Hud Vitals`.
4. **Score Salvage, Wave Counter, and Weapon Name are not truncated** at either resolution
   with worst-case strings (`Unbanked 99999 · Banked 99999`; `Run 99999 · Intensity 5 ·
   Wave 5/5 · 59:59`; longest catalog `displayName`). Wrapping, auto-size, or a wider card
   is allowed if criterion 2 still holds. Same rule on Ship Score.
5. **Hud Profile EXP line is inactive** on Bastion and Ship. Profile Level is not the
   literal string `LV 1`.
6. **Station chrome is a panel, not a Settings shell.** While ARMORY / CREW QUARTERS /
   FABRICATION / NAV MAP is open, `Categories` and `Preview Image` are inactive or deleted
   on that canvas. Header, body, and close are separate RectTransforms with ≥16 canvas
   units between header bottom and first body row, and between last body row and close.
   A scrim covers the body (existing `BlurManager` idiom named in M35, or one authored
   Image whose alpha is ≥ 0.65). Body TMP remains readable over the live Ship view. No
   `GUILayout`.
7. **Each station panel carries a purpose line** under the station name, the subject from
   `../../_meta/_definitions/language.md` (The stations): ARMORY — the weapon in your hands;
   CREW QUARTERS — you; FABRICATION — the Hatch and the Ship's gear; NAV MAP — where you go,
   and going there. Visible at open. Empty purpose fails the test.
8. **ARMORY states that upgrades stay on that weapon** before the first wheel purchase.
   **FABRICATION states the Repair Rig deal before embark** (Rig required; salvage spent as
   HP is applied; figure from `ShipSystems.RepairSalvagePerHp` at bind time). **No sell-back**
   once, shared on `StationPanelView`. **Opening a console locks movement**, shared on
   `StationPanelView`. **NAV MAP start-Wave copy names deepest cleared**, not deepest
   reached. Locked rows keep naming the lock (pin, already ships).
9. **Ship always-on HUD gutters.** The three Ship hosts (`Hud Profile`, `Hud Score`,
   `Hud Weapon` — `../../Decisions.md`) have pairwise AABB gaps ≥24 canvas units at
   1920×1080 and 1280×720. Interact prompt does not intersect `Hud Weapon`.
10. **Player-build keys.** In a player build (Development and Release), `ShortcutPanel` does
    not list F1–F9. **Player builds never bind F2–F9.** Editor Play Mode may still show and
    bind them. After M58, F10/F11 also leave the player-build panel (join is the Main Menu);
    until M58 they may remain on Development builds as the only join — this row is waived
    until M58, or done in the same change if M58 is already in. Do not block M59 start on
    M58.
11. **Pause overlay** (Esc, already non-blocking) lists the current **playable** bindings
    from the same `Controls` list the corner panel uses — no DEV section. Prep vs Assault
    changes the list. Header/body/close gutters ≥16. Driven: Esc. Do not `GUILayout` in the
    Bastion HUD. Do not reuse `OptionsScreen.BuildControlRows` (that dumps every context).
    Do not `Time.timeScale`.
12. **End screen:** scrim α ≥ 0.65; verdict and net salvage visible without opening Details;
    ≥16 gutters. Fight cards remain behind the scrim, not beside it.
13. **The player action:** Splash → Ship, open all four consoles. A reader who has not been
    briefed can answer from the panels: where weapons are; that wheel points do not follow a
    new Primary; where the Repair Rig is and that repair costs Salvage; that opening a
    console locks movement; where EMBARK is; that start-Wave is deepest cleared. Then EMBARK.
    A field terminal's desk is below chest and the screen is at eye height. The five HUD
    cards have visible gutters in Prep and Assault. Score lines fully readable. No EXP.
    Esc: pause lists playable keys only. End (quit or die): verdict + net. Development
    player build: F2 does not skip a Wave; corner panel has no F1–F9. Solo unchanged. No
    `[Dev]` in the close log.
14. EditMode / PlayMode: Scale test (criterion 1); HUD AABB on `Bastion.unity` and Ship hosts
    at both resolutions (criteria 2–3, 9); overflow / auto-size (criterion 4); purpose
    strings non-empty and equal the language table; `AppendDevRows` / player-build path has
    no F1–F9. Suites green.

## Implementation

1. **Change** `Assets/Scripts/Editor/Bastion/BastionFieldConsole.cs` — `Scale => 1f`. Comment
   names the rejected formula (`StoreyHeight / 2.5f` → 2) and why (room-fill, not use).
   `ApplyScale` already writes Desk/Screen/Label from `Scale`; keep it. Run the terminals
   request so scene meshes match.
2. **Change** `Assets/Tests/EditMode/Bastion/` — assert Scale and the three vectors in
   criterion 1 (extend `BastionKitFootprintTests` or a sibling). Do not assert storey height
   changed; it stays 5.
3. **Change** `Bastion.unity` fight hosts only as far as criteria 2–4 require. Enable wrap
   and/or auto-size on Score Salvage, Wave Counter, Weapon Name. Hide Profile Exp. Do not
   replace corners with homemade bars (`../../Decisions.md`). No `GUILayout` in the Bastion
   HUD (`../../Architecture.md` gotcha 3).
4. **Create** `Assets/Tests/EditMode/Ui/HudSeparationTests.cs` — parse the five host
   RectTransforms from `Bastion.unity` / `Ship.unity`; pairwise gap ≥24 at the two
   resolutions; shortcut expanded vs Vitals/Weapon; Prep heading vs Hatch. Assert wrap or
   auto-size on the overflow TMP names.
5. **Change** `Ship.unity` station canvases — deactivate or delete `Categories` / `Preview
   Image` on the four station opens (Esc Settings may keep its own). Add or bind a scrim.
   Nudge header/body/close for the 16-unit gutters. Do not clone a new chrome tree. Do not
   revive M46 row-uncloning. Ship HUD hosts: same overflow and Profile Exp hide as Bastion.
6. **Change** `StationPanelView.SetHeader` — add `string purpose`. Bind a TMP under the
   title. Empty purpose logs and fails the test. Shared footer: `No sell-back.` and
   `Opening a console locks movement.`
7. **Change** each station — `Purpose` constant passed at `SetHeader`, matching language.md.
   ARMORY persistent `Upgrades stay on this weapon.` FABRICATION Rig line reads
   `ShipSystems.RepairSalvagePerHp`. NAV MAP start-Wave line names deepest cleared.
8. **Change** `ShortcutPanel.cs` — DEV append is `#if UNITY_EDITOR` for F1–F9. Extract
   `AppendDevRows(..., DevSurface)` with `None | Editor | DevelopmentPlayerJoin` so EditMode
   can exercise the player-build path. Pause always passes `None`. After M58 (or same
   change): no DEV rows outside the editor.
9. **Change** `DevConsole.cs` — poll F1–F9 under `#if UNITY_EDITOR` only. F10/F11 under
   `#if UNITY_EDITOR || DEVELOPMENT_BUILD` until M58, then editor-only. Editor F1 overlay
   unchanged.
10. **Change** `HudController.Pause.cs` / `Bastion.unity` (`Canvas - Pause Menu`) — on
    `OpenPause`, `ShortcutPanel.FillRows` with `FightShortcutContext` and `DevSurface.None`,
    into an authored `ShortcutPanelView` on the pause canvas.
11. **Change** `HudController.Fight` / `ShipHud` — stop pushing `LV 1` / `EXP —`. Hide EXP.
12. **Change** `EndScreenView` / Bastion end canvas — scrim α ≥ 0.65; gutters ≥16.
13. **Create** `Assets/Tests/EditMode/Ship/StationPurposeTests.cs` and extend
    `ShortcutRowApiTests` for `DevSurface.None`.
14. **Change** `../../Play/The-Ship.md` Gaps: strike the rows this slice closes (transfer,
    Rig deal, no sell-back, movement lock, start-Wave vs cleared). Leave Gaps this slice
    does not own (empty Drone slot, ladder preview, mid-Run Rig prompt, old-save VK-7,
    Dev-taint on the hub).
15. **Change** docs: Roadmap row, Changelog. Delete `RepairStationPanels.cs` /
    `StripSettingsToStations.cs` **if they still exist** when this closes (M35 criterion 5
    sentinel). Architecture currently lists them; glob on 2026-08-13 found neither — do not
    recreate them.

Rejected: music / SFX this slice (`M69`). Rejected: Scale 0.75 as a guess — Scale 1 is the
current Ship console the field graybox was copied from; ship-console replacement is M61.
Rejected: rebuilding HUD hosts. Rejected: M46 panel rewrite. Rejected: a tutorial overlay.
Rejected: Input System rebinding. Rejected: a duration floor on the bound wavs.

## Supporting work

- **Driven by** walking to a T / console / Esc — existing Interact and look.
- **Shown by** desk height, HUD gutters, purpose line, pause key list — here.
- **Reads back as** F2 doing nothing in a player build; EditMode Scale + HUD AABB + purpose
  strings — here. Music slider is `M69`.

Guns, zoom, ship desks as art: [`M61`](../3.%20completed/M61-the-polish-pass-2.md). Music and gun/foot SFX:
[`M69`](M69-the-session-has-a-voice.md). Compendium: [`M62`](M62-the-compendium-you-can-open.md).

## Verification

1. Criterion 1, 14 — `Tools/verify/run.sh --filter BastionFieldConsole` (or the new test
   name); then `gkb_verify_request.txt` if scene meshes or MonoBehaviours changed.
2. Criteria 2–5, 9 — HUD AABB and overflow tests green; playthrough at 1920×1080 and
   1280×720.
3. Criteria 6–8, 13 — Splash → Ship. Open all four. Confirm scrim, gutters, purpose, ARMORY
   transfer, FABRICATION Rig + salvage/HP, no-sell-back, movement lock, start-Wave cleared,
   EMBARK. Stand at a field T. Log has no `[Dev]`.
4. Criteria 10–12 — EMBARK, Esc: playable keys, no F1–F9. End: verdict + net, scrim. Development
   player build: F2 does not replay a Wave. Editor: F1 overlay still works.

## Do not build

| Not building | Why not |
|---|---|
| View models, zoom, ship-console art | M61 |
| Fire / footstep / reload SFX, Bastion music | M69 |
| Art pass / new kit meshes | Scale the existing Desk/Screen/Label |
| In-game Compendium | M62 |
| Replacing fight HUD hosts | Locked in Decisions.md |
| Authored-panel rewrite / row un-cloning | M46 cancelled |
| Tutorial / first-run overlay | The panel is the teacher |
| A store SFX pack | M69; 276617 stays rejected |

## Watch

- `ApplyScale` is editor-only. A T placed by hand after this ships still needs the
  terminals request, or it keeps Scale-2 meshes.
- Pause is non-blocking (`../../Decisions.md`, 2026-08-09): listing keys must not touch
  `Time.timeScale`.
- `FillLines` still has a DEV append — port or `#if` it the same way as `FillRows`.
- Countable salvage/HP belongs in the generated Field Manual; the panel *reads* the
  constant.
- NAV MAP already carries `PremiseBrief` (M27). Purpose line is the subject; do not
  replace the premise.
- Widening `Hud Score` leftward can close the Hatch gap; re-run criterion 2 after any
  size change.

## Depends on / Unblocks

Depends on M47 (kit T's exist) and M35 (fight hosts and stations exist). Criterion 10's
F10/F11 row waits on M58. Does not depend on M61 or M69. Unblocks a friends session that
can leave the Ship without a briefing and without being handed god mode. M62's pause
Compendium row wants this pause list already there.
