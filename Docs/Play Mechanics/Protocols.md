<!--
Purpose: Current representation of Boarder Protocols as they are in the build — the mechanic page; instances fill the four-row sheet.
Status: working
Update trigger: any milestone that touches this mechanic, in the same change.
Last updated: 2026-08-31 (DEPOT Protocol — enum Depot)
Where:
- Protocol ids, doctrine/crew/personal/passive mapping, windows, cooldowns, and the 0-cost claim — Assets/Scripts/Sim/Meta/BoarderProtocol.cs (BoarderProtocolId, DoctrineOf, CrewAbilityOf, PersonalAbilityOf, PassiveAt, Window, Cooldown, TryClaim)
- The automatic grant, and the load path that hands it to every save — Assets/Scripts/Sim/Meta/BoarderProtocol.cs (GrantedProtocol, ValidateEquip) and Assets/Scripts/Sim/Meta/BoarderState.cs (CreateNew), pinned by Assets/Tests/EditMode/Meta/ProtocolGrantTests.cs
- The protocol on both machine log lines — Assets/Scripts/Game/Bastion/GameController.Client.cs (RunBeginsFields) and Assets/Scripts/Game/Bastion/GameController.EndScreen.cs (LogRunMetric, schema 2)
- The offer rule: it stands only while no protocol is worn, deals in catalog order and never deals the held one — Assets/Scripts/Sim/Meta/ProtocolOffer.cs (IsPickPending, Deal, RefuseReason, CardCount)
- Deck mode versus catalog mode, and the two unbuilt sheets padding the catalog as Coming Soon rows — Assets/Scripts/Game/Ship/CrewQuartersStation.cs (DeckAvailable, BuildPickMode, BuildCatalogMode, BuildSheetMode, ComingSoonNames)
- Authored sheet copy, one row set per protocol — Assets/Scripts/Sim/Meta/BoarderSheets.cs
- Claim command and its save — Assets/Scripts/Game/Ship/ShipCommands.cs (BuyProtocol); Ship-only passive re-pick (PickPassive)
- Equipped pair persists on the traveling half of the save — Assets/Scripts/Sim/Meta/BoarderState.cs (EquippedProtocol, EquippedPassive); sanitised by BoarderProtocol.ValidateEquip
- G/H input, both windows and cooldowns, no-protocol refusals — Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs (OnCrew, OnPersonal, Tick); bindings Controls.cs (ControlId.CrewAbility = G, ControlId.PersonalAbility = H)
- Embark binds the equipped pair onto the rig — Assets/Scripts/Game/Bastion/GameController.Client.cs (proto.Bind)
- NAV MAP warns EMBARK — no protocol and still embarks — Assets/Scripts/Game/Ship/MapStation.cs (PromptHint)
- Doctrine written at decoy placement, from the placer's equipped protocol — Assets/Scripts/Game/Bastion/DecoyBay.cs (PlaceMode, TryPlace); Assets/Scripts/Sim/Run/DecoyState.cs (TryPlace)
- Fire multipliers compose as one self source times one capped zone source onto the gun — Assets/Scripts/Game/Player/PlayerGun.Slots.cs; BoarderProtocol.ComposeFireMul
- HUD overlay fill — BoarderProtocol.OverlayFill, drawn by Assets/Scripts/Game/Ui/ProtocolAbilityBarView.cs
-->

# Protocols

A protocol is the identity a Boarder embarks wearing, fixed for the whole Boarding. Every Boarder is granted DEPOT without claiming it, so nobody ever fights with no crew ability, no personal ability and no doctrine; the other sheets are claimed at CREW QUARTERS on the Ship and replace whatever is worn. A claim costs nothing and is never lost to a bad Boarding, but only one is worn at a time and it cannot be swapped aboard the Bastion. In the fight it gives the Boarder two keys: a burst whose effect lands on the crew and whose cost lands on the caster, and a burst for the caster alone. The sheets differ in the four rows each one answers, and two of the named sheets are designed but not yet sold.

## Game

A protocol is the identity I embark wearing, and it is the only thing on the Ship that changes what I am instead of what I carry.

Every sheet answers the same four rows, in the same order. A doctrine, always on, changing what my hardware does in my hands without changing what the hardware is. A crew burst, whose output belongs to whoever is near and whose cost belongs to me. A personal burst, mine alone. And a passive, where the sheet has one.

The plainest sheet is **DEPOT**. It is granted rather than claimed, the way the Secondary is free, and for a stronger reason than the lesson: a Boarder with no protocol has two dead keys and dies markedly faster, and the play data could not tell that Boarder from any other until the log started naming the protocol. Two more are claimable beside it, and the roster is not finished — two further sheets are named and not yet sold.

One protocol at a time, fixed the moment I embark. The bursts refuse if I embarked wearing nothing, and the NAV MAP will let me do exactly that.

The sheets are not tiers. They are different answers to one question: what do I want this room looking at.

## Details

