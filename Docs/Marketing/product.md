# Product

<!--
Purpose: What this game is, as a buyer would meet it, with play evidence.
Not a store page. Numbers and verdicts are cited; unsourced "fun" is omitted.
Status: working
Update trigger: when a playthrough verdict changes the first-session read, or
when a milestone changes a pitch gap.
Last updated: 2026-08-30 (0.34.2: Hatch-start loop, Ante parked, kills bank on landing,
respawn from realm bank, one-deck hull; Play path; 2026-08-25 and 2026-08-29 sessions)
-->

## Session

Ship (one deck, four consoles, three hatches) → embark NAV MAP → Bastion already
tethered at the Hatch → quiet Prep until `T` at the Hatch starts the zone's composite
→ Assault until `T` at the far terminal shuts the zone off (kills already banked;
trickle pays nothing) → Prep again → lost (the Boarder down with no respawn left, or
Hatch 0) or Escaped (Master Control survived) or Docked (hold Interact after a shutoff
or a survived Convert).
`../Play Mechanics/Getting-Started.md`, `../Play Mechanics/A-Boarding.md`.

One destination: **ALPHA-7**. `SceneFlow.BastionName`. The combat scene behind it is
`Bastion_Coop` — `M95` replaced the old solo bastion rather than adding a second.

Hold Interact on the authored pad beside T2 / T5 teleports to the Hatch
(`HatchRecallPad`, 1.5 s). The Hatch offers Master Control only when
`ControlRoomReachable` is true; while a zone is done and the control gate is still
shut, Convert / Withdraw can answer (`../Play Mechanics/The-Hatch.md`).

## What is unusual (felt in a first Boarding)

| Mechanic | What the player does | Source |
|---|---|---|
| You start the clock | Nothing arrives until `T` at the Hatch. A Wave **begins at the Hatch and ends at the terminal** | `../Play Mechanics/Getting-Started.md`; belief 4 in Design |
| Salvage is loot-on-kill | A kill credits the realm's bank at face value the moment the body falls. No ending takes it back. Withdraw at the Hatch is the only way to put money at risk | `../Play Mechanics/Salvage.md`; `RunSim.AwardKill` |
| Kinds refuse | Hunter never Hatch; Thief never turns when shot and never hurts you; the Cloaker will not advance while you are looking at it | `../Play Mechanics/Gatekeepers.md`, `../Play Mechanics/Gatekeeper-Cloaker.md` |
| The weapon is the build | Per-weapon wheel after the first lifetime Salvage; five shop rows, three of them Primaries that read as three different guns since `M93` | `../Play Mechanics/Weapons.md`; unlock is `MetaUpgrades.UpgradesUnlocked` → `LifetimeSalvageEarned > 0` |
| Protocol | Default claim 0 Salvage: Barrage `G`, Rush `H`. VEIL and FORGE claimable since `M94`; FORGE stands a charged turret and an Outrider. AEGIS and LEDGER Coming Soon | `../Play Mechanics/Protocols.md`, `../Play Mechanics/Protocol-FORGE.md` |
| Death has a price, a cap, and a clock | A dead seat buys back from the **realm's bank**, `150 × min(taken + 1, 3)`, at most **2** respawns this Boarding, **10 s** to confirm. Carrying nothing is no longer what ends it — a spent bank, or the third death, is | `../Play Mechanics/Going-Down.md`; `Respawns.cs` |

The Ante (per-Wave wager) is **parked** in this build (`Ante.Enabled = false`,
2026-08-30). Waves run at intensity 0. Do not list it as a first-Boarding verb.

Body count is small on purpose. Groups with a gap, not a stream
(`../Design/3-Mechanics.md`). Dial-0 solo is **11 / 18 / 25 / 32 / 39**
(`WaveDirector.BaseEnemyCount = 11`, `CountPerWave = 7`). Marketing that sells
"deluge" is describing a different game (`landscape.md`).

## Play evidence (quality)

Only `../GatekeeperZo Notes/playthrough.md`. Six that constrain marketing:

