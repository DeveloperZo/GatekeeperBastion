# M70 — The Default Protocol

<!--
Status: later
Update trigger: when a Default sheet row is renamed, moved or cut, when a window or
cooldown moves off its starting value, when the doctrine mode enum gains a value, or
when the VEIL slice ships and the deed spine lands.
Last updated: 2026-08-15
-->

CREW QUARTERS stops selling the four attribute tracks and equips **Boarder
Protocols**. This slice ships the protocol frame and one protocol: the **Default
Protocol** — the Default Gatekeeper's mirror, the plain kind worn by a player. Its
doctrine is **STOCK** (hardware exactly as sold), its Crew ability is **Anchor** on
`G`, its Personal ability is **Bulkhead** on `H`, and its passive is a pick of
**Ghost** or **Foresight**. Owned and equipped from the first session; every later
protocol reads as a deviation from it. The specialist sheets and the deed unlock
model stay in `../../Brainstorms/boarder-protocols.md`.

**Recut 2026-08-15 (Zo): free-pick pools became protocols, and this slice shrank to
the Default.** The 2026-08-14 cut (three slots, free picks per pool, formerly titled
"The Boarder gets a verb") is superseded: identity moves from emergent reads of picks
to named protocols, per the brainstorm's overturn of this file's own "Sold
archetypes" Do-not-build row — the doctrine layer has no carrier in free-pick
grammar. AEGIS was cut from the slice by direction (it must not block the system; its
Steadfast clause waits on a damage-interrupts-repair pre-req). The **turret half
(`UTL-H`) moves out whole** to the FORGE slice: its criteria spent a "Primary
reserve" that `PlayerGun` does not have (`clipRemaining` / `MagazineCapacity` only),
so it waits on the ammo-reserve decision at its own milestone. Ping, Cloak, Recall
and Warden move to their protocols' slices (VEIL, LEDGER, AEGIS).

## Story

As a Boarder, I embark wearing the Default Protocol — I can plant an Anchor to slow
the room for my crew, arm a Bulkhead to eat one hit I chose in advance, and pick the
rule that says how Gatekeepers read me — so that `G` and `H` are verbs my teammates
can name, and CREW QUARTERS equips identity a Run can feel rather than percentages it
cannot.

## Why it matters

The tracks had no job a weapon cannot do: a level's `+4.5%` sits under the measured
`~20%` run-to-run spread (`../../Design/4-Data.md`, quoting B1 in
`../../Design/2-Dynamics.md`) — no attribute purchase was ever perceptible.
Abilities change *what* the Boarder can do rather than *how much*, the only kind of
purchase that clears that floor.

Every row is a trade, not a grant (Zo, 2026-08-14): Anchor buys area control with
your mobility, Bulkhead buys one negated hit with timing paid in advance. No row
grants damage — the reload/clip/damage/fire-rate currencies belong to the M21 wheel.
A Passive is a rule, never a number; identity lives in the always-on layer.

The Default ships first because the frame has no player-visible existence without one
sheet, and the Default's rows are the plainest verbs on the cheapest seams — no
Medium mechanic in the unit (brainstorm mechanics bill: items 6 and 10 only; Anchor
needs one speed-recompute funnel beside `Provoke()`'s existing `_agent.speed`
writes, Bulkhead the one Small hook, Ghost is a parameter change).

Leverage tier (`../../Working-Rules.md`): **#5, rules** — what CREW QUARTERS sells
and what a Boarder can do changes. Windows, cooldowns and radii below are **#12
starting values** tuned only after logged Runs.

## Kill criteria

This is a bet. Evaluate at **10** logged Runs after close, or by **2026-09-30**,
whichever comes first. Loop, not arc: roster × hardware × twists × waves is the
ongoing cycle; this slice is that loop's first cycle with no rival protocol.

1. **Reflex, not decision** — every activation on either active slot fires within
   **2 s** of the cooldown ending (the activation line carries seconds-sat-ready).
   Stop and recut. Mash-on-cooldown fires this; that is the observation.
2. **The split collapsed** — one active slot carries fewer than a fifth of the
   other's activations (both counts are in the log, per slot). The slots answered the
   same moment; fold back to one. This tests abandonment, not the Crew-beneficiary
   rule (a Crew press that helped only the presser is a co-op observation, named for
   M65 / VEIL, not this sample).
