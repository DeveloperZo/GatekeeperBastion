# 1 — First terminal

<!--
Purpose: First Bastion: explore, zone 1's two field terminals — one Wave each —
teach the Ante (risk/reward) between them, then Convert. Overrun rules live in
beat 2.
Status: demo spec
Authority: implementable
Update trigger: when the zone shape changes, when the Ante-teach timing changes,
when the DemoZones table moves
Last updated: 2026-08-25 (the zone re-spec and the DemoActive gate shipped; MaxWaves is
an absolute ceiling; the teach line says Ante; Convert in the prompts)
Depends: voice.md, 0-wake-on-a-dead-ship.md
Play: GatekeeperZo Notes/playthrough.md 2026-08-16 starting-weapon entry
-->

## Why this beat exists

Play 2026-08-16: first session should handle **multiple Waves**, then overrun at
the Hatch, losing **some** Salvage. Comparative starting weapons one-shot fodder
early. Juice is the risk/reward verb; withholding it until Escaped cannot teach
it. One Wave then convert-then-fight cannot teach the Convert as a transfer you
can lose part of.

Ruled shape (Zo, 2026-08-17): zone 1 = **two** terminals, **one** Wave per
trigger, then the zone's Convert. The Convert is locked until both terminals
are complete; surviving its timer window unlocks D1, and a failed Convert never
unlocks (beat 2 owns the window). The consecutive multi-Wave trigger debuts in
zone 2 (3 Waves) and peaks in the finale (5) — another editor's beats. The
`MaxWaves` re-key that ruling assumed never landed: `WaveDirector.MaxWaves` is
**5** and is an absolute end-Wave ceiling. Zone 1 runs Waves 1–2 and zone 2 runs
3–5, so every authored Wave sits under it and nothing needed re-keying.

The zone re-spec has landed. `FirstTerminal` is now zone 1's rules and nothing
else — `Waves` is **1**, T2 unblocks on T1's Wave clearing with no
Convert-started clause, and the Convert unblocks when **both** are done. The
authored table itself moved to `DemoZones` (zone 1 = T1, T2, one Wave per
trigger, Waves 1–2, door D1), so a later zone raises its own counts without
touching this class. Every rule is gated on `DemoActive`, so a save that has
Escaped gets live terminals and a free Hatch. What is still missing is the
voice and two logs.

## Functional storyboard

Continues from beat 0 step 6: `demo.embark_first` plays on the first Bastion
Prep frame. Standing objective arrives as **Find a field terminal**.

Copy stamped here until `DemoLines` exists. Speaker is **Game**; Assault lines
are Subtitle only ([`voice.md`](voice.md)).

### 0 — Arrive (Docked → Prep)

| | |
|---|---|
| **Goal** | Know the job: find a field terminal. |
| **Player** | Continue dismisses `demo.embark_first` (stamped in beat 0: *Quiet Hatch. Find a field terminal. Select it, then start — wake the place.*). Walk. |
| **Standing** | Find a field terminal |
| **Prompt** | — (mission drawer auto-opens on first Tethered Prep) |
| **Log** | existing `[Run] run begins at {BastionName} starting at wave 1 — …` |
| **Have** | `GameController` boot: `StartRun` then `EngageTether` (Docked → Prep). `MsgGeneral` shows `arriving at the bastion...` on Docked. Drawer auto-open in `HudController`. |
| **Build** | Do not stack: skip or replace `MsgGeneral` when `demo.embark_first` plays. Drawer may carry the standing objective (`voice.md`, optional). |

### 1 — Explore (optional nag)

| | |
|---|---|
| **Goal** | Reach the terminal. Kit south block: Hatch R1 south; T1 R2 west; T2 R3 east; both reachable without D1. |
| **Player** | Walk. 45 s of Prep with no Field terminal look-at fires the nag once. |
| **Game** | `demo.explore` · Subtitle · Local · Game. **The Bastion is dormant. Field terminals wear a T. Find one.** Auto-advance 4 s. |
| **Standing** | Find a field terminal |
| **Log** | `[Demo] line id=demo.explore mode=Subtitle scope=Local speaker=Game` |
| **Have** | Nothing — no nag timer in tree. |
| **Build** | 45 s Prep timer, once per Run, in the demo runner. |

### 2 — Select and start T1's Wave

