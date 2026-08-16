# M60 — A join a friend can use

<!--
Status: completed 2026-08-15
Update trigger: closed
Last updated: 2026-08-15
Closed on: Zo 2026-08-15. Gate: Relay + Authentication packages locked (1.2.0 /
3.7.0); cloudProjectId linked; NetSessionAddressTests all Passed incl.
TryParseJoinCode (`gkb_verify_report.json` 2026-08-15T21:47:12Z); Sim has no
Netcode/Services. Criterion 4 eyes-on Zo-closed. Multiplayer Services 2.3.0
removed (rejected). Kill criterion satisfied by close before 2026-08-28.
-->

Graduates `../../Brainstorms/co-op.md` "a join surface a friend can use". M58 is typed LAN
IP. This is the internet/friends path: **Unity Relay on the existing UnityTransport**,
recorded in `../../Milestones/3. completed/M64-a-build-you-can-hand-to-a-friend.md` and
locked in `../../Decisions.md`. Steam is deferred until a real appid exists.

## Story

As a friend, I want to host or join from the Main Menu with a short join code, so that a
session over the internet does not require a typed LAN IP, a VPN, a port forward, or Steam.

## Why it matters

Alpha gate sentence: two friends install a build, join without Zo touching a keyboard.
Leverage tier (`../../Working-Rules.md`): **#10, structure** — how a session starts over
the internet. Completes the join surface. No host migration (rejected in M45).

