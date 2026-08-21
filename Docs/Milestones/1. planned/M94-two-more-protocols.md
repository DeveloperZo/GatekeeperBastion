# M94 — Two more protocols

<!--
Status: planned
Update trigger: when a protocol is added or retired, when an ability id gains numbers,
when the legality rule changes, when the CREW sheet layout changes
Last updated: 2026-08-21
-->

`BoarderProtocolId` grows from one to three. **VEIL** and **FORGE** are authored end to
end: doctrine, crew ability, personal ability, a passive, sheet copy, and a legality
table that stops being a single hardcoded pair.

## Story

As a Boarder standing next to a teammate, I want to be playing a different protocol
from theirs, so that the two of us are contributing different things to the same
fight.

## Why it matters

**One protocol exists and it is hardcoded, not configured.**
`Sim/Meta/BoarderProtocol.cs` returned `Default` or nothing from every lookup:
`ShopProtocols()` was a one-element array, `DoctrineOf` returned `Stock`
unconditionally, `CrewAbilityOf` and `PersonalAbilityOf` were ternaries on `Default`,
`PassiveCount` was `return 0`, and `IsLegalPair` was the single expression
`protocol == Default && passive == None`. Adding a protocol is not a data entry
today; it is turning a set of constants into a catalog.

**This is the co-op demo's largest content gap and the one its argument rests on.**
`Docs/Demo - Coop/README.md` exists to sell four maturations, one of which is seeing
your teammate. Two seats running the same protocol makes that maturation hollow.

**Half the ids were already reserved.** `BoarderAbilityId` carries `Anchor`,
`Bulkhead`, `Ghost` and `Foresight` alongside the live `Barrage` and `Rush`. The
naming work was done; the numbers and behaviour were not.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.** This adds a new
axis of player difference rather than tuning an existing one.

## Which two, and why

**Settled 2026-08-21: VEIL and FORGE.** `../../Brainstorms/boarder-protocols.md`
carries four finished specialist sheets, so this was never a design pass — it was a
cost decision between four authored candidates. The audit that decided it:

| Sheet | Crew / Personal | Substrate that already exists | Genuinely new |
|---|---|---|---|
| **VEIL** | Ping / Cloak | `PlayerHealth.AcquisitionMul` (built, only ever set to `1f`), `PlayerRegistry.NearestAlive` as the targeting list, `EnemyGatekeeper.Provoke` for the pull | the through-walls render |
| **FORGE** | Field Rearm / Redeploy | `DecoyBay`, `DecoyState`, unbanked wallets, `FirstPersonController` | channel + carry state |
| **AEGIS** | Anchor / Bulkhead | `AnchorField.cs` complete, `NotifyBulkheadConsumed`, `ArmBulkhead` | little — but see below |
| **LEDGER** | Writ / Recall | unbanked wallets, the Hatch | world salvage piles, through-walls render, per-enemy mark, in-Bastion teleport |

**AEGIS is the cheapest and was still rejected.** Zo deferred it 2026-08-15, and
independently its ANCHOR is a planted, rooted, radius-8 zone — structurally the same
shape as Default's BARRAGE, differing only in what the zone does. Criterion 6 asks
for effects that are not the same effect with different numbers, and two planted
rooted zones is the reskin that criterion exists to catch. **LEDGER is the most
expensive**, needing four subsystems this milestone's *Do not build* list never
budgeted for. VEIL and FORGE are the pair whose crew abilities are each a different
*shape* — a windowed pulse and a rooted channel against Default's planted zone.

VEIL also carries the brainstorm's stated pacing: *"pacing starts at VEIL"*, and
VEIL's deed is named there as the first to ship.

**Deviation, recorded:** the brainstorm gives VEIL the **Sightline** passive with
Ghost parked. This ships **Ghost** instead. Sightline renders acquisition radii
in-world, which needs the same unbuilt render path as Ping's; Ghost is an acquisition
bias whose magnitude is already in the catalog and whose reader is already wired. One
passive per protocol is what criterion 4 asks for, and the brainstorm's own kill
criterion 5 permits a protocol to ship one passive and no pick. Sightline stays
parked as VEIL's second.

