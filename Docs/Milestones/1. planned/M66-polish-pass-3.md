# M66 — Polish Pass 3

<!--
Status: later
Update trigger: when hit feedback, enemy audio tells, or player identity change
Last updated: 2026-08-14
-->

Third friends QoL pass: **the fight reads**. Three surfaces in one gate, because a friend
who can join, operate the Ship and read the Compendium still cannot tell whether their shots
landed, cannot hear what is about to hit them, and cannot tell which capsule is which.

`../../_meta/_definitions/milestone.md` forbids a pass with no end state. This one has three,
numbered below. It is not an art phase.

Polish Pass 1 (`M59`) is the room and the hull a stranger can operate. Polish Pass 2
(`M61`) is the gun, the sight, and the sound of firing. This one is whether the fight
reads.

## Story

As a player who has never been briefed, I want to see that I hit something, hear what is
coming, and know which of us is which, so that the fight reads as a fight rather than as
capsules overlapping in a grey room.

## Why it matters

**Graybox systematically under-reads anything whose value is feedback.** That is the standing
rule in `.claude/skills/gkb-design`, and it is exactly the trap here: the shooting may be
fine and will be judged bad, because nothing confirms a hit. Friends will not say "the hit
feedback is missing." They will say the guns feel weak, and that is a verdict on the wrong
system.

**In first person an enemy you cannot see has no telegraph at all.** Every telegraph budget
this game has is audio. It is also how a player knows what is happening to a friend they
cannot see, which is most of a co-op session in a multi-level bastion.

**And three players makes identity a requirement rather than a nicety.**
`../../Brainstorms/co-op.md` deliberately shipped the avatar as a puppet capsule, correctly,
for a two-person loopback test. With `M65`'s third player, "who is that and did they already
take the salvage" has no answer.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** No mechanic changes and
no number moves. What changes is what the player can tell.

## Kill criterion

The first friends session after this ships. **If a player says the guns feel weak, or asks
who someone is, or is hit by something they had no way to know about, the surface it belongs
to gets recut** rather than argued with. If none of the three comes up, the pass is done and
does not grow.

## Acceptance criteria

### 1. The hit reads

1. **A landed shot confirms on the target and on the crosshair**, distinguishably from a
   miss, within one frame of the hit registering. Enough that a player can tell a hit from a
   miss with the sound off.
2. **A kill reads differently from a hit.** A player firing into a group can tell how many
   died without counting bodies.
3. **Damage taken reads.** The player knows they were hit and roughly from where, which in
   a space with vertical routes and blind corners is a direction problem and not a health-bar
   problem.

### 2. The fight is audible

4. **Every Gatekeeper kind in the build has its own pre-attack sound**, distinguishable from
   the others with the screen not showing the enemy. Three kinds today; the rule is written
   into `../../Brainstorms/gatekeepers-and-waves.md` so the twelfth kind arrives with one
   rather than needing a later pass.
5. **The Hatch taking damage is audible from anywhere a player can stand.** It is the thing
   being defended and it currently loses HP silently to someone who walked away.
6. **A downed teammate is audible**, so the answer to "where did they go" does not require
   asking.

### 3. Who is who

7. **Each player is visually distinct** at the range a bastion lane allows, without reading
   text.
8. **A name label resolves at close and mid range** and does not clutter the fight at
   distance.
9. **The fight HUD names the other players** and their state, alive or down, in a form that
   works for three and does not assume two (`M65` criterion 2).

### Nothing regresses

10. **A solo run is unchanged** apart from the new feedback. Same numbers, same session-log
    shape.
11. **Suites green, same names.** No new counter, no accumulator, no stats object
    (`../../Decisions.md`). No new menu row without asking
    (`../../Architecture/README.md`).

## Implementation

**Change** the existing hit path to raise a confirm rather than adding a parallel one, and
reuse `Game/Combat/Effects.cs` for the visual half.

**Create** the pre-attack sound hooks on the Gatekeeper state machine, one per kind, and the
rule in the brainstorm that a new kind ships with its sound.

**Change** the M61 `Boarder` to carry a colour and a label, driven from the player
registry rather than from a scene reference. Do not swap back to a capsule.

Rejected: **a ping wheel or voice.** The Boarder mesh and locomotion/fire/reload anims
are `M61` (Infinity Qube astronaut + Mixamo). This slice tints and labels that body;
it does not author a second rig. `../../Brainstorms/co-op.md` still parks voice.

Rejected: **damage numbers as the hit confirm.** They read as a different genre and they
invite balance arguments about numbers this project has already decided are the weakest
place to intervene.

Rejected: **a full audio pass.** Music and fire/footsteps are `M69`. This is
telegraphs and confirmations only.

Rejected: **growing the pass.** Three surfaces, and the kill criterion is what adds a fourth.

Rejected: **Unity Relay / friends join.** That is `M60`.

Rejected: **Gatekeeper meshes.** That is `M63` (androids: RobotFemale / HelmetArmor / War_Robot).

## Verification

1. Criteria 1 and 2 — fire at a group with the sound off and confirm hits, misses and kills
   are distinguishable.
2. Criterion 4 — stand with the enemy off screen and identify the kind from sound alone.
3. Criteria 7 to 9 — a three-player Multiplayer Play Mode session, each player identifying
   the other two at lane range.
4. Criteria 10 and 11 — one solo run diffed against the previous session log; both suites.

## Watch

- **This is the pass most likely to be judged as art and cut.** It is not art. It is whether
  the systems already built can be perceived, and a mechanic that cannot be perceived reads
  as a bad mechanic.
- **Audio scales with the roster.** Twelve kinds means twelve distinguishable sounds, so the
  rule in criterion 4 matters more than the three sounds it ships.
- **Labels get in the way fast.** A name over every player at every range is worse than no
  label. Range-gate it.

## Depends on / Unblocks

**Better after `M65`**, because criteria 7 to 9 are about three players, but not blocked on
it: two players still need to tell each other apart.

**Unblocks nothing.** It is the last thing between a build that works and a build a friend
enjoys, which is the only reason it is a milestone rather than a wishlist.
