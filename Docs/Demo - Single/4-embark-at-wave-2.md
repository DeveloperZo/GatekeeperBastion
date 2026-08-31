# 4 — Embark after the first Convert

<!--
Purpose: Plain re-embark after loss 1 (Zo, 2026-08-17: NAV skip is dead —
refight-avoidance lives at the terminal). Zone 1's deepest terminal retriggers
the zone composite by default; partial start electable. This time the bank
window holds and D1 opens on window survival, not on terminal completions.
New death direction: after a survived bank window the player may dock at the
Hatch — Run ends Docked, not Lost. CREW menu + Default claim unchanged.
Status: demo spec
Authority: implementable
Update trigger: when the composite-retrigger or door-on-Convert-survival ruling
is overridden, when the dock-at-Hatch rule moves, when Protocol beat is moved
Last updated: 2026-08-25 (CREW key is the first Convert, not DemoWave2Seen, which does
not exist; MaxWaves is an absolute ceiling; retrigger and dock shipped; VEIL and FORGE
are claims, not Coming Soon; buy-back row rewritten from Respawns.cs)
Depends: voice.md, 2-first-hatch-bank.md, 3-ship-after-first-loss.md (same grain)
-->

## Why this beat exists

The old text of this beat encoded a NAV **skip** to `DemoNavUnlockWave` with a
per-Run wave budget behind it. Both are dead (Zo, 2026-08-17). The `MaxWaves`
re-key that ruling leaned on never landed — `WaveDirector.MaxWaves` is **5** and
is an absolute end-Wave ceiling — but the conclusion holds anyway: the authored
zones run Waves 1–2 and 3–5, so there is nothing for a picker to skip past. NAV embark is **plain** — lead commits,
guests Ready, countdown, no wave choice. Refight-avoidance moved to the
terminal: after a loss, the zone's **deepest** terminal retriggers the zone
composite (all its waves consecutively) by default, and the player may elect a
**partial start** (skip waves at or below the zone's deepest cleared), clear
the remainder, then bank.

Loop unit (Zo, 2026-08-17): a zone's terminal waves, then the hatch-bank wave.
Surviving the bank's timer window unlocks the zone's door. A failed bank never
unlocks. The bank is locked until the zone's terminals are complete. Demo
zone 1 = **2 terminals × 1 wave each + bank → D1** (counts tunable; shape is
spec). Loss 1 (beat 2) was zone 1's bank failing after both zone-1 waves were
cleared — so this beat is: claim the Default, spend loss 1's banked ~500,
embark plain, retrigger zone 1, and hold the window. D1 opens.

**Key code delta, stated plainly:** the in-tree door graph keys door open on
**terminal completions** (`BastionUnlockGraph` AND → `SetOpen`, M52/M53
lineage). This spec keys doors on **bank-window survival** (Zo, 2026-08-17).
Needs code verification: what the graph listens to today, and where a
bank-clear event can drive `SetOpen` without the terminal-AND input.

**v9 fields ruling (state once, hold everywhere):** `DemoNavUnlockWave` and
`DemoBankCapWave` are **re-keyed, not deleted** (Zo, 2026-08-17) — replaced by
per-zone `DemoZoneDeepestWave`: the deepest cleared field wave in that zone,
written on each field-wave clear, read by the deepest terminal's composite
retrigger (partial-start ceiling) and by guest `BankCap` while co-op. NAV
reads nothing. Beat 5 uses the same field; do not resurrect the old two.

CREW is separate and unchanged: the claim path is **already in tree**
(select-then-sheet, `BuyProtocol` → `TryClaim`, 0 salvage, claimed log — M70's
in-tree work, needs code verification). This beat only decides when the CREW
**menu** replaces the beat-0 heal-only interact.

## Functional storyboard

Same milestone as beat 3. Standing arrives as **Spend Salvage at the ARMORY**
(produced by `demo.first_hatch_down`, beat 2 — first loss; second-and-later
losses produce theirs via beat 5's `demo.second_loss`).

Same-frame Lock order (stated): on the first Docked after loss 1, beat 2's
`demo.first_hatch_down` and this beat's `demo.crew_ready` both trigger. Lock
lines queue; `demo.first_hatch_down` plays first, `demo.crew_ready` second.
No other same-frame Lock pair exists in this beat (the dock offer in step 5 is
prompt + Subtitle, never Lock, by construction).

### 0 — The CREW menu is open (this Ship visit)

| | |
|---|---|
| **Goal** | Notice CREW has more than bandages now. Beat 2's window dripped salvage through the Hatch, so `HatchConversions` is 1 by this first Docked. |
| **Player** | Walk past CREW (or be told once). |
| **Game** | `demo.crew_ready` · Lock · Local · Game. **CREW QUARTERS has more than a bunk now. Your protocol waits there.** Once, on the first Docked after the first Convert with the menu still locked — queued after `demo.first_hatch_down` (order above). |
| **Standing** | Claim the Default Protocol (produced by `demo.crew_ready`; optional before EMBARK — NAV never requires one) |
| **Prompt** | `[E] CREW QUARTERS — protocols` |
| **Log** | `[Demo] CREW menu unlocked` on the flip |
| **Have** | `MetaState.DemoCrewMenuUnlocked` and `HatchConversions`; CREW station + panel live; heal-on-interact from beat 0. |
| **Build** | Flip `DemoCrewMenuUnlocked` on the first Docked with `HatchConversions >= 1`. **`DemoWave2Seen` does not exist and must not be added** — it was deleted with the 2026-08-18 re-key that moved the wheel and the CREW menu onto the first Convert. |

### 1 — Open CREW, claim the Default

| | |
|---|---|
| **Goal** | Own a protocol: Barrage on `G`, Rush on `H`. |
| **Player** | Interact Down on CREW. Panel opens (heal still applies on the visit): catalog rows — Default, VEIL and FORGE are all claimable (`BoarderProtocolId`), AEGIS and LEDGER are Coming Soon. SELECT → Protocol Sheet → EQUIP (0 salvage for the Default). |
| **Game** | `demo.crew_claim` · Lock · Local · Game. **This is your protocol. Claim the Default — Barrage on G, Rush on H. It costs nothing.** |
| **Standing** | Claim the Default Protocol → on claim success, flips to **EMBARK from the NAV MAP** (producing event: the claim itself, existing `claimed Default Protocol track=crew` log — no new line) |
| **Prompt / panel** | Live: sheet headings Protocol Title / Description / Crew / Personal / Passive; Default doctrine `STOCK — hardware as sold`; button EQUIP → EQUIPPED. |
| **Log** | `[Demo] CREW menu unlocked` then existing `claimed Default Protocol track=crew` |
| **Have** | Select-then-sheet, `BuyProtocol` → `TryClaim` (sets `EquippedProtocol`), `G`/`H` refuse `no protocol` until claimed. `M70` is planned but its claim path is in tree — needs code verification; do not rebuild CREW. |
| **Build** | `DemoCrewMenuUnlocked` (v9), the two lines, heal folded into the open. Do not auto-equip Default. |

### 2 — NAV: plain EMBARK

| | |
|---|---|
| **Goal** | Back to the Bastion. No wave choice — the fight resumes at the terminal, not at NAV. |
| **Player** | Open powered NAV. One commit: EMBARK. Lead commits; guests Ready; 5 s countdown. No picker, no stepper, no wave number. |
| **Game** | `demo.embark_wave2` · Lock · Party · Game — **trigger changed** (Zo, 2026-08-17): first powered-NAV open while Docked after a loss. Copy draft for Zo (old copy named the skip and Wave 4): **The NAV MAP takes you back. The Bastion remembers what you cleared — the terminal will offer it.** (copy draft — Zo) |
| **Standing** | EMBARK from the NAV MAP (produced in step 1 by the claim; if the optional claim was skipped, produced here by `demo.embark_wave2` on NAV open — both described paths have a producer) |
| **Prompt / panel** | Plain `EMBARK` commit. The live `START WAVE {n} / cleared {deepest}` picker line does not render while `DemoActive` — and the live picker/chain question after the demo is `The-Ship.md`'s, not this beat's. |
| **Log** | `[Demo] embark plain` on the Bastion side |
| **Have** | Lead-only commit via lobby (`M72` c4–c6); Ready + 5 s countdown; join-during-countdown refused. Picker + stepper + `PendingStartWave` wave choice exist in tree — needs code verification of how much of the picker surface `MapStation.BuildPanel` can suppress cleanly. |
| **Build** | While `DemoActive`: no picker (plain EMBARK). `StartRun` takes no wave argument from NAV. **Do not build** the old skip (`NavMax`, picker default, chain-off branch) — dead (Zo, 2026-08-17). |

### 3 — Zone 1's deepest terminal: composite retrigger

| | |
|---|---|
| **Goal** | Refight-avoidance at the terminal. Default: the whole zone again. Electable: start deeper. |
| **Player** | Walk to zone 1's deepest terminal (T2). Interact opens the retrigger offer: default **composite** — both zone-1 waves, consecutively; or elect a **partial start** (waves at or below `DemoZoneDeepestWave[1]` may be skipped; here both are cleared, so partial refights wave 2 only). `T` commits; the elected waves run consecutively, then the bank re-arms when the zone is complete. |
| **Game** | `demo.zone_retrigger` · Lock (Prep) · Party · Game — **new id**. **This terminal remembers the zone. Run it all again, or start deeper and finish what's left. The Hatch re-arms when the zone is clear.** (copy draft — Zo) Trigger: first retrigger offer opened per zone per save. |
| **Standing** | Clear zone 1 and bank at the Hatch (produced by `demo.zone_retrigger`) |
| **Prompt** | Offer panel: `RESTART ZONE — {k} waves` (default) / `START AT WAVE {j}` election rows. Copy draft — Zo. |
| **Log** | `[Demo] zone retrigger zone=1 start=1 waves=2 mode=composite` or `[Demo] zone retrigger zone=1 start=2 waves=1 mode=partial` |
| **Have** | Shipped. `GameController.Retrigger.cs` builds the offer, `RunSim.TriggerCompositeDraw` runs the elected waves consecutively, and `DemoZones.DeepestTerminalId` picks the terminal that carries it. `MaxWaves` stayed a **5**-Wave absolute ceiling and did not need re-keying: zone 2 runs Waves 3–5 under it. |
| **Build** | The two logs. |

### 4 — The bank holds: D1 opens on window survival

| | |
|---|---|
| **Goal** | The loop closes the right way: survive the window, the door is the reward. |
| **Player** | Zone complete → Hatch `E` arms, `T` starts (beat 2's surface, unchanged). Loss 1's ~500 bought upgrades (beat 3) — this time the 45 s window holds. Dump on survive; **D1 opens**. |
| **Game** | `demo.door_d1` · Lock (Prep; Subtitle if somehow Assault) · Party · Game — id kept from the old beat 5, **trigger changed** (Zo, 2026-08-17): D1 `SetOpen(true)` driven by zone 1 bank-window survival, not by terminal completions. **That door is open. The next terminals are behind it.** (existing string; serves D1 and D2) |
| **Standing** | Zone 2 is behind D1 (hand-off to beat 5; produced by `demo.door_d1`) |
| **Prompt** | Convert refuse while the zone is incomplete: `Convert — finish the zone` (live string, `RepairInteractable`). |
| **Log** | existing `[Hatch] hatch-bank started …`; `[Demo] bank window survived zone=1`; `[Demo] door open D1 cause=bank` |
| **Have** | Drip 8/s, no dump on start, dump on survive, 45 s window, attrition — beat 2 facts, in tree. `BastionUnlockGraph.TryOpenDoors` AND on terminal completions — **superseded reader** (key code delta above); needs code verification of what the graph listens to today. |
| **Build** | Bank locked until the zone's terminals are complete (refuse + log). Door open keyed to bank-window survival; a failed bank never unlocks (Zo, 2026-08-17). The successful-bank snapshot now carries **banked + door state** (see step 5); needs code verification of what the snapshot stores today. |

### 5 — Optional: dock at the Hatch (Run ends Docked, not Lost)

| | |
|---|---|
| **Goal** | Death is no longer the only run ender (Zo, 2026-08-17). A shop trip without dying. |
| **Player** | After a survived bank window: **held interact** at the Hatch, available until the next wave is triggered. Run ends **Docked**. Banked and door state persist via the successful-bank snapshot. Dock, spend, re-embark (step 2) — D1 stays open. |
| **Game** | `demo.dock_option` · **Subtitle** · Party · Game — **new id**, deliberately not Lock so it cannot collide with `demo.door_d1` on the survival frame (`demo.door_d1` plays first; this follows). **The Hatch can take you home — hold to dock. Or push on.** (copy draft — Zo) |
| **Standing** | unchanged (Zone 2 is behind D1) — Docked-and-return re-produces it via the snapshot restore log, so the standing has a producer on the dock path too |
| **Prompt** | `[hold E] Dock — return to Ship` while available; hidden once the next wave is triggered. |
| **Log** | `[Demo] docked at hatch — run Docked banked=<n> doors=D1`; on next embark `[Demo] snapshot restored doors=D1` |
| **Have** | Shipped. `RunSim.Dock.cs` carries `DockedCause = "docked"`, `DockOffered`, the `BankWindowSurvived` event and `DockRun()`; the offer is refused into the finale rather than left unguarded. |
| **Build** | The two logs, and the snapshot's door-state contents if they are not there yet. Consistency (Zo, 2026-08-17): docking requires a **survived** window, so a window the kit cannot clear (beat 5's loss 2) cannot be skipped by docking. |

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 4 |
|---|---|---|---|
| Plain lead commit + Ready + countdown | yes | `M72` c4–c6 lobby | Have (needs code verification) |
| NAV picker / stepper / `PendingStartWave` wave choice | yes | `MapStation`, `NavMapPanelView` | Superseded surface — suppress while `DemoActive`; do not build the skip |
| Chain on deep start / `ClampStartWave` | yes | `RunSim` | Have — `MaxWaves` stayed an absolute ceiling, so the clamp is still the right reader |
| Composite retrigger at deepest terminal | **yes** | `GameController.Retrigger.cs`, `RunSim.TriggerCompositeDraw` | Have |
| Partial-start election | **yes** | `DemoRetriggerOffer` + `DemoZoneDeepestWave` | Have |
| Door open on terminal completions | yes | `BastionUnlockGraph.TryOpenDoors` → `SetOpen` (M52/M53 lineage) | **Superseded** — re-key to bank-window survival; needs code verification |
| Bank locked until zone terminals complete | partial | old T1-done start gate (`GameController.HatchBank`) | Build (generalize per zone; needs code verification) |
| Dock at Hatch (Boarding ends Docked) | **yes** | `RunSim.Dock.cs` | Have |
| Successful-bank snapshot | yes | restore path in `RunSim` | Re-key contents to banked + door state — needs code verification |
| CREW select→sheet→claim | yes | `CrewQuartersStation`, `ProtocolSheetView`, `BuyProtocol` (M70 in-tree work) | Have — needs code verification |
| `G`/`H` refuse without protocol | yes | `crew/self refused — no protocol` | Have (needs code verification) |
| `DemoCrewMenuUnlocked` | yes | `MetaState` / `RealmState` | Have — the write on the first Docked after the first Convert is this beat's |
| `DemoWave2Seen` | **deleted** | — | None. Do not add it; the CREW key is `HatchConversions >= 1` |
| `DemoZoneDeepestWave` (per zone) | yes | `MetaState.SetZoneDeepest` / `DemoZoneCount` | Have |
| Voice ids | no | [`voice.md`](voice.md) | Build |

## Create

| Path | Role |
|---|---|
| `DemoProgress.ZoneDeepest(meta, zone)` | deepest cleared field wave per zone; written on field-wave clear; read by retrigger election + guest BankCap |
| `DemoRun.RetriggerOffer(zone)` | default composite vs partial election at the zone's deepest terminal |
| Hatch dock interact | held `E` after survived window, until next wave trigger → `EndRun("docked")` |
| Tests | composite launches all zone waves consecutively; partial launches remainder only; failed bank leaves D1 shut; survived bank opens D1; dock ends Run Docked with snapshot |

## Change

| File | Change |
|---|---|
| `MapStation.BuildPanel` | Plain EMBARK while `DemoActive` — no picker, no default wave |
| `RunSim` | Nothing for `MaxWaves` — it stayed **5** and absolute, and every authored Wave fits under it |
| `RunSim.TriggerCompositeDraw` | Shipped: elected start..zone end, consecutively |
| `BastionUnlockGraph` / `BastionDoor.SetOpen` | Door keyed to bank-window survival, not terminal AND (key code delta; needs code verification of the current listener) |
| `GameController.HatchBank` | Zone-complete gate replaces the T1-done gate; refuse + log |
| `RunSim.EndRun` | Shipped — cause `docked`. Owed: the snapshot carrying door state |
| `CrewQuartersStation` | Heal-only until `DemoCrewMenuUnlocked`; panel after |
| `Play/The-Ship.md` | One **Gaps** line: plain embark while `DemoActive`; picker/chain return after first Escaped is that file's question |

`DeepStartTests`: the demo-skip cases from the old text of this beat are dead —
do not add them. Live chain tests stay untouched for `!DemoActive`.

## Delete

| Item | Why |
|---|---|
| NAV skip build (`NavMax`, picker max/default override, chain-off branch, `StartRun` clamp-to-NavMax) | never built — do not add (Zo, 2026-08-17) |
| `DemoNavUnlockWave`, `DemoBankCapWave` (v9) | re-keyed to `DemoZoneDeepestWave` per zone (Zo, 2026-08-17); never shipped, do not add under the old names |
| Beat 2's checkpoint-on-bank-start write rule | superseded — `DemoZoneDeepestWave` writes on field-wave **clear**, not bank start (upstream edit is beat 2's editor's; noted in Gaps) |

## Numbers

| Name | Value | Tunable? | Source |
|---|---|---|---|
| Zone 1 shape | 2 terminals × 1 wave each + bank → D1 | counts tunable; **shape is spec** | Zo, 2026-08-17 |
| Banked after loss 1 | ~500 | tunable | Zo, 2026-08-17 — covers both Secondary buys (150+300=450, in tree) |
| Window | 45 s | tunable | `HatchBank.DurationSeconds` (in tree) |
| Drip | 8/s | tunable | `HatchBank.DripPerSecond` (in tree, lever 2 shipped) |
| Countdown | 5 s | — | `NetPartyLobby.CountdownSeconds` |
| Default claim cost | 0 | — | `TryClaim` |
| `MaxWaves` | **5**, an absolute end-Wave ceiling | tunable | `WaveDirector.MaxWaves`. The per-trigger re-key was an intent, not the tree; zone 2 runs Waves 3–5 under the ceiling |
| Zone 1 wave rosters | existing wave-table indices as placeholder | tunable — mapping to zone waves is Zo's | in-tree `NextWave` tables |

No dial-0 Wave-4 roster and no BankCap(3) arithmetic here — those were the
skip's numbers; dead with it.

## Multiplayer

| Event | Authority |
|---|---|
| EMBARK | Lead commit, plain (`M72` c4). Guest NAV read-only. No wave choice to replicate |
| Retrigger election | Host sim, any seat at the console (live terminal rule); elected start replicated with the wave spec |
| Guest BankCap | Guest's own `DemoZoneDeepestWave` (re-key, Zo 2026-08-17); needs code verification of the wallet-deepest path it replaces |
| Door open | Host, on bank-window survival; replicated |
| Dock at Hatch | Ends the Run as Docked for the party; which seat's held interact commits it is **not ruled** — see Gaps |
| Dead-seat buy-back | Shipped, and wider than the 2026-08-17 stamp: any dead seat is offered a respawn for as long as the Boarding is Tethered, with no Convert-window gate. Price is **150** × (respawns taken this Boarding + 1), paid from that seat's **own Unbanked**, remainder surviving; nothing else can pay. The dead-host case closed with it — the host takes the same offer at the DOWN overlay |
| Ready / countdown / join refuse | Unchanged `M72` |
| `demo.embark_wave2` | Party, lead opened NAV |
| CREW menu / claim | Local. Each seat reads its own `HatchConversions`; a seat on `NetShipWait` still gets it |

Lead disconnect: session ends.

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.crew_ready` | Lock | Local | first Docked with `HatchConversions >= 1`, menu locked — queued after `demo.first_hatch_down` (stated order) | Claim the Default Protocol |
| `demo.crew_claim` | Lock | Local | first CREW panel open | Claim the Default Protocol |
| `demo.embark_wave2` | Lock | Party | **changed**: first powered-NAV open while Docked after a loss (was: unlock ≥ 2 skip) — id stale, see Gaps | EMBARK from the NAV MAP |
| `demo.zone_retrigger` | Lock (Prep) | Party | **new**: first retrigger offer opened per zone | Clear zone 1 and bank at the Hatch |
| `demo.door_d1` | Lock (Prep) | Party | **changed** (moved from beat 5): `SetOpen(true)` via bank-window survival | Zone 2 is behind D1 |
| `demo.dock_option` | Subtitle | Party | **new**: bank-window survival, after `demo.door_d1` (Subtitle by construction — no Lock collision) | unchanged |

New/changed copy above is draft for Zo. Fight: `G`/`H` are live once claimed.
No extra Assault line for Protocol.

## Driven / Shown / Reads-back

- **Driven by** CREW open + claim; lead plain EMBARK; retrigger election at
  T2; Hatch `E`/`T`; optional held-`E` dock.
- **Shown** plain EMBARK (no picker); retrigger offer (composite vs partial);
  window held; D1 opening on survival; dock prompt until the next wave.
- **Reads-back** `[Demo] embark plain`, `[Demo] zone retrigger zone=1 …`,
  `[Demo] bank window survived zone=1`, `[Demo] door open D1 cause=bank`,
  `[Demo] docked at hatch — run Docked …`, `[Demo] CREW menu unlocked`,
  existing `claimed Default Protocol track=crew`.

## Logs

| Event | Line | Status |
|---|---|---|
| CREW | `[Demo] CREW menu unlocked` | build |
| Claim | `claimed Default Protocol track=crew` | in tree (needs code verification) |
| Plain embark | `[Demo] embark plain` | build |
| Retrigger | `[Demo] zone retrigger zone=1 start=<j> waves=<k> mode=composite|partial` | build |
| Bank refused (zone incomplete) | `[Demo] bank refused — zone incomplete` | build |
| Window survived | `[Demo] bank window survived zone=1` | build |
| Door | `[Demo] door open D1 cause=bank` | build |
| Dock | `[Demo] docked at hatch — run Docked banked=<n> doors=D1` | build |
| Snapshot restore | `[Demo] snapshot restored doors=D1` | build |

## Traps

- Rebuilding the NAV skip "because the constants are still in the README."
  Dead. Refight-avoidance is the terminal's retrigger, nothing at NAV.
- Keying D1 on terminal completions because that is what the in-tree graph
  does. The graph is the superseded reader — doors open on bank-window
  survival only (Zo, 2026-08-17). A failed bank must leave D1 shut.
- Letting the bank arm before the zone's terminals are complete.
- Writing `DemoZoneDeepestWave` on bank start (the old checkpoint rule). It
  writes on field-wave **clear**.
- Leaving the dock interact live after the next wave is triggered — the
  window is survival-to-next-trigger only.
- Counting a Docked end as a loss (`PodEjections++`, Lost screen). Docked is its
  own cause. It does still open the wheel, because the wheel key is the Convert
  that happened, not the death that did not.
- Two Locks on the survival frame — `demo.dock_option` is Subtitle by
  construction; do not promote it.
- Requiring Ready on CREW claim. Auto-owning Default when the menu unlocks —
  the claim is the walk.
- Dropping the heal when the menu unlocks — CREW keeps restoring health on a
  visit for the rest of the demo.
- Rewriting `The-Ship.md`'s live picker paragraph as if plain embark were the
  campaign.

## Depends / Unblocks

Depends: beat 2 loss shape (zone 1 bank failed); beat 3 ARMORY spend (same
milestone); CREW claim path (M70 in-tree work, needs code verification).
Unblocks: beat 5 (zone 2 behind D1; dock/buy-back rules carry forward).

## Verification

1. EditMode: zone 1 with `DemoZoneDeepestWave=2`, failed Convert → retrigger at
   T2 defaults to composite; log `[Demo] zone retrigger zone=1 start=1 waves=2
   mode=composite`; both waves launch consecutively, inside the **5**-Wave
   ceiling.
2. EditMode: partial election → log `… start=2 waves=1 mode=partial`; only
   wave 2 launches; bank re-arms after it clears.
3. EditMode: bank start refused before zone complete — log `[Demo] bank
   refused — zone incomplete`; no slot/wave side effects.
4. EditMode: window survival → `SetOpen(true)` on D1, log `[Demo] door open
   D1 cause=bank`; `EndRun("hatch")` instead → D1 stays shut, no door log.
5. PlayMode: powered NAV shows plain EMBARK, no picker; log `[Demo] embark
   plain`; no `START WAVE` line while `DemoActive`.
6. PlayMode: after window survival, held `E` at the Hatch before the next
   trigger → log `[Demo] docked at hatch — run Docked …`; end screen is not
   the Lost screen; `PodEjections` unchanged; next embark logs `[Demo]
   snapshot restored doors=D1` and D1 is open.
7. PlayMode: trigger the next wave first → dock prompt gone; held `E` does
   nothing.
8. CREW menu opens this Ship visit (`HatchConversions >= 1` from beat 2's
   window); heal still fires; Lock order on Docked is `demo.first_hatch_down`
   then `demo.crew_ready`.
9. Existing live tests stay green for `!DemoActive`.
10. No `[Dev]`.

## Gaps

- **Stale filename/id (Zo's call to rename):** this file is
  `4-embark-at-wave-2.md` and the voice id is `demo.embark_wave2`; nothing in
  this beat embarks "at wave 2" — embark is plain and the retrigger lives at
  the terminal. Content is written to the ruled spec; the names are kept
  as-is pending Zo.
- Door-graph listener: what `BastionUnlockGraph` actually subscribes to today
  (M52/M53 lineage) — needs code verification before the bank-survival re-key
  is scoped.
- Partial-start election floor when the whole zone was already cleared (this
  beat's case): whole composite vs last-wave-only vs straight-to-bank is not
  pinned; this file assumes at least one field wave refights before the bank
  re-arms.
- Successful-bank snapshot contents today vs the ruled banked + door state —
  needs code verification.
- Dock commit seat in co-op (who holds `E` to end the party's Run) — not
  ruled.
- Dead-host buy-back vs `M49` — closed by what shipped. Every seat takes the
  same priced respawn, and `M49` reduces to the case where no seat left can pay.
- The `demo.embark_wave2` id and this file's name both still say "wave 2" and
  nothing here embarks at one. Rename is Zo's — see the first Gap.

## Rejected

The NAV skip and per-Run wave budget (superseded, Zo 2026-08-17 — not merely
deferred). Doors on terminal completions. Gift-completing a terminal on loss.
Dock as a Lost variant. Docking past an unsurvived window. Shared protocol
loadout. Host-forced EMBARK without Ready (`M72` c5).
