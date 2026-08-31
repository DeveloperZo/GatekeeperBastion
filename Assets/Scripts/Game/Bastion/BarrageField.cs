using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Dump aura (Depot crew). Follows the caster; guns read
    /// <see cref="FireMulAt"/>. Does not slow Gatekeepers — the caster's
    /// <see cref="FirstPersonController.MoveSpeedMul"/> is the foot tax.</summary>
    public sealed class BarrageField : MonoBehaviour
    {
        static readonly List<BarrageField> Live = new List<BarrageField>();

        Vector3 _origin;
        float _radiusSq;
        float _fireMul = 1f;
        Transform _follow;

        public static float FireMulAt(Vector3 position)
        {
            float mul = 1f;
            for (int i = 0; i < Live.Count; i++)
            {
                var f = Live[i];
                if (f == null) continue;
                Vector3 d = position - f._origin;
                if (d.sqrMagnitude <= f._radiusSq && f._fireMul > mul)
                    mul = f._fireMul;
            }
            if (mul > BoarderProtocol.BarrageFireMul) mul = BoarderProtocol.BarrageFireMul;
            return mul;
        }

        /// <summary>Aura parented to the caster so Dump moves with them.</summary>
        public static BarrageField Plant(Transform host)
        {
            var go = new GameObject("DumpAura");
            if (host != null) go.transform.SetParent(host, false);
            else go.transform.position = Vector3.zero;
            var field = go.AddComponent<BarrageField>();
            field._follow = host;
            field._origin = host != null ? host.position : Vector3.zero;
            float r = BoarderProtocol.Radius(BoarderAbilityId.Barrage);
            field._radiusSq = r * r;
            field._fireMul = BoarderProtocol.FireMul(BoarderAbilityId.Barrage);
            Live.Add(field);
            AttachDisc(go.transform);
            return field;
        }

        /// <summary>World-point plant for net scenery that has no host transform yet.
        /// Prefer <see cref="Plant(Transform)"/> on the caster.</summary>
        public static BarrageField Plant(Vector3 origin)
        {
            var go = new GameObject("DumpAura");
            go.transform.position = origin;
            var field = go.AddComponent<BarrageField>();
            field._follow = null;
            field._origin = origin;
            float r = BoarderProtocol.Radius(BoarderAbilityId.Barrage);
            field._radiusSq = r * r;
            field._fireMul = BoarderProtocol.FireMul(BoarderAbilityId.Barrage);
            Live.Add(field);
            AttachDisc(go.transform);
            return field;
        }

        public static void Uproot(BarrageField field)
        {
            if (field == null) return;
            Live.Remove(field);
            Object.Destroy(field.gameObject);
        }

        void LateUpdate()
        {
            if (_follow != null)
                _origin = _follow.position;
        }

        static void AttachDisc(Transform parent)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "DumpDisc";
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            disc.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
            var col = disc.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) disc.layer = ignore;
            var rend = disc.GetComponent<Renderer>();
            if (rend == null) return;
            var shader = Shader.Find("GKB/FxSolid");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return;
            var mat = new Material(shader) { color = new Color(1f, 0.55f, 0.15f, 0.35f) };
            rend.sharedMaterial = mat;
        }

        void OnDisable()
        {
            Live.Remove(this);
        }
    }
}
