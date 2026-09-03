# Bastion with friends

<!--
Purpose: Friends-session storyboard for the live alpha. Beats 0–5 walk a party
through what 0.34.2 actually does; rows marked Build are still unbuilt maturation.
Voice lines live in voice-coop.md and do not ship (`DemoCoopLines` does not exist).
Play Mechanics (The-Crew, A-Boarding, Going-Down, The-Hatch, The-Faucet-Loop,
Master-Control) outrank this folder for the rules. This folder is the evening's
order, not a second source of truth.
Status: working
Authority: Have columns are the tree (2026-08-30). Ruled rows are Zo's. Stamped
maturation (M-A targeting, revive, presence, co-op lines) awaits his override.
Update trigger: when a stamp is overridden, when a Have row drifts from the tree,
when Ante returns, when multi-door combat or teammate revive lands
Last updated: 2026-08-30 (hatch ownership restamped to shipped PackFor assignment)
Depends: ../Demo - Single/README.md (solo gates still apply on a fresh realm),
../Play Mechanics/The-Crew.md, ../Decisions.md
-->

## What this is

A friends evening on the live game, not a gated co-op demo and not a continuation
of a Demo - Single save. There is no `DemoCoopActive`, no `DemoCoopLines`, and no
co-op runner. Hosting is REALMS **OPEN TO FRIENDS**; the party loads **Ship - CoOp**
then **Bastion_Coop**.

A fresh realm still starts `DemoActive` (`RealmState.cs`). Friends unlock when
that realm's Boarder is armed (`DemoProgress.MultiplayerUnlocked` keys on
`DemoWeaponGranted`, not a wheel buy). The solo ship tutorial still runs unless
`SkipShipTutorial` has been set. This folder authors no teaching beats on top of
that.

## The evening

Host opens a realm, a friend joins with the code, both kit on the shared Ship,
embark, start Waves at the Hatch, walk a zone to its terminal, shut it off, come
home, restock, go back. Master Control is the win. Incremental: T2 then D1, T5
then D3, then the control room.

## What this evening exists to sell

| # | Maturation | Where it lands | What is true in the tree today |
|---|---|---|---|
| **M-A** | **A fight that needs two** | Beats 1, 3, 5 | **Layout shipped, fight did not.** Three `ShipHatchDoor`s on R1; `ShipHatches.OpenFor` opens middle / 1+3 / all three. Combat is one `HatchStation.Instance` with one **600** HP pool (`ShipSystems.HatchMaxHp`). `WaveDirector.NextWave` still multiplies `enemyCount` by `playerCount`. Targeting does not pick a door. |
| **M-B** | **Shared stakes** | Beats 0, 4 | **The realm pot shipped.** `RealmState.BankedSalvage` is the one bank; kills credit it on landing (`AwardKill` → `BankKill`). ARMORY still spends `GameSession.Meta` locally (`ShipCommands.BuyWeapon`); a guest's readout is `RealmRecordSource.Banked` (the host mirror). No shop RPC in `NetVerbs`. |
| **M-C** | **Seeing your teammate** | Beat 4 | `NetPlayerAvatar` replicates position, **yaw only**, HP, slot, protocol, flags, Unbanked, Banked. **Fire and Reload ClientRpcs landed** (`NetPlayerAvatar.Anim.cs`). Not replicated: pitch, weapon id, name label. |
| **M-D** | **Downed and revive** | Beat 2 | **Revive does not exist** (zero `revive` hits under `Assets/Scripts/`). Live verb: 10 s bleed-out, priced self-respawn from the **realm bank** at **150 / 300** (`Respawns.CostFor`, cap **2** this Boarding). Guest waits on the Ship (`WaitingOnShip`). A downed host does not end the Boarding while another seat fights. |

## The beats

