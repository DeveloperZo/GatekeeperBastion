# Bastion with friends demo

<!--
Purpose: Map of the co-op demo. A separate entry, authored for a player who has
already finished Demo - Single, built on the expanded Ship, hatch doors that open by
seat count, and one crew pot. Beats 0-5 are functional storyboards; voice-coop.md
carries the lines.
Status: demo spec
Authority: proposed. Rows marked Ruled are Zo's; rows marked Stamped are mine and
await his override.
Update trigger: when a stamp is overridden, when a beat ships, when M92 lands, when
the Ante or persistence come back
Last updated: 2026-08-25 (the retired wreck-word struck from the hull row;
thiefCount/cloakerCount identifiers; the down state and priced respawn now ship;
relay cap already at 2)
Depends: ../Demo - Single/README.md (the verbs, already taught), ../Decisions.md
-->

## What this demo is

A separate entry, not a continuation of a Demo - Single save, authored for someone who
has finished it. **No teaching beats.** No wake, no capsule, no verb tutorial. The
party is shopping within a minute and in the bastion within two.

**Runs are fresh and there is one crew pot** (Zo, 2026-08-20). Persistence is deferred.
Nothing carries between sessions, no seat owns a wallet, and every salvage readout on
the Ship is the party's shared balance.

**Progress is incremental.** Hold the doors, come home, upgrade and restock, go back in
further. The boss is reached by repeating that, not by one deep run.

## The rule the whole demo rests on

**A hatch is a door from the bastion to your ship.** Gatekeepers attack the door
itself. Hatches do not spawn anything: bodies come from the spawn rooms exactly as they
do today.

**Three hatch doors on the Ship's side. The open count follows the seat count**: one
seat opens the middle, two open 1 and 3, three open all three. Solo is byte-identical
to today.

**Nobody is assigned a hatch** (Zo, 2026-08-20). Responsible assignment does not help
gameplay. They are doors the party holds between them, and who covers what is decided
in the moment rather than at the hull. There is **no dramatic split**: the drama is
that there is more door than there are people.

**The hatch replaces the player-count multiplier; it does not stack on it.**
`WaveDirector.NextWave` today multiplies `enemyCount` by `playerCount` and pushes the
surplus through the same mouths as Default gatekeepers. `hunterCount`, `thiefCount`
and `cloakerCount` are fixed table rows that never see `playerCount`, and neither do
health, group size or spawn interval. Keeping both counts the second
player twice. Per-player body count stays where it is; what changes is that those
bodies now have more than one door to choose from.

## What this demo exists to sell

| # | Maturation | Where it lands | What is true in the tree today |
|---|---|---|---|
| **M-A** | **A fight that needs two** | Beats 1, 3, 5 | Covered by the hatch rule. The delta is `WaveDirector` target selection plus multi-door support. |
| **M-B** | **Shared stakes** | Beats 0, 4 | **Now the crew pot, not the bank.** What one of you buys, the other cannot. Station buys are not net-routed at all today: every station mutates that seat's local `GameSession.Meta` with no server check, so the pot is the first validated buy in the project. |
| **M-C** | **Seeing your teammate** | Beat 4 | `NetPlayerAvatar` replicates position, **yaw only**, HP, slot, protocol, flags. Not replicated: pitch, weapon id, firing, name. Remote animation is derived from transform delta into one `Speed` float (`:355-379`). |
| **M-D** | **Downed and revive** | Beat 2 | **Revive does not exist.** A repo-wide grep for `revive` over `Assets/Scripts/` returns nothing — no type, method, field, RPC or binding. What ships instead is a down state and a priced self-respawn: `RunSim.SetSeatDown` freezes the dead seat's pile and `RunSim.TryRespawn` debits `Respawns.CostFor` — **150** × (respawns taken this Boarding + 1) — from that seat's **own Unbanked**. No teammate is involved at any point, which is why it still does not sell co-op. |

M-B was per-seat conversion caps at the bank in an earlier draft. Deferring persistence
and adopting one pot removed that entirely, and the shared stake moved to the shop,
where it is more visible and costs no new systems.

## The beats

| File | Player-visible change | Sells |
|---|---|---|
| [`0-the-invite.md`](0-the-invite.md) | Code, join, both shop one pot for a kit, hatch doors open by seat count, embark. | M-B |
| [`1-two-doors-to-hold.md`](1-two-doors-to-hold.md) | Zone 1's terminal with two doors open. Bodies pick whichever is weaker, so one player cannot hold both. | M-A |
| [`2-picking-each-other-up.md`](2-picking-each-other-up.md) | Authored down. Bleed-out timer, teammate revives, cost is exposure. A downed host does not end the run. | M-D |
| [`3-the-bank-and-what-it-costs.md`](3-the-bank-and-what-it-costs.md) | The Convert window with more than one door. Pile drips into the crew pot; the remainder is lost when the doors fall. Authored loss 1. | M-A |
| [`4-restock-and-go-back.md`](4-restock-and-go-back.md) | The loop. Spend the pot together, see what your teammate is carrying, embark and get further. Repeats. | M-B, M-C |
| [`5-deeper-until-the-boss.md`](5-deeper-until-the-boss.md) | Incremental progress across trips until the kit reaches the boss. Escaped ends the demo. | M-A |
| [`voice-coop.md`](voice-coop.md) | Line ids, speakers, audience rule for a party. Extends the solo runner. | |

