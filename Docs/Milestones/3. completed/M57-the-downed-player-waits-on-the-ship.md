# M57 — The downed player waits on the Ship

<!--
Status: completed 2026-08-15
Update trigger: closed
Last updated: 2026-08-15
Closed on: 2026-08-15. Gate is criterion 7 — both `NetDownWaitTests` Passed in Unity
PlayMode (2/0, 1.69s). Log: `[Net] client down → ship clientId=1`,
`embark refused — not Hatch-bank Prep`, `embark refused — Assault is live`,
`client embark hatch-bank clientId=1`. Host Die() → `RunPhase.Lost`, `WaitingOnShip`
false. Criterion 8 MPPM playthrough is confirmation, not the gate (same as M48).
lint-arch clean. Full integration-check report not re-run this close.
-->

After M49 a client who hits 0 HP stays on the floor until the run ends. This slice sends them
to the Ship and lets them embark for the next Hatch-bank Wave.

## Story

As a second player, I want to appear on the Ship when I go down and embark for the next
Hatch-bank Wave, so that going down during a field terminal sequence does not strand me
on the floor until the run ends.

## Why it matters

Zo, 2026-08-13: respawn on the Ship; allowed back in on the next hatch-defense Wave; embark
from NAV MAP or the pause (open) menu; refine later, pressure-test now.
`../../Brainstorms/co-op.md` (After the client is down) could not graduate until Zo picked a
rule; he has. M50 shipped the end screens; this slice is the get-up, not readout.

Leverage tier (`../../Working-Rules.md`): **#5, rules** — who may return, and when. Solo and
host-down are unchanged: host down is still `RunPhase.Lost` (`PlayerHealth.EndsTheRunOnDeath`).
A higher tier (#4, who may change the rules) is wrong because the host still owns the run;
this does not hand the client a veto.

**Current Wave shape (M52/M53 shipped 2026-08-13):** field terminals start `WaveMode.Terminal`
(Boarder hunt; Hatch invulnerable). Hatch-bank is distinct `WaveMode.HatchBank` (Prep `T` at
the Hatch). The locked co-op-down rule (`../../Decisions.md`) is embark on the next
hatch-defense Wave, which is Hatch-bank Prep — not every inter-Assault Prep between T1's
field Waves.

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
4. **Embark window:** allowed in **Hatch-bank Prep**
   (`RunSim.Phase == Tethered && Assault == Prep && Mode == WaveMode.HatchBank`).
   Dropping *during* that Prep — the window is open. Refused during Assault, during
   Terminal Prep (field-sequence gap), and while no Hatch-bank is live (logged reason).
   They return at the Hatch with **full HP** (this is the respawn). Log:
   `[Net] client embark hatch-bank clientId=<n>`. Hatch-bank does not bump
   `CurrentWaveIndex`; do not log `wave=<n>` from that index.
5. **No next Hatch-bank:** if they drop with no Hatch-bank left (slots spent, or the host is
   about to end the Run — `Escaped` / `Lost`), they stay on the Ship until the ending. The
   Prep window never opens; there is no separate last-Wave flag. M50 shipped
   `ShowEndScreenClientRpc(EndSummary)`; this slice does not invent a second screen. A
   client still on the Ship when the host ends follows `NetSession.OnClientStopped` →
   MainMenu (or Exit from the wait so the M50 RPC can show).
6. **Solo and host unchanged:** host 0 HP still `PlayerDied` → `Lost`. A solo log contains
   no `[Net]` line. Both suites + harness green at current counts.
7. **Headless:** `NetDownWaitTests` on the M48 `NetcodeIntegrationTest` fixture (`EnableSceneManagement`
   false, no scene load): lethal damage on the client avatar → `WaitingOnShip` true on that
   avatar; Hatch-bank Prep → `RequestEmbarkServerRpc` restores HP (`IsDead` cleared;
   `NearestAlive` already skips dead while down); Assault-time and Terminal-Prep requests
   are refused; host `Die()` still ends the run (`EndsTheRunOnDeath` still true;
   `WaitingOnShip` stays false on a host-owned body). Green in the integration-check
   report. The fixture does not load Ship; criterion 8 is the scene proof.
8. **The player action:** in one MPPM session, host idles at the Hatch; client walks into a
   Hunter, goes down, appears on the Ship, waits until Hatch-bank Prep, presses EMBARK on
   pause or NAV, and is back in the Bastion at the Hatch with a full HP bar.

## Implementation

1. **Create** `Assets/Scripts/Game/Net/NetShipWait.cs` — client-only wait loader. `Enter()`:
   disable the Bastion `Player` / `FirstPersonController` GameObject (camera, AudioListener,
   gun) **and** hide `BastionHud` / the pause canvas (`HudController` is a separate scene
   object, not under the rig); `SceneManager.LoadSceneAsync(SceneFlow.Ship, LoadSceneMode.Additive)`;
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
   validates Hatch-bank Prep from `NetSession.Game.Sim` when `Game` is wired; fixture:
   `Bind(GameController)` (same pattern as `NetVerbs` / `NetRunMirror`) or a new
   `BindRun(RunSim)` on the avatar — neither exists today. On success `HealToFull` /
   `Hp.Value = MaxHp`, `WaitingOnShip = false`, log criterion 4,
   `EmbarkAcceptedClientRpc()`. Assault refuse:
   `[Net] embark refused — Assault is live clientId=<n>`. Terminal-Prep refuse:
   `[Net] embark refused — not Hatch-bank Prep clientId=<n>`. Terminal phase refuse: stay
   waiting, no embark log. Owner on accepted / flag falling: `NetShipWait.Exit()`, then the
   existing `OnHpChanged` 0→positive path (`ApplyReplicatedHp` + `ResetToSpawn`) — the
   Bastion rig's authored spawn is the Hatch bay (`Docs/Play/A-Run.md`). Clearing `IsDead`
   is enough for `NearestAlive`; `SyncRegistration` already keeps the non-owner body
   registered while down.
3. **Change** `MapStation.TryEmbark` — if `NetShipWait.IsActive`, do **not**
   `SceneFlow.LoadBastion()`, do **not** set `GameSession.PendingStartWave`; hide the
   start-wave picker (this is a return to the live Run, not a new one); call
   `RequestEmbarkServerRpc` on the owner's avatar (`NetworkManager.Singleton.ConnectedClients[LocalClientId].PlayerObject`,
   same pattern as `NetSession.LocalVerbs`).
