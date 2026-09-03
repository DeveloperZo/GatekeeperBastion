# 5 — Repeat: zone 2 and the loss that opens the Primary

<!--
Purpose: The loop unit repeats at zone 2 (Zo, 2026-08-17; recut 2026-08-19):
one terminal launching 3 consecutive waves the secondary kit can hold, then
a heavier bank it cannot — that bank falling is the authored loss that sells
the Primary, whether it is the save's first ejection or its second.
Retrigger, clear, bank with Primary + 1 → D2 opens on window survival.
Finale is beats 6–7.
Status: demo spec — SUPERSEDED IN PART 2026-09-02 (Zo): the wheel ladder (rung 1's two buys,
rung 2's Primary + 2) is cut; anything affordable is buyable. Docs/Decisions.md, Cut. The
rung-2 announce (demo.second_loss) and the Primary rack gate stand.
Authority: implementable
Update trigger: when the zone shape, the Convert-resolve Primary gate, the ladder
rungs, or the respawn rule is overridden
Last updated: 2026-08-25 (slot bookkeeping is gone from the tree; buy-back superseded by
the priced respawn and the dead-host gap closed; MaxWaves stated once, as the ceiling)
Depends: voice.md, 4-embark-at-wave-2.md
-->

## Why this beat exists

The old text of this beat encoded a door AND-graph keyed to terminal
completions (D1=T1+T2, D2=T3, D3=T4+T5), a nearest-shut-door allow list, a
one-Field-completion-per-bank-start rule, and 3-per-Run Convert slots. All dead
(Zo, 2026-08-17), and the slots are now gone from the tree as well. The ruled
loop unit: a zone's terminal waves, then the zone's Convert; surviving the
Convert's timer window unlocks the zone's door; a failed Convert never unlocks;
the Convert is locked until the zone's terminals are complete. `MaxWaves` is
**5** and is an absolute end-Wave ceiling, not a per-trigger cap — see Numbers.
There is no slot budget and no per-Run wave ladder.

Demo zone 2 (Zo, 2026-08-17; recut 2026-08-19): **1 terminal × 3 consecutive
waves + a heavier bank → D2** (counts tunable; shape is spec). The 3 field
waves are the previous Wave: survivable on the secondary kit, barely. The
bank is the authored loss, built the same composition-not-script way as zone
1's bank. It may be the save's **first** ejection (zone 1 was docked) or its
**second**. Either way, this bank falling is what sells the Primary.
`PodEjections` is not the gate.

The ordering **is** the spec (numbers tunable):

> secondary-kit ceiling < zone 2 bank requirement < Primary + 1-upgrade kit

A rung-1 kit holding the 3 field waves is expected — not a defect. A rung-1
kit holding the bank is a tuning defect. Consistency (Zo, 2026-08-17): the
dock-at-Hatch return from beat 4 requires a **survived** window, so
return-to-Ship cannot skip this bank with the secondary kit.

The loss **sells** a **Primary + 2 upgrade buys outside the 2-buy secondary
cap** — the cap is a ladder, not a wall: rung 1 was 2 secondary buys (beat
3); this bank falling opens rung 2 (Primary + 2 more buys). Not granted. Not
keyed on a second ejection.

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

### 1 — Zone 2's terminal: 3 consecutive waves (survivable)

| | |
|---|---|
| **Goal** | One trigger, three waves, no Ship between them. Barely survivable on the secondary kit — by design. |
| **Player** | `E`/`T` zone 2's terminal (behind D1). The trigger launches **3 consecutive waves** — Waves 3, 4 and 5, which sit under the **5**-Wave ceiling. The Ante stays live — taught in beat 1, no second lecture. |
| **Game** | — (the consecutive launch is the reader; `demo.zone_retrigger` is for post-loss offers, not first triggers) |
| **Standing** | Clear zone 2's terminal → on composite clear, flips to **Bank at the Hatch** (producing event: zone-complete, log below) |
| **Prompt** | Live terminal prompt. Convert while the zone is incomplete: `Convert — finish the zone` (live string, `RepairInteractable`). |
| **Log** | `[Demo] zone start zone=2 waves=3 mode=composite` · `[Demo] zone complete zone=2` (build) |
| **Have** | Terminals, Prep-only start, host-sim gate. Verified 2026-08-20: `wavesToComplete` does **not** drive a consecutive launch — N there means N separate triggers with Prep between. The consecutive primitive is `RunSim.TriggerCompositeDraw`, which is zone-agnostic and reusable. There is no old `FirstTerminal` 3-wave cycle — `FirstTerminal.Waves == 1`; nothing to retire. |
| **Build** | Zone 2 terminal config (3 waves, consecutive); `DemoZoneDeepestWave[2]` written on each field-wave clear; the two logs. |

### 2 — The heavier bank: the authored loss that opens the Primary

| | |
|---|---|
| **Goal** | Lose at the banked wave, by construction — the previous Waves were held; this window is the hand-off. |
| **Player** | Hatch `E` then `T` (beat 2's surface). The window's composition is **heavier** than zone 1's (tunable): above the secondary-kit ceiling, below Primary + 1. The Hatch falls; leftover Unbanked wipes; dripped salvage is aboard. Lose screen, `ENTER`. |
| **Game** | — (end screen is the reader; no scripted kill) |
| **Standing** | Bank at the Hatch (unchanged through the loss; the Docked standing is step 3's) |
| **Log** | existing `[Hatch] hatch-bank started …`, `[Hatch] HATCH BREACHED — the run is over`, `[Run] RUN OVER after …s … banked now …` (in tree, needs code verification — cause tokens `hatch`/`player`, not `down`) |
| **Have** | Drip 8/s, no dump on start, wipe on `EndRun`, attrition — beat 2 facts, in tree. `EndRun("hatch")` → Lost, `PodEjections++`. |
| **Build** | Heavier window composition for zone 2 (numbers tunable; the ordering above is the spec). Write `DemoZone2BankLost` (or the implemented flag) when this window **resolves either way** — Lost during it (`hatch` or player-down), or survived (amended Zo 2026-08-25: the rack rewards having answered zone 2, not specifically having bled there; the loss-only original under-rewarded the player who held). A Lost during the 3 field Waves still does not write it. Do not call `Die()`. Do not script the loss. |

### 3 — Docked after this bank falls: the Primary is for sale

| | |
|---|---|
| **Goal** | This bank falling reads on Docked: a standing is produced, and the rack sells a Primary — whether this ejection is the save's first or its second. |
| **Player** | Dismiss end screen; Ship loads. ARMORY now offers the **Primary** row plus **2 upgrade buys outside the secondary 2-cap** (rung 2 of the ladder). Buy Primary + at least 1 upgrade (this bank's pile funds it; numbers tunable). CREW heal still live; FAB stays dark. |
| **Game** | `demo.second_loss` · Lock · Local · Game — **id kept** (historical name; trigger recut 2026-08-19). **What transferred is aboard — and the rack has heavier hardware now.** (copy draft — Zo; must not require this to have been a second ejection). Trigger: Docked after `DemoZone2BankLost`, at any ejection count — `== 0` is now reachable (a held zone-2 window sets the flag since the 2026-08-25 resolve-either-way amendment) and the line's copy was restamped outcome-agnostic for it. Not `PodEjections >= 2` alone. No same-frame Lock partner exists on this Docked (`demo.crew_ready` latched in beat 4); if a future line lands here, order must be stated then. |
| **Standing** | Re-equip at the ARMORY (produced by `demo.second_loss`) → on Primary purchase, flips to **EMBARK from the NAV MAP** (producing event: the buy, existing wheel/weapon success log — no new line) |
| **Prompt / panel** | ARMORY rows: Primary + remaining rung-2 buys. Refuse past the rung: named ladder refuse (copy draft — Zo), not the salvage-short line. |
| **Log** | `[Demo] ladder rung=2 opened — primary + 2 buys` · existing buy success lines · `[Demo] wheel refused — ladder rung spent` (build) |
| **Have** | Wheel, catalog, named refuses, costs — beat 3 surface, in tree (needs code verification). `DemoWheelBuysLeft` (v9, beat 3) counted rung 1 only. |
| **Build** | Ladder bookkeeping: rung 2 = the PRIMARY rows + 2 buys on **their own counter** (`DemoRungTwoBuysLeft`), opened by `DemoZone2BankLost` (recut Zo 2026-08-19; not `PodEjections >= 2`); the line; the two logs. **Do not extend `DemoWheelBuysLeft`** — its absolute value is the multiplayer key (`< 2`) and the once-per-save MP line's guard (`== 1`), so raising it re-locks multiplayer mid-demo (verified 2026-08-20). Primary costs are already in the catalog: 900 / 1,200 / 1,500. |

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
| Door open on terminal AND | yes | `BastionUnlockGraph.TryOpenDoors` → `SetOpen` (M52/M53 lineage) | **Done in tree** (verified 2026-08-20) — `RecordBankSurvived` is the only path to `TryOpenDoors`; the AND survives as *eligibility* and is **not** deletable, D2 reads T3 through it |
| Manual `[E] Open door` on done terminal | **no** | removed | **Done in tree** (verified 2026-08-20) — the branch is gone with a retirement comment; nothing owed |
| Door prompts on the door itself | **no** | doors are not `IInteractable` | None — do not add |
| 3-consecutive-wave terminal trigger | **primitive yes, wiring no** | `RunSim.TriggerCompositeDraw` is zone-agnostic and shipped | Build: zone 2's terminal config only. No `MaxWaves` change — Waves 3–5 fit the ceiling |
| Bank locked until zone complete | partial | old T1-done gate | Beat 4 build, reused |
| Retrigger offer + partial election | no | — | Beat 4 build, reused for zone 2 |
| Convert slots per Boarding | **gone** | `HatchBank.SlotsPerRun` and every reader are removed from the tree | Done — nothing owed |
| Ladder rung 2 (Primary + 2 buys) | **no** | beat 3's `DemoWheelBuysLeft` is rung 1 | Build |
| Dock at Hatch / Docked cause | no | — | Beat 4 build, reused |
| Buy-back | **shipped, in a different shape** | `Respawns.cs`, `RunSim.Respawns.cs`, `GameController.Down.cs` | Have. Not window-gated and not paid from banked Salvage: any dead seat pays **150** × (respawns taken this Boarding + 1) out of its **own Unbanked** while the Boarding is Tethered |
| Survive / down rules | yes | `M49`/`M57`, plus the down state above | Have — the dead-host case closed with the priced respawn |
| Door voice / logs | no | — | Build |

## Create

| Path | Role |
|---|---|
| Zone 2 terminal config | 1 terminal × 3 consecutive waves (counts tunable; shape is spec) |
| Zone 2 bank composition | heavier window (tunable) satisfying: secondary ceiling < requirement < Primary+1 |
| Ladder rung 2 | Primary row + 2 buys outside the secondary 2-cap, opened by zone 2's bank falling |
| Tests | 3-wave composite per trigger; bank refuse before zone complete; loss at bank leaves D2 shut; retrigger composite/partial; survival opens D2; rung refuse past ladder; buy-back refused outside a live window |

## Change

| File | Change |
|---|---|
| `RunSim` | Nothing. `TriggerCompositeDraw` is in tree and the slot bookkeeping is already gone |
| `GameController.HatchBank` | Zone-complete gate (beat 4); heavier zone 2 window spec |
| `BastionUnlockGraph` / `BastionDoor.SetOpen` | Bank-survival keying (beat 4 change; D2 wired here) |
| `ShipCommands` | Ladder rung counter; Primary row unlock on `DemoZone2BankLost`; named rung refuse. Do not read `PodEjections >= 2` |
| `BastionTerminal.Prompt` | Zone-incomplete bank string; retrigger offer rows (beat 4 surface) |

Do not rebuild rooms. Do not emit a second kit.

## Delete

| Item | Why |
|---|---|
| Nearest-shut-door allow list (`DemoRun.AllowedField`) | never built — do not add; zone gating via doors + zone-complete bank lock replaces it (Zo, 2026-08-17) |
| One-Field-completion-per-bank-start rule | dead (Zo, 2026-08-17) — never built, do not add |
| Door AND-graph keying (D1=T1+T2 etc.) as this beat's spec | superseded by bank-window survival (Zo, 2026-08-17); the in-tree graph is the superseded reader — removal scope needs code verification |
| Convert slot bookkeeping (3 per Boarding) | dead (Zo, 2026-08-17) and now **removed** — `HatchBank.SlotsPerRun` and its readers are gone from the tree. Do not reintroduce it |
| Multi-terminal zone / D3 = T4+T5 structure | dead **as this beat's spec** (Zo, 2026-08-17); finale structure is beats 6–7's editor's. **Do not delete D3 from the graph** — `BastionUnlockGraph.Validate` requires exactly one `OpensControlRoom` door and fails without it (verified 2026-08-20) |
| `demo.door_d3` | dead with the above; beats 6–7 own the Control Room line |

## Numbers

| Name | Value | Tunable? | Source |
|---|---|---|---|
| Zone 2 shape | 1 terminal × 3 consecutive waves + heavier bank → D2 | counts tunable; **shape is spec** | Zo, 2026-08-17 |
| Bank-loss ordering | secondary ceiling < zone 2 bank requirement < Primary + 1 upgrade | numbers tunable; **ordering is spec** | Zo, 2026-08-17; recut 2026-08-19: field waves survivable |
| Ladder rung 2 | Primary + 2 upgrade buys outside the secondary 2-cap | rung contents tunable | Zo, 2026-08-17 |
| Primary cost | BR-12 1,500 / MR-35 2,000 / PRI-C 2,500 (raised 2026-09-02; were 900 / 1,200 / 1,500) | tunable | `WeaponCatalog.Def`; the rack gate reads the cheapest (`DemoProgress.PrimaryRackSalvage`) |
| Respawn cost | **150** × (respawns taken this Boarding + 1) | tunable | `Respawns.BaseCost` / `Respawns.CostFor`. The counter is the Boarding's, not the seat's, so a party that keeps dying prices itself out together |
| `MaxWaves` | **5**, an absolute end-Wave ceiling | tunable | `WaveDirector.MaxWaves`. The per-trigger re-key was an intent, not the tree. Zone 2's Waves are 3–5 and fit under the ceiling, so no re-key is required — and this is the row every other file should be corrected against |
| Window | 45 s base | tunable | `HatchBank.DurationSeconds` (in tree) |
| Drip | 8/s | tunable | `HatchBank.DripPerSecond` (in tree) |
| Zone 2 wave rosters | existing wave-table indices as placeholder (bodies `18+12·i`, HP muls, Hunter/Thief tables) | tunable — mapping to zone waves is Zo's | in-tree `NextWave` tables |

No slot arithmetic (slots are gone from the tree). No "cannot Convert
infinitely" clause — the zone-complete lock and the **5**-Wave ceiling bound the
loop instead.

## Multiplayer

| Event | Authority |
|---|---|
| Terminal start / retrigger election | Host sim, any seat at the console (live) |
| Bank refuse (zone incomplete) | Host; all seats see the prompt |
| Door open | Host, on bank-window survival; replicated |
| Wave scale | `playerCount` on every `NextWave` |
| Guest down | `M57` wait; host continues. Converted drip stays on guest wallet |
| **Buy-back** | Shipped, and wider than the 2026-08-17 stamp. Any dead seat is offered a respawn while the Boarding is Tethered — no Convert-window gate — priced at **150** × (respawns taken this Boarding + 1) out of that seat's **own Unbanked**, remainder surviving. Nothing else can pay. Host-authoritative (`RunSim.TryRespawn`) |
| **Dead host** | Closed. The host seat takes the same offer at the DOWN overlay, and `M49` now bites only when the dying seat cannot pay and no other seat is still fighting |
| Dock at Hatch | Beat 4 rule; commit-seat question is beat 4's Gap |
| Voice door lines | Party |

Late joiner on Ship between Runs: own loadout, own `BankCap` from their own
`DemoZoneDeepestWave` (re-key; needs code verification of the wallet-deepest
path it replaces). No wave picker exists to inherit.

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.repeat_unit` | Lock | Party | first Prep frame, once per Run after the first (trigger kept; **copy changed** — draft, Zo) | Clear zone 2's terminal |
| `demo.second_loss` | Lock | Local | **recut 2026-08-19**: Docked after `DemoZone2BankLost` (id kept; not `PodEjections >= 2`) | Re-equip at the ARMORY |
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
  failing on the secondary kit; the Primary row appearing after that bank
  falls; D2 opening on survival, not on terminal completion.
- **Reads-back** `[Demo] zone start zone=2 waves=3 mode=composite`,
  `[Demo] zone complete zone=2`, `[Demo] bank refused — zone incomplete`,
  `[Demo] ladder rung=2 opened — primary + 2 buys`,
  `[Demo] zone retrigger zone=2 …`, `[Demo] bank window survived zone=2`,
  `[Demo] door open D2 cause=bank`, and the in-tree `respawn taken — slot=<n> cost=<n> carry <a>-><b> taken=<k>`.

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
| Buy-back | `respawn taken — slot=<n> cost=<n> carry <a>-><b> taken=<k>` · refuse `respawn refused — not enough unbanked` | in tree (`RunSim.Respawns.cs`) |

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
- Letting a dead seat pay from the party's pile, the realm's bank or another
  seat. `Respawns.Refuse` gates on two things only: the Boarding is Tethered,
  and the seat's own carry covers the price.
- Re-gating the respawn to a live Convert window. It is not window-gated, and
  narrowing it would fork the rule between the two demos.
- Firing `demo.first_hatch_down` on this bank falling — that id is first-loss
  of the *save* only, and this bank may be the save's first ejection;
  `demo.second_loss` owns Docked after `DemoZone2BankLost`, including when
  `PodEjections == 1`.
- Keying the Primary row on `PodEjections >= 2` — a player who docked zone 1
  never sees it.
- Making the 3 field Waves the loss. They are survivable; the bank is the
  hand-off.
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
4. EditMode: Docked after `DemoZone2BankLost` at `PodEjections=1` **and** at
   `PodEjections=2` → `demo.second_loss` fires and the ladder log
   `[Demo] ladder rung=2 opened — primary + 2 buys` appears; a Lost during
   zone 2's field Waves does not write the flag or open the row; third
   rung-2 upgrade buy refuses with `[Demo] wheel refused — ladder rung
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
8. Loopback: a dead guest carrying at least the price buys back → `respawn
   taken — slot=<n> cost=<n> …`, rejoins at the Hatch, its own Unbanked
   debited and the remainder intact; a seat carrying less → `respawn refused
   — not enough unbanked`. Door open replicates to both seats.
9. No `[Dev]`.

## Gaps

- **Dead host vs `M49`**: closed by the priced respawn. The host takes the same
  offer as every other seat, and `M49` reduces to the case where nobody left can
  pay.
- Filename `5-repeat-to-control-room.md` predates the ruled shape (the
  Control Room walk is beats 6–7's, another editor); content is written to
  the ruled spec; rename is Zo's call.
- Zone 2's terminal physical reachability while D1 is shut (the old R6–R9
  open passage let T3 start early): whether a refuse surface is still needed
  at the terminal, or the layout closes it, needs code verification — the
  old nearest-shut-door rule that covered it is dead.
- Primary cost, buy-back cost, heavier-bank composition, exact wave counts:
  tunable, not set — the orderings above are the spec.
- Primary cost is set; what is not is the respawn price against this beat's
  authored window. A Boarder mid-push carries far more than **150**, so the
  window has to fall on the **Hatch** to be a reliable loss, exactly as zone 1's
  does.

## Rejected

The nearest-shut-door allow list and one-completion-per-bank-start rule
(superseded, Zo 2026-08-17 — not merely unbuilt). A fourth bank slot (slots
are dead entirely). Auto-opening D2 on terminal completion. Scripted bank
kill. Gifting the Primary. Keying the Primary on `PodEjections >= 2`.
Making the 3 field Waves unclearable. A flat wheel cap (it is a ladder).
Buy-back from the shared pile, the realm's bank, or another seat. Hunter
lecture. Siphon. Second destination.
Door-mounted prompts.
