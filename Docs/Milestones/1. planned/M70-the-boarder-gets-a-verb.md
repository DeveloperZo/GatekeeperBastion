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
protocol reads as a deviation from it. The system design, the specialist sheets and
the deed unlock model live in `../../Brainstorms/boarder-protocols.md`.

**Recut 2026-08-15 (Zo): free-pick pools became protocols, and this slice shrank to
the Default.** The 2026-08-14 cut (three slots, free picks per pool, formerly titled
"The Boarder gets a verb") is superseded: identity moves from emergent reads of picks
to named protocols, per the brainstorm's overturn of this file's own "Sold
archetypes" Do-not-build row — the doctrine layer has no carrier in free-pick
grammar. AEGIS was cut from the slice by direction (it must not block the system; its
Steadfast clause waits on a damage-interrupts-repair pre-req). The **turret half
(`UTL-H`) moves out whole** to the FORGE slice: its criteria spent a "Primary
reserve" that `PlayerGun` does not have, so it waits on the ammo-reserve decision at
its own milestone. Ping, Cloak, Recall and Warden move to their protocols' slices
(VEIL, LEDGER, AEGIS).

## Story

As a Boarder, I embark wearing the Default Protocol — I can plant an Anchor to slow
the room for my crew, arm a Bulkhead to eat one hit I chose in advance, and pick the
rule that says how Gatekeepers read me — so that `G` and `H` are verbs my teammates
can name, and CREW QUARTERS equips identity a Run can feel rather than percentages it
cannot.

## Why it matters

The tracks had no job a weapon cannot do: session
`Logs/GatekeeperBastion_2026-08-08_17-00-15.log` spent **4,200** on the weapon and
**600** on Vitality, and a level's `+4.5%` sits under the measured `~20%` run-to-run
spread (`../../Design/2-Dynamics.md`) — no attribute purchase was ever perceptible.
Abilities change *what* the Boarder can do rather than *how much*, the only kind of
purchase that clears that floor.

