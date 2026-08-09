# 7 — File layout

<!--
Purpose: The region order every C# file in Assets/Scripts follows, and the rules that make it mechanically checkable rather than aspirational.
Status: working
Authority: MANDATORY — the order is fixed; which regions appear is not.
Update trigger: when a region is added to or removed from either order, or when the checker's rules change.
Last updated: 2026-08-09
-->

Adopted from InfinityQube's `03-file-standards.md` (`6-InfinityQube-Precedence.md`).
**M34 applies it to the existing tree and adds the check.** Until M34 ships, this governs new
files only.

## The two orders

**The order is mandatory. Presence is not** — a region with nothing in it is not written, and
an empty `#region` is a defect the checker rejects.

### Unity-facing types — `Game/`, `Editor/`, `Util/`

| # | Region | Holds |
|---|---|---|
| 1 | `Constants` | `const`, `static readonly` |
| 2 | `Inspector` | `[SerializeField]` and serialized public fields, with their `[Header]`s |
| 3 | `References` | cached component and service references resolved in `Awake` |
| 4 | `State` | private runtime fields |
| 5 | `Properties` | properties, public and private |
| 6 | `Unity Lifecycle` | `Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`, `OnDisable`, `OnDestroy`, `OnGUI`, `OnValidate`, `OnDrawGizmos` |
| 7 | `Public API` | everything callable from outside, commands first |
| 8 | `Private` | internal implementation |
| 9 | `Events` | handlers subscribed to something else |
| 10 | `Nested Types` | nested `struct`, `class`, `enum` |

InfinityQube's eleventh region is `Debug`, holding an `enableDebugLogs` bool and a per-class
`DebugLog` helper. **Not adopted** — this project logs through `Util/Log`, and IQ's pattern
guards nothing the caller pays for (`3-Unity-Runtime.md` §6).

### `Sim/` — pure C#, no MonoBehaviour

| # | Region | Holds |
|---|---|---|
| 1 | `Constants` | tuning constants and their `<summary>` |
| 2 | `State` | fields |
| 3 | `Properties` | |
| 4 | `Public API` | the surface the Game layer codes against |
| 5 | `Private` | |
| 6 | `Nested Types` | |

No `Inspector`, no `References`, no `Unity Lifecycle` — a `Sim/` file containing any of those
is a seam violation and `1-Boundaries.md` §1 is the rule it breaks.

## Rules

**Regions do not count toward the line cap.** Sim ≤ 600, Game ≤ 400
(`Docs/Architecture.md`), measured as non-blank, non-comment, non-directive lines.

**A partial class regions each part independently.** `GameController.DevCommands.cs` gets
its own `Public API` and `Private`; it does not continue the ones in `GameController.cs`.

**A file whose class body contains a preprocessor directive is exempt from reordering** and
carries a one-line comment saying so. `DevConsole.cs` is wrapped entirely in
`#if UNITY_EDITOR || DEVELOPMENT_BUILD`; moving a member across a directive boundary changes
what compiles. Regions may still wrap what is there.

**Do not reorder fields whose initializer references another field in the same type.** C#
runs field initializers in textual order, so a reorder is a behaviour change. The checker
flags these rather than moving them.

**A type with fewer than three members needs no regions.** Enums, `WeaponId`, `WeaponSlot`,
`SpawnPoint` and similar are exempt; ceremony costs more than it returns there.

## Why this is worth the sweep

Not for beauty. Two reasons, both mechanical:

**It makes a member's category a stated fact rather than a guess.** A field appearing under
`References` is a cached lookup and belongs in `Awake`; the same field under `State` is
runtime data. Today both are "a private field near the top", and `1-Boundaries.md`'s wiring
rules cannot be checked against that.

**It gives the size cap somewhere to point.** `Docs/Architecture.md` says split at 400 lines
and `PlayerGun.cs` is 471. "Split it" is an unanswerable instruction; "extract the `Firing`
region" is an answerable one. InfinityQube's caps failed precisely because the rule said what
the number was and never said where the seam went — twelve files over, and the largest at
2,083 lines against 600 (`6-InfinityQube-Precedence.md`).

## The check, not the convention

**A rule enforced by a person re-reading this file is a rule that will hold for about three
weeks.** M34's real deliverable is `Tools/lint-layout/run.sh` wired into
`Tools/verify/run.sh`, alongside M28's language check. That is the InfinityQube lesson
applied: their file-size rule was CRITICAL, script-backed, and ended at 247% over — because
the script had to be invoked, and a verification step that has to be remembered is a
verification step that is skipped.
