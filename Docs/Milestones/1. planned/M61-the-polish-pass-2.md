# M61 — Polish Pass 2

<!--
Status: later
Update trigger: when station chrome, pause key list, or player-build F-key policy changes.
Last updated: 2026-08-13
-->

Second friends QoL pass, previously *The Ship is a hull*: the hull as something a stranger can operate. Three surfaces
in one gate — station panels (chrome + the copy the panels still omit), playable keys,
HUD gutters on the Ship — because M59 makes the Bastion a room and then dumps the friend
into Settings leftovers, blank shop headers, and a shortcut strip that still lists god
mode.

Not M46. Authored-panel rewrite stays cancelled. Layout of the four panels does not move;
chrome, copy, and keys do.

## Story

As a player who has never been briefed, I want the Ship's consoles, the key list, and the
hub HUD to be separable and complete, so that I can embark, buy, and pause without a
developer on voice chat.

## Why it matters

M35 closed the station **drive** path and deferred panel *detail*: Settings-shell strip,
row chrome, readability over the live view (`../3. completed/M35-one-look-drawn-from-one-place.md`,
criteria 18–25 soften; Open questions still name a scrim). `Ship.unity` still contains
`Categories` and `Preview Image` objects. `StationPanelView.SetHeader` paints name + banked
+ lifetime and nothing else.

`Play/The-Ship.md` Gaps: upgrades do not transfer; FABRICATION never states the Rig-and-Salvage
deal before embark; nothing says there is no sell-back. Wheel lock copy already ships
(`LOCKED — survive a death first`). Hatch look-at already quotes missing Rig and salvage/HP
(`RepairInteractable.Prompt`) — do not rebuild that prompt.

M15 shipped F1–F9 on the shortcut panel because the person playing was the person
developing (`../3. completed/M15-shortcut-panels.md`). `ShortcutPanel` appends DEV rows
under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, so a Development player build still lists
skip-wave / god / grant-salvage. Pause does not list playable keys at all.

Leverage: **#6, information flows**. Stations, locks, Repair Rig, and bindings already
work. What is missing is readable chrome, the sentence on the panel, and who is told about
F2–F9. Rebinding / Input System is the wrong tier.

## Acceptance criteria

1. **Station chrome is a panel, not a Settings shell.** While ARMORY / CREW QUARTERS /
   FABRICATION / NAV MAP is open, `Categories` and `Preview Image` are inactive or deleted
   on that canvas. Header, body, and close are separate RectTransforms with ≥16 canvas
   units between header bottom and first body row, and between last body row and close.
   A scrim covers the body (existing `BlurManager` idiom named in M35, or one authored
   Image whose alpha is ≥ 0.65). Body TMP remains readable over the live Ship view. No
   `GUILayout`.
2. **Each station panel carries a purpose line** under the station name, the subject from
   `../../_meta/_definitions/language.md` (The stations): ARMORY — the weapon in your hands;
   CREW QUARTERS — you; FABRICATION — the Hatch and the Ship's gear; NAV MAP — where you go,
   and going there. Visible at open. Empty purpose fails the test.
3. **ARMORY states that upgrades stay on that weapon** before the first wheel purchase.
   **FABRICATION states the Repair Rig deal before embark** (Rig required; salvage spent as
   HP is applied; figure from `ShipSystems.RepairSalvagePerHp` at bind time). **No sell-back**
   once, shared on `StationPanelView`. Locked rows keep naming the lock (pin, already ships).
4. **Ship always-on HUD gutters.** The three Ship hosts (`Hud Profile`, `Hud Score`,
   `Hud Weapon` — `../../Decisions.md`) have pairwise AABB gaps ≥24 canvas units at
   1920×1080 and 1280×720. Interact prompt does not intersect `Hud Weapon`.
5. **Player-build keys.** In a player build (Development and Release), `ShortcutPanel` does
   not list F1–F9. **Player builds never bind F2–F9.** Editor Play Mode may still show and
   bind them. After M58, F10/F11 also leave the player-build panel (join is the Main Menu);
   until M58 they may remain on Development builds as the only join — this row is waived
   until M58, or done in the same change if M58 is already in. Do not block M61 start on
   M58.
6. **Pause overlay** (Esc, already non-blocking) lists the current **playable** bindings
   from the same `Controls` list the corner panel uses — no DEV section. Prep vs Assault
   changes the list. Driven: Esc. Do not `GUILayout` in the Bastion HUD. Do not reuse
   `OptionsScreen.BuildControlRows` (that dumps every context).
7. **The player action:** Play → Ship, open all four consoles. A reader who has not been
   briefed can answer from the panels: where weapons are; that wheel points do not follow a
   new Primary; where the Repair Rig is and that repair costs Salvage; where EMBARK is.
   Then EMBARK, Esc: pause lists playable keys only. Development player build: F2 does not
   skip a Wave; corner panel has no F1–F9.
8. EditMode: purpose strings non-empty and equal the language table; `AppendDevRows` /
   player-build path has no F1–F9; HUD AABB on Ship hosts. Suites green. Solo unchanged.

