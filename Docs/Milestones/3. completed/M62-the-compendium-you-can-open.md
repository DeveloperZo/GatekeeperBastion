# M62 — The Compendium you can open

<!--
Status: completed 2026-08-15
Update trigger: closed
Last updated: 2026-08-15
Closed on: 2026-08-15. Gate: `CompendiumFormTests` 6/0 (Game + See also on every
hand-written page, no Page|Answers table, Game dimension has no milestone IDs,
StreamingAssets stem set matches Docs/Play, ExtractGameView omits Reference/Gaps,
catalog volumes cover every Play page except README). Menu
`GatekeeperBastion/Build/Copy Compendium Pages`; player-build hooks the copy.
Pause Esc → Compendium on Bastion and Ship; scroll nav of all volumes/entries;
Game body + See also; HUD hidden while open. Zo close — criteria 2–6 playthrough
is confirmation (same as M57/M59).
-->

The Compendium is the Boarder's working knowledge of the game — volumes of related
entries, not a glossary of terms. Markdown lives in `Docs/Play/` (M28 language lint;
player-facing name is **Compendium**, never Codex / encyclopedia / wiki). The
player-visible change is opening those volumes from pause and reading them inside the
build.

Voice and entry shape are pinned in `../../_meta/_definitions/language.md` (*Player-facing
voice*). This slice authors the pages to that pin; the reader copies them. A dictionary
index (Page | Answers) is the rejected form. It shipped on 2026-08-13 and was recut the
same day.

Form references (builder-only): Mass Effect's Codex (volumes, Primary vs appendix, 2–4
paragraphs) and God of War 2018's journal (origin plus fight tells in the same pause
entry). Steal from those: dedicated pause command, practical and world in one page,
numbers in an appendix. Reject: unlock-on-encounter, companion first-person, folding the
reader into the key list (ME3), a completion ledger, a stat-block-as-entry.

## Story

As a Boarder, I want to open a Compendium from pause and read how this world fits
together, so that I can learn the garrison, the Hatch, and the hull without a developer
on voice chat and without treating the game as a list of nouns.

## Why it matters

Friends alpha is a session a stranger can operate. The Play pages already hold the
knowledge; they were filed as a wiki of one page per noun. A friend does not look up
"Ram". They need the garrison as a set of refusals, the Hatch as the umbilical, the
Ship as four subjects — then the Ram entry, which only makes sense after that.

ME1's Codex was opened because a short briefing arrived as you met the thing and
explained the physics the scene had just named. Always-on and no voiceover remove both
hooks. The remaining equivalent is a first paragraph that states a rule the HUD does not,
and See also from the fight.

Leverage: **#6, information flows**. The rules are in the build. The Compendium is how a
player finds them again.

**Loop**, not arc: re-read as the fight teaches you. Unlock-on-encounter is an arc and
waits. Alpha is always-on.

## Kill criterion

The second friends session after the reader ships. **If Zo is still answering questions
the open entry already states, in either of those two sessions, stop and recut the
reader** (wrong volume, unreadable in pause, or nobody opens it). If nobody opens it in
both sessions, that is a kill — Shown failed.

## Acceptance criteria

1. **Form and voice (markdown).** `Docs/Play/README.md` is volume front matter —
   Situation, a Run, Bastion, Garrison, Arms, Hull, appendix — not a table of Page |
   Answers, and not builder instructions in the visible body. Every hand-written page in
   that folder except `README.md` and `Field-Manual.md` has `## Game` (pause dimension),
   `## Reference` (fuller brief), and a `## See also` that names at least one other entry.
   Countable facts and bindings stay only in `Field-Manual.md`. The pause reader shows
   title + Game + See also only. Visible Game bodies pass `../../_meta/_definitions/language.md`
   *Player-facing voice* (rules 1–13). Kind and weapon Game sections are origin plus fight
   tells, one pause. Hubs are 2–4 short paragraphs in Game. `Tools/lint-language/run.sh`
   still passes; Play/ never uses the never-called column (Codex, encyclopedia, wiki, lore
   bible) or the extra `builder_play_ban` terms (`Wave-defense`, `Codex`).
2. **Pause opens Compendium.** Esc (already non-blocking) shows a **Compendium** command
   on the pause overlay as its own control, not a row inside the playable-key list.
   Activating it opens the reader without closing pause's ownership of input.
   `Time.timeScale` stays 1. No `GUILayout` in the Bastion HUD.
3. **The reader is the volumes.** Left: the six volumes plus appendix. Right: the open
   entry as scrollable TMP (or a Shift scroll view). Opening an entry from See also
   inside the reader navigates to that entry. Field Manual is the appendix volume, not
   mixed into Garrison or Arms. Getting Started sits in Situation as first hours, not as
   a seventh volume. The reader omits HTML comments and every `## Gaps` section.
4. **One source.** Player-build pages are a copy of `Docs/Play/*.md` into
   `Assets/StreamingAssets/Compendium/` at build (editor preprocessor or a menu the
   player-build pipeline already runs). Runtime reads StreamingAssets, never
   `Docs/Play/` (that path does not exist in a player build). Do not duplicate into
   ScriptableObjects this slice. Generated `Field-Manual.md` is copied too; regenerate
   then copy. The copy step does not rewrite voice.
5. **Always-on.** Every shipped entry is readable from the first pause in a campaign.
   No unlock gate. A later unlock slice is a different milestone.
