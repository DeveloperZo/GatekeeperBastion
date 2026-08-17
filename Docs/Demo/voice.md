# Voice pipe

<!--
Purpose: Shared Dialogue System import, runner, net RPC, and conversation ids every
demo beat starts. Implement this before beat 0’s lines, or the repair has no speaker.
Status: demo spec
Authority: implementable
Update trigger: when the pack version changes, when a conversation id is added, when
input-lock rules change
Last updated: 2026-08-16
-->

There is no narration system in the tree. Pause exists and does not stop the sim
(`Time.timeScale` is off the table: `Decisions.md`, `HudController.Pause`). Compendium
is a pause encyclopedia (`M62`), not a director. Interact prompts are mechanical. The
mission drawer is fight chrome.

## Player-visible change

On the dead Ship, repairing NAV plays a Ship-PA line in a Shift-skinned subtitle
panel, movement locks, cursor frees, sim may tick. In Bastion Prep, a terminal or
Hatch line can do the same. During Assault, a line can appear as subtitles only —
look, move, and fire stay live.

No companion. No named character. Speakers are furniture: Ship PA, Hatch, terminal.

## Asset

Zo owns [Dialogue System for Unity](https://assetstore.unity.com/packages/tools/behavior-ai/dialogue-system-for-unity-11672)
(Pixel Crushers, SKU **11672**). Library copy shows **1.1.4** (not imported). Store
current is **2.2.73.2**, Unity 6 listed compatible. **Zero** `PixelCrushers` /
`DialogueSystem` references under `Assets/` today.

Import **2.2.73.2** (or the store current at import time) into
`Assets/Plugins/Pixel Crushers/Dialogue System/`, same treatment as Odin
(`Assets/Plugins/Sirenix/`). Tracked plugin, not a Synty dump. Pack size ~33 MB.

**Do not import Storyteller.** Same library folder; it is not this pipe.

After import: `read_console` for compile; add an asmdef reference from
`GatekeepersProtocol` (Game) only. `GatekeepersProtocol.Sim` must not reference
Pixel Crushers (`Architecture.md` one-way rule).

## Create

| Path | Role |
|---|---|
| `Assets/Plugins/Pixel Crushers/Dialogue System/` | Vendor pack |
| `Assets/Dialogue/Demo Database.asset` | Dialogue System database. Conversations named exactly as the id table below |
| `Assets/Scripts/Game/Demo/DemoVoice.cs` | LOCAL-OWNER runner. Wraps `DialogueManager.StartConversation`. Owns input-lock policy |
| `Assets/Scripts/Game/Demo/DemoConversationIds.cs` | `const string` ids. Game + EditMode tests. No Pixel Crushers types |
| `Assets/Scripts/Game/Net/NetSession` (partial or existing) | Server RPC `StartDemoConversation(string id)` → ClientRpc all seats |

Optional: `Assets/Scripts/Game/Demo/DemoVoice.asmdef` only if the vendor asmdef
forces a split. Prefer Game assembly + vendor asmdef reference.

## Change

| File | Change |
|---|---|
| `HudController.Pause` | A live conversation is not pause. Esc during a **locking** conversation ends the conversation (same as closing a station), it does not open Compendium over it. Esc during an **Assault** subtitle dismisses the subtitle only |
| `ShipController` | While `DemoVoice.LocksInput`, do not open stations; cursor already free |
| `FirstPersonController` | `DemoVoice.LocksInput` uses the same move lock as an open `StationTerminal` |
| `.gitignore` | Do not ignore the Dialogue System folder |

## Delete

Nothing. Do not delete Compendium, mission drawer, or interact prompts.

## Runner contract

`DemoVoice.Play(string id, DemoVoiceMode mode)`

| Mode | When | Input | Cursor | Sim | UI |
|---|---|---|---|---|---|
| `Lock` | Docked (Ship). Bastion **Prep** | Move/look/fire/interact eaten. Conversation UI owns clicks | Unlocked | Ticks | Subtitle + response if the database node has one. Prefer **no** player response nodes for demo lines (advance on timer or Continue) |
| `Subtitle` | Bastion **Assault** | All fight input live | Locked | Ticks | Subtitle only. No Continue that requires a click. Auto-advance from the node’s Sequence time, fallback **4 s** if unset |

`Play` no-ops and logs `[Demo] voice skipped — unknown id=<id>` if the database has
no conversation with that title. Missing database at runtime: log once
`[Demo] voice pipe missing database` and continue the beat’s mechanical gates
(repair still works). The demo may be mute; it may not be stuck.

`Play` while another conversation is live: stop the old one, start the new, log
`[Demo] voice preempted from=<old> to=<new>`.

`PauseGame` / Dialogue System `Time.timeScale` hooks: **off**. If the pack defaults
them on, the runner clears them on `Awake`. A conversation that requires
`timeScale = 0` is refused (`Decisions.md`).

Skin: Dialogue System subtitle panel, or a Shift box already in the HUD family.
Do not add a second HUD family. No `GUILayout` in the Bastion HUD
(`Architecture.md`).

## Multiplayer

Host is the only seat that **decides** a conversation starts (repair completed,
Wave cleared, door opened, Hatch-bank started). Host calls
`NetSession.StartDemoConversationServerRpc` is wrong from a client — host-side
`DemoVoice.PlayLocalAndBroadcast(id, mode)`.

| Seat | What happens |
|---|---|
| Host, solo | `Play` local |
| Host, party | `Play` local + ClientRpc to connected seats with `(id, mode)` |
| Guest | On ClientRpc, `Play` local. Do not re-broadcast |
| Guest join mid-line | No catch-up. Next id plays normally |
| Guest on additive Ship wait (`M57`) | ClientRpc still plays locally on that wait scene |

Do **not** enable Dialogue System’s built-in networking. Conversation state is an
id + mode, not a synced node pointer.

Personal beats (weapon grant, ARMORY repair) play **local only** — other seats do
not hear “you picked up a sidearm” for someone else’s grant. Ship-wide beats (NAV
repair, Hatch-bank convert, door open, Master Control, Hatch finale) broadcast.

`voice.md` ids mark `Local` vs `Party` in the table.

## Conversation ids

Titles in the database **are** these strings. `DemoConversationIds` mirrors them.

| Id | Mode | Scope | Beat | Intent (write the actual lines in the database, not here) |
|---|---|---|---|---|
| `demo.wake` | Lock | Local | 0 | You are on a dead Ship. Move, look, find NAV |
| `demo.nav_repair` | Lock | Party | 0 | NAV is live. Take a weapon. EMBARK is the Hatch |
| `demo.weapon_grant` | Lock | Local | 0 | MK-1 is in hand. ARMORY is still dark |
| `demo.embark_blocked` | Lock | Local | 0 | Named refuse: NAV dark / no weapon |
| `demo.embark_first` | Lock | Party | 0→1 | Quiet Hatch. Find a terminal. Wake the place |
| `demo.explore` | Subtitle | Party | 1 | Optional, Prep, once, if they have not looked at a Field terminal in 45 s |
| `demo.terminal_look` | Lock | Party | 1 | This is a field terminal. `E` select, `T` start. No juice mention |
| `demo.after_wave1` | Lock | Party | 1 | Another terminal opens the next doors. Hatch-bank first |
| `demo.hatch_convert` | Lock | Party | 2 | Convert now, then hold. Prep |
| `demo.hatch_fight` | Subtitle | Party | 2 | Assault start of first Hatch-bank |
| `demo.first_hatch_down` | Lock | Party | 2 | After return to Ship: the bank is already aboard |
| `demo.armory_repair` | Lock | Local | 3 | ARMORY live. Two purchases on this gun |
| `demo.wheel_cap` | Lock | Local | 3 | Third buy refused (also a prompt string) |
| `demo.crew_ready` | Lock | Local | 4 | CREW can be repaired (Wave 2 has started) |
| `demo.crew_repair` | Lock | Local | 4 | Claim Default. `G` Barrage, `H` Rush |
| `demo.embark_wave2` | Lock | Party | 4 | NAV skip to Wave 2. Lead still commits |
| `demo.repeat_unit` | Lock | Party | 5 | Same verbs. Next terminal, then Hatch-bank |
| `demo.door_d1` / `demo.door_d2` / `demo.door_d3` | Subtitle or Lock in Prep | Party | 5 | That door is open |
| `demo.control_room` | Lock | Party | 6 | This room seals. The fight is inside. Not extract |
| `demo.master_control` | Lock | Party | 6 | Prep, on Master look-at / first pull |
| `demo.hatch_finale` | Subtitle | Party | 7 | Door open: get the pile to the Hatch |
| `demo.escaped` | Lock | Party | 7 | Docked after Escaped. DemoActive ends |

Lines are Zo’s to author in the database. The runner must work with placeholder
one-liners (`TODO: demo.wake`) so mechanics can ship before copy pass.

## Driven / Shown / Reads-back

- **Driven by** host (or local-only) `DemoVoice.Play`, never by pause, never by
  Compendium open.
- **Shown** Shift-skinned subtitle (and Continue in Lock mode).
- **Reads-back** log `[Demo] voice id=<id> mode=<Lock|Subtitle> scope=<Local|Party>`.
  A line that played with no log is a defect.

## Logs

Tag `[Demo]`. Never `[Dev]` on a close path.

| Event | Line |
|---|---|
| Start | `voice id=demo.wake mode=Lock scope=Local` |
| End | `voice ended id=demo.wake` |
| Preempt | `voice preempted from=demo.wake to=demo.nav_repair` |
| Missing db | `voice pipe missing database` (once per session) |
| Unknown id | `voice skipped — unknown id=demo.wake` |
| Broadcast | `voice rpc id=demo.nav_repair clientId=<n>` |

## Traps

- Dialogue System `PauseGame` defaults will set `timeScale = 0`. Clear on `Awake`.
- `GUILayout` in a custom DS UI on the Bastion HUD corrupts IMGUI for the session.
- Importing Storyteller.
- Using DS networking and our RPC together — pick ours.
- Locking input during Assault. Wave 1 will kill a reader; that is intended, so do
  not “fix” it by pausing.
- Starting `demo.wake` on a save with `DemoActive == false`.

## Depends / Unblocks

Depends on: owned SKU 11672 available to import. Unblocks: every beat’s lines.
Mechanical gates in beats 0–7 must still function if the database is empty.

## Verification (for the milestone)

1. EditMode: `DemoConversationIds` uniqueness; every beat doc’s id is in the const
   class.
2. PlayMode (solo, Ship): `DemoVoice.Play("demo.wake", Lock)` logs start+end;
   `Time.timeScale == 1` for the duration; move input ignored; Esc ends the
   conversation.
3. PlayMode (Bastion Prep fixture): Lock conversation; sim `Tick` still advances
   `RunElapsed`.
4. PlayMode (Bastion Assault fixture): Subtitle conversation; fire still shoots;
   `timeScale == 1`.
5. Loopback party: host `Play` Party id → guest log `voice rpc` and local start.
6. No `[Dev]` in those logs.

## Rejected

Atreus-style companion (`M62`). Storyteller. `timeScale = 0`. A second pause-as-story
layer. Auto-playing Compendium pages as narration.
