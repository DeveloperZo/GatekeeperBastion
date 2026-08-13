# M48 — The first networked kill

<!--
Status: completed 2026-08-13
Update trigger: closed
Last updated: 2026-08-13
Closed on: Unity suite EditMode 318/0, PlayMode 24/0 including NetKillTests.KillLoop_AppliesOnceOnHost_DespawnsAndMirrors (gkb_verify_report.json 2026-08-13T12:49:08Z; PlayMode was 23/0 at M45). lint-arch clean; rg "Unity.Netcode" Assets/Scripts/Sim/ empty. NGO 2.13.1 + MPPM 2.0.2. Editor.log [Net] lines in that run are only the fixture (hit + mirror); Bastion PlayMode tests emitted none. MPPM F10/F11 playthrough is confirmation, not the gate.
-->

The co-op slice M45's Watch section cut: **a second instance joins over loopback, shoots one
server-spawned gatekeeper, it dies on both screens, and the wave counter reads the same number
on both.** No Steam, no lobby, no client HUD beyond the wave line, no second player in the
targeting model. This is the first netcode integration and therefore the first real test of
the NGO decision (`../../Decisions.md`, 2026-08-10).

**This slice also ships the co-op test rig, and the rig is the point.** Zo is one developer;
a milestone whose gate needs two hands on two keyboards cannot be verified here. NGO ships
`NetcodeIntegrationTest` (`Unity.Netcode.Runtime.Tests` in 2.13.1; namespace
`Unity.Netcode.TestHelpers.Runtime`), which runs a host and N
clients **inside one PlayMode test** — so the kill loop closes headless in the integration
check, and MPPM play is confirmation, not the gate. **Multiplier:** every later co-op slice
writes its gate against this same fixture and closes without a second player present.

## Story

As a second player, I want to walk my friend's bastion and kill a gatekeeper that his
simulation owns, so that co-op exists in play rather than in architecture.

## Why it matters

Everything above this slice is already decided or built: the library
(`../../Decisions.md` — NGO, host-client, server-authoritative sim, client-authoritative
own-movement), and the isolation pre-work
(`../3. completed/M45-two-runs-in-one-process.md` — no static mutable state in `Sim/`, one
gameplay `Instantiate` site, spawner-owned live ledger, `RunContext`). What has never
existed is a packet. The NGO choice was made on AI-writability; until an agent has written
one working slice against it, that decision is an untested bet.

Leverage tier (`../../Working-Rules.md`): **#10, structure** — no tuned number moves.

## Kill criterion

Work starts 2026-08-13. **If criterion 5 — the networked kill — cannot be
demonstrated within ten working days of start, stop.** Record what NGO blocked, and re-open
the library decision against FishNet (the named runner-up) in `../../Decisions.md` before
writing more netcode. Separately: if this slice forces a static back into `Sim/` or a second
copy of run state, that falsifies M45's claim — record it there as a correction, do not
route around it silently.

## Acceptance criteria

1. `Packages/manifest.json` pins `com.unity.netcode.gameobjects` **2.13.1** and
   `com.unity.multiplayer.playmode` **2.0.2** (latest on the Unity registry for this editor,
   6000.3.21f1; 3.0.0 is not published). No other netcode package. `Tools/verify/run.sh` still
   compiles `Sim/` shim-only and `rg "Unity.Netcode" Assets/Scripts/Sim/` returns nothing.
2. In a Bastion scene, **F10 starts a host and F11 joins `127.0.0.1` over UnityTransport**,
   dev-only (same `#if UNITY_EDITOR || DEVELOPMENT_BUILD` envelope as the rest of the dev
   console, listed on the F1 overlay). Host logs `[Net] host started`; on join, both
   instances log the connection with its clientId.
3. Each connected instance owns exactly one `NetPlayerAvatar`. Walking on the client moves
   its avatar on the host's screen (owner-authoritative `NetworkTransform`); the owner never
   sees its own capsule.
