using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): seat tint and marker.
    /// Split so the avatar file stays under cap.</summary>
    public sealed partial class NetPlayerAvatar
    {
        void ApplySeatTint()
        {
            int slot = Slot.Value;
            if (slot < 0) slot = 0;
            Color tint = SeatTints[slot % SeatTints.Length];
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    var r = _renderers[i];
                    if (r == null) continue;
                    if (_seatMarker != null && r == _seatMarker) continue;
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    // URP Lit may ignore _BaseColor on some materials — marker below is the
                    // seat discriminator when MPB tint no-ops.
                    block.SetColor("_BaseColor", tint);
                    block.SetColor("_Color", tint);
                    r.SetPropertyBlock(block);
                }
            }
            EnsureSeatMarker(slot, tint);
        }

        /// <summary>Floating quad above the capsule — size scales with seat index so peers
        /// stay distinguishable even when body tint fails.</summary>
        void EnsureSeatMarker(int slot, Color tint)
        {
            if (_seatMarker == null)
            {
                Transform existing = transform.Find("SeatMarker");
                if (existing != null)
                    _seatMarker = existing.GetComponent<Renderer>();
            }
            if (_seatMarker == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "SeatMarker";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                go.transform.localRotation = Quaternion.identity;
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                _seatMarker = go.GetComponent<Renderer>();
                if (_seatMarker != null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Unlit/Color");
                    if (shader != null)
                        _seatMarker.sharedMaterial = new Material(shader);
                }
            }
            if (_seatMarker == null) return;
            float s = 0.28f + slot * 0.12f;
            _seatMarker.transform.localScale = new Vector3(s, s, s);
            var markerBlock = new MaterialPropertyBlock();
            _seatMarker.GetPropertyBlock(markerBlock);
            markerBlock.SetColor("_BaseColor", tint);
            markerBlock.SetColor("_Color", tint);
            _seatMarker.SetPropertyBlock(markerBlock);
            // Owner hides body renderers in ApplyOwnerPresentation; marker is created after
            // that pass, so mirror the hide here.
            _seatMarker.enabled = !IsOwner;
        }
    }
}
