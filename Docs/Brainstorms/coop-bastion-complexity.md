# Co-op bastion complexity

<!--
Purpose: handoff. What Zo decided this thread, what the build does, what
is still open. Extra players should author more intensity, not dump extra
guns into the same trickle.
Status: brainstorm — not a milestone
Update trigger: when the faucet loop shape, player-count wave scaling, enemy
health, extra terminals, cores, or run-loss rules change
Last updated: 2026-08-25 (M102 build state recorded, M98's priced respawn
replaces the buy-back gap, retired Bastion.unity fence removed)
Audience: another agent. Do not require the chat log.
-->

## What Zo wants

The co-op station is too small and too simple for two or three players.
Wave 1 in one room does not occupy three people. Max complexity at three
seats; lock parts down at two. Per-player required terminals are **not**
the only lever, and must not be required (if one person is down, the
others still open the door). Optional extra terminals that pay for risk
are allowed.

This is not Left 4 Dead. Do not add specials whose job is to break a
stack. Reference wave games: Guns 'n Goblins (ancestor for the call, not
for intensity) and Endless Dungeon (hold the wave; extra rooms are
optional upgrades). Fun co-op, resources that buy bonuses and expression
the player chose.

Most testing is solo. A friend is a scheduled session. Three players is
rare. Do not wait on a three-player log.

## Intensity (short name — do not sprawl this)

You **pay** for intensity. Intensity returns a **multiplier** on rewards,
or an **unlock** for rewards.

Juice at a trigger is one payment: Unbanked in, more Defaults and more
Salvage per kill from one table (`IntensityJuice`, `TetherSiphon`). Cost
100 Unbanked per `+` step. `-` is free, no refund. Intensity 0 is a legal
start. Juice does **not** start the wave. `E` then `T` does. Assault
locks intensity. Juice does not raise HP, group size, spawn gap, Hunters,
or Thieves.

**Stake** is the category: pay to raise danger and payout from the same
source (`3-Mechanics.md`). Juice is one stake. The hatch bank is another
(Unbanked converts or wipes). Carry is a third. Do not call juice “the
stake.”

**Cores** are intensity you can hold. Each core has a named risk/reward
axis (density, toughness, tempo — names in `economy.md`, Zo 2026-08-20).
Spend one to harden that axis on the wave, or to buy a Protocol/Run
multiplier on you. Same one-source rule. Currency split lives in
`economy.md`. This file only needs cores as intensity-in-object-form.

The faucet loop (`M102`) moves the intensity choice to the shutoff draft;
whether the shipped juice dial survives beside it is Open.

## Ancestor — Guns 'n Goblins, then the split

Guns 'n Goblins (`1-Experience.md` §1.1): ring a bell to call the swarm;
a failed trip can still pay. Beliefs 1 and 4 started there. Intensity is
**not** their bell.

Their bell is **one object**: it starts the swarm, and losing it ends the
night. This game splits that object in two:

| Object | Job |
|---|---|
| **Field terminal** | Starts the wave (`T`). Wave hunts the player. Kills fill Unbanked. Not safe yet. |
| **Hatch** | GameObject. Door to the Ship. Destroyed → Run is **Lost**. Hatch wave is the **bank**: Unbanked starts converting; converted Salvage is safe. |

Dying on a wave **costs Unbanked**. Respawn is not immediate. Delayed
in-Run respawn is **open**.

**Lost is the Hatch dying** (or failing the bank). A terminal wave is
earn-and-risk, not the lose object.

This game also inverts the Bastion (theirs; you board) and refuses idle,
XP sheet, build-your-fortress, thousands on screen, single-player as the
only session.

## The loop (shipped)

1. Look at a field terminal. Optionally pay intensity. `E`, `T`. Wave
   hunts **you**. Kills → Unbanked.
2. Walk to the Hatch. Optionally pay intensity. `E`, `T`. Wave hits the
   **Hatch** (and sometimes you). Survive → Unbanked converts (safe).
   Die or Hatch destroyed → Lost; Unbanked gone; banked stays.
3. Spend **banked** Salvage on the Ship (guns, Protocol, Fabrication).

Leaving the first room is this rotation. It is already in the game.

## The faucet loop (graduated to `M102`)

