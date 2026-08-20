# 0 — Wake on a dead Ship

<!--
Purpose: Teleporter wake (starboard crew alcove) → repair NAV → ship terminal start
routes power, hands the MK-1 over, and spawns two weak Gatekeepers → gated
Lock (intruder release on Continue) → kill them → CREW heal (no menu) →
EMBARK. Only NAV shows a name or interaction before it has a function
(2026-08-18 revision — Zo): ARMORY / CREW / FABRICATION are hidden until they
gain one. Solo by construction: multiplayer stays locked until that save's
first wheel purchase (beat 3).
Status: demo spec
Authority: implementable
Update trigger: when spawn furniture changes, when the ship-fight spec changes,
when stamped line copy changes, when the multiplayer gate moves
Last updated: 2026-08-20
Depends: voice.md
-->

## Why this beat exists

The live game opens with four live consoles, a 0-salvage ARMORY buy, and NAV
that warns "no weapon" then EMBARKs anyway. This beat teaches every verb the
Bastion will ask for, at home, in order: hold-repair (NAV), the terminal
contract — select, start, enemies come, and the start arms you (ship
terminal), fight, and heal (CREW). By EMBARK nothing on the Bastion is a new
verb except the stakes. Stations that have no function yet do not exist to
the player: no world label, no prompt, no interaction (2026-08-18 revision) —
each surfaces the moment it gains one (CREW after the fight, ARMORY at the
wheel unlock, FABRICATION when the demo ends).

Solo: multiplayer is locked until this save's first wheel purchase
([`README.md`](README.md)). Speaker is **Game**. The player does not reply
([`voice.md`](voice.md)).

## How to read this

This is the play-through. Every player-facing string is in **bold**. A dash
means the game says nothing that beat. **Assumption** marks a working
structure that is not in the tree yet — change it in this file, then implement
from the new stamp. Do not leave a beat as "Read" or "walk there."

Lock lines use the Shift box: speaker label **GAME**, body copy, one
**Continue** control (click the box, or Space). Esc dismisses the same as
Continue. No reply row. Subtitle lines sit at the bottom of the screen and
auto-advance in **4 s**. The interact prompt is the existing Ship prompt
(`ShipHudView.SetPrompt`). When nothing is targeted and a standing objective
is set, that string is the idle prompt.

## Working assumptions (change here, then build)

