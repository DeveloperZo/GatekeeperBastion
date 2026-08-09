# M24 — An options menu

<!--
Status: later
Update trigger: when a setting is added, or when the settings store changes
Last updated: 2026-08-06
-->

One screen where the player changes how the game behaves, reachable from the main menu and
from inside a Run, backed by a single settings store.

## Story

As a player, I want somewhere to change how the game plays for me, so that preferences are
things I set rather than things I put up with.

## Why it matters

There is no options screen. Mouse sensitivity — the one setting that exists — lives in a
lone `PlayerPrefs` key written from the main menu and read in `FirstPersonController.Awake`,
with no other home. M15 drafted a held-`Tab` reference overlay, cut it as "a settings screen
in disguise", and said plainly that an options menu is where that belongs and the game has
none.

Everything that wants to be a setting is currently blocked behind that absence. This is
**durable infrastructure under the tooling exception** (`_meta/_definitions/milestone.md`):
it is what M25 needs, and every preference after it.

Leverage tier: **#6, information flows** — it gives the player a channel into the game's
rules that does not currently exist.

## Acceptance criteria

1. An options screen reachable from the main menu **and** from a pause in the Derelict and
   on the Ship. Same screen, one implementation.
2. Opening it in a Run pauses nothing that matters and frees the cursor; closing it restores
   the previous cursor state exactly, including mid-Assault.
3. A `Settings` store in `Sim/` or `Game/Flow/` that is the **only** reader and writer of
   preference values. `FirstPersonController` reads from it, never from `PlayerPrefs`
   directly.
4. Mouse sensitivity migrates into the store, keeping the existing `PlayerPrefs` key so a
   player's current value survives the update.
5. Every setting has a stated default, and a RESTORE DEFAULTS action that returns all of
   them to it.
6. Settings persist across sessions and are **separate from the campaign save** — changing a
   setting never touches `gatekeeper_meta.json`, and a Dev-tainted session still writes
   settings (settings are not progress).
7. Adding a setting is one row in one place: a declaration with a name, a type, a default and
   a label. Adding one must not require touching the screen's layout code.
8. The screen lists at least mouse sensitivity, and is honest about being short — no
   placeholder rows for settings that do not exist.

## Usable, or it did not ship

- **Driven** — a key from the pause state and a button on the main menu, both in
  `Controls`/`ControlId` and on the M15 shortcut panel.
- **Shown** — the screen itself, with current values and defaults.
- **Reads back** — one log line per changed setting, naming the setting and the new value,
  so a support question about behaviour is answerable from the log.

## Implementation

**Create** `Game/Flow/Settings.cs` — the store. Declaration-driven per criterion 7.

**Create** `Game/Flow/OptionsScreen.cs` — one IMGUI screen, drawn from the declarations.

**Change** `Game/Flow/MainMenuController.cs` — its inline sensitivity slider is replaced by a
button that opens the screen. Delete the duplicated slider rather than leaving two.

**Change** `Game/Player/FirstPersonController.cs` — read from `Settings`.

**Change** `Game/Input/Controls.cs` — the open-options binding.

### Tests

**Create** `Tests/EditMode/Flow/SettingsTests.cs` — defaults, round trip, restore-defaults,
and that a settings write leaves the campaign save byte-identical (reuse M17's
`PlayModeSaveFixture` idea).

## Open questions

- **Whether the Derelict pause should stop the sim** is unresolved and matters for co-op
  later, where one player pausing cannot stop a shared Run. Recommend: it does **not** stop
  the sim, so the behaviour is already co-op-shaped. *Zo's call.*

## Watch

- The temptation is to fill this screen. It ships with the settings that exist and no others;
  a screen of greyed-out rows is worse than a short screen.
- Do not put key rebinding in this milestone. `Controls` supports a binding list and rebinding
  is its own milestone with its own conflict rules.

## Depends on

Nothing. **Blocks M25.**
