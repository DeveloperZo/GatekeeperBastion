# M69 — The session has a voice

<!--
Status: later
Update trigger: when the Bastion music host, bound SFX set, or the no-pack reversal fires.
Last updated: 2026-08-14
-->

After EMBARK a track is playing, and fire / reload / walk are audible on the SFX slider.
Menu track stays Stardust Drifter. Both must not play at once. Split out of M59 so the UI
pass is not also an audio pass.

No store SFX pack. Bind the five wavs and two mp3s already in the repo. Shift Hover/Click
stay the UI bus. Pre-attack tells, Hatch hit, and downed cue stay `M66`.

## Story

As a Boarder, I want the Bastion to have a looping track and the gun in my hands to make
sound on the SFX bus, so the session is not silent once the menu ends.

## Why it matters

`Design/1-Experience.md` §1.2 ranks Sensation as not addressed: no audio pass. The menu
loop **Stardust Drifter** stops on Continue/Start New (`FrontEndMusic.Stop`). Bastion never
starts a replacement. **Whispers of the Void** sits unused (`Assets/Resources/Audio/`,
5.50 MB, 229 s). `PlayerGun.Fire` / footsteps already call `PlayOneShot` against
`Assets/Resources/Audio/Sfx/` (Fire-Pistol **4012 B / 0.090 s**, Fire-Rifle **0.070 s**,
Fire-Shotgun **0.180 s**, Reload **0.220 s**, Footstep-Walk **0.080 s**, all PCM 16-bit
mono 22050 Hz). `WeaponView.SfxOneShotScale` is **0.5** because those bursts were harsh at
bus 0.9.

Leverage: **#8, information flows**. No damage, spread, or range number moves. A store
library is the 5 GB mistake again.

## Acceptance criteria

1. **Bastion plays a looping track.** On Bastion load, an `AudioSource` named so
   `AudioBuses.IsMusic` matches it (name contains `Music`) plays **Whispers of the Void**
   (`Assets/Resources/Audio/Whispers of the Void.mp3`, guid `482f8aea…`), looped, 2D,
   volume from `SettingId.MusicVolume`. Host name **Level Music** (not `Background Music`).
   Menu track stays Stardust Drifter. Both must not play at once: `FrontEndMusic.Stop`
   still runs on Continue/Start New; the Bastion source stops when Bastion unloads or Main
   Menu captures again.
2. **Fire, reload, and footsteps ride the SFX slider.** `PlayerGun.Fire` plays the class
   one-shot (pistol / shotgun / rifle). Reload plays `Reload`. Walk/sprint footsteps on the
   existing clock (`WeaponView.WalkFootstepSeconds` / `SprintFootstepSeconds`). Clips are
   the five Resource names in `WeaponView`. Source GameObject names do not contain `Music`.
   SFX slider to 0 silences them; Music slider does not.
3. **No store SFX pack imported.** Shift `Click.ogg` / `Hover.ogg` remain the UI bus
   (`UiPitchRandomizer` / UIManager). No FMOD. No second DDOL music host that fights
   FrontEndMusic across Splash.
4. **The player action:** Splash → Ship → EMBARK. A track is playing; Music slider in pause
   Settings moves it. Fire and walk are heard. SFX 0 silences shots and footsteps; Music 0
   silences the loop independently. QUIT TO MENU: Stardust Drifter, not both. Solo
   unchanged. No `[Dev]` in the close log.
5. EditMode: a Bastion music host is classified as Music; stopping FrontEndMusic does not
   leave two `*Music*` sources playing; SFX clips load from Resources. Suites green.

## Implementation

1. **Create** `Assets/Scripts/Game/Flow/LevelMusic.cs` — same capture/join/stop shape as
   `FrontEndMusic`, host name **Level Music**. `CaptureOrJoin` on Bastion start
   (`GameController` or a small scene-local source authored in `Bastion.unity`). Clip via
   authored reference on that source (prefab freeze: push or author in the scene, do not
   hope a code default plays). `Stop` when returning to Main Menu (`SceneFlow.LoadMainMenu`
   / end-screen return).
2. **Change** `AudioBuses` only if `IsMusic` would miss **Level Music** — it already
   matches `"Music"` in the GameObject name.
3. Fire / reload / footsteps already play from `WeaponViewModel` / `FirstPersonController`.
   Confirm source names omit `Music` and `AudioBuses.ApplyFromSettings` classifies them as
   SFX. Do not replace the five wavs this slice.
4. **Change** docs: Roadmap row, Changelog. `Design/1-Experience.md` Sensation row in the
   **close** commit, not before.

Rejected: composing a new track. Rejected: playing Stardust Drifter in the Bastion.
Rejected: Sound Design Starter Kit **276617** (5.31 GB / 1173 files). Rejected: importing
328565 / 258432 / 126448 / 123544 this slice. Rejected: a duration floor on the bound wavs
(feel proxy; reversal below). Rejected: FMOD. Rejected: M66 tells.

## Supporting work

- **Driven by** EMBARK / LMB / walk / Esc Settings — existing.
- **Shown by** audible loop and fire — here.
- **Reads back as** Music slider moving the Bastion loop; SFX slider moving fire and
  footsteps — here.

UI / terminals / keys: [`M59`](M59-the-polish-pass-1.md). View-models and zoom:
[`M61`](../3.%20completed/M61-the-polish-pass-2.md). Hit confirm and per-kind tells:
[`M66`](M66-polish-pass-3.md).

## Verification

1. Criteria 1, 5 — EditMode music-host classification; playthrough EMBARK, hear Whispers of
   the Void; Music 0 silences it; QUIT TO MENU, Stardust Drifter, not both.
2. Criteria 2–4 — fire and walk audible; SFX 0 / Music unchanged; Music 0 / SFX unchanged.
   Log has no `[Dev]`.

## Watch

- Prefab `[SerializeField]` on an authored Bastion AudioSource: bind the clip in the
  scene (`../../Architecture.md` gotcha 1).
- Level Music host name must not be `Background Music` or FrontEndMusic will steal or
  destroy it.
- Pause is non-blocking: music keeps playing; do not `timeScale` it off.
- Shift `Background.ogg` must not replace Stardust. `Start.ogg` is unused PCM misnamed
  `.ogg`.

## Do not build

| Not building | Why not |
|---|---|
| A store SFX pack | No sub-150 MB listing covers ballistic pistol/shotgun/rifle + metal feet + three android tells |
| UI click pack | Shift Hover/Click already bound |
| Per-kind Gatekeeper tells, Hatch hit, downed cue | M66 |
| A third music clip | Two tracks already in the repo |

## What would reverse the no-pack call

1. A friends session **after** M66 visual hit-confirm, SFX up, players still say the guns
   feel weak — then replace the five wavs with **named** clips from 328565 or 258432, not
   the whole package.
2. M66 off-screen kind ID fails with Kenney/CC0 — then 260624 (104.7 MB robotic) or three
   clips from 123544. Still not 276617.
3. A single sub-150 MB listing that names pistol + shotgun + rifle + reload + metal feet +
   three robot attack tells + hatch hit + downed. None of the 2026-08-14 pages did.

## Depends on / Unblocks

Better after M59 (HUD gutters, pause Settings) but not blocked on it. Does not wait on M61
meshes — the one-shots already fire. Unblocks a friends session that is not silent air.