| Date | What | Verdict | Marketing implication |
|---|---|---|---|
| 2026-08-07 | Five-Wave clear, MR-35 T4, start Wave 2 | **fun and semi-challenging** | Loop can read as a shooter people want — at an **upgraded** loadout. Log `GatekeeperBastion_2026-08-07_16-34-30.log`. Hatch 300/300 every Wave of that clear. |
| 2026-08-09 | Wave 3 deep start, MR-35 T9 | Zo **report**: still felt easy; formal verdict **pending** | Mid-game loadout is not the first night. Do not trailer T9 as the product. |
| 2026-08-13 | M53 Convert | **hatch wave worked pretty well**; run ending after it was the miss (sim fixed, not replayed) | Convert fight is sellable **as an opt-in Hatch bet**, not as the payroll. |
| 2026-08-16 | Solo, starting loadout, no log `[unconfirmed]` | **too hard as single player**; multiple Waves then overrun at the Hatch, lose **some** Salvage | "Too hard first night" still constrains. "Lose some Salvage" was the Unbanked-until-Convert model, which inverted on 2026-08-25. |
| 2026-08-25 | Spine, ALPHA-7, MK-1 T2, no Primary, Ante 0 | `result=lost cause=player wave=2 duration=108 respawns=2 banked=227`. Verdict **pending**. Log `Logs/GatekeepersProtocol_2026-08-25_19-28-58_p63308.log` | Starting kit lost zone 1's composite with 25 live and the respawn ladder feeding it. Do not market an easy first night. Soft-lock (227 banked, cheapest Primary 900, rack behind zone 2) is `[unconfirmed]`. |
| 2026-08-29 | FORGE, MR-35 T2, Hatch lost on wave 8 | Verdict **pending**. Log `Logs/GatekeepersProtocol_2026-08-29_22-53-02_p14936.log` | FORGE's machines shipped enough to play. 237 kills, 230 with Primary; machines absorbed **0** damage; EMP 6 of 8 caught nobody. Do not trailer the Outrider as the thing that holds the room. |

No other verdict is a reception claim. Two Wave jobs already ship: a field Wave hunts
**you** (and can nick the Hatch at 0.15 scale); a Convert hunts the Hatch
(`../Play Mechanics/A-Boarding.md`). Copy may name both jobs. A dungeon crawl still
oversells camping.

## Gaps vs locked pitch (`pitch.md`)

| Pitch | Product |
|---|---|
| Alone | True. |
| With friends | Host/Join + shared Ship (`M72` closed 2026-08-17, Relay `KPRMPR`), plus a realm both can hold (`M96`, `M100`) and a late arrival who follows the party in at a break (`M98`). Five-Wave friends-alpha dry-run still open, and `M100` is unproven on two machines. **0.34.2** is a zip of that lobby, not of the dry-run. |
| Defeat the Gatekeepers | True as the fight, four kinds since the Cloaker; Escaped is Master Control, not Wave 5. A field Wave ends at the terminal shutoff. |
| Their Protocols | False until a Gatekeeper wears a pool modifier, diluted (`boarder-protocols.md`). Default, VEIL and FORGE are all **your** concentrated sheets. |
| Hold the Hatch | True as Hatch defense. Convert is an opt-in Withdraw bet, struck from the demo. |
| Salvage banks as they fall | True. Kills credit the realm bank on landing (`RunSim.AwardKill`). |

## Audience jobs (inference)

| They already play | Job that matches this build | Evidence | Inference |
|---|---|---|---|
| Horde survivor / GnG | Start every fight; keep banked on death | Belief 4; Prep pays 0; `AwardKill` banks on landing | Stay if they hated idle tags. Leave if they wanted thousands. The Ante that used to let them author the swarm is parked. |
| KF2-style Waves | Gun identity, friends, clear a Wave | Wheel; kinds; Fellowship incomplete | Bounce until friends-alpha if they came for reliable co-op. |
| Extraction | Get value out through a fight | Convert is optional on a Withdraw; kills are already safe | They will look for an extract button. The one exit that ends Docked is holding interact at the Hatch after a shutoff or a *survived* Convert (`M77`). Do not use extract-shelf verbs — that shelf is Arc Raiders / Marathon (`landscape.md`). |

## First session the market will meet

`../Play Mechanics/Getting-Started.md` is the walkthrough a 0.34.2 player can follow.
`../Demo - Single/` still specifies Ante-teach and Convert-as-payroll; those beats have
not been recut against the Hatch-start loop.

| Beat | They meet |
|---|---|
| Ship | REALMS → unseal → ARMORY MK-1 → CREW if the panel opens → NAV MAP EMBARK. Tutorial skippable on later realms since 2026-08-24, off by default. FABRICATION stays locked "FULL VERSION" while `DemoActive` (`DemoStationGate`) |
| Bastion | `T` at the Hatch. Walk the corridor. `T` at the terminal. Kills already banked. Dock on `E`, or `T` at the Hatch for the next stretch. Zone 1 is **2** Waves, zone 2 is **3** (`DemoZones`) |
| Finale | Field done: `T` at the Hatch is Master Control when the control gate is open. Kill the oversized Gatekeeper (arms the switch). `T` at Master. **BASTION STRIPPED** |

Until Demo beats 0–7 are recut, Getting Started is onboarding. **Quote no Ante number,
no Convert-slot number, and no T1 Wave count from language.md** — see `gaps.md`.
