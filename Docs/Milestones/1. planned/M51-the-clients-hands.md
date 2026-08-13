# M51 — The client's hands

<!--
Status: later
Update trigger: re-cut against M50's landed shapes before starting (Zo, 2026-08-12: author
now, refine through implementation) — and consider splitting if repair alone proves a full
slice. Also when an in-run verb is added or removed.
Last updated: 2026-08-12
-->

After M50 the client sees everything and touches nothing: repair, decoys and the ability
cone are still local-only, and using them would act on a world the client does not own.
This slice routes the client's **in-run verbs** — repair, decoy placement, abilities —
through the server, with the same single-application invariant M48 established for damage.
After it, two people play a complete run over loopback: fight, spend, repair, win or lose.

## Story

As a second player, I want to repair the hatch, place decoys and use my abilities, so that
I carry half the run instead of half the shooting.

## Why it matters

Co-op in this genre is division of labour — one player holds the door while the other
repairs it — and right now every non-gun verb is host-only. Leverage tier
(`../../Working-Rules.md`): **#10, structure** — verbs move across the wire; no number
changes, and repair cost/rate stay where `../../Design/4-Data.md` puts them.

The scope cut that keeps this one slice: **in-run verbs only.** The client brings their
own loadout from their own local save; the armory, fabrication and the ship stay
host-local because the ship is not co-op (M50's table). One wallet: the host sim's
salvage is the only wallet, so every client spend is a server-validated request.

## Acceptance criteria

1. **Repair**: holding interact on the repair rig as a client repairs the real (host)
   hatch — client sends `SetRepairingServerRpc(bool)` on hold start/stop, the **server**
   runs the channel with its own dt, validates range, and spends salvage exactly as a
   host repair does (`RepairInteractable.Interact(dt)` semantics, host-side). Hatch HP
   and salvage move on **both** screens via the M50 mirror; the host log carries exactly
   one spend trail. Releasing the key stops the channel within one tick.
2. **Decoy**: the client places a decoy through the existing placement flow; the request
   is `PlaceDecoyServerRpc(position)`, the server runs `DecoyBay.TryPlace` with its own
   validation, and the spawned decoy is a `NetworkObject` visible on both screens.
   Gatekeepers divert to it (server AI already reads the server's `DecoyBay.Live` — no
   enemy changes).
3. **Abilities**: the client's concussive cone and manual pin act on the server's live
   ledger — activation sends origin/direction, the server evaluates the cone against
   `WaveSpawner.Live` and applies the effect; the client sees stagger/pin on the clones.
   Cooldowns run client-side (they gate input, not truth).
4. **Denial reads back**: a spend the wallet cannot cover is refused server-side and the
   client sees the existing insufficient-salvage feedback within the same second — no
   silent failure, no optimistic local spend that snaps back.
5. **The single-application invariant holds for every verb**: no repair tick, decoy cost
   or ability effect applies on the client instance; the fixture proves each verb applies
   once, server-side, and never locally (the M48 criterion-5 pattern, per verb).
6. **The player action: in one MPPM run, the client repairs the hatch under attack while
   the host shoots, then turns a wave with a decoy** — the division-of-labour moment,
   driven by one person (host idles while client acts).
7. **Headless**: `NetVerbTests` (the M48 fixture) asserts criteria 1–5's mechanics:
   channel start/stop, server-side spend, denial path, decoy spawn/replication, cone
   application to the server ledger. Green in the integration-check report.
8. **Solo is unchanged**: both suites and the harness green at current counts; no `[Net]`
   line in a solo log.

## Implementation

Order inside the slice is the risk order: repair first (the loop's core verb, and the
channel pattern everything else copies), decoys second (re-uses M48's spawn path),
abilities last (pure RPC, no new objects).

1. **Change** `NetPlayerAvatar.cs` (or a sibling `NetVerbs.cs` NetworkBehaviour on the
   avatar prefab — one component per concern if the avatar file passes ~150 lines) —
   the client's RPC surface: `SetRepairingServerRpc(bool)`, `PlaceDecoyServerRpc(Vector3)`,
   `ActivateConcussiveServerRpc(Vector3 origin, Vector3 dir)`, `RequestPinServerRpc(ulong
   targetNetId)`. Server handlers reach the run through the scene `GameController`
   (via the `NetSession` reference M48 wired).
2. **Change** `Interactor.cs` / `RepairInteractable.cs` — client-only branch at the hold
   site: forward hold start/stop instead of running `Interact(dt)` locally. The prompt
   and hold UX stay local and unchanged.
3. **Change** `DecoyPlacement.cs` / `DecoyBay.cs` — client-only branch at confirm:
   request instead of `TryPlace`. `DecoyObject` prefab gains `NetworkObject` +
   server-auth `NetworkTransform` and a `DefaultNetworkPrefabs` row.
4. **Change** `PlayerAbility.cs` — client-only branch at `ActivateConcussive` /
   `TryManualPin`: send parameters, skip local application; keep local cooldown and VFX
   trigger (cosmetic feedback may be optimistic — state may not).
5. **Create** `Assets/Tests/PlayMode/NetVerbTests.cs` — criterion 7.
6. **Change** docs: Roadmap row, Changelog; the co-op brainstorm's "full loopback run"
   marker — after this slice, the Steam entry's graduation test is met.

Rejected: making every interactable a `NetworkObject` — the hatch and rig are
scene-authored singletons reachable through the references that already exist; identity
over the wire is only needed for things the server *spawns*. Rejected: optimistic client
spend with reconciliation — the wallet is small and the RTT is loopback; showing truth a
tick late beats snapping a lie back. Rejected: client fabrication/armory — ship verbs,
and the ship is not co-op.

## Supporting work

- **Driven by** the existing bindings — interact hold, decoy confirm, ability keys. No
  new input.
- **Shown by** the same prompts and panels the host sees — no new surface.
- **Reads back as** hatch HP and salvage moving on both screens (M50 mirror), the decoy
  standing there, the stagger landing — here.

## Verification

1. Criteria 7, 8 — `gkb_verify_request.txt`; integration-check report green at current
   counts plus `NetVerbTests`; harness and lint-arch green.
2. Criteria 1–6 — playthrough path: MPPM one virtual player. Host F10, board, then idle
   at the door. Client: hold repair under attack (watch hatch HP rise on both screens and
   salvage fall), place a decoy mid-wave (watch gatekeepers divert on both screens), fire
   the cone into a group (watch the stagger), then drain the wallet and confirm the
   denial message. Read both logs: every spend/effect line appears exactly once, host
   side.

## Do not build

| Not building | Why not |
|---|---|
| Client armory / fabrication / ship verbs | The ship is host-local (M50's table); nothing in-run needs them |
| Optimistic prediction of any spend | Rejected above — loopback RTT makes truth cheap |
| Per-player wallets or spend attribution | One wallet is the decision; who-spent-what is guest-progression territory (open, co-op brainstorm) |
| Voice, ping wheel, comms | Real co-op features, real design weight — brainstorm entries when wanted |

## Depends on / Unblocks

Depends on M50 (the client can see salvage and hatch state — spending blind is not a
verb). **Completes the loopback epic**: after this, a full co-op run works end to end,
which is the graduation test for the Steam transport + join surface entry in
`../../Brainstorms/co-op.md`.
