# Verification

<!--
Purpose: The single list of what is shipped but unproven, ordered so Zo can spend a playthrough on the items that matter and skip the ones that do not. Milestones own their own criteria; this is the condensed view across all of them.
Status: working — regenerated as milestones ship and items close
Update trigger: when a milestone ships with open rows, or when an item is verified
Last updated: 2026-08-17
-->

## How this works

Shipped milestones carry rows that only a played Run can close. **They accumulate faster
than they get checked, and that is fine** — the point of this file is that they accumulate
somewhere ordered, so nothing is silently dropped and nothing low-value takes up attention.

**Ordered by leverage, not by age.** Work the first section during a normal Run. Ignore the
last section entirely unless you are at a keyboard with the editor open.

**Closing an item is two edits**: mark it here, and update the row in the milestone that owns
it. A milestone's own file stays the record; this is the index.

**An item nobody has closed after three sessions is a signal the item is not worth checking.**
Say so and delete it rather than carrying it forever.

---

## Next Run — four things, one Run

These are the four claims the game currently rests on that nothing has tested.

### 1. The Pin Shot fires at all — M31, criteria 7 and 10

**Code fix landed 2026-08-11** (weapon-scoped charge; corpse tipping shots do not spend).
Still needs a clean play log. Embark with MR-35, land three first-body hits within 1.25 s
(trash kills count), then a hit on a living body — expect `MR-35 Rifle — Pin Shot` and
`pin held Ns`. At T10 the living tip is usually a Thief (code id Ram).

- **If no pin line appears, stop and say so.**

### 2. The decoy peels them off the door — M30, playthrough path

The answer to the oldest open design problem in the project, and it has never been played.

- Buy the drone payload, embark at Wave 3, place a decoy **away from the Hatch**.
- Expect Defaults and Hunters to peel, and **every Thief to walk straight past it**.
- Use three charges, then try a fourth: expect a refusal.
- Read the `Decoy` line on the Wave summary.

### 3. `SEC-D` changes how you fight — M26, playthrough path

- Embark with MR-35 plus `SEC-D`. Let a Hunter reach contact range: it should die fast.
- Shoot a Default across the room with `SEC-D`: it should do close to nothing.
- **The question to answer in `playthrough.md`:** does it change how you fight, or is it only
  what you press when the Primary is dry? If the second, M26's premise is wrong.

### 4. Two opposite wheel focuses feel like different weapons — M21, playthrough feel

M21's whole claim. Take the BR-12 to two opposite focuses at the same total points and play a
Wave with each. **If they feel the same, the wheel is a spreadsheet with a nice picture on it**
and that is worth knowing now that M32 has added a third Primary to it.

---

## Two machines — M72 criterion 9

**Closed 2026-08-17.** Host log `GatekeepersProtocol_2026-08-17_13-24-37_p49280` /
`Player.log`: Relay `KPRMPR`, `clientId=1`, Ready gate, countdown, Bastion
`playerCount=2`, kills, party return Ship, no `[Dev]`.

---

## When convenient

Real, lower leverage. None of these blocks anything.

