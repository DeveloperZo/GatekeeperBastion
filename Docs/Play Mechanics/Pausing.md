<!--
Purpose: Current representation of pausing — the Esc overlay that takes the Boarder out of the fight without taking the fight off the clock — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# Pausing

Pausing is the Esc overlay aboard the Bastion. It takes the Boarder's hands off the game and
leaves the game running. Nothing stops: Gatekeepers keep closing, new groups keep arriving,
the Hatch keeps taking damage, a downed seat's bleed-out clock keeps running and a Convert
window keeps counting down. What it does gate is the Boarder, whose movement, look, firing
and interact all stop, along with the key that would buy a downed seat back. So opening it
mid-Assault costs Hatch health and Salvage. The only free moment to open it is Prep, which
is untimed, and nothing in the game says so.

## The interaction

The Boarder invokes it: **Esc**, aboard the Bastion, with nothing else open. The key is dispatched down a fixed priority, and only when none of these is standing does the press open the pause overlay:

1. a live demo line eats it;
2. an open modal closes;
3. the Settings screen closes;
4. the Compendium reader closes;
5. an already-open pause closes;
6. a terminal phase returns to the Ship.

**What opening it does.** The overlay captures the Boarder's current movement lock and current cursor state, then sets `MovementLocked` and frees the cursor: `Cursor.lockState` to none, `Cursor.visible` to true. The pause canvas group becomes interactable and raycast-blocking, the window animates in, the background blurs, and a shortcut panel is filled with the keycaps live in the current context — the Ante level among them in Prep, absent in Assault — plus the session's join code where there is one.

Closing restores what was captured rather than assuming a state, so a pause opened over a death lock closes back into that lock; only a pause that captured a locked cursor and an unlocked Boarder re-locks the cursor and hands movement back. From the overlay, RESUME closes it, the Compendium button opens the reader (and hides the fight chrome while it is up), and QUIT TO MENU saves and leaves.

**What opening it does not do, on purpose.** It never touches `Time.timeScale`, and it does not disable the fight canvas — both are written into the file as a criterion, not left as an accident. The consequence is that the pause is an input state, not a stop: it takes the Boarder's hands off the game and leaves the game running.

**What it actually gates.** Three input readers test it, and only three: the Boarding's prep commands (ante up, select, start, zone shutoff), the crew and personal ability keys, and the DOWN overlay's respawn confirm. Everything else in the Bastion's written per-frame order is called regardless — the sim tick, locomotion, the gun, the abilities' own timers, decoy placement, interact, and spawn delivery — and gatekeeper AI is never in that order at all: it is engine-driven and its one "stop everything" test is a terminal phase, which a pause is not.

Driven by Esc (bindings: [Field Manual](Field-Manual.md)), whose Bastion label is *pause*. Shown by the overlay, its blur, and the shortcut panel it fills. Reads back as the cursor appearing and the window animating in — opening and closing the pause writes no log line and moves no counter; only QUIT TO MENU records anything.

## Consequences

**The Boarder is rooted and disarmed for as long as it is open.** `MovementLocked` takes `WASD` and mouse look together, and the freed cursor independently refuses both remaining verbs: `PlayerGun.CanFire` returns a refusal on an uncaptured cursor, and `Interactor.Tick` returns before it casts, so no prompt is published and no hold advances. [Interacting](Interacting.md) owns that precondition and both its faces. The one thing that keeps working is the stamina pool, which refills under any movement lock ([Moving](Moving.md)).

**Opening it mid-Assault has four separate costs, and each is real on its own.**

*Bodies keep closing.* `EnemyGatekeeper.Update` is engine-driven and never consults the pause; it stops only on the Boarding being over. Spawn delivery is worse than neutral — the spawner's per-frame emission sits in the written order below the pause-gated calls, so new groups keep arriving as well ([Waves](Waves.md) owns delivery).

*The Hatch keeps taking damage.* A gatekeeper in range swings, and the swing routes through the scene proxy into the sim Hatch. On a field-terminal Wave the blow is **scaled down**, not refused, so even a field Wave bleeds the Hatch behind an open pause — slowly rather than not at all. And hatch damage attrits the carried unbanked stake in the same call, so the pause costs Salvage as well as HP ([The Hatch](The-Hatch.md) owns the bank).