## Decisions

**Ruled by Zo, 2026-08-20:**

| Decision | Ruling |
|---|---|
| M89 | **Killed.** First person is chosen, not inherited. The isometric probe never runs. |
| The hull | One ship. A bigger Ship on the Normandy SR-2 profile, doubling as the safehouse. The player-owned-bastion idea is struck; "Bastion" means only the garrisoned vessel you board. |
| What a hatch is | A **door from the bastion to your ship**. Gatekeepers attack the door. Hatches spawn nothing. |
| Hatch count | Three on a side. Open count follows seat count: middle for one, 1 and 3 for two, all three for three. |
| Hatch ownership | **None.** Assignment of responsibility does not help gameplay. No owner strings, no claiming, no swapping. |
| Kit | **Bought, not granted.** The party shops the Ship at the start of the run. |
| Buying | **No restrictions.** A player buys anything they can afford, from the first visit. No purchase caps, no rung ladder, no progression gate on any catalog row, no gating a Primary behind a loss. Freedom and expression at the shop is the point. The only limit is the pot's balance. |
| Persistence | **Deferred.** Fresh runs, one **crew pot** to shop from. No per-seat wallets, no carry between sessions. |
| Juice | **Deferred.** Gate the code, do not delete it. |
| Progress | **Incremental.** Hold doors, return, upgrade and restock, go further. The boss is the top of that climb, not a separate finale structure. |
| Downed host | **Does not end a co-op Run while another seat is alive.** `M49` bends in co-op and is unchanged solo. The Run ends when the last living seat bleeds out. |
| Entry | Separate save, competent player. No teaching beats. |
| Sequence | `M92` ship, `M93` primaries, `M94` protocols, `M95` this bastion, then the rest. |

**Stamped by me, awaiting override:**

| Decision | Stamp | Where it lands |
|---|---|---|
| Enemy target selection | A body from a spawn room targets whichever open door is banking or weakest, re-evaluated on door HP change rather than per frame. Spawn rooms are untouched. Hunters ignore doors and are unaffected | Beats 1, 3, 5. Blocking M-A |
| Hatch HP | Split the existing pool across open doors, do not duplicate it. Otherwise a third player buys durability | Beats 3, 5 |
| One door of several falling | Slows the drip; the window ends when the **last** open door falls, not the first | Beat 3 |
| Multiplayer gate | `DemoProgress.MultiplayerUnlocked` now takes `(demoActive, wheelBuysLeft)` and answers true whenever the demo is off, so a save outside the solo demo hosts freely. It still refuses a save with `DemoActive` true and two buys left, which is what a fresh co-op save would look like until the co-op entry sets its own flag. Callers: `MainMenuController`, `RealmSelectController.Hosting`, and the two editor dev keys | Beat 0. Blocking |
| Shop gating | `DemoProgress.PrimaryRowsOpen(m)` hides Primary rows in the ARMORY (`ArmoryStation.cs:246`) and refuses the buy (`ShipCommands.Weapons.cs:44`). It is solo-demo progression and must be **inert under `DemoCoopActive`**, or the no-restrictions ruling does not survive contact with the tree | Beats 0, 4. Blocking |
| Party size | Three seats, three doors. **Landed** — `RelayJoin.MaxGuestConnections` is **2**, so the session tops out at three seats | Every beat |
| Revive | A teammate verb, not a self-serve respawn. Immobile at 0 HP for a bleed-out window; any living seat holds Interact to bring them up at partial HP. M57's `WaitingOnShip` stays as the fallback on bleed-out. Cost is exposure | Beat 2. Blocking M-D |
| Unbanked on down | **Landed, and by a different route than this row asked for.** A downed seat's pile is no longer zeroed at 0 HP; `NetPlayerAvatar.TryMarkWaiting` freezes it instead, because that pile is the purse the respawn is bought with. With the crew pot there may be nothing per-seat left to freeze, and the respawn then has nothing to charge; confirm when the pot lands | Beat 2 |
| Teammate presence | Pitch, weapon id, a fire event, a name label. Four adds to `NetPlayerAvatar`. Not replicated: reload, crouch, sprint, ADS, voice, ping wheel | Beat 4. Blocking M-C |
| Scope fence | The Ship expansion adds doors and the space they need, not new stations. **Overridden 2026-08-22:** co-op bastion is another kit spine (same as the solo demo), three hatches on R1 — not one zone plus the boss stretch. No second destination, no new resource | Every beat |

