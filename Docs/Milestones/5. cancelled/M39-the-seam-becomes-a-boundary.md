# M39 — The seam becomes a boundary

<!--
Status: later
Update trigger: when an assembly is added or split, when Sim/ gains a dependency, or when the offline harness's source list changes
Last updated: 2026-08-09
-->

`Sim/` becomes its own assembly with `noEngineReferences: true`. **After this,
`using UnityEngine;` in `Sim/` is `CS0246` rather than a code review.** The multiplayer seam
stops being a convention held by everyone remembering it.

## Story

As the developer, I want the one structural rule this project has enforced by the compiler,
so that it survives the sessions where nobody is thinking about it.

## Why it matters

**This is the rule the whole architecture exists for and it is the one nothing checks.**
`Architecture/README.md` lists it first under *Never do*. `Docs/Roadmap.md` says co-op "is
paid for continuously" through this seam. It has held for the entire project on discipline
alone.

**It has held.** Audited 2026-08-09, exhaustively: 34 files, 3,386 lines, and the only
`UnityEngine` match in the whole folder is the word inside a comment in `DecoyState.cs:4`.
No `UnityEditor`. No `Mathf`, `Vector3`, `Quaternion`, `MonoBehaviour`, `ScriptableObject`,
`SerializeField`, `Time`, `Application`, `PlayerPrefs`, `JsonUtility`, `GameObject`,
`Transform`, `Physics`. The five `[Serializable]` attributes are `System.Serializable`.
Every `using` in the folder is `System.*`, `System.Collections.Generic`, or
`GatekeeperBastion.Util`.

**One file blocks the split, and it turns out to be nearly empty.** `Sim/` calls `Util.Log`
from **two files, fourteen sites**. Thirteen are `Log.Info` in `RunSim.cs`, and `Log.Info` is
`[Conditional("GKB_VERBOSE")]` — **`GKB_VERBOSE` is defined nowhere in the project.** Not in
`ProjectSettings.asset`, not in the `.csproj`, not in the harness. **Those thirteen lines have
never executed, in the Editor or in any build.** The fourteenth is one refusal message at
`MetaState.cs:124`.

So the real cost of `noEngineReferences: true` is **one call site**, and the project already
has the pattern for it: `Sim/Settings/ISettingsStore.cs` is a Sim-declared interface that
`Game/Flow/PlayerPrefsSettingsStore.cs:14` installs with `Settings.Bind(...)`. Same shape,
already shipped, already passing `1-Boundaries.md` section 2's no-container rule.

Leverage tier (`../../Working-Rules.md`): **#10, structure.** It changes what is possible
rather than telling anyone anything — the highest tier any milestone in this project has
claimed.

**Multiplier** (the tooling exception, `../../_meta/_definitions/milestone.md`): co-op is an
epic paid for continuously rather than scheduled (`../../Roadmap.md`). Every session from here
either holds the seam or does not, and after this one the compiler decides.

## Acceptance criteria

1. **`Assets/Scripts/Sim/GatekeeperBastion.Sim.asmdef` exists with
   `"noEngineReferences": true`** and no references.
2. **All four consuming assemblies reference it.** asmdef references are not transitive and
   three of these are easy to miss:

   | Assembly | Why it needs the reference |
   |---|---|
   | `Assets/Scripts/GatekeeperBastion.asmdef` | the Game layer |
   | `Assets/Scripts/Editor/GatekeeperBastion.Editor.asmdef` | `SceneBuilder.cs:2` and `SceneBuilder.Enemies.cs:2` both `using GatekeeperBastion.Sim` |
   | `Assets/Tests/EditMode/GatekeeperBastion.Tests.asmdef` | 24 files use the Sim namespace, and it sets `"overrideReferences": true` |
   | `Assets/Tests/PlayMode/GatekeeperBastion.PlayTests.asmdef` | 6 files, same shape |

   **The project compiles in Unity before criterion 6 is attempted.** Miss one of these and
   nothing builds, which reads as the split having failed rather than as a missing line.
3. **`Sim/` declares the log sink and the Game layer installs it.** One interface, one method
   taking tag and message, modelled on `Sim/Settings/ISettingsStore.cs`.
   **An interface, not a `static` delegate field** — `Log.Info` already carries
   `[Conditional]`, and C# rejects a method-group conversion from a conditional method with
   `CS1618`. A body that *calls* one is fine; a delegate bound to one is not.
4. **The thirteen `Log.Info` calls in `RunSim.cs` are deleted, not rerouted.** They have never
   run. Rerouting them through the sink would make them run for the first time and add
   thirteen lines to every Editor session log; wrapping them in a Sim-side `[Conditional]`
   would put a define-dependent branch inside `Sim/`, which
   `../../Architecture/4-Testing-and-Determinism.md` forbids for `#if` and forbids in spirit
   here — the offline harness would compile a different program from the one Unity builds.
   **If a diagnostic is wanted later it is added as an `Event` that actually runs.**
5. **`Log.Metric` is deleted.** It has **zero call sites** in `Assets/` and `Tools/`.
   `Architecture/README.md`'s "147 call sites" for `Event` plus `Metric` is corrected in the
   same commit to the measured **101 and 0**.
6. **`using UnityEngine;` added to any file in `Sim/` fails the Unity compile with `CS0246`.**
   Seeded, observed, removed. This is the milestone.
