# 2 — Picking each other up

<!--
Purpose: Revive as a teammate verb. Bleed-out window, hold-interact by any living
seat, partial HP. Host down no longer ends the Run (Zo, 2026-08-20 — M49 bends in
co-op). Unbanked survives a down and is wiped only on bleed-out.
Status: demo spec
Authority: implementable
Update trigger: when the revive cost stamp is overridden, when bleed-out length is
tuned, when M57's ship-wait path changes
Last updated: 2026-08-20
Depends: voice-coop.md, 1-two-holes-in-the-wall.md
-->

## Why this beat exists

Revive does not exist anywhere in the codebase. A repo-wide grep for revive, downed
and buyback returns seven prose comments and no type, method, field, RPC or binding.
What exists instead is M57's ship-wait: a **guest** at 0 HP is marked
`WaitingOnShip`, has their unbanked wiped, and self-serve respawns at full HP with no
cost, no timer and no teammate involved
(`NetPlayerAvatar.cs:301-310, 381-411`), gated on
`Phase == Tethered && Assault == Prep && Mode == HatchBank`.

That is a respawn, not a rescue. It asks nothing of the other player, which is why
the demo cannot sell co-op with it.

**Ruled by Zo, 2026-08-20: a downed host does not end the Run while another seat is
alive.** This overrides `M49` (`endsTheRunOnDeath`) in co-op. `Decisions.md:272`
records M49 as architecture-driven and left the question open; this closes it. M49's
behaviour stands unchanged for a solo Run, where there is no other seat.

## The verb

A seat at 0 HP enters a **downed** state: immobile, alive, look free, weapon holstered,
still targetable. A bleed-out timer runs. Any other living seat holds Interact on them
to bring them up at partial HP.

**Cost is exposure, not salvage** (stamped). Reviving means standing still, in the open,
next to something the enemies are already routing toward. That is the price. The
alternative, a salvage cost from the reviver's unbanked, would make rescue a bet and
come out of the crew pot; it is in Gaps because nobody knows which reads better without play.

**Unbanked survives the down.** Today a downed guest's pile is zeroed the instant HP
hits 0 (`:307-308`). If that stays, the revive is worthless the moment it lands. The
wipe moves to bleed-out expiry.

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
| **Have** | HP at 0 on `NetPlayerAvatar` (REPLICATED-STATE), `PlayerRegistry` for all/nearest. |
| **Build** | The downed state itself. HOST-ONLY authority on down/up transitions; the local seat drives only its own camera and input lock (R5). **Do not** wipe unbanked here. **Do not** call the host's death path. |

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
| **Goal** | Losing a teammate costs something real without ending the Run. |
| **Player** | Nobody reaches them in time. |
| **Game** | `demo.coop.bled_out` · Subtitle · Party · Game. **<name> is out. Their unbanked is gone. They can come back at the next window.** |
| **Standing** | Unchanged for the living |
| **Prompt** | — |
| **Log** | `[Down] bled out slot=<n> unbanked wiped=<n>` (build) |
| **Have** | M57's `WaitingOnShip` mark and self-serve respawn, gated on a live Hatch-bank Prep. |
| **Build** | Bleed-out expiry wipes that seat's unbanked and routes them into the existing `WaitingOnShip` path, which becomes the **fallback** rather than the primary. Buy-back at a live window is beat 5's. |

### 3 — The host goes down

| | |
|---|---|
| **Goal** | Prove the ruling in play, not in a doc. |
| **Player** | The host is downed. The Run continues. A guest revives them. |
| **Game** | Same lines as steps 0 and 1. No special-casing in copy. |
| **Standing** | Unchanged |
| **Log** | `[Down] slot=0 hatch=<id> bleedout=<s>s` plus `[Run] host down, run continues seats alive=<n>` (build) |
| **Have** | `M49` `endsTheRunOnDeath` on the host path. |
| **Build** | In co-op, a downed host does not call the Run-end path while any seat is alive. When the **last** living seat bleeds out, the Run ends Lost as today. Solo is unchanged: one seat, no other seat, M49's behaviour is what happens. Sim-side this is a count, not a host special case, which is the shape that keeps R4 (no singular player) honest. |

## Acceptance shape

Player action: one player is downed mid-Wave and the other walks over and holds
Interact to bring them up. Reads back: the down and revive log lines, the bleed-out
bar, and a Run that survives the host being downed.

## Verification

The PlayMode net fixture (`NetcodeIntegrationTest`, 11 suites) can hold the sim half:
down sets the state without wiping unbanked, revive restores partial HP, bleed-out
wipes and marks `WaitingOnShip`, and a downed host with a living guest does not end
the Run. The hold interrupt, the camera and whether a rescue under pressure feels
possible are eyes-on and cannot be gated there.

## Gaps

- **Revive cost.** Exposure only is stamped. Salvage from the reviver's unbanked is
  the alternative. Unknown which reads better; needs play data, not argument.
- **Bleed-out length.** Absent, not invented. It interacts with hatch spacing and wave
  pressure, so it is a tune after the layout exists.
- **Partial HP on revive.** Not specced. Same reason.
- Whether a downed seat can be revived **during** a bank window, or only between
  Waves, is open. Stamped permissive, because the beat 5 finale has no between.
