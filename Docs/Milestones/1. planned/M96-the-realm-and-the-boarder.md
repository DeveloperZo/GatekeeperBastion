# M96 — The realm and the Boarder

<!--
Status: planned
Update trigger: when an item moves between the character side and the realm side, when
the save version changes, when the realm list gains a feature
Last updated: 2026-08-21
-->

One save becomes two. A **Boarder** is yours and travels: weapons, focus-ring upgrades,
protocols, equipment. A **realm** is a Ship and the bastion progress attached to it, and
it stays with whoever owns it. Playing in someone else's realm means their world keeps
the clears and the ship upgrades while your Boarder and its kit come home with you.

## Story

As a player joining a friend's game, I want the weapons and upgrades I earned to come
with me and their world's progress to stay theirs, so that neither of us loses anything
by playing together.

## Why it matters

**The ownership question is already ruled and currently unimplementable.** Zo ruled
2026-08-20 that a guest does work on someone else's ship and takes things back to their
own, with clear ownership over shared. `MetaState` has no seam that expresses it:
`BankedSalvage`, `EquippedPrimary`, `EquippedProtocol`, `WeaponsOwned`,
`WeaponWheelPoints`, upgrade levels and run counters are one document with one owner.
Persistence was deferred in the co-op demo specifically because this seam did not exist
(`../../Demo - Coop/README.md`).

**The model is borrowed and simplified.** `../../Brainstorms/realms.md` records the
source: No Rest for the Wicked's Realms, where the character is fully portable and the
world holds cleared areas, boss kills, town upgrades and resources.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.** This adds a
structure later work sits on rather than tuning a value.

## The split, ruled

Zo approved the initial allocation 2026-08-20 with the note that **play testing will
dictate the specifics**. Items may move; the seam does not.

| Boarder, travels | Realm, stays |
|---|---|
| Weapons owned | Salvage pot |
| Focus-ring upgrades per weapon | The whole salvage ledger |
| Equipped Primary and Secondary | Bastions cleared |
| Equipped protocol and passive | Door and unlock state |
| Equipment slot, equipment, drone bay | Ship and hatch upgrades |
| | Deepest wave reached |
| | Runs lost, clean exits |
| | Hatch conversions |
| | The twelve first-Bastion demo flags |
| | Realm name |

**Three groups were unallocated when this was written and are now ruled** (Zo, 2026-08-21),
because each one decides observable behaviour that the criteria below test:

- **The whole salvage ledger** — `BankedSalvage`, `LifetimeSalvageEarned` and
  `LifetimeSalvageSpent` — is realm-side together. Splitting the trio breaks the invariant
  `Earned == Banked + Spent`: the spend floor in `ReconstructSpentFromLevels` is built from
  weapons owned and derived Tiers, so a well-equipped Boarder entering a fresh realm would
  make that realm claim thousands of salvage it never earned. The realm's floor therefore
  keeps only its own hatch-armour term; the kit term moves to the Boarder as
  `ReconstructSpentOnKit` and stops feeding any realm's ledger.
- **`HatchConversions`** is realm-side. It gates the focus wheel and the equipment slot, and
  the wheel spends the realm's pot, so the gate belongs to the pot. A veteran Boarder entering
  a fresh realm has a locked wheel until that realm's first bank. That is coherent with the
  accepted consequence above rather than a new one.
- **All twelve demo flags** are realm-side. The demo *is* the first Bastion, which is a world.
  A new realm therefore replays it. Stated consequence: `DemoWheelBuysLeft` doubles as the
  multiplayer gate (`DemoProgress.MultiplayerUnlocked`), so entering a fresh realm re-locks
  multiplayer until that realm's first wheel purchase.

**"Protocols claimed" has no backing field.** `MetaState` carries only `EquippedProtocol` and
`EquippedPassive`; no claimed-or-owned mask exists anywhere in `Sim/Meta/BoarderProtocol.cs`.
The row is satisfied by carrying the two equipped ids. A claimed set, if one is ever wanted,
is a later milestone's field and lands on the Boarder.

**Whose pot does a guest bank into?** The host's. The pot is the world's salvage
(*Do not build*, below, rejects only a pooled *player* wallet), so everyone playing in a realm
draws on that realm's pot. A guest's credits and purchases hit the host's realm; the weapon
bought lands on the guest's Boarder and travels home. Consequence, accepted: the pot becomes
host-authoritative and replicated, and `M90`'s buy-back re-points from the guest's local
wallet onto it.

**Stated consequence, accepted rather than mitigated**: with weapons and upgrades on the
Boarder, a fresh realm can be entered by an arbitrarily strong player. Realm difficulty
cannot assume a curve it controls. That is the source model's behaviour and Zo's
farm-valve read of it is recorded under *Watch* in the brainstorm.

## Acceptance criteria

1. A player creates a realm from the main menu, names it, and enters it with an existing
   Boarder.
