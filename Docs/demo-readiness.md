<!--
Purpose: The demo-readiness ledger — evidence-based critique of the first-Bastion demo against measured demo practice, and the ordered work list that hardens it. Verdicts cite the research brief (sources inline) and the codebase as it stands.
Status: working
Authority: checklist — each row's Verdict/Owner is actionable; DECIDE rows block on Zo.
Update trigger: when a row executes, when a ruling lands, or when play data contradicts a verdict.
Last updated: 2026-08-25
-->

# Demo readiness

Three research passes (structure, onboarding, quit/conversion — sources at the bottom)
against the demo as authored in `Docs/Demo - Single/` and as built. One table of record.

## The bar, in numbers

- **Median demo playtime is the metric that tracks revenue tier**: 7 min bronze / 18 silver /
  38 gold / 65 diamond (Zukowski, 130-demo survey). Target: **30–45 min median**, build 2–3×
  that in content. Our authored spine (beat 0 → loss 1 → shop → zone 2 → loss 2 → Primary →
  finale → Escaped) pencils to ~30–45 min for a completing player — the right band.
- **The first 120 seconds decide it.** Core verb (shooting) inside 2 minutes; onboarding
  quits never wishlist.
- **Play→wishlist median 19.3%**; most wishlists come from the store page, so the demo's job
  is to not lose the players it gets.
- **One dev tripled median playtime (15→42 min) by defaulting difficulty easier.**
- **Audio is a demo-killer, not polish** (practitioner consensus, Brown first-hand): a silent
  game reads as low-quality; for a horde shooter, missing combat audio is the most punished gap.

## What already holds (verified, no action)

| Finding | Our state |
|---|---|
| Loop-complete + tease ending beats a cliffhanger | **HOLDS.** Escaped → BASTION STRIPPED tally → ship fully opens. Structure is right; CTA row below is the missing half. |
| Failure should pay out (Hades pattern) | **HOLDS.** Both authored losses ARE the unlock ladder: loss 1 opens the wheel + multiplayer; zone-2 resolution sells the Primary. Re-keyed 2026-08-24/25 so both actually fire under self-funded respawns. |
| Replayable > linear; meta-progress between runs | **HOLDS.** Banked salvage, wheel, upgrades, door persistence. |
| Event-triggered, once-latched, never-stacking voice | **HOLDS architecturally.** DemoVoice is event-driven, Lock/Subtitle split, no timer lines. Half the lines are dark — row E1. |
| Risk economy taught by feel, both numbers visible | **HOLDS.** Persistent `Unbanked X · Banked Y` + the respawn-shortfall tell; the carry pays for respawns so the rule is felt. |
| Settings floor | **MOSTLY HOLDS.** Sensitivity, master/music/SFX sliders, fullscreen-with-revert. No rebinds (minor; note in demo FAQ). |
| Loop closure + quit path | **HOLDS.** Verified end to end incl. QUIT TO MENU; suite green (EM 843/6 recorded set, PM 93/0). |

## Execution ledger — ordered by conversion impact

