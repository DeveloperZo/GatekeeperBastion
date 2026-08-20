# HP-1 — Beat zero, observed

<!--
Status: open
Closable by: the AutoVerify file bridge (with a pre-v9 save staged as a fixture),
plus one focused play pass for the refuse strings and three quit-and-reload cycles
Update trigger: when an item is added, settled or evicted
Last updated: 2026-08-19
-->

One AutoVerify run over the beat 0 suites, then one play pass that attempts EMBARK in each
refused state and reads the log. M75 closed on Zo's beat 0 playthrough, which observed
criterion 1; these are the five that were never separately seen.

## Items

1. **TH-1** — M75 criterion 2: HP floor on the Ship while `DemoActive`, overdamage leaves
   1 HP and never calls `Die()`. Settles on the AutoVerify run, or on a play observation of
   the Ship taking lethal damage while `DemoActive`.
2. **TH-2** — M75 criterion 3: early EMBARK refuses with the right string and log for each
   of the three reasons (terminal not started, no weapon, intruders alive). Settles on one
   play pass attempting EMBARK in all three states, log read after.
3. **TH-3** — M75 criterion 4: F10/F11 and lobby entry refuse with `[Demo] multiplayer
   locked - buy an upgrade first` while `DemoActive` and `DemoWheelBuysLeft == 2`. Settles
   on the same play pass, all three tried before the first wheel buy.
4. **TH-4** — M75 criterion 5: `SaveVersion` 9 round-trips, a fresh save carries the README
   defaults, and a pre-v9 save with `CleanExits >= 1` loads `DemoActive == false`. Touches
   the save file, so the harness cannot answer it. Settles on the AutoVerify run with a
   pre-v9 save present as a fixture alongside a fresh one.
5. **TH-5** — M75 criterion 6: the reload matrix from beat 0's Reload table, all three quit
   and reload cases. Settles on one sitting that quits and reloads at each point in turn.
   Not reachable from the offline harness.
6. **TH-6** — M75 criterion 7: every reads-back log in beat 0's Driven/Shown/Reads-back list
   fires, and no `[Dev]` marker appears on those paths. Settles by reading the log from the
   pass that settles TH-2 and TH-3, end to end against the list.

## Not this pass

- Anything past the dock — `HP-2`.
- Deleting `_to_delete/m75_scratch/` — `HP-3`.
