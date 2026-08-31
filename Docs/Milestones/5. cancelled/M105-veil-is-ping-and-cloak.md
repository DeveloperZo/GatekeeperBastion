# M105 — VEIL is Ping and Cloak

<!--
Status: later
Update trigger: if doctrines are reintroduced under the conditions in the parked entry (equipment is more than one row, and two protocols need the same machine to differ).
Last updated: 2026-08-29
-->

VEIL stops carrying a decoy doctrine. A protocol becomes exactly three things: a crew
burst, a personal burst, and a passive. The doctrine concept comes out with it, because
after this change it has no live members. It is parked rather than buried: it returns when
equipment is worth modifying, and it returns with behaviour rather than as a row every
sheet has to fill.

## Story

As a Boarder, I want VEIL to be about attention — who is looking at me and whether they
can find me — so that its sheet reads as one idea instead of three unrelated ones.

## Why it matters

VEIL was authored as Ping, Cloak and Ghost, all of which are about attention, plus a
fourth row, PHANTOM, that changes what a placed decoy does. Zo, 2026-08-29: "VEIL has
nothing to do with phantom. Veil using Ping and Cloak is the focus." The doctrine row
makes the sheet answer a question the protocol is not about, and it is the only row on
the sheet a player cannot exercise from the two ability keys.

Nothing in play loses a behaviour. PHANTOM's transfer is already inert:
`EnemyGatekeeper.Targeting.TransferInterestTo` writes `_decoyTarget` and then calls
`Retarget()` as its last statement, which re-derives from `DecoyState.ShouldPull` and
hands back any body outside the pull radius — so the only bodies PHANTOM leaves on a
decoy are the ones the ordinary pull would have taken on the next tick. The placement log
still counts them as "inherited", which is the line that made the doctrine look like it
worked. FORGE's PARALLEL BAYS is the other member and has no caller at all
(`BoarderProtocol.MaxLiveUnder` and `ParallelBaysMaxLive` are unreferenced;
`DecoyState.MaxLive` is assigned from the payload catalog, which is 1).

With PHANTOM gone, doctrine has zero live members. Zo ruled 2026-08-29 to take the
concept out rather than keep an empty slot, and to bring it back **after equipment is
implemented and solidified** (`../../Brainstorms/boarder-protocols.md`, *Doctrines*).

That parking is why this milestone deletes rather than dormants. The diagnosis in the
parked entry is that doctrine was a slot before it was a behaviour: one member did nothing
by definition, two did nothing in fact, and every sheet printed a row no ability key could
exercise. Leaving the enum in place as a hook preserves precisely the thing that failed.
Nothing here reaches the save schema either — `DecoyState.Mode` is Run state and no
doctrine is serialized — so there is no migration cost to deleting it and no saved data to
honour when it returns.

