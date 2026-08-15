# M70 — The Boarder gets a verb

<!--
Status: later
Update trigger: when a roster row is added, renamed or cut, when a window/cooldown moves off
its starting value, when the Cloaker slice ships and the stealth read changes, or when the
turret's siphon rate, life, onboard magazine or damage model changes.
Last updated: 2026-08-14
-->

CREW QUARTERS stops selling the four attribute tracks and sells **Crew abilities**: a
Boarder-owned burst — press, a short window, a long wait — one equipped per Boarder, on `G`.
This file is the system (equip, key, window, cooldown, HUD, net), the starter roster of
four, and the `UTL-H` turret payload — folded from M56 because it is an ability
(Zo, 2026-08-14); it keeps its FABRICATION shape and does not move to `G`. Graduated from
`../../Brainstorms/crew-skills-and-perks.md` (Zo, 2026-08-12 / 2026-08-14) and, for the
turret, `../../Brainstorms/fabrication.md`.

## Story

As a Boarder, I want one bought ability that changes what I can do for a few seconds, so
that I differ from my teammates by more than gun and loadout, and so CREW QUARTERS sells
something a Run can feel.

## Why it matters

The tracks had no job a weapon cannot do: session
`Logs/GatekeeperBastion_2026-08-08_17-00-15.log` spent **4,200** on the weapon and **600**
on Vitality, and a level's `+4.5%` sits under the measured `~20%` run-to-run spread
(`../../Design/2-Dynamics.md`) — no attribute purchase was ever perceptible. Abilities
change *what* the Boarder can do rather than *how much*, which is the only kind of purchase
that clears that floor without a percentage.

