# Architecture

<!--
Purpose: Governs how code in this project is structured, named and written. Rules only — the audit that produced them is in 5-Audit-2026-08-09.md, and the reasoning behind any single rule is in the file that states it.
Status: working
Authority: MASTER REFERENCE for how code is written. Docs/Architecture.md remains the reference for what this codebase is.
Update trigger: when a rule is added, changed or retired, or when a new audit finds a rule nobody follows.
Last updated: 2026-08-11
-->

## Never do

- **Put `using UnityEngine` in `Sim/`.** That is the multiplayer seam and it is the one
  structural rule this project has.
- **Add a static MonoBehaviour singleton.** Two are grandfathered by type —
  `GameController` and `HatchStation`. `DecoyBay`'s static was owed, not
  grandfathered, and has been retired (see *The singletons, named*). There is
  no third.
- **Add a counter, a rollup or a stats object** (`../Decisions.md`). Emit a log line.
- **Use `?.`, `??`, `??=`, `is null` or `ReferenceEquals` on a `UnityEngine.Object.`**
  Only `== null`, `!= null`, `if (x)` respect Unity's lifetime check.
- **Mutate Run or Meta state from anywhere but a `GameController` command method.**
- **Hand-edit a generated scene.** `SceneBuilder` writes them; your edit is lost on rebuild.
- **Allocate in a per-frame path** — no LINQ, no `params`, no capturing closure, no
  interpolated string, no `foreach` over an interface.
- **Commit a partial method body or an incomplete control structure.** See *Complete unit*.
- **Write a conclusion into a code comment.** Contracts, invariants, units and traps only.

## Always do

- **Cache `GetComponent` in `Awake`**; use `TryGetComponent` when the component may be absent.
- **Inject dt.** `Sim/` never reads `Time`.
- **Say why a command was refused**, in the log at minimum. Silence is a defect.
- **Name the responsibility**, not the act of managing it.
- **Mark unfinished work `// POC:`** with what is unfinished and why.
- **Put a member in its region** (`7-File-Layout.md`). The order is fixed; presence is not.
- **Run `Tools/verify/run.sh`, then the EditMode mirror.** Path 1 alone does not close a gate.
- **Update the tool in the same commit as the log line it parses.**

## The singletons, named

Measured 2026-08-11 (`rg -n "static\s+\w+\s+Instance" Assets/Scripts/Game` for the
declarations, `rg -n "<Type>\.Instance" Assets/Scripts` for the call sites): exactly
two `static Instance` declarations exist.

- **`GameController`** — grandfathered. Declared `Game/Bastion/GameController.cs:41`;
  41 call sites (re-measured 2026-08-11 after the DecoyBay retirement routed 5 more through it). It is the command surface every sim mutation already routes through.
- **`HatchStation`** — grandfathered. Declared `Game/Bastion/HatchStation.cs:22`;
  3 code call sites (`DevConsole.cs:362`, `EnemyGatekeeper.Targeting.cs:95`,
  `PlayerAbility.cs:217`), plus the melee-range convention in `Architecture.md`
  gotcha 2.
