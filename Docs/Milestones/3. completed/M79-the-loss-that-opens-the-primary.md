# M79 — This loss opens the Primary

<!--
Status: shipped, Aug 2026 — built, realigned and closed 2026-08-20. Criterion 10
closed under the dev-tainted allowance ruled the same day (`../../Decisions.md`);
criterion 9's composition bet stays open and needs a logged Run
Update trigger: when the zone-2 shape, the bank-loss Primary gate, ladder
rung 2, or the zone table in Demo beat 5 moves
Last updated: 2026-08-20
-->

Beat 5, recut 2026-08-19 (Zo): zone 2 is one terminal launching 3 consecutive
Waves the secondary kit can hold, then a heavier bank it cannot — that bank
falling is the authored loss that sells the Primary, whether it is the save's
first ejection or its second. Retrigger, survive with Primary + 1, D2 opens.
Slots retired. Cut 6 of the demo family.

**Recut 2026-08-19 (Zo).** The previous Waves are survivable. This bank is
where the demo hands the player a loss, and that loss is the Primary gate —
not `PodEjections >= 2`.

**Realigned 2026-08-20 against the tree.** Four premises this file carried
were wrong, and the corrections change what gets built. They are stated in
*Code state* below rather than left to be rediscovered. Co-op buy-back left
this milestone for `M90` in the same edit.

## Story

As a player who cleared zone 2's field Waves on the kit I already have, I
want that zone's bank to knock me down and put a Primary on the rack, so the
loop deepens by playing it, and the gun is something I walk to and buy.

## Why it matters

Zone 2's three field Waves are the previous Wave: survivable on the secondary
kit, barely. The bank behind them is authored above that ceiling and below
Primary + 1. The player reaches the bank; the bank is the loss; the loss
opens the buy. Arriving with zero prior ejections or one does not change the
gate — `PodEjections` counts endings, it does not sell guns.

M78 already moved the wheel off the first ejection (`../../Decisions.md`,
the wheel opens on the first bank). Keying the Primary on the second ejection
would repeat that mistake for anyone who docked through zone 1: they would
never see the row. The gate is the event the player just played.

Leverage is **#5 rules** (`../../Working-Rules.md`): what purchase this loss
unlocks. Softening the three field Waves, or hardening them until they *are*
the loss, is the wrong tier — that is composition in service of a count, and
the count is what this recut drops.

Arc on the authored bank and the Primary row; loop on the 3-Wave trigger and
the retrigger.

## Code state

Verified 2026-08-20 by reading the tree. Where this section and the prose
above disagree, this section wins.

1. **The Primary rows are not locked today — they are open.** `Br12Shotgun`
   (900), `Mr35Rifle` (1200) and `PriC` (1500) are all in
   `WeaponCatalog.ShopWeapons()`, and `ShipCommands.BuyWeapon` carries no
   demo check at all (`MetaUpgrades.UpgradesUnlocked`'s own docstring says
   "Buying a weapon itself is NOT gated"). `ArmoryStation` binds the PRIMARY
   tab and its BUY rows unconditionally. So this milestone **adds a gate**;
   it does not open one. Until it does, a large zone-1 bank lets a player buy
   PRI-C in beat 3 and walk into zone 2 already holding the gun — which
   trips this file's own kill criterion 1.
2. **There is no `PodEjections >= 2` reader to delete.** The only production
   comparisons are `>= 1` in the v11 migration and `> 0` in the HUD, and
   neither gates anything here. What actually encodes "loss 2 → Primary" is a
   *writer* — dev checkpoint 4 sets `PodEjections = 2` and grants PRI-C — and
   its test. Those are what get recut.
3. **Rung 2 must not extend `DemoWheelBuysLeft`.** That field's absolute
   value is load-bearing for a second feature: `DemoProgress.MultiplayerUnlocked`
   is `DemoWheelBuysLeft < 2`, and the once-per-save MP line keys on the
   counter passing exactly 1. Raising it to 2 for rung 2 **re-locks
   multiplayer mid-demo and replays `demo.mp_unlocked`**, and a source test
   forbids the literal reset in either `ShipCommands` file. Rung 2 gets its
   own counter.
