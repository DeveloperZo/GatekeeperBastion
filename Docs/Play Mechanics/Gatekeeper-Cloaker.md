<!--
Purpose: Cloaker, an instance of Gatekeepers — differences only.
Status: working
in_build: true
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31
Where:
- Gaze-gated refusals; unobserved = Default — `Assets/Scripts/Game/Enemies/CloakerGatekeeper.cs` (`MayTargetHatch` / `MayTargetPlayer` gate on `_observed`)
- Cone, range and Schmitt release band (pure half) — `Assets/Scripts/Sim/Waves/CloakerWatch.cs` (`Watched`; `ReleaseAngleSlackDegrees`, `ReleaseRangeSlackMetres`)
- Line of sight, all live seats, global freeze — `Assets/Scripts/Game/Enemies/CloakerGatekeeper.cs` (`AnyoneWatching` over `PlayerRegistry.All`; `ClearLineFrom`)
- Fast poll, same-frame retarget on flip — `Assets/Scripts/Game/Enemies/CloakerGatekeeper.cs` (`KindTick`, `WatchRefresh`, the `Retarget` call)
- Grudge not gated on the gaze; the shout still fires — `Assets/Scripts/Game/Enemies/CloakerGatekeeper.cs` (`CanBeProvoked => !ignoresPlayer`); `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`OnDamaged` → `Provoke(shout: true)`)
- Reveal on hit; corpse stays visible — `Assets/Scripts/Game/Enemies/CloakerGatekeeper.cs` (`OnRevealHit`, `RevealOnHitSeconds`; `PushCloak`'s dead-body clause)
- Watched refuses decoys — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (decoy branch gated on `MayTargetPlayer`)
- The gaze releases the commit latch — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs` (`Retarget`: `if (!hatchAvailable) _committedToHatch = false;`, and `hatchAvailable` reads the gaze-gated `MayTargetHatch`)
- Owns only its HP; the rest is Default's constants — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`StatsFor`, Cloaker case; neither refusal flag set — the state lives in the Game layer)
-->

# Cloaker

Instance of [Gatekeepers](Gatekeepers.md).

The only kind stopped by being looked at — held in any Boarder's sight it refuses every target and stands still, and it is the only kind whose hold on the Hatch a look can interrupt. Holding it costs whatever those eyes were covering, and it comes for whoever shot it the moment the gaze leaves.

## Game

I know it is in the room the way you know a door you closed is standing open.

Look at it and it stops. Solid, caught, patient. It does not flinch and does not fight the gaze — it stands in the open letting me understand that my eyes are the only wall it respects, and that I have exactly two.

It costs me everywhere I am not looking. Hold one still and the door goes unwatched, the corridor behind me goes unwatched, the Thief is halfway home. In a room this size, everywhere I am not looking is most of the room.

My gaze holds it for the whole crew, so two of us can pin two of them. The room has more directions than we have eyes.

While it moves it is a shimmer with lit seams, hardest to find exactly when nobody is looking for it. Shoot into the dark and a hit answers: one flash of something solid where nothing should be. The flash is the only honest thing it does. It takes a little more killing than the red ones, and it hits no harder.

Unwatched, it wants what a Default wants — the Hatch when the Hatch is in the fight, me when I get close or make it personal. Watched, it wants nothing at all. That is the part I mind most.

## Details

### Structure

The watch is a state, not an answer given at spawn. Watched, both targeting questions answer no and it holds. Unwatched, it fills them as a Default does.

### Refusals

Watched. While any Boarder watches it, it refuses every target and stands still.

### Behaviour

Unwatched, it targets as a Default, on Default's own constants — two gears, Hatch when the Hatch is in the fight. Figures: [Field Manual](Field-Manual.md).

A hit still shouts. A watched Cloaker refuses decoys: the pull is gated on its player-targeting answer, so the freeze cannot be bypassed by a lure.

It owns only its health; every other figure is Default's so the watch rule stays the kind's only question.

### Rules

The watch test: inside a planar gaze cone of any live Boarder, within watch range, with clear line of sight. Release uses a Schmitt band so mouse jitter can never strobe the edge, while a deliberate look-away frees it in one poll. Polled faster than the shared retarget tick, and it re-derives its target the frame the state flips.

The freeze is global. Any one watcher — remote seats included — holds it for the whole crew; a second gaze adds nothing to one already held.

Shots land the grudge through the freeze. Provocation is not gated on the gaze: a hit on a held Cloaker latches the provocation and still shouts its neighbours; the Cloaker itself stays frozen while watched and comes for the shooter the moment the gaze leaves it.

Any connecting hit drops the cloak for a moment, gaze or no gaze. The killing blow leaves the corpse visible for good.

This is the one kind whose Hatch commit is not for life. The commit latch clears whenever the Hatch stops being a target for that body, and for a Cloaker the gaze is what does that — a look releases the latch and the freeze holds it in place. Looking away re-commits it on the next tick, so this buys a pause, not a rescue.

## Additional Details

### Gaps

- Nothing tells you a Cloaker is in the Wave until you happen to see the shimmer or take a swing from something you never saw. The group arrival line names it; nothing on screen does.
- The freeze has no confirm beyond the body stopping — no sound, no stagger, nothing that says "you are holding it". At range, a stopped cloaked machine and a walking one read the same until it drops the cloak.
- Whether an ally's gaze is currently holding one is invisible, so two players sometimes both babysit the same Cloaker while a second one walks.
- It has no voice, no footstep and no cloak-drop sound of its own yet, so every tell is visual and every visual tell needs eyes already pointed the right way.
