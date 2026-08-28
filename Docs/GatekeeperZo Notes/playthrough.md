Repairing the Hatch should cost resources. 

The upgrades a player can make need to be aligned with getting them to progress further without dying on the same Wave more than 3 times.

Observation: Right now there's no incentive to move away from the Hatch

Playing after dying does not return the state and wave counters correctly. Restarts should land with the appropriate state.

---

# Sessions

One entry per play session. **The verdict is the point** — the log file holds the numbers,
this holds what it was like. Numbers alone cannot be read as quality until there are enough
paired entries here to check an inference against. Until then, nothing in `Design/2-Dynamics.md`
gets to claim a Wave was fun, boring, hard or easy.

Keep an entry to a few lines. Format: date, what was played, the verdict, anything that
stood out. Link the log by filename.

## How entries get written (Zo, 2026-08-09)

**The agent drafts the entry from the log; Zo supplies the verdict.** The two are separate
lines and never merge. Zo's three responses:

| Response | Means |
|---|---|
| **Concur** | The assessment matches what it felt like. The verdict line records that and the entry closes. |
| **Update** | The assessment is wrong or incomplete. Zo's words replace it; the agent's draft is deleted, not argued with. |
| **Replay** | Zo cannot say anything from memory. The entry stays open and the session is played again. |

**A `pending` verdict flags a claim; it does not block one** (Zo, 2026-08-09). The bar scales
with what the claim is being used to move, because the real question is not how sure you are —
it is **how expensive the change is to reverse.** Leverage tier is the proxy
(`../Working-Rules.md`).

| Tier of the change | Bar | If it turns out wrong |
|---|---|---|
| **#12–#10** — a constant, a table, a curve | An unconfirmed assessment is enough. Tag it `[unconfirmed]` and go. | Move the number back. One commit. |
| **#6–#5** — a new information flow, a new rule, a verb the player did not have | Say so at the top of the milestone and proceed unless Zo objects. | A shipped capability comes back out, and something was built on it. |
| **#4–#1** — self-organisation, goals, the paradigm | Proceed only on Zo's explicit say-so, recorded with the date. | The doc set was reordered around it. |

**Override is always available at every tier.** "Go" from Zo closes the question and the
record says he did, which is the whole mechanism — not a gate, a trail.

**The flag travels with the claim.** A milestone criterion resting on an unconfirmed
assessment carries `[unconfirmed: <entry date>]` in the criterion itself, not in a footnote.
That is what makes a later "that was wrong" cheap: `grep -rn "\[unconfirmed"` returns
everything that depended on it. A claim that quietly loses its flag on the way into a design
doc is the failure this is guarding, and it is a smaller failure than blocking on a verdict
nobody has time to give.


**What earns an entry.** A Run that reached a decision the log can show — a purchase, a
Wave cleared or lost with the Hatch under pressure, a new loadout, a criterion being tested.
Not: a session under one Wave, a `[Dev]`-tainted Run, or a repeat of a reading already
recorded. Two sessions on 2026-08-08 (`00-03-53`, and `14-45-28`/`15-03-32`) were skipped on
that bar — the first re-read the same 0.9 ability figure already logged on 08-07.

**`[Dev]` taint is per-Run, not per-file.** A file can carry `[Dev]` lines and still hold
clean Runs if the taint arrives after they end. Say which. F10/F11 host/join log `[Net]`
and are not taint.

## 2026-08-07 — first five-Wave clear

Started at Wave 2. MR-35 T1, died on Wave 3. Bought up to T4, went again, cleared all five.

**Verdict: fun and semi-challenging.**

Log: `Logs/GatekeeperBastion_2026-08-07_16-34-30.log`. Numbers in `../Design/2-Dynamics.md` under
*Five-Wave session*. Hatch finished 300/300 on every Wave of the T4 run; kill rate rose
1.23 → 1.50 → 1.67/s across Waves 3–5.

## 2026-08-07 — M20 ability smoke (MK-1 only)

Started at Wave 2 with MR-35 T4 + MK-1 T3, dial 0. Pressed Regen Surge twice (Secondary
active). Bodies reached the Hatch (272/300 on the chained Wave-2 clear).

Log: `Logs/GatekeeperBastion_2026-08-07_23-44-23.log`.

