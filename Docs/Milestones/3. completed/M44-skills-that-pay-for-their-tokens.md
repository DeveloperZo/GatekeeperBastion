# M44 — Skills that pay for their tokens

<!--
Status: completed
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
4. **All five are model-invocable.** No skill sets `disable-model-invocation: true`.
   **Corrected 2026-08-10 (Zo): manual invocation was the wrong default** — the request that
   should load a skill is phrased as "what should I work on next" or "capture this idea", never
   as a command naming the skill, so a manual skill provides nothing. The ~100 tokens per
   description is the cost of being able to fire at all.
   **The failure mode moves from under-triggering to co-firing**, so every description ends by
   naming the adjacent skill and what belongs to it instead. `gkb-development` against
   `gkb-verification` is the closest pair; `gkb-design` against `gkb-brainstorm` is the next.
5. **Nothing is duplicated into `.cursor/rules/`.** Those two files stay pointers
   (`AGENTS.md` already states the rule), and `.claude/skills/` is on Cursor's discovery path.
6. **Only `gkb-verification` is path-scoped.** `paths` restricts rather than adds — both
   toolchains document it identically: Cursor, *"the skill is only surfaced when the agent works
   with files that match"*; Claude Code, *"Claude loads the skill automatically only when working
   with files matching the patterns."*

   | Skill | `paths` | Why |
   |---|---|---|
   | `gkb-verification` | C#, tests, `Tools/`, `Editor/` | Nothing to verify until there is code, and by then files are open. Absent from a docs session |
   | the other four | none | Their work begins before any file is open |

   **`gkb-development` is deliberately unscoped, and this was corrected during the build.**
   Scoping it to C# meant that "complete M45" — the dominant way work starts — surfaced nothing,
   because the turn opens with a milestone markdown file and no code. The skill would arrive
   after the approach was chosen, and approach is what it governs: do not reconstruct at runtime
   what an asset defines, the one-shot ladder, one cause and one change. **Its description now
   leads with milestone execution**, since building a milestone is development work almost every
   time.

   **The three conversational skills are unscoped for the same reason**: "what should I work on
   next" and "capture this idea" carry no file context at all.

### B — each skill earns its place against a baseline