| File | Player-visible change | Sells |
|---|---|---|
| [`0-the-invite.md`](0-the-invite.md) | REALMS OPEN TO FRIENDS, join by code, kit from the realm pot, hatch doors open by seat count, embark. | M-B |
| [`1-two-doors-to-hold.md`](1-two-doors-to-hold.md) | Zone 1 from the Hatch to T2. Two doors are open; one HP pool is what they attack. | M-A |
| [`2-picking-each-other-up.md`](2-picking-each-other-up.md) | A seat goes down. Bleed-out, bank-priced return. Teammate revive is still Build. | M-D |
| [`3-the-bank-and-what-it-costs.md`](3-the-bank-and-what-it-costs.md) | Convert is an opt-in bet on a withdrawn pile. Hatch 0 HP ends the Boarding. | M-A |
| [`4-restock-and-go-back.md`](4-restock-and-go-back.md) | Dock, spend the realm pot, re-embark, T5. | M-B, M-C |
| [`5-deeper-until-the-boss.md`](5-deeper-until-the-boss.md) | Master Control: five control-room Waves, then the elite, then the switch. Escaped. | M-A |
| [`voice-coop.md`](voice-coop.md) | Line ids. None of them ship. | |

## Decisions

**Ruled by Zo, 2026-08-20**, restamped 2026-08-30 where the tree moved:

| Decision | Ruling | Tree today |
|---|---|---|
| M89 | **Killed.** First person is chosen. | Unchanged |
| The hull | One ship. "Bastion" means the garrisoned vessel you board. | `Ship - CoOp` + `Bastion_Coop` |
| What a hatch is | A **door from the bastion to your ship**. Gatekeepers attack the door. Hatches spawn nothing. | Combat is still one `Subsystem` Hatch. Three hull doors are the open mask. |
| Hatch count | Three on a side. Open count follows seat count: middle for one, 1 and 3 for two, all three for three. | `ShipHatches.OpenFor` — **landed** (`M92`) |
| Hatch ownership | **Host-owned assignment, 2026-08-30.** The i-th open door belongs to the i-th seat (`ShipHatches.PackFor`). Prompts `yours` / `SEAT n` / `sealed`. No player claim or swap. Overturns 2026-08-20 "nobody assigned" — tearing `PackFor` out orphans M92's packed wire. | Landed (`M92`). |
| Kit | **Bought, not granted.** | MK-1 and SEC-D cost **0**. `SkipShipTutorial` grants the MK-1. Primaries stay gated while `DemoActive`. |
| Buying | **No restrictions** beyond the pot. | `DemoProgress.PrimaryRowsOpen` still hides Primaries until `DeepestWaveEntered >= 2` (or demo off). `DemoCoopActive` was never added. |
| Persistence | **Deferred** in 2026-08-20. | **Expired.** Realms and Boarders persist (`M97`). Fresh *session*, not a fresh *save*. |
| Juice / Ante | Deferred 2026-08-20. | **Parked 2026-08-30.** `Ante.Enabled = false`; Waves at dial 0. Spend path stays behind the flag. |
| Progress | **Incremental.** Hold, return, restock, go further. | Faucet spine: Hatch start → terminal shutoff. Doors T2→D1, T5→D3. |
| Downed host | **Does not end a co-op Boarding while another seat is alive.** | Landed. `GameController.TryEnterLocalDown` / `NotifySeatWentDown`. |
| Entry | Competent player. No extra teaching beats. | Solo beat 0 still runs on a fresh realm until the skip flag. |
| Sequence | `M92` ship, `M93` primaries, `M94` protocols, `M95` this bastion, then the rest. | All four are in `3. completed/`. Remaining maturation is M-A combat, revive, presence, lines. |

**Stamped, awaiting override:**