Decided this thread (all Zo, 2026-08-24), spec and criteria in the
milestone — the milestone is the record. **Built 2026-08-24 and dev-gated off
by default; harness green and re-verified 2026-08-25 after the Convert rename
landed on top (13 `FaucetLoopTests` pass). Zo's playthrough, which is the
experiment itself, is owed** — so nothing below has play evidence yet:

- **Waves hunt the Hatch.** A Gatekeeper is provoked onto a Boarder near its
  path. Hunt-the-Boarder stops being the field grammar. This retires the
  spawn-distance problem: spawn points are fixed, intent is readable from
  direction of travel, and the layout can author the fight.
- **Terminals are faucets with an off verb.** Bringing terminals online
  unlocks more of the Bastion; the spawn surface is the unlocked footprint,
  so progression and escalation are one fact.
- **Unbanked rides Waves endured**, more than kills. Shooting is defense,
  income is nerve; Hatch HP governs greed, so no anti-farm rule is needed.
- **After N Waves the game marks a terminal to turn off**, picked from an
  authored seat set (seats refined by hand for tower-defense path reading).
  The mark is the commitment floor — no off verb before N. Holding to N+1
  and N+2 grows the modifier draft. Waves keep coming while you hold and
  while you travel.
- **The off verb opens the draft**: any Boarder may initiate, every seat
  votes on a visible panel, the party lead confirms. Choose the modifier —
  spend it on the coming stand, or carry it to the next terminal — then the
  party teleports to the Hatch and defends.
- **Traversal is asymmetric.** Back to the Hatch is cheap; out to terminals
  is on foot through live streams and unforgiving. All trip risk sits on
  the opt-in leg.

The greed dial is the hold: leave at N for one option or ride to N+2
against more Hatch attrition and a harder trip. Legibility is three
countable constants — Hatch loss per Wave, unbanked per Wave, trip cost —
plus the Wave count itself; if any stops being countable the decision
collapses into guessing.

## Co-op problem (not “they never walk”)

Extra guns melt Wave 1 in one doorway. Count already scales with
`playerCount`. Health does not (`WaveDirector.EnemyHealthPerWave` 0.40;
Wave 1 multiplier 1.00). MK-1 T0 one-shots a Wave-1 Default (30 HP).

`NextWave` multiplies **body count** by n. Group size and spawn interval
do not. Wave 1 L0: 18 vs 54 bodies, groups of 3, gap 4.2 s → **0.71
bodies/s** at any n. Solo delivery ~21 s; three seats ~71 s. Same pipe,
longer wave. Extra Defaults from juice use that same gap. Three guns eat
both.

Co-op splits Salvage evenly (`Decisions.md`, M65): further, not richer.
Extra seats should mean more intensity they chose, or further into the
station, not more Salvage per person.

## Layout facts (`Tools/bastion-sketch/bastion-coop-layout.txt`, M95)

28 rooms, two storeys, kit scaler 2. Ground = demo spine R1–R15.
T1+T2 → D1, T3 → D2, T4+T5 → Control D3. Upper islands do not skip
those gates. Stairs `R1 U1`, `R6 U6`, `R10 U10`.

Three hatches on **R1** south at **−14, 0, +14** (28 m span). Walk 5 m/s.
M95 criterion 5: hatch 1→3 inside 10 s (test `28 / 5`). Two seats: hatches
1 and 3 open, hatch 2 sealed; S7–S12 muted. Three seats: all three
hatches; S7–S12 live. Upper closets still path to R1 via the stair.

R1 is one OpenArea (42 m class). During a **terminal** wave the Hatch
cannot die, so three mouths in R1 do not occupy extra guns. During a
**hatch** wave terminals are idle, so T1/T2 in the next rooms do not
occupy extra guns on that fight.

Do not SceneBuilder Build All over authored stairs. (`Bastion.unity`, the
old solo scene this line used to fence off, was retired 2026-08-24; the
combat scene is `Bastion_Coop` and the practice range is `Range.unity`.)

## Build vs this pattern (gap)

