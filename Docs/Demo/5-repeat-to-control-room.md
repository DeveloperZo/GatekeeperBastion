# 5 — Repeat: zone 2 and the second loss

<!--
Purpose: The loop unit repeats at zone 2 (Zo, 2026-08-17): one terminal
launching 3 consecutive waves, clearable barely by the secondary kit; then a
heavier bank the secondary kit cannot clear — the second authored loss lands
at the banked wave by construction. The loss grants Primary + 2 upgrades
outside the 2-buy secondary cap (the cap is a ladder). Retrigger, clear, bank
with Primary + 1 → D2 opens on window survival. Finale is beats 6–7.
Status: demo spec
Authority: implementable
Update trigger: when the zone shape or the loss-2 ordering is overridden,
when the ladder rungs change, when the buy-back or dead-host rule is ruled
Last updated: 2026-08-17
Depends: voice.md, 4-embark-at-wave-2.md
-->

## Why this beat exists

The old text of this beat encoded a door AND-graph keyed to terminal
completions (D1=T1+T2, D2=T3, D3=T4+T5), a nearest-shut-door allow list, a
one-Field-completion-per-bank-start rule, and 3-per-Run bank slots. All dead
(Zo, 2026-08-17). The ruled loop unit: a zone's terminal waves, then the
hatch-bank wave; surviving the bank's timer window unlocks the zone's door; a
failed bank never unlocks; the bank is locked until the zone's terminals are
complete. `MaxWaves` caps consecutive waves **per terminal trigger**, not per
Run — there is no slot budget and no per-Run wave ladder.

Demo zone 2 (Zo, 2026-08-17): **1 terminal × 3 consecutive waves + a heavier
bank → D2** (counts tunable; shape is spec). This beat is the second authored
loss, built the same authored-both-sides way as loss 1 — composition, not
script. The ordering **is** the spec (numbers tunable):

> secondary-kit ceiling < zone 2 bank requirement < Primary + 1-upgrade kit

The 3 field waves are clearable **barely** by the secondary kit; the banked
wave cannot be cleared without Primary + 1 upgrade minimum — so the death
lands at the banked wave by construction. Consistency (Zo, 2026-08-17): the
dock-at-Hatch return from beat 4 requires a **survived** window, so
return-to-Ship cannot skip this bank with the secondary kit.

The death grants a **Primary + 2 upgrade buys outside the 2-buy secondary
cap** — the cap is a ladder, not a wall: loss 1 opened rung 1 (2 secondary
buys, beat 3); loss 2 opens rung 2 (Primary + 2 more buys).

v9 ruling (stated in beat 4, held here): `DemoNavUnlockWave` /
`DemoBankCapWave` are **re-keyed** to per-zone `DemoZoneDeepestWave` —
deepest cleared field wave per zone, written on field-wave clear, read by the
deepest terminal's composite retrigger and guest `BankCap`. Do not resurrect
the old fields or the bank-start checkpoint.

## Functional storyboard

