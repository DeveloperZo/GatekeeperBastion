# Voice pipe

<!--
Purpose: Demo mission dialogue — on-screen lines the game (and at most a few
guides) speak. No VO. No player replies. Runner, net RPC, and line ids every
beat starts.
Status: demo spec
Authority: implementable
Update trigger: when a speaker is named, when a line id is added, when
input-lock rules change
Last updated: 2026-08-26 (demo.ship_lost re-keyed to the one-deck hull — the nudge points at
the NAV MAP, not at a stair. Earlier: 2026-08-25 (the pipe and the copy shipped;
Convert/Ante/Boarding in the intents — language.md trap 8; zone_retrigger and dock_option
added to the registry))
-->

There is no VO in the tree and none is required for this demo. Pause does not
stop the sim (`Time.timeScale` is off the table on the Bastion: `Decisions.md`,
`HudController.Pause`). The 2026-08-17 `demo.intruders` `timeScale = 0`
exception is **withdrawn** (Zo, 2026-08-17): no line pauses. Beat 0's pause is
**gated execution** — the caller holds the gated step (the intruder release)
until the line's Continue; the sim keeps ticking, sim-safe. Compendium is a
pause encyclopedia (`M62`), not a director. Interact prompts stay mechanical. The Bastion mission drawer (Tab) is
fight chrome and may show the **standing objective**; it is not a conversation
UI.

## Player-visible change

The game tells the Boarder what the current job is, as text. Repairing NAV,
looking at a terminal, starting a Convert — each fires a **line id**. Ship
and Bastion Prep may lock move/look so they can read (same lock as an open
station). Assault shows the line as a subtitle only — look, move, and fire stay
live.

**The player never talks back.** Continue (Lock) and auto-advance (Subtitle)
dismiss or step the line. There are no reply choices.

**Speakers this iteration:** the **game** (default — mission / system text) and
**at most a few named guides**. A guide is a speaker label on a line, not a companion
who follows or a station you converse with. A fourth named guide replaces one
of the existing three; do not grow the cast. Names are stamped here when chosen.
Until then every id is speaker **Game**.

No VO clips. Missing copy does not block mechanical gates (repair still works).

## Asset

Do **not** buy or import Pixel Crushers Dialogue System (SKU 11672, $95). Do
**not** import HeneGames Dialogue System 1.1.4 (SKU 248969). Do **not** import
Storyteller.

Shift is the HUD family. Ship uses the existing prompt / a Shift box. Bastion
uses `MsgPlayer` / `MsgGeneral` and may push the standing objective into the
mission drawer. No second HUD family. No `GUILayout` in the Bastion HUD
(`Architecture.md`).

## Create

| Path | Role |
|---|---|
| `Assets/Scripts/Game/Demo/DemoLines.cs` | **Shipped**, as static code rather than an asset: id → speaker, mode, scope, copy, with one author and nothing per-instance to drift |
| `Assets/Scripts/Game/Demo/DemoVoice.cs` | **Shipped**. LOCAL-OWNER runner. `Play(id, mode)` and `PlayLocalAndBroadcast(id, mode)`. Owns input-lock policy. No vendor types |
| `Assets/Scripts/Game/Demo/DemoConversationIds.cs` | **Shipped**. `const string` ids plus an `All` array the harness audits `DemoLines` against |
| `NetRunMirror.DemoLineClientRpc` | **Shipped**, and it is the broadcast leg — not a `NetSession` partial. The starting seat has already played locally |

`GatekeepersProtocol.Sim` must not reference UI or line assets (`Architecture.md`).

## Change

| File | Change |
|---|---|
| `HudController.Pause` | A live **Lock** line is not pause. Esc ends the line (same as closing a station). Esc during an **Assault** subtitle dismisses the subtitle only |
| `ShipController` | While `DemoVoice.LocksInput`, do not open stations; cursor already free |
| `FirstPersonController` | `DemoVoice.LocksInput` uses the same move lock as an open `StationTerminal` |
| Bastion mission drawer | Optional: standing objective string for the live id while `DemoActive`. Tab still toggles. Not a reply surface |

