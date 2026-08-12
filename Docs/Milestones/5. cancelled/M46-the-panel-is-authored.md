# M46 — The panel is authored

<!--
Status: cancelled, Aug 2026
Update trigger: none — cancelled
Last updated: 2026-08-12
Cancelled 2026-08-12 (Zo).
-->

Every element a station panel can ever show exists in Ship.unity at author time. Code writes
text into fields, toggles rows on and off, and wires clicks. Nothing is cloned, nothing is
assembled at runtime, and what the Scene view shows with the panel enabled is what Play shows.

Decision record (Zo, 2026-08-11): fully authored, zero cloning — chosen over
authored-sections-plus-row-prefab. The accepted cost: a roster change is a scene edit.

## Story

As the game's only author, I want to lay a station panel out in the editor and trust that
Play shows exactly that layout, so that panel design is a visual act instead of a compile,
play, walk, press E, squint loop.

## Why it matters

**The 2026-08-11 play screenshot is the argument.** The panel finally paints, and the layout
is what StationPanelView's builder emitted: slot buttons floating above the title, a stat
line in space, CATALOG orphaned mid-panel. Nobody authored that. Nobody *can* author it — the
order and shape live in `BuildPanel` code, so every layout change is a code change, which is
backwards for a purely visual concern.

**The repo already knows the right pattern, twice.** The panel chrome (Station Title, Banked
Salvage, Lifetime) is authored in the scene and bound by `SetHeader` — it has never
scrambled. The ARMORY wheel is an authored hierarchy under Wheel Slot that
`ArmoryWheelView.Configure` fills from a display struct — it has never scrambled either. The
body is the only part still assembled from cloned templates at runtime, and it is the only
part that looks wrong. This milestone finishes the migration the wheel started.

**Scope note on the development rule.** "Never reconstruct at runtime what an asset already
defines" carves out runtime list rows as legitimate. That carve-out priced in a variable
roster; ours is not variable at runtime — it changes only when Zo ships a weapon, which is
already a scene-touching event. The carve-out buys nothing here and costs the authoring loop.

## The shape

One authored view per station, replacing the shared builder:

- **ARMORY** — two slot-toggle buttons; equipped block (name/tier line, stat line); wheel
  section (rotate hint, Wheel Slot as today, not-authored-ring note); CATALOG block with one
  authored catalog row per weapon in the slot's roster. A catalog row is name, cost, level,
  effect, one action button, one reason line — the same fields `StationRowView` binds today,
  authored once as scene objects per row.
- **FABRICATION** — HATCH heading + hatch armor row; EQUIPMENT heading + slot row + repair
  rig row; PAYLOAD heading + decoy row + bay line.
- **CREW QUARTERS** — one row per attribute track.
- **NAV MAP** — its current labels and the disembark action, authored.

States that today produce *different* rows (owned vs buyable vs locked vs equipped) become
the *same* row with its texts rewritten and its button/reason toggled. A row that cannot
apply (equipment before the slot is bought) is deactivated, not absent.

Rows live under each station's `List/List Content` (VerticalLayoutGroup + ContentSizeFitter,
added 2026-08-11), so authored elements align and overflow scrolls. Authoring stays visual:
reorder by dragging in the hierarchy, restyle by editing the row objects.

Authored placeholder text must be realistic sample content ("MK-1 Sidearm · T3/10"), never
"New Text" — the edit-mode view is the design surface and it should read like the game.

## Acceptance criteria

1. **Author-time truth:** with ARMORY enabled in edit mode, the Scene/Game view shows the
   complete panel — every block, row, and button in final position — with no Play press.
2. **Play matches author:** a screenshot of the open ARMORY panel in Play and the edit-mode
   view of criterion 1 differ only in text values and row active-states, not in position,
   order, or style of any element.
3. **Zero cloning:** `Instantiate` is absent from the station panel path. `StationPanelView`'s
   `AddLabel` / `AddButton` / `AddSpacer` / `AddPricedRow` / `BeginRow` / `ClearBody` and the
   template fields are deleted, and the Templates folders are deleted from all four stations
   in Ship.unity.
4. **Every station binds:** all four stations open with correct values through the authored
   views; buy, equip, slot toggle, and wheel purchase still work, and the disabled/reason
   states still show (unaffordable rows stay visible and disabled with their reason, as
   today).