4. **Zone 2 does not exist in code.** There is no terminal→zone table:
   `FirstTerminal` knows T1/T2 only, `WavesFor("T3") == 1`, the Hatch-bank
   gate names T1/T2 literally, the retrigger offer hard-refuses any terminal
   but T2, and nothing outside a dev checkpoint writes
   `DemoZoneDeepestWave[2]`. This is the bulk of the build.

Three premises resolved **in the milestone's favour**, so nothing is owed:
the done-terminal `[E] Open door` branch is already removed; doors are
already keyed to bank survival with the terminal-AND demoted to eligibility
(so the AND graph is **not** deletable — D2 reads T3 through it, and
`BastionUnlockGraph.Validate` requires exactly one control gate, so D3 stays
too); and there is no `FirstTerminal` 3-Wave cycle to retire —
`FirstTerminal.Waves == 1`.

Two mechanisms already carry weight this file assumed was unbuilt:
`RunSim.TriggerCompositeDraw` is fully zone-agnostic and drives consecutive
Waves with no Prep between them, and the bank's composition already rises
with carry (zone 1 banks at carry 2 → ×1.10; zone 2 at carry 3 → ×1.25 plus
`HatchBank.Carry3ExtraRam`). The heavier bank is mostly a consequence of the
zone being three Waves deep, not new composition code.

## Decisions this file makes

- **Which Primary.** Rung 2 opens the PRIMARY slot **as a group** — all three
  shop Primaries at their catalog costs. The rack is a choice, and dev
  checkpoint 4's PRI-C stays a valid instance of that choice. Beat 5's
  "Primary cost: not set" stamp is stale and is dropped: the catalog prices
  them (900 / 1200 / 1500).
- **The zone table is a new type.** `Sim/Bastion/DemoZones.cs` holds the
  per-zone table (terminal ids, Waves per trigger, first/end Wave, door id).
  `FirstTerminal` keeps only the zone-1 sequencing rules that are genuinely
  its own and forwards its zone-1 constants to the table, so there is one
  source of truth. Rejected: growing `FirstTerminal` to hold zone 2 — the
  name would then be a lie, and it is the file every zone rule would land in.
  Also rejected: renaming `FirstTerminal` wholesale, which is churn across
  ~15 files that this milestone does not need.
- **Which zone the bank gates on.** Current zone = 1 + the number of zone
  doors already open. This reproduces zone 1's behaviour exactly while D1 is
  shut, and needs no new state. Past the last authored zone the bank is not
  blocked (the finale is beats 6–7).
- **`MaxWaves` is not re-keyed.** It is an absolute end-Wave ceiling of 5;
  zone 2's Waves are 3..5 and fit under it, so no change is required here.
  The per-trigger span guard in `RunSim.Composite.cs` is unreachable dead
  code; it is left in place rather than removed inside this milestone.
  Earlier text calling the per-trigger cap "re-keyed" described an intent,
  not the tree.
- **Per-Wave progress inside a composite.** A composite that dies mid-chain
  records nothing today, so the partial election would have nothing to read.
  `RunSim` gains a `FieldWaveCleared` event raised per Wave (chain advance
  and single-Wave clear alike); the Game layer writes `DemoZoneDeepestWave`
  from it. Rejected: polling `CurrentWaveIndex` from `Update`.
- **The flag is written in the sim.** `RunSim.EndRun` still holds `Mode` and
  `Assault` at entry, so "died inside a live bank window" is
  `Mode == WaveMode.HatchBank && Assault == AssaultState.Assault` — free, and
  the same place `PodEjections++` already lives.
- **No save-version bump.** The new flags ride the existing v9 demo block;
  `ExtractBool` defaults a missing key to `false`, which is the correct load
  for every existing save. v11 stays.

## Kill criteria

Composition bet. Evaluate on the first logged play of zone 2's bank, or by
**2026-09-30**, whichever comes first. Zo's play report is read first.

1. **A rung-1 kit (no Primary) logs** `[Demo] bank window survived zone=2`
   — the bank is no longer the loss, the Primary has nothing to gate, and
   this ordering is dead before D2 is treated as reachable.
2. **Docked after this bank falls with `PodEjections == 1` does not offer
   the Primary** — the recut failed; the row is still hiding behind a second
   ejection.

## Acceptance criteria

