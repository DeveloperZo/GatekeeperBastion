# Architecture

<!--
Purpose: Governs how code in this project is structured, named and written. Rules only — the audit that produced them is in 5-Audit-2026-08-09.md, and the reasoning behind any single rule is in the file that states it.
Status: working
Authority: MASTER REFERENCE for how code is written. Docs/Architecture.md remains the reference for what this codebase is.
Update trigger: when a rule is added, changed or retired, or when a new audit finds a rule nobody follows.
Last updated: 2026-08-30 (ShipLookPass rows under GatekeepersProtocol/; HatchWaves const; Ante.Enabled static readonly; _Scratch tracked-empty)
-->

## Never do

- **Put a `MonoBehaviour`, a `GameObject`, a scene lookup, `Time`, ambient `Random`,
  `SerializeField`, `UnityEditor` or static mutable state in `Sim/`.** That is the one
  structural rule this project has; CoreModule value types (`Vector3`, `Mathf`) are fine.
  The rule, the reason (testability and one-directional dependency, not engine purity) and
  the enforcement are in `1-Boundaries.md` section 1.
- **Add a static MonoBehaviour singleton.** One is grandfathered by type —
  `HatchStation`, locator only. `GameController.Instance` was retired 2026-08-12
  (the scene host now owns a `RunContext` and passes or serializes itself to every
  consumer); `DecoyBay`'s static was retired 2026-08-11. There is no second named one, and
  one unnamed one the check cannot see — `RealmShareBeacon`, below.
- **Add a counter, a rollup or a stats object** (`../Decisions.md`). Emit a log line.
- **Use `?.`, `??`, `??=`, `is null` or `ReferenceEquals` on a `UnityEngine.Object.`**
  Only `== null`, `!= null`, `if (x)` respect Unity's lifetime check.
- **Mutate Run state from anywhere but a `GameController` command method.** Meta / save
  mutation is `ShipCommands` only.
- **Assume Build All rebuilds the Ship.** It does not. `RebuildShipGeometry` is retired
  (2026-08-26) and `BuildAllCore` does not call it; the hull is
  `Tools/ship-hull/build_hull.py` (`../Architecture.md` gotcha 4, `8-Scene-Authoring.md`).
  On Bastion, kit segments under `Environment` skip the geometry wipe; Build All rebakes
  NavMesh. Stations, hosts and UI are authored and survive.
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

Measured 2026-08-12 and re-measured 2026-08-30 (`rg -n "static\s+\w+\s+Instance"
Assets/Scripts` for the declarations): exactly one `static Instance` declaration exists, and
it is still `HatchStation`'s.

- **`HatchStation`** — grandfathered, **locator only** (a transform / pathing target;
  it holds no Run or player state — its hatch-subsystem access goes through its
  serialized `_game` reference). Declared `Game/Bastion/HatchStation.cs`.
- **`GameController`** — **debt paid 2026-08-12.** The scene host constructs one
  `RunContext` (meta, sim, decoy state, spawn ids) and hands itself out explicitly:
  serialized references for scene-stable consumers (HUD, spawner, hatch, repair),
  `Bind(this)` pushes for the runtime-resolved player rig (gun, ability, health,
  bay), a `Configure(stats, game, spawner)` pass for spawned gatekeepers, and a
  dev-only lazy lookup in `DevConsole`. Nothing singular remains that co-op would
  have to make plural.
- **`DecoyBay`** — debt paid 2026-08-11; its former call sites now reach the bay
  through their own `GameController` reference (`.Decoys`). The record stays so the
  payment is not rediscovered as a mystery.