## Implementation

1. **Change** `Ship.unity` station canvases — deactivate or delete `Categories` / `Preview
   Image` on the four station opens (Esc Settings may keep its own). Add or bind a scrim.
   Nudge header/body/close for the 16-unit gutters. Do not clone a new chrome tree. Do not
   revive M46 row-uncloning.
2. **Change** `StationPanelView.SetHeader` — add `string purpose`. Bind a TMP under the
   title. Empty purpose logs and fails the test.
3. **Change** each station — `Purpose` constant passed at `SetHeader`, matching language.md.
   ARMORY persistent `Upgrades stay on this weapon.` FABRICATION Rig line reads
   `ShipSystems.RepairSalvagePerHp`. Shared footer `No sell-back.`
4. **Change** Ship HUD hosts only as far as criterion 4 requires.
5. **Change** `ShortcutPanel.cs` — DEV append is `#if UNITY_EDITOR` for F1–F9. Extract
   `AppendDevRows(..., DevSurface)` with `None | Editor | DevelopmentPlayerJoin` so EditMode
   can exercise the player-build path. After M58 (or same change): no DEV rows outside the
   editor.
6. **Change** `DevConsole.cs` — poll F1–F9 under `#if UNITY_EDITOR` only. F10/F11 under
   `#if UNITY_EDITOR || DEVELOPMENT_BUILD` until M58, then editor-only. Editor F1 overlay
   unchanged.
7. **Change** `HudController.Pause.cs` / `Bastion.unity` (`Canvas - Pause Menu`) — on
   `OpenPause`, `ShortcutPanel.FillRows` with `FightShortcutContext`, into an authored
   `ShortcutPanelView` on the pause canvas. No DEV section.
8. **Create** `Assets/Tests/EditMode/Ship/StationPurposeTests.cs` and extend
   `ShortcutRowApiTests` for `DevSurface.None`. HUD AABB sibling of M59's test, Ship hosts.
9. **Change** `../../Play/The-Ship.md` Gaps: strike the rows this slice closes. Leave
   Gaps this slice does not own (movement lock, empty Drone slot, start-Wave label vs
   cleared, Dev-taint on the hub).
10. **Change** docs: Roadmap row, Changelog. Delete `RepairStationPanels.cs` /
    `StripSettingsToStations.cs` **if they still exist** when this closes (M35 criterion 5
    sentinel). Architecture currently lists them; glob on 2026-08-13 found neither — do not
    recreate them.

Rejected: M46 panel rewrite. Rejected: a tutorial overlay. Rejected: Input System
rebinding. Rejected: a third music clip for the Ship (Bastion track is M59; menu keeps
Stardust Drifter; Ship stays silent this pass). Rejected: putting the Compendium on a
station this slice.

## Supporting work

- **Driven by** walking to a console / Esc — existing.
- **Shown by** purpose line, scrim, pause key list, no F1–F9 — here.
- **Reads back as** those strings, F2 doing nothing in a player build, EditMode — here.

Bastion terminals / fight HUD / level track: [`M59`](M59-the-bastion-is-a-room.md).
Compendium button on pause: [`M62`](M62-the-compendium-you-can-open.md) (this slice lists
keys on pause; M62 adds the Compendium row).

## Verification

1. Criteria 2, 5, 8 — `Tools/verify/run.sh --filter StationPurpose`; ShortcutRow
   `DevSurface.None`; then `gkb_verify_request.txt` if MonoBehaviour binds changed.
2. Criteria 1, 3, 4, 6, 7 — playthrough: Splash → Ship. Open all four. Confirm scrim,
   gutters, purpose, ARMORY transfer, FABRICATION Rig + salvage/HP, no-sell-back, EMBARK.
   EMBARK, Esc: playable keys, no F1–F9. Development player build: F2 does not replay a
   Wave. Editor: F1 overlay still works.

## Do not build

| Not building | Why not |
|---|---|
| Authored-panel rewrite / row un-cloning | M46 cancelled |
| Bastion terminal scale, fight HUD, level track | M59 |
| In-game Compendium | M62 |
| Tutorial / first-run overlay | The panel is the teacher |
| Ship music | Two clips in repo; menu + Bastion already own them |
| Movement-lock warning, Drone-empty, start-Wave label | Remaining Gaps; not this pass |

## Watch

- Prefab `[SerializeField]` on panel TMP: bind in the scene (`../../Architecture.md` gotcha 1).
- Countable salvage/HP belongs in the generated Field Manual; the panel *reads* the
  constant.
- NAV MAP already carries `PremiseBrief` (M27). Purpose line is the subject; do not
  replace the premise.
- Pause is non-blocking: listing keys must not touch `Time.timeScale`.
- `FillLines` still has a DEV append — port or `#if` it the same way as `FillRows`.

## Depends on / Unblocks

Depends on M35 (stations exist) and M15 (the panel). Criterion 5's F10/F11 row waits on
M58. Unblocks a friends session that can leave the Ship without a briefing and without
being handed god mode. M62's pause Compendium row wants this pause list already there.
