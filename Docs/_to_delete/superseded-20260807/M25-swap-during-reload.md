# M25 — Swapping during a reload is your choice

<!--
Status: later
Update trigger: when the reload-cancel rule or its setting changes
Last updated: 2026-08-06
-->

Switching weapons mid-reload currently discards the reload progress, silently. Keep the
behaviour, and put it behind a setting so the player picked it.

## Story

As a player, I want to decide whether swapping weapons costs me my reload, so that losing
it is a rule I accepted rather than one I discovered at the worst possible moment.

## Why it matters

`PlayerGun` cancels the outgoing slot's reload and zeroes its timer on every switch. Nothing
on screen says so, and the cost lands at exactly the moment a player is least able to
diagnose it — mid-fight, with something closing. It is a defensible rule: it makes the
swap a real trade rather than a free reload-cancel, and the Secondary's whole job is
covering the Primary's reload window.

But an invisible rule that punishes you is indistinguishable from a bug, and this one was
only found by reading the code to write `../../Play/Weapons.md`.

Leverage tier: **#6, information flows**, with a small **#5** — the rule itself becomes
selectable.

## Acceptance criteria

1. A setting, default **on** (reload progress is lost on swap — today's behaviour), that
   when off preserves the outgoing slot's reload progress and resumes it on return.
2. With the setting off, a reload that completed its remaining time while the slot was
   holstered is **not** silently finished — progress resumes from where it stopped, it does
   not tick while stowed. Otherwise "off" is a free reload and the Secondary loses its job.
3. The HUD shows that a holstered slot has a reload in progress, in both modes. In the
   default mode this is what makes the loss visible before it happens.
4. The setting is declared in M24's store like any other and appears on the options screen
   with its default.
5. An EditMode test drives both modes over a swap-out and swap-back and asserts the timer in
   each.

## Usable, or it did not ship

- **Driven** — the existing slot keys; the setting itself on the options screen.
- **Shown** — the holstered slot's reload state on the HUD, and the setting's current value.
- **Reads back** — the log line that already reports reloads names which slot, so a lost
  reload is visible in the transcript.

## Implementation

**Change** `Game/Player/PlayerGun.cs` — the switch path reads the setting instead of
unconditionally zeroing `reloadTimer`. The `reloadLogged` flag needs the same treatment or a
resumed reload logs twice.

**Change** `Game/Derelict/HudController.cs` — a holstered-slot reload indicator.

**Change** `Game/Flow/Settings.cs` — one declaration row.

## Watch

- Do not let a stowed slot's reload tick. That turns the Secondary into a free reload timer
  and quietly deletes the reason the slot exists.

## A conflict that has been removed

An earlier design, `SEC-B` — a Secondary that fires by itself during the Primary's reload —
would have required a stowed slot's reload to keep ticking, contradicting this milestone's
central rule. **`SEC-B` was rejected on 2026-08-07** on a broader ground: a Secondary's value
must come from what it does on its own. The rule below is therefore safe to fix in code.

## Depends on

**M24** — there is nowhere to put the setting until the options screen exists.
