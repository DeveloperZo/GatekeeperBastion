# Gatekeeper's Protocol — the game on one page

Repository folders, Tools, and scene names: root [`README.md`](../README.md).
This page is the game and the docs index.

Sci-fi wave-defense FPS. Unity 6, PC/Steam, first-person. The Boarder is the IQ astronaut
with FaxLab3D shooter-pack guns (the Cutter sword is still Synty, 2026-08-19); Ship desks
and the Bastion kit are dressed (M61-2 closed 2026-08-14:
scaler-2 station rooms, sliding doors, bulkhead Hatch). Solo playable;
host-client co-op exists over UnityTransport (Main Menu Host / Join by Relay code or
typed IPv4; F10/F11 stay the editor fallback). Shared Ship and party EMBARK closed
with `M72` (2026-08-17, Relay `KPRMPR`). A five-Wave friends evening is still the
Roadmap alpha sentence and is not that file.

Your ship cuts into a **Bastion** — a garrisoned alien vessel — and siphons it for Salvage.
You are the **Boarder**. You fight inside it, holding the **Hatch**, the umbilical back to
your ship, while the **Gatekeepers** who hold the Bastion come to take it back. You juice
**intensity** at a trigger: more Salvage, more Gatekeepers. **You cannot leave a Run at will.**

## The pillars

**The player sets both the pace and the stake, and the game supplies neither.** Nothing
starts until you juice, select with `E`, and press `T` at a field terminal, Master Control,
or the Hatch. The clock never pays. Juice is a bet you place at the trigger before you see
what you bought.

**Every Gatekeeper kind is defined by what it refuses to do.** A Hunter can never hurt the Hatch.
A Thief never turns when shot. On a Hatch-bank it never hunts you; on a field-terminal Wave it
does. These are rules in code, not tuning values, so no balance pass can soften one by accident.

**The weapon is the build.** Progression is a weapon and its upgrade wheel, not a stat sheet.
CREW QUARTERS equips a Boarder Protocol, not buyable attributes.

**Death is the only thing that ends a Run early.** There is no bail-out and no respawn
for the Boarder who owns the Run (solo or host), and losing takes nothing back. A second
Boarder going down waits on the Ship (`M57`) and does not end the host's Run (`M49`).

The session is for **Fellowship, Expression, and Fantasy** (`Design/1-Experience.md` §1.2).
The pillars above are the Boarder's contract; restamp this line when the rank table moves.

## The loop

```
                 SHIP                                    BASTION
   ┌───────────────────────────────┐        ┌──────────────────────────────────┐
   │  ARMORY      weapon + wheel   │        │   PREP    quiet. no income.      │
   │  CREW Q.     protocol         │        │           repair, reposition,    │
   │  FABRICATION hatch + rig      │───────▶│           juice intensity        │
   │  NAV MAP     where, how deep  │ embark │              │ E then T          │
   └───────────────────────────────┘        │              ▼                   │
                   ▲                        │   ASSAULT gatekeepers walk in from  │
                   │                        │           spawn pts. kills pay     │
                   │  RETURN TO SHIP        │           Unbanked; intensity locked│
                   │  (died, or Master      │              │                   │
                   │   Control survived)    │              ▼                   │
                   └────────────────────────┤   wave clear ──▶ PREP  (xN)      │
                       Hatch-bank converts  │   Master Control Hatch-bank ──▶ Escaped│
                       Unbanked to the ship │   hatch breached ──▶ run over    │
                                            │   you die        ──▶ run over    │
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
trivial or desperate. `Design/1-Experience.md`, section 1.3. Mean live grades Challenge;
Fellowship and Fantasy stay play-reported.

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
| `Hygiene/` | Owed items grouped into **hygiene passes** — one file per sitting, folder as status. Unobserved criteria from a closed milestone, one-shot leftovers, cleanups only Zo can do. Nothing with a real gate. Index and id register: [`Hygiene/README.md`](Hygiene/README.md). Contract: [`_meta/_definitions/hygiene.md`](_meta/_definitions/hygiene.md). |
| `Roadmap.md` | Order, status, strategy and risks. The summary layer over `Milestones/`. |
| `Changelog.md` | The roadmap's audit trail: milestone state transitions and structural edits, most recent first. |
| `Milestones/` | One file per milestone, filed in a folder named for its status. |
| `Architecture/` | The rules subtree behind `Architecture.md` — boundaries, naming, Unity runtime, testing, and the dated audits. [`Architecture/README.md`](Architecture/README.md) declares itself the master reference for how code is written. |
| `Animation-Inventory.md` | Clip tracker: what is on disk, what code fires that has no clip, and the rulings behind each. |
| `Release-Gate.md` | The pre-send checklist, and [`Release-Send.md`](Release-Send.md) the copy that goes with a build. |
| `GatekeeperZo Notes/` | Zo's own files — `verification.md` is the Next-Run list the dashboard reads; `playthrough.md` is raw session notes. |
| [`Architecture/asset-audit-2026-08-18.md`](Architecture/asset-audit-2026-08-18.md) | Dated snapshot of what art is on disk against what is referenced. Figures drift after its stamp, by its own declaration. |
| `Brainstorms/` | Ideas before they graduate. |
| `Demo - Single/`, `Demo - Coop/` | Solo and friends demo beat specs (voice pipes included). Cut milestones from these; do not duplicate them into a brainstorm. |
| `Story/` | Fiction premise, beats, voices — draft, not on the Design ladder. Index: [`Story/README.md`](Story/README.md). |
| `Marketing/` | Research, strategy, and claim-accurate copy. Pitch lives here, not in `Design/` or `Play/`. Honesty gaps: [`Marketing/gaps.md`](Marketing/gaps.md). |
| `_meta/_definitions/` | `milestone.md`, `brainstorm.md`, `hygiene.md`, `animation-sidecar.md`, and `language.md` — the ubiquitous language every doc, log line and player-facing string uses. |
| `Play/` | The **Compendium** — volumes of related entries. Each hand-written page holds `## Game` (what pause shows) and `## Reference` (fuller brief). Folder path is `Play/` (language lint); player-facing name is Compendium. In-game reader shipped (M62, Esc → Compendium). |

