# 5 — Repeat until the Control Room

<!--
Purpose: Same unit through the kit’s three door sets: terminal → Wave → Hatch-bank
→ next doors. Later Hatch-banks may be survived. No new verb.
Status: demo spec
Authority: implementable
Update trigger: when the kit graph changes, when Hatch-bank-first is lifted for
later doors
Last updated: 2026-08-16
Depends: voice.md, 4-embark-at-wave-2.md
-->

## What the player sees

Same unit: next terminal → wave(s) → Hatch-bank → next doors. Later Hatch-banks
in this demo may be survived. No new verb. Hunters appear when that Wave’s roster
says so, not as a lecture.

Control Room door is D3 (`OpensControlRoom`). Walking through it is beat 6.

## Why this beat exists

The kit already **is** this unit if Hatch-bank sits between completing a door’s
terminals and walking the new rooms. Live play can chain T1+T2 without a
Hatch-bank and still open D1. Beat 1 refused the second terminal until Hatch-bank.
This beat keeps that refuse until the **current door’s required terminals are
done**, then allows the next room’s terminals after the Hatch-bank **starts**
(checkpoint) or after it is survived — spec below.

## In tree today

Sketch (`bastion-defense-layout.txt`):

| Door | Room face | Required terminals | Behind it |
|---|---|---|---|
| D1 | R1 N | T1 and T2 | Hub R6, spine to R9 |
| D2 | R9 N | T3 | Staging R10, T4/T5 |
| D3 | R15 S | T4 and T5 | Control Room R15 (`control`) |

Rooms: T1 R2, T2 R3, Hatch R1 south (z = −61). T3 R9 (reachable before D2; D2
sits north of R9 so T3 is not behind its own gate). T4 R11, T5 R12. Master `M`
in R15. Spawns S1/S2 live from the start; S3–S6 unseal when D2 opens.

`BastionUnlockGraph`: AND of required terminal ids; `OpensControlRoom` last.
Field `wavesToComplete` default 1. Mid-Run Hatch-bank slots still 3 (beat 2
spent 1 on the designed loss, 2 left unless a survive refunds — it does not).

Hunters per wave index: `{0,4,6,10,12}`. Thieves `{0,0,2,4,7}`. Wave 3
(`NextWave(2)`) is the first Thief row.

No sequencer copy. Juice still live (demo hides until Escaped).

## The change

### Repeating unit (host `DemoRun`, `DemoActive`)

After beat 2, `DemoRun` is not only “first field.” Generalize:

1. **Hatch-bank first for the current door.** While a door’s AND is incomplete,
   `T` on a Field terminal that is **not** needed for the current door, or a
   second terminal of that door **before** a Hatch-bank has been **started**
   this cycle, follows the table.

Define a **cycle** as: Docked→EMBARK until next Docked, or from Hatch-bank
start until next Hatch-bank start.

Simpler rule that matches the demanded sentence (“another terminal unlocks the
next doors — Hatch-bank first”):

**Until the Control Room door is open, they may complete at most one Field
terminal Interact per Hatch-bank start.**

- After a Field clear, other Field `T` refuse: `Hatch-bank first` (same as beat 1).
- Hatch-bank start (beat 2 checkpoint) clears the refuse for the rest of the
  Run (they may then complete the AND partner, e.g. T2, **after** they have
  started or survived that Hatch-bank).

Wait: if they must Hatch-bank after T1 before T2, D1 never opens in the same
Run as T1 unless they Hatch-bank then walk to T2 **in the same Run**. Slots: 3
per Run, beat 2 spent 1. They can Hatch-bank after T1 (slot 2), then T2, then
Hatch-bank (slot 3), then D1 opens.

Alternatively T1 Hatch-bank **loss** returns to Ship; next Run T2 at Wave 2,
Hatch-bank, D1 opens. That is multiple Runs, which the demo wants.

Both are legal. Spec:

