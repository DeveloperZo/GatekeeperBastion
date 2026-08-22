# M99 — A range you can fight in

<!--
Status: active — started 2026-08-22 (`M93` freed the slot)
Update trigger: when a weapon, weapon ability, protocol, protocol ability, payload or
Gatekeeper kind is added, or when the Ship door that reaches the range moves
Last updated: 2026-08-22
-->

A range reached through a door on `Ship - CoOp`: any weapon, any protocol, any payload,
granted on the spot and **reusable without waiting**, fired at real Gatekeepers at marked
distances, with no Run in between.

**Recut 2026-08-21 (Zo).** The first cut was a weapon range. The kit is not just weapons —
it is the weapon, its ability, the protocol's crew and personal abilities, the doctrine
written onto a decoy, and the payload. A range that only fires bullets tests a third of
what the player brings. Valorant's practice range is the shape: **everything available,
everything reusable, immediately.**

## Story

As the developer, I want to exercise my whole kit — gun, weapon ability, both protocol
abilities and a payload — against live Gatekeepers without buying anything or embarking,
so that a combat-feel judgement costs a minute instead of three Runs.

## Why it matters

**Combat criteria are currently gated on economy and on cooldowns.** `M93`'s verification
read *"buy each Primary at the ARMORY across three Runs"*. `M94` adds Veil and Forge, which
are `BoarderProtocolId` values that exist in code and cannot be claimed. Every ability in
the game has a real cooldown (`PlayerAbility._cooldown`), so judging feel means waiting
between every attempt, in a Run, having paid for the thing first.

**None of what these ask is a question about salvage or about patience.** Does the pin
hold, does Barrage read as a crew ability, does a Phantom decoy pull differently from a
Stock one — none of that is answered faster by making the developer wait.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No stat moves here.

**Durable tooling, with the multiplier stated.** Not a player-visible change; qualifies
under the tooling exception (`../_meta/_definitions/milestone.md`). It retires `M93`'s
three-Run path outright, and makes `M82`, `M84`, `M85`, `M66` and `M94` cheaper by the same
mechanism — each needs a live Gatekeeper in front of a live kit, and each currently gets
there through a Run.

## The range is its own scene, not a room in the Ship

Zo's preference was to build it into the Ship. **Rejected, for two reasons that are not
about taste.**

**A Gatekeeper cannot think on the Ship.** `EnemyGatekeeper.RunIsOver()` returns true when
`_game` is null — *"no controller: nothing left to fight for"* — and targeting reads
`_game.Sim` and `HatchStation.Instance`. That is why `DemoShipFight.SpawnIntruder` disables
the brain and hand-rolls `ShipIntruder`. `PlayerAbility` pins and shoves `EnemyGatekeeper`
specifically, and `DecoyState.ShouldPull` is read per kind off a live brain, so a range
built on `ShipIntruder` could test neither an ability nor a doctrine.

**PRI-C outranges the Ship.** Its range is 90 m and MR-35's is 60 m
(`../../Design/4-Data.md`); the Ship interior is roughly 40 m across.

So: **`Assets/Scenes/Range.unity`, reached through a door on `Ship - CoOp`** — the
destination hub (Zo, 2026-08-21). The fiction survives: you walk out of your hideout into
your range.

## Acceptance criteria

1. A door in `Ship - CoOp` loads `Range.unity`, and a door in the range returns, with
   `MetaState` unchanged — no salvage earned, spent or lost either way.
2. The range grants **any weapon at any tier**, **any protocol** including ones never
   claimed (`Veil`, `Forge`), **any payload** and **any equipment**, from a console and
   without purchase. Every grant is session-only through the existing dev-taint path
   (`GameSession.NotifyDevAction` / `AdoptDevState`) and **never writes the real save**.
3. **Wheel points are settable per weapon, 0 to max.** `WeaponAbility` magnitudes are
   driven by `dialPoints`, so an ability at 0 and the same ability at full focus are
   different abilities; a range that cannot set the dial cannot judge either.
4. **Cooldowns are bypassable, and the bypass is a toggle.** Default is no cooldown —
   the ability fires as fast as it can be pressed, which is what practice needs. One
   input restores the real cooldown, because *how long it makes you wait* is also a
   thing being judged. The toggle state is on screen.
5. A console spawns a chosen **Gatekeeper kind** at a chosen marker with its brain live:
   it paths, it swings, it can be pinned, shoved and pulled. Spawns are capped and one
   input clears the lane.
6. **Respawn mode**: killed bodies return to their marker at full HP after a stated delay,
   toggleable, so a burst can be repeated without walking back to the console.
7. A lane with **distance markers at 5, 14, 30, 60 and 90 m**, readable from the firing
   line — the range values of BR-12, MR-35 and PRI-C, so falloff is checked by walking.
8. **A payload placed in the range behaves as it does in a Run**: a decoy pulls Gatekeepers
   inside `DecoyState.PullRadius`, and switching protocol switches the doctrine written
   onto it. Refusals by kind (a Thief never pulls) hold here too.
