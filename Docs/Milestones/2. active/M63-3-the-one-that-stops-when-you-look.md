# M63-3 — The one that stops when you look

<!--
Status: active — unparked 2026-08-24 at Zo's direction (Active runs 4/3 with it; flagged)
Update trigger: when the Cloaker's watch rule, its cone, its body or its Wave counts
change, or when a fifth kind ships
Last updated: 2026-08-24 — premise check against the tree; five corrections, marked
"Premise fix 2026-08-24" where they land
-->

The garrison's fourth kind. A **Cloaker** advances only while no Boarder is looking at
it: centre it in your view and it stops dead and materialises; look away and it walks.
It takes the last unspent FaxLab3D silhouette, `SK_Tactical`. The **Ram→Thief rename**
and the **`in_build` Compendium filter** come across from `M63` with it, because a
fourth kind cannot be authored or read about without them.

## Story

As a Boarder, I want an enemy that only moves where I am not looking, so that watching
the room becomes something I spend instead of something I get for free.

## Why it matters

`M63` names the defect exactly: **three kinds were designed for one room and the room
changed.** The Bastion has levels, lanes, blind corners, terminals and a second player,
and Default, Hunter and Thief use none of it. All three resolve into *walk at the thing
I want*, all three are answered by *aim and hold the trigger*, and not one of them costs
the player anything to look at. Attention is the only resource in this game that is
currently free.

The Cloaker is the roster row that prices it
(`../../Brainstorms/gatekeepers-and-waves.md`: takes **information**, refuses **being
seen**). Holding one still costs the arc of the room you were covering; letting it walk
costs the door. Neither answer dominates, and the second player does not delete the
problem — two Boarders cover two arcs, not four.

**The body is already bought and is the last one free.** `M88` spent three of the pack's
four robots (Default→UtilityBot, Hunter→Hunter, Thief→Exterminator) and left
`SK_Tactical` unassigned against `M80`'s boss/elite ruling. `M80` shipped 2026-08-20 with
its elite as a high-HP Thief on the Exterminator body, so nothing is waiting on
`SK_Tactical` and `../../Animation-Inventory.md`'s "still free" line resolves here.

Leverage tier (`../../Working-Rules.md`): **#5, rules of the system** — a new rule about
when a Gatekeeper may target anything at all. The single new number (45 HP) is #12 and is
derived rather than tuned: two MK-1 T0 shots, where a Default is one
(`../../Design/4-Data.md`).

**This slice is not blocked on `M63`'s door-time log.** That measurement decides stat
lines for kinds that break the Hatch by dwelling at it; the Cloaker's damage numbers are
copied from Default unchanged, so it writes no number the door-time answer could move.
Its one new number is read off the MK-1's shot table, which exists.

## Kill criterion

Two triggers, either alone, **by the second logged five-Wave Run containing Cloakers, or
2026-08-31, whichever comes first.**

1. **If, across that Run, the player never once turns to hold a Cloaker still and never
   once deliberately looks away from one**, the gaze gate produced no decision — it is a
   Default in a costume. The kind is **recut to a plain stalker**, not extended and not
   retuned.
2. **If, in a two-seat Run, the pair covers two arcs and no Cloaker ever moves at all**,
   the kind is deleted by a second player, and the gaze rule is not a foundation for
   anything. Recut before any later kind builds on it.

Neither trigger is feel. Both are "did anyone's behaviour change", which is what does not
improve with polish.

**Pre-mortem, unsoftened.** Three ways this ships broken. (a) The cone is wide enough that
a Cloaker is frozen whenever it is on screen, so the player never sees one move and learns
nothing — the tell is the band at the screen edge, and if that band is empty the kind is
invisible in the sense that matters. (b) A Wave of twenty bodies means somebody is always
looking somewhere, every Cloaker is frozen for the whole Wave, and it becomes free
salvage. (c) The cloak reads as a rendering fault rather than as an enemy, which is what
the reveal-on-hit and the lit seams exist to prevent. (b) is the one to watch first, and
the Wave counts are deliberately small because of it.

