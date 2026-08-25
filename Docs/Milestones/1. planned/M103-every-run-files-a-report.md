# M103 — Every Run files a report

<!--
Status: later (building 2026-08-24)
Update trigger: when the Metric line schema changes, when the harness log location changes, or when a checkpoint configuration's taint flag flips
Last updated: 2026-08-24
-->

The session log becomes a document that answers questions instead of one that must be
mined. Three changes, one milestone because each is worthless without the others: the
test harness stops writing into the play-data corpus, every Run ends with one
machine-readable summary line, and the dashboard reads that line — including which
checkpoint configuration a Run was played on, and whether that configuration is
marked tainted.

## Story

As the developer, I want every logged Run to carry the figures the open balance
questions need, so that opening the dashboard answers "what did the last week of play
tell us" instead of showing noise I have to argue with.

## Why it matters

The dashboard has been uninformative since it shipped, and the causes are measurable
(all figures from `Logs/` on 2026-08-24):

- **The corpus is harness, not play.** 19 of the day's 28 log files are
  PlayMode-verification files (`stage=playmode`), together carrying ~550 synthetic
  "Runs"; the human sessions are the 4–32 KB files carrying 1–5 Runs each. With
  `LogFile.KeepSessions = 30`, a day of verification **evicts every real session** —
  the play-data corpus has a retention of hours. The Session tab's *Include PlayMode*
  default was on, so the page opened onto the noise.
- **The summary channel exists and nothing uses it.** `Util/Log.cs` defines
  `Log.Metric` for "end-of-run summary lines"; it had **zero call sites**. The only
  end lines are `RUN OVER / BASTION STRIPPED / DOCKED … gross, repair, net, banked`
  (`GameController.EndScreen.cs`), so deaths, respawns, seats, dial, checkpoint
  provenance and the loss cause were absent, and `DOCKED` never even closed a Run in
  the dashboard's model. Killer attribution degraded to `killed by unknown` past a
  5 s window (`PlayerHealth.cs`).
- **Checkpoint play was unclassifiable.** Zo plays segments through the F12/F11
  checkpoints by design (`Decisions.md`, no-playthroughs-for-gates), and the M95
  session added loadout configurations on top (`GameController.DevCheckpoints.
  Configs.cs`, rows marked PROVISIONAL pending an owner ruling). The log carried no
  way to tell a checkpoint Run on a representative loadout (fair play data) from one
  on a provisional loadout (not yet), so the only safe reading was to distrust all of
  it.

