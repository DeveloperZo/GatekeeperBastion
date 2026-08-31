<!--
Purpose: Thief, an instance of Gatekeepers — differences only.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31
Where:
- Never targets the player, hardened — `Assets/Scripts/Game/Enemies/ThiefGatekeeper.cs` (`MayTargetPlayer => false`, with the 2026-08-27 revert note)
- `playerDamage` 0 and the pushed flag — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, Thief case); `Assets/Scripts/Sim/Waves/GatekeeperStats.cs` (`ignoresPlayer`)
- Never provoked, never shouts, declines the shout — `Assets/Scripts/Game/Enemies/ThiefGatekeeper.cs` (`CanBeProvoked => false`); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Provoke` refuses before the shout loop; joiner loop counts only real joins)
- Flinch is presentation only — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`OnDamaged` fires `HitReact` before `Provoke` is refused)
- Refuses decoys, by kind — `Assets/Scripts/Sim/Run/DecoyState.cs` (`ShouldPull` returns false for `GatekeeperKind.Thief`); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`TransferInterestTo` kind test)
- One gear — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, Thief case: `patrolSpeed` equals `chaseSpeed`)
- Inverted crowd priority, expensive solver — `Assets/Scripts/Game/Enemies/ThiefGatekeeper.cs` (`AvoidancePriority`, `AvoidanceQuality`)
- Salvage rule recorded, not built — `Assets/Scripts/Sim/Waves/GatekeeperKind.cs` (Thief doc note)
-->

# Thief

Instance of [Gatekeepers](Gatekeepers.md).

The only kind that ignores the Boarder completely — it walks for the Hatch and cannot be shot, shouted or lured off it, and a wall of bodies parts for it instead of stalling it. It is also the one safe thing to shoot in a crowd, because it never shouts its group onto anybody.

## Game

Somebody built this thing to walk through gunfire, and then never told it about the gunfire.

Heaviest frame on the hull, slowest thing aboard, and it has never once taken my blood. On every Wave it walks at the door. On a field Wave the bite is thinner. On a Convert it arrives with a bite that costs more than killing it would have.

I put a full clip into one's back at arm's length. It flinches. It does not turn, and it does not call its escort. The rounds land, the walk continues. The ones beside it do not look up.

Its slowness is not mercy. It is the invoice. It is armor with an appointment, and the time it gives me is exactly the time it takes to kill — spent facing away from everything else in the room. Backing away buys seconds, and seconds are not an answer.

It carries the most health of anything here, and it scales with the Wave like everything else: the same frame late in a Boarding is a longer job than the first one I met. The Ante never adds one. The schedule is the schedule.

In a crowd it is the one safe thing to shoot — the only frame that will not shout the group onto me. Fire through the group into the Thief and the group never learns my name.

## Details

### Structure

May target the Hatch. May not target a player. Cannot be provoked. The player question is the one it answers no.

### Refusals

The player, ever, on every Wave mode. Player damage is zero. It does not hunt Boarders on field Waves.

It refuses provocation and the shout: a shot changes nothing about it — not target, not speed — and it never shouts; a neighbour's shout is silently declined. The flinch it plays when hit is presentation only.

It refuses decoys: excluded by kind from the pull and from interest transfer.

### Behaviour

One gear. Patrol speed equals chase speed; nothing makes it change its mind. Slowest thing aboard. Figures: [Field Manual](Field-Manual.md).

Crowd priority inverted. It outranks every Default and Hunter in avoidance and keeps the expensive solver, so a wall of bodies at the Hatch parts for it instead of stalling it.

### Rules

It walks to the Hatch on every Wave mode.

Recorded, not built: the salvage rule — never damages while there is salvage to take. Today it walks the battering errand.

## Additional Details

### Gaps

- That shooting a Thief does nothing to its mind — no provoke, no shout, no slowdown — is never communicated. It reads like the AI is broken.
- Nothing tells you a Thief is present or how many are left. The Assault count is one number for the whole Wave.
- Its slowness reads as harmlessness. Nothing frames it as a budget you have to spend.
- That it pushes through crowds rather than jamming in them is invisible — you cannot block it with bodies.
