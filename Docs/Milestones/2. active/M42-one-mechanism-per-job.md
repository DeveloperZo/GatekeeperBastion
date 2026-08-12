# M42 — One mechanism per job

<!--
Status: later
Update trigger: when a criterion's grep gate changes meaning because M39 or M35 landed first, or when a listed deletion acquires a live caller
Last updated: 2026-08-10
Origin: six-lens adversarial audit, 2026-08-10 (Architecture/5-Audit-2026-08-10.md). Renumbered note: the git-normalization milestone formerly at this number is now M43, content unchanged.
-->

Every job in the tree gets exactly one mechanism, and every mechanism keeps a live caller.
Today the save format has two migration pipelines, the repair verb has two entry points, the
falloff formula has two copies (the tested one is not the one the game runs), a cut economy
mechanic still ships a tested API, and four names promise behaviour the code does not have.
Each item below was found by one audit lens and confirmed by an adversarial second read
against the code; nothing here rests on a doc.

## Story

As the developer, I want one copy of every mechanism and names that state what the code does,
so that a change edits one place and a reader with ten years of experience is never surprised
by the implementation.

## Why it matters

**The duplicate copies have already drifted, twice, in ways that tests cannot catch.**
`MetaState.FromSaveData` feeds the salvage-ledger `-1` sentinel only when `SaveVersion < 2`
while `FromJson` feeds it whenever the key is absent at any version (`MetaState.cs:418-420`
vs `567-569`) — two migration answers for one question, and production only ever asks one of
them. `PlayerGun.Firing.ApplyFalloff` returns full damage past `def.range` when
`falloffStart <= 0` while the Sim copy returns 0 (`PlayerGun.Firing.cs:164-172` vs
`WeaponWheel.cs:198-210`) — masked today only because the ray is capped at `def.range`, so
the 19 test references pin a formula the game does not run.

