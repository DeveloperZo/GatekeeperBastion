# Gaps

<!--
Purpose: Where locked copy cannot be honest yet, and which existing work
would make it true. Not a store page. Does not invent mechanics.
Status: working
Update trigger: when a milestone changes a pitch Accuracy row, when a claim's
source identifier is renamed or deleted, or when Play tense is restamped.
Last updated: 2026-08-30 (Copy restamped; 0.34.2 grill: payroll inverted, Ante parked, respawn
purse, one-deck hull, DemoZones {2,3}, SalvagePerKill 42; play reports 08-25 and 08-29)
-->

Grill of `strategy.md`, `product.md`, `landscape.md`, `gtm.md`, `pitch.md`
against code, Play Mechanics, milestones, Demo, and brainstorms. Accuracy patches
land in those files the same day.

**The 2026-08-17 copy was restamped 2026-08-25 against a build that then moved
again.** 0.34.2 (`ProductVersion.Display`) ships three inverted rules this folder
still described as current until this pass: kills bank on landing, the respawn
purse is the realm's bank, the Ship is one deck. Ante is parked. That is the
condition this register exists to catch.

## Blocking

**Five-wave friends dry-run.** Shared Ship + party EMBARK closed with `M72`
(2026-08-17, host log `13-24-37` / Relay `KPRMPR`). The Roadmap friends-alpha
sentence still needs a full five-Wave both-participating Boarding, and nothing
since has supplied one — including the 0.34.2 zip. The co-op work that landed is
capability, not evidence.

Recommendation: qualify "finish a Boarding together" until that dry-run lands.
Play shared-Ship present tense is no longer blocked by lobby topology.

**A shared realm has never run on two machines.** `M100` is built and its
criteria 1, 2, 9 and 10 are explicitly owed on two boxes. Until they close,
"play in your friend's world while they are offline" is a built feature with
no proof.

## Landed under the copy, 2026-08-26 to 2026-08-30

Each of these changes what may honestly be said.

| What shipped | What it changes for copy |
|---|---|
| Kills bank on landing (2026-08-25, Play `Salvage.md`) | Unbanked-until-Convert is false. Convert is an opt-in Withdraw bet. Struck from the demo |
| Hatch-start / terminal-end (`Getting-Started.md`; faucet-loop **shape** on by default) | A Wave begins at the Hatch and ends at the zone terminal. Field terminals do not offer a start. Printable. The M102 graybox on top of it (auto-chain, Hatch-hunt, endure income, draft) stays **not printable** |
| Roster cut ~40% (2026-08-27, `WaveDirector`) | Dial-0 solo is 11 / 18 / 25 / 32 / 39, not 18–66. Salvage per kill is **42**, not 25 |
| Field Waves can nick the Hatch (`HatchBank.FieldWaveDamageScale = 0.15`) | "Field Wave, Hatch cannot be hurt" is false |
| `M101` superseded (2026-08-26, Zo) | The Ship is one deck. Two-deck screenshots are of a hull that no longer exists |
| Respawn purse recut | Realm bank, cap **2**, bleed-out **10 s**. File comment on `Respawns.cs` is stale |
| Ante parked (2026-08-30, Zo) | `Ante.Enabled = false`. No ante up, no intensity select, no Ante readout. Waves at dial 0 |
| Hatch recall pads (2026-08-30) | Hold 1.5 s at T2 / T5 Portal teleports to the Hatch. Operational, not a store pillar |
| Hatch Master gate (`ControlRoomReachable`, 2026-08-30) | Hatch offers Master only when the control room is reachable; Convert / Withdraw can answer while the gate is shut |
| Join-code reuse + Boarder name field (2026-08-30) | OPEN TO FRIENDS keeps the Relay allocation; REALMS has a Boarder name box. Send polish, not a pitch sentence |
| SHIFT wordmark off; surfaces read GATEKEEPER'S PROTOCOL (2026-08-30) | **Not printable.** Chrome, not a product beat |
| `M99` range | Graybox / dev tool. Not a store beat (`gaps.md` 2026-08-25 still holds) |
| FORGE turret + Outrider (`Protocol-FORGE.md`, 2026-08-29) | May name the machines as **yours**. `M106` / `M107` still sit in `1. planned/` — Play is the citation, not those files |
| Wheel unlock re-keyed | `MetaUpgrades.UpgradesUnlocked` is `LifetimeSalvageEarned > 0`, not first Convert and not first Lost |
| `DemoZones.WavesPerTriggerByZone` | `{ 2, 3 }`. Zone 1 runs **2** Waves as one composite. `language.md` still says `{ 1, 3 }` |

## Claims whose source no longer exists, or now disagrees

