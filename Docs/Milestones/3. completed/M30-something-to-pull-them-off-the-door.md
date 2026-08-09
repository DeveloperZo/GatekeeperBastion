# M30 — Something to pull them off the door

<!--
Status: shipped, August 2026 — Path 1 EditMode green; Path 2 / playthrough open
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-09
-->

The drone bay carries one payload. The first payload is the decoy: place it and Defaults and
Hunters walking to your door walk to it instead, until they have chewed through it.

## Done when

**Met (code + EditMode Path 1).** Bay selectable; decoy buy/equip at FABRICATION; ghost
place; pull rules (Ram never, committed Default never); 120 HP / 3 charges / one live;
`Decoy` wave gatekeeper-seconds line; SaveVersion **6**. Harness: **DecoyTests 9/0**; suite
**247 / 0** (Controls / Dev / ReloadSwap / PremiseCopy excluded as before).

## Remaining to close

| # | Status | Evidence |
|---|---|---|
| Path 2 AutoVerify | **open** | Bay select + ghost invalid off-NavMesh + retarget with baked NavMesh; Unity MCP unavailable this session |
| PlayMode `DecoyPlayModeTests` | **open** | Written; not run (needs Unity Test Runner) |
| Playthrough path | **open** | Buy → embark Wave 3 → place off-door → Defaults/Hunters peel, Ram walks past → three charges → fourth refused → read `Decoy` wave line |

## What shipped

- `PayloadId` / `PayloadCatalog` (Decoy: 250 Salvage, 3 charges, 120 HP, 12 m pull, 20 m place).
- `DecoyState` — charges, live HP, `AbsorbedSeconds = damage / 3.43`, pull refusals.
- `DecoyBay` / `DecoyPlacement` / `DecoyObject` — two-press ghost, Ignore-Raycast prop, no `Damageable`.
- Targeting: decoy ahead of Hatch/player; Ram + committed Default refuse.
- FABRICATION Payload row; HUD charges + live; `ControlId.AltFire` cancels ghost.
- `SaveData` v6 (`OwnedPayloadMask`; `EquippedDrone` is `PayloadId`).
- `Docs/Design/4-Data.md` decoy table.

## Story

As a player, I want to put something in the room that Gatekeepers would rather attack than
my door, so that where I choose to stand and what I choose to defend become decisions
instead of one fixed answer.

## Why it matters

Every incentive in the game currently points at the Hatch: all three Gatekeeper kinds
resolve there, and standing on the door answers all of them
(`../../Brainstorms/gatekeepers-and-waves.md`). `../../Working-Rules.md` files *a reason to
be somewhere other than the Hatch* as the highest-value unused structure in the game, and
nothing built so far supplies one.

The decoy supplies it in the door's own currency. The Hatch dies to `arrivals x dwell`
(`../../Design/3-Mechanics.md`); a decoy that absorbs attacks converts damage that would
have landed on the door into damage that lands on an object you placed. Because a parked
Default delivers the same effective damage to either, **the decoy's HP is gatekeeper-seconds
at a fixed exchange rate** — the milestone's central number, below.

Leverage tier (`../../Working-Rules.md`): **#5, rules.** It adds a target Gatekeepers may
prefer, which is a new rule about who walks where, not a tuning value.

## The decoy's worth, in the budget's own unit

One parked Default delivers `4 / 0.7 = 5.71` dps at the measured ~60% landing rate = **3.43
effective dps** (`../../Design/4-Data.md`). That rate is the same against the Hatch and
against the decoy, so:

```
gatekeeper-seconds absorbed = decoyHP / 3.43
120 HP  ->  35 gatekeeper-seconds
```

**And it is independent of how many attack it** — more attackers kill it faster and absorb
the same total. That is why HP is the right supply for this payload: the thing the decoy
sells is exactly the thing the door spends.

Against a 300 HP Hatch's ~88 gatekeeper-second budget, one decoy is **~40% of a door-load**,
and three charges across a Run is ~105 gatekeeper-seconds — enough to matter, capped so it
cannot carry a fight (`../../Brainstorms/fabrication.md`, the payload contract).

`120 HP`, `3 charges`, `12 m pull radius`, `20 m placement range`, `250 Salvage` are all
**initial values, *tunable, needs play data***, recorded in `../../Design/4-Data.md`.

## Acceptance criteria

1. The drone bay holds a payload rather than a weapon. **Met** — slot rename (v5) + payload
   ownership (v6).
2. **One payload per Run.** **Met** — `SetEquippedPayload` mid-Run is a logged no-op
   (`OnePayloadPerRun`).
3. The decoy is a FABRICATION payload row, bought with Salvage, and equipping it is what
   puts it in the bay. Owning no payload leaves the bay empty and selectable-but-inert.
   **Met (code)** — Path 2/play for UI.
4. **In a Run, selecting the bay (`3`, `Q` or the wheel) and pressing fire shows a
   placement ghost; pressing fire again places the decoy there.** **Met (code)** — Path 2
   open for ghost invalid / confirm.
5. A placed decoy retargets **Defaults and Hunters within 12 m**. **Met (code + EditMode
   pull helper)** — scene retarget needs Path 2 / play.
6. **Two refusals hold.** **Met** — `CommittedDefaultIsNotPulled`, `RamIsNeverPulled`.
7. The decoy has **120 HP**, Hatch-damage intake, no timer, survives Wave boundary, player
   fire cannot damage it. **Met (EditMode + PlayMode written)**.
8. **3 charges per Run**, one live, reset at Run start. **Met** — EditMode.
9. Per-Wave `Decoy` gatekeeper-seconds line. **Met (code)** — play to read.
10. HUD charges + live; FABRICATION shows equipped payload. **Met (code)** — Path 2 open.

## Verification

### Path 1 — EditMode harness `--filter Decoy`

| Test | Asserts | Criterion |
|---|---|---|
| `ChargesAreSpentOnPlaceAndRefusedAtZero` | 3 ok, 4th refused | 8 |
| `OnlyOneLiveAtATime` | live refuse spends nothing | 8 |
| `AbsorbedSecondsIsDamageOverThreeFourThree` | 120 → ~35.0 gs | 9 |
| `DecoyDiesExactlyAtZeroHp` | dies at 0 | 7 |
| `CommittedDefaultIsNotPulled` | commit refuses | 6 |
| `RamIsNeverPulled` | Ram refuses | 6 |
| `PayloadIdIsStableAcrossSave` | id 1 survives v6 | 1 |
| `OnePayloadPerRun` | mid-Run equip no-op | 2 |
| `PreV6EquippedDroneSentinelBecomesNone` | −1 → None | 1 |

Evidence: 2026-08-09 harness run — `PASS  9 passed, 0 failed` (Decoy filter); full suite
`247 / 0`.

### Path 2 — AutoVerify / play

Still required: selectable bay on strip; invalid ghost; Default retarget on baked NavMesh;
player fire leaves decoy HP untouched in a live Run.

### Playthrough path

Play → FABRICATION → buy and equip the decoy → NAV MAP → start at Wave 3 → let a group
spawn and start walking → press `3`, place the decoy **out in the room, not at the door** →
watch Defaults and Hunters break off and attack it, and a Ram walk past it. Let it die,
place a second, then a third, and confirm the fourth is refused. Read the per-Wave
`Decoy` line and compare the Hatch's remaining HP with a Run where the decoy was never
placed.

## Depends on

Save bump **v6** (v5 was the Drone rename).

## Unblocks

Further payloads in `../../Brainstorms/fabrication.md` become catalog rows plus behaviour.
