# Pitch

<!--
Purpose: The store sentences Zo locked, and the truth value of each.
Status: working
Update trigger: when Zo restamps the Copy block, or when a cited
milestone closes and a row's status changes.
Last updated: 2026-08-30 (Copy restamped: Hold the Hatch. Salvage banks as they fall.
0.34.2 Accuracy recut for parked Ante, Hatch-start, realm-bank respawn, one-deck hull)
-->

## Copy

Zo, 2026-08-30. Do not paraphrase in this file; restamp the block.

Break into the Bastion alone or with friends. Defeat the Gatekeepers. Use their own Protocols against them. Hold the Hatch. Salvage banks as they fall.

## Accuracy

| Sentence | Status | Why |
|---|---|---|
| Break into the Bastion alone or with friends. | **alone true; friends lobby true; five-Wave dry-run open** | Solo boarding: `../Play Mechanics/The-Game.md`, `../Play Mechanics/A-Boarding.md`. Shared Ship + party EMBARK closed (`M72`, host log `13-24-37` / Relay `KPRMPR`). A realm a Boarder travels between (`M96`), the realm's record on the hull (`M97`), a late arrival who follows at a break (`M98`), a realm a friend can open while its owner is offline (`M100`), and a 28-room co-op bastion (`M95`) all sit under this sentence. `M100`'s two-machine criteria are still owed. One destination: **ALPHA-7** (`SceneFlow.BastionName`). The loop a stranger meets in **0.34.2** (`ProductVersion.Display`): a Wave **begins at the Hatch and ends at the zone terminal** (`../Play Mechanics/Getting-Started.md`; `HatchOffersWaveStart`). The two-deck hull `M101` shipped is superseded — the interior is one deck again (Zo, 2026-08-26, `../Play Mechanics/The-Ship.md`). |
| Defeat the Gatekeepers. | **true** | A field Wave ends when the Boarder shuts the zone's terminal, not when the roster is empty — leftover bodies trickle and pay nothing (`../Play Mechanics/A-Boarding.md`). The Boarding is **Escaped** only after Master Control (`M54`), not field Wave 5. Four kinds ship since `M63-3` added the Cloaker (2026-08-24). |
| Use their own Protocols against them. | **false until** a Gatekeeper wears a Protocol modifier, diluted | Shared pool (`../Brainstorms/boarder-protocols.md`): Boarder form is concentrated; Gatekeeper form is the same modifier, watered down, authored, never rolled. Default, VEIL and FORGE are all **your** sheets (`../Play Mechanics/Protocols.md`). FORGE's charged turret and Outrider ship as of the 2026-08-29 Play page; they are still yours. AEGIS and LEDGER are Coming Soon. Albion deeds unlock the next sheet; they do not make Gatekeepers wear the pool. Probe: one modifier-wearing Gatekeeper a player can name after a Boarding. |
| Hold the Hatch. | **true** | Zero Hatch is Lost; nothing buys the door (`../Play Mechanics/The-Hatch.md`). Field Waves can nick it at `HatchBank.FieldWaveDamageScale = 0.15`. Convert is an opt-in Hatch bet on a Withdraw pile, not the defense sentence. |
| Salvage banks as they fall. | **true** | Kills credit the realm's bank the moment the body falls (`RunSim.AwardKill` → `BankKill`; `../Play Mechanics/Salvage.md`). No ending takes banked Salvage back. Convert moves a Withdraw pile; it is not the payroll. Struck from the demo 2026-08-25 (`RunSim.Dock.cs`). |
| **Death is no longer simply the end.** | not a locked sentence — a change under Hold the Hatch / Salvage banks | A dead seat buys back from the **realm's bank** (`Respawns.Refuse` purse; `RunSim.TryRespawn` → `SpendBanked`). Price is `Respawns.CostFor(taken)`: `150 × min(taken + 1, 3)`. A Boarding may buy **2** respawns (`Respawns.MaxRespawns`); the third death is refused `out of respawns` whatever the bank holds. Bleed-out is **10 s** (`Respawns.BleedOutSeconds`). The file header on `Respawns.cs` still says Unbanked; `Refuse` and `../Play Mechanics/Going-Down.md` are the rule. Copy that says a single death ends everything is wrong; copy that says you can always buy back is also wrong. |