| # | Assumption | Why it is stamped |
|---|---|---|
| A1 | First-person the whole beat. No third-person wake cut, no companion, no VO. | Matches live Ship / Bastion. |
| A2 | Fresh Start New: screen is **black** for **0.4 s**, then fades up over **1.5 s** into the closed capsule. | No fade exists in tree. Full-screen black Image, lerp alpha. |
| A3 | Player starts **inside** a closed capsule. Camera at eye height in the bed, looking at the lid. Move locked (Rooted), look **free** until the lid opens — the small-cone clamp was dropped at build (2026-08-17): the rig has no cone mechanism and full look inside a sealed lid reads fine; revisit only on play data. `SM_Prop_CryoBed_01` is not in this tree, so the capsule is graybox primitives until an art pass. | "Interact with the capsule to open it." A standing start on the deck skips the unlock. |
| A4 | Stir text plays **after** the fade, **before** the unlock prompt. One Lock line, then the prompt. | Text-then-verb. Not a prompt on black. |
| A5 | Capsule unlock is hold Interact **1.0 s**. Lid lifts over **0.8 s**. Then they can stand and walk. | Same hold family as NAV (1.5 s). Shorter because it is the first hold. |
| A6 | Dead hull: Directional Light intensity **0.15** (live is 0.8). Ceiling panel emissives stay; they read as cold glow, not working lights. No new light API. | One Directional exists. |
| A7 | Capsule sits at the **starboard crew bunk** — it replaces that bed (`ShipDress/CrewStarboard/Bed` inactive), world **(7.5, 0, 6.2)**, facing the corridor side (2026-08-18 revision — Zo; was the port alcove at (-3, 1, 8)). Deck exit `Spawn_Capsule` at **(7.5, 1.1, 7.6)**. | They wake in a crew bunk like the rest of the crew would. Exact pose is art. |
| A8 | Ship terminal is a new console mid-deck between NAV and ARMORY, working pose **(6.0, 0, 22.0)**. Visible from NAV after the repair line points at it. | Object does not exist. Not a `BastionTerminal` component. |
| A9 | Intruders spawn at `Spawn_Intruder_1` **(2.5, 0, -8)** (FAB side of the corridor) and `Spawn_Intruder_2` **(2.5, 0, 0)** (mid-corridor), and walk toward the player / ARMORY. | Two bodies, in sight of the ARMORY rack. |
| A10 | Ship fight uses the live first-person gun and `PlayerHealth`. Ship HUD has **no vitals today** — the demo shows a minimal HP readout while the fight is live or the player is hurt. Scene check restamped (2026-08-17 review): the live Ship YAML has **no PlayerGun** and **no NavMesh** (`NavMeshSettings.m_NavMeshData` fileID 0). Scratch authors the gun on the rig camera. Intruders straight-walk when off-mesh (`ShipIntruder`). | Resolved — was `unknown — needs scene check`. |
| A11 | `demo.intruders` never writes `timeScale`. The pause is **gated execution**: `T` fires the line and arms the intruder **release**; the release executes on the line's Continue — and on unload/reload, so the gate cannot stick. Sim-safe. | Zo 2026-08-17 — the earlier `timeScale = 0` exception is withdrawn. |
| A12 | No music stinger. Existing Ship bed / silence. No new audio pack this beat. | Keep the grain on verbs and copy. |
| A13 | First menu of the demo is the NAV picker at EMBARK. ARMORY / CREW / FABRICATION are **hidden** this beat — no label, no prompt, no interaction (CREW surfaces after the fight for the tap-heal). | Beat 3 / 4 own those menus. |
| A14 | Continue / new save from the main menu loads Ship with `DemoActive`. Main-menu chrome is unchanged. | Beat 0 starts on the first Ship frame, not on the menu. |

Hold Interact already reaches `IInteractable.Interact(dt)` (`Interactor.Tick`,
range **3.2 m**). Stations open on Interact **Down** — that path must be gated
**before** `OpenStationPanel` or it skips every hold and pops menus this beat
forbids.

## Play-through

### 0 — Load (black)

The main menu Start New (or Continue on a fresh demo save) loads `Ship.unity`.
The first Ship frame is **black**. No prompt. No standing objective. Move and
look locked. Camera is already inside the closed glass (A3).

| | |
|---|---|
| **See** | Full-screen black. No HUD. |
| **Game** | — |
| **Prompt** | — |
| **Standing** | — |
| **Player** | Nothing. 0.4 s hold on black (A2). |
| **Log** | existing `[Ship] aboard the ship — …` then `[Demo] spawn teleporter` |

### 1 — Fade up, still sealed

Black overlay fades to 0 over **1.5 s**. First-person: standing on the pad,
the glass cylinder wrapped around the camera, cold emitter glow from overhead
through it (A6). Hull beyond is barely readable. The view starts **level** —
no forced pitch, because there is no lid to look down at. Look is free inside a
small cone; WASD does nothing. Crosshair off while sealed.

| | |
|---|---|
| **See** | Closed glass cylinder. Dark ship beyond. No station labels lit. |
| **Game** | — (fade is silent) |
| **Prompt** | — |
| **Standing** | — |
| **Player** | May look. Cannot walk. Cannot interact yet. |

### 2 — Stir (text before the unlock)

When the fade finishes, the Shift box opens. Move still locked.

| | |
|---|---|
| **See** | Same sealed view, standing. Shift box over it. Speaker **GAME**. |
| **Game** | `demo.wake_stir` · Lock · Game. **You wake in a sealed capsule. The ship is dark. Nothing answers.** |
| **Continue** | **Continue** (click / Space). Esc same. |
| **Prompt** | Hidden while the Lock is up. |
| **Standing** | — (not yet; they have not been given a job) |
| **Player** | Read. Dismiss. |
| **Log** | `[Demo] line id=demo.wake_stir mode=Lock scope=Local speaker=Game` then `line ended` |

### 3 — Release the teleporter

