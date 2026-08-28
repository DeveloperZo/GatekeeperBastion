# M69 — The session has a voice

<!--
Status: completed 2026-08-17
Update trigger: closed
Last updated: 2026-08-17
Closed on: 2026-08-17. Machine: SessionMusicHostTests 4/0 Passed (Ship/Bastion
Level Music → Whispers guid 482f8aea…; Splash/MainMenu Background Music → Stardust;
five SFX wavs + both mp3s on disk; reload/footstep first-sample fade). Code already
in tree: LevelMusic.CaptureOrJoin on Ship/Bastion; FrontEndMusic.Stop on Continue/
Start/Host/Join; WeaponViewModel fire/reload + FPC footsteps on Sfx* hosts;
AudioBuses name-classifies Music vs SFX. Criterion 4 slider independence playthrough
is confirmation (same as M59/M62) — not claimed played this close. No store pack.
M66 keeps pre-attack tells / Hatch hit / downed cue.
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

`Design/1-Experience.md` §1.2 ranked Sensation incomplete: no audio pass. The menu
loop **Stardust Drifter** stops on Continue/Start New (`FrontEndMusic.Stop`). Ship and
Bastion play **Whispers of the Void** (`Assets/Resources/Audio/`, 5.50 MB, 229 s). Fire /
footsteps call `PlayOneShot` against `Assets/Resources/Audio/Sfx/` (Fire-Pistol, Fire-Rifle,
Fire-Shotgun, Reload, Footstep-Walk). `WeaponView.SfxOneShotScale` is **0.5**.

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

Shipped ahead of formal close (Changelog 2026-08-15/16): `LevelMusic`, Ship/Bastion
`Level Music` hosts, `FrontEndMusic.Stop` on menu net paths, SFX one-shots and
`AudioBuses`. Close is the EditMode gate + Sensation stamp.

Rejected: playing Stardust on Ship/Bastion. Rejected: composing a new track.
Rejected: Sound Design Starter Kit **276617**. Rejected: FMOD. Rejected: M66 tells.

## Supporting work

- **Driven by** EMBARK / LMB / walk / Esc Settings — existing.
- **Shown by** audible loop and fire — here.
- **Reads back as** Music slider moving the Bastion loop; SFX slider moving fire and
  footsteps — here.

UI / terminals / keys: [`M59`](M59-the-polish-pass-1.md). View-models and zoom:
[`M61`](M61-the-polish-pass-2.md). Hit confirm and per-kind tells:
[`M66`](../4.%20deferred/M66-polish-pass-3.md).

## Verification

1. Criteria 1, 5 — `SessionMusicHostTests` 4/0 Passed (Unity EditMode job
   `1b0348c2ef1d49568ea59247102e3b17`, 2026-08-17).
2. Criteria 2–3 — code path: `WeaponViewModel` / `FirstPersonController` PlayOneShot on
   `Sfx*` hosts; `AudioBuses.NameReadsAsMusic` excludes them; no store pack in Resources.
3. Criterion 4 — confirmation playthrough (slider independence); same stamp pattern as
   M59/M62. Not claimed played this close.

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

Closed after M59/M61 shipped the room and the gun presentation. Unblocks a friends session
that is not silent air. M66 owns remaining combat audio tells.
