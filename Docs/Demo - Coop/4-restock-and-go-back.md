# 4 — Restock and go back

<!--
Purpose: The return trip. Back aboard, spend the crew pot together, see what your
teammate is carrying, embark again and get further. This beat repeats; it is the
demo's loop, not a one-off.
Status: demo spec
Authority: implementable
Update trigger: when the crew pot rules change, when the presence adds land, when
persistence stops being deferred
Last updated: 2026-08-20
Depends: voice-coop.md, 3-the-bank-and-what-it-costs.md
-->

## Why this beat exists

**This is the loop, and it runs more than once.** Progress through the bastion is
incremental (Zo, 2026-08-20): hold the doors, come home, upgrade and restock, go back
in and get further. The boss is reached by repeating this, not by a single deep run.

**Persistence is deferred** (Zo, 2026-08-20). Runs are fresh and there is one crew pot.
Nothing carries between sessions and no seat owns a wallet. That removes every question
about whose salvage is whose, whose save is authoritative, and what a guest takes home.
Those questions come back later; they are not this demo's.

What is left is the part that matters for co-op: two people spending one balance, and
being able to see what the other one bought.

## Functional storyboard

Continues from beat 3. Standing arrives as **Restock at the ship**.

### 0 — Back aboard

| | |
|---|---|
| **Goal** | Orient. The pot is what you have; the run is not over. |
| **Player** | Walk to the ARMORY. |
| **Game** | — (`demo.coop.first_bank_down` already carried the instruction) |
| **Standing** | Restock at the ship |
| **Prompt** | Live station strings. |
| **Log** | existing `[Ship] aboard the ship — <n> banked …`, reading the crew pot |
| **Have** | Dock path, station prompts. |
| **Build** | Every salvage readout on the Ship reads the crew pot. There is no per-seat balance to show. |

### 1 — Spend it together

| | |
|---|---|
| **Goal** | Decide, out loud, what the party buys with what it has. Anyone may buy anything they can afford. |
| **Player** | Either seat buys weapons or wheel upgrades. The pot falls for both. |
| **Game** | — (the panel is the reader) |
| **Standing** | Restock at the ship |
| **Prompt / panel** | Crew pot balance in the header, updating live as either seat spends. |
| **Log** | `[Shop] slot=<n> bought <thing> cost=<n> pot=<n>` (from beat 0) |
| **Have** | ARMORY panel and wheel; the crew pot from beat 0. |
| **Build** | Nothing new. Beat 0 built the pot; this step is it being used a second time with more at stake. Every row stays on sale and nothing unlocks over the demo. The only refusal is insufficient balance. |

### 2 — See what they brought

| | |
|---|---|
| **Goal** | Know what your teammate is carrying before you are relying on it. |
| **Player** | Look at them. |
| **Game** | `demo.coop.see_them` · Subtitle · Local · Game. **\<name\> is carrying a \<weapon\>.** |
| **Standing** | Restock at the ship |
| **Prompt** | — |
| **Log** | — |
| **Have** | `NetPlayerAvatar` replicates position, **yaw only**, HP, slot, protocol, flags. |
| **Build** | **The four presence adds**, which are the whole of M-C: replicated camera **pitch**, replicated **weapon id**, a **fire event** for remote muzzle and tracer, and a **name** label. Not replicated and not this beat's: reload state, crouch, sprint, ADS, voice, ping wheel. |

### 3 — Go back in

| | |
|---|---|
| **Goal** | Get further than last time. |
| **Player** | READY at NAV, lead commits, countdown, embark. |
| **Game** | `demo.coop.re_embark` · Lock (Ship) · Party · Game. **Same doors. Further in this time.** |
| **Standing** | Get past where you stopped |
| **Log** | existing `[Run] run begins …` |
| **Have** | Party embark (`M72`). Post-loss the zone's deepest terminal retriggers the zone composite by default, with a partial start electable, exactly as the solo demo rules it. No NAV skip. |
| **Build** | Nothing new for the embark. The open-door count is recomputed from the seat count, which only changes if someone joined or left. |

## Acceptance shape

Player action: after a lost bank, both players spend the crew pot at the ARMORY, one of
them can name what the other is carrying without being told, and they re-embark and
reach further into the bastion than the previous attempt. Reads back: the pot header
falling for both seats as either spends, and the run-begin log on the second embark.

## Verification

The net fixture can hold: the pot is one balance, both seats' buys decrement it, an
overdraw is refused. Pitch, weapon id, the fire event and the name label are eyes-on
across two editor instances; the fixture cannot see them.

**Playthrough path**: lose the bank in beat 3, dock, open the ARMORY on both seats, buy
from one and watch the other's header fall, look at your teammate and read the weapon,
READY and embark.

## Gaps

- **Persistence is deferred, not solved.** When it comes back, the crew pot is the thing
  that has to be re-decided, and this beat is where that decision lands.
- How many times this loop should repeat before the boss is reachable is **not specced**.
  It is a tune against the crew pot's rate, and it needs a played run.
- `demo.coop.see_them` may read as hand-holding once the weapon is actually visible in
  their hands. Cheapest fix is to cut the line and let the model do the work.