1. Zone 2's terminal (T3) launches 3 Waves consecutively on one trigger with
   no Prep between them; `[Demo] zone start zone=2 waves=3 mode=composite`
   and `[Demo] zone complete zone=2` fire; the Hatch-bank refuses named until
   Wave 3 clears. A rung-1 kit clearing those 3 Waves is expected. It is not
   a defect.
2. `EndRun("hatch")` or player-down **during zone 2's bank window** writes
   `DemoZone2BankLost`, leaves D2 shut, and is the only Lost that opens rung
   2. A Lost during zone 2's field Waves does not write it. Both still
   increment `PodEjections` — that counter is untouched by this milestone.
3. Before the flag, the ARMORY's PRIMARY rows refuse named and
   `ShipCommands.BuyWeapon` refuses a Primary without reaching
   `SpendSalvage`. This is a new gate (*Code state* 1).
4. Docked after that flag plays `demo.second_loss` (id kept; copy restamped
   so it does not assert a second ejection), logs
   `[Demo] ladder rung=2 opened — primary + 2 buys`, and opens rung 2 = the
   PRIMARY rows + 2 wheel buys on their own counter, outside the secondary
   cap. A buy past the rung logs `[Demo] wheel refused — ladder rung spent`
   without reaching `SpendSalvage`. Rung-1 history, the MP unlock and
   `demo.mp_unlocked`'s once-per-save are all unaffected (*Code state* 3).
   The same Docked at `PodEjections == 1` and at `== 2` both offer the rows.
   The Primary is sold, not granted.
5. Retrigger at T3 defaults to composite (`start=3 waves=3` — the log carries
   absolute Wave numbers); partial election reads `DemoZoneDeepestWave[2]`,
   which is written per field-Wave clear including mid-composite, and
   launches the remainder only.
6. Bank-slot bookkeeping (`HatchBank.SlotsPerRun`, `RunSim.HatchBankSlotsLeft`)
   is retired across all 16 live call sites, the two player-visible strings
   that quote it (the HUD prep line and the Hatch prompt), the two refuse
   branch selectors that need a replacement condition, the 4 test assertions,
   `Tools/docgen/Program.cs` (a compile dependency) and its GEN block in
   `../../Design/4-Data.md`.
7. Survival of zone 2's bank with Primary + 1 logs
   `[Demo] bank window survived zone=2` then `[Demo] door open D2 cause=bank`.
   The machinery already exists and fires; what this milestone adds is a zone
   2 that is three Waves deep in front of it.
8. Dev checkpoint 4 writes the new flags instead of faking the state with
   `PodEjections = 2`, and still pays for its PRI-C through the ledger.
9. Tuning observation, open until logged (`[unconfirmed]`): a rung-1-only
   run never logs `bank window survived zone=2`. Not closable on arithmetic
   alone.
10. Playthrough: D1 open, T3 trigger, 3 Waves held on the secondary kit, bank
    falls, Docked, Primary bought → retrigger → survive with Primary + 1 →
    D2 opens → optional dock. Confirms 1–5 and 7 on screen. Valid whether it
    is the save's first ejection or its second.

## Implementation

Ordered. Beat 5's Create / Change / Delete tables are the file list, corrected
by *Code state*.

1. **Create `Assets/Scripts/Sim/Bastion/DemoZones.cs`** — the per-zone table.
   Zone 1: T1, T2; 1 Wave per trigger; Waves 1..2; door D1. Zone 2: T3;
   3 Waves per trigger; Waves 3..5; door D2. Indexed accessors
   (`TerminalCount(zone)` / `TerminalIdAt(zone, i)`), never an array-valued
   property — the prompt getters that call this run per frame.
   `ZoneOf(terminalId)`, `ZoneOfWave(wave)`, `FirstWave`, `EndWave`,
   `WavesPerTrigger`, `DeepestTerminalId`, `DoorId`, and the generalised
   `HatchBankBlocked`. **Change `FirstTerminal.cs`** to forward its zone-1
   constants here and drop `ZoneOfWave` / `WavesFor` / `HatchBankBlocked`.
2. **Change `RunSim`** — add `event Action<int> FieldWaveCleared` (1-based),
   raised on chain advance and on single-Wave clear.