## Delete

Nothing. Do not delete Compendium, mission drawer, or interact prompts.

## Runner contract

`DemoVoice.Play(string id, DemoVoiceMode mode)`

| Mode | When | Input | Cursor | Sim | UI |
|---|---|---|---|---|---|
| `Lock` | Docked (Ship). Bastion **Prep** | Move/look/fire/interact eaten. Continue advances | Unlocked | Ticks | Shift box. **No reply row** |
| `Subtitle` | Bastion **Assault**; the beat-0 Ship fight | All fight input live | Locked | Ticks | Subtitle / `MsgPlayer` only. Auto-advance **4 s** if the line has no duration |

`demo.intruders` is a plain Lock (Zo, 2026-08-17 — the paused variant is
withdrawn): beat 0 gates the intruder **release** on that line's Continue
(gated execution, not pause). The sim ticks throughout.

`Play` no-ops and logs `[Demo] line skipped — unknown id=<id>` if the table has
no row. Missing table at runtime: log once `[Demo] line table missing` and
continue. The demo may be mute; it may not be stuck.

`Play` while another line is live: stop the old one, start the new, log
`[Demo] line preempted from=<old> to=<new>`.

Same-frame Docked collision (Zo, 2026-08-17): if `demo.first_hatch_down` and
any other Lock line start on the same Docked frame, `demo.first_hatch_down`
plays first and the other line is **queued** to start on its Continue — not
preempted. This is the only queue; every other collision preempts as above.

A line that requires `timeScale = 0` is refused (`Decisions.md`) — no
exceptions. The 2026-08-17 `demo.intruders` exception is withdrawn (Zo,
2026-08-17); its caller gates execution instead.

## Multiplayer

Multiplayer is locked until a save's first wheel purchase (beat 3,
[`README.md`](README.md)), so every line in beats 0–2 plays on a solo seat —
`Party` scope only matters from `demo.mp_unlocked` on.

Host is the only seat that **decides** a line starts (repair completed, Wave
cleared, door opened, Convert started). Host-side
`DemoVoice.PlayLocalAndBroadcast(id, mode)`.

| Seat | What happens |
|---|---|
| Host, solo | `Play` local |
| Host, party | `Play` local + ClientRpc to connected seats with `(id, mode)` |
| Guest | On ClientRpc, `Play` local. Do not re-broadcast |
| Guest join mid-line | No catch-up. Next id plays normally |
| Guest on additive Ship wait (`M57`) | ClientRpc still plays locally on that wait scene |

Conversation state is an id + mode, not a synced node pointer. No vendor net.

Personal beats (weapon grant, wheel cap, protocol claim) play **local only**.
Ship-wide beats from beat 4 on (retrigger embark, door open, finale battle,
final Convert) broadcast.

Ids mark `Local` vs `Party` in the table below.

## Line ids

`DemoConversationIds` mirrors these strings and `DemoLines` carries the copy;
neither is a placeholder any more, so a row here that disagrees with those two
is this file being wrong. Every row below is in `DemoConversationIds.All`.

