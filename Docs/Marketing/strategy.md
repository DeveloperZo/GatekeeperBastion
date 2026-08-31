# Strategy

<!--
Purpose: How Gatekeeper's Protocol should be received, and in what order the
market is allowed to hear which sentence.
Status: working
Update trigger: when friends-alpha closes, when the locked pitch restamps, or
when Steam-tag policy for this title is decided.
Last updated: 2026-08-30 (0.34.2: Ante parked so "author the swarm" is not a live
verb; payroll inverted; one-deck hull; Extraction Shooter added to the refuse list;
body counts 11–39)
-->

## Category

We are a **manual first-person shooter** in which you **board a held vessel**,
**start every Wave yourself**, and **hold the Hatch while Salvage banks as it
falls**. Solo is a complete product. A friend on the same Ship is the session the
roadmap is pointed at (`../Roadmap.md` friends alpha).

The Ante that used to let the Boarder author the swarm is **parked** in 0.34.2
(`Ante.Enabled = false`). Pace is still the player's. Stake is not, until the
flag flips.

We are not Bullet Heaven (Valve's 2026 tag: upgrades while **automatically**
attacking hordes —
[Tweaktown](https://www.tweaktown.com/news/111680/steam-has-finally-given-vampire-survivors-and-similar-games-its-own-genre-tag/index.html)).
We are not an idler. We are not castle defense. We are not an extraction shooter.

If Steam tagging is ever chosen, **do not take** Idler, Incremental, Bullet
Heaven, Building, or **Extraction Shooter**. The first four are the ancestor's
shelf
([Guns 'n Goblins](https://store.steampowered.com/app/881940/Guns_n_Goblins/)).
Extraction Shooter is the 2026 PvPvE shelf ([Arc Raiders](https://store.steampowered.com/app/1808500/ARC_Raiders/),
[Marathon](https://store.steampowered.com/app/3065800/Marathon/)) — Convert and
Docked will be filed there if the page uses their verbs. Prefer FPS,
Co-op, Action, Sci-fi, and only add Survival if it means "the Hatch can kill
the Boarding," not "survive the night on your fortress."

Do not print the Play-banned phrase **Wave-defense** on a store page
(`../_meta/_definitions/language.md`, rule 3: store sentences follow the Play ban).
**The ban is on the copy, not on the repository.** `Tools/lint-language/run.sh` scans
`Docs/Play/` when that directory exists (it currently does not — player docs live in
`Docs/Play Mechanics/`), and the root `README.md` opens "Sci-fi wave-defense FPS" — that
is a repository map for builders and is not a violation. Builder notes inside these
Marketing files, including this sentence, may name the banned terms; the copy block may not.

## Who it is for

**Primary (now):** a friend of the developer who will extract a zip, Host or
Join, and finish a Boarding. That market is specified in `../Release-Gate.md` and
`gtm.md`. It is small and it is the only one that can falsify the Fellowship
sentence. **0.34.2** is that zip.

**Primary (Steam, later):** players who already like **co-op FPS with a job**,
not a spectacle count. The jobs that match the build: hold a door with a
friend; bring a gun you authored; leave with money that banked as it fell.
Inference, not a survey.

**Not for (if copy is honest):** players shopping thousands-on-screen, AFK
income, hero XP, "rebuild your base," or PvPvE extract. Sending them here from
the ancestor's press ("incremental horde survivor FPS") is a failed impression.

## Pillars (builder)

Mapped to `../Design/1-Experience.md` §1.2. Do not print the aesthetic names.

| Order | Player-facing proof | Must be true in the build |
|---|---|---|
| 1 | Alone or with a friend, one Ship, three hatches | Shared Ship closed (`M72`). The hull is **one deck** (`../Play Mechanics/The-Ship.md`, 2026-08-26; `M101` two-deck is superseded). Five-Wave dry-run still open |
| 2 | The gun is the build; a Protocol is who you are on this Boarding | Three sheets ship — Default's 0-salvage claim (`M70`), VEIL and FORGE (`M94`), FORGE with turret and Outrider — and three Primaries read as three guns (`M93`). "Their" Protocols still wait on one diluted Gatekeeper modifier from the same pool |
| 3 | You cut into a vessel **they** still hold | True now (NAV MAP premise, `M27`). A Wave starts at the Hatch and ends at a terminal they garrison |

The fight — quiet Prep, `T`, the walk, the Hatch — is how those three are tested. It is
not a fourth store pillar. "Hold the Hatch" and "Salvage banks as they fall" are the locked
loop sentences; both Accuracy rows in `pitch.md` are **true**.

## What marketing must not do

- Lead with body count. Dial-0 Waves 1–5 **solo** are 11 / 18 / 25 / 32 / 39.
  Co-op multiplies by connected seats, cap `min(120 × n, round((11 + 7×i) × dial) × n)`
  (`WaveDirector`; `../Design/4-Data.md` generated wave table). The ancestor leads with thousands. The co-op formula line that still reads `18 + 12×i` in 4-Data is stale — cite the director.
- Show Prep as empty boredom. Quiet until **you** start is the product.
- Promise a career path ("Apprentice to Master"). We have first-Salvage unlocks
  and a wheel, not a named rise.
- Use "Bastion" as the player's home. Home is the **Ship**.
- Print Ante / ante up / "author the swarm" as a live verb. Parked 2026-08-30.
- Cite the Field Manual for the Ante table or for a T1 Wave count that disagrees with `DemoZones`.
- Write **Ram** anywhere. The rename landed 2026-08-20; it is Thief in code and in copy alike.
- Say a single death ends a Boarding, or that you can always come back. `Respawns.MaxRespawns = 2`; the purse is the realm's bank; bleed-out is 10 s.
- Say Salvage waits on a Convert. Kills bank on landing.
- Show the two-deck hull. It is not the ship.
- Take the Extraction Shooter tag, or write extract / exfil / raid / loot-out / “get in, get paid, get out” as the loop.
- Write siege / defend the fortress, unlock skills, or ultimate arsenal. Those are KF3 / Space Marine 2 copy.

## Sequence

| Stage | Market | Printable |
|---|---|---|
| Now (0.34.2 zip) | Friends send (`gtm.md`) | Alone; defeat Gatekeepers; Salvage banks as it falls; start at the Hatch. Not "with friends" as a solved product. Not "their Protocols." Not Ante. Not a shared realm. Not Convert as payroll. |
| Friends-alpha closed | Same people, then a wider zip | The first pitch sentence in full |
| Ante restored (`Ante.Enabled`) | Same people | The wager, if Zo restamps it into Copy |
| One diluted Gatekeeper modifier (probe) | Store / trailer | Protocols sentence. The sheets themselves already ship |
| Second destination | Store | Bastions, plural |
| Steam page | Wishlist | Tags and trailer per this file; copy from `pitch.md` only where Accuracy is **true** |

Observation that would kill a Steam push: a wishlist comment that calls it an
idle game, a Vampire Survivors FPS, "Guns 'n Goblins in space", or an extraction
shooter because the page used their verbs (deluge, rebuild, thousands, XP, extract,
exfil, raid, siege, ultimate arsenal).
