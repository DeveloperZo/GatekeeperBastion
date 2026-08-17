# 0 — Wake on a dead Ship

<!--
Purpose: Capsule wake, repair NAV, grant MK-1, keep ARMORY / CREW / FAB dark.
Status: demo spec
Authority: implementable
Update trigger: when spawn furniture changes, when grant target changes, when
DemoNavPowered replication changes
Last updated: 2026-08-16
Depends: voice.md
-->

## What the player sees

They wake in a **capsule** (furniture, not a lose word). The Ship is barely powered.
Walk, look, and hold interact are taught by **repairing NAV MAP**. NAV is dark until
that repair; without it there is no Bastion to select. A second interact **grants**
the MK-1. ARMORY, CREW QUARTERS, and FABRICATION stay dark.

Party: everyone stands on the same hull. The first seat to finish the NAV hold
powers NAV for the Ship. Each seat grants their own MK-1.

## Why this beat exists

The live game opens with four live consoles, a 0-salvage ARMORY buy, and NAV that
warns “no weapon” then EMBARKs anyway (`MapStation.PromptHint`, `TryEmbark`). That
teaches shopping before the Hatch. This demo teaches move / look / interact on a
dead hull, then puts a gun in the hand without a shop row.

## In tree today

| Piece | Fact |
|---|---|
| Walk / look | `FirstPersonController` on authored Ship spawn |
| Hold-E | `Interactor` (range 3.2 m) + `ShipController.TryOpenStationFromInteract` on Interact **Down** — stations open on press, not hold |
| Stations | `MapStation`, `ArmoryStation`, `CrewQuartersStation`, `EngineeringStation` (FABRICATION). All four live on a fresh save |
| Weapon | `ShipCommands.BuyWeapon(WeaponId.Mk1Sidearm)` cost 0. Same for `SecD`. Both are ARMORY rows, not world grants |
| NAV EMBARK | Warns if `!OwnsAnyWeapon` or `!HasRepairRigEquipped`; still commits. Party: lead only (`M72` c4), Ready + 5 s (`NetPartyLobby`) |
| Save | `SaveData.CurrentVersion = 8`. No demo flags |
| Protocol | `EquippedProtocol` defaults to Default in data; `M70` recut makes Default a 0-salvage CREW **claim**. NAV still lets you go with no protocol |
| Spawn | FPC is authored in `Assets/Scenes/Ship.unity`. No capsule spawn, no `Spawn_Nav` |

## The change

1. Fresh save, `DemoActive`: spawn at `Spawn_Capsule`. Play `demo.wake` (Lock, Local)
   once per seat the first time that seat’s Ship loads with `!DemoWeaponGranted`.
2. NAV, ARMORY, CREW, FAB do not open. Prompt on a dark console:
   `[E] repair NAV MAP` (NAV) or `[E] NAV MAP — unpowered` (other dark stations
   name themselves: `ARMORY — unpowered`).
3. Hold Interact on NAV for `DemoRepair.HoldSeconds = 1.5` s (constant, Sim or
   Game const — not serialized on the prefab). Fill is prompt text only
   (`[E] repairing NAV MAP`), no new HUD widget. On complete: host sets
   `DemoNavPowered = true`, `GameSession.Save()`, lobby `NetworkVariable` mirrors
   it, play `demo.nav_repair` (Lock, Party).
4. After NAV is powered, NAV opens as today (panel + EMBARK). EMBARK still
   refuses until **that seat** owns a weapon (see refuse table).
5. `WeaponGrant` interactable (rack or NAV desk child). Prompt
   `[E] take MK-1 Sidearm`. Hold **0.5** s. Calls `ShipCommands.BuyWeapon(Mk1Sidearm)`
   (already owned → log and skip). Sets `DemoWeaponGranted`, equips Secondary,
   plays `demo.weapon_grant` (Lock, Local). Destroy or disable the grant collider
   for that seat (local disable is enough; other seats still need it).
6. ARMORY / CREW / FAB remain dark until later beats. `TryOpenStationFromInteract`
   returns false and logs `[Demo] station dark ARMORY`.

### EMBARK refuses (named, logged, voiced once)

