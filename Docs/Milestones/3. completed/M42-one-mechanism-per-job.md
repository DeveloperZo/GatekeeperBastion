# M42 — One mechanism per job

<!--
Status: completed 2026-08-12
Update trigger: closed
Last updated: 2026-08-12
Origin: six-lens adversarial audit, 2026-08-10 (Architecture/5-Audit-2026-08-10.md). Renumbered note: the git-normalization milestone formerly at this number is now M43, content unchanged.
Stale refresh 2026-08-12 before build: M39 cancelled; M35 removed the HUD un-multiply preview (criterion 7 retargeted); ShiftShipUiImport gone → StationSceneGate.Stations; MonoBehaviours 43 (was 36); suite counts remasured on close.
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
`MetaState.FromSaveData` fed the salvage-ledger `-1` sentinel only when `SaveVersion < 2`
while `FromJson` fed it whenever the key is absent at any version — two migration answers
for one question. `PlayerGun.Firing.ApplyFalloff` returned full damage past `def.range` when
`falloffStart <= 0` while the Sim copy returned 0 — masked only because the ray is capped
at `def.range`.

**The log overstated income whenever the kill-value fraction is ≥ .5.** `KillLog.Format`
rounded the raw float while `AwardKill` floored it; at dial 5 a fresh save banked 37 and
logged "+38 salvage".

**Four names lied to their reader.** `LossReport` was also the success report.
`TrySelectSlotPublic` was a void `TryX`. `SalvagePerSecond` asserted the clock pays.
The player-damage comment promised rate-limiting that no code performed.

Leverage tier (`../../Working-Rules.md`): **#10, structure**.

**Multiplier** (the tooling exception, `../../_meta/_definitions/milestone.md`): the next
save-schema version, the owed weapon row, and every transcript-based economy audit get cheaper.

## Acceptance criteria

**One copy of each mechanism:**

1. **One save-migration path.** `FromJson` parses keys into a `SaveData` and returns
   `FromSaveData(d)`; `ToJson` serializes `ToSaveData()`. The v3/v4/v6 version gates exist
   once. Checkable: `grep -n "rawVersion >=" MetaState.cs` returns nothing. **The JSON
   path's observable behaviour is the contract**: a missing ledger key maps to `-1` at any
   version (FromSaveData must not re-gate on SaveVersion).
2. **One falloff copy.** Per-body damage in `FireRay` flows through
   `WeaponWheel.FalloffFactor(def, distance)`; `ApplyFalloff` is deleted. Out-of-range
   resolves to the Sim behaviour (0 past range).
3. **One repair path and a symmetric damage path.** `HatchStation.ApplyRepair` deleted.
   `HatchStation.ApplyDamage` calls `GameController.DamageHatch(float)` mirroring
   `RepairHatch`. **HATCH BREACHED and quarter-threshold log lines stay byte-identical.**
4. **The cut siphon income is gone.** `grep -rn SalvagePerSecond Assets` returns nothing.
5. **Inert members are gone.** `WaveEmit.Pending` deleted; `WeaponDef.abilityDial` deleted
   and callers use `WeaponAbility.Magnitude(id, 0)`.
6. **`WeaponCatalog.ArraySize` is computed from the `WeaponId` enum** with a plain loop (no
   LINQ).
7. **`RunSim.KillValueAt(int siphonLevel)` exists**; `CurrentKillValue` is
   `KillValueAt(Siphon.Level)`. (M35 removed the HUD un-multiply preview this criterion
   originally named; KillValueAt remains the single arithmetic entry.)

**The log tells the truth:**

8. **Kill lines quote the banked amount.** `RunSim.CurrentKillAward` exists; `KillLog`
   takes `long`; WaveSpawner and GameController forecast sites quote it.
9. **The player-damage comment states what the code does** — one line per damaging hit,
   recorded before `Die()`.
10. **`EffectiveClip` rounding is explicit** — `MidpointRounding.AwayFromZero` plus a
    comment that no catalog product lands on an exact .5 today.

**Names state behaviour:**

