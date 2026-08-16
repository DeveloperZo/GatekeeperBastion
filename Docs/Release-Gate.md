# Release Gate

<!--
Update trigger: when an M64 gate item is added or retired, or when a friends session finds a failure the gate did not catch
Last updated: 2026-08-16
-->

Friends Alpha send checklist. Red blocks the send. Amber is recorded and sent anyway.
Machine red items: `Tools/release-gate/run.sh` (also from `Tools/verify/run.sh`).
Human items: eyes and a second machine. Send copy: `Release-Send.md`.

| # | Colour | Check | Player sees when it fails | Who |
|---|---|---|---|---|
| 4 | red | Editor patched for CVE-2025-59489 (or Application Patcher on output) | Defender blocks / quarantines the build | human (+ version note in run.sh) |
| 5 | red | Dev / Autoconnect Profiler / Deep Profiling / Script Debugging all off | Heavy build, profiler prompts, cheats reachable | machine |
| 6 | red | Company / Product / Version set; not `DefaultCompany` | Wrong save/log folder; no version to identify the zip | machine |
| 7 | red | `Application.version` on main menu and in zip name | Friend cannot say which build they have | human |
| 8 | red | Fullscreen Window; native res; resizable; allow fullscreen switch | Stuck exclusive fullscreen on the wrong monitor | machine |
| 9 | red | Run In Background on; cursor unlocks on focus loss | Alt-tab freezes the session for the other seat | machine (setting) / human (cursor) |
| 10 | red | Auto Graphics API, or D3D11 first | Black screen / no launch on iGPU laptops | machine |
| 11 | red | `targetFrameRate` 60 and `vSyncCount` 1 outside fight | Menu cooks a laptop | machine (`targetFrameRate` / `OutsideFightTarget`) |
| 12 | red | Dev keys / cheats / debug overlays behind `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` | Friend presses F-keys and mutates the run | machine |
| 13 | red | Extracted and run on a non-build machine; LocalLow + HKCU cleared first | "Doesn't launch" — exe run from inside the zip | human |
| 14 | red | Esc → Quit from every scene | Friend trapped mid-fight with no exit | human |
| 15 | red | Failed join: reason + timeout ≤ 10 s | Spinner forever; evening spent guessing | human |
| 16 | red | Audio on another machine, including device change after launch | Silent build; no way to know fight SFX landed | human |
| 17 | red | `run-windowed.bat` beside the exe | No escape hatch for bad display/GPU | human (file ships; machine can prove template exists) |
| 18 | red | Dual-box chrome: normal window (no `-popupwindow`) | Eyes-on session has no title bar / min-max | human |
| 19 | red | Host and Join: shared Ship; party lead EMBARK | No co-presence; guest local embark; hang | human |
| 20 | red | ARMORY change on that Ship is the gun in Bastion (both seats) | Wrong loadout; CREW/FAB feel broken | human |
| 21 | red | Client prep verbs once, server-side (`E` / `+/-` / `T`) | Client cannot start a Wave or juice | human (+ PlayMode) |
| 22 | red | M51 verbs in a player build (repair, decoy, ability) | Client can move/shoot only | human |
| 23 | red | Player-build fight: kill, HUD, muzzle, fire SFX both seats | Connect-only session; silent guns | human |
| 24 | red | End screen both seats; clean leave; session restartable | One seat stuck; cannot rematch | human |
| 25 | red | Full player-action loop host then client (swap seats); no `[Dev]` | Partial evening; tainted close | human |
| 26 | amber | SmartScreen warns (unsigned) | More info → Run anyway | recorded |
| 27 | amber | AV may quarantine download | Missing file, not a warning | recorded (VirusTotal before send) |
| 28 | amber | One-page send message with the build | Friend improvises install and firewall | `Release-Send.md` |