## May print

Store sentences use the player's words, never the builder's — Boarding, Hatch, Convert
(`../_meta/_definitions/language.md`, rule 3 and trap 8). Ante / ante up are legal words
and are **parked** in this build (`Ante.Enabled = false`, 2026-08-30). They do not cross
into this block until the flag flips.

Alone; defeat Gatekeepers; hold the Hatch; Salvage banks as they fall; a Wave starts at the Hatch and
ends at the terminal. The Boarder, the Ship (one deck, three hatches), the Bastion they
hold, Gatekeepers (Default, Hunter, Thief, Cloaker), banked Salvage surviving a loss, and
a Boarding a friend can join between Waves.

Do not print the five sentences as one block until the friends half and the Protocols row
are true. The Hatch sentence and the Salvage sentence may print on their own.

## May not print yet

- **Ante, ante up, or "you author the swarm"** as a live dial. `Ante.Enabled = false`; Waves run at intensity 0 (`IntensityJuice.cs`). The Field Manual still generates the Ante table — that is the generator, not the build a friend launches.
- AEGIS or LEDGER as if they ship. VEIL and FORGE do (`M94`), including FORGE's turret and Outrider, and may be named as **yours**.
- Plural Bastions / a star map (one destination, ALPHA-7).
- A five-Wave friends evening a stranger finishes without you (`Roadmap.md`).
- “Their” Protocols as a CREW or HUD sentence.
- A shared realm as proven — `M100` is built and has never run on two machines.
- Convert as how Salvage is kept, or any number for how many Converts a Boarding allows (there is no cap, and Convert is not the payroll).
- The retired sentence **Fight off waves to defend your Salvage.** That was Convert-as-payroll.
- The two-deck hull. `M101` is superseded; `../Play Mechanics/The-Ship.md` is one deck (2026-08-26).
- Unbanked-until-Convert, or a respawn paid from the carried pile.
- The faucet-loop graybox (auto-chain, Hatch-hunt, endure income, the draft). Dev-gated; does not compile into a player build (`../Play Mechanics/The-Faucet-Loop.md`). The Hatch-start / terminal-end **shape** is the shipped loop and may be named.

## Do not cite for copy

| Stale | Prefer |
|---|---|
| `../Play/*.md` (folder renamed) | `../Play Mechanics/` |
| `../Play Mechanics/Field-Manual.md` Ante table and `+/-` bindings, as if they ship | `Ante.Enabled` in `IntensityJuice.cs`. The manual is generated and still prints the parked dial |
| `language.md` `WavesPerTriggerByZone` `{ 1, 3 }` | `DemoZones.WavesPerTriggerByZone` is `{ 2, 3 }` — zone 1 runs **2** Waves as one composite. Field Manual: "zone 1 = 2 waves, the deeper zone = 3 waves" |
| Field Manual tables **Ram** | Player-facing **Thief** everywhere |
| `HatchBank.SlotsPerRun` | Identifier does not exist. Convert has no per-Boarding cap |
| Dial-0 body counts **18 / 30 / 42 / 54 / 66** | `WaveDirector`: `11 + 7×i` → **11 / 18 / 25 / 32 / 39**. Co-op `min(120 × n, round((11 + 7×i) × dial) × n)` |
| `MetaUpgrades.SalvagePerKill` as 25 | Returns **42** |
| `M101` two-deck screenshots | `../Play Mechanics/The-Ship.md` (one deck, 2026-08-26) |
| `Respawns.cs` file comment (Unbanked purse) | `Respawns.Refuse` + `Going-Down.md` (realm bank, cap 2, 10 s) |
| `Demo - Single/` as the first night | `../Play Mechanics/Getting-Started.md`. The demo specs still teach Ante and Convert-as-payroll |
