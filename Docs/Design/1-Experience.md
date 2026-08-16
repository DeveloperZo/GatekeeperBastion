# 1 — Experience

**Aesthetics** in the MDA sense (Hunicke, LeBlanc & Zubek): the emotional response the game
is trying to produce. The top of the leverage ladder — #1 paradigm, #2 system goals,
#3 actor incentives.

MDA's central claim is that a designer can only directly build Mechanics but must aim at
Aesthetics, and that the player meets the chain in the opposite order: they feel the
experience first and never see the constants. Meadows says the same thing in the other
vocabulary. So this file is first, and `4-Data.md` is last, and a proposal made low in that
order has to be argued against everything above it.

**This is the builder's view.** The game as a player meets it is `../Play/The-Game.md`.
Countable facts are not restated here (`../README.md`, *Generated, or written*).

---



## 1.1 The paradigm — what the game believes (#1)

**Stated by Zo, 2026-08-08.** These four are the authority. The table under them records
how far the shipped build actually carries each one, and a gap between the two is a design
task, not a documentation error.

1. **Progress through failure.** A lost Run still moves you forward.
2. **Reward success rather than punish failure.** Winning pays; losing does not take.
3. **Skills and loadout amplify player expression.** What you bring, and what you are good
  at, is how the player says something.
4. **You start the action, so a challenge is also a chance to prepare.** Nothing arrives
  before you call it.


| Belief                                    | Carried today by                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Gap                                                                                                               |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Progress through failure                  | **The whole purchasable surface.** Hatch-bank drip/dump credits `MetaState.BankedSalvage`; leftover Unbanked is wiped on a lost Run, so shop money is what made it through the Hatch. Tiers additionally unlock only after a first loss (`MetaUpgrades.UpgradesUnlocked == PodEjections >= 1`), so failing once is the thing that opens the deepest progression track. The loss screen says `converted this run: N`, then the banked total.                                       | None identified.                                                                                                  |
| Reward success rather than punish failure | No ending moves **banked** Salvage. Unbanked since the last Hatch-bank is lost on a down or Hatch breach (`../Decisions.md` expiry). Dying and clearing differ in which counter ticks, the deepest Wave shown, and what the screen says. The loss-stake "musical chairs" rotor was built and cut. Repair is an **exchange**, Salvage for Hatch HP, priced at `ShipSystems.RepairSalvagePerHp`; it is a purchase like any other and not a forfeit.                                 | None identified.                                                                                                  |
| Skills and loadout amplify expression     | The weapon is the build, per-weapon rather than a global stat sheet, and each purchase is now a choice: `ShipCommands.BuyWheelUpgrade` centres one upgrade spoke and `WeaponWheel` awards 5 + 3 + 3 = 11 points across three of the five, nothing to the two at distance 2. BR-12, MR-35, PRI-C, MK-1 and SEC-D order their rings differently (`WeaponWheel.RingOf`), so the same focus buys a different gun. Intensity juice is a stake the player authors at a look-at trigger. | SEC-D's Ability spoke cannot be bought (`AbilityId.None`). The Boarder Protocol system is designed and not built (M70 recut, `../Brainstorms/boarder-protocols.md`). |
| You start the action                      | No Wave timer until juice is selected and `T` starts. Look at a field terminal, Master Control, or the Hatch; `+/-` juices Unbanked; `E` selects; `T` starts. T1 still needs 5 clears to open its door. Untimed Prep after each clear. Intensity locked in Assault.                                                                                                                                                                                                               | None. This is the belief the build carries most completely.                                                       |


Together: **the player sets both the pace and the stake, and the game supplies neither.**
Pressure here is authored by the person under it. Juice, `E`, then `T` at a field terminal,
Master Control, or the Hatch is what starts a clock.

**What the game refuses to be** (confirmed by Zo, 2026-08-08), each refusal paid for by a
cut already made:

- **Not an idle game.** Time never pays; standing in Prep earns exactly zero.
`BaseSalvagePerSecond` is gone.
- **No exit mid-Run.** There is no bail-out lever and no in-Run respawn for solo play or
for the host. `EscapeSpool` and `RunPhase.Spooling` are dormant, kept only so the enum
stays serialization-stable. Exception (Zo, 2026-08-13): co-op client get-up is
`../Milestones/3. completed/M57-the-downed-player-waits-on-the-ship.md`
(Ship wait, then embark on the next hatch-defense Wave). Shipped 2026-08-15.
- **It does not rush you.** The one thing it will never do is start without you.