## Acceptance criteria

1. **A fourth kind exists.** `GatekeeperKind.Cloaker = 3` in `Sim/Waves/GatekeeperKind.cs`,
   and the Cloaker row in `../../Brainstorms/gatekeepers-and-waves.md` reads `in-build`.
   No other row's `state` moves.
2. **The watch rule is in code and is the whole kind.** While any live player has the
   Cloaker inside a **30° half-angle cone** of their planar forward, within
   **`watchRange` (26 m)**, with clear line of sight, both `MayTargetHatch` and
   `MayTargetPlayer` are false and the agent holds position. Outside all three conditions
   it targets exactly as a Default does — the Hatch on Hatch-bank Waves, the player when
   provoked, when near, or when there is no Hatch in the fight.
3. **A player can produce the effect on purpose.** In a Run, centring a Cloaker at lane
   range stops it within **0.25 s**, and turning away resumes it within **0.25 s**. It
   does not strobe when held at the edge of the cone.
4. **The cloak is two materials, not a shader trick.** Alive and unobserved, the body
   renders on `Packs/Gatekeepers/Cloaker_Cloaked.mat`; observed, on
   `Packs/Gatekeepers/Cloaker.mat`. **Being damaged forces the solid material for 1.0 s
   regardless of gaze**, so a blind hit that connects tells the player where it is.
   Neither material is a mutation of a vendor asset (`Tools/lint-vendor/run.sh` stays
   green).
5. **The body is `SK_Tactical` and nothing else moves.** The Cloaker wrapper's
   `SkinnedMeshRenderer.sharedMesh` is owned by
   `FaxLab3D/SciFiRobotCharactersPack/Models/SK_Tactical.fbx`. Root scale 1.0 and agent
   radius 0.4 / height 2 / baseOffset 1 — **identical to Default**, so no navmesh
   assumption changes. Default, Hunter and Thief keep their meshes, scales and footprints
   unchanged.
6. **One new number.** `WaveDirector.StatsFor(Cloaker)`: hp **45**, player damage **6**,
   Hatch damage **4**, patrol **3.2**, chase **5.6**, and the shared 0.7 s swing clock.
   Everything except hp is Default's value read from the same constants Default reads.
   **`GatekeeperStats` gains no field** — the refusal is a *state*, not a constant, so it
   cannot be a pushed flag and lives in `CloakerGatekeeper`.
7. **Composition is authored, small, and does not touch the dial.**
   `CloakerPerWave = { 0, 0, 1, 2, 3 }` and a new `WaveSpec.cloakerCount`. The four counts
   still sum to `enemyCount`, the dial's surplus is still entirely Default, and no
   existing per-Wave count moves.
8. **Co-op sees the same body.** Guests see a Cloaker cloaked exactly when the host does,
   through **one `NetworkVariable<bool>` on `NetEnemy`**, server-write, everyone-read. No
   new RPC. Clone AI and NavMeshAgent stay disabled as today.
9. **Ram is Thief in code** (`M63` criterion 5, moved here): `GatekeeperKind.Ram`,
   `WaveDirector.RamPerWave`, `WaveSpec.ramCount`, `Game/Enemies/RamGatekeeper.cs`,
   prefab paths, tests and doc references. **Its own commit, first, behaviour-preserving**
   — a Run before and after differs only in the name. Persisted save keys are checked and
   either migrated or left alone deliberately, stated either way in the Changelog.
10. **The Compendium ships only in-build pages** (`M63` criteria 8 and 9, moved here).
    `CompendiumCopy` reads an `in_build` flag from the page's opening HTML comment;
    **missing means true**; a page marked false is not written to StreamingAssets and is
    not listed by `CompendiumCatalog`.
11. **The Cloaker has a player page.** `Docs/Play/Gatekeeper-Cloaker.md` exists with
    `in_build: true`, sits in the Garrison volume, and states the watch rule in Game voice
    without numbers. `CompendiumFormTests` and `Tools/lint-language/run.sh` green.
