# 4 — Testing and determinism

<!--
Purpose: Which tier a test belongs in, what the offline harness can and cannot prove, what actually breaks determinism, and what this project deliberately does not test.
Status: working
Authority: MANDATORY — a gate closed on Path 1 alone is not closed.
Update trigger: when the harness changes, when a PlayMode test is added, or when a determinism requirement changes (lockstep would rewrite §3).
Last updated: 2026-08-25 (tier table re-read against verify/run.sh and the newest report; save fixtures named)
-->

## 1. Three tiers, and what each is for

| Tier | Runs | Proves | Cost |
|---|---|---|---|
| `Tools/verify/run.sh` (mcs + mono) | The language, arch and vendor lints, then Sim + `Log.cs` + EditMode tests with no Unity, then docgen `--check` | The rules | seconds |
| EditMode (Unity Test Runner) | Same tests, Unity's compiler and NUnit | That the above did not diverge from Unity | ~18 s for the same suite |
| PlayMode | In the player loop | Engine-mediated facts only | domain reload per run |

The lints run **before** the compile so a violation is not buried under mcs noise, and any
one of them failing stops the harness. Tier 2 and 3 land together in
`gkb_verify_report.json` at the repo root, which carries `status`, `stage`, `message` and a
per-tier `{passed, failed, skipped, tests}` — read it rather than counting tests from a doc.