Leverage tier (`../../Working-Rules.md`): **#6, information flows** — same tier M38
claimed, unfinished. **Multiplier**: every open "needs play data" label in `Design/`
reads this page, and the two dated kill criteria (`M63-3`'s two logged Runs by
2026-08-31, `M102`'s faucet criteria) cannot be adjudicated at all once their Runs
rotate out of `Logs/`.

## Acceptance criteria

1. **A PlayMode verification session writes its log under `Logs/harness/`**, detected
   by the Unity Test Framework's generated `InitTestScene*` first scene — never by the
   `IntegrationCheck`/`AutoVerify` tag (the M38 rule). Human sessions keep writing to
   `Logs/` exactly as today.
2. **Pruning is per class**: `Logs/` keeps 30 files, `Logs/harness/` keeps 6. A day of
   verification deletes no human session file.
3. **Every Run end emits one `[Run] summary schema=1 …` Metric line** — beside the
   three prose end lines (RUN OVER, BASTION STRIPPED, DOCKED), plus a
   `result=abandoned` form when a live Run is torn down without a terminal phase
   (quit to menu, mid-run checkpoint reload, closing the player). Key=value tokens,
   no spaces inside a value, prose lines unchanged beside it.
4. The Metric line carries exactly: `result cause wave startWave duration gross repair
   net banked respawns seats dial checkpoint config configTaint dev scene seed`.
   **Facts the game owns at run end, and nothing summed from events**: kills and
   deaths stay on their per-event lines and the readers do the arithmetic — the
   no-counters-in-Assets decision (`Decisions.md`, cited in `Tools/wave-report/
   Program.cs`), reaffirmed here after this milestone's first cut proposed sim-side
   tallies and was wrong. Loadout stays on the `run begins` fields. Adding a field is
   compatible; renaming or removing one is `schema=2`.
5. **Checkpoint provenance and config taint are explicit, and taint is
   beat-relative.** `checkpoint=`/`config=` name the F12/F11 beat and loadout row the
   session was built from (`GameSession.CheckpointBeat/CheckpointConfig`, cleared by
   every real-save entry path). **Ruling encoded (Zo, 2026-08-24)**: configurations
   grow valid as the beats progress — a row granting a Primary is tainted before the
   beat where the Primary rung unlocks, and a kit whose from-scratch price exceeds
   what the progression had earned by that beat is tainted anywhere (a max-upgraded
   kit always is). `GameController.CheckpointConfigTainted(beatState, config)` reads
   both facts off the CONSTRUCTED beat state (`DemoRungTwoOpened`,
   `LifetimeSalvageEarned`) **before the overlay lands** — the rule follows the beat
   builders, never a hardcoded beat table, so retuning a beat's economy moves the
   verdicts with it. Verdicts against today's beats: row 1 (MK-1 T2) valid from
   beat 3; rows 2–4 (PRI-C T2 + protocol) valid from beat 5; validity is monotonic
   across beats. A checkpoint Run whose row is valid at its beat **counts as play
   data**; `dev=1` records only that the session's save is suppressed.
6. **`killed by unknown` is gone from attributable deaths**: the death line names the
   attacker's kind (`killed by hunter at 2.1m`), the kind outliving the 5 s distance
   window. `unknown` survives only for genuinely unattributed damage.
7. **The dashboard samples human play by default**: *Include PlayMode* defaults off
   (pre-quarantine files; new harness files are out of `Logs/` entirely), and
   config-tainted Runs ride the same chart gate as Dev — labeled always, charted only
   when the toggle says so. Every exclusion is counted on the page (the M38
   criterion-7 rule, still honoured).
8. **A panel answers "how do Runs end, and what does dying cost?"** — end mix
   (result/cause), killer mix, and per-Run deaths / respawns / respawn spend / seats /
   dial / checkpoint provenance, reading the Metric line first with the prose end
   lines as fallback for pre-M103 logs. This feeds the respawn price curve
   (`4-Data.md`, Respawns) and the three-death pacing question. Recutting the
   remaining panels onto the current question register stays open — this milestone
   ships the data path and the first such panel.
9. **The taint classification is tunable from the dashboard.** A *Checkpoint
   configurations* panel lists every observed (beat, config) cell — Run count, the
   game's verdicts (a *mixed* cell means the beat economy changed between Runs), and
   a per-cell override (game rule / clean / tainted). An override re-classifies those
   Runs across every panel immediately, is browser-local (`localStorage`, one key,
   one *Clear overrides* button), and never touches the game's rule — the log keeps
   the facts, the page keeps the opinion. The per-Run table marks overridden rows
   `[override]` so a tuned reading is never mistaken for a stamped one.
10. **Old logs still parse**: `Tools/wave-report` over `Docs/_meta/Logs/` produces
    byte-identical `--summary` and table output before and after this milestone.

## Implementation

What shipped 2026-08-24 (order was: quarantine, then the writer, then the readers):

**Change** `Assets/Scripts/Util/LogFile.cs` — `SessionBanner` (AfterSceneLoad)
detects `InitTestScene*` and close-move-reopens the session file into
`Logs/harness/` (`MoveToHarness`; on any IO failure logging continues at the old
path). `Prune(dir, keep)` takes the cap; `KeepHarnessSessions = 6`. `_harness` resets
in `Open` (statics survive in-editor play sessions).

**Change** `Assets/Scripts/Game/Flow/GameSession.cs` — `CheckpointBeat` /
`CheckpointConfig` statics beside `PendingDemoFight`; `ClearCheckpoint()` called from
`ResetStatics`, `StartNew`, `EnterRealm`, `EnterRemoteRealm`, `ContinueGame`.

**Change** `GameController.DevCheckpoints.cs` — `LoadDemoCheckpoint` judges the taint
BEFORE `ApplyCheckpointConfig` (the overlay mints its own kit into
`LifetimeSalvageEarned`, which would feed the affordability test its answer) and
records the three GameSession statics. The file is the M95 session's in-flight work,
touched minimally by agreement.
**Change** `GameController.DevCheckpoints.Configs.cs` — the beat-relative rule:
`CheckpointConfigTainted(MetaState, int)`, `ConfigTaintedFor(MetaState,
CheckpointConfig)` (row form, so tests can judge rows not in the table),
`ConfigKitCost` pricing at the slot-aware `WeaponTier.SpentToReach(slot, tier)` curve
— today's shop price, never cheaper than the historical curve the beats mint at, so
the test cannot call a beat's own kit unaffordable.
**Create** `Assets/Tests/EditMode/Dev/CheckpointConfigTaintTests.cs` — six pins
against the real beat builders: monotonic validity, the beat-1/beat-3 secondary
verdicts, the primary gate either side of beat 5, the max-kit taint, the kit-cost
arithmetic, and the 0/out-of-range never-tainted rule. In `Tools/verify/run.sh`
EXCLUDED (needs GameController), same reason as `DevCheckpointTests`.

**Change** `GameController.EndScreen.cs` — `LogRunMetric(result, cause, gross,
repair)` emits the schema-1 line; called from `EnterTerminalPhase` and from
`LogAbandonMetricIfLiveRun` (which reads **live** `Sim.Salvage.RunSalvage` /
`Sim.RepairSalvageSpent`, not `LastRunReport` — that still holds the previous run's
numbers).
**Change** `GameController.cs` — `OnDestroy` calls the abandon guard.

**Change** `Assets/Scripts/Game/Player/PlayerHealth.cs` — `_lastAttackerKind`
resolved at hit time from the source's `EnemyGatekeeper.Kind`; the death line leads
with the kind and keeps the 5 s window for the distance flourish only; cleared in
`ResetToSpawn`.

**Change** `Tools/wave-report/Program.cs` — reads `summary schema=` lines for
`configTaint`; `cfg` flag in the table, config-tainted soft-excluded from `--summary`
means beside Dev. Everything else untouched (criterion 9).

**Change** `Docs/dashboard.html` — Metric-line parser (authoritative when present);
`DOCKED` closes a Run; `YOU DIED` / `respawn taken` lines feed deaths, killer,
respawns and spend; the criterion-8 panel; playmode default off; cfg chips in the
sample strip, status line, runs and rows tables.

**Change** `Docs/Design/4-Data.md` — wave-report section records the quarantine, the
schema and the taint rule.

**Not changed, deliberately**: `RunSim` (no counters — criterion 4);
`GameController.ApplyMetaEffects` warnings and the per-frame audio-listener spam
(both proved playmode-only across the day's human logs — quarantine removes them
from the corpus, and a content filter in a deliberately filter-free sink was
rejected). Rejected: JSON for the Metric line (the log's machine tokens are
key=value); a separate telemetry file per Run (two files desynchronise; the log
survives crashes line-by-line).

## Supporting work

- **Driven by** playing a Run — nothing new to press; verification runs drive the
  harness path for free.
- **Shown by** the dashboard's sample strip and status line (kept / playmode / short /
  dev / cfg-taint counts) — here.
