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
Carriers restamped 2026-08-30.

1. **Progress through failure.** A lost Run still moves you forward.
2. **Reward success rather than punish failure.** Winning pays; losing does not take.
3. **Skills and loadout amplify player expression.** What you bring, and what you are good
  at, is how the player says something.
4. **You start the action, so a challenge is also a chance to prepare.** Nothing arrives
  before you call it.


| Belief                                    | Carried today by                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Gap                                                                                                                                           |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Progress through failure                  | **The whole purchasable surface, and now the award path itself.** `RunSim.AwardKill` credits `MetaState.BankedSalvage` the moment a body falls, at face value and with no carry multiplier (`BankKill`), so shop money is simply what the Run killed. The rule is that it must be impossible to die with no progress to show for it (Zo, 2026-08-25, `../Decisions.md`): the 2026-08-25 playthrough cleared Wave 1, died twice on Wave 2 and reported `gross 0, repair 0, net 0`, because both events that used to bank sat past the point where a starting kit dies. Tiers and the equipment slot unlock on `MetaUpgrades.UpgradesUnlocked == LifetimeSalvageEarned > 0` — lifetime on purpose, so spending to zero does not re-lock; re-keyed from `HatchConversions >= 1` on 2026-08-25 when the Convert left the demo, and from `PodEjections >= 1` before that (`M78`). The Primary rack is `DemoProgress.PrimaryRowsOpen`, now `DeepestWaveReached >= DemoZones.EndWave(1)`: the Run that proves you need a Primary is the Run that sells you one.                                                                                                                      | The end screen still reads `converted this boarding: N` — the old label on the right number.                                                   |
| Reward success rather than punish failure | No ending moves **banked** Salvage, and banked is now everything the Run earned. The only stake an ending settles is a pile the player deliberately pulled out of the bank with `RunSim.Withdraw` at the Hatch; that is wiped on a down or a breach and keeps `HatchBank.ReturnedKeepShare` on a Docked return. Nothing stakes money on the player's behalf, which is what makes a Withdraw a bet rather than a toll. Dying and clearing differ in which counter ticks, the deepest Wave shown, and what the screen says. The loss-stake "musical chairs" rotor was built and cut. Repair is an **exchange**, Salvage for Hatch HP, priced at `ShipSystems.RepairSalvagePerHp`; it is a purchase like any other and not a forfeit — as is the respawn fee, which charges the same bank.                                                                                                                | None identified.                                                                                                                              |
| Skills and loadout amplify expression     | The weapon is the build, per-weapon rather than a global stat sheet, and each purchase is now a choice: `ShipCommands.BuyWheelUpgrade` centres one upgrade spoke and `WeaponWheel` awards 5 + 3 + 3 = 11 points across three of the five, nothing to the two at distance 2. BR-12, MR-35, PRI-C, MK-1 and SEC-D order their rings differently (`WeaponWheel.RingOf`), so the same focus buys a different gun. Intensity juice is parked 2026-08-30 (`Ante.Enabled = false` in `IntensityJuice.cs`); Waves run at dial 0; `Enabled` is the return switch. CREW QUARTERS equips a named Boarder Protocol (`Sim/Meta/BoarderProtocol.cs`). | SEC-D's Ability spoke cannot be bought (`AbilityId.None`). Default, VEIL and FORGE ship (`M94`); AEGIS and LEDGER are still Coming Soon stubs (`../Brainstorms/boarder-protocols.md`). |
| You start the action                      | No Wave timer until `T` starts it. **The wave begins at the Hatch** (Zo, 2026-08-25): `GameController.HatchOffersWaveStart` makes the Hatch the start line for as long as the current zone has a terminal left, and that zone's field terminals refuse the key and say so. Finale too: `T` at Master refuses (`refused — Master Control starts at the Hatch`); `StartZoneWaveFromHatch` starts it when `ControlRoomReachable`. `+/-` and E-select at triggers are gated off (`Ante.Enabled = false`, `GameController.Terminals.cs` / `ShortcutPanel.cs`); retrigger still uses `E` for composite/partial election. The wave ENDS at the terminal — `RunSim.ShutOffFieldWave` banks the carry and completes the zone. Untimed Prep after each clear. In a friends session the party lead is who calls it (`M72`). | None on the solo contract. Party lead is the co-op reading of "you". |


Together: **the player sets both the pace and the stake, and the game supplies neither.**
`T` at the Hatch starts the clock — field waves and the finale. Juice / Ante is parked
2026-08-30 (`Ante.Enabled = false` in `IntensityJuice.cs`); dial stays 0; `Enabled` is
the return switch.

