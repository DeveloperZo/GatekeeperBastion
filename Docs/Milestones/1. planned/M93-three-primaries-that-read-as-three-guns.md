# M93 — Three primaries that read as three guns

<!--
Status: planned
Update trigger: when a Primary is added, retired or re-statted, when M82's viewmodel
contract changes, when PrimaryRowsOpen moves
Last updated: 2026-08-20
-->

The three shippable Primaries are bought, held and fired, and each one is
distinguishable from the other two while playing rather than by reading the catalog.

**Count correction, stated once.** There are **three** Primaries, not four. `VK-7` is
disabled in `WeaponCatalog.cs`, kept only so old saves deserialize, and is excluded
from `IsAvailable` and `ShopWeapons`. The shippable set is **BR-12 Shotgun** (900),
**MR-35 Rifle** (1200), **PRI-C** (1500).

## Story

As a player picking a Primary, I want the three to feel like different weapons in my
hands, so that the choice at the ARMORY is a real one rather than a number comparison.

## Why it matters

**The differentiation is already designed and already in the sim.** The three do not
share a stat profile: BR-12 is 7 pellets at 7° over 14 m, MR-35 is 32 damage with
pierce 2 at ×0.26 over 60 m, PRI-C is 90 damage at 0.6 shots/s with pierce 3 at ×0.75
over 90 m. Each carries its own ability with real code behind it in
`PlayerAbility.cs`: `Br12Concussive`, `Mr35Pin`, `PriCRailshot`. Nothing here needs
designing.

**What is missing is everything that makes a difference legible.**
`WeaponViewModel.cs` drives Fire, Reload and Speed on an Animator and no controller
exists anywhere in the project, so the code path is a no-op (`M82`). Without motion,
three weapons with different fire rates read as one weapon with different numbers.
This is exactly the case `../../_meta` warns about: judging a mechanic with no
feedback under-reads it.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No stat moves in
this milestone. What changes is that the differences already in the data become
visible.

## Supporting work

**Shown** depends on `M82` (one shared viewmodel animator, four clips, one
controller). If `M82` slips, this milestone slips with it: the criteria below cannot
be met by a static weapon. `M66` (hit confirm and audio tells) sharpens this further
and is **not** a dependency; criteria here are met without it.

## Acceptance criteria

1. All three Primaries appear in the ARMORY Primary list, are purchasable with enough
   salvage, and equip to the Primary slot.
2. Pressing `1` in a Run selects the equipped Primary and the HUD names it.
3. Each Primary's ability fires on its own input, applies its own effect, and writes
   its own log line: BR-12 concussive push, MR-35 pin, PRI-C railshot arming.
4. Firing each Primary plays the viewmodel fire motion, and reloading plays the reload
   motion, at that weapon's own rate.
5. `PriC` railshot arms for its window, and the armed state is visible on the HUD
   before it expires.
6. `Br12Concussive` moves at least one gatekeeper a measurable distance, and the log
   line reports the distance.
7. `Mr35Pin` holds a gatekeeper for its duration, and the pinned body does not advance
   toward a hatch during it.
8. A player who owns all three can swap between them at the ARMORY across three
   consecutive Runs with no loss of upgrades on any of them.
9. Nothing in this milestone changes a damage, rate, clip, reload, range, pellet,
   spread, pierce or falloff value in `WeaponCatalog.cs`.

## Implementation

1. **Land** `M82` first. It is a separate milestone and is not restated here.
2. **Check** the parked `_m61_2_handoff/` tree against the current source before
   applying anything from it. It was written 2026-08-14, has never compiled against
   this tree, and `HANDOFF.md` is the entry point. Apply or discard deliberately; do
   not merge it blind.
3. **Change** `Game/Ship/ArmoryStation.cs` only if `DemoProgress.PrimaryRowsOpen(m)`
   hides rows in a state this milestone needs to test. The gate itself is demo
   progression and stays; the fix, if one is needed, is a dev path to open it, not a
   change to the rule.
4. **Change** `PlayerAbility.cs` only where an ability's log line is missing or does
   not carry the number criterion 6 or 7 asks for. The abilities themselves work; do
   not rewrite them.
5. **Create** an EditMode test asserting the three catalog rows differ on at least
   three stat fields each, so a future retune cannot quietly collapse them into each
   other.

Rejected: adding a fourth Primary. `VK-7` is disabled deliberately and re-enabling it
is a separate decision with its own reason, not a side effect of this work.

## Verification

- Criteria 1, 2, 8: playthrough. From `MainMenu`, Continue into a save with salvage,
  buy each Primary at the ARMORY across three Runs, press `1` each time and read the
  HUD name. Check `Logs/gkb_latest.log` for the equip lines.
- Criteria 3, 5, 6, 7: playthrough with the log open. Fire each ability at a live
  gatekeeper and read the ability log lines. Criterion 6's distance and criterion 7's
  duration are read from the log, not judged by eye.
- Criterion 4: eyes-on, and it cannot be closed before `M82`. Named here as untested
  until then rather than assumed.
- Criterion 9: `git diff` on `Sim/Meta/WeaponCatalog.cs` shows no numeric change.

**Playthrough path**: Play, Continue, ARMORY, buy BR-12, embark, press `1`, fire, fire
the ability at a Default gatekeeper and watch it move. Repeat for MR-35 and PRI-C.
Three Runs, one weapon each.

## Usable, or it did not ship

**Driven**: buying at the ARMORY, pressing `1`, firing, and each ability's input.
**Shown**: three visibly different weapons in hand with different motion. **Reads
back**: the HUD weapon name, the armed-railshot indicator, and one log line per
ability carrying its number.

## Do not build

A fourth Primary. Any stat retune. Arms meshes. Per-weapon bespoke clips beyond
`M82`'s shared set. Hit confirm, which is `M66`.
