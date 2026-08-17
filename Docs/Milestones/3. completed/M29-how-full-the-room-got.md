# M29 — How full the room got

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

An offline reader over the session logs that prints, per Wave, how many Gatekeepers were
alive in the room on average. It is the effort measure `Design/1-Experience.md`, section 1.3
names. Three log lines gained machine-readable tails on the same stream; `Assets/` gained
no counter.

## Done when

**Met (tool + emit + docs).** `Tools/wave-report/Program.cs` + `run.sh`; group line
`— N live`; `[Ship] bought … track=`; `run begins … startWave= primary= …`; mean live
defined in `Design/3-Mechanics.md` and named in `1-Experience.md` §1.3; `language.md`
rule 3 lists mean live / mean lifetime; `Working-Rules.md` verification path 2.

**Pinned arithmetic checked on the Aug-8 stand-in** (fixture file missing on disk — see
Remaining): `Logs/GatekeeperBastion_2026-08-08_17-00-15.log` reads mean live
`5.00 / 11.30` (Run 1 w1 / w2) and `9.88` (Run 2 `w1+2+3+4+5`), matching
`GatkeeperZo Notes/playthrough.md`. Pre-rename
`Docs/_meta/Logs/GatekeeperBastion_2026-08-03_11-31-59.log` parses. Dev after RUN OVER
does not taint (`23-44-23` marks `dev` mid-Run; Aug-8 post-Run Dev does not).

## Remaining to close

| # | Status | Evidence / action |
|---|---|---|
| Criterion 2 pin file | **blocked** | Restore `Logs/GatekeeperBastion_2026-08-07_16-34-30.log` (absent on this machine; cited everywhere). Re-run reader; expect `2.84 / 5.97 / 9.52 / 11.26` and Run 1 `5.47 / 11.04`. |
| Criterion 6 lost pin | **blocked** | Restore `Logs/GatekeeperBastion_2026-08-03_19-18-22.log`; expect lost w2 at `13.13` and `11.40`. |
| Criterion 8 suite | **open** | AutoVerify EditMode twice hit `stuck` at 180s idle while tests were still running (`gkb_verify_report.json` 2026-08-09T20:07:13Z). Re-run via menu `GatekeepersProtocol > Run Verification Now` once the editor is quiet; expect green EditMode (format-string-only change). |
| Playthrough (Verification §7) | **open** | Embark Wave 1 → clear one Wave → quit; confirm `— N live` on group lines and segment lands on zero live. |
| `run.sh` on this host | **env** | Needs `mcs`+`mono` on PATH (same as verify/docgen). Verified here via `dotnet` compile of the same `Program.cs`. |

## Story

As the developer, I want a number that says how hard a Wave actually was, so that a
tuning change can be graded instead of argued.

## Why it matters

**The current instrument carries no information on a good Run.** Hatch remaining measures
leakage. On `Logs/GatekeeperBastion_2026-08-07_16-34-30.log` the second Run cleared all
five Waves at 300/300 every Wave, and Zo reported it as fun and semi-challenging
(`Design/1-Experience.md`, section 1.3). Four Waves, one number, zero spread. M16's ±15 band was
retired for exactly this reason (`Milestones/3. completed/M16-a-difficulty-curve.md`,
*Correction*), and nothing replaced it, so M20, M21, M23, M25 and M26 all end in a
judgement call with no reading behind it.

**Mean live Gatekeepers separates the same Waves 4:1.** Same log, same Run, chain-merged
Waves 1+2 then Waves 3, 4, 5:

| Wave | bodies | seconds | Hatch at clear | **mean live** | peak live | mean lifetime |
|---|---|---|---|---|---|---|
| 1+2 chained | 48 | 56.5 | 300 | **2.84** | 8 | 3.34 s |
| 3 | 42 | 34.1 | 300 | **5.97** | 10 | 4.85 s |
| 4 | 54 | 35.9 | 300 | **9.52** | 18 | 6.33 s |
| 5 | 66 | 39.5 | 300 | **11.26** | 20 | 6.75 s |

It also separates loadouts within one session. The first Run in that same file is the same
player on the same Waves with the Primary at T1 instead of T4:

| Wave | Hatch at clear | mean live, T1 | mean live, T4 |
|---|---|---|---|
| 1+2 chained | 272 vs 300 | 5.47 | 2.84 |
| 3 | lost vs 300 | 11.04 | 5.97 |

