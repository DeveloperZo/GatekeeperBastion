# M92 — A bigger ship with three hatches

<!--
Status: active
Update trigger: when the hatch count or the seat-count assignment changes, when a
station moves, when the co-op demo's beat 0 is re-specced
Last updated: 2026-08-20
-->

The Ship is rebuilt larger on a Normandy SR-2 profile and doubles as the safehouse.
Three hatches sit along one side. **One opens per seat, assigned by count**: one seat
opens the middle hatch, two seats open hatches 1 and 3, three seats open all three.

**The expanded hull is authored in `Assets/Scenes/Ship 1.unity`** (decision
2026-08-20): `Ship.unity` stays the shipped hub, byte-untouched, while the new layout
is walked and iterated in its own scene. Swapping `Ship 1` in as the boot hub is a
deliberate later step with its own stamp, not a side effect of this milestone.

**This milestone is the hull and the hatch doors. It is not the fight.** Multi-hatch
damage routing, per-hatch HP and enemy target selection are the co-op demo's work and
are named under *Unblocks*, not built here. What ships here is a bigger walkable Ship
with three real doors on it, correct open state for the seat count, and the four
existing stations still working.

## Story

As a Boarder arriving on a friend's ship, I want to walk a hull that visibly has a
door for each of us, so that I understand what I am responsible for before we embark.

## Why it matters

**The current Ship cannot express the co-op rule.** `Assets/Scenes/Ship.unity` is a
single deck with `Stations` (`Station_Map`, `Station_Armory`, `Station_CrewQuarters`,
`Station_Fabrication`) and `ShipDress` holding `CrewPort` and `CrewStarboard`, each
with a `Bed`. There is no hatch on it at all. The co-op demo rests on hatch doors being
things you can see, walk to and be assigned, and none of that has a home in the tree.

**The perspective question is settled, so the layout is safe to build.** `M89` was
cancelled 2026-08-20 (its doc sits in `5. cancelled/`) and first person is chosen
rather than inherited — the ruling is recorded in `../../Demo - Coop/README.md`;
`Decisions.md` does not carry the entry yet and owes one. A larger layout no longer
risks being re-authored under a different camera, which is why this milestone is
startable now and was not before.

**Assignment by count is what authors the separation.** Two seats are placed at
hatches 1 and 3, the far ends of the hull, by the rule rather than by a player's
choice. If seats claimed hatches instead, a party could claim adjacent ones and the
fight the co-op demo is built to create would not happen.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation** — this adds a new
structure the later co-op slices build on, rather than changing a parameter.

## Acceptance criteria

1. Loading `Ship 1.unity` from Play puts the Boarder on a walkable multi-room hull
   that is larger than `Ship.unity`'s layout, with no hole, seam or unreachable
   station.
2. All four stations open and operate exactly as they did before this milestone:
   `Station_Map` embarks, `Station_Armory` opens the wheel, `Station_CrewQuarters`
   heals and opens the sheet, `Station_Fabrication` reports unpowered or opens per its
   current state.
3. Three hatch doors exist along one side of the hull, each with a stable id, each
   reachable on foot, and each visible from at least one other hatch's position.
