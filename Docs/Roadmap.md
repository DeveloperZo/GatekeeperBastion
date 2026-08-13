# 06 — Milestones & Roadmap

<!--
Purpose: Summary and strategy layer over Docs/Milestones/. Order, status, risks, and the rules that govern milestone files. Detail belongs in the milestone file, never here.
Status: working
Update trigger: when a milestone changes status, when a gate is rewritten, or when a Brainstorm item graduates.
Last updated: 2026-08-13
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

**M49 is active** (the gatekeepers see the second player). **M47 completed 2026-08-13**.
**M48 completed 2026-08-13**. **M52–M55 planned** (field loop — M52 next on the path).
**M50–M51 planned** (later co-op). **M45 completed 2026-08-12**.

**Recommended next**, in this order.

1. **M52** — Waves start at a terminal (Hatch not a target; door opens on clear).
2. **M53 → M54 → M55** — Hatch-bank convert, Master Control win, pay-for-intensity.
3. **M49** — finish gatekeeper targeting of the second player (active).
4. **Play close-outs** — M32 criterion 7, M26 Path 2, M31 pin playthrough; M47 criterion 18
   wave-report when a full kit Run is logged.
5. **Ship station terminal UI** — unfiled; craft when that polish is the next cut.

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
| M26 | A second sidearm, and a loadout that has a shape | later | `SEC-D` in the catalog; distinguishable from the MK-1 without reading a number; never rivals a Primary | [`1. planned/M26`](Milestones/1.%20planned/M26-a-second-sidearm.md) |
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
| M46 | The panel is authored | **cancelled, Aug 2026** | Cancelled (Zo) — authored-panel rewrite not a roadmap slot; station polish stays the unfiled Current cut | [`5. cancelled/M46`](Milestones/5.%20cancelled/M46-the-panel-is-authored.md) |
| M47 | The bastion gets height and angles | completed, Aug 2026 | Kit palette + authored `Bastion.unity`; H/S/T/D + Control Room last; freestyle BuildBastion removed; sightline/path criteria closed on author play (Zo); M37 median compare open | [`3. completed/M47`](Milestones/3.%20completed/M47-the-bastion-gets-height-and-angles.md) |
| M52 | Waves start at a terminal | later | Interact at a field terminal starts a Boarder-hunt Wave; Hatch takes no damage; door opens on clear; `T` refused | [`1. planned/M52`](Milestones/1.%20planned/M52-waves-start-at-a-terminal.md) |
| M53 | Salvage leaves through the Hatch | later | Kills fill unbanked; Hatch-bank drips / attrits / dumps; 3 slots; fat pile ×1.25/×1.50 | [`1. planned/M53`](Milestones/1.%20planned/M53-salvage-leaves-through-the-hatch.md) |
| M54 | Master Control is the win | later | Wave 5 no longer ends the Run; Master Control is 5 terminal then 5 Hatch-bank; leftover converts; finale spends no slot | [`1. planned/M54`](Milestones/1.%20planned/M54-master-control-is-the-win.md) |
| M55 | Pay for intensity | later | `[` `]` spend 100 unbanked per step; HUD says Intensity; siphon dial retired; same `TetherSiphon` curve | [`1. planned/M55`](Milestones/1.%20planned/M55-pay-for-intensity.md) |
| M48 | The first networked kill | completed, Aug 2026 | Client joins over loopback (F10/F11, dev-only); shoots a server-spawned gatekeeper; damage applies once on the host; death on both screens; wave counter identical via `NetRunMirror`; kill loop closed headless (`NetKillTests` Passed, PlayMode 24/0, report 2026-08-13T12:49:08Z) | [`3. completed/M48`](Milestones/3.%20completed/M48-the-first-networked-kill.md) |
| M49 | The gatekeepers see the second player | next | Avatar carries a registered `PlayerHealth` + collider; a gatekeeper hunts the nearer client via the existing `NearestAlive` path with **no enemy code changes**; melee drops replicated HP and the owner's rig shows it; client down survivable (host run continues; no revive — parked); `NetTargetingTests` headless; solo unchanged | [`2. active/M49`](Milestones/2.%20active/M49-the-gatekeepers-see-the-second-player.md) |
| M50 | The client lives the run | later | Mirror carries the fight readout (salvage, hatch HP, elapsed at 1 Hz); client HUD reads it; all three endings reach the client as an `EndSummary` ClientRpc with the host's numbers; session ends clean and re-joins twice in a row; `NetRunMirrorTests` headless. **Re-cut against M48's landed shapes before start** | [`1. planned/M50`](Milestones/1.%20planned/M50-the-client-lives-the-run.md) |
| M51 | The client's hands | later | Client repair channel, decoy placement and ability cone all apply once, server-side (per-verb single-application in `NetVerbTests`); denial reads back; one host wallet; **completes the loopback run** — Steam entry's graduation test. **Re-cut against M50 before start** | [`1. planned/M51`](Milestones/1.%20planned/M51-the-clients-hands.md) |

## Strategy

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

**Art, audio and a Steam page are a phase, not a milestone.** They do not belong in
this table and previously sat in it as "M9 — off gray-box". They start when the loop
is worth looking at; nothing above depends on them.

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
| Armory, weapons, Tiers, slots | [`Brainstorms/armory-and-weapons`](Brainstorms/armory-and-weapons.md) |
| Fabrication, ship systems, deployables | [`Brainstorms/fabrication`](Brainstorms/fabrication.md) |
| Crew attributes, skills, perks | [`Brainstorms/crew-skills-and-perks`](Brainstorms/crew-skills-and-perks.md) |
| Missions, the console restructure, destinations | [`Brainstorms/missions-and-destinations`](Brainstorms/missions-and-destinations.md) |
| Gatekeeper kinds and Wave composition | [`Brainstorms/gatekeepers-and-waves`](Brainstorms/gatekeepers-and-waves.md) |
| Co-op beyond the filed slices | [`Brainstorms/co-op`](Brainstorms/co-op.md) |

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
`3. completed/`, `4. deferred/`. **`Milestones/5. cancelled/` also exists on disk, empty.**
`_meta/_definitions/milestone.md` says four is the set and forbids new status folders, so
either the contract gains a fifth row or the folder goes — **Zo's call**. Recorded here on
2026-08-09 rather than deleted silently.

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
