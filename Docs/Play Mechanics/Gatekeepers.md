<!--
Purpose: Current representation of Gatekeepers — garrison-invoked targeting — as it is in the build: the mechanic page; the kinds are its instances.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# Gatekeepers

Gatekeepers are the garrison holding the Bastion, and the mechanic is how each one
chooses what to attack. Every body runs the same rule — go for the Hatch, or go for the
nearest Boarder — and a kind is defined by which part of that rule it refuses. The
Boarder's own conduct is the input: shooting a Gatekeeper flips it onto the Boarder for
good and alerts its whole group, and being seen across a room does the same, so attention
is bought with every shot fired and every sightline held. The kinds differ in what each
one refuses, and four of the twelve planned kinds are in the build.

## Game

They are not defending a wreck. They are home, and I am the thing that got in.

Four frames, four machines, not four paints — I can sort a group by silhouette before it is close enough to matter. None swings faster than another. None has a weak point. None resists anything. What separates them is what each one refuses to do, and each refusal tells me exactly what ignoring it costs.

One question sorts the whole room: can this be moved off what it is doing? A Default can, at a price. A Hunter is already on me and cannot be moved anywhere. A Thief hunts me on a field-terminal Wave, hunts the Hatch on a Convert, and will not be shouted at in either direction. A Cloaker moves only where nobody's eyes are, so moving it is free and holding it still is what costs.

They arrive in groups, a whole group through one spawn point, the next group somewhere else. The point rotates. I cannot hold a direction here. I can only cover what I can turn to in time.

That is the entire garrison that walks today. Nothing shoots. Nothing heals. Nothing makes another one stronger. There is no larger version of anything. Every one of those sentences is on the roster's list of things to take from me, and the pages marked Coming Soon say which frame collects.

And the rooms they walk out of do not open for me, which I think about more than I should.

## The interaction

The garrison invokes this one, not a Boarder seat — it is the entry the definition's agent clause names. Every Gatekeeper runs one shared targeting rule against the Boarding's state, and a kind is exactly the set of three questions it answers differently: may it target the Hatch, may it target a player, can it be provoked (`Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` is that seam).

- **Targeting.** Each body re-derives its target on a fixed tick: the Hatch, when its kind allows one and the Hatch stands, or the nearest live Boarder. The Hatch is targetable on both Wave modes (Zo, 2026-08-27 — field Waves no longer remove it, which had forced every body onto the player unconditionally); what differs between modes is where the player is standing. Inside the commit radius a Hatch-seeker latches onto the Hatch and holds that latch for as long as the Hatch is still a target for it — shooting it, standing beside it, shoving and pinning it do not clear it; only the Hatch leaving that body's world does.
- **Provoke-on-damage.** Being shot flips a provokable Gatekeeper onto the player permanently and shifts it to its chase gear. There is no cooling off.
- **The shout.** The body whose provocation just landed alerts every Gatekeeper within the alert radius; joiners are provoked without shouting again, so one shot pulls one group, never the Bastion by relay. Kinds that refuse provocation silently decline the shout.
- **Gaze-noticing.** Clear line of sight to a player who is inside notice range and *outside* the aggro radius provokes, exactly as permanently as being shot; the sight branch is skipped for a player already close enough to divert the body by proximity. Proximity inside the aggro radius diverts a body only while the player stays close — the one transient trigger. (Sight-provoke is suppressed on faucet field Waves, a dev-gated graybox mode; damage and proximity still work there.)
- **The Cloaker watch.** One kind's refusal is a state rather than a flag: watched — planar gaze cone, in range, clear line of sight, with a Schmitt release band so the edge cannot strobe — it refuses every target and holds position.
- **Group arrival.** A Wave is delivered as groups, and every Gatekeeper in a group enters at the same spawn point, picked once per group on a weighted rotation, ringed around it so nobody lands inside anybody. [Waves](Waves.md) owns the schedule; this page owns what a body does after it arrives.

Driven by no input of the garrison's own — the Boarder's fire, position and gaze are what it answers. Shown by the body's path and pace, the group breaching at a spawn point, and the Assault count. Reads back as log lines: `gatekeeper provoked — N nearby joined the hunt`, `group N: … breached at <point> — N live`, `gatekeeper reached the hatch — attacking home`.

