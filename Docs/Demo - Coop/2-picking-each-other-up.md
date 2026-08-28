# 2 — Picking each other up

<!--
Purpose: Revive as a teammate verb. Bleed-out window, hold-interact by any living
seat, partial HP. Host down no longer ends the Boarding (Zo, 2026-08-20 — M49 bends in
co-op). Unbanked survives a down and is wiped only on bleed-out.
Status: demo spec
Authority: implementable
Update trigger: when the revive cost stamp is overridden, when bleed-out length is
tuned, when the priced-respawn rule moves
Last updated: 2026-08-25 (the down state and the priced self-respawn shipped; the
unbanked wipe on down is gone; Depends pointed at the real beat-1 filename)
Depends: voice-coop.md, 1-two-doors-to-hold.md
-->

## Why this beat exists

Revive does not exist anywhere in the codebase. A repo-wide grep for `revive` over
`Assets/Scripts/` returns nothing — no type, method, field, RPC or binding.

A **down state** and a **priced self-respawn** do exist and shipped after this beat was
written. A seat at 0 HP is marked down; `RunSim.SetSeatDown` freezes its Unbanked rather
than wiping it, because that pile is the purse; and `RunSim.TryRespawn` debits
`Respawns.CostFor` — **150** × (respawns taken this Boarding + 1) — from it, leaving the
remainder with the seat. The host and solo seat get the same offer at the DOWN overlay
(`GameController.Down.cs`); a waiting guest asks for it over
`NetPlayerAvatar.RequestRespawn`. Nothing else can pay: not the realm's bank, not another
seat.

That is still a respawn rather than a rescue. It asks nothing of the other player, which
is why the demo cannot sell co-op with it, and this beat's verb sits on top of it rather
than replacing it.

**Ruled by Zo, 2026-08-20: a downed host does not end the Boarding while another seat is
alive.** This overrides `M49` (`endsTheRunOnDeath`) in co-op, and the shipped down state
already behaves that way — `GameController.TryEnterLocalDown` holds the Boarding open
while any remote seat is fighting, even when the local seat cannot afford its own return,
and `NotifySeatWentDown` ends it only when nobody left can stand. Solo is unchanged in
substance: one seat, no other seat, so the Boarding ends unless that seat's own carry
covers the price.

## The verb

A seat at 0 HP enters a **downed** state: immobile, alive, look free, weapon holstered,
still targetable. A bleed-out timer runs. Any other living seat holds Interact on them
to bring them up at partial HP.

**Cost is exposure, not salvage** (stamped). Reviving means standing still, in the open,
next to something the enemies are already routing toward. That is the price. The
alternative, a salvage cost from the reviver's unbanked, would make rescue a bet and
come out of the crew pot; it is in Gaps because nobody knows which reads better without play.

**Unbanked survives the down.** This landed on its own:
`NetPlayerAvatar.TryMarkWaiting` no longer zeroes a downed seat's pile, it freezes it,
because the priced respawn buys from that pile. What this beat still owes is moving the
wipe to bleed-out expiry, and deciding what a bleed-out means for a seat that could have
paid.

## Functional storyboard

Continues from beat 1. This beat is authored to happen during zone 1's second Wave or
the bank window, whichever the tune lands on; it is not a scripted knockdown. The
composition does the work, the same way solo's authored losses do.

### 0 — Go down

| | |
|---|---|
| **Goal** | Understand instantly that this is not death. |
| **Player** | HP reaches 0. Camera drops, movement locks, look stays free, weapon holsters. |
| **Game** | Downed seat: `demo.coop.you_are_down` · Subtitle · Local · Game. **You are down. They can bring you up.** Other seats: `demo.coop.they_are_down` · Subtitle · Local · Game. **<name> is down.** |
| **Standing** | Downed seat: Hold on. Others: Bring them up |
| **Prompt** | Downed: bleed-out bar. Others, in range: `[Hold E] Revive <name>`. |
| **Log** | `[Down] slot=<n> hatch=<id> bleedout=<s>s` (build) |
| **Have** | HP at 0 on `NetPlayerAvatar` (REPLICATED-STATE), `PlayerRegistry` for all/nearest. The down state and its pile freeze (`RunSim.SetSeatDown`, `GameController.Down.cs`), and neither path wipes unbanked. |
| **Build** | The bleed-out timer and the immobile-but-alive body on top of that state. HOST-ONLY authority on down/up transitions; the local seat drives only its own camera and input lock (R5). **Do not** wipe unbanked here. **Do not** call the host's death path. |

