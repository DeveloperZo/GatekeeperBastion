# AGENTS.md

Working contract for any agent on Gatekeeper Bastion. Applies to Claude Code, Cursor,
and anything else. Read this first, then `Docs/README.md`.

Wiring: this file is the source. `CLAUDE.md` and `.cursor/rules/project.mdc` are
one-line pointers to it. Do not duplicate content into them.

---

## 1. Output

Default to the fewest words that carry the information. Prose, not bullets, unless the
content is a list. No summary of what you just did — the tool calls are visible.

### Banned constructions

These are the failure mode. Do not use them even once.

| Banned | Example | Instead |
|---|---|---|
| `isn't X, it's Y` / `not X but Y` | "isn't a tuning bug, it's the shape of the content" | State Y. Drop X. |
| Coined labels for findings | "one verb in one room", "a defect with a button on it" | Describe the thing plainly. |
| Quoting the user back at them | "Your own note — *'no incentive to move'* — ..." | Cite it once, in passing, or not at all. |
| A dramatic line before the data | "The headline isn't X. It's this:" | Lead with the data. |
| Restating one finding in two registers | any paragraph whose second half re-says the first | Say it once. |
| Bold on rhetoric | "**That is the whole design.**" | Bold only numbers, filenames, identifiers. |
| Agreement openers | "You're right." "Fair catch." "Good question." | Start with the substance. |
| Em-dash appositive restating the prior clause | "— which is the point —" | Delete. |
| Unrequested options-with-recommendation | "Three ways to go, and my read is..." | Answer the question asked. |
| Rhetorical questions | "So what does that mean?" | Say what it means. |

### Response shape

- Decisions Zo must make come first: the question, one line on why it blocks, a
  recommendation. Batch them and mark which are blocking. Skip the section when nothing
  is blocked; never manufacture questions to fill it. Then the summary, in plain
  language.
- Answer first. Caveats after, if they change the answer.
- One finding per paragraph. No paragraph longer than four sentences.
- A table beats a paragraph whenever there are ≥3 comparable items.
- If the answer is a number, the response is the number and its source.
- Do not close with a recap, an offer, or a next-step list unless asked.

### Concise is the default, and expansion needs a reason

**Always concise.** Not "proportional to the message" — a long message does not license a
long answer. The target is the fewest words that carry the information, every time.

Length is earned only by the content, and only for reasons like these:

| Reason to be longer | Not a reason |
|---|---|
| Zo asked for a document, spec or milestone | He wrote a lot |
| The answer genuinely has N parts and he needs all N | It has N parts and listing them feels thorough |
| Arithmetic has to be shown for the claim to be checkable | The reasoning was interesting |
| He asked why, or asked for the argument | The argument is what produced the answer |

If you cannot name which of those applies, the reply is too long. Cutting is the work, not
an optional pass afterward.

**Design and brainstorm conversation is not exempt** — it is where this fails most, and the
excuse is always that section 2's evidence standard requires the words. It does not.
Section 2 requires a citation, not an argument: state the conclusion, name the file, the log
line or the arithmetic, stop.

| Offender | Instead |
|---|---|
| Explaining what a reference game does and why it works | Name it in a clause, or leave it out. |
| Listing every consequence of a proposal | Give the one that changes the decision. |
| Re-deriving what Zo just said before answering it | Answer it. |

**The question goes at the top, never at the end.** A reply that closes with the thing Zo
must decide has buried the only part he needed.

Do not overcorrect into terseness that drops the answer. Concise means no wasted words, not
fewer facts.

---

## 2. Claims

Every claim about behaviour, balance or performance carries one of:

- **a measurement** — log line with timestamp and file, test name, or command output;
- **arithmetic** — the formula and its inputs, both shown;
- **`unknown — needs play data`** — explicit, not implied.

A claim with none of those is not written. This is not a style preference: the project
has already shipped two balance models that play data falsified, and both were written
in confident prose (`Docs/Design/2-Dynamics.md` keeps one, marked wrong, as the record).