**The log overstates income whenever the kill-value fraction is ≥ .5.** `KillLog.Format`
rounds the raw float to nearest while `AwardKill` floors it (`RunSim.cs:328`, comment: "the
bank and the run tally must agree to the unit"); at dial 5 a fresh save banks 37 and logs
"+38 salvage". `4-Testing-and-Determinism.md` makes the kill line a parsed contract, so
today the transcript of a 30-kill run disagrees with the bank by up to 30 salvage.

**Four names lie to their reader.** `LossReport` is also the success report (its own doc
comment says `"cleared"` is "a SUCCESS", and the PlayMode helpers already coined the correct
name, "run report"). `TrySelectSlotPublic` is a void `TryX` — the shape `2-Naming.md` §2
calls an error code wearing the pattern's costume. `SalvagePerSecond` asserts the clock pays
in a project whose economy rule is "the clock never pays" (`Decisions.md`). The comment above
the player-damage log line promises rate-limiting and aggregation that no code performs
(`PlayerHealth.cs:75-78`) — a false contract sitting on the project's verification surface.

Leverage tier (`../../Working-Rules.md`): **#10, structure** — it changes what a save-schema
change, a weapon row, or a falloff retune costs, rather than telling anyone anything.

**Multiplier** (the tooling exception, `../../_meta/_definitions/milestone.md`): the save
schema has moved six versions in five weeks and `Decisions.md` already owes a v-next weapon
row; both of those expected changes get cheaper, and every future economy audit becomes
checkable from the transcript.

## Acceptance criteria

**One copy of each mechanism:**

1. **One save-migration path.** `FromJson` parses keys into a `SaveData` and returns
   `FromSaveData(d)`; `ToJson` serializes `ToSaveData()`. The v3/v4/v6 version gates exist
   once. Checkable: `grep -n "rawVersion >=" MetaState.cs` returns nothing. **The JSON
   path's observable behaviour is the contract**: a missing key maps to what `FromJson` maps
   it to today — including the `-1` ledger sentinel at any version, which `FromSaveData`
   currently gets wrong — so `MetaState_FromJson_MissingFieldsFallBackToDefaults` and every
   raw-JSON migration test pass unchanged.
2. **One falloff copy.** Per-body damage in `FireRay` flows through the Sim functions via a
   shared `FalloffFactor(def, distance)`; `ApplyFalloff` is deleted. The out-of-range
   divergence resolves to the Sim behaviour (0 past range). No play change — the ray cap
   masks the divergent region today, and the derived numbers in
   `WeaponPierceFalloffTests` do not move.
3. **One repair path and a symmetric damage path.** `HatchStation.ApplyRepair` (zero call
   sites, guards duplicated from `GameController.RepairHatch`) is deleted.
   `HatchStation.ApplyDamage:95` stops calling `hatch.Damage(amount)` directly and calls a
   new `GameController.DamageHatch(float)` mirroring `RepairHatch`; the `RunIsOver` guard
   moves into it. **The HATCH BREACHED and quarter-threshold log lines stay byte-identical**
   — only the mutation line moves.
4. **The cut siphon income is gone.** `grep -rn SalvagePerSecond Assets` returns nothing:
   the method (`TetherSiphon.cs:51-54`) and its only caller
   (`Siphon_SalvagePerSecond_AppliesMultiplier`) are deleted. `RewardMultiplier` /
   `RewardMultiplierFor` are untouched — they carry the live economy.
5. **Inert members are gone.** `WaveEmit.Pending` (written twice, read nowhere) is deleted
   and the comment at `WaveSpawner.Groups.cs:107-109` is reworded to name what actually
   prevents the softlock: the `Queue.RemoveAt` plus the `_active.Count`/`AliveCount` clear
   test. `WeaponDef.abilityDial` (derived, never read at runtime) is deleted and its test
   references point at `WeaponAbility.Magnitude(id, 0)`.
6. **`WeaponCatalog.ArraySize` is computed from the `WeaponId` enum** with a plain loop (no
   LINQ), so a new weapon row cannot ship silently unownable
   (`MetaState.SetOwned:233` bounds-checks and returns). Ring/identity data stays in
   `WeaponWheel` for now — see *Do not build*.
7. **The income preview stops re-deriving sim arithmetic.** `RunSim.KillValueAt(int
   siphonLevel)` exists; `HudController.Panels.cs:87` calls it instead of un-multiplying
   `CurrentKillValue` and re-multiplying. The displayed number is identical today.

**The log tells the truth:**

8. **Kill lines quote the banked amount.** `RunSim.CurrentKillAward =>
   (long)Math.Floor((double)CurrentKillValue)` exists; `KillLog` and the four `:0` forecast
   sites (`HudController.cs:170`, `HudController.Panels.cs:80`, `GameController.cs:224`,
   `:399`) quote it. Summing a run's kill lines equals the run's gross to the unit.
   `Tools/wave-report` is checked for parsing the `+N salvage` shape and updated in the same
   change if it reads the number.
9. **The player-damage comment states what the code does** — one line per damaging hit,
   recorded before `Die()` so the killing blow precedes the death line. No window is built
   (`Decisions.md` cut the aggregation layer; the comment is the bug).
10. **`EffectiveClip` rounding is explicit and stated.** `MidpointRounding.AwayFromZero`
    (the `WaveDirector.cs:150-156` precedent) plus a comment giving the rule and the min-1
    floor. Value-neutral today — no catalog product lands on an exact .5; say so in the
    comment rather than claiming test coverage that does not exist.

**Names state behaviour:**

11. **`LossReport` → `RunReport`, `LastLossReport` → `LastRunReport`** — mechanical across
    Sim, 3 Game call sites and ~10 test files, **plus the reflection member-name string at
    `RunReportAssertions.cs:36`**, which fails the PlayMode end-screen tests by design if
    missed. Nothing is serialized under the old name. `grep -rn LossReport Assets` returns
    nothing.
12. **One slot-select method.** `TrySelectSlot`/`TrySelectSlotPublic` collapse into one
    public `void SelectSlot(WeaponSlot)`; the empty-slot and same-slot no-ops keep their
    semantics — `CycleSlot` relies on them.
13. **The lost-run counter stops reading as deaths.** `GameSession.cs:95` `"death(s)"` →
    `"run(s) lost"` (it increments on hatch loss with the player alive);
    `UpgradesUnlocked_FalseAtZeroDeaths_TrueFromFirstDeath` is renamed to say lost runs; a
    one-line trap comment lands on `MetaState.cs:23`. **The field and JSON names stay** —
    see *Do not build*.
14. **`PlayerDownAndGrubstakeTests` is renamed to match its contents**
    (`PlayerDownAndNewSaveEconomyTests`) — no test in it asserts a grubstake and the
    mechanic exists nowhere. `grep -rni grubstake Assets` returns only the
    `CoreLoopTests.cs:61` assert message recording its absence.

**Conventions hold again:**

15. **Every MonoBehaviour under `Game/` carries exactly one of the three R5 labels.** The 14
    unlabeled `Game/Ui` views get one each; `DecoyObject.cs:8` ("HOST-OWNED") and
    `DecoyBay.cs:7` ("LOCAL + HOST") are normalized to the sanctioned vocabulary. 36 of 36
    files pass the label grep.
16. **One namespace under `Game/`.** The 12 `GatekeeperBastion.Game.Ui` files and
    `FxColors.cs`'s `Game.Combat` fold back to `GatekeeperBastion.Game`, restoring the
    documented convention ("adding a folder never means adding a namespace"). Mechanical; no
    behaviour.
17. **The station-name lists cannot drift silently.** An EditMode test asserts
    `ShiftShipUiImport.StationPanels` (made public-readonly) equals the `StationName` of
    every `StationTerminal` subclass — the same four strings currently live in six places
    with only a comment holding them together. The test file is added to
    `Tools/verify/run.sh`'s exclusion list (it references the Editor assembly, which the
    mcs harness cannot compile) and the Tests asmdef gains the Editor-assembly reference.
