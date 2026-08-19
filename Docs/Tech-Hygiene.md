# Tech Hygiene

<!--
Purpose: Standing ledger of small owed items that are real but do not justify a milestone: acceptance criteria a closed milestone never separately observed, one-shot artefacts left behind, and cleanups only the developer can perform.
Status: living tracker
Authority: none over design or code. A row here records a debt and what would settle it; it does not gate anything.
Update trigger: when a milestone closes with criteria unobserved, when a one-shot artefact is left in the tree, or when a row is settled.
Last updated: 2026-08-19
-->

Things the project owes itself that are too small to be a milestone. A row is
written when something real was left undone and would otherwise be forgotten:
an acceptance criterion that was never separately observed, a scratch file that
outlived its run, a cleanup that needs a hand the tooling does not have.

**Anything with a real gate belongs in a milestone instead.** If settling it
changes what a player sees, or needs a decision made, or is large enough to
have its own acceptance criteria, it is a milestone and this file is the wrong
place. This ledger is for the residue.

Nothing here is a claim that something is broken, and nothing here is a claim
that something works. A row says only that it was not observed.

**Authority for settling any row that touches a MonoBehaviour, a scene or the
save file is the AutoVerify file bridge**: write `gkb_verify_request.txt` at the
project root with the editor open and read `gkb_verify_report.json`. The offline
harness `Tools/verify/run.sh` cannot see those, so a green harness run does not
settle those rows (`Working-Rules.md`, Verification).

## Open

