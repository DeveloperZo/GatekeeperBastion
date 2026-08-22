# M98 — Drop in between waves

<!--
Status: planned
Update trigger: when the between-waves gate moves, when the hatch open rule changes,
when wave composition stops being computed per wave, when M100 changes where a realm
lives
Last updated: 2026-08-22
-->

A friend joins a realm whenever they like, walks the Ship alone if nobody is home, and
follows the party into a live run **between waves, never during one**. Their hatch door
opens at the same moment, so the party's next wave is composed for the larger party.

## Story

As someone logging on after my friends have already started, I want to join their run at
the next break rather than wait for it to end, so that showing up late costs nobody
anything.

## Why it matters

**The payoff of a realm is that nobody has to be online at the same minute.** Zo's
argument, 2026-08-20: coupling a joinable safehouse with joining the action shortly
after is what motivates everyone to jump on even when they cannot ready up together.
`M96` and `M97` build the realm and the reason to stand in it; without this it is a
waiting room.

**Today there is no path in.** Joins are refused during the embark countdown (`M72`) and
a guest cannot dock the Run at all (`DockRun` has no net route). A seat that arrives
after embark has nowhere to go.

**Between waves is the ruling and it is also the cheap seam** (Zo, 2026-08-20: the join
rule follows how quickly the game can scale for the player, "in between waves not during
waves"). `WaveDirector.NextWave` composes each wave from `playerCount` at the moment it
is called. A seat that joins during Prep is simply present the next time that runs, and
nothing has to rescale live bodies. A seat joining mid-Assault would require rescaling a
wave already on the floor, which is the expensive version and is not built.

Leverage tier (`../../Working-Rules.md`): **#4, self-organisation.**

## Acceptance criteria

1. A player enters a realm they are a member of, spawns on the Ship, and can walk it
   and use every station alone. *Whether the owner has to be online for that is `M100`'s
   question, not this one.*
2. A player joins a realm whose party is mid-run and lands on the Ship, not in the
   bastion, with a prompt saying they will join at the next break.
3. Pressing that prompt during the party's **Prep** puts them in the bastion with the
   party.
4. The same prompt during **Assault** is visible but refuses, with a reason, and does not
   queue silently.
5. A seat that joined mid-run is counted by `WaveDirector` on the **next** wave, not the
   current one, and the wave composition log shows the higher count.
6. The joining seat's hatch door opens at the moment they enter the bastion, and the
   open-count log matches the new seat count.
7. No wave already on the floor changes size, health or composition when a seat joins.
8. A seat leaving mid-run is dropped from the count on the next wave and their door
   seals at the same boundary.
9. Solo is unchanged: a single-seat run never sees any of this and its wave composition
   is byte-identical to before.
10. Joins during the embark countdown are still refused, exactly as `M72` has them.

## Implementation

1. **Change** `NetSession` / `NetJoinIntent` so a join while the party is Tethered lands
   the seat on the Ship rather than refusing. This is the whole of criterion 2.
2. **Create** a follow command on `GameController`, host-only (R1), gated on
   `Phase == Tethered && Assault == Prep`. That is the same gate M57's ship-wait already
   uses, so the condition is proven rather than new.
3. **Change** the hatch open rule from `M92` to recompute on seat change **at the Prep
   boundary only**, never mid-Assault. Criterion 7 is this restriction's visible face.
4. **Change** `NetPrep` to surface the prompt and its refusal reason. The refusal is a
   named log line, not a silent no-op.
5. **Do not** touch `WaveDirector.NextWave`'s composition maths. The whole point of the
   between-waves rule is that the existing per-wave computation already does the right
   thing once the count has changed at the right moment.
6. **Change** the leave path symmetrically. A seat dropping mid-Assault leaves its door
   open until the next Prep, because sealing a door under attack would be a free save.

Rejected: queueing a mid-Assault join to fire automatically at the next Prep. It reads as
the game deciding when you enter, and a player standing in the ship with a live button
knows more than a player watching a spinner.

## Verification

- Criteria 3, 4, 5, 7, 8, 10: PlayMode net tests against `NetcodeIntegrationTest`. Join
  during Assault and assert refusal plus unchanged live composition; join during Prep and
  assert entry, then assert the next wave's composition reflects the new count and the
  current one did not.
- Criteria 1, 2, 6: playthrough across two editor instances. Host embarks, guest joins,
  guest lands on the Ship, guest follows at the next Prep, and the open-count log rises.
- Criterion 9: run the offline wave harness at `playerCount == 1` and diff against the
  recorded baseline. Any difference fails.

**Playthrough path**: host enters a realm and embarks. Guest joins the same realm and
spawns on the Ship. Guest reads the follow prompt, presses it during a wave and is
refused, presses it again once the wave clears, and arrives in the bastion as a second
door opens.

## Built, 2026-08-22

The distinction the code did not have is now a type: `RunSeats`, a host-only roster of the
seats **playing** as against the seats **connected**. It opens as the run becomes Tethered
seeded with everyone who embarked, and closes when the run ends or the Ship loads for real.
A seat that connects mid-run is not in it until it follows; a seat that drops is held in it
until the next Prep. Three consumers read it while a run is live — the wave-composition
count, the wallet split behind it, and the hatch doors — and none of them before one, so the
lobby is untouched and solo still answers one seat.

`WaveDirector.NextWave` was not touched, as implementation step 5 required.

**Reused rather than rebuilt**: the downed-Boarder wait already puts a seat on the Ship over
a live Bastion. `AwaitingFollow` distinguishes an arrival from a casualty — that one buys
back at a Hatch-bank window, this one only waits for the break — and `TryAcceptFollow` is
`TryAcceptEmbark` with the Hatch-bank condition dropped, since a seat that never embarked has
nothing to buy back.

**Found while building, fixed here**: a `NetworkVariable`'s initial value raises no
`OnValueChanged` on the owner that has just spawned, and the Bastion scene can still be
loading when one would have fired. The owner enters the wait from its own tick instead,
scoped to the late arrival so the buy-back path keeps being driven purely by the callback.

**Owed.** Nothing has been through Unity: `mono` is not on this machine's PATH, so
`Tools/verify/run.sh` ran its lints (clean) and not its Sim leg, and neither test fixture has
executed. Criteria 1, 2 and 6 are eyes-on across two editor instances and nobody has looked.
Criterion 9's offline wave-harness diff against the recorded baseline has not been run —
`RunSeatsTests` argues solo is unchanged from the count's side, which is weaker than the diff
the milestone asks for.

## Usable, or it did not ship

**Driven**: joining a realm, and pressing follow. **Shown**: the ship you land on, the
prompt, and a door opening as you arrive. **Reads back**: the refusal reason during a
wave, the open-count log, and the next wave's composition line carrying the higher count.

## Supporting work

Depends on `M96` for realms to join and `M92` for hatch doors to open. `M97` is
unrelated and can land either side.

`M100` owns reaching a realm whose owner is not running the game. This milestone was
written on 2026-08-20 as though that already worked; it never has, and folding it in here
would have made a join-timing milestone carry a storage change. The two are independent:
this one is about *when* a seat may enter a live run, `M100` is about *whether the realm
is there to enter*.

## Open questions

- Whether the arriving seat should be placed at the party or at a door is **unknown,
  needs play data**. Stamped: at the party, since nobody owns a door.
- Whether a mid-run join should cost the realm anything is not specced. Stamped: no.

## Do not build

Mid-Assault joining. Automatic queued entry. Rescaling a live wave. Host migration.
Spectating while waiting.
