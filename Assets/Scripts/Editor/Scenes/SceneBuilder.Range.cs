using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        /// <summary>The range hosts the SHIP bed, not the Bastion's combat one. It came in on
        /// the Bastion copy carrying the wrong music and a FrontEndMusic host besides; the audio
        /// tests already class the range as ship-like, and they are right — nothing is hunting
        /// you on a lane.</summary>
        const string LevelMusicClipPath = "Assets/Resources/Audio/Beds/Hydro-Ship.mp3";

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

        /// <summary>Clearance kept behind the farthest mark so a body is not standing in the
        /// wall.</summary>
        const float EndMargin = 1f;

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
                // Opened, not yet saved anywhere. Writing Range.unity up front meant a later
                // refusal left a HALF-BUILT scene on disk — stripped of the Bastion and without
                // the lane. The save happens once, at the end, when there is something worth
                // saving. Bastion.unity is never written: SaveScene re-targets to the new path.
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

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
                Transform marksRoot = PlaceMarks(env.transform);
                PlaceFiringLine();
                EnsureLevelMusic();
                EnsureRangeRoot(marksRoot);

                BakeNavMesh(env, RangeNavMeshPath);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, RangeScenePath))
                {
                    Debug.LogError("[SceneBuilder] Could not write " + RangeScenePath);
                    return;
                }
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
            // FLOOR renderers only. Bounds over everything includes the walls, whose faces sit
            // metres outside the floor they enclose — measuring off those put the firing line
            // two-tenths of a metre past the last floor tile, on the invisible collider slab but
            // OFF the navmesh, which is baked from render meshes. The player stood there fine and
            // nothing walking could reach them.
            var renderers = hall.GetComponentsInChildren<MeshRenderer>(true);
            bool any = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (renderers[i].name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (!any) { bounds = renderers[i].bounds; any = true; }
                else bounds.Encapsulate(renderers[i].bounds);
            }
            if (!any)
            {
                Debug.LogError("[SceneBuilder] Range: the hall generated no floor renderers — "
                    + "the navmesh bakes from render meshes, so there would be nothing to walk on.");
                return false;
            }

            // Stand as far back as the floor allows, up to the preferred inset. The hall is
            // only a few centimetres longer than 90 m plus that inset, and refusing over one of
            // them would have been pedantry — but a mark PAST the floor is not, so the fit is
            // checked after the squeeze rather than instead of it.
            float longestMark = RangeMarks[RangeMarks.Length - 1];
            float room = bounds.size.x - longestMark - EndMargin;
            float inset = Mathf.Clamp(room, 0f, FiringLineInset);
            _firingLineX = bounds.min.x + inset;
            _laneZ = bounds.center.z;

            if (_firingLineX + longestMark > bounds.max.x)
            {
                Debug.LogError("[SceneBuilder] Range: the floor is " + bounds.size.x.ToString("0.#")
                    + " m across and the longest mark is " + longestMark
                    + " m — it will not fit. Refusing to build a lane that lies.");
                return false;
            }
            Debug.Log("[SceneBuilder] Range lane: FLOOR x " + bounds.min.x.ToString("0.#") + ".."
                + bounds.max.x.ToString("0.#") + ", firing line " + _firingLineX.ToString("0.#")
                + ", lane z " + _laneZ.ToString("0.#"));
            return true;
        }

        /// <summary>A marker where each body stands, and a post either side of it so the
        /// distance is readable from the firing line. Markers are empty transforms: the console
        /// spawns onto the navmesh at one, rather than into a prop.</summary>
        static Transform PlaceMarks(Transform env)
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
            return marks.transform;
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

        /// <summary>The range's own root: the rules, the console that grants and spawns, and the
        /// readout. Everything is bound here rather than dragged in the inspector, so a rebuild
        /// cannot leave a half-wired scene behind.</summary>
        static void EnsureRangeRoot(Transform marks)
        {
            GameObject root = GameObject.Find("Range");
            if (root == null) root = new GameObject("Range");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var rules = root.GetComponent<RangeRules>();
            if (rules == null) rules = root.AddComponent<RangeRules>();
            var console = root.GetComponent<RangeConsole>();
            if (console == null) console = root.AddComponent<RangeConsole>();
            var readout = root.GetComponent<RangeReadout>();
            if (readout == null) readout = root.AddComponent<RangeReadout>();

            var game = Object.FindFirstObjectByType<GameController>();
            if (game == null)
            {
                Debug.LogWarning("[SceneBuilder] Range: no GameController to bind to.");
                return;
            }
            rules.Bind(game);
            console.Bind(game, rules, marks);
            readout.Bind(game, rules, BuildReadoutText(root.transform));
        }

        /// <summary>Swap the Bastion's music host for the ship bed. The copy arrives with a
        /// "Background Music" root, which is the FRONT-END host — leaving it in a playable scene
        /// means two things owning the music.</summary>
        static void EnsureLevelMusic()
        {
            GameObject frontEnd = GameObject.Find("Background Music");
            if (frontEnd != null) Object.DestroyImmediate(frontEnd);

            GameObject host = GameObject.Find("Level Music");
            if (host == null) host = new GameObject("Level Music");
            host.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var source = host.GetComponent<AudioSource>();
            if (source == null) source = host.AddComponent<AudioSource>();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(LevelMusicClipPath);
            if (clip == null)
            {
                Debug.LogWarning("[SceneBuilder] Range: no music bed at " + LevelMusicClipPath);
                return;
            }
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
        }

        /// <summary>A screen-space line for the readout. Its own canvas rather than the Bastion
        /// HUD's: the HUD is driven by run state this scene deliberately never enters, and
        /// borrowing it would put range numbers inside a fight's layout.</summary>
        static TMP_Text BuildReadoutText(Transform parent)
        {
            var canvasGo = new GameObject("Range HUD",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var textGo = new GameObject("Readout", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.fontSize = 26f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text = "";
            text.raycastTarget = false;

            var rt = textGo.GetComponent<RectTransform>();
            // Top-left, inset. Anchored to the corner so it does not drift with resolution.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -40f);
            rt.sizeDelta = new Vector2(900f, 120f);
            return text;
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
