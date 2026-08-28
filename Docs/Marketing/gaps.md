# Gaps

<!--
Purpose: Where locked copy cannot be honest yet, and which existing work
would make it true. Not a store page. Does not invent mechanics.
Status: working
Update trigger: when a milestone changes a pitch Accuracy row, when a claim's
source identifier is renamed or deleted, or when Play tense is restamped.
Last updated: 2026-08-25 (regrilled against the tree after eight days of
milestones landed under untouched copy; three claims cited identifiers that no
longer exist)
-->

Grill of `strategy.md`, `product.md`, `landscape.md`, `gtm.md`, `pitch.md`
against code, Play, milestones, Demo, and brainstorms. Accuracy patches land in
those files the same day.

**The 2026-08-17 pass grilled a build that has since moved a long way and the
copy did not move with it.** Everything under *Landed under the copy* below
shipped after that stamp and against files nobody restamped. That is the
condition this register exists to catch, so it is listed first.

## Blocking

**Five-wave friends dry-run.** Shared Ship + party EMBARK closed with `M72`
(2026-08-17, host log `13-24-37` / Relay `KPRMPR`). The Roadmap friends-alpha
sentence still needs a full five-Wave both-participating Boarding, and nothing
since has supplied one — the co-op work that landed is capability, not evidence.

Recommendation: qualify "finish a Boarding together" until that dry-run lands.
Play shared-Ship present tense is no longer blocked by lobby topology.

**A shared realm has never run on two machines.** `M100` is built and carries 41
tests, and its criteria 1, 2, 9 and 10 are explicitly owed on two boxes. Until
they close, "play in your friend's world while they are offline" is a built
feature with no proof, which is a different thing from a shipped one.

## Landed under the copy, 2026-08-18 to 2026-08-25

Each of these changes what may honestly be said, and none of it was in the copy
files before this pass.

| What shipped | What it changes for copy |
|---|---|
| `M93` — three Primaries that read as three guns | "The weapon is the build" has three distinct answers, not a catalog |
| `M94` — VEIL and FORGE | Two specialist Protocols are claimable. "Specialist Protocols as if they ship" is no longer a blanket ban; AEGIS and LEDGER are the Coming Soon rows |
| `M96` — the realm and the Boarder | A Boarder travels between worlds; a realm keeps the pot, the doors and the record. Save v13 refuses older documents |
| `M97` — the ship remembers | The realm's record is on the hull: twelve clean-exit marks, a crew rack, a plate |
| `M99` — a range you can fight in | A door off the Ship into a practice range with the catalogue unlocked. Presentation is graybox and it is a dev tool, so it is not a store beat |
| `M101` — two decks and a stair | The Ship is a place with an upstairs. Any screenshot of the old wide single deck is of a hull that no longer exists |
| `M63-3` — the Cloaker | A fourth Gatekeeper kind, and the first whose refusal is a state rather than a target: watched it holds, unwatched it advances |
| `M98` recut — one priced respawn | Death is no longer flatly terminal. See the row below |
| `M95` — the co-op bastion | 28 rooms over two storeys, three hatches. Built and unverified in play |
| `M102` — the faucet loop graybox | Dev-gated and off by default. **Not printable in any form** — it is an experiment whose own playthrough is owed |
| Convert rename (2026-08-24) | Player text says Boarding, Ante, ante up, Convert. Every store sentence follows |

## Claims whose source no longer exists

The worst kind, because each reads as verified. Found 2026-08-25 by grepping the
identifier rather than trusting the sentence.

| Claim, and where | The problem | Honest line |
|---|---|---|
| Convert is "3 slots/Run", cited as `HatchBank.SlotsPerRun` — was in `pitch.md` and `product.md` | `SlotsPerRun` has zero hits in `Assets/Scripts/`. `language.md` still says Convert is capped per Boarding, so a cap exists; its source is unfound | Say "capped per Boarding" and print no number until someone names the constant |
| T1's Wave count — `pitch.md` and `gaps.md` both asserted **3** | Four sources disagree as of 2026-08-25: the generated Field Manual says 5, `language.md` says 3, and `DemoZones.WavesPerTriggerByZone` is `{ 1, 3 }`, so zone 1 authors 1. The Field Manual's sentence is a hardcoded literal in `Tools/docgen/Program.cs:880`, not a generated value, which is how it drifted without failing `--check` | Cite no number. `DemoZones.WavesPerTrigger` is the source. Not a marketing fix — the generator needs it |
| "code id stays `Ram` until M63" — `pitch.md`, `gaps.md`, `strategy.md` | `M63-3`'s rename landed 2026-08-20 and `Ram` has zero hits in `Assets/Scripts/`. `M63` itself is deferred, so the condition can never be met as written | Thief in code and in copy alike |
| `MetaUpgrades.SalvagePerKill`, "25 + 5×Scavenging" | Takes no argument and returns a flat 25. `UpgradeTrack` has one member left, `HatchArmor` | Salvage per kill is flat and does not vary by kind |
| Dead links: `../Play/A-Run.md`, `../Play/Intensity.md`, `../Demo/README.md` | Renamed with the Convert pass (`A-Boarding.md`, `Ante.md`) and split (`Demo - Single/`, `Demo - Coop/`) | Fixed in this pass |

