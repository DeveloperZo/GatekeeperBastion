# HP-2 — The dock round trip

<!--
Status: open
Closable by: Zo at the keyboard, one run
Update trigger: when an item is added, settled or evicted
Last updated: 2026-08-19
-->

Survive a bank window, dock, spend, re-embark through the open door, and read the log.
**M78's criterion 9 already schedules this run**, so this pass is settled by a playthrough
that has to happen anyway rather than by a separate sitting.

M77 closed on a capsule-to-first-door playthrough that stopped before the dock, so
everything below sits past the point that run reached.

## Items

1. **TH-17** — M77 criterion 1: `EndRun("docked")` lands phase `Returned` with no
   `PodEjections++`, no Lost screen, and the end screen names the Docked cause. The sim half
   is covered by `DockedRunTests`; the screen is not. Settles on one dock, reading the end
   screen and the `[Run] DOCKED` line. *(Was TH-9, renumbered — id collided with M76's set.)*
2. **TH-18** — M77 criterion 4: `[hold E] Dock — return to Ship` is live at the Hatch after
   a survived window and gone once the next Wave is triggered. Invisible to the offline
   harness. Settles by reading the prompt, triggering the next Wave, confirming held E then
   does nothing. *(Was TH-10.)*
3. **TH-19** — M77 criterion 5: the successful-bank snapshot persists banked and door state,
   and `[Demo] snapshot restored doors=<…>` fires with the door open on re-embark. Touches
   the save file. Settles by docking, spending, re-embarking, reading the line and walking
   through. *(Was TH-11.)*
4. **TH-20** — M77 criterion 6: `demo.door_d1` then `demo.dock_option` play in that order on
   the survival frame. Pinned offline in `DemoVoiceTests`; never heard live. Settles on one
   survived window with the sound on. *(Was TH-12.)*
5. **TH-14** — M77 criterion 8: the full playthrough, survive, dock, spend, re-embark. This
   is the item the other six fall out of.
6. **TH-10** — M76 criterion 3: `DemoZoneDeepestWave[1]` written on each field-Wave clear,
   and the bank does not write it. Code landed with M78; the write has not been seen in a
   play log. Settles on the same log read.
7. **TH-12** — M76 criteria 6–7: first Docked plays `demo.first_hatch_down`, plus the
   remaining beat 1–2 voice and `zone terminals complete zone=1`. Code landed with M78;
   never heard live. Settles on the same log read against beat 1–2's Driven/Shown/Reads-back.

## Not this pass

- M78's own criteria 0–9 — those are M78's close gate, not owed items, and the same run
  observes them.
- The two Lost endings and the demo-off run (TH-13) — split to
  [`HP-4`](HP-4-the-lost-endings-hold.md) on 2026-08-19. A death and a Hatch breach both end
  the run `Lost`, so neither can be observed in the `Returned` run this pass rides.
- The juice teach and the `first-hatch survived` line — evicted, see
  [the register](../README.md#evicted-with-the-reason).
