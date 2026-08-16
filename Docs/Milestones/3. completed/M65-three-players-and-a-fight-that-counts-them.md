# M65 — Three players, and a fight that counts them

<!--
Status: completed 2026-08-15
Update trigger: closed
Last updated: 2026-08-15
Closed on: 2026-08-15. Gate: EditMode `PlayerWalletTests` 7/0,
`WavePlayerCountTests` 3/0; PlayMode `NetThreePlayerTests` Passed
(`gkb_verify_report.json` 2026-08-15T20:34:58Z). Cumulative BankCap across drip
ticks; `RunSim.Wallets.cs` partial for the Sim line cap; guest spawn offset after
Slot sync. Full suite still carries unrelated reds (BastionDoorAperture, SessionMusicHost,
NetSessionAddress, NetVerb repair epsilon) — not this slice.
-->

**Three players: a host and two guests.** Stated once, tested with three, and built so that
**nothing in the code assumes fewer than six**, because six is the intent and the difference
between three and six should be a number and a tuning pass rather than a rewrite.

Also the two things a third player breaks that nothing currently owns: **waves do not know
how many people are shooting**, and **all the salvage goes to one wallet**. Salvage is split
equally, every player banks to their own meta, and what they may bank is capped by their own
progress.

## Story

As a friend, I want to be the second or third person in a run and have the fight account for
me, so that bringing more people makes the game bigger rather than easier.

## Why it matters

**Nothing states a player count anywhere.** Every co-op milestone from M48 to M57 is written
around "the client," singular. No milestone names a maximum and no test spawns a third. The
first time three people connect is currently a discovery, made live, with everyone already
on a call.

**Waves are tuned for one player and cannot see a second.**
`WaveDirector.NextWave(int waveIndex, int siphonLevel)` takes no player count, and a grep for
`playerCount`, `numPlayers` or `PerPlayer` across `Sim/` and `Game/` returns nothing. Wave
size is `BaseEnemyCount + CountPerWave * waveIndex` scaled only by the siphon dial. Three
players is roughly three times the damage against a fight sized for one, and M16's curve was
fitted solo. The first three-player session clears everything and reads as broken.

**And one wallet was never the goal** (Zo, 2026-08-14). M51 recorded the host's wallet as the
only one, with armory, fabrication and the Ship host-local. Across four runs in an evening the
guest plays the same character every time while the host gets stronger, which is survivable
for one session and is the reason there is not a second one. It also breaks the host: three
players earning into one wallet is three times the income against an upgrade curve fitted for
one, so having friends over is the fastest way to wreck your own solo curve.

Leverage tier (`../../Working-Rules.md`): **#9, rules.** The wave-size rule gains an input it
never had.

## Kill criterion

The first friends session with three players. **If a Wave is cleared with no arrivals at the
Hatch on more than half the Waves, the scaling rule is too weak and gets recut.** If three
players find it harder than one player does, it is too strong, which is worse, because it
punishes bringing friends. Both readable off `Tools/wave-report/run.sh`.

## Acceptance criteria

### The number

1. **Three is the supported count and it is written down.** `../../Decisions.md` records
   three players, host plus two, as supported for the friends alpha, records **six as the
   intent**, and names what six would cost: a tuning pass, spawn placement, and HUD layout.
   Not architecture.
2. **No code assumes fewer than six.** No hardcoded 2, no two-element player arrays, no
   single-client field standing in for a registry, no UI that can only draw two. Checked by
   grep and by criterion 3 running with three. **This criterion is what keeps six cheap**,
   and it costs nothing today.
3. **A three-instance test exists and passes**: one host and two clients in the
   `NetcodeIntegrationTest` fixture, running a Wave, all three killing, all three reading the
   same Wave counter and the same ending. Multiplayer Play Mode covers the eyes-on version.

### The fight counts them

4. **`WaveDirector.NextWave` takes the player count** and wave size scales with it. The rule
   is stated in the file rather than discovered, generated into `../../Design/4-Data.md` in
   the same change. **Start crude and linear**, and treat the number as tunable after the
   first session rather than as something to model now.
5. **A solo run is byte-identical to before.** One player, same wave sizes, same numbers,
   same session-log shape. If solo moves, the scaling rule was written as a general change
   instead of a player-count input.
6. **There is no single wallet.** Salvage is split **equally among connected players** at
   the moment it is awarded, host included. `AwardKill` credits every player's unbanked pile,
   not one. **One wallet was never the goal** (Zo, 2026-08-14). Equal is chosen over
   kill-weighted and damage-weighted deliberately: this game has jobs that are not shooting,
   and a contribution split makes chasing a Thief, repairing, clearing a Source or placing a
   turret cost the player money at exactly the moment doing it is correct. Damage weighting
   also pays the best-geared player the most, which is the host.