3. **Dead first session** — a fresh save's first embark logs both abilities unused.
   The Default sheet is too thin; widen it (brainstorm kill criterion 7). Session
   notes do not close this.
4. **Bulkhead is a guess** — more than half of Bulkhead windows in the sample expire
   un-triggered (the expiry line exists for this). The window is unreadable; recut
   the window length or the arming rule, once. There is no graze path today
   (`EnemyGatekeeper.Attack` → `PlayerHealth.TakeDamage(playerDamage)`); the
   negation line logs the amount so a later smaller-hit source has data.
5. **Ghost wiring / Foresight unused** — Ghost dies this slice only if
   Ghost-equipped Runs show acquisition at the unbiased `noticeRange` / `aggroRadius`
   (bias not applied). Perceptibility waits on VEIL's Sightline; do not apply the
   kill-rate ~20% floor to time-to-first-acquisition (`../../Brainstorms/crew-skills-and-perks.md`).
   Foresight dies if Foresight-equipped Runs contain zero preview lines.

## The Default sheet

Windows, cooldowns and radii are #12 starting values. They sit far above the
weapon-ability band (`WeaponAbility.Cooldown` 6–12 s) so `F`, `G` and `H` read as
different classes of thing. The sheet ships naked: twist slots and their pairs are
later slices per the brainstorm's paced-unlock grammar.

| Slot | Row | Rule | Trade | Window / Cooldown |
|---|---|---|---|---|
| Doctrine | **STOCK** | Hardware exactly as sold; deployed decoys carry the mode field, and STOCK writes 0 | The reference other doctrines recolor | always on |
| Crew `G` | **Anchor** | Plant in place; Gatekeepers within 8 m move at 0.5×; unplant to move | Your mobility for area control | up to 8 s / 30 s |
| Personal `H` | **Bulkhead** | Window: the first Gatekeeper swing that lands on you is negated (`TakeDamage` from `Attack`); the window ends on that negation | One hit erased, bought in advance with timing | 6 s / 40 s |
| Passive (pick 1) | **Ghost** | Gatekeepers acquire you at **0.70×** `noticeRange` and `aggroRadius` — a bias on the existing tests | Never invisibility; a bias, not a veto | — |
| Passive (pick 1) | **Foresight** | Field terminals name the next Wave's `hunterCount` / `ramCount` / `defaultCount` from `WaveDirector.NextWave(idx, siphonLevel: 0)` — never the juiced `enemyCount` | Information, not power | — |

Kind refusals stand everywhere: a Ram never retargets, a Hunter never touches the
Hatch, and no row above overrides either. Ghost's counter-case is in Watch: it does
not delay a Hunter's hunt-from-breach.

## Acceptance criteria

1. **CREW QUARTERS shows the protocol.** One PROTOCOLS panel: the Default Protocol
   named, its doctrine text ("STOCK — hardware as sold"), both abilities with key
   labels, and the passive pick (two rows, one EQUIPPED); no attribute row remains.
   Purpose line stays "you" (M59 criterion 7). The authored heading in `Ship.unity`
   reads PROTOCOL (SceneBuilder does not own it).
