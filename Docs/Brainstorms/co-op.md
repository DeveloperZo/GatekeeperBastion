# Co-op

## What this covers

Everything co-op that is not yet a milestone. The decided ground lives in
`../Decisions.md` (NGO, host-client, server-authoritative sim, client-auth own-movement,
Unity Relay on UnityTransport) and is not re-argued here. The filed slices, in execution order:
`[M48](../Milestones/3.%20completed/M48-the-first-networked-kill.md)` (first networked kill +
the in-process test rig; shipped 2026-08-13),
`[M49](../Milestones/3.%20completed/M49-the-gatekeepers-see-the-second-player.md)` (the client
is huntable and downable; shipped 2026-08-13, Zo playtest),
`[M50](../Milestones/3.%20completed/M50-the-client-lives-the-run.md)` (the run's readout and
endings on the client; shipped 2026-08-13, fixture close),
`[M51](../Milestones/3.%20completed/M51-the-clients-hands.md)` (repair, decoys and abilities
over the wire — loopback run complete; shipped 2026-08-13, fixture close),
`[M57](../Milestones/3.%20completed/M57-the-downed-player-waits-on-the-ship.md)` (get-up; shipped 2026-08-15),
`[M58](../Milestones/1.%20planned/M58-a-build-two-machines-can-run.md)` (player build),
`[M60](../Milestones/1.%20planned/M60-a-join-a-friend-can-use.md)` (Relay join). M50 and M51
were authored ahead of M48/M49 landing on Zo's call (2026-08-12) and each carries a
re-cut-before-start update trigger.

## Where it stands

M45 (completed 2026-08-12) removed the retrofit cost: no static mutable state in `Sim/`,
registry-driven targeting, one gameplay `Instantiate` site, `RunContext`. M48 shipped the
kill loop (2026-08-13). M49 shipped (2026-08-13, Zo playtest): the client is huntable and
downable; down does not end the host Run. M50 shipped (2026-08-13, fixture close): readout
and endings on the client. M51 shipped (2026-08-13, fixture close): client repair,
decoy and world-acting abilities apply once, server-side. M57 shipped 2026-08-15
(get-up: Ship wait, Hatch-bank Prep embark). M58 planned (player build). M60 planned (Relay join). The epic's testability
story is the `NetcodeIntegrationTest` fixture — every slice writes its gate against it and
closes headless in the integration check — the single-developer constraint (Zo, 2026-08-12)
makes that non-negotiable, so **a slice whose gate cannot be phrased against the fixture is
not ready to graduate.**

## Graduated — these left this file

Link, never restate.


| What                                           | Milestone                                                                        |
| ---------------------------------------------- | -------------------------------------------------------------------------------- |
| In-run client verbs (repair, decoy, abilities) | `[M51](../Milestones/3.%20completed/M51-the-clients-hands.md)`                   |
| Client down wait / get-up                      | `[M57](../Milestones/3.%20completed/M57-the-downed-player-waits-on-the-ship.md)` |
| A join surface a friend can use (Unity Relay)  | `[M60](../Milestones/1.%20planned/M60-a-join-a-friend-can-use.md)`               |




## Ideas



### Guest progression

**Graduated 2026-08-14 to** `M65`**.** Settled: salvage splits **equally** among connected
players, every player banks to their own meta, and what a player may bank is capped by their
own `DeepestWaveReached`, which rises from co-op clears as well as solo ones. A player brings
their own loadout and inherits nothing. The principle it encodes: **co-op gets you further,
not richer.** Equal split was chosen over kill- or damage-weighted because this game has jobs
that are not shooting, and weighting by contribution makes doing them cost money.

What is still open and belongs to play data rather than this file: whether a new guest joining
a deep run needs a floor, and whether being carried should raise the banking cap at all.

### Second-player presence beyond a capsule

The avatar is deliberately a puppet capsule (M48's rejected-alternatives). A visible
character rig, name label, third-person animations, a ping wheel, voice — each is real
design work and none is needed for the loopback epic. Get-up after down is
`[M57](../Milestones/3.%20completed/M57-the-downed-player-waits-on-the-ship.md)`.

**Graduation test:** co-op sessions are actually being played — presence polish before
then is paint on a room with no door.

## Watch

- **The fixture cannot test MPPM, transports or the join surface** — those stay eyes-on.
Keep everything else fixture-first or the single-dev constraint bites back.
- **Shared ship-scene co-op is still absent.** M57 owns a client-local additive Ship for
the down wait only; the host does not walk a shared ship. If that assumption breaks
during a session, record it here.

