# M60 — A join a friend can use

<!--
Status: later
Update trigger: re-cut against M51 and M58's landed menu/session shapes before starting
(Zo, 2026-08-13: author now, same pattern as M50/M51). Also when the Relay / Steam
decision in Decisions.md changes.
Last updated: 2026-08-14
-->

Graduates `../../Brainstorms/co-op.md` "a join surface a friend can use". M58 is typed LAN
IP. This is the internet/friends path: **Unity Relay on the existing UnityTransport**,
recorded in `../../Milestones/1. planned/M64-a-build-you-can-hand-to-a-friend.md` and
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

Work starts on the day implementation begins. **If criterion 4 — two player builds, one
Relay join code, one kill on both screens — cannot be demonstrated within ten working days
of start, stop.** Record what Relay / Authentication / the UGS project blocked, and re-open
the transport decision in `../../Decisions.md` before writing more netcode. Same shape as
M48's NGO kill criterion.

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

Re-cut this file against whatever M58 actually landed for the menu before writing code.
The menu Host / Join / intent / Bastion-owned `NetSession` are M58's; this slice adds Relay
on that UnityTransport and changes the Join contents to a code.

1. **Change** `Packages/manifest.json` — add `com.unity.services.relay` and
   `com.unity.services.authentication`. Resolve versions compatible with NGO 2.13.1 at
   start and record them here. Do not add `com.community.netcode.transport.facepunch`.
2. **Open, labelled:** Unity Cloud / UGS project ID is Zo's. Host cannot allocate until
   the Unity project is linked. Anonymous sign-in; no per-player Unity account.
3. **Change** `NetSession.cs` — friends Host: `SignInAnonymouslyAsync`,
   `CreateAllocationAsync(maxConnections: 3)` (host plus two guests; `M65` raises the
   count, not this slice), `GetJoinCodeAsync`, `UnityTransport.SetRelayServerData`, then
   `StartHost` as M58. Friends Join: `JoinAllocationAsync(code)`, `SetRelayServerData`,
   `StartClient`. LAN fallback keeps M58 `SetConnectionData`. Do not enable NGO scene
   management. Do not enable host migration. Do not add Unity Lobby.
4. **Change** Main Menu — Host: after a successful allocation, show the join code on the
   Host confirmation and copy it. Join panel default is a code field + Connect; LAN IP
   fields remain as the fallback row. Invalid / empty code refuses on the panel without
   loading Bastion. Allocation or auth failure paints a named refusal (same `LastError`
   path as M58). Rejected: a browsable server list. Rejected: requiring both players to
   still be on the menu — join-in-Tethered is criterion 2.
5. **Change** pause — while a Relay session is live, the join code is readable there so a
   late joiner can be told it. Do not add a new always-on HUD row
   (`../../Architecture/README.md`).
6. **Change** disconnect — client quit: `NetworkManager.Shutdown()`, `SceneFlow.LoadMainMenu`,
   host session continues. Host quit: client sees a visible message, log `[Net] host left`,
   return to MainMenu. Reuse M50's clean shutdown; add the host-quit *message* here if M50
   only disconnects.
7. **Change** F10/F11 — editor-only once M58/M59 have landed; not the friends path.
8. Headless: no new Relay fixture. Keep UnityTransport shutdown/rejoin coverage.
9. **Change** docs: Roadmap row, Changelog; co-op.md join entry graduates on close.
   `rg "Unity.Netcode" Assets/Scripts/Sim/` stays empty; lint-arch green. Close against
   `M64`'s gate if that file has shipped; if it has not, criterion 4 still needs two
   player builds on two networks.

Rejected: host migration (M45). Rejected: Unity Lobby (M64 — the join code is enough).
Rejected: Steam / Facepunch as this slice's path (real appid cost; 480 is not a
distribution path). Rejected: port forwarding and Tailscale as the shipping answer
(Tailscale is the zero-engineering path *until this ships*, per M64).

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
