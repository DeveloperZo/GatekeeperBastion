# 3 — Ship after first loss

<!--
Purpose: First ARMORY panel open (wheel unlocked by loss 1), at most two
Secondary wheel purchases — ladder rung 1 — multiplayer unlocks on the first
purchase. Exit standing is state-keyed (no buyable upgrades remain). CREW menu
unlock lands on this Docked (re-key); FAB stays dark.
Status: demo spec
Authority: implementable
Update trigger: when the wheel ladder or the multiplayer gate moves, when SEC-D
grant is overridden, when the exit-standing state key changes
Last updated: 2026-08-17
Depends: voice.md, 0-wake-on-a-dead-ship.md (ARMORY powered there),
2-first-hatch-bank.md
-->

## Why this beat exists

ARMORY was powered in beat 0 (ship terminal) and has already handed over the
MK-1 — but its panel has never opened. The wheel unlocks on first loss
(`UpgradesUnlocked = PodEjections >= 1`) with ten tiers and no cap. This beat
opens the shop, caps it at two purchases — rung 1 of the wheel ladder (Zo,
2026-08-17): **2 Secondary buys after loss 1; at loss 2 a Primary + 2 upgrades
are granted outside that count** (loss 2 is zone 2's bank, another editor's
beat; [`README.md`](README.md) states the ladder) — and unlocks multiplayer on
the first one. The demo's rule is you play the loop once solo before friends
join ([`README.md`](README.md)).

## Functional storyboard

Continues from beat 2 step 3: `demo.first_hatch_down` played on Docked (it wins
any same-frame Lock collision — `voice.md`). Standing arrives as **Spend
Salvage at the ARMORY**. They hold whatever dripped before the Run ended — the
authored aim is **~500** banked (tunable, beat 2), which funds both buys
(150 + 300 = 450); a short tune still funds one. Do not gift Salvage.

### 0 — Back aboard

| | |
|---|---|
| **Goal** | Orient: ARMORY is the job. CREW heals; FAB is dark. |
| **Player** | Walk to ARMORY. |
| **Game** | — (`demo.first_hatch_down` already carried the instruction) |
| **Standing** | Spend Salvage at the ARMORY |
| **Prompt** | ARMORY, wheel unlocked: `[E] ARMORY — equip and focus-upgrade the weapon in your hands` (live string). CREW: `[E] CREW QUARTERS — restore health`; the CREW **menu** is unlocked from this Docked (`DemoCrewMenuUnlocked` re-keyed to first Docked after first loss, Zo 2026-08-17 — replaces the Wave-2-seen key); its panel spec and lines stay beat 4's. FAB: `[E] FABRICATION — unpowered`. |
| **Log** | existing `[Ship] aboard the ship — <n> banked, 1 run(s) lost, 0 cleared` |
| **Have** | `UpgradesUnlocked = PodEjections >= 1` is now true. `DemoArmoryPowered` true since beat 0. |
| **Build** | Write `DemoCrewMenuUnlocked` on this Docked (re-key — v9, README Shared flags). Nothing else this step. |

### 1 — Open the ARMORY panel (first menu after NAV)

| | |
|---|---|
| **Goal** | See the wheel: focus a spoke, pay, the gun gets better. |
| **Player** | Interact Down on ARMORY. Panel opens — the direct-grant behaviour from beat 0 ends when `UpgradesUnlocked` flips. Wheel on the MK-1 (ring: Damage / Ability / Reload / Clip / FireRate) plus the SEC-D row. |
| **Game** | — (the panel is the reader) |
| **Standing** | Spend Salvage at the ARMORY |
| **Prompt / panel** | Deal line `Upgrades stay on this weapon.` (live). Wheel rows show remaining demo buys (build). |
| **Log** | — |
| **Have** | Panel, wheel, deal line, catalog rows all live. |
| **Build** | `DemoStationGate` Armory branch: grant path while `!UpgradesUnlocked`, panel after. Wheel rows show `demoBuysLeft`. |

### 2 — First purchase → multiplayer unlocks

