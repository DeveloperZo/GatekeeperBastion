using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: code-built particle effects for the gray-box. Materials come from
    /// Resources/Fx (project shaders GKB/FxParticle and GKB/FxSolid) so player builds cannot strip
    /// them the way Shader.Find on Built-In/URP names can. Everything is a one-shot burst
    /// ParticleSystem that destroys itself.</summary>
    /// <remarks>
    /// Cosmetic only: every entry point is a no-op when the app is quitting or a scene load is
    /// in flight. Creating GameObjects while a scene is unloading spawns them into the dying
    /// scene (and can throw during teardown), which is the last thing you want firing on the
    /// frame the player dies and the enemy AI is still swinging at the hatch.
    /// </remarks>
    public static class Effects
    {
        const string Tag = "Effects";

        static Material _particleMat;
        static Material _solidMat;
        static bool _suspended;

        /// <summary>Clears statics at play-mode start (domain-reload-disabled safety) and wires the
        /// sceneLoaded hook that lifts the suspend flag — and carries the one scene-entry line.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _particleMat = null;
            _solidMat = null;
            _suspended = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _suspended = false;
            // The ONE scene-entry line in the whole log, and the only structural line kept: it is
            // the separator between "what I did in the ship" and "what happened in the bastion".
            // Tagged Scene, not Effects — the FX system is merely where the sceneLoaded hook lives.
            Log.Event("Scene", $"—— entered {scene.name} ——");
        }

        static void OnQuitting()
        {
            _suspended = true;
        }

        /// <summary>True while FX creation is disabled (scene load in flight, or app quitting).</summary>
        public static bool Suspended => _suspended || !Application.isPlaying;

        /// <summary>Called by SceneFlow immediately before a scene load: stop making objects that
        /// would land in the scene being unloaded.</summary>
        public static void SuspendForSceneChange()
        {
            _suspended = true;
        }

        /// <summary>Re-enable FX (used when a scene load was aborted).</summary>
        public static void Resume()
        {
            _suspended = false;
        }

        static Material ParticleMat
        {
            get
            {
                if (_particleMat == null) // Unity-null: UnloadUnusedAssets can reap it across scene loads
                {
                    _particleMat = LoadPinnedMat("Fx/ParticleUnlit", "GKB/FxParticle");
                    if (_particleMat == null)
                        Log.Warn(Tag, "No usable particle shader; particle FX disabled this session.");
                }
                return _particleMat;
            }
        }

        static Material SolidMat
        {
            get
            {
                if (_solidMat == null)
                {
                    _solidMat = LoadPinnedMat("Fx/SolidUnlit", "GKB/FxSolid");
                    if (_solidMat == null)
                        Log.Warn(Tag, "No usable solid FX shader; pulse FX disabled this session.");
                }
                return _solidMat;
            }
        }

        /// <summary>Resources material first (pulls the project shader into the player), then
        /// Shader.Find on the project shader name. Built-In/URP names are not used — they strip.</summary>
        static Material LoadPinnedMat(string resourcesPath, string projectShaderName)
        {
            var template = Resources.Load<Material>(resourcesPath);
            if (template != null)
                return new Material(template) { hideFlags = HideFlags.HideAndDontSave };

            var shader = Shader.Find(projectShaderName);
            if (shader == null) return null;
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        /// <summary>Small spark burst at a weapon impact point.</summary>
        public static void ImpactSparks(Vector3 position)
            => Burst(position, FxColors.FxSparks, 14, speed: 5f, size: 0.07f, lifetime: 0.28f, gravity: 0.6f);

        /// <summary>CRITICAL HIT impact — the same spark burst as ImpactSparks but roughly triple
        /// the particles, faster, and pushed toward white, so a crit reads as a crit from across
        /// the room without any HUD text. Two bursts (hot core + white flash) for the same reason
        /// DeathExplosion uses two: a single tinted burst just looks like a normal hit that lagged.</summary>
        public static void CritSparks(Vector3 position)
        {
            Burst(position, FxColors.FxBurst, 40, speed: 8f, size: 0.11f, lifetime: 0.34f, gravity: 0.5f);
            Burst(position, Color.white, 14, speed: 3.2f, size: 0.13f, lifetime: 0.22f, gravity: 0f);
        }

        /// <summary>Big kill-confirm explosion where an enemy died. Sound rides the visual —
        /// one seam, so a future second caller can never produce a silent death.</summary>
        public static void DeathExplosion(Vector3 position, Color tint)
        {
            Burst(position, tint, 42, speed: 7f, size: 0.14f, lifetime: 0.5f, gravity: 0.4f);
            Burst(position, Color.white, 10, speed: 2.5f, size: 0.09f, lifetime: 0.35f, gravity: 0f);
            CombatSfx.EnemyDown(position);
        }

        /// <summary>Short flash at the muzzle each shot.</summary>
        public static void MuzzleFlash(Vector3 position)
            => Burst(position, FxColors.FxMuzzle, 6, speed: 1.6f, size: 0.05f, lifetime: 0.09f, gravity: 0f);

        /// <summary>Sparks when something hits the hatch — damage you can see across the room,
        /// and (same seam) hear from behind you.</summary>
        public static void HatchDamageSparks(Vector3 position)
        {
            Burst(position, FxColors.FxBlood, 20, speed: 4f, size: 0.1f, lifetime: 0.4f, gravity: 0.8f);
            CombatSfx.HatchStruck(position);
        }

        /// <summary>Overcharge ignition — the turret going redline, readable from across the
        /// room. Window length is the machine's charge; this is the one-shot that says it
        /// started.</summary>
        public static void OverchargeIgnite(Vector3 position)
        {
            Burst(position, FxColors.FxOvercharge, 48, speed: 7f, size: 0.16f, lifetime: 0.45f, gravity: 0.15f);
            Burst(position, Color.white, 16, speed: 3f, size: 0.14f, lifetime: 0.22f, gravity: 0f);
            Burst(position, FxColors.FxBurst, 22, speed: 4.5f, size: 0.11f, lifetime: 0.32f, gravity: 0.2f);
        }

        /// <summary>EMP blast at the Outrider. Particles travel the catch radius so a miss
        /// and a hit read as the same disc, not a spark at the caster's feet.</summary>
        public static void EmpBlast(Vector3 position, float radius)
        {
            float r = radius > 0.5f ? radius : 0.5f;
            const float life = 0.55f;
            float speed = r / life;
            Burst(position, FxColors.FxEmp, 28, speed: 5f, size: 0.18f, lifetime: 0.3f, gravity: 0f, shapeRadius: 0.4f);
            Burst(position, FxColors.FxEmp, 80, speed: speed, size: 0.14f, lifetime: life, gravity: 0f, shapeRadius: 0.25f);
            Burst(position, Color.white, 20, speed: 3f, size: 0.16f, lifetime: 0.22f, gravity: 0f, shapeRadius: 0.2f);
            PulseDisc.Spawn(position, r, life, FxColors.FxEmp);
        }

        /// <summary>Hotter muzzle while a machine's special is spending charge.</summary>
        public static void SpecialMuzzle(Vector3 position, Color tint)
        {
            Burst(position, tint, 18, speed: 3.2f, size: 0.1f, lifetime: 0.16f, gravity: 0f);
            Burst(position, Color.white, 8, speed: 1.8f, size: 0.08f, lifetime: 0.1f, gravity: 0f);
        }

        /// <summary>Looping ember parented to a machine for the length of Special. Caller
        /// destroys the returned object when the window ends — leaving it would keep
        /// emitting after Exhausted.</summary>
        public static GameObject AttachSpecialAura(Transform parent, Color color)
        {
            if (Suspended || parent == null) return null;
            var mat = ParticleMat;
            if (mat == null) return null;

            var go = new GameObject("FXAura");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.35f;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = color;
            main.gravityModifier = -0.15f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 22f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;

            ps.Play();
            return go;
        }

        static void Burst(Vector3 position, Color color, int count, float speed, float size,
            float lifetime, float gravity, float shapeRadius = 0.08f)
        {
            if (Suspended) return; // guard: never create objects into a scene that is unloading / while quitting
            var mat = ParticleMat;
            if (mat == null) return;

            var go = new GameObject("FX");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = shapeRadius;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;

            ps.Play();
            Object.Destroy(go, lifetime + 0.5f);
        }

        /// <summary>Floor disc that grows to the blast radius. One-shot; destroys itself.</summary>
        sealed class PulseDisc : MonoBehaviour
        {
            float _life;
            float _age;
            float _endRadius;
            Material _mat;

            public static void Spawn(Vector3 position, float radius, float life, Color color)
            {
                if (Suspended) return;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "FXPulse";
                go.transform.position = position + Vector3.up * 0.06f;
                go.transform.localScale = new Vector3(0.7f, 0.05f, 0.7f);
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = go.GetComponent<Renderer>();
                Material mat = null;
                if (rend != null)
                {
                    var template = SolidMat;
                    if (template != null)
                    {
                        mat = new Material(template)
                        {
                            color = color,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        rend.sharedMaterial = mat;
                    }
                }

                var pulse = go.AddComponent<PulseDisc>();
                pulse._life = life;
                pulse._age = 0f;
                pulse._endRadius = radius;
                pulse._mat = mat;
            }

            void Update()
            {
                _age += Time.deltaTime;
                float t = _life > 0f ? Mathf.Clamp01(_age / _life) : 1f;
                float r = Mathf.Lerp(0.4f, _endRadius, t);
                transform.localScale = new Vector3(r * 2f, 0.05f, r * 2f);
                if (_age >= _life) Destroy(gameObject);
            }

            void OnDestroy()
            {
                if (_mat != null) Destroy(_mat);
            }
        }
    }
}
