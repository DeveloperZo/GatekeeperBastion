<!--
Purpose: Current representation of the opening — the gate ladder every fresh realm runs under, what each Ship station will and will not answer while it holds, the metered purchase budget, and the single event that ends it — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-30
-->

# The Opening

Every new realm starts inside the opening: a ladder of gates that walks a Boarder from
waking sealed on the teleporter pad, to repairing the NAV MAP, to the ship terminal, to
taking the free MK-1 that arms them for a first embark. While it holds, most of the Ship
is out of reach — FABRICATION opens no panel at all, the Primary rack stays sealed until a
Boarding has gone deep enough, and upgrade-wheel purchases are rationed to two, then two
more. The gates belong to the realm, so a well-equipped Boarder entering a fresh world
runs the whole ladder again. It ends on one event: the realm's first Boarding to throw
Master Control's switch, which opens every console and the whole catalog at once.

## The interaction

The Boarder invokes it on their own Ship, against the realm's first-time gates. Every
realm is created inside the opening — `RealmState.DemoActive` is declared `true`, so a
new world opens with its stations shut and its budget metered, and it stays that way
until this realm's first **Escaped**. The gates are realm-side ([Realms](Realms.md)), so
a well-equipped Boarder walking into a new world runs it again.

**The wake.** A load with `DemoCapsuleOpened` unwritten seals the Boarder on the
teleporter pad: look is free, movement is Rooted, the screen holds black then fades, and
the first line plays. The interact prompt goes live only when that line is dismissed, and
a held interact releases the pad — the flag is written and saved, the glass retracts, the
Boarder is unrooted. The hull light runs dimmed for the whole opening. A stretch of quiet
before the NAV repair replays a line pointing at the NAV MAP. (The pad, the
glass and the fade image are serialized scene bindings; what is verified here is the
phase machine and the flag write, not the scene wiring.)

**The NAV repair.** `NavVerdict` answers **Dark** until `DemoNavPowered`. A dark NAV MAP
opens no panel at all — the interact is consumed before `OpenStationPanel` is reached and
the prompt is the answer. A held interact repairs it: the flag is written and saved, the
console screen lights, and the standing points at the ship terminal. Releasing the hold
resets it.

**The ship terminal teaches arm-then-start.** A mid-deck console, not a station panel.
It refuses while the NAV MAP is dark ("NAV first"). Once the NAV MAP is live, a tap arms
it and the start key starts it — the same two-key contract every field terminal and the
Hatch use for the rest of the game ([A Boarding](A-Boarding.md)). The start routes power
(`DemoArmoryPowered`), saves, and points at the ARMORY. It is one-shot per realm: after
that the prompt reads "power routed" and the component does nothing.

**Taking the MK-1.** `ArmoryVerdict` answers **Hidden** until the terminal has routed
power — no name, no prompt, no panel, and an empty prompt string so the crosshair reads
as nothing targeted — and **Open** from then on. Buying the MK-1 there is the **arming
beat**: the same write sets `DemoWeaponGranted` and `DemoShipFightDone`, saves, puts the
weapon in the Boarder's hands and logs that multiplayer has unlocked. The MK-1 costs
nothing and is still a purchase ([Weapons](Weapons.md)). Buying the other Secondary
instead does **not** satisfy the gate — the arming beat tests for the MK-1 specifically,
so EMBARK still refuses "You are unarmed."

**EMBARK refuses in order.** `EmbarkRefusal` names the first unmet gate: the ship
terminal, then no weapon, then intruders aboard. Each reason speaks once per Ship visit.

**The four consoles, while the opening holds:**

| Station | What it answers | Until |
|---|---|---|
| NAV MAP | Dark (the hold-repair target, no panel), then prompts with no picker | Repaired, then the ship is clear |
| ARMORY | Hidden — no name, no prompt, no panel | The ship terminal routes power |
| CREW QUARTERS | Hidden, then restore-health on a tap and no panel | The first Ship arrival after this realm's first lost Boarding writes the menu unlock |
| FABRICATION | Visible, named, prompt "FABRICATION — FULL VERSION"; the interact is consumed, never a panel | The opening ends |

