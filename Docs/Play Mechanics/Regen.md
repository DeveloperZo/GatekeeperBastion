<!--
Purpose: Current representation of passive health regeneration — the base climb, the decaying kill boost, the post-damage hold and the death freeze — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# Regen

Regen is the Boarder's health climbing back on its own, with no key and no item. It runs every frame of a Boarding: a base climb from any depth all the way to full, plus a boost that every Gatekeeper killed adds to and that decays away over seconds, so healing by fighting is several times faster than healing by hiding. Taking a hit stops the climb for a second, and the boost is capped low enough that no rate of killing outheals even one Gatekeeper standing on the Boarder. Death freezes it, a bought respawn resets it, and the Gatekeepers that drip in after a Wave's roster is spent pay neither Salvage nor health.

## The interaction

The Boarder invokes it, and never with a key. Passive health regeneration runs on the Boarder's own hit points every frame of a Boarding, and the only two things that move it are things the player does in the fight: put a body down, or take a hit. The rule is a struct in Sim (`PlayerRegen`) that reads no clock — `PlayerHealth` injects `dt`, hands it the current HP, and owns whatever comes back.

**The floor.** With nothing killed recently the Boarder climbs at a base rate, from any depth, all the way to full. It clamps at maximum and never overshoots.

**The kill boost.** Each Gatekeeper that dies adds a fixed amount to a standing boost measured in HP per second, and the frame rate is base plus boost. The boost decays continuously on wall clock at a fixed half-life, so it is a *recent-kills* window rather than a Boarding kill count — a flat total would make the rate a function of how deep into the Boarding the Boarder is (Wave 1 unhealable, Wave 6 self-sustaining) and would keep paying a Boarder who has stopped fighting.

The boost is capped, and the cap is the load-bearing number: the ceiling rate is **56% of one Default's damage per second**, so even at maximum boost, standing in melee with a single Gatekeeper still loses health.

**The hold.** Every hit that actually lands holds regen for one second. A negated hit does not — the Bulkhead path returns from `TakeDamage` before the hold is armed, so only damage that was really taken pays it. The boost keeps decaying on wall clock through the hold, so a boost cannot be banked by tanking. Because every kind swings on one 0.7 s clock ([Gatekeepers](Gatekeepers.md)), a body actually on the Boarder holds regen at zero for as long as it stays there.

**A body that pays nothing mends nothing — for the trickle.** Once a field Wave's roster is spent the lane drips Gatekeepers that award no Salvage ([Waves](Waves.md) owns the trickle). Since 2026-08-26 the kill seam returns before the boost while the trickle is live, because feeding it turned the walk up the lane into a healing station. That return covers the trickle only: a kill inside a Convert window also awards nothing (`CurrentKillAward` reads zero there too) and still feeds the boost.

**Cold state and the freeze.** Boarding start, a respawn and the dev heal all reset boost and hold to zero. Death **freezes** the rule rather than decaying through it — `Tick` returns HP unchanged while dead — so no arrangement of ticks regenerates a corpse. On a client-only peer the tick is skipped outright: the server owns that body's HP and pushes it.

Driven by no binding at all; the inputs are kills landed and hits taken. Shown by the HP bar on the vitals band climbing between hits. Reads back as that number and nothing else: the rule exposes a current rate and a current boost, no surface reads either, there is no log line, and [Field Manual](Field-Manual.md) does not print the rates.

## Consequences

The rule exists because of a recorded session (`Logs/GatekeepersProtocol_2026-08-25_22-35-26_p17676.log`, cited in the rule itself): the player cleared Wave 1 of a two-wave composite at 22/100 HP, and a composite has no Prep gap between its Waves, so Wave 2 drew immediately and they were dead 4.1 s in.

That death was **structural, not a difficulty result** — at 22 HP there was no route back to fighting strength at any skill level, because nothing in the game restored health inside a Boarding. This mechanic is that route, and its shape decides where the route runs.

The decision it creates is **heal by fighting, not by hiding**. Both work — Zo asked for a climb to full, and the base rate delivers one — but the fighting route is several times faster, so retreating to a quiet corner is a real option that is priced, in Hatch damage and in Wave time, against turning round and killing the thing that hurt them. The cap is what stops that from inverting: killing hard enough can restore a Boarder between engagements, never sustain one inside a fight. The hold enforces the same boundary from the other side — while something is on the Boarder, the number does not move.

Two knock-ons the player must account for. Damage is no longer a one-way ratchet across a Boarding, so a bad Wave 2 is a delay rather than a sentence, and the affordability calculus of [Going Down](Going-Down.md) shifts: a Boarder at low HP with room to disengage now has a cheaper option than a bought respawn. And the trickle stays a clock rather than an income in both currencies, so stalling in the lane earns neither Salvage nor health.

Removal test: take it out and every point of damage is permanent for the Boarding, the only refills being Boarding start and a paid respawn — the entry-at-low-HP death returns, disengaging stops being a recovery and becomes only a delay, and the kill loses half of what it currently pays. Decisions change; it is a mechanic. The loop itself does not change shape, so it is not core.

