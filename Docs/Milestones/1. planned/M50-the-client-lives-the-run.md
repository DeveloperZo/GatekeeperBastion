# M50 — The client lives the run

<!--
Status: later
Update trigger: re-cut against M48's landed mirror/session shapes before starting (Zo,
2026-08-12: author now, refine through implementation). Also when the down rule or an
ending changes.
Last updated: 2026-08-12
-->

After M49 the client can fight and die, but the run happens on the host's screen: the
client has no salvage readout, no hatch state, no endings. This slice replicates the run's
readout and its three endings, so the client **experiences the run** — prep, assault,
victory or loss — instead of an unexplained fight that stops.

## Story

As a second player, I want to see the run's state and its ending on my own screen, so that
I am playing a run with my friend rather than fighting in the dark next to one.

## Why it matters

The fight readout is the run's information flow, and the client currently has almost none
of it (M48 replicates phase/wave/alive only). Leverage tier (`../../Working-Rules.md`):
**#7, information flows** — nothing about the run changes; who can see it does.

Everything here reads state the host already owns: `RunSim.Phase` / `PhaseChanged`,
`RunElapsed`, the hatch pool in `ShipSystems`, the salvage values the fight HUD prints,
and the run report (`grossEarned`, `repairSpent`, net) the end screens already show
(`GameController.EndScreen.cs:51`).

## Acceptance criteria

1. **`NetRunMirror` carries the fight readout**: phase, wave, alive (M48), plus salvage,
   hatch HP / hatch max, and elapsed as **whole seconds** (`NetworkVariable<int>`, written
   when the integer changes — one write per second, never per frame). Host is the only
   writer; every field writes on change only.
2. **The client's Bastion HUD is right during an assault**: fight panel, the M36 always-on
   hatch readout, and the salvage readout all read the mirror when client-only, and both
   screens show the same wave, alive count, salvage, hatch HP, and mm:ss (±1 s) at any
   moment Zo pauses to compare. Weapon/ammo stays local — the client's gun is real.
3. **All three endings reach the client**: waves cleared, hatch destroyed, and host down
   each show the client the existing end screen with the same gross / repair / net numbers
   as the host, delivered as a one-shot `ShowEndScreenClientRpc(EndSummary)` where
   `EndSummary` is a serializable struct of exactly the fields the end screens print. A
   one-time screen is an event, not state — an RPC, not a NetworkVariable.
4. **The session ends as cleanly as the run**: dismissing the end screen (or the host
   quitting to ship) disconnects the client and returns it to its own MainMenu; no
   orphaned network objects (NGO shutdown log clean); F10/F11 works again immediately —
   host, join, run, end, re-join is one loop that works twice in a row.
5. **The player action: the client, without touching the host's machine, watches the wave
   counter tick, the hatch HP fall under attack, salvage rise on a kill, and gets an end
   screen naming the ending** — one MPPM session, one person driving.
6. **Headless**: `NetRunMirrorTests` (the M48 fixture) asserts mirror equality across a
   scripted phase sequence, the once-per-second elapsed write rate, and `EndSummary`
   delivery for all three ending kinds. Green in the integration-check report.
7. **Solo is unchanged**: both suites and the harness green at current counts; no `[Net]`
   line in a solo log.

## Implementation

1. **Change** `NetRunMirror.cs` — add `NetworkVariable<long> Salvage`,
   `<float> HatchHp`, `<float> HatchMax`, `<int> ElapsedSeconds`. Host writes from
   `_game.Run.Sim` on change (`PhaseChanged` event for phase; polled compare for the
   rest, same pattern as M48's wave field).
2. **Create** `EndSummary` (in `NetSession.cs` or its own file under `Game/Net/`) —
   `INetworkSerializable` struct: ending kind (byte), grossEarned, repairSpent, net,
   deepest wave. Fields mirror what `GameController.EndScreen.cs` logs at :55.
3. **Change** `GameController.EndScreen.cs` — at the single place each ending resolves,
   if hosting: `ShowEndScreenClientRpc(summary)` through the mirror (it is the one
   NetworkBehaviour that always exists in a session).
4. **Change** `HudController.EndScreens.cs` / `HudController.Panels.cs` — client-only:
   end screens render from the received `EndSummary`; hatch and salvage readouts source
   from the mirror (extends the M48 seam, same branch shape).
5. **Change** `NetSession.cs` — clean shutdown: host quit or end-screen dismissal calls
   `NetworkManager.Shutdown()`, client returns to MainMenu via the existing `SceneFlow`
   path; make the session restartable in-process (criterion 4's join-twice loop).
6. **Create** `Assets/Tests/PlayMode/NetRunMirrorTests.cs` — criterion 6.
7. **Change** docs: Roadmap row, Changelog.

Rejected: replicating `RunSim` itself to the client — the client would then *have* a sim
to fork, which criterion 4 of M48 exists to prevent; the client gets a readout, never the
machine. Rejected: per-frame elapsed replication — 60 writes/s for a clock; whole seconds
is what the HUD prints anyway. Rejected: NGO server-time arithmetic for elapsed — correct
but adds a clock-sync concept for zero visible gain over 1 Hz writes.

**Open, labelled:** what a connected client earns from the run — the summary banks to the
host's meta only; guest progression is **unknown — needs design**, owned by the co-op
brainstorm, and nothing here forecloses it.

## Supporting work

- **Driven by** being in a session when the run runs — no new input.
- **Shown by** the HUD itself filling in — the readout is the surface.
- **Reads back as** identical numbers on both screens and the end screen — here.

## Verification

1. Criteria 6, 7 — `gkb_verify_request.txt`; integration-check report green at current
   counts plus `NetRunMirrorTests`; harness and lint-arch green.
2. Criteria 2–5 — playthrough path: MPPM one virtual player. Host F10, board, fight one
   wave while the client watches its own HUD; force each ending across three short runs
   (clear at wave 1 via dev jump; F4-free hatch fall; host death with invuln off). Compare
   screens at a pause for criterion 2; read both logs for the `EndSummary` line; dismiss,
   re-join, run again for criterion 4.

## Do not build

| Not building | Why not |
|---|---|
| Ship-scene co-op / client on the ship | The run is the game; the ship stays host-local until a slice needs otherwise |
| Guest progression / split banking | Open design question, named above — do not decide it by accident in a payload struct |
| Client-side pause semantics | Pause is already non-blocking by decision (`../../Decisions.md`, 2026-08-09); nothing new needed |
| Spectator cam on client death | The M49 down flow already covers the wait; polish later |

## Depends on / Unblocks

Depends on M48 (mirror, session, fixture) and M49 (a client the run can end). Unblocks
M51 — spending salvage from the client only makes sense once the client can see salvage.