12. **The data doc regenerates to four rows.** `Tools/docgen` emits the fourth row into
    `../../Design/4-Data.md`'s `gatekeepers` block, and the hand-written prose around it
    (the "three kinds" heading and the per-kind arithmetic list) is updated in the same
    edit. `--check` clean.
13. **Nothing else grows.** No new counter and no accumulator (`../../Decisions.md`) — in
    particular **the HUD live count is not masked** (see Implementation, rejected). No new
    menu row (`../../Architecture/README.md`). Both suites green, same test names plus the
    Cloaker cases.

## Implementation

**Built 2026-08-24, steps 2–13 in one sitting** (step 1 landed 2026-08-20). Everything
below exists in the tree; the deviations from the written plan are the "Premise fix
2026-08-24" blocks in place. What the build verified: offline EditMode 670/11 and the
full Unity bridge run EditMode 796/14 / PlayMode 81/1 — every failure the recorded
pre-existing set, none of them this slice's; `CloakerWatchTests` (PlayMode, real Bastion,
real agent) passed on the first bridge run; the scene-build console line reads
*"gatekeeper models retarget: all four avatars are Humanoid."*; `Tools/docgen --check`
clean; lint-arch / lint-vendor / lint-language add no new violations. The prefab was
built through `SceneBuilder.CreateEnemyPrefabs` (reflection one-shot, deleted after),
and the `cloakerPrefab` / `_cloakerPrefab` wires were authored as YAML into
`Bastion.unity`, `Bastion_Coop.unity`, `Range.unity` and both `NetSession.prefab`
copies. The pure half of the watch rule was lifted all the way into
`Sim/Waves/CloakerWatch.cs` (stronger than the plan's "a static"): the offline harness
runs its tests, at the cost of a `Vector3` shim in `Tools/verify/Shims.cs` — the
sanctioned extension. Still owed: Zo's playthrough (criterion 3's felt 0.25 s, the
two-seat eyes-on of criterion 8) and the kill criterion's two logged Runs.

Ordered. The rename is step 1 and lands alone, exactly as `M63` requires, so no file in
this slice is ever authored under the old name.

1. **Change — the rename, on its own commit.** `Ram` → `Thief` in
   `Sim/Waves/GatekeeperKind.cs`, `Sim/Waves/WaveDirector.cs` (`RamPerWave`),
   `Sim/Waves/WaveSpec.cs` (`ramCount`), `Sim/Run/MasterControl.cs`,
   `Sim/Run/DecoyState.cs`, `Game/Enemies/RamGatekeeper.cs` → `ThiefGatekeeper.cs`
   (`git mv`, with its `.meta`, so the component guid survives),
   `Game/Enemies/WaveSpawner*.cs`, `Game/Combat/Damageable.cs`,
   `Game/Player/PlayerAbility.cs`, `Editor/Scenes/SceneBuilder*.cs`, the prefab
   `Assets/Prefabs/RamGatekeeper.prefab` → `ThiefGatekeeper.prefab`, and the nine test
   files that name it. Afterwards `grep -rn "\bRam\b" Assets/ Docs/` returns only
   deliberate historical references — shipped milestone files are a record and keep their
   wording.

   **Landed 2026-08-20.** Two things the plan did not say, both forced by the tree:

   - **Two serialized fields carry `[FormerlySerializedAs]` rather than a scene edit.**
     `WaveSpawner.ramPrefab` → `thiefPrefab` and `NetSession._ramPrefab` →
     `_thiefPrefab` are wired in `Bastion.unity` and in both `NetSession.prefab` copies.
     The editor was open with Zo's work in it, so rewriting those keys in YAML risked
     being clobbered on the next save — and a lost binding is silent, the spawner just
     falls back to `enemyPrefab`. The attribute binds both spellings, so the key migrates
     on whatever save happens naturally. `Ram` therefore survives in three asset files by
     design, which is the one exception to the grep above.
   - **It is not on its own commit.** Ten of the files the rename touches were already
     dirty with Zo's in-progress `M80`/`M91`/`M93` work, so staging the rename alone is
     not possible without committing that too. The edit is still one mechanical pass and
     nothing else; the isolation the criterion asks for is Zo's to take when the tree is
     theirs to commit.
