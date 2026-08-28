# Pitch

<!--
Purpose: The store sentences Zo locked, and the truth value of each.
Status: working
Update trigger: when Zo restamps the four sentences, or when a cited
milestone closes and a row's status changes.
Last updated: 2026-08-25 (May print recut to player words per language rule 3 / trap 8; VEIL
and FORGE ship; the Cloaker added; death has a priced return; two cited identifiers no longer
exist and are registered in gaps.md. The Copy block is untouched — Zo restamps it)
-->

## Copy

Zo, 2026-08-17. Do not paraphrase in this file; restamp the block.

Break into the Bastion alone or with friends. Defeat the Gatekeepers. Use their own Protocols against them. Fight off waves to defend your Salvage.

## Accuracy

| Sentence | Status | Why |
|---|---|---|
| Break into the Bastion alone or with friends. | **alone true; friends lobby true; five-Wave dry-run open** | Solo boarding: `../Play/The-Game.md`, `../Play/A-Boarding.md`. Shared Ship + party EMBARK closed (`M72`, host log `13-24-37` / Relay `KPRMPR`). Much has landed under this sentence since 2026-08-17 without moving it: a realm a Boarder travels between (`M96`), the realm's record on the hull (`M97`), a late arrival who follows the party in at a break (`M98`), a realm a friend can open while its owner is offline (`M100`), and a 28-room co-op bastion (`M95`). Full friends-alpha — install, join without the developer, finish five Waves together — is still the Roadmap sentence and still unplayed, and `M100`'s two-machine criteria are owed. One destination: **ALPHA-7** (`SceneFlow.BastionName`). |
| Defeat the Gatekeepers. | **true** | A Wave ends when every Gatekeeper of it is dead (`../Play/A-Boarding.md`). The Boarding is **Escaped** only after Master Control (`M54`), not field Wave 5. Four kinds ship since `M63-3` added the Cloaker (2026-08-24). |
| Use their own Protocols against them. | **false until** a Gatekeeper wears a Protocol modifier, diluted | Shared pool (`../Brainstorms/boarder-protocols.md`): Boarder form is concentrated; Gatekeeper form is the same modifier, watered down, authored, never rolled. Default in CREW is **your** assault sheet (Barrage `G`, Rush `H`); that is not this sentence, and neither are VEIL and FORGE, which ship as of `M94` (2026-08-24) and are also yours. AEGIS and LEDGER are Coming Soon. Albion deeds unlock the next sheet; they do not make Gatekeepers wear the pool. Probe: one modifier-wearing Gatekeeper a player can name after a Boarding. |
| Fight off waves to defend your Salvage. | **true with caveat** | Hold the **Hatch**. Kills fill **Unbanked**; a **Convert** at the Hatch drips and dumps it to banked, capped per Boarding. A loss wipes leftover Unbanked; converted stays (`../Play/Salvage.md`). Not kills-on-drop, and not defending a fortress you own. The cap used to be cited here as `HatchBank.SlotsPerRun = 3`; that identifier no longer exists in the tree, so the number is unverified and registered in `gaps.md`. |
| **Death is no longer simply the end.** | not a locked sentence — a change under the fourth one | `M98`'s recut (2026-08-24) gives every dead seat, host and solo included, one priced way back: it pays out of the Unbanked it is carrying at `150 × (taken + 1)`, the counter the Boarding's rather than the seat's, and the remainder of the carry survives the death (`Sim/Run/Respawns.cs`). Nothing else can pay. A seat carrying nothing still ends the Boarding there. Copy that says a single death ends everything is now wrong; copy that says you can always buy back is also wrong. |

## May print

Store sentences use the player's words, never the builder's — Boarding, Ante, ante up,
Convert (`../_meta/_definitions/language.md`, rule 3 and trap 8). The old words survive in code
and in the rationale column above; they never cross into this block.

Alone; defeat Gatekeepers; Convert your Salvage through the Hatch (caveat in Accuracy). The
Boarder, the Ship, the Bastion they hold, the Hatch, Gatekeepers (Default, Hunter, Thief, and
the Cloaker since `M63-3`), the Ante as a wager, banked Salvage surviving a loss, and a
Boarding a friend can join between Waves.

Do not print the four sentences as one block until the friends half and the Protocols row are true.

## May not print yet

- AEGIS or LEDGER as if they ship. VEIL and FORGE do (`M94`), and may be named.
- Plural Bastions / a star map (one destination, ALPHA-7).
- A five-Wave friends evening a stranger finishes without you (`Roadmap.md`).
- “Their” Protocols as a CREW or HUD sentence.
- A shared realm as proven — `M100` is built but has never run on two machines.
- Any number for how many Converts a Boarding allows; the identifier it was read from is gone.

## Do not cite for copy

| Stale | Prefer |
|---|---|
| `../Play/Field-Manual.md` “T1 is 5”; `../Play/Getting-Started.md` five clears to open the door; this row's own “3”, which was true when written | Cite no number. `DemoZones.WavesPerTrigger` is the source, four documents currently disagree about it, and the Field Manual's sentence is a hardcoded literal in `Tools/docgen/Program.cs` rather than a generated one. Registered in `gaps.md` |
| Field Manual tables **Ram** | Player-facing **Thief** everywhere. The code identifier was renamed with `M63-3` on 2026-08-20 and `Ram` now has zero hits in `Assets/Scripts/` — it survives only in shipped milestone files and play records |
| `HatchBank.SlotsPerRun` | The identifier does not exist. Convert is capped per Boarding; the cap's source is unfound |
