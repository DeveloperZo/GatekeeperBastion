# 3 — Ship after first loss

<!--
Purpose: Repair ARMORY, at most two wheel purchases on the MK-1, keep CREW and
FAB dark. Default Protocol stays unclaimed.
Status: demo spec
Authority: implementable
Update trigger: when wheel cap lifetime is overridden, when SEC-D grant is
overridden
Last updated: 2026-08-16
Depends: voice.md, 2-first-hatch-bank.md
-->

## What the player sees

Back on the Ship after the first Lost. NAV is already live. Repair **ARMORY**
(same hold as NAV). The wheel is open because they have been Lost
(`PodEjections >= 1`). At most **two** purchases on the gun they hold. CREW and
FAB stay dark.

They have ~450 banked if Wave 1 fully paid and convert-then-fight dumped it.
Two Secondary purchases cost 150 then 300 = 450.

## Why this beat exists

Live ARMORY never needs a repair. Wheel has ten purchases (`WeaponTier.MaxTier`)
and unlocks on first loss with no cap of two. CREW is a 0-salvage Default claim
whenever they walk there (`M70`). This beat teaches the console that spends
Salvage, then stops them emptying the wheel before Wave 2.

## In tree today

| Piece | Fact |
|---|---|
| Unlock | `MetaUpgrades.UpgradesUnlocked` = `PodEjections >= 1` |
| Buy wheel | `ShipCommands.BuyWheelUpgrade` — owned weapon, ring, `UpgradesUnlocked`, `tier < MaxTier`, `WeaponTier.Cost(slot, tier)` |
| Secondary costs | 150 + 150×tier → first two **150+300** |
| Panel | `ArmoryStation`: catalog first; owned weapon opens wheel; Secondary tab first when empty |
| Prompt | `buy a Secondary — both cost nothing` if `!OwnsAnyWeapon`; else equip/upgrade copy |
| MK-1 | Already owned from beat 0 grant |
| SEC-D | Still a 0-salvage catalog row once ARMORY opens |
| CREW | Live shop, Default claim 0 (`CrewQuartersStation`, `M70` not closed) |
| FAB | Live Hatch Armor / equipment / payload |
| NAV | Live. Start-wave picker capped by `DeepestWaveReached` (still 1 if they died on Hatch-bank). Beat 4 recuts the cap |
| Save | `GameSession.Save` after each `ShipCommands` success |

## The change

### ARMORY repair

Same hold pattern as NAV (`DemoRepair.HoldSeconds = 1.5`). Per-seat:
`DemoArmoryPowered`. Prompt `[E] repair ARMORY` until powered, then the live
panel opens.

On complete: `DemoArmoryPowered = true`, Save, `demo.armory_repair` Local Lock
(mentions two purchases). Party does **not** hear another seat’s ARMORY repair.

### Wheel cap

While `DemoActive` && `DemoWheelBuysLeft > 0`, each successful `BuyWheelUpgrade`
decrements `DemoWheelBuysLeft`. At 0: refuse with
`[Demo] wheel refused — demo cap` and panel copy
`two purchases this demo — NAV is ready`. Play `demo.wheel_cap` Local Lock once
per Ship visit when they try a third.

`DemoWheelBuysLeft` starts at **2** on the v9 fresh save (not reset on ARMORY
repair). Surviving first Hatch-bank without a loss: wheel still locked by
`UpgradesUnlocked` until a later Lost; cap still 2 when that unlock happens.

Do not change `WeaponTier.Cost`.

### SEC-D

Catalog row remains. They may buy SEC-D for 0 after ARMORY is powered. They may
not get a third wheel purchase on either gun. Rec: do not grant SEC-D at the
rack (beat 0). If they switch guns, both wheels share the same **2-buy** cap
(seat total, not per weapon).

### CREW / FAB

Stay dark (`DemoCrewPowered` / `DemoFabPowered` false). Same refuse as beat 0.
CREW lights in beat 4 after `DemoWave2Seen`. FAB lights when `DemoActive`
becomes false (beat 7).

### NAV this visit

Picker still shows Wave 1 until beat 4’s checkpoint is wired. **Ship beat 3 and
NAV beat 4 ship in one milestone** so they cannot EMBARK at 1 with a full chain
after this repair. If split, gate beat 3’s NAV: hide EMBARK until
`DemoNavUnlockWave >= 2` is in tree, or leave them able to replay Wave 1 (worse).
README order: **3 + 4 in one grain**.

This file still specifies ARMORY only; beat 4 specifies the picker. Implement
together.

### Protocol

Do not auto-claim Default. `G`/`H` refuse until beat 4’s CREW claim (`M70`).
NAV must not require a protocol to EMBARK (already true).

## Create

| Path | Role |
|---|---|
| `DemoStationGate` | ARMORY hold, already from beat 0; add Armory branch |
| Panel copy | `ArmoryPanelView` / deal line: remaining demo buys if `DemoActive` |

## Change

