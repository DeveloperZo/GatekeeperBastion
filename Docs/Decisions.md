# 05 — Decisions

Why the game is not the thing it used to be. Read before proposing a feature;
re-litigating a cut costs more than the cut saved.

## Parked — may return

- **The Ante (intensity dial / juice).** Parked 2026-08-30 (Zo): player-facing ante-up,
  E-select lock, and Ante readout are off; Waves run at dial 0. Spend path and curve stay
  behind `Ante.Enabled` (`static readonly` in `Assets/Scripts/Sim/Run/IntensityJuice.cs`; each
  `RunSim.AnteEnabled` copies it). Spend-path tests set the instance field. Not a permanent cut — flip the
  flag when the stake half of "pace and stake" is wanted again. Play Mechanics page
  `Ante.md` is status parked.

## Cut — do not rebuild

- **Spawn-direction variance (2026-09-02).** Proposed as M116's second lever from Left 4
  Dead's flow-distance spawning and Vermintide's front/ambush roll. Zo: the players defend a
  static point, so L4D is the wrong comparison; direction is a side effect of level design
  (today the lane is a long hallway), and the overtune is a player attacked from behind while
  pressing forward, or worse walking back because a body spawned behind them and hit the
  Hatch. Do not add a seeded closet plan, an ahead/behind split or a pincer rule. Direction
  changes when the level does.
