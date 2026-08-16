# Change log

<!--
Purpose: The audit trail for Docs/Roadmap.md. Milestone state transitions and structural edits to the roadmap, most recent first. Split out of Roadmap.md on 2026-08-08, entries unchanged.
Status: working
Update trigger: when a milestone changes status, when a gate is rewritten, or when the roadmap's structure changes.
Last updated: 2026-08-16
-->

- 2026-08-16: **First-session brainstorm recut to the first-Bastion demo.**
  Wake, repair NAV, grant weapon, one terminal, designed Hatch-bank loss, ARMORY
  cap 2, start Wave 2. Extra sequences: second terminal, Default Protocol.
  Not FAB / juice / Thieves / Master Control.
  [`Brainstorms/first-session.md`](Brainstorms/first-session.md).
- 2026-08-16: **Pause Compendium drops Field Manual.** The generated tables have no
  TMP layout and the body scroll does not move, so the appendix stays in `Docs/Play/`
  only. Field Manual links in Game copy render as labels, not entries.
- 2026-08-16: **Ship plays Whispers of the Void.** The Ship host was still named
  `Background Music` and bound to Stardust; `LevelMusic.CaptureOrJoin` only adopts
  `Level Music`, so the loop started on Bastion. Renamed and bound to Whispers
  (`guid: 482f8aea…`), PlayOnAwake off, matching Bastion.
- 2026-08-16: **M70 recut refined** — locks for BarrageField (not AnchorField),
  Barrage×Rush product 3.0, CREW/NAV copy, shortcut `CooldownFill` on Fight and
  Pause templates, disc marker, docgen, no activate SFX. Not closed.
- 2026-08-16: **M70 recut** — Default is a 0-salvage CREW claim (Andy/Soldier:
  Barrage `G` / Rush `H`); Coming Soon stubs; Fight shortcut is the G/H readout.
  Anchor/Bulkhead/Ghost/Foresight leave Default. Frame in tree stays. Not closed.
- 2026-08-16: **Docs hydration** — Play/Compendium, Design 1–4, Architecture map,
  language.md party terms. Field Manual regen: CREW protocol, Decoy, Hatch Armor only,
  crit frozen. Dashboard Session labels PlayMode/short/Dev instead of dropping them
  (hatch-bank era ≥ 2026-08-13). `dashboard-data` friends head is M72. M72 still
  open (polish + two-machine criterion 9). M70 in tree, not closed.
- 2026-08-16: **Bastion kit stamps unpack; Authored rooms are not rebuilt.** Kit prefabs
  remain generate-once templates. `BastionSketchEmitter` unpacks `Completely` (was
  outermost, leaving Synty links). `SpawnPiece` unpacks on editor Rebuild. Emit
  `RebuildAllSegments` skips `Authored/`, so a re-emit cannot replace unique rooms
  with a fresh pack stamp.
- 2026-08-15: **M72 kernel in tree** — DDOL `NetSession` from menu Host/Join;
  `EnableSceneManagement` on; party Ready + 5s countdown (`NetPartyLobby`); guest
  local Bastion refuse; party ReturnToShip without shutdown. Release-Gate #19 →
  shared Ship / party EMBARK. Eyes-on criterion 9 still open.
- 2026-08-15: **M60, M63-2, M71 completed.** M60 Relay join code Zo-closed (criterion 4
  eyes-on; `NetSessionAddressTests` green at `gkb_verify_report.json`
  2026-08-15T21:47:12Z; Multiplayer Services package removed). M63-2 three meshes +
  player capsule off (`GatekeeperPresentationTests`; silhouette Zo-closed). M71 already
  stamped earlier the same day; stale `2. active/` copy removed. Active empty; next is
  M72 then M69.
- 2026-08-15: **M72 active / hardened** — grill locked DDOL + ESM + Ready-before-commit +
  5s cancel-only. Kernel in tree; post-grill fix: `Spawn(destroyWithScene:false)`,
  SceneEvent lobby-ship log, guest party-return RPC, pause Ready. Criterion 9 eyes-on
  still open.
- 2026-08-15: **M72 created** — co-op polish / shared Ship lobby. Party lead owns NAV;
  personal resources (M65); seamless same-instance join; party-wide scene loads
  (ready + 5s countdown); Bastion verbs unaffected. Overturns M64 solo-Ship friends
  shape after dual-box Relay failure. Queue: M72 / M69. Kill clock 2026-08-28 (was
  shared with M60 criterion 4; M60 now closed).
- 2026-08-15: **M64 completed** — friends send gate. Machine: `Docs/Release-Gate.md`,
  `Tools/release-gate/run.sh` PASS (company Gatekeeper), Host/Join → `LoadShip`,
  `NetPrep` + `NetPrepTests` 3/0 Passed (juice/start/range;
  `gkb_verify_report.json` 2026-08-15T21:34:37Z), `NetRunMirror` Intensity, menu
  version, `FrameBudget`, `run-windowed.bat`, `Docs/Release-Send.md`. Criteria 13–18
  and 19–25 playthrough are confirmation (same as M59/M62) — not claimed played this
  session. Unrelated suite reds: `BastionDoorApertureTests`, `SessionMusicHostTests`,
  `NetVerbTests.Repair` epsilon — not this slice. Solo-Ship product bar superseded by
  M72 after eyes-on failure the same day.
- 2026-08-15: **M65 completed** — three players supported (six cheap); waves scale by
  player count; salvage splits to own wallets with banking capped by own deepest Wave.
  Gate: EditMode `PlayerWalletTests`, `WavePlayerCountTests`; PlayMode
  `NetThreePlayerTests`. `Convert_GuestDeepest2` fixed to cumulative BankCap;
  `RunSim.Wallets.cs` partial for the line cap. Full suite still has unrelated reds
  (`BastionDoorApertureTests`, `SessionMusicHostTests`); not claimed green.
- 2026-08-15: **M67 cancelled.** Premise was the M52+M53 compound gate (no bank
  until a five-Wave clear). M53's carry recut already converts after Wave 1
  (`HatchBank.CarryConvert(1)`, `HatchBankTests.NibbleThreeTimes200_Banks600AtOneTimes`).
  File moved to `5. cancelled/`. Fresh-save tooling or a kill-rate envelope can
  be refiled if a session needs them.
- 2026-08-15: **M63 recut; M63-2 active — the three that walk.** M63 keeps the
  twelve-kind roster, Ram→Thief in code, `in_build` filter, and door-time. Bodies,
  Mixamo melee walk, player capsule mesh removal, and Thief-named Play pages for
  Default / Hunter / Thief (code still Ram) are `M63-2`.
- 2026-08-15: **M63 roster restored to the brainstorm.** The twelve kinds named when
  M63 was cut (Default, Hunter, Thief, Source, Enabler, Alarmer, Bypasser, Reclaimer,
  Exploder, Standoff Shooter, Ailment Shooter, Cloaker) were only in the milestone's
  criteria list; `Brainstorms/gatekeepers-and-waves.md` still described three.
  The roster table, property list, draw rule and Thief recording are in that file.
  Play pages still cover the three in the build.
- 2026-08-15: **M60 in progress** — Unity Relay join code on Main Menu Host/Join;
  allocate on the menu before Bastion; LAN fallback; pause shows the code; F10/F11
  editor-only. Kill criterion 2026-08-28. UGS project ID still empty — Host refuses
  by name until linked. Packages: `com.unity.services.relay` 1.2.0 +
  `com.unity.services.authentication` 3.7.0.
- 2026-08-15: **M62 completed** — Compendium from pause (Bastion + Ship). Hand-written
  Play pages hold `## Game` / `## Reference` / `## See also`; reader shows Game +
  See also in a scrollable volume tree; StreamingAssets copy via
  `GatekeeperBastion/Build/Copy Compendium Pages` (also player-build); HUD/pause chrome
  hidden while open. Gate: `CompendiumFormTests`. Zo close; playthrough confirmation.
- 2026-08-15: **M64 recut — both seats play the loop.** Definition of done is host and
  client each shop on their own Ship, EMBARK, start Waves, fight with muzzle/SFX, repair
  and decoy, and see an ending. Host/Join currently skip Ship; client Wave-start is an
  explicit refuse in `GameController.Terminals`. Shareability machine checks stay. Five-Wave
  clear, split wallets, Whispers, hit-confirm remain M65 / M69 / M66
  (M67 later cancelled same day).
- 2026-08-15: **M59 completed** — Field Scale 1; fight HUD pairwise ≥24 (Shortcut Keys
  nudged for 1280×720); Score/Wave/Weapon overflow; Profile Exp inactive; station
  Categories/Preview off, Background Blur α≥0.65, purpose/deal/footer copy; pause
  `ShortcutPanelView` bound with `DevSurface.None`; end Scrim α≥0.65. Gate:
  `HudSeparationTests` + `M59PolishChromeTests` + `StationPurposeTests` Passed
  (`gkb_verify_report.json` 2026-08-15T18:03:48Z). Criterion 13 confirmation.
- 2026-08-15: **M62 in progress — Game dimension.** Each hand-written `Docs/Play/` page
  holds `## Game` (pause reader) + `## Reference` (fuller brief) + `## See also` +
  `## Gaps`. Reader and StreamingAssets copy show Game + See also only. Pause command
  on Bastion and Ship; menu `GatekeeperBastion/Build/Copy Compendium Pages`.
- 2026-08-15: **M55 and M71 completed.** Intensity juice (`+/-` Unbanked, `E` then `T`,
  HUD Intensity); MK-1 ring D–A–R–C–F and SEC-D F–A–R–C–D with Secondary cost
  150+150×t. Zo close; M55 kill criterion and M71 Regen Surge mag sit on
  `GatekeeperZo Notes/verification.md`. Active 0/3.
- 2026-08-15: **M70 Default Protocol in tree.** CREW equips Default (STOCK / Anchor `G` /
  Bulkhead `H` / Ghost or Foresight); save v8 refunds crew tracks; `PayloadLoadoutLocked`
  unchanged. PlayMode `NetProtocolTests` Passed. HUD protocol TMP authored on
  `Hud Vitals` (host stayed 400×150). Not closed — playthrough still owed.
- 2026-08-15: **M70 recut — the Default Protocol.** Free-pick pools superseded (doctrine
  needs a named carrier). Slice is STOCK / Anchor `G` / Bulkhead `H` / Ghost or
  Foresight pick-1; turret to FORGE; specialists to their slices. Same-day audit:
  Ghost **0.70×** named; doctrine is a `TryPlace` write, not a seam proof; repair is
  not a place path; `PayloadLoadoutLocked` not renamed; Ghost does not delay Hunters;
  file slug `M70-the-default-protocol.md`.
- 2026-08-15: **M58 completed** — Windows Development player + Main Menu Host/Join
  (`0.0.0.0` listen, typed IPv4 join); dual-box
  `Tools/player-session/run.ps1` PASS with local Player present. Criterion 4 re-cut:
  connect + Player; networked kill / both-screens HUD+VFX moved to M64 (items 18–19).
  `-popupwindow` removed from the harness (no title-bar handles). Planned count 10/10.

- 2026-08-15: **Dashboard Focus is judgment, not inventory.** Synthesis paragraph,
  templated insight cards (severity + move + evidence), friends-alpha as do-now over
  off-path actives, Project demoted to lists. Regen: `Tools/dashboard-data/run.ps1`.

- 2026-08-15: **Dashboard grows Focus / Project tabs.** `Docs/dashboard.html` keeps the
  Session log picker (M38) and adds lane meters, milestone lists, commit timeline,
  outstanding questions by priority, and a rule-based next-focus card from
  `Docs/dashboard-data.js` (`Tools/dashboard-data/run.ps1`). Kill lines with
  `(+N unbanked)` count toward kill salvage again.

- 2026-08-15: **Bastion Player restored for dual-box.** Working-tree Bastion had lost the
  local Player rig (FPC / PlayerHealth); `-gkb-host` connected but neither window was
  drivable. Restored from HEAD plus Boarder prefab instance. Shift UI null-guards and
  always-included `UI/Particles/Additive` stop the Development Console overlay.
  Bastion OptionsView stays disabled until its dead fileIDs are rewired. `one-click.cmd`
  is interactive windowed dual-box (no auto-quit).
- 2026-08-15: **Same-machine dual-box harness.** Host advertises `127.0.0.1` while
  listening on `0.0.0.0` (typed `0.0.0.0` join is refused — that was the failed dual-box
  log). `PlayerLaunch` args `-gkb-host` / `-gkb-join` skip Splash into Bastion.
  `Tools/player-session/run.ps1` launches two player builds and greps connect lines.
  File bridge `gkb_player_build_request.txt` builds a Development Windows player into
  `Logs/Builds/` without SceneBuilder.

- 2026-08-15: **Build All skips a dressed Ship.** A `ShipDress` root is treated like
  Bastion kit: scenes still register, Environment is not wiped. ARMORY wheel BUY now
  writes Shift `buttonText`, so the salvage cost shows on the control.
- 2026-08-15: **Bastion plays Whispers of the Void** on a `Level Music` host after
  EMBARK (menu Stardust still stops on Continue). Reload and footstep wavs got a
  12 ms / 8 ms fade-in so the first sample is no longer a click. M69 playthrough
  gate still open.
