using System.Collections.Generic;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE: static registry of live PlayerHealth instances (self-registered
    /// in OnEnable/OnDisable). The ONLY sanctioned way to find "the player" in new code —
    /// co-op ready: enemies target Nearest, local-only UI reads Local.</summary>
    /// <remarks>
    /// Every accessor is Unity-null-safe and prunes destroyed entries before use. A static list
    /// outlives scene loads (and, with Unity 6 "Enter Play Mode without Domain Reload", outlives
    /// play sessions), so an entry can be a DESTROYED component that still occupies a slot:
    /// dereferencing its .transform throws NullReferenceException, and returning it from Local
    /// makes every caller's `!= null` test fail silently (which is how the death-screen cursor
    /// stops being freed). Both are guarded here.
    /// </remarks>
    public static class PlayerRegistry
    {
        const string Tag = "PlayerRegistry";

        static readonly List<PlayerHealth> Registry = new List<PlayerHealth>();

        /// <summary>Wipes the static list at play-mode start — prevents destroyed players from a
        /// previous session surviving into this one when domain reload is disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Registry.Clear();
        }

        /// <summary>All currently enabled, live players (destroyed entries pruned first).</summary>
        public static IReadOnlyList<PlayerHealth> All
        {
            get
            {
                Prune();
                return Registry;
            }
        }

        /// <summary>The local FPS rig (FirstPersonController), or null. Prefers a registry
        /// entry that has an FPC so remote NetPlayerAvatar puppets registered for targeting
        /// cannot steal Local after a NetworkSceneManager load. Never returns a destroyed
        /// component — a fake-null Local silently disables cursor release on death.</summary>
        public static PlayerHealth Local
        {
            get
            {
                Prune();
                for (int i = 0; i < Registry.Count; i++)
                {
                    var candidate = Registry[i];
                    if (candidate == null) continue;
                    if (candidate.GetComponent<FirstPersonController>() != null)
                        return candidate;
                }
                return null;
            }
        }

        /// <summary>The player closest to a world position, or null when none are registered.</summary>
        public static PlayerHealth Nearest(Vector3 position)
        {
            Prune(); // guard: Registry[i].transform on a destroyed player is an NRE (enemy retarget path)
            PlayerHealth best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < Registry.Count; i++)
            {
                var candidate = Registry[i];
                if (candidate == null) continue; // guard: entry destroyed mid-iteration
                float sqr = (candidate.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>The closest player that is still alive, or null. Enemy AI uses this so a
        /// downed player stops drawing aggro once the run has ended.</summary>
        public static PlayerHealth NearestAlive(Vector3 position)
        {
            Prune();
            PlayerHealth best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < Registry.Count; i++)
            {
                var candidate = Registry[i];
                if (candidate == null || candidate.IsDead) continue;
                float sqr = (candidate.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>Called by PlayerHealth.OnEnable.</summary>
        public static void Register(PlayerHealth player)
        {
            if (player == null) return;
            Prune();
            if (!Registry.Contains(player)) Registry.Add(player);
            Log.Info(Tag, $"Registered {player.name} (count {Registry.Count}).");
        }

        /// <summary>Called by PlayerHealth.OnDisable.</summary>
        public static void Unregister(PlayerHealth player)
        {
            Registry.Remove(player);
            Prune();
        }

        /// <summary>Drops destroyed (Unity fake-null) entries. Cheap: the list is 1 long in solo.</summary>
        static void Prune()
        {
            for (int i = Registry.Count - 1; i >= 0; i--)
            {
                if (Registry[i] != null) continue;
                Registry.RemoveAt(i);
                // Guard trip: a destroyed player left in the registry would NRE in Nearest and
                // make Local fake-null (cursor never freed on the lose screen).
                Log.Warn(Tag, "Pruned a destroyed PlayerHealth from the registry (stale scene/domain state).");
            }
        }
    }
}
