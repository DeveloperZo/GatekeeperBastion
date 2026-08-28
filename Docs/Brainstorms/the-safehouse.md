# The Safehouse

<!--
Purpose: The Ship as a hub friends occupy between Runs — what there is to do there, and what those activities are allowed to pay.
Status: brainstorm
Update trigger: when an activity graduates, when the Ship gains or loses a station, or when session persistence for guests is answered.
Last updated: 2026-08-25 (Ship - Demo retired and removed from every claim here; M99's range door shipped; M101 hull recount; M100 answers the offline-contribution half)
-->

## What this covers

**Boundary with `realms.md`** (one canonical list per thing): that file owns ownership,
identity and where state lives; this one owns what there is to do. Activities here never
propose storage — anything the hub banks is realm state.

Everything the Ship is when nobody is embarking: activities, the personality they carry,
and what they may pay. The Ship's role *inside* a Run — the Hatch, the bank window, ship
damage — is `missions-and-destinations.md`. What the four existing stations sell is
`../Design/1-Experience.md` §1.4 and is not restated here.

## Where it stands

**The hub scene is authored, and it is the only hub.** `Ship - CoOp.unity` — **2,784**
GameObjects measured 2026-08-25, after `M101` rebuilt the hull over two decks — generated on the
grid convention (`Floor_x_y`, `GW_<dir>_x_y`, `GS_*`, `GB_*`) and then dressed, per the
generate-then-author paradigm. Its shape is `ship-hull-and-interior.md`, not this file. It
holds:

- **Four stations**, each a full fixture rather than a menu hook: `Station_Armory`,
  `Station_Fabrication`, `Station_Map`, `Station_CrewQuarters`, each with `Desk`, `Screen`,
  `Label`, `Console`, `ConsoleScreen` and `Interact`. **Three of those six are not doing
  what this list implies** (audited 2026-08-22): `Desk` and `Screen` are authored inactive on
  all four stations, and every reference to the per-station console materials
  (`Mat_ConsoleArmory` and siblings) sits on those inactive renderers — so the four live
  `ConsoleScreen`s all wear the generic hull atlas and no station has its own colour. The
  interact surface is not the visible console either: it is an invisible `Interact` volume
  2.25 m in front of the station, while the `Console` prop's collider is disabled and sits to
  one side.
- **Three hatches** — `Hatch_1`, `Hatch_2`, `Hatch_3`, sharing `Mat_Hatch` as their OPEN
  material only. The sealed material on all three is the hull atlas, and the leaves are
  authored with it, so **a sealed hatch is indistinguishable from the wall** and every door
  reads sealed until the first assignment runs. `Assets/Materials/Mat_HatchSealed.mat` exists
  and is referenced by nothing (audited 2026-08-22). The multi-port idea in
  `missions-and-destinations.md` already has its geometry.
- **The teleporter** — `Demo Teleporter` and `Spawn_Teleporter`, so `M91` landed.
- `Environment`, `ShipDress`, `Level Music`, `Player`, `ShipController`, `Ship Terminal`.

It shares the Bastion's look — several hundred references to the Synty atlas material — so
the 2026-08-20 repaint reaches it.

**So the hub is a place with fixtures in it, and the gap is narrower than "build a hub".**
`M72` supplied the raw capability — a party over Relay, party-wide scene changes, the lead
calling EMBARK — and the scene supplies the room. What is missing is that **every one of
the four stations is still a shop or a launcher**: something you open, transact at, and
close. Nothing in the Ship is an *activity*.

**Wired 2026-08-21, corrected 2026-08-22, closed 2026-08-25:** the scene pair is gone.
`SceneFlow.ShipCoop` is `"Ship - CoOp"` and there is no `SceneFlow.Ship` — the demo hub
retired with the scene-pair consolidation, so `Ship - Demo.unity` is not on disk and nothing
loads it. Every way in lands on the one hull: CONTINUE, the realm chooser's ENTER, party
Host/Join, the additive ship-wait, return-to-ship at the end of a Run, and the F12 dev
checkpoints alike. The claims this paragraph carried before — first that solo paths load
Demo, then that the DEMO button and the checkpoints still reach it — both expired.

**A consequence worth naming**: a fresh realm is `DemoActive`, so the first-Bastion tutorial
plays out on the hull, and until 2026-08-22 two of its safety systems still tested for the
demo scene — the 1-HP damage floor did not apply here, and a tutorial death on the Ship had
no lose screen to land on. The tutorial is an arc consumed once by a player while its flags
live on the realm, so every new world used to teach it again; `Settings.SkipShipTutorial`
(2026-08-24) is the opt-out, set by finishing the ship fight, read by `GameSession.Adopt`, and
defaulting off so a first-time player is still taught.

## Graduation test

Whether an activity that pays nothing is worth a friend's time — answerable only by two
people spending a session in the Ship without embarking, and reporting whether they
wanted to.

## Decided

| Decision | Owner, date |
|---|---|
| The Ship is the hub. Activities live there or behind a door from there, not in a separate menu flow. | Zo, 2026-08-20 |
| First pass adds no new systems and no depth — a set of actions, not an economy. | Zo, 2026-08-20 |
| Activities are doable **prior to or in lieu of** core gameplay. Never a prerequisite for it. | Zo, 2026-08-20 |
| Bonuses, where they exist, stay small. | Zo, 2026-08-20 |
| **The point is asynchronous contribution.** A player's time should visibly impact gameplay they are not present for — the survival-game shape: someone stocks the chest at 2am, the crew spends it at 4pm. Accepted as a bet, chore risk included. | Zo, 2026-08-21 |

## Ideas

### A lobby of doors and a room of props
The hub splits by weight. Anything needing a live Gatekeeper, a NavMesh or a
`GameController` is **a door** to its own scene, and `M99` proved the shape rather than
predicting it: `Range.unity` exists, it is registered in Build Settings, and a `Range Door`
in `Ship - CoOp` opens it (closed 2026-08-23). The machinery stays out of the one scene
deliberately free of Run machinery, and the fiction still reads as your hideout because you
walk there. Anything that is a prop and an animation is **furniture in the Ship**. That split
decides where each idea below lands before it is designed, and it keeps the hub scene — most
of whose object count is already UI under `Canvas` — from absorbing every new feature. (This
paragraph named `Ship.unity` until 2026-08-22; there is no such file, and there is now only
one hub, `Ship - CoOp.unity`.)

### Calibration (the Garrus nod)
A bench where a weapon is calibrated: a deliberate action, a short animation, a small
stated effect. The joke in the reference is that the calibrating never visibly does
anything and the character never stops, so **the nod lands better as a small effect and an
absurd amount of flavour than as a real tuning system**. It is also the cleanest test of
the pay-for-an-action rule below, because a bench is obviously an action and obviously not
a clock. *Open: whether the effect is per-Run or persists, and whether a second
calibration on the same weapon does anything.*

### Contribution that outlives the session
The intent is the survival-game shape rather than Deep Rock's: a player stocks the chest at
2am and the crew spends it at 4pm, and the payoff is **seeing your own mark in someone
else's run**. Albion's guild islands and Foxhole's logistics arm are the pure cases — in
Foxhole an entire half of the playerbase never fires a shot and produces everything the
front consumes, which works because every crate is attributable and consumed visibly. What
transfers is not the depth of those economies but the property that makes them feel good:
**attribution, not simultaneity.** A contribution that persists with a name on it delivers
the feeling; a shared clock is not what does the work.

### The realm is already the answer to where it lives
`M96` splits one save into two: a **Boarder** that travels with the player and a **realm**
that stays with its owner, and *ship and hatch upgrades* are explicitly on the realm side.
So a contribution made in someone's Ship persists in their world by construction — Zo's
2026-08-20 ruling, that a guest does work on someone else's ship and takes their own kit
home, is the seam this file was asking for. **This brainstorm proposes no storage of its
own**; anything the hub banks is realm state (`realms.md`, `M96`).

What the realm does not carry is **whose it was**. The upgrade persists; the name on it
does not, and the name is the payoff — seeing your own mark in a run you were not present
for. That is a field, not a system, and it is the smallest thing standing between `M96`
and the feeling this file is chasing.

### A crew board
Deep Rock's loadout screen sits in the hub and shapes the mission without being played.
The equivalent here is a board where the party sees each other's Protocol and kit before
EMBARK — no new system, since `M70` already claims a named Protocol and `M72` already
knows the party. It is Fellowship (rank 1) for close to nothing: the value is that three
people can see they have brought three of the same thing.

## Open

| Question | Why it matters | Owner |
|---|---|---|
| **Can a contribution be made alone?** | Answered in principle by `M100` (built 2026-08-24, two-machine criteria owed): a realm is no longer only a local save, it lives as per-member copies in Cloud Save with a single-writer lock, so a member can open the owner's realm while the owner is offline. What is not answered is whether anything in the hub is worth doing while alone in someone else's Ship — that is the graduation test, not a storage question. | Zo |
| Per-Run or persistent effects? | A per-Run bonus is a ritual before embarking. A persistent one is progression, and progression that only exists in the hub makes the hub mandatory — which the *in lieu of* rule forbids. | Zo |
| How many activities before the hub reads as a place rather than a menu? | Unknown, and the graduation test is what answers it. *needs play data* | — |

## Constraints

- **Pay for an action, never for elapsed time.** `../Decisions.md` cuts idle outright:
  *"Time never pays; standing in Prep earns exactly zero. `BaseSalvagePerSecond` is
  gone."* An activity that resolves on a deliberate action is not that mechanic; anything
  accruing while the player stands in the room is exactly that mechanic returning under a
  new name.
- **No activity may become the efficient path.** Belief 2 says losing does not take; a
  bonus large enough that skipping it is a loss converts an optional ritual into a tax,
  and *in lieu of core gameplay* stops being true. **This is also the whole of the chore
  question**: contribution reads as a chore when it is required, not when it is quiet, so
  the rule above is the guard and no further one is needed.
- **Doors, not stations, for anything that needs a brain.** See *A lobby of doors*.
- **One subtree per edit to `Ship - CoOp.unity`** (`../Architecture/8-Scene-Authoring.md`).

## Rejected

- **The Jump Space model — the ship as an active job.** Cited as a reference, but its ship
  is worked *during* combat: fires on the command deck, the power grid adjusted mid-fight.
  That is a different game shape and it contradicts the Ship being the exhale
  (`missions-and-destinations.md`: the Ship is only dangerous when you have chosen to open
  the Hatch). The transferable half is that a ship can be a place with jobs in it; the
  during-combat half is out. Rejected 2026-08-20.
- **Albion's *depth* on a first pass** — a persistent market with real supply chains.
  The *shape* is not rejected and is in fact the intent (see *Contribution that outlives the
  session*); what is out for a first pass is the economy behind it. Narrowed 2026-08-21.
