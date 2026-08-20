# First Bastion demo

<!--
Purpose: Map of the first-Bastion demo (through the finale, with multiplayer) and
the shared save/net flags every beat mutates. Beats 0–7 are functional storyboards —
the implementable specs — plus voice.md. Milestones cut 2026-08-17: M74–M80.
Status: demo spec
Authority: implementable — table “Treat as spec” is locked until Zo overrides a row
Update trigger: when a stamp is overridden, when a beat ships, when M72 closes
Last updated: 2026-08-19
-->

The player is walked from a dead Ship to **Escaped** on the first Bastion. The
loop unit (Zo, 2026-08-17): a zone's terminal Waves, then that zone's
Hatch-bank Wave. Surviving the bank's timer window unlocks the zone's door; a
failed bank **never** unlocks it; the bank is locked until the zone's terminals
are complete. `MaxWaves` caps **consecutive Waves per terminal trigger**, not
per Run. The finale is a 5-consecutive-Wave terminal battle, then **one** final
bank window with the first boss/elite; the haul home is the Hatch. Intensity is
taught on the first terminal cycle (once Unbanked > 0) and stays live.
**Multiplayer is locked until that save's first wheel purchase (beat 3)** —
every seat solos beats 0–3 by construction.

This folder is what an implementer reads. `Brainstorms/first-session.md` points here.

## Treat as spec until overridden

