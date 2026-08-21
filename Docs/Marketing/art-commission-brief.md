# What I'd need — Gatekeeper's Protocol

I'm a solo developer making a first-person sci-fi game in Unity. You board a held space
station, fight the machines garrisoning it, and haul salvage back to your ship. It's all
interiors — corridors, terminal rooms, a hatch bay, and the ship you come home to.

I want it to look like *Réflexions Faites*. Neon as the only thing showing you the way,
a tight palette, wet floors doing the reflecting. Indoors instead of a city street.

## The ask

I don't need scenes painted. I build the levels myself out of a kit of modular pieces, so
what I need is **the kit restyled** — then everything I assemble is on-style automatically.

Conveniently, that's a small number of files. The whole station and ship run off one shared
texture atlas, so repainting it restyles every wall, floor, prop and hull piece at once.

### The station and the ship

| File | Size | What it does |
|---|---|---|
| `PolygonSciFiSpace_Texture_01_A.png` | 2048² | The colour atlas. **Every** piece of the station and ship samples this one image. |
| `PolygonSciFiSpace_01_Emissive.png` | 2048² | The glow atlas — which parts of that same layout light up, and in what colour. |
| `PolygonSciFiSpace_Signs_Texture_01_A.png` | 1024² | Signage sheet, with its own emissive. |

I'll send you the originals. They're UV atlases — a patchwork of colour regions rather than
a picture — so the work is repainting regions in place, keeping the layout identical. The
pack ships five official recolours of the first one, so I can show you exactly what a
restyle looks like as a worked example.

The emissive atlas is the one I care most about. That's where the neon lives.

### The robots (four enemies)

`FaxLab3D/SciFiRobotCharactersPack` — Exterminator, Hunter, Tactical, UtilityBot. Each has
a 4096² BaseColor and an Emissive, and each ships a `_NeutralPaint` version that's the
unpainted base to work from, so you're not painting over someone else's paint job.

Eight files. The emissives matter more than the base colours — I need to tell these four
apart across a dark room at a glance, and glow is what does that.

### Signage and screens, if you're up for it

The station's consoles have screens, and the corridors need wayfinding. Flat glowing 2D is
exactly your thing, and it's the one category where what you draw ships as-is with nothing
in between. Room markers, arrows, hazard bands, terminal displays. Not essential, but the
best value per hour if you want the work.

### The rules, so I don't wreck it after

A page with the palette — the four or five colours the game is allowed to use, hex values —
and where each one goes. Ground, safe, hazard, objective. Plus how you'd light a room:
key colour and direction, and what's meant to glow. That's what keeps me consistent when
I'm building levels at midnight six months from now.

## Things that would break if you designed around them

Not preferences, just what the engine can't do:

- **Reflections are soft.** Floors reflect blurry colour and light, not sharp images. A
  reflected sign won't be readable. Reflected *glow* works and looks great.
- **The geometry has no surface detail.** It's flat-shaded low-poly — no scratches, no
  grime, no micro-texture on the models. Anything like that has to be in the texture.
- **It's all interiors** at human scale, first person, 60° field of view. Ceilings are 5 m.

## Format

PNG, same dimensions as the originals, layout unchanged so the UVs still line up. Emissive
kept as its own file rather than baked into the colour — that way I can pulse a terminal
when it goes live, or dim the signage during a fight. Layered source file if you work that
way and don't mind sharing it.

## Licence

Three uses, and I'd rather agree them up front than come back later: in the game itself,
on the Steam page and in trailers, and in press screenshots. Happy to drop any of those if
it changes the number.
