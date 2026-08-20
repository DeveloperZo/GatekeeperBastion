# M78 — The first return

<!--
Status: completed, Aug 2026
Closed on: 2026-08-20 (Zo). Criterion 9's double playthrough (`Lost` and
`Returned`) stamped by the close request. Criteria 0–8 were in tree from the
2026-08-19 build; 1–7 are confirmed on screen by criterion 9's construction.
HP-2 (TH-10, TH-12, TH-14, TH-17–TH-20) settles with the `Returned` half.
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-20
-->

Beats 3 + 4 (minus the run-structure piece, which is M77): the first ARMORY
panel with the two-buy ladder rung, multiplayer unlocking on the first
purchase, the state-keyed exit standing, the CREW menu + Default claim on the
re-keyed unlock, plain EMBARK, and the composite retrigger at the zone's
deepest terminal. Cut 5 of the demo family. One milestone because without the
retrigger a post-loss EMBARK lands in a from-scratch chain (beat 3's stated
reason).

**Reframed 2026-08-18 (Zo).** The trigger is **the first return to the Ship
with salvage banked**, not the first loss. `M77` made losing optional, so a
loss-keyed beat never fires for a player who survives their bank window and
docks.

## Story

As a player back aboard with my first salvage, I want to spend what came
through the Hatch on the wheel, claim my protocol, and embark straight back to
a terminal that remembers the zone — so that a return reads as progress spent
and a fight resumed, however the run ended.

## Why it matters

