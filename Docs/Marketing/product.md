# Product

<!--
Purpose: What this game is, as a buyer would meet it, with play evidence.
Not a store page. Numbers and verdicts are cited; unsourced "fun" is omitted.
Status: working
Update trigger: when a playthrough verdict changes the first-session read, or
when a milestone changes a pitch gap.
Last updated: 2026-08-25 (Boarding / Ante / Convert names; the Cloaker, VEIL and FORGE, the
two-deck hull and the priced respawn added; three dead Play and Demo links fixed; the
SlotsPerRun and T1 numbers withdrawn to gaps.md)
-->

## Session

Ship (four consoles, now over two decks joined by an open stair) → embark NAV MAP →
Bastion already tethered at the Hatch → quiet Prep until you ante up, select and
start at a field terminal, the Hatch, or Master Control → Assault until that Wave's
Gatekeepers are dead → Prep again → lost (the Boarder down with nothing to pay with,
or Hatch 0) or Escaped (Master Control's Convert half survived).
`../Play/The-Game.md`, `../Play/A-Boarding.md`.

One destination: **ALPHA-7**. `SceneFlow.BastionName`. The combat scene behind it is
`Bastion_Coop` — `M95` replaced the old solo bastion rather than adding a second.

## What is unusual (felt in a first Boarding)

| Mechanic | What the player does | Source |
|---|---|---|
| You start the clock | Nothing arrives until `T` | `../Play/The-Game.md`; belief 4 in Design |
| The Ante is a wager | `+/-` spends Unbanked; more Defaults, more Salvage per kill; not tougher kinds | `../Play/Ante.md`; surplus is Defaults only (`WaveDirector`) |
| Salvage is not loot-on-kill | Unbanked until a Convert at the Hatch, capped per Boarding; leftover wipes on a loss | `../Play/Salvage.md`. The cap's old citation is dead — see `gaps.md` |
| Kinds refuse | Hunter never Hatch; Thief never turns when shot and never hurts you; the Cloaker will not advance while you are looking at it | `../Play/Gatekeepers.md`, `../Play/Gatekeeper-Cloaker.md` |
| The weapon is the build | Per-weapon wheel after the first loss; five shop rows, three of them Primaries that read as three different guns since `M93` | `../Play/Weapons.md` |
| Protocol | Default claim 0 Salvage: Barrage `G`, Rush `H`. VEIL and FORGE claimable since `M94`; AEGIS and LEDGER Coming Soon | `../Play/Protocols.md` |
| Death has a price, not a wall | A dead seat buys back with the Unbanked it is carrying, `150 × (taken + 1)`, remainder surviving; carrying nothing ends it | `../Play/Going-Down.md`; `Sim/Run/Respawns.cs` |

Body count is small on purpose. Groups with a gap, not a stream
(`../Design/3-Mechanics.md`). Marketing that sells "deluge" is describing a
different game (`landscape.md`).

## Play evidence (quality)

Only `../GatekeeperZo Notes/playthrough.md`. Two that constrain marketing:

| Date | What | Verdict | Marketing implication |
|---|---|---|---|
| 2026-08-07 | Five-Wave clear, MR-35 T4, start Wave 2 | **fun and semi-challenging** | Loop can read as a shooter people want — at an **upgraded** loadout. Log `GatekeeperBastion_2026-08-07_16-34-30.log`. Hatch 300/300 every Wave of that clear. |
| 2026-08-09 | Wave 3 deep start, MR-35 T9 | Zo **report**: still felt easy; formal verdict **pending** | Mid-game loadout is not the first night. Do not trailer T9 as the product. |
| 2026-08-13 | M53 Convert | **hatch wave worked pretty well**; run ending after it was the miss (sim fixed, not replayed) | Convert fight is sellable. |
| 2026-08-16 | Solo, starting loadout, no log `[unconfirmed]` | **too hard as single player**; multiple Waves then overrun at the Hatch, lose **some** Salvage | Do not market "easy first night." Demo file 1 is the teach (`../Demo - Single/`). |

No other verdict is a reception claim, **and nothing after 2026-08-16 is one at
all** — every milestone since closed on disk and harness evidence, several with
playthroughs still owed. Two Wave modes already ship (`WaveMode`): a field-terminal
Wave hunts **you** (Hatch cannot be hurt); a Convert hunts the Hatch (Hunters still
hunt you) (`../Play/A-Boarding.md`). "No incentive to move from the Hatch"
(playthrough header) is camping **field** waves — they walk to the Boarder. Copy may
name both jobs. A dungeon crawl still oversells camping.

## Gaps vs locked pitch (`pitch.md`)

| Pitch | Product |
|---|---|
| Alone | True. |
| With friends | Host/Join + shared Ship (`M72` closed 2026-08-17, Relay `KPRMPR`), plus a realm both can hold (`M96`, `M100`) and a late arrival who follows the party in at a break (`M98`). Five-Wave friends-alpha dry-run still open, and `M100` is unproven on two machines. |
| Defeat the Gatekeepers | True as the fight, four kinds since the Cloaker; Escaped is Master Control, not Wave 5. |
| Their Protocols | False until a Gatekeeper wears a pool modifier, diluted (`boarder-protocols.md`). Default, VEIL and FORGE are all **your** concentrated sheets. Deeds unlock the next protocol; they do not wear it on a body. |
| Defend Salvage | True as a Convert. False as fortress defense. |

## Audience jobs (inference)

| They already play | Job that matches this build | Evidence | Inference |
|---|---|---|---|
| Horde survivor / GnG | Author the swarm; keep banked on death | Beliefs 1 and 4; Prep pays 0 | Stay if they hated idle tags. Leave if they wanted thousands. |
| KF2-style Waves | Gun identity, friends, clear a Wave | Wheel; kinds; Fellowship incomplete | Bounce until friends-alpha if they came for reliable co-op. |
| Extraction | Get value out through a fight | The Convert; no bail-out mid-Boarding | Same job, different verb. They will look for an extract button that does not exist — the one exit that does is holding interact at the Hatch after a *survived* Convert, which ends the Boarding Docked with banked Salvage kept (`M77`). |

## First session the market will meet

If `../Demo - Single/` ships (milestones not cut):

| Beat | They meet |
|---|---|
| 0 | Dead Ship, grant MK-1. Skippable on later realms since 2026-08-24, and off by default so a first-timer is still taught |
| 1 | The zone's Waves, an ante up after the first |
| 2 | Convert overrun; lose some Unbanked |
| 3–5 | ARMORY + wheel (cap **2** while `DemoActive`); FAB dark; NAV skip; doors toward Control Room |
| 6–7 | Master Control is not the win; the Hatch finale is Escaped |

Until then, Getting Started is onboarding — and it still prints a T1 Wave count that
disagrees with the code and with the Field Manual. **Quote no number for it**; the
four-way disagreement and its source are in `gaps.md`.
