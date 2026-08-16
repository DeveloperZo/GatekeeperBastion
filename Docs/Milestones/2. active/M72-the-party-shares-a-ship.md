# M72 — The party shares a Ship

<!--
Status: next (current)
Update trigger: when party-lead rules, ready-vs-timer, or NetworkSceneManager choice changes
Last updated: 2026-08-16
Recut: 2026-08-15 grill — false greens cut; DDOL/Ready/ESM/M57 locked; Release-Gate #19 moved with code
Post-grill fixes: Spawn(destroyWithScene:false); SceneEvent lobby log; guest Return RPC; pause Ready
Dual-box gaps: seat marker; premise countdown; LastError on Relay fail; Join refuses second session
Criterion 9 eyes-on open. 2026-08-16 dual-box: host saw guest, guest did not see host
(host puppet unbound at MainMenu spawn); remote Boarder static. Fixes: SpawnMirror order,
owner-rig rebind, Speed animator drive, PlayerRegistry.Local prefers FPC.
-->

**Co-op polish / beta lobby.** Ship is the shared session lobby. Party lead (host) owns
NAV / EMBARK. Each player owns their own resources (M65 — not re-proven here). Join lands
everyone in the same live instance. Scene changes are party-wide (Ready + declared
countdown). Bastion verbs already shipped stay correct (regression note, not a free gate).

Overturns M64's solo-Ship friends path. Product failure: 2026-08-15 dual-box Relay join
(code path) — no co-presence on Ship, host EMBARK moved one seat, client never met a peer.
(Machine send checks in M64 stand; human criteria 19–25 and `Release-Gate` #19 are
superseded by this file.)

Reference: NGO Boss Room / Invaders (lobby NetworkManager, ready, host `NetworkSceneManager`).
Unity Lobby *service* stays rejected (M60).

## Story

As a friend, I want to join by code, stand on the same Ship as the party, shop my own
loadout, and EMBARK into Bastion with everyone, so the session is one game.

## Why it matters

**Leverage #10.** A join code and correct Bastion verbs still fail if "Host" is not
listening, Ship is offline, and EMBARK is a local scene load. LAN-only connect does not
close the friends alpha sentence.

## Kill criterion

If two player builds cannot: join by Relay code → both visible on the same Ship → party
EMBARK into Bastion → one networked kill on both screens, by **2026-08-28**, stop and
re-open session topology in `../../Decisions.md`. Eyes-on; fixture cannot prove Relay.

## Acceptance criteria

1. **Session listens before Ship play.** After menu Host confirm (Relay or LAN) or Join
   connect succeeds, the host's `NetworkManager.IsListening` (guest: connected client) is
   true before that seat can open ARMORY. `NetJoinIntent` is consumed when that menu action
   starts the session — not deferred until Bastion. Solo Continue / Start New: no session.
   Friends path is only Host/Join; Continue cannot be joined by code.
2. **Shared Ship instance.** Host loads Ship via `NetworkSceneManager`. Guest join lands
   in that networked Ship (not a second offline Ship). Each seat logs
   `[Net] lobby ship clientId=<n>` within **10000** ms (`ConnectTimeoutMs`). Failure sets
   `NetJoinIntent.LastError` to a named string and returns the guest to MainMenu (no hang).
3. **Presence.** Each connected seat sees the other seats' lobby avatars on Ship within
   that window. Capsule OK; each seat has a distinct tint or index marker so "I don't see
   you" is not "identical capsules." Nameplates / rigs stay M66.
4. **Party lead owns NAV commit.** Host is party lead. Only the lead's EMBARK commits the
   party. Guest NAV shows no commit control; copy names that the party lead embarks.
   Guest attempt to local-load Bastion while a session is active refuses with
   `[Net] embark refused — not party lead`. Lead disconnect ends the session (no migration).
5. **Ready gates commit; 5 s is cancel-only.** Every connected guest must be Ready before
   the lead's EMBARK commit succeeds (solo-in-party: lead alone, no guest Ready). Ready is
   a session control (HUD or any station), not NAV-only. After commit, every seat shows a
   **5.0 s** countdown; **any** seat Cancel (or un-Ready) aborts, clears Ready, and aborts
   the load. When the timer hits 0, host runs party Bastion load. Lead cannot force past
   missing Ready. During countdown: **refuse new joins** (named refusal). Guest leave drops
   the seat and recomputes Ready quorum; lead gets a named left line.
6. **Party-wide Bastion load.** At countdown 0, host `NetworkSceneManager` loads Bastion
   for every connected seat. `GameSession.PendingStartWave` is the **lead's** pick (guests'
   deepest does not drive the run). Destination line on guest NAV is read-only.
7. **Party-wide return after run end.** After the existing end screen dismiss, host loads
   Ship via `NetworkSceneManager` for every seat still in session (no `ShutdownSession` on
   that path). Quit / leave still shuts down and returns that seat to MainMenu.
8. **Relay lifetime.** Menu allocation stays (M60); `StartHost` binds it **before** Ship
   shopping. Expired / unreachable join paints named `LastError` on MainMenu.
9. **Player action (closes):** two player builds, two networks (LAN same-build does
   **not** close). Host shows code → guest joins → both on Ship and see each other → each
   completes one local ARMORY or CREW change → all Ready → lead EMBARK → countdown → both
   in Bastion → one server kill both screens → end → both on shared Ship (or clean leave).
   No `[Dev]` in either close log.

