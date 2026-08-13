# 3 — Unity runtime idioms

<!--
Purpose: The measured, Unity-6-current rules for code that runs in the engine. Separates what is still true from advice that modern Unity has invalidated.
Status: working
Authority: CRITICAL — correctness against the engine, and measured cost.
Update trigger: when Unity's guidance changes, when a rule here is measured against this project, or when a "stale" entry turns out to still bite.
Last updated: 2026-08-11
-->

Everything here is `[documented]` or `[measured]` and is **not** outrankable by convention.
A style guide cannot make `Debug.Log` free or make `?.` respect Unity's lifetime check.

Two facts gate the rest. Unity's GC is **Boehm — non-generational and non-compacting**, so
there is no cheap gen-0 and small per-frame allocations are a real cost in a way they are not
in modern .NET ([GC best practices](https://docs.unity.cn/6000.1/Documentation/Manual/performance-garbage-collection-best-practices.html)).
And Unity 6 is **C# 9**, not .NET 6+, so string interpolation still lowers to
`string.Format(object[])` — an array allocation plus boxing of every value-type hole
([C# compiler](https://docs.unity3d.com/6000.5/Documentation/Manual/csharp-compiler.html)).
That second fact invalidates "interpolation is free now" advice imported from modern .NET,
and it is the reason §6 matters here.

## 1. Allocation

**Rule: nothing in a per-frame path allocates.** This applies to every `Update()`, `OnGUI`
(which runs at least twice per frame, Layout and Repaint), and the firing path.

Allocating constructs, all Unity-documented
([performance-reference-types](https://docs.unity3d.com/6000.0/Documentation/Manual/performance-reference-types.html)):
`params` methods (one array per call), closures capturing outer scope, string `+` chains
(five strings → five intermediates), **string interpolation with value-type holes**, boxing,
LINQ, and Unity's array-returning *properties* — `mesh.vertices`, `Input.touches`,
`Renderer.materials` return a fresh copy on every access.

`list.Clear()` does **not** release the backing array, which is the whole scratch-buffer
idiom: a field-level `List<T>`, cleared at the top of the frame. `UnityEngine.Pool`
(`ListPool<T>`, `ObjectPool<T>`) ships in Unity 6 and is the right answer to "I need a temp
list in a method called every frame".

**`foreach` — mostly stale, one live landmine.** "`foreach` over a `List<T>` allocates 40
bytes" was true through Unity 5.2 and fixed in 5.6; arrays, `List`, `Dictionary`, `HashSet`,
`Queue`, `Stack` all iterate at zero bytes ([Jackson Dunstan, measured](https://www.jacksondunstan.com/articles/3805)).
**Still true in Unity 6:** iterating through an *interface* boxes the struct enumerator, 40
bytes every loop. So a field typed `IReadOnlyList<T>`, or an `IEnumerable<T>` parameter, turns
a free loop into a per-frame allocation. Type hot-path fields as the concrete `List<T>` or
`T[]`. Same for `foreach (Transform child in transform)`, whose enumerator is non-generic.

**`struct` is the wrong call when** it ends up boxed anyway (a dictionary key without
`IEquatable<T>` boxes on every lookup), when it is large and passed by value, when it is
mutable and accessed through a property or indexer (`myList[0].hp -= 1` silently mutates a
copy), or when it holds reference-type fields.

**Live in this codebase:** `new WaitForSeconds(...)` allocates on every call.
`WaveSpawner.Groups.cs` used to construct one per group inside its spawn loop with a
per-wave-constant interval; since 2026-08-11 it allocates the wait once before the loop
and reuses it. `Damageable.cs` allocates one per hit flash outside any loop. Cache a
`static readonly WaitForSeconds` per distinct duration, or per wave. Also: `yield return
null` is free; `yield return 0` boxes.

**Incremental GC is on by default in Unity 6 and does not mean allocation is free.** It
time-slices marking into ~3 ms budgets, adds write-barrier cost on every reference write, and
under heavy reference churn can fail to finish marking and fall back to a full blocking
collection ([Incremental GC](https://docs.unity3d.com/6000.1/Documentation/Manual/performance-incremental-garbage-collection.html)).

## 2. Component and object lookup

**Cache `GetComponent` results in `Awake`.** Unity's documented rule
([programming-best-practices](https://docs.unity3d.com/6000.5/Documentation/Manual/programming-best-practices.html)).
This project already does it; the pattern to keep is `PlayerGun.cs:264-265`.

**Use `TryGetComponent<T>` when the component may be absent.** `GetComponent` returning null
allocates in the Editor for its diagnostic machinery; `TryGetComponent` does not
([TryGetComponent](https://docs.unity3d.com/ScriptReference/Component.TryGetComponent.html)).
Editor-only, but the Editor is where you profile. **Audited 2026-08-09: 39 `GetComponent<>`
call sites, 0 `TryGetComponent`.** Most are `Awake`-cached and fine; the probing ones
(`PlayerGun.Firing`'s `GetComponentInParent<EnemyGatekeeper>` on every damaged body) are the
ones worth converting.

**`GetComponentsInChildren(List<T> results)` over the array-returning overload** — the array
version allocates a fresh array per call.

**`FindObjectOfType` is deprecated in Unity 6.** Replacements: `FindAnyObjectByType<T>()`
(fastest, no ordering guarantee — the common case), `FindFirstObjectByType<T>()`
(deterministic, slower), `FindObjectsByType<T>(FindObjectsSortMode.None)` — "not sorting saves
time" ([docs](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.FindObjectOfType.html)).
**This project is already on the new API** (`ShipHud.cs:55`, `:64`). One note: it uses
`FindFirstObjectByType` where any instance would do; `FindAnyObjectByType` is the cheaper
form and there is only one `ShipController` in the scene.

**`Camera.main` is no longer catastrophic.** Unity 2020.2 added an internal cache; a
contrived 50,000-object test measured a 21,000×–51,000× improvement
([Unity blog](https://blog.unity.com/technology/new-performance-improvements-in-unity-2020-2)).
Current docs put it at "comparable to calling `GameObject.GetComponent`"
([Camera.main](https://docs.unity3d.com/ScriptReference/Camera-main.html)). Cache it in a hot
path; do not build a camera-service singleton to solve it. The remaining hazard is
**correctness, not speed** — it returns the first *enabled* MainCamera-tagged camera, and
null when that camera is disabled. `FirstPersonController.cs:95-96` guards correctly.

## 3. Null checks on `UnityEngine.Object` — the rule with no exceptions

A destroyed Unity object is `== null` but **not** `ReferenceEquals(x, null)`: the C# wrapper
outlives the C++ object and Unity's custom `operator ==` reports the destruction
([Unity: Custom == operator](https://unity.com/blog/engine-platform/custom-operator-should-we-keep-it)).
Every C# feature that uses reference equality instead of `operator ==` silently disagrees.

| Construct | Respects lifetime? |
|---|---|
| `x == null` / `x != null` | **yes** |
| `if (x)` implicit bool | **yes** |
| `x?.Foo()` | no — calls into a destroyed object |
| `x ?? fallback` | no — returns the destroyed object |
| `x ??= new()` | no |
| `x is null` / `x is not null` | no — the C# spec guarantees no user `==` is invoked |
| `ReferenceEquals(x, null)` | no — Unity's docs say explicitly not to use it for this |

**Rule: on any `UnityEngine.Object`, only `== null` / `!= null` / `if (x)`.** `is null` is the
one that slips through code review because the Rider inspection does not cover it.

**Install `Microsoft.Unity.Analyzers`.** USP0002 exists specifically to *suppress* Roslyn's
IDE0031 "simplify to `?.`" on Unity types, because the IDE's own refactoring introduces this
bug ([USP0002](https://github.com/microsoft/Microsoft.Unity.Analyzers/blob/main/doc/USP0002.md)).
Any editor with "apply all suggestions" on will do this unprompted.

**Audited 2026-08-09: no violations — then later UI work reintroduced the constructs, and
on 2026-08-11 every flagged construct line across the 22 files were converted back to the Unity-null-safe
forms.** The §9 row now enforces this at zero. The rule is written down because the next
`?.` might not be safe, and history shows it arrives with routine UI work.

**Do not "optimize" `== null` into `ReferenceEquals`.** You buy nanoseconds and sell the
destroyed-object check. The legitimate optimization is hoisting the check out of the loop.

## 4. Update loops and time

**Every `Update()`, `OnGUI`, and the firing path follows these rules.** See
`1-Boundaries.md` §5: no update manager. What applies at this scale:

- **Never declare an empty `Update`/`Start`/`OnGUI`.** Unity registers the callback only if
  the method exists, but an empty body is still invoked every frame. Audited: none present.
- **`Time.deltaTime` inside `FixedUpdate` already returns `Time.fixedDeltaTime`**
  ([docs](https://docs.unity3d.com/ScriptReference/Time-deltaTime.html)), so writing
  `fixedDeltaTime` there is redundant. The real errors are physics forces applied from
  `Update`, and `Time.time` for gameplay timers (breaks at `timeScale = 0`).
- **`Time.timeScale = 0` stops `FixedUpdate` entirely.** Pause logic living there never runs.
- **Sim never reads `Time`.** dt is injected through `RunSim.Tick(dt)`. This is already the
  rule and `4-Testing-and-Determinism.md` §3 says why it is load-bearing.
- **`Awaitable` over `Task`** if async is ever adopted: Unity 6 pools `Awaitable` instances
  ([docs](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Awaitable.html)). Sharp
  edge: pooling means awaiting a stored one twice reads a recycled instance. And unlike
  coroutines, async continuations do **not** stop when the host GameObject is disabled — plumb
  `destroyCancellationToken` or you will touch destroyed objects.

## 5. Physics and raycasting

**`Physics.RaycastNonAlloc` and friends are current in Unity 6 for 3D** and are the
documented recommendation ([Optimize raycasts and physics queries](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization-raycasts-queries.html)).
`PlayerGun.Firing` already uses `RaycastNonAlloc` with a reused buffer.
**Every `Physics2D.*NonAlloc` is `[Obsolete]` in Unity 6** — irrelevant here, but Rider and
Microsoft.Unity.Analyzers still recommend them, so do not trust the analyzer on 2D.

**The `NonAlloc` trap is correctness, not performance.** The array overloads **silently
truncate** at buffer size, and the buffer is **not cleared**, so entries past the returned
count are stale hits from a previous query. Iterating `buffer.Length` instead of the returned
count is the classic bug. `PlayerGun.Firing` sorts `_pierceHits` by distance over `hitCount`
and is correct on both points — but a crowded line at buffer capacity will silently drop the
far bodies, which for a pierce weapon is a gameplay difference, not a rounding error.

**Filter with layer masks, not with `if` after the hit.** Post-filtering makes PhysX do full
work and, with a fixed buffer, burns capacity on hits you discard — which then truncates the
ones you wanted. `FireRay` currently raycasts `~0` (everything) and filters afterwards by
walking to a `Damageable`. That is the pattern this rule is about.

**Two settings-level landmines:** `Physics.Raycast(origin, dir, out hit, mask)` compiles fine
because `mask` implicitly converts to `float maxDistance` — always pass `maxDistance`
explicitly. And *Queries Hit Triggers* is **on** by default, which is most "my raycast hit the
invisible volume" bugs; `FireRay` correctly passes `QueryTriggerInteraction.Ignore`.

**`Reuse Collision Callbacks` is enabled by default in Unity 6.** The physics system creates
one `Collision` instance and reuses it, so storing a `Collision` reference from
`OnCollisionEnter` for later gives you an aliased, mutated object by the next callback. Copy
the fields out ([PhysicsManager](https://docs.unity3d.com/6000.1/Documentation/Manual/class-PhysicsManager.html)).

**Batched `RaycastCommand` is not warranted here.** It wins in the hundreds-to-thousands
range; for a player's aim ray you pay `NativeArray` allocation, scheduling latency and a
`Complete()` sync point for nothing.

## 6. Logging — the one measured cost in this codebase

**`Debug.Log` is not stripped from release builds** and its arguments are evaluated
regardless. Stack-trace resolution is the dominant per-call cost and is on by default for
`Log`, `Warning` and `Error`. Unity's flat instruction: do not ship with stack trace enabled
([Stack trace logging](https://docs.unity3d.com/6000.1/Documentation/Manual/stack-trace.html)).

`Util/Log.cs` already gets the pattern right for one of its five methods:

```csharp
[System.Diagnostics.Conditional("GKB_VERBOSE")]
public static void Info(string tag, string msg) => UnityEngine.Debug.Log(Line(tag, msg));
```

`[Conditional]` is the only approach that removes **argument evaluation**, because the
compiler deletes the whole call expression at every call site.

**The pattern this is better than**, and the one InfinityQube mandates on every component:
a `DebugLog(method, message)` helper guarded internally by an `enableDebugLogs` bool. The
guard is inside the method, so it guards nothing the caller pays for — `DebugLog("Foo",
$"hp {hp}")` builds the string, allocates the `object[]` and boxes `hp` **at the call site**,
before the bool is read, and `GetType().Name` adds a reflection call. `6-InfinityQube-Precedence.md`
records why this project keeps its own.

**Audited 2026-08-09: `Log.Event` and `Log.Metric` are not `[Conditional]`, and 61 of 147
call sites interpolate.** Under C# 9 each of those builds an `object[]`, boxes every
value-type hole, formats the string, prepends the tag with `Line()`, resolves a stack trace,
and writes to the platform log — in a shipped build, whether or not anything reads it. 15 of
those sites are in the combat and spawn paths, which fire per kill, per group and per reload.
A further **47 interpolations sit inside `OnGUI` files**, which run at least twice per frame.

**Rule: `Log.Event` and `Log.Metric` become `[Conditional("GKB_LOG")]`, with `GKB_LOG` defined
in Editor and development builds and absent in release. `Warn` and `Error` stay unconditional
and keep their stack traces**, because crash reporting hooks the log callback and stripping
errors blinds it. Set `Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)`
regardless.

The tension to state plainly: **the session log is this project's primary verification
surface.** Every milestone gate reads it, and M29 builds a tool on it. So the goal is not
less logging — it is that logging costs nothing in a build the player runs, and everything in
the build the developer runs. `[Conditional]` gives exactly that. `Debug.Assert` already
works this way, gated on `UNITY_ASSERTIONS`.

## 7. Stale advice this project will not adopt

Each of these is circulating, plausible, and wrong for Unity 6. Written down so an agent
reading a 2018 blog post does not re-propose it.

| Claim | Status |
|---|---|
| "`Camera.main` does a tag search every call" | Fixed 2020.2; measured 21,000×–51,000× improvement. Cache it, do not architect around it |
| "`foreach` over `List<T>` allocates 40 bytes" | Fixed in Unity 5.6. Zero bytes. The live case is `foreach` over an **interface** |
| "Use `Physics2D.RaycastNonAlloc`" | Every 2D `NonAlloc` is `[Obsolete]` in Unity 6. Rider and the MS analyzers still say otherwise |
| "`FindObjectOfType` is how you find a singleton" | Deprecated in Unity 6 |
| "Incremental GC solved GC hitches, allocate freely" | It time-slices, adds write-barrier cost, and falls back to a full blocking collection under churn |
| "Structs are always faster, they don't allocate" | Wrong for large structs, mutable structs behind properties, and dictionary keys without `IEquatable<T>` |
| "Use `ReferenceEquals` instead of `== null` for speed" | Unity's docs say don't — it skips the destroyed-object check |
| "Wrap logs in `if (Debug.isDebugBuild)`" | Does not stop the string building or the boxing. `[Conditional]` does |
| "Build an update manager" | Unity: "not a one-size-fits-all"; the threshold is hundreds to thousands. This project has 8 |

## 8. Claims with no modern measurement — treat with suspicion

Per-call `GetComponent` cost in Unity 6 (all circulating figures trace to Unity 5-era
benchmarks on 2015 hardware), per-call `Debug.Log` milliseconds (sources are qualitative),
and update-manager gains on Unity 6 / IL2CPP specifically (the authoritative measurement is
Unity 5.2.2 on iOS). **Profile before acting on any of the three.**

## 9. The check — enforced or baselined, per row

`Tools/lint-arch/run.sh` (M41) reads the fenced block at the end of this section and
runs from `Tools/verify/run.sh`. Each row below is either **enforced** — zero
violations, any new one fails the run — or **baselined** — it prints `NOTE file:line`
and fails only when the count in the fence is exceeded. All counts measured 2026-08-11
with `rg` over `Assets/Scripts` using the patterns in the fence.

| Row | Decision | Measured 2026-08-11 |
|---|---|---|
| `?.` / `??` / `is null` / `ReferenceEquals` on what may be a `UnityEngine.Object` (§3), scanned as the raw constructs in `Game/`, comment lines excluded | **enforced at 0** (stamped 2026-08-11) | 0 — every flagged construct line across the 22 baselined files were converted to Unity-null-safe forms (`== null` / `!= null` / plain truthiness) on 2026-08-11 |
| `using System.Linq` in runtime code (§1) | **enforced at 0** | 0 — the one site M41's draft recorded has been fixed since |
| `new WaitForSeconds` in a loop body (§1) | **enforced at 0** (stamped 2026-08-11) | 0 — the row greps every site in `Game/` and subtracts the files the fence names as known non-loop sites (`Damageable.cs:150`, not in a loop; `WaveSpawner.Groups.cs`, whose per-group wait was hoisted out of its `while` loop into a single allocation before it on 2026-08-11). Any new site outside the two named files fails the run; a new in-loop wait inside a named file is masked, so touching those files means re-checking the loop by eye |
| `FindObjectOfType` / `FindObjectsOfType` (§2) | **enforced at 0** | 0 |
| Empty `Update` / `Start` / `LateUpdate` / `FixedUpdate` / `OnGUI` body (§4) | **enforced at 0** | 0 |
| A milestone ID (`M<digits>`) in an identifier — a file name, type, namespace, menu path or define | **enforced at 0** | 0 — true today (`M35Wp4OptionsPatch.cs`, the type, the file and its menu row, is already deleted). Zero exceptions |
| A milestone ID in a comment or string literal | **enforced at 0** (stamped 2026-08-11) | 0 — the 66 baselined lines across 39 files were cleaned on 2026-08-11; comments now say what the code does, not which milestone touched it |
| `[InitializeOnLoad]` outside the fence's allowlist | **enforced at 0** | 4 sites, all four in the allowlist |
| A scene lookup (`FindFirstObjectByType` / `FindObjectsByType` / `FindAnyObjectByType` / legacy `FindObject(s)OfType` / `GameObject.Find` / `Camera.main`) in `Game/` outside the fence's allowlist | **enforced at 0** (stamped 2026-08-12) | 0 outside the named files. Replaces the two-runs milestone gate's manual grep — the wiring rule is serialize/authored refs for scene-stable objects, pass-from-owner for runtime-spawned. The allowlist is the named-exemption list: cross-scene flow/audio dedupe (`SceneFlow`, `FrontEndMusic`), the settings-change volume broadcast (`AudioBuses`), the bind-time Shift-manager locator (`UiBind`), the two dev files, and the two spawned-network lookups (`NetRunMirror`, `NetPlayerAvatar`) — spawned prefabs cannot be scene-wired |
| An `Input.` read in `Game/` outside `Controls.cs` (the sole producer) or the dev console | **enforced at 0** (stamped 2026-08-12) | 0 — `Controls.Sample()` produces one `PlayerInputFrame` per frame; gameplay reads the frame. Replaces the same milestone gate's input grep |
| An ambient `UnityEngine.Random` draw in `Game/` outside the named sites | **enforced at 0** (stamped 2026-08-12) | 0 outside `PlayerGun.Firing.cs` (crit + pellet cone — deliberately off the replayable stream, see `4-Testing-and-Determinism.md`) and `UiPitchRandomizer.cs` (cosmetic). Spawn placement draws come from the seeded generator on `RunSim`. Replaces the same milestone gate's randomness grep |
| `AssetDatabase.DeleteAsset` naming a `.cs` file (self-deleting script) | **enforced at 0** | 0 |
| A non-source, non-`.meta` file under `Assets/Scripts/` (`.cs`, `.asmdef` and `.meta` are source here) | **enforced at 0** | 0 |
| A `static Instance` on a type not named in `Architecture/README.md`'s fence | see that file's fence | 2 declarations, both named there (`DecoyBay`'s was retired 2026-08-11) |
| A file over its folder's cap | see `Docs/Architecture.md`'s fence | 0 since 2026-08-11 |

The scan is textual — a construct inside a string or comment counts, which is
deliberate for the milestone-ID text row and a cheap over-approximation for the rest.
A row narrowed until its count reaches zero is a failed row, not a passing one.

```arch-lint
fakenull_pattern=^(?![ \t]*//).*?(\?\.|\?\?|\bis[ \t]+null\b|\bReferenceEquals\b)
fakenull_file_baseline=0
linq_pattern=^[ \t]*using[ \t]+System\.Linq
linq_enforced=0
waitforseconds_pattern=\bnew[ \t]+WaitForSeconds\b
waitforseconds_baseline=0
waitforseconds_nonloop_allow=Game/Combat/Damageable.cs,Game/Enemies/WaveSpawner.Groups.cs
findobjectoftype_pattern=\bFindObjects?OfType\b
findobjectoftype_enforced=0
emptybody_pattern=\bvoid[ \t]+(Update|Start|LateUpdate|FixedUpdate|OnGUI)[ \t]*\([ \t]*\)\s*\{\s*\}
emptybody_enforced=0
midid_pattern=(\b(class|struct|interface|enum|namespace)[ \t]+[A-Za-z0-9_.]*M[0-9]{2,3}|MenuItem\("[^"]*M[0-9]{2,3}|^[ \t]*#(define|if|elif)[^/\r\n]*M[0-9]{2,3})
midid_name_pattern=M[0-9]{2,3}
midid_enforced=0
midtext_pattern=\bM[0-9]{2,3}\b
midtext_line_baseline=0
midtext_file_baseline=0
initializeonload_pattern=\[InitializeOnLoad\]
initializeonload_allow=Editor/Scenes/SceneBuilder.cs,Editor/Tools/IntegrationCheck.cs,Editor/Tools/RepairStationPanels.cs,Editor/Tools/StripSettingsToStations.cs,Editor/Bastion/BastionKitEmitter.cs,Editor/Bastion/BastionSketchEmitter.cs
scenelookup_pattern=\bFind(FirstObjectByType|AnyObjectByType|ObjectsByType|Objects?OfType)\b|\bGameObject\.Find\b|\bCamera\.main\b
scenelookup_allow=Game/Flow/SceneFlow.cs,Game/Flow/FrontEndMusic.cs,Game/Ui/AudioBuses.cs,Game/Ui/UiBind.cs,Game/Dev/DevConsole.cs,Game/Bastion/GameController.DevCommands.cs,Game/Net/NetRunMirror.cs,Game/Net/NetPlayerAvatar.cs
ambientinput_pattern=(^|[^A-Za-z0-9_.])Input\.
ambientinput_allow=Game/Input/Controls.cs,Game/Dev/DevConsole.cs
ambientrandom_pattern=\bUnityEngine\.Random\b|(^|[^A-Za-z0-9_.])Random\.(Range|value|insideUnitSphere|onUnitSphere|rotation)\b
ambientrandom_allow=Game/Player/PlayerGun.Firing.cs,Game/Ui/UiPitchRandomizer.cs
selfdelete_pattern=AssetDatabase\.DeleteAsset[^\r\n]*\.cs
selfdelete_enforced=0
nonsource_enforced=0
```
