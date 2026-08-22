# 05 — Decisions

Why the game is not the thing it used to be. Read before proposing a feature;
re-litigating a cut costs more than the cut saved.

## Cut — do not rebuild

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
  not lock CREW; FABRICATION's equipment slot (and ARMORY Tiers) remain the
  first-loss gate. Do not re-lock CREW behind a death.
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
  2026-08-14). Eight project files reference nine assets in it (`Assets/Packs/Weapons/*`,
  `Assets/Packs/Boarder/Boarder.prefab`, `Assets/Scenes/Ship.unity`). After clone, import
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
| `Hud Profile` | TL `(64, −24)` | 320 × 64 | Shadow, **Profile Icon** (sprite), Profile Level, Protocol Line (`G`/`H` READY/countdown), Profile Exp |
| `Hud Score` | TR `(−24, −24)` | 320 × 72 | Shadow, Score Salvage, Wave Counter (VLG) |
| `Hud Hatch` | Top centre `(0, −24)`, anchors `(0.5, 1)` | 360 × 72 | Shadow, Shift **Hatch** row (Icon Border / Slider / Text) + value `hp/max` |
| `Hud Vitals` | BL `(135, 24)` | 400 × 150 | Shift **Health** + **Stamina** rows, VLG. Protocol G/H readout is Hud Profile `Protocol Line` (always on) plus the Fight shortcut strip when the left drawer is open |
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
Panel copy stays at the basic use case (NAV: wave + EMBARK; CREW: Default claim + Coming Soon stubs;
FAB: hatch / equipment / payload; ARMORY: slot + wheel + catalog).

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
- **Open — needs Zo's call, recorded not resolved:** a dead HOST rejoining collides
  with host-death-ends-the-run (`M49`), which is architecture-driven.
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
