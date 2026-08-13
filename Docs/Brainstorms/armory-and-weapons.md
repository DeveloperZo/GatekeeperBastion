# Armory and weapons

<!--
Purpose: The ARMORY console — the weapon catalog, the gun classes, slots, and what a weapon can be.
Status: brainstorm
Update trigger: when a weapon changes state in the roster, a class contract changes, or a row in Decided/Open moves.
Last updated: 2026-08-12
-->

## What this covers

Everything bought at the ARMORY: the weapon catalog, the gun classes, slots, and
the firing modes a weapon can express. Not player attributes (CREW QUARTERS), and not the
drone or its payloads (FABRICATION).

**Boundary with FABRICATION, restated 2026-08-08 (Zo):** the ARMORY sells what is in your
hands — guns, their Tiers, rings and abilities. The drone, and every former Utility item
now a drone payload, is FABRICATION's ([`fabrication`](fabrication.md)). The
carried-vs-installed line this paragraph used to state is retired with that move.

## Where it stands

Shipped in M11, then M21 (wheel), M26 Path 1 (SEC-D), M32 (PRI-C). `Sim/Meta/WeaponCatalog.cs`
holds the catalog. Shop rows: MK-1 and SEC-D (Secondaries, both cost 0), BR-12, MR-35, PRI-C
(Primaries). VK-7 stays disabled with its id reserved. Tiers are wheel purchases,
`WeaponTier.MaxTier = 10`, cost `400 + 200 * currentTier` (`../Design/4-Data.md`). Firing
modes are data fields on `WeaponDef`. `WeaponSlot` is `Primary / Secondary / Drone`.

**The VK-7 Assault Rifle is retired** (2026-08-06). Its `WeaponId` value stays so old saves
deserialize and the id is never reused. See `../Decisions.md`.

## Graduated — these left this file

Link, never restate. If a detail below matters, it lives in the milestone.

| What | Milestone |
|---|---|
| The activated ability slot, and both Primary abilities | `M20 — The fifth attribute` |
| Choice-driven upgrades, and both Primary rings | `M21 — The upgrade wheel` |
| The repair rig as a carried Utility item | `M23 — The repair rig is a tool you bring` |
| The swap-during-reload setting | `M25`, behind `M24 — An options menu` |
| A Primary that wants the far wall (`PRI-C`, Railshot) | `M32 — A Primary that wants the far wall` |

## Graduation test

**Each class states its job as a number, and one weapon per class hits it** — how many
gatekeepers a Primary must stop per Wave, how much of the reload gap a Secondary must cover.
Until a class can say that, a new weapon is a damage number with a different name and the
catalog grows without the fight changing. (The Utility clause moved with the class to
[`fabrication`](fabrication.md), 2026-08-08.)

## Decided

Settled. Each line is dated and attributed; nothing here is re-litigated without saying so.

### The three classes (Zo, 2026-08-06)

The Hatch dies to two things multiplied together (`../Design/3-Mechanics.md`): **how many gatekeepers
reach it**, and **how long each one stands there**. Two other things a player can spend
against that: **the seconds spent reloading**, and **Salvage**.

**The order matters:** a player starts with a Secondary and earns a Primary. Nobody ever
holds a Primary alone, which is why the Secondary is the weapon that has to work unaided.

| class | what it is for | the rule that keeps it in its lane |
|---|---|---|
| **Secondary** | The weapon you start with. Carries the early game alone; the fallback when the Primary is reloading or empty. | **Never *permanently* dry** — it regenerates its own ammo, so it can be outshot but always comes back. Weaker than a Primary, so earning one still matters. |
| **Primary** | Earned, not given. The heavy damage, and the reason you save up. | It burns scarce ammo. Better *while loaded*, which is what makes feeding it a decision. |
| **Utility** | Thrown or placed items — mines, grenades, decoys, charges. Fixes one bad situation. | Finite supply. **May hit very hard**, but it rescues a moment and cannot carry a fight. |

Utility is the only class whose value depends on *where* it is used — the first decision the
game asks that is about position rather than aim. `../Working-Rules.md` files "a reason to
be somewhere other than the Hatch" as the highest-value unused structure in the game.