| Condition | Log | Voice |
|---|---|---|
| `!DemoNavPowered` | `[Demo] embark refused — NAV unpowered` | `demo.embark_blocked` |
| Local `!OwnsAnyWeapon` | `[Demo] embark refused — no weapon` | `demo.embark_blocked` |
| Party: any connected seat `!OwnsAnyWeapon` | `[Demo] embark refused — seat clientId=<n> has no weapon` | lead hears `demo.embark_blocked` |
| Party: not lead | existing `[Net] embark refused — not party lead` | none |
| Party: guests not Ready | existing `[Net] embark refused — guests not Ready` | none |

Repair rig is **not** required for this demo’s first EMBARK. Drop the
`no repair rig` warning from the commit gate while `DemoActive`; keep it as
panel copy if it already sits on the map body. FAB is dark, so they cannot buy
the rig yet.

On successful first EMBARK: `demo.embark_first` (Lock, Party) before the load, or
on Bastion Prep first tick if a Ship lock would eat the countdown. Prefer **after**
countdown 0, first Bastion Prep frame, so Ready/Cancel stays usable.

## Create

| Path | Role |
|---|---|
| `Assets/Scripts/Sim/Meta/DemoProgress.cs` | Pure helpers: `CanOpenStation`, `CanEmbark`, `HoldSeconds`, `GrantHoldSeconds`. No UnityEngine |
| `SaveData` / `MetaState` | Fields in [`README.md`](README.md) Shared flags. `CurrentVersion = 9` |
| `Assets/Scripts/Game/Demo/DemoStationGate.cs` | Called from `ShipController.TryOpenStationFromInteract` and station `PromptHint` |
| `Assets/Scripts/Game/Demo/WeaponGrant.cs` | `IInteractable`. Hold 0.5 s → `BuyWeapon(Mk1)` |
| `Assets/Scripts/Game/Demo/ShipSpawn.cs` | On Ship load: capsule vs NAV spawn from Meta + lobby NAV flag |
| Scene `Ship.unity` | Empty `Spawn_Capsule`, `Spawn_Nav` (at current FPC pose). Capsule mesh if Synty has a cryo/sleep pod; else a marked alcove. Child `WeaponGrant` on NAV desk |
| `NetPartyLobby` | `NetworkVariable<bool> DemoNavPowered` |

## Change

| File | Change |
|---|---|
| `ShipController.TryOpenStationFromInteract` | If `DemoStationGate.IsDark(station, meta)`: start/continue hold on NAV only; refuse others. Do not `OpenStationPanel` |
| `MapStation.PromptHint` | Dark: repair copy. Powered, `DemoActive`, no weapon: `EMBARK — take the MK-1 first`. Powered + weapon: existing embark copy without forcing a rig |
| `MapStation.TryEmbark` / `TryPartyEmbark` | Apply refuse table before `RequestPartyEmbark` / `LoadBastion` |
| `ArmoryStation` / `CrewQuartersStation` / `EngineeringStation` `PromptHint` | Unpowered string while their flag is false |
| `MetaState.ToSaveData` / `FromSaveData` / JSON | v9 fields |
| `GameSession` remark | Stale “future co-op lives on the host” — guests have own Meta (`M65`). Do not move guest demo flags onto host Meta |
| `NetPartyLobby` | Host writes `DemoNavPowered` when repair completes. Guests read for prompts and spawn |
| `PlayerHealth.spawnPoint` / Ship FPC | `ShipSpawn` sets pose on load |

## Delete

Nothing. Do not remove ARMORY buy rows; they stay behind the dark gate.

## Numbers

| Name | Value | Source |
|---|---|---|
| NAV repair hold | 1.5 s | this spec (`DemoRepair.HoldSeconds`) |
| Weapon grant hold | 0.5 s | this spec |
| Interact range | 3.2 m | `Interactor.range`, `ShipController.InteractRange` |
| MK-1 cost | 0 | `WeaponCatalog` `Mk1Sidearm` |
| Capsule | furniture | language: not Podded, not ejection |

## Multiplayer