| | |
|---|---|
| **Goal** | First Wave — one Wave per trigger in zone 1 (`MaxWaves`, Zo 2026-08-17). |
| **Player** | Look at T1: `demo.terminal_look` Lock, Continue. Juice is legal now but Unbanked is 0, so `+/-` fails. `E` arms the pick. `T` starts the terminal's single Wave. (`E` again = edit, back to unarmed.) |
| **Game** | `demo.terminal_look` · Lock · Local · Game. **A field terminal. E selects it. T starts a Wave. The Wave hunts you — the Hatch is not its target.** |
| **Standing** | Clear the Wave (1/2) |
| **Prompt** | Unarmed: `[+/-] Terminal T1 — Wave (0/1)` + stake (` Intensity 0 juice 100 unbanked 0 …bodies`) + ` [E] select`. Armed: `[T] Terminal T1 — start Wave (0/1)` + stake + ` [E] edit`. |
| **Log** | `[Demo] first field id=T1 waves=1` then `[Terminal] T1 started wave 1 intensity=0 juiced=0` |
| **Have** | `FirstTerminal.Id` forwards to `DemoZones.TerminalIdAt(1, 0)` — T1, hard-first; select/ante/arm loop in `GameController.Terminals` (`StartTerminalWave`). Anting up after arm refused: `[Run] juice refused — intensity selected`. Prompt strings live in `BastionTerminal`. `FirstTerminal.Waves` is **1** and `FirstTerminalTests` sit on the zone shape. |
| **Build** | `demo.terminal_look` line. Keep hard T1-first unless play shows T2-first confusion — one id is fewer moving parts, and T2's refuse prompt already names the rule. Restamp here if generalized. |

### 3 — T1's Wave (Assault)

| | |
|---|---|
| **Goal** | Clear 18 Defaults. MK-1 (damage **30**, in tree) one-shots a 30 HP Wave-1 Default. |
| **Player** | Fight. Terminal-mode Wave hunts the Boarder; the Hatch cannot be hurt. |
| **Game** | — (no locking line in Assault) |
| **Standing** | Clear the Wave (1/2) — drawer Assault shows wave / remaining |
| **Log** | `[Run] wave 1/5 — 18 gatekeepers, intensity=0 juiced=0 (each kill +25)` then `[Terminal] T1 wave 1/1 cleared` |
| **Have** | Targeting: Hatch targetable only in `WaveMode.HatchBank`. Kill award `floor(25 × dial)`, split across `PlayerCount` wallets, 0 during a Convert. |
| **Build** | Nothing beyond the re-spec. |

### 4 — Ante teach (Prep after T1)

| | |
|---|---|
| **Goal** | Learn the bet. Unbanked is now > 0 (450 after a full T1 clear solo dial 0). |
| **Player** | Look at **T2** — the first Field look with Unbanked > 0: `demo.juice_teach` Lock, once. Then `+`/`-` while unarmed (each step −100 Unbanked), `E`, `T` for T2's Wave. Juicing 0 is legal. |
| **Game** | `demo.juice_teach` · Lock · Local · Game. **Kills paid you Unbanked Salvage. + and - spend it on the Ante: the next Wave brings more Gatekeepers and pays more. Spending nothing is also a bet.** (shipped copy, `DemoLines`; the id keeps the code's word) |
| **Standing** | Clear the Wave (2/2) |
| **Prompt** | Stake line live: ` Intensity {n}  juice 100  unbanked {u}  carry {k}  ×{x}  {bodies}` (`RepairInteractable.AppendStake` / `BastionTerminal.AppendStake` — the two retired words are un-swept shipped strings, quoted here as they print) |
| **Log** | `[Demo] juice teach` (Build). Spend today is **silent** — success writes no Event line; refusal is `Log.Info` (verbose only). |
| **Have** | `IntensityJuice.Try`: Prep only, at trigger, 100/step, no refund on lower. Dial curve `1.00…1.50` (`TetherSiphon.Curve` — builder name). Prep HUD tip `+/- raise Intensity {n} → {bodies}`. |
| **Build** | Teach line + `[Demo] juice teach` log. One Event log on juice spend so Reads-back is answerable from the log. |

### 5 — T2's Wave (second terminal)

| | |
|---|---|
| **Goal** | Finish zone 1's terminals. T2's Wave rides the in-tree Wave-2 spec: 30 bodies, 4 Hunters, ×1.40 — `[unconfirmed]` for the new shape; the zone Wave table is retune territory under the loss-1 aims (beat 2, Gaps below). |
| **Player** | `T` on T2; clear; untimed Prep before it; optional juice, no second lecture. |
| **Game** | — |
| **Standing** | Clear the Wave (2/2) |
| **Prompt** | `(k/2)` counts up across the zone. T2 before T1's clear: refuse suffix in prompt. Hatch before 2/2: `Convert — finish the zone` (live string). |
| **Log** | `[Terminal] T2 wave 1/1 cleared`. T2 `T` before T1's clear: `[Demo] start refused — finish this terminal` (log line in tree). Hatch `T` before 2/2: `[Demo] hatch-bank refused — finish the terminal` (log lines keep the code's word — trap 8). |
| **Have** | Untimed Prep: Field clears set no follow-up timer; `ArmSequentialDraws` exists and zone 1's Field start never calls it. Refuse plumbing in `GameController.Terminals` / `GameController.HatchBank`. |
| **Build** | Re-key the refuse conditions: `OtherFieldBlocked` → T2 blocked until T1's Wave clears; `HatchBankBlocked` → clears when **both** terminals are done (was: T1 done). |

