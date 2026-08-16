# M69 — The session has a voice

<!--
Status: later
Update trigger: when the Bastion music host, bound SFX set, or the no-pack reversal fires.
Last updated: 2026-08-14
-->

After EMBARK a track is playing, and fire / reload / walk are audible on the SFX slider.
Menu is **Stardust Drifter**. Ship and Bastion are **Whispers of the Void**. Split out of M59 so
the UI pass is not also an audio pass.

No store SFX pack. Bind the five wavs and two mp3s already in the repo. Shift Hover/Click
stay the UI bus. Pre-attack tells, Hatch hit, and downed cue stay `M66`.

## Story

As a Boarder, I want the Bastion to have a looping track and the gun in my hands to make
sound on the SFX bus, so the session is not silent once the menu ends.

## Why it matters

`Design/1-Experience.md` §1.2 ranks Sensation as not addressed: no audio pass. The menu
loop **Stardust Drifter** stops on Continue/Start New (`FrontEndMusic.Stop`). Ship and
Bastion play **Whispers of the Void** (`Assets/Resources/Audio/`, 5.50 MB, 229 s). `PlayerGun.Fire` / footsteps already call `PlayOneShot` against
`Assets/Resources/Audio/Sfx/` (Fire-Pistol **4012 B / 0.090 s**, Fire-Rifle **0.070 s**,
Fire-Shotgun **0.180 s**, Reload **0.220 s**, Footstep-Walk **0.080 s**, all PCM 16-bit
mono 22050 Hz). `WeaponView.SfxOneShotScale` is **0.5** because those bursts were harsh at
bus 0.9.

Leverage: **#8, information flows**. No damage, spread, or range number moves. A store
library is the 5 GB mistake again.

## Acceptance criteria

1. **Whispers plays on Ship and Bastion.** Host name **Level Music** (not `Background Music`).
   Clip is **Whispers of the Void** (`Assets/Resources/Audio/Whispers of the Void.mp3`,
   guid `482f8aea…`), looped, 2D, volume from `SettingId.MusicVolume`. `FrontEndMusic.Stop`
   runs on Continue / Start / Host / Join. Ship and Bastion each author a scene copy that
   `LevelMusic.CaptureOrJoin` joins or yields. Direct-play adopts the scene source.
   Returning to MainMenu stops Level Music; Stardust starts again. Both must not play at once.
2. **Fire, reload, and footsteps ride the SFX slider.** `PlayerGun.Fire` plays the class
   one-shot (pistol / shotgun / rifle). Reload plays `Reload`. Walk/sprint footsteps on the
   existing clock (`WeaponView.WalkFootstepSeconds` / `SprintFootstepSeconds`). Clips are
   the five Resource names in `WeaponView`. Source GameObject names do not contain `Music`.
   SFX slider to 0 silences them; Music slider does not.
3. **No store SFX pack imported.** Shift `Click.ogg` / `Hover.ogg` remain the UI bus
   (`UiPitchRandomizer` / UIManager). No FMOD. No second DDOL music host that fights
   FrontEndMusic across Splash.
4. **The player action:** Splash → Ship → EMBARK. Whispers is playing on Ship and on
   Bastion; Music slider in pause Settings moves it. Fire and walk are heard. SFX 0
   silences shots and footsteps; Music 0 silences the loop independently. QUIT TO MENU:
   Stardust, not both. Solo unchanged. No `[Dev]` in the close log.
5. EditMode: Ship and Bastion YAML host `Level Music` bound to Whispers; Splash/MainMenu
   host `Background Music` bound to Stardust; SFX clips load from Resources. Suites green.

## Implementation

1. `FrontEndMusic.Stop` on Continue / Start / Host / Join. ShipController and
   GameController `LevelMusic.CaptureOrJoin(null)`. Clip via authored reference on each
   scene's `Level Music`. `LevelMusic.Stop` on load to MainMenu / Splash.
2. Fire / reload / footsteps already play from `WeaponViewModel` / `FirstPersonController`.
   Confirm source names omit `Music` and `AudioBuses.ApplyFromSettings` classifies them as
   SFX. Do not replace the five wavs this slice.
3. **Change** docs: Roadmap row, Changelog. `Design/1-Experience.md` Sensation row in the
   **close** commit, not before.

Rejected: playing Stardust on Ship/Bastion. Rejected: composing a new track.
Rejected: Sound Design Starter Kit **276617** (5.31 GB / 1173 files). Rejected: importing
328565 / 258432 / 126448 / 123544 this slice. Rejected: a duration floor on the bound wavs
(feel proxy; reversal below). Rejected: FMOD. Rejected: M66 tells.

## Supporting work

- **Driven by** EMBARK / LMB / walk / Esc Settings — existing.
- **Shown by** audible loop and fire — here.
- **Reads back as** Music slider moving the Bastion loop; SFX slider moving fire and
  footsteps — here.

UI / terminals / keys: [`M59`](../3.%20completed/M59-the-polish-pass-1.md). View-models and zoom:
[`M61`](../3.%20completed/M61-the-polish-pass-2.md). Hit confirm and per-kind tells:
[`M66`](M66-polish-pass-3.md).

## Verification

1. Criteria 1, 5 — EditMode Whispers host on Ship and Bastion; playthrough Splash → Ship →
   EMBARK, hear Whispers; Music 0 silences it; QUIT TO MENU, Stardust, not both.
2. Criteria 2–4 — fire and walk audible; SFX 0 / Music unchanged; Music 0 / SFX unchanged.
   Log has no `[Dev]`.

## Watch

- Prefab `[SerializeField]` on an authored Bastion AudioSource: bind the clip in the
  scene (`../../Architecture.md` gotcha 1).
- Scene copies of `Level Music` must yield to the DDOL host; PlayOnAwake stays off.
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