3. **Change `Assets/Scripts/Sim/Meta/`** — three new demo flags on
   `MetaState` + `SaveData` + `MetaState.Json` write/read + the v≥9 branch of
   `ApplyDemoFlags`: `DemoZone2BankLost` (bool), `DemoRungTwoOpened` (bool),
   `DemoRungTwoBuysLeft` (int, default 0). The pre-v9 branch sets all three
   to their defaults rather than to `played`; comment why. No version bump.
4. **Change `RunSim.EndRun`** — write `DemoZone2BankLost` when the end is not
   docked and the window was live and the Wave belongs to zone 2.
5. **Delete slot bookkeeping** — criterion 6's list.
6. **Change `GameController`** — zone-generic terminal trigger (composite
   when `WavesPerTrigger > 1`, the two zone logs), zone-generic bank gate,
   `DemoZoneDeepestWave` written from `FieldWaveCleared`, and the retrigger
   offer / spawn activation / completion bookkeeping generalised off the zone
   table (`DemoRun.RetriggerOffer`, `ActivateZoneOneSpawns`,
   `RecordRetriggerClear`).
7. **Change `ShipCommands` + `ArmoryStation` + `DemoProgress`** — the Primary
   gate, rung-2 counter and its named refuse, and
   `NoBuyableUpgradeRemains` reading the summed counters.
8. **Change `DemoShipDirector`** — the rung-2 Docked resolve: play
   `demo.second_loss`, log the rung, set the counter, latch
   `DemoRungTwoOpened`. Restamp the copy in `DemoLines`.
9. **Change `GameController.DevCheckpoints`** — criterion 8.
10. **Tests** — criterion 6's four assertions updated; new EditMode coverage
    for the zone table, the flag write, the rung-2 ladder and the Primary
    refuse; `FirstReturnSourceTests` extended rather than rewritten.

**Do not** key the Primary rows, rung-2 buys or `demo.second_loss` on
`PodEjections`. **Do not** raise `DemoWheelBuysLeft` for rung 2. **Do not**
delete the terminal-AND graph or D3.

## Supporting work

- **Driven by** `E`/`T` on T3 and the Hatch; ARMORY Primary buy after this
  bank falls; retrigger election; optional dock — here.
- **Shown by** 3 Waves off one trigger with no Prep between; the bank falling
  on the secondary kit; the PRIMARY rows refusing before the loss and selling
  after it; D2 opening on survival — here.
- **Reads back as** `[Demo] zone start zone=2 waves=3 mode=composite`,
  `[Demo] ladder rung=2 opened — primary + 2 buys`,
  `[Demo] bank window survived zone=2`, `[Demo] door open D2 cause=bank` —
  here.
- **Co-op buy-back** is `M90`, not here. If it slips, this milestone still
  ships whole: buy-back was never on the solo path this beat teaches.

## Verification

`Tools/lint-arch/run.sh` clean, the offline EditMode harness green, then the
AutoVerify file bridge for anything touching a MonoBehaviour, a scene or the
save. Named per criterion:

- **1** — new EditMode zone-table tests plus the session log: one `T` at T3
  produces one `zone start … waves=3 mode=composite`, three Wave clears and
  one `zone complete zone=2`, with no Prep line between them; a bank attempt
  before Wave 3 logs the named refuse.
- **2** — EditMode: `EndRun` inside a live zone-2 bank window sets the flag;
  `EndRun` during a zone-2 field Wave does not; both move `PodEjections`.
- **3, 4** — EditMode over `ShipCommands`: a Primary buy before the flag
  refuses named with `BankedSalvage` unchanged; after the flag it succeeds; a
  third rung-2 wheel buy logs `wheel refused — ladder rung spent` with
  `BankedSalvage` unchanged; `MultiplayerUnlocked` is true throughout rung 2
  and `demo.mp_unlocked` is logged exactly once across both rungs.