## Consequences

Every shot is also a targeting decision: it buys the target's permanent attention and, through the shout, its group's — deliberately (a group pulled off the Hatch onto ground the Boarder chose) or by accident (a stray hit mid-crowd). Every sightline held is a silent provocation, so long corridors hand out pulls for free.

A Hatch-seeker inside the commit radius has stopped being a decision and become a timer on [The Hatch](The-Hatch.md)'s HP. A watched Cloaker costs the arc the Boarder's eyes were covering. And because each kind refuses a different part of this rule, sorting the group in front of the Boarder — what can be moved, what cannot, what shooting costs — is the game's moment-to-moment challenge.

Removal test: take garrison-invoked targeting out and nothing contests the Hatch or the Boarder — no Wave is a fight, the Ante prices nothing, kills cannot pay, and every Boarding ends the same. A different game; core.

## Classification

Core, loop. Serves Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. The Fantasy served is that each kind is an identity defined by its refusal.

## Interacts with

- [Waves](Waves.md) — which kinds a Wave sends, group width, gaps, health scaling and kind debuts, none of which this page restates; that page owns delivery and composition.
- [A Boarding](A-Boarding.md) — a Gatekeeper's death banks Salvage the moment the body falls, only while an Assault is live; that page owns kills-pay-on-landing.
- [The Hatch](The-Hatch.md) — the Hatch's HP is what Hatch-seekers spend their swings on; that page owns the withdraw and repair spends.
- [The Ante](Ante.md) — every extra body the wager buys is a Default, and the fixed kinds never see the dial; that page owns the ante spend.
- [Going Down](Going-Down.md) — garrison hits to the Boarder are what force the priced return; that page owns the respawn spend.

## Instances

Instances vary on the three targeting questions (Hatch, player, provocation — each kind is what it refuses), on their stat lines ([Field Manual](Field-Manual.md)), and on crowd behaviour (avoidance priority and solver quality). No instance may vary the shared swing clock — all kinds swing on one cadence, `GatekeeperAttackInterval`; the rule is `../Design/3-Mechanics.md` §3.1, *All four kinds share one swing clock* — and a variant that differs only in numbers is a Field Manual row, not an instance.

This table is the canonical roster of Gatekeeper instances, anywhere in Docs.

| Instance | Differs in | State | Page |
|---|---|---|---|
| Default | Refuses nothing — the whole shared rule, unmodified | in build | [Default Gatekeeper](Gatekeeper-Default.md) |
| Hunter | Refuses the Hatch, ever, at any range | in build | [Hunter](Gatekeeper-Hunter.md) |
| Thief | Refuses the player, provocation and the shout | in build | [Thief](Gatekeeper-Thief.md) |
| Cloaker | Refuses every target while any Boarder watches it | in build | [Cloaker](Gatekeeper-Cloaker.md) |
| Source | Takes the empty room — *behaviour undecided* | Coming Soon | [The Fifth Frame — Coming Soon](Gatekeeper-Source.md) |
| Enabler | Its business is the other kinds — *behaviour undecided* | Coming Soon | [The Sixth Frame — Coming Soon](Gatekeeper-Enabler.md) |
| Alarmer | Takes the group's size — *behaviour undecided* | Coming Soon | [The Seventh Frame — Coming Soon](Gatekeeper-Alarmer.md) |
| Bypasser | Takes coverage — *behaviour undecided* | Coming Soon | [The Eighth Frame — Coming Soon](Gatekeeper-Bypasser.md) |
| Reclaimer | Takes salvage on the floor — *behaviour undecided* | Coming Soon | [The Ninth Frame — Coming Soon](Gatekeeper-Reclaimer.md) |
| Exploder | Takes the ground the Boarder stands on — *behaviour undecided* | Coming Soon | [The Tenth Frame — Coming Soon](Gatekeeper-Exploder.md) |
| Standoff-Shooter | Takes the Hatch from range, refuses closing — *behaviour undecided* | Coming Soon | [The Eleventh Frame — Coming Soon](Gatekeeper-Standoff-Shooter.md) |
| Ailment-Shooter | Takes a resource that is not HP — *behaviour undecided* | Coming Soon | [The Twelfth Frame — Coming Soon](Gatekeeper-Ailment-Shooter.md) |