## Premises corrected against the code, 2026-08-21

Read these before implementing; the earlier draft asserted each wrongly.

- **The next free protocol ids are 2 and 3, not 4 and 5.** The enum is `None = 0`,
  `Default = 1`, and no id has ever been retired.
- **`SaveData.CurrentVersion` is 11, not 8.** `ApplySaveEquip`'s `>= 8` gate is a
  floor for the equip fields, not the current schema.
- **No save bump, and no key rename.** The earlier step 7 cited a standing rule in
  `../../Decisions.md` that is not there. `EquippedProtocol` / `EquippedPassive` are
  already ints; new enum values change no schema shape and need no migration. Bumping
  would force a migration of every unrelated field for nothing.
- **`RequestBulkheadServerRpc` is at `NetVerbs.cs:235`**, not 232-236.
- **The Bulkhead defect is worse than described.** `ArmBulkhead()` has **zero
  callers** and the RPC handler is empty — both halves are built and neither is
  connected — while `PlayerProtocolAbilities.cs:166` called `RequestBulkhead()` on
  **every** `H` press regardless of protocol. Default's personal is Rush. Neither
  chosen protocol takes Bulkhead, so per *Do not build* the handler stays unbuilt and
  the wrong call site is deleted instead.

## Acceptance criteria

1. `ShopProtocols()` returns three ids and the CREW station lists all three.
2. A player claims a protocol other than Default at CREW and the claim persists across
   a save and load.
3. Each of the three protocols returns a distinct crew ability and a distinct personal
   ability from `CrewAbilityOf` and `PersonalAbilityOf`.
4. Each new protocol has at least one passive, so `PassiveCount` returns non-zero for
   it, and `PassiveAt` returns the id.
5. `IsLegalPair` accepts every protocol paired with a passive that protocol owns, and
   rejects every protocol paired with a passive it does not own, for all three.
6. In a Run, pressing the personal-ability input on each protocol produces that
   protocol's effect and its own log line, and the three effects are not the same
   effect with different numbers.
7. The CREW sheet shows the correct doctrine, crew, personal and passive text for
   whichever protocol is equipped, with no empty string on any row.
8. Two players in one Run equipped with different protocols each get their own
   abilities, and neither seat's ability applies to the other's input.
9. `MetaState.EquippedProtocol` round-trips all three ids through save and load, and
   an id that no longer exists resolves to `None` rather than throwing.

## Implementation

1. **Change** `Sim/Meta/BoarderProtocol.cs`. Add `Veil = 2` and `Forge = 3` (never
   renumber, never reuse). Replace every ternary-on-Default lookup with a switch over
   all three. Append `Phantom` and `ParallelBays` to `DoctrineMode` after `Stock`.
2. **Add** `Sim/Meta/BoarderSheets.cs` for the authored copy. The catalog is numbers
   and the sheets are prose; they change for different reasons, so they are separate
   types rather than one file split with `partial`.
3. **Change** `IsLegalPair` to the general rule the single expression was a special
   case of: catalogued, and either owning the passive or wearing none. **A general
   `OwnsPassive || None` alone is not enough** — an unknown id owns nothing and wears
   none, so it would read as legal and survive. `IsCatalogued` is what makes
   criterion 9 hold.
4. **Change** `Game/Player/PlayerProtocolAbilities.cs` to dispatch on ability id, with
   a log line per ability. Delete the unconditional `RequestBulkhead()` call. Gate
   `RushOpen` and `BarrageWindowOpen` on the **ability id**, not on the window — a
   Cloak or Redeploy window opens the same slot and must buy nothing at the gun.
5. **Add** `Game/Player/PlayerHardwareHandling.cs` for FORGE's two verbs. Both act on
   the same machine and hold state across frames, which is why they are one type and
   not two, and why they are not in `PlayerProtocolAbilities` — that owns windows and
   input, this owns the machine.
