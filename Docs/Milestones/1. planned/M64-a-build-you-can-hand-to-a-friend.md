# M64 — A build you can hand to a friend

<!--
Status: later
Update trigger: when a gate item is added or retired, when the transport choice changes, or when a friends session finds a failure the gate did not catch
Last updated: 2026-08-15
-->

The **Friends Alpha gate**: a player build both seats can play, and a machine check that
fails the send traps. Recut 2026-08-15 (Zo): definition of done is a full loop from
**host and from client** — Ship upgrades, Bastion interactions, fight SFX/VFX — not a
zip that launches plus one networked kill.

**M60 closes against this gate.** M58 closed 2026-08-15 on player-build Host/Join connect
(local Player present). A build that fails any red item is not sent.

## Story

As a friend, I want to host or join a player build, shop on my own Ship, and drive the
Bastion loop from either seat, so a session is the game rather than a connect test the
host has to narrate.

## Why it matters

**Every prior co-op gate closed on loopback or on connect.** M51's in-run verbs and M50's
end screen exist in the editor fixture. M58's player-build path skips the Ship
(`MainMenuController.OnHost` / `OnJoinConnect` call `SceneFlow.LoadBastion`) and
`GameController.Terminals` refuses Wave start on the client (`"client cannot start a Wave"`,
same for Hatch-bank and Master Control). `TickClientLocal` never runs `TickPrepCommands`.
A friend on the Join seat can move and shoot; they cannot shop, start a Wave, juice, or
bank.

Leverage tier (`../../Working-Rules.md`): **#10, structure** for the Ship-before-Bastion
path and the prep RPCs; **#6, information flows** for the shareability checks. No combat
number moves.

## Kill criterion

The first two friends sessions after the gate exists. **If either session loses more than
fifteen minutes to a problem the gate should have caught, or a friend cannot start a Wave
or shop because they are the client, the gate is wrong and gains that item before the
next send.** If both sessions start clean and both seats drive the loop, the gate stops
growing.

## The transport decision this gate assumes

Recorded here because the gate's networking items depend on it, and because the research is
current as of 2026-08-13.

