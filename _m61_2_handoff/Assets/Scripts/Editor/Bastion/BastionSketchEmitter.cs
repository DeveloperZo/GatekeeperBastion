using System;
using System.Collections.Generic;
using System.IO;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Emits kit segment instances from a bastion sketch file. Trigger via
    /// gkb_bastion_sketch_request.txt at the project root (path to sketch, or empty for default).</summary>
    [InitializeOnLoad]
    public static class BastionSketchEmitter
    {
        const string KitDir = "Assets/Prefabs/Bastion/Kit";
        const string DefaultSketchRelative = "Tools/bastion-sketch/example-zo-layout.txt";
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

            if (!File.Exists(SentinelPath)) return;

            string sketchPath = DefaultSketchPath;
            try
            {
                string sentinelText = File.ReadAllText(SentinelPath).Trim();
                File.Delete(SentinelPath);
                if (sentinelText.Length > 0)
                    sketchPath = Path.IsPathRooted(sentinelText)
                        ? sentinelText
                        : Path.Combine(Root, sentinelText);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BastionSketchEmitter] sentinel: " + e.Message);
                return;
            }

            EmitFromSketchFile(sketchPath);
        }

        public static void EmitFromSketchFile(string sketchPath)
        {
            if (!File.Exists(sketchPath))
            {
                Debug.LogError("[BastionSketchEmitter] Sketch not found: " + sketchPath);
                return;
            }

            const string bastionScenePath = "Assets/Scenes/Bastion.unity";
            Scene bastion = EditorSceneManager.OpenScene(bastionScenePath, OpenSceneMode.Single);

            string text = File.ReadAllText(sketchPath);
            BastionSketchParseResult parsed = BastionSketchParser.Parse(text);
            if (!parsed.Success)
            {
                Debug.LogError("[BastionSketchEmitter] Parse failed: " + parsed.Error);
                return;
            }

            EmitSketch(parsed.Sketch, sketchPath, bastion);
        }

        static void EmitSketch(BastionSketch sketch, string sketchPath, Scene bastionScene)
        {
            BastionSegment.SuppressValidateRebuild = true;
            int roomCount = 0;
            int markerCount = 0;
            int doorMarkerCount = 0;

            try
            {
                GameObject layoutRoot = FindOrCreateRoot();
                FindOrCreateSubtrees(layoutRoot, out GameObject generated, out GameObject authored);
                MigrateLegacyChildren(layoutRoot, generated, authored);
                ClearChildren(generated);

                TryAddLayoutComponent(layoutRoot);

                var roomObjects = new Dictionary<string, GameObject>();
                var effectiveSides = BuildEffectiveSides(sketch);
                ApplyDoorSides(sketch, effectiveSides);

                var spawnByName = new Dictionary<string, SpawnPoint>();

                for (int i = 0; i < sketch.Rooms.Count; i++)
                {
                    BastionSketchRoom room = sketch.Rooms[i];
                    GameObject instance = InstantiateRoom(room);
                    if (instance == null)
                        continue;

                    instance.name = room.Id;
                    instance.transform.SetParent(generated.transform, false);
                    instance.transform.position = new Vector3(room.CenterX, 0f, room.CenterZ);

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
                        if (CreateMark(instance.transform, room.Marks[m], spawnByName))
                            markerCount++;
                    }
                }

                for (int i = 0; i < sketch.Doors.Count; i++)
                {
                    BastionSketchDoor door = sketch.Doors[i];
                    if (!roomObjects.TryGetValue(door.RoomId, out GameObject roomGo))
                        continue;

                    CreateDoorMarker(roomGo.transform, door);
                    doorMarkerCount++;
                }

                WireSealedSpawns(spawnByName);

                if (bastionScene.IsValid())
                    EditorSceneManager.MarkSceneDirty(bastionScene);
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

            if (bastionScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(bastionScene);
                EditorSceneManager.SaveScene(bastionScene, "Assets/Scenes/Bastion.unity");
            }

            if (geomCreated != geomMarked)
                Debug.LogError("[BastionSketchEmitter] Rebuild created " + geomCreated
                    + " object(s) but marked " + geomMarked + " with BastionGeneratedGeom.");

            Debug.Log("[BastionSketchEmitter] Emitted from " + sketchPath
                + ": rooms=" + roomCount
                + " markers=" + markerCount
                + " doorMarkers=" + doorMarkerCount
                + " doors=" + sketch.Doors.Count
                + " sideOverrides=" + sketch.Sides.Count
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

                switch (door.Direction)
                {
                    case BastionSketchDirection.North: set.North = BastionSideKind.Door; break;
                    case BastionSketchDirection.East: set.East = BastionSideKind.Door; break;
                    case BastionSketchDirection.South: set.South = BastionSideKind.Door; break;
                    case BastionSketchDirection.West: set.West = BastionSideKind.Door; break;
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

            return instance;
        }

        sealed class SideSet
        {
            public BastionSideKind North = BastionSideKind.Wall;
            public BastionSideKind East = BastionSideKind.Wall;
            public BastionSideKind South = BastionSideKind.Wall;
            public BastionSideKind West = BastionSideKind.Wall;
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

                switch (side.Direction)
                {
                    case BastionSketchDirection.North: set.North = side.Kind; break;
                    case BastionSketchDirection.East: set.East = side.Kind; break;
                    case BastionSketchDirection.South: set.South = side.Kind; break;
                    case BastionSketchDirection.West: set.West = side.Kind; break;
                }
            }

            return map;
        }

        static void ApplySides(BastionSegment segment, SideSet sides)
        {
            segment.North = sides.North;
            segment.East = sides.East;
            segment.South = sides.South;
            segment.West = sides.West;
        }

        static bool CreateMark(
            Transform room,
            BastionSketchMark mark,
            Dictionary<string, SpawnPoint> spawnByName)
        {
            switch (mark.Kind)
            {
                case BastionSketchMarkKind.Hatch:
                    CreateNamedMarker(room, "H", null);
                    return true;
                case BastionSketchMarkKind.Spawn:
                {
                    GameObject go = CreateNamedMarker(room, "S_" + mark.RefId, typeof(SpawnPoint));
                    var sp = go.GetComponent<SpawnPoint>();
                    if (sp != null)
                        spawnByName[mark.RefId] = sp;
                    return true;
                }
                case BastionSketchMarkKind.Terminal:
                {
                    GameObject go = CreateNamedMarker(room, "T_" + mark.RefId, typeof(BastionTerminal));
                    var terminal = go.GetComponent<BastionTerminal>();
                    if (terminal != null)
                    {
                        terminal.Configure(mark.RefId, BastionTerminalKind.Field);
                        BastionFieldConsole.AttachIfMissing(terminal);
                    }
                    return true;
                }
                default:
                    return false;
            }
        }

        static void CreateDoorMarker(Transform room, BastionSketchDoor door)
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
            }
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
                if (pair.Key == "S1" || pair.Key == "S2")
                    continue;
                if (pair.Value != null)
                    lockedWingSpawns.Add(pair.Value);
            }

            for (int i = 0; i < doors.Length; i++)
            {
                BastionDoor d = doors[i];
                if (d == null || d.DoorId == "D1")
                    continue;
                d.SetSealedSpawns(lockedWingSpawns.ToArray());
                for (int s = 0; s < lockedWingSpawns.Count; s++)
                {
                    if (lockedWingSpawns[s] != null)
                        lockedWingSpawns[s].gameObject.SetActive(false);
                }
                break; // first mid/control door owns sealed list for graybox
            }
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
                segment.Rebuild();
            }
        }

        /// <summary>Batchmode entry: emit default sketch then exit with code 0.</summary>
        public static void EmitFromBatch()
        {
            EmitFromSketchFile(DefaultSketchPath);
            EditorApplication.Exit(0);
        }
    }
}