### 6 — 2/2 complete → Convert next

| | |
|---|---|
| **Goal** | Take the pile to the Hatch. Full zone clear solo dial 0: **1200** Unbanked (48 kills × 25, in-tree Wave 1+2 specs). The ruled bank-start aim is **~2k** (tunable, Zo 2026-08-17) — the gap is zone-Wave/award tuning (Gaps below), not this file's to resolve. |
| **Player** | After the 2/2 clear, look at the Hatch: `demo.after_terminal` Lock. Beat 2 owns the window. |
| **Game** | `demo.after_terminal` · Lock · Local · Game. **The terminals are done. Your Salvage is still Unbanked. Go to the Hatch and hold it — the pile transfers home while you defend. E, then T.** |
| **Standing** | Convert at the Hatch |
| **Prompt** | Hatch unblocked: `[+/-] Convert` + stake + ` [E] select` (live). |
| **Log** | `[Terminal] T2 complete — wave cleared` (pattern in tree) plus `[Demo] zone terminals complete zone=1 unbanked=<n>` (Build) |
| **Have** | `FirstTerminal.HatchBankBlocked` / `OtherFieldBlocked` plumbing (conditions re-keyed per step 5). |
| **Build** | `demo.after_terminal` line; zone-complete log. |

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 1 |
|---|---|---|---|
| Zone model (2 × 1-Wave terminals, Convert after both) | **yes** | `DemoZones` table; `FirstTerminal` forwards to it | Have |
| Waves per trigger in zone 1 | yes | `FirstTerminal.Waves` is **1**; `WavesFor` reads `DemoZones.WavesPerTrigger` | Have |
| Hatch refuse until both terminals done | yes | `FirstTerminal.HatchBankBlocked` → `DemoZones.HatchBankBlocked` → `GameController.HatchBank` | Have |
| Other-Field refuse | yes | `FirstTerminal.OtherFieldBlocked` → `GameController.Terminals`; the Convert-started clause is gone, which is what would have deadlocked T2 | Have |
| Refuse / first-field logs | yes | `[Demo] first field id=T1 waves=1`, both refuses | Have |
| `DemoActive` gate on those rules | **yes** | every `FirstTerminal` / `DemoZones` rule takes the caller's `demoActive` | Have — a save that Escaped gets live terminals and a free Hatch |
| Juice (100/step, curve, Prep-only) | yes | `IntensityJuice`, `TetherSiphon.Curve` | Have. Spend is silent — Build one Event log |
| Prompt + stake strings | yes | `BastionTerminal` | Have; `(k/1)` / `(k/2)` counts follow the re-spec |
| Untimed Prep on Field | yes | no `ArmSequentialDraws` on Field path | Have — stays for zone 1's single-Wave triggers |
| MK-1 damage 30 (lever 1) | yes | `WeaponCatalog` | Have — shipped |
| Mission drawer (Tab) | yes | auto-opens first Prep; prep/assault content | Have; standing objective optional |
| `MsgGeneral` arriving line | yes | `BastionHudView` | Build: do not stack with `demo.embark_first` |
| Voice ids / `DemoVoice` | no | [`voice.md`](voice.md) | Build |
| `[Demo] juice teach` / zone-complete logs | no | — | Build |

