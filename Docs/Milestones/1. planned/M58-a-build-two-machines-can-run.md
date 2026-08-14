# M58 — A build two machines can run

<!--
Status: later
Update trigger: when bind/join address, the Main Menu host/join surface, or
EnableSceneManagement changes. Re-cut against M48's landed NetSession before starting.
Last updated: 2026-08-14
-->

Every gate so far closed in editor / PlayMode / MPPM loopback. F11 joins `127.0.0.1` only
(`NetSession.ConfigureTransport`). Two machines cannot connect with the current join. This
slice is a Windows player build and a Main Menu LAN host/join.

## Story

As a friend, I want to install a Windows player build and join a host on another machine, so
that co-op is not an editor ritual.

## Why it matters

The alpha gate needs a build someone who is not Zo can install. Leverage tier
(`../../Working-Rules.md`): **#10, structure** — how the session starts; no number moves.

This is infrastructure with a developer-Story exception under
`../../_meta/_definitions/milestone.md`: every friends session uses it after it ships.
Multiplier: unblocks M59 (F10/F11 can leave the player panel) and M60 (Relay join code
replaces typed IP as the friends path). Durable: the menu join outlives this slice.

No kill criterion — not a design bet. NGO already survived M48.

## Acceptance criteria

1. A **Windows player build** from this project's Build Profile launches Splash → MainMenu
   without the Unity editor. The session log writes under
   `Application.persistentDataPath/Logs/` (`../../Working-Rules.md`).
2. **Main Menu Host** starts a listen server on `0.0.0.0:7777` (not loopback-only), then
   loads Bastion the same way today's F10 host does (`NetSession.StartHost`, scene management
   still off; both machines load Bastion themselves).
3. **Main Menu Join** takes a typed IPv4 and port (default 7777) and connects via
   UnityTransport. F11 / `127.0.0.1` remains as an editor/dev fallback, not the friends path.
4. **The player action:** two player-build instances (or one player build + one editor as
   the second machine) on two machines or two LAN addresses: host Host, client types the
   host LAN IP, both appear in Bastion; client kills one server-spawned gatekeeper; death on
   both screens (M48 loop in a player build).
5. A failed connect (wrong IP, host not listening) **reads back** a visible refusal within a
   few seconds — no silent hang. Logged.
6. Solo player-build launch (no Host) still reaches Ship → Bastion as today. No `[Net]` in
   that log.
7. Headless: extend or add a PlayMode test that `StartClient` can target a non-loopback
   address the fixture binds (or an address parameter on `NetSession`); existing
   `NetKillTests` stay green. Do not require two physical machines in the integration
   check — criterion 4 is the two-machine playthrough.

## Implementation

`NetSession` stays on Bastion (`../3. completed/M48-the-first-networked-kill.md`: no statics
on the session object; `EnableSceneManagement` false). Main Menu cannot hold a reference to
it, so a tiny intent object carries host/join across the scene load.

1. **Change** `NetSession.cs` — `StartHost(string bindAddress = "0.0.0.0", ushort port = 7777)`
   and `StartClient(string address, ushort port = 7777)`. `ConfigureTransport` calls
   `UnityTransport.SetConnectionData` with those values, never the hardcoded
   `LoopbackAddress`. F10 calls `StartHost()` (0.0.0.0 still accepts loopback, so MPPM F11
   keeps working). F11 calls `StartClient("127.0.0.1", 7777)`.
2. **Create** `Assets/Scripts/Game/Net/NetJoinIntent.cs` — static, Game/ only: `RequestHost()`,
   `RequestJoin(string address, ushort port)`, `LastError`. Consumed once in
   `NetSession.Awake` (all Awakes run before Starts, so `IsClientOnly` is true before
   `GameController.Start`). Not a field on `NetSession` — that type's no-statics contract
   stays.
3. **Change** `MainMenuController.cs` / `MainMenuView.cs` / `MainMenu.unity` — Home gains
   **Host** and **Join** next to Continue / Start New. Join opens a Shift
   `MainPanelManager` panel (`"Join"`): IPv4 field, port field default 7777, Connect, Back,
   refusal label. Host: `ContinueGame` if a save exists else `StartNew` (no overwrite
   modal), `NetJoinIntent.RequestHost()`, `SceneFlow.LoadBastion()`. Join: same session
   load, `RequestJoin`, `LoadBastion`. Continue / Start New still `LoadShip`. Menu Host/Join
   must **not** call `GameSession.NotifyDevAction` — a `[Dev]`-tainted log cannot close
   criterion 4. F10/F11 already skip that call (2026-08-13): they stay on the Dev key
   table so they compile out of release, but they are session start, not a mutation.
