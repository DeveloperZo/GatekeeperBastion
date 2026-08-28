using System.Collections.Generic;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: trivial marker for an enemy breach/spawn location; enabled instances
    /// self-register in a static list consumed by WaveSpawner.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        static readonly List<SpawnPoint> Registry = new List<SpawnPoint>();

        /// <summary>All currently enabled spawn points.</summary>
        public static IReadOnlyList<SpawnPoint> All => Registry;

        [SerializeField, Tooltip(
            "Share of a wave that breaches here, relative to the other live closets. 1 is a "
            + "normal door; raise it on the closet at the FAR end of the lane so most of the "
            + "wave has to walk the whole approach (Zo, 2026-08-25).")]
        int weight = 1;

        /// <summary>Draw share for this closet — at least 1, so a closet can never be
        /// registered and then never picked.</summary>
        public int Weight => weight < 1 ? 1 : weight;

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
