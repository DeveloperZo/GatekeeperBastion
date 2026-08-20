# 06 — Milestones & Roadmap

<!--
Purpose: Summary and strategy layer over Docs/Milestones/. Order, status, risks, and the rules that govern milestone files. Detail belongs in the milestone file, never here.
Status: working
Update trigger: when a milestone changes status, when a gate is rewritten, or when a Brainstorm item graduates.
Last updated: 2026-08-20
-->

There is exactly one roadmap and this is it. It is not branched, shadowed, or
split into a separate history file — **shipped milestones stay in the table.**

**This file summarises; `Milestones/` explains.** One row and one paragraph per
milestone here; everything else — the gate's reasoning, the leverage analysis, the
open questions, the traps — lives in the milestone's own file. If a section here
grows past a paragraph, it belongs in `Milestones/`.

No point estimates and no dates. The previous project's roadmap tracked 316
estimate points against a ~22 pts/month velocity, and its postmortem named that
bookkeeping as procrastination. Order is the only scheduling signal here.
**Dates are stamped on completion only, never projected forward.**

**Every milestone's gate is its numbered `## Acceptance criteria`, and every criterion
has to be checkable from the log, the test harness, or a number in `Design/4-Data.md`**
(`Working-Rules.md`, Evidence). "Feels better" does not close a milestone. A milestone with
a vague gate is not ready to start. The `Done when` line this file used to require was
replaced by the criteria list on 2026-08-04 (`_meta/_definitions/milestone.md`); shipped
files written before that date still carry one and are a record.

## Current

**M79** completed 2026-08-20 (built, realigned and closed the same day; criterion
10 closed under the dev-tainted allowance ruled that day — criterion 9's
composition bet is open and needs a logged Run). **M80** is now startable: both
its owner-Zo inputs were stamped 2026-08-20 — one high-HP elite, no escorts, and
D3 stays exactly as authored (zero code).
**M78** completed 2026-08-20 (Zo close; criterion 9 `Lost` +
`Returned` playthroughs). **M88** completed 2026-08-19 (Zo close). **M81** and **M87**
completed 2026-08-19 (Zo close). **M76** completed 2026-08-18 (Zo close; juice
teach deferred — TH-9, evicted 2026-08-19; survive-log TH-11 evicted;
zone-deepest TH-10 and Docked voice TH-12 settled 2026-08-20 in
[`HP-2`](Hygiene/2.%20settled/HP-2-the-dock-round-trip.md)).
**M75** closed 2026-08-18 on Zo's beat 0 playthrough through to the Bastion;
criteria 2 to 7 were not separately observed and are owed as TH-1 to TH-6 in
[`HP-1`](Hygiene/1.%20open/HP-1-beat-zero-observed.md). Survive path (door unlock
on bank success, Ship return) is **M77** (built; criteria 2 and 3 stamped at
close; TH-14 and TH-17–TH-20 settled 2026-08-20 in
[`HP-2`](Hygiene/2.%20settled/HP-2-the-dock-round-trip.md); TH-13 remains in
[`HP-4`](Hygiene/1.%20open/HP-4-the-lost-endings-hold.md)).
**M74** closed 2026-08-17 (demo voice pipe). **M72** and **M73** completed
2026-08-17.
**M47, M48, M49, M50, M51, M52, M53, M54** completed 2026-08-13. **M61** and **M61-2**
completed 2026-08-14. **M55, M57, M58, M59, M60, M62, M63-2, M64, M65, M71** completed
2026-08-15. **M69** and **M70** completed 2026-08-17.

**Friends alpha gate** (Zo, 2026-08-13): two friends install a build, join without the
developer touching a keyboard, play a full five-wave run with both participating, and the
run ends with a screen that tells them what happened. Queue is ordered against that
sentence. Shared Ship + party EMBARK closed with **M72** (2026-08-17); remaining friends
work is polish and dry-run length, not lobby topology.

**Demo family cut 2026-08-17**: M74–M80 from `Demo/`'s beat specs. **M80** is
cut but not started-able until Zo stamps the boss/elite composition and the
finale-access ruling — the file names both.

**Recommended next**, in this order.

1. **HP-4 — The lost endings hold** — Zo, two Lost runs plus one demo-off run
   ([`HP-4`](Hygiene/1.%20open/HP-4-the-lost-endings-hold.md), TH-13). M77's remaining play stamp.
2. **M66** — hit confirm, per-kind tells, distinct players at lane range.
3. **Play close-outs** still open on completed files: M32 criterion 7, M26 Path 2, M31 pin
   playthrough, M47 criterion 18 wave-report, M61-2 criteria 4/13/14, M55 juice unused,
   M71 Regen Surge mag, M64 criteria 13–25 dry-run, M69 criterion 4, M73 criterion 6.
   Next-Run list: `GatekeeperZo Notes/verification.md`.

Planned count is **10** (at cap 10). Active **0** (cap 3).

## Order

Status maps 1:1 to the folder the file sits in, so a status change is a file move.
**The `#` is a stable ID, not a position** — rows are listed in execution order and
reordering is a row move, never a renumber.

**Every row below is the same grain**: one player-visible change, specified well
enough to build, with criteria that can only be true or false
(`_meta/_definitions/milestone.md`). Work that is wanted but cannot yet be cut that
way is in **Brainstorm** and has no file. That rule is new, and it emptied most of
this table — see the change log.