4. **Change** `NetSession.cs` — subscribe `OnClientDisconnectCallback`. If the client never
   became a connected peer, set `LastError`, log `[Net] join refused reason=… address=…`,
   `SceneFlow.LoadMainMenu()`. `UnityTransport.ConnectTimeoutMS = 4000`. Invalid IPv4 or
   empty address refuses on the Join panel without loading Bastion. MainMenu paints
   `LastError` on the refusal label.
5. **Create** `Assets/Tests/PlayMode/NetSessionAddressTests.cs` — spawn a
   NetworkManager + UnityTransport + NetSession; `StartClient("10.0.0.1", 7777)` then assert
   `UnityTransport.ConnectionData.Address` is `10.0.0.1` (and Host configures listen
   `0.0.0.0`). Shutdown. Do not actually connect to 10.0.0.1. `NetKillTests` untouched.
6. **Change** docs: Roadmap row, Changelog.

Rejected: keeping F10/F11 as the only join — a friend cannot be told to press F11 in a
player build and type nothing. Rejected: Unity Relay in this slice — that is M60, on this
same UnityTransport. Rejected: a DDOL NetworkManager on
MainMenu so listen starts before the scene load — second session object, fights Bastion-owned
`NetSession`. Player-visible order is menu then Bastion; bind happens on the existing Net
object after `LoadBastion`. Rejected: NGO scene management — both machines load Bastion
themselves, as M48.

IPv4 dotted quad only (four octets 0–255). No hostnames.

## Supporting work

- **Driven by** Main Menu Host / Join (typed IP + Connect) — here. F10/F11 stay as the
  editor fallback until M59 unbinds them from player builds.
- **Shown by** the Host and Join buttons on Home, and the Join panel's IP/port fields — here.
- **Reads back as** both machines in Bastion, a kill on both screens, or a visible join
  refusal — here. Failed join may briefly load Bastion (where `NetSession` lives) then
  return; the refusal is the MainMenu line plus the log.

## Verification

1. Criteria 6, 7 — `gkb_verify_request.txt`; integration-check report green at current
   counts plus `NetSessionAddressTests`; `NetKillTests` still green; harness and lint-arch
   green. Solo player-build log: no `[Net]`.
2. Criterion 1 — File → Build Profiles → Windows, player build, launch the .exe; Splash →
   MainMenu; confirm a file under `persistentDataPath/Logs/`.
3. Criteria 2–5 — playthrough path: two machines (or two LAN addresses). Development
   player builds are fine (logs). Host: Main Menu Host. Client: Join, type the host's LAN
   IPv4, Connect. Both in Bastion; client kills one gatekeeper; both screens despawn it
   (M48 lines in both logs). Wrong IP: refusal on MainMenu within ~4 s, `[Net] join
   refused` in the client log. One editor instance may stand in for the second machine.

## Do not build

| Not building | Why not |
|---|---|
| Unity Relay / join code | M60; same UnityTransport, internet path (`../../Decisions.md`) |
| Host migration | Rejected in M45 |
| Client HUD / in-run verbs | M50 / M51 |
| Friends-facing key strip | M59 |
| NGO scene management | Both load Bastion themselves (M48) |

## Watch

- Development player builds still compile `DevCommands` (`DEVELOPMENT_BUILD`) — F2–F9
  taint until M59. F10/F11 do not. Do not press F2–F9 in the criterion-4 log.
- Windows Firewall on UDP 7777 — a bind that works and a join that hangs is often this,
  not netcode.
- `EnableSceneManagement` stays false.
- Prefabs freeze serialized values (`../../Architecture.md`): do not serialize the bind
  address on `UnityTransport`; push it from `StartHost` / `StartClient`.

## Depends on / Unblocks

Depends on M48 (session exists). Recommended after M51 so there is a loop to join, but the
gate itself only needs the M48 kill loop. Unblocks M59 (player-build keys) and M60.