18. **The docs this milestone touches stop asserting the opposite of the code**, in the same
    change as the code: `2-Naming.md:156`'s worked example states what actually happened
    (the `EquippedMelee` key changed with the rename, shim banned — `SaveData.cs:15-18`);
    `Architecture.md`'s convention sentence names `WaveSpawner`'s two delivery-handshake
    notifications as the sanctioned R1 exception (`RunSim.cs:40` and `:115` already state
    that contract); `ShipController.cs:40` gets a comment recording the loadout-unlock as
    the direct-play/transition belt; the "8 Update() methods" count in the two working docs
    is reworded structurally ("every `Update()`, `OnGUI`, and the firing path") so it cannot
    go stale again.

**And nothing else moved:**

19. **Both suites green with the expected deltas and no others.** Unity: 282 `[Test]`
    methods (−1 siphon, +1 station drift). Harness: 265 (−1 siphon; the drift test is
    excluded). Re-measure both before starting — the counts assume neither M39 nor a test
    change has landed since 2026-08-10. One session log line changes (`"death(s)"` →
    `"run(s) lost"`); the kill-line shape keeps `+N salvage`; every other tag and shape is
    unchanged in a played Wave.

## Implementation

Order groups the work so each commit is one criterion cluster and `Tools/verify/run.sh`
passes between groups.

**Change** `Sim/Meta/MetaState.cs` — criterion 1. The codec becomes dumb: parse → `SaveData`
→ delegate. Keep the regex extractors; delete the codec-side version branches.
**Change** `Sim/Meta/SaveData.cs` — nothing structural; its version-history remarks are now
the single migration record.
Rejected: deleting the DTO path instead (`ToSaveData`/`FromSaveData`) and leaving `FromJson`
as the only pipeline — it re-homes migration logic inside a regex parser and rewrites eight
test call sites to reach the same single-path payoff.

**Change** `Sim/Meta/WeaponWheel.cs`, `Game/Player/PlayerGun.Firing.cs` — criterion 2:
extract `FalloffFactor`, delete `ApplyFalloff`, apply crit/cutter multipliers on top in the
Game layer (the Sim functions do not know about them).

**Change** `Game/Bastion/HatchStation.cs`, `Game/Bastion/GameController.cs` — criterion 3.
**Delete** `HatchStation.ApplyRepair`.
Rejected: a doc sentence naming `HatchStation` as the damage proxy instead of the command —
the repair half already routes through `GameController`, and the asymmetry is itself the
surprise this milestone exists to remove.

**Delete** `TetherSiphon.SalvagePerSecond` + its test; `WaveEmit.Pending` + both writes;
`WeaponDef.abilityDial` + its catalog write — criteria 4, 5.

**Change** `Sim/Meta/WeaponCatalog.cs` — criterion 6 (`ArraySize` from enum).
**Change** `Sim/Run/RunSim.cs` — criteria 7, 8 (`KillValueAt`, `CurrentKillAward`);
`Sim/Combat/KillLog.cs`, the two HUD sites, the two GameController wave lines.
**Change** `Game/Player/PlayerHealth.cs` — criterion 9, comment only.
**Change** `Sim/Meta/WeaponWheel.cs` `EffectiveClip` — criterion 10.

Renames — criteria 11-14: `Sim/Run/RunSim.cs` and call sites; `Game/Player/PlayerGun.cs`;
`Assets/Tests/PlayMode/RunReportAssertions.cs:36` (the string); `Game/Flow/GameSession.cs:95`;
`Assets/Tests/EditMode/Run/PlayerDownAndGrubstakeTests.cs` (file + class + one method name +
`.meta`). One rename per commit so a miss bisects.

Labels and namespaces — criteria 15, 16: comment lines and namespace lines only, no code.
Rejected: blessing `Game.Ui` as a namespace and renaming the five stragglers — it amends a
written convention to ratify drift, and the convention's reason (one namespace per layer
keeps `using` lists trivial) still holds. If Zo prefers the blessing, criterion 16 inverts to
"the 5 stragglers + `FxColors` adopt `Game.Ui`/fold respectively and `Architecture.md:19-21`
is amended" and nothing else changes.