11. **`LossReport` → `RunReport`, `LastLossReport` → `LastRunReport`** including the
    reflection string at `RunReportAssertions.cs`. `grep -rn LossReport Assets` empty.
12. **One slot-select method.** Public `void SelectSlot(WeaponSlot)`.
13. **`"death(s)"` → `"run(s) lost"`**; upgrade-gate test renamed for lost runs; trap
    comment on `MetaState.PodEjections`. Field/JSON names stay.
14. **`PlayerDownAndGrubstakeTests` → `PlayerDownAndNewSaveEconomyTests`.**
    `grep -rni grubstake Assets` returns only the CoreLoopTests assert message.

**Conventions hold again:**

15. **Every MonoBehaviour under `Game/` carries exactly one of HOST-ONLY / LOCAL-OWNER /
    REPLICATED-STATE.** Remasured 2026-08-12: **43 of 43** (BastionDoor / Layout / Segment /
    Terminal / GeneratedGeom and extra Ui views landed after the audit's 36).
16. **One namespace under `Game/`.** `Game.Ui` and `Game.Combat` fold to
    `GatekeepersProtocol.Game`; scene `m_EditorClassIdentifier` strings updated with them.
17. **Station name lists cannot drift.** EditMode test asserts `StationSceneGate.Stations`
    (public) equals `StationName` of every concrete `StationTerminal` subclass. Excluded
    from the mcs harness (Editor assembly); Tests asmdef references `GatekeepersProtocol.Editor`.
18. **Docs match the code** for EquippedMelee (key changed, shim banned), WaveSpawner R1
    exception, ShipController loadout belt, and structural Update/OnGUI/firing wording.

**And nothing else moved:**

19. **Both suites green.** Remasured 2026-08-12 close:
    - Harness (`Tools/verify/run.sh`): **279 passed**, 0 failed (StationPanelDrift excluded).
    - Unity AutoVerify: EditMode **311/0**, PlayMode **23/0** (`gkb_verify_report.json`
      `timestampUtc` 2026-08-12T16:07:41Z).
    One session log line changes (`"death(s)"` → `"run(s) lost"`); kill-line shape keeps
    `+N salvage`.

## Implementation

Shipped as one build. Criterion clusters match the list above. Supporting fixes required
for the gate on this tree: Windows path normalisation in `Tools/lint-arch/run.sh`;
`BastionSegment` split under the 400-line `game_cap`; `PlayerGun` ARMORY vocabulary;
PlayMode `ShoveDoesNotUncommit` no longer depends on Retarget when no hatch is available.

## Verification

| Criterion | Command / observation | Expected |
|---|---|---|
| 1 | `rg -n "rawVersion >=" Assets/Scripts/Sim/Meta/MetaState.cs` | empty |
| 2 | `rg -n ApplyFalloff Assets/Scripts` | empty |
| 3 | `rg -rn ApplyRepair Assets` | empty |
| 4 | `rg -rn SalvagePerSecond Assets` | empty |
| 8 | AutoVerify + kill-line shape | `+N salvage` floored |
| 11 | `rg -rn LossReport Assets` | empty |
| 12 | `rg -rn TrySelectSlot Assets` | empty |
| 15 | label grep over Game MonoBehaviours | 43/43 |
| 16 | `rg -n "^namespace" Assets/Scripts/Game` | one namespace |
| 19 | `Tools/verify/run.sh`; AutoVerify | 279 / 311+23 green |

## Do not build

- **The `PodEjections`/`CleanExits` field rename** (~88 references).
- **The ring/identity move into `WeaponDef`.**
- **A shared sentinel-bridge helper for Editor tools.**
- **FX unification.**
- **A damage-log window.**
- **Anything DecoyBay** beyond the R5 label normalisation.

## Watch

- Criterion 1's behaviour contract was the only real risk; raw-JSON migration tests stayed green.
- Reflection string at `RunReportAssertions` caught by PlayMode.
- M39 cancelled 2026-08-12 — no longer a sequencing partner.

## Depends on / Unblocks

**Depends on nothing.** M39 cancelled; no remaining dependency.

**Unblocks nothing formally.** Cheaper next save-schema / weapon row / transcript audit.
