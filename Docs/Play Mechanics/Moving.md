<!--
Purpose: Current representation of getting around — the walk, the rationed sprint and its stamina pool, the states that take movement away, and the isometric mode the Ship's rig can put the same controller into — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31
Where:
- Walk and sprint speeds, serialized and restated as code defaults — Assets/Scripts/Game/Player/FirstPersonController.cs (moveSpeed = 5, sprintSpeed = 7.5, Reset)
- Why sprint is rationed, and the five-numbers-together rule — Assets/Scripts/Game/Player/FirstPersonController.cs (class remarks, WHY SPRINT IS RATIONED)
- Sprint requires Shift and movement and stamina; standing still costs nothing — Assets/Scripts/Game/Player/FirstPersonController.cs (TickSprint, bool wants = moving && Controls.Held(ControlId.Sprint))
- Drain, delay re-armed every sprinting frame, refill after the delay — Assets/Scripts/Game/Player/FirstPersonController.cs (SprintDrainPerSecond, StaminaRegenDelaySeconds, StaminaRegenPerSecond, TickSprint)
- The exhaustion latch and its re-engage threshold — Assets/Scripts/Game/Player/FirstPersonController.cs (ReengageFraction, _exhausted, IsSprinting)
- Stamina still returns while movement is locked out — Assets/Scripts/Game/Player/FirstPersonController.cs (Tick's else branch calling TickSprint(false, dt))
- Movement gate: cursor lock, MovementLocked, the voice lock, Rooted — Assets/Scripts/Game/Player/FirstPersonController.cs (Tick, canLook / canMove; the Rooted || voiceLocked early return that skips gravity and Move)
- Self-driven on the Ship and in menus, tick-owner-driven aboard the Bastion — Assets/Scripts/Game/Player/FirstPersonController.cs (_hosted, SetHosted, Update yielding); Assets/Scripts/Game/Bastion/GameController.cs (ApplyMetaEffects → fpc.SetHosted(true))
- Rooted is the protocol plant (and the sealed Ship teleporter) — Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs (Plant, Unplant); Assets/Scripts/Game/Ship/ShipTeleporter.cs (_fpc.Rooted)
- MovementLocked setters: non-Tethered phases, pause, options, the Ship's panel walk mode, death — Assets/Scripts/Game/Bastion/GameController.cs (_fpc.MovementLocked = !fighting); Assets/Scripts/Game/Bastion/HudController.Pause.cs; Assets/Scripts/Game/Flow/OptionsScreen.cs; Assets/Scripts/Game/Ship/ShipController.cs (ApplyWalkMode); Assets/Scripts/Game/Player/PlayerHealth.cs (Die, HealToFull)
- The pool is pushed at Boarding start and is a frozen constant — Assets/Scripts/Game/Bastion/GameController.cs (ApplyMetaEffects → fpc.ConfigureStamina(MetaUpgrades.StaminaMax())); Assets/Scripts/Sim/Meta/MetaUpgrades.cs (StaminaMax(), no level argument, "Frozen at the old level-0 value"); Assets/Scripts/Game/Player/FirstPersonController.cs (ConfigureStamina fills the tank and clears the latch)
- Chase and patrol speeds the walk is authored against, and the deliberate under-the-walk note — Assets/Scripts/Sim/Waves/WaveDirector.cs (GatekeeperChaseSpeed = 4.6 with its Zo 2026-08-25 remark, GatekeeperPatrolSpeed, StatsFor — Hunter chaseSpeed 5.6 "still the only thing that outruns the player", Thief 2.2, Cloaker reading Default's constants)
- Binding — Assets/Scripts/Game/Input/Controls.cs (ControlId.Sprint, LeftShift/RightShift, MOVE group; moveX / moveY axes)
- The stamina bar and its SPRINT label — Assets/Scripts/Game/Bastion/HudController.Fight.cs (SetStaminaLabel, SetStaminaBar, _fpc.IsSprinting); Assets/Scripts/Game/Ui/BastionHudView.cs (SetStaminaBar)
- Footstep cadence differs between walk and sprint — Assets/Scripts/Game/Player/FirstPersonController.cs (TickFootsteps); Assets/Scripts/Sim/Run/WeaponView.cs (WalkFootstepSeconds, SprintFootstepSeconds)
- The rig's presence and enabled state is the isometric-mode flag, and consumers read null-or-disabled as first person — Assets/Scripts/Game/Ship/IsometricCameraRig.cs (class summary, Awake, LateUpdate, ScreenRay)
- Locked rotation, the dead zone, the follow rate and the back-off distance are one constant set shared by rig and docgen — Assets/Scripts/Sim/Ship/ShipCamera.cs (YawDegrees, PitchDegrees, FovDegrees, FollowDistance, DeadZoneHalfX/Z, FollowLerpPerSecond)
- Camera-relative movement built once from the locked yaw; look off, move on, sprint unchanged — Assets/Scripts/Game/Player/FirstPersonController.cs (IsoMoveBasis, _isoRig, Tick's isoDriven — the skipped click-to-relock, canLook / canMove, and world = IsoMoveBasis * input); the deliberate null camera wire Assets/Scripts/Game/Player/FirstPersonController.cs (Awake, cameraTransform tooltip)
- The Ship skips the cursor lock in isometric mode, inverting the walking-means-locked invariant — Assets/Scripts/Game/Ship/ShipController.cs (ApplyWalkMode's lockCursor test, _isoRig, class remarks)
- Every consumer's rig binding is optional and defaults to first person, so whether the rig is authored and enabled in the shipped hub is not readable here — Assets/Scripts/Game/Player/FirstPersonController.cs (_isoRig, [SerializeField, OptionalBinding]); Assets/Scripts/Game/Ship/ShipController.cs (_isoRig, [SerializeField, OptionalBinding])
-->

# Moving

Moving is the walk, the look and the sprint: one flat walking speed, no jump, and a faster gear on Left Shift that spends a stamina pool. The pool is the cost. It drains only while ground is actually being covered, buys about four seconds of sprint, and refills slowly after a pause; bottoming it out refuses sprint until the pool has climbed some way back. The speeds are the whole point. The commonest Gatekeeper's chase is tuned just under the walk, so giving ground buys a moment; the Hunter is tuned just over it; and sprint outruns everything the garrison fields, which is why it is rationed. Some states take movement away — a crew ability roots the Boarder while leaving them looking and firing, and a pause screen, a station panel or death take movement and look together.

## Details

### Structure

The Boarder invokes it: `WASD` drives a flat planar walk, mouse look turns the body and pitches the camera, and Left Shift buys a faster gear out of a stamina pool. There is no jump. The controller is self-driven on the Ship and in menus; aboard the Bastion the tick owner calls it.

The walk is one speed. There is no acceleration curve and no crouch. That speed is the baseline every chase speed in the game is authored against.

When movement is taken away, two distinct states:

- **Rooted** drops `WASD` while look and fire stay live, and while rooted neither gravity nor the body move at all — the Default sheet's crew plant is what sets it in the fight ([Protocols](Protocols.md) owns the plant; a sealed Ship teleporter is the other setter).
- **Movement locked** takes movement and look together: the pause and options screens, any phase that is not Tethered (so both end screens), a station panel open on the Ship ([The Ship](The-Ship.md) owns the panel), and death, which locks the body until a respawn clears it ([Going Down](Going-Down.md)).

The same controller has a second mode, wired for the Ship: an isometric camera rig whose presence and enabled state is the whole mode flag — a null or disabled rig reads as first person, unchanged. While it is live, movement becomes camera-relative (`W` is screen-up), look stops existing, and the cursor stays free. Speeds, the pool, the delay and the latch are untouched. Whether the rig is authored and enabled in the shipped hub is scene wiring and is not readable here.

### Sprint

Sprint while Left Shift is held and there is movement input and there is stamina. Holding Shift while standing still costs nothing: the pool is a tax on ground covered, not on the key.

Every sprinting frame re-arms a delay. Not sprinting, the delay is served out first and only then does the pool refill, slower than the drain. Tapping sprint repeatedly re-arms the delay and refills nothing. The refill does not require the Boarder to be able to move: when movement is locked out the sprint rule still ticks with no movement, so stamina returns behind a station panel or a pause screen.

Bottoming out at zero latches the Boarder exhausted, and sprint stays refused until the pool is back above a fraction of maximum — not the first frame the bar moves. Without the latch a held Shift re-engages on the first regen frame, spends the fraction it just got back inside one frame, and the player gets a flickering sprint state instead of being out of breath.

The pool's size is pushed onto the controller at Boarding start from a frozen constant — the crew attribute tracks were deleted and nothing on the Ship sells stamina ([Upgrades](Upgrades.md) owns that fact). The push also fills the tank and clears the latch, so a Boarding never starts winded.

Walk, sprint, drain, refill and delay are one argument. Change those five together or not at all.

### Rules

Driven by `WASD` and Shift; bindings: [Field Manual](Field-Manual.md). Shown by the vitals band's stamina bar, whose label reads SPRINT while the Boarder is actually sprinting and STAMINA otherwise, and by footstep cadence shortening between walk and sprint. Reads back as that bar and label — the walk speed itself is never displayed anywhere, and neither is the latch.

The Boarder's walk is the baseline each kind's chase is authored against. A provoked Default (and a Cloaker reading the same numbers) chases just under it, so giving ground buys a moment. The Hunter runs just over it. A Thief is slower still. Sprint outruns everything the garrison fields, which is why the pool rations it. Speeds per kind: [Field Manual](Field-Manual.md). What each kind refuses, and the targeting that makes a pull's aftermath survivable: [Gatekeepers](Gatekeepers.md).

## Additional Details

### Consequences

Give-ground answers differently per kind. Against a marcher or a Thief it is a real purchase of time; against a Hunter it only spends the distance more slowly. The marcher margin exists only while the Boarder is actually walking, so every pause to fire or reload hands the distance straight back, and the lane being retreated down runs toward the Boarder's own Hatch with the far spawn point still feeding it. Speeds: [Field Manual](Field-Manual.md). The under-the-walk Default chase is deliberate (Zo, playtest 2026-08-25).

Sprint is the answer to the Hunter, and it is rationed for exactly that reason. Unlimited sprint would restore the kiting exploit the targeting work closed — outrun the pack forever, plink at it, never take a swing, never risk the Hatch. The pool prices it as a burst: break contact, cross the room, get back to the Hatch. Over any window longer than a few seconds the Boarder's average speed is still below the garrison's.

The latch is what the player actually gets caught by. The bar refills visibly while sprint is still refused, so a sprint counted on can simply fail to engage, and nothing on screen distinguishes recovering from usable. The playable consequence is to leave a reserve rather than spend to zero. The delay compounds it: the pool does not even begin returning for a beat after the last sprinting frame.

Losing movement is a cost, not a pause. Rooted keeps the Boarder shooting and looking while pinning them in place, which is what makes the Default sheet's crew burst a real trade rather than a free window; a station panel or the pause screen removes movement entirely but keeps refilling the pool, which is why the stamina economy never punishes shopping.

Removal test: take locomotion out and the Bastion's geometry stops existing — no lane between the Hatch start line and the zone terminal, no giving ground, no repositioning, no reaching a console. Every other mechanic in this folder is priced in ground covered. Different game; core.

### Classification

Core, loop. Serves Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Fantasy at rank 3: a body moving through a held vessel at a speed the garrison is deliberately tuned against.

### Interacts with

- [Gatekeepers](Gatekeepers.md) — every kind's chase speed is authored against the walk, and the walk-versus-chase margin is what makes a pull's aftermath survivable; that page owns targeting and what each kind refuses.
- [Waves](Waves.md) — group width and the gap between groups decide how much of the room is standing between the Boarder and the ground they meant to give; that page owns delivery.
- [A Boarding](A-Boarding.md) — movement is live only while Tethered, and the distance between the Hatch start line and the zone terminal is the fight; that page owns the lane and the trickle that stops it being skipped.
- [Protocols](Protocols.md) — the Default sheet's crew plant roots the Boarder for its window, and no protocol moves sprint speed or the pool; that page owns the sheets and their windows.
- [Regen](Regen.md) — the same one-second post-event hold shape on the other resource: breaking contact is what lets health climb, and breaking contact is what stamina buys; that page owns the regeneration rule.
- [Upgrades](Upgrades.md) — there is no stamina track and no console sells the pool; that page owns the frozen-constant fact.
- [The Ship](The-Ship.md) — the hull's length is the one place walking is the whole interaction; that page owns the panel lock.
- [Going Down](Going-Down.md) — death locks the body, and a bought respawn resets it to spawn and unlocks it; that page owns the respawn spend.
- [Staying Alive](Staying-Alive.md) — the tactics these speeds produce: retreat to reposition rather than to kite, sprint to break contact, leave a stamina reserve.