### Structure

Every sheet is four rows. The rows are the mechanic. The instances only fill them.

- **Crew ability** — a burst whose output lands on the crew and whose cost lands on the caster. Key `G`.
- **Personal ability** — a burst on the presser alone. Key `H`.
- **Doctrine** — written onto every decoy at the moment of placement. Placement, not repair. It changes what the hardware already in the Boarder's hands does, without changing what the hardware is.
- **Passive** — where the sheet has one, configured onto the rig at bind and read by the garrison's target selection. Always on. Nothing to press.

Wearing nothing, both keys refuse. The NAV MAP warns and embarks anyway.

### Rules

Claim at CREW QUARTERS. DEPOT is already worn on a fresh Boarder and reads as EQUIPPED there; a swap is a catalog claim, still costing nothing. Two named sheets pad that list as Coming Soon and cannot be claimed. Claiming equips it — a sheet that owns a passive arrives wearing its first — and is refused mid-Boarding, so swapping is a Ship action.

One protocol at a time. Embark binds the equipped protocol and passive onto the rig for the whole Boarding.

Solo, the crew half degrades: the output shrinks to the caster (and their hardware), the cost does not.

What a protocol never moves: health, sprint, Salvage per kill, crit, or any weapon's own numbers. Fire-rate bursts compose onto whatever gun is in hand as one self source times one zone source, capped. That is the entire overlap with the ARMORY.

Kind refusals outrank every protocol effect. A Thief is never pulled, never provoked, never handed to a decoy.

Windows, cooldowns, radii and multipliers: [Field Manual](Field-Manual.md). Each instance page names the four rows that sheet fills.

## Additional Details

### Consequences

A protocol changes what the room is looking at, and what pressing one of two keys buys in the next few seconds — a plant, a pull, a machine, a disappearance. The choice is made on the Ship and cannot be revisited aboard, so it is a Prep-of-Preps decision the whole Boarding answers for.

A claim spends nothing and is never lost to a bad Boarding.

Removal test: without protocols there is no `G`/`H`, no doctrine layer on the decoy, and no passive rule — every decision about where to stand for a burst, when to hand attention away, and what the garrison targets first disappears. Different game; the entry stands.

### Classification

Primary, loop. Serves Expression and Fellowship (`../Design/1-Experience.md` §1.2, 2026-08-16 rank stamp), with Challenge as the vehicle. Expression is rank 2 and Fellowship rank 1; the crew half of every sheet exists for a crew.

### Interacts with

- [The Drone](The-Drone.md) — doctrine is written onto the decoy at placement; that page owns the bay, its charges and its live count.
- [Gatekeepers](Gatekeepers.md) — every kind refusal outranks every protocol effect; that page owns targeting.
- [Weapons](Weapons.md) — the bursts multiply the equipped gun's fire rate and never touch its damage, clip or spread; that page owns the guns and the upgrade wheel.
- [The Crew](The-Crew.md) — the Crew ability's output belongs to whoever is near; that page owns the session.
- [The Ship](The-Ship.md) — claiming and swapping are Ship actions; that page owns the hull and its stations.

### Instances

Instances vary on the four rows: doctrine, Crew ability, Personal ability, Passive. No instance may vary the bindings (`G`/`H` for every sheet), the one-protocol-at-a-time rule, the caster-pays cost shape, or any weapon's own numbers — those are rules (`../Design/3-Mechanics.md` §3.1).

| Instance | Differs in | State | Page |
|---|---|---|---|
| DEPOT | STOCK doctrine; Dump crew fire-rate + heavy slow (aura follows caster); Feed toggle ammo regen; no passive. Save id `Depot` (1). | in build | [DEPOT Protocol](Protocol-DEPOT.md) |
| VEIL | take the eyes (through-walls + pull onto the caster), hand them to a PHANTOM decoy, go dark; Ghost shortens notice | in build | [VEIL Protocol](Protocol-VEIL.md) |
| FORGE | crew turret with charge-spent Overcharge, personal Outrider with EMP, proximity-gated pairing, passive ** CLASSIFIED ** | in build | [FORGE Protocol](Protocol-FORGE.md) |
| AEGIS | the holding-ground identity — designed, not built | Coming Soon | [AEGIS — Coming Soon](Protocol-AEGIS.md) |
| LEDGER | the salvage-as-position identity — designed, not built | Coming Soon | [LEDGER — Coming Soon](Protocol-LEDGER.md) |

### Gaps

- Fight HUD names the sheet top-left. The doctrine layer is still invisible: a decoy placed under PHANTOM and one placed under STOCK look identical.
- Embarking with no protocol is allowed and both bindings then refuse silently in play; the warning is only on the NAV MAP.
- Nothing explains that the Crew ability's cost lands on the caster alone. Solo players read the crew half as missing rather than degraded.
- The AEGIS and LEDGER sheets exist in the design and are not sold. Nothing on the panel indicates the roster is unfinished.
