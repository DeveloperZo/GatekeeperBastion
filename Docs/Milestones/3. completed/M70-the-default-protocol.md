# M70 — The Default Protocol

<!--
Status: completed 2026-08-17
Update trigger: closed
Last updated: 2026-08-17
Closed on: 2026-08-17. Machine: BoarderProtocolTests 14/0 (None-until-claim,
v7 refund, v8 Default+Ghost→Default+None, STOCK TryPlace, OverlayFill, Coming
Soon not ids); M59PolishChrome Protocol Line/Bar/CooldownFill 10/0. PlayMode
NetProtocolTests 4/0 same-day (Changelog; editor runner stuck unfocused this
close). Play: Logs/GatekeepersProtocol_2026-08-17_00-44-24 claimed Default;
09-43-30 Barrage sat-ready=3.52 / window ended, Rush sat-ready=26.51 / window
ended, wave 1 cleared, no [Dev]; 08-37-17 self refused 21s left. Save
EquippedProtocol=1 EquippedPassive=0. Unequipped G/H `refused — no protocol`
not in a log this close (embark none is). Kill criteria wait 10 Runs or
2026-09-30.
-->

CREW QUARTERS sells **Boarder Protocols**. This slice is the shop, one buyable
sheet, and the Fight readout that makes `G`/`H` findable. The **Default Protocol**
is the plain assault identity (Advance Wars' Andy / Mass Effect's Soldier): you
shoot faster, on purpose, in a window you can hear. Other protocols are **Coming
Soon** stubs — names on the panel, no enum ids, no sheets.

**Recut 2026-08-16 (Zo):** the 2026-08-15 sheet (Anchor / Bulkhead / Ghost /
Foresight, auto-owned from the first session, HUD as a Vitals TMP) is superseded.
Play could not find `G`/`H`; Bastion no longer binds `_protocolLine`; auto-equip
meant CREW had nothing to buy. The first protocol has to be a purchase you walk
to, and a fire-rate verb you can see without a document. Anchor, Bulkhead, Ghost
and Foresight return to their donor sheets in `../../Brainstorms/boarder-protocols.md`.

**Already in tree — do not rebuild:** `BoarderProtocol` catalog + `BoarderAbilityState`;
SaveVersion **8** and the four-track refund; `UpgradeTrack.HatchArmor` only;
`Controls` `G`/`H`; `PlayerProtocolAbilities` tick + `Rooted`; decoy `TryPlace`
writes STOCK; spawn RPC + `ProtocolId`/`PassiveId` NetworkVariables;
`NetPlayerAvatar` partials (Protocol / Presentation); CREW heading PROTOCOL;
`Hud Vitals` host **400 × 150** (do not grow it); Fight shortcut already lists
`G`/`H` from `Controls` (`crew` / `self` labels).

## Story

As a Boarder, I buy the Default Protocol at CREW QUARTERS and then press `G` and
`H` in a fight to dump fire and to rush fire — so that CREW is a shop, the first
sheet is an assault identity I can hear, and the keys that drive it are on screen
with a cooldown.

## Why it matters

The attribute tracks sat under the measured ~20% run-to-run spread
(`../../Design/4-Data.md`, quoting B1 in `../../Design/2-Dynamics.md`). A
windowed fire-rate mul is a verb: 2× for eight seconds is not a +4.5% that play
cannot see. Always-on fire-rate as a Passive would fail that floor; this slice
has no Passive pick.

M11 shipped weapon slots nobody could find; the contract
(`../../_meta/_definitions/milestone.md`, *Usable, or it did not ship*) exists
because of that. The 2026-08-15 HUD criterion was a change-gated string on Hud
Vitals. The scene no longer binds `_protocolLine`. The Fight shortcut strip
already lists the keys. That strip is the Shown / Reads-back surface. Weapon `F`
already reads back as READY/countdown text in the weapon drawer
(`HudController.Weapon`); `G`/`H` copy that text onto the shortcut rows and add
a fill on those two keycaps.

Leverage (`../../Working-Rules.md`): **#5, rules**. Fire-rate muls and windows
below are **#12 starting values**. The wheel still owns baseline fire rate
(M21). This slice adds a **windowed multiplier** on `PlayerGun.EffectiveFireRate`.
Dated in `../../Decisions.md`.

## Kill criteria

Bet. Evaluate at **10** logged Runs after close, or by **2026-09-30**, whichever
comes first. Loop: one assault sheet × hardware × waves.

1. **Reflex, not decision** — every `G`/`H` activation fires within **2 s** of
   cooldown ending (activation line carries seconds-sat-ready). Mash-on-cooldown
   fires this.