## Honest now / not yet

| Pitch | Now | Gate |
|---|---|---|
| Alone | True | — |
| With friends | Shared Ship + party EMBARK (`M72`), a realm both can hold (`M96`, `M100`), a late arrival who follows in at a break (`M98`) | Five-Wave dry-run still open; `M100` unproven on two machines |
| Defeat Gatekeepers | True as the fight, four kinds | Escaped = Master Control (`M54`), not field Wave 5 |
| Their Protocols | False | Same modifier pool, Gatekeeper form **diluted** (`boarder-protocols.md`). Probe: one body a player can name. Your own sheets — Default, VEIL, FORGE — are not this sentence |
| Defend Salvage | True as Convert | False as fortress / "your Bastion". The per-Boarding cap has no citable number |
| Death ends it | **Half true, and this is new.** `M98` prices one way back per dead seat: paid from the Unbanked that seat is carrying, `150 × (taken + 1)` with the counter the Boarding's, remainder surviving the death (`Sim/Run/Respawns.cs`). Nothing else can pay — not the realm's bank, not another seat | A seat carrying nothing cannot pay and the Boarding ends there. Neither "one death and it is over" nor "you can always come back" is printable |

Trailer that is true tonight: quiet Prep, an ante up, a **Terminal** hunt, a
**Convert** at the Hatch, solo boarding, two people on a shared Ship, a crewmate
who animates. Not AEGIS or LEDGER. Not thousands. Not the faucet loop. Not "they
wear your Protocols" until the probe.

## Focus (existing work only)

Ranked to Fellowship / Expression / Fantasy (`../Design/1-Experience.md` §1.2).

| # | Lane | Work | Makes printable |
|---|---|---|---|
| 1 | Product | Five-Wave both-participating dry-run | Friends-alpha sentence in full |
| 2 | Product | `M100` on two real machines | A shared realm as a claim rather than a build |
| 3 | Copy | Keep Play and Compendium tense aligned with the shipped realm and Convert names | Stops stale hedging and stops retired words leaking outward |
| 4 | Product | One diluted Gatekeeper modifier (probe). Deeds later, as unlock spine | Protocols sentence |
| 5 | Build | Name the Convert cap and the T1 count in one place each, and make the Field Manual generate them | Removes the two unciteable numbers above |
| 6 | Product | Demo beats 0–7 — teach the Terminal hunt then the Convert | First-night as the two jobs that already ship |
| 7 | Product | Second destination that differs in layout / hazard / composition (`missions-and-destinations.md`) | Bastions, plural |

Presence polish is `M66` for nameplates and remote-gun; the crewmate body itself
is done (`M83`, and `M86` for the downed pose, active). Voice is rejected in
`M66`, parked in `../Brainstorms/co-op.md`. Neither unlocks a pitch sentence.

Do not revive parked crew attributes / XP (`crew-skills-and-perks.md`). Do not
sell FAB payloads as the Protocols sentence (`fabrication.md`).

## Play that constrains copy

Only `../GatekeeperZo Notes/playthrough.md`.

| Date | Constraint |
|---|---|
| 2026-08-07 | Loop can read as a shooter people want — **upgraded** loadout (MR-35 T4). Log named in `product.md` |
| 2026-08-09 | T9 felt easy; verdict **pending**. Do not trailer it |
| 2026-08-13 | The Convert fight is sellable |
| 2026-08-16 `[unconfirmed]` | Starting solo too hard; teach multiple Waves then overrun, lose **some** Salvage. Demo file 1 |
| Header | Field Waves still walk to the Boarder (camping). The Convert is the door job. Two modes are printable; a dungeon crawl is not |

**Nothing since 2026-08-16 is a play report.** Everything in *Landed under the
copy* closed on disk and harness evidence, and several carry owed playthroughs —
`M95`, `M100`, `M102` and `M86` by name. No quality claim may be made from any
of them.
