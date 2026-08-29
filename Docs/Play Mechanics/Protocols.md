<!--
Purpose: Current representation of Boarder Protocols as they are in the build — the mechanic page; instances carry only their differences.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-29
-->

# Protocols

A protocol is the identity a Boarder embarks wearing, claimed at CREW QUARTERS on the Ship and fixed for the whole Boarding. A claim costs nothing and is never lost to a bad Boarding, but only one is worn at a time and it cannot be swapped aboard the Bastion. In the fight it gives the Boarder two keys: a burst whose effect lands on the crew and whose cost lands on the caster, and a burst for the caster alone. The sheets differ in the four rows each one answers, and two of the named sheets are designed but not yet sold.

## Game

A protocol is the identity I embark wearing, and it is the only thing on the Ship that changes what I am instead of what I carry.

Every sheet answers the same four rows, in the same order. A doctrine, always on, changing what my hardware does in my hands without changing what the hardware is. A crew burst, whose output belongs to whoever is near and whose cost belongs to me. A personal burst, mine alone. And a passive, where the sheet has one.

The plainest sheet costs nothing, same as the free Secondary, and for the same reason: the claiming is the lesson. Two more are claimable beside it, and the roster is not finished — two further sheets are named and not yet sold.

One protocol at a time, fixed the moment I embark. The bursts refuse if I embarked wearing nothing, and the NAV MAP will let me do exactly that.

The sheets are not tiers. They are different answers to one question: what do I want this room looking at.

## The interaction

The Boarder claims a **Boarder Protocol** at CREW QUARTERS. While none is worn the station deals its pick deck — the shipped sheets in catalog order, the one already worn never dealt, every card a claim costing nothing. The deck stands only while the Boarder has no protocol, so a swap afterwards is made from the catalog list and the sheet behind it, at the same price of nothing. The roster's two unbuilt sheets pad that list as named *Coming Soon* rows that open a stub sheet and never a claim.

Claiming equips it — a sheet that owns a passive arrives wearing its first — and refuses out loud mid-Boarding, so swapping is a Ship action. One protocol at a time; embark binds the equipped protocol and passive onto the rig for the whole Boarding.

Aboard the Bastion the protocol is three interactions and a standing rule:

- The **Crew ability** is a burst whose output lands on the crew and whose cost lands on the caster.
- The **Personal ability** is a burst on the presser alone.
- The **doctrine** is written onto every decoy at the moment of placement — placement, not repair.
- The **Passive**, where the sheet has one, is configured onto the rig at bind and read by the garrison's target selection.

Wearing nothing, both ability bindings refuse with a logged reason; the NAV MAP prompt warns `EMBARK — no protocol` and embarks anyway.

Driven by `E` at CREW QUARTERS (claim), then `G` (crew) and `H` (personal) in the fight. Shown by the HUD ability bar's window/cooldown overlay fill and the CREW panel's four-row sheet. Reads back as the `Protocol` log lines — `crew Barrage sat-ready=…`, `self refused — no protocol` — and the overlay draining.

## Consequences

A protocol changes what the room is looking at, and what pressing one of two keys buys in the next few seconds — a plant, a pull, a machine, a disappearance. The choice is made on the Ship and cannot be revisited aboard, so it is a Prep-of-Preps decision the whole Boarding answers for. Solo, the crew half degrades: the output shrinks to the caster (and their hardware), the cost does not.

What it never moves: health, sprint, Salvage per kill, crit, or any weapon's own numbers — the fire-rate bursts compose onto whatever gun is in hand as one self source times one zone source, capped, and that is the entire overlap with the ARMORY. A claim spends nothing and is never lost to a bad Boarding.

Removal test: without protocols there is no `G`/`H`, no doctrine layer on the decoy, and no passive rule — every decision about where to stand for a burst, when to hand attention away, and what the garrison targets first disappears. Different game; the entry stands.

## Classification