**It is one number, not two.** `mean live = arrivals per second × mean lifetime`, and the
two factors split the reading cleanly: arrivals per second is the Wave table
(`Design/4-Data.md`), mean lifetime is the player. Printing both costs nothing and says
which side moved.

**Nothing new has to be counted.** `WaveSpawner.AliveCount` already exists and is already
written to every kill line. The reader reconstructs the step function from the group and
kill lines that ship today, which is why this does not reopen `Decisions.md`'s ban on
counters, rollups and a stats object.

### Why the emit side is in scope, and why it stays small

A dashboard built by hand from the 18 logs on disk on 2026-08-09 needed **no code change at
all** — every panel came out of lines that already ship. Three things were brittle, and all
three are line *format* rather than missing data:

| Brittle today | Why | Fix |
|---|---|---|
| Purchase attribution | `bought MR-35 Rifle focus reload (+damage, +fire rate) for 1200` is prose the tool string-matches to bucket | one explicit track token |
| Group composition | `3 Gatekeepers, 1 Hunter breached` is prose parsed to recover a spawn count | the `AliveCount` suffix, criterion 12 |
| Run attribution | loadout, start Wave and rig state are parsed out of one long sentence | keep the sentence, append stable fields |

**Mean live stays a computation, and must.** Integrating live count over time at runtime is a
counter, which `../../Decisions.md` forbids. The log carries events; the tool does the
arithmetic. That division is why this is a reader and not a feature.

**One line, two audiences.** The human sentence and the machine fields sit on the same line,
the way M31's `— 9 live` suffix already does. The session log stays readable and the tool
stops guessing — which is what "keep the playthrough logs focused" buys without a second
stream.

Leverage tier (`Working-Rules.md`): **#6, information flows.** No parameter moves here.
This is the reading that the parameter milestones above it currently lack.

### Multiplier, and why this is not a chore

`_meta/_definitions/milestone.md` names "a log parser written to settle one balance
argument" as the example of a chore. This one fails that description by the test the
contract gives: it outlives the milestone that motivated it. Every balance gate on the
roadmap reads it, the retired M16 band is the thing it replaces, and the alternative is
a fresh ad-hoc parse per tuning question with no two questions answered the same way.

### The metric that was proposed and does not work

*Time at full attention* — the share of a Wave with a Combat or Player event inside a 2 s
window — was recommended before it was computed. On the fixtures it reads **98.1% on a
Wave that was cleared comfortably and 80.1% on a Wave that was lost**, and it saturates
above ~95% where the interesting Waves sit. It is recorded here so it is not proposed
again.

## Acceptance criteria

1. `Tools/wave-report/run.sh <log>...` exits 0 and prints one row per Wave segment per
   Run: Run index, Wave label, bodies, seconds, mean live, peak live, mean lifetime,
   Hatch at clear, and `lost` where the segment ended a Run.
2. **The 2026-08-07 fixture is pinned.** On `Logs/GatekeeperBastion_2026-08-07_16-34-30.log`
   the second Run reads mean live `2.84 / 5.97 / 9.52 / 11.26` (±0.05) for segments
   `w1+2 / w3 / w4 / w5`, and the first Run reads `5.47 / 11.04`. A change that breaks
   reading this file fails the run.
3. **Chained Waves are one segment, labelled `w1+2`.** A Wave line arriving with no
   `pulled the siphon draw` since the previous Wave line is a deep-start chain
   (`Milestones/3. completed/M14-five-waves-and-a-deep-start.md`), and `AliveCount` is
   aggregate across it by design (`WaveSpawner.cs:133`). The reader merges rather than
   splitting, and never reports a per-Wave figure it cannot derive.
4. **Pre-rename and pre-group logs parse.** Every fixture predates 2026-08-08, so the kill
   line reads `killed a Boarder` and the wave line reads `N boarders`, both meaning the
   enemy; logs before M10 also arrive one body at a time
   (`boarder 4/9 breached at Breach_NW`) rather than in groups. The reader accepts all
   three older forms; its own output always says Gatekeeper
   (`_meta/_definitions/language.md`, trap 5). A form it does not recognise is a named
   parse failure, never a silently dropped event.
5. **A Run containing `[Dev]` is marked on every row of that Run** and excluded from
   `--summary`. A dev-tainted reading cannot close a gate
   (`Milestones/3. completed/M13-dev-mode.md`).
6. A segment that ends in `RUN OVER` or `HATCH BREACHED` is reported with seconds counted
   to that line and `lost` set — not dropped. The lost Waves are the ones a curve is
   graded on.
