# M36 — The Hatch is always on

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-12
-->

Hatch integrity becomes a fifth always-on Bastion HUD host on the top edge centre — same
Shift bar recipe as player Health — so the loss condition is readable while the crosshair is
elsewhere. The left-drawer Hatch bar goes away; one fact, one place.

## Story

As a player, I want Hatch integrity on screen for the whole fight without opening a drawer,
so that holding the door is something I am doing rather than something I remember to check.

## Why it matters

**The Hatch is the Run's fail state and it is not on the always-on surface.** After M35,
Hatch HP lives in the left drawer (`BastionHudView.SetDrawerHatch`) plus a `MsgPlayer` attack
line (`HudController.Fight`). Quiet Prep is four corner cards and a crosshair
(`../../Milestones/3. completed/M35-one-look-drawn-from-one-place.md`). The door that ends
the Run is opt-in UI.

**That is the same gap Zo named twice** — "the UI is missing information severely"
(2026-08-08) and "no incentive to move away from the Hatch" (`../../Design/1-Experience.md`).
M30 gives a reason to leave the bay; this milestone is what makes leaving survivable to read.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No constant, no rule —
only what is visible without a keypress.

## Acceptance criteria

### Agent-closable (code + scene + bindings)

1. **`Hud Hatch` exists under `Canvas - Dynamic` in `Bastion.unity`**, active by default, and
   is shown whenever fight corner cards are shown (`ShowCornerCards` / `ShowFight`). Absent
   from Ship / Splash / Main Menu hierarchies.
2. **Placement is top edge centre:** anchor `(0.5, 1)`, pivot `(0.5, 1)`,
   `anchoredPosition.y = -24` (±1), width 320–400, height 64–80. Profile (TL) and Score (TR)
   unchanged.
3. **Contents:** Shift Health-row recipe — Icon Border + Slider fill + label TMP + value TMP
   (`hp/max`). No homemade bar chrome. `BastionHudView` serializes `_hatchRoot`, `_hatchLabel`,
   `_hatchValue`, `_hatchBar` (all required — BindingValidator fails if null).
4. **`SetDrawerHatch` / `_drawerHatchLabel` / `_drawerHatchBar` are gone** from
   `BastionHudView`. The left-drawer Armor/Hatch row GameObject is inactive or deleted.
5. **Nothing new is counted** — reads `sim.Ship.Hatch.Hp` / `MaxHp` and the existing
   `_hatchAlert` timer. No new accumulator field (`../../Decisions.md`).
6. **`../../Decisions.md` Bastion fight HUD table has a `Hud Hatch` row** (top centre, size,
   contents). Four corner hosts stay.
7. **`gkb_bindings_report.json` reports 0 missing bindings** on Bastion after the wire
   (`GatekeeperBastion/Validate Scene Bindings` or `gkb_bindings_request.txt`).

### Zo-closable (Game view — agent must not mark these done)

8. **Integrity readable with the value TMP disabled** — screenshots at 100 / 70 / 40 / 15
   percent fill; Zo orders the four correctly. Record in
   `../../GatkeeperZo Notes/playthrough.md`.
9. **Colour is never the only channel** — fill length differs across those four shots.
10. **Hatch HP drop emphasises the card ≥1.5 s** (label weight and/or flash). With animation
    disabled / emphasis off, criteria 1–3 and 8–9 still hold on the static fill.
11. **Player action:** Hatch bay, Wave 3, crosshair on a Gatekeeper ~30 m away — Zo states
    integrity band {full, high, mid, low} without opening a drawer, then checks the number.

## Completion bar

**Closed 2026-08-12.** Agent criteria 1–7 evidenced in the Watch table; Zo criteria 8–11
closed by playtest verdict (*"hatch hud addition is great for this point in time"*) —
recorded in `../../GatkeeperZo Notes/playthrough.md`.

## Implementation

**Author** `Hud Hatch` under `Canvas - Dynamic` by cloning the `Hud Vitals/Health` row (Shift
recipe) into a top-centre host; add a value TMP for `hp/max`. Use `Assets/_Scratch/` + a
sentinel for the scene edit (new fileIDs) — not a committed menu row.

**Change** `BastionHudView` — `_hatchRoot` / `_hatchLabel` / `_hatchValue` / `_hatchBar` /
`_hatchGroup` (optional CanvasGroup for flash); `SetHatch`, `SetHatchAlert`; include hatch in
`ShowCornerCards` / `HideChrome`.
**Change** `HudController.Fight` — push Hatch into `SetHatch` / `SetHatchAlert`; stop drawer
Hatch writes; `MsgPlayer` attack toast may stay as text-only (no HP bar duplicate).
**Change** `../../Decisions.md` — add the `Hud Hatch` row.
**Delete** drawer Hatch bindings and deactivate the drawer Armor/Hatch row.

Rejected: **bottom edge centre.** Vitals and Weapon own the bottom; `MsgPlayer` clears the
left drawer top.
Rejected: folding Hatch into `Hud Vitals`. Different fail meanings.
Rejected: prior tether instrument (pulses, door marks, Prep/Assault re-skin, spawn marks).
Rejected: world-space Hatch readout.

## Supporting work

- **Driven** — no new input.
- **Shown** — always on in a Tethered Run.
- **Reads back** — fill + value on the card; criteria 8 and 11.

## Verification

1. Criteria 1–2 — scene YAML / Hierarchy: `m_Name: Hud Hatch`, rect values.
2. Criteria 3, 4, 7 — BindingValidator report 0 missing; `grep SetDrawerHatch` empty under
   `Assets/Scripts`.
3. Criterion 5 — `git diff Assets/Scripts` adds no cross-frame accumulator.
4. Criterion 6 — `Decisions.md` table row present.
5. Criteria 8–11 — Zo play + screenshots; agent prepares the build only.

## Watch

- BindingValidator green ≠ Game view painted.
- Do not animate the numeric value TMP.
- True Shadow glow on an always-on centre card is expensive
  (`../../Architecture/3-Unity-Runtime.md`).
- Pin Shot / Prep–Assault instrument work stay out of scope.

### Close-out 2026-08-12

| Criterion | Evidence |
|---|---|
| 1–2 | `Bastion.unity`: `Hud Hatch` under `Canvas - Dynamic`; anchors `(0.5,1)`, pos `(0,−24)`, size `360×72` |
| 3, 7 | `BastionHudView` wires `_hatchRoot/_hatchLabel/_hatchValue/_hatchBar/_hatchGroup`; `gkb_bindings_report.json` `2026-08-12T15:34:50Z` **missingCount=0** |
| 4 | Drawer `Armor` `m_IsActive: 0`; `SetDrawerHatch` gone from `Assets/Scripts` |
| 5 | No new accumulator — reuse `_hatchAlert` / hatch HP caches |
| 6 | `Decisions.md` table has `Hud Hatch` row |
| 8–11 | **Zo playtest 2026-08-12** — Hatch always-on host accepted for this stage of the game (`playthrough.md`) |

## Depends on / Unblocks

**Depends on M35** (shipped). Unblocks nothing formally. Pairs with **M30**.
