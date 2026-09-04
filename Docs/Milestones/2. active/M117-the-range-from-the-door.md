# M117 — The range, from the door

<!--
Status: active
Update trigger: when the door, the loadout panel, the range interactions or the wave terminal change, or when the play stamp lands
Last updated: 2026-09-03
-->

The practice range becomes something a player reaches, kits, and runs waves in, without a
dev console. A door beside the ARMORY opens a loadout panel; confirming it loads the range
with that kit; targets, toggles and waves are things you walk to and press.

## Story

As a Boarder, I want to walk through a door beside the ARMORY, pick a protocol and two
weapons, and try them against standing targets and real waves I start myself, so that
learning a kit happens on the range and not on a Boarding I paid for.

## Why it matters

`M99` built a range nothing reaches: the way in is "Coming Soon" (`The-Range.md`), the
targets and toggles are dev-console digits compiled out of shipped builds, and the range
"never draws a Wave" (`3-Mechanics.md`). The 2026-09-02 tuning day showed the cost: every
weapon and wave question was answered on live Boardings, and the range kit is "everything
unlocked at full focus", which answers questions about a kit nobody owns.

Zo, 2026-09-02: "Gate it by a UI selection for Protocol and weapon (weapons will have stats
player has unlocked, for unlocked weapons player can use base level). We will need to move
dev commands to interactions in range. we should add spawn rooms and terminal to press and
adjust waves."

Zo, 2026-09-03, on the first cut's posts, console and far terminal: "The terminal range side
is horrible. I shouldnt have to cycle through permutations give me a key for each dimension
or something refined. I also shouldnt have to walk to generate anything. Its a shooting
range. I need ability to go from spawn to firing quickly." So criteria 3 and 4 were rewritten
that day: the range's verbs are keys live anywhere on the lane, one per dimension, with the
board's state printed on the readout; nothing on the lane is walked to.

Zo, 2026-09-03, on the second cut's six keys: "Dimensions: Kind - Z / Distance - default to
20 m / Wave Size - X, small, normal, large / Draw Wave - T / Cooldown - no bypass. reset them
on wave start." So the board is two keys and the draw, the breach is 20 m from the firing
line, the closets are gone, and the bypass with them.

Leverage: **#6 information flows** (the panel and the readout tell the player what a kit
does before a Boarding), **#5 rules** (what the range hands out, and that it draws waves).
Higher tiers are wrong because the range serves the loop, it is not one; lower tiers are
wrong because no constant makes a dev digit a door.

## Acceptance criteria

1. **The door.** `Ship - CoOp` carries one way-in door, authored by `SceneBuilder.RangeDoors`
   on a wall of the ARMORY's bay within 5 m of the station and clear of its panel; the
   FABRICATION coordinate is retired. `E` opens the loadout panel (stations open on the press,
   like the ARMORY); nothing loads until it is confirmed. The return door in `Range.unity`
   stands proud of the near wall, not inside it, and survives a Range Scene rebuild.
2. **The loadout panel.** A station panel (the Ship's `StationTerminal` / `StationPanelView`
   grammar) with three rows: Protocol (any of DEPOT, VEIL, FORGE), Primary, Secondary. A
   weapon the realm owns is offered with the wheel points it owns; a weapon it does not own
   is offered at base, marked so. Confirm loads the range with exactly that kit
   (`RangeConsole` adopts a `RangeLoadout` instead of `UnlockEverything`); the session is
   tainted and never saved, as today. Cancel closes the panel and loads nothing.
   `SceneFlow.LoadRange` keeps its live-Run refusal.
3. **The board replaces the dev digits.** One `RangeBoard` on the range root, live
   anywhere on the lane — no surface is looked at or walked to. The dimensions are Zo's
   (2026-09-03): `Z` target kind (Default, Hunter, Thief, Cloaker), `X` wave size (small 1,
   normal 11, large 25 — `PracticeWave`), `T` draws; the breach is fixed at
   `PracticeWave.SpawnMetres` (20 m) from the firing line. Bound in `Controls` under a
   `Range` context that shares no key with anything live in a fight. The readout prints the
   board — every key beside its current value, or the standing count while a wave is live —
   so nothing is memorised. No cooldown bypass: the timers are reset once, from outside, as a
   wave is drawn. The `DevConsole` range rows are gone; the dev console keeps its general
   rows. Every action logs.
4. **The practice wave.** `T` draws `X` bodies of `Z` through the real `WaveDirector`,
   `WaveCadence` and `WaveSpawner` — wave 1's group width, mean gap and health multiplier,
   the roster rewritten to one kind at the size — breaching at the 20 m marker and nowhere
   else, refused (and said so) while one is live. It pays nothing, banks nothing, holds no
   Assault open past its last body. The Hatch is switched off. The player stands on the
   firing line on the first frame.
5. **The record.** `3-Mechanics.md`'s "never drawing a Wave" and `The-Range.md`'s "Coming
   Soon" are rewritten to what shipped; `Decisions.md` records the reversal and the door.

## Kill criterion

None; a defect in reach is not a bet. The range exists (`M99`); this is the way in.

## Pre-mortem

