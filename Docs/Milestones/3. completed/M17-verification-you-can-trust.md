# M17 — Verification you can trust

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-05
-->

Make a test run say why it failed, and stop it eating the developer's campaign.

## Done when

**Met.** Failed verification writes a non-empty `message`; compile failure is
`stage:"precheck"` with no tests run; PlayMode leaves `gatekeeper_meta.json`
byte-identical; an orphaned `.playtestbackup` is restored on the next suite with the
fixture log line. AutoVerify EditMode **203 / 0**, PlayMode **10 / 0**
(`gkb_verify_report.json`, 2026-08-06T01:09:25Z). Precheck receipt:
`status:"error","stage":"precheck"` at 2026-08-05T20:08:48 (local), message *Scripts
failed to compile… no tests were run.* Orphan receipt:
`Logs/GatekeeperBastion_2026-08-05_20-09-20.log` —
`[PlayModeSaveFixture] restored a save backup left by a crashed test run.`

## What shipped

- `PlayModeSaveFixture` — assembly-level `[SetUpFixture]` (global namespace) owns
  `SaveFileGuard`; recovers orphaned `.playtestbackup` before guarding; logs guard /
  restore / orphan recovery.
- `PlayTestUtil.SaveFileGuard` — nest-safe: if `.playtestbackup` already exists, a
  nested guard is a no-op (per-test guards removed from Bastion / Session suites).
- `AutoVerify` — `WriteReport` always emits `message`; error statuses refuse an empty
  message; `.writefail` fallback if the report path is unwritable; precheck on
  `EditorUtility.scriptCompilationFailed` and missing test assemblies; `OnError`
  first-writer-wins; PlayMode kick retries after EditMode teardown; stuck idle is
  180s from last progress (`RunStarted` / `TestStarted` heartbeat).

## What it did not settle

Historical campaign pollution from pre-fixture PlayMode runs is not repaired (see
Watch). Duplicate `TestRunnerApi` callback *registration* across domain reloads is
left alone — the handler is idempotent.

## Story

As the developer, I want a failed verification run to tell me what broke and to leave my
save untouched, so that the tool I check my work with is not itself a source of damage
and confusion.

## Why it matters

**The file bridge transmits nothing on failure.** `AutoVerify.OnError(string message)`
logged the reason to the Unity console and then called
`WriteReport("error", "failed", null, null)` — a method with **no message parameter**.
The report on disk read `{"status":"error","stage":"failed","editMode":null,
"playMode":null}`. The entire point of the bridge is reading results without Unity open,
and on the most common failure it emitted "something went wrong" with the cause
discarded. It has produced one usable report in the project's life; every real error this
week was found with the mcs harness instead.

**The most common failure is invisible by construction.** A test assembly that does not
build surfaces as Unity's generic *"An unexpected error happened while running tests"* —
indistinguishable from a crashed runner, a play-mode collision, or a stuck stage. Three
report shapes, all `"error"`, none with a reason.

**And the save guard was dead code.** `PlayTestUtil.SaveFileGuard` correctly backs up
`SaveService.SavePath` and restores on `Dispose` — and **nothing ever constructed it.**
Two references in the whole test tree, both the declaration. So every Play-mode run wrote
into the live `gatekeeper_meta.json`. Measured across three sessions with no play between
them:

```
11:47   3433 banked, 18 run(s), 14 death(s)
11:55   3545 banked, 19 run(s), 15 death(s)
12:11   3657 banked, 20 run(s), 16 death(s)
```

**+112 Salvage, +1 run and +1 death per test execution**, into the campaign the balance
milestones read their numbers from.

This is infrastructure, not a feature, and qualifies under the tooling exception in
`../../_meta/_definitions/milestone.md`: it lowers the cost of verifying M14, M15, M16
and everything after. **It was implemented before this file was written, which is the
rule that exception exists to enforce — the code landed with no criteria and nothing
proving it works. The criteria below are unmet until someone runs them.**

## Acceptance criteria

1. Starting verification while a test assembly is not loaded writes
   `status:"error", stage:"precheck"` with a `message` naming the assembly, and **runs no
   tests**.