- **`DecoyBay`** — **debt paid.** The static was owed, not grandfathered: it was
  declared in `Game/Bastion/DecoyBay.cs` with three call sites (two in the
  HudController weapon readout, one in `EnemyGatekeeper.Targeting`'s retarget tick).
  Retired 2026-08-11: the bay now registers itself with `GameController` on enable
  (and unregisters on disable), and every former call site reaches it through
  `GameController.Instance.Decoys` — the command surface everything else already
  routes through. The record stays so the debt's payment is not rediscovered as a
  mystery.

A fourth pattern existed when M40 was written (2026-08-09): `StationPanelView.EnsureInstance()`,
a static factory — the same pattern under another name. It has since been removed
(confirmed absent 2026-08-11), so its absence is expected, not a mystery to rediscover.

`Tools/lint-arch/run.sh` reads the fenced block below; a `static Instance` declaration
on any type not named in it fails the run. The owed list has been empty since
2026-08-11 — an entry there would print as a baselined note rather than a pass.

```arch-lint
singleton_allow=GameController,HatchStation
singleton_owed=
menu_root=GatekeeperBastion
menu_rows=Build All Scenes,Run Verification Now,Validate Scene Bindings,Logs/Open Logs Folder,Logs/Copy Latest Log Path
```

**One menu root, and every row is named above.** A `[MenuItem]` under any other root fails the
run; a row not in `menu_rows` fails it too. **Adding a row requires permission before it is
written**, and the ask names the row it replaces — a menu entry is a shared, permanent surface
that outlives the reason it was added.

`Gatekeeper/HUD/` existed until 2026-08-11. It was created by two one-shot HUD migration tools
(`PullBastionHudToShip.cs`, `RestoreShiftArmorHealth.cs`, ~220 lines each) that should never
have been in the tracked tree. **The sprawl arrives as a menu and starts as a script nobody
deleted**, which is why this list is checked rather than trusted.

## The files

| File | Authority | Covers |
|---|---|---|
| `1-Boundaries.md` | **CRITICAL** | Assemblies, the Sim seam, dependency direction, wiring, composition, `partial` |
| `2-Naming.md` | MANDATORY | Naming, method-name diagnostics, CQS, least astonishment, comments |
| `3-Unity-Runtime.md` | **CRITICAL** | Measured Unity 6 idioms — fake null, allocation, lookup, update loops, physics, logging |
| `4-Testing-and-Determinism.md` | MANDATORY | EditMode/PlayMode split, the offline harness, determinism, what not to test |
| `5-Audit-2026-08-09.md` | record | What this codebase does against the rules above, with `file:line` |
| `6-InfinityQube-Precedence.md` | record | What was taken from InfinityQube, what was rejected, and on what evidence |
| `7-File-Layout.md` | MANDATORY | The region order every file follows, and why the check matters more than the convention |

**CRITICAL** means a violation breaks a load-bearing property — the multiplayer seam, or
correctness against the engine. MANDATORY means it is required and a deviation needs a reason
in writing. A record is history: corrections only.

`Docs/Architecture.md` describes **this** codebase — its seam, its map, its four gotchas.
This folder describes **how code should be written** in it. If the two contradict each other,
`Architecture.md` wins on facts about the build and this folder wins on rules about the craft,
and whichever is wrong gets fixed in the same commit.

## Precedence

Three sources. **A lower source never overrides a higher one; it fills the gaps.**

1. **InfinityQube's `Assets/Docs/AI Agent`.** Zo's prior art. Outranks everything below on
   convention. **Read 2026-08-09; outcomes in `6-InfinityQube-Precedence.md`.**
2. **Documented, measured sources.** Unity's Manual and e-books, Microsoft's .NET guidelines,
   NUnit and UTF docs, named practitioner work with numbers behind it. Only outranked by
   evidence — a convention cannot make `Debug.Log` free or make `?.` respect Unity's
   lifetime check. Two InfinityQube rules were rejected on this basis and one is stale
   pre-Unity-6 guidance rather than a disagreement.
3. **Judgement.** Marked as such where it appears.

## Two rules from InfinityQube that apply everywhere

**Complete unit.** A modification is a whole method from signature to closing brace, a whole
property with its accessors, or a cohesive cluster of them. Never a partial body, never an
incomplete control structure, never a single line inside a complex method.

**POC has no upgrade requirement.** Code marked `// POC: <what is unfinished and why>` is
allowed to be unoptimised and is not debt to be paid down on principle — if it works, it
works. It may violate optimisation rules. It may not violate safety or correctness rules.
This is how gray-box code stays honest instead of pretending to be finished.

## How a rule earns a place here

- **State the rule, the reason, the source, and the case where following it is wrong.** A
  rule with no stated counter-case has not been thought about.
- **Cite, don't assert.** Unity performance folklore is thick with advice that modern Unity
  invalidated. `3-Unity-Runtime.md` §7 lists nine such claims, three of which this project
  could plausibly have adopted.
- **Prefer a check to a paragraph — and prefer a check that cannot be skipped.**
  InfinityQube's file-size rule was CRITICAL, script-enforced, and ended with a 2,083-line
  file against a 600-line limit. A rule enforced by a script someone has to run documents
  violations rather than preventing them (`6-InfinityQube-Precedence.md`).
- **Say what is not a rule.** Update managers, DI containers and ScriptableObject event
  channels are rejected by name in `1-Boundaries.md` §5 so they are not re-proposed.

## Where this stands today

Audited 2026-08-09 across `Assets/Scripts` (68 files). Detail in `5-Audit`.

| | Finding |
|---|---|
| **Already right** | No LINQ in runtime code. No empty `Update`/`Start` bodies. No `FindObjectOfType` — already on the Unity 6 forms. No `Manager`/`Helper`/`Utils` class anywhere. No fake-null bypass on any `UnityEngine.Object`. `Log.Info` already `[Conditional]`. 8 `Update()` methods total. |
| **The structural gap** | `Sim/` is pure — zero `UnityEngine`, zero `Vector3`/`Mathf`/`Random` — and nothing enforces it, because it shares an assembly with `Game/`. The usual cost of fixing that is not payable here. `1-Boundaries.md` §1. |
| **The measured cost** | `Log.Event` and `Log.Metric` are not `[Conditional]` and ship in release; 61 of 147 call sites interpolate, which under C# 9 allocates and boxes whether or not anything reads the log. 47 more interpolations sit inside `OnGUI`. `3-Unity-Runtime.md` §6. |
| **The name that started this** | `RegisterPinHitIfNeeded` is the **only** `*IfNeeded` in the codebase — and it is the one place an audit found a shipped bug. The name predicted the defect. `2-Naming.md` §2. |
| **In flight** | InfinityQube's region order is adopted (`7-File-Layout.md`); **M34** applies it to the existing tree and adds `Tools/lint-layout` to the verify run. Until it ships, the order governs new files only. **M34 is placed at 11 of 10 on Zo's explicit override, 2026-08-09; the cap itself is unchanged.** |
| **Open for Zo** | Whether to adopt InfinityQube's `Manager`/`Utils`/`Helper` class naming. `6-InfinityQube-Precedence.md`. |
