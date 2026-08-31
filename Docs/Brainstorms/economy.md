# Economy

<!--
Purpose: What currencies exist, what each is for, and the Gatekeeper roster as something the player buys rather than something the game hands out.
Status: brainstorm
Update trigger: when a currency gains or loses a sink, when the roster scope moves, or when Experience income is first measured.
Last updated: 2026-08-30 (Ante parked; kills bank as they land; respawn spends the realm bank; Cloaker debut is wave 4)
-->

## What this covers

Which currencies exist, which domain each owns, and where each comes from. The roster
of Gatekeepers as a **purchase** belongs here because it is a sink, not a wave-design
question; how those kinds behave is `gatekeepers-and-waves.md`. What a stake *is* — the
category that governs juicing — is settled and lives in `../Design/3-Mechanics.md` §3.1,
*Stake — the authored risk*; this file does not restate it.

## Where it stands

**One currency exists.** `BankedSalvage`, `LifetimeSalvageEarned` and `LifetimeSalvageSpent`
are the whole of it; there is no Experience field anywhere in `Sim/`, and `BuyProtocol` charges
0. `MetaState` is no longer where they live — `M96` split the save into a `BoarderState` that
travels with the player and a `RealmState` that stays with the world, and `MetaState` is now a
facade forwarding to both. The salvage ledger is the realm's half. **Kills bank as they land** (`RunSim.AwardKill` →
`BankKill` → `_meta.EarnSalvage`). Unbanked fills only on a deliberate Withdraw at the Hatch.
Carry depth (`HatchBank.CarryConvert`) is still a stake on that opt-in pile. The intensity dial
(`IntensityJuice`, `TetherSiphon`) is **parked** (`Ante.Enabled = false`, Zo 2026-08-30;
`../Decisions.md`) — Waves run at dial 0; the spend path stays behind the flag.

**Four facts the build already supplies, which the ideas below depend on:**

- The Hatch-bank window pays nothing — `CurrentKillAward` returns 0 in `WaveMode.HatchBank`.
  The window's reward slot is empty by design, not by oversight.
- Kind introduction is wave-indexed and resets every Run (`WaveDirector.HunterPerWave`,
  `ThiefPerWave`, `CloakerPerWave`). Hunter debuts wave 2, Thief wave 3, Cloaker wave 4
  (`CloakerPerWave = {0,0,0,1,2}` — one wave after the Thief so two new kinds do not land
  together). Five Waves cannot introduce twelve kinds, so the roster of twelve needs a second
  axis whatever else is decided.
- Salvage value is flat per kill and does not vary by kind — `MetaUpgrades.SalvagePerKill()`
  now takes no argument and returns 25. The Scavenging term this line used to carry is gone
  with the attribute tracks; `UpgradeTrack` has one member left, `HatchArmor`. So per-kind
  value has nothing to replace or multiply, which sharpens the Open question below rather
  than answering it.
- **The respawn fee spends the realm's bank** (`RunSim.TryRespawn` → `SpendBanked`; refusal
  `"not enough banked"`). Cap is 2 (`Respawns.MaxRespawns`), bleed-out 10 s, price
  `150 × min(taken+1, 3)`. The class-comment on `Respawns.cs` still describes the superseded
  Unbanked-purse rule; the method body and `RespawnsTests.TryRespawn_ChargesTheRealmsBank_NotAnySeatsCarry`
  are the record. Unbanked is an opt-in stake, not the buy-back purse. Any Core or Ante idea
  below spends the same bank kills already fill.

## Graduation test

What one Run pays in Experience — the roster price against that income is the only thing
that decides whether a bought Bastion is a commitment or a formality, and no entry here can
be priced without it.

## Decided

| Decision | Owner, date |
|---|---|
| Three currencies: Experience, Salvage, Cores. Experience owns the world, Salvage owns your kit, Cores own yourself. | Zo, 2026-08-20 |
| Experience is a wallet and is spent, not a rank. Never at risk. | Zo, 2026-08-20 |
| Salvage keeps its current domain and adds ammo. | Zo, 2026-08-20 |
| Cores buy Protocol unlock **and** upgrade, plus Run multipliers. | Zo, 2026-08-20 |
| The roster is set at first boarding and cleared when Master Control falls. Not permanent, not per-Run. | Zo, 2026-08-20 |
| One buff and one debuff per Gatekeeper to start; three of each once the first is played. | Zo, 2026-08-20 |
| Composition shaping waits on programmatic wave generation with guardrails. Until then Cores buy attribute multipliers only. | Zo, 2026-08-20 |
| Stakes may stack; the multiplier stack is a tuning matter, not a defect. Recorded in `../Design/3-Mechanics.md` §3.1. | Zo, 2026-08-20 |