---



## 1.2 Which kinds of fun (#2, aesthetics)

**Stated by Zo, 2026-08-08, in priority order: Expression, then Fellowship, then
Challenge.** Those three must be present. The rest are left to be settled by what gets
built, and should get more intentional over time rather than being decided now.

MDA's eight aesthetics, plus competition, read against what the build currently serves.
This is a claim about the shipped game, not a wish list; each row cites what makes it true.


| Aesthetic      | Rank  | Status              | Evidence                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| -------------- | ----- | ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Expression** | **1** | served              | The upgrade wheel ships (`Sim/Meta/WeaponWheel.cs`, bought from the ARMORY via `ShipCommands.BuyWheelUpgrade`). One purchase centres a spoke and spends 5 + 3 + 3 = 11 points across three adjacent spokes of five, so the focus is a decision with a cost: the two spokes at distance 2 get nothing. Rings are per weapon, so the same focus builds a different gun on BR-12 than on MR-35, PRI-C, MK-1 or SEC-D. Still missing: SEC-D has no ability (`SpokePurchasable` refuses Ability), and the five-skill system is unbuilt.                                                                                                                  |
| **Fellowship** | **2** | in play, incomplete | Kill loop (`../Milestones/3. completed/M48-the-first-networked-kill.md`), huntable/downable second Boarder (`../Milestones/3. completed/M49-the-gatekeepers-see-the-second-player.md`), client readout/endings (`../Milestones/3. completed/M50-the-client-lives-the-run.md`), in-run verbs (`../Milestones/3. completed/M51-the-clients-hands.md`), and get-up (`../Milestones/3. completed/M57-the-downed-player-waits-on-the-ship.md`) over UnityTransport loopback. Gap: a build a friend can install (`../Milestones/3. completed/M58-a-build-two-machines-can-run.md`), Relay join (`../Milestones/2. active/M60-a-join-a-friend-can-use.md`). |
| **Challenge**  | **3** | served              | The Run is an obstacle course with one fail state. Death is the only early ender; there is no difficulty option that is not intensity juice.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Sensation      | —     | in play, incomplete | Boarder is the IQ astronaut with Synty guns, stroke crosshair, RMB zoom 60/40, Ship desks `SM_Prop_ControlPanel_02` (`M61`, closed 2026-08-14). Bastion kit dressed — scaler-2 station rooms, sliding doors, bulkhead Hatch (`M61-2`, closed 2026-08-14). Audio is `M69`.                                                                                                                                                                                                                                                                                                                                                                           |
| Discovery      | —     | declined for now    | One destination, one Bastion, generated scenes.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| Narrative      | —     | declined            | No story beats. The fiction is a frame (`ALPHA-7`, Salvage, the umbilical) and carries no progression.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| Fantasy        | —     | frame only          | Same as above.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| Submission     | —     | actively declined   | The clock pays nothing. There is no version of this game you leave running.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| Competition    | —     | absent              | No score against anyone else. `CleanExits` and `PodEjections` are counters, not a ladder.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |


Challenge, ranked third, is served. Expression, ranked first, has a mechanism as of M21:
the upgrade wheel is buyable and its point spread makes which spoke you centre a real trade.
Fellowship, ranked second, has gameplay over loopback (`M48`–`M51`, `M57`); a session a friend
can install and join is unbuilt (`M58` LAN, `M60` Relay).

**What that implies for ordering work:** the leverage tier that serves Expression is #4,
who may change the rules (`3-Mechanics.md`), because expression is the player authoring
their own Run. The MK-1 ring and the Decoy payload shipped; the protocol system starts
at the M70 recut. Fellowship's remaining work is a session a friend can operate
(`M58`, `M60`).

---



## 1.3 What a good Run is (#2, the optimisation target)

Everything below this in the doc set is tuned toward some answer. The instrument that
grades a Wave is **mean live Gatekeepers** — how full the room was, on average, while the
Wave was live — read offline by `Tools/wave-report/run.sh` from the session log
(`../Milestones/3. completed/M29-how-full-the-room-got.md`).

