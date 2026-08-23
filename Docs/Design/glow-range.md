# Gatekeeper glow range

<!--
Purpose: One `_EmissionColor` per Gatekeeper kind, at an intensity that clears bloom and reads hotter than the station.
Status: reference
Update trigger: when a kind is added or retired, or when the bloom threshold moves.
Last updated: 2026-08-22
-->

Values go straight into `_EmissionColor` on the kind's material. The `_Emissive` map is a
greyscale mask on `SG_SciFiRobotMat` and is already bound, so the colour is the only thing
that changes per kind — no new textures.

**Why the values exceed 1.0.** Bloom threshold is 0.70 and station glow lands near 1.0. A
kind at LDR either fails to bloom at all — `Thief` sat at peak 0.66 and could not — or
blooms no harder than the wall behind it. Everything here peaks 2.2–2.8 so a Gatekeeper
reads **hotter than the room**, which is the separation that survives a dark corridor.

**Colour carries family, not kind.** At range the three families are unmistakable and
individual kinds inside a family are not — Default, Hunter and Exploder blur into one
orange, and the three movers are nearly identical. That is the limit of hue at distance,
not a tuning failure. Kind identity is carried by silhouette and by what each one refuses
to do (`Design/1-Experience.md` §1.4); colour tells the player which *class* of problem is
walking at them.

**Legibility rule.** `Design/2-Dynamics.md` §2.1, *The frame* — a Gatekeeper reads against
the surface behind it, at the range it is fought from. These values are the fix that rule
names: raise the body's emissive, never the room's ambient.


## Baseline

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Default | `r: 2.232, g: 1.014, b: 0.848` | 2.23 | yes — was (0.85, 0.15, 0.12) — peak 0.85, barely cleared bloom |

## Aggressor — it hurts you

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Hunter | `r: 2.585, g: 0.645, b: 0.129` | 2.58 | yes — was (1, 0.78, 0.1) — peak 1.0, bloomed no harder than the wall |
| Exploder | `r: 2.82, g: 1.269, b: 0.0` | 2.82 | — |
| Standoff Shooter | `r: 2.35, g: 0.282, b: 0.468` | 2.35 | — |
| Ailment Shooter | `r: 2.35, g: 0.47, b: 1.316` | 2.35 | — |

## Support — it makes others worse

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Source | `r: 0.247, g: 2.468, b: 0.269` | 2.47 | — |
| Enabler | `r: 0.939, g: 2.35, b: 0.517` | 2.35 | — |
| Alarmer | `r: 0.135, g: 2.702, b: 0.931` | 2.7 | — |

## Mover — it takes or evades

| Kind | `_EmissionColor` | Peak | In build |
|---|---|---|---|
| Thief | `r: 1.466, g: 0.47, b: 2.35` | 2.35 | yes — was (0.38, 0.28, 0.66) — peak 0.66, **could not bloom at all** |
| Reclaimer | `r: 1.873, g: 0.67, b: 2.232` | 2.23 | — |
| Bypasser | `r: 0.882, g: 0.282, b: 2.35` | 2.35 | — |
| Cloaker | `r: 0.842, g: 0.752, b: 1.88` | 1.88 | — |

## Applied 2026-08-22

The three in-build kinds keep the hues Zo chose and were scaled to HDR rather than
replaced — `Packs/Gatekeepers/Default.mat`, `Hunter.mat`, `Thief.mat`. The nine below them
are proposals for kinds that do not exist yet (`Brainstorms/gatekeepers-and-waves.md`,
`M63`).

## Watch

Red-green separation is the common colour-vision deficiency, and *aggressor* versus
*support* is exactly that pair. Both families are also separated by value and by behaviour,
but a player who cannot tell red from green is reading this roster on silhouette alone.
Worth a second channel if it ever matters — a shape tell, or a HUD mark.