Every row is a trade, not a grant (Zo, 2026-08-14; the turret below is the same filter run
on the payload shelf: it fires with rounds siphoned from the player's own reserve). No row
grants damage — the reload/clip/damage/fire-rate currencies belong to the M21 wheel, and a
+dps window is a worse Tier.

The turret's claim was never dps, and the measurement says dps would not have worked:
raising player dps lifted kill fraction 58 to 83% **and** arrivals 15 to 22, in
near-cancellation (the Wave-2 triple, `../../Design/2-Dynamics.md`). The siphon holds
total rounds per Run flat — it moves where and when that ammo is fired, it does not add
ammo — so whether a second firing position moves the outcome becomes answerable. It is
also the discouragement (Zo, 2026-08-13): the Hatch is invulnerable outside a Hatch-bank
Wave (`M53`), so a parked turret was never defending anything, and the clock siphon makes
parking it cost the exploring you left to do.

Leverage tier (`../../Working-Rules.md`): **#5, rules** — what the station sells and what a
Boarder can do changes — plus **#10, structure** for the turret's second firing position;
the windows, cooldowns, prices and the turret's three constants below are **#12 starting
values** to be tuned only after logged Runs. (M56's file named the siphon "#9, rules";
the ladder puts rules at #5 — corrected here.)

## Kill criteria

This is a bet. Evaluate at **10** logged Runs with an ability equipped, or by
**2026-09-30**, whichever comes first:

1. **Reflex, not decision** — every activation fires within **2 s** of the cooldown ending
   (the activation log line carries seconds-sat-ready). Stop and recut.
2. **One row is the only pick** — one ability accounts for more than **8 of 10** embarks
   (the embark log names the equipped ability). Recut the roster.
3. Every-row-is-silent-+dps is prevented by construction: no row below grants damage.

Stealth-specific, evaluated over the first **10** logged Runs after a layer-native
Gatekeeper ships (the Cloaker, [`M63`](M63-the-garrison-a-roster-with-bodies.md) roster): players
cloak on cooldown regardless of
situation, or die in the layer more often than they escape with it. Either one means it is
a reflex or a trap rather than a choice (Zo, 2026-08-14).

Turret-specific, over ten logged Runs after close: **placed and left standing near the
Hatch on most of them, the siphon rate is too low to do its job; never placed at all, the
exchange rate is wrong.** Adjust the two numbers once. If a second ten Runs reads the
same, cut `UTL-H` from the payload roster rather than tuning it a third time.

## The roster

One canonical table. Prices are flat so the pick is by identity, not budget; **400** is
7.6% of the one measured full clear (gross **5,250**, `../../Design/2-Dynamics.md` §2.6).
Windows and cooldowns are #12 starting values; they sit far above the weapon-ability band
(`WeaponAbility.Cooldown` 6–12 s) so `F` and `G` read as different classes of thing.

| Row | Trades | Window | Cooldown | Price | Refuses |
|---|---|---|---|---|---|
| **Cloak** | The world's identity for your visibility: you enter the shared layer, actors outside render as identity-blind orbs, nothing outside the layer sees you | 6 s | 45 s | 400 | Damage; a wind-up exit (exit is instant); shifting a teammate — ever |
| **Anchor** | Your mobility for area control: plant in place, Gatekeepers within 8 m move at 0.5×; you cannot move until you unplant | up to 8 s | 30 s | 400 | Damage; moving while planted |
| **Recall** | Your unbanked pile for escape: instant return to the Hatch; the whole pile drops where you stood as a world pile any Boarder can reclaim by touch | instant | 60 s | 400 | Carrying the salvage with you; activating with 0 unbanked (refused — a free teleport is the cut mobility skill) |
| **Ping** | Your concealment for information: every live Gatekeeper renders through walls as orbs for you, and every Default and Hunter targets you for the window | 5 s | 30 s | 400 | Identity detail (orbs stay identity-blind); moving a Ram — its refusal stands (`M63` criterion 2) |

Names are working names on the weapon-codename rule (`../../Decisions.md`): renameable when
play earns a fiction noun, recorded in `language.md` with that mark.

Cloak rules settled in the brainstorm and carried whole: everyone in the layer sees everyone
else in it; collision, interaction and shooting still work; attacking drops you out; exit is
instant; orbs are positionally honest and identity-blind; the Hatch stays readable among the
orbs. Orbs are **actors only** (Gatekeepers, Boarders outside the layer) — geometry renders
normally, because positional honesty needs the room to still be a room. Whoever chose the
shift is the one it favours; dragging an enemy in is an upgrade candidate, not this slice.
A cloaked teammate is an orb to an uncloaked one — seen strangely, never moved (Warframe
Limbo is the shipped failure of moving teammates; Mordekaiser's Realm of Death is the
shipped success of pulling enemies — conclusions on file in the brainstorm's record).

The turret is not a row on this table and does not move to `G`: a placed drone item
belongs on the payload shelf — the constraint that shaped this roster — so `UTL-H` ships
in this slice on FABRICATION, under its own criteria below.

## Acceptance criteria

1. **CREW QUARTERS sells the roster.** Four rows, price and state (BUY / EQUIP / EQUIPPED)
   from the catalog; no attribute row remains; every refusal (short, mid-Run equip) is a
   logged `Ship` line. Purpose line stays "you" (M59 criterion 7).
2. **One equipped per Boarder.** Buying equips (payload idiom); equip is refused mid-Run
   with a logged reason; the embark log names the equipped ability.
3. **The player action.** With Cloak equipped, pressing `G` during Assault enters the
   layer: live Gatekeepers render as orbs, none targets the Boarder, firing exits the layer
   the same frame, and the window then cooldown read back on the HUD. Provoked Defaults
   resume the Hatch march while their target is layered and resume the chase on exit — the
   provoke flag itself never clears (provoke is permanent for that Gatekeeper's life).
4. **Anchor plants.** `G` locks the Boarder in place; Gatekeepers inside 8 m move at 0.5×
   agent speed while inside; `G` again (or window end) unplants; cooldown starts at unplant.
5. **Recall trades the pile.** `G` teleports the Boarder to the Hatch stand point, unbanked
   goes to 0, a pile holding the exact amount spawns where they stood, touch restores it to
   the toucher's unbanked, and the pile despawns at Run end. `G` with 0 unbanked is a
   logged refusal. The log quotes the amount dropped and reclaimed.
6. **Ping lights both sides up.** For the window, every live Default and Hunter targets the
   pinger and every live Gatekeeper renders through walls for the pinger; Rams never
   retarget; prior targeting resumes at window end.
7. **Cooldown reads back.** `G` during cooldown is one logged refusal naming seconds left;
   each activation line carries seconds-sat-ready (the kill-criterion instrument); `G` with
   nothing equipped logs "no Crew ability equipped".
8. **HUD.** `Hud Vitals` gains one crew line — equipped name plus READY / countdown /
   window state — extending the locked host (`../../Decisions.md`), no `GUILayout`. The
   shortcut panel shows `G crew` from the `Controls` list with no panel edit.
9. **Net.** Each verb applies once, server-side (M48 invariant): a client Cloak removes it
   from server targeting, Anchor slows host bodies, Recall mutates the host ledger and
   spawns one pile, Ping retargets host enemies. Requester gates on equipped (M51 rig-gate
   idiom); cooldowns run client-side. `NetCrewAbilityTests` proves single application.
10. **The tracks are gone.** `UpgradeTrack` holds only `HatchArmor`; the four formulas are
    constants (`PlayerMaxHp` 100, `StaminaMax` 100, `SalvagePerKill` 25); crit chance is 0
    with the roll mechanism left in `PlayerGun.Firing`; docgen drops the rows from
    `4-Data.md` with no generator edit (M33).
11. **Old saves refund.** Loading v7 or older refunds exactly `200 × (2^level − 1)` per
    track into banked (max case: 4 maxed tracks = **24,800**), `SalvageLedgerBalances`
    stays true, `SaveVersion` is **8**, the refund is one logged line. Level fields leave
    `MetaState`; `SaveData` keeps them parse-only.
12. **Solo unchanged otherwise.** Both suites green plus the new tests; language lint
    passes; no `[Dev]` in any close log. **No new counter, no accumulator, no stats
    object** (`../../Decisions.md`); **no new menu row without asking**
    (`../../Architecture/README.md`).
13. **`UTL-H` is a drone-bay payload**, bought flat on the FABRICATION shelf, one payload
    per Run, own drone and own choice per player in co-op
    (`../../Brainstorms/fabrication.md`).
14. **Placement is ghost-and-confirm**, the pattern `M30` shipped. No new placement code
    path; `DecoyPlacement`'s ghost is the one mechanism.
15. **`TurretOnboardRounds = 30`.** It fires these first, before the siphon takes anything.
16. **The siphon runs on the clock, not on the trigger.** While deployed it draws
    `TurretSiphonPerSecond = 2` from the player's Primary reserve **whether or not it is
    firing**. A per-shot siphon makes an idle turret free and inverts the mechanic it
    exists to create.
17. **It ends at `TurretLifeSeconds = 45`, or when it needs a round and both the onboard
    magazine and the player's reserve are empty, whichever comes first.** The siphon may
    take the player to zero reserve; that is the cost being real. Logged on death with
    rounds fired and rounds siphoned.
18. **It does not inherit weapon upgrades.** Fixed damage and fire rate on the payload, not
    read from `WeaponCatalog` or the M21 spokes. A turret that scales with the build is a
    dps knob wearing a placement ghost, and `fabrication.md` already decided payload
    upgrades wait until a payload has been played.
19. **Targeting is nearest alive in range with line of sight**, through the existing
    `NearestAlive`. Gatekeeper refusals are unchanged: the turret is not a Hatch and not a
    Boarder, and no kind's refusal is overridden by its presence.
20. **The HUD shows the cost while it happens.** The Primary reserve readout drains visibly
    during the siphon, and the turret's remaining life is readable when looked at. The
    player should never be surprised by an empty gun.
21. **Turret constants land in `../../Design/4-Data.md` generated, not hand-written**
    (`M33`), in the same change.
22. **Turret sim ownership is per player and placement is server-authoritative** (the M51
    idiom, shipped). Kills it scores credit the placing player's Unbanked pile (`M53`).
23. **A session log line carries rounds fired and rounds siphoned per Run**, so
    `Tools/wave-report` can read the turret kill criterion off the log rather than off
    memory.

## Implementation

Order: sim types and migration, then the solo verbs, then net, then station and HUD, then docs.

1. **Create** `Assets/Scripts/Sim/Meta/CrewAbility.cs` — `CrewAbilityId`
   (`None=0, Cloak=1, Anchor=2, Recall=3, Ping=4`; save-adjacent, never renumber or reuse);
   `CrewAbilityCatalog` (`Def`: id, displayName, cost, windowSeconds, cooldownSeconds;
   `OwnershipBit`); `CrewAbilityState` struct (`WindowRemaining`, `CooldownRemaining`,
   `Tick(dt)`, `TryActivate()`, `EndWindowEarly()`) — pure, mirroring `WeaponAbility` +
   `PinBuildState`, harness-testable.
2. **Change** `Sim/Meta/MetaState.cs` — add `OwnedCrewAbilityMask` / `EquippedCrewAbility`
   with owns/set/equip methods on the payload idiom; rename `PayloadLoadoutLocked` to
   `LoadoutLocked` and gate crew equip on it too (one lock, one mechanism — M42). Delete
   `VitalityLevel`/`StaminaLevel`/`ScavengingLevel`/`LuckLevel`; drop their four lines from
   `ReconstructSpentFromLevels`; in `FromSaveData`, v<8 refunds
   `Σ 200×(2^level −1)` — `BankedSalvage +=`, `LifetimeSalvageSpent −=`, so the ledger
   identity holds unchanged.
3. **Change** `Sim/Meta/SaveData.cs` — `CurrentVersion` 8; mask + equipped fields; the four
   level fields move to the legacy parse-only region beside `RepairRigLevel`.
4. **Change** `Sim/Meta/UpgradeTrack.cs` / `MetaUpgrades.cs` — enum keeps `HatchArmor`
   only; the four formulas become the constants in criterion 10; `CritChance` deleted and
   `PlayerGun.CritChance` returns 0 (the 2× roll in `PlayerGun.Firing` stays — Luck-shaped
   crit may return, and no Crew ability may ever be a crit source). `RunSim.BaseKillValue`
   and `GameController.ApplyMetaEffects` read the constants.
5. **Change** `Game/Input/Controls.cs` — `ControlId.CrewAbility`, `KeyCode.G`, display
   `G`, label `crew`, section FIGHT, `FightAlways`. `G` chosen because it sits beside `F`:
   `F` is the gun's ability, `G` is the Boarder's.
6. **Create** `Game/Player/PlayerCrewAbility.cs` — LOCAL-OWNER, the `PlayerAbility`
   pattern: reads `EquippedCrewAbility` pushed at `ApplyMetaEffects` (push-at-spawn,
   `../../Architecture.md` gotcha 1 — never serialize it), polls
   `Controls.Down(ControlId.CrewAbility)`, dispatches by id, `SetHosted` tick, exposes
   name/window/cooldown for the HUD, logs `Log.Event("CrewAbility", ...)` with
   seconds-sat-ready on every activation.
7. **Create** the four verbs, game-side (rules sim-side are only the state struct —
   targeting and rendering are Game-layer by the seam):
   - Cloak: layer flag on `PlayerRegistry`; `EnemyGatekeeper.Targeting` / `NearestAlive`
     skip layered players; per-Gatekeeper orb proxy toggled for a layered local viewer;
     fire-that-fires exits.
   - Anchor: `Game/Bastion/AnchorField.cs` — server-side radius test each tick,
     `EnemyGatekeeper.SetSlowMultiplier(0.5f)` on enter / clear on leave; FPC locked while
     planted.
   - Recall: `GameController.RecallToHatch(player)` command (all sim mutation through
     `GameController`) — zeroes that player's unbanked via a `RunSim` mutator, spawns
     `Game/Bastion/SalvagePile.cs` (amount, touch-to-reclaim, despawn at Run end),
     teleports to `HatchStandPoint`.
   - Ping: window overlay in `EnemyGatekeeper.Targeting` preferring the pinger — a
     separate overlay, never the provoke flag; Ram excluded by its refusal; through-wall
     orb render for the pinger reuses the Cloak orb proxy.
8. **Change** `Game/Net/NetVerbs.cs` — `RequestCloakServerRpc(bool entering)`,
   `RequestAnchorServerRpc(Vector3 position, bool planting)`, `RequestRecallServerRpc()`,
   `RequestPingServerRpc()`; server applies on its registry/ledger exactly as the solo
   path; magnitudes clamped to catalog values like the pin/concussive RPCs.
9. **Change** `Game/Ship/CrewQuartersStation.cs` + `Game/Ui/CrewQuartersPanelView.cs` —
   four ability rows (BUY → EQUIP → EQUIPPED) replace the attribute rows; `ContentHash`
   from mask + equipped; `PromptHint` "equip a Crew ability"; the authored ATTRIBUTES
   heading in `Ship.unity` becomes CREW ABILITIES. `SprintSeconds` dies with the rows.
10. **Change** `Game/Ship/ShipCommands.cs` — `BuyCrewAbility(id)` / `EquipCrewAbility(id)`
    on the payload idiom (buy equips, refusals logged, `track=crew` token); the four cases
    leave `BuyUpgrade` (`HatchArmor` stays).
11. **Change** `HudController.Fight.cs` / `Game/Ui/BastionHudView.cs` — crew line under the
    `Hud Vitals` rows, the weapon-detail string idiom from `HudController.Weapon.cs`.
12. **Create** tests — EditMode `Assets/Tests/EditMode/Meta/CrewAbilityTests.cs` (catalog
    pins, state machine, buy/equip/lock, refund arithmetic, ledger balance, removed-track
    APIs gone — the M23 `RemovedTracksAreGone` shape); PlayMode
    `Assets/Tests/PlayMode/NetCrewAbilityTests.cs` (M48 fixture; criterion 9).
13. **Create** the `UTL-H` payload — the shelf row, the placed object, and its siphon
    tick. The siphon lives in `Sim/`, driven from the Run tick, not a MonoBehaviour
    `Update` (`M45`). **Reuse** `DecoyPlacement`'s ghost-and-confirm and `DecoyObject`'s
    placed-object lifetime — one payload added, not a second placement system. Placement
    RPC on step 8's `NetVerbs` idiom; turret kills credit the placer's Unbanked.
14. **Change** docs — `language.md`: Crew ability row drops *Designed, not built*, four
    track rows leave the tracks table, stations table CREW line, four working-name rows
    marked renameable; `../../Decisions.md`: Locked ship-terminals CREW line (was "four
    tracks") and the +5%/level retune note narrow to FABRICATION, dated; `Play/The-Ship.md`
    CREW section recut in the Play voice; `4-Data.md` regenerates via
    `Tools/docgen/run.sh`, carrying the three turret constants. Roadmap row and Changelog
    per lifecycle.

Rejected, by name: **Overclock** — its grant is +damage in a window, the wheel's currency
and the roster's named kill condition, whatever the cost side does. **Several equipped
with slots** — a second key and co-op stacking before any play data; one-equipped is the
drone-payload grain (adopted from the brainstorm's Open recommendation — Zo can veto).
**Grant instead of refund** — M23 granted because the rig directly succeeded the deleted
tracks; no ability succeeds an attribute, so the refund lets the player re-decide.
**Freezing the levels** — two permanent classes of save for a shop that no longer sells
the thing. **Gating the roster on first loss** — attributes were deliberately ungated and
this is the station's identity purchase, same as buying a weapon. **XP as the crew
currency** — cut, stays cut (`../../Decisions.md` via the brainstorm). **Per-ability
upgrades this slice** — upgrades change what a row does, not how much (noise floor);
that is a later slice per row. **A stealth weapon ability** — the layer shifts the
Boarder, not the gun, and every weapon already spends its `F`.

Turret rejections carried from M56: **a turret with its own independent supply** — that is
free cover; the siphon is the whole point. **Per-shot siphon** — criterion 16. **Auto-repair
or any Hatch-defence framing** — the Hatch is invulnerable outside a Hatch-bank Wave; the
Deployed Repair Rig is already parked in `fabrication.md` for this reason. **Payload upgrade
tracks** — `fabrication.md`; the roster has not been played. **Moving the turret to CREW
QUARTERS or onto `G`** — a placed drone item belongs on the payload shelf; folding the
milestones does not fold the consoles.

On the cut list: **mobility.** Recall is built anyway because the verdict's premise
expired — the cut reads "not a mechanic in a 30 × 52 m bastion" and the M61-2 rescale made
the station 15 rooms up to **152 × 76 m** — and because Recall is a priced burst that
refuses to fire for free, not a permanent movement stat. Zo can veto the row.

## Supporting work

- **Driven by** `G` (new `Controls` row) and the CREW QUARTERS panel — here.
- **Shown by** the shortcut panel row (automatic from `Controls`), the station roster rows,
  and the `Hud Vitals` crew line — here.
- **Reads back as** the HUD countdown, the orb render, the pile object, and one
  `CrewAbility` log line per activation and refusal — here.

## Verification

1. Criteria 1–2, 10–11 — `Tools/verify/run.sh --filter CrewAbility` plus the migration
   tests: seed a v7 save with known levels, assert banked rises by the exact refund and
   `SalvageLedgerBalances`. Docgen check: `Tools/docgen/run.sh --check` exits 0 only after
   regeneration.
2. Criterion 9 — `gkb_verify_request.txt`; `NetCrewAbilityTests` Passed in
   `gkb_verify_report.json`.
3. Playthrough path — from Play: Ship → CREW QUARTERS → buy Cloak (400) → embark → start a
   Wave → press `G` mid-Assault: orbs, no targeting, fire to exit, watch the HUD count
   down; `G` again during cooldown for the refusal line. Return, equip each other row, one
   Wave each: Anchor slow visibly opens a gap; Recall drops and reclaims a quoted pile;
   Ping turns the room toward you and shows it through walls. Close log carries one
   `CrewAbility` line per activation, seconds-sat-ready on each, and no `[Dev]`.
4. Criterion 3's negative half — fresh save, no purchase, `G` logs "no Crew ability
   equipped"; the panel still opens and sells.
5. Criteria 15–17 — deploy the turret with a known reserve, stand still, and watch the
   reserve fall on the clock with the turret not firing; then deploy with a near-empty
   reserve and confirm it dies early.
6. Criterion 18 — a maxed Primary and a base Primary produce the same turret damage.
7. Criteria 19, 22 — an EditMode test for targeting and ownership; a loopback PlayMode
   test on the M48 fixture.
8. Criterion 23 — one played Run, then read rounds siphoned out of the session log.
9. Turret kill criterion — ten logged Runs, reported together, not one at a time.

## Do not build

| Not building | Why not |
|---|---|
| Overclock, or any damage-granting row | The wheel owns dps; named kill condition |
| Ability upgrade tiers | Later slice per row; upgrades change what, not how much |
| The Cloaker, or any layer-native Gatekeeper | `M63` roster; each kind is its own slice |
| Dragging an enemy into the layer | Upgrade candidate on record, not this slice |
| Shifting or teleporting a teammate | Limbo; enemies yes, allies never |
| A second equipped slot | Open question answered one-equipped; revisit needs play data |
| A crit source anywhere in the roster | Luck stays the only crit source, even at level 0 |
| Turret upgrades, or damage read from the weapon build | A scaling turret is a dps knob wearing a placement ghost (`fabrication.md`) |

## Watch

- This is deliberately one milestone carrying a system, its content, and the turret
  payload — larger than the contract's one-grain rule — by Zo's explicit call; noted once
  here, not re-litigated.
- **The near-cancellation is the turret's likeliest outcome, not a worst case.** If kill
  fraction rises and arrivals rise with it, the turret changed the numbers and not the
  fight, exactly as the Wave-2 triple did. A finding to record; the turret kill criterion
  exists for it.
- **Criterion 16 is the one that will be argued down**, because a clock siphon feels
  punishing in the first ten minutes of play. It is punishing on purpose. Softening it to
  per-shot is the same as deleting the payload.
- **The turret's zero-reserve case needs watching rather than pre-solving.** Being left
  dry by your own turret may read as a good hard lesson or as a bug. One played Run
  answers it; a reserve floor is the fix if it reads as a bug, and it is a constant, not
  a re-cut.
- Until the Cloaker ships, Cloak has no layer-native threat and reads as escape-only; the
  stealth graduation Run (cloak with a native stealther present and without, reading as
  different decisions) can only happen after that slice, and the stealth kill criteria are
  dated from it.
- Push the equipped id at spawn; a serialized ability field on the rig prefab is gotcha 1
  waiting to happen.
- No `GUILayout` in the Bastion HUD (gotcha 3) — the crew line is a bound TMP on the
  locked host.
- Ping's overlay must stay separate from the provoke flag, or the window permanently
  provokes the room.
- M59 re-gates HUD card gaps at ≥24; if the crew line grows `Hud Vitals`, re-run that
  check in whichever slice lands second.

## Open questions

- **How much of a Wave is layer-side** — sets how often the Cloak interaction happens, not
  whether the rule works. Owner: the Cloaker/Wave-composition slice out of
  `../../Brainstorms/gatekeepers-and-waves.md` (`M63` roster). *Unknown, needs that slice.*
- **Every number in the roster table and on the turret** — windows, cooldowns, prices, the
  8 m radius, the 0.5× slow, and the turret's onboard 30 / siphon 2 per s / life 45 s.
  Starting values at tier #12; first tune only after the kill-criteria Runs exist.
  *Unknown, needs play data.*

## Depends on / Unblocks

Depends on nothing unbuilt: M51's net verbs, M53's unbanked pile, M23's migration pattern,
and M30's drone bay, charge model and placement ghost are all shipped. The stealth read
deepens when the Cloaker ships (`M63` roster and its per-kind slices); Cloak does not wait
for it. The turret was queued after `M55` and that ordering survives the fold — it is the
natural first slice of this file. Unblocks per-row upgrade slices, the `fabrication.md`
question of whether a second firing position is worth a payload slot (that roster's
largest unknown), and gives `M65`'s three-player fight a second axis of difference between
Boarders.
