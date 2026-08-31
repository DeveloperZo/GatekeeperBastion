# 5 — Deeper, until Master Control

<!--
Purpose: The evening's back half. Repeat Hatch-start → terminal-shutoff →
restock until D3 is open, then Master Control: five control-room Waves, the
elite, the switch. Escaped ends the Boarding, not a co-op-demo flag.
Status: working
Authority: implementable against 0.34.2
Update trigger: when Master Control's sequence changes, when a boss kind is
specced, when DemoZones moves
Last updated: 2026-08-30 (T2→D1, T5→D3; Master starts at the Hatch;
elite then switch, not a Convert-keyed boss stretch)
Depends: voice-coop.md, 4-restock-and-go-back.md, ../Play Mechanics/Master-Control.md
-->

## Why this beat exists

**Progress through the bastion is incremental** (Zo, 2026-08-20). The party
does not make one deep run. They start at the Hatch, shut off a terminal, come
home, restock, and go further.

There is **no dramatic split** and **no co-op boss kind**. The live finale is
Master Control: five Terminal Waves in the control room, then one oversized
Thief, then `T` at the Master terminal. Convert and dock are refused for the
whole sequence.

## Functional storyboard

Continues from beat 4. D3 is open (`ControlRoomReachable`).

### 0 — Further each time

| | |
|---|---|
| **Goal** | Feel the run extending. |
| **Player** | Embark, `T` at the Hatch, shut off the live terminal, dock or Convert, restock. Repeat until D3 is open. |
| **Game** | — |
| **Standing** | Get past where you stopped |
| **Prompt / HUD** | Existing Hatch start line and terminal shutoff. |
| **Log** | existing wave / shutoff / door-open lines |
| **Have** | Doors open on shutoff (`OpenEligibleDoors`) and persist on the realm. Deepest-entered rises on every draw. |
| **Build** | `[Demo] deepest reached=<n> attempt=<n>` if a played evening needs a reader. |

### 1 — The control room is open

| | |
|---|---|
| **Goal** | Know the finale is the end and that this kit is what is fighting it. |
| **Player** | D3 open. Hatch start line offers Master (`HatchOffersWaveStart` && `ControlRoomReachable`). |
| **Game** | `demo.coop.boss_ahead` · Lock (Prep) · Party · Game. **Whatever is through there is the last of it. This is the kit you are taking in.** |
| **Standing** | Take Master Control |
| **Log** | existing Master-start line |
| **Have** | `T` at the Hatch starts `StartMasterControl`. The Master terminal itself refuses to start — it is the switch at the end. Convert and dock refused while the sequence is live. |
| **Build** | The line. |

### 2 — The sequence

| | |
|---|---|
| **Goal** | Five control-room Waves, then the elite, then the switch. The Hatch is still live (scaled field-Wave damage). |
| **Player** | Fight it. Killing the elite arms the Master terminal; it does not end the Boarding. `T` at Master throws the switch. |
| **Game** | — |
| **Standing** | Take Master Control |
| **Prompt / HUD** | Master terminal prompt after the elite is down. `MsgPlayer` Hatch damage still names the door. |
| **Log** | `master-control elite — kill it to arm the terminal`; `master shutoff banked N` |
| **Have** | `MasterControl.TerminalWaves = 5`. Elite: one Thief, `FinaleEliteHp = 240`, `FinaleEliteScale = 1.5`, no escorts. Control-room door is **not** sealed. Field trickle off during the sequence. Kills bank on landing. |
| **Build** | A boss kind with its own behaviour is unspecced. `GatekeeperKind` is Default, Hunter, Thief, Cloaker. |

### 3 — Escaped

| | |
|---|---|
| **Goal** | End as a party. |
| **Player** | Switch thrown. Carry banks on the spot. Screen BASTION STRIPPED. RETURN TO SHIP. |
| **Game** | `demo.coop.escaped` · Lock (Docked) · Party · Game. **Clear. You both got out with what you moved.** |
| **Standing** | — (Boarding over; realm persists) |
| **Log** | `CompleteRun` → Escaped, `CleanExits++` |
| **Have** | Escaped is a party result: a seat waiting on the Ship still escapes. There is no `DemoCoopActive` to clear. A loss here does not wipe the realm; the party restocks and comes back. |
| **Build** | The line, and `[Demo] co-op demo complete` only if a co-op-demo flag is ever added. |

## Acceptance shape

Player action: two players open D3, start Master at the Hatch, kill the elite,
throw the switch, reach Escaped. Reads back: elite log, master shutoff, clean
exit.

Reaching the finale only after restock trips is the evening's shape, not a
gate — a strong kit can push T2 and T5 in one Boarding.

## Verification

The net fixture can hold: D3 keys on T5 shutoff, Master start requires
`ControlRoomReachable`, Escaped fires from `ShutOffMasterControl`, a waiting
seat still escapes. Whether the climb feels like progress is eyes-on.

## Gaps

- **The boss is unspecced.** The live elite is a scaled Thief with no behaviour
  of its own. This beat cannot close a "boss" promise without a real spec.
- Trip count before Master Control is a tune against the pot's fill rate
  (kills bank on landing, so losses still pay). Needs a played evening.
- Three-seat behaviour is untested by construction: this evening authors for
  two and the rules must not break at three.
- Buy-back during Master Control is the ordinary bank-priced rule; Convert is
  refused for the sequence, so there is no window-gated return.