7. **`Tools/verify/run.sh` stops compiling `Assets/Scripts/Util/Log.cs`.** `Log.cs` is a
   Game-layer file and has no business in the Sim harness; after criterion 3 the harness
   installs its own sink through `Runner.cs`.
8. **`Tools/verify/Shims.cs` loses both fakes.** `grep -n "Debug\|Mathf" Tools/verify/Shims.cs`
   returns nothing. **The `Mathf` shim is already dead** — zero `Mathf` references in `Sim/`
   and zero in `Assets/Tests/EditMode`. `Runner.cs:27`'s `UnityEngine.Debug.Silent = !verbose`
   moves onto the harness sink.
9. **Both test paths pass with the same names and the same counts as today:**
   **282 `[Test]` methods in Unity**, and **266 in the offline harness** — 282 minus the 16
   held by the three files `run.sh` excludes by name (`ControlsTests.cs` 6,
   `Dev/DevSessionTests.cs` 5, `Weapons/ReloadSwapTests.cs` 5). **Both numbers are stated
   because they differ**, and an implementer who finds only one of them will assume the other
   is a regression. No exclusion is removed to make a number match.
10. **No player-visible change, and one log line changes.** `MetaState.cs:124`'s refusal
    message still appears, still worded identically. Nothing else in the session log moves,
    and no gate on any shipped milestone moves.

## Implementation

**Create** `Assets/Scripts/Sim/GatekeeperBastion.Sim.asmdef`.
**Change** the four asmdefs in criterion 2.

**Create** the sink interface in `Sim/`, next to `Sim/Settings/ISettingsStore.cs` or in
`Sim/Diagnostics/`. **Change** `Sim/Meta/MetaState.cs:124` to call it.
**Change** `Sim/Run/RunSim.cs` — delete the thirteen `Log.Info` calls.
**Change** the Game layer's startup to install an implementation that calls `Log.Event`.

**Change** `Assets/Scripts/Util/Log.cs` — delete `Metric`.
**Change** `Architecture/README.md` — the call-site count.

**Change** `Tools/verify/run.sh` — drop `$LOG_CS` from `SOURCES` and delete the existence
check for it. **Change** `Tools/verify/Shims.cs` — delete `Debug` and `Mathf`.
**Change** `Tools/verify/Runner.cs` — install the harness sink, move `Silent` onto it.

Rejected: keeping `Util.Log` reachable from `Sim/` by moving `Log.cs` into the Sim assembly
behind a Unity-free write path. It puts the engine-facing file on the wrong side of the seam
to avoid deleting fourteen calls, thirteen of which have never executed.

Rejected: a `static` delegate field as the sink. `CS1618`, per criterion 3.

**Rejected here, and recorded so it is not re-proposed: making `Log.Event` `[Conditional]`.**
The intent is right — 101 call sites building strings in a release build is real cost — but
**this project has no release build.** It is gray-box, solo-playable, and every measurement it
has ever made comes from an Editor session log. Attributing away the logging to save a build
nobody produces, in the same milestone that moves the seam, is how criterion 10 gets broken by
accident. **Revisit when a non-development build is actually produced**, and gate it on a
log-shape diff rather than on a byte-identical one — see `../../GatekeeperZo Notes/verification.md`.

## Supporting work

Developer infrastructure, so the three questions are answered for the developer.

- **Driven** — the Unity compiler, on every edit.
- **Shown** — `CS0246`, at the moment the mistake is made.
- **Reads back** — the error, with `file:line`.

## Verification

1. Criterion 2 — open Unity and compile before touching anything else. Zero errors.
2. Criterion 6 — seed `using UnityEngine;` in `RunSim.cs`; expect `CS0246`; remove it.
3. Criterion 9 — `Tools/verify/run.sh` reports **266**; the Unity EditMode runner reports
   **282**; both with the same test names as before the change.
4. Criteria 7, 8 — the greps, plus a harness run that still prints game log lines under
   `--verbose`.
5. Criterion 10 — play one Wave and confirm the refusal path at `MetaState.cs:124` still logs
   its line. Diff the tag set of the session log against the previous session's: same tags,
   minus nothing.

## Watch

- **The four asmdefs in criterion 2 are the whole risk of the first hour.** `overrideReferences: true`
  on the two test assemblies means they do not inherit anything; a missing line there fails
  every test rather than one.
- **The asmdef split will make compiles slower, not faster.** `Sim/` is edited constantly and
  `Game/` depends on all of it, so expect a near-full rebuild on Sim edits. **Adopt it for the
  compile error, not for iteration speed** — the widely repeated "10 to 100 times faster"
  claims for asmdefs are unsourced (`../../Architecture/1-Boundaries.md` section 1).
- **The sink is a place a counter could grow.** It takes a tag and a message and returns
  nothing. If it ever gains a level enum, a filter or a buffer, that is
  `../../Decisions.md`'s two-stream logger arriving through the back door.
- **Deleting thirteen unexecuted lines will feel like losing diagnostics.** It is not. They
  have never produced a character of output. The record of what they said is in git.

## Depends on / Unblocks

**Depends on nothing.** Not on M35 — it touches no UI file. This is the half of the
architecture work that is ready today and compiler-enforced, which is why it goes first.

**Unblocks M40 and M41**, which make the remaining rules measurable and then check them.
