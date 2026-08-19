# Tech Hygiene

<!--
Purpose: Standing ledger of small owed items that are real but do not justify a milestone: acceptance criteria a closed milestone never separately observed, one-shot artefacts left behind, and cleanups only the developer can perform.
Status: living tracker
Authority: none over design or code. A row here records a debt and what would settle it; it does not gate anything.
Update trigger: when a milestone closes with criteria unobserved, when a one-shot artefact is left in the tree, or when a row is settled.
Last updated: 2026-08-18
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
| TH-8 | Delete `Assets/_Scratch/Editor/` (the folder, not just `SwapWeaponMeshTool.cs`) | The verify gate fails while `Assets/_Scratch/` holds anything but `.gitkeep` and `*.meta`, and the directory entry itself counts, so `Tools/verify/run.sh` exits 1 on the scratch gate before it compiles anything. The one-shot weapon-mesh swap it holds has already run and says in its own header to delete it. The Cowork bridge cannot delete files on this disk, only move them. | Zo deletes `Assets/_Scratch/Editor/`. The stray `Editor.meta` left behind at the top level is tolerated by the gate. | 2026-08-19 |

## Closed and removed

None yet.
