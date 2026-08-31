<!--
Purpose: the DEPOT Protocol (save id Depot = 1), an instance of Protocols — STOCK doctrine,
Dump (crew fire-rate + heavy slow), Feed (toggle ammo regen), no passive.
Status: working
Update trigger: any milestone that touches this instance, in the same change.
Last updated: 2026-08-31 (player name DEPOT; enum BoarderProtocolId.Depot; Feed toggle)
Where:
- Dump/Feed mapping and the zero passive count — BoarderProtocol.cs (CrewAbilityOf,
  PersonalAbilityOf, PassiveCount); ability enum ids stay Barrage/Rush (never renumber)
- STOCK as doctrine 0 — BoarderProtocol.cs (DoctrineMode.Stock, DoctrineOf)
- Dump aura follows the caster; heavy MoveSpeedMul, not Rooted — BarrageField.cs,
  PlayerProtocolAbilities.cs, FirstPersonController.MoveSpeedMul
- Feed toggle regen — PlayerProtocolAbilities.cs + PlayerGun.TickFeedRegen; rate
  BoarderProtocol.FeedRegenOfFireRate
- Zone fire mul — BarrageField.FireMulAt; SelfFireMul no longer multiplies for Feed
- Windows and cooldowns — BoarderProtocol.Window / Cooldown
-->

# DEPOT Protocol

Instance of [Protocols](Protocols.md).

The sheet that spends legs and clip-time so the room empties faster. Save id is
`BoarderProtocolId.Depot` (1); the player-facing name is **DEPOT**.

## Game

The sheet everything else is measured against. It costs nothing and it is claimable the
first minute I stand on my Ship.

Its doctrine is that there is no doctrine. Hardware behaves exactly as FABRICATION sold
it.

The crew burst is Dump. My gun and every Boarder near me dump fire for the window. I am
not rooted — I am heavily slowed. I can still face the Hunter; I cannot kite it. Press
`G` again to stand the burst down early.

The personal burst is Feed. I press `H` and the clip climbs while I shoot — never faster
than the gun spends, enough that continuous fire gets about a quarter more rounds out of
the same magazine. Press again to cut it early. Solo it is still a whole sheet: Dump on
my own gun, Feed on my own clip.

No passive. Nothing moves attention. The room keeps looking wherever it was looking. It
just runs out of bodies and reloads sooner.

## Details

### Structure

Four rows. Doctrine STOCK. Crew **Dump**. Personal **Feed**. Passive none.

`G` opens Dump (fire ×2 for caster and Boarders within 8 m; caster MoveSpeed ×0.35).
Press again to stand down early. `H` toggles Feed: while the window is open, regenerate
clip at `0.22 ×` effective fire rate; press again or window end clears, then cooldown.

### Doctrine

**STOCK.** A decoy placed while wearing DEPOT behaves exactly as FABRICATION described
it. STOCK is also what every caller that omits a doctrine gets.

### Crew

**Dump** — `G`. (Code id `Barrage`.)

Output: the caster's fire rate, and that of every Boarder inside the aura, ×2 for the
window.

Cost: the caster is **heavily slowed** (MoveSpeed ×0.35), not rooted. Look and fire stay.
Press `G` again to stand down early. Window end or death clears the aura and the slow.

The aura follows the caster. There is no planted disc left behind.

### Personal

**Feed** — `H` toggle. (Code id `Rush`.)

Output: regenerate rounds into the active clip while the window is open, at
`FeedRegenOfFireRate × EffectiveFireRate` (0.22). Continuous fire yields about
`1/(1−0.22) ≈ 1.28×` rounds from the same magazine (~25–30% more). Regen never meets or
exceeds fire rate.

Cost: the presser's `H` window and its cooldown. Press `H` again to stand down early.
Does not multiply fire rate. Does not refill during an empty-clip reload that owns the
magazine.

### Passive

None. DEPOT is the only sheet whose passive count is zero.

### Rules

The two bursts sit on separate cooldowns.

Nothing on this sheet changes what the garrison looks at.

**Cut 2026-08-31:** rooted plant, BarrageField left at a world point, Rush as a mobile
fire-rate personal, Barrage×Rush compose on the caster.

Windows, radii and multipliers: [Field Manual](Field-Manual.md) and
`BoarderProtocol` constants.

### Kill criteria

- If Dump's slow is ignored (casters sprint Dump like Rush used to) by one friends
  evening after ship, the cost is wrong — raise the slow or restore a harder foot tax.
- If Feed's continuous-fire shot count is outside 20–35% above dry-clip by the same
  evening, move `FeedRegenOfFireRate` only (one lever).
- If solo Feed feels like the whole sheet and Dump is never pressed, Dump's fire mul or
  slow is wrong relative to Feed — not a third ability.

## Additional Details

### Gaps

- Dump's slow is not signposted before you press it.
- Nothing shows the 8 m aura, so you cannot tell whether a teammate is inside it.
- Feed and Dump clocks are not comparable at a glance on the HUD.
- STOCK still reads as an empty doctrine row.
