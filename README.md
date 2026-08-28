# Gatekeeper's Protocol

Sci-fi wave-defense FPS. Unity 6, PC/Steam, first-person. You are the **Boarder**:
your Ship cuts into a garrisoned **Bastion**, you hold the **Hatch** (the umbilical
home), and **Gatekeepers** come to take it back. The **Ante** is a wager you ante up
before a Wave; unbanked Salvage only reaches the ship on a **Convert**. Solo is
playable; host-client co-op exists (Relay join code or LAN).

**Start here for the game and the docs:** [`Docs/README.md`](Docs/README.md).  
**Start here if you are an agent:** [`AGENTS.md`](AGENTS.md) (skills in `.claude/skills/`
outrank that file when they apply).

## Repository layout

```
.
├── AGENTS.md / CLAUDE.md     Agent working contract (CLAUDE.md is a one-line pointer)
├── .claude/skills/           gkb-design, gkb-development, gkb-verification,
│                             gkb-brainstorm, gkb-strategy
├── .cursor/rules/            Points at AGENTS.md
├── Docs/                     Design, architecture, process, Compendium — see Docs/README.md
├── Assets/                   Unity content (scenes, prefabs, scripts, packs)
│   ├── Scripts/Sim/          Pure C# sim assembly — no MonoBehaviour / scene / Time
│   ├── Scripts/Game/         MonoBehaviour views and hosts
│   ├── Scripts/Editor/       SceneBuilder, IntegrationCheck, bake/validate tools
│   ├── Scripts/Util/         LogFile — one log file per session
│   ├── Tests/                EditMode + PlayMode
│   └── Scenes/               Splash, MainMenu, RealmSelect, Ship - CoOp,
│                             Bastion_Coop, Explore - Kit, Range
├── Tools/                    Headless verify, lints, docgen, dashboard-data, release
├── Packages/                 UPM manifest (+ PackageCache locally)
└── ProjectSettings/          Unity project settings
```

Tracked at the root: `README.md`, `AGENTS.md`, `CLAUDE.md`, `.gitignore`,
`.gitattributes`, `.vsconfig`, `.cursorignore`. Unity regenerates `*.csproj` /
`*.slnx` here on open — gitignored and hidden in the editor tree; delete anytime.
Local staging only: `Builds/`, `Recordings/`, `_backup/`, and `_to_delete/` at the root
and under `Docs/` (all gitignored).

### Docs shelves (summary)

| Path | Role |
|---|---|
| [`Docs/README.md`](Docs/README.md) | The game on one page + docs index |
| [`Docs/Design/`](Docs/Design/) | MDA ladder — read **1 → 4** in order |
| [`Docs/Architecture.md`](Docs/Architecture.md) | What the codebase is; code map |
| [`Docs/Architecture/`](Docs/Architecture/) | How code is written (rules) |
| [`Docs/Working-Rules.md`](Docs/Working-Rules.md) | Lanes, evidence, verify paths, Tools menu |
| [`Docs/Play/`](Docs/Play/) | Compendium (player-facing) |
| [`Docs/Story/`](Docs/Story/) | Fiction beats / premise (draft; not on the Design ladder) |
| [`Docs/Milestones/`](Docs/Milestones/), [`Roadmap.md`](Docs/Roadmap.md) | Work units + order |
| [`Docs/Decisions.md`](Docs/Decisions.md) | Cuts and locks |
| [`Docs/Brainstorms/`](Docs/Brainstorms/) | Ideas before they graduate |
| [`Docs/Demo - Single/`](Docs/Demo%20-%20Single/), [`Demo - Coop/`](Docs/Demo%20-%20Coop/) | Demo beat specs |
| [`Docs/dashboard.html`](Docs/dashboard.html) | Project dashboard |

### Tools (summary)

| Path | Role |
|---|---|
| [`Tools/verify/run.sh`](Tools/verify/run.sh) | Fast Sim + EditMode harness (no Unity) |
| [`Tools/docgen/run.sh`](Tools/docgen/run.sh) | Field Manual + `Design/4-Data.md` from Sim |
| [`Tools/dashboard-data/run.ps1`](Tools/dashboard-data/run.ps1) | Regen `Docs/dashboard-data.js` |
| [`Tools/lint-arch/`](Tools/lint-arch/), [`lint-language/`](Tools/lint-language/), [`lint-vendor/`](Tools/lint-vendor/) | Architecture / vocabulary / vendor gates |
| [`Tools/release/run-windowed.bat`](Tools/release/run-windowed.bat) | Escape hatch template (ship beside the player exe) |

## Clone to play

1. In Unity Hub, create a **Unity 6 (6000.x)** project from the **Universal 3D**
   template. Open once, then close.
2. Copy this repo's `Assets/` and `Packages/manifest.json` over the generated ones.
3. Reopen. Menu **GatekeepersProtocol > Build > All Scenes** (pins Splash, MainMenu,
   RealmSelect, Ship - CoOp, Bastion_Coop at the head of the build list, and keeps
   other build scenes after them).
4. Press Play from Splash. Main menu → choose a realm → **START NEW** → you wake on
   the Ship. Buy the MK-1 at the ARMORY, then EMBARK from the NAV MAP.

## Controls and the Compendium

Bindings live in `Assets/Scripts/Game/Input/Controls.cs`. The live list is
**[Docs/Play/Field-Manual.md](Docs/Play/Field-Manual.md)** (generated). What the game
is: **[Docs/Play/The-Game.md](Docs/Play/The-Game.md)**.

## Tests and logs

- **EditMode:** Window → General → Test Runner → EditMode → Run All (Sim is pure C#).
- **PlayMode:** same runner, PlayMode tab (scenes / loop contracts).
- **Both headless:** menu **GatekeepersProtocol > Validate > Integration Check** →
  `gkb_verify_report.json` at the project root (gitignored).
- **Fast Sim-only:** `Tools/verify/run.sh`.
- **Logs:** [`Docs/Working-Rules.md`](Docs/Working-Rules.md) § Evidence
  (`Logs/GatekeepersProtocol_*.log`).