- Activations logged: `MK-1 Sidearm — Regen Surge (dial 0, mag 4)` ×2.
- Reported gatekeeper-seconds: **0.9** (regen rounds 1.8) — under the ≥2 bar.
- No BR-12 / no pin in the session.
- Dev overlay opened after the clear (`[Dev] overlay open`) — session tainted.
- Deep-start wiped Wave-1 ability credit when Wave 2 started (fixed after this session).

**Criterion 5 not met from this Run.** Need BR-12 shove and MR-35 pin Waves at T0–T1, and
MK-1 used on cooldown against a door that is actually under pressure, without Dev.

## 2026-08-08 — two losses either side of a full shop spend

Log: `Logs/GatekeeperBastion_2026-08-08_17-00-15.log`. **Both Runs are clean** — the `[Dev]`
lines land at 17:29:25, after the second Run ended at 17:28:43.

**Run 1**, MR-35 T4, no Repair Rig, started at Wave 1. Wave 1 cleared at **192/300** with 6
Hatch arrivals and repair refused (`equipment slot not purchased`). Lost on Wave 2 after 79 s.

**Between Runs**, 5,100 Salvage spent: Repair Rig 300, MR-35 T5→T7 (1,200 + 1,400 + 1,600),
Vitality L1–L2 (600).

**Run 2**, MR-35 T7 with the rig, deep start at Wave 5 — all 210 bodies chained. Lost on
Wave 5 after 154 s, 24 Hatch arrivals.

**Numbers.** Mean live Gatekeepers (`M29`'s measure, computed offline): Run 1 Wave 1 **5.00**,
Wave 2 **11.30** at peak 17. Run 2 across the whole chain **9.88**, peak **33**, mean lifetime
7.18 s. Ability credit across the five chained Waves: **0 / 0.6 / 0 / 0** — and the four zeros
are Waves where nothing activated at all. Player damage in Run 1: **14 hits of 6, 100 HP → 16
in 7.4 seconds**, all inside Wave 2.

**Assessment `[unconfirmed]` — three claims, each falsifiable, each tagged with the tier
it would move:**

1. **The deep-start conditional does not hold at Wave 5, at the shop ceiling.** *(#10 —
   the chain rule and the inter-Wave gap. Reversible in one constant.)* M14's rule is
   that chaining should be *easier* if and only if the player has bought enough. This Run is
   the strongest test yet — T7 is the top of the shop — and it lost. M14's own *Watch* section
   predicted this in advance and said to record the number rather than weaken Waves 4–5.
2. **Vitality is not a lever against this damage shape.** *(#12 to observe, **#5 to act on** —
   a swing-concurrency cap is a new rule, so that milestone needs your say-so, not this entry.)* L1+L2 cost 600 and added 10 HP
   against a burst of 14 hits in 7.4 s — 1.7 more hits, about 0.9 s. The lever is how many
   Gatekeepers can swing at once, not the size of the pool.
3. **The Primary's ability contributed nothing, and could not have.** *(Not a tier question —
   it is a defect, diagnosed from code rather than from feel, and needs no verdict.)* The four zeros are not
   a tuning result: `RegisterPinHitIfNeeded` fires per damaged body in a pierce line and
   `PinBuildState.RegisterHit` zeroes the count on target change, so the build never exceeds
   1 against a threshold of 3. **Fixed in M31 Path 1** (pin registers on the first damaged
   body only); playthrough criterion still open on that file.

**Zo's verdict:** _pending_ — concur, update, or replay. **Claims 1 and 3 are cleared to act
on now** under the table above; claim 2's remedy is a #5 rule change and waits for you.

---

## M26 — SEC-D play question (open)

After Path 1: buy `SEC-D`, embark with MR-35, let a Hunter reach you and swap. Then try the
same gun on a Default across the room.

**Does carrying SEC-D instead of the MK-1 change how you fight, or only what you press when
the Primary is empty?** Record kill-share from the session log (`with Primary` / `with
Secondary`). Ability + ring still need Zo before it is a full build.

---

## 2026-08-09 — Wave-3 deep start at MR-35 T9

Log: `Logs/GatekeeperBastion_2026-08-09_15-21-59.log`. Clean, no `[Dev]` lines. Waves 1, 2
and 3 chained from a Wave-3 start, repair rig equipped, 3538 banked.

**Zo's report: started at Wave 3 and it still felt easy with M35. The pin seemed not to work,
or there was no way to tell it was active, and enemies died quickly.**