Scene REGISTRIES are a different shape and stay: `PlayerRegistry` (live players,
self-registered OnEnable/OnDisable — the sanctioned "the player" lookup) and
`SpawnPoint.All` (scene spawn markers; `SceneBuilder` rebuilds the points and wires
nothing by contract). They hold lists of scene objects, not Run state; one Unity
process can only ever host one Game-layer world (the NavMesh statics make two
impossible), so scene-scoped and Run-scoped coincide at this layer. The Sim layer
is where two Runs coexist, and it is required to hold no statics at all — enforced by
`TwoRunIsolationTests.SimAssembly_HoldsNoStaticMutableState`. HatchWaves is `const`; Ante is
`static readonly` plus a per-run `RunSim.AnteEnabled` tests set.

A fourth pattern existed when M40 was written (2026-08-09): `StationPanelView.EnsureInstance()`,
a static factory — the same pattern under another name. It has since been removed
(confirmed absent 2026-08-11), so its absence is expected, not a mystery to rediscover.

**`RealmShareBeacon` is the shape without the name, and it is unruled.**
`Game/Flow/RealmShareBeacon.cs:18` holds `static RealmShareBeacon _instance`, lazily
created, `DontDestroyOnLoad`'d, guarded by `Ensure()` / `Retire()`. The fence below greps
for a member named `Instance`, so it does not see this and the check passes.
`5-Audit-2026-08-25.md` raised it: milder than a service locator, since nothing reads
state through it from outside, so grandfather-and-document may be the right answer rather
than a refactor. Still present 2026-08-30 at the same field. Until Zo rules, it is
neither allowed by name nor flagged by the check.

`Tools/lint-arch/run.sh` reads the fenced block below; a `static Instance` declaration
on any type not named in it fails the run. The owed list has been empty since
2026-08-11 — an entry there would print as a baselined note rather than a pass.

```arch-lint
singleton_allow=HatchStation
singleton_owed=
menu_root=GatekeepersProtocol
menu_rows=Record/Gameplay/This scene (Insert to stop),Record/Gameplay/Ship,Record/Gameplay/Bastion,Record/Ship Flythrough,Logs/Open Logs Folder,Logs/Copy Latest Log Path,Build/All Scenes,Build/Place Bastion Obstacles,Build/Group Bastion Geom In Open Scene,Build/Unpack Bastion Geom In Open Scene,Build/Copy Compendium Pages,Build/Range Scene,Build/Protocol Bar,Build/Ship Look Pass,Bake/Bastion NavMesh,Bake/Ship Look Lighting,Validate/Integration Check,Validate/Scene Bindings
```

**One menu root, five folders, and every row is named above.** Folders are Record, Logs,
Build, Bake, Validate — catalog and priority bands in `../Working-Rules.md`. A `[MenuItem]`
under any other root fails the run; a row not in `menu_rows` fails it too; a fence row
missing from the code fails it too. Paths must be string literals (`MenuRoot + "…"` is
invisible to the check). **Adding a row requires permission before it is written**, names
the folder and the row it replaces, and updates this fence in the same change — a menu
entry is a shared, permanent surface that outlives the reason it was added.

`ShipLookPass` sat under `Tools/Scenes/Ship/` until 2026-08-30. Those two rows are now
`Build/Ship Look Pass` and `Bake/Ship Look Lighting`. Do not put them back under `Tools/`.

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
| `5-Audit-2026-08-10.md` | record | Supersedes the 08-09 pass: six lenses, re-derived, 28 surviving findings |
| `5-Audit-2026-08-25.md` | record | Five lenses against AGENTS.md §4 and `gkb-development`; superseded as the newest pass by 08-30 |
| `5-Audit-2026-08-30.md` | record | The newest pass, and the one to read first: five lenses post-0.34.2, four findings plus two decisions owed |
| `6-InfinityQube-Precedence.md` | record | What was taken from InfinityQube, what was rejected, and on what evidence |
| `7-File-Layout.md` | MANDATORY | The region order every file follows, and why the check matters more than the convention |
| `8-Scene-Authoring.md` | MANDATORY | Scene roots, what an agent may edit, and what a scene edit must assert |
| `asset-audit-2026-08-18.md` | record | Dated snapshot of what art is on disk against what is referenced; figures drift after its stamp, by its own declaration |