Lock dismisses. Still inside. The prompt sits on the console ring at the pad's
edge — **eye level, a horizontal raycast**, not overhead and not underfoot.
This is the first interact of the game.

| | |
|---|---|
| **See** | Closed glass. Prompt on the console ring. |
| **Game** | — |
| **Prompt** | **[E] release** |
| **Standing** | Release the teleporter (idle prompt is the same string if they look away from the console — they cannot walk, so this is insurance) |
| **Player** | Hold Interact **1.0 s**. Release before 1.0 s resets. During hold the prompt reads **[E] releasing**. |
| **Then** | Glass retracts **down into the pad** over **0.8 s**. Movement unroots; **the view is left exactly where they were looking** — no pitch snap. They may walk out onto the deck at `Spawn_Teleporter`. Crosshair on. |
| **Log** | `[Demo] teleporter released` |

If they never hold: they stay on the pad. No other interactable is in range.
No skip. No auto-open.

### 4 — On the deck (job named)

The moment both feet are on the deck (or the glass finishes retracting —
**assumption**: fire on glass-down complete; there is no stand animation to
wait for now that the wake is already upright), the Shift box opens again.

| | |
|---|---|
| **See** | Dead midships from the starboard crew room (A7). NAV MAP is forward along the hull `(2.5, 0, 28.2)`. Ship terminal is not obvious yet (mid-deck, A8). The only station world-label visible is **MAP** — ARMORY / CREW QUARTERS / FABRICATION are hidden until they have a function (A13). |
| **Game** | `demo.wake` · Lock · Game. **Capsule open. Ship power is dead. Find the NAV MAP — hold Interact to repair it.** |
| **Continue** | **Continue** |
| **Prompt** | Hidden during Lock. |
| **Standing** (after dismiss) | **Repair the NAV MAP** |
| **Idle prompt** | **Repair the NAV MAP** |
| **Player** | Dismiss. WASD + mouse. |
| **Log** | `[Demo] line id=demo.wake …` |

### 5 — Walk to NAV (hidden consoles)

They walk the corridor toward NAV. A hidden station shows **nothing** —
looking at it keeps the idle prompt, Interact Down is consumed silently
(log only), and its world label is off.

| Target | Prompt | On E | Log |
|---|---|---|---|
| Nothing | **Repair the NAV MAP** | — | — |
| NAV MAP (dark) | **[E] repair NAV MAP** | starts the hold (step 6) | — |
| ARMORY | — (hidden) | nothing, no panel | `[Demo] station hidden ARMORY` |
| CREW QUARTERS | — (hidden) | nothing | `[Demo] station hidden CREW QUARTERS` |
| FABRICATION | — (hidden) | nothing | `[Demo] station hidden FABRICATION` |
| Ship terminal (unpowered — NAV not repaired) | **[E] Ship terminal — NAV first** | refuse | `[Demo] station dark Ship terminal` |

No line for a hidden bump. Walking the wrong way is allowed; the dead
consoles read as furniture until they matter.

### 6 — Repair NAV MAP

At NAV. Hold Interact **1.5 s**. Release resets. No bar — text only.

| | |
|---|---|
| **See** | Dark NAV console. During hold the prompt changes. On complete: NAV world-label / console reads live (existing powered look — **assumption**: whatever the live powered station already does; do not author a new VFX pack). |
| **Prompt during hold** | **[E] repairing NAV MAP** |
| **Game** (on complete) | `demo.nav_repair` · Lock · Game. **NAV MAP is live. Now the ship terminal — select it, then start it. That is how every terminal works.** |
| **Continue** | **Continue** |
| **Standing** (after dismiss) | **Start the ship terminal** |
| **Idle prompt** | **Start the ship terminal** |
| **NAV prompt after power, fight not done** | **[E] NAV MAP — clear the ship first** (Down opens nothing until the fight is done — or opens the picker and EMBARK refuses; **assumption**: no panel until `DemoShipFightDone`. Named refuse if they somehow commit.) |
| **Log** | `[Demo] NAV powered` then `line id=demo.nav_repair …` |

### 7 — Ship terminal: look, select, start

They walk from NAV toward mid-deck. The ship terminal sits there (A8).
No extra line on look-at — `demo.nav_repair` already taught select-then-start.