5. **Roster overflow fails loud:** if the catalog roster for a slot exceeds the authored rows,
   the panel logs one error naming the station, the slot, and the missing weapon, and shows
   an authored "ROWS MISSING — see log" line. Silent truncation is a criterion failure.
6. **The gate agrees:** `StationSceneGate.AssertOpenPath` asserts the new postconditions
   (authored view present, its required bindings non-null, `_bodyContent` is List Content)
   and passes on Ship.unity as committed. Its current `_bodyContent == List` check dies here.
7. **Refresh discipline holds:** the ContentHash change-gate keeps working — steady state
   allocates nothing and rewrites nothing; a banked-salvage change rewrites only text.

## Implementation

**Create** `Game/Ui/ArmoryPanelView.cs`, `FabricationPanelView.cs`, `CrewQuartersPanelView.cs`,
`NavMapPanelView.cs` — serialized fields for every authored element, one `Bind(MetaState)`
entry point each, no layout math, no colour, no font (M35: look rules live in the scene and
UI Manager). A shared `PanelRowView` (rename of `StationRowView`) binds one authored row.

**Change** the four `StationTerminal` subclasses — `BuildPanel(view, m)` becomes a bind call
into their authored view. `TickWhileOpen`, `ContentHash`, open/close, and the wheel's
`Configure` path are untouched.

**Change** Ship.unity — author the four panels: rows under List Content, ARMORY catalog rows
to the current roster per slot. This is scene work in the editor, which is the point.

**Delete** the builder API and template fields from `StationPanelView` (or fold what remains —
header, close, wheel slot accessors — into the per-station views and delete the class), the
Templates folders, and `StationTerminal`'s ScrollRect/Mask force-disable workaround, which
has been inert since List Content landed.

**Change** `StationSceneGate` to the criterion-6 postconditions.

Rejected: authored sections with one cloned row prefab for the catalog. Rejected by Zo
2026-08-11 in favour of full authoring; revisit only if criterion 5 fires in practice more
than once a milestone, and say so in Decisions.md when it does.

Rejected: an edit-mode preview generator that fills the panel with sample rows. That is the
runtime builder wearing a costume; the layout would still be emergent, just earlier.

## Supporting work

- **Driven** — no new input; E and the existing panel verbs.
- **Shown** — the panel is its own discovery mechanism; nothing new to teach.
- **Reads back** — criteria 1, 2, and 4, played and screenshotted.

## Verification

1. Criteria 1 and 2 — the two screenshots, side by side, filed in
   `../../GatkeeperZo Notes/playthrough.md` with the verdict.
2. Criterion 3 — `grep -rn "Instantiate" Assets/Scripts/Game/Ui Assets/Scripts/Game/Ship`
   returns nothing on the panel path; Ship.unity contains no `m_Name: Templates` under a
   station.
3. Criterion 5 — temporarily add a roster entry in a scratch branch, open ARMORY, confirm the
   error line and log, revert.
4. Criteria 4, 6, 7 — play all four stations once; run the station gate; watch the log for
   rewrite spam while idling in an open panel.

## Watch

- **The wheel is already right — do not migrate it.** Wheel Slot and `ArmoryWheelView` stay
  exactly as they are; this milestone makes the rest of the panel meet their standard.
- **State-variant rows are where the old design will leak back in.** The catalog row must be
  one authored object whose texts and button change, not two stacked variants toggled — two
  variants per state is how the scene stops being readable.
- **Placeholder drift.** Authored sample text will go stale against real formats; that is
  cosmetic and acceptable. What is not acceptable is code that skips writing a field because
  the placeholder "looked right".
- Serialized-fields-outrank-code applies with more surface now: a tuning value visible in a
  panel comes from the sim at bind time, never authored into the scene as a number someone
  forgets to bind.

## Depends on / Unblocks

**Depends on** the 2026-08-11 Ship.unity repairs (List Content per station, fonts wired, Main
Panels animator disabled) — all committed.
**Unblocks** any future station content, which becomes scene authoring instead of builder
code. Pairs with the OptionsView rebind repair (M35 criterion 13 spam), which is separate
and stays separate.