6. **The player action:** Splash → Ship → EMBARK → Esc → Compendium. Open Garrison →
   Gatekeepers, then Ram via See also. Open Hull → The Ship. Close the reader, close
   pause, fight still live. Music (M59) still playing. Client can open it too (local UI).
7. EditMode: every required `## See also` present; README has no `| Page | Answers |`
   table; StreamingAssets copy lists the same stem set as `Docs/Play/` (minus nothing
   the copy step is told to include). Suites green. Solo unchanged.

## Implementation

1. **Pin** the markdown form and voice: README volumes, See also mesh, `language.md`
   Voice section and `builder_play_ban` extras. Recut every hand-written Play page that
   fails Voice (genre pitch, milestone IDs, bindings, builder "where it is going"). If a
   later edit reintroduces a Page | Answers index or a first-person diarist, criterion 1
   fails until it is gone.
2. **Create** `Assets/Scripts/Editor/Tools/CompendiumCopy.cs` — copies `Docs/Play/*.md`
   → `Assets/StreamingAssets/Compendium/`. Runs from the player-build pipeline and from
   a menu `GatekeeperBastion/Build/Copy Compendium Pages`. `Field-Manual.md` included. No
   transform of the prose except stripping HTML comments and `## Gaps` sections (and the
   rest of that heading's body until the next `##`). Prefer keeping comments out of the
   rendered body by stripping at copy, not by deleting them from `Docs/Play/`. Runtime
   extracts the Game dimension from the copied page.
3. **Create** `Assets/Scripts/Game/Ui/CompendiumView.cs` — LOCAL-OWNER. Volume list +
   entry body. Parses markdown enough for headings, bold, links of the form
   `[text](Other-Page.md)`. Full CommonMark is rejected. Authored on
   `Canvas - Pause Menu` (Shift scroll + Rajdhani, same theme). Serialized references,
   no name-resolution fallback.
4. **Change** `HudController.Pause.cs` / pause canvas — Compendium command, separate
   from the key list. Open/close the view; pause listing of keys (M59) remains. If M59
   has not shipped, the command still works; the key list can be empty.
5. **Create** `Assets/Tests/EditMode/Ui/CompendiumFormTests.cs` — criterion 1 (See also
   present; README has no `| Page | Answers |` table; visible bodies contain no `M[0-9]`
   milestone ids). Copy-step test: stem set matches.
6. **Change** `../../_meta/_definitions/language.md` Voice row if a later page invents a
   narrator. `Docs/README.md` Play/ row. Roadmap, Changelog. Brainstorm
   `../../Brainstorms/compendium.md` is already deleted on graduation — do not recreate
   it.

Rejected: renaming `Docs/Play/` → `Docs/Compendium/` this slice (M28 + docgen + every
milestone link). Rejected: ScriptableObject entries. Rejected: unlock-on-encounter.
Rejected: Atreus-style first-person companion (there is no companion). Rejected: a Ship
station that is the Compendium (pause is the journal; stations own one subject each).
Rejected: rendering Gaps to the player. Rejected: folding Compendium into the pause key
list. Rejected: look-at-a-Gatekeeper-to-open-its-page (Hades context-open; later if
ever). Rejected: XP or a completion percentage as the reason to open.

## Supporting work

- **Driven by** Esc → Compendium — here. Pause itself is M35; key list on pause is M59.
- **Shown by** the command on the pause overlay — here. Not by a "new entry" ping; alpha
  is always-on, so the command has to be findable without a collectible tick.
- **Reads back as** the open entry's title and body, and See also navigation — here.

If M59 slips, this still ships with a Compendium command and no playable-key list on
pause. If M61 slips, the reader still opens; the gun may still be invisible.

## Verification

1. Criterion 1, 7 — `CompendiumFormTests` (6/0). Closed 2026-08-15.
2. Criteria 2–6 — Zo close. Playthrough (EMBARK → Esc → Compendium → Garrison → Ram,
   Hull → The Ship, Field Manual; Gaps/comments absent; HUD off while open) is
   confirmation, not the gate.

## Do not build

| Not building | Why not |
|---|---|
| Folder rename `Play/` → `Compendium/` | M28 / docgen path; later if ever |
| Unlock-on-encounter | Arc; alpha is always-on |
| New lore that is not in the build | We write what we built |
| Station that opens the Compendium | Pause is the journal |
| Full markdown engine | Headings, bold, `.md` links |
| Companion narrator / dual-voice | No companion |
| Completion ledger or XP for reading | Trains noun-hunting |
| Context-open from a live Gatekeeper | Later if ever |

## Watch

- StreamingAssets copy can go stale. The copy step belongs in the player-build pipeline
  M58 already requires, and in editor Play Mode via the menu or an `InitializeOnLoad`
  that runs only when a Play page's timestamp is newer — do not copy every domain reload
  unconditionally (gotcha: editor hitch).
- Prefab freeze on pause TMP: author on the canvas.
- `## Gaps` omission is load-bearing: those lists are builder audit, not player text.
- Non-blocking pause: reader must not freeze the fight clock. A long entry the player
  reads during Assault is a fight they are not fighting — keep entries one-pause short
  so that is a glance, not a study hall.
- Always-on with no VO and no "new" tick: Shown is the command itself. If friends do not
  find Esc → Compendium, the kill criterion fires.

## Depends on / Unblocks

Depends on M35 pause existing. Better after M59 (pause already lists keys) but not
blocked on it. Unblocks a friends session that can answer "what is a Ram" without Zo.
