<!--
Purpose: Default, an instance of Gatekeepers — differences only.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-30
-->

# Default Gatekeeper

Instance of [Gatekeepers](Gatekeepers.md).

The kind with nothing switched off — it can be provoked into chasing the Boarder, and it will attack the Hatch when it can reach it. Its chase is slower than a walking Boarder, and every extra body a raised Ante would buy is this one.

## Game

The plain frame. Everything else on this hull, I measure against it.

On a field Wave it walks to the Hatch and will pass me by to get there, unless I give it a reason not to. On a Convert it does the same thing at full swing. It closes and it swings. That is the whole manual.

Three things put one on me, and I keep the list in order. Shooting it: permanent. Being seen down a clear line at distance: permanent. Standing in its space: wears off when I leave. The permanent two also put it in a hurry — still slower than my walk, slower than my sprint — which makes backing away while firing a real move, and a thin one.

Hurt one and it shouts, and everything near it drops what it was doing and joins. The ones it pulls do not shout again. One shot buys one group. Whether that was a purchase or an accident depends on whether I meant it.

At the Hatch it stops negotiating. It hits the door while I shoot it, while I stand beside it, while its friends chase me. A Default is a decision while it walks and a timer once it arrives.

Weakest thing here, one at a time. It has never needed to be strong. It brings count, and every extra body a raised Ante would add is this one.

## Differs

- **Refuses nothing.** Neither refusal flag is set; the Default is the one kind on which the mechanic page's whole rule runs unmodified, Hatch included. It is the base class itself, not a subclass.
- **Two gears.** Provoked movement switches from patrol to chase speed — the two-speed pattern the Cloaker shares by running on Default's own constants, and the one Hunter and Thief flatten into a single speed. Its chase speed sits deliberately *under* the Boarder's walk (2026-08-25): giving ground does outpace it, and what makes backing away a losing plan is the corridor and the far closet still feeding, not the footspeed. Figures: [Field Manual](Field-Manual.md).
- **Every body the Ante buys is a Default.** The dial scales body count only, and the surplus lands entirely on this kind — the fixed Hunter, Thief and Cloaker counts never see it.
- **The field-Wave trickle is Defaults only.** The drip that holds a field Wave open until its terminal spawns this kind alone, deliberately — a late-arriving copy of the same Gatekeeper, not a new problem.
- **It is the fallback identity.** Unknown kinds get Default stats, and a missing per-kind prefab falls back to the Default's.

## Where

- Refuses nothing: neither flag in its stat line — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, default case)
- The base class is the kind: no subclass, no overrides — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Kind` virtual returns `GatekeeperKind.Default`)
- Two gears: chase speed on provocation, not on proximity — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`RecomputeSpeed` reads `_provoked`, not `_chasingPlayer`); the constants — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`GatekeeperPatrolSpeed`, `GatekeeperChaseSpeed` — deliberately just under the player's walk since 2026-08-25); the walk it sits under — `Assets/Scripts/Game/Player/FirstPersonController.cs` (`moveSpeed`, `sprintSpeed`)
- Ante surplus is all Default — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`NextWave`: `defaultCount` absorbs the dial-scaled remainder; fixed rows never scaled)
- Trickle bodies are Defaults — `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`SpawnTrickleBody`)
- Fallback stats and prefab — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, default case); `Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs` (`PrefabFor`)

## Gaps

- Nothing distinguishes a provoked Default from an unprovoked one except that it eventually walks at you. There is no alert, no marker, and no audio cue for the flip.
- The game never says that provocation is permanent. Backing off from one looks like it should work and never does.
- The game never says that being *seen* provokes one. Players reasonably assume only damage does.
- Nothing tells you that shooting one alerts its neighbours, or how far that reaches.
- A Default that has reached the Hatch is unrecoverable by any means except killing it, and the game presents that as the AI simply ignoring your fire.
- Provoked movement sitting *under* the walk is never stated. Giving ground works; the corridor and the far closet still feeding are what make it a thin plan.