7. `--summary` over several logs prints one row per Wave number with the mean and the
   sample count across all clean Runs, and states how many Runs it read.
8. **`Assets/` gains no type, field or counter.** The only change under `Assets/` is
   format strings on existing log lines. Re-cut: three lines (group / bought / run begins).
   No new type, no new field, no aggregation.
9. `Design/3-Mechanics.md` defines mean live beside the gatekeeper-second budget, and
   states plainly that they are different quantities: gatekeeper-seconds are time spent
   at the Hatch, mean live is time spent in the room.
10. `Design/1-Experience.md`, section 1.3 names mean live as the instrument, records the
    2026-08-07 curve above as the only measured one, and replaces the retired ±15 band
    with the open question and its trigger from *Open questions* below.
11. `_meta/_definitions/language.md` rule 3 lists mean live and mean lifetime as
    builder-only, so M28's check keeps them out of `Docs/Play/`.
12. **The group line carries `AliveCount`** — `group 3: 3 Gatekeepers, 1 Hunter breached at
    Spawn_NE (12/30) — 9 live`. Existing state on `WaveSpawner`, appended to an existing
    line: a format change, not a counter.
13. **Every `[Ship] bought` line carries an explicit track token** — `track=weapon`,
    `track=crew`, `track=fabrication`, `track=equipment` — so spend attribution is a read
    rather than a string match. The human half of the sentence is unchanged.
14. **`[Run] run begins` ends with a stable field list** — `startWave=`, `primary=`,
    `primaryTier=`, `secondary=`, `secondaryTier=`, `rig=`, `banked=`. The prose stays; the
    fields are appended.
15. **`--html` writes a self-contained page** — no CDN, no network, inline CSS and JS — with
    the per-Wave curve, the Run outcomes, the spend split, and a table view of every row.
    Re-running it overwrites the file, so a post-session hook keeps it current.
16. **Still no counter, rollup or stats object** (`../../Decisions.md`). `git diff Assets/`
    touches three log format strings and nothing else — no new type, no new field, no
    aggregation. **A reviewer who finds an accumulator has found a milestone that failed.**

## Implementation

**Create** `Tools/wave-report/Program.cs` and `Tools/wave-report/run.sh` — the layout
`Tools/docgen` and `Tools/verify` already use: C# sources compiled with `mcs`, run with
`mono`, driven by a `run.sh` that takes the file list and the flags.

C# rather than shell and `awk`, matching `docgen`: the reading is a time-weighted integral
over an interleaved event stream, and the fixture pin in criterion 2 wants exact
arithmetic rather than `awk`'s.

### The reading

```
segment      one Wave, or one chain of Waves, from its wave line to its clear or run-end line
live(t)      +group size on each group line, -1 on each kill line, 0 at segment start
meanLive     integral of live(t) over the segment / segment seconds
peakLive     max live(t)
meanLifetime meanLive / (bodies / seconds)      -- Little's Law; the player's factor
```

`live(t)` is exact rather than sampled. Spawned minus killed is the whole population:
nothing else removes a Gatekeeper, and a segment that ends in a clear must land on zero.
**A segment ending on a non-zero live count is a parse failure and says so** — that check
is what catches a log-format change instead of silently reporting a wrong number.

### The one code change

**Change** `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs:120` — append `AliveCount`
to the group line:

```
group 3: 3 Gatekeepers, 1 Hunter breached at Spawn_NE (12/30) — 9 live
```

`AliveCount` is existing live state on `WaveSpawner`, not a new counter, so this is a log
format change and not a `Decisions.md` violation. Without it the reader has to infer the
group size from `Describe(tally)`'s prose, which is the one part of the parse that breaks
on a wording change. The reader still accepts lines without the suffix, or every log
written before this ships becomes unreadable — and those are the fixtures.

### Flags

| flag | does |
|---|---|
| *(none)* | per-Wave rows, grouped by Run |
| `--summary` | one row per Wave number across all files, mean and n, clean Runs only |
| `--csv` | the same rows, comma-separated, for pasting into a Design table |
| `--html <out>` | a self-contained dashboard page, overwritten each run |

**Do not build**: a target band, a pass/fail exit code on the numbers, or a hook into
`Tools/verify/run.sh`. Verify is a gate on tests; this is a readout, and wiring a
readout into a gate before there is a band to gate on is how M16's band got written
against one Run.

## Supporting work

Developer tooling, so the three questions are answered for the developer.

