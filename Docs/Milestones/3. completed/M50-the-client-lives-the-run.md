# M50 — The client lives the run

<!--
Status: completed 2026-08-13
Update trigger: closed
Last updated: 2026-08-13
Closed on: Zo 2026-08-13. Gate is criterion 6 — `NetRunMirrorTests.Mirror_ReplicatesReadout_DeliversEndSummary` Passed in `gkb_verify_report.json` 2026-08-13T15:19:01Z. Criteria 2–5 playthrough cannot close on one terminal Wave (T1 complete then refused; D1 stays locked; hatch invulnerable). N Waves per terminal and door-from-terminal are M52. Hatch-destroyed eyes-on waits on M53. Get-up is M57.
-->

After M49 the client can fight and die, but the run happens on the host's screen: the
client has no salvage readout, no hatch state, no endings. This slice replicates the run's
readout and its three endings, so the client **experiences the run** — prep, assault,
victory or loss — instead of an unexplained fight that stops.

**M48/M49 shapes adopted.** `NetRunMirror.Phase` is `AssaultState` as a byte; host writes
every field from LateUpdate on change (same poll as Wave — not `PhaseChanged`). Client
`GameController.Start` still StartRun+EngageTether, then Update skips `Sim.Tick`, so the
local sim stays Tethered with frozen hatch/salvage/elapsed; fight HUD already runs, and
wave/alive/assault already read the mirror. This slice extends that seam. Client phase
never becomes Lost/Escaped, so endings are a ClientRpc, not a phase follow. `ReturnToShip`
on the client cannot gate on `IsTerminal`. Get-up after client down is not this slice
(Roadmap M57).

## Story

As a second player, I want to see the run's state and its ending on my own screen, so that
I am playing a run with my friend rather than fighting in the dark next to one.

## Why it matters

The fight readout is the run's information flow, and the client currently has almost none
of it (M48 replicates assault/wave/alive only). Leverage tier (`../../Working-Rules.md`):
**#7, information flows** — nothing about the run changes; who can see it does.

Everything here reads state the host already owns: `RunSim.Assault`, `CurrentWaveIndex`,
`RunElapsed`, `Ship.Hatch`, `Salvage.RunSalvage`, and the run report
(`grossEarned`, `repairSpent`, net) the end screens already show
(`GameController.EndScreen.cs:51`).

## Acceptance criteria

1. **`NetRunMirror` carries the fight readout**: assault/wave/alive (M48), plus salvage
   (`NetworkVariable<long>`), hatch HP / hatch max (`<float>`), and elapsed as **whole
   seconds** (`NetworkVariable<int>`, written when the integer changes — one write per
   second, never per frame). Host is the only writer; every field writes on change only.
2. **The client's Bastion HUD is right during an assault**: fight panel, the always-on
   hatch readout, the salvage readout, and the score-line clock (`m:ss`) all read the
   mirror when client-only, and both screens show the same wave, alive count, salvage,
   hatch HP, and elapsed (±1 s) at any moment Zo pauses to compare. Weapon/ammo stays
   local — the client's gun is real.
3. **All three endings reach the client**: waves cleared, hatch destroyed, and host down
   each show the client the existing end screen with the same gross / repair / net /
   banked / deepest-wave numbers as the host, delivered as a one-shot
   `ShowEndScreenClientRpc(EndSummary)` where `EndSummary` is a serializable struct of
   exactly those fields. A one-time screen is an event, not state — an RPC, not a
   NetworkVariable. Host HUD still follows `sim.Phase`; the RPC is ignored there.
   Both logs show `[Net] end kind=<n> gross=<n> repair=<n> net=<n>`.
4. **The session ends as cleanly as the run**: dismissing the end screen (or the host
   quitting to ship) disconnects the client and returns it to its own MainMenu; the host
   still goes to Ship; no orphaned network objects (NGO shutdown log clean); F10/F11
   works again immediately — host, join, run, end, re-join is one loop that works twice
   in a row.
5. **The player action: the client, without touching the host's machine, watches the wave
   counter tick, the hatch HP fall under attack, salvage rise on a kill, and gets an end
   screen naming the ending** — one MPPM session, one person driving.
6. **Headless**: `NetRunMirrorTests` (the M48 fixture) asserts the new fields match on
   both managers after a scripted write, and `EndSummary` delivery for all three ending
   kinds. Write-on-change for elapsed is the LateUpdate `if` (same shape as Wave), not a
   fixture clock. Green in the integration-check report.
