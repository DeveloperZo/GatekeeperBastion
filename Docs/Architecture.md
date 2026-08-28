# 03 — Architecture

<!--
Purpose: Where code lives, the conventions that govern it, and the gotchas that cost real time. The rules subtree behind it is Architecture/, which is the master reference for how code is written.
Status: working
Update trigger: when an assembly, folder or convention changes, or when a gotcha is found that would otherwise be rediscovered.
Last updated: 2026-08-25 (four files now over cap; Cloaker, Demo/ and Range/ in the map; gotcha 4 re-read against BastionCoopSceneTests and the Ship hull rebuild)
-->

## The one structural rule

`Assets/Scripts/Sim/` (namespace `GatekeepersProtocol.Sim`) is its own assembly,
`GatekeepersProtocol.Sim.asmdef`, with no assembly references. Inside it: no
`MonoBehaviour`, no `GameObject`, no scene lookup, no `Time`, no ambient
`Random`, no `SerializeField`, no `UnityEditor`, no static mutable state.
CoreModule value types (`Vector3`, `Quaternion`, `Mathf`) are permitted.
Deterministic, seedable (`RunSim.Seed` / `RunSim.Rng`), driven only by explicit
`Tick(dt)`. `Sim/Util/Log.cs` rides along inside the assembly.

The reason is **testability and one-directional dependency** — EditMode tests
with no scene and no domain reload, and two independent Runs advancing in one
process (`TwoRunIsolationTests`). NGO is in (`M48`): the sim is host-authoritative;
MonoBehaviours are replicated views. The full argument, with sources, is
in `Architecture/1-Boundaries.md` section 1.

`Assets/Scripts/Game/` MonoBehaviours are **thin views**: they read sim state and
draw/spawn/move. They do not own rules.

## Code map

Folders group by subject. **Namespaces do not follow folders** — everything under
`Scripts/Sim/**` is `GatekeepersProtocol.Sim`, everything under `Scripts/Game/**` is
`GatekeepersProtocol.Game`. Adding a folder never means adding a namespace.

**Read this map as a subject index.** 282 `.cs` files as of 2026-08-25 (70 Sim, 190 Game,
21 Editor, 1 Util), against maybe half that many names below — a name missing here means
the map has not caught up, never that the type is absent. `ls` the folder before concluding
anything from an omission.

