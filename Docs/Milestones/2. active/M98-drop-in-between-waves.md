# M98 — Drop in between waves

<!--
Status: active
Update trigger: when the between-waves gate moves, when the respawn constants are
retuned (retune the 4-Data respawns block with them), when the hatch open rule
changes, when wave composition stops being computed per wave, when M100 changes
where a realm lives
Last updated: 2026-08-24
-->

A friend joins a realm whenever they like, walks the Ship alone if nobody is home, and
follows the party into a live run **between waves, never during one**. Their hatch door
opens at the same moment, so the party's next wave is composed for the larger party.

**Recut 2026-08-24 (Zo):** the same seam now owns death. Every dead seat — guest, host,
solo — buys its way back through one rule: during a wave while a limited shared pool
lasts, at any wave break without limit, each respawn at a cost that climbs with every
one taken. The free Hatch-bank embark (`M57`) and the flat buy-back (`M90`) retire into
it. Rulings stamped below under *The respawn rule*.

## Story

As someone logging on after my friends have already started, I want to join their run at
the next break rather than wait for it to end, so that showing up late costs nobody
anything. And as any seat that goes down, I want to pay my way back in at the next break
— or immediately, while the party still has respawn charges — so that one death is a
setback, not a spectator seat.

## Why it matters

**The payoff of a realm is that nobody has to be online at the same minute.** Zo's
argument, 2026-08-20: coupling a joinable safehouse with joining the action shortly
after is what motivates everyone to jump on even when they cannot ready up together.
`M96` and `M97` build the realm and the reason to stand in it; without this it is a
waiting room.

**Today there is no path in.** Joins are refused during the embark countdown (`M72`) and
a guest cannot dock the Run at all (`DockRun` has no net route). A seat that arrives
after embark has nowhere to go.

**Between waves is the ruling and it is also the cheap seam** (Zo, 2026-08-20: the join
rule follows how quickly the game can scale for the player, "in between waves not during
waves"). `WaveDirector.NextWave` composes each wave from `playerCount` at the moment it
is called. A seat that joins during Prep is simply present the next time that runs, and
nothing has to rescale live bodies. A seat joining mid-Assault would require rescaling a
wave already on the floor, which is the expensive version and is not built.

**Death was the remaining seat with nowhere to go.** Before the recut a downed guest
could re-enter only at a Hatch-bank window — free at its Prep, flat 450 during its
Assault — and a field wave offered nothing at all, while a host or solo death ended the
run for everyone (`Decisions.md`, *Locked — down is a wait, not a fail*, amended by this
recut). One rule at one seam replaces three windows at two.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation** for the join half;
**#5, rules** for what a death costs and what it can be undone for. The constants are
**#12** (`../../Design/4-Data.md`, respawns block).

## The respawn rule (recut 2026-08-24, all four stamped by Zo)

- **Everyone respawns, including solo.** Host and solo death becomes a DOWN state while
  the offer can be honored; otherwise the run ends Lost exactly as before.
- **Cost escalates per use**: `Respawns.BaseCost × (respawns taken this run + 1)` —
  150 / 300 / 450 / … — debited from the realm's banked pot (the pot is the world's,
  `M96`). The counter is the run's, not the seat's, and resets with the run.
- **The pool is per seat and the pool is not the price**: `PoolPerSeat × seats` charges
  seeded as the run becomes Tethered, plus `PoolPerSeat` per seat that follows in
  mid-run. An in-wave respawn burns a charge AND pays the cost; a break respawn pays
  cost only, without limit. Leaving seats give nothing back.
- **The free window retires.** Hatch-bank Prep embark and the flat 450 buy-back are
  subsumed; the late-arrival FOLLOW stays free, because that seat never died and its
  admission is the join half of this milestone.

## Kill criterion