4. With a session active, gatekeepers exist as **server-spawned `NetworkObject`s only**: the
   host's `WaveSpawner` spawns them, the client's spawner emits nothing, and client clones
   have `EnemyGatekeeper` and `NavMeshAgent` disabled — they move only by replication.
   Boarding is refused on a client (`[Net] client cannot board — the host owns the run`), so
   a dev key cannot fork a second live sim.
5. **The player action: the client fires at a server-spawned gatekeeper and damage applies
   exactly once, on the host** — host logs `[Net] hit enemy=<netId> dmg=<n> slot=<slot>`, the
   client clone's `Damageable` is never written. Its death despawns it on both screens, and
   the host banks **exactly one** kill (one kill line in the host log for that netId).
6. During an assault, **both screens show the same wave number**: the client's fight/assault
   readout is fed by `NetRunMirror` (phase, wave, alive), and both instances log
   `[Net] mirror wave=<n>` on change.
7. **The kill loop closes headless.** `NetKillTests` (PlayMode, derived from
   `NetcodeIntegrationTest`, one host + one client in-process) asserts, without MPPM and
   without a scene: a server-spawned gatekeeper clone appears on the client with AI and
   `NavMeshAgent` disabled; `SubmitHitServerRpc` drops host-side `Damageable.Hp` by exactly
   the sent amount while the client clone's `Damageable` is untouched; lethal damage despawns
   the clone on the client; and the mirror's `Wave`/`Alive` values read equal on both
   managers. These tests run inside the integration-check report
   (`gkb_verify_report.json`, PlayMode block) — that is what makes every later co-op slice
   verifiable by one person.
8. **Solo is unchanged**: with no session started, both Unity suites and the offline harness
   pass at their current counts, and a solo Bastion run's log contains no `[Net]` line.
9. The two new one-shot `FindAnyObjectByType` calls (spawned network prefabs cannot be
   scene-wired; see Implementation) are **added to the named-exemption fence** in
   `../../Architecture/3-Unity-Runtime.md` (the mechanical allowlist `lint-arch` reads;
   `../../Architecture/1-Boundaries.md` §2 names the exemption), in the same change, and
   `Tools/lint-arch/run.sh` stays green.

## Implementation

All new code under `Assets/Scripts/Game/Net/`, namespace `GatekeeperBastion.Game`. `Sim/`
is untouched. In order:

1. **Change** `Packages/manifest.json` — add the two packages (criterion 1). Record the
   resolved versions here if they differ.
2. **Create** `NetSession.cs` — MonoBehaviour on a new authored `Net` GameObject in
   `Bastion.unity` (authored scene content survives Build All), alongside `NetworkManager` +
   `UnityTransport` (`127.0.0.1:7777`, scene management **disabled** — both instances load
   Bastion themselves, which sidesteps in-scene `NetworkObject` sync entirely).
   API: `bool Active`, `bool IsClientOnly`, `StartHost()`, `StartClient()`; on host start it
   spawns the `NetRunMirror` prefab. No statics.
3. **Create** `NetRunMirror.cs` + prefab `Assets/Prefabs/Net/NetRunMirror.prefab` —
   NetworkBehaviour holding `NetworkVariable<byte> Phase`, `<int> Wave`, `<int> Alive`. Host
   writes them from `_game.Run.Sim` / `_game.Spawner.AliveCount` when they change. Spawned,
   not in-scene: with scene management off, a spawned prefab is the semantics-free path. On
   client, `OnNetworkSpawn` registers itself with the scene `NetSession` via one one-shot
   `FindAnyObjectByType<NetSession>()` (exemption, criterion 9).
