# Gatekeepers and Waves

<!--
Purpose: The gatekeeper roster and the Wave composition curve — what kinds exist, what each one refuses, and how Waves escalate across the five.
Status: brainstorm
Update trigger: when a gatekeeper kind is added or its refusal changes, or when the Wave composition tables move.
Last updated: 2026-08-13
-->

## What this covers

The gatekeeper roster and how Waves are composed from it: which kinds exist, what each kind
refuses to do, and how Waves 1–5 escalate. Not the tuning of any single number — that is
`../Design/4-Data.md` — and not what the player shoots them with, which is
[`armory-and-weapons`](closed/armory-and-weapons.md).

## Where it stands

Three kinds in `Sim/Waves/GatekeeperKind.cs`, each defined by a refusal: the Default walks
to the Hatch, the Hunter refuses the Hatch and comes for the player, the Ram refuses the
player and comes for the door. `WaveDirector`'s `HunterPerWave` / `RamPerWave` tables fix
the counts per Wave and `defaultCount` absorbs the remainder; the siphon dial's surplus
is entirely Default, by design.

All three resolve at the door **in the shipped one-room fight**. Targeting mode (same mix,
who they walk toward) is **M52 / M53** — terminal hunts the Boarder; Hatch-bank is
Hatch-primary; Rams hunt the Boarder on terminal Waves only. Master Control uses both
halves (**M54**).

A **refusal** is the rule that defines a kind, in code, so a balance pass cannot soften it:
Hunter never hurts the Hatch; Ram never hurts the Boarder; Default does both and provokes.
`1-Experience.md` §1.4. Overriding one for a Wave mode is a dated break.

## Graduation test

**A logged five-Wave run with the gatekeeper-seconds spent at the door recorded per Wave** —
which term of `arrivals x dwell` actually breaks the Hatch at Waves 4 and 5. Every valid
loss on record is `HATCH BREACHED` with the player alive, but not one of them says whether
the door fell to too many bodies or to bodies standing too long, and a new kind specified
without that answer is a guess wearing a stat line.

## Ideas

### A gatekeeper you must leave the Hatch for
A fourth kind that shells the Hatch from stand-off range and never closes, so killing it
means walking away from the door. Ignoring one is affordable; ignoring several is not.
The appeal is that it adds a decision — leave or absorb — rather than scaling an existing
number. Two things are unresolved: *whether a threat that cannot hurt the player is
compelling to walk toward is unknown, needs play data* (the Ram has that property and
reads well, but the Ram comes to you), and it would be the first kind to refuse two
things rather than one, which may read as arbitrary rather than legible. It would also be
the first to break the shared 0.7 s swing clock, an exception that needs stating in
`../Design/4-Data.md` rather than assuming.

*Previously specified as milestone M18 and demoted 2026-08-06 — the stat line, Wave
counts and damage band in that file were derived from the Wave-4 arithmetic, not from
play, so they failed the bar rather than the idea. The retired file is in
`../_to_delete/demoted-M18-20260806/` if the numbers are wanted again. M18's ID is
retired and not reused.*