2. **The split collapsed** — one slot has fewer than a fifth of the other's
   activations. Fold to one key.
3. **Dead first session** — a fresh save's first embark after the CREW claim logs
   both abilities unused. The sheet is invisible; the HUD recut failed.
4. **Rush is unused motion** — more than half of Rush windows contain no shot
   between the activate line and the expiry line (weapon fire is already logged;
   no new counter). Recut the window or the mul, once.
5. **Coming Soon reads as live** — a Coming Soon row changes `EquippedProtocol`
   or spends salvage. The stub leaked into the catalog.

## The Default sheet

No Passive pick. Twist slots stay later. Windows / cooldowns / muls are #12.

| Slot | Row | Rule | Trade | Window / Cooldown |
|---|---|---|---|---|
| Doctrine | **STOCK** | Hardware as sold; decoy `TryPlace` writes 0 | Reference other doctrines recolor | always on |
| Crew `G` | **Barrage** | You `Rooted` (WASD locked, look/fire stay). Caster `EffectiveFireRate` × **2.0**. Other Boarders inside 8 m × **2.0**, not rooted, checked every tick (leave the radius and the mul drops). `G` again or window end unplants; cooldown starts then | Your mobility for a dump the crew can hear | 8 s / 30 s |
| Personal `H` | **Rush** | You move. Caster × **1.5**. No plant. No `ArmBulkhead` | Timing, not a spoke | 6 s / 40 s |

`BoarderAbilityId`: keep `Anchor=1`, `Bulkhead=2`, `Ghost=3`, `Foresight=4`
(never renumber). Append `Barrage=5`, `Rush=6`. Default binds Barrage / Rush /
passive `None`. `IsLegalPair(Default, None)` is the only legal Default pair.

Kind refusals unchanged: Ram never retargets, Hunter never Hatch. Barrage does
not slow Gatekeepers.

## Locks

Implementer does not choose these.

| Topic | Lock |
|---|---|
| Claim API | `ShipCommands.BuyProtocol(BoarderProtocolId)` mutates local `GameSession.Meta` and `Save()`, Mk1 shape. Cost **0**. Refuse `None`, already-owned, `PayloadLoadoutLocked`. Sim has no shop. |
| Bind `None` | Stays `None`. Do not coerce to Default or Ghost. |
| Barrage field | **Create** `BarrageField` (same plant-list shape as `AnchorField`). Do not plant `AnchorField`. `EnemyGatekeeper` slow stays 1 unless a later sheet plants Anchor. |
| Stacking | Barrage × Rush **multiply** (caster in both = **3.0**). Two Barrage plants on one ally **SET max 2.0**, never 2×2. One multiply in `EffectiveFireRate`, never also in `TryFire` / `Fire()`. |
| Who gets the mul | Cutter **yes** (same getter). Drone slot **no** (place, not a shot clock). |
| Rooted | Owner-local on Plant/Unplant. No NetworkVariable. Death: Unplant + `EndWindowEarly` on both slots (one close line each, cooldown starts). |
| Ally radius | Continuous: each owner's gun samples live plants every tick. |
| Net RPCs | Keep `RequestAnchor` / `RequestBulkhead` names. Server: Barrage plant / Rush window. ClientRpc the plant so each owner sees 8 m. Magnitudes clamped to catalog. Cooldowns stay client-side. |
| F vs G/H | No exclusion. Weapon ability and protocol may fire the same frame. |
| Guest CREW | Local meta, same as `BuyWeapon`. Party EMBARK does not require every seat equipped. |
| Logs (`Tag` `Protocol`) | Activate: `crew Barrage sat-ready=` / `self Rush sat-ready=` plus the number. Early `G`: `crew Barrage unplant sat-ready=` only. Natural end: `crew Barrage window ended` or `self Rush window ended` only — never both for one close. Cooldown: `crew refused — {n}s left` / `self refused — {n}s left`. Open Rush: `self refused — window open`. Unequipped: `crew refused — no protocol` / `self refused — no protocol`. |
| CREW copy | Catalog is SELECT only. SELECT opens `ProtocolSheetView`: authored headings Protocol Title / Protocol Description / Crew Ability / Personal Ability / Passive Ability; injected `SheetOf` values; claim; BACK. Esc / chrome BACK / template BACK return to catalog. Default claim `BUY` unowned, `EQUIPPED` owned. Stubs inject name + `Coming Soon — no sheet yet` on the three ability bodies; claim `Coming Soon` disabled. |
| NAV | `ShowWarnings(noWeapon, noRig, noProtocol)`. PromptHint includes `no protocol` the same way as no-weapon / no-rig. EMBARK ANYWAY if any of the three. Author `_noProtocolWarning` on `NavMapPanelView` in `Ship.unity` (same recipe as no-weapon). |
| HUD | Always-on `Protocol Line` under `Hud Profile` (under BOARDER): unequipped `NO PROTOCOL`; equipped the protocol name only (`Default Protocol`). Cooldown and keys live on authored `Protocol Bar` at bottom centre — Passive, Personal (`H`, Q/E analogue), Crew (`G`, ult, larger). Each slot: name, keycap, Filled Radial360 `CooldownFill`, countdown while windowed/cooling. Overlay: READY = 0; window = remaining/window; cooldown = remaining/cooldown. Do not change `Controls` labels. Do not write READY/countdown onto `Protocol Line`. Shortcut strip stays Controls labels. |
| Keycap fill | Author `CooldownFill` on each Protocol Bar slot in `Bastion.unity` (`m_Type` Filled, Radial360, a real sprite — not `{fileID: 0}`). Overlay: READY = 0; window = remaining/window; cooldown = remaining/cooldown. Fight/Pause shortcut templates keep their fills (unused by this readout). |
| Hear | Gun cadence is the audio. No new clip, no activate SFX. |
| World | Barrage plant: 0.2 m unlit disc at the caster's feet (Unlit/Color, destroyed on unplant). No radius ring, no screen FX, no ally buff VFX. |
| Docgen | Change `Tools/docgen/Program.cs` CREW line: Default claim, Barrage/Rush, no Passive, not "from the first session". Regen Field Manual in this change. |