**Utility re-homed (Zo, 2026-08-08):** the class contract above is unchanged, but its items
are now **drone payloads bought at FABRICATION** — player-activated, one equipped per Run.
The roster rows and their notes moved to [`fabrication`](fabrication.md); the third slot
itself becomes the **drone bay** (decided there the same day).

### Ammo (Zo, 2026-08-06)

The Secondary generates its own ammo slowly — unlimited in theory, rate-limited in practice.
The Primary is fed by a small trickle from Gatekeepers plus purchase between complete Waves at
a Salvage cost. Ammo is **not a second currency**; it is a second thing to spend Salvage on,
competing with Hatch repair (M19). It also gives Prep a cost, which the roadmap carried as an
open unknown.

Why this shape: it rewards progression and proficiency separately, and the regenerating
Secondary is the floor that stops a struggling player — who burns ammo *and* lets bodies
reach the door — from spiralling.

### Codenames until identity is solid (Zo, 2026-08-06)

Unshipped rows carry `PRI-` / `SEC-` / `UTL-` codes and nothing else. A name given before a
weapon has an identity in play has to be defended later and fixes the fiction before the
mechanic earns one. Shipped weapons keep their names. Take a real name the moment one is
obviously right. Also in `../Decisions.md`.

### Melee is not a slot (Zo, 2026-08-06)

`WeaponSlot.Melee` becomes `WeaponSlot.Utility`. Melee as a weapon was an idea, not a
requirement, and is not carried forward.

**Correction, 2026-08-07:** an earlier version of this line claimed the rename "costs one
enum value no save has ever used." That is false. `SaveData.EquippedMelee`, the JSON key
`"EquippedMelee"`, `MetaState.EquippedMelee`, `ControlId.SlotMelee` and suppression logic in
`PlayerGun` and `HudController` all reference it. The decision stands; the cost estimate was
wrong, and **the JSON key must not change** or old saves lose the slot.

**Note, 2026-08-08 (closed same day):** the third slot becomes the **drone bay** — decided
in [`fabrication`](fabrication.md). Same slot, same JSON key, the equipped payload shown in
it; the rule above stands.

### Two Primaries is the roster (Zo, 2026-08-06)

A close-range shotgun and an all-round rifle. Depth comes from their ability and their ring
(M20/M21), not from more rows. A third Primary was proposed as a rhythm extreme and
**cancelled**; see *Open* for the risk that leaves.

**Overturned in part 2026-08-09 by M32.** The cancelled identity was the rhythm extreme
(huge magazine, punishing reload). M32 shipped `PRI-C` as a different third Primary: far-wall
damage, no indoor falloff, Railshot on `F`. The rhythm-extreme row stays cancelled; the
two-Primary cap does not.

### Repair rate and the Utility spokes (Zo, 2026-08-07)

**O1 —** The repair rig ships with a **flat repair rate and no upgrade path**. M23 deletes
FABRICATION's Repair Rig track and CREW QUARTERS' Engineering attribute; whether repair rate
later becomes a spoke is M21's to decide, not M23's to assume.

**O2 —** The Utility spoke mapping is a **per-item reading, stated on the row**, not one
fixed table. Clip / reload / fire rate / damage mean whatever that item's five meaningful
axes are; a thrown charge and a repair rig do not read them the same way. "All five spokes
meaningful" stays a rule — the reading is what varies.

### The second Secondary is the close-quarters one (Zo, 2026-08-07)

**O7 —** `SEC-D` is picked: the sidearm you draw when something is already on you. The MK-1
stays the ranged default, which makes the loadout a pairing rather than a single choice — see
the matrix in *What a Secondary has to be*.

Not yet authored, and neither blocks anything: `SEC-D`'s **ability** (M20's contract — one
key, one cooldown, one scalar dial) and its **ring** (M21 — strength and weakness declared,
weakness opposite strength). Both are wanted before it is built, not before it is picked.

### The Secondary deploys its own damage (Zo, 2026-08-07)

