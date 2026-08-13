# Co-op

<!--
Purpose: The multiplayer epic beyond the filed slices — what is deliberately after a full loopback run, and the open design questions the slices must not decide by accident.
Status: brainstorm
Update trigger: when a filed slice (M48–M51) ships or is re-cut, or when the Steam entry graduates.
Last updated: 2026-08-12
-->

## What this covers

Everything co-op that is not yet a milestone. The decided ground lives in
`../Decisions.md` (NGO, host-client, server-authoritative sim, client-auth own-movement,
Steam relay eventually) and is not re-argued here. The filed slices, in execution order:
[`M48`](../Milestones/1.%20planned/M48-the-first-networked-kill.md) (first networked kill +
the in-process test rig),
[`M49`](../Milestones/1.%20planned/M49-the-gatekeepers-see-the-second-player.md) (the client
is huntable and downable),
[`M50`](../Milestones/1.%20planned/M50-the-client-lives-the-run.md) (the run's readout and
endings on the client), and
[`M51`](../Milestones/1.%20planned/M51-the-clients-hands.md) (repair, decoys and abilities
over the wire — completes the loopback run). M50 and M51 were authored ahead of M48/M49
landing on Zo's call (2026-08-12) and each carries a re-cut-before-start update trigger.

## Where it stands

M45 (completed 2026-08-12) removed the retrofit cost: no static mutable state in `Sim/`,
registry-driven targeting, one gameplay `Instantiate` site, `RunContext`. Nothing networked
exists yet. The epic's testability story is fixed by M48: every slice writes its gate
against the `NetcodeIntegrationTest` fixture and closes headless in the integration check —
the single-developer constraint (Zo, 2026-08-12) makes that non-negotiable, so **a slice
whose gate cannot be phrased against the fixture is not ready to graduate.**

## Ideas

### Steam, and a join surface a friend can use

Facepunch transport over Steam relay (the decided path, with its known maintenance risk —
`../Decisions.md` states the cost), a main-menu host/join surface replacing the F10/F11
dev keys, and session lifecycle a non-developer can survive: join-in-Tethered, clean
disconnect, host-quit messaging. Explicitly not host migration (rejected in M45's table).

**Graduation test:** a full co-op run completes over loopback (M51 shipped) — transport
swapping and front-end are worthless before there is a game to join, and the transport
layer is the most swappable piece in the stack.

### Guest progression

What a connected client takes home. M50's run summary banks to the host's meta only;
whether a guest banks salvage, keeps unlocks, or is a pure visitor is untouched design
with economy weight (split banking changes the salvage curve for two players). **Unknown —
needs design**, and M50/M51 are written so nothing forecloses it.

**Graduation test:** M50 shipped and a real two-person run has produced an opinion about
what the second player deserved from it — this one wants play data, not architecture.

### Second-player presence beyond a capsule

The avatar is deliberately a puppet capsule (M48's rejected-alternatives). A visible
character rig, name label, third-person animations, revive-by-teammate, a ping wheel,
voice — each is real design work and none is needed for the loopback epic.

**Graduation test:** the loopback epic shipped and co-op sessions are actually being
played — presence polish before then is paint on a room with no door.

## Watch

- **The fixture cannot test MPPM, transports or the join surface** — those stay eyes-on.
  Keep everything else fixture-first or the single-dev constraint bites back.
- **Ship-scene co-op is deliberately absent everywhere above.** The run is the game; the
  ship between runs is host-local until a slice needs otherwise. If that assumption breaks
  during M50, record it here.
