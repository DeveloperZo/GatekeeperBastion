# 2 — First Convert (authored loss 1, lose some Salvage)

<!--
Purpose: First Convert — zone 1's Convert after its two 1-Wave terminals.
Authored loss 1: overwhelm vs the no-upgrade kit ceiling. Live drip. Overrun
wipes remaining Unbanked; dripped stays. Survived banks (later zones) unlock
the zone's door and allow Ship return. No convert-then-fight.
Status: demo spec
Authority: implementable
Update trigger: when DripPerSecond changes, when the loss-1 overwhelm aims
move, when the ship-return / respawn rules change
Last updated: 2026-09-01 (M110 constant-drift pass: the door's HP is re-read from the Sim
constants. Prior: 2026-08-25 — Convert in player-facing strings; Carry3ExtraThief; slots are
gone from the tree; wheel key is HatchConversions; respawn rule read from Respawns.cs)
Depends: voice.md, 1-first-terminal.md
Play: GatekeeperZo Notes/playthrough.md 2026-08-16 starting-weapon entry
-->

## Why this beat exists

Play 2026-08-16: handle multiple Waves, then overrun defending the Hatch, lose
**some** Salvage. Convert-then-fight dumped the pile before the fight, so a loss
lost nothing. Drip at 20/s emptied a pile too fast for the wipe to read.

