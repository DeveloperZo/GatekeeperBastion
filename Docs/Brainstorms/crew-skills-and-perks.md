# Crew, skills and perks

<!--
Purpose: The CREW QUARTERS console — player attributes, and the skill/perk system designed but never built.
Status: brainstorm
Update trigger: when an attribute changes, or when the skill pairing model is decided.
Last updated: 2026-08-08
-->

## What this covers

Everything about the player rather than the weapon or the ship: the five attributes, and
the skill system sketched in `../Decisions.md` and never built.

## Where it stands

`Game/Ship/CrewQuartersStation.cs` sells five attribute tracks — Vitality, Stamina,
Engineering, Scavenging, Luck — each `base * (1 + 0.05 * level)`, five levels, on the
`200 * 2^level` doubling curve (6,200 a track, 31,000 all five). Luck is the game's only
source of crit. Engineering is already scheduled for deletion by M23 (planned) — see the
entry below.

A skill system is designed and unbuilt: two slots, a Primary effect, a Secondary effect,
and a passive layer — five families sketched: Leech (health), Reclaimer (ammo/reload),
Marksman (distance and pierce), Breacher (AoE and double damage), Bulwark (defence).
Power budget +20/+10/+8% in `../Decisions.md`.

**A level is smaller than the measured noise floor.** Attribute purchases are worth
`+4.5%` against a `~20%` run-to-run spread (`../Design/2-Dynamics.md`), so no attribute
purchase is perceptible. Weapon Tiers were rebuilt to clear that floor; attributes were
not. The spread was measured on kill-rate, so among the five it applies directly only to
Luck; the equivalent spreads for damage taken, sprint use, or run gross have never been
measured — the damage-taken one is computable from existing log transcripts.

## Graduation test

One skill pair written as numbers — Primary, Secondary and the passive the pairing
produces — with the arithmetic showing the strongest and weakest pairs within the stated
power band. A 2026-08-08 attempt at this test stalled on missing exchange rates, not on
authoring — see *The pricing wall*.

## Ideas