**O3 —** A Secondary's value must come from what it does on its own, **not from interacting
with the Primary**. `SEC-B` — fires by itself during the Primary's reload — is **rejected**
on this rule: it is an exception to how weapons work, and it is not strong enough to earn
one. This also removes the contradiction with M25's stowed-reload rule.

### The MK-1's ability (Zo, 2026-08-07)

**O4 / O6 —** Increased ammo regeneration, as an **activated ability with a duration**.
Press the key, regeneration runs faster for a stated number of seconds. **Dial = the
duration** (fixed rate multiplier). It rewards actually holding the Secondary rather than
treating it as a backup, and it is self-contained — it touches nothing the Primary does.
O6 closed 2026-08-07 with M20.

Note for M20: this is the first ability whose value reaches the door **indirectly** — more
ammo means more shots means fewer arrivals. The gatekeeper-second bar still applies, but it is
measured one step removed from the effect.

### Ammo drops and resupply (Zo, 2026-08-07)

**O5 —** Primary ammo drops from **every Gatekeeper, at a per-kind percentage**, and some kinds
are 0%. A resupply **eats a Utility slot** like any other item. *(Superseded in part
2026-08-08: resupply is the drone's **preloaded ammo reserve** — rounds bought in Prep at
the normal Salvage price, fed to the Primary mid-fight; it eats the drone bay. See
[`fabrication`](fabrication.md). The drop model stands.)*

The per-kind percentage is a design surface, not just a number: a kind that drops nothing is
a kind that costs you to kill, which is a lever for making some Gatekeepers unattractive targets
without touching their health.

### What a Secondary has to be (2026-08-07, after three rejections)

Three proposals failed in three different ways, and the pattern is worth more than the
proposals were:

- **`SEC-B`** worked by reading the Primary's reload state — an exception to how weapons work,
  not strong enough to earn one.
- **`SEC-C`** was one big shot on a per-Wave recharge. That is the *Utility* contract word for
  word: finite supply, rescues a moment, cannot carry a fight. It was a grenade with a trigger
  guard.
- **`SEC-D`** traded rate of spend against damage per shot. That is a stat variation on the
  MK-1, not a second weapon — "a damage number with a different name", which is exactly what
  the graduation test exists to catch.

The rules that leaves:

1. **A Secondary is a sidearm-scale object a person would carry**, not a mechanic with a
   trigger attached. If it cannot be described as a thing before it is described as a rule, it
   is not in this class.
2. **Its value is its own** — it never reads the Primary's state.
3. **It is not a Utility item.** One-use, per-Wave recharge, or "answers one specific
   situation" all mean it belongs in the other class.
4. **It is not the MK-1 with the numbers moved.** A different point on the same axis is a
   variant, not a weapon.

**The class's axis (Zo, 2026-08-07 — this replaces a weaker version of it).** "When the
Primary cannot do the job" is not abstract: **it is when the Primary is out of ammo.** Primary
ammo drops from Gatekeepers at a per-kind percentage and some kinds drop nothing (O5), so running
dry is a real state, not an edge case. The Secondary regenerates. That makes the Secondary's
share of a fight substantial rather than incidental, and it is why the class deserves more
than one row.

**Two jobs, not one**, and an earlier version of this section collapsed them:

1. **The starting weapon.** You begin with a Secondary and earn a Primary, so *which*
   Secondary is the player's first build decision — made before they can afford anything else.
   That is starting variety, and it is worth something on its own.
2. **The dry-Primary fallback.** What you are holding when the good gun is empty.

**Loadout pairing is the point.** With two Primaries and two Secondaries the choice stops
being "which sidearm" and becomes "what shape is my whole loadout":

| | ranged Secondary (MK-1) | close Secondary |
|---|---|---|
| **BR-12** (close Primary) | covers the shotgun's blind spot — something to answer what you cannot reach | committed brawler: everything dies inside 10 m, nothing dies outside it |
| **MR-35** (ranged Primary) | pure marksman, and **no answer at all to a Hunter in your face** | the complement: reach when loaded, contact when dry |

That matrix is the argument for a second Secondary. One weapon in the class gives every
loadout the same second half.