| Decision | Spec | Why it blocks a grain if reopened |
|---|---|---|
| Convert-then-fight | **Overturned** (2026-08-16 play). Live drip; overrun wipes remainder | Beat 2 |
| Loop unit | Zone terminal Waves → Hatch-bank Wave (Zo, 2026-08-17). Surviving the bank's timer window unlocks the zone's door; a failed bank **never** unlocks; the bank is locked until the zone's terminals are complete | Beats 1–7 |
| MaxWaves | Caps **consecutive Waves per terminal trigger**, not per Run (Zo, 2026-08-17). Zone 1 triggers are 1 Wave; zone 2's terminal 3; the finale terminal 5 | Beats 1, 5–7 |
| Demo shape | Zone 1 = **2** terminals × **1** Wave each + bank → D1; zone 2 = **1** terminal × **3** consecutive Waves + heavier bank → D2; finale = **5**-consecutive-Wave terminal battle then **one** final bank window with the first boss/elite (Zo, 2026-08-17). Finale kit mapping `[unconfirmed]` | Beats 1–7. Loss 2 and the finale are another editor's files |
| First terminal | Re-ruled (Zo, 2026-08-17): zone 1 = two 1-Wave terminals, untimed Prep, juice taught once Unbanked > 0, bank after both | Beat 1 |
| NAV after that Hatch-bank | **Dead** (Zo, 2026-08-17). No NAV skip; refight-avoidance lives at the terminal: post-loss the zone's **deepest terminal retriggers the composite** by default, partial start electable | Beat 4. v9 skip fields re-key or die (Shared flags) |
| Starting weapon | **MK-1** off the bare ARMORY rack, mid beat-0 fight, no panel. SEC-D stays a 0-salvage buy when the panel unlocks | Beats 0 and 3 |
| Beat 0 fight | Ship terminal (E/T, Bastion pattern) powers ARMORY + spawns **2** weak Gatekeepers; gated Lock for the pickup (intruder release gated on the line's Continue — **no** `timeScale = 0`; Zo, 2026-08-17); HP floor **1**; CREW heals on interact, **no menu** | Beat 0. First menu of the demo is NAV's picker |
| Two authored losses | The unlock spine (Zo, 2026-08-17). Loss 1 = zone 1's first bank: authored overwhelm vs the no-upgrade kit ceiling (aim ~2k Unbanked at start, ~500 banked when the Hatch falls; tunable) → wheel + Secondary buys + multiplayer on first purchase. Loss 2 = zone 2's banked Wave (another editor's files) → Primary + 2 upgrades granted outside the count. Both windows are **unclearable** by design | Beats 2–3 + loss 2's beat |
| Ship return after a survived bank | Held interact at the Hatch, available until the next Wave is triggered; ends the Run **Docked**, not Lost; banked + door state persist via the successful-bank snapshot (Zo, 2026-08-17). Never skips the unlock deaths — both authored losses are unclearable windows | Beats 2, 5+. Wheel key re-keyed to the first bank — `HatchConversions >= 1` (Zo, 2026-08-18; `Decisions.md`, *the wheel opens on the first bank*) |
| Co-op buy-back | A dead player may buy back in **only while a bank window is live**, rejoining at the Hatch, paying from their **own** banked salvage (cost tunable) (Zo, 2026-08-17) | Beats 4+. Open: dead-host vs `M49` (Gaps) |
| Doors | Unlock on **zone bank success** (Zo, 2026-08-17): D1 = zone 1's bank, D2 = zone 2's, then the finale's single final bank | Beats 2, 5–7 |
| Default Protocol | CREW **menu** unlocks on **first Docked after first loss** (re-keyed off Wave-2-seen; Zo, 2026-08-17); CREW heals on interact from beat 0 | Beats 3–4. `M70` is the claim |
| Multiplayer gate | **Locked until that save's first wheel purchase** (`DemoWheelBuysLeft < 2 \|\| !DemoActive`). F10/F11 and lobby entry refuse with a named log. Beats 0–3 are solo for every save | Every beat. Kills guest capsule-skip rules |
| Voice audience | Host starts a **line id**; every seat plays it locally. No player replies. Assault: subtitles, no input lock | `voice.md` |
| Wheel during demo | A **ladder** (Zo, 2026-08-17): at most **2** Secondary purchases while `DemoActive` after loss 1; at loss 2 a **Primary + 2 upgrades are granted outside that count**. The loss-1 aim (~500 banked, tunable) funds both buys | Beat 3 + loss 2's beat. Do not gift Salvage |
| Beat 3 exit standing | Fires when **no buyable upgrades remain** (state-keyed; replaces the third-refuse trigger) (Zo, 2026-08-17) | Beat 3 |
| Juice | **Taught** after first Wave 1. Live after that, including the finale | Beat 1. Withhold stamp overturned |
| FABRICATION | Dark while `DemoActive` | Beats 0, 3, 5 |
| First Hatch-bank fail | Hatch 0 or host down — authored loss 1 either way; the first-loss line's copy is cause-agnostic (beat 2). `M49`/`M57` apply (any-seat-down overturned) | Beat 2 |

## Files

| File | Player-visible change |
|---|---|
| [`voice.md`](voice.md) | Mission dialogue, no VO, no player replies; Shift runner; Ship/Prep can lock input; Assault cannot; no line pauses (`timeScale` exception withdrawn 2026-08-17) |
| [`0-wake-on-a-dead-ship.md`](0-wake-on-a-dead-ship.md) | Play-through: fade, stir, unlock capsule, NAV, ship terminal, 2 Gatekeepers (release gated on the line's Continue), MK-1, CREW heal, EMBARK |
| [`1-first-terminal.md`](1-first-terminal.md) | Explore → zone 1's two field terminals, one Wave each; teach juice; Hatch-bank next |
| [`2-first-hatch-bank.md`](2-first-hatch-bank.md) | Authored loss 1: live drip (slowed); overwhelm at the Hatch; leftover Unbanked wiped. Survived banks (later zones) unlock doors and allow Ship return |
| [`3-ship-after-first-loss.md`](3-ship-after-first-loss.md) | ARMORY panel + two Secondary buys (ladder rung 1); first buy unlocks multiplayer; state-keyed exit standing; CREW menu unlock lands on this Docked; FAB dark |
| [`4-embark-at-wave-2.md`](4-embark-at-wave-2.md) | Post-loss EMBARK; the zone's deepest terminal retriggers the composite (partial start electable) — no NAV skip |
| [`5-repeat-to-control-room.md`](5-repeat-to-control-room.md) | Zone 2: one terminal, 3 consecutive Waves, heavier bank → D2 (loss 2 — that editor's spec); later banks may be survived |
| [`6-master-control.md`](6-master-control.md) | Finale terminal battle — 5 consecutive Waves (re-ruled 2026-08-17; that editor's file) |
| [`7-hatch-finale.md`](7-hatch-finale.md) | **One** final bank window with the first boss/elite; survive → Escaped (that editor's file) |

Default Protocol lines live in beat 4 (`demo.crew_ready`, `demo.crew_claim`);
until the re-keyed unlock (first Docked after first loss) CREW is heal-only.
Do not cut a tenth milestone for it unless the queue is empty.

## How to cut milestones

**Cut 2026-08-17** — IDs assigned: 1 → `M74`, 2 → `M75`, 3 → `M76`, 4 → `M78`,
5 → `M79`, 6 → `M80`, plus **`M77`** (Ship return / bank-keyed doors / Docked run
end), split out of cut 4 at Zo's ruling because it is the run-structure piece the
whole game keeps. Execution order M74 → M75 → M76 → M77 → M78 → M79 → M80.

The original guidance, kept as the record: one grain per
file, gate from that beat’s **Verification** section, Implementation from **Create /
Change / Delete**. `Docs/_meta/_definitions/milestone.md` is the scaffold.

Suggested execution order (IDs assigned at cut time):

1. Mission dialogue pipe (`voice.md`). Multiplier: every beat’s lines. No VO pack.
2. Dead Ship (0).
3. First cycle (1 + 2). Drip 8/s and MK-1 30 are already in tree (tuning
   sequence below); what remains is the `FirstTerminal` re-spec to the zone
   shape, the authored-overwhelm tune, voice, gates and the re-keyed checkpoint.
4. After first loss (3 + 4). Exit-standing state key + composite retrigger +
   the re-keyed checkpoint in the **same** change.
5. Zone 2 to D2 (5).
6. Finale (6 + 7) — re-ruled 2026-08-17: 5-consecutive-Wave terminal battle,
   then one final bank window with the first boss/elite; those files are
   another editor's. `M54` sequence stays; this is not a third win. Juice stays
   live. Resolve the fight-spawn gap (beat 6) before cutting.

`M72` closed 2026-08-17 (shared Ship, Relay dual-box). Friends demo close still
needs a five-wave session; solo can close beats without friends. The CREW claim
path (select → sheet → `BuyProtocol`) is already in tree; `M70` formalized it.
Do not rebuild the shop.

Lane cap (`Working-Rules.md`): at 10 files in `Milestones/1. planned/`, stop cutting
and build one.

## Tuning sequence (one lever per commit)

Play 2026-08-16 `[unconfirmed]`. Higher tier than #12 would be “don’t teach juice”
or “dump before the fight” — those stamps are already overturned. Remaining
pain is TTK and transfer rate.

| Order | Lever | Tier | Why not a higher tier | Status |
|---|---|---|---|---|
| 1 | MK-1 damage 14 → **30** | #12 | Fodder TTK. Do not lower `BaseEnemyHp` (Thief 120 / Hunter 20 stay distinct) | **shipped** — `WeaponCatalog` |
| 2 | `HatchBank.DripPerSecond` 20 → **8** | #12 | They named transfer rate. Do not cut `DurationSeconds` unless a log shows they always last 45 s at Hatch 300 | **shipped** — `HatchBank.cs` |
| 3 | Zone terminal shape + juice teach | #6 | Sequencer, not a constant | partial — `FirstTerminal.cs` in tree is the old 3-Wave T1 (re-spec to zone 1's 2×1-Wave, beat 1; needs `DemoActive` gate); juice teach is voice, unbuilt |

Carry-3 extra Thief is in tree (`HatchBank.Carry3ExtraRam`) — keyed to the old
3-Wave shape; the loss-1 window is now authored overwhelm (beat 2, tunable).
Kill observation re-aimed (Zo, 2026-08-17): a 0-upgrade MK-1 log of this window
reaching `NotifyAssaultCleared` with Hatch > 0 is now a **tuning defect**, not
a target — the window must be unclearable at the no-upgrade ceiling.

## Shared flags (SaveVersion 9)

Add fields on `SaveData` / `MetaState`. Migration: missing keys → demo-on for a new
campaign (`DemoActive = true`), demo-off for a save that already has `CleanExits >= 1`
or `DeepestWaveReached > 1` (they have already played; do not trap them in the capsule).

| Field | Default | Who writes it | Who reads it |
|---|---|---|---|
| `DemoActive` | `true` | Set `false` on first `Escaped` (beat 7) | FAB, wheel cap, composite retrigger, zone-shape rules, HP floor, MP gate |
| `DemoCapsuleOpened` | `false` | Beat 0 lid-open complete | Skip reseal on reload; spawn on deck |
| `DemoNavPowered` | `false` | Local seat, beat 0 repair. **Not replicated** — beat 0 is solo | NAV live vs dark; capsule vs `Spawn_Nav` |
| `DemoArmoryPowered` | `false` | Beat 0 ship terminal `T` | ARMORY lit; rack grant available. Panel unlock is `UpgradesUnlocked` (derived), beat 3 |
| `DemoWeaponGranted` | `false` | Beat 0 rack grant | EMBARK refuse; skip grant |
| `DemoShipFightDone` | `false` | Last beat-0 intruder down | EMBARK refuse; intruder respawn on load |
| `DemoCrewMenuUnlocked` | `false` | First Docked after the first bank (`HatchConversions >= 1`) — re-keyed again with the wheel key (Zo, 2026-08-18; was first-loss, was Wave-2-seen) | CREW panel vs heal-only. Heal-on-interact is live from beat 0 regardless |
| `DemoFabPowered` | `false` | Stays false while `DemoActive`. Set true with `DemoActive` false | FAB open vs dark |
| `DemoZoneDeepestWave` (per zone) | `0` | Deepest **cleared** field Wave in that zone, written on each field-Wave clear (ruling confirmed at cut, Zo 2026-08-17 — replaces `DemoNavUnlockWave` / `DemoBankCapWave`, which are never added) | Composite retrigger default / partial-start bounds (beat 4); guest `BankCap` while `DemoActive` and `PlayerCount > 1` |
| `DemoWheelBuysLeft` | `2` | Decrement on `BuyWheelUpgrade` while `DemoActive` | ARMORY refuse at 0; **multiplayer gate** (`< 2` unlocks). Loss-2 grants land outside this count |

`DemoWave2Seen` is **deleted** (Zo, 2026-08-17) — the CREW unlock re-keyed to
first Docked after first loss. `DeepestWaveReached` stays “highest field Wave
**cleared**.” The retrigger-source question closed at the cut (Zo, 2026-08-17):
the composite retrigger and guest `BankCap` both read per-zone
`DemoZoneDeepestWave` — one answer for host and guests; guest wallets’
`Deepest(slot)` used by `BankCap` tracks the same per-zone deepest, not a skip
wave (the skip is dead).

Every flag is that seat’s `GameSession.Meta` (`M65`, `M72` criterion 6 — personal
shopping). Nothing replicates: beats 0–3 are solo, and from beat 4 on the only
shared state is the lobby’s existing start-wave commit.

## Multiplayer bar (beats 4+ — earlier beats are solo by construction)

- Multiplayer locked until first wheel purchase: F10/F11 and lobby entry refuse with
  `[Demo] multiplayer locked — buy an upgrade first` while
  `DemoActive && DemoWheelBuysLeft == 2`.
- Host-authoritative `RunSim`. Client-authoritative own movement. `[Net]` logs, never
  `[Dev]`, on a close path.
- Party lead owns NAV commit (`M72` c4). Guests Ready + 5 s countdown (c5). Join during
  countdown refused.
- Lead’s `PendingStartWave` drives the Run (c6). Guests’ re-keyed retrigger
  fields do not.
- Personal wallets (`M65`). After beat 4, `BankCap` keys the re-keyed per-zone
  deepest (or the terminal’s derivation) or guests bank a Wave they have not
  converted.
- Guest down waits on Ship (`M57`); host/solo down ends the Run (`M49`). First
  Hatch-bank does not override that. Buy-back (Zo, 2026-08-17): a dead player
  may buy back in **only while a bank window is live**, rejoining at the Hatch,
  paying from their **own** banked salvage (cost tunable). Open (Gaps):
  dead-**host** rejoin vs `M49`.
- Voice: `voice.md`. Game + at most a few guides. Player does not reply. No vendor net.

`M72` closed 2026-08-17 (Relay `KPRMPR`, `13-24-37`). A friends close of this demo
still needs a five-Wave both-participating Run.

## After first Escaped

`DemoActive = false`. FAB lights. Wheel uncapped. NAV chain from
`The-Ship.md` returns (`StartRun(N)` delivers 1..N). Convert stays drip + dump
(already). Capsule never runs again on that save.

## Gaps / parked

- **Open** (Zo, 2026-08-17, record not resolve): dead-host rejoin during a live
  bank window vs the `M49` host-death-ends-run rule.
- **Closed at cut** (Zo, 2026-08-17): composite retrigger source is per-zone
  `DemoZoneDeepestWave` (beat 4's stamp; Shared flags row above corrected).
- **Parked intent, not spec** (Zo, 2026-08-17): early Ship return loses/reduces
  the future salvage multiplier when the juice mechanic lands.
- Finale kit mapping (which kit terminal hosts the 5-consecutive battle)
  `[unconfirmed]` — beats 6–7 (another editor).
- Loss-1 pile gap: in-tree zone-1 Wave specs yield 1200 Unbanked at dial 0 vs
  the ruled ~2k bank-start aim (tunable) — beat 1/2 tuning, not resolved here.

## Rejected (do not rebuild)

Companion narrator (`M62`). Per-second siphon (`Decisions.md`). CREW locked behind
death (`Decisions.md`). Pause that sets `Time.timeScale = 0`. Storyteller asset.
Pixel Crushers / any store dialogue pack for this demo. Player reply trees.
A third win condition besides `M54`’s Hatch-bank half. Unity Lobby service.
NAV wave-skip (`DemoNavUnlockWave` picker skip — dead 2026-08-17).

## In-tree facts the beats assume

Wave 1: 18 Defaults, HP mul 1.0. MK-1 damage **30**.
Salvage 25/kill. Secondary wheel 150 then 300. Hatch-bank window 45 s, drip **8**/s.
Slots 3. Player 100 HP. Swing 0.7 s. Hatch 300 HP.

Kit graph: `bastion-defense-layout.txt` lines 6–8 and 70–73.