| | |
|---|---|
| **See** | A console that is not one of the four shops. Unlit until they arm it (**assumption**: armed state uses the same selected look as a Bastion Field terminal). |
| **Prompt unarmed** | **[E] Ship terminal — route power** |
| **On E** | Arms. No panel. |
| **Prompt armed** | **[T] Ship terminal — start** |
| **On T** | One-shot. Sets `DemoArmoryPowered` and **grants the MK-1** in the same save (`DemoWeaponGranted`, Secondary slot, in the hands — 2026-08-18 revision: the terminal arms you, there is no rack walk). Fires `demo.intruders`; the two Gatekeepers **release on its Continue** (A9, A11). Immediate step 8. |
| **Game** | — (no line on arm; the gated line is the next beat) |
| **Standing** | **Start the ship terminal** until T |
| **Log** | `[Demo] ship terminal started` then `[Demo] granted MK-1` then existing `[Ship] bought MK-1 Sidearm for 0 — equipped Secondary…` |

After the fight this console's prompt is **[E] Ship terminal — power routed**
(no-op). No juice / Intensity row — there is no Unbanked on the Ship.

### 8 — Intruders. Gated Lock. Instruction.

`T` fires `demo.intruders` in the same frame and **arms** the intruder
release. `timeScale` stays **1** (A11) — the release executes on the line's
Continue (and on unload/reload, so the gate cannot stick). Nothing hunts them
while the box is up.

| | |
|---|---|
| **See** | Shift box. The corridor is still clear — the intruders release on Continue. The MK-1 is already in the hands behind the box. |
| **Game** | `demo.intruders` · Lock · Game. **Gatekeepers aboard. The terminal armed you — MK-1 Sidearm, loaded. Put them down.** |
| **Continue** | **Continue** — releases the intruders (gated execution, A11). No `timeScale` write. |
| **Standing** (after dismiss) | **Kill the Gatekeepers** |
| **Idle prompt** | **Kill the Gatekeepers** |
| **Player** | Dismiss. The two bodies spawn and walk toward them (A9). They are **armed** — the fight starts the moment they Continue. HP floor is 1 — they can be hit, they cannot die. |
| **Log** | `line id=demo.intruders mode=Lock scope=Local speaker=Game` then, on Continue, `[Demo] intruders spawned n=2` |

Reload mid-fight: both bodies respawn, no line replay, immediate release;
standing is **Kill the Gatekeepers**. (A pre-revision save that powered the
ARMORY without the grant gets the grant on load and hears the line it never
heard.)

### 9 — Armed at the start (no rack walk)

Retired step (2026-08-18 revision): the MK-1 grant rides the terminal's T —
there is no ARMORY visit this beat. ARMORY stays **hidden** until the wheel
unlocks (beat 3). `demo.weapon_grant` is retired with it — the intruders line
carries the armed copy. HP bar comes visible with the fight (A10).

### 10 — The fight

| | |
|---|---|
| **See** | Two weak Defaults (lowest HP / damage tier in tree). First-person MK-1. HP can drop; it stops at **1**. |
| **Game** | — during the fight. |
| **Prompt** | Interact prompts as in the tables; idle **Kill the Gatekeepers**. |
| **Player** | Shoot both. Getting hit is intended. |
| **On first kill** | No line. Log `[Demo] intruder down 1/2`. |
| **On last kill** | `demo.ship_clear` · Lock · Game. **Clear. You took hits — CREW QUARTERS patches you up. Then the NAV MAP: EMBARK when you are ready.** |
| **Continue** | **Continue** |
| **Standing** (after dismiss) | **Restore health at CREW QUARTERS** if HP < max; else **EMBARK from the NAV MAP** |
| **Idle prompt** | the standing string |
| **Log** | `[Demo] intruder down 2/2` then `[Demo] ship clear` (`DemoShipFightDone`, Save) |

Bodies despawn or stay as corpses — **assumption**: despawn on death, same as
Bastion fodder. Do not leave two obstacles in the corridor.

### 11 — CREW heal (no menu)

CREW **surfaces here** — its label and prompt appear the moment the fight is
done (its first live function, 2026-08-18 revision). Optional. EMBARK is not
gated on full HP.

