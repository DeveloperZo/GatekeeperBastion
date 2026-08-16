# 04 — Agent Working Rules

One file, on purpose — see the documentation rule in `README.md`.

## The three lanes

Work runs in three lanes and they are supposed to stay in balance (Zo, 2026-08-07):
**playtest**, **milestone generation**, **brainstorming** — playing the game, expanding the
game, planning the game.

**"Expanding" means building, not writing a milestone.** The lane runs
*brainstorm entry → milestone → **built** → verified*, and generation is the front of it, not
the whole of it. A milestone file is planning wearing an expansion costume; the game has not
grown until something runs.

They feed each other in one direction, and that is what balance means here:

- **Play** produces the only evidence about quality (`GatekeeperZo Notes/playthrough.md`) and
  the only numbers worth trusting.
- **Brainstorm** turns loose observations from play into ideas with a graduation test.
- **Milestone → build** turns an idea into something to play.

So a lane may not run far ahead of the lane that feeds it. **The check: at most 10 files in
`Milestones/1. planned/` and at most 3 in `2. active/`. At either cap, stop generating and
build one.** (Zo, 2026-08-08; the planned cap was 3 until then.) On 2026-08-06 the queue
reached six with none of them built, which is what this rule exists to catch — it was being
ordered by argument because there was no play data to order it with. The cap is a ceiling on
unbuilt planning, not a target to fill.

The failure is not laziness in any lane, it is that planning is the cheapest of the three and
therefore the one that quietly expands to fill a session.

## Reporting to Zo

**Lead with what you need decided, then everything else.** Decisions are the only thing an
agent cannot supply for itself, and they gate the work downstream. A response that opens with
a status report and buries a question at the bottom either costs a full turn or, worse, gets
answered by the agent proceeding on undecided ground — which is how two milestones came to be
written against unanswered questions on 2026-08-06.

The shape:

1. **What I need from you** — first, before status or findings. Skip the section entirely when
   nothing is blocked; never manufacture a question to fill it.
2. **Everything else**, ordered by what matters rather than by what happened first.

Rules for the asks:

- **Compact context.** The question, one line on why it blocks, and the recommendation. Not
  the reasoning that produced it — Zo will ask if he wants it.
- **Batch them.** Five questions in one turn beats five turns of one question.
- **Say which are blocking** and which are nice-to-have, so the blocking ones can be answered
  and the work can move.
- **Always carry a recommendation.** "Your call" with no position is work handed back rather
  than done. Zo overrules freely; that is cheap. An agent with no view is not.

The mirror of this rule lives in `_meta/_definitions/brainstorm.md`: questions that are open
belong in a brainstorm's *Open* table with an owner, and **nothing in that table may be
written into a milestone until it is answered.**

## Scope

**Build exactly what is asked and nothing more.** Three over-built systems were
cut from this project *after* being built: a loss-stake "musical chairs" rotor, a
`RunStats` aggregation layer, a two-stream logger (`Decisions.md`). Every one
was a reasonable-sounding extra that had to be deleted. When a request is
ambiguous, build the smaller reading and say what you left out.

**Simplify by default.** If you are choosing between a mechanism and a constant,
take the constant. If a system has one caller, inline it. If a doc section
describes rather than pins down, delete it.

**Do not silently expand a change.** Renaming, "cleaning up", or refactoring
adjacent code that was not part of the request is out of scope even when it is an
improvement. Say it, don't do it.

## Design rules are boundaries, not laws

Every design rule in this project — class power bands, "a Secondary must not rival a Primary",
"all five spokes meaningful", "one thing, one name" — exists to protect **fun and player
expression**. That is the purpose; the rule is a means. **A rule can be broken deliberately if
what replaces it is more fun or gives the player more to express** (Zo, 2026-08-07).

What is not allowed is breaking one by accident, or breaking one and calling it a judgement
call afterwards. The discipline that separates the two:

**Write the reason next to the rule.** A boundary stated without what it protects cannot be
knowingly overridden — the next reader can only obey it or ignore it, and both are wrong.

