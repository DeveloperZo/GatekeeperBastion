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
"all five spokes meaningful", "one thing, one name" — exists to protect the ranked aesthetics
in `Design/1-Experience.md` §1.2 (stamp 2026-08-16: Fellowship, Expression, Fantasy). Restamp
that sentence when the rank table moves. **A rule can be broken deliberately if what replaces
it serves that list better** (Zo, 2026-08-07: more fun, or more for the player to express).

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
   Settings chrome present (Background, Background Blur, Top and Bottom Panel); each
   station has `StationPanelView` with non-null `_bodyContent` and `_closeButton`, its
   mapped body view with no null serialized field, and a live MPM entry with a non-null
   `panelObject`. **A surviving `Templates` folder is a FAIL** — panels are authored now
   and clone nothing, which is why `_buttonPrefab`, `_labelPrefab` and `_rowPrefab` are
   gone rather than merely unchecked.
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
| `Tools/verify/run.sh` | EditMode harness (path 1). Runs lint-language, lint-arch and lint-vendor first. |
| `Tools/lint-arch/run.sh` | Architecture fence, including the menu allowlist. |
| `Tools/lint-language/run.sh` | Language table. |
| `Tools/lint-vendor/run.sh` | A bought pack's Shader Graph keeps the Built-In target this project renders with — the loss of one turned every robot magenta and was invisible in a gitignored dump. Reads the `vendor-lint` fence in `Decisions.md`. |
| `Tools/docgen/run.sh` | Field Manual + `4-Data.md` countable blocks. |
| `Tools/lint-constants/run.sh` | The other half of that: prose and XML comments that restate a `Sim/` array. Reads the `constant-lint` fence in this file. `python3`, not mono. |
| `Tools/release-gate/run.sh` | The machine-red half of `../Docs/Release-Gate.md` (also from `Tools/verify/run.sh`). |
| `Tools/wave-report/run.sh` | Session-log reader (path 2). |
| `Tools/dashboard-data/run.ps1` | Regen `Docs/dashboard-data.js`. |
| `Tools/player-session/` | Dual-box player harness (`run.ps1`, `one-click.cmd`). |
| `Tools/bastion-sketch/` | Sketch files; emitter is sentinel-only. |
| `Assets/Scripts/Editor/Tools/IntegrationCheck.cs` | Full EditMode+PlayMode + player-build file bridge. Renamed from `AutoVerify.cs`; a doc still saying AutoVerify is stale, not describing a second tool. |

**Menu item.** The highest bar, because the menu is the developer's surface and every
row costs a scan. It has to **save real time or make a playthrough verifiable more
directly**, and:

- be the only way to do the thing, or clearly the fastest;
- not be a narrower duplicate of a row already there;
- not be a workaround for a bug — **fix the bug.** A recovery command in the menu is a
  defect with a button on it;
- not put in the Editor console what belongs in a file.

**One root, five folders.** `GatekeepersProtocol/{Record,Logs,Build,Bake,Validate}`.
Unity draws a separator when priority differs by 11 or more; bands below are that
structure. The fence in `Architecture/README.md` is what lint-arch checks — this table
is why each row exists. If they disagree, the fence wins.

