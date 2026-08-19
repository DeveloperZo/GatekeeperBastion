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

**Stated by Zo, 2026-08-08.** These four are the Boarder's contract with the Bastion.
They do not move when §1.2 ranks move. The mapping under the table is what gets restamped.

1. **Progress through failure.** A lost Run still moves you forward.
2. **Reward success rather than punish failure.** Winning pays; losing does not take.
3. **Skills and loadout amplify player expression.** What you bring, and what you are good
  at, is how the player says something.
4. **You start the action, so a challenge is also a chance to prepare.** Nothing arrives
  before you call it.


| Belief                                    | Carried today by                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Gap                                                                                                                                           |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Progress through failure                  | **The whole purchasable surface.** Hatch-bank drip/dump credits `MetaState.BankedSalvage`; leftover Unbanked is wiped on a lost Run, so shop money is what made it through the Hatch. Tiers additionally unlock only after a first loss (`MetaUpgrades.UpgradesUnlocked == PodEjections >= 1`), so failing once is the thing that opens the deepest progression track. The loss screen says `converted this run: N`, then the banked total.                                                                                                                      | None identified.                                                                                                                              |
| Reward success rather than punish failure | No ending moves **banked** Salvage. Unbanked since the last Hatch-bank is lost on a down or Hatch breach (`../Decisions.md` expiry). Dying and clearing differ in which counter ticks, the deepest Wave shown, and what the screen says. The loss-stake "musical chairs" rotor was built and cut. Repair is an **exchange**, Salvage for Hatch HP, priced at `ShipSystems.RepairSalvagePerHp`; it is a purchase like any other and not a forfeit.                                                                                                                | None identified.                                                                                                                              |
| Skills and loadout amplify expression     | The weapon is the build, per-weapon rather than a global stat sheet, and each purchase is now a choice: `ShipCommands.BuyWheelUpgrade` centres one upgrade spoke and `WeaponWheel` awards 5 + 3 + 3 = 11 points across three of the five, nothing to the two at distance 2. BR-12, MR-35, PRI-C, MK-1 and SEC-D order their rings differently (`WeaponWheel.RingOf`), so the same focus buys a different gun. Intensity juice is a stake the player authors at a look-at trigger. CREW QUARTERS equips a named Boarder Protocol (`Sim/Meta/BoarderProtocol.cs`). | SEC-D's Ability spoke cannot be bought (`AbilityId.None`). Specialist protocols wait on their slices (`../Brainstorms/boarder-protocols.md`). |
| You start the action                      | No Wave timer until juice is selected and `T` starts. Look at a field terminal, Master Control, or the Hatch; `+/-` juices Unbanked; `E` selects; `T` starts. T1 still needs 5 clears to open its door. Untimed Prep after each clear. Intensity locked in Assault. In a friends session the party lead is who calls it (`M72`). | None on the solo contract. Party lead is the co-op reading of "you". |


Together: **the player sets both the pace and the stake, and the game supplies neither.**
Pressure here is authored by the person under it. Juice, `E`, then `T` at a field terminal,
Master Control, or the Hatch is what starts a clock.

**How the four serve §1.2** (stamp 2026-08-16; restamp when the rank table moves):

