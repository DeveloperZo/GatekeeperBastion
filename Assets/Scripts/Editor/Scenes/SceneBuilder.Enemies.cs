using System;
using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>SceneBuilder, enemy assets: the four gatekeeper prefabs and the materials that make
    /// them tellable apart. Same idempotent contract as the rest of the builder — each prefab is
    /// saved to a fixed path so re-running keeps its GUID and any scene reference to it.</summary>
    /// <remarks>
    /// COLOUR AND SIZE ARE THE ONLY TUTORIAL THIS GAME HAS. A player meets several kinds at once
    /// and has to triage them before knowing any of their names, so the read is built from two
    /// channels that survive a dark corridor and a glance:
    ///   Default — the red they already know, at 1.0 of the base spread.
    ///   Hunter  — bright amber, 0.85. Small and loud: the fast fragile one coming for you.
    ///   Thief   — saturated violet-steel, 1.4. Well over half again the silhouette of a Hunter.
    ///   Cloaker — cold teal, 1.0 (Default's footprint exactly — its tell is the cloak and the
    ///             freeze, not a new silhouette size; Docs/Milestones, "the one that stops when
    ///             you look").
    /// Those are RELATIVE; BodyScale multiplies all three, and the finale elite multiplies again
    /// on top of the Thief (MasterControl.FinaleEliteScale).
    /// Thief's violet is deliberately brighter and more saturated than the bastion's own walls
    /// (0.22, 0.17, 0.30) — a genuinely dark violet body against a dark violet hull is invisible,
    /// which is the opposite of the point. Its size does the rest of the work.
    /// Silhouette is one FaxLab3D robot per kind (Default UtilityBot, Hunter Hunter, Thief
    /// Exterminator), which is what separates them on sight. The per-kind emission tint still
    /// rides on top, on each robot's authored glow regions, written by EnsureWrapperEmission.
    ///
    /// NUMBERS DO NOT LIVE HERE. Everything baked below (hp, agent speed) is read from
    /// WaveDirector.StatsFor so the saved asset matches the sim, and every one of them is pushed
    /// again onto the live instance by WaveSpawner at spawn (Docs/Architecture.md GOTCHA 1). Only the
    /// things the sim has no opinion about — colour, scale, collider geometry — are authored here.
    /// </remarks>
    public static partial class SceneBuilder
    {
        const string HunterPrefabPath = PrefabDir + "/HunterGatekeeper.prefab";
        const string ThiefPrefabPath = PrefabDir + "/ThiefGatekeeper.prefab";
        const string CloakerPrefabPath = PrefabDir + "/CloakerGatekeeper.prefab";
        const string GatekeeperPackDir = "Assets/Packs/Gatekeepers";
        const string GatekeeperControllerPath = GatekeeperPackDir + "/Gatekeeper.controller";
        const string DefaultWrapperPath = GatekeeperPackDir + "/Default.prefab";
        const string HunterWrapperPath = GatekeeperPackDir + "/Hunter.prefab";
        const string ThiefWrapperPath = GatekeeperPackDir + "/Thief.prefab";
        const string CloakerWrapperPath = GatekeeperPackDir + "/Cloaker.prefab";
        const string CloakerCloakedMatPath = GatekeeperPackDir + "/Cloaker_Cloaked.mat";
        // One robot model per kind. Silhouette is what tells the kinds apart now; before this
        // they were three Synty humanoids separated by an emission tint, which asked the player to
        // read a colour on a body they had already learned.
        const string RobotDir = "Assets/FaxLab3D/SciFiRobotCharactersPack/";
        const string DefaultFbx = RobotDir + "Models/SK_UtilityBot.fbx";
        const string HunterFbx = RobotDir + "Models/SK_Hunter.fbx";
        const string ThiefFbx = RobotDir + "Models/SK_Exterminator.fbx";
        const string CloakerFbx = RobotDir + "Models/SK_Tactical.fbx";
        const string PackDefault = RobotDir + "Prefabs/UtilityBot_Base.prefab";
        const string PackHunter = RobotDir + "Prefabs/Hunter_Base.prefab";
        const string PackThief = RobotDir + "Prefabs/Exterminator_Base.prefab";
        const string PackCloaker = RobotDir + "Prefabs/Tactical_Base.prefab";
        // BODY SCALE PER KIND. The relative spread (1 : 0.85 : 1.4) is the authored read;
        // BodyScale shrinks all three together so the finale elite towers over them
        // (Zo, 2026-08-20 — the elite did not read as bigger, because until this change
        // NO kind's body scaled at all: see EnsureVisualChild).
        const float BodyScale = 0.75f;
        const float DefaultScale = 1f * BodyScale;
        const float HunterScale = 0.85f * BodyScale;
        const float ThiefScale = 1.4f * BodyScale;
        // Identical to Default on purpose (milestone criterion 5): same footprint, same agent
        // radius/height/baseOffset, so no navmesh assumption changes with the fourth kind.
        const float CloakerScale = 1f * BodyScale;
        const string ClipIdle = "Assets/Packs/Boarder/Anim_PlayerIdle.fbx";
        const string ClipWalk = "Assets/Packs/Boarder/Anim_Walking.fbx";
        const string ClipRun = "Assets/Packs/Boarder/Anim_PlayerRun.fbx";
        const string ClipAttack = GatekeeperPackDir + "/Animations/Anim_attack_melee.fbx";
        const string ClipFlinch = GatekeeperPackDir + "/Animations/Anim_flinch.anim";
        const string ClipDeath = GatekeeperPackDir + "/Animations/Anim_death_falling_back.fbx";
        const string UpperBodyMaskPath = GatekeeperPackDir + "/UpperBody.mask";

        /// <summary>The four gatekeeper prefabs, wired into the WaveSpawner as a set.</summary>
        sealed class EnemyPrefabs
        {
            public GameObject Default, Hunter, Thief, Cloaker;
        }

        static EnemyPrefabs CreateEnemyPrefabs(Palette pal)
        {
            bool dress = GatekeeperModelsRetarget(out string reason);
            if (!dress)
            {
                Debug.LogError(
                    "[SceneBuilder] clips will not retarget onto the gatekeeper models (" +
                    reason + ") — keeping capsules.");
            }
            else
            {
                EnsureGatekeeperWrappers(pal);
            }

            return new EnemyPrefabs
            {
                Default = CreateGatekeeperPrefab<EnemyGatekeeper>(
                    "EnemyGatekeeper", EnemyPrefabPath, pal.Enemy, GatekeeperKind.Default, DefaultScale, dress),
                Hunter = CreateGatekeeperPrefab<HunterGatekeeper>(
                    "HunterGatekeeper", HunterPrefabPath, pal.Hunter, GatekeeperKind.Hunter, HunterScale, dress),
                Thief = CreateGatekeeperPrefab<ThiefGatekeeper>(
                    "ThiefGatekeeper", ThiefPrefabPath, pal.Thief, GatekeeperKind.Thief, ThiefScale, dress),
                Cloaker = CreateGatekeeperPrefab<CloakerGatekeeper>(
                    "CloakerGatekeeper", CloakerPrefabPath, pal.Cloaker, GatekeeperKind.Cloaker, CloakerScale, dress),
            };
        }

        /// <summary>One gatekeeper prefab: capsule root, NavMeshAgent, Damageable, the kind's own
        /// EnemyGatekeeper subclass, and the net components (inert until a session Spawn()s).
        /// If the asset already exists, load and dress it — never CreatePrimitive-wipe.
        /// <paramref name="scale"/> scales the silhouette AND the agent footprint together,
        /// so a Thief takes up the room it looks like it takes up. The visual child rides the
        /// root's scale unchanged — see DressVisual for the inverse that used to cancel it.</summary>
        static GameObject CreateGatekeeperPrefab<T>(string name, string path, Material material,
                                                 GatekeeperKind kind, float scale, bool dress)
            where T : EnemyGatekeeper
        {
            GatekeeperStats stats = WaveDirector.StatsFor(kind);
            GameObject wrapper = dress
                ? AssetDatabase.LoadAssetAtPath<GameObject>(WrapperPath(kind))
                : null;
            bool canDress = wrapper != null;
            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

            GameObject go;
            if (existed)
            {
                go = PrefabUtility.LoadPrefabContents(path);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = name;
            }

            go.name = name;
            go.transform.localScale = Vector3.one * scale;

            var agent = GetOrAdd<NavMeshAgent>(go);
            // Speed is baked from the kind's patrol speed for consistency only; WaveSpawner pushes
            // it (and the chase speed) again through EnemyGatekeeper.Configure at spawn.
            agent.speed = stats.patrolSpeed;
            agent.acceleration = 14f;
            agent.angularSpeed = 400f;
            // Geometry follows the capsule: a 1.4-scale Thief is 1.4x as wide and as tall, and its
            // origin sits at the capsule's centre, so baseOffset must match the half-height or the
            // body sinks into (or floats over) the floor.
            agent.radius = 0.4f * scale;
            agent.height = 2f * scale;
            agent.baseOffset = scale;
            agent.stoppingDistance = 1.6f;

            GetOrAdd<CapsuleCollider>(go);
            // Baked at the kind's own maxHp so the saved prefab matches the tuned baseline.
            // WaveSpawner still sets HP absolutely from the same source at spawn time, so the live
            // value can never drift with whatever this asset happens to carry.
            GetOrAdd<Damageable>(go).SetMaxHp(Mathf.Max(1f, stats.maxHp));
            GetOrAdd<T>(go);
            GetOrAdd<NetworkObject>(go);
            var netTransform = GetOrAdd<NetworkTransform>(go);
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            GetOrAdd<NetEnemy>(go);

            if (canDress)
            {
                DestroyCapsuleMesh(go);
                DressVisual(go, kind, wrapper);
                // The Cloaker's two-material swap rides a separate component (NetEnemy disables
                // the AI on clones), and its references are SERIALIZED here, not resolved at
                // runtime — the prefab-freeze rule.
                if (kind == GatekeeperKind.Cloaker) WireCloakerPresentation(go);
            }
            else if (!existed)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }

            GameObject prefab;
            if (existed)
            {
                PrefabUtility.SaveAsPrefabAsset(go, path);
                PrefabUtility.UnloadPrefabContents(go);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            else
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                UnityEngine.Object.DestroyImmediate(go);
            }
            return prefab;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        static void DestroyCapsuleMesh(GameObject go)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) UnityEngine.Object.DestroyImmediate(renderer);
            var filter = go.GetComponent<MeshFilter>();
            if (filter != null) UnityEngine.Object.DestroyImmediate(filter);
        }

        static void DressVisual(GameObject root, GatekeeperKind kind, GameObject wrapper)
        {
            string childName = VisualChildName(kind);
            Transform existing = root.transform.Find(childName);
            GameObject visual;
            if (existing != null)
            {
                visual = existing.gameObject;
            }
            else
            {
                visual = (GameObject)PrefabUtility.InstantiatePrefab(wrapper, root.transform);
                visual.name = childName;
            }

            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            // NO COUNTER-SCALE. This used to be 1/scale, to stop Synty's SM_Chr_BR_War_Robot_01
            // — which carries its own 1.4 — stacking a second 1.4 on the root. The robot pack's
            // wrappers are all native scale 1, so that inverse had nothing left to cancel except
            // the kind scale itself: every kind rendered at exactly 1.0 and the size channel the
            // enemy tutorial claims to use did nothing (Zo, 2026-08-20). The body now wears the
            // root's scale, which is what makes a Thief look like the room it takes up.
            visual.transform.localScale = Vector3.one;
        }

        static string WrapperPath(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return HunterWrapperPath;
            if (kind == GatekeeperKind.Thief) return ThiefWrapperPath;
            if (kind == GatekeeperKind.Cloaker) return CloakerWrapperPath;
            return DefaultWrapperPath;
        }

        static string PackPrefabPath(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return PackHunter;
            if (kind == GatekeeperKind.Thief) return PackThief;
            if (kind == GatekeeperKind.Cloaker) return PackCloaker;
            return PackDefault;
        }

        static string PackModelPath(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return HunterFbx;
            if (kind == GatekeeperKind.Thief) return ThiefFbx;
            if (kind == GatekeeperKind.Cloaker) return CloakerFbx;
            return DefaultFbx;
        }

        static string VisualChildName(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return "Hunter";
            if (kind == GatekeeperKind.Thief) return "Thief";
            if (kind == GatekeeperKind.Cloaker) return "Cloaker";
            return "Default";
        }

        /// <summary>Serialize the Cloaker's presentation references into the saved prefab: the
        /// body's skinned renderers and the two committed materials. An unwired presentation is
        /// a silent no-op at runtime, so missing materials are an ERROR here, not a fallback —
        /// same policy as EnsureWrapperEmission.</summary>
        static void WireCloakerPresentation(GameObject root)
        {
            var presentation = GetOrAdd<CloakerPresentation>(root);
            var solid = AssetDatabase.LoadAssetAtPath<Material>(
                GatekeeperPackDir + "/" + VisualChildName(GatekeeperKind.Cloaker) + ".mat");
            var cloaked = AssetDatabase.LoadAssetAtPath<Material>(CloakerCloakedMatPath);
            if (solid == null || cloaked == null)
            {
                Debug.LogError("[SceneBuilder] Cloaker material missing ("
                    + (solid == null ? "Cloaker.mat" : "Cloaker_Cloaked.mat")
                    + ") — cloak swap left unwired.");
                return;
            }
            presentation.renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            presentation.solidMaterial = solid;
            presentation.cloakedMaterial = cloaked;
        }

        static bool GatekeeperModelsRetarget(out string reason)
        {
            // Gate on the models the kinds are actually dressed from. Checking a fixed pair of
            // Synty FBXs kept passing after the kinds moved off them, which is a guard that reports
            // green about files nothing renders.
            if (!FbxIsHuman(DefaultFbx, out reason)) return false;
            if (!FbxIsHuman(HunterFbx, out reason)) return false;
            if (!FbxIsHuman(ThiefFbx, out reason)) return false;
            if (!FbxIsHuman(CloakerFbx, out reason)) return false;
            AnimationClip walk = LoadHumanClip(ClipWalk);
            if (walk == null)
            {
                reason = ClipWalk + " missing clip";
                return false;
            }
            if (!walk.isHumanMotion)
            {
                reason = ClipWalk + " is not Humanoid";
                return false;
            }
            reason = null;
            Debug.Log("[SceneBuilder] gatekeeper models retarget: all four avatars are Humanoid.");
            return true;
        }

        static bool FbxIsHuman(string path, out string reason)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                reason = path + " missing";
                return false;
            }
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                reason = path + " animationType=" + importer.animationType;
                return false;
            }

            Avatar avatar = null;
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                avatar = assets[i] as Avatar;
                if (avatar != null) break;
            }
            if (avatar == null)
            {
                reason = path + " has no Avatar";
                return false;
            }
            if (!avatar.isValid || !avatar.isHuman)
            {
                reason = path + " avatar isHuman=" + avatar.isHuman + " isValid=" + avatar.isValid;
                return false;
            }
            reason = null;
            return true;
        }

        static void EnsureGatekeeperWrappers(Palette pal)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Packs"))
                AssetDatabase.CreateFolder("Assets", "Packs");
            if (!AssetDatabase.IsValidFolder(GatekeeperPackDir))
                AssetDatabase.CreateFolder("Assets/Packs", "Gatekeepers");

            AnimatorController controller = EnsureGatekeeperController();
            if (controller == null) return;

            EnsureWrapper(GatekeeperKind.Default, controller);
            EnsureWrapper(GatekeeperKind.Hunter, controller);
            EnsureWrapper(GatekeeperKind.Thief, controller);
            EnsureWrapper(GatekeeperKind.Cloaker, controller);

            // Colour is the tutorial: kind-tinted emission on the pack's authored glow regions.
            // Runs after EnsureWrapper every build, so an already-dressed wrapper still converges.
            EnsureWrapperEmission(GatekeeperKind.Default, pal.Enemy.color);
            EnsureWrapperEmission(GatekeeperKind.Hunter, pal.Hunter.color);
            EnsureWrapperEmission(GatekeeperKind.Thief, pal.Thief.color);
            EnsureWrapperEmission(GatekeeperKind.Cloaker, pal.Cloaker.color);
        }

        /// <summary>Assigns the kind's authored material to every renderer slot in its wrapper and
        /// writes the palette tint into whichever emission colour the material's shader exposes.
        /// The material is a committed asset per kind, not built here.</summary>
        static void EnsureWrapperEmission(GatekeeperKind kind, Color tint)
        {
            string matPath = GatekeeperPackDir + "/" + VisualChildName(kind) + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                // Deliberately an error rather than a rebuild. Synthesising one from a pack material
                // is how a kind quietly ends up wearing another pack's atlas.
                Debug.LogError("[SceneBuilder] gatekeeper material missing: " + matPath);
                return;
            }

            mat.SetFloat("_Enable_Emission", 1f);
            // Three spellings across the shaders this has worn: the Synty graph exposes the first
            // two, the robot graph the third. Setting all three keeps the tint working through a
            // shader change instead of failing silently on one name.
            // Live on the Built-In Standard materials: each carries its robot's emissive map, so
            // this tints the authored glow regions and leaves the body alone. The material ships
            // with white here, which shows the map's own colour if a build never runs.
            mat.SetColor("_Emission_Color", tint);
            mat.SetColor("_EmissionColor", tint);
            mat.SetColor("_EmissiveColor", tint);
            EditorUtility.SetDirty(mat);

            string wrapperPath = WrapperPath(kind);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(wrapperPath);
            bool changed = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = renderers[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == mat) continue;
                    mats[m] = mat;
                    changed = true;
                }
                renderers[i].sharedMaterials = mats;
            }
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, wrapperPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void EnsureWrapper(GatekeeperKind kind, AnimatorController controller)
        {
            string path = WrapperPath(kind);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && WrapperLooksDressed(existing, kind)) return;

            string packPath = PackPrefabPath(kind);
            GameObject pack = AssetDatabase.LoadAssetAtPath<GameObject>(packPath);
            if (pack == null)
            {
                Debug.LogError("[SceneBuilder] pack prefab missing: " + packPath);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(pack);
            PrefabUtility.UnpackPrefabInstance(
                instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = VisualChildName(kind);

            StripInactivePackMeshes(instance);
            BindAnimator(instance, controller, kind);

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        /// <summary>Dressed means dressed from the kind's CURRENT model. The old test asked only
        /// whether a skinned mesh existed, so a wrapper built from a source that has since changed
        /// reported dressed forever and the swap never took.</summary>
        static bool WrapperLooksDressed(GameObject wrapper, GatekeeperKind kind)
        {
            SkinnedMeshRenderer[] skins = wrapper.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length == 0) return false;
            string model = PackModelPath(kind);
            for (int i = 0; i < skins.Length; i++)
            {
                Mesh mesh = skins[i].sharedMesh;
                if (mesh == null) return false;
                if (AssetDatabase.GetAssetPath(mesh) != model) return false;
            }
            return true;
        }

        static void StripInactivePackMeshes(GameObject root)
        {
            var toDestroy = new List<GameObject>();
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i].gameObject;
                if (go == root) continue;
                if (!go.name.StartsWith("SM_Chr_", StringComparison.Ordinal)) continue;
                if (go.activeSelf) continue;
                toDestroy.Add(go);
            }
            for (int i = 0; i < toDestroy.Count; i++)
            {
                if (toDestroy[i] != null)
                    UnityEngine.Object.DestroyImmediate(toDestroy[i]);
            }
        }

        static void BindAnimator(GameObject root, AnimatorController controller, GatekeeperKind kind)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            Animator keep = null;
            for (int i = 0; i < animators.Length; i++)
            {
                Avatar av = animators[i].avatar;
                if (av != null && av.isHuman)
                {
                    keep = animators[i];
                    break;
                }
            }
            if (keep == null && animators.Length > 0) keep = animators[0];
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != keep)
                    UnityEngine.Object.DestroyImmediate(animators[i]);
            }

            if (keep == null) keep = root.AddComponent<Animator>();
            Avatar packAvatar = LoadAvatar(PackModelPath(kind));
            if (keep.avatar == null || !keep.avatar.isHuman)
                keep.avatar = packAvatar;
            keep.runtimeAnimatorController = controller;
            keep.applyRootMotion = false;
        }

        static Avatar LoadAvatar(string fbxPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            for (int i = 0; i < assets.Length; i++)
            {
                var avatar = assets[i] as Avatar;
                if (avatar != null && avatar.isHuman) return avatar;
            }
            return null;
        }

        static AnimatorController EnsureGatekeeperController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(GatekeeperControllerPath);
            if (existing != null)
            {
                // Returning the existing asset used to skip the HitReact layer and the Death state.
                bool dirty = EnsureHitReactLayer(existing);
                if (EnsureDeathState(existing)) dirty = true;
                if (dirty)
                {
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }
                return existing;
            }

            AnimationClip idle = LoadHumanClip(ClipIdle);
            AnimationClip walk = LoadHumanClip(ClipWalk);
            AnimationClip run = LoadHumanClip(ClipRun);
            if (idle == null || walk == null || run == null)
            {
                Debug.LogError("[SceneBuilder] Mixamo Boarder clips missing — cannot build Gatekeeper.controller.");
                return null;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(GatekeeperControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState move = sm.AddState("Locomotion");
            var tree = new BlendTree
            {
                name = "Move",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.5f);
            tree.AddChild(run, 1f);
            move.motion = tree;
            sm.defaultState = move;

            // The melee take is trimmed to a single 0.53s slash, so the exit time sits past its
            // contact frame and the state is back in Locomotion inside the
            // GatekeeperAttackInterval swing clock. Falling back to Idle here would rebuild the
            // stand-in pose the clip replaced, so say so rather than doing it silently.
            AnimationClip attackClip = LoadHumanClip(ClipAttack);
            if (attackClip == null)
            {
                Debug.LogError("[SceneBuilder] " + ClipAttack + " missing clip — Attack falls back to Idle.");
                attackClip = idle;
            }
            AnimatorState attack = sm.AddState("Attack");
            attack.motion = attackClip;
            AnimatorStateTransition toAttack = sm.AddAnyStateTransition(attack);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack.hasExitTime = false;
            toAttack.duration = 0.05f;
            toAttack.canTransitionToSelf = false;
            AnimatorStateTransition back = attack.AddTransition(move);
            back.hasExitTime = true;
            back.hasFixedDuration = true;
            back.exitTime = 0.9f;
            back.duration = 0.08f;

            EnsureHitReactLayer(controller);
            EnsureDeathState(controller);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>Returns true when the controller, clip or mask was created or patched,
        /// so the caller only SaveAssets on a real change — not on every Build All.</summary>
        static bool EnsureHitReactLayer(AnimatorController controller)
        {
            if (controller == null) return false;

            bool dirty = false;
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFlinch);
            if (clip == null)
            {
                clip = new AnimationClip { name = "flinch", frameRate = 30 };
                clip.legacy = false;
                AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Animator),
                    propertyName = "Spine Front-Back"
                }, FlinchMuscleCurve(-0.7f));
                AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Animator),
                    propertyName = "Chest Front-Back"
                }, FlinchMuscleCurve(-0.9f));
                AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Animator),
                    propertyName = "Head Nod Down-Up"
                }, FlinchMuscleCurve(0.45f));
                AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Animator),
                    propertyName = "Left Arm Down-Up"
                }, FlinchMuscleCurve(0.55f));
                AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Animator),
                    propertyName = "Right Arm Down-Up"
                }, FlinchMuscleCurve(0.55f));
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, ClipFlinch);
                dirty = true;
            }

            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    var part = (AvatarMaskBodyPart)i;
                    bool on = part == AvatarMaskBodyPart.Body
                        || part == AvatarMaskBodyPart.Head
                        || part == AvatarMaskBodyPart.LeftArm
                        || part == AvatarMaskBodyPart.RightArm
                        || part == AvatarMaskBodyPart.LeftFingers
                        || part == AvatarMaskBodyPart.RightFingers;
                    mask.SetHumanoidBodyPartActive(part, on);
                }
                AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
                dirty = true;
            }

            bool hasHitReact = false;
            AnimatorControllerParameter[] parms = controller.parameters;
            for (int i = 0; i < parms.Length; i++)
            {
                if (parms[i].name == "HitReact")
                {
                    hasHitReact = true;
                    break;
                }
            }
            if (!hasHitReact)
            {
                controller.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
                dirty = true;
            }

            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "HitReact") return dirty;
            }

            controller.AddLayer("HitReact");
            // Unity copies the layers array; mutate the last element and assign it back.
            layers = controller.layers;
            int last = layers.Length - 1;
            layers[last].defaultWeight = 1f;
            // Override with an empty default writes the bind pose onto every masked bone
            // for the rest of the run. Additive Empty is a no-op.
            layers[last].blendingMode = AnimatorLayerBlendingMode.Additive;
            layers[last].avatarMask = mask;
            controller.layers = layers;

            var sm = controller.layers[controller.layers.Length - 1].stateMachine;
            AnimatorState empty = sm.AddState("Empty");
            empty.writeDefaultValues = false;
            sm.defaultState = empty;

            AnimatorState flinch = sm.AddState("Flinch");
            flinch.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFlinch);
            flinch.writeDefaultValues = false;

            AnimatorStateTransition toFlinch = sm.AddAnyStateTransition(flinch);
            toFlinch.AddCondition(AnimatorConditionMode.If, 0f, "HitReact");
            toFlinch.hasExitTime = false;
            toFlinch.duration = 0.05f;
            toFlinch.canTransitionToSelf = true;

            AnimatorStateTransition back = flinch.AddTransition(empty);
            back.hasExitTime = true;
            back.hasFixedDuration = true;
            back.exitTime = 0.85f;
            back.duration = 0.08f;

            EditorUtility.SetDirty(controller);
            return true;
        }

        /// <summary>Returns true when the Die parameter or Death state was added.
        /// No-op when both are already present, so a later Build All does not dirty the asset.</summary>
        static bool EnsureDeathState(AnimatorController controller)
        {
            if (controller == null) return false;

            bool dirty = false;
            bool hasDie = false;
            AnimatorControllerParameter[] parms = controller.parameters;
            for (int i = 0; i < parms.Length; i++)
            {
                if (parms[i].name == "Die")
                {
                    hasDie = true;
                    break;
                }
            }
            if (!hasDie)
            {
                controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
                dirty = true;
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            ChildAnimatorState[] children = sm.states;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].state != null && children[i].state.name == "Death")
                    return dirty;
            }

            AnimationClip clip = LoadHumanClip(ClipDeath);
            if (clip == null)
            {
                Debug.LogError("[SceneBuilder] " + ClipDeath + " missing clip — Death state skipped.");
                return dirty;
            }

            AnimatorState death = sm.AddState("Death");
            death.motion = clip;
            AnimatorStateTransition toDeath = sm.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            toDeath.hasExitTime = false;
            toDeath.duration = 0.05f;
            toDeath.canTransitionToSelf = false;
            EditorUtility.SetDirty(controller);
            return true;
        }

        static AnimationCurve FlinchMuscleCurve(float peak)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.06f, peak),
                new Keyframe(0.25f, 0f));
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        static AnimationClip LoadHumanClip(string fbxPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            for (int i = 0; i < assets.Length; i++)
            {
                var clip = assets[i] as AnimationClip;
                if (clip == null) continue;
                if (clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                return clip;
            }
            return null;
        }
    }
}
