# Product

<!--
Purpose: What this game is, as a buyer would meet it, with play evidence.
Not a store page. Numbers and verdicts are cited; unsourced "fun" is omitted.
Status: working
Update trigger: when a playthrough verdict changes the first-session read, or
when M70 / M72 / a second destination changes a pitch gap.
Last updated: 2026-08-17
-->

## Session

Ship (four consoles) → embark NAV MAP → Bastion already tethered at the Hatch →
quiet Prep until juice / select / start at a field terminal, Hatch, or Master
Control → Assault until that Wave's Gatekeepers are dead → Prep again → Lost
(host down or Hatch 0) or Escaped (Master Control Hatch-bank survived).
`../Play/The-Game.md`, `../Play/A-Run.md`.

One destination: **ALPHA-7**. `SceneFlow.BastionName`.

## What is unusual (felt in a first Run)

| Mechanic | What the player does | Source |
|---|---|---|
| You start the clock | Nothing arrives until `T` | `../Play/The-Game.md`; belief 4 in Design |
| Intensity is a wager | `+/-` spends Unbanked; more Defaults, more Salvage per kill; not tougher kinds | `../Play/Intensity.md`; surplus is Defaults only (`WaveDirector`) |
| Salvage is not loot-on-kill | Unbanked until Hatch-bank (3 slots/Run); leftover wipes on Lost | `../Play/Salvage.md`; `HatchBank.SlotsPerRun = 3` |
| Kinds refuse | Hunter never Hatch; Thief never turns when shot and never hurts you | `../Play/Gatekeepers.md` |
| The weapon is the build | Per-weapon wheel after first Lost; five guns | `../Play/Weapons.md` |
| Protocol | Default claim 0 Salvage: Barrage `G`, Rush `H`. Specialists Coming Soon | `../Play/Upgrades.md` |

Body count is small on purpose. Groups with a gap, not a stream
(`../Design/3-Mechanics.md`). Marketing that sells "deluge" is describing a
different game (`landscape.md`).

## Play evidence (quality)

Only `../GatekeeperZo Notes/playthrough.md`. Two that constrain marketing:

| Date | What | Verdict | Marketing implication |
|---|---|---|---|
| 2026-08-07 | Five-Wave clear, MR-35 T4, start Wave 2 | **fun and semi-challenging** | Loop can read as a shooter people want — at an **upgraded** loadout. Log `GatekeeperBastion_2026-08-07_16-34-30.log`. Hatch 300/300 every Wave of that clear. |
| 2026-08-09 | Wave 3 deep start, MR-35 T9 | Zo **report**: still felt easy; formal verdict **pending** | Mid-game loadout is not the first night. Do not trailer T9 as the product. |
| 2026-08-13 | M53 Hatch-bank | **hatch wave worked pretty well**; run ending after it was the miss (sim fixed, not replayed) | Convert fight is sellable. |
| 2026-08-16 | Solo, starting loadout, no log `[unconfirmed]` | **too hard as single player**; multiple Waves then overrun at the Hatch, lose **some** Salvage | Do not market "easy first night." Demo file 1 is the teach (`../Demo/README.md`). |

No other verdict is a reception claim. Two Wave modes already ship (`WaveMode`):
a field-terminal Wave hunts **you** (Hatch cannot be hurt); a Hatch-bank hunts
the Hatch (Hunters still hunt you) (`../Play/A-Run.md`). "No incentive to move
from the Hatch" (playthrough header) is camping **field** waves — they walk to
the Boarder. Copy may name both jobs. A dungeon crawl still oversells camping.

## Gaps vs locked pitch (`pitch.md`)

| Pitch | Product |
|---|---|
| Alone | True. |
| With friends | Host/Join + shared Ship (`M72` closed 2026-08-17, Relay `KPRMPR`). Five-Wave friends-alpha dry-run still open. |
| Defeat the Gatekeepers | True as the fight; Escaped is Master Control, not Wave 5. |
| Their Protocols | False until a Gatekeeper wears a pool modifier, diluted (`boarder-protocols.md`). Default is **your** concentrated sheet. Deeds unlock the next protocol; they do not wear it on a body. |
| Defend Salvage | True as Hatch-bank. False as fortress defense. |

## Audience jobs (inference)

| They already play | Job that matches this build | Evidence | Inference |
|---|---|---|---|
| Horde survivor / GnG | Author the swarm; keep banked on death | Beliefs 1 and 4; Prep pays 0 | Stay if they hated idle tags. Leave if they wanted thousands. |
| KF2-style Waves | Gun identity, friends, clear a Wave | Wheel; kinds; Fellowship incomplete | Bounce until friends-alpha if they came for reliable co-op. |
| Extraction | Get value out through a fight | Hatch-bank, no bail-out | Same job, different verb. They will look for an extract button that does not exist. |

## First session the market will meet

If `../Demo/` ships (milestones not cut):

| Beat | They meet |
|---|---|
| 0 | Dead Ship, grant MK-1 |
| 1 | Three Waves, juice after Wave 1 |
| 2 | Hatch-bank overrun; lose some Unbanked |
| 3–5 | ARMORY + wheel (cap **2** while `DemoActive`); FAB dark; NAV skip; doors toward Control Room |
| 6–7 | Master Control is not the win; Hatch finale is Escaped |

Until then, Getting Started is onboarding — and it still says five T1 Waves in places; code is **3** (`FirstTerminal.cs`). Do not quote the stale count (`pitch.md`).