**Cold start:** this page, then `Design/` 1 → 4, then `Architecture.md` and
`Working-Rules.md` before touching anything, then `Decisions.md` before proposing a feature
and `Roadmap.md` before starting work. **If you have never played it**, start instead with the Compendium:
`Play/README.md` (the volumes), then `Play/The-Game.md`. Numbers: `Play/Field-Manual.md`.

**Dashboard:** open [`dashboard.html`](dashboard.html). **Focus** / **Project** read
[`dashboard-data.js`](dashboard-data.js) (regen with `Tools/dashboard-data/run.ps1`) —
lane caps, milestones, hygiene passes, commits, outstanding questions, rule-based next
focus. Both panels warn when the committed data file is older than HEAD or 36 hours.
**Session** chooses the `Logs` folder (or drop `GatekeepersProtocol_*.log`;
`GatekeeperBastion_*.log` still parse) and parses in
the browser: mean live by era (hatch-bank loop ≥ 2026-08-13), kills, spend/SKU, hatch
pressure, intensity, Hatch-bank starts, ability credit. PlayMode / short / Dev are
**labeled**, not dropped, until the corpus is large. Far-wall / `hatchDist` is on the
kill line; `wave-report` prints median and far-wall share per kind.

### Three standing rules

**Countable is generated; arguable is written.** `Tools/docgen/run.sh` writes
`Play/Field-Manual.md` and splices the countable blocks in `Design/4-Data.md` by calling the
same code the game runs, and `--check` exits non-zero on drift (also from
`Tools/verify/run.sh`). A hand edit inside a generated region is lost on the next Run, on
purpose; prose outside `4-Data.md`'s `GKB` markers is preserved. **Do not restate a countable
fact anywhere else** — link to the manual or the block. On 2026-08-04 three documents
described three different games and none was the one in the repo; each had been correct when
written.

**Keep the set small.** The previous project reached 48 markdown files and its own postmortem
named the docs as a failure: productive procrastination. Before adding a file it has to fail
this test: *would an agent otherwise hallucinate this, or repeat a known mistake?* Prefer
editing an existing file. Every line should be a fact, a number with its justification, or a
rule that prevents a specific failure. `Milestones/` is the one exception to the count, and it
has its own rule instead: a milestone file exists only for a milestone in the table, it links
rather than copies, and it dies with the milestone.

**Only recurring document classes get an authoring contract.** `milestone.md`,
`brainstorm.md`, `hygiene.md` and `animation-sidecar.md` qualify, because new instances
get written from memory and drift.
`language.md` is in `_definitions/` under a different rationale and is the only one: it is a
standing rule applying to every file, every log line and every string a player reads, and it
would otherwise be duplicated into six files and drift in four.

Numbers in these docs were read out of `Assets/Scripts/Sim/*.cs` and
`Assets/Scripts/Game/*.cs`. When you retune a Sim/ constant that owns a `4-Data.md` block or
the Field Manual, run `Tools/docgen/run.sh` in the same change. Root `README.md` has
clone-to-play steps.