2. **Every** error path writes a non-empty `message` field — precheck, TestRunner
   `OnError`, stuck stage, and report-write failure.
3. A complete Play-mode run leaves `gatekeeper_meta.json` **byte-identical**: banked
   Salvage, run count and death count unchanged.
4. A Run killed mid-flight leaves a `.playtestbackup`, and the **next** run restores it
   during setup and logs that it did.
5. Duplicate `OnError` callbacks — which accumulate across domain reloads — write the
   report **once**; the first one owns it.
6. The EditMode and PlayMode result blocks still appear in the report on a green run.

## Implementation

*Shipped 2026-08-04, ahead of this file; closed 2026-08-05 after criteria runs.*

**Create** `Assets/Tests/PlayMode/PlayModeSaveFixture.cs` — an NUnit `[SetUpFixture]`
**deliberately outside any namespace**, so it wraps every test in the
`GatekeeperBastion.PlayTests` assembly rather than one namespace. `[OneTimeSetUp]`
constructs `PlayTestUtil.SaveFileGuard`; `[OneTimeTearDown]` disposes it.

Assembly-level rather than per-test on purpose: **opt-in isolation is isolation that gets
forgotten**, which is precisely how the guard sat unused since it was written.

Setup also **recovers an orphaned backup first**. A Run that dies mid-flight never reaches
teardown, so the backup survives and the live save keeps whatever the tests left in it.
Restoring a stale backup on the next setup is what makes the guard crash-safe rather than
merely correct — criterion 4.

**Change** `Assets/Tests/PlayMode/PlayModeTestUtil.cs` — `SaveFileGuard.ResolveSavePath`
becomes `internal`, exposed through `PlayTestUtil.ResolveSavePathForFixture()` so the
fixture can find the save before any guard exists. Nested guards no-op when
`.playtestbackup` already exists.

**Change** `Assets/Scripts/Editor/Tools/AutoVerify.cs` — `WriteReport` `message` field;
precheck via `scriptCompilationFailed` and missing assemblies; `OnError` first-writer-wins;
PlayMode kick retries after EditMode; stuck timer heartbeats on progress.

**Not done, deliberately:** the duplicate callback *registration* is untouched.
`[InitializeOnLoad]` re-registers on every domain reload and the old registration is not
removed. Making the handler idempotent fixes the harm; chasing the registration means
fighting `TestRunnerApi` lifetime across reloads for a cosmetic gain.

## Verification

1. Criteria 1–2 — intentional compile break in EditMode tests; sentinel →
   `stage:"precheck"`, message *Scripts failed to compile… no tests were run*
   (2026-08-05T20:08:48 local). Stuck-path message earlier the same day:
   *TestRunner never finished stage 'playmode'…*.
2. Criterion 3 — SHA256 of `gatekeeper_meta.json` identical before/after green suite
   (`A5F8FA…`); 425 banked / 5 runs / 5 deaths unchanged.
3. Criterion 4 — planted `.playtestbackup` with live polluted to 99999; next PlayMode
   setup logged `restored a save backup left by a crashed test run`
   (`Logs/GatekeeperBastion_2026-08-05_20-09-20.log`); save restored to 425.
4. Criteria 5–6 — green report `complete` with EditMode 203 and PlayMode 10
   (`gkb_verify_report.json`, 2026-08-06T01:09:25Z). `OnError` returns when
   `StageKey` is already clear.

## Watch

- **The campaign is already polluted.** `TotalRuns` and `PodEjections` count test
  executions, and banked Salvage includes roughly 112 per past run. Nothing here repairs
  history — M16 reads Hatch and kill data from logs rather than from `MetaState`, so it
  is unaffected, but any future claim about run counts is not.
- `SaveFileGuard.Dispose` reloads the session by reflection on `GameSession.ContinueGame`.
  If that method is renamed the restore silently becomes a file-only restore and the live
  session keeps the test's state until the next scene load.

## Depends on / Unblocks

Depends on nothing. Unblocks honest verification of M14, M15 and M16 — all three gate on
runs that cannot currently be told apart from a broken runner.
