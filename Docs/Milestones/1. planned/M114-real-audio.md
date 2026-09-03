# M114 — Real audio replaces the placeholder set

<!--
Status: later
Update trigger: when a licensed or commissioned pack is chosen, or when a new combat
event needs a clip this milestone did not scope
Last updated: 2026-09-01
-->

The nine synthesized placeholder clips bound across `WeaponView` and `CombatSfx` are
replaced by a bought or commissioned pack at the same seams, with the placeholder
files deleted rather than kept as a silent fallback.

## Story

As a player, I want the fire, hit, footstep, reload and Hatch sounds to sound like a
finished game, so that the fight doesn't read as cheap before I've formed any other
opinion of it.

## Why it matters

Zo's priority 6 of six named 2026-08-30 (`Advice.md`): "Fire / hit / foot / Hatch /
UI read as a bought session, not the five wavs bound in `M69`... This priority
reopens the buy gate: pick a pack (or commission), bind, kill the placeholder set.
Do not retune volumes and call it done." `demo-readiness.md` row E2 independently
names audio "the most punished gap" for a horde shooter, citing practitioner
consensus and first-hand report, and its own fix was explicitly scoped as a
placeholder: *"Flag: placeholder quality — swap assets in `Resources/Audio/Sfx`, no
code moves."*

The placeholder set is nine files, all under `Assets/Resources/Audio/Sfx/`, bound at
two seams: `Fire-Pistol.wav` / `Fire-Shotgun.wav` / `Fire-Rifle.wav` / `Reload.wav` /
`Footstep-Walk.wav` (bound in `M69`, read by `WeaponViewModel` and
`FirstPersonController` via `WeaponView`'s path constants), and `Hit-Confirm.wav` /
`Enemy-Down.wav` / `Hatch-Struck.wav` / `Player-Hurt.wav` (bound in the
2026-08-25 demo-readiness pass, read by `CombatSfx`). `CombatSfx.cs`'s own header
already states the trade this milestone completes: *"Replacing a clip is an asset
swap; no code moves."*

Leverage tier (`Working-Rules.md`): **#7, presentation.** No Sim number, no seam and
no verb changes — every one of these nine paths is already wired to a real gameplay
event. This is a content swap, not a build.

## Acceptance criteria

1. All nine placeholder files at `Assets/Resources/Audio/Sfx/` — `Fire-Pistol.wav`,
   `Fire-Shotgun.wav`, `Fire-Rifle.wav`, `Reload.wav`, `Footstep-Walk.wav`,
   `Hit-Confirm.wav`, `Enemy-Down.wav`, `Hatch-Struck.wav`, `Player-Hurt.wav` — are
   replaced with clips from a licensed pack or a commission, at the same filenames
   and the same `Resources.Load<AudioClip>` paths, so `WeaponView.cs` and
   `CombatSfx.cs` need no code change to pick them up.
2. No file in the replacement set is a placeholder-era synthesized clip. A licensing
   note (source, license type, one line) is added to `Docs/Marketing/
   art-commission-brief.md` or a new adjacent doc naming what was bought and under
   what terms, so a future audit can tell licensed audio from a leftover synth clip
   without re-listening to the build.
3. The three fire clips remain distinguishable from each other by class (pistol,
   shotgun, rifle report) — the replacement must not collapse the identity cue
   `WeaponView.FireClip` already gives each weapon class.
4. `CombatSfx`'s min-interval throttling (`HitConfirm` 0.03s, `EnemyDown` 0.06s,
   `HatchStruck` 0.1s, `PlayerHurt` 0.12s) still reads as one event on a 40-body wave
   dying at once, not a stacked blast — the new clips must not be so long or so loud
   that the existing throttle stops doing its job.
5. A fresh player-extract build (Release-Gate item 16 / 23: "Audio on another
   machine, including device change after launch") plays every one of the nine
   sounds audibly on a machine that never had the editor installed — the exact gap
   Advice.md's debt table names under this priority: *"Confirm FX + cursor capture
   on fresh player extract | Silent guns / stuck cursor on first guest launch."*

## Implementation

**Buy or commission** the pack. `demo-readiness.md`'s own sourcing note names the
options: an asset-store SFX pack scoped to sci-fi/combat, or a commission matching
`art-commission-brief.md`'s existing brief format. Either way the deliverable is nine
`.wav` files (or a superset the implementer trims to nine) matching the existing
filenames.

**Change** `Assets/Resources/Audio/Sfx/*.wav` — replace the nine files in place.
**No `.cs` file changes.** `WeaponView.cs`'s five `const string` paths and
`CombatSfx.cs`'s four `const string` paths already point at these exact names; a
same-name replacement is picked up by `Resources.Load` with nothing recompiled.

**Change** `Docs/Marketing/art-commission-brief.md` (or a new `Docs/Marketing/
audio-sourcing.md` if the licensing note doesn't fit the existing brief's shape) —
criterion 2's provenance line.

**Delete** nothing in code. This milestone's entire implementation is an asset swap;
if it grows a `.cs` change beyond binding a tenth event this milestone didn't scope,
that change belongs to a different milestone, not folded in here.

Rejected: **retuning `WeaponView.SfxOneShotScale` (0.5) or the per-event min-interval
constants as part of this milestone.** Advice.md says so directly — "do not retune
volumes and call it done" — and a volume retune on synthesized placeholders answers
nothing about whether the replacement pack needs the same numbers. Re-tune only if
the new clips' own mix demands it, in a commit that says so.

## Verification

1. Criterion 1 — `ls Assets/Resources/Audio/Sfx/*.wav` names the same nine files;
   `git diff --stat` on this milestone's commit shows only those nine paths changed
   plus the doc from criterion 2.
2. Criterion 2 — the licensing line exists and names source + license.
3. Criteria 3–4 — playthrough path: from Play, EMBARK, fire each of the three
   Primaries in turn with sound on (Release-Gate item 16), confirm each is
   identifiable by ear; let a group of bodies die together and confirm one
   `Enemy-Down` read, not a stacked blast.
4. Criterion 5 — Release-Gate item 16 and item 23 re-run on an extracted build on a
   non-build machine: audio plays, guns are not silent, no `[Dev]` line. This is the
   same class of check `M109`'s stamp run already exercises; ride the same session
   rather than a dedicated one if one is already scheduled.
5. Suites green, same names — this milestone changes no code path a test asserts on.

## Watch

- `CombatSfx`'s `Missing clips log once and go quiet` behavior (its own header
  comment) means a typo'd filename fails silently in play, not loudly. Confirm each
  of the nine loads with no `Missing SFX clip:` warning in the log before calling
  criterion 1 closed.
- `AudioBuses` classifies sources by GameObject name (`CombatSfx.Spawn`'s "must NOT
  read as Music" comment) — a replacement pipeline that changes how the source
  `GameObject` is named breaks bus routing silently. Keep the existing spawn path.

## Depends on / Unblocks

Independent of everything else in the current queue. Rides `M109`'s existing
fresh-extract verification session for criterion 5 rather than requiring its own.