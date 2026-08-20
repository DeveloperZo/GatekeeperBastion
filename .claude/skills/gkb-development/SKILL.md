---
name: gkb-development
description: Unity 6 and C# implementation practice for the Gatekeeper's Protocol codebase — the Sim/ boundary, fake-null lifetime checks, per-frame allocation, pooling, serialized-vs-code defaults, one-shot editor work, and repair discipline when fixing something that already works. Use this skill whenever a milestone is being built, implemented, executed or completed — including working through its acceptance criteria — because building a milestone is development work almost every time, and whenever writing or changing C# in this repository, or repairing a defect. Load it before reading the milestone's criteria, not after opening the first file, because it governs the approach and not just the typing. Use it even for a one-line change, because the rules most often broken here are scoped ones that look like they do not apply. For proving the work is done, or adding a test, script or menu row, use gkb-verification instead.
metadata:
  project: gatekeepers-protocol
  update-trigger: when a rule in Docs/Architecture/ changes, or when a repair goes wrong in a new way
---

# Development

## Never

- **Reconstruct at runtime something an asset already defines.** The fix for a scene that looks
  wrong is an edit to that scene, not a script that rebuilds it on `Awake`. This has cost this
  project more than any other habit. *Runtime construction of gameplay objects — pooled enemies,
  wave contents, list rows from data — is a different thing and is correct.*
- **Name anything after a work item.** No type, file, namespace, menu path or define contains a
  milestone or work-package ID. A type is named for the responsibility it holds.
- **Leave a one-shot script in the tracked tree**, and never use `[InitializeOnLoad]` or have a
  script delete itself.
- **Add a `[MenuItem]` without asking first.** Menu entries are a shared, permanent surface and
  they are added far more easily than they are removed. Ask, and name the row you propose to
  replace. **There is one menu root, `GatekeepersProtocol/`, with five folders: Record, Logs,
  Build, Bake, Validate.** A second root is never the answer. A new row updates `menu_rows`
  in `Docs/Architecture/README.md` in the same change.
- **Put `MonoBehaviour`, `GameObject`, a scene lookup, `Time` or `UnityEngine.Random` in
  `Sim/`.** *A seeded generator the sim owns and is handed is required, not forbidden.*
- **Use `?.`, `??`, `??=` or `is null` on a `UnityEngine.Object`.** *Scoped: see below. In `Sim/`
  they are correct.*
- **Allocate in a per-frame path** — no boxing, no capturing closure, no LINQ, no interpolated
  string, no array-valued property such as `mesh.vertices` inside a loop. *Editor tooling, load,
  and once-per-Wave code are exempt.*
- **Add a static MonoBehaviour singleton.** The grandfathered ones are named in
  `Docs/Architecture/README.md`; anything not on that list is new. *Never `Player` — there is
  about to be more than one.*

## Always

- **Cache `GetComponent` in `Awake`**, and re-resolve in `OnEnable` for anything pooled.
- **Inject dt.** `Sim/` never reads `Time`.
- **Say why a command was refused**, in the log at minimum.
- **Mark unfinished work `// POC:`** with what is unfinished and why.
- **Run `Tools/verify/run.sh` before claiming a sim or tuning change works.**

---

## The `Sim/` boundary, and what it is for

`Sim/` holds game logic as plain C#. **The load-bearing property is one-directional
dependency**: `Sim/` never reaches into the scene. It is not type purity.

`Vector3`, `Quaternion` and `Mathf` are fine there. Banning them buys cross-platform bitwise
determinism, which only deterministic lockstep or rollback needs, and this project does neither.
The cost of banning them is real: no `Physics.Raycast`, no NavMesh, and a hand-written mapping
layer for every replicated field.

What the boundary buys: EditMode tests that need no scene and no domain reload, and logic that
cannot silently depend on frame order.



## Fake null is a presentation-layer rule

A destroyed `UnityEngine.Object` leaves a live C# wrapper. Unity's overloaded `==` reports that
state; `?.` and `??` use the runtime null check and bypass it, so `Instance?.Method()` can call
into a destroyed object.

**This applies only where `UnityEngine.Object` values exist.** In `Sim/` there are none, so `?.`
is correct there. Treating it as a global ban is why a large backlog of flagged sites exists
that were never wrong.

