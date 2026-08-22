using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Builds <c>Assets/Scenes/Range.unity</c>, the practice range.
    ///
    /// Two decisions, both about not building things that already exist.
    ///
    /// The scene is a COPY of the Bastion. The milestone calls the range "Bastion-shaped" and
    /// means it literally: a Gatekeeper's brain reads run state off a live
    /// <see cref="GameController"/>, targeting reads <c>HatchStation</c>, and the camera rig,
    /// post-processing and player wiring are already correct there. Re-deriving those by hand
    /// would be re-deriving working serialized references, and the first thing to go wrong
    /// would be a silent null.
    ///
    /// The room is ONE <c>ControlRoom</c> kit block (Zo, 2026-08-22: "most likely just a
    /// Control Room block"). It is the wide finale hall — <c>Multiple 47 × Scaler 2</c> with
    /// aspect 0.5 gives 94 m across by 47 m deep. The 90 m mark still fits on the long
    /// axis with a 3 m firing-line inset (1 m past the last mark). A hand-built lane would
    /// have been a second, worse copy of the kit.
    ///
    /// Re-runnable: the room and the marks are rebuilt from scratch each time, so tuning the
    /// lane is a re-run rather than a hand-edit (Docs/Milestones — a range you can fight in).</summary>
    public static partial class SceneBuilder
    {
        const string RangeScenePath = "Assets/Scenes/Range.unity";
        const string ControlRoomPrefabPath = "Assets/Prefabs/Bastion/Kit/ControlRoom.prefab";

        /// <summary>The range's OWN navmesh asset. BakeNavMesh defaults to the Bastion's, and
        /// letting it do that here baked the lane straight over Bastion-NavMesh.asset — the
        /// Bastion and the range would then have shared one surface, each rebake silently
        /// breaking whichever scene was baked less recently.</summary>
        const string RangeNavMeshPath = "Assets/Scenes/Range-NavMesh.asset";

        /// <summary>Metres down the lane at which a Gatekeeper can be stood. The range values of
        /// BR-12, MR-35 and PRI-C plus the two short marks, so falloff is checked by walking
        /// rather than by reading a table (criterion 7).</summary>
        static readonly int[] RangeMarks = { 5, 14, 30, 60, 90 };

        /// <summary>How far inside the hall's near wall the firing line stands. Everything
        /// else is MEASURED off the geometry the kit actually generated: the hall is not centred
        /// on its own origin, and assuming it was put the player half a metre past the floor's
        /// edge, where they fell through.</summary>
        const float FiringLineInset = 3f;

        static float _firingLineX;
        static float _laneZ;

        [MenuItem("GatekeepersProtocol/Build/Range Scene", false, 62)]
        public static void BuildRangeSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildRangeSceneInPlace();
        }

        /// <summary>Copy the Bastion to Range.unity, replace its rooms with one hall, bake, and
        /// register. Bastion.unity is not written to — SaveScene re-targets the open scene.</summary>
        public static void BuildRangeSceneInPlace()
        {
            // Refuse rather than discard. This runs from a file sentinel, so there is nobody to
            // answer a save prompt — and OpenScene would throw away whatever is unsaved in the
            // editor. Another agent authoring a scene alongside this is the normal case here,
            // not the exception.
            Scene open = SceneManager.GetActiveScene();
            if (open.isDirty)
            {
                Debug.LogError("[SceneBuilder] Range build refused — '" + open.name
                    + "' has unsaved changes. Save it first; nothing was touched.");
                return;
            }

            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, RangeScenePath))
                {
                    Debug.LogError("[SceneBuilder] Could not write " + RangeScenePath);
                    return;
                }

                GameObject env = GameObject.Find("Environment");
                if (env == null)
                {
                    Debug.LogError("[SceneBuilder] The Bastion copy has no Environment root.");
                    return;
                }

                StripBastionRooms(env);
                GameObject hall = PlaceControlRoom(env.transform);
                if (hall == null) return;
                if (!MeasureLane(hall)) return;
                PlaceMarks(env.transform);
                PlaceFiringLine();
                EnsureRangeRoot();

                BakeNavMesh(env, RangeNavMeshPath);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, RangeScenePath);
                RegisterRangeScene();
                AssetDatabase.SaveAssets();
                Debug.Log("[SceneBuilder] Range.unity built — one ControlRoom hall, firing line at x="
                    + _firingLineX.ToString("0.#") + ", marks at " + string.Join("/", RangeMarks) + " m, NavMesh baked.");
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        /// <summary>Everything under Environment goes. The Bastion's rooms, doors and terminals
        /// are what the range is NOT, and a door left behind would register with the layout and
        /// drop a NavMeshObstacle across the lane.</summary>
        static void StripBastionRooms(GameObject env)
        {
            var doomed = new List<GameObject>();
            foreach (Transform child in env.transform)
                doomed.Add(child.gameObject);
            for (int i = 0; i < doomed.Count; i++)
                Object.DestroyImmediate(doomed[i]);

            // The layout component stays: GameController requires it and BastionDoor registers
            // against it. With no doors it holds nothing, which is the correct state rather
            // than a missing reference.
            if (env.GetComponent<BastionLayout>() == null)
                env.AddComponent<BastionLayout>();
        }

        /// <summary>One hall, from the shipped kit prefab, at the origin.</summary>
        static GameObject PlaceControlRoom(Transform env)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ControlRoomPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[SceneBuilder] Range: no kit prefab at " + ControlRoomPrefabPath);
                return null;
            }
            var hall = (GameObject)PrefabUtility.InstantiatePrefab(prefab, env);
            hall.name = "Range Hall";
            hall.transform.localPosition = Vector3.zero;
            hall.transform.localRotation = Quaternion.identity;

            var segment = hall.GetComponent<BastionSegment>();
            if (segment == null)
            {
                Debug.LogError("[SceneBuilder] Range: the kit prefab carries no BastionSegment.");
                return null;
            }
            if (segment.BlockKind != BastionBlockKind.ControlRoom)
                Debug.LogWarning("[SceneBuilder] Range: kit prefab is " + segment.BlockKind
                    + ", not ControlRoom — the lane length assumes the wide hall.");

            // The kit prefab is an EMPTY shell — one GameObject with this component. Every
            // floor and wall is generated by Rebuild. This whole build runs with
            // SuppressValidateRebuild set so the Bastion's own segments stay still, and that
            // silence is why the first range shipped a hall with no floor and dropped the
            // player into the void. Ask for the geometry explicitly.
            segment.Rebuild();

            int emitted = hall.GetComponentsInChildren<MeshRenderer>(true).Length;
            if (emitted == 0)
            {
                Debug.LogError("[SceneBuilder] Range: the hall generated no geometry — "
                    + "nothing to stand on, refusing to save a scene that would drop the player.");
                return null;
            }
            Debug.Log("[SceneBuilder] Range hall: " + emitted + " renderers generated.");
            return hall;
        }

        /// <summary>Read the lane off the floor the kit generated. Refuses if the longest mark
        /// would land outside it, because a mark past the far wall is worse than no mark: it
        /// reads as a distance the weapon cannot reach.</summary>
        static bool MeasureLane(GameObject hall)
        {
            var renderers = hall.GetComponentsInChildren<MeshRenderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            _firingLineX = bounds.min.x + FiringLineInset;
            _laneZ = bounds.center.z;

            float longest = _firingLineX + RangeMarks[RangeMarks.Length - 1];
            if (longest > bounds.max.x - 1f)
            {
                Debug.LogError("[SceneBuilder] Range: the hall is " + bounds.size.x.ToString("0.#")
                    + " m across; the " + RangeMarks[RangeMarks.Length - 1]
                    + " m mark would sit outside it. Refusing to build a lane that lies.");
                return false;
            }
            Debug.Log("[SceneBuilder] Range lane: floor x " + bounds.min.x.ToString("0.#") + ".."
                + bounds.max.x.ToString("0.#") + ", firing line " + _firingLineX.ToString("0.#")
                + ", lane z " + _laneZ.ToString("0.#"));
            return true;
        }

        /// <summary>A marker where each body stands, and a post either side of it so the
        /// distance is readable from the firing line. Markers are empty transforms: the console
        /// spawns onto the navmesh at one, rather than into a prop.</summary>
        static void PlaceMarks(Transform env)
        {
            Palette palette = CreatePalette();
            var marks = new GameObject("Marks");
            marks.transform.SetParent(env, false);

            for (int i = 0; i < RangeMarks.Length; i++)
            {
                int m = RangeMarks[i];
                float x = _firingLineX + m;

                var spawn = new GameObject("RangeMark_" + m);
                spawn.transform.SetParent(marks.transform, false);
                spawn.transform.localPosition = new Vector3(x, 0f, _laneZ);

                Box("Post_" + m + "m_N", new Vector3(x, 0.6f, _laneZ + 3f),
                    new Vector3(0.16f, 1.2f, 0.16f), palette.Rib, marks.transform);
                Box("Post_" + m + "m_S", new Vector3(x, 0.6f, _laneZ - 3f),
                    new Vector3(0.16f, 1.2f, 0.16f), palette.Rib, marks.transform);
            }

            Box("Firing_Line", new Vector3(_firingLineX, 0.01f, _laneZ),
                new Vector3(0.12f, 0.02f, 6f), palette.Breach, marks.transform);
        }

        /// <summary>Put the player on the firing line looking down the lane. The rig came from
        /// the Bastion copy already wired; only where it stands is the range's business.</summary>
        static void PlaceFiringLine()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[SceneBuilder] Range: no Player root in the Bastion copy.");
                return;
            }
            // Facing +X, down the marks, standing ON the firing line rather than behind it —
            // the inset is only 3 m and stepping back put the player through the floor.
            player.transform.SetPositionAndRotation(
                new Vector3(_firingLineX, 0.2f, _laneZ), Quaternion.Euler(0f, 90f, 0f));
        }

        /// <summary>The range's own root, carrying the rules. The console and readout attach
        /// here as they land; nothing else in the scene owns them.</summary>
        static void EnsureRangeRoot()
        {
            GameObject root = GameObject.Find("Range");
            if (root == null) root = new GameObject("Range");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var rules = root.GetComponent<RangeRules>();
            if (rules == null) rules = root.AddComponent<RangeRules>();

            var game = Object.FindFirstObjectByType<GameController>();
            if (game != null) rules.Bind(game);
            else Debug.LogWarning("[SceneBuilder] Range: no GameController to bind the rules to.");
        }

        /// <summary>Append Range.unity to the build settings, after the boot scenes and without
        /// disturbing them. RegisterBuildScenes keeps anything registered later, so this only
        /// has to happen once — but it is idempotent so a rebuild cannot double-register.</summary>
        static void RegisterRangeScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] == null || scenes[i].path != RangeScenePath) continue;
                if (!scenes[i].enabled) scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(RangeScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