- **Driven** — `Tools/wave-report/run.sh Logs/*.log`.
- **Shown** — a row in `Working-Rules.md`'s verification paths, beside `Tools/verify` and
  `Tools/docgen`. Without that row it is a script nobody knows exists, which is the M15
  failure in a different costume.
- **Reads back** — the table, and a named parse failure when a log stops matching.

## Verification

1. Criteria 1–2 — `Tools/wave-report/run.sh Logs/GatekeeperBastion_2026-08-07_16-34-30.log`;
   expect the eight rows above with the pinned figures.
2. Criterion 3 — the same file; both Runs embark `starting at wave 2`, so both must show a
   `w1+2` row and no standalone `w1`.
3. Criterion 4 — `Docs/_meta/Logs/GatekeeperBastion_2026-08-03_11-31-59.log` is
   pre-weapon and pre-rename; expect rows, not a parse failure.
4. Criterion 5 — any log from 2026-08-05 onward containing `[Dev]`; expect every row of
   that Run marked and the Run absent from `--summary`'s n.
5. Criterion 6 — `Logs/GatekeeperBastion_2026-08-03_19-18-22.log`, two lost Runs; expect
   `w2` rows at mean live `13.13` and `11.40`, both `lost`.
6. Criterion 8 — `git diff --stat Assets/` names the three format-string files;
   `Tools/verify/run.sh` / AutoVerify EditMode passes.
7. Playthrough path — from Play: embark at Wave 1, clear one Wave, quit. Run the reader
   over the new log. The `— N live` suffix appears on the group lines and the segment
   lands on zero live.

## Open questions

- **What mean live a Wave should target is unknown; needs play data.** One Run is not a
  curve, and setting a band from the 2026-08-07 figures would repeat M16 exactly. Trigger:
  three clean Runs at the same loadout, plus Zo's report on each. Owner: the milestone
  that next moves a Wave parameter.
- **Whether mean live predicts a loss is not claimed and should not be.** In the fixtures
  a cleared Wave 5 reads 11.26 and a lost Wave 2 reads 11.40. The measure is effort, not
  outcome; whether the Hatch survives is what Hatch remaining is for, and the two readings
  are complementary rather than competing.

## Watch

- **Mean lifetime is the discriminating factor in the fixtures and it is 11 samples.**
  Every cleared Wave reads ≤ 6.75 s at 300 Hatch, 7.1–9.5 s where the Hatch leaked, and
  ≥ 10.6 s where the Run was lost. That is suggestive and it is four sessions and two
  weapons. Do not promote it to a threshold in this milestone.
- The chain rule in criterion 3 depends on the `pulled the siphon draw` line staying the
  only thing between sequential Waves. If a future milestone adds a Wave start path that
  does not log a draw, the reader silently merges Waves that were not chained.
- `gatekeeper-seconds` in `Design/3-Mechanics.md` is a different quantity with a nearly
  identical name (criterion 9). This is exactly the collision `language.md` rule 2 exists
  to prevent, and it is accepted here only because both terms are already load-bearing —
  which is why the definition has to say so in both places.

- **The three format changes are a contract.** `Tools/wave-report` and any future reader
  breaks when a line's shape changes, exactly as a caller breaks on a changed signature. A
  milestone that edits one of these lines updates the reader in the same commit
  (`../../Architecture/4-Testing-and-Determinism.md`, section 6).
- **`--html` is a convenience, not a gate.** Nothing may depend on the page existing; the
  rows are the deliverable and the page is a rendering of them.

## Depends on / Unblocks

Depends on nothing. Unblocks the balance gate on M20, M21, M23, M25 and M26, and is what
a re-opened M16 would need before it could state a band.

## Correction (2026-08-09, M38)

`--html` shipped as one self-contained file with no fixed path. Its Run set included
PlayMode harness Runs (`stage=playmode`). Spend without `track=` landed in a `legacy`
bucket. **M38** filters PlayMode by `stage=playmode`, drops Runs shorter than 5 s,
name-maps pre-token purchases, and groups by balance era.

**Correction (same day):** the first M38 cut wrote a generated `Dashboard/data.js` snapshot
because `file://` cannot scan `Logs/` unaided. That was the wrong shape. The bookmark is
**`Docs/dashboard.html`**: open it, choose the Logs folder (or drop the files); JavaScript
reads and parses the logs in the browser. `Tools/wave-report --dashboard` is removed;
`--html <out>` remains for a one-shot file dump.