| | |
|---|---|
| **Goal** | Buy once; the Ship can take a crew now. |
| **Player** | Focus a spoke, buy (150). |
| **Game** | `demo.mp_unlocked` · Lock · Local · Game. **The upgrade stays on the weapon in your hands. And the Ship answers hails now — friends can join you.** Once per save, on the first successful wheel buy. |
| **Standing** | Spend Salvage at the ARMORY — unless the buy leaves no affordable upgrade (state key, step 3), then **EMBARK from the NAV MAP** |
| **Log** | existing wheel success line + `demoBuysLeft=1` then `[Demo] multiplayer unlocked` |
| **Have** | `BuyWheelUpgrade` gate chain (owned → ring → `UpgradesUnlocked` → tier < 10 → bank); success awards centre +5, neighbours +3, Save. Costs `150 + 150×tier`. |
| **Build** | `DemoWheelBuysLeft` decrement (v9). F10/F11 + lobby gate reads `DemoWheelBuysLeft < 2 || !DemoActive` — no new flag. The line + log. |

### 3 — Second buy; the cap and the exit standing

| | |
|---|---|
| **Goal** | The cap is named, and the exit standing flips itself. |
| **Player** | Buy again (300) if banked covers it. A third click refuses. |
| **Game** | On a third-buy attempt: `demo.wheel_cap` · Lock · Local · Game. **Two purchases is the cap for now. The NAV MAP is ready when you are.** Once per Ship visit. The line no longer produces the standing. |
| **Standing** | **State-keyed** (Zo, 2026-08-17 — replaces the third-refuse trigger): flips to **EMBARK from the NAV MAP** when **no buyable upgrades remain** — `DemoWheelBuysLeft == 0`, or banked below the next buy's cost — evaluated on Docked and after every buy / refuse / spend. Until then it stays **Spend Salvage at the ARMORY**. |
| **Prompt / panel** | Cap refuse is named, not the salvage-short line. Panel copy `two purchases this demo — NAV is ready`. |
| **Log** | `[Demo] wheel refused — demo cap`; on the flip, `[Demo] exit standing set — no buyable upgrades remain` (build) |
| **Have** | Existing named refuses in `ShipCommands` to pattern-match; salvage-short refuse if banked < cost. |
| **Build** | Cap refuse before `SpendSalvage`; the line; both logs; the state-keyed standing evaluator. |

### 4 — Optional: SEC-D

| | |
|---|---|
| **Goal** | A second free Secondary exists; it is not a wheel purchase. |
| **Player** | May buy SEC-D for 0 (catalog row, `BuyWeapon` path). |
| **Game** | — |
| **Standing** | per the state key (SEC-D is cost 0 and not a wheel upgrade — it never counts as a “buyable upgrade” for the key) |
| **Log** | existing `[Ship] bought SEC-D for 0 — …` |
| **Have** | SEC-D cost 0, separate from `BuyWheelUpgrade` — the 2-buy cap does not count it and it does not unlock multiplayer. |
| **Build** | Nothing. |