Continues from beat 4 step 4/5: D1 open, party in zone 2 (or Docked and
re-embarked — beat 4's dock path lands here too). Standing arrives as
**Zone 2 is behind D1** (produced by `demo.door_d1`, beat 4; on the dock
path, re-produced by the snapshot-restore log — every described path has a
producer).

### 0 — The unit, named once per Run

| | |
|---|---|
| **Goal** | Recognize the loop: the zone's terminal waves, then the Hatch; the door opens when the bank survives. |
| **Player** | Continue dismisses the Lock. |
| **Game** | `demo.repeat_unit` · Lock · Party · Game — id kept, **copy changed** (old copy said doors open when terminals are done — superseded). **Same job, deeper rooms: the zone's waves, then the Hatch. The door opens when the bank survives.** (copy draft — Zo) Once per Run after the first, first Prep frame. |
| **Standing** | Clear zone 2's terminal (produced by `demo.repeat_unit`) |
| **Log** | `[Demo] line id=demo.repeat_unit …` |
| **Have** | Nothing — no per-Run repeat line. |
| **Build** | The line + once-per-Run latch in the demo runner. |

### 1 — Zone 2's terminal: 3 consecutive waves

| | |
|---|---|
| **Goal** | One trigger, three waves, no Ship between them. Barely survivable on the secondary kit — by design. |
| **Player** | `E`/`T` zone 2's terminal (behind D1). The trigger launches **3 consecutive waves** (under the re-keyed `MaxWaves` per-trigger cap). Juice stays live — taught in beat 1, no second lecture. |
| **Game** | — (the consecutive launch is the reader; `demo.zone_retrigger` is for post-loss offers, not first triggers) |
| **Standing** | Clear zone 2's terminal → on composite clear, flips to **Bank at the Hatch** (producing event: zone-complete, log below) |
| **Prompt** | Live terminal prompt. Bank while zone incomplete: `Hatch-bank — finish the zone` (beat 4 string, copy draft — Zo). |
| **Log** | `[Demo] zone start zone=2 waves=3 mode=composite` · `[Demo] zone complete zone=2` (build) |
| **Have** | Terminals, Prep-only start, host-sim gate. `wavesToComplete` exists per terminal — needs code verification that it can drive a 3-consecutive launch under the re-keyed `MaxWaves`. Old `FirstTerminal` 3-wave cycle encodes the dead shape — needs code verification before reuse. |
| **Build** | Zone 2 terminal config (3 waves, consecutive); `DemoZoneDeepestWave[2]` written on each field-wave clear; the two logs. |

### 2 — The heavier bank: the second authored loss

| | |
|---|---|
| **Goal** | Lose at the banked wave, by construction — same authored-both-sides design as loss 1, one zone deeper. |
| **Player** | Hatch `E` then `T` (beat 2's surface). The window's composition is **heavier** than zone 1's (tunable): above the secondary-kit ceiling, below Primary + 1. The Hatch falls; leftover Unbanked wipes; dripped salvage is aboard. Lose screen, `ENTER`. |
| **Game** | — (end screen is the reader; no scripted kill) |
| **Standing** | Bank at the Hatch (unchanged through the loss; the Docked standing is step 3's) |
| **Log** | existing `[Hatch] hatch-bank started …`, `[Hatch] HATCH BREACHED — the run is over`, `[Run] RUN OVER after …s … banked now …` (in tree, needs code verification — cause tokens `hatch`/`player`, not `down`) |
| **Have** | Drip 8/s, no dump on start, wipe on `EndRun`, attrition — beat 2 facts, in tree. `EndRun("hatch")` → Lost, `PodEjections++`. |
| **Build** | Heavier window composition for zone 2 (numbers tunable; the ordering above is the spec). Do not call `Die()`. Do not script the loss. |

### 3 — Docked after loss 2: the ladder opens

| | |
|---|---|
| **Goal** | Second-and-later losses read on Docked (carried defect, fixed): a standing is produced, and the rack has heavier hardware. |
| **Player** | Dismiss end screen; Ship loads. ARMORY now offers the **Primary** row plus **2 upgrade buys outside the secondary 2-cap** (rung 2 of the ladder). Buy Primary + at least 1 upgrade (the loss-2 pile funds it; numbers tunable). CREW heal still live; FAB stays dark. |
| **Game** | `demo.second_loss` · Lock · Local · Game — **new id** (fixes the carried defect: beat 2's `demo.first_hatch_down` fires on the **first** loss only, so second-and-later Docked had no standing producer). **Thrown off again. What transferred is aboard — and the rack has heavier hardware now.** (copy draft — Zo) Trigger: Docked after a Lost, second and later losses (`PodEjections >= 2`). No same-frame Lock partner exists on this Docked (`demo.crew_ready` latched in beat 4); if a future line lands here, order must be stated then. |
| **Standing** | Re-equip at the ARMORY (produced by `demo.second_loss`) → on Primary purchase, flips to **EMBARK from the NAV MAP** (producing event: the buy, existing wheel/weapon success log — no new line) |
| **Prompt / panel** | ARMORY rows: Primary + remaining rung-2 buys. Refuse past the rung: named ladder refuse (copy draft — Zo), not the salvage-short line. |
| **Log** | `[Demo] ladder rung=2 opened — primary + 2 buys` · existing buy success lines · `[Demo] wheel refused — ladder rung spent` (build) |
| **Have** | Wheel, catalog, named refuses, costs — beat 3 surface, in tree (needs code verification). `DemoWheelBuysLeft` (v9, beat 3) counted rung 1 only. |
| **Build** | Ladder bookkeeping: rung counter extending `DemoWheelBuysLeft` (v9) — rung 2 = Primary row + 2 buys outside the secondary cap (Zo, 2026-08-17); the line; the two logs. Primary cost not set — tunable, Zo. |

### 4 — Retrigger zone 2, clear, bank with Primary + 1 → D2

| | |
|---|---|
| **Goal** | The post-loss retrigger at multi-wave scale, then the door. |
| **Player** | Plain EMBARK (beat 4 step 2). At zone 2's terminal (its only — therefore deepest — terminal): retrigger offer, default **composite** (all 3 waves consecutively); **partial start electable** (e.g. wave 3 of 3 — all were cleared before the loss; election reads `DemoZoneDeepestWave[2]`), clear the remainder independently, then bank. With Primary + 1 upgrade minimum, the window is clearable — survive it; **D2 opens**. |
| **Game** | `demo.zone_retrigger` (beat 4 id, per-zone latch — fires here for zone 2) then, on survival, `demo.door_d2` · Lock (Prep) · Party · Game — id kept, **trigger changed** (Zo, 2026-08-17): `SetOpen(true)` via zone 2 bank-window survival, not terminal AND. **That door is open. The next terminals are behind it.** (existing string, serves D1 and D2) |
| **Standing** | Clear zone 2 and bank at the Hatch (produced by `demo.zone_retrigger`) → on D2 open, **Toward the Control Room** (produced by `demo.door_d2`; beats 6–7 own what it points at — copy draft, Zo) |
| **Prompt** | Retrigger offer rows (beat 4 surface). After survival: dock prompt (beat 4 step 5) — available until the next wave is triggered. |
| **Log** | `[Demo] zone retrigger zone=2 start=<j> waves=<k> mode=composite|partial` · `[Demo] bank window survived zone=2` · `[Demo] door open D2 cause=bank` |
| **Have** | Retrigger offer, election, bank-keyed door open — beat 4 builds, shared. `BastionUnlockGraph` terminal-AND listener is the superseded reader (beat 4's key code delta; needs code verification). |
| **Build** | Zone 2 wiring only; the logs. Nothing new mechanically beyond beat 4's builds. |

### 5 — Optional dock; hand-off

Surviving zone 2's window offers the same held-interact dock as beat 4
step 5 (`demo.dock_option`, Subtitle — no Lock collision with
`demo.door_d2`, which plays first; stated order). Run ends **Docked**;
banked + door state (D1, D2) persist via the successful-bank snapshot.
Walking on through D2 toward the Control Room is beats 6–7 (another editor).
FAB stays dark; juice stays live; no Hunter or Thief lecture — wave rosters
introduce them.

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 5 |
|---|---|---|---|
| Kit doors + rooms | yes | sketch `bastion-defense-layout.txt` + emitter wiring | Have — zone mapping is new: zone 1 → D1, zone 2 → D2; D3/T4/T5 out of demo shape (finale, beats 6–7 editor) |
| Door open on terminal AND | yes | `BastionUnlockGraph.TryOpenDoors` → `SetOpen` (M52/M53 lineage) | **Superseded** — bank-window survival keys doors (beat 4 delta); needs code verification |
| Manual `[E] Open door` on done terminal | yes | `BastionTerminal` done-Field branch | Superseded reader — a completed terminal no longer opens a door; needs code verification of removal scope |
| Door prompts on the door itself | **no** | doors are not `IInteractable` | None — do not add |
| 3-consecutive-wave terminal trigger | **no** | `wavesToComplete` default 1 | Build (under re-keyed `MaxWaves`; needs code verification) |
| Bank locked until zone complete | partial | old T1-done gate | Beat 4 build, reused |
| Retrigger offer + partial election | no | — | Beat 4 build, reused for zone 2 |
| Bank slots 3 per Run | yes | `HatchBank.SlotsPerRun`, `RunSim.StartRun` | **Dead** (Zo, 2026-08-17) — see Delete; needs code verification of readers |
| Ladder rung 2 (Primary + 2 buys) | **no** | beat 3's `DemoWheelBuysLeft` is rung 1 | Build |
| Dock at Hatch / Docked cause | no | — | Beat 4 build, reused |
| Buy-back during live window | **no** | — | Build (cost tunable) |
| Survive / down rules | yes | `M49`/`M57` | Have — dead-host buy-back case OPEN (Gaps) |
| Door voice / logs | no | — | Build |

## Create

| Path | Role |
|---|---|
| Zone 2 terminal config | 1 terminal × 3 consecutive waves (counts tunable; shape is spec) |
| Zone 2 bank composition | heavier window (tunable) satisfying: secondary ceiling < requirement < Primary+1 |
| Ladder rung 2 | Primary row + 2 buys outside the secondary 2-cap, opened by loss 2 |
| Buy-back interact | dead seat, live bank window only, rejoin at Hatch, pays own banked (cost tunable) |
| Tests | 3-wave composite per trigger; bank refuse before zone complete; loss at bank leaves D2 shut; retrigger composite/partial; survival opens D2; rung refuse past ladder; buy-back refused outside a live window |

## Change

| File | Change |
|---|---|
| `RunSim` | Zone 2 composite launch per trigger (re-keyed `MaxWaves`); retire per-Run slot bookkeeping (see Delete) |
| `GameController.HatchBank` | Zone-complete gate (beat 4); heavier zone 2 window spec |
| `BastionUnlockGraph` / `BastionDoor.SetOpen` | Bank-survival keying (beat 4 change; D2 wired here) |
| `ShipCommands` | Ladder rung counter; Primary row unlock on `PodEjections >= 2`; named rung refuse |
| `BastionTerminal.Prompt` | Zone-incomplete bank string; retrigger offer rows (beat 4 surface) |
| Hatch (net) | Buy-back interact for dead seats while a window is live |

Do not rebuild rooms. Do not emit a second kit.

## Delete

| Item | Why |
|---|---|
| Nearest-shut-door allow list (`DemoRun.AllowedField`) | never built — do not add; zone gating via doors + zone-complete bank lock replaces it (Zo, 2026-08-17) |
| One-Field-completion-per-bank-start rule | dead (Zo, 2026-08-17) — never built, do not add |
| Door AND-graph keying (D1=T1+T2 etc.) as this beat's spec | superseded by bank-window survival (Zo, 2026-08-17); the in-tree graph is the superseded reader — removal scope needs code verification |
| Bank slot bookkeeping (3 per Run) | dead (Zo, 2026-08-17); in tree (`HatchBank.SlotsPerRun`, reset in `RunSim.StartRun`) — removal is a code change; needs code verification of other readers |
| Multi-terminal zone / D3 = T4+T5 structure | dead for the demo shape (Zo, 2026-08-17); finale structure is beats 6–7's editor's |
| `demo.door_d3` | dead with the above; beats 6–7 own the Control Room line |

## Numbers

| Name | Value | Tunable? | Source |
|---|---|---|---|
| Zone 2 shape | 1 terminal × 3 consecutive waves + heavier bank → D2 | counts tunable; **shape is spec** | Zo, 2026-08-17 |
| Loss-2 ordering | secondary ceiling < bank requirement < Primary + 1 upgrade | numbers tunable; **ordering is spec** | Zo, 2026-08-17 |
| Ladder rung 2 | Primary + 2 upgrade buys outside the secondary 2-cap | rung contents tunable | Zo, 2026-08-17 |
| Primary cost | not set | tunable — Zo | — |
| Buy-back cost | not set | tunable — Zo | Zo, 2026-08-17 (rule ruled, cost open) |
| `MaxWaves` | ≥ 3, per terminal trigger | tunable | re-key (Zo, 2026-08-17) |
| Window | 45 s base | tunable | `HatchBank.DurationSeconds` (in tree) |
| Drip | 8/s | tunable | `HatchBank.DripPerSecond` (in tree) |
| Zone 2 wave rosters | existing wave-table indices as placeholder (bodies `18+12·i`, HP muls, Hunter/Thief tables) | tunable — mapping to zone waves is Zo's | in-tree `NextWave` tables |

No slot arithmetic (slots are dead). No "cannot bank infinitely" clause — the
zone-complete lock and per-trigger `MaxWaves` bound the loop instead.

## Multiplayer

| Event | Authority |
|---|---|
| Terminal start / retrigger election | Host sim, any seat at the console (live) |
| Bank refuse (zone incomplete) | Host; all seats see the prompt |
| Door open | Host, on bank-window survival; replicated |
| Wave scale | `playerCount` on every `NextWave` |
| Guest down | `M57` wait; host continues. Converted drip stays on guest wallet |
| **Buy-back** | A dead player may buy back **only while a bank window is live**, rejoining at the Hatch, paying from **their own** banked salvage (cost tunable) (Zo, 2026-08-17). Host-authoritative; log below |
| **Dead host** | `M49` says host death ends the Run; a live-window buy-back for the **host** contradicts it — **OPEN, recorded, not resolved** (Zo, 2026-08-17) |
| Dock at Hatch | Beat 4 rule; commit-seat question is beat 4's Gap |
| Voice door lines | Party |

Late joiner on Ship between Runs: own loadout, own `BankCap` from their own
`DemoZoneDeepestWave` (re-key; needs code verification of the wallet-deepest
path it replaces). No wave picker exists to inherit.

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.repeat_unit` | Lock | Party | first Prep frame, once per Run after the first (trigger kept; **copy changed** — draft, Zo) | Clear zone 2's terminal |
| `demo.second_loss` | Lock | Local | **new**: Docked after Lost, `PodEjections >= 2` (second and later losses — fixes the missing Docked standing) | Re-equip at the ARMORY |
| `demo.zone_retrigger` | Lock (Prep) | Party | beat 4 id; per-zone latch — fires on zone 2's first retrigger offer | Clear zone 2 and bank at the Hatch |
| `demo.door_d2` | Lock (Prep) | Party | **changed**: `SetOpen(true)` via zone 2 bank-window survival | Toward the Control Room |
| `demo.dock_option` | Subtitle | Party | beat 4 id; zone 2 window survival, after `demo.door_d2` (stated order; Subtitle — no Lock collision) | unchanged |
| `demo.door_d3` | — | — | **dead** — D3 structure out of demo shape; beats 6–7 own the Control Room line | — |

New/changed copy is draft for Zo. No Hunter tutorial line. No Thief tutorial
line.

## Driven / Shown / Reads-back

- **Driven by** `E`/`T` on zone 2's terminal and the Hatch; ARMORY rung-2
  buys; retrigger election; optional dock; co-op buy-back.
- **Shown** 3 waves off one trigger with no Ship between; the heavier window
  failing on the secondary kit; the Primary row appearing after loss 2; D2
  opening on survival, not on terminal completion.
- **Reads-back** `[Demo] zone start zone=2 waves=3 mode=composite`,
  `[Demo] zone complete zone=2`, `[Demo] bank refused — zone incomplete`,
  `[Demo] ladder rung=2 opened — primary + 2 buys`,
  `[Demo] zone retrigger zone=2 …`, `[Demo] bank window survived zone=2`,
  `[Demo] door open D2 cause=bank`, `[Demo] buyback seat=<id> cost=<n>`.

## Logs

| Event | Line | Status |
|---|---|---|
| Zone start | `[Demo] zone start zone=2 waves=3 mode=composite` | build |
| Zone complete | `[Demo] zone complete zone=2` | build |
| Bank refused | `[Demo] bank refused — zone incomplete` | build (beat 4) |
| Loss 2 end | `[Run] RUN OVER after …s … banked now …` | in tree (needs code verification) |
| Ladder | `[Demo] ladder rung=2 opened — primary + 2 buys` | build |
| Rung refuse | `[Demo] wheel refused — ladder rung spent` | build |
| Retrigger | `[Demo] zone retrigger zone=2 start=<j> waves=<k> mode=composite|partial` | build (beat 4 shape) |
| Window survived | `[Demo] bank window survived zone=2` | build |
| Door | `[Demo] door open D2 cause=bank` | build |
| Buy-back | `[Demo] buyback seat=<id> cost=<n>` · refuse `[Demo] buyback refused — no live window` | build |

## Traps

- Carrying any of the dead structure forward: slots, AND-graph, allow list,
  one-completion-per-bank-start, D3=T4+T5. Do not.
- Opening D2 from the terminal's done-branch (`[E] Open door`) — that path is
  a superseded reader; doors open on bank-window survival only.
- Softening the window so the secondary kit can clear it "to be fair." The
  ordering is the spec: the loss must land at the banked wave, and dock
  cannot skip an unsurvivable window — both break if the ceiling moves above
  the requirement.
- Scripting the death (`Die()`, forced breach). Composition is the design,
  same as loss 1.
- Granting the Primary on Docked instead of selling it — the ladder opens
  buys; the walk to the rack is the beat.
- Counting the Primary or SEC-D against the secondary 2-cap, or rung-2 buys
  against rung 1. The cap is a ladder.
- Letting a dead player buy back between windows, or pay from the party's
  pile — live window only, own banked only.
- Resolving the dead-host case in code "while in there" — it is recorded
  OPEN, not ruled.
- Firing `demo.first_hatch_down` on loss 2 — its trigger is first-loss only;
  `demo.second_loss` owns Docked from `PodEjections >= 2`.
- Teaching FAB because the heavier window is harsh. Withheld.
- A door tutorial on the door itself — doors are not interactables.

## Depends / Unblocks

Depends: beat 4 (plain embark, retrigger offer, bank-keyed doors, dock,
`DemoZoneDeepestWave`); beat 3 ladder rung 1. Unblocks: beats 6–7 (another
editor) when D2 is open.

## Verification

1. EditMode: zone 2 trigger launches 3 waves consecutively; log `[Demo] zone
   start zone=2 waves=3 mode=composite`; no Ship phase between them; the
   re-keyed `MaxWaves` caps the trigger, not the Run.
2. EditMode: bank start before wave 3 clears → `[Demo] bank refused — zone
   incomplete`; after `[Demo] zone complete zone=2` → bank arms.
3. EditMode: `EndRun("hatch")` on zone 2's window → D2 `SetOpen` never fires;
   no `[Demo] door open D2` line.
4. EditMode: Docked with `PodEjections=2` → `demo.second_loss` fires and the
   ladder log `[Demo] ladder rung=2 opened — primary + 2 buys` appears;
   third rung-2 upgrade buy refuses with `[Demo] wheel refused — ladder rung
   spent` without `SpendSalvage`; rung-1 history unaffected.
5. EditMode: retrigger at zone 2's terminal with `DemoZoneDeepestWave[2]=3`
   defaults to composite (`start=1 waves=3`); partial election logs
   `start=3 waves=1 mode=partial`.
6. PlayMode: window survival with Primary + 1 → `[Demo] bank window survived
   zone=2` then `[Demo] door open D2 cause=bank`; `demo.door_d2` plays before
   the dock prompt appears (stated order).
7. Tuning observation (open, `[unconfirmed]` until logged): a secondary-kit
   run (rung-1 buys only, no Primary) never logs `bank window survived
   zone=2` — the authored ceiling holds. Do not close this beat on
   arithmetic alone.
8. Loopback: dead guest during a live window buys back → `[Demo] buyback
   seat=<id> cost=<n>`, rejoins at the Hatch, own banked debited; same
   attempt after the window ends → `[Demo] buyback refused — no live
   window`. Door open replicates to both seats.
9. No `[Dev]`.

## Gaps

- **Dead host vs `M49`** (host-death-ends-run vs live-window buy-back):
  **OPEN — recorded, not resolved** (Zo, 2026-08-17). Do not build the host
  branch until ruled.
- Filename `5-repeat-to-control-room.md` predates the ruled shape (the
  Control Room walk is beats 6–7's, another editor); content is written to
  the ruled spec; rename is Zo's call.
- Zone 2's terminal physical reachability while D1 is shut (the old R6–R9
  open passage let T3 start early): whether a refuse surface is still needed
  at the terminal, or the layout closes it, needs code verification — the
  old nearest-shut-door rule that covered it is dead.
- Primary cost, buy-back cost, heavier-bank composition, exact wave counts:
  tunable, not set — the orderings above are the spec.
- Slot-bookkeeping removal scope (`HatchBank.SlotsPerRun` readers) — needs
  code verification.
- Upstream files (beats 1–3, README) still carry slots, the terminal-AND
  door rows, and the old checkpoint fields — superseded; their editors'.

## Rejected

The nearest-shut-door allow list and one-completion-per-bank-start rule
(superseded, Zo 2026-08-17 — not merely unbuilt). A fourth bank slot (slots
are dead entirely). Auto-opening D2 on terminal completion. Scripted loss-2
kill. Gifting the Primary. A flat wheel cap (it is a ladder). Buy-back from
the shared pile. Hunter lecture. Siphon. Second destination. Door-mounted
prompts.
