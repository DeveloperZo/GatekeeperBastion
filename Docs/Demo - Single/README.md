# First Bastion demo

<!--
Purpose: Map of the first-Bastion demo (through the finale, with multiplayer) and
the shared save/net flags every beat mutates. Beats 0–7 are functional storyboards —
the implementable specs — plus voice.md. Milestones cut 2026-08-17: M74–M80.
Status: demo spec
Authority: implementable — table “Treat as spec” is locked until Zo overrides a row
Update trigger: when a stamp is overridden, when a beat ships, when M72 closes
Last updated: 2026-09-01 (M110 constant-drift pass: the in-tree facts below are re-read from
WaveDirector, MetaUpgrades and ShipSystems. Prior: 2026-08-25 — MaxWaves is an absolute ceiling, not a per-trigger cap; the
priced respawn replaced the co-op buy-back rule and closed the dead-host question; the
finale elite is specced in code; Carry3ExtraThief)
-->

The player is walked from a dead Ship to **Escaped** on the first Bastion. The
loop unit (Zo, 2026-08-17): a zone's terminal Waves, then that zone's
Convert. Surviving the Convert's timer window unlocks the zone's door; a
failed Convert **never** unlocks it; the Convert is locked until the zone's
terminals are complete. `MaxWaves` is **5**, an absolute end-Wave ceiling for
the Boarding — the per-trigger re-key ruled below was an intent and never
landed, and the authored zones fit under the ceiling as written. The finale is a 5-consecutive-Wave terminal battle, then **one** final
Convert window with the finale elite; the haul home is the Hatch. The Ante is
taught on the first terminal cycle (once Unbanked > 0) and stays live.
**Multiplayer is locked until that save's first wheel purchase (beat 3)** —
every seat solos beats 0–3 by construction.

This folder is what an implementer reads. The first-session brainstorm that pointed here was retired 2026-08-19; these beats are the record.

## Treat as spec until overridden

