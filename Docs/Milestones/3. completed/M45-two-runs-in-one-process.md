# M45 — Two Runs in one process

<!--
Status: completed 2026-08-12
Update trigger: closed
Last updated: 2026-08-12
Closed on: offline harness 283/283 with poison proof observed (seeded static failed both reflection teeth, green on revert); Unity suite EditMode 315/0 including the four TwoRunIsolationTests, PlayMode 23/0 (report 2026-08-12T22:59:06Z). Review closed three execution gaps first: enemy static live list retired to the spawner ledger, DecoyBay adopts RunContext.Decoy, CoreLoopPlayModeTests wires the hatch's serialized _game the way the authored scene does.
-->

Everything that becomes ruinously expensive after co-op starts, done now, while the game is
still single-player. **No netcode integration, no second player, no transport.** The gate is
one EditMode test: two independent Sim Runs advance in the same process without touching each
other.

**Multiplier:** makes the first co-op slice integration rather than a rewrite, and every future
isolation EditMode test cheap to write against the same context shape.

## Story

As the developer, I want the single-player codebase to already have the shape co-op needs, so
that adding co-op is integration work rather than a rewrite.

## Why it matters

**This is the thing that kills co-op retrofits, and it is not the netcode.** Unknown Worlds on
Subnautica: adding multiplayer *"would take us many months of work. We almost have to start
over from the beginning, as it affects everything"*
([statement](https://unknownworlds.com/en/news/subnautica-multiplayer-when-if-how-and-why)).
They did not say the networking was hard. They said it touched everything.

**The existence proof is one person.** Lethal Company is solo-developed in Unity, 4-player PvE
co-op, 10M+ copies, host-authoritative, off-the-shelf netcode, no prediction, no dedicated
servers, no host migration ([networking notes](https://lethal.wiki/dev/advanced/networking)).
The architecture that ships this genre is not elaborate.

**The tree is close already.** Re-measured 2026-08-12: two `Instance` singletons
(`GameController`, `HatchStation`; `DecoyBay.Instance` already retired), thirty-one
`Find*`/`Camera.main` lines across eighteen Game files, one gameplay prefab `Instantiate`
(`WaveSpawner.Groups.cs:173`), seven files reading `Input.` directly, two gameplay Random
sites plus one cosmetic, and **zero `Time.` references in `Sim/`**. `RunSim.Tick` already
exists; gun / AI / move still advance from peer `Update`s. This is a week of work now and a
month of work after co-op starts.

Leverage tier (`../../Working-Rules.md`): **#10, structure.** No constant moves and nothing is
player-visible; what changes is what is possible later.

## The correction this milestone carries

**`noEngineReferences: true` is dropped, and M39 is cancelled.** The strict rule is load-bearing
for exactly one thing — cross-platform bitwise determinism for lockstep or deterministic
rollback — and this project is doing neither.

- Unity's own docs: NGO *"doesn't support full client-side prediction and reconciliation"*
  ([client anticipation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html)).
- NGO's documented physics model runs Unity physics **on the server**
  ([physics](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/manual/advanced-topics/physics.html)),
  which a pure sim cannot call.
- Unity's own co-op reference sample puts game logic in `NetworkBehaviour` and uses
  `NavMeshAgent` server-side
  ([Boss Room architecture](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/samples/bossroom/architecture.html)).
- Photon Quantum mandates **float-freedom, not engine-freedom**: `FP` fixed-point "completely
  replaces all usages of floats and doubles to ensure cross-platform determinism"
  ([Fixed Point](https://doc.photonengine.com/quantum/current/manual/quantum-ecs/fixed-point)).
  Quantum 3 is a Unity Verified Solution with Unity as its view layer, so it is not evidence that
  a sim must leave the engine. It is evidence that a *deterministic* sim must own its own math,
  physics and navigation, which is the same conclusion the NavMesh singleton forces below.

**Overwatch is the citation to stop using.** Its simulation is its engine, and the separation
it describes is by tick rate, not by type purity.

**The client-authoritative own-movement decision has a named NGO mechanism**, so it is not left
as an intention: `AnticipatedNetworkVariable<T>` and `AnticipatedNetworkTransform`
([client anticipation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html)).
NGO 2.x also added **distributed authority** as a first-class topology; it is documented as
unsuitable where one machine should govern the physics simulation, so host-client stands and the
alternative is rejected deliberately rather than missed.

**"No NavMesh inside the sim" is permanent, not a current limitation.** `NavMesh` is a documented
singleton with static queries, and the experimental multi-world API that promised isolation is
**obsoleted without replacement** in Unity 6
([NavMeshWorld](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Experimental.AI.NavMeshWorld.html)).
Two Run simulations can never path independently in one Unity process at any grain. Recorded so
criterion 12's scoping is not re-litigated as something a later slice could widen.

**What survives is the dependency direction, not the type ban.** `Sim/` may use `Vector3`,
`Quaternion` and `Mathf`. It may not touch a MonoBehaviour, a scene, or ambient time. The cost
of the strict version is concrete: no `Physics.Raycast` and no NavMesh inside the sim, and
`INetworkSerializable` lives in an assembly that references UnityEngine, so strict purity buys a
hand-written mapping layer for every replicated field, permanently.

## Kill criterion

**If criterion 12 cannot be written as an EditMode test without loading a scene or constructing
MonoBehaviours, stop and re-cut this milestone** — the co-op seam is not where this file
thinks. Work started 2026-08-12; the test was written scene-free on day one, so the kill did
not fire. Failure of a later co-op slice is that slice's kill, not this one's.

## Acceptance criteria

### A — the seam is restated with the reason it actually has

1. **`Assets/Scripts/Sim/GatekeeperBastion.Sim.asmdef` exists with
   `"noEngineReferences": false`** and an empty `references` list (no project assembly
   upstream of it). **Corrected 2026-08-12:** the original wording "references only
   `UnityEngine.CoreModule`" is not expressible in asmdef JSON — engine modules are not
   asmdef references, and `noEngineReferences: false` attaches the whole engine set. The
   CoreModule-only surface is enforced where it can be: `Tools/verify/run.sh` stage 1
   compiles `Sim/` alone against CoreModule-shaped shims (no MonoBehaviour / scene / `Time` /
   ambient `Random`), and criterion 12b's reflection tooth asserts the assembly identity.
   `GatekeeperBastion`, `GatekeeperBastion.Editor`, `GatekeeperBastion.Tests` and
   `GatekeeperBastion.PlayTests` all reference it. **The two test assemblies set
   `"overrideReferences": true`** and inherit nothing, so a missing line there fails every test
   rather than one.
2. **The rule in `../../Architecture/1-Boundaries.md` reads: no `MonoBehaviour`, no
   `GameObject`, no scene lookup, no `Time`, no ambient `Random`, no `SerializeField`, no
   `UnityEditor`, and no static mutable state** — and says nothing about `Vector3`. **The reason
   is rewritten from "the multiplayer seam" to testability and one-directional dependency**, in
   the same edit, with the sources above. Lint / Architecture README fences that still say
   `noEngineReferences: true` are updated in the same edit.
2b. **`Sim/` holds no static mutable state.** Four exist today, all in
   `Sim/Settings/Settings.cs:36-39` (`_store`, `_floats`, `_bools`, `_ints`) — process-global,
   per-player, mutated by `Bind`/`Unbind`. `RunSim` never reads them, which is exactly how they
   got there and why the asmdef in criterion 1 is the precondition rather than a formality.
   `Settings` becomes an instance reached through `RunContext` or moves out of `Sim/`.
   (`Declarations` and other `static readonly` tables are fine and stay.)
3. **`../../Decisions.md` Multiplayer entry is extended** (NGO is already chosen, 2026-08-10)
   with **host-client, server-authoritative simulation**, transport over **Steam's relay**, and
   **client-authoritative own-movement** as correct for PvE with no anti-cheat requirement. The
   Deferred row's "pure-C# sim seam" wording is aligned with criterion 2 in the same edit.
4. **This milestone does not choose a netcode library** (already decided). Criteria B–D transfer
   unchanged between NGO, Mirror and FishNet; the library is not a prerequisite for any of them.

### B — nothing singular that must become plural

5. **No static holds Run or player state.** Fate, decided:

   | Static | Fate |
   |---|---|
   | `GameController.Instance` | **Removed.** Scene host holds / is given one `RunContext`; callers take it from the host or an injected field. |
   | `HatchStation.Instance` | **Kept** as a scene locator (transform / pathing target only). Holds no Run or player state. Named in the allow-list. |
   | `DecoyBay.Instance` | **Already gone** — do not reintroduce. |
   | `GameSession.Meta` | Campaign / cross-run only. Criterion 12 uses **separate `MetaState` instances**; production may keep one campaign Meta. |
   | `PlayerRegistry` | Becomes Run-scoped (or takes an explicit registry from `RunContext`). **`Player` must never become a singleton.** |
   | `EnemyGatekeeper` live list / `SpawnPoint.All` | Run-scoped or scene-owned through the spawner / context; not a process-global actor list that two worlds would share. |

   **Executed 2026-08-12 with one argued narrowing:** `PlayerRegistry` and `SpawnPoint.All`
   stay as static scene REGISTRIES (self-registration lists of scene objects, no Run state)
   rather than moving onto `RunContext`. The argument, recorded in
   `../../Architecture/README.md` *The singletons, named*: this file's own NavMesh finding
   makes a second Game-layer world in one process impossible forever, so scene-scoped and
   Run-scoped coincide at the Game layer — the plural-players hazard is covered because the
   registry already holds a LIST and `Player` is not a singleton, and the two-worlds hazard
   only exists in `Sim/`, which holds no statics at all (reflection-asserted). The enemy
   live list is spawner-owned (`_live` ledger). `GameController`'s duplicate-instance guard
   was deleted with the static it protected. If a later co-op slice falsifies the narrowing,
   the registry moves onto `RunContext` then, as this table originally specified.

6. **No `FindObjectOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`,
   `FindObjectsByType`, `FindObjectsOfType`, `GameObject.Find` or `Camera.main` in gameplay
   code.** Wiring rule: **serialize via SceneBuilder / authored refs where the object is
   scene-stable; pass from the owning controller for runtime-spawned.** No new service locator.

   Re-derive the list at the start of the work; as measured 2026-08-12 it is **31 lines across
   18 files**:
   `GameController.cs:71,358` · `GameController.DevCommands.cs:195,200` ·
   `HudController.cs:104,115` · `HudController.Panels.cs:91` · `HudController.Pause.cs:116` ·
   `DevConsole.cs:355` · `WaveSpawner.cs:148,235` · `FrontEndMusic.cs:95,109` ·
   `MainMenuController.cs:112` · `SceneFlow.cs:77` · `FirstPersonController.cs:94,95` ·
   `ShipController.cs:96,235` · `ShipHud.cs:39,152,160,161,163,168` · `StationTerminal.cs:45,49` ·
   `AudioBuses.cs:36` · `MainMenuView.cs:140` · `UiBind.cs:90` · `UiPitchRandomizer.cs:17`.

   **Exempt, named so the exemption is deliberate:** `DevConsole.cs` and
   `GameController.DevCommands.cs` are dev-only (execution added one site there: the static
   dev `SetLoadout` re-push lost `Instance` like everyone else and now does a dev-only
   lookup). **UI and menu code is not exempt** — those are the sites that silently return
   *a* player rather than *the* player.

   **Four more exemptions were named during execution, argued not slipped** — each is
   process-singular infrastructure with zero player-or-Run ambiguity, which is the hazard
   this criterion exists to kill: `SceneFlow.cs` (EventSystem dedupe across additively
   loaded scenes — no owner can know about duplicates by construction), `FrontEndMusic.cs`
   (DDOL music-host dedupe, same shape), `AudioBuses.cs` (settings-change volume broadcast
   to authored AudioSources; the real fix is an AudioMixer asset, deferred), and
   `UiBind.cs` (the bind-time locator for Shift's third-party singleton managers, already
   gated by `UiBindGatingTests`). All six exempt files are now a lint fence
   (`3-Unity-Runtime.md` section 9, `scenelookup_allow`) — the grep this criterion
   specified is a standing check in `Tools/lint-arch/run.sh`, not a one-time audit.
   Criteria 8 and 10's greps became fences the same way (`ambientinput_allow`,
   `ambientrandom_allow`).
7. **All gameplay prefab `Instantiate` and matching `Destroy` go through one spawner with a
   stable ID** (sequential int on `RunContext` is enough). `WaveSpawner.Groups.cs:173` is the
   only gameplay prefab site. **UI sites exempt, named:** `OptionsView.cs:308`,
   `ShortcutPanelView.cs:141,235`. VFX, decals, shells, and `CreatePrimitive` presentation
   props stay local forever.

   **The `Destroy` half is 25 sites and is the one that will be skipped.** The nine that are
   gameplay or Run-lifetime, named so "matching `Destroy`" is checkable rather than aspirational:
   `DecoyBay.cs:95`, `DecoyObject.cs:39`, `DecoyPlacement.cs:135`, `GameController.cs:99`,
   `GameController.DevCommands.cs:204`, `EnemyGatekeeper.cs:410`, `WaveSpawner.cs:152,239`.
   The remaining sixteen are material, VFX and audio cleanup and stay local.

### C — input, time and randomness become data

8. **Input is one serializable struct `PlayerInputFrame`, produced in exactly one place.**
   Fields: move XY, look XY, sprint, fire, altFire, reload, slot bits (primary / secondary /
   drone / last / cycle), weaponAbility, interact, pullDraw, dial±, wheel±, freeCursor,
   confirm, togglePanel, toggleMissionDrawer — matching today's `ControlId` set.
   **`Controls.Sample()` (or a thin `InputReader` beside it) is the only producer**; gameplay
   reads the frame. **No gameplay file calls `Input.` directly.** Re-derive at start; as of
   2026-08-12 the seven files are `Controls.cs` (producer), `FirstPersonController.cs`,
   `HudController.Pause.cs`, `GameController.EndScreen.cs`, `EndScreenView.cs`,
   `SplashController.cs`, and `Dev/DevConsole.cs` (**dev-only, exempt**).
9. **Gameplay time comes from one accessor passed into the tick** (the `dt` argument), not from
   `Time.` inline inside systems that mutate Run / combat / player state. Presentation keeps
   using `Time.deltaTime` freely. `Sim/` already has zero `Time.` references and must keep
   them.

   **Exempt, named 2026-08-12 so the rule does not dissolve silently** — each is a
   single-machine concern that never replicates, the same argument as criterion 10's crit
   exemption: enemy AI self-timing (`EnemyGatekeeper.Update` reads `Time.deltaTime` once at
   its own boundary — permitted by criterion 11 step 7 until the later slice that folds AI
   in), `PlayerHealth`'s last-attacker attribution window (`Time.time`, kill-credit logging
   only), `PlayerGun`'s same-frame arm latch (`Time.frameCount`, input coalescing), and the
   hub-scene lockouts in `StationTerminal` / `ShipController` (Ship scene, outside the
   Bastion fight tick entirely).
10. **Spawn placement draws come from the seeded generator on `RunSim`**, and the seed is
    logged on `run begins`. **`WaveSpawner.Groups.cs` is the seeded site** (it already prefers
    `RunSim.Rng`; remove the `UnityEngine.Random` fallback). **Exempt, named, matching
    `../../Architecture/4-Testing-and-Determinism.md`:** `PlayerGun.Firing` crits and pellet
    cone stay on ambient `UnityEngine.Random` so trigger pulls do not advance the replayable
    stream; `UiPitchRandomizer.cs` is cosmetic.
11. **Bastion fight advances from one explicit caller with this written system order**, rather
    than peer `Update()` methods relying on Unity's script order:

    1. Sample `PlayerInputFrame`
    2. `RunSim.Tick(dt)`
    3. Prep commands (draw / dial)
    4. Player locomotion
    5. Combat actions (gun / ability / decoy confirm)
    6. Interact / repair
    7. Enemy AI ticks (may remain engine-driven views until a later slice; order is still
       documented and they must not read ambient Input / `GameController.Instance`)
    8. Spawn delivery (Tick-driven group emission replaces gameplay-critical
       `WaitForSeconds` dependence where it gates clear)
    9. Assault-clear probe
    10. Presentation last (HUD / VFX / audio keep `Time.deltaTime`)

    One caller owns steps 1–9 (`GameController.Update` or a thin `RunTicker` it invokes).
    Presentation `Update`s stay local.

### D — the gate

12. **An EditMode test constructs two independent Sim Runs in one process — each a
    `RunContext` with its own `MetaState` and `RunSim` — advances both with different command
    / input sequences, and asserts phase, wave index, hatch HP and salvage differ with no
    cross-talk.** **This is the milestone.** "Run simulation" here means **Sim-owned state
    only** (`RunSim` + owned siphon / ship / salvage / rng / decoy state). It does **not**
    include MonoBehaviours, NavMesh, `WaveSpawner` Instantiates, HUD or audio. Criteria 5–11
    are verified by greps and the written Tick order, not by spawning two fight loops in
    EditMode. The test fails if the two contexts share one `MetaState`, or if a static
    `RunContext.Instance` (or equivalent) is reintroduced.

    **Measured 2026-08-12, and this is the thing to design against: the criterion as stated is
    nearly met by code that already exists.** `RunSim` takes its `MetaState` by injection
    (`RunSim.cs:165`) and owns a seeded `System.Random` (`:168`), so two instances are already
    independent by construction, and `Tests/EditMode/Run/CoreLoopTests.cs:114` already builds a
    second `RunSim` with a second `MetaState` inside one test. Worse, `RunSim.Tick(dt)`
    (`:450-470`) is a phase guard, `RunElapsed += dt`, and a deep-start inter-wave timer — so
    "advance both and assert they differ" is satisfied by `Tick(1f)` versus `Tick(2f)`. The
    scoping decision above is right; the assertions are what is thin.

12b. **The gate has to bite, so it carries two teeth that current code cannot pass:**

    - **The advance goes through the deep-start chain**, which is the only branch of `Tick` with
      logic. Each context is driven through wave launch, delivery notify, and the inter-wave
      timer firing, so `CurrentWaveIndex` diverges because the chain advanced and not because
      two floats differ.
    - **Replaying a seed reproduces its Run exactly.** A third context seeded identically to the
      first, driven with the identical command sequence, ends in identical state. Determinism is
      the property co-op actually needs, it is free to assert against the existing `Rng`, and it
      is the assertion that fails the moment an ambient `Random` or an ambient clock creeps back
      into the sim.

    **Re-cut 2026-08-12, before work started, per Verification step 4.** Run against HEAD,
    both teeth above PASSED — `RunSim` was already injection-clean and seed-owned, so they
    measured construction, exactly the failure the Watch section predicted. The gate gained a
    third tooth, and it is the one that was red at HEAD:

    - **The Sim assembly holds no static mutable state, asserted by reflection** over
      `typeof(RunSim).Assembly` (plus: the assembly must BE `GatekeeperBastion.Sim`, so the
      asmdef's existence is itself asserted). Red at HEAD on exactly `Settings`' four statics;
      green once criterion 2b moved them out. This is the standing guard against the next
      Settings-shaped leak, and `RunContext` additionally asserts zero static members so an
      `Instance`-equivalent cannot return under another name.
13. **`Tools/verify/run.sh` and the Unity suite both pass with the same test names as before,
    plus the new one.**
14. **No player-visible change.** Existing suite green; greps match the named exemptions;
    the new seed log line from criterion 10 is present with its expected tag. No editor play
    session is required to close this gate.

## Implementation

**Create** `Assets/Scripts/Sim/GatekeeperBastion.Sim.asmdef` — `"noEngineReferences": false`,
reference `UnityEngine.CoreModule` only.

**Create** `Assets/Scripts/Sim/Run/RunContext.cs` (or equivalent name under `Sim/Run/`):

```csharp
sealed class RunContext
{
    public MetaState Meta;     // own instance for isolation tests
    public RunSim Sim;         // phase, waves, hatch, salvage, Rng, Tick
    public DecoyState Decoy;    // today on DecoyBay — reference explicitly
    public int NextSpawnId;    // stable IDs for criterion 7
}
```

**Create** `PlayerInputFrame` (plain serializable struct beside `Controls`) and
`Controls.Sample() -> PlayerInputFrame` as the sole producer. Keep `PushForTest` /
`ClearTestBindings` for EditMode.

**Create** `Assets/Tests/EditMode/Run/TwoRunIsolationTests.cs` last — two `RunContext`s,
distinct metas and seeds, divergent commands, assert independence; poison cases: shared meta,
and a static on the context.

**Change** `GatekeeperBastion.asmdef` and the Editor / Tests / PlayTests asmdefs to reference
Sim; update `Tools/verify/run.sh` for CoreModule.

**Change** `../../Architecture/1-Boundaries.md`, Architecture README fences,
`../../Architecture/4-Testing-and-Determinism.md` only if wording still conflicts with
criterion 10's exemptions, and `../../Decisions.md` Multiplayer / Deferred rows (criteria 1–4).

**Change**, one behaviour-preserving commit each, in order:

1. Drop `GameController.Instance`; host holds `RunContext`. Keep `HatchStation.Instance` as
   locator-only; scope `PlayerRegistry` / enemy live list / spawn points per criterion 5.
2. Clear Find* / `Camera.main` sites per the wiring rule (criterion 6).
3. Route gameplay prefab spawn through the spawner + stable ID (criterion 7).
4. Land `PlayerInputFrame` + sole producer; migrate the five gameplay `Input.` consumers
   (criterion 8).
5. Pass `dt` into gameplay systems that mutate state; leave presentation on `Time.`
   (criterion 9).
6. Remove `WaveSpawner.Groups` ambient Random fallback; log seed on `run begins`
   (criterion 10).
7. Fold Bastion fight into the written Tick order; one caller (criterion 11).
8. Two-Run EditMode test (criterion 12).

Rejected alternative for criterion 12: proving two full Bastion fight loops in one process.
That needs NavMesh, Instantiates and Update extraction at a different grain; co-op wants
**one** Run and N players, so Sim isolation plus Game greps is the right proof.

Rejected, and recorded so none of it is re-proposed:

| Not building | Why not, and when it would change |
|---|---|
| Client prediction and rollback | Solves own-character input lag under server authority. Client-authoritative own-movement is free and correct for PvE. Revisit only for PvP |
| Lag compensation | Server rewinding hitboxes. In PvE the shooting client can decide its own hits |
| Dedicated server | Costs money and a second build target forever. Host-client is standard at 2-4 players. If ever wanted, ship the binary and host nothing |
| Host migration | A product feature, and expensive. Lethal Company shipped without it |
| Anti-cheat | Players invite their friends |
| Custom serialization, interest management | Bandwidth problems that do not exist at four players |
| An ECS rewrite | A scope decision about entity counts, not a networking one. Do not let co-op be the excuse |
| Deterministic lockstep, fixed-point math | The requirement this project explicitly does not have |
| A DI container or update-manager | Rejected in `../../Architecture/1-Boundaries.md`; explicit pass / serialize wiring is enough |
| Choosing the netcode library inside this file | Already decided in `../../Decisions.md` (NGO, 2026-08-10). Not a prerequisite for B–D |

## Verification

1. Criteria 5 to 10 — the greps, expecting the named exemptions and nothing else.
2. Criterion 11 — the written order appears in code (comment or `RunTicker` step list) and one
   caller invokes it; peer gameplay `Update`s that still mutate fight state are gone or are
   presentation-only.
3. Criterion 12 — `TwoRunIsolationTests` (or the name that lands). Assert divergent state and
   no cross-talk. **Then** force a shared `MetaState` and confirm the independence assert
   fails; **then** seed a static accessor on `RunContext` and confirm the test fails; **then**
   put one of `Settings`' statics back in the sim's path and confirm the replay assert fails. A
   gate that cannot fail is not a gate.
4. Criterion 12b — run the test against `HEAD` before any of criteria 5 to 11 land. **It must
   fail there.** If it passes, the assertions are measuring construction and the criterion is
   re-cut before the work starts, not after.
5. Criterion 13 — both suites, same names plus the new one.
6. Criterion 14 — suite green + greps + seed log tag present. No Play-mode session required.

## Watch

- **Criterion 12 is the whole milestone.** Everything above it is a means. If the two-Run test
  is hard to write without a scene, that is the kill criterion firing, not an obstacle to
  route around.
- **The opposite failure is the likely one: the test is trivially green on day one.** Two
  `RunSim`s with two `MetaState`s already coexist in `CoreLoopTests.cs:114`. If
  `TwoRunIsolationTests` passes before criteria 5 to 11 land, it is measuring construction
  rather than isolation, and 12b is what makes that visible. A gate that is green before the
  work is the same defect as a gate that cannot fail.
- **`Settings`' statics are the shape of the next leak.** Nothing in `Sim/` reads them today, so
  they cost nothing today, and that is precisely why they survived. Criterion 2b removes them;
  the asmdef is what stops the next four.
- **The exemptions are the risk.** Every one of them is named here on purpose — HatchStation
  locator, UI Instantiate, dev console Input, gun crit / cone Random, cosmetic pitch. An
  exemption added later without being named is how the rule dissolves.
- **Do not start the netcode integration inside this milestone.** The next slice is small and
  specific: *the client shoots one server-spawned enemy, it dies, and the wave counter reads the
  same number on both screens.* It gets its own file.
- **Networked physics is the part everyone underestimates.** The R.E.P.O. developers, on a
  Unity co-op game that sold millions: *"making all the physics feel smooth for clients was
  something we struggled with"*
  ([Photon case study](https://blog.photonengine.com/r-e-p-o-multiplayer-success-powered-by-photon/)).
  Budget for it when it arrives; do not pre-solve it here.

## Depends on / Unblocks

Depends on nothing. **Replaces M39**, which was premised on `noEngineReferences: true`.

**Unblocks the first co-op slice**, and is the reason it can be a slice rather than a rewrite.
