using System;
using System.Collections.Generic;
using System.IO;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Emits kit segment instances from a bastion sketch file. Trigger via
    /// gkb_bastion_sketch_request.txt at the project root: first non-empty line is the sketch
    /// path (empty file = default sketch); optional second line is the scene path to emit into
    /// (empty / omitted = Assets/Scenes/Bastion.unity). Closets only:
    /// gkb_bastion_spawn_rooms_request.txt — no room emit.</summary>
    [InitializeOnLoad]
    public static class BastionSketchEmitter
    {
        const string KitDir = "Assets/Prefabs/Bastion/Kit";
        const string DefaultSketchRelative = "Tools/bastion-sketch/bastion-defense-layout.txt";
        const string DefaultSceneRelative = "Assets/Scenes/Bastion.unity";
        const string RootName = "BastionKit";
        const string GeneratedName = "Generated";
        const string AuthoredName = "Authored";

        static readonly Dictionary<BastionBlockKind, string> PrefabNames = new Dictionary<BastionBlockKind, string>
        {
            { BastionBlockKind.Quarter, "Quarter" },
            { BastionBlockKind.Half, "Half" },
            { BastionBlockKind.Full, "Full" },
            { BastionBlockKind.OpenArea, "OpenArea" },
            { BastionBlockKind.ControlRoom, "ControlRoom" },
        };

        static double _nextSentinelPoll;
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string SentinelPath => Path.Combine(Root, "gkb_bastion_sketch_request.txt");
        static string SpawnRoomsSentinelPath => Path.Combine(Root, "gkb_bastion_spawn_rooms_request.txt");
        static string SpawnRoomsReportPath => Path.Combine(Root, "gkb_bastion_spawn_rooms_report.json");
        static string ControlRebuildSentinelPath => Path.Combine(Root, "gkb_bastion_control_rebuild_request.txt");
        static string SettleSentinelPath => Path.Combine(Root, "gkb_bastion_settle_request.txt");
        static string DefaultSketchPath => Path.Combine(Root, DefaultSketchRelative);

        static BastionSketchEmitter()
        {
            EditorApplication.update += PollSentinel;
        }

        static void PollSentinel()
        {
            if (EditorApplication.timeSinceStartup < _nextSentinelPoll) return;
            _nextSentinelPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            if (File.Exists(SpawnRoomsSentinelPath))
            {
                try { File.Delete(SpawnRoomsSentinelPath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[BastionSketchEmitter] spawn-rooms sentinel: " + e.Message);
                    return;
                }

                PlaceAuthoredSpawnRoomsFromDisk();
                return;
            }

            if (File.Exists(ControlRebuildSentinelPath))
            {
                string rebuildText = "";
                try
                {
                    rebuildText = File.ReadAllText(ControlRebuildSentinelPath);
                    File.Delete(ControlRebuildSentinelPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BastionSketchEmitter] control-rebuild sentinel: " + e.Message);
                    return;
                }

                RebuildControlRoomsFromSentinel(rebuildText);
                return;
            }

            if (File.Exists(SettleSentinelPath))
            {
                string settleText = "";
                try
                {
                    settleText = File.ReadAllText(SettleSentinelPath);
                    File.Delete(SettleSentinelPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BastionSketchEmitter] settle sentinel: " + e.Message);
                    return;
                }

                SettleSceneFromSentinel(settleText);
                return;
            }

            if (!File.Exists(SentinelPath)) return;

            string sketchPath = DefaultSketchPath;
            string scenePath = DefaultSceneRelative;
            try
            {
                string sentinelText = File.ReadAllText(SentinelPath);
                File.Delete(SentinelPath);
                ParseSketchSentinel(sentinelText, ref sketchPath, ref scenePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BastionSketchEmitter] sentinel: " + e.Message);
                return;
            }

            EmitFromSketchFile(sketchPath, scenePath);
        }

        /// <summary>Opens Bastion from disk, places missing closets, saves. Does not emit
        /// rooms. Report is <c>gkb_bastion_spawn_rooms_report.json</c>.</summary>
        static void PlaceAuthoredSpawnRoomsFromDisk()
        {
            try
            {
                Scene bastion = EditorSceneManager.OpenScene(DefaultSceneRelative, OpenSceneMode.Single);
                int placed = EnsureAuthoredSpawnRooms();
                EditorSceneManager.MarkSceneDirty(bastion);
                EditorSceneManager.SaveScene(bastion);
                File.WriteAllText(SpawnRoomsReportPath, "{\"placed\":" + placed + "}\n");
            }
            catch (Exception e)
            {
                try
                {
                    File.WriteAllText(SpawnRoomsReportPath,
                        "{\"error\":\"" + e.GetType().Name + "\"}\n");
                }
                catch
                {
                    // report is best-effort
                }

                Debug.LogError("[BastionSketchEmitter] PlaceAuthoredSpawnRoomsFromDisk: " + e);
            }
        }

        /// <summary>One ControlRoom line per request: scene path, optional sketch path on the
        /// next line (positions). Unparents from Authored so Rebuild can run, stamps the new
        /// kit footprint, unpacks leftover pack links, then settles back under Authored.</summary>
        static void RebuildControlRoomsFromSentinel(string text)
        {
            string scenePath = DefaultSceneRelative;
            string sketchPath = "";
            if (!string.IsNullOrEmpty(text))
            {
                string[] lines = text.Split('\n');
                int filled = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0)
                        continue;
                    if (filled == 0)
                        scenePath = NormalizeScenePath(line);
                    else if (filled == 1)
                        sketchPath = Path.IsPathRooted(line) ? line : Path.Combine(Root, line);
                    filled++;
                }
            }

            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject layoutRoot = FindOrCreateRoot();
                FindOrCreateSubtrees(layoutRoot, out GameObject generated, out GameObject authored);

                var sketchCenters = new Dictionary<string, Vector3>();
                if (!string.IsNullOrEmpty(sketchPath) && File.Exists(sketchPath))
                {
                    BastionSketchParseResult parsed = BastionSketchParser.Parse(File.ReadAllText(sketchPath));
                    if (parsed.Success)
                    {
                        for (int i = 0; i < parsed.Sketch.Rooms.Count; i++)
                        {
                            BastionSketchRoom room = parsed.Sketch.Rooms[i];
                            if (room.Kind != BastionBlockKind.ControlRoom)
                                continue;
                            sketchCenters[room.Id] = new Vector3(room.CenterX, 0f, room.CenterZ);
                        }
                    }
                    else
                        Debug.LogError("[BastionSketchEmitter] Control rebuild sketch failed: " + parsed.Error);
                }

                BastionSegment[] segments = UnityEngine.Object.FindObjectsByType<BastionSegment>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                int rebuilt = 0;
                BastionSegment.SuppressValidateRebuild = true;
                try
                {
                    for (int i = 0; i < segments.Length; i++)
                    {
                        BastionSegment segment = segments[i];
                        if (segment == null || segment.BlockKind != BastionBlockKind.ControlRoom)
                            continue;
                        if (!segment.CanMutateHierarchy())
                            continue;

                        Transform t = segment.transform;
                        t.SetParent(generated.transform, true);
                        if (sketchCenters.TryGetValue(t.name, out Vector3 center))
                            t.position = new Vector3(center.x, t.position.y, center.z);

                        segment.Rebuild();
                        UnpackGeom(t);
                        t.SetParent(authored.transform, true);
                        rebuilt++;
                    }
                }
                finally
                {
                    BastionSegment.SuppressValidateRebuild = false;
                }

                UnpackGeom(generated.transform);
                UnpackGeom(authored.transform);
                int settled = SettleGeneratedIntoAuthored(generated, authored);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[BastionSketchEmitter] ControlRoom rebuild scene=" + scene.path
                    + " rebuilt=" + rebuilt + " settled=" + settled);
            }
            catch (Exception e)
            {
                Debug.LogError("[BastionSketchEmitter] ControlRoom rebuild: " + e);
            }
        }

        static void SettleSceneFromSentinel(string text)
        {
            string scenePath = DefaultSceneRelative;
            if (!string.IsNullOrEmpty(text))
            {
                string[] lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0)
                        continue;
                    scenePath = NormalizeScenePath(line);
                    break;
                }
            }

            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject layoutRoot = FindOrCreateRoot();
                FindOrCreateSubtrees(layoutRoot, out GameObject generated, out GameObject authored);
                UnpackGeom(layoutRoot.transform);
                int settled = SettleGeneratedIntoAuthored(generated, authored);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[BastionSketchEmitter] Settled " + settled
                    + " room(s) into Authored in " + scene.path);
            }
            catch (Exception e)
            {
                Debug.LogError("[BastionSketchEmitter] Settle: " + e);
            }
        }

        /// <summary>Move every remaining Generated child under Authored. Rooms rebuild while
        /// they live in Generated; once they sit here they are unique scene geometry.
        /// Uses the subtree refs from the open emit, not GameObject.Find — BastionKit
        /// lives under Environment, and a missed Find left rooms in Generated so the
        /// next ClearChildren wiped hand stairs.</summary>
        static int SettleGeneratedIntoAuthored(GameObject generated, GameObject authored)
        {
            if (generated == null || authored == null)
                return 0;

            UnpackGeom(generated.transform);
            int moved = 0;
            for (int i = generated.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = generated.transform.GetChild(i);
                if (child == null)
                    continue;
                child.SetParent(authored.transform, true);
                moved++;
            }

            return moved;
        }

        static void UnpackGeom(Transform root)
        {
            if (root == null)
                return;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i].gameObject;
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    continue;
                PrefabUtility.UnpackPrefabInstance(
                    go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }

        static void CollectExistingMarks(
            Transform room,
            Dictionary<string, SpawnPoint> spawnByName,
            Dictionary<string, BastionTerminal> terminalById)
        {
            SpawnPoint[] spawns = room.GetComponentsInChildren<SpawnPoint>(true);
            for (int i = 0; i < spawns.Length; i++)
            {
                SpawnPoint sp = spawns[i];
                if (sp == null)
                    continue;
                string id = sp.name;
                if (id.StartsWith("Spawn_", StringComparison.Ordinal))
                    id = id.Substring("Spawn_".Length);
                if (id.Length > 0)
                    spawnByName[id] = sp;
            }

            BastionTerminal[] terminals = room.GetComponentsInChildren<BastionTerminal>(true);
            for (int i = 0; i < terminals.Length; i++)
            {
                BastionTerminal t = terminals[i];
                if (t == null || string.IsNullOrEmpty(t.TerminalId))
                    continue;
                terminalById[t.TerminalId] = t;
            }
        }

        public static void EmitFromSketchFile(string sketchPath)
        {
            EmitFromSketchFile(sketchPath, DefaultSceneRelative);
        }

        public static void EmitFromSketchFile(string sketchPath, string scenePath)
        {
            if (!File.Exists(sketchPath))
            {
                Debug.LogError("[BastionSketchEmitter] Sketch not found: " + sketchPath);
                return;
            }

            if (string.IsNullOrEmpty(scenePath))
                scenePath = DefaultSceneRelative;
            scenePath = NormalizeScenePath(scenePath);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            string text = File.ReadAllText(sketchPath);
            BastionSketchParseResult parsed = BastionSketchParser.Parse(text);
            if (!parsed.Success)
            {
                Debug.LogError("[BastionSketchEmitter] Parse failed: " + parsed.Error);
                return;
            }

            EmitSketch(parsed.Sketch, sketchPath, scene);
        }

        /// <summary>Places missing sketch spawn closets on the open Bastion scene without
        /// emitting rooms or rebuilding Authored geometry. Idempotent by <c>Spawn_*</c> name.
        /// Removes leftover centre <c>S_S*</c> markers that are not under a SpawnRoom.</summary>
        public static int EnsureAuthoredSpawnRooms()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != DefaultSceneRelative)
            {
                Debug.LogError("[BastionSketchEmitter] EnsureAuthoredSpawnRooms needs Bastion.unity open.");
                return 0;
            }

            if (!File.Exists(DefaultSketchPath))
            {
                Debug.LogError("[BastionSketchEmitter] Sketch not found: " + DefaultSketchPath);
                return 0;
            }

            BastionSketchParseResult parsed = BastionSketchParser.Parse(File.ReadAllText(DefaultSketchPath));
            if (!parsed.Success)
            {
                Debug.LogError("[BastionSketchEmitter] Parse failed: " + parsed.Error);
                return 0;
            }

            BastionSegment[] segments = UnityEngine.Object.FindObjectsByType<BastionSegment>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var spawnByName = new Dictionary<string, SpawnPoint>();
            int placed = 0;
            var game = UnityEngine.Object.FindAnyObjectByType<GameController>(
                FindObjectsInactive.Include);
            var terminals = new Dictionary<string, BastionTerminal>();

            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                for (int i = 0; i < parsed.Sketch.Rooms.Count; i++)
                {
                    BastionSketchRoom room = parsed.Sketch.Rooms[i];
                    BastionSegment segment = FindSegmentNamed(segments, room.Id);
                    if (segment == null)
                        continue;

                    for (int m = 0; m < room.Marks.Count; m++)
                    {
                        BastionSketchMark mark = room.Marks[m];
                        if (mark.Kind != BastionSketchMarkKind.Spawn)
                            continue;

                        Transform existing = segment.transform.Find("Spawn_" + mark.RefId);
                        if (existing != null)
                        {
                            var closet = existing.GetComponent<SpawnRoom>();
                            if (closet != null && closet.Spawn != null)
                            {
                                closet.SetMinSeats(mark.MinSeats);
                                spawnByName[mark.RefId] = closet.Spawn;
                            }
                            continue;
                        }

                        if (CreateMark(segment.transform, mark, spawnByName, terminals, game))
                            placed++;
                    }
                }

                if (spawnByName.Count == 0)
                {
                    Debug.LogError("[BastionSketchEmitter] EnsureAuthoredSpawnRooms found no closets.");
                }
                else
                {
                    RemoveCentreSpawnMarkers();
                    WireSealedSpawns(spawnByName);
                    WireControlSpawns(spawnByName);
                }
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }

            Debug.Log("[BastionSketchEmitter] EnsureAuthoredSpawnRooms placed=" + placed
                + " tracked=" + spawnByName.Count);
            return placed;
        }

        static BastionSegment FindSegmentNamed(BastionSegment[] segments, string id)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] != null && segments[i].gameObject.name == id)
                    return segments[i];
            }
            return null;
        }

        static void RemoveCentreSpawnMarkers()
        {
            SpawnPoint[] points = UnityEngine.Object.FindObjectsByType<SpawnPoint>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < points.Length; i++)
            {
                SpawnPoint sp = points[i];
                if (sp == null)
                    continue;
                if (sp.GetComponentInParent<SpawnRoom>() != null)
                    continue;
                UnityEngine.Object.DestroyImmediate(sp.gameObject);
            }
        }

        static void EmitSketch(BastionSketch sketch, string sketchPath, Scene scene)
        {
            BastionSegment.SuppressValidateRebuild = true;
            int roomCount = 0;
            int markerCount = 0;
            int doorMarkerCount = 0;
            var roomObjects = new Dictionary<string, GameObject>();
            GameObject generated = null;
            GameObject authored = null;

            try
            {
                GameObject layoutRoot = FindOrCreateRoot();
                FindOrCreateSubtrees(layoutRoot, out generated, out authored);
                MigrateLegacyChildren(layoutRoot, generated, authored);
                ClearChildren(generated);

                TryAddLayoutComponent(layoutRoot);

                var game = UnityEngine.Object.FindAnyObjectByType<GameController>(FindObjectsInactive.Include);
                if (game == null)
                    Debug.LogError("[BastionSketchEmitter] No GameController in scene — terminals emit unwired and Interact dead-ends.");

                var effectiveSides = BuildEffectiveSides(sketch);
                ApplyDoorSides(sketch, effectiveSides);

                var spawnByName = new Dictionary<string, SpawnPoint>();
                var terminalById = new Dictionary<string, BastionTerminal>();

                ApplySpawnSides(sketch, effectiveSides);

                for (int i = 0; i < sketch.Rooms.Count; i++)
                {
                    BastionSketchRoom room = sketch.Rooms[i];
                    Transform authoredRoom = authored.transform.Find(room.Id);
                    if (authoredRoom != null)
                    {
                        // Already unique scene geometry. Keep it; do not stamp a twin into
                        // Generated. Sketch centre (including storey Y) still wins so a kit
                        // retune can slide the room.
                        authoredRoom.position = new Vector3(
                            room.CenterX, room.CenterY, room.CenterZ);
                        roomObjects[room.Id] = authoredRoom.gameObject;
                        CollectExistingMarks(authoredRoom, spawnByName, terminalById);
                        StampSpawnMinSeats(room, spawnByName);
                        continue;
                    }

                    GameObject instance = InstantiateRoom(room);
                    if (instance == null)
                        continue;

                    instance.name = room.Id;
                    instance.transform.SetParent(generated.transform, false);
                    instance.transform.position = new Vector3(room.CenterX, room.CenterY, room.CenterZ);

                    // Room roots carry BastionSegment, so CleanupOrphanGeneratedGeom skips them.
                    if (instance.GetComponent<BastionGeneratedGeom>() == null)
                        instance.AddComponent<BastionGeneratedGeom>();

                    var segment = instance.GetComponent<BastionSegment>();
                    if (segment != null && effectiveSides.TryGetValue(room.Id, out SideSet sides))
                        ApplySides(segment, sides);

                    roomObjects[room.Id] = instance;
                    roomCount++;

                    for (int m = 0; m < room.Marks.Count; m++)
                    {
                        if (CreateMark(instance.transform, room.Marks[m], spawnByName, terminalById, game))
                            markerCount++;
                    }

                    StampSpawnMinSeats(room, spawnByName);
                }

                Material gateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Hatch.mat");
                for (int i = 0; i < sketch.Doors.Count; i++)
                {
                    BastionSketchDoor door = sketch.Doors[i];
                    if (!roomObjects.TryGetValue(door.RoomId, out GameObject roomGo))
                        continue;
                    if (roomGo.transform.Find("D_" + door.DoorId) != null)
                        continue;

                    BastionDoor component = CreateDoorMarker(roomGo.transform, door, gateMat);
                    doorMarkerCount++;

                    // BastionLayout's unlock graph walks terminal.UnlocksDoor, not the door's
                    // own required list — a terminal without this reference opens nothing.
                    if (component == null)
                        continue;
                    for (int t = 0; t < door.UnlockTerminalIds.Count; t++)
                    {
                        if (terminalById.TryGetValue(door.UnlockTerminalIds[t], out BastionTerminal terminal))
                            terminal.SetUnlocksDoor(component);
                    }
                }

                // Every open passage gets an unlocked auto door: proximity slides the leaf,
                // no terminal gates it. Locked gates above keep the gate material; these fall
                // back to the segment wall material so they read as doors, not seals.
                List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
                for (int i = 0; i < passages.Count; i++)
                {
                    BastionSketchPassage passage = passages[i];
                    if (!roomObjects.TryGetValue(passage.RoomId, out GameObject roomGo))
                        continue;
                    if (roomGo.transform.Find("D_" + passage.Id) != null)
                        continue;
                    GameObject go = CreateNamedMarker(roomGo.transform, "D_" + passage.Id, typeof(BastionDoor));
                    var auto = go.GetComponent<BastionDoor>();
                    if (auto == null)
                        continue;
                    auto.Configure(
                        passage.Id,
                        System.Array.Empty<string>(),
                        controlGate: false,
                        ToDoorEdge(passage.Direction),
                        locked: false);
                    doorMarkerCount++;
                }

                WireSealedSpawns(spawnByName);
                WireControlSpawns(spawnByName);
                BastionSketchHatchDoors.SnapStationToMiddle();

                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }

            BastionSegment.ResetRebuildCounters();
            RebuildAllSegments();
            int geomCreated = BastionSegment.RebuildObjectsCreated;
            int geomMarked = BastionSegment.RebuildObjectsMarked;
            BastionKitEmitter.CleanupOrphanGeneratedGeom();
            // Stairs parent to rooms still in Generated, then settle carries them.
            int stairCount = BastionSketchStairs.Emit(sketch, roomObjects);
            int settled = SettleGeneratedIntoAuthored(generated, authored);

            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (geomCreated != geomMarked)
                Debug.LogError("[BastionSketchEmitter] Rebuild created " + geomCreated
                    + " object(s) but marked " + geomMarked + " with BastionGeneratedGeom.");

            Debug.Log("[BastionSketchEmitter] Emitted from " + sketchPath
                + " into " + scene.path
                + ": rooms=" + roomCount
                + " markers=" + markerCount
                + " doorMarkers=" + doorMarkerCount
                + " doors=" + sketch.Doors.Count
                + " stairs=" + stairCount
                + " sideOverrides=" + sketch.Sides.Count
                + " settled=" + settled
                + (geomCreated == geomMarked
                    ? " geomCreated=" + geomCreated + " geomMarked=" + geomMarked
                    : ""));
        }

        static GameObject FindOrCreateRoot()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing == null)
                existing = GameObject.Find("BastionLayout");
            if (existing == null)
                existing = new GameObject(RootName);

            // Keep kit under Environment so NavMeshSurface Children collect includes floors.
            GameObject env = GameObject.Find("Environment");
            if (env != null && existing.transform.parent != env.transform)
                existing.transform.SetParent(env.transform, true);

            return existing;
        }

        static void FindOrCreateSubtrees(GameObject root, out GameObject generated, out GameObject authored)
        {
            // Neither node gets BastionGeneratedGeom: CleanupOrphanGeneratedGeom destroys any
            // marked object with no BastionSegment parent, and these sit above the segments.
            generated = FindOrCreateChild(root, GeneratedName);

            // Authored is Zo's subtree. This emitter creates it if absent and never reads,
            // moves, clears or destroys anything in it.
            authored = FindOrCreateChild(root, AuthoredName);
        }

        static GameObject FindOrCreateChild(GameObject root, string name)
        {
            Transform existing = root.transform.Find(name);
            if (existing != null)
                return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            return go;
        }

        /// <summary>Sorts pre-split direct children of the kit root: old generated content
        /// (anything holding a BastionSegment or BastionGeneratedGeom) is destroyed, the rest
        /// is presumed hand-placed and moved under Authored. No-op after the first emit.</summary>
        static void MigrateLegacyChildren(GameObject root, GameObject generated, GameObject authored)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child == generated.transform || child == authored.transform)
                    continue;

                if (child.GetComponentInChildren<BastionSegment>(true) != null
                    || child.GetComponentInChildren<BastionGeneratedGeom>(true) != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    continue;
                }

                child.SetParent(authored.transform, true);
                Debug.Log("[BastionSketchEmitter] Moved hand-placed \"" + child.name + "\" under " + AuthoredName + ".");
            }
        }

        static void TryAddLayoutComponent(GameObject root)
        {
            // One layout host: Environment when present, else the kit root. Two hosts double-register.
            GameObject env = GameObject.Find("Environment");
            GameObject host = env != null ? env : root;
            if (host.GetComponent<BastionLayout>() == null)
                host.AddComponent<BastionLayout>();

            if (env != null && root != env)
            {
                BastionLayout stray = root.GetComponent<BastionLayout>();
                if (stray != null)
                    UnityEngine.Object.DestroyImmediate(stray);
            }
        }

        static void ApplyDoorSides(BastionSketch sketch, Dictionary<string, SideSet> map)
        {
            for (int i = 0; i < sketch.Doors.Count; i++)
            {
                BastionSketchDoor door = sketch.Doors[i];
                if (!map.TryGetValue(door.RoomId, out SideSet set))
                {
                    set = new SideSet();
                    map[door.RoomId] = set;
                }

                set.Set(door.Direction, BastionSideKind.Door, door.Along);
            }
        }

        static void ApplySpawnSides(BastionSketch sketch, Dictionary<string, SideSet> map)
        {
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind != BastionSketchMarkKind.Spawn || !mark.HasDirection)
                        continue;
                    if (!map.TryGetValue(room.Id, out SideSet set))
                    {
                        set = new SideSet();
                        map[room.Id] = set;
                    }

                    set.Set(mark.Direction, BastionSideKind.Door, 0f);
                }
            }
        }

        static void ClearChildren(GameObject root)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        static GameObject InstantiateRoom(BastionSketchRoom room)
        {
            if (!PrefabNames.TryGetValue(room.Kind, out string prefabName))
            {
                Debug.LogError("[BastionSketchEmitter] No prefab mapping for " + room.Kind);
                return null;
            }

            string path = KitDir + "/" + prefabName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("[BastionSketchEmitter] Missing prefab: " + path);
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[BastionSketchEmitter] Failed to instantiate " + path);
                return null;
            }

            // Kit prefabs stamp a room quickly; Completely unpacks so the scene owns unique
            // geometry. Pack updates do not propagate into authored rooms.
            PrefabUtility.UnpackPrefabInstance(
                instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            return instance;
        }

        sealed class SideSet
        {
            public BastionSideKind North = BastionSideKind.Wall;
            public BastionSideKind East = BastionSideKind.Wall;
            public BastionSideKind South = BastionSideKind.Wall;
            public BastionSideKind West = BastionSideKind.Wall;
            public float NorthAlong;
            public float EastAlong;
            public float SouthAlong;
            public float WestAlong;

            public void Set(BastionSketchDirection direction, BastionSideKind kind, float along)
            {
                switch (direction)
                {
                    case BastionSketchDirection.North: North = kind; NorthAlong = along; break;
                    case BastionSketchDirection.East: East = kind; EastAlong = along; break;
                    case BastionSketchDirection.South: South = kind; SouthAlong = along; break;
                    case BastionSketchDirection.West: West = kind; WestAlong = along; break;
                }
            }
        }

        static Dictionary<string, SideSet> BuildEffectiveSides(BastionSketch sketch)
        {
            var map = new Dictionary<string, SideSet>();
            for (int i = 0; i < sketch.Sides.Count; i++)
            {
                BastionSketchSideOverride side = sketch.Sides[i];
                if (!map.TryGetValue(side.RoomId, out SideSet set))
                {
                    set = new SideSet();
                    map[side.RoomId] = set;
                }

                set.Set(side.Direction, side.Kind, side.Along);
            }

            return map;
        }

        static void ApplySides(BastionSegment segment, SideSet sides)
        {
            segment.North = sides.North;
            segment.East = sides.East;
            segment.South = sides.South;
            segment.West = sides.West;
            segment.NorthDoorAlong = sides.NorthAlong;
            segment.EastDoorAlong = sides.EastAlong;
            segment.SouthDoorAlong = sides.SouthAlong;
            segment.WestDoorAlong = sides.WestAlong;
        }

        static bool CreateMark(
            Transform room,
            BastionSketchMark mark,
            Dictionary<string, SpawnPoint> spawnByName,
            Dictionary<string, BastionTerminal> terminalById,
            GameController game)
        {
            switch (mark.Kind)
            {
                case BastionSketchMarkKind.Hatch:
                    if (!BastionSketchHatchDoors.TryCreate(room, mark))
                        CreateNamedMarker(room, "H", null);
                    return true;
                case BastionSketchMarkKind.Spawn:
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnRoom.PrefabPath);
                    if (prefab == null)
                    {
                        Debug.LogError("[BastionSketchEmitter] Missing " + SpawnRoom.PrefabPath);
                        return false;
                    }

                    var instance = PrefabUtility.InstantiatePrefab(prefab, room) as GameObject;
                    if (instance == null)
                        return false;

                    instance.name = "Spawn_" + mark.RefId;
                    var closet = instance.GetComponent<SpawnRoom>();
                    if (closet == null)
                        closet = instance.AddComponent<SpawnRoom>();

                    var segment = room.GetComponent<BastionSegment>();
                    closet.AttachTo(segment, ToDoorEdge(mark.Direction));
                    closet.NameSpawn(mark.RefId);
                    closet.SetMinSeats(mark.MinSeats);

                    var sp = closet.Spawn;
                    if (sp != null)
                        spawnByName[mark.RefId] = sp;
                    return true;
                }
                case BastionSketchMarkKind.Terminal:
                    return CreateTerminalMark(room, mark.RefId, BastionTerminalKind.Field, terminalById, game);
                case BastionSketchMarkKind.Master:
                    return CreateTerminalMark(room, "Master", BastionTerminalKind.Master, terminalById, game);
                default:
                    return false;
            }
        }

        static bool CreateTerminalMark(
            Transform room,
            string id,
            BastionTerminalKind kind,
            Dictionary<string, BastionTerminal> terminalById,
            GameController game)
        {
            GameObject go = CreateNamedMarker(room, "T_" + id, typeof(BastionTerminal));
            var terminal = go.GetComponent<BastionTerminal>();
            if (terminal != null)
            {
                terminal.Configure(id, kind, waves: FirstTerminal.WavesFor(id));
                terminal.SetGameController(game);
                terminalById[id] = terminal;
                BastionFieldConsole.AttachIfMissing(terminal);
            }
            return true;
        }

        static BastionDoor CreateDoorMarker(Transform room, BastionSketchDoor door, Material gateMat)
        {
            GameObject go = CreateNamedMarker(room, "D_" + door.DoorId, typeof(BastionDoor));
            var component = go.GetComponent<BastionDoor>();
            if (component != null)
            {
                string[] unlocks = door.UnlockTerminalIds.ToArray();
                component.Configure(
                    door.DoorId,
                    unlocks,
                    door.Control,
                    ToDoorEdge(door.Direction),
                    locked: true);
                if (gateMat != null)
                    component.SetGateMaterial(gateMat);
            }
            return component;
        }

        static BastionDoorEdge ToDoorEdge(BastionSketchDirection direction)
        {
            switch (direction)
            {
                case BastionSketchDirection.North: return BastionDoorEdge.North;
                case BastionSketchDirection.South: return BastionDoorEdge.South;
                case BastionSketchDirection.East: return BastionDoorEdge.East;
                case BastionSketchDirection.West: return BastionDoorEdge.West;
                default: return BastionDoorEdge.North;
            }
        }

        /// <summary>Spawns behind progression gates stay inactive until the door opens.
        /// Owned by <see cref="BastionDoor.SealedSpawns"/> — terminals do not reference them.</summary>
        static void WireSealedSpawns(Dictionary<string, SpawnPoint> spawnByName)
        {
            BastionDoor[] doors = UnityEngine.Object.FindObjectsByType<BastionDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var lockedWingSpawns = new List<SpawnPoint>();
            foreach (KeyValuePair<string, SpawnPoint> pair in spawnByName)
            {
                if (IsZoneOneAlwaysLive(pair.Key))
                    continue;
                if (IsControlRoomSpawn(pair.Value))
                    continue;
                if (pair.Value != null)
                    lockedWingSpawns.Add(pair.Value);
            }

            // Lowest door id after D1 owns the sealed list for graybox — FindObjectsByType
            // order is undefined, so pick deterministically or the control door can win.
            BastionDoor owner = null;
            for (int i = 0; i < doors.Length; i++)
            {
                BastionDoor d = doors[i];
                if (d == null || string.IsNullOrEmpty(d.DoorId) || d.DoorId == "D1")
                    continue;
                if (d.OpensFor == BastionDoorOpensFor.Enemies)
                    continue;
                if (owner == null || string.CompareOrdinal(d.DoorId, owner.DoorId) < 0)
                    owner = d;
            }

            if (owner == null)
                return;

            owner.SetSealedSpawns(lockedWingSpawns.ToArray());
            for (int s = 0; s < lockedWingSpawns.Count; s++)
            {
                if (lockedWingSpawns[s] != null)
                    lockedWingSpawns[s].gameObject.SetActive(false);
            }
        }

        /// <summary>Control Room closets wait on Master Control. Inactive until the room
        /// fight starts; D2 must not own them.</summary>
        static void WireControlSpawns(Dictionary<string, SpawnPoint> spawnByName)
        {
            var control = new List<SpawnPoint>();
            foreach (KeyValuePair<string, SpawnPoint> pair in spawnByName)
            {
                if (!IsControlRoomSpawn(pair.Value))
                    continue;
                control.Add(pair.Value);
                pair.Value.gameObject.SetActive(false);
            }

            if (control.Count == 0)
                return;

            BastionTerminal[] terminals = UnityEngine.Object.FindObjectsByType<BastionTerminal>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            BastionTerminal master = null;
            for (int i = 0; i < terminals.Length; i++)
            {
                BastionTerminal t = terminals[i];
                if (t != null && t.Kind == BastionTerminalKind.Master)
                {
                    master = t;
                    break;
                }
            }

            if (master == null)
            {
                Debug.LogError("[BastionSketchEmitter] Control Room spawns but no Master terminal");
                return;
            }

            master.SetUnlockedSpawns(control.ToArray());
        }

        static bool IsControlRoomSpawn(SpawnPoint spawn)
        {
            if (spawn == null)
                return false;
            BastionSegment segment = spawn.GetComponentInParent<BastionSegment>();
            return segment != null && segment.BlockKind == BastionBlockKind.ControlRoom;
        }

        /// <summary>S1/S2 (ground) and S7/S8 (upper island A) stay live from the start.
        /// S9–S12 wait on D2 with S3–S6. Two-seat mute is runtime <see cref="BastionSeatSpawns"/>,
        /// not this list.</summary>
        static bool IsZoneOneAlwaysLive(string spawnId)
        {
            return spawnId == "S1" || spawnId == "S2" || spawnId == "S7" || spawnId == "S8";
        }

        static void StampSpawnMinSeats(BastionSketchRoom room, Dictionary<string, SpawnPoint> spawnByName)
        {
            for (int m = 0; m < room.Marks.Count; m++)
            {
                BastionSketchMark mark = room.Marks[m];
                if (mark.Kind != BastionSketchMarkKind.Spawn)
                    continue;
                if (!spawnByName.TryGetValue(mark.RefId, out SpawnPoint sp) || sp == null)
                    continue;
                SpawnRoom closet = sp.GetComponentInParent<SpawnRoom>();
                if (closet != null)
                    closet.SetMinSeats(mark.MinSeats);
            }
        }

        static void ParseSketchSentinel(string text, ref string sketchPath, ref string scenePath)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string[] lines = text.Split('\n');
            int filled = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                if (filled == 0)
                {
                    sketchPath = Path.IsPathRooted(line)
                        ? line
                        : Path.Combine(Root, line);
                    filled = 1;
                }
                else
                {
                    scenePath = NormalizeScenePath(line);
                    break;
                }
            }
        }

        static string NormalizeScenePath(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return DefaultSceneRelative;
            string n = raw.Trim().Replace('\\', '/');
            int assets = n.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assets >= 0)
                return n.Substring(assets + 1);
            if (n.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return n;
            return n;
        }

        static GameObject CreateNamedMarker(Transform parent, string name, Type componentType)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            // Markers live under rooms, so GetComponentInParent<BastionSegment> in
            // CleanupOrphanGeneratedGeom skips them despite the marker component.
            go.AddComponent<BastionGeneratedGeom>();

            if (componentType != null && typeof(MonoBehaviour).IsAssignableFrom(componentType))
                go.AddComponent(componentType);

            return go;
        }

        static void RebuildAllSegments()
        {
            BastionSegment[] segments = UnityEngine.Object.FindObjectsByType<BastionSegment>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < segments.Length; i++)
            {
                BastionSegment segment = segments[i];
                if (segment == null || !segment.CanMutateHierarchy())
                    continue;
                // Authored rooms are unique scene geometry. Rebuilding them from kit
                // prefabs would replace hand edits with a fresh stamp.
                if (IsUnderNamed(segment.transform, AuthoredName))
                    continue;
                segment.Rebuild();
            }
        }

        static bool IsUnderNamed(Transform t, string name)
        {
            while (t != null)
            {
                if (t.name == name)
                    return true;
                t = t.parent;
            }
            return false;
        }

        /// <summary>Batchmode entry: emit default sketch then exit with code 0.</summary>
        public static void EmitFromBatch()
        {
            EmitFromSketchFile(DefaultSketchPath);
            EditorApplication.Exit(0);
        }
    }
}

