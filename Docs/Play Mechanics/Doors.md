<!--
Purpose: Current representation of the Bastion's doors — what opens one, what an open one changes, and that it stays open across Boardings — as it is in the build.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-30
-->

# Doors

Doors are the Bastion's gates, and the Boarder never operates one — there is no key that
works on a door. A zone's gate opens when its terminal is shut off, or when a Convert window
at the Hatch is survived; opening it frees the corridor for Boarder and Gatekeeper alike and
wakes the spawn points the door was sealing. What makes this a mechanic is that an open door
stays open. The open set is saved onto the realm the moment it opens, so even a Boarding
that opens a gate and is then lost keeps it, and the next Boarding starts with that ground
already taken. Terminal completion does not persist the same way, so a second Boarding walks
further into the hull but refights the zone.

## The interaction

The garrison holds the doors and the Boarder never touches one. A door is the one operable-looking fixture in the Bastion with no interact verb at all — no door implements the interact interface, so the crosshair says nothing at it and the key does nothing on it ([Interacting](Interacting.md)). It carries one bit, locked or unlocked, and that bit is written by the sim on the Boarder's behalf when a zone is finished.

The Bastion holds three populations of door, and only one of them is a gate:

- **Progression gates.** Authored `startsLocked`, with a list of terminals they wait on — the union of the door's own authored list and every field terminal that names it. The demo's spine has two: one at the end of zone 1, and the **control gate** at the end of zone 2, the door to the Control Room. Ids and which zone owns which: `DemoZones.DoorIdByZone`.
- **Passage doors.** Every open passage gets one, emitted unlocked with no requirements. They gate nothing and are never in the graph's way.
- **Spawn-closet doors.** Unlocked, and set to open **for Gatekeepers only** — the leaf rises for a body walking out of the closet and never for the Boarder standing in front of it ([Waves](Waves.md) owns delivery).

**Completing the terminals does not open the gate; it only makes it eligible.** Terminal completion writes into the graph and stops there; a separate call, `OpenEligibleDoors`, turns eligibility into an open door, and a door whose requirement list is empty can never satisfy that test at all.

Two things in the game make that call, through one shared method so they cannot drift: shutting off a zone's terminal ([A Boarding](A-Boarding.md) owns the shutoff) and surviving a Convert window ([The Hatch](The-Hatch.md) owns the window). A failed Convert ends the Boarding before it reaches the call, so it never unlocks anything.

**Opening is one-way.** Nothing in the build re-locks a door once open: the graph only ever adds ids, and the one re-lock path that exists — the Control Room seal — is now called with `false` and nothing else, left in place as an idempotent unseal since the finale stopped being a box fight ([Master Control](Master-Control.md)).

What opening changes, all at once:

- the navmesh carve is switched off for good, and the doorway becomes pathable for Gatekeepers as well as walkable;
- the leaf's collider stops filling the aperture;
- the leaf's material swaps from the gate material to the segment's own wall — locked reads as a seal, unlocked reads as station wall, and that swap is the only thing on screen that ever says a door is a gate;
- the spawn points the door was sealing go live, so the wing behind the first gate starts delivering the moment it opens.

**The slide is fluff.** An unlocked leaf polls for anyone within 5 m, rises once, and never comes down again — it used to read proximity every poll and shut behind the player mid-fight, dropping a slab across the corridor they were shooting down, which is what the current one-way behaviour was written to stop. The code says why the check survived at all: it is kept "rather than replaced with `true` so the leaf still plays its rise the first time somebody walks up to it", and it is gated behind `_unlocked`, so a door the graph has not opened never polls. By the removal test the slide changes no decision and no outcome: delete the animation and the mechanic — the locked bit, the carve, the sealed spawns, the persistence — is untouched. The one residue is that the collider blocks until the leaf clears the aperture, so the first approach to a passage costs the moment the rise takes.

Driven by no input at all: a door answers to the terminal and the Convert window, never to a key. Shown by the leaf standing up and by the gate-to-wall material swap, plus a spoken line on a demo save. Reads back as the `door open <id> cause=shutoff` / `cause=bank` log lines when it opens, and `snapshot restored doors=…` on the next embark.

