# HP-4 — The lost endings hold

<!--
Status: open
Closable by: Zo at the keyboard, two runs plus one demo-off run
Update trigger: when an item is added, settled or evicted
Last updated: 2026-08-25 (TH-13 restated against the M98 respawn recut — the solo-death half is now conditional)
-->

Prove the two Lost endings still end the run Lost, and that the rule holds with the demo off.

Split out of `HP-2` on 2026-08-19. `HP-2` settled 2026-08-20 with M78's `Returned`
close. This item cannot ride that run. A death and a Hatch breach each end the
run **Lost**, so neither can happen in a run that ends `Returned`, and a
`DemoActive false` run is a third configuration again. Three runs, not one —
which is a second sitting, and the grain rule says that is a second pass.

## Items

1. **TH-13** — M77 criterion 7: solo down and Hatch breach still end the run Lost exactly as
   before, and the rule holds with `DemoActive false`. The sim half is pinned by
   `DockedRunTests.SoloDown_AndHatchBreach_StillEndLost`; the demo-off case and the on-screen
   Lost path are not. Settles on one Hatch breach on screen, one unpayable death on screen,
   and one run with the demo off.

**The solo-death half narrowed on 2026-08-24 and the observation has to narrow with it.**
`M98`'s recut priced a respawn for every dead seat, solo included, so
`GameController.PlayerDied` now sends the body to the DOWN overlay whenever a respawn can
still be honored and only falls through to `Sim.WipeUnbankedSlot(0)` and
`Sim.NotifyPlayerDown()` when it cannot. A seat carrying at least `Respawns.BaseCost` —
**150** for a run's first — no longer ends the run by dying, and a solo death watched with
a full pile will show the overlay rather than the Lost screen. The observation that settles
this half is a death the seat cannot pay for: carry under `RunSim.NextRespawnCost`, log line
`respawn refused — not enough unbanked`, then the Lost screen.

The pinned test is unaffected and does not cover the new gate. It calls
`RunSim.NotifyPlayerDown` directly, which sits downstream of the DOWN check, so it proves
the sim still ends Lost when told the player is down and says nothing about whether the
Game layer will tell it. The Hatch-breach half is untouched: a breach ends the run with no
respawn offered.

Whether the item should be re-scoped to the unpayable death alone, or widened to cover the
payable one as a third configuration, is Zo's call. It stays open either way.

## Not this pass

- The `Returned` dock round trip — [`HP-2`](../2.%20settled/HP-2-the-dock-round-trip.md),
  settled 2026-08-20 with M78.
- M78's own criteria — closed 2026-08-20.
