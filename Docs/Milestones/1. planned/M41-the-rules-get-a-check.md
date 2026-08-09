# M41 — The rules get a check

<!--
Status: later
Update trigger: when a rule is added to Docs/Architecture/, when a check row is added or retired, or when a baseline count changes
Last updated: 2026-08-09
-->

`Tools/lint-arch/run.sh` reads the rules out of `Docs/Architecture/` and runs from
`Tools/verify/run.sh`. **The tree is not clean today and the check is not allowed to pretend
it is** — every rule either gets fixed here or gets a counted, named baseline that cannot
silently grow.

## Story

As the developer, I want the architecture rules checked on every verify run, so that a
violation is a failed run rather than something someone notices in a review.

## Why it matters

**InfinityQube is the worked example.** A CRITICAL, script-backed file-size rule that ended
with twelve files over and a 2,083-line class, because the script had to be invoked
(`../../Architecture/6-InfinityQube-Precedence.md`). **A rule enforced by a person re-reading
a document holds for about three weeks.**

**This project is already there.** `Architecture/README.md` lists nine *Never do* rules and
eight *Always do* rules. Measured 2026-08-09, the tree violates at least three of them:

| Rule | Live today |
|---|---|
| No `?.` / `??` / `is null` / `ReferenceEquals` on a `UnityEngine.Object` | **23 `Game/` files**, including `HudController.cs:424` `GameController.Instance?.ReturnToShip();` and `SplashController.cs:23` |
| No LINQ in runtime code | `Game/Ui/ShortcutPanelView.cs:3` |
| No `new WaitForSeconds` in a loop body | `Game/Enemies/WaveSpawner.Groups.cs:82`, inside the `while` at line 71 |

**None of these was chosen.** They are the ordinary result of a rule that only exists in
prose. That is the argument for the check, and it is also the trap: a check written to pass
on this tree is a check narrowed until it matches nothing.

**The pattern is already twice-proven here.** M28's `lint-language` and M34's `lint-layout`
both read their rules out of a document and fail `verify`. This is the third, and it is the
one whose rules already have known violations — which is why criterion 4 below is written the
way it is.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** The rule already exists;
this puts the reading of it where the mistake is made.

**Multiplier** (the tooling exception, `../../_meta/_definitions/milestone.md`): every
milestone from here is written against these rules, and `Tools/verify/run.sh` already runs
before every gate.

## Acceptance criteria

1. **`Tools/lint-arch/run.sh` exists, runs from `Tools/verify/run.sh`, and fails that run.**
   Shell and grep, matching `Tools/docgen`, `Tools/verify` and `Tools/lint-language`. **It is
   a third script, not an extension of `lint-layout`** — M34 checks region order within a
   file, this checks the shape of the tree, and merging them makes one script two people have
   to read.
2. **It reads every rule from `Docs/Architecture/`** — one fenced machine-readable block per
   rule, in the file that states the rule, with a line of prose saying the check reads it.
   **A cap, a banned token, or a grandfathered type name changed in a document changes the
   check with no script edit.**
3. **The script contains no literal from any fence.** `grep -E "600|400|System\.Linq|ReferenceEquals|GameController|HatchStation" Tools/lint-arch/`
   returns nothing but variable names. **And deleting a fence makes the check exit 2 with a
   "missing fence" message, not exit 0 and not fall back to a default.** `${CAP:-600}` is the
   specific defeat this guards, and `Tools/lint-language/run.sh` already has the right
   behaviour to copy.
4. **Each row is either fixed here or baselined with a number, and the choice is stated per
   row in the document that holds the rule.** A row is one of:

   - **enforced** — zero violations, any new one fails the run; or
   - **baselined** — it prints `NOTE file:line` and does not fail, **with today's count
     written into the fence** so it cannot grow silently. Exceeding the baseline fails.

   **No allowlist file, no `# lint-arch: ignore` comment, and no pattern narrowed to make a
   count reach zero.** A row reporting zero on a tree where the violation demonstrably exists
   is a failed criterion, not a passing one.

5. **The rows, with today's measured counts as the baseline to reproduce on the first clean
   run:**

   | Row | Source | Today |
   |---|---|---|
   | A file over its folder's cap | `Architecture.md`, M40's unit | set by M40's `Editor/` number; `Sim/` and `Game/` are **0** |
   | `?.` `??` `??=` `is null` `ReferenceEquals` on a `UnityEngine.Object` | `3-Unity-Runtime.md` | **23 files** |
   | `using System.Linq` in runtime code | `3-Unity-Runtime.md` | **1** — `Game/Ui/ShortcutPanelView.cs:3` |
   | `new WaitForSeconds` inside a loop body | `3-Unity-Runtime.md` | **1** — `Game/Enemies/WaveSpawner.Groups.cs:82` |
   | `FindObjectOfType` / `FindObjectsOfType` | deprecated in Unity 6 | **0** — enforced |
   | An empty `Update` / `Start` / `LateUpdate` / `FixedUpdate` / `OnGUI` body | `3-Unity-Runtime.md` | **0** — enforced |
   | A `static` MonoBehaviour singleton not on M40's named list | `Architecture/README.md` | **1** — `DecoyBay.Instance` |

   **The singleton row matches type names from M40's fence, never a count.** A count-based
   check passes forever by renaming.

