# First Bastion demo

<!--
Purpose: Map of the first-Bastion demo (through Master Control, with multiplayer) and
the shared save/net flags every beat mutates. Beats 0–7 plus voice.md are the
implementable specs. Milestones are not written yet.
Status: demo spec
Authority: implementable — table “Treat as spec” is locked until Zo overrides a row
Update trigger: when a stamp is overridden, when a beat ships, when M70 or M72 closes
Last updated: 2026-08-16
-->

The player is walked from a dead Ship to **Escaped** on the first Bastion. Same verbs
each cycle: find a terminal, start a Wave, Hatch-bank, next doors. Control Room fight
is inside; the haul home is the Hatch. Intensity is not mentioned until this save has
Escaped once.

This folder is what an implementer reads. `Brainstorms/first-session.md` points here.

## Treat as spec until overridden

| Decision | Spec | Why it blocks a grain if reopened |
|---|---|---|
| Convert-then-fight | First Hatch-bank of this save only | Beat 2. Later Hatch-banks keep drip + dump |
| NAV start after that Hatch-bank | **Skip** to `DemoNavUnlockWave`. No 1..N chain while `DemoActive` | Beat 4. `BankCap` must not use the skip wave |
| Starting weapon | Grant **MK-1** only. SEC-D stays a 0-salvage ARMORY buy after ARMORY is powered | Beats 0 and 3 |
| Door sets before Control Room | **3**, from the kit: D1 = T1+T2, D2 = T3, D3 control = T4+T5 | Beat 5. `Tools/bastion-sketch/bastion-defense-layout.txt` |
| Default Protocol | In this demo, after a Wave 2 assault has **started** once | Extra beat on 3/4. `M70` is the claim; this folder gates when CREW lights |
| Friends onboarding | Once per **seat save**. Guests skip the capsule if the Ship’s NAV is already powered. A seat that has already Escaped skips the whole demo | Every beat |
| Voice audience | Host starts a conversation **id**; every seat plays it locally. Assault: subtitles, no input lock | `voice.md` |
| Wheel during demo | At most **2** purchases while `DemoActive` | Beat 3. 150+300 = 450 = Wave 1 kill salvage at 25/kill |
| Juice | Withheld while `DemoActive` (`TryJuice` refuses; prompt omits `+/-`) | Beats 1, 6 |
| FABRICATION | Dark while `DemoActive` | Beats 0, 3, 5 |
| First Hatch-bank down | Any seat down ends the Run (overrides `M49`/`M57` for that one window) | Beat 2 co-op |

## Files

| File | Player-visible change |
|---|---|
| [`voice.md`](voice.md) | Dialogue System in the tree; one runner; Ship/Prep can lock input; Assault cannot |
| [`0-wake-on-a-dead-ship.md`](0-wake-on-a-dead-ship.md) | Capsule; repair NAV; grant MK-1; other consoles dark |
| [`1-first-terminal.md`](1-first-terminal.md) | Explore → one field terminal → Wave 1 Defaults; no juice; Hatch-bank next |
| [`2-first-hatch-bank.md`](2-first-hatch-bank.md) | Convert then fight; designed loss at 0 upgrades; banked Salvage on the Ship |
| [`3-ship-after-first-loss.md`](3-ship-after-first-loss.md) | Repair ARMORY; two wheel buys; CREW/FAB still dark |
| [`4-embark-at-wave-2.md`](4-embark-at-wave-2.md) | NAV offers Wave 2 because that Hatch-bank **started**; skip, not chain |
| [`5-repeat-to-control-room.md`](5-repeat-to-control-room.md) | Same unit through D1, D2, D3; later Hatch-banks may be survived |
| [`6-master-control.md`](6-master-control.md) | Room fight inside a sealed Control Room. Not the win |
| [`7-hatch-finale.md`](7-hatch-finale.md) | Door opens; defend Salvage at the Hatch; survive → Escaped |

Default Protocol lines live in beat 3 (CREW stays dark until Wave 2 has started) and
beat 4 (the claim fires on the next Docked). Do not cut a tenth milestone for it unless
the queue is empty.

## How to cut milestones

Do not write milestone files from this folder until asked. When asked, one grain per
file, gate from that beat’s **Verification** section, Implementation from **Create /
Change / Delete**. `Docs/_meta/_definitions/milestone.md` is the scaffold.

Suggested execution order (IDs assigned at cut time):

1. Voice pipe (`voice.md`). Multiplier: every beat’s lines.
2. Dead Ship (0).
3. First cycle (1 + 2 in one milestone if the gate can still be binary; split if
   convert-then-fight slips).
4. After first loss (3 + 4). Checkpoint + skip + `BankCap` in the **same** change.
5. Repeat to Control Room (5).
6. Master Control + Hatch finale copy (6 + 7). `M54` sequence stays; this is send and
   juice-hide, not a third win.

`M72` (shared Ship, criterion 9 two-machine) stays ahead of any friends close of this
demo. Solo can close beats without it. `M70` (CREW claim) must be in tree for the
Protocol beat; do not rebuild the shop.

