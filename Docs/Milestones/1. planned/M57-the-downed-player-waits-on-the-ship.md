# M57 — The downed player waits on the Ship

<!--
Status: later
Update trigger: when the wait window, ship-load path, or M52/M53 Hatch-bank retarget changes
Last updated: 2026-08-13
-->

After M49 a client who hits 0 HP stays on the floor until the run ends. This slice sends them
to the Ship and lets them embark for the next hatch-defense Wave.

## Story

As a second player, I want to appear on the Ship when I go down and embark for the next
hatch-defense Wave, so that going down in Wave two does not strand me on the floor until
the run ends.

## Why it matters

Zo, 2026-08-13: respawn on the Ship; allowed back in on the next hatch-defense Wave; embark
from NAV MAP or the pause (open) menu; refine later, pressure-test now.
`../../Brainstorms/co-op.md` (After the client is down) could not graduate until Zo picked a
rule; he has. Do not fold this into M50 — that slice is readout and endings.

Leverage tier (`../../Working-Rules.md`): **#5, rules** — who may return, and when. Solo and
host-down are unchanged: host down is still `RunPhase.Lost` (`PlayerHealth.EndsTheRunOnDeath`).
A higher tier (#4, who may change the rules) is wrong because the host still owns the run;
this does not hand the client a veto.

**Current Wave shape:** every Wave is hatch-defense (Siphon Draw `T`). "Hatch terminal wave"
in today's build is the next Wave. After M52/M53, Hatch-bank Waves are a distinct
`WaveMode.HatchBank`; retarget the window there (Watch). Do not build that retarget in this
slice.

## Kill criterion

The second friends session after this ships. **If a downed player quits during the Ship wait
in either of the first two friends sessions, stop and recut the wait** (mid-Assault embark,
or a shorter window). If nobody goes down, the criterion does not fire.

## Acceptance criteria

1. **Client down → Ship, not the floor.** At 0 HP the owner's local Bastion rig leaves the
   fight view and they are walking the Ship. The avatar drops from targeting (`NearestAlive`
   already skips dead — M49). Host stays in the Bastion; the run continues. Log:
   `[Net] client down → ship clientId=<n>`.
2. **`SceneFlow.LoadShip()` is not the path.** That call is `SceneManager.LoadScene` (single)
   and would unload Bastion, destroying `NetSession`. Chosen: **additive client-only load of
   Ship**; Bastion and `NetworkManager` stay loaded; `EnableSceneManagement` stays false
   (`NetSession` already sets it; F11 is still `127.0.0.1`). Additive Ship + live Bastion
   duplicates an EventSystem and an AudioListener. `SceneFlow.EnsureEventSystem` already
   collapses extra EventSystems (ShipController.Awake calls it). The extra listener is
   removed by disabling the Bastion rig before Ship loads. Host never loads Ship.
3. **Stations on that Ship wait:** NAV MAP embark is live; the Ship pause overlay has an
   EMBARK command (same word as NAV). ARMORY / CREW QUARTERS / FABRICATION are refused
   (logged) — no shopping on a live Run. Host meta is the only wallet (M50/M51).
4. **Embark window:** allowed in **Prep after the Assault they dropped in has cleared**
   (`RunSim.Phase == Tethered && Assault == Prep`). Dropping *during* Prep is already past
   that Assault — the window is open. Refused during Assault (logged reason). They return at
   the Hatch with **full HP** (this is the respawn). Log:
   `[Net] client embark wave=<n> clientId=<n>` (`n` is the 1-based Wave they are joining:
   `CurrentWaveIndex + 1`).
5. **No next Wave:** if they drop during the last Wave, or the host is about to end the Run
   (`Escaped` / `Lost`), they stay on the Ship until the ending. The Prep window never
   opens; there is no separate last-Wave flag. End screens are M50 — this slice does not
   invent them; if M50 has not shipped, they wait until the host session ends (M50 criterion
   4 still owns the screen).
6. **Solo and host unchanged:** host 0 HP still `PlayerDied` → `Lost`. A solo log contains
   no `[Net]` line. Both suites + harness green at current counts.
7. **Headless:** `NetDownWaitTests` on the M48 `NetcodeIntegrationTest` fixture (`EnableSceneManagement`
   false, no scene load): lethal damage on the client avatar → `WaitingOnShip` true on that
   avatar; Prep after clear → `RequestEmbarkServerRpc` restores HP and targeting registration;
   Assault-time request is refused; host `Die()` still ends the run (`EndsTheRunOnDeath` still
   true; `WaitingOnShip` stays false on a host-owned body). Green in the integration-check
   report. The fixture does not load Ship; criterion 8 is the scene proof.
8. **The player action:** in one MPPM session, host idles at the Hatch; client walks into a
   Hunter, goes down, appears on the Ship, waits until Prep, presses EMBARK on pause or NAV,
   and is back in the Bastion for the next Wave with a full HP bar.

## Implementation

1. **Create** `Assets/Scripts/Game/Net/NetShipWait.cs` — client-only wait loader. `Enter()`:
   disable the Bastion `FirstPersonController` GameObject (camera, AudioListener, gun, HUD
   canvases on `HudController`); `SceneManager.LoadSceneAsync(SceneFlow.Ship, LoadSceneMode.Additive)`;
   `SceneFlow.EnsureEventSystem()`. `Exit()`: `UnloadSceneAsync(Ship)`, re-enable the Bastion
   rig and HUD. `IsActive` is the local flag `PlayerGun` reads. **Do not** call
   `SceneFlow.LoadShip()` or `LoadBastion()`. **Do not** `SetActiveScene` to Ship — Bastion
   stays the active scene so NGO objects stay put. `InCombatScene` therefore remains true
   and `PlayerGun` would fire on the Ship rig because a live `GameController` is still
   present (`PlayerGun.CanFire` treats that as a combat fallback). Refuse fire while
   `NetShipWait.IsActive`.
2. **Change** `NetPlayerAvatar.cs` — `NetworkVariable<bool> WaitingOnShip` (server write).
   On server lethal HP for a non-host owner: set the flag, log criterion 1. Owner
   `WaitingOnShip` rising → `NetShipWait.Enter()`. `LateUpdate` must **not** copy the Ship
   rig onto the avatar (owner-authoritative `NetworkTransform` would otherwise teleport the
   corpse to Ship coordinates in the Bastion). Freeze at the death pose; host sees the
   capsule where they fell. `RequestEmbarkServerRpc()` (no args): owner-only; server
   validates `Phase == Tethered && Assault == Prep` from `NetSession.Game.Sim` when bound,
   else from `BindRun(RunSim)` for the fixture; on success `HealToFull` / `Hp.Value = MaxHp`,
   `WaitingOnShip = false`, log criterion 4, `EmbarkAcceptedClientRpc()`. Assault refuse:
   `[Net] embark refused — Assault is live clientId=<n>`. Terminal phase refuse: stay
   waiting, no embark log. Owner on accepted / flag falling: `NetShipWait.Exit()`, then the
   existing `OnHpChanged` 0→positive path (`ApplyReplicatedHp` + `ResetToSpawn`) — the
   Bastion rig's authored spawn is the Hatch bay (`Docs/Play/A-Run.md`). Re-registration is
   `SyncRegistration` once `IsDead` is false (`NearestAlive` sees them again).
3. **Change** `MapStation.TryEmbark` — if `NetShipWait.IsActive`, do **not**
   `SceneFlow.LoadBastion()`, do **not** set `GameSession.PendingStartWave`; hide the
   start-wave picker (this is a return to the live Run, not a new one); call
   `RequestEmbarkServerRpc` on the owner's avatar (`NetworkManager.LocalClient.PlayerObject`).
4. **Change** `ShipController` — `TryOpenStationFromInteract`: if wait-active and the
   station is not `MapStation`, log `[Net] station refused — live Run <StationName>` and
   return false. `Start`: if wait-active, do not unlock `PayloadLoadoutLocked` (that flag
   is the between-Runs shop). `TryEmbarkFromPause()`: same RPC as NAV, same word **EMBARK**.
5. **Change** `Assets/Scenes/Ship.unity` — authored Ship pause panel (UI canvases survive
   Build All; SceneBuilder does not own them) gets an EMBARK control wired to
   `ShipController.TryEmbarkFromPause`. Visible for the wait; Assault still goes through
   the server refuse.
6. **Change** `PlayerGun.CanFire` — refuse while `NetShipWait.IsActive`.
7. **Create** `Assets/Tests/PlayMode/NetDownWaitTests.cs` — criterion 7, same fixture as
   `NetKillTests` / `NetTargetingTests`. Bind a constructed `RunSim` (`StartRun` → Prep;
   `TriggerDraw` → Assault; `NotifyAssaultCleared` → Prep). No Ship scene.

Rejected, by name: `SceneFlow.LoadShip()` / NGO `NetworkSceneManager` for this wait;
Wave-start auto-revive standing in the Bastion (the parked 2026-08-13 option); revive-by-teammate
(still brainstorm); host-down respawn (host owns the run); client shopping during the wait;
networked Ship simulation (host does not see the client on the Ship); mid-Assault embark
(pressure-test the Prep window first).

## Supporting work

- **Driven by** going down (no new input) and EMBARK on NAV or the Ship pause — here.
- **Shown by** walking the Ship, and the EMBARK command on NAV and pause — here.
- **Reads back as** a full HP bar in the Bastion and the two `[Net]` lines — here.

## Verification

1. Criterion 7 — `gkb_verify_request.txt`; integration-check report green at current counts
   plus `NetDownWaitTests`; harness and lint-arch green.
2. Criterion 6 — both suites + harness at current counts; a solo Bastion log contains no
   `[Net]` line.
3. Criteria 1–5, 8 — playthrough path: MPPM, host F10, client F11. Host idles at the Hatch.
   Client walks into a Hunter, goes down. Client screen is the Ship (criterion 1 line in
   both logs). Client opens ARMORY — refused line. During Assault, EMBARK on pause or NAV
   — refused line. After clear, Prep, EMBARK — criterion 4 line; client is at the Hatch
   with a full HP bar. Last-Wave down: they stay on the Ship until the host ending (M50
   screen if shipped; otherwise until the host session ends).

## Do not build

| Not building | Why not |
|---|---|
| Friendly fire | Unrelated; still brainstorm |
| Steam / join surface | Co-op brainstorm; gated on a finished loopback run |
| Client HUD / endings | M50 |
| Client verbs (repair, decoy, abilities) | M51 |
| M52/M53 Hatch-bank retarget | Watch, not this gate |
| Mid-Assault embark | Kill criterion may recut to this; do not pre-build it |

## Depends on / Unblocks

Depends on M49. Unblocks a friends session that survives a down. M50 still owns end screens
reaching a client who is on the Ship. M52/M53 retarget the window to Hatch-bank — Watch,
not this gate.

## Watch

Additive Ship + live Bastion: two AudioListeners (disable the Bastion rig first) and two
EventSystems (`SceneFlow.EnsureEventSystem`). `InCombatScene` stays true because Bastion
remains the active scene; `PlayerGun`'s GameController fallback would arm the Ship rig —
`NetShipWait.IsActive` is the refuse. M52/M53: retarget "next Wave" to the next Hatch-bank
Wave (`WaveMode.HatchBank`).