- **Reads back as** the `[Run] summary schema=1` line in the log and the
  "How do Runs end" panel — here.

## Verification

Ran 2026-08-24 (offline; the tree carried three concurrent sessions, so the Unity
bridge was left un-armed rather than colliding with a live editor):

1. Criterion 10 — old and new `wave-report` built with dotnet, run over all of
   `Docs/_meta/Logs/*.log`: `--summary` and table output **byte-identical**.
2. Criteria 3–5, 7–9 — `dashboard.html`'s script node-checked, then its
   `parseFile`/`finalize`/`render` driven in node over (a) a synthetic log carrying a
   checkpoint-config-2 Run with two deaths, a respawn and a Metric line, and (b) a
   DOCKED Run — 30/30 assertions pass, including: configTaint labels and excludes,
   the checkpoint cell renders, DOCKED closes the run, a checkpoint Run is **not**
   dev-tainted (the [Dev] load line precedes `run begins`), and the full tuning loop
   (override→clean flips the Run, its segments and the exclusion counts; the rule
   verdict survives underneath; override→tainted flips back; clearing returns to the
   rule; the per-Run table shows `[override]`). Over two real 2026-08-24 logs (one
   human, one harness): 40 Runs parsed, playmode default keeps exactly the 5 human
   Runs.
3. Compiles — `dotnet build` on Unity's generated `GatekeepersProtocol.csproj`,
   `.Tests.csproj`, `.PlayTests.csproj`: 0 errors. `lint-arch`: no new violations
   (the 5 present are the concurrent M95 session's files). `lint-language`: clean on
   the new docs.
4. Criterion 5 — the taint rule EXECUTED offline against the real beat builders
   (console harness over the dotnet-built Game dll, since the rule and
   `BuildDemoCheckpoint` are pure statics): all five verdict checks pass, and the
   full table reads config 1 tainted at beats {1,2}, configs 2–4 at {1..4}, earned =
   0/0/500/1400/4300 with `DemoRungTwoOpened` flipping at beat 5 — the ruling
   verbatim. The EditMode pins assert the same facts inside Unity.

Owed (needs a quiet editor — one `gkb_verify_request.txt` when the tree settles):

5. Criteria 1–2 — run the PlayMode suite once: the new log lands in `Logs/harness/`,
   `Logs/` gains nothing, human files survive. **This is also the live test of the
   quarantine itself** — the suite's own log is the fixture.
6. Criterion 6 — one dev Run died to a named kind: the death line and Metric line
   carry it.
7. Playthrough path — F12 → beat 3 → config 1 (valid there) and beat 3 → config 2
   (tainted there: Primary before the rung), die once, RETURN TO SHIP, open the
   dashboard: the first Run charts, the second is labeled `cfg`, both fill the
   "How do Runs end" panel.

## Watch

- **The exclusion rules are still the risk** (M38's watch, inherited): the harness
  split is a filter that can silently delete real data if `InitTestScene` detection
  ever matches a real session. The excluded counts on the page are the tell.
- The M95 session owns `DevCheckpoints*.cs` and was editing them mid-build (the F11
  co-op parameter landed between two reads). Re-grep before any further edit there.
- `MainMenuControllerSourceTests`-style source-pinning tests exist — none pin
  `LogFile` or the end lines today; check again before renaming either.

## Depends on / Unblocks

Depends on nothing open. Unblocks the adjudication of `M63-3`'s two-logged-Runs
criterion and `M102`'s faucet kill criteria; feeds every "needs play data" label in
`Design/` with a standing readout instead of an ad-hoc parse.
