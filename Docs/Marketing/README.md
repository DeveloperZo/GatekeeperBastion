# Marketing

<!--
Purpose: Research, strategy, and claim-accurate copy so the market receives
Gatekeeper's Protocol as the game that ships — not as the ancestor page, and
not as Design jargon.
Status: working
Update trigger: when friends-alpha closes, when a pitch sentence's truth value
changes, or when a comparable's public positioning would change our shelf.
Last updated: 2026-08-30 (0.34.2 grill: Ante parked, kills bank on landing, Hatch-start
loop, respawn from the realm bank, one-deck hull; Play path is Play Mechanics/; GnG demo
reception and the Extraction Shooter tag added to landscape)
-->

Not Design. Not Play Mechanics. Not `Release-Send.md` (install instructions).

**Grill.** A claim that is not true in `../Play Mechanics/` or in a closed milestone is
heading or refused. Play reports in `../GatekeeperZo Notes/playthrough.md` are
the only quality evidence; do not invent reception.

| File | Kind | Answers |
|---|---|---|
| [`strategy.md`](strategy.md) | Strategy | Category, audience, pillars, what we refuse to be on a store page, sequence of markets. |
| [`product.md`](product.md) | Research | What a session is, what is unique, what play actually said, gaps vs the pitch. |
| [`landscape.md`](landscape.md) | Research | Ancestor press, adjacent Steam shelves, comparisons that help vs that mis-shelf us. |
| [`gtm.md`](gtm.md) | Strategy | First market is friends-alpha, not Steam. Blockers and what that send may promise. |
| [`pitch.md`](pitch.md) | Copy | Locked sentences and per-sentence truth. |
| [`gaps.md`](gaps.md) | Research | Where copy cannot be honest yet, and which existing work would make it true. |
| [`art-commission-brief.md`](art-commission-brief.md) | Copy | What an outside artist would need to restyle the kit. Addressed to a contractor, not to a player. |

**External copy** uses Play language (`../_meta/_definitions/language.md`). The line runs
between the copy block and the note beside it: a store sentence says **Boarding**, **Hatch**,
**Convert** (rule 3, trap 8), while the rationale column beside it may name Run, intensity,
juice and Hatch-bank because those are still the code's words. **Ante / ante up are parked
in 0.34.2** (`Ante.Enabled = false`) — they stay legal words, they are not live verbs to
print. Hunicke names, milestone IDs and the Play ban list stay on the builder side of that
line. The machine lint does not reach this folder — it scans `Docs/Play/` when that
directory exists. Player docs now live in `Docs/Play Mechanics/`, which the lint does not
yet cover, so the copy block is checked by reading it.

**A claim's source is an identifier, and identifiers get deleted.** Grep the identifier
before restating a number, and register anything you cannot verify in `gaps.md` rather
than carrying it forward. The 2026-08-25 pass already caught three dead ones; the 0.34.2
pass caught a worse class: whole rules that inverted (payroll, respawn purse, ship decks)
while this folder still described the prior rule.

**Design** remains authority for ranks and rules (`../Design/1-Experience.md`).
This folder does not restamp them.