| # | Milestone | Status | Gate in one line | File |
|---|---|---|---|---|
| M0 | Playable v0 loop | completed, Aug 2026 | Splash→Ship→Bastion→Ship runs end-to-end solo | [`3. completed/M0`](Milestones/3.%20completed/M0-playable-v0-loop.md) |
| M10 | Wave 2 is winnable | completed, Aug 2026 | A logged Run at dial 0 clears Wave 2 with Hatch >25% | [`3. completed/M10`](Milestones/3.%20completed/M10-wave-2-is-winnable.md) |
| M11 | The weapon is the build | completed, Aug 2026 | Four weapons, contextual ARMORY, one Tier >20% dps | [`3. completed/M11`](Milestones/3.%20completed/M11-the-weapon-is-the-build.md) |
| M2 | Wave 3 is winnable | completed, Aug 2026 | A logged Run clears Wave 3 with both Rams killed | [`3. completed/M2`](Milestones/3.%20completed/M2-wave-3-is-winnable.md) |
| M13 | Dev mode | completed, Aug 2026 | F1 overlay; every action a `GameController` command; compiles out of release | [`3. completed/M13`](Milestones/3.%20completed/M13-dev-mode.md) |
| M17 | Verification you can trust | completed, Aug 2026 | Failure reports name a cause; a test run leaves the save byte-identical | [`3. completed/M17`](Milestones/3.%20completed/M17-verification-you-can-trust.md) |
| M15 | Shortcut panels | completed, Aug 2026 | On-screen key panel incl. dev keys; no key double-bound | [`3. completed/M15`](Milestones/3.%20completed/M15-shortcut-panels.md) |
| M16 | A difficulty curve | completed, Aug 2026 | HP slope moved and pinned; the ±15 play band is retired, not pending — see the file's correction | [`3. completed/M16`](Milestones/3.%20completed/M16-a-difficulty-curve.md) |
| M19 | Repair costs Salvage | completed, Aug 2026 | 2 Salvage/HP charged as applied; stops at zero bank | [`3. completed/M19`](Milestones/3.%20completed/M19-repair-costs-salvage.md) |
| M14 | Five Waves, and you choose how deep to start | completed, Aug 2026 | 5 Waves; starting at N delivers 1..N back to back | [`3. completed/M14`](Milestones/3.%20completed/M14-five-waves-and-a-deep-start.md) |
| M20 | The fifth attribute: every weapon does something | completed, Aug 2026 | Every shop weapon has an ability on a key; >=5 gatekeeper-seconds/Wave at full focus | [`3. completed/M20`](Milestones/3.%20completed/M20-the-fifth-attribute.md) |
| M21 | The upgrade wheel | completed, Aug 2026 | Focus pays 5/3/3/0/0 by ring distance; opposite focuses differ >=2x in burst or window | [`3. completed/M21`](Milestones/3.%20completed/M21-the-upgrade-wheel.md) |
| M22 | The game stops misreporting itself | completed, Aug 2026 | Run tally equals Salvage banked to the unit; gross/repair/net shown separately; Dev-taint visible on screen | [`3. completed/M22`](Milestones/3.%20completed/M22-the-game-stops-lying.md) |
| M23 | The repair rig is equipment you bring | completed, Aug 2026 | Equipment slot unlocks at first loss and is bought; repair refused without the rig; Repair Rig + Engineering tracks deleted | [`3. completed/M23`](Milestones/3.%20completed/M23-the-repair-rig-is-a-tool.md) |
| M24 | Settings that reach the fight | completed, Aug 2026 | One screen from menu, Ship and Bastion; store in `Sim/`; sensitivity migrated and live mid-Run | [`3. completed/M24`](Milestones/3.%20completed/M24-settings-that-reach-the-fight.md) |
| M25 | The reload you are about to lose | completed, Aug 2026 | Warning before the swap, discard logged, setting defaults to today's behaviour | [`3. completed/M25`](Milestones/3.%20completed/M25-the-reload-you-are-about-to-lose.md) |
| M26 | A second sidearm, and a loadout that has a shape | completed, Aug 2026 | `SEC-D` in the catalog; Path 1 shipped; Path 2 / ability / ring still open on the file | [`3. completed/M26`](Milestones/3.%20completed/M26-a-second-sidearm.md) |
| M27 | The game says who you are | completed, Aug 2026 | Before the first embark the player is told the Bastion is held and the Hatch is their way home; `Gate` removed | [`3. completed/M27`](Milestones/3.%20completed/M27-the-game-says-who-you-are.md) |
| M28 | Vocabulary that cannot rot | completed, Aug 2026 | `Tools/lint-language/run.sh` fails on a retired term, on builder vocabulary inside `Docs/Play/`, and on an uncapitalised station name; runs from `verify` | [`3. completed/M28`](Milestones/3.%20completed/M28-vocabulary-that-cannot-rot.md) |
| M29 | How full the room got | completed, Aug 2026 | `Tools/wave-report` prints mean live; three log lines machine-readable; pin-file restore + playthrough open on file | [`3. completed/M29`](Milestones/3.%20completed/M29-how-full-the-room-got.md) |
| M30 | Something to pull them off the door | completed, Aug 2026 | Drone bay + decoy; Path 1 EditMode green; Path 2 / playthrough open on file | [`3. completed/M30`](Milestones/3.%20completed/M30-something-to-pull-them-off-the-door.md) |
| M31 | Range and depth both cost damage | completed, Aug 2026 | Pierced bodies take 0.26×; every Primary has a falloff start; T0 MR-35 stops clearing a Wave-1 line; **the Pin Shot fires** — playthrough 7/10 remain open on the file | [`3. completed/M31`](Milestones/3.%20completed/M31-range-and-depth-both-cost-damage.md) |
| M32 | A Primary that wants the far wall | completed, Aug 2026 | `PRI-C` in the catalog; full damage at 50 m; sustained within 5% of the MR-35; Railshot on `F` — playthrough 7 open on file | [`3. completed/M32`](Milestones/3.%20completed/M32-a-primary-that-wants-the-far-wall.md) |
| M33 | The numbers doc reads itself out of the build | completed, Aug 2026 | `Tools/docgen/run.sh --check` exits non-zero when a `Sim/` number changes and `Design/4-Data.md` is not regenerated; deleting an `UpgradeTrack` value drops its row from both docs with no generator edit | [`3. completed/M33`](Milestones/3.%20completed/M33-the-numbers-doc-reads-itself.md) |
| M34 | A file you can navigate | **cancelled, Aug 2026** | Region order stays in `Architecture/7-File-Layout.md` for new files; tree-wide sweep and `lint-layout` check dropped | [`5. cancelled/M34`](Milestones/5.%20cancelled/M34-a-file-you-can-navigate.md) |
| M35 | One look, drawn from one place | completed, Aug 2026 | Four authored scenes, one Shift theme, no UI generators; Splash→MainMenu→Ship→Bastion; stations drive path / HUD / pause / quit-to-menu; SceneBuilder owns environment/spawns/light/NavMesh only — station panel polish deferred | [`3. completed/M35`](Milestones/3.%20completed/M35-one-look-drawn-from-one-place.md) |
| M36 | The Hatch is always on | completed, Aug 2026 | `Hud Hatch` top-centre always-on in Tethered; Shift Health-row recipe; drawer Hatch bar gone; Decisions table row; Zo playtest closed readability / emphasis / bay call-out | [`3. completed/M36`](Milestones/3.%20completed/M36-the-hatch-is-always-on.md) |
| M37 | Where they die | completed, Aug 2026 | Every kill line carries `hatchDist=` and `spawnDist=` as raw metres, no formula in the game; `wave-report` prints median distance per Wave **per Gatekeeper kind** | [`3. completed/M37`](Milestones/3.%20completed/M37-where-they-die.md) |
| M38 | A dashboard you can open | completed, Aug 2026 | `Docs/dashboard.html` parses logs in-browser after you choose Logs/; PlayMode by `stage=playmode`; no `legacy` spend; era curves | [`3. completed/M38`](Milestones/3.%20completed/M38-a-dashboard-you-can-open.md) |
| M39 | The seam becomes a boundary | **cancelled, Aug 2026** | Premised on `noEngineReferences: true`, which is load-bearing only for deterministic lockstep. Replaced by M45 | [`5. cancelled/M39`](Milestones/5.%20cancelled/M39-the-seam-becomes-a-boundary.md) |
| M40 | The rules become measurable | completed, Aug 2026 | One statement of the counted-line unit, linked from everywhere else; `Editor/` has a number; the two grandfathered singletons named by type; `5-Audit` corrected; **no file under `Assets/` edited** | [`3. completed/M40`](Milestones/3.%20completed/M40-the-rules-become-measurable.md) |
| M41 | The rules get a check | completed, Aug 2026 | `Tools/lint-arch/run.sh` reads fenced rules from `Docs/Architecture/`, runs from `verify`, holds no literal from a fence and exits 2 on a deleted one; every row reproduces its measured baseline **and** catches a seeded violation | [`3. completed/M41`](Milestones/3.%20completed/M41-the-rules-get-a-check.md) |
| M42 | One mechanism per job | completed, Aug 2026 | One save-migration path; falloff/repair/slot-select one copy; cut APIs gone; kill lines quote banked amount; `LossReport`→`RunReport`; 43/43 R5 labels; one `Game/` namespace; harness 279 / Unity 311+23 green | [`3. completed/M42`](Milestones/3.%20completed/M42-one-mechanism-per-job.md) |
| M43 | A repository that reads as clean | **cancelled, Aug 2026** | Premises gone: package phantoms cleared, LFS already tracking binaries, history is 21 commits — orphan rewrite not warranted; root `gkb_*` junk is a chore | [`5. cancelled/M43`](Milestones/5.%20cancelled/M43-a-repository-that-reads-as-clean.md) |
| M44 | Skills that pay for their tokens | completed, Aug 2026 | Five skills at `.claude/skills/`, portable frontmatter, bodies under 500 lines; **trigger gate 50/50** (`run.sh trigger`); `AGENTS.md` under 200 lines; always-on token count recorded before and after | [`3. completed/M44`](Milestones/3.%20completed/M44-skills-that-pay-for-their-tokens.md) |
| M45 | Two Runs in one process | completed, Aug 2026 | Two independent Run simulations advance in one process without touching each other, proved by an EditMode test that fails when a static is seeded back; no netcode library named anywhere in it | [`3. completed/M45`](Milestones/3.%20completed/M45-two-runs-in-one-process.md) |
| M46 | The panel is authored | **cancelled, Aug 2026** | Cancelled (Zo) — authored-panel rewrite not a roadmap slot; station chrome and copy are M59 | [`5. cancelled/M46`](Milestones/5.%20cancelled/M46-the-panel-is-authored.md) |
| M47 | The bastion gets height and angles | completed, Aug 2026 | Kit palette + authored `Bastion.unity`; H/S/T/D + Control Room last; freestyle BuildBastion removed; sightline/path criteria closed on author play (Zo); M37 median compare open | [`3. completed/M47`](Milestones/3.%20completed/M47-the-bastion-gets-height-and-angles.md) |
| M48 | The first networked kill | completed, Aug 2026 | Client joins over loopback (F10/F11, dev-only); shoots a server-spawned gatekeeper; damage applies once on the host; death on both screens; wave counter identical via `NetRunMirror`; kill loop closed headless (`NetKillTests` Passed, PlayMode 24/0, report 2026-08-13T12:49:08Z) | [`3. completed/M48`](Milestones/3.%20completed/M48-the-first-networked-kill.md) |
| M49 | The gatekeepers see the second player | completed, Aug 2026 | Avatar `PlayerHealth` + collider; nearer client hunted via existing `NearestAlive` with no enemy code changes; melee drops replicated HP; client down does not end host run; get-up is M57 not this slice; `NetTargetingTests`; solo unchanged. Closed Zo playtest 2026-08-13 | [`3. completed/M49`](Milestones/3.%20completed/M49-the-gatekeepers-see-the-second-player.md) |
| M50 | The client lives the run | completed, Aug 2026 | Mirror salvage/hatch/elapsed; client HUD `m:ss`; three `EndSummary` kinds as ClientRpc; `NetRunMirrorTests` Passed 2026-08-13T15:19:01Z. Playthrough 2–5 recut: one-wave loop and hatch invuln cannot prove them (M52/M53) | [`3. completed/M50`](Milestones/3.%20completed/M50-the-client-lives-the-run.md) |
| M57 | The downed player waits on the Ship | completed, Aug 2026 | Client down → additive Ship; embark in Hatch-bank Prep via NAV or pause; full HP at Hatch; host Die() still Lost; `NetDownWaitTests` 2/0. Criterion 8 MPPM is confirmation | [`3. completed/M57`](Milestones/3.%20completed/M57-the-downed-player-waits-on-the-ship.md) |
| M51 | The client's hands | completed, Aug 2026 | Client repair, decoy and world-acting abilities apply once, server-side; all five `NetVerbTests` Passed (`gkb_verify_report.json` 2026-08-13T18:09:48Z, PlayMode 31/0). Criterion 6 confirmation not the gate; hatch-under-attack waits on M53 | [`3. completed/M51`](Milestones/3.%20completed/M51-the-clients-hands.md) |
| M58 | A build two machines can run | completed, Aug 2026 | Windows player build; Main Menu Host `0.0.0.0:7777`; Join typed IPv4; dual-box connect + local Player; refusal reads back — player-build kill eyes-on deferred to M64 | [`3. completed/M58`](Milestones/3.%20completed/M58-a-build-two-machines-can-run.md) |
| M64 | A build you can hand to a friend | completed, Aug 2026 | Machine send gate; Host/Join → LoadShip + NetPrep. Solo-Ship human loop superseded by M72 after 2026-08-15 friends-path failure | [`3. completed/M64`](Milestones/3.%20completed/M64-a-build-you-can-hand-to-a-friend.md) |
| M72 | The party shares a Ship | completed, Aug 2026 | Shared Ship; party lead NAV; Ready+5s; party Bastion↔Ship. Gate: NetLobbySession 3/0 + eyes-on Relay `KPRMPR` log `13-24-37` | [`3. completed/M72`](Milestones/3.%20completed/M72-the-party-shares-a-ship.md) |
| M59 | Polish Pass 1 | completed, Aug 2026 | Field Scale 1; fight HUD ≥24; overflow; no EXP; station chrome+copy; player-build keys; pause shortcuts; end scrim α≥0.65. EditMode HudSeparation + M59PolishChrome + StationPurpose green (`gkb_verify_report.json` 2026-08-15T18:03:48Z) | [`3. completed/M59`](Milestones/3.%20completed/M59-the-polish-pass-1.md) |
| M61 | Polish Pass 2 | completed, Aug 2026 | IQ astronaut + Synty Space guns/desks (SKU 138857); Mixamo fire/reload stand-ins; stroke crosshair; RMB zoom | [`3. completed/M61`](Milestones/3.%20completed/M61-the-polish-pass-2.md) |
| M61-2 | Polish Pass 2, the Bastion | completed, Aug 2026 | Kit blocks are POLYGON Sci-Fi Space meshes; re-framed mid-pass to scaler-2 station rooms with functional sliding doors and a bulkhead Hatch; Generated/Authored split; suite 2026-08-15T00:09Z adds no reds of its own; eyes/Stats/solo-run rows open in verification.md | [`3. completed/M61-2`](Milestones/3.%20completed/M61-2-polish-pass-2-the-bastion.md) |
| M69 | The session has a voice | completed, Aug 2026 | Whispers on Ship/Bastion; fire/reload/footsteps on SFX; no store pack. Gate: `SessionMusicHostTests` 4/0. Criterion 4 slider playthrough open | [`3. completed/M69`](Milestones/3.%20completed/M69-the-session-has-a-voice.md) |
| M63 | The garrison is a roster | later | Twelve-kind roster + properties + draw rule recorded; Ram→Thief in code; `in_build` filter; door-time logged | [`1. planned/M63`](Milestones/1.%20planned/M63-the-garrison-a-roster-with-bodies.md) |
| M63-2 | The three that walk | completed, Aug 2026 | Default/Hunter/Thief (Ram) Synty meshes + Mixamo melee walk; player capsule mesh removed; Play pages Thief-named. Gate: GatekeeperPresentationTests; silhouette Zo-closed | [`3. completed/M63-2`](Milestones/3.%20completed/M63-2-the-three-that-walk.md) |
| M62 | The Compendium you can open | completed, Aug 2026 | Esc → Compendium; Game dimension + scroll nav; StreamingAssets copy; HUD hidden while open; `CompendiumFormTests` | [`3. completed/M62`](Milestones/3.%20completed/M62-the-compendium-you-can-open.md) |
| M60 | A join a friend can use | completed, Aug 2026 | Unity Relay join code from Main Menu; join-in-Tethered; clean disconnect; host-quit message; LAN IP fallback. Gate: NetSessionAddressTests + Zo eyes-on criterion 4 | [`3. completed/M60`](Milestones/3.%20completed/M60-a-join-a-friend-can-use.md) |
| M65 | Three players, and a fight that counts them | completed, Aug 2026 | Three supported, six cheap; waves scale by player count; salvage split to own wallets. Gate: `PlayerWalletTests`, `WavePlayerCountTests`, `NetThreePlayerTests` (cumulative BankCap; `RunSim.Wallets.cs`). Full suite still has unrelated reds | [`3. completed/M65`](Milestones/3.%20completed/M65-three-players-and-a-fight-that-counts-them.md) |
| M66 | Polish Pass 3 | later | Hit confirm, per-kind pre-attack audio, players distinct at lane range | [`1. planned/M66`](Milestones/1.%20planned/M66-polish-pass-3.md) |
| M73 | The spawn point is a room | completed, Aug 2026 | Eight hung closets; Enemies GateSlab; ControlW/E on Master. Sketch 18/0; BastionKitScene 1/0. Criterion 6 playthrough confirmation | [`3. completed/M73`](Milestones/3.%20completed/M73-the-spawn-point-is-a-room.md) |
| M52 | Waves start at a terminal | completed, Aug 2026 | Interact starts T1's five Boarder-hunt Waves; Hatch invulnerable; D1 on Nth clear; Wave 5 `Escaped`. Close log `14-46-59` (no `[Dev]`). AwardKill-to-bank is M53 | [`3. completed/M52`](Milestones/3.%20completed/M52-waves-start-at-a-terminal.md) |
| M53 | Salvage leaves through the Hatch | completed, Aug 2026 | Kills fill Unbanked; Hatch-bank 45 s drip/attrit/dump, 3 slots, fat pile ×1.25/×1.50. Close log `18-23-55` (Run clean). Stay-Prep after dump is EditMode. Harness 316/0 | [`3. completed/M53`](Milestones/3.%20completed/M53-salvage-leaves-through-the-hatch.md) |
| M54 | Master Control is the win | completed, Aug 2026 | Wave 5 no longer ends the Run; Master Control is 5 terminal then 5 Hatch-bank; leftover converts; finale spends no slot | [`3. completed/M54`](Milestones/3.%20completed/M54-master-control-is-the-win.md) |
| M55 | Pay for intensity | completed, Aug 2026 | `+/-` spend 100 unbanked per step; `E` select, `T` start; HUD says Intensity; siphon dial retired; same `TetherSiphon` curve | [`3. completed/M55`](Milestones/3.%20completed/M55-pay-for-intensity.md) |
| M70 | The Default Protocol | completed, Aug 2026 | CREW sells Default as a 0-salvage claim (Barrage `G`, Rush `H`); Coming Soon stubs; Protocol Line + Protocol Bar. Gate: BoarderProtocolTests 14/0; play log `09-43-30` Barrage/Rush, no `[Dev]` | [`3. completed/M70`](Milestones/3.%20completed/M70-the-default-protocol.md) |
| M67 | Playable from zero | **cancelled, Aug 2026** | Premise gone — M53 carry convert after Wave 1; M52+M53 compound gate no longer exists | [`5. cancelled/M67`](Milestones/5.%20cancelled/M67-playable-from-zero.md) |
| M71 | The sidearm joins the wheel | completed, Aug 2026 | MK-1 ring D–A–R–C–F (Reload / Damage); SEC-D ring F–A–R–C–D (FireRate / Reload), Ability spoke not purchasable; Secondary-slot cost 150 + 150×t; focused never-rival holds | [`3. completed/M71`](Milestones/3.%20completed/M71-the-sidearm-joins-the-wheel.md) |
| M74 | The demo speaks | completed, Aug 2026 | `DemoVoice` Lock/Subtitle runner + id registry + party RPC; `DemoVoiceView` on Ship and Bastion; sim never pauses; every line logs | [`3. completed/M74`](Milestones/3.%20completed/M74-the-demo-speaks.md) |
| M75 | Wake on a dead Ship | completed, Aug 2026 | Beat 0 end-to-end (capsule → NAV → ship terminal → MK-1 → fight → heal → EMBARK) + SaveVersion 9 flags and migration | [`3. completed/M75`](Milestones/3.%20completed/M75-wake-on-a-dead-ship.md) |
| M76 | The first cycle ends in a loss | completed, Aug 2026 | Zone 1 = 2×1-Wave terminals; first Hatch-bank loss 1. Juice teach deferred (Zo). Gate: FirstTerminalTests + Zo close; TH-10 / TH-12 settled 2026-08-20 in HP-2, TH-9 / TH-11 evicted | [`3. completed/M76`](Milestones/3.%20completed/M76-the-first-cycle-ends-in-a-loss.md) |
| M77 | The run can end Docked | completed, Aug 2026 (criteria 2 and 3 at close; 1, 4, 5, 6, 8 settled 2026-08-20 in HP-2; 7 as TH-13 in HP-4) | Doors key on bank-window survival (failed bank never unlocks); held interact at the Hatch → `EndRun("docked")`; snapshot persists banked + doors. Kill criteria on the dock/push-on decision | [`3. completed/M77`](Milestones/3.%20completed/M77-the-run-can-end-docked.md) |
| M78 | The first return | completed, Aug 2026 | ARMORY ladder rung 1 + MP unlock on first buy + state-keyed exit standing + CREW claim (re-keyed unlock) + plain EMBARK + composite retrigger with partial election. Closed 2026-08-20 (Zo; `Lost` + `Returned` playthroughs). HP-2 settled with the `Returned` half | [`3. completed/M78`](Milestones/3.%20completed/M78-after-the-first-loss.md) |
| M79 | This loss opens the Primary | completed, Aug 2026 | Zone 2 = 1 terminal × 3 consecutive Waves + a heavier bank; that bank falling writes `DemoZone2BankLost`, which sells the PRIMARY rows + 2 buys on their own counter; D2 on survival; slots retired. **The Primary rows were already open — the milestone had to ADD the gate.** Closed 2026-08-20 under the dev-tainted allowance; criterion 9's composition bet is open and needs a logged Run | [`3. completed/M79`](Milestones/3.%20completed/M79-the-loss-that-opens-the-primary.md) |
| M80 | One final window | later | Control Room copy + `M73` spawn wiring; `HatchWaves` 5 → 1 with the first boss/elite; `EndDemo` on Escaped only. Blocked on two owner-Zo inputs (boss composition; finale access) | [`1. planned/M80`](Milestones/1.%20planned/M80-one-final-window.md) |
| M81 | The Gatekeeper's swing | completed, Aug 2026 | One retargeted attack clip into the shared Gatekeeper controller — Attack stops playing PlayerIdle for all three kinds and ship intruders. Free-retarget probe accepted (Zo close 2026-08-19) | [`3. completed/M81`](Milestones/3.%20completed/M81-the-gatekeepers-swing.md) |
| M82 | The weapon in your hands | later | Shared viewmodel controller + four hand-keyed clips (idle/move/fire/reload) for all six weapons; reload speed-scales to each weapon's duration; resolves the two controller-less Animators in Bastion.unity | [`1. planned/M82`](Milestones/1.%20planned/M82-the-weapon-in-your-hands.md) |
| M83 | Crewmates fire on camera | later | Replicate fire/reload to non-owner avatars via the existing ClientRpc pattern — drives Boarder.controller's wired-but-unreachable Fire/Reload states; code only, clips already exist | [`1. planned/M83`](Milestones/1.%20planned/M83-crewmates-fire-on-camera.md) |
| M84 | Gatekeepers flinch when shot | later | One flinch clip on a masked upper-body layer of the shared controller, triggered from the existing `Damaged` event; movement and attacks uninterrupted; ShipIntruder gains the same subscription | [`1. planned/M84`](Milestones/1.%20planned/M84-gatekeepers-flinch-when-shot.md) |
| M85 | The collapse under the burst | later | Death clip under the unchanged kill-confirm burst; `Despawn` keeps ledger removal immediate and defers only the destroy tail, so a corpse never delays a wave clear; co-op carrier named as the open question | [`1. planned/M85`](Milestones/1.%20planned/M85-the-collapse-under-the-burst.md) |
| M86 | Downed crewmates look downed | later | `Dead` bool on Boarder.controller driven from replicated Hp; reversible on restore-from-zero (dev heal now, buy-back after M79); local first-person out of scope | [`1. planned/M86`](Milestones/1.%20planned/M86-downed-crewmates-look-downed.md) |
| M87 | A bought body in an hour | completed, Aug 2026 | Utility Bot on the pack's Built-In Shader Graph in the Ship; graduation test passed 2026-08-18; Zo close 2026-08-19 (criterion 6 elapsed time unrecorded) | [`3. completed/M87`](Milestones/3.%20completed/M87-a-bought-body-in-an-hour.md) |
| M88 | The Gatekeepers are machines | completed, Aug 2026 | One FaxLab3D robot per Gatekeeper kind — silhouette tells the kind, on the pack's Built-In Shader Graph. Zo close 2026-08-19 | [`3. completed/M88`](Milestones/3.%20completed/M88-the-gatekeepers-are-machines.md) |
| M89 | The Ship, from above | cancelled, Aug 2026 | Killed 2026-08-20 (Zo): first person is chosen, not inherited — the isometric probe never runs. Part of the rig had already landed with `ce7d2ba` (`Ship_Isometric.unity`, `IsometricCameraRig`, `WallRenderState`, `IsometricBodyPresentation`); it stays in the tree, disabled in Build Settings | [`5. cancelled/M89`](Milestones/5.%20cancelled/M89-the-ship-from-above.md) |
| M91 | You wake standing | active (code landed, scene pending) | Beat 0's wake bed becomes a teleporter — upright materialise on a pad inside a glass cylinder that retracts on hold-E. Removes three dated defects, chiefly the session-long `SetCapsuleTrigger(true)` that was never restored. Code landed 2026-08-20; the `Demo Teleporter` subtree and its four bindings are Zo's scene pass | [`2. active/M91`](Milestones/2.%20active/M91-you-wake-standing.md) |
| M90 | Buy back at a live window | later | A dead guest pays their own banked Salvage to re-enter while a bank window is live, rejoining at the Hatch. Split out of `M79` 2026-08-20. **Blocked on three owner-Zo rulings**: per-seat banked salvage does not exist (a guest's `Banked` is never reported to the host), what "live window" means against the existing free embark, and whether door state becomes networked | [`1. planned/M90`](Milestones/1.%20planned/M90-buy-back-at-a-live-window.md) |
| M92 | A bigger ship with three hatches | active | Ship hull grown on the SR-2 profile with a starboard hatch gallery; three hatch doors with stable ids, open state assigned by seat count (1 → middle, 2 → outer pair, 3 → all); look-at strings + `[Hatch] assigned` log; guest cap 3 → 2. The hull and the doors only — per-hatch HP and enemy routing are the co-op demo's. Started 2026-08-20 | [`2. active/M92`](Milestones/2.%20active/M92-a-bigger-ship-with-three-hatches.md) |

## Strategy

**Friends alpha.** The filter is a session a friend can operate without Zo narrating: two
friends install a build, join, play five Waves, both participate, and an end screen names
what happened. Until this date the filter was whether the design works when Zo plays.

M53 closed. Unbanked and Hatch-bank are in the loop. M57 closed: a downed client waits
on the Ship and embarks in Hatch-bank Prep. **M58** closed 2026-08-15: player-build Host/Join
connect with local Player. **M64** closed 2026-08-15: send gate machine checks + both-seat
prep path (own Ship → Bastion, `NetPrep`, mirrored Intensity); criteria 13–25 playthrough
confirmation. **M59** closed 2026-08-15: Bastion as a room and Ship as something a stranger can
operate (terminals, HUD, station chrome and copy, keys, pause list, end scrim).
**M69** closed 2026-08-17: Bastion Whispers loop and fire/footstep audio; no store pack;
criterion 4 slider confirmation still open.
**M70** closed 2026-08-17: CREW Default claim, Barrage `G` / Rush `H`, Protocol Line +
Protocol Bar. Kill criteria wait 10 Runs or 2026-09-30.
**M72** closed 2026-08-17: shared Ship over Relay (`13-24-37`, joinCode KPRMPR,
playerCount=2, Ready gate, 5 s countdown, party return). Five-wave friends-alpha
is still the sentence above; this file closed the lobby kernel.
**M61** closed 2026-08-14: gun, sight, zoom, Ship desks. **M63-2** closed 2026-08-15:
the three in-build Gatekeeper bodies from that same pack. **M63** is the roster record,
Ram→Thief in code, and door-time. **M62** closed 2026-08-15: Compendium from pause.
**M65** closed 2026-08-15: three players, wave scale, split wallets. **M60** closed
2026-08-15: Unity Relay join code (M64 machine gate closed; dry-run confirmation still
open). Starting bastion (M47 kit) is the demo floor.

M55 closed. The FORGE/turret slice remains the field-loop rest; it stays queued
after the session is possible.

Critical path: `M0 ✅ → M10 ✅ → M13 → M11 → M2 → M15 → M16 → M14`.

**M10 shipped and M11 is what remains of that pair.** M10 made the fight survivable
by widening the spawn group gap and moving nothing else. M11 makes buying something
matter: a level was worth `+4.5%` against a measured `~20%` swing in play
(`Design/2-Dynamics.md`), so no purchase had ever been perceptible.

**M11 is one milestone because half of it is not a change.** Weapon stats live inside
the upgrade curve (`MetaUpgrades.GunDamage` hardcodes the MK-1's 12 damage), which is
why the game has exactly one gun — but splitting that out and shipping a single weapon
is infrastructure, not something a player experiences. The system and the weapons that
make it mean anything are one unit: four weapons, a contextual ARMORY, per-weapon
**Tiers** at ~30% a step, and a Primary locked for the Run so choosing it is a
decision. It also gives each console a subject — **ARMORY owns the weapon**,
**CREW QUARTERS the player**, **FABRICATION the ship**.

The three Primaries map onto the three gatekeeper kinds — the VK-7 for the steady
column, the BR-12 shotgun for the cluster on the door, the MR-35 for killing a Ram
before it arrives. **M2 then finishes the fight**: Wave 3 is the only Wave carrying
Rams and has never been reached on this build.

**M13 sits early because it is a multiplier, not a feature.** Every milestone after
it gates on a logged Run, and every one of those runs currently carries a prologue
that teaches nothing — at five Waves, ~150 s of it. It is the one piece of tooling in
this table, admitted under an amended rule that separates durable infrastructure from
a one-off chore; the bar it has to clear is that it makes the rest cheaper to verify,
and a `[Dev]`-tagged log can never close a gate.

**M15 comes before M14 because it is already overdue.** The weapon slots M11 specifies
are bound and working — `1`/`2`/`3`, `Q` and the mouse wheel all function — and the
game never says so, which is how the developer who specified them failed to find them
in play. It ships an on-screen panel of usable keys, **dev keys included**: at this
stage the person playing is the person developing, so `F1`–`F9` are controls like any
other. It also resolves a live collision — the dial reads `Alpha0 + level` while the
slots read `Alpha1..3`, so during Prep pressing `1` does both. The dial moves to
`[` / `]`.

**M16 comes before M14 because the curve has no shape yet.** Logged at dial 0: an
MR-35 T3 cleared all three Waves with **zero arrivals and zero damage taken**, while six
runs a day earlier lost Wave 3 every time. Two measured facts drive it — kill rate barely
depends on the weapon (0.61–1.08/s across every loadout, because realization falls as
dps rises and the gun idles), and leak is a *difference* of two near-equal rates, so a
33% kill-rate change swings it 2.6x. Extending an unshaped curve to five Waves means
tuning five instead of three.

**M14 is last because it doubles the content and the iteration cost at once.** Five
Waves only makes sense once the fight in one room is settled, and the deep start is
what keeps the back half reachable — which is why the Wave count and the start
selector are one milestone rather than two.

**A Steam page, and replacing Bastion kit meshes, remain a phase, not a milestone.**
They previously sat in this table as "M9 — off gray-box". **M61 shipped that exception**
(Zo, 2026-08-13 / closed 2026-08-14): view models, sight, zoom, Ship consoles. Fire and
footsteps closed with M69. Kit footprints stay locked. Nothing above depends on a Steam page.

**M20 before M21, and the reason is arithmetic.** The wheel's expressive surface is the
ring: five attributes give 12 distinct arrangements and exclude two per focus, four give
`3!/2 = 3` and exclude one. No weapon in the game has an activated ability today — a grep
of `Assets/Scripts` finds only the dev console's invulnerability toggle — so M20 exists to
make the fifth spoke real before the wheel is built on top of it. **M16 collides with
both**: its gate reads "3 Tiers" and M21 dissolves Tier into a derived number, so M16 lands
first against the flat curve or its gate is rewritten in an edit that says so.

**Co-op is an epic and is paid for continuously, not scheduled.** The seam is held
every session — pure-C# `Sim/`, ownership labels on MonoBehaviours, commands through
`GameController` — rather than budgeted as a milestone. It earns rows here when one
slice of it can be cut with real criteria.

## Brainstorm

Moved out of this file. Mechanic areas that are wanted but not yet cuttable live in
`Brainstorms/`, one file per area, with a stated graduation test:

| Area | File |
|---|---|
| Working surface (entries exit into a topic file) | [`Brainstorms/Master-Brainstorm`](Brainstorms/Master-Brainstorm.md) |
| Fabrication, ship systems, deployables | [`Brainstorms/fabrication`](Brainstorms/fabrication.md) |
| Crew attributes, skills, perks | [`Brainstorms/crew-skills-and-perks`](Brainstorms/crew-skills-and-perks.md) |
| Missions, the console restructure, destinations | [`Brainstorms/missions-and-destinations`](Brainstorms/missions-and-destinations.md) |
| Gatekeeper kinds and Wave composition | [`Brainstorms/gatekeepers-and-waves`](Brainstorms/gatekeepers-and-waves.md) |
| Co-op beyond the filed slices | [`Brainstorms/co-op`](Brainstorms/co-op.md) |
| Boarder Protocols beyond Default | [`Brainstorms/boarder-protocols`](Brainstorms/boarder-protocols.md) |

The First Bastion demo graduated 2026-08-17: beats 0–7 + voice cut as **M74–M80**
(`Demo/` stays as the implementable spec; the first-session brainstorm was retired
2026-08-19, its scope fence carried into `Demo/README.md`).

Closed — Open table empty, kept as record (`Brainstorms/closed/`):

| Area | File |
|---|---|
| Armory, weapons, Tiers, slots | [`closed/armory-and-weapons`](Brainstorms/closed/armory-and-weapons.md) |

An entry graduates when its acceptance criteria can be written; the entry is deleted and
a milestone replaces it (`_meta/_definitions/brainstorm.md`).

## Dependencies and risks

- **Retuning ahead of a logged Run is the likeliest failure here.** Retuning against arithmetic alone
  is a fix-that-fails: the symptom moves, the structure doesn't, and the next
  session inherits a number with no play behind it. This has now happened once —
  see the marked-wrong model in `Design/2-Dynamics.md`.
- **Changing more than one lever per logged Run.** The three Wave-2 attempts on
  2026-08-03 changed weapon level *and* siphon dial across the set, which is why
  they can show that upgrades don't decide the outcome but cannot say how much of
  the throughput change was the purchase. One lever, one Run.
- **The upgrade curve was smaller than the noise, and is not any more.** The old flat
  track paid ~4.5% a level against a measured ~20% run-to-run spread, so no purchase was
  perceptible. M11 and then M21 inverted it: one wheel purchase pays `FocusAward` 5 points
  to the centred spoke, and at `DamagePerPoint` 1.0456 that is `1.0456^5` = **+25%** damage
  on a damage focus, above the noise floor by construction (`Sim/Meta/WeaponWheel.cs`).
  Updated 2026-08-09. **The risk moved rather than closing**: the ceiling is now the
  problem, not the step. A full focus is 50 points — `FocusAward` 5 x `MaxPurchases`
  (`WeaponTier.MaxTier`) 10 — for `1.0456^50` = **9.30x** damage, and the ~20% spread means
  nothing about whether a 9.3x ceiling is a game. That is unknown, needs play data, and
  M29 ships the reading (`Tools/wave-report`).
- **A tune can silently not reach the running game.** Prefab `[SerializeField]`
  values outrank code defaults and have already eaten a full tuning pass
  (`Architecture.md`, gotcha 1). Mitigation is the push-at-spawn pattern; the
  risk is a new tunable getting serialized instead of pushed.
- **M6's cost is paid continuously, not at M6.** Every MonoBehaviour without an
  ownership label and every `using UnityEngine` reaching `Sim/` has to be undone
  later, under pressure.
- **M4's balance is unvalidated by its own reference** — Remnant 2's archetype
  pairing is cosmetic (`Decisions.md`). Status: unknown, needs play data.
- **This structure is the risk it warns about.** A roadmap plus ten milestone files
  is the easiest thing in the project to work on instead of the project. If a
  session spends longer in `Docs/` than in `Assets/`, that is the signal.

## Review cadence

No calendar. Three triggers: a milestone changing status, a retune landing in
`Design/4-Data.md`, and any session that ends with a new Brainstorm entry. Solo
project — the cadence is the work, not a meeting.

## Milestone files

One file per milestone in `Milestones/<status>/`, named `M#-slug.md`. **The folder
is the status**, so changing status is `git mv` plus the table row, in one commit.
Folders, numbered so they sort in workflow order: `1. planned/`, `2. active/`,
`3. completed/`, `4. deferred/`. **`Milestones/5. cancelled/` holds five files** (M34, M39,
M43, M46, M67) and the Order table already links them.
`_meta/_definitions/milestone.md` says four is the set and forbids new status folders, so
either the contract gains a fifth row or the folder is folded into completed-with-cancelled
status — **Zo's call**, still open since 2026-08-09.

The authoring contract — required sections, the copyable scaffold, the
link-never-copy rule, and the lifecycle — is `_meta/_definitions/milestone.md`. Read it
before creating a milestone file. It is not repeated here.

## Maintaining this file

- A milestone moves status only when every one of its acceptance criteria is checkable
  and checked — not when the code exists. Move the file, stamp the month, update the row.
- Update the **Current** line in the same edit that changes a status, or it rots
  into the most misleading line in the file.
- Reordering is normal; **silently softening a gate is not.** If a gate turns out
  to be wrong, change it in an edit that says so.
- New work goes to Brainstorm first, and earns a file when someone can write its
  gate.
- Nothing here overrides `Decisions.md`. If a roadmap item smells like a cut
  feature, it is one.
- Log structural edits and status transitions in `Changelog.md`. A roadmap nobody can
  audit is a roadmap that quietly rewrites its own history to match what happened.

## Change log

The log lives in `Changelog.md` — state transitions and structural edits, most recent
first. Append there, not here.
