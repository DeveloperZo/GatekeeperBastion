# M59 — Polish Pass 1

<!--
Status: later
Update trigger: when field-console scale, fight-HUD hosts, or Bastion music ownership changes.
Last updated: 2026-08-13
-->

First friends QoL pass, previously *The Bastion is a room*: the Bastion as a place a person stands in. Three surfaces in
one gate — field terminals, fight HUD separation, a looping track — because a friend who
can join still walks into furniture taller than they are, overlapping chrome, and silence.
`../../_meta/_definitions/milestone.md` forbids a phase with no end state. This pass has
one: those three surfaces, numbered below. It is not an art/audio phase.

Keys, station chrome, and the Compendium are **not** this slice (`M61`, `M62`).

## Story

As a Boarder, I want the Bastion's consoles, HUD, and sound to fit a person in a room, so
that the fight reads as a place rather than a graybox scaled to fill the storey.

## Why it matters

Friends alpha asks whether someone who is not Zo wants to stay in the session. The Bastion
is the session. Three measured holes, same room:

| Surface | Evidence | Now |
|---|---|---|
| Field terminals | `BastionFieldConsole.Scale` = `BastionKit.StoreyHeight / 2.5`. Storey is **5** (`BastionKitFootprintTests.StoreyHeight_IsFive`). Scale = **2**. Desk cube `3.2 × 2.4 × 1.4` m, centre Y `1.2` → top at **2.4 m**. | Taller than a standing Boarder. Authored to "read in Full/Open rooms", not to be used. |
| Fight HUD | Locked hosts in `../../Decisions.md` (2026-08-10). M35 required ≥24 canvas units from **screen edges**. Pairwise gaps between cards, shortcut vs vitals, Prep vs Hatch were not gated. | Cards and strips sit on top of each other depending on resolution and drawer state. |
| Music | Splash/MainMenu loop **Stardust Drifter** (`FrontEndMusic`, guid `baf9676f…`). `MainMenuController` calls `FrontEndMusic.Stop()` on Continue and Start New. Bastion never starts a replacement. **Whispers of the Void** sits unused in `Assets/Resources/Audio/`. | The level is silent. Music volume in Settings has nothing to drive once the menu ends. |

Leverage: **#8, information flows** for HUD; **#10, structure** for terminal scale (the
room-fill multiplier, not a texture); music is a missing driven surface, not a mix pass.
A higher tier (new kit geometry, new HUD hosts) is wrong: the hosts and the Desk/Screen/Label
recipe stay; they get a human scale and a gap rule.

## Acceptance criteria

1. **`BastionFieldConsole.Scale` is `1f`.** The StoreyHeight/2.5 multiplier is gone. At
   Scale 1 the desk cube is `1.6 × 1.2 × 0.7` m, centre Y `0.6` (top **1.2 m**); screen
   centre Y **1.7 m**. EditMode asserts `Scale == 1f` and those three vectors. Re-apply
   via existing `ApplyScale` / `gkb_bastion_terminals_request.txt` so every authored T in
   `Bastion.unity` matches; do not leave old Scale-2 meshes in the scene.
2. **Always-on fight cards do not overlap.** The five locked hosts (`Hud Profile`,
   `Hud Score`, `Hud Hatch`, `Hud Vitals`, `Hud Weapon` — `../../Decisions.md`) have pairwise
   AABB gaps ≥ **24** canvas units at 1920×1080 and 1280×720. Edge gap ≥24 stays (M35).
   Hosts are not rebuilt; they may move by that gap. Shift Icon Border + Slider + Text rows
   stay intact.
3. **Shortcut and Prep do not sit on those cards.** Expanded shortcut AABB does not
   intersect `Hud Vitals` or `Hud Weapon`. Prep/Assault heading sits ≥24 below `Hud Hatch`.
   Interact prompt stays centre; it does not intersect `Hud Vitals`.
4. **Bastion plays a looping track.** On Bastion load, an `AudioSource` named so
   `AudioBuses.IsMusic` matches it (name contains `Music`) plays **Whispers of the Void**
   (`Assets/Resources/Audio/Whispers of the Void.mp3`, guid `482f8aea…`), looped, 2D,
   volume from `SettingId.MusicVolume`. Menu track stays Stardust Drifter. Both must not
   play at once: `FrontEndMusic.Stop` still runs on Continue/Start New; the Bastion source
   stops when Bastion unloads or Main Menu captures again.
5. **The player action:** Splash → Ship → EMBARK. A field terminal's desk is below chest
   and the screen is at eye height. The five HUD cards have visible gutters between them in
   Prep and Assault. A track is playing; Music slider in pause Settings moves it. Solo
   unchanged. No `[Dev]` in the close log.
6. EditMode / PlayMode: Scale test (criterion 1); HUD AABB test on `Bastion.unity` at both
   resolutions (criteria 2–3); a test that a Bastion music host is classified as Music and
   that stopping FrontEndMusic does not leave two `*Music*` sources playing. Suites green.