| Decision | Spec | Why it blocks a grain if reopened |
|---|---|---|
| Convert-then-fight | **Overturned** (2026-08-16 play). Live drip; overrun wipes remainder | Beat 2 |
| Loop unit | Zone terminal Waves → Hatch-bank Wave (Zo, 2026-08-17). Surviving the bank's timer window unlocks the zone's door; a failed bank **never** unlocks; the bank is locked until the zone's terminals are complete | Beats 1–7 |
| MaxWaves | **Not what this row ruled.** `WaveDirector.MaxWaves` is **5** and is an absolute end-Wave ceiling; the per-trigger re-key was an intent, not the tree. Zone 1 triggers run 1 Wave, zone 2's terminal 3 (Waves 3–5), the finale terminal 5 — all under the ceiling, so nothing needed the re-key | Beats 1, 5–7 |
| Demo shape | Zone 1 = **2** terminals × **1** Wave each + Convert → D1; zone 2 = **1** terminal × **3** consecutive Waves + heavier Convert → D2; finale = **5**-consecutive-Wave terminal battle then **one** final Convert window with the finale elite (Zo, 2026-08-17). The zone table itself ships in `DemoZones` (T1/T2 → D1, T3 → D2); T4/T5 → D3 is the Control Room approach and is not a zone. Finale kit mapping `[unconfirmed]` | Beats 1–7. Loss 2 and the finale are another editor's files |
| First terminal | Re-ruled (Zo, 2026-08-17): zone 1 = two 1-Wave terminals, untimed Prep, juice taught once Unbanked > 0, bank after both | Beat 1 |
| NAV after that Hatch-bank | **Dead** (Zo, 2026-08-17). No NAV skip; refight-avoidance lives at the terminal: post-loss the zone's **deepest terminal retriggers the composite** by default, partial start electable | Beat 4. v9 skip fields re-key or die (Shared flags) |
| Starting weapon | **MK-1** off the bare ARMORY rack, mid beat-0 fight, no panel. SEC-D stays a 0-salvage buy when the panel unlocks | Beats 0 and 3 |
| Beat 0 fight | Ship terminal (E/T, Bastion pattern) powers ARMORY + spawns **2** weak Gatekeepers; gated Lock for the pickup (intruder release gated on the line's Continue — **no** `timeScale = 0`; Zo, 2026-08-17); HP floor **1**; CREW heals on interact, **no menu** | Beat 0. First menu of the demo is NAV's picker |
| Two authored losses | The unlock spine (Zo, 2026-08-17; **zone-2 recut 2026-08-19**). Loss 1 = zone 1's first bank: authored overwhelm vs the no-upgrade kit ceiling (aim ~2k Unbanked at start, ~500 banked when the Hatch falls; tunable) → wheel + Secondary buys + multiplayer on first purchase. Zone 2's **field Waves are survivable** on the secondary kit; zone 2's **bank** is the authored loss that **sells** the Primary + 2 upgrades outside the count — that ejection may be the save's first or its second. `PodEjections` is not the Primary gate. Zone 2's bank remains **unclearable** on the secondary kit | Beats 2–3 + beat 5 |
| Ship return after a survived bank | Held interact at the Hatch, available until the next Wave is triggered; ends the Run **Docked**, not Lost; banked + door state persist via the successful-bank snapshot (Zo, 2026-08-17). Zone 2's bank remains an unclearable window on the secondary kit, so return-to-Ship cannot skip the Primary gate. Wheel key is the first bank — `HatchConversions >= 1` (Zo, 2026-08-18; `Decisions.md`, *the wheel opens on the first bank*). Primary key is zone 2's bank falling (Zo, 2026-08-19; `M79`) | Beats 2, 5+. |
| Buy-back | **Superseded by what shipped.** Any dead seat — host, guest, solo — is offered a respawn while the Boarding is Tethered, with no Convert-window gate. Price is `Respawns.CostFor`: **150** × (respawns taken this Boarding + 1). The purse is that seat's **own Unbanked**, frozen at the down and debited on the buy, with the remainder surviving; the realm's bank and other seats can never pay. A seat carrying less than the price does not get the offer | Beats 4+. Closes the dead-host question |
| Doors | Unlock on a **survived zone Convert** (Zo, 2026-08-17): D1 = zone 1's, D2 = zone 2's, then the finale's single final Convert. `DemoZones.DoorId` is the table | Beats 2, 5–7 |
| Default Protocol | CREW **menu** unlocks on the **first Docked after the first Convert** (`HatchConversions >= 1`, re-keyed with the wheel key — Zo, 2026-08-18; the Wave-2-seen key it replaced never shipped); CREW heals on interact from beat 0. Default, VEIL and FORGE are all claimable; AEGIS and LEDGER are Coming Soon | Beats 3–4. `M70` is the claim |
| Multiplayer gate | **Locked until that save's first wheel purchase** (re-keyed 2026-08-25 to the MK-1: `DemoProgress.MultiplayerUnlocked`). F10/F11 and lobby entry refuse with a named log. Beats 0–3 are solo for every save | Every beat. Kills guest capsule-skip rules |
| Voice audience | Host starts a **line id**; every seat plays it locally. No player replies. Assault: subtitles, no input lock | `voice.md` |
| Wheel during demo | **Cut 2026-09-02 (Zo: anything affordable is buyable; `Decisions.md`).** Was: A **ladder** (Zo, 2026-08-17; Primary gate recut 2026-08-19): at most **2** Secondary purchases while `DemoActive` after the first bank; when zone 2's bank falls a **Primary + 2 upgrades are sold outside that count** (not granted; not keyed on a second ejection). The loss-1 aim (~500 banked, tunable) funds both secondary buys | Beat 3 + beat 5. Do not gift Salvage |
| Beat 3 exit standing | Fires when **no buyable upgrades remain** (state-keyed; replaces the third-refuse trigger) (Zo, 2026-08-17) | Beat 3 |
| Juice | **Taught** after first Wave 1. Live after that, including the finale | Beat 1. Withhold stamp overturned |
| FABRICATION | Dark while `DemoActive` | Beats 0, 3, 5 |
| First Convert fail | Hatch 0 or the seat going down with less than **150** Unbanked — authored loss 1 either way; the first-loss line's copy is cause-agnostic (beat 2). A seat that can pay stands up instead, so the authored window has to land on the **Hatch** to be reliable | Beat 2 |

## Files

| File | Player-visible change |
|---|---|
| [`voice.md`](voice.md) | Mission dialogue, no VO, no player replies; Shift runner; Ship/Prep can lock input; Assault cannot; no line pauses (`timeScale` exception withdrawn 2026-08-17) |
| [`0-wake-on-a-dead-ship.md`](0-wake-on-a-dead-ship.md) | Play-through: fade, stir, release teleporter, NAV, ship terminal, 2 Gatekeepers (release gated on the line's Continue), MK-1, CREW heal, EMBARK |
| [`1-first-terminal.md`](1-first-terminal.md) | Explore → zone 1's two field terminals, one Wave each; teach juice; Hatch-bank next |
| [`2-first-hatch-bank.md`](2-first-hatch-bank.md) | Authored loss 1: live drip (slowed); overwhelm at the Hatch; leftover Unbanked wiped. Survived banks (later zones) unlock doors and allow Ship return |
| [`3-ship-after-first-loss.md`](3-ship-after-first-loss.md) | ARMORY panel + two Secondary buys (ladder rung 1); first buy unlocks multiplayer; state-keyed exit standing; CREW menu unlock lands on this Docked; FAB dark |
| [`4-embark-at-wave-2.md`](4-embark-at-wave-2.md) | Post-loss EMBARK; the zone's deepest terminal retriggers the composite (partial start electable) — no NAV skip |
| [`5-repeat-to-control-room.md`](5-repeat-to-control-room.md) | Zone 2: one terminal, 3 consecutive Waves (survivable), heavier bank falls → Primary for sale → D2 on a later survival |
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
| 1 | MK-1 damage 14 → **30** | #12 | Fodder TTK. Do not lower `BaseEnemyHp` (`ThiefHp` and `HunterHp` stay distinct from it) | **shipped** — `WeaponCatalog` |
| 2 | `HatchBank.DripPerSecond` 20 → **8** | #12 | They named transfer rate. Do not cut `DurationSeconds` unless a log shows they always last the whole 45 s against an undamaged Hatch | **shipped** — `HatchBank.cs` |
| 3 | Zone terminal shape + juice teach | #6 | Sequencer, not a constant | partial — `FirstTerminal.cs` in tree is the old 3-Wave T1 (re-spec to zone 1's 2×1-Wave, beat 1; needs `DemoActive` gate); juice teach is voice, unbuilt |

Carry-3 extra Thief is in tree (`HatchBank.Carry3ExtraThief`) — keyed to the old
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
| `DemoWheelBuysLeft` | removed 2026-09-02 | — | the wheel ladder is cut; the key is no longer written or read |

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
  `DemoActive && !DemoWeaponGranted` (re-keyed 2026-08-25).
- Host-authoritative `RunSim`. Client-authoritative own movement. `[Net]` logs, never
  `[Dev]`, on a close path.
- Party lead owns NAV commit (`M72` c4). Guests Ready + 5 s countdown (c5). Join during
  countdown refused.
- Lead’s `PendingStartWave` drives the Run (c6). Guests’ re-keyed retrigger
  fields do not.
- Personal wallets (`M65`). After beat 4, `BankCap` keys the re-keyed per-zone
  deepest (or the terminal’s derivation) or guests bank a Wave they have not
  converted.
- A dead seat is offered a priced respawn at **150** × (respawns taken + 1) out of
  its own frozen Unbanked, for as long as the Boarding is Tethered. The host and
  solo seat take it at the DOWN overlay; a waiting guest asks for it over
  `NetPlayerAvatar.RequestRespawn`. `M49` now bites only when the dying seat
  cannot pay and no other seat is still fighting.
- Voice: `voice.md`. Game + at most a few guides. Player does not reply. No vendor net.

`M72` closed 2026-08-17 (Relay `KPRMPR`, `13-24-37`). A friends close of this demo
still needs a five-Wave both-participating Run.

## After first Escaped

`DemoActive = false`. FAB lights. Wheel uncapped. NAV chain from
`The-Ship.md` returns (`StartRun(N)` delivers 1..N). Convert stays drip + dump
(already). Capsule never runs again on that save.

## Gaps / parked

- **Closed by what shipped**: the dead-host rejoin question. The host seat takes
  the same priced respawn every other seat does, and `M49` reduces to the case
  where nobody left can pay.
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

**Scope fence**, carried over 2026-08-19 from the retired first-session brainstorm, which
was the only place it was written whole: the demo is the **first** Bastion through Master
Control — **no second destination**, and **no new resource**. The capsule is furniture and
the retired ejection verb stays retired.

**Kill observation for "overrun"** (same source): a 0-upgrade MK-1 (damage 50 after lever 1; 30 before 2026-09-02)
solo log of the first Hatch-bank window reaching `NotifyAssaultCleared` with Hatch > 0 kills
it. Next lever after that is +1 Thief on that carry (`2-first-hatch-bank.md`).

## In-tree facts the beats assume

Wave 1: **11** Defaults, roster `11 + 7*waveIndex`, health multiplier 1.0. `BaseEnemyHp` 50. MK-1 damage **30**.
Salvage **42**/kill (`MetaUpgrades.SalvagePerKill`). Secondary wheel 150 then 300 on the same spoke. Hatch-bank window 45 s, drip **8**/s.
Slots 3. Player 100 HP. Swing 0.7 s. Hatch 600 HP (`ShipSystems.HatchMaxHp`).

Kit graph: `Tools/bastion-sketch/bastion-defense-layout.txt`.

## The Bastion spine and its loop (Zo, 2026-08-25)

The sprawl is gone. `Tools/bastion-sketch/bastion-coop-layout.txt` now authors ONE straight
spine on storey 0: **R1 OpenArea** (the hold, three hatches on its south wall) → **R2/R3 Half**
(T2 in R3) → **R4 OpenArea** → **R5–R8 Half** (T5 in R7) → **R9 ControlRoom** (Master).
Two open areas, six halfs, one control room, R1 through R9, no galleries and no stairs.

**Why.** The shipped layout put zone 1's two spawn closets 84 m apart on opposite flanks;
measured from the hatch their approaches were 91 degrees apart, widening to 149 as the player
pushed up. At a 60-degree FOV no position held both streams on screen, so movement was spent
time-slicing two fronts that damage could never answer from one place. Every spawn now feeds
the same corridor: the spread is 0 degrees.

**The loop.** A wave BEGINS at the Hatch and ENDS at a terminal.
* `[T]` at the Hatch starts the next zone's whole trigger
  (`GameController.StartZoneWaveFromHatch`). It used to read "Convert — finish the zone".
* **The terminal REFUSES to start one** — `CanSelectTerminal` returns false for a field
  terminal while `HatchOffersWaveStart`, so the local look-at and `TryNetStartTerminal` (the
  co-op host entry) are both covered, its prompt reads *"start the wave at the Hatch"*, and
  the demo voice points there. Master Control is exempt: the control room is its own start
  line. **Two start lines is what the 2026-08-25 playthrough actually hit** — the Hatch was
  offering the start AND T2 was offering its retrigger, and `demo.terminal_look` pointed at
  the terminal.
* The retrigger's election (composite vs partial, beat 4) is NOT lost: `E` and `+/-` still
  work at the terminal, and `StartZoneWaveFromHatch` honours whatever is armed.
* The Hatch's start prompt is checked ahead of Convert/Withdraw, because a terminal shutoff
  leaves nothing unbanked and the Convert branch would otherwise swallow it.
* `StartZoneWaveFromHatch` arms the terminal it answers at before calling `StartTerminalWave`
  — that method's "select the Ante first" check is the Ante's, and the first build tripped
  over it: it logged a start and then refused to draw.
* Most of the wave breaches at the FAR closet — `SpawnPoint.Weight`, drawn by a weighted
  rotation in `WaveSpawner`. Zone 1 answers at S1 (R3, 66 m, weight 3), zone 2 at S6
  (R7, 156 m, weight 3); every other door is 1.
* `[T]` at the zone's terminal while the wave runs shuts it off
  (`GameController.ShutOffTerminal` → `RunSim.ShutOffFieldWave`). The terminal completes
  outright — the objective is positional, so fighting all the way up the lane finishes the
  zone whether or not the last body fell. A Convert window has no off switch.
* **The shutoff banks everything unbanked** (Zo, 2026-08-25). Reaching the terminal is the
  walk's payoff, so the pile is credited there at `HatchBank.MultLow` (face value, no carry
  bonus) rather than waiting on a second trip back to the Hatch. Pinned by
  `HatchBankTests.ShutOffFieldWave_*`. The Hatch Convert survives only where it still has a
  stake on it: the bank window, which refuses the off switch.
* The door still opens only on a survived Convert, exactly as before.

**Zones.** One terminal each: zone 1 = T2 → D1 (waves 1–2), zone 2 = T5 → D3 (waves 3–5).
T1, T3, T4 and D2 no longer exist. `DemoZones` is the single source of truth and the dev
checkpoints read it rather than naming ids, which is how "T4" outlived the terminal.