NAV this visit: beat 4's spec (plain EMBARK — the NAV skip is dead; post-loss
refight-avoidance lives at the terminal, where the zone's deepest terminal
retriggers the composite, partial start electable). **Beats 3 and 4 ship in one
milestone** — without beat 4's retrigger they could EMBARK into a from-scratch
chain. This file specifies ARMORY only.

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 3 |
|---|---|---|---|
| ARMORY powered | yes (after beat 0) | `DemoArmoryPowered` from the ship terminal | Have |
| Wheel unlock on first loss | yes | `UpgradesUnlocked` (`PodEjections >= 1`) | Have — also the panel-vs-grant switch |
| `BuyWheelUpgrade` gates + logs | yes | `ShipCommands` | Have |
| Wheel award / costs | yes | centre +5, neighbours +3; 150 / 300 | Have |
| Wheel lock copy | yes | `LOCKED — survive a death first` | Have (survive branch) |
| SEC-D free row | yes | `BuyWeapon`, cost 0 | Have |
| CREW heal-on-interact / FAB dark | spec | beat 0 gates | Have (built in beat 0's milestone) |
| CREW menu unlock re-key | no | v9 `DemoCrewMenuUnlocked` (README) | Build — write on this Docked (Zo, 2026-08-17); panel spec stays beat 4's |
| `DemoWheelBuysLeft` | no | v9 | Build |
| State-keyed exit standing | no | — | Build (Zo, 2026-08-17) |
| Multiplayer gate + unlock line | no | F10/F11 + lobby path | Build |
| Voice ids | no | [`voice.md`](voice.md) | Build |

## The change

1. ARMORY panel replaces the beat-0 grant path once `UpgradesUnlocked`.
2. Wheel cap = ladder rung 1 (Zo, 2026-08-17): `DemoWheelBuysLeft` 2 → 0,
   named refuse at 0. Not reset per visit. Loss 2's granted Primary + 2
   upgrades land **outside** this count (another editor's beat). Do not change
   `WeaponTier.Cost`.
3. Multiplayer unlock on the first successful buy: the F10/F11 / lobby refuse
   (built with beat 0) starts passing; `demo.mp_unlocked` names it.
4. Exit standing is **state-keyed** — no buyable upgrades remain — replacing
   the third-refuse producer (Zo, 2026-08-17).
5. CREW menu unlock re-keys to this Docked (Zo, 2026-08-17); the claim flow and
   its lines stay beat 4's. FAB stays dark until `DemoActive` ends.

## Create

| Path | Role |
|---|---|
| Panel copy | `ArmoryPanelView` deal line: remaining demo buys while `DemoActive` |

## Change

| File | Change |
|---|---|
| `ShipCommands.BuyWheelUpgrade` | After existing gates: `DemoActive && DemoWheelBuysLeft <= 0` → named cap refuse. On success decrement; first decrement fires `demo.mp_unlocked` + log |
| `DemoStationGate` | Armory: panel when `UpgradesUnlocked`, grant path before. CREW: menu when `DemoCrewMenuUnlocked` (re-key) |
| `ArmoryStation` | Wheel rows show remaining buys |
| Demo runner / `DemoProgress` | State-keyed exit standing (no buyable upgrades remain); `DemoCrewMenuUnlocked` write on first Docked after first loss |

## Delete

Nothing.

## Numbers

| Name | Value | Source |
|---|---|---|
| Banked at this Docked (authored aim) | **~500**, tunable | Zo, 2026-08-17 (beat 2 overwhelm aim) |
| Buy 1 / Buy 2 | 150 / 300 | `WeaponTier.Cost(Secondary, 0/1)` |
| MaxTier | 10 | live wheel; gated to 2 buys |
| Wheel award | +5 centre, +3 neighbours | `MetaState.AwardWheel` |
| Multiplayer unlock | first successful wheel buy | this spec |
| Ladder rung 2 | Primary + 2 upgrades granted at loss 2, **outside** the 2-buy count | Zo, 2026-08-17 — another editor's beat |

At the ~500 aim both buys land and a third click meets the cap refuse. If the
tune lands short (e.g. banked 300): first buy lands (and unlocks multiplayer),
the second shows the existing salvage-short refuse — and the exit standing
still flips, because it is state-keyed on no-buyable-remaining, not on the cap
refuse. Do not gift Salvage.

## Multiplayer

This beat **is** the unlock. Before the first buy: F10/F11 and lobby entry
refuse with `[Demo] multiplayer locked — buy an upgrade first`. After it: live
`M72` rules (lead, Ready, countdown). A joining friend runs their own solo
beats 0–3 first — their save carries the same gate.

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.mp_unlocked` | Lock | Local | first successful wheel buy, once per save | per the state key |
| `demo.wheel_cap` | Lock | Local | third-buy attempt at cap, once per visit | — (standing is state-keyed, not line-produced) |

No party PA "everyone shop."

## Driven / Shown / Reads-back

- **Driven by** ARMORY Down (panel now); wheel click; the state-keyed standing
  evaluator.
- **Shown** the wheel; remaining buys; the standing flip; F10 refuse before the
  buy, lobby after.
- **Reads-back** existing wheel success line + `demoBuysLeft=<n>`,
  `[Demo] multiplayer unlocked`, `[Demo] wheel refused — demo cap`,
  `[Demo] exit standing set — no buyable upgrades remain`.

## Logs

| Event | Line | Status |
|---|---|---|
| Wheel ok | existing line + `demoBuysLeft=<n>` | build suffix |
| MP unlock | `[Demo] multiplayer unlocked` | build |
| MP still locked | `[Demo] multiplayer locked — buy an upgrade first` | build (beat 0 gate) |
| Cap | `[Demo] wheel refused — demo cap` | build |
| Exit standing | `[Demo] exit standing set — no buyable upgrades remain` | build |

## Traps

- Re-adding an ARMORY repair step — power came from the beat-0 ship terminal;
  this beat's gate is `UpgradesUnlocked`, nothing else.
- Treating cost-0 SEC-D as a wheel purchase — it neither counts against the
  cap, nor unlocks multiplayer, nor holds the exit standing open.
- Resetting `DemoWheelBuysLeft` to 2 on every Ship visit.
- Unlocking multiplayer on panel **open** instead of purchase.
- Keying the exit standing to the third refuse — a seat that can afford only
  one buy would never see it. The key is state: no buyable upgrades remain.
- Counting loss 2's granted Primary + 2 upgrades against the 2-buy cap —
  ladder rung 2 is outside the count.
- Re-locking the CREW menu until beat 4 — the unlock re-keyed to this Docked
  (Zo, 2026-08-17); the claim flow and lines are still beat 4's spec.
- Unlocking FAB "so Hatch Armor matches the designed Hatch-bank." FAB stays dark.
- Granting a Primary this beat. They hold a Secondary; the Primary is loss 2's
  grant (another editor's beat).

## Depends / Unblocks

Depends: beat 0 (`DemoArmoryPowered`, MP gate built), beat 2 authored loss +
Lost (`PodEjections >= 1`). Banked is dripped remainder — authored aim ~500
(tunable), not a guaranteed 450. Unblocks: beat 4 EMBARK + composite retrigger
(same milestone); multiplayer for the rest of the demo.

## Verification

1. EditMode: `BuyWheelUpgrade` twice succeeds only if banked ≥ 450; third
   refuses cap without calling `SpendSalvage`. At banked 300: first succeeds,
   second named salvage-short.
2. EditMode: `DemoProgress.MultiplayerUnlocked` false at buys-left 2, true at 1,
   true when `DemoActive` false.
3. EditMode: exit standing flips at buys-left 0, and at buys-left 1 with
   banked below the next cost — no third-buy click required.
4. PlayMode: ARMORY Down opens the panel (not the grant) after a loss; two
   buys; third named refuse; standing reads EMBARK from the NAV MAP once no
   buyable upgrade remains. CREW: heal-on-interact still works; the menu gate
   follows `DemoCrewMenuUnlocked` (true from this Docked — panel spec beat 4).
5. PlayMode: F10 before the first buy logs the locked line; after it, hosts.
6. No `[Dev]`.

## Survive branch from beat 2

The first window is **authored unclearable** (Zo, 2026-08-17), so this branch
is a tuning-defect guard, not a path. If `first-hatch survived` ever logs with
`PodEjections == 0`: ARMORY Down still takes the grant path —
`UpgradesUnlocked` is false and the wheel copy `LOCKED — survive a death
first` applies at the panel gate; multiplayer stays locked (no purchase is
possible). A survived-bank **Ship return** (beat 2) ends the Run **Docked**
with `PodEjections` still 0 — the same locked state, by design: return-to-Ship
never skips the unlock deaths, and the wheel key stays `PodEjections >= 1`
because the demo's first ship return is necessarily loss 1. Do not force a
death; retune the overwhelm (beat 2).

## Rejected

Cap of two **per weapon**. ARMORY hold-repair (beat 0 owns power now).
Multiplayer unlock on Escaped instead of first purchase. Gift Salvage to make
two buys. `timeScale = 0` shop tutorial. Exit standing produced by the third
refuse (replaced 2026-08-17 — state-keyed).
