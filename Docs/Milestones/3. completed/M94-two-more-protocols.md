# M94 — Two more protocols

<!--
Status: planned
Update trigger: when a protocol is added or retired, when an ability id gains numbers,
when the legality rule changes, when the CREW sheet layout changes
Last updated: 2026-08-23
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
| **FORGE** | Field Deploy / Overcharge | `DecoyBay` as the shape to copy, `Damageable`, the spawner ledger, `WaveSpawner`'s death path for kill credit | the turret itself |
| **AEGIS** | Anchor / Bulkhead | `AnchorField.cs` complete, `NotifyBulkheadConsumed`, `ArmBulkhead` | little — but see below |
| **LEDGER** | Writ / Recall | unbanked wallets, the Hatch | world salvage piles, through-walls render, per-enemy mark, in-Bastion teleport |

**AEGIS is the cheapest and was still rejected.** Zo deferred it 2026-08-15, and
independently its ANCHOR is a planted, rooted, radius-8 zone — structurally the same
shape as Default's BARRAGE, differing only in what the zone does. Criterion 6 asks
for effects that are not the same effect with different numbers, and two planted
rooted zones is the reskin that criterion exists to catch. **LEDGER is the most
expensive**, needing four subsystems this milestone's *Do not build* list never
budgeted for. VEIL and FORGE are the pair whose crew abilities are each a different
*shape* — a windowed pulse and a placed machine against Default's planted zone.

VEIL also carries the brainstorm's stated pacing: *"pacing starts at VEIL"*, and
VEIL's deed is named there as the first to ship.

### FORGE was recut on 2026-08-21, and why

FORGE first shipped as **Field Rearm** (channel unbanked into a machine) and **Redeploy**
(carry a machine and set it down). Zo rejected it, correctly: both abilities were the same
shape — *make yourself weak while you fiddle with hardware* — and neither had a moment where
the Boarder became stronger. That is the criterion 6 failure this milestone used to reject
AEGIS, and the first audit missed it by checking FORGE against Default and never against
itself.

It was also the Engineer with no engine. The only deployable in the game was the decoy: a
120 HP aggro sponge that does not shoot. The brainstorm had flagged this all along — Field
Rearm is written *"wants UTL-H to fully matter"* and Redeploy *"wants UTL-H"* — and
"works on decoys today" turned out to be true and thin.

So the turret is built here rather than waited for, and FORGE becomes **Field Deploy** and
**Overcharge**. Two refinements were forced by standing rules and are recorded so they are
not re-litigated:

- Zo's first wording gave allies in radius **increased damage**. Damage grants are on the
  `M70` cut list, carried in the brainstorm. The damage is delivered instead by **the
  turret's own gun** — the area does hit harder because a machine is shooting in it, and no
  player weapon is granted damage.
- Fire rate is Default's signature: Barrage already *is* "allies in radius fire faster" and
  Rush already *is* "you fire faster". FORGE differs in **shape**, not in number. Barrage is
  a window where the caster stands rooted and the aura is centred on *them*; Field Deploy is
  a **destructible object** placed and walked away from, whose uptime the room decides and
  which outlives the press. Overcharge is not a free self-buff: it refuses without a machine
  standing, and it **costs that machine its life**.