CREW QUARTERS keeps one early door: a Boarder holding no protocol gets the panel opened
for the claim and nothing else, because a first Boarding is exactly the seat that has no
protocol ([Protocols](Protocols.md)). The heal happens first either way, so a Boarder
walking in hurt never loses it to the claim.

**FABRICATION is a locked preview for the entire opening** — `FabricationVerdict` returns
it unconditionally while the gates hold. That console binds four rows: Hatch Armor, the equipment slot, the Repair Rig and
the Decoy payload ([Upgrades](Upgrades.md), [The Drone](The-Drone.md)). None of them can
be bought until the opening is over — not gated by price, by the earned-Salvage lock or
by anything the player can act on, simply not reachable.

**The two-rung wheel budget.** Upgrade-wheel purchases are metered by two separate
counters that never reset per visit. Rung 1 starts at **two** buys on a fresh realm. Rung
2 opens on a Ship arrival once zone 2 has been resolved, sets **two** more on its own
counter, and latches so it opens once. Rung 1 is spent first; rung 2 only after it is
empty. With both at zero the purchase is refused by name before any Salvage is checked —
"demo cap" while rung 2 has not opened, "ladder rung spent" once it has.

**The Primary rack opens on a Wave REACHED, not cleared.** `PrimaryRowsOpen` is true once
zone 2 has been resolved, or once the realm's *deepest entered* Wave reaches the last Wave
of zone 1. Entered, not cleared, deliberately: dying on that Wave opens the rack, so the
Boarding that proves a Secondary is not enough is the Boarding that sells the answer.

While it is shut the PRIMARY tab is still reachable but binds no BUY row for an unowned
Primary, and the buy command refuses a Primary before any price check — so a player who is
also short hears the lock, which is the reason still true after they bank more. An owned
Primary stays selectable and upgradeable.

**Co-op unlocks on being armed.** The multiplayer rule is `DemoWeaponGranted`, or the
opening being over. The chooser reads it off the picked realm's document before Relay is
asked for anything, so a code is never handed out for a realm that cannot use it
([The Crew](The-Crew.md)).

**The start-Wave picker is hidden throughout.** Solo, the NAV MAP forces the start Wave to
1 and hides the stepper; in a party the lead loses it too, and no Wave number renders on
the panel or in the countdown line. Refighting during the opening lives at the zone
terminal instead ([Retriggering](Retriggering.md)).

**The intruder fight is built, and all but unreachable in normal play.** The Ship fight still exists in code — two
intruders, the MK-1 as the only damage source, and Boarder HP floored at 1 so a death on
the Ship is impossible while the opening runs. What was removed is its trigger: the ship
terminal's start no longer spawns or grants anything, and `DemoShipFight.ArmRelease` has
no caller anywhere in `Assets/Scripts`.

The one surviving arm is the load-time resume — power routed, fight not done — and the
arming beat writes both of those flags in the same save, so a straight-through opening
never loads that state. It is still reachable across a quit: the ship terminal's start
saves `DemoArmoryPowered` on its own, so leaving between the terminal and the ARMORY and
coming back does load the resume branch, grant the MK-1 late and release both intruders.
In one sitting, it never fires.

**The machine-level skip.** `SettingId.SkipShipTutorial` is a preference on the machine,
not on the realm, and it defaults to false. When it is set, adopting a realm pair runs
`SkipShipTutorial`, which — on a realm that still has an unmet embark gate — writes
`DemoCapsuleOpened`, `DemoNavPowered`, `DemoArmoryPowered`, `DemoWeaponGranted` and
`DemoShipFightDone`, and owns and equips the MK-1. A fresh realm therefore opens past the
wake, the repair, the terminal and the arming beat, armed, with co-op already unlocked.
What it does **not** touch is the rest of the opening — the wheel counters, the Primary
rack, the CREW menu, FABRICATION and `DemoActive` all stand.