| Id | Mode | Scope | Beat | Intent |
|---|---|---|---|---|
| `demo.wake_stir` | Lock | Local | 0 | Fade finished, still sealed. Text before the unlock prompt |
| `demo.wake` | Lock | Local | 0 | Lid open. Dead Ship. Find NAV, hold Interact to repair |
| `demo.ship_lost` | Subtitle | Local | 0 | Lost-player net (2026-08-25): 45 quiet s before the first NAV repair. Its job is to point them at the NAV MAP, forward on the port bow bulkhead. Re-keyed 2026-08-26 with the one-deck collapse — there is no stair and no upper deck to route them through, so the nudge is a bearing along one floor |
| `demo.nav_repair` | Lock | Local | 0 | NAV live. Route power from the ship terminal — select, then start |
| `demo.armory_call` | Lock | Local | 0 | Armory recut (2026-08-25): terminal start points at the rack — power routed, ARMORY live, take the MK-1 |
| `demo.mp_unlocked` | Lock | Local | 0 | MOVED here from beat 3 (2026-08-25): the ARMORY's MK-1 is the multiplayer key — armed, and the Ship answers hails |
| `demo.intruders` | Lock | Local | 0 | Retired at the armory recut (2026-08-25): the ship fight is struck — no caller; row survives so the registry stays whole |
| `demo.weapon_grant` | Subtitle | Local | 0 | Retired at the terminal-grant cut (2026-08-18) and has no caller; the row survives so the registry stays whole |
| `demo.ship_clear` | Lock | Local | 0 | Clear. CREW heals; then NAV, EMBARK |
| `demo.embark_blocked` | Lock | Local | 0 | Named refuse: terminal not started / no weapon / intruders alive |
| `demo.embark_first` | Lock | Local | 0→1 | Quiet Hatch. Find a terminal. Wake the place |
| `demo.explore` | Subtitle | Local | 1 | Optional, Prep, once, if they have not looked at a Field terminal in 45 s |
| `demo.terminal_look` | Lock | Local | 1 | Field terminal. `E` selects, `T` starts a Wave, and that Wave hunts the Boarder rather than the Hatch |
| `demo.juice_teach` | Lock | Local | 1 | Once Unbanked > 0 (second terminal look): `+/-` ante up out of Unbanked; more bodies, more Salvage; spending nothing is also a bet. Id keeps the code's word |
| `demo.after_terminal` | Lock | Local | 1 | Zone terminals done. Convert next. Pile transfers while you hold |
| `demo.hatch_bank_teach` | Lock | Local | 2 | Drip; if the Hatch falls, what has not moved is gone. Id keeps the code's word |
| `demo.hatch_fight` | Subtitle | Local | 2 | Assault start of the first Convert |
| `demo.first_hatch_down` | Lock | Local | 2 | First lost Boarding's Docked (any cause — copy is cause-agnostic): what dripped is aboard. Spend it at the ARMORY |
| `demo.mp_unlocked` | Lock | Local | 3 | First wheel purchase made. Friends can join now |
| `demo.wheel_cap` | Lock | Local | 3 | Cap refuse on a third-buy attempt (also a prompt string). Exit standing is state-keyed (no buyable upgrades remain), not this line |
| `demo.crew_ready` | Lock | Local | 4 | CREW menu is open now (unlocked on the first Docked after the first Convert — re-keyed 2026-08-18 with the wheel key). Protocol waits |
| `demo.crew_claim` | Lock | Local | 4 | Claim Default. `G` Barrage, `H` Rush |
| `demo.embark_wave2` | Lock | Party | 4 | Post-loss EMBARK: no wave skip — the zone's deepest terminal retriggers the composite (partial start electable). Lead still commits. Id is stale to its content; rename is Zo's (beat 4, Gaps) |
| `demo.zone_retrigger` | Lock | Party | 4–5 | First retrigger offer per zone: run the whole zone again, or start deeper and finish what is left |
| `demo.dock_option` | Subtitle | Party | 4–5 | After a survived Convert window: hold at the Hatch to go home Docked, or push on |
| `demo.repeat_unit` | Lock | Party | 5 | Same verbs. Next zone's terminals, then its Convert; the door opens when that Convert survives |
| `demo.second_loss` | Lock | Local | 5 | Docked after zone 2's Convert **resolves** — held or lost (Zo, 2026-08-25): what transferred is aboard, and the rack has heavier hardware now. **Trigger is `DemoZone2BankLost`, never `PodEjections >= 2`** (recut 2026-08-19). The owed copy restamp landed; the shipped line opens "Zone 2 is settled" and asserts no ejection count |
| `demo.door_d1` / `demo.door_d2` | Lock in Prep | Party | 5 | That door is open. One string serves both |
| `demo.door_d3` | — | — | 5 | Never plays: D3 open fires `demo.control_room` instead (beat 6 merge) |
| `demo.control_room` | Lock | Party | 6 | This room seals. The fight is inside. Not extract |
| `demo.master_control` | Lock | Party | 6 | Prep, on Master look-at / first pull |
| `demo.hatch_finale` | Subtitle | Party | 7 | Door open: get the pile to the Hatch |
| `demo.escaped` | Lock | Party | 7 | Docked after Escaped. DemoActive ends |