**`ReferenceEquals` is the exception to its own rule**: it is the correct idiom when you
deliberately want the C# reference, such as telling "never assigned" apart from "destroyed".
Use it on purpose and say so in a comment.

## Serialized data outranks code defaults

A field initializer on a MonoBehaviour is overwritten by whatever the prefab serialized.
Changing `= 10f` to `= 20f` in code changes nothing on any existing prefab, and this has already
eaten a full tuning pass here.

Put code defaults in `Reset()` so the two cannot diverge silently. Values the sim's correctness
depends on belong in code or in one asset, so they cannot be overridden per instance.

## Data-drive on the second call site, not the first

The usual argument for pulling values into assets is designers iterating without programmers.
**There is no second person here**, so the bar is higher than the literature implies. The real
reason to data-drive is a faster tuning loop and smaller diffs.

Data: wave composition, enemy stat blocks, weapon tunables. Code: tick rate, damage formulas,
state transitions — changing those is a design change that should break a test.

## Assembly definitions

Adopt them for the compile error, not for speed. **The circulated speed multipliers are
unsourced** and Unity's own documentation claims no number; expect a near-full rebuild when a
widely-referenced assembly changes.

## Repair discipline

The expensive failure is not the first wrong fix. It is the turns after it.

- **Repair what exists.** Never write a runtime factory to work around a broken asset.
- **Name what was working before, and check it still works.** A fix that quietly removes a
  shipped behaviour is a net loss even when the original defect closes.
- **One cause, one change.** If the evidence proves one cause, ship one change. Four changes for
  one cause means three are unexplained, and each can hide a new defect.
- **A workaround that bypasses the system is a defect with a button on it.** Drawing text in
  `OnGUI` because the Canvas is blank does not fix the blank Canvas.
- **Change only what was asked.** Behaviour nobody reported broken is out of scope, and swapping
  a design decision inside a bug fix hides the swap.

## One-shot editor work

Cheapest rung first:

1. **Edit the YAML.** Serialization is Force Text, so scenes, prefabs and assets are plain text.
   Setting a field, swapping a reference or renaming an object is a text edit — the diff is the
   review and `git checkout` is the undo. Stops being safe when new GUIDs or fileIDs are needed.
2. **A gitignored scratch folder**, for anything needing the AssetDatabase. Unity compiles it, a
   committed `[MenuItem]` runs it, the folder is emptied after. *M41 creates this folder and the
   check that fails the verify run while it is dirty. Until then, the emptying is yours to do.*
3. **A committed tool**, only once the operation has served more than one session and more than
   one question.

**Delete-after-verify is not a mechanism.** Cleanup inside a script only runs when the script
succeeds, and the script that fails halfway is the one that stays. The log write dies the same
way: wrap the body in try/catch so every exit, including the throw, still writes the log — a
tool that died before its log write burned several blind runs here.

**A clone of scene objects keeps their prefab links**, and `DestroyImmediate` on a child inside
a prefab instance throws. For plain objects: staging copy → unpack all instances →
`SaveAsPrefabAsset` to a temp path → `InstantiatePrefab` at the target → unpack completely →
delete staging and temp asset in `finally`.

**Before writing a run sentinel, confirm the recompile happened.** The handshake — dll mtime
against source, and what a consumed sentinel with no log means — is stated once, in
gkb-verification.

## Do not use the editor to find something out

Every scene, prefab and asset is YAML that `grep` reads in milliseconds; a domain reload plus a
scene open is tens of seconds. **Writing code needs no editor at all** — `Sim/` and the EditMode
tests compile and run under `Tools/verify/run.sh` in seconds, and `Game/` compile errors are
written to `Library/Bee/CachedNodeOutput` (generated, not committed) rather than being something
to ask the editor about.

## Rejected by name, so they are not re-proposed

Update manager or `IUpdatable` registry, DI containers, ScriptableObject event channels,
`FindObjectOfType` in `Start`, a two-stream logger, and counters or rollup or stats objects in
`Sim/`.

`partial` needs a stated reason per class, tested by one question: would the split files pass as
separate classes if the language allowed it? If yes, extract a real type. Using `partial` to get
a class under its line cap hides size without removing it.