9. A readout shows, for the current kit since the last clear: shots fired, total damage,
   time-to-kill on the last body, and each ability's cooldown state. It reads from existing
   events; **no counters or rollups are added to `Sim/`**
   (`../../Design/3-Mechanics.md`, rejected list).
10. The range never runs a Wave, never touches `RunSim`, and cannot end a Run. Entering it
    while a Run is live is refused, with the reason in the log.
11. `Range.unity` is registered by `SceneBuilder.RegisterBuildScenes` as a non-boot scene
    and survives Build All without displacing the five boot scenes.

## Implementation

**Create** `Assets/Scenes/Range.unity` — a Bastion-shaped scene: `GameController`,
`HatchStation`, a baked NavMesh over the lane, a `PlayerCamera` rig matching the Bastion's,
and the post-processing layer and volume per the 2026-08-20 pass. The lane is a straight
bay of at least 95 m with the five markers as authored geometry.

**Create** `Game/Range/RangeConsole.cs` — grants and spawn control. Grants go through
`AdoptDevState` on a constructed `MetaState`, the same taint `LoadDemoCheckpoint` already
uses, so the range cannot corrupt a real save by construction rather than by care.

**Create** `Game/Range/RangeRules.cs` — the cooldown bypass and respawn mode. **The bypass
zeroes `PlayerAbility`'s cooldown from outside rather than branching inside it**: the
ability path stays the shipped one, so what is being judged is the real mechanism and not
a range-only copy of it. Respawn re-enables the shipped body at its marker; it does not
spawn a new kind of actor.

**Create** `Game/Range/RangeReadout.cs` — subscribes to damage and ability events that
already exist, draws through `Game/Ui/`, never `OnGUI` (`../../Architecture.md` gotcha 3).

**Change** `Game/Flow/SceneFlow.cs` — a `Range` constant and `LoadRange()` beside
`LoadShip()`, same shape.

**Change** `Ship - CoOp` — one door interactable. Single subtree
(`../../Architecture/8-Scene-Authoring.md`); `Ship - Demo` gets nothing.

**Do not build**: a second enemy type for the range, a range-only weapon or ability path,
a range-only protocol, or any `ShipIntruder` extension. The range exercises the shipped kit
against the shipped Gatekeeper or it is not measuring the game.

## Verification

- Criteria 1, 10: enter and leave, reload the Ship, diff `gatekeeper_meta.json` —
  byte-identical. Attempt entry mid-Run and read the refusal line.
- Criterion 2: grant PRI-C at tier 4 and the `Veil` protocol, return, reload, confirm the
  loadout and the claimed-protocol list are what they were. The Dev-taint banner is up
  throughout.
- Criterion 3: fire `Mr35Pin` at 0 dial points, then at max, and read the `pin held`
  duration difference off the log.
- Criterion 4: press the weapon ability five times in five seconds with bypass on; toggle
  off and confirm the second press is refused until `CooldownRemaining` reaches 0.
- Criteria 5, 6, 8: spawn a Default at 30 m, pin it, shove it, read `concussive shove`
  metres from the log; place a decoy and confirm it pulls; switch protocol and confirm the
  doctrine on the next decoy changes; spawn a Thief and confirm it never pulls. Kill one
  body and watch it return with respawn on. **This is `M93` criteria 6 and 7 with no Run.**
- Criterion 7: fire BR-12 at 5 m and 30 m and read the damage difference.
- Criterion 9: kill one body, read TTK; clear, swap the whole kit, repeat.
- Criterion 11: `Tools/verify/run.sh` — Build All keeps the boot five in order and keeps
  `Range.unity` after them.

**Playthrough path**: Play, Continue, walk to the range door on `Ship - CoOp`, grant BR-12
plus Default protocol, spawn a Default at 14 m, empty a clip, fire the weapon ability, fire
Barrage and Rush, place a decoy, read the readout, clear, swap to PRI-C and Veil, repeat.

## Usable, or it did not ship

**Driven** — a door, then a console with weapon, protocol, payload and spawn rows, plus the
bypass and respawn toggles. **Shown** — the door is signed, the markers are painted on the
lane, and the toggle states are on screen. **Reads back** — the readout, and the same log
lines the Bastion writes.

## Depends on / Unblocks

Depends: nothing. The Gatekeeper, the weapons, the abilities, the protocols and the decoy
are all shipped or defined.
Unblocks: `M93` (retires the three-Run path), and cheapens `M82`, `M84`, `M85`, `M66` and
`M94` — `M94`'s two protocols can be developed against something before they are claimable.

## Trap

**Do not let a grant touch the real save.** The failure is silent and expensive: a tier or
a protocol granted for a test that survives into a played save makes every later economy
and progression observation wrong, and nothing in the game would report it. `AdoptDevState`
exists for this and is the only sanctioned path.

**Do not branch the ability path on "am I in the range".** A bypass that lives inside
`PlayerAbility` means the thing being judged is no longer the thing that ships. Zero the
cooldown from outside, or the range measures a copy.