The only code that sets the preference automatically is the intruder fight's last kill,
which a straight-through opening never reaches, so in practice the preference is whatever
the options screen says.

**The ending is one event.** The first **Escaped** — Master Control's switch thrown
after the elite is down ([Master Control](Master-Control.md)). The terminal-phase handler calls `EndDemo`
on Escaped only; a lost or a Docked ending never does. `EndDemo` flips `DemoActive` off
and writes `DemoFabPowered`. Every gate above no-ops once `DemoActive` is false, so that
one write opens FABRICATION, opens the ARMORY's whole catalog, stops the two counters
being read, puts the start-Wave picker on the NAV MAP and unlocks co-op regardless of
weapon.

`DemoFabPowered` is a record of what happened, not a gate: nothing in `Assets/Scripts`
reads it. The counters and `DemoRungTwoOpened` are left alone for the same reason.

Driven by the held interact at the pad and at the dark NAV MAP, the tap-then-start at the
ship terminal, the ARMORY's BUY rows, and EMBARK (bindings:
[Field Manual](Field-Manual.md)). Shown by the sealed pad and the retracting glass, the
dimmed hull, the per-station prompt overrides, the standing objective line on the Ship
HUD, the sealed PRIMARY tab, and the absent start-Wave stepper. Reads back as the `Demo`
log lines — `NAV powered`, `ship terminal started`, `multiplayer unlocked`,
`station hidden ARMORY`, `station locked FABRICATION (full version)`,
`wheel refused — demo cap`, `weapon refused — primary locked`,
`ladder rung=2 opened — primary + 2 buys`, `demo ended` — and the named EMBARK refusals.

## Consequences

**The opening's spends are forced choices, because the budget is bounded inside a shop
that is mostly shut.** Three consoles are unavailable or partly unavailable, and the
fourth is metered. FABRICATION never opens, so Hatch Armor — the buy for Boardings lost
with the Boarder's health bar still full — and the Repair Rig — the thing that makes
repairing the Hatch exist at all ([The Hatch](The-Hatch.md)) — are not options during
exactly the stretch where the Hatch is most likely to be what ends a Boarding.

The whole purchasable surface of the opening is: the MK-1 (free), the protocol claim
(free), two wheel buys, then a Primary and two more wheel buys. That makes each spend a decision the player cannot defer, spread or undo. Rung 1's two
buys land while only Secondaries are on the rack, so both go into one weapon's ring: which
spoke to centre is the choice, and there is no "come back later" — coming back later is
the same two buys. Rung 2 arrives at the same
moment the Primary rows do, which is the fork: put both into the Secondary already carried
and its tier, or buy a Primary and start a second ring at nothing with two buys left. The
Primary rack keying on the Wave *entered* is what makes the fork honest — it opens on the
Boarding that went deep enough to hurt, whether that Boarding was cleared, lost or docked
out of.

The gates also order what a fresh realm can do at all: it cannot be opened to friends until
its Boarder is armed, so taking the MK-1 is both the first purchase and the thing that makes
the world shareable; and it cannot start deep, because the picker is hidden, so the opening
is played forward from Wave 1 and a refight is chosen at a terminal rather than at the NAV
MAP.

Removal test: strip the gates and a fresh realm opens on a lit Ship with four live
consoles, the full catalog, an unmetered wheel and a deep-start picker. The later game is
unchanged — which is why this is an arc and not the loop — but the first campaign stops
being a taught sequence with a rationed budget, and every forced choice above becomes a
free one. The decisions the player must account for change, so it is a mechanic, not
framing.

## Classification

Primary, arc. Serves Fantasy and Expression (`../Design/1-Experience.md` §1.2, 2026-08-16
rank stamp), with Challenge as the vehicle. Fantasy at rank 3 is the dead-Ship opening,
that goal's named heading; Expression at rank 2 is the metered spends as the first
statements the player gets to make; and Fellowship at rank 1 is gated on being armed.

## Interacts with

