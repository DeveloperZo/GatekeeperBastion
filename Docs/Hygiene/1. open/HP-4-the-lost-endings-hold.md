# HP-4 — The lost endings hold

<!--
Status: open
Closable by: Zo at the keyboard, two runs plus one demo-off run
Update trigger: when an item is added, settled or evicted
Last updated: 2026-08-19
-->

Prove the two Lost endings still end the run Lost, and that the rule holds with the demo off.

Split out of `HP-2` on 2026-08-19. `HP-2` is settled by the `Returned` round trip that M78's
criterion 9 already schedules; this item cannot ride that run. A death and a Hatch breach each
end the run **Lost**, so neither can happen in a run that ends `Returned`, and a `DemoActive
false` run is a third configuration again. Three runs, not one — which is a second sitting, and
the grain rule says that is a second pass.

## Items

1. **TH-13** — M77 criterion 7: solo down and Hatch breach still end the run Lost exactly as
   before, and the rule holds with `DemoActive false`. The sim half is pinned by
   `DockedRunTests.SoloDown_AndHatchBreach_StillEndLost`; the demo-off case and the on-screen
   Lost path are not. Settles on one death on screen, one Hatch breach on screen, and one run
   with the demo off.

## Not this pass

- The `Returned` half of M78's criterion 9 and everything that falls out of it — that is
  [`HP-2`](HP-2-the-dock-round-trip.md).
- M78's own criteria 0–9 — those are M78's close gate, not owed items.
