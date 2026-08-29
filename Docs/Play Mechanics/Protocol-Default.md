<!--
Purpose: the Default Protocol, an instance of Protocols — differences only: STOCK doctrine, Barrage, Rush, no passive.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-29
-->

# Default Protocol

Instance of [Protocols](Protocols.md).

The only sheet that moves no attention at all and the only one with no passive: it adds fire rate and changes nothing else, so everything that was coming for the Boarder still is. Its crew burst roots the caster in place while it runs; the personal one gives less for a shorter window but keeps them moving.

## Game

The sheet everything else is measured against. It costs nothing and it is claimable the first minute I stand on my Ship.

Its doctrine is that there is no doctrine. Hardware behaves exactly as FABRICATION sold it. Nothing I place is quietly different from the panel's description, and I have learned to price that honesty.

The crew burst is a plant. I put it down, I am rooted while it runs, and my gun and every Boarder standing near me dump fire for the window. I can pull it up early. The root is the entire price: the fastest I will ever shoot, from a spot I chose before the group arrived — or a spot I will die in, because I chose late.

The personal burst keeps me moving and speeds only my own gun. Less, for a shorter window, on a longer wait — but on my feet.

No passive. Nothing standing, nothing conditional, nothing to remember at the wrong moment.

It is not the beginner sheet. It is the sheet that adds output and moves nothing else. The room keeps looking wherever it was looking. It just runs out of bodies sooner.

## Differs

**Doctrine: STOCK.** A decoy placed while wearing Default is written `Stock` and behaves exactly as FABRICATION described it, with the payload's own authored live count. STOCK is also what every caller that omits a doctrine gets — it is the baseline the other doctrines deviate from, not an empty row.

**Crew: Barrage.** A plant. The caster is rooted for the window; their fire rate and that of other Boarders inside the planted field's radius are multiplied, and the others are not rooted. Pressing `G` again stands the burst down early; the window ending, or dying, unplants unconditionally. The zone multiplier belongs to this ability alone — it is gated on the Barrage id, not on any open crew window, so no sibling sheet can hand it out.

**Personal: Rush.** The caster's own gun only, and they keep moving. A smaller multiplier than Barrage's, for a shorter window, on a longer cooldown — the choice between the two bursts is mobility, not damage. A caster inside their own Barrage field composes both, self times zone.

**Passive: none.** Default is the only sheet whose passive count is zero. Nothing on this sheet moves attention: everything that was coming for the Boarder is still coming.

Values, windows, radii and cooldowns: [Field Manual](Field-Manual.md).

## Where

- Barrage/Rush mapping and the zero passive count — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`CrewAbilityOf`, `PersonalAbilityOf`, `PassiveCount` — Default falls to the 0 default)
- STOCK as doctrine 0 and the omitted-mode default — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`DoctrineMode.Stock`, `DoctrineOf`); `Assets/Scripts/Sim/Run/DecoyState.cs` (`TryPlace` overload writes `Stock` when callers omit a mode)
- Root on plant, early stand-down on a second press, unconditional unplant on death — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`Plant`, `Unplant`, `OnCrew`, `NotifyDeath`)
- The planted zone, its radius and the zone cap — `Assets/Scripts/Game/Bastion/BarrageField.cs` (`Plant`, `FireMulAt`)
- Zone multiplier gated on the Barrage id, Rush on its own id — `Assets/Scripts/Game/Player/PlayerProtocolAbilities.cs` (`BarrageWindowOpen`, `RushOpen`, `SelfFireMul`)
- The two multipliers, windows and cooldowns, and their composition — `Assets/Scripts/Sim/Meta/BoarderProtocol.cs` (`BarrageFireMul`, `RushFireMul`, `Window`, `Cooldown`, `ComposeFireMul`); read by `Assets/Scripts/Game/Player/PlayerGun.Slots.cs`

## Gaps

- The root on Barrage is not signposted before you press it. You learn it by being unable to move with a Hunter closing.
- Nothing shows the crew radius, so you cannot tell whether a teammate is inside it.
- Rush and Barrage are on separate cooldowns and the HUD does not make the two clocks comparable at a glance.
- That STOCK is a real doctrine rather than the absence of one is never stated. It reads as an empty row.
