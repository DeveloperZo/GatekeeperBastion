# Glyphs

<!--
Purpose: which characters the game's own text may use, and where the answer comes from.
Status: working
Update trigger: when a font asset is added, retired or regenerated, when a new source of
user-facing text appears, or when an ASCII substitute is agreed for a character.
Last updated: 2026-08-26 (created; scan scope and substitution table for Tools/lint-glyphs)
-->

**A character the font atlas cannot draw renders as a blank or a box.** It compiles, it
tests green, and it is visible only on sight. This has shipped four times: `U+25B8` in
`HudController.Weapon.cs`, `U+2713` in `NavMapPanelView.cs`, and `U+2192` in
`HudController.Panels.cs` and `StationTerminal.cs`.

Two of those four were not what they looked like, which is why this file exists rather
than a rule of thumb:

- `U+2192` **draws**. `LiberationSans SDF` chains to a DYNAMIC fallback over the whole
  LiberationSans cmap, and that fallback's atlas already holds the arrow - the trace of it
  being rendered at runtime. On a LiberationSans label the arrow was never a box. On a
  Rajdhani label it always is.
- The 2026-08-25 repair of `U+25B8` replaced it with `U+00B7`, which Rajdhani-Bold does
  not have either. The comment above that line names the right rule and the line below it
  breaks the rule again. A hand-applied fix cannot see a font atlas; a check can.

## The coverage is not written down here

There is no list of allowed characters in this file, on purpose. A list here would be a
second source of truth and would outrank the font assets. `Tools/lint-glyphs/coverage.py`
derives it every run from:

| Source | What it decides |
| --- | --- |
| `ProjectSettings/EditorBuildSettings.asset` | which scenes ship, and therefore which fonts ship |
| the shipping scenes and the prefab roots below | which font assets are bound to a `TMP_Text` |
| `Assets/TextMesh Pro/Resources/TMP Settings.asset` | the default font asset, for a `TMP_Text` that binds none |
| each font asset's `m_CharacterTable` | what is actually baked into that atlas |
| `m_AtlasPopulationMode` + `m_SourceFontFile` | whether a DYNAMIC asset can add glyphs at runtime, and from which `.ttf` |
| `m_FallbackFontAssetTable` | the same, recursively, down the fallback chain |

Read the actual table, never `m_CreationSettings.characterSequence`. The sequence is the
recipe that was *requested*; the table is what was *baked*. The Rajdhani atlases ask for
`9633` (`U+25A1`) and do not have it, because Rajdhani has no such glyph.

Run `Tools/lint-glyphs/run.sh --coverage` to print what the tree currently supports.

## The two atlases disagree, so there are two severities

`LiberationSans SDF` is STATIC over 250 characters but chains to `LiberationSans SDF -
Fallback`, which is DYNAMIC over the whole 2294-codepoint LiberationSans cmap. A DYNAMIC
asset renders a missing glyph into its atlas at runtime, so anything the `.ttf` maps
draws. `U+2192` is one of them, and the fallback atlas already holds it.

The Rajdhani atlases are STATIC over 98 codepoints with no source font and no fallback:
printable ASCII, `U+00A0`, `U+200B`, `U+2026`. Nothing else. No `U+2014`, no `U+00B7`, no
`U+00D7`, no `U+2192`.

- **box** - proven. The exact bound font lacks the glyph (scene text, Compendium), or no
  shipping font has it at all. Fails the run.
- **risk** - a C# literal using a character LiberationSans has and Rajdhani does not.
  Whether it boxes depends on which label the string reaches, which no static check can
  always know. Reported, not fatal.

Rajdhani-Bold is the **HUD** font: `Weapon Name`, `Weapon Ammo`, `Weapon Detail`, `Wave
Counter`, `Score Salvage`, `Profile Level`, `Profile Exp`, the message lines and the
drawer headers, in `Bastion_Coop`, `Range` and `Ship - CoOp`. The check derives the
**narrow surface** from that binding - the files of the classes that hold those labels,
plus the files that name those classes - and lists risk sites there first. Those are the
ones worth opening.

**The standing repair is at the asset, not in the linter.** Give the Rajdhani assets a
DYNAMIC fallback, or stop binding them, and the risk tier empties out on its own; then
set `risk_fails=all` and the whole class is gated. Until then risk is advisory, because
a gate that fires on 200 correct em-dashes gets switched off.

## What is scanned

- **C# string literals** under the roots below, lexed rather than grepped, so comments and
  XML doc are invisible to the check. Em-dashes in comments are correct and stay.
- **Strings TMP never draws** are counted and not checked: `Log.Event` writes to a plain
  text file, `GUI.Label` is IMGUI, and an attribute argument such as `[Tooltip("...")]` is
  Inspector chrome. `DemoStationGate.cs` may keep its `U+2192`.
- **Authored `m_text` in the shipping scenes**, checked against that component's own
  `m_fontAsset` - exact, no guessing.
- **`Assets/StreamingAssets/Compendium/*.md`**, which `CompendiumView` renders into TMP.
  This is the copy `CompendiumCopy` produces from `Docs/Play`, so it is the bytes that
  actually ship, already stripped of HTML comments and `## Gaps`.

## What the check cannot see

State these when quoting a clean run; a clean run is not a clean tree.

- **`Docs/Play/*.md` before the copy step.** The check reads the copy in
  `StreamingAssets/`, so a bad glyph added to `Docs/Play` is invisible until
  `CompendiumCopy` runs - at which point it ships and is caught.
- **Strings composed at runtime.** `name + suffix`, a format result, a player name, a
  realm code off the network, anything out of the save file. Only literals are visible.
- **Which label a C# literal reaches.** The narrow surface is derived at file granularity,
  not by a call graph, so a controller two hops from the HUD is not on it.
- **Prefabs instantiated by name at runtime**, and any scene not enabled in
  `EditorBuildSettings`.
- **Rich-text tags and sprite assets.** `<sprite=3>` is an atlas lookup, not a glyph, and
  the check does not follow it.
- **Bidi, combining marks and shaping.** A codepoint being in the atlas does not mean the
  sequence lays out correctly.

```glyph-lint
# Comma-separated. Keys are fixed; Tools/lint-glyphs reads this fence only.
cs_roots=Assets/Scripts
md_roots=Assets/StreamingAssets/Compendium
md_owner=Assets/Scripts/Game/Ui/CompendiumView.cs
prefab_roots=Assets
# Strings passed to these never reach a font atlas: a log file, or Unity's own IMGUI.
# Literals inside an attribute -- [Tooltip("...")], [Header("...")] -- are skipped too;
# the Inspector draws those, and the check finds them from the syntax, not from a list.
not_tmp_calls=Log.,Debug.Log,Debug.LogWarning,Debug.LogError,Debug.LogFormat,Debug.LogException,Debug.Assert,Assert.,Console.WriteLine,GUI.,GUILayout.,EditorGUI.,EditorGUILayout.,Handles.
# Editor and test code is drawn by the editor's own font, or by nothing at all.
exempt_paths=Assets/Scripts/Editor/,Assets/Tests/,Assets/_Scratch/
# no | surface | all -- whether a "risk" character is only reported or also fails the run.
risk_fails=no
# codepoint(hex)>what to write instead
substitutes=2014>--,2013>-,2192>->,2190><-,2194><->,21D2>=>,2713>[X] or OK,25B8>>,25C2><,00B7>-,00D7>x,00B0> deg,2212>-,2248>~,2264><=,2265>>=,2229>and,2022>*,00B1>+/-,2026>...
```
