using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY: placed decoy prop. Stats pushed at spawn — never serialized.
    /// Player fire cannot damage it: there is no <see cref="Damageable"/>; Gatekeepers call
    /// <see cref="ApplyDamage"/> with their Hatch-damage value.</summary>
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
                Destroy(gameObject);
            }
            return destroyed;
        }

        /// <summary>Silent removal for a run reset: no destroyed line, no bay notify — the bay
        /// clears its own handle and state. The prop's CREATE (<see cref="Spawn"/>) and both
        /// DESTROY paths live in this file; nothing else destroys a live decoy.</summary>
        public void Despawn()
        {
            Destroy(gameObject);
        }

        /// <summary>Build a gray-box decoy at world position. Caller registers it with the bay.</summary>
        public static DecoyObject Spawn(Vector3 position, DecoyBay bay, float hp)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Decoy";
            go.transform.position = position + Vector3.up * 0.6f;
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