## Acceptance criteria

1. **CREW is a shop.** Catalog rows SELECT. SELECT opens the Protocol Sheet
   template (Crew / Personal / Passive + claim + BACK). BACK / Esc return to
   catalog. Default and stubs inject into the same template. Purpose stays
   "you". Authored heading PROTOCOL. Serialized field names stay (`_vitalityRow`
   Default; stamina / scavenging / luck the three stubs; luck shown).
2. **Default is a claim.** Fresh `CreateNew`: `EquippedProtocol` `None`,
   `EquippedPassive` `None`. `BuyProtocol(Default)` owns and equips. Mid-Run
   buy is a logged refusal (`PayloadLoadoutLocked`, not renamed). v&lt;8 refunds
   `200 × (2^level − 1)` per track then `None`/`None`. v8 Default stays Default;
   Default+Ghost/Foresight coerces to Default+None. Illegal pairs → None/None.
3. **Barrage dumps.** `G` with Default: `Rooted`; caster ×2.0; other Boarders
   inside 8 m ×2.0 each tick; leave radius and the ally mul is 1; `G` or window
   end unplants and starts cooldown. Disc at feet while planted. Stacking lock
   holds. `G` during cooldown is criterion 6.
4. **Rush is mobile fire.** `H` arms ×1.5 on the caster; WASD stay; no
   Bulkhead absorb. Arming during cooldown or while the window is open is a
   logged refusal (no refresh).
5. **Unequipped is a warning, not a wall.** NAV lock. `G`/`H` with `None` log
   `refused — no protocol`. Improvised cutter path unchanged. Cutter takes the
   protocol mul; drone slot does not.
6. **Cooldown reads back.** Log strings match the log lock (seconds left,
   sat-ready, one close line).
7. **Fight HUD names the protocol; the bar names the keys.** `Hud Profile` `Protocol Line` is
   bound and always on (`NO PROTOCOL` or the protocol name). Bottom-centre `Protocol Bar`
   shows Passive / Personal / Crew with `G`/`H` keycaps, READY/countdown, and `CooldownFill`.
   Hud Vitals stays **400 × 150**. Do not grow Hud Vitals. EditMode pins the bar and Filled
   `CooldownFill` on all three slots.
8. **STOCK still writes at decoy place.** Existing EditMode pin stays. Repair
   is not a place.
9. **Net.** Barrage caster mul and 8 m ally mul apply per the stacking lock,
   from `RequestAnchor` + plant ClientRpc. Rush from `RequestBulkhead` as the
   personal window. Spawn RPC still carries ids; server accepts only
   Default+None. `NetProtocolTests`: SET once per source, product 3.0 when both
   open on the caster, overlapping Barrages stay 2.0. Coming Soon names are not
   ids and are not networked. Rooted is not replicated.
