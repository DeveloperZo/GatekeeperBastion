# M38 — A dashboard you can open

<!--
Status: completed
Update trigger: when the dashboard's file layout changes, or when a Run-exclusion rule changes
Last updated: 2026-08-09
-->

`--html` shipped in M29 and works. It gains a fixed home, a page that stops being rewritten
on every run, and three fixes to what it prints — two of them defects that make the current
output wrong rather than merely cluttered.

## Story

As the developer, I want one file on disk that always shows the current state of every logged
Run, so that checking whether a change worked is opening a bookmark rather than remembering a
command.

## Why it matters

**It already reads from disk and that is not the problem.** Run 2026-08-09 over all 12 session
logs: it writes a self-contained page, opens from `file://` with no console errors, no CDN.
Zo's ask — open it from Explorer or from Cursor, reading from disk — is already satisfied.
**What is wrong is the output.**

**Two defects make it print wrong things, both confirmed by running it:**

| Defect | Evidence | Consequence |
|---|---|---|
| PlayMode test runs counted as Runs | `15-23-51.log` and `14-28-53.log` carry `stage=playmode`; between them they contribute six "Runs", two lasting under a second | The Run-outcomes table is a third test noise, and the loss count is wrong |
| Spend split reads `legacy 11 / 3800` and `weapon 1 / 0` | M29's `track=` token exists on exactly **one** purchase line across all 12 logs; everything older has none | The one category that reads correctly shows **zero salvage**, and the real spending sits in a bucket named after the parser |

**And the per-Wave chart is 30 bars on one axis**, spanning three balance eras, both weapons,
clean and dev-tainted Runs together. Nothing can be read off it.

**The page and the data are the same file**, so every regeneration is a whole-file diff and
the layout can never be reviewed or hand-edited.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** A readout nobody can read
is the same as no readout.

**Multiplier**: every tuning milestone from here reads this page. It replaces the ad-hoc
one-off parse that has preceded every balance argument in this project so far.

## Acceptance criteria

1. **`Tools/wave-report/run.sh --dashboard` writes `Dashboard/index.html` and
   `Dashboard/data.js`** at the repo root, with no path argument.
2. **The existing `--html <out>` is unchanged.** It still writes one self-contained file to an
   explicit path. **Making the path optional is forbidden** — `run.sh --html Logs/*.log` would
   consume the first log as the output path and overwrite a session log with HTML.
3. **`Dashboard/index.html` is committed and byte-identical between two consecutive
   `--dashboard` runs.** `Dashboard/data.js` is gitignored and is the only file a regeneration
   touches.
4. **The page opens by double-click** from a file manager and in Cursor's preview, with no
   server and no console error. `data.js` loads through a `<script src>` tag, which works over
   `file://` where a `fetch` of JSON is blocked.
5. **Runs from PlayMode are excluded**, identified by `stage=playmode` in the log —
   **not by the presence of `AutoVerify`**, which appears in ordinary session logs including
   `15-21-59.log`, the one every current balance claim rests on.
6. **Runs shorter than 5 seconds are excluded** as a second filter, since a crashed or aborted
   Run carries no reading. The threshold is stated here and in `Design/4-Data.md`.
7. **The page states how many Runs it excluded and why**, per rule. A silent filter is a page
   that lies about its own sample size.
8. **No `legacy` bucket in the spend split.** Purchases with no `track=` token are bucketed by
   name, using the same mapping the reader already has, and the row is labelled `pre-token` so
   it reads as a parsing artefact rather than a category of spending.
9. **The per-Wave chart is grouped by balance era** — pre-weapon, horde retune, weapons and
   wheel — and **excludes dev-tainted Runs by default**, with the Dev column kept so the
   exclusion is visible rather than assumed.
10. **A dev-tainted Run is never silently dropped**: it is excluded from charts and present in
    the table, flagged.

## Implementation

**Change** `Tools/wave-report/Program.cs` — add a `Dashboard` mode alongside `Html`; split
`BuildHtml` into a page emitter that runs only when `Dashboard/index.html` is absent or
`--force-page` is passed, and a `data.js` emitter that runs every time. Add the two Run
filters, the spend-bucket relabel and the era grouping.

**Create** `Dashboard/index.html` — committed; layout and drawing only, reading the global
`DATA`.

**Change** `.gitignore` — `Dashboard/data.js`.

**Change** `Docs/Milestones/3. completed/M29-how-full-the-room-got.md` — append a correction
recording that `--html` shipped as one self-contained file with no fixed path, that its Run
set included PlayMode tests, and that M38 fixes both. Shipped files take corrections, never
retro-edits (`../../_meta/_definitions/milestone.md`).

Rejected: `fetch('data.json')`. Blocked by CORS over `file://`, so it would need a local
server and stop being a double-click.

Rejected: keeping one self-contained file and living with the diff. The page is the part worth
reviewing and hand-tuning; burying it in generated output is what makes charts stay bad.

## Supporting work

- **Driven** — `Tools/wave-report/run.sh --dashboard`.
- **Shown** — a fixed path, `Dashboard/index.html`, named in `Docs/README.md` so it is
  findable without reading a milestone.
- **Reads back** — the page, and the excluded-Run count on it.

## Verification

1. Criteria 1, 3 — run `--dashboard` twice; `git status` shows `index.html` unchanged and
   `data.js` untracked.
2. Criterion 2 — `run.sh --html` with no path exits non-zero with a usage message, and no log
   file is modified.
3. Criterion 4 — double-click the file; open it in Cursor's preview; check the console.
4. Criteria 5, 6, 7 — over all 12 logs, the page reports the excluded count. **`15-21-59.log`
   must still be included**; if it is not, criterion 5 has been implemented as the
   `AutoVerify` rule this milestone exists to prevent.
5. Criterion 8 — the spend table has no `legacy` row and the weapon row is non-zero.
6. Criteria 9, 10 — three era groups; a dev-tainted Run appears in the table and in no chart.

## Watch

- **The exclusion rules are the risk, not the layout.** Every one of them can silently delete
  real data, which is why criteria 7 and 10 exist and why criterion 4 pins a specific log that
  must survive.
- Adding M37's panel here would couple two milestones. **M37 ships its rows as terminal output
  and this page picks them up whenever both have landed**; neither blocks the other.

## Depends on / Unblocks

Depends on M29, shipped. Independent of M37 — either can go first.

## What shipped (2026-08-09)

**Re-cut same day:** `Docs/dashboard.html` — open the file, choose the Logs folder (or drop
`GatekeepersProtocol_*.log`); JavaScript parses the logs. No regenerate script. Root
`Dashboard/` + `--dashboard` snapshot removed (`file://` cannot enumerate `Logs/` alone).

Filters kept: PlayMode via `stage=playmode`, short Runs under **5 s**, name-mapped spend
(no `legacy`), era-grouped mean-live curves. `Tools/wave-report --html` remains for a
terminal file dump.

### Still on Zo

1. Open `Docs/dashboard.html`, choose `Logs` (and optionally `Docs/_meta/Logs`), confirm
   `15-21-59` is in the sample and the console is clean.
2. Criterion 10 demo — needs a ≥5 s Dev-tainted Run in the sample (none on disk today).