2. That Boarder arrives carrying every item in the left column: the same weapons owned,
   the same focus-ring points on each, the same equipped Primary, Secondary and protocol.
3. The new realm starts with none of the right column: no bastions cleared, no door
   unlocks, no ship upgrades, and the starting pot rather than a carried balance.
4. Buying a weapon in realm A and entering realm B shows that weapon owned in realm B.
5. Clearing a bastion in realm A and entering realm B shows that bastion uncleared in
   realm B.
6. Quitting to the main menu and re-entering realm A restores its cleared state, door
   state, ship upgrades and pot exactly.
7. A guest joining a host's realm plays in the host's world state and, on leaving, keeps
   every left-column change made during the session.
8. Nothing a guest does writes to the host's Boarder, and nothing the host does writes to
   a guest's.
9. Two realms exist simultaneously with independent right-column state and neither
   writes into the other.
10. A save written before this milestone does not load, and the game says so plainly
    rather than loading a corrupted state.

## Implementation

1. **Replace** `Sim/Meta/SaveData.cs` with `Sim/Meta/SaveFormat.cs`: version 12, and two
   serialized documents rather than one. **Premise corrected 2026-08-21** — the "never write
   a migration shim" rule this step cited to `../../Decisions.md` is not there, and the
   shipped practice through v11 was the opposite: `FromSaveData` carried gates for v3, v4,
   v6, v7, v9, v10 and v11, and `Decisions.md` keeps `WeaponId.Vk7Rifle`'s value expressly
   "so old saves deserialize". Refusing v11 is therefore **new policy, ruled here** (Zo,
   2026-08-21) and not the application of an old one. It is worth the break: a Boarder
   assembled out of a single-document save has to guess which half of that save was the
   world's, and a wrong guess corrupts a campaign quietly. The v1–v11 ladder is deleted with
   the tests that exercise it; the version history survives as documentation on `SaveFormat`.
2. **Create** `Sim/Meta/BoarderState.cs` (pure Sim). Left-column fields, lifted out of
   `MetaState` rather than duplicated.
3. **Create** `Sim/Meta/RealmState.cs` (pure Sim). Right-column fields, same lift.
4. **Change** `Sim/Meta/MetaState.cs` to compose the two rather than own the fields.
   Every existing read site keeps working through forwarding; that is what makes this a
   restructure rather than a rewrite. **One exception, found 2026-08-21**: `MetaUpgrades.Buy`
   takes a `ref` to the public field `HatchArmorLevel` (`Game/Ship/ShipCommands.cs` plus
   eight test sites), and a property cannot be passed by `ref`. Those call sites become
   `ref m.Realm.HatchArmorLevel`, which is legal because the field lives on a heap object.
   No shadow field is added to `MetaState` to dodge it.
5. **Change** `GameController` so the realm side is host-owned and mutated only through
   command methods (R1, R3). A guest's Boarder is written locally by that guest; the
   realm is written only by the host.
6. **Create** the realm list in a **new scene**, `Assets/Scenes/RealmSelect.unity`:
   create, name, enter, and pick which Boarder enters. No difficulty selection, which is the
   source model's and not a question this project has. **Scope corrected 2026-08-21** (Zo):
   `MainMenu.unity` is not edited — it is 74,385 lines of authored Michsky Shift content
   bound by serialized reference, and a new panel inside it is neither reviewable as a diff
   nor safe to hand-author. The existing CONTINUE and START NEW buttons reach the new scene
   through `MainMenuController`, which is code, so the menu scene stays untouched. The list
   itself copies the template-clone idiom already used by `OptionsView.SetControlRows` and
   `CompendiumView`.
7. **Change** `SaveService` call sites so Boarder and realm write independently. A realm
   write must not require a Boarder write or the guest case deadlocks. Two things found
   2026-08-21 ride here:
   - **The PlayMode save-guard resolves `SaveService.SavePath` by reflection**
     (`Assets/Tests/PlayMode/PlayModeTestUtil.cs`) and backs up exactly one file. Split the
     save without extending the guard and the suite silently stops guarding half the
     developer's campaign — the failure mode `M17` was built to prevent. The guard is
     extended in the same commit as the split, not after.
   - **A guest's banked salvage is never persisted at end of run today.** It is credited in
     memory on the owner RPC, but the guest branch of `ReturnToShip` returns before the
     `GameSession.Save()` that follows it, so a guest who banks and quits without buying
     loses it. Criterion 7 cannot pass over that defect, so closing it is this milestone's
     work rather than a follow-up's.

8. **Correct** `GameSession`'s class header. It states that this state lives on the host and
   is never mutated by remote clients; four owner-targeted RPC handlers on `NetPlayerAvatar`
   already disprove it, and the split makes the true rule statable — the Boarder is written
   by its own client, the realm only by the host.