| Decision | Stamp | Where it lands |
|---|---|---|
| Enemy target selection | A body targets the banking door else the lowest-HP open door. **Not built.** Seekers use `HatchStation.Instance`; hatch commit is latched. Hunters ignore the Hatch (`HunterGatekeeper.MayTargetHatch => false`). | Beats 1, 3, 5. Blocking M-A |
| Hatch HP | Split the existing pool across open doors. **Not built.** One pool of **600** × (1 + 0.05 × Hatch Armor). | Beats 3, 5 |
| One door of several falling | Slow the drip; window ends when the **last** open door falls. **Not built.** Hatch 0 HP calls `EndRun("hatch")`. | Beat 3 |
| Multiplayer gate | Armed Boarder, or demo off. **Landed** as `MultiplayerUnlocked(demoActive, weaponGranted)`. | Beat 0 |
| Shop gating | Primary rows inert in co-op. **Not built.** Gate is still `DemoActive` + zone-1 Wave entered. | Beats 0, 4 |
| Party size | Three seats, three doors. **Landed** — `RelayJoin.MaxGuestConnections` is **2**. | Every beat |
| Revive | Teammate hold-Interact. **Not built.** Live path is bank-priced self-respawn. | Beat 2. Blocking M-D |
| Unbanked on down | Freeze, do not wipe. **Landed.** Freeze protects the next Convert, not the respawn purse (the purse is the bank). | Beat 2 |
| Teammate presence | Pitch, weapon id, fire event, name label. Fire (and reload) **landed**. Pitch, weapon id, name **not**. | Beat 4. Blocking M-C |
| Scope fence | Co-op bastion is the kit spine, three hatches on R1. Layout **landed** (`M95`). | Every beat |

## The multi-hatch change, counted

Hull doors are identity plus an open mask. `.Hatch` mutation is still one pool:
`RunSim` damages `Ship.Hatch`, repairs the same, `ShipSystems` constructs one
`Subsystem`. `MetaUpgrades.HatchHp` is one shop line. Per-door HP and
lowest-HP targeting remain the co-op demo's work (`ShipSystems.cs` says so).

## Open, needs Zo

1. **Revive versus the priced return.** Exposure-only teammate revive is still
   stamped. The live verb charges the realm bank, **150 / 300**, cap **2**,
   **10 s** bleed (`Respawns.cs`). Unknown which reads better in a party; needs
   play data.
2. **The realm pot's fill rate against the number of trips** before Master
   Control. Kills bank on landing, so a lost Boarding still funds the next. Too
   few trips and the loop does not read; too many and it is a grind. Needs a
   played friends evening.
3. **The boss.** No boss kind ships. The live finale is
   `MasterControl.ApplyFinaleElite`: one Thief at **240** HP pre-multiplier and
   **1.5×** body scale, then `T` at the Master terminal. Whether that is the
   co-op boss or a placeholder is Zo's.

## Kill criterion for the hatch rule

Stamped 2026-08-20, while the decision was still neutral. **If, in a logged
two-player bank window, both players spend the majority of the window within the
same door's defensible area and still bank above target, the rule did nothing
the geometry could not, and it reverts to one door.**

This cannot fire on 0.34.2: targeting is singular, so both players *are*
defending one door. The criterion waits on M-A targeting.

## In-tree facts these beats assume

Verified against code, 2026-08-30.

- **Hosting**: REALMS **OPEN TO FRIENDS** (`RealmSelectController.Hosting`); JOIN
  on Main Menu takes the 6-char code. Closing the panel and reopening the same
  realm **reuses** the Relay allocation. LAN fallback on relay failure. No Unity
  Lobby.
- **Friends unlock**: `DemoProgress.MultiplayerUnlocked(demoActive, weaponGranted)`.
  Callers: `RealmSelectController.Hosting`, `MainMenuController`.
- **F10** is `#if UNITY_EDITOR` LAN host. **End** is editor LAN join. **F11** is
  the co-op checkpoint arm, not join. All three gate on `MultiplayerUnlocked`.
- **Boarder name**: `RealmSelectView.BoarderNameText` on NEW BOARDER, separate
  from the realm name field.