The wheel unlocks on first loss with ten tiers and no cap; this caps it at
ladder rung 1 — 2 Secondary buys — and makes the first purchase the
multiplayer key (`../../Demo/3-ship-after-first-loss.md`). The NAV skip is
dead; refight-avoidance lives at the terminal as the composite retrigger with
partial start electable (`../../Demo/4-embark-at-wave-2.md`). Retrigger
election reads per-zone `DemoZoneDeepestWave` (ruled at cut, Zo 2026-08-17 —
beat 4's stamp; README corrected in this cut).

**The gate moves from the first loss to the first bank, and the reason is a
conflict inside `../../Design/1-Experience.md`.** `MetaUpgrades.UpgradesUnlocked`
is `PodEjections >= 1`, and `RunSim.EndRun` reads `if (!docked) _meta.PodEjections++`,
so a player who returns to the Ship never unlocks Tiers or the equipment slot.
Skill locks you out of the deepest progression track. Belief 1 says a lost run
still moves you forward; it does not say you must lose to move forward, and
gating the wheel on dying is belief 2's opposite. That reading was harmless
until `M77` shipped. **The wheel spends salvage that came through the Hatch, so
it opens when salvage first comes through the Hatch** — the same event, stated
honestly, and it fires for the player who banks and docks as well as the one
who banks and dies. A player who banked nothing has nothing to spend.

Arc on the authored ladder and lines; loop on the retrigger — every return in
the shipped game resumes this way.

## Acceptance criteria

0. **`MetaUpgrades.UpgradesUnlocked` keys on the first bank, not on
   `PodEjections`.** A new `MetaState` counter records that salvage has
   converted through the Hatch at least once; the wheel and the equipment slot
   read that. `PodEjections` keeps its own job (endings, `demo.second_loss`)
   and is no longer a progression gate. **`../../Design/1-Experience.md`'s
   belief-1 row is rewritten in the same change**, since the loss-gate stops
   being what carries it, and `../../Decisions.md` records the move.
0b. **Surviving pays a little, and only a little.** A `Returned` ending keeps a
   stated small share of Unbanked instead of wiping all of it; `Lost` still
   wipes. The share is one number, generated into `../../Design/4-Data.md`
   (`M33`), initial value small and tunable after a logged Run. Door-state
   persistence via the successful-bank snapshot is already part of this reward
   and is not double-counted. **It must stay slight**: greed is still carrying
   a bigger pile into fewer extracts, and a large survival rebate turns the
   dock into the optimal line.
1. On the first Docked with banked salvage, ARMORY Down opens the panel (grant
   path only while `!UpgradesUnlocked`); two wheel buys succeed at banked ≥ 450; a third
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
5. `DemoCrewMenuUnlocked` writes on the first Docked after the first bank —
   whether the run ended `Lost` or `Returned`; CREW
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
9. Playthrough, **run twice: once ending `Lost` and once ending `Returned`**,
   and confirm both open the panel — two buys, claim, plain EMBARK,
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

**Added 2026-08-18; restamped 2026-08-19:** keying anything else in the demo
family to `PodEjections` as a progression gate. `M79` keys the Primary on
zone 2's bank falling (`DemoZone2BankLost`), not on a second ejection. The
bank is still composition-forced on the secondary kit. Nothing else may
assume a loss happened.

Beat 3 + beat 4 Traps apply verbatim. Highest-cost three: keying the exit
standing to the third refuse (a one-buy seat never sees it), resetting
`DemoWheelBuysLeft` per visit, and rebuilding any part of the NAV skip.

## Depends on / Unblocks

Depends: M75 (v9, MP gate plumbing), M76 (loss 1, ~500 banked,
`DemoZoneDeepestWave` writes), M77 (survival machinery the loop closes on).
Unblocks: M79 (multiplayer from here on; ladder rung 2 extends this
bookkeeping).

## Built 2026-08-19 — what landed, and what it proved

Every criterion's code surface is in the tree; nothing here closes criterion 9,
which is a playthrough only Zo can run.

**The re-key (criteria 0/0b).** `MetaState.HatchConversions` (save v11) counts
bank windows that converted for a seat; `MetaUpgrades.UpgradesUnlocked` reads
`>= 1`, and so do the equipment-slot refuse and `RepairMissingStep`. Written on
a window's first credit (`RunSim.Wallets.CreditSlot`) and, for guests, on their
own `CreditBankedClientRpc`. Pre-v11 saves with `PodEjections >= 1` or
`CleanExits >= 1` migrate to 1 — the re-key never re-locks an open wheel.
`HatchBank.ReturnedKeepShare` (0.10, generated into `../../Design/4-Data.md`)
is kept per slot by `RunSim.KeepReturnedShare` on a Returned end only.
`../../Design/1-Experience.md` belief-1 row rewritten; `../../Decisions.md`
records the move (*the wheel opens on the first bank*); the two stale README
rows (wheel key, CREW re-key) corrected with stamps.

**Finding, stated plainly (0b's real surface):** dump-on-survive already
converts the whole pile when a window is survived, so Unbanked at a Returned
end is **post-window money only — today, a withdraw**. The share as specified
is therefore a guard on withdraw-then-dock (keeps 10% instead of wiping 100%),
not a general survival rebate. Implemented literally; if the intent was a
larger reward, that is a design change for Zo, not a defect in this cut.

**Beat 3 (criteria 1–4).** `BuyWheelUpgrade` (now `ShipCommands.Weapons.cs` —
the demo cap pushed the file over the Game line cap): named cap refuse before
any `SpendSalvage`, decrement + `demoBuysLeft=<n>` suffix on success, 2 → 1
fires `demo.mp_unlocked` + `[Demo] multiplayer unlocked` once per save by
construction. `demo.wheel_cap` plays once per visit from the ARMORY wheel's BUY
click at cap; deal line carries remaining buys / the cap copy; the lock label
re-keys ("bank salvage through the Hatch first"). Exit standing is state-keyed:
`DemoProgress.NoBuyableUpgradeRemains` (owned wheels only — an un-owned SEC-D
never counts) evaluated by `DemoShipStanding.Reevaluate` on Docked and after
every ShipCommands money path, flip logged.

**Beats 3+4 CREW / NAV (criteria 5–6).** `DemoShipDirector.ResolveFirstReturn`:
first Docked with `HatchConversions >= 1` plays `demo.first_hatch_down`, queues
`demo.crew_ready` (the stated same-frame order), writes `DemoCrewMenuUnlocked`
+ `[Demo] CREW menu unlocked` — once per save via the flag. CREW panel folds
the heal in on open and plays `demo.crew_claim` while Default is unclaimed;
never auto-equips. NAV hides the picker whole while `DemoActive` (solo and
party lead), forces wave 1, plays `demo.embark_wave2` on first open after the
first bank; the Bastion logs `[Demo] embark plain`.

**Beat 4 retrigger (criterion 7).** `RunSim.TriggerCompositeDraw(start, end)`
launches the elected Waves consecutively on the deep-start chain machinery,
capped per trigger by `MaxWaves` (refuses, never clamps). `DemoRun`
(engine-free) owns the offer: zone 1's deepest terminal, only when the saved
`DemoZoneDeepestWave` is ahead of this Run's zone progress and D1 is shut;
partial start = the deepest cleared Wave, so at least one Wave refights.
`GameController.Retrigger.cs`: E arms (first arm per zone speaks
`demo.zone_retrigger`), +/- elects composite vs partial, T commits with the
`[Demo] zone retrigger zone=1 start=<j> waves=<k> mode=…` log; the chain's one
clear completes every zone terminal (`[Demo] zone terminals complete zone=1`)
so the bank re-arms. Bank refuse re-copied to `[Demo] bank refused — zone
incomplete` / `Hatch-bank — finish the zone`; `[Demo] bank window survived
zone=<k>` logs on survival. M76's owed `SetZoneDeepest` write landed on the
field-clear path (TH-10) and the first-Docked voice on the Ship (TH-12).

## Verification status (2026-08-19)

- **Offline harness** (dotnet replication of `Tools/verify/run.sh` — mono is
  not installed on this machine; same sources, same Shims, same runner): Sim
  compiles alone against the stubs; **497 passed, 8 failed**, the 8 the known
  pre-existing partial-mirror set (HudSeparation ×2, M59Polish ×5,
  SessionMusic ×1). lint-language, lint-arch, lint-vendor, and
  `docgen --check` (both targets) pass.
- **AutoVerify file bridge** (editor open, full suites): **EditMode 552/9,
  PlayMode 56/2.** The 9 are the same 8 plus `BastionDoorApertureTests`
  (aperture 3.5 vs 4.5 — kit geometry; nothing in this change reads it); the
  PlayMode 2 are `NetVerbTests.Repair_ClientChannel…` (0.5 HP past a 0.05
  tolerance — repair timing, untouched here) and `BuildSettings_OrderIs…`
  (the uncommitted `Ship_Isometric` scene in Build Settings — the iso-camera
  probe, prior session). None of the four reads a file this milestone touched;
  none is claimed settled.
- **Net new tests:** `ZoneRetriggerTests` (10: composite chain, partial,
  refusals, offer matrix, zone map), `FirstReturnSourceTests` (11 source pins
  on the Game wiring), 4 added to `DockedRunTests` (share kept on Returned,
  none on Lost, conversion counting), 1 to `DemoSaveFlagsTests` ×2 (v11 round
  trip + migration), 6 exit-standing rows in `DemoProgressTests`.
- **Rewritten, not deleted** (they pinned the rule this milestone re-keys):
  `EquipmentTests` ×3, `PlayerAttributesTests` ×1,
  `PlayerDownAndNewSaveEconomyTests` ×2, `CoreLoopTests` ×1 (a loss that
  banked nothing now leaves the shop shut — that is the re-key, asserted),
  `DemoProgressTests` ×2, `DockedRunTests` ×1, `DemoSaveFlagsTests` version pin.

**Not proven, by anything here:** criteria 1–8 on screen, and criterion 9's
double playthrough. The state-keyed standing, the cap line, the retrigger
prompts and the CREW flow have never been seen in play. Nobody has pressed
Play on this build.

## Closed 2026-08-20

Zo close. Criterion 9's `Lost` and `Returned` playthroughs are the stamp.
Criteria 1–7 are confirmed on screen by that construction. `HP-2` settled
with the `Returned` half.

## Deltas from the beat text, recorded

- **Once-per-save voice latches without save flags:** `demo.embark_wave2` and
  `demo.zone_retrigger` have no v9 field to carry them, so their latches are
  per session (statics, reset on subsystem registration); an app restart can
  replay them once. `demo.crew_claim` keys on Default-unclaimed, so the claim
  itself retires it. Adding save fields for voice latches was not in the v9
  spec and is Zo's call if replays grate.
- **Co-op retrigger election is host-only in this cut** — beat 4's Multiplayer
  row says any seat at the console elects, replicated; a client seat currently
  reads no offer (the live client-cannot-start-a-Wave rule). Owed to the beat
  when multiplayer work resumes (beats 4+ are the first co-op beats anyway).
- **Retrigger UX shape:** the offer is prompt-driven (E select, +/- election,
  T commit) rather than a panel — Bastion terminals have no panel surface, and
  the intensity-select pattern is already the terminal's grammar. Copy is
  draft — Zo, per the beat.
