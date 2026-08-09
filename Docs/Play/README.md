# Play — the game from the outside

<!--
Purpose: the player-facing half of the documentation, and the index of the wiki. What
Gatekeeper Bastion is, and how everything in it actually behaves.
Status: working
Update trigger: when a page is added to or removed from this folder.
Last updated: 2026-08-06
-->

`Docs/` above this folder is written for whoever is **building** the game — where code
lives, why a number is what it is, what was cut and why. This folder is the other half:
the game as a player meets it.

## Start here

| Page | Answers |
|---|---|
| [`The-Game.md`](The-Game.md) | What is this, what is a Run, why is it built this way. |
| [`Getting-Started.md`](Getting-Started.md) | You just installed it. What to do, in order. |
| [`Field-Manual.md`](Field-Manual.md) | Every key, weapon, Gatekeeper, Wave, cost and track. **Generated.** |

## The Run

| Page | Answers |
|---|---|
| [`A-Run.md`](A-Run.md) | The shape of a Run start to finish: Prep, the draw, Assault, and the three ways it ends. |
| [`Waves.md`](Waves.md) | How Waves escalate, and where the difficulty actually comes from. |
| [`The-Siphon-Dial.md`](The-Siphon-Dial.md) | The wager: what it changes, what it does not, and when to raise it. |
| [`Staying-Alive.md`](Staying-Alive.md) | Tactics, each one traced to the rule that makes it work. |

## What comes at you

| Page | Answers |
|---|---|
| [`Gatekeepers.md`](Gatekeepers.md) | The roster, how they arrive, and what puts one on you. |
| [`Gatekeeper-Default.md`](Gatekeeper-Default.md) | The marcher — the only kind you can steer. |
| [`Gatekeeper-Hunter.md`](Gatekeeper-Hunter.md) | Comes for you, never for the Hatch. |
| [`Gatekeeper-Ram.md`](Gatekeeper-Ram.md) | Comes for the Hatch, never for you, and cannot be moved. |

## What you carry

| Page | Answers |
|---|---|
| [`Weapons.md`](Weapons.md) | Slots, buying, the upgrade wheel, reloading, switching, and how to choose. |
| [`Weapon-MK-1-Sidearm.md`](Weapon-MK-1-Sidearm.md) | The ranged Secondary. |
| [`Weapon-SEC-D.md`](Weapon-SEC-D.md) | The close-quarters Secondary. |
| [`Weapon-BR-12-Shotgun.md`](Weapon-BR-12-Shotgun.md) | The answer to things that come to you. |
| [`Weapon-MR-35-Rifle.md`](Weapon-MR-35-Rifle.md) | The answer to things that do not. |
| [`Weapon-PRI-C.md`](Weapon-PRI-C.md) | The answer to the far wall. |

## Between runs

| Page | Answers |
|---|---|
| [`The-Ship.md`](The-Ship.md) | The hub, the four consoles, and what to buy first. |
| [`Salvage.md`](Salvage.md) | The only currency: how you earn it and what competes for it. |
| [`Upgrades.md`](Upgrades.md) | The permanent tracks, what is gated, and what is worth buying. |
| [`The-Hatch.md`](The-Hatch.md) | The thing you defend, and what repairing it really costs. |

## The rules this folder follows

**Anything countable is generated; anything arguable is written.** A weapon's damage is
countable and belongs in `Field-Manual.md`, which `Tools/docgen/run.sh` writes by calling
the same code the game runs. Why a Ram refuses to notice you is arguable and belongs on a
wiki page. **No page in this folder hand-types a number or a key binding** — it names the
action and links to the manual. That rule exists because the hand-maintained versions had
drifted three separate ways by 2026-08-04, including a controls table listing a key that
had already moved.

**We wiki what we build.** A page exists because a thing exists in the game, not because
a wiki is expected to have a page of that kind. There is no items page, no crafting page
and no bestiary of things that have not shipped. When a page would have to describe
something that does not exist, it goes in `## Gaps` instead.

**Every page ends with `## Gaps`** — what the game never tells the player, and what a
player could not discover in play. Those lists are written for the builder as much as the
reader: a wiki that has to explain a rule the game keeps secret has found a design
problem, and the Gaps section is where it gets recorded.

`Tools/docgen/run.sh --check` fails if the committed manual no longer matches the build.