**Numbers**, from `Tools/wave-report/run.sh` (the authority; an earlier draft of this entry
quoted 4.01 and 3.98 from an ad-hoc parse and was wrong). 90 bodies in 89.2 s. Mean live
**3.78**, peak 10, mean lifetime **3.74 s**.
Hatch **300/300**. **Zero Hatch arrivals, zero `hatch under attack` lines, zero player
damage.** Ability credit `0 / 0 / 0`, and no `[Ability]` activation line of any kind.

**Assessment `[unconfirmed]` — four claims:**

1. **"Easy" is now measured, not felt.** *(#12 to observe.)* Mean lifetime 3.74 s against
   4.85 s for Wave 3 alone at T4 on 2026-08-07, and mean live 3.78 against 5.97 for the same
   Wave. **Bodies die faster at T9 than at T4 and the room holds fewer of them, on a chained
   start that should be harder.** Nothing reached the door across 90 bodies. This is not a
   close call.
2. **The Pin Shot still has not fired, in any session, ever.** *(Defect, not tuning.)* M31
   shipped the fix that registers a pin hit on the first damaged body only, and the credit is
   still `0 / 0 / 0` with no activation line. Either the fix did not land, or the pin has a
   second failure behind the first. **This is now the third session in a row and it is the
   first thing to check.** `verification.md`, item 1.
3. **"Could not tell when it was active" is a separate, real gap.** *(#6, information flows.)*
   Even a firing pin has no visible state: a pinned Gatekeeper looks like an unpinned one, and
   `PlayerAbility.TryActivate` returns silently on two of its three failure paths. **A working
   pin would still read as broken.** Both halves are HUD feedback still owed after M36
   (always-on Hatch only — pin state was out of scope there).
4. **MR-35 T9 needs explaining.** *(Question, not a claim.)* `WeaponTier.MaxTier` is 7 and the
   cost curve is defined for T0 through T6. A T9 either means the cap moved deliberately or it
   means the wheel's derived tier is no longer bounded by it. Worth one look before any
   tuning conclusion rests on it.

**Zo's verdict:** _pending_ — concur, update, or replay. Claim 1 is cleared to act on; claim 2
is a defect and needs no verdict; claim 4 is a question for the code, not for play.

### On the metric Zo proposed

**Measure Gatekeeper distance moved before dying.** Taken as **M37**, with one change and then
a correction. Raw path length needs per-agent accumulation, which `Decisions.md` forbids. The
first draft replaced it with a normalised `penetration` fraction; **an adversarial review
killed that** — its lower clamp fires in exactly the three cases worth measuring (a Hunter
dragged past its spawn, a body pulled by M30's decoy, any knockback), all reading 0 and
indistinguishable from dying on the spot. M37 now logs **two raw distances**, `hatchDist` and
`spawnDist`, with no formula in the game at all.

Today's session is exactly why it is worth having. Zero arrivals is the same reading whether
those 90 bodies died at 40 m or at 5 m, and those are different games.

### M37 first live reading — 2026-08-11

`Logs/GatekeeperBastion_2026-08-11_20-02-31.log`. Deep start wave 5 (chained w1..5), MR-35 T10
+ SEC-D, **lost** on Hatch breach after 157 s. Untainted (no Dev).

| | |
|---|---|
| Kill lines | **185 / 185** carry `hatchDist=` and `spawnDist=` (criteria 1, 3) |
| `spawnDist=-1` | **0** (criterion 4 unset path not exercised) |
| mean live | **9.20** (peak 28, mean lifetime 6.79 s) — `wave-report` |

Per-kind median `hatchDist` / share >25 m (`wave-report`):

| kind | n | medHatch | farShare |
|---|---|---|---|
| Gatekeeper | 145 | **25.9** | 0.55 |
| Hunter | 29 | **20.1** | 0.24 |
| Ram | 11 | **19.6** | 0.18 |

Within the same Run, first-half kill median hatchDist **29.1** (farShare 0.72) vs second-half
**17.0** (farShare 0.25) — the instrument moves as the fight collapses toward the Hatch.
**Criterion 8 closed by Zo override 2026-08-11** (contrasting Hatch-bay / far-wall pair not
run; within-run split accepted as the difference proof).

## 2026-08-12 — M36 Hatch HUD

**Played:** Bastion fight with top-centre `Hud Hatch` always on.

**Zo's verdict:** Hatch HUD addition is great for this point in time. Criteria 8–11 closed;
M36 shipped.

## 2026-08-13 — T1 five Waves from one Interact

Played: `Bastion.unity`, T1, one Interact, Waves 1–5, no F1–F9.

Log: `Logs/GatekeeperBastion_2026-08-13_14-46-59.log`. Untainted. Interact 14:47:17; T1
`started wave` 1 then 2/3/4/5 at 8.000 s after each prior clear; `wave 5/5 cleared` and
T1 complete 14:53:33; `BASTION STRIPPED`. Hatch **300/300** every clear. Kills `+25
unbanked`; banked stayed 12208. `wave-report` median `hatchDist` 44.8–67.4 m, farShare
**1.00** every kind/Wave (parser failed on `Spawn_MidWest (1)` parens; medians still
printed).

**Verdict:** Zo concurred 2026-08-13. Entry closed.

## 2026-08-13 — M53 Hatch-bank

Played: `Bastion.unity`, T1 five Waves then Hatch-bank at the Hatch. No F1–F9 during the Run.

Log: `Logs/GatekeeperBastion_2026-08-13_18-23-55_p92464.log`. Run clean (`[Dev]` StartCoopHost is after the end screen). T1 complete 18:31:10, hatch 300/300, salvage 0. Hatch-bank 18:31:53 wave 5 unbanked=5250 slots=2, 78 gatekeepers, kills `+0`. Timer 45.002 s; salvage 7413, hatch 292/300. Then `BASTION STRIPPED` (recut after this log: dump stays Prep).

**Verdict:** Zo 2026-08-13 — hatch wave worked pretty well; the run ending after it was the remaining miss (fixed in sim, not replayed). Entry closed.

## 2026-08-16 — starting weapon / first session `[unconfirmed]`

Played: solo, starting loadout (no session log attached). Comparative read: other wave-defense FPS starting weapons one-shot fodder for the first few Waves.

**Zo's verdict** (this session, no log): the game is tuned too hard as single player. The first session should let the Boarder handle **multiple Waves**, then get **overrun defending the Hatch** and lose **some** Salvage. Demo file 1 should teach that: multiple Waves, the intensity multiplier (risk/reward), and Hatch-bank. Slow the Hatch-bank transfer rate.

**Entry closed** on verdict. Claims 1–2 remain `[unconfirmed]` until a starting-MK-1 log exists; they are specced as levers in `Docs/Demo/README.md`. Claim 3 is the beat-1 recut.

1. **MK-1 does not one-shot a Wave-1 Default.** *(#12, `WeaponCatalog` MK-1 damage 14 vs `BaseEnemyHp` 30 — three taps at mul 1.0.)* Comparative one-shot is the demand. Do not lower Default HP (kinds stay distinct; Thief 120 unchanged).
2. **Drip 20/s empties a small pile before overrun reads.** *(#12, `HatchBank.DripPerSecond`.)* 20×45=900; Wave-1 solo pile is 450, gone in 22.5 s. Slowing the rate leaves Unbanked to wipe/attrit when the Hatch falls.
3. **First demo cycle was one Wave, juice withheld, convert-then-fight.** *(#6/#5, demo sequencer.)* That cannot teach multiplier or “lose some Salvage.” Overturned in `Docs/Demo/` (2026-08-16).

A stacked-Primary session on 2026-08-09 (MR-35 T9, Wave 3 “easy”, Hatch 300/300) does not contradict this. That was not the starting weapon.

## 2026-08-25 — first spine run: the overwhelm point, and the loadout it was met with

Played: solo, `Bastion_Coop.unity` on the new spine, returning save in realm "ok". Run clean —
no `[Dev]` lines in the file.

Log: `Logs/GatekeepersProtocol_2026-08-25_19-28-58_p63308.log`. Seed 200441750, ALPHA-7,
`result=lost cause=player wave=2 duration=108 respawns=2 banked=227`.

### What the Boarder had

Everything, and it was not much:

| | |
|---|---|
| Primary | **none** |
| Secondary | **MK-1 Sidearm T2** — 30 dmg, 3.0 shots/s, clip 9 (T2), reload 1.4 s, range 45 m, **falloff from 12 m** |
| Repair rig | none |
| Salvage | **227 banked** |
| Ante | 0, never raised |
| Abilities | Regen Surge available; `wave 1 gatekeeper-seconds removed: 0` — never used |
| Record | 2 runs lost, 0 cleared |

The Ship said `exit standing set — no buyable upgrades remain` before the embark. **This is a
soft-lock, not a choice.** The cheapest Primary is 900 (BR-12) and the rack that sells it is
gated behind a zone-2 bank the player has never reached; with 227 banked there is nothing on
any shelf. The only kit that could clear the composite is behind the composite.

### The overwhelm point

**Wave 2, 19:30:13.4 — group 3 lands and the live count hits 18.** It never came back down:
22 → 25, and for the remaining 61 s of the Run it never dropped below 21. Everything after
that timestamp is the Run being over slowly.

The arithmetic that produced it, per wave:

| | wave 1 | wave 2 |
|---|---|---|
| bodies | 18 | 30 |
| spawn window | 21.0 s | 28.0 s |
| **spawn rate** | **0.86 /s** | **1.07 /s** |
| **realised kill rate** | **0.66 /s** | 1.09 /s, but only across a 10.1 s burst |
| peak live | 8 | **25** |
| kills at (median) | 65.3 m from Hatch, 13.9 m from the closet | 12.3 m from Hatch, 48.9 m from the closet |
| died | t+23.8 s, 6 still alive | t+29.2 s and t+68.7 s, **25 still alive** both times |

**Wave 1 is where it was lost.** Kill rate 0.66/s against a spawn rate of 0.86/s is a deficit
of 0.20 bodies per second sustained for the whole wave — the field cannot empty, by
construction. Waves 1 and 2 are one composite with no Prep between them (wave 2 drew 0.2 s
after wave 1 ended), so the deficit carries straight into a wave that spawns 24% faster again.

**The weapon was not the ceiling.** MK-1 T2 at point blank is 9 rounds / (9/3 + 1.4 s) =
**2.05 kills/s**, and one round kills a 30 HP Default outright inside 12 m. The player
realised **32%** of that in wave 1 and 53% in the wave-2 burst. What ate the rest is time
not shooting: acquisition across the lane, walking, and reloads landing while a group breached.

**Incoming damage tells the same story from the other side.** Time from first hit to death:
16.0 s in wave 1, then **7.2 s** and **8.0 s** in wave 2 — every death was 17 hits of 6, so
the halving is purely how many bodies were in contact at once. At 25 live the player is taking
~14 dps and cannot buy the 1.4 s the reload costs.

**Respawns fed it.** 150, then 300, then `no respawn — not enough unbanked`. Each respawn put
the Boarder back into a 25-body field with the same weapon and no ground gained.

### The two positions the player tried

Both are visible in the kill distances, and both failed for different reasons:

* **Wave 1 — pushed to the closet** (65 m up the lane, killing at 14 m from the spawn). Kills
  land at full damage, but there is no backstop: groups breach behind the push.
* **Wave 2 — held the Hatch** (killing at 12 m, bodies having walked 49 m). This is the corridor
  working as designed — one lane, 0° of spread — and it still lost, because 1.07 bodies/s
  arriving beats 0.66 killed/s regardless of how good the firing angle is.

That is the finding: **the spine fixed the geometry problem and exposed an arithmetic one.**
The old complaint ("I have to run in circles, I can't hold ground") is gone — he held ground
at the Hatch for wave 2. What is left is that holding ground is not survivable on the kit the
demo hands you.

### Assessment `[unconfirmed]` — pending Zo's verdict

Three claims, each falsifiable from the next log:

1. **Zone 1's composite is unwinnable on MK-1 T2 with no Primary.** *(#12/#10 — wave counts and
   spawn cadence.)* 48 bodies continuous against a realised 0.66 kills/s. Falsified by any clean
   log that clears zone 1 on the starting kit. The levers are the composite (2 waves back to
   back may be one too many at the very start), the spawn cadence, and the body count.
2. **The economy is soft-locked at this point.** *(#6 — a gate, not a number.)* 227 banked,
   nothing buyable, and the rack that would sell a Primary is behind the wave that needs one.
   Falsified by showing a purchase reachable from this state.
3. **The respawn ladder accelerates the loss rather than answering it.** *(#12.)* Both post-
   respawn deaths took ~7.5 s. Falsified by a log where a respawn recovers a wave.

**Not yet acted on.** These are the level/economy dials, and the session's three shipped changes
(the Hatch as the sole start line, enemy speeds down, terminals banking) were the ones Zo named.