**Create** `Assets/Tests/EditMode/Ship/StationPanelDriftTests.cs` — criterion 17.
**Change** `Assets/Tests/EditMode/GatekeeperBastion.Tests.asmdef` (Editor reference),
`Tools/verify/run.sh` (exclusion).

**Change** `Docs/Architecture/2-Naming.md`, `Docs/Architecture.md`,
`Docs/Architecture/3-Unity-Runtime.md`, `Docs/Architecture/README.md`,
`Docs/Architecture/1-Boundaries.md` — criterion 18, same commits as the code they describe.

## Supporting work

Developer infrastructure; the three questions are answered for the developer.

- **Driven** — `Tools/verify/run.sh` and the grep gates above, on every commit.
- **Shown** — a red test or a non-empty grep at the moment a duplicate returns.
- **Reads back** — kill lines that sum to the bank, and `git grep` finding one copy of each
  mechanism.

## Verification

| Criterion | Command | Expected |
|---|---|---|
| 1 | `grep -n "rawVersion >=" Assets/Scripts/Sim/Meta/MetaState.cs` | empty |
| 1 | `Tools/verify/run.sh --filter MetaState` | all migration tests pass, names unchanged |
| 2 | `grep -n ApplyFalloff Assets/Scripts` | empty |
| 3 | `grep -rn ApplyRepair Assets` | empty; `DamageHatch` has exactly two callers (ApplyDamage, tests) |
| 4 | `grep -rn SalvagePerSecond Assets` | empty |
| 5 | `grep -n "Pending" Assets/Scripts/Game/Enemies/` | empty; `grep -rn abilityDial Assets` empty |
| 8 | play one Wave, sum the `+N salvage` lines | equals the run's gross exactly |
| 11 | `grep -rn LossReport Assets` | empty |
| 12 | `grep -rn TrySelectSlot Assets` | empty |
| 14 | `grep -rni grubstake Assets` | only the CoreLoopTests assert message |
| 15 | label grep over `: MonoBehaviour` files in `Game/` | 36 of 36 |
| 16 | `grep -rn "^namespace" Assets/Scripts/Game/` | one namespace |
| 19 | `Tools/verify/run.sh`; AutoVerify | 265 / 282, all green |

Playthrough path — from Play: start a Run at dial 5, kill three gatekeepers, let the hatch
take damage, repair it, die. Confirms: kill lines match the banked ledger to the unit, the
hatch damage/repair lines are byte-identical to a pre-milestone log, and the end screen
renders from `LastRunReport` exactly as it did from `LastLossReport`.

## Do not build

- **The `PodEjections`/`CleanExits` field rename** (~88 references, permanent
  field-vs-JSON-key divergence). Criterion 13's three edits capture the payoff; revisit only
  if a fourth mistranslation appears.
- **The ring/identity move into `WeaponDef`.** Do it with the next weapon row
  (`Decisions.md` owes one), where it pays for itself.
- **A shared sentinel-bridge helper for the five Editor tools.** The genuinely identical
  part is ~20 lines of poll plumbing; extract it when a sixth tool is written, not before.
- **FX unification** (`SpawnSpark` vs `Effects`). Real scruff, no scheduled FX change;
  wait for an FX milestone.
- **A damage-log window.** The comment is the bug (criterion 9); `Decisions.md` cut the
  aggregation layer.
- **Anything DecoyBay.** The singleton is M40/M41's named debt; the unreachable activation
  path is a separate wire-or-cut decision recorded in `5-Audit-2026-08-10.md` and owned by
  Zo.

## Watch

- **Criterion 1's behaviour contract is the milestone's only real risk.** The two pipelines
  disagree today (the `-1` sentinel edge); unification must land on the JSON path's answer.
  The raw-JSON migration tests are the tripwire — if one goes red, the port picked the wrong
  pipeline's behaviour.
- **The reflection string at `RunReportAssertions.cs:36` does not show up in a compile.**
  It is the one rename site the compiler cannot catch; the PlayMode suite is the net.
- **M39 touches `RunSim.cs` and the harness source list.** Either order works, but whichever
  lands second re-measures criterion 19's counts first (M41's own convention).
- **The kill-line change alters a parsed number, not the shape.** `Docs/dashboard.html` and
  `Tools/wave-report` read these logs; check both against a post-change log before closing.

## Depends on / Unblocks

**Depends on nothing.** Independent of M35, M39, M40, M41 — file overlap with M39
(`RunSim.cs`, harness) is sequencing, not dependency.

**Unblocks nothing formally.** Makes the next save-schema version, the owed weapon row, and
every transcript-based economy audit cheaper, and removes the false doc statements an agent
would otherwise inherit as priors.
