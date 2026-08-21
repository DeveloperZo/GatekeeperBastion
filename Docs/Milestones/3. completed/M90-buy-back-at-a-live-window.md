# M90 — Buy back at a live window

<!--
Status: shipped, Aug 2026
Closed on: 2026-08-21 (Zo). Playthrough (criterion 6) stamped by the close.
  Two-box host-save half of criterion 2 stays unprovable in-process — host and
  guest share Meta; `TryNetBuyBack` is the host-side proof it does not spend.
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-21
-->

A dead guest pays their own banked Salvage to re-enter the Run while a Hatch-bank
Assault is live, and rejoins at the Hatch. Split out of `M79` on 2026-08-20.
Rulings settled 2026-08-21: report-and-ask wallet, live window = Hatch-bank Assault,
doors deferred to `M95`.

## Story

As a player whose seat went down during a co-op bank window, I want to pay my
own Salvage to get back in before the window closes, so the most valuable
minute of the Run is one I can buy my way back into instead of watching.

## Why it matters

`../../Decisions.md` *Co-op buy-back*: a dead guest pays `HatchBank.BuyBackCost`
from their own banked Salvage and rejoins at the Hatch while a Hatch-bank
Assault is running. Free wait-then-embark (`M57`) stays on Hatch-bank Prep.
The two windows are disjoint.

Leverage is **#5 rules**: what a death costs and what it can be undone for.
The constant is **#12** (`../../Design/4-Data.md`, hatch-bank buy-back row).

## Kill criterion

If the first friends dry-run has a guest down during a Hatch-bank Assault and the
session log has neither `[Demo] buyback seat=` nor `[Demo] buyback refused —`,
the offer failed Shown. Revisit the Ship-side surface before retuning the cost.

## Acceptance criteria

1. A dead guest, while `WaveMode.HatchBank && AssaultState.Assault`, sees a named
   buy-back offer on NAV MAP and Ship pause with its cost, and the same surface
   refuses named with `no live window` at any other time. Hatch-bank Prep still
   offers free EMBARK.
2. Accepting debits **that guest's own** banked Salvage on their own machine,
   never the host's. `GameController.TryNetBuyBack` does not call `SpendSalvage`.
3. The guest rejoins alive at the Bastion Hatch spawn with full health, inside
   the same window, and both seats see them.
4. `[Demo] buyback seat=<n> cost=<c>` is logged host-side; a refused attempt
   logs `[Demo] buyback refused — <reason>`.
5. A guest who cannot afford it is refused named (`cannot afford`), and nothing
   is debited on either machine.
6. The shared in-Run pile is untouched: every seat's `Unbanked` is unchanged by
   a buy-back. Dying already wipes the seat's own `Unbanked`; buy-back is pure
   cost.
7. The dead-host branch is not built, and the attempt is refused named
   (`host cannot buy back`).

## Implementation

**Change** `Assets/Scripts/Sim/Run/HatchBank.cs` — `BuyBackCost`, `IsBuyBackWindow`,
`IsFreeEmbarkWindow`, `BuyBackRefuse`. The rule lives here so EditMode can see it.

**Change** `Assets/Scripts/Game/Net/NetRunMirror.cs` — `NetworkVariable<byte> Mode`
written from `sim.Mode`. Clients need it to tell Hatch-bank Assault from field
Assault.

**Create** `Assets/Scripts/Game/Bastion/GameController.BuyBack.cs` —
`TryNetBuyBack(NetPlayerAvatar from, long reportedBanked)`. Range-free; does not
spend. Split for the Game line cap.

**Create** `Assets/Scripts/Game/Net/NetPlayerAvatar.BuyBack.cs` —
`RequestBuyBack` reports this machine's `BankedSalvage`; server asks
`TryNetBuyBack`, then `SpendBankedClientRpc` (owner Meta) and `ApplyRejoin`
(same heal + Hatch spawn as free embark). Friends-only: a modified client can
lie about the report.

**Change** `Assets/Scripts/Game/Net/NetPlayerAvatar.cs` — `ApplyRejoin` shared by
free embark and buy-back.

**Change** `Assets/Scripts/Game/Ship/MapStation.cs` and `ShipController.cs` — wait
surface: Hatch-bank Prep stays EMBARK; live Assault is BUY BACK (cost); else the
press refuses `no live window`.

Rejected: moving banked into the sim per slot. Save-format change, nothing else
queued needs it, friends alpha is not the internet. Rejected: buy-back on the
survived Prep window. Free embark already covers it. Rejected: replicating door
state here. Guest spawns at the Hatch; `M95` criterion 9 owns guest door parity.

## Supporting work

- **Driven by** NAV MAP embark / pause embark while `NetShipWait.IsActive` — here.
- **Shown by** `BUY BACK (<cost>)` on that button — here.
- **Reads back as** the guest body at Hatch spawn, banked falling on their save,
  `[Demo] buyback seat=<n> cost=<c>` — here.

## Verification

1. `HatchBankTests.BuyBackRefuse_LiveWindow_AllowsGuestWhoCanPay` — `Tools/verify/run.sh --filter BuyBackRefuse`. Expect the live-window allow and the four named refuses.
2. `NetDownWaitTests.BuyBack_HatchBankAssaultRejoins_RefusesOtherwise` — IntegrationCheck. Terminal refuse + unaffordable refuse debit nothing; funded Assault rejoins; `TryNetBuyBack` leaves `GameSession.Meta.BankedSalvage` unchanged; Unbanked unchanged. In-process host and guest share Meta, so the owner spend then drops that same purse — criterion 2 on two boxes is host save unchanged after a guest buy-back.
3. `NetDownWaitTests.BuyBack_HostAttempt_RefusedNamed` — host `RequestBuyBack` logs `host cannot buy back`; Run stays Tethered.
4. `NetRunMirrorTests.Mirror_ReplicatesReadout_DeliversEndSummary` — client `Mode` matches host HatchBank.
5. `DownWait_HatchBankPrepEmbarks_RefusesOtherwise` still green — free embark is not replaced.
6. Playthrough path — two seats, guest down during a Hatch-bank Assault: NAV MAP shows `BUY BACK (450)`; accept → guest at Hatch, log `[Demo] buyback seat=1 cost=450`. After the window, the same button refuses `no live window`. Prep after survival still EMBARK.

## Watch

`CreditBankedClientRpc` already mutates owner Meta outside `ShipCommands`. Buy-back spend is the same shape. `NetRunMirror.Phase` is `AssaultState`, not `RunPhase`.

## Depends on / Unblocks

Depends: `M79` (built), `M49`/`M57` (down-and-wait). Unblocks: nothing queued.
`M80` does not depend on this. `M86` reads this when it lands. Door parity is `M95`.
