# Language

<!--
Purpose: the ubiquitous language of Gatekeeper Bastion. One name per thing, used in code,
in builder docs, in player docs and in the game's own text. This file is the authority.
Status: working
Update trigger: when a term is added, renamed, or retired, or when player-facing voice rules change.
Last updated: 2026-08-16
-->

**One thing, one name, everywhere.** The same word in the code, in `Docs/`, in
`Docs/Play/`, in the HUD, in a log line, and in anything a player ever reads. A concept
with two names is a concept nobody can search for, and it is how a codebase and its
documentation start describing two different games.

This is a standing rule, not a style preference. It applies to agents and to the developer
equally, and it applies the moment a name is chosen — renaming later costs a sweep of
every file above.

## Who is who

The title names both sides. **Gatekeepers** hold a **Bastion**. The player is the
**Boarder** who cuts into it, holds the Hatch behind them, and strips it.

This inverts the reading the code grew up with, where "boarder" meant the thing attacking
you. It does not any more. The player boards; the Gatekeepers repel. Any sentence written
before 2026-08-08 that uses "boarder" for an enemy is wrong, not merely old.

## The rules

**1. If it is in this table, use this word.** Not a synonym, not a shortened form, not a
more evocative one. The Hatch is not "the door", "the gate", "the objective" or "the
junction" in any sentence a player or an agent will read.

**2. If it is not in this table, do not coin one.** Inventing shorthand and then using it
as though it were agreed is the failure this file exists to stop. Say the plain thing:
*"the Primary's job is killing them before they reach the Hatch"* beats any abstraction
that compresses it. If a term genuinely earns its keep, it earns a row here first.

**3. Analysis vocabulary is builder-only and never appears in player docs.**
`gatekeeper-seconds`, `arrivals x dwell`, `mean live` and `mean lifetime` are defined in
`../../Design/3-Mechanics.md` (and mean live also in `../../Design/1-Experience.md`,
section 1.3); `realization` is used in `../../Design/2-Dynamics.md`; `hatchDist` and
`spawnDist` are kill-line metres read by `Tools/wave-report`. A player has no way
to look any of them up, so a page in `Docs/Play/` that uses one has failed. Checked
2026-08-08: none leak; re-check when M28 runs.

**4. Code casing follows the code's own conventions; the *word* stays the same.**
`GatekeeperKind`, `RunPhase.Tethered`, `MetaState.BankedSalvage` — the identifier can be
`PascalCase`, but it is never a different noun from the one below.

**5. A rename is one commit, everywhere.** Code, `Docs/`, `Docs/Play/`, and this table
together. A half-done rename is worse than the old name.

**6. Not everything needs a row.** A state or event the player understands from plain
English does not earn vocabulary. A lost run is lost; it is not given a proper noun. Rule
2 cuts both ways — the table is a list of terms that had to exist, not a glossary of
everything the game contains.

## The table

### The loop