| # | Item | Evidence | Owner | Status |
|---|---|---|---|---|
| E1 | **Wire the seven dark tutorial lines** (`explore`, `terminal_look`, `juice_teach`, `after_terminal`, `hatch_bank_teach`, `hatch_fight`, `repeat_unit`) to their authored triggers (beats 1/2/5). The teaching layer for the economy is written and never plays — the single largest "no idea how to play" leak. | Onboarding brief §5.1; telemetry: tutorial confusion is the biggest single drop | agent | **DONE 2026-08-25** — `DemoTeach.cs` (hosted by GameController on demo runs), triggers per beat tables; `juice_teach` once-per-save via `RealmState.DemoJuiceTaught` |
| E2 | **Placeholder combat audio, minimum set**: enemy-hit confirm, enemy death, hatch struck, player hurt. Synthesized placeholders wired at the shared Effects/Damageable seams; Zo replaces assets later without rewiring. | Quit brief §2 — audio is the most punished gap in a horde shooter | agent | **DONE 2026-08-25** — `Hit-Confirm/Enemy-Down/Hatch-Struck/Player-Hurt.wav` (synthesized) + `CombatSfx`; death/hatch sound rides the `Effects` visuals, hit-confirm on the pierce gate, hurt local-only. Flag: placeholder quality — swap assets in `Resources/Audio/Sfx`, no code moves |
| E3 | **Wishlist/CTA surfaces**: WISHLIST row on MainMenu + pause + BASTION STRIPPED end screen. Steam-overlay call needs Steamworks (absent) — build the surfaces now behind one `DemoBuild` flag, wire overlay when the SDK lands. | Quit brief §4.1–2: highest-evidence conversion hook; must be overlay, not browser | Zo + agent | **DEFERRED 2026-08-25 (Zo)** — no Steamworks for now; E8's text CTA carries the ask |
| E4 | **Visible locked content**: FABRICATION reads as dark/dead in-demo. Relabel to sell breadth ("FABRICATION — FULL VERSION" style) instead of hiding it. Same for locked protocol rows (AEGIS/LEDGER already do this well — Coming Soon in the Compendium). | Structure brief §1: locked-content visibility telegraphs breadth (Brown, Death Trash) | DECIDE (copy is Zo's) | **DONE 2026-08-25** — default copy taken: FABRICATION is visible and locked, prompt "FABRICATION — FULL VERSION" (DemoStationGate) |
| E5 | **Cloaker debut is unreadable**: it debuts in wave 3 alongside the Thief inside a 42-body mix; its rule (freezes when watched) cannot be read in a crowd. Recommend: debut waves stagger — Thief w3, Cloaker w4, with the Cloaker's first appearance a small group so the freeze is witnessed. One `WaveDirector` schedule change. | Onboarding brief §2: one debut per encounter; the refusal must be demonstrable | DECIDE (tuning) | **DONE 2026-08-25** — recommendation taken: Cloaker debut moved to wave 4 via `CloakerPerWave`; the 2026-08-27 roster cut halved that row, so the debut is now a single Cloaker rather than a pair — the freeze is easier to read, not harder. The live values are in `WaveDirector`, deliberately not restated here; Thief keeps wave 3; pins + Field-Manual + Compendium updated |
| E6 | **Time-to-gun in beat 0**: two interaction chores precede the weapon. DOOM bar: shooting in ~20s; practitioner bar: real combat < 2 min. Measure beat 0 wall-clock; if teleporter+NAV+terminal exceed ~90s for a first-timer, tighten (shorter NAV hold, closer walk) rather than restructure. | Onboarding brief §1; quit brief §1 (first-120-seconds) | DECIDE (beat spec is locked) | **DONE 2026-08-25 (ARMORY RECUT — Zo, supersedes the armed-start ruling)** — fresh campaign starts unarmed; terminal routes power and points at the rack (demo.armory_call); the ARMORY's free MK-1 buy is the arming beat and alone readies embark; the ship intruder fight is STRUCK (demo.intruders retired). Beat: wake → NAV → terminal → ARMORY → EMBARK |
| E7 | **First-loss fairness read**: authored loss 1 is the design (and pays out per Hades pattern) — but the overwhelm must read authored, not unfair. Verify: pre-loss field waves easy (real log: w1 cleared by unupgraded player ✓), loss cause overwhelmingly the HATCH (respawn-proof), first_hatch_down line lands on return ✓ (re-keyed). Consider an explicit framing line at the overwhelm's start. | Quit brief §3 (difficulty walls cluster quits); structure brief §1 (err easier) | Zo playtest | **OPEN — Zo playtesting** (ruled 2026-08-25) |
| E8 | **Escaped end screen CTA**: add the "thanks / what's in the full game / wishlist" beat to BASTION STRIPPED (demo build only). Pairs with E3. | Quit brief §4.2 — demos that just stop waste their best moment | Zo + agent | **DONE 2026-08-25** — text CTA on BASTION STRIPPED (EndScreenView._ctaLabel, lands with the RETURN button): thanks + full-game breadth + wishlist line; overlay button waits for Steamworks |
| E9 | **Milestone telemetry**: session logs already record every beat/wave/death (wave-report reads them). For a Steam demo, decide whether Steamworks stats mirror the same milestones. | Quit brief §5.1 | DECIDE | **DEFERRED 2026-08-25 (Zo)** |
| E10 | **Difficulty default**: no difficulty setting exists; the demo IS the default. The 15→42-min datum argues for softening the FIELD waves pre-loss-1 only (the authored losses stay). Tuning lever: wave 1–2 body counts or enemy HP at ante 0. | Quit brief §3 | DECIDE (tuning) | **DEFERRED 2026-08-25 (Zo)** |
| — | Known defect backlog folded in: CooldownFill sprite, Pause Shortcuts GO, drawer truncation, 22px cards, Ante payoff invisible, hatch damage-state visual, telegraphs, directional damage. All previously audited; each is a legibility leak inside the first 15 min. | All three briefs | agent (canvas items need editor idle) | **PART-DONE 2026-08-25** (visual-inspection pass): Protocol-bar CooldownFills have their sprite (both scenes + builder); stairs rematerialed + deck holes closed; HUD pass (score card width/font floor, DOWN overlay rect + crosshair hide, drawer rows 100→44, no-wrap state labels, ammo fs 14→30, card spacing, end-screen rects). STILL OPEN: Pause Shortcuts GO + shortcut-template CooldownFill children (need a `GatekeepersProtocol/Build` re-run in the editor), drawer two-column split, Ante payoff, hatch damage visual, telegraphs, directional damage |

## What the demo deliberately withholds (and should keep withholding)

Breadth, not depth: one bastion, the Secondary tier + one Primary, Default protocol only,
FABRICATION dark (relabel per E4), co-op locked until the first purchase. This matches the
DRG:S/Word Play pattern — full loop, sliced horizon. Do not add content to the demo; add
visibility of what's locked.

## Sources

Zukowski / howtomarketagame.com (playtime benchmark; wishlist rates 2026; demo ideas; demo-effect
case study; Feb 2026 Next Fest analysis) · GMTK/Mark Brown Next Fest guide + postmortems ·
FREERUNNERS postmortem (cbgamedev.com) · Valve L4D design commentary · George Fan GDC 2012 ·
Supergiant writing blog + Kasavin GDC · Doom 2016 design analyses · presskit.gg · Steam Page
Analyzer · gamineai telemetry guide. Full links in the session's research briefs.