**How the four serve §1.2** (ranks 2026-08-16; carriers 2026-08-30):

| Belief | Serves |
|---|---|
| Progress through failure | Expression (shop money is everything the Run killed); Challenge (the vehicle) |
| Reward success rather than punish failure | Expression (banked stays); Fellowship (guest down does not take the host's Run, `M49`) |
| Skills and loadout amplify expression | Expression (rank 2) — the only belief that names an aesthetic |
| You start the action | Challenge (the vehicle); Fellowship (party lead starts `T` / EMBARK) |

**Not named by any belief:** Fellowship as the reason a friend embarks — session layer
(Host/Join, `M65` wallets, `M72`). Fantasy as the unique lane (`../Story/README.md`) —
kind refusals, the kit, the Hatch start. Those are §1.2 goals. A fifth belief is a Zo
restatement, not a mapping edit.

**Ancestor** (Zo, 2026-08-17): [Guns 'n Goblins](https://store.steampowered.com/app/881940/Guns_n_Goblins/)
([IGN](https://www.ign.com/games/guns-n-goblins)). You ring a bell to call the swarm;
death still grows you. Beliefs 1 and 4 started as those two. This game keeps the call
and that a failed trip can still pay — here, literally: a kill is banked as it lands, so
a trip that fails always paid. Their bell is one object (start the swarm, lose it, night
over). Here that splits: the **Hatch** is where the wave starts (Zo, 2026-08-25) and is
also the GameObject that, destroyed, ends the Run; a **field terminal** is where the wave
is switched off. Dying on a wave costs the fee, charged to the realm's bank
(`M98` recut, re-pursed 2026-08-25). Intensity juice is parked (`Ante.Enabled`, 2026-08-30)
and was never their bell. This game also inverts the Bastion (theirs;
you board) and refuses the rest of that Steam page: idler, incremental character
sheet, build-your-fortress, thousands-strong spectacle, single-player as the
session.

**What the game refuses to be** (confirmed by Zo, 2026-08-08), each refusal paid for by a
cut already made:

- **Not an idle game.** Time never pays; standing in Prep earns exactly zero.
`BaseSalvagePerSecond` is gone.
- **No exit mid-Run.** There is no bail-out lever. `EscapeSpool` and `RunPhase.Spooling`
are dormant, kept only so the enum stays serialization-stable. Exception (Zo, 2026-08-13):
co-op client get-up is
`../Milestones/3. completed/M57-the-downed-player-waits-on-the-ship.md`
(Ship wait, then embark on the next hatch-defense Wave). Shipped 2026-08-15.
Amended (Zo, 2026-08-17): **death is no longer the only Run ender.** A player who
survives a hatch-bank window, or who shuts off a zone (`OfferDockAfterZoneShutoff`),
may return to the Ship — held interact at the Hatch, available until the next Wave is
triggered — and the Run ends **Docked**, not Lost;
banked Salvage and door state persist via the successful-bank snapshot (`../Decisions.md`,
*Locked — return to Ship at a surviving bank*).
Amended again (`M98` recut): **a dead seat now buys its own way back** — guest, host and
solo alike, in `RunPhase.Tethered`, for `Respawns.BaseCost` × (respawns taken + 1),
charged to the **realm's bank** (`RunSim.TryRespawn` → `SpendBanked`; re-pursed 2026-08-25
when kills began banking on landing and the carried pile stopped filling on its own). The
DOWN overlay is capped at `Respawns.MaxRespawns` (2) and timed at `Respawns.BleedOutSeconds`
(10 s; numbers in `4-Data.md`). The refusal narrows rather than falls: still no bail-out
lever, and outside a Dock-offered window the only enders are breach, a death the bank
cannot afford, the cap, or bleed-out with nobody standing — which makes every purchase
and repair also a decision about how many deaths the crew can absorb.
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
| **Expression** | **2** | goal; served | The weapon is the build (`WeaponWheel` 5+3+3, per-weapon rings). CREW sells a named Protocol (`M70` closed, VEIL and FORGE added by `M94`; `../Brainstorms/boarder-protocols.md` — twists). FAB payloads (`../Brainstorms/fabrication.md`). ARMORY closed brainstorm is the gun shelf. Gap: SEC-D Ability spoke; AEGIS and LEDGER. |
| **Fantasy** | **3** | goal; frame only | Boarder, Bastion, Gatekeepers, Hatch, Salvage — always on, no fantasy *progression*. The goal is the unique lane in `../Story/README.md`: friends own the silence, choose the kit, start the wave, hold one Hatch. Heading: twelve-kind garrison (`../Brainstorms/gatekeepers-and-waves.md`); a second destination that differs in layout, hazard, or composition (`../Brainstorms/missions-and-destinations.md`); named Protocols as identities; dead-Ship demo (`../Demo - Single/`). |
| **Challenge** | **4** | served | The Run’s fail state, Hatch-bank fight, kind refusals. Demo first cycle and Wave roster live here. Play reports name this (`playthrough.md`). This is how Fellowship, Expression, and Fantasy are tested. |
| **Sensation** | **5** | in play | Dressed kit, guns, HUD (`M61`, `M61-2`); session voice (`M69` — Stardust slices on Splash/Menu/RealmSelect, Hydrodynamics beds on Ship/Bastion, fire/foot SFX). Heading: combat tells (`M66`). |
| **Discovery** | **6** | declined for now; heading | One Bastion today. Heading: second destination (`missions-and-destinations.md` graduation test — layout, hazard, or composition, not difficulty alone); kit rooms and the twelve-kind roster as things to learn. |
| **Narrative** | **7** | declined; backdrop | No plot. Fiction is wallpaper (`../Story/`). Demo PA / Dialogue System stays furniture (`../Demo - Single/voice.md`, `M62` companion cut). |
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
| Thief   | the Hatch on a Hatch-bank Wave; the Boarder on a field-terminal Wave | being turned around by being shot; `playerDamage` stays 0. |
| Cloaker | exactly what a Default wants, while nobody is looking at it          | moving or targeting at all while a live Boarder holds it in a 30° gaze cone with line of sight (`CloakerGatekeeper`). Being shot still latches the grudge. |


**Boarders, in a friends session** (Fellowship, rank 1):

| Actor | Wants | Rule |
|---|---|---|
| Host | the Run to continue | down raises the DOWN overlay and offers the priced respawn for `Respawns.MaxRespawns` (2) inside `Respawns.BleedOutSeconds` (10 s); `PlayerDied` / `NotifySeatWentDown` / bleed-out end the Run when no seat can pay or stand |
| Guest | the same Wave on their own loadout | down waits (`M49` / `M57`), or pays the same `Respawns.CostFor` price out of the realm's bank (`GameController.TryNetRespawn`); every seat's kills credit that one bank |
| Party lead | NAV pick and EMBARK | `NetPartyLobby` (`M72` closed 2026-08-17) |

The player's incentive is split across four stations, and the split is the design:


| Station       | What it is for                                                                                                  | Serves | Gated                                                      |
| ------------- | --------------------------------------------------------------------------------------------------------------- | ------ | ---------------------------------------------------------- |
| ARMORY        | the weapon in your hands, and what that weapon becomes                                                          | Expression | Wheel purchases locked until the realm has earned any salvage (`LifetimeSalvageEarned > 0`); buying a weapon is not |
| CREW QUARTERS | the Boarder's protocol — doctrine, Crew and Personal abilities; Default, VEIL and FORGE are claims, AEGIS and LEDGER are Coming Soon | Expression, Fantasy (named identity) | never: Coming Soon stubs are not a gate |
| FABRICATION   | the Ship's gear and the Hatch — the Hatch Armor track, the Decoy payload, plus the equipment slot and the Equipment bought into it | Expression, Fantasy (Ship gear on a held vessel) | Hatch Armor and Decoy never; equipment slot rides the same `LifetimeSalvageEarned > 0` key as the wheel |
| NAV MAP       | where you go and how deep you start                                                                             | Fellowship (party lead); Discovery (heading) | never                                                      |


What each station sells, and for how much, is in `../Play/Field-Manual.md`, generated from
the code. Repair is Equipment rather than a track since M23: the slot is bought once and the
Repair Rig into it (`ShipCommands.BuyEquipmentSlot`, `ShipCommands.BuyEquipment`,
`Sim/Meta/EquipmentId.cs`), and repairing requires owning and carrying the rig. The old
FABRICATION Repair Rig and CREW QUARTERS Engineering tracks are gone from the enum and from
the save: migration ended at schema 13, and a document below it is refused by name rather
than reshaped (`Sim/Meta/SaveFormat.cs`). `EngineeringStation.cs` keeps the
old class name on purpose; every string the player reads says FABRICATION
(`../_meta/_definitions/language.md`).

---

How these incentives collide at runtime: `2-Dynamics.md`. What is enforced and who may
change it: `3-Mechanics.md`. The values it all currently Runs at: `4-Data.md`.