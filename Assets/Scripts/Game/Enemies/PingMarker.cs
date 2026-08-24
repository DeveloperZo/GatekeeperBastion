using System.Collections.Generic;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>VEIL's PING made visible: a flat marker riding a Gatekeeper that draws through
    /// geometry for the length of the window, then removes itself. Every Boarder sees it — the
    /// crew getting the map is the whole output of the ability, so this runs on each client off
    /// a broadcast rather than only where the caster stands.
    /// <para>Self-expiring by design: the marker owns its own lifetime, so nothing has to hold a
    /// window open on its behalf, and a Gatekeeper dying mid-window takes its marker with it.
    /// A Gatekeeper spawned after the pulse gets no marker — the pulse is a snapshot of the room
    /// at the moment it was called, which is also how it reads in play.</para></summary>
    public sealed class PingMarker : MonoBehaviour
    {
        /// <summary>How far above the body the marker floats, in metres.</summary>
        const float HeightMetres = 2.3f;
        const float SizeMetres = 0.45f;

        static readonly Color MarkerColor = new Color(0.55f, 0.95f, 1f, 0.85f);
        static Material _shared;

        float _remaining;

        /// <summary>Mark every live Gatekeeper for the window. Safe to call with an empty or
        /// null ledger — an empty room simply produces no markers.</summary>
        public static void ShowAll(IReadOnlyList<EnemyGatekeeper> live, float seconds)
        {
            if (live == null || seconds <= 0f) return;
            for (int i = 0; i < live.Count; i++)
            {
                var gk = live[i];
                if (gk == null) continue;
                Attach(gk.transform, seconds);
            }
        }

        /// <summary>Public since the faucet-loop graybox marks its origin terminal with
        /// the same quad — the host is any Transform, not only a Gatekeeper's.</summary>
        public static void Attach(Transform host, float seconds)
        {
            // One marker per body: a second pulse refreshes the first rather than stacking.
            var existing = host.GetComponentInChildren<PingMarker>();
            if (existing != null)
            {
                existing._remaining = Mathf.Max(existing._remaining, seconds);
                return;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "PingMarker";
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(0f, HeightMetres, 0f);
            go.transform.localScale = new Vector3(SizeMetres, SizeMetres, SizeMetres);

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) go.layer = ignore;

            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = SharedMaterial();

            var marker = go.AddComponent<PingMarker>();
            marker._remaining = seconds;
        }

        /// <summary>One material for every marker in the room. Built once; the shader is the
        /// project's own always-on-top pass, and a missing shader degrades to no marker rather
        /// than to a magenta one.</summary>
        static Material SharedMaterial()
        {
            if (_shared != null) return _shared;
            var shader = Shader.Find("GKB/AlwaysOnTop");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return null;
            _shared = new Material(shader) { color = MarkerColor };
            _shared.hideFlags = HideFlags.HideAndDontSave;
            return _shared;
        }

        void LateUpdate()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) Destroy(gameObject);
        }

        /// <summary>Face the viewer. Billboarding is what makes a flat quad readable from
        /// anywhere in the room, which is the entire point of marking a body you cannot see.
        /// <para>Done here rather than in a tick because <c>Camera.current</c> is only set while
        /// rendering; read anywhere else it is null, and the quad would sit edge-on and
        /// invisible for the whole window.</para></summary>
        void OnWillRenderObject()
        {
            var cam = Camera.current;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}