- 2026-08-15: **M57 completed** — client down loads Ship additively; Hatch-bank Prep
  EMBARK (NAV or pause) restores full HP at the Hatch; Assault and Terminal-Prep
  refuse. Gate: `NetDownWaitTests` 2/0. Criterion 8 MPPM is confirmation.
- 2026-08-15: **Main Menu Host / Join.** Host listens `0.0.0.0:7777`; Join takes a typed
  IPv4 and port (default 7777). Intent crosses the Bastion load via `NetJoinIntent`;
  `NetSession.StartHost` / `StartClient` push the address (no serialized loopback).
  Failed connect logs `[Net] join refused` and returns to the Join panel. F10/F11 stay
  as the editor fallback. `NetSessionAddressTests` asserts the transport address.
- 2026-08-15: **Document refresh.** Roadmap Current named only M55 and claimed planned
  14 / cancelled-empty; disk is M55+M71 active, planned 12, `5. cancelled/` holds M34/M39/M43/M46.
  M64–M67 existed as planned files with no Order rows — indexed. Design 1–3 and the
  one-pager still described Interact-to-start, a free siphon dial, Wave-5 Escaped, and a
  Ram that never hunts; code is juice / `E` / `T`, Master Control Hatch-bank for Escaped,
  Ram hunts on field Waves. Play overview, language Ram row, Architecture singleton and
  scene-edit claims, 4-Data block count, and the M53 brainstorm link brought into line.
  Mobility cut in Decisions records M70's expiry argument, unstamped.
