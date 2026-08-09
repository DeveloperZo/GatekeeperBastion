# M35 — One look, drawn from one place

<!--
Status: active — re-authored twice on 2026-08-09; the second pass raises the bar from "ported" to "finished"
Update trigger: when a Shift version lands, when the palette or motif changes, or when a screen is added
Last updated: 2026-08-09
-->

Every player-facing screen is a **prefab variant of a Shift prefab**, authored in the editor,
built to a stated per-screen specification and a measured visual standard. Code binds data and
does nothing else. **The bar is not "ported" — it is that a stranger opening the build would
not call it a prototype.**

## Why the first attempt failed, and whose fault that was

An agent pass on 2026-08-09 migrated the UI off IMGUI and produced flat coloured rectangles.
It built the hierarchy at runtime in `UiFactory` / `EnsureBuilt()` and touched Shift only by
reflection, for button sounds. Zo's verdict: too minimal, no Shift chrome, novice.

**The criteria caused it.** Every acceptance criterion in the first version was a negative
grep — no `OnGUI`, no `new Color` outside `Theme.cs`, every screen is a prefab, no behaviour
change. **A grey box passes all four.** Nothing required Shift to be the substrate and nothing
described what the result should look like. The implementer met the gate exactly as written.

This version is built the other way round. **Section A fails if Shift is not the substrate.
Section B fails if it does not match Shift's own measured rhythm. Section C fails if a screen
is missing content a finished game would have. Section D fails if it looks bought.**

## Two corrections to earlier versions

**Shift does ship a HUD.** `Scenes/Simple HUD/` in Desktop, Console and Cross-Platform
variants plus URP and HDRP copies, `Scenes/Main Menu/`, and a full **Pause Menu** animation
set. What it lacks is HUD *scripts*. Both demo scenes have now been measured; the numbers are
in Section B.

**Shift's numbers are not the web's numbers.** An earlier draft specified an 8-point grid,
150ms transitions and 0.1em tracking, all defensible in general and all wrong here. Measured:
Shift is a **25-based rhythm at a 1920x1280 reference, 0 tracking, 250 to 833ms motion.**
Imposing tidy numbers on a kit is how a bought UI ends up looking worse than its own demo.

## Story

As a player, I want the game to look like someone made it on purpose from the moment it boots,
so that it reads as a game being built rather than a prototype with labels on it.

## The rule that governs everything

**If a screen could be produced by code that creates GameObjects, it has failed.** Screens are
authored assets. Code holds serialized references and pushes values into them.

---

## Section A — substrate

1. **Every screen prefab under `Assets/Prefabs/Ui/` is a prefab variant** whose
   `m_SourcePrefab` GUID resolves into `Assets/Shift - Complete Sci-Fi UI/Prefabs/`.
   **A prefab with no Shift ancestor is a failed screen.**
2. **Vendor prefabs are never edited in place.** `git status` on the Shift folder is clean
   afterwards. Styling lives in variants, which is Unity's documented workflow for a kit.
3. **No runtime UI construction.** `UiFactory.cs` and `ShiftUi.cs` are **deleted**;
   `grep -rn "new GameObject(\|AddComponent<" Assets/Scripts/Game/Ui` returns nothing.
4. **Theming is central**: one Shift `UIManager` asset, configured once, never per object.
5. **Variant depth at most two**, hierarchy nesting at most seven.
6. Shift's demo scenes and the `Evo.UI.Demo` namespace **do not ship**. Shift **v2.0.12** is
   named in the commit.

## Section B — the measured standard

| | Generic guidance | **Shift, measured 2026-08-09** |
|---|---|---|
| Canvas reference | 1920x1080 | **1920x1280**, match 0.5 |
| Type sizes | 1.333 ratio from ~16 | **25 / 28 / 30 / 36 / 40** — 28 body, 40 display |
| Tracking | 0.08–0.15em on caps | **0, everywhere** |
| Spacing rhythm | 8pt grid | **25-based**: 25, 30, 35, 50, 70 |
| Panel padding | 16 / 24 | **25 left and bottom, 30–45 right** |
| Panel enter / exit | 200–300ms | **583ms / 250ms** |
| Modal enter | 200–300ms | **833ms** |
| Button press | 100–150ms | **500ms** |

