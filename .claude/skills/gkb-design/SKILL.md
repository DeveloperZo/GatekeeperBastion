---
name: gkb-design
description: Game design method for Gatekeeper's Protocol — classifying an idea as a loop or an arc, writing kill criteria before building, when to measure versus when to play, and what graybox evidence cannot tell you. Use this skill whenever a mechanic, weapon, enemy, economy or progression change is being evaluated, scoped, argued about or tuned, and before a milestone is written for one — including when someone proposes adding something new to the game and the question is whether it belongs. Use it even when the idea seems obviously good, because the point is to find the cheapest thing that could kill it. For capturing or parking an idea into a brainstorm document without judging it, invoke gkb-brainstorm instead.
metadata:
  project: gatekeepers-protocol
  update-trigger: when a design rule is added or a tuning approach is proven wrong in play
---

# Design

## Never

- **Cost an idea before classifying it as a loop or an arc.**
- **Set a kill criterion on feel.** Feel improves discontinuously with polish; a mechanic can be
  genuinely bad at 80% and good at 95%.
- **Propose something on the cut list** in `Docs/Decisions.md` without a stated reason the
  original verdict was wrong or has expired.
- **Let framework analysis substitute for the experiment.**
- **Kill a mechanic on graybox evidence** when what it lacks is feedback.
- **Retune against arithmetic alone.** It moves the symptom and leaves the structure. That has
  already happened here once, and the resulting model is marked wrong in `Docs/Design/` rather
  than deleted.

## Always

- **State what observation would disprove this**, before the work starts.
- **Write a kill criterion for anything that is a bet** — a measurable state plus a date, agreed
  while the decision is still neutral. *A defect fix is not a bet and needs none.*
- **Measure to find where to look. Decide by playing.**
- **Write the pre-mortem adversarially**, because solo there is nobody else to voice the doubt.
- **Prototype to answer one named question, with a deadline.**

---

## Loop or arc — the first question, every time

A **loop** is mental model, action, feedback, updated model, repeated. An **arc** is consumed
once. A loop generates ongoing engagement from a fixed authoring cost; an arc puts you on a
content treadmill.

**Wave defense lives on loop quality, and a solo developer cannot fund a treadmill.** A
handcrafted scripted wave is an arc. An enemy composition rule that produces novel pressure is a
loop.

Arcs are not inferior and the game needs some — onboarding and payoff do work loops cannot. They
are priced differently, and pricing them is what this classification is for.

## Kill criteria

Written **before** the work: *if X is not true by Y, this stops.* Pre-registered, because
quitting on time reliably feels like quitting too early once you are invested.

**Not on feel.** Set them on what does not improve with polish:

- Does it produce interesting decisions, or one dominant answer?
- Does it interact with the systems already in the game, or sit beside them?
- Does it survive co-op, where there is more than one player?

Acceptance criteria answer *did it work*. Kill criteria answer *should we still be doing this*.
Different questions; both belong on a milestone that is a bet.

**This is the one place a forward date is written.** The roadmap stamps dates on completion only
and never projects them; a kill criterion is the deliberate exception, because its whole
function is to be set in advance.

## Pre-mortem

The technique's stated mechanism is giving people licence to voice doubts. **Solo, that person
does not exist**, so the agent writes it and does not soften it: concrete failure modes, each
with the observation that would reveal it.

## Measuring versus playing

**Measure to find where to look. Decide by playing.**

Balanced-by-the-numbers is a common way to ship something nobody enjoys. The canonical case: a
designer found players expected to win 1.5-to-0.5 battles and felt cheated when they lost, so
the odds moved rather than the players. **The numbers that matter are perceived**, and they are
not the numbers in the spreadsheet.

The counter-trap: tuning purely to perception in a co-op game creates exploits.

**Instrument rather than guess.** This project's sim is testable offline and its session log is
the record; a balance claim cites `Tools/wave-report/run.sh`, not an ad-hoc parse. What
telemetry cannot give you is legibility — that needs someone who has not seen the systems.

## Graybox

Graybox strips confounds and is a falsification tool, not a debt.

**It systematically under-reads anything whose value is feedback or legibility** — telegraphs,
hit confirmation, spatial readability. A mechanic judged with no feedback reads worse than it
is. Before killing one on graybox evidence, add the hit flash and re-test.

## On the frameworks

**MDA** earns one use: the perspective inversion. You author mechanics; the player only ever
experiences aesthetics, working backwards. Use it to locate which layer a problem sits in. It is
weak as a generative tool, and neither it nor the elemental tetrad will tell you whether a
mechanic is good.

## Done enough

A mechanic gets another pass while sessions keep producing new problems *with it*, and is done
when a session produces only problems belonging to other systems.

**Solo, that never fires** — you stop generating novel observations about your own systems long
before they are good. Substitute: done for this milestone when its acceptance criteria pass, and
explicitly allowed one revisit at integration.

Any numeric threshold you read for this — three clean playtests, an 80% completion rate — is
unsourced. Do not adopt one.