Rejected: keeping one document with an owner field per record. It reads as cheaper and
makes every future query ask "whose is this", which is exactly the confusion the split
exists to remove.

## Verification

- Criteria 1-6, 9: EditMode tests over save and load of the two documents, asserting
  each column lands in the right place and that a second realm's right column is
  independent. Then a playthrough: create realm A, buy a weapon, clear a bastion, quit,
  create realm B, confirm the weapon and not the clear.
- Criteria 7, 8: PlayMode net test against `NetcodeIntegrationTest`. **Method corrected
  2026-08-21** — the test as originally written cannot be built. `NetcodeIntegrationTest`
  runs host and guest in one process and `GameSession.Meta` is a single static, so there is
  literally one Boarder object for both roles and "the host's does not" has nothing to
  compare against. `NetDownWaitTests` already works around this by asserting that the *host
  gate* does not debit and, separately, that the *owner RPC* does. Criterion 8 therefore
  needs a per-role seam first: `GameSession` must be able to hold a Boarder per client id in
  a test session. That seam is part of this milestone's step 5, and it was not costed in the
  original plan.
- Criterion 10: load a v11 save and read the refusal in `Logs/gkb_latest.log`.

**Driven path** (supersedes the playthrough this section originally called for; Zo,
2026-08-21): from the chooser, create realm "A", enter with a Boarder, grant a Primary and a
cleared bastion through the dev commands rather than by replaying the loop to earn them, quit
to the menu, create realm "B", enter with the same Boarder. The Primary is in your hands. The
bastion is uncleared. The taint is stated in the close, per `../../Decisions.md`.

Most of that is automatable and should be automated — see *State at build* above.

## A realm's ship is the co-op hull (Zo, 2026-08-21)

Entering a realm opens `Ship - CoOp`, not `Ship - Demo`, and so does CONTINUE and the return
from a Run. A realm is one world however you reached it; landing in a different hull depending
on which button you pressed reads as a defect. `Ship - Demo` stays reachable only from the F12
dev checkpoints, which build a demo beat directly.

Checked rather than assumed: `Ship - CoOp` is a strict superset of `Ship - Demo` — every
project component in the demo ship is present in the co-op one, and the co-op scene carries
about half as many again. Solo play is safe there because the party panel and the hatch
assignment each check for a live session and fall back to a single seat.

**Consequence, accepted**: a fresh realm starts `DemoActive`, so the first-Bastion tutorial now
plays out on the co-op hull. That is the intent — the demo is a world's first Bastion, and the
world's ship is this one.

## State at build, 2026-08-21

Merged to `main`. **Verified in the Unity runner**, both modes, on the merged tree.

**EditMode 649 passed / 11 failed. PlayMode 68 passed / 1 failed.** Every one of those twelve
is the recorded pre-existing set — the M59 polish five, the two HUD separation rows, the
BastionDoorAperture 3.5-vs-4.5, SessionMusic, the `MasterControl.HatchWaves` isolation row,
ViewModelAnim, and `NetVerbTests` repair timing at 229.5 against a 229.0 ±0.05 tolerance.
**This milestone introduces no test failure.** Attribution was by experiment rather than
argument: the same offline harness was run against a detached worktree of `main` before the
merge, and the failing sets match.

**What the criteria now rest on.** 1 and 6 are closed by `RealmChooserTests`, which loads
`RealmSelect.unity`, types a name, creates a realm, enters it, leaves for another world and
comes back to find the pot, the doors, the clears and the ship upgrade as they were. 2, 3, 4,
5, 9 and 10 are closed by `RealmAndBoarderSaveTests` at the document level, which is where
those claims live, with 4 and 5 also driven end-to-end through the chooser. 7 is covered for
the half that one process can show.

The scene is no longer an unknown: it was hand-authored offline, and a test now loads it every
run and proves the name field is bound both ways rather than merely present.

**Still owed.**

- **Criterion 8's second half.** That a guest's session does not write the host's Boarder is
  tested. The mirror is not, and deliberately has no test: host and guest share one static
  `GameSession.Meta` in a single process, so a guest's Boarder would be an object nothing was
  handed and any such test would pass by construction. It needs two machines.
- **Eyes on the chooser.** It is proven to load and bind; nobody has looked at it. The layout
  was reasoned about, not seen.

## Usable, or it did not ship

**Driven**: creating a realm, naming it, choosing which Boarder enters. **Shown**: the
realm list on the main menu, and a fresh world with a familiar gun. **Reads back**: the
weapon in hand in a realm that has never seen it, and the realm's own cleared state on
return.

## Unblocks

`M97` (the ship shows the realm's record) and `M98` (drop-in), neither of which has
anything to display or join without this.

## Do not build

Realm difficulty selection. Multiple realms as a headline feature beyond criterion 9's
correctness. Host migration. Any migration shim. A shared player wallet, which stays
rejected: the realm pot is the world's salvage, not a pooled personal balance.
