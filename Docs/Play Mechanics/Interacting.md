<!--
Purpose: Current representation of interacting — the look-at-and-hold verb every operable surface in the game borrows — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-30
-->

# Interacting

Interacting is the one verb a Boarder uses on every operable surface in the game: look at
it from within a few paces and hold the interact key. The prompt under the crosshair names
what the hold is about to do, and a surface with nothing to offer shows nothing at all. No
hold anywhere banks partial progress: looking away or letting go zeroes it and it starts
over, so what a commitment costs is standing still, and taking it back costs nothing. A
free cursor switches the verb off entirely, with no prompt and no refusal shown.

## The interaction

The Boarder invokes it, against whatever the crosshair is on, in every scene the game has. One component does the whole verb: a raycast forward from the player camera out to a serialized **3.2 m**, the first collider hit walked up to its `IInteractable`, that object's own `Prompt` string published for the HUD, and — only while `E` is held — that object's `Interact(dt)` called once per frame with the frame's delta.

Nothing central composes the prompt and nothing central knows what a verb costs: the interface is two members, a string and a per-frame tick, and every surface in the game fills them in for itself.

**The prompt names what the Boarder is about to do, not what they are looking at.** It is re-read from the target every frame and cleared the moment the ray misses, so it is a live statement of the verb on offer — `[E] Portal — return to Ship`, `[E] repair NAV MAP`, `[T] shut it down`, and at a damaged Hatch the current HP and the price per point. A surface with nothing to offer returns null and the crosshair says nothing at all, which is how a dormant portal pad and a hidden Ship station read as scenery rather than as a broken key.

**A hold accumulates while held, and releasing resets it.** This is one rule with one mechanism, shared by every hold in the game: the Interactor simply stops calling `Interact(dt)` on a release or a look-away, and each holder detects the missed frame and zeroes its own timer (`Time.frameCount` compared against the last serviced frame; the Hatch does it in `LateUpdate` instead, same effect). No hold anywhere resumes, banks partial progress, or survives a glance away — a player who looks away mid-hold has changed their mind, not paused. The only readback of a hold in progress is the prompt swapping to a present participle — `releasing`, `Docking...`, `repairing NAV MAP`, `Withdrawing … unbanked` — there is no meter, no ring and no sound cue anywhere in the verb.

**A free cursor disables interact entirely, and says nothing.** `Tick` returns before it casts whenever the OS cursor is not locked, so no prompt is published and no hold advances; the one exemption is the isometric rig, which aims with the cursor by design and needs no lock.

**This is the same precondition that refuses firing** (`PlayerGun.CanFire`, same iso exemption) — so Escape, or any panel that frees the cursor, leaves a Boarder who can walk but cannot shoot and cannot interact, with no on-screen explanation for either. Firing at least writes a log line naming the fix; interacting refuses in silence. The cursor is recaptured by a left click, and only during the fight — a click while a station panel or a menu owns the screen is left to the UI.

**Tap versus hold is a per-surface choice, not a property of the key.** The Interactor only ever delivers the held drive; a surface that wants a tap reads `Controls.Down(ControlId.Interact)` itself and leaves `Interact(dt)` empty.

The Ship's four consoles are the tap case: `ShipController` resolves the console under the ray on Interact Down and opens its panel (`TryOpenStationFromInteract`), and `StationTerminal.Interact` forwards to the demo gate and no-ops everywhere else — so a station opens the instant the key goes down and holding it does nothing. The Bastion's terminals are the same shape for a different reason: `BastionTerminal.Interact` is empty, and the select is a tap read by the Boarding's prep commands off the interactor's current target (`T` starts or shuts off — [A Boarding](A-Boarding.md) owns the start). With the Ante parked, `E` at a trigger does not arm intensity.

In the Bastion the verb is ticked by the fight's written order, sixth, after locomotion and the gun; on the Ship the component drives itself. Because the prep commands run earlier in that order than the raycast, a select reads the target the raycast found on the previous frame.

Driven by held or tapped `E` (bindings: [Field Manual](Field-Manual.md)). Shown by the one prompt line under the crosshair, on the Bastion HUD and on the Ship HUD alike. Reads back as that line changing — to a present participle mid-hold, to the completed state after, or to nothing when the ray misses.

## Consequences

Interacting is the game's only way to address an object, so its rules are the ceiling on every verb built on it. The 3.2 m reach makes every operable thing a place the Boarder has to stand — the Hatch, the terminal at the end of the lane, the console at the far end of the Ship — which is what makes the walk between them cost something.

The release rule prices commitment without a confirmation dialog anywhere in the game: the way the build says "are you sure" is 1.5 seconds of standing still, and the way it takes the question back is letting go. And because the prompt is the target's own sentence, refusals are told at the crosshair rather than logged — `Terminal T2 — start the wave at the Hatch`, `ARMORY — sealed while the Boarding is live` — which is the build's whole answer to "I pressed the key and nothing happened".