| Term | Means | Never called |
|---|---|---|
| **Run** | One trip from the Ship to the Bastion and back, ending in a clear or a loss. | mission, level, raid, expedition |
| **the Ship** | The player's own vessel. The hub between Runs. | base, home, hub *(in player-facing text)* |
| **Bastion** | The fortified vessel the Gatekeepers hold and the player boards. Not a wreck — it is garrisoned, which is why Gatekeepers keep arriving. | derelict *(retired 2026-08-08)*, station, dungeon |
| **Boarder** | The player. The one who cuts in. | operative, marine, survivor, defender |
| **the tether** | The line from the Ship to the Bastion. What the siphon runs along, and what the Hatch anchors. | cable, line, connection |
| **Hatch** | The umbilical junction back to the Ship. The Boarder's way out, and the thing they must hold. | door, gate, objective, junction |
| **embark** | Leaving the Ship for the Bastion, from the NAV MAP. | deploy, launch, start mission |
| **Prep** | The quiet state between Waves. Untimed, pays nothing, the only time juice raises intensity. | downtime, intermission, break |
| **Assault** | The state while a Wave is live. | combat, the fight |
| **the Siphon Draw** | Retired as the Wave-start verb (M52). Prep `T` starts the selected trigger (Field, Master, Hatch-bank). `T` without a select is a logged refusal. | spawn, trigger, call the wave, the draw *(alone)* |
| **Wave** | One delivery of Gatekeepers. `+/-` juice, `E` selects, `T` starts at a **field terminal**. A terminal opens its door after its authored count (T1 is 5). Prep `E` then `T` at the Hatch is Hatch-bank. | round, horde |
| **field terminal** | A bastion console marked **T**. `E` selects intensity; `T` starts one Boarder-hunt Wave. N clears complete it and open its **D**. | station *(alone)*, objective, console *(the Ship's four are stations)* |
| **cleared** / **stripped** | Survived Master Control's Hatch-bank half. Field Waves beaten is field stripped, not Escaped. | won, completed, extracted |
| **Escaped** | Terminal, good: Master Control's Hatch-bank half survived. Wave 5 does not Escaped. | won, victory |
| **Master Control** | Control Room console. `E` then `T` starts 5 terminal Waves (door sealed) then 5 Hatch-bank windows (door open). That Hatch-bank does not spend a mid-Run slot. | win terminal, extract console, D1 *(the door is not the win)* |

A Run that ends badly is **lost**: the Boarder went down, or the Hatch fell. It is listed
under Run phases below only because `RunPhase.Lost` is a real enum value the code branches
on. In prose, treat it as the plain English word it is and do not capitalise it. *Podded is
retired: struck 2026-08-08.*

### Run phases

`RunPhase` is the code's spine and every value is player-visible somewhere, so all of them
are named here.

| Term | Means | Never called |
|---|---|---|
| **Docked** | Aboard the Ship. Not yet boarded. Solo hub, or a friends session's shared Ship. | menu, lobby, idle |
| **Tethered** | Aboard the Bastion with the tether live. The whole fight happens here. | in-run, active, boarded |
| **Spooling** | Reserved, unused in v0 — the escape window after the tether is cut. | *(do not use it for anything else)* |
| **Escaped** | Terminal, good: Master Control's Hatch-bank half survived. Wave 5 does not Escaped. | won, victory |
| **Lost** | Terminal, bad: the Boarder went down or the Hatch fell. | podded *(retired)*, death, game over, wipe, fail |

### What comes at you

| Term | Means | Never called |
|---|---|---|
| **Gatekeeper** | Any enemy. They hold the Bastion; you are the intruder. | enemy, mob, creature, hostile, boarder *(that is the player now)* |
| **Default** | The Gatekeeper that marches on the Hatch and can be provoked onto the Boarder. | grunt, basic, marcher *(descriptive prose may say "marcher"; the kind's name is Default)* |
| **Hunter** | The Gatekeeper that only ever wants the Boarder. | chaser, stalker |
| **Thief** | The Gatekeeper that hunts the Boarder on a field-terminal Wave and the Hatch on a Hatch-bank Wave; shooting it never shouts. Code identifier is still `Ram` until M63. | brute, tank, charger, Ram *(player-facing)* |
| **spawn point** | Where a group of Gatekeepers enters the fight. They garrison the Bastion; they do not breach it. | breach point *(retired 2026-08-08: the Boarder is the one who breaches)*, muster point *(coined and struck the same day; do not reintroduce)*, portal, door |
| **group** | The unit Gatekeepers arrive in — one spawn point, several bodies, at once. | pack, squad, cluster |
| **provoke** | To make a Default target the Boarder. Permanent for that Gatekeeper's life. | aggro *(builder shorthand only, never player-facing)* |
| **shout** | A provoked Gatekeeper pulling its neighbours onto the Boarder. Does not relay. | alert, call, aggro chain |

### What you carry

| Term | Means | Never called |
|---|---|---|
| **Primary** | The earned weapon slot. Heavy damage. | main, rifle slot |
| **Secondary** | The starting weapon slot. Never runs dry. | sidearm slot *(the MK-1 is a sidearm; the slot is the Secondary)*, backup |
| **Drone** | The third weapon slot. Holds the FABRICATION drone, one payload per Run, player-activated. | Utility *(retired 2026-08-08)*, melee *(no melee slot exists)*, gadget, tool, third slot |
| **Equipment** | Gear bought at FABRICATION into the equipment slot (unlocked after first loss, bought once). The Repair Rig is the first piece. | payload *(that is the Drone's)*, item, kit, Utility |
| **Tier** | A step on a weapon's upgrade track. | level, rank, mark, upgrade |
| **ability** | A weapon's one activated effect, on a cooldown. | special, power, skill, ult |
| **Boarder Protocol** | The named sheet CREW QUARTERS sells: one doctrine, one Crew ability, one Personal ability. Default is a 0-salvage claim; specialists show as Coming Soon until their slice. Working name **DEFAULT** — renameable. | archetype, class, kit, loadout *(loadout is weapons)* |
| **doctrine** | How a protocol treats hardware at decoy place. **STOCK** (working name, renameable) is hardware as sold. Written at `TryPlace`, not on repair. | stance, mode-as-flavour |
| **Crew ability** | A Boarder-owned burst on `G`: brief window, moderate-to-long cooldown. Default sheet: **Barrage** (working name, renameable). | skill, perk, track, weapon ability |
| **Personal ability** | A Boarder-owned burst on `H`: brief window, long cooldown, for the presser. Default sheet: **Rush** (working name, renameable). | skill, perk, ult, weapon ability |
| **Passive** | Always-on protocol rule. Reserved — Default has none. Specialist sheets pick one later. A rule, never a number. | stat, track, perk |
| **twist** | Reserved protocol modifier. Ships later. | mutator, relic, charm |
| **loadout** | The Primary, Secondary and Drone a Boarder embarks with, plus their Equipment. | kit, gear, build |
| **improvised cutter** | What a Boarder fights with having bought no weapon at all. Deliberately bad. | salvage cutter *(the code's old name)*, fists, melee, starter weapon |
| **crit** | A critical hit. Chance is 0; the Luck track is gone. | critical strike, headshot |
| **upgrade wheel** | The ARMORY's five-spoke upgrade selector. *Built (M21) — every shop weapon has a ring; SEC-D Ability is not purchasable.* | the wheel *(alone)*, carousel *(the shape is a wheel; the ring is undirected)*, tree |
| **upgrade spoke** | One of the upgrade wheel's five attributes. | node, slot, stat |
| **upgrade focus** | The upgrade spoke the player centres before buying. | specialisation, path, build *(a build is the result, not the act)* |
| **clip** | Rounds in the weapon now. | magazine, mag, ammo |

Every row here names something that ships today. When a row must be added for something
only designed, mark it *Designed, not built* with the milestone that owns it, and delete the
mark in the commit that builds it.

### The economy

| Term | Means | Never called |
|---|---|---|
| **Salvage** | The only currency. Kills fill **unbanked**; only a **Hatch-bank** converts it to the ship. | credits, scrap, cash, resources |
| **unbanked** | Salvage earned this Run that has not gone through the Hatch. Lost on down or Hatch breach. | floating, pending, at-risk |
| **Hatch-bank** | Prep `E` then `T` while looking at the Hatch: a Wave that converts unbanked to the ship. Capped per Run. | extract, cash-out, deposit |
| **banked** | Salvage on the ship that the player can spend. | stored, saved |
| **intensity** | The per-Wave wager 0–5: more Salvage per Gatekeeper, more Gatekeepers. | siphon dial, difficulty, risk slider, multiplier *(as a name for this)* |
| **juice** | Paying Unbanked at a trigger to raise intensity. | siphon, buy difficulty, map device |
| **track** | A permanent upgrade line at FABRICATION (Hatch Armor). | skill, perk, stat line |
| **party** | Connected seats in a friends session. | group, team |
| **party lead** | The host. Owns NAV EMBARK. | leader, captain |
| **Ready** | Guest session control before party EMBARK. | prepared, set |
| **join code** | Relay code shown on Host, typed at Join. | room code, lobby code |
| **shared Ship** | The one Ship instance a friends session stands on. | lobby *(player-facing)* |

### The tracks

Every track is on screen and in the wiki, so every track is named here.

| Term | Owns | Bought at |
|---|---|---|
| **Hatch Armor** | The Hatch's maximum HP. | FABRICATION |

### The stations

Written in caps in player-facing text, matching what the player reads on the console.

| Term | Owns | Never called |
|---|---|---|
| **ARMORY** | The weapon in your hands. | weapons bay, shop, Armory *(caps, always)* |
| **CREW QUARTERS** | The Boarder's protocol — doctrine, Crew and Personal abilities. Default is a claim; other protocols Coming Soon. | barracks, skills, attributes |
| **FABRICATION** | The Ship's gear and the Hatch. | workshop |
| **NAV MAP** | Where you go, and going there. | map, bridge, terminal |
| **Compendium** | The Boarder's working knowledge of the game: volumes of related entries (situation, Run, Bastion, garrison, arms, hull), not a glossary of terms. Markdown lives in `Docs/Play/` (M28 language lint). In-game reader shipped (`M62`, Esc → Compendium). | Codex, encyclopedia, wiki, lore bible, Hyrule Compendium, Persona Compendium |

## Player-facing voice

Applies to `Docs/Play/` and to any string a Boarder reads in the build. Builder docs keep
the agent voice in `AGENTS.md`. This register will move as the game does; changing it is
an edit to this section, in the same change as the pages it recuts.

The Compendium is working knowledge, written as the Boarder already in the place. Form
references are builder-only (never those titles in player text): Mass Effect's Codex
(volumes, Primary vs appendix) and God of War 2018's journal (practical and world in the
same pause entry).

**Dimensions.** Each hand-written Play page (not `README.md`, not generated
`Field-Manual.md`) holds every dimension of the topic in one file:

| Heading | Who reads it | In the pause reader |
|---|---|---|
| `## Game` | Boarder, one pause | **Shown** (plus title and `## See also`) |
| `## Reference` | Builder / fuller brief | Omitted |
| `## See also` | Both | Shown (navigation) |
| `## Gaps` | Builder audit | Stripped at copy and at load |

Test a Play page's Game dimension (skip HTML comments, Reference, and Gaps). One hit fails.

| # | Rule | Good | Banned |
|---|---|---|---|
| 1 | Person is *you* / the Boarder. No *I*, named diarist, or "the player". | "You cut in. They are already home." | "I've watched them stomp…" / "The player should…" |
| 2 | Sentence one is a situating fact in this world. | "The Thief is walking to the Hatch." | "A first-person Wave-defense game…" |
| 3 | The first paragraph states a playable rule the HUD does not print. | "Shooting it does not provoke it." | Opening that only restates HP, clip, or a key already on pause. |
| 4 | Numbers and bindings only by linking Field Manual. | "How many: [Field Manual](Field-Manual.md)." | "The Thief has 180 HP." / "Press F." |
| 5 | No milestone IDs, "not built", or builder paths in the visible body. | The page describes what ships. | "M23 shipped the Repair Rig." |
| 6 | No never-called terms from the table above. | Hatch, Gatekeeper, siphon draw. | Codex, encyclopedia, wiki, enemy, aggro, mission. |
| 7 | Humor is the situation's exactness. No pun, slang persona, or pop-culture aside. | "You are scenery." | "Think of it as the tank from every horde game." |
| 8 | Fiction is a frame, not a plot. No byline, Day N, or origin myth. | "Gatekeepers hold the Bastion." | "Day 12. The garrison still will not yield. — J." |
| 9 | `## See also` names at least one other Play page. The page is not a glossary stub. | Thief → Hatch, Waves. | "Thief — a large Gatekeeper." as the whole page. |
| 10 | No analysis vocabulary (rule 3). | "A group arrives together." | `gatekeeper-seconds`, `hatchDist`. |
| 11 | Visible body starts as player prose. HTML comments and `## Gaps` are not the opening. | First readable line is the title, then the world. | Purpose / Status as the thing a Boarder reads. |
| 12 | Do not teach by citing another game or by riddle. | "They arrive from garrison to repel an intrusion." | "Like a Left 4 Dead tank…" |
| 13 | `## Game` is the pause dimension; `## Reference` holds the fuller brief. | Game then Reference then See also. | One undifferentiated body the reader dumps whole. |

Always-on for friends alpha: every shipped entry is readable from the first pause. Unlock-
on-encounter, a companion diary, a completion ledger, and a stat-block-as-entry are
refused. Destiny's Grimoire (lore outside the game) is the shape this folder exists to
prevent.

## Language check (machine-readable)

`Tools/lint-language/run.sh` reads the fenced block below. Do not delete it as clutter —
a term retired without a row here is a term the check does not know about (M28).

```language-lint
# Comma-separated lists. Keys are fixed; the script parses this fence only.
retired=derelict,Podded,podded,breach point,muster point
builder_play_ban=gatekeeper-seconds,arrivals x dwell,realization,hatchDist,spawnDist,Wave-defense,encyclopedia,lore bible,Codex,siphon dial,lobby
stations_title=Armory,Crew Quarters,Fabrication,Nav Map
boarder_enemy=boarders expected,the boarders,boarder kind,boarder-seconds
```

## The traps in this table

**1. `EngineeringStation.cs` is FABRICATION.** The class keeps its historical name; every
string the player reads says FABRICATION. The CREW QUARTERS Engineering attribute track
was deleted by M23 (repair is the Repair Rig as equipment). Do not revive the track name
on the console or invent a new Engineering attribute.

**2. `MapStation.cs` is the NAV MAP.** Same shape as the trap above: the class name is the
forbidden short form. Recorded here so a future session reads it as a known debt rather
than a bug to be silently renamed on its own.

**3. There is no melee in this game, and the third weapon slot is the Drone.** Zo, 2026-08-08.
The slot has been called three things: `Melee` in code, Utility in this table until that
day, and Drone in the fabrication brainstorm. Drone is the answer; Utility and melee are
both retired. The Repair Rig is **equipment**, not a weapon-slot occupant — M23 shipped
that cut. The Decoy is the payload that fills the bay (`PayloadCatalog`, FABRICATION).

**The save key changed with it and the schema went to v5.** Zo's call: this project is not
deep enough to carry save compatibility, and the slot has never held anything, so a v4 save
loses a `WeaponId.None` and nothing else. No compatibility shim, and no reading of the old
key. **Serialized keys are not exempt from rule 4** — treating them as exempt is how this
name survived being wrong through three separate renames.

**`melee` is still a legal word for attack distance.** `EnemyGatekeeper` and `HatchStation`
measure melee range, meaning how close a Gatekeeper must be to strike. That is builder
vocabulary about geometry, not a slot name, and it stays.

**4. Gatekeeper and Bastion collide with the project name.** The namespace, the asmdef and
every editor menu path contain `GatekeeperBastion`, so `grep Gatekeeper` matches ~71 files
that have nothing to do with enemies. This was accepted deliberately on 2026-08-08: the
title naming both sides is worth the noisier search. Grep for `GatekeeperKind`,
`EnemyGatekeeper` or `Gatekeepers` instead of the bare word.

**5. "Boarder" reversed meaning on 2026-08-08.** It used to mean the enemy. It now means the
player. Anything predating that date reads plausibly and means the opposite — this is the
one rename in the file that fails silently rather than loudly.

**6. `Gate` was removed, not redefined (M27, 2026-08-09).** It had three conflicting uses —
`Gate signature` on the NAV MAP, "the gate" for the Run-loss lock in `Play/Upgrades.md`, and
the forbidden Hatch synonym already listed above. None earned a row. NAV MAP now says the
Bastion is garrisoned; the upgrade lock is named as a lock. Do not reintroduce `Gate` as a
fiction noun.

**7. Player-facing name is Thief; the code identifier is still `Ram` until M63.**
Play pages, this table, and the Compendium use Thief. `GatekeeperKind.Ram`,
`RamGatekeeper`, and the generated Field Manual stay Ram until M63's rename
commit. Do not half-fix the code in a Play-page change.

## Adding a term

A term earns a row when it names something a player or an agent will need to refer to more
than once, *and* plain English will not do (rule 6). Add the row in the same commit that
introduces the thing. If you find yourself about to write a definition in a milestone or a
wiki page, the definition belongs here and the page links to it.