- The loadout panel needs authored UI, and nothing here can drive the Editor. Mitigation:
  the panel is the shared `StationPanelView` the ARMORY already uses, authored by the same
  builder path (`SceneBuilder.Ship` / `ControlPanels`) under the range-door sentinel, so Zo
  runs one sentinel and the door and its panel land together.
- A range wave with the Hatch in the copy could be lost to `cause=hatch`. The copy's Hatch is
  excluded from targeting, or removed by `StripBastionRooms` — whichever the scene already
  does; the criterion pins it either way.
- "Owned weapons at owned points" reads a realm's wheel into a tainted session and the taint
  banner makes it look like a save was touched. The three write paths are already logged
  no-ops under taint; the panel says "practice — nothing is saved" on its confirm row.

## Implementation

**Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.RangeDoors.cs` — the ship door moves
to the ARMORY's bay and becomes a `RangeDoorStation` with a `StationPanelView`; the return
door is placed off the measured floor, not on the wall plane.
**Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` — copies from
`Bastion_Coop.unity` (`Bastion.unity` retired in `3d674ab` and the build threw on it), authors
the return door itself, switches the copy's portals off beside its Hatch.
**Create** `Assets/Scripts/Sim/Range/RangeLoadout.cs` — the kit as data: protocol, primary,
secondary, and per-weapon points (owned or base); `From(MetaState, choices)`.
**Create** `Assets/Scripts/Game/Ship/RangeDoorStation.cs` — the panel; confirm calls
`SceneFlow.LoadRange` with the loadout pending on `GameSession`.
**Change** `Assets/Scripts/Game/Range/RangeConsole.cs` — adopt the pending loadout;
`SpawnAt` / `ClearAt` by mark; `Marks` read off the authored markers.
**Create** `Assets/Scripts/Game/Range/RangeBoard.cs` — the two keys, the draw, the line the
readout prints. (2026-09-02's `RangeMarkPost`, `RangeControlConsole` and `RangeWaveTerminal`
were built and deleted the next day on Zo's play; the six-key board lasted one message.)
**Create** `Assets/Scripts/Sim/Range/PracticeWave.cs` — `PracticeSize`, the three counts,
`SpawnMetres`. **Change** `RunSim.Practice.cs` — `DrawPracticeWave(kind, size)`.
**Change** `Assets/Scripts/Game/Input/Controls.cs`, `PlayerInputFrame.cs` — `RangeKind` on
`Z`, `RangeSize` on `X`, a `Range` context, off the shortcut panel (the readout teaches them).
**Change** `Assets/Scripts/Game/Range/RangeRules.cs` — the bypass deleted; `ResetCooldowns`.
**Change** `Assets/Scripts/Game/Range/RangeReadout.cs` — prints the board.
**Change** `Assets/Scripts/Editor/Scenes/SceneBuilder.Range.cs` — one breach marker at 20 m
on the spawner's list instead of two closets; the board on the range root; the marks stay as
posts to read distance by.
**Change** `Assets/Scripts/Game/Dev/DevConsole.Range.cs` — delete the range digit rows.
**Change** `Docs/Play Mechanics/The-Range.md`, `Docs/Design/3-Mechanics.md`,
`Docs/Decisions.md`, `Docs/Architecture/README.md` (`menu_rows` only if a row is added — none
is planned; the sentinel drives the builders).

## Verification

1. Criteria 1–2: `RangeLoadoutTests` (owned points carried, unowned at base, protocol any of
   three); source-shape pin that `SceneBuilder.RangeDoors` places the door on the ARMORY's
   wall; the builder's log line after Zo runs the sentinel.
2. Criterion 3: `ControlsTests` — the two range keys exist, sit in the `Range` context
   alone, and share no key with any row live in a fight (`FightAlways`), since the fight
   keys stay live on the lane; the log lines `range wave — 11 Hunter (normal) drawn at 20 m`
   and `cooldowns reset — wave start` from one range visit.
3. Criterion 4: `PracticeWaveTests` — the sizes are 1 / wave 1's roster / wave 3's roster;
   a draw is wave 1's beat with the roster one kind at the size, no debut gate, attaches a
   `WaveCadence` schedule, pays zero (`CurrentKillAward` 0), is refused off practice and
   while one is live, and returns to Prep with nothing owed; the log's `group k … breached
   at` lines from one range wave.
4. Playthrough path — from the Ship: hold `E` at the door beside the ARMORY, pick FORGE +
   MR-35 + MK-1, confirm; on the firing line press `Z` to Hunter, `X` to large, `T`; the wave
   breaches 20 m out with every cooldown at zero; return through the door behind you.

## Do not build

- A Salvage payout, a Hatch stake, or a save write from the range.
- A second door, or a door that loads without the panel.
- Dev-only digits kept "just in case".
- A verb you walk to. A post, a console or a terminal on the lane is the 2026-09-02 cut Zo
  rejected; the range is a shooting range, and its verbs are keys.
- A cooldown bypass, a distance dial, a wave or shape picker, or a standing-target ledger on
  the board. Two keys and the draw (Zo, 2026-09-03); anything more is the six-key cut back.

## Depends on / Unblocks

Depends on: `M116` (the wave terminal draws its shapes and beat). Unblocks: tuning
questions answered on the range rather than on Boardings; `M112`'s turret criterion has a
room to run in.