## Consequences

**A door that opens stays open across Boardings, and that is the whole reason this is an entry.** The open set is snapshotted onto the realm the instant the doors open — inside the same method that opens them — written to disk when the Boarding reaches any terminal phase, and restored on **every** embark, not only a deep one.

Every part of that is load-bearing: because the snapshot happens at the moment of opening rather than at a good ending, a Boarding that opens a door and is then **lost** still keeps the door; and because the restore is unconditional, a Docked return that re-embarks at Wave 1 still finds the corridor it paid for standing open. Door state is realm-side, so it stays with the world and not with the Boarder ([Realms](Realms.md) owns the split), and it stopped being derivable from terminal completions when doors moved off completion — it is its own saved field for exactly that reason.

So the second Boarding into a realm is not the first one again. The lane past the first gate is open from the moment the Boarder lands, the closets that gate was sealing are live from the same moment, and the walk to the Control Room has one fewer seal in it. The open set is also what names the zone the Boarding is working: the code counts open doors up from zone 1 to find the current zone, which is what the Convert's zone gate then reads.

**What does not persist alongside it is the interesting half.** Terminal completion is snapshotted only at the end of a Convert window's Assault, and restored only when the Boarding starts deeper than Wave 1. A Convert window is reachable after a zone's terminals are done and before the control gate opens ([The Hatch](The-Hatch.md)).

On an ordinary Wave-1 embark into a realm with a gate already open, the doors stand open while every terminal reads incomplete: the Hatch offers that zone's Wave again and the zone is re-fought, with its own door open behind it the whole time. Progress through the hull persists; progress through the terminals does not. A Boarding therefore starts where the last one *walked* to, not where it *finished*.

Removal test: take doors out and the Bastion is one open room. The Control Room is reachable on the first Boarding, so the zone order becomes advice rather than structure; the shutoff stops earning anything beyond its payout; the sealed wing delivers from the start; and nothing a Boarding achieves is still standing there the next time. Different game.

## Classification

