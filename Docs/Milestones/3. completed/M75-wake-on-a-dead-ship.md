# M75 — Wake on a dead Ship

<!--
Status: completed, Aug 2026
Update trigger: when Demo/0-wake-on-a-dead-ship.md re-stamps an assumption
Last updated: 2026-08-19
-->

Beat 0 plus the SaveVersion 9 demo flags: capsule wake, NAV repair, the ship
terminal that powers ARMORY and releases two intruders, the MK-1 grant, CREW
heal, EMBARK — and the v9 fields every later beat reads. Cut 2 of the demo
family.

## Story

As a new player, I want the first minutes to teach every verb the Bastion will
ask for — hold-repair, select-then-start, take a weapon, fight, heal — at
home, in order, so that by EMBARK nothing on the Bastion is a new verb except
the stakes.

## Why it matters

The live game opens with four live consoles and NAV that warns "no weapon"
then EMBARKs anyway (`../../Demo/0-wake-on-a-dead-ship.md`, Why this beat
exists). Arc: authored once-per-save onboarding; its cost is one scene's
furniture and gates, not a content treadmill.

This cut also owns **SaveVersion 9**: `DemoActive`, `DemoCapsuleOpened`,
`DemoNavPowered`, `DemoArmoryPowered`, `DemoWeaponGranted`,
`DemoShipFightDone`, `DemoCrewMenuUnlocked`, `DemoFabPowered`,
`DemoWheelBuysLeft`, and per-zone `DemoZoneDeepestWave` (ruled at cut, Zo
2026-08-17 — the re-key beat 4 stamps; `DemoNavUnlockWave` / `DemoBankCapWave`
are never added). Migration: missing keys → demo-on for a new campaign,
demo-off for a save with `CleanExits >= 1` or `DeepestWaveReached > 1`
(`../../Demo/README.md`, Shared flags).

## Acceptance criteria

1. Start New plays the full beat in order: black 0.4 s → fade 1.5 s →
   `demo.wake_stir` → hold-unlock 1.0 s → lid 0.8 s → `demo.wake` → dark-station
   refusals → NAV hold 1.5 s → `demo.nav_repair` → ship terminal E/T →
   `demo.intruders` with the corridor still clear and `timeScale == 1` →
   Continue releases 2 intruders → ARMORY E grants the MK-1 with no panel →
   both die → `demo.ship_clear` → CREW E heals with no panel → NAV panel →
   EMBARK → `demo.embark_first` on the first Bastion Prep frame.
2. HP floor: on the Ship while `DemoActive`, overdamage leaves 1 HP and never
   calls `Die()`.
3. Early EMBARK refuses with the right string and log for each of the three
   reasons (terminal not started / no weapon / intruders alive).
4. F10/F11 and lobby entry refuse with
   `[Demo] multiplayer locked — buy an upgrade first` while
   `DemoActive && DemoWheelBuysLeft == 2`.
5. v9 round-trips; fresh save has the README defaults; a pre-v9 save with
   `CleanExits >= 1` loads `DemoActive == false` and beat 0 is a no-op on it.
6. Reload matrix (beat 0, Reload table): quit after unlock reloads on deck
   with no reseal; quit after T unarmed replays `demo.intruders` with the
   release gated again; armed skips the line and releases immediately.
7. Every reads-back log in the beat's Driven/Shown/Reads-back list fires; no
   `[Dev]` on any of these paths.

## Implementation

Beat 0's Create / Change / Delete tables are the file list: **Create**
`DemoProgress`, v9 fields, `DemoStationGate`, `ShipTerminal`, `ShipCapsule`,
`ShipSpawn`, scene work (cryo bed, spawns, terminal, fade overlay, NavMesh if
missing); **Change** `ShipController` (gate Down before `OpenStationPanel` —
the beat's stated blocker), `MapStation`, prompt strings, `PlayerHealth`
floor, `ShipHud`, F10/F11, save JSON; **Delete** nothing. Working assumptions
A1–A14 are stamped in the beat — change them there first, then build.

One `unknown — needs scene check` is load-bearing: whether `PlayerGun` and a
NavMesh exist on the Ship (A10). Resolve the check before scoping the fight
step; the beat says scope after check, not guess.

## What closed it

Closed by Zo, 2026-08-18, on a beat 0 playthrough run end to end through to the
Bastion. That playthrough is criterion 1, and the scratch pass had already been
run (`RelocateDemoCapsule.cs` moved out of `Assets/_Scratch/` at close, awaiting
deletion).

Criteria 2 to 7 were not separately observed at close. They are owed, not
verified, and each carries an item in
`../../Hygiene/1. open/HP-1-beat-zero-observed.md` (TH-1 to TH-6; register
`../../Hygiene/README.md`): the
HP floor, the three EMBARK refusals, the multiplayer lock, the v9 round-trip and
downgrade, the reload matrix, and the reads-back log audit. Anything on that
list touching a MonoBehaviour, a scene or the save file settles through the
AutoVerify bridge, not the offline harness.

## Verification

Beat 0's Verification list 1–8 maps onto the criteria above (EditMode v9 +
`CanEmbark` matrix; the watch-the-tape playthrough; HP floor; F10 refuse;
the two quit/reload cases; no `[Dev]`). Playthrough path is criterion 1
end-to-end — from Play, one sitting, reading the log after.

## Traps

Beat 0 Traps apply verbatim. The two that break other milestones if missed:
open-on-Down ungated (pops menus this beat forbids), and any `timeScale`
write on the intruder line (withdrawn exception).

## Depends on / Unblocks

Depends: M74 (runner; the beat functions mute if copy is TODO, but the gated
release rides the line's Continue). Unblocks: M76 (EMBARK, v9 gate), M78
(`DemoArmoryPowered` already true, MP gate built here).