- **5** — EditMode: a composite dying on Wave 4 leaves `DemoZoneDeepestWave[2]`
  at **3**, and the next offer's partial start is **3**, not 4. The shipped rule
  is `partial = deepest` — at least one Wave refights before the bank re-arms
  (`DemoRun`, and beat 4's whole-zone-cleared floor). An earlier draft of this
  line said 4, which would have been a silent change to zone 1's shipped
  behaviour; the shipped rule stands and this criterion follows it.
- **6** — the offline harness compiles with no reference to the deleted
  symbols; `Tools/docgen/run.sh --check` passes against the regenerated GEN
  block.
- **7, 10** — the playthrough, from Play with dev checkpoint 3 (D1 open).
  Read the log for the four strings above in order. Criterion 10 is Zo's and
  is this milestone's close gate; it is **not** closable from a dev-tainted
  session.
- **9** — stays open. Not closable on arithmetic.

## Verification run — 2026-08-20

Built and checked the same day it was started. What was actually run, and what it
could and could not see.

**Lints** — `lint-language`, `lint-arch`, `lint-vendor` all clean;
`Assets/_Scratch/` empty.

**Offline EditMode harness** — `mcs`/`mono` are not installed on this machine, so
`Tools/verify/run.sh` cannot run past its lint stage. The compile+run stages were
replicated faithfully under `dotnet` (Sim alone against `Tools/verify/Shims.cs`,
then `Game/Settings` + `Game/Demo` + the tests against that dll, minus run.sh's
EXCLUDED list, with `Tools/verify/Runner.cs`). **520 passed, 9 failed.** All 9 are
pre-existing and none is in this milestone's surface: 8 scene/prefab-YAML
assertions (`HudSeparationTests` ×2, `M59PolishChromeTests` ×5,
`SessionMusicHostTests`) plus `DemoVoiceTests.BeatDocs_CitedIdsAreInTheRegistry`,
which fails because `Docs/Demo/` was renamed to `Docs/Demo - Single/` in an
unrelated working-tree change — the test hardcodes the old path.

**Full Unity suites via the integration-check file bridge** — run twice, with an
**identical** failing set both times, so these are stable rather than the PlayMode
flake this machine is known for. **EditMode 581 passed / 10 failed; PlayMode 54
passed / 3 failed / 1 skipped.** EditMode's 10 are the offline 9 plus
`BastionDoorApertureTests` (3.5-vs-4.5, pre-existing). The 3 PlayMode failures are
each shown not to be this milestone's:

- `NetVerbTests.Repair_ClientChannel...` — the known repair-timing 0.5 HP
  tolerance miss, recorded before this work.
- `BastionCombatAndDeathTests.Firing_DamagesAnEnemy...` — no file changed by this
  milestone is in the firing path.
- `CoreLoopPlayModeTests.CoreLoop_AutoBoard...` — asserts
  `MetaUpgrades.UpgradesUnlocked` after a player death, with **no Hatch-bank
  anywhere in the test**. That is the pre-`M78` rule: `M78` re-keyed the unlock to
  `HatchConversions >= 1`, which this run never reaches. A stale assertion `M78`
  left behind; this milestone's diff touches neither symbol.

**The gap this run closed that the offline harness structurally cannot.** The
offline harness compiles only `Sim/`, `Game/Settings` and `Game/Demo` — **not
`Game/Bastion` or `Game/Ship`**. The first bridge run came back
`status: error, stage: precheck` on a real compile error (a stale
`ReadRepairView` call site left by the slot removal) that every offline check had
passed over. Anything touching those folders must go through the bridge.

**Proved twice, per the verification rules.** The two new behaviour suites were
each run green on the real tree *and* against a seeded violation:
`ZoneTwoBankLossTests` (8 tests) catches removing the live-window condition from
`EndRun` — the seed makes a zone-2 *field* loss write the gate;
`DemoProgressTests`' new ladder cases catch reverting
`NoBuyableUpgradeRemains` to reading rung 1 alone. Both seeds were reverted and
the tree re-verified.

**Not run.** `Tools/docgen/run.sh --check` (needs mono); the `4-Data.md` GEN block
was instead diffed against docgen's actual output under `dotnet` and is
byte-faithful. No clean-tree baseline run was taken — the working tree carries
unrelated concurrent edits, so the pre-existing failures above are argued from the
recorded set and from which files this milestone touches, not from a fresh
baseline.

**Closing numbers (2026-08-20, final).** Lints clean. Offline harness **522 passed
/ 8 failed**. Full Unity suites **EditMode 594 / 9 failed**, **PlayMode 49 / 11
failed**. Every EditMode failure is the recorded pre-existing set. The 11 PlayMode
failures were **attributed by experiment, not by argument**: this milestone's test
file was removed from the tree and the suite re-run, and the same 11 failed
without it. They belong to **`M91`, in progress in the same working tree**:
that milestone renames the wake capsule to a teleporter, and until the Ship scene
is re-authored `DemoShipDirector` logs `[Demo] no teleporter wired` as an ERROR,
which the runner turns into a failure for every fixture that loads the Ship.
Unrelated to zone 2. (An earlier draft of this note blamed the machine's pre-v9
saves; the error string names the real cause.)

**End-to-end evidence (criteria 1–5, 7).** `ZoneTwoLoopPlayModeTests` drives the
**real `Bastion.unity`** through the real `GameController`: the bank refuses while
zone 2 is incomplete; one trigger on the zone's terminal launches one composite
and clearing a Wave inside it does **not** return to Prep; the zone completes and
the bank unblocks; a Hatch breach inside the window writes the gate and leaves D2
shut; surviving opens D2 and leaves the gate shut. Every zone fact is read from
`DemoZones`, so the test survives the table changing. Both tests pass.

**A defect found and fixed during the close.** A Lost on the Master Control Hatch
half was writing `DemoZone2BankLost`, because the Master terminal half draws past
`MaxWaves` and `ZoneOfWave` **floors** a deeper Wave onto the last authored zone.
The gate now asks `DemoZones.WaveIsIn(zone, wave)`, which does not floor; the
floor is still the right read for naming a zone in a log. Pinned by
`AWaveDeeperThanZoneTwo_DoesNotWriteTheGate`, proved against the seeded old
behaviour.

**A test-hygiene defect found and fixed during the close.** The first draft of the
PlayMode fixture called `GameSession.EnsureLoaded()` so it would always have a
session to snapshot. That pulled the developer's campaign into the test session
and left it there, and every later Ship fixture then loaded a demo-active Ship.
A fixture may not decide what the session is for the fixtures after it; it now
snapshots only a session that already exists and hands back an inert one
otherwise.

**Criterion 10, closed under the new allowance.** Zo ruled 2026-08-20 that a
dev-tainted session may close a milestone gate (`../../Decisions.md`), and this is
the first application. **Stated plainly: no on-screen playthrough was observed by
the implementer.** What closes it is the end-to-end PlayMode evidence above,
which drives the real scene rather than a stub. What that evidence cannot see is
named in the next paragraph.

**Criterion 9 stays open and is carried forward.** The PlayMode tests do not
fight — bodies are despawned rather than killed and the window is advanced by
ticking — so they prove the loop's **shape**, not its **difficulty**. Nothing here
says whether a rung-1 kit can hold zone 2's three Waves, or whether the bank sits
above that ceiling and below Primary + 1. That is the composition bet, it needs a
logged Run, and it is the one thing this close does not settle.

## Open

- Zone 2 terminal reachability while D1 is shut — T3 sits behind D1 in
  `Bastion.unity`, and the bank gate now keys on the current zone, so a shut
  D1 keeps the player in zone 1 by construction. No refuse surface is needed.
  Recorded rather than built.
- `demo.second_loss` copy — the restamp is owed to Zo. The shipped line opens
  "Thrown off again", which asserts the second ejection this recut forbids;
  the implementer replaces it with a cause-agnostic draft and Zo overrides.

## Traps

Beat 5 Traps apply, with the four highest-cost restated: keying rung 2 on
`PodEjections` (a player who docked zone 1 never sees the Primary), making
the 3 field Waves the loss (the previous Wave is survivable — that is the
recut), softening the bank so the secondary kit can clear it (breaks the
ordering and the dock-skip guard at once), and raising `DemoWheelBuysLeft`
for rung 2 (*Code state* 3 — it silently re-locks multiplayer).

Fifth, new: shipping the rung without the gate. Because the Primary rows are
open today (*Code state* 1), every criterion about rung 2 can pass while a
player still buys PRI-C in beat 3. Criterion 3 is the one that closes that.

M76's record that zone 1's first bank is authored-unclearable is not
rewritten here. This milestone does not require a prior loss.

## Depends on / Unblocks

Depends: M77 (door keying, dock, snapshot), M78 (plain embark, retrigger
offer, ladder rung 1). Unblocks: M80 (D2 open, party at the Control Room),
`M90` (buy-back rides this milestone's bank window).