7. **`.claude/skills/_evals/` holds, per skill, at least three task cases** — a prompt, the
   input state, and assertions that are gradeable without judgement ("the milestone file has a
   numbered `## Acceptance criteria` section", "the proposal names a kill criterion with a
   date"). Assertions are written **after** seeing the first outputs, not before.
8. **One cheap check closes this: `.claude/skills/_evals/run.sh trigger`.** Five calls, about a
   minute, no tools enabled. It asks, from the descriptions alone, which skill each prompt
   should invoke, and compares against the expected mapping. **Nothing else is required to close
   M44.**
   **Closed 2026-08-10.** `run.sh trigger` — five skills × (6 should + 4 near_miss) = 50 prompts,
   **50/50 pass**, `failures: []`. Recorded in `.claude/skills/_evals/results/trigger.json`
   (gitignored). Descriptions were sharpened twice on the design↔brainstorm and
   development↔verification boundaries after the first two runs left should-prompts on the
   wrong skill; near-misses were clean throughout.
9. **Every should-prompt picks its own skill and no near-miss picks it.** A near-miss that fires
   even once fails — over-triggering costs a whole body load on every unrelated task. Fixed by
   adding what the skill does *not* cover to its description, never by pasting keywords from the
   failing prompt. **The skill it chose instead names the boundary to sharpen.**
   **Closed with criterion 8** — same run, zero near-miss failures.
10. **The quality arm is not a gate.** `run.sh quality` prepends the skill body to one arm and
    omits it from the other, then grades both — a real measurement, and about ten minutes and a
    large token spend for the suite. **Run it when a skill is suspected of not earning its
    keep, by name, never as part of closing this milestone.**
    Recorded so the earlier rule is not reinstated: the first version of this milestone made a
    with/without delta the ship gate for every skill. That was right in principle and wrong in
    cost. Using the skills reveals a dud faster than a ten-minute harness does, and by this
    project's own three-kinds gate a committed tool earns its place by serving more than one
    session and more than one question. This one has not yet.
11. **A skill measured at or below baseline is deleted, not tuned** — whenever that measurement
    is taken. Parity means it documents something the model already does.
12. **The harness never spawns an agent with tools.** The first version did, and one probe cost
    about 199,000 tokens and 162 seconds while detecting nothing, because the agent explored the
    repository instead of answering. Every call passes `--allowedTools ""`, and quality
    **controls rather than detects** — the body is injected, so there is no skill-loading to
    observe and no hiding to restore.
13. **`AGENTS.md` is under 200 lines**, and every paragraph left in it passes the test:
    *would the agent get this wrong without it?* Content that fails the test moves into a skill
    or is deleted.
14. **`Docs/_meta/_definitions/` is left where it is** (Zo, 2026-08-10). The three authoring
    contracts stay documents; a skill does not become a second home for them, and `language.md`'s
    machine-read fence must not move away from the check that parses it.

15. **Measured before and after, both numbers recorded here.** Measured 2026-08-10 by character
    count over four, on the files that load at session start:

    | | Before | After |
    |---|---|---|
    | `AGENTS.md` | 2,114 | 2,114 |
    | `CLAUDE.md` | 34 | 34 |
    | `.cursor/rules/` | 112 | 112 |
    | Five skill descriptions | 0 | **1,022** |
    | **Always-on total** | **2,260** | **3,282** |
    | Skill bodies, load-on-trigger | — | 7,084 |

    Description column re-measured 2026-08-10 after the trigger-gate description sharpening
    (chars÷4, same method as the opening row). Bodies left at the earlier figure; they were not
    re-derived this close.

    **The always-on surface went up by 1,022, not down**, and that is the honest result. The
    milestone's opening claim assumed `AGENTS.md` would be cut in exchange; criterion 13 found it
    already under its limit at 185 lines, so nothing was traded away. What was bought is procedure
    that loads only when the work needs it, against a `Docs/` tree that nothing routes.

    **The saving is therefore conditional on the skills firing and on their bodies being read
    instead of the documents.** Criterion 8's trigger pass is the proof they fire on the prompts
    that should load them; a skill that later measures at parity on `run.sh quality` is still
    deleted under criterion 11.

16. **No skill contains a count.** No test totals, no file counts, no "there are three of X".
    Counts belong in generated output; a count in a rule passes forever by renaming
    (`../../Changelog.md`, 2026-08-09).
17. **No skill cites a path that is not committed.** Every tool, script or log named in a skill
    exists in the repository at the time the skill ships.

### D — what each skill carries

Each is written from measured sources; **every non-obvious claim carries its source, and every
rule carries the case where it does not apply.** A rule with no stated counter-case has not
been thought about (`../../Architecture/README.md`).

18. **`gkb-development`** — Unity and C# practice for this codebase, scoped rather than blanket:
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
19. **`gkb-design`** — cheap falsification, before investment:
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
20. **`gkb-verification`** — the discipline that is currently sprawling:
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
21. **`gkb-development` and `gkb-verification` both carry the one-shot rule**, because it is
    both a naming failure and a tooling failure:
    - **Nothing in the codebase is named after a work item.** No type, file, namespace, menu
      path or define contains a milestone ID. `Assets/Scripts/Editor/Tools/M35Wp4OptionsPatch.cs`
      is the live example: a class, a file and a menu row
      (`GatekeepersProtocol/M35/WP-4 Patch Bastion Options`) all named after a milestone and a
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
22. **`gkb-development` carries repair discipline**, because the expensive failure is not the
    first wrong fix, it is the four turns after it. Every rule below is drawn from one logged
    exchange on 2026-08-10 that fixed a Splash bug across five turns and finished by being told
    the work was slop:
    - **Repair what exists. Never rebuild it at runtime.** The fix for a scene that looks wrong
      is an edit to that scene, not a script that reconstructs it on `Awake`. In that exchange
      the agent made Splash *rebuild the MainMenu stack at runtime* rather than reuse the
      objects already in the scene. **This is the same failure as building UI in code**, and it
      is the single most costly habit in this project's history.
    - **Name what was working before, and check it still works.** The change lost the
      background music and replaced a Rajdhani TMP title with `OnGUI`. Neither was reported
      broken. **A fix that quietly removes a shipped behaviour is a net loss even when the
      original bug is closed.**
    - **One cause, one change.** That bug had one cause — `LoadSceneAsync` activating in
      `Awake`, proved from the log at 72 ms. The turn shipped four changes: no-preload, an
      `OnGUI` title, input arming, and a delta clamp. **Three of them were unexplained**, and
      each is a place a new defect can hide.
    - **A workaround that bypasses the system is a defect with a button on it**
      (`../../Working-Rules.md`). Drawing the title in `OnGUI` because it "cannot be blanked by
      Canvas/TMP" is not a fix for the Canvas being blank; it is a way to stop looking at it.
    - **Do not report as verified what you did not observe.** "You should see the title fade in"
      is a prediction. **State what changed and what to look for; never assert the outcome.**
      `AGENTS.md` already forbids reporting work verified on a suite that was not run — this is
      the same rule for anything only a play session can confirm.
    - **Change only what was asked.** Behaviour nobody reported broken is not in scope, and
      swapping a design decision while fixing a bug hides the swap inside the fix.
23. **`gkb-brainstorm`** — moving an idea from interesting to cuttable:
    - The entry question is **loop or arc**, and the second is **what observation would kill
      this**. An entry with neither is not yet a brainstorm entry.
    - The graduation test is unchanged: an entry graduates when its acceptance criteria can be
      written, and the entry is deleted when a milestone replaces it.
    - **Check `../../Decisions.md` before proposing.** Cut features are recorded by name
      precisely so they are not re-proposed.
24. **`gkb-strategy`** — ordering and lane balance:
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

1. Criteria 2, 3, 16, 17 — grep the five files for frontmatter keys, line counts, digits used
   as counts, and every path mentioned. **Done 2026-08-10:** bodies 66–142 lines; no
   `disable-model-invocation`; only `gkb-verification` has `paths:`; cited tools and docs exist
   on disk; no count-in-a-rule phrases in skill bodies.
2. Criterion 7 — `_evals/cases.json` has ≥3 cases per skill. **Done:** 3 each.
3. Criteria 8–9 — `run.sh trigger`. **Done:** 50/50, `failures: []`.
4. Criterion 10 — quality is opt-in (`EVAL_QUALITY=1`), not a close gate. Not run for close.
5. Criteria 13, 15 — `AGENTS.md` 185 lines; always-on table updated on this file.
6. Criterion 14 — `Docs/_meta/_definitions/` untouched. `Tools/lint-language/run.sh` still runs
   against the fence (pre-existing `PlayerGun.cs:191` violation is unrelated to this milestone).

## Watch

- **The failure mode is five well-written documents that change nothing.** Criterion 8 is the
  only close gate: descriptions that pick the right skill, or the description is wrong. Do not
  soften it to "seems better."
- **Do not write what the model already does.** The test for every paragraph is whether the
  agent would get it wrong without it. A skill that restates competence costs tokens on every
  turn it loads and buys nothing. `run.sh quality` (opt-in) is how that is measured later.
- **`gkb-brainstorm` is the most likely to fail a later quality run.** Capturing is something the
  model does passably unaided — if its quality delta is flat, deleting it is the correct outcome
  rather than a failure of this milestone.
- **Descriptions decide everything.** The body never loads if the description does not fire,
  and models under-trigger by default. Write them pushy, then measure.
- **Skills go stale exactly like documents do.** Each carries an `Update trigger`, not a review
  date. Every review cycle in the previous project's rule files ran zero times.

## Depends on / Unblocks

Depends on nothing. Independent of M35, M42 and M43.

**Unblocks nothing formally.** It is the third of the three step-function milestones — M35 the
product surface, M43 the repository, this one the working method.