6. **Change** `Game/Ship/CrewQuartersStation` to fill its rows from `ShopProtocols()`
   and pad the remainder with Coming Soon copy, so a protocol leaves the stub list by
   entering the catalog rather than by being edited in two places.
7. **Change** `Game/Net/NetPlayerAvatar.Protocol.cs` to clamp through the catalog
   once. The hand-listed Default+Ghost/Foresight case was the general rule spelled out
   for the only protocol that existed.

Rejected: making protocols purchasable rather than claimed. `M70` settled claim at 0
salvage and changing it is a separate economic decision.

## Deferred, deliberately

Named so they read as decisions rather than omissions:

- **Ping's through-walls render.** Ping ships as its pull half — the crew ability
  turns the room onto the caster and says so in the log. `PingWindowOpen` is the flag
  a presentation pass reads. Marked `// POC:` at the site. **This is the one place
  M94 does not fully meet *Shown*.**
- **Doctrine behaviour.** `Phantom` and `ParallelBays` are *written* onto placed
  decoys; neither has a reader yet. This is exactly how `Stock` shipped — the
  brainstorm's own note reads *"STOCK writes 0. VEIL is the first reader."*
  `MaxLiveUnder` is in the catalog, tested, and unconsumed by `DecoyBay`.
- **Twist slots and second passives.** Growth levers, one at a time, behind the
  pick-rate audit. Not this slice.
- **Server paths for the new verbs.** Barrage still routes through `NetVerbs`; Ping,
  Cloak, Field Rearm and Redeploy run host-side only. Criterion 8 is what tests
  whether that is enough.

## Open questions

- **Whether the third protocol should be reachable inside the co-op demo or granted at
  its start.** *Unknown.* The demo grants kit rather than teaching shops, which argues
  for granted.
- **`2. active/` is at its cap of 3** (`M63-3`, `M91`, `M93`), so this file cannot move
  to active without a status change elsewhere. Owner call.

## Verification

- Criteria 1, 2, 7: playthrough. Play, CREW, see three rows, claim a non-Default one,
  read every sheet row, quit to menu, Continue, confirm it is still equipped.
- Criteria 3, 4, 5, 9: `Assets/Tests/EditMode/Meta/ProtocolCatalogTests.cs` — distinct
  abilities asserted pairwise, `PassiveCount` non-zero, `IsLegalPair` over the **full
  enum cross product** rather than sampled pairs, and round-trip including an unknown
  id resolving to `None`.
- Criterion 6: playthrough per protocol with the log open. Rush multiplies fire and
  you move; Cloak removes you from `PlayerRegistry.NearestAlive` and collapses on your
  own first shot; Redeploy puts a machine in your hands, slows you and holsters the
  gun. Whether the three *read* as different in play is eyes-on and is the thing the
  tests cannot see.
- Criterion 8: PlayMode net test against `NetcodeIntegrationTest` with two seats on
  different protocols, asserting each seat's ability state moves independently.

**Playthrough path**: Play, Continue, CREW, claim VEIL, embark, press `H` at a
gatekeeper and watch it lose you; press `G` and watch the room turn. Repeat with
FORGE: place a decoy, let it take damage, hold `G` beside it to pour unbanked into it,
then `H` to pick it up and walk it somewhere better.

## Usable, or it did not ship

**Driven**: claiming at CREW, and `G` / `H` in a Run. **Shown**: three rows at the
CREW station and a sheet that changes with the claim. **Reads back**: the sheet text,
the ability bar, and one log line per ability — Field Rearm's names the salvage spent
and the HP bought; Redeploy's names the machine's life as it is lifted and set down.

**Where it falls short**: Ping's *Shown* is the log line and nothing in the world
until the render pass lands. Called out above rather than left silent.

## Unblocks

`Docs/Demo - Coop/` beat 4, which has nothing to show if both seats are Default.

## Do not build

A fourth protocol. Protocol purchase costs. Passive swapping UI beyond what the sheet
already does. The `Bulkhead` handler — neither chosen protocol takes it. Twists.
Sightline's in-world radii. LEDGER's world piles.
