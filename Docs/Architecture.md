# 03 — Architecture

## The one structural rule

`Assets/Scripts/Sim/` (namespace `GatekeeperBastion.Sim`) is its own assembly,
`GatekeeperBastion.Sim.asmdef`, with no assembly references. Inside it: no
`MonoBehaviour`, no `GameObject`, no scene lookup, no `Time`, no ambient
`Random`, no `SerializeField`, no `UnityEditor`, no static mutable state.
CoreModule value types (`Vector3`, `Quaternion`, `Mathf`) are permitted.
Deterministic, seedable (`RunSim.Seed` / `RunSim.Rng`), driven only by explicit
`Tick(dt)`. `Sim/Util/Log.cs` rides along inside the assembly.

The reason is **testability and one-directional dependency** — EditMode tests
with no scene and no domain reload, and two independent Runs advancing in one
process (`TwoRunIsolationTests`). When NGO lands, the sim runs host-authoritative
and MonoBehaviours become replicated views; the full argument, with sources, is
in `Architecture/1-Boundaries.md` section 1.

`Assets/Scripts/Game/` MonoBehaviours are **thin views**: they read sim state and
draw/spawn/move. They do not own rules.

## Code map

Folders group by subject. **Namespaces do not follow folders** — everything under
`Scripts/Sim/**` is `GatekeeperBastion.Sim`, everything under `Scripts/Game/**` is
`GatekeeperBastion.Game`. Adding a folder never means adding a namespace.

```
Assets/Scripts/
  Sim/                       own assembly (GatekeeperBastion.Sim), no scene deps  (~2,800 lines)
    Run/     RunSim  RunContext  RunPhase  AssaultState  SalvageLedger  EscapeSpool
    Waves/   WaveDirector  WaveSpec  GatekeeperKind  GatekeeperStats
    Meta/    MetaState  MetaUpgrades  SaveData  TetherSiphon
             EquipmentId  EquipmentCatalog
             WeaponId  WeaponSlot  WeaponDef  WeaponCatalog
             WeaponTier  WeaponWheel  WeaponAbility  WeaponCrosshair
    Ship/    ShipSystems  Subsystem
    Util/    Log (front end — rides inside the Sim assembly)
  Game/                      MonoBehaviours, thin views     (~7,700 lines)
    Player/    FirstPersonController  PlayerGun(+.Firing)  PlayerAbility
               PlayerHealth  PlayerRegistry  Interactor
    Enemies/   EnemyGatekeeper(+.Targeting)  HunterGatekeeper  RamGatekeeper
               WaveSpawner(+.Groups)  SpawnPoint
    Combat/    Damageable  Effects
    Bastion/  GameController(+.EndScreen, +.DevCommands)  HudController(+.EndScreens, +.Panels)
               HatchStation  RepairInteractable
    Ship/      ShipController  ShipHud  StationTerminal  ShipCommands
               ArmoryStation  EngineeringStation(FABRICATION)  CrewQuartersStation  MapStation
    Flow/      SceneFlow  GameSession  SaveService  SplashController  MainMenuController
               PlayerPrefsSettingsStore
    Input/     Controls  PlayerInputFrame  ShortcutPanel — one binding list + one input frame
    Settings/  Settings  SettingId  SettingDecl  ISettingsStore (moved out of Sim/, M45)
    Dev/       DevCommands  DevConsole (editor/dev-build only)
  Editor/
    Scenes/  SceneBuilder(+.Ship, +.Enemies) — Environment/spawns/light/NavMesh; hosts+UI authored
    Tools/   IntegrationCheck (file-bridge test runner; formerly AutoVerify)  LogTools (Logs menu)
  Util/      LogFile (one file per session)

Assets/Tests/
  EditMode/  Run/  Waves/  Meta/  Ship/     pure sim, 227 tests (last recorded integration-check run)
  PlayMode/                                 scene contracts
Assets/Prefabs/    prefab assets only
Assets/Materials/  every Mat_*.mat; SceneBuilder writes here (MaterialDir)
```