| File | Change |
|---|---|
| `ShipCommands.BuyWheelUpgrade` | After existing gates, if `DemoActive && DemoWheelBuysLeft <= 0` refuse. On success decrement |
| `ArmoryStation` | Dark until `DemoArmoryPowered`. Wheel rows show remaining buys. Third click uses existing unaffordable-style **named** refuse (how much you need is wrong here — use cap copy, not salvage short) |
| `CrewQuartersStation` / `EngineeringStation` | Remain dark |
| `ShipController` | Hold-repair ARMORY like NAV |

## Delete

Nothing.

## Numbers

| Name | Value | Source |
|---|---|---|
| Banked after full Wave 1 convert | 450 | beat 2 |
| Buy 1 | 150 | `WeaponTier.Cost(Secondary, 0)` |
| Buy 2 | 300 | `WeaponTier.Cost(Secondary, 1)` |
| Left after two | 0 | 450−450 |
| MaxTier | 10 | live wheel; gated to 2 |
| Repair hold | 1.5 s | same as NAV |

If they got fewer than 18 kills, banked < 450. First buy 150 still fits at ≥150;
second may show “N salvage short” (existing). Cap of 2 still applies. Do not
gift Salvage.

## Multiplayer

| Event | Authority |
|---|---|
| ARMORY repair | Local Meta. Host repairing does not light the guest’s ARMORY |
| Wheel / SEC-D | Local `GameSession` (`M72` c6 personal shopping). No shared bank |
| Station panel | First-come local panel, no station lock (M72 impl 6) |
| Ready | Guests may Ready while a seat still holds ARMORY. Lead EMBARK: beat 4’s wave picker. Do not require wheel spends |
| `demo.armory_repair` | Local only |
| Guest `NetShipWait` | Not this beat (Run already ended) |

If one seat skipped the first Hatch-bank (joined after): they have `DemoActive`,
NAV maybe powered, `PodEjections` 0, ARMORY dark, wheel locked. They need their
own Lost for upgrades. Do not copy host `PodEjections`. They may EMBARK unupgraded
into Wave 2 with the party (lead’s start wave). That is allowed; they are fragile.

## Voice

`demo.armory_repair` Local Lock on repair complete.
`demo.wheel_cap` Local Lock on third-buy refuse (once per visit).

No party PA “everyone shop.”

## Driven / Shown / Reads-back

- **Driven by** hold-E ARMORY; wheel click.
- **Shown** remaining buys on the wheel row; dark CREW/FAB prompts.
- **Reads-back** `[Demo] ARMORY powered`, `[Ship] bought MK-1 Sidearm …` wheel
  line (existing), `[Demo] wheel refused — demo cap`, `DemoWheelBuysLeft` in the
  wheel success log.

## Logs

| Event | Line |
|---|---|
| Repair | `[Demo] ARMORY powered` |
| Wheel ok | existing Ship wheel line plus `demoBuysLeft=<n>` |
| Cap | `[Demo] wheel refused — demo cap` |
| Dark CREW | `[Demo] station dark CREW QUARTERS` |

## Traps

- ARMORY prompt “buy a Secondary” when they already hold MK-1. Use the owned
  prompt.
- Treating cost 0 SEC-D as a wheel purchase. It is not; cap is `BuyWheelUpgrade`
  only.
- Resetting `DemoWheelBuysLeft` to 2 on every Ship visit.
- Unlocking FAB “so Hatch Armor matches the designed Hatch-bank.” FAB stays dark.
- Auto-claiming Default because `EquippedProtocol` already defaults in SaveData.
  `M70` claim must still be a walk to CREW.
- Spending 400 Primary-curve by putting a Primary in their hands. They hold a
  Secondary. Do not grant a Primary this beat.

## Depends / Unblocks

Depends: beat 2 convert + Lost (`PodEjections >= 1`) for the wheel. ARMORY
**repair** can be done even if they somehow have 0 banked; the panel opens empty.
Unblocks: beat 4 EMBARK at Wave 2 with a capped gun.

## Verification

1. EditMode: `BuyWheelUpgrade` twice succeeds at 450 banked; third refuses cap;
   banked 0; `DemoWheelBuysLeft` 0.
2. EditMode: third refuse does not call `SpendSalvage`.
3. PlayMode: dark ARMORY; 1.5 s hold; panel; two buys; third named refuse; CREW
   Down does not open.
4. Loopback: host two buys do not decrement guest `DemoWheelBuysLeft`.
5. No `[Dev]`.

## Survive branch from beat 2

If `first-hatch survived` and `PodEjections == 0`: ARMORY can still be repaired
(teach the console) but wheel rows stay locked (`UpgradesUnlocked` false). Copy:
existing “locked until first death” (`ShipCommands` already logs that). Do not
force a death.

## Rejected

Cap of two **per weapon**. Lighting CREW here. Gift Salvage to make two buys.
Granting SEC-D as the “second gun” instead of wheel. `timeScale = 0` shop tutorial.