Every row is a trade, not a grant (Zo, 2026-08-14): Anchor buys area control with
your mobility, Bulkhead buys one negated hit with timing paid in advance. No row
grants damage — the reload/clip/damage/fire-rate currencies belong to the M21 wheel.
A Passive is a rule, never a number; identity lives in the always-on layer (Deep
Rock's stated design; Vermintide careers read as their passives).

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
whichever comes first:

1. **Reflex, not decision** — every activation on either active slot fires within
   **2 s** of the cooldown ending (the activation line carries seconds-sat-ready).
   Stop and recut.
2. **The split collapsed** — one active slot carries fewer than a fifth of the
   other's activations (both counts are in the log, per slot). The slots answered the
   same moment; fold back to one.
3. **Dead first session** — a fresh save's first embark shows both abilities unused,
   or a first-Run session note says nothing about the protocol: the Default sheet is
   too thin. Widen it (brainstorm kill criterion 7).
4. **Bulkhead is a guess** — most Bulkhead windows expire un-triggered across the
   sample (the expiry line exists for this). The window is unreadable; recut the
   window length or the arming rule, once.
5. **Ghost** dies if the sample's median time-to-first-acquisition (log-derived)
   sits within the measured **~20%** run-to-run spread (`../../Design/2-Dynamics.md`)
   of the no-Ghost median — a bias under the noise floor; **Foresight** dies if no
   session note across the sample names a decision the preview changed (the
   per-preview log lines cross-check). A dead passive is replaced, not buffed.

## The Default sheet

Windows, cooldowns and radii are #12 starting values. They sit far above the
weapon-ability band (`WeaponAbility.Cooldown` 6–12 s) so `F`, `G` and `H` read as
different classes of thing. The sheet ships naked: twist slots and their pairs are
later slices per the brainstorm's paced-unlock grammar.

| Slot | Row | Rule | Trade | Window / Cooldown |
|---|---|---|---|---|
| Doctrine | **STOCK** | Hardware exactly as sold; deployed hardware carries the mode field, and STOCK writes baseline | The reference other doctrines recolor | always on |
| Crew `G` | **Anchor** | Plant in place; Gatekeepers within 8 m move at 0.5×; unplant to move | Your mobility for area control | up to 8 s / 30 s |
| Personal `H` | **Bulkhead** | Window: the first Gatekeeper damage that would land on you is negated; the window ends on that negation | One hit erased, bought in advance with timing | 6 s / 40 s |
| Passive (pick 1) | **Ghost** | Gatekeepers acquire you at reduced range — a bias on the existing nearest-target test | Never invisibility; a bias, not a veto | — |
| Passive (pick 1) | **Foresight** | Field terminals name the next Wave's composition before you pull — never what intensity added (the bet at the trigger stays blind) | Information, not power | — |

Kind refusals stand everywhere: a Ram never retargets, a Hunter never touches the
Hatch, and no row above overrides either.

## Acceptance criteria

1. **CREW QUARTERS shows the protocol.** One PROTOCOLS panel: the Default Protocol
   named, its doctrine text ("STOCK — hardware as sold"), both abilities with key
   labels, and the passive pick (two rows, one EQUIPPED); no attribute row remains.
   Purpose line stays "you" (M59 criterion 7).
2. **The Default is owned and equipped from the first session.** Fresh saves and
   migrated saves alike embark with it — no purchase, no deed, no empty slot state.
   Passive re-pick is free and Ship-only under the one `LoadoutLocked` gate (M42
   idiom); a mid-Run re-pick is a logged refusal.
3. **Anchor plants.** `G` locks the Boarder in place; Gatekeepers inside 8 m move at
   0.5× agent speed while inside; `G` again (or window end) unplants; cooldown starts
   at unplant.
4. **Bulkhead negates one hit.** `H` arms the window; the first Gatekeeper damage
   instance that would land on the caster is negated in the player-damage path and
   ends the window, one logged line naming the Gatekeeper; a window that expires
   un-triggered logs its expiry. Arming during cooldown is a logged refusal.
5. **The passive leaves a trace.** The embark log names protocol and passive. Ghost's
   bias applies server-side in targeting. Foresight logs one line per preview shown,
   naming the composition — and the preview never includes what intensity added.
6. **Cooldown reads back on both keys.** `G` or `H` during that slot's cooldown is
   one logged refusal naming seconds left; each activation line carries the slot and
   seconds-sat-ready (kill criteria 1 and 2 read these).
7. **HUD.** `Hud Vitals` gains two ability lines — Crew and Personal, each name plus
   READY / countdown / window state — and the passive's name beside them, extending
   the locked host (`../../Decisions.md`), no `GUILayout`. The shortcut panel shows
   `G crew` and `H self` from the `Controls` list with no panel edit.
8. **Doctrine plumbing is proven by changing nothing.** Placed hardware (decoy,
   repair rig deploys) carries a doctrine mode written at deploy from the placing
   player's equipped protocol; under STOCK, a test proves behavior is identical to
   today's — the seam ships, the content waits.
9. **Net.** Each verb applies once, server-side (M48 invariant): a client Anchor
   slows host bodies; Bulkhead's negation runs in the host's damage path; Ghost's
   bias lives in server-side targeting state set from the equipped ids at spawn;
   Foresight's preview is served from the host's `WaveDirector`. Requester gates on
   equipped-in-slot (M51 rig-gate idiom); cooldowns run client-side.
   `NetProtocolTests` proves single application.
10. **The tracks are gone.** `UpgradeTrack` holds only `HatchArmor`; the four
    formulas are constants (`PlayerMaxHp` 100, `StaminaMax` 100, `SalvagePerKill`
    25); crit chance is 0 with the roll mechanism left in `PlayerGun.Firing`; docgen
    drops the rows from `4-Data.md` with no generator edit (M33).
11. **Old saves refund.** Loading v7 or older refunds exactly `200 × (2^level − 1)`
    per track into banked (max case: 4 maxed tracks = **24,800**),
    `SalvageLedgerBalances` stays true, `SaveVersion` is **8**, the refund is one
    logged line. Level fields leave `MetaState`; `SaveData` keeps them parse-only.
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
   `WeaponAbility` idiom; the verbs and step 9's server clamps read only these, so a
   later twist inherits them untouched. `BoarderAbilityState` for the two active
   slots (`WindowRemaining`, `CooldownRemaining`, `Tick(dt)`, `TryActivate()`,
   `EndWindowEarly()`) — pure, dumb on purpose (trigger policy stays with the
   caller; later twists change who calls, not the struct), mirroring `WeaponAbility`
   + `PinBuildState`, harness-testable. Passives carry no state struct.
2. **Change** `Sim/Meta/MetaState.cs` — `EquippedProtocol` (defaulting to `Default`)
   and `EquippedPassive` (defaulting to Ghost; re-pick Ship-only). Rename
   `PayloadLoadoutLocked` to `LoadoutLocked` and gate the passive pick on it (one
   lock, one mechanism — M42). Delete `VitalityLevel`/`StaminaLevel`/
   `ScavengingLevel`/`LuckLevel`; drop their lines from
   `ReconstructSpentFromLevels`; in `FromSaveData`, v<8 refunds
   `Σ 200×(2^level −1)` — `BankedSalvage +=`, `LifetimeSalvageSpent −=`, so the
   ledger identity holds unchanged.
3. **Change** `Sim/Meta/SaveData.cs` — `CurrentVersion` 8; protocol + passive fields;
   the four level fields move to the legacy parse-only region beside
   `RepairRigLevel`.
4. **Change** `Sim/Meta/UpgradeTrack.cs` / `MetaUpgrades.cs` — enum keeps
   `HatchArmor` only; the four formulas become the constants in criterion 10;
   `CritChance` deleted and `PlayerGun.CritChance` returns 0 (the 2× roll in
   `PlayerGun.Firing` stays — Luck-shaped crit may return, and no Boarder ability may
   ever be a crit source). `RunSim.BaseKillValue` and `GameController.ApplyMetaEffects`
   read the constants.
5. **Change** `Game/Input/Controls.cs` — `ControlId.CrewAbility`, `KeyCode.G`,
   display `G`, label `crew`; `ControlId.PersonalAbility`, `KeyCode.H`, display `H`,
   label `self`; both section FIGHT, `FightAlways`. `F` is the gun's ability, `G` is
   the crew's, `H` is yours.
6. **Create** `Game/Player/PlayerProtocolAbilities.cs` — LOCAL-OWNER, the
   `PlayerAbility` pattern: reads the equipped ids pushed at `ApplyMetaEffects`
   (push-at-spawn, `../../Architecture.md` gotcha 1 — never serialize them), polls
   both control ids, `SetHosted` tick, exposes name/window/cooldown per slot and the
   passive name for the HUD, logs `Log.Event("Protocol", ...)` with slot and
   seconds-sat-ready on every activation and refusal.
7. **Create** the verbs, game-side:
   - Anchor: `Game/Bastion/AnchorField.cs` — server-side radius test each tick. No
     slow API exists: `Provoke()` writes `_agent.speed` directly today, which would
     silently shed a slow mid-ring. Add a slow multiplier applied in **one
     speed-recompute funnel** on `EnemyGatekeeper` that `Configure`, `Provoke` and
     ring enter/leave all route through (Suppressor and Ward Stone later land in the
     same funnel); FPC locked while planted.
   - Bulkhead: an armed-window flag consumed in the player-damage path — the first
     Gatekeeper damage instance to the holder zeroes and clears the flag, logging
     the source.
   - Ghost: a per-player bias on the **acquisition tests only** —
     `CanSeePlayer()`'s notice range and the aggro-radius check in `Retarget()` —
     never inside `PlayerRegistry.NearestAlive`'s selection: "who" is the seam the
     future windowed-override family owns; Ghost is "from how far". No new counter.
   - Foresight: the field-terminal panel asks `WaveDirector` for the next
     composition and shows it pre-intensity; display only.
8. **Create** the doctrine seam — the `DoctrineMode` is written into **Sim state at
   placement** (`DecoyState` gains the field, set as a `TryPlace` argument from the
   placing player's equipped protocol — per-place, per-placer, which covers a client
   placing on the host bay), with `DecoyObject.Configure` mirroring it for display;
   rig deploys the same way. Sim-side because the clauses that will read it
   (`ShouldPull`, `MaxLive`) live there, and a MonoBehaviour field is invisible to
   the mcs harness the STOCK test runs in. Never read off a class enum at use time
   (the brainstorm's Destiny 2 lesson). STOCK is the only value this slice; an
   EditMode test pins STOCK-equals-baseline.
9. **Change** `Game/Net/NetVerbs.cs` —
   `RequestAnchorServerRpc(Vector3 position, bool planting)`,
   `RequestBulkheadServerRpc()`; server applies exactly as the solo path; magnitudes
   clamped to catalog values like the pin/concussive RPCs. Passives need no per-use
   RPC. What they do need is new: **no shipped mechanism carries a client's equip
   state to the server** (`NetPlayerAvatar` replicates Hp and WaitingOnShip only;
   M51's rig gate is requester-side). Add one avatar-spawn push carrying the
   equipped **protocol and passive ids** — the server derives Ghost's bias and
   doctrine-at-deploy from the catalog. Ids, never derived flags: every later
   protocol, doctrine deploy and elite read rides this one push.
10. **Change** `Game/Ship/CrewQuartersStation.cs` + `Game/Ui/CrewQuartersPanelView.cs`
    — the PROTOCOLS panel of criterion 1 replaces the attribute rows, and it
    **enumerates catalog rows** (the ARMORY `ShopWeapons()` precedent) — one row
    today, never a hardcoded Default, so VEIL is a catalog row and a deed line, not
    a panel edit; `ContentHash` from protocol + passive; `PromptHint` "your
    protocol"; the authored ATTRIBUTES heading in `Ship.unity` becomes PROTOCOL.
    `SprintSeconds` dies with the rows.
11. **Change** `Game/Ship/ShipCommands.cs` — `PickPassive(id)` on the payload idiom
    (refusals logged, `track=crew` token); the four cases leave `BuyUpgrade`
    (`HatchArmor` stays).
12. **Change** `HudController.Fight.cs` / `Game/Ui/BastionHudView.cs` — two ability
    lines plus the passive name under the `Hud Vitals` rows, the weapon-detail
    string idiom from `HudController.Weapon.cs`.
13. **Create** tests — EditMode `Assets/Tests/EditMode/Meta/BoarderProtocolTests.cs`
    (catalog pins, state machine, passive pick + lock, refund arithmetic, ledger
    balance, removed-track APIs gone — the M23 `RemovedTracksAreGone` shape;
    STOCK-equals-baseline); PlayMode `Assets/Tests/PlayMode/NetProtocolTests.cs`
    (M48 fixture; criterion 9).
14. **Change** docs — `language.md`: **Boarder Protocol**, **doctrine**, **Crew
    ability** (the `G` slot), **Personal ability**, **Passive**, **twist** (reserved,
    ships later), working-name rows (DEFAULT, STOCK, Anchor, Bulkhead, Ghost,
    Foresight) marked renameable; four track rows leave the tracks table; stations
    table CREW line; `../../Decisions.md`: Locked ship-terminals CREW line narrows to
    FABRICATION, dated, and the "Sold archetypes" overturn is recorded there, dated,
    citing the brainstorm; `../../Brainstorms/fabrication.md` gains the doctrine
    note (deployed hardware carries a mode field written at deploy);
    `Play/The-Ship.md` CREW section recut in the Play voice; `4-Data.md` regenerates
    via `Tools/docgen/run.sh`. Roadmap row and Changelog per lifecycle.

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
attribute, so the refund lets the player re-decide.

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
3. Criterion 8 — the STOCK-equals-baseline EditMode test, plus one played Run
   deploying a decoy and confirming behavior unchanged.
4. Playthrough path — fresh save → Ship: PROTOCOLS panel shows the Default equipped,
   pick Foresight over Ghost → embark → start a Wave → `G` plants Anchor and the
   room visibly slows inside the ring; `H` arms Bulkhead and the next hit that lands
   is negated with its log line; both keys during cooldown log refusals naming
   seconds left. Terminal shows next-Wave composition, nothing extra after juicing.
   Close log carries one `Protocol` line per activation with slot and
   seconds-sat-ready, and no `[Dev]`.
5. Criterion 2's migration half — load a v7 save: refund line, Default equipped,
   embark works with no purchase.

## Do not build

| Not building | Why not |
|---|---|
| A second protocol, or the deed spine | VEIL's slice; the spine ships with its first customer |
| Twist slots or any twist pair | Later slices per the paced-unlock grammar |
| The `UTL-H` turret | FORGE slice, behind the ammo-reserve decision |
| Doctrine clauses that change behavior | STOCK proves the seam by changing nothing; clauses arrive with their protocols |
| Overclock, or any damage-granting row | The wheel owns dps; named kill condition |
| A numeric passive | Under the ~20% floor by construction |
| A heal or revive row | No heal path; M57 owns the down rule |
| A crit source anywhere | Luck stays the only crit source, even at level 0 |
| Teammate displacement, permanent mobility | Cut and locked; Anchor roots only the caster |

## Watch

- **This slice's whole bet is that plain is playable.** The Default has no doctrine
  clause and no twists — kill criterion 3 (dead first session) is the instrument. If
  the Default reads as an empty tutorial rather than a plain identity, the fix is
  widening its sheet, not rushing VEIL.
- **STOCK-equals-baseline is the slice's regression net.** The doctrine field
  threading through deploy paths is exactly where behavior drifts silently.
- **Bulkhead's chip-hit read.** The base ability spends the window on whatever lands
  first, including a graze. That is the trade as designed (the Deadbolt twist later
  makes it automatic); if session notes call the base a waste, kill criterion 4
  answers with the expiry data, once.
- **Deed fairness.** Kills made before the VEIL slice ships do not count toward its
  deed — Albion counts from when the node exists. Noted here so the VEIL slice says
  it on purpose rather than discovering it.
- Push the equipped ids at spawn; a serialized field on the rig prefab is gotcha 1
  waiting to happen.
- No `GUILayout` in the Bastion HUD (gotcha 3) — the ability lines are bound TMPs on
  the locked host.
- M59 re-gates HUD card gaps at ≥24; two ability lines grow `Hud Vitals`, so re-run
  that check in whichever slice lands second.

## Open questions

- **Every number** — Anchor's 8 m / 0.5× / 8 s / 30 s, Bulkhead's 6 s / 40 s,
  Ghost's acquisition bias: #12 starting values; first tune only after the
  kill-criteria Runs exist. *Unknown, needs play data.*
- **The initial passive** — written as Ghost equipped by default (a fresh save must
  embark whole); if the first-session read says the pick should be forced instead of
  defaulted, that is a panel change, not a grammar change.

## Depends on / Unblocks

Depends on nothing unbuilt: M51's net verbs, M23's migration pattern and M42's
loadout lock are shipped; the slice's own new seams are the speed-recompute funnel
(step 7), the player-damage hook (Bulkhead) and the equip-id push (step 9), each
sized in Implementation. Unblocks the VEIL slice (the second protocol,
the deed spine, the twist machinery), the FORGE slice (the turret and the
ammo-reserve decision), and gives `M65`'s co-op sessions protocol identity to read.
The brainstorm holds every specialist sheet and the roster path to 8–10.