7. **Canvas reference is 1920x1280, match 0.5.** Using 1080 makes every Shift prefab the wrong
   size on screen.
8. **Spacing and padding use Shift's 25-based rhythm.** Legal: 25, 30, 35, 45, 50, 70.
   **Mixing an 8pt grid into a 25-based kit is the specific mistake this prevents.**
9. **Type is Shift's five sizes, Shift's font assets, tracking 0.**
10. **Motion matches Shift's clips** — panels 583 and 250, modals 833, button press 500. Enter
    is longer than exit. **Do not "correct" these toward web timings**; the components already
    move at these speeds and a faster wrapper reads as a stutter.
11. **Internal spacing never exceeds external spacing**, so a label obviously belongs to its
    value. Independent of which scale is used.
12. **Three depth planes on every panelled screen** — scrim, panel, content at distinct alphas,
    plus a 1px accent border. Shift's demo panels sit at 0.88, 0.69, 0.49 and 0.39; use its
    values. **A single flat fill is the exact defect this milestone exists to remove.**
13. **Five states on every interactive element** — enabled, hover, focus, pressed, disabled —
    each differing from enabled on **at least two channels**. Shift's Animator controllers
    already provide these; wiring a bare `Button` and skipping the controller forfeits them.
    Focus is distinct from hover, because a gamepad has no hover.
14. **Text contrast at least 4.5:1**, measured against the darkest and lightest background the
    element can sit over, not a flat swatch.

## Section C — what each screen contains

Low-fidelity panels with text are the thing being replaced. Each screen is specified by
content, not just by component.

15. **Splash.** Logo fade in and out, a version string, and a **"press any key" state gated
    behind load completion** with a breathing pulse of about 1.2s. **Skippable by any input at
    any point.** The main menu loads asynchronously behind it. Boot to menu under 10s.
16. **Main menu.** Entries in frequency order — **CONTINUE, START NEW, OPTIONS, QUIT** — with
    CONTINUE **first and default-focused**, and **hidden rather than disabled** when there is no
    save. A persistent campaign strip showing banked Salvage, Runs and deepest Wave, the figures
    the Ship already logs. **Behind it, the Bastion arena on a slow orbiting camera**; the level
    already exists and this costs nothing. Selection slides an accent bar between entries.
17. **Options.** Five tabs — Graphics, Audio, Controls, Gameplay, and **Accessibility as its own
    top-level tab**. Audio has separate Master, Music and SFX sliders. **Settings auto-apply and
    auto-save on change with no Apply button**, except display mode, which gets a 15-second
    countdown revert. Per-page "Reset to defaults" behind a confirm.
18. **Confirmation modals.** Only for destructive or irreversible actions: overwrite save,
    abandon Run, quit. **Verb-labelled buttons** — "Overwrite and start new" / "Keep my save",
    never Yes/No. The destructive option is **not default-focused**. Background dimmed and
    blurred. Escape always cancels.
19. **Ship HUD and station terminals.** Banked Salvage, objective line and interact prompt as a
    persistent strip. Stations are panels with scrollable priced rows; a row shows name, cost,
    current level and what the purchase does. **Unaffordable rows are visibly disabled, not
    hidden.**
20. **Bastion HUD.** **Content is unchanged and belongs to M36**; this milestone changes only
    how it is drawn. The presentation treatment still applies: values **lerp rather than snap**,
    a changed value punch-scales to about 1.15 and settles in 150ms, and elements not currently
    relevant sit near 40% alpha rather than full. Everything inside a 5% title-safe margin.
21. **End screens.** Sequenced, not dumped: the verdict held alone for about a second, then the
    headline number, then stat rows staggered 200 to 300ms apart, then the Salvage tally
    counting up, then Actions. **Any input fast-forwards to the fully populated state; a second
    input proceeds.** At most six stats on screen, the rest behind a details toggle.