- **Enemies** are real NetworkObjects, host-spawned, client clones with AI and
  NavMeshAgent disabled. Client hits are **client-authoritative**:
  `NetEnemy.SubmitHitServerRpc` applies the client's damage (`NetEnemy.cs:89-109`).
- **Party embark** (`M72`): guests READY, lead commits, 5 s countdown
  (`NetPartyLobby.CountdownSeconds`), any seat may cancel, joins refused during
  the countdown.
- **FOLLOW**: Prep-only, free, for a seat that never embarked (`M98`). Assault
  refuses by name.
- **Guest run state**: local `RunSim` is never ticked; readout is `NetRunMirror`.
  Lead starts Waves at the Hatch and takes the party home. A guest **can** open
  a Convert (`TryNetStartHatchBank`); a guest **cannot** start a zone or Master.
- **The loop**: `T` at the Hatch starts the current zone's composite
  (`HatchOffersWaveStart` / `StartZoneWaveFromHatch`). `T` at that zone's
  terminal shuts it off in Assault or Prep (`ShutOffFieldWave`) — banks the
  carry, completes the terminal, opens the door, offers dock. Field terminals
  refuse to start while the Hatch is the start line (`CanSelectTerminal`).
- **Zones**: `DemoZones` is two rows — zone 1 `{T2}` × **2** Waves → **D1**;
  zone 2 `{T5}` × **3** Waves → **D3**. Master starts at the Hatch only when
  `ControlRoomReachable`.
- **Recall pads**: hold Interact **1.5 s** on `T2 Portal` / `T5 Portal`
  (`HatchRecallPad`) teleports to the Hatch.
- **Kills bank on landing.** Unbanked does not fill from kills. Withdraw is the
  only fill. Convert drips a withdrawn pile: `HatchBank.DripPerSecond = 8f`,
  `DurationSeconds = 45f`, no dump on start. Hatch 0 HP → `EndRun("hatch")` and
  `WipeUnbanked`. Banked Salvage survives every ending.
- **Ante parked**: `Ante.Enabled = false`. Convert prompt is
  `[T] Convert  unbanked {n}` (`RepairInteractable.cs:143-148`).
- **Primaries**: VK-7 disabled. BR-12 **1500**, MR-35 **2000**, PRI-C **2500** (raised 2026-09-02).
  MK-1 and SEC-D cost **0**.
- **`RequestBulkheadServerRpc`** is an empty handler in `NetVerbs.cs` with a live
  call site. Do not build on it.
- **Respawn**: `Respawns.BaseCost = 150`, `CostFor` = 150 × min(taken+1, 3),
  `MaxRespawns = 2`, `BleedOutSeconds = 10`. Purse is `BankedSalvage` via
  `RunSim.SpendBanked`. Freeze on down; remainder of carry survives a paid
  return. Heal to full. The Hatch falling cannot be bought back.
- **Co-op Convert ceiling**: in a session, `HatchBank.BankCap(deepestWave)` per
  seat; excess is destroyed, not redistributed (`RunSim.Wallets.CreditSlot`).
- **PlayMode**: net fixtures subclass `NetcodeIntegrationTest`. They cannot test
  MPPM, transports or the join surface.

## Rejected, do not rebuild

Teaching beats on top of solo beat 0. A co-op wake or capsule. Granted kit
(beyond the skip-tutorial MK-1). Purchase caps invented for this folder. Hatch
claiming or swapping as a player verb (the shipped `PackFor` assignment is
host-owned, not a claim). Per-seat wallets. Buy-back priced from Unbanked — the
shipped rule charges the realm bank. A player-owned bastion separate from the
Ship. A dock ring of per-player ships. A sealed-room finale. Unity Lobby.
Voice chat. Ping wheel. Host migration. A second destination. Player reply
trees. `Time.timeScale = 0` for any line. M102 faucet graybox in a player
build (`FaucetActive` is editor/dev only).
