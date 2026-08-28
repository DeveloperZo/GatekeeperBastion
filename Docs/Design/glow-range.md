# Gatekeeper glow range

<!--
Purpose: One `_EmissionColor` per Gatekeeper kind, at an intensity that clears bloom and reads hotter than the station.
Status: reference
Update trigger: when a kind is added or retired, or when the bloom threshold moves.
Last updated: 2026-08-26 (band raised for M104's district fixtures; the tree still holds LDR)
-->

Values belong in `SceneBuilder.CreatePalette()`
(`Assets/Scripts/Editor/Scenes/SceneBuilder.cs`), which is where every kind's tint is
authored. `EnsureWrapperEmission` writes it into `_Emission_Color`, `_EmissionColor` and
`_EmissiveColor` on that kind's material on the next Build All, so a value typed straight
into the `.mat` is lost. The `_Emissive` map is a greyscale mask on `SG_SciFiRobotMat` and
is already bound, so the colour is the only thing that changes per kind — no new textures.

**Why the values exceed 1.0.** Bloom threshold is 0.70 and station glow lands near 1.0. A
kind at LDR either fails to bloom at all — `Thief` sits at peak 0.66 and cannot — or
blooms no harder than the wall behind it. The aggressor and support proposals peak 4.0–4.9
so a Gatekeeper reads **hotter than the room**, which is the separation that survives a dark
corridor. The Cloaker is the one row deliberately under that band: it is meant to be missed.

**Why the band sits this high — 2026-08-26.** `M104` dresses the Ship with district fixtures
at peak 3.0, above the old 1.4 architecture ceiling, because the look depends on it. At the
previous 2.2–2.8 every kind would have been dimmer than the wall behind it, inverting the
legibility rule below. Zo chose to raise the band rather than cap the districts. The triples
were **scaled ×1.8**, not replaced, so every family keeps its hue ratios and the Cloaker keeps
its deliberate position under the band.

**Colour carries family, not kind.** At range the three families are unmistakable and
individual kinds inside a family are not — Default, Hunter and Exploder blur into one
orange, and the three movers are nearly identical. That is the limit of hue at distance,
not a tuning failure. Kind identity is carried by silhouette and by what each one refuses
to do (`1-Experience.md` §1.4); colour tells the player which *class* of problem is
walking at them.

**Legibility rule.** `2-Dynamics.md` §2.1, *The frame* — a Gatekeeper reads against
the surface behind it, at the range it is fought from. These values are the fix that rule
names: raise the body's emissive, never the room's ambient.


## Baseline

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Default | `r: 4.018, g: 1.825, b: 1.526` | 4.02 | yes — still wearing (0.85, 0.15, 0.12), peak 0.85, barely clears bloom |

## Aggressor — it hurts you

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Hunter | `r: 4.653, g: 1.161, b: 0.232` | 4.65 | yes — still wearing (1, 0.78, 0.1), peak 1.0, blooms no harder than the wall |
| Exploder | `r: 5.076, g: 2.284, b: 0.000` | 5.08 | — |
| Standoff Shooter | `r: 4.230, g: 0.508, b: 0.842` | 4.23 | — |
| Ailment Shooter | `r: 4.230, g: 0.846, b: 2.369` | 4.23 | — |

## Support — it makes others worse

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Source | `r: 0.445, g: 4.442, b: 0.484` | 4.44 | — |
| Enabler | `r: 1.690, g: 4.230, b: 0.931` | 4.23 | — |
| Alarmer | `r: 0.243, g: 4.864, b: 1.676` | 4.86 | — |

## Mover — it takes or evades

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Thief | `r: 2.639, g: 0.846, b: 4.230` | 4.23 | yes — still wearing (0.38, 0.28, 0.66), peak **0.66**, cannot bloom at all |
| Reclaimer | `r: 3.371, g: 1.206, b: 4.018` | 4.02 | — |
| Bypasser | `r: 1.588, g: 0.508, b: 4.230` | 4.23 | — |
| Cloaker | `r: 1.516, g: 1.354, b: 3.384` | 3.38 | in build since `M63-3`, but wearing cold teal `(0.20, 0.85, 0.78)`, peak 0.85 — the violet on this row was never taken |

## Not applied — read 2026-08-25

**None of the values above are in the tree.** `SceneBuilder.CreatePalette()` still authors
Default `(0.85, 0.15, 0.12)`, Hunter `(1, 0.78, 0.10)`, Thief `(0.38, 0.28, 0.66)` and
Cloaker `(0.20, 0.85, 0.78)`, and `Assets/Packs/Gatekeepers/Default.mat`, `Hunter.mat`,
`Thief.mat` and `Cloaker.mat` each hold exactly those numbers — the "was" column of the
tables above, unchanged. Every peak is at or under 1.0, so on the stated 0.70 bloom
threshold the Thief still cannot bloom and the Hunter still blooms no harder than the wall.
The section this replaces claimed the three in-build kinds had been scaled to HDR on
2026-08-22; whatever happened in the editor that day did not reach the palette or the
materials. Applying them is a one-line-per-kind change in `CreatePalette` plus a Build All.

**`M104` is what finally applies them**, and it applies the raised numbers rather than the
2.2–2.8 ones, because the districts it puts on the Ship's walls sit at 3.0. That milestone
carries the gate: a Gatekeeper identifiable by family at the far end of the dressed Ship's
longest sightline. The jump is LDR → 4.0-odd in one step, so expect the first look to be too
hot rather than too cold, and tune down from there.

The four in-build kinds are Default, Hunter, Thief and Cloaker. The eight below them are
proposals for kinds that do not exist yet (`../Brainstorms/gatekeepers-and-waves.md`,
`M63`).

## Watch

Red-green separation is the common colour-vision deficiency, and *aggressor* versus
*support* is exactly that pair. Both families are also separated by value and by behaviour,
but a player who cannot tell red from green is reading this roster on silhouette alone.
Worth a second channel if it ever matters — a shape tell, or a HUD mark.

**Bloom clipping, new on 2026-08-26.** Threshold is 0.70 and the tonemapper is ACES. Above
peak 4 the family hues desaturate toward white as they clip, and eight of the twelve rows now
sit above 4 — which erodes the same aggressor/support/mover separation this band exists to
carry. Check it on the Ship before the Bastion. Raising the bloom threshold protects the kinds
but costs `M104`'s district fixtures their halo; that trade is Zo's, not an implementer's.
