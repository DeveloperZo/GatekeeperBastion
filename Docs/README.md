# Gatekeeper Bastion — the game on one page

Sci-fi wave-defense FPS. Unity 6, PC/Steam, first-person, gray-box art. Solo playable now;
2–4 player co-op designed for and not built.

Your ship cuts into a **Bastion** — a garrisoned alien vessel — and siphons it for Salvage.
You are the **Boarder**. You fight inside it, holding the **Hatch**, the umbilical back to
your ship, while the **Gatekeepers** who hold the Bastion come to take it back. You set the
siphon dial, which multiplies both reward and pressure. **You cannot leave a Run at will.**

## The pillars

**The player sets both the pace and the stake, and the game supplies neither.** Nothing
starts until you press T. The clock never pays. The dial is a bet you place before you see
what you bought.

**Every Gatekeeper kind is defined by what it refuses to do.** A Hunter can never hurt the door.
A Ram can never hurt you and will not turn around when shot. These are rules in code, not
tuning values, so no balance pass can soften one by accident.

**The weapon is the build.** Progression is a weapon and its Tiers, not a stat sheet.
Attributes are a side investment and stay cheap and slow on purpose.

**Death is the only thing that ends a Run early.** There is no bail-out, no respawn, and
losing takes nothing back.

## The loop

```
                 SHIP                                    BASTION
   ┌───────────────────────────────┐        ┌──────────────────────────────────┐
   │  ARMORY      weapon + Tiers   │        │   PREP    quiet. no income.      │
   │  CREW Q.     attributes       │        │           repair, reposition,    │
   │  FABRICATION hatch + rig      │───────▶│           set the dial           │
   │  NAV MAP     where, how deep  │ embark │              │ press T           │
   └───────────────────────────────┘        │              ▼                   │
                   ▲                        │   ASSAULT gatekeepers walk in from  │
                   │                        │           three spawn pts, ~15s     │
                   │  RETURN TO SHIP        │           away. kills pay, the   │
                   │  (died, or stripped    │           dial is locked.        │
                   │   the bastion)        │              │                   │
                   └────────────────────────┤              ▼                   │
                       salvage is banked    │   wave clear ──▶ PREP  (xN)      │
                       the instant a        │   hatch breached ──▶ run over    │
                       gatekeeper dies         │   you die        ──▶ run over    │
                                            └──────────────────────────────────┘
```

The reinforcing loop is kills → Salvage → upgrades → faster kills, and **it closes once per
Run, in the Ship** — so a Run cannot spiral, and its difficulty is fixed the moment you
embark. The balancing loop is your kill rate against the arrival rate, and it is weaker than
it looks: killing faster cuts each gatekeeper's time at the door but raises how many arrive, and
the two nearly cancel.

## What a good Run is

**Open, and it is the most valuable unanswered question in the project.** The two constraints
that exist are an economy one — no Wave should cost more than three failed Runs' earnings to
buy past — and a curve target on Hatch remaining. The second does not work: Hatch remaining
measures leakage, not effort, and reads 100% for a skilled player whether the Wave was
trivial or desperate. `Design/1-Experience.md`, section 1.3.

## What it refuses to be

Not an idle game: time never pays. Not a game with an exit: there is no lever to pull. Not a
game that confiscates: dying and clearing differ only in which counter ticks. Not a game that
rushes you: it will never start without you.

---

## The docs

**`Design/` is read in order, and the order is the argument.** It descends Meadows' leverage
ladder, which is MDA read backwards: a designer can only build Mechanics but must aim at
Aesthetics, so a proposal made low in the order has to be argued against everything above it.

| File | Layer | Ladder | Answers |
|---|---|---|---|
| `Design/1-Experience.md` | Aesthetics | #1–#3 | What the game believes, which kinds of fun it serves, what a good Run is, what each actor wants. |
| `Design/2-Dynamics.md` | Dynamics | #6–#9 | The player's feedback loop, the four game loops and their gain, the delays, and every pacing model — including the ones play data falsified. |
| `Design/3-Mechanics.md` | Mechanics | #4, #5, #10 | The rules enforced in code, who may change them and when, the geometry, the stocks. |
| `Design/4-Data.md` | Mechanics | #11–#12 | Every constant and the arithmetic that justifies it. Read last. |

Everything else is how the game gets made, and is deliberately **not** on that ladder:

| File | Answers |
|---|---|
| `Architecture.md` | Where code lives, the conventions, and the gotchas that cost real time. |
| `Working-Rules.md` | How to work here: lanes, scope, the leverage gate, evidence, logs, verification. |
| [`.claude/skills/`](../.claude/skills/) | How to do a kind of work — development, design, verification, brainstorm, strategy. Loaded by the agent when the work matches, not read by hand. Read by both Claude Code and Cursor. |
| `Decisions.md` | What was decided, cut, deferred, or designed-but-not-built, and why. |
| `Roadmap.md` | Order, status, strategy and risks. The summary layer over `Milestones/`. |
| `Changelog.md` | The roadmap's audit trail: milestone state transitions and structural edits, most recent first. |
| `Milestones/` | One file per milestone, filed in a folder named for its status. |
| `Brainstorms/` | Ideas before they graduate. |
| `_meta/_definitions/` | `milestone.md`, `brainstorm.md`, and `language.md` — the ubiquitous language every doc, log line and player-facing string uses. |
| `Play/` | The game from the outside. The player's half; everything above is the builder's. |

**Cold start:** this page, then `Design/` 1 → 4, then `Architecture.md` and
`Working-Rules.md` before touching anything, then `Decisions.md` before proposing a feature
and `Roadmap.md` before starting work. **If you have never played it**, start instead with
`Play/The-Game.md` and `Play/Field-Manual.md`.

**Wave dashboard:** open [`dashboard.html`](dashboard.html). Choose the `Logs` folder (or
drop `GatekeeperBastion_*.log`). Parses in the browser: mean live by era, per-weapon and
per-kind kill share, spend/SKU, salvage ledger, hatch pressure, ability credit, and
editable qualitative scores. Far-wall / `hatchDist` waits on M37.

### Three standing rules

**Countable is generated; arguable is written.** `Tools/docgen/run.sh` writes
`Play/Field-Manual.md` by calling the same code the game runs, and `--check` exits non-zero
on drift. A hand edit there is lost on the next Run, on purpose. **Do not restate a countable
fact anywhere else** — link to the manual. On 2026-08-04 three documents described three
different games and none was the one in the repo; each had been correct when written.

**Keep the set small.** The previous project reached 48 markdown files and its own postmortem
named the docs as a failure: productive procrastination. Before adding a file it has to fail
this test: *would an agent otherwise hallucinate this, or repeat a known mistake?* Prefer
editing an existing file. Every line should be a fact, a number with its justification, or a
rule that prevents a specific failure. `Milestones/` is the one exception to the count, and it
has its own rule instead: a milestone file exists only for a milestone in the table, it links
rather than copies, and it dies with the milestone.

**Only recurring document classes get an authoring contract.** `milestone.md` and
`brainstorm.md` qualify, because new instances get written from memory and drift.
`language.md` is in `_definitions/` under a different rationale and is the only one: it is a
standing rule applying to every file, every log line and every string a player reads, and it
would otherwise be duplicated into six files and drift in four.

Numbers in these docs were read out of `Assets/Scripts/Sim/*.cs` and
`Assets/Scripts/Game/*.cs`. When you retune, update `Design/4-Data.md` in the same change.
Root `README.md` has clone-to-play steps.