| | |
|---|---|
| **See** | CREW QUARTERS label now visible; console prompt live. HP bar still up until they leave the Ship or it is full. |
| **Game** | — (`demo.ship_clear` already sold it) |
| **Prompt** | **[E] CREW QUARTERS — restore health** |
| **On E** | HP to max in one tap. No panel. No hold. |
| **Prompt at full** | **[E] CREW QUARTERS — crew is rested** (no-op) |
| **Standing** | **EMBARK from the NAV MAP** (flips on heal, or already flipped if they dismissed `demo.ship_clear` at full HP) |
| **Idle prompt** | **EMBARK from the NAV MAP** |
| **Log** | `[Demo] CREW heal hp=<n>→<max>` |

FAB is still hidden.

### 12 — EMBARK (first menu)

NAV, fight done. Interact **Down** opens the picker — the demo's first menu.
**Assumption**: the live NAV panel, start wave **1**, no chain, no repair-rig
commit gate. Solo: one EMBARK control, no Ready, no countdown.

| | |
|---|---|
| **See** | NAV MAP panel. Start wave 1. EMBARK. |
| **Prompt on the console** | live embark copy without forcing a rig — **assumption**: **[E] NAV MAP — EMBARK** |
| **Game on Ship** | — |
| **Player** | Click EMBARK. Ship unloads. Bastion loads. |
| **On first Bastion Prep frame** | `demo.embark_first` · Lock · Game. **Quiet Hatch. Find a field terminal. Select it, then start — wake the place.** |
| **Continue** | **Continue** |
| **Standing** | **Find a field terminal** (beat 1) |
| **Log** | existing `[Ship] embarked…` plus `[Demo] first embark` once |

Early EMBARK (if the panel is reachable before the fight is done) uses
`demo.embark_blocked` — one id, three strings, first play **per reason** per
Ship visit:

| Condition | Game says | Log |
|---|---|---|
| Terminal not started | **You are not ready. Start the ship terminal first.** | `[Demo] embark refused — ship terminal` |
| No weapon (pre-revision saves only — the start grants) | **You are unarmed. Start the ship terminal.** | `[Demo] embark refused — no weapon` |
| Intruders alive | **Gatekeepers are still aboard. Clear the ship first.** | `[Demo] embark refused — intruders aboard` |

### Reload / later loads (same hull)

| Condition | Spawn | What they see / hear |
|---|---|---|
| Capsule still sealed (`!DemoCapsuleOpened`) | inside capsule, black → fade | `demo.wake_stir` then unlock then `demo.wake` |
| Capsule opened, NAV dark | `Spawn_Capsule` (deck beside the starboard bunk) | lid open, no reseal, no stir; standing **Repair the NAV MAP** |
| NAV powered, terminal not started | `Spawn_Nav` (deck at `(2.5, 1.1, 12.5)` until art moves it) | no stir; standing **Start the ship terminal** |
| Terminal started, fight not done | `Spawn_Nav` | both intruders release immediately, no line replay; standing **Kill the Gatekeepers** (a pre-revision save without the grant gets it on load, and hears `demo.intruders` gated on its Continue) |
| `DemoShipFightDone` | `Spawn_Nav` | beat 0 over; standing **EMBARK from the NAV MAP** |
| `DemoActive` false | live Ship pose | this beat is a no-op |

Quit mid-fade or mid-stir: next load restarts from black. Do not serialize
intruder instances — respawn from flags.

## Voice — every spoken line in this beat