*A bleed-out clock runs.* It is ticked above the Convert branch in the sim tick, deliberately, so a downed seat bleeds on the same clock in every mode — and the sim tick is not gated. The twist is what makes this the sharpest of the four: the reader that *acts* on the expiry, and the reader that takes the buy-back press, are the same pause-gated method. So the clock runs while the key that answers it is dead, and the Boarding does not end until the pause closes — at which point it may end immediately. That is not a reprieve; it is the fee accruing behind a door the player shut ([Going Down](Going-Down.md) owns the clock and the respawn spend).

*A Convert window counts down.* Its timer is decremented on the same ungated sim tick, and the window ends on time whether or not anyone is playing. The drip keeps converting while unbanked remains, so a paused Convert does still bank — but it banks into a window the Boarder cannot defend, at a Hatch that is live.

**There is no safe moment to open it during an Assault, and the player is not told.** The overlay's own content is keycaps and a join code; nothing on it names a clock, a body, or a Hatch. The fight values do keep being pushed behind it — the phase-chrome switch is the only thing the pause holds back — so a Hatch bar that is still falling is technically on screen, but the pause never says to look, and opening the Compendium from the pause hides the fight chrome outright while the Wave runs.

The only free moment is Prep, and it is free for a reason that has nothing to do with the pause: Prep is untimed and pays nothing ([A Boarding](A-Boarding.md) owns Prep). The playable consequence is that Esc is a mid-Assault decision with a price the build never quotes.

Removal test: take the overlay out and the Compendium, the options and QUIT TO MENU become unreachable without abandoning the Boarding, and the decision "can I afford to look away right now" stops existing along with the HP and Salvage it currently costs. That is a decision the player must account for and an outcome the Boarding records, so it earns an entry. It is not a different game without it — the Bastion is the same Bastion.

## Classification

Secondary, loop. Serves Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle.

Secondary by the tier test: it is available at any moment but solves nothing the Bastion asks, and it earns its place only in combination — with the reader, with the options, with a decision to leave. The Fantasy claim (rank 3) rests on the one thing the code is explicit about rather than on the overlay's art: refusing `Time.timeScale` is what makes the Bastion a place that does not wait for the Boarder, and it is the reason this is a mechanic instead of a menu.

## Interacts with

- [Interacting](Interacting.md) — this is one of the things that frees the cursor, and the loudest case because the Boarder cannot walk away either; that page owns the free-cursor precondition that disarms firing and interact alike.
- [Moving](Moving.md) — `MovementLocked` is the same lock a station panel and death use; that page owns the lock and the fact that stamina still returns under it.
- [A Boarding](A-Boarding.md) — the prep commands are what the pause gates, and Prep being untimed is what makes it the only free moment to open this; that page owns Prep, Assault and the start.
- [The Hatch](The-Hatch.md) — the Hatch keeps taking damage and the Convert window keeps counting down while the pause is open; that page owns the bank, the window and the withdraw.
- [Going Down](Going-Down.md) — the bleed-out clock runs while the pause holds the respawn confirm shut; that page owns the clock and the respawn spend.
- [Waves](Waves.md) — spawn delivery keeps emitting groups behind the overlay; that page owns delivery.
- [Protocols](Protocols.md) — the crew and personal ability keys are gated with the prep commands, but a window already open keeps burning down; that page owns the windows.
- [The Ship](The-Ship.md) — the hub's Esc is a separate dispatcher with the same lock-and-free shape and no fight to run on; that page owns the hull and its stations.

## Where