4. **Create** `NetPlayerAvatar.cs` + prefab `Assets/Prefabs/Net/NetPlayerAvatar.prefab` —
   capsule mesh, `NetworkObject` (set as NGO's default player prefab), `NetworkTransform`
   with **owner authority** (the client-auth own-movement mechanism named in
   `../../Decisions.md`). Owner: hide own renderer; each `LateUpdate`, copy the local rig's
   position and yaw (rig reached by the second one-shot `FindAnyObjectByType`, at spawn).
   No collider — nothing in this slice targets or shoots players.
5. **Create** `NetEnemy.cs` — NetworkBehaviour added to the three gatekeeper prefabs
   (`PrefabFor` kinds: Gatekeeper, Hunter, Ram), each of which also gains `NetworkObject` +
   server-authoritative `NetworkTransform` and a row in the `DefaultNetworkPrefabs` list.
   `OnNetworkSpawn` on a non-server disables `EnemyGatekeeper` and `NavMeshAgent`.
   `SubmitHitServerRpc(float dmg, byte slot)` applies
   `Damageable.TakeDamage(dmg, senderAvatar.transform, (WeaponSlot)slot)` on the host and
   emits the criterion-5 log line. Enemy targeting still only knows the host's player — the
   client is invisible to AI in this slice, deliberately (next slice's problem).
6. **Change** `GameController` — optional serialized `NetSession _net`
   (`[OptionalBinding]`, scene-wired, BindingValidator covers it) exposed as `Net`; the
   boarding path refuses when `Net.IsClientOnly` (criterion 4). `WaveSpawner`, `PlayerGun`
   and `HudController` reach the session through their existing `GameController` reference;
   if one has none, it gains its own optional binding instead.
7. **Change** `WaveSpawner.Groups.cs` `SpawnOne` — skip Instantiate when client-only; after
   `Configure`, if a session is active on the server, `GetComponent<NetworkObject>().Spawn(true)`.
   `WaveSpawner.Despawn` calls `NetworkObject.Despawn` when spawned so client clones die.
   **Change** `SceneBuilder.Enemies.cs` — stamp `NetworkObject`, server-authoritative
   `NetworkTransform` and `NetEnemy` so Build All does not wipe them. With no session the
   components are inert and solo behaviour is byte-identical.
8. **Change** `PlayerGun.Firing.cs` — at the single damage-application site (line 132
   today): if the target carries `NetEnemy` and the session is client-only, call
   `SubmitHitServerRpc` **instead of** local `TakeDamage`. No client-side HP prediction:
   one application, on the host, is the whole point of the slice. Falloff and crit are
   computed on the shooting client and sent as the final number — client-authoritative
   hits, per M45's Do-not-build table (no lag compensation).
9. **Change** `HudController.Fight.cs` / `HudController.Panels.cs` — when client-only, the
   phase/wave/alive inputs to the fight and assault lines come from the mirror instead of
   `sim`. Every other panel keeps reading local state and is allowed to be wrong on a
   client — out of scope, named below.
10. **Change** `Game/Dev/DevCommands.cs` — two new `Binding` rows: `F10 host co-op session`,
    `F11 join 127.0.0.1`. `GameController.DevCommands.cs` and `DevConsole.cs` dispatch them.
11. **Change** `Packages/manifest.json` again — add `com.unity.netcode.gameobjects` to
    `"testables"` (how NGO exposes `NetcodeIntegrationTest` to a project's own tests). This
    also surfaces NGO's package tests in the Test Runner window; the integration check is
    unaffected because it pins `assemblyNames` to `GatekeeperBastion.PlayTests`.
12. **Change** `Assets/Tests/PlayMode/GatekeeperBastion.PlayTests.asmdef` — add references
    to `Unity.Netcode.Runtime` and `Unity.Netcode.Runtime.Tests` (the 2.13.1 assembly that
    contains `NetcodeIntegrationTest`).
13. **Create** `Assets/Tests/PlayMode/NetKillTests.cs` — fixture derives
    `NetcodeIntegrationTest` with `NumberOfClients = 1`; registers the gatekeeper and
    mirror prefabs on both managers (`CreateNetworkObjectPrefab` for scratch objects, real
    prefabs for the components under test); asserts everything in criterion 7. The fixture
    uses NGO's own test-owned `NetworkManager`s — the scene `Net` object is absent, which is
    exactly why `NetEnemy`/`NetRunMirror` must not assume a `NetSession` exists (null-check,
    same as every optional binding).
14. **Change** docs in the same change: Decisions.md Multiplayer row gains "first slice
    shipped over UnityTransport loopback (M48); Steam transport still deferred";
    Architecture exemption fence (criterion 9); Roadmap row; Changelog.

Rejected: making the real player rig the network player prefab — it drags the camera, gun,
input and HUD stack into NGO ownership for zero slice value; a puppet capsule proves
presence and movement. Rejected: NetworkSceneManager — both instances already load Bastion,
and scene sync is the largest new failure surface NGO offers. Rejected: client-side damage
prediction — double-application is the classic first netcode bug; this slice buys the
simple invariant instead (HP lives on the host, full stop).

## Supporting work

- **Driven by** F10/F11 dev keys — here, deliberately dev-only (M13's F7 precedent: ships
  dormant for real players). A player-facing host/join surface belongs to a later co-op
  slice, unnumbered until this one closes.
- **Shown by** the F1 dev overlay listing both bindings — here.
- **Reads back as** the joined log lines, the avatar on the host's screen, and the shared
  wave counter — here.

## Verification

Headless first — the playthrough confirms what the fixture already proved.

1. Criteria 1, 9 — `Tools/verify/run.sh` (via Git Bash) green at current counts; the two
   `rg` checks; `Tools/lint-arch/run.sh` green.
2. Criteria 4, 5, 6, 7 (mechanics) — write `gkb_verify_request.txt`; the integration-check
   report's PlayMode block lists `NetKillTests` green. This closes the replication, the
   single-application invariant, the despawn and the mirror equality without a second
   instance running.
3. Criterion 8 — same report green at current counts plus `NetKillTests`; grep the solo
   session log for `[Net]` (must be absent).
4. Criteria 2, 3 and the on-screen halves of 5–6 — playthrough path: open `Bastion.unity`
   directly (M14 deep-start path), enable **one virtual player** in the Multiplayer Play
   Mode window, enter Play. Main editor: F10, then board the hatch. Virtual player: F11,
   walk in, shoot the first gatekeeper dead. Read: both instances' logs for the connection
   and mirror lines, the host log for exactly one `[Net] hit` + one kill line per netId,
   and both screens for the same wave number. Zo closes by eye; the log lines are the
   record. One person can drive this alone — the host needs no input while the client
   walks and shoots.

## Do not build

| Not building | Why not |
|---|---|
| Steam / Facepunch transport | Transport is the most swappable layer (`../../Decisions.md`); loopback proves the slice |
| Client in the targeting model | Enemies hunting the second player is its own slice with its own criteria |
| Full client HUD, salvage, repair, abilities over the wire | Each is a later slice; only the wave line replicates here |
| Ship scene co-op, lobby, main-menu join UI | The run is the game; front-end plumbing waits until a slice needs it |
| Everything in M45's Do-not-build table | Prediction, lag comp, dedicated server, host migration, anti-cheat — still rejected there |

## Watch

- **MPPM virtual players desync via `Library/VP` corruption** — the documented fix is
  delete-and-regenerate (package FAQ). Do not debug netcode through a corrupted clone.
- **NGO 2.13 supports Fast Enter Play Mode with domain reload off** (2.13.0 changelog); if
  connection state survives a Play session strangely, check the domain-reload setting first.
- **Networked smoothness is the underestimated part** — R.E.P.O. quote in M45's Watch.
  Jittery replication does not fail this gate; it gets measured, written down, and budgeted
  for the next slice.
- **Prefabs freeze serialized values** (`../../Architecture.md`): the NetworkTransform and
  NetworkObject settings added to the gatekeeper prefabs are structural and authored once —
  fine — but any *tunable* that grows here gets pushed at spawn like everything else.

## Depends on / Unblocks

Depends on M45 (completed 2026-08-12). Unblocks every later co-op slice: targeting the
second player, the client's full HUD, Steam transport, the join surface.