| Situation | Allowed Field `T` | Hatch-bank |
|---|---|---|
| No Field completed this Run yet | Any one incomplete Field that is reachable | After that clear |
| One Field completed this Run, no Hatch-bank started this Run | None | Required |
| Hatch-bank started this Run | Any remaining reachable incomplete Field | After that clear, again |

Reachable = door to that room is open. T3 is reachable before D2 (kit). They
could T3 as their “one terminal” before D1 is open. **Do not allow that.**

**Allowed terminals** = incomplete Field terminals whose room is reachable
**and** that are required by the nearest still-shut door (D1, else D2, else D3).

| Shut door | Allowed ids |
|---|---|
| D1 | T1, T2 |
| D2 | T3 |
| D3 | T4, T5 |
| None (D3 open) | none Field; Master is beat 6 |

T3 is physically reachable before D1 opens (open passage R6–R9). Prompt on T3
while D1 is shut: `Terminal T3 — doors behind you first`. `T` refuse
`[Demo] start refused — door D1 first`.

### Hatch-banks after the first

Live drip + dump. Convert-then-fight **off**. Hunter-add spec **off**. Any-seat
down **off** (guest `M57`, host `M49`). Designed loss is not repeated.

If they die on a later Hatch-bank: Unbanked leftover wiped, dripped stays,
`DemoNavUnlockWave` already rose on **start**, `DemoBankCapWave` rose on start.

If they die on a **field** Wave: NavUnlock does **not** rise. Example: last
Hatch-bank started after W2 (unlock 3); they clear W3 field and die before
Hatch-bank → still unlock 3, not 4.

### Copy

- `demo.repeat_unit` Party Lock Prep once per Run after the first, on first
  Prep frame (not every terminal).
- `demo.door_d1` / `d2` / `d3` Party, on door `SetOpen(true)`: Prep Lock if Prep,
  Subtitle if somehow Assault (doors should open in Prep after complete).

Mission drawer stays chrome. Juice still withheld (`DemoActive`). FAB still dark.

### Wave picker across repeats

`DemoNavUnlockWave` climbs on each Hatch-bank start: after a Wave 2 field +
Hatch start → unlock 3, bankCap 2, etc. Cap `MaxWaves` 5. Skip remains: EMBARK
at 4 launches Wave 4 only, not 1..4.

Slots reset to 3 each `StartRun`.

### Protocol

Already claimable once CREW is powered (beat 4). No extra lecture when Hunters
appear. Wave 2 already showed 4 Hunters.

## Create

| Path | Role |
|---|---|
| `DemoRun.AllowedField(id, graph, doorsOpen)` | Nearest shut door’s required ids, Hatch-bank-first per Run |
| Tests | T3 refuse while D1 shut; second Field `T` refuse before Hatch start; after Hatch start T2 allowed |

## Change

| File | Change |
|---|---|
| `GameController` start Wave | Call `DemoRun` refuse before `TriggerDraw` |
| `BastionTerminal.Prompt` | Door-behind / Hatch-bank-first strings |
| `BastionDoor.SetOpen` | Hook `DemoVoice` door ids |
| `MapStation` | Already using `DemoNavUnlockWave` from beat 4 |

Do not rebuild rooms. Do not emit a second kit. `SceneBuilder` / sketch stay
the layout.

## Delete

Nothing.

## Numbers

| Door | Terminals | First Wave they likely meet it | Hunter row at that index |
|---|---|---|---|
| D1 | T1, T2 | W1 then W2 (beats 1–4) | 0 then 4 |
| D2 | T3 | W3 if they skip to unlock | 6 Hunters, **2 Thieves** (`RamPerWave[2]`) |
| D3 | T4, T5 | W4–W5 | 10 then 12 Hunters, 4 then 7 Thieves |

Body counts solo dial 0: 18 / 30 / 42 / 54 / 66 (`18+12*i`). HP mul
1.00 / 1.40 / 1.80 / 2.20 / 2.60.

