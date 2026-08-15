# Crew, skills and perks

<!--
Purpose: The CREW QUARTERS console — the player beyond weapon and ship. The burst-ability
system graduated to M70; the RPG attribute tracks and the unbuilt 5-skill pairing are parked.
Status: brainstorm
Update trigger: when M70 ships, or when a parked entry is revived or a remaining entry graduates.
Last updated: 2026-08-14
-->

## What this covers

Everything about the player rather than the weapon or the ship. The Crew-ability system and
its starter roster graduated to
[`M70`](../Milestones/1.%20planned/M70-the-boarder-gets-a-verb.md) (Zo, 2026-08-14). What
remains here: the parked attribute tracks and 5-skill record, the health/no-healing
question, and perks as run modifiers.

## Where it stands

`Game/Ship/CrewQuartersStation.cs` still sells four attribute tracks — Vitality, Stamina,
Scavenging, Luck — each `base * (1 + 0.05 * level)`, five levels, on the
`200 * 2^level` doubling curve. Luck is the game's only crit source. The conversion to
Crew abilities, including what happens to the tracks and to saves holding levels in them,
is `M70` (planned, not built).

The 5-skill system in `../Design/3-Mechanics.md` §3.2 is unbuilt and parked (see below).

**A level is smaller than the measured noise floor.** Attribute purchases are worth
`+4.5%` against a `~20%` run-to-run spread (`../Design/2-Dynamics.md`), so no attribute
purchase is perceptible. Weapon Tiers were rebuilt to clear that floor; attributes were
not. The spread was measured on kill-rate, so among the five it applies directly only to
Luck; the equivalent spreads for damage taken, sprint use, or run gross have never been
measured — the damage-taken one is computable from existing log transcripts.

## Graduation test

For the remaining entries: the health entry needs the `player {hp}/{max}` Wave-clear log
line and Runs read through it; the perks entry needs the NAV-MAP-fork question under it
answered. Nothing else here is mineable.

## Decided

### CREW QUARTERS sells burst abilities, not attributes (Zo, 2026-08-12)

**Graduated to [`M70`](../Milestones/1.%20planned/M70-the-boarder-gets-a-verb.md)**
(Zo, 2026-08-14) — system, starter roster, kill criteria and the fate of the four tracks
are decided there. The milestone is the record.

The four tracks and the 5-skill pairing stay **parked** as record below.
XP-as-crew-currency stays **cut**.

## Open — blocked until decided

Nothing open. The one-equipped-or-several question was answered in `M70` first as
one-equipped, then recut 2026-08-14 (Zo) to three slots — Crew (`G`, outward), Personal
(`H`, on you), Passive (a rule, never a number) — with the overturn reasoning recorded on
that file.

## Ideas

### Stealth layer and the trade filter — graduated to M70 (2026-08-14)

The stealth-layer entry (shared visibility layer, whoever-chose-the-shift-is-favoured, the
Cloaker interplay, the Limbo/Mordekaiser comp conclusions, its graduation test and kill
criteria) and the trade-filter entry (Overclock, Anchor, Recall, Ping) both moved whole
into [`M70`](../Milestones/1.%20planned/M70-the-boarder-gets-a-verb.md). The milestone is
the record: roster picked there, Overclock rejected there by name, the layer-side
Wave-composition question carried there as open.

### Experience as the CREW QUARTERS currency (Zo, 2026-08-12)

**Cut the same day.** CREW now has a job (burst abilities). A second wallet was the wrong
lever; see Decided.

### Specialization with a stark hole (Zo, 2026-08-12)

Walk back “never powerful as the player” toward: power on one axis guarantees a hole on
another. Three players may stack the same axis; do not balance that away. The wheel already
runs this. Crew abilities are a *window* of power, not a permanent hole-grammar — the
cooldown *is* the hole. Do not author a second pair-flat skill system beside them.

### Parked — 5-skill pairing (audit 2026-08-08)

Superseded by Crew abilities (Decided, 2026-08-12). Kept so the pricing-wall and stat-inventory
findings are not rediscovered. Do not graduate any of these entries.

### The spec exists in two incompatible readings
Found by audit 2026-08-08: `../Design/3-Mechanics.md` section 3.2 gives each skill a Passive
Link of its own — "both equipped passives apply" — which is **5 passives to author**; this file
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
**Closed — M23 shipped.** The track is gone from `UpgradeTrack`; legacy save fields migrate
to a free equipment slot + Repair Rig. Do not revive the name (`../_meta/_definitions/language.md`,
trap 1).

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

- A Crew ability is not a weapon ability and not a payload. If it can be described as a
  gun effect or a placed drone item, it belongs on that console.
- Luck stays the only crit source.
- The reload/clip/damage/fire-rate currencies belong to the M21 wheel. A Crew ability that
  is a silent +dps during its window is a worse version of a Tier.
- Mobility is cut (`../Decisions.md`) — the cut governs permanent movement stats. `M70`
  argues the verdict's 30 × 52 m premise expired for a priced burst (Recall); Zo can veto
  there.
- `_meta/_definitions/language.md`: CREW QUARTERS is never called skills. **Crew ability**
  is the working term until a fiction noun earns a row. Weapon **ability** stays the gun's
  `F`.
- Sprint vs chase speed is filed and not a felt problem at the current room size (Zo,
  2026-08-06).
