# M78 — After the first loss

<!--
Status: later
Update trigger: when the ladder, MP gate, exit-standing key, CREW re-key, or
retrigger spec in Demo beats 3–4 moves
Last updated: 2026-08-17
-->

Beats 3 + 4 (minus the run-structure piece, which is M77): the first ARMORY
panel with the two-buy ladder rung, multiplayer unlocking on the first
purchase, the state-keyed exit standing, the CREW menu + Default claim on the
re-keyed unlock, plain EMBARK, and the composite retrigger at the zone's
deepest terminal. Cut 5 of the demo family. One milestone because without the
retrigger a post-loss EMBARK lands in a from-scratch chain (beat 3's stated
reason).

## Story

As a player back from my first loss, I want to spend what dripped home on the
wheel, claim my protocol, and embark straight back to a terminal that
remembers the zone — so that a loss reads as progress spent and a fight
resumed, not a restart.

## Why it matters

The wheel unlocks on first loss with ten tiers and no cap; this caps it at
ladder rung 1 — 2 Secondary buys — and makes the first purchase the
multiplayer key (`../../Demo/3-ship-after-first-loss.md`). The NAV skip is
dead; refight-avoidance lives at the terminal as the composite retrigger with
partial start electable (`../../Demo/4-embark-at-wave-2.md`). Retrigger
election reads per-zone `DemoZoneDeepestWave` (ruled at cut, Zo 2026-08-17 —
beat 4's stamp; README corrected in this cut).

Arc on the authored ladder and lines; loop on the retrigger — every post-loss
run in the shipped game resumes this way.

## Acceptance criteria

1. After a loss, ARMORY Down opens the panel (grant path only while
   `!UpgradesUnlocked`); two wheel buys succeed at banked ≥ 450; a third
   refuses with the named cap log and no `SpendSalvage`; at banked 300 the
   second shows the salvage-short refuse instead.
2. The first successful wheel buy decrements `DemoWheelBuysLeft`, fires
   `demo.mp_unlocked` once per save, and F10/F11 + lobby start passing;
   before it they refuse with the locked log.
3. Cost-0 SEC-D neither counts against the cap, nor unlocks multiplayer, nor
   holds the exit standing open.
4. The exit standing is state-keyed: it flips to **EMBARK from the NAV MAP**
   when no buyable upgrades remain — buys-left 0, or banked below the next
   cost — evaluated on Docked and after every buy / refuse / spend, with the
   flip logged. No third-buy click is required to flip it.
5. `DemoCrewMenuUnlocked` writes on the first Docked after first loss; CREW
   opens its panel from then on (heal folded in, never dropped); the claim
   path is the in-tree `M70` flow — select, sheet, `BuyProtocol` at 0 — and
   Default is never auto-equipped.
6. While `DemoActive`, NAV shows plain EMBARK — no picker, no wave number,
   no `START WAVE` line; lead commits, guests Ready, 5 s countdown
   (`M72` rules untouched); `[Demo] embark plain` logs.
7. The zone's deepest terminal offers the retrigger when the zone has cleared
   Waves and no surviving bank: default composite (all zone Waves,
   consecutively, under the re-keyed per-trigger `MaxWaves`), partial start
   electable at or below `DemoZoneDeepestWave`, both logged with
   `mode=composite|partial`; the bank re-arms when the zone is complete.
8. Live tests stay green for `DemoActive false` — picker, chain, and
   `DeepStartTests` untouched for the campaign.
9. Playthrough: from the loss-1 Docked — two buys, claim, plain EMBARK,
   composite retrigger, zone 1 cleared, bank survived, D1 open (M77's
   machinery). Confirms 1–7 on screen.

## Implementation

Beats 3 and 4 Create / Change / Delete tables are the file list, minus the
rows M77 owns (door keying, `EndRun("docked")`, dock interact, snapshot).
The dead NAV skip is a Delete-table entry in beat 4: never build `NavMax`,
picker overrides, or the chain-off branch. `MaxWaves` re-key needs its
reader sweep first (`needs code verification`, beat 4) — read every current
reader of `MaxWaves` / `ClampStartWave` before retiring the per-Run clamp.

Voice per the two beats' tables; the beat-4 copy marked *draft — Zo* ships as
draft and is restamped in the beat when Zo edits it.

## Open

- Partial-start election floor when the whole zone was already cleared
  (beat 4, Gaps): the beat assumes at least one field Wave refights before
  the bank re-arms. Owner Zo; trigger: first play of this milestone's
  retrigger.
- Stale names `4-embark-at-wave-2.md` / `demo.embark_wave2` — rename is Zo's
  call; content already matches the ruled spec.

## Traps

Beat 3 + beat 4 Traps apply verbatim. Highest-cost three: keying the exit
standing to the third refuse (a one-buy seat never sees it), resetting
`DemoWheelBuysLeft` per visit, and rebuilding any part of the NAV skip.

## Depends on / Unblocks

Depends: M75 (v9, MP gate plumbing), M76 (loss 1, ~500 banked,
`DemoZoneDeepestWave` writes), M77 (survival machinery the loop closes on).
Unblocks: M79 (multiplayer from here on; ladder rung 2 extends this
bookkeeping).
