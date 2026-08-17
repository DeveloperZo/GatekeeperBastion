using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY planted slow zone. Static live list; EnemyGatekeeper.RecomputeSpeed
    /// reads <see cref="SlowMulAt"/>.</summary>
    public sealed class AnchorField : MonoBehaviour
    {
        static readonly List<AnchorField> Live = new List<AnchorField>();

        Vector3 _origin;
        float _radiusSq;
        float _slowMul = 1f;

        public static float SlowMulAt(Vector3 position)
        {
            float mul = 1f;
            for (int i = 0; i < Live.Count; i++)
            {
                var f = Live[i];
                if (f == null) continue;
                Vector3 d = position - f._origin;
                if (d.sqrMagnitude <= f._radiusSq && f._slowMul < mul)
                    mul = f._slowMul;
            }
            return mul;
        }

        public static AnchorField Plant(Vector3 origin)
        {
            var go = new GameObject("AnchorField");
            var field = go.AddComponent<AnchorField>();
            field._origin = origin;
            float r = BoarderProtocol.Radius(BoarderAbilityId.Anchor);
            field._radiusSq = r * r;
            field._slowMul = BoarderProtocol.Magnitude(BoarderAbilityId.Anchor);
            Live.Add(field);
            return field;
        }

        public static void Uproot(AnchorField field)
        {
            if (field == null) return;
            Live.Remove(field);
            Object.Destroy(field.gameObject);
        }

        void OnDisable()
        {
            Live.Remove(this);
        }
    }
}
