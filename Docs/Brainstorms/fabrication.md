# Fabrication

<!--
Purpose: The FABRICATION console — the drone and its payloads, equipment, and the ship-systems shelf.
Status: brainstorm
Update trigger: when a payload row changes state, a drone control question in Open is answered, or the Hatch loss model changes.
Last updated: 2026-08-08
-->

## What this covers

The FABRICATION console: **the drone** and the payloads it is outfitted with,
**equipment** — gear you equip that makes a capability possible at all — and the
ship-systems shelf (currently Hatch Armor). **Boundary with the ARMORY, restated
2026-08-08:** the ARMORY sells what is in your hands — guns, their Tiers, rings and
abilities. FABRICATION sells the drone and what it carries. The old carried-vs-installed
line is retired with the Utility migration below; the split is by **mechanism**, not by
where the item sits.

## Where it stands

`Game/Ship/EngineeringStation.cs` (labelled FABRICATION in the UI) sells Hatch Armor,
equipment, and the decoy payload (`M30`). **The drone bay and decoy shipped**; further
payload rows in the roster below remain design.

The Hatch still dies to `arrivals x dwell` (`../Design/3-Mechanics.md`); one level of Hatch
Armor is +15 HP = +4.4 gatekeeper-seconds against a measured ~120 gatekeeper-second deficit at
Wave 2, which is why the station needed this file.

## Graduation test

**The drone's control loop is stated — how a payload is aimed, spent and resupplied —
and the first payload names the failure it answers with a number:** *N gatekeepers parked
for M seconds is what kills the door, and this reduces N or M by X.* The drone is a
delivery mechanism; the test lands on each payload.

## Decided

### FABRICATION is the drone (Zo, 2026-08-08)

The station is centered on a **drone the player outfits with one payload per Run**. The
payload gives the player an activated ability — **player-activated, not autonomous**
(Zo, same day). This absorbs the carried-Utility item list from
[`armory-and-weapons`](armory-and-weapons.md) **wholesale**: the intent of every item is
unchanged, the mechanism is recentered on the drone. Zo's four named payload types:
mine, turret, decoy, ammo.

The Utility class contract survives as the payload contract: **finite supply, may hit
very hard, rescues a moment, cannot carry a fight.** Payload choice is made at the ship,
which gives embark a loadout decision the NAV MAP currently lacks.

### The third slot becomes the drone bay (Zo, 2026-08-08)

`WeaponSlot.Utility` (still `Melee` in code) does not die — it **is** the drone: same
slot, the equipped payload shown in it. ~~The JSON key `"EquippedMelee"` still must never~~
**WITHDRAWN 2026-08-08 (Zo):** the slot is `WeaponSlot.Drone`, the key is `"EquippedDrone"`,
and the save is v5 with no compatibility shim. The original line continues below as a record.
The JSON key `"EquippedMelee"` still must never
change (armory correction, 2026-08-07). `M23`'s slot-cost premise translates cleanly —
the rig is a payload, and carrying repair costs the bay something else wanted — but the
milestone is written against a *carried* rig and **needs re-cutting before it is built**.
One payload per Run makes it a harder gate than M23 wrote: a player without the repair
payload cannot repair at any price, and the rigless case still has no numbers
(`../Design/2-Dynamics.md`) — the first thing a logged Run after the re-cut should report.

*(Superseded in part later the same day: the rig is equipment, not the bay's payload — see
the Equipment decision below. The slot decision itself stands.)*

### The ammo payload is a preloaded reserve (Zo, 2026-08-08)

Loaded during Prep with rounds bought at the normal Salvage price; the drone feeds them
to the Primary mid-fight. **It moves ammo, it does not create it** — O5's drop model and
the ammo-versus-repair bidding war stand, and the dry-Primary state the Secondary class
is built on survives because the reserve is finite and bought at the same price as any
other ammo. What the payload buys is timing: the resupply reaches you during Assault
instead of after it.

### Placement is ghost-and-confirm (Zo, 2026-08-08)

Activation is **contextual per payload**. A payload placed in the world — the turret, the
mine, the ammo box — shows a **placement ghost and deploys on confirm**, the standard
pattern. Placed objects expire by supply, not by timer: the turret dies when its ammo is
spent; the ammo box disappears when drained. A payload that is not a placement states its
own activation on its row — the same per-item reading the Utility spokes used.

### Per player in co-op (Zo, 2026-08-08)

Each player has their own drone and their own payload choice. The bay is part of the
player's loadout; sim ownership is per player from the first payload coded (R-rules).

### Payload upgrades wait (Zo, 2026-08-08)

Neither M21 spokes nor FABRICATION tracks until a payload has been played. The shelf
sells payloads flat; whether payload upgrades exist at all is play data's question.

### Equipment is the second category (Zo, 2026-08-08, superseding rig-as-payload from earlier the same day)

