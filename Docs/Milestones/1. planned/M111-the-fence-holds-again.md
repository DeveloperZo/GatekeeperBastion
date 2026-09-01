# M111 — The fence holds again

<!--
Status: later
Update trigger: when lint-arch's rule set or baselines change, or when a standing-red test is retired by another milestone
Last updated: 2026-09-01
-->

`Tools/lint-arch/run.sh` reports 16 violations against baselines of zero, and four tests
have been red long enough to be called "standing". A gate that always fails gates nothing:
this returns both fences to a state where a new red means something.

## Story

As the developer, I want the architecture lint at zero and the test suites' failure set at
zero, so that the next regression shows up as the only red on the board instead of the
seventeenth line everyone has learned to scroll past.

## Why it matters

The August sessions proved the cost twice over: five loop regressions shipped while the
suites were "green except the usual", and the offline harness's own false-green incident
was survivable only because the real number was checked later. Today's fence state:

- **7 over-cap files** — `BastionSketchEmitter.cs` 976/800, `BastionSketchParser.cs`
  639/600, `GameController.Terminals.cs` 611/400, `NetVerbs.cs` 549/400,
  `PlayerProtocolAbilities.cs` 459/400, `DevConsole.cs` 421/400, `GameController.cs`
  418/400.
- **8 milestone-id lines** in `SceneBuilder.Ship.cs`, `SceneBuilder.cs`,
  `BoarderProtocol.cs` — the zero-exceptions rule (`../../Architecture/3-Unity-Runtime.md`).
- **7 scene lookups outside the allowlist** — `HatchRecallPad.cs` ×3, `HatchPortal.cs` ×2,
  `GameController.Wallets.cs`, `ShipHud.cs`; the dock-slice work landed without entries.
- **4 standing test reds**, two of which (`M59PolishChromeTests` pair) demo-readiness
  already diagnoses as "needs a `GatekeepersProtocol/Build` re-run".

## Acceptance criteria

1. `bash Tools/lint-arch/run.sh` exits 0 on the clean tree, with every baseline still at
   zero — no baseline is raised to make it pass.
2. The over-cap files are under their caps by **extraction of real types**, not by
   `partial` splits — each extracted type passes the would-it-stand-alone question
   (`.claude/skills/gkb-development`), and `GameController.Terminals.cs`'s shutoff/start
   seam is the named first candidate.
3. The 8 milestone-id lines read as what the code does, not which milestone touched it.
4. Each of the 7 scene lookups either gains an allowlist entry with a one-line
   justification in the allowlist itself, or is refactored to a serialized binding —
   decided per site, and `HatchPortal`'s `FindFirstObjectByType<GameController>` is
   presumed refactor (the portal already lives in a scene that owns a `GameController`).
5. `M59PolishChromeTests.PauseShortcuts_AreBoundOnHudController` and
   `ShortcutTemplates_HaveFilledCooldownFill` are green: the human runs
   `GatekeepersProtocol/Build` once, and the gate reads the resulting scene YAML plus the
   test result — the criterion gates on the output, not on driving the editor.
6. `ShortcutRowApiTests.FillRows_JuiceRow_CarriesIntensity` and
   `TwoRunIsolationTests.SimAssembly_HoldsNoStaticMutableState` are green — for the
   latter, `MasterControl.HatchWaves` (the named static, which the finale no longer reads)
   is deleted with its pinning test re-keyed, or made non-static with the reason stated.
7. `Tools/verify/run.sh`'s exclusion list gains the row it has owed
   `Settings/BuildSceneRegistrationTests.cs` since that file was added (the list's own
   comment admits the debt).
8. Milestone-tree hygiene from the 2026-09-01 survey is fixed: `4. deferred/M63` names its
   return trigger; `3. completed/M101`'s frontmatter agrees with its folder.

## Implementation

**Change** `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` — extract the
terminal shutoff/arming seam into a type named for that responsibility; the file's own
611 lines are the argument. **Change** the other six over-cap files by the same method,
smallest honest extraction first; if a file cannot lose enough without an incoherent
split, say so on this file and raise *that* discussion, not the baseline.
**Change** `SceneBuilder.Ship.cs`, `SceneBuilder.cs`, `BoarderProtocol.cs` — reword the 8
lines. **Change** `Tools/lint-arch/` allowlist — entries per criterion 4.
**Change** `Assets/Scripts/Sim/Run/MasterControl.cs` + `MasterControlTests` — criterion 6.
**Change** `Tools/verify/run.sh` — criterion 7. **Change** the two milestone files —
criterion 8.

Rejected alternative: raising baselines to today's counts. That is how a fence becomes a
diary. Rejected alternative: fixing the chrome reds in code — demo-readiness already
locates them in unbuilt scene chrome; the builder is the fix, code would be a workaround
with a button on it.

## Verification

1. Criterion 1 — `bash Tools/lint-arch/run.sh`; exit 0. Prove twice: seed one over-cap
   line block and one scene lookup, expect both named; revert.
2. Criteria 5–6 — file bridge run; EditMode failure set is empty. This retires the
   "standing reds" phrase from every later milestone's Verification.
3. Criterion 7 — read `Tools/verify/run.sh:103–129`; the row exists with a reason.
4. Playthrough path — none owed; behaviour must not change. The offline harness count
   moving at all (beyond the retired reds) is a fail, not a pass.

## Watch

- Extractions from `GameController.*` must keep the fake-null and cached-`GetComponent`
  disciplines they inherit; a mechanical move that drops an `== null` for a `?.` is the
  known trap.
- The Build re-run touches scenes; commit before it so its diff is only the builder's.

## Do not build

- New lint rules (that is M110's lane for its one rule).
- Any behaviour change riding along "while the file is open".

## Depends on / Unblocks

Depends on: nothing. Unblocks: every later milestone's Verification shrinking to "no
reds"; `M108`'s HUD work lands on freshly built chrome instead of beside two known
corpses.
