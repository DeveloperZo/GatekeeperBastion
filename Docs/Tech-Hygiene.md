# Tech Hygiene

<!--
Purpose: Pointer. The ledger became hygiene passes on 2026-08-19; this file records the move, the id reissue, and the two rows evicted rather than migrated.
Status: superseded
Update trigger: when the last reference to a TH id outside Hygiene/ is retired
Last updated: 2026-08-19
-->

**Superseded 2026-08-19.** Owed items now live in `Hygiene/` as passes, one file per
sitting, folder as status. The contract is `_meta/_definitions/hygiene.md`.

The ledger was the wrong unit. Six rows that all close with one AutoVerify run are one
thing to schedule, not six things to track, and a table cannot say so. Grouping by the
observation that settles them collapsed twenty rows into three sittings, one of which
(`HP-2`) is a playthrough `M78` already requires.

## Where the rows went

| Was | Now |
|---|---|
| TH-1 … TH-6 (M75 criteria 2–7) | [`HP-1 — Beat zero, observed`](Hygiene/1.%20open/HP-1-beat-zero-observed.md) |
| TH-7 (delete `_to_delete/m75_scratch/`) | [`HP-3 — Hands only Zo has`](Hygiene/1.%20open/HP-3-hands-only-zo-has.md) |
| TH-10, TH-12 (M76), TH-13, TH-14, and M77's TH-9–TH-12 → reissued TH-17–TH-20 | [`HP-2 — The dock round trip`](Hygiene/1.%20open/HP-2-the-dock-round-trip.md) |
| TH-8, TH-15, TH-16 | Already settled and removed; ids retired, never reissued |
| TH-9 (M76 juice teach), TH-11 (M76 `first-hatch survived`) | **Evicted**, below |

## Ids reissued

The ledger issued **TH-9, TH-10, TH-11 and TH-12 twice**, once for M76 on 2026-08-18 and
again for M77 on 2026-08-19. That is what a shared table with no issuing rule produces, and
it silently re-points any older citation at newer work.

M77's four are renumbered **TH-17 … TH-20**, continuing past the highest id ever issued
(TH-16). M76's keep the original numbers. `hygiene.md` now states the rule: ids are issued
once, never reused, monotonic across open, settled and evicted.

## Evicted, with the reason

Neither of these is an unobserved criterion. Both are code that was never written, and the
hygiene contract excludes that on purpose: a row saying "the string is not in Scripts" is
an unbuilt criterion, not an unseen one, and parking it here makes it invisible.

- **TH-9 (M76 juice teach)** — first Field look with Unbanked > 0 plays
  `demo.juice_teach`; one `+` spends 100 and the next Wave starts `intensity=1` with an
  Event log line. The line id exists in `DemoLines`; there is no Game `DemoVoice.Play`
  call and the spend path is silent for Events. **Owed action:** write it as part of a
  milestone, or retire the Demo beat-1 juice teach row in an edit that says so. Zo
  deferred it at M76 close and it has been sitting in the wrong container since.
- **TH-11 (M76 `first-hatch survived`)** — `[Demo] first-hatch survived` on survival of the
  authored loss-1 window. String not in Scripts, and surviving that window is a tuning
  defect per Demo beat 2. **Owed action:** log the line when the no-upgrade seat clears the
  first Hatch-bank, or restamp the defect rule.