**CRITICAL** means a violation breaks a load-bearing property — the Sim dependency boundary, or
correctness against the engine. MANDATORY means it is required and a deviation needs a reason
in writing. A record is history: corrections only.

`Docs/Architecture.md` describes **this** codebase — its seam, its map, its gotchas.
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

Last audited 2026-08-30 (`5-Audit-2026-08-30.md` is the newest record). File counts
across `Assets/Scripts`: **306** `.cs` files (78 in `Sim/`, 200 in `Game/`, 27 in
`Editor/`, 1 in `Util/`). The 08-09, 08-10 and 08-25 passes stay as the record of
what the rules were written against.

| | Finding |
|---|---|
| **Already right** | Re-checked 2026-08-30. No LINQ in runtime code — `using System.Linq` does not appear once. No empty `Update`/`Start` bodies (fence pattern). No `FindObjectOfType`. No `Manager`/`Helper`/`Utils` class anywhere. No `?.` / `??` in `Game/`; `is null` only in comments. `Log.Info` already `[Conditional]`. `partial` discipline holds — 15 classes, 72 files, including `HudController.Protocol.cs` and `SceneBuilder.ProtocolCards.cs`. |
| **The structural gap** | Was: `Sim/` pure by convention, unenforced, sharing an assembly with `Game/`. **Closed 2026-08-12 (M45)** by `GatekeepersProtocol.Sim.asmdef` plus `TwoRunIsolationTests.SimAssembly_HoldsNoStaticMutableState`. The two remaining static mutables closed 2026-08-30: `MasterControl.HatchWaves` is `const int = 1`; `Ante.Enabled` is `static readonly bool = false` and spend-path tests set `RunSim.AnteEnabled`. `1-Boundaries.md` §1. |
| **The measured cost** | `Log.Event` and `Log.Metric` are still not `[Conditional]` and ship in release. **478** call sites between them as of 2026-08-30 (477 Event, 1 Metric), **90** of them containing an interpolated string (`$"`), which under C# 9 allocates and boxes whether or not anything reads the log. `3-Unity-Runtime.md` §6. |
| **The name that started this** | `RegisterPinHitIfNeeded` was the only `*IfNeeded` when the audit found it, and it is still the one place an audit found a shipped bug the name predicted. Re-rg 2026-08-30: still four names — `RegisterPinHitIfNeeded`, `BindGunIfNeeded`, `BindAvatarIfNeeded`, `ApplyAudioIfNeeded`. `2-Naming.md` §2. |
| **The rules with no fence** | Where enforcement is habit rather than a compile-time boundary. Tracked `Assets/_Scratch/` is `.gitkeep` only (08-25's 25 committed scripts are gone). This working tree still holds **13** gitignored Explore scripts there; `Tools/verify/run.sh` fails while they sit. HUD `Prompt` getters still allocate on some branches: `RepairInteractable` Dock/Docking concatenates; `StationTerminal` sealed-while-boarding concatenates; `BastionTerminal` HatchBank/Resource concatenate; `HatchRecallPad` concatenates when available; `HatchPortal` uses two literals. `Reset()` for serialized tunables now exists on the four 08-25 misses (`EnemyGatekeeper`, `PlayerGun`, `Damageable`, `FirstPersonController`) plus `PixelRender`; remaining tunable MonoBehaviours without `Reset()`: unknown — not a full pass. |
| **Convention, no check** | InfinityQube's region order is adopted (`7-File-Layout.md`) and governs new files. **M34 still cancelled** (`Milestones/5. cancelled/M34-a-file-you-can-navigate.md`) — no tree-wide sweep, no `Tools/lint-layout` in verify. |
| **Open for Zo** | Whether to adopt InfinityQube's `Manager`/`Utils`/`Helper` class naming (`6-InfinityQube-Precedence.md`), and how `RealmShareBeacon` is ruled (above). |
