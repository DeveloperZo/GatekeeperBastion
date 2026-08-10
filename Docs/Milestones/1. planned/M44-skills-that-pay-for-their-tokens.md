# M44 — Skills that pay for their tokens

<!--
Status: later
Update trigger: when a skill is added or retired, when a skill's eval fails, or when the always-on budget changes
Last updated: 2026-08-10
-->

Five skills at `.claude/skills/`, each proven against a no-skill baseline before it ships.
**A skill that does not beat baseline is deleted, not tuned.** The always-on context shrinks;
the procedural knowledge moves behind triggers that load it only when the work needs it.

## Story

As the developer, I want the agent to arrive at each kind of work already competent, without
paying for that competence on every unrelated turn.

## Why it matters

**`Docs/` is 139,568 words across 93 files** — roughly 186,000 tokens, more than a session
holds. Nothing routes it. An agent either reads too little and reinvents, or reads too much
and degrades.

**The loading model is the lever.** A skill's `description` costs ~100 tokens and is always
present; the body costs nothing until it triggers, and a bundled script's *source never enters
context at all* — only its output does
([Agent Skills overview](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview)).
Five descriptions ≈ **500 tokens** against `AGENTS.md`'s current 1,420 words.

**Less instruction has measured better.** Anthropic removed over 80% of Claude Code's system
prompt for this model generation "with no measurable loss on our coding evaluations"
([context engineering, 2026-07-24](https://claude.com/blog/the-new-rules-of-context-engineering-for-claude-5-generation-models)).

Leverage tier (`../../Working-Rules.md`): **#6, information flows** — it changes what reaches
the agent at the moment of the work.

## Constraints that decide the design

| Fact | Source | Consequence |
|---|---|---|
| `.claude/skills/` is read natively by Claude Code **and** by Cursor as a compatibility path | [Cursor skills](https://cursor.com/docs/skills) | **One location serves both tools.** Nothing is duplicated into `.cursor/rules/` |
| Only `name`, `description`, `license`, `compatibility`, `metadata`, `allowed-tools` validate everywhere | [spec](https://agentskills.io/specification) | Claude-Code-only frontmatter breaks portability. `paths` and `disable-model-invocation` are the two extras both tools honour |
| `name` must match its directory, lowercase, hyphens only | spec | `gkb-<area>/SKILL.md` |
| Body: **under 500 lines / 5,000 tokens** | spec, and [best practices](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices) | Overflow goes to `references/`, exactly one level deep |
| An invoked body **persists for the session**; compaction keeps only the **first 5,000 tokens** per skill, 25,000 combined | [context window](https://code.claude.com/docs/en/context-window) | Most important instructions go at the top of the file |
| Models **under-trigger** skills, especially for work they can already do passably | [skill-creator](https://github.com/anthropics/skills/blob/main/skills/skill-creator/SKILL.md) | Descriptions are written deliberately pushy |
| `disable-model-invocation: true` costs **zero** until typed as `/name` | Claude Code docs | The right setting for anything deliberately invoked |

## Acceptance criteria

### A — the five skills exist and are portable

1. **Five directories under `.claude/skills/`**, each with a `SKILL.md`:
   `gkb-development`, `gkb-design`, `gkb-verification`, `gkb-brainstorm`, `gkb-strategy`.
2. **Frontmatter is restricted to `name`, `description`, `metadata`, and where used `paths` and
   `disable-model-invocation`.** No other key appears. Checkable by grep across the five files.
3. **Every body is under 500 lines**, and **the first 40 lines of each contain its
   non-negotiables** — what survives compaction.
4. **`gkb-brainstorm` and `gkb-strategy` set `disable-model-invocation: true`.** Both are
   deliberate acts, not reactions to a file being opened, and both cover work the model will
   attempt unaided rather than trigger for. They cost nothing until invoked as `/gkb-brainstorm`
   and `/gkb-strategy`.
5. **Nothing is duplicated into `.cursor/rules/`.** Those two files stay pointers
   (`AGENTS.md:6-7` already states the rule).

### B — each skill earns its place against a baseline

6. **`.claude/skills/_evals/` holds, per skill, at least three task cases** — a prompt, the
   input state, and assertions that are gradeable without judgement ("the milestone file has a
   numbered `## Acceptance criteria` section", "the proposal names a kill criterion with a
   date"). Assertions are written **after** seeing the first outputs, not before.
7. **Every case runs twice — with the skill and without it — in a clean context**, and the
   result records `pass_rate`, `total_tokens` and `duration_ms` for both.
8. **A skill ships only if it beats its baseline on pass rate.** The recorded delta is the
   evidence. **A skill whose with/without results are identical is deleted**, not rewritten —
   it is documenting something the model already does.
9. **Assertions that pass in both configurations are removed from the eval.** They inflate the
   with-skill number and measure nothing.
10. **Description triggering is measured**, not assumed: per skill, at least six prompts that
    should trigger it and four near-misses that should not. **Near-misses share vocabulary with
    the skill** — a weak negative tests nothing. Trigger rate is recorded.

### C — the always-on surface shrinks

11. **`AGENTS.md` is under 200 lines**, and every paragraph left in it passes the test:
    *would the agent get this wrong without it?* Content that fails the test moves into a skill
    or is deleted.
12. **The three files in `Docs/_meta/_definitions/` (5,904 words) become skill bodies or
    `references/`**, not a fourth copy. `language.md`'s ```language-lint fence stays where
    `Tools/lint-language/run.sh` reads it — **a skill must not become a second home for a
    machine-read rule.**
13. **Measured before and after, and both numbers recorded on this file**: total always-on
    tokens (`AGENTS.md` + `CLAUDE.md` + `.cursor/rules/` + the five descriptions), and the
    word count of `Docs/`. **The after must be smaller on the first.**
14. **No skill contains a count.** No test totals, no file counts, no "there are three of X".
    Counts belong in generated output; a count in a rule passes forever by renaming
    (`../../Changelog.md`, 2026-08-09).
15. **No skill cites a path that is not committed.** Every tool, script or log named in a skill
    exists in the repository at the time the skill ships.

### D — what each skill carries

Each is written from measured sources; **every non-obvious claim carries its source, and every
rule carries the case where it does not apply.** A rule with no stated counter-case has not
been thought about (`../../Architecture/README.md`).

16. **`gkb-development`** — Unity and C# practice for this codebase, scoped rather than blanket:
    - The sim/presentation seam **as the Humble Object pattern**, and the fixed-tick rule that
      follows from it: `Sim/` never reads `Time`, presentation runs on render delta.
    - **`== null` versus `?.` is a presentation-layer rule, not a global one.** The lifetime
      check exists because a destroyed `UnityEngine.Object` has a live C# wrapper; `Sim/`
      contains no such object, so `?.` is correct there. **The current blanket rule is why M41
      baselines 23 files** — the skill states the scope so the count stops growing.
    - The allocation list — boxing, capturing closures, array-valued properties like
      `mesh.vertices`, string concatenation, LINQ — **scoped to per-frame paths only.**
      Editor tooling and once-per-Wave setup are explicitly exempt.
    - `GetComponent` in `Awake`, **re-resolved in `OnEnable` for anything pooled.**
    - Serialized values outrank code defaults; code defaults belong in `Reset()` so the two
      cannot silently diverge (`../../Architecture.md` gotcha 1).
    - **Data-drive on the second call site, not the first.** The solo case is weaker than the
      literature implies — the usual argument is designers iterating without programmers, and
      there is no second person here.
    - **Assembly definitions are adopted for the compile error, not for speed.** The circulated
      multipliers are unsourced; Unity's own documentation claims no number.
17. **`gkb-design`** — cheap falsification, before investment:
    - **Classify every idea as a loop or an arc before costing it**
      ([Cook](https://lostgarden.com/2012/04/30/loops-and-arcs/)). A wave-defense game lives on
      loop quality and a solo developer cannot fund a content treadmill. Arcs are not inferior —
      onboarding and payoff need them — but they are priced differently.
    - **Kill criteria as a state and a date, written before the work starts**, while the
      decision is neutral. **Not on feel** — feel improves discontinuously with polish, so a
      mechanic can be genuinely bad at 80% and good at 95%. Set them on whether the system
      produces interesting decisions, interacts with other systems, and survives co-op.
    - **The agent writes the pre-mortem adversarially.** The technique's stated mechanism is
      giving team members licence to voice doubts, which is the part a solo developer lacks.
    - **Measure to find where to look; decide by playing.** Balanced-by-the-numbers is a common
      way to ship something nobody enjoys.
    - **Graybox under-reads legibility and feedback.** Before killing a mechanic on graybox
      evidence, add hit confirmation and re-test.
    - **MDA is diagnostic vocabulary, not a generative tool.** Use it to locate which layer a
      problem is in. Framework analysis is not the experiment.
18. **`gkb-verification`** — the discipline that is currently sprawling:
    - **The three paths, and only three** (`../../Working-Rules.md`): the offline harness, the
      log reader, the AutoVerify bridge. Adding a fourth means naming the one it replaces.
    - **The three-kinds gate on tooling** — throwaway lives in `/tmp` and is **never named in a
      document**; a committed tool earns it by serving more than one session and more than one
      question; a menu row is the highest bar.
    - **Generated output has one destination and a cleanup step.** Nineteen `gkb_*.log` and
      `*_report.json` files sit at the repository root today, ignored by git and never deleted.
      Ignoring is not cleaning.
    - **A tainted log cannot close a gate**, and work is not reported verified on a suite that
      was not run.
    - **Bundled scripts, not prose procedures.** A script's source never enters context; a
      procedure the agent re-derives each session costs tokens every time.
    - **Determinism is tested, not declared** — a recorded input stream replayed in an EditMode
      test against a state hash. Same-build only; that is a regression check, not a
      cross-platform guarantee.
19. **`gkb-development` and `gkb-verification` both carry the one-shot rule**, because it is
    both a naming failure and a tooling failure:
    - **Nothing in the codebase is named after a work item.** No type, file, namespace, menu
      path or define contains a milestone ID. `Assets/Scripts/Editor/Tools/M35Wp4OptionsPatch.cs`
      is the live example: a class, a file and a menu row
      (`GatekeeperBastion/M35/WP-4 Patch Bastion Options`) all named after a milestone and a
      work package. **A type is named for the responsibility it holds**
      (`../../Architecture/2-Naming.md`), and a milestone ID is not a responsibility — it is a
      date stamp that stops meaning anything the moment the milestone ships.
    - **A one-shot migration is not a committed asset.** It lives in `/tmp` and is never named
      in a document (`../../Working-Rules.md`, the three-kinds gate). If it genuinely must run
      inside Unity, it is a plain `[MenuItem]` invoked once and deleted **in the same commit
      that lands its output** — not left in the tree to be deleted later.
    - **No `[InitializeOnLoad]` that polls.** `M35Wp4OptionsPatch` registers an
      `EditorApplication.update` handler firing every 0.5 s on every domain reload. This is the
      `VerifyKickOnce.cs` incident again (`../../Working-Rules.md`): a one-shot whose own
      comment said delete-after-verify and which was still firing days later. **A tool that runs
      because the editor loaded cannot be un-run.**
    - **Nothing deletes itself.** `DeleteSelf()` removes its own `.cs` from the AssetDatabase on
      success, so the repository state after a run depends on whether the run succeeded. That is
      not reviewable and it is not revertable.
    - **A file-sentinel handshake is the AutoVerify bridge, and there is one of them.**
      `gkb_wp4_options_request.txt` / `gkb_wp4_options_report.log` is a second copy of a
      mechanism that already exists and is already the documented third verification path.
    - **No stray file under `Assets/`.** `Assets/Scripts/Editor/Tools/_wake2.txt` is a bare
      timestamp with a `.meta`, committed into the source tree.
    - **A new menu row names the row it replaces** (`../../Working-Rules.md`). A milestone-named
      submenu is a namespace that outlives its reason.
21. **`gkb-brainstorm`** — moving an idea from interesting to cuttable:
    - The entry question is **loop or arc**, and the second is **what observation would kill
      this**. An entry with neither is not yet a brainstorm entry.
    - The graduation test is unchanged: an entry graduates when its acceptance criteria can be
      written, and the entry is deleted when a milestone replaces it.
    - **Check `../../Decisions.md` before proposing.** Cut features are recorded by name
      precisely so they are not re-proposed.
22. **`gkb-strategy`** — ordering and lane balance:
    - Three lanes, queue caps, and the rule that a lane may not run ahead of the lane feeding
      it. **The cap on unbuilt planning is the point** — the previous project's postmortem named
      documentation as productive procrastination.
    - The Meadows leverage tier as the ordering argument, and the durable-tooling exception
      requiring a stated multiplier.
    - **Countable is generated; arguable is written.** Do not restate a countable fact in prose.
    - **Lead with what needs deciding, and always carry a recommendation.**

## Implementation

**Create** `.claude/skills/{gkb-development,gkb-design,gkb-verification,gkb-brainstorm,gkb-strategy}/SKILL.md`,
plus `references/` and `scripts/` where a body would otherwise exceed its budget.

**Create** `.claude/skills/_evals/` — cases, and a runner that executes each case with and
without the skill and writes the deltas. The runner is a bundled script, not a documented
procedure.

**Change** `AGENTS.md` — cut to under 200 lines, moving procedure into the skills.
**Move** the three `_meta/_definitions/` files into skill bodies or `references/`, leaving
`language.md`'s lint fence where the tool reads it.

**Change** `Docs/README.md` — the router gains one row naming `.claude/skills/` and what lives
there, so the skills are discoverable without reading this milestone.

Order: write one skill and its evals end to end **before** starting the second. Five skills
written blind and evaluated at the end is five times the rework.

Rejected: duplicating skills into `.cursor/rules/`. It reintroduces the always-on cost the
milestone exists to remove, and `.claude/skills/` is already on Cursor's discovery path.

Rejected: one large skill covering all five areas. Over-broad skills are hard to trigger
precisely and the agent "may pursue unproductive paths triggered by instructions that don't
apply" (best practices).

Rejected: a skill per Unity subsystem. Over-narrow scoping forces several skills to load for
one task, and they then contradict each other.

Rejected: writing the skills first and evaluating later. Anthropic's stated order is evals
first, "so your Skill solves real problems rather than documenting imagined ones."

## Verification

1. Criteria 2, 3, 14, 15 — grep the five files for frontmatter keys, line counts, digits used
   as counts, and every path mentioned.
2. Criterion 7 — the eval runner, showing both configurations per case.
3. Criterion 8 — read the deltas. **Any skill at parity is deleted in the same commit**, and
   the deletion is recorded on this file with its numbers.
4. Criterion 10 — the trigger sets, with recorded rates.
5. Criteria 11, 13 — the before and after token measurements, written onto this file.
6. Criterion 12 — `Tools/lint-language/run.sh` still passes, proving the fence did not move.

## Watch

- **The failure mode is five well-written documents that change nothing.** Criterion 8 is the
  only defence: a measured delta, or the skill goes. Do not soften it to "seems better."
- **Do not write what the model already does.** The test for every paragraph is whether the
  agent would get it wrong without it. A skill that restates competence costs tokens on every
  turn it loads and buys nothing.
- **`gkb-brainstorm` is the most likely to fail its eval.** Brainstorming is something the
  model does passably unaided, which is why it is manual-invocation only — and if its delta is
  flat, deleting it is the correct outcome rather than a failure of this milestone.
- **Descriptions decide everything.** The body never loads if the description does not fire,
  and models under-trigger by default. Write them pushy, then measure.
- **Skills go stale exactly like documents do.** Each carries an `Update trigger`, not a review
  date. Every review cycle in the previous project's rule files ran zero times.

## Depends on / Unblocks

Depends on nothing. Independent of M35, M42 and M43.

**Unblocks nothing formally.** It is the third of the three step-function milestones — M35 the
product surface, M43 the repository, this one the working method.