The free-cursor precondition is the sharp edge. It is a real and silent failure mode: pause, or close a panel in a way that leaves the cursor free, and the Boarder keeps walking while both the trigger and the interact key are dead, with the prompt line simply absent rather than refusing. Nothing on screen names the cause or the fix.

Removal test: take the verb out and there is no repair, no Withdraw, no Convert select, no dock, no Wave select, no shop and no way off the wake pad. Every mechanic in this folder either is a hold at a surface or is started by one.

## Classification

Core, loop. Serves Fantasy and Expression (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Fantasy at rank 3 is hands on a held vessel's fixtures, one object at a time; Expression at rank 2 is the hold as the place a stake is committed. Removal does not change the game, it stops it.

## Interacts with

- [A Boarding](A-Boarding.md) — the Wave select is a tap read off this verb's current target and the start is `T`; that page owns the start line and the shutoff.
- [The Hatch](The-Hatch.md) — the densest user: several verbs behind one prompt line, and the only holds that spend; that page owns the withdraw and repair spends.
- [The Ship](The-Ship.md) — the four consoles open on the tap and take the screen, which frees the cursor and so disables this verb until the cursor is recaptured; that page owns the hull and its stations.
- [The Crew](The-Crew.md) — the hull hatch doors are look-only surfaces whose prompt names the seat that owns each one; that page owns the seat roster.
- [Master Control](Master-Control.md) — the finale is one of the two places a select is still required at the terminal itself; that page owns the sequence.
- [Doors](Doors.md) — the one fixture in the Bastion this verb cannot address: a door answers to the terminal, never to the key; that page owns the doors.

## Instances

The implementers of `IInteractable` are this mechanic's roster. Instances vary on:

- **what the hold does and how long it takes**;
- **whether the surface takes a hold at all** — tap-only, or look-only with an empty `Interact`;
- **whether a prompt is offered at all** — a null prompt hides the surface completely;
- **what state the surface refuses in**.

No instance may vary the look-at reach or the release rule: both live in the Interactor, which cannot deliver a hold across a released frame, and no instance may be reached at all with a free cursor.

This table is the canonical roster of interactable surfaces, anywhere in Docs.

| Instance | Differs in | State | Page |
|---|---|---|---|
| The Hatch | Several verbs behind one prompt, ordered by a pure rule; repair and Withdraw apply per frame with no threshold to reach, the dock completes at 1.5 s | in build | [The Hatch](The-Hatch.md) |
| Portal pad | The same 1.5 s dock as the Hatch's, deliberately identical; offers no prompt and shows no lit part until the way home is open | in build | [The Hatch](The-Hatch.md) |
| Recall pad | Hold 1.5 s at the authored Ship-teleporter graybox beside T2/T5; teleports to the Hatch while Tethered | in build | [A Boarding](A-Boarding.md) |
| Bastion terminal | Takes no hold at all — `Interact` is empty; `E` taps to select and `T` starts or shuts off, read off this verb's target by the Boarding's prep commands | in build | [A Boarding](A-Boarding.md) |
| Ship stations (ARMORY, CREW QUARTERS, FABRICATION, NAV MAP) | Open on the tap, not the hold; the panel then takes the screen and frees the cursor, which disables this verb until a click recaptures it | in build | [The Ship](The-Ship.md) |
| NAV MAP repair | The only station that takes a hold: 1.5 s powers the dark console, once per save, and the prompt is the only thing that says so | in build | [The Opening](The-Opening.md) |
| Wake teleporter | 1.0 s to release the glass — the game's first hold; prompts only while sealed and only after the stir line clears, then never again | in build | [The Opening](The-Opening.md) |
| Ship terminal | The mid-deck console that teaches select-then-start at home — `E` taps to arm, `T` starts, the hold path is unused, and after the start it reads `power routed` for the rest of the save | in build | [The Opening](The-Opening.md) |
| Hull hatch door | This page: look-only. Empty `Interact`, and the prompt is a label rather than a verb — `HATCH 2 — yours` / `SEAT 3` / `sealed` — naming which seat the opening belongs to | in build | [The Crew](The-Crew.md) |
| Range door | A 1.0 s hold that changes scene, matched to the teleporter's so the two feel the same; the refusal while a Boarding is live lives in the loader, not the door | in build, the way in Coming Soon | [The Range](The-Range.md) |

## Where

- The interface — a prompt string plus a per-frame `Interact(dt)` while held and targeted — `Assets/Scripts/Game/Player/Interactor.cs` (`IInteractable.Prompt`, `IInteractable.Interact`)
- Camera-forward raycast to a serialized 3.2 m; the target is the hit collider's `IInteractable` in its parents; iso casts from the cursor but re-checks the same range against the player — `Assets/Scripts/Game/Player/Interactor.cs` (`range`, `Tick`, `IsoCastDistance`)
- The prompt is the target's own string, republished every frame and null when the ray misses — `Assets/Scripts/Game/Player/Interactor.cs` (`CurrentPrompt`, `Current`); shown by `Assets/Scripts/Game/Bastion/HudController.Fight.cs` (`SetPrompt`) and `Assets/Scripts/Game/Ship/ShipHud.cs`
- The hold drive: `Interact(dt)` only while the Interact binding is held — `Assets/Scripts/Game/Player/Interactor.cs` (`Controls.Held(ControlId.Interact)`); binding `Assets/Scripts/Game/Input/Controls.cs` (`ControlId.Interact`, `Held`, `Down`)
- Release resets, one mechanism, every holder: the missed frame is the release — `Assets/Scripts/Game/Range/RangeDoor.cs` (`Interact` frame check), `Assets/Scripts/Game/Ship/ShipTeleporter.cs` (`Interact`, `Prompt`), `Assets/Scripts/Game/Bastion/HatchPortal.cs` (`Interact`, `Update`), `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`LateUpdate` — `_dockHold = 0f`), `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`TickHold`, `PromptOverride`)
- A free cursor returns before the cast, with no prompt and no log; the isometric rig is the exemption — `Assets/Scripts/Game/Player/Interactor.cs` (`Tick` cursor guard, `_isoRig`)
- The same precondition refuses firing, logged rather than shown — `Assets/Scripts/Game/Player/PlayerGun.cs` (`CanFire`, `Refuse`, `IsoAimActive`); recapture is a click, fight only — `Assets/Scripts/Game/Player/FirstPersonController.cs` (`Tick` click-to-relock, `SetCursorLocked`)
- Ship stations open on Interact Down against the console under the ray; the held path only forwards to the demo gate — `Assets/Scripts/Game/Ship/ShipController.cs` (`Update` Interact Down branch, `ResolveTargetedStation`, `TryOpenStationFromInteract`); `Assets/Scripts/Game/Ship/StationTerminal.cs` (`Interact`, `Prompt`)
- Bastion terminals take no hold; the select is a tap read off the interactor's current target — `Assets/Scripts/Game/Bastion/BastionTerminal.cs` (`Interact`, `Prompt`); `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`TickPrepCommands`, `ToggleTerminalSelect`, `ToggleHatchSelect`)
- Ticked sixth in the Bastion's written order, after the gun; self-driven on the Ship — `Assets/Scripts/Game/Bastion/GameController.cs` (`Tick` step 6, `ApplyMetaEffects` binding and `SetHosted`); `Assets/Scripts/Game/Player/Interactor.cs` (`SetHosted`, `Update`)
- Hold lengths, per surface — `Assets/Scripts/Game/Bastion/HatchPortal.cs` (`HoldSeconds`), `Assets/Scripts/Game/Bastion/HatchRecallPad.cs` (`HoldSeconds`), `Assets/Scripts/Game/Bastion/RepairInteractable.cs` (`DockHoldSeconds`), `Assets/Scripts/Game/Ship/ShipTeleporter.cs` (`UnlockHoldSeconds`), `Assets/Scripts/Game/Ship/DemoStationGate.cs` (`NavRepairHoldSeconds`), `Assets/Scripts/Game/Range/RangeDoor.cs` (`HoldSeconds`)
- Recall pad beside T2/T5 — authored Ship teleporter graybox (`T2 Portal` / `T5 Portal`) with `HatchRecallPad`; teleport `Assets/Scripts/Game/Bastion/GameController.Faucet.cs` (`TeleportLocalToHatch`)
- The tap-only Ship terminal: arm on Down, start on `T`, one-shot per save — `Assets/Scripts/Game/Ship/ShipTerminal.cs` (`Update`, `Interact`, `StartTerminal`)
- The look-only hull hatch door: empty `Interact`, prompt names the owning seat — `Assets/Scripts/Game/Ship/ShipHatchDoor.cs` (`Prompt`, `Interact`, `Refresh`)
- The range door's own refusal lives in the loader rather than on the door — `Assets/Scripts/Game/Flow/SceneFlow.cs` (`LoadRange`, refusing on `Sim.Phase == RunPhase.Tethered`); whether a slab is authored into the shipped hub is scene wiring and is not readable here — the mirror carries no `.unity` files, and `SceneFlow`'s `Range` remark saying nothing in the hub reaches it is a comment, not a checked fact ([The Range](The-Range.md) holds that question)