Narrowed by the `M98` recut (2026-08-24). One priced respawn rule replaced
the buy-back windows: a dead seat — host and solo included — pays out of the
Unbanked it is carrying at `150 × uses+1` (`Sim/Run/Respawns.cs`, the
counter the Run's, not the seat's), and host/solo death becomes a DOWN state
whenever that offer can be honored. A seat carrying nothing cannot pay, and
there the first loss still ends the Run. So the gap is now a narrow one: the
pattern above says Lost is Hatch destroyed / bank failed, and the build still
ends a Run on a field-wave death that cannot buy itself back. Do not “fix”
host-death as a side effect of a layout pass.

## Refusals this thread

- Required `T1_1` AND `T1_2` bound to who embarked.
- Isolate/punish kinds as the co-op model (L4D smoker/boomer).
- Treating `bodies × n` or juice-as-shipped as a 3p difficulty fix.
- Treating “leave the first room” as the co-op puzzle (it is the loop).
- Equating juice with Guns 'n Goblins’ bell.

## Superseded (2026-08-24)

- **The first inversion** — the Hatch-bank activating terminals the party
  runs out to shut off. The faucet loop's on/off verbs keep authorship with
  the Boarder and supply the same occupancy and traversal.
- **Mandated funnels = seats+1.** Emerges instead from footprint and faucet
  count; the layout's job is to make seats+1 available and attractive, not
  to impose it.
- **The walk back after shutoff.** Dead time after the decision was made;
  replaced by the teleport, with all trip risk on the outbound leg.

## Loop vs arc

**Loop.** The faucet loop is a loop by construction: the authored seat set
and spawn rooms are a fixed cost; the per-cycle pick, the hold decision and
the draft supply the variety.

**Arc.** A bigger hull only for three players, or a unique terminal each
player must use.

## What would kill it

Pre-registered, structure not feel:

- The hold decision is not real — always leave at N, or always ride to
  N+2. The attrition/draft curve is flat. Structural, not tuning.
- The outbound trip is never contested — the teleport removed the wrong
  half of the risk.
- Standing at the Hatch and mowing converging streams beats everything —
  the one-doorway problem at a new address. Funnel spacing, not numbers.
- A Boarder cannot tell whether another Wave is affordable — the three
  constants are not countable. Fix legibility before touching numbers.
- Extra terminals nobody uses, or someone says “your terminal.”
- A core axis that does not change who stands where.

The old-frame levers (extra Default HP per seat, `bodies × n` as the fix)
are parked, not open: revisit only if the paradigm dies.

## Tests (one lever at a time)

1. **Solo graybox first** (`M102`): two spawn rooms feeding the Hatch,
   terminal marked at N, draft, teleport, Hatch stand. Named question:
   **is the hold decision real** — does the player ever leave at N, ever
   reach N+2, and when do they choose to travel?
2. **Friend session after**: does the vote panel produce a conversation
   (“do we take N+2?”) or a rubber stamp, and did the second Boarder have
   a job at the stand?

## Open

- Draft semantics: one pick from a set that grows at N/N+1/N+2, or
  additional picks. `M102` builds pick-one-of-a-growing-set; revisit on
  play.
- Is the modifier the **core** (`economy.md`) or a new class? Folding
  keeps one reward system; do not grow a second beside it silently.
- What feeds the Hatch stand after shutoff — remaining bodies converging,
  a surge, or the next faucet — and what converts unbanked during it.
- Do kills pay at all under Waves-endured income, or only the endure bonus.
- Does the shipped juice dial survive beside the draft, or fold into it.
- The language table's **field terminal** row is recut when the faucet
  loop ships for real (not for the graybox).
- Lost rules — the respawn half landed with the `M98` recut (above); what a
  field-wave death should mean when nothing can pay is still Zo's, worked
  separately. Do not fix in passing.
- Recut R1 only if the one-doorway kill fires in the graybox.

## Pointers

Milestones are cited by bare id — the folder is the status, and
`Docs/Roadmap.md` is the addressable index.

- Layout: `Tools/bastion-sketch/bastion-coop-layout.txt`
- Faucet loop milestone: `M102` (the record for the graduated loop)
- Co-op hull milestone: `M95` (28 rooms, built, playthrough seams owed)
- Ancestor: `Docs/Design/1-Experience.md` §1.1
- Cores/currencies: `Docs/Brainstorms/economy.md`
- Co-op salvage: `Docs/Decisions.md` M65
- Do not activate M95 or recut the 28-room lock unless Zo asks