Core, arc. Serves Fantasy (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Fantasy at rank 3: territory taken in a held vessel, and the only part of it that stays taken. Removal does not tune the Bastion, it flattens it into one room with no order and no memory, and order is the one thing a Boarding leaves behind. Arc rather than loop because the consequence lands between Boardings, not inside one.

## Interacts with

- [A Boarding](A-Boarding.md) — the terminal shutoff is one of the two calls that opens a door; that page owns the shutoff and the loop it ends.
- [The Hatch](The-Hatch.md) — a survived Convert window is the other call; that page owns the window and the dock the same survival offers.
- [Realms](Realms.md) — open door ids are realm-side state, saved with the world and not with the Boarder; that page owns the split.
- [Master Control](Master-Control.md) — the control gate is the door to the finale's room, and the seal that used to shut it during the sequence is gone; that page owns the sequence.
- [Waves](Waves.md) — the spawn closets a gate seals go live when it opens, and a closet's own door rises for Gatekeepers only; that page owns delivery.
- [Interacting](Interacting.md) — the one Bastion fixture that verb cannot address; that page owns the verb and its roster of surfaces.

## Where

- A door is one bit and no verb: locked/unlocked, no interact interface on it — `Assets/Scripts/Game/Bastion/BastionDoor.cs` (`SetOpen`, `IsOpen`, `StartsLocked`)
- Requirements are the union of the door's authored list and every field terminal that names it — `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`RebuildGraph`); `Assets/Scripts/Game/Bastion/BastionTerminal.cs` (`UnlocksDoor`, `SetUnlocksDoor`)
- Completion only makes a door eligible; opening is a separate call, and an empty requirement list never opens — `Assets/Scripts/Sim/Bastion/BastionUnlockGraph.cs` (`TryCompleteTerminal`, `OpenEligibleDoors`, `TryOpenDoors`, `AllRequiredTerminalsDone`)
- Two callers, one shared method: the zone shutoff and the survived Convert window — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`OpenDoorsFor`, called from `ShutOffTerminal`); `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`OnBankWindowSurvived`); the scene-side arm `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`OpenEligibleDoors`, `RefreshDoorStates`, `ApplyDoorState`)
- Opening kills the navmesh carve for good, frees the aperture, swaps the leaf material, and arms the spawn points the door sealed — `Assets/Scripts/Game/Bastion/BastionDoor.cs` (`SetOpen`, `ApplyLeafMaterial`, `sealedSpawns`)
- The demo's two gates, one per zone, the second the control gate — `Assets/Scripts/Sim/Bastion/DemoZones.cs` (`DoorIdByZone`, `DoorId`); the control-gate rule `Assets/Scripts/Sim/Bastion/BastionUnlockGraph.cs` (`ControlRoomReachable`, `Validate`)
- Passage doors emitted unlocked with no requirements; spawn-closet doors unlocked and Gatekeeper-only — `Assets/Scripts/Editor/Bastion/BastionSketchEmitter.cs` (passage emit `Configure(..., locked: false)`, `WireSealedSpawns`); `Assets/Scripts/Game/Bastion/SpawnRoom.cs` (`ReassertClosetDoor`); `Assets/Scripts/Game/Bastion/BastionDoor.Proximity.cs` (`CountsAsOpener`, `AnyoneNear`)
- The slide: an unlocked leaf rises once within range and never shuts again; the collider blocks only while the leaf fills the aperture — `Assets/Scripts/Game/Bastion/BastionDoor.cs` (`Update`, `ProximityRange`, `SlideSpeed`, `LeafTargetY`)
- Nothing re-locks a door; the Control Room seal is only ever called with `false` — `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`SetControlRoomSealed`, `ApplyDoorState`); `Assets/Scripts/Game/Bastion/GameController.MasterControl.cs` (`StartMasterControl` failure path); `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`OnMasterHatchHalfStarted`)
- The open set is snapshotted onto the realm inside the opening call — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`OpenDoorsFor` → `Meta.SetOpenDoorIds`, `layout.CopyOpenDoorIds`); `Assets/Scripts/Sim/Meta/MetaState.cs` (`OpenDoorIds`, `SetOpenDoorIds`); `Assets/Scripts/Sim/Meta/RealmState.cs` (`_openDoorIds`, `SetOpenDoorIds`, JSON write/read)
- Persisted at every terminal phase, lost Boardings included — `Assets/Scripts/Game/Bastion/GameController.EndScreen.cs` (`IsTerminal`, `EnterTerminalPhase` → `GameSession.Save`)
- Restored on every embark, not only a deep one — `Assets/Scripts/Game/Bastion/GameController.cs` (`Start`'s `RestoreOpenDoors` block and its `snapshot restored doors=` line); `Assets/Scripts/Game/Bastion/BastionLayout.cs` (`RestoreOpenDoors`); `Assets/Scripts/Sim/Bastion/BastionUnlockGraph.cs` (`RestoreOpenDoors`, `CopyOpenDoorIds`)
- Door state is its own saved field because it stopped being derivable from completions — `Assets/Scripts/Sim/Meta/SaveFormat.cs` (schema history, entry 10)
- Terminal completion snapshots only at a Convert window's end and restores only on a deep start — `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`SnapshotHatchBankGraph`); `Assets/Scripts/Game/Bastion/GameController.cs` (`OnAssaultEnded` HatchBank branch; Start's `Sim.StartWave > 1` guard on `RestoreCompletedTerminals`)
- The open set names the zone the Boarding is working, which the Convert gate reads — `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`CurrentDemoZone`, `HatchBankStartBlocked`)
- Readback: the open and restore log lines, and the demo's spoken door line — `Assets/Scripts/Game/Bastion/GameController.Terminals.cs` (`OpenDoorsFor` logging); `Assets/Scripts/Game/Bastion/GameController.HatchBank.cs` (`DoorLineFor`)