## Implementation

1. **Change** `Assets/Scripts/Editor/Bastion/BastionFieldConsole.cs` — `Scale => 1f`. Comment
   names the rejected formula (`StoreyHeight / 2.5f` → 2) and why (room-fill, not use).
   `ApplyScale` already writes Desk/Screen/Label from `Scale`; keep it. Run the terminals
   request so scene meshes match.
2. **Change** `Assets/Tests/EditMode/Bastion/` — assert Scale and the three vectors in
   criterion 1 (extend `BastionKitFootprintTests` or a sibling). Do not assert storey height
   changed; it stays 5.
3. **Change** `Bastion.unity` fight hosts only as far as criteria 2–3 require. Do not
   replace corners with homemade bars (`../../Decisions.md`). No `GUILayout` in the Bastion
   HUD (`../../Architecture.md` gotcha 3).
4. **Create** `Assets/Tests/EditMode/Ui/HudSeparationTests.cs` (or PlayMode if the scene
   must be open) — load or parse the five host RectTransforms; pairwise gap ≥24 at the two
   resolutions; shortcut expanded vs Vitals/Weapon; Prep heading vs Hatch.
5. **Create** `Assets/Scripts/Game/Flow/LevelMusic.cs` — same capture/join/stop shape as
   `FrontEndMusic`, host name **Level Music** (not `Background Music`, which is the menu
   host). `CaptureOrJoin` on Bastion start (`GameController` or a small scene-local source
   authored in `Bastion.unity`). Clip via `Resources.Load<AudioClip>("Audio/Whispers of the Void")`
   or an authored reference on that source; authored reference preferred (prefab freeze:
   push or author in the scene, do not hope a code default plays). `Stop` when returning
   to Main Menu (`SceneFlow.LoadMainMenu` / end-screen return). Do not `DontDestroyOnLoad`
   a second host that fights FrontEndMusic across Splash.
6. **Change** `AudioBuses` only if `IsMusic` would miss **Level Music** — it already
   matches `"Music"` in the GameObject name. Do not add FMOD.
7. **Change** docs: Roadmap row, Changelog. `Play/The-Game.md` "What it looks like right
   now" still says gray boxes until this closes; update that sentence in the same commit
   as close, not before.

Rejected: composing a new track. Rejected: playing Stardust Drifter in the Bastion (that
clip is the menu identity; Splash already uses it). Rejected: Scale 0.75 as a guess —
Scale 1 is the Ship console the field graybox was copied from; if it is still large after
play, that is a one-lever retune of `Scale`, not a second pass. Rejected: rebuilding HUD
hosts.

## Supporting work

- **Driven by** walking to a T / standing in Prep — existing Interact and look.
- **Shown by** desk height, HUD gutters, audible loop — here.
- **Reads back as** Music slider moving the Bastion loop; EditMode Scale + HUD AABB — here.

Keys / F2–F9: [`M61`](M61-the-ship-is-a-hull.md). Station chrome and copy: M61. Compendium:
[`M62`](M62-the-compendium-you-can-open.md).

## Verification

1. Criterion 1, 6 — `Tools/verify/run.sh --filter BastionFieldConsole` (or the new test
   name); then `gkb_verify_request.txt` if scene meshes or MonoBehaviours changed.
2. Criteria 2–3 — HUD AABB test green; playthrough at 1920×1080 and 1280×720: no card
   overlapping another, shortcut off vitals, Prep below Hatch.
3. Criterion 4 — playthrough: EMBARK, hear Whispers of the Void; Esc Settings, Music to 0,
   silence; Music up, track returns; QUIT TO MENU, Stardust Drifter, not both.
4. Criterion 5 — playthrough path: Splash → Continue → Ship → NAV MAP → EMBARK. Stand at a
   field T. Log has no `[Dev]`.

## Do not build

| Not building | Why not |
|---|---|
| Art pass / new kit meshes | Scale the existing Desk/Screen/Label |
| New music composition | Unused clip is already in the repo |
| Player-build key strip / unbinding F2–F9 | M61 |
| Station panel chrome or copy | M61 |
| In-game Compendium | M62 |
| Replacing fight HUD hosts | Locked in Decisions.md |

## Watch

- `ApplyScale` is editor-only. A T placed by hand after this ships still needs the
  terminals request, or it keeps Scale-2 meshes.
- Prefab `[SerializeField]` on an authored Bastion AudioSource: bind the clip in the
  scene (`../../Architecture.md` gotcha 1).
- Level Music host name must not be `Background Music` or FrontEndMusic will steal or
  destroy it.
- Pause is non-blocking (`../../Decisions.md`, 2026-08-09): music keeps playing.

## Depends on / Unblocks

Depends on M47 (kit T's exist) and M35 (fight hosts exist). Does not depend on M58.
Unblocks a friends session that does not look like a scaled-up prop dump. M61 can start
without this; order is room then hull then Compendium.