```
Assets/Scripts/
  Sim/                       own assembly (GatekeepersProtocol.Sim), no scene deps
    Run/     RunSim(+7 partials)  RunContext  RunPhase  AssaultState  SalvageLedger
             EscapeSpool  IntensityJuice  HatchBank  DecoyState  MasterControl
             PlayerWallets  WaveMode  WeaponView  Respawns  FaucetLoop  TurretState
    Waves/   WaveDirector  WaveSpec  GatekeeperKind  GatekeeperStats  CloakerWatch
    Meta/    MetaState  MetaUpgrades  SaveFormat  JsonText  TetherSiphon
             BoarderProtocol  BoarderSheets  BoarderState
             RealmState  RealmRecord  RealmShare  ProductVersion
             EquipmentId  EquipmentCatalog  PayloadCatalog  PayloadId  UpgradeTrack
             WeaponId  WeaponSlot  WeaponDef  WeaponCatalog
             WeaponTier  WeaponWheel  WeaponAbility  WeaponCrosshair
    Bastion/ BastionKit  BastionSketch*  BastionSeatSpawns  BastionUnlockGraph
             BastionMeshMap  DemoZones  FirstTerminal
    Combat/  KillLog
    Ship/    ShipSystems  Subsystem  ShipCamera  ShipHatches
    Util/    Log (front end — rides inside the Sim assembly)
  Game/                      MonoBehaviours, thin views
    Player/    FirstPersonController  PlayerGun(+.Firing, +.Slots, +.Reload)
               PlayerProtocolAbilities  PlayerHealth  PlayerRegistry  Interactor
               BoarderPresentation  WeaponViewModel
    Enemies/   EnemyGatekeeper(+.Targeting, +.Death)  HunterGatekeeper  ThiefGatekeeper
               CloakerGatekeeper(+Presentation)  WaveSpawner(+.Groups, +.Range)  SpawnPoint
    Combat/    Damageable  Effects
    Bastion/  GameController(+17 partials)  HudController(+.EndScreens, +.Panels,
               +.Fight, +.Pause, +.Weapon)
               HatchStation  RepairInteractable  AnchorField  SpawnRoom
               BastionSegment(+.Geom, +.Aperture)  BastionDoor  BastionGeneratedGeom
               DecoyBay  DecoyObject  DecoyPlacement  BastionTerminal
    Demo/      DemoRun  DemoProgress  DemoVoice  DemoLines  DemoConversationIds
    Range/     RangeConsole  RangeRules  RangeReadout  RangeDoor  RespawnQueue
    Ui/        BastionHudView  station panels  Compendium*  UiBind
               (controllers bind; views own layout)
    Ship/      ShipController  ShipHud  StationTerminal  ShipCommands(+.Weapons)
               ArmoryStation  EngineeringStation(FABRICATION)  CrewQuartersStation  MapStation
               ShipHatchDoor  ShipHatchAssignment  ShipTeleporter  ShipIntruder
               IsometricCameraRig  WallRenderState  Demo* (ship demo beats)
    Flow/      SceneFlow  GameSession  SaveService  SplashController  MainMenuController
               PlayerLaunch  PlayerPrefsSettingsStore  OptionsScreen  ArmedAction
               RealmSelectController(+.Hosting)  RealmRegistry  IRealmStore
               CloudRealmStore  RealmShareService  RealmShareRemote  RealmShareBeacon
               FrontEndMusic  LevelMusic  MusicFader  FrameBudget
    Input/     Controls  PlayerInputFrame  ShortcutPanel — one binding list + one input frame
    Settings/  Settings  SettingId  SettingDecl  ISettingsStore (moved out of Sim/, M45)
    Dev/       DevCommands  DevConsole (editor/dev-build only)
    Net/       NetSession(+.Internal)  NetJoinIntent  NetRunMirror  NetPartyLobby
               NetPlayerAvatar(+5 partials)  NetShipWait  NetEnemy  NetVerbs  NetPrep
               RelayJoin  EndSummary  RunSeats  RealmRecordPublisher
               (friends: DDOL NetSession from Resources/Net/NetSession; EnableSceneManagement on;
               M57 additive wait stays off NetworkSceneManager)
  Editor/
    Scenes/  SceneBuilder(+.Ship, +.Enemies, +.Range, +.RangeDoors, +.ProtocolBar)
             — Environment/spawns/light/NavMesh; hosts+UI authored
    Bastion/ BastionKitEmitter  BastionSketchEmitter  BastionSketchHatchDoors
             BastionSketchStairs  BastionGeomGroupTool
             BastionFieldConsole  BastionTerminalEditor
    Tools/   IntegrationCheck  BindingValidator  LogTools  GameplayRecorder
             ShipFlythrough  StationSceneGate  CompendiumCopy  ReleaseGateDump
  Util/      LogFile (one file per session)

Assets/Tests/
  EditMode/  by subject (Run, Waves, Meta, Ship, Enemies, Ui, …) — count from the latest
             gkb_verify_report.json, not this map
  PlayMode/                                 scene contracts
Assets/Prefabs/    prefab assets only; Net/NetSession is the authored copy
Assets/Resources/Net/  runtime Load path for DDOL NetSession
Assets/Packs/Gatekeepers/  dress wrappers; Prefabs/*Gatekeeper.prefab are SceneBuilder outputs
Assets/Materials/  every Mat_*.mat; SceneBuilder writes here (MaterialDir)
```

The load-bearing entries: `RunSim` is the Run state machine and the whole public API the
Game layer codes against; `WaveDirector` holds deterministic Wave scaling and every gatekeeper
combat constant; `MetaUpgrades` holds costs, gates, and every level→effect formula;
`GameController` owns the RunSim and exposes every command that mutates it;
`ShipCommands` is the only path by which station UI mutates MetaState.
`BoarderProtocol` is the CREW sheet; G/H actives and planted fields are Game views
(`PlayerProtocolAbilities`, `AnchorField`) pushed at Bind.