## Implementation

Locked choices (no OR left to the implementer):

1. **DDOL** `NetSession` + `NetworkManager` + `UnityTransport`, spawned/started from menu
   Host/Join success **before** Ship load. Rejected: NetworkManager authored only inside
   `Ship.unity`. Rebind `GameController` when Bastion loads. Remove Bastion-only lifetime.
2. **`EnableSceneManagement = true`** whenever this session object is active (Relay or
   LAN). Solo never creates it. Document expiry of M58/M60 "stays false" in
   `../../Decisions.md` and one line in `../../Architecture.md`. M57 additive wait **never**
   uses `NetworkSceneManager`.
3. **`MainMenuController`:** on Host/Join success → StartHost/StartClient → host
   `NetworkSceneManager` loads Ship. Remove pending-intent-across-offline-Ship for friends.
   Host-while-already-hosting paints named refusal (no second session).
4. **`MapStation` / NAV / lobby HUD:** Ready session verb; lead EMBARK commit; 5.0 s
   countdown UI; Cancel. Party Bastion commit is a **new** server API (e.g. party embark
   on session/lobby) — **do not** overload `NetPlayerAvatar.RequestEmbark` (that is M57
   Hatch-bank get-up).
5. **Lobby avatars:** reuse PlayerPrefab / `NetPlayerAvatar`; Ship spawn offsets; distinct
   seat tint/index; Bind must tolerate missing Bastion `GameController` on Ship.
6. **Personal shopping:** unchanged local `GameSession` — no networked ARMORY stock; no
   shared bank (M65). Shared station geometry; first-come local panel; no station lock.
7. **`ReturnToShip` / end:** party Ship load without shutdown when staying in session;
   Quit still `ShutdownSession`.
8. **`Docs/Release-Gate.md` #19** and Play Ship pages: shared lobby / party EMBARK (same
   commit as code). Pause join-code row stays valid on Ship.
9. **`ConnectTimeoutMs = 10000`.**
10. **Fixtures:** existing Net* keep forcing `EnableSceneManagement = false`. **Create**
    loopback lobby test: session ESM true, host party-loads a build-registered scene (or
    stub), guest local Bastion load refused. List every Net* file touched.

**Rejected:** Unity Lobby service. Host migration. Shared salvage/loadout. Closing on
LAN-only. Lead-force EMBARK without guest Ready. Soft timer-only with no Ready.
Overloading M57 `RequestEmbark`.

## Supporting work

- **Driven by** Main Menu Host/Join; Ready control; lead EMBARK — here.
- **Shown by** other avatars + seat tint on Ship; Ready/countdown chrome — here.
- **Reads back as** `[Net] lobby ship…` then both seats in Bastion; kill both screens —
  criterion 9.

## Verification

1. Criteria 1–4, 6 (party load) — named PlayMode lobby fixture Passed in
   `gkb_verify_report.json`; assert ESM true on session managers; guest local Bastion
   refuse logged. Fixtures: `NetLobbySessionTests.NetSession_StartHost_EnablesSceneManagement`,
   `NetLobbySessionTests.Host_LocalBastionLoad_RefusedWhileSessionActive`,
   `NetLobbySessionTests.SceneFlow_LoadBastion_RefuseLines_PresentInSource`.
2. Criteria 5, 7, 8 — Ready/cancel and party return covered in fixture where possible;
   Relay lifetime eyes-on.
3. Criterion 9 — eyes-on two player builds, two networks, Relay code.
4. M57 coexistence — one MPPM or dual-box: mid-Assault client down → additive wait while
   host stays in Bastion; Hatch-bank EMBARK still works. Not a substitute for criterion 9.
5. Regression (non-gating): after party EMBARK, one NetPrep Wave-start + one M51 client
   verb still apply once server-side.
6. **2026-08-16 dual-box presence** — PlayMode `NetLobbyPresenceTests` Passed
   (`gkb_verify_report.json` 2026-08-16T16:01:57Z): late FPC track; production
   `SpawnUnparentedMirror`; both seats' avatars; remote Animator Speed. Multiagent review
   (same day) hardened false-green SpawnMirror test and Bastion client StartRun boot.
7. **Not dual-box until** loopback NetLobby* green on current dll (above). Criterion 9
   (Relay, two networks) remains eyes-on only — do not treat presence fixtures as AC9.

## Do not build

| Not building | Why not |
|---|---|
| Nameplates / character rigs | M66 |
| Voice / ping | co-op.md |
| Steam | Decisions / M60 |
| Wave = party min clear auto | Lead pick stays; guest confusion is copy, not a formula change this slice |
| Replacing Relay | M60 |

## Watch

- **M57** additive Ship is not the party lobby. Never party-unload Ship while a seat's
  wait Ship is loaded without exiting wait first.
- Prefabs freeze serialized values — session mode selects lobby vs fight; do not bake it.
- Fixture cannot close Relay or dual-box chrome.
- Editor F10/F11 and `-gkb-host`/`-gkb-join` may still skip lobby (LAN smoke). Friends
  path may not.

## Depends on / Unblocks

Depends on M60 join-code path and M51/M64 Bastion verbs. Unblocks honest friends alpha.
M66 waits until this room has a door.