**Stated intent, on the economy** (`2-Dynamics.md`, *The pacing target*): a player who dies
repeatedly on a Wave can buy past it within three failed Runs. This is a constraint — it
says what must not happen, not what a good Run is.

**Retired instrument.** Hatch remaining at each Wave clear (M16's ±15 band: Wave 1 ~100%,
Wave 2 ~85%, Wave 3 ~65%) measures leakage, not effort. A player who kills everything
before it arrives reads 100% whether the Wave was trivial or whether they were at their
limit. On 2026-08-07 a five-Wave clear at MR-35 T4 read 300/300 on every Wave, and the
player reported the session as fun and semi-challenging. Both facts are true and Hatch
remaining carried neither.

**The only measured curve** (`Logs/GatekeeperBastion_2026-08-07_16-34-30.log`, Run 2,
MR-35 T4, start Wave 2, dial 0; Zo: fun and semi-challenging):


| Segment | mean live | peak | mean lifetime | Hatch |
| ------- | --------- | ---- | ------------- | ----- |
| w1+2    | 2.84      | 8    | 3.34 s        | 300   |
| w3      | 5.97      | 10   | 4.85 s        | 300   |
| w4      | 9.52      | 18   | 6.33 s        | 300   |
| w5      | 11.26     | 20   | 6.75 s        | 300   |


Same session, Run 1 at T1: w1+2 **5.47**, w3 **11.04** (lost). One Run is not a curve.

**Open: what mean live a Wave should target.** Unknown — needs play data. Trigger: three
clean Runs at the same loadout, plus Zo's report on each. Owner: the milestone that next
moves a Wave parameter. Do not set a band from the table above; that is how M16's band
was written.

**The player's report still outranks the instrumentation** (`../GatkeeperZo Notes/playthrough.md`).
Mean live grades effort; it does not decide whether a Run was good.

---



## 1.4 What each actor wants (#3)

The subsystem goals. **Each gatekeeper kind is defined by what it refuses to do, and each
refusal is a rule in code** (`3-Mechanics.md`), not a tuning value — so no tuning pass can
soften one by accident. The table below is what is in the build. The decided roster of
twelve lives in `../Brainstorms/gatekeepers-and-waves.md`.


| Actor   | Wants                                                                | Refuses                                                   |
| ------- | -------------------------------------------------------------------- | --------------------------------------------------------- |
| Default | the Hatch; diverts to you when provoked                              | —                                                         |
| Hunter  | you, only                                                            | the Hatch, at any tuning                                  |
| Ram     | the Hatch on a Hatch-bank Wave; the Boarder on a field-terminal Wave | being turned around by being shot; `playerDamage` stays 0 |


The player's incentive is split across four stations, and the split is the design:


| Station       | What it is for                                                                                                  | Gated                                                                                              |
| ------------- | --------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| ARMORY        | the weapon in your hands, and what that weapon becomes                                                          | Tiers locked until the first death; buying a weapon is not                                         |
| CREW QUARTERS | the Boarder's own attributes — four tracks (`UpgradeTrack`)                                                     | never, by design: attributes are the answer to dying, so they must be buyable before you have died |
| FABRICATION   | the Ship's gear and the Hatch — the Hatch Armor track, plus the equipment slot and the Equipment bought into it | never                                                                                              |
| NAV MAP       | where you go and how deep you start                                                                             | never                                                                                              |


What each station sells, and for how much, is in `../Play/Field-Manual.md`, generated from
the code. Repair is Equipment rather than a track since M23: the slot is bought once and the
Repair Rig into it (`ShipCommands.BuyEquipmentSlot`, `ShipCommands.BuyEquipment`,
`Sim/Meta/EquipmentId.cs`), and repairing requires owning and carrying the rig. The old
FABRICATION Repair Rig and CREW QUARTERS Engineering tracks survive only as legacy save
fields, migrated by `MetaState.MigrateLegacyRepairTracks`. `EngineeringStation.cs` keeps the
old class name on purpose; every string the player reads says FABRICATION
(`../_meta/_definitions/language.md`).

---

How these incentives collide at runtime: `2-Dynamics.md`. What is enforced and who may
change it: `3-Mechanics.md`. The values it all currently Runs at: `4-Data.md`.