The eight Coming Soon rows come from the twelve-kind roster (`../Brainstorms/gatekeepers-and-waves.md`, named 2026-08-13, Zo — twelve is a target, not a promise).

## Where

- The kind seam: three targeting virtuals, one refusal each — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`MayTargetHatch` / `MayTargetPlayer` / `CanBeProvoked`)
- Provoke-on-damage, permanent for life — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`OnDamaged` → `Provoke`; the `_provoked` latch never clears)
- The shout pulls one group, not a relay — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Provoke`'s alertRadius loop over the spawner's live ledger; joiners called with `shout: false`)
- Gaze-noticing provokes at range; proximity is transient — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`CanSeePlayer`; `Retarget`'s sight-provoke call, guarded by `!playerNear`; `Retarget` re-derives `playerNear` every tick and passes it to `ChasesPlayer` instead of latching it)
- Sight-provoke suppressed on faucet field Waves — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`faucetField` guard in `Retarget`); `Assets/Scripts/Sim/Run/RunSim.Faucet.cs` (`FaucetHatchHunt`)
- The Hatch is targetable on both Wave modes; commit latches inside the radius — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Retarget`, 2026-08-27 note; `_committedToHatch`, `hatchCommitRadius`); shove/pin do not clear it — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Shove`, `Pin`, `IsCommittedToHatch`)
- The Cloaker watch, pure half (cone, range, Schmitt band) — `Assets/Scripts/Sim/Waves/CloakerWatch.cs` (`Watched`); state and line-of-sight half — `Assets/Scripts/Game/Enemies/CloakerGatekeeper.cs` (`AnyoneWatching`)
- Groups enter whole at one spawn point, weighted rotation, ringed slots — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`EmitGroup`, `GroupSlot`); `Assets/Scripts/Game/Enemies/WaveSpawner.cs` (`PickSpawnPoint`, once per group); `Assets/Scripts/Game/Enemies/SpawnPoint.cs` (`All`, `Weight`)
- One swing clock, pushed onto every kind — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`GatekeeperAttackInterval`; `StatsFor` writes it first, before any kind branch); `Assets/Scripts/Sim/Waves/GatekeeperStats.cs` (`attackInterval`)
- Refusals pushed from the sim at spawn, hardened per kind — `Assets/Scripts/Sim/Waves/GatekeeperStats.cs` (`ignoresHatch` / `ignoresPlayer`); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Configure`)
- Kind identity — `Assets/Scripts/Sim/Waves/GatekeeperKind.cs` (`GatekeeperKind`)

## Gaps

- The decided roster is twelve kinds, recorded in `Docs/Brainstorms/gatekeepers-and-waves.md`. Four are in the build. The other eight carry Coming Soon marker pages that name only what each will take; none gets a full Game entry until it ships.
- Nothing on screen ever breaks the remaining count down by kind. The Assault panel gives you one total, so "eleven left" could be eleven Defaults or eight Defaults and three Thieves, and you cannot check.
- Nothing tells you a Wave's composition *before* you start it. Prep tells you how many bodies are coming, not how many of them are Hunters or Thieves.
- The shout is invisible and silent. Nothing indicates that shooting one Gatekeeper just flipped four others onto you.
- Being seen at range provokes a Default permanently, and there is no cue for it at all. A Gatekeeper that noticed you across a room looks exactly like one that did not until it arrives — same frame, same walk; only the path changes.
- That Gatekeepers near the Hatch can never be pulled off it is never stated. It reads as the AI ignoring you.
- Entry points rotate per group, and the game never shows you where they are or which one is next.
- Your health does not regenerate at any point in a Boarding, and nothing on the Bastion restores it. Damage taken in the first Wave is still missing in the last one.
- There are no weak points and no headshots. Where you hit a Gatekeeper does not matter.