4. In a solo run, the **middle** hatch is open and the other two read as sealed.
5. In a two-seat run, hatches **1 and 3** are open and the middle reads as sealed.
6. In a three-seat run, all three are open.
7. Looking at a hatch shows its state: `HATCH <id> — yours`, `HATCH <id> — SEAT <n>`,
   or `HATCH <id> — sealed`. No interact verb on any of them. (Player names exist
   nowhere in the tree — `NetPlayerAvatar` does not replicate one; a name label is the
   co-op demo's M-C work. The seat number is what is replicated today.)
8. Open state recomputes when a seat joins or leaves before embark, and the log line
   in criterion 12 fires again with the new count.
9. A guest walking the host's ship sees the same open hatches the host sees.
10. `Ship.unity` remains present in Build Settings, enabled, and **byte-identical to
    its committed state** — the expanded hull lives in `Ship 1.unity` and this
    milestone never edits the shipped hub. (`Bastion_Coop.unity` does not exist —
    `M95` creates it, registers it, and must also amend
    `SceneBuilder.RegisterBuildScenes`, which hard-overwrites the scene list to
    exactly four entries on every Build All. An earlier draft of this criterion
    required the entry here, before the scene existed.)
11. The Boarder spawn point places the player where at least one hatch is in view
    without walking.
12. `[Hatch] assigned slot=<n> hatch=<id> seats=<n>` is written once per seat on
    assignment, always-on.

## Implementation

1. **Author** `Assets/Scenes/Ship 1.unity` as a kit-built ship — a copy of
   `Ship.unity`'s content (props, stations, teleporter, rig, UI) whose hull is
   assembled from the Synty PolygonSciFiSpace pieces the ship already uses,
   following the ship's own convention: **invisible graybox carries all collision;
   the kit shell has its per-instance prefab colliders disabled**. 57-cell
   Normandy-profile single deck — `SM_Bld_Bridge_*` glass-canopy module at the bow
   (node (10,0,50), rear door on the spine axis), 1-wide neck, 3-wide CIC (Map
   station unmoved), central spine with corridor arches and ceiling pipe runs,
   port crew quarters/mess/wing (stations and furniture unmoved), starboard
   armory/teleporter bay, aft engineering (Fabrication unmoved) and an engine room
   with a drive-core column, and the starboard gallery whose three hatches are
   `SM_Bld_Wall_Escpod_Hatch_01` airlock pieces with `SM_Sign_AirLock_01` over
   them. `ShipHatchDoor` tints each airlock's door-leaf renderer: open = the
   safety-orange hatch material, sealed = the kit's own atlas material. Doorway
   collider stubs flank each `SM_Bld_Wall_Doorframe_01` opening. Only the two helm
   chairs moved (into the canopy bridge). `Ship.unity` is never edited.
   The new occupancy grid is a **superset** of the old one, so `Stations`,
   `ShipDress` and the M91 teleporter keep their positions and bindings untouched
   while the hull grows around them — a bow cockpit, stern engineering, and a
   full-length starboard gallery carrying the hatches, on the SR-2 profile, loosely
   and simply. Hull renderers are enabled across the rebuild — the doors and the
   hull they sit on are Shown. The middle hatch sits on the spawn row, and the
   Player spawn faces it. Single deck. No elevator, no second floor.
   **Coordinate with `M91`**: its scene contract has Zo hand-authoring a teleporter
   subtree into `Ship.unity`; `Ship 1` inherits whatever was committed at its copy
   date (the teleporter half is in), and a re-copy after M91 closes picks up the
   rest.
2. **Create** `Assets/Scripts/Sim/Ship/ShipHatches.cs`. Pure Sim. Holds three hatch
   slots with stable ids and an open flag each, plus
   `static int[] OpenFor(int seatCount)` returning the middle index for 1, the outer
   two for 2, and all three for 3. No engine types (R7).
3. **Change** `Assets/Scripts/Sim/Ship/ShipSystems.cs` to own a `ShipHatches` instance
   alongside the existing `Subsystem Hatch`. **Do not** split `Hatch` into three here.
   The single subsystem stays the damage target for now; per-hatch HP is the co-op
   demo's milestone and doing it here would couple two changes that fail separately.
4. **Create** `Assets/Scripts/Game/Ship/ShipHatchDoor.cs`. `REPLICATED-STATE` header
   (R5). One per door in the scene, carrying its id, driving open and sealed visuals,
   and supplying the look-at string. Reads state; never writes it (R2).
5. **Create** `Assets/Scripts/Game/Ship/ShipHatchAssignment.cs`: one static command
   method that assigns hatches from the current seat count and writes the log line.
   Not `GameController` — an earlier draft named it, but `GameController` is
   Bastion-only (`NetSession._game` is null on Ship) and solo never creates a
   `NetSession` at all. Called from `ShipController` on Ship load (solo and host),
   from `NetSession.OnClientConnected` / `OnClientDisconnect` on the host (party
   change), and from `NetPartyLobby`'s embark commit (before embark). Host-only
   writes; guests only read (R1, R3). Seats map in ascending slot order onto
   `OpenFor`'s ascending indices: at two seats, slot 0 takes hatch 1 and slot 1 takes
   hatch 3.
6. **Change** `Assets/Scripts/Game/Net/NetRunMirror.cs` to carry the assignment as
   one packed-int `NetworkVariable` (3 open bits + an owning slot per hatch), written
   by an explicit host-side method, so a guest's doors read correctly. Not the
   mirror's `LateUpdate` — that path is gated on the Bastion `GameController`, which
   is null on Ship. No new NetworkBehaviour (R8).
7. *(struck — `Bastion_Coop` does not exist yet; registering it is `M95`'s step.
   `Ship_Isometric` was deleted outright on 2026-08-20 (Zo) — scene, PlayMode fixture
   and Build Settings row — so there is no longer a row to keep disabled here.)*
8. **Change** `RelayJoin.MaxGuestConnections` from 3 to 2, so four seats cannot exist
   against three hatches. One constant.

Rejected: authoring the hatches as a prefab array spawned at runtime. Scenes are
source in this project (`M35`), and doors placed by hand are what the layout pass is
for.

## Verification

- Criteria 1, 2, 3, 11: playthrough. Open `Ship 1.unity` in the editor and press Play
  (it is not in Build Settings; the hub `Ship.unity` still boots the game), walk the
  hull, open all four stations, walk to each of the three hatches. Read
  `Logs/gkb_latest.log` for station-open lines and the absence of NullReference
  entries.
- Criteria 4, 5, 6, 8, 12: EditMode test on `ShipHatches.OpenFor`, asserting the exact
  index sets for 1, 2 and 3. Then a PlayMode net test against `NetcodeIntegrationTest`
  that starts a host, asserts the middle hatch open, connects one guest, and asserts
  hatches 1 and 3 open and the middle sealed, with two `[Hatch] assigned` lines at
  `seats=2`.
- Criteria 7, 9: eyes-on, two editor instances. The fixture cannot see look-at strings
  or MPPM, per `../../Brainstorms/co-op.md`. Named here as an untested surface rather
  than claimed.
- Criterion 10: read `ProjectSettings/EditorBuildSettings.asset` and confirm
  `Assets/Scenes/Ship.unity` present with `enabled: 1`, and `git status` showing
  `Ship.unity` unmodified.

**Playthrough path**: open `Ship 1.unity`, press Play, look at the middle hatch and
read `HATCH 2 — yours`, look at either outer hatch and read `HATCH 1 — sealed`. The
two-seat readout (outer pair owned, middle sealed) needs the party flow, which boots
through `Ship.unity` until the hub swap — until then it is covered by the net fixture
test and the two-editor eyes-on.

## Usable, or it did not ship

**Driven**: the player walks to a hatch and looks at it. **Shown**: three doors on the
hull, two of them visibly sealed in a solo run. **Reads back**: the look-at string
naming the owner, and the `[Hatch] assigned` log line.

## Unblocks

Per-hatch HP and the split pool, enemy hatch target selection, and beat 0 of the co-op
demo. All three are named in `Docs/Demo - Coop/README.md` and none are built here.

## Do not build

New stations. A second deck or an elevator. Trophy or display objects. Per-hatch HP.
Enemy routing. Hatch claiming or swapping by the player. Any change to the four
stations' behaviour.