10. **Frame still holds.** Tracks gone, parameterless HP/stamina/salvage/crit,
    refund arithmetic, SaveVersion 8, language lint, docgen `--check` 0 after
    regen, no `[Dev]` close log, no new counter, no new menu row.
    `Tools/verify/run.sh --filter BoarderProtocol` plus recut PlayMode tests
    green. Unrelated reds (Ship Level Music, HUD gap 22 at 1280×720 with host
    150, M59 polish) are not this gate.

## Implementation

Do not recreate the frame. Delta only. Every "or" is already locked above.

1. **Change** `Sim/Meta/BoarderProtocol.cs` — append Barrage=5, Rush=6. Default:
   Crew Barrage, Personal Rush, `PassiveCount` 0, `IsLegalPair` only
   `(Default, None)`. Catalog numbers from the sheet. `ApplySaveEquip` per
   criterion 2. `ShopProtocols()` stays `{ Default }`.
2. **Change** `Sim/Meta/MetaState.cs` + `SaveData.cs` — new saves `None`/`None`
   (ints 0). Drop Ghost default.
3. **Change** `Game/Ship/ShipCommands.cs` — `BuyProtocol` per the claim lock.
   `PickPassive` stays a logged refusal (no panel caller).
4. **Change** `Game/Ship/CrewQuartersStation.cs` + `CrewQuartersPanelView.cs`
   + `ProtocolSheetView.cs` + `Ship.unity` — catalog/detail swap; one template;
   inject `SheetOf` / stub copy.
5. **Change** `Game/Ship/MapStation.cs` + `Game/Ui/NavMapPanelView.cs` +
   `Assets/Scenes/Ship.unity` — NAV lock (`ShowWarnings` third flag + authored
   warning).
6. **Create** `Game/Bastion/BarrageField.cs` — static live list, Plant/Uproot/
   `FireMulAt(Vector3)` returns 2.0 inside any live 8 m plant else 1. Disc
   child per the world lock. **Change** `PlayerProtocolAbilities` — Barrage
   plants this, sets `Rooted`, caster mul 2.0; Rush caster mul 1.5, never
   `ArmBulkhead`. Unequipped refusals. Death cleanup. Logs per lock.
   **Change** `PlayerGun.Slots.cs` `EffectiveFireRate` — `wheelOrCutter *
   protocolMul * barrageMul` (protocolMul from local Rush/Barrage caster,
   barrageMul from `BarrageField.FireMulAt` on this rig). Default 1.
   **Change** `Game/Bastion/GameController.Client.cs` — Bind with `None` is
   legal. **Do not** call `PlayerHealth.ArmBulkhead` on Default Personal.
7. **Change** `Game/Net/NetVerbs.cs` + `NetPlayerAvatar.Protocol.cs` — RPC
   apply + Default+None accept; plant ClientRpc. Clamp to catalog.
8. **Change** `HudController.Fight.cs` / `HudController.Panels.cs` /
   `ProtocolAbilityBarView.cs` / `Bastion.unity` — HUD locks. Protocol Line is
   the name. Protocol Bar is Passive / Personal / Crew. **Do not grow Hud Vitals.**
9. **Change** tests — `BoarderProtocolTests`: claim, None default, Coming Soon
   not in `ShopProtocols`, Barrage/Rush pins, `IsLegalPair(Default, Ghost)`
   false, v&lt;8 refund + None, v8 Default+Ghost → Default+None, stacking
   arithmetic 2 / 1.5 / 3. `NetProtocolTests`: SET once per source, product
   3.0, overlapping Barrage 2.0, Default acquisition mul 1.0. EditMode: CREW
   Coming Soon labels; Protocol Bar three Filled `CooldownFill` slots.
10. **Change** `Tools/docgen/Program.cs` — CREW emitter per the docgen lock;
    `Tools/docgen/run.sh` then `--check`. Docs already recut
    (`language.md`, `Decisions.md`, brainstorm DEFAULT, `Play/The-Ship.md`,
    `Upgrades.md`, `Salvage.md`, `1-Experience.md`); regen Field Manual here.

Rejected, by name: **rebuilding the frame**. **Auto-own Default**. **Ghost /
Foresight pick on Default**. **Always-on fire-rate Passive**. **Re-authoring
Hud Vitals protocol TMP**. **Growing Hud Vitals**. **`BoarderProtocolId.Veil`**.
**Renaming the existing RPCs**. **Renaming `PayloadLoadoutLocked`**. **A salvage
price on Default**. **Enemy slow as the first Crew verb**. **Reuse `AnchorField`
for Barrage**. **Activate SFX / radius ring / screen FX**. **Replicating
Rooted**. **Adding `ControlId` to `ShortcutRow`** — match keycap `G`/`H`.