### 1 — The rescue

| | |
|---|---|
| **Goal** | Pay for it in exposure. |
| **Player** | Living seat walks to them and holds Interact. Hold completes; downed seat stands at partial HP. Interrupt by moving, firing, or taking a hit resets the hold. |
| **Game** | On completion, revived seat: `demo.coop.up` · Subtitle · Local · Game. **Up. Get back to your hatch.** |
| **Standing** | Back to normal for both |
| **Prompt** | `[Hold E] Revive <name>` with fill. |
| **Log** | `[Down] revived slot=<n> by=<n> hp=<n>` (build) |
| **Have** | Hold-interact family exists (NAV repair 1.5 s, capsule 1.0 s in solo beat 0). |
| **Build** | The hold, the interrupt rule, partial-HP restore. Command routes guest → host through `NetVerbs` (R1); the host applies it. |

### 2 — Bleed-out

| | |
|---|---|
| **Goal** | Losing a teammate costs something real without ending the Boarding. |
| **Player** | Nobody reaches them in time. |
| **Game** | `demo.coop.bled_out` · Subtitle · Party · Game. **<name> is out. Their unbanked is gone. They can come back at the next window.** |
| **Standing** | Unchanged for the living |
| **Prompt** | — |
| **Log** | `[Down] bled out slot=<n> unbanked wiped=<n>` (build) |
| **Have** | `WaitingOnShip` and the priced self-respawn out of that seat's own frozen Unbanked, offered for as long as the Boarding is Tethered. |
| **Build** | Bleed-out expiry wipes that seat's unbanked and routes them into the existing `WaitingOnShip` path as the **fallback**. A wipe there also removes the purse, so it prices the respawn at infinity — whether the co-op bleed-out is allowed to do that, or whether it should leave the priced return standing, is the ruling in Gaps. |

### 3 — The host goes down

| | |
|---|---|
| **Goal** | Prove the ruling in play, not in a doc. |
| **Player** | The host is downed. The Boarding continues. A guest revives them. |
| **Game** | Same lines as steps 0 and 1. No special-casing in copy. |
| **Standing** | Unchanged |
| **Log** | `[Down] slot=0 hatch=<id> bleedout=<s>s` plus `[Run] host down, run continues seats alive=<n>` (build) |
| **Have** | Landed. `GameController.TryEnterLocalDown` holds the Boarding open behind the DOWN overlay whenever a remote seat is still fighting, and `NotifySeatWentDown` ends it only when no seat left can pay its way back. The log line above is what is missing. |
| **Build** | The log. The rule itself already reads seats rather than special-casing the host, which is the shape that keeps R4 (no singular player) honest. Solo is unchanged in substance: one seat, no other seat, so the Boarding ends unless that seat's own carry covers the price. |

## Acceptance shape

Player action: one player is downed mid-Wave and the other walks over and holds
Interact to bring them up. Reads back: the down and revive log lines, the bleed-out
bar, and a Run that survives the host being downed.

## Verification

The PlayMode net fixture (`NetcodeIntegrationTest`) can hold the sim half:
down sets the state without wiping unbanked, revive restores partial HP, bleed-out
wipes and marks `WaitingOnShip`, and a downed host with a living guest does not end
the Boarding. The first and last of those already pass on the shipped down state. The
hold interrupt, the camera and whether a rescue under pressure feels possible are
eyes-on and cannot be gated there.

## Gaps

- **Revive cost.** Exposure only is stamped. Salvage from the reviver's unbanked is
  the alternative. Unknown which reads better; needs play data, not argument.
- **Revive against the priced respawn.** Both now put the same seat back on its feet,
  one free and asking a teammate, one costing **150** × (respawns taken + 1) of that
  seat's own Unbanked. Whether the co-op demo keeps both, hides the priced one behind
  bleed-out, or drops the price in a party is unruled and is Zo's.
- **Bleed-out length.** Absent, not invented. It interacts with hatch spacing and wave
  pressure, so it is a tune after the layout exists.
- **Partial HP on revive.** Not specced. Same reason.
- Whether a downed seat can be revived **during** a bank window, or only between
  Waves, is open. Stamped permissive, because the beat 5 finale has no between.