## The multi-hatch change, counted

A hatch is a door. Doors already carry identity through the sketch marks and the unlock
graph, so the world side is a door prefab with a different mark.

Sim side, counted rather than estimated: `.Hatch` returns 28 hits, of which most are
the unrelated `MasterControlPhase.Hatch` enum or read-only HP display in
`HudController` and `NetRunMirror`. The actual mutation surface is `RunSim.cs:448`
(`Ship.Hatch.Damage`), `:544` and `:596` (repair), and the `ShipSystems` constructor.
`MetaUpgrades.HatchHp` stays one shop line feeding a pool split across open doors.

That is a small change, not a milestone, and an earlier draft of this file called it
one without counting.

## Open, needs Zo

1. **Revive cost.** Stamped as exposure only. The alternative is a salvage cost from
   the crew pot, which makes reviving something the party pays for. Unknown which reads
   better; needs play data.
2. **Bleed-out length.** Absent, not invented. A tune after the layout exists.
3. **The crew pot's starting figure, and its fill rate against the number of trips**
   before the boss is reachable. Too few trips and the loop does not read; too many and
   it is a grind.
4. **The boss.** Stats and composition unspecced. No boss kind ships: `GatekeeperKind` is
   Default, Hunter, Thief and Cloaker. The nearest shipped thing is the solo finale's
   elite — `MasterControl.ApplyFinaleElite` puts one Thief at **240** HP pre-multiplier
   and **1.5×** body scale in the last Convert window, no escorts — and whether that is
   the co-op boss or a placeholder for one is Zo's call.

## Kill criterion for the hatch rule

Stamped now, while the decision is still neutral. The failure mode is that one door is
simply better placed, so the party always defends that one and eats the loss elsewhere.

**If, in a logged two-player bank window, both players spend the majority of the window
within the same door's defensible area and still bank above target, the rule did
nothing the geometry could not, and it reverts to one door.** This needs per-player
position logging against door positions, which does not exist today.

## In-tree facts these beats assume

Verified against code, not against docs. Rows re-read 2026-08-25.

- **Transport**: Unity Relay is wired (`RelayJoin.cs`), project linked. No Unity Lobby
  service and none wanted. Join is a 6-char code on MainMenu with a LAN fallback.
- **F10 / F11** are `#if UNITY_EDITOR` LAN dev keys, not the friends path, and gate on
  the same `MultiplayerUnlocked` flag.
- **Enemies** are real NetworkObjects, host-spawned, client clones with AI and
  NavMeshAgent disabled. Client hits are **client-authoritative**: the client computes
  damage and the host trusts the number (`NetEnemy.cs:75-76`).
- **Party embark** is built (`M72`): guests READY, lead commits, 5 s countdown, any seat
  may cancel, joins refused during the countdown.
- **Guest run state**: a guest runs a local `RunSim` that is never ticked; readout comes
  from `NetRunMirror`. A guest cannot dock the Run.
- **Bank mechanics**: `HatchBank.DripPerSecond = 8f`, `DurationSeconds = 45f`, live drip
  with no dump on start, `EndRun` wiping the remainder.
- **Primaries**: three shippable, not four. `VK-7` is disabled in `WeaponCatalog.cs` and
  excluded from `IsAvailable` and `ShopWeapons`. The set is BR-12 (900), MR-35 (1200),
  PRI-C (1500), each with an implemented ability.
- **`RequestBulkheadServerRpc`** is an empty handler in `NetVerbs.cs` with a live call
  site. Do not build on it.
- **Down and respawn**: a seat at 0 HP is marked down, its Unbanked is frozen rather
  than wiped, and it may buy itself back at **150** × (respawns taken this Boarding + 1)
  out of that same frozen pile. The remainder survives the death. Nothing else can pay —
  not the realm's bank, not another seat — so a seat that has just Converted its whole
  pile cannot stand up (`Respawns.cs`, `RunSim.Respawns.cs`, `GameController.Down.cs`).
- **Test fixture**: 14 PlayMode suites, the net ones against `NetcodeIntegrationTest`.
  The fixture cannot test MPPM, transports or the join surface, which stay eyes-on.

## Rejected, do not rebuild

Teaching beats. A co-op wake or capsule. Granted kit. Purchase caps, buy ladders, or
any progression gate on what the shop will sell. Hatch ownership, claiming or
swapping. Per-seat wallets or conversion caps in this demo. Buy-back priced from banked
salvage and gated to a live Convert window — the shipped rule charges the dead seat's own
Unbanked and is not window-gated, so re-adding the old shape would fork the rule. A player-owned bastion separate from the
Ship. A dock ring of per-player ships. A sealed-room finale separate from the climb.
Unity Lobby service. Voice chat. Ping wheel. Host migration. A second destination.
Player reply trees. `Time.timeScale = 0` for any line.