- [Weapons](Weapons.md) — the two-rung budget and the rack gate that hold over them during
  the opening are named here; that page owns the guns, the upgrade wheel and the Primary rack.
- [Upgrades](Upgrades.md) — everything FABRICATION sells (Hatch Armor, the equipment slot,
  the Repair Rig) is unreachable while this page's locked preview holds; that page owns those
  purchases and the earned-Salvage lock.
- [The Crew](The-Crew.md) — a realm refuses OPEN TO FRIENDS and JOIN until the MK-1 is taken;
  that page owns the session.
- [A Boarding](A-Boarding.md) — the ship terminal teaches the select-then-start contract the
  Hatch and the field terminals use, and the start-Wave picker is hidden for the whole
  opening; that page owns the start line and the deep start.
- [Retriggering](Retriggering.md) — the refight that stands in for the hidden picker, gated to
  the opening by the same flag; that page owns the offer and its elections.
- [Realms](Realms.md) — the gate flags and both counters are realm-side, so every new world
  runs the opening again, the machine-level skip being the one thing that is not; that page
  owns the realm and Boarder split.
- [Master Control](Master-Control.md) — throwing its switch is the single event that
  ends the opening; that page owns the sequence.
- [Protocols](Protocols.md) — the one door the gated CREW QUARTERS still opens is an
  unclaimed protocol; that page owns the claim and the sheets.
- [The Drone](The-Drone.md) — the Decoy is on the locked FABRICATION panel, so the bay stays
  empty for the whole opening; that page owns the bay and its payload.

## Where