| Item | Milestone | What to do |
|---|---|---|
| A solo run completes on the dressed bastion | M61-2 | Criterion 14: play one run through to the end screen. Waves already proven to arrive on the final geometry (log `2026-08-14_17-29-54`, untainted, hatchDist to 4.0 m) — only the completion is unproven |
| Walk every block kind in the dressed bastion | M61-2 | Criterion 4: stand in each block kind — no sky through walls, no gaps at neighbours, no floating fixtures. One dated line per kind in `playthrough.md`, screenshot anything questionable |
| M37 median compare on the kit bastion | M47 | Criterion 18: `wave-report` on a kit-bastion log vs the old pin; *unknown — needs wave-report* |
| PRI-C owns the far wall | M32 | Buy PRI-C, embark Wave 3, stand at the Hatch and fire up the bore; expect far kills. Swap to MR-35 at the same Tier; expect those kills not to land. Then `F` for Railshot into a line — expect `railshot armed` / `railshot fired` Ability lines |
| Options reach the fight | M24 | Menu OPTIONS, set sensitivity, embark, press `O` mid-Assault, confirm Gatekeepers keep moving and the change applies in the same Run. One `[Settings]` line per commit |
| Reload discard warning | M25 | Mid-reload, read `SWAP DISCARDS THIS RELOAD`, swap, confirm the discard line. Then turn it off in options and confirm `reload paused` |
| Groups walk out of the closet | M73 | EMBARK, `E` then `T`. Slab rises for the group, not the Boarder. Log `breached at S_S` and no `[Dev]`. Control Room: same from `S_ControlW` / `S_ControlE` after Master Control |
| Juice unused on ten Runs | M55 | Kill criterion: if every post-close start-Wave line is `intensity=0`, stop. Readable off `intensity=` |
| MK-1 Regen Surge mag after wheel points | M71 | Criterion 7: with ability points bought, the Regen Surge line's dial value changes (4 s at 0, 10 s at focus-max 50) |
| First Run with no rig | M23 | The Hatch budget numbers assume the rig. Play one Run without buying it and see whether the door survives Wave 2 |
| Rig prices 300 + 300 | M23 | Marked *tunable, needs play data*. Only worth an opinion after the item above |
| Cold-start premise | M27 | Needs **another person**. Someone who has not read the wiki embarks and then says who the Gatekeepers are and what the Hatch does |
| `SEC-D` kill share | M26 | Not a play item: grep a logged Run for `with Secondary` against `with Primary` |

---

## Not play — developer or environment

**Ignore these during a playthrough.** They need the editor, a missing file, or a decision.

| Item | Milestone | Blocker |
|---|---|---|
| Stats baseline for the dressed bastion | M61-2 | Criterion 13: Stats window standing in the ControlRoom at 1080p, numbers into `playthrough.md`. The before-numbers are unrecoverable — the primitive bastion no longer exists — so record after-only as the baseline the next pass argues from |
| Pinned fixture logs | M29 | `GatekeeperBastion_2026-08-07_16-34-30.log` and `..._2026-08-03_19-18-22.log` are **absent from this machine** and are cited across `Design/` and two milestones. Restore from backup or the pins are unverifiable forever |
| EditMode suite green | M29 | IntegrationCheck hit `stuck` at 180 s twice while tests were still running. Re-run from the menu once the editor is quiet |
| Save byte-identical | M24 | Needs one clean IntegrationCheck run |
| Decoy Path 2 + PlayMode tests | M30 | `DecoyPlayModeTests` written, never run |
| `FireRay` layer mask | M31 | No Enemy or World layers exist yet; buffer raised 16 to 32 with a full-buffer warning as the interim |
| `SEC-D` ability | M26 | **Blocked on Zo**, not on testing. Ability meeting M20's contract. Ring shipped 2026-08-15 (F–A–R–C–D); Ability spoke not purchasable until this lands |

---

## Closed

| Item | Milestone | Closed by |
|---|---|---|
| Shared Ship / party EMBARK | M72 | 2026-08-17 close. Host `13-24-37` Relay `KPRMPR`, `clientId=1`, Ready gate, countdown, `playerCount=2`, party return, no `[Dev]`. Guest log not on this machine |
| Default Protocol in a Run | M70 | 2026-08-17 close. `09-43-30` Barrage/Rush sat-ready + window ended, no `[Dev]`; `00-44-24` claimed Default; `08-37-17` `self refused 21s left`. Unequipped `refused — no protocol` not in a log |
| MK-1 wheel ring | M21 | 2026-08-14, M71: ring D–A–R–C–F authored, strength Reload / weakness Damage; `RingsAreUniqueAcrossShopWeapons` and `WeaknessNeverAdjacentToStrength` cover it, harness green |
| SEC-D wheel ring | M21 | 2026-08-15, M71: ring F–A–R–C–D authored, strength FireRate / weakness Reload; Ability spoke not purchasable |
| `— N live` on group lines, segment lands on zero | M29 | 2026-08-09 session `15-21-59`: group lines read `— 3 live`, `— 6 live`, `— 5 live`; the reader parses it and the Wave-3 segment resolves |