| Event | Authority |
|---|---|
| NAV hold complete | First seat to finish. Host applies `DemoNavPowered` even if a guest held (guest ServerRpc `RequestNavRepair`). Duplicate complete: log `[Demo] NAV already powered` |
| Weapon grant | Local Meta only. Each seat must grant |
| Spawn | If lobby `DemoNavPowered` and this seat `DemoWeaponGranted`: `Spawn_Nav`. If NAV powered but this seat has no gun: `Spawn_Nav` still (they are not in the capsule), grant still available. If NAV dark: `Spawn_Capsule` |
| Guest join during repair hold | No partial hold sync. They start a new hold |
| Guest join after NAV powered | Skip `demo.wake`. Play nothing, or a one-line local `demo.nav_repair` if they have never heard it (`DemoWeaponGranted` false is the cue they are new) |
| Seat already Escaped (`DemoActive` false) | Live Ship: all consoles up, no capsule, no grant |
| Join during countdown | Existing refuse (`M72` c5) |
| Host disconnect | Session ends (`M72` c4). No migration |

`NetShipWait` (`M57`): additive Ship during a live Run. Beat 0 does not apply there
(NAV already powered for a mid-run wait). Gate: `NetShipWait.IsActive` → existing
NAV-only station rule, ignore demo capsule.

## Voice

`demo.wake` Local Lock after capsule spawn.
`demo.nav_repair` Party Lock on NAV power.
`demo.weapon_grant` Local Lock on grant.
`demo.embark_blocked` Local Lock on first refuse per reason per Ship visit.
`demo.embark_first` Party, first Prep on Bastion (see above).

## Driven / Shown / Reads-back

- **Driven by** hold-E on NAV, hold-E on grant, lead EMBARK.
- **Shown** dark prompts; NAV panel only when powered; MK-1 in Secondary slot
  (weapon drawer / hands on next Bastion).
- **Reads-back** `[Demo] NAV powered`, `[Demo] granted MK-1`, embark refuse lines,
  `[Ship] bought MK-1 Sidearm for 0` (existing `ShipCommands` line).

## Logs

| Event | Line |
|---|---|
| Capsule spawn | `[Demo] spawn capsule clientId=<n>` |
| NAV hold tick complete | `[Demo] NAV powered` |
| Grant | `[Demo] granted MK-1` then existing `[Ship] bought MK-1 Sidearm for 0 — equipped Secondary…` |
| Dark station | `[Demo] station dark FABRICATION` (name the station) |
| EMBARK ok | existing `[Ship] embarked…` plus `[Demo] first embark` once |

## Traps

- Opening the station on Interact Down will skip the hold. Gate **before**
  `OpenStationPanel`.
- `BuyWeapon` already-owned returns false and logs “already owned” — grant must
  treat owned as success for `DemoWeaponGranted`.
- Prefab-serialized hold duration will freeze (`Architecture.md`). Constants in
  code, pushed at spawn if a view needs them.
- Host Meta vs guest Meta: powering NAV from a guest must ServerRpc; writing only
  the guest save leaves the host’s NAV dark.
- `OwnsAnyWeapon` true from an old save: skip grant, skip capsule if NAV already
  powered (migration rule in README).
- Party EMBARK while a guest is still in `demo.weapon_grant` lock: refuse until
  that seat owns a weapon (table), not until the conversation ends.
- `GUILayout` in a custom repair bar.

## Depends / Unblocks

Depends: `voice.md` runner (mechanics work mute). Unblocks: beat 1 EMBARK.

## Verification

1. EditMode: v9 round-trip JSON; pre-v9 with `CleanExits >= 1` → `DemoActive false`;
   fresh → `DemoActive true`, NAV false.
2. EditMode: `DemoProgress.CanEmbark` matrix (NAV, weapon, party seats).
3. PlayMode Ship: fresh save spawns at capsule; ARMORY Down does not open; 1.5 s
   NAV hold logs `NAV powered`; grant logs `granted MK-1`; EMBARK before grant
   refuses; after grant, solo EMBARK loads Bastion.
4. Loopback: guest hold powers NAV on host; host grant does not grant the guest a
   gun; party EMBARK refuses until both own MK-1; Ready + 5 s still works.
5. `Time.timeScale == 1` through wake conversation.
6. No `[Dev]`.

## Survive / skip branches

If `DemoActive` and NAV already powered (rejoin): no capsule. If they already own
MK-1: grant collider local-off. If `DemoActive` false: this beat is a no-op.

## Rejected

Granting SEC-D at the rack. Auto-own Default Protocol in the capsule (`M70` recut).
Lighting FAB so they can buy a Repair Rig before the first Run. Teaching juice on
the Ship. A companion who wakes you.