`demo.door_d3` is deliberately absent from `DemoConversationIds`: D3 open fires
`demo.control_room` instead, and a dead id in that class would read as a line
somebody should wire.

Copy for every beat is stamped in that beat's file (0–7); this table is the id
registry, not the copy source. `DemoLines` is the runtime table and now carries
real copy for every row. Beats 6–7 rows above are cross-references only — those
files are another editor's and carry their own re-ruled shape.

## Driven / Shown / Reads-back

- **Driven by** host (or local-only) `DemoVoice.Play`, never by pause, never by
  Compendium open.
- **Shown** Shift box (Lock) or subtitle / `MsgPlayer` (Subtitle). Continue is
  advance, not a reply.
- **Reads-back** log `[Demo] line id=<id> mode=<Lock|Subtitle> scope=<Local|Party> speaker=<Game|guide>`.
  A line that played with no log is a defect.

## Logs

Tag `[Demo]`. Never `[Dev]` on a close path.

| Event | Line |
|---|---|
| Start | `line id=demo.wake mode=Lock scope=Local speaker=Game` |
| End | `line ended id=demo.wake` |
| Preempt | `line preempted from=demo.wake to=demo.nav_repair` |
| Queue (Docked collision) | `line queued id=<id> behind=demo.first_hatch_down` |
| Missing table | `line table missing` (once per session) |
| Unknown id | `line skipped — unknown id=demo.wake` |
| Broadcast | `line rpc id=demo.nav_repair clientId=<n>` |

## Traps

- Importing a dialogue framework for ~25 one-way lines.
- Player reply UI “so they feel involved.”
- A companion who walks with them (`M62`).
- `timeScale = 0` anywhere — the 2026-08-17 `demo.intruders` exception is
  withdrawn; beat 0 gates the intruder release on Continue instead.
- Preempting `demo.first_hatch_down` with another Docked line — it wins the
  same-frame collision; the other line queues.
- `GUILayout` on the Bastion HUD.
- Locking input during Assault.
- Starting `demo.wake_stir` / `demo.wake` on a save with `DemoActive == false`.
- Unlock prompt on black, or `demo.wake` before the lid opens.

## Depends / Unblocks

Depends on: Shift HUD already in tree. Unblocks: every beat’s lines.
Mechanical gates in beats 0–7 must still function if copy is `TODO`.

## Verification (for the milestone)

1. EditMode: `DemoConversationIds` uniqueness; every beat doc’s id is in the const
   class; no line asset field named a player reply.
2. PlayMode (solo, Ship): `DemoVoice.Play("demo.wake", Lock)` logs start+end;
   `Time.timeScale == 1`; move input ignored; Esc ends the line; no reply control.
3. PlayMode (Bastion Prep fixture): Lock line; sim `Tick` still advances
   `RunElapsed`.
4. PlayMode (Bastion Assault fixture): Subtitle line; fire still shoots;
   `timeScale == 1`.
5. Loopback party: host `Play` Party id → guest log `line rpc` and local start.
6. Same-frame Docked collision fixture: `demo.first_hatch_down` + another Lock
   start on one frame → loss line plays first, `line queued` logged, queued
   line starts on Continue.
7. No `[Dev]` in those logs.

## Rejected

Player reply / branching. VO for this demo. Pixel Crushers 11672. HeneGames
248969. Storyteller. Atreus-style companion (`M62`). `timeScale = 0` (the
2026-08-17 `demo.intruders` exception is withdrawn). A second
pause-as-story layer. Auto-playing Compendium pages as narration.
