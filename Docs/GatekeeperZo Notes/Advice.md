# Advice

<!--
Purpose: Zo's product priorities and the agent-review debts that still sit under them —
not a roadmap, not a milestone queue. Zo's list outranks the send checklist when they
conflict. Stamp or strike a row when you act on it.
Status: working
Update trigger: when Zo restates priorities, after a friends send, or after a review sweep
Last updated: 2026-08-30 (Zo priorities: hands, Default rename/tether, Explore Kit,
Bastion tune, UI polish, SFX)
-->

## Zo's priorities

These are the product gaps Zo named 2026-08-30. They are what the game still *feels*
like it is missing. Agent send/verify rows below do not displace them.

| # | Priority | What "done" looks like | Home / note |
|---|---|---|---|
| 1 | **Hands that hold the gun** | First-person arms + gun in sockets; fire/reload/move read as a body, not a floating weapon | `M82` closed as **no-arms alpha scoping** — Animator contract (Fire / Reload / Speed) is the seam; next pass is an FPS arms pack under that contract (`M82` Why it matters). Not a viewmodel-bob retune. |
| 2 | **Name Default better, and refine it** | **DEPOT** ships: Dump (`G` fire ×2 + MoveSpeed ×0.35) and Feed (`H` toggle clip regen ~28% more continuous fire). | `Protocol-DEPOT.md`; `BoarderProtocolId.Depot`. Gatekeeper kind **Default** is still a separate rename if it reads wrong. |
| 3 | **Refine aesthetics in Explore – Kit** | The walkable look target in `Assets/Scenes/Explore - Kit.unity` is the bar the Ship/Bastion must match | Feeds `M104` (Ship look). Kit scene is the gallery; do not dress `Ship - CoOp` from memory while Kit still looks unfinished. |
| 4 | **Tune Bastion and refine** | Zone-1 / first Boarding reads fair and worth moving for; soft-lock and Hatch-camping answered by play | Open feel debts: soft-lock (08-25 claim 2), Hatch camping vs recall pads, swing clumps as damage structure (`playthrough.md`). One lever per change. |
| 5 | **UI past amateur polish** | Corners, CREW catalog, station chrome read intentional — not Shift defaults with wrong insets | `M108` is the chrome/inset pass; broader ability HUD grammar stays in `Brainstorms/hud-and-ability-ui.md`. Friends see this before they feel the gun. |
| 6 | **Much higher quality SFX** | Fire / hit / foot / Hatch / UI read as a bought session, not the five wavs bound in `M69` | `M69` closed with **no store pack**. This priority reopens the buy gate: pick a pack (or commission), bind, kill the placeholder set. Do not retune volumes and call it done. |

### Capture before it evaporates

**DEPOT Feed is in tree** (toggle regen on own clip). The earlier "teammates fire refill your
clip" tether idea is **not** this — do not reintroduce party-fed ammo without a new kill
criterion. Barrage/Rush player names are retired; code ids remain.

## Decisions that still block a clean send

Keep these honest even while the feel list above is the north star.

| Decide | Why it blocks | Recommendation |
|---|---|---|
| Respawn purse: **realm bank** or **Unbanked** — one rule, strike the other | Friends copy promises bank; Decisions amend still names Unbanked | Keep **realm bank**; amend Decisions + Open-Rulings |
| 0.34.2 player FX still dark? | Same-day logs had tracers/particles disabled; pin claimed in Changelog | Confirm on a **fresh** extract; re-zip if still dark |
| M86 downed anim: finish or park | Only Active slot (1/3); clip never through Editor | **Park** until after hands / UI / SFX priority work, or source the clip — do not leave half-open |

## Under the priorities — send and verify debts

Agent review rows. Do them when they unblock a priority or a friends evening; they do not outrank the table above.

| Do this | Why it still matters | Ties to |
|---|---|---|
| Friends five-Wave dry-run on an extracted zip (two machines, hands off) | Only launch criterion in `gtm.md` / Roadmap | Proves SFX/UI/hands gaps under real guest eyes |
| Release-Gate human reds 13–25 on that send | Machine path closed; human path never claimed | Same |
| Stamp Concur / Update / Replay on 08-25, 08-29, 08-30 playthroughs | Pending verdicts block Bastion-tune claims | Priority 4 |
| Confirm FX + cursor capture on fresh player extract | Silent guns / stuck cursor on first guest launch | Priorities 1, 6 |
| Play HP-4 Lost endings | End screen sentence after M98 respawn recut | Friends send |
| Ship or skip **M108** | Amateur HUD is Zo priority 5 with a filed milestone | Priority 5 |
| One finished FORGE Boarding — any machine `damage absorbed`? | 08-29/08-30 logged 0 | Bastion / protocol refine |
| Soft-lock: banked Salvage, nothing buyable, Primary behind the wave | 08-25 claim 2 unacted | Priority 4 |
| Restart-after-death rematch state | Gate #24; top `playthrough.md` observation | Friends send |
| Recall pads vs Hatch camping — one Boarding that asks the question | Oldest feel note; pads shipped unanswered | Priority 4 |

## Leave alone

- **Ante** — parked through friends alpha (`Decisions.md`).
- **Story break ending** / mid-Boarding remix — unauthored (`Open-Rulings.md`).
- Fiction carriers — Compendium, existing state UI, ≤1 Hatch line.
- Flipping `Ante.Enabled` or widening EMP radius before orbit is answered.

## Cheap doc drift

- Strike Unbanked / ante-up teaching in `Docs/README.md` ASCII loop.
- Restate respawn-purse amend in `Decisions.md` to match code + `gtm.md`.
- DEPOT rename landed 2026-08-31 — strike this row when the Gatekeeper kind is renamed or parked.