FABRICATION expands to **Drone and Equipment**. Equipment is equipped gear that makes a
capability possible at all; without it the capability does not exist. **The repair rig is
equipment, not a payload** — it leaves the payload roster, repair stays impossible without
it equipped, and the bought decision stands. **The rig is upgradeable; the upgrade shape is
TBD** (Open — O1's flat-rate rule is superseded).

**Closed later the same day (Zo):** equipment gets its **own slot**, and the slot itself is
**unlocked through progression and bought with Salvage** — the tool arrives after the
problem twice over. And **M23 does not build the drone bay**: it builds the equipment slot
and the repair gate only; the bay ships with the first real payload's milestone.

## Open — blocked until decided

Nothing in this table may be written into a milestone until it is answered
(`../_meta/_definitions/brainstorm.md`).

| Question | Why it blocks | Owner | Recommendation |
|---|---|---|---|
| The rig's upgrade shape (declared TBD) | Nothing — recorded so nobody designs it by accident | Zo | Leave undesigned until the rig is played |

*Closed 2026-08-08 (Zo): the equipment slot unlocks at the first lost Run —
`PodEjections >= 1`, the same counter that unlocks Tiers. Recorded in `M23`, re-cut the
same day and buildable.*

Everything decided earlier today stands: bay = third slot, ghost-and-confirm placement,
per-player drones, the preloaded ammo box, payload upgrades wait. Per-payload supply
numbers still live in each payload's milestone.

## Graduated — these left this file

Link, never restate. If a detail below matters, it lives in the milestone.

| What | Milestone |
|---|---|
| The drone bay, the ghost-placement pattern, the charge model, and the decoy | `M30 — Something to pull them off the door` |

## The payload roster

**One table; `state` is the only place a row's status lives.** Codes, states and
refusal grammar carried over from `armory-and-weapons.md` (migrated 2026-08-08).

| Code | Refuses | Identity in one line | State |
|---|---|---|---|
| UTL-C | subtlety | Opens the hull; drags everything loose toward the hole and out. | picked |
| UTL-D | letting them walk | Welds one Gatekeeper where it stands; others path around it. | candidate |
| UTL-E | small targets | Mine; arms only under a crowd, so it pays for letting one form. | candidate |
| UTL-F | killing | Yanks one body off the Hatch. | candidate |
| UTL-G | fighting at all | Preloaded ammo box, placed; gone when drained. | picked (Zo, 2026-08-08 — see *Decided*) |
| UTL-H | downtime | Turret; fires while you reload, walk or repair, and dies when its ammo is spent. | candidate |

**Ship order for the picked rows**, if the goal is play data rather than coverage: the
control payload graduated first (`M30`), and `UTL-C` is the damage half that follows — the
smallest set that shows the drone is not a third gun. Everything after those waits for a
Run that says it is missing.

### Notes on the rows

**UTL-C** — the damage half of the pair, and it earns the setting: a mechanic that only
makes sense in space rather than a fireball with a blue tint. *Whether the pull reads as
thrilling or as loss of control is unknown and needs a spike*, and it needs a rule for
what happens to the player standing in it.

**UTL-D** — the player builds walls out of Gatekeepers. Most novel, least predictable:
*whether the NavMesh produces legible avoidance or visible jitter is unknown.*

**UTL-E** — rewards the opposite instinct to every other weapon.
`WaveDirector.MaxGroupSize = 7` already puts real clusters on the floor.

**UTL-H** — the only payload with no prior ARMORY row. The dps caution below applies to
it hardest, but its unique claim is not dps — it is **delivery during the player's dead
time**: the 1.4–2.6 s reload windows, walking, repairing. Worth holding against the
Wave-2 triple in `../Design/2-Dynamics.md`, where raising player dps lifted kill fraction
58→83% *and* arrivals 15→22 in near-cancellation. *Whether a second firing position
moves the outcome where more player dps did not is unknown, needs play data.*

## Equipment

One piece exists. Same codename rule as everything unshipped; a capability gate is
checkable by construction, so the graduation bar is the gate itself. **Equipment lives in
its own slot, and the slot is unlocked through progression and bought with Salvage** (Zo,
2026-08-08). The unlock is the first lost Run; initial prices are `M23`'s (300 slot,
300 rig, *tunable, needs play data*).

| Piece | Enables | State |
|---|---|---|
| repair rig | Repairing the Hatch — impossible without it equipped. Bought (M23); upgradeable, shape TBD. | picked — `M23`, re-cut 2026-08-08, buildable |

## Ideas

### Ship systems with run-level consequences

Subsystems that change a Run — a scanner that marks Ram spawns, a beacon that shifts
breach locations — would make the shelf more than the drone plus Hatch Armor. The
deferred subsystems in `../Decisions.md` (Reactor, TetherCore, Engines, LifeSupport)
are waiting on exactly this outfitting layer.

## Rejected

- **Hatch repair as an active ability** (2026-08-08) — `M23` decided repair is the rig,
  and the standing constraint was that only one of the two exists.
- **An autonomous drone** (Zo, 2026-08-08) — player-activated chosen. A drone acting on
  its own rules is not re-proposed without overturning that.
- **"Something that reduces arrivals" / "something that reduces dwell" as standalone
  shelf items** (2026-08-08) — absorbed by the payload roster; the two terms live in the
  graduation test now.

## Constraints

- **An item that only adds dps is a worse version of a Tier.** Applies to `UTL-H`
  hardest.
- Every payload states which term of `arrivals x dwell` it reduces; the gatekeeper-second
  budget (`../Design/3-Mechanics.md`) is the unit.
- Any ammo payload must not delete the dry-Primary state — the Secondary class's axis
  (`armory-and-weapons.md`, decided 2026-08-07) depends on running dry being real.
