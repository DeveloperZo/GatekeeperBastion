# 6 — InfinityQube precedence pass

<!--
Purpose: What was taken from InfinityQube's Assets/Docs/AI Agent, what was adapted, what was rejected and on what evidence, and the one fork that is Zo's to settle. Read once; the outcomes are folded into files 1–4.
Status: record of one pass
Update trigger: when InfinityQube's AI Agent docs change materially, or when a rejection here is overturned.
Last updated: 2026-08-09; calibration figures re-counted 2026-08-30 (the pass itself is unchanged)
-->

Read 2026-08-09: `AI Agent/README.md`, `01-core-constraints`, `02-approval-gates`,
`03-file-standards`, `04-code-patterns`, `05-safe-zones`, `06-validation-rules`,
`09-scope-boundaries`, two task templates, plus `Technical Doc/TechnicalCritiques.md` and
`TechnicalDebt.md`. The AI Agent docs are v2.0, dated November 2024 — **pre-Unity 6**, which
matters for three rules below.

Zo's instruction: InfinityQube takes precedence unless completely outranked by research.
Applied literally. Everything below says which of the two won and why.

## The finding that shapes everything else

**InfinityQube's file-size rule was written, marked CRITICAL, backed by a validation script,
and it did not hold.** Its own audit, December 2025:

| File | Lines | Limit | Over |
|---|---|---|---|
| `WaveManager.cs` | 2,083 | 600 | 247% |
| `PlayerActionManager.cs` | 1,936 | 600 | 223% |
| `CubeManager.cs` | 1,535 | 600 | 156% |
| `GridManager.cs` | 1,384 | 600 | 131% |

Twelve files over limit. And the limits themselves moved to accommodate them — `01-core-
constraints.md` says 600 core / 400 manager, `TechnicalCritiques.md` a year later says 750
core / 600 manager. `TechnicalCritiques.md` names the result: *God Class Anti-Pattern*, in
`WaveManager` and `PlayerActionManager`.

**The lesson is not that the rule was wrong. It is that a rule enforced by a script someone
has to run is a rule that documents a violation rather than preventing one.** That is the
same shape as this project's Sim seam (`1-Boundaries.md` §1), enforced today by
`Tools/verify/run.sh` and by nothing else, and it is why that file argues for the asmdef.

For calibration, re-counted 2026-08-30: **seven** files over, the worst still
`BastionSketchEmitter.cs` at **976** against an 800 cap — **22%** over, against
InfinityQube's 247%. The largest Game file is now `GameController.Terminals.cs` at 611
against 400. `PlayerGun.cs` remains under (371). Not the same problem. Yet.

## Adopted from InfinityQube

Each of these is now referenced by the file that owns it.

1. **Authority levels on every rule** — CRITICAL / MANDATORY / OPERATIONAL / GUIDANCE. Files
   1–4 gain a level in their header. A folder of undifferentiated rules gets skimmed; a
   folder where four things are CRITICAL gets those four followed.
2. **The NEVER DO / ALWAYS DO card.** Terse, memorable, agent-facing, at the front of
   `README.md`. IQ's version is the single most usable page in its folder.
3. **The Complete Unit Rule.** A modification is a whole method from signature to closing
   brace, a whole property with its accessors, or a cohesive cluster — never a partial body
   or an incomplete control structure. This is the best agent-facing rule in the set and
   this project had nothing like it.
4. **POC marking.** `// POC: <what is unfinished and why>`, with IQ's rule attached
   verbatim: **POC code has no upgrade requirement — if it works, it works.** It may violate
   optimisation rules but never safety rules. This lets gray-box code be honest instead of
   pretending to be finished, and it is a better fit for this project than anything I would
   have written.
5. **The file-size table with a *current violations* column and a required action.** IQ's
   format makes a violation visible in the document; this project's cap is a sentence two
   files silently break.
6. **Approval gates by complexity score.** IQ stops at ≥ 7/10 and on any validation failure.
   The mechanism here is the milestone contract and the queue caps, but the trigger list —
   architectural change, new pattern, cross-system modification, external dependency — is
   worth having explicitly rather than by feel.
7. **Safe zones, green / yellow / red.** What an agent may change without asking. This
   project has the shape informally (`Decisions.md` red-zones `RunStats`; `Architecture.md`
   red-zones scene files and singletons) and no list.

## Adapted

**Region organisation.** IQ mandates a fixed `#region` order — Constants, Inspector
Configuration, Manager References, Runtime State, Properties, Unity Lifecycle, Public API,
Private Methods, Debug. This codebase uses no regions at all. **Adopted for new files;
retrofitting 68 existing files is a chore with no player-visible change and would
displace a milestone that has one** (`_meta/_definitions/milestone.md`). If Zo wants the
retrofit it is a one-session sweep, not a milestone.

**File-size limits.** IQ's categories are Core 600 / Manager 400 / Utility 300 / Data 200 /
Interface 100. This project's are Sim 600 / Game 400, which is the same idea against
different categories, and they are already tighter in the place that matters. Keeping this
project's numbers; adopting IQ's table format and its violations column.

