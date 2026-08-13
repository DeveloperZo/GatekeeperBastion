# M60 — A join a friend can use

<!--
Status: later
Update trigger: re-cut against M51 and M58's landed menu/session shapes before starting
(Zo, 2026-08-13: author now, same pattern as M50/M51). Also when the Steam transport
decision in Decisions.md changes.
Last updated: 2026-08-13
-->

Graduates `../../Brainstorms/co-op.md` "Steam, and a join surface a friend can use". M58 is
typed LAN IP. This is the internet/friends path already decided in `../../Decisions.md`.

## Story

As a friend, I want to host or join from the Main Menu over Steam, so that a session does
not require a typed LAN IP or anyone touching the host's keyboard after I have the build.

## Why it matters

Alpha gate sentence: two friends install a build, join without Zo touching a keyboard.
Leverage tier (`../../Working-Rules.md`): **#10, structure** — how a session starts over
the internet. Completes the join surface. No host migration (rejected in M45).

Facepunch transport over Steam relay is the decided path, with its cost stated in
`../../Decisions.md` (community package `com.community.netcode.transport.facepunch` pins an
old Facepunch.Steamworks build). NGO was chosen on the AI-authorship criterion in that same
entry; transport is the most swappable layer, which is why this slice is last of the join
stack and carries a kill criterion.

## Kill criterion

Work starts on the day implementation begins. **If criterion 4 — two Steam accounts, two
player builds, one kill on both screens — cannot be demonstrated within ten working days of
start, stop.** Record what the Facepunch transport blocked, and re-open the transport
decision in `../../Decisions.md` against Unity Relay / a different Steam transport before
writing more netcode. Same shape as M48's NGO kill criterion.

## Acceptance criteria

1. Main Menu Host / Join uses the **Facepunch Steam transport** when Steam is running; typed
   LAN IP from M58 remains as a fallback when Steam is not running (offline / no Steam).
2. Join-in-Tethered is allowed (client can connect while the host is already in Bastion).
   Clean disconnect returns the client to MainMenu. Host quit messages the client (not a
   silent stall).
3. F10/F11 are not the friends path (editor-only after M58/M61).
4. **The player action:** two friends, two player builds, Steam overlay logged in: one Host,
   the other Join from a friends list or lobby — no typed IP, no editor, no Zo on either
   keyboard after launch. One gatekeeper dies on both screens. Session ends clean (M50
   shutdown if M50 has shipped; otherwise M48 shutdown).
5. Headless fixture cannot prove Steam — **eyes-on for criterion 4**. Fixture still proves
   shutdown/rejoin on the existing UnityTransport path (M50 criterion 4 if present, else
   M48). The fixture cannot test transports (`../../Brainstorms/co-op.md` Watch).
6. Solo unchanged; `Sim/` has no netcode references.

## Implementation

Re-cut this file against whatever M58 actually landed for the menu before writing code.
The menu Host / Join / intent / Bastion-owned `NetSession` are M58's; this slice swaps the
transport and the Join contents when Steam is up.

1. **Change** `Packages/manifest.json` — add `com.community.netcode.transport.facepunch` as
   named in `../../Decisions.md`. Resolve the version compatible with NGO 2.13.1 at start
   and record it here. Init Facepunch.Steamworks the way that package requires.
2. **Open, labelled:** Steam App ID is Zo's when a Steamworks app exists. Until then use
   **480 (Spacewar)**, the Facepunch.Steamworks development id. Nothing here ships a store
   page.
3. **Change** `NetSession.cs` — when Steam is running, assign the Facepunch transport and
   start host/client through it; when it is not, keep M58's UnityTransport +
   `SetConnectionData`. Do not enable NGO scene management. Do not enable host migration.
4. **Change** Main Menu Join — Steam running: list **Steam lobbies whose owner is a friend
   currently in a Gatekeeper Bastion lobby** (refresh on open). Click a row to join. Host
   creates that lobby then `LoadBastion` / `StartHost` as M58. Steam not running: M58 IP
   fields unchanged. Rejected: typed Steam IDs (worse than typed IP). Rejected: requiring
   both players to still be on the menu — join-in-Tethered is criterion 2.
5. **Change** disconnect — client quit: `NetworkManager.Shutdown()`, `SceneFlow.LoadMainMenu`,
   host session continues. Host quit: client sees a visible message (MainMenu refusal line
   or a disconnect banner), log `[Net] host left`, return to MainMenu. Reuse M50's clean
   shutdown if it has shipped; add the host-quit *message* here if M50 only disconnects.
6. **Change** F10/F11 — editor-only once M58/M61 have landed; not the friends path.
7. Headless: no new Steam fixture. Keep UnityTransport shutdown/rejoin coverage (extend
   `NetRunMirrorTests` / `NetKillTests` only if M50 criterion 4 is not already there).
8. **Change** docs: Roadmap row, Changelog; co-op.md Steam entry graduates on close.
   `rg "Unity.Netcode" Assets/Scripts/Sim/` stays empty; lint-arch green.

Rejected: host migration (M45). Rejected: Unity Relay as the primary path — metered; it is
the named fallback only if this kill criterion fires.

## Supporting work

- **Driven by** Main Menu Host / Join (lobby row, or M58 IP when Steam is down) — here.
- **Shown by** the lobby list when Steam is running, IP fields when it is not — here.
- **Reads back as** both player builds in Bastion, a kill on both screens, a host-quit
  message, a clean return to MainMenu — criterion 4 is eyes-on; shutdown/rejoin is the
  fixture.

## Verification

1. Criteria 5, 6 — `gkb_verify_request.txt`; integration-check report green at current
   counts; UnityTransport shutdown/rejoin still covered; `rg "Unity.Netcode"
   Assets/Scripts/Sim/` empty; harness and lint-arch green. That run does **not** close
   criterion 4.
2. Criteria 1–4 — playthrough path: two Steam accounts, two Windows player builds, overlay
   logged in. Host: Main Menu Host (lobby created). Client: Main Menu Join, pick the host's
   lobby, no typed IP. Client may join after the host is already in Bastion. One
   gatekeeper dies on both screens. Client quit → MainMenu, host continues; host quit →
   client sees the message and is on MainMenu. Repeat once. Offline (Steam quit): M58 IP
   join still works.

## Do not build

| Not building | Why not |
|---|---|
| Host migration | Rejected in M45; a dead host ends the session |
| Voice / presence cosmetics | `../../Brainstorms/co-op.md` — paint on a room that now has a door |
| Guest progression | Open design, same brainstorm; M50 banks to the host |
| Unity Relay as the ship path | Kill-criterion fallback only |

## Watch

- The fixture cannot test MPPM, transports, or the join surface (`../../Brainstorms/co-op.md`).
  Criterion 4 is eyes-on or the slice is not closable.
- Old Facepunch.Steamworks pin is the known maintenance cost (`../../Decisions.md`).
- `EnableSceneManagement` stays false; both machines still load Bastion themselves.
- Prefabs freeze serialized values: do not serialize the transport choice; select it at
  host/join from Steam-running-or-not.

## Depends on / Unblocks

Depends on M51 (full loopback run — co-op.md graduation test) and M58 (player build +
menu). Unblocks the friends alpha session over the internet.
