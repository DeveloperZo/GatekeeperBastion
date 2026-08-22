# Gatekeeper's Protocol

Sci-fi wave-defense FPS. Your ship latches onto a dormant alien bastion and
siphons it for salvage; you fight inside, defending the **Hatch** — the umbilical
back to your ship — as gatekeepers push toward it. You set the siphon dial (rewards
double per level, and so does the pressure) and you cannot leave a run at will:
three waves, then you have cleared it or you are dead. Salvage is banked the
instant a gatekeeper dies, so dying costs you the run and nothing else.

Unity 6, PC/Steam, gray-box v0, solo now, 2–4 player co-op designed but not built.

**Docs: [Docs/README.md](Docs/README.md)** — start there.

## Clone to Play

1. In Unity Hub, create a new **Unity 6 (6000.x)** project from the
   **Universal 3D** template. Open it once, then close it.
2. Copy this repo's `Assets/` and `Packages/manifest.json` over the generated ones.
3. Reopen the project. Run menu **GatekeepersProtocol > Build > All Scenes** (builds
   and registers Splash, MainMenu, Ship, Bastion).
4. Press Play from the Splash scene. Main menu → **START NEW** → you spawn on the
   **Ship**. Buy the MK-1 at the ARMORY (you start with exactly its 100 salvage),
   then use the MAP console to embark.

## Controls

Bindings live in one table in `Assets/Scripts/Game/Input/Controls.cs`, and the game shows
you whichever are live on a panel in the bottom-left. The full list is
**[Docs/Play/Field-Manual.md](Docs/Play/Field-Manual.md)**, generated from that same table
— it is not repeated here, because the copy that used to live here drifted (it still
listed the siphon dial on the number row after it moved to `[` and `]`).

New to the game? **[Docs/Play/The-Game.md](Docs/Play/The-Game.md)** is what it is and what
a run feels like.

## Tests

Window > General > Test Runner > **EditMode** > Run All. The sim core is pure C#
and fully covered there. PlayMode tests cover scene contracts and the core loop.
Menu **GatekeepersProtocol > Validate > Integration Check** runs both and writes
`gkb_verify_report.json` at the project root (gitignored session output). Logs:
see [`Docs/Working-Rules.md`](Docs/Working-Rules.md) § Evidence.
