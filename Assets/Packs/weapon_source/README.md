# weapon_source

FaxLab3D Shooter Weapons Pack (fbx source + extracted textures). Zo's mesh
mapping, recovered by byte-size match against the store zip 2026-08-19:

| Game weapon | Pack source | Material |
|---|---|---|
| MK-1  | Pistol_Energy | mat/M_EnergyPistol |
| BR-12 | Shotgun       | mat/M_Shotgun |
| PRI-C | Sniper        | mat/M_Sniper |
| MR-35 | Rifle_Semi    | mat/M_RifleSemi |
| SEC-D | SMG           | mat/M_SMG |

Cutter keeps the Synty sword (pack has no melee). Unused meshes (SMG twin,
Minigun, Launcher, grenades, scopes) stay here as future roster material.

Wired into the game 2026-08-19: each `Packs/Weapons` wrapper prefab nests its
fbx as a prefab instance (Boarder name/active overrides key off the wrapper
roots, which kept their fileIDs). Every fbx imports at **globalScale 100** —
the Blender export lands ~1/100 real size. The models are authored X = length
(muzzle -X), Z = up; the wrapper child carries the corrective rotation
`AngleAxis(90, forward) * AngleAxis(90, up)` so the barrel is +Z, top +Y.

Tracking (see `.gitignore`): the `.meta` files, `mat/`, `tex/*.meta` and this
README are in git — they carry the GUIDs and hand edits the wrappers depend
on. The binary fbx/png payload is not; restore it from
`Downloads/shooterweaponspackassets.zip` (rename per the table above, textures
from `tex/`).

Materials are Built-in Standard, remapped through each fbx meta's
externalObjects, so every instance picks them up automatically. Textures:
BaseColor + NormalGL + Met + AO per set, capped 2048. **Smoothness is a
uniform 0.35 via _GlossMapScale** — the pack's per-pixel roughness needs a
channel repack (rough -> inverted alpha) that is deferred as tuning; raise or
lower _GlossMapScale per material to taste. Full texture sets (Rough,
RoughMetAO, Height, Curv, Emissive) remain in Downloads/shooterweaponspackassets.zip.
