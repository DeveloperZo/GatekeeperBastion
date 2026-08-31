# Boarder Protocols — the class system

<!--
Purpose: The class/archetype system (Zo, 2026-08-15). A Protocol is a sold identity at
CREW QUARTERS: a doctrine that recolors the FABRICATION hardware, a Crew ability with
one twist slot (pick 1 of 2), a Personal ability with one twist slot (pick 1 of 2),
and one of two passives (pick locked by Zo). This file is the structure, the deed
unlock model, the growth levers, the five protocol sheets (the Default plus four
specialists), the candidate axes toward the 8–10 roster, and the doctrine matrix. It
spans the crew-skills and fabrication brainstorm areas, which is why it has its own
file.
Status: Default shipped (`M70`); VEIL shipped (`M94`); FORGE recut (`M106`/`M107`) is in
the 0.34.2 tree while those milestones stay planned. AEGIS and LEDGER remain brainstorm-only.
Shipped sheets of record: `Sim/Meta/BoarderSheets.cs`. The M94 FORGE draft below is
superseded.
Update trigger: when Zo vetoes or accepts a row, doctrine or structure call; when a gate
named on an expansion row ships; when a candidate axis earns or loses a sheet.
Last updated: 2026-08-30 (0.34.2: charged turret + Outrider in tree; Magnet Hands gone; FORGE recut is the live sheet)
-->

## What this is

A **Boarder Protocol** is the class purchase. Equipping one grants a **doctrine** — an
always-on rule set that changes what the FABRICATION hardware does in your hands —
plus its **Crew ability** on `G` with **one twist slot (pick 1 of 2)**, its **Personal
ability** on `H` with **one twist slot (pick 1 of 2)**, and **one of two passives**
for thematic expression — the Kingdom Rush branch shape. A twist changes how the
ability works, never how much. Archetype identity moves from emergent reads of free
picks (`M70`'s position) to named, sold protocols.

**Recut 2026-08-15 (Zo): rival pools became abilities with twists.** The first cut
offered 3 rows per active pool, pick 1. Rejected because the prototypical verb cannot
share a pool with its understudies — a stealth player choosing between Cloak and Feint
is a false choice; Cloak never loses. The choice moves into the twists: everyone
running the protocol has its abilities, and the pick is 1 of 2 twists on how each
works. This brings forward `M70`'s deferred per-ability upgrades on that deferral's
own logic — upgrades change what a row does, not how much — as the core grammar
rather than a later slice. The displaced pool rows fold in as twists (Feint folded
into Phantom Echo; Challenge, first an Anchor twist, since promoted to AEGIS's Crew
signature) or sit in the parked and alternates records.

## Where it stands

Three protocols are claimable. `BoarderSheets.cs` is the CREW copy: Default Barrage/Rush,
VEIL Ping/Cloak/Ghost, FORGE Field Deploy (charge → Overcharge on **G hold**, Exhausted)
and Outrider (charge → EMP on **H hold**, pairing). Magnet Hands is gone (`PassiveForge`
is `** CLASSIFIED **`; `PassiveCount(Forge) = 0`). Doctrine row PARALLEL BAYS still prints
and is "Not yet active." `TurretState` / `OutriderState` / `GatekeeperRoot` / Game controls
and `NetVerbs.RequestOutrider` / `RequestEmp` are in the tree. `M106` and `M107` remain
under `1. planned/` — implementation landed ahead of the folder move; kill criteria and
FORGE's passive wait on play. AEGIS and LEDGER are unbuilt.

**The structure is the growth system.** Two levers, both incremental: **add a twist
slot to an existing ability** — each ability below already carries a recorded parked
pair, and a new slot ships as a pair, one ability at a time, behind the pick-rate
audit (kill criterion 3) — or **add abilities**: a new protocol's sheet (the roster
path), or a new ability on an existing protocol only when a key exists to hold it.
Never widen a pair beyond two; a third option in one slot rebuilds the rival-pool
failure this recut deleted. The quarry for new abilities is the Gatekeeper roster
itself — a shared modifier pool, concentrated in a Boarder, worn diluted by an
authored elite where dilution stays legible (The modifier pool, a bet with kill
questions, below).

Target roster: **8–10 protocols** (Zo, 2026-08-15). Five sheets are below — the
**Default Protocol**, a 0-salvage CREW claim, plus four specialists; the rest
are candidate axes that earn a sheet one at a time (The roster to 8–10). Protocols
unlock on Albion's rule — **doing the thing unlocks it**: each protocol names a deed
counter on play the game already logs (Zo's example: destroying X Default
Gatekeepers earns a protocol) — plus a modest salvage price, and twists inside a
protocol unlock progressively the same way. The structure is below; every timing
number is a #12 value tuned later.

**This overturns M70's "Sold archetypes" Do-not-build row.** Reason the verdict expired
(cut-list rule, `../Decisions.md`): the emergent-reads position assumed identity could
only ever be the picks. The doctrine layer — hardware behaving differently by class —
has no carrier in free-pick grammar, because a doctrine must attach to one exclusive
equipped thing. Zo directed the protocol structure 2026-08-15; the shipped-game evidence
below says the always-on layer is where class identity actually lives.

**Loop or arc:** loop. The protocol roster × three hardware items × twist picks × wave
pressure generate per-Run decisions from fixed authoring. The arc risk is the twist
quota forcing filler pairs — pre-mortem 1 and kill criterion 5 exist for it.

## The grammar

- One protocol equipped per Boarder. Bought flat at CREW QUARTERS; swap is Ship-only
  under the one `PayloadLoadoutLocked` gate (M42 idiom; `M70` does not rename the flag).
- **Doctrine is always on and never bought separately.** A transformed item priced above
  the plain item dies — Payday 2's 24–40-point sentries lost to 13-point doctor bags.
- The sort test (carried from the pre-recut `M70`, owned here now): the Crew ability
  acts on Gatekeepers or teammates, the Personal ability acts on the Boarder pressing
  the key, a Passive is a standing rule and never a number. A twist inherits its
  ability's slot; a twist arguing into the other slot gets recut, not the grammar.
- **The Crew beneficiary rule (Zo, 2026-08-15): the output belongs to the crew; the
  cost belongs to the caster.** The sort test says what a Crew ability touches; this
  says who it is for. Its render is team-shared, its zone is anyone's to fight in,
  its channel feeds anyone's machine, its mark pays whoever collects — and its price
  (the pull, the root, the salvage, the walk-at-you) lands on the presser alone.
  Solo, the crew degrades to you and the Hatch: the output shrinks, the cost never
  does, and the ability must still be worth pressing at that floor. A Crew ability
  whose output only the caster can consume is a Personal ability in the wrong slot.
- **Twists change what the ability does, never how much.** A twist moves the verb,
  target, trigger or aftermath; every window, radius and count stays a #12 value the
  twists inherit. A twist whose whole text is a number is not written.
