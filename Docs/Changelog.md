# Change log

<!--
Purpose: The audit trail for Docs/Roadmap.md. Milestone state transitions and structural edits to the roadmap, most recent first. Split out of Roadmap.md on 2026-08-08, entries unchanged.
Status: working
Update trigger: when a milestone changes status, when a gate is rewritten, or when the roadmap's structure changes.
Last updated: 2026-08-10
-->

Most recent first. **State transitions and structural edits only** — a milestone moving
folder, a gate rewritten, a queue cap changed, a Brainstorm item graduating. Not progress
notes, not tuning results, not anything the milestone file already holds.

This file is history. **`Roadmap.md` is what is next**: order, status, strategy and risks.
`Milestones/` is the detail behind any entry here.

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
