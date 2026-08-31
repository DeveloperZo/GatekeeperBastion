# M104 — The ship wears the look

<!--
Status: next (current)
Update trigger: when the Ship hull changes shape, when the glow band moves, or when the look
is carried into the Bastion.
Last updated: 2026-08-26
-->

The Ship hub stops being an untinted Synty kit and becomes the game's own interior: near-black
architecture, districts of coloured light on the walls, a tinted mirror deck, and slow motes in
the air. The look was settled in `Assets/_Scratch/` against a walkable gallery; this milestone
lands it on one real scene and nothing else.

## Story

As a Boarder between runs, I want the Ship to look like somewhere I live rather than a kit
demo, so that returning to it reads as coming home and not as leaving the game.

## Why it matters

`Docs/Marketing/art-commission-brief.md:7-9` states the target in Zo's words — neon as the only
thing showing the way, a tight palette, wet floors doing the reflecting. Nothing in the build
does that yet: `Ship - CoOp.unity` greps **1046 renderers, 1 realtime light, 0 reflection
probes**, and `Brainstorms/ship-hull-and-interior.md:94-103` records the failure mode as ours —
"a light on every fixture, nothing dark, so nothing reads as bright."

The look itself is settled and walkable, not proposed: the vignette gallery converged on
Skyline's districts, Swarm's mirror floor and splash-grammar motes. This milestone is the
carry-over, not the exploration.

**Leverage tier**: this is a *structure* change (what emits light and where), not a parameter
tune. The parameter changes it forces — the glow band — are listed and priced below.

## Decisions

Made 2026-08-26, so no implementer has to choose:

| Question | Decision | Rejected because |
|---|---|---|
| Where do light pools come from? | **Baked lightmaps.** Emissive district fixtures become real light with bounce; runtime light count stays at 1. | Raising the realtime budget re-opens what `M61-2:155` closed, and 6 lights/room is what crashed the scratch scene at scale. |
| How do per-hue fixtures avoid killing batching? | **A small shared fixture set** — one material per cast hue on dedicated dash-row meshes. The atlas is never instanced. | Property blocks do not feed the lightmapper, so they cannot combine with the bake. Restyling the atlas makes district zoning impossible. |
| Districts at 3.0 vs architecture ceiling 1.4? | **Raise the enemy band** to 3.4–5.1 so Gatekeepers stay hotter than the room. Triples are *scaled*, not replaced, so family hue ratios survive. | Capping districts at 1.4 was recommended and declined; Zo wants the vignette preserved. |
| Which scene first? | **Ship hub only.** The Bastion is out of scope. | Combat legibility risk belongs in its own milestone with the far-sightline check as its gate. |

## Acceptance criteria

1. `Ship - CoOp.unity` has **at most 1 realtime light** after the pass — the same count it has
   today. Baked lighting supplies everything else.
2. Every renderer in the Ship's dressed geometry still resolves to a **shared** material; a
   grep for material instances in the scene returns none.
3. The Ship carries **≤ 6 district fixture materials**, one per cast hue, each shared across
   every room that uses that hue.
4. The deck reads as a tinted mirror: reflection is present and takes its colour from the
   palette rather than from a neutral grey.
5. Motes are present and moving: **40 per major space**, alpha 0.32, rising, at the splash
   values (`Assets/Scenes/Splash.unity`, `Glow Motes`).
6. `SceneBuilder.CreatePalette()` authors the new band for all four in-build kinds, and a
   Build All has written it through to their materials. `glow-range.md` already records it.
7. A Gatekeeper standing in the dressed Ship is **identifiable by family at the far end of the
   longest sightline**, per `Design/2-Dynamics.md:59-62`. This is the gate; if it fails, the
   district peak comes down rather than the enemy band going up further.
8. Frame time at 1080p is recorded before and after. No target — the number is the baseline the
   Bastion pass will argue from, which `M61-2:165-169` still owes.

## Implementation

**Create** `Assets/Scripts/Editor/Scenes/ShipLookPass.cs` — a committed one-shot editor tool,
menu `GatekeepersProtocol/Build/Ship Look Pass`. It reads a `ShipLookProfile` asset and writes authored
scene content: fixture rows, deck material, mote emitters, probe and probe-group placement. It
does **not** run at runtime and nothing reconstructs it on `Awake`
(`gkb-development`, *Never §1*).

**Create** `Assets/Settings/ShipLookProfile.asset` — the palette and fixture numbers as one
asset: body tint, the cast hues, district rows/falloff/peak, deck tint and gloss, mote count,
colour, alpha, rise. Ported from the converged `MyShipLook.asset` in `_Scratch`.

**Create** `Assets/Packs/ShipFixtures/` — written by the pass on first run: the dash-row meshes
(one per rounded wall length, with lightmap UVs), the six shared emissive materials, and the
mote texture. The pass generates them itself rather than importing from `_Scratch`, so nothing
in the gitignored folder is a dependency and the Bastion pass can reuse the same code.

**Change** `Assets/Scenes/Ship - CoOp.unity` — dressed by the tool, then hand-finished. Per
`Working-Rules.md:196-208` the tool seeds and Zo hand-moves; that split is the workflow.

**Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.cs`, `CreatePalette()` — the band moves,
and this is the only place it can move from. `EnsureWrapperEmission` writes the palette into
each kind's material on Build All, so **a value typed into a `.mat` is lost**
(`glow-range.md`, preamble). Four kinds are in build: Default, Hunter, Thief, Cloaker.

Note what this really is: the tree has **never worn HDR**. Every in-build kind sits at or under
peak 1.0 today, so this is not a band being nudged — it is LDR → 4-odd in one step, at values
chosen to clear district fixtures at 3.0. Expect the first look to be too hot and tune down.
The Cloaker keeps its deliberate place under the band (3.38) because it is meant to be missed,
and its contested cold-teal hue is **not** resolved here.

**Change** `Docs/Design/glow-range.md` — already done, 2026-08-26. Scaled ×1.8, no floor:

| Kind | Old peak | New `_EmissionColor` | New peak |
|---|---|---|---|
| Default | 2.23 | `r: 4.018, g: 1.825, b: 1.526` | 4.02 |
| Hunter | 2.58 | `r: 4.653, g: 1.161, b: 0.232` | 4.65 |
| Exploder | 2.82 | `r: 5.076, g: 2.284, b: 0.000` | 5.08 |
| Standoff Shooter | 2.35 | `r: 4.230, g: 0.508, b: 0.842` | 4.23 |
| Ailment Shooter | 2.35 | `r: 4.230, g: 0.846, b: 2.369` | 4.23 |
| Source | 2.47 | `r: 0.445, g: 4.442, b: 0.484` | 4.44 |
| Enabler | 2.35 | `r: 1.690, g: 4.230, b: 0.931` | 4.23 |
| Alarmer | 2.70 | `r: 0.243, g: 4.864, b: 1.676` | 4.86 |
| Thief | 2.35 | `r: 2.639, g: 0.846, b: 4.230` | 4.23 |
| Reclaimer | 2.23 | `r: 3.371, g: 1.206, b: 4.018` | 4.02 |
| Bypasser | 2.35 | `r: 1.588, g: 0.508, b: 4.230` | 4.23 |
| Cloaker | 1.88 | `r: 1.516, g: 1.354, b: 3.384` | 3.38 |

Only the four in build — `Default`, `Hunter`, `Thief`, `Cloaker` — are applied, via
`CreatePalette` plus a Build All. The other eight are proposals and stay proposals.

**Delete** nothing in `Assets/_Scratch/`. It is gitignored and is emptied wholesale when the
look question closes, per `Working-Rules.md:246-249`. Nothing there ships.

Ordered, because order matters:

1. Port the profile asset and export the fixture meshes and materials. Nothing visible yet.
2. Apply the look pass to the Ship. Realtime light count must not move.
3. Place Light Probe Groups before baking, or every dynamic object in a baked room reads unlit.
4. Bake. Record the bake time; it is the iteration cost every later change pays.
5. Move the glow band in `CreatePalette`, run Build All, then immediately run criterion 7.
   **The band is the risky half of this milestone, not the districts** — it is the project's
   first HDR emission on a body, and eight of twelve rows now sit where ACES starts clipping.

Rejected: applying the look at runtime from the profile — an asset already defines the scene,
and reconstructing it on `Awake` is the habit that has cost this project most.

## Verification

1. Criteria 1–3 — grep the saved scene: `grep -c '^Light:'` expect 1;
   `grep -c "Instance"` on material names expect 0; count fixture materials expect ≤ 6.
2. Criterion 5 — select a mote emitter; the Inspector shows count 40, alpha 0.32, rise > 0.
3. Criterion 6 — `Assets/Packs/Gatekeepers/Hunter.mat` holds `_EmissionColor` peak ≈ 4.65
   after Build All, not the 1.0 it holds today. That is the proof the palette reached the
   material; the doc alone proves nothing.
4. Criterion 7, the gate — from Play: MainMenu → Ship. Stand at the stern end of the spine
   corridor and confirm the Hunter is identifiable as Aggressor before it is in range, **not
   merely visible**. A `[Dev]` log cannot close this one; it is an eye test.
5. Criterion 8 — record frame time, batches and set-pass calls at 1080p, before and after, into
   this file.

## Watch

- **Bloom clipping is the named risk.** Threshold is 0.70 and the tonemapper is ACES
  (`Assets/_Scratch/PostProcessingSetup.cs:95-109`). At peaks above 4 the family hues desaturate
  toward white as they clip, which weakens exactly the Aggressor / Support / Mover separation
  the band exists to carry. If criterion 7 fails, raising the threshold protects the enemies but
  costs the districts their halo — the trade goes back to Zo, not to the implementer.
- **Prefabs freeze serialized values** (`Architecture.md:161-170`). Gatekeeper emission lives on
  materials, not prefabs, so this pass is safe — but do not push the band through a prefab field.
- **The vendor shader is machine-enforced** (`Decisions.md:93-107`). `SG_SciFiRobotMat` keeps its
  BuiltIn target; a material edit is fine, a shader edit fails `vendor-lint`.
- **A bake goes stale on every layout change**, and this milestone's whole cost model is the
  bake. The two-deck rebuild this once warned about landed as `M101` and was then undone: the
  interior collapsed back to **one deck on 2026-08-26**, which invalidated the existing
  `LightingData.asset` and the reflection probe with it. The layout is settled now, so the
  sequencing note is no longer "wait for the rebuild" but "Apply Look Pass first, then Bake" —
  the probe group and the reflection probe are derived from `ShipDress` bounds, so a bake run
  before the Apply still describes the old volume.

## Depends on / Unblocks

- **Depends on** nothing in flight. The look is settled in `_Scratch` and walkable today.
- **Unblocks** the Bastion look pass, which inherits the profile, the fixture set and the band —
  and which owns the combat-legibility gate this milestone deliberately does not touch.