The load-bearing entries: `RunSim` is the Run state machine and the whole public API the
Game layer codes against; `WaveDirector` holds deterministic Wave scaling and every gatekeeper
combat constant; `MetaUpgrades` holds costs, gates, and every level→effect formula;
`GameController` owns the RunSim and exposes every command that mutates it;
`ShipCommands` is the only path by which station UI mutates MetaState.

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

- **All sim mutation goes through `GameController` command methods.** Nothing
  else calls `RunSim` mutators except `WaveSpawner`'s two sanctioned R1
  delivery-handshake notifications.
- **UI is read-only.** HUD and station panels read state and draw. Ship purchases
  go through `ShipCommands`; they never call `MetaUpgrades.Buy` or `GameSession.Save`
  directly.
- **No new static MonoBehaviour singletons.** `GameController.Instance` and
  `HatchStation.Instance` are grandfathered. Use `PlayerRegistry` for players.
- **Every MonoBehaviour class comment starts with an ownership label**:
  `HOST-ONLY:`, `LOCAL-OWNER:`, or `REPLICATED-STATE:`. This is the co-op
  homework done in advance; a class with no label is a class nobody has thought
  about networking.
- **File size**: Sim ≤ 600, Game ≤ 400, Editor ≤ 800 counted lines — one unit for
  all three caps, defined once in `Architecture/7-File-Layout.md` and only there;
  Editor's cap is higher because editor assemblers and scene-strip tools carry long
  serialized-wiring sequences, largest today `StripSettingsToStations.cs` at 791
  counted lines (measured 2026-08-11 by the counted-line awk in
  `Tools/lint-arch/run.sh`). Split with `partial` (see `GameController` /
  `HudController`) only when the split passes `Architecture/1-Boundaries.md`
  section 4's test — *would the split files pass as separate classes if the
  language allowed it?*
- Sim randomness comes from `RunSim.Rng` only. `PlayerGun` deliberately uses
  `UnityEngine.Random` for crits so trigger pulls never advance the replayable
  sim stream.

No file is over its cap today. The last one, `Game/Bastion/HudController.cs` (613
counted lines against 400), was split by responsibility on 2026-08-11 — the weapon
readout moved to `HudController.Weapon.cs` and the per-frame fight readout to
`HudController.Fight.cs`, alongside the existing Pause / Panels / EndScreens
partials — and the baseline returned to zero, where it stays.
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

**3. No `GUILayout` in the HUD.** An exception thrown between `BeginArea` and
`EndArea` leaves the IMGUI layout stack unbalanced, which then throws
"Mismatched LayoutGroup" on every subsequent `OnGUI` for the rest of the session.
The lose screen never renders and RETURN TO SHIP — the only exit from a Run —
can never be clicked. `HudController` uses absolute `GUI` rects only and wraps
`OnGUI` in a try/catch that logs once. Station panels in the Ship scene may use
GUILayout; the Bastion HUD may not. GUIStyles there are per-instance, never
static (static styles survive domain-reload-disabled sessions holding destroyed
font references).

**4. Geometry is generated; UI and gameplay hosts are authored.** `SceneBuilder` rebuilds
only the `Environment` root, the `SpawnPoints` root, the Directional Light, and the
NavMesh asset — in place, in the open scene. It does **not** touch stations, the player
rig, `ShipController`, `GameSystems`, the `EventSystem`, or any UI canvas; those are
authored scene content and survive Build All. Build settings list **Splash, MainMenu,
Ship, Bastion**. After any geometry or spawn-point change, re-run
**`GatekeeperBastion > Build All Scenes`**. Hand-editing **geometry** loses the edit on
the next rebuild; hand-editing **UI or gameplay hosts** does not. A scene missing from
build settings makes `SceneManager.LoadScene` throw, which strands the player on the end
screen — `SceneFlow` guards this and logs an error telling you to rebuild.

Do not re-invent the deleted mutators: `ShiftFrontEndMutate`, `ShiftShipUiImport`,
`ShiftBastionUiImport`.
