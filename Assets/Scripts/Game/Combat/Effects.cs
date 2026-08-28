using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: code-built particle effects for the gray-box — no assets, URP-safe
    /// materials. Everything is a one-shot burst ParticleSystem that destroys itself.</summary>
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
        static bool _suspended;

        /// <summary>Clears statics at play-mode start (domain-reload-disabled safety) and wires the
        /// sceneLoaded hook that lifts the suspend flag — and carries the one scene-entry line.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _particleMat = null;
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
                    var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                    if (shader == null) shader = Shader.Find("Unlit/Color");
                    if (shader == null)
                    {
                        // Guard trip: new Material(null) errors on every call; bail out instead.
                        Log.Warn(Tag, "No usable shader found; particle FX disabled this session.");
                        return null;
                    }
                    _particleMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                return _particleMat;
            }
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

        static void Burst(Vector3 position, Color color, int count, float speed, float size, float lifetime, float gravity)
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
            shape.radius = 0.08f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;

            ps.Play();
            Object.Destroy(go, lifetime + 0.5f);
        }
    }
}