**Magnet Hands became load-bearing.** It was a sheet row with no behaviour when FORGE was
Field Rearm / Redeploy. It is now the rule that makes the turret mortal: a Gatekeeper that
would come for the Boarder comes for the machine instead when both are in reach. Without it
nothing ever attacks a turret, so the passive is what buys FORGE its risk.

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
   Ability ids 9 and 10 briefly named `FieldRearm` / `Redeploy` during this milestone and
   are **reused**, not retired, for `FieldDeploy` / `Overcharge`: only protocol and passive
   ids are persisted and crew/personal ids are always derived, so neither number ever
   reached a save.
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
5. **Add** `Sim/Run/TurretState.cs`, `Game/Bastion/TurretObject.cs` and
   `Game/Player/PlayerTurretControl.cs`. The turret is spawned **by the ability**, not
   bought: it is not a `PayloadId`, has no FABRICATION row, and does not touch the drone
   bay or the purchase economy. Its numbers live beside the other ability constants. The
   turret is the first player-owned thing that deals damage; it goes through
   `Damageable.TakeDamage` with the placing Boarder as the source, so kill credit reaches
   Unbanked through `WaveSpawner`'s existing death path and no wallet code is touched.
   `PlayerTurretControl` is a separate type from `PlayerProtocolAbilities` because that one
   owns windows, input and log lines while this owns a thing in the world with life of its
   own — not to get a file under its cap.

   **Field Deploy is instant**, not a window: the turret's own life is its uptime, and a
   window would be a second clock claiming to mean something it does not. That is what
   `BoarderAbilityState.TrySpendInstant` exists for.

8. **Change** `Game/Enemies/EnemyGatekeeper.Targeting.cs` and its `Attack()` for MAGNET
   HANDS: a Gatekeeper that would chase the Boarder chases their turret instead when both
   are in reach, and a swing that lands on a machine spends the Hatch-damage figure.
6. **Change** `Game/Ship/CrewQuartersStation` to fill its rows from `ShopProtocols()`
   and pad the remainder with Coming Soon copy, so a protocol leaves the stub list by
   entering the catalog rather than by being edited in two places.
7. **Change** `Game/Net/NetPlayerAvatar.Protocol.cs` to clamp through the catalog
   once. The hand-listed Default+Ghost/Foresight case was the general rule spelled out
   for the only protocol that existed.

Rejected: making protocols purchasable rather than claimed. `M70` settled claim at 0
salvage and changing it is a separate economic decision.

## Telegraphing, added 2026-08-23

Every ability was a rule with no body — a multiplier or a targeting-list removal the
player had no way to see. Three things fixed that:

- **`ProtocolLayerView`** puts a screen treatment on whichever window is running, built
  at run time as a post-process volume. **No scene edit, no custom shader, no new
  package**: PPv2 was already installed, `PostProcessLayer` is already on the fight
  cameras, and the HUD canvas is Screen Space Overlay so the readout stays crisp.
  **Only Cloak drains colour** — it is a disengage and the world may go somewhere else.
  Ping, Rush, Barrage and Overcharge land mid-fight, where taking saturation or light
  away would be hostile, so they get an edge wash in their own colour and nothing more.
- **`PingMarker`** is Ping's through-walls render, on the project's own
  `GKB/AlwaysOnTop` shader. Broadcast from the server so the whole crew sees it, which
  is the ability's entire output.
- **The HUD stopped lying.** `CdLabel` returned a bare seconds count for both an open
  window and a cooldown, and the bar hid the ability name whenever a countdown was
  present — so a *running* ability read exactly like a *cooling* one. The bar now takes
  an active flag: live squares keep their name and burn a hot fill, cooling ones drop
  the name and burn dim.

## Deferred, deliberately

Named so they read as decisions rather than omissions:

- **PARALLEL BAYS.** Written onto placed decoys with no reader, exactly how `Stock`
  shipped. Raising the live count needs `DecoyState` to track more than one machine —
  a change to the decoy payload, not to this protocol — so the sheet and the Compendium
  both say *"not yet active"* rather than describing a behaviour the build does not
  have. `MaxLiveUnder` is in the catalog and tested against the day it gets a reader.
- **The turret's life is not shown.** It tints hot while overcharged and that is all;
  there is no health readout on the machine.
- **Every number is a first guess.** Turret life, aura, burn rate and the screen-tint
  values have never been played.
- **Twist slots and second passives.** Growth levers, one at a time, behind the
  pick-rate audit. Not this slice.