**A failing boundary test asks a question, it does not give an order.** When an assertion like
`SecondariesNeverRivalPrimaries` goes red, the question is *did we mean to?* — not *revert*.
The test is a tripwire that makes an unintended break visible; it is not the design.

**Breaking one costs a sentence, dated, saying what it buys.** "`SEC-D` exceeds the Secondary
band at contact range; deliberate, because the moment a Hunter reaches you should have an
answer that feels decisive." That sentence is the same standard as everywhere else here: name
what you expect so play can prove you wrong. Without it a break is indistinguishable from drift, and
six weeks later nobody can tell which rules were chosen and which were forgotten.

So the rule cannot simply be "use judgement". If every boundary is negotiable at no cost, none
of them constrains anything, and the catalog fills with weapons that each seemed reasonable
alone.

## Scene UI tools — fail closed (Zo, 2026-08-10)

Ship/Bastion UI scene mutators have already reported `OK` while leaving Interact
unable to paint a panel. The rule:

1. **WARN or FAIL in the tool log means the run failed.** Status may not say `OK`.
2. **Destroying a panel GO is forbidden until its replacement is bound** on
   `MainPanelManager.panelObject` and on the console's `_view`.
3. **Postconditions before SaveScene** (see `StationSceneGate`): Canvas scale ≥ 0.5;
   each station has `StationPanelView` with non-null `_bodyContent`, `_closeButton`,
   `_buttonPrefab`, `_labelPrefab`, `_rowPrefab`; a Background/Shadow; MPM entry live.
4. **Do not claim a terminal or HUD works from YAML inspection alone.** Open-path
   evidence is the gate log PASS, a play Interact that shows chrome, or both.
5. **Prefer `Object.Instantiate` from a working scene/prefab** over hand-cloned YAML
   trees for Shift chrome.

## Tools, scripts and the menu

Tooling accretes faster than anything else here, because every piece is individually
defensible. The gate is not "is this useful" — everything ever written was useful
once. It is **which of three kinds is this, and does it clear that kind's bar.**

**Throwaway.** Answers one question, then dies. It lives in the agent's container
under `/tmp`, never in the repo, never in the menu, and **never in a doc** — a
milestone's Verification section naming a `/tmp` path is naming something that will
not exist next session. Three milestone gates cited `/tmp/gkbharness/run.sh` until
2026-08-04, by which point the harness had been rebuilt from scratch several times and
the path pointed at nothing. Put the *answer* in the milestone; let the script die.

**Committed tool.** Lives in the repo, versioned with the code it tests, citable by
path. It earns that by being **used by more than one session for more than one
question** — the same test that separates durable tooling from a chore
(`_meta/_definitions/milestone.md`). It states its prerequisites in its own header: a
tool nobody else can run is a script with documentation.

| Path | What |
|---|---|
| `Tools/verify/run.sh` | EditMode harness (path 1). Runs lint-language and lint-arch first. |
| `Tools/lint-arch/run.sh` | Architecture fence, including the menu allowlist. |
| `Tools/lint-language/run.sh` | Language table. |
| `Tools/docgen/run.sh` | Field Manual + `4-Data.md` countable blocks. |
| `Tools/wave-report/run.sh` | Session-log reader (path 2). |
| `Tools/dashboard-data/run.ps1` | Regen `Docs/dashboard-data.js`. |
| `Tools/player-session/` | Dual-box player harness (`run.ps1`, `one-click.cmd`). |
| `Tools/bastion-sketch/` | Sketch files; emitter is sentinel-only. |
| `Editor/Tools/IntegrationCheck.cs` | Full EditMode+PlayMode + player-build file bridge. |

**Menu item.** The highest bar, because the menu is the developer's surface and every
row costs a scan. It has to **save real time or make a playthrough verifiable more
directly**, and:

- be the only way to do the thing, or clearly the fastest;
- not be a narrower duplicate of a row already there;
- not be a workaround for a bug — **fix the bug.** A recovery command in the menu is a
  defect with a button on it;