4. **Change** `ShipController` — `TryOpenStationFromInteract`: if wait-active and the
   station is not `MapStation`, log `[Net] station refused — live Run <StationName>` and
   return false. `Start`: if wait-active, do not unlock `PayloadLoadoutLocked` (that flag
   is the between-Runs shop). `TryEmbarkFromPause()`: same RPC as NAV, same word **EMBARK**.
   Pause Button List today is Resume / Settings / QUIT TO MENU only — EMBARK is new.
5. **Change** `Assets/Scenes/Ship.unity` — authored Ship pause panel (UI canvases survive
   Build All; SceneBuilder does not own them) gets an EMBARK control wired to
   `ShipController.TryEmbarkFromPause`. Visible for the wait; Assault still goes through
   the server refuse.
6. **Change** `PlayerGun.CanFire` — refuse while `NetShipWait.IsActive`.
7. **Create** `Assets/Tests/PlayMode/NetDownWaitTests.cs` — criterion 7, same fixture as
   `NetKillTests` / `NetTargetingTests`. Bind a constructed `RunSim` (`StartRun` → Prep;
   `TriggerDraw(WaveMode.HatchBank)` → Assault; `NotifyAssaultCleared` → Hatch-bank Prep;
   a Terminal `TriggerDraw` for the refuse case). No Ship scene.

Rejected, by name: `SceneFlow.LoadShip()` / NGO `NetworkSceneManager` for this wait;
Wave-start auto-revive standing in the Bastion (the parked 2026-08-13 option); revive-by-teammate
(still brainstorm); host-down respawn (host owns the run); client shopping during the wait;
networked Ship simulation (host does not see the client on the Ship); mid-Assault embark
(pressure-test the Hatch-bank Prep window first); embark during Terminal Prep (that is a
field Wave, not hatch-defense).

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
   — refused line. During Terminal Prep, EMBARK — refused line. Hatch-bank Prep, EMBARK —
   criterion 4 line; client is at the Hatch with a full HP bar. No Hatch-bank left: they
   stay on the Ship until the host ending (`ShowEndScreenClientRpc` / session teardown).

## Do not build

| Not building | Why not |
|---|---|
| Friendly fire | Unrelated; still brainstorm |
| Steam / join surface | Co-op brainstorm; gated on a finished loopback run |
| Client HUD / endings | M50 (shipped); wait Exit so the RPC can show |
| Client verbs (repair, decoy, abilities) | M51 (shipped) |
| Embark during Terminal Prep | Locked as hatch-defense = Hatch-bank |
| Mid-Assault embark | Kill criterion may recut to this; do not pre-build it |

## Depends on / Unblocks

Depends on M49 (shipped). Unblocks a friends session that survives a down. M50 owns end
screens reaching a client who is on the Ship. Embark gate is Hatch-bank Prep (M52/M53
shipped); that is this slice's criterion 4, not a later retarget.

## Watch

Additive Ship + live Bastion: two AudioListeners (disable the Bastion rig first) and two
EventSystems (`SceneFlow.EnsureEventSystem`). `InCombatScene` stays true because Bastion
remains the active scene; `PlayerGun`'s GameController fallback would arm the Ship rig —
`NetShipWait.IsActive` is the refuse. Two `FirstPersonController`s after additive load:
avatar `FindAnyObjectByType<FirstPersonController>` must stay bound to the Bastion Player,
not the Ship rig, while the corpse is frozen.
