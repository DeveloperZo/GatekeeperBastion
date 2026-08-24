using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The machine's life made visible: a billboarded bar floating over FORGE's
    /// turret, filled by fraction and stepped from friendly blue through amber to red so
    /// "about to fall" reads from across the room. Presentation only — the value comes from
    /// <see cref="TurretObject"/> each frame, which reads the server's state on the machine
    /// that shoots and the mirrored state on every scenery copy, so host, owner and crew all
    /// see the same bar. The overcharge burn draining it in real time is what tells the
    /// operator what the redline is costing.</summary>
    public sealed class TurretLifeBar : MonoBehaviour
    {
        /// <summary>How far above the turret base the bar floats, in metres.</summary>
        const float HeightMetres = 1.35f;
        const float WidthMetres = 0.9f;
        const float ThicknessMetres = 0.12f;

        static readonly Color BackColor = new Color(0.07f, 0.09f, 0.11f, 1f);
        static readonly Color HealthyColor = new Color(0.35f, 0.75f, 1f, 1f);
        static readonly Color WarningColor = new Color(1f, 0.72f, 0.2f, 1f);
        static readonly Color CriticalColor = new Color(1f, 0.25f, 0.15f, 1f);

        static Material _back;
        static Material _healthy;
        static Material _warning;
        static Material _critical;

        Transform _fillPivot;
        Renderer _fill;
        float _shown = -1f;

        /// <summary>Build the bar under a turret root. One per machine, full on arrival.</summary>
        public static TurretLifeBar Attach(Transform host)
        {
            var root = new GameObject("TurretLifeBar");
            root.transform.SetParent(host, false);
            root.transform.localPosition = new Vector3(0f, HeightMetres, 0f);

            var back = MakeQuad(root.transform, Shared(ref _back, BackColor));
            back.transform.localScale = new Vector3(WidthMetres, ThicknessMetres, 1f);

            // The fill empties toward the left edge rather than shrinking about the centre:
            // the pivot sits on that edge and carries the scale, with the quad hung half a
            // width to its right.
            var pivot = new GameObject("Fill");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(-WidthMetres * 0.5f, 0f, -0.002f);

            var fill = MakeQuad(pivot.transform, Shared(ref _healthy, HealthyColor));
            fill.transform.localPosition = new Vector3(WidthMetres * 0.5f, 0f, 0f);
            fill.transform.localScale = new Vector3(WidthMetres, ThicknessMetres * 0.6f, 1f);

            var bar = root.AddComponent<TurretLifeBar>();
            bar._fillPivot = pivot.transform;
            bar._fill = fill.GetComponent<Renderer>();
            return bar;
        }

        /// <summary>Show a life fraction. Cheap to call every frame — a repeat of the shown
        /// value returns before touching a transform or a material.</summary>
        public void SetFraction(float fraction)
        {
            if (fraction < 0f) fraction = 0f;
            if (fraction > 1f) fraction = 1f;
            if (fraction == _shown) return;
            _shown = fraction;

            if (_fillPivot != null)
            {
                Vector3 s = _fillPivot.localScale;
                s.x = fraction;
                _fillPivot.localScale = s;
            }
            if (_fill == null) return;
            Material want = fraction > 0.5f ? Shared(ref _healthy, HealthyColor)
                : fraction > 0.25f ? Shared(ref _warning, WarningColor)
                : Shared(ref _critical, CriticalColor);
            if (_fill.sharedMaterial != want) _fill.sharedMaterial = want;
        }

        static GameObject MakeQuad(Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) go.layer = ignore;
            var rend = go.GetComponent<Renderer>();
            if (rend != null && material != null) rend.sharedMaterial = material;
            return go;
        }

        /// <summary>One material per colour for every bar in the room, built once. A missing
        /// shader degrades to an untinted bar rather than a magenta one.</summary>
        static Material Shared(ref Material slot, Color color)
        {
            if (slot != null) return slot;
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) return null;
            slot = new Material(shader) { color = color };
            slot.hideFlags = HideFlags.HideAndDontSave;
            return slot;
        }

        /// <summary>Face the viewer. Camera.current is only set while rendering — read
        /// anywhere else it is null and the quads would sit edge-on and invisible.</summary>
        void OnWillRenderObject()
        {
            var cam = Camera.current;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}