2. **Change** `Sim/Waves/GatekeeperKind.cs` — add `Cloaker = 3`, with its refusal stated
   in the summary: *advances only while unobserved; a Boarder's gaze is the refusal.*
3. **Change** `Sim/Waves/WaveDirector.cs` — `CloakerPerWave = { 0, 0, 1, 2, 3 }` beside
   the other two tables; a `case GatekeeperKind.Cloaker` in `StatsFor` reading Default's
   own constants for everything but hp (`45f`, written once with the two-shot derivation
   in its doc comment); `NextWave` fills `spec.cloakerCount` with the same `Row` +
   `Math.Min` clamp shape as `ramCount`, and `defaultCount` becomes
   `enemyCount - hunterCount - thiefCount - cloakerCount`.
4. **Change** `Sim/Waves/WaveSpec.cs` — `public int cloakerCount;`, and the summary on
   `enemyCount` updated to name four terms.
5. **Change** `Game/Enemies/EnemyGatekeeper.cs` — **one** base addition:
   `protected virtual void KindTick(float dt) { }`, called once from the existing
   `Update()` ahead of the retarget tick. This is the seam the Cloaker needs, and the
   alternative — a subclass declaring its own `Update` — silently hides the base's, which
   would stop every gatekeeper in the game.
6. **Create** `Game/Enemies/CloakerGatekeeper.cs`:

   ```csharp
   public sealed class CloakerGatekeeper : EnemyGatekeeper
   {
       [SerializeField] float watchRange = 26f;      // mirrors EnemyGatekeeper.noticeRange
       [SerializeField] float watchHalfAngle = 30f;
       const float WatchRefresh = 0.1f;    // gaze poll: the 0.25s criterion needs better than the 0.75s retarget
       const float ReleaseAngleSlack = 6f; // hysteresis: held at <=30 deg, released only past 36
       const float ReleaseRangeSlack = 2f; // and only past 28 m, when range is what freed it
       const float RevealOnHitSeconds = 1f;

       public override GatekeeperKind Kind => GatekeeperKind.Cloaker;
       protected override bool MayTargetHatch => !_observed && base.MayTargetHatch;
       protected override bool MayTargetPlayer => !_observed && base.MayTargetPlayer;
       // NOT gated on _observed: being shot while held still latches the grudge, so a
       // Cloaker you wounded comes for you the moment you look away — which is what
       // "targets exactly as a Default" requires. Retarget still refuses to chase while
       // observed, because MayTargetPlayer is false; the latch just survives the freeze.
       protected override bool CanBeProvoked => !ignoresPlayer;
       protected override void KindTick(float dt) { ... }
       bool AnyoneWatching() { ... }      // iterates PlayerRegistry.All; allocates nothing
   }
   ```

   **Premise fix 2026-08-24 — the hysteresis is angular, not temporal.** The first cut
   held "observed" for 0.35 s after the gaze left, which contradicts this file's own
   criterion 3: a deliberate 45° turn-away would resume the walk at ~0.45 s, not 0.25.
   Edge strobing is angle noise, so the guard is a Schmitt band on the angle (enter at
   30°, release past 36°) and on range (enter at 26 m, release past 28 m), polled every
   0.1 s. A deliberate turn crosses the whole band in one poll — both directions land
   inside 0.25 s — while mouse jitter of a few degrees can never cross 6°. LOS stays
   binary and unslacked.

   `AnyoneWatching` is the whole rule: for each live player, planar delta to the eye
   point, `Vector3.Angle` against that player's `transform.forward` — the FPS root's yaw
   **is** the look yaw, pitch lives on the camera child (`FirstPersonController` line 218)
   — then range against `watchRange`, then the same `Physics.Raycast` shape
   `CanSeePlayer()` already uses. Remote seats work unchanged: `NetPlayerAvatar` is an
   owner-authoritative `NetworkTransform`, so a guest's yaw is on the host. When
   `_observed` flips, `KindTick` calls `Retarget()` in that frame instead of waiting up to
   0.75 s, and pushes the state to the presentation and to `NetEnemy`. It subscribes to
   `Damageable.Damaged` in `OnEnable`/`OnDisable` (never in `Awake` — a subclass `Awake`
   would hide the base's and unhook the whole kind) to hold the solid material for
   `RevealOnHitSeconds`.

   **Premise fix 2026-08-24 — two base changes ride along with the seam, both one-liners.**
   `Retarget()` becomes `protected` (it is `private`; the flip-frame retarget cannot reach
   it otherwise), and the decoy branch in `Retarget()` gains a `MayTargetPlayer &&` guard —
   without it a frozen Cloaker walks to a PHANTOM decoy, because that branch bypasses all
   three targeting virtuals. The guard is behaviour-preserving for every existing kind:
   `DecoyState.ShouldPull` already refuses Thieves by kind, and Default and Hunter answer
   `MayTargetPlayer` true always.

   **30° is chosen against the screen, not by taste.** Hip FOV is 60° vertical
   (`Sim/Run/WeaponView.HipFov`), which at 16:9 is ≈91° horizontal — ≈45.7° each side. A
   30° cone therefore leaves a band at each screen edge where a Cloaker still walks **on
   screen**, and that band is the entire tell. Widening the cone to the screen edge
   deletes it.
7. **Create** `Game/Enemies/CloakerPresentation.cs` — `SetCloaked(bool)`, swapping the
   visual child's `SkinnedMeshRenderer.sharedMaterial` between the two assets. **A
   separate component on purpose:** `NetEnemy` disables `EnemyGatekeeper` on clones, so
   anything living on the AI component is dead on a guest's screen. Assign
   `sharedMaterial` (pointing at a different asset is free); never touch `.material`,
   which leaks a material instance per spawn, and never mutate either asset at runtime.
8. **Change** `Game/Net/NetEnemy.cs` — `public NetworkVariable<bool> Cloaked` (server
   write, everyone read), written by the host when `_observed` flips, with an
   `OnValueChanged` on clones calling `CloakerPresentation.SetCloaked`. The flag is global
   rather than per-viewer because the freeze is global: one player looking stops it for
   everybody, so there is nothing per-viewer to replicate.

   **Premise fix 2026-08-24 — the network prefab registration this step forgot.**
   `NetSession` carries one serialized field per enemy prefab and registers each with NGO
   (`EnsurePrefabsRegistered`); a kind missing there cannot replicate. So this slice also
   adds `_cloakerPrefab` to `NetSession`, registers it, and wires it — in **both**
   `NetSession.prefab` copies (`Prefabs/Net`, `Resources/Net`) and in the scene-embedded
   NetSessions of `Bastion.unity`, `Bastion_Coop.unity` and `Range.unity`, beside the
   `WaveSpawner.cloakerPrefab` wire in the two Bastion scenes.

   **And the defect the check found:** all three scenes were re-saved after the Thief
   rename with `thiefPrefab`/`_thiefPrefab` written as **None** — the old `ramPrefab`
   key is gone and nothing bound the new one, so every Thief has been spawning on the
   `enemyPrefab` fallback (Default body, Default colour, warning suppressed after the
   first), and `ThiefGatekeeper.prefab` is not network-registered from those scenes. The
   two `NetSession.prefab` assets still carry the wired old key, which is how it stayed
   invisible. Repaired here by YAML edit, its own commit, before any Cloaker wiring.
9. **Change** `Game/Enemies/WaveSpawner.cs` / `.Groups.cs` — a `cloakerPrefab` field
   beside the other two, `PrefabFor` / `KindName` cases, and
   `AddKind(emit, GatekeeperKind.Cloaker, spec.cloakerCount)` in `BuildRoster`. The roster
   shuffle picks it up with no further change. **Premise fix 2026-08-24:** the group tally
   line does NOT — `EmitGroup` allocates `var tally = new int[3]` and indexes it by kind,
   so `Cloaker = 3` is an `IndexOutOfRangeException` on the first mixed group. The array
   goes to 4 in the same edit.
10. **Change** `Editor/Scenes/SceneBuilder.cs` — a `Cloaker` palette entry, a cold teal
    `(0.20, 0.85, 0.78)`: distinct from Default red, Hunter amber and Thief violet, and
    from the hull's own violet wall and teal rib at a glance.
    **Change** `Editor/Scenes/SceneBuilder.Enemies.cs` — `CloakerFbx` / `PackCloaker`
    (`Prefabs/Tactical_Base.prefab`) constants, `CloakerPrefabPath`, `CloakerWrapperPath`,
    `EnemyPrefabs.Cloaker`, a fourth
    `CreateGatekeeperPrefab<CloakerGatekeeper>(..., 1f, dress)`, and the fourth
    `EnsureWrapper` / `EnsureWrapperEmission` / `VisualChildName` / `WrapperPath` case.
    `GatekeeperModelsRetarget` gains `SK_Tactical`, so a fourth non-Humanoid avatar fails
    the build loudly instead of shipping a T-pose.
11. **Create** `Packs/Gatekeepers/Cloaker.mat` — a copy of the vendor `M_Tactical.mat`,
    the same way the other three kind materials are copies, so paint mask, wear mask and
    the emissive map keep working and `EnsureWrapperEmission` has its glow regions.
    **Create** `Packs/Gatekeepers/Cloaker_Cloaked.mat` — Built-In `Standard`, Rendering
    Mode **Fade**, albedo alpha **0.12**, emission on with the same teal and the pack's
    `SK_Tactical` emissive map, so the seams stay lit while the body goes. Precedent and
    shape: `Assets/Materials/Mat_CanopyGlass.mat` (Standard/Fade, 2026-08-20). The vendor
    Shader Graph is not opened, edited or retargeted — that is what `lint-vendor` guards,
    and `M88` records what it cost the last time.
12. **Change** `Editor/Tools/CompendiumCopy.cs` — read `in_build` from the opening HTML
    comment before `StripForPlayer`; skip a page marked false; missing means true. The
    parser lives once, in `CompendiumCatalog.IsInBuild`, so the copier, the reader and the
    tests cannot drift.
    **Change** `Game/Ui/CompendiumCatalog.cs` — `IsInBuild`, and `"Gatekeeper-Cloaker"`
    joins the Garrison stems. **Change** `Game/Ui/CompendiumView.cs` — the nav loop skips
    a stem whose page is not in-build; that is the "filters at read time" of criterion 10
    (`Volumes` itself stays the mirror of `Docs/Play/README.md` and lists every page).
    **Premise fix 2026-08-24:** `CompendiumFormTests.StreamingAssets_StemSetMatchesPlay`
    asserts Play and StreamingAssets hold the SAME stem set, so the first page ever marked
    `in_build: false` fails it. The test becomes flag-aware in the same edit: a false page
    must be absent from StreamingAssets, an in-build page present.
    **Create** `Docs/Play/Gatekeeper-Cloaker.md` — the shape of `Gatekeeper-Hunter.md`:
    Game / Reference / See also / Gaps, `in_build: true`, no numbers (they live in the
    Field Manual). **Change** `Docs/Play/Gatekeepers.md` and the three existing kind pages'
    See-also lines, **and `Docs/Play/README.md`'s Garrison paragraph** — the catalog
    mirrors the README (`CompendiumCatalog.Volumes` doc comment), so a page added to one
    and not the other is unreachable or a test failure.
13. **Change** docs: `../../Brainstorms/gatekeepers-and-waves.md` (Cloaker row to
    `in-build`; the Thief row's "rename in `M63`" note resolved),
    `../../Animation-Inventory.md` (`SK_Tactical` is spent), `../../Design/4-Data.md` via
    `Tools/docgen`, `../../Roadmap.md` row and **Current** line, `../../Changelog.md`.
    **Premise fix 2026-08-24:** docgen's `AppendGkRow` derives the `refuses` column from
    the two pushed flags, and the Cloaker sets neither — its row would print `—`, exactly
    the "Default in a costume" this file exists to avoid. The emitter states the Cloaker's
    refusal explicitly: being seen, a state in `CloakerGatekeeper`, not a pushed flag.

**Rejected: masking the HUD live count while a Cloaker is cloaked.** It is the truest
reading of "takes information", and it costs more than it returns: `AliveCount` *is* the
wave-cleared handshake (`WaveSpawner.cs`, `if (AliveCount > 0) return;`), so masking it
either breaks the clear or needs a second parallel count — a new counter, which
`../../Decisions.md` forbids. The information a Cloaker takes is spatial, and the cloak
takes it.

**Rejected: making the Cloaker invulnerable while watched.** The roster's property list
allows "invulnerable until a stated condition", and here it produces one dominant answer
in each direction — never look at it, or never be able to kill it.

**Rejected: a static `ignoresGaze`-style flag on `GatekeeperStats`.** The refusal changes
several times a second; a pushed constant cannot express it, and adding a field only one
kind's Game-layer code reads is how that struct stops being the complete profile it claims
to be.

**Rejected: new locomotion or a cloak animation.** The pack ships six static poses and
nothing else; the shared `Gatekeeper.controller` clips already retarget onto ARP robots
(`M88`). A Cloaker holding still is the Idle state, which exists.

**Rejected: giving the Cloaker its own damage numbers.** They are copies of Default's,
deliberately — see *Why it matters* on the door-time log.

## Supporting work

- **Driven by** the player's existing mouse look — no new input, here.
- **Shown by** the freeze itself, the two-material cloak, and the teal seams — here. The
  Compendium page (criterion 11) is the words; the behaviour is meant to be learnable
  without reading them.
- **Reads back as** the kill-log line naming `Cloaker` (the existing `KindName` path) and
  the group tally line "1 Cloaker" on spawn — here. **Audio identity is `M66`** and does
  not ship with this: if `M66` slips, the Cloaker still has two visual channels and no
  sound of its own, which is where the other three kinds already stand.

## Verification

1. Criterion 9 (rename) — `grep -rn "\bRam\b" Assets/ Docs/` returns only shipped-file
   history; play one Run before and after and diff the session log. Only the name moves.
2. Criteria 1, 6, 7 — EditMode `GatekeeperVariantsTests`: a Cloaker case pinning the stat
   line, and a `WaveSpec` case asserting the four counts sum to `enemyCount` at every Wave
   index and dial. `SiphonAndWaveTests` still shows the dial's surplus landing only in
   `defaultCount`.
3. Criterion 2 — EditMode over the pure part of the rule (the angle/range arithmetic
   lifted to a static, so it needs no scene), plus PlayMode `CloakerWatchTests`: place a
   Cloaker 26 m ahead, rotate the player onto it, assert the agent `isStopped` with no
   destination; rotate away, assert it re-paths.
4. Criteria 4, 5 — from the prefab text, no editor:

   ```bash
   grep -A1 m_Mesh: "Assets/Packs/Gatekeepers/Cloaker.prefab" | grep -oE 'guid: [0-9a-f]{32}' | sort -u
   ```

   resolved against `SK_Tactical.fbx.meta`; then the scene-build console line
   `[SceneBuilder] gatekeeper models retarget: all four avatars are Humanoid.`, with no
   `pack prefab missing` or `gatekeeper material missing` error.
5. Criterion 8 — two-seat loopback (F10/F11): both screens show the same Cloaker cloaked
   and solid at the same moments, and the guest console carries no clone-AI warning.
6. Criteria 10, 11 — `CompendiumFormTests`; `Tools/lint-language/run.sh`; a page marked
   `in_build: false` is absent from `Assets/StreamingAssets/Compendium` after
   `GatekeepersProtocol/Build/Copy Compendium Pages`; Esc → Compendium → Garrison →
   Cloaker opens.
7. Criterion 12 — `Tools/docgen` `--check` clean (offline: the dotnet replication), four
   rows in the `gatekeepers` block.
8. Criterion 13 — both suites via `gkb_verify_request.txt`; `Tools/lint-arch/run.sh` and
   `Tools/lint-vendor/run.sh` green. The known pre-existing failures are the 2026-08-19
   set and are not this slice's.
9. **Playthrough path** — from Play: EMBARK → `T` a Field Wave → reach Wave 3, where
   exactly one Cloaker arrives. Centre it: it stops and goes solid. Turn 45°: it walks.
   Shoot it blind: it flashes solid for a second, wherever it is. That is criteria 3 and 4
   and the first half of the kill criterion in one sitting.

## Watch

- **`NetEnemy` disables `EnemyGatekeeper` on clones.** Any presentation living on the AI
  component is dead on a guest's screen; that is why `CloakerPresentation` is separate.
- **`Damageable` caches its flash colour from `sharedMaterial` in `Awake`** and drives the
  flash through a `MaterialPropertyBlock`, which writes `_Color` including its alpha. The
  reveal-on-hit makes that moot — the solid material is the one assigned during the flash
  — and `ClearColor` clears the block rather than restoring a value, so no opacity can
  stick. Do not "fix" this by writing the material directly.
- **`_hasTint` is false on the vendor Shader Graph** (`_BaseColor` is a texture there), so
  the red hit flash may not land on a robot at all — already true for the other three
  kinds. The Cloaker's reveal-on-hit is its own confirmation and does not depend on it.
- **`Assets/FaxLab3D/` is a vendor dump with only its materials tracked** (`M88`). A
  re-import silently overwrites; `lint-vendor` detects, it does not prevent.
- **`SceneBuilder` saves scenes when it runs**, and polls sentinel files at the repo root
  once a second. A Build All is a scene write, not a preview.
- **Prefab freeze:** serialize the wrapper and material references; do not rely on a code
  default to find them at runtime.
- **ADS narrows the camera FOV** (`WeaponView.ZoomFov`) while the watch cone stays 30°, so
  a zoomed player freezes Cloakers that are off their own screen. Harmless direction — the
  failure that matters is the opposite one — and it is why the cone is not derived from the
  live FOV.

## Do not build

| Not building | Why not |
|---|---|
| The other eight named kinds | Each is its own slice |
| The Thief's salvage rule | Recorded in `M63`, built in the Thief's own slice |
| Ranged attacks or projectiles | Both Shooter rows need them; no kind here shoots |
| HUD count masking | Rejected above — it needs a second counter |
| A cloak shader or a distortion effect | Two materials; the project is Built-In and the vendor graph is gated |
| Audio identity for the Cloaker | `M66` |
| `M63`'s door-time log, roster prose, properties or draw rule | Stays in `M63` |

## Depends on / Unblocks

Depends on the FaxLab3D pack import that `M87` and `M88` already required, and on nothing
in `M63` — the two criteria it needed came across into this file.

`M63` keeps the twelve-kind roster record, the refusal rewording, the properties and draw
rule, `M53`'s Ram exception and the door-time log; it loses its criteria 5, 8 and 9 to
this file.

Unblocks the first Gatekeeper that uses the Bastion's shape rather than its distances, and
gives `M66` a fourth identity to sound. Whether Alarmer and Source, or Thief and Reclaimer,
read as distinct pairs is still owed to play and is untouched here.