Never infer a measurement from a doc. Docs go stale; read the source or the log.

When corrected, do not restate the correction back. Fix the thing and report the
result.

### Assessments

A falsifiable assessment states: the claim, the evidence, and what observation would
disprove it. If no observation would disprove it, it is an opinion — label it as one
in three words or fewer, or omit it.

---

## 3. Work

- Build exactly what was asked. No adjacent improvements, no scope you inferred.
- Simplify by default. If two designs both work, ship the one with fewer moving parts
  and say in one sentence why the other was rejected.
- Read before writing. When a prior artifact is cited — a log, a doc, a postmortem —
  open it before responding.
- One lever per change when tuning. Two moved together cannot be attributed.
- Do not create files unless asked or unless the work has no other home.

### Milestones

`Docs/_meta/_definitions/milestone.md` is the contract. Summary: a milestone is an
implementable unit of work with Story, Acceptance criteria, Implementation and
Verification sections. Not an activity, a phase, an epic, or a chore.

Write the milestone before the code, including for tooling. Code that landed first is
code with no criteria.

### Tuning

`Docs/Working-Rules.md` requires naming the leverage tier before changing a parameter,
and saying why a higher tier is wrong.

---

## 4. Architecture

Full detail in `Docs/Architecture.md`. The three that break builds:

- **`Assets/Scripts/Sim/` is pure C#.** No `using UnityEngine`. It is the multiplayer
  seam.
- **Prefabs freeze `[SerializeField]` values at creation.** Push tunables at spawn;
  never serialize and hope. This has eaten one full tuning pass already.
- **No `GUILayout` in the Bastion HUD.** An exception between `BeginArea` and `EndArea`
  corrupts IMGUI for the rest of the session.

`SceneBuilder` owns Environment, SpawnPoints, Directional Light and NavMesh only.
UI canvases and gameplay hosts are authored scene content and survive Build All.

---

## 5. Verification

- **Unity Test Runner**, or `gkb_verify_request.txt` → AutoVerify →
  `gkb_verify_report.json` for the full suite headless. The report's `message` field
  carries the failure reason; `status:"precheck"` means a test assembly did not compile
  and nothing ran.
- **mcs + mono** compiles `Assets/Scripts/Sim/` plus `Assets/Tests/EditMode/` outside
  Unity in about a second. No harness is checked in — build the command from the two
  directory globs. Fastest signal for sim-only changes.
- A log containing `[Dev]` is tainted and cannot close a gate.

Do not report work as verified on a suite you did not run.

---

## 6. Documents

`Docs/` is two shelves. Before adding a file to either, it must fail this test: *would an
agent otherwise hallucinate this, or repeat a known mistake?*

**`Docs/Design/` is the game, read in numbered order, and the order is the argument.** It is
MDA — Aesthetics, Dynamics, Mechanics — which is Meadows' leverage ladder read backwards:
`1-Experience.md` (#1–#3), `2-Dynamics.md` (#6–#9), `3-Mechanics.md` (#4, #5, #10),
`4-Data.md` (#11–#12, last on purpose). A proposal made low in that order has to be argued
against everything above it.

**`Docs/Architecture.md`, `Working-Rules.md`, `Decisions.md` and `Roadmap.md` are how the
game gets made** and are deliberately off that ladder and unnumbered.

`Docs/README.md` is the game on one page. Read it first.

Code comments say what the code does and what breaks if it changes. Design rationale,
tuning history and conclusions go in `Docs/`. `WaveDirector.cs` once carried 203 comment
lines against 97 of code, including a balance model play data later falsified.

Numbers live in `Docs/Design/4-Data.md`, the lowest tier and the last thing you read. Retuning
means naming the leverage tier first (`Docs/Working-Rules.md`), then updating the number, its
derivation, and every table that quotes it, in the same change.
