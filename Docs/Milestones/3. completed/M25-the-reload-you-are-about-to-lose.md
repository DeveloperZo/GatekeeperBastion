# M25 — The reload you are about to lose

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-08
-->

Switching weapons mid-reload throws the reload away. The HUD says so before it happens, the log
names it when it does, and a setting (default on) can keep the progress instead.

## Done when

**Met (code + EditMode Path 1).** Warning while reloading with a swap target; discard and
completion lines name the slot; `DiscardReloadOnSwap` defaults on; preserve / stowed-never-ticks /
interruptible resume covered by `ReloadSwapTests`. AutoVerify EditMode **245 / 0** (five
`ReloadSwapTests` + five `SettingsTests`) — `gkb_verify_report.json`, 2026-08-09T01:53:33Z.
PlayMode suite still carries one pre-existing fail (`ShoveDoesNotUncommit`, M20) — not this
milestone.

## Remaining to close

| # | Status | Evidence |
|---|---|---|
| Playthrough path | **open** | Embark → mid-reload read `SWAP DISCARDS THIS RELOAD` → swap → Combat discard line; `O` options → turn discard off → preserve + strip shows `reload paused` |
| Path 2 HUD assert | **open** | No automated draw assert; needs a played Run |

`OptionsScreen` (M24, shipped) draws every `Settings.Declarations` row, so the discard
toggle is on that screen. Path 2 playthrough still open for both.

## What shipped

- `Sim/Settings/` store + `SettingId.DiscardReloadOnSwap` (default true); `PlayerPrefsSettingsStore` boots.
- `PlayerGun`: per-slot accessors, `EffectiveReloadSeconds`, setting-aware swap, discard log,
  completion line names slot, injectable `TickReload(dt)`, fire interrupts resumed reload with rounds left.
- `HudController`: `SWAP DISCARDS THIS RELOAD` (not HOLSTERED); stowed `reload paused` when setting off.
- `ReloadSwapTests` / `SettingsTests`; Play docs (`Weapons.md`, `Staying-Alive.md`) updated.

## Story

As a player, I want to know that swapping now costs me my reload, so that losing it is a
decision I made rather than something the game did to me without comment.

## Why it matters

`PlayerGun.TrySelectSlotPublic` zeroed the outgoing slot's `reloadTimer` on every accepted
switch. Nothing said so. The HUD's `RELOADING` bar was replaced by the `CLIP x/y` line, which
read as the reload being hidden, not destroyed. Session log
`Logs/GatekeeperBastion_2026-08-08_17-00-15.log` shows `switched to Secondary` / `switched to
Primary` with no discard line, and `reloaded — clip 17` with no slot name.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.**

## Acceptance criteria

1. HUD warns before discard while active slot reloads and another slot holds a weapon — **Met**
   (`SWAP DISCARDS THIS RELOAD` when setting on).
2. Discard emits one Combat log line naming slot and seconds remaining — **Met**
   (`SwapDiscardsWhenSettingOn`).
3. Reload-completion line names its slot — **Met** (`CompletionLineNamesSlot`).
4. Setting default on in the Settings store — **Met** (`DiscardReloadOnSwapDefaultsOn`).
5. Setting off keeps progress; stowed slot never advances — **Met**
   (`SwapPreservesWhenSettingOff`, `StowedSlotNeverAdvances`).
6. Resumed reload with rounds left is interruptible by fire — **Met**
   (`ResumedReloadIsInterruptible`).
7. `SlotIsReloading` / `SlotReloadProgress` — **Met**.
8. Stowed paused-reload marker when setting off — **Met** (slot strip `reload paused`).

## Supporting work

- **Driven** — slot keys here; setting toggle via `OptionsScreen` / `O` (M24 owns the screen).
- **Shown** — warning + paused marker here.
- **Reads back** — discard + completion log lines here.

## Verification

Path 1 green — see Done when. Path 2 / playthrough still open in Remaining.

## Watch

- Do not let a stowed slot's reload tick.
- M21 shortens the window this setting arbitrates; do not tune around it.