| Folder | Priority | Rows | Earns it by |
|---|---|---|---|
| **Record/** | 10 | Gameplay/This scene (F9 to stop), Gameplay/Ship, Gameplay/Bastion, Ship Flythrough (22) | Playtest capture. F9 stops. |
| **Logs/** | 40 | Open Logs Folder, Copy Latest Log Path | The log is the evidence behind every gate. |
| **Build/** | 60 | All Scenes (60), Place Bastion Obstacles (61), Range Scene (62), Group Bastion Geom In Open Scene (62), Protocol Bar (63), Unpack Bastion Geom In Open Scene (63), Copy Compendium Pages (64) | Scene generation and kit authoring (`Architecture.md`, gotcha 4), plus the two scene-content emitters and the Compendium page copy the build needs. |
| **Bake/** | 80 | Bastion NavMesh | Rebake walkable mesh without wiping kit. |
| **Validate/** | 100 | Integration Check, Scene Bindings | Full suites; missing serialized refs. |

Sentinel-only, no menu: kit emit, sketch emit, field consoles, player build. Menus are
for Zo; sentinels are for agents. Same code path behind both where both exist.

**Adding a row means naming the folder, naming the row it replaces, and updating
`menu_rows` in the same change.** Seven rows existed on 2026-08-04; four survived that
question. Sixteen exist on 2026-08-25 — the count grew for capture, bake, obstacle seed,
kit geom group/unpack, the Range and Protocol Bar scene emitters, and the Compendium page
copy.

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
| Segment layout + H/S/T/D + unlock graph | agent | sketch under `Tools/bastion-sketch/`, then `gkb_bastion_sketch_request.txt` (path in file, or empty for default; second line is the scene path). **Write that second line.** `BastionSketchEmitter.DefaultSceneRelative` is still `Assets/Scenes/Bastion.unity`, and that scene was deleted on 2026-08-24, so an omitted scene path now resolves to nothing — `Bastion_Coop.unity` or `Explore - Kit.unity` is what you mean. Emits into the named scene under `Environment`. New rooms rebuild in `Generated/` then **settle into `Authored/`** (unpacked, unique scene geometry). Rooms already in `Authored/` are not duplicated. Kit footprint retune of Control Rooms: `gkb_bastion_control_rebuild_request.txt` (scene path, optional sketch path). Settle leftovers: `gkb_bastion_settle_request.txt` (scene path). |
| Obstacle seed | agent | `GatekeepersProtocol/Build/Place Bastion Obstacles` (or `gkb_bastion_obstacles_request.txt`). Names encode intent (`LongLane_*`, `Corner_*`, `Choke_*`, `Mask_*`). Skips hatch bay `02` when already authored. |
| Field consoles | agent | `gkb_bastion_terminals_request.txt` — Ship Desk/Screen/Label on each `BastionTerminal`; ensures GateSlab on existing `BastionDoor` markers only (does not move gates or change Door/None sides); ensures `BastionLayout` on Environment. |
| Passages vs gates | author | Segment **Door** with no `D` = passage (always walk). **`D` / `BastionDoor`** = progression gate (starts locked). Soft-lock parked. |
| Hand finish | Zo | Move segments, sides, props, spawns in the Scene view. |
| NavMesh | either | `GatekeepersProtocol/Bake/Bastion NavMesh` after geometry moves. |

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

- Editor: `<projectRoot>/Logs/GatekeepersProtocol_yyyy-MM-dd_HH-mm-ss_p{pid}.log`
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
- Menu `GatekeepersProtocol/Logs/`: Open Logs Folder, Copy Latest Log Path.

Theorising about a runtime bug before reading the log is how a session gets
spent on the wrong cause.

**Root litter is not evidence.** Editor batches leave `gkb_*.log` / `gkb_*.json` /
`gkb_*.txt` at the project root; they are gitignored and disposable. Do not cite them
in a milestone. The session log under `Logs/` is the record. One-shot handoffs land in
`_to_delete/` or `Assets/_Scratch/`, never as permanent root folders.

**Root stays folders + a handful of contract files.** Tracked at the top level:
`README.md`, `AGENTS.md`, `CLAUDE.md`, `.gitignore`, `.gitattributes`, `.vsconfig`.
Unity regenerates `*.csproj` / `*.slnx` at the root on open — gitignored, hidden in
the editor tree, safe to delete. Do not commit them. The player escape hatch template is
`Tools/release/run-windowed.bat`.

## Verification

Three paths, in increasing cost:

1. **`Tools/verify/run.sh`** — the `mcs`+`mono` EditMode harness, and more than the
   tests. In order: lint-language, lint-arch, lint-vendor, lint-constants, the
   `Assets/_Scratch/` empty check, then two compile stages, then
   `Tools/docgen/run.sh --check` and
   `Tools/release-gate/run.sh`. Stage 1 builds `Assets/Scripts/Sim/` alone — `Log.cs`
   included, it lives at `Sim/Util/Log.cs` — against `Tools/verify/Shims.cs`, which is
   the offline enforcement of the Sim boundary. Stage 2 builds
   `Assets/Tests/EditMode/*.cs` plus `Game/Settings/` and `Game/Demo/` against that dll,
   the same dependency direction the asmdefs enforce. Seconds end to end (183 tests in
   ~80 ms on 2026-08-04). Fastest signal on any sim or tuning change; run it before
   claiming anything works. `--filter <substring>` narrows it, `--verbose` shows passing
   names and game log lines. Needs `mono-devel`. It prints every EditMode file it could
   not compile out here and why — a `skipped` line is coverage you did not get, so read
   them. `Assets/Scripts/Util/` holds only `LogFile.cs` and is not compiled here.
2. **`Tools/wave-report/run.sh`** — mean live Gatekeepers per Wave from session logs
   (`Design/1-Experience.md`, section 1.3). Offline reader; no band, no verify hook.
   `--summary`, `--csv`, `--html <out>`. Needs `mono-devel`. **Do not pin a log by name**
   — `Logs/` is gitignored and keeps only the newest 30, so a named log is gone within a
   fortnight and off any clone from the start; the pin that stood here from 2026-08-07
   already resolved to nothing. Point at the run instead. Both prefixes parse:
   `GatekeepersProtocol_*` is what `LogFile.Product` writes today,
   `GatekeeperBastion_*` is the pre-rename name and still the bulk of the corpus.
   Interactive page: `Docs/dashboard.html` Session tab (pick Logs/). Focus / Project
   tabs: regen with `Tools/dashboard-data/run.ps1` → `Docs/dashboard-data.js`.
3. **Integration-check file bridge** (renamed from AutoVerify; a doc still saying
   AutoVerify means this) — write `<projectRoot>/gkb_verify_request.txt`;
   with the editor open, `Assets/Scripts/Editor/Tools/IntegrationCheck.cs` runs the full EditMode then
   PlayMode suites and writes `<projectRoot>/gkb_verify_report.json` with
   per-test results. Survives the domain reloads PlayMode causes. Also available
   as menu `GatekeepersProtocol/Validate/Integration Check`. This is the only way to run
   the PlayMode suite, and the only authority on anything touching a MonoBehaviour,
   a scene or the save file — path 1 cannot see those.
4. **Compile errors** land in `Library/Bee/CachedNodeOutput` — read them there
   rather than asking what the editor said.

If you changed only comments, the harness result must be identical. If it isn't,
you changed code; revert.

### Restated constants

`Tools/docgen/run.sh --check` proves the **derived** half of a countable document. Between
`GKB:GEN` and `GKB:END` the text is regenerated from `Sim/`, so it cannot be wrong for long.
It says nothing about the **hand-written** half, and on 2026-08-26 that half drifted in three
places at once while `--check` stayed green: the prose in `Design/4-Data.md`, the formula
block a dozen lines above it, and `WaveDirector`'s own XML comment three lines under the
array it describes all put a **1** in the third slot of `cloakerCount`, where
`CloakerPerWave` has a **0** — claiming the Cloaker debuts a wave earlier than it does. The
generated table in that same file was right the whole time.

The failure has a shape: **a number appears in two places and only one of them is the
source.** `Tools/lint-constants/run.sh` covers one narrow, mechanical case of it — a numeric
array literal, restated outside its declaration, that *nearly* matches a live `Sim/` array.
Near is the signal. A restatement that agrees is silent, and a list of unrelated numbers is
too far away to match, so the check has one thing to say and says it rarely.

**What it cannot catch, and this is most of the surface:**

- **Prose that paraphrases.** "The Cloaker debuts on the fourth wave" carries the same claim
  with no literal in it. Out of reach, and a clean run is not proof the docs are right.
- **A restatement written any other way.** `0/0/1/2/3` and "0, 0, 1, 2, 3" in a sentence are
  both invisible. Slash-separated lists were measured and rejected: they collide with every
  `1/2` progress counter in `Docs/Demo - Single/` — 52 hits, all noise.
- **Scalar constants.** A doc naming a `const` and a wrong value beside it was built and
  measured too, and dropped at 6 hits, every one a false positive: a line may cite
  `FinaleEliteCount` while discussing HP, and `Build`, `Count` and `Curve` are English words
  before they are symbols. It would also not have caught the Cloaker drift, which names no
  symbol. Restating a scalar in prose stays a review problem.
- **A drift that changes the array's length**, or one that moves more values than
  `max_distance`. At that distance it is a different array, not a stale copy of this one.

`min_length` is not tuning. Two-value arrays (`FirstWaveByZone`, `EndWaveByZone`) match
`{1,2}`-shaped set notation in prose and were the only false positive measured on the tree;
below three values an array is not distinctive enough to be worth comparing. `must_resolve`
is the tripwire that stops the check being gutted by accident: rename `CloakerPerWave`, or
write it in a shape the declaration pattern cannot read, and the check goes **exit 2** rather
than quietly finding nothing to compare and passing. A negative check that can pass by
seeing nothing is not a gate.

`Tools/lint-constants/run.sh` reads every value below from this fenced block; changing a
value here changes the check with no script edit. `--selftest` proves the rule still fires,
`--no-exempt` shows what the exempt list hides.

```constant-lint
min_length=3
max_distance=2
sim_dir=Assets/Scripts/Sim
doc_dirs=Docs
code_dirs=Assets/Scripts,Assets/Tests
must_resolve=CloakerPerWave,HunterPerWave,ThiefPerWave,Curve
exempt=Docs/Changelog.md,Docs/Milestones/3. completed,Docs/_meta/Logs,Docs/_to_delete,Docs/Brainstorms
```

The `exempt` list is history, not convenience. `Changelog.md` and a completed milestone
**record what the value used to be** — `M63-3` states the old `CloakerPerWave` twice as the
change it was making, and correcting that would be falsifying the record. Brainstorms propose
numbers that are deliberately not live yet.

One authoring constraint falls out of this: **a document may not spell a drifted array as a
literal**, including this one, or the check reports itself. Say which slot is wrong instead.
The alternative was exempting `Working-Rules.md` wholesale the way `lint-language` exempts
its own rule document, and that would blind the check to every other number on this page.

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