| Claim, and where it lived | The problem | Honest line |
|---|---|---|
| Convert is "3 slots/Run", `HatchBank.SlotsPerRun` | Identifier has zero hits. `HatchBank.BankCap` exists and is co-op conversion math, not a Convert count. `language.md` Convert row: **there is no per-Boarding cap** | Print no number. Convert is not the payroll |
| T1's Wave count as 3 or 5 | `DemoZones` is `{ 2, 3 }`. Field Manual: zone 1 = 2 waves. `language.md` still says `{ 1, 3 }` | Cite `DemoZones`. Not a marketing fix — language.md is stale |
| Dial-0 bodies 18 / 30 / 42 / 54 / 66 | `WaveDirector` 2026-08-27: `11 + 7×i` → 11 / 18 / 25 / 32 / 39 | Cite the director. 4-Data's co-op formula line still reads `18 + 12×i` and is stale |
| `MetaUpgrades.SalvagePerKill` = 25 | Returns **42** | 42 |
| Respawn from carried Unbanked | `Respawns.Refuse` reads the realm bank; refusal string `not enough banked` | `Going-Down.md` |
| Two-deck Ship (`M101`, product.md 2026-08-25) | Superseded 2026-08-26 | `The-Ship.md` one deck |
| First Lost / first Convert unlocks the wheel | `LifetimeSalvageEarned > 0` | `MetaUpgrades.UpgradesUnlocked` |
| `../Play/*.md` | Folder is `Play Mechanics/` | Fix the path |
| Field Manual Ante table as live | Generator still prints it; `Ante.Enabled` is false | Do not print |

## Honest now / not yet

| Pitch | Now | Gate |
|---|---|---|
| Alone | True | — |
| With friends | Shared Ship + party EMBARK (`M72`), a realm both can hold (`M96`, `M100`), a late arrival who follows in at a break (`M98`). 0.34.2 is a zip of that | Five-Wave dry-run still open; `M100` unproven on two machines |
| Defeat Gatekeepers | True as the fight, four kinds, Hatch-start / terminal-end | Escaped = Master Control (`M54`), not field Wave 5 |
| Their Protocols | False | Same modifier pool, Gatekeeper form **diluted**. Probe: one body a player can name. Your sheets — Default, VEIL, FORGE (turret + Outrider) — are not this sentence |
| Defend Salvage (old Copy) | **Restamped 2026-08-30.** Hold the Hatch. Salvage banks as they fall. Both **true** | Convert remains opt-in on Withdraw; no cap; struck from the demo. Do not print the retired sentence |
| Death ends it | **Priced, capped, timed.** Realm bank, `150 / 300`, then `out of respawns`. 10 s bleed-out | Neither "one death and it is over" nor "you can always come back" is printable |
| Ante / author the swarm | **Parked** | Flip `Ante.Enabled`; then Zo restamps Copy if he wants the wager in the block |

Trailer that is true tonight: quiet Prep, `T` at the Hatch, the walk, a terminal
shutoff, solo boarding, two people on a shared Ship, a crewmate who animates.
Not AEGIS or LEDGER. Not thousands. Not the faucet graybox. Not the range.
Not the wordmark swap. Not Ante. Not "they wear your Protocols" until the probe.
Not Convert as payroll. Not the two-deck hull.

## Focus (existing work only)

Ranked to Fellowship / Expression / Fantasy (`../Design/1-Experience.md` §1.2).

| # | Lane | Work | Makes printable |
|---|---|---|---|
| 1 | Product | Five-Wave both-participating dry-run | Friends-alpha sentence in full |
| 2 | Product | `M100` on two real machines | A shared realm as a claim rather than a build |
| 3 | Copy | Keep Play and Compendium tense on Hatch-start and bank-on-kill | Stops the retired "defend your Salvage" sentence leaking outward |
| 4 | Copy | Recut `Demo - Single/` against Getting Started (Hatch-start, no Ante teach, kills already banked) | First-night spec matches the zip |
| 5 | Product | One diluted Gatekeeper modifier (probe). Deeds later, as unlock spine | Protocols sentence |
| 6 | Product | Flip `Ante.Enabled` if the stake half of pace-and-stake is wanted again | The wager. Not a copy fix — Zo parked it |
| 7 | Product | Second destination that differs in layout / hazard / composition (`missions-and-destinations.md`) | Bastions, plural |

Presence polish is `M66` for nameplates and remote-gun; the crewmate body itself
is done (`M83`, and `M86` for the downed pose, active). Voice is rejected in
`M66`, parked in `../Brainstorms/co-op.md`. Neither unlocks a pitch sentence.

Do not revive parked crew attributes / XP (`crew-skills-and-perks.md`). Do not
sell FAB payloads as the Protocols sentence (`fabrication.md`). Do not sell the
faucet graybox. Do not sell Ante while the flag is false.

## Play that constrains copy

Only `../GatekeeperZo Notes/playthrough.md`.

| Date | Constraint |
|---|---|
| 2026-08-07 | Loop can read as a shooter people want — **upgraded** loadout (MR-35 T4). Log named in `product.md` |
| 2026-08-09 | T9 felt easy; verdict **pending**. Do not trailer it |
| 2026-08-13 | The Convert fight is sellable as an opt-in Hatch bet |
| 2026-08-16 `[unconfirmed]` | Starting solo too hard. "Lose some Salvage" was the old Unbanked model |
| 2026-08-25 pending | MK-1 T2, no Primary, lost zone 1 composite at 25 live, 2 respawns, 227 banked. Log named in `product.md`. Soft-lock `[unconfirmed]`. Do not market an easy first night |
| 2026-08-29 pending | FORGE T2, Hatch lost wave 8; machines 0 damage absorbed; EMP 6/8 caught 0. Do not trailer the Outrider as the hold |
| Header | Field Waves still walk to the Boarder (camping). The Convert is the door job. Two modes are printable; a dungeon crawl is not |