22. **Transitions.** **Nothing hard-cuts.** One vocabulary everywhere: fade out 200 to 300ms,
    load under black, fade in. Menu music continues across screens rather than restarting.
    Loading always shows motion, with a minimum display of about 0.8s so a fast load does not
    flash.

## Section D — so it does not look bought

The failure mode after "novice" is "asset flip". These are cheap and they are what make the kit
read as this game's.

23. **Repalette before building any screens.** One accent drawn from the game world — the Hatch
    amber already in `Theme.cs` — one warning colour, one neutral ramp. **Shift's default
    cyan-on-dark is the single most recognisable thing about it.**
24. **Replace the font.** A display face for headers, a clean sans for body. Kit fonts are
    recognised faster than kit shapes.
25. **One signature motif, applied everywhere** — a single corner treatment, one bracket shape,
    one stripe. Chosen once, used on every panel. **One shape consistently beats five
    decorations.**
26. **Layouts are rebuilt from Shift's components, not edited copies of its demo scenes.** Keep
    the mechanics, discard the arrangement. Demo proportions surviving into the game is the
    second most recognisable tell.
27. **No placeholder copy anywhere** — no "Button", no "Panel Title", no "Option 1". Every
    string is this game's vocabulary (`_meta/_definitions/language.md`).
28. **UI audio exists.** `UIElementSound` with hover, confirm, back and error clips, **pitch
    randomised about 5%**, hover debounced and quieter. The build has none today, and silence is
    the loudest unfinished signal.
29. **Match the UI's fidelity to the art's.** The game is gray-box. **A glossy, bloom-heavy
    sci-fi UI over gray-box geometry reads as two projects glued together** — dial the UI
    *down*, flatter and more restrained, so the two read as one intentional style. This is the
    likeliest way to make the game look worse while making the UI look better.

## Section E — effects, in priority order

The first four are required. The rest are taken while cheap and dropped the moment they cost a
day.

| | Effect | In scope |
|---|---|---|
| 1 | **Staggered element entrance** — rows fade and slide 20–30px, 200ms each, 40–60ms apart, one reusable component | **Required.** The highest single win; turns a static panel into a designed screen |
| 2 | **UI sound set** (criterion 28) | **Required** |
| 3 | **Value animation** — count-up, bar lerp, punch-scale on change | **Required**, and it is criterion 20 |
| 4 | **Menu background** — arena scene, slow camera drift, panels parallaxing 5–15px | **Required**, criterion 16 |
| 5 | Blur and dim behind modals and pause, rendered once on open rather than per frame | Take |
| 6 | Vignette and light grain on menus, under 25% | Take |
| 7 | Particle accents in the **background layer only** | Take. **Never inside panels** — that reads as a screensaver |
| 8 | Chromatic aberration on transitions, 200ms maximum | Only if free |
| 9 | Scanlines and CRT overlay | **Do not.** Lowest ratio here, hurts legibility, and it is the "I put a hexagon on it" of sci-fi UI |

**Skip entirely**: idle breathing on every element, animated backgrounds behind body text, more
than one accent colour.

## Section F — architecture

30. **The pause overlay is non-blocking. Do not use `Time.timeScale = 0`.** This project's
    architecture exists for co-op — the Sim seam is the whole point (`Docs/Architecture.md`) —
    and time cannot be frozen in multiplayer. **Building a blocking pause now means
    retrofitting it later**, which is exactly the class of decision the seam was created to
    avoid. Duck audio, blur the frame, keep the game running behind a semi-transparent panel.
31. **No behaviour change.** `Tools/verify/run.sh` and the EditMode suite pass with the same
    test count and the same test names.
32. **The M21 wheel keeps its behaviour** — five spokes, two dark, `[` and `]` rotate, the
    projection in fire and reload seconds still correct.

## Verification