| id | Item | Why it is owed | What would close it | Added |
|---|---|---|---|---|
| TH-1 | M75 criterion 2: HP floor on the Ship while `DemoActive`, overdamage leaves 1 HP and never calls `Die()` | M75 closed 2026-08-18 on Zo's ruling after he played beat 0 end to end through to the Bastion. That playthrough is criterion 1. Criterion 2 was not separately observed or reported at close. | One AutoVerify run over the beat 0 suites, which would also settle TH-2, TH-3 and TH-4. Alternatively a play observation of the Ship taking lethal damage while `DemoActive`. | 2026-08-18 |
| TH-2 | M75 criterion 3: early EMBARK refuses with the right string and log for each of the three reasons (terminal not started / no weapon / intruders alive) | Not separately observed or reported when M75 closed. | The same AutoVerify run as TH-1, or one focused play pass that attempts EMBARK in all three states and reads the log after. | 2026-08-18 |
| TH-3 | M75 criterion 4: F10/F11 and lobby entry refuse with `[Demo] multiplayer locked - buy an upgrade first` while `DemoActive` and `DemoWheelBuysLeft == 2` | Not separately observed or reported when M75 closed. | The same AutoVerify run as TH-1, or the same play pass as TH-2 with F10, F11 and lobby entry all tried before the first wheel buy. | 2026-08-18 |
| TH-4 | M75 criterion 5: `SaveVersion` 9 round-trips, a fresh save carries the README defaults, and a pre-v9 save with `CleanExits >= 1` loads `DemoActive == false` with beat 0 a no-op on it | Not separately observed or reported when M75 closed. Touches the save file, so the harness cannot answer it. | The same AutoVerify run as TH-1, with a pre-v9 save present as a fixture alongside a fresh one. | 2026-08-18 |
| TH-5 | M75 criterion 6: the reload matrix from beat 0's Reload table, quit after unlock reloads on deck with no reseal, quit after T unarmed replays `demo.intruders` with the release gated again, armed skips the line and releases immediately | Not separately observed or reported when M75 closed. Three quit and reload cases, none of them covered by the criterion 1 playthrough. | One sitting that quits and reloads at each of the three points in turn. Not reachable from the offline harness. | 2026-08-18 |
| TH-6 | M75 criterion 7: every reads-back log in beat 0's Driven/Shown/Reads-back list fires, and no `[Dev]` marker appears on any of those paths | Not separately observed or reported when M75 closed. The playthrough happened but its log was not read line by line against the list. | Reading the log from whichever play pass settles TH-2 and TH-3, end to end against beat 0's Driven/Shown/Reads-back list. | 2026-08-18 |
| TH-7 | Delete `_to_delete/m75_scratch/`, holding `RelocateDemoCapsule.cs` and its `.meta` | The one-shot scratch script had already been run, and the verify gate fails while `Assets/_Scratch/` holds anything but `.gitkeep`, so it was moved out of `Assets/_Scratch/` to `_to_delete/m75_scratch/` at the repo root on 2026-08-18. The tooling available could move it but not delete it. | Zo deletes the folder. | 2026-08-18 |
| TH-9 | M76 juice teach: first Field look with Unbanked > 0 plays `demo.juice_teach`; one `+` spends 100 and the next Wave starts `intensity=1` with an Event log line | Deferred at M76 close (Zo, 2026-08-18) — not a close gate. Line id exists in `DemoLines`; no Game `DemoVoice.Play(JuiceTeach)`; spend path still silent for Events. | Wire teach + Event on spend, or retire the Demo beat-1 juice teach row in an edit that says so. | 2026-08-18 |
| TH-10 | M76 criterion 3: `DemoZoneDeepestWave[1]` written on each field-Wave clear; bank does not write it | `MetaState.SetZoneDeepest` exists; Game never calls it on field clear (`RecordPendingTerminalClear` only logs Terminal). | Call `SetZoneDeepest` from the field-clear path; EditMode or play log shows zone 1 deepest. | 2026-08-18 |
| TH-11 | M76 criterion 5: `[Demo] first-hatch survived` on survival of the authored loss-1 window | String not in Scripts. Survive of this window is a tuning defect per Demo beat 2. | Log the line when the no-upgrade seat clears the first Hatch-bank; or restamp the defect rule. | 2026-08-18 |
| TH-12 | M76 criteria 6–7: first Docked after loss plays `demo.first_hatch_down`; remaining beat-1/2 voice + `zone terminals complete zone=1` | `FirstHatchDown` copy + same-frame win exist in `DemoVoice`; no Game play on first Docked. `first field … waves=1` ships; `zone terminals complete` not found. | Wire Docked trigger + remaining Demo logs; one log read against beat 1–2 Driven/Shown/Reads-back. | 2026-08-18 |
| TH-9 | M77 criterion 1: `EndRun("docked")` lands phase Returned with no `PodEjections++` and no Lost screen, and the end screen names the Docked cause | M77 closed 2026-08-19 on a capsule-to-first-door playthrough. That run never docked, so no Docked end screen has been seen. The sim half is covered by `DockedRunTests`; the screen is not. | One dock at the Hatch, reading the end screen and the `[Run] DOCKED` log line. | 2026-08-19 |
| TH-10 | M77 criterion 4: `[hold E] Dock — return to Ship` is live at the Hatch after a survived window and gone once the next Wave is triggered | Not observed. The prompt is a `RepairInteractable` branch, invisible to the offline harness. | Survive a window, read the prompt, trigger the next Wave, confirm held E then does nothing. | 2026-08-19 |
| TH-11 | M77 criterion 5: the successful-bank snapshot persists banked + door state, and `[Demo] snapshot restored doors=<…>` fires with the door open on re-embark | Not observed. Touches the save file, which the harness cannot see. The v10 JSON round trip is covered; the restore-into-a-live-scene path is not. | Dock, spend, re-embark, read the log line and walk through the door. | 2026-08-19 |
| TH-12 | M77 criterion 6: `demo.door_d1` (Lock) then `demo.dock_option` (Subtitle) play in that order on the survival frame | The ordering rule is pinned offline in `DemoVoiceTests`, but nobody has heard the pair fire in a live survival. | One survived window with the sound on. | 2026-08-19 |
| TH-13 | M77 criterion 7: solo down and Hatch breach still end the run Lost exactly as before, and the rule holds with `DemoActive false` | The sim half is pinned by `DockedRunTests.SoloDown_AndHatchBreach_StillEndLost`; the demo-off case and the on-screen Lost path are not. | One death and one breach on screen, plus one run with the demo off. | 2026-08-19 |
| TH-14 | M77 criterion 8: the full playthrough — survive a window, dock, spend, re-embark through the open door | The closing playthrough stopped at the door opening. This is the criterion that would close TH-9 through TH-12 in one sitting. | One run carried through the dock and back out. | 2026-08-19 |

## Closed and removed

| id | Item | Where it went | Removed |
|---|---|---|---|
| TH-15 | `Packs/Gatekeepers/Default.mat` approximated the UtilityBot rather than reproducing it | Moot 2026-08-19: the approximation is gone. The pack's Shader Graph had been stripped of its Built-In target, not shipped without one; restoring it from the `.unitypackage` returned every vendor material to its authored form and the kind materials are copies of them. See **M88**. | 2026-08-19 |
| TH-16 | Every FaxLab3D robot magenta, and `Assets/FaxLab3D/` gitignored so nothing survived a re-import | Settled 2026-08-19 by restoring the Shader Graph from the package. `Tools/lint-vendor/run.sh` now fails `verify` if that graph loses its Built-In target, and the pack's `Materials/` and `Textures/*.meta` are tracked so an overwrite shows as a diff. See **M88**. | 2026-08-19 |
| TH-8 | Delete `Assets/_Scratch/Editor/` (the folder, not just `SwapWeaponMeshTool.cs`) | Settled 2026-08-19 — Zo deleted `Assets/_Scratch/Editor/`. `Tools/verify/run.sh` gets past the scratch gate and compiles again; first full run after it was 462 passed / 8 failed, the 8 all pre-existing per `gkb_verify_report.json` of 2026-08-18. | 2026-08-19 |