- **The demo wheel ladder — two metered buys, then two more (2026-09-02).** Rung 1
  (`DemoWheelBuysLeft = 2`, Zo 2026-08-17) and rung 2 (`DemoRungTwoBuysLeft`, opened by zone
  2's bank, `M79`) are gone: the counters, the `demo cap` / `ladder rung spent` refuses, the
  `demo.wheel_cap` line and the ARMORY deal-line suffix. Rung 2 had also been orphaned since
  the Convert left the demo on 2026-08-25 — its only writer needed a zone-2 Hatch-bank window
  — so a player was capped at two wheel buys for the whole opening. The rule that replaces it
  is the Locked entry of the same date: anything affordable is buyable. `DemoRungTwoOpened`
  stays as the rack-opened announce latch (a save v13 key). Do not re-meter the wheel.
- **Default Barrage plant / root, and Rush as fire-rate personal (2026-08-31).** Recut as
  **Depot**: Dump is a following fire-rate aura with heavy slow (not rooted); Feed is a
  **toggle** clip regen at `0.22×` fire rate. Barrage×Rush compose (caster 3.0) is gone.
  Ability enum ids stay Barrage/Rush (never renumber). Do not restore the planted disc,
  rooted caster, or a hold-to-Feed without a stated reason this cut was wrong.

- **Independent-roll rewards.** A drop, win, or proc that can starve N attempts or
  clump three in a Wave. When chance is added, it is a pity timer, escalating
  weights, or pre-determination (`Design/3-Mechanics.md` §3.1). Not a gacha shop.
  Wave composition is already a table; crit stays frozen at 0. Stamped 2026-08-16.
- **Loss-stake "musical chairs" rotor.** A rotator cycled named stakes (Salvage
  Hold 50%, Weapon Rig 25%, Core Cache 0%, Lucky Break 75%), advancing once per
  Wave, and whichever was current when you lost was forfeited. Built, then cut.
  **Currency is simply kept** on the ship: already-converted Salvage is never
  taken back. `AwardKill` credits Unbanked immediately; only a Hatch-bank
  converts it to `MetaState.BankedSalvage`. `SalvageLedger.BankCleanExit/BankPodded` are gone.
  **Expiry, 2026-08-12 (Zo), for the field loop only:** unbanked Salvage since the
  last Hatch-bank is lost on a down. The ship bank is still kept. The rotor stays
  cut — this is a known pile the player authored, not a named random forfeit.
  Voluntary withdraw-from-bank into the Run is settled 2026-08-13: unspent withdrawn
  is lost on a down; spent stays. Pay-to-juice (more danger, more reward) is the in-Run
  spend, not pay-to-skip (`Brainstorms/missions-and-destinations.md`). Do not delay
  `AwardKill` in the shipped one-room fight.
- **Leave-anytime tether / escape spool.** You cannot cut the tether at will.
  `EscapeSpool.cs` and `RunPhase.Spooling` / `WaveDirector.SpoolAssault` are
  dormant, kept only so the enum stays serialization-stable.
- **Per-second siphon income.** `BaseSalvagePerSecond` is gone. Kills are the
  entire economy; the clock never pays. Prep earns nothing, by design.
- **The `RunStats` aggregation layer.** Replaced by plain log statements. Do not
  add counters, rollups, or a stats object.
- **A two-stream logger.** One file per session, one write path.
- **A mobility skill.** Mobility is not a mechanic in a 30 x 52 m bastion.
  **Expiry argued 2026-08-14 (M70), not stamped:** the M61-2 rescale is 152 × 76 m, and
  Recall is a priced burst rather than a permanent movement stat. Recall moved to the
  LEDGER slice in the 2026-08-15 protocol recut (`Brainstorms/boarder-protocols.md`);
  Zo stamps this or vetoes the row when that slice is cut.
- **Sold archetypes as CREW grammar.** Overturned 2026-08-15
  (`Brainstorms/boarder-protocols.md`): identity is a named, equipped Boarder Protocol,
  not an emergent read of free picks. The 2026-08-14 free-pick pools recut of M70 is
  superseded. Do not rebuild free-pick pools.
- **CREW QUARTERS first-loss lock.** Narrowed 2026-08-15, recut 2026-08-16: Default
  is a **0-salvage CREW claim** (Mk1 shape), not auto-owned. First-loss still does
  not lock CREW. Do not re-lock CREW behind a death. **The gate on FABRICATION's
  equipment slot and ARMORY Tiers is no longer a loss** — *Locked — the wheel opens on the
  first bank* (2026-08-18) re-keyed it to `MetaState.HatchConversions >= 1`, and
  `MetaUpgrades.UpgradesUnlocked` reads that. The 2026-08-16 wording above predates it.
- **Windowed fire-rate as a protocol verb.** Exception 2026-08-16 (M70 recut):
  Default's Barrage / Rush multiply `PlayerGun.EffectiveFireRate` for a window.
  The M21 wheel still owns baseline fire rate; this is not a spoke and not an
  always-on Passive. The 2026-08-15 "no damage-granting row" cut still bans
  Overclock-as-spoke, always-on dps passives, and crit sources.
- **A pause that stops the game.** `Time.timeScale = 0` is off the table (Zo, 2026-08-09).
  Pause in any multiplayer setting is a menu for one player while time keeps flowing, so the
  overlay is non-blocking from the start: duck audio, blur the frame, keep the sim ticking
  behind a semi-transparent panel. Building it blocking now is the retrofit the Sim seam
  exists to avoid (`Architecture.md`). Enforced as M35 criterion 31.
  **Beat-0 demo pause, 2026-08-17 (Zo):** the demo's Beat-0 pause is gated execution after
  dialog/interaction, not `Time.timeScale = 0`. The cut stands; the demo's earlier claimed
  timeScale exception is withdrawn.
- **SceneBuilder owning UI or gameplay hosts.** Rejected (M35). `SceneBuilder` owns
  Environment, SpawnPoints, Directional Light and NavMesh only. Stations, the player rig,
  `ShipController`, `GameSystems`, EventSystem and every UI canvas are authored scene
  content and survive Build All. The deleted mutators — `ShiftFrontEndMutate`,
  `ShiftShipUiImport`, `ShiftBastionUiImport` — are not rebuilt.
- **Unity asset files in Git LFS.** `ProjectSettings/EditorSettings.asset` sets
  `m_SerializationMode: 2` — Force Text — so every `.unity`, `.prefab`, `.asset`, `.mat`,
  `.anim`, `.controller` and `.meta` in this project is YAML. **They stay in git as text.**
  The instinct is to LFS them because `.unity` is the largest category on disk (22.7 MB,
  almost all of it Shift's six demo scenes), and doing so trades a diffable, mergeable file
  for an opaque pointer. LFS takes true binaries only — `.png`, `.dll`, `.mp4`, `.ttf`,
  `.ogg` and the rest of the list in `../.gitattributes` (Zo, 2026-08-10).
- **Asset Store vendor dumps in git.** The pack tree is not project history; the wrappers
  that reference it are. `Assets/Synty/` is gitignored (260 MB / 6728 files on disk,
  2026-08-14). Project wrappers that resolve Synty GUIDs live under `Assets/Packs/`
  (weapons, Boarder) and the dressed Ship hub `Ship - CoOp.unity` (`Ship - Demo.unity` was
  folded into it and no longer exists).
  After clone, import
  [POLYGON Sci-Fi Space](https://assetstore.unity.com/packages/3d/environments/sci-fi/polygon-sci-fi-space-pack-art-by-synty-138857)
  SKU **138857**, **v1.14.0**, to `Assets/Synty/PolygonSciFiSpace/` so those GUIDs resolve.
  `Assets/Packs/` stays tracked. Shift, Odin (`Assets/Plugins/Sirenix/`), and MCPForUnity
  stay tracked — the HUD and editor are built in those trees, not wrapped off them
  (Zo, 2026-08-14). Vendor dumps that are not yet wrapped are also ignored:
  `Assets/Packs/shooterweaponspackassets/`
  (2026-08-18). Keep `Assets/FaxLab3D.meta` tracked like `Assets/Synty.meta`.
  **`Assets/Packs/weapon_source/` narrowed 2026-08-19** when the five gun
  wrappers in `Packs/Weapons` were repointed at it (Synty stays only on the
  Cutter sword): its `.meta` files, `mat/`, `tex/*.meta` and README are
  tracked — they carry the GUIDs, externalObjects material remaps and
  globalScale=100 the wrappers depend on — while the binary fbx/png payload
  restores from `Downloads/shooterweaponspackassets.zip` per that README.
  **`Assets/FaxLab3D/` narrowed 2026-08-19**: its `Materials/` and `Textures/*.meta`
  are tracked (102 files, 316KB) because they carry hand edits the game depends on;
  the 534MB of textures, models and scenes stays out. See the block below.

  **The FaxLab3D pack's Shader Graph must keep its Built-In target.**
  `SG_SciFiRobotMat.shadergraph` ships three targets — `BuiltInTarget`, `HDTarget`
  and `UniversalTarget` — so it compiles here as authored and no conversion is
  needed (M87 read this from the package 2026-08-18). **The copy that reached the
  project had been stripped to `UniversalTarget` alone**, which is why every robot
  rendered magenta, and why an afternoon went into converting 24 materials onto
  Built-In `Standard` before the cause was found. Restored from the package
  2026-08-19. `Tools/lint-vendor/run.sh` reads the values below and fails the gate
  if the Built-In target goes missing again; changing a value here changes the
  check with no script edit.

```vendor-lint
pack=Assets/FaxLab3D/SciFiRobotCharactersPack
shader=Materials/SG_SciFiRobotMat.shadergraph
required_target=UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInTarget
```
  **NavMesh `.asset` files are binary** and ride LFS (`*NavMesh*.asset` in `.gitattributes`);
  treating them as Force Text YAML produced phantom rebake diffs (2026-08-18).
- **`core.autocrlf` as the line-ending fix.** Rejected in favour of `.gitattributes`. The
  config setting lives on one machine and does not travel with the repository, so the
  141-phantom-modification problem returns on the next clone or on CI. The repository is the
  only place the rule can live (2026-08-10). This machine's Git for Windows still ships
  `core.autocrlf=true` in the system config — override with **local**
  `git config core.autocrlf false` (and `git lfs install --local`) so it cannot fight
  `.gitattributes` (2026-08-18).
- **The VK-7 Assault Rifle** (2026-08-06). Shipped disabled in M11 and kept "until a weapon
  polish pass"; the pass arrived and it is not wanted. `WeaponId.Vk7Rifle` **keeps its value
  so old saves deserialize, and the id is never reused**; the row leaves `ShopWeapons()`,
  `All()` and `IsAvailable`, and its reference row leaves `Design/4-Data.md`. The roster still
  needs a weapon at the rhythm extreme — huge magazine, punishing reload — but that is a new
  row, not this one.
- **Naming a weapon before it has an identity in play.** Unshipped catalog rows carry
  codenames (`PRI-C`, `UTL-B`) until play earns them a name. A name given early has to be
  defended later and fixes the fiction before the mechanic has earned one.

- **Netcode for GameObjects is the networking library** (Zo, 2026-08-10). **Chosen on the
  criterion that the code will be AI-written, so the material a model can read and reason
  against is worth more than raw library quality.** On that criterion NGO wins decisively:
  it is first-party, so its documentation is Unity's versioned manual; and **Boss Room is a
  complete, Unity-authored, 4-player co-op reference game** an agent can read end to end
  (`https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/samples/bossroom/architecture.html`).
  Nothing else in the field has a canonical implementation of this genre's exact shape.
  Lethal Company is the shipped existence proof — solo developer, Unity, 4-player PvE co-op,
  host authority, 10M+ copies.
  **What this costs, stated so it is not a surprise:** the internet path is Unity Relay on
  the existing UnityTransport (`M60`). Free tier is 50 average monthly CCU and 150 GiB
  egress; above that it is metered. Steam / Facepunch is deferred until a real Steamworks
  appid exists (`M64`). **Transport is the most swappable layer in the stack**, so swapping
  later is not a blocking problem.
  **Rejected: FishNet**, which was the pick until the AI-authorship criterion was raised. Better
  transport ownership (its Steam transports are by the netcode's own author) and no metered path,
  but thinner documentation and no reference game of Boss Room's scale.
  **Rejected: Photon Fusion and Quantum.** Both meter concurrent users, and below 500 CCU the cap
  is hard — real players get disconnected until you pay. Quantum additionally mandates
  deterministic fixed-point simulation, a constraint this project does not have.
  **Rejected: PUN2**, in maintenance mode with no further feature updates.
  Nothing in M45 depends on this choice; the retrofit transfers between all of them.

**The criterion generalises.** When an agent writes the code, weight a dependency's
documentation, version stability and the existence of a readable reference implementation above
its feature list. A library the model writes correctly on the first attempt beats a better
library it writes subtly wrong.

## Locked — co-op down (Zo, 2026-08-13)

A client Boarder at 0 HP does not end the host's Run (`M49`, `endsTheRunOnDeath` false
on the avatar). Get-up is a wait on the Ship, then embark on the next hatch-defense Wave
via NAV MAP or pause (`Milestones/3. completed/M57-the-downed-player-waits-on-the-ship.md`);
built 2026-08-15. Solo and host: no in-Run respawn.
**Amended 2026-08-17 (Zo):** a dead player may buy back into the Run while a bank window is
live — see *Locked — return to Ship at a surviving bank* below. Wait-then-embark stays;
buy-back is the new option. Solo and host: still no in-Run respawn (host is the open
question recorded there).
**Amended 2026-08-24 (Zo, `M98` recut):** one respawn rule replaces the windows.
Every dead seat — guest, host, **solo included** — may respawn during a wave while a
shared per-seat pool has charges, and at any wave break without limit, each respawn
costing `Respawns.BaseCost × (taken + 1)` from the **realm's pot** (the pot is the
world's, `M96` — the 2026-08-17 "their own banked Salvage" wording was already
superseded by that ruling). The free Hatch-bank Prep embark and the flat buy-back are
retired. Host and solo death enters a DOWN state while the offer can be honored;
otherwise Lost exactly as before — a fresh realm's pot is 0, so the demo's first loss
is unchanged. The old host-death open question is closed by this: the host respawns
from the DOWN overlay, and host migration stays unbuilt.
**Amended 2026-08-24 (Zo, same day, superseding the purse and the pool):** a respawn is
paid out of the **dying seat's own Unbanked**, not the realm's pot, and **the remainder
of that carry survives the death with it** — the fee is the whole price. The shared
charge pool is retired with it (`Respawns.PoolPerSeat`, `RunSim.RespawnStock`): the carry
is the ration, so a mid-wave return and a break return price identically. Nothing else
may pay — not the bank, not another seat — so a seat carrying less than the price dies
for good, and banking everything is what makes you mortal. Death still wipes the pile on
the path where no respawn is bought.

## Locked — Bastion kit geometry (Zo, 2026-08-13)

Playable Bastion floors and walls are **kit segments** (`BastionSegment` /
`Assets/Prefabs/Bastion/Kit/`). No freestyle floor plates. Build All does not regenerate
bastion Environment geometry; NavMesh rebake and enemy-prefab refresh only. Sketch emit
and hand authoring compose rooms; Soft-lock doors stay parked.

## Locked — Bastion fight HUD corner starting point (Zo, 2026-08-10)

The four always-on fight cards under `Canvas - Dynamic` are the
baseline, plus the top-centre Hatch host added by M36. Agents extend these hosts; they do
not replace them with homemade bars or ghost Shadow chrome. Shift package rows (Icon Border
+ Slider + Text) stay intact. The scene was `Bastion.unity`, deleted 2026-08-24; the hosts
live in `Bastion_Coop.unity` and `Range.unity` now.

| Host | Corner | Size (canvas) | Contents |
|---|---|---|---|
| `Hud Profile` | TL `(40, −40)` | 360 × 96 | Shadow, **Profile Icon**, **BOARDER** (Profile Level), Protocol Line. EXP stays inactive |
| `Hud Score` | TR `(−24, −40)` | 400 × 96 | Shadow, Score Salvage, Wave Counter (VLG) |
| `Hud Hatch` | Top centre `(0, −24)`, anchors `(0.5, 1)` | 360 × 72 | Shadow, Shift **Hatch** row (Icon Border / Slider / Text) + value `hp/max` |
| `Hud Vitals` | BL `(135, 24)` | 400 × 150 | Shift **Health** + **Stamina** rows, VLG. Ability G/H slots stay on the bottom-centre protocol bar |
| `Hud Weapon` | BR `(−24, 24)` | 400 × 150 | Shadow, Weapon Name, Weapon Ammo, Shift **Reload** row (same bar recipe as vitals), VLG |

`BastionHudView` wires those hosts. EXP-as-slider and further polish are manual on top
of this layout — not a reason to rebuild the corners. Hatch integrity is always-on during
Tethered; the left-drawer Armor/Hatch bar is retired.

## Locked — Ship HUD corners and station terminals (Zo, 2026-08-10)

**Ship always-on HUD** = Bastion fight corner hosts **minus `Hud Vitals`**, pulled by
`Object.Instantiate` from the fight scene, not by hand-cloned YAML. The one-shot that did
it (`Gatekeeper/HUD/Pull Bastion Corners Into Ship`) and its whole menu root were deleted
on 2026-08-11 — see `Architecture/README.md` — and `Bastion.unity` went with the scene set
on 2026-08-24; what is locked here is the layout, not a tool to re-run. No always-on
shortcut strip. The old text strip
(`Title` / `Salvage` / `Objective` / `Directory` / `Shortcut Keys`) is not the baseline.

| Host | Corner | Size (canvas) | Ship contents |
|---|---|---|---|
| `Hud Profile` | TL `(40, −40)` | 360 × 96 | Shadow, Profile Icon, **BOARDER**, Protocol Line |
| `Hud Score` | TR `(−24, −40)` | 360 × 96 | Shadow, Salvage, Wave + **runs** |
| `Hud Weapon` | BR `(−24, 24)` | 320 × 72 | Shadow, primary + secondary names (no Reload) |
| *(no)* `Hud Vitals` | — | — | Omitted on Ship |

No slogans or embark/objective commands on the corners — those belong in station
panels. Crosshair and console interact prompt stay. `ShipHudView` wires the three
hosts; `ShipHud` pushes meta into those bindings.

**Ship terminals** (ARMORY, FABRICATION, CREW QUARTERS, NAV MAP): walk the ship;
look at a console and Interact (`Controls.Down` in `ShipController.Update`) opens
that station's full-screen menu — `TryOpenStationFromInteract` →
`OpenStationPanel`, the same open path that painted Armory from anywhere; the
camera ray picks which console (Interactor still drives the HUD prompt only).
Cloned from **Main Menu (Desktop) Settings** (`Assets/Shift - Complete Sci-Fi
UI/Scenes/Main Menu/Main Menu (Desktop).unity`) — Background + Top/Bottom chrome
kept; Categories / Preview / top Button List stripped. The one-shot that stripped them
(`Gatekeeper/HUD/Strip Settings Shell Onto Stations`) died with that menu root on
2026-08-11; the stripped shell is what is locked. Only that menu takes input.
Esc or Close returns to walk. **Body rows no longer clone.** Panels are authored, the
`Templates` folders were deleted with the runtime body, and `StationSceneGate` now fails a
scene where one survives — a leftover `Templates` means an unfinished migration that would
still be cloned from.
Panel copy stays at the basic use case (NAV: wave + EMBARK, plus READY and the countdown in a
party; CREW: the Default, VEIL and FORGE claims, with AEGIS and LEDGER as Coming Soon stubs —
`M94` shipped the two specialists; FAB: hatch / equipment / payload; ARMORY: slot + wheel +
catalog).

## Locked — title (Zo, 2026-08-16)

Player-facing title is **Gatekeeper's Protocol**. Identifier, namespace, asmdef, menu
root and new log prefix are `GatekeepersProtocol`. Unity `cloudProjectId`
`3b96df23-b5f0-499c-998f-86d5643d6d88` is kept. `gkb_` sentinels and tool prefixes stay.
GitHub repo name is unchanged in this pass. Bastion is the place, Gatekeeper is the
enemy, Boarder Protocol is the CREW sheet.

## Locked — the demo loop (Zo, 2026-08-17)

Recorded here because the demo folder previously carried sanctions this file did not.

- **`WaveDirector.MaxWaves` is a cap per terminal interaction:** one terminal trigger
  launches up to MaxWaves consecutive Waves. It is **not** a per-Run Wave budget. The
  rule row in `Design/3-Mechanics.md` §3.1 is restated to match.
- **The loop unit is a zone:** the zone's terminal Waves, then the hatch-bank Wave.
  Surviving the bank's timer window unlocks the zone's door; a failed bank never
  unlocks. The bank is locked until the zone's terminals are complete — callable
  anytime, unbanked accumulates unknowingly with no reward.
- **Post-loss:** the deepest terminal of a zone retriggers the zone's composite — all
  its Waves consecutively — by default. The player may elect a partial start (e.g.
  Wave 3 of 5), clear the remainder independently, then bank.

Wave counts are tunable, not fixed by this entry.

## Locked — return to Ship at a surviving bank (Zo, 2026-08-17)

**Death is no longer the only thing that ends a Run early.** A player who survives a
hatch-bank window may return to the Ship: held interact at the Hatch, available until
the next Wave is triggered. The Run ends **Docked**, not Lost; banked Salvage and door
state persist via the existing successful-bank snapshot. This is one window, not a
bail-out lever — the leave-anytime tether cut above stands, and outside a surviving
bank window down and breach remain the only enders.

- **Parked intent, not spec:** when the juice/multiplier mechanic lands, returning to
  Ship early loses or reduces the salvage multiplier — mitigating risk has a price.
- **Co-op buy-back:** a dead **guest** may buy back into the Run only while a Hatch-bank
  Assault is live (`WaveMode.HatchBank && AssaultState.Assault`), rejoining at the Hatch,
  paying `HatchBank.BuyBackCost` from their own banked Salvage. Free wait-then-embark
  stays on Hatch-bank Prep. Guest wallet is report-and-ask (client-authoritative):
  friends-only (`../Marketing/gtm.md`). Banked Salvage does not move into the sim.
  Door state stays HOST-ONLY; `M95` criterion 9 owns replicating it. **Owned by `M90`.**
  **Superseded 2026-08-24 by the `M98` recut** — see the amendments under *Locked — co-op
  down* above, and read the second one: one escalating-cost respawn rule for every dead
  seat, solo and host included, paid out of **that seat's own Unbanked** with the remainder
  surviving the death. The realm pot and the shared charge pool were the first version of
  the recut and were retired the same day; `Respawns.Refuse` and `RunSim.TryRespawn` carry
  the shipped rule.
- **Open — needs Zo's call, recorded not resolved:** a dead HOST rejoining collides
  with host-death-ends-the-run (`M49`), which is architecture-driven. **Closed
  2026-08-24 by the `M98` recut:** the host respawns from the DOWN overlay while the
  run continues; host migration stays unbuilt.
- **Demo consistency:** the demo's two authored losses are unclearable windows, so
  return-to-Ship cannot skip the unlock deaths; the wheel unlock key — the first pod
  ejection — still holds. *(Superseded 2026-08-18 — see* Locked — the wheel opens on
  the first bank *below: the key is the first Hatch conversion, which those unclearable
  windows still author.)*

## Locked — the wheel opens on the first bank, not the first loss (Zo, 2026-08-18)

`MetaUpgrades.UpgradesUnlocked` was `PodEjections >= 1`, and `RunSim.EndRun` only
increments `PodEjections` when the Run did not dock — so once *return to Ship at a
surviving bank* (above) shipped in `M77`, a player who kept surviving their bank
windows and docking could never unlock Tiers or the equipment slot. Skill locked the
deepest progression track, which is belief 2's opposite; belief 1 says a lost Run
still moves you forward, not that you must lose to move forward
(`Design/1-Experience.md`, belief-1 row rewritten in the same change).

**The wheel spends salvage that came through the Hatch, so it opens when salvage first
comes through the Hatch**: `MetaState.HatchConversions >= 1` (save v11), written on the
first credit of a bank window — the same event, stated honestly, firing for the player
who banks and docks as well as the one who banks and dies. A player who banked nothing
has nothing to spend. `PodEjections` keeps its own job (endings) and is no
longer a progression gate; nothing else in the demo family may key a
progression gate to it (`M78`, Traps). `demo.second_loss` keeps its id;
its trigger is zone 2's bank falling (`M79`, recut 2026-08-19), not
`PodEjections >= 2`.

**Surviving pays a little, and only a little:** a Returned ending keeps
`HatchBank.ReturnedKeepShare` of leftover Unbanked instead of wiping all of it; Lost
still wipes. One number, generated into `Design/4-Data.md`, initial value small and
tunable after a logged Run — greed must stay carrying a bigger pile into fewer
extracts. Door-state persistence already rides the successful-bank snapshot and is not
double-counted. `M78`.

## Locked — the Primary opens when zone 2's bank falls, not on the second ejection (Zo, 2026-08-19)

Zone 2's three field Waves are survivable on the secondary kit. That zone's
bank is authored above that ceiling and below Primary + 1. The bank falling
is the loss that **sells** the Primary (rung 2: Primary row + 2 buys outside
the secondary cap), whether that ejection is the save's first (zone 1 was
docked) or its second. `PodEjections` counts endings. It does not open the
row. A Lost during zone 2's field Waves is a loss and is not this gate.

`demo.second_loss` keeps its id; the trigger is Docked after this bank fell.
Copy is draft-Zo and must not require a prior ejection. M76's record that
zone 1's first bank is authored-unclearable is not rewritten here. `M79`.

## Locked — a dev-tainted session may close a milestone gate (Zo, 2026-08-20)

A `[Dev]`-tainted Run can close a milestone's playthrough criterion. The previous
rule — never close a gate with a tainted log — held the demo family's gates
hostage to a clean run of a beat whose whole point is that it takes several Runs
to reach, and the dev checkpoints exist precisely so that beat can be reached in
seconds. Blocking closure on an untainted run bought confidence nobody was
short of and cost milestones that were built and sitting.

**What this does not change.** Taint still gets **stated**: a close that rode a
checkpoint says which checkpoint and that the Run was tainted, so the evidence can
be re-read later for what it is. And taint still disqualifies a Run from being a
**play-data** entry in `GatekeeperZo Notes/playthrough.md` — tuning readings,
balance verdicts and "is this fun" still need a clean Run. The relaxation is about
*proving a mechanism works*, not about *judging how it feels*.

First applied closing `M79`.

## Locked — a play stamp needs a replayable slice (Zo, 2026-08-22)

A milestone may not **require** a playthrough stamp to close until a **replayable
slice** exists that exercises the criteria without a full economy Run (the range
`M99`, a named F12 beat, a PlayMode fixture that paints the Game view — named in
Verification). Disk criteria, EditMode, and PlayMode harnesses still close what
they can see. Playthrough paths stay written in the milestone; they are not the
blocking gate while the only route is "embark and play a Run."

Cause: weapon-feel and hit-feedback gates (`M82`, `M84`, `M93`, later `M85` /
`M66`) were stacking unpaid play stamps with no short loop to stamp against.
`M99` is the slice that unlocks those stamps — not an optional convenience after
them.

## Locked — dev-assisted runs are play data; context divergence is the taint (Zo, 2026-08-24)

**Rescinds the play-data exclusion in the 2026-08-20 ruling above.** A Run that used dev
tools is **dev-assisted**, not disqualified: the owner rarely plays the raw game, so
excluding assisted Runs from tuning and balance readings excluded nearly all the data
there is. What invalidates a Run as a measurement is **context divergence** — completing
a beat with materially more resources, kit or progression than the beat calls for — not
the fact that a dev tool was touched.

The rules that replace the exclusion:

- **A Run's context is stamped, not guessed.** A Run launched through an F12 beat
  (optionally with a loadout configuration) starts context-exact by construction — the
  checkpoint *is* the context-constraint button, and testing snippets without re-earning
  progression is its purpose. The log records which beat and configuration.
- **Dev commands split into context-preserving and context-breaking.** Loading a beat,
  the live reset, and read-only overlays preserve context. Granting salvage, swapping
  loadout mid-Run, invulnerability, clearing gatekeepers, and refills **break** it; the
  first such command marks the Run's data context-broken in the log, and a
  context-broken Run is what "dev-tainted" now means for data.
- **Measurements segment by stamp.** Playthrough-derived bounds (kill rate, reload
  cadence, miss rate) read dev-assisted context-exact Runs freely and group them by
  beat + configuration — like-for-like — while context-broken stretches are dropped.
- Unchanged: taint still suppresses saving, a gate close still states what it rode on,
  and "is this fun" is still decided by playing, not by the numbers.

## Locked — the save refuses what it cannot honestly read (Zo, 2026-08-21)

From save version 13, a document written by an older build **does not load**. The game says
so by name and stops, rather than reshaping the file into something the current build can
open.

**This is new policy, and it replaces a rule that was never written down.** The realm and
Boarder milestone cited this document for "bump the schema, rename the keys, never write a
migration shim". No such rule was here. The opposite was: `WeaponId.Vk7Rifle` keeps its
value expressly *so old saves deserialize*, and every version from 3 to 11 shipped a
migration gate in `FromSaveData`. The rule is real now because it was argued for on its
merits, not because it was already binding.

**Why the break is worth it here.** One progression document became two — a Boarder that
travels with the player and a realm that stays with the world. A migration would have to
decide, for a single-document save, which half of it belonged to the world. There is no
honest answer: the same save records a pot the world owns and weapons the player owns, and
the whole reason the split exists is that the old document could not tell them apart. A
wrong guess does not fail loudly; it hands the player a corrupted campaign that looks fine.

**What this does not license.** It is not permission to break saves whenever a schema is
inconvenient. The bar is that migration would have to *invent* a fact the old document never
recorded. A field that is merely added, renamed, or re-keyed still migrates, and a missing
key whose absent value is also its correct default still needs no version bump at all.

The version history stays in `Sim/Meta/SaveFormat.cs` even though none of it executes any
more: it is the record of why keys are spelled the way they are.


## Shipped

- **Deliver-loot-before-the-run-ends.** The *spirit* of Gears of War's active
  reload, not its form: a neutral default, and an opt-in that pays more and makes
  the Run harder. Explicitly **not** a QTE timing-bar mimic. Juice is that opt-in:
  Unbanked spent at a trigger to raise intensity. Shipped M55.

## Deferred, with intent

- **Multiplayer.** Unity Netcode for GameObjects, **host-client, server-authoritative
  simulation** (the host's `RunSim` is the one that counts), with **client-authoritative
  own-movement** — correct for PvE with no anti-cheat requirement, and NGO names the
  mechanism (`AnticipatedNetworkVariable<T>` / `AnticipatedNetworkTransform`). NGO 2.x's
  distributed-authority topology is rejected deliberately: it is documented as unsuitable
  where one machine should govern the simulation. First slices shipped over UnityTransport
  loopback (M48, M49). Internet path is **Unity Relay** on that same transport (`M60`):
  anonymous sign-in, join code, free at 50 average monthly CCU. Steam / Facepunch deferred
  until a real appid. Three players on one hull (host plus two guests); six is the recorded
  intent, priced as a tuning pass on spawn placement and HUD layout (`M65`) — not
  architecture.
- **Co-op salvage.** No single wallet. Salvage splits **equally among connected
  players** at award, host included; **equal over kill- or damage-weighted**
  because this game has jobs that are not shooting (repair, escorting a Thief,
  clearing a Source, placing a turret) and a contribution split makes doing them
  cost money at the moment doing them is correct. Damage weighting also pays the
  best-geared player most, which is the host. Every player carries their own
  unbanked pile and loses it on a down. At conversion a player banks at most what
  their own `MetaState.DeepestWaveReached` justifies, excess lost not
  redistributed; that cap rises from co-op clears as well as solo ones. Players
  bring their own loadout and inherit nothing. **The principle: co-op gets you
  further, not richer** — salvage per player per hour stays roughly flat as
  players are added, and what co-op buys is depth. Zo, 2026-08-14; `M65`.
  **Built, and no longer the deferred half of this row:** `RunSim.AwardKill` calls
  `PlayerWallets.AwardSplit`, which divides the award across the connected seat count and
  gives slot 0 the remainder; `RunSim.SetSlotDeepest` carries each guest's own progress for
  the banking cap, and `WipeUnbankedSlot` is the per-seat loss on a down. What is still
  deferred here is the seat count above three and the tuning pass on it.
- **Shared Ship lobby (Zo, 2026-08-15).** Friends session: Ship is a shared lobby;
  party lead (host) is ship owner for NAV / EMBARK; each player still owns their
  own resources (above). Party-wide scene loads (ready-up + declared countdown).
  NGO `EnableSceneManagement` on for that topology — expires M58/M60's
  "both load Bastion themselves" rule for the friends path. M64's solo-Ship stamp
  failed play. `M72`. M57 additive Ship remains down-wait only, not the party lobby.
  Landed: DDOL `NetSession`; `ConnectTimeoutMs` 10000. **The entry point moved on
  2026-08-22** — hosting is REALMS *OPEN TO FRIENDS*, not a menu HOST; see *Locked — you
  host a realm, not a session* below. JOIN is still on the menu. Ready-up and the countdown
  are `NetPartyLobby`; the lead's EMBARK and the guest's READY are both `MapStation`.
- **Ship subsystems** (Reactor, TetherCore, Engines, LifeSupport) return with the
  bastion outfitting layer. `ShipSystems` stays the aggregate so the wiring keeps
  its shape.

## Designed, not built — the 5-skill system

Moved to `Design/3-Mechanics.md` section 3.2 on 2026-08-08: it is a design, and player
rulemaking authority lives there.

## Retunes that already happened

- **MR-35 damage 16 → 18 (Zo, 2026-09-02).** #12; higher tiers are wrong because the rifle's
  rate, clip and reload are the 2026-08-27 assault-rifle recut and stay. 18 is the highest whole
  number that keeps the deepest Thief out of one magazine (486 against 520) and the field
  out-delivering a T0 rifle before the last wave (`SiphonAndWaveTests`); it takes one shot off
  waves 1, 2, 3 and 5 of the Default ladder. The PRI-C dps shadow and the BR-12 kills/s parity are
  given up; each row keeps its own axis. Derivation in `Design/4-Data.md`. Play-unconfirmed.
- **MK-1 Sidearm 30 → 50 damage, 3.0 → 1.4 shots/s (Zo, 2026-09-02).** #12; a higher tier is
  wrong because the kind rules and the roster are Zo's own 2026-08-27 calls and only the number
  under the starting gun was unowned. Zo's 2026-08-16 verdict asked for one-shot first-session
  fodder; the 08-27 `BaseEnemyHp` 30 → 50 raise undid it silently and the pin was renamed to
  match. Damage one-shots a Wave-1 Default and two-shots Wave 2. The rate is solved so the
  MR-35 still out-kills the sidearm on Wave-1 kills/s (1.16 against 1.12) — the 08-27 guard
  that no Primary loses to the free gun on its own axis stands; at 1.8 it did not. Sustained
  dps 59.0 → 56.2. Derivation in `Design/4-Data.md`, MK-1 section. Feel unplayed.
- **Bastion kit re-scaled to station rooms (Zo, 2026-08-14, `M61-2`).** The M47
  footprint lock is waived: multiples are now metres on `BastionKit` scaler 2 —
  Half 16 m, Full 26 m, OpenArea 42 m, ControlRoom 94×47 m (multiple 47, aspect 0.5)
  — sized so a room is a place a Boarder walks through and the whole station reads
  as one huge lost structure. Sketch room centres are world metres, so the scaler
  and the sketch move together (`Tools/bastion-sketch/bastion-defense-layout.txt`).
  The old 5×8 scaler produced one aircraft-carrier diagram, not rooms; play feedback,
  same day.
- **Global +5%/level — attributes and ship tracks only.** Every CREW QUARTERS and
  FABRICATION track is `base * (1 + 0.05*level)`, CritChance additive at the same slope.
  The old per-track slopes (+4 damage/level, +20 HP/level, +0.15 repair-speed/level,
  0.03 crit/level) are gone; uniform ramps keep the shop legible. **Weapons left this
  curve**: damage, fire rate and clip moved out of `MetaUpgrades` into `WeaponCatalog` +
  `WeaponTier`, which are per weapon and on a linear cost ramp. Two curves in one shop is
  deliberate — weapons are the progression and must stay reachable; attributes were a side
  investment and should get expensive. **Crew tracks deleted save v8 (2026-08-15):**
  Vitality / Stamina / Scavenging / Luck refunded. Hatch Armor is the remaining +5% track.
  CritChance is frozen at 0. CREW QUARTERS equips Boarder Protocol.
- **Fire rate becomes upgradeable (M21, 2026-08-08).** Cadence was previously frozen as
  the weapon's identity (`WeaponTier` never scaled fire rate). The wheel keeps identity via
  **ring position** instead: changing cadence costs the far side of the ring. The flat
  Tier curve (`damage ×1.25^t`, `clip ×1.15^t`) is replaced by the 5/3/3/0/0 focus spread.
- **Five Waves, draw-triggered.** `MaxWaves = 5`, no Wave timer;
  deep start chains Waves 1..N with a 3 s inter-Wave gap.
  `WaveSpec.timeUntilNextWave` is obsolete and always 0.
- **Spawn-gap widened (M10)** to `max(0.4, 4.2 - 0.20*wave)` — 4.20 / 4.00 / 3.80 —
  so arrivals no longer outrun the Hatch budget on Wave 2. Earlier softens (`-0.13/wave`
  from `-0.55`, then dropping the dial's gap term) were not enough after the horde and
  swing-landing fixes.
  **SUPERSEDED 2026-08-27 — the sign is now positive.** `max(0.4, 4.2 + 0.20*min(wave,
  MaxWaves-1))` — 4.2 / 4.4 / 4.6 / 4.8 / 5.0. M10's name was accurate for Wave 2 and
  misleading beyond it: the gap shrank as the Run escalated, so delivery time stayed near
  30 s while the roster grew 18 → 66 and arrivals climbed 0.86 → 2.16/s. Group width was
  rising 3 → 7 over the same span, so the two compounded rather than offsetting. A logged
  five-wave run ended `lost` at Wave 5 with 103 bodies alive. M10's intent — arrivals must
  not outrun what the player can answer — relocates here intact; only its sign was wrong,
  and it was wrong because it rested on a kill-rate claim that later play data falsified
  (see `Docs/Design/2-Dynamics.md`, B1).
  **SUPERSEDED AGAIN 2026-08-27, hours later — the base and the slope both moved.** The live
  formula is `max(0.4, 7.3 + 0.40*min(wave, MaxWaves-1))` — 7.30 / 7.70 / 8.10 / 8.50 / 8.90
  (`WaveDirector.SpawnGapBase`, `WaveDirector.SpawnGapPerWave`). The sign flip above was
  solved against the `18+12i` roster; the fewer-and-tougher pass cut that roster to `11+7i`
  the same day and raised every kind's HP by 5/3, which shortened every delivery window by
  ~40% while group width stayed 3/4/5/6/7. Body ARRIVALS barely moved, so nothing in a
  bodies-per-second reading showed it, but each body now carries 5/3 the HP: HP pressure per
  second rose by that factor, Wave 5 from 114 to 203 HP/s. The gap was re-solved to hold HP/s
  at the pre-cut figures (25.1 / 40.9 / 69.4 / 82.8 / 113.9 against 25.7 / 40.9 / 61.6 /
  92.8 / 114.4). **The roster and the gap are one lever** — moving either alone moves HP/s,
  which is the trap that made this a same-day second supersession. Derivation in
  `Docs/Design/4-Data.md`, *Waves and intensity*.

## Locked — hatch doors are assigned by seat count (Zo, 2026-08-30)

`ShipHatches.PackFor` is the rule: the i-th open door belongs to the i-th seat in
ascending slot order. Prompts `HATCH n — yours` / `SEAT n` / `sealed`. Recomputed
at embark, FOLLOW, and the break. No player claim or swap.

Overturns Demo-Coop 2026-08-20 "nobody assigned". That ruling never reached the
tree; `M92` shipped assignment into the packed wire, and tearing it out orphans
the format. Combat is still one Hatch HP pool — assignment names a hull door, it
does not split the fight.

**Cut with this lock:** a player verb that claims or swaps a hatch.

## Locked — you host a realm, not a session (Zo, 2026-08-22)

Hosting belongs to the realm chooser and nowhere else. The Main Menu's HOST button is gone.

The menu could not name what it was hosting. `HOST` called `LoadCampaignForNet`, which resolves
whichever realm was opened last, so the world a party landed in was decided by history rather
than by the player. That is backwards once the realm is the centre: a realm is the world you
and everyone you invite share, and choosing it is the first decision, not a side effect.

The chooser is the only screen that knows the realm **and** the Boarder, so **OPEN TO FRIENDS**
sits beside **ENTER** there — the same decision, taken with the door open or shut. The Relay
code is asked for first and shown large with COPY beside it; the realm is entered and the
session started only on **TO SHIP**, so backing out has changed nothing. The LAN fallback moved
across with it, because Relay being down must not take co-op with it.

**JOIN stays on the menu.** It is the one door that leads to somebody else's world and needs no
realm of your own. Its own-code branch — type the code you were just given and host instead of
joining — went with hosting, since no code is allocated on the menu any more.

**Not settled here**: the code is still a Relay *session* code. It is minted per session and
dies with it, so "a code is for a realm, and a player admitted once keeps a slot they can
return to at their leisure" is not what this builds. That needs the realm to have an identity
and an access list that outlive the session, which is `M100`.

**`M100` built it (2026-08-24).** `RealmShareEnvelope` carries `RealmId`, `OwnerId` and a
`MemberIds` / `MemberNames` admission list, with a heartbeat lock so one machine holds a
realm open at a time; `CloudRealmStore` is where a copy lives between sessions. The Relay
code stays per-session — a code still opens a door rather than being one — but admission
now outlives it. Playthrough not yet stamped.

## Locked — kills bank as they land, and the Convert leaves the demo (Zo, 2026-08-25)

**The rule: it must be impossible to die with no progress to show for it.**

The 2026-08-25 playthrough is the evidence. A fresh realm on an MK-1 T0 cleared Wave 1 outright
— 18 of 18 — died twice on Wave 2, and the Run summary read `gross 0, repair 0, net 0`. Thirty-
four bodies and a cleared Wave, and the realm ended exactly where it started. The two things
that banked salvage — reaching a terminal, and surviving a Convert window — both sat past the
point where a starting kit dies, so the whole Run was a bet the player could only lose.

**A kill is credited the moment the body falls**, at face value, into the realm's bank. The
carried pile still exists, but it fills only by a deliberate Withdraw at the Hatch: nothing is
at risk that the player did not choose to put at risk. Death still wipes a staked pile, which
is what keeps a Withdraw a real bet, and can no longer reach the bank behind it.

**The Ante and the respawn fee charge the bank.** There is one pot now and everything costs
from it, so raising the Ante spends real take-home progress rather than money that was never
safe. That is a heavier decision than it was, deliberately.

**The Convert window leaves the demo.** With nothing unbanked by default it could not start, and
it was the demo's engine for two authored losses. It survives in code because Master Control's
finale runs on it, but no demo beat and no gate depends on it any more.

**So both gates it fed were re-homed**, and this is the part that actually answers the
playthrough:

* The wheel and the equipment slot were gated on `HatchConversions >= 1` — a counter only a
  Convert raised. Now `LifetimeSalvageEarned > 0`. Lifetime on purpose: spending down to zero
  must not re-lock the wheel.
* The Primary rack was gated on `DemoZone2BankLost`, resolved by zone 2's Convert. Now the
  Wave **reached**: `DeepestWaveReached >= DemoZones.EndWave(1)`. Reached, not survived — the
  player above died on Wave 2 holding 227 banked and was told "no buyable upgrades remain",
  because the only kit that could carry zone 1 sat behind clearing zone 1. **The Run that
  proves you need a Primary is now the Run that sells you one.** The legacy flag still opens
  the rack so existing saves keep theirs.

**Not settled here.** Whether the Convert should come back as an opt-in bet on withdrawn
salvage — the machinery is intact and the Hatch already prompts a Withdraw — or be cut from the
code entirely. Left standing, unused by the demo, until a playthrough asks for it.

## Locked — anything affordable is buyable, and the picker shows in the opening (Zo, 2026-09-02)

Playthrough, 2026-09-02: "after first loss only able to buy 2 upgrades despite being able to
afford more … I should always be able to purchase something I can afford. This rule violation
keeps the player from improving from failure." Belief 1 (progress through failure) outranks the
opening's rationed budget, so the budget goes (the Cut entry above). The wheel now refuses only
for the earned-Salvage lock, SEC-D's Ability spoke, the ten-purchase cap and the price, and the
exit standing (`DemoProgress.NoBuyableUpgradeRemains`) keys on affordability alone.

**The Primary rack opens on Salvage earned** (Zo, same day: "re-key rack opened when player
earns enough"): `DemoProgress.PrimaryRowsOpen` is `LifetimeSalvageEarned >=
PrimaryRackSalvage`, the cheapest available Primary's price read off the catalog. Lifetime,
like the wheel's own lock, so spending to zero cannot re-lock it and a granted pile opens
nothing. This supersedes the 2026-08-25 re-key (deepest Wave entered) and the 2026-08-19 lock
above (zone 2's bank), whose only writer the shipped loop never reaches. The rack-opened line
(`demo.second_loss`, id kept) plays on the same gate. `DemoZone2BankLost` is still written as
the record of zone 2 resolving; nothing reads it for the rack.

**And the prices went up** (Zo, same day: "then push up price"; #12): BR-12 900 → **1,500**,
MR-35 1,200 → **2,000**, PRI-C 1,500 → **2,500**, the 3:4:5 band kept. With the price as the
gate, 900 opened the rack on the first Boarding (Wave 1 alone pays 11 × 42 = 462 and Wave 2
adds 756). At 1,500 the cheapest Primary is one zone-1 clear (1,218) plus seven kills — the
second Boarding at the latest, and one the player fought for. Unplayed; the number is Zo's to
move, and moving it moves the gate with it.

Same session: "when I return to the Bastion I can't elect to start where I last left off." The
NAV MAP's start-Wave picker shows in the opening, solo and for the party lead, capped as before
by the deeper of Wave entered and Wave cleared (`RealmRecord.From`). What a pick does is
unchanged: any Wave past 1 restores the terminals already completed, so the Hatch offers the
next zone (`GameController.Start`, `StartZoneWaveFromHatch`); Wave 1 replays from the top. A
skip inside a zone is still the terminal retrigger's partial start. The beat-4 ruling that hid
the picker (2026-08-17) is overridden for the picker only.

## Locked — the terminal calls the last wave (Zo, 2026-09-02)

"Terminal press spawns final wave." The Hatch draws a zone's opening span; the first `T` at
the zone's terminal draws the zone's last wave into the live field; when that wave is down the
terminal arms and the next `T` shuts the zone off. The finale's grammar (kill the elite arms
the switch, the switch ends it) applied to every zone. What it replaces: a shutoff legal once
the zone's first wave had drawn, which the 2026-09-02 logs show is a 10-second sprint past
wave 1 in three Boardings of four, and the source of the stacked chains that followed. `M115`.
The Hatch stays the only start line; the walk out is still the objective; a zone still cannot
end by attrition.

## Locked — waves differ by rhythm, shape and finale, not by direction (Zo, 2026-09-02)

From the day's play: "the play felt static. No felt variance in waves." Three levers, one
milestone (`M116`), one commit each: the group gap becomes a seeded beat inside an unchanged
window (`WaveCadence`); waves are authored shapes (Mass, Pack, Convoy) instead of one ramp,
the zone's opening from one and its terminal-called last wave from another; and Master
Control is two waves, a quiet beat, then an escorted elite instead of five identical rosters.
What every lever holds still: the `11 + 7i` roster and its HP ladder, the debut gates, the
one-lane spine. Variance redistributes what a wave is, never how much of it there is — the
"thousands on screen" and idle-dial refusals stand. Direction is cut (above).

## Locked — the range is reached from a door beside the ARMORY, kitted from a panel, and draws waves (Zo, 2026-09-02)

"Gate it by a UI selection for Protocol and weapon (weapons will have stats player has
unlocked, for unlocked weapons player can use base level). We will need to move dev commands
to interactions in range. we should add spawn rooms and terminal to press and adjust waves."
`M117`. Three rulings inside that:

- **The way in is one door beside the ARMORY**, on the partition wall that closes its bay
  (x 7.5, z 24.8, 2.5 m to the station's left — the station's own wall holds the airlock, a
  window and the desk; a 2026-09-03 cut at z 17.5 sat in the airlock), not the FABRICATION
  coordinate the 2026-08-29 builder carried and never authored. Hold `E` opens a
  loadout panel; nothing loads until it is confirmed.
- **The range kit is chosen, not granted.** Protocol any of DEPOT, VEIL, FORGE; a weapon the
  realm owns comes with the wheel points it bought; a weapon it does not own comes at base and
  says so. This supersedes `M99`'s "everything owned, every weapon at full focus" (Zo,
  2026-08-22), which answered questions about a kit nobody owns. Tainted and unsaved as before.
- **The range draws waves.** `3-Mechanics.md`'s "never drawing a Wave" and `The-Range.md`'s
  "beyond six it is a Wave, and Wave behaviour is what the Bastion is for" are reversed: a
  wave terminal on the range draws any authored wave and shape through the real director,
  cadence, shapes and spawner, paying nothing and banking nothing. The standing-target cap of
  six stays behind the dev console. Dev-console range digits are retired for keys on the range
  board — one per dimension, live anywhere on the lane, printed on the readout. The first cut
  (posts on the marks, a console at the firing line, a terminal 90 m away) lasted one play:
  Zo, 2026-09-03, "I shouldnt have to walk to generate anything. Its a shooting range." The
  second (six keys: kind, distance, stand, clear, wave, shape) lasted one message; the board
  is Zo's dimensions of the same day — `Z` kind, `X` wave size (small 1, normal 11, large
  25), `T` draw, the breach fixed 20 m from the firing line — and the M99 cooldown bypass is
  cut: "no bypass. reset them on wave start."
