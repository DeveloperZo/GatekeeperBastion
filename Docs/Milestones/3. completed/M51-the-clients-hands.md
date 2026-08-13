# M51 — The client's hands

<!--
Status: completed 2026-08-13
Update trigger: closed
Last updated: 2026-08-13
Closed on: Zo 2026-08-13. Gate is criterion 7 — all five `NetVerbTests` Passed in
`gkb_verify_report.json` 2026-08-13T18:09:48Z (PlayMode 31/0). Criterion 6 playthrough
is confirmation, not the gate: Terminal Waves leave the hatch invulnerable, so
repair-under-attack waits on M53. Criterion 8 EditMode still carries
`BastionDoorApertureTests.TryGetDoorAperture_NorthDoor_AtHalfDepth` (expected 4.5, was
3.5) — same miss as the M50 close, not this slice.
-->

After M50 the client sees the run and still cannot spend, repair, or place. This
slice routes the client's in-run verbs — repair, decoy, world-acting abilities —
through the server, with the same single-application invariant M48 established for
damage. Hatch-under-attack and hatch-fall stay on M53: Terminal-mode Waves leave
the hatch invulnerable (`GameController.DamageHatch`).

## Story

As a second player, I want to repair the hatch, place decoys and use my abilities, so that
I carry half the run instead of half the shooting.

## Why it matters

Co-op in this genre is division of labour — one player holds the door while the other
repairs it — and right now every non-gun verb is host-only. Leverage tier
(`../../Working-Rules.md`): **#10, structure** — verbs move across the wire; no number
changes, and repair cost/rate stay where `../../Design/4-Data.md` puts them.

**In-run verbs only.** The client brings their own loadout from their own local save;
the armory, fabrication and the ship stay host-local. One wallet: the host sim's
`BankedSalvage` is the only purse, so every client spend is a server-validated request.

## Acceptance criteria

1. **Repair**: holding interact on the repair point as a client repairs the host hatch.
   Client sends `SetRepairing` on hold start/stop; the **server** runs the channel with
   its own dt, validates range against `HatchStation.ClosestSurfacePoint` (Interactor
   range 3.2 m), and spends host `BankedSalvage` at `ShipSystems.RepairSalvagePerHp`.
   The rig gate is the **requester** (client only sends while local `Configure` says
   equipped). The server does not require the host to have the rig equipped
   (`RepairHatchFromNet`). Hatch HP and banked move on both screens via the M50 mirror
   plus `Banked`. Releasing the key stops the channel within one tick.
2. **Decoy**: `3` selects the drone slot when the local bay has a payload. Fire is the
   existing two-press ghost; confirm sends `PlaceDecoy(position)`. The server runs
   `DecoyBay.TryPlace` on the **host** bay (host charges). The spawned decoy is a
   `NetworkObject` on `DefaultNetworkPrefabs`, visible on both screens. Gatekeepers
   divert via the server's `DecoyBay.Live`. Connecting `SlotDrone` and ensuring
   `DecoyBay` exists on the rig is in scope — `DecoyPlacement.OnFirePressed` had no
   callers and `ApplyMetaEffects` logged a missing bay.
3. **Abilities**: BR-12 concussive and MR-35 pin act on the server's live ledger.
   Concussive sends origin, direction, metres, range; the server evaluates the cone
   and `Shove`s. Pin sends `NetworkObjectId` (not `GetInstanceID`) and duration; the
   server `Pin`s that body. Mk1 surge and PRI-C railshot stay local (owner gun).
   Cooldowns run client-side.