Primary, loop. Serves Expression and Fellowship (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Expression is rank 2 and Fellowship rank 1; the crew half of every sheet exists for a crew.

## Interacts with

- [The Drone](The-Drone.md) — the doctrine is written onto the decoy payload at `TryPlace`; that page owns the bay, its charges and its live count.
- [Gatekeepers](Gatekeepers.md) — every kind refusal outranks every protocol effect: a Thief is never pulled, never provoked, never handed to a decoy; that page owns targeting.
- [Weapons](Weapons.md) — the bursts multiply the equipped gun's fire rate and never touch its damage, clip or spread; that page owns the guns and the upgrade wheel.
- [The Crew](The-Crew.md) — the Crew ability's output belongs to whoever is near, and the session is where "near" means somebody; that page owns the session.
- [The Ship](The-Ship.md) — claiming and swapping are Ship actions and nothing changes a protocol aboard the Bastion; that page owns the hull and its stations.

Bindings and every window, radius and multiplier: [Field Manual](Field-Manual.md).

## Instances

Instances vary on the four rows: doctrine, Crew ability, Personal ability, Passive. No instance may vary the bindings (`G`/`H` for every sheet), the one-protocol-at-a-time rule, the caster-pays cost shape, or any weapon's own numbers — those are rules (`../Design/3-Mechanics.md` §3.1).

| Instance | Differs in | State | Page |
|---|---|---|---|
| Default | STOCK doctrine — hardware exactly as sold; rooted crew plant, mobile personal burst, no passive | in build | [Default Protocol](Protocol-Default.md) |
| VEIL | PHANTOM doctrine — a placed decoy inherits the attention its placer was carrying; crew wallhack that pulls the room onto the caster, personal drop off every target list, shorter-notice passive | in build | [VEIL Protocol](Protocol-VEIL.md) |
| FORGE | PARALLEL BAYS doctrine (written, not yet read — the bay still holds one); crew turret plant with a fire-rate aura, personal redline of that turret, turret-taunt passive | in build | [FORGE Protocol](Protocol-FORGE.md) |
| AEGIS | the holding-ground identity — designed, not built | Coming Soon | [AEGIS — Coming Soon](Protocol-AEGIS.md) |
| LEDGER | the salvage-as-position identity — designed, not built | Coming Soon | [LEDGER — Coming Soon](Protocol-LEDGER.md) |

## Where

- Protocol ids, doctrine/crew/personal/passive mapping, windows, cooldowns, and the 0-cost claim — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`BoarderProtocolId`, `DoctrineOf`, `CrewAbilityOf`, `PersonalAbilityOf`, `PassiveAt`, `Window`, `Cooldown`, `TryClaim`)
- The offer rule: it stands only while no protocol is worn, deals in catalog order and never deals the held one, with per-card refusal copy — `Assets/Scripts/Sim/Meta/ProtocolOffer.cs` (`IsPickPending`, `Deal`, `RefuseReason`, `CardCount`)
- Deck mode versus catalog mode, and the two unbuilt sheets padding the catalog as Coming Soon rows — `Assets/Scripts/Game/Ship/CrewQuartersStation.cs` (`DeckAvailable`, `BuildPickMode`, `BuildCatalogMode`, `BuildSheetMode`, `ComingSoonNames`)
- Authored sheet copy, one row set per protocol — `Assets/Scripts/Sim/Meta/BoarderSheets.cs`
- Claim command and its save — `Assets/Scripts/Game/Ship/ShipCommands.cs` (`BuyProtocol`, "0-salvage CREW claim"); Ship-only passive re-pick (`PickPassive`)
- Equipped pair persists on the traveling half of the save; loaded pairs sanitised — `Assets/Scripts/Sim/Meta/BoarderState.cs` (`EquippedProtocol`, `EquippedPassive`), `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`ValidateEquip`, `IsLegalPair`)
- `G`/`H` input, both windows and cooldowns, no-protocol refusals, caster-side effects — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`OnCrew`, `OnPersonal`, `Tick`); bindings — `Assets/Scripts/Game/Input/Controls.cs` (`ControlId.CrewAbility` = G, `ControlId.PersonalAbility` = H)
- Embark binds the equipped pair onto the rig — `Assets/Scripts/Game/Bastion/GameController.Client.cs` (`proto.Bind(this, Meta.EquippedProtocol, Meta.EquippedPassive)`)
- NAV MAP warns `EMBARK — no protocol` and still embarks — `Assets/Scripts/Game/Ship/MapStation.cs` (`PromptHint`)
- Doctrine written at decoy placement, from the placer's equipped protocol — `Assets/Scripts/Game/Bastion/DecoyBay.cs` (`PlaceMode`, `TryPlace`), `Assets/Scripts/Sim/Run/DecoyState.cs` (`TryPlace(mode)`)
- Fire multipliers compose as one self source times one capped zone source onto the gun — `Assets/Scripts/Game/Player/PlayerGun.Slots.cs`, `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`ComposeFireMul`)
- HUD overlay fill — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`OverlayFill`), drawn by `Assets/Scripts/Game/Ui/ProtocolAbilityBarView.cs`

## Gaps

- Nothing in the Bastion names the protocol you are wearing. The HUD shows the ability bindings and their cooldowns and never the sheet they came from.
- The doctrine layer is invisible. A decoy placed under PHANTOM and one placed under STOCK look identical, and nothing says the hardware behaved differently.
- Embarking with no protocol is allowed and both bindings then refuse silently in play; the warning is only on the NAV MAP.
- Nothing explains that the Crew ability's cost lands on the caster alone. Solo players read the crew half as missing rather than degraded.
- The AEGIS and LEDGER sheets exist in the design and are not sold. Nothing on the panel indicates the roster is unfinished.