**Sections A, B, F**: greps, `git status`, and reading numbers off prefab YAML and Animator
clips. All binary.

**Section C**: the playthrough path below, one screenshot per screen against its criterion.

**Sections B, D, E** need four tests, each producing an artefact kept with the commit:

1. **Squint test.** Screenshot each screen, 8 to 10px Gaussian blur. **One element must
   dominate, related items must clump, buttons must still read as buttons.** Uniform mush fails
   criterion 12 regardless of what the YAML says.
2. **Greyscale test.** Desaturate. If hierarchy or selected-versus-unselected disappears,
   meaning is carried by hue alone and criterion 13 has failed.
3. **Rhythm audit.** Read every `m_Spacing` and padding value out of the screen prefabs;
   **target zero values off Shift's 25-based scale.**
4. **State matrix.** Every interactive prefab against the five states, one screenshot per cell.
   **An empty cell is a bug**; a cell identical to `enabled` is criterion 13 failing.

**Side by side against the demo.** Screenshot the game's main menu beside `Main Menu (Desktop)`.
**They must not be mistakable for each other**, which is Section D passing, **and the game's
must not look cheaper**, which is Sections B and C passing.

**In context, never on a swatch.** Every HUD screenshot is taken over a busy Wave-3 frame with
bodies on screen. The first attempt looked tolerable against grey and fell apart against the
game.

**Playthrough path**: cold boot; splash skipped by keypress; main menu with the arena behind it;
OPTIONS and back; START NEW with a save present so the modal fires; all four stations; rotate
the wheel and buy one Tier; embark; clear a Wave; shortcut panel; die; end screen; return.
Repeat at 1280x720.

## Implementation

**Delete** `Game/Ui/UiFactory.cs` and `Game/Ui/ShiftUi.cs`. They are the failed approach and
leaving them invites a third attempt down the same path.

**Do Section D first, before building any screen.** Repalette and refont on the `UIManager`
asset, and pick the motif. Doing it afterwards means restyling every screen twice.

**Then the two reusable pieces**: the staggered-entrance component and the value-animation
component. Every screen after them uses both.

**Then the screens, in this order**: Ship HUD, station terminals, splash, main menu, options,
end screens, Bastion HUD, **ARMORY wheel last** — it holds M21's freshly shipped behaviour and
goes after the pattern is settled.

**Keep** `Game/Ui/Theme.cs` as the semantic token list with the `UIManager` asset configured to
match. Mapping: `Ink` to `primaryColor`, `InkDim` to `secondaryColor`, `Panel` to
`backgroundColor`, `Danger` to `negativeColor`; `Accent`, `Warn`, `Good`, `InkFaint` and `Line`
have no Shift equivalent and stay in `Theme.cs`.

**Keep** the existing views and controllers as the binding layer, stripped of construction.

Rejected: normalising Shift onto an 8-point grid or shortening its motion. Both were in an
earlier draft and both are wrong; see Section B.

Rejected: writing our own components against Shift's sprites. That is the first attempt with
extra steps, and it forfeits the states, motion and audio the kit already ships.

Rejected: `Time.timeScale = 0` for pause. Criterion 30.

## Watch

- **The likeliest failure is a second wireframe that passes the greps.** Sections B to E and the
  four tests exist for that reason, and **a screenshot that fails the squint test fails the
  milestone** with every grep green.
- **The second likeliest is imposing tidy numbers on the kit.** See Section B.
- **The third is the UI outshining the game.** Criterion 29. Gray-box geometry under a glossy
  HUD looks worse than gray-box geometry under a restrained one.
- This is a large milestone and **Section E is where it grows without limit.** Items 8 and 9 are
  droppable and 9 is already dropped. If it runs long, cut from the bottom of Section E, never
  from Sections A to D.
- True Shadow glow on a combat HUD is overdraw. Ship and menus freely, Bastion sparingly, and
  check the frame cost rather than assuming it is free.

## Depends on / Unblocks

Depends on Shift v2.0.12, imported. **Unblocks M36** and the UI half of every later screen.