| Belief | Serves |
|---|---|
| Progress through failure | Expression (shop money is what survived a Run); Challenge (the vehicle) |
| Reward success rather than punish failure | Expression (banked stays); Fellowship (guest down does not take the host's Run, `M49`) |
| Skills and loadout amplify expression | Expression (rank 2) — the only belief that names an aesthetic |
| You start the action | Challenge (the vehicle); Fellowship (party lead starts juice / `T` / EMBARK) |

**Not named by any belief:** Fellowship as the reason a friend embarks — session layer
(Host/Join, `M65` wallets, `M72`). Fantasy as boarding a held vessel — kind refusals, the
kit, destinations still heading. Those are §1.2 goals. A fifth belief is a Zo restatement,
not a mapping edit.

**Ancestor** (Zo, 2026-08-17): [Guns 'n Goblins](https://store.steampowered.com/app/881940/Guns_n_Goblins/)
([IGN](https://www.ign.com/games/guns-n-goblins)). You ring a bell to call the swarm;
death still grows you. Beliefs 1 and 4 are those two. This game inverts the Bastion
(theirs; you board) and refuses the rest of that Steam page: idler, incremental
character sheet, build-your-fortress, thousands-strong spectacle, single-player as
the session.

**What the game refuses to be** (confirmed by Zo, 2026-08-08), each refusal paid for by a
cut already made:

- **Not an idle game.** Time never pays; standing in Prep earns exactly zero.
`BaseSalvagePerSecond` is gone.
- **No exit mid-Run.** There is no bail-out lever and no in-Run respawn for solo play or
for the host. `EscapeSpool` and `RunPhase.Spooling` are dormant, kept only so the enum
stays serialization-stable. Exception (Zo, 2026-08-13): co-op client get-up is
`../Milestones/3. completed/M57-the-downed-player-waits-on-the-ship.md`
(Ship wait, then embark on the next hatch-defense Wave). Shipped 2026-08-15.
Amended (Zo, 2026-08-17): **death is no longer the only Run ender.** A player who
survives a hatch-bank window may return to the Ship — held interact at the Hatch,
available until the next Wave is triggered — and the Run ends **Docked**, not Lost;
banked Salvage and door state persist via the successful-bank snapshot. The refusal
narrows rather than falls: still no bail-out lever, and outside a surviving bank
window down and breach remain the only enders (`../Decisions.md`,
*Locked — return to Ship at a surviving bank*).
- **It does not rush you.** The one thing it will never do is start without you.

---



## 1.2 Which kinds of fun (#2, aesthetics)

The aesthetics are Hunicke, LeBlanc & Zubek’s eight, the same eight Riot teaches in
[URF Academy — Game Feeling](https://www.riotgames.com/en/urf-academy/fun-and-feeling):
Sensation, Fellowship, Challenge, Fantasy, Narrative, Discovery, Expression, Submission.
Competition is not in that set and is not an aesthetic here.

**Ranks 1–3 are goals** (Zo, 2026-08-16): **Fellowship, Expression, Fantasy.** A proposal
that does not serve one of those three has to say why it is still on the ladder. Paradigm
mapping is §1.1; mechanics Protects column is `3-Mechanics.md`. The 2026-08-08 order
(Expression, then Fellowship, then Challenge) is superseded.

**Ranks 4–8 follow the build and the brainstorms** — what the mechanics do today, and
where `../Brainstorms/` is aimed. Challenge is the vehicle: you fight so Fellowship,
Expression, and Fantasy have something to happen in.

| Aesthetic | Rank | Status | Evidence |
|---|---|---|---|
| **Fellowship** | **1** | goal; in play | Friends alpha (`Roadmap.md`): install, join, share a Ship. In tree: Host/Join, wallets (`M65`), shared Ship + party EMBARK (`M72` closed 2026-08-17), guest wait (`M57`). Heading: `../Brainstorms/co-op.md` polish (nameplates, voice). Solo is still a complete Run; the goal is that a friend is the reason you embark. |
| **Expression** | **2** | goal; served | The weapon is the build (`WeaponWheel` 5+3+3, per-weapon rings). Juice is a stake you author. CREW sells a named Protocol (`M70` closed; `../Brainstorms/boarder-protocols.md` — specialists, twists). FAB payloads (`../Brainstorms/fabrication.md`). ARMORY closed brainstorm is the gun shelf. Gap: SEC-D Ability spoke; specialist sheets. |
| **Fantasy** | **3** | goal; frame only | Boarder, Bastion, Gatekeepers, Hatch, Salvage — always on, no fantasy *progression*. Heading: twelve-kind garrison (`../Brainstorms/gatekeepers-and-waves.md`); a second destination that differs in layout, hazard, or composition (`../Brainstorms/missions-and-destinations.md`); named Protocols as identities; dead-Ship demo (`../Brainstorms/first-session.md`, `../Demo/`). The goal is boarding a held vessel. |
| **Challenge** | **4** | served | The Run’s fail state, juice, Hatch-bank fight, kind refusals. Demo first cycle and Wave roster live here. Play reports name this (`playthrough.md`). This is how Fellowship, Expression, and Fantasy are tested. |
| **Sensation** | **5** | in play | Dressed kit, guns, HUD (`M61`, `M61-2`); session voice (`M69` — Whispers + fire/foot SFX). Heading: combat tells (`M66`). |
| **Discovery** | **6** | declined for now; heading | One Bastion today. Heading: second destination (`missions-and-destinations.md` graduation test — layout, hazard, or composition, not difficulty alone); kit rooms and the twelve-kind roster as things to learn. |
| **Narrative** | **7** | declined; heading light | No plot. Heading: demo PA / Dialogue System (`../Demo/voice.md`) as furniture with a line (`M62` companion cut). |
| **Submission** | **8** | refused | Clock pays nothing. No brainstorm revives idle. |

**What that implies for ordering work:** Fellowship's session lobby closed with `M72`.
Expression's remaining gate is specialist Protocols after Default (`M70`). Fantasy
thickens when a second destination or a kind that uses the kit's lanes ships. Default HP
is the wrong lever. Challenge work (first-session TTK, drip) is in service of those three.

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

**The player's report still outranks the instrumentation** (`../GatekeeperZo Notes/playthrough.md`).
Mean live grades Challenge (the vehicle, rank 4). It does not grade Fellowship or Fantasy.

---



## 1.4 What each actor wants (#3)

The subsystem goals. **Each gatekeeper kind is defined by what it refuses to do, and each
refusal is a rule in code** (`3-Mechanics.md`), not a tuning value — so no tuning pass can
soften one by accident. The table below is what is in the build. The decided roster of
twelve lives in `../Brainstorms/gatekeepers-and-waves.md`.


| Actor   | Wants                                                                | Refuses                                                   |
| ------- | -------------------------------------------------------------------- | --------------------------------------------------------- |
| Default | you, on a field-terminal Wave; the Hatch on a Hatch-bank; diverts to you when provoked | — |
| Hunter  | you, only                                                            | the Hatch, at any tuning                                  |
| Thief   | the Hatch on a Hatch-bank Wave; the Boarder on a field-terminal Wave | being turned around by being shot; `playerDamage` stays 0. Code id `Ram` until M63. |


**Boarders, in a friends session** (Fellowship, rank 1):

| Actor | Wants | Rule |
|---|---|---|
| Host | the Run to continue | down ends it (`EndRun`) |
| Guest | the same Wave on their own loadout | down waits (`M49` / `M57`), or buys back at a live bank window from their own banked Salvage, cost tunable (Zo, 2026-08-17); Salvage split equal (`M65`) |
| Party lead | NAV pick and EMBARK | `NetPartyLobby` (`M72` closed 2026-08-17) |

The player's incentive is split across four stations, and the split is the design:


| Station       | What it is for                                                                                                  | Serves | Gated                                                      |
| ------------- | --------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------- |
| ARMORY        | the weapon in your hands, and what that weapon becomes                                                          | Expression | Tiers locked until the first death; buying a weapon is not |
| CREW QUARTERS | the Boarder's protocol — doctrine, Crew and Personal abilities; Default is a 0-salvage claim | Expression, Fantasy (named identity) | never: Coming Soon stubs are not a death gate |
| FABRICATION   | the Ship's gear and the Hatch — the Hatch Armor track, the Decoy payload, plus the equipment slot and the Equipment bought into it | Expression, Fantasy (Ship gear on a held vessel) | Hatch Armor and Decoy never; equipment slot locked until the first death |
| NAV MAP       | where you go and how deep you start                                                                             | Fellowship (party lead); Discovery (heading) | never                                                      |


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