# M94 — Two more protocols

<!--
Status: planned
Update trigger: when a protocol is added or retired, when an ability id gains numbers,
when the legality rule changes, when the CREW sheet layout changes
Last updated: 2026-08-20
-->

`BoarderProtocolId` grows from one to three. Two new protocols are authored end to
end: doctrine, crew ability, personal ability, passives, sheet copy, and a legality
table that stops being a single hardcoded pair.

## Story

As a Boarder standing next to a teammate, I want to be playing a different protocol
from theirs, so that the two of us are contributing different things to the same
fight.

## Why it matters

**One protocol exists and it is hardcoded, not configured.**
`Sim/Meta/BoarderProtocol.cs` returns `Default` or nothing from every lookup:
`ShopProtocols()` is a one-element array, `DoctrineOf` returns `Stock` unconditionally,
`CrewAbilityOf` and `PersonalAbilityOf` are ternaries on `Default`, `PassiveCount` is
`return 0`, and `IsLegalPair` is the single expression
`protocol == Default && passive == None`. Adding a protocol is not a data entry today;
it is turning a set of constants into a catalog.

**This is the co-op demo's largest content gap and the one its argument rests on.**
`Docs/Demo - Coop/README.md` exists to sell four maturations, one of which is seeing
your teammate. Two seats running the same protocol makes that maturation hollow: there
is nothing to see. Every other item in that demo is a system; this is the content that
gives the systems something to differentiate.

**Half the ids are already reserved.** `BoarderAbilityId` carries `Anchor`, `Bulkhead`,
`Ghost` and `Foresight` alongside the live `Barrage` and `Rush`, none of them used.
The naming work was done; the numbers and behaviour were not.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.** This adds a new
axis of player difference rather than tuning an existing one.

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

1. **Change** `Sim/Meta/BoarderProtocol.cs`. Add two ids to `BoarderProtocolId` (4 and
   5 are next; never renumber, never reuse). Replace every ternary-on-Default lookup
   with a switch over all three. Add the doctrine modes the two new protocols write
   onto decoys, appended to `DoctrineMode` after `Stock`.
2. **Change** the same file's numbers block. Each new ability gets its constants beside
   `BarrageFireMul` and `RushFireMul`, in the same style: a named constant per number,
   read by verbs and server clamps and nowhere else.
3. **Change** `IsLegalPair` to `OwnsPassive(protocol, passive) || passive == None`.
   That is the general rule the current single expression is a special case of.
4. **Change** `Game/Player/PlayerProtocolAbilities.cs` and `Game/Player/PlayerAbility.cs`
   to switch on the new ability ids. **Note**: `NetVerbs.RequestBulkheadServerRpc`
   (`NetVerbs.cs:232-236`) is an **empty handler with a live call site** at
   `PlayerProtocolAbilities.cs:166`. If a new protocol takes `Bulkhead`, that handler
   must be implemented in this milestone rather than inherited. If neither new protocol
   takes it, leave it and do not build on it.
5. **Change** `Game/Ui/ProtocolSheetView.cs` and `ProtocolAbilityBarView.cs` to read
   the catalog rather than assume one protocol. These are read-only views (R2).
6. **Change** `Game/Ship/CrewStation` claim path to list and claim from
   `ShopProtocols()`. `BuyProtocol` → `TryClaim` already exists from `M70`; extend it,
   do not replace it.
7. **Bump** the save schema and rename the key rather than writing a migration shim,
   per the standing rule in `../../Decisions.md`.

Which two protocols, and what they do, is **not decided here** and is the one thing
this milestone cannot start without. See *Open questions*.

Rejected: making protocols purchasable rather than claimed. `M70` settled claim at 0
salvage and changing it is a separate economic decision.

## Open questions

- **Which two protocols, and what each one refuses.** *Unknown, needs a design pass.*
  `../../Brainstorms/boarder-protocols.md` carries the identity framing and the
  reserved ability ids; it does not carry two chosen protocols. The roster rule that
  applies here is the same one `M63` uses for gatekeepers: a thing is defined by what
  it refuses, and if two protocols read as the same protocol the set is
  over-specified. **This milestone cannot be started until the two are chosen.**
- Whether the third protocol should be reachable inside the co-op demo or granted at
  its start. *Unknown.* The demo grants kit rather than teaching shops, which argues
  for granted.

## Verification

- Criteria 1, 2, 7: playthrough. Play, CREW, see three rows, claim a non-Default one,
  read every sheet row, quit to menu, Continue, confirm it is still equipped.
- Criteria 3, 4, 5, 9: EditMode tests over all three ids: distinct abilities asserted
  pairwise, `PassiveCount` non-zero, `IsLegalPair` true for owned and false for
  unowned across the full cross product, and a round-trip test including an unknown id
  resolving to `None`.
- Criterion 6: playthrough per protocol with the log open, reading each ability's line.
  Whether the three read as different in play is eyes-on and is the thing the tests
  cannot see.
- Criterion 8: PlayMode net test against `NetcodeIntegrationTest` with two seats on
  different protocols, asserting each seat's ability state moves independently.

**Playthrough path**: Play, Continue, CREW, claim the second protocol, embark, press
the personal-ability input at a gatekeeper, read the log line. Repeat with the third.

## Usable, or it did not ship

**Driven**: claiming at CREW and pressing the ability input. **Shown**: three rows at
the CREW station and a sheet that changes with the claim. **Reads back**: the sheet
text, the ability bar, and one log line per ability.

## Unblocks

`Docs/Demo - Coop/` beat 4, which has nothing to show if both seats are Default.

## Do not build

A fourth protocol. Protocol purchase costs. Passive swapping UI beyond what the sheet
already does. The `Bulkhead` handler, unless a chosen protocol takes it.
