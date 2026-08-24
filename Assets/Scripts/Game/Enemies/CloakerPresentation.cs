using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>REPLICATED-STATE (applied on every peer): swaps the Cloaker's body between its
    /// two committed materials — solid when observed or freshly hit, faded when cloaked. A
    /// SEPARATE component from the AI on purpose: NetEnemy disables EnemyGatekeeper on clones,
    /// so presentation living on the AI would be dead on a guest's screen. The host's
    /// CloakerGatekeeper calls this directly; clones get it through NetEnemy's replicated
    /// Cloaked bool.</summary>
    /// <remarks>
    /// Assigns sharedMaterials only — pointing a renderer at a different committed asset is
    /// free. Never touch .material (leaks an instance per spawn) and never mutate either asset
    /// at runtime. The references are serialized by SceneBuilder at prefab build (prefab
    /// freeze), not resolved at runtime; unwired (a hand-placed body) this is a silent no-op.
    /// Damageable's hit flash rides a MaterialPropertyBlock on top of whichever material is
    /// assigned, so the swap and the flash never fight.
    /// </remarks>
    public sealed class CloakerPresentation : MonoBehaviour
    {
        [Tooltip("The body's skinned renderers. Wired by SceneBuilder.")]
        public SkinnedMeshRenderer[] renderers;

        [Tooltip("Packs/Gatekeepers/Cloaker.mat — observed / revealed. Wired by SceneBuilder.")]
        public Material solidMaterial;

        [Tooltip("Packs/Gatekeepers/Cloaker_Cloaked.mat — unobserved. Wired by SceneBuilder.")]
        public Material cloakedMaterial;

        // Per-renderer slot arrays, built once: sharedMaterials assignment wants the whole
        // array, and building it inside SetCloaked would allocate on every flip.
        Material[][] _solidSets;
        Material[][] _cloakedSets;
        bool _built;

        void Awake()
        {
            BuildSets();
        }

        void BuildSets()
        {
            if (_built) return;
            if (renderers == null || solidMaterial == null || cloakedMaterial == null) return;
            _solidSets = new Material[renderers.Length][];
            _cloakedSets = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                int slots = renderers[i].sharedMaterials.Length;
                _solidSets[i] = new Material[slots];
                _cloakedSets[i] = new Material[slots];
                for (int s = 0; s < slots; s++)
                {
                    _solidSets[i][s] = solidMaterial;
                    _cloakedSets[i][s] = cloakedMaterial;
                }
            }
            _built = true;
        }

        /// <summary>Point every body renderer at the committed asset for this state.
        /// Idempotent and null-safe; callers do not need to dedupe.</summary>
        public void SetCloaked(bool cloaked)
        {
            BuildSets(); // guard: a caller can land before Awake on a freshly spawned clone
            if (!_built) return;
            var sets = cloaked ? _cloakedSets : _solidSets;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || sets[i] == null) continue;
                renderers[i].sharedMaterials = sets[i];
            }
        }
    }
}