## Rejected, with the evidence

### The `enableDebugLogs` debug pattern — completely outranked

IQ mandates this on every manager and major component:

```csharp
[Header("Debug")] public bool enableDebugLogs = true;
private void DebugLog(string methodName, string message) {
    if (enableDebugLogs) Debug.Log($"[{GetType().Name}] {methodName}: {message}");
}
```

**The guard is inside the method, so it does not guard anything the caller pays for.**
`DebugLog("Foo", $"hp {hp}")` builds the interpolated string, allocates an `object[]` and
boxes `hp` **at the call site**, before `enableDebugLogs` is ever read. Under Unity 6's C# 9
there is no interpolated-string handler to avoid this
([C# compiler](https://docs.unity3d.com/6000.5/Documentation/Manual/csharp-compiler.html)).
`GetType().Name` adds a reflection call per log. And `Debug.Log` is not stripped from release
builds, so this ships.

This project's `Util/Log.cs` already does it correctly for `Info` via
`[System.Diagnostics.Conditional("GKB_VERBOSE")]`, which is the only construct that removes
**argument evaluation**, because the compiler deletes the call expression at every call site.
`3-Unity-Runtime.md` §6 extends that to `Event` and `Metric`.

This is not a matter of taste and IQ's own docs half-agree with the conclusion —
`TechnicalCritiques.md` §8 recommends wrapping debug code in
`#if UNITY_EDITOR || DEVELOPMENT_BUILD`, which is the same instinct without the mechanism.

### `FindObjectOfType` "acceptable in Start" — stale, not wrong

`04-code-patterns.md` presents `FindObjectOfType<WaveManager>()` in `Start` as an approved
manager-reference method. It is **deprecated in Unity 6**
([docs](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.FindObjectOfType.html)).
The doc predates Unity 6 and IQ's own code has already moved on — `TechnicalCritiques.md` §6.3
discusses `FindAnyObjectByType` usage. This project is already on the new API. No conflict;
the rule just needs its Unity 6 spelling, which `3-Unity-Runtime.md` §2 carries.

### The singleton pattern — this project is stricter and stays stricter

IQ documents an approved `Instance` singleton with `DontDestroyOnLoad` and duplicate
destruction. `Architecture.md` here says **no new static MonoBehaviour singletons**, with
`GameController.Instance` and `HatchStation.Instance` grandfathered. Keeping the stricter
rule. IQ's pattern is the right *shape* if one is ever approved, and is recorded for that
case only. Unity 6 adds a reason to hold the line: with domain reload disabled, statics keep
their values and static events keep their subscribers between play sessions
([domain reloading](https://docs.unity3d.com/6000.5/Documentation/Manual/domain-reloading.html)).

## The one fork — Zo's call

**`XManager`, `XUtils`, `XHelper` naming.**

IQ mandates it: managers are `[Function]Manager`, utilities are `[Function]Utils` or
`[Function]Helper`, and `Scripts/` is organised into `Managers/`, `Components/`, `Utils/`.

`2-Naming.md` §2 calls all three name smells — a noun that names no responsibility, a class
admitting the class it helps is not doing its job, and a static bag discoverable only by
tribal knowledge.

**I am not calling this "completely outranked", because it is not.** The name-smell argument
is practitioner opinion, not documented Microsoft guidance, and .NET itself ships
`ResourceManager` and `SceneManager`. By Zo's precedence rule IQ wins by default.

**But IQ's own audit is the strongest evidence against its own convention.** The two files
`TechnicalCritiques.md` names as god classes are `WaveManager` and `PlayerActionManager` —
the two largest, at 2,083 and 1,936 lines. The convention predicted the outcome: a class
named after "managing" has no natural boundary to stop growing at.

And this project has **zero** `Manager`, `Helper` or `Utils` classes today. Adopting the
convention would mean renaming `WaveDirector` → `WaveManager`, `SalvageLedger` →
`SalvageManager`, `ShipCommands` → `ShipManager`. Those three names each state a
responsibility; `Manager` would remove that.

**Recommendation: keep this project's responsibility-naming and record IQ's convention as
deliberately not adopted, with the reason above.** If Zo wants uniformity across both
projects, say so and I will invert it — but the rename should go the other way, and that is
IQ's call, not this project's.

## What this pass changed in files 1–4

- `README.md` — precedence section updated; `[pending IQ]` marks resolved; NEVER/ALWAYS card
  and authority levels added.
- `1-Boundaries.md` §4 — the `partial` position keeps its discriminating question. IQ has no
  position on partial classes, so nothing outranked it.
- `2-Naming.md` §1 — casing stands; IQ's conventions match Microsoft's on every point checked
  except the `Manager`/`Utils`/`Helper` fork above.
- `3-Unity-Runtime.md` §6 — reinforced, not changed. IQ's debug pattern is now cited there as
  the worked example of a guard that guards nothing.
- `5-Audit-2026-08-09.md` — unchanged. It is a record.
