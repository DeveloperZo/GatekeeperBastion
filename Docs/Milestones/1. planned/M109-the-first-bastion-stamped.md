# M109 — The first bastion, stamped

<!--
Status: later
Update trigger: any change to the loop's verbs (start, shutoff, elite gate, dock) or to Docs/Design/0-Gameplay.md's invariant checklist
Last updated: 2026-09-01
-->

ALPHA-7 demonstrated from wake to Escaped, adjudicated from the session logs' own lines —
assembled across as many sessions, checkpoint starts and replayed sections as Zo needs
(taint stated, per the 2026-08-20 ruling) — and the eighteen-box invariant checklist in
`0-Gameplay.md` stops being a list of unchecked claims.

## Story

As a Boarder, I want to start at the Hatch, fight up both zones, kill the elite, throw the
Master switch and see the run end Escaped, so that the first bastion is a game that can be
finished rather than a corridor of verbs that each worked once in a test.

## Why it matters

Every loop regression of 2026-08-27/28 was a verb that existed, was correct, and could not
be reached — the shutoff dead in Assault, the finale unstartable at the control room, the
armed Master switch unthrowable in Prep, the elite spawning 156 m from its fight. All four
were found by reading, not playing, and three were found *after* the suites were green.
`../../Design/0-Gameplay.md` now carries the loop's invariants as an eighteen-box checklist
(§ "The invariants, as a checklist") and **every box is unchecked**. The alpha build (0.34.2)
is cut; the one thing it has never demonstrated is its own ending. This is a verification
milestone, not a bet — no kill criterion.

## Acceptance criteria

1. The session logs (one or more; skips, replays and checkpoint starts permitted, taint
   stated per the 2026-08-20 ruling) together contain the full chain: `wave started from
   the Hatch — zone 1`, `T2 shut down`, `wave started from the Hatch — zone 2`, `T5 shut
   down`, `wave started from the Hatch — Master Control`, `master-control elite`,
   `master-control elite down`, `Master shut down`, and a summary line whose
   `cause=cleared` — and **each consecutive pair of those beats appears inside a single
   log at least once**, so every seam is crossed live rather than assumed. (All four
   August regressions lived in the seams; the pair rule is what this stamp keeps of the
   old single-run requirement, dropped 2026-09-01 at Zo's direction.)
2. In the log covering the finale, the Master terminal never starts a wave: no `master-control started`
   line precedes the Hatch's `Master Control answers at` line, and
   `refused — Master Control starts at the Hatch` appears only if the player pressed T
   there.
3. The stake bit at least once across the evidence: `hatch under attack` appears, and a
   summary's hatch HP is below max — or, if the door never took damage, that fact is
   recorded on the M112 evidence list rather than ignored (it is M112's opening exhibit).
4. A second log (taint permitted, stated) shows the dock leg: `dock offered` after a zone
   shutoff, then `portal entered` ending the run `Docked` — from a **terminal-side** portal,
   not the Hatch one, so the T5 walk-back removal is what is proven.
5. Every box in `0-Gameplay.md`'s invariant checklist is checked, each with a one-line
   evidence note (log line, test name, or criterion number here) — no box checked by
   assertion alone.
6. `Docs/demo-readiness.md` row **E7** (first-loss fairness) carries Zo's verdict from the
   playthrough — worded by Zo, any verdict closes it.
7. The file-bridge suites are green against the recorded baseline: EditMode and PlayMode
   via `gkb_verify_request.txt`, with no failures beyond the standing set named in
   Verification, and `HatchStartLinePlayModeTests` (both finale tests) passing.

## Implementation

No production code is in scope. If a criterion cannot be met, the defect it exposes is
fixed under this milestone only when it is a wiring defect of the loop's verbs; anything
that is a tuning judgment is recorded and deferred to M112 rather than adjusted mid-stamp
(one cause, one change).

**Change** `Docs/Design/0-Gameplay.md` — check the boxes, one evidence note each.
**Change** `Docs/demo-readiness.md` — E7 verdict, and re-stamp `Last updated`.

Rejected alternative (original form): one untainted end-to-end run. Dropped 2026-09-01 —
Zo will skip and replay sections freely, and the taint ruling already permits tainted
sessions to close gates when stated. What survives of it is criterion 1's seam-pair rule:
a checkpoint leg proves a verb, and only a log that crosses a seam proves the seam.

## Supporting work

- **Driven by** the player's own inputs — here; nothing scripted drives the stamp run.
- **Shown by** the existing HUD and prompts — nothing new is shown by this milestone.
- **Reads back as** the session log lines named in criteria 1–4 — here; `M103`'s summary
  line is the adjudicator of the `cause=` field, and if `M103`'s Unity leg slips, criterion
  1 falls back to reading the raw `[Run]` lines, which already exist.

## Verification

1. Criteria 1–4 — `Tools/wave-report/run.sh <log>` plus reading the named lines in the raw
   log. No new tooling; the lines all exist in the 0.34.2 tree.
2. Criterion 5 — read `Docs/Design/0-Gameplay.md`; eighteen checked boxes, eighteen notes.
3. Criterion 7 — file bridge (`gkb_verify_request.txt` → `gkb_verify_report.json`), editor
   open. Baseline standing reds at time of writing:
   `M59PolishChromeTests.PauseShortcuts_AreBoundOnHudController`,
   `M59PolishChromeTests.ShortcutTemplates_HaveFilledCooldownFill`,
   `ShortcutRowApiTests.FillRows_JuiceRow_CarriesIntensity`,
   `TwoRunIsolationTests.SimAssembly_HoldsNoStaticMutableState` — fewer is fine (M111
   retires them), more is a fail.
4. Playthrough path — from Play: MainMenu → realm → EMBARK, then the run itself is the
   milestone. Replayable slice, named per the 2026-08-22 decision:
   `GameController.BuildDemoCheckpoint` digits 1–6 for per-leg reruns, and
   `HatchStartLinePlayModeTests` / `BastionRunDrive.WinThroughMasterControl` as the
   scripted twins of criteria 1–2.

## Watch

- Taint is stated wherever it exists (`Docs/Decisions.md`, 2026-08-20) — it disqualifies
  nothing here, but an unstated taint disqualifies the log it hides in. Note the contrast:
  `M112` still requires untainted runs, because its purpose is play *data*, not wiring
  proof.
- `FieldWaveAwaitsShutoff` returns false during the finale by construction (zone table
  ends at 2) — no trickle in the control room is correct, not a bug to fix mid-stamp.

## Do not build

- Any tuning change, however tempting mid-run — that is M112's lane.
- New log lines; the stamp must be readable from what 0.34.2 already emits.

## Depends on / Unblocks

Depends on: nothing in the tree; the build is cut. `M103`'s Unity leg makes criterion 1
cheaper but is not a gate. Unblocks: `M112` (its evidence base is this run plus its
successors), and the friends-alpha dry-run sentence in `../../Roadmap.md`.
