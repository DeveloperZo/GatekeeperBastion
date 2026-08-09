Repairing the Hatch should cost resources. 

The upgrades a player can make need to be aligned with getting them to progress further without dying on the same Wave more than 3 times.

Observation: Right now there's no incentive to move away from the Hatch

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
clean Runs if the taint arrives after they end. Say which.

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
   pin would still read as broken.** Both halves belong with the HUD work, M36.
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
