# M76 — The first cycle ends in a loss

<!--
Status: next (started 2026-08-18)
Update trigger: when the zone-1 shape, the loss-1 aims, or the checkpoint
ruling in Demo beats 1–2 move
Last updated: 2026-08-17 (audited against code 2026-08-18)
-->

Beats 1 + 2: zone 1 as two 1-Wave terminals with the juice teach between them,
then the first Hatch-bank as authored loss 1 — an overwhelm the no-upgrade kit
cannot clear, losing some Salvage and keeping what dripped. Cut 3 of the demo
family.

## Story

As a new player, I want my first Bastion cycle to be: wake two terminals, bet
Unbanked on Intensity, then hold the Hatch and lose it — keeping what
transferred — so that the loop unit (zone terminals, then the zone's bank) and
the cost of losing are both learned by playing them once.

## Why it matters

Play 2026-08-16 named the shape: multiple Waves, then overrun, losing *some*
(`../../Demo/1-first-terminal.md` / `2-first-hatch-bank.md`, Why this beat
exists). The in-tree `FirstTerminal` is the overturned 3-Wave T1; the ruled
zone model (Zo, 2026-08-17) replaces it. Loss 1 is the unlock spine's first
death: it funds and unlocks the wheel (M78) and the wheel key stays
`PodEjections >= 1`.

Arc on the authored-loss composition; the zone re-spec itself is loop
structure the whole game keeps. The overwhelm is composition, not script — a
`first-hatch survived` log at the no-upgrade ceiling is a tuning defect, not a
target (`../../Demo/README.md`, Tuning sequence).

## Acceptance criteria

1. Zone 1 rules while `DemoActive`: T1 starts one Wave; T2 refuses until T1's
   clear; the Hatch refuses until 2/2; then the Hatch arms. With
   `DemoActive false` the live terminals and a free Hatch are untouched.
2. The juice teach fires once, on the first Field look with Unbanked > 0; one
   `+` step spends 100 and the next Wave starts `intensity=1`; the spend
   writes an Event log line (today it is silent).
3. `DemoZoneDeepestWave[1]` is written on each field-Wave **clear** (ruled at
   cut, Zo 2026-08-17 — not on bank start); `DeepestWaveReached` is unchanged
   by the bank.
4. The first Hatch-bank plays as authored loss 1: no dump on start, drip 8/s
   with attrition, and a 0-upgrade MK-1 seat after the zone's two Waves ends
   in `RUN OVER` — leftover Unbanked wiped, dripped banked kept,
   `PodEjections == 1`.
5. `[Demo] first-hatch survived` flags any survival of this window as a
   tuning defect in the log; it is never a pass.
6. First Docked after the loss plays `demo.first_hatch_down` (cause-agnostic
   copy), wins its same-frame collision, and sets the standing; second-and-
   later losses set the state-keyed standing with no line.
7. All beat-1/beat-2 voice lines fire on their stamped triggers; the zone
   logs (`first field … waves=1`, `zone terminals complete zone=1`,
   re-keyed refuse lines) read back; no `[Dev]`.
8. Playthrough: from Play through EMBARK (M75), one full cycle — two Waves,
   one juice step, bank, loss, Docked — confirms 1–6 on screen and in the log.

## Implementation

Beat 1 and beat 2 Create / Change / Delete tables are the file list:
`FirstTerminal` zone re-spec + re-keyed refuse conditions + `DemoActive` gate;
`DemoRun` (nag timer, played-once latches, voice triggers); the juice Event
log (today it is `Log.Info` verbose-only in `IntensityJuice`, not silent); the authored-overwhelm composition (aims ~2k at start / ~500 banked at
the fall, both tunable — restamp in beat 2 when tuned); carry re-key to 2
(`CarryConvert(2)` = x1.10, `HatchBank.Mult2`, read 2026-08-18); the later-loss standing. **Delete** nothing
in tree; the two never-added flags stay never-added.

Out of scope, ruled 2026-08-18 against the Roadmap's M77 row: beat 2's Build
table also lists the survive path (door unlock on bank success, successful-bank
snapshot, Ship return). Those are **M77's**. This window is authored
unclearable, so nothing here can exercise them, and an unexercisable path
delivered as built is how an untested surface gets called working.

Checkpoint ruling applied at cut (Zo, 2026-08-17): beat 2's
write-on-bank-start rule is superseded — `DemoZoneDeepestWave` writes on
field-wave clear (beat 4's stamp). Beat 2 is edited to match in this cut.

The loss-1 pile gap (in-tree specs yield 1200 vs the ~2k aim) is zone-Wave /
award tuning inside this milestone's tune step — one lever per commit
(`../../Demo/README.md`, Tuning sequence). The ~500-banked aim is a separate
matter: 8/s x 45 s x 1.10 caps this window at 396 banked even if the Hatch
survives, so the aim needs a lever moved or restamping down (beat 2, Numbers).

## Verification

Beat 1 Verification 1–6 and beat 2 Verification 1–5 are the gate, retargeted
`FirstTerminalTests` included. Criterion 8 is the playthrough path; read
`Logs/` after — the always-on `RUN OVER` line is the loss's evidence, never
the verbose `Run ended` line (beat 2 trap).

## Traps

Beat 1 + beat 2 Traps apply verbatim. The three that cost the most if missed:
`ArmSequentialDraws` in zone 1, unlocking D1 on a failed bank, and gifting
Salvage so both wheel buys always land.

## Depends on / Unblocks

Depends: M74 (lines), M75 (EMBARK, v9). Unblocks: M77 (a bank system worth
surviving), M78 (~500 banked, `PodEjections >= 1`).
