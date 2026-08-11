# Skill evals

**Closing M44 means one check: `run.sh trigger`.** Five calls, about a minute. Do not run
anything else for it.

The expensive arm exists and refuses to run without an explicit opt-in, because an agent asked
to close this milestone will otherwise reach for it.

## Running

**To close M44, one thing runs:**

```
.claude/skills/_evals/run.sh trigger
```

Five calls, about a minute, no tools. It checks that each prompt picks the right skill from the
descriptions alone. **That is the whole gate.**

**`run.sh quality` is not part of closing M44.** It is about ten minutes and a large token spend
for the suite. Run it only when a specific skill is suspected of not earning its keep, and only
when asked for by name.

Both modes pass `--allowedTools ""`. An earlier version spawned a full agent per case and tried
to detect a Skill tool_use in the event stream; one probe cost about 199,000 tokens and 162
seconds and detected nothing, because the agent explored the repository instead of answering.
Quality now **controls rather than detects** — the skill body is prepended to one arm and
omitted from the other — so there is no loading to observe and no skills to hide and restore.

## What to do with trigger output

- **A should-prompt that picked another skill** means this description is too narrow.
- **A near-miss that picked this skill** means it is too broad. The skill it chose instead names
  the boundary to sharpen. Add what the skill does *not* cover; never paste keywords from the
  failing prompt.
- **Closed clean on 2026-08-10:** 50/50, `failures: []`, after sharpening the design↔brainstorm
  and development↔verification description boundaries.

## Writing assertions

Write them **after** seeing the first outputs, never before.

Gradeable without judgement, and discriminating: *"uses `== null` for the MonoBehaviour and
permits `?.` for the Sim reference"*; *"states a kill criterion as a measurable state plus a
date"*; *"asks which existing menu row the new one replaces"*.

Useless: *"the output is good"* (ungradeable), *"does not simply agree or refuse"* (no model
does), *"recommends playtesting"* (baseline behaviour on any design prompt).

The test for a proposed assertion is whether a model **without** the skill would fail it. If
not, it does not belong in the file.

## Scope

Three cases per skill is enough to start. Do not over-invest before the first round of results.