4. **Denial reads back**: empty host `Banked` heals nothing. The client prompt reads
   mirrored `HatchHp` / `HatchMax` / `Banked` (not the frozen local sim, not the
   client's own `Meta.BankedSalvage`) and shows the existing "no salvage to repair"
   line. No optimistic local spend.
5. **Single-application**: no repair tick, decoy charge or pin/shove applies on the
   client instance. `NetVerbTests` proves each verb applies once, server-side.
6. **Playthrough (confirmation, not the gate)**: in one MPPM run the client repairs a
   **damaged** hatch (fixture or host `Hatch.Damage` — Terminal Waves do not damage
   it), then places a decoy, then fires the cone. "Under attack" waits on M53.
7. **Headless**: `NetVerbTests` (M48 fixture, `EnableSceneManagement = false`) asserts
   criteria 1–5. Green in the integration-check report.
8. **Solo is unchanged**: both suites and the harness green at current counts; no `[Net]`
   line in a solo log.

## Implementation

Order: repair, then decoy, then abilities.

1. **Create** `Assets/Scripts/Game/Net/NetVerbs.cs` on the avatar prefab (avatar file is
   past ~150 lines). RPC surface: `SetRepairingServerRpc(bool)`,
   `PlaceDecoyServerRpc(Vector3)`, `ActivateConcussiveServerRpc(origin, dir, metres, range)`,
   `RequestPinServerRpc(ulong targetNetId, float seconds)`. Server finds `GameController`
   via `NetSession.Game`, or `Bind` in the fixture. Repair ticks in `Update` while the
   flag is set. Range skipped when `HatchStation.Instance` is null (fixture).
2. **Create** `GameController.Client.cs` — `TickClientLocal` (FPC, gun, ability, decoy
   placement, Interactor) so the client path does not grow `GameController.cs` past cap.
   `RepairHatchFromNet` lives here: same spend as `RepairHatch`, no host-rig check.
3. **Change** `NetRunMirror` — `NetworkVariable<long> Banked` (host `Meta.BankedSalvage`,
   write-on-change). Fight HUD banked and the repair prompt read it on the client.
4. **Change** `RepairInteractable` — client-only: rising-edge start / LateUpdate stop;
   no local `RepairHatch`. Prompt from the mirror when `IsClientOnly`.
5. **Change** `PlayerGun` — `ConfigureDrone(bool)`; `SlotDrone` selects it; Fire Down on
   that slot calls `DecoyPlacement.OnFirePressed` (no clip). `ApplyMetaEffects` adds
   `DecoyBay` on the gun if missing, then `ConfigureDrone`.
6. **Change** `DecoyPlacement` / `DecoyBay` / `DecoyObject` — client confirm RPCs;
   `TryPlace` instantiates `NetSession.DecoyPrefab` (or a test override) and
   `NetworkObject.Spawn` on the server; solo with no prefab keeps the primitive.
7. **Change** `PlayerAbility` — client concussive/pin send parameters and skip local
   `Shove`/`Pin`; Mk1 and railshot unchanged.
8. **Create** `Assets/Prefabs/Net/NetDecoy.prefab` (`NetworkObject` + server-auth
   `NetworkTransform` + `DecoyObject`). Row on `DefaultNetworkPrefabs` and
   `NetSession._decoyPrefab` in Bastion.
9. **Create** `Assets/Tests/PlayMode/NetVerbTests.cs` — in-process host+client; host
   `GameController` for a live `RunSim`. Tests: channel start/stop spends host bank
   and raises hatch HP; empty bank heals nothing; decoy spawns on both; pin on host
   `EnemyGatekeeper` (`IsPinned`) and not on the client clone; concussive `Shove`s the
   host body.
10. **Change** docs: Roadmap Current + row; Changelog; co-op.md loopback marker.

Rejected: NetworkObject on every interactable; optimistic spend; client ship/armory;
per-player wallets; folding M57 or M52 into this slice; `GetInstanceID` over the wire.

## Supporting work

- **Driven by** interact hold, `3` + fire confirm, ability key. No new input.
- **Shown by** the existing repair prompt, decoy ghost, ability cooldown. No new surface.
- **Reads back as** mirrored hatch HP and banked, a decoy both screens can see, a
  shove that NetworkTransform carries.

## Verification

1. Criteria 7, 8 — `gkb_verify_request.txt`; `NetVerbTests` Passed; harness and
   lint-arch green. Gate is this, not the playthrough.
2. Criterion 6 — MPPM confirmation after the fixture is green. Host F10, board.
   Damage the hatch (not a Terminal Wave). Client: hold repair, watch HP rise and
   banked fall on both screens; `3` and place a decoy; fire the cone. Drain the
   wallet and confirm "no salvage to repair". Both logs: each spend/effect line
   once, host side. A `[Dev]` log cannot close this.

## Do not build

| Not building | Why not |
|---|---|
| Client armory / fabrication / ship verbs | The ship is host-local (M50's table) |
| Optimistic prediction of any spend | Loopback RTT makes truth cheap |
| Per-player wallets or spend attribution | One wallet; who-spent-what is guest-progression (co-op.md) |
| Hatch damage during Terminal Waves | M53 |
| Voice, ping wheel, comms | Brainstorm |

## Depends on / Unblocks

Depends on M50 (the client can see salvage and hatch). Completes the loopback epic
for in-run verbs. Steam join is still [`M60`](../1.%20planned/M60-a-join-a-friend-can-use.md).
Get-up is [`M57`](../1.%20planned/M57-the-downed-player-waits-on-the-ship.md).