The pool and the price exist to make a death a decision. If the first co-op session with
three or more deaths (or 2026-09-15, whichever first) logs `respawn seat=` lines and not
one `respawn refused —`, the price never bound and death stopped mattering; if it logs
refusals and not one taken respawn, the price is set wrong or failed Shown. Either way:
retune `BaseCost` / `PoolPerSeat` (both #12) before anything builds on the rule, or
revert the recut.

## Acceptance criteria

Criteria 1–10 are the join half and stand as built (record below). 11+ are the recut.

1. A player enters a realm they are a member of, spawns on the Ship, and can walk it
   and use every station alone. *Whether the owner has to be online for that is `M100`'s
   question, not this one.*
2. A player joins a realm whose party is mid-run and lands on the Ship, not in the
   bastion, with a prompt saying they will join at the next break.
3. Pressing that prompt during the party's **Prep** puts them in the bastion with the
   party.
4. The same prompt during **Assault** is visible but refuses, with a reason, and does not
   queue silently.
5. A seat that joined mid-run is counted by `WaveDirector` on the **next** wave, not the
   current one, and the wave composition log shows the higher count.
6. The joining seat's hatch door opens at the moment they enter the bastion, and the
   open-count log matches the new seat count.
7. No wave already on the floor changes size, health or composition when a seat joins.
8. A seat leaving mid-run is dropped from the count on the next wave and their door
   seals at the same boundary.
9. Solo is unchanged: a single-seat run never sees any of this and its wave composition
   is byte-identical to before.
10. Joins during the embark countdown are still refused, exactly as `M72` has them.
11. A downed guest, during any Assault with pool stock left, presses `RESPAWN (<cost>)`
    on the Ship wait surface and rejoins alive at the Hatch inside the same wave; the
    stock falls by one and `[Demo] respawn seat=<n> cost=<c>` logs host-side.
12. The same press with the stock at 0 refuses named (`no charges until the break`) and
    debits nothing; at the next Prep the same button respawns them, paid, no charge
    burned.
13. Each respawn costs `Respawns.CostFor(taken)` from the realm's pot; a pot below the
    cost refuses named (`cannot afford`) and debits nothing on any machine.
14. The pool seeds at `PoolPerSeat × PlayerCount` as the run becomes Tethered and grows
    by `PoolPerSeat` when a seat is admitted mid-run; seed and growth are named log
    lines.
15. A host or solo player at 0 HP — while a respawn is available now, or in co-op while
    any other seat still fights — stays Tethered behind the DOWN overlay, which names
    the cost or the refuse reason, and Confirm respawns them at the spawn exactly as a
    guest respawn does, logging `[Demo] respawn seat=0 cost=<c>`.
16. A solo death with no respawn available (no charges mid-wave, or the pot cannot pay)
    ends the run Lost exactly as before the recut, with `no respawn — <reason>` logged.
    A fresh realm's pot is 0, so its first death is byte-identical to today.
17. Death still wipes the dying seat's Unbanked, respawned or not; a respawn is pure
    cost (`M90` criterion 6 carried forward).
18. `HatchBank.BuyBackCost`, `IsBuyBackWindow`, `IsFreeEmbarkWindow` and
    `BuyBackRefuse` no longer exist; every dead-seat re-entry goes through the one
    respawn rule.
19. Wave composition and the wallet split are unchanged by a death or a respawn; the
    roster still never moves outside a Prep boundary.

## Implementation

Join half (steps 1–6) as built — see the record below. The recut:

7. **Create** `Assets/Scripts/Sim/Run/Respawns.cs` — `PoolPerSeat = 1`,
   `BaseCost = 150L`, `CostFor(int taken)`, and
   `Refuse(RunPhase, AssaultState, int stock, long cost, long pot)` returning null or
   the named reason (`no run` / `no charges until the break` / `cannot afford`). The
   rule lives sim-side so EditMode sees it, like `HatchBank`.
8. **Create** `Assets/Scripts/Sim/Run/RunSim.Respawns.cs` (partial) — `RespawnStock`,
   `RespawnsTaken`, `NextRespawnCost`, `RespawnRefuse()`, `TryRespawn(out long cost)`
   (adjudicates, debits `_meta.SpendSalvage`, burns stock only in Assault, counts), and
   the seed/grow internals. **Change** `RunSim.cs` — `EngageTether` seeds;
   `SetPlayerCount` grows on a mid-run increase. Growth on increase only: a leaving
   seat refunds nothing, or leaving-and-refollowing farms charges.
9. **Change** `Assets/Scripts/Sim/Run/HatchBank.cs` — delete the four buy-back members
   (criterion 18). Deleted rather than adapted: the window rule is the thing being
   retired, and a stub would invite callers back.
10. **Change** `GameController.BuyBack.cs` → rename `GameController.Respawn.cs`
    (keep the .meta guid) — `TryNetRespawn(NetPlayerAvatar from, out long cost)`:
    host-only guards (not client, not the host's own seat, `WaitingOnShip` true,
    `AwaitingFollow` false — a late arrival follows for free), then `Sim.TryRespawn`.
11. **Change** `NetPlayerAvatar.BuyBack.cs` → rename `NetPlayerAvatar.Respawn.cs` —
    `RequestRespawn()` / `RequestRespawnServerRpc` → `TryNetRespawn` → `ApplyRejoin()`.
    **Change** `NetPlayerAvatar.cs` — delete `RequestEmbark`, `TryAcceptEmbark`,
    `RequestEmbarkServerRpc`; the free path is gone (criterion 18).
12. **Create** `GameController.Down.cs` (partial) — `TryEnterLocalDown()` called from a
    recut `PlayerDied()`: enter DOWN when `Sim.RespawnRefuse() == null` or any remote
    seat still fights (their banking can refund the pot; the hatch is the backstop);
    else `Sim.NotifyPlayerDown()` with `no respawn — <reason>` logged. Entering wipes
    slot 0's Unbanked. `TickLocalDown()` reads Confirm → `TryLocalRespawn()` →
    `Sim.TryRespawn` + rig `ResetToSpawn()` + `HealToFull()` (ResetToSpawn alone leaves
    `MovementLocked` set).
13. **Change** `HudController.Fight.cs` `PushVignette` — the existing DOWN vignette
    text carries the offer host-side: `YOU ARE DOWN — ENTER: RESPAWN (<cost>)` or the
    refuse reason. Clients keep the plain text; their surface is the Ship wait.
14. **Change** `NetRunMirror.cs` — `NetworkVariable<int> RespawnStock` and
    `RespawnsTaken`, synced in the existing LateUpdate block, so guest labels can price
    the button without asking the host.
15. **Change** `MapStation.cs` and `ShipController.cs` wait surfaces — FOLLOW branch
    unchanged; the free-embark branch deleted; the label is `RESPAWN (<cost>)` with
    named disabled states (`no charges until the break`, `can't afford`), wired to
    `RequestRespawn`.
16. **Change** `Tools/docgen/Program.cs` — drop the buy-back row from `EmitHatchBank`,
    add a `respawns` block; regenerate the 4-Data blocks.

Rejected: queueing a mid-Assault join to fire automatically at the next Prep (join
half, stands). Rejected: auto-respawn on a timer — a purchase is a decision, and the
button is the decision point. Rejected: per-seat escalation — the pot is shared, so the
counter is too; a repeat-dier raises the price for everyone, which is the social
pressure working as intended. Rejected: an ABANDON control on the DOWN overlay — the
hatch is the backstop and the pause menu still works; revisit on the first stuck log.

## Verification

- Criteria 3, 4, 5, 7, 8, 10: PlayMode net tests against `NetcodeIntegrationTest`
  (`NetFollowSeatTests`, `RunSeatsTests`) — as built.
- Criteria 11–13, 17, 18 (guest side): `NetDownWaitTests` recut — down a guest during a
  field Assault with stock, respawn succeeds and debits `CostFor(0)`; drain the stock,
  the same press refuses named and debits nothing; the next Prep press succeeds without
  burning stock; a second respawn costs `CostFor(1)`; a pot below cost refuses named.
- Criteria 13, 14 (sim side): `RespawnsTests` (EditMode, offline harness) — escalation,
  refuse matrix, seeding at `EngageTether`, growth on `SetPlayerCount` increase only,
  Assault-only stock burn, pot debit and refusal without debit.
- Criteria 15, 16: PlayMode solo fixture — kill the player with a funded pot: phase
  stays Tethered and `TryLocalRespawn` restores them, pot falls; kill with an empty
  pot: phase goes Lost with `no respawn — cannot afford`, as today.
- Criterion 9: run the offline wave harness at `playerCount == 1` and diff against the
  recorded baseline. Any difference fails. (Composition only — the death rule change is
  criterion 16's.)
- Criteria 1, 2, 6: playthrough across two editor instances, as before.

**Playthrough path**: host enters a realm and embarks. Guest joins, lands on the Ship,
follows at the break as a second door opens. Mid-wave the guest goes down, presses
`RESPAWN (150)` on the NAV MAP and is back at the Hatch inside the wave; goes down
again with the pool dry, is refused named, and returns at the break for 300. Host goes
down with the pot funded, reads the cost on the DOWN overlay, presses ENTER, and is
back at the spawn.

## Built, 2026-08-22

The distinction the code did not have is now a type: `RunSeats`, a host-only roster of the
seats **playing** as against the seats **connected**. It opens as the run becomes Tethered
seeded with everyone who embarked, and closes when the run ends or the Ship loads for real.
A seat that connects mid-run is not in it until it follows; a seat that drops is held in it
until the next Prep. Three consumers read it while a run is live — the wave-composition
count, the wallet split behind it, and the hatch doors — and none of them before one, so the
lobby is untouched and solo still answers one seat.

`WaveDirector.NextWave` was not touched, as implementation step 5 required.

**Reused rather than rebuilt**: the downed-Boarder wait already puts a seat on the Ship over
a live Bastion. `AwaitingFollow` distinguishes an arrival from a casualty — that one buys
back at a Hatch-bank window, this one only waits for the break — and `TryAcceptFollow` is
`TryAcceptEmbark` with the Hatch-bank condition dropped, since a seat that never embarked has
nothing to buy back. *(The buy-back and embark paths named here were retired by the
2026-08-24 recut; the reuse stands.)*

**Found while building, fixed here**: a `NetworkVariable`'s initial value raises no
`OnValueChanged` on the owner that has just spawned, and the Bastion scene can still be
loading when one would have fired. The owner enters the wait from its own tick instead,
scoped to the late arrival so the buy-back path keeps being driven purely by the callback.

**Verified in Unity, 2026-08-22.** EditMode 669 passed / 11 failed, PlayMode 70 / 1 — the
twelve failures are the recorded pre-existing set, name for name. All thirteen `RunSeatsTests`
and both `NetFollowSeatTests` pass.

**Caught by that run, and fixed**: the first build regressed two `NetDownWaitTests`. The host
owns its own avatar, and the mid-run marking had no `IsOwner` guard — `TryMarkWaiting` excludes
it for exactly that reason — so the host was put on the Ship at its own embark, which also
stopped buy-back refusing a host attempt. Underneath it, a *live but empty* roster was being
read as "nobody is admitted", when it means solo or a run whose seats were never enumerated.
Consumers now ask `HasRoster` — live **and** naming seats — so solo and the lobby fall back to
exactly the old behaviour. Both causes have a test.

**Compiled offline as well.** The whole `GatekeepersProtocol` assembly — 165 sources, every change
here among them — and both new fixtures compile clean against Unity's own `UnityEngine`
modules and the `Unity.Netcode.Runtime` the project builds with, via a `dotnet` replica of the
assembly. That is a real compile of the real source set, not a subset smoke test.

**Owed.** Criteria 1, 2 and 6 are eyes-on across two editor instances and nobody has
looked — the fixtures prove the seat rule and the doors, not what a late arrival sees. Criterion
9's offline wave-harness diff against the recorded baseline has not been run: `RunSeatsTests`
argues solo is unchanged from the count's side, which is weaker than the diff the milestone
asks for. `mono` is not on this machine's PATH, so `Tools/verify/run.sh` ran its lints only.

## Recut built, 2026-08-24

Implemented as specified, steps 7–16, commits `a3aa959` and `b263880` on the doc recut
`8d9f81a`. Two designs moved while building, both recorded in the files:

- **PlayerDied carries the dying body.** `PlayerRegistry.Local` requires an FPS rig by
  design (puppets must not steal it), which returned null under the test fixture — and
  round-tripping the registry was never necessary, because the dying `PlayerHealth` is
  the caller. The bare `PlayerDied()` overload still ends the run, which is exactly what
  the legacy dev/test callers mean by it.
- **ESC would have bought a respawn.** `Controls.Sample()` ORs bindings without context,
  and ESC is bound to Confirm (end screens) and FreeCursor (pause) both. The DOWN tick
  accepts Confirm only when the press is not also FreeCursor.

**Verified.** EditMode through Unity: 767 passed / 14 failed — the recorded pre-existing
set plus `BastionCoopSceneTests` and `CompendiumFormTests`, both belonging to concurrent
in-flight work on other milestones, none in this area; all eight `RespawnsTests` and the
recut fixtures pass. PlayMode through Unity: 79 / 2 — the recorded `NetVerbTests`
repair-timing flake, plus `Down_HostFundedPot_HoldsRunOpenAndRespawns` failing on the
registry lookup that the second commit replaced. Offline, the full source set (Sim,
Game, EditMode tests, PlayMode tests) compiles clean with the fix in, and the offline
EditMode replication holds at the recorded failure set. `docgen --check` matches the
recut 4-Data blocks — which also re-derives the solo wave and hatch-bank composition
tables from the live `WaveDirector` against the committed baseline, criterion 9's diff
in substance.

**The owed PlayMode leg landed the same afternoon** (run of 2026-08-24 20:24 UTC, over
the tree carrying both commits): PlayMode 80 passed / 1 failed — the recorded
`NetVerbTests` repair-timing flake alone. `Down_HostFundedPot_HoldsRunOpenAndRespawns`
and every other recut fixture pass in Unity. EditMode 773 / 16: the recorded set plus
four failures belonging to concurrent in-flight work on other milestones
(`BastionCoopSceneTests`, `CompendiumFormTests`, `RealmShareServiceTests` ×2), none in
this area.

**Owed.** Criteria 1, 2, 6 eyes-on (join half) as recorded above; the recut's own
eyes-on (the DOWN overlay text, the priced button labels) rides the same two-editor
session. The kill criterion reads the first co-op session's log.

## Usable, or it did not ship

**Driven**: joining a realm, pressing follow, pressing `RESPAWN (<cost>)` on the Ship
wait surface, pressing ENTER on the DOWN overlay. **Shown**: the ship you land on, the
prompt, a door opening as you arrive, the priced button, the DOWN overlay naming the
cost or the reason. **Reads back**: the refusal reasons, the open-count log, the next
wave's composition line, `respawn seat=<n> cost=<c>`, the pool seed and growth lines,
and the realm pot falling by the cost.

## Supporting work

Depends on `M96` for realms to join and `M92` for hatch doors to open. `M97` is
unrelated and can land either side.

`M100` owns reaching a realm whose owner is not running the game. This milestone was
written on 2026-08-20 as though that already worked; it never has, and folding it in here
would have made a join-timing milestone carry a storage change. The two are independent:
this one is about *when* a seat may enter a live run, `M100` is about *whether the realm
is there to enter*.

The recut amends `Decisions.md` (*Locked — down is a wait, not a fail* and *Co-op
buy-back*) and retunes the 4-Data hatch-bank block; both edits land with it. `M86`'s
downed-crewmate presentation reads the same replicated Hp and is untouched.

## Open questions

- Whether the arriving seat should be placed at the party or at a door is **unknown,
  needs play data**. Stamped: at the party, since nobody owns a door.
- Whether a mid-run join should cost the realm anything is not specced. Stamped: no —
  and the recut keeps it: FOLLOW is free, respawn is not, because one seat died and the
  other did not.
- Whether solo respawns cheapen the demo's designed early losses is **unknown, needs
  play data**. The fresh-realm pot is 0, so the first-loss beat survives structurally;
  watch the zone logs for `respawn seat=0` before the primary opens.
- Whether a down host waiting out a wave needs an abandon affordance is **unknown,
  needs play data**. Stamped: not built; first stuck log reopens it.

## Do not build

Mid-Assault *joining* (the pool is a death rule; an arriving seat still waits for the
break). Automatic queued entry. Auto-respawn on a timer. Rescaling a live wave. Host
migration. Spectating while waiting. Refunds on unspent pool charges.