Leverage tier: this is a **rule change** (#5), not a parameter. No number moves.

## Acceptance criteria

1. Claiming VEIL at CREW QUARTERS and reading its sheet shows exactly three rows — crew,
   personal, passive — and no doctrine row. The same is true of Default and FORGE.
2. Placing a decoy while wearing VEIL pulls Gatekeepers by the ordinary rule only: no
   transfer runs, and the placement log line contains no inherited-gatekeeper clause.
3. `DoctrineMode`, `BoarderProtocol.DoctrineOf`, `BoarderProtocol.MaxLiveUnder`,
   `ParallelBaysMaxLive`, `BoarderProtocol.DoctrineText`, `BoarderSheets.Doctrine`,
   `BoarderSheets.DoctrineDefault` / `DoctrineVeil` / `DoctrineForge`,
   `DecoyBay.ApplyPhantom` and `DecoyBay.PlaceMode` do not exist in the tree.
4. `BoarderProtocol.Sheet` has four fields (name, crew, personal, passive) and every
   construction of it, including the Coming Soon stub in `CrewQuartersStation`, passes
   four arguments.
5. `DecoyState.TryPlace` has one signature and takes no mode; `DecoyState.Mode` and
   `DecoyObject.Mode` do not exist.
6. The CREW QUARTERS card and sheet views render with no empty row and no gap where the
   doctrine row was, on Default, VEIL, FORGE, and both Coming Soon stubs.
7. The offline harness passes with the recorded failure set and no new reds.
8. `Docs/Play Mechanics/Protocol-VEIL.md`, `Protocols.md` and `The-Drone.md` describe
   VEIL as three rows and carry no doctrine claim, in this same change.

## Implementation

**Change** `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` — delete the `DoctrineMode` enum,
`DoctrineOf`, `MaxLiveUnder`, `ParallelBaysMaxLive` and `DoctrineText`. Drop `Doctrine`
from the `Sheet` struct and its constructor, and drop the argument from `SheetOf`.

**Change** `Assets/Scripts/Sim/Meta/BoarderSheets.cs` — delete `DoctrineDefault`,
`DoctrineVeil`, `DoctrineForge` and `Doctrine(BoarderProtocolId)`.

**Change** `Assets/Scripts/Game/Bastion/DecoyBay.cs` — delete `ApplyPhantom` and
`PlaceMode`, the `inherited` local and the inherited clause in the placement log line, and
the `ApplyPhantom` call site. `TryPlace` passes no mode.

**Change** `Assets/Scripts/Game/Bastion/DecoyObject.cs` — delete `_mode`, `Mode` and the
three-argument `Configure`; keep the two-argument one as the only overload.

**Change** `Assets/Scripts/Sim/Run/DecoyState.cs` — delete `Mode`, delete the
mode-taking `TryPlace` overload and keep the plain one as the only signature, and remove
the mode reset in `ResetForRun`.

**Change** `Assets/Scripts/Game/Ui/ProtocolCardView.cs` and
`Assets/Scripts/Game/Ui/ProtocolSheetView.cs` — remove the `_doctrine` serialized field
and its bind call.

**Change** `Assets/Scripts/Game/Ship/CrewQuartersStation.cs` — the Coming Soon stub sheet
construction drops its empty doctrine argument.

**Change** the CREW QUARTERS card and sheet prefabs — delete the doctrine text object.
This is authored, not code: a removed serialized field leaves the object in the prefab
showing its last authored string, which is exactly how a retired rule keeps being read as
live. **This is the step most likely to be missed**, and criterion 6 is what catches it.

**Change** `Docs/Play Mechanics/Protocol-VEIL.md`, `Protocols.md`, `The-Drone.md` — remove
the doctrine claims and the PHANTOM material, in this same change (the entry is written or
updated in the change that ships the mechanic, `_meta/_definitions/play-mechanic.md`).

**Change** `Docs/Brainstorms/boarder-protocols.md` — the parked *Doctrines* entry is
already written there (2026-08-29) and needs nothing further. What is still owed is the
sheet table further down that file, whose doctrine column is no longer the shape protocols
use: mark the column against the parked entry in one line rather than deleting the
thinking, since the UTL-H turret column beside it is a separate live design.

Rejected: keeping `DoctrineMode` with a single `Stock` member as a hook for the
reintroduction. It leaves a concept in the sheets and the panel that no player can
observe, which is what this milestone exists to remove, and it is the exact shape the
parked entry blames for the first failure. The reintroduction's own bar is a named rule
with behaviour on the day it ships, which needs no hook left behind for it.

## Supporting work

- **Driven by** the existing `G` and `H` bindings — unchanged here.
- **Shown by** the CREW QUARTERS sheet, which is the surface losing a row — here.
- **Reads back as** the sheet itself and the decoy placement log line — here.

## Verification

1. Criterion 3 and 5 — `grep -rn "DoctrineMode\|DoctrineOf\|MaxLiveUnder\|ParallelBaysMaxLive\|DoctrineText\|ApplyPhantom\|PlaceMode" Assets/Scripts` returns nothing.
2. Criterion 4 — the tree compiles; every `new BoarderProtocol.Sheet(` call site passes four arguments.
3. Criterion 7 — `Tools/verify/run.sh`, expect the recorded failure set and no new reds.
4. Criteria 1 and 6 — playthrough path: Play, from the Ship walk to CREW QUARTERS, tap
   interact, read each of the five catalog rows' sheets. Confirms three rows on each, no
   empty row, no gap.
5. Criterion 2 — playthrough path: claim VEIL, embark, buy and equip the Decoy first if the
   bay is empty, start a Wave at the Hatch, pull a Gatekeeper onto yourself, then place the
   decoy. The log line reads `placed — charges left N, hp N` with no inherited clause, and
   the body on you does not change target at the moment of placement.

## Watch

- The prefab step above. A serialized field removed in code does not remove the authored
  object.
- `Docs/_meta/_definitions/language.md` has no doctrine row today; if one is added before
  this lands, it goes with the concept.

## Depends on / Unblocks

Unblocks nothing. FORGE's recut is `M106` and `M107`; this change clears a row from
FORGE's sheet without deciding anything about them.

Does not block the reintroduction. Its trigger is equipment, not this milestone:
doctrines return when the equipment roster has stopped being one row and two protocols
independently want the same machine to behave differently — both halves, per
`../../Brainstorms/boarder-protocols.md`, *Doctrines*.