- **The turret as bought hardware.** No `PayloadId`, no FABRICATION shelf row, no charges,
  no UTL-H spec. Field Deploy's cooldown is what rations it. The brainstorm's Gunsmith
  doctrine and its hand-fed magazine still wait on an ammo reserve that does not exist.
- **A repair verb for the turret.** Field Rearm's channel was written and then cut with the
  recut; a wounded machine currently has no way back. It is the obvious home for that
  channel when a later slice wants it.


## Open questions

- **Whether the third protocol should be reachable inside the co-op demo or granted at
  its start.** *Unknown.* The demo grants kit rather than teaching shops, which argues
  for granted.
- **`2. active/` is at its cap of 3** (`M82`, `M84`, `M93`). Code for VEIL/FORGE is on
  disk; Compendium no longer says Coming Soon. Play-close and activate wait on a free
  slot (phase 2) or a Zo over-cap call.

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
- Criterion 8: `NetProtocolTests.TwoSeats_DifferentProtocolsKeepTheirOwnAbilities`,
  asserting on the **server** that one seat's cloak reaches it, that its neighbour is
  untouched, and that a turret stands for the seat that deployed it and for no one else.
  Asserted server-side on purpose: gatekeepers are host-only, so an ability that exists
  only on the guest's own machine is a no-op dressed as an ability. That is precisely
  what the first cut of this milestone shipped, and what this criterion caught.

**Playthrough path**: Play, Continue, CREW, claim VEIL, embark, press `H` at a
gatekeeper and watch it lose you; press `G` and watch the room turn. Repeat with
FORGE: press `G` to stand a turret, stand inside its ring and feel the gun speed up, then
press `H` and watch the turret redline — and watch its life fall while it does.

## Usable, or it did not ship

**Driven**: claiming at CREW, and `G` / `H` in a Run. **Shown**: three rows at the
CREW station, a sheet that changes with the claim, and a turret standing in the room that
tints hot while it is overcharged. **Reads back**: the sheet text, the ability bar, and one
log line per ability — Field Deploy's names the machine's life and its aura; Overcharge's
names both multipliers, and a separate line fires when the burn is what killed the turret.

**Where it falls short**: the turret carries no health readout, so a machine about to
die looks like one that just stood up. Called out above rather than left silent.

## Co-op, closed 2026-08-23

The first cut ran every new ability host-side only, which made this milestone's own
justification false: `Cloaked` was written on the local rig, while every host-side
gatekeeper reads `PlayerRegistry.NearestAlive`, which for a guest holds the **avatar's**
`PlayerHealth`. A guest's cloak was invisible to every enemy in the game, and the same
shape applied to Ping and to the turret.

All four now cross the wire on the idiom Barrage already used — an owner request, a
server-authoritative apply, and a ClientRpc for presentation:

- **Cloak** sets the flag on the server's copy of the player, which is the body enemies
  actually target.
- **Ping** provokes where the gatekeepers are, and broadcasts its render to the crew.
- **Field Deploy** stands the machine that shoots on the server; every client gets a
  presentation-only copy that never fires, because two guns for one machine would double
  its output.
- **Overcharge** burns life on the server's machine, on the server's clock. The owner
  reads a replicated `TurretStanding` to refuse without one — asking its own rig would
  refuse every guest while a turret stood in front of them.

## Unblocks

`Docs/Demo - Coop/` beat 4, which has nothing to show if both seats are Default.

## Do not build

A fourth protocol. Protocol purchase costs. Passive swapping UI beyond what the sheet
already does. The `Bulkhead` handler — neither chosen protocol takes it. Twists.
Sightline's in-world radii. LEDGER's world piles. A second turret. Turret upgrades.
Multi-live decoys for PARALLEL BAYS. Tuning any of the first-guess numbers.
**Making the decoy destructible** — `DecoyObject.ApplyDamage` has no caller and decoys are
currently invulnerable lures; that is a real pre-existing defect, it is filed separately,
and it is deliberately not fixed inside this milestone.