### What each player takes home

7. **The in-run stake is fully real for every player.** Every player carries unbanked
   salvage, loses it on a down, and makes the extract decision. **This is the criterion not
   to trade away for simplicity.** A guest who keeps nothing has no reason to care about
   `M53`, the Thief or the Reclaimer, and is playing a shooting gallery beside someone
   playing the game.
8. **A player banks at most what their own progress justifies.** At conversion the cap is set
   by that player's `MetaState.DeepestWaveReached`, already tracked at `RunSim.cs:432`, and
   the excess is lost rather than redistributed. **The cap rises from co-op clears as well as
   solo ones**, so playing with friends progresses you legitimately.
9. **The rule those two encode, written down so a tuning pass cannot lose it: co-op gets you
   further, not richer.** Equal split plus criterion 4's player-scaled waves should leave
   salvage per player per hour roughly flat as players are added, while deepest Wave reached
   goes up. Those are the two numbers to read from the log.
10. **A player brings their own loadout.** No inheriting the host's unlocks. Progression is
    the player's own, which is the point of banking to their own meta.

### Nothing regresses

11. **Spawn placement, the fight HUD and targeting handle three** without two players sharing
   a spawn point or the HUD overlapping itself.
12. **Suites green, same names plus the three-player test.** No new counter, no accumulator,
    no stats object (`../../Decisions.md`). No new menu row without asking
    (`../../Architecture/README.md`).

## Implementation

**Change** `WaveDirector.NextWave` to take player count, and the caller to pass it from the
registry rather than from a constant.

**Change** `AwardKill` to credit every connected player's unbanked pile rather than one, and
conversion to apply the per-player cap.

**Create** the three-instance fixture test.

**Change** `../../Decisions.md` and `../../Brainstorms/co-op.md`.

Rejected: **six players now.** Criterion 2 keeps the door open at zero cost; testing and
tuning six is a real spend and there are not six friends waiting.

Rejected: **modelling the scaling curve before a session.** Crude and linear, then read the
log.

Rejected: **kill-weighted or damage-weighted splitting.** See criterion 6. Revisit only if a
session shows equal split producing genuine passengers, and reward signature acts (banking a
delivery, killing a Thief before it escapes) rather than throughput if it does.

Rejected: **a separate guest currency.** A second economy for a solo developer, and it makes
co-op read as a side mode.

## Verification

1. Criterion 2 — the greps, plus criterion 3 passing with three rather than two.
2. Criterion 3 — the fixture, then one eyes-on Multiplayer Play Mode session with three.
3. Criteria 4 to 6 — one solo run diffed against the previous session log, then one
   three-player run through `Tools/wave-report/run.sh`. **Read the arrivals number**, since
   that is what the kill criterion is set on.
4. Criteria 6 to 10 — a three-player run where one player only repairs and escorts and never
   fires: confirm they bank the same as the shooters. Then a player whose deepest Wave is 2
   banks from a Wave 5 clear and is capped.

## Watch

- **Too strong is worse than too weak.** If three players is harder than one, the game
  punishes bringing friends, and that is the opposite of the point. Err light on the first
  pass.
- **The economy is the second-order effect and it will be missed.** Wave scaling is visible
  in the fight; three times the salvage income against a solo-fitted upgrade curve is not
  visible until someone is fully upgraded by Wave 3.
- **A new friend joins with a starting loadout into a deep Wave and may have a bad hour.**
  Criterion 10 says progression is their own, which is right, and the first session is what
  says whether a guest needs a floor. That is a number, not a re-cut.
- **Being carried raises the cap permanently.** One evening dragged to Wave 5 and that
  player's ceiling is Wave 5 money forever. With three close friends nobody is grinding that,
  so nothing is built for it. If it ever matters, count only Waves the player was alive at
  the end of.
- **Criterion 2 is the one that quietly fails.** Nobody writes `playerCount = 2`; they write
  a HUD that draws one guest panel, or a spawn array with two entries, and it looks fine
  until the third person joins.

## Depends on / Unblocks

**Depends on nothing.** The fixture already supports more than one client.

**Unblocks a friends session with more than one guest**, and makes six a tuning decision
later rather than a refactor.

**Graduates** `../../Brainstorms/co-op.md`'s *Guest progression* entry, which asked what a
connected client takes home. Criteria 6 to 10 are the answer.