## Ideas

### Experience buys the opposition
Experience is spent on which Gatekeepers are in the active roster, what level they are,
and their buff/debuff configuration, with Master Control enforcing a minimum so the
Bastion is never empty. It is the furthest extension of belief 4 — the player authors the
opposition as well as the pace and the stake — and it closes the Fantasy gap
`../Design/1-Experience.md` §1.2 names against itself (*"frame only… no fantasy
progression"*). It also gives the twelve-kind roster the second axis it needs, since the
per-Wave introduction table is out of room. *Contradicts nothing in `../Decisions.md`.*

### Cores, and what the Hatch window pays
A second banked currency earned by surviving a Hatch-bank window, scaled by carry depth,
spent on Protocol depth or on Run multipliers. It fills a reward slot the build
deliberately left empty, and it rides the same lever as Salvage — carrying deeper raises
both — so greed stays one decision rather than two. Families are attribute axes rather
than kinds (resilience, density, tempo), each spendable either on the wave or on your own
Protocol, which makes every Core held a choice between making yourself stronger and making
the test harder.

### Rewards ride the Gatekeeper
Salvage value and Core drops become properties of the enemy rather than of the Wave, so a
Wave's payout is whatever is standing in it. This satisfies the one-source stake rule by
construction instead of by discipline — there is no second table to drift — and it splits
tuning cleanly into a Bastion level (what shows up) and a Gatekeeper level (what each body
is worth). It retires the reward half of the intensity dial, which becomes a draw weight.
*Requires settling whether per-kind value replaces `MetaUpgrades.SalvagePerKill` or
multiplies it.*

### Compositions authored at full roster, drawn from the unlocked set
Wave composition is authored assuming every kind exists, then filtered to what the player
has unlocked. Fewer unlocked kinds produce a more concentrated Wave, and concentration is
easier to read, so the legibility curve falls out of the filter rather than being authored
per unlock state. No fallback substitution: a Gatekeeper-X fantasy Wave is simply weaker
without X, and that gap is measured rather than patched (see Constraints).

## Open

| Question | Why it matters | Owner |
|---|---|---|
| Who configures the roster in co-op? | Fellowship is rank 1 and this is the only place the design touches it; a guest fighting a roster they had no say in is a regression. The house pattern is party lead (`M72`). | Zo |
| What does a Run pay in Experience, against what a roster costs? | The graduation test. Cheap Experience means you leave, re-board and rebuy, and the Bastion scope is cosmetic. | the milestone that first pays Experience |
| Does per-kind Salvage value replace `MetaUpgrades.SalvagePerKill` or multiply it? | The question got simpler when Scavenging was deleted: the base is now a flat 25 with nothing scaling it, so per-kind value would be the only thing making it vary. | Zo |

## Constraints

Any milestone from this file respects these.

- **A buff or debuff may never touch a refusal.** `../Design/1-Experience.md` §1.4 — each
  kind is defined by what it refuses, and each refusal is a rule in code *"so no tuning pass
  can soften one by accident."* The config surface moves HP, speed, damage and value only.
- **No drop is an independent roll.** `../Decisions.md` cut list, *Independent-roll rewards*.
  The filtered draw needs weights or pre-determination too, not a per-spawn roll.
- **The minimum roster stays inside the three-failed-Runs buy-past**
  (`../Design/2-Dynamics.md` §2.5). A Bastion-scoped roster can otherwise trap a player
  behind a wall they built.
- **Every stake raises danger and reward from one source**
  (`../Design/3-Mechanics.md` §3.1).
- **The guard reads both escalation axes.** Experience adds variety per Bastion, Cores add
  intensity per Run, and neither sees the other. A wave-power assertion over
  (Bastion × unlock set) that reads only one is reading half the game — the `M52` class.

## Rejected

- **A third currency for wave modifiers** ("Clearance"). Being short of it would have sent
  the player to take more risk, which is already how Salvage and Cores are earned — the same
  activity behind a third number. The modifier is the Core; there is nothing to buy it with.
  Rejected 2026-08-20.
- **A currency for buying access or skipping deeper** — the other job the third currency
  could have had. `../Decisions.md` already stamps *pay-to-juice, not pay-to-skip*.
  Rejected 2026-08-20.
- **Experience as a rank rather than a wallet.** Considered and overruled: unlocking and
  upgrading Gatekeepers does not need to be permanent, and a spendable, respecable roster is
  a loadout rather than a ratchet. Zo, 2026-08-20.
