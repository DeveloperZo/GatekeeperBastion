# 1 — Boundaries

<!--
Purpose: Where the lines in this codebase are, what enforces them, and which popular Unity architecture patterns this project rejects and why.
Status: working
Authority: CRITICAL — the Sim seam and the mutation surface are load-bearing.
Update trigger: when an assembly is added or split, when the wiring approach changes, or when a rejected pattern is reconsidered.
Last updated: 2026-08-12
-->

## 1. The Sim boundary: one-directional dependency, enforced by an assembly

`Assets/Scripts/Sim/GatekeeperBastion.Sim.asmdef` exists with `"noEngineReferences": false`
and no assembly references. `GatekeeperBastion`, `GatekeeperBastion.Editor`,
`GatekeeperBastion.Tests` and `GatekeeperBastion.PlayTests` all reference it; nothing it
depends on can ever reference back, because Unity prohibits cyclic assembly references and
surfaces violations as compile errors
([Referencing assemblies](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-referencing.html)).

**Rule: inside `Sim/` there is no `MonoBehaviour`, no `GameObject`, no scene lookup, no
`Time`, no ambient `Random`, no `SerializeField`, no `UnityEditor`, and no static mutable
state.** (`static readonly` tables are fine. `Sim/Util/Log.cs` rides along in the Sim
assembly — plain C# over `Debug`.)

**Reason: testability and one-directional dependency — not engine purity.** What the
boundary buys is EditMode tests that need no scene and no domain reload, logic that cannot
silently depend on frame order, and two independent Runs that can advance in one process
(`Assets/Tests/EditMode/Run/TwoRunIsolationTests.cs` asserts exactly that, including by
reflection over the assembly for static mutable state). The earlier, stricter form of this
rule — no `UnityEngine` types at all — was load-bearing for exactly one thing, cross-platform
bitwise determinism for lockstep or deterministic rollback, and this project does neither:

- Unity's own docs: NGO *"doesn't support full client-side prediction and reconciliation"*
  ([client anticipation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html)).
- NGO's documented physics model runs Unity physics on the server
  ([physics](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/manual/advanced-topics/physics.html)),
  which a type-pure sim could not call.
- Unity's co-op reference sample puts game logic in `NetworkBehaviour` and uses
  `NavMeshAgent` server-side
  ([Boss Room architecture](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/samples/bossroom/architecture.html)).
- Photon Quantum mandates float-freedom, not engine-freedom: `FP` fixed-point "completely
  replaces all usages of floats and doubles to ensure cross-platform determinism"
  ([Fixed Point](https://doc.photonengine.com/quantum/current/manual/quantum-ecs/fixed-point)).
  A *deterministic* sim must own its math; that is not evidence a sim must leave the engine.

So `Vector3`, `Quaternion` and `Mathf` are permitted in `Sim/`, and the cost of banning them
is named so the strict form is not re-proposed: no `Physics.Raycast` and no NavMesh inside
the sim, and `INetworkSerializable` lives in an assembly that references UnityEngine, so
strict purity buys a hand-written mapping layer for every replicated field, permanently.

**Enforcement is layered and none of it is a review item.** The asmdef makes the dependency
direction a compile error. `Tools/verify/run.sh` compiles `Sim/` alone against the stub
surface in `Tools/verify/Shims.cs` — CoreModule value types plus `Debug` — so a
MonoBehaviour, scene call, `Time` or ambient `Random` in `Sim/` fails the offline harness at
stage 1. And `TwoRunIsolationTests.SimAssembly_HoldsNoStaticMutableState` fails the suite if
a static mutable field appears anywhere in the Sim assembly.

**Do not add more assemblies than these.** Unity's own package guidance is that one assembly
per code folder — Editor, Runtime, Tests — "typically suffices"
([Create or edit assembly definitions](https://docs.unity3d.com/6000.4/Documentation/Manual/cus-asmdef.html)).
Core/Gameplay/UI splits are community practice, not Unity guidance, and the usual outcome is
a "Shared" assembly that everything references, which is the god-module you were avoiding.
`GatekeeperBastion.Sim`, `GatekeeperBastion`, `GatekeeperBastion.Editor` and the two test
assemblies is the complete set.

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

**Counter-case.** Authored `[SerializeField]` wiring cannot express interfaces directly,
cannot cross scenes, and cannot reach runtime-instantiated objects — which is why the
`HatchStation.Instance` locator, `PlayerRegistry` and the `SpawnPoint` registry exist, and
why runtime-spawned actors get their references pushed (`Bind` / `Configure`) from the
owning controller instead. Those are the seams a container would clean up. If the wiring
graph ever stops fitting in the authored scenes plus the push calls, revisit this rather
than adding a new static.

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
