# 1 — Boundaries

<!--
Purpose: Where the lines in this codebase are, what enforces them, and which popular Unity architecture patterns this project rejects and why.
Status: working
Authority: CRITICAL — the Sim seam and the mutation surface are load-bearing.
Update trigger: when an assembly is added or split, when the wiring approach changes, or when a rejected pattern is reconsidered.
Last updated: 2026-08-09
-->

## 1. The Sim seam is a convention, not a boundary — and making it a boundary is free

`Docs/Architecture.md` opens with the project's one structural rule: `Assets/Scripts/Sim/`
is pure C#, no `using UnityEngine`, and it is the multiplayer seam.

**Nothing in Unity enforces it.** `Assets/Scripts/GatekeeperBastion.asmdef` covers `Sim/`,
`Game/` and `Util/` together, with `"noEngineReferences": false`. A `using UnityEngine` added
to `RunSim` compiles cleanly and Unity says nothing.

What does catch it today is `Tools/verify/run.sh`, which compiles `Sim/` plus `Util/Log.cs`
with `mcs` and no Unity assemblies on the reference path — so a Unity type in Sim breaks the
harness. **That is real enforcement, and it is fragile**: it only fires when someone runs the
harness, and it disappears the moment anyone adds a Unity DLL to that script's references to
make one thing compile.

**The fix costs nothing here, which is unusual.** The standard objection to
`noEngineReferences: true` is that you lose `Vector2/3`, `Quaternion`, `Mathf`, `Color`,
`Random`, `[SerializeField]` and `Debug` — most projects flip the flag back off within a day.
Audited 2026-08-09: `Sim/` uses **none of them**. Zero matches for `UnityEngine`, `Vector2`,
`Vector3`, `Quaternion`, `Mathf`, `Color`, `Random.`, `SerializeField`, `Debug.` across the
whole folder. The only outside dependency is `Util/Log.cs`, which is a five-method static
wrapper that would move with it or behind an interface.