## Comments

Code comments say **what this code does and what will break if you change it** — contracts,
invariants, units, and traps. They do **not** carry design rationale, tuning history,
balance tables, or conclusions from play data. Those live in `Docs/`, where they can be
revised in one place when the finding changes. A number's justification belongs in
`Design/4-Data.md`; a decision's belongs in `Decisions.md`.

This is not a style preference. `WaveDirector.cs` reached 203 comment lines against 97 of
code, carrying a balance model that play data later falsified — and the stale essay sat
directly above the live constants, which is exactly how a later session reverts a retune.

## Conventions

- **Run mutation goes through `GameController` command methods.** Nothing
  else calls `RunSim` mutators except `WaveSpawner`'s two sanctioned R1
  delivery-handshake notifications. **Meta / save mutation goes through `ShipCommands`.**
- **UI is read-only.** HUD and station panels read state and draw. Presentation lives under
  `Game/Ui/` (`BastionHudView`, station views). Ship purchases
  go through `ShipCommands`; they never call `MetaUpgrades.Buy` or `GameSession.Save`
  directly.
- **No new static MonoBehaviour singletons.** `HatchStation.Instance` is grandfathered
  (locator only). `GameController.Instance` was retired 2026-08-12. Use `PlayerRegistry`
  for players.
- **Every MonoBehaviour class comment starts with an ownership label**:
  `HOST-ONLY:`, `LOCAL-OWNER:`, or `REPLICATED-STATE:`. Dual-role session types may use
  `HOST / CLIENT:` (`NetSession`). A class with no label is a class nobody has thought
  about networking.
- **File size**: Sim ≤ 600, Game ≤ 400, Editor ≤ 800 counted lines — one unit for
  all three caps, defined once in `Architecture/7-File-Layout.md` and only there;
  Editor's cap is higher because editor assemblers and scene-strip tools carry long
  serialized-wiring sequences. Split with `partial` (see `GameController` /
  `HudController`) only when the split passes `Architecture/1-Boundaries.md`
  section 4's test — *would the split files pass as separate classes if the
  language allowed it?*
- Sim randomness comes from `RunSim.Rng` only. `PlayerGun` deliberately uses
  `UnityEngine.Random` for crits so trigger pulls never advance the replayable
  sim stream.

Four files are over their cap, counted 2026-08-25 by `Tools/lint-arch/run.sh`:
`Editor/Bastion/BastionSketchEmitter.cs` at **976** against 800,
`Sim/Bastion/BastionSketchParser.cs` at **639** against 600,
`Game/Dev/DevConsole.cs` at **420**, and `Game/Bastion/GameController.cs` at **404** and
climbing, both against 400. The fence below stays at `over_cap_baseline=0`, so the check
fails on all four rather than forgiving them — baselining a file by name says the split is
not going to happen, and that is Zo's call in the change that decides it. The precedent for
the other answer is `Game/Bastion/HudController.cs`, 613 counted lines against 400 on
2026-08-11, split by responsibility into `HudController.Weapon.cs` and
`HudController.Fight.cs` alongside the existing Pause / Panels / EndScreens partials.
`Tools/lint-arch/run.sh` reads the caps and the baseline from this fenced block;
changing a value here changes the check with no script edit.

```arch-lint
sim_cap=600
game_cap=400
editor_cap=800
over_cap_baseline=0
over_cap_known=
```

## GOTCHAS

These are real bugs that cost real time. Each one is load-bearing.

**1. Unity prefabs freeze `[SerializeField]` values at creation.** Editing a
default in code does **not** reach an existing prefab asset — the prefab's stored
value silently wins. This cost a whole tuning pass: gatekeepers kept hitting for the
original 4 damage every 1.2s while the code said 18 every 0.7s. That is why
`WaveSpawner` **pushes Gatekeeper stats from sim constants at spawn**
(`Damageable.SetMaxHp(WaveDirector.BaseEnemyHp * multiplier)` and
`EnemyGatekeeper.Configure(...)`), and why the same pattern is used for
`PlayerGun.Configure`, `FirstPersonController.ConfigureStamina` and
`RepairInteractable.Configure` in `GameController.ApplyMetaEffects`. If you add a
tunable, push it; don't serialize it and hope.