- not put in the Editor console what belongs in a file.

**One root, five folders.** `GatekeeperBastion/{Record,Logs,Build,Bake,Validate}`.
Unity draws a separator when priority differs by 11 or more; bands below are that
structure. The fence in `Architecture/README.md` is what lint-arch checks — this table
is why each row exists. If they disagree, the fence wins.

| Folder | Priority | Rows | Earns it by |
|---|---|---|---|
| **Record/** | 10 | Gameplay/This scene (F9 to stop), Gameplay/Ship, Gameplay/Bastion, Ship Flythrough (22) | Playtest capture. F9 stops. |
| **Logs/** | 40 | Open Logs Folder, Copy Latest Log Path | The log is the evidence behind every gate. |
| **Build/** | 60 | All Scenes, Place Bastion Obstacles, Group Bastion Geom In Open Scene, Unpack Bastion Geom In Open Scene | Scene generation and kit authoring (`Architecture.md`, gotcha 4). |
| **Bake/** | 80 | Bastion NavMesh | Rebake walkable mesh without wiping kit. |
| **Validate/** | 100 | Integration Check, Scene Bindings | Full suites; missing serialized refs. |

Sentinel-only, no menu: kit emit, sketch emit, field consoles, player build. Menus are
for Zo; sentinels are for agents. Same code path behind both where both exist.

**Adding a row means naming the folder, naming the row it replaces, and updating
`menu_rows` in the same change.** Seven rows existed on 2026-08-04; four survived that
question. The count grew since for capture, bake, obstacle seed, and kit geom group/unpack.

Cut on 2026-08-04, worth not re-adding: **Build Bastion Scene** (narrower duplicate),
**Reset Verification State** (a workaround — the sentinel path already self-heals),
**Dump Latest Log to Console** (a file, retyped into a worse reader).

**And nothing runs itself.** `VerifyKickOnce.cs` was an `[InitializeOnLoad]` one-shot
that cleared verification state and launched a Run on every domain reload; its own
summary said *delete after M15 verify* and it was still firing days later. A tool the
developer did not ask for, at a moment they did not choose, is the worst version of
this failure — invisible, so nothing prompts anyone to remove it. One-shots do not go
in `Assets/`.

### Bastion authoring (seed, then hand-finish)

Zo, 2026-08-12: agents **seed** kit segments, sketch placement, unlock marks, and named
obstacle sets; Zo **hand-moves** them for the finishing effect. That split is the workflow —
not a temporary compromise.

| Step | Who | How |
|---|---|---|
| Kit prefabs | agent | `gkb_bastion_kit_request.txt` |
| Segment layout + H/S/T/D + unlock graph | agent | sketch under `Tools/bastion-sketch/`, then `gkb_bastion_sketch_request.txt` (path in file, or empty for default). Emits into `Bastion.unity` under `Environment`. |
| Obstacle seed | agent | `GatekeeperBastion/Build/Place Bastion Obstacles` (or `gkb_bastion_obstacles_request.txt`). Names encode intent (`LongLane_*`, `Corner_*`, `Choke_*`, `Mask_*`). Skips hatch bay `02` when already authored. |
| Field consoles | agent | `gkb_bastion_terminals_request.txt` — Ship Desk/Screen/Label on each `BastionTerminal`; ensures GateSlab on existing `BastionDoor` markers only (does not move gates or change Door/None sides); ensures `BastionLayout` on Environment. |
| Passages vs gates | author | Segment **Door** with no `D` = passage (always walk). **`D` / `BastionDoor`** = progression gate (starts locked). Soft-lock parked. |
| Hand finish | Zo | Move segments, sides, props, spawns in the Scene view. |
| NavMesh | either | `GatekeeperBastion/Bake/Bastion NavMesh` after geometry moves. |

**Do not** run **Build/All Scenes** against a kit bastion or a dressed Ship to “refresh”
floors — when `BastionSegment`s or a `ShipDress` root are present, geometry wipe is skipped,
but the habit still belongs to the old graybox. Sketch + obstacle place + NavMesh bake are
the Bastion refresh path.

Sentinels are for agents and batch; menus are for Zo. Same code path behind both where both
exist.

**Sides vs craft.** Enum sides seed `Gen_*` meshes under `_geom`. Hand fill that must survive
rebuilds and NavMesh bake goes under `obstacles` (or under `_geom` **without** a `Gen_` name).
`Gen_*` is owned by the segment. Door/Window openings shift with **Door Along** (metres from
edge centre; N/S → ±X, E/W → ±Z) so a Full south Door can line up with a Half north Door
without manual wall fill.

### One-shot editor work — the ladder (M41)

Delete-after-verify has failed twice (`VerifyKickOnce.cs` above, and the M35 options
patch): cleanup that runs inside a script only happens when the script succeeds, and a
script that fails halfway is exactly the one that stays. The ladder replaces the
discipline with a mechanism. Cheapest rung first; a rung is only skipped with a stated
reason.

| Rung | Use when | Residue |
|---|---|---|
| **Edit the YAML** | setting a field, swapping a reference, renaming an object. Serialization is Force Text (`ProjectSettings/EditorSettings.asset`, `m_SerializationMode: 2`), so scenes, prefabs and assets are plain text | none — the diff is the review, `git checkout` is the undo |
| **`Assets/_Scratch/`** | anything needing the AssetDatabase, `PrefabUtility`, or new fileIDs | none — the folder is gitignored and emptied |
| **A committed tool** | the operation has served more than one session and more than one question (the three-kinds gate above) | it is the deliverable |

Rung 1 covers more than it appears to. The background-music change on 2026-08-10 was
three `sed` edits across a `.asset` and two `.unity` files, where the shipped
alternative was a 500-line editor script. Rung 1 stops being safe when the job needs
new GUIDs or fileIDs.

`Assets/_Scratch/` is gitignored except its committed `.gitkeep`; Unity compiles what
is dropped there and a committed `[MenuItem]` runs it. **Cleanup is not the script's
job:** `Tools/verify/run.sh` fails while the folder holds anything but `.gitkeep` (and
the `.meta` files Unity generates for it), so a scratch script that half-worked blocks
the next gate instead of surviving into the tree. And nothing in `Assets/_Scratch/` is
ever cited by a document — the same rule the three-kinds gate sets for `/tmp`.

## Leverage — the gate before tuning

Meadows' twelve points in the tiers this project uses. **The map is not here** — it is the
spine of the design docs, and this section is only the gate in front of them.

| Tier | Points | Where it is argued |
|---|---|---|
| TRANSFORMATIVE | #1 paradigm, #2 system goals, #3 actor incentives | `Design/1-Experience.md` |
| HIGH | #6 information flows | `Design/2-Dynamics.md`, section 2.1 |
| HIGH | #4 who may change the rules, #5 rules | `Design/3-Mechanics.md`, sections 3.1-3.2 |
| MEDIUM | #7 loop gain, #8 balancing loops, #9 delays | `Design/2-Dynamics.md`, sections 2.3-2.4 |
| LOW | #10 stocks and physical structure | `Design/3-Mechanics.md`, section 3.3 |
| LOW | #11 buffer sizes, #12 constants | `Design/4-Data.md` |

The layers are MDA: `1-Experience.md` is Aesthetics, `2-Dynamics.md` is Dynamics,
`3-Mechanics.md` and `4-Data.md` are Mechanics. A designer builds only the bottom and aims
at the top, which is the same claim Meadows makes about leverage.

**Before proposing or accepting a parameter change, name the tier it targets and say why a
higher tier is not the right move.** Numbers are the tier you can always return to; they
are not where a session starts. `Design/` is read in numbered order so that a change
proposed low in the ladder has to be argued against everything above it.

**Three checks before you tune:**

1. Is the parameter actually reaching the running game, or frozen in a prefab
   (`Architecture.md`, gotcha 1)?
2. Is there a structural (#10) or rule-level (#5) cause? `Design/2-Dynamics.md` holds the cases
   where there was — including three consecutive upgrade purchases that produced three
   spawn points, fixed by a group-gap change.
3. Is the thing being tuned measurable from the log at all? If not, the #6 fix comes
   first, because otherwise you cannot tell whether the tune worked.

**Archetype to watch — fixes that fail.** A symptom treated with a number returns at a new
value, and each pass lowers the urgency to look at the structure under it. The archetypes
this project has actually hit are recorded at the end of `Design/2-Dynamics.md`.

## Evidence

**Never state a balance or pacing claim without one of three things:**

1. arithmetic you can show (see `Design/4-Data.md` for the format), or
2. a source file and line you read, or
3. an explicit "**unknown, needs play data**" label.

"Feels good", "should be balanced", "roughly doubles" are not claims — they are
placeholders that later get quoted back as facts. Numbers have gone stale
mid-session in this project before; **read the source, never trust a summary**,
including this file's own numbers if they disagree with the code.

## Runtime bugs

**Read the log yourself.** Never ask the developer to relay an exception or
paste a stack trace.

- Editor: `<projectRoot>/Logs/GatekeeperBastion_yyyy-MM-dd_HH-mm-ss_p{pid}.log`
  (gitignored). Player build: `Application.persistentDataPath/Logs/`. MPPM writes one
  file per process; the pid suffix stops two Play instances sharing a path. Header
  `Process:` matches the suffix.
- One file per process per Play, newest 30 kept, readable while Unity runs.
- Line format: `[HH:mm:ss.fff] [Log|Warning|Error] [Tag] message` — wall clock,
  Unity's own level words. Stack traces follow errors and exceptions.
- Everything reaching `Debug.*` lands there: our `Log.Event` / `Log.Metric` /
  `Log.Warn` / `Log.Error` / `Log.Info` (that last needs the `GKB_VERBOSE`
  define), plus Unity's own chatter. No filtering, no dedup, **no aggregation
  layer** — plain flow lines only. Do not build counters.
- Menu `GatekeeperBastion/Logs/`: Open Logs Folder, Copy Latest Log Path.

Theorising about a runtime bug before reading the log is how a session gets
spent on the wrong cause.

## Verification

Three paths, in increasing cost:

1. **`Tools/verify/run.sh`** — the `mcs`+`mono` EditMode harness. Compiles
   `Util/Log.cs`, `Sim/*.cs` and `Tests/EditMode/*.cs` outside Unity and runs them in
   seconds (183 tests in ~80 ms on 2026-08-04). Fastest signal on any sim or tuning
   change; run it before claiming anything works. `--filter <substring>` narrows it,
   `--verbose` shows passing names and game log lines. Needs `mono-devel`. It prints
   every EditMode file it could not compile out here and why — a `skipped` line is
   coverage you did not get, so read them.
2. **`Tools/wave-report/run.sh`** — mean live Gatekeepers per Wave from session logs
   (`Design/1-Experience.md`, section 1.3). Offline reader; no band, no verify hook.
   `--summary`, `--csv`, `--html <out>`. Needs `mono-devel`. Pin:
   `Logs/GatekeeperBastion_2026-08-07_16-34-30.log`. Interactive page:
   `Docs/dashboard.html` Session tab (pick Logs/). Focus / Project tabs: regen with
   `Tools/dashboard-data/run.ps1` → `Docs/dashboard-data.js`.
3. **Integration-check file bridge** (formerly AutoVerify) — write
   `<projectRoot>/gkb_verify_request.txt`;
   with the editor open, `Editor/Tools/IntegrationCheck.cs` runs the full EditMode then
   PlayMode suites and writes `<projectRoot>/gkb_verify_report.json` with
   per-test results. Survives the domain reloads PlayMode causes. Also available
   as menu `GatekeeperBastion/Validate/Integration Check`. This is the only way to run
   the PlayMode suite, and the only authority on anything touching a MonoBehaviour,
   a scene or the save file — path 1 cannot see those.
4. **Compile errors** land in `Library/Bee/CachedNodeOutput` — read them there
   rather than asking what the editor said.

If you changed only comments, the harness result must be identical. If it isn't,
you changed code; revert.

## Driving the editor

**The editor is for effecting change and for running the game. It is never for finding
something out.** Asset serialization is Force Text
(`ProjectSettings/EditorSettings.asset`, `m_SerializationMode: 2`), so every scene, prefab
and asset is plain YAML that `grep` reads in milliseconds. A domain reload plus a scene open
is tens of seconds and a round-trip of tokens, and it is the single largest sink of
development time in this project.

**The test: could this be answered or done by reading or editing text?** If yes, the editor
is the wrong tool, whatever the question.

**Writing code is the largest activity and none of it needs the editor.** `.cs` files are
text. What needs Unity is *compiling* them, and that is not the same as driving it:

- **`Sim/` and `Tests/EditMode/` never need Unity at all.** `Tools/verify/run.sh` compiles
  them with `mcs` and runs them in about 80 ms. Write, compile, test, iterate — the editor is
  not in the loop, and after M39's assembly split the boundary is enforced by the compiler.
- **`Game/` and `Editor/` need Unity to compile, and Unity does that itself on focus.**
  Read the result from `Library/Bee/CachedNodeOutput`. **Do not drive the editor to ask
  whether it compiled** — the answer is a file. One MCP call to force a compile when the
  editor is unfocused is fine; a call per edit is the loop this rule exists to kill.
- **A compile is not a test.** Compiling proves nothing about behaviour. If the change is
  sim logic, path 1 answers it offline; if it touches a MonoBehaviour, a scene or the save
  file, only path 3 does.

| Do it offline | Needs the editor |
|---|---|
| **Write any `.cs` file** — game code, tests, tools | Compile `Game/` or `Editor/` — but read the errors from `Library/Bee/CachedNodeOutput`, do not ask the editor |
| Read any serialized value — a field, a reference, a component list, a hierarchy | Create a GameObject, component or prefab instance — fileIDs and GUIDs must be allocated by the serializer |
| Set an existing field, swap a reference, rename an object | Re-import an asset after changing its importer settings |
| Count, audit or grep anything across the tree | Bake NavMesh, lightmaps or a sprite atlas |
| Sim and tuning tests — `Tools/verify/run.sh`, seconds | PlayMode tests, and anything touching a MonoBehaviour, a scene or the save file — path 3 |
| Read what happened in a Run — `Tools/wave-report/run.sh`, the session log | Confirm the project actually compiles and the scenes load |
| Read a compile error — `Library/Bee/CachedNodeOutput` | Author UI: the human does this in the editor, not an agent through a script |

**Three rules on top of the table.**

**A milestone's acceptance criteria may not require driving the editor — only reading its
output.** A criterion that reads "the agent assembles X in the scene" turns authoring into a
code-generation problem and puts a domain reload in the inner loop. Criteria gate on a grep,
the harness, a session log, or a screenshot a human took.

**If the editor is genuinely needed, batch it.** One session, one entry point, one pass —
`-executeMethod` or a single menu item. Never a per-item loop, never a polling handshake, and
never `[InitializeOnLoad]`. The IntegrationCheck bridge is the one file-sentinel mechanism this
project has and there is not a second one.

**Never use the editor twice for the same answer.** If a value has to be checked repeatedly,
it belongs in a generated document or a check, not in a repeated look
(`README.md`: countable is generated, arguable is written).

## Log discipline

Write a line when a **player-visible thing happened** or a **refusal needs to be
answerable from the log alone** ("I clicked BUY and nothing happened"). Log the
reason, once per distinct reason — not once per frame. `Log.Info` is verbose
detail and compiles away; don't put anything load-bearing in it.