- 2026-08-15: **ARMORY shows every shop wheel, with cost.** Owned catalog rows say
  UPGRADE (opens that weapon's wheel) instead of EQUIPPED; unowned stay BUY, so the
  other Secondary remains purchasable after the first. The panel opens on Secondary
  when Primary is empty. Wheel lock/short/Ability-blocked lines all name the salvage
  cost (`150 + 150×t` on Secondaries). SEC-D ring authored F–A–R–C–D (strength
  FireRate, weakness Reload); `SpokePurchasable` refuses a centred Ability buy while
  `AbilityId.None`. Tests: `SecD_AbilityUnset_RingAuthored`,
  `SecDFireRateFocus_NeverRivalsFocusedPrimaries`, `SpokePurchasable_FollowsCatalogAbility`,
  `RingsAreUniqueAcrossShopWeapons` now includes SEC-D.
- 2026-08-14: **Main Menu QUIT leaves in one click.** The Exit modal's GO AHEAD
  is skipped; Start New still confirms overwrite.
- 2026-08-14: **Secondary wheel purchases get their own cost curve** (M71 criterion 5
  recut, Zo). The shared 400 entry ate a wave-1 clear (450) whole; Zo set the gate at
  entry ≥150 with headroom for a possible second buy. `WeaponTier.Cost(WeaponSlot, int)`:
  Secondary 150 + 150 × tier (150…1,500, cumulative 8,250), Primary unchanged
  (400 + 200 × tier, 13,000). Charge and ARMORY price label price by slot;
  `ReconstructSpentFromLevels` deliberately stays on the historical shared curve — it
  only runs for pre-wheel migration and tamper repair, both priced there. Tests:
  `SecondaryTierCosts_LinearRamp_SumsTo8250`, pacing test recut to entry ≥150 and
  150 + 300 ≤ 450. Harness 360/4 — the four reds still the parallel HUD pass.
- 2026-08-14: **M70 recut — one Crew ability became three slots.** Zo: every Boarder picks
  from the ability pools into a Crew slot (`G`, acts on Gatekeepers or teammates: Anchor,
  Ping), a Personal slot (`H`, acts on you: Cloak, Recall) and a Passive slot (standing
  rules, never numbers: Ghost, Warden, Foresight — proposed rows, Zo-vetoable on the
  file). The first cut's "several equipped with slots" rejection is overturned on
  grammar — the slots answer different moments, and a new kill criterion instruments the
  collapse case (one active slot under a fifth of the other's activations across ten
  Runs). Its co-op-stacking half survives as an open question for M65. A Medic-lane
  passive is deliberately absent until the `player {hp}/{max}` log instrument exists.
  Turret half unchanged on FABRICATION. Full kit 3 × 400 = 1,200, 22.9% of the measured
  5,250 clear.
- 2026-08-14: **M71 authored and built — the sidearm joins the wheel.** MK-1 ring
  Damage–Ability–Reload–Clip–FireRate, strength Reload / weakness Damage (Zo). Wheel
  purchase, ARMORY UI, storage and save round-trip shipped slot-agnostic in M21, so the
  build is two `WeaponWheel` switch cases plus gating tests: kill criterion did not fire
  — `Mk1DamageFocus_NeverRivalsFocusedPrimaries` passes at every purchase count with the
  shared per-point table, and `FirstWheelPurchase_AffordableOffOneWaveOneClear` pins Zo's
  pacing constraint (`WeaponTier.Cost(0)` 400 ≤ 18 × 25 = 450). Harness 359/4, the four
  reds all `HudSeparationTests` from the parallel HUD pass. SEC-D ring rejected this
  slice — its ability is `AbilityId.None` (M26 row, blocked on Zo), a ring would sell
  dead points. **Planned queue was 13 against the cap of 10 at authoring — cap waived by
  Zo for this slice**; the file moved to active the same session.
- 2026-08-14: **M61-2 status later → completed.** Kit blocks are POLYGON Sci-Fi Space
  meshes on scaler-2 station rooms, functional sliding doors, bulkhead Hatch,
  Generated/Authored split. Gates: `gkb_verify_report.json` 2026-08-15T00:09:20Z —
  EditMode 394/5, PlayMode 30/1; both baseline reds unchanged, the other four are
  `HudSeparationTests` from the parallel HUD pass, no reds of this slice's own. Scene
  greps: 1 realtime light, 0 MeshColliders, 2735 renderers all on the one atlas
  material, 2735 static-flagged, 179 shadow-off fixtures. Waves arrive on the final
  geometry (logs `17-29-54`/`17-34-29`, untainted). Open play rows filed in
  `GatekeeperZo Notes/verification.md`: criterion 4 eyes lines per block kind,
  criterion 13 Stats after-numbers (before unrecoverable — the primitive bastion no
  longer exists), criterion 14 one solo completion.
- 2026-08-14: **M61 status later → completed.** IQ astronaut + Synty guns (six Packs
  wrappers), stroke crosshair, RMB zoom Hip 60 / Zoom 40, Ship `SM_Prop_ControlPanel_02`
  ×4 + `SM_Prop_Screen_Small_01`. Unity EditMode `WeaponViewTests`+`ControlsTests` 15/15;
  PlayMode `ShipSceneContractTests` 5/5. Audio is M69.
- 2026-08-14: **M61-2 rebuild-on-reload landmine defused after it destroyed authored
  geometry.** `BastionSegment.OnValidate` fires on every script reload, not just
  inspector edits, and it rebuilt `_geom` each time — every compile silently reset all
  rooms to canonical packed pieces. The compile for the grouping change fired it and
  wiped Zo's fully-unpacked geometry; the grouping tool's save then wrote that over his
  scene. Unrecoverable (no scene backups); everything outside `_geom` — room roots,
  doors, terminals, spawns, hatch — survived. Fix: a serialized `rebuildStamp` hash of
  the geometry inputs (block kind, side kinds, alongs) gates OnValidate — reloads with
  unchanged settings never touch `_geom`, inspector changes still rebuild. Pre-stamp
  instances with built geometry adopt the hash without rebuilding. The unpacked state
  was restored via `GatekeeperBastion/Unpack Bastion Geom In Open Scene` (0 packed
  `Gen_` pieces, 2854 plain, stamps persisted); piece-level hand tweaks from that
  window, if any, are lost. Same window, second defect: a play-mode rebuild parented
  its fresh geometry into groups that deferred `Destroy` was about to kill, so every
  collider died at end of frame one — the "player falls through the floor" report.
  `ClearGeneratedChildren` now detaches before destroying; live-verified (player
  standing at y 0.98 on R1's floor in play mode).
- 2026-08-14: **M61-2 room geometry grouped by category.** Zo: walls, floors and ceiling
  items each under a parent node. Every segment's `_geom` now holds three group nodes —
  `Walls` (wall runs, doorframes, windows, corners, walk blocks, side colliders),
  `Floors`, `Ceiling` (tiles and lights) — and rebuilds emit grouped natively
  (`EnsureGeomGroup`; groups carry the generated marker so a rebuild clears them
  wholesale). The open scene was regrouped in place via
  `GatekeeperBastion/Group Bastion Geom In Open Scene` — reparent only, no rebuild, so
  Zo's hand edits survive and world transforms are untouched.
- 2026-08-14: **Unowned Ship hands are empty.** Boarder prefab left Mk1 active; Ship has
  no PlayerGun so WeaponViewModel never hid it. `WeaponView.TryShownWeapon`: empty
  hands on the Ship until a buy; cutter only in a fight with nothing owned. ARMORY
  catalog columns are LayoutElement mins (Name 220 / Cost 160 / Effect 260) with
  ChildControlWidth on; `cost 1200` no longer stacks, and `need N more` stays on
  Reason instead of the action button.
- 2026-08-14: **M61-2 rooms emit unpacked.** Zo: packed room instances block hierarchy
  edits during authoring. `BastionSketchEmitter` unpacks the outermost root at emit;
  the Synty pieces under `_geom` keep their prefab links. Zo then moved all 15 rooms
  under `Authored/` — the layout is his now. `Generated/` is empty: a re-emit builds a
  second layout on top of the authored one, so the sketch emitter is retired for this
  bastion unless the authored copy is deleted first.
- 2026-08-14: **M61-2 doors made functional; hatch is a bulkhead.** Zo: passage doors
  open/close, terminal doors stay locked until their wave count clears, hatch styled
  uniquely. `BastionDoor` is now a sliding leaf — locked gates hold an orange seal until
  the unlock graph opens them, unlocked doors slide for anyone in range and shut behind
  (already the graph's contract; the leaf just moves now). Every open passage gets an
  auto door at emit (`BastionSketchPassages`, 11 on this layout). Two defects fixed the
  same pass: rooms had re-emitted at old half-spacing so 42 m footprints overlapped
  (the "walking through walls" and stray-slab reads), and the pack doorframe ships with
  a shut leaf baked in — it read as sealed and baked a NavMesh wall across every doorway;
  the visuals now disable it and the NavMesh bake hides door slabs. The Hatch is a
  7×4.5 m bulkhead slab on R1's south wall in its own `Mat_Bulkhead` (yellow, emissive).
- 2026-08-14: **M56 folded into M70 — the turret is an ability (Zo).** `UTL-H` keeps its
  decided shape — FABRICATION payload, ghost-and-confirm, onboard 30 then a 2/s clock
  siphon off the Primary reserve, life 45 s, no weapon-upgrade inheritance — and ships in
  the ability slice beside the Crew roster. Its console does not move: a placed drone item
  stays on the payload shelf, not on `G`. Turret kill criterion carried whole (ten Runs;
  parked-at-Hatch or never-placed recuts once, a second same ten cuts the row). Leverage
  tier restated per the ladder (M56's file said #9 rules; rules are #5, the second firing
  position #10). Queue position survives: the turret reads best after M55. M56's number
  retires; file deleted. Planned 14 against cap 10.
- 2026-08-14: **M70 created — CREW QUARTERS sells Crew abilities, not attributes.** Graduated
  from `Brainstorms/crew-skills-and-perks.md` (Decided 2026-08-12; stealth layer and trade
  filter 2026-08-14); one unified milestone — system plus starter roster — by Zo's call.
  Roster of four on `G`, one equipped per Boarder: Cloak (shared visibility layer), Anchor
  (plant and slow), Recall (unbanked pile for escape), Ping (concealment for information).
  Overclock rejected — its grant is the wheel's damage currency. The four attribute tracks
  are deleted and refunded at exact spend (`200×(2^level−1)`, SaveVersion 8); crit chance is
  0 with the roll kept. Stealth kill criteria date from the Cloaker slice (M63 roster).
  Planned 15 against cap 10.
- 2026-08-14: **M68 folded into M63 — the garrison: a roster with bodies.** One slice now
  carries the twelve-kind roster record, Ram→Thief, the Garrison volume with `in_build`
  flags, the door-time log line, and the three in-build bodies (Default
  `SM_Chr_RobotFemale_01`, Hunter `SM_Chr_SpaceSoldier_HelmetArmor_Male_01`, Thief
  `SM_Chr_BR_War_Robot_01`; Mixamo Humanoid or keep capsules). The rename lands before the
  wrappers, so the body work is Thief-named from the start. Leverage tier stated once as
  #6 information flows (M68's file said #8). M68's number retires; file deleted.
  Planned 14 against cap 10.
- 2026-08-14: **M61-2 re-framed mid-pass — station rooms, not one giant diagram.** Zo:
  enclosed space station, individual walkable rooms with doors, hatch docked against a
  wall, scale up to read as a huge lost station. Kit multiples became metres on scaler 2
  (Half 16 m, Full 26 m, OpenArea 42 m, ControlRoom 152×76 m); layout re-authored from
  Zo's diagram as `Tools/bastion-sketch/bastion-defense-layout.txt` (15 rooms, T1+T2→D1,
  T3→D2, T4+T5→D3 control). M47's footprint lock is waived by Zo for this pass. Sketch
  emit now wires `_game` and `UnlocksDoor` on terminals — emitted terminals were
  inert without them.
- 2026-08-14: **Settings list first row.** Each tab's `List Content` VerticalLayoutGroup
  had `m_Top: -26`, which placed the first row above the mask (Resolution is 60px,
  so ~26px of the label was clipped). All four tabs in MainMenu, Ship, and Bastion
  now use top 25, matching left/bottom.
- 2026-08-14: **M59 recut — UI only; audio is M69.** Music and gun/foot SFX leave M59.
  M59 adds Score overflow, hide Profile EXP, end-screen scrim, movement-lock and
  start-Wave-cleared copy. M69 binds the five wavs already on disk; no store pack
  (276617 stays rejected). Planned 15 against cap 10.
- 2026-08-14: **M68 mapping recut — androids, not aliens.** Default `SM_Chr_RobotFemale_01`,
  Hunter `SM_Chr_SpaceSoldier_HelmetArmor_Male_01` (helmet on, head/hair off), Ram
  `SM_Chr_BR_War_Robot_01`. Organic aliens and unhelmeted faces rejected. EVA rejected
  (Boarder is already the astronaut).
- 2026-08-14: **M68 created — the garrison has a body.** Default / Hunter / Ram map to
  Synty `SM_Chr_Alien_01` / `SM_Chr_Psionic_01` / `SM_Chr_BR_BigAlien_01` (SKU 138857).
  Agent footprint unchanged. Mixamo Humanoid or keep capsules; no T-pose. Not folded into
  `M61`, `M61-2`, or `M66`. Crew/soldier meshes rejected (wrong faction).
- 2026-08-14: **M60 recut — Unity Relay, not Steam.** Join code on Main Menu Host/Join;
  anonymous sign-in; LAN IP stays as fallback. Free tier 50 average monthly CCU (`M64`).
  Steam / Facepunch deferred until a real appid. Kill criterion is two player builds, one
  code, one kill, ten working days. `M66` does not include this.
- 2026-08-14: **M61 packs closed — Synty POLYGON Sci-Fi Space (SKU 138857).** Guns are
  wrapper prefabs under `Assets/Packs/Weapons/`; desks are `SM_Prop_ControlPanel_02` +
  `SM_Prop_Screen_Small_01`. SFX are five wavs in `Assets/Resources/Audio/Sfx/`. Mixamo
  fire/reload/walk files are Idle/Run stand-ins until distinct takes replace them.
  Criterion 6 no longer bans Synty; kit footprints stay. Playthrough still the close.
- 2026-08-13: **M61 store packs un-committed.** Mixamo owns body anims, so a gun pack
  must be hand-parented meshes, not an FPS kit. Arsenal V2, Control Room 3, and the
  5.3 GB SFX kit fail that test (0 ratings; V2 has no listed shotgun and unused anims;
  SFX kit is 1173 files for five clips). Shortlist: PBR SciFi Weapons v3, original
  Sci-fi Weapons Arsenal, Kenney Blaster Kit probe. Rooms follow the gun look. Open
  section blocks M61 start until Zo picks.
- 2026-08-13: **M61 Boarder is the Infinity Qube astronaut.** Idle+Run copied from
  `InfinityQube/Assets/animations/Player/`; fire/reload/walk from Mixamo on that
  Humanoid. Arsenal meshes parent to the right hand. Remote clients see the astronaut
  this slice; M66 still owns colour and labels.
- 2026-08-13: **M55 recut — `+/-` juice, `E` select, `T` start.** Juice still
  100 Unbanked per step at the look-at. `E` locks the value; `T` starts Field,
  Master, and Hatch-bank. `E` again unlocks. ARMORY `[` `]` stay on the wheel.
- 2026-08-13: **M59/M61 recut.** M59 absorbs hull QoL (station chrome and copy,
  player-build keys, pause bindings) plus the existing room pass. M61 is the gun, sight,
  RMB FOV zoom, Ship consoles, fire and footsteps — contract phase-rule overridden for
  this named slice. Three store packs: Sci-fi Arsenal V2 (246550), SciFi Control Room 3
  (357948), Sci-fi Shooter SFX kit (276617, bound clips only). Synty rejected (fights
  Shift). Kit footprints stay.
- 2026-08-13: **Hatch-bank carry.** Convert and Hatch composition read field Waves
  since the last bank: 1 → ×1.00 / Wave 1; 5 → ×1.50 / Wave 5. Pile-size convert retired.
- 2026-08-13: **Field Interact is one Wave again.** T1 still needs 5 clears to open
  D1. Prep after each clear is untimed; Hatch-bank is legal there. `ArmSequentialDraws`
  stays in Sim for later terminals. New save can convert after Wave 1 (18×25 = 450
  unbanked). Reverses the M52 recut that chained remaining Waves from one Interact.
- 2026-08-13: **M54 status next → completed.** Wave 5 never Escaped; Master Control
  is 5 terminal (door sealed, `S_Control`) then 5 Hatch-bank (no slot spend); dump
  on last window. Zo playthrough. Harness 324/0; Unity `MasterControlTests` 8/8.
  PlayMode Repair tick (229.5 vs 229.0) and door-aperture 4.5 vs 3.5 are not this
  slice. Unblocks M55.
- 2026-08-13: **M55 status later → next.** Juice spends Unbanked at the trigger;
  HUD says Intensity; siphon dial retired in Play pages. Depends on M53/M54.
- 2026-08-13: **M54 status later → next.** Wave 5 Unbanked-0 Escaped is the remaining
  win path this slice removes. Recut on start: `BastionTerminalKind.Master`;
  InterWaveGap of Prep after each clear (not overlap); Hatch-bank windows dump
  only on half complete; SaveVersion 7; author `T_Master`. M57 stays planned.
- 2026-08-13: **M57 recut — embark is Hatch-bank Prep.** M52/M53 shipped; "every Wave
  is hatch-defense" and "if M50 has not shipped" were stale. Criterion 4 is
  `Mode == HatchBank` Prep, matching the locked co-op-down rule in Decisions.md
  (next hatch-defense Wave). Terminal Prep between field Waves is a refuse.
  Gate rewrite, not a status change.
- 2026-08-13: **M53 status next → completed.** Playthrough
  `Logs/GatekeeperBastion_2026-08-13_18-23-55_p92464.log` (Run clean): T1 five Waves
  hatch 300/300 salvage 0; Hatch-bank 18:31:53 unbanked=5250 slots=2, 78 bodies, `+0`,
  45.002 s, salvage 7413 hatch 292/300. Wave 5 with Unbanked did not Escaped. Dump
  still `BASTION STRIPPED` in that log; stay-Prep is EditMode after the recut. Harness
  316/0. PlayMode not re-run. No milestone active.
- 2026-08-13: **M52 status next → completed.** Playthrough
  `Logs/GatekeeperBastion_2026-08-13_14-46-59.log` (no `[Dev]`): one Interact starts T1
  Waves 1–5, 8.000 s gap, `wave 5/5 cleared`, T1 complete, `BASTION STRIPPED`, hatch
  300/300. AwardKill-to-bank is M53 (`+25 unbanked` in that log). Harness 309/0.
  M53 remains the only active.
- 2026-08-13: **M53 status later → next.** Recut on start: convert multiplier snapshotted
  at Hatch-bank start; Hatch-bank never deep-start chains; T1 Wave Spawns plus completed
  Field UnlockedSpawns for the extract; withdraw is `RunSim.Withdraw`, not `RepairHatch`.
  Kill criterion dated 2026-09-13. M52 stays active.
- 2026-08-13: **M52 recut — one Interact starts the terminal's N Waves.** After each
  clear, `TerminalChainGap` (8 s) of Prep, then the next draw. Not a deep-start
  overlap (that would make T1's default Interact the NAV MAP's hardest start). Play
  through is one Interact at T1, not five.
- 2026-08-13: **F10/F11 no longer taint.** Host/join stay on the Dev key table (compile
  out of release) but do not call `NotifyDevAction` and do not write `[Dev]`. The taint
  rule is unchanged: a `[Dev]` log still cannot close a gate. Unblocks co-op playthroughs
  (M50 close noted F10 at 10:29:53 as taint).
- 2026-08-13: **M51 status next → completed.** Gate is all five `NetVerbTests`
  Passed (`gkb_verify_report.json` 2026-08-13T18:09:48Z, PlayMode 31/0). Criterion 6
  playthrough is confirmation: Terminal Waves leave the hatch invulnerable, so
  repair-under-attack waits on M53. EditMode 327/1
  `BastionDoorApertureTests.TryGetDoorAperture_NorthDoor_AtHalfDepth` (4.5 vs 3.5)
  is the same miss as the M50 close, not this slice. Loopback in-run verbs done;
  M52 remains active.
- 2026-08-13: **M51 status later → next.** Recut against landed M50: client
  `Update` must tick Interactor / ability / decoy; repair prompt and HUD banked
  read mirrored `Banked` (M50 only mirrored run salvage); `RepairHatchFromNet`
  skips the host-rig check; `NetVerbs` on the avatar; drone slot wired
  (`SlotDrone` had no caller); networked decoy prefab; pin by `NetworkObjectId`.
  Criterion 6 playthrough is a damaged hatch, not under-attack (M53). Gate is
  `NetVerbTests`.
- 2026-08-13: **M52 status later → next.** Waves start at a terminal is in build: N
  Waves per terminal (T1 = 5), D1 unlocks on T1 only, Prep `T` refused, Hatch
  invulnerable in `WaveMode.Terminal`, Open-door Interact if the slab is still up.
  Recut this start: Wave 5 still `Escaped`; walking D1 is M54. Deep-start chain
  counts every Wave it delivered toward N.
- 2026-08-13: **M62 recut.** Voice pinned in `language.md` (*Player-facing voice*). Play
  pages authored to that pin this slice; the reader copies, it does not write. Compendium
  is its own pause command, not a row in the key list. Entry shape: origin plus fight
  tells, one pause. Unlock, companion diary, and completion ledger stay rejected.
- 2026-08-13: **M50 status next → completed.** Gate is `NetRunMirrorTests` Passed
  (`gkb_verify_report.json` 2026-08-13T15:19:01Z). Criteria 2–5 playthrough recut: one
  terminal Wave then `already complete`, D1 locked, hatch invulnerable — cannot prove
  hatch-fall, a second Wave, or an ending. N Waves per terminal (T1 = 5) and Open-door
  Interact are M52. Hatch-destroyed eyes-on waits on M53. No milestone active; next is
  M57.
- 2026-08-13: **M52 recut.** Terminals have N Waves, not 1. T1 is 5. D1 requires only T1.
  Completed terminal Interact opens its door if the slab is still up. Evidence:
  `Logs/GatekeeperBastion_2026-08-13_10-29-42.log` T1 refused after Wave 1; door stayed
  locked.
- 2026-08-13: **M59/M61 recut, M62 created.** QoL is two multi-area passes, not keys then
  copy: M59 the Bastion is a room (terminal Scale 1, HUD gutters, Whispers of the Void);
  M61 the Ship is a hull (station chrome + copy, player-build keys). M62 the Compendium
  you can open (volumes in `Docs/Play/`, pause reader). Brainstorm `compendium.md`
  deleted on graduation. Planned 12; cap overage continues.
- 2026-08-13: **M61 created** — *The stations a friend can read*, second QoL cut. Purpose
  lines on the four Ship consoles; ARMORY transfer note; FABRICATION Rig + salvage/HP;
  no sell-back. Not M46. Planned 11; cap overage continues.
- 2026-08-13: **Compendium.** `Docs/Play/` is the player-facing name Compendium (index
  rewritten). In-game reader is `Brainstorms/compendium.md`, not a milestone yet.
- 2026-08-13: **M50 status later → next.** Client lives the run is in build: readout
  fields on `NetRunMirror`, `EndSummary` ClientRpc, client HUD seam, session shutdown to
  MainMenu. Recut against landed M48/M49 (LateUpdate poll, client sim stays Tethered,
  endings cannot follow `sim.Phase`).
- 2026-08-13: **`Brainstorms/closed/`.** Files with nothing to mine (empty Open, no
  mineable Ideas) move here instead of sitting in the live set. `armory-and-weapons.md`
  is the first: its own Open section says nothing is open. Contract amended —
  `_meta/_definitions/brainstorm.md`. Do not delete a closed file that still holds a
  roster or Decided table.
- 2026-08-13: **M50 re-cut** against M48/M49 landed shapes (AssaultState byte, LateUpdate
  poll, `EndSummary` ClientRpc, client `ReturnToShip` cannot gate on `IsTerminal`). M57
  owns Ship wait.
- 2026-08-13: **M49 status next → completed.** Zo playtest: client huntable, down does not
  end host run. `NetTargetingTests` is the headless record; integration-check report not
  re-run this close. Get-up is M57.
- 2026-08-13: **Friends-alpha reorder.** Filter is a session a friend can operate. Execution
  order: M50 → M57 → M51 → M58 → M59 → M60 → M52–M56. Planned cap 10 broken to 11 with Zo's
  authorization this date; Working-Rules cap unchanged.
- 2026-08-13: **M57 created** (graduate down rule from co-op.md): Ship wait, embark next
  Prep, NAV or pause.
- 2026-08-13: **M58 created:** Windows player build, Main Menu LAN host/join (typed IP).
  F11 stays 127.0.0.1 editor fallback.
- 2026-08-13: **M59 created:** first friends QoL cut — player-build keys, pause lists
  playable bindings, F2–F9 unbound outside editor.
- 2026-08-13: **M60 created** (graduate Steam join from co-op.md): Facepunch transport,
  re-cut before start against M51+M58.
- 2026-08-13: **M26 row corrected** in the roadmap table (file already in `3. completed/`;
  Path 1 shipped).
- 2026-08-13: **M56 row added** to the roadmap table (file already in `1. planned/`).
- 2026-08-13: **M49 gate cut — no in-run client respawn.** Wave-start revive is out of
  criteria 5–6, Implementation, and `NetTargetingTests`. Client down still does not end
  the host run; how or whether the client gets back up is parked in
  `Docs/Brainstorms/co-op.md` (Zo: not decided). Live `WaveStarted` revive path removed
  so the slice cannot decide it by shipping.
- 2026-08-13: **M47 status next → completed.** Kit palette + authored Bastion; freestyle
  `BuildBastion` removed from SceneBuilder; Decisions + Architecture §4 kit-authored.
  Gate edit (Zo): criteria 13–16 close on author play (no raycast harness); criterion 18
  left `unknown — needs wave-report`. Unblocks formal M52 close. M49 stays active.
- 2026-08-13: **Field terminal inspector.** `kind` still gates the UI; Field shows
  `Unlocks Door` + `Unlocked Spawns` again (hidden on HatchBank / Resource / Master).
  Graph unions door `requiredTerminalIds` with terminals that reference that door.
- 2026-08-13: **`BastionTerminalKind`.** Terminals carry `kind` (Field / HatchBank /
  Resource / Master) only — no per-terminal door id or pressure-spawn list. Unlock is on
  `BastionDoor.requiredTerminalIds`; sealed spawns enable when that door opens. Terminals
  wire no longer moves gate transforms or rewrites Door/None sides.
- 2026-08-13: **M49 status later → next.** Gatekeepers see the second player is in build:
  capsule collider + server-registered `PlayerHealth` on the avatar, HP mirrored to the
  owner, client down survivable (host run continues; revive next wave). M48 shapes adopted
  (skip auto-register, client `Die()` does not call `PlayerDied`, `EnableSceneManagement` off in
  the fixture). M47 stays current. Kill/hunt playthrough remains confirmation, not the gate.

- 2026-08-13: **Field terminal starts the Wave.** Prep `T` refuses; Interact on a progression
  `T` calls `TriggerDraw` (`WaveMode.Terminal`); Hatch invulnerable that Wave; clear completes
  the terminal and opens its `D`. Terminal roles table in missions-and-destinations.
- 2026-08-13: **Passage vs progression gate.** Door sides without a `D` mark stay walkable;
  `BastionDoor` seals the aperture, starts locked, snaps to the segment opening. Soft-lock
  (switch / salvage) parked in missions-and-destinations. Wave-on-`T` remains M52.
- 2026-08-13: **M48 status next → completed.** First networked kill: NGO 2.13.1 +
  MPPM 2.0.2, F10/F11 loopback, `NetKillTests` Passed in PlayMode 24/0 (was 23/0 at
  M45). Report `gkb_verify_report.json` 2026-08-13T12:49:08Z. MPPM playthrough is
  confirmation, not the gate. M47 stays current.
- 2026-08-13: **M48 status later → next.** First networked kill is in build: NGO 2.13.1 +
  MPPM 2.0.2 (3.0.0 is not on the Unity registry), F10/F11 loopback, `NetKillTests` fixture. Kill criterion clock started
  2026-08-13. M47 stays current. Fence path corrected to `Architecture/3-Unity-Runtime.md`
  (the allowlist `lint-arch` actually reads).
- 2026-08-13: **M52–M55 created** in `1. planned/` (field loop, gameplay-impact order after
  M47, before M48–M51). M52 *Waves start at a terminal*; M53 *Salvage leaves through the
  Hatch*; M54 *Master Control is the win*; M55 *Pay for intensity*. Graduated out of
  `Brainstorms/missions-and-destinations.md` and targeting-mode out of
  `gatekeepers-and-waves.md`. Queue 8/10 planned.

- 2026-08-12: **M47 status later → next (current).** Folder was already `2. active/`;
  frontmatter and the roadmap row now match. Current line names M47 as the active build;
  M48 stays planned; station-UI polish stays unfiled.

This file is history. **`Roadmap.md` is what is next**: order, status, strategy and risks.
`Milestones/` is the detail behind any entry here.

- 2026-08-12: **M50 and M51 created** in `1. planned/` (Zo: author all slices now, refine
  through implementation). M50 *The client lives the run* — mirror widened to the fight
  readout, endings as an `EndSummary` ClientRpc, clean session shutdown; guest progression
  explicitly left open. M51 *The client's hands* — repair channel, decoy placement and
  abilities as server-validated RPCs with the per-verb single-application invariant;
  completes the loopback epic. Both carry re-cut-before-start update triggers since they
  are authored ahead of M48/M49 landing. `Brainstorms/co-op.md` rewritten: the two
  graduated entries deleted, replaced by three that stay — Steam + join surface, guest
  progression (unknown — needs design), presence beyond a capsule.
- 2026-08-12: **The co-op epic is chunked** (Zo: single dev, so testing needs a framework).
  Four slices to a full loopback run: M48 (kill + rig) → M49 (client is huntable) → "the
  client lives the run" → "the client's hands", the last two as `Brainstorms/co-op.md`
  entries with graduation tests tied to M48/M49's real shapes — pre-filing them would breed
  the vague gates the contract forbids. Steam + join surface graduates only after a full
  loopback run completes. **M48 revised**: it now also ships the in-process test rig
  (`NetcodeIntegrationTest`, one host + one client in one PlayMode test; new criterion 7
  `NetKillTests`), so the kill loop closes headless — the gate no longer needs two hands.
  **M49 created** in `1. planned/` — avatar gains a registered `PlayerHealth` + collider;
  targeting needs no enemy code changes (`NearestAlive` already multi-player).
- 2026-08-12: **AutoVerify renamed IntegrationCheck** (Zo). `Editor/Tools/IntegrationCheck.cs`
  (git mv), class, log tag and menu row (`Run Integration Check Now`) renamed; the sentinel
  filenames `gkb_verify_request.txt` / `gkb_verify_report.json` are kept as the stable wire
  protocol. Living docs updated (AGENTS §5, Working-Rules, Architecture map, 3-Unity-Runtime
  `initializeonload_allow` fence, 4-Testing, 4-Data discriminator note, gkb-verification
  skill); completed milestones and this log's history keep the old name as the record.
- 2026-08-12: **M48 created** in `1. planned/` — *The first networked kill*, the co-op slice
  M45's Watch section named: a second instance joins over UnityTransport loopback, kills one
  server-spawned gatekeeper, and both screens read the same wave counter. First netcode
  integration, so it carries a kill criterion testing the NGO choice (ten working days to
  criterion 5 or the library decision re-opens against FishNet). Packages pinned NGO 2.13.1
  / Multiplayer Play Mode 3.0.0.
- 2026-08-12: **M45 completed.** Offline harness 283/283 with the poison proof observed
  (a seeded static in `RunContext` failed both reflection teeth; green on revert); Unity
  suite EditMode 315/0 including the four `TwoRunIsolationTests`, PlayMode 23/0. The one
  PlayMode failure the first run surfaced — `CoreLoopPlayModeTests` hatch damage routing —
  was the test building an empty scene without the serialized `_game` wiring the authored
  scene carries; the test now wires it by reflection, the suite's established pattern.
  File moved to `3. completed/`.
- 2026-08-12: **M45 review closed the execution gaps.** Criterion 5's recorded narrowing said
  the enemy live list was spawner-owned; the static `EnemyGatekeeper.Gatekeepers` list and
  `AllLive` remained with three call sites — retired now (`WaveSpawner.Live` is the one
  ledger; `PlayerAbility` reaches it through the bound controller, the provoke shout through
  `_spawner`). `DecoyBay` now adopts `RunContext.Decoy` at Bind instead of a parallel
  `DecoyState`. Criterion 1 corrected in an edit that says so: "references only CoreModule"
  is not expressible in asmdef JSON; enforcement is the offline shim compile plus the
  reflection tooth. Criterion 9's residual `Time.` sites named as exemptions (attribution
  window, frame latch, hub lockouts). Roadmap row and Current moved to `2. active/`; status
  string normalized to `next (current)`. Unity suite with the isolation tests still owed.
- 2026-08-12: **M45 executed** (agent, on Zo's go; Unity gate pending one editor focus).
  Sim asmdef landed (`noEngineReferences: false`, no references; Boundaries rule rewritten
  to testability + one-directional dependency); `Settings` moved to `Game/Settings/` (Sim
  holds zero static mutable state, reflection-asserted); `Log.cs` rides in the Sim assembly;
  harness split two-stage (Sim compiles alone against Shims). `GameController.Instance`
  retired (RunContext scene host; serialized refs + Bind pushes; singleton fence now
  `HatchStation` only). 31 scene-lookup lines cleared to six named exempt files; input is
  one `PlayerInputFrame` from `Controls.Sample()`; spawner delivery is tick-driven (no
  WaitForSeconds), draws seeded-only, stable spawn ids from `RunContext`; the Bastion
  advances in a written order from `GameController.Update`. Gate re-cut per its own
  Verification step 4 (both original teeth green at HEAD; third tooth — no static mutable
  Sim state — was red at HEAD on `Settings`' four statics). Three new lint fences replace
  the milestone's manual greps. Poison proofs: shared meta, lint-evading static accessor,
  ambient random — all fail the gate. Harness 283/283. File moved to `2. active/` pending
  Unity suite + scene binding wiring validation.
- 2026-08-12: **M42 completed.** One save-migration path, one falloff/repair/slot-select,
  cut APIs deleted, kill lines quote banked amount, `LossReport`→`RunReport`, 43/43 R5
  labels, one `Game/` namespace. Harness 279; AutoVerify EditMode 311 / PlayMode 23.
  File moved to `3. completed/`.
- 2026-08-12: **M45 refined** (Zo accepted review recommendations). Criterion 12 locked to
  Sim-only `RunContext` isolation (two `MetaState`s); `DecoyBay.Instance` dropped from
  criterion 5; HatchStation kept as locator; gun crit RNG stays ambient per Architecture;
  editor play removed from criterion 14; kill criterion and Implementation signatures added;
  live counts refreshed (2026-08-12 audit).
- 2026-08-12: **M36 completed** (Zo playtest: Hatch always-on host accepted for this stage).
  Criteria 8–11 closed; file moved to `3. completed/`.
- 2026-08-12: **M46 cancelled** (Zo). Authored-panel rewrite not a roadmap slot; station
  polish remains the unfiled Current cut. File moved to `5. cancelled/`.
- 2026-08-12: **M43 cancelled** (Zo). Line-ending phantoms and LFS adoption already true;
  history is 21 commits so the orphan rewrite is no longer cheap. Root `gkb_*` / `_to_delete`
  cleanup stays a chore. File moved to `5. cancelled/`.
- 2026-08-12: **M36 active — Hatch always-on host.** Gate split into agent-closable (scene,
  bindings, drawer removal) vs Zo-closable (screenshots / play bands). Completion bar forbids
  `3. completed/` until playthrough notes land. File in `2. active/`.
- 2026-08-12: **M36 rewritten — Hatch always-on host, not the tether instrument** (Zo). Dropped
  salvage pulses, bodies-at-door marks, Prep/Assault re-skin and directional hit marks. Gate is
  a fifth `Hud Hatch` card on the top edge centre (Shift Health-row recipe), drawer Hatch bar
  removed, Decisions table extended on ship. File renamed
  `M36-the-hatch-is-always-on.md`.
- 2026-08-12: **M34 cancelled** (Zo). Region order stays in `Architecture/7-File-Layout.md`
  for new files; tree-wide sweep and `Tools/lint-layout` are not a roadmap slot. File moved
  to `5. cancelled/`; Architecture "in flight" row retired.
- 2026-08-13: **BastionSegment door along + craft-safe bake.** Per-edge Door/Window along offset
  (metres from centre). OnEnable no longer rebuilds when `_geom` already has children; NavMesh /
  obstacle place suppress validate rebuild so hand craft is not wiped.
- 2026-08-12: **Bastion authoring workflow + field terminals.** Working-Rules: agent seeds
  sketch/obstacles, Zo hand-finishes, NavMesh bake; menus for Place Obstacles / Bake NavMesh.
  missions-and-destinations: reuse Ship console presentation; wing T opens D; distinct Control
  Master terminal; T feeding a locked D starts a Wave (door after clear).
- 2026-08-12: **Control Room multiple 76 → 47** (footprint 235 × 376 at scaler 5×8).
- 2026-08-12: **M33 completed.** `Tools/docgen` splices ten countable blocks into
  `Design/4-Data.md`, repairs Field-Manual against `WeaponWheel` / `UpgradeTracks`, and
  `--check` runs from `Tools/verify/run.sh`.
- 2026-08-11: **M47 unlock graph + sketch emit.** Sim `BastionUnlockGraph`; Game door/terminal/layout;
  line-oriented `Tools/bastion-sketch/` + `gkb_bastion_sketch_request.txt` emitter. Kit enums/data
  moved to Sim (no UnityEngine in BastionKit). Verification blockers remediated (Sim purity,
  Validate terminal ids, exactly-one control door, example spacing, InitializeOnLoad allowlist).
- 2026-08-11: **M47 map heuristics H/S/T/D.** Defend at Hatch; terminals open locked doors and
  pressure spawns; Control Room (76) last in unlock graph; unlock order authored independent of
  spatial distance. Layout + graph in M47; terminal runtime stays missions-and-destinations.
- 2026-08-11: **M47 = palette artifact + bastion as done-proof.** Zo: both. Prefabs alone do not
  close; producing an authored kit bastion (shape, sightlines, M37 medians) is the verification.
  Criteria 1–5 palette; 6–17 produced bastion.
- 2026-08-11: **M47 gate = palette, not a bastion.** First cut; superseded same day by
  palette + produce-bastion verification above.
- 2026-08-11: **M47 kit scaffolding.** `BastionSegment` with per-side Wall/Door/Window/None,
  `BastionKit` scaler table, EditMode footprint tests, `BastionKitEmitter` via sentinel
  `gkb_bastion_kit_request.txt` (no menu row). Prefab emit pending Unity import while the
  editor holds the project lock. Storey height locked at 5; side sight/walk table in the
  milestone.
- 2026-08-11: **M47 per-side Wall / Door / Window / None.** Four sides on every segment, toggled
  without rebuilding the footprint; obstacles are the remaining authoring after sides are set.
- 2026-08-11: **M47 corrections (Zo).** "Unit" renamed **scaler** (math, not a player concept).
  Compose from **Control Room outward**; Hatch is a segment shape some distance away. No connector
  kit — openings are wall add/remove on segments; generate→tune→author is emit segments, set wall
  state, freeze. Soft lane/corner criteria met by wall and obstacle placement. Two-path split
  collapsed again.
- 2026-08-11: **M47 unit = 5×8; rooms are multiples.** Quarter 5→25×40, Half 8→40×64, Full
  13→65×104, Open 21→105×168, Control 76→380×608. (Naming corrected to scaler same day.)
- 2026-08-11: **M47 building blocks, not envelope.** Zo: the five sizes are room blocks that
  compose a bastion — Hatch may be stacked rooms; Control Room is a chamber you place.
  Reference composition: hub with Quarter/Half/Full around it. Criterion 8 no longer requires
  exact 30×52.
- 2026-08-11: **M47 map units from the golden mean.** First pass; superseded same day by the
  unit×multiple table above.
- 2026-08-11: **M47 gate rewrite — paths are not disjoint; generate→tune→author.** Dropped the
  Path 1 / Path 2 split and the "no visual change" intermediate close. End state is one authored
  multi-level bastion. Segment kinds: HatchChamber (freeze of today), LongLane,
  ConcealedApproach, VerticalConnector. Draft writer may generate into the open scene once;
  Build All must not regenerate playable geometry; the draft tool dies or parks under
  `_to_delete/` at freeze. Path-length band locked at ±25% of median. Architecture §4 and
  Decisions amend on the same commit as the freeze. Roadmap row updated.
- 2026-08-11: **M47 derived from the missions-and-destinations brainstorm — the bastion gets
  height and angles.** Zo's observation is the premise: firing lanes make the player very
  effective, corners and blocking walls give gatekeepers time to close unshot, and the current
  30 x 52 room only has the first half. **The heuristic it makes checkable: the player cannot
  see or kill at a spawn point** — arrivals come from off the accessible map through a concealed
  approach, and the check is a raycast grid from the Hatch chamber to every spawn, all blocked,
  plus the first two seconds of travel out of sight.
  Originally cut into two paths (authored freeze, then the space). Superseded the same day by
  the generate→tune→author rewrite above.
  **Hunters are deliberately outside the lane design** (Zo) — `MayTargetHatch` is false for them,
  so the approach work does not describe them and is not meant to. Recorded so a later reader
  does not read it as an oversight.
  Depends on M37 so the median-move criterion has a before to compare against.
- 2026-08-11: **M37 Zo-closed.** Criteria 4 and 8 overridden; within-run hatchDist halves
  already differed (29.1 vs 17.0 on `20-02-31`). Remaining table removed.

- 2026-08-11: **M37 completed — where they die.** Kill lines carry planar `hatchDist=` /
  `spawnDist=`; one `SpawnedAt` on the Gatekeeper, assigned in `SpawnOne`;
  `wave-report` prints per-Wave per-kind median and share beyond 25 m. Criterion 8
  (Hatch-bay vs far-wall medians) and a live Wave play check remain open on the file.
  Moved to `3. completed/`.

- 2026-08-11: **M40 + M41 completed.** Moved to `3. completed/`. Build record already on
  this file (same date); status catch-up so the folder matches the shipped work.
  `2. active/` empty; next recommended remains M36.
- 2026-08-11: **M40 + M41 built — the architecture rules are decided, then checked.**
  M40: the counted-line unit is stated once (`Architecture/7-File-Layout.md`) and linked
  from everywhere else; `Editor/` gets its first cap, 800 counted lines, higher because
  editor assemblers and scene-strip tools carry long serialized-wiring sequences
  (largest today `StripSettingsToStations.cs`, 791, measured 2026-08-11); the
  singletons are named by type — `GameController` and `HatchStation` grandfathered,
  `DecoyBay.Instance` recorded as owed with its three call sites, and
  `StationPanelView.EnsureInstance()` recorded as since-removed so it is not
  rediscovered. `5-Audit-2026-08-09.md` takes an appended correction: its file-size
  figures match no measure now in use, and `HudController.cs`'s overage (613 counted
  against 400) is baselined by name. M41: `Tools/lint-arch/run.sh` reads every rule
  from `arch-lint` fences in the document that states it (`Architecture.md`,
  `Architecture/3-Unity-Runtime.md`, `Architecture/README.md`) and runs from
  `Tools/verify/run.sh` beside the language check; every row is enforced at zero or
  baselined by a count in its fence (over-cap 1, fake-null 23 files,
  `WaitForSeconds`-in-loop 1), and a missing fence is exit 2, never a default.
  Milestone-ID identifiers (file, type, namespace, menu, define) are enforced at zero;
  milestone references in comments and strings are baselined (66 lines / 39 files,
  2026-08-11) with the player-facing ones cleaned and the rest a follow-up. `Assets/_Scratch/` exists, gitignored
  except its `.gitkeep`, and verify fails while it holds anything else; the one-shot
  ladder lands in `Working-Rules.md`. With M39 cancelled, criterion 12's 266/282 count
  expectation is replaced by re-measurement on the first clean run; one pre-existing
  PlayMode failure (`WeaponAbilityPlayModeTests.ShoveDoesNotUncommit`) blocks a green
  verify and is outside M41's scope.
- 2026-08-11: **M35 completed — Zo close.** Moved to `3. completed/`. Softens named on the
  file: criterion 5 admits the two station-shell sentinel tools until a follow-up deletes
  them; section D panel *detail* and criterion 41 screenshots deferred to an unfiled
  ship-terminal UI milestone; criterion 42 closed by Zo's verdict. Bindings report
  `missingCount:0` (`2026-08-11T02:00:47Z`).

- 2026-08-10: **M44 completed — trigger gate 50/50.** Moved to `3. completed/`.
  `.claude/skills/_evals/run.sh trigger` passed all five skills (6 should + 4 near_miss each,
  `failures: []`). Descriptions sharpened on the design↔brainstorm and development↔verification
  boundaries after two earlier should-misses. Quality remains opt-in (`EVAL_QUALITY=1`), not a
  close gate. Always-on description budget re-measured at 1,022 (chars÷4) after the sharpening.

- 2026-08-10: **M44 built — five skills live at `.claude/skills/`, all model-invocable.**
  `gkb-development`, `gkb-design`, `gkb-verification`, `gkb-brainstorm`, `gkb-strategy`, plus an
  eval harness at `_evals/`. Criteria 1 to 6, 11, 14 and 15 close; the eval runs need a session
  with the `claude` CLI.
  **Criterion 4 was corrected during the build (Zo): manual invocation was the wrong default.**
  Two skills shipped with `disable-model-invocation: true` on the reasoning that models
  under-trigger for work they can attempt unaided. The real usage pattern settles it — the
  request that should load a skill is phrased "what should I work on next", never as a command
  naming the skill — so a manual skill provides nothing and the description cost is what buys the
  ability to fire at all. **The failure mode moves from under-triggering to co-firing**, and every
  description now ends by naming its adjacent skill.
  An adversarial review of the first draft found the eval runner would have produced meaningless
  numbers three ways, each looking like a legitimate result: **skill firing was undetectable**
  (the result envelope carries no tool-call records, so the check was grepping prose for the word
  "Skill"); **the two manual skills were condemned by construction** (unable to load from a prompt,
  so both configurations were identical, scoring the exact parity that the milestone deletes on);
  and **every API failure recorded a zero-token non-firing run and exited 0**. The runner now
  reads `stream-json` tool-use events, counts only the skill under test, and dies loudly. A
  `selftest` mode runs first because a broken detector reports every skill as useless.
  Also caught: `gkb-development` shipped the exact banned pattern — a count inside a rule — that
  `gkb-verification` cites as the project's worked example of one.
  **Recorded because it will recur: `.claude/` cannot be written through the Cowork device
  bridge.** Files land elsewhere in the repository and are copied in with a shell command.
  **Path-scoped after a second correction (Zo): the skills have different homes.** Development
  and verification happen almost exclusively in Cursor; brainstorm and strategy almost
  exclusively in a planning session. `paths` is the mechanism, and both toolchains document it
  as restricting rather than adding — Cursor: "the skill is only surfaced when the agent works
  with files that match"; Claude Code: "Claude loads the skill automatically only when working
  with files matching the patterns." So `gkb-development` and `gkb-verification` are scoped to
  C#, tests and `Tools/`, and vanish from a docs session.
  **The other three are deliberately unscoped**: their work is conversational and often has no
  file in context, so a path scope would make them absent exactly where they are wanted.
  **`Docs/_meta/_definitions/` stays where it is** (Zo). Criterion 13 previously moved it into
  the skills; a skill is not a second home for an authoring contract, and `language.md`'s
  machine-read fence must not move away from the check that parses it.
- 2026-08-10: **Multiplayer work pulled out into M45, and M39 cancelled** (Zo: move related items
  into their own milestone, research the architecture, do not over-engineer or copy a AAA dev).
  **The strict `noEngineReferences: true` rule did not survive the research.** It is load-bearing
  for exactly one thing, cross-platform bitwise determinism for lockstep or deterministic
  rollback, and this project is doing neither. Unity's own docs say NGO does not implement
  prediction and reconciliation; NGO's documented physics model runs Unity physics on the server,
  which a pure sim cannot call; and Boss Room, Unity's official co-op sample, puts game logic in
  `NetworkBehaviour` and uses `NavMeshAgent` server-side. The one architecture that mandates
  engine-freedom, Photon Quantum, states determinism as the reason.
  **Overwatch is retired as a citation for this rule** — its simulation is its engine, and the
  separation it describes is by tick rate, not type purity.
  What survives is the dependency direction: `Sim/` may use `Vector3`, `Quaternion` and `Mathf`,
  and may not touch a MonoBehaviour, a scene, `Time` or `Random`. The stated reason changes from
  "the multiplayer seam" to testability and one-directional dependency.
  **M45's scope is the retrofit that is cheap now and ruinous later**, gated on one EditMode test:
  two independent Run simulations in one process, which fails the moment a static comes back.
  Measured 2026-08-10, the tree is close — three singletons, seven scene lookups, one gameplay
  `Instantiate`, two gameplay random draws, seven files reading `Input`, and zero `Time.` in
  `Sim/`. Netcode model recorded as host-client over Steam's relay, client-authoritative
  own-movement, and **no library is named** because the work transfers between all of them.
  Prediction, rollback, lag compensation, dedicated servers, host migration, anti-cheat, custom
  serialization, interest management and an ECS rewrite are all rejected by name on the file.
  **`Milestones/5. cancelled/` is now in use**, which forces the open question in this file's own
  Milestone-files section: the contract says four folders are the set. Either it gains a fifth
  row or the folder goes — still Zo's call, now with an occupant.
- 2026-08-10: **M35 gate rewritten off the prefab-generator scope.** Roadmap row and
  Recommended-next no longer describe Build UI Prefabs / `GUILayout` menu / SceneBuilder-
  instantiated UI. Gate is four authored scenes, one Shift theme, generators deleted;
  SceneBuilder owns environment/spawns/light/NavMesh only. Docs sweep under criterion 8 /
  WP-5 (`AGENTS.md`, `Architecture.md`, `Decisions.md`, `M36`).
- 2026-08-10: **M44 added — the working method becomes five skills, each gated on a measured
  delta** (Zo: the third step-function milestone, with token efficiency as the through line).
  Placed at `.claude/skills/`, which Claude Code reads natively and Cursor reads as a
  compatibility path, so **one location serves both tools and nothing is duplicated into
  `.cursor/rules/`**. The loading model is the whole argument: a description costs ~100 tokens
  and is always present, a body costs nothing until it triggers, and **a bundled script's
  source never enters context at all** — only its output. Five descriptions ≈ 500 tokens
  against `AGENTS.md`'s 1,420 words, and `Docs/` is 139,568 words that nothing routes.
  **The gate is a with-skill / without-skill pass-rate delta, and a skill at parity is deleted
  rather than rewritten** — it is documenting something the model already does. Two of the five
  (`gkb-brainstorm`, `gkb-strategy`) are `disable-model-invocation: true` and cost nothing until
  invoked, because models under-trigger skills for work they can attempt unaided.
  Two rules the research changed rather than confirmed. **`== null` versus `?.` is a
  presentation-layer rule, not a global one** — the lifetime check exists because a destroyed
  `UnityEngine.Object` keeps a live C# wrapper, and `Sim/` contains no such object; the blanket
  version is why M41 baselines 23 files. And **the allocation list is scoped to per-frame paths
  only**; applying it to editor tooling is how a codebase gets unreadable for no measured gain.
- 2026-08-10: **M42 re-cut as the code-cleanliness milestone; the git normalisation moves to
  M43 unchanged.** A six-lens audit with adversarial verification (40 findings raised, 28
  confirmed — `Architecture/5-Audit-2026-08-10.md`) found the tree's debt is duplication, not
  structure: two save-migration pipelines already divergent on the `-1` ledger sentinel, two
  falloff copies where the tested one is not the live one, a kill log that rounds while the
  bank floors, and a cut economy mechanic still shipping a tested API. The new M42
  (`one-mechanism-per-job`) deletes the second copies, makes four lying names true, restores
  the R5 label and one-namespace conventions, and pins the log to the bank. Zo's call on the
  renumber; the stable-ID rule bends once because M42 had not shipped and only Roadmap.md
  referenced it. Deliberately NOT in M42, recorded so they are not re-raised: DecoyBay's
  singleton (M40/M41 own it), fake-null/LINQ (M41 fences them), the PodEjections full rename
  (~88 refs for two demonstrated errors), and the decoy wire-or-cut — the audit's one open
  decision, FABRICATION sells a payload with no reachable in-run activation path.
- 2026-08-10: **M42 added — the repository is normalised once, and the milestone is written
  for an agent to execute.** The project gained a remote (`DeveloperZo/GatekeeperBastion`,
  one commit) and immediately showed **141 tracked files as modified with zero real changes**;
  `git diff --ignore-cr-at-eol` across the whole tree prints nothing. That breaks `git status`
  and `git diff` as signals in the same week this file's own rule — a status change is a
  `git mv` plus the table row, in one commit — starts to matter.
  Two decisions were recorded in `Decisions.md` rather than left to the run.
  **Unity YAML stays in git, not LFS**: asset serialization is Force Text, so `.unity`,
  `.prefab`, `.asset` and `.meta` are text — and `.unity` is the largest category on disk at
  22.7 MB, which makes it exactly what an optimiser reaches for first.
  **`.gitattributes`, not `core.autocrlf`**: the config lives on one machine and does not
  travel with the repository, so the problem returns on the next clone.
  Also rejected: `git lfs migrate`. History is one commit called `Initial commit`, so a fresh
  orphan root commit reaches the same place with fewer failure modes.
  **M42 is the first milestone here written for a coding agent**: every check states its
  expected output, four are hard aborts, and criterion 5 checks the staged set in *both*
  directions — the "must be present" column exists because two files of real M35 work
  (`UiEventSystem.cs`, `MainButton.prefab`) have never been committed, and a rewrite that
  only verifies exclusions would drop them.
- 2026-08-09: **The architecture refactor enters the table as M39, M40 and M41, not one
  milestone** (Zo: the next tent pole is a refactor to align with the Architecture principles).
  It was drafted as one and split after two adversarial reviews, on the one-grain rule: the
  three have different dependency chains and different risk. **M39 depends on nothing** —
  not on M35 — and its gate is a compile error. **M40 is documents only.** **M41 is the lint
  script and holds all the risk.**
  Four claims in the single-milestone draft were false and are recorded because each is the
  obvious thing to believe. **The file-size caps do have a stated unit** —
  `Architecture/7-File-Layout.md:55`, "non-blank, non-comment, non-directive" — so M40 is
  reconciliation, not a decision; the draft claimed the unit had never been chosen.
  **The two audits' disagreement is not explained by raw-versus-code lines**: `MetaState.cs`
  is recorded at 590 and is today 644 raw and 550 code, matching neither, so M40 corrects
  `5-Audit-2026-08-09.md` with that rather than with the tidy explanation.
  **Thirteen of `Sim/`'s fourteen `Log` calls have never executed** — they are `Log.Info`,
  which is `[Conditional("GKB_VERBOSE")]`, and `GKB_VERBOSE` is defined nowhere in the
  project; M39 deletes them rather than rerouting them into the log for the first time.
  **`Architecture/README.md:15`'s "two are grandfathered" names neither and there are three** —
  `GameController`, `HatchStation`, `DecoyBay` — so M40 names them by type, because a
  count-based rule passes forever by renaming.
  Also cut, and recorded so it is not re-proposed: **making `Log.Event` `[Conditional]`.**
  The reasoning is sound and the target does not exist — this project produces no release
  build, and every measurement it has ever made comes from an Editor session log.
  Revisit when a non-development build is actually produced.
- 2026-08-09: **M38 dashboard re-cut to in-browser parse** — `Docs/dashboard.html` reads
  `GatekeeperBastion_*.log` via folder/file picker (JS). Root `Dashboard/` + `--dashboard`
  snapshot removed; `file://` cannot scan `Logs/` alone, which is why the picker exists.
- 2026-08-09: **M38 completed** — first cut used generated `data.js`; superseded same day by
  the in-browser page above. Criterion filters (PlayMode / short / eras / spend) kept.
- 2026-08-09: **M37 rewritten and M38 split out, after an adversarial review of the draft** (Zo:
  fix the gaps, run a subagent to grill yourself). Three findings held.
  **The formula was broken**: `penetration = 1 - dist/spawnDist` clamped at 0, and the lower clamp fires
  in exactly the three cases worth measuring — a Hunter dragged past its spawn, a body pulled by M30's
  decoy, any knockback — all reading 0 and indistinguishable from dying on the spot. It also assumed
  bodies spawn at the spawn point, which `GroupSlot` disproves. **M37 now logs two raw distances and
  no derived value in the game.** **The exclusion rule would have deleted its own evidence**: the draft
  filtered logs containing `AutoVerify`, and `15-21-59.log` contains it — the log every current balance
  claim rests on. The discriminator is `stage=playmode`. **And the draft bundled two grains**, so the
  dashboard work is now **M38**. Cited numbers corrected against the shipped tool: mean live **3.78**
  not 4.01, mean lifetime **3.74 s** not 3.98, **12** session logs not 18, and M30 is shipped rather
  than in progress. `playthrough.md` carried the wrong pair and is corrected with a note saying so.
- 2026-08-09: **M35 code migration landed; close-out blocked on Editor** — player `OnGUI`
  removed (Dev only); `Game/Ui/` Theme + views; SceneBuilder instantiates `Prefabs/Ui` when
  present. Prefab build / scene rebuild / playthrough still open on the file (Unity project
  lock + MCP down). Roadmap row → **active**, link `2. active/M35`.
- 2026-08-09: **M32 Path 1 shipped** — `PRI-C` catalog row (id 6), full damage indoors
  (`falloffStart` 55), sustained dps 36.5 within 5% of MR-35, Railshot arms next shot
  (3→8 bodies at full damage, 12 s / 4 s arm). Ring **C D R A F** (Clip/Reload swapped vs
  MR-35 — M21 uniqueness). Playthrough criterion 7 open on file. Moved `2. active/` →
  `3. completed/`.
- 2026-08-09: **M37 added, and `GatekeeperZo Notes/verification.md` populated** (Zo's play report:
  Wave-3 start still felt easy, the pin seemed not to work, and a request to measure Gatekeeper distance
  moved before dying). M37 takes the metric with one change: raw path length needs per-agent accumulation,
  which `Decisions.md` forbids, and it conflates spawning far away with getting deep — so the kill line
  gets **distance from the Hatch** and **penetration normalised per spawn point**, both read once at death.
  The session (`15-21-59`, clean) measured mean live **4.01**, mean lifetime **3.98 s**, Hatch 300/300 and
  **zero arrivals** across 90 bodies, corroborating the report. **The Pin Shot has now failed to fire in
  three consecutive sessions, including after M31's fix** — recorded as a defect and the first item on the
  verification list. `verification.md` condenses every open row across shipped milestones into four
  next-Run items, a when-convenient set, and a not-play set to ignore during play.
- 2026-08-09: **M26 Path 1 shipped** — `SEC-D` catalog row, Dot+coral identity, kill lines
  name the slot (`KillLog`), both Secondaries free. Ability + ring remain Zo Open on the
  file; Path 2 / playthrough open. Moved `2. active/` → `3. completed/`.
- 2026-08-09: **M35 and M36 corrected against the actual Shift import, and M35's scope widened**
  (Zo: M35 should cover a baseline UI including menu items and splash). Reading the package rather than
  the store page changed two claims. **Shift ships no HUD component of any kind** — its component list
  is menus, panels and a server browser — so it covers the Ship and the front end and gives M36's combat
  HUD nothing. And **Shift has no DOTween dependency**; it animates with Animator clips and coroutines,
  so DOTween is the game's own motion layer only. Also pinned: namespace `Michsky.UI.Shift`, v2.0.12,
  `StandaloneInputModule` since the project is on legacy Input, and a colour mapping between the five
  fields on Shift's `UIManager` ScriptableObject and the nine tokens in `Ui/Theme.cs` — four of which
  have no Shift equivalent. `QualityManager` is explicitly not adopted; M24 already shipped settings.
  **M35 now specifies a baseline front end** (splash, main menu with a hidden-not-disabled CONTINUE,
  options, overwrite modal, station panels, end screens) and names what is deliberately excluded:
  pause menu, credits, rebinding, save slots, localisation.
- 2026-08-09: **M35 re-cut to a uGUI migration, and M36 added** (Zo purchased Shift, Complete
  Sci-Fi UI, and asked for UI work that gives the game a distinct feel). M35 was a token layer inside
  IMGUI; with a component kit in hand the cheaper path stopped being the right one, since IMGUI has no
  layout, no reuse and no animation. Stack decided: uGUI + TextMeshPro, Shift for components, DOTween
  Pro for motion, CanvasScaler for scaling, True Shadow sparingly, **dev surfaces stay IMGUI**. UI lives
  in prefabs under `Assets/Prefabs/Ui/` because `SceneBuilder` regenerates every scene (gotcha 4).
  `iTween` is deleted. **M36** is the feel pass built on it: the Hatch bar and Gatekeeper counter are
  replaced by one tether readout carrying integrity, salvage pulses and bodies at the door; Prep and
  Assault become different instruments with the Siphon Draw as the transition. Its gate is three
  screenshot tests a stranger can run, because a feel milestone that cannot prove itself is a chore.
- 2026-08-09: **M28 shipped** — `Tools/lint-language/run.sh` reads the `language-lint` fence
  in `_meta/_definitions/language.md`; retired terms, `Docs/Play/` builder vocabulary, and
  Title-Case station names in `Assets/Scripts` strings fail with `file:line`; wired into
  `Tools/verify/run.sh`. Live `Armory` HUD / cutter copy corrected to `ARMORY`.
- 2026-08-09: **M29 shipped** — `Tools/wave-report` (mean live / peak / mean lifetime,
  `--summary` `--csv` `--html`); group line `— N live`; `bought … track=`; `run begins`
  stable fields. No counter under `Assets/`. Pin logs `2026-08-07_16-34-30` and
  `2026-08-03_19-18-22` absent on disk; playthrough Verification §7 and EditMode confirm
  remain open on the file. Stand-in check:
  `Logs/GatekeeperBastion_2026-08-08_17-00-15.log` → `5.00 / 11.30 / 9.88`.
- 2026-08-09: **M27 shipped** — NAV MAP premise (garrisoned Bastion / Hatch = way home),
  end screens (`thrown off the Bastion`, `out through the Hatch`), `Gate` removed rather than
  redefined (`language.md` trap 6). Criterion 6 (cold-start ask) and the four-surface
  playthrough remain open on the file. Unblocks M28's first prose target. AutoVerify
  EditMode 265/0 including `PremiseCopyTests`.
- 2026-08-09: **M30 Path 1 shipped** — drone bay + decoy payload; SaveVersion **6**;
  DecoyTests 9/0 (suite 247/0 harness). Path 2 AutoVerify and playthrough remain open
  on the file. `2. active/` no longer holds M30.
- 2026-08-09: **M31 criterion 6 closed** — Zo set MR-35 `pierceDamageMul` to **0.26**
  against `FocusAtMax` 50 (`78/297.5`). Playthrough criteria 7/10 still open on the file.
- 2026-08-09: **M31 Path 1 shipped** — `pierceDamageMul` (then MR-35 0.5 flat), falloff
  table, pin-on-first-body. Criterion 6 was blocked until the 0.26 retune above.
  Unblocks M32 / M29 Path 1. M20 takes a correction for the pin defect.
- 2026-08-09: **M35 added — the UI has no system, and that is measurable** (Zo: the UI is
  troublesome and needs a step up, not professional polish). Seven drawing files hold **83**
  `new Rect(` literals, 294 two-digit numeric literals and **20 `new Color(` calls with no two
  identical** — the Hatch amber differs between files by 0.05, warning yellow appears three ways.
  M35 adds `Game/Ui/` (Theme, Draw, Layout) and redraws every existing screen through it with no
  change to what any screen says. **Ordering revised in the same pass**: M35 now leads, M31 and
  M30 follow, M27 drops out of the top three. The earlier order ranked only what was on the board.
- 2026-08-09: **Documentation drift pass over `Roadmap.md`, `Changelog.md` and
  `Milestones/`.** Structural edits, no gate softened. `Roadmap.md`: the rule text requiring a
  `Done when` line replaced by the numbered acceptance criteria the milestone contract has
  required since 2026-08-04; M27 and M30 rows moved to **active** with their `File` links
  repointed at `2. active/`; M34 and M35 rows added, both files having existed with no row;
  five `File` links whose capitalisation did not match the file on disk corrected (`M10`,
  `M2`, `M19`, `M14`, and `Brainstorms/gatekeepers-and-waves.md`) — they resolve on a
  case-insensitive filesystem and 404 elsewhere; the *upgrade curve is smaller than the noise*
  risk rewritten against `WeaponWheel` (one purchase is `1.0456^5` = +25% on a focused spoke,
  above the ~20% run spread, so the risk is now the 9.30x ceiling rather than the step);
  `Milestones/5. cancelled/` recorded as existing and contradicting the four-folder rule,
  left for Zo. `M27` and `M30` frontmatter `Status:` lines corrected from `later` to match
  the folder they sit in. Corrections appended, never retro-edits, to **M20** (criterion 4's
  dial range is 0..50, not 0..35), **M21** and **M23** (`SaveVersion` has moved past the 4
  they record). **M31** carries a dated note that its pierce justification was sized on a
  35-point focus that no longer exists, with both sets of arithmetic and the multiplier left
  to Zo. **M32** and **M33** corrected off the same dead 35-point anchor, and M33 gains
  criterion 15 so a generator built to it cannot bake a point ceiling in as a literal.

- 2026-08-09: **`SaveVersion` 5 → 6, during M30.** `SaveData.CurrentVersion` is **6**
  (`Sim/Meta/SaveData.cs:28`): `EquippedDrone` now holds a `PayloadId` rather than the
  `WeaponId.None` sentinel, and `OwnedPayloadMask` is new; pre-v6 `EquippedDrone` values of
  -1 or unknown sanitize to `PayloadId.None` (`Sim/Meta/SaveData.cs:19`). This is the bump
  M30 reserved and it landed while M30 was being built. **Invalidates the v5 claims in the
  entry below and in M30's own `## Depends on`**, which both read v5 as current; v5 is now
  history, not the head. Recorded the same day rather than late, which the entry below was.

- 2026-08-09: **`SaveVersion` 4 → 5, recorded late.** The bump landed on 2026-08-08 with the
  third weapon slot's rename from Melee to Drone and had no entry here until now.
  `SaveData.CurrentVersion` reached **5** that day (`Sim/Meta/SaveData.cs:15`, version
  history); the JSON key moved
  `"EquippedMelee"` → `"EquippedDrone"` and the field is `SaveData.EquippedDrone`, alongside
  `WeaponSlot.Drone`, `ControlId.SlotDrone` and `MetaState.EquippedDrone`. **Deliberately not
  backward compatible** (Zo, 2026-08-08): the slot has never held anything, so a v4 save loses
  a `WeaponId.None` and nothing else, and no shim reads the old key
  (`Sim/Meta/SaveData.cs:16`; `_meta/_definitions/language.md`, trap 3). **Invalidates**
  every "`SaveVersion` 4" claim written before this date — M21's criterion 4, M23's `Done when`
  and `What shipped`, and the 2026-08-08 entries below saying it stays 4. M21 and M23 carry
  dated corrections rather than retro-edits; the entries below are history and stand as
  written. **M30's bump was v6 and it has now been spent** (entry above). Logged here on 2026-08-09
  because this file is the audit trail for exactly this transition and it did not have it.

- 2026-08-09: **`WeaponTier.MaxTier` 7 → 10, recorded late.** M21 shipped the change and this
  file never carried it. `Sim/Meta/WeaponTier.cs:9` sets `MaxTier = 10`; `Cost` is
  `400 + 200 * currentTier` over `currentTier` 0..9, so **400 through 2,200** per step and
  **13,000** cumulative for a full track, against M11's 7 steps and 7,000.
  `WeaponWheel.MaxPurchases = WeaponTier.MaxTier`, so a full focus is
  `FocusAward 5 x 10` = **50** points to the centred spoke and `NeighbourAward 3 x 10` = **30**
  to each neighbour, over 110 points total. **Invalidates every figure sized on a 35-point
  focus or a 21-point neighbour.** A full damage focus is `DamagePerPoint 1.0456^50` = **9.30x**,
  not the 4.76x that 35 points gives, and `WeaponAbility.MaxDialPoints` is
  `WeaponWheel.FocusAtMax` = 50, not 35. Found in scope: M31's whole pierce justification
  (dated note added, and the pierce multiplier now needs Zo's decision), M32's Railshot dial,
  M33's generated `wheel` block anchors, and M20's criterion 4 dial range. The 2026-08-08
  entry below reading "the wheel multiplies it to 4.8x" is history and stands as written; the
  live figure is 9.30x.

- 2026-08-09: **M29 re-cut — the emit side folded in, and the two-stream idea declined**
  (Zo asked for more of the dashboard to be a straight read of logs, with separate dimensions to keep
  the playthrough log focused). **A second logger is already a rejected decision** — `Decisions.md`:
  *"A two-stream logger. One file per session, one write path."* Rather than reverse it silently, M29
  now makes three existing lines machine-readable on the same stream — an `AliveCount` suffix on the
  group line, a `track=` token on every purchase, and a stable field list on `run begins` — and gains
  `--html`. Mean live stays a computation: integrating live count at runtime would be a counter, which
  the same decision forbids. Queue unchanged at 11; no new file.
- 2026-08-08: **The MR-35's Pin Shot has never been able to fire, and the cause is pierce.**
  `RegisterPinHitIfNeeded` runs once per damaged body inside `FireRay`; `PinBuildState.RegisterHit`
  zeroes `Hits` whenever the target changes. Any shot through two or more bodies therefore leaves
  the build at 1 against a `PinHitThreshold` of 3, so the pin arms only against a Gatekeeper with
  nothing behind it. Six sessions, zero pin events. **The fix is folded into M31**, which adds the
  first-damaged-body flag for `pierceDamageMul` anyway; M20 takes a correction rather than a
  retro-edit. Found by reading the code after Zo reported pressing `F` and getting nothing.
- 2026-08-08: **M31 and M32 added — pierce and distance become costs** (Zo). Pierce is a base
  MR-35 stat at every Tier and the wheel multiplies it to 4.8x, so an upgraded shot deletes a
  line of three. M31 adds a per-weapon `pierceDamageMul` (MR-35 0.5, flat not compounding) and
  a falloff start on every Primary, deliberately leaving the far half of the room unowned; M32
  fills it with `PRI-C`. Decisions by Zo: 0.5 as a per-weapon field, MR-35 keeps pierce-2,
  falloff on every Primary with the sniper last. Planned queue now 9 of 10.

- 2026-08-08: **M21 moved to active** — upgrade wheel implemented (Path 1 EditMode 224/0 mcs).
  Path 2 ARMORY play, MK-1 ring, and opposite-focus feel remain open on the file. `SaveVersion`
  stays **4** (shared with M23). `Current` → M21.

- 2026-08-08: **M23 shipped.** Equipment slot + Repair Rig; Repair Rig / Engineering tracks
  deleted; `SaveVersion` 4; AutoVerify EditMode 227/0, PlayMode gate Passed. Playthrough
  path and price tuning remain open on the file. `Current` clears; `2. active/` empty.

- 2026-08-08: **M30 added — the drone bay and its first payload, graduated from
  `Brainstorms/fabrication.md`.** The bay and the decoy ship together because neither is a
  player-visible change alone. The decoy's supply is HP and its worth is stated in
  gatekeeper-seconds at the measured 3.43 effective dps, so the payload is priced in the
  same unit as the door it defends. It carries the `WeaponSlot.Melee` rename that M23 gave
  up when the rig became Equipment. `UTL-B` and its notes left the brainstorm.

- 2026-08-08: **M20 shipped** (Zo's verdict after play). Criterion 5, the gatekeeper-second
  bar, was closed by the player rather than by a figure recorded in the milestone; the file
  says so rather than implying a measurement it does not hold. `Current` moves to **M23**,
  which is cut with nothing open. M21 is unblocked — the fifth attribute now exists for the
  wheel to spend points on.

- 2026-08-08: **M29 added — an effort measure exists again.** M16's correction retired the
  Hatch-remaining band and left every balance gate on this roadmap without a reading.
  M29 ships `Tools/wave-report`, an offline reader over existing log lines that prints mean
  live Gatekeepers per Wave. Chosen over *time at full attention*, which was recommended
  first and then computed: it reads 98.1% on a comfortably cleared Wave and 80.1% on a lost
  one. Mean live separates the same four Waves 2.84 / 5.97 / 9.52 / 11.26 where Hatch
  remaining reads 300 on all four. Planned queue now 8 of 10.


- 2026-08-08: **M15 and M16 marked completed**, matching the folders they already sat in;
  both rows had 404ing `1. planned/` links. `Current` corrected from M15 to M20.
  **M16 carries a correction**: the Hatch-remaining instrument it tuned against measures
  leakage rather than effort and saturates at 100% for a skilled player, so its deferred
  ±15 play band is retired rather than pending (`Design/1-Experience.md`, section 1.3). Grading a
  curve now needs an effort measure that does not exist.
- 2026-08-08: Queue caps raised to **10 planned / 3 active** (Zo); was 3 planned.

- 2026-08-08: **M23 re-cut a second time — equipment, not a drone payload** (Zo).
  FABRICATION expanded to Drone and Equipment; the rig lives in its own equipment slot,
  unlocked at the first lost Run (`PodEjections >= 1`) and bought (initial 300 slot +
  300 rig). The drone bay left M23 entirely and ships with the first payload milestone;
  `WeaponSlot` is untouched. Criteria renumbered 1–10; buildable.
- 2026-08-08: **M27 and M28 added, out of the language pass.** The 2026-08-08 vocabulary
  inversion (Gatekeepers hold a Bastion; the player is the Boarder; the old terminal-loss phase name struck) landed
  in code, builder docs and the wiki in one pass, and in nothing the player sees — all 20
  pages of `Play/` contain no instance of *board*, *intruder* or *garrison*. **M27** puts the
  premise in front of the player and closes `Gate`, which currently means three different
  things across `MapStation`, `Play/Upgrades.md` and the language table's forbidden list.
  **M28** is the tooling exception: a committed check reading the retired-term list out of
  `_meta/_definitions/language.md`, motivated by `Boarder` reversing meaning rather than
  retiring — the one drift class that cannot be caught by reading, because nothing looks
  wrong. Not promoted to milestones, deliberately: the Field Manual regen, the VK-7 wiki
  page, the `MapStation` rename and the `BankPodded` comments are chores.

- 2026-08-08: **M23 re-cut around the FABRICATION drone** (Zo's call, the day the drone was
  decided — `Brainstorms/fabrication.md`). The rig is the drone's first payload rather than
  a carried Utility item: the third slot becomes the drone bay (`WeaponSlot.Drone`, JSON
  key unchanged), FABRICATION sells and equips it instead of the ARMORY, and the milestone
  now also builds the bay and its one-payload-per-run constraint. Criteria 8 → 9. No
  status change.
- 2026-08-07: **Queue hardened after a staleness audit, and M26 added.** Three subagents
  audited M23/M24/M25 against the code; the findings were bad enough to justify the pass.
  **Every milestone written on 2026-08-06 was missing the mandatory `## Verification`
  section** and carried `Status: planned`, which is not in the contract's vocabulary — both
  fixed across all six. **M24's stated premise was false**: `MainMenuController` already has an
  options panel, and the milestone said the game had none; it is re-cut around the real problem
  (the screen is unreachable in a Run and its one value is read once at `Awake`). **M25 was
  re-cut** from a setting with asserted-but-absent supporting information into an
  information-flow milestone — the warning and the discard log line did not exist and it
  claimed they did. **M23** absorbed six audit blockers, chiefly that deleting the `SaveData`
  fields makes its own migration criterion unimplementable, and that "push nothing when no rig
  is carried" leaves the serialized 8 HP/s standing so a rigless Run repairs normally. The two
  superseded files are in `_to_delete/superseded-20260807/`.

- 2026-08-07: **First five-Wave clear.** MR-35 T4, all five Waves, Hatch untouched, zero
  repair; an earlier T1 run lost Wave 3. Player report: **fun and semi-challenging.**
  Measurements in `Design/2-Dynamics.md`. An earlier version of this entry and of that section read
  the Hatch-HP numbers as proof the difficulty curve was broken and called M16's reopen
  condition fired — **withdrawn as an overreach**: Hatch remaining measures leakage, not
  effort, and it saturates at 100% for a skilled player across a wide range of real
  difficulty. No status change. The live question is whether Hatch-remaining can grade a
  curve at all, which is a question about the instrument.

- 2026-08-07: **M22 completed.** Kill awards floor once into bank and run tally; end
  screens show gross / repair / net; Dev-taint banner on Bastion HUD, end screens, and
  station panels. AutoVerify EditMode **213 / 0**, PlayMode **12 / 0**
  (`gkb_verify_report.json`, 2026-08-07T21:32:19Z). AutoVerify PlayMode kick also fixed
  (late EditMode `RunStarted` no longer zeroes the kick; kick resumes after domain reload).
- 2026-08-06: **Six milestones now sit in `1. planned/` and none has been built this
  session** (M20, M21, M22, M23, M24, M25, alongside M15 unverified and M16 open). Recorded
  here rather than in a brainstorm because this file's own risk list names it: *if a session
  spends longer in `Docs/` than in `Assets/`, that is the signal.* The queue is coherent, but
  it is now longer than the evidence supporting it — M16's difficulty curve and M1-style
  logged Runs are what should decide the order, and neither has run since M14 shipped.
- 2026-08-06: **The VK-7 is retired and unshipped rows go to codenames.** The VK-7 was kept
  disabled "until a weapon polish pass"; the pass arrived and it is not wanted, so it leaves
  the shop lists while its `WeaponId` value stays for save compatibility and is never reused.
  Placeholder weapons now carry `PRI-`/`SEC-`/`UTL-` codes instead of invented names — a name
  given before a weapon has an identity in play has to be defended later and fixes the fiction
  before the mechanic earns one. Both in `Decisions.md`. The roster still needs a rhythm
  extreme (huge magazine, punishing reload); it is `PRI-C`, a new row.
- 2026-08-06: **M23, M24, M25 added, all by Zo's decision rather than by a graduation test.**
  M23 makes the repair rig a carried Utility item and gates repairing on it, deleting
  FABRICATION's Repair Rig track and CREW QUARTERS' Engineering attribute — repair had three
  knobs and will have one. Note that repair is **not** gated today (`RepairPerSecond(0)`
  returns a working rate), so this is a rule change, not a codification. M24 builds the
  options screen the project has never had; M25 puts the reload-cancel-on-swap rule behind a
  setting and depends on it. `_meta/_definitions/brainstorm.md` amended in the same pass: the
  graduation test is a default for undecided entries, not a gate on the owner.

- 2026-08-06: **`Docs/Play/` became a 20-page wiki**, and `_meta/_definitions/language.md`
  added as the ubiquitous-language contract — one thing, one name, across code, docs, HUD
  and log lines. The wiki holds the existing generated/written split: no page hand-types a
  number or a key, every page ends in `## Gaps`. Written from the code by four agents, which
  was the point — the Gaps sections are a defect list produced by trying to explain the game
  to a stranger. `Docs/README.md` amended to admit `language.md` under an explicit exception,
  since it is a standing rule rather than a document-class contract.
- 2026-08-06: **M22 added** from two of those Gaps. The end screen's "earned this Run" is
  net of repairs but labelled as banked, and it is computed from the float run tally rather
  than the floored bank — **19 and 21 Salvage of drift** in the 2026-08-04 logs. Separately,
  `GameSession.DevTainted` correctly suppresses saving (M13's Watch) but says so only in the
  log, so a tainted session discards a play session with nothing on screen. Neither is a
  balance question; both are information flows.

- 2026-08-06: **M20 and M21 added — weapon upgrades become a choice.** Zo's design: each
  weapon carries a cyclic ordering of five attributes (reload, clip, damage, fire rate,
  ability); the player centres one on a wheel and buying pays 5 points to it, 3 to each
  ring-neighbour and 0 to the two opposite. Adjacency is the weapon's identity, so per-point
  values stay global and per-weapon tuning is one permutation with no numbers in it. Two
  entries graduated out of `Brainstorms/armory-and-weapons.md`. Found while specifying it:
  **no weapon has an ability at all**, which is why M20 is a separate milestone and blocks
  M21; and the save's per-weapon Tier cannot represent a mixed focus history, so it becomes
  five point counters with Tier derived. Reverses two shipped decisions — fire rate becomes
  upgradeable, and the flat Tier curve is replaced — both to be recorded in `Decisions.md`
  when M21 lands.

- 2026-08-06: **M18 demoted to Brainstorm.** It was an idea wearing a milestone's
  sections: the Siege's stat line, its `SiegePerWave` counts and the 300-400 HP band were
  all derived from Wave 4's delivery arithmetic, and the roadmap's own first risk is
  tuning ahead of a logged Run. The idea survives as an entry in the new
  `Brainstorms/gatekeepers-and-waves.md` — the first area file covering the roster itself —
  whose graduation test is a five-Wave run showing what actually kills the door. The
  retired file is in `_to_delete/demoted-M18-20260806/`; **M18's ID is not reused.**
  Current moved to M15, which the previous Current line skipped over.

- 2026-08-04: **`Brainstorms/` created** with `_meta/_definitions/brainstorm.md` as its
  contract. The roadmap's inline Brainstorm list had outgrown a summary file and the
  milestones demoted on 2026-08-04 (M3 fabrication, M4 skills, M5 loot, M6 co-op,
  M7 outfitting, M8 destinations) had nowhere to keep their content. Four area files;
  each names the one thing that must be known before a milestone can be written from it.
- 2026-08-04: **M18** (Siege gatekeeper — shells the Hatch from 35 m and never closes) and
  **M19** (repair costs 2 Salvage/HP) added. M18 gives Waves 4–5 a new kind rather than
  more of the existing three; M19 gives Salvage an in-run use.

- 2026-08-05: **M17 completed.** AutoVerify precheck + non-empty error `message`;
  PlayModeSaveFixture guards the campaign (byte-identical save; orphan
  `.playtestbackup` restore logged). Green suite EditMode 203 / PlayMode 10.
- 2026-08-05: **M14 completed.** Five Waves, deep start, `DeepestWaveReached`, NAV MAP
  selector, chained delivery. AutoVerify EditMode 203 / PlayMode 10. Play criteria
  7–9 left as watch (no gate log at ship).

- 2026-08-05: **M19 completed.** Hatch repair costs `RepairSalvagePerHp` (2) via
  `RunSim.RepairHatch`; bank stops at zero; end report `earned` subtracts repair spend.
  AutoVerify EditMode 211/0 (8 `RepairCostTests`), PlayMode 10/0
  (`gkb_verify_report.json`, 2026-08-06T03:26:56Z). Play confirm of HUD prompts left open.

- 2026-08-04: **Table rebuilt from the milestone folders.** Rows for M11, M2 and M13 still
  read `next`/`later` and linked to `1. planned/` paths that 404 — the folder is the
  status, so the folders won. M12's row removed (merged into M11 on 2026-08-04); M16's row
  restored after a concurrent save dropped it.
- 2026-08-04: **M17 added** — AutoVerify reported failures with the cause discarded, and
  `PlayTestUtil.SaveFileGuard` was never constructed, so Play-mode runs banked +112 Salvage
  and +1 run/death into the live campaign each time. Implementation landed **before** the
  milestone was written, which is the rule the tooling exception exists to enforce; the
  criteria are unmet until someone runs them.

- 2026-08-04: **M16 added** — a stated difficulty curve as Hatch-remaining per Wave,
  tuned by measurement rather than by model. Found in the logs: realization is not a
  constant (0.31–0.72, rising with Wave and falling with weapon power), and
  `MaxGroupSize = 4` is slowing Wave 3's delivery to 38.0 s against the 30.4 s the
  5-wide curve intended — a 25% change introduced as M2's fix and never revisited after
  the Primaries shipped.

- 2026-08-04: **Repair priced at 2 Salvage/HP** (`Design/4-Data.md`), making the door
  compete with the shop. Full-repair play breaks the three-death pacing target at Waves
  4–5; that gap is the decision the cost exists to create, and the target is now stated
  for a player who repairs what they must.
- 2026-08-04: **M15 added** — an on-screen shortcut panel, dev keys included, backed by
  a single binding list so the panel and the input code cannot disagree. Found while
  writing it: the weapon slots already work and are undiscoverable, and the number row
  is double-bound between the siphon dial and the weapon slots. The dial moves to
  `[` / `]`. First draft was a held-`Tab` reference overlay — cut as a settings screen
  in disguise; an options menu is where that belongs and the game has none.

- 2026-08-04: **M10 completed.** `spawnInterval` is `max(0.4, 4.2 - 0.20*waveIndex)`;
  `SiphonAndWaveTests` asserts 4.20 / 4.00 / 3.80. Current milestone is now M13.

- 2026-08-04: **M11 and M12 merged into M11 — "The weapon is the build."** M11 alone
  shipped a catalog, slots and Tiers with one weapon in them, which is infrastructure
  rather than a player-visible change; M12 alone was a data edit. Together they are
  one unit: the weapon system and the four weapons that make it mean anything. M12 is
  retired, its ID not reused. The one criterion lost in the merge — "adding a weapon
  changed no file outside `WeaponCatalog`" — is replaced by a throwaway-row test
  exercising composed firing modes, with the `git diff` version deferred to the fifth
  weapon.

- 2026-08-04: **M13 (dev mode)** and **M14 (five Waves and a deep start)** added.
  M13 required amending `_meta/_definitions/milestone.md`: the "a chore or a tool is
  not a milestone" rule was too broad and excluded durable infrastructure that
  unblocks verification across the roadmap. Chores still do not qualify; tooling now
  does, with a developer Story and a stated multiplier. M14 extends the Wave curve to
  five and adds a NAV MAP start-Wave selector where starting at N delivers Waves 1..N back
  to back — a condensed run, not a skip.

- 2026-08-04: M12 became **A primary for each threat** — the MR-35 rifle joins the
  BR-12, so the three Primaries map onto the three gatekeeper kinds. M11 hardened to
  carry everything M12 depends on: all four firing modes (single, pellets, pierce,
  falloff, composable), per-slot clip state, slot switching on 1/2/3 plus wheel and
  Q, and three independent weapon-identity cues (HUD name, crosshair, tracer) so a
  player can tell which gun they hold with no art. M12 is now a `WeaponCatalog` edit
  and nothing else; its first criterion checks exactly that.

- 2026-08-04: **One grain enforced.** `_meta/_definitions/milestone.md` reshaped
  around *implementable unit of work* — mandatory Story, Acceptance criteria,
  Implementation and Verification sections, and an explicit list of what is not a
  milestone: an activity, a phase, an epic, a chore. The table ranged across all
  four and could not be ordered. Demoted to Brainstorm, files removed: **M1** (an
  activity — playing is how milestones are verified, so its content became the
  Verification sections of M10 and M2), **M3**, **M4**, **M5**, **M6** (an epic),
  **M7**, **M8**, and **M9** (a phase — art/audio moved to strategy prose). What
  remains is five rows of one size. M2 rewritten to the new shape and rescoped to
  Wave 3 only.

- 2026-08-04: M11 and M12 merged and re-cut. Upgrades are **per equipped weapon**,
  not global tracks, so the weapon-slot system and the upgrade curve are one
  milestone (M11) rather than two that collide in `MetaUpgrades`. ARMORY becomes
  contextual and the three consoles divide by subject — weapon / player / ship.
  Global Damage, Fire Rate and Clip tracks are deleted, replaced by per-weapon Mark
  levels at ~30% a step. M12 becomes the second Primary plus the tuning that makes
  the pair a choice, gated on two loadouts failing differently rather than on the
  weapon existing. Melee reserves its slot and ships empty, trigger named.

- 2026-08-04: M10/M11/M12 replaced. The first set (log telemetry, HUD readout,
  prefab wiring) was instrumentation and cosmetics — it moved no part of the game,
  and left the two actual blockers sitting in `later`. Dropped to
  `_to_delete/dropped-milestones-20260804/`. The replacements are the blockers:
  M10 Wave 2 winnable, M11 the weapon as the progression, M12 a real weapon choice. M2 rewritten around the measured gatekeeper-second budget — its previous
  gate (TTK ≤ delivery at 62.5 dps) rested on a falsified model and is retired
  rather than softened — and rescoped back to Wave 3 only. `#` declared a stable ID
  rather than a position so rows can be reordered.

- 2026-08-03: Added `_meta/_definitions/milestone.md` as the authoring contract; moved
  the milestone template out of this file so it exists in exactly one place.
- 2026-08-03: Split detail into `Milestones/<status>/` — one file per milestone,
  folder as status. This file became the summary and strategy layer.
- 2026-08-03: Added risks, review cadence, this log, and the frontmatter update
  trigger — ported from the Serennovas knowledge base's roadmap contract.
- 2026-08-03: Added the Current pointer and completion-only date stamping; M0
  stamped August 2026 — ported from the InfinityQube roadmap.
- 2026-08-03: File created. M0 shipped; M1 set as current; M2–M9 ordered;
  Brainstorm opened with five unowned items.