## The change

1. **Zone re-spec** — landed. `DemoZones` authors zone 1 as T1 and T2 at one
   Wave per trigger, Waves 1–2, door D1, and `FirstTerminal` forwards to it.
2. **Voice**: four lines above, on the triggers above. Mechanics already work mute.
3. **`DemoActive` gate** — landed. Every zone rule takes the caller's flag.
4. **Logs**: `[Demo] juice teach`, `[Demo] zone terminals complete zone=1
   unbanked=<n>`, one Event line on the Ante spend.
5. **`MsgGeneral`** vs `demo.embark_first`: one speaks, not both.

Do **not** `ArmSequentialDraws` in zone 1 — its triggers are single-Wave. Do
not hide `+/-` while Unbanked is 0 — the spend fails with its own reason.

## Create

| Path | Role |
|---|---|
| `Assets/Scripts/Game/Demo/DemoRun.cs` | Host: explore nag timer, juice-teach played-once, voice triggers |
| — | `DemoStationGate` / `DemoVoice` / flags come from beats 0 and voice.md |

`DemoHud.cs` from the earlier cut is **not needed**: `(k/2)` prompt and refuse
strings already render via `BastionTerminal`.

## Change

| File | Change |
|---|---|
| `FirstTerminal` + call sites | Nothing — the zone re-spec and the `DemoActive` gate are in `DemoZones` and this class already forwards to them |
| `GameController.Terminals` | Voice triggers on look-at / first clear; zone-complete log |
| `IntensityJuice` or its caller | Event log on spend |
| `BastionHudView` | Arriving message vs `demo.embark_first` |

## Delete

Nothing. Do not delete `AppendStake`. Do not hide `+/-`.

## Numbers

| Name | Value | Source |
|---|---|---|
| Zone 1 terminals | **2**, one Wave per trigger | Zo, 2026-08-17 |
| `MaxWaves` | **5**, an absolute end-Wave ceiling | `WaveDirector.MaxWaves`. Zone 1 is Waves 1–2 and zone 2 is 3–5, so both fit under it |
| Juice cost | 100 / step | `IntensityJuice.CostPerStep` |
| Dial curve | 1.00 … 1.50 | `TetherSiphon.Curve` |
| Zone 1 kills solo dial 0 | 48 | 18+30 (in-tree Wave 1+2 specs — `[unconfirmed]` for the new shape, retune territory) |
| Unbanked if no juice, full zone clear | **1200** | 48×25 — vs the ruled ~2k bank-start aim (tunable); see Gaps |
| One juice step before T2 | −100 Unbanked, bodies ×1.10 | curve index 1 |
| MK-1 damage | 30 | `WeaponCatalog` (lever 1 shipped) |
| Default HP W1 | 30 | `BaseEnemyHp` × 1.0 |
| Explore nag | 45 s | this spec |
| Salvage / kill | `floor(25 × dial)`, 0 in `WaveMode.HatchBank` | `RunSim` award |

Gaps: in-tree zone-1 Wave specs give 1200 Unbanked at dial 0; the ruled loss-1
aim is **~2k** Unbanked at bank start (tunable, Zo 2026-08-17). Zone
Wave/award tuning is open — record here, resolve with beat 2's overwhelm tune.

## Multiplayer

None — this beat is solo by construction (multiplayer locked until the first
wheel purchase, [`README.md`](README.md)). The host-authoritative rules
(`E`/`T`, refuse strings, `playerCount` scale) still hold for post-demo play;
nothing here needs replication work. Voice scopes are Local.

## Voice