### The spec exists in two incompatible readings
Found by audit 2026-08-08: `../Decisions.md` gives each skill a Passive Link of its
own — "both equipped passives apply" — which is **5 passives to author**; this file
previously said "each Link unique to its pair", which is **10**. They are different
systems (a skill's passive is portable in one, pair-emergent in the other), and the
"ten Links to author" stall belongs only to the second. Which reading is intended is
undecided — Zo's.

### The pricing wall — EV pricing has nothing to price against
The graduation test was attempted with Marksman/Bulwark (the pair needing the fewest new
mechanics: zero and one). Naive EV pricing makes every pair total the same +46% nominal —
zero spread, by construction. Priced in outcomes instead, the band is unbounded under the
only measured exchange rates: the n=3 series in `../Design/2-Dynamics.md` shows +17% throughput
raising arrivals so the two nearly cancel (dps→door ≈ 0), and every valid loss on record
is a Hatch breach with the player alive (player defence → door ≈ 0 in logged conditions).
The stall is not authoring links; it is that pricing needs dps→door and heal→death
exchange rates only logged Runs can supply, and three of five domains (health, AoE,
mitigation) have no baseline stat to take a percentage of. *Unknown — needs play data;
the cheapest instrument is the Wave-clear log line under the health entry.*

### The stat inventory cannot fund either spec reading
Unclaimed, %-shaped, fight-relevant stats in the sim today: ~4–5 clean (weapon
range/falloff, spread, ability cooldown, pierce with caveats), ~8 counting flagged ones
(Secondary regen rate — already multiplied by the MK-1 surge; repair price — M19's
decision). The per-skill reading needs ~10 unique stats, the per-pair reading ~15. No
heal path, no radius damage, and no damage-reduction field exists anywhere in the code;
Marksman is the only family whose domain is fully built and unclaimed. Shapes that fit
the count: 3 skills (needs 6), 4 skills with per-skill passives (8, at the ceiling),
formula-derived links, adjacency-only links on a ring (reuses M21's grammar), or no
passive layer at all. Counts only — the pick is Zo's.

### Per-skill collisions (audit, 2026-08-08)
Leech: building it decides the open no-healing question as a side effect. Reclaimer: a
reload shape doubles the M21 reload spoke — an on-budget +20% skill outdoes the wheel's
entire 35-point reload focus; an ammo shape has no baseline until the decided ammo model
is built; a regen shape multiplies the number Regen Surge already multiplies. Marksman:
+range is worth exactly 0 on the MR-35 (80 m vs a 60.0 m longest sightline on the 30×52
floor), and pierce EV is unmeasurable — no log line records bodies hit per ray. Breacher:
no AoE mechanic exists, and "double" is the pattern M20 rejected as "a second crit source
in all but name"; area damage sold at CREW QUARTERS also crosses the FABRICATION/Utility
boundary. Bulwark: no mitigation stat exists; a Hatch-facing one multiplies Hatch Armor's
budget and sells ship defence on the player console.

### Health does not come back, and nothing says so
There is no healing in a Run — `PlayerHealth` has no in-run heal path (run-start refill,
scene reset, and Dev F5 only), and damage taken on the first Wave is still missing on the
last. Defensible — it is why Vitality and Stamina are attributes at all — but currently a
secret design. Two separable questions:

- **Communicate?** Yes, and it costs nothing — seven wiki locations currently explain the
  one absent sentence of UI. One naming collision to word around: the MK-1's HUD already
  prints "Regen Surge", which regenerates Secondary ammo only.
- **Change?** Open, and there are still zero data points: every valid logged loss is a
  Hatch breach with the player alive, and the Wave-clear log line records Hatch HP but
  omits player HP. Adding `player {hp}/{max}` to that line answers chip-vs-cliff from any
  transcript. Prep-heal price candidates: 2/HP ≈ one attribute level (198 worst-case full
  heal, 3.8% of a measured full clear); 6/HP ≈ a full Hatch repair (594, 11.3%). Shipped
  references for the priced-against-power shape exist (FTL hull at 2 scrap/point is the
  nearest match; Slay the Spire rest-vs-smith; Hades Well); the current shipped state —
  persistent pool, zero recovery — has no exact match (Devil Daggers is no-healing via no
  pool at all). Co-op wrinkle: a player heal sink scales with player count, Hatch repair
  does not, and player HP is not sim state — the seam decision the ownership labels exist
  to force. *Unknown which the game wants — needs the play data above.*

### Engineering is sold while scheduled for deletion
M23 (planned) deletes the track and the rig track by name — no refund, and any save with
≥1 level gets the same free rig, so L1's 200 and L5's 6,200 buy identical compensation.
Until M23 lands, CREW QUARTERS sells up to 6,200 into a track a planned milestone
deletes. Options: sell as today, freeze the row, annotate it. Zo's call; reordering M23
is a roadmap move, not this file's.

### The attribute panel cannot show a player what a level is worth
Each panel shows the current and next value of a track and never the ladder, so a player
is asked to spend on something they can neither preview nor feel. A ladder view needs no
new state — all six values per track are one `MetaUpgrades` call each. Found alongside:
the CREW QUARTERS row and the fight HUD format Scavenging round-to-nearest (`:0`) while
the bank floors — L2 displays 28 and banks 27, L3 displays 29 and banks 28. Three
surfaces round what the bank floors.

### The gate is explained wrong
The locked Tier row reads "survive a death first" (`ArmoryStation.DrawPanel`), but
`MetaUpgrades.UpgradesUnlocked` checks `PodEjections >= 1`, and `RunSim.EndRun` bumps
that counter for both the "player" and "Hatch" causes — so losing the door unlocks Tiers
and nothing says so. Nothing anywhere shows the gated/ungated split in one place either.

### Attributes above the noise floor
At a constant +25% total, one single +25% step is the only equal-step layout that clears
the measured ~20% floor — two equal steps are +11.8% each and do not. Two or more
perceptible steps mean abandoning the cap (2 × +20% = ×1.44 total). And no price
preserves both halves of the recorded trade — "cheap early" and "gets expensive" — under
a single step; that tension is the decision. The floor is only demonstrated for dps-side
tracks; for the others the honest basis would be legibility, with the needed spreads
unmeasured.

### Perks as run modifiers rather than permanent stats
A perk chosen per Run would give the ship a decision at embark, which the NAV MAP currently
lacks. Survey of shipped comps (2026-08-08; sources in the session audit report): every
shipped per-run choice that alters shared run terms is single-authority or team-shared —
DRG mission+hazard at the host terminal (hazard pays +25–133%, Haz5+ toggles +3–5%/level;
warnings +15–50% are imposed, not picked), HD2 difficulty (+25–300%; its operation
modifiers are imposed and pay nothing), RoR2 artifacts (team toggle, rule changes, pay
nothing), Hades heat (solo; a first-clear bounty, not a multiplier). Every shipped
per-player pre-run choice is kit-only (HD2 stratagems, DRG loadout, B4B decks); no
located precedent for a per-player pick that modifies shared run terms. So the idea
forks: a NAV MAP/host shared perk has precedent everywhere; a per-player perk is precedented
only as self-affecting kit — which is the unbuilt skill system with persistence changed
from bought-forever to picked-per-run. Open question: is an embark perk the deferred
deliver-loot opt-in (`../Decisions.md`) approached from the NAV MAP end, or a third wager
beside the siphon? No comp ships three stacked reward wagers.

## Constraints

- The weakest and strongest pair must not differ much in power, or the meta collapses to
  one pairing and the rest is dead content.
- Luck stays the only crit source. Two multiplicative sources of the same currency is how
  one build becomes dominant.
- The reload/clip/damage/fire-rate/ability currencies belong to the M21 wheel, the
  Secondary regen rate is shared with the MK-1 surge, and mobility is cut
  (`../Decisions.md`). A skill effect on any of these doubles an existing owner.
- Skill slots need `MetaState`/`SaveData` fields and a `SaveVersion` bump — they join the
  M21/M23 single-holder queue for that bump.
- `_meta/_definitions/language.md`: "track" is never called skill or perk, and CREW
  QUARTERS is "never called: skills" — shipping the system needs new language rows first.
- The Remnant 2 archetype pairing this was modelled on is **cosmetic** in the original
  (`../Decisions.md`); the mechanical version here is unvalidated by its own
  reference.
- Sprint speed versus a provoked Gatekeeper's chase speed decides whether backing away works,
  and it appears nowhere a player can read it. Filed as known and
  **not** a felt problem at the current room size — it becomes one when the space is large
  enough that crossing it is a decision (Zo, 2026-08-06).