**Risk this creates, worth checking rather than assuming:** if the Primary spends real time
dry, the Secondary is carrying a large share of the fight, and "weaker than a Primary" has to
hold across that share or the Primary stops being the reason to save up. *Unknown until ammo
scarcity is played — the check is what fraction of kills come from each slot in a logged Run.*

### Class power bands (Zo, 2026-08-07)

**A Primary's floor is a Secondary's ceiling.** Every Primary has a higher baseline than every
Secondary, so overlap between the classes exists but is never decisive on raw output. This is
what keeps "must not rival a Primary" from needing per-weapon adjudication — it is a property
of the classes, checked once, not a balance argument re-run for every new row.

**But flat comparison is the wrong comparison, and the band only covers half the problem.**
A short-range Secondary against a long-range Primary is not one number against another: at
contact range the Secondary may well win, and that is correct rather than broken. The player
swapping to the right tool for the moment is the game working. What the band guarantees is
that no Secondary is *aggregately* better — not that it loses everywhere.

Which leaves two different failure modes needing two different checks:

| Failure | Check | Where |
|---|---|---|
| A Secondary out-performs a Primary on raw sustained output | Band test at equal investment — arithmetic, cheap, run every build | EditMode |
| A Secondary's situational advantage is large enough, and its situation common enough, that it becomes the weapon you actually use | **Share of kills per slot in a logged Run** | Play |

The second is the one that matters and the one the band cannot see. A loadout can satisfy the
power band perfectly and still have the player doing most of their killing with the sidearm —
at which point the Primary has stopped being the reason to save up, and nothing in the
arithmetic will have said so.

### Constraints that bind any new row

These are **boundaries, not laws** (`../Working-Rules.md`). Each protects fun or player
expression, and each may be broken deliberately when what replaces it serves those better —
with a dated sentence saying what the break buys. Broken by accident is the failure; broken on
purpose and recorded is design.

- **The wheel bounds the roster.** M21 gives every weapon five spokes — reload, clip, damage,
  fire rate, ability — and all five must be meaningful or the weapon has dead spokes and
  fewer than five builds. This rules out a beam with no clip and a weapon that reloads from
  kills instead of a timer. (Whether drone payloads keep the wheel is Open in
  [`fabrication`](fabrication.md), 2026-08-08.)
- **An item that only adds dps is a worse version of a Tier.** FABRICATION's caution, applied
  here unchanged.
- **Class is declared on `WeaponDef`** and the ARMORY groups by it. A weapon that cannot state
  its class does not have one.

## Open — blocked until decided

**Nothing is open in this file.** O1–O7 are all answered and recorded in *Decided*
(2026-08-07). A milestone can be written from any picked gun row without waiting on a
decision. The one outside question that briefly bound this file — the fate of
`WeaponSlot.Utility` — closed in [`fabrication`](fabrication.md) the day it opened: the
slot becomes the drone bay (2026-08-08).

When something opens, it goes here as a table — question, why it blocks, owner, recommendation
— and **nothing in it may be written into a milestone until it is answered**
(`../_meta/_definitions/brainstorm.md`).

**Standing risk, not a question:** both close Primaries (BR-12) and the far-wall PRI-C now
exist beside the all-round MR-35. M21's wheel assumes a player can push a weapon toward what
it is good at. Reload-focus vs clip-focus distinguishability on the MR-35 is still
*unknown until the wheel is played*; PRI-C's 3.2 s reload is a different rhythm, not the
cancelled huge-magazine identity.

## The roster

**One table. `state` is the only place a row's status is recorded.**
`shipped` — in the build. `picked` — agreed, not built. `candidate` — an idea, not agreed.
`rejected` — decided against, kept so it is not re-proposed.

Each row is named by **what it refuses**, the same grammar the gatekeeper roster uses.