Stamped copy is in the storyboard. Triggers:

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.explore` | Subtitle | Local | 45 s Prep, no Field look-at, once per Run | unchanged |
| `demo.terminal_look` | Lock | Local | first Field look this Run | Clear the Wave (k/2) |
| `demo.juice_teach` | Lock | Local | first Field look with Unbanked > 0 (in practice T2), once | Clear the Wave (k/2) |
| `demo.after_terminal` | Lock | Local | Hatch look-at after 2/2 (or on the 2/2 clear if already looking) | Convert at the Hatch |

Assault: no locking line.

## Driven / Shown / Reads-back

- **Driven by** `E`/`T` on T1, then T2; `+/-` in the Prep between.
- **Shown** `(k/2)`; stake line with juice cost; Unbanked falling when they juice.
- **Reads-back** `[Demo] first field id=T1 waves=1`, `[Demo] juice teach`,
  `[Terminal] Tk wave 1/1 cleared`, `[Demo] zone terminals complete zone=1`,
  and both refuse lines.

## Logs

| Event | Line | Status |
|---|---|---|
| First select | `[Demo] first field id=T1 waves=1` | in tree |
| Wave start | `[Terminal] Tk started wave n intensity=n juiced=j` | in tree |
| Clear | `[Terminal] Tk wave 1/1 cleared` | in tree |
| Juice teach | `[Demo] juice teach` | build |
| Juice spend | one Event line | build (today silent) |
| Hatch too early | `[Demo] hatch-bank refused — finish the terminal` | in tree; the condition already reads both terminals |
| T2 before T1 | `[Demo] start refused — finish this terminal` | in tree |
| 2/2 | `[Demo] zone terminals complete zone=1 unbanked=<n>` | build |

## Traps

- `ArmSequentialDraws` in zone 1 — its triggers are single-Wave. The
  consecutive unit is zone 2's and the finale's (other beats); do not pull it
  forward, it skips the juice teach.
- Restating zone 1's counts here instead of reading `DemoZones`. That table is
  the one source of truth and this beat forwards to it.
- Opening D1 on terminal completion. The door unlocks on a **survived Convert**
  (beat 2); terminals only unlock the Convert.
- Hiding `+/-` because Unbanked is 0 on the first Wave. Keep the prompt; the
  spend fails until they have a pile.
- Juicing after `E`: refused (`juice refused — intensity selected`). `E` again
  (edit) reopens the bet. Copy must not promise juice while armed.
- Dropping the `DemoActive` argument on a `FirstTerminal` or `DemoZones` call.
  It is what makes the demo a no-op after Escaped, and every rule takes it.
- Setting prefab `wavesToComplete` on every terminal. The per-zone `WavesFor`
  table scopes it; keep it that way.
- Teaching the Ante as “siphon”, “dial”, “intensity” or “multiplier.” Those are
  builder or retired names; the player-facing words are the Ante and anting up.
- Retuning MK-1 in the same commit as drip (two levers). Lever 1 already shipped.

## Depends / Unblocks

Depends: beat 0 MK-1 grant; v9 flags for the `DemoActive` gate. Unblocks: beat 2
with Unbanked from the zone's two Waves (the Convert still refuses `unbanked=0`
if they juiced everything and got no kills).

## Verification

1. PlayMode: Hatch `T` starts zone 1's whole trigger (both Waves, back to back);
   `T` on T2 while it runs shuts the Wave off, completes the zone and banks the
   pile. `FirstTerminalTests` and `HatchStartLinePlayModeTests` sit on the zone
   shape and the start line respectively.
2. `T` on T2 during Prep is REFUSED — there is exactly one start line and it is
   the Hatch. The refusal sits on `CanSelectTerminal`, so `TryNetStartTerminal`
   (co-op) is covered too. This is the 2026-08-25 playtest defect: both ends of
   the lane offered a start and the demo voice pointed at the wrong one.
3. `+/-` one step spends 100 and the trigger starts `intensity=1`; spend Event
   line present.
4. Duo: zone 1's first Wave `enemyCount=36` at dial 0.
5. With `DemoActive` false: live terminals, Hatch unblocked, no demo lines.
6. No `[Dev]`.

## Rejected

Juice withheld until Escaped (2026-08-16 stamp, overturned). One Wave then
Hatch (a single terminal straight to the bank). Convert-then-fight.
Auto-starting the first Wave. Three-Wave first terminal (2026-08-16 shape —
overturned by the 2026-08-17 zone ruling and gone from the tree; the
consecutive multi-Wave trigger lives in zone 2 and the finale). Five-Wave first terminal (2026-08-13 run was
6 min) — 5-consecutive is the finale terminal's unit (beats 6–7), not the
opener's. Any-first-id `FirstTerminal` — hard T1 shipped simpler; reopen only
on play data showing T2-first confusion. Companion.
