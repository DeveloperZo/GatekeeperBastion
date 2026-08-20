# Hygiene

<!--
Purpose: Summary and id register over Docs/Hygiene/. Which passes exist, what each settles, and every TH and HP id ever issued with where it went. The summary layer a pass folder needs, exactly as Roadmap.md is to Milestones/.
Status: living index
Authority: none over design or code. A pass records a debt and the observation that settles it; it does not gate anything.
Update trigger: when a pass is created, moves folder, or when an id is issued, settled or evicted.
Last updated: 2026-08-19
-->

Owed items live here as **hygiene passes** — one file per sitting, folder as status. The
authoring contract is [`../_meta/_definitions/hygiene.md`](../_meta/_definitions/hygiene.md).

This file is to `Hygiene/` what [`../Roadmap.md`](../Roadmap.md) is to `../Milestones/`: the
summary layer, and the **permanent id register**. It replaced `Tech-Hygiene.md`, the table this
folder grew out of, on 2026-08-19.

Nothing here is a claim that something is broken, and nothing here is a claim that something
works. An item says only that it was not observed.

**Authority for settling any item that touches a MonoBehaviour, a scene or the save file is the
AutoVerify file bridge**: write `gkb_verify_request.txt` at the project root with the editor
open and read `gkb_verify_report.json`. The offline harness `Tools/verify/run.sh` cannot see
those, so a green harness run does not settle those items
([`../Working-Rules.md`](../Working-Rules.md), Verification).

## Passes

| id | Pass | Status | Closable by | Items |
|---|---|---|---|---|
| HP-1 | [Beat zero, observed](1.%20open/HP-1-beat-zero-observed.md) | open | AutoVerify (pre-v9 save staged), plus one play pass for the refuse strings and three quit-and-reload cycles | TH-1 … TH-6 |
| HP-2 | [The dock round trip](1.%20open/HP-2-the-dock-round-trip.md) | open | Zo at the keyboard, one run — the `Returned` half of M78 criterion 9 | TH-10, TH-12, TH-14, TH-17 … TH-20 |
| HP-3 | [Hands only Zo has](1.%20open/HP-3-hands-only-zo-has.md) | open | Zo, at the filesystem | TH-7 |
| HP-4 | [The lost endings hold](1.%20open/HP-4-the-lost-endings-hold.md) | open | Zo, two runs plus one demo-off run | TH-13 |

Settled passes move to `2. settled/` and stay in this table with their close date.

## The id register

**Ids are issued once, never reused, monotonic across open, settled and evicted.** The next id
is one past the highest that has ever existed. **Next free: `TH-21`, `HP-5`.**

This table is the register. It is the reason this file outlives any single pass: milestones and
the changelog cite item ids, so a reused id silently re-points an old reference at new work.

| id | Owed | Where it is |
|---|---|---|
| TH-1 … TH-6 | M75 criteria 2–7, never separately observed | `HP-1`, open |
| TH-7 | Delete `_to_delete/m75_scratch/` | `HP-3`, open |
| TH-8 | Delete `Assets/_Scratch/Editor/` | **Settled** 2026-08-19 — Zo deleted it; the verify gate compiles again |
| TH-9 | M76 juice teach — `demo.juice_teach`, `+` spends 100, next Wave `intensity=1` with an Event line | **Evicted** 2026-08-19 — see below |
| TH-10 | M76 criterion 3: `DemoZoneDeepestWave[1]` on each field-Wave clear | `HP-2`, open |
| TH-11 | M76 criterion 5: `[Demo] first-hatch survived` on surviving the loss-1 window | **Evicted** 2026-08-19 — see below |
| TH-12 | M76 criteria 6–7: first Docked plays `demo.first_hatch_down`, plus beat 1–2 voice and `zone terminals complete zone=1` | `HP-2`, open |
| TH-13 | M77 criterion 7: down and breach still end Lost, and with `DemoActive false` | `HP-4`, open |
| TH-14 | M77 criterion 8: the full survive → dock → spend → re-embark playthrough | `HP-2`, open |
| TH-15 | `Packs/Gatekeepers/Default.mat` approximated the UtilityBot | **Settled** 2026-08-19 — moot; the Shader Graph was restored from the package. See `M88` |
| TH-16 | Every FaxLab3D robot magenta, pack gitignored so nothing survived re-import | **Settled** 2026-08-19 — Shader Graph restored, `lint-vendor` now gates it, materials tracked. See `M88` |
| TH-17 | M77 criterion 1: `EndRun("docked")` lands `Returned`, no `PodEjections++`, end screen names the cause | `HP-2`, open — **was TH-9** |
| TH-18 | M77 criterion 4: `[hold E] Dock` live at the Hatch after a survived window, gone once the next Wave triggers | `HP-2`, open — **was TH-10** |
| TH-19 | M77 criterion 5: bank snapshot persists banked + door state, restores on re-embark | `HP-2`, open — **was TH-11** |
| TH-20 | M77 criterion 6: `demo.door_d1` then `demo.dock_option`, in that order, on the survival frame | `HP-2`, open — **was TH-12** |

### The double-issued block

The old ledger issued **TH-9 through TH-12 twice** — once for M76 on 2026-08-18, again for M77
on 2026-08-19. M77's four were renumbered **TH-17 … TH-20**, continuing past the highest id
that had ever existed (TH-16); M76's keep the original numbers.

Any citation written before 2026-08-19 reading "M77's TH-9 to TH-12" means TH-17 … TH-20. The
citations in `../Roadmap.md`, `../Milestones/3. completed/M77-the-run-can-end-docked.md` and
`../Milestones/3. completed/M75-wake-on-a-dead-ship.md` were repointed on 2026-08-19; entries
in `../Changelog.md` predating that date were left as written, because a changelog is a record
of what was true when it was written.

## Evicted, with the reason

**Eviction is a recorded outcome, not a deletion.** Neither of these was an unobserved
criterion. Both are code that was never written, and the contract excludes that on purpose: an
item saying "the string is not in Scripts" is an unbuilt criterion, not an unseen one, and
parking it in a hygiene pass makes it invisible.

Both carry an owed action that is **not tracked here** — a pass cannot hold them. Each needs a
milestone, a defect, or a deliberate retirement in an edit that says so.

- **TH-9 — M76 juice teach.** First Field look with Unbanked > 0 plays `demo.juice_teach`; one
  `+` spends 100 and the next Wave starts `intensity=1` with an Event log line. The line id
  exists in `DemoLines`; there is no Game `DemoVoice.Play` call and the spend path is silent
  for Events. Zo deferred it at M76 close and it sat in the wrong container until the
  migration. **Owed action:** write it as part of a milestone, or retire the Demo beat-1 juice
  teach row in an edit that says so.
- **TH-11 — M76 `first-hatch survived`.** `[Demo] first-hatch survived` on survival of the
  authored loss-1 window. String not in Scripts, and surviving that window is a tuning defect
  per Demo beat 2. **Owed action:** log the line when the no-upgrade seat clears the first
  Hatch-bank, or restamp the defect rule.
