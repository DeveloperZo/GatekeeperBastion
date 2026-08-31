# 4 — Restock and go back

<!--
Purpose: The return trip. Back aboard, spend the realm pot, see what your
teammate is carrying, embark again, take zone 2 (T5). This beat repeats.
Status: working
Authority: implementable against 0.34.2
Update trigger: when shop routing changes, when presence adds land, when
PrimaryRowsOpen moves
Last updated: 2026-08-30 (persistence shipped; fire RPC landed; T5 is zone 2)
Depends: voice-coop.md, 3-the-bank-and-what-it-costs.md, ../Play Mechanics/Realms.md
-->

## Why this beat exists

**This is the loop, and it runs more than once.** Hold the Hatch, shut off a
terminal, come home, restock, go back further. Master Control is reached by
repeating this.

**Persistence shipped (`M97`).** The realm keeps the pot, the doors, the
deepest Wave. The Boarder keeps the kit. Nothing here is a fresh-save wipe.
What does not persist is Hatch HP (starts whole every Boarding) and the
Boarding's respawn counter.

What is left for co-op: two people spending one pot, and being able to see
what the other bought.

## Functional storyboard

Continues from beat 3. Standing arrives as **Restock at the ship**.

### 0 — Back aboard

| | |
|---|---|
| **Goal** | Orient. The pot is what you have; the realm is not over. |
| **Player** | RETURN TO SHIP (lead loads **Ship - CoOp** for everybody; a guest's press is a request). Walk to the ARMORY. |
| **Game** | — (`demo.coop.first_bank_down` already carried the instruction, on a Hatch fall) |
| **Standing** | Restock at the ship |
| **Prompt** | Live station strings. |
| **Log** | existing `[Ship] aboard the ship — <n> banked …` |
| **Have** | Dock path. Guest RETURN is `RequestPartyReturnToShip`. Every salvage readout on the Ship should read the realm being played (`RealmRecordSource`); ARMORY still spends local Meta (beat 0 Build). |
| **Build** | Guest shop commands hitting the host pot. |

### 1 — Spend it together

| | |
|---|---|
| **Goal** | Decide what the party buys. Anyone may buy anything the rack will sell and the pot covers. |
| **Player** | Either seat buys weapons or wheel upgrades. |
| **Game** | — (the panel is the reader) |
| **Standing** | Restock at the ship |
| **Prompt / panel** | Balance in the header. |
| **Log** | existing shop lines |
| **Have** | ARMORY + wheel. Primaries appear once `DeepestWaveEntered >= 2` (zone 1's last Wave) or demo off. CREW: Default is 0-salvage; VEIL and FORGE are claimable. FABRICATION: Hatch Armor, Rig, Decoy — equipment slot on first Convert (`HatchConversions >= 1`). |
| **Build** | `PrimaryRowsOpen` inert for friends (still keyed on `DemoActive`). |

### 2 — See what they brought

| | |
|---|---|
| **Goal** | Know what your teammate is carrying before you rely on it. |
| **Player** | Look at them. |
| **Game** | `demo.coop.see_them` · Subtitle · Local · Game. **\<name\> is carrying a \<weapon\>.** |
| **Standing** | Restock at the ship |
| **Prompt** | — |
| **Log** | — |
| **Have** | Position + yaw, HP, slot, protocol, flags. **Fire and Reload** cosmetic ClientRpcs (`NetPlayerAvatar.Anim.cs`). Remote animator `Speed` from transform delta, plus `Dead`. |
| **Build** | Replicated camera **pitch**, **weapon id**, **name** label. Not this beat: crouch, sprint, ADS, voice, ping wheel. |

### 3 — Go back in

| | |
|---|---|
| **Goal** | Zone 2. |
| **Player** | READY, lead commits, countdown, embark. `T` at the Hatch starts T5's composite (**3** Waves). Shut off at **T5**. Recall pad beside T5. |
| **Game** | `demo.coop.re_embark` · Lock (Ship) · Party · Game. **Same doors. Further in this time.** |
| **Standing** | Shut off T5 |
| **Log** | existing `[Run] run begins …` |
| **Have** | Party embark. Zone 2 `{T5}` → **D3** (control door). Deepest-terminal retrigger with partial start electable, same as solo. FOLLOW admits a late seat in Prep and opens its door. |
| **Build** | Nothing new for the embark. Open-door count recomputes from the seat roster at the break. |

## Acceptance shape

Player action: after a dock or a loss, both players spend at the ARMORY, they
re-embark, and they shut off T5. Reads back: the pot moving, the second
run-begin log, D3 open.

Naming the other's weapon without being told waits on the weapon-id replicate.

## Verification

The net fixture can hold: banked salvage persists across RETURN, D3 keys on
the T5 shutoff. Pitch, weapon id and the name label are eyes-on across two
editor instances.

## Gaps

- Guest shop vs host pot is still the beat-0 hole.
- How many times this loop should repeat before Master Control is reachable is
  **not specced**. Kills bank on landing, so even a lost Boarding funds a buy.
  Needs a played evening.
- `demo.coop.see_them` may read as hand-holding once the weapon is visible in
  their hands. Cheapest fix is to cut the line.