The Unity Test Framework classifies a test by **its assembly's references**, not by an
attribute: EditMode assemblies declare `"includePlatforms": ["Editor"]`; PlayMode assemblies
must not, and cannot reference `Assembly-CSharp`
([Edit mode and Play mode tests](https://docs.unity3d.com/6000.4/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)).

**Rule: default to `[Test]`, not `[UnityTest]`.** Unity's own guidance — use `[UnityTest]`
only when you need to yield instructions or skip frames.

**What genuinely requires PlayMode**, and nothing else does: physics stepping and
collision/trigger callbacks, NavMesh agents actually moving, Animator and particle systems
advancing over frames, coroutine-based gameplay, `Awake`/`OnEnable`/`Start` ordering, scene
load/unload, and anything that must run on a device.

**Rule: an assertion about the game's rules belongs in Sim, not in PlayMode.** The project
already has a fast tier; anything expressible there and put in PlayMode instead is a
self-inflicted tax. This is contested — teams whose logic lives in MonoBehaviours have no
cheap tier and reasonably go PlayMode-first — but that argument does not apply to a codebase
with a pure Sim.

**The counter-case that does apply:** PlayMode's cost is mostly *fixed*. 400 PlayMode tests
are not 400× worse than one. Minimise the number of PlayMode **runs** in the inner loop, not
the number of tests in CI.

## 2. What the offline harness cannot prove

`Tools/verify/run.sh` is the inner loop and it is right to be. Its divergences from Unity,
worst first — this is the section that matters, because a green harness is not a green build.

| Divergence | Consequence |
|---|---|
| **No Unity native backend** | Anything touching engine services gives silent wrong answers rather than exceptions. This is the strongest argument for `1-Boundaries.md` §1: an assembly that *cannot* reference Unity beats one that merely happens not to |
| **Different NUnit and runtime** | Unity ships a customised NUnit 3.5; mono/mcs and Unity's Mono are not the same build. BCL behaviour can differ — see `System.Random` in §3 |
| **IL2CPP is AOT** | No `Reflection.Emit`, no `dynamic`, managed stripping can remove reflection-only types. A green harness says nothing about an IL2CPP player ([scripting restrictions](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-restrictions.html)) |
| **Different defines** | `UNITY_EDITOR`, `DEVELOPMENT_BUILD`, `GKB_VERBOSE` are unset. **Keep `#if` out of `Sim/` entirely** or the harness tests a different program |
| **Different float codegen** | Bit-identical float results between mono and a player are not guaranteed |

**Rule: the EditMode mirror run is not optional.** The harness is the inner loop; a Unity
EditMode run of the same tests is the outer loop, and it exists to catch the rows above. This
is what IntegrationCheck's Path 1 / Path 2 split already encodes — the rule here is that a
milestone gate citing only Path 1 has not been verified. Path 2 is driven either from
**GatekeepersProtocol → Validate → Integration Check** or by dropping
`gkb_verify_request.txt` at the repo root; both write `gkb_verify_report.json` beside it.

## 3. Determinism

**What this project needs today: replay determinism on one machine, one build.** Under that
requirement floats are fine, and the rules are cheap. **What it would need for the co-op the
Sim seam exists for: cross-platform lockstep.** Under *that* requirement floats are not fine
and the fix is fixed-point — a large, irreversible tax on every math call and every tuning
value. **Do not pay that tax on speculation.** Pay it when lockstep is committed.
([Gaffer: Floating Point Determinism](https://gafferongames.com/post/floating_point_determinism/),
[Photon Quantum's Q48.16](https://doc.photonengine.com/quantum/current/manual/quantum-ecs/fixed-point))

The cheap rules, all of which this project already follows or nearly does:

**dt is injected, never read.** `RunSim.Tick(dt)` is the entry point and Sim never touches
`Time`. The testing consequence is the point: with dt injected, a determinism test is
`for (i…) sim.Tick(step);` plus a hash of the final state. With `Time.deltaTime` inside Sim
that test cannot exist. Presentation-layer smoothing, camera lerp and VFX *should* use
`Time.deltaTime` — this is a boundary rule, not a ban.

**RNG state is sim data.** `RunSim.Rng` is seeded and part of the run. Two hazards worth
naming: `System.Random`'s algorithm and default seeding **differ between .NET Framework and
.NET Core** ([Random ctor](https://learn.microsoft.com/en-us/dotnet/api/system.random.-ctor)),
so `new Random(42)` can produce different sequences under the mcs harness than under Unity —
which will look exactly like a determinism bug. If a fixture ever pins an RNG sequence, own
the generator (a ~20-line xorshift struct with a public `State`) rather than depending on
`System.Random`'s. And keep separate streams per concern so adding a cosmetic roll does not
shift combat outcomes.

**`PlayerGun` deliberately uses `UnityEngine.Random` for crits** so trigger pulls never
advance the replayable stream. That is correct and `Architecture.md` already records it. The
implication to state: **crits are therefore not reproducible from a seed**, so any test or
tool that depends on exact damage must disable crit or account for it.

**What else breaks determinism**, and the ones that are live here:
`Dictionary`/`HashSet` **iteration order** is not a stated guarantee — sort by ID for anything
sim-affecting; sorting by reference or by an unstable key; uninitialised struct fields;
`GetComponent`/`FindObjectsByType` ordering and `Awake`/`Start` ordering, all engine-owned
and not contractually stable — which is reason enough on its own to keep Sim out of
MonoBehaviours; and culture-sensitive string comparison — use `StringComparison.Ordinal` in
Sim.

**The highest-value determinism test costs one recorded input log:** replay it and compare
per-tick checksums. Split the checksum per subsystem so a mismatch localises the system
rather than just the tick ([bugnet](https://bugnet.io/blog/how-to-debug-desync-in-deterministic-lockstep-games)).
Worth building only alongside co-op, and named here so it is not reinvented.

## 4. Isolation against persistent state

`PlayerPrefs` and `Application.persistentDataPath` in the Editor write to **real, developer-
visible** locations outside the repo, and nothing resets them between tests. A test calling
`PlayerPrefs.DeleteAll()` wipes the developer's own session
([PlayerPrefs](https://docs.unity3d.com/ScriptReference/PlayerPrefs.html)).

This project has already been bitten: before M17, a test run polluted the real campaign by
+112 salvage, +1 run and +1 death. The fix was `PlayTestUtil.SaveFileGuard` driven by a
`[SetUpFixture]` declared **outside any namespace** so it wraps the whole PlayTests assembly
([NUnit SetUpFixture](https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html)).
That fixture is `Assets/Tests/PlayMode/PlayModeSaveFixture.cs`, with
`Assets/Tests/EditMode/EditModeSaveFixture.cs` doing the same for the other assembly. Do not
nest a second `SaveFileGuard` inside a test: it overwrites `.playtestbackup` and leaves the
fixture unable to restore.

**Ranked, best first:**

1. **Don't reach the boundary.** Sim depends on an interface; Unity implements it over the
   file system; tests use an in-memory one. `PlayerPrefs` is a static class and therefore
   unmockable, which is the whole reason to not depend on it directly.
2. **Redirect the root.** Point the real serializer at a temp directory per test.
3. **Guard and restore** — what `SaveFileGuard` does. **Its failure mode is that a crashed or
   interrupted run never restores**, which is why the guard writes a `.playtestbackup` and
   `[OneTimeSetUp]` recovers an orphaned one. Keep that recovery; it is the load-bearing half.

Two NUnit facts worth writing down because they cause silent double-execution:
`[SetUpFixture]` cannot contain `[SetUp]`/`[TearDown]`, and ordering between two SetUpFixtures
at the same level is **indeterminate** — never make them interdependent. And when a
`[UnityTest]` yields an instruction that triggers a domain reload, **the non-Unity `[SetUp]`
methods run again** before the second half of the test.

**Leakage sources within a PlayMode run:** scene objects persist between tests unless
destroyed; statics survive the whole run because there is one domain reload per run, not per
test; and `Time.timeScale`, `Physics.autoSimulation` and `QualitySettings` are global knobs a
test can mutate and forget. **Randomise test order in CI periodically** — leakage bugs are
order-dependent and a fixed order hides them indefinitely.

## 5. What this project does not test

**Rule: no automated test asserts that a MonoBehaviour has a reference assigned, that a
prefab has a component, or that a click calls a method.** These restate the wiring, break on
every scene edit, and produce the maintenance drag that convinces people testing does not work
in games. The formal name for the discipline is the Humble Object pattern
([xunitpatterns](http://xunitpatterns.com/Humble%20Object.html)) — the Sim/Game split *is*
Humble Object, and the MonoBehaviour is the half you accept as untested.

**What replaces it:**

- **Editor validation, not tests.** A menu-item pass that reports null serialized references
  across prefabs catches more than per-field unit tests, faster.
- **One smoke PlayMode test per scene**: load it, run N frames, assert no errors logged. UTF
  already fails a test when Unity logs an error, so this is nearly free
  ([LogAssert](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/api/UnityEngine.TestTools.LogAssert.html)).
  Treat `LogAssert.ignoreFailingMessages = true` as requiring justification.
- **Play verification** for anything about feel. There is no assertion for "the Wave felt
  hard", and pretending otherwise produces tests asserting arbitrary constants — which is
  precisely how M16's ±15 band got written and then retired.

**Where the community disagrees, stated rather than resolved:** there is a pro-coverage camp
(Unity's own how-to demonstrates testing component instantiation and validity) and an
anti-TDD-in-games camp (gameplay churns too fast during design exploration). Most teams land
phase-dependent: prototype gets no tests; once a system's *rules* stabilise they move into
Sim and get tested; presentation stays untested. That is what this project's architecture
already implies.

## 6. Instrumentation, and the line `Decisions.md` drew

`Docs/Decisions.md` forbids counters, rollups and a stats object after `RunStats` was built
and cut. That decision is correct and this file supplies the reasoning it was missing.

**Emit events; aggregate outside.** A metrics object inside the sim is mutable shared state
written from everywhere — a determinism hazard (write order, float accumulation), a
test-isolation hazard (static state leaking across tests), and a coupling hazard. It is also
the classic wrong-abstraction trap: the aggregation you need in month two is not the one you
defined in month one, and the call sites are already everywhere. And aggregation is one-way —
you can always derive metrics from an event log and never the reverse.

**This is why the session log is the right verification surface** and why M29 is a reader over
it rather than a counter inside it. The rule that follows: **a log line that a tool parses is
a contract.** Changing `killed a {kind} [with {slot}] (+{n} salvage) — {n} left` breaks
`Tools/wave-report` as surely as changing a method signature breaks a caller. Log-format
changes that tools depend on belong in the milestone that makes them, with the tool updated
in the same commit. The optional `with {slot}` segment is M26 kill-share attribution;
`KillRe` matches on kind alone so older logs still parse.

**Where metrics genuinely win** and this rule does not reach: shipping analytics for
retention and funnels, and frame-time histograms for performance telemetry. Neither is a
simulation concern.
