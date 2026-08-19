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
    /// <summary>SceneBuilder, enemy assets: the three gatekeeper prefabs and the materials that make
    /// them tellable apart. Same idempotent contract as the rest of the builder — each prefab is
    /// saved to a fixed path so re-running keeps its GUID and any scene reference to it.</summary>
    /// <remarks>
    /// COLOUR AND SIZE ARE THE ONLY TUTORIAL THIS GAME HAS. A player meets three kinds at once
    /// and has to triage them before knowing any of their names, so the read is built from two
    /// channels that survive a dark corridor and a glance:
    ///   Default — the red they already know, unchanged, at 1.0 scale.
    ///   Hunter  — bright amber, 0.85 scale. Small and loud: the fast fragile one coming for you.
    ///   Ram     — saturated violet-steel, 1.4 scale. Twice the silhouette of a Hunter.
    /// Ram's violet is deliberately brighter and more saturated than the bastion's own walls
    /// (0.22, 0.17, 0.30) — a genuinely dark violet body against a dark violet hull is invisible,
    /// which is the opposite of the point. Its size does the rest of the work.
    /// Silhouette is one FaxLab3D robot per kind (Default UtilityBot, Hunter Hunter, Ram
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
        const string RamPrefabPath = PrefabDir + "/RamGatekeeper.prefab";
        const string GatekeeperPackDir = "Assets/Packs/Gatekeepers";
        const string GatekeeperControllerPath = GatekeeperPackDir + "/Gatekeeper.controller";
        const string DefaultWrapperPath = GatekeeperPackDir + "/Default.prefab";
        const string HunterWrapperPath = GatekeeperPackDir + "/Hunter.prefab";
        const string ThiefWrapperPath = GatekeeperPackDir + "/Thief.prefab";
        // One robot model per kind. Silhouette is what tells the kinds apart now; before this
        // they were three Synty humanoids separated by an emission tint, which asked the player to
        // read a colour on a body they had already learned.
        const string RobotDir = "Assets/FaxLab3D/SciFiRobotCharactersPack/";
        const string DefaultFbx = RobotDir + "Models/SK_UtilityBot.fbx";
        const string HunterFbx = RobotDir + "Models/SK_Hunter.fbx";
        const string RamFbx = RobotDir + "Models/SK_Exterminator.fbx";
        const string PackDefault = RobotDir + "Prefabs/UtilityBot_Base.prefab";
        const string PackHunter = RobotDir + "Prefabs/Hunter_Base.prefab";
        const string PackThief = RobotDir + "Prefabs/Exterminator_Base.prefab";
        const string ClipIdle = "Assets/Packs/Boarder/Anim_PlayerIdle.fbx";
        const string ClipWalk = "Assets/Packs/Boarder/Anim_Walking.fbx";
        const string ClipRun = "Assets/Packs/Boarder/Anim_PlayerRun.fbx";

        /// <summary>The three gatekeeper prefabs, wired into the WaveSpawner as a set.</summary>
        sealed class EnemyPrefabs
        {
            public GameObject Default, Hunter, Ram;
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
                    "EnemyGatekeeper", EnemyPrefabPath, pal.Enemy, GatekeeperKind.Default, 1f, dress),
                Hunter = CreateGatekeeperPrefab<HunterGatekeeper>(
                    "HunterGatekeeper", HunterPrefabPath, pal.Hunter, GatekeeperKind.Hunter, 0.85f, dress),
                Ram = CreateGatekeeperPrefab<RamGatekeeper>(
                    "RamGatekeeper", RamPrefabPath, pal.Ram, GatekeeperKind.Ram, 1.4f, dress),
            };
        }

        /// <summary>One gatekeeper prefab: capsule root, NavMeshAgent, Damageable, the kind's own
        /// EnemyGatekeeper subclass, and the net components (inert until a session Spawn()s).
        /// If the asset already exists, load and dress it — never CreatePrimitive-wipe.
        /// <paramref name="scale"/> scales the silhouette AND the agent footprint together,
        /// so a Ram takes up the room it looks like it takes up. Visual child localScale is
        /// 1/scale so War_Robot does not stack a second 1.4 on the root.</summary>
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
            // Geometry follows the capsule: a 1.4-scale Ram is 1.4x as wide and as tall, and its
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
                DressVisual(go, kind, scale, wrapper);
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

        static void DressVisual(GameObject root, GatekeeperKind kind, float scale, GameObject wrapper)
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
            float inv = 1f / Mathf.Max(0.01f, scale);
            visual.transform.localScale = new Vector3(inv, inv, inv);
        }

        static string WrapperPath(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return HunterWrapperPath;
            if (kind == GatekeeperKind.Ram) return ThiefWrapperPath;
            return DefaultWrapperPath;
        }

        static string PackPrefabPath(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return PackHunter;
            if (kind == GatekeeperKind.Ram) return PackThief;
            return PackDefault;
        }

        static string PackModelPath(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return HunterFbx;
            if (kind == GatekeeperKind.Ram) return RamFbx;
            return DefaultFbx;
        }

        static string VisualChildName(GatekeeperKind kind)
        {
            if (kind == GatekeeperKind.Hunter) return "Hunter";
            if (kind == GatekeeperKind.Ram) return "Thief";
            return "Default";
        }

        static bool GatekeeperModelsRetarget(out string reason)
        {
            // Gate on the models the kinds are actually dressed from. Checking a fixed pair of
            // Synty FBXs kept passing after the kinds moved off them, which is a guard that reports
            // green about files nothing renders.
            if (!FbxIsHuman(DefaultFbx, out reason)) return false;
            if (!FbxIsHuman(HunterFbx, out reason)) return false;
            if (!FbxIsHuman(RamFbx, out reason)) return false;
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
            Debug.Log("[SceneBuilder] gatekeeper models retarget: all three avatars are Humanoid.");
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
            EnsureWrapper(GatekeeperKind.Ram, controller);

            // Colour is the tutorial: kind-tinted emission on the pack's authored glow regions.
            // Runs after EnsureWrapper every build, so an already-dressed wrapper still converges.
            EnsureWrapperEmission(GatekeeperKind.Default, pal.Enemy.color);
            EnsureWrapperEmission(GatekeeperKind.Hunter, pal.Hunter.color);
            EnsureWrapperEmission(GatekeeperKind.Ram, pal.Ram.color);
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
            if (existing != null) return existing;

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

            // No Mixamo melee take in the Boarder clips — Idle stand-in, short so the
            // GatekeeperAttackInterval swing clock is not locked behind an 8s idle loop.
            AnimatorState attack = sm.AddState("Attack");
            attack.motion = idle;
            AnimatorStateTransition toAttack = sm.AddAnyStateTransition(attack);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack.hasExitTime = false;
            toAttack.duration = 0.05f;
            toAttack.canTransitionToSelf = false;
            AnimatorStateTransition back = attack.AddTransition(move);
            back.hasExitTime = true;
            back.hasFixedDuration = true;
            back.exitTime = 0.2f;
            back.duration = 0.08f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
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