- **The twist-pair law** (branch research, 2026-08-15): name the situational axis
  before writing the pair, and both ends of it must occur in wave content at a known
  rate; both sides get a verb at symmetric cost, and neither side gets an extra axis
  free (Bloons' Ninja 1xx); the naked ability must be complete — a twist that fixes a
  hole is mandatory and the pair is dead (Warframe's band-aid augments); neither side
  may be redundant with what the ability already does. Pick rates get audited (kill
  criterion 3). A live pair is also solo-strong on both sides — a co-op-weighted or
  rule-bending twist sits in a parked pair, arriving after the identity is set.
- All `M70` bans carry: no damage grants, no crit source, no heal or revive row, no
  numeric passive, no teammate displacement, no permanent mobility, kind refusals stand
  (a Thief never retargets, a Hunter never touches the Hatch). Every ability and every
  twist is a trade.
- **Passives are pick 1 of 2 — locked at the onset (Zo, 2026-08-15).** Both-on stacks
  a pair into a second doctrine (Warden + Doorward is a permanent double-taunt); kill
  criterion 5's quota rule still lets a protocol ship one passive and no pick when a
  second cannot pass the bar.
- Keys unchanged: `F` gun, `G` crew, `H` self, `3` hardware. No new keys, no new panel.
- Unlock: a **deed gate** plus a **modest** salvage price per protocol (Unlock and
  progression, below). The deed does the pacing; the price makes protocol 3 vs 4 a
  decision, not a wall. #12 values. Owned protocols swap free at Ship, and re-picking
  twists inside an owned protocol is always free — charge to acquire, never to re-pick
  (V Rising's rule; Darktide and OMD3 both pair exclusive picks with free swapping).
- Buying a protocol grants its doctrine, both abilities naked, and the first Passive —
  playable that session. The two twist slots and the second Passive are the three
  paced unlocks. The keys never lock: a locked `G` or `H` reads as a broken control,
  and two naked abilities are already the minimum playable state.
- Hardware stays sold at FABRICATION. The doctrine is read at deploy from the placing
  player's equipped protocol and written onto the placed object as its own mode field —
  never checked off a class enum at use time (Destiny 2 rewrote 19 exotics that keyed
  off the class instead of the item).

## Unlock and progression

The spine is Albion's rule (Zo, 2026-08-15): **the more you do the thing, the more it
unlocks.** No account XP, no level — every gate is a deed counter on play the game
already logs, advertised with live progress in the CREW QUARTERS catalog beside the
doctrine text and the salvage price. Nothing is hidden (hidden unlocks are guide-bait,
Remnant 2's Engineer), and every deed is a core verb of a Run — PoE2's trials poison
because one mandatory *detour* gates the fantasy; a deed that is just playing cannot
detour.

| Gate | What it opens | Shape |
|---|---|---|
| Protocol deed | The next protocol becomes buyable | One themed counter per protocol, visible with progress from day one (Albion's destiny board). Zo's example on record: destroy X Default Gatekeepers |
| Protocol play | That protocol's three paced unlocks — the Crew twist slot, the Personal twist slot, the second Passive | Counters on using the protocol itself — the activation log lines exist by design; OMD3 gates a trap's fork behind mastery with that trap, and a twist is only a choice once the naked ability is understood |

- **Identity ships at hour zero.** Recut 2026-08-16: the **Default Protocol is a
  0-salvage CREW claim** (Mk1 shape), not auto-owned. CREW shows VEIL / AEGIS /
  LEDGER as Coming Soon copy until those slices add catalog ids. Default has no
  Passive pick and no paced unlocks; pacing starts at VEIL. Killing Floor 2's
  level 0 — passives too weak to feel, no skill until level 5 — is still the
  dead-early-game case this claim exists to avoid.
- **Deed candidates, themed so earning a protocol rehearses it** (#12 values, each
  named for real at its protocol's slice): VEIL — destroy X Default Gatekeepers
  (Zo's example, the first deed to ship); AEGIS — complete X Hatch repairs;
  LEDGER — bank X salvage lifetime; FORGE — deploy X payloads. Each counts a
  host-side event that already logs.
- **Salvage stays the only wallet, and the deed model deletes the XP field
  entirely** — cleaner against the 2026-08-12 XP cut than the gate-that-never-spends
  compromise it replaces. Deed counters are new persistent state; the milestone that
  ships them names the exception to `M70` criterion 12's no-new-counter line.
- V Rising's guardrails carry: the deed is advertised in advance with visible
  progress, and slots pace by entries, never by locking keys — a locked `G` reads
  as a broken control.

## The doctrine matrix

The flagship layer. Every clause is a verb or target change, never a number. The
defensive→offensive direction is closed by the no-damage-grant law; the open directions
are offensive→defensive, offensive→information, offensive→economy, and utility adds.
**The UTL-H fires as a gun under STOCK and FORGE** — the three specialist doctrines
flip or recolor it. **Stamped (Zo, 2026-08-15)**: the flip stands over the rejected
clause-add alternative (safer, flatter, no identity). The requirement that survives
from the grilling: the FABRICATION shelf row shows the doctrine mode at purchase — a
bought turret that never fires is otherwise a bug report, and that is the milestone
contract's Shown test, not polish. Individual clause texts remain vetoable at their
columns' slices.

| | VEIL | AEGIS | LEDGER | FORGE |
|---|---|---|---|---|
| **Decoy** | **Phantom** — spawns holding your current aggro instead of a neutral pull | **Bastion totem** — no proximity pull; any Gatekeeper that damages it locks to it until destroyed, then the pack re-acquires at once | **Gleaner** — pulls as normal; world piles in its radius migrate to its foot | **Parallel bays** — two may stand live at once |
| **UTL-H turret** | **Watchtower** — never fires; renders Gatekeepers in its arc through walls for the whole team; siphon runs at a reduced clock | **Suppressor** — never damages; Gatekeepers in its arc are slowed; siphon unchanged | **Assayer** — fires; its kills pay as a pile at the turret's foot instead of instant credit | **Gunsmith** — fires, per the turret baseline (its spec ships at the FORGE slice); hold interact to hand-feed its onboard magazine from your reserve |
| **Repair Rig** | **Ghost hands** — the acquisition bias extends to the repair channel: channel-repairing does not raise you in target selection | **Steadfast** — the repair channel cannot be interrupted by damage you take *(code check 2026-08-15: damage does not interrupt repair today — `RepairInteractable` ends the hold on key release only. Grilled: the baseline rule ships first as its own micro-milestone with solo bank-wave completability in its verification — a tier-#5 rules change riding the class slice makes this clause's kill criterion unreadable. If Zo declines the rule, this column ships baseline with no clause)* | **Actuary** — repair may spend unbanked when banked is empty *(boundary-adjacent: a new spend path, not a conversion — flagged below)* | **Field service** — the rig also repairs deployed hardware, not only the Hatch |

Precedents per mechanism: verb swap (DRG overclocks — one line of text, "+Shotgun Jump",
is the most-picked overclock in the Karl.GG corpus at 20% share; Payday 2's AP sentry
mode), target/persistence flip (Payday 2 Joker, the most remembered and most expensive
kind), added clause (Remnant 2 perks; Back 4 Blood Medical Professional, where one
scarce clause created the healer role by itself).

## The protocol sheets

Working names on the codename rule (`../Decisions.md`) — renameable when play earns a
fiction noun. Each sheet: a Crew ability and a Personal ability, each with **one twist
slot (pick 1 of 2)**, and two Passives (pick 1). Default (M70 recut 2026-08-16) has
no Passive pick and no twist slots.

**Three of the five sheets have since shipped and left this file's authority.** Default,
VEIL and FORGE are in `Sim/Meta/BoarderSheets.cs` and the milestones are the record: `M70`
for Default, `M94` for the other two. Read the shipped copy there, not the sheets below.
Only AEGIS and LEDGER are still spec of record here, and the shipped three are kept in place
because the twist tables under them are the parked pairs the growth lever draws from. Where a
shipped sheet and this file disagree, this file is the older draft.

Under each ability, the **parked pair** is the
recorded candidate for its future second twist slot (growth lever), and the alternates
record keeps the cuts and the wall each one hit. Live pairs were selected 2026-08-15
under the twist-pair law: solo-strong on both sides, unflagged, clearest axis; every
co-op-weighted, mobility-flagged or rule-bending twist parks.

### DEFAULT — the unmodified Boarder

The assault identity: Advance Wars' Andy / Mass Effect's Soldier. A 0-salvage CREW
claim (Mk1 shape). Every later protocol reads as a deviation from it. `M70` recut
2026-08-16.

**Doctrine: STOCK** — hardware as sold. Written at decoy `TryPlace`. STOCK writes 0.
VEIL is the first reader.

**BARRAGE (`G`)** — `Rooted`; caster fire rate ×2.0; other Boarders within 8 m also
×2.0, not rooted. Stand and dump. Cooldown starts at unplant.

**RUSH (`H`)** — caster fire rate ×1.5; you move. No plant.

No Passive pick. Twist slots later.

Anchor, Bulkhead, Ghost and Foresight are **not** on this sheet. They return to
AEGIS (Anchor / Bulkhead — already on that sheet below), VEIL (Ghost, beside
Sightline), FORGE (Foresight, beside Magnet Hands). The 2026-08-15 move onto
Default is overturned.

### VEIL — who notices you, and what you know

**Shipped by `M94` largely as authored**: PHANTOM doctrine, Ping on `G`, Cloak on `H`, and
Ghost as the Passive rather than the Sightline this sheet names. No twist slot shipped with
it, so both twist tables below are still parked pairs.

The Assassin translated. The precision half (crits, weakspots, executes) is banned
damage vocabulary, so it stays in the weapon catalog; the protocol carries the
who-notices-you half. Two VEILs in one lobby read at a glance: the Phantom
Echo/Thrown Voice player is a moving stalker never standing where the room converges;
the Long Watch/Hairtrigger player holds ground and lets the room come.

**PING (`G`)** [M70, recut 2026-08-15 under the beneficiary rule] — every Gatekeeper
renders through walls **for every Boarder**; every Default and Hunter targets you for
the window. The crew gets the map; you get the room's attention. Solo, the render is
yours alone and the price is unchanged — the floor the rule requires.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Phantom Echo** | The cast plants a stationary ghost of you holding the pulled targeters; on expiry the pack re-acquires nearest-alive at once (Feint, folded; Splinter Cell's last-known-position) | Enemies converge on a point you must abandon, and the expiry spike is yours |
| **Long Watch** | Press becomes hold: render and pull persist while you hold `G` and stand still; moving or firing ends it (Hunt's Dark Sight) | A stationary, gun-down lighthouse |

Axis: leave the pull, or hold it. Parked pair (second slot): **Deep Read** (rendered
Gatekeepers show a tether to their current target; firing ends the window —
Dishonored's Dark Vision) / **Peel** (the pull widens to every Default and Hunter
already committed to a teammate — you take over their fights; co-op-weighted) —
"deepen the read, or widen the pull." **Relay is absorbed:** the team-shared render
it carried is now Ping's base, per the beneficiary rule. Alternates on record:
**Tripwire Ping** (deferred cast reads as a bug); **Grudge** (needs persistent
per-enemy render state; thin against waves).

**CLOAK (`H`)** [M70] — the layer, spec of record here (settled in
`crew-skills-and-perks.md`, carried through the pre-recut `M70`): ~6 s off every
targeting list, the world as identity-blind orbs — positionally honest, actors only,
geometry renders normally — firing exits instantly, no teammate is ever shifted.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Hairtrigger** | Cloak pre-arms instead of casting; fires itself the instant a Gatekeeper acquires you; manual press gone while armed (RoR2's Old War Stealthkit) | You never choose the moment — a stray glance burns the window |
| **Thrown Voice** | Entering the layer drops a noisemaker at your entry spot pulling the nearest non-committed, non-Thief Gatekeeper to it (Lure, folded; Thief's noisemaker) | The lure lands where you just were — cloaking in place parks an enemy on you; only moving pays |

Axis: automatic escape, or chosen slip. Parked pair (second slot): **Shroud**
(teammates in radius enter with you; any cloaked Boarder firing collapses it for all;
carriers stay visible — The Finals' carrier rule; co-op-weighted) / **Attuned Orbs**
(orbs kind-code; sprinting exits like firing — Crysis; **bends the identity-blind
orb rule in the layer spec above, Zo veto due when this slot ships**). Alternates on record: **Slip**
(mobility-cut flag); **Parting Veil** (unreadable mid-fight, solo it re-selects you).

Passives, pick 1 of 2: **Sightline** — while stationary, Gatekeeper acquisition radii
render in-world (Mark of the Ninja) — and a second to re-author at VEIL's slice
(Ghost moved to the Default sheet, 2026-08-15).

### AEGIS — aggro rules and geometry

**Deferred (Zo, 2026-08-15) — AEGIS must not block the system.** Its Steadfast clause
waits on the damage-interrupts-repair pre-req (R). Anchor and Bulkhead sit on this
sheet (returned from Default, 2026-08-16 recut). Challenge remains a twist until
AEGIS's slice re-authors. Warden/Doorward and all three of its matrix clauses stand
as authored (Suppressor ships earlier, with the turret column at the FORGE slice).

The Paladin translated. Shipped tanks speak in damage-reduction percentages; what
survives the numeric-passive ban untouched is aggro rules and geometry. Two AEGIS
players diverge as: the Challenge/Turnstile player never leaves the doorway; the
Ward Stone/Deadbolt player is never in it.

**ANCHOR (`G`)** [M70] — plant; Gatekeepers within 8 m slow; you cannot move until
unplant. The room slows because you stopped. Beneficiary check: the zone is anyone's
to fight in and the Hatch's to hide behind; the root is yours.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Challenge** | While planted, Defaults and Hunters in the radius target you; unplanting breaks it; Thieves refuse (folded; Vermintide's man-sized taunt, DR number stripped) | Everything in reach converges on a man who cannot move |
| **Ward Stone** | The plant becomes a destructible stake at a ghost-and-confirm spot; the slow lives on the stake until Gatekeepers smash it; one at a time (Bloons' placed slow, destructibility added) | Uptime is enemy-decided, and you gave up the indestructible version — yourself |

Axis: you root, or a stake does. Parked pair (second slot): **Sieve** (the zone slows
only Gatekeepers moving on the Hatch; your own hunters pass at full speed — pairs with
Doorward) / **Sally** (unplanting becomes a stamina-priced lunge toward the Hatch or a
teammate — DRG's Dash; **mobility-cut flag, Zo veto**) — "what the zone serves, and
how you leave it." Alternates on record: **Grudging Ground** (stacks incoherently with
Challenge); **Roadblock** (needs player-as-nav-obstacle, unshipped).

**BULKHEAD (`H`)** [new] — window: the first Gatekeeper swing that lands on you is
negated; the window ends on that negation. One hit erased, bought in advance with
timing.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Turnstile** | Negates the first swing from *each* Gatekeeper rather than the first overall; ends the instant you fire (For Honor's full-block stance) | Gun holstered while it runs — pop it and hold the repair channel while each first swing whiffs |
| **Deadbolt** | Pre-arms instead of casting; fires automatically on the first swing that would land; you move at walk speed while armed (RoR2's Safer Spaces) | No chosen moment — it burns on a chip hit — and arming costs your sprint |

Axis: cast the window, or wear it. Parked pair (second slot): **Sentinel** (while
still, the negation guards the nearest teammate — Overwatch's Projected Barrier;
co-op-weighted) / **Doorplate** (the negation guards the Hatch while you stand in
reach — the one legal answer to a Thief's output; needs a Hatch-damage intercept hook)
— "who else the window guards." Alternates on record: **Rebuff** (solo it re-selects
you); **Rebuke** (no stagger state; gate `M63`); **Intervene** (displaced, mobility
flag).

Passives, pick 1 of 2: **Warden** [M70] — provoke-to-self on damage (the one writer of
the permanent flag). **Doorward** — Gatekeepers attacking the Hatch prefer you while
you stand within N m of it. Warden is co-op-inert solo (recorded here; the recut
`M70` no longer carries Warden): a solo AEGIS pick is effectively Doorward, and the
panel says so.

### LEDGER — where salvage goes, and what death costs

The Necromancer/Medic translated: with healing and summons banned, the fantasy lands on
the salvage economy — five independent shipped proofs (Nekros Desecrate, Hunt's overflow
economy, Payday's corpse objects, Tarkov insurance, CoD Zombies kill-money) that
death-as-economy carries a class without a heal. Hard wall: **unbanked converts only via
Hatch-bank** (`../Design/3-Mechanics.md`) — every line shapes risk and position; none
bypasses conversion. Two Ledgers diverge as: the economist turning kills into
ground-truth piles versus the anchor running a conveyor of payouts into a killzone.

**WRIT (`G`)** [new] — mark one non-Thief Gatekeeper for a window: it renders through
walls for the team and walks toward you — a windowed overlay, never the provoke flag
(the separation rule, carried from the pre-recut `M70`: windowed overlays never
share a code path with Warden's permanent flag). The payout walks at you, armed. Beneficiary
check: the render is the crew's and the kill pays whoever takes it; the armed
delivery is yours.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Dead Letter** | The mark's kill payout drops as a world pile at the corpse instead of auto-crediting the killer (Hunt's bounty token at the corpse) | The payout becomes position — walkable, losable, stealable post-`M63` |
| **Probate** | A mark dying inside the window passes the Writ to the nearest non-Thief for the remainder; the window never refreshes | You never choose the second target |

Axis: the mark's death pays, or passes. Parked pair (second slot): **Summons** (the
mark walks to your placed machine instead of you — DRG's L.U.R.E.) / **Class Action**
(the walk is deleted; every Gatekeeper of the marked kind renders — co-op-weighted)
— "what the walk becomes." Alternates on record: **Contempt** (writes the permanent
flag, which is Warden's alone); **Lien** (an unbounded overlay is a second provoke
flag in costume).

**RECALL (`H`)** [M70] — teleport to the Hatch; your whole unbanked drops where you
stood as a pile anyone can reclaim; refuses at 0 unbanked. You arrive broke and the
pile is in the field.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Escrow** | The teleport is deleted: hold to pour unbanked into a pile at your feet, release to stop (folded) | You keep position and gun but lose the escape |
| **Beacon** | For a window after the drop, world piles in radius migrate into yours and merge (folded; richer post-`M63` Thief) | One pile, one theft, one trip |

Axis: keep position, or take the teleport. **Audit note:** this pair was forced — the
other two twists carry flags — and Beacon reads near-additive beside Escrow's deleted
teleport; if any live pair fails kill criterion 3 first, it is this one. Parked pair
(second slot): **Ration** (the drop may include reserve ammo; a broke Recall fires
priced in ammo — **bends the 0-unbanked refusal, Zo veto due when this slot ships**) /
**Round Trip** (press again to jump back to your pile, only at 0 unbanked —
**mobility-cut flag, Zo veto**). Alternates on record: **Tithe** (cut on sight:
salvage never auto-banks); **Dead Drop** (blinds your team more than the Thief;
revisit post-`M63`).

Passives, pick 1 of 2: **Salvage Sense** — world piles render through walls.
**Testament** — going down drops your unbanked as a pile instead of wiping
[**Zo decision required** — down currently wipes unbanked (`../Decisions.md`,
2026-08-12); this is a dated break bought with the passive slot. **Source note
(2026-08-15): solo, down ends the Run (`RunSim` → `EndRun` → `WipeUnbanked`), so the
pile has no one to reclaim it — Testament only means anything in co-op, where `M57`'s
down-wait keeps the Run alive. Effectively co-op-only; the panel must say so.**]

### Doctrines — retired 2026-08-29, reintroduce after equipment is solid

A **doctrine** was the fourth row on a protocol sheet: what changes about a machine the
Boarder places, because of which protocol they are wearing. Three existed — Default's
STOCK (nothing changes), VEIL's PHANTOM (a placed decoy comes up holding the attention its
placer was carrying), FORGE's PARALLEL BAYS (two decoys live at once). `M105` deletes the
concept.

**Why it failed the first time, stated so the reintroduction does not repeat it.** It was
a slot before it was a behaviour. Of the three members, one was defined as doing nothing
and the other two did nothing in fact: PHANTOM's handover ended by calling `Retarget`,
which gave back every body outside the pull radius, and PARALLEL BAYS had no caller at
all. Every sheet carried a row a player could not exercise from either ability key, and
the CREW panel printed copy for rules that were not running. The lesson is not that
per-owner hardware rules are wrong — it is that the row existed to be filled rather than
because two protocols needed to change the same machine differently.

**Reintroduce after equipment is implemented and solidified** (Zo, 2026-08-29). The
reasoning is that a doctrine only has something to modify once there is a body of placed,
owned hardware worth modifying. Today that body is one payload (the Decoy) and one piece
of equipment (the Repair Rig) — `fabrication.md`, *Equipment*, where the roster is a
single row. A rule about how *your* machines differ from someone else's is not worth a
sheet row until there is more than one machine and more than one piece.

**Graduation test for this entry**, tighter than the file's: equipment has stopped being
one row and its rules are settled, AND two protocols independently want the same machine
to behave differently. Both halves, because either alone reproduces the empty slot. When
it returns, it returns as a named rule with behaviour on the day it ships, never as a
column every protocol must fill.

*Unknown, needs play data:* whether per-owner hardware differences read at all in play, or
whether a Boarder simply sees "my turret" and never notices it behaves differently from a
crewmate's.

## FORGE — the recut (Zo, 2026-08-29)

**This supersedes the FORGE draft below it.** Enshrined from Zo's recut the same day, with
four rulings taken in the same pass. Cut into `M106` (the turret half) and `M107` (the
Outrider half). **Both are in the 0.34.2 tree** (`TurretState`, `OutriderState`,
`PlayerTurretControl`, `PlayerOutriderControl`, `GatekeeperRoot`); the milestones stay
planned until the kill criterion is played. This section is the design those two were
written from. The `### FORGE — serviceable hardware` sheet below is parked verbs, not
the build.

**Identity.** FORGE holds a position. The turret is the expression of that; the Outrider
is what lets the Boarder leave it.

**Input grammar — FORGE only, for now** (Zo's ruling). Two keys, four inputs. A press
deploys, a press-and-hold fires that machine's special. The turret sits on the crew
binding, the Outrider on the personal binding. Pressing with a machine already out
redeploys it: both are freely redeployable, and placing a second retires the first.
Default keeps its second-press stand-down and VEIL keeps its plain presses; two protocols
answering the same key differently is a legibility cost taken knowingly, so the hold can
be judged on FORGE before it spreads.

**States.** Both machines run one chain: **Available** (off-field, cooldown elapsed,
deployable) → **Deployed** (on field, active) → **Special** (its alternate mechanic
running) → **Exhausted** (on field, no longer active, despawning) → **Despawned** (gone,
by damage or by exhaustion).

**Turret.** Deployed: acquires and fires on its own out to its range; Boarders inside its
aura fire faster; firing builds its charge. Special — **Overcharge**: increased firing
rate, and if the Outrider is deployed and near, the Outrider fires at maximum rate too.
Ends into Exhausted.

**Outrider.** Deployed: follows the Boarder and fires on the Gatekeeper nearest to them;
firing builds its charge. Special — **EMP**: a blast from the Outrider, and if it is near
the turret, the turret's fire becomes EMP rounds for the duration. Ends into Exhausted.
**EMP's effect on a body is light damage and a medium-length root** (Zo, 2026-08-29).

**The loop.** Both machines charge only by firing, so neither can be loaded in a quiet
room — the special is always earned inside the fight it is spent in.

- *Turret first.* Place it, let it fire and charge while it defends. Deploy the Outrider.
  Trigger Overcharge: both go to max rate, which also charges the Outrider faster. Trigger
  EMP: the Outrider blasts and the overcharged turret fires EMP rounds at max rate.
  Overcharge ends, turret Exhausted; EMP ends, Outrider Exhausted.
- *Outrider first.* Deploy it, fight, let it charge. Place the turret. Trigger EMP: the
  turret fires EMP rounds immediately. Charge the turret, trigger Overcharge: the Outrider
  picks up max rate. Same exit.

Both orders reach the same peak — a max-rate EMP turret with a max-rate Outrider beside
it — and both collapse into a double Exhausted.

**Why the two orders matter.** A charged turret is evidence it was placed and defending. A
charged Outrider with no turret out is evidence the Boarder went and found the fight. The
orders are a record of how the Wave was played, and they map onto the Assault's real fork:
hold where you are and grind the Wave down, or push across the zone and shut the terminal
off early. Turret-first is the hold line. Outrider-first is the push line, because the
Outrider charges by travelling. Overcharge also accelerates charging, since charge comes
from firing — so turret-first escalates toward the peak on its own, while Outrider-first
buys lockdown up front at normal rate. Greedy versus safe, same destination.

**Rulings taken 2026-08-29 alongside the recut:**

- **The follower is not called a Drone.** *Drone* is the third weapon slot and its
  payloads (`../_meta/_definitions/language.md`, rule 3, Zo 2026-08-08). FORGE's follower
  is named separately so a Boarder reading the sheet never confuses it with the bay.
  **Outrider is a working name and needs Zo's word before it enters `language.md`.**
- **Machines are naturally vulnerable.** Rather than a passive that steers Gatekeepers
  onto hardware, a deployed machine is a target in its own right — either mortal and
  targetable by the ordinary rule, or registered so targeting treats it as it treats a
  Boarder. **Magnet Hands retires**: it exists to make Gatekeepers prefer the turret, and
  natural vulnerability makes that redundant.
- **FORGE's passive is open.** Zo, 2026-08-29: invent one or keep it TODO. Held open
  deliberately rather than filled to make the sheet look complete; the trigger is the
  `M107` close, when the two-machine loop has been played and it is clear what the sheet
  is missing. Owner: Zo.
- **Doctrine is gone.** PARALLEL BAYS retires with the doctrine concept (`M105`), so
  nothing in this recut depends on a per-owner rule for placed hardware.

**What this costs, said plainly.** This is a bet, not a defect fix. It adds a second
machine, a charge resource, a status effect the garrison has never had, a five-state
chain and an input grammar that no other protocol uses. The pre-mortem and the kill
criterion are on `M107`, because the claim being tested — that the two orders read as a
record of how the Wave was played — cannot be observed until both machines exist.

### FORGE — serviceable hardware

**Superseded 2026-08-29 / in-tree 2026-08-30.** The recut above plus `M106`/`M107` code
are the build. This sheet is parked verbs (Field Rearm / Redeploy twists) so they are
not rediscovered. Do not treat Field Deploy-on-`H` / Magnet Hands as live.

The Engineer. Its doctrine keeps the turret a gun; its abilities make hardware
something you tend rather than fire-and-forget. Two Forges diverge as: the
quartermaster feeding live machines versus the architect rebuilding dead ones; the
conductor playing machines from cover versus the nomad who carries his armed.

**FIELD REARM (`G`)** [new, wants UTL-H to fully matter; decoys carry it today] —
channel unbanked salvage into a deployed turret or decoy, yours or a teammate's, to
restore and extend it (TF2's wrench). Your payout becomes machine life, and the
channel roots you. Beneficiary check: the machine life is anyone's — a teammate's
killzone runs on your salvage; the spend and the root are yours.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Cross-Feed** | The channel may move rounds from your reserve into a turret's siphon pool instead of salvage into life (Payday 2's sentries eating carried ammo) | Your gun starves to keep the machine fed. **Gate: the ammo reserve does not exist (mechanics bill 4) — same decision UTL-H itself waits on** |
| **Salvage Weld** | The channel may rebuild an expired machine where it died — but a welded machine can never be picked up or Redeployed again | Permanence bought with the hardware's mobility |

Axis: feed the live, or rebuild the dead. Parked pair (second slot): **Primed
Delivery** (a full channel makes the machine act once — turret burst, decoy pulse;
BTD6's activated abilities) / **Long Wrench** (channel at range on any sighted
machine; you cannot move or fire while it runs) — "what the channel pays, and from
where." Alternates on record: **Work Order** (a second banking-wall break; Actuary
holds that flag and the pre-mortem says a third proposal shaped like a bypass means
stop); **Sensor Bolt** (a third verb, not a twist; parked as a seed for a future
ability or hardware payload).

**REDEPLOY (`H`)** [new, wants UTL-H; works on decoys today] — pick up your placed
hardware, carry it hands-full and slow, re-place it within its life (Remnant Engineer,
Aliens Fireteam). Disarmed and slow while the machine is in your arms.

| Twist (pick 1 of 2) | Rule change | Trade |
|---|---|---|
| **Remote Trigger** | Tap on a sighted machine commands one act — turret burst, decoy pulse; hold still picks up (folded; Payday 2's sentry toggle) | Every command spends the machine |
| **Shoulder Rig** | Carrying no longer disarms you — sidearm stays usable — but sprint locks while you carry | Firepower for pace |

Axis: command it, or carry it armed. Parked pair (second slot): **Bootstrap**
(dismantle in place; unfired rounds and unspent charges come back — Payday 2's sentry
refund) / **Handoff** (machines pass between teammates' hands; ownership rebinds on
re-place — co-op-weighted) — "how the machine changes hands." Alternates on record:
**Pack Mule** (quantity-only, numeric in spirit); **Autopilot** (an autonomous
decision-maker, the exact banned thing).

Passives, pick 1 of 2: **Magnet Hands** — when target selection is ambiguous,
Gatekeepers prefer your deployables over you — and a second to re-author at FORGE's
slice (Foresight moved to the Default sheet, 2026-08-15).

### Parked verbs

Former pool rows with no home in the branch grammar, kept with their gates:
**Aegis Dome** (placed projectile-blocking dome — gate: `M63` Standoff Shooter);
**Glitch Snare** (trap disabling special behaviors — gate: `M63` leaper/caller kinds);
**Haul** (butcher a corpse into a carried bundle — gate: corpse objects, unscoped);
**Sensor Bolt** (above). Each is a seed for a future protocol's ability or a
hardware payload, not a twist.

## The roster to 8–10

Endless Dungeon's roster math is the guard: nine heroes, three comp slots, and the
rows that shared a job went situational — live rows equal distinct jobs. So the roster
grows one sheet at a time, and **a candidate earns a sheet only by claiming a job and a
doctrine column no authored protocol owns.** Sheets are authored after slice-1 play
data, not before. Candidates that cannot name an unclaimed job do not join this table.

| Candidate | The job it would claim | Flag |
|---|---|---|
| **COURIER** | Movement and carrying — routes, the pile in transit | The mobility cut looms over the whole axis; every active must be a priced burst, and the sheet needs Zo's veto on that ground alone |
| **RAMPART** | The Hatch itself — the door fight as a specialty | Authored only if AEGIS play data shows player-tank and door-tank are different jobs; otherwise it is AEGIS |
| **HIJACK** | The faction flip — one Gatekeeper fights for you | The Joker precedent: the most remembered transform shipped and the most expensive to support. One per roster, ever; gated on `M63` AI states |
| **GAMBIT** | The wager — intensity, the trigger, the bet | Grinds against a pillar (the bet at the trigger stays blind); pre-mortem before any sheet, may be unbuildable without a break |
| **TETHER** | Fellowship — escort, shared windows | A whole co-op-weighted protocol is dead solo; wants `M65` three-player data first |

COURIER and HIJACK carry draft abilities from the 2026-08-15 generation pass, held
here until their flags clear. COURIER: **Dispatch** (`G`) — hurl your entire unbanked
as a thrown parcel landing as a world pile at the aim point; refuses at 0 unbanked; a
parcel thrown at the Hatch lands at its foot and still banks only by hand during a
Hatch-bank wave (The Finals' cashbox throw). Twist pair drafted: Ammo Run (the parcel
may be reserve ammo), Bucket Brigade (a mid-air catch goes straight to the catcher's
unbanked; co-op-weighted). **Dead Run** (`H`) — full-sprint burst that sheds your
unbanked behind you as micro-piles; sits squarely on the mobility cut. HIJACK:
**Override** (`G`) — channel one non-Thief, non-Hunter Gatekeeper to fight beside you,
one at a time; while it stands, your kill payouts credit into the convert and fall as
a pile wherever it dies — income held hostage in a body the enemy is deleting (Payday
2's Joker, with an escrow price). **Switchboard** (`H`) — swap positions with your
convert; dead key without one; mobility-cut flag on top of the `M63` gate.

## Content bill and sequencing

`M70`'s seven rows redistribute (Anchor, Ghost, Foresight → DEFAULT; Ping, Cloak →
VEIL; Warden → AEGIS; Recall → LEDGER), and Bulkhead moves to DEFAULT with its pair.
Per protocol the shipping bill is **2 abilities, 1 twist pair each (4 twists), 2
passives, 3 doctrine clauses** (DEFAULT: zero clauses — STOCK is the baseline), plus
a recorded parked pair per ability that costs nothing until its slot ships. A twist
is cheaper than a full row — it edits an existing verb's target, trigger or
aftermath — but the earlier claim that VEIL and AEGIS run on shipped seams today was
wrong: the 2026-08-15 source audit found the cloak layer, the windowed target
override and the through-wall render all absent. What each sheet actually costs is
The mechanics bill, below.

Recommended slices, in order (recut 2026-08-15, Zo: AEGIS defers — it must not block
the system — and `M70` recuts as the Default Protocol; this supersedes the same-day
AEGIS-first cut, whose R micro-milestone would have gated the whole frame on a rules
decision):

1. **`M70` recut 2026-08-16 — the Default Protocol:** CREW claim at 0 salvage,
   Coming Soon stubs, Barrage `G` / Rush `H` (windowed fire-rate mul), Fight
   shortcut cooldown readout. Frame already in tree (catalog, v8 refund, `G`/`H`
   binds, STOCK at place, net ids). No Passive pick. No specialist ids.
2. **VEIL + the unlock spine:** bill 1, 2, 5, 12. The second protocol brings the
   system's remaining sockets: the first deed gate (destroy X Default Gatekeepers,
   Zo's example) with its persistent counter and catalog row, the twist-slot
   machinery with VEIL's pairs, the full VEIL sheet and its Phantom and Ghost-hands
   clauses. The spine ships with its first customer; this unit may split again at
   authoring (sheet first, twists after) if it reads over-grain.
3. **FORGE + the turret:** UTL-H, the whole turret doctrine column
   (Watchtower/Suppressor/Assayer/Gunsmith), and the ammo-reserve decision (bill 4)
   at the turret's own milestone, plus FORGE's sheet and deed.
4. **LEDGER:** after `M63`'s Thief slice; its pillar is the world pile.
5. **AEGIS, last by direction (Zo, 2026-08-15):** the R pre-req
   (damage-interrupts-repair micro-milestone) plus the re-authoring its donated rows
   force — Challenge to Crew signature, a new Personal on the Hatch-geometry lane.
   Warden/Doorward, Bastion totem and Steadfast then land as authored.

A future payload ships with baseline behavior under every doctrine until it has been
played — `fabrication.md`'s no-upgrades-until-played rule, applied to clauses, caps the
matrix growing multiplicatively with the payload roster.

At the 8–10 target the full-roster arithmetic is **16–20 abilities, 32–40 live
twists, 16–20 passives and 21–27 doctrine clauses** (the Default carries none),
before any second twist slot ships. That bill is funded one protocol per slice, each behind the previous one's
play data; kill criterion 6 exists so a protocol that cannot claim a job is never
authored at all. For scale: one added hero took Amplitude, a full studio, a
five-month community process to ship.

## The mechanics bill

Source-audited 2026-08-15 against `Assets/Scripts` (evidence: class and method names
per row). This is the deliberate-growth map: protocols are the expression lever, the
weapon wheel is their rival for identity, and each mechanic below is priced by how
many expression rows it unlocks. Build order follows demand, not sheet order.

| # | Mechanic | Source status | Unlocks | Size |
|---|---|---|---|---|
| 1 | **Windowed target override** — a timed target/goal override on `EnemyGatekeeper`, beside the permanent `_provoked` and the decoy's `_chasingDecoy` | Absent; `_chasingDecoy` is the precedent to generalize | Ping base + Phantom Echo + Long Watch + Thrown Voice; Challenge; Writ base + Probate + Summons; Phantom and Bastion-totem doctrine clauses — **10 rows across three protocols** | Medium — Sim-side, extends `Retarget()`/`TryGetGoal()` |
| 2 | **Through-wall render layer** — outline/marker rendering for flagged enemies, piles, radii; team-shared from day one (Ping's base is crew-facing per the beneficiary rule) | Absent | Ping base; Writ base; Salvage Sense; Sightline; Deep Read + Class Action (parked); Watchtower clause; Sensor Bolt (seed) — **8 rows, the whole information vocabulary** | Medium — Game-side render feature, no Sim change |
| 3 | **World pile** — droppable salvage object, reclaim by touch | Absent (`HatchBank.cs` notes the old pile-size convert was removed); `RunSim.Unbanked`/`AwardKill`/`WipeUnbanked` are the ledger hooks | Recall base + Escrow + Beacon; Dead Letter; Testament; Gleaner + Assayer clauses; Dispatch (candidate) — **8 rows, LEDGER's entire identity** | Medium — new Game object + Sim ledger ops |
| 4 | **Ammo reserve** — a backpack reserve behind the clip | Absent — `PlayerGun` is clip-only, reload refills free; no transfer API | UTL-H's own siphon premise; Cross-Feed; Gunsmith clause; Ration + Ammo Run + Bootstrap (parked/candidate) | **A weapons-lane decision, not a protocol task** — it changes reload economy for everyone. Decide it as its own call; if declined, FORGE's feeding verbs recut to salvage-only and UTL-H needs a new fuel rule |
| 5 | **Cloak layer** — targeting exclusion + identity-blind orb render + fire-to-exit | Absent; targeting half is a filter in `PlayerRegistry.NearestAlive`/`Retarget()`, render half is real work | Cloak base + all 4 twists; Ghost hands clause reuses the exclusion filter | Medium-large — VEIL's identity; the one mechanic a single protocol fully owns |
| 6 | **Player-swing negation hook** (Bulkhead base) | Absent; player damage path is the hook | Bulkhead + Turnstile/Deadbolt (live pair) + Sentinel (parked) | Small |
| 7 | **Hatch-damage intercept** (Doorplate) | Verified small — single funnel `HatchStation.ApplyDamage` | Doorplate (parked) | Small |
| 8 | **Teleport / priced burst movement** | Absent (no gameplay dash or teleport exists) | Recall base; Sally + Round Trip (parked) + Slip (alternate), all mobility-flagged | Small-medium — build once for Recall, the only unflagged demander |
| 9 | **Carry state** (hands-full, slow) | Absent (`MovementLocked` is down/menu only) | Redeploy base + Shoulder Rig; Haul (seed) | Small-medium |
| 10 | **Wave-preview kind surface** (Foresight) | Data exists (`WaveDirector.NextWave` has kind counts); UI shows body count only | Foresight | Small — surface `hunterCount`/`thiefCount` |
| 11 | **Destructible placed aura** (Ward Stone) | Close — decoy has hp, placement flow (`DecoyPlacement` ghost-and-confirm) exists to reuse | Ward Stone | Small-medium |
| 12 | **Deed counters** — one themed persistent counter per protocol gate, plus per-protocol play counters for the paced unlocks | Absent; `MetaState`/`SaveData` (`gatekeeper_meta.json`) is the home | The whole unlock spine (Albion model, Unlock and progression) | Small state per deed; every candidate deed counts a host-side event that already logs (kill, repair, bank, deploy) — no new earn rule to invent |
| 13 | **Elite scaffolding** — a kind wearing one authored modifier: fixed per-wave counts in the existing composition-array pattern (`HunterPerWave`), an HP-and-telegraph tier authored with the body, never rolled, never touching a kind's refusal (why rolled died: The modifier pool) | Absent — source has kinds only, no variant tiers; `enemyHealthMultiplier` is one number for every kind (`WaveSpec`) | The modifier pool's Gatekeeper side; the whole elite system | Medium — gated on the one-Gatekeeper probe (kill question 1) reading, and the probe itself ships as a one-off exempt from this gate |

Source facts that shape rows, found in the same audit: **kill award is 0 during
Hatch-bank waves** (`CurrentKillAward`), so Dead Letter and Assayer produce no piles
during bank waves — inherited, not a bug. **`G` and `H` are unbound** in
`Controls.cs`, and the `M70` F-key ability pipeline (`PlayerAbility`, `AbilityId`,
cooldown HUD, `NetVerbs`) is live scaffolding the protocol slots can mirror — the
weapons already own `F`, which is the expression split working in the bindings
themselves. **Ghost** is a parameter change on existing acquisition range
(`CanSeePlayer()`), the cheapest passive on any sheet.

The lane rule this table enforces: mechanics 1, 2, 3 and 5 are position, information,
economy and concealment — expression channels the weapon wheel cannot occupy, which
is what makes protocols its rival rather than its accessory. Mechanic 4 is the one
row where a protocol sheet quietly demanded a weapons-lane rework; it is marked as a
decision so the growth stays deliberate.

## The modifier pool

Posture: a brainstorm bet, pressure-tested on paper, then grilled adversarially
2026-08-15; the amendments that grilling forced are folded into the points below. The
kill questions are at the end of this section; nothing here hardens until a
modifier-carrying Gatekeeper ships and reads in play.

The established points (Zo, 2026-08-15; amended by the grilling):

1. **The class system is a pool of modifiers, and the pool shares where it shares.**
   A modifier is a rule change — verb, target, trigger, aftermath — riding a
   substrate mechanic built once: the override, the render layer, the pile, the
   cloak layer (bill 1, 2, 3, 5, each with verified customers on both sides of the
   boarding). A protocol is a curated bundle of concentrated forms; the sheets above
   are the pool's Boarder-side presentation. What the two sides do *not* share is
   the wrapper: a Boarder form owes input, cooldown, HUD, RPC and logging
   (`PlayerAbility.cs` is 433 lines for four abilities), a Gatekeeper form owes a
   targeting branch and a telegraph (`Provoke()` is ~30 lines). The first draft's
   "authored once with two strengths" table is deleted — the mechanics bill is the
   contract, not a unified modifier ledger.
2. **The same modifiers go on Gatekeepers — authored, never rolled.** An elite is a
   kind wearing one modifier from the pool. The rolled half of PoE's rare-monster
   model died on four source facts: `WaveDirector`'s determinism contract (pure
   functions of wave index and dial, "host and clients agree byte-for-byte" — an RNG
   in the wave pipeline breaks stated netcode); measured kill cadence of one every
   0.6–0.8 s (`2-Dynamics.md` §2.3) against waves of 18–66 bodies, no time to read a
   rolled mod list mid-fight; the kind refusal contract (`GatekeeperKind` — a roll
   that touches a refusal gambles with the rules the game teaches); and no HP tier
   (`enemyHealthMultiplier` is one number for every kind) against a hatch budget
   already oversubscribed. Authored variants survive all four: fixed per-wave counts
   in the existing composition arrays, one modifier per elite, telegraph authored
   with the body. And nothing is free — a Boarder-authored modifier still owes its
   dilution and telegraph as a second authoring job. Antecedents in the mirror
   table: Magnet Hands inverted is a saboteur elite that hunts your deployables;
   Hairtrigger diluted is an elite that flickers into the layer when first acquired;
   Dead Letter diluted is an elite that pays as a pile at its corpse.
3. **The inspiration runs Gatekeeper → modifier → Boarder — proven three times,
   aspiration for the rest.** The shipped proofs: `_provoked` → Warden,
   `_chasingDecoy` → the override family, the Hunter's hunt → Writ. The other ~48
   player-side rows on the sheets were authored Boarder-first from genre precedent;
   the mirror table below is the quarry for making the direction true, not evidence
   that it already is.
4. **The entrance is Headhunter, inverted — and the pitch waits.** PoE2 prices
   wearing elite modifiers at a mythic chase — the Headhunter belt; here it is the
   door. But no elite ships in the funded horizon (bill row 13, absent in source —
   "elite" appears in `Assets/Scripts` once, inside the word "Roguelite"), so at
   launch the sentence *you wear what elites wear* is false; the true launch-day
   sentence is its inversion — elites, when they arrive, will wear what you wear.
   The pitch lives in the fiction hook until the one-Gatekeeper probe reads. It does
   not go on a panel before then.

Seed inventory — the overlap already on the sheets, found in the 2026-08-15 source
audit:

| Gatekeeper behavior | Shared system | Boarder expression of it |
|---|---|---|
| Default's damage lock (`_provoked`, permanent) | The provoke flag | **Warden** writes the identical flag, pointed at itself |
| Decoy-chase (`_chasingDecoy`, the shipped goal override) | Windowed target override (mechanics bill 1) | **Phantom Echo**'s ghost, **Summons**, **Challenge**, **Thrown Voice** — all generalizations of the chase |
| Hunter's player-hunt | Targeting/goal selection | **Writ** — a windowed hunt, leashed and pointed at the caster; **Ping**'s pull is the hunt invited |
| Thief's absolute refusals | Kind refusal rules | The wall every ability respects; **Doorplate** is the one legal answer to a Thief |
| Caller (`M63`, planned) | Overriding others' targets | **Ping** is the call inverted — onto yourself |
| Thief (`M63`, planned) | World pile (mechanics bill 3) | **LEDGER**'s entire sheet, played from the other side |
| Standoff Shooter (`M63`, planned) | Ranged emplacement | **UTL-H** is your standoff shooter; **Aegis Dome**'s reason to exist |
| Leaper (`M63`, planned) | Burst movement | The mobility cut's whole docket (Sally, Slip, Dead Run) — the enemy holds the verb the player was deliberately denied |
| A stalker kind (unplanned, candidate) | Cloak layer (mechanics bill 5) | **VEIL**'s layer, built once, wearable by either side |

Three consequences, deliberately priced:

- **Dual-demand pricing.** A mechanic with a customer on both sides of the boarding —
  a kind and a protocol — is cheap per expression row: the override serves the caller
  and three protocols, the pile serves the Thief and LEDGER, the cloak layer serves
  VEIL and a future stalker kind. A single-sided mechanic gets grilled harder; the
  ammo reserve (bill item 4) is the current example with no Gatekeeper customer, and
  it is already the one marked as a decision.
- **Co-planned slices.** Each `M63` kind lands with its mirrored protocol material —
  the existing sequencing already does this once (LEDGER after the Thief); the bet
  makes it the pattern, not the coincidence.
- **The mirror is a quarry, not a checklist.** Every kind is a candidate ability
  source and every pillar mechanic should look for its second user, but symmetry as a
  quota — every kind must yield a protocol, every protocol must map to a kind — is
  the filler trap under a new name. HIJACK stays the one pure-form lift, one per
  roster, as flagged.

Kill questions, written before anything is built on this bet:

- **Dilution meets the noise floor.** A weak modifier on a body in a crowd may be
  imperceptible — the same reason numeric passives are banned for Boarders, and the
  documented PoE failure (rare-monster mods nobody can read mid-fight), at a TTK
  10–100× this game's. The cheapest test: ship ONE modifier-wearing Gatekeeper and
  ask after a Run whether the player can name what it did. If they cannot, the
  diluted form is dead and the pool serves Boarders only. **The probe is exempt from
  bill row 13's gate** — the gate cannot wait on the probe it depends on — and ships
  as a one-off, no scaffolding.
- **Some modifiers refuse the dial.** Cloak diluted across a horde is an invisible
  wave — a legibility disaster, not a variant; Testament diluted is nothing. The pool
  shares where it shares; forcing every row through the concentration dial is a
  quota, and quota systems ship dead rows (the evidence section's record).
- **Elites do not exist.** No elite scaffolding is in source — kinds only, no variant
  tiers (mechanics bill 13). The elite recipe is the bet's biggest prize and its
  biggest unbuilt dependency; if elites never ship, the pool has one customer and
  point 2 falls back to shared systems only. Dual-demand pricing and co-planned
  slices already stand on their own without it.

The minimal surviving version, after the grilling: four substrate mechanics —
windowed target override, through-wall render, world pile, cloak layer — built once
at the seam the code already has (`Retarget()`/`TryGetGoal()`), priced by customers
on both sides of the boarding. Protocols are their concentrated consumers now; each
`M63` kind lands with its mirrored protocol material. Elites, when built, are
authored variants — one modifier each, fixed per-wave counts, an HP-and-telegraph
tier authored with the body, never rolled, never touching a kind's refusal. The
Gatekeeper side stays gated on the one-Gatekeeper probe; the pitch stays in the
fiction hook until that probe reads.

Fiction hook, parked for the naming pass: protocols as reverse-engineered Gatekeeper
doctrine — *you wear what elites wear, concentrated*, told as story.

Naming note (Zo, 2026-08-15): the game itself will most likely be renamed
**Gatekeeper's Protocol** — the protocol system is the game's core identity and is
treated as such. The title and the fiction hook are the same sentence. The rename is
Zo's call, not yet stamped; nothing renames until it is, and the working names above
stay on the codename rule regardless.

## What the shipped-game evidence says

- **Identity concentrates in the always-on layer.** Remnant 2's secondary archetype
  keeps skills and perks and loses exactly the prime perk — the game's own statement of
  where the class lives. DRG's CEO: the tools, not the weapons, make each class feel
  different. Doctrine is this system's prime perk.
- **Verb swap buys the most identity per authored line** (DRG overclocks); a
  target/faction flip is the most memorable and the most expensive (Payday 2's Joker);
  numeric riders read as build math, never as class feel (Remnant's minor perk rows).
- **Subtraction is free identity** (Vermintide's Grail Knight loses the ranged slot and
  is the most distinct career). Protocols subtract by exclusivity: only VEIL has the
  layer, only LEDGER moves piles, and the turret goes silent under VEIL and AEGIS.
- **Quota systems ship dead rows** (Vermintide's stagger-THP talent; DRG's 27 unused
  overclocks). Kill criterion 5 is the guard.
- **Identity ships whole; options pace** (2026-08-15 survey). Every surveyed success
  grants the class at hour zero and paces the entries — DRG's four free classes,
  Vermintide's level-7 second career; Killing Floor 2's level 0 is the failure on
  record.
- **Slots never lock, entries do.** PoE's economy-gated sockets were that game's
  longest-running complaint and the sequel deleted them; Back 4 Blood fixes the deck
  and paces the cards. The lone slot-unlock success, DRG's perk slots, is rare, late
  and additive.
- **A branch stays live only on a named axis** (2026-08-15 branch survey). Kingdom
  Rush's tower branches change the tower's job — swarm/elite, stall/kill — and its one
  documented dead branch is the specialist whose niche was rare (Musketeer vs Rangers).
  Bloons' dead-crosspath record names the causes: a side off the build's real output,
  a side bundling two axes (Ninja 1xx), a side redundant with the skill itself. Hades'
  flat-number hammers are the bait case — an additive number in a verb system is dead
  on arrival. Warframe's band-aid augments are the mandatory case — a mod completing
  an incomplete base is a tax, not a choice. And Kingdom Rush Alliance deleted the
  branch to fix balance and got "cookie-cutter" backlash: dead branches get repaired,
  never removed.
- **Unlock-by-deed delights when the deed is chosen, advertised, and pays choice** —
  V Rising 1.0 replaced fixed boss-drops with spendable tiered points and reviewers
  called it the single best change — **and poisons when one mandatory activity gates
  the fantasy** (PoE2's Ascendancy trials, its most-hated system).
- **Class identity stays on the player; the gear-carried budget is already spent.**
  No Rest for the Wicked, the strongest gear-carried-identity game shipped, moved
  utility abilities off weapons in 2025 and is adding a player-carried class layer for
  1.0, citing onboarding, respec and legibility. Endless Dungeon's most distinct
  heroes are the ones expressed through the shared turret hardware — which is this
  system's doctrine layer, validated in-genre. The guns' `F` abilities and the
  doctrine matrix are the right amount of gear-carried identity; `G`, `H` and the
  Passive stay on the Boarder.
- **PoE2 and V Rising both locate class in a small always-on rule layer** (an
  Ascendancy's few keystones, a blood type's stance) and generate variety from a
  shared ability layer the class does not own. The doctrine is this system's version,
  and it also answers the problem both games solve with pricing: an off-meta protocol
  changes the team's machines instead of being stat-taxed into irrelevance.

## Kill criteria

Pre-registered. Evaluate at **10 logged Runs after the second protocol ships** —
before VEIL lands, criteria 1 and 3 have no rival to read (`M70`'s recut carries its
own kill criteria for the Default alone) — or **2026-11-30**, whichever comes first.
The embark log line names protocol plus all picks; each doctrine clause logs one line
per proc (`Doctrine` tag).

1. **Protocol monopoly** — one protocol on more than 8 of 10 embarks: recut the
   others, or the system.
2. **Doctrine inert** — a clause whose log lines are absent across the Runs where its
   protocol embarked did not change play: fold that clause to flat hardware.
3. **Twist monopoly** — one side of a twist pair on more than 8 of 10 picks with no
   situational skew (pick rates from the embark line): revive the dead side with a
   behavior change — never a flat buff, and never by deleting the choice (Bloons'
   repair record; Kingdom Rush Alliance's deletion backlash is the counter-case).
   First expected failure on record: Recall's Escrow/Beacon (audit note on its sheet).
4. **Slot collapse** — `M70` kill criterion 2 (one active slot below a fifth of the
   other's activations), re-evaluated here across the roster.
5. **Quota kill** — evaluated at authoring, not play: an ability that cannot field a
   pair passing the twist-pair law ships naked, and a protocol that cannot field a
   second passive passing the bar ships with one passive and no pick. The quota
   bends; the bar does not.
6. **Job collision** — evaluated at authoring: a candidate protocol that cannot name a
   job and doctrine column no authored protocol owns is not authored.
7. **Dead first session** — a fresh save's first embark shows the abilities unused,
   or a first-Run session note says nothing about the protocol: the day-one grant is
   too thin. Widen it.
8. **The price displaces the gate** — deed gates clear but purchases lag them by
   multiple Runs, or protocol purchases visibly compete with FABRICATION hardware for
   the same banked salvage: the price is not modest. Lower it; the deed is the pacer.
9. **Railroad** — every player's unlock order is identical where a choice was intended
   (the Back 4 Blood supply-line complaint). Loosen the order or stop presenting it as
   a choice.

## Pre-mortem

- **Filler twists ship anyway.** The structure pressures every ability to a pair.
  Observation: a twist whose trade column took more than one rewrite to state, or
  whose pair has no nameable axis, is the filler.
- **A twist becomes mandatory.** If one side of a pair fixes a hole in the naked
  ability, the pair is a tax (Warframe's band-aid record). Observation: session
  notes describing the base ability as unusable until an unlock. Fix the ability, not
  the twist.
- **A generalist masquerades as a pair.** One side good always, the other good rarely
  (Kingdom Rush's Musketeer). Observation: kill criterion 3's pick-rate audit.
- **The matrix compounds.** Every future payload owes four clauses or reads unfinished.
  Observation: the next payload milestone arrives at double its expected size. The
  baseline-until-played rule above is the cap; hold it.
- **Sold classes price expression away** — the live half of the overturned Do-not-build.
  Observation: session notes read "wanted Cloak but I'm AEGIS." The answer on file is
  buying a second protocol, not cross-picking; if that answer keeps failing, the
  protocol layer is wrong, not the sheets.
- **A Crew ability drifts selfish.** The beneficiary rule says output to the crew,
  cost to the caster. Observation: a co-op session note where a Crew press helped
  only the presser, or a sheet where the crew's share of the output takes more than
  one sentence to state.
- **Co-op-weighted twists read dead solo.** Every co-op-weighted twist (Peel, Shroud,
  Sentinel, Class Action, Handoff) already sits in a parked pair by the selection
  rule; when a second slot ships one, the panel labels it, and a twist that still
  reads dead moves to alternates rather than gaining a bolted-on solo effect.
- **LEDGER erodes the banking pillar.** Two flags on its sheet already (Testament,
  Actuary). Each break costs a dated sentence (`../Working-Rules.md`); a third proposal
  shaped like a conversion bypass is the signal to stop recutting the wall and recut
  the protocol.
- **Expansion rows get built early.** Each names its gate. Observation: any expansion
  row appearing in a milestone before its gate ships.
- **The deed system grows a second game.** Counters, catalog progress rows, save
  migration — all of it lands in one slice. Observation: the unlock skeleton's
  implementation steps outnumber the slice's ability steps.
- **A deed reads as a chore.** The model's whole defence is that the deed is play the
  player was doing anyway (Albion's property). Observation: a session note naming
  grinding toward a protocol as why the Run was played, displacing the Run's own
  goal — that deed is mis-themed; re-theme it, never bypass-price it.
- **Short Runs make the price bind before the gate.** Weapons, hardware and protocols
  draw on one banked wallet. Observation: kill criterion 8's lag, or Ship logs showing
  protocol purchases deferred across many Runs while gates sit cleared.

## Graduation

Graduated by direction (Zo, 2026-08-15): **`M70` is the Default Protocol**. Recut
2026-08-16 (Zo): Default is a CREW claim, Barrage/Rush replace Anchor/Bulkhead/Ghost/
Foresight on that sheet, Coming Soon stubs stand in for specialists, Fight shortcut
is the G/H readout.

**VEIL graduated to `M94`, closed 2026-08-24.** FORGE's first ship was also `M94`; the
2026-08-29 recut (`M106` turret charge/Exhausted, `M107` Outrider/EMP/pairing) is what
0.34.2 runs. Magnet Hands retired. VEIL's Passive shipped as Ghost, not the Sightline
its sheet names. PARALLEL BAYS still prints "Not yet active." FORGE's passive is open
until `M107` is played.

AEGIS and LEDGER are what is left to graduate from this file, and the ordering is
unchanged: AEGIS last. Do not re-graduate FORGE.

## Open questions

- **When does a second twist slot ship?** The parked pairs are recorded; the lever is
  named in What this is. Written intent: not before the kill-criteria Runs exist, and
  one ability at a time. The veto backlog rides with it — Attuned Orbs (bends the
  identity-blind orb rule), Ration (bends Recall's 0-unbanked refusal), Sally and
  Round Trip (mobility cut) are all parked, and each needs Zo's call only when its
  slot ships.
- **Deed values and co-op credit** — each deed's threshold is a #12 value named at
  its protocol's slice, and per-kind kill attribution for the first deed (destroy X
  Defaults) needs its counter written host-side at the kill seam (`RunSim.AwardKill`
  knows the killer; the kind must ride along). Co-op client credit rides `M65`'s
  mechanism (`../Decisions.md` already commits deepest-wave to rise from co-op
  clears). *The superseded account-XP analysis (waves-cleared earn rule) is in this
  file's history if deeds fail their pre-mortem.*
- **Gear-carried revamp** — researched 2026-08-15 at Zo's request (No Rest for the
  Wicked shape: abilities on weapons and gear). Recommendation on file is no; the
  evidence paragraph above is the argument. Zo can overrule.
- **Second-protocol rules** — swap-at-Ship is written; whether owning two ever allows a
  Remnant-style secondary (doctrine from one, an ability from the other) is parked until
  the base system has play data.
- **Every number** — the protocol price, every deed threshold, windows, cooldowns,
  radii, the Watchtower's reduced siphon clock: #12 starting values, tuned only after
  the kill-criteria Runs exist.
- **Names** — DEFAULT, VEIL, AEGIS, LEDGER, FORGE are working names on the codename
  rule; STOCK is the Default's doctrine name on the same rule.
