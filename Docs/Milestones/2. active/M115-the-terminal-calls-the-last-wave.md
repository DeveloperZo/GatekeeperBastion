# M115 — The terminal calls the last wave

<!--
Status: active
Update trigger: when the zone grammar (Hatch opening / terminal last wave / armed shutoff) changes, or when the play stamp lands
Last updated: 2026-09-02
-->

A zone's last wave belongs to its terminal. `T` at the Hatch draws the zone's opening
waves; `T` at the terminal draws the last one into whatever is still standing; when that
wave is down the terminal arms, and the next `T` shuts the zone off. The finale's grammar
(kill arms, throw ends) applied to every zone.

## Story

As a Boarder, I want reaching the terminal to start the zone's hardest fight rather than
end the zone, so that the walk up the lane and the stand at the switch are two different
pressures and a sprint past the first wave is not a way through.

## Why it matters

The 2026-09-02 logs (`playthrough.md`, claims 1 and 2): runs B, C and D shut T2 at +16 s,
+32 s and +9.3 s with 3 to 6 of wave 1's 11 dead and wave 2 never drawn, because
`FieldShutoff.WavesHaveRun` accepts the zone's first drawn wave. The shut-off zone's
delivery then outlived the shutoff and advanced the next zone's chain (wave 4 launched 5.9 s
after wave 3), which is half of the "wave 5 wall". Zo, same day: "the play felt static … the
static door path presented no dynamic fight situation."

Leverage: **#5, a rule** — what a terminal press does and when a zone may close. Higher
tiers are wrong because the loop's verbs (start at the Hatch, end at a terminal) are
Zo's 2026-08-25 lock and stay; what moves is which wave each verb owns. Lower tiers are
wrong because no constant makes a first-wave shutoff not a skip.

## Acceptance criteria

1. `T` at the Hatch on a multi-wave zone draws waves `FirstWave(zone)..EndWave(zone)-1` as
   one composite (`StartZoneComposite`). A one-wave zone is unchanged.
2. `T` at a terminal of that zone, once the opening span has been drawn and before the last
   wave has, draws `EndWave(zone)` into the live field: `RunSim.CallZoneFinalWave` — legal in
   Assault or Prep, clears the trickle, chains nothing, logs
   `zone final wave called at the terminal — wave N`.
3. The shutoff is refused until the last wave has been drawn **and** is down
   (`FieldShutoff.MayRun` with `zoneShutoffReady`); `FieldTrickle.Resolve` answers
   `ArmShutoff`, not `StartTrickle`, once the last wave's field is empty, and the sim's
   `FieldShutoffArmed` rises. Every Assault exit clears it.
4. The trickle still holds the lane between the opening span dying and the terminal press;
   the sim never drops to Prep mid-zone.
5. The terminal prompt reads `[T] call the last wave` before the call, `— the last wave is
   still standing` while it lives, and `[T] shut it down` once armed.
6. Retrigger composites and partial starts draw to `EndWave-1`; a partial start electing
   only the last wave draws it at the Hatch and the armed shutoff follows as in 3.
7. Master Control is untouched.

## Kill criterion

A rule, not a bet: it restores the documented loop (`0-Gameplay.md`, "before the zone's
waves have been drawn"). No kill criterion.

## Pre-mortem

- The player calls the last wave with the opening span's bodies still alive and the pile is
  worse than the old stack. Intended — that is the decision the press now carries. The tell
  that it is wrong: every log calls it only from an empty lane.
- Zone 2's last wave (39 bodies, four Thieves) arrives while the player stands 156 m from
  the door. The door stake finally bites off the starting kit here. Watch M112's table.
- A stale delivery from the opening span still advances a later chain. The composite ends
  one wave earlier now and the last wave never chains, so there is no chain left to
  advance; the spawner-side fix for the cross-wave completion is a separate defect if a log
  still shows it.

## Implementation

**Change** `Assets/Scripts/Sim/Waves/FieldTrickle.cs` — `FieldWaveResolution.ArmShutoff`;
`Resolve` takes `finalWaveDrawn`.
**Change** `Assets/Scripts/Sim/Bastion/FieldShutoff.cs` — `MayRun` takes `zoneShutoffReady`
and requires the last wave on multi-wave zones; `MayCallFinalWave`.
**Change** `Assets/Scripts/Sim/Run/RunSim.Composite.cs` — `ZoneFinalWaveDrawn`,
`FieldShutoffArmed`, `ZoneShutoffReady`, `CanCallZoneFinalWave`, `CallZoneFinalWave`,
`ArmFieldShutoff`, `ResetFieldHold`; the four Assault exits reset the arm.
**Change** `Assets/Scripts/Game/Enemies/WaveSpawner.cs` — applies `ArmShutoff`.
**Change** `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` /
`GameController.Retrigger.cs` — composite spans end one wave early; `CanCallFinalWave`,
`CallFinalWave`; `T` routes to the call before the shutoff; `FieldWaveAwaitsShutoff` means
"the zone still owes its terminal", not "the shutoff is legal now".
**Change** `Assets/Scripts/Game/Bastion/BastionTerminal.cs` — the three prompt states.
**Create** `Assets/Tests/EditMode/Run/ZoneFinalWaveTests.cs`.
**Change** `FieldShutoffTests`, `FieldWaveResolutionTests` — new signatures and rule.
**Change** `Docs/Design/0-Gameplay.md`, `Docs/Play Mechanics/A-Boarding.md`,
`Docs/Play Mechanics/Retriggering.md`, `Docs/Decisions.md`.

## Verification

1. Criteria 1–4, 6: `ZoneFinalWaveTests`, `FieldShutoffTests`, `FieldWaveResolutionTests`
   under `Tools/verify/run.sh` (or its compile-and-test stage where mono is absent).
2. Criterion 5: eyes-on in one Boarding; the three prompt strings in `BastionTerminal.cs`.
3. Playthrough path — EMBARK, `T` at the Hatch, walk to T2 while wave 1 lives, `T`: wave 2
   lands on the field; kill it; `T` again closes the zone. Log lines: `zone final wave
   called at the terminal — wave 2`, `field shutoff armed`, `T2 shut down`. Not a blocking
   gate until played.

## Do not build

- A second start line. The Hatch still starts every zone; the terminal only finishes one.
- A timer on the armed terminal, or a wave that ends by attrition.

## Depends on / Unblocks

Depends on: nothing new. Unblocks: M112's door table (zone 2's last wave now arrives with
the player at T5, which is the stake's designed case).
