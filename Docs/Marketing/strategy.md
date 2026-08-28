# Strategy

<!--
Purpose: How Gatekeeper's Protocol should be received, and in what order the
market is allowed to hear which sentence.
Status: working
Update trigger: when friends-alpha closes, when the locked pitch restamps, or
when Steam-tag policy for this title is decided.
Last updated: 2026-08-25 (Wave-defense ban scoped to the copy and answered against the root
README; pillars restated after M70 closed and M94/M93/M101 shipped; Ram and the death claim
added to what marketing must not do)
-->

## Category

We are a **manual first-person shooter** in which you **board a held vessel**,
**author every Wave**, and **convert Salvage through the Hatch**. Solo is a
complete product. A friend on the same Ship is the session the roadmap is
pointed at (`../Roadmap.md` friends alpha).

We are not Bullet Heaven (Valve's 2026 tag: upgrades while **automatically**
attacking hordes —
[Tweaktown](https://www.tweaktown.com/news/111680/steam-has-finally-given-vampire-survivors-and-similar-games-its-own-genre-tag/index.html)).
We are not an idler. We are not castle defense.

If Steam tagging is ever chosen, **do not take** Idler, Incremental, Bullet
Heaven, or Building. Those are the ancestor's shelf
([Guns 'n Goblins](https://store.steampowered.com/app/881940/Guns_n_Goblins/)
lists Idler, Incremental, Building, Survival among user tags — Steam page,
not cached in this repo). Prefer FPS,
Co-op, Action, Sci-fi, and only add Survival if it means "the Hatch can kill
the Run," not "survive the night on your fortress."

Do not print the Play-banned phrase **Wave-defense** on a store page
(`../_meta/_definitions/language.md`, rule 3: store sentences follow the Play ban).
**The ban is on the copy, not on the repository.** `Tools/lint-language/run.sh` only scans
`Docs/Play/` for that list, and the root `README.md` opens "Sci-fi wave-defense FPS" — that
is a repository map for builders and is not a violation. Builder notes inside these
Marketing files, including this sentence, may name the banned terms; the copy block may not.

## Who it is for

**Primary (now):** a friend of the developer who will extract a zip, Host or
Join, and finish a Run. That market is specified in `../Release-Gate.md` and
`gtm.md`. It is small and it is the only one that can falsify the Fellowship
sentence.

**Primary (Steam, later):** players who already like **co-op FPS with a job**,
not a spectacle count. The jobs that match the build: hold a door with a
friend; bring a gun you authored; leave with money that survived the Hatch.
Inference, not a survey.

**Not for (if copy is honest):** players shopping thousands-on-screen, AFK
income, hero XP, or "rebuild your base." Sending them here from the ancestor's
press ("incremental horde survivor FPS") is a failed impression.

## Pillars (builder)

Mapped to `../Design/1-Experience.md` §1.2. Do not print the aesthetic names.

| Order | Player-facing proof | Must be true in the build |
|---|---|---|
| 1 | Alone or with a friend, one Ship, three Hatches | Shared Ship closed (`M72`), and the ship became a two-deck hull with a realm and a record on it (`M101`, `M96`, `M97`). Five-Wave dry-run still open |
| 2 | The gun is the build; a Protocol is who you are on this Boarding | Three sheets ship — Default's 0-salvage claim (`M70`, closed), VEIL and FORGE (`M94`, closed 2026-08-24) — and three Primaries read as three guns (`M93`). "Their" Protocols still wait on one diluted Gatekeeper modifier from the same pool |
| 3 | You cut into a vessel **they** still hold | True now (NAV MAP premise, `M27`) |

The fight — anting up, Waves, the Convert — is how those three are tested. It is
not a fourth store pillar. "Fight off waves to defend your Salvage" is the loop
sentence, with the Salvage caveat in `pitch.md`.

## What marketing must not do

- Lead with body count. Dial-0 Waves 1–5 **solo** are 18 / 30 / 42 / 54 / 66.
  Co-op multiplies by connected seats, cap `min(120 × n, …)`
  (`../Design/4-Data.md`). The ancestor leads with thousands.
- Show Prep as empty boredom. Quiet until **you** start is the product.
- Promise a career path ("Apprentice to Master"). We have first-loss unlocks
  and a wheel, not a named rise.
- Use "Bastion" as the player's home. Home is the **Ship**.
- Cite the Field Manual for the T1 Wave count. Four sources disagree and its
  sentence is a hardcoded literal rather than a generated one (`gaps.md`).
- Write **Ram** anywhere. The rename landed 2026-08-20 and the identifier has zero
  hits in `Assets/Scripts/`; it is Thief in code and in copy alike.
- Say a single death ends a Boarding. `M98` prices one way back per dead seat, paid
  from that seat's carried Unbanked. Saying you can always come back is equally wrong.

## Sequence

| Stage | Market | Printable |
|---|---|---|
| Now | Friends send (`gtm.md`) | Alone; defeat Gatekeepers; Convert your Salvage. Not "with friends" as a solved product. Not "their Protocols." Not a shared realm, which is built and unproven on two machines. |
| Friends-alpha closed | Same people, then a wider zip | The first pitch sentence in full |
| One diluted Gatekeeper modifier (probe) | Store / trailer | Protocols sentence. The sheets themselves already ship |
| Second destination | Store | Bastions, plural |
| Steam page | Wishlist | Tags and trailer per this file; copy from `pitch.md` only where Accuracy is **true** |

Observation that would kill a Steam push: a wishlist comment that calls it an
idle game, a Vampire Survivors FPS, or "Guns 'n Goblins in space" because the
page used their verbs (deluge, rebuild, thousands, XP).