**2. Melee range is measured against the target's collider surface on the floor
plane, not its transform.** The Hatch's centre is 1.5 m up and a gatekeeper's origin
is at its feet, so the vertical offset ate most of a 2.4 m budget on its own.
Use `HatchStation.ClosestSurfacePoint` + a planar (y-zeroed) distance. Related:
pathing at the Hatch's transform aims *inside* its collider where no navmesh
exists, so the agent gets a partial path and never registers as arrived —
`HatchStandPoint` samples a point just off its face instead.

**3. No `GUILayout` in the Bastion HUD.** Fight and end screens draw through
`BastionHudView` (`Game/Ui/`). `HudController.OnGUI` is the Dev-taint banner only
(`#if` + `DevTaintBanner`). An exception between `BeginArea` and `EndArea` still
unbalances IMGUI for the rest of the session, so the banner stays on absolute `GUI`
rects. Station panels in the Ship scene may use GUILayout. GUIStyles there are
per-instance, never static (static styles survive domain-reload-disabled sessions
holding destroyed font references).

**4. Bastion geometry is kit-authored; the Ship hull regrows under its dressing.**
Kit prefabs stamp rooms quickly; `BastionSketchEmitter` unpacks `Completely` and
`SpawnPiece` unpacks on editor Rebuild. `Rebuild` and `OnValidate` refuse rooms under
`Authored/`. Orphan kit cleanup only destroys `Gen_*` litter — room roots carry
`BastionGeneratedGeom`, and a missing script during compile used to delete the whole
room. A YAML test (`BastionCoopSceneTests`, EditMode) fails the run if
`Bastion_Coop.unity` loses any of the rooms `R1 R6 R10 R15 U1 U6 U10`, falls outside a
20–30 `BastionSegment` band, loses `Hatch_1..3` or any of the nine `Stair_*` /
`GStairWalk_*` / `GStairLand_*` objects, leaves anything under `Generated`, or moves the
rooms out of `Authored`. `EnsureAuthoredSpawnRooms` re-places closets without emitting
rooms (`gkb_bastion_spawn_rooms_request.txt`). Build All does not wipe an owned root that
still holds `SpawnRoom` instances. `Ship - CoOp.unity` is **not** the same shape any more:
Build All wipes and regrows `Environment`, `SpawnPoints` and `Directional Light` there,
because `ShipDress` is a skeleton of empty per-room containers that the owned-roots wipe
never reaches, so the hull can rebuild underneath it. It does **not** touch stations, the
player rig, `ShipController`, `GameSystems`, the `EventSystem`, or any UI canvas. Build
settings lead with **Splash, MainMenu, RealmSelect, Ship - CoOp, Bastion_Coop**, then
Range; `Explore - Kit` is not registered. After a
spawn-point or kit-floor change, drop `gkb_bastion_navmesh_request.txt` (or Build All).
Hand-editing **kit geometry** and **ShipDress** persists. A scene missing from build
settings makes `SceneManager.LoadScene` throw, which strands the player on the end
screen — `SceneFlow` guards this and logs an error telling you to rebuild.

**5. Friends session bootstrap is `Resources/Net/NetSession`, not an in-scene NetworkManager.**
Menu Host/Join loads that prefab DDOL (`destroyWithScene: false`) before Ship.
`EnableSceneManagement` is on for party scene loads. Solo Continue / Start New never
creates a session. Additive Ship-wait for a downed client (`M57`) stays off
`NetworkSceneManager`. `M72` closed 2026-08-17 (Relay host log `13-24-37`).

Do not re-invent the deleted mutators: `ShiftFrontEndMutate`, `ShiftShipUiImport`,
`ShiftBastionUiImport`.
