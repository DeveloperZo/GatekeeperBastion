# Animation sidecar

<!--
Purpose: Authoring contract for the `.md` files paired with animation FBXs under
Assets/Packs/. Says what a sidecar records, what it must not record, and what to do
for clips that predate the convention.
Status: working
Update trigger: when the recorded field set changes, or when a clip source other than
Mixamo enters the project.
Last updated: 2026-08-18
-->

Every animation `.fbx` under `Assets/Packs/` carries a `.md` of the same stem beside
it. Started by Zo 2026-08-18 in `Packs/Gatekeepers/Animations/`.

This is the second recurring file class in the project after the milestone, and it
gets a definition for the same reason: there will be dozens and they must agree.

## Why it exists

**Mixamo bakes its sliders and trim into the downloaded FBX.** Attitude, Speed,
Stride, Overdrive, arm-space and the trim handles are applied server-side before the
file is produced. The FBX cannot say what produced it, Unity cannot show it, and the
settings cannot be re-derived from the file. Without a sidecar, "download it again
slightly faster" means guessing from scratch.

That test is the whole boundary: **record what is lost at export, nothing else.**

## What it must not record

**Unity import settings.** Rig type, avatar definition and source, clip name,
`firstFrame`/`lastFrame`, loop time and root-transform bakes all live in the
`.fbx.meta` and are the live truth. Copying them into a sidecar creates two sources
that drift, and the sidecar is the one nobody updates.

If you want to know how a clip is imported, read the `.meta`. If you want to know how
it was generated, read the sidecar.

## Fields

```
Source: <library> — <animation name exactly as listed there>
Slot: <row in Docs/Animation-Inventory.md, or "candidate, unassigned">
In Place: <on | off | not offered>
<Slider> - <value>          (only the sliders that clip offered)
Trim: <start> - <end>
Note: <optional, one line — anything a future reader would otherwise rediscover>
```

Order is fixed so the files diff and grep cleanly. Omit no field; write
`unrecorded` rather than dropping a line, because a missing line reads as an
oversight and `unrecorded` reads as a fact.

**`Slot`** is the field that earns its place beyond provenance. When more than one
clip is a candidate for one tracker row, it is the only thing separating a deliberate
spare from an unexplained duplicate. Three death clips against one Death row is the
case that forced it (ruled 2026-08-18, Zo: one wired, the rest imported for later
variation).

## Clips that predate the convention

The six in `Packs/Boarder/` were downloaded before this existed and their slider
positions are gone. They still get a sidecar, and its job is to say so: `Source` and
`Slot` are recoverable, the sliders are `unrecorded (pre-convention)`. A sidecar that
records a loss stops the next reader hunting for a file that was never written.

**Do not reconstruct settings by inference.** A plausible slider value written down
becomes a fact within a week. `unrecorded` is always the correct entry for something
nobody wrote down.
