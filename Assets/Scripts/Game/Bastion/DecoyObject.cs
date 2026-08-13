using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: placed decoy prop. Stats pushed at spawn — never serialized.
    /// Player fire cannot damage it: there is no <see cref="Damageable"/>; Gatekeepers call
    /// <see cref="ApplyDamage"/> with their Hatch-damage value. A networked prefab is spawned
    /// when a session is live; solo still builds a primitive.</summary>
    public sealed class DecoyObject : MonoBehaviour
    {
        const string Tag = "Decoy";

        DecoyBay _bay;
        float _hp;
        float _maxHp;

        public float Hp => _hp;
        public float MaxHp => _maxHp;
        public bool IsAlive => _hp > 0f;

        public void Configure(DecoyBay bay, float hp)
        {
            _bay = bay;
            _maxHp = hp;
            _hp = hp;
        }

        /// <summary>Gatekeeper Hatch-damage intake. Returns true when this hit destroys the decoy.</summary>
        public bool ApplyDamage(float amount)
        {
            if (_bay == null || amount <= 0f || _hp <= 0f) return false;
            bool destroyed = _bay.State.TakeDamage(amount);
            _hp = _bay.State.Hp;
            if (destroyed)
            {
                Log.Event(Tag, $"destroyed — absorbed {_bay.State.AbsorbedSeconds:0.##} gatekeeper-seconds total");
                _bay.NotifyDestroyed(this);
                DespawnInternal();
            }
            return destroyed;
        }

        /// <summary>Silent removal for a run reset: no destroyed line, no bay notify — the bay
        /// clears its own handle and state. The prop's CREATE (<see cref="Spawn"/>) and both
        /// DESTROY paths live in this file; nothing else destroys a live decoy.</summary>
        public void Despawn()
        {
            DespawnInternal();
        }

        void DespawnInternal()
        {
            var net = GetComponent<NetworkObject>();
            if (net != null && net.IsSpawned)
            {
                var nm = NetworkManager.Singleton;
                if (nm != null && nm.IsServer) net.Despawn(true);
                return;
            }
            Destroy(gameObject);
        }

        /// <summary>Build a decoy at world position. Caller registers it with the bay.
        /// When <paramref name="networkPrefab"/> is set and this instance is the server,
        /// the prefab is spawned as a NetworkObject; otherwise a local primitive.</summary>
        public static DecoyObject Spawn(Vector3 position, DecoyBay bay, float hp,
            GameObject networkPrefab = null)
        {
            Vector3 at = position + Vector3.up * 0.6f;
            var nm = NetworkManager.Singleton;
            if (networkPrefab != null && nm != null && nm.IsListening && nm.IsServer)
            {
                var go = Object.Instantiate(networkPrefab, at, Quaternion.identity);
                go.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
                var decoy = go.GetComponent<DecoyObject>();
                if (decoy == null) decoy = go.AddComponent<DecoyObject>();
                decoy.Configure(bay, hp);
                var net = go.GetComponent<NetworkObject>();
                if (net != null && !net.IsSpawned)
                    net.Spawn(true);
                return decoy;
            }

            return SpawnPrimitive(at, bay, hp);
        }

        static DecoyObject SpawnPrimitive(Vector3 at, DecoyBay bay, float hp)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Decoy";
            go.transform.position = at;
            go.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);

            // Ignore Raycast so player fire never finds a Damageable / collider hit on the decoy.
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) go.layer = ignore;

            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    var mat = new Material(shader) { color = FxColors.DecoyBody };
                    rend.sharedMaterial = mat;
                }
            }

            var decoy = go.AddComponent<DecoyObject>();
            decoy.Configure(bay, hp);
            return decoy;
        }
    }
}
