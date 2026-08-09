# M22 — The game stops misreporting itself

<!--
Status: shipped, August 2026
Update trigger: never — shipped milestones are a record. Corrections only.
Last updated: 2026-08-07
-->

End-screen Salvage figures match the bank, and a Dev-tainted session says so on screen.

## Done when

**Met.** `AwardKill` floors once into bank and `SalvageLedger.RunSalvage` (now `long`);
`LossReport` is `{ cause, grossEarned, repairSpent }` with net derived by the HUD;
both end screens show gross / repair / net; `DevTaintBanner` reads `GameSession.DevTainted`
on the Bastion HUD, end screens, and station panels (compiles out with Dev). AutoVerify
EditMode **213 / 0** (incl. two `SalvageDriftTests`), PlayMode **12 / 0** (incl. two
`DevTaintBannerTests`) — `gkb_verify_report.json`, 2026-08-07T21:32:19Z. mcs harness
(EditMode minus Controls/Dev) **202 / 0**.

## What shipped

- `RunSim.AwardKill` — one `floor(CurrentKillValue)` to ledger and `MetaState`.
- `SalvageLedger.RunSalvage` is `long`; `Earn(long)`.
- `LossReport.grossEarned` + `repairSpent` (no pre-subtracted `earned`).
- End screens and `GameController.EndScreen` log: gross, repair, net.
- `DevTaintBanner` shared by `HudController` and `StationTerminal`.
- `SalvageDriftTests`, `DevTaintBannerTests`; `RunReportAssertions` updated.
- `Docs/Design/4-Data.md` notes pre/post-M22 log incomparability; `Docs/Play/Salvage.md`
  Gaps for drift / net label / silent taint removed.
- `AutoVerify` PlayMode kick: ignore late EditMode `RunStarted`, resume kick after
  domain reload / lost `delayCall` (unblocked PlayMode after EditMode 213).

## What it did not settle

Whether the taint banner should offer "discard and reload from disk" — open in the
milestone; out of scope here.

## Story

As a player, I want the numbers on screen to match what actually happened to my Salvage,
so that I can trust the end screen enough to make decisions from it.

## Why it matters

Both defects were found by writing `Docs/Play/` from the code (2026-08-06). Both are
cheap. Neither is a balance question.

**The end-screen figure is wrong twice over.** `RunSim.BuildLossReport` computes
`gross = floor(Salvage.RunSalvage)` — the Run tally, which accumulates the *unrounded*
kill value — and then reports `earned = gross - RepairSalvageSpent`. Two separate
problems fall out:

1. **It overstates what was banked.** `AwardKill` credits the bank
   `floor(CurrentKillValue)` per kill but adds the raw float to the Run tally, so the
   tally floors once at the end where the bank floored on every body. Measured in the
   2026-08-04 logs: **19 and 21 Salvage of drift** on two Runs, and 0 on a third where
   the dial happened to make the kill value whole. The player is shown money they never
   received.
2. **It is labelled "earned this Run: N — already banked"** while being a figure net of
   repairs. The gross is never shown anywhere. A Run that killed well and then patched
   the Hatch reads as a poor run, and "already banked" is false of the net number —
   the gross was banked, then repair was withdrawn.

**A Dev-tainted session discards progress silently.** `GameSession.DevTainted` making
`Save()` a no-op is correct and deliberate (M13's Watch: a tainted session must never
overwrite a real campaign). The defect is that it is announced **only in the log**. On
screen, purchases appear to happen, Salvage appears to accumulate, and none of it exists
next launch. At this stage the person playing is the person developing, so this loses
real play sessions.

Leverage tier (`../../Working-Rules.md`): **#6, information flows.** Neither fix changes
a rule or a number — both change what the game tells you about state it already has.

## Acceptance criteria

1. `AwardKill` floors the kill value **once** and gives the same integer to both the bank
   and the Run tally. `SalvageLedger.RunSalvage` and the sum of banked awards agree
   exactly, for any dial and any Scavenging level.
2. An EditMode test drives a Run of mixed kill values at a fractional dial multiplier and
   asserts `Salvage.RunSalvage == ` the total credited to `MetaState`, to the unit.
3. The end screen shows **gross earned**, **repair spent**, and **net** as three distinct
   labelled figures. No figure labelled "earned" is net of anything.
4. "already banked" appears only against the gross figure, because that is the only one
   that was.
5. `LossReport` carries gross and net separately rather than one pre-subtracted field, so
   the HUD is not the thing doing the arithmetic.
6. When `GameSession.DevTainted` is true, a persistent on-screen banner states that
   progress will not be saved. It is visible in the Bastion HUD, on both end screens, and
   on every ship station panel — anywhere the player might spend or earn.
7. The banner is present from the moment the first Dev command runs, not from the next
   scene load.
8. A PlayMode test asserts the banner's condition is live after a Dev command and absent
   on a clean session.

## Usable, or it did not ship

- **Driven** — nothing new to drive; both are readouts.
- **Shown** — three figures on the end screen instead of one; a taint banner wherever
  Salvage is displayed.
- **Reads back** — the existing `Save skipped — session tainted by Dev mode` log line
  stays, and the end-screen log line gains the gross alongside the net.

## Implementation

### Sim

**Change** `Sim/Run/RunSim.cs` — in `AwardKill`, compute `long award =
(long)Math.Floor((double)CurrentKillValue)` once, then `Salvage.Earn(award, 1)` and
`_meta.EarnSalvage(award)`. This is the whole drift fix.

**Change** `Sim/Run/LossReport` — replace the single `earned` with `grossEarned` and
`repairSpent`; net is derived by the caller. Do not keep a pre-subtracted field "for
convenience"; that is the field that caused this.

**Change** `Sim/Run/SalvageLedger.cs` — `RunSalvage` can stay a float for display, but its
comment must stop claiming it mirrors the bank if it no longer needs to, or become a long.
Prefer `long`: there is no longer a fractional value to hold.

### Game

**Change** `Game/Bastion/HudController.EndScreens.cs` — three lines, three labels, on
both the Lost and the Escaped screen. Identical treatment on both (the existing file
comment already argues why the two screens must not diverge).

**Change** `Game/Bastion/GameController.EndScreen.cs` — the `Log.Event` line reports
gross, repair and net.

**Create** the taint banner. It reads `GameSession.DevTainted` and nothing else. Put the
draw call somewhere both `HudController` and `StationTerminal` can use rather than
duplicating the string in two files.

**Compile-out check:** M13 compiles Dev mode out of release builds. The banner must
compile out with it and must not leave a dangling reference in `StationTerminal`.

### Tests

**Create** `Tests/EditMode/Run/SalvageDriftTests.cs` — criterion 2.

**Change** the existing end-of-run tests for the `LossReport` shape change.

**Create** the PlayMode assertion for criterion 8, using the existing dev-command path.

## Open questions

- **Whether the taint banner should also offer a way out** — a "discard and reload from
  disk" action — is unresolved. It would turn a warning into a recovery. Out of scope
  here; the banner is the milestone.

## Watch

- Do not "fix" the drift by making the bank take the float. Salvage is an integer
  currency; the bank is the correct side and the tally is the one that was wrong.
- The drift fix changes numbers in every future log. Runs logged before it are not
  comparable on `earned` to runs after it, and `../../Design/2-Dynamics.md` should say so where
  it quotes run takes.

## Depends on

Nothing. Independent of M20 and M21.
