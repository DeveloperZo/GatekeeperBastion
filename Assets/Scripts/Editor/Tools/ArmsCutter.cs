// First-person arms for M113: cut a rigged pose out of the Boarder's own skinned mesh and
// save it as a rigid prop, one child per arms class under Packs/Boarder/Arms.prefab.
// No Blender: SkinnedMeshRenderer.BakeMesh writes the CURRENT pose into a static mesh with
// vertex order preserved, so the bone weights on sharedMesh still decide which triangles
// are arm. The bake is expressed in the posing gun's wrapper-root frame, so the prop lands
// on any Packs/Weapons wrapper at identity with no tuning.
//
// GatekeepersProtocol > Arms > ...  (Step 0 to Step 4, then Show weapon/* in play)
//
// What breaks if this changes: Packs/Boarder/arms_source/README.md documents this as the
// only way to re-cut the arms; WeaponViewModel.EnableOnly expects the prefab layout this
// writes (root Arms, one child named by WeaponView.ArmsClassName, hands under it); the
// m_IsActive:0 override MenuWire writes on the Boarder.prefab instance is M113 criterion 7.

using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    public sealed class ArmsCutter : EditorWindow
    {
        #region Constants

        const string SourceFbx = "Assets/FaxLab3D/SciFiRobotCharactersPack/Models/SK_Tactical.fbx";
        const string MaterialPath = "Assets/Packs/Boarder/BoarderBot.mat";
        const string OutDir = "Assets/Packs/Boarder";
        const string PrefabAsset = OutDir + "/Arms.prefab";
        const string BoarderPrefab = "Assets/Packs/Boarder/Boarder.prefab";
        const string RigName = "ArmsCutter_PosingRig";

        // Metres from a hand bone within which a gun counts as the posing gun. A Boarder's own
        // weapon 44 m away, or a prefab-stage copy at its stage origin, is not it.
        const float GunReach = 0.6f;

        // Auto-Rig Pro names, read off the fbx. Forearm + hand + digits: the elbow cut sits
        // behind the camera near plane in first person, so the upper arm is not needed.
        static readonly string[] DefaultBones =
        {
            "hand", "forearm_stretch", "forearm_twist", "forearm_twist_2",
            "c_index1", "c_index2", "c_index3", "c_middle1", "c_middle2", "c_middle3",
            "c_ring1", "c_ring2", "c_ring3", "c_pinky1", "c_pinky2", "c_pinky3",
            "c_thumb1", "c_thumb2", "c_thumb3"
        };

        // A neutral grip as a starting point, not a finished pose; the rig is posed by hand
        // against the gun before baking.
        const float Knuckle1 = 45f, Knuckle2 = 55f, Knuckle3 = 30f, ThumbYaw = -35f;

        #endregion

        #region Inspector

        // [SerializeField] on an EditorWindow survives domain reloads. Without it a recompile
        // reset the pivot to null and the bake fell back to the hand bone without anyone noticing.
        [SerializeField] GameObject _rig;
        [SerializeField] Transform _gunPivot;
        [SerializeField] ArmMode _armMode = ArmMode.BothArmsAsPosed;
        [SerializeField] WeaponView.ArmsClass _class = WeaponView.ArmsClass.Pistol;
        [SerializeField] float _weightThreshold = 0.5f;

        #endregion

        #region State

        Vector2 _scroll;
        string _status = "Spawn the rig, pose it on a weapon wrapper, then bake.";

        #endregion

        #region Properties

        bool BothArms => _armMode == ArmMode.BothArmsAsPosed;

        #endregion

        #region Unity Lifecycle

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "1. Spawn rig.  2. Pose the hands on an instance of that class's Packs/Weapons " +
                "wrapper prefab (Curl Fingers is a start).  3. Bake: writes Arms.<Class>.asset " +
                "and that class child of Arms.prefab, then re-wires Boarder.prefab.", MessageType.Info);

            _rig = (GameObject)EditorGUILayout.ObjectField("Posed rig", _rig, typeof(GameObject), true);
            _gunPivot = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Gun pivot", "The gun the hands are posed on. The bake is expressed " +
                    "in this transform's frame, so the prop lands on the wrapper root with no tuning. " +
                    "Use an instance of the class's Packs/Weapons wrapper prefab (its root is gun " +
                    "space). Empty = the nearest wrapper to either hand."),
                _gunPivot, typeof(Transform), true);
            _class = (WeaponView.ArmsClass)EditorGUILayout.EnumPopup(
                new GUIContent("Class", "Which arms child this bake writes. Pistol: Mk1. Rifle: Mr35, " +
                    "PriC, SecD. Shotgun: Br12. Melee: the cutter."), _class);
            _armMode = (ArmMode)EditorGUILayout.EnumPopup(
                new GUIContent("Arms", "BothArmsAsPosed: .l and .r as posed, one mesh per side. " +
                    "GunHandOnly: the hand holding the gun. GunHandMirroredForOther: that hand's " +
                    "mesh again at scale x = -1 for the other side."), _armMode);
            if (_gunPivot != null && UnderGrip(_gunPivot))
                EditorGUILayout.HelpBox("'" + _gunPivot.name + "' is under a Grip: a Boarder's own weapon, not the posing gun. Bake will use the nearest wrapper instead.", MessageType.Error);
            Transform auto = _gunPivot != null ? null : FindGunInScene(RigHandPosition("hand.l"), RigHandPosition("hand.r"));
            if (auto != null)
                EditorGUILayout.HelpBox("Gun pivot is empty; bake will use '" + auto.name + "' found in the scene.", MessageType.None);
            else if (_gunPivot == null)
                EditorGUILayout.HelpBox("No gun pivot and none found in the scene: bake will use the hand bone as the frame and the prop will not line up in play.", MessageType.Warning);
            _weightThreshold = EditorGUILayout.Slider(
                new GUIContent("Arm weight >", "A vertex counts as arm when this much of its " +
                    "skin weight sits on the forearm/hand/finger bones."), _weightThreshold, 0.1f, 0.95f);

            EditorGUILayout.Space();
            if (GUILayout.Button("1 — Spawn rig in scene")) SpawnRig();
            using (new EditorGUI.DisabledScope(_rig == null))
            {
                if (GUILayout.Button("2 — Curl fingers (starting pose)")) CurlFingers();
                if (GUILayout.Button("3 — Bake this class")) { Bake(); Debug.Log("[ArmsCutter] " + _status); }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Public API

        [MenuItem("GatekeepersProtocol/Arms/Arms Cutter", false, 1)]
        public static void Open() => GetWindow<ArmsCutter>("Arms Cutter").minSize = new Vector2(340, 300);

        // The steps as menu items so a script or the MCP bridge can drive them; the window
        // buttons run the same code. The rig is found by name.
        [MenuItem("GatekeepersProtocol/Arms/Step 0 - Clean scene temps", false, 10)]
        public static void MenuClean()
        {
            int n = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || !go.scene.IsValid() || !go.scene.isLoaded) continue;
                if (EditorUtility.IsPersistent(go)) continue;
                bool temp = go.name == "ArmsPreview_TEMP" || go.name == RigName;
                bool strayArms = go.name == "Arms" && go.transform.parent != null
                    && go.transform.parent.name == "Grip"
                    && PrefabUtility.IsAddedGameObjectOverride(go);
                if (!temp && !strayArms) continue;
                Undo.DestroyObjectImmediate(go);
                n++;
            }
            Debug.Log("[ArmsCutter] Removed " + n + " scene temp object(s).");
        }

        [MenuItem("GatekeepersProtocol/Arms/Step 1 - Spawn Rig", false, 11)]
        public static void MenuSpawn() { var w = GetWindow<ArmsCutter>("Arms Cutter"); w.SpawnRig(); Debug.Log("[ArmsCutter] " + w._status); }

        [MenuItem("GatekeepersProtocol/Arms/Step 2 - Curl Fingers", false, 12)]
        public static void MenuCurl() { var w = GetWindow<ArmsCutter>("Arms Cutter"); w.FindRig(); w.CurlFingers(); Debug.Log("[ArmsCutter] " + w._status); }

        [MenuItem("GatekeepersProtocol/Arms/Step 3 - Bake", false, 13)]
        public static void MenuBake() { var w = GetWindow<ArmsCutter>("Arms Cutter"); w.FindRig(); w.Bake(); Debug.Log("[ArmsCutter] " + w._status); }

        [MenuItem("GatekeepersProtocol/Arms/Step 3b - Migrate prefab to class children", false, 14)]
        public static void MenuMigrate()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAsset) == null) { Debug.LogWarning("[ArmsCutter] No Arms.prefab to migrate."); return; }
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabAsset);
            try { EnsureClassLayout(root); PrefabUtility.SaveAsPrefabAsset(root, PrefabAsset); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            MenuWire();
        }

        // Uses the Editor's prefab API, not a prefab stage: LoadPrefabContents gives a
        // detached root, the arms are nested under Grip as a prefab instance, the instance is
        // set inactive (M113 criterion 7 - an override on the nested instance, greppable in
        // Boarder.prefab; EnableOnly turns it on for the local owner and a remote avatar has
        // no WeaponViewModel to do so), and the asset is saved.
        [MenuItem("GatekeepersProtocol/Arms/Step 4 - Wire into Boarder.prefab", false, 15)]
        public static void MenuWire()
        {
            var armsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAsset);
            if (armsAsset == null) { Debug.LogError("[ArmsCutter] No " + PrefabAsset + " - bake first."); return; }
            GameObject root = PrefabUtility.LoadPrefabContents(BoarderPrefab);
            try
            {
                Transform grip = FindNamed(root.transform, "Grip");
                if (grip == null) { Debug.LogError("[ArmsCutter] No Grip in " + BoarderPrefab); return; }
                GameObject inst = null;
                for (int i = 0; i < grip.childCount; i++)
                    if (grip.GetChild(i).name == "Arms") { inst = grip.GetChild(i).gameObject; break; }
                if (inst == null)
                {
                    inst = (GameObject)PrefabUtility.InstantiatePrefab(armsAsset, grip);
                    inst.name = "Arms";
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;
                }
                inst.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, BoarderPrefab);
                Debug.Log("[ArmsCutter] One inactive Arms instance under Grip in " + BoarderPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // A scene whose Boarder is an unpacked copy (Ship - CoOp) never receives the Arms
        // nested in Boarder.prefab. Give every scene Grip that is not part of a prefab
        // instance its own inactive Arms instance; the scene is marked dirty, not saved.
        [MenuItem("GatekeepersProtocol/Arms/Step 4b - Wire into unpacked scene Grips", false, 16)]
        public static void MenuWireScene()
        {
            var armsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAsset);
            if (armsAsset == null) { Debug.LogError("[ArmsCutter] No " + PrefabAsset + " - bake first."); return; }
            int wired = 0, already = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || !go.scene.IsValid() || !go.scene.isLoaded || EditorUtility.IsPersistent(go)) continue;
                if (go.name != "Grip" || PrefabUtility.IsPartOfPrefabInstance(go)) continue;
                bool has = false;
                for (int i = 0; i < go.transform.childCount; i++)
                    if (go.transform.GetChild(i).name == "Arms") has = true;
                if (has) { already++; continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(armsAsset, go.transform);
                inst.name = "Arms";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                inst.SetActive(false);
                Undo.RegisterCreatedObjectUndo(inst, "Wire Arms into scene Grip");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                wired++;
            }
            Debug.Log("[ArmsCutter] Wired " + wired + " unpacked scene Grip(s); " + already + " already had Arms. Save the scene.");
        }

        // Play-mode weapon switch a script can drive (the bridge cannot press F7). Same call
        // the dev loadout picker makes, then the slot is selected so the viewmodel shows it.
        [MenuItem("GatekeepersProtocol/Arms/Show weapon/Mk1", false, 30)] public static void ShowMk1() => ShowWeapon(WeaponId.Mk1Sidearm);
        [MenuItem("GatekeepersProtocol/Arms/Show weapon/SecD", false, 31)] public static void ShowSecD() => ShowWeapon(WeaponId.SecD);
        [MenuItem("GatekeepersProtocol/Arms/Show weapon/Br12", false, 32)] public static void ShowBr12() => ShowWeapon(WeaponId.Br12Shotgun);
        [MenuItem("GatekeepersProtocol/Arms/Show weapon/Mr35", false, 33)] public static void ShowMr35() => ShowWeapon(WeaponId.Mr35Rifle);
        [MenuItem("GatekeepersProtocol/Arms/Show weapon/PriC", false, 34)] public static void ShowPriC() => ShowWeapon(WeaponId.PriC);

        #endregion

        #region Private

        static string MeshAssetFor(WeaponView.ArmsClass c) => OutDir + "/Arms." + c + ".asset";
        static string OtherMeshAssetFor(WeaponView.ArmsClass c) => OutDir + "/Arms." + c + ".Other.asset";

        static void ShowWeapon(WeaponId id)
        {
            if (!Application.isPlaying) { Debug.LogWarning("[ArmsCutter] Show weapon: enter play mode first."); return; }
            var gun = Object.FindFirstObjectByType<Game.PlayerGun>();
            if (gun == null) { Debug.LogWarning("[ArmsCutter] Show weapon: no PlayerGun in the scene."); return; }
            foreach (WeaponSlot slot in new[] { WeaponSlot.Primary, WeaponSlot.Secondary })
            {
                bool inSlot = false;
                foreach (var w in WeaponCatalog.ForSlot(slot)) if (w == id) inSlot = true;
                if (!inSlot) continue;
                Game.GameController.SetLoadout((int)slot, (int)id, 0);
                gun.SelectSlot(slot);
                Debug.Log("[ArmsCutter] Showing " + id + " in slot " + slot);
                return;
            }
            Debug.LogWarning("[ArmsCutter] Show weapon: " + id + " is in no slot.");
        }

        void FindRig()
        {
            if (_rig != null) return;
            _rig = GameObject.Find(RigName);
            if (_rig == null) _status = "No " + RigName + " in the scene — run Step 1 first.";
        }

        void SpawnRig()
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            if (src == null) { _status = "Not found: " + SourceFbx + " (the FaxLab3D pack is not in git; re-import it)."; return; }
            _rig = (GameObject)PrefabUtility.InstantiatePrefab(src);
            _rig.name = RigName;
            Undo.RegisterCreatedObjectUndo(_rig, "Spawn posing rig");
            Selection.activeGameObject = _rig;
            _status = "Spawned. Put an instance of the class's weapon wrapper in its hands, pose, then bake. " +
                      "Step 0 removes the rig afterwards.";
        }

        void CurlFingers()
        {
            if (_rig == null) return;
            var smr = _rig.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) { _status = "No SkinnedMeshRenderer under the rig."; return; }
            int n = 0;
            foreach (Transform b in smr.bones)
            {
                if (b == null) continue;
                string name = b.name;
                float x = 0f, y = 0f;
                if (name.StartsWith("c_thumb")) { y = ThumbYaw; x = Knuckle3; }
                else if (name.EndsWith("1") && IsDigit(name)) x = Knuckle1;
                else if (name.EndsWith("2") && IsDigit(name)) x = Knuckle2;
                else if (name.EndsWith("3") && IsDigit(name)) x = Knuckle3;
                else continue;
                Undo.RecordObject(b, "Curl fingers");
                b.localRotation *= Quaternion.Euler(x, y, 0f);
                n++;
            }
            _status = "Curled " + n + " digit bones. This is a guess — adjust by hand against the weapon before baking.";
        }

        static bool IsDigit(string bone) =>
            bone.StartsWith("c_index") || bone.StartsWith("c_middle") ||
            bone.StartsWith("c_ring") || bone.StartsWith("c_pinky");

        void Bake()
        {
            if (_rig == null) return;
            var smr = _rig.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null) { _status = "No SkinnedMeshRenderer under the rig."; return; }
            Mesh source = smr.sharedMesh;
            if (source == null) { _status = "SkinnedMeshRenderer has no sharedMesh."; return; }

            Transform[] bones = smr.bones;
            Transform handL = null, handR = null;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                if (bones[i].name == "hand.l") handL = bones[i];
                if (bones[i].name == "hand.r") handR = bones[i];
            }
            if (handL == null || handR == null) { _status = "Rig has no hand.l / hand.r bones."; return; }

            // Which gun, which hand: the gun closest to either hand, then the hand closest to
            // it. The rig can be posed left- or right-handed; the cutter follows the gun.
            Transform gun = _gunPivot;
            Transform nearest = FindGunInScene(handL.position, handR.position);
            if (gun == null) gun = nearest;
            else
            {
                float d = Mathf.Min(Vector3.Distance(gun.position, handL.position), Vector3.Distance(gun.position, handR.position));
                if (UnderGrip(gun) || d > GunReach)
                {
                    Debug.LogWarning("[ArmsCutter] Gun pivot " + gun.name + " is " + d.ToString("0.00") +
                        " m from the nearest hand (or under a Grip) - not the gun the rig was posed against. " +
                        (nearest != null ? "Using " + nearest.name + " instead." : "No posing gun found; falling back to the hand bone."));
                    gun = nearest;
                }
            }
            Transform pivotBone = handR;
            if (gun != null && Vector3.Distance(gun.position, handL.position) < Vector3.Distance(gun.position, handR.position))
                pivotBone = handL;
            string side = pivotBone == handL ? ".l" : ".r";
            string otherSide = side == ".l" ? ".r" : ".l";
            float frameNoteDistance = gun != null ? Vector3.Distance(gun.position, pivotBone.position) : 0f;

            var keepBone = SideBones(bones, side);
            var keepOther = BothArms ? SideBones(bones, otherSide) : null;
            if (keepBone.Count == 0) { _status = "No arm bones matched — check the rig names."; return; }

            // BakeMesh preserves vertex order, so sharedMesh's weights index the baked verts.
            // Baked verts are in the renderer's local space; move them into world and then
            // into the gun's frame so parenting the prop at identity reproduces the pose.
            var baked = new Mesh();
            smr.BakeMesh(baked, false);
            Transform smrT = smr.transform;
            string frameNote;
            Matrix4x4 toProp = GunFrame(gun, out frameNote);
            if (gun != null) frameNote += " at " + frameNoteDistance.ToString("0.00") + " m from hand" + side;
            else
            {
                toProp = Matrix4x4.Translate(-pivotBone.position);
                Debug.LogWarning("[ArmsCutter] No gun pivot: baking in the hand" + side + " frame. The prop will " +
                    "not line up with the weapon in play. Put a weapon wrapper in the rig's hands.");
            }
            BoneWeight[] weights = source.boneWeights;
            if (weights.Length != baked.vertexCount)
            {
                _status = "Weight/vertex mismatch (" + weights.Length + " vs " + baked.vertexCount + ").";
                return;
            }

            Vector3[] srcVerts = baked.vertices;
            Vector3[] srcNormals = baked.normals;
            for (int v = 0; v < srcVerts.Length; v++)
                srcVerts[v] = toProp.MultiplyPoint3x4(smrT.TransformPoint(srcVerts[v]));
            if (srcNormals != null)
                for (int v = 0; v < srcNormals.Length; v++)
                    srcNormals[v] = toProp.MultiplyVector(smrT.TransformDirection(srcNormals[v])).normalized;
            Vector2[] srcUv = baked.uv;

            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            Cut(baked, weights, keepBone, srcVerts, srcNormals, srcUv, verts, normals, uv, tris);
            if (tris.Count == 0) { _status = "Kept 0 triangles. Lower the weight threshold and bake again."; return; }
            List<Vector3> vertsO = null, normalsO = null; List<Vector2> uvO = null; List<int> trisO = null;
            if (keepOther != null)
            {
                vertsO = new List<Vector3>(); normalsO = new List<Vector3>(); uvO = new List<Vector2>(); trisO = new List<int>();
                Cut(baked, weights, keepOther, srcVerts, srcNormals, srcUv, vertsO, normalsO, uvO, trisO);
            }

            // Re-bakes write into the existing mesh asset so the prefab keeps its reference.
            string meshPath = MeshAssetFor(_class), otherPath = OtherMeshAssetFor(_class);
            Mesh arms = WriteMesh(meshPath, "Arms." + _class, verts, normals, uv, tris);
            Mesh other = (trisO != null && trisO.Count > 0)
                ? WriteMesh(otherPath, "Arms." + _class + ".Other", vertsO, normalsO, uvO, trisO)
                : null;

            // The prefab root stays ACTIVE so it previews and can be dropped in a scene. Under
            // it, one child per class; this bake rewrites only its own class child. Hands are
            // named by the hand they actually are, each its own MeshRenderer, so either can be
            // ticked off in the prefab and EnableOnly (which toggles class children only)
            // leaves it off in play.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            bool haveExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAsset) != null;
            GameObject go = haveExisting ? PrefabUtility.LoadPrefabContents(PrefabAsset) : new GameObject("Arms");
            try
            {
                EnsureClassLayout(go);
                string className = WeaponView.ArmsClassName(_class);
                Transform old = go.transform.Find(className);
                if (old != null) DestroyImmediate(old.gameObject);
                var cls = new GameObject(className);
                cls.transform.SetParent(go.transform, false);

                string gunName = side == ".l" ? "Hand.L" : "Hand.R";
                string otherName = side == ".l" ? "Hand.R" : "Hand.L";
                var gunHand = new GameObject(gunName);
                gunHand.transform.SetParent(cls.transform, false);
                GameObject otherHand = null;
                if (_armMode == ArmMode.GunHandMirroredForOther)
                {
                    otherHand = new GameObject(otherName);
                    otherHand.transform.SetParent(cls.transform, false);
                    otherHand.transform.localScale = new Vector3(-1f, 1f, 1f);
                }
                else if (other != null)
                {
                    otherHand = new GameObject(otherName);
                    otherHand.transform.SetParent(cls.transform, false);
                }
                foreach (GameObject hand in new[] { gunHand, otherHand })
                {
                    if (hand == null) continue;
                    hand.AddComponent<MeshFilter>().sharedMesh = (hand == otherHand && other != null) ? other : arms;
                    var mr = hand.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                PrefabUtility.SaveAsPrefabAsset(go, PrefabAsset);
            }
            finally
            {
                if (haveExisting) PrefabUtility.UnloadPrefabContents(go); else DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // A re-save can give the prefab root a new fileID, which would leave Boarder.prefab's
            // m_IsActive:0 override pointing at nothing and put arms on remote avatars. Re-wire
            // after every bake so the override always targets the live root.
            MenuWire();

            _status = "Baked " + (tris.Count / 3) + " triangles, " + verts.Count + " verts" +
                      (trisO != null ? " + other arm " + (trisO.Count / 3) + " tris" : "") +
                      ", " + _armMode + " (gun hand" + side + "), " + frameNote + ".\n" +
                      "Wrote " + meshPath + " and the " + _class + " child of " + PrefabAsset + ".";
        }

        static Mesh WriteMesh(string path, string name, List<Vector3> verts, List<Vector3> normals, List<Vector2> uv, List<int> tris)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh { name = name };
            else mesh.Clear();
            mesh.SetVertices(verts);
            if (normals.Count == verts.Count) mesh.SetNormals(normals);
            if (uv.Count == verts.Count) mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            if (normals.Count != verts.Count) mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            if (fresh) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // The first bakes put Hand.L / Hand.R straight under the root and the mesh at
        // Arms.asset. Fold that into the Pistol class child once; MoveAsset keeps the mesh
        // guid so the prefab's references survive. Idempotent.
        static void EnsureClassLayout(GameObject root)
        {
            var pistol = WeaponView.ArmsClass.Pistol;
            string pistolName = WeaponView.ArmsClassName(pistol);
            bool legacy = false;
            for (int i = 0; i < root.transform.childCount; i++)
                if (root.transform.GetChild(i).name.StartsWith("Hand.")) legacy = true;
            if (!legacy) return;
            Transform cls = root.transform.Find(pistolName);
            if (cls == null) { cls = new GameObject(pistolName).transform; cls.SetParent(root.transform, false); }
            var move = new List<Transform>();
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform c = root.transform.GetChild(i);
                if (c.name.StartsWith("Hand.")) move.Add(c);
            }
            foreach (Transform c in move) c.SetParent(cls, false);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(OutDir + "/Arms.asset") != null
                && AssetDatabase.LoadAssetAtPath<Mesh>(MeshAssetFor(pistol)) == null)
                AssetDatabase.MoveAsset(OutDir + "/Arms.asset", MeshAssetFor(pistol));
            if (AssetDatabase.LoadAssetAtPath<Mesh>(OutDir + "/Arms.Other.asset") != null
                && AssetDatabase.LoadAssetAtPath<Mesh>(OtherMeshAssetFor(pistol)) == null)
                AssetDatabase.MoveAsset(OutDir + "/Arms.Other.asset", OtherMeshAssetFor(pistol));
            Debug.Log("[ArmsCutter] Migrated legacy Hand.* children into the " + pistolName + " class child.");
        }

        // World -> gun-space matrix. Any Packs/Weapons wrapper instance is gun space at its
        // root (barrel +Z, top +Y; the fbx child carries that weapon's own correction and
        // shift) and is used directly. A raw pack fbx is corrected against the Mk1 wrapper's
        // child and only holds when it is the SAME asset the wrapper nests; the pack's own
        // SM_Pistol_Energy at a different import came out rolled - palm up in play.
        static Matrix4x4 GunFrame(Transform pivot, out string note)
        {
            note = "frame: hand bone";
            if (pivot == null) return Matrix4x4.identity;
            if (IsWeaponWrapper(pivot.gameObject))
            {
                note = "frame: wrapper root '" + pivot.name + "' (" + System.IO.Path.GetFileName(SourcePath(pivot.gameObject)) + ")";
                return pivot.worldToLocalMatrix;
            }
            var mk1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Packs/Weapons/Mk1.prefab");
            Transform fbxChild = (mk1 != null && mk1.transform.childCount > 0) ? mk1.transform.GetChild(0) : null;
            if (fbxChild == null)
            {
                note = "frame: '" + pivot.name + "' as-is (no Mk1 wrapper to read a correction from)";
                return pivot.worldToLocalMatrix;
            }
            string pivotSrc = SourcePath(pivot.gameObject);
            string childSrc = SourcePath(fbxChild.gameObject);
            if (string.IsNullOrEmpty(pivotSrc) || pivotSrc != childSrc)
                Debug.LogWarning("[ArmsCutter] Posing gun '" + pivot.name + "' comes from '" + pivotSrc +
                    "' but the Mk1 wrapper nests '" + childSrc + "'. Same model, different import - the frame " +
                    "will not match the in-game gun. Pose against an instance of a Packs/Weapons wrapper prefab instead.");
            Matrix4x4 childLocal = Matrix4x4.TRS(fbxChild.localPosition, fbxChild.localRotation, fbxChild.localScale);
            Matrix4x4 rootWorld = pivot.localToWorldMatrix * childLocal.inverse;
            note = "frame: raw fbx '" + pivot.name + "' (" + System.IO.Path.GetFileName(pivotSrc) + ") with the Mk1 wrapper correction undone";
            return rootWorld.inverse;
        }

        // The posing gun: a Packs/Weapons wrapper instance within reach of either hand beats
        // a raw SM_Pistol_Energy even if the raw one is closer. Anything with a Grip in its
        // ancestry is a Boarder's own weapon, never the posing gun.
        static Transform FindGunInScene(Vector3 handA, Vector3 handB)
        {
            Transform bestWrapper = null, bestRaw = null; float dWrapper = float.MaxValue, dRaw = float.MaxValue;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || !go.scene.IsValid() || !go.scene.isLoaded || EditorUtility.IsPersistent(go)) continue;
                if (UnderGrip(go.transform)) continue;
                bool wrapper = IsWeaponWrapper(go);
                bool raw = go.name == "SM_Pistol_Energy";
                if (!wrapper && !raw) continue;
                float d = Mathf.Min(Vector3.Distance(go.transform.position, handA), Vector3.Distance(go.transform.position, handB));
                if (wrapper && d < dWrapper) { dWrapper = d; bestWrapper = go.transform; }
                if (raw && d < dRaw) { dRaw = d; bestRaw = go.transform; }
            }
            if (bestWrapper != null && dWrapper <= GunReach) return bestWrapper;
            return bestRaw != null ? bestRaw : bestWrapper;
        }

        static Vector3 RigHandPosition(string bone)
        {
            var rig = GameObject.Find(RigName);
            if (rig == null) return Vector3.zero;
            Transform h = FindNamed(rig.transform, bone);
            return h != null ? h.position : rig.transform.position;
        }

        static string SourcePath(GameObject go)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            return src == null ? "" : AssetDatabase.GetAssetPath(src);
        }

        // The root of an instance of Assets/Packs/Weapons/<Weapon>.prefab. Only the root: the
        // nested fbx child is an instance of the fbx, not of the wrapper.
        static bool IsWeaponWrapper(GameObject go)
        {
            string path = SourcePath(go);
            if (!path.StartsWith("Assets/Packs/Weapons/") || !path.EndsWith(".prefab")) return false;
            return PrefabUtility.GetNearestPrefabInstanceRoot(go) == go;
        }

        static bool UnderGrip(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
                if (p.name == "Grip") return true;
            return false;
        }

        static HashSet<int> SideBones(Transform[] bones, string side)
        {
            var keep = new HashSet<int>();
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                string name = bones[i].name;
                if (!name.EndsWith(side)) continue;
                for (int d = 0; d < DefaultBones.Length; d++)
                {
                    if (!name.StartsWith(DefaultBones[d])) continue;
                    keep.Add(i);
                    break;
                }
            }
            return keep;
        }

        // Keep a triangle only when every corner is arm - a partial triangle drags torso
        // geometry into frame, which is the whole thing this cut exists to avoid.
        void Cut(Mesh baked, BoneWeight[] weights, HashSet<int> keepBone,
            Vector3[] srcVerts, Vector3[] srcNormals, Vector2[] srcUv,
            List<Vector3> verts, List<Vector3> normals, List<Vector2> uv, List<int> tris)
        {
            bool[] isArm = new bool[baked.vertexCount];
            for (int v = 0; v < weights.Length; v++)
            {
                BoneWeight w = weights[v];
                float armWeight = 0f;
                if (keepBone.Contains(w.boneIndex0)) armWeight += w.weight0;
                if (keepBone.Contains(w.boneIndex1)) armWeight += w.weight1;
                if (keepBone.Contains(w.boneIndex2)) armWeight += w.weight2;
                if (keepBone.Contains(w.boneIndex3)) armWeight += w.weight3;
                isArm[v] = armWeight >= _weightThreshold;
            }
            var remap = new Dictionary<int, int>();
            for (int sub = 0; sub < baked.subMeshCount; sub++)
            {
                int[] indices = baked.GetTriangles(sub);
                for (int t = 0; t + 2 < indices.Length; t += 3)
                {
                    int a = indices[t], b = indices[t + 1], c = indices[t + 2];
                    if (!isArm[a] || !isArm[b] || !isArm[c]) continue;
                    tris.Add(Emit(a, remap, verts, normals, uv, srcVerts, srcNormals, srcUv));
                    tris.Add(Emit(b, remap, verts, normals, uv, srcVerts, srcNormals, srcUv));
                    tris.Add(Emit(c, remap, verts, normals, uv, srcVerts, srcNormals, srcUv));
                }
            }
        }

        static int Emit(int v, Dictionary<int, int> remap, List<Vector3> verts, List<Vector3> normals,
            List<Vector2> uv, Vector3[] srcVerts, Vector3[] srcNormals, Vector2[] srcUv)
        {
            int mapped;
            if (remap.TryGetValue(v, out mapped)) return mapped;
            mapped = verts.Count;
            remap[v] = mapped;
            verts.Add(srcVerts[v]);
            if (srcNormals != null && srcNormals.Length == srcVerts.Length) normals.Add(srcNormals[v]);
            if (srcUv != null && srcUv.Length == srcVerts.Length) uv.Add(srcUv[v]);
            return mapped;
        }

        static Transform FindNamed(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform f = FindNamed(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        #endregion

        #region Nested Types

        public enum ArmMode { BothArmsAsPosed, GunHandOnly, GunHandMirroredForOther }

        #endregion
    }
}