7. **Solo is unchanged**: both suites and the harness green at current counts; no `[Net]`
   line in a solo log.

**Gate edit (Zo, 2026-08-13):** Criteria 2–5 playthrough cannot close on the current field
loop. Session `Logs/GatekeeperBastion_2026-08-13_10-29-42.log`: T1 started Wave 1 at
10:30:54, cleared at 10:32:03, then `T1 refused — already complete` at 10:32:56 and
10:33:03; no `[Net] end` (session stopped 10:33:15); F10 at 10:29:53 taints the log.
D1 stays locked (sketch AND of T1+T2; no Open-door verb on the terminal). Hatch HP cannot
fall (`GameController.DamageHatch` no-op while `WaveMode.Terminal`). Hatch-destroyed
eyes-on waits on M53. N Waves per terminal and door-from-terminal are M52. The gate that
closes this file is criterion 6.

## Implementation

1. **Change** `NetRunMirror.cs` — add `NetworkVariable<long> Salvage`, `<float> HatchHp`,
   `<float> HatchMax`, `<int> ElapsedSeconds`. Host LateUpdate writes from `_game.Sim` /
   `_game.Spawner` on change, same poll as Wave. `ServerShowEnd` fires the ClientRpc.
2. **Create** `EndSummary` under `Game/Net/` — `INetworkSerializable` struct: kind
   (`RunEndKind` byte: Cleared / Hatch / Player), grossEarned, repairSpent, net,
   deepest wave, banked. Banked is the host's meta, displayed, not a guest purse.
3. **Change** `GameController.EndScreen.cs` — `EnterTerminalPhase` calls
   `Mirror.ServerShowEnd` when hosting. `ReturnToShip`: client-only shuts the session
   (MainMenu via `OnClientStopped`); host shuts the session then LoadShip. Do not add
   lines to `GameController.cs`.
4. **Change** `HudController` partials — client-only fight readout (salvage, hatch,
   elapsed, `m:ss` on the score line) from the mirror; hatch alert from mirror HP;
   `HasEnd` presents `PushFromSummary` and ShowEnd even though local phase is still
   Tethered. Esc / Confirm / Return on a presented client end call `ReturnToShip`.
5. **Change** `NetSession.cs` — `ShutdownSession`; `OnClientStopped` on a non-host
   loads MainMenu. Session restartable after shutdown (F10/F11 on a fresh Bastion load).
6. **Create** `Assets/Tests/PlayMode/NetRunMirrorTests.cs` — criterion 6.
7. **Change** docs: Roadmap row, Changelog, co-op link.

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
- **Reads back as** identical numbers on both screens, the end screen, and the `[Net] end`
  log line — here.

## Verification

1. Criteria 6, 7 — `gkb_verify_report.json` 2026-08-13T15:19:01Z: `NetRunMirrorTests`
   Passed; PlayMode 25/1 (`CoreLoopPlayModeTests` hatch 275 vs 300 — `DamageHatch` no-op
   in Terminal mode, M52/M53, not this slice); EditMode 321/1 (`BastionDoorApertureTests`
   size.y 3.5 vs 4.5, not this slice).
2. Criteria 2–5 — playthrough confirmation, not the gate. Host log
   `Logs/GatekeeperBastion_2026-08-13_10-29-42.log`: `[Net] mirror wave=1` at 10:30:54,
   `[Net] hit` on the kills, no `[Net] end`. Recut above.

## Do not build

| Not building | Why not |
|---|---|
| Shared ship-scene co-op | Host does not walk a shared Ship. **M57** owns a client-local additive Ship for the down wait only |
| Guest progression / split banking | Open design question, named above — do not decide it by accident in a payload struct |
| Client-side pause semantics | Pause is already non-blocking by decision (`../../Decisions.md`, 2026-08-09); nothing new needed |
| Spectator cam on client death | The wait is M57, not a spectator cam |
| N Waves per terminal / door-from-terminal | M52 |

## Depends on / Unblocks

Depends on M48 (mirror, session, fixture) and M49 (a client the run can end). Unblocks
M51 — spending salvage from the client only makes sense once the client can see salvage.
M57 is a sibling after M49; this slice's end screens must still reach a client who is on
the Ship once M57 exists.