2. **The Default is owned and equipped from the first session.** Fresh saves and
   migrated saves alike embark with it — no purchase, no deed, no empty slot state.
   Passive re-pick is free and Ship-only under `PayloadLoadoutLocked` (M42 idiom, not
   renamed — M57's shipped text uses that name); a mid-Run re-pick is a logged
   refusal. Ghost is the granted first Passive; the pick is not forced.
3. **Anchor plants.** `G` locks WASD (`FirstPersonController.Rooted`); look and fire
   stay. Gatekeepers inside 8 m move at 0.5× agent speed while inside; `G` again (or
   window end) unplants; cooldown starts at unplant. `G` during cooldown is criterion
   6, not a second plant.
4. **Bulkhead negates one hit.** `H` arms the window; the first `PlayerHealth.TakeDamage`
   from `EnemyGatekeeper.Attack` on the caster zeroes the amount and clears the flag,
   one logged line naming the Gatekeeper and the negated amount; a window that expires
   un-triggered logs its expiry. Arming during cooldown, or while the window is already
   open, is a logged refusal (no refresh).
5. **The passive leaves a trace.** The embark log names protocol and passive. Ghost's
   0.70× applies server-side in `CanSeePlayer()` and the `Retarget()` aggro-radius
   check only — never inside `PlayerRegistry.NearestAlive`. Foresight: `BastionTerminal.AppendStake`
   lists the three siphon-0 kind counts when Foresight is equipped, and never the
   juiced `enemyCount`. Intensity / juice / unbanked stay on the stake as today.
6. **Cooldown reads back on both keys.** `G` or `H` during that slot's cooldown is
   one logged refusal naming seconds left; each activation line carries the slot and
   seconds-sat-ready (kill criteria 1 and 2 read these).
7. **HUD.** `Hud Vitals` gains two ability lines — Crew and Personal, each name plus
   READY / countdown / window state — and the passive's name beside them, extending
   the locked host (`../../Decisions.md`), no `GUILayout`. Authored TMPs on
   `BastionHudView` in `Bastion.unity` (they survive Build All). If the host grows,
   the Decisions size cell updates in this change and the M59 ≥24 AABB check is
   re-run at 1920×1080 and 1280×720 as this criterion's last step. The shortcut panel
   shows `G crew` and `H self` from the `Controls` list with no panel edit.
8. **Doctrine mode is written at decoy place.** `DecoyState.TryPlace` takes the
   placing player's `DoctrineMode` and stores it; STOCK is 0. An EditMode test asserts
   the field equals the placer's equipped doctrine **and** that pull radius, max-live,
   HP and decoy pull match today's baseline. Repair is hold-E (`RepairInteractable`),
   not a place path, and is out of this slice. The field is a write for VEIL to read;
   STOCK-equals-baseline does not prove a later clause.
9. **Net.** Each verb applies once, server-side (M48 invariant): a client Anchor
   slows host bodies; Bulkhead's negation runs in the host's damage path; Ghost's
   bias lives in server-side targeting state set from the equipped ids at spawn;
   Foresight's preview is served from the host's `WaveDirector`. Requester gates on
   equipped-in-slot (M51 rig-gate idiom); cooldowns run client-side, same as M51 —
   the server does not track protocol cooldown. `NetProtocolTests` proves single
   application. Spawn RPC carries protocol and passive ids; server accepts any
   catalog-legal Default pair (Ghost or Foresight). Deed-gated protocols need a
   server unlock copy — that is VEIL.
10. **The tracks are gone.** `UpgradeTrack` holds only `HatchArmor`; `MetaUpgrades.PlayerMaxHp()`,
    `StaminaMax()`, `SalvagePerKill()` become parameterless and return **100**, **100**,
    **25**; `CritChance()` returns 0; the 2× roll in `PlayerGun.Firing` stays — Luck-shaped
    crit may return, and no Boarder ability may ever be a crit source. `RunSim.BaseKillValue`
    and `GameController.ApplyMetaEffects` call the parameterless methods. Docgen drops
    the rows from `4-Data.md` with no generator edit (M33).
11. **Old saves refund.** Loading v7 or older refunds exactly `200 × (2^level − 1)`
    per track into banked (max case: 4 maxed tracks = **24,800**), matching
    `MetaState.SpentOnTrack`; `SalvageLedgerBalances` stays true, `SaveVersion` is
    **8**, the refund is one logged line. Level fields leave `MetaState`; `SaveData`
    keeps them parse-only beside `RepairRigLevel`.
12. **Solo unchanged otherwise.** Both suites green plus the new tests; language lint
    passes; no `[Dev]` in any close log. **No new counter, no accumulator, no stats
    object** (`../../Decisions.md`) — the protocol and passive equip fields are
    state, not counters, and the deed counters are explicitly not this slice (their
    exception is named at the VEIL slice that ships them). **No new menu row without
    asking** (`../../Architecture/README.md`).

## Implementation

Order: sim types and migration, then the solo verbs, then net, then station and HUD,
then docs.

1. **Create** `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` — `BoarderProtocolId`
   (`None=0, Default=1`; save-adjacent, never renumber), `DoctrineMode`
   (`Stock=0`; later protocols append), `BoarderAbilityId`
   (`None=0, Anchor=1, Bulkhead=2, Ghost=3, Foresight=4`), a catalog binding the
   protocol to its doctrine, ability and passive ids **and holding every number as a
   function of ability id** — `Window(id)`, `Cooldown(id)`, `Magnitude(id)`, the
   `WeaponAbility` idiom (`Magnitude(Ghost)` is **0.70**; `Magnitude(Anchor)` is the
   0.5× slow). The verbs and step 9's server clamps read only these, so a later twist
   inherits them untouched. `BoarderAbilityState` for the two active slots
   (`WindowRemaining`, `CooldownRemaining`, `Tick(dt)`, `TryActivate()`,
   `EndWindowEarly()`) — pure, dumb on purpose (trigger policy stays with the
   caller; later twists change who calls, not the struct), mirroring `WeaponAbility`
   + `PinBuildState`, harness-testable. Passives carry no state struct.
2. **Change** `Sim/Meta/MetaState.cs` — `EquippedProtocol` (defaulting to `Default`)
   and `EquippedPassive` (defaulting to Ghost; re-pick Ship-only). Gate the passive
   pick on existing `PayloadLoadoutLocked` (one lock, one mechanism — M42). Do not
   rename the flag. Delete `VitalityLevel`/`StaminaLevel`/`ScavengingLevel`/
   `LuckLevel`; drop their lines from `ReconstructSpentFromLevels`; in `FromSaveData`,
   v<8 refunds `Σ 200×(2^level −1)` — `BankedSalvage +=`, `LifetimeSalvageSpent −=`,
   so the ledger identity holds unchanged.
3. **Change** `Sim/Meta/SaveData.cs` — `CurrentVersion` 8; protocol + passive fields;
   the four level fields move to the legacy parse-only region beside
   `RepairRigLevel`.
4. **Change** `Sim/Meta/UpgradeTrack.cs` / `MetaUpgrades.cs` — enum keeps
   `HatchArmor` only; the four formulas become the parameterless methods in
   criterion 10; `CritChance()` returns 0 and `PlayerGun.CritChance` returns 0 (the
   2× roll in `PlayerGun.Firing` stays). `UpgradeTracks.All` drops the four. Call
   sites that passed a level (`RunSim.BaseKillValue`, `GameController.ApplyMetaEffects`)
   switch to the no-arg form.
5. **Change** `Game/Input/Controls.cs` **and** `Game/Input/PlayerInputFrame.cs` —
   `ControlId.CrewAbility`, `KeyCode.G`, display `G`, label `crew`;
   `ControlId.PersonalAbility`, `KeyCode.H`, display `H`, label `self`; both section
   FIGHT, `FightAlways`. Add `crewAbilityDown` / `personalAbilityDown` bits; wire
   `Sample`, `Down` (and `Held` only if a later twist channels). `F` is the gun's
   ability, `G` is the crew's, `H` is yours. Shortcut panel rows follow
   `Controls.CollectPanelRows` with no panel edit.
6. **Create** `Game/Player/PlayerProtocolAbilities.cs` — LOCAL-OWNER, the
   `PlayerAbility` pattern: reads the equipped ids pushed at `ApplyMetaEffects`
   (push-at-spawn, `../../Architecture.md` gotcha 1 — never serialize them), polls
   both control ids, `SetHosted` tick, exposes name/window/cooldown per slot and the
   passive name for the HUD, logs `Log.Event("Protocol", ...)` with slot and
   seconds-sat-ready on every activation and refusal. Sets `FirstPersonController.Rooted`
   while Anchor is planted (move locked, look and fire stay — `MovementLocked` is the
   pause/death freeze and must not be stomped).
7. **Create** the verbs, game-side:
   - Anchor: `Game/Bastion/AnchorField.cs` — host-only planted marker at the Boarder's
     position, static live list, 8 m radius from the catalog. `EnemyGatekeeper` gains
     `RecomputeSpeed()`: `_agent.speed = (provoked ? chaseSpeed : patrol) * slowMul`,
     where `slowMul` is 0.5 when inside any live field, else 1. `Configure`, `Provoke`,
     and ring enter/leave all call it (Suppressor and Ward Stone later land in the
     same funnel). Never write `_agent.speed` elsewhere. FPC `Rooted` while planted.
   - Bulkhead: an armed-window flag consumed at the top of `PlayerHealth.TakeDamage`
     when `source` is a Gatekeeper — zero the amount, clear the flag, log. Invulnerable
     and dead still return before the flag.
   - Ghost: multiply `noticeRange` and `aggroRadius` by `Magnitude(Ghost)` **at the
     test**, against the `_player` currently selected — never write the serialized
     fields (gotcha 1). `Retarget` still assigns `_player = PlayerRegistry.NearestAlive`
     first; Ghost does not change who. In 2p, a nearer Ghost Boarder can occupy
     `_player` and fail acquire while a farther one is never checked — accepted this
     slice; "who" is the windowed-override family's seam.
   - Foresight: `BastionTerminal.AppendStake` asks `WaveDirector.NextWave(idx, 0)` and
     appends `hunterCount`/`ramCount`/`defaultCount` when the local (host-served)
     equipped passive is Foresight. `idx` is `sim.CurrentWaveIndex` (the wave that
     would start if you pulled now). No new panel.
8. **Change** `Sim/Run/DecoyState.cs` — `DoctrineMode Mode` set by
   `TryPlace(float x, y, z, DoctrineMode mode, out reason)`; `DecoyObject.Configure`
   mirrors it for display. Callers (`NetVerbs.PlaceDecoyServerRpc` and the solo place
   path) pass the placing player's equipped protocol's mode. STOCK is the only value
   this slice; the EditMode test pins field-is-written and baseline pull/hp/max-live.
   Do not thread a mode through `RepairInteractable`.
9. **Change** `Game/Net/NetVerbs.cs` —
   `RequestAnchorServerRpc(Vector3 position, bool planting)`,
   `RequestBulkheadServerRpc()`; server applies exactly as the solo path; magnitudes
   clamped to catalog values like the pin/concussive RPCs. Passives need no per-use
   RPC. **Change** `Game/Net/NetPlayerAvatar.cs` — no shipped mechanism carries a
   client's equip state (`Hp` and `WaitingOnShip` only). Add one spawn RPC carrying
   the equipped **protocol and passive ids**; the server stores them on the avatar
   (two `NetworkVariable<int>`, server-write, like `Hp`) and derives Ghost's bias and
   doctrine-at-deploy from the catalog. Ids, never derived flags: every later
   protocol, doctrine deploy and elite read rides this one push.
10. **Change** `Game/Ship/CrewQuartersStation.cs` + `Game/Ui/CrewQuartersPanelView.cs`
    — the PROTOCOLS panel of criterion 1 replaces the attribute rows, and it
    **enumerates catalog rows** (the ARMORY `WeaponCatalog.ShopWeapons()` precedent)
    — one row today, never a hardcoded Default, so VEIL is a catalog row and a deed
    line, not a panel edit; `ContentHash` from protocol + passive; `PromptHint` "your
    protocol"; the authored ATTRIBUTES heading in `Ship.unity` becomes PROTOCOL.
    `SprintSeconds` dies with the rows.
11. **Change** `Game/Ship/ShipCommands.cs` — `PickPassive(id)` on the payload idiom
    (refusals logged, `track=crew` token); the four cases leave `BuyUpgrade`
    (`HatchArmor` stays).
12. **Change** `Game/Bastion/HudController.Fight.cs` / `Game/Ui/BastionHudView.cs` —
    two ability lines plus the passive name under the `Hud Vitals` rows, the
    weapon-detail string idiom from `HudController.Weapon.cs`. Author the TMPs in
    `Bastion.unity`; do not have SceneBuilder emit them.
13. **Create** tests — EditMode `Assets/Tests/EditMode/Meta/BoarderProtocolTests.cs`
    (catalog pins including Ghost 0.70, state machine, passive pick + lock, refund
    arithmetic, ledger balance, removed-track APIs gone — the M23 `RemovedTracksAreGone`
    shape in `EquipmentTests`; STOCK field-written + baseline decoy); PlayMode
    `Assets/Tests/PlayMode/NetProtocolTests.cs` (M48 `NetcodeIntegrationTest` fixture
    as `NetKillTests` / `NetVerbTests`; criterion 9).
14. **Change** docs — `language.md`: **Boarder Protocol**, **doctrine**, **Crew
    ability** (drop *designed, not built*; retarget off `crew-skills-and-perks.md` onto
    this file), **Personal ability**, **Passive**, **twist** (reserved, ships later),
    working-name rows (DEFAULT, STOCK, Anchor, Bulkhead, Ghost, Foresight) marked
    renameable; four track rows leave the tracks table; stations table CREW line.
    `../../Decisions.md`: Locked ship-terminals CREW line narrows to FABRICATION,
    dated; HUD Vitals contents/size cell if the host grew; the "Sold archetypes"
    overturn is recorded there, dated, citing the brainstorm (no such row exists in
    Decisions today — this is the first write). `../../Brainstorms/fabrication.md`
    gains the doctrine note (decoy place writes a mode field; repair is not a place).
    `Play/The-Ship.md` CREW section recut in the Play voice. `1-Experience.md` CREW
    row: protocols, not four tracks. `3-Mechanics.md` §3.2 (5-skill) gains a dated
    line that protocols occupy CREW and 5-skill stays parked in
    `crew-skills-and-perks.md`. `4-Data.md` regenerates via `Tools/docgen/run.sh`.
    Roadmap row and Changelog per lifecycle.

Rejected, by name: **free-pick pools** — the 2026-08-14 grammar; a stealth player
choosing between rival rows was a false choice, and the doctrine layer has no carrier
without a named, equipped protocol (`../../Brainstorms/boarder-protocols.md`, "Recut
2026-08-15"). **AEGIS in this slice** — deferred by direction (Zo, 2026-08-15); it
must not block the system, and its Steadfast clause needs the damage-interrupts-repair
pre-req first. **The turret in this slice** — moved to the FORGE slice whole; its
criteria spend an ammo reserve that is a weapons-lane decision, not a protocol task.
**Ping, Cloak, Recall, Warden in this slice** — they are VEIL, LEDGER and AEGIS
material now; each ships with its protocol. **Twist slots in this slice** — the
machinery ships with the first authored pairs (VEIL's slice). **Deed counters in this
slice** — the unlock spine ships with its first customer, the second protocol; a gate
with no prize is vapor. **Overclock, or any damage-granting row** — the wheel owns
dps. **Numeric passives** — an attribute track wearing a new panel. **A Medic or heal
row** — no heal path exists; the lane waits on the `player {hp}/{max}` instrument.
**XP in any form** — the deed model deletes the XP field entirely; the 2026-08-12 cut
stands stronger than before. **Grant instead of refund** — no ability succeeds an
attribute, so the refund lets the player re-decide. **Renaming `PayloadLoadoutLocked`**
— cosmetic; shipped M57 text uses the current name. **Doctrine on repair** — not a
place path. **STOCK-equals-baseline as seam proof** — that test never inspects a
reader; VEIL's Phantom / Ghost-hands is the first reader.

## Supporting work

- **Driven by** `G` and `H` (two new `Controls` rows) and the CREW QUARTERS panel —
  here.
- **Shown by** the shortcut panel rows (automatic from `Controls`), the PROTOCOLS
  panel, and the `Hud Vitals` ability lines — here.
- **Reads back as** the HUD countdowns, one `Protocol` log line per activation and
  refusal with slot and seconds-sat-ready, the Bulkhead negation and expiry lines,
  the passive trace lines, and the embark line naming protocol + passive — here.

## Verification

1. Criteria 10–11 — `Tools/verify/run.sh --filter BoarderProtocol` plus the migration
   tests: seed a v7 save with known levels, assert banked rises by the exact refund
   and `SalvageLedgerBalances`. Docgen check: `Tools/docgen/run.sh --check` exits 0
   only after regeneration.
2. Criterion 9 — `gkb_verify_request.txt`; `NetProtocolTests` Passed in
   `gkb_verify_report.json`.
3. Criterion 8 — the STOCK field-written + baseline EditMode test.
4. Criterion 7 last step — M59 AABB ≥24 at 1920×1080 and 1280×720 if `Hud Vitals`
   grew.
5. Playthrough path — fresh save → Ship: PROTOCOLS panel shows the Default equipped
   (Ghost), pick Foresight → embark → start a Wave → `G` plants Anchor (WASD dead,
   look/fire live) and Gatekeepers inside 8 m drop to 0.5×; `H` arms Bulkhead and the
   next `Attack` that lands is negated with its log line (amount included); both keys
   during cooldown log refusals naming seconds left; `H` while the window is open
   logs a refusal. Terminal stake shows hunter/ram/default at siphon 0, and the
   juiced extra Defaults are absent from that list after juicing. Close log carries
   one `Protocol` line per activation with slot and seconds-sat-ready, and no
   `[Dev]`.
6. Criterion 2's migration half — load a v7 save: refund line, Default equipped,
   Ghost passive, embark works with no purchase.

## Do not build

| Not building | Why not |
|---|---|
| A second protocol, or the deed spine | VEIL's slice; the spine ships with its first customer |
| Twist slots or any twist pair | Later slices per the paced-unlock grammar |
| The `UTL-H` turret | FORGE slice, behind the ammo-reserve decision |
| Doctrine clauses that change behavior | STOCK writes 0; the first reader is VEIL |
| Doctrine on the Repair Rig | Hold-E channel, not a place; fabrication note only |
| Overclock, or any damage-granting row | The wheel owns dps; named kill condition |
| A numeric passive | Under the ~20% floor by construction |
| A heal or revive row | No heal path; M57 owns the down rule |
| A crit source anywhere | Luck stays the only crit source, even at chance 0 |
| Teammate displacement, permanent mobility | Cut and locked; Anchor roots only the caster |
| `PayloadLoadoutLocked` rename | Shipped M57 vocabulary |

## Watch

- **This slice's whole bet is that plain is playable.** The Default has no doctrine
  clause and no twists — kill criterion 3 (dead first session) is the instrument. If
  the Default reads as an empty tutorial rather than a plain identity, the fix is
  widening its sheet, not rushing VEIL.
- **Ghost does not delay a Hunter.** `Retarget` sets `_chasingPlayer` for a Hunter
  via `!hatchAvailable` (`EnemyGatekeeper.Targeting.cs`); the notice and aggro tests
  never have to pass for that hunt to start. Ghost's bias is Default acquisition
  (sight / aggro to *start* a hunt). A Hunter never gains a Hatch path from Ghost
  (`MayTargetHatch` is false). A Ram never retargets (`CanBeProvoked` is false).
- **STOCK field-written is the slice's regression net**, not a proof VEIL can read
  it. Threading the field through `TryPlace` is exactly where behavior drifts
  silently.
- **Deed fairness.** Kills made before the VEIL slice ships do not count toward its
  deed — the counter starts when the node exists. Albion-visible-from-day-one is a
  VEIL panel question (show the locked node), not a reason to start a counter here.
- Push the equipped ids at spawn; a serialized field on the rig prefab is gotcha 1
  waiting to happen. Ghost's 0.70× is applied at the test, never written onto
  `noticeRange` / `aggroRadius`.
- No `GUILayout` in the Bastion HUD (gotcha 3) — the ability lines are bound TMPs on
  the locked host.
- `Rooted` must not stomp `MovementLocked` (pause, death, and `GameController`'s
  fighting gate all write that flag).
- M59 re-gates HUD card gaps at ≥24; two ability lines grow `Hud Vitals`, so that
  check is criterion 7's last step, not a later slice's problem.

## Open questions

- **Every number except Ghost's 0.70×** — Anchor's 8 m / 0.5× / 8 s / 30 s,
  Bulkhead's 6 s / 40 s: #12 starting values; first tune only after the
  kill-criteria Runs exist. *Unknown, needs play data.* Ghost 0.70× is the same
  tier, named so the implementer does not invent it; first tune in the same pass.

## Depends on / Unblocks

Depends on nothing unbuilt: M51's net verbs, M23's migration pattern and M42's
loadout lock are shipped; the slice's own new seams are the speed-recompute funnel
(step 7), `Rooted` (step 6), the player-damage hook (Bulkhead) and the equip-id push
(step 9), each sized in Implementation. Unblocks the VEIL slice (the second protocol,
the deed spine, the twist machinery), the FORGE slice (the turret and the
ammo-reserve decision), and gives `M65`'s co-op sessions protocol identity to read.
The brainstorm holds every specialist sheet and the roster path to 8–10.
