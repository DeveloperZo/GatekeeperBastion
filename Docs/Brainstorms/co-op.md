# Co-op

<!--
Purpose: The multiplayer epic beyond the filed slices — what is deliberately after a full loopback run, and the open design questions the slices must not decide by accident.
Status: brainstorm
Update trigger: when a filed slice (M50–M51 / M57–M60) ships or is re-cut.
Last updated: 2026-08-13
-->

## What this covers

Everything co-op that is not yet a milestone. The decided ground lives in
`../Decisions.md` (NGO, host-client, server-authoritative sim, client-auth own-movement,
Steam relay eventually) and is not re-argued here. The filed slices, in execution order:
[`M48`](../Milestones/3.%20completed/M48-the-first-networked-kill.md) (first networked kill +
the in-process test rig; shipped 2026-08-13),
[`M49`](../Milestones/3.%20completed/M49-the-gatekeepers-see-the-second-player.md) (the client
is huntable and downable; shipped 2026-08-13, Zo playtest),
[`M50`](../Milestones/3.%20completed/M50-the-client-lives-the-run.md) (the run's readout and
endings on the client; shipped 2026-08-13, fixture close),
[`M51`](../Milestones/3.%20completed/M51-the-clients-hands.md) (repair, decoys and abilities
over the wire — loopback run complete; shipped 2026-08-13, fixture close),
[`M57`](../Milestones/1.%20planned/M57-the-downed-player-waits-on-the-ship.md) (get-up),
[`M58`](../Milestones/1.%20planned/M58-a-build-two-machines-can-run.md) (player build),
[`M60`](../Milestones/1.%20planned/M60-a-join-a-friend-can-use.md) (Steam join). M50 and M51
were authored ahead of M48/M49 landing on Zo's call (2026-08-12) and each carries a
re-cut-before-start update trigger.

## Where it stands

M45 (completed 2026-08-12) removed the retrofit cost: no static mutable state in `Sim/`,
registry-driven targeting, one gameplay `Instantiate` site, `RunContext`. M48 shipped the
kill loop (2026-08-13). M49 shipped (2026-08-13, Zo playtest): the client is huntable and
downable; down does not end the host Run. M50 shipped (2026-08-13, fixture close): readout
and endings on the client. M51 shipped (2026-08-13, fixture close): client repair,
decoy and world-acting abilities apply once, server-side. M57 planned
(get-up). M58 planned (player build). M60 planned (Steam join). The epic's testability
story is the `NetcodeIntegrationTest` fixture — every slice writes its gate against it and
closes headless in the integration check — the single-developer constraint (Zo, 2026-08-12)
makes that non-negotiable, so **a slice whose gate cannot be phrased against the fixture is
not ready to graduate.**

## Graduated — these left this file

Link, never restate.

| What | Milestone |
|---|---|
| In-run client verbs (repair, decoy, abilities) | [`M51`](../Milestones/3.%20completed/M51-the-clients-hands.md) |
| Client down wait / get-up | [`M57`](../Milestones/1.%20planned/M57-the-downed-player-waits-on-the-ship.md) |
| Steam, and a join surface a friend can use | [`M60`](../Milestones/1.%20planned/M60-a-join-a-friend-can-use.md) |

## Ideas

### Guest progression

What a connected client takes home. M50's run summary banks to the host's meta only;
whether a guest banks salvage, keeps unlocks, or is a pure visitor is untouched design
with economy weight (split banking changes the salvage curve for two players). **Unknown —
needs design**, and M50/M51 are written so nothing forecloses it.

**Graduation test:** M50 shipped and a real two-person run has produced an opinion about
what the second player deserved from it — this one wants play data, not architecture.

### Second-player presence beyond a capsule

The avatar is deliberately a puppet capsule (M48's rejected-alternatives). A visible
character rig, name label, third-person animations, a ping wheel, voice — each is real
design work and none is needed for the loopback epic. Get-up after down is
[`M57`](../Milestones/1.%20planned/M57-the-downed-player-waits-on-the-ship.md).

**Graduation test:** co-op sessions are actually being played — presence polish before
then is paint on a room with no door.

## Watch

- **The fixture cannot test MPPM, transports or the join surface** — those stay eyes-on.
  Keep everything else fixture-first or the single-dev constraint bites back.
- **Shared ship-scene co-op is still absent.** M57 owns a client-local additive Ship for
  the down wait only; the host does not walk a shared ship. If that assumption breaks
  during a session, record it here.