## Classification

Primary, loop. Serves Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Fantasy at rank 3: a Boarder is a body that mends, and mends faster while fighting, rather than a bar that only ever falls.

## Interacts with

- [Gatekeepers](Gatekeepers.md) — the garrison supplies both inputs: a landed hit arms the hold, a death raises the boost, and the shared 0.7 s swing clock is why a body on the Boarder pins regen at zero; that page owns targeting and the shared swing clock.
- [Waves](Waves.md) — a trickle body pays no Salvage and, since 2026-08-26, no boost either; that page owns delivery and the field trickle.
- [A Boarding](A-Boarding.md) — the boost rides the same single kill seam, so "a body died" has one place in the Game layer, and the composite (a zone's whole span with no Prep between Waves) is the case the rule was written against; that page owns kills-pay-on-landing.
- [Going Down](Going-Down.md) — a bought respawn heals to full and clears the boost, and death freezes the rule rather than decaying it; that page owns the respawn spend.
- [The Crew](The-Crew.md) — the boost is applied to that machine's own seat (`PlayerRegistry.Local`) rather than to whichever seat scored the kill, and a client-only peer does not regen locally at all because the server owns its HP; that page owns the session and its seats.
- [The Hatch](The-Hatch.md) — the door has no equivalent, its HP returning only through a paid repair: the Boarder mends for free and the Hatch never does; that page owns the withdraw and repair spends.
- [Moving](Moving.md) — the same one-second post-event hold idiom governs the sprint pool, and buying the quiet that lets regen run is usually spent out of stamina; that page owns the pool and the walk.
- [Protocols](Protocols.md) — no protocol moves health, and the Bulkhead negation is the one protocol effect that touches this rule, doing so by *not* arming the hold; that page owns the sheets and their windows.

## Where

- The whole rule — base rate, per-kill add, boost cap, decay half-life, damage hold, and the frame advance — `Assets/Scripts/Sim/Player/PlayerRegen.cs` (`BaseRegenPerSecond`, `RegenPerKill`, `MaxKillBoostPerSecond`, `KillBoostHalfLifeSeconds`, `DamageDelaySeconds`, `Tick`)
- Kill raises the boost, capped; a landed hit arms the hold and never touches the boost — `Assets/Scripts/Sim/Player/PlayerRegen.cs` (`NotifyKill`, `NotifyDamaged`)
- Death freezes rather than decays; clamp at max, no overshoot — `Assets/Scripts/Sim/Player/PlayerRegen.cs` (`Tick`'s `isDead` guard and `hp >= maxHp` clamp)
- The cap's ratio to one Default's damage per second — `Assets/Scripts/Sim/Player/PlayerRegen.cs` (class remarks, CAP paragraph); the dps it is measured against — `Assets/Scripts/Sim/Waves/WaveDirector.cs` (`GatekeeperPlayerDamage`, `GatekeeperAttackInterval`)
- The component that owns the HP and injects `dt`; skipped on a client-only peer — `Assets/Scripts/Game/Player/PlayerHealth.cs` (`Tick`, the `_regen` field)
- Only a hit that actually landed pays the hold — `Assets/Scripts/Game/Player/PlayerHealth.cs` (`TakeDamage`, `_regen.NotifyDamaged()` placed after the bulkhead early return)
- Cold state at Boarding start, respawn and dev heal — `Assets/Scripts/Game/Player/PlayerHealth.cs` (`ConfigureMaxHp`, `ResetToSpawn`, `HealToFull`, each calling `_regen.Reset()`)
- Death locks the body and ends the Boarding, so the freeze is permanent for the seat — `Assets/Scripts/Game/Player/PlayerHealth.cs` (`IsDead`, `Die`)
- The one kill seam in the Game layer, routed to the local seat — `Assets/Scripts/Game/Bastion/GameController.cs` (`GatekeeperKilled` → `PlayerRegistry.Local.NotifyKill()`); the receiving end — `Assets/Scripts/Game/Player/PlayerHealth.cs` (`NotifyKill`)
- A trickle body feeds no boost — `Assets/Scripts/Game/Bastion/GameController.cs` (`GatekeeperKilled`'s `if (Sim.FieldTrickleLive) return;`, 2026-08-26 note); the trickle flag and the zero award — `Assets/Scripts/Sim/Run/RunSim.cs` (`FieldTrickleLive`, `CurrentKillAward`, which also reads zero on `WaveMode.HatchBank`)
- Rate and standing boost are exposed and nothing reads them — `Assets/Scripts/Game/Player/PlayerHealth.cs` (`RegenRatePerSecond`, `RegenKillBoostPerSecond`); the only surface the climb reaches is the HP bar — `Assets/Scripts/Game/Bastion/HudController.Fight.cs` (`SetPlayerBar` call), `Assets/Scripts/Game/Ui/BastionHudView.cs` (`SetPlayerBar`)
- No composition with the MK-1 surge: that ability regenerates Secondary clip rounds, not HP — `Assets/Scripts/Sim/Player/PlayerRegen.cs` (class remarks, COMPOSITION paragraph)