Hatch-bank slots per Run: 3. A Run that does T2 + Hatch + T3 + Hatch + T4
would spend 3 and then cannot Hatch-bank T5’s pile until Master or a new Run.
**Spec:** they may return to Ship only by Lost or Escaped — no bail-out
(`Docs/README.md`). So a single Run cannot bank infinitely. After 3 Hatch-banks
they must fight without convert or die. Demo copy does not explain slots; live
HUD already should. If the HUD hides remaining slots, that is a live gap, not
this beat’s new widget.

Door-set count: **3**. Not unknown.

## Multiplayer

| Event | Authority |
|---|---|
| Terminal start | Host sim, any seat at the console (live) |
| Allowed-id refuse | Host, all seats see prompt |
| Door open | Host layout, replicated |
| Wave skip | Lead NAV |
| Scale | `playerCount` on every `NextWave` |
| Guest down | `M57` wait; host continues. Converted drip still on guest wallet |
| Voice door lines | Party |

Late joiner on Ship between Runs: they share lead’s start wave, own loadout,
own `DemoNavUnlockWave` unused for the Run. Their BankCap is their own deepest
Hatch-bank starts. A brand-new save joining a Wave-4 party: BankCap(1)=450.

## Voice

`demo.repeat_unit` once per Run after the first demo Run.
`demo.door_d1` `demo.door_d2` `demo.door_d3` on open.

No Hunter tutorial line. No Thief tutorial line.

## Driven / Shown / Reads-back

- **Driven by** `E`/`T` on allowed terminals and Hatch.
- **Shown** D1/D2/D3 open when AND complete; refuse copy on T3 behind D1.
- **Reads-back** `[Demo] start refused — Hatch-bank first`,
  `[Demo] start refused — door D1 first`,
  `[Demo] door open D1`, existing graph logs if any.

## Logs

| Event | Line |
|---|---|
| Refuse Hatch-first | `[Demo] start refused — Hatch-bank first` |
| Refuse wrong door | `[Demo] start refused — door D1 first` |
| Door | `[Demo] door open D1` (and existing `[Terminal] T2 opened D1` if that path logs) |
| Hatch start checkpoint | same as beat 2 line with new wave numbers |

## Traps

- T3 physically reachable before D1. Prompt+refuse is load-bearing.
- Completing T1+T2 without Hatch-bank by using `wavesToComplete` tricks. Keep
  one Interact per Hatch-bank start.
- Restoring completed T1 from a **lost** Run (beat 4 trap) would skip a
  terminal. Keep success-only snapshot.
- Spending slot 3 then standing on D3 with Unbanked and no Hatch-bank: they
  fight Master with a pile or die. Do not add a fourth slot for the demo.
- Juice leaking back on later terminals. `DemoActive` until Escaped.
- Rebuilding R15 as a new room type. Kit ControlRoom already exists.
- Teaching FAB because Hatch Armor 0 makes later Hatch-banks harsh. Withheld.

## Depends / Unblocks

Depends: beats 1–4 gates. Unblocks: beat 6 when D3 is open and they enter R15.

## Verification

1. EditMode graph: D1 AND T1+T2; T3 refuse helper while D1 shut.
2. PlayMode (or host log): after T1 clear, T2 `T` refused until Hatch-bank
   start; after that start, T2 `T` allowed; D1 still shut until T2 complete.
3. T3 `T` while D1 shut refused even if standing in R9.
4. Hatch-bank start at field Wave 3 logs `navUnlock=4 bankCapWave=3`.
5. Die on field Wave 3 without Hatch start: navUnlock unchanged.
6. Loopback: door open visible both seats; guest down on a later Hatch-bank
   does not end host Run.
7. No `[Dev]`.

## Rejected

A new corridor type. Auto-opening D1 after first Hatch-bank. Hunter lecture.
Fourth Hatch-bank slot. Siphon. Second destination.
