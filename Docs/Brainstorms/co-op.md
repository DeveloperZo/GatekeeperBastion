# Co-op

<!--
Purpose: Everything co-op that is not yet a milestone — the open questions and the filed slices, with the decided ground left in Decisions.md rather than re-argued.
Status: brainstorm
Update trigger: when a co-op slice ships or is filed, or when a decided point is reopened.
Last updated: 2026-08-25 (six co-op milestones since M72 recorded; the avatar is a rigged, animated body, not a capsule)
-->

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
`[M58](../Milestones/3.%20completed/M58-a-build-two-machines-can-run.md)` (player build),
`[M60](../Milestones/3.%20completed/M60-a-join-a-friend-can-use.md)` (Relay join). M50 and M51
were authored ahead of M48/M49 landing on Zo's call (2026-08-12) and each carries a
re-cut-before-start update trigger.

## Where it stands

M45 (completed 2026-08-12) removed the retrofit cost: no static mutable state in `Sim/`,
registry-driven targeting, one gameplay `Instantiate` site, `RunContext`. M48 shipped the
kill loop (2026-08-13). M49 shipped (2026-08-13, Zo playtest): the client is huntable and
downable; down does not end the host Run. M50 shipped (2026-08-13, fixture close): readout
and endings on the client. M51 shipped (2026-08-13, fixture close): client repair,
decoy and world-acting abilities apply once, server-side. M57 shipped 2026-08-15
(get-up: Ship wait, Hatch-bank Prep embark). M58 shipped 2026-08-15 (player build).
M60 shipped 2026-08-15 (Relay join; Zo eyes-on). M72 shipped 2026-08-17 (shared Ship
lobby; host close log `13-24-37` / Relay `KPRMPR`).

**Co-op stopped being polish-only after 2026-08-17, and none of it graduated from this
file** — the surface below has not tracked it. `M92` and `M101` rebuilt the hull into a
three-hatch, two-deck ship; `M95` built the 28-room co-op bastion; `M96` split the save so
a Boarder travels and a realm stays; `M97` put the realm's record on the hull; `M98` split
the seats playing from the seats connected and gave every dead seat one priced way back;
`M100` made a realm reachable while its owner is offline. Those milestones are the record.
What is still open here is presence and the questions below.

The epic's testability story is the `NetcodeIntegrationTest` fixture —
every slice writes its gate against it and closes headless in the integration check —
the single-developer constraint (Zo, 2026-08-12) makes that non-negotiable, so **a slice
whose gate cannot be phrased against the fixture is not ready to graduate.**

## Graduated — these left this file

Link, never restate.


| What                                           | Milestone                                                                        |
| ---------------------------------------------- | -------------------------------------------------------------------------------- |
| In-run client verbs (repair, decoy, abilities) | `[M51](../Milestones/3.%20completed/M51-the-clients-hands.md)`                   |
| Client down wait / get-up                      | `[M57](../Milestones/3.%20completed/M57-the-downed-player-waits-on-the-ship.md)` |
| A join surface a friend can use (Unity Relay)  | `[M60](../Milestones/3.%20completed/M60-a-join-a-friend-can-use.md)`               |
| Shared Ship / party EMBARK                     | `[M72](../Milestones/3.%20completed/M72-the-party-shares-a-ship.md)`               |




## Ideas



### Guest progression

**Graduated 2026-08-14 to** `M65`**; shipped 2026-08-15**
([`3. completed/M65`](../Milestones/3.%20completed/M65-three-players-and-a-fight-that-counts-them.md)).
Settled: salvage splits **equally** among connected players, every player banks to their own
meta, and what a player may bank is capped by their own `DeepestWaveReached`, which rises from
co-op clears as well as solo ones. A player brings their own loadout and inherits nothing. The
principle it encodes: **co-op gets you further, not richer.** Equal split was chosen over kill-
or damage-weighted because this game has jobs that are not shooting, and weighting by
contribution makes doing them cost money.

What is still open and belongs to play data rather than this file: whether a new guest joining
a deep run needs a floor, and whether being carried should raise the banking cap at all.

### Second-player presence beyond a capsule

The avatar stopped being a puppet capsule. `NetPlayerAvatar` drives a real `Animator` off
replicated state — a `Speed` blend against the same thresholds the first-person view model
uses, and a `Dead` bool — so a crewmate walks, runs and fires on camera (`M83`) and looks
downed when they are (`M86`, active; `Anim_Down.fbx` is unsourced so `Down` has no motion
yet). Get-up after down is `M57`, and the priced return is `M98`.

What is still unbuilt of this entry: name labels, a ping wheel, and voice. Voice is rejected
in `M66` and parked below.

**Graduation test:** co-op sessions are actually being played — presence polish before
then is paint on a room with no door.

## Watch

- **The fixture cannot test MPPM, transports or the join surface** — those stay eyes-on.
Keep everything else fixture-first or the single-dev constraint bites back.
- **Shared Ship lobby is M72.** M64's own-Ship-before-Bastion failed the friends path in
play (2026-08-15 Relay dual-box: no co-presence, host EMBARK moved one seat). M57's
additive Ship remains the *downed client* wait only. Party lobby + party-wide scene
loads: `[M72](../Milestones/3.%20completed/M72-the-party-shares-a-ship.md)` (closed
2026-08-17).

