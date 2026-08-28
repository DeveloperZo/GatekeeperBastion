# HUD and ability presentation

<!--
Purpose: How the HUD presents player abilities, perk/build depth, and risk stakes —
evidence and candidate shapes, gathered from reference-game research at Zo's request
(2026-08-26). Distinct from `boarder-protocols.md`, which owns what a Protocol ability
*does*; this file owns how the choice and its state are *shown* and whether it grows
a perk layer at all.
Status: brainstorm
Update trigger: when a reference game changes what it does with abilities/perks/stakes UI,
or when the Ante/Protocol systems this file references change shape.
Last updated: 2026-08-26
-->

## What this covers

HUD and menu presentation for player abilities, cooldowns, build/perk depth, and
risk-stake systems — not the mechanics themselves (`boarder-protocols.md`,
`Docs/Design/4-Data.md`), not the Ante number (`Docs/Play/Ante.md`,
`Sim/Meta/IntensityJuice.cs`).

## Where it stands

- `Assets/Scripts/Game/Ui/ProtocolAbilityBarView.cs`: one ability slot, radial
  `Image.fillAmount` cooldown, distinct Active/Cooling fill colors. A player carries
  exactly one Protocol ability at a time (`boarder-protocols.md`'s swap-at-Ship).
- The "what can I do here" surface at every interactable (Hatch, Bastion terminal,
  Ship station) is a single crosshair-line string (`IInteractable.Prompt`) — no panel,
  no icon grid.
- No perk-tree UI exists anywhere in the game; a Protocol is one pick, not built out
  or leveled.

## Graduation test

Whether Protocols ever grow a second, in-fight choice layer — already an open question
in `boarder-protocols.md` ("second twist slot," Open Questions). This file has nothing
to graduate until that's decided; everything below is evidence to have on hand if it is.

## Ideas

### A second twist slot, if it ships, is a verb-swap — not a perk stack

Three independent sources converge on one shape, not three data points to weigh.
**Kingdom Rush** (`landscape.md`, already Took): a tower twist is 1-of-2, mutually
exclusive, and changes the job — never a numeric buff. **Fireteam Elite 2's own perk
board**, read against that, splits on exactly this line: its "ability replacer" tier
is a mutually exclusive verb swap (Sentry Turret → Gas Turret, a different damage
type and behavior, not a stronger turret); its "core perk" and "modifier" tiers are
incremental stat layers on top — a different shape wearing the same grid UI. Only
the replacer tier matches Kingdom Rush's. **`boarder-protocols.md`'s own kill
question** already bans numeric passives for Boarders outright, on the same
illegible-mid-fight-PoE-mod grounds, at a TTK 10–100× faster than PoE's.

Argument: if `boarder-protocols.md`'s second twist slot ever ships, it should be
built as a second named ability variant that swaps the verb (VEIL's second form
reads different targets, not "VEIL but stronger"), not as a perk or modifier layer.
The modifier-tier shape isn't an open option — it's the shape three independent
sources, including this project's own written rule, already rule out. Fireteam's
own reception data backs it: the criticism (resource-stripping, "doesn't matter")
concentrated on modifier-tier cards, not the replacer tier.

*Still open: whether a single verb-swap alone reads clearly enough at this game's
kill cadence (`2-Dynamics.md` §2.3) even stripped of the incremental layer — the
existing "dilution meets the noise floor" kill question tests a flag-and-modifier
mechanic, not a fully alternate ability, so it may not transfer as-is. The cheapest
test stays the same one already written there: ship the probe, ask if a player can
name what changed.*

### Challenge Card valence + category taxonomy

Fireteam Elite 2 tags each of its 103 Challenge Cards by valence (Positive / Neutral /
Negative), by what it affects (Bots / Enemies / Environment / Mechanics / Players /
Tracker / Weapons), and by rarity, then gates the harder ones behind a store unlock.
Ante is a single scalar dial today, not a card pool, and no milestone or brainstorm
proposes changing that. Recorded as evidence for if Ante (or a future stakes system)
ever grows named, discrete modifiers instead of a number — not a live proposal.

## Rejected

- **Numeric aggregate power score ("Combat Rating").** Fireteam Elite 2 sums a
  numeric rating across every equipped item — weapon, attachment, perk — Destiny-style.
  That's a gear-rarity/RNG-progression convention; Bastion has five named weapons and
  track-based upgrades, nothing to aggregate. Recorded so it isn't rediscovered as a
  UI idea later.