**Unity Relay is free at this scale.** Free tier is **50 average monthly CCU** and 150 GiB
egress ([UGS pricing](https://unity.com/products/gaming-services/pricing)). Four friends
for a few hours a week is a rounding error. Players sign in anonymously — no Unity account
per friend. `CreateAllocationAsync` returns a join code the host pastes into chat. It sits
on the UnityTransport already in the build; there is no transport swap
([NGO Relay](https://docs.unity.com/Packages/com.unity.netcode.gameobjects@2.7/manual/relay/relay.html)).
Steam is the wrong tool this month: a real appid is $100 plus a 30-day wait; appid 480 is
a shared lobby space and not a distribution path (`M64`).

NGO stays on the AI-authorship criterion in `../../Decisions.md`. Transport remains the
swappable layer, which is why this slice is last of the join stack and carries a kill
criterion.

## Kill criterion

Work started 2026-08-15. **If criterion 4 — two player builds, one Relay join code, one
kill on both screens — cannot be demonstrated by 2026-08-28 (ten working days), stop.**
Record what Relay / Authentication / the UGS project blocked, and re-open the transport
decision in `../../Decisions.md` before writing more netcode. Same shape as M48's NGO kill
criterion. UGS project ID is empty in `ProjectSettings` at start — Host must refuse by
name until Zo links the Unity project.

## Acceptance criteria

1. Main Menu Host / Join uses **Unity Relay** as the friends path. Host allocates, shows a
   join code, copies it. Join takes that code (six characters, case-insensitive). Typed LAN
   IP from M58 remains as a fallback when Relay is unavailable (offline, auth failure, no
   UGS project).
2. Join-in-Tethered is allowed (client can connect while the host is already in Bastion).
   The join code stays visible on pause so the host does not have to remain on the menu.
   Clean disconnect returns the client to MainMenu. Host quit messages the client (not a
   silent stall).
3. F10/F11 are not the friends path (editor-only after M58/M59).
4. **The player action:** two friends, two player builds, two networks that are not the
   same LAN: one Host, the other Join by code — no typed public IP, no port forward, no
   Steam, no editor, no Zo on either keyboard after launch. One gatekeeper dies on both
   screens. Session ends clean (M50 shutdown).
5. Headless fixture cannot prove Relay — **eyes-on for criterion 4**. Fixture still proves
   shutdown/rejoin on the existing UnityTransport path (M50 criterion 4). The fixture
   cannot test transports (`../../Brainstorms/co-op.md` Watch).
6. Solo unchanged; `Sim/` has no netcode references and no Unity Services references.

## Implementation

Recut 2026-08-15 against M58's landed menu. Host / Join live on `MainMenuController` /
`MainMenuView` / the authored Join panel. Intent is `NetJoinIntent` (static, Game/ only).
`NetSession` stays Bastion-owned; `EnableSceneManagement` stays false. Player-build CLI
(`PlayerLaunch` `-gkb-host` / `-gkb-join`) stays the LAN dual-box path.

1. **Change** `Packages/manifest.json` — `com.unity.services.relay` **1.2.0** and
   `com.unity.services.authentication` **3.7.0**, compatible with NGO 2.13.1 and this
   project's embedded `com.unity.transport` 2.6.0. Do not add
   `com.community.netcode.transport.facepunch`. **Rejected:** `com.unity.services.multiplayer`
   2.3.0 — it depends on transport 2.7.3 and collections 2.6.7 against the embedded
   2.6.0 / 2.2.1 pair, and it wraps Lobby, which this slice rejects. Standalone Relay
   1.2.0 is deprecated on Unity 6 and is the last 1.x; `UnityTransport.SetHostRelayData`
   / `SetClientRelayData` on the existing transport is the integration. **Resolved lock
   (2026-08-15):** authentication **3.7.0**, relay **1.2.0** (`Packages/packages-lock.json`).
2. **Open, labelled:** Unity Cloud / UGS project ID is Zo's. `cloudProjectId` is empty in
   `ProjectSettings` at start. Host cannot allocate until the Unity project is linked.
   Anonymous sign-in; no per-player Unity account. Missing ID fails Host with a named
   refusal, not a hang.
3. **Create** `Assets/Scripts/Game/Net/RelayJoin.cs` — allocate / join / named errors /
   `Apply(UnityTransport, Payload)`. No NetworkManager. Max connections 3 (host plus two
   guests; `M65` raises the count). **Change** `NetJoinIntent.cs` — `TryParseJoinCode`
   (six alphanumeric, case-insensitive), `RequestHostRelay` / `RequestJoinRelay` carrying
   `RelayJoin.Payload`, `JoinCode` readable after Consume. LAN `RequestHost` /
   `RequestJoin(address, port)` unchanged.
4. **Change** `NetSession.cs` — if the consumed intent has a Relay payload, `RelayJoin.Apply`
   then `StartHost` / `StartClient` as M58; else M58 `SetConnectionData`. Expose `JoinCode`
   for pause. Do not enable NGO scene management. Do not enable host migration. Do not add
   Unity Lobby.
5. **Change** Main Menu — Host click allocates on the menu (async) *before* `LoadBastion`,
   so the code can show on a Host confirmation without a DDOL NetworkManager (rejected in
   M58). Success: show the code, copy it, Enter Bastion. Failure: named refusal + **Host
   on LAN** (M58 `RequestHost`). Join panel default is a code field + Connect; LAN IP/port
   remain the fallback row. Invalid / empty code refuses on the panel without loading
   Bastion. Join allocation failure refuses on the panel (no Bastion bounce). Rejected: a
   browsable server list. Rejected: requiring both players to still be on the menu —
   join-in-Tethered is criterion 2.
6. **Change** pause — while a Relay session is live, the join code is a pause shortcut
   row (`CODE` / the six characters / `SESSION`). Do not add a new always-on HUD row
   (`../../Architecture/README.md`).
7. **Change** `GameController.QuitToMenu` — if a session is active, `ShutdownSession`
   before `LoadMainMenu`. Client quit: host continues. Host quit: client log `[Net] host
   left`, `LastError` = `Host left the session.`, return to MainMenu (M50 shutdown plus
   the message M50 did not paint).
8. **Change** F10/F11 — `UNITY_EDITOR` only in `DevCommands.All`. Development player
   builds no longer list or bind them (`ShortcutPanel.DefaultDevSurface` → `None`). Not
   the friends path.
9. Headless: no new Relay fixture. Keep UnityTransport shutdown/rejoin coverage.
   `NetSessionAddressTests` gains `TryParseJoinCode`; LAN address tests stay.
10. **Change** docs: Roadmap row, Changelog; co-op.md join entry graduates on close.
    `rg "Unity.Netcode" Assets/Scripts/Sim/` stays empty; `rg "Unity.Services" Assets/Scripts/Sim/`
    stays empty; lint-arch green. Close against `M64`'s gate if that file has shipped; if
    it has not, criterion 4 still needs two player builds on two networks.

Rejected: host migration (M45). Rejected: Unity Lobby (M64 — the join code is enough).
Rejected: Steam / Facepunch as this slice's path (real appid cost; 480 is not a
distribution path). Rejected: port forwarding and Tailscale as the shipping answer
(Tailscale is the zero-engineering path *until this ships*, per M64). Rejected: allocating
after Bastion loads — Host confirmation could not show the code without a second session
object.

## Supporting work

- **Driven by** Main Menu Host / Join (join code, or M58 IP when Relay is down) — here.
- **Shown by** the join code on Host confirmation and on pause, code field on Join — here.
- **Reads back as** both player builds in Bastion, a kill on both screens, a host-quit
  message, a clean return to MainMenu — criterion 4 is eyes-on; shutdown/rejoin is the
  fixture.

## Verification

1. Criteria 5, 6 — `gkb_verify_request.txt`; integration-check report green at current
   counts; UnityTransport shutdown/rejoin still covered; `rg "Unity.Netcode"
   Assets/Scripts/Sim/` empty; harness and lint-arch green. That run does **not** close
   criterion 4.
2. Criteria 1–4 — playthrough path: two Windows player builds on two networks. Host: Main
   Menu Host, code shown and copied. Client: Main Menu Join, paste the code, no typed IP.
   Client may join after the host is already in Bastion. One gatekeeper dies on both
   screens. Client quit → MainMenu, host continues; host quit → client sees the message
   and is on MainMenu. Repeat once. Offline / Relay down: M58 IP join still works.

## Do not build

| Not building | Why not |
|---|---|
| Host migration | Rejected in M45; a dead host ends the session |
| Unity Lobby | Join code is the session; a server list is a public-demo feature (`M64`) |
| Steam / Facepunch transport | Deferred until a real Steamworks appid; not this month |
| Voice / presence cosmetics | `../../Brainstorms/co-op.md` — paint on a room that now has a door |
| Guest progression | Settled in `M65`; not this slice |
| A fourth always-on HUD chip for the code | Pause already exists; ask before a menu row |

## Watch

- The fixture cannot test MPPM, transports, or the join surface (`../../Brainstorms/co-op.md`).
  Criterion 4 is eyes-on or the slice is not closable.
- Relay's free tier is average monthly CCU, not peak. Re-read before any wider test (`M64`).
- `EnableSceneManagement` stays false; both machines still load Bastion themselves.
- Prefabs freeze serialized values: do not serialize Relay vs LAN; select it at host/join.
- A missing UGS project ID fails Host with a named refusal, not a hang.

## Depends on / Unblocks

Depends on M51 (full loopback run) and M58 (player build + menu Host/Join). Unblocks the
friends alpha session over the internet. `M66` does not include this.
