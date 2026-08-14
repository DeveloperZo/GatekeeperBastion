# Crew, skills and perks

<!--
Purpose: The CREW QUARTERS console — player burst abilities. The RPG attribute tracks and
the unbuilt 5-skill pairing are parked.
Status: brainstorm
Update trigger: when a Crew ability is added, or when the four tracks are deleted or revived.
Last updated: 2026-08-13
-->

## What this covers

Everything about the player rather than the weapon or the ship: **Crew abilities** (Zo,
2026-08-12) — a strong burst over a brief window with a moderate-to-long cooldown. The four
attribute tracks and the 5-skill pairing in `../Design/3-Mechanics.md` §3.2 are parked.

## Where it stands

`Game/Ship/CrewQuartersStation.cs` still sells four attribute tracks — Vitality, Stamina,
Scavenging, Luck — each `base * (1 + 0.05 * level)`, five levels, on the
`200 * 2^level` doubling curve. Luck is the game's only crit source. **Intended (Zo,
2026-08-12): this station stops being an RPG sheet.** Session
`Logs/GatekeeperBastion_2026-08-08_17-00-15.log` spent 4,200 on the weapon and 600 on
Vitality; the tracks had no job a weapon cannot do.

The 5-skill system in `../Design/3-Mechanics.md` §3.2 is unbuilt and parked (see below).

**A level is smaller than the measured noise floor.** Attribute purchases are worth
`+4.5%` against a `~20%` run-to-run spread (`../Design/2-Dynamics.md`), so no attribute
purchase is perceptible. Weapon Tiers were rebuilt to clear that floor; attributes were
not. The spread was measured on kill-rate, so among the five it applies directly only to
Luck; the equivalent spreads for damage taken, sprint use, or run gross have never been
measured — the damage-taken one is computable from existing log transcripts.

## Graduation test

One Crew ability written as window, cooldown, and what it refuses — and it is not a weapon
ability (ARMORY, key `F`) and not a payload (FABRICATION, finite, placed). Until that
sentence exists, a new row is a damage number with a cooldown.

## Decided

### CREW QUARTERS sells burst abilities, not attributes (Zo, 2026-08-12)

A Crew ability is a strong power-up over a small-to-brief window with a moderate-to-long
cooldown. **Liked, not ready to graduate** (Zo, 2026-08-13): it is how a Boarder differs
from teammates besides gun and loadout, and it still has to be grounded in a bastion verb
— not a second gun. **Loop:** press, a short window, a long wait. **What would kill it:**
pressed on cooldown every time with no hold-for-a-moment, or one row is the only pick, or
every row is silent +dps (a worse Tier). **Touches:** weapon abilities (`F`), payload
contract, the four tracks, `language.md`. ARMORY is the gun; CREW is the Boarder; FAB is
the drone.

The four tracks and the 5-skill pairing stay **parked**. XP-as-crew-currency stays **cut**.
Do not author a roster until one ability can be written against the graduation test.

## Open — blocked until decided

| Question | Why it blocks | Owner | Recommendation |
|---|---|---|---|
| One equipped Crew ability, or several? | Shop shape, a second cooldown key, co-op stacking. | Zo | One equipped per Boarder, same grain as the drone payload. Station sells a short roster (3–4). |

## Ideas

### Stealth is a layer, not an invisibility flag (Zo, 2026-08-14)

**The rule.** Stealth is a shared visibility layer. Everyone in it sees everyone else in it,
and nobody outside it. The world outside renders as orbs. Nothing else changes: collision,
interaction and shooting all still work. You are simply unseen by things that are not in the
layer, and they are shapes to you.

**The principle that decides every case: whoever chose the shift is the one it favours.** An
enemy that stealthed itself is stronger in there. An enemy you dragged in is weaker. A Cloaker
that follows you in chose to, so it is strong. A Cloaker you force in is prey, which is the
player's counter to the one enemy built to punish this ability.

What the rule buys, none of which needs new systems:

- Stealth stops being a safety button and becomes a change of *which* threats apply.
- The Cloaker gets a second job with no new AI. Normally it is the thing you cannot see; in the
  layer it is the thing you can.
- **Being first stops paying.** A native stealther is strong whether it entered before or after
  you, so the ordering heuristic that dominates other stealth games does not apply here. What
  replaces it is reading whether the layer is already occupied, which is a question with an
  answer you can work for.
- **The Cloaker and the ability already compose into a scouting ritual.** The Cloaker is
  invisible while you stand still and visible while you move, so the correct play before
  cloaking is move, look, commit. Movement is the scan. No new mechanic.
- The Cloaker taxes the ability by existing, so it does work even against a player who never
  cloaks.

**Upgrades change what it does, not how much**, which matters because a percentage is below
this game's noise floor: kill rate sits at 0.61 to 1.08 per second regardless of loadout
(`../Design/2-Dynamics.md`), so `+15% duration` would be imperceptible. Candidates: threat
assessment, which reads identity among the orbs; and dragging a damaged enemy in with you.

Settled in the same conversation: attacking drops you out; exit is instant, because a native
stealther can kill you in there and a wind-up would make being caught a death with no input;
orbs are positionally honest and identity-blind, so you see that twelve things stand between
you and the Hatch but not which are Rams; the Hatch stays readable among the orbs; and a
dragged enemy needs a visible tell so the player can tell which fight they are in.

**Open:** how much of a Wave is layer-side, which sets how often the interaction happens rather
than whether the rule works. And whether this lives here as a Crew burst or on a weapon
(`Sim/Meta/WeaponAbility.cs`).

**Not doing, with the reason:** shifting a *teammate*. Warframe's Limbo is the shipped version
of this ability and one of that game's most disliked frames, entirely because he moves other
players. DE's U23 stopped Stasis freezing allied projectiles and the community summary was that
nobody complains about Limbo any more. Pulling an *enemy* in is Mordekaiser's Realm of Death,
which shipped and works. Enemies yes, allies never.

**Graduation test:** a logged Run in which the player cloaks once with a native stealther
present and once without, and the two read as different decisions. If they feel the same, the
layer is not populated enough and that is a Wave-composition finding, not an ability one.

**Kill it if** players cloak on cooldown regardless of situation, or die in the layer more
often than they escape with it. Either one means it is a reflex or a trap rather than a choice.

### Abilities are defined by what they trade, not what they grant (Zo, 2026-08-14)

The frame the stealth entry came out of. A dull ability is "gain X for N seconds." An
interesting one is a simultaneous trade, and this game already has a shipped example: `M56`'s
turret fires with rounds siphoned from the player's own reserve.

Run through that filter, common abilities stop being generic. **Overclock** trades Hatch
integrity for damage, drawing power from the thing you are defending. **Anchor** trades
mobility for area control: plant, slow everything near you, and you cannot move until you
unplant. **Recall** trades your unbanked salvage for escape, returning you to the Hatch and
dropping the pile where you stood for a Reclaimer to find. **Ping** trades concealment for
information, showing you every Gatekeeper through walls and showing every Gatekeeper exactly
where you are, which in co-op becomes one player lighting themselves up so the others can act.

None of these is picked. The filter is the point.

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
- Mobility is cut (`../Decisions.md`).
- `_meta/_definitions/language.md`: CREW QUARTERS is never called skills. **Crew ability**
  is the working term until a fiction noun earns a row. Weapon **ability** stays the gun's
  `F`.
- Sprint vs chase speed is filed and not a felt problem at the current room size (Zo,
  2026-08-06).
