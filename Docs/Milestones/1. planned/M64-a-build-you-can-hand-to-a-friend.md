# M64 — A build you can hand to a friend

<!--
Status: later
Update trigger: when a gate item is added or retired, when the transport choice changes, or when a friends session finds a failure the gate did not catch
Last updated: 2026-08-13
-->

The **Friends Alpha gate**: one checklist that says whether a build is safe to send, and an
editor check that fails the ones a machine can judge. Nothing here is a feature. It exists
so a friends session is spent playing rather than troubleshooting, and so "is it ready"
stops being a feeling.

**M58 and M60 close against this gate.** A build that fails any red item is not sent.

## Story

As the builder, I want a single pass/fail marker for shareability, so that I know the
difference between a build that runs on my machine and a build that runs on someone else's.

## Why it matters

**Every gate so far closed in the editor, PlayMode, or MPPM loopback.** None of them says
anything about a zip arriving on a stranger's Windows machine. The failure modes are
well-documented, cheap to prevent, and expensive to hit live, because each one costs a
whole evening of four people's time.

**And one of them will actively block the build.** CVE-2025-59489 affects Unity 2017.1
through Unity 6 on Windows, and **Microsoft Defender was updated to detect and block it**.
A build made on an unpatched editor is not warned about, it is stopped
([Unity remediation](https://unity.com/security/sept-2025-01/remediation)).

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No number moves and no
mechanic changes. What changes is that shareability is measured instead of assumed.

## Kill criterion

The first two friends sessions after the gate exists. **If either session loses more than
fifteen minutes to a problem the gate should have caught, the gate is wrong and gains that
item before the next send.** If both sessions start clean, the gate is doing its job and
stops growing.

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
distribute builds breaches the Steamworks SDK Access Agreement. `M60`'s Steam premise
should be re-cut to Relay.

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

### Red, human-checked

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

### Amber, recorded and sent anyway

18. **SmartScreen will warn**, because the exe is unsigned. A certificate is the only real
    fix and is not worth it yet. The mitigation is the send message.
19. **Antivirus may quarantine the download**, which the friend sees as a missing file
    rather than a warning. Check the build on VirusTotal before sending and name what to
    whitelist.
20. **The one-page send message ships with the build** and covers: extract the whole folder
    first, SmartScreen More info then Run anyway, what to whitelist, the host ticks both
    Private and Public on the firewall prompt, where to report a problem, and the
    `Player.log` path.

## Implementation

**Create** `../../Release-Gate.md` with the items above, each stating its player-visible
failure.

**Create** `Tools/release-gate/run.sh` plus a small editor reporter that dumps the relevant
`PlayerSettings` and build-profile flags to JSON, so the shell check has something to read.
Same sentinel pattern as `gkb_verify_request.txt`.

**Change** `Tools/verify/run.sh` to call it. **Change** M58 and M60 to close against this
gate. **Change** `../../Decisions.md` to record Relay as the transport and Steam as
deferred with its reason.

Rejected: **a code-signing certificate.** Amber item, real cost, no benefit until there are
strangers.

Rejected: **Unity Lobby.** Relay's join code is enough; a browsable server list is a public-demo
feature.

Rejected: **port forwarding and UDP hole punching.** More work than Relay and defeated by
CGNAT.

Rejected: **growing the gate speculatively.** It grows only when a session finds something,
per the kill criterion.

## Verification

1. Criteria 4 to 12 — `Tools/verify/run.sh` red on a deliberately bad build (flip
   Development Build on) and green on a good one. A gate that cannot fail is not a gate.
2. Criteria 13 to 17 — one full dry run on a second machine, from zip to a joined session,
   with the profile folders deleted first.
3. Criterion 15 — start a client with no host running and confirm a named failure inside
   ten seconds.

## Watch

- **The human half is the half that rots.** Machine items stay true; items 13 to 17 get
  skipped when a send feels urgent. That is exactly when they matter.
- **The gate is not a quality bar for the game.** It says the build is operable, not that it
  is fun. Do not let a green gate stand in for a played session.
- **Relay's free tier is generous but not infinite.** It is measured in average monthly CCU,
  not peak, so it is a rounding error at four players. Worth re-reading before any wider
  test.

## Depends on / Unblocks

**Depends on nothing.** The document and the check can land before M58.

**Gates M58 and M60**, and is the marker for the alpha sentence: two friends install a build,
join without Zo touching a keyboard, play a full run, and see an ending.