Ruled (Zo, 2026-08-17): this window is **authored loss 1** — an overwhelm the
no-upgrade kit cannot clear. Aim: **~2k** Unbanked at start, **~500** banked
when the Hatch falls (both tunable). Surviving a bank's timer window unlocks
the zone's door (D1 here); a failed bank never unlocks. The loss funds and
unlocks the wheel (beat 3). Zone 2's banked Wave is the loss that sells the
Primary (`M79`; may be this save's first or second ejection).

The base mechanics are in tree: drip is **8/s** (`HatchBank.DripPerSecond` —
lever 2 shipped), no dump on start, survive dumps, `EndRun` wipes the remainder
and keeps what dripped. The in-tree carry pieces (`Carry3ExtraThief`,
`CarryConvert(3)` ×1.25) are keyed to the old 3-Wave shape — zone 1 now
carries **2** field Waves (`CarryConvert(2)` = **x1.10**). What this beat
builds is voice, the authored-overwhelm tune, the survive path (door unlock +
Ship return), and two logs. The per-Boarding slot budget is gone from the tree
and takes its arithmetic with it.

## Functional storyboard

Continues from beat 1 step 6: `demo.after_terminal` played on Hatch look-at.
Standing arrives as **Convert at the Hatch**.

### 0 — Arm the Hatch (Prep)

| | |
|---|---|
| **Goal** | Know the transfer and the stake before starting it. |
| **Player** | At the Hatch: `E` arms. `demo.hatch_bank_teach` Lock, Continue. `T` starts. (`E` again = edit.) |
| **Game** | `demo.hatch_bank_teach` · Lock · Local · Game. **Hold the Hatch and the pile transfers home while you defend. If the Hatch falls, whatever has not moved is gone. T when ready.** |
| **Standing** | Start the Convert |
| **Prompt** | Unarmed: `[+/-] Convert` + stake + ` [E] select`. Armed: `[T] Convert` + stake + ` [E] edit`. Before 2/2 it reads `Convert — finish the zone`. The stake `RepairInteractable.AppendStake` writes still prints `Intensity <n>  juice <n>  unbanked <n>  carry <n>  ×<x>  <bodies>` — those two words are un-swept shipped strings, not this file being stale. |
| **Log** | existing `[Hatch] hatch-bank started wave … carry=<k> convert=<x> unbanked=<n> intensity=<n> juiced=<n>` (in-tree; log lines keep the code's words — trap 8. There is no `slots=` field: the budget is gone) |
| **Have** | Start gate: host, Tethered+Prep, armed, Unbanked > 0, zone terminals complete (`GameController.HatchBank` — condition re-keyed in beat 1). `TriggerDraw(HatchBank)`: snapshot carry, timer 45 s, **no dump**. Prompts in `RepairInteractable`. |
| **Build** | Teach line. Checkpoint superseded at cut (Zo, 2026-08-17): the composite retrigger reads per-zone `DemoZoneDeepestWave`, written on each field-Wave **clear** (beat 1's clears; beat 4's stamp) — **nothing is written on this start**, and `DemoNavUnlockWave` / `DemoBankCapWave` are never added. Guests, when co-op exists from beat 4 on, `SetDeepest` from the same rule. Do **not** write `DeepestWaveReached` here. |

### 1 — The window (Assault, 45 s)

| | |
|---|---|
| **Goal** | Hold. Unbanked drips into banked at 8/s × convert while they defend. This window is authored so the no-upgrade kit **cannot** hold it to the end. |
| **Player** | Fight at the door. Defaults and Thieves walk to the Hatch; Hunters still hunt the Boarder. |
| **Game** | `demo.hatch_fight` · Subtitle · Local · Game. **The Wave wants the Hatch now. Hold it — the pile is moving.** Auto-advance 4 s. Fight input stays live. |
| **Standing** | Hold the Hatch |
| **Prompt / HUD** | `MsgPlayer` `HATCH UNDER ATTACK` on any Hatch HP drop (live). Unbanked falls, banked rises on Score. |
| **Log** | drip credits per wallet (live); `[Gatekeepers] gatekeeper reached the hatch — attacking home` |
| **Have** | Window rules: timer end = clear; enemy wipe and Unbanked 0 do **not** end the window early. Hatch damage also **attrits Unbanked 1:1** (floored) on top of HP loss. Hatch 600 HP at Armor 0 (`ShipSystems.HatchMaxHp`); Defaults 4 / Thieves 20 per 0.7 s swing; Hunters 0. Kill award is **0** in `WaveMode.HatchBank`. In-tree composition (carry-3 → 43 bodies, 6 Hunters, 3 Thieves, ×1.25) is the **old shape**. |
| **Build** | The **authored overwhelm** (Zo, 2026-08-17): composition tuned so a 0-upgrade MK-1 seat cannot clear the window and the Hatch falls with ~500 banked from the ~2k pile (aims tunable). Numbers are restamped here when tuned — not invented now. |

Survive path (the timer ends with Hatch > 0 — later Converts; a defect on this
one): the Convert dumps, the zone's **door unlocks**, and the **successful-bank
snapshot** persists banked + door state. **Ship return** (Zo, 2026-08-17):
after a survived window, held interact at the Hatch — available until the next
Wave is triggered — ends the Boarding **Docked**, not Lost. That path shipped
(`RunSim.Dock.cs`, `DockOffered`, cause `docked`); on **this** window a survive
is still a tuning defect (`[Demo] first-hatch survived` flags it). It no longer
needs to be walled off to protect the shop: the 2026-08-18 re-key opens the
wheel on `HatchConversions >= 1`, so a Docked return and a Hatch breach unlock
it alike.

### 2 — Overrun (authored, not scripted)

| | |
|---|---|
| **Goal** | Lose **some**: leftover Unbanked wipes when the Boarding ends, dripped Salvage is already aboard. |
| **Player** | The Hatch falls. (A seat going down no longer ends the Boarding on its own: `RunSim.TryRespawn` sells it back for `Respawns.CostFor` — **150** × (respawns taken this Boarding + 1) — out of the Unbanked that seat is carrying, and the remainder survives with it. A Boarder holding the price stands up instead. This beat therefore lands on the **Hatch**, which has no such offer at any price. The other way in is a seat carrying less than the price, which on this window means one that has already dripped most of its pile home.) Hatch cause: lose screen cause **HATCH DESTROYED**, framing *thrown off the Bastion*, converted / repair spent / net / banked total / DEEPEST WAVE lines. `ENTER` returns. |
| **Game** | — (end screen is the reader) |
| **Standing** | — |
| **Log** | `[Hatch] HATCH BREACHED — the run is over` then always-on `[Run] RUN OVER after …s … banked now …`. (`[RunSim] Run ended (hatch\|player)` is verbose-only — do not gate verification on it. Cause tokens are `hatch` / `player`, not `down`.) |
| **Have** | `EndRun("hatch")` → wipe remaining Unbanked, keep credited banked, `PodEjections++`, phase Lost. `DeepestWaveReached` is **2** (rose on the field clears; a lost Convert does not reset it). |
| **Build** | Nothing mechanical. Do not call `Die()`. The overwhelm tune (step 1) owns hitting the ~500-banked aim. |

### 3 — Docked after the loss

| | |
|---|---|
| **Goal** | The loss reads as “some came home.” Hand off to beat 3. |
| **Player** | Dismiss end screen; Ship loads. |
| **Game** | `demo.first_hatch_down` · Lock · Local · Game. **The Boarding ended. What dripped home is banked aboard. The rest is gone. Spend it at the ARMORY.** (Copy re-stamped cause-agnostic, Zo 2026-08-17 — the trigger also fires on a host-down loss, so the copy no longer claims the Hatch fell.) |
| **Standing** | Spend Salvage at the ARMORY |
| **Log** | existing `[Ship] aboard the ship — <n> banked, 1 run(s) lost, 0 cleared` |
| **Have** | End-screen persist + `GameSession.Save()`; return log `returning to the ship — <n> banked`. |
| **Build** | The line. Trigger: first Docked after this save's first Lost Run (any cause). Same-frame rule: if any other Lock line lands on this Docked frame, `demo.first_hatch_down` plays first and the other queues on its Continue (one ordering rule, `voice.md`, Zo 2026-08-17). Second-and-later losses (Zo, 2026-08-17): no line, but **set a standing objective on Docked** — same state key as beat 3's exit standing: **Spend Salvage at the ARMORY** while buyable upgrades remain, else **EMBARK from the NAV MAP**. This Docked also writes `DemoCrewMenuUnlocked` (re-key, README / beat 3). |

## Have vs build (inventory)

| Piece | Exists? | Where | Beat 2 |
|---|---|---|---|
| Drip 8/s | **yes — lever 2 shipped** | `HatchBank.DripPerSecond` | Have |
| No dump on start / dump on survive / wipe on EndRun | yes | `RunSim` draw, clear, `EndRun` | Have |
| Slots per Boarding | **gone** | removed from `HatchBank` and `RunSim` | None — Converting carries no per-Boarding budget, so no arithmetic here depends on one |
| Carry pieces (`Carry3ExtraThief`, `CarryConvert`) | yes — old shape | `HatchBank` | Re-key to carry 2 (`CarryConvert(2)` = **x1.10**); the loss-1 composition is the authored overwhelm, restamped when tuned |
| Authored overwhelm composition | **no** | — | Build (tunable; aims ~2k start / ~500 banked at fall) |
| Targeting split (Hatch vs Hunter) | yes | Sim targeting, `WaveMode.HatchBank` only | Have |
| Attrition on Hatch damage | yes | Unbanked −1 per damage point | Have |
| Start log with carry/convert/unbanked | yes | `[Hatch] hatch-bank started …` | Have — spec's `[Demo] hatch-bank start` is redundant, dropped |
| Re-keyed checkpoint fields | no | v9 (`README.md` Shared flags) | Closed at cut (Zo, 2026-08-17): per-zone `DemoZoneDeepestWave`, written on field-Wave clear (beat 1), not here |
| Door unlock on bank success (D1) | no | — | Build with the bank system (beat 5 consumes) |
| Ship return after a survived bank | no | — | Build with the bank system (held interact at the Hatch until the next Wave trigger → Docked, not Lost; snapshot persists). Never fires on this authored window |
| Buy-back | **shipped, and not window-gated** | `Respawns.cs`, `RunSim.Respawns.cs`, `GameController.Down.cs` | Have — see Multiplayer below |
| Voice ids | no | [`voice.md`](voice.md) | Build |
| Designed-overrun scripting | — | — | None. Composition is the design |

## The change

1. **Voice**: three lines above (`demo.first_hatch_down` copy cause-agnostic).
2. **Checkpoint** — closed at cut (Zo, 2026-08-17): per-zone
   `DemoZoneDeepestWave` writes on field-Wave clear (beat 1), nothing on bank
   start. Beat 4 consumes it.
3. **Authored overwhelm** tune to the ~2k / ~500 aims (tunable; restamp here).
4. **Survive path**: door unlock on bank success + successful-bank snapshot +
   Ship return (held interact until the next Wave trigger → Docked, not Lost).
5. **Later-loss standing** on Docked (state-keyed with beat 3's exit standing).
6. Drip and wipe stay as shipped. There are no slots to keep.

## Delete

`DemoFirstHatchConvertDone`, `DemoFirstHatchSpecUsed` (never added, do not add).
Any-seat-down override (not in tree; stays out).

## Numbers

| Name | Value | Source |
|---|---|---|
| Drip | **8**/s | `HatchBank.DripPerSecond` (in tree) |
| Window | 45 s | `HatchBank.DurationSeconds` |
| Carry | 2 | zone 1's two field Waves (was 3 — old shape) |
| Convert mul | **x1.10** | `HatchBank.CarryConvert(2)` = `Mult2`, read 2026-08-18; the shipped ×1.25 was `CarryConvert(3)` |
| Window composition | authored overwhelm — unstamped, tunable | Zo, 2026-08-17; restamp when tuned |
| Extra Thieves at carry 2 | **0** | `HatchBank.ExtraThief` fires only at carry 3, so the zone re-spec drops the extra body the old shape had; the authored overwhelm has to put it back on purpose |
| Unbanked at start (aim) | **~2k**, tunable | Zo, 2026-08-17. In-tree zone Waves give 1200 (beat 1 Gaps) |
| Banked when the Hatch falls (aim) | **~500**, tunable | Zo, 2026-08-17 |
| Full ~2k pile at 8/s | ~250 s >> 45 s window | arithmetic on the aim |
| **Ceiling on banked from this window** | **396** | 8/s x 45 s = 360 taken, x1.10 convert — and that is the survive-the-whole-window ceiling, which this authored loss never reaches. The ~500 aim is unreachable without moving drip, the window, or the convert table; beat 3 already degrades to one buy at 300 |
| Hatch HP | 600 at Armor 0 (`ShipSystems.HatchMaxHp`); dps vs Hatch is the overwhelm tune `[unconfirmed]` | this spec |
| Two Secondary buys | 150+300 = 450 — the ~500 aim funds both (tunable) | Zo, 2026-08-17; beat 3 |
| Respawn cost | **150** × (respawns taken this Boarding + 1) | `Respawns.BaseCost` / `Respawns.CostFor`. The counter is the Boarding's, not the seat's |

## Multiplayer

None — this beat is solo by construction (multiplayer locked until the first
wheel purchase, [`README.md`](README.md)). The buy-back rule recorded here for
beat 4+ has since shipped, and in a different shape than the 2026-08-17 stamp:
any dead seat is offered a respawn for as long as the Boarding is Tethered,
with no Convert-window gate, priced at **150** × (respawns taken this Boarding
+ 1) out of that seat's **own Unbanked**. Nothing else can pay. The dead-host
question closed with it — the host seat takes the same offer at the DOWN
overlay. Per-zone `DemoZoneDeepestWave` still caps guests correctly on a later
co-op Boarding. Voice scopes are Local.

## Voice

| Id | Mode | Scope | Trigger | Standing after |
|---|---|---|---|---|
| `demo.hatch_bank_teach` | Lock | Local | Hatch `E` (armed) before first `T` | Start the Convert |
| `demo.hatch_fight` | Subtitle | Local | window start | Hold the Hatch |
| `demo.first_hatch_down` | Lock | Local | first Docked after this save's first Lost (any cause); wins the same-frame Docked collision (`voice.md`) | Spend Salvage at the ARMORY |

Do not say convert-then-fight. No siphon. Copy must not name a cause the
trigger cannot guarantee — and with the priced respawn in the tree it cannot
name a death either, since a seat that could pay did not have one.

## Driven / Shown / Reads-back

- **Driven by** Hatch `E` then `T`.
- **Shown** Unbanked falling during the window; Hatch HP; banked rising; lose
  screen with converted / net / banked total.
- **Reads-back** `[Hatch] hatch-bank started …` (in tree), `[Run] RUN OVER …`
  (in tree, always-on), aboard-ship line.

## Logs

| Event | Line | Status |
|---|---|---|
| Start | `[Hatch] hatch-bank started wave … carry=<k> convert=<x> unbanked=<n> intensity=<n> juiced=<n>` | in tree (values follow the re-spec; no `slots=` field) |
| Checkpoint | — dropped at cut (Zo, 2026-08-17): `DemoZoneDeepestWave` writes on field-Wave clear (beat 1), not on bank start | closed |
| Breach | `[Hatch] HATCH BREACHED — the run is over` | in tree |
| End | `[Run] RUN OVER after …s … banked now …` | in tree (always-on) |
| Survive | `[Demo] first-hatch survived hatchHp=<n>` | build — on this window it flags a **tuning defect** |
| Ship return | `[Demo] ship return — run ends docked` | build (later banks) |

## Traps

- Dump-on-start sneaking back “so both buys always land.” The ~500 aim is drip
  through the window (tunable). Do not gift Salvage.
- Gating a test on `Run ended (hatch|down)`: the live line is verbose-only and
  the token is `player`, not `down`. Assert on `RUN OVER` or sim state.
- Raising `DeepestWaveReached` on this start. It is already **2** from the
  field clears; leave it.
- Tuning the overwhelm so the seat dies before any drip lands — the loss must
  read “some came home” (~500 aim).
- `DurationSeconds` cut instead of composition/drip (wrong lever unless a log
  shows they always last the whole 45 s against an undamaged Hatch).
- Scripted `Die()`.
- Forgetting attrition: Hatch damage burns Unbanked 1:1 on top of the drip, so
  a mauled Hatch shrinks the pile even before the wipe.
- Unlocking D1 on a failed Convert. Failed Converts never unlock.
- Retuning the window because a Docked return "skips loss 1". It does not skip
  anything the shop needs: the wheel key is `HatchConversions >= 1`. If a
  0-upgrade clear shows up in a log, the overwhelm is the wrong strength — fix
  that, and leave the wheel key alone.
- First-loss copy that names the Hatch on a cause-agnostic trigger (fixed
  2026-08-17 — keep it cause-agnostic).

## Depends / Unblocks

Depends: beat 1 zone pile; the per-zone `DemoZoneDeepestWave` field. Unblocks:
beat 3 with **some** banked (~500 aim) and `HatchConversions >= 1`; beat 4's
composite retrigger reads the per-zone deepest.

## Verification

1. EditMode: Convert start with the zone pile, no dump; after 10 s of ticks
   Unbanked −80, banked +floor(80 × `CarryConvert(2)`) (`needs-check`);
   `EndRun` wipes remainder, keeps banked.
2. EditMode: the bank start writes no checkpoint; `DemoZoneDeepestWave[1]`
   already reads 2 from the field clears (beat 1's write);
   `DeepestWaveReached` unchanged at 2.
3. PlayMode: 0-upgrade MK-1 after the zone's two field Waves: the window ends
   in loss — `RUN OVER` present. A `first-hatch survived` log is a tuning
   defect, not a pass.
4. PlayMode (later banks, once the survive path is built): survived window →
   held interact at the Hatch before the next Wave trigger ends the Run
   **Docked**; banked + door state persist via the snapshot. Not closable on
   this authored window.
5. No `[Dev]`.

## Rejected

Convert-then-fight (overturned 2026-08-16). Hunter-add spec (player-kill as
the loss driver). Any-seat-down override. Gifting Salvage so two wheel buys
always land. Scripted kill. A `[Demo]` start log duplicating the live
`[Hatch]` line. NAV wave-skip checkpoint (`navUnlock=4` — dead 2026-08-17).