| Code | Class | Refuses | Identity in one line | State |
|---|---|---|---|---|
| **BR-12 Shotgun** | Primary | range | Deletes a cluster in your face; useless past the doorway. | shipped |
| **MR-35 Rifle** | Primary | close quarters | Punches through a line at distance; loses to anything already on you. | shipped |
| **PRI-C** | Primary | the near fight | Full damage at 50 m; Railshot on `F`. Not the cancelled rhythm-extreme identity. | shipped (M32) |
| PRI-D | Primary | the doorway | Hardest hitting; damages the Hatch if fired at it from close. | candidate |
| PRI-E | Primary | roaming | Strong, but only reloads near the Hatch. | candidate |
| **MK-1 Sidearm** | Secondary | — (see O4) | The starting weapon. Regenerates its own ammo. | shipped |
| SEC-B | Secondary | your attention | Fires by itself, only while the Primary is reloading. | **rejected** (Zo, 2026-08-07 — a Secondary must not work by interacting with the Primary) |
| SEC-C | Secondary | sustain | One enormous shot, once per Wave. | **rejected** (Zo, 2026-08-07 — a Utility item wearing a Secondary label) |
| **SEC-D** | Secondary | reach | Close-quarters sidearm: empties its pool fast, hits hard, useless past a few metres. | Path 1 shipped (ability/ring still open) |

**The repair rig and the UTL rows left this table 2026-08-08** — they are drone payloads
now, and their one canonical list is the roster in [`fabrication`](fabrication.md).

**Ship order for remaining candidates:** nothing picked is unbuilt. PRI-D and PRI-E wait
for a Run that says a fourth Primary is missing. Payload ship order lives with the payloads.

### Notes on the candidates

**SEC-D** — the sidearm you draw when something is already on you. Empties its regenerating
pool in a few seconds and refills over the quiet, which makes it the mirror of the MK-1's
thrift rather than a number moved along the same axis. It is also the only thing in the game
that answers a Hunter at contact range without the player having bought the BR-12, which
matters because the MR-35's stated weakness is exactly that. *Whether it undercuts the BR-12's
reason to exist is the open risk — a close Secondary and a close Primary may be one weapon too
many pointed at the same problem.*

**PRI-D** — the strongest Primary in the game would be the one that cannot be used from the
safest position. *Unknown whether that reads as a trade or as a bug.*

**PRI-E** — the opposite pull to every Utility payload, which is what makes it interesting in
co-op: one crew member anchored by their gun while another is pulled away by theirs.

## Ideas — not weapons

### A weapon that is not a gun
The Utility class was this idea's first real test; the class moved to FABRICATION as drone
payloads (2026-08-08) and the test moved with it — the drone is the first non-gun. What
breaks in the hitscan `WeaponDef` shape when something is not a gun now gets answered there,
and it is still worth knowing before a fifth gun makes that shape feel settled.

## Rejected

- **Per-weapon upgrade tracks** (damage/rate/clip each per weapon). Triples the shop for no
  decision the player wants. M21's wheel is **not** a reversal: one purchase line per weapon,
  differentiated by ring order, not by adding tracks.
- **Fire rate scaling with Tier.** ⚠ **Overturned 2026-08-06 by M21** — identity becomes ring
  *position*, so raising cadence costs the far side of the ring. Recorded as a reversal in
  `../Decisions.md`.
- **A third Primary as a rhythm extreme** (huge magazine, punishing reload). Cancelled
  2026-08-06 under "two Primaries is the roster." **M32 shipped a different third Primary**
  (`PRI-C`, far-wall / Railshot) on 2026-08-09; that identity is not this rejection.
- **`SEC-C`** (one enormous shot per Wave), 2026-08-07 — a Utility item wearing a Secondary
  label. See *What a Secondary has to be*.
- ~~`SEC-D`~~ was rejected the same day as a stat variation, then **revived** once Zo supplied
  the identity it lacked: it is the close-quarters sidearm, and it is half of the pairing
  matrix above. The original rejection was right about the description and wrong about the
  weapon.
- **`SEC-B`, the offhand autoloader** (2026-08-07). A Secondary that works by reading the
  Primary's reload state is an exception to how weapons work, and this one was not strong
  enough to earn it. The rule it leaves behind: **a Secondary's value comes from what it does
  on its own.**
- **Naming a weapon before it has an identity in play.** See *Codenames* above.