**Rule: `Sim/` gets its own assembly definition with `noEngineReferences: true`.**
**Reason:** Unity prohibits cyclic assembly references outright and surfaces violations as
`CS0246` at compile time ([Referencing assemblies](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-referencing.html)),
which turns the project's most load-bearing rule from a code-review item into a build error.
Secondary benefit, measured: the same test suite runs ~18 s under the Unity Test Runner
against ~2 s under a plain .NET runner, of which 0.38 s is compile and 3.45 s is domain
reload before a single test executes ([gamedev.center](https://gamedev.center/run-unity-tests-faster-dotnet/)).
**Counter-case:** asmdefs are not free. Each assembly carries fixed compile overhead — one
measured report puts it near 2 s per asmdef, and an assembly that everything references
produces a near-full rebuild on every edit ([Unity Discussions](https://discussions.unity.com/t/unity-assembly-definition-files-slower-not-faster-how-to-compile-faster/687349)).
`Sim/` is edited constantly and `Game/` depends on all of it, so **this split will not make
compiles faster and may make them slower.** Adopt it for the enforcement, not the speed, and
say so — the widely circulated "10–100× faster iteration" figures for asmdefs are unsourced.
**Status:** `[judgement]` on timing, `[documented]` on mechanism. Needs a milestone; the
gate is that `using UnityEngine;` added to any file in `Sim/` fails the Unity compile.

**Do not add more assemblies than that.** Unity's own package guidance is that one assembly
per code folder — Editor, Runtime, Tests — "typically suffices"
([Create or edit assembly definitions](https://docs.unity3d.com/6000.4/Documentation/Manual/cus-asmdef.html)).
Core/Gameplay/UI splits are community practice, not Unity guidance, and the usual outcome is
a "Shared" assembly that everything references, which is the god-module you were avoiding.
Three assemblies — `Sim`, `GatekeeperBastion`, `GatekeeperBastion.Editor` — is the target.

## 2. Wiring: a composition root, not a container

**Rule: MonoBehaviours are constructed by Unity, so dependencies enter through
`[SerializeField]` wiring done by `SceneBuilder`, or through an explicit `Configure(...)` /
`Initialize(...)` call from the owning controller. No DI container. No service locator.**

**Reason.** The project already does this and it is the right call at this size. The
alternatives both have documented costs that a solo project pays without getting the
benefit: DI containers move wiring into reflection that is invisible in the call graph, and
the objections in [Unity's own DI thread](https://discussions.unity.com/t/dependency-injection-in-unity-inversion-of-control-container/914827)
— least-astonishment violation, debugging opacity, load-time cost — all land harder when
there is one developer and no wiring graph too large for one file. Service location is
genuinely contested: Nystrom defends it *sparingly* for fundamentally singular systems
([Service Locator](https://gameprogrammingpatterns.com/service-locator.html)), Seemann calls
it an anti-pattern because the constructor lies about what the class needs
([ploeh](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/)), Bogard defends
it for lifetime mismatches ([jimmybogard](https://www.jimmybogard.com/service-locator-is-not-an-anti-pattern/)).
The debate does not resolve, so the project should not depend on which side is right.

**Counter-case.** `SceneBuilder`-authored `[SerializeField]` wiring cannot express interfaces
directly, cannot cross scenes, and cannot reach runtime-instantiated objects — which is why
`GameController.Instance`, `HatchStation.Instance` and `PlayerRegistry` exist. Those are the
seams a container would clean up. If the wiring graph ever stops fitting in `SceneBuilder`,
revisit this rather than adding a fourth static.

**Rule: the existing "all sim mutation goes through `GameController`" convention stays, and
its scope gets stated.** It is the Command pattern's single-writer property, and its real
payoff is not networking-in-advance: it is that **"what mutated this?" has one answer you
can breakpoint** ([Command](https://gameprogrammingpatterns.com/command.html)).
**The scope correction:** in a MonoBehaviour game the mutation surface is not yours.
`Transform.position`, `NavMeshAgent`, the Animator and every prefab mutate state outside the
command layer. State the claim as **"`GameController` is the only mutation surface for Run
and Meta state"** — claiming more than that produces exactly the desync surprise the pattern
was adopted to prevent.

## 3. Composition, and when a base class is right

**Rule: components over hierarchy, cut along domain boundaries.** The failure this avoids is
the blob, and its smell is a condition that entangles two domains at once — Nystrom's example
is `if (collidingWithFloor() && getRenderState() != INVISIBLE)`
([Component](https://gameprogrammingpatterns.com/component.html)).

**A base class is right when the shared thing is policy a subtype cannot opt out of**, not
merely shared code. `Damageable` enforcing HP clamping and death ordering qualifies.
`EnemyGatekeeper` → `HunterGatekeeper` / `RamGatekeeper` qualifies as long as the base owns
the targeting contract rather than accumulating each kind's behaviour.
**Falsification test:** if a subclass ever overrides a method to do nothing, or if two
siblings share nothing but the base's existence, the hierarchy is wrong.

**Counter-case Nystrom names and this project already lives with:** components create
implicit update-ordering dependencies, which is why Unity ships a Script Execution Order
dialog. Ordering that matters belongs in an explicit tick from the owning controller, not in
the dialog, because the dialog is invisible in code and does not diff.

## 4. Partial classes — the project's convention disagrees with Microsoft's

`Architecture.md` currently says: *"File size: ≤600 lines in Sim, ≤400 in Game. Split with
`partial` … rather than growing past it."* Five classes are split across 13 files:
`GameController` (×3), `HudController` (×3), `SceneBuilder` (×3), `PlayerGun` (×2),
`EnemyGatekeeper` (×2), `WaveSpawner` (×2).

Microsoft documents exactly three motivating cases for `partial`: multiple programmers
editing one class, separating tool-generated code from hand-written code, and source
generators ([Partial Classes and Members](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods)).
"Splitting a large class for readability" is not among them. The argument against is that
`partial` hides size without removing it: a 900-line class in three files still has one
mutable state surface reachable from every method. You have removed the signal and kept the
problem.

**Rule: `partial` needs a stated reason per class, and the reason is tested by one question
— would the split files pass as separate classes if the language allowed it?**
If yes, extract a real type with a real boundary. If no — the parts genuinely share private
state and Unity requires them to be one component — `partial` is legitimate and the class
comment says which.

Applying it to today's five: `SceneBuilder` is editor tooling with no shared mutable state
worth protecting and is the weakest case for `partial` and the easiest extraction.
`GameController` and `HudController` are the strongest cases — Unity requires each to be one
component and each part reads the same serialized fields. `PlayerGun.Firing` sits in between
and is worth the question. **`[judgement]`.** InfinityQube's AI Agent docs take no position on partial classes
(checked 2026-08-09), so nothing outranks this.

**File-size caps as measured 2026-08-09:** Sim's 600-line cap holds (max `MetaState.cs`,
590). Game's 400-line cap does **not** — `PlayerGun.cs` is 471 and `DevConsole.cs` is 411.
Either the cap moves or those split; a cap two files violate is not a cap.

## 5. Rejected, by name

These are the recommendations most likely to be proposed by an agent reading generic Unity
architecture advice. Each is rejected for this project with a stated trigger to revisit.

| Pattern | Why not here | Revisit when |
|---|---|---|
| **Update manager / `IUpdatable` registry** | Unity's own measurement is 10,000 `Update` calls costing 9.98 ms with 42 ms of that being user code across the run ([10000 Update() calls](https://unity.com/blog/engine-platform/10000-update-calls)). This project has **8** `Update()` methods. Unity's Manual says the pattern is for "hundreds or thousands" and to profile first ([events-per-frame-optimization](https://docs.unity3d.com/6000.5/Documentation/Manual/events-per-frame-optimization.html)). Building it now buys microseconds and costs registration bugs. | Live MonoBehaviour count with `Update` passes ~500, or a profile shows script-update overhead in the frame budget |
| **DI container (VContainer / Zenject)** | §2. Reflection, opacity, and a wiring graph that currently fits in one `SceneBuilder`. | The wiring no longer fits in `SceneBuilder`, or a second developer joins |
| **ScriptableObject event channels / SO variables** | Moves coupling out of code, where the compiler and Find-All-References can see it, into asset references, where only Unity's dependency browser can. Unity's own SO e-book warns that runtime SO edits **do not persist in a build** while they do persist in the Editor — an editor/build divergence. Genuinely contested: Unity and Unity Atoms document zero drawbacks, which is itself a signal; [cathei](https://github.com/cathei/AntiScriptableObjectArchitecture) documents several. | A non-programmer needs to rewire game logic without touching code |
| **SO as mutable runtime state** | Same divergence, plus shared-mutation hazards and domain-reload retention. | Never for Run/Meta state — that is `Sim/`'s job and it is serialized deliberately |
| **State classes for `RunPhase`** | Unity Learn's own caveat: with few states "the extra structure can be overkill" ([State pattern](https://learn.unity.com/tutorial/develop-a-modular-flexible-codebase-with-the-state-programming-pattern)). The class-based refactor also tends to leak — states end up reaching into the owner's fields. | A phase needs its **own data** that would otherwise be a nullable field on `RunSim` valid in only one phase. State *count* is not the trigger |

**Keeping `RunPhase` from rotting without promoting it.** Three cheap rules, worth more than
the refactor: one validated `SetPhase` with an explicit legal-transition set that throws in
the editor; explicit `OnEnter`/`OnExit` switches so setup does not scatter to call sites; and
no reading the enum outside `RunSim` — expose intent-named predicates instead, because
`if (phase == RunPhase.Tethered)` appearing in forty files is the actual decay.