## Supporting work

- **Driven by** CREW BUY, then `G`/`H` — here.
- **Shown by** CREW catalog SELECT → Protocol Sheet template (Crew / Personal /
  Passive + BUY + BACK), Coming Soon stubs, NAV no-protocol warn, Fight
  `Protocol Line` (name) + bottom `Protocol Bar` (Passive / Personal / Crew
  fills), Barrage disc — here. Art on the squares is later.
- **Reads back as** faster shot clock, `Rooted` on Barrage, disc while
  planted, Protocol Bar fill, `Protocol` log lines — here. Audio is the gun's
  existing cadence, not a new clip.

## Verification

1. Criteria 2 and 10 — `Tools/verify/run.sh --filter BoarderProtocol`: fresh
   save None; BUY then Default+None; v7 seed refunds exact salvage and lands
   None; v8 Default+Ghost → Default+None; catalog Barrage 2.0 / Rush 1.5 /
   product 3; Coming Soon names absent from `ShopProtocols`. Docgen `--check`
   exits 0 after regen.
2. Criterion 9 — `gkb_verify_request.txt`; recut `NetProtocolTests` Passed
   (the 2026-08-16T14:01Z report asserted Anchor/Bulkhead/Ghost and does not
   close this cut).
3. Criterion 8 — existing STOCK EditMode pin still passes.
4. Criterion 1 — EditMode: `ProtocolSheetView` bound on CREW Protocol Sheet;
   `_sheet` bound on `CrewQuartersPanelView`.
   Criterion 7 — `Protocol Line` and `Protocol Bar` bound; three Filled
   `CooldownFill` slots. Playthrough Game view: top-left shows `Default Protocol`
   after claim; bottom bar countdown moves on `G`. Hud Vitals still 400 × 150.
5. Playthrough — new save → ARMORY free sidearm → CREW catalog (no sheet
   yet) → SELECT Default: template shows Crew / Personal / Passive; BUY on
   the sheet; BACK returns to catalog; SELECT VEIL shows Coming Soon, claim
   disabled → NAV EMBARK (no-protocol warn gone after BUY) →
   Wave → `G` roots, disc at feet, gun dumps; `H` rushes without root; both
   keys on cooldown refuse with seconds left; Protocol Bar fill matches.
   Close log: one `Protocol` line per activation/close, no `[Dev]`.
6. Unequipped path — new save, skip CREW, NAV EMBARK ANYWAY: warn shows
   protocol; `G`/`H` log `refused — no protocol`.

## Do not build

| Not building | Why not |
|---|---|
| A second protocol sheet, or deed spine | Coming Soon is the placeholder; VEIL is the first customer |
| Twist slots | Later, with the first authored pairs |
| `UTL-H` turret | FORGE slice |
| Doctrine clauses that change behavior | STOCK writes 0 |
| Ghost / Foresight / Anchor / Bulkhead on Default | Donor sheets; ids stay reserved |
| Always-on fire-rate Passive | ~20% floor |
| A fire-rate wheel spoke | M21 already owns that spoke |
| Activate SFX, radius ring, screen FX, ally VFX | Cadence + Rooted + disc + fill are the readout |
| A heal, a crit source, XP | Cut list unchanged |
| `BoarderProtocolId` for VEIL/AEGIS/LEDGER/FORGE | Copy, not saves |
| Hud Vitals growth | Host stays 400 × 150; protocol lives on Hud Profile |
| New menu row | Ask first |

## Watch

- Barrage ally mul is owner-gun state. Host NPC speed is the wrong pipe.
- `Rooted` must not stomp `MovementLocked`.
- Prefabs freeze `[SerializeField]`; push muls at spawn.
- No `GUILayout` in the Bastion HUD.
- FORGE has no fifth CREW row; it is not a Coming Soon stub until a row exists.
- Protocol Bar is three authored slots (Passive / Personal / Crew). Missing
  `_protocolBar` is a silent no-cooldown path.
- `NavMapPanelView.ShowWarnings` today is two bools; a missing third warning
  GO is a silent no-protocol path.

## Open questions

- Barrage 2.0 / Rush 1.5 / 8 s / 6 s / 30 s / 40 s / 8 m — #12, first tune
  after the kill-criteria Runs. *Unknown, needs play data.*

## Depends on / Unblocks

Depends on nothing unbuilt. Unblocks VEIL (first real second row, replacing
one Coming Soon stub with a catalog id), and gives M65/M72 sessions an assault
identity that reads on the shortcut strip.