| # | Id | When | Mode | Exact copy |
|---|---|---|---|---|
| 1 | `demo.wake_stir` | fade finished, still sealed | Lock | You wake in a sealed capsule. The ship is dark. Nothing answers. |
| 2 | `demo.wake` | lid open complete | Lock | Capsule open. Ship power is dead. Find the NAV MAP — hold Interact to repair it. |
| 3 | `demo.nav_repair` | NAV hold complete | Lock | NAV MAP is live. Now the ship terminal — select it, then start it. That is how every terminal works. |
| 4 | `demo.intruders` | T on ship terminal (grant already landed) | Lock (release gated on Continue) | Gatekeepers aboard. The terminal armed you — MK-1 Sidearm, loaded. Put them down. |
| 5 | `demo.weapon_grant` | retired (2026-08-18 — the terminal grants; row kept in the registry, no caller) | Subtitle | Loaded. Put them down. |
| 6 | `demo.ship_clear` | last intruder down | Lock | Clear. You took hits — CREW QUARTERS patches you up. Then the NAV MAP: EMBARK when you are ready. |
| 7 | `demo.embark_blocked` | early EMBARK | Lock | (three strings in the refuse table) |
| 8 | `demo.embark_first` | first Bastion Prep | Lock | Quiet Hatch. Find a field terminal. Select it, then start — wake the place. |

Everything else the player reads is a **prompt** or a **standing / idle**
string, listed in the play-through. Those are not voice ids.

## Have vs build (inventory)

| Object | Exists? | Where | Beat 0 |
|---|---|---|---|
| `Player` + FPC + `PlayerHealth` | yes | `Ship.unity` `(2.5, 1.1, 12.5)` | Have. `spawnPoint` unset |
| Fade-from-black overlay | yes | `Demo Fade` on Ship HUD | Authored 2026-08-17 |
| Closed capsule + lid + in-bed camera | yes | `Demo Capsule` graybox at A7 | Authored 2026-08-17 |
| `PlayerGun` / Ship NavMesh / damage | gun yes, NavMesh no | PlayerGun on Ship camera; no `Ship-NavMesh` asset | A10. Intruders straight-walk |
| `Spawn_Capsule` / `Spawn_Nav` / `Spawn_Intruder_1/2` | yes | empty transforms | Authored 2026-08-17 |
| Ship terminal | yes | `Ship Terminal` at A8 | Authored 2026-08-17 |
| Four stations | yes | poses in play-through | Have; dark / grant / heal gates live |
| Ship HUD vitals | yes | `Demo Vitals` | Authored 2026-08-17 |
| Weakest Gatekeeper spec | yes | fodder Default | Have; count 2 |
| HP floor | yes | `PlayerHealth` + `ApplyReplicatedHp` | Clamp 1 on Ship while `DemoActive` |
| MK-1 buy | yes | `BuyWeapon`, cost 0, owned→false | Have; grant treats owned as success |
| Open-on-Down | yes | `ShipController` via `DemoStationGate` | Gated before `OpenStationPanel` |
| Multiplayer gate | yes | F10/F11 + lobby | Refuse while `DemoWheelBuysLeft == 2` |
| Demo flags | yes | v9 + `DemoCapsuleOpened` | Fresh campaign demo-on; pre-v9 played → demo-off |

## Create

| Path | Role |
|---|---|
| `DemoProgress.cs` | `CanOpenStation`, `CanEmbark`, `MultiplayerUnlocked`. No UnityEngine |
| `SaveData` / `MetaState` | v9 fields + `DemoCapsuleOpened` |
| `DemoStationGate.cs` | Dark / hold / grant / heal routing before `OpenStationPanel` |
| `ShipTerminal.cs` | E arm / T start → route power, grant the MK-1, arm the gated spawn |
| `ShipCapsule.cs` | In-bed camera, lid hold, fade hook |
| `ShipSpawn.cs` | Capsule vs NAV vs mid-fight respawn |
| Scene | Cryo bed, spawns, ship terminal, NavMesh if missing, fade overlay on Ship HUD |

## Change

| File | Change |
|---|---|
| `ShipController` | Gate Down through `DemoStationGate` |
| `MapStation` | Prompts + refuse table; no panel until fight done |
| Station `PromptHint`s | Strings in the play-through |
| `PlayerHealth` | Floor 1 on Ship while `DemoActive` |
| `ShipHud` / `ShipHudView` | Idle standing prompt; HP during the fight |
| F10/F11 | Multiplayer lock log |
| `MetaState` / JSON | v9 |

## Delete

Nothing. ARMORY catalog rows stay behind the gate (panel unlocks in beat 3).

## Numbers