Lane cap (`Working-Rules.md`): at 10 files in `Milestones/1. planned/`, stop cutting
and build one.

## Shared flags (SaveVersion 9)

Add fields on `SaveData` / `MetaState`. Migration: missing keys → demo-on for a new
campaign (`DemoActive = true`), demo-off for a save that already has `CleanExits >= 1`
or `DeepestWaveReached > 1` (they have already played; do not trap them in the capsule).

| Field | Default | Who writes it | Who reads it |
|---|---|---|---|
| `DemoActive` | `true` | Set `false` on first `Escaped` (beat 7) | Juice, FAB, wheel cap, NAV skip, first-hatch rules |
| `DemoNavPowered` | `false` | Host, beat 0 repair. Replicated on `NetPartyLobby` | Every seat: NAV live vs dark. EMBARK refuse |
| `DemoWeaponGranted` | `false` | Local seat, beat 0 grant | That seat’s EMBARK refuse; skip grant |
| `DemoArmoryPowered` | `false` | Local seat, beat 3 repair | That seat’s ARMORY open vs dark |
| `DemoCrewPowered` | `false` | Local seat, after `DemoWave2Seen` and CREW repair | That seat’s CREW open vs dark |
| `DemoFabPowered` | `false` | Stays false while `DemoActive`. Set true with `DemoActive` false | FAB open vs dark |
| `DemoFirstHatchConvertDone` | `false` | Beat 2, on first Hatch-bank `TriggerDraw` | Convert-then-fight once |
| `DemoFirstHatchSpecUsed` | `false` | Beat 2, first Hatch-bank launch | Hunter-add spec once |
| `DemoWave2Seen` | `false` | Any Wave 2 assault started (field or skip start) | Lights CREW repair |
| `DemoNavUnlockWave` | `1` | Hatch-bank **start** (non-Master): `max(field, CurrentWaveIndex+1)` | NAV picker max while `DemoActive` |
| `DemoBankCapWave` | `1` | Same Hatch-bank start: `max(field, CurrentWaveIndex)` | `HatchBank.BankCap` while `DemoActive` and `PlayerCount > 1` |
| `DemoWheelBuysLeft` | `2` | Decrement on `BuyWheelUpgrade` while `DemoActive` | ARMORY refuse at 0 |

`DeepestWaveReached` stays “highest field Wave **cleared**.” It does **not** become the
NAV cap while `DemoActive`. Guest wallets’ `Deepest(slot)` used by `BankCap` must track
`DemoBankCapWave` on Hatch-bank start, not the skip wave.

Ship-wide vs per-seat: `DemoNavPowered` is the hull (host Meta + lobby
`NetworkVariable<bool>`). Weapon, ARMORY, CREW, wheel, `DemoActive` are that seat’s
`GameSession.Meta` (`M65`, `M72` criterion 6 — personal shopping).

## Multiplayer bar (every beat)

- Host-authoritative `RunSim`. Client-authoritative own movement. `[Net]` logs, never
  `[Dev]`, on a close path.
- Party lead owns NAV commit (`M72` c4). Guests Ready + 5 s countdown (c5). Join during
  countdown refused.
- Lead’s `PendingStartWave` drives the Run (c6). Guests’ `DemoNavUnlockWave` does not.
- Personal wallets (`M65`). After beat 4, `BankCap` keys `DemoBankCapWave` or guests
  bank a Wave they have not converted.
- After the first demo Hatch-bank, guest down waits on Ship (`M57`); host/solo down
  ends the Run (`M49`). During that first Hatch-bank only, any seat down ends the Run.
- Voice: `voice.md`. Do not use Dialogue System’s own NGO layer.

`M72` header (2026-08-16): host saw guest, guest did not see host. Criterion 9 open.
A friends close of this demo cannot claim green on that gap.

## After first Escaped

`DemoActive = false`. Juice returns. FAB lights. Wheel uncapped. NAV chain from
`The-Ship.md` returns (`StartRun(N)` delivers 1..N). Convert is drip + dump.
`M49`/`M57` already restored after beat 2. Capsule never runs again on that save.

## Rejected (do not rebuild)

Companion narrator (`M62`). Per-second siphon (`Decisions.md`). CREW locked behind
death (`Decisions.md`). Pause that sets `Time.timeScale = 0`. Storyteller asset.
A third win condition besides `M54`’s Hatch-bank half. Unity Lobby service.

## In-tree facts the beats assume

Wave 1 (`WaveDirector.NextWave(0, 0, 1)`): 18 Defaults, 0 Hunters, 0 Thieves, HP mul
1.0. Salvage 25/kill → 450 Unbanked if every body pays. Secondary wheel: 150 then 300
(`WeaponTier.Cost(Secondary, 0..1)`). Hatch-bank window 45 s, drip 20/s, slots 3.
Player 100 HP. Swing clock 0.7 s. Hatch 300 HP at Hatch Armor 0.

Kit graph: `bastion-defense-layout.txt` lines 6–8 and 70–73.