6. **The counter/rollup row is advisory and inventory-based, or it is not shipped.**
   `../../Decisions.md` bans a specific rejected design — the `RunStats` aggregation layer —
   not the word "stats". `Sim/` legitimately holds `MetaState.cs:21` `TotalRuns`,
   `SaveData.cs:36`, and `Sim/Waves/GatekeeperStats.cs`, which `Architecture.md`'s own tree
   diagram lists as part of the intended shape. **The row prints those three as expected and
   fails only on a fourth**; if that cannot be made to work, the row is removed rather than
   narrowed to `grep -w RunStats`, which would seed and catch perfectly while detecting
   nothing.
7. **Every row is proved twice**: it reproduces its baseline count on the tree as it stands,
   **and** it catches a seeded violation. **Seeding alone is not proof** — a gutted row seeds
   and catches fine. The baseline is what proves it still sees the real tree.
8. **`Tools/verify/run.sh` passes**, with the same test counts as M39 leaves —
   **266 in the harness, 282 in Unity** — and the new check running before the compile, next
   to the existing `lint-language` call.
9. **No file under `Assets/Scripts` is edited to satisfy a check.** If a row's honest state is
   ugly, it is baselined and the fix is a separate milestone. **The one exception is any row
   this milestone chooses to enforce at zero**, which is then listed here with its file list
   before the work starts, not after.

## Implementation

**Create** `Tools/lint-arch/run.sh`.
**Change** `Tools/verify/run.sh` — call it beside `lint-language`, same failure shape.

**Change** `Docs/Architecture.md` — the fenced block for the caps.
**Change** `Docs/Architecture/3-Unity-Runtime.md` — the fenced block for the banned
constructs and the per-row enforced/baselined decision.
**Change** `Docs/Architecture/README.md` — the fenced block for the named singletons
(written by M40; this milestone only marks it as machine-read).

Rejected: a Roslyn analyzer. Correct in principle and it would run in the IDE, but it is a
second toolchain and a package dependency for rules a shell script covers.

Rejected: fixing the 23 `?.`-on-`UnityEngine.Object` sites here. That is a real defect class —
Unity's fake-null lifetime check means `Instance?.Method()` calls into a destroyed object —
and it is 23 files of behaviour-changing edits. **Baseline it, then give it a milestone
that says what it is.**

Rejected: merging into `lint-layout`. See criterion 1.

## Supporting work

Developer infrastructure, so the three questions are answered for the developer.

- **Driven** — `Tools/verify/run.sh`, which already runs before every gate.
- **Shown** — a failing verify run naming file, line and rule; `NOTE` lines for baselined rows.
- **Reads back** — the exit code, one line per violation, and the baseline counts.

## Verification

1. Criterion 3, first half — the grep across `Tools/lint-arch/`.
2. Criterion 3, second half — **delete one fence and run; expect exit 2 and "missing fence".**
   Then change one value inside a fence, touching no script, and confirm the next run reflects
   it. Both halves, per fence.
3. Criteria 4, 5, 7 — run the check on the untouched tree and confirm every baseline count
   matches the table above **exactly**. Then seed one violation per row and confirm each is
   named with `file:line` and that the baselined rows fail on the increment.
4. Criterion 6 — the counter row prints exactly the three known matches and nothing else.
5. Criterion 8 — `Tools/verify/run.sh` reports 266; Unity reports 282; same names.
6. Criterion 9 — `git diff --stat` shows `Tools/` and `Docs/` only, unless a row was listed as
   enforced-at-zero in advance.

## Watch

- **The failure mode of this milestone is a green check that sees nothing.** Every criterion
  above exists because of that one outcome. If a row is hard, baseline it loudly; do not tune
  the pattern until the count is zero. **M35 failed by meeting criteria that a grey box could
  meet, and a lint script narrowed to zero is the same failure in shell.**
- **The baseline counts are stamped 2026-08-09 and will drift** as M35 and M39 land.
  Re-measure before starting and update the fence in the same commit; a stale baseline that
  fails the first run reads as the check being broken.
- The counter row in criterion 6 is a **heuristic** and says so in its own output, per the
  rule M28 set for its own fuzzy check.
- **`WaveSpawner.Groups.cs:82` may be a false positive worth keeping.** It allocates a
  `WaitForSeconds` per spawn interval inside a coroutine — real garbage, but on a path that
  runs a few times a second, not per frame. Baseline it and let the number argue.

## Depends on / Unblocks

**Depends on M40**, which decides the rules this check reads. **Depends on M39** only for
criterion 8's harness count.

**Unblocks nothing formally**, and makes every later milestone's architecture claims
verifiable rather than asserted.