| Name | Value | Source |
|---|---|---|
| Black hold | 0.4 s | A2 |
| Fade up | 1.5 s | A2 |
| Capsule unlock hold | 1.0 s | A5 |
| Lid lift | 0.8 s | A5 |
| NAV repair hold | 1.5 s | this spec |
| Subtitle dwell | 4 s | `voice.md` |
| Intruders | 2, weakest fodder | this spec |
| HP floor | 1 | this spec |
| Interact range | 3.2 m | `Interactor.range` |
| MK-1 cost | 0 | `WeaponCatalog` |
| Fodder damage vs player | **4** per **0.7 s** swing; body 30 HP, walk 3.2 m/s | starting values at the build (2026-08-17), from the Bastion fodder-vs-Hatch figures; tunable, needs play data |

## Multiplayer

None this beat. Gate: `DemoActive && DemoWheelBuysLeft == 2`. F10/F11 log
`[Demo] multiplayer locked — buy an upgrade first`.

## Driven / Shown / Reads-back

- **Driven by** fade, hold-E teleporter, hold-E NAV, E/T ship terminal, E grant,
  kills, E heal, EMBARK.
- **Shown** black → sealed glass → dead hull (only MAP labelled) → NAV live →
  gated Lock with the gun already in hand → two bodies walking → HP drop →
  CREW surfaces and heals → NAV picker.
- **Reads-back** `[Demo] spawn teleporter`, `teleporter released`, `NAV powered`,
  `ship terminal started`, `granted MK-1`, `intruders spawned n=2`,
  `intruder down k/2`, `ship clear`, `CREW heal`, `first embark`,
  `station hidden <name>` on hidden bumps, refuse lines, every `line id=…`.

## Traps

- Skipping the lid and spawning them on the deck. The unlock is the first
  hold.
- Putting `demo.wake` on black, or the unlock prompt on black. Stir text is
  after the fade, prompt is after stir dismiss.
- Open-on-Down at ARMORY/CREW. Gate before `OpenStationPanel`.
- `BuyWeapon` already-owned returns false — grant treats owned as success.
- A stuck intruder gate if the line is skipped by a scene change — release on
  Continue **and** on unload/reload. No `timeScale` write anywhere (the
  2026-08-17 exception is withdrawn — Zo, 2026-08-17).
- HP floor that misses fall / self damage.
- Serializing live intruders.
- Dragging `BastionTerminal` onto the Ship.
- Teaching juice here.
- A companion who opens the lid.

## Depends / Unblocks

Depends: `voice.md` runner; fade overlay; ship-fight scene check. Unblocks:
beat 1 EMBARK. Beat 3 reads `DemoArmoryPowered` already true.

## Verification

1. EditMode: v9 round-trip; `DemoCapsuleOpened` false on fresh; pre-v9 with
   `CleanExits >= 1` → `DemoActive false`.
2. EditMode: `CanEmbark` matrix; `MultiplayerUnlocked` false at buys-left 2.
3. PlayMode, watch the tape: black 0.4 s → fade 1.5 s → `wake_stir` copy →
   Continue → **[E] release** → hold 1.0 s → glass down → `wake` copy → walk →
   hidden ARMORY shows nothing (no label, no prompt, E consumed) → NAV hold →
   `nav_repair` copy → E/T terminal → `granted MK-1` in the log, gun in hand →
   `intruders` copy, corridor clear, `timeScale == 1` → Continue → intruders
   release and walk → both die → `ship_clear` copy → CREW label appears →
   CREW E heals, no panel → NAV panel → EMBARK → Bastion `embark_first` copy.
4. HP floor: overdamage leaves 1, no `Die()`.
5. F10 refuses with the lock log.
6. Quit after unlock, before NAV: reload on deck, no reseal.
7. Quit after T, before both kills: reload releases both intruders
   immediately, no line replay, gun still in hand.
8. No `[Dev]`.

## Rejected

Granting via an ARMORY panel. The ARMORY rack walk (revised out 2026-08-18 —
the terminal's start arms you). Named "unpowered" prompts on stations with no
function (hidden instead, same revision). Auto-open lid. Auto-own Default.
Lighting FAB. A third Gatekeeper. Invulnerability instead of the 1-HP floor.
VO. A wake companion. Third-person cryo cut. A `timeScale = 0` pause
(exception withdrawn 2026-08-17).
