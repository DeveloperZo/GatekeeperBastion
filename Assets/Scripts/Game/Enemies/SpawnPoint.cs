using System.Collections.Generic;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: trivial marker for an enemy breach/spawn location; enabled instances
    /// self-register in a static list consumed by WaveSpawner.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        static readonly List<SpawnPoint> Registry = new List<SpawnPoint>();

        /// <summary>All currently enabled spawn points.</summary>
        public static IReadOnlyList<SpawnPoint> All => Registry;

        void OnEnable()
        {
            Registry.Add(this);
        }

        void OnDisable()
        {
            Registry.Remove(this);
        }
    }
}
