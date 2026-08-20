# Voice pipe, co-op

<!--
Purpose: Line ids, speakers and audience rules for the co-op demo. Extends the solo
runner; does not fork it. Low line count by design — there is nothing to teach.
Status: demo spec
Authority: implementable
Update trigger: when a line id is added or cut, when the audience rule changes
Last updated: 2026-08-20
Depends: ../Demo - Single/voice.md
-->

## What carries over unchanged

Everything mechanical. `DemoLines`, `DemoVoice` (LOCAL-OWNER runner) and
`DemoConversationIds` are the solo demo's and are extended, not forked. Shift is the
HUD family. No VO. No player replies. `Time.timeScale = 0` is off the table.
Interact prompts stay mechanical. The Bastion mission drawer may carry the standing
objective and is not a conversation UI.

**Audience**: the host starts a line id and every seat plays it locally. Party-scoped
lines already ride `NetRunMirror.DemoLineClientRpc`.

**Modes**: Lock in Ship and Bastion Prep may hold move and look so they can read, with
one Continue. Subtitle in Assault, auto-advancing in 4 s, with look, move and fire
live. A Boarder under pressure is never rooted.

## What co-op adds

**Scope**, a third field on every line beside id and mode:

| Scope | Meaning |
|---|---|
| **Party** | Host starts it, every seat plays it. The default for anything about the run. |
| **Local** | One seat plays it. For anything about *that* seat: you are down, you are up, you bought back. |

Local lines are still started by the host where the state is host-owned; the RPC
targets one client rather than all. A line that says "you" must be Local. A line that
says "<name>" must be Party, or the other seats never learn who.

**Name substitution.** Several lines carry `<name>`, which needs the name field from
the M-C presence adds (beat 4). Until that lands, `<name>` renders as the slot number
and the lines still work. Missing copy does not block mechanical gates.

**Collision.** When two lines would fire on the same frame, the later beat wins, same
rule as solo. `demo.coop.first_bank_down` wins any collision on the Docked frame.

## Line ids

Copy is stamped here until `DemoCoopLines` exists. Speaker is **Game** throughout;
no guides in this demo.

| id | Beat | Mode | Scope | Text |
|---|---|---|---|---|
| `demo.coop.aboard` | 0 | Subtitle | Local | You are aboard. Kit up at the ARMORY, then take a terminal with them. |
| `demo.coop.crew_pot` | 0 | Lock | Party | One pot for the crew. What they spend, you do not have. |
| `demo.coop.hatches` | 0 | Subtitle | Party | Two hatch doors open with two of you. Both are ways in. |
| `demo.coop.first_terminal` | 1 | Lock | Party | Two hatch doors open, and they will go for whichever is weaker. They will not split evenly. |
| `demo.coop.after_terminal` | 1 | Subtitle | Party | Wave down. The Hatch-bank moves salvage home through those same doors. |
| `demo.coop.you_are_down` | 2 | Subtitle | Local | You are down. They can bring you up. |
| `demo.coop.they_are_down` | 2 | Subtitle | Local | \<name\> is down. |
| `demo.coop.up` | 2 | Subtitle | Local | Up. Get back on a door. |
| `demo.coop.bled_out` | 2 | Subtitle | Party | \<name\> is out. They come back when you next dock. |
| `demo.coop.bank_teach` | 3 | Lock | Party | The pile moves into the crew pot while you hold the doors. Whatever has not moved when a door falls is gone. |
| `demo.coop.first_bank_down` | 3 | Lock | Party | They took the doors. What moved is in the pot. Spend it and go back in. |
| `demo.coop.see_them` | 4 | Subtitle | Local | \<name\> is carrying a \<weapon\>. |
| `demo.coop.re_embark` | 4 | Lock | Party | Same doors. Further in this time. |
| `demo.coop.boss_ahead` | 5 | Lock | Party | Whatever is through there is the last of it. This is the kit you are taking in. |
| `demo.coop.escaped` | 5 | Lock | Party | Clear. You both got out with what you moved. |

Fifteen lines for the whole demo, against the solo demo's much larger set. That gap is
the point: these players have been taught.

## Copy rules

`demo.coop.hatches` reads the open door count and must not be written as fixed text.
At three seats it reads *Three hatch doors open with three of you*.

Anything cause-agnostic stays cause-agnostic. `demo.coop.first_bank_down` plays whether
the doors fell or the last standing seat bled out, exactly as solo's first-loss line
does.

## Create

| Path | Role |
|---|---|
| `Assets/Scripts/Game/Demo/DemoCoopLines.cs` | The table above: id → mode, scope, text. Placeholder `TODO:` entries legal |
| existing `DemoVoice.cs` | Extended with scope. LOCAL-OWNER. No vendor types |
| existing `DemoConversationIds.cs` | `const string` ids added, not a second file |

## Gaps

- `demo.coop.see_them` is a stamp and may read as hand-holding once the weapon is
  actually visible in their hands. Cheapest fix is to cut it.
- Juice is deferred, so `demo.coop.juice_proposed` is cut rather than written. It comes
  back with juice.
- Whether the standing objective should differ per seat is not specced. Stamped: one
  shared standing objective, since nobody owns a door.