- Every fresh realm starts inside it; the gate flags and both counters are realm-side — `Assets/Scripts/Sim/Meta/RealmState.cs` (`DemoActive` declared `true`, `DemoCapsuleOpened`, `DemoNavPowered`, `DemoArmoryPowered`, `DemoWeaponGranted`, `DemoShipFightDone`, `DemoCrewMenuUnlocked`, `DemoWheelBuysLeft` = 2, `DemoRungTwoOpened`, `DemoRungTwoBuysLeft`, `DemoFabPowered`)
- The flags read through the composed session state — `Assets/Scripts/Sim/Meta/MetaState.cs` (`DemoActive`, `DemoWheelBuysLeft`, `DemoRungTwoBuysLeft`, `DemoZone2BankLost`)
- The wake: sealed pad, Rooted movement, black-then-fade, prompt after the line, held interact writes the flag — `Assets/Scripts/Game/Ship/ShipTeleporter.cs` (`BeginSealed`, `Prompt`, `Interact`, `Update`, `BeginOpen`, `BeginDormant`)
- Where a load resumes the opening; the dimmed hull; the lost-player nudge; the rung-2 and first-return resolves — `Assets/Scripts/Game/Ship/DemoShipDirector.cs` (`TryResolveSpawn`, `ApplyHullLight`, `TickLostNudge`, `ResolveFirstReturn`, `ResolveRungTwo`)
- NAV dark → hold-repair → prompts with no picker — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`NavVerdict`); `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`TryHandleInteractDown`, `PromptOverride`, `TickHold`)
- The repaired console lights — `Assets/Scripts/Game/Ship/MapStation.cs` (`ShowPoweredLook`, `Awake`)
- Ship terminal: NAV-first refusal, tap arms, start routes power, one-shot — `Assets/Scripts/Game/Ship/ShipTerminal.cs` (`Prompt`, `Update`, `StartTerminal`)
- ARMORY hidden until power is routed, then open — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`ArmoryVerdict`); silent consume at `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`TryHandleInteractDown`)
- Taking the MK-1 is the arming beat: `DemoWeaponGranted` and `DemoShipFightDone` in one write, the co-op line, and the MK-1-only test — `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWeapon`, `armingBeat`)
- CREW hidden, then heal-on-tap, then the menu; the one early door for an unclaimed protocol — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`CrewVerdict`, `CrewOpensForProtocolClaim`); `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`Heal`, `HealOnVisit`)
- FABRICATION is a locked preview for the whole opening, unconditionally — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`FabricationVerdict`); the interact consumed with no panel at `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`TryHandleInteractDown`, `EngineeringStation` branch)
- What that console would otherwise sell — `Assets/Scripts/Game/Ship/EngineeringStation.cs` (`BuildPanel`, `BindEquipmentSlotRow`, `BindEquipmentRow`, `BindPayloadRow`)
- Two-rung wheel budget: named refusals before any spend, rung 1 first, no per-visit reset — `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWheelUpgrade` demo branch); the cap line at `Assets/Scripts/Game/Ship/ArmoryStation.cs` (`TryBuyFromWheel`)
- Both counters summed for the "nothing left to buy" state — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`NoBuyableUpgradeRemains`)
- Primary rack opens on the deepest Wave ENTERED reaching zone 1's last Wave, or on zone 2 resolving — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`PrimaryRowsOpen`, `PrimaryRowsWave`); `Assets/Scripts/Sim/Bastion/DemoZones.cs` (`EndWave`)
- Zone 2 resolved is written either way — survived window or lost inside one — `Assets/Scripts/Sim/Run/RunSim.Dock.cs` (`OfferDockIfBankSurvived`); `Assets/Scripts/Sim/Run/RunSim.cs` (`EndRun`, `DemoZone2BankLost` write)
- The sealed PRIMARY tab keeps the tab and loses the BUY rows; the buy refuses before any price check — `Assets/Scripts/Game/Ship/ArmoryStation.cs` (`SealedRack`, `BindCatalog`); `Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs` (`BuyWeapon` primary-locked branch)
- EMBARK refusal order and its three named reasons — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`EmbarkRefusal`, `DemoEmbarkRefusal`); `Assets/Scripts/Game/Ship/MapStation.cs` (`RefuseDemoEmbark`)
- Co-op refuses until the MK-1 is taken, checked on the realm document before a code is issued — `Assets/Scripts/Game/Demo/DemoProgress.cs` (`MultiplayerUnlocked`); `Assets/Scripts/Game/Flow/RealmSelectController.Hosting.cs`; `Assets/Scripts/Game/Flow/MainMenuController.cs`
- Start-Wave picker hidden solo and in a party; no Wave number anywhere on the panel — `Assets/Scripts/Game/Ship/MapStation.cs` (demo branch forcing the start Wave to 1 and `ShowStartWavePicker(false)`; `BuildPartyPanel`)
- A Boarding that somehow begins unarmed inside the opening is granted the MK-1 late — `Assets/Scripts/Game/Bastion/GameController.cs` (`Start`)
- The intruder fight, its Ship-only damage rules and its one surviving arm; `ArmRelease` has no caller — `Assets/Scripts/Game/Ship/DemoShipFight.cs` (`IsActive`, `CanFireOnShip`, `FloorsPlayerHp`, `ArmRelease`, `ResumeArmed`, `Release`, `OnIntruderDown`); armed only from `Assets/Scripts/Game/Ship/DemoShipDirector.cs` (`TryResolveSpawn`, the `!DemoShipFightDone` branch)
- The machine-level skip: the preference, its false default, what it grants, and where it is applied — `Assets/Scripts/Game/Settings/SettingId.cs` (`SkipShipTutorial`); `Assets/Scripts/Game/Settings/Settings.cs` (`Declarations`, `defaultBool: false`); `Assets/Scripts/Game/Demo/DemoProgress.cs` (`SkipShipTutorial`); `Assets/Scripts/Game/Flow/GameSession.cs` (`Adopt`)
- The preference's only automatic writer is the intruder fight's last kill — `Assets/Scripts/Game/Ship/DemoShipFight.cs` (`OnIntruderDown`)
- The first Escaped ends it, Escaped only; the two writes and what they leave alone — `Assets/Scripts/Game/Bastion/GameController.EndScreen.cs` (`EnterTerminalPhase`); `Assets/Scripts/Game/Demo/DemoProgress.cs` (`EndDemo`)
