# 05 — Decisions

Why the game is not the thing it used to be. Read before proposing a feature;
re-litigating a cut costs more than the cut saved.

## Cut — do not rebuild

- **Loss-stake "musical chairs" rotor.** A rotator cycled named stakes (Salvage
  Hold 50%, Weapon Rig 25%, Core Cache 0%, Lucky Break 75%), advancing once per
  Wave, and whichever was current when you lost was forfeited. Built, then cut.
  **Currency is simply kept**: `AwardKill` banks each kill instantly and no
  ending moves Salvage. `SalvageLedger.BankCleanExit/BankPodded` are gone.
- **Leave-anytime tether / escape spool.** You cannot cut the tether at will.
  `EscapeSpool.cs` and `RunPhase.Spooling` / `WaveDirector.SpoolAssault` are
  dormant, kept only so the enum stays serialization-stable.
- **Per-second siphon income.** `BaseSalvagePerSecond` is gone. Kills are the
  entire economy; the clock never pays. Prep earns nothing, by design.
- **The `RunStats` aggregation layer.** Replaced by plain log statements. Do not
  add counters, rollups, or a stats object.
- **A two-stream logger.** One file per session, one write path.
- **A mobility skill.** Mobility is not a mechanic in a 30 x 52 m bastion.
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

- **FishNet is the networking library** (2026-08-10). Chosen for a solo dev shipping small co-op
  on Steam: no CCU meter and no metered service path that opens up later, Unity 6 fully
  supported, and — the deciding detail — **its Steam transports (`FishySteamworks`,
  `FishyFacepunch`) are maintained by the netcode's own author**, where NGO's community Facepunch
  transport still pins a 2020 build of Facepunch.Steamworks. Client-side prediction is included
  if it is ever wanted, so there is no library switch waiting later.
  **Runner-up: Netcode for GameObjects**, and it would not be a mistake — Lethal Company is a
  solo-developed Unity 4-player PvE co-op game that sold 10M+ copies on NGO with host authority.
  Take NGO instead if documentation and tutorial coverage matter more than transport ownership.
  **Rejected: Photon Fusion and Quantum.** Both meter concurrent users, and below 500 CCU the cap
  is hard — real players get disconnected until you pay. Quantum additionally mandates
  deterministic fixed-point simulation, which is the constraint this project does not have.
  **Rejected: PUN2**, in maintenance mode with no further feature updates.
  Nothing in M45 depends on this choice; the retrofit transfers between all of them.

## Deferred, with intent

- **Deliver-loot-before-the-run-ends.** The *spirit* of Gears of War's active
  reload, not its form: a neutral default, and an opt-in that pays more and makes
  the Run harder. Explicitly **not** a QTE timing-bar mimic. Not built.
- **Multiplayer.** Unity Netcode for GameObjects with a Steam (Facepunch)
  transport, client-hosted, zero server cost. The design treats co-op as
  first-class — hence the pure-C# sim seam and the ownership labels in
  `Architecture.md` — but **nothing is built**. 2–4 players on one hull.
- **Ship subsystems** (Reactor, TetherCore, Engines, LifeSupport) return with the
  bastion outfitting layer. `ShipSystems` stays the aggregate so the wiring keeps
  its shape.

## Designed, not built — the 5-skill system

Moved to `Design/3-Mechanics.md` section 3.2 on 2026-08-08: it is a design, and player
rulemaking authority lives there.

## Retunes that already happened

- **Global +5%/level — attributes and ship tracks only.** Every CREW QUARTERS and
  FABRICATION track is `base * (1 + 0.05*level)`, CritChance additive at the same slope.
  The old per-track slopes (+4 damage/level, +20 HP/level, +0.15 repair-speed/level,
  0.03 crit/level) are gone; uniform ramps keep the shop legible. **Weapons left this
  curve**: damage, fire rate and clip moved out of `MetaUpgrades` into `WeaponCatalog` +
  `WeaponTier`, which are per weapon and on a linear cost ramp. Two curves in one shop is
  deliberate — weapons are the progression and must stay reachable; attributes are a side
  investment and should get expensive.
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
