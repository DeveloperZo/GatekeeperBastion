# 05 — Decisions

Why the game is not the thing it used to be. Read before proposing a feature;
re-litigating a cut costs more than the cut saved.

## Cut — do not rebuild

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
- **CREW QUARTERS first-loss lock.** Narrowed 2026-08-15: the protocol is owned from
  the first session. The remaining first-loss gate is FABRICATION's equipment slot
  (and ARMORY Tiers). Do not re-lock CREW behind a death.
- **A pause that stops the game.** `Time.timeScale = 0` is off the table (Zo, 2026-08-09).
  Pause in any multiplayer setting is a menu for one player while time keeps flowing, so the
  overlay is non-blocking from the start: duck audio, blur the frame, keep the sim ticking
  behind a semi-transparent panel. Building it blocking now is the retrofit the Sim seam
  exists to avoid (`Architecture.md`). Enforced as M35 criterion 31.
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
  2026-08-14). Eight project files reference nine assets in it (`Assets/Packs/Weapons/*`,
  `Assets/Packs/Boarder/Boarder.prefab`, `Assets/Scenes/Ship.unity`). After clone, import
  [POLYGON Sci-Fi Space](https://assetstore.unity.com/packages/3d/environments/sci-fi/polygon-sci-fi-space-pack-art-by-synty-138857)
  SKU **138857**, **v1.14.0**, to `Assets/Synty/PolygonSciFiSpace/` so those GUIDs resolve.
  `Assets/Packs/` stays tracked. Shift, Odin (`Assets/Plugins/Sirenix/`), and MCPForUnity
  stay tracked — the HUD and editor are built in those trees, not wrapped off them
  (Zo, 2026-08-14).
- **`core.autocrlf` as the line-ending fix.** Rejected in favour of `.gitattributes`. The
  config setting lives on one machine and does not travel with the repository, so the
  141-phantom-modification problem returns on the next clone or on CI. The repository is the
  only place the rule can live (2026-08-10).
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

## Locked — Bastion kit geometry (Zo, 2026-08-13)

Playable Bastion floors and walls are **kit segments** (`BastionSegment` /
`Assets/Prefabs/Bastion/Kit/`). No freestyle floor plates. Build All does not regenerate
bastion Environment geometry; NavMesh rebake and enemy-prefab refresh only. Sketch emit
and hand authoring compose rooms; Soft-lock doors stay parked.

## Locked — Bastion fight HUD corner starting point (Zo, 2026-08-10)

The four always-on fight cards in `Bastion.unity` under `Canvas - Dynamic` are the
baseline, plus the top-centre Hatch host added by M36. Agents extend these hosts; they do
not replace them with homemade bars or ghost Shadow chrome. Shift package rows (Icon Border
+ Slider + Text) stay intact.

| Host | Corner | Size (canvas) | Contents |
|---|---|---|---|
| `Hud Profile` | TL `(64, −24)` | 320 × 64 | Shadow, **Profile Icon** (sprite), Profile Level, Profile Exp |
| `Hud Score` | TR `(−24, −24)` | 320 × 72 | Shadow, Score Salvage, Wave Counter (VLG) |
| `Hud Hatch` | Top centre `(0, −24)`, anchors `(0.5, 1)` | 360 × 72 | Shadow, Shift **Hatch** row (Icon Border / Slider / Text) + value `hp/max` |
| `Hud Vitals` | BL `(135, 24)` | 400 × 150 | Shift **Health** + **Stamina** rows, protocol G/H + Passive TMP inside the card, VLG |
| `Hud Weapon` | BR `(−24, 24)` | 400 × 150 | Shadow, Weapon Name, Weapon Ammo, Shift **Reload** row (same bar recipe as vitals), VLG |

`BastionHudView` wires those hosts. EXP-as-slider and further polish are manual on top
of this layout — not a reason to rebuild the corners. Hatch integrity is always-on during
Tethered; the left-drawer Armor/Hatch bar is retired.

## Locked — Ship HUD corners and station terminals (Zo, 2026-08-10)

**Ship always-on HUD** = Bastion fight corner hosts **minus `Hud Vitals`**, pulled with
`Gatekeeper/HUD/Pull Bastion Corners Into Ship` (`Object.Instantiate` from
`Bastion.unity` — not YAML theatre). No always-on shortcut strip. The old text strip
(`Title` / `Salvage` / `Objective` / `Directory` / `Shortcut Keys`) is not the baseline.

| Host | Corner | Size (canvas) | Ship contents |
|---|---|---|---|
| `Hud Profile` | TL `(64, −24)` | 320 × 64 | Shadow, Profile Icon, **LV** / **EXP** (same slots as Bastion) |
| `Hud Score` | TR `(−24, −24)` | 320 × 72 | Shadow, Salvage, Wave + **runs** |
| `Hud Weapon` | BR `(−24, 24)` | 320 × 72 | Shadow, primary + secondary names (no Reload; card matches Score) |
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
kept; Categories / Preview / top Button List stripped via
`Gatekeeper/HUD/Strip Settings Shell Onto Stations`. Only that menu takes input.
Esc or Close returns to walk. Body rows clone from each panel's inactive
`Templates` under the Settings `List` — that host must survive `ClearBody`.
Panel copy stays at the basic use case (NAV: wave + EMBARK; CREW: protocol + Passive pick;
FAB: hatch / equipment / payload; ARMORY: slot + wheel + catalog).

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
- **Shared Ship lobby (Zo, 2026-08-15).** Friends session: Ship is a shared lobby;
  party lead (host) is ship owner for NAV / EMBARK; each player still owns their
  own resources (above). Party-wide scene loads (ready-up + declared countdown).
  NGO `EnableSceneManagement` on for that topology — expires M58/M60's
  "both load Bastion themselves" rule for the friends path. M64's solo-Ship stamp
  failed play. `M72`. M57 additive Ship remains down-wait only, not the party lobby.
  Landed: DDOL `NetSession` from menu Host/Join; `ConnectTimeoutMs` 10000.
- **Ship subsystems** (Reactor, TetherCore, Engines, LifeSupport) return with the
  bastion outfitting layer. `ShipSystems` stays the aggregate so the wiring keeps
  its shape.

## Designed, not built — the 5-skill system

Moved to `Design/3-Mechanics.md` section 3.2 on 2026-08-08: it is a design, and player
rulemaking authority lives there.

## Retunes that already happened

- **Bastion kit re-scaled to station rooms (Zo, 2026-08-14, `M61-2`).** The M47
  footprint lock is waived: multiples are now metres on `BastionKit` scaler 2 —
  Half 16 m, Full 26 m, OpenArea 42 m, ControlRoom 152×76 m (multiple 76, aspect 0.5)
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
