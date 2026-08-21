# M93 — Three primaries that read as three guns

<!--
Status: next
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

**Shown** (criterion 4) depends on `M82` (one shared viewmodel animator, four clips,
one controller). The other criteria do not wait on it. `M66` (hit confirm and audio
tells) sharpens this further and is **not** a dependency; criteria here are met
without it.

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
8. Swapping the equipped Primary loses no wheel points on the parked one. Proven by
   EditMode over `MetaState`, plus one swap seen in the range (`M99`) — **not by
   consecutive Runs**, which test the economy rather than the weapon.
9. Nothing in this milestone changes a damage, rate, clip, reload, range, pellet,
   spread, pierce or falloff value in `WeaponCatalog.cs`.

## Implementation

Catalog, shop, slot `1`, abilities, pin hold, and per-weapon upgrade storage were
already in the tree (`M11`, `M20`, `M31`, `M32`). This milestone does not rebuild
them. Remaining edits:

1. **Do not land** `_m61_2_handoff/`. That tree is `M61-2` bastion mesh (completed
   2026-08-15). Nothing in it applies here. Discarded 2026-08-20 on review.
2. **Do not change** `ArmoryStation.cs` or `DemoProgress.PrimaryRowsOpen`. The rack
   opens on `DemoZone2BankLost` or a demo-off save; `SetLoadout` is the dev path.
   Criterion 1's playthrough uses a save where the rack is already open.
3. **Change** `PlayerAbility.ActivateConcussive` and the matching host log in
   `NetVerbs.ApplyConcussive` so the `concussive shove` line carries metres.
   `Shove` returns walk-back seconds (`moved / GatekeeperPatrolSpeed`); metres are
   that product. Pin and railshot lines already carry their numbers. Do not rewrite
   the abilities.
4. **Change** `HudController.Weapon.PushAbilityReadout` so a live Railshot window
   paints **ARMED** before the cooldown line hides it. Cooldown starts on arm, so
   the cooldown branch was winning.
5. **Create** `Assets/Tests/EditMode/Meta/PrimaryDifferentiationTests.cs` — the
   three shop Primaries pairwise differ on at least three combat fields.
6. Criterion 4 waits on `M82`'s clips. Those landed 2026-08-20 — one shared
   controller, fire kick and reload speed-scaled per weapon. This milestone
   does not grow a second controller. Eyes-on still required.

Rejected: adding a fourth Primary. `VK-7` is disabled deliberately and re-enabling it
is a separate decision with its own reason, not a side effect of this work.

## Verification

- Criterion 1: playthrough on a save where `PrimaryRowsOpen` is already true
  (`DemoZone2BankLost` or demo-off). ARMORY Primary tab lists BR-12, MR-35, PRI-C.
- Criteria 2, 8: in the range (`M99`), grant each Primary, press `1`, read the HUD
  name, and check `Logs/gkb_latest.log` for the equip lines. Criterion 8's wheel-point
  survival is EditMode over `MetaState`; the range confirms the equip half. **No Run and
  no purchase** — neither is what these criteria are about.
- Criteria 3, 5, 6, 7: in the range, with the log open. Spawn a Default at a marker
  and fire each ability at it. Criterion 6 reads `concussive shove` metres off the log.
  Criterion 7 reads `pin held` duration; the body stays put for that many seconds. The
  range spawns the shipped Gatekeeper with its brain live, which is why these are
  answerable there at all (`M99`).
- Criterion 4: eyes-on in a Run. `M82` clips are in; fire kick and reload
  motion should read at each Primary's rate. Unchanged build would already
  fire `NotifyFired` / `NotifyReload` — the clips are what made that visible.
- Criterion 5: HUD shows `ARMED` on Railshot for the armed window, not only the
  cooldown.
- Criterion 9: `git diff` on `Sim/Meta/WeaponCatalog.cs` shows no numeric change.
- Differentiation test: `Tools/verify/run.sh --filter PrimaryDifferentiation`.

**Playthrough path**: Play, Continue, walk to the range door, grant BR-12, spawn a
Default at 14 m, fire, fire the ability and watch it move. Clear, grant MR-35, repeat.
One session, three weapons.

**Depends on `M99`** for every criterion but 1 and 9. Criterion 1 is an ARMORY listing
and needs a save, not a Run; criterion 9 is a `git diff`.

## Usable, or it did not ship

**Driven**: buying at the ARMORY, pressing `1`, firing, and each ability's input
(`F`). **Shown**: three visibly different weapons in hand with different motion —
`M82`, criterion 4. **Reads back**: the HUD weapon name, the armed-railshot
indicator, and one log line per ability carrying its number.

## Do not build

A fourth Primary. Any stat retune. Arms meshes. Per-weapon bespoke clips beyond
`M82`'s shared set. Hit confirm, which is `M66`. Opening the Primary rack before
zone 2's bank falls.