**Unity Relay is free at this scale and is the answer to "usage-based and free."** The free
tier is 50 average monthly CCU and 150 GiB egress; four friends for a few hours a week is a
rounding error against that. It needs no Unity account per player (anonymous sign-in), no
Lobby service, and `CreateAllocationAsync` returns a join code the host can paste into chat.
Roughly fifty lines on top of the existing `UnityTransport`, with no transport swap
([NGO Relay docs](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/relay/relay.html),
[UGS pricing](https://unity.com/products/gaming-services/pricing)).

**Steam is the wrong tool this month.** A real appid is $100 with bank and identity
onboarding and a 30-day wait, tied to an actual store product. Appid 480 shares a global
lobby space with every other developer testing on it, and using another product's appid to
distribute builds breaches the Steamworks SDK Access Agreement. `M60` is Relay.

**Tailscale is the zero-engineering path available today.** It is free for personal use at
six users, has best-in-class NAT traversal, survives CGNAT, and makes `M58`'s typed-address
build work unchanged over the internet with no code at all. Use it for the first session if
Relay has not landed. Not a shipping answer, because you cannot ask strangers to install a
VPN, but it is the correct answer for four friends this month.

**Port forwarding is not recommended.** CGNAT makes it impossible on many modern
connections, and detecting that costs an evening.

## Acceptance criteria

### The gate exists and is enforced where it can be

1. **`Docs/Play/` gains nothing; this is a builder document.** The checklist lives at
   `../../Release-Gate.md` with every item stating what the player experiences when it
   fails, so nobody argues an item away without knowing its cost.
2. **`Tools/release-gate/run.sh` fails the build on any automatable red item**, and runs
   from `Tools/verify/run.sh` beside the existing checks. It reads `PlayerSettings` and the
   active build profile. **Ask before adding a menu row** (`../../Architecture/README.md`).
3. **Every item is red or amber.** Red blocks the send. Amber is recorded and sent anyway.
   An item with no colour is not a gate item.

### Red, machine-checkable

4. **Editor is patched against CVE-2025-59489**, or the Unity Application Patcher has been
   run on the output. Defender blocks builds that are not.
5. **Development Build, Autoconnect Profiler, Deep Profiling Support and Script Debugging
   are all off** in the active Build Profile.
6. **Company Name, Product Name and Version are set** and are not `DefaultCompany`. The
   save and log folder is named from these, and the version string is what tells you who is
   on which build.
7. **`Application.version` is visible in the main menu** and is in the zip filename.
8. **Fullscreen Mode is Fullscreen Window**, Default Is Native Resolution on, Resizable
   Window on, Allow Fullscreen Switch on. Exclusive fullscreen on the wrong monitor with no
   settings menu is an unrecoverable state for the player.
9. **Run In Background is on**, and the cursor unlocks on focus loss.
10. **Auto Graphics API is on, or Direct3D11 is first.** Vulkan-only or DX12-only kills
    integrated-graphics laptops.
11. **`targetFrameRate` is 60 and `vSyncCount` is 1** outside the fight, so a menu does not
    cook a laptop.
12. **Dev keys, cheat commands and debug overlays are compiled out** of a release build
    (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`). M15 shipped `F1`–`F9` on the shortcut panel
    on the stated reasoning that the player is the developer. That is no longer true.

### Red, human-checked — the zip is operable

13. **The zip was extracted and run on a machine that is not the build machine**, with
    `AppData/LocalLow/<Company>/<Product>` and `HKCU\Software\<Company>\<Product>` deleted
    first. **Running the exe from inside the zip is the single most common launch failure**,
    ahead of missing redistributables, which modern Unity does not need.
14. **Esc reaches a Quit from every scene**, including mid-fight.
15. **A failed join shows a reason and times out inside ten seconds.** A spinner that never
    resolves is the worst possible outcome, because nobody can tell a host that is not
    running from a firewall from a wrong address from a bug, and the evening goes to
    guessing. Surface `NetworkManager.DisconnectReason`.
16. **Audio verified on another machine**, including a device change after launch.
17. **`run-windowed.bat` ships beside the exe** with
    `-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11`, as the escape
    hatch for the display and GPU cases the gate cannot predict. The Unity resolution dialog
    was removed after 2019.1 and does not exist to fall back on.
18. **Dual-box chrome.** `Tools/player-session/run.ps1` launches with normal window
    chrome (title bar, min/max). `-popupwindow` is rejected for eyes-on; it hid those
    handles on the 2026-08-15 dual-box. `-gkb-host` / `-gkb-join` may still skip Splash
    into Bastion for the connect smoke; that skip is not the playthrough.

### Red, human-checked — both seats play the loop

19. **Host and Join go through that player's own Ship before Bastion.** Menu Host / Join
    call `SceneFlow.LoadShip()`, not `LoadBastion()`. `NetJoinIntent` stays pending across
    Ship and is consumed on Bastion `NetSession.Awake` after EMBARK, same as today. ARMORY,
    CREW QUARTERS and FABRICATION are live on that Ship (own `GameSession` save, own
    `BankedSalvage`). **Not a shared ship** — M50/M57 stay: the host does not walk the
    client's Ship. M57's additive down-wait Ship still refuses those three stations.
    **Host EMBARKs first** so `StartHost` is listening before the client EMBARKs; a client
    EMBARK with no host paints the existing named join refusal and leaves them on Ship or
    MainMenu, not a hang.
20. **A completed ARMORY change on that Ship is the gun in Bastion.** Equip or one wheel
    purchase; the run-begins log names that loadout. True for the host seat and the client
    seat. CREW / FAB open and can complete a purchase when banked allows; they are the same
    local-save path, not a second netcode surface.
21. **Client prep verbs apply once, server-side**, with the same range and Prep gates as
    the host. `E` select, `+/-` juice, `T` start at a Field terminal, at the Hatch
    (Hatch-bank), and at Master Control. Local `StartTerminalWave` / `StartHatchBankWave` /
    `StartMasterControl` keep the client refuse (they must not fork a sim). The RPC is the
    path. Intensity on the client HUD reads a mirrored siphon level, not the frozen local
    sim.
22. **M51 verbs in a player build**, not only the loopback fixture: client repair, decoy
    place, concussive or pin — each applies once on the host, visible or readable on both
    screens.
23. **Player-build fight (deferred from M58, Zo 2026-08-15).** Two player builds (or
    player + editor): Host / Join, each seat kills one server-spawned gatekeeper, death on
    both screens. Both windows show fight HUD. The firing machine shows muzzle and plays
    the bound fire one-shot on the SFX bus. Connect-only is not enough to send. Remote-gun
    replication is M66, not this slice.
24. **Both seats see the end screen** (M50 `ShowEndScreenClientRpc`) and leave clean
    (client MainMenu, host Ship, session restartable).
25. **The player action:** two player builds on LAN (Relay is M60). Run the loop once as
    host and once as client (swap seats). Each seat: Ship ARMORY change → EMBARK (host
    first) → client-started Field Wave → one kill with muzzle and fire SFX → repair →
    decoy → Hatch-bank started from the client seat at least once across the two runs →
    end screen on both. No `[Dev]` in either close log.

### Amber, recorded and sent anyway

26. **SmartScreen will warn**, because the exe is unsigned. A certificate is the only real
    fix and is not worth it yet. The mitigation is the send message.
27. **Antivirus may quarantine the download**, which the friend sees as a missing file
    rather than a warning. Check the build on VirusTotal before sending and name what to
    whitelist.
28. **The one-page send message ships with the build** and covers: extract the whole folder
    first, SmartScreen More info then Run anyway, what to whitelist, the host ticks both
    Private and Public on the firewall prompt, host EMBARKs before the client does, where
    to report a problem, and the `Player.log` path.

## Implementation

**Create** `../../Release-Gate.md` with the items above, each stating its player-visible
failure.

**Create** `Tools/release-gate/run.sh` plus a small editor reporter that dumps the relevant
`PlayerSettings` and build-profile flags to JSON, so the shell check has something to read.
Same sentinel pattern as `gkb_verify_request.txt`.

**Change** `Tools/verify/run.sh` to call it.

**Change** `MainMenuController.OnHost` / `OnJoinConnect` — `SceneFlow.LoadShip()` after
setting `NetJoinIntent`. Do not consume the intent on Ship. Do not add `NetSession` to
Ship. `PlayerLaunch.TryBootFromCommandLine` stays `LoadBastion` (connect harness).

**Change** `NetJoinIntent` comment — pending survives Ship; consumed on Bastion after
EMBARK. `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` still clears on domain
reload only.

**Create** `Assets/Scripts/Game/Net/NetPrep.cs` on the avatar prefab (`NetVerbs.cs` stays
the in-run combat verbs). RPC surface, server-validated:

- `RequestJuiceServerRpc(int delta)` → `GameController` `TryJuice` on the host sim.
- `RequestStartTerminalServerRpc(string terminalId)` → `StartTerminalWave` after range
  (`Interactor` 3.2 m to that `BastionTerminal`) and `CanSelectTerminal`.
- `RequestStartHatchBankServerRpc()` → `StartHatchBankWave` after range to the Hatch
  repair point.
- `RequestStartMasterServerRpc(string terminalId)` → `StartMasterControl` after range
  and kind check.

Client `TickClientLocal` gains the Prep input path (`E` / `+/-` / `T` against
`_interactor.Current`) and sends those RPCs; it does not call `StartTerminalWave` locally.
Server ignores a second start while Assault is live (existing refuse). Single-application:
no juice tick and no Wave start on the client instance.

**Change** `NetRunMirror` — `NetworkVariable<int> Intensity` written on change from
`Sim.Siphon.Level`. Client fight HUD Intensity row reads it. Unbanked is already mirrored.

**Change** `HudController.Fight` — client Intensity from the mirror, same branch as
Unbanked / Banked.

**Create** `Assets/Tests/PlayMode/NetPrepTests.cs` on the M48 fixture: client juice
changes host siphon once; client start Field Wave puts host `Assault == Assault`; client
Hatch-bank start when the fixture can arm it; empty-range refuse logs and does not start.
`NetVerbTests` stay green.

**Change** docs: Roadmap row, Changelog; `../../Brainstorms/co-op.md` Watch notes
own-Ship-before-Bastion as M64, shared ship still absent. Close against this gate is what
M60 waits on.

Rejected: **a code-signing certificate.** Amber item, real cost, no benefit until there are
strangers.

Rejected: **Unity Lobby.** Relay's join code is enough; a browsable server list is a public-demo
feature.

Rejected: **port forwarding and UDP hole punching.** More work than Relay and defeated by
CGNAT.

Rejected: **a shared Ship scene.** Own save, own stations, then Bastion. Cheapest path that
matches M51 (loadout from local save) and M65 (inherit nothing). Shared-ship was refused in
M50 and M57.

Rejected: **moving `NetSession` onto Ship** so the host can listen while shopping. Host
EMBARKs first. Join-in-Tethered is already M60 criterion 2.

Rejected: **guest salvage split, remote-gun VFX, Whispers loop, hit-confirm pass,
five-Wave dry run.** M65 / M66 / M69 / this file's playthrough. This gate is that both
seats can drive the loop, not that the evening is a five-Wave clear on a fresh save.
(M67 cancelled — M53 already allows convert after Wave 1.)

Rejected: **growing the gate speculatively.** It grows only when a session finds something,
per the kill criterion.

## Supporting work

- **Driven by** Main Menu Host / Join, Ship Interact (ARMORY / CREW / FAB / NAV EMBARK),
  Bastion `E` `+/-` `T`, repair hold, decoy, ability, fire — here or already bound.
- **Shown by** Ship stations, Intensity HUD, fight HUD, muzzle, fire SFX, end screen —
  here.
- **Reads back as** run-begins loadout, `[Net]` start/kill/end lines once on the host,
  mirrored Intensity / Unbanked / Hatch, end screen on both — here.

Owned elsewhere and **not required to close this file:**

| Piece | Owner | If it slips |
|---|---|---|
| Relay join code | M60 | LAN / Tailscale send still uses this gate |
| Split wallets / guest banking | M65 | Run salvage still the host wallet; Ship shops the local save |
| Whispers loop | M69 | Bound fire / reload / footstep one-shots still must play (criterion 23) |
| Hit-confirm pass, per-kind tells, player identity | M66 | Existing muzzle particles and networked death are the bar |
| Compendium from pause | M62 | Session is playable without it |

## Verification

1. Criteria 4 to 12 — `Tools/verify/run.sh` red on a deliberately bad build (flip
   Development Build on) and green on a good one. A gate that cannot fail is not a gate.
2. Criteria 21 — `NetPrepTests` Passed in the integration-check report; `NetVerbTests`
   still Passed; `rg "Unity.Netcode" Assets/Scripts/Sim/` empty.
3. Criteria 13 to 18, 19, 20, 22 to 25 — one dry run per seat on player builds, profile
   folders deleted first on the second machine. Host EMBARK then client EMBARK. Log: ARMORY
   change, run-begins loadout, client-started Field Wave once on the host, one
   `[Net] hit` / death per seat, repair spend once, decoy spawn, Hatch-bank started from
   the client at least once, `[Net] end` on both, no `[Dev]`. Dual-box harness has normal
   window chrome.
4. Criterion 15 — start a client with no host running and confirm a named failure inside
   ten seconds.

Playthrough path — from a player-build Main Menu, not `-gkb-host`: Host → Ship → ARMORY →
EMBARK. Client → Join → Ship → ARMORY → EMBARK. Client `E` `T` at T1. Both fight. Swap
seats, repeat. Confirms criteria 19–25.

## Watch

- **The human half is the half that rots.** Machine items stay true; items 13 to 25 get
  skipped when a send feels urgent. That is exactly when they matter.
- **This gate says both seats can drive the loop.** It does not say the game is fun.
  Do not let a green gate stand in for a played friends session.
- **Host wallet during the run is still the M51 purse.** A client who juiced or repaired
  spent the host's Unbanked. Their Ship shops used their own Banked. Confusing until M65;
  do not "fix" it in this slice.
- **Relay's free tier is generous but not infinite.** It is measured in average monthly CCU,
  not peak, so it is a rounding error at four players. Worth re-reading before any wider
  test.

## Depends on / Unblocks

**Depends on M51** (in-run verbs), **M57** (down-wait Ship stays refuse-to-shop), **M58**
(player-build Host/Join). Document and machine checks can still land without a friends send.

**Gates M60**, and is the marker for the alpha sentence: two friends install a build,
join without Zo touching a keyboard, play a full run, and see an ending. The five-Wave
clear is that session, not this file's dry run.