- Esc dispatch priority — demo line, modal, Settings, Compendium, close pause, terminal return, then open — `Assets/Scripts/Game/Bastion/HudController.Pause.cs` (`TickEscape`)
- Open captures the movement lock and cursor state, then locks movement and frees the cursor — `Assets/Scripts/Game/Bastion/HudController.Pause.cs` (`OpenPause`, `_pauseCapturedLock`, `_pauseCapturedCursor`, `_pauseCapturedVisible`)
- Never touches `Time.timeScale`; the fight canvas stays enabled — `Assets/Scripts/Game/Bastion/HudController.Pause.cs` (class summary, and the `// Criterion 31` comment closing `OpenPause`)
- Close restores what was captured, and only re-locks the cursor for a Tethered Boarder who was unlocked when it opened — `Assets/Scripts/Game/Bastion/HudController.Pause.cs` (`ClosePause`)
- The overlay's buttons: RESUME, the Compendium (which hides the fight chrome), QUIT TO MENU — `Assets/Scripts/Game/Bastion/HudController.Pause.cs` (`ResumePause`, `OpenCompendium` → `_view.HideChrome()`); `Assets/Scripts/Game/Bastion/GameController.cs` (`QuitToMenu`)
- The overlay's content is keycap rows plus a session join code, and nothing about the fight — `Assets/Scripts/Game/Bastion/HudController.Panels.cs` (`FillPauseShortcuts`, `PushShortcutPanel`)
- The three readers that the pause gates — `Assets/Scripts/Game/Bastion/GameController.cs` (`TickFightInput`'s `OptionsScreen.IsOpen || HudController.IsPauseOpen` return); `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`Tick`, the same test above the crew/personal reads); `Assets/Scripts/Game/Bastion/GameController.Down.cs` (`TickLocalDown`, the same test above the bleed-out and confirm reads)
- What is not gated: the sim tick at step 2, locomotion/gun/ability/protocol/placement/interact at steps 4–6, and spawn delivery at step 8 — `Assets/Scripts/Game/Bastion/GameController.cs` (`Update`, and the written order in its remarks)
- Gatekeeper AI is engine-driven and stops only on a terminal phase — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Update`, `RunIsOver`)
- The swing lands on the Hatch through the scene proxy; field-Wave damage is scaled rather than refused, and the same call attrits the carried stake — `Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs` (`Attack` → `hatch.ApplyDamage`); `Assets/Scripts/Game/Bastion/HatchStation.cs` (`ApplyDamage`); `Assets/Scripts/Sim/Run/RunSim.cs` (`DamageHatch`, `AttritUnbanked`); `Assets/Scripts/Sim/Run/HatchBank.cs` (`FieldWaveDamageScale`)
- The bleed-out clock is ticked above the Convert branch, in every mode — `Assets/Scripts/Sim/Run/RunSim.cs` (`Tick` → `TickBleedOut`); `Assets/Scripts/Sim/Run/RunSim.Respawns.cs` (`TickBleedOut`, `BleedOutRemaining`, `BleedOutExpired`); length `Assets/Scripts/Sim/Run/Respawns.cs` (`BleedOutSeconds`)
- The Convert window's timer and its drip advance on the same ungated tick — `Assets/Scripts/Sim/Run/RunSim.cs` (`Tick`, the `Mode == WaveMode.HatchBank` branch — `_hatchBankTimer`, `DripTick`)
- A free cursor refuses firing and returns interact before the cast — `Assets/Scripts/Game/Player/PlayerGun.cs` (`CanFire`, `Refuse`, `IsoAimActive`); `Assets/Scripts/Game/Player/Interactor.cs` (`Tick` cursor guard)
- `MovementLocked` takes movement and look together — `Assets/Scripts/Game/Player/FirstPersonController.cs` (`Tick`, `canLook` / `canMove`)
- Fight values keep pushing under the pause; only the phase-chrome switch is held — `Assets/Scripts/Game/Bastion/HudController.cs` (`LateUpdate` → `PushValues` → `PushFightValues`, ungated; `ApplyPhase`'s `OptionsScreen.IsOpen || IsPauseOpen` early return)
- Binding: Esc, labelled *pause* on the Bastion strip and *settings* on the Ship — `Assets/Scripts/Game/Input/Controls.cs` (`ControlId.FreeCursor`, `KeyCode.Escape`, the `FightAlways` and `ControlContext.Ship` rows)
- The Ship's own Esc dispatcher: same lock-and-free shape, no fight to run on — `Assets/Scripts/Game/Ship/ShipController.cs` (`HandleEscape`, `OpenSettings` → `SetWalkMode(false)`, `ApplyWalkMode`)
