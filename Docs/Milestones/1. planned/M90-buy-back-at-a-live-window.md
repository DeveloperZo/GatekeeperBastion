# M90 — Buy back at a live window

<!--
Status: later
Update trigger: when the guest-wallet model is ruled, when the "live window"
ambiguity is settled, or when door state becomes networked
Last updated: 2026-08-20
-->

A dead guest pays their own banked Salvage to re-enter the Run while a Hatch-bank
window is live, and rejoins at the Hatch. Split out of `M79` on 2026-08-20: the
rule is ruled (`../../Decisions.md`, *Co-op buy-back*), but the data model it
needs does not exist, and building it inside `M79` would have tripled that
milestone and put client-authoritative money on the critical path of a solo beat.

## Story

As a player whose seat went down during a co-op bank window, I want to pay my
own Salvage to get back in before the window closes, so the most valuable
minute of the Run is one I can buy my way back into instead of watching.

## Why it matters

`../../Decisions.md` rules it: *"a dead player may buy back into the Run only
while a bank window is live, rejoining at the Hatch, paying from their own
banked Salvage; cost tunable. Respawns re-enter at the critical moment, when
Salvage is being banked."* Today a dead guest loads the Ship scene
(`NetShipWait`) and waits with no agency; the only way back is the free embark
once the window has already been survived — which is exactly the moment the
stake is gone.

Leverage is **#5 rules**: what a death costs and what it can be undone for.

## Blocked on — read before starting

Three things must be true before this is startable. None is code.

1. **The guest-wallet ruling.** Banked Salvage is a per-machine `MetaState`
   field, not per-seat sim state. `NetPlayerAvatar.SyncWallet` writes the
   `Banked` NetworkVariable **only for slot 0, from the host's own save**; a
   guest's `Banked.Value` is `0` forever and no RPC reports it to the host.
   So the host cannot read, verify or spend a guest's balance. Two shapes,
   and Zo picks:
   - **(a) Report-and-ask.** A guest→host `ReportBankedServerRpc`, a host
     `SpendBankedClientRpc` with an ack. Cheap, but the guest's save stays
     client-authoritative — a modified client can buy back for free. Acceptable
     only if co-op is explicitly friends-only.
   - **(b) Move banked into the sim** as a per-slot pile, host-authoritative,
     reconciled to each save at Run end. Correct, and a save-format change
     touching `MetaState.Json`, `SaveData` and every wallet reader.
2. **What "live window" means.** `Decisions.md` says "while a bank window is
   live". The existing free-embark path (`NetPlayerAvatar.TryAcceptEmbark`)
   requires the *survived* window — `Prep && Mode == HatchBank`. If buy-back
   means the assault is still running, the two are disjoint and both should
   exist. If it means the survived window, **buy-back is strictly dominated by
   waiting a moment and embarking for free**, and the feature is dead as
   specified. Owner: Zo.
3. **Dead host stays out of scope.** Open and recorded (Zo, 2026-08-17): a dead
   host rejoining collides with host-death-ends-the-run (`M49`), which is
   architecture-driven. Do not build the host branch until ruled.

## What is already there

- **Dead-seat detection** — `NetPlayerAvatar.TryMarkWaiting`, `WaitingOnShip`,
  `NetShipWait`. Host-authoritative and working.
- **A paid client→host interact pattern** to copy —
  `NetVerbs.SetRepairingServerRpc` → `GameController.RepairHatchFromNet` →
  `RunSim.RepairHatch`, which range-checks then spends.
- **A rejoin mechanism** — `TryAcceptEmbark` heals to full, clears
  `WaitingOnShip` and the owner resets to the authored Bastion spawn. It is
  free and gated on the opposite window; the movement itself is right.

## What is missing

- **A replicated bank-window flag.** `NetRunMirror` carries `Phase`, `Wave`,
  `Alive`, `Salvage`, `HatchHp`, `ElapsedSeconds`, `Banked`, `Unbanked`,
  `Intensity` — **not `WaveMode`**. A client cannot tell a Hatch-bank assault
  from a field-Terminal assault, so neither the prompt nor the `no live window`
  refuse can be produced client-side.
- **Networked door state.** `BastionDoor` is explicitly HOST-ONLY and each peer
  restores doors from *its own save*. A guest's door state is whatever their
  campaign unlocked, not the host's Run. Pre-existing co-op divergence — this
  milestone is the first to make it a gate.
- **The interact surface.** A dead guest has no Bastion prompts at all:
  `GameController.TickClientLocal` returns early on `NetShipWait.IsActive`. The
  press must originate on the Ship (`MapStation` / pause), the same place
  `RequestEmbark` already lives. "Driven by `E`/`T` at the Hatch" — as `M79`
  and beat 5 both said — is **not implementable**; "at the Hatch" describes the
  destination, not the button.

## Acceptance criteria

Written against ruling **(a)**; if Zo picks **(b)**, criteria 2 and 5 are
rewritten before starting.

1. A dead guest, while a bank window is live (per the ruling in *Blocked on* 2),
   sees a named buy-back offer on the Ship-side surface with its cost, and the
   same surface refuses named with `no live window` at any other time.
2. Accepting debits **that guest's own** banked Salvage on their own machine,
   never the host's. A Run in which a guest buys back leaves the host's
   `BankedSalvage` changed only by the host's own spends.
3. The guest rejoins alive at the Bastion Hatch spawn with full health, inside
   the same window, and both seats see them.
4. `[Demo] buyback seat=<n> cost=<c>` is logged host-side; a refused attempt
   logs its reason.
5. A guest who cannot afford it is refused named, and nothing is debited on
   either machine.
6. The shared in-Run pile is untouched: every seat's `Unbanked` is unchanged by
   a buy-back. Note that dying already wipes the seat's own `Unbanked`
   (`NetPlayerAvatar.TryMarkWaiting`), so buy-back is pure cost — it does not
   recover what the death lost. Confirm that is intended when tuning.
7. The dead-host branch is not built, and the attempt is refused named.

## Implementation

Not written until *Blocked on* clears. The shape under ruling (a) is: a
`WaveMode` byte added to `NetRunMirror`; `ReportBankedServerRpc` /
`SpendBankedClientRpc` on `NetPlayerAvatar`; a `TryNetBuyBack(from)` on
`GameController` following the `TryNetJuice` pattern; the offer surfaced on
`MapStation` beside the existing EMBARK row; and a decision on whether door
state is replicated here or deferred again.

## Verification

PlayMode, two seats, via the AutoVerify bridge — this is entirely
MonoBehaviour and network state, so the offline harness cannot see any of it.
Named per criterion once the implementation is written.

## Open

All three items in *Blocked on*, all owner Zo. This file exists because the
work is specifiable enough to name its blockers, not because it is startable.

## Depends on / Unblocks

Depends: `M79` (the zone-2 bank window this rides — **built and closed
2026-08-20**, so the window exists), `M49`/`M57` (down-and-wait). Unblocks:
nothing queued. `M80` explicitly does **not** depend on this — its finale
criterion for buy-back was withdrawn 2026-08-20 so the finale is not held
hostage to the wallet ruling; if this lands first the finale inherits it, since
it is the same bank-window rule. `M86` names buy-back as the eventual reversible path
for a downed body's look, and reads this milestone when it lands.